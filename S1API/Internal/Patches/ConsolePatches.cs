using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using S1API.Console;
using S1API.Internal.Utils;
#if (MONOMELON || MONOBEPINEX || IL2CPPBEPINEX)
using S1Console = ScheduleOne.Console;

#elif (IL2CPPMELON)
using S1Console = Il2CppScheduleOne.Console;
#endif

#if (IL2CPPMELON || IL2CPPBEPINEX)
using Il2CppInterop.Runtime.Injection;
#endif

namespace S1API.Internal.Patches
{
    [HarmonyPatch]
    internal static class ConsolePatches
    {
        private static readonly Logging.Log Logger = new Logging.Log("Console");
        

        /// <summary>
        /// Discover and register custom console commands derived from BaseConsoleCommand.
        /// We keep them in a managed registry and route unknown commands to them at submit time.
        /// </summary>
        /// <param name="__instance">The instance of the S1Console being initialized.</param>
        [HarmonyPatch(typeof(S1Console), "Awake")]
        [HarmonyPostfix]
        private static void AddCommands(S1Console __instance)
        {
            if (__instance == null)
                return;

            var commandTypes = ReflectionUtils.GetDerivedClasses<BaseConsoleCommand>();
            foreach (var type in commandTypes)
            {
                Logger.Msg($"Found console command: {type.FullName}");

                if (type.GetConstructor(Type.EmptyTypes) == null)
                    continue;

                try
                {
                    var userCommand = (BaseConsoleCommand)Activator.CreateInstance(type)!;
                    CustomConsoleRegistry.Register(userCommand);
                    Logger.Msg($"Registered custom command '{userCommand.CommandWord}' into managed registry");
                }
                catch (Exception e)
                {
                    Logger.Warning($"[Console] Failed to register {type.FullName}: {e.Message}");
                }
            }
        }

#if (MONOMELON || MONOBEPINEX)
        private static FieldInfo? _monoCommandsField;

        /// <summary>
        /// Routes unknown commands to our managed registry on Mono.
        /// </summary>
        [HarmonyPatch(typeof(S1Console), nameof(S1Console.SubmitCommand), new Type[] { typeof(System.Collections.Generic.List<string>) })]
        [HarmonyPrefix]
        private static bool RouteCustomCommandsMono(System.Collections.Generic.List<string> args)
        {
            try
            {
                if (args == null || args.Count == 0)
                    return true;

                string? cmd = args[0];
                if (string.IsNullOrEmpty(cmd))
                    return true;

                string key = cmd.ToLowerInvariant();

                // If the game's dictionary has this command, let it handle it
                _monoCommandsField ??= typeof(S1Console).GetField("commands", BindingFlags.NonPublic | BindingFlags.Static);
                var dict = _monoCommandsField?.GetValue(null) as IDictionary<string, S1Console.ConsoleCommand>;
                if (dict != null && dict.ContainsKey(key))
                    return true;

                // Otherwise try our managed registry
                if (CustomConsoleRegistry.TryExecuteManaged(args))
                    return false; // handled

                return true;
            }
            catch (Exception e)
            {
                Logger.Warning($"[Console] Custom command routing failed (Mono): {e.Message}");
                return true;
            }
        }
#endif

#if (IL2CPPMELON || IL2CPPBEPINEX)
        /// <summary>
        /// Routes unknown commands to our managed registry on Il2Cpp.
        /// </summary>
        [HarmonyPatch(typeof(S1Console), nameof(S1Console.SubmitCommand), new Type[] { typeof(Il2CppSystem.Collections.Generic.List<string>) })]
        [HarmonyPrefix]
        private static bool RouteCustomCommandsIl2Cpp(Il2CppSystem.Collections.Generic.List<string> args)
        {
            try
            {
                if (args == null || args.Count == 0)
                    return true;

                var first = args[0];
                if (first == null)
                    return true;

                string key = first.ToLower();

                // If the game's dictionary has this command, let it handle it
                var dict = S1Console.commands;
                if (dict != null && dict.ContainsKey(key))
                    return true;

                // Otherwise try our managed registry
                if (CustomConsoleRegistry.TryExecute(args))
                    return false; // handled

                return true;
            }
            catch (Exception e)
            {
                Logger.Warning($"[Console] Custom command routing failed (Il2Cpp): {e.Message}");
                return true;
            }
        }
#endif
    }
}
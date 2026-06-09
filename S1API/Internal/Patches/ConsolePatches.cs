using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using S1API.Console;
using S1API.Internal.Utils;
using UnityEngine;
using Object = UnityEngine.Object;
#if (MONOMELON || MONOBEPINEX || IL2CPPBEPINEX)
using S1Console = ScheduleOne.Console;
using S1CommandListScreen = ScheduleOne.CommandListScreen;
using TMPro;
#elif (IL2CPPMELON)
using S1Console = Il2CppScheduleOne.Console;
using S1CommandListScreen = Il2CppScheduleOne.CommandListScreen;
using Il2CppTMPro;
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

#if (MONOMELON || MONOBEPINEX)
        private static FieldInfo? _commandEntriesField;
#endif

        /// <summary>
        /// Custom commands that were added to command list screen, stored here to prevent duplicate additions.
        /// </summary>
        private static HashSet<string> _addedCommandsToList = new();

        /// <summary>
        /// Adds custom commands to command list screen.
        /// </summary>
        [HarmonyPatch(typeof(S1CommandListScreen), "Start")]
        [HarmonyPostfix]
        private static void AddCustomCommandEntries(S1CommandListScreen __instance)
        {
            try
            {
                if (__instance == null || __instance.CommandEntryPrefab == null ||
                    __instance.CommandEntryContainer == null)
                    return;

                _addedCommandsToList.Clear();
#if (MONOMELON || MONOBEPINEX)
                _commandEntriesField ??= 
                    typeof(S1CommandListScreen)
                        .GetField("commandEntries", BindingFlags.NonPublic | BindingFlags.Instance);
                var commandEntries = _commandEntriesField?.GetValue(__instance) as List<RectTransform>;
#elif (IL2CPPMELON || IL2CPPBEPINEX)
                var commandEntries = __instance?.commandEntries;
#endif

                foreach (var command in CustomConsoleRegistry.RegisteredCommands)
                {
                    var commandKey = command.Key;
                    try
                    {
                        if (_addedCommandsToList.Contains(commandKey))
                            continue;
                        if (IsNativeCommand(commandKey))
                            continue;

                        var rt = Object.Instantiate(__instance.CommandEntryPrefab, __instance.CommandEntryContainer);
                        rt.Find("Command").GetComponent<TextMeshProUGUI>().text =
                            command.Value.CommandWord;
                        rt.Find("Description").GetComponent<TextMeshProUGUI>().text =
                            command.Value.CommandDescription;
                        rt.Find("Example").GetComponent<TextMeshProUGUI>().text =
                            command.Value.ExampleUsage;

                        commandEntries?.Add(rt);
                        _addedCommandsToList.Add(commandKey);
                    }
                    catch (Exception e)
                    {
                        Logger.Warning($"[Console] Failed to add command '{commandKey}' to command list screen: {e.Message}");
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Warning($"[Console] Failed to add custom commands to command list screen: {e.Message}");
            }
        }

        private static bool IsNativeCommand(string commandKey)
        {
            if (string.IsNullOrWhiteSpace(commandKey))
                return false;

#if (MONOMELON || MONOBEPINEX)
            _monoCommandsField ??= typeof(S1Console).GetField("commands", BindingFlags.NonPublic | BindingFlags.Static);
            var dict = _monoCommandsField?.GetValue(null) as IDictionary<string, S1Console.ConsoleCommand>;
            return dict != null && dict.ContainsKey(commandKey);
#elif (IL2CPPMELON || IL2CPPBEPINEX)
            var dict = S1Console.commands;
            return dict != null && dict.ContainsKey(commandKey);
#else
            return false;
#endif
        }
    }
}

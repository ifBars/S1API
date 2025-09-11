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

#if (IL2CPPMELON)
        /// <summary>
        /// Patch to ensure Execute calls correctly in IL2CPP. This may be not needed in ML 0.7.1+
        /// </summary>
        /// <param name="__instance">The instance of the ConsoleCommand being executed.</param>
        /// <param name="args">The list of arguments passed to the command.</param>
        /// <returns>False to skip the original method execution.</returns>
        [HarmonyPatch(nameof(S1Console.ConsoleCommand), "Execute")]
        [HarmonyPrefix]
        private static bool ExecuteIl2Cpp(S1Console.ConsoleCommand __instance, Il2CppSystem.Collections.Generic.List<string> args)
        {
            // call directly, it gets confused in Il2Cpp due to virtual/abstract methods
            __instance.Execute(args);
            return false;
        }
#endif

        /// <summary>
        /// Patch to add custom console commands derived from BaseConsoleCommand to the game's console command list.
        /// </summary>
        /// <param name="__instance">The instance of the S1Console being initialized.</param>
        [HarmonyPatch(typeof(S1Console), "Awake")]
        [HarmonyPostfix]
        private static void AddCommands(S1Console __instance)
        {
            if (__instance == null)
                return;

#if (MONOMELON || MONOBEPINEX)
            var commandsField = typeof(S1Console).GetField("commands", BindingFlags.NonPublic | BindingFlags.Static);
            if (commandsField == null)
            {
                Logger.Warning("Failed to find private static field 'commands' in S1Console");
                return;
            }

            var commandsDict = (IDictionary<string, S1Console.ConsoleCommand>?)commandsField.GetValue(null);
            if (commandsDict == null)
            {
                Logger.Warning("'commands' dictionary is null");
                return;
            }
#elif (IL2CPPMELON || IL2CPPBEPINEX)
            var commandsDict = S1Console.commands;
#endif

            var commandTypes = ReflectionUtils.GetDerivedClasses<BaseConsoleCommand>();
            foreach (var type in commandTypes)
            {
                Logger.Msg($"Found console command: {type.FullName}");

                if (type.GetConstructor(Type.EmptyTypes) == null)
                    continue;

                try
                {
                    var userCommand = (BaseConsoleCommand)Activator.CreateInstance(type)!;

                    var wrapped = new ConsoleCommandWrapper(userCommand);

                    commandsDict.Add(userCommand.CommandWord, wrapped);
                    S1Console.Commands.Add(wrapped);
                }
                catch (Exception e)
                {
                    Logger.Warning($"[Console] Failed to register {type.FullName}: {e.Message}");
                }
            }
        }
    }
}
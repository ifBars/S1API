using System;
using System.Collections.Generic;

namespace S1API.Console
{
    /// <summary>
    /// Holds managed custom console commands and executes them when routed by patches.
    /// Avoids inheriting from Il2Cpp abstract types; safe for both Mono and Il2Cpp.
    /// </summary>
    internal static class CustomConsoleRegistry
    {
        private static readonly Logging.Log Logger = new Logging.Log("Console");

        internal static readonly Dictionary<string, BaseConsoleCommand> registry = new Dictionary<string, BaseConsoleCommand>(StringComparer.OrdinalIgnoreCase);

        internal static void Register(BaseConsoleCommand command)
        {
            if (command == null)
                return;

            var key = (command.CommandWord ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(key))
            {
                Logger.Warning("Skipping registration of command with empty CommandWord");
                return;
            }

            registry[key] = command; // override duplicates deterministically
        }

        /// <summary>
        /// Tries to execute a managed-routed command (Mono runtime).
        /// </summary>
        internal static bool TryExecuteManaged(System.Collections.Generic.List<string> args)
        {
            if (args == null || args.Count == 0)
                return false;

            // Game lowercases all tokens prior to lookup; mirror that when we short-circuit
            for (int i = 0; i < args.Count; i++)
            {
                if (args[i] != null)
                    args[i] = args[i].ToLowerInvariant();
            }

            string key = args[0];
            if (!registry.TryGetValue(key, out var command))
                return false;

            try
            {
                var forwarded = new System.Collections.Generic.List<string>(args);
                forwarded.RemoveAt(0);
                command.ExecuteCommand(forwarded);
                return true;
            }
            catch (Exception e)
            {
                Logger.Warning($"[Console] Error executing custom command '{key}': {e.Message}");
                return false;
            }
        }

#if (IL2CPPMELON || IL2CPPBEPINEX)
        /// <summary>
        /// Tries to execute a routed command (Il2Cpp runtime).
        /// </summary>
        internal static bool TryExecute(Il2CppSystem.Collections.Generic.List<string> args)
        {
            if (args == null || args.Count == 0)
                return false;

            string key = (args[0] ?? string.Empty).ToLower();
            if (!registry.TryGetValue(key, out var command))
                return false;

            try
            {
                var forwarded = new System.Collections.Generic.List<string>(Math.Max(args.Count - 1, 0));
                for (int i = 1; i < args.Count; i++)
                {
                    var a = args[i];
                    forwarded.Add(a);
                }
                command.ExecuteCommand(forwarded);
                return true;
            }
            catch (Exception e)
            {
                Logger.Warning($"[Console] Error executing custom command '{key}' (Il2Cpp): {e.Message}");
                return false;
            }
        }
#endif
    }
}



using System;
using System.Collections.Generic;
using S1API.Internal.Rendering;
using S1API.Logging;

namespace S1API.Internal.Console
{
    internal sealed class PresentationWorkbenchCommand :
        global::S1API.Console.BaseConsoleCommand
    {
        private static readonly Log Logger = new Log("PresentationWorkbench");

        public PresentationWorkbenchCommand()
        {
        }

        public override string CommandWord => "presentationworkbench";

        public override string CommandDescription =>
            "Open the local icon and equippable presentation authoring workbench.";

        public override string ExampleUsage =>
            "presentationworkbench [product|item] <id> | close";

        public override void ExecuteCommand(List<string> args)
        {
            if (args == null || args.Count == 0)
            {
                Logger.Msg($"Usage: {ExampleUsage}");
                return;
            }

            string action = args[0]?.Trim() ?? string.Empty;
            if (action.Equals("close", StringComparison.OrdinalIgnoreCase))
            {
                PresentationWorkbenchRuntime.Close();
                Logger.Msg("Presentation workbench closed.");
                return;
            }

            string? targetKind = null;
            string targetId = action;
            if (PresentationWorkbenchResolver.IsTargetKind(action))
            {
                if (args.Count != 2 || string.IsNullOrWhiteSpace(args[1]))
                {
                    Logger.Msg($"Usage: {ExampleUsage}");
                    return;
                }

                targetKind = action;
                targetId = args[1].Trim();
            }
            else if (args.Count != 1)
            {
                Logger.Msg($"Usage: {ExampleUsage}");
                return;
            }

            if (!PresentationWorkbenchRuntime.Open(targetId, targetKind))
            {
                Logger.Warning(
                    $"Could not open presentation workbench target '{targetId}'.");
            }
        }
    }
}

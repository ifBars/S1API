using System;
using S1API.Logging;

namespace S1API.Internal.Entities
{
    /// <summary>
    /// INTERNAL: Runs the handlers of the NPC attack and knockout events, each in its own try/catch.
    /// </summary>
    internal static class NpcAttackEvents
    {
        private static readonly Log Logger = new Log("NPCAttackEvents");

        internal static Action<string, string>? LogSinkForTesting;

        internal static void Invoke(Delegate? handlers, string eventName, Action<Delegate> call)
        {
            if (handlers == null)
                return;
            foreach (var handler in handlers.GetInvocationList())
            {
                try
                {
                    call(handler);
                }
                catch (Exception ex)
                {
                    Error($"A handler of {eventName} threw: {ex}");
                }
            }
        }

        internal static void Info(string message)
        {
            if (LogSinkForTesting != null) LogSinkForTesting("info", message);
            else Logger.Msg(message);
        }

        internal static void Warn(string message)
        {
            if (LogSinkForTesting != null) LogSinkForTesting("warn", message);
            else Logger.Warning(message);
        }

        internal static void Error(string message)
        {
            if (LogSinkForTesting != null) LogSinkForTesting("error", message);
            else Logger.Error(message);
        }
    }
}

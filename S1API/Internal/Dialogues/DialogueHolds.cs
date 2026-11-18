using System;
using System.Collections.Generic;
using S1API.Entities;
using S1API.Lifecycle;
using S1API.Logging;

namespace S1API.Internal.Dialogues
{
    /// <summary>
    /// INTERNAL: Continue holds per NPC dialogue and the continue press requested from code.
    /// </summary>
    internal static class DialogueHolds
    {
        private static readonly Log Logger = new Log("DialogueFlow");

        internal sealed class Hold : IDisposable
        {
            internal readonly object Owner;
            internal readonly string Name;
            internal readonly Func<bool> Condition;
            internal bool Faulted;

            internal Hold(object owner, string name, Func<bool> condition)
            {
                Owner = owner;
                Name = name;
                Condition = condition;
            }

            public void Dispose()
            {
                if (Holds.Remove(this))
                    Info($"Continue hold on {Name} removed.");
            }
        }

        private static readonly List<Hold> Holds = new List<Hold>();
        private static bool _lifecycleHooked;

        /// <summary>A continue press requested by <see cref="S1API.Dialogues.DialogueFlow.TryContinue"/>, applied on the next canvas update.</summary>
        internal static bool PendingContinue;

        internal static Action<string, string>? LogSinkForTesting;

        internal static int Count => Holds.Count;

        internal static IDisposable Add(object owner, string name, Func<bool> condition)
        {
            if (owner == null)
                throw new ArgumentNullException(nameof(owner));
            if (condition == null)
                throw new ArgumentNullException(nameof(condition));
            EnsureLifecycleHook();
            var hold = new Hold(owner, name, condition);
            Holds.Add(hold);
            Info($"Continue hold on {name} added.");
            return hold;
        }

        /// <summary>
        /// True when a hold of <paramref name="owner"/> is active. A hold whose condition throws is logged once and
        /// treated as released, so a broken mod cannot lock a conversation.
        /// </summary>
        internal static bool IsHeld(Func<object, bool> matches)
        {
            for (int i = 0; i < Holds.Count; i++)
            {
                var hold = Holds[i];
                bool owned;
                try { owned = matches(hold.Owner); }
                catch (Exception ex) { Error($"Matching the continue hold on {hold.Name} failed: {ex}"); continue; }
                if (!owned || hold.Faulted)
                    continue;
                try
                {
                    if (!hold.Condition())
                        continue;
                }
                catch (Exception ex)
                {
                    hold.Faulted = true;
                    Error($"Continue hold on {hold.Name} threw and is treated as released: {ex}");
                    continue;
                }
                Info($"Continue on {hold.Name} is held; the dialogue stays open.");
                return true;
            }
            return false;
        }

        /// <summary>Takes the requested press, if any, so it is applied once.</summary>
        internal static bool TakePendingContinue()
        {
            if (!PendingContinue)
                return false;
            PendingContinue = false;
            Info("Continue pressed from code.");
            return true;
        }

        internal static void ResetForTesting()
        {
            Holds.Clear();
            PendingContinue = false;
            LogSinkForTesting = null;
        }

        private static void EnsureLifecycleHook()
        {
            if (_lifecycleHooked)
                return;
            _lifecycleHooked = true;
            GameLifecycle.OnPreSceneChange += () =>
            {
                if (Holds.Count > 0)
                    Info($"Continue holds kept across the scene change: {Holds.Count}.");
                PendingContinue = false;
            };
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

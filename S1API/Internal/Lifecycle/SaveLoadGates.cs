using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using MelonLoader;
using S1API.Logging;
using UnityEngine;

namespace S1API.Internal.Lifecycle
{
    /// <summary>
    /// Save vetoes and load gates registered through <see cref="S1API.Lifecycle.GameLifecycle"/>.
    /// </summary>
    /// <remarks>
    /// A veto is asked before every save; the first one that returns a reason blocks it. A load gate defers
    /// <c>LoadManager.StartGame</c> and <c>LoadManager.LoadAsClient</c> until it opens or its timeout passes; the
    /// deferred call is then made again with the original arguments.
    /// </remarks>
    internal static class SaveLoadGates
    {
        private static readonly Log Logger = new Log("SaveLoadGates");
        private static readonly List<VetoEntry> Vetoes = new List<VetoEntry>();
        private static readonly List<GateEntry> Gates = new List<GateEntry>();
        private static readonly HashSet<GateEntry> TimedOut = new HashSet<GateEntry>();
        private static readonly HashSet<object> Faulted = new HashSet<object>();

        private static PendingLoad? _pending;
        private static bool _resuming;
        private static bool _waiting;

        /// <summary>Raised with owner and reason when a veto blocks a save.</summary>
        internal static event Action<string, string>? SaveBlocked;

        /// <summary>Test hook that receives every log line in place of MelonLoader.</summary>
        internal static Action<string, string>? LogSinkForTesting;

        /// <summary>Test hooks for the clock and for starting the wait routine without Unity.</summary>
        internal static Func<float>? ClockForTesting;
        internal static Action<IEnumerator>? StartWaitForTesting;

        private static string Num(float value, string format) => value.ToString(format, CultureInfo.InvariantCulture);

        private static float Now() => ClockForTesting != null ? ClockForTesting() : UnityNow();

        // Kept out of Now() so the unit tests never JIT the Unity engine call.
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static float UnityNow() => Time.realtimeSinceStartup;

        /// <summary>True while a deferred load waits for its gates.</summary>
        internal static bool HasPendingLoad => _pending != null;

        internal static IDisposable AddSaveVeto(string owner, Func<string?> reason)
        {
            if (string.IsNullOrWhiteSpace(owner))
                throw new ArgumentException("A save veto needs an owner name.", nameof(owner));
            if (reason == null)
                throw new ArgumentNullException(nameof(reason));

            var entry = new VetoEntry(owner, reason);
            Vetoes.Add(entry);
            Info($"Save veto registered by {owner}.");
            return new Registration(() =>
            {
                if (!Vetoes.Remove(entry))
                    return;
                Faulted.Remove(entry);
                Info($"Save veto from {owner} removed.");
            });
        }

        internal static IDisposable AddLoadGate(string owner, Func<bool> ready, float timeoutSeconds)
        {
            if (string.IsNullOrWhiteSpace(owner))
                throw new ArgumentException("A load gate needs an owner name.", nameof(owner));
            if (ready == null)
                throw new ArgumentNullException(nameof(ready));
            if (float.IsNaN(timeoutSeconds) || timeoutSeconds <= 0f)
                throw new ArgumentOutOfRangeException(nameof(timeoutSeconds), "The timeout must be greater than zero.");

            var entry = new GateEntry(owner, ready, timeoutSeconds);
            Gates.Add(entry);
            Info($"Load gate registered by {owner} (timeout {Num(timeoutSeconds, "0.#")} s).");
            return new Registration(() =>
            {
                if (!Gates.Remove(entry))
                    return;
                TimedOut.Remove(entry);
                Faulted.Remove(entry);
                Info($"Load gate from {owner} removed.");
            });
        }

        /// <summary>
        /// Asks every veto. Returns false, logs a warning and raises <see cref="SaveBlocked"/> when one returns a
        /// reason. A veto that throws is logged and skipped.
        /// </summary>
        internal static bool AllowSave(string what)
        {
            for (int i = 0; i < Vetoes.Count; i++)
            {
                var veto = Vetoes[i];
                string? reason;
                try
                {
                    reason = veto.Reason();
                }
                catch (Exception ex)
                {
                    if (Faulted.Add(veto))
                        Error($"Save veto from {veto.Owner} threw and is skipped: {ex}");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(reason))
                    continue;

                Warning($"{what} blocked by {veto.Owner}: {reason}");
                RaiseSaveBlocked(veto.Owner, reason!);
                return false;
            }
            return true;
        }

        /// <summary>
        /// Defers a load while a gate is closed. Returns true when the call was taken over; <paramref name="resume"/>
        /// makes the same call again once every gate is open or timed out.
        /// </summary>
        internal static bool TryDefer(string what, Action resume)
        {
            if (resume == null)
                throw new ArgumentNullException(nameof(resume));
            if (_resuming)
                return false;

            TimedOut.Clear();
            var closed = ClosedGates(0f);
            if (closed.Count == 0)
                return false;

            if (_pending != null)
                Warning($"{what} replaces the deferred {_pending.What}.");

            _pending = new PendingLoad(what, resume, Now());
            Info($"{what} deferred until the load gate of {string.Join(", ", closed)} opens.");
            if (!_waiting)
            {
                _waiting = true;
                if (StartWaitForTesting != null) StartWaitForTesting(WaitForGates());
                else MelonCoroutines.Start(WaitForGates());
            }
            return true;
        }

        /// <summary>
        /// Checks the gates of the deferred load once and starts it when none is closed. Called every frame while a
        /// load waits; exposed for tests.
        /// </summary>
        internal static bool Poll(float now)
        {
            var pending = _pending;
            if (pending == null)
                return true;

            var closed = ClosedGates(now - pending.Since);
            if (closed.Count > 0)
                return false;

            _pending = null;
            float waited = now - pending.Since;
            string why = TimedOut.Count > 0 ? $"waited {Num(waited, "0.0")} s, {TimedOut.Count} gate(s) timed out" : $"gates open after {Num(waited, "0.0")} s";
            _resuming = true;
            try
            {
                Info($"Deferred {pending.What} starts now ({why}).");
                pending.Resume();
            }
            catch (Exception ex)
            {
                Error($"Deferred {pending.What} failed: {ex}");
            }
            finally
            {
                _resuming = false;
                TimedOut.Clear();
            }
            return true;
        }

        private static IEnumerator WaitForGates()
        {
            try
            {
                while (_pending != null)
                {
                    bool done;
                    try
                    {
                        done = Poll(Now());
                    }
                    catch (Exception ex)
                    {
                        Error($"Load gate check failed, the deferred load starts now: {ex}");
                        ForceResume();
                        done = true;
                    }
                    if (done && _pending == null)
                        break;
                    yield return null;
                }
            }
            finally
            {
                _waiting = false;
            }
        }

        private static void ForceResume()
        {
            var pending = _pending;
            _pending = null;
            if (pending == null)
                return;
            _resuming = true;
            try { pending.Resume(); }
            catch (Exception ex) { Error($"Deferred {pending.What} failed: {ex}"); }
            finally { _resuming = false; }
        }

        /// <summary>Owners of the gates that are still closed after <paramref name="waited"/> seconds.</summary>
        private static List<string> ClosedGates(float waited)
        {
            var closed = new List<string>();
            for (int i = 0; i < Gates.Count; i++)
            {
                var gate = Gates[i];
                if (TimedOut.Contains(gate))
                    continue;

                bool ready;
                try
                {
                    ready = gate.Ready();
                }
                catch (Exception ex)
                {
                    if (Faulted.Add(gate))
                        Error($"Load gate from {gate.Owner} threw and is treated as open: {ex}");
                    continue;
                }

                if (ready)
                    continue;

                if (waited >= gate.TimeoutSeconds)
                {
                    TimedOut.Add(gate);
                    Error($"Load gate from {gate.Owner} did not open within {Num(gate.TimeoutSeconds, "0.#")} s. The load continues without it.");
                    continue;
                }
                closed.Add(gate.Owner);
            }
            return closed;
        }

        private static void RaiseSaveBlocked(string owner, string reason)
        {
            var handlers = SaveBlocked;
            if (handlers == null)
                return;
            foreach (var handler in handlers.GetInvocationList())
            {
                try { ((Action<string, string>)handler)(owner, reason); }
                catch (Exception ex) { Error($"OnSaveBlocked handler {handler.Method.DeclaringType?.FullName}.{handler.Method.Name} threw: {ex}"); }
            }
        }

        internal static void ResetForTesting()
        {
            Vetoes.Clear();
            Gates.Clear();
            TimedOut.Clear();
            Faulted.Clear();
            _pending = null;
            _resuming = false;
            _waiting = false;
            SaveBlocked = null;
            LogSinkForTesting = null;
            ClockForTesting = null;
            StartWaitForTesting = null;
        }

        private static void Info(string message)
        {
            if (LogSinkForTesting != null) { LogSinkForTesting("info", message); return; }
            Logger.Msg(message);
        }

        private static void Warning(string message)
        {
            if (LogSinkForTesting != null) { LogSinkForTesting("warn", message); return; }
            Logger.Warning(message);
        }

        private static void Error(string message)
        {
            if (LogSinkForTesting != null) { LogSinkForTesting("error", message); return; }
            Logger.Error(message);
        }

        private sealed class VetoEntry
        {
            internal VetoEntry(string owner, Func<string?> reason) { Owner = owner; Reason = reason; }
            internal string Owner { get; }
            internal Func<string?> Reason { get; }
        }

        private sealed class GateEntry
        {
            internal GateEntry(string owner, Func<bool> ready, float timeoutSeconds) { Owner = owner; Ready = ready; TimeoutSeconds = timeoutSeconds; }
            internal string Owner { get; }
            internal Func<bool> Ready { get; }
            internal float TimeoutSeconds { get; }
        }

        private sealed class PendingLoad
        {
            internal PendingLoad(string what, Action resume, float since) { What = what; Resume = resume; Since = since; }
            internal string What { get; }
            internal Action Resume { get; }
            internal float Since { get; }
        }

        private sealed class Registration : IDisposable
        {
            private Action? _dispose;
            internal Registration(Action dispose) { _dispose = dispose; }
            public void Dispose()
            {
                var dispose = _dispose;
                _dispose = null;
                dispose?.Invoke();
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using S1API.Logging;

namespace S1API.Dialogues
{
    /// <summary>
    /// Adds, hides and disables dialogue choices on vanilla NPC menus, evaluated each time a menu is built.
    /// </summary>
    /// <remarks>
    /// Several mods can add to the same menu: S1API applies every registration in one place, so no mod
    /// overwrites another one's entries. Each callback runs in its own try/catch; a failure is logged with the
    /// name of the mod that registered it and does not stop the other registrations.
    /// Registrations survive save loads. Register once, for example in <c>OnInitializeMelon</c>.
    /// </remarks>
    public static class DialogueChoices
    {
        private static readonly Log Logger = new Log("DialogueChoices");
        private static readonly List<ChoiceRegistration> _choices = new List<ChoiceRegistration>();
        private static readonly List<HideRegistration> _hides = new List<HideRegistration>();

        /// <summary>
        /// Adds a choice to a node of an NPC's menu.
        /// </summary>
        /// <param name="npcId">ID of the NPC, for example <c>ray_hoffman</c>.</param>
        /// <param name="nodeLabel">Label of the dialogue node whose choices get the entry, for example <c>ENTRY</c>.</param>
        /// <param name="spec">The choice. Its properties are read each time the menu is built.</param>
        /// <returns>Dispose it to remove the choice again.</returns>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static IDisposable Add(string npcId, string nodeLabel, DialogueChoiceSpec spec)
        {
            if (string.IsNullOrEmpty(npcId))
                throw new ArgumentException("An NPC ID is required.", nameof(npcId));
            if (string.IsNullOrEmpty(nodeLabel))
                throw new ArgumentException("A node label is required.", nameof(nodeLabel));
            if (spec == null)
                throw new ArgumentNullException(nameof(spec));
            if (string.IsNullOrEmpty(spec.Label))
                throw new ArgumentException("The choice needs a label.", nameof(spec));

            var registration = new ChoiceRegistration(npcId, nodeLabel, spec, OwnerOf(Assembly.GetCallingAssembly()));
            _choices.Add(registration);
            Info($"{registration.Owner} added choice '{spec.Label}' to {npcId}/{nodeLabel}.");
            return new Registration(() =>
            {
                if (_choices.Remove(registration))
                    Info($"{registration.Owner} removed choice '{spec.Label}' from {npcId}/{nodeLabel}.");
            });
        }

        /// <summary>
        /// Hides a choice of an NPC's menu, vanilla or added, while <paramref name="hidden"/> returns true.
        /// </summary>
        /// <param name="npcId">ID of the NPC.</param>
        /// <param name="choiceLabel">Label of the choice to hide.</param>
        /// <param name="hidden">Evaluated each time the menu is built.</param>
        /// <returns>Dispose it to stop hiding the choice.</returns>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static IDisposable Hide(string npcId, string choiceLabel, Func<bool> hidden)
        {
            if (string.IsNullOrEmpty(npcId))
                throw new ArgumentException("An NPC ID is required.", nameof(npcId));
            if (string.IsNullOrEmpty(choiceLabel))
                throw new ArgumentException("A choice label is required.", nameof(choiceLabel));
            if (hidden == null)
                throw new ArgumentNullException(nameof(hidden));

            var registration = new HideRegistration(npcId, choiceLabel, hidden, OwnerOf(Assembly.GetCallingAssembly()));
            _hides.Add(registration);
            Info($"{registration.Owner} hides choice '{choiceLabel}' of {npcId} while its condition holds.");
            return new Registration(() =>
            {
                if (_hides.Remove(registration))
                    Info($"{registration.Owner} stopped hiding choice '{choiceLabel}' of {npcId}.");
            });
        }

        /// <summary>
        /// INTERNAL: Returns the specs to add to this menu, in registration order, after their <c>IsShown</c> check.
        /// </summary>
        internal static List<DialogueChoiceSpec> ShownChoices(string npcId, string? conversationName, string nodeLabel)
        {
            var shown = new List<DialogueChoiceSpec>();
            foreach (ChoiceRegistration registration in _choices.ToArray())
            {
                if (!registration.Matches(npcId, conversationName, nodeLabel))
                    continue;

                Func<bool>? isShown = registration.Spec.IsShown;
                if (isShown == null)
                {
                    shown.Add(registration.Spec);
                    continue;
                }

                try
                {
                    if (isShown())
                        shown.Add(registration.Spec);
                }
                catch (Exception ex)
                {
                    Fail($"{registration.Owner}: IsShown of choice '{registration.Spec.Label}' threw; the choice stays hidden. {ex}");
                }
            }

            return shown;
        }

        /// <summary>
        /// INTERNAL: True when a <see cref="Hide"/> registration hides this choice right now.
        /// </summary>
        internal static bool IsHidden(string npcId, string choiceLabel)
        {
            foreach (HideRegistration registration in _hides.ToArray())
            {
                if (registration.NpcId != npcId || registration.ChoiceLabel != choiceLabel)
                    continue;

                try
                {
                    if (registration.Hidden())
                        return true;
                }
                catch (Exception ex)
                {
                    Fail($"{registration.Owner}: hide condition of choice '{choiceLabel}' threw; the choice stays visible. {ex}");
                }
            }

            return false;
        }

        /// <summary>
        /// INTERNAL: Finds the index to insert <paramref name="spec"/> at, given the labels of the current menu.
        /// </summary>
        internal static int InsertIndex(IReadOnlyList<string?> labels, DialogueChoiceSpec spec)
        {
            if (!string.IsNullOrEmpty(spec.InsertAfterLabel))
            {
                for (int i = 0; i < labels.Count; i++)
                {
                    if (labels[i] == spec.InsertAfterLabel)
                        return i + 1;
                }
            }

            if (spec.InsertBeforeLastChoice && labels.Count > 0)
                return labels.Count - 1;

            return labels.Count;
        }

        /// <summary>
        /// INTERNAL: Finds the added choice behind a label in this menu.
        /// </summary>
        internal static DialogueChoiceSpec? Find(string npcId, string? conversationName, string nodeLabel, string choiceLabel)
        {
            foreach (ChoiceRegistration registration in _choices.ToArray())
            {
                if (registration.Spec.Label == choiceLabel && registration.Matches(npcId, conversationName, nodeLabel))
                    return registration.Spec;
            }

            return null;
        }

        /// <summary>
        /// INTERNAL: Evaluates <c>IsEnabled</c> of an added choice. Returns false with the reason when it is disabled.
        /// </summary>
        internal static bool CheckEnabled(DialogueChoiceSpec spec, out string reason)
        {
            reason = string.Empty;
            Func<bool>? isEnabled = spec.IsEnabled;
            if (isEnabled == null)
                return true;

            try
            {
                if (isEnabled())
                    return true;
            }
            catch (Exception ex)
            {
                Fail($"{OwnerOfSpec(spec)}: IsEnabled of choice '{spec.Label}' threw; the choice stays enabled. {ex}");
                return true;
            }

            reason = spec.DisabledReason ?? string.Empty;
            return false;
        }

        /// <summary>
        /// INTERNAL: Runs <c>OnChosen</c> of an added choice.
        /// </summary>
        internal static void RunChosen(DialogueChoiceSpec spec)
        {
            Action? onChosen = spec.OnChosen;
            if (onChosen == null)
                return;

            try
            {
                onChosen();
            }
            catch (Exception ex)
            {
                Fail($"{OwnerOfSpec(spec)}: OnChosen of choice '{spec.Label}' threw. {ex}");
            }
        }

        /// <summary>
        /// INTERNAL: Removes every registration. Only for tests.
        /// </summary>
        internal static void ResetForTesting()
        {
            _choices.Clear();
            _hides.Clear();
        }

        /// <summary>
        /// INTERNAL: Receives log lines instead of MelonLoader while set. Only for tests, which run without MelonLoader.
        /// </summary>
        internal static Action<string>? LogSinkForTesting { get; set; }

        private static void Info(string message)
        {
            if (LogSinkForTesting != null)
                LogSinkForTesting("info: " + message);
            else
                Logger.Msg(message);
        }

        private static void Fail(string message)
        {
            if (LogSinkForTesting != null)
                LogSinkForTesting("error: " + message);
            else
                Logger.Error(message);
        }

        private static string OwnerOfSpec(DialogueChoiceSpec spec)
        {
            foreach (ChoiceRegistration registration in _choices.ToArray())
            {
                if (ReferenceEquals(registration.Spec, spec))
                    return registration.Owner;
            }

            return "unknown mod";
        }

        private static string OwnerOf(Assembly assembly) =>
            assembly.GetName().Name ?? "unknown mod";

        private sealed class ChoiceRegistration
        {
            internal ChoiceRegistration(string npcId, string nodeLabel, DialogueChoiceSpec spec, string owner)
            {
                NpcId = npcId;
                NodeLabel = nodeLabel;
                Spec = spec;
                Owner = owner;
            }

            internal string NpcId { get; }
            internal string NodeLabel { get; }
            internal DialogueChoiceSpec Spec { get; }
            internal string Owner { get; }

            internal bool Matches(string npcId, string? conversationName, string nodeLabel) =>
                NpcId == npcId &&
                NodeLabel == nodeLabel &&
                (Spec.ConversationName == null || Spec.ConversationName == conversationName);
        }

        private sealed class HideRegistration
        {
            internal HideRegistration(string npcId, string choiceLabel, Func<bool> hidden, string owner)
            {
                NpcId = npcId;
                ChoiceLabel = choiceLabel;
                Hidden = hidden;
                Owner = owner;
            }

            internal string NpcId { get; }
            internal string ChoiceLabel { get; }
            internal Func<bool> Hidden { get; }
            internal string Owner { get; }
        }

        private sealed class Registration : IDisposable
        {
            private Action? _remove;

            internal Registration(Action remove) =>
                _remove = remove;

            public void Dispose()
            {
                _remove?.Invoke();
                _remove = null;
            }
        }
    }
}

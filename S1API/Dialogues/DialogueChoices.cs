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
    /// Each row S1API inserts is bound to its registration until the menu is built again or the registration is
    /// disposed, so a vanilla row is never touched, even when an added choice uses the same label.
    /// Registrations survive save loads. Register once, for example in <c>OnInitializeMelon</c>.
    /// </remarks>
    public static class DialogueChoices
    {
        private static readonly Log Logger = new Log("DialogueChoices");
        private static readonly List<ChoiceRegistration> _choices = new List<ChoiceRegistration>();
        private static readonly List<HideRegistration> _hides = new List<HideRegistration>();
        private static readonly List<RowBinding> _bindings = new List<RowBinding>();

        /// <summary>
        /// Adds a choice to a node of an NPC's menu.
        /// </summary>
        /// <param name="npcId">ID of the NPC, for example <c>ray_hoffman</c>.</param>
        /// <param name="nodeLabel">Label of the dialogue node whose choices get the entry, for example <c>ENTRY</c>.</param>
        /// <param name="spec">The choice. Label, text, position and <c>IsShown</c> are read each time the menu is built; <c>IsEnabled</c> and <c>OnChosen</c> when the player hovers or picks the row.</param>
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
            if (string.IsNullOrWhiteSpace(spec.Label))
                throw new ArgumentException("The choice needs a label that is not blank.", nameof(spec));

            var registration = new ChoiceRegistration(npcId, nodeLabel, spec, OwnerOf(Assembly.GetCallingAssembly()));
            _choices.Add(registration);
            Info($"{registration.Owner} added choice '{spec.Label}' to {npcId}/{nodeLabel}.");
            return new Registration(() =>
            {
                if (_choices.Remove(registration))
                {
                    _bindings.RemoveAll(b => ReferenceEquals(b.Spec, registration.Spec));
                    Info($"{registration.Owner} removed choice '{spec.Label}' from {npcId}/{nodeLabel}.");
                }
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

                // Label is mutable, so a blank one is caught again here; the row would have no usable identity.
                if (string.IsNullOrWhiteSpace(registration.Spec.Label))
                {
                    if (!registration.BlankLabelReported)
                    {
                        registration.BlankLabelReported = true;
                        Warn($"{registration.Owner}: a choice for {registration.NpcId}/{registration.NodeLabel} has a blank label; it is skipped.");
                    }

                    continue;
                }

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
        /// INTERNAL: Forgets the rows bound for a handler. Called before its menu is built again.
        /// </summary>
        internal static void ClearBindings(object handlerKey) =>
            _bindings.RemoveAll(b => Equals(b.HandlerKey, handlerKey));

        /// <summary>
        /// INTERNAL: Binds an inserted row of a handler's menu to the registration behind <paramref name="spec"/>.
        /// The label is kept as it was when the row was inserted.
        /// </summary>
        internal static void Bind(object handlerKey, string rowGuid, string label, DialogueChoiceSpec spec) =>
            _bindings.Add(new RowBinding(handlerKey, rowGuid, label, spec));

        /// <summary>
        /// INTERNAL: Finds the registration bound to one row. Null for vanilla rows.
        /// </summary>
        internal static DialogueChoiceSpec? FindBoundByRow(object handlerKey, string rowGuid)
        {
            foreach (RowBinding binding in _bindings.ToArray())
            {
                if (Equals(binding.HandlerKey, handlerKey) && binding.RowGuid == rowGuid)
                    return binding.Spec;
            }

            return null;
        }

        /// <summary>
        /// INTERNAL: Finds the registrations bound to rows with this label, in menu order. Empty for vanilla rows.
        /// </summary>
        internal static List<DialogueChoiceSpec> FindBoundByLabel(object handlerKey, string label)
        {
            var found = new List<DialogueChoiceSpec>();
            foreach (RowBinding binding in _bindings.ToArray())
            {
                if (Equals(binding.HandlerKey, handlerKey) && binding.Label == label)
                    found.Add(binding.Spec);
            }

            return found;
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

            // Captured first: the callback may dispose its own registration, after which the owner is lost.
            string owner = OwnerOfSpec(spec);

            try
            {
                onChosen();
            }
            catch (Exception ex)
            {
                Fail($"{owner}: OnChosen of choice '{spec.Label}' threw. {ex}");
            }
        }

        /// <summary>
        /// INTERNAL: Removes every registration. Only for tests.
        /// </summary>
        internal static void ResetForTesting()
        {
            _choices.Clear();
            _hides.Clear();
            _bindings.Clear();
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

        private static void Warn(string message)
        {
            if (LogSinkForTesting != null)
                LogSinkForTesting("warn: " + message);
            else
                Logger.Warning(message);
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
            internal bool BlankLabelReported { get; set; }

            internal bool Matches(string npcId, string? conversationName, string nodeLabel) =>
                NpcId == npcId &&
                NodeLabel == nodeLabel &&
                (Spec.ConversationName == null || Spec.ConversationName == conversationName);
        }

        private sealed class RowBinding
        {
            internal RowBinding(object handlerKey, string rowGuid, string label, DialogueChoiceSpec spec)
            {
                HandlerKey = handlerKey;
                RowGuid = rowGuid;
                Label = label;
                Spec = spec;
            }

            internal object HandlerKey { get; }
            internal string RowGuid { get; }
            internal string Label { get; }
            internal DialogueChoiceSpec Spec { get; }
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

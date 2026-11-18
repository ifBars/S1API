#if (IL2CPPMELON)
using S1Dialogue = Il2CppScheduleOne.Dialogue;
using ChoiceList = Il2CppSystem.Collections.Generic.List<Il2CppScheduleOne.Dialogue.DialogueChoiceData>;
#elif MONOMELON
using S1Dialogue = ScheduleOne.Dialogue;
using ChoiceList = System.Collections.Generic.List<ScheduleOne.Dialogue.DialogueChoiceData>;
#endif

using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using S1API.Dialogues;
using S1API.Logging;

namespace S1API.Internal.Patches
{
    /// <summary>
    /// INTERNAL: Applies <see cref="DialogueChoices"/> registrations to vanilla NPC menus.
    /// </summary>
    /// <remarks>
    /// Every NPC menu S1API extends runs through a <c>ControlledDialogueHandler</c>, the only handler that overrides
    /// <c>ModifyChoiceList</c>. The small base bodies of <c>DialogueHandler</c> are not patched: IL2CPP builds can
    /// inline them into the overrides, so a detour on the base never runs, and trivial ones such as
    /// <c>ShouldChoiceBeShown</c> can be folded into one native function shared with unrelated methods. Hidden
    /// choices are therefore removed from the built list, and <c>CheckChoice</c> and <c>ChoiceCallback</c> are
    /// patched on each override, which the game reaches through virtual dispatch.
    /// </remarks>
    [HarmonyPatch]
    internal static class DialogueChoicePatches
    {
        private static readonly Log Logger = new Log("DialogueChoicePatches");

        /// <summary>
        /// Adds and hides choices after the controller and the handler built the list of a node.
        /// </summary>
        [HarmonyPatch(typeof(S1Dialogue.ControlledDialogueHandler), "ModifyChoiceList")]
        [HarmonyPostfix]
        private static void ModifyChoiceList_Postfix(
            S1Dialogue.ControlledDialogueHandler __instance,
            string dialogueLabel,
            ref ChoiceList existingChoices)
        {
            if (existingChoices == null)
                return;

            string? npcId = NpcIdOf(__instance);
            if (npcId == null)
                return;

            try
            {
                string? conversationName = S1Dialogue.DialogueHandler.ActiveDialogue?.name;
                AddShown(npcId, conversationName, dialogueLabel, existingChoices);
                RemoveHidden(npcId, existingChoices);
            }
            catch (Exception ex)
            {
                Logger.Error($"Choice list of {npcId}/{dialogueLabel} could not be changed: {ex}");
            }
        }

        /// <summary>
        /// INTERNAL: <c>ControlledDialogueHandler</c> and each subclass that declares <paramref name="name"/>.
        /// </summary>
        internal static IEnumerable<MethodBase> ControlledOverrides(string name)
        {
            Type root = typeof(S1Dialogue.ControlledDialogueHandler);
            var found = new List<MethodBase>();
            foreach (Type type in AccessTools.GetTypesFromAssembly(root.Assembly))
            {
                try
                {
                    if (type != root && !type.IsSubclassOf(root))
                        continue;

                    MethodInfo? method = AccessTools.DeclaredMethod(type, name);
                    if (method != null && !method.IsAbstract)
                        found.Add(method);
                }
                catch (Exception ex)
                {
                    Logger.Warning($"Type {type.FullName} skipped while looking for {name} overrides: {ex.Message}");
                }
            }

            if (found.Count == 0)
                Logger.Error($"No {name} override found on {root.Name}; added dialogue choices will not react to it.");
            else
                Logger.Msg($"Patching {name} on {string.Join(", ", found.ConvertAll(m => m.DeclaringType?.Name ?? "?"))}.");

            return found;
        }

        internal static DialogueChoiceSpec? FindActive(S1Dialogue.DialogueHandler handler, string choiceLabel)
        {
            if (string.IsNullOrEmpty(choiceLabel))
                return null;

            string? npcId = NpcIdOf(handler);
            S1Dialogue.DialogueNodeData? node = S1Dialogue.DialogueHandler.ActiveDialogueNode;
            if (npcId == null || node == null)
                return null;

            return DialogueChoices.Find(npcId, S1Dialogue.DialogueHandler.ActiveDialogue?.name, node.DialogueNodeLabel, choiceLabel);
        }

        private static void RemoveHidden(string npcId, ChoiceList choices)
        {
            for (int i = choices.Count - 1; i >= 0; i--)
            {
                string? label = choices[i]?.ChoiceLabel;
                if (!string.IsNullOrEmpty(label) && DialogueChoices.IsHidden(npcId, label!))
                    choices.RemoveAt(i);
            }
        }

        private static void AddShown(string npcId, string? conversationName, string nodeLabel, ChoiceList choices)
        {
            List<DialogueChoiceSpec> shown = DialogueChoices.ShownChoices(npcId, conversationName, nodeLabel);
            foreach (DialogueChoiceSpec spec in shown)
            {
                var labels = new List<string?>(choices.Count);
                bool present = false;
                for (int i = 0; i < choices.Count; i++)
                {
                    string? label = choices[i]?.ChoiceLabel;
                    labels.Add(label);
                    present |= label == spec.Label;
                }

                if (present)
                {
                    Logger.Warning($"Choice '{spec.Label}' is already in {npcId}/{nodeLabel}; not added twice.");
                    continue;
                }

                var choice = new S1Dialogue.DialogueChoiceData
                {
                    Guid = Guid.NewGuid().ToString(),
                    ChoiceLabel = spec.Label,
                    ChoiceText = spec.Text
                };
                choices.Insert(DialogueChoices.InsertIndex(labels, spec), choice);
            }
        }

        private static string? NpcIdOf(S1Dialogue.DialogueHandler handler)
        {
            if (handler == null || handler.NPC == null)
                return null;

            return handler.NPC.ID;
        }
    }

    /// <summary>
    /// INTERNAL: Disables added choices whose <c>IsEnabled</c> returns false.
    /// </summary>
    [HarmonyPatch]
    internal static class DialogueChoiceCheckPatches
    {
        private static IEnumerable<MethodBase> TargetMethods() =>
            DialogueChoicePatches.ControlledOverrides("CheckChoice");

        // Runs once per override in the call chain; a choice already refused stays refused.
        [HarmonyPostfix]
        private static void Postfix(
            S1Dialogue.DialogueHandler __instance,
            string choiceLabel,
            ref string invalidReason,
            ref bool __result)
        {
            if (!__result)
                return;

            DialogueChoiceSpec? spec = DialogueChoicePatches.FindActive(__instance, choiceLabel);
            if (spec == null)
                return;

            if (!DialogueChoices.CheckEnabled(spec, out string reason))
            {
                __result = false;
                invalidReason = reason;
            }
        }
    }

    /// <summary>
    /// INTERNAL: Runs <c>OnChosen</c> of an added choice and moves to its target node.
    /// </summary>
    [HarmonyPatch]
    internal static class DialogueChoiceCallbackPatches
    {
        private static readonly Log Logger = new Log("DialogueChoicePatches");
        private static object? _lastHandler;
        private static string? _lastLabel;
        private static int _lastFrame = -1;

        private static IEnumerable<MethodBase> TargetMethods() =>
            DialogueChoicePatches.ControlledOverrides("ChoiceCallback");

        // Where an override calls base without inlining it, this runs once per level; only the first one acts.
        [HarmonyPostfix]
        private static void Postfix(S1Dialogue.DialogueHandler __instance, string choiceLabel)
        {
            DialogueChoiceSpec? spec = DialogueChoicePatches.FindActive(__instance, choiceLabel);
            if (spec == null)
                return;

            object handlerKey = KeyOf(__instance);
            int frame = UnityEngine.Time.frameCount;
            if (frame == _lastFrame && choiceLabel == _lastLabel && Equals(handlerKey, _lastHandler))
                return;

            _lastHandler = handlerKey;
            _lastLabel = choiceLabel;
            _lastFrame = frame;

            S1Dialogue.DialogueNodeData? before = S1Dialogue.DialogueHandler.ActiveDialogueNode;
            DialogueChoices.RunChosen(spec);

            if (string.IsNullOrEmpty(spec.TargetNodeLabel))
                return;

            if (!SameNode(before, S1Dialogue.DialogueHandler.ActiveDialogueNode))
            {
                Logger.Msg($"Choice '{spec.Label}' opened another node in OnChosen; target node '{spec.TargetNodeLabel}' skipped.");
                return;
            }

            S1Dialogue.Conversation? conversation = S1Dialogue.DialogueHandler.ActiveDialogue;
            S1Dialogue.DialogueNodeData? target = conversation?.GetDialogueNodeByLabel(spec.TargetNodeLabel);
            if (target == null)
            {
                Logger.Warning($"Choice '{spec.Label}': node '{spec.TargetNodeLabel}' is not in conversation '{conversation?.name ?? "none"}'; the dialogue ends.");
                return;
            }

            try
            {
                __instance.ShowNode(target);
            }
            catch (Exception ex)
            {
                Logger.Error($"Choice '{spec.Label}': node '{spec.TargetNodeLabel}' could not be shown: {ex}");
            }
        }

        private static object KeyOf(S1Dialogue.DialogueHandler handler)
        {
#if (IL2CPPMELON)
            return handler.Pointer;
#else
            return handler;
#endif
        }

        private static bool SameNode(S1Dialogue.DialogueNodeData? a, S1Dialogue.DialogueNodeData? b)
        {
            if (a == null || b == null)
                return a == null && b == null;
#if (IL2CPPMELON)
            return a.Pointer == b.Pointer;
#else
            return ReferenceEquals(a, b);
#endif
        }
    }
}

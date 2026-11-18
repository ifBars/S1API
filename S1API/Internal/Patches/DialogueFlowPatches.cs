#if IL2CPPMELON
using S1Dialogue = Il2CppScheduleOne.Dialogue;
using S1UI = Il2CppScheduleOne.UI;
#elif MONOMELON
using S1Dialogue = ScheduleOne.Dialogue;
using S1UI = ScheduleOne.UI;
#endif

using System;
using HarmonyLib;
using S1API.Entities;
using S1API.Internal.Dialogues;
using S1API.Logging;

namespace S1API.Internal.Patches
{
    /// <summary>
    /// INTERNAL: Continue holds and the continue press from code.
    /// </summary>
    [HarmonyPatch]
    internal static class DialogueFlowPatches
    {
        private static readonly Log Logger = new Log("DialogueFlowPatches");

#if MONOMELON
        private static readonly AccessTools.FieldRef<S1UI.DialogueCanvas, bool> ContinuePressed =
            AccessTools.FieldRefAccess<S1UI.DialogueCanvas, bool>("_continuePressed");
#endif

        [HarmonyPatch(typeof(S1Dialogue.DialogueHandler), nameof(S1Dialogue.DialogueHandler.ContinueSubmitted))]
        [HarmonyPrefix]
        private static bool ContinueSubmittedPrefix(S1Dialogue.DialogueHandler __instance)
        {
            try
            {
                if (__instance == null || DialogueHolds.Count == 0)
                    return true;
                return !DialogueHolds.IsHeld(owner => owner is NPCDialogue dialogue && dialogue.Handler == __instance);
            }
            catch (Exception ex)
            {
                Logger.Error($"Continue hold check failed; continue runs as vanilla: {ex}");
                return true;
            }
        }

        // Update clears the flag and then sets it from the continue input; the dialogue coroutine reads it after
        // Update in the same frame, so a press set here counts like the player's key.
        [HarmonyPatch(typeof(S1UI.DialogueCanvas), "Update")]
        [HarmonyPostfix]
        private static void CanvasUpdatePostfix(S1UI.DialogueCanvas __instance)
        {
            if (!DialogueHolds.PendingContinue)
                return;
            try
            {
                if (!DialogueHolds.TakePendingContinue())
                    return;
#if IL2CPPMELON
                __instance._continuePressed = true;
#else
                ContinuePressed(__instance) = true;
#endif
            }
            catch (Exception ex)
            {
                Logger.Error($"Continue press from code failed: {ex}");
            }
        }
    }
}

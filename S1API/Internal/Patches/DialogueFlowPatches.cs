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
    /// <remarks>
    /// The dialogue canvas coroutine waits for <c>_continuePressed</c> at the continue prompt, then hides the prompt
    /// and calls <c>DialogueHandler.ContinueSubmitted</c> once. A hold therefore clears the press before the
    /// coroutine reads it: the prompt stays and the next press after the hold is released advances as usual.
    /// </remarks>
    [HarmonyPatch]
    internal static class DialogueFlowPatches
    {
        private static readonly Log Logger = new Log("DialogueFlowPatches");

#if MONOMELON
        private static readonly AccessTools.FieldRef<S1UI.DialogueCanvas, bool> ContinuePressed =
            AccessTools.FieldRefAccess<S1UI.DialogueCanvas, bool>("_continuePressed");
        private static readonly AccessTools.FieldRef<S1UI.DialogueCanvas, S1UI.DialogueChoiceEntry> ContinueChoice =
            AccessTools.FieldRefAccess<S1UI.DialogueCanvas, S1UI.DialogueChoiceEntry>("continueChoice");
        private static readonly AccessTools.FieldRef<S1UI.DialogueCanvas, S1Dialogue.DialogueHandler> CurrentHandler =
            AccessTools.FieldRefAccess<S1UI.DialogueCanvas, S1Dialogue.DialogueHandler>("currentHandler");
#endif

        // Update clears the flag and then sets it from the continue input; the dialogue coroutine reads it after
        // Update in the same frame. A press from code is set here like the player's key, and a held press is
        // cleared here so the coroutine keeps waiting at the continue prompt.
        [HarmonyPatch(typeof(S1UI.DialogueCanvas), "Update")]
        [HarmonyPostfix]
        private static void CanvasUpdatePostfix(S1UI.DialogueCanvas __instance)
        {
            if (!DialogueHolds.PendingContinue && DialogueHolds.Count == 0)
                return;
            try
            {
                bool open = __instance != null && __instance.IsOpen;
                if (DialogueHolds.PendingContinue)
                {
                    if (!open)
                        DialogueHolds.DropPendingContinue();
                    else if (DialogueHolds.TakePendingContinue())
                        SetPressed(__instance!, true);
                }

                if (!open || DialogueHolds.Count == 0 || !GetPressed(__instance!))
                    return;
                // During the text rollout the press only completes the text; the hold applies at the prompt.
                if (!AtContinuePrompt(__instance!))
                    return;
                var handler = GetHandler(__instance!);
                if (handler == null)
                    return;
                if (DialogueHolds.IsHeld(owner => owner is NPCDialogue dialogue && dialogue.Handler == handler))
                    SetPressed(__instance!, false);
            }
            catch (Exception ex)
            {
                Logger.Error($"Continue hold check failed; the press counts as vanilla: {ex}");
            }
        }

        private static bool AtContinuePrompt(S1UI.DialogueCanvas canvas)
        {
#if IL2CPPMELON
            var entry = canvas.continueChoice;
#else
            var entry = ContinueChoice(canvas);
#endif
            return entry != null && entry.GameObject != null && entry.GameObject.activeSelf;
        }

        private static S1Dialogue.DialogueHandler GetHandler(S1UI.DialogueCanvas canvas)
        {
#if IL2CPPMELON
            return canvas.currentHandler;
#else
            return CurrentHandler(canvas);
#endif
        }

        private static bool GetPressed(S1UI.DialogueCanvas canvas)
        {
#if IL2CPPMELON
            return canvas._continuePressed;
#else
            return ContinuePressed(canvas);
#endif
        }

        private static void SetPressed(S1UI.DialogueCanvas canvas, bool value)
        {
#if IL2CPPMELON
            canvas._continuePressed = value;
#else
            ContinuePressed(canvas) = value;
#endif
        }
    }
}

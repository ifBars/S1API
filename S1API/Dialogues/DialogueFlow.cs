#if IL2CPPMELON
using Il2CppScheduleOne.DevUtilities;
using Il2CppScheduleOne.UI;
#elif MONOMELON
using ScheduleOne.DevUtilities;
using ScheduleOne.UI;
#endif

using System.Runtime.CompilerServices;
using S1API.Internal.Dialogues;

namespace S1API.Dialogues
{
    /// <summary>
    /// Reads and drives the open dialogue from code.
    /// </summary>
    public static class DialogueFlow
    {
        /// <summary>
        /// Whether the dialogue canvas currently shows a conversation.
        /// </summary>
        public static bool IsOpen => CanvasOpen();

        /// <summary>
        /// Presses continue for the open dialogue, the same as the player pressing the continue key.
        /// </summary>
        /// <remarks>
        /// The press is applied on the next dialogue canvas update: it skips the text rollout or answers the continue
        /// prompt, as the player's key would. A continue hold (<c>NPCDialogue.HoldContinue</c>) applies at the continue
        /// prompt, not to skipping the rollout. A press that is held is dropped, not kept for later.
        /// Returns false and logs a warning when no dialogue is open.
        /// </remarks>
        /// <returns>True when the press was queued.</returns>
        public static bool TryContinue()
        {
            if (!CanvasOpen())
            {
                DialogueHolds.Warn("TryContinue ignored: no dialogue is open.");
                return false;
            }
            DialogueHolds.PendingContinue = true;
            return true;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool CanvasOpen() =>
            Singleton<DialogueCanvas>.InstanceExists && Singleton<DialogueCanvas>.Instance.IsOpen;
    }
}

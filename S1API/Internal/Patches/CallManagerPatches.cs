using System;
using System.Reflection;
using HarmonyLib;
using MelonLoader;
using S1API.PhoneCalls;
using S1API.Internal.Utils;

#if (IL2CPPMELON)
using S1Calling = Il2CppScheduleOne.Calling;
using S1ScriptableObjects = Il2CppScheduleOne.ScriptableObjects;
using S1UIPhone = Il2CppScheduleOne.UI.Phone;
#elif (MONOMELON || MONOBEPINEX || IL2CPPBEPINEX)
using S1Calling = ScheduleOne.Calling;
using S1ScriptableObjects = ScheduleOne.ScriptableObjects;
using S1UIPhone = ScheduleOne.UI.Phone;
#endif

namespace S1API.Internal.Patches
{
    /// <summary>
    /// INTERNAL: Ensures all phone calls, including those queued by the base game,
    /// flow through the S1API queuing layer so they respect pending calls.
    /// </summary>
    [HarmonyPatch]
    internal static class CallManagerPatches
    {
        /// <summary>
        /// Intercept game QueueCall and route to S1API queue unless we're currently
        /// dispatching to the game (to avoid recursion).
        /// </summary>
        [HarmonyPatch(typeof(S1Calling.CallManager), "QueueCall")]
        [HarmonyPrefix]
        private static bool QueueCall_Prefix(S1ScriptableObjects.PhoneCallData data)
        {
            if (CallManager.IsDispatchingToGameQueue)
            {
                return true; // allow original; this was initiated by S1API dispatcher
            }

            var gameCallManager = S1Calling.CallManager.Instance;
            if (gameCallManager == null)
            {
                return true; // no game manager yet; let original handle or no-op safely
            }

            var callInterface = S1UIPhone.CallInterface.Instance;
            
            // Route base game calls through S1API queue if:
            // 1. There's an active call (need to preserve order)
            // 2. S1API has pending calls (S1API is managing the queue)
            // 3. The game has a queued call (could be from S1API, need to preserve order)
            // This ensures proper ordering: if anything is queued or active, route through S1API.
            bool hasActiveCall = callInterface != null && callInterface.ActiveCallData != null;
            bool hasPendingCalls = CallManager.PendingCount > 0;
            var queuedCallData = ReflectionUtils.TryGetFieldOrProperty(gameCallManager, "QueuedCallData") as S1ScriptableObjects.PhoneCallData;
            bool hasQueuedCall = queuedCallData != null;
            
            if (hasActiveCall || hasPendingCalls || hasQueuedCall)
            {
                CallManager.QueueCall(data);
                return false; // skip original to avoid CLOBBERIN' TIME
            }

            // No active call, no queued call, and S1API queue is empty; allow the original to proceed.
            return true;
        }

        [HarmonyPatch(typeof(S1Calling.CallManager), "CallCompleted")]
        [HarmonyPostfix]
        private static void CallCompleted_Postfix()
        {
            CallManager.TryProcessQueue();
        }

        /// <summary>
        /// Process the queue after CallInterface.Close() clears ActiveCallData.
        /// This ensures we process the queue after ActiveCallData is actually null, not just when CallCompleted fires.
        /// </summary>
        [HarmonyPatch(typeof(S1UIPhone.CallInterface), "Close")]
        [HarmonyPostfix]
        private static void Close_Postfix()
        {
            var callInterface = S1UIPhone.CallInterface.Instance;
            if (callInterface == null)
                return;
            
            // Only process if the interface is actually closed and ActiveCallData is cleared
            if (!callInterface.IsOpen && callInterface.ActiveCallData == null)
            {
                CallManager.TryProcessQueue();
            }
        }

        /// <summary>
        /// Defensive patch: if the game tries to start a call with a null CallerID, stub one to prevent UI breakage.
        /// </summary>
        [HarmonyPatch(typeof(S1UIPhone.CallInterface), "StartCall")]
        [HarmonyPrefix]
        private static void StartCall_Prefix(ref S1ScriptableObjects.PhoneCallData data)
        {
            if (data == null)
                return;
            try
            {
                if (data.CallerID == null)
                {
                    var caller = UnityEngine.ScriptableObject.CreateInstance<S1ScriptableObjects.CallerID>();
                    caller.Name = "Unknown Caller";
                    caller.ProfilePicture = null;
                    data.CallerID = caller;
                }
                // If stages are missing or empty, skip starting the call
                if (data.Stages == null || data.Stages.Length == 0)
                {
                    // Cancel UI open by finishing immediately and letting the dispatcher try the next one
                    return;
                }
            }
            catch (Exception e)
            {
                try { MelonLogger.Warning($"[CallManager] StartCall_Prefix failed: {e.Message}\n{e.StackTrace}"); } catch { }
            }
        }
    }
}



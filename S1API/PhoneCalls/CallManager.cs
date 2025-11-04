#if (IL2CPPMELON)
using S1Calling = Il2CppScheduleOne.Calling;
using S1UIPhone = Il2CppScheduleOne.UI.Phone;
using S1ScriptableObjects = Il2CppScheduleOne.ScriptableObjects;
using ActionPhoneCall = Il2CppSystem.Action<Il2CppScheduleOne.ScriptableObjects.PhoneCallData>;
#elif (MONOMELON || MONOBEPINEX || IL2CPPBEPINEX)
using S1Calling = ScheduleOne.Calling;
using S1UIPhone = ScheduleOne.UI.Phone;
using S1ScriptableObjects = ScheduleOne.ScriptableObjects;
using ActionPhoneCall = System.Action<ScheduleOne.ScriptableObjects.PhoneCallData>;
#endif
using System;
using System.Collections.Generic;
using System.Reflection;
using S1API.Internal.Utils;

namespace S1API.PhoneCalls
{
    /// <summary>
    /// Provides a safe queuing layer on top of the game's CallManager.
    /// Multiple pending calls are stored and dispatched to the game one at a time.
    /// </summary>
    public static class CallManager
    {
        private static readonly Queue<S1ScriptableObjects.PhoneCallData> PendingCalls = new();
        internal static bool IsDispatchingToGameQueue;

        /// <summary>
        /// Number of calls currently pending in the S1API queue (excluding the game's active/queued call).
        /// </summary>
        public static int PendingCount => PendingCalls.Count;

        /// <summary>
        /// Enqueue a phone call to be played. If the game's queue is empty, this will be forwarded immediately;
        /// otherwise it will be held until the current call completes.
        /// </summary>
        public static void QueueCall(PhoneCallDefinition phoneCallDefinition)
        {
            if (phoneCallDefinition == null)
            {
                throw new ArgumentNullException(nameof(phoneCallDefinition));
            }

            PendingCalls.Enqueue(phoneCallDefinition.S1PhoneCallData);
            TryProcessQueue();
        }

        /// <summary>
        /// Enqueue a raw in-game PhoneCallData. Used by patches that intercept the base game's queuing.
        /// </summary>
        internal static void QueueCall(S1ScriptableObjects.PhoneCallData phoneCallData)
        {
            if (phoneCallData == null)
            {
                throw new ArgumentNullException(nameof(phoneCallData));
            }

            PendingCalls.Enqueue(phoneCallData);
            TryProcessQueue();
        }

        /// <summary>
        /// Clears all pending S1API-queued calls. Does not affect the game's current queued/active call.
        /// </summary>
        public static void ClearPendingQueue()
        {
            PendingCalls.Clear();
        }

        private static S1ScriptableObjects.PhoneCallData GetQueuedCallData(S1Calling.CallManager manager)
        {
            return ReflectionUtils.TryGetFieldOrProperty(manager, "QueuedCallData") as S1ScriptableObjects.PhoneCallData;
        }

        internal static void TryProcessQueue()
        {
            var gameCallManager = S1Calling.CallManager.Instance;
            if (gameCallManager == null)
            {
                return;
            }

            var callInterface = S1UIPhone.CallInterface.Instance;
            if (callInterface == null) return;

            // If there's an active call in progress, wait until it completes.
            if (callInterface.ActiveCallData != null)
            {
                return;
            }

            // If the game already has a queued call, wait until it is consumed/completed.
            if (GetQueuedCallData(gameCallManager) != null)
            {
                return;
            }

            if (PendingCalls.Count == 0)
            {
                return;
            }

            S1ScriptableObjects.PhoneCallData next = null;
            // Pull until we find a valid call or run out
            while (PendingCalls.Count > 0)
            {
                var candidate = PendingCalls.Dequeue();
                if (candidate == null)
                    continue;
                try
                {
                    if (candidate.CallerID == null)
                    {
                        var caller = UnityEngine.ScriptableObject.CreateInstance<S1ScriptableObjects.CallerID>();
                        caller.Name = "Unknown Caller";
                        caller.ProfilePicture = null;
                        candidate.CallerID = caller;
                    }
                    if (candidate.Stages == null)
                    {
                        candidate.Stages = System.Array.Empty<S1ScriptableObjects.PhoneCallData.Stage>();
                    }
                }
                catch { }

                // Skip calls with no stages; they will crash UI (ShowStage(0))
                if (candidate.Stages == null || candidate.Stages.Length == 0)
                {
                    continue;
                }

                next = candidate;
                break;
            }

            if (next == null)
            {
                return;
            }
            IsDispatchingToGameQueue = true;
            try
            {
                gameCallManager.QueueCall(next);
            }
            finally
            {
                IsDispatchingToGameQueue = false;
            }
        }
    }
}
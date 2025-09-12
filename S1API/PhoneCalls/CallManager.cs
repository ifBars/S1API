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

namespace S1API.PhoneCalls
{
    /// <summary>
    /// Provides a safe queuing layer on top of the game's CallManager.
    /// Multiple pending calls are stored and dispatched to the game one at a time.
    /// </summary>
    public static class CallManager
    {
        private static readonly Queue<S1ScriptableObjects.PhoneCallData> PendingCalls = new Queue<S1ScriptableObjects.PhoneCallData>();
        private static bool subscribedToCallCompleted;
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
            EnsureSubscribed();
            TryProcessQueue();
        }

        /// <summary>
        /// Enqueue a raw in-game PhoneCallData. Used by patches that intercept the base game's queuing.
        /// </summary>
        public static void QueueCall(S1ScriptableObjects.PhoneCallData phoneCallData)
        {
            if (phoneCallData == null)
            {
                throw new ArgumentNullException(nameof(phoneCallData));
            }

            PendingCalls.Enqueue(phoneCallData);
            EnsureSubscribed();
            TryProcessQueue();
        }

        /// <summary>
        /// Clears all pending S1API-queued calls. Does not affect the game's current queued/active call.
        /// </summary>
        public static void ClearPendingQueue()
        {
            PendingCalls.Clear();
        }

        private static void EnsureSubscribed()
        {
            if (subscribedToCallCompleted)
            {
                return;
            }

            var callInterface = S1UIPhone.CallInterface.Instance;
            if (callInterface == null)
            {
                // Instance not available yet; we'll attempt subscription again on the next call.
                return;
            }

#if (IL2CPPMELON || IL2CPPBEPINEX)
            // For IL2CPP, use CombineImpl to properly combine Il2CppSystem.Action instances
            var systemAction = new System.Action<S1ScriptableObjects.PhoneCallData>(OnCallCompleted);
            var il2cppAction = (ActionPhoneCall)systemAction;
            callInterface.CallCompleted = (ActionPhoneCall)(callInterface.CallCompleted?.CombineImpl(il2cppAction) ?? il2cppAction);
#else
            callInterface.CallCompleted = (ActionPhoneCall)Delegate.Combine(
                callInterface.CallCompleted,
                new ActionPhoneCall(OnCallCompleted)
            );
#endif

            subscribedToCallCompleted = true;
        }

        private static void OnCallCompleted(S1ScriptableObjects.PhoneCallData _)
        {
            // When any call completes, attempt to push the next pending call into the game's queue.
            TryProcessQueue();
        }

        private static void TryProcessQueue()
        {
            var gameCallManager = S1Calling.CallManager.Instance;
            if (gameCallManager == null)
            {
                return;
            }

            // If the game already has a queued call, wait until it is consumed/completed.
            if (gameCallManager.QueuedCallData != null)
            {
                return;
            }

            if (PendingCalls.Count == 0)
            {
                return;
            }

            var next = PendingCalls.Dequeue();
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

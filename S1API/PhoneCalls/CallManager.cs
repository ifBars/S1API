#if (IL2CPPMELON)
using S1Calling = Il2CppScheduleOne.Calling;
using S1UIPhone = Il2CppScheduleOne.UI.Phone;
using S1ScriptableObjects = Il2CppScheduleOne.ScriptableObjects;
using ActionPhoneCall = Il2CppSystem.Action<Il2CppScheduleOne.ScriptableObjects.PhoneCallData>;
using Il2CppInterop.Runtime;
#elif (MONOMELON || MONOBEPINEX || IL2CPPBEPINEX)
using S1Calling = ScheduleOne.Calling;
using S1UIPhone = ScheduleOne.UI.Phone;
using S1ScriptableObjects = ScheduleOne.ScriptableObjects;
using ActionPhoneCall = System.Action<ScheduleOne.ScriptableObjects.PhoneCallData>;
#endif
using System;
using System.Collections.Generic;
using System.Reflection;

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

#if IL2CPPMELON
        // Cache delegates for IL2CPP to prevent GC and invalid function pointers
        private static System.Action<S1ScriptableObjects.PhoneCallData>? cachedManagedOnCompleted;
        private static ActionPhoneCall? cachedIl2CppOnCompleted;
#endif

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
        internal static void QueueCall(S1ScriptableObjects.PhoneCallData phoneCallData)
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

#if IL2CPPMELON
            // Build and cache delegates to ensure stable IL2CPP invocation
            cachedManagedOnCompleted ??= new Action<S1ScriptableObjects.PhoneCallData>(OnCallCompleted);
            cachedIl2CppOnCompleted ??= DelegateSupport.ConvertDelegate<ActionPhoneCall>(cachedManagedOnCompleted);

            // Prefer the generated add_ accessor if available
            var addMethod = typeof(S1UIPhone.CallInterface).GetMethod("add_CallCompleted", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (addMethod != null)
            {
                addMethod.Invoke(callInterface, new object[] { cachedIl2CppOnCompleted });
            }
            else
            {
                // Fallback to explicit combine using Il2CppSystem.Delegate
                var current = callInterface.CallCompleted;
                if (current == null)
                {
                    callInterface.CallCompleted = cachedIl2CppOnCompleted;
                }
                else
                {
                    var combined = Il2CppSystem.Delegate.Combine(current, cachedIl2CppOnCompleted);
                    callInterface.CallCompleted = (ActionPhoneCall)combined;
                }
            }
#else
            // Managed backends can use standard subscription semantics
            callInterface.CallCompleted += new ActionPhoneCall(OnCallCompleted);
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


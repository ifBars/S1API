using HarmonyLib;
using S1API.PhoneCalls;

#if (IL2CPPMELON)
using S1Calling = Il2CppScheduleOne.Calling;
using S1ScriptableObjects = Il2CppScheduleOne.ScriptableObjects;
#elif (MONOMELON || MONOBEPINEX || IL2CPPBEPINEX)
using S1Calling = ScheduleOne.Calling;
using S1ScriptableObjects = ScheduleOne.ScriptableObjects;
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
        [HarmonyPatch(typeof(S1Calling.CallManager), nameof(S1Calling.CallManager.QueueCall))]
        [HarmonyPrefix]
        private static bool QueueCall_Prefix(S1ScriptableObjects.PhoneCallData data)
        {
            if (CallManager.IsDispatchingToGameQueue)
            {
                return true; // allow original; this was initiated by S1API dispatcher
            }

            // Always route through S1API queue so ordering is preserved
            CallManager.QueueCall(data);
            return false; // skip original
        }
    }
}



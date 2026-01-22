#if (IL2CPPMELON)
using S1Graffiti = Il2CppScheduleOne.Graffiti;
#elif (MONOMELON || MONOBEPINEX || IL2CPPBEPINEX)
using S1Graffiti = ScheduleOne.Graffiti;
#endif

using System;
using HarmonyLib;
using S1API.Graffiti;
using S1API.Logging;

namespace S1API.Internal.Patches
{
    /// <summary>
    /// INTERNAL: Patches for graffiti-related gameplay events.
    /// </summary>
    [HarmonyPatch]
    internal class GraffitiPatches
    {
        private static readonly Log Logger = new Log("GraffitiPatches");

        /// <summary>
        /// Fires the GraffitiCompleted event when a player finishes a graffiti piece and receives rewards.
        /// </summary>
        /// <param name="__instance">The WorldSpraySurface instance that called Reward.</param>
        [HarmonyPatch(typeof(S1Graffiti.WorldSpraySurface), "Reward")]
        [HarmonyPostfix]
        private static void WorldSpraySurface_Reward_Postfix(S1Graffiti.WorldSpraySurface __instance)
        {
            try
            {
                if (__instance == null)
                {
                    Logger.Warning("__instance is null in Reward patch");
                    return;
                }

                // Fire the event through GraffitiEvents
                var wrappedSurface = new SpraySurface(__instance);
                GraffitiEvents.OnGraffitiRewarded(wrappedSurface);
            }
            catch (Exception ex)
            {
                Logger.Error($"Error in Reward patch: {ex.Message}");
                Logger.Error($"StackTrace: {ex.StackTrace}");
            }
        }
    }
}


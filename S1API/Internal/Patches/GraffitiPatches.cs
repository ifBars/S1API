#if (IL2CPPMELON)
using S1Graffiti = Il2CppScheduleOne.Graffiti;
using Il2CppGuid = Il2CppSystem.Guid;
#elif (MONOMELON || MONOBEPINEX || IL2CPPBEPINEX)
using S1Graffiti = ScheduleOne.Graffiti;
#endif

using System;
using System.Collections.Generic;
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
        private static readonly HashSet<string> _processedSurfaces = new HashSet<string>();

        /// <summary>
        /// Patch SetFinalized (ObserversRpc) which is called after the drawing is marked as complete.
        /// This is more reliable than Reward() which doesn't run on IL2CPP for some reason.
        /// </summary>
        [HarmonyPatch(typeof(S1Graffiti.WorldSpraySurface), "SetFinalized")]
        [HarmonyPostfix]
        private static void WorldSpraySurface_SetFinalized_Postfix(S1Graffiti.WorldSpraySurface __instance)
        {
            try
            {
                if (__instance == null)
                    return;

                // Convert GUID to string for tracking
#if (IL2CPPMELON)
                string guidString = __instance.GUID.ToString();
#elif (MONOMELON || MONOBEPINEX || IL2CPPBEPINEX)
                string guidString = __instance.GUID.ToString();
#endif

                // Check if we've already processed this surface
                if (_processedSurfaces.Contains(guidString))
                    return;

                // Only fire the event if the surface has actually been marked by the player
                if (__instance.HasEverBeenMarkedByPlayer && __instance.DrawingStrokeCount > 0)
                {
                    _processedSurfaces.Add(guidString);
                    
                    var wrappedSurface = new SpraySurface(__instance);
                    GraffitiEvents.OnGraffitiRewarded(wrappedSurface);
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error in SetFinalized patch: {ex.Message}");
                Logger.Error($"StackTrace: {ex.StackTrace}");
            }
        }
    }
}


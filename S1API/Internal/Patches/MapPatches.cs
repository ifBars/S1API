#if (IL2CPPMELON)
using S1Map = Il2CppScheduleOne.Map;
using S1Economy = Il2CppScheduleOne.Economy;
using S1AvatarAnimation = Il2CppScheduleOne.AvatarFramework.Animation;
#elif (MONOMELON || MONOBEPINEX || IL2CPPBEPINEX)
using S1Map = ScheduleOne.Map;
using S1Economy = ScheduleOne.Economy;
using S1AvatarAnimation = ScheduleOne.AvatarFramework.Animation;
#endif

using HarmonyLib;
using S1API.Map;
using S1API.Avatar;

namespace S1API.Internal.Patches
{
    /// <summary>
    /// INTERNAL: Patches to apply to Map objects for reliable tracking.
    /// </summary>
    [HarmonyPatch]
    internal class MapPatches
    {
        /// <summary>
        /// INTERNAL: Registers buildings when they are created.
        /// </summary>
        /// <param name="__instance">The building to register.</param>
        [HarmonyPatch(typeof(S1Map.NPCEnterableBuilding), "Awake")]
        [HarmonyPostfix]
        private static void NPCEnterableBuildingAwake(S1Map.NPCEnterableBuilding __instance) =>
            Building.Register(__instance);

        /// <summary>
        /// INTERNAL: Registers delivery locations when they are created.
        /// </summary>
        /// <param name="__instance">The delivery location to register.</param>
        [HarmonyPatch(typeof(S1Economy.DeliveryLocation), "Awake")]
        [HarmonyPostfix]
        private static void DeliveryLocationAwake(S1Economy.DeliveryLocation __instance) =>
            DeliveryLocation.Register(__instance);
    }
}

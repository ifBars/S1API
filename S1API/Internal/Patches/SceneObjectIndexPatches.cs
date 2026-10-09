using HarmonyLib;
using S1API.Entities;
using S1API.Internal.Utils;
#if IL2CPPMELON
using S1Map = Il2CppScheduleOne.Map;
using S1Relations = Il2CppScheduleOne.UI.Relations;
#elif MONOMELON
using S1Map = ScheduleOne.Map;
using S1Relations = ScheduleOne.UI.Relations;
#endif

namespace S1API.Internal.Patches
{
    /// <summary>INTERNAL: Keeps indexed UI membership aligned with native assignment.</summary>
    [HarmonyPatch]
    internal static class SceneObjectIndexPatches
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(S1Map.NPCPoI), nameof(S1Map.NPCPoI.SetNPC))]
        private static void NpcPoiAssigned(S1Map.NPCPoI __instance) =>
            NPCAppearance.PoisByNpcId.NotifyChanged(__instance);

        [HarmonyPostfix]
        [HarmonyPatch(typeof(S1Relations.RelationCircle), nameof(S1Relations.RelationCircle.AssignNPC))]
        private static void RelationCircleAssigned(S1Relations.RelationCircle __instance) =>
            ContactsAppPatches.CirclesByNpcId.NotifyChanged(__instance);
    }
}

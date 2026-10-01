#if IL2CPPMELON
using S1NPCs = Il2CppScheduleOne.NPCs;
using Il2CppFishNet.Object;
#elif MONOMELON
using S1NPCs = ScheduleOne.NPCs;
using FishNet.Object;
#endif

using HarmonyLib;
using S1API.Logging;

namespace S1API.Internal.Patches
{
    /// <summary>
    /// Keeps a custom NPC's avatar on until the NPC is spawned.
    /// </summary>
    /// <remarks>
    /// <c>SetVisible(false)</c> switches the Avatar object off, and native <c>Awake</c> must find it when the NPC
    /// spawns, or S1API refuses the spawn. A mod can hide an S1API NPC that exists but is not spawned yet: The Big
    /// Pimpin's escort setup does, on the second load of a session. Such a hide is ignored; <c>FinalizeNetworkSpawn</c>
    /// applies the intended visibility after the spawn, and the NPC can be hidden normally from then on.
    /// </remarks>
    [HarmonyPatch(typeof(S1NPCs.NPC), nameof(S1NPCs.NPC.SetVisible), new[] { typeof(bool), typeof(bool) })]
    internal static class NPCHideBeforeSpawnPatch
    {
        private static readonly Log Logger = new Log("NPCPatches");

        [HarmonyPrefix]
        private static bool Prefix(S1NPCs.NPC __instance, bool visible)
        {
            if (visible || __instance == null)
                return true;

            try
            {
                var networkObject = __instance.GetComponent<NetworkObject>();
                if (!ShouldIgnoreHide(
                        networkObject != null,
                        networkObject != null && networkObject.IsSpawned,
                        NPCPatches.IsS1ApiCustomNpcComponent(__instance)))
                    return true;

                Logger.Debug($"[NPC] Ignored a hide on '{__instance.gameObject.name}' before it was spawned.");
                return false;
            }
            catch
            {
                return true;
            }
        }

        /// <summary>Does the game still have the method this patches? A test fails when it does not.</summary>
        internal static bool TargetExists() =>
            typeof(S1NPCs.NPC).GetMethod(nameof(S1NPCs.NPC.SetVisible), new[] { typeof(bool), typeof(bool) }) != null;

        /// <summary>Is this hide one to ignore: on an S1API NPC with a NetworkObject that is not spawned yet?</summary>
        internal static bool ShouldIgnoreHide(bool hasNetworkObject, bool isSpawned, bool isS1ApiNpc) =>
            hasNetworkObject && !isSpawned && isS1ApiNpc;
    }
}

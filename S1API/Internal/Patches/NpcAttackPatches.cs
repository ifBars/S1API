#if (IL2CPPMELON)
using S1NPCs = Il2CppScheduleOne.NPCs;
using S1PlayerScripts = Il2CppScheduleOne.PlayerScripts;
#elif (MONOMELON)
using S1NPCs = ScheduleOne.NPCs;
using S1PlayerScripts = ScheduleOne.PlayerScripts;
#endif

using System;
using System.Collections.Generic;
using HarmonyLib;
using S1API.Entities;
using S1API.Internal.Entities;

namespace S1API.Internal.Patches
{
    /// <summary>
    /// INTERNAL: Raises <see cref="NPC.OnAttackedByPlayer"/> and <see cref="NPC.OnKnockedOut"/> from the NPC health component.
    /// </summary>
    [HarmonyPatch]
    internal static class NpcAttackPatches
    {
#if MONOMELON
        private static readonly AccessTools.FieldRef<S1NPCs.NPCHealth, S1NPCs.NPC> HealthNpc =
            AccessTools.FieldRefAccess<S1NPCs.NPCHealth, S1NPCs.NPC>("_npc");
#endif

        private static readonly HashSet<string> UnwrappedWarned = new HashSet<string>();

        [HarmonyPatch(typeof(S1NPCs.NPCHealth), nameof(S1NPCs.NPCHealth.NotifyAttackedByPlayer))]
        [HarmonyPostfix]
        private static void NotifyAttackedByPlayerPostfix(S1NPCs.NPCHealth __instance, S1PlayerScripts.Player player)
        {
            try
            {
                var npc = Resolve(__instance, "an attack");
                npc?.RaiseAttackedByPlayer(player);
            }
            catch (Exception ex)
            {
                NpcAttackEvents.Error($"Raising OnAttackedByPlayer failed: {ex}");
            }
        }

        [HarmonyPatch(typeof(S1NPCs.NPCHealth), nameof(S1NPCs.NPCHealth.KnockOut))]
        [HarmonyPostfix]
        private static void KnockOutPostfix(S1NPCs.NPCHealth __instance)
        {
            try
            {
                if (!__instance.IsKnockedOut)
                {
                    NpcAttackEvents.Info("KnockOut on an invincible NPC; OnKnockedOut not raised.");
                    return;
                }
                var npc = Resolve(__instance, "a knockout");
                npc?.RaiseKnockedOut();
            }
            catch (Exception ex)
            {
                NpcAttackEvents.Error($"Raising OnKnockedOut failed: {ex}");
            }
        }

        private static NPC? Resolve(S1NPCs.NPCHealth health, string what)
        {
#if IL2CPPMELON
            var native = health._npc;
#else
            var native = HealthNpc(health);
#endif
            if (native == null)
            {
                NpcAttackEvents.Warn($"NPC health without an NPC reported {what}; no event raised.");
                return null;
            }
            var npc = NPCPatches.FindWrapperForS1Npc(native);
            if (npc == null && UnwrappedWarned.Add(native.ID))
                NpcAttackEvents.Warn($"{native.ID} has no S1API wrapper; {what} raises no event for it.");
            return npc;
        }
    }
}

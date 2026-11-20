#if (IL2CPPMELON)
using S1NPCs = Il2CppScheduleOne.NPCs;
using S1Combat = Il2CppScheduleOne.Combat;
using S1PlayerScripts = Il2CppScheduleOne.PlayerScripts;
#elif (MONOMELON)
using S1NPCs = ScheduleOne.NPCs;
using S1Combat = ScheduleOne.Combat;
using S1PlayerScripts = ScheduleOne.PlayerScripts;
#endif

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using S1API.Entities;
using S1API.Internal.Entities;

namespace S1API.Internal.Patches
{
    /// <summary>
    /// INTERNAL: Raises <see cref="NPC.OnImpact"/> after the game processed a hit.
    /// </summary>
    [HarmonyPatch]
    internal static class NpcImpactPatches
    {
#if MONOMELON
        private static readonly AccessTools.FieldRef<S1NPCs.NPC, List<int>> ImpactHistory =
            AccessTools.FieldRefAccess<S1NPCs.NPC, List<int>>("impactHistory");
#endif

        /// <summary>Selects the generated receive-impact RPC logic, which runs on every peer that processes a hit.</summary>
        /// <returns>The unique method matching the name prefix and the impact parameter.</returns>
        internal static MethodBase TargetMethod() =>
            typeof(S1NPCs.NPC)
                .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Single(method =>
                    method.Name.StartsWith("RpcLogic___ReceiveImpact_", StringComparison.Ordinal)
                    && method.GetParameters().Select(parameter => parameter.ParameterType)
                        .SequenceEqual(new[] { typeof(S1Combat.Impact) }));

        /// <summary>
        /// Notes whether the game will act on the hit. It skips a hit for an NPC that ignores combat impacts
        /// and a hit id it already saw, so the postfix raises the event only for the first case.
        /// </summary>
        [HarmonyPrefix]
        private static void ReceiveImpactPrefix(S1NPCs.NPC __instance, S1Combat.Impact __0, out bool __state)
        {
            __state = false;
            try
            {
                if (__0 == null || __instance.NPCData == null)
                    return;
#if IL2CPPMELON
                var history = __instance.impactHistory;
#else
                var history = ImpactHistory(__instance);
#endif
                __state = !__instance.NPCData.Behaviour.IgnoreCombatImpacts && !history.Contains(__0.ImpactID);
            }
            catch (Exception ex)
            {
                NpcAttackEvents.Error($"Reading the impact state failed; OnImpact not raised for this hit: {ex}");
            }
        }

        [HarmonyPostfix]
        private static void ReceiveImpactPostfix(S1NPCs.NPC __instance, S1Combat.Impact __0, bool __state)
        {
            if (!__state)
                return;
            try
            {
                var npc = NpcAttackPatches.ResolveNative(__instance, "an impact");
                if (npc == null)
                    return;
                Player? attacker = null;
                if (__0.IsPlayerImpact(out var nativePlayer) && nativePlayer != null)
                    attacker = Player.All.FirstOrDefault(p => p.S1Player == nativePlayer);
                npc.RaiseImpact(new NpcImpact(__0, attacker));
            }
            catch (Exception ex)
            {
                NpcAttackEvents.Error($"Raising OnImpact failed: {ex}");
            }
        }

    }
}

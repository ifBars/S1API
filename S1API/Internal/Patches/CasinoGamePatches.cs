#if IL2CPPMELON
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using S1Casino = Il2CppScheduleOne.Casino;
using S1NetworkConnection = Il2CppFishNet.Connection.NetworkConnection;
#elif MONOMELON
using S1Casino = ScheduleOne.Casino;
using S1NetworkConnection = FishNet.Connection.NetworkConnection;
#endif

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using S1API.Casino;

namespace S1API.Internal.Patches
{
    /// <summary>
    /// Bridges native casino state transitions to the read-only managed casino API.
    /// </summary>
    [HarmonyPatch]
    internal static class CasinoGamePatches
    {
        [HarmonyPatch(typeof(S1Casino.BlackjackGameController), "set_CurrentStage")]
        [HarmonyPrefix]
        private static void BlackjackStagePrefix(
            S1Casino.BlackjackGameController __instance,
            out BlackjackStage __state)
        {
            __state = (BlackjackStage)(int)__instance.CurrentStage;
        }

        [HarmonyPatch(typeof(S1Casino.BlackjackGameController), "set_CurrentStage")]
        [HarmonyPostfix]
        private static void BlackjackStagePostfix(
            S1Casino.BlackjackGameController __instance,
            S1Casino.BlackjackGameController.EStage __0,
            BlackjackStage __state)
        {
            CasinoGameRegistry.NotifyBlackjackStageChanged(
                __instance,
                __state,
                (BlackjackStage)(int)__0);
        }

        [HarmonyPatch(typeof(S1Casino.RTBGameController), "set_CurrentStage")]
        [HarmonyPrefix]
        private static void RideTheBusStagePrefix(
            S1Casino.RTBGameController __instance,
            out RideTheBusStage __state)
        {
            __state = (RideTheBusStage)(int)__instance.CurrentStage;
        }

        [HarmonyPatch(typeof(S1Casino.RTBGameController), "set_CurrentStage")]
        [HarmonyPostfix]
        private static void RideTheBusStagePostfix(
            S1Casino.RTBGameController __instance,
            S1Casino.RTBGameController.EStage __0,
            RideTheBusStage __state)
        {
            CasinoGameRegistry.NotifyRideTheBusStageChanged(
                __instance,
                __state,
                (RideTheBusStage)(int)__0);
        }

        internal static MethodBase? FindSlotStartLogicMethod(Type slotMachineType)
        {
            return slotMachineType
                .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .FirstOrDefault(method =>
                {
                    if (!method.Name.StartsWith("RpcLogic___StartSpin_", StringComparison.Ordinal))
                        return false;

                    ParameterInfo[] parameters = method.GetParameters();
                    return method.ReturnType == typeof(void)
                        && parameters.Length == 3
                        && parameters[0].ParameterType == typeof(S1NetworkConnection)
#if IL2CPPMELON
                        && parameters[1].ParameterType == typeof(Il2CppStructArray<S1Casino.SlotMachine.ESymbol>)
#else
                        && parameters[1].ParameterType == typeof(S1Casino.SlotMachine.ESymbol[])
#endif
                        && parameters[2].ParameterType == typeof(int);
                });
        }

        [HarmonyPatch]
        private static class SlotSpinPatch
        {
            private static MethodBase? TargetMethod() =>
                FindSlotStartLogicMethod(typeof(S1Casino.SlotMachine));

            [HarmonyPrefix]
            private static void Prefix(
                S1Casino.SlotMachine __instance,
                S1NetworkConnection __0,
#if IL2CPPMELON
                Il2CppStructArray<S1Casino.SlotMachine.ESymbol> __1,
#else
                S1Casino.SlotMachine.ESymbol[] __1,
#endif
                int __2,
                out SlotStartPatchState __state)
            {
                var symbols = new List<SlotSymbol>(__1.Length);
                for (int i = 0; i < __1.Length; i++)
                    symbols.Add((SlotSymbol)(int)__1[i]);

                __state = new SlotStartPatchState(
                    __instance.IsSpinning,
                    new SlotSpinSnapshot(
                        __2,
                        SlotSpinSnapshot.Freeze(symbols),
                        __0 != null && __0.IsLocalClient,
                        outcome: null,
                        winAmount: null));
            }

            [HarmonyPostfix]
            private static void Postfix(
                S1Casino.SlotMachine __instance,
                SlotStartPatchState __state)
            {
                if (!__state.WasSpinning && __instance.IsSpinning)
                    CasinoGameRegistry.NotifySlotSpinStarted(__instance, __state.Snapshot);
            }
        }

        [HarmonyPatch(typeof(S1Casino.SlotMachine), "DisplayOutcome")]
        [HarmonyPostfix]
        private static void SlotOutcomePostfix(
            S1Casino.SlotMachine __instance,
            S1Casino.SlotMachine.EOutcome __0,
            int __1)
        {
            CasinoGameRegistry.NotifySlotSpinCompleted(
                __instance,
                (SlotOutcome)(int)__0,
                __1);
        }

        private readonly struct SlotStartPatchState
        {
            internal SlotStartPatchState(bool wasSpinning, SlotSpinSnapshot snapshot)
            {
                WasSpinning = wasSpinning;
                Snapshot = snapshot;
            }

            internal bool WasSpinning { get; }
            internal SlotSpinSnapshot Snapshot { get; }
        }
    }
}

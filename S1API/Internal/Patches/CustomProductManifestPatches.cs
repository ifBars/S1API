#if IL2CPPMELON
using S1Connection = Il2CppFishNet.Connection.NetworkConnection;
using S1Persistence = Il2CppScheduleOne.Persistence;
using S1Player = Il2CppScheduleOne.PlayerScripts.Player;
#elif MONOMELON
using S1Connection = FishNet.Connection.NetworkConnection;
using S1Persistence = ScheduleOne.Persistence;
using S1Player = ScheduleOne.PlayerScripts.Player;
#endif

using System;
using System.Reflection;
using HarmonyLib;
using S1API.Internal.Products;

namespace S1API.Internal.Patches
{
    /// <summary>INTERNAL: Starts and stops custom-product manifest handshakes with native loading.</summary>
    [HarmonyPatch]
    internal static class CustomProductManifestLoadPatches
    {
        [HarmonyPatch(typeof(S1Persistence.LoadManager), nameof(S1Persistence.LoadManager.StartGame))]
        [HarmonyPrefix]
        private static void StartGamePrefix()
        {
            CustomProductManifestRuntime.BeginHostSession();
        }

        [HarmonyPatch(typeof(S1Persistence.LoadManager), nameof(S1Persistence.LoadManager.LoadAsClient))]
        [HarmonyPrefix]
        private static void LoadAsClientPrefix()
        {
            CustomProductManifestRuntime.BeginClientSession();
        }

        [HarmonyPatch(typeof(S1Persistence.LoadManager), nameof(S1Persistence.LoadManager.ExitToMenu))]
        [HarmonyPrefix]
        private static void ExitToMenuPrefix()
        {
            CustomProductManifestRuntime.EndHostSession();
            CustomProductManifestRuntime.EndClientSession();
        }

        [HarmonyPatch(typeof(S1Persistence.LoadManager), nameof(S1Persistence.LoadManager.Update))]
        [HarmonyPostfix]
        private static void UpdatePostfix()
        {
            CustomProductManifestRuntime.Tick();
        }
    }

    /// <summary>INTERNAL: Prevents client inventory deserialization before manifest validation.</summary>
    [HarmonyPatch]
    internal static class CustomProductManifestPlayerPatches
    {
        [HarmonyPatch]
        private static class RequestPlayerDataPatch
        {
            private static MethodBase TargetMethod() =>
                AccessTools.DeclaredMethod(
                    typeof(S1Player),
                    "RequestPlayerData_Server") ??
                throw new MissingMethodException(
                    typeof(S1Player).FullName,
                    "RequestPlayerData_Server");

            private static bool Prefix(
                S1Player __instance,
                string playerCode,
                bool isHost,
                MethodBase __originalMethod)
            {
                return CustomProductManifestRuntime.AuthorizeClientPlayerDataRequest(
                    () => __originalMethod.Invoke(
                        __instance,
                        new object[] { playerCode, isHost }));
            }
        }

        [HarmonyPatch]
        private static class SetPlayerDataPatch
        {
            private static MethodBase TargetMethod() =>
                AccessTools.DeclaredMethod(
                    typeof(S1Player),
                    "SetPlayerData_Client") ??
                throw new MissingMethodException(
                    typeof(S1Player).FullName,
                    "SetPlayerData_Client");

            private static bool Prefix(
                object __instance,
                object[] __args,
                MethodBase __originalMethod)
            {
                if (__args.Length == 0 || !(__args[0] is S1Connection connection))
                    return true;

                return CustomProductManifestRuntime.AuthorizeHostPlayerData(
                    __instance,
                    connection,
                    __args,
                    __originalMethod);
            }
        }
    }
}

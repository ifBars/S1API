#if (IL2CPPMELON)
using S1PlayerScripts = Il2CppScheduleOne.PlayerScripts;
using S1Product = Il2CppScheduleOne.Product;
using S1Vehicles = Il2CppScheduleOne.Vehicles;
using S1NetObject = Il2CppFishNet.Object.NetworkObject;
#elif (MONOMELON)
using S1PlayerScripts = ScheduleOne.PlayerScripts;
using S1Product = ScheduleOne.Product;
using S1Vehicles = ScheduleOne.Vehicles;
using S1NetObject = FishNet.Object.NetworkObject;
#endif

using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using S1API.Entities;
using S1API.Internal.Entities;
using S1API.Products;
using S1API.Vehicles;

namespace S1API.Internal.Patches
{
    /// <summary>
    /// INTERNAL: Raises the consume and vehicle events of <see cref="Player"/>.
    /// </summary>
    [HarmonyPatch]
    internal static class PlayerEventPatches
    {
        /// <summary>
        /// Raises <see cref="Player.OnProductConsumed"/> after the game's consume call. A remote player's consume
        /// reaches the game through a different method and raises nothing.
        /// </summary>
        [HarmonyPatch(typeof(S1PlayerScripts.Player), nameof(S1PlayerScripts.Player.ConsumeProduct))]
        [HarmonyPostfix]
        private static void ConsumeProductPostfix(S1PlayerScripts.Player __instance, S1Product.ProductItemInstance product)
        {
            try
            {
                var player = Resolve(__instance, "a consumed product");
                if (player == null)
                    return;
                if (product == null)
                {
                    PlayerEvents.Warn($"{player.Name} consumed a null product; OnProductConsumed not raised.");
                    return;
                }
                player.RaiseProductConsumed(new ProductInstance(product));
            }
            catch (Exception ex)
            {
                PlayerEvents.Error($"Raising OnProductConsumed failed: {ex}");
            }
        }

        /// <summary>
        /// Raises <see cref="Player.OnExitingVehicle"/> before the game moves the body. The state tells the
        /// postfix whether the player was in a vehicle, as the game ignores an exit without one.
        /// </summary>
        [HarmonyPatch(typeof(S1PlayerScripts.Player), nameof(S1PlayerScripts.Player.ExitVehicle))]
        [HarmonyPrefix]
        private static void ExitVehiclePrefix(S1PlayerScripts.Player __instance, out bool __state)
        {
            __state = false;
            try
            {
                var vehicle = CurrentVehicle(__instance);
                if (vehicle == null)
                    return;
                __state = true;
                var player = Resolve(__instance, "a vehicle exit");
                var wrapped = VehicleRegistry.Wrap(vehicle);
                if (player == null)
                    return;
                if (wrapped == null)
                {
                    PlayerEvents.Warn($"{player.Name} exits a vehicle S1API has no wrapper for; OnExitingVehicle not raised.");
                    return;
                }
                player.RaiseExitingVehicle(wrapped);
            }
            catch (Exception ex)
            {
                PlayerEvents.Error($"Raising OnExitingVehicle failed: {ex}");
            }
        }

        /// <summary>Raises <see cref="Player.OnVehicleChanged"/> with null after the local player left a vehicle.</summary>
        [HarmonyPatch(typeof(S1PlayerScripts.Player), nameof(S1PlayerScripts.Player.ExitVehicle))]
        [HarmonyPostfix]
        private static void ExitVehiclePostfix(S1PlayerScripts.Player __instance, bool __state)
        {
            if (!__state)
                return;
            RaiseChanged(__instance, null, "a vehicle exit");
        }

        /// <summary>Raises <see cref="Player.OnVehicleChanged"/> with the vehicle after the local player entered it.</summary>
        [HarmonyPatch(typeof(S1PlayerScripts.Player), nameof(S1PlayerScripts.Player.EnterVehicle))]
        [HarmonyPostfix]
        private static void EnterVehiclePostfix(S1PlayerScripts.Player __instance, S1Vehicles.LandVehicle vehicle)
        {
            RaiseChanged(__instance, vehicle, "a vehicle entry");
        }

        /// <summary>
        /// Raises <see cref="Player.OnVehicleChanged"/> for a remote player when the synced vehicle arrives.
        /// The game skips the owner here (its own enter and exit calls cover that). A host may receive the
        /// change as a server call, a client call or both, so the first call per change raises and the last known
        /// vehicle per player drops the repeat.
        /// </summary>
        [HarmonyPatch(typeof(S1PlayerScripts.Player), "CurrentVehicleChanged")]
        [HarmonyPostfix]
        private static void CurrentVehicleChangedPostfix(S1PlayerScripts.Player __instance, S1NetObject oldVeh, S1NetObject newVeh, bool asServer)
        {
            try
            {
                if (__instance.IsOwner || oldVeh == newVeh)
                    return;
                if (RemoteVehicles.TryGetValue(__instance, out var known) ? known == newVeh : newVeh == null)
                    return;
                if (newVeh == null)
                    RemoteVehicles.Remove(__instance);
                else
                    RemoteVehicles[__instance] = newVeh;
                var vehicle = newVeh != null ? newVeh.GetComponent<S1Vehicles.LandVehicle>() : null;
                RaiseChanged(__instance, vehicle, "a synced vehicle change");
            }
            catch (Exception ex)
            {
                PlayerEvents.Error($"Raising OnVehicleChanged for a synced change failed: {ex}");
            }
        }

        private static readonly Dictionary<S1PlayerScripts.Player, S1NetObject> RemoteVehicles =
            new Dictionary<S1PlayerScripts.Player, S1NetObject>();

        private static void RaiseChanged(S1PlayerScripts.Player native, S1Vehicles.LandVehicle? vehicle, string what)
        {
            try
            {
                var player = Resolve(native, what);
                if (player == null)
                    return;
                var wrapped = vehicle != null ? VehicleRegistry.Wrap(vehicle) : null;
                if (vehicle != null && wrapped == null)
                {
                    PlayerEvents.Warn($"{player.Name} changed to a vehicle S1API has no wrapper for; OnVehicleChanged not raised.");
                    return;
                }
                player.RaiseVehicleChanged(wrapped);
            }
            catch (Exception ex)
            {
                PlayerEvents.Error($"Raising OnVehicleChanged after {what} failed: {ex}");
            }
        }

        private static S1Vehicles.LandVehicle? CurrentVehicle(S1PlayerScripts.Player native)
        {
            var netObject = native.CurrentVehicle;
            return netObject != null ? netObject.GetComponent<S1Vehicles.LandVehicle>() : null;
        }

        private static Player? Resolve(S1PlayerScripts.Player native, string what)
        {
            var player = Player.All.FirstOrDefault(p => p.S1Player == native);
            if (player == null)
                PlayerEvents.Warn($"A player without an S1API wrapper reported {what}; no event raised.");
            return player;
        }
    }
}

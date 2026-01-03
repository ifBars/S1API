#if (IL2CPPMELON)
using Il2Cpp;
using S1Vehicles = Il2CppScheduleOne.Vehicles;
using S1Guid = Il2CppSystem.Guid;
#elif (MONOMELON || MONOBEPINEX || IL2CPPBEPINEX)
using S1Vehicles = ScheduleOne.Vehicles;
using S1Guid = System.Guid;
#endif
using System;
using System.Collections.Generic;
using System.Linq;
using S1API.Internal.Map;
using UnityEngine.SceneManagement;

namespace S1API.Vehicles
{
    /// <summary>
    /// Registry utilities for discovering and resolving vehicles without exposing game types.
    /// </summary>
    public static class VehicleRegistry
    {
        private static readonly Dictionary<object, LandVehicle> _cache = new Dictionary<object, LandVehicle>();

        /// <summary>
        /// Returns all currently spawned land vehicles wrapped for modder use.
        /// </summary>
        public static LandVehicle[] GetAll()
        {
            try
            {
                var list = S1Vehicles.VehicleManager.Instance.AllVehicles;
                if (list == null)
                    return Array.Empty<LandVehicle>();
                
                var results = new List<LandVehicle>();
                foreach (var vehicle in list)
                {
                    if (vehicle != null)
                    {
                        var wrapped = Wrap(vehicle);
                        if (wrapped != null)
                            results.Add(wrapped);
                    }
                }
                return results.ToArray();
            }
            catch
            {
                return Array.Empty<LandVehicle>();
            }
        }

        /// <summary>
        /// Finds a vehicle by GUID string. Returns null if not found.
        /// </summary>
        public static LandVehicle GetByGUID(string guid)
        {
            if (string.IsNullOrEmpty(guid))
                return null;
            try
            {
#if (MONOMELON || MONOBEPINEX)
                var g = new System.Guid(guid);
#else
                var g = new Il2CppSystem.Guid(guid);
#endif
                var veh = GUIDManager.GetObject<S1Vehicles.LandVehicle>(g);
                if (veh == null)
                    return null;
                return Wrap(veh);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Finds a vehicle by GameObject name. Useful when vehicles aren't spawned yet.
        /// Falls back to finding in all vehicles if GameObject.Find fails.
        /// </summary>
        /// <param name="gameObjectName">The name of the GameObject containing the vehicle.</param>
        /// <returns>A vehicle wrapper, or null if not found.</returns>
        public static LandVehicle GetByName(string gameObjectName)
        {
            if (string.IsNullOrEmpty(gameObjectName))
                return null;
            try
            {
                // First try GameObject.Find
                var go = UnityEngine.GameObject.Find(gameObjectName);
                if (go != null)
                {
                    var veh = go.GetComponent<S1Vehicles.LandVehicle>();
                    if (veh != null)
                        return Wrap(veh);
                }

                // Fallback: scan all vehicles for matching name
                var list = S1Vehicles.VehicleManager.Instance.AllVehicles;
                if (list != null)
                {
                    for (int i = 0; i < list.Count; i++)
                    {
                        var veh = list[i];
                        if (veh != null && veh.gameObject.name == gameObjectName)
                            return Wrap(veh);
                    }
                }

                // If not found and we're in Menu scene, create deferred wrapper
                if (DeferredMapResolver.IsMenuScene())
                {
                    var deferredVehicle = new LandVehicle(gameObjectName, isDeferred: true);
                    DeferredMapResolver.RegisterDeferredLookup(new DeferredLookup(gameObjectName, (resolved) =>
                    {
                        if (resolved is LandVehicle vehicle && vehicle != null && vehicle.S1LandVehicle != null)
                        {
                            deferredVehicle.S1LandVehicle = vehicle.S1LandVehicle;
                            deferredVehicle._isDeferredByName = false;
                        }
                    }));
                    return deferredVehicle;
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Creates a new vehicle instance using a vehicle code and returns a wrapper.
        /// Useful when you need a vehicle that doesn't exist yet.
        /// </summary>
        /// <param name="vehicleCode">The vehicle code to spawn (e.g., "Sedan", "SUV", etc.).</param>
        /// <returns>A new vehicle wrapper, or null if creation fails.</returns>
        public static LandVehicle CreateVehicle(string vehicleCode)
        {
            if (!string.IsNullOrEmpty(vehicleCode))
            {
                try
                {
                    return new LandVehicle(vehicleCode);
                }
                catch
                {
                    return null;
                }
            }
            return null;
        }

        /// <summary>
        /// Removes a vehicle from the game's lists, for permanently destroying a vehicle. This will be called automatically in OnDestroy, so there's likely no need to call this manually.
        /// </summary>
        /// <param name="guidString">The GUID string of the vehicle to remove.</param>
        public static void RemoveVehicle(string guidString) {
            if (string.IsNullOrEmpty(guidString))
                return;

            var registry = S1Vehicles.VehicleManager.Instance;
            if (registry == null)
                return;

            S1Guid guid = new S1Guid(guidString);

            int? allIndex = null;
            S1Vehicles.LandVehicle? gameVehicle = null;

            var allVehicles = registry.AllVehicles;
            if (allVehicles != null)
            {
                for (int i = 0; i < allVehicles.Count; ++i) {
                    if (allVehicles[i]?.GUID.Equals(guid) ?? false) {
                        allIndex = i;
                        gameVehicle = allVehicles[i];
                        break;
                    }
                }
            }

            int? playerIndex = null;
            var playerVehicles = registry.PlayerOwnedVehicles;
            if (playerVehicles != null)
            {
                for (int i = 0; i < playerVehicles.Count; ++i) {
                    if (playerVehicles[i]?.GUID.Equals(guid) ?? false) {
                        playerIndex = i;
                        break;
                    }
                }
            }

            if (allIndex != null && allVehicles != null) {
                allVehicles.RemoveAt((int)allIndex);
            }
            if (playerIndex != null && playerVehicles != null) {
                playerVehicles.RemoveAt((int)playerIndex);
            }

            if (gameVehicle != null && _cache.ContainsKey(gameVehicle))
                _cache.Remove(gameVehicle);
        }

        private static LandVehicle Wrap(S1Vehicles.LandVehicle veh)
        {
            if (veh == null)
                return null;
            if (_cache.TryGetValue(veh, out var existing))
                return existing;
            var wrapper = new LandVehicle(veh);
            _cache[veh] = wrapper;
            return wrapper;
        }
    }
}



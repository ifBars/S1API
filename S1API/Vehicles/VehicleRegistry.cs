#if (IL2CPPMELON)
using Il2Cpp;
using S1Vehicles = Il2CppScheduleOne.Vehicles;
#elif (MONOMELON || MONOBEPINEX || IL2CPPBEPINEX)
using S1Vehicles = ScheduleOne.Vehicles;
#endif
using System;
using System.Collections.Generic;
using System.Linq;

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



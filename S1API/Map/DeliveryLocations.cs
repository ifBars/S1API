#if (IL2CPPMELON)
using S1Economy = Il2CppScheduleOne.Economy;
#elif (MONOMELON || MONOBEPINEX || IL2CPPBEPINEX)
using S1Economy = ScheduleOne.Economy;
#endif

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace S1API.Map
{
    /// <summary>
    /// Provides lookup and enumeration of delivery locations.
    /// Mirrors the simplicity of NPC.Get<T>() with name-based helpers.
    /// </summary>
    public static class DeliveryLocations
    {
        private static readonly List<DeliveryLocation> _all = new List<DeliveryLocation>();

        /// <summary>
        /// All delivery locations currently known in the scene.
        /// Populated on scene load and kept in sync when scenes change.
        /// </summary>
        public static IReadOnlyList<DeliveryLocation> All => _all;

        /// <summary>
        /// Returns a delivery location by case-insensitive name match.
        /// </summary>
        public static DeliveryLocation GetByName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return null;
            return _all.FirstOrDefault(l => string.Equals(l?.Name, name, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Returns a delivery location by GUID string.
        /// </summary>
        public static DeliveryLocation GetByGuid(string guid)
        {
            if (string.IsNullOrEmpty(guid))
                return null;
            return _all.FirstOrDefault(l => string.Equals(l?.GUID, guid, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// INTERNAL: Registers a delivery location with the S1API system.
        /// Called automatically by Harmony patches when delivery locations are created.
        /// </summary>
        /// <param name="gameDeliveryLocation">The game's DeliveryLocation instance.</param>
        internal static void Register(object gameDeliveryLocation)
        {
            if (gameDeliveryLocation == null)
                return;
            try
            {
                var type = gameDeliveryLocation.GetType();
                var guidProp = type.GetProperty("GUID", BindingFlags.Public | BindingFlags.Instance);
                var rawGuid = guidProp?.GetValue(gameDeliveryLocation);
                // Support both System.Guid and Il2CppSystem.Guid by using ToString()
                string guid = rawGuid?.ToString() ?? string.Empty;
                if (string.IsNullOrEmpty(guid)) return;
                if (_all.Any(l => string.Equals(l?.GUID, guid, StringComparison.OrdinalIgnoreCase)))
                    return;
                _all.Add(new DeliveryLocation((S1Economy.DeliveryLocation)gameDeliveryLocation));
            }
            catch { }
        }

        /// <summary>
        /// INTERNAL: Unregisters a delivery location from the S1API system.
        /// Called automatically by Harmony patches when delivery locations are destroyed.
        /// </summary>
        /// <param name="gameDeliveryLocation">The game's DeliveryLocation instance.</param>
        internal static void Unregister(object gameDeliveryLocation)
        {
            if (gameDeliveryLocation == null)
                return;
            try
            {
                for (int i = _all.Count - 1; i >= 0; i--)
                {
                    if (ReferenceEquals(_all[i]?.S1Location, gameDeliveryLocation))
                        _all.RemoveAt(i);
                }
            }
            catch { }
        }

        /// <summary>
        /// INTERNAL: Clears all registered delivery locations. Used during scene cleanup.
        /// </summary>
        internal static void Clear()
        {
            _all.Clear();
        }
    }
}



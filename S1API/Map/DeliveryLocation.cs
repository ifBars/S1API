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
    /// Wrapper for a base-game delivery location.
    /// </summary>
    public sealed class DeliveryLocation
    {
        // Registry (name-based)
        internal static readonly System.Collections.Generic.List<DeliveryLocation> All = new System.Collections.Generic.List<DeliveryLocation>();

        internal readonly S1Economy.DeliveryLocation S1Location;

        internal DeliveryLocation(S1Economy.DeliveryLocation s1)
        {
            S1Location = s1;
        }

        /// <summary>
        /// Location display name.
        /// </summary>
        public string Name =>
            S1Location != null ? S1Location.LocationName : string.Empty;

        /// <summary>
        /// Human-readable description for UI.
        /// </summary>
        public string Description =>
            S1Location != null ? S1Location.LocationDescription : string.Empty;

        /// <summary>
        /// GUID string identifier used by the base game.
        /// </summary>
        public string GUID
        {
            get
            {
                try
                {
                    return S1Location != null ? S1Location.GUID.ToString() : string.Empty;
                }
                catch
                {
                    return string.Empty;
                }
            }
        }

        /// <summary>
        /// Customer standing position.
        /// </summary>
        public Transform CustomerStandPoint =>
            S1Location != null ? S1Location.CustomerStandPoint : null;

        /// <summary>
        /// Teleport target point near the location.
        /// </summary>
        public Transform TeleportPoint =>
            S1Location != null ? S1Location.TeleportPoint : null;

        /// <summary>
        /// Returns all known delivery locations (sorted by name).
        /// </summary>
        public static DeliveryLocation[] GetAll()
        {
            return All.OrderBy(l => l.Name).ToArray();
        }

        /// <summary>
        /// Returns a delivery location by case-insensitive name match.
        /// </summary>
        public static DeliveryLocation GetByName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return null;
            return All.FirstOrDefault(l => string.Equals(l?.Name, name, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Returns a delivery location by GUID string.
        /// </summary>
        public static DeliveryLocation GetByGuid(string guid)
        {
            if (string.IsNullOrEmpty(guid))
                return null;
            return All.FirstOrDefault(l => string.Equals(l?.GUID, guid, StringComparison.OrdinalIgnoreCase));
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
                if (All.Any(l => string.Equals(l?.GUID, guid, StringComparison.OrdinalIgnoreCase)))
                    return;
                All.Add(new DeliveryLocation((S1Economy.DeliveryLocation)gameDeliveryLocation));
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
                for (int i = All.Count - 1; i >= 0; i--)
                {
                    if (ReferenceEquals(All[i]?.S1Location, gameDeliveryLocation))
                        All.RemoveAt(i);
                }
            }
            catch { }
        }

        /// <summary>
        /// INTERNAL: Clears all registered delivery locations. Used during scene cleanup.
        /// </summary>
        internal static void Clear()
        {
            All.Clear();
        }

        /// <summary>
        /// Resolves a delivery location using a typed identifier T.
        /// Declare an identifier class annotated with [DeliveryLocations.DeliveryLocationName("...")].
        /// </summary>
        public static DeliveryLocation Get<T>() where T : DeliveryLocations.IDeliveryLocationIdentifier
        {
            var t = typeof(T);
            string name = TryGetNameFromIdentifier(t);
            if (!string.IsNullOrEmpty(name))
                return GetByName(name);
            // Fallback: try the type name itself
            return GetByName(t.Name);
        }

        private static string TryGetNameFromIdentifier(Type t)
        {
            try
            {
                var attr = t.GetCustomAttributes(false).FirstOrDefault(a => a.GetType().FullName == typeof(DeliveryLocations.DeliveryLocationNameAttribute).FullName);
                if (attr != null)
                {
                    var prop = attr.GetType().GetProperty("Name", BindingFlags.Public | BindingFlags.Instance);
                    return prop?.GetValue(attr) as string;
                }
            }
            catch { }
            return null;
        }
    }
}



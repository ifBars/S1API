using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MelonLoader;
#if MONOMELON
using ScheduleOne.Map;
#else
using Il2Cpp;
using Il2CppScheduleOne.Map;
#endif
using UnityEngine;

namespace S1API.Map
{
    /// <summary>
    /// Registry utilities for finding enterable buildings.
    /// </summary>
    public static class Buildings
    {
        /// <summary>
        /// All currently active buildings tracked by S1API.
        /// </summary>
        public static readonly List<Building> All = new List<Building>();


        /// <summary>
        /// INTERNAL: Registers a building with the S1API system.
        /// Called automatically by Harmony patches when buildings are created.
        /// </summary>
        /// <param name="gameBuilding">The game's NPCEnterableBuilding instance.</param>
        internal static void Register(object gameBuilding)
        {
            if (gameBuilding == null)
                return;
            try
            {
                var type = gameBuilding.GetType();
                var guidProp = type.GetProperty("GUID", BindingFlags.Public | BindingFlags.Instance);
                var rawGuid = guidProp?.GetValue(gameBuilding);
                // Support both System.Guid and Il2CppSystem.Guid by using ToString()
				string guid = rawGuid?.ToString() ?? string.Empty;
				if (string.IsNullOrEmpty(guid)) return;
				// Skip Guid.Empty or obviously invalid values
				if (System.Guid.TryParse(guid, out var parsed) && parsed == System.Guid.Empty)
					return;
				if (string.Equals(guid, "00000000-0000-0000-0000-000000000000", StringComparison.OrdinalIgnoreCase))
					return;
                if (All.Any(b => string.Equals(b.GUID, guid, StringComparison.OrdinalIgnoreCase)))
                    return;
                All.Add(new Building(guid, gameBuilding));
            }
            catch { }
        }

        /// <summary>
        /// INTERNAL: Unregisters a building from the S1API system.
        /// Called automatically by Harmony patches when buildings are destroyed.
        /// </summary>
        /// <param name="gameBuilding">The game's NPCEnterableBuilding instance.</param>
        internal static void Unregister(object gameBuilding)
        {
            if (gameBuilding == null)
                return;
            try
            {
                for (int i = All.Count - 1; i >= 0; i--)
                {
                    if (ReferenceEquals(All[i]?._gameBuilding, gameBuilding))
                        All.RemoveAt(i);
                }
            }
            catch { }
        }

        /// <summary>
        /// Returns all enterable buildings currently available in the project.
        /// </summary>
        public static Building[] GetAll()
        {
            return All.OrderBy(b => b.Name).ToArray();
        }

        /// <summary>
        /// Finds a building by GUID. Returns null if not found.
        /// </summary>
        public static Building GetByGUID(string guid)
        {
            if (string.IsNullOrEmpty(guid)) return null;
            return GetAll().FirstOrDefault(b => string.Equals(b.GUID, guid, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Finds a building by display name. Returns the first match, or null.
        /// </summary>
        public static Building GetByName(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            return GetAll().FirstOrDefault(b => string.Equals(b.Name, name, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// INTERNAL: Clears all registered buildings. Used during scene cleanup.
        /// </summary>
        internal static void Clear()
        {
            All.Clear();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
    /// Modder-facing wrapper for an enterable building in the world.
    /// Provides stable GUID and basic metadata without exposing game types.
    /// </summary>
    public sealed class Building
    {
        internal readonly string _guid;
        internal object _gameBuilding;

        internal Building(string guid, object gameBuilding)
        {
            _guid = guid;
            _gameBuilding = gameBuilding;
        }

        /// <summary>
        /// Unique GUID string identifier for this building.
        /// </summary>
        public string GUID => _guid;

        /// <summary>
        /// Display name of the building, if available.
        /// </summary>
        public string Name
        {
            get
            {
                try
                {
                    var go = ResolveGameBuilding();
                    if (go == null) return string.Empty;
                    var nameField = go.GetType().GetField("BuildingName", BindingFlags.Public | BindingFlags.Instance);
                    return nameField?.GetValue(go) as string ?? string.Empty;
                }
                catch { return string.Empty; }
            }
        }

        internal object ResolveGameBuilding()
        {
            if (_gameBuilding != null)
                return _gameBuilding;
            try
            {
#if MONOMELON
                var guid = new Guid(_guid);
#else
                var guid = new Il2CppSystem.Guid(_guid);
#endif
                // Assign the resolved object so subsequent calls are fast
                _gameBuilding = GUIDManager.GetObject<NPCEnterableBuilding>(guid);
            }
            catch { /* ignore */ }
            return _gameBuilding;
        }
    }

    /// <summary>
    /// Registry utilities for finding enterable buildings.
    /// </summary>
    public static class Buildings
    {
        /// <summary>
        /// All currently active buildings tracked by S1API.
        /// </summary>
        public static readonly List<Building> All = new List<Building>();

        private static bool _attemptedDiscovery = false;

        /// <summary>
        /// Attempts a lazy scene discovery of all NPCEnterableBuilding instances
        /// when the registry is still empty (e.g. early lifecycle like ConfigurePrefab).
        /// Safe to call multiple times; will only execute once.
        /// </summary>
        private static void TryLazyDiscover()
        {
            if (_attemptedDiscovery)
                return;
            _attemptedDiscovery = true;
            try
            {
                // Includes inactive objects as well
                var found = Resources.FindObjectsOfTypeAll<NPCEnterableBuilding>();
                if (found == null || found.Length == 0)
                    return;
                foreach (var b in found)
                {
                    Register(b);
                }
            }
            catch
            {
                // Best-effort only
            }
        }

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
                if (All.Any(b => string.Equals(b.GUID, guid, StringComparison.OrdinalIgnoreCase)))
                    return;
                All.Add(new Building(guid, gameBuilding));
            }
            catch { }
        }

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
        /// Returns all enterable buildings currently available in the project (includes inactive).
        /// </summary>
        public static Building[] GetAll()
        {
            if (All.Count == 0)
            {
                TryLazyDiscover();
                if (All.Count == 0)
                    return Array.Empty<Building>();
            }
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
    }
}



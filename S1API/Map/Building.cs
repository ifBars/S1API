using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace S1API.Map
{
    /// <summary>
    /// Modder-facing wrapper for an enterable building in the world.
    /// Provides name-based lookup and basic metadata without exposing game types.
    /// </summary>
    public sealed class Building
    {
        // Registry (name-based)
        internal static readonly List<Building> All = new List<Building>();

        internal readonly string _name;
        internal object _gameBuilding;

        internal Building(string name, object gameBuilding)
        {
            _name = name;
            _gameBuilding = gameBuilding;
        }

        /// <summary>
        /// Display name of the building.
        /// </summary>
        public string Name => _name;

        /// <summary>
        /// Returns the underlying game building object, resolving if needed.
        /// </summary>
        internal object ResolveGameBuilding()
        {
            if (_gameBuilding != null)
                return _gameBuilding;

            try
            {
                // Fallback: try to find by name in-scene
#if (IL2CPPMELON || IL2CPPBEPINEX)
                var arr = UnityEngine.Object.FindObjectsOfType<Il2CppScheduleOne.Map.NPCEnterableBuilding>(includeInactive: true);
#elif (MONOMELON || MONOBEPINEX)
                var arr = UnityEngine.Object.FindObjectsOfType<ScheduleOne.Map.NPCEnterableBuilding>(true);
#else
                var arr = Array.Empty<UnityEngine.Object>();
#endif
                for (int i = 0; i < arr.Length; i++)
                {
                    var b = arr[i];
                    if (b == null) continue;
                    var type = b.GetType();
                    var nameField = type.GetField("BuildingName", BindingFlags.Public | BindingFlags.Instance);
                    string name = nameField?.GetValue(b) as string;
                    if (!string.IsNullOrEmpty(name) && string.Equals(name, _name, StringComparison.OrdinalIgnoreCase))
                    {
                        _gameBuilding = b;
                        break;
                    }
                }
            }
            catch { /* ignore */ }

            return _gameBuilding;
        }

        // Static API

        /// <summary>
        /// INTERNAL: Registers a building by its display name.
        /// Called automatically by Harmony patches when buildings are created.
        /// </summary>
        internal static void Register(object gameBuilding)
        {
            if (gameBuilding == null)
                return;
            try
            {
                var type = gameBuilding.GetType();
                var nameField = type.GetField("BuildingName", BindingFlags.Public | BindingFlags.Instance);
                var nameProp = type.GetProperty("BuildingName", BindingFlags.Public | BindingFlags.Instance);
                string name = nameField?.GetValue(gameBuilding) as string;
                if (string.IsNullOrEmpty(name))
                    name = nameProp?.GetValue(gameBuilding) as string;
                if (string.IsNullOrEmpty(name))
                    return;

                // Update existing or add new
                var existing = All.FirstOrDefault(b => b != null && string.Equals(b._name, name, StringComparison.OrdinalIgnoreCase));
                if (existing != null)
                {
                    existing._gameBuilding = gameBuilding;
                    return;
                }

                All.Add(new Building(name, gameBuilding));
            }
            catch { }
        }

        /// <summary>
        /// INTERNAL: Unregisters a building instance.
        /// Called automatically by Harmony patches when buildings are destroyed.
        /// </summary>
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
        /// Returns all known buildings (sorted by name).
        /// </summary>
        public static Building[] GetAll()
        {
            return All.OrderBy(b => b.Name).ToArray();
        }

        /// <summary>
        /// Returns the first building with the provided display name, or null.
        /// </summary>
        public static Building GetByName(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            return GetAll().FirstOrDefault(b => string.Equals(b.Name, name, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Resolves a building using a typed identifier T.
        /// Declare an identifier class annotated with [Buildings.BuildingName("...")].
        /// </summary>
        public static Building Get<T>() where T : S1API.Map.Buildings.IBuildingIdentifier
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
                var attr = t.GetCustomAttributes(false).FirstOrDefault(a => a.GetType().FullName == typeof(S1API.Map.Buildings.BuildingNameAttribute).FullName);
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

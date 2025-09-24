using System;
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
					var type = go.GetType();
					// Try public field first (as in base game)
					var nameField = type.GetField("BuildingName", BindingFlags.Public | BindingFlags.Instance);
					string name = nameField?.GetValue(go) as string;
					if (!string.IsNullOrEmpty(name))
						return name;
					// Try as a property in case IL2CPP emits it differently
					var nameProp = type.GetProperty("BuildingName", BindingFlags.Public | BindingFlags.Instance);
					name = nameProp?.GetValue(go) as string;
					if (!string.IsNullOrEmpty(name))
						return name;
					// Fallback: use UnityEngine.Object.name via reflection
					var unityNameProp = type.GetProperty("name", BindingFlags.Public | BindingFlags.Instance);
					name = unityNameProp?.GetValue(go) as string;
					return name ?? string.Empty;
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
}

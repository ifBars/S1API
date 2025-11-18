using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using S1API.Internal.Map;
using S1API.Entities.Schedule;
using UnityEngine.SceneManagement;

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

        private string _name;
        internal object _gameBuilding;
        private bool _isDeferred;
        private Type _deferredIdentifierType;

        internal Building(string name, object gameBuilding)
        {
            _name = name;
            _gameBuilding = gameBuilding;
            _isDeferred = false;
        }

        /// <summary>
        /// Creates a deferred building wrapper that will be resolved later.
        /// </summary>
        internal Building(Type identifierType, string name)
        {
            _name = name;
            _gameBuilding = null;
            _isDeferred = true;
            _deferredIdentifierType = identifierType;
        }

        /// <summary>
        /// Display name of the building.
        /// </summary>
        public string Name => _name;

        /// <summary>
        /// INTERNAL: Gets the deferred identifier type if this building is deferred, otherwise null.
        /// </summary>
        internal Type DeferredIdentifierType => _isDeferred ? _deferredIdentifierType : null;

        /// <summary>
        /// INTERNAL: Whether this building wrapper is deferred and not yet resolved.
        /// </summary>
        internal bool IsDeferred => _isDeferred;

        /// <summary>
        /// Returns the underlying game building object, resolving if needed.
        /// </summary>
        internal object ResolveGameBuilding()
        {
            if (_gameBuilding != null)
                return _gameBuilding;

            // Try to resolve if deferred
            if (_isDeferred)
            {
                if (_deferredIdentifierType != null)
                {
                    var resolved = TryResolveDeferred(_deferredIdentifierType);
                    if (resolved != null)
                    {
                        _gameBuilding = resolved._gameBuilding;
                        _name = resolved._name;
                        _isDeferred = false;
                        return _gameBuilding;
                    }
                }
                else if (!string.IsNullOrEmpty(_name))
                {
                    var resolved = GetByName(_name);
                    if (resolved != null && !resolved._isDeferred)
                    {
                        _gameBuilding = resolved._gameBuilding;
                        _isDeferred = false;
                        return _gameBuilding;
                    }
                }
            }

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

        private static Building TryResolveDeferred(Type identifierType)
        {
            try
            {
                var name = TryGetNameFromIdentifier(identifierType);
                if (!string.IsNullOrEmpty(name))
                {
                    return GetByName(name);
                }
                return GetByName(identifierType.Name);
            }
            catch
            {
                return null;
            }
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
                    existing._isDeferred = false;
                    
                    // Notify pending schedule actions that this building is now available
                    TryResolvePendingScheduleActions(name, gameBuilding);
                    return;
                }

                var newBuilding = new Building(name, gameBuilding);
                All.Add(newBuilding);
                
                // Notify pending schedule actions that this building is now available
                TryResolvePendingScheduleActions(name, gameBuilding);
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
            
            var found = GetAll().FirstOrDefault(b => string.Equals(b.Name, name, StringComparison.OrdinalIgnoreCase));
            if (found != null)
                return found;

            // If not found and we're in Menu scene, create deferred wrapper
            if (DeferredMapResolver.IsMenuScene())
            {
                var deferredWrapper = new Building(typeof(object), name);
                DeferredMapResolver.RegisterDeferredLookup(new DeferredLookup(name, (resolved) =>
                {
                    if (resolved is Building building && building != null && building._gameBuilding != null)
                    {
                        deferredWrapper._gameBuilding = building._gameBuilding;
                        deferredWrapper._name = building._name;
                        deferredWrapper._isDeferred = false;
                    }
                }));
                return deferredWrapper;
            }

            return null;
        }

        /// <summary>
        /// Resolves a building using a typed identifier T.
        /// Declare an identifier class annotated with [Buildings.BuildingName("...")].
        /// </summary>
        public static Building Get<T>() where T : Buildings.IBuildingIdentifier
        {
            var t = typeof(T);
            string name = TryGetNameFromIdentifier(t);
            if (!string.IsNullOrEmpty(name))
            {
                var found = GetByName(name);
                if (found != null)
                    return found;
            }

            // Try type name as fallback
            var byTypeName = GetByName(t.Name);
            if (byTypeName != null)
                return byTypeName;

            // If still not found and we're in Menu scene, create deferred wrapper
            if (DeferredMapResolver.IsMenuScene())
            {
                string deferredName = !string.IsNullOrEmpty(name) ? name : t.Name;
                var deferredWrapper = new Building(t, deferredName);
                DeferredMapResolver.RegisterDeferredLookup(new DeferredLookup(t, (resolved) =>
                {
                    if (resolved is Building building && building != null && building._gameBuilding != null)
                    {
                        deferredWrapper._gameBuilding = building._gameBuilding;
                        deferredWrapper._name = building._name;
                        deferredWrapper._isDeferred = false;
                    }
                }));
                return deferredWrapper;
            }

            return null;
        }

        private static string TryGetNameFromIdentifier(Type t)
        {
            try
            {
                var attr = t.GetCustomAttributes(false).FirstOrDefault(a => a.GetType().FullName == typeof(Buildings.BuildingNameAttribute).FullName);
                if (attr != null)
                {
                    var prop = attr.GetType().GetProperty("Name", BindingFlags.Public | BindingFlags.Instance);
                    return prop?.GetValue(attr) as string;
                }
            }
            catch { }
            return null;
        }

        /// <summary>
        /// INTERNAL: Attempts to resolve pending schedule actions that are waiting for this building.
        /// </summary>
        private static void TryResolvePendingScheduleActions(string buildingName, object gameBuilding)
        {
            try
            {
                StayInBuildingSpec.TryResolvePendingActions();
            }
            catch
            {
                // Silently ignore - this is best-effort
            }
        }
    }
}

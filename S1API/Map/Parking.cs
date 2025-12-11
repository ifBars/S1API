#if MONOMELON
using ScheduleOne.Map;
using ScheduleOne.Vehicles;
#else
using Il2Cpp;
using Il2CppScheduleOne.Map;
using Il2CppScheduleOne.Vehicles;
#endif
using System;
using System.Linq;
using S1API.Internal.Map;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using GameObject = UnityEngine.GameObject;
using Object = UnityEngine.Object;
using S1API.Vehicles;
using UnityEngine;


namespace S1API.Map
{
    /// <summary>
    /// Modder-facing wrapper for a parking lot.
    /// </summary>
    public sealed class ParkingLotWrapper
    {
        internal string _guid;
        internal string _gameObjectName;
        internal ParkingLot _lot;
        internal bool _isDeferred;
        private Type _deferredIdentifierType;

        internal ParkingLotWrapper(ParkingLot lot)
        {
            _lot = lot;
            _guid = lot.GUID.ToString();
            _gameObjectName = lot.gameObject.name;
            _isDeferred = false;

        }

        /// <summary>
        /// Creates a deferred parking lot wrapper that will be resolved later.
        /// </summary>
        internal ParkingLotWrapper(Type identifierType, string name)
        {
            _guid = string.Empty;
            _gameObjectName = name;
            _lot = null;
            _isDeferred = true;
            _deferredIdentifierType = identifierType;
        }

        /// <summary>
        /// Unique GUID string identifier for this lot.
        /// </summary>
        public string GUID => _guid;

        /// <summary>
        /// GameObject name of the parking lot.
        /// </summary>
        public string GameObjectName => _gameObjectName;

        /// <summary>
        /// Optional entry point world position, if configured.
        /// </summary>
        public UnityEngine.Vector3? EntryPointPosition => _lot?.EntryPoint != null ? _lot.EntryPoint.position : (UnityEngine.Vector3?)null;

        /// <summary>
        /// Total amount of parking spots are in this lot, available or otherwise
        /// </summary>
        public int ParkingSpotsCount => _lot == null ? 0 : _lot.ParkingSpots.Count;
        /// <summary>
        /// Gets the parking spot at the specified index, does not validate the index
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public ParkingSpotWrapper GetSpot(int index) => new ParkingSpotWrapper(_lot.ParkingSpots[index]);

        internal ParkingLot ResolveGameLot()
        {
            if (_isDeferred && _lot == null)
            {
                // Try to resolve now that Main scene might be loaded
                if (_deferredIdentifierType != null)
                {
                    var resolved = TryResolveDeferred(_deferredIdentifierType);
                    if (resolved != null)
                    {
                        _lot = resolved._lot;
                        _isDeferred = false;
                        return _lot;
                    }
                }
                else if (!string.IsNullOrEmpty(_gameObjectName))
                {
                    var resolved = ParkingLotRegistry.GetByName(_gameObjectName);
                    if (resolved != null && !resolved._isDeferred)
                    {
                        _lot = resolved._lot;
                        _isDeferred = false;
                        return _lot;
                    }
                }
            }
            return _lot;
        }

        private static ParkingLotWrapper TryResolveDeferred(Type identifierType)
        {
            try
            {
                var name = TryGetNameFromIdentifier(identifierType);
                if (!string.IsNullOrEmpty(name))
                {
                    return ParkingLotRegistry.GetByName(name);
                }
                return ParkingLotRegistry.GetByName(identifierType.Name);
            }
            catch
            {
                return null;
            }
        }

        private static string TryGetNameFromIdentifier(Type t)
        {
            try
            {
                var attr = t.GetCustomAttributes(false).FirstOrDefault(a => a.GetType().FullName == typeof(ParkingLotNameAttribute).FullName);
                if (attr != null)
                {
                    var prop = attr.GetType().GetProperty("Name", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                    return prop?.GetValue(attr) as string;
                }
            }
            catch { }
            return null;
        }
    }

    /// <summary>
    /// Modder-facing wrapper for a parking spot within a lot.
    /// </summary>
    public sealed class ParkingSpotWrapper
    {
        internal readonly ParkingSpot _spot;

        internal ParkingSpotWrapper(ParkingSpot spot) => _spot = spot;

        /// <summary>
        /// True if the spot currently has no vehicle occupant.
        /// </summary>
        public bool IsFree => _spot != null && _spot.OccupantVehicle == null;

        /// <summary>
        /// Alignment enum for this spot.
        /// </summary>
        public Vehicles.ParkingAlignment Alignment => (Vehicles.ParkingAlignment)_spot.Alignment;

        ///<summary>
        /// Alignment Point for Occupant Vehicle
        ///</summary>
        public Transform AlignmentPoint => _spot.AlignmentPoint;

        internal Vehicles.LandVehicle? _OccupantVehicle => new Vehicles.LandVehicle(_spot.OccupantVehicle);
        /// <summary>
        /// LandVehicle currently occupying this spot
        /// </summary>
        public Vehicles.LandVehicle? OccupantVehicle { get { return _OccupantVehicle; } }

        /// <summary>
        /// Sets the vehicle currently occupying this spot
        /// </summary>
        /// <param name="vehicle"></param>
        public void SetOccupant(Vehicles.LandVehicle vehicle) => _spot.SetOccupant(vehicle.S1LandVehicle); 
    }

    /// <summary>
    /// Registry and utilities for parking-related queries.
    /// </summary>
    public static class ParkingLotRegistry
    {
        // Registry (automatically populated by Harmony patches)
        internal static readonly List<ParkingLotWrapper> All = new List<ParkingLotWrapper>();
        /// <summary>
        /// Returns all lots currently registered.
        /// </summary>
        public static ParkingLotWrapper[] GetAll()
        {
            return All.OrderBy(l => l.GameObjectName).ToArray();
        }

        /// <summary>
        /// Finds a lot by GUID string. Returns null if not found.
        /// </summary>
        public static ParkingLotWrapper GetByGUID(string guid)
        {
            if (string.IsNullOrEmpty(guid))
                return null;
            return All.FirstOrDefault(l => string.Equals(l?.GUID, guid, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Finds a parking lot by GameObject name.
        /// </summary>
        /// <param name="gameObjectName">The name of the GameObject containing the ParkingLot component.</param>
        /// <returns>A parking lot wrapper, or null if not found.</returns>
        public static ParkingLotWrapper GetByName(string gameObjectName)
        {
            if (string.IsNullOrEmpty(gameObjectName))
                return null;

            // First try to find in registry
            var found = All.FirstOrDefault(l => string.Equals(l?.GameObjectName, gameObjectName, StringComparison.OrdinalIgnoreCase));
            if (found != null)
                return found;

            // If not found and we're in Menu scene, create a deferred wrapper
            if (DeferredMapResolver.IsMenuScene())
            {
                var deferredWrapper = new ParkingLotWrapper(typeof(object), gameObjectName);
                DeferredMapResolver.RegisterDeferredLookup(new DeferredLookup(gameObjectName, (resolved) =>
                {
                    if (resolved is ParkingLotWrapper wrapper && wrapper != null && wrapper._lot != null)
                    {
                        deferredWrapper._lot = wrapper._lot;
                        deferredWrapper._guid = wrapper._guid;
                        deferredWrapper._gameObjectName = wrapper._gameObjectName;
                        deferredWrapper._isDeferred = false;
                    }
                }));
                return deferredWrapper;
            }

            return null;
        }

        /// <summary>
        /// Finds free spots in a lot, by GUID.
        /// </summary>
        public static ParkingSpotWrapper[] GetFreeSpots(string lotGuid)
        {
            var lot = GetByGUID(lotGuid);
            if (lot == null)
                return Array.Empty<ParkingSpotWrapper>();
            try
            {
                var spots = lot.ResolveGameLot().GetFreeParkingSpots();
                var results = new List<ParkingSpotWrapper>();
                foreach (var spot in spots)
                {
                    results.Add(new ParkingSpotWrapper(spot));
                }
                return results.ToArray();
            }
            catch
            {
                return Array.Empty<ParkingSpotWrapper>();
            }
        }

        /// <summary>
        /// Finds free spots in a lot, by GameObject name.
        /// </summary>
        public static ParkingSpotWrapper[] GetFreeSpotsByName(string lotGameObjectName)
        {
            var lot = GetByName(lotGameObjectName);
            if (lot == null)
                return Array.Empty<ParkingSpotWrapper>();
            try
            {
                var spots = lot.ResolveGameLot().GetFreeParkingSpots();
                var results = new List<ParkingSpotWrapper>();
                foreach (var spot in spots)
                {
                    results.Add(new ParkingSpotWrapper(spot));
                }
                return results.ToArray();
            }
            catch
            {
                return Array.Empty<ParkingSpotWrapper>();
            }
        }

        /// <summary>
        /// INTERNAL: Registers a parking lot with the S1API system.
        /// Called automatically by Harmony patches when parking lots are created.
        /// </summary>
        /// <param name="gameParkingLot">The game's ParkingLot instance.</param>
        internal static void Register(object gameParkingLot)
        {
            if (gameParkingLot == null)
                return;
            try
            {
                var lot = (ParkingLot)gameParkingLot;
                var guid = lot.GUID.ToString();
                // Avoid duplicates
                if (All.Any(l => string.Equals(l?.GUID, guid, StringComparison.OrdinalIgnoreCase)))
                    return;
                All.Add(new ParkingLotWrapper(lot));
            }
            catch { }
        }

        /// <summary>
        /// INTERNAL: Unregisters a parking lot from the S1API system.
        /// Called automatically by Harmony patches when parking lots are destroyed.
        /// </summary>
        /// <param name="gameParkingLot">The game's ParkingLot instance.</param>
        internal static void Unregister(object gameParkingLot)
        {
            if (gameParkingLot == null)
                return;
            try
            {
                for (int i = All.Count - 1; i >= 0; i--)
                {
                    if (ReferenceEquals(All[i]?._lot, gameParkingLot))
                        All.RemoveAt(i);
                }
            }
            catch { }
        }

        /// <summary>
        /// Resolves a parking lot using a typed identifier T.
        /// Declare an identifier class annotated with [ParkingLotName("...")].
        /// </summary>
        /// <typeparam name="T">A type implementing IParkingLotIdentifier with ParkingLotNameAttribute</typeparam>
        /// <returns>The parking lot wrapper, or null if not found.</returns>
        public static ParkingLotWrapper Get<T>() where T : IParkingLotIdentifier
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
                var deferredWrapper = new ParkingLotWrapper(t, deferredName);
                DeferredMapResolver.RegisterDeferredLookup(new DeferredLookup(t, (resolved) =>
                {
                    if (resolved is ParkingLotWrapper wrapper && wrapper != null && wrapper._lot != null)
                    {
                        deferredWrapper._lot = wrapper._lot;
                        deferredWrapper._guid = wrapper._guid;
                        deferredWrapper._gameObjectName = wrapper._gameObjectName;
                        deferredWrapper._isDeferred = false;
                    }
                }));
                return deferredWrapper;
            }

            return null;
        }

        /// <summary>
        /// INTERNAL: Clears all registered parking lots. Used during scene cleanup.
        /// </summary>
        internal static void Clear()
        {
            All.Clear();
        }

        /// <summary>
        /// Helper method to extract the parking lot name from a type's ParkingLotNameAttribute.
        /// </summary>
        private static string TryGetNameFromIdentifier(System.Type t)
        {
            try
            {
                var attr = t.GetCustomAttributes(false).FirstOrDefault(a => a.GetType().FullName == typeof(ParkingLotNameAttribute).FullName);
                if (attr != null)
                {
                    var prop = attr.GetType().GetProperty("Name", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                    return prop?.GetValue(attr) as string;
                }
            }
            catch { }
            return null;
        }
    }
}



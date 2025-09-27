#if MONOMELON
using ScheduleOne.Map;
using ScheduleOne.Vehicles;
#else
using Il2Cpp;
using Il2CppScheduleOne.Map;
using Il2CppScheduleOne.Vehicles;
#endif
using System;
using System.Collections.Generic;
using System.Linq;

namespace S1API.Map
{
    /// <summary>
    /// Modder-facing wrapper for a parking lot.
    /// </summary>
    public sealed class ParkingLotWrapper
    {
        internal readonly string _guid;
        internal ParkingLot _lot;

        internal ParkingLotWrapper(ParkingLot lot)
        {
            _lot = lot;
            _guid = lot.GUID.ToString();
        }

        /// <summary>
        /// Unique GUID string identifier for this lot.
        /// </summary>
        public string GUID => _guid;

        /// <summary>
        /// Optional entry point world position, if configured.
        /// </summary>
        public UnityEngine.Vector3? EntryPointPosition => _lot?.EntryPoint != null ? _lot.EntryPoint.position : (UnityEngine.Vector3?)null;

        internal ParkingLot ResolveGameLot() => _lot;
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
    }

    /// <summary>
    /// Registry and utilities for parking-related queries.
    /// </summary>
    public static class ParkingLots
    {
        /// <summary>
        /// Returns all lots currently registered in GUIDManager.
        /// </summary>
        public static ParkingLotWrapper[] GetAll()
        {
            try
            {
                // There is no direct list; scan active components in the scene
                var all = UnityEngine.Object.FindObjectsOfType<ParkingLot>();
                if (all == null || all.Length == 0)
                    return Array.Empty<ParkingLotWrapper>();
                
                var results = new List<ParkingLotWrapper>();
                foreach (var lot in all)
                {
                    if (lot != null)
                        results.Add(new ParkingLotWrapper(lot));
                }
                return results.ToArray();
            }
            catch
            {
                return Array.Empty<ParkingLotWrapper>();
            }
        }

        /// <summary>
        /// Finds a lot by GUID string. Returns null if not found.
        /// </summary>
        public static ParkingLotWrapper GetByGUID(string guid)
        {
            if (string.IsNullOrEmpty(guid))
                return null;
            try
            {
#if MONOMELON
                var g = new System.Guid(guid);
#else
                var g = new Il2CppSystem.Guid(guid);
#endif
                var lot = GUIDManager.GetObject<ParkingLot>(g);
                if (lot == null)
                    return null;
                return new ParkingLotWrapper(lot);
            }
            catch
            {
                return null;
            }
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
    }
}



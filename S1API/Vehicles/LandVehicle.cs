#if (IL2CPPMELON)
using S1Vehicles = Il2CppScheduleOne.Vehicles;
using S1PlayerScripts = Il2CppScheduleOne.PlayerScripts;
using Il2Cpp;
using Il2CppFishNet;
using Il2CppFishNet.Connection;
using Guid = Il2CppSystem.Guid;
#elif MONOMELON
using S1Vehicles = ScheduleOne.Vehicles;
using S1PlayerScripts = ScheduleOne.PlayerScripts;
using FishNet;
using FishNet.Connection;
using Guid = System.Guid;
#endif
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using UnityEngine;
using S1API.Internal.Utils;
using S1API.Logging;
using S1API.Storages;

namespace S1API.Vehicles
{
    /// <summary>
    /// Represents a land vehicle in the game.
    /// </summary>
    public class LandVehicle
    {
        // Public members intended to be used by modders
        #region Public Members
        /// <summary>
        /// Creates a new LandVehicle instance.
        /// </summary>
        public LandVehicle(string vehicleCode)
        {
            var vehiclePrefab = S1Vehicles.VehicleManager.Instance.GetVehiclePrefab(vehicleCode);
            if (vehiclePrefab == null)
            {
                _logger.Error($"SpawnVehicle: '{vehicleCode}' is not a valid vehicle code!");
                return;
            }

            var component = UnityEngine.Object.Instantiate<GameObject>(vehiclePrefab.gameObject)
                .GetComponent<S1Vehicles.LandVehicle>();

            component.SetGUID(GUIDManager.GenerateUniqueGUID());
            S1Vehicles.VehicleManager.Instance.AllVehicles.Add(component);

            S1LandVehicle = component;
            SetConnection();
            UpdateGuidFromGame();
            _storage = new StorageInstance(component.Storage);
            VehicleRegistry.Register(S1LandVehicle, this);
        }

        /// <summary>
        /// Vehicle price.
        /// </summary>
        public float VehiclePrice
        {
            get => S1LandVehicle.VehiclePrice;
            set => ReflectionUtils.TrySetFieldOrProperty(S1LandVehicle, "vehiclePrice", value);
        }

        /// <summary>
        /// Vehicle's top speed.
        /// </summary>
        public float TopSpeed
        {
            get => S1LandVehicle.TopSpeed;
            set => S1LandVehicle.TopSpeed = value;
        }

        /// <summary>
        /// If the vehicle is owned by the player.
        /// </summary>
        public bool IsPlayerOwned
        {
            get => S1LandVehicle.IsPlayerOwned;
            set => SetIsPlayerOwned(value);
        }

        /// <summary>
        /// If this vehicle has any occupants
        /// </summary>
        public bool IsOccupied {
            get => S1LandVehicle.IsOccupied;
            set => S1LandVehicle.IsOccupied = value;
        }

        /// <summary>
        /// Gets the vehicle's seats in their native order.
        /// </summary>
        /// <remarks>
        /// The collection is read-only and its seat wrappers retain stable identities while this
        /// vehicle wrapper remains valid.
        /// </remarks>
        public IReadOnlyList<VehicleSeatInfo> Seats => GetSeats();

        /// <summary>
        /// When this vehicle has started
        /// </summary>
        public event Action OnVehicleStart {
            add => S1LandVehicle.onVehicleStart += value;
            remove => S1LandVehicle.onVehicleStart -= value;
        }
        /// <summary>
        /// When this vehicle has stopped
        /// </summary>
        public event Action OnVehicleStop {
            add => S1LandVehicle.onVehicleStop += value;
            remove => S1LandVehicle.onVehicleStop -= value;
        }

        /// <summary>
        /// When the handbrake has been applied
        /// </summary>
        public event Action OnHandbrakeApplied {
            add => S1LandVehicle.onHandbrakeApplied += value;
            remove => S1LandVehicle.onHandbrakeApplied -= value;
        }

        /// <summary>
        /// When this vehicle has collided with something
        /// </summary>
        public event Action<Collision> OnCollision {
            add => S1LandVehicle.onCollision += value;
            remove => S1LandVehicle.onCollision -= value;
        }

        /// <summary>
        /// Vehicle's color.
        /// </summary>
        public VehicleColor Color
        {
            get => (VehicleColor)S1LandVehicle.OwnedColor;
            set => SetColor(value);
        }

        /// <summary>
        /// Unique GUID string for this vehicle.
        /// </summary>
        public string GUID => _guid;

        /// <summary>
        /// Spawns the vehicle in the game world.
        /// </summary>
        /// <param name="position">Position in the world</param>
        /// <param name="rotation">Rotation of the vehicle</param>
        public void Spawn(Vector3 position, Quaternion rotation)
        {
            if (!InstanceFinder.IsServer)
            {
                _logger.Warning("Spawn can only be called on the server!");
                return;
            }

            if (S1LandVehicle == null)
                throw new Exception("Unable to spawn vehicle, S1LandVehicle is null!");

            S1LandVehicle.transform.position = position;
            S1LandVehicle.transform.rotation = rotation;
            S1Vehicles.VehicleManager.Instance.Spawn(S1LandVehicle.gameObject);
        }

        /// <summary>
        /// Aligns car to parking spot <see cref="Map.ParkingSpotWrapper"/>.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="type"></param>
        /// <param name="network"></param>
        /// <exception cref="Exception"></exception>
        public void AlignTo(Transform target, ParkingAlignment type, bool network = false) {
            if (S1LandVehicle == null)
                throw new Exception("Unable to align to position, S1LandVehicle is null!");
            S1LandVehicle.AlignTo(target, (S1Vehicles.EParkingAlignment)type, network);

        }

        /// <summary>
        /// Exit parking spot, and optionally, the parking lot
        /// </summary>
        /// <param name="moveToExitPoint"></param>
        public void ExitPark(bool moveToExitPoint = true) => S1LandVehicle.ExitPark(moveToExitPoint);

        /// <summary>
        /// Set this vehicle as visible or not
        /// </summary>
        /// <param name="vis"></param>
        public void SetVisible(bool vis) => S1LandVehicle.SetVisible(vis);

        /// <summary>
        /// Deletes the land vehicle
        /// </summary>
        public void DestroyVehichle() => S1LandVehicle.DestroyVehicle();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="col"></param>
        public void ApplyColor(VehicleColor col) => S1LandVehicle.ApplyColor((S1Vehicles.Modification.EVehicleColor)col);

        /// <summary>
        /// Parks the vehicle in the specified slot <seealso cref="Map.ParkingData"></seealso>
        /// </summary>
        /// <param name="parkData"></param>
        /// <param name="network"></param>
        public void Park(Map.ParkingData parkData, bool network) {

            var vanillaData = new S1Vehicles.ParkData() {
                lotGUID = Guid.Parse(parkData.LotId),
                spotIndex = parkData.Index,
                alignment = (S1Vehicles.EParkingAlignment)parkData.Alignment
            };

            S1LandVehicle.Park(_conn, vanillaData, network);
        }


        /// <summary>
        /// Trunk space
        /// </summary>
        public StorageInstance? Storage { get { return _storage; } }

        #endregion
        
        // Internal members used by S1API
        #region Internal Members

        /// <summary>
        /// INTERNAL: The stored reference to the land vehicle in-game (see <see cref="S1Vehicles.LandVehicle"/>).
        /// </summary>
        internal S1Vehicles.LandVehicle S1LandVehicle = null!;
        internal bool _isDeferredByName = false;
        internal StorageInstance? _storage;

        /// <summary>
        /// INTERNAL: Creates a LandVehicle instance from an in-game land vehicle instance.
        /// </summary>
        /// <param name="landVehicle">The in-game land vehicle instance.</param>
        internal LandVehicle(S1Vehicles.LandVehicle landVehicle)
        {
            S1LandVehicle = landVehicle;
            SetConnection();
            UpdateGuidFromGame();
            _storage = new StorageInstance(landVehicle.Storage);
            _isDeferredByName = false;
            VehicleRegistry.Register(S1LandVehicle, this);
        }

        /// <summary>
        /// INTERNAL: Creates a deferred LandVehicle for name-based lookup.
        /// </summary>
        internal LandVehicle(string vehicleName, bool isDeferred)
        {
            _isDeferredByName = isDeferred;
        }
        
        #endregion
        
        // Private members used by LandVehicle class
        #region Private Members

        /// <summary>
        /// Logger for the LandVehicle class.
        /// </summary>
        private static readonly Log _logger = new Log("S1API.LandVehicle");

        private IReadOnlyList<VehicleSeatInfo>? _seats;

        /// <summary>
        /// Connection to the player that owns the vehicle.
        /// </summary>
        private NetworkConnection? _conn;

        /// <summary>
        /// Cached GUID string for this vehicle.
        /// </summary>
        private string _guid = string.Empty;

        internal void NotifySeatOccupantChanged(
            int seatIndex,
            S1PlayerScripts.Player? previous,
            S1PlayerScripts.Player? current)
        {
            if (VehicleSeatInfo.HasSameNativeIdentity(previous, current))
                return;

            var seats = GetSeats();
            if (seatIndex < 0 || seatIndex >= seats.Count)
                return;

            seats[seatIndex].NotifyOccupantChanged(previous, current);
        }

        private IReadOnlyList<VehicleSeatInfo> GetSeats()
        {
            if (_seats != null)
                return _seats;

            var nativeSeats = S1LandVehicle?.Seats;
            if (nativeSeats == null)
                return Array.Empty<VehicleSeatInfo>();

            var seats = new VehicleSeatInfo[nativeSeats.Length];
            for (int i = 0; i < nativeSeats.Length; i++)
                seats[i] = new VehicleSeatInfo(i, nativeSeats[i]);

            _seats = new ReadOnlyCollection<VehicleSeatInfo>(seats);
            return _seats;
        }

        /// <summary>
        /// Sets the connection to the player that owns the vehicle.
        /// </summary>
        private void SetConnection()
        {
            var nm = InstanceFinder.NetworkManager;
            if (nm.IsClientOnly)
            {
                var tempConn = InstanceFinder.ClientManager.Connection;
                if (tempConn != null && tempConn.IsValid)
                    _conn = tempConn;
            }
            else if (nm.IsServerOnly || (nm.IsServer && !nm.IsClient))
            {
                var owner = S1LandVehicle.Owner;
                if (owner != null && owner.IsValid)
                    _conn = owner;
            }
        }

        /// <summary>
        /// Helper method to set the vehicle as player owned.
        /// </summary>
        /// <param name="isPlayerOwned">If true, sets vehicle as player owned</param>
        private void SetIsPlayerOwned(bool isPlayerOwned)
        {
            S1LandVehicle.SetIsPlayerOwned(_conn, isPlayerOwned);
            // make sure to add/remove the vehicle from the player owned vehicles list
            if (isPlayerOwned)
                S1Vehicles.VehicleManager.Instance.PlayerOwnedVehicles.Add(S1LandVehicle);
            else
                S1Vehicles.VehicleManager.Instance.PlayerOwnedVehicles.Remove(S1LandVehicle);
        }

        /// <summary>
        /// Helper method to set the vehicle color.
        /// </summary>
        /// <param name="color">Vehicle's color</param>
        private void SetColor(VehicleColor color)
        {
            var setOwnedColorMethod =
                typeof(S1Vehicles.LandVehicle).GetMethod("SetOwnedColor",
                    BindingFlags.Instance | BindingFlags.NonPublic);
            if (setOwnedColorMethod == null)
            {
                _logger.Error("SetOwnedColor method not found!");
                return;
            }

            setOwnedColorMethod.Invoke(S1LandVehicle, [_conn, (S1Vehicles.Modification.EVehicleColor)color]);
        }

        /// <summary>
        /// Refreshes the cached GUID from the underlying game object.
        /// </summary>
        private void UpdateGuidFromGame()
        {
            try
            {
                var guidProp = typeof(S1Vehicles.LandVehicle).GetProperty("GUID", BindingFlags.Public | BindingFlags.Instance);
                if (guidProp == null)
                    return;
#if MONOMELON
                var g = (System.Guid)(guidProp.GetValue(S1LandVehicle) ?? System.Guid.Empty);
                _guid = g.ToString();
#else
                var g = (Il2CppSystem.Guid)(guidProp.GetValue(S1LandVehicle) ?? new Il2CppSystem.Guid());
                _guid = g.ToString();
#endif
            }
            catch
            {
                // ignore
            }
        }
        #endregion
    }
}

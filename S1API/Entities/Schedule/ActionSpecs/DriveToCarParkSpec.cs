#if (IL2CPPMELON)
using Il2Cpp;
using S1NPCs = Il2CppScheduleOne.NPCs;
using S1NPCsSchedules = Il2CppScheduleOne.NPCs.Schedules;
using S1Map = Il2CppScheduleOne.Map;
using S1Vehicles = Il2CppScheduleOne.Vehicles;
using S1VehiclesAI = Il2CppScheduleOne.Vehicles.AI;
using S1ObjectScripts = Il2CppScheduleOne.ObjectScripts;
#elif (MONOMELON || MONOBEPINEX || IL2CPPBEPINEX)
using S1NPCs = ScheduleOne.NPCs;
using S1NPCsSchedules = ScheduleOne.NPCs.Schedules;
using S1Map = ScheduleOne.Map;
using S1Vehicles = ScheduleOne.Vehicles;
using S1VehiclesAI = ScheduleOne.Vehicles.AI;
using S1ObjectScripts = ScheduleOne.ObjectScripts;
#endif
using System;
using System.Collections;
using System.Reflection;
using UnityEngine;
using S1API.Map;
using S1API.Vehicles;
using S1API.Logging;
using S1API.Internal.Utils;

namespace S1API.Entities.Schedule
{
    /// <summary>
    /// Specifies an action that makes an NPC drive to a car park and park a specified vehicle.
    /// </summary>
    /// <remarks>
    /// This action creates a <see cref="S1NPCsSchedules.NPCSignal_DriveToCarPark"/> that will
    /// make the NPC drive the specified vehicle to the designated parking lot and park it.
    /// The parking lot and vehicle can be specified using wrapper objects.
    /// </remarks>
    public sealed class DriveToCarParkSpec : IScheduleActionSpec
    {
        private static readonly Log Logger = new Log("DriveToCarParkSpec");

        /// <summary>
        /// Gets or sets the time when this action should start, in minutes from midnight.
        /// </summary>
        /// <value>The start time in minutes (0-1439 for a 24-hour day).</value>
        public int StartTime { get; set; }
        
        /// <summary>
        /// Gets or sets the GUID of the parking lot where the vehicle should be parked.
        /// </summary>
        /// <value>The parking lot GUID, or <c>null</c> if using wrapper object.</value>
        /// <remarks>
        /// This property is used as a fallback if <see cref="ParkingLot"/> is not specified.
        /// The GUID should match the parking lot's unique identifier in the game.
        /// </remarks>
        public string ParkingLotGUID { get; set; }
        
        /// <summary>
        /// Gets or sets the GUID of the vehicle that should be driven to the parking lot.
        /// </summary>
        /// <value>The vehicle GUID, or <c>null</c> if using wrapper object.</value>
        /// <remarks>
        /// This property is used as a fallback if <see cref="Vehicle"/> is not specified.
        /// The GUID should match the vehicle's unique identifier in the game.
        /// </remarks>
        public string VehicleGUID { get; set; }
        
        /// <summary>
        /// Gets or sets whether to override the default parking type behavior.
        /// </summary>
        /// <value><c>true</c> to override parking type; otherwise, <c>null</c> for default behavior.</value>
        /// <remarks>
        /// When set to <c>true</c>, the action will use the parking type specified in
        /// <see cref="ParkingType"/> or <see cref="Alignment"/> instead of the default behavior.
        /// </remarks>
        public bool? OverrideParkingType { get; set; }
        
        /// <summary>
        /// Gets or sets the parking type to use when parking the vehicle.
        /// </summary>
        /// <value>The parking type as an integer, or <c>null</c> to use default or alignment-based type.</value>
        /// <remarks>
        /// This property is used when <see cref="OverrideParkingType"/> is <c>true</c> and
        /// <see cref="Alignment"/> is not specified. The value should correspond to the
        /// <see cref="S1Vehicles.EParkingAlignment"/> enum values.
        /// </remarks>
        public int? ParkingType { get; set; }
        
        /// <summary>
        /// Gets or sets the S1API-facing parking alignment to use when parking the vehicle.
        /// </summary>
        /// <value>The parking alignment, or <c>null</c> to use default or type-based alignment.</value>
        /// <remarks>
        /// This property takes precedence over <see cref="ParkingType"/> when both are specified.
        /// It provides a more type-safe way to specify parking behavior using the S1API
        /// <see cref="ParkingAlignment"/> enum.
        /// </remarks>
        public ParkingAlignment? Alignment { get; set; }
        
        /// <summary>
        /// Gets or sets the optional name for this action.
        /// </summary>
        /// <value>The action name, or <c>null</c> to use the default name "DriveToCarPark".</value>
        public string Name { get; set; }
        
        /// <summary>
        /// Gets or sets the parking lot wrapper object to avoid GUID lookups.
        /// </summary>
        /// <value>The parking lot wrapper, or <c>null</c> to use GUID-based lookup.</value>
        /// <remarks>
        /// This property takes precedence over <see cref="ParkingLotGUID"/>.
        /// </remarks>
        public ParkingLotWrapper ParkingLot { get; set; }
        
        /// <summary>
        /// Gets or sets the vehicle wrapper object to avoid GUID lookups.
        /// </summary>
        /// <value>The vehicle wrapper, or <c>null</c> to use GUID-based lookup.</value>
        /// <remarks>
        /// This property takes precedence over <see cref="VehicleGUID"/> and <see cref="VehicleName"/>.
        /// </remarks>
        public LandVehicle Vehicle { get; set; }
        
        /// <summary>
        /// Gets or sets the name of the parking lot GameObject for runtime resolution.
        /// </summary>
        /// <value>The GameObject name, or <c>null</c> if not using name-based resolution.</value>
        /// <remarks>
        /// This property is used at runtime to find the parking lot by GameObject name.
        /// Takes precedence over GUID lookup but is overridden by wrapper object.
        /// </remarks>
        public string ParkingLotName { get; set; }
        
        /// <summary>
        /// Gets or sets the name of the vehicle GameObject for runtime resolution.
        /// </summary>
        /// <value>The GameObject name, or <c>null</c> if not using name-based resolution.</value>
        /// <remarks>
        /// This property is used at runtime to find the vehicle by GameObject name.
        /// Takes precedence over GUID lookup but is overridden by wrapper object.
        /// </remarks>
        public string VehicleName { get; set; }
        
        /// <summary>
        /// Gets or sets a vehicle code for runtime vehicle creation.
        /// </summary>
        /// <value>The vehicle code to create (e.g., "Sedan", "SUV"), or <c>null</c> if not creating.</value>
        /// <remarks>
        /// This property is used at runtime to create a new vehicle if one doesn't exist.
        /// Only used if Vehicle, VehicleGUID, and VehicleName are all null or failed resolution.
        /// </remarks>
        public string VehicleCode { get; set; }
        
        /// <summary>
        /// Gets or sets the spawn position for a created vehicle.
        /// </summary>
        /// <value>The world position where the vehicle should spawn.</value>
        /// <remarks>
        /// This property is used when creating a vehicle via <see cref="VehicleCode"/>.
        /// The vehicle will be spawned at this exact position when the schedule action executes.
        /// </remarks>
        public Vector3 VehicleSpawnPosition { get; set; }
        
        /// <summary>
        /// Gets or sets the spawn rotation for a created vehicle.
        /// </summary>
        /// <value>The world rotation for the vehicle spawn, or <c>null</c> to use identity rotation.</value>
        /// <remarks>
        /// This property is used when creating a vehicle via <see cref="VehicleCode"/>.
        /// If not specified, the vehicle will spawn with identity rotation (no rotation).
        /// </remarks>
        public Quaternion? VehicleSpawnRotation { get; set; }

        void IScheduleActionSpec.ApplyTo(NPCSchedule schedule)
        {
            var action = schedule.AddActionInternal<S1NPCsSchedules.NPCSignal_DriveToCarPark>(StartTime,
                string.IsNullOrEmpty(Name) ? "DriveToCarPark" : Name);
            if (action == null)
                return;

            try
            {
                // Resolve lot
                object lotObj = null;
                S1Map.ParkingLot gameLot = null;
                if (ParkingLot != null)
                {
                    gameLot = ParkingLot.ResolveGameLot();
                    lotObj = gameLot;
                }
                else if (!string.IsNullOrEmpty(ParkingLotName))
                {
                    var lotWrap = ParkingLotRegistry.GetByName(ParkingLotName);
                    gameLot = lotWrap?.ResolveGameLot();
                    lotObj = gameLot;
                }
                else if (!string.IsNullOrEmpty(ParkingLotGUID))
                {
                    var lotWrap = ParkingLotRegistry.GetByGUID(ParkingLotGUID);
                    gameLot = lotWrap?.ResolveGameLot();
                    lotObj = gameLot;
                }

                if (lotObj == null)
                {
                    Logger.Warning($"DriveToCarPark: Parking lot could not be resolved (Name='{ParkingLotName}', GUID='{ParkingLotGUID}'). Action will not function correctly.");
                }
                else
                {
                    ReflectionUtils.TrySetFieldOrProperty(action, "ParkingLot", lotObj);

                    // Warn if the lot has no parking spots - vehicles parked here will be hidden by the game
                    if (gameLot != null && (gameLot.ParkingSpots == null || gameLot.ParkingSpots.Count == 0))
                    {
                        Logger.Warning($"DriveToCarPark: Parking lot '{gameLot.gameObject.name}' has no parking spots. " +
                            "The game will hide (SetVisible=false) any vehicle parked here. Choose a lot with ParkingSpot children.");
                    }
                }

                // Resolve vehicle
                object vehObj = null;
                if (Vehicle != null && Vehicle.S1LandVehicle != null)
                {
                    vehObj = Vehicle.S1LandVehicle;
                }
                else if (!string.IsNullOrEmpty(VehicleName))
                {
                    var v = VehicleRegistry.GetByName(VehicleName);
                    vehObj = v?.S1LandVehicle;
                }
                else if (!string.IsNullOrEmpty(VehicleGUID))
                {
                    var v = VehicleRegistry.GetByGUID(VehicleGUID);
                    vehObj = v?.S1LandVehicle;
                }
                else if (!string.IsNullOrEmpty(VehicleCode))
                {
                    var v = VehicleRegistry.CreateVehicle(VehicleCode);
                    if (v == null)
                    {
                        Logger.Error($"DriveToCarPark: Failed to create vehicle with code '{VehicleCode}'. Verify the code is valid.");
                    }
                    else
                    {
                        vehObj = v.S1LandVehicle;
                    }

                    // If we're on the server and the vehicle needs spawning, spawn it now
                    if (vehObj != null)
                    {
                        var wrapper = v;
                        if (wrapper != null && wrapper.S1LandVehicle != null)
                        {
                            try
                            {
                                #if (MONOMELON || MONOBEPINEX || IL2CPPBEPINEX)
                                var nm = FishNet.InstanceFinder.NetworkManager;
                                #else
                                var nm = Il2CppFishNet.InstanceFinder.NetworkManager;
                                #endif

                                if (nm != null && nm.IsServer)
                                {
                                    var spawnRot = VehicleSpawnRotation ?? Quaternion.identity;
                                    wrapper.Spawn(VehicleSpawnPosition, spawnRot);
                                }
                            }
                            catch (Exception ex)
                            {
                                Logger.Error($"DriveToCarPark: Failed to spawn created vehicle '{VehicleCode}': {ex.Message}");
                                Logger.Error($"DriveToCarPark: Stack trace: {ex.StackTrace}");
                            }
                        }
                    }
                }

                if (vehObj == null)
                {
                    Logger.Warning("DriveToCarPark: Vehicle could not be resolved. The NPC will not be able to drive.");
                }
                else
                {
                    ReflectionUtils.TrySetFieldOrProperty(action, "Vehicle", vehObj);

                    // Track vehicles created for lots with no spots so the patch can prevent hiding
                    if (gameLot != null && (gameLot.ParkingSpots == null || gameLot.ParkingSpots.Count == 0))
                    {
                        var gameVeh = vehObj as S1Vehicles.LandVehicle;
                        if (gameVeh != null)
                            VehiclesAtNoSpotLots.Add(gameVeh);
                    }
                }

                // Flags
                if (OverrideParkingType.HasValue)
                    ReflectionUtils.TrySetFieldOrProperty(action, "OverrideParkingType", OverrideParkingType.Value);

                if (Alignment.HasValue)
                {
                    var boxed = (S1Vehicles.EParkingAlignment)(int)Alignment.Value;
                    ReflectionUtils.TrySetFieldOrProperty(action, "ParkingType", boxed);
                }
                else if (ParkingType.HasValue)
                {
                    var boxed = (S1Vehicles.EParkingAlignment)ParkingType.Value;
                    ReflectionUtils.TrySetFieldOrProperty(action, "ParkingType", boxed);
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"DriveToCarPark: Unexpected error during ApplyTo: {ex.Message}");
                Logger.Error($"DriveToCarPark: Stack trace: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// INTERNAL: Tracks vehicles assigned to parking lots with no spots.
        /// Used by the LandVehicle.Park patch to prevent the game from hiding these vehicles.
        /// </summary>
        internal static readonly System.Collections.Generic.HashSet<S1Vehicles.LandVehicle> VehiclesAtNoSpotLots =
            new System.Collections.Generic.HashSet<S1Vehicles.LandVehicle>();
    }
}

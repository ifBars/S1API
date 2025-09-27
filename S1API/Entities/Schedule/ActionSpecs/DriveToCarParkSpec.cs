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
using UnityEngine;
using S1API.Map;
using S1API.Vehicles;

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
        /// This property takes precedence over <see cref="VehicleGUID"/>.
        /// </remarks>
        public LandVehicle Vehicle { get; set; }

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
                if (ParkingLot != null)
                {
                    lotObj = ParkingLot.ResolveGameLot();
                }
                else if (!string.IsNullOrEmpty(ParkingLotGUID))
                {
                    var lotWrap = ParkingLots.GetByGUID(ParkingLotGUID);
                    lotObj = lotWrap?.ResolveGameLot();
                }
                if (lotObj != null)
                    action.GetType().GetField("ParkingLot")?.SetValue(action, lotObj);

                // Resolve vehicle
                object vehObj = null;
                if (Vehicle != null && Vehicle.S1LandVehicle != null)
                {
                    vehObj = Vehicle.S1LandVehicle;
                }
                else if (!string.IsNullOrEmpty(VehicleGUID))
                {
                    var v = VehicleRegistry.GetByGUID(VehicleGUID);
                    vehObj = v?.S1LandVehicle;
                }
                if (vehObj != null)
                    action.GetType().GetField("Vehicle")?.SetValue(action, vehObj);

                // Flags
                if (OverrideParkingType.HasValue)
                    action.GetType().GetField("OverrideParkingType")?.SetValue(action, OverrideParkingType.Value);

                var parkingField = action.GetType().GetField("ParkingType");
                if (Alignment.HasValue)
                {
                    var boxed = (S1Vehicles.EParkingAlignment)(int)Alignment.Value;
                    parkingField?.SetValue(action, boxed);
                }
                else if (ParkingType.HasValue)
                {
                    var boxed = (S1Vehicles.EParkingAlignment)ParkingType.Value;
                    parkingField?.SetValue(action, boxed);
                }
            }
            catch { }
        }
    }
}

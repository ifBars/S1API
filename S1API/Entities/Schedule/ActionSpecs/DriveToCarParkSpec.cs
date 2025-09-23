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
    /// Drive to a car park and park the specified vehicle.
    /// </summary>
    public sealed class DriveToCarParkSpec : IScheduleActionSpec
    {
        public int StartTime { get; set; }
        public string ParkingLotGUID { get; set; }
        public string VehicleGUID { get; set; }
        public bool? OverrideParkingType { get; set; }
        public int? ParkingType { get; set; }
        
        /// <summary>
        /// Optional S1API-facing parking alignment. If set, overrides ParkingType.
        /// </summary>
        public ParkingAlignment? Alignment { get; set; }
        public string Name { get; set; }
        
        /// <summary>
        /// Optional: pass resolved wrappers directly to avoid GUID lookups.
        /// </summary>
        public ParkingLotWrapper ParkingLot { get; set; }
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

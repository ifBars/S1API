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
    /// Modder-facing, game-type-free schedule action spec.
    /// </summary>
    public interface IScheduleActionSpec
    {
        public void ApplyTo(NPCSchedule schedule);
    }

    /// <summary>
    /// Walk to a world position at a given time.
    /// </summary>
    public sealed class WalkToSpec : IScheduleActionSpec
    {
        public Vector3 Destination { get; set; }
        public int StartTime { get; set; }
        public bool FaceDestinationDirection { get; set; } = true;
        public float Within { get; set; } = 1f;
        public bool WarpIfSkipped { get; set; } = false;
        public string Name { get; set; }

        void IScheduleActionSpec.ApplyTo(NPCSchedule schedule)
        {
            schedule.AddWalkTo(Destination, StartTime, FaceDestinationDirection, Within, WarpIfSkipped, Name);
        }
    }

    /// <summary>
    /// Ensure the customer DealSignal exists under the schedule.
    /// </summary>
    public sealed class EnsureDealSignalSpec : IScheduleActionSpec
    {
        void IScheduleActionSpec.ApplyTo(NPCSchedule schedule)
        {
            schedule.EnsureDealSignal();
        }
    }

    /// <summary>
    /// Remain inside a building for a duration window.
    /// </summary>
    public sealed class StayInBuildingSpec : IScheduleActionSpec
    {
        public string BuildingGUID { get; set; }
        public string BuildingName { get; set; }
        public int StartTime { get; set; }
        public int DurationMinutes { get; set; } = 60;
        public int? DoorIndex { get; set; }
        public string Name { get; set; }

        public void ApplyTo(NPCSchedule schedule)
        {
            var action = schedule.AddActionInternal<S1NPCsSchedules.NPCEvent_StayInBuilding>(StartTime, string.IsNullOrEmpty(Name) ? "StayInBuilding" : Name);
            if (action == null)
                return;

            action.Duration = Mathf.Max(1, DurationMinutes);

            // Resolve building using S1API.Map registry first
            object gameBuilding = null;
            if (!string.IsNullOrEmpty(BuildingGUID))
            {
                var wrapper = Map.Buildings.GetByGUID(BuildingGUID);
                gameBuilding = wrapper?.ResolveGameBuilding();
            }
            else if (!string.IsNullOrEmpty(BuildingName))
            {
                var wrapper = Map.Buildings.GetByName(BuildingName);
                gameBuilding = wrapper?.ResolveGameBuilding();
                BuildingGUID = wrapper?.GUID ?? BuildingGUID;
            }

            if (gameBuilding != null)
            {
                var prop = action.GetType().GetField("Building");
                prop?.SetValue(action, gameBuilding);

                if (DoorIndex.HasValue)
                {
                    var doorField = action.GetType().GetField("Door");
                    var buildingType = gameBuilding.GetType();
                    var doorsProp = buildingType.GetProperty("Doors");
                    var list = doorsProp?.GetValue(gameBuilding) as System.Collections.IList;
                    if (list != null && DoorIndex.Value >= 0 && DoorIndex.Value < list.Count)
                    {
                        doorField?.SetValue(action, list[DoorIndex.Value]);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Go to a destination and start a location-based dialogue/action.
    /// </summary>
    public sealed class LocationDialogueSpec : IScheduleActionSpec
    {
        public Vector3 Destination { get; set; }
        public Vector3? Forward { get; set; }
        public int StartTime { get; set; }
        public bool FaceDestinationDirection { get; set; } = true;
        public float Within { get; set; } = 1f;
        public bool WarpIfSkipped { get; set; } = false;
        public int GreetingOverrideToEnable { get; set; } = -1;
        public int ChoiceToEnable { get; set; } = -1;
        public string Name { get; set; }

        void IScheduleActionSpec.ApplyTo(NPCSchedule schedule)
        {
            var action = schedule.AddActionInternal<S1NPCsSchedules.NPCEvent_LocationDialogue>(StartTime, string.IsNullOrEmpty(Name) ? "LocationDialogue" : Name);
            if (action == null)
                return;

            action.Destination = CreateMarker(schedule, action.transform, Destination, Forward);
            action.FaceDestinationDir = FaceDestinationDirection;
            action.DestinationThreshold = Mathf.Max(0.01f, Within);
            action.WarpIfSkipped = WarpIfSkipped;
            action.GreetingOverrideToEnable = GreetingOverrideToEnable;
            action.ChoiceToEnable = ChoiceToEnable;
        }

        internal static Transform CreateMarker(NPCSchedule schedule, Transform parent, Vector3 position, Vector3? forward)
        {
            var go = new GameObject("Marker");
            go.transform.position = position;
            if (forward.HasValue && forward.Value.sqrMagnitude > 0.001f)
                go.transform.forward = forward.Value.normalized;
            else
            {
                var look = schedule.NPC.gameObject.transform.position;
                var dir = (position - look);
                if (dir.sqrMagnitude > 0.001f)
                    go.transform.forward = dir.normalized;
            }
            return go.transform;
        }
    }

    /// <summary>
    /// Use the nearest vending machine (or specific by GUID in future).
    /// </summary>
    public sealed class UseVendingMachineSpec : IScheduleActionSpec
    {
        public int StartTime { get; set; }
        public string MachineGUID { get; set; }
        public string Name { get; set; }

        void IScheduleActionSpec.ApplyTo(NPCSchedule schedule)
        {
            var action = schedule.AddActionInternal<S1NPCsSchedules.NPCSignal_UseVendingMachine>(StartTime, string.IsNullOrEmpty(Name) ? "UseVending" : Name);
            if (action == null)
                return;

            if (!string.IsNullOrEmpty(MachineGUID))
            {
                try
                {
#if MONOMELON
                    var guid = new System.Guid(MachineGUID);
#else
                    var guid = new Il2CppSystem.Guid(MachineGUID);
#endif
                    var machine = GUIDManager.GetObject<S1ObjectScripts.VendingMachine>(guid);
                    action.MachineOverride = machine;
                }
                catch { }
            }
        }
    }

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



#if (IL2CPPMELON)
using S1NPCsSchedules = Il2CppScheduleOne.NPCs.Schedules;
#elif (MONOMELON || MONOBEPINEX || IL2CPPBEPINEX)
using S1NPCsSchedules = ScheduleOne.NPCs.Schedules;
#endif

using System;
using UnityEngine;
using S1API.Map;
using S1API.Vehicles;

namespace S1API.Entities.Schedule
{
    /// <summary>
    /// Fluent builder for composing an NPC's schedule programmatically.
    /// </summary>
    public sealed class NPCScheduleBuilder
    {
        private readonly NPCSchedule _schedule;

        internal NPCScheduleBuilder(NPCSchedule schedule)
        {
            _schedule = schedule;
        }

        /// <summary>
        /// Adds a walk-to action at the given start time.
        /// </summary>
        public NPCScheduleBuilder WalkTo(Vector3 destination, int startTime, bool faceDestinationDir = true, float within = 1f, bool warpIfSkipped = false, string name = null)
        {
            _schedule.AddWalkTo(destination, startTime, faceDestinationDir, within, warpIfSkipped, name);
            return this;
        }

        /// <summary>
        /// Ensures the customer deal signal exists under this schedule.
        /// </summary>
        public NPCScheduleBuilder EnsureDealSignal()
        {
            _schedule.EnsureDealSignal();
            return this;
        }

        /// <summary>
        /// Adds a custom schedule action using an S1API spec.
        /// </summary>
        public NPCScheduleBuilder Add(IScheduleActionSpec spec)
        {
            if (spec == null)
                return this;
            spec.ApplyTo(_schedule);
            return this;
        }

        /// <summary>
        /// INTERNAL: Adds a custom action type with an optional configuration callback.
        /// </summary>
        internal NPCScheduleBuilder Add<T>(int startTime, Action<T> configure = null, string name = null) where T : S1NPCsSchedules.NPCAction
        {
            var action = _schedule.AddActionInternal<T>(startTime, name);
            configure?.Invoke(action);
            return this;
        }

        // Adds a "Stay in Building" event using S1API.Map.Building wrapper.
        // Commented out as Building instances might be null during ConfigurePrefab
        /*
        public NPCScheduleBuilder StayInBuilding(Map.Building building, int startTime, int durationMinutes = 60, int? doorIndex = null, string name = null)
        {
            if (building == null)
                return this;
            var spec = new StayInBuildingSpec
            {
                BuildingGUID = building.GUID,
                StartTime = startTime,
                DurationMinutes = durationMinutes,
                DoorIndex = doorIndex,
                Name = name
            };
            spec.ApplyTo(_schedule);
            return this;
        }
        */

        /// <summary>
        /// Adds a "Drive to Car Park" action using wrappers.
        /// </summary>
        public NPCScheduleBuilder DriveToCarPark(ParkingLotWrapper lot, LandVehicle vehicle, int startTime, ParkingAlignment? alignment = null, bool? overrideParkingType = null, string name = null)
        {
            var spec = new DriveToCarParkSpec
            {
                StartTime = startTime,
                ParkingLot = lot,
                Vehicle = vehicle,
                Alignment = alignment,
                OverrideParkingType = overrideParkingType,
                Name = name
            };
            ((IScheduleActionSpec)spec).ApplyTo(_schedule);
            return this;
        }

        /// <summary>
        /// Adds a "Drive to Car Park" action using GUIDs.
        /// </summary>
        public NPCScheduleBuilder DriveToCarPark(string parkingLotGUID, string vehicleGUID, int startTime, ParkingAlignment? alignment = null, bool? overrideParkingType = null, string name = null)
        {
            var spec = new DriveToCarParkSpec
            {
                StartTime = startTime,
                ParkingLotGUID = parkingLotGUID,
                VehicleGUID = vehicleGUID,
                Alignment = alignment,
                OverrideParkingType = overrideParkingType,
                Name = name
            };
            ((IScheduleActionSpec)spec).ApplyTo(_schedule);
            return this;
        }

        /// <summary>
        /// Clears all configured actions. Use carefully on NPCs with authored schedules.
        /// </summary>
        public NPCScheduleBuilder ClearAll()
        {
            _schedule.ClearActions();
            return this;
        }
    }

    /// <summary>
    /// Plan-time schedule builder used during prefab composition.
    /// Collects <see cref="IScheduleActionSpec"/> entries without requiring a live NPC instance.
    /// </summary>
    public sealed class PrefabScheduleBuilder
    {
        private readonly System.Collections.Generic.List<IScheduleActionSpec> _specs = new System.Collections.Generic.List<IScheduleActionSpec>();

        internal System.Collections.Generic.List<IScheduleActionSpec> Build() => _specs;

        /// <summary>
        /// Adds a walk-to action at the given start time.
        /// </summary>
        public PrefabScheduleBuilder WalkTo(UnityEngine.Vector3 destination, int startTime, bool faceDestinationDir = true, float within = 1f, bool warpIfSkipped = false, string name = null)
        {
            _specs.Add(new WalkToSpec { Destination = destination, StartTime = startTime, FaceDestinationDirection = faceDestinationDir, Within = within, WarpIfSkipped = warpIfSkipped, Name = name });
            return this;
        }

        /// <summary>
        /// Ensures the customer deal signal exists under the schedule.
        /// </summary>
        public PrefabScheduleBuilder EnsureDealSignal()
        {
            _specs.Add(new EnsureDealSignalSpec());
            return this;
        }

        /// <summary>
        /// Adds a custom schedule action using an S1API spec.
        /// </summary>
        public PrefabScheduleBuilder Add(IScheduleActionSpec spec)
        {
            if (spec != null)
                _specs.Add(spec);
            return this;
        }

        /// <summary>
        /// Adds a "Stay in Building" event using S1API.Map.Building wrapper.
        /// </summary>
        public PrefabScheduleBuilder StayInBuilding(Building building, int startTime, int durationMinutes = 60, int? doorIndex = null, string name = null)
        {
            if (building != null)
            {
                _specs.Add(new StayInBuildingSpec
                {
                    BuildingName = building.Name,
                    StartTime = startTime,
                    DurationMinutes = durationMinutes,
                    DoorIndex = doorIndex,
                    Name = name
                });
            }
            return this;
        }
        

        /// <summary>
        /// Adds a "Drive to Car Park" action using wrappers.
        /// </summary>
        public PrefabScheduleBuilder DriveToCarPark(ParkingLotWrapper lot, LandVehicle vehicle, int startTime, ParkingAlignment? alignment = null, bool? overrideParkingType = null, string name = null)
        {
            _specs.Add(new DriveToCarParkSpec
            {
                StartTime = startTime,
                ParkingLot = lot,
                Vehicle = vehicle,
                Alignment = alignment,
                OverrideParkingType = overrideParkingType,
                Name = name
            });
            return this;
        }

        /// <summary>
        /// Adds a "Drive to Car Park" action using GUIDs.
        /// </summary>
        public PrefabScheduleBuilder DriveToCarPark(string parkingLotGUID, string vehicleGUID, int startTime, ParkingAlignment? alignment = null, bool? overrideParkingType = null, string name = null)
        {
            _specs.Add(new DriveToCarParkSpec
            {
                StartTime = startTime,
                ParkingLotGUID = parkingLotGUID,
                VehicleGUID = vehicleGUID,
                Alignment = alignment,
                OverrideParkingType = overrideParkingType,
                Name = name
            });
            return this;
        }
    }
}



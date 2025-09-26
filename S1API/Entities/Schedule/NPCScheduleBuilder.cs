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
        /// Adds a walk-to action that moves the NPC to a specific world position at the given start time.
        /// </summary>
        /// <param name="destination">The world position where the NPC should walk to.</param>
        /// <param name="startTime">The time when this action should start, in minutes from midnight (0-1439).</param>
        /// <param name="faceDestinationDir">Whether the NPC should face the destination direction when walking. Default is <c>true</c>.</param>
        /// <param name="within">The distance threshold within which the NPC is considered to have arrived. Default is 1.0f.</param>
        /// <param name="warpIfSkipped">Whether the NPC should be warped to the destination if the action is skipped. Default is <c>false</c>.</param>
        /// <param name="name">The optional name for this action. If <c>null</c>, uses "WalkTo".</param>
        /// <returns>This builder instance for method chaining.</returns>
        /// <remarks>
        /// This method creates a <see cref="S1NPCsSchedules.NPCSignal_WalkToLocation"/> action that will
        /// make the NPC walk to the specified destination. The NPC will be considered to have arrived
        /// when they are within the specified threshold distance of the destination.
        /// </remarks>
        public NPCScheduleBuilder WalkTo(Vector3 destination, int startTime, bool faceDestinationDir = true, float within = 1f, bool warpIfSkipped = false, string name = null)
        {
            _schedule.AddWalkTo(destination, startTime, faceDestinationDir, within, warpIfSkipped, name);
            return this;
        }

        /// <summary>
        /// Ensures that a customer deal signal exists under this schedule for handling deal interactions.
        /// </summary>
        /// <returns>This builder instance for method chaining.</returns>
        /// <remarks>
        /// This method ensures that a <see cref="S1NPCsSchedules.NPCSignal_WaitForDelivery"/> component
        /// exists on the NPC's schedule manager. This signal is required for customer NPCs to
        /// properly handle deal interactions and handovers with the player.
        /// 
        /// The deal signal allows the NPC to wait for deliveries and toggle customer handover states.
        /// If the signal doesn't exist, it will be created and properly initialized.
        /// </remarks>
        public NPCScheduleBuilder EnsureDealSignal()
        {
            _schedule.EnsureDealSignal();
            return this;
        }

        /// <summary>
        /// Adds a custom schedule action using an S1API action specification.
        /// </summary>
        /// <param name="spec">The action specification to apply to the schedule.</param>
        /// <returns>This builder instance for method chaining.</returns>
        /// <remarks>
        /// This method allows adding custom schedule actions using the <see cref="IScheduleActionSpec"/>
        /// interface. The specification will be applied to the underlying schedule, creating
        /// the appropriate action components.
        /// 
        /// If the specification is <c>null</c>, this method does nothing and returns the builder unchanged.
        /// </remarks>
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

        /// <summary>
        /// Adds a "Drive to Car Park" action that makes the NPC drive a vehicle to a parking lot using wrapper objects.
        /// </summary>
        /// <param name="lot">The parking lot wrapper where the vehicle should be parked.</param>
        /// <param name="vehicle">The vehicle wrapper that should be driven to the parking lot.</param>
        /// <param name="startTime">The time when this action should start, in minutes from midnight (0-1439).</param>
        /// <param name="alignment">The optional parking alignment to use when parking the vehicle.</param>
        /// <param name="overrideParkingType">Whether to override the default parking type behavior.</param>
        /// <param name="name">The optional name for this action. If <c>null</c>, uses "DriveToCarPark".</param>
        /// <returns>This builder instance for method chaining.</returns>
        /// <remarks>
        /// This method creates a <see cref="S1NPCsSchedules.NPCSignal_DriveToCarPark"/> action that will
        /// make the NPC drive the specified vehicle to the designated parking lot and park it.
        /// </remarks>
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
        /// Adds a "Drive to Car Park" action that makes the NPC drive a vehicle to a parking lot using GUIDs.
        /// </summary>
        /// <param name="parkingLotGUID">The GUID of the parking lot where the vehicle should be parked.</param>
        /// <param name="vehicleGUID">The GUID of the vehicle that should be driven to the parking lot.</param>
        /// <param name="startTime">The time when this action should start, in minutes from midnight (0-1439).</param>
        /// <param name="alignment">The optional parking alignment to use when parking the vehicle.</param>
        /// <param name="overrideParkingType">Whether to override the default parking type behavior.</param>
        /// <param name="name">The optional name for this action. If <c>null</c>, uses "DriveToCarPark".</param>
        /// <returns>This builder instance for method chaining.</returns>
        /// <remarks>
        /// This method creates a <see cref="S1NPCsSchedules.NPCSignal_DriveToCarPark"/> action that will
        /// make the NPC drive the specified vehicle to the designated parking lot and park it.
        /// The GUIDs will be resolved to their corresponding objects at runtime.
        /// </remarks>
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
        /// Clears all configured actions from the schedule. Use carefully on NPCs with authored schedules.
        /// </summary>
        /// <returns>This builder instance for method chaining.</returns>
        /// <remarks>
        /// This method removes all schedule actions (both signals and events) from the NPC's schedule.
        /// It should be used with caution, especially on NPCs that have pre-authored schedules,
        /// as it will completely reset their behavior.
        /// 
        /// After clearing, you can rebuild the schedule using the builder methods.
        /// </remarks>
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
        /// Adds a walk-to action that moves the NPC to a specific world position at the given start time.
        /// </summary>
        /// <param name="destination">The world position where the NPC should walk to.</param>
        /// <param name="startTime">The time when this action should start, in minutes from midnight (0-1439).</param>
        /// <param name="faceDestinationDir">Whether the NPC should face the destination direction when walking. Default is <c>true</c>.</param>
        /// <param name="within">The distance threshold within which the NPC is considered to have arrived. Default is 1.0f.</param>
        /// <param name="warpIfSkipped">Whether the NPC should be warped to the destination if the action is skipped. Default is <c>false</c>.</param>
        /// <param name="name">The optional name for this action. If <c>null</c>, uses "WalkTo".</param>
        /// <returns>This builder instance for method chaining.</returns>
        /// <remarks>
        /// This method creates a <see cref="WalkToSpec"/> that will be applied when the prefab is configured.
        /// The specification is stored and will create a <see cref="S1NPCsSchedules.NPCSignal_WalkToLocation"/> action
        /// when applied to the actual NPC schedule.
        /// </remarks>
        public PrefabScheduleBuilder WalkTo(UnityEngine.Vector3 destination, int startTime, bool faceDestinationDir = true, float within = 1f, bool warpIfSkipped = false, string name = null)
        {
            _specs.Add(new WalkToSpec { Destination = destination, StartTime = startTime, FaceDestinationDirection = faceDestinationDir, Within = within, WarpIfSkipped = warpIfSkipped, Name = name });
            return this;
        }

        /// <summary>
        /// Ensures that a customer deal signal exists under the schedule for handling deal interactions.
        /// </summary>
        /// <returns>This builder instance for method chaining.</returns>
        /// <remarks>
        /// This method creates an <see cref="EnsureDealSignalSpec"/> that will be applied when the prefab is configured.
        /// The specification ensures that a <see cref="S1NPCsSchedules.NPCSignal_WaitForDelivery"/> component
        /// exists on the NPC's schedule manager for proper customer deal handling.
        /// </remarks>
        public PrefabScheduleBuilder EnsureDealSignal()
        {
            _specs.Add(new EnsureDealSignalSpec());
            return this;
        }

        /// <summary>
        /// Adds a custom schedule action using an S1API action specification.
        /// </summary>
        /// <param name="spec">The action specification to add to the prefab schedule.</param>
        /// <returns>This builder instance for method chaining.</returns>
        /// <remarks>
        /// This method allows adding custom schedule actions using the <see cref="IScheduleActionSpec"/>
        /// interface. The specification will be stored and applied when the prefab is configured.
        /// 
        /// If the specification is <c>null</c>, this method does nothing and returns the builder unchanged.
        /// </remarks>
        public PrefabScheduleBuilder Add(IScheduleActionSpec spec)
        {
            if (spec != null)
                _specs.Add(spec);
            return this;
        }

        /// <summary>
        /// Adds a "Stay in Building" action that makes the NPC remain inside a building for a specified duration.
        /// </summary>
        /// <param name="building">The building wrapper where the NPC should stay.</param>
        /// <param name="startTime">The time when this action should start, in minutes from midnight (0-1439).</param>
        /// <param name="durationMinutes">The duration for which the NPC should remain in the building. Default is 60 minutes.</param>
        /// <param name="doorIndex">The optional door index to use when entering the building.</param>
        /// <param name="name">The optional name for this action. If <c>null</c>, uses "StayInBuilding".</param>
        /// <returns>This builder instance for method chaining.</returns>
        /// <remarks>
        /// This method creates a <see cref="StayInBuildingSpec"/> that will be applied when the prefab is configured.
        /// The specification will create a <see cref="S1NPCsSchedules.NPCEvent_StayInBuilding"/> action
        /// that keeps the NPC inside the specified building for the given duration.
        /// 
        /// If the building is <c>null</c>, this method does nothing and returns the builder unchanged.
        /// </remarks>
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
        /// Adds a "Drive to Car Park" action that makes the NPC drive a vehicle to a parking lot using wrapper objects.
        /// </summary>
        /// <param name="lot">The parking lot wrapper where the vehicle should be parked.</param>
        /// <param name="vehicle">The vehicle wrapper that should be driven to the parking lot.</param>
        /// <param name="startTime">The time when this action should start, in minutes from midnight (0-1439).</param>
        /// <param name="alignment">The optional parking alignment to use when parking the vehicle.</param>
        /// <param name="overrideParkingType">Whether to override the default parking type behavior.</param>
        /// <param name="name">The optional name for this action. If <c>null</c>, uses "DriveToCarPark".</param>
        /// <returns>This builder instance for method chaining.</returns>
        /// <remarks>
        /// This method creates a <see cref="DriveToCarParkSpec"/> that will be applied when the prefab is configured.
        /// The specification will create a <see cref="S1NPCsSchedules.NPCSignal_DriveToCarPark"/> action
        /// that makes the NPC drive the specified vehicle to the designated parking lot and park it.
        /// </remarks>
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
        /// Adds a "Drive to Car Park" action that makes the NPC drive a vehicle to a parking lot using GUIDs.
        /// </summary>
        /// <param name="parkingLotGUID">The GUID of the parking lot where the vehicle should be parked.</param>
        /// <param name="vehicleGUID">The GUID of the vehicle that should be driven to the parking lot.</param>
        /// <param name="startTime">The time when this action should start, in minutes from midnight (0-1439).</param>
        /// <param name="alignment">The optional parking alignment to use when parking the vehicle.</param>
        /// <param name="overrideParkingType">Whether to override the default parking type behavior.</param>
        /// <param name="name">The optional name for this action. If <c>null</c>, uses "DriveToCarPark".</param>
        /// <returns>This builder instance for method chaining.</returns>
        /// <remarks>
        /// This method creates a <see cref="DriveToCarParkSpec"/> that will be applied when the prefab is configured.
        /// The specification will create a <see cref="S1NPCsSchedules.NPCSignal_DriveToCarPark"/> action
        /// that makes the NPC drive the specified vehicle to the designated parking lot and park it.
        /// The GUIDs will be resolved to their corresponding objects at runtime.
        /// </remarks>
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



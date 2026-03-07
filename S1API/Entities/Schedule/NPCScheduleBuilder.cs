#if (IL2CPPMELON)
using S1NPCsSchedules = Il2CppScheduleOne.NPCs.Schedules;
#elif (MONOMELON || MONOBEPINEX || IL2CPPBEPINEX)
using S1NPCsSchedules = ScheduleOne.NPCs.Schedules;
#endif

using System;
using S1API.Casino;
using UnityEngine;
using S1API.Map;
using S1API.Vehicles;
using S1API.Logging;

namespace S1API.Entities.Schedule
{
    // Runtime NPCScheduleBuilder removed. Schedules must be declared in ConfigurePrefab via PrefabScheduleBuilder.

    /// <summary>
    /// Plan-time schedule builder used during prefab composition.
    /// Collects <see cref="IScheduleActionSpec"/> entries without requiring a live NPC instance.
    /// </summary>
    /// <remarks>
    /// <para><strong>IMPORTANT:</strong> Avoid scheduling multiple actions at start time 0 (midnight).
    /// The game's action sorting comparator has a bug that can cause inconsistent sort results when
    /// multiple non-signal actions share the same start time. This issue is most commonly encountered
    /// at time 0 when using <see cref="EnsureDealSignal()"/> which creates a signal at time 0.</para>
    ///
    /// <para>To avoid this issue, schedule your first action at time 1 or later (e.g., 10 minutes = 0:10 AM).</para>
    /// </remarks>
    public sealed class PrefabScheduleBuilder
    {
        private static readonly Log Logger = new Log("PrefabScheduleBuilder");
        private readonly System.Collections.Generic.List<IScheduleActionSpec> _specs = new System.Collections.Generic.List<IScheduleActionSpec>();

        internal System.Collections.Generic.List<IScheduleActionSpec> Build() => _specs;

        /// <summary>
        /// Adds a walk-to action that moves the NPC to a specific world position at the given start time.
        /// </summary>
        /// <param name="destination">The world position where the NPC should walk to.</param>
        /// <param name="startTime">The time when this action should start, in minutes from midnight (0-1439).
        /// <strong>Avoid using 0</strong> to prevent sort comparison issues.</param>
        /// <param name="faceDestinationDir">Whether the NPC should face the destination direction when walking. Default is <c>true</c>.</param>
        /// <param name="within">The distance threshold within which the NPC is considered to have arrived. Default is 1.0f.</param>
        /// <param name="warpIfSkipped">Whether the NPC should be warped to the destination if the action is skipped. Default is <c>false</c>.</param>
        /// <param name="forward">The optional forward direction vector for the destination marker. If <c>null</c>, the direction will be calculated from the NPC's position to the destination.</param>
        /// <param name="name">The optional name for this action. If <c>null</c>, uses "WalkTo".</param>
        /// <returns>This builder instance for method chaining.</returns>
        /// <remarks>
        /// This method creates a <see cref="WalkToSpec"/> that will be applied when the prefab is configured.
        /// The specification is stored and will create a <see cref="S1NPCsSchedules.NPCSignal_WalkToLocation"/> action
        /// when applied to the actual NPC schedule.
        /// 
        /// If <paramref name="forward"/> is specified, it will be used to orient the destination marker.
        /// Otherwise, the direction will be automatically calculated from the NPC's current position to the destination.
        /// </remarks>
        public PrefabScheduleBuilder WalkTo(UnityEngine.Vector3 destination, int startTime, bool faceDestinationDir = true, float within = 1f, bool warpIfSkipped = false, UnityEngine.Vector3? forward = null, string name = null)
        {
            _specs.Add(new WalkToSpec { Destination = destination, StartTime = startTime, FaceDestinationDirection = faceDestinationDir, Within = within, WarpIfSkipped = warpIfSkipped, Forward = forward, Name = name });
            return this;
        }

        /// <summary>
        /// Adds a seating action that moves the NPC to an available seat within the specified seat set.
        /// </summary>
        /// <param name="seatSetName">The GameObject name of the <c>AvatarSeatSet</c> to use. Can be <c>null</c> if <paramref name="seatSetPath"/> is provided.</param>
        /// <param name="startTime">The time when this action should start, in 24-hour HHMM format (e.g. 830 for 8:30 AM, 1400 for 2:00 PM).</param>
        /// <param name="durationMinutes">Duration of the sit action in minutes. Defaults to 60. Must be positive for the action to trigger.</param>
        /// <param name="warpIfSkipped">Whether the NPC should be warped to the seat if the action is skipped. Default is <c>false</c>.</param>
        /// <param name="name">Optional custom name for this action; defaults to "Sit".</param>
        /// <param name="seatSetPath">Optional full hierarchy path to the seat set GameObject (e.g. "Map/Hyland Point/Region_Docks/WaterFront/OutdoorBench (1)"). Use this when multiple seat sets share the same name to target a specific one.</param>
        /// <returns>This builder instance for method chaining.</returns>
        /// <remarks>
        /// <para>This method creates a <see cref="SitSpec"/> that resolves a seat set by name, path, or both.
        /// When multiple seat sets share the same name (e.g. "outdoorbench"), use <paramref name="seatSetPath"/>
        /// to target a specific one. The path is matched case-insensitively against the full transform hierarchy.</para>
        ///
        /// <para><strong>Example — by name:</strong></para>
        /// <code>plan.SitAtSeatSet("Fast Food Booth", 900, durationMinutes: 60)</code>
        ///
        /// <para><strong>Example — by path (when name is ambiguous):</strong></para>
        /// <code>plan.SitAtSeatSet(null, 1650, durationMinutes: 130, seatSetPath: "Map/Hyland Point/Region_Docks/WaterFront/OutdoorBench (1)")</code>
        ///
        /// <para>If the seat set cannot be resolved at runtime, the action is disabled and a warning is logged.
        /// This prevents a NullReferenceException that would permanently break the NPC's schedule.</para>
        /// </remarks>
        public PrefabScheduleBuilder SitAtSeatSet(string seatSetName, int startTime, int durationMinutes = 60, bool warpIfSkipped = false, string name = null, string seatSetPath = null)
        {
            if (!string.IsNullOrEmpty(seatSetName) || !string.IsNullOrEmpty(seatSetPath))
            {
                _specs.Add(new SitSpec
                {
                    SeatSetName = seatSetName,
                    SeatSetPath = seatSetPath,
                    StartTime = startTime,
                    DurationMinutes = durationMinutes,
                    WarpIfSkipped = warpIfSkipped,
                    Name = name
                });
            }

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
        /// Adds a vending machine usage action at the specified time.
        /// </summary>
        /// <param name="startTime">The time when this action should start, in minutes from midnight (0-1439).</param>
        /// <param name="machineGUID">Optional GUID of a specific vending machine to use; if null, the nearest reachable machine will be used.</param>
        /// <param name="name">Optional custom name for this action; defaults to "UseVending".</param>
        /// <returns>This builder instance for method chaining.</returns>
        /// <remarks>
        /// Creates a <see cref="UseVendingMachineSpec"/> that, when applied, configures a
        /// <see cref="S1NPCsSchedules.NPCSignal_UseVendingMachine"/> under the NPC's schedule manager.
        /// </remarks>
        public PrefabScheduleBuilder UseVendingMachine(int startTime, string machineGUID = null, string name = null)
        {
            _specs.Add(new UseVendingMachineSpec { StartTime = startTime, MachineGUID = machineGUID, Name = name });
            return this;
        }

        /// <summary>
        /// Adds a slot machine usage action at the specified time.
        /// </summary>
        /// <param name="startTime">The time when this action should start, in minutes from midnight (0-1439).</param>
        /// <param name="machinePosition">The world position of the slot machine to use.</param>
        /// <param name="betAmount">The amount to bet in dollars (default: 10).</param>
        /// <param name="sessionMode">The gambling session mode (default: single spin).</param>
        /// <param name="maxSearchDistance">Maximum distance to search for a slot machine from the position (default: 5.0).</param>
        /// <param name="name">Optional custom name for this action; defaults to "UseSlotMachine".</param>
        /// <returns>This builder instance for method chaining.</returns>
        /// <remarks>
        /// Creates a <see cref="UseSlotMachineSpec"/> that makes the NPC walk to the slot machine
        /// location and play it according to the session mode. The NPC must have sufficient cash
        /// in their inventory to place bets.
        /// </remarks>
        public PrefabScheduleBuilder UseSlotMachine(
            int startTime, 
            Vector3 machinePosition, 
            int betAmount = 10, 
            GamblingSessionMode sessionMode = GamblingSessionMode.SingleSpin,
            float maxSearchDistance = 5f, 
            string name = null)
        {
            _specs.Add(new UseSlotMachineSpec 
            { 
                StartTime = startTime, 
                MachinePosition = machinePosition, 
                BetAmount = betAmount,
                SessionMode = sessionMode,
                MaxSearchDistance = maxSearchDistance,
                Name = name 
            });
            return this;
        }

        /// <summary>
        /// Adds a slot machine usage action that plays multiple spins.
        /// </summary>
        /// <param name="startTime">The time when this action should start, in minutes from midnight (0-1439).</param>
        /// <param name="machinePosition">The world position of the slot machine to use.</param>
        /// <param name="spinCount">The number of spins to play.</param>
        /// <param name="betAmount">The amount to bet per spin in dollars (default: 10).</param>
        /// <param name="timeBetweenSpins">Time to wait between spins in seconds (default: 10.0).</param>
        /// <param name="maxSearchDistance">Maximum distance to search for a slot machine from the position (default: 5.0).</param>
        /// <param name="name">Optional custom name for this action; defaults to "UseSlotMachine".</param>
        /// <returns>This builder instance for method chaining.</returns>
        /// <remarks>
        /// The NPC will play the specified number of spins, waiting between each one.
        /// If the NPC runs out of cash before completing all spins, the session will end early.
        /// </remarks>
        public PrefabScheduleBuilder UseSlotMachineMultipleTimes(
            int startTime, 
            Vector3 machinePosition, 
            int spinCount,
            int betAmount = 10,
            float timeBetweenSpins = 10f,
            float maxSearchDistance = 5f, 
            string name = null)
        {
            _specs.Add(new UseSlotMachineSpec 
            { 
                StartTime = startTime, 
                MachinePosition = machinePosition, 
                BetAmount = betAmount,
                SessionMode = GamblingSessionMode.SpinCount,
                SpinCount = spinCount,
                TimeBetweenSpins = timeBetweenSpins,
                MaxSearchDistance = maxSearchDistance,
                Name = name 
            });
            return this;
        }

        /// <summary>
        /// Adds a slot machine usage action that plays until a specific time.
        /// </summary>
        /// <param name="startTime">The time when this action should start, in minutes from midnight (0-1439).</param>
        /// <param name="endTime">The time when gambling should stop, in minutes from midnight (0-1439).</param>
        /// <param name="machinePosition">The world position of the slot machine to use.</param>
        /// <param name="betAmount">The amount to bet per spin in dollars (default: 10).</param>
        /// <param name="timeBetweenSpins">Time to wait between spins in seconds (default: 10.0).</param>
        /// <param name="stopIfBroke">If true, stops gambling when out of cash; if false, only stops at end time (default: true).</param>
        /// <param name="maxSearchDistance">Maximum distance to search for a slot machine from the position (default: 5.0).</param>
        /// <param name="name">Optional custom name for this action; defaults to "UseSlotMachine".</param>
        /// <returns>This builder instance for method chaining.</returns>
        /// <remarks>
        /// The NPC will gamble continuously until the end time is reached.
        /// If <paramref name="stopIfBroke"/> is true, the session will also end if the NPC runs out of cash.
        /// </remarks>
        public PrefabScheduleBuilder UseSlotMachineUntilTime(
            int startTime,
            int endTime,
            Vector3 machinePosition, 
            int betAmount = 10,
            float timeBetweenSpins = 10f,
            bool stopIfBroke = true,
            float maxSearchDistance = 5f,
            Map.Building building = null,
            string name = null)
        {
            _specs.Add(new UseSlotMachineSpec 
            { 
                StartTime = startTime, 
                MachinePosition = machinePosition, 
                BetAmount = betAmount,
                SessionMode = stopIfBroke ? 
                    GamblingSessionMode.UntilTimeOrBroke : 
                    GamblingSessionMode.UntilTime,
                EndTime = endTime,
                TimeBetweenSpins = timeBetweenSpins,
                MaxSearchDistance = maxSearchDistance,
                Building = building,
                Name = name 
            });
            return this;
        }

        /// <summary>
        /// Adds a slot machine usage action that plays until the NPC runs out of cash.
        /// </summary>
        /// <param name="startTime">The time when this action should start, in minutes from midnight (0-1439).</param>
        /// <param name="machinePosition">The world position of the slot machine to use.</param>
        /// <param name="betAmount">The amount to bet per spin in dollars (default: 10).</param>
        /// <param name="timeBetweenSpins">Time to wait between spins in seconds (default: 10.0).</param>
        /// <param name="maxSearchDistance">Maximum distance to search for a slot machine from the position (default: 5.0).</param>
        /// <param name="name">Optional custom name for this action; defaults to "UseSlotMachine".</param>
        /// <returns>This builder instance for method chaining.</returns>
        /// <remarks>
        /// The NPC will gamble continuously until they no longer have enough cash for another bet.
        /// This can result in the NPC gambling away all their money, so ensure they have a reasonable
        /// amount of cash or use time-based limits instead.
        /// </remarks>
        public PrefabScheduleBuilder UseSlotMachineUntilBroke(
            int startTime,
            Vector3 machinePosition, 
            int betAmount = 10,
            float timeBetweenSpins = 10f,
            float maxSearchDistance = 5f, 
            string name = null)
        {
            _specs.Add(new UseSlotMachineSpec 
            { 
                StartTime = startTime, 
                MachinePosition = machinePosition, 
                BetAmount = betAmount,
                SessionMode = GamblingSessionMode.UntilBroke,
                TimeBetweenSpins = timeBetweenSpins,
                MaxSearchDistance = maxSearchDistance,
                Name = name 
            });
            return this;
        }

        /// <summary>
        /// Adds an ATM usage action at the specified time.
        /// </summary>
        /// <param name="startTime">The time when this action should start, in minutes from midnight (0-1439).</param>
        /// <param name="atmGUID">Optional GUID of a specific ATM to use; if null, the ATM should be resolved by gameplay systems.</param>
        /// <param name="name">Optional custom name for this action; defaults to "UseATM".</param>
        /// <returns>This builder instance for method chaining.</returns>
        /// <remarks>
        /// Creates a <see cref="UseATMSpec"/> that, when applied, configures a
        /// <see cref="S1NPCsSchedules.NPCSignal_UseATM"/> under the NPC's schedule manager.
        /// </remarks>
        public PrefabScheduleBuilder UseATM(int startTime, string atmGUID = null, string name = null)
        {
            _specs.Add(new UseATMSpec { StartTime = startTime, ATMGUID = atmGUID, Name = name });
            return this;
        }

        /// <summary>
        /// Adds a location-dialogue action that moves to a destination and enables dialogue.
        /// </summary>
        /// <param name="destination">The world position where the NPC should walk to.</param>
        /// <param name="startTime">The time when this action should start, in minutes from midnight (0-1439).</param>
        /// <param name="faceDestinationDir">Whether the NPC should face the destination direction when walking. Default is <c>true</c>.</param>
        /// <param name="within">The distance threshold (in world units) within which the NPC is considered to have arrived. Default is 1.0f.</param>
        /// <param name="warpIfSkipped">Whether to warp the NPC to the destination if the action is skipped. Default is <c>false</c>.</param>
        /// <param name="greetingOverrideToEnable">Greeting override index to enable upon arrival; use -1 to disable. Default is -1.</param>
        /// <param name="choiceToEnable">Choice index to enable upon arrival; use -1 to disable. Default is -1.</param>
        /// <param name="name">Optional custom name for this action; defaults to "LocationDialogue".</param>
        /// <returns>This builder instance for method chaining.</returns>
        /// <remarks>
        /// Creates a <see cref="LocationDialogueSpec"/> that, when applied, configures a
        /// <see cref="S1NPCsSchedules.NPCEvent_LocationDialogue"/>. The NPC walks to the destination and
        /// then sets dialogue-related overrides for player interaction.
        /// </remarks>
        public PrefabScheduleBuilder LocationDialogue(Vector3 destination, int startTime, bool faceDestinationDir = true, float within = 1f, bool warpIfSkipped = false, int greetingOverrideToEnable = -1, int choiceToEnable = -1, string name = null)
        {
            _specs.Add(new LocationDialogueSpec
            {
                Destination = destination,
                Forward = null,
                StartTime = startTime,
                FaceDestinationDirection = faceDestinationDir,
                Within = within,
                WarpIfSkipped = warpIfSkipped,
                GreetingOverrideToEnable = greetingOverrideToEnable,
                ChoiceToEnable = choiceToEnable,
                Name = name
            });
            return this;
        }

        /// <summary>
        /// Starts building a location-based action that moves to a destination, then triggers a typed arrive behavior.
        /// </summary>
        /// <param name="destination">The world position where the NPC should walk to.</param>
        /// <param name="startTime">The time when this action should start, in minutes from midnight (0-1439).</param>
        /// <param name="durationMinutes">The duration for this action in minutes. Default is 60.</param>
        /// <returns>A fluent sub-builder used to configure and finalize the action.</returns>
        /// <remarks>
        /// Use the returned <see cref="LocationBasedActionSpecBuilder"/> to configure optional values such as
        /// destination threshold and warp behavior, then call a terminal method like
        /// <see cref="LocationBasedActionSpecBuilder.OnArriveSmokeBreak"/> to finalize and add the spec.
        /// </remarks>
        public LocationBasedActionSpecBuilder LocationBased(Vector3 destination, int startTime, int durationMinutes = 60)
        {
            return new LocationBasedActionSpecBuilder(this, destination, startTime, durationMinutes);
        }

        /// <summary>
        /// Adds a handle-deal action for dealer-type NPCs.
        /// </summary>
        /// <param name="startTime">The time when this action should start, in minutes from midnight (0-1439).</param>
        /// <param name="name">Optional custom name for this action; defaults to "HandleDeal".</param>
        /// <returns>This builder instance for method chaining.</returns>
        /// <remarks>
        /// As of v0.4.2f4, deal handling is now automatic through the DealerAttendDealBehaviour system.
        /// This method is kept for backwards compatibility but is a no-op. Dealer NPCs set up with
        /// EnsureDealer() will automatically handle deals when contracts are assigned.
        /// </remarks>
        [System.Obsolete("HandleDeal is no longer needed as of game version 0.4.2f4. Deal handling is now automatic through DealerAttendDealBehaviour.")]
        public PrefabScheduleBuilder HandleDeal(int startTime, string name = null)
        {
            _specs.Add(new HandleDealSpec { StartTime = startTime, Name = name });
            return this;
        }

        /// <summary>
        /// Adds a "Stay in Building" action that makes the NPC remain inside a building for a specified duration.
        /// </summary>
        /// <param name="building">The building wrapper where the NPC should stay.</param>
        /// <param name="startTime">The time when this action should start, in minutes from midnight (0-1439).
        /// <strong>Avoid using 0</strong> to prevent sort comparison issues.</param>
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
        public PrefabScheduleBuilder StayInBuilding(Map.Building building, int startTime, int durationMinutes = 60, int? doorIndex = null, string name = null)
        {
            if (building == null)
            {
                Logger.Error($"StayInBuilding called with null building at time {startTime}. Action will not be added to schedule.");
                return this;
            }

            string buildingName = building.Name;
            if (string.IsNullOrEmpty(buildingName))
            {
                Logger.Error($"StayInBuilding called with building that has null/empty Name at time {startTime}. Building wrapper exists but Name property is invalid.");
                return this;
            }

            _specs.Add(new StayInBuildingSpec
            {
                BuildingName = buildingName,
                BuildingIdentifierType = building.DeferredIdentifierType,
                StartTime = startTime,
                DurationMinutes = durationMinutes,
                DoorIndex = doorIndex,
                Name = name
            });
            
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

        /// <summary>
        /// Adds a "Drive to Car Park" action using GameObject names for runtime resolution.
        /// More reliable than GUIDs for modders as names are more predictable across different players.
        /// </summary>
        /// <param name="parkingLotName">The GameObject name of the parking lot where the vehicle should be parked.</param>
        /// <param name="vehicleName">The GameObject name of the vehicle that should be driven.</param>
        /// <param name="startTime">The time when this action should start, in minutes from midnight (0-1439).</param>
        /// <param name="alignment">The optional parking alignment to use when parking the vehicle.</param>
        /// <param name="overrideParkingType">Whether to override the default parking type behavior.</param>
        /// <param name="name">The optional name for this action. If <c>null</c>, uses "DriveToCarPark".</param>
        /// <returns>This builder instance for method chaining.</returns>
        /// <remarks>
        /// This method creates a <see cref="DriveToCarParkSpec"/> that uses name-based resolution.
        /// The parking lot and vehicle will be found by GameObject name at runtime, which is more
        /// reliable than GUID-based lookup for mods that need to work across different players.
        /// </remarks>
        public PrefabScheduleBuilder DriveToCarParkByName(string parkingLotName, string vehicleName, int startTime, ParkingAlignment? alignment = null, bool? overrideParkingType = null, string name = null)
        {
            _specs.Add(new DriveToCarParkSpec
            {
                StartTime = startTime,
                ParkingLotName = parkingLotName,
                VehicleName = vehicleName,
                Alignment = alignment,
                OverrideParkingType = overrideParkingType,
                Name = name
            });
            return this;
        }

        /// <summary>
        /// Adds a "Drive to Car Park" action that creates a vehicle using a vehicle code.
        /// Useful when you don't have an existing vehicle to reference.
        /// </summary>
        /// <param name="parkingLotName">The GameObject name of the parking lot where the vehicle should be parked.</param>
        /// <param name="vehicleCode">The vehicle code to create (e.g., "Sedan", "SUV", etc.).</param>
        /// <param name="startTime">The time when this action should start, in minutes from midnight (0-1439).</param>
        /// <param name="vehicleSpawnPosition">The world position where the vehicle should spawn.</param>
        /// <param name="vehicleSpawnRotation">The optional rotation for the vehicle spawn. If not specified, uses identity rotation.</param>
        /// <param name="alignment">The optional parking alignment to use when parking the vehicle.</param>
        /// <param name="overrideParkingType">Whether to override the default parking type behavior.</param>
        /// <param name="name">The optional name for this action. If <c>null</c>, uses "DriveToCarPark".</param>
        /// <returns>This builder instance for method chaining.</returns>
        /// <remarks>
        /// This method creates a <see cref="DriveToCarParkSpec"/> that will create a new vehicle
        /// instance at runtime using the provided vehicle code. This is useful when you don't
        /// have an existing vehicle to reference but need the NPC to drive somewhere.
        /// 
        /// The vehicle will be spawned at the specified <paramref name="vehicleSpawnPosition"/> and rotation
        /// when the schedule action executes.
        /// </remarks>
        public PrefabScheduleBuilder DriveToCarParkWithCreateVehicle(string parkingLotName, string vehicleCode, int startTime, Vector3 vehicleSpawnPosition, Quaternion? vehicleSpawnRotation = null, ParkingAlignment? alignment = null, bool? overrideParkingType = null, string name = null)
        {
            _specs.Add(new DriveToCarParkSpec
            {
                StartTime = startTime,
                ParkingLotName = parkingLotName,
                VehicleCode = vehicleCode,
                VehicleSpawnPosition = vehicleSpawnPosition,
                VehicleSpawnRotation = vehicleSpawnRotation,
                Alignment = alignment,
                OverrideParkingType = overrideParkingType,
                Name = name
            });
            return this;
        }
    }
}



#if (IL2CPPMELON)
using S1NPCs = Il2CppScheduleOne.NPCs;
using S1NPCsSchedules = Il2CppScheduleOne.NPCs.Schedules;
using S1DevUtilities = Il2CppScheduleOne.DevUtilities;
using S1GameTime = Il2CppScheduleOne.GameTime;
using MelonLoader;
#elif (MONOMELON || MONOBEPINEX || IL2CPPBEPINEX)
using S1NPCs = ScheduleOne.NPCs;
using S1NPCsSchedules = ScheduleOne.NPCs.Schedules;
using S1DevUtilities = ScheduleOne.DevUtilities;
using S1GameTime = ScheduleOne.GameTime;
#endif

using UnityEngine;
using S1API.Casino;
using S1API.GameTime;
using S1API.Internal.Utils;
using S1API.Map;
using S1API.Map.Buildings;
using S1API.Logging;

namespace S1API.Entities.Schedule
{
    /// <summary>
    /// Specifies an action that makes an NPC use a slot machine at a scheduled time.
    /// Supports single spins, multiple spins, time-based sessions, or gambling until broke.
    /// </summary>
    /// <remarks>
    /// This action creates a custom schedule entry that makes the NPC walk to a slot machine
    /// location and play it according to the specified session mode. The NPC will use cash from
    /// their inventory, and any winnings will be added back to their inventory.
    /// </remarks>
    public sealed class UseSlotMachineSpec : IScheduleActionSpec
    {
        private static readonly Log Logger = new Log("UseSlotMachineSpec");
        /// <summary>
        /// Gets or sets the time when this action should start, in minutes from midnight.
        /// </summary>
        /// <value>The start time in minutes (0-1439 for a 24-hour day).</value>
        public int StartTime { get; set; }
        
        /// <summary>
        /// Gets or sets the world position of the slot machine to use.
        /// </summary>
        /// <value>The 3D position of the slot machine in world space.</value>
        /// <remarks>
        /// The NPC will search for the nearest slot machine to this position.
        /// Ensure the position is accurate to avoid the NPC using the wrong machine.
        /// </remarks>
        public Vector3 MachinePosition { get; set; }
        
        /// <summary>
        /// Gets or sets the bet amount in dollars.
        /// </summary>
        /// <value>The amount to bet per spin, in dollars.</value>
        /// <remarks>
        /// Common bet amounts are 5, 10, 25, 50, or 100. The NPC must have this amount
        /// in their inventory (as cash items) to use the machine.
        /// </remarks>
        public int BetAmount { get; set; } = 10;
        
        /// <summary>
        /// Gets or sets the gambling session mode.
        /// </summary>
        /// <value>The session mode that determines how long the NPC gambles. Default is <see cref="GamblingSessionMode.SingleSpin"/>.</value>
        /// <remarks>
        /// <para><see cref="GamblingSessionMode.SingleSpin"/>: Play once and stop.</para>
        /// <para><see cref="GamblingSessionMode.SpinCount"/>: Play until <see cref="SpinCount"/> spins are completed.</para>
        /// <para><see cref="GamblingSessionMode.UntilTime"/>: Play until <see cref="EndTime"/> is reached.</para>
        /// <para><see cref="GamblingSessionMode.UntilBroke"/>: Play until the NPC can't afford another bet.</para>
        /// <para><see cref="GamblingSessionMode.UntilTimeOrBroke"/>: Play until <see cref="EndTime"/> OR out of cash.</para>
        /// </remarks>
        public GamblingSessionMode SessionMode { get; set; } = GamblingSessionMode.SingleSpin;
        
        /// <summary>
        /// Gets or sets the number of spins to play when using <see cref="GamblingSessionMode.SpinCount"/>.
        /// </summary>
        /// <value>The number of spins. Default is 1.</value>
        public int SpinCount { get; set; } = 1;
        
        /// <summary>
        /// Gets or sets the end time in minutes from midnight when using time-based session modes.
        /// </summary>
        /// <value>The end time in minutes (0-1439 for a 24-hour day). Default is 0.</value>
        /// <remarks>
        /// Used with <see cref="GamblingSessionMode.UntilTime"/> and <see cref="GamblingSessionMode.UntilTimeOrBroke"/>.
        /// </remarks>
        public int EndTime { get; set; }
        
        /// <summary>
        /// Gets or sets the time to wait between spins in seconds.
        /// </summary>
        /// <value>The delay between spins in seconds. Default is 10.0 seconds.</value>
        /// <remarks>
        /// This delay occurs after the slot machine finishes spinning and displaying results,
        /// before the NPC starts the next spin. This makes the gambling behavior more realistic.
        /// </remarks>
        public float TimeBetweenSpins { get; set; } = 10f;
        
        /// <summary>
        /// Gets or sets the maximum search distance from the machine position.
        /// </summary>
        /// <value>The search radius in world units. Default is 5.0.</value>
        public float MaxSearchDistance { get; set; } = 5f;
        
        /// <summary>
        /// Gets or sets the optional name for this action.
        /// </summary>
        /// <value>The action name, or <c>null</c> to use the default name "UseSlotMachine".</value>
        public string Name { get; set; }
        
        /// <summary>
        /// Gets or sets the optional building that contains the slot machine.
        /// </summary>
        /// <value>The building wrapper, or <c>null</c> to auto-detect or skip building entry.</value>
        /// <remarks>
        /// If specified, the NPC will first enter this building before walking to the slot machine.
        /// This is useful when the slot machine is inside a building and the NPC needs to enter first.
        /// If not specified, the system will attempt to pathfind directly to the slot machine position.
        /// </remarks>
        public Map.Building Building { get; set; }

        void IScheduleActionSpec.ApplyTo(NPCSchedule schedule)
        {
            var npc = schedule.NPC;
            if (npc == null)
            {
                Logger.Warning("ApplyTo called with null NPC");
                return;
            }
            
            // Check current time to handle late loads (when save is loaded after StartTime but before EndTime)
            var timeManager = S1DevUtilities.NetworkSingleton<S1GameTime.TimeManager>.Instance;
            int currentTime = timeManager != null ? timeManager.CurrentTime : -1;
            bool isStartTimePassed = currentTime >= 0 && currentTime >= StartTime;
            bool isStillInWindow = false;
            
            if (isStartTimePassed && timeManager != null)
            {
                // Check if we're still within the gambling window
                if (SessionMode == GamblingSessionMode.UntilTime || SessionMode == GamblingSessionMode.UntilTimeOrBroke)
                {
                    if (EndTime > StartTime)
                    {
                        isStillInWindow = currentTime < EndTime;
                    }
                    else
                    {
                        // Handle wrap-around (e.g., StartTime=1630, EndTime=2300, but day wraps)
                        isStillInWindow = currentTime < EndTime || currentTime >= StartTime;
                    }
                }
                else
                {
                    // For other modes, if start time passed, we can still try
                    isStillInWindow = true;
                }
                
                if (!isStillInWindow)
                {
                    return;
                }
            }

            // Calculate effective start time (use current time if start time has passed, otherwise use original start time)
            int effectiveStartTime = isStartTimePassed && isStillInWindow ? Mathf.Max(1, currentTime) : StartTime;

            // If a building is specified, first make the NPC enter it
            // This ensures the NPC is inside before trying to walk to the slot machine
            if (Building != null)
            {
                // Add a StayInBuilding action that starts slightly before the slot machine action
                // This gives the NPC time to enter the building
                int buildingStartTime = Mathf.Max(1, effectiveStartTime - 5); // Start 5 minutes earlier, minimum 1
                var stayAction = schedule.AddActionInternal<S1NPCsSchedules.NPCEvent_StayInBuilding>(
                    buildingStartTime, 
                    string.IsNullOrEmpty(Name) ? "EnterBuildingForSlotMachine" : Name + "_EnterBuilding");
                
                if (stayAction != null)
                {
                    var gameBuilding = Building.ResolveGameBuilding();
                    if (gameBuilding != null)
                    {
                        // Calculate duration to cover the entire gambling session
                        int durationMinutes = 60; // Default 1 hour
                        if (SessionMode == GamblingSessionMode.UntilTime || SessionMode == GamblingSessionMode.UntilTimeOrBroke)
                        {
                            durationMinutes = EndTime > StartTime 
                                ? EndTime - buildingStartTime 
                                : (1440 - buildingStartTime) + EndTime; // Handle wrap-around
                        }
                        else
                        {
                            // For other modes, estimate duration (e.g., 1 hour for safety)
                            durationMinutes = 60;
                        }
                        
                        stayAction.Duration = Mathf.Max(1, durationMinutes);
                        
#if (IL2CPPMELON || IL2CPPBEPINEX)
                        var endTime = Il2CppScheduleOne.GameTime.TimeManager.AddMinutesTo24HourTime(buildingStartTime, stayAction.Duration);
#elif (MONOMELON || MONOBEPINEX)
                        var endTime = ScheduleOne.GameTime.TimeManager.AddMinutesTo24HourTime(buildingStartTime, stayAction.Duration);
#endif
                        ReflectionUtils.TrySetFieldOrProperty(stayAction, "EndTime", endTime);
                        ReflectionUtils.TrySetFieldOrProperty(stayAction, "Building", gameBuilding);
                    }
                    else
                    {
                        Logger.Warning($"[{npc.ID}] Failed to resolve game building: {Building.Name}");
                    }
                }
                else
                {
                    Logger.Warning($"[{npc.ID}] Failed to create StayInBuilding action");
                }
            }

            // Verify the destination is reachable, or find a reachable slot machine
            Vector3 targetPosition = MachinePosition;
            if (!npc.Movement.CanGetTo(targetPosition))
            {
                Logger.Warning($"[{npc.ID}] Initial position not reachable, searching for nearest slot machine");
                // Try to find the nearest reachable slot machine
                var machine = SlotMachineHelper.FindNearestSlotMachine(targetPosition, MaxSearchDistance * 2f);
                if (machine != null)
                {
                    // Check if this machine is reachable
                    if (npc.Movement.CanGetTo(machine.transform.position))
                    {
                        targetPosition = machine.transform.position;
                    }
                    else
                    {
                        // Can't find any reachable slot machine - abort
                        Logger.Error($"[{npc.ID}] Found slot machine but it's not reachable, aborting");
                        return;
                    }
                }
                else
                {
                    // No slot machine found - abort
                    Logger.Error($"[{npc.ID}] No slot machine found near position, aborting");
                    return;
                }
            }

            // Create a destination marker transform for the slot machine position
            var destinationTransform = NPCDestinationContainer.CreateDestinationMarker(
                npc.gameObject.name, 
                "SlotMachineDestination", 
                targetPosition, 
                null); // No specific forward direction needed

            if (destinationTransform == null)
            {
                // Can't create destination marker - abort
                Logger.Error($"[{npc.ID}] Failed to create destination marker, aborting");
                return;
            }

            var walkAction = schedule.AddActionInternal<S1NPCsSchedules.NPCSignal_WalkToLocation>(
                effectiveStartTime, 
                string.IsNullOrEmpty(Name) ? "WalkToSlotMachine" : Name + "_Walk");
            
            if (walkAction == null)
            {
                Logger.Error($"[{npc.ID}] Failed to create WalkTo action, aborting");
                return;
            }

            walkAction.Destination = destinationTransform;
            walkAction.FaceDestinationDir = true;
            walkAction.DestinationThreshold = 2f; // Allow NPC to be within 2 units of slot machine
            // If start time passed, allow warping if skipped to ensure NPC gets to slot machine
            walkAction.WarpIfSkipped = isStartTimePassed && isStillInWindow;

            // Start a coroutine that waits for arrival, then starts gambling
#if (IL2CPPMELON)
            MelonCoroutines.Start(WaitForArrivalThenGamble(npc, targetPosition, BetAmount, MaxSearchDistance, 
                SessionMode, SpinCount, EndTime, TimeBetweenSpins, effectiveStartTime));
#else
            S1DevUtilities.Singleton<S1DevUtilities.CoroutineService>.Instance.StartCoroutine(
                WaitForArrivalThenGamble(npc, targetPosition, BetAmount, MaxSearchDistance, 
                    SessionMode, SpinCount, EndTime, TimeBetweenSpins, effectiveStartTime));
#endif
        }

        private static System.Collections.IEnumerator WaitForArrivalThenGamble(
            NPC npc, 
            Vector3 targetPosition, 
            int bet, 
            float maxDistance,
            GamblingSessionMode mode,
            int spinCount,
            int endTime,
            float timeBetweenSpins,
            int startTime)
        {
            // Wait until the start time is reached (only if it hasn't passed yet)
            var timeManager = S1DevUtilities.NetworkSingleton<S1GameTime.TimeManager>.Instance;
            int currentTime = timeManager != null ? timeManager.CurrentTime : -1;
            
            if (currentTime >= 0 && currentTime < startTime)
            {
                while (timeManager != null && timeManager.CurrentTime < startTime)
                {
                    yield return new WaitForSeconds(1f);
                }
            }

            // Wait for NPC to arrive at the slot machine position
            // Check if NPC can pathfind to the location first
            if (npc != null && npc.gameObject != null && !npc.Movement.CanGetTo(targetPosition))
            {
                Logger.Warning($"[{npc.ID}] NPC can't pathfind to target, searching for alternative");
                // NPC can't reach the destination - try to find the nearest slot machine instead
                var machine = SlotMachineHelper.FindNearestSlotMachine(targetPosition, maxDistance * 2f);
                if (machine != null)
                {
                    targetPosition = machine.transform.position;
                }
                else
                {
                    // Can't find any reachable slot machine, abort
                    Logger.Error($"[{npc.ID}] No alternative slot machine found, aborting");
                    yield break;
                }
            }

            float arrivalThreshold = 2f; // Distance threshold for "arrived"
            int maxWaitTime = 300; // Max 5 minutes of waiting
            int waitedSeconds = 0;
            
            while (npc != null && npc.gameObject != null && waitedSeconds < maxWaitTime)
            {
                float distance = Vector3.Distance(npc.Movement.FootPosition, targetPosition);
                bool isMoving = npc.Movement.IsMoving;
                
                if (distance <= arrivalThreshold && !isMoving)
                    break;
                
                yield return new WaitForSeconds(1f);
                waitedSeconds++;
            }

            if (waitedSeconds >= maxWaitTime)
            {
                Logger.Warning($"[{npc?.ID}] Max wait time reached ({maxWaitTime}s), proceeding anyway");
            }

            // If NPC arrived (or we gave up waiting), start gambling
            if (npc != null && npc.gameObject != null)
            {
                yield return GamblingSession(npc, targetPosition, bet, maxDistance, mode, spinCount, endTime, timeBetweenSpins);
            }
            else
            {
                Logger.Error($"[{npc?.ID}] NPC is null when trying to start gambling session");
            }
        }

        private static System.Collections.IEnumerator GamblingSessionDelayed(
            NPC npc, 
            Vector3 position, 
            int bet, 
            float maxDistance,
            GamblingSessionMode mode,
            int spinCount,
            int endTime,
            float timeBetweenSpins,
            int startTime)
        {
            // Wait until the start time is reached
            var timeManager = S1DevUtilities.NetworkSingleton<S1GameTime.TimeManager>.Instance;
            while (timeManager != null && timeManager.CurrentTime < startTime)
            {
                yield return new WaitForSeconds(1f);
            }

            // Start the gambling session
            yield return GamblingSession(npc, position, bet, maxDistance, mode, spinCount, endTime, timeBetweenSpins);
        }

        private static System.Collections.IEnumerator GamblingSession(
            NPC npc, 
            Vector3 position, 
            int bet, 
            float maxDistance,
            GamblingSessionMode mode,
            int spinCount,
            int endTime,
            float timeBetweenSpins)
        {
            yield return new WaitForSeconds(1f);

            // Ensure NPC has cash if random cash is configured but they don't have any
            if (npc != null && npc.S1NPC?.Inventory != null)
            {
                float currentCash = SlotMachineHelper.GetNPCCash(npc);
                var inventory = npc.S1NPC.Inventory;
                
                // Check if random cash is configured but NPC has no cash
                bool randomCash = ReflectionUtils.TryGetFieldOrProperty(inventory, "RandomCash") is bool enabled && enabled;
                int randomCashMin = ReflectionUtils.TryGetFieldOrProperty(inventory, "RandomCashMin") is int min ? min : 0;
                int randomCashMax = ReflectionUtils.TryGetFieldOrProperty(inventory, "RandomCashMax") is int max ? max : 0;
                if (currentCash == 0 && randomCash && randomCashMax > 0)
                {
                    int cashToAdd = UnityEngine.Random.Range(
                        Mathf.Max(randomCashMin, bet), // At least enough for one bet
                        randomCashMax + 1);
                    
                    SlotMachineHelper.AddNPCCash(npc, cashToAdd);
                    
                    // Wait a frame for insertion to complete
                    yield return null;
                }
            }

            int completedSpins = 0;
            var timeManager = S1DevUtilities.NetworkSingleton<S1GameTime.TimeManager>.Instance;
            int loopIteration = 0;

            while (true)
            {
                loopIteration++;

                if (SlotMachineHelper.IsSceneTransitionInProgress)
                    yield break;
                
                // Check if NPC is still valid
                if (npc == null || npc.gameObject == null)
                {
                    if (!SlotMachineHelper.IsSceneTransitionInProgress)
                        Logger.Warning($"[{npc?.ID}] NPC is null in gambling session loop, breaking");
                    yield break;
                }

                float npcCash = SlotMachineHelper.GetNPCCash(npc);
                int currentTime = timeManager != null ? timeManager.CurrentTime : -1;
                
                // Check session end conditions based on mode
                bool shouldContinue = mode switch
                {
                    GamblingSessionMode.SingleSpin => completedSpins < 1,
                    GamblingSessionMode.SpinCount => completedSpins < spinCount,
                    GamblingSessionMode.UntilTime => timeManager != null && currentTime < endTime,
                    GamblingSessionMode.UntilBroke => npcCash >= bet,
                    GamblingSessionMode.UntilTimeOrBroke => 
                        (timeManager != null && currentTime < endTime) && 
                        npcCash >= bet,
                    _ => false
                };

                if (!shouldContinue)
                {
                    yield break;
                }

                // Check if NPC has enough cash
                if (npcCash < bet)
                    yield break;

                // Use the slot machine
                bool success = SlotMachineHelper.UseSlotMachine(npc, position, bet, maxDistance);
                if (!success)
                {
                    if (SlotMachineHelper.IsSceneTransitionInProgress)
                        yield break;

                    // Failed to use machine (might be occupied or not found)
                    Logger.Warning($"[{npc.ID}] Failed to use slot machine (attempt {loopIteration}), will retry in 5s");
                    // Wait a bit and try again
                    yield return new WaitForSeconds(5f);
                    continue;
                }

                completedSpins++;

                // Wait between spins (only if we're continuing)
                if (mode != GamblingSessionMode.SingleSpin || completedSpins < 1)
                    yield return new WaitForSeconds(timeBetweenSpins);
            }
        }
    }
}



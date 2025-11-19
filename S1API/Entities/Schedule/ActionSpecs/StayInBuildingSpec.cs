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
using S1API.Internal.Utils;
using S1API.Internal.Map;
using S1API.Logging;
using System;
using System.Reflection;
using System.Collections;

namespace S1API.Entities.Schedule
{
    /// <summary>
    /// Specifies an action that makes an NPC remain inside a building for a specified duration.
    /// </summary>
    /// <remarks>
    /// This action creates a <see cref="S1NPCsSchedules.NPCEvent_StayInBuilding"/> that will
    /// keep the NPC inside the specified building for the given duration. The building can
    /// be identified by name-based lookup.
    /// </remarks>
    public sealed class StayInBuildingSpec : IScheduleActionSpec
    {
        private static readonly Log Logger = new Log("StayInBuildingSpec");
        private static readonly System.Collections.Generic.List<PendingAction> PendingActions = new System.Collections.Generic.List<PendingAction>();

        private sealed class PendingAction
        {
            public S1NPCsSchedules.NPCEvent_StayInBuilding Action;
            public NPCSchedule Schedule;
            public string BuildingName;
            public int StartTime;
            public int? DoorIndex;
        }

        /// <summary>
        /// Gets or sets the GUID of the building where the NPC should stay.
        /// </summary>
        /// <value>The building GUID, or <c>null</c> if using name-based lookup.</value>
        /// <remarks>
        /// The building GUID is typically generated at runtime and may not be stable across game sessions.
        /// For modder-facing APIs prefer using name-based lookup via <see cref="Building.GetByName(string)"/>
        /// or typed identifiers via <see cref="Building.Get{T}()"/>. Use the GUID only if you have a
        /// reliable runtime reference to the exact game object.
        /// </remarks>
        public string BuildingGUID { get; set; }
        
        /// <summary>
        /// Gets or sets the name of the building where the NPC should stay.
        /// </summary>
        /// <value>The building name, or <c>null</c> if using GUID-based lookup.</value>
        /// <remarks>
        /// The building name takes precedence over <see cref="BuildingGUID"/> and is the
        /// recommended identifier for mod developers. It should match a building registered
        /// in the S1API building registry (see <see cref="Building.GetByName(string)"/> and
        /// <see cref="Building.Get{T}()"/>). Names are stable across game sessions and
        /// preferred for persistence and prefab configuration.
        /// </remarks>
        public string BuildingName { get; set; }

        /// <summary>
        /// INTERNAL: Gets or sets the building identifier type for deferred resolution.
        /// Set automatically when a deferred building wrapper is used.
        /// </summary>
        internal Type BuildingIdentifierType { get; set; }
        
        /// <summary>
        /// Gets or sets the time when this action should start, in minutes from midnight.
        /// </summary>
        /// <value>The start time in minutes (0-1439 for a 24-hour day).</value>
        public int StartTime { get; set; }
        
        /// <summary>
        /// Gets or sets the duration for which the NPC should remain in the building.
        /// </summary>
        /// <value>The duration in minutes. Default is 60 minutes.</value>
        /// <remarks>
        /// The NPC will stay in the building for this duration starting from the start time.
        /// Must be at least 1 minute.
        /// </remarks>
        public int DurationMinutes { get; set; } = 60;
        
        /// <summary>
        /// Gets or sets the optional door index to use when entering the building.
        /// </summary>
        /// <value>The door index, or <c>null</c> to use the default entrance.</value>
        /// <remarks>
        /// Some buildings have multiple entrances. This specifies which door the NPC
        /// should use when entering the building. If not specified, the default entrance is used.
        /// </remarks>
        public int? DoorIndex { get; set; }
        
        /// <summary>
        /// Gets or sets the optional name for this action.
        /// </summary>
        /// <value>The action name, or <c>null</c> to use the default name "StayInBuilding".</value>
        public string Name { get; set; }

        public void ApplyTo(NPCSchedule schedule)
        {
            var action = schedule.AddActionInternal<S1NPCsSchedules.NPCEvent_StayInBuilding>(StartTime, string.IsNullOrEmpty(Name) ? "StayInBuilding" : Name);
            if (action == null)
            {
                Logger.Warning($"Failed to create StayInBuilding action at time {StartTime} - AddActionInternal returned null");
                return;
            }

            action.Duration = Mathf.Max(1, DurationMinutes);
            
            // Calculate and set EndTime from StartTime + Duration
            // This is required for the action to properly start and end
#if (IL2CPPMELON || IL2CPPBEPINEX)
            var endTime = Il2CppScheduleOne.GameTime.TimeManager.AddMinutesTo24HourTime(StartTime, action.Duration);
#elif (MONOMELON || MONOBEPINEX)
            var endTime = ScheduleOne.GameTime.TimeManager.AddMinutesTo24HourTime(StartTime, action.Duration);
#endif
            ReflectionUtils.TrySetFieldOrProperty(action, "EndTime", endTime);

            // Resolve building using S1API.Map name-based registry
            object gameBuilding = null;
            Building wrapper = null;
            
            if (!string.IsNullOrEmpty(BuildingName))
            {
                // Try to get building wrapper
                wrapper = Building.GetByName(BuildingName);
                if (wrapper == null)
                {
                    Logger.Warning($"Building '{BuildingName}' not found in registry for StayInBuilding action at time {StartTime}. Will register deferred lookup.");
                    
                    // Try one more time after a brief delay - buildings might be registering asynchronously
                    // But first, check if we're in Main scene - if so, try direct lookup
                    if (!DeferredMapResolver.IsMenuScene())
                    {
                        // We're in Main scene, try to find building directly in scene
                        gameBuilding = TryFindBuildingInScene(BuildingName);
                        if (gameBuilding != null)
                        {
                            ApplyBuildingToAction(action, gameBuilding);
                            return;
                        }
                    }
                    
                    RegisterDeferredBuildingResolution(action, schedule);
                    return;
                }
                
                gameBuilding = wrapper.ResolveGameBuilding();
                if (gameBuilding == null)
                {
                    // Building wrapper exists but not resolved yet
                    if (wrapper.IsDeferred)
                    {
                        RegisterDeferredBuildingResolution(action, schedule, wrapper);
                        return;
                    }
                    else
                    {
                        // Wrapper exists but building isn't resolved - try direct scene lookup as fallback
                        Logger.Warning($"Building wrapper for '{BuildingName}' exists but ResolveGameBuilding returned null. Trying direct scene lookup.");
                        gameBuilding = TryFindBuildingInScene(BuildingName);
                        if (gameBuilding == null)
                        {
                            Logger.Error($"Failed to resolve game building for '{BuildingName}' (wrapper exists but ResolveGameBuilding returned null) at time {StartTime}");
                        }
                    }
                }
            }
            else if (!string.IsNullOrEmpty(BuildingGUID))
            {
                Logger.Warning($"StayInBuildingSpec at time {StartTime} has BuildingGUID set but BuildingName is empty. GUID-based lookup is not yet implemented.");
            }
            else
            {
                Logger.Error($"StayInBuildingSpec at time {StartTime} has neither BuildingName nor BuildingGUID set. Action will be created without a building reference.");
            }

			if (gameBuilding != null)
			{
				ApplyBuildingToAction(action, gameBuilding);
			}
			else
			{
				Logger.Warning($"StayInBuilding action at time {StartTime} was created but Building field is null. Will retry when building is registered.");
				
				// Register as pending action to retry when building is registered
				lock (PendingActions)
				{
					PendingActions.Add(new PendingAction
					{
						Action = action,
						Schedule = schedule,
						BuildingName = BuildingName,
						StartTime = StartTime,
						DoorIndex = DoorIndex
					});
				}
				
				// Try to resolve immediately in case building was just registered
				TryResolvePendingActions();
			}
        }

        /// <summary>
        /// INTERNAL: Attempts to resolve all pending StayInBuilding actions.
        /// Called when buildings are registered or when Main scene loads.
        /// </summary>
        internal static void TryResolvePendingActions()
        {
            lock (PendingActions)
            {
                if (PendingActions.Count == 0)
                    return;

                var resolved = new System.Collections.Generic.List<PendingAction>();
                
                for (int i = PendingActions.Count - 1; i >= 0; i--)
                {
                    var pending = PendingActions[i];
                    if (pending.Action == null || pending.Schedule == null)
                    {
                        resolved.Add(pending);
                        continue;
                    }

                    var wrapper = Building.GetByName(pending.BuildingName);
                    if (wrapper != null)
                    {
                        var gameBuilding = wrapper.ResolveGameBuilding();
                        if (gameBuilding != null)
                        {
                            // Create a temporary spec to apply the building
                            var spec = new StayInBuildingSpec
                            {
                                BuildingName = pending.BuildingName,
                                StartTime = pending.StartTime,
                                DoorIndex = pending.DoorIndex
                            };
                            spec.ApplyBuildingToAction(pending.Action, gameBuilding);
                            
                            // Re-initialize schedule to fix sorting
                            try
                            {
                                pending.Schedule.InitializeActions();
                            }
                            catch (Exception ex)
                            {
                                Logger.Warning($"Failed to re-initialize schedule after resolving pending building '{pending.BuildingName}': {ex.Message}");
                            }
                            
                            resolved.Add(pending);
                        }
                    }
                }

                // Remove resolved actions
                foreach (var r in resolved)
                {
                    PendingActions.Remove(r);
                }
            }
        }

        private void ApplyBuildingToAction(S1NPCsSchedules.NPCEvent_StayInBuilding action, object gameBuilding)
        {
            if (action == null)
            {
                Logger.Error($"Cannot apply building to null action for '{BuildingName}' at time {StartTime}");
                return;
            }

            if (gameBuilding == null)
            {
                Logger.Error($"Cannot apply null gameBuilding to action for '{BuildingName}' at time {StartTime}");
                return;
            }

            bool success = ReflectionUtils.TrySetFieldOrProperty(action, "Building", gameBuilding);
            if (!success)
            {
                Logger.Error($"Failed to set Building field on StayInBuilding action for '{BuildingName}' at time {StartTime}. Field may not exist or be inaccessible.");
            }
            else
            {
                // Verify it was actually set
                var verifyValue = ReflectionUtils.TryGetFieldOrProperty(action, "Building");
                if (verifyValue == null)
                {
                    Logger.Warning($"Building field appears to be null after setting for '{BuildingName}' at time {StartTime}");
                }
            }

            if (DoorIndex.HasValue)
            {
                var buildingType = gameBuilding.GetType();
                const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
                IList doorsList = null;
                var doorsField = buildingType.GetField("Doors", flags);
                if (doorsField != null)
                    doorsList = doorsField.GetValue(gameBuilding) as IList;
                if (doorsList == null)
                {
                    var doorsProp = buildingType.GetProperty("Doors", flags);
                    if (doorsProp != null)
                        doorsList = doorsProp.GetValue(gameBuilding) as IList;
                }
                if (doorsList != null && DoorIndex.Value >= 0 && DoorIndex.Value < doorsList.Count)
                {
                    ReflectionUtils.TrySetFieldOrProperty(action, "Door", doorsList[DoorIndex.Value]);
                }
                else if (DoorIndex.HasValue)
                {
                    Logger.Warning($"DoorIndex {DoorIndex.Value} is out of range for building '{BuildingName}' (has {doorsList?.Count ?? 0} doors)");
                }
            }
        }

        private void RegisterDeferredBuildingResolution(S1NPCsSchedules.NPCEvent_StayInBuilding action, NPCSchedule schedule, Building existingWrapper = null)
        {
            // If we have an identifier type, use typed lookup
            if (BuildingIdentifierType != null)
            {
                DeferredMapResolver.RegisterDeferredLookup(new DeferredLookup(BuildingIdentifierType, (resolved) =>
                {
                    if (resolved is Building building && building != null)
                    {
                        var gameBuilding = building.ResolveGameBuilding();
                        if (gameBuilding != null && action != null)
                        {
                            ApplyBuildingToAction(action, gameBuilding);
                            
                            // Re-initialize actions to ensure proper sorting now that building is set
                            try
                            {
                                schedule.InitializeActions();
                            }
                            catch (Exception ex)
                            {
                                Logger.Warning($"Failed to re-initialize schedule after building resolution: {ex.Message}");
                            }
                        }
                    }
                }));
            }
            // Otherwise, try to use the existing wrapper's deferred identifier type
            else if (existingWrapper != null && existingWrapper.DeferredIdentifierType != null)
            {
                DeferredMapResolver.RegisterDeferredLookup(new DeferredLookup(existingWrapper.DeferredIdentifierType, (resolved) =>
                {
                    if (resolved is Building building && building != null)
                    {
                        var gameBuilding = building.ResolveGameBuilding();
                        if (gameBuilding != null && action != null)
                        {
                            ApplyBuildingToAction(action, gameBuilding);
                            
                            // Re-initialize actions to ensure proper sorting now that building is set
                            try
                            {
                                schedule.InitializeActions();
                            }
                            catch (Exception ex)
                            {
                                Logger.Warning($"Failed to re-initialize schedule after building resolution: {ex.Message}");
                            }
                        }
                    }
                }));
            }
            // Fallback: periodically retry resolution by name
            else if (!string.IsNullOrEmpty(BuildingName))
            {
                // Register a name-based deferred lookup that will retry when Main scene loads
                // Note: DeferredMapResolver doesn't handle name-based lookups well, so we'll retry on next resolution attempt
            }
        }

        private object TryFindBuildingInScene(string buildingName)
        {
            try
            {
#if (IL2CPPMELON || IL2CPPBEPINEX)
                var arr = UnityEngine.Object.FindObjectsOfType<Il2CppScheduleOne.Map.NPCEnterableBuilding>(includeInactive: true);
#elif (MONOMELON || MONOBEPINEX)
                var arr = UnityEngine.Object.FindObjectsOfType<ScheduleOne.Map.NPCEnterableBuilding>(true);
#else
                var arr = Array.Empty<UnityEngine.Object>();
#endif
                for (int i = 0; i < arr.Length; i++)
                {
                    var b = arr[i];
                    if (b == null) continue;
                    var type = b.GetType();
                    var nameField = type.GetField("BuildingName", BindingFlags.Public | BindingFlags.Instance);
                    string name = nameField?.GetValue(b) as string;
                    if (string.IsNullOrEmpty(name))
                    {
                        var nameProp = type.GetProperty("BuildingName", BindingFlags.Public | BindingFlags.Instance);
                        name = nameProp?.GetValue(b) as string;
                    }
                    if (!string.IsNullOrEmpty(name) && string.Equals(name, buildingName, StringComparison.OrdinalIgnoreCase))
                    {
                        return b;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Warning($"Exception during direct building lookup for '{buildingName}': {ex.Message}");
            }
            return null;
        }
    }
}

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
                return;

            action.Duration = Mathf.Max(1, DurationMinutes);

            // Resolve building using S1API.Map name-based registry
            object gameBuilding = null;
            if (!string.IsNullOrEmpty(BuildingName))
            {
                var wrapper = Building.GetByName(BuildingName);
                gameBuilding = wrapper?.ResolveGameBuilding();
            }

			if (gameBuilding != null)
			{
				ReflectionUtils.TrySetFieldOrProperty(action, "Building", gameBuilding);

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
				}
			}
        }
    }
}

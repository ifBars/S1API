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
using System.Reflection;
using System.Collections;

namespace S1API.Entities.Schedule
{
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

            // Resolve building using S1API.Map name-based registry
            object gameBuilding = null;
            if (!string.IsNullOrEmpty(BuildingName))
            {
                var wrapper = Building.GetByName(BuildingName);
                gameBuilding = wrapper?.ResolveGameBuilding();
            }

			if (gameBuilding != null)
			{
				TrySetFieldOrProperty(action, "Building", gameBuilding);

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
						TrySetFieldOrProperty(action, "Door", doorsList[DoorIndex.Value]);
					}
				}
			}
        }

		private static bool TrySetFieldOrProperty(object target, string memberName, object value)
		{
			if (target == null) return false;
			var type = target.GetType();
			const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
			var fi = type.GetField(memberName, flags);
			if (fi != null)
			{
				if (value == null || fi.FieldType.IsInstanceOfType(value))
				{
					fi.SetValue(target, value);
					return true;
				}
			}
			var pi = type.GetProperty(memberName, flags);
			if (pi != null && pi.CanWrite)
			{
				if (value == null || pi.PropertyType.IsInstanceOfType(value))
				{
					pi.SetValue(target, value);
					return true;
				}
			}
			return false;
		}
    }
}

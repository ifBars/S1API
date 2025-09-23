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
    /// Remain inside a building for a duration window.
    /// </summary>
    public sealed class StayInBuildingSpec : IScheduleActionSpec
    {
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
            if (!string.IsNullOrEmpty(BuildingName))
            {
                var wrapper = Map.Buildings.GetByName(BuildingName);
                gameBuilding = wrapper?.ResolveGameBuilding();
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
}

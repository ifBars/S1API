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
    /// Defines a specification for a schedule action that can be applied to an NPC's schedule.
    /// This interface provides a modder-facing, game-type-free way to define schedule actions
    /// that can be used during prefab configuration.
    /// </summary>
    public interface IScheduleActionSpec
    {
        /// <summary>
        /// Applies this action specification to the given NPC schedule.
        /// </summary>
        /// <param name="schedule">The NPC schedule to apply this action to.</param>
        /// <remarks>
        /// This method should create and configure the appropriate schedule action
        /// on the provided schedule instance. The implementation should handle
        /// any necessary object resolution, validation, and error handling.
        /// </remarks>
        public void ApplyTo(NPCSchedule schedule);
    }
}

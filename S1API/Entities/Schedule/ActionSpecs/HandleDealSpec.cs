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

namespace S1API.Entities.Schedule
{
    /// <summary>
    /// Specifies an action that handles a customer deal handover at the active contract location.
    /// </summary>
    /// <remarks>
    /// Creates a <see cref="S1NPCsSchedules.NPCSignal_HandleDeal"/>. This action expects the owning NPC
    /// to be a Dealer and for gameplay systems to assign the active contract at runtime.
    /// </remarks>
    public sealed class HandleDealSpec : IScheduleActionSpec
    {
        /// <summary>
        /// The time when this action should start, in minutes from midnight.
        /// </summary>
        public int StartTime { get; set; }

        /// <summary>
        /// Optional custom name for the action.
        /// </summary>
        public string Name { get; set; }

        void IScheduleActionSpec.ApplyTo(NPCSchedule schedule)
        {
            schedule.AddActionInternal<S1NPCsSchedules.NPCSignal_HandleDeal>(StartTime, string.IsNullOrEmpty(Name) ? "HandleDeal" : Name);
        }
    }
}



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
    /// Specifies an action that ensures a customer deal signal exists under the NPC's schedule.
    /// </summary>
    /// <remarks>
    /// This action ensures that a <see cref="S1NPCsSchedules.NPCSignal_WaitForDelivery"/> component
    /// exists on the NPC's schedule manager. This signal is required for customer NPCs to
    /// properly handle deal interactions and handovers with the player.
    /// 
    /// The deal signal allows the NPC to wait for deliveries and toggle customer handover states.
    /// If the signal doesn't exist, it will be created and properly initialized with network
    /// components and wired to the customer component.
    /// </remarks>
    public sealed class EnsureDealSignalSpec : IScheduleActionSpec
    {
        /// <summary>
        /// Applies this specification to the given NPC schedule by ensuring a deal signal exists.
        /// </summary>
        /// <param name="schedule">The NPC schedule to ensure the deal signal on.</param>
        /// <remarks>
        /// This method calls <see cref="NPCSchedule.EnsureDealSignal"/> to create or activate
        /// the deal signal component. If the signal already exists, it will be properly
        /// initialized and wired to the customer component.
        /// </remarks>
        void IScheduleActionSpec.ApplyTo(NPCSchedule schedule)
        {
            schedule.EnsureDealSignal();
        }
    }
}

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
    /// Specifies an action that makes an NPC use a vending machine at a scheduled time.
    /// </summary>
    /// <remarks>
    /// This action creates a <see cref="S1NPCsSchedules.NPCSignal_UseVendingMachine"/> that will
    /// make the NPC interact with a vending machine. The machine can be specified by GUID,
    /// or if not specified, the NPC will use the nearest available vending machine.
    /// </remarks>
    public sealed class UseVendingMachineSpec : IScheduleActionSpec
    {
        /// <summary>
        /// Gets or sets the time when this action should start, in minutes from midnight.
        /// </summary>
        /// <value>The start time in minutes (0-1439 for a 24-hour day).</value>
        public int StartTime { get; set; }
        
        /// <summary>
        /// Gets or sets the GUID of the specific vending machine to use.
        /// </summary>
        /// <value>The vending machine GUID, or <c>null</c> to use the nearest machine.</value>
        /// <remarks>
        /// If specified, the NPC will use this specific vending machine. If not specified
        /// or if the machine cannot be found, the NPC will use the nearest available
        /// vending machine instead.
        /// </remarks>
        public string MachineGUID { get; set; }
        
        /// <summary>
        /// Gets or sets the optional name for this action.
        /// </summary>
        /// <value>The action name, or <c>null</c> to use the default name "UseVending".</value>
        public string Name { get; set; }

        void IScheduleActionSpec.ApplyTo(NPCSchedule schedule)
        {
            var action = schedule.AddActionInternal<S1NPCsSchedules.NPCSignal_UseVendingMachine>(StartTime, string.IsNullOrEmpty(Name) ? "UseVending" : Name);
            if (action == null)
                return;

            if (!string.IsNullOrEmpty(MachineGUID))
            {
                try
                {
#if MONOMELON
                    var guid = new System.Guid(MachineGUID);
#else
                    var guid = new Il2CppSystem.Guid(MachineGUID);
#endif
                    var machine = GUIDManager.GetObject<S1ObjectScripts.VendingMachine>(guid);
                    action.MachineOverride = machine;
                }
                catch { }
            }
        }
    }
}

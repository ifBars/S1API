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
    /// Use the nearest vending machine (or specific by GUID in future).
    /// </summary>
    public sealed class UseVendingMachineSpec : IScheduleActionSpec
    {
        public int StartTime { get; set; }
        public string MachineGUID { get; set; }
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

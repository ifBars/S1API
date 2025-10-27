#if (IL2CPPMELON)
using Il2Cpp;
using S1NPCs = Il2CppScheduleOne.NPCs;
using S1NPCsSchedules = Il2CppScheduleOne.NPCs.Schedules;
using S1Map = Il2CppScheduleOne.Map;
using S1Vehicles = Il2CppScheduleOne.Vehicles;
using S1VehiclesAI = Il2CppScheduleOne.Vehicles.AI;
using S1ObjectScripts = Il2CppScheduleOne.ObjectScripts;
using S1Money = Il2CppScheduleOne.Money;
#elif (MONOMELON || MONOBEPINEX || IL2CPPBEPINEX)
using S1NPCs = ScheduleOne.NPCs;
using S1NPCsSchedules = ScheduleOne.NPCs.Schedules;
using S1Map = ScheduleOne.Map;
using S1Vehicles = ScheduleOne.Vehicles;
using S1VehiclesAI = ScheduleOne.Vehicles.AI;
using S1ObjectScripts = ScheduleOne.ObjectScripts;
using S1Money = ScheduleOne.Money;
#endif
using System.Reflection;
using UnityEngine;

namespace S1API.Entities.Schedule
{
    /// <summary>
    /// Specifies an action that makes an NPC use an ATM at a scheduled time.
    /// </summary>
    /// <remarks>
    /// This action creates a <see cref="S1NPCsSchedules.NPCSignal_UseATM"/> that will
    /// make the NPC walk to and interact with an ATM. If <see cref="ATMGUID"/> is not set
    /// the action expects ATM to be assigned at runtime by gameplay systems.
    /// </remarks>
    public sealed class UseATMSpec : IScheduleActionSpec
    {
        /// <summary>
        /// The time when this action should start, in minutes from midnight.
        /// </summary>
        public int StartTime { get; set; }

        /// <summary>
        /// Optional specific ATM GUID to target.
        /// </summary>
        public string ATMGUID { get; set; }

        /// <summary>
        /// Optional custom name for the action.
        /// </summary>
        public string Name { get; set; }

        void IScheduleActionSpec.ApplyTo(NPCSchedule schedule)
        {
            var action = schedule.AddActionInternal<S1NPCsSchedules.NPCSignal_UseATM>(StartTime, string.IsNullOrEmpty(Name) ? "UseATM" : Name);
            if (action == null)
                return;

            if (!string.IsNullOrEmpty(ATMGUID))
            {
                try
                {
#if MONOMELON
                    var guid = new System.Guid(ATMGUID);
#else
                    var guid = new Il2CppSystem.Guid(ATMGUID);
#endif
                    var atm = GUIDManager.GetObject<S1Money.ATM>(guid);
                    TrySetFieldOrProperty(action, "ATM", atm);
                }
                catch { }
            }
        }

        private static bool TrySetFieldOrProperty(object target, string memberName, object value)
        {
            if (target == null) return false;
            var type = target.GetType();
            const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            
            // Try field first
            var fi = type.GetField(memberName, flags);
            if (fi != null)
            {
                try
                {
                    if (value == null || fi.FieldType.IsInstanceOfType(value))
                    {
                        fi.SetValue(target, value);
                        return true;
                    }
                }
                catch { }
            }
            
            // Try property
            var pi = type.GetProperty(memberName, flags);
            if (pi != null && pi.CanWrite)
            {
                try
                {
                    if (value == null || pi.PropertyType.IsInstanceOfType(value))
                    {
                        pi.SetValue(target, value);
                        return true;
                    }
                }
                catch { }
            }
            
            return false;
        }
    }
}



#if (IL2CPPMELON)
using Il2CppScheduleOne.NPCs;
#else
using ScheduleOne.NPCs;
#endif
using System.Linq;

namespace S1API.Entities.NPCs.PoliceOfficers
{
    /// <summary>
    /// Officer Howard is a police officer.
    /// He is the officer with a light brown afro and goatee!
    /// </summary>
    public class OfficerHoward : NPC
    {
        /// <summary>
        /// Static NPC ID for Officer Howard. Used to resolve connections during prefab configuration.
        /// </summary>
        public new static string NPCId => "officerhoward";
        
        internal OfficerHoward() : base(NPCManager.NPCRegistry.ToArray().First(n => n.ID == "officerhoward")) { }
    }
}

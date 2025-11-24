#if (IL2CPPMELON)
using Il2CppScheduleOne.NPCs;
#else
using ScheduleOne.NPCs;
#endif
using System.Linq;

namespace S1API.Entities.NPCs.PoliceOfficers
{
    /// <summary>
    /// Officer Lopez is a police officer.
    /// She is the officer with a blue button-up and long black hair!
    /// </summary>
    public class OfficerLopez : NPC
    {
        /// <summary>
        /// Static NPC ID for Officer Lopez. Used to resolve connections during prefab configuration.
        /// </summary>
        public new static string NPCId => "officerlopez";
        
        internal OfficerLopez() : base(NPCManager.NPCRegistry.ToArray().First(n => n.ID == "officerlopez")) { }
    }
}

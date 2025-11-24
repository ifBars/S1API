#if (IL2CPPMELON)
using Il2CppScheduleOne.NPCs;
#else
using ScheduleOne.NPCs;
#endif
using System.Linq;

namespace S1API.Entities.NPCs.PoliceOfficers
{
    /// <summary>
    /// Officer Jackson is a police officer.
    /// He is the officer with a light brown goatee and police hat!
    /// </summary>
    public class OfficerJackson : NPC
    {
        /// <summary>
        /// Static NPC ID for Officer Jackson. Used to resolve connections during prefab configuration.
        /// </summary>
        public new static string NPCId => "officerjackson";
        
        internal OfficerJackson() : base(NPCManager.NPCRegistry.ToArray().First(n => n.ID == "officerjackson")) { }
    }
}

#if (IL2CPPMELON)
using Il2CppScheduleOne.NPCs;
#else
using ScheduleOne.NPCs;
#endif
using System.Linq;

namespace S1API.Entities.NPCs.PoliceOfficers
{
    /// <summary>
    /// Officer Oakley is a police officer.
    /// He is the officer with light brown spiky hair and a goatee!
    /// </summary>
    public class OfficerOakley : NPC
    {
        /// <summary>
        /// Static NPC ID for Officer Oakley. Used to resolve connections during prefab configuration.
        /// </summary>
        public new static string NPCId => "officeroakley";
        
        internal OfficerOakley() : base(NPCManager.NPCRegistry.ToArray().First(n => n.ID == "officeroakley")) { }
    }
}

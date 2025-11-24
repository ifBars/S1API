#if (IL2CPPMELON)
using Il2CppScheduleOne.NPCs;
#else
using ScheduleOne.NPCs;
#endif
using System.Linq;

namespace S1API.Entities.NPCs.PoliceOfficers
{
    /// <summary>
    /// Officer Murphy is a police officer.
    /// He is the balding officer with grey hair and wrinkles!
    /// </summary>
    public class OfficerMurphy : NPC
    {
        /// <summary>
        /// Static NPC ID for Officer Murphy. Used to resolve connections during prefab configuration.
        /// </summary>
        public new static string NPCId => "officermurphy";
        
        internal OfficerMurphy() : base(NPCManager.NPCRegistry.ToArray().First(n => n.ID == "officermurphy")) { }
    }
}

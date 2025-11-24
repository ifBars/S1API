#if (IL2CPPMELON)
using Il2CppScheduleOne.NPCs;
#else
using ScheduleOne.NPCs;
#endif
using System.Linq;

namespace S1API.Entities.NPCs.PoliceOfficers
{
    /// <summary>
    /// Officer Green is a police officer.
    /// She is the officer with light brown hair in a bun!
    /// </summary>
    public class OfficerGreen : NPC
    {
        /// <summary>
        /// Static NPC ID for Officer Green. Used to resolve connections during prefab configuration.
        /// </summary>
        public new static string NPCId => "officergreen";
        
        internal OfficerGreen() : base(NPCManager.NPCRegistry.ToArray().First(n => n.ID == "officergreen")) { }
    }
}

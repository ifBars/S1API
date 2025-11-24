#if (IL2CPPMELON)
using Il2CppScheduleOne.NPCs;
#else
using ScheduleOne.NPCs;
#endif
using System.Linq;

namespace S1API.Entities.NPCs.Docks
{
    /// <summary>
    /// Cranky Frank is a customer.
    /// He lives in the Docks region.
    /// Frank is the NPC with a pot on his head!
    /// </summary>
    public class CrankyFrank : NPC
    {
        /// <summary>
        /// Static NPC ID for Cranky Frank. Used to resolve connections during prefab configuration.
        /// </summary>
        public new static string NPCId => "cranky_frank";
        
        internal CrankyFrank() : base(NPCManager.NPCRegistry.ToArray().First(n => n.ID == "cranky_frank")) { }
    }
}

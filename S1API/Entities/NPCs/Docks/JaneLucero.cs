#if (IL2CPPMELON)
using Il2CppScheduleOne.NPCs;
#else
using ScheduleOne.NPCs;
#endif
using System.Linq;

namespace S1API.Entities.NPCs.Docks
{
    /// <summary>
    /// Jane Lucero is a dealer.
    /// She lives in the Docks region.
    /// Jane is the dealer with a tear tattoo!
    /// </summary>
    public class JaneLucero : NPC
    {
        /// <summary>
        /// Static NPC ID for Jane Lucero. Used to resolve connections during prefab configuration.
        /// </summary>
        public new static string NPCId => "jane_lucero";
        
        internal JaneLucero() : base(NPCManager.NPCRegistry.ToArray().First(n => n.ID == "jane_lucero")) { }
    }
}

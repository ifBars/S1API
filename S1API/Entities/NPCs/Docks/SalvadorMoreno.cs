#if (IL2CPPMELON)
using Il2CppScheduleOne.NPCs;
#else
using ScheduleOne.NPCs;
#endif
using System.Linq;

namespace S1API.Entities.NPCs.Docks
{
    /// <summary>
    /// Salvador Moreno is a supplier.
    /// He lives in the Docks region.
    /// Salvador is the NPC that supplies coca seeds to the player!
    /// </summary>
    public class SalvadorMoreno : NPC
    {
        /// <summary>
        /// Static NPC ID for Salvador Moreno. Used to resolve connections during prefab configuration.
        /// </summary>
        public new static string NPCId => "salvador_moreno";
        
        internal SalvadorMoreno() : base(NPCManager.NPCRegistry.ToArray().First(n => n.ID == "salvador_moreno")) { }
    }
}

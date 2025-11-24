#if (IL2CPPMELON)
using Il2CppScheduleOne.NPCs;
#else
using ScheduleOne.NPCs;
#endif
using System.Linq;
using NPC = S1API.Entities.NPC;

namespace S1API.Entities.NPCs.Uptown
{
    /// <summary>
    /// Pearl Moore is a customer.
    /// She lives in the Uptown region.
    /// Pearl is the NPC with long white hair with bangs!
    /// </summary>
    public class PearlMoore : NPC
    {
        /// <summary>
        /// Static NPC ID for Pearl Moore. Used to resolve connections during prefab configuration.
        /// </summary>
        public new static string NPCId => "pearl_moore";
        
        internal PearlMoore() : base(NPCManager.NPCRegistry.ToArray().First(n => n.ID == "pearl_moore")) { }
    }
}

#if (IL2CPPMELON)
using Il2CppScheduleOne.NPCs;
#else
using ScheduleOne.NPCs;
#endif
using System.Linq;

namespace S1API.Entities.NPCs.Suburbia
{
    /// <summary>
    /// Jeremy Wilkinson is a customer.
    /// He lives in the Suburbia region.
    /// Jeremy is the NPC that works at Hyland Auto!
    /// </summary>
    public class JeremyWilkinson : NPC
    {
        /// <summary>
        /// Static NPC ID for Jeremy Wilkinson. Used to resolve connections during prefab configuration.
        /// </summary>
        public new static string NPCId => "jeremy_wilkinson";
        
        internal JeremyWilkinson() : base(NPCManager.NPCRegistry.ToArray().First(n => n.ID == "jeremy_wilkinson")) { }
    }
}

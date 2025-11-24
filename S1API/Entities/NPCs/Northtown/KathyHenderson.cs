#if (IL2CPPMELON)
using Il2CppScheduleOne.NPCs;
#else
using ScheduleOne.NPCs;
#endif
using System.Linq;

namespace S1API.Entities.NPCs.Northtown
{
    /// <summary>
    /// Kathy Henderson is a customer.
    /// She lives in the Northtown region.
    /// Kathy is the NPC with long blonde hair with bangs!
    /// </summary>
    public class KathyHenderson : NPC
    {
        /// <summary>
        /// Static NPC ID for Kathy Henderson. Used to resolve connections during prefab configuration.
        /// </summary>
        public new static string NPCId => "kathy_henderson";
        
        internal KathyHenderson() : base(NPCManager.NPCRegistry.ToArray().First(n => n.ID == "kathy_henderson")) { }
    }
}

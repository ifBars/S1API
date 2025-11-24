#if (IL2CPPMELON)
using Il2CppScheduleOne.NPCs;
#else
using ScheduleOne.NPCs;
#endif
using System.Linq;

namespace S1API.Entities.NPCs.Downtown
{
    /// <summary>
    /// Kevin Oakley is a customer.
    /// He lives in the Downtown region.
    /// Kevin is the NPC wearing a green apron!
    /// </summary>
    public class KevinOakley : NPC
    {
        /// <summary>
        /// Static NPC ID for Kevin Oakley. Used to resolve connections during prefab configuration.
        /// </summary>
        public new static string NPCId => "kevin_oakley";
        
        internal KevinOakley() : base(NPCManager.NPCRegistry.ToArray().First(n => n.ID == "kevin_oakley")) { }
    }
}

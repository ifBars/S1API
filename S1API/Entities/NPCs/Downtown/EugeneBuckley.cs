#if (IL2CPPMELON)
using Il2CppScheduleOne.NPCs;
#else
using ScheduleOne.NPCs;
#endif
using System.Linq;

namespace S1API.Entities.NPCs.Downtown
{
    /// <summary>
    /// Eugene Buckley is a customer.
    /// He lives in the Downtown region.
    /// Eugene is the NPC with light brown hair, freckles, and black glasses!
    /// </summary>
    public class EugeneBuckley : NPC
    {
        /// <summary>
        /// Static NPC ID for Eugene Buckley. Used to resolve connections during prefab configuration.
        /// </summary>
        public new static string NPCId => "eugene_buckley";
        
        internal EugeneBuckley() : base(NPCManager.NPCRegistry.ToArray().First(n => n.ID == "eugene_buckley")) { }
    }
}

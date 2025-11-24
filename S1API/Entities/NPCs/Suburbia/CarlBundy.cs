#if (IL2CPPMELON)
using Il2CppScheduleOne.NPCs;
#else
using ScheduleOne.NPCs;
#endif
using System.Linq;

namespace S1API.Entities.NPCs.Suburbia
{
    /// <summary>
    /// Carl Bundy is a customer.
    /// He lives in the Suburbia region.
    /// Carl is the NPC with a brown apron!
    /// </summary>
    public class CarlBundy : NPC
    {
        /// <summary>
        /// Static NPC ID for Carl Bundy. Used to resolve connections during prefab configuration.
        /// </summary>
        public new static string NPCId => "carl_bundy";
        
        internal CarlBundy() : base(NPCManager.NPCRegistry.ToArray().First(n => n.ID == "carl_bundy")) { }
    }
}

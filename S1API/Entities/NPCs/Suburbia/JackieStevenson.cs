#if (IL2CPPMELON)
using Il2CppScheduleOne.NPCs;
#else
using ScheduleOne.NPCs;
#endif
using System.Linq;

namespace S1API.Entities.NPCs.Suburbia
{
    /// <summary>
    /// Jackie Stevenson is a customer.
    /// He lives in the Suburbia region.
    /// Jackie is the NPC with short brown hair and light freckles!
    /// </summary>
    public class JackieStevenson : NPC
    {
        /// <summary>
        /// Static NPC ID for Jackie Stevenson. Used to resolve connections during prefab configuration.
        /// </summary>
        public new static string NPCId => "jackie_stevenson";
        
        internal JackieStevenson() : base(NPCManager.NPCRegistry.ToArray().First(n => n.ID == "jackie_stevenson")) { }
    }
}

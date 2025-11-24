#if (IL2CPPMELON)
using Il2CppScheduleOne.NPCs;
#else
using ScheduleOne.NPCs;
#endif
using System.Linq;

namespace S1API.Entities.NPCs.Suburbia
{
    /// <summary>
    /// Harold Colt is a customer.
    /// He lives in the Suburbia region.
    /// Harold is the NPC with grey spiky hair and wrinkles!
    /// </summary>
    public class HaroldColt : NPC
    {
        /// <summary>
        /// Static NPC ID for Harold Colt. Used to resolve connections during prefab configuration.
        /// </summary>
        public new static string NPCId => "harold_colt";
        
        internal HaroldColt() : base(NPCManager.NPCRegistry.ToArray().First(n => n.ID == "harold_colt")) { }
    }
}

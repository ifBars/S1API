#if (IL2CPPMELON)
using Il2CppScheduleOne.NPCs;
#else
using ScheduleOne.NPCs;
#endif
using System.Linq;

namespace S1API.Entities.NPCs.Suburbia
{
    /// <summary>
    /// Karen Kennedy is a customer.
    /// She lives in the Suburbia region.
    /// Karen is the NPC with wavy blonde hair and purple eyelids!
    /// She can be found at the casino upstairs when it's open.
    /// </summary>
    public class KarenKennedy : NPC
    {
        /// <summary>
        /// Static NPC ID for Karen Kennedy. Used to resolve connections during prefab configuration.
        /// </summary>
        public new static string NPCId => "karen_kennedy";
        
        internal KarenKennedy() : base(NPCManager.NPCRegistry.ToArray().First(n => n.ID == "karen_kennedy")) { }
    }
}

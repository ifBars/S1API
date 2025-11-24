#if (IL2CPPMELON)
using Il2CppScheduleOne.NPCs;
#else
using ScheduleOne.NPCs;
#endif
using System.Linq;

namespace S1API.Entities.NPCs.Westville
{
    /// <summary>
    /// Kim Delaney is a customer.
    /// She lives in the Westville region.
    /// Kim is the NPC with long, black hair with bangs!
    /// </summary>
    public class KimDelaney : NPC
    {
        /// <summary>
        /// Static NPC ID for Kim Delaney. Used to resolve connections during prefab configuration.
        /// </summary>
        public new static string NPCId => "kim_delaney";
        
        internal KimDelaney() : base(NPCManager.NPCRegistry.ToArray().First(n => n.ID == "kim_delaney")) { }
    }
}

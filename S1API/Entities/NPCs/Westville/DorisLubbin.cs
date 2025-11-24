#if (IL2CPPMELON)
using Il2CppScheduleOne.NPCs;
#else
using ScheduleOne.NPCs;
#endif
using System.Linq;

namespace S1API.Entities.NPCs.Westville
{
    /// <summary>
    /// Doris Lubbin is a customer.
    /// She lives in the Westville region.
    /// Doris is the NPC with light brown, wavy hair and black glasses!
    /// </summary>
    public class DorisLubbin : NPC
    {
        /// <summary>
        /// Static NPC ID for Doris Lubbin. Used to resolve connections during prefab configuration.
        /// </summary>
        public new static string NPCId => "doris_lubbin";
        
        internal DorisLubbin() : base(NPCManager.NPCRegistry.ToArray().First(n => n.ID == "doris_lubbin")) { }
    }
}

#if (IL2CPPMELON)
using Il2CppScheduleOne.NPCs;
#else
using ScheduleOne.NPCs;
#endif
using System.Linq;

namespace S1API.Entities.NPCs.Westville
{
    /// <summary>
    /// Dean Webster is a customer.
    /// He lives in the Westville region.
    /// Dean is the NPC that owns Top Tattoo!
    /// </summary>
    public class DeanWebster : NPC
    {
        /// <summary>
        /// Static NPC ID for Dean Webster. Used to resolve connections during prefab configuration.
        /// </summary>
        public new static string NPCId => "dean_webster";
        
        internal DeanWebster() : base(NPCManager.NPCRegistry.ToArray().First(n => n.ID == "dean_webster")) { }
    }
}

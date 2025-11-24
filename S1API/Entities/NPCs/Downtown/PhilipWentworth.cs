#if (IL2CPPMELON)
using Il2CppScheduleOne.NPCs;
#else
using ScheduleOne.NPCs;
#endif
using System.Linq;

namespace S1API.Entities.NPCs.Downtown
{
    /// <summary>
    /// Philip Wentworth is a customer.
    /// He lives in the Downtown region.
    /// Philip is the bald NPC with a goatee!
    /// </summary>
    public class PhilipWentworth : NPC
    {
        /// <summary>
        /// Static NPC ID for Philip Wentworth. Used to resolve connections during prefab configuration.
        /// </summary>
        public new static string NPCId => "philip_wentworth";
        
        internal PhilipWentworth() : base(NPCManager.NPCRegistry.ToArray().First(n => n.ID == "philip_wentworth")) { }
    }
}

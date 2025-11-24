#if (IL2CPPMELON)
using Il2CppScheduleOne.NPCs;
#else
using ScheduleOne.NPCs;
#endif
using System.Linq;

namespace S1API.Entities.NPCs.Northtown
{
    /// <summary>
    /// Benji Coleman is a dealer.
    /// He lives in the Northtown region.
    /// Benji lives at the motel in room #2.
    /// He is the first dealer the player unlocks!
    /// </summary>
    public class BenjiColeman : NPC
    {
        /// <summary>
        /// Static NPC ID for Benji Coleman. Used to resolve connections during prefab configuration.
        /// </summary>
        public new static string NPCId => "benji_coleman";
        
        internal BenjiColeman() : base(NPCManager.NPCRegistry.ToArray().First(n => n.ID == "benji_coleman")) { }
    }
}

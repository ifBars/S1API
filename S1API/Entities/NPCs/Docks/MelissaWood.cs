#if (IL2CPPMELON)
using Il2CppScheduleOne.NPCs;
#else
using ScheduleOne.NPCs;
#endif
using System.Linq;

namespace S1API.Entities.NPCs.Docks
{
    /// <summary>
    /// Melissa Wood is a customer.
    /// She lives in the Docks region.
    /// Melissa is the Blackjack dealer at the casino!
    /// </summary>
    public class MelissaWood : NPC
    {
        /// <summary>
        /// Static NPC ID for Melissa Wood. Used to resolve connections during prefab configuration.
        /// </summary>
        public new static string NPCId => "melissa_wood";
        
        internal MelissaWood() : base(NPCManager.NPCRegistry.ToArray().First(n => n.ID == "melissa_wood")) { }
    }
}

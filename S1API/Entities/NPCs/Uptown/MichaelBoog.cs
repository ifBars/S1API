#if (IL2CPPMELON)
using Il2CppScheduleOne.NPCs;
#else
using ScheduleOne.NPCs;
#endif
using System.Linq;
using NPC = S1API.Entities.NPC;

namespace S1API.Entities.NPCs.Uptown
{
    /// <summary>
    /// Michael Boog is a customer.
    /// He lives in the Uptown region.
    /// Michael is the NPC with a bright blue flat cap and black glasses!
    /// </summary>
    public class MichaelBoog : NPC
    {
        /// <summary>
        /// Static NPC ID for Michael Boog. Used to resolve connections during prefab configuration.
        /// </summary>
        public new static string NPCId => "michael_boog";
        
        internal MichaelBoog() : base(NPCManager.NPCRegistry.ToArray().First(n => n.ID == "michael_boog")) { }
    }
}

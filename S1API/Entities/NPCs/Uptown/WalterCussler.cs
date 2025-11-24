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
    /// Walter Cussler is a customer.
    /// He lives in the Uptown region.
    /// Walter is the NPC with white hair and dressed as a priest!
    /// </summary>
    public class WalterCussler : NPC
    {
        /// <summary>
        /// Static NPC ID for Walter Cussler. Used to resolve connections during prefab configuration.
        /// </summary>
        public new static string NPCId => "walter_cussler";
        
        internal WalterCussler() : base(NPCManager.NPCRegistry.ToArray().First(n => n.ID == "walter_cussler")) { }
    }
}

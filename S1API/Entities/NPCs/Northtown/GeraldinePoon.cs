#if (IL2CPPMELON)
using Il2CppScheduleOne.NPCs;
#else
using ScheduleOne.NPCs;
#endif
using System.Linq;

namespace S1API.Entities.NPCs.Northtown
{
    /// <summary>
    /// Geraldine Poon is a customer.
    /// He lives in the Northtown region.
    /// Geraldine is the balding NPC with small gold glasses!
    /// </summary>
    public class GeraldinePoon : NPC
    {
        /// <summary>
        /// Static NPC ID for Geraldine Poon. Used to resolve connections during prefab configuration.
        /// </summary>
        public new static string NPCId => "geraldine_poon";
        
        internal GeraldinePoon() : base(NPCManager.NPCRegistry.ToArray().First(n => n.ID == "geraldine_poon")) { }
    }
}

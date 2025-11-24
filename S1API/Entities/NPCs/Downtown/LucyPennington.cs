#if (IL2CPPMELON)
using Il2CppScheduleOne.NPCs;
#else
using ScheduleOne.NPCs;
#endif
using System.Linq;

namespace S1API.Entities.NPCs.Downtown
{
    /// <summary>
    /// Lucy Pennington is a customer.
    /// She lives in the Downtown region.
    /// Lucy is the NPC with blonde haired buns up high!
    /// </summary>
    public class LucyPennington : NPC
    {
        /// <summary>
        /// Static NPC ID for Lucy Pennington. Used to resolve connections during prefab configuration.
        /// </summary>
        public new static string NPCId => "lucy_pennington";
        
        internal LucyPennington() : base(NPCManager.NPCRegistry.ToArray().First(n => n.ID == "lucy_pennington")) { }
    }
}

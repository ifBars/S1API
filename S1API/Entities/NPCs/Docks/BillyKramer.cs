#if (IL2CPPMELON)
using Il2CppScheduleOne.NPCs;
#else
using ScheduleOne.NPCs;
#endif
using System.Linq;

namespace S1API.Entities.NPCs.Docks
{
    /// <summary>
    /// Billy Kramer is a customer.
    /// He lives in the Docks region.
    /// </summary>
    public class BillyKramer : NPC
    {
        /// <summary>
        /// Static NPC ID for Billy Kramer. Used to resolve connections during prefab configuration.
        /// </summary>
        public new static string NPCId => "billy_kramer";
        
        internal BillyKramer() : base(NPCManager.NPCRegistry.ToArray().First(n => n.ID == "billy_kramer")) { }
    }
}

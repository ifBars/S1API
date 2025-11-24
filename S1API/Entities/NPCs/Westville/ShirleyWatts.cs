#if (IL2CPPMELON)
using Il2CppScheduleOne.NPCs;
#else
using ScheduleOne.NPCs;
#endif
using System.Linq;

namespace S1API.Entities.NPCs.Westville
{
    /// <summary>
    /// Shirley Watts is a supplier.
    /// She lives in the Westville region.
    /// Shirley is the supplier for pseudo!
    /// </summary>
    public class ShirleyWatts : NPC
    {
        /// <summary>
        /// Static NPC ID for Shirley Watts. Used to resolve connections during prefab configuration.
        /// </summary>
        public new static string NPCId => "shirley_watts";
        
        internal ShirleyWatts() : base(NPCManager.NPCRegistry.ToArray().First(n => n.ID == "shirley_watts")) { }
    }
}

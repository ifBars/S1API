#if (IL2CPPMELON)
using Il2CppScheduleOne.NPCs;
#else
using ScheduleOne.NPCs;
#endif
using System.Linq;

namespace S1API.Entities.NPCs
{
    /// <summary>
    /// Uncle Nelson is a NPC.
    /// He is the uncle of the main character!
    /// </summary>
    public class UncleNelson : NPC
    {
        /// <summary>
        /// Static NPC ID for Uncle Nelson. Used to resolve connections during prefab configuration.
        /// </summary>
        public new static string NPCId => "uncle_nelson";
        
        internal UncleNelson() : base(NPCManager.NPCRegistry.ToArray().First(n => n.ID == "uncle_nelson")) { }
    }
}

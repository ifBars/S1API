#if (IL2CPPMELON)
using Il2CppScheduleOne.NPCs;
#else
using ScheduleOne.NPCs;
#endif
using System.Linq;

namespace S1API.Entities.NPCs
{
    /// <summary>
    /// Igor Romanovich is a npc.
    /// He is Manny's bodyguard.
    /// Igor can be found inside the Warehouse!
    /// </summary>
    public class IgorRomanovich : NPC
    {
        /// <summary>
        /// Static NPC ID for Igor Romanovich. Used to resolve connections during prefab configuration.
        /// </summary>
        public new static string NPCId => "igor_romanovich";
        
        internal IgorRomanovich() : base(NPCManager.NPCRegistry.ToArray().First(n => n.ID == "igor_romanovich")) { }
    }
}

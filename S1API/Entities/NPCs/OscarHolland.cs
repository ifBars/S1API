#if (IL2CPPMELON)
using Il2CppScheduleOne.NPCs;
#else
using ScheduleOne.NPCs;
#endif
using System.Linq;

namespace S1API.Entities.NPCs
{
    /// <summary>
    /// Oscar Holland is a NPC.
    /// He is a supplier located in the Warehouse!
    /// </summary>
    public class OscarHolland : NPC
    {
        /// <summary>
        /// Static NPC ID for Oscar Holland. Used to resolve connections during prefab configuration.
        /// </summary>
        public new static string NPCId => "oscar_holland";
        
        internal OscarHolland() : base(NPCManager.NPCRegistry.ToArray().First(n => n.ID == "oscar_holland")) { }
    }
}

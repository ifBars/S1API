#if (IL2CPPMELON)
using Il2CppScheduleOne.NPCs;
#else
using ScheduleOne.NPCs;
#endif
using System.Linq;

namespace S1API.Entities.NPCs
{
    /// <summary>
    /// Manny is a NPC.
    /// He provides workers to the player.
    /// Manny can be found in the Warehouse!
    /// </summary>
    public class MannyOakfield : NPC
    {
        /// <summary>
        /// Static NPC ID for Manny Oakfield. Used to resolve connections during prefab configuration.
        /// </summary>
        public new static string NPCId => "manny_oakfield";
        
        internal MannyOakfield() : base(NPCManager.NPCRegistry.ToArray().First(n => n.ID == "manny_oakfield")) { }
    }
}

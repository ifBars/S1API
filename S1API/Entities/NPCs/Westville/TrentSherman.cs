#if (IL2CPPMELON)
using Il2CppScheduleOne.NPCs;
#else
using ScheduleOne.NPCs;
#endif
using System.Linq;

namespace S1API.Entities.NPCs.Westville
{
    /// <summary>
    /// Trent Sherman is a customer.
    /// He lives in the Westville region.
    /// Trent is the NPC with short black hair and dark-colored skin!
    /// </summary>
    public class TrentSherman : NPC
    {
        /// <summary>
        /// Static NPC ID for Trent Sherman. Used to resolve connections during prefab configuration.
        /// </summary>
        public new static string NPCId => "trent_sherman";
        
        internal TrentSherman() : base(NPCManager.NPCRegistry.ToArray().First(n => n.ID == "trent_sherman")) { }
    }
}

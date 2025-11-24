#if (IL2CPPMELON)
using Il2CppScheduleOne.NPCs;
#else
using ScheduleOne.NPCs;
#endif
using System.Linq;

namespace S1API.Entities.NPCs.Docks
{
    /// <summary>
    /// Genghis Barn is a customer.
    /// He lives in the Docks region.
    /// Genghis is the NPC with a mohawk!
    /// </summary>
    public class GenghisBarn : NPC
    {
        /// <summary>
        /// Static NPC ID for Genghis Barn. Used to resolve connections during prefab configuration.
        /// </summary>
        public new static string NPCId => "genghis_barn";
        
        internal GenghisBarn() : base(NPCManager.NPCRegistry.ToArray().First(n => n.ID == "genghis_barn")) { }
    }
}

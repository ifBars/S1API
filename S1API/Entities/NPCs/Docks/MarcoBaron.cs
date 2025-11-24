#if (IL2CPPMELON)
using Il2CppScheduleOne.NPCs;
#else
using ScheduleOne.NPCs;
#endif
using System.Linq;

namespace S1API.Entities.NPCs.Docks
{
    /// <summary>
    /// Marco Baron is a customer.
    /// He lives in the Docks region.
    /// Marco is the NPC that runs the Auto Shop!
    /// </summary>
    public class MarcoBaron : NPC
    {
        /// <summary>
        /// Static NPC ID for Marco Baron. Used to resolve connections during prefab configuration.
        /// </summary>
        public new static string NPCId => "marco_baron";
        
        internal MarcoBaron() : base(NPCManager.NPCRegistry.ToArray().First(n => n.ID == "marco_baron")) { }
    }
}

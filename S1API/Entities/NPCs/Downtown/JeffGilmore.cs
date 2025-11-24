#if (IL2CPPMELON)
using Il2CppScheduleOne.NPCs;
#else
using ScheduleOne.NPCs;
#endif
using System.Linq;

namespace S1API.Entities.NPCs.Downtown
{
    /// <summary>
    /// Jeff Gilmore is a customer.
    /// He lives in the Downtown region.
    /// Jeff is the NPC that runs the skateboard shop!
    /// </summary>
    public class JeffGilmore : NPC
    {
        /// <summary>
        /// Static NPC ID for Jeff Gilmore. Used to resolve connections during prefab configuration.
        /// </summary>
        public new static string NPCId => "jeff_gilmore";
        
        internal JeffGilmore() : base(NPCManager.NPCRegistry.ToArray().First(n => n.ID == "jeff_gilmore")) { }
    }
}

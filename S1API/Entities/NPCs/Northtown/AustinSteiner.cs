#if (IL2CPPMELON)
using Il2CppScheduleOne.NPCs;
#else
using ScheduleOne.NPCs;
#endif
using System.Linq;

namespace S1API.Entities.NPCs.Northtown
{
    /// <summary>
    /// Austin Steiner is a customer.
    /// He lives in the Northtown region.
    /// Austin is the NPC with a red/orange afro and black glasses!
    /// </summary>
    public class AustinSteiner : NPC
    {
        /// <summary>
        /// Static NPC ID for Austin Steiner. Used to resolve connections during prefab configuration.
        /// </summary>
        public new static string NPCId => "austin_steiner";
        
        internal AustinSteiner() : base(NPCManager.NPCRegistry.ToArray().First(n => n.ID == "austin_steiner")) { }
    }
}

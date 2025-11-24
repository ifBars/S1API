#if (IL2CPPMELON)
using Il2CppScheduleOne.NPCs;
#else
using ScheduleOne.NPCs;
#endif
using System.Linq;

namespace S1API.Entities.NPCs.Downtown
{
    /// <summary>
    /// Greg Fliggle is a customer.
    /// He lives in the Downtown region.
    /// Greg is the NPC with a teardrop tattoo and wrinkles!
    /// </summary>
    public class GregFliggle : NPC
    {
        /// <summary>
        /// Static NPC ID for Greg Fliggle. Used to resolve connections during prefab configuration.
        /// </summary>
        public new static string NPCId => "greg_fliggle";
        
        internal GregFliggle() : base(NPCManager.NPCRegistry.ToArray().First(n => n.ID == "greg_fliggle")) { }
    }
}

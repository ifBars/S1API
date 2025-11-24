#if (IL2CPPMELON)
using Il2CppScheduleOne.NPCs;
#else
using ScheduleOne.NPCs;
#endif
using System.Linq;

namespace S1API.Entities.NPCs
{
    /// <summary>
    /// UNCONFIRMED: Dan Samwell is a customer.
    /// He is the NPC that owns Dan's Hardware!
    /// If you confirm this, please let us know so we can update the documentation!
    /// </summary>
    public class DanSamwell : NPC
    {
        /// <summary>
        /// Static NPC ID for Dan Samwell. Used to resolve connections during prefab configuration.
        /// </summary>
        public new static string NPCId => "dan_samwell";
        
        internal DanSamwell() : base(NPCManager.NPCRegistry.ToArray().First(n => n.ID == "dan_samwell")) { }
    }
}

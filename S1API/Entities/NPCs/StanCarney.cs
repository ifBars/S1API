#if (IL2CPPMELON)
using Il2CppScheduleOne.NPCs;
#else
using ScheduleOne.NPCs;
#endif
using System.Linq;

namespace S1API.Entities.NPCs
{
    /// <summary>
    /// Stan Carney is a NPC.
    /// He is the NPC that sells weapons.
    /// Stan can be found in the Warehouse!
    /// </summary>
    public class StanCarney : NPC
    {
        /// <summary>
        /// Static NPC ID for Stan Carney. Used to resolve connections during prefab configuration.
        /// </summary>
        public new static string NPCId => "stan_carney";
        
        internal StanCarney() : base(NPCManager.NPCRegistry.ToArray().First(n => n.ID == "stan_carney")) { }
    }
}

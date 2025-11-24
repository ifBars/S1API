#if (IL2CPPMELON)
using Il2CppScheduleOne.NPCs;
#else
using ScheduleOne.NPCs;
#endif
using System.Linq;

namespace S1API.Entities.NPCs.Docks
{
    /// <summary>
    /// Anna Chesterfield is a customer.
    /// She lives in the Docks region.
    /// Anna also works at the Barbershop.
    /// </summary>
    public class AnnaChesterfield : NPC
    {
        /// <summary>
        /// Static NPC ID for Anna Chesterfield. Used to resolve connections during prefab configuration.
        /// </summary>
        public new static string NPCId => "anna_chesterfield";
        
        internal AnnaChesterfield() : base(NPCManager.NPCRegistry.ToArray().First(n => n.ID == "anna_chesterfield")) { }
    }
}

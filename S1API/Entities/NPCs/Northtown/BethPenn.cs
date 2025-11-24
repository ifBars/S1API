#if (IL2CPPMELON)
using Il2CppScheduleOne.NPCs;
#else
using ScheduleOne.NPCs;
#endif
using System.Linq;

namespace S1API.Entities.NPCs.Northtown
{
    /// <summary>
    /// Beth Penn is a customer.
    /// She lives in the Northtown region.
    /// Beth is the NPC with a blonde bowl cut and wears green glasses!
    /// </summary>
    public class BethPenn : NPC
    {
        /// <summary>
        /// Static NPC ID for Beth Penn. Used to resolve connections during prefab configuration.
        /// </summary>
        public new static string NPCId => "beth_penn";
        
        internal BethPenn() : base(NPCManager.NPCRegistry.ToArray().First(n => n.ID == "beth_penn")) { }
    }
}

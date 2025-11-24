#if (IL2CPPMELON)
using Il2CppScheduleOne.NPCs;
#else
using ScheduleOne.NPCs;
#endif
using System.Linq;

namespace S1API.Entities.NPCs.Westville
{
    /// <summary>
    /// George Greene is a customer.
    /// He lives in the Westville region.
    /// George is the NPC with light brown, spiky hair and gold glasses!
    /// </summary>
    public class GeorgeGreene : NPC
    {
        /// <summary>
        /// Static NPC ID for George Greene. Used to resolve connections during prefab configuration.
        /// </summary>
        public new static string NPCId => "george_greene";
        
        internal GeorgeGreene() : base(NPCManager.NPCRegistry.ToArray().First(n => n.ID == "george_greene")) { }
    }
}

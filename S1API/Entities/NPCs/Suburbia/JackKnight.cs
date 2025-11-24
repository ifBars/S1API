#if (IL2CPPMELON)
using Il2CppScheduleOne.NPCs;
#else
using ScheduleOne.NPCs;
#endif
using System.Linq;

namespace S1API.Entities.NPCs.Suburbia
{
    /// <summary>
    /// Jack Knight is a customer.
    /// He lives in the Suburbia region.
    /// Jack is the balding NPC with small gold glasses!
    /// </summary>
    public class JackKnight : NPC
    {
        /// <summary>
        /// Static NPC ID for Jack Knight. Used to resolve connections during prefab configuration.
        /// </summary>
        public new static string NPCId => "jack_knight";
        
        internal JackKnight() : base(NPCManager.NPCRegistry.ToArray().First(n => n.ID == "jack_knight")) { }
    }
}

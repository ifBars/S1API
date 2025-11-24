#if (IL2CPPMELON)
using Il2CppScheduleOne.NPCs;
#else
using ScheduleOne.NPCs;
#endif
using System.Linq;

namespace S1API.Entities.NPCs.Northtown
{
    /// <summary>
    /// Peter File is a customer.
    /// He lives in the Northtown region.
    /// Peter is the NPC with a black bowl cut and black glasses!
    /// </summary>
    public class PeterFile : NPC
    {
        /// <summary>
        /// Static NPC ID for Peter File. Used to resolve connections during prefab configuration.
        /// </summary>
        public new static string NPCId => "peter_file";
        
        internal PeterFile() : base(NPCManager.NPCRegistry.ToArray().First(n => n.ID == "peter_file")) { }
    }
}

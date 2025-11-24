#if (IL2CPPMELON)
using Il2CppScheduleOne.NPCs;
#else
using ScheduleOne.NPCs;
#endif
using System.Linq;

namespace S1API.Entities.NPCs.Northtown
{
    /// <summary>
    /// Albert Hoover is a supplier.
    /// He lives in the Northtown region.
    /// Albert is the supplier for weed seeds!
    /// </summary>
    public class AlbertHoover : NPC
    {
        /// <summary>
        /// Static NPC ID for Albert Hoover. Used to resolve connections during prefab configuration.
        /// </summary>
        public new static string NPCId => "albert_hoover";
        
        internal AlbertHoover() : base(NPCManager.NPCRegistry.ToArray().First(n => n.ID == "albert_hoover")) { }
    }
}

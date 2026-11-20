using UnityEngine;

namespace S1API.Map
{
    /// <summary>
    /// Describes a map region added by a mod. Pass it to <see cref="Regions.Register"/>.
    /// </summary>
    /// <remarks>
    /// S1API copies the definition when it is registered; changing the instance afterwards has no effect.
    /// </remarks>
    public sealed class RegionDefinition
    {
        /// <summary>
        /// Stable identifier of the region. S1API keys the region's save data by it, so it must not change between
        /// versions of the mod. Compared without regard to case.
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Display name of the region in the map, the contacts app and the unlock notice.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// The rank (the numeric value of the game's rank enum) that unlocks the region when the player reaches it.
        /// 0 unlocks the region from the start. A negative value never unlocks it by rank; use
        /// <see cref="RegionHandle.Unlock"/> instead.
        /// </summary>
        public int UnlockRank { get; set; }

        /// <summary>
        /// Cartel influence in the region on a new save, from 0 to 1. A save that already holds the region keeps its
        /// own value.
        /// </summary>
        public float StartInfluence { get; set; }

        /// <summary>
        /// Picture shown with the region in the "region unlocked" notice. Optional.
        /// </summary>
        public Sprite? ContactsMapImage { get; set; }

        /// <summary>
        /// Outline of the region on the ground, at least three points. Each point is a world position with x as world x and y as world z. The game
        /// asks this outline which region a position belongs to.
        /// </summary>
        public Vector2[] Polygon { get; set; } = new Vector2[0];

        /// <summary>
        /// The vanilla region whose regional cartel activities the region uses, since the game defines activities per
        /// vanilla region only. Defaults to <see cref="Region.Northtown"/>.
        /// </summary>
        public Region StandIn { get; set; } = Region.Northtown;
    }
}

using System;
using System.Collections.Generic;
using S1API.Internal.Map;

namespace S1API.Map
{
    /// <summary>
    /// Registry of custom map regions. A custom region works in the map, the cartel influence, the cartel
    /// activities and the contacts app, and stays out of the game's own save data, so a save loads without the mod.
    /// </summary>
    /// <example>
    /// <code>
    /// Regions.Register(new RegionDefinition
    /// {
    ///     Id = "mymod.harbor",
    ///     Name = "Harbor",
    ///     StartInfluence = 0.4f,
    ///     Polygon = new[] { new Vector2(0, 0), new Vector2(50, 0), new Vector2(50, 50), new Vector2(0, 50) },
    /// });
    /// </code>
    /// </example>
    public static class Regions
    {
        /// <summary>
        /// Registers a custom region. Call it before the world loads, for example in the mod's initialization.
        /// Registering an id again replaces its definition; a running world keeps the old one until it is reloaded.
        /// </summary>
        /// <param name="def">The region to add.</param>
        /// <returns>The handle of the region.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="def"/> is null.</exception>
        /// <exception cref="ArgumentException">The id or name is empty, the polygon has fewer than three finite points, or the start influence is outside 0 to 1.</exception>
        public static RegionHandle Register(RegionDefinition def) => CustomRegionRegistry.Register(def);

        /// <summary>
        /// All registered custom regions, in registration order.
        /// </summary>
        public static IReadOnlyList<RegionHandle> Custom => CustomRegionRegistry.Handles();

        /// <summary>
        /// Finds a registered custom region by its id.
        /// </summary>
        /// <param name="id">The id given in the definition.</param>
        /// <returns>The handle, or null if no region has that id.</returns>
        public static RegionHandle? Get(string id) => CustomRegionRegistry.Find(id)?.Handle;
    }
}

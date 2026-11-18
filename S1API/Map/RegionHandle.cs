using System;
using S1API.Internal.Map;

namespace S1API.Map
{
    /// <summary>
    /// A custom region registered through <see cref="Regions.Register"/>.
    /// </summary>
    public sealed class RegionHandle
    {
        internal CustomRegionEntry Entry { get; }

        internal RegionHandle(CustomRegionEntry entry)
        {
            Entry = entry;
        }

        /// <summary>
        /// The identifier given in the <see cref="RegionDefinition"/>.
        /// </summary>
        public string Id => Entry.Id;

        /// <summary>
        /// The display name of the region.
        /// </summary>
        public string Name => Entry.Definition.Name;

        /// <summary>
        /// True once the region has its number. Numbers are given when the first world loads after all mods registered
        /// their regions.
        /// </summary>
        public bool IsAssigned => CustomRegionRegistry.IsAssigned(Entry);

        /// <summary>
        /// The value of the region for the game's region parameters, above every vanilla region. Cast it to
        /// <see cref="Region"/> for the S1API members that take one. Only valid for the running world.
        /// </summary>
        /// <exception cref="InvalidOperationException">The region has no number yet (<see cref="IsAssigned"/> is false).</exception>
        public Region Value
        {
            get
            {
                if (!IsAssigned)
                    throw new InvalidOperationException($"Region '{Id}' has no number yet; read Value after the world loaded.");
                return (Region)Entry.Value;
            }
        }

        /// <summary>
        /// Whether the player has unlocked the region. False while no world is loaded.
        /// </summary>
        public bool IsUnlocked => CustomRegionWorld.IsUnlocked(Entry);

        /// <summary>
        /// The current cartel influence in the region from 0 to 1. Before a world is loaded, the start influence.
        /// </summary>
        public float Influence => CustomRegionWorld.GetInfluence(Entry);

        /// <summary>
        /// Unlocks the region. Does nothing and logs a warning while no world is loaded.
        /// </summary>
        public void Unlock() => CustomRegionWorld.Unlock(Entry);

        /// <summary>
        /// Changes the cartel influence in the region by the given amount. The change goes through the game's
        /// networked influence call and does nothing while no world is loaded.
        /// </summary>
        /// <param name="amount">Positive raises the influence, negative lowers it.</param>
        public void ChangeInfluence(float amount) => CustomRegionWorld.ChangeInfluence(Entry, amount);
    }
}

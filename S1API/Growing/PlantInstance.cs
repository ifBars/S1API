#if (IL2CPPMELON)
using S1Growing = Il2CppScheduleOne.Growing;
using S1Trash = Il2CppScheduleOne.Trash;
using S1DevUtilities = Il2CppScheduleOne.DevUtilities;
#elif (MONOMELON || MONOBEPINEX || IL2CPPBEPINEX)
using S1Growing = ScheduleOne.Growing;
using S1Trash = ScheduleOne.Trash;
using S1DevUtilities = ScheduleOne.DevUtilities;
#endif

using S1API.Internal.Utils;
using S1API.Items;
using UnityEngine;

namespace S1API.Growing
{
    /// <summary>
    /// Represents an instance of a growing plant in the world.
    /// </summary>
    public class PlantInstance
    {
        /// <summary>
        /// INTERNAL: The in-game Plant object.
        /// </summary>
        internal readonly S1Growing.Plant S1Plant;

        /// <summary>
        /// INTERNAL: Create a wrapper around an existing Plant.
        /// </summary>
        /// <param name="plant">The in-game Plant to wrap.</param>
        internal PlantInstance(S1Growing.Plant plant)
        {
            S1Plant = plant;
        }

        /// <summary>
        /// The current growth stage as a float from 0.0 to 1.0.
        /// </summary>
        public float NormalizedGrowth =>
            S1Plant.NormalizedGrowthProgress;

        /// <summary>
        /// Whether the plant is fully grown.
        /// </summary>
        public bool IsFullyGrown =>
            S1Plant.IsFullyGrown;

        /// <summary>
        /// The SeedDefinition that this plant originated from.
        /// </summary>
        public SeedDefinition SeedDefinition =>
            new SeedDefinition(S1Plant.SeedDefinition);

        /// <summary>
        /// The quality level of this plant.
        /// </summary>
        public float Quality =>
            S1Plant.QualityLevel;

        /// <summary>
        /// The yield multiplier of this plant.
        /// </summary>
        public float YieldMultiplier =>
            S1Plant.YieldMultiplier;

        /// <summary>
        /// The GameObject of the plant.
        /// </summary>
        private GameObject GameObject =>
            S1Plant.gameObject;

        /// <summary>
        /// Destroys this plant in-game.
        /// </summary>
        /// <param name="dropScraps">Whether to drop plant scraps (trash) at the plant's location.</param>
        public void Destroy(bool dropScraps = false)
        {
            if (dropScraps && S1Plant.PlantScrapPrefab != null)
            {
                try
                {
                    // Spawn the plant scrap at the plant's position
                    var trashManager = S1DevUtilities.NetworkSingleton<S1Trash.TrashManager>.Instance;
                    if (trashManager != null)
                    {
                        var position = S1Plant.transform.position;
                        var rotation = Quaternion.identity;
                        trashManager.CreateTrashItem(
                            S1Plant.PlantScrapPrefab.ID,
                            position,
                            rotation
                        );
                    }
                }
                catch
                {
                    // Silently fail if trash spawning fails - still destroy the plant
                }
            }

            Object.Destroy(S1Plant.gameObject);
        }
    }
}

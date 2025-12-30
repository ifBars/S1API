#if (IL2CPPMELON)
using S1Growing = Il2CppScheduleOne.Growing;
#elif (MONOMELON || MONOBEPINEX || IL2CPPBEPINEX)
using S1Growing = ScheduleOne.Growing;
#endif

using S1API.Internal.Utils;
using UnityEngine;

namespace S1API.Growing
{
    /// <summary>
    /// Represents an instance of a mushroom colony growing in the world.
    /// </summary>
    /// <remarks>
    /// Mushroom colonies grow in mushroom beds and require cool temperatures (≤15°F) to grow properly.
    /// Each colony can produce multiple individual mushrooms that can be harvested.
    /// </remarks>
    public sealed class ShroomColonyInstance
    {
        /// <summary>
        /// The maximum temperature (in Fahrenheit) at which mushrooms can grow.
        /// </summary>
        public const float MaxTemperatureForGrowth = 15f;

        /// <summary>
        /// INTERNAL: The in-game ShroomColony object.
        /// </summary>
        internal readonly S1Growing.ShroomColony S1ShroomColony;

        /// <summary>
        /// INTERNAL: Create a wrapper around an existing ShroomColony.
        /// </summary>
        /// <param name="colony">The in-game ShroomColony to wrap.</param>
        internal ShroomColonyInstance(S1Growing.ShroomColony colony)
        {
            S1ShroomColony = colony;
        }

        /// <summary>
        /// The base yield of mushrooms this colony will produce when fully grown.
        /// </summary>
        public int BaseShroomYield =>
            S1ShroomColony.BaseShroomYield;

        /// <summary>
        /// The current growth progress as a float from 0.0 to 1.0.
        /// </summary>
        public float GrowthProgress =>
            S1ShroomColony.GrowthProgress;

        /// <summary>
        /// Whether the colony is fully grown and ready for harvest.
        /// </summary>
        public bool IsFullyGrown =>
            S1ShroomColony.IsFullyGrown;

        /// <summary>
        /// Whether the colony's temperature is too hot for growth.
        /// Mushrooms require temperatures at or below 15°F to grow.
        /// </summary>
        public bool IsTooHotToGrow =>
            S1ShroomColony.IsTooHotToGrow;

        /// <summary>
        /// The number of individual mushrooms currently grown in this colony.
        /// </summary>
        public int GrownMushroomCount =>
            S1ShroomColony.GrownMushroomCount;

        /// <summary>
        /// The normalized quality level of this colony (0.0 to 1.0).
        /// </summary>
        public float NormalizedQuality =>
            S1ShroomColony.NormalizedQuality;

        /// <summary>
        /// The GameObject of the colony.
        /// </summary>
        public GameObject GameObject =>
            S1ShroomColony.gameObject;

        /// <summary>
        /// Destroys this mushroom colony in-game.
        /// </summary>
        /// <remarks>
        /// This will remove the colony and all its mushrooms from the world.
        /// Use with caution as this operation cannot be undone.
        /// </remarks>
        public void Destroy()
        {
            Object.Destroy(S1ShroomColony.gameObject);
        }
    }
}


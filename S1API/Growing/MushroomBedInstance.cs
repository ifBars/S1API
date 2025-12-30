#if (IL2CPPMELON)
using S1ObjectScripts = Il2CppScheduleOne.ObjectScripts;
using S1Growing = Il2CppScheduleOne.Growing;
#elif (MONOMELON || MONOBEPINEX || IL2CPPBEPINEX)
using S1ObjectScripts = ScheduleOne.ObjectScripts;
using S1Growing = ScheduleOne.Growing;
#endif

using S1API.Internal.Utils;
using UnityEngine;

namespace S1API.Growing
{
    /// <summary>
    /// Represents an instance of a mushroom bed in the world.
    /// </summary>
    /// <remarks>
    /// Mushroom beds are specialized growing containers that can hold mushroom colonies.
    /// They require cool temperatures (maintained by AC units) for optimal mushroom growth.
    /// </remarks>
    public sealed class MushroomBedInstance
    {
        /// <summary>
        /// INTERNAL: The in-game MushroomBed object.
        /// </summary>
        internal readonly S1ObjectScripts.MushroomBed S1MushroomBed;

        /// <summary>
        /// INTERNAL: Create a wrapper around an existing MushroomBed.
        /// </summary>
        /// <param name="mushroomBed">The in-game MushroomBed to wrap.</param>
        internal MushroomBedInstance(S1ObjectScripts.MushroomBed mushroomBed)
        {
            S1MushroomBed = mushroomBed;
        }

        /// <summary>
        /// The mushroom colony currently growing in this bed, or null if empty.
        /// </summary>
        public ShroomColonyInstance CurrentColony
        {
            get
            {
                var colony = S1MushroomBed.CurrentColony;
                return colony != null ? new ShroomColonyInstance(colony) : null;
            }
        }

        /// <summary>
        /// Whether this mushroom bed currently contains a growing mushroom colony.
        /// </summary>
        public bool ContainsGrowable() =>
            S1MushroomBed.ContainsGrowable();

        /// <summary>
        /// Gets the current growth progress of the mushroom colony (0.0 to 1.0).
        /// Returns 0 if no colony is present.
        /// </summary>
        public float GetGrowthProgressNormalized() =>
            S1MushroomBed.GetGrowthProgressNormalized();

        /// <summary>
        /// The GameObject of the mushroom bed.
        /// </summary>
        public GameObject GameObject =>
            S1MushroomBed.gameObject;

        /// <summary>
        /// The transform of the mushroom bed.
        /// </summary>
        public Transform Transform =>
            S1MushroomBed.transform;

        /// <summary>
        /// Gets the average temperature of the tiles under this mushroom bed.
        /// </summary>
        /// <returns>The average temperature in Fahrenheit.</returns>
        public float GetAverageTileTemperature() =>
            S1MushroomBed.GetAverageTileTemperature();

        /// <summary>
        /// Checks if the mushroom bed is ready for harvest.
        /// </summary>
        /// <param name="reason">Output parameter that contains the reason if not ready for harvest.</param>
        /// <returns>True if ready for harvest, false otherwise.</returns>
        public bool IsReadyForHarvest(out string reason) =>
            S1MushroomBed.IsReadyForHarvest(out reason);

        /// <summary>
        /// Gets the side length of the grow surface in world units.
        /// </summary>
        /// <returns>The side length of the internal grow surface.</returns>
        public float GetGrowSurfaceSideLength() =>
            S1MushroomBed.GetGrowSurfaceSideLength();
    }
}


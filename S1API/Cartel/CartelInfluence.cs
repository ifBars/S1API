#if (IL2CPPMELON)
using S1Cartel = Il2CppScheduleOne.Cartel;
using EMapRegion = Il2CppScheduleOne.Map.EMapRegion;
#elif (MONOMELON || MONOBEPINEX || IL2CPPBEPINEX)
using S1Cartel = ScheduleOne.Cartel;
using EMapRegion = ScheduleOne.Map.EMapRegion;
#endif

using System;
using S1API.Map;

namespace S1API.Cartel
{
    /// <summary>
    /// Provides access to cartel influence per map region.
    /// Influence ranges from 0 (no cartel presence) to 1 (full cartel control).
    /// </summary>
    public class CartelInfluence
    {
        /// <summary>
        /// INTERNAL: Reference to the game's CartelInfluence instance.
        /// </summary>
        internal readonly S1Cartel.CartelInfluence S1Influence;

        /// <summary>
        /// INTERNAL: Constructor to create a wrapper from a game CartelInfluence instance.
        /// </summary>
        internal CartelInfluence(S1Cartel.CartelInfluence influence)
        {
            S1Influence = influence;
        }

        /// <summary>
        /// Gets the cartel influence level for a specific region.
        /// </summary>
        /// <param name="region">The map region to check.</param>
        /// <returns>Influence level from 0.0 to 1.0</returns>
        public float GetInfluence(Region region)
        {
            return S1Influence.GetInfluence(ConvertToGameRegion(region));
        }

        /// <summary>
        /// Changes the cartel influence in a region by a specified amount.
        /// Positive values increase influence, negative values decrease it.
        /// </summary>
        /// <param name="region">The map region to modify.</param>
        /// <param name="amount">The amount to change (-1.0 to 1.0).</param>
        public void ChangeInfluence(Region region, float amount)
        {
            S1Influence.ChangeInfluence(ConvertToGameRegion(region), amount);
        }

#if !IL2CPPMELON
        /// <summary>
        /// Event fired when cartel influence changes in any region.
        /// Parameters: region, old influence, new influence.
        /// Note: This event is only available in Mono builds.
        /// </summary>
        public event Action<Region, float, float> OnInfluenceChanged
        {
            add
            {
                if (value == null) return;
                S1Influence.OnInfluenceChanged += (gameRegion, oldVal, newVal) =>
                {
                    value?.Invoke(ConvertFromGameRegion(gameRegion), oldVal, newVal);
                };
            }
            remove
            {
                // Note: Event removal is not fully supported due to delegate wrapping
            }
        }

        /// <summary>
        /// Converts game's EMapRegion to S1API Region.
        /// </summary>
        private static Region ConvertFromGameRegion(EMapRegion region)
        {
            return (Region)(int)region;
        }
#endif

        /// <summary>
        /// Converts S1API Region to game's EMapRegion.
        /// </summary>
        private static EMapRegion ConvertToGameRegion(Region region)
        {
            return (EMapRegion)(int)region;
        }
    }
}

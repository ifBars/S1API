#if (IL2CPPMELON)
using S1Packaging = Il2CppScheduleOne.Product.Packaging;
#elif (MONOMELON || MONOBEPINEX || IL2CPPBEPINEX)
using S1Packaging = ScheduleOne.Product.Packaging;
#endif

namespace S1API.Products.Packaging
{
    /// <summary>
    /// Represents the stealth level of packaging.
    /// </summary>
    public enum StealthLevel
    {
        /// <summary>
        /// No stealth level.
        /// </summary>
        None = 0,

        /// <summary>
        /// Basic stealth level.
        /// </summary>
        Basic = 1,

        /// <summary>
        /// Advanced stealth level.
        /// </summary>
        Advanced = 2
    }

    /// <summary>
    /// Provides extension methods for converting between <see cref="S1Packaging.EStealthLevel"/> and
    /// <see cref="StealthLevel"/> enumerations.
    /// </summary>
    internal static class StealthLevelExtensions
    {
        /// <summary>
        /// Converts an instance of <see cref="S1Packaging.EStealthLevel"/> to its corresponding
        /// <see cref="StealthLevel"/> representation.
        /// </summary>
        /// <param name="stealthLevel">The <see cref="S1Packaging.EStealthLevel"/> instance to convert.</param>
        /// <returns>A <see cref="StealthLevel"/> value that represents the converted stealth level.</returns>
        internal static StealthLevel ToAPI(this S1Packaging.EStealthLevel stealthLevel)
        {
            return stealthLevel switch
            {
                S1Packaging.EStealthLevel.None => StealthLevel.None,
                S1Packaging.EStealthLevel.Basic => StealthLevel.Basic,
                S1Packaging.EStealthLevel.Advanced => StealthLevel.Advanced,
                _ => StealthLevel.None,
            };
        }

        /// <summary>
        /// Converts an instance of the <see cref="StealthLevel"/> enum to its corresponding
        /// <see cref="S1Packaging.EStealthLevel"/> enum representation.
        /// </summary>
        /// <param name="stealthLevel">The <see cref="StealthLevel"/> enum value to convert.</param>
        /// <returns>The corresponding <see cref="S1Packaging.EStealthLevel"/> enum value.</returns>
        internal static S1Packaging.EStealthLevel ToInternal(this StealthLevel stealthLevel)
        {
            return stealthLevel switch
            {
                StealthLevel.None => S1Packaging.EStealthLevel.None,
                StealthLevel.Basic => S1Packaging.EStealthLevel.Basic,
                StealthLevel.Advanced => S1Packaging.EStealthLevel.Advanced,
                _ => S1Packaging.EStealthLevel.None,
            };
        }
    }
}

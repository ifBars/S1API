#if (IL2CPPMELON)
using S1Product = Il2CppScheduleOne.Product;
#elif (MONOMELON || MONOBEPINEX || IL2CPPBEPINEX)
using S1Product = ScheduleOne.Product;
#endif

using UnityEngine;

namespace S1API.Products
{
    /// <summary>
    /// Represents the visual appearance settings for mushroom products, including colors and spot patterns.
    /// </summary>
    public sealed class ShroomAppearanceSettings
    {
        /// <summary>
        /// INTERNAL: The underlying game appearance settings.
        /// </summary>
        internal readonly S1Product.ShroomAppearanceSettings S1AppearanceSettings;

        /// <summary>
        /// INTERNAL: Creates a wrapper around the game's ShroomAppearanceSettings.
        /// </summary>
        /// <param name="settings">The game's appearance settings to wrap.</param>
        internal ShroomAppearanceSettings(S1Product.ShroomAppearanceSettings settings)
        {
            S1AppearanceSettings = settings;
        }

        /// <summary>
        /// The default primary color used for mushrooms.
        /// </summary>
        public static Color32 DefaultPrimaryColor =>
            S1Product.ShroomAppearanceSettings.DefaultPrimaryColor;

        /// <summary>
        /// The default secondary color used for mushrooms.
        /// </summary>
        public static Color32 DefaultSecondaryColor =>
            S1Product.ShroomAppearanceSettings.DefaultSecondaryColor;

        /// <summary>
        /// The default spots color used for mushrooms.
        /// </summary>
        public static Color32 DefaultSpotsColor =>
            S1Product.ShroomAppearanceSettings.DefaultSpotsColor;

        /// <summary>
        /// The primary color of the mushroom.
        /// </summary>
        public Color32 PrimaryColor =>
            S1AppearanceSettings.PrimaryColor;

        /// <summary>
        /// The secondary color of the mushroom.
        /// </summary>
        public Color32 SecondaryColor =>
            S1AppearanceSettings.SecondaryColor;

        /// <summary>
        /// Whether this mushroom has spots on its cap.
        /// </summary>
        public bool HasSpots =>
            S1AppearanceSettings.HasSpots;

        /// <summary>
        /// The color of the spots on the mushroom cap (if HasSpots is true).
        /// </summary>
        public Color32 SpotsColor =>
            S1AppearanceSettings.SpotsColor;

        /// <summary>
        /// Checks if the appearance settings are uninitialized (have default/clear colors).
        /// </summary>
        /// <returns>True if the appearance settings are uninitialized, false otherwise.</returns>
        public bool IsUninitialized() =>
            S1AppearanceSettings.IsUnintialized();
    }
}


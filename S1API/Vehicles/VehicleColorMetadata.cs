using UnityEngine;

namespace S1API.Vehicles
{
    /// <summary>
    /// Describes a vehicle color using the base game's display metadata.
    /// </summary>
    public sealed class VehicleColorMetadata
    {
        internal VehicleColorMetadata(
            VehicleColor color,
            string? displayName,
            Color materialColor,
            Color32 uiColor)
        {
            Color = color;
            DisplayName = displayName;
            MaterialColor = materialColor;
            UIColor = uiColor;
        }

        /// <summary>
        /// Gets the vehicle color represented by this metadata.
        /// </summary>
        public VehicleColor Color { get; }

        /// <summary>
        /// Gets the display name configured for this color in the base game.
        /// </summary>
        public string? DisplayName { get; }

        /// <summary>
        /// Gets the color applied to vehicle materials.
        /// </summary>
        public Color MaterialColor { get; }

        /// <summary>
        /// Gets the color used for this color in the base game UI.
        /// </summary>
        public Color32 UIColor { get; }
    }
}

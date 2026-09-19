using UnityEngine;

namespace S1API.Items.Clothing
{
    /// <summary>
    /// Describes a clothing color using the base game's display metadata.
    /// </summary>
    public sealed class ClothingColorMetadata
    {
        internal ClothingColorMetadata(
            ClothingColor color,
            string displayName,
            Color actualColor,
            Color labelColor)
        {
            Color = color;
            DisplayName = displayName;
            ActualColor = actualColor;
            LabelColor = labelColor;
        }

        /// <summary>
        /// Gets the clothing color represented by this metadata.
        /// </summary>
        public ClothingColor Color { get; }

        /// <summary>
        /// Gets the clothing color enum identifier used by the base game as its label.
        /// </summary>
        public string DisplayName { get; }

        /// <summary>
        /// Gets the color applied to clothing materials.
        /// </summary>
        public Color ActualColor { get; }

        /// <summary>
        /// Gets the contrasting color used to label this color in the base game UI.
        /// </summary>
        public Color LabelColor { get; }
    }
}

using UnityEngine;

namespace S1API.Items.Clothing
{
    /// <summary>
    /// Describes a clothing slot using the base game's display metadata.
    /// </summary>
    public sealed class ClothingSlotMetadata
    {
        internal ClothingSlotMetadata(
            ClothingSlot slot,
            string displayName,
            Sprite? icon)
        {
            Slot = slot;
            DisplayName = displayName;
            Icon = icon;
        }

        /// <summary>
        /// Gets the clothing slot represented by this metadata.
        /// </summary>
        public ClothingSlot Slot { get; }

        /// <summary>
        /// Gets the base game's display name for the slot.
        /// </summary>
        public string DisplayName { get; }

        /// <summary>
        /// Gets the base game's icon for the slot, if one is configured.
        /// </summary>
        public Sprite? Icon { get; }
    }
}

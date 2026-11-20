using System;
using UnityEngine;
using S1API.Avatar;

namespace S1API.Shops
{
    /// <summary>
    /// Describes one option a mod lists in a character customization shop category.
    /// </summary>
    public sealed class CustomizationOptionSpec
    {
        /// <summary>
        /// Name of the shop category to list the option in, as the shop shows it (for example "Chest").
        /// Compared ignoring case, spaces and underscores. Required.
        /// </summary>
        public string Category = string.Empty;

        /// <summary>
        /// Text shown on the option button. Required.
        /// </summary>
        public string Label = string.Empty;

        /// <summary>
        /// Price in dollars. 0 shows "Free". Must not be negative.
        /// </summary>
        public float Price;

        /// <summary>
        /// Id of the avatar object the shop adds to the previewed appearance when the option is selected.
        /// It must exist in the avatar object library when the shop is built (custom avatar objects registered
        /// through S1API are included). Required.
        /// </summary>
        public string AvatarObjectId = string.Empty;

        /// <summary>
        /// Not shown yet: the vanilla option button has no icon slot. Setting it logs a warning.
        /// </summary>
        public Sprite? Icon;

        /// <summary>
        /// Optional. Called each time the shop marks the option as selected, with a snapshot of the appearance
        /// the shop is previewing (it already contains this option). That happens on a click or controller focus,
        /// when the category opens with the option applied or when the option is purchased, so it can run without a
        /// click and more than once per visit. The shop itself applies <see cref="AvatarObjectId"/>; changes made to
        /// the snapshot are not written back.
        /// </summary>
        public Action<BasicAvatarSettings>? Apply;
    }
}

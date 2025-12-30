using System;
using UnityEngine.UI;

namespace S1API.Utils
{
    /// <summary>
    /// Utilities for subscribing to and managing Toggle value change events in a
    /// cross-compatible way between Mono and IL2CPP. Handles Unity versions where
    /// Toggle.onValueChanged is exposed as either a field or a property.
    /// This class is intended for public use by mod developers.
    /// </summary>
    public static class ToggleUtils
    {
        /// <summary>
        /// Adds a listener to a Toggle's onValueChanged event in an IL2CPP-safe manner.
        /// </summary>
        public static void AddListener(Toggle toggle, Action<bool> listener) =>
            Internal.Utils.ToggleUtils.AddListener(toggle, listener);

        /// <summary>
        /// Removes a previously added listener from a Toggle's onValueChanged event.
        /// </summary>
        public static void RemoveListener(Toggle toggle, Action<bool> listener) =>
            Internal.Utils.ToggleUtils.RemoveListener(toggle, listener);

        /// <summary>
        /// Removes all listeners from a Toggle's onValueChanged event.
        /// </summary>
        public static void ClearListeners(Toggle toggle) =>
            Internal.Utils.ToggleUtils.ClearListeners(toggle);

        /// <summary>
        /// Sets the Toggle's checkmark graphic in a version-agnostic manner (field or property).
        /// </summary>
        public static void SetGraphic(Toggle toggle, Graphic graphic) =>
            Internal.Utils.ToggleUtils.SetGraphic(toggle, graphic);

        /// <summary>
        /// Gets the Toggle's checkmark graphic in a version-agnostic manner.
        /// </summary>
        public static Graphic? GetGraphic(Toggle toggle) =>
            Internal.Utils.ToggleUtils.GetGraphic(toggle);
    }
}


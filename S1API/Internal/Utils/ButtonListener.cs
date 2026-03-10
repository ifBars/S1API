using System;
using S1API.Internal.Abstraction;
using UnityEngine;
using UnityEngine.UI;

namespace S1API.Internal.Utils
{
    /// <summary>
    /// INTERNAL: Utility helpers for managing Unity UI <see cref="Button"/>s.
    /// This class is intended for internal API use only. Mod developers should use <see cref="S1API.Utils.ButtonUtils"/> instead.
    /// </summary>
    internal static class ButtonUtils
    {
        /// <summary>
        /// Adds a click listener to the specified button, ensuring compatibility with IL2CPP and Mono.
        /// </summary>
        internal static void AddListener(Button button, Action action)
        {
            if (button == null || action == null) return;
            EventHelper.AddListener(action, button.onClick);
        }

        /// <summary>
        /// Removes a previously added click listener from the specified button.
        /// </summary>
        internal static void RemoveListener(Button button, Action action)
        {
            if (button == null || action == null) return;
            EventHelper.RemoveListener(action, button.onClick);
        }

        /// <summary>
        /// Removes all listeners from the specified button safely.
        /// </summary>
        internal static void ClearListeners(Button button)
        {
            if (button == null) return;
            button.onClick.RemoveAllListeners();
        }

        /// <summary>
        /// Enables the button and optionally updates the label.
        /// </summary>
        internal static void Enable(Button button, Text? label = null, string? text = null)
        {
            if (button != null) button.interactable = true;
            if (label != null && !string.IsNullOrEmpty(text)) label.text = text;
        }

        /// <summary>
        /// Disables the button and optionally updates the label.
        /// </summary>
        internal static void Disable(Button button, Text? label = null, string? text = null)
        {
            if (button != null) button.interactable = false;
            if (label != null && !string.IsNullOrEmpty(text)) label.text = text;
        }

        /// <summary>
        /// Sets the label text of a button with a known Text child.
        /// </summary>
        internal static void SetLabel(Text label, string text)
        {
            if (label != null) label.text = text;
        }
        
        /// <summary>
        /// Sets the button label and background color.
        /// </summary>
        internal static void SetStyle(Button button, Text label, string text, Color bg)
        {
            if (button == null || label == null) return;
            label.text = text;
            button.image.color = bg;
        }
    }
}

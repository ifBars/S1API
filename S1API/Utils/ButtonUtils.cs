using System;
using UnityEngine;
using UnityEngine.UI;

namespace S1API.Utils
{
    /// <summary>
    /// Utility helpers for managing Unity UI <see cref="Button"/>s.
    /// This class is intended for public use by mod developers.
    /// </summary>
    public static class ButtonUtils
    {
        /// <summary>
        /// Adds a click listener to the specified button, ensuring compatibility with IL2CPP and Mono.
        /// </summary>
        public static void AddListener(Button button, Action action) =>
            Internal.Utils.ButtonUtils.AddListener(button, action);

        /// <summary>
        /// Removes a previously added click listener from the specified button.
        /// </summary>
        public static void RemoveListener(Button button, Action action) =>
            Internal.Utils.ButtonUtils.RemoveListener(button, action);

        /// <summary>
        /// Removes all listeners from the specified button safely.
        /// </summary>
        public static void ClearListeners(Button button) =>
            Internal.Utils.ButtonUtils.ClearListeners(button);

        /// <summary>
        /// Enables the button and optionally updates the label.
        /// </summary>
        public static void Enable(Button button, Text? label = null, string? text = null) =>
            Internal.Utils.ButtonUtils.Enable(button, label, text);

        /// <summary>
        /// Disables the button and optionally updates the label.
        /// </summary>
        public static void Disable(Button button, Text? label = null, string? text = null) =>
            Internal.Utils.ButtonUtils.Disable(button, label, text);

        /// <summary>
        /// Sets the label text of a button with a known Text child.
        /// </summary>
        public static void SetLabel(Text label, string text) =>
            Internal.Utils.ButtonUtils.SetLabel(label, text);

        /// <summary>
        /// Sets the button label and background color.
        /// </summary>
        public static void SetStyle(Button button, Text label, string text, Color bg) =>
            Internal.Utils.ButtonUtils.SetStyle(button, label, text, bg);
    }
}


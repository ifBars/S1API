using System;
using System.Reflection;
using S1API.Internal.Abstraction;
using UnityEngine.Events;
using UnityEngine.UI;

namespace S1API.Internal.Utils
{
    /// <summary>
    /// INTERNAL: Utilities for subscribing to and managing Toggle value change events in a
    /// cross-compatible way between Mono and IL2CPP. Handles Unity versions where
    /// Toggle.onValueChanged is exposed as either a field or a property.
    /// This class is intended for internal API use only. Mod developers should use <see cref="S1API.Utils.ToggleUtils"/> instead.
    /// </summary>
    internal static class ToggleUtils
    {
        private static FieldInfo? _onValueChangedField;
        private static PropertyInfo? _onValueChangedProperty;
        private static FieldInfo? _graphicField;
        private static PropertyInfo? _graphicProperty;

        /// <summary>
        /// Adds a listener to a Toggle's onValueChanged event in an IL2CPP-safe manner.
        /// </summary>
        internal static void AddListener(Toggle toggle, Action<bool> listener)
        {
            if (toggle == null)
                return;

            if (!TryGetOnValueChanged(toggle, out UnityEvent<bool>? evt) || evt == null)
                return;

            EventHelper.AddListener(listener, evt);
        }

        /// <summary>
        /// Removes a previously added listener from a Toggle's onValueChanged event.
        /// </summary>
        internal static void RemoveListener(Toggle toggle, Action<bool> listener)
        {
            if (toggle == null)
                return;

            if (!TryGetOnValueChanged(toggle, out UnityEvent<bool>? evt) || evt == null)
                return;

            EventHelper.RemoveListener(listener, evt);
        }

        /// <summary>
        /// Removes all listeners from a Toggle's onValueChanged event.
        /// </summary>
        internal static void ClearListeners(Toggle toggle)
        {
            if (!TryGetOnValueChanged(toggle, out UnityEvent<bool>? evt) || evt == null)
                return;

            evt.RemoveAllListeners();
        }

        private static bool TryGetOnValueChanged(Toggle toggle, out UnityEvent<bool>? evt)
        {
            evt = null;

            _onValueChangedField ??= typeof(Toggle).GetField("onValueChanged", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (_onValueChangedField != null)
            {
                if (_onValueChangedField.GetValue(toggle) is UnityEvent<bool> fieldEvt)
                {
                    evt = fieldEvt;
                    return true;
                }
            }

            _onValueChangedProperty ??= typeof(Toggle).GetProperty("onValueChanged", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (_onValueChangedProperty?.GetMethod != null)
            {
                if (_onValueChangedProperty.GetValue(toggle) is UnityEvent<bool> propEvt)
                {
                    evt = propEvt;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Sets the Toggle's checkmark graphic in a version-agnostic manner (field or property).
        /// </summary>
        internal static void SetGraphic(Toggle toggle, Graphic graphic)
        {
            if (toggle == null)
                return;

            _graphicField ??= typeof(Toggle).GetField("graphic", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (_graphicField != null)
            {
                _graphicField.SetValue(toggle, graphic);
                return;
            }

            _graphicProperty ??= typeof(Toggle).GetProperty("graphic", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (_graphicProperty?.SetMethod != null)
            {
                _graphicProperty.SetValue(toggle, graphic);
            }
        }

        /// <summary>
        /// Gets the Toggle's checkmark graphic in a version-agnostic manner.
        /// </summary>
        internal static Graphic? GetGraphic(Toggle toggle)
        {
            if (toggle == null)
                return null;

            _graphicField ??= typeof(Toggle).GetField("graphic", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (_graphicField != null)
            {
                return _graphicField.GetValue(toggle) as Graphic;
            }

            _graphicProperty ??= typeof(Toggle).GetProperty("graphic", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (_graphicProperty?.GetMethod != null)
            {
                return _graphicProperty.GetValue(toggle) as Graphic;
            }

            return null;
        }
    }
}
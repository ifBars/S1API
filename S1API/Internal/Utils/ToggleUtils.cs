using System;
using System.Reflection;
using S1API.Internal.Abstraction;
using UnityEngine.Events;
using UnityEngine.UI;

namespace S1API.Internal.Utils
{
    /// <summary>
    /// Utilities for subscribing to and managing Toggle value change events in a
    /// cross-compatible way between Mono and IL2CPP. Handles Unity versions where
    /// Toggle.onValueChanged is exposed as either a field or a property.
    /// </summary>
    public static class ToggleUtils
    {
        private static FieldInfo? _onValueChangedField;
        private static PropertyInfo? _onValueChangedProperty;

        /// <summary>
        /// Adds a listener to a Toggle's onValueChanged event in an IL2CPP-safe manner.
        /// </summary>
        public static void AddListener(Toggle toggle, Action<bool> listener)
        {
            if (toggle == null || listener == null)
                return;

            if (!TryGetOnValueChanged(toggle, out UnityEvent<bool>? evt) || evt == null)
                return;

            EventHelper.AddListener(listener, evt);
        }

        /// <summary>
        /// Removes a previously added listener from a Toggle's onValueChanged event.
        /// </summary>
        public static void RemoveListener(Toggle toggle, Action<bool> listener)
        {
            if (toggle == null || listener == null)
                return;

            if (!TryGetOnValueChanged(toggle, out UnityEvent<bool>? evt) || evt == null)
                return;

            EventHelper.RemoveListener(listener, evt);
        }

        /// <summary>
        /// Removes all listeners from a Toggle's onValueChanged event.
        /// </summary>
        public static void ClearListeners(Toggle toggle)
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
    }
}
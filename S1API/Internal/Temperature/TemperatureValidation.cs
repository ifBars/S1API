using System;
using UnityEngine;

namespace S1API.Internal.Temperature
{
    internal static class TemperatureValidation
    {
        internal const float DefaultAmbientTemperature = 20f;
        internal const float MinTemperature = 0f;
        internal const float MaxTemperature = 40f;
        internal const float DefaultRange = 5f;
        internal const float MinRange = 0.1f;
        internal const float MaxRange = 100f;

        internal static float ClampTemperature(float temperature, string parameterName)
        {
            EnsureFinite(temperature, parameterName);
            return Clamp(temperature, MinTemperature, MaxTemperature);
        }

        internal static float ClampRange(float range, string parameterName)
        {
            EnsureFinite(range, parameterName);
            return Clamp(range, MinRange, MaxRange);
        }

        internal static void EnsureFinite(Vector3 position, string parameterName)
        {
            if (!IsFinite(position.x) || !IsFinite(position.y) || !IsFinite(position.z))
                throw new ArgumentOutOfRangeException(parameterName, "Vector components must be finite.");
        }

        internal static void EnsureFinite(float value, string parameterName)
        {
            if (!IsFinite(value))
                throw new ArgumentOutOfRangeException(parameterName, "Value must be finite.");
        }

        private static float Clamp(float value, float minimum, float maximum) =>
            value < minimum ? minimum : value > maximum ? maximum : value;

        private static bool IsFinite(float value) =>
            !float.IsNaN(value) && !float.IsInfinity(value);
    }
}

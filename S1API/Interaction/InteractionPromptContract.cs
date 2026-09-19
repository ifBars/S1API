using System;
using UnityEngine;

namespace S1API.Interaction
{
    internal static class InteractionPromptContract
    {
        internal const float NativeMaxInteractionRange = 4f;
        internal const float DefaultAngleLimit = 90f;

        internal static string NormalizeMessage(string message)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            if (string.IsNullOrWhiteSpace(message))
                throw new ArgumentException("Interaction prompt messages cannot be empty or whitespace.", nameof(message));

            return message;
        }

        internal static float NormalizeRange(float range)
        {
            if (float.IsNaN(range) || float.IsInfinity(range) || range <= 0f || range > NativeMaxInteractionRange)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(range),
                    "Interaction prompt range must be finite, greater than zero, and at most four metres.");
            }

            return range;
        }

        internal static float NormalizeAngleLimit(float angleLimit)
        {
            if (float.IsNaN(angleLimit) || float.IsInfinity(angleLimit) || angleLimit <= 0f || angleLimit > 180f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(angleLimit),
                    "Interaction prompt angle limits must be finite, greater than zero, and at most 180 degrees.");
            }

            return angleLimit;
        }

        internal static void ValidateEnum<TEnum>(TEnum value, string parameterName)
            where TEnum : struct, Enum
        {
            if (!Enum.IsDefined(typeof(TEnum), value))
                throw new ArgumentOutOfRangeException(parameterName, value, "The interaction prompt value is not defined.");
        }

        internal static void ValidateTarget(GameObject target)
        {
            if (ReferenceEquals(target, null) || target == null)
                throw new ArgumentNullException(nameof(target));
        }
    }
}

#if (IL2CPPMELON)
using S1Law = Il2CppScheduleOne.Law;
#elif (MONOMELON || MONOBEPINEX || IL2CPPBEPINEX)
using S1Law = ScheduleOne.Law;
#endif

namespace S1API.Law
{
    /// <summary>
    /// Controls law enforcement intensity and automatic activity systems.
    /// Manages the automatic evaluation of checkpoints, patrols, and other law enforcement activities.
    /// </summary>
    /// <remarks>
    /// Law enforcement intensity (1-10) determines which automatic activities are enabled.
    /// The automatic evaluation system runs every in-game minute based on intensity, time, day, and curfew status.
    /// To prevent automatic checkpoint activation, set intensity to 1-4.
    /// </remarks>
    public static class LawController
    {
        /// <summary>
        /// INTERNAL: Provides access to the underlying law controller singleton.
        /// </summary>
        private static S1Law.LawController Internal =>
            S1Law.LawController.Instance;

        #region Intensity Management

        /// <summary>
        /// Gets the current law enforcement intensity level (1-10).
        /// </summary>
        /// <remarks>
        /// Higher values trigger more aggressive automatic law enforcement activities.
        /// Checkpoint activation typically requires intensity level 5 or higher.
        /// </remarks>
        public static int Intensity
        {
            get
            {
                if (Internal == null) return 1;
#if (IL2CPPMELON)
                return Internal.LE_Intensity;
#else
                return Internal.LE_Intensity;
#endif
            }
        }

        /// <summary>
        /// Gets the internal law enforcement intensity as a normalized value (0.0-1.0).
        /// </summary>
        /// <remarks>
        /// This is the underlying value used by the game. The public <see cref="Intensity"/> property
        /// is derived from this value mapped to a 1-10 range.
        /// </remarks>
        public static float InternalIntensity
        {
            get
            {
                if (Internal == null) return 0f;
#if (IL2CPPMELON)
                // Access via reflection as it's a private field in IL2CPP
                return 0f; // Safe fallback - modders should use Intensity property instead
#else
                // In Mono we can access the field directly through reflection if needed
                var fieldInfo = typeof(S1Law.LawController).GetField("internalLawIntensity",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (fieldInfo != null)
                {
                    return (float)fieldInfo.GetValue(Internal);
                }
                return 0f;
#endif
            }
        }

        /// <summary>
        /// Changes the law enforcement intensity by a specified amount.
        /// </summary>
        /// <param name="change">The amount to change intensity by (can be negative). Value is clamped internally.</param>
        /// <remarks>
        /// The internal intensity is stored as a 0.0-1.0 value and mapped to 1-10 for display.
        /// Small changes (e.g., 0.1) will have a noticeable effect.
        /// </remarks>
        public static void ChangeIntensity(float change)
        {
            if (Internal == null) return;
            Internal.ChangeInternalIntensity(change);
        }

        /// <summary>
        /// Sets the law enforcement intensity to a specific normalized value (0.0-1.0).
        /// </summary>
        /// <param name="intensity">The intensity value to set (0.0 = minimum, 1.0 = maximum). Will be clamped.</param>
        /// <remarks>
        /// This sets the internal intensity directly. The public <see cref="Intensity"/> property
        /// will reflect the change as a 1-10 value.
        /// </remarks>
        public static void SetInternalIntensity(float intensity)
        {
            if (Internal == null) return;
            Internal.SetInternalIntensity(intensity);
        }

        /// <summary>
        /// Sets the law enforcement intensity to a specific level (1-10).
        /// </summary>
        /// <param name="level">The intensity level to set (1-10). Will be clamped.</param>
        /// <remarks>
        /// This is a convenience method that converts a 1-10 level to the internal 0.0-1.0 range.
        /// To prevent automatic checkpoint activation, use level 1-4.
        /// </remarks>
        public static void SetIntensityLevel(int level)
        {
            if (Internal == null) return;
            // Convert 1-10 range to 0.0-1.0 internal range
            float normalizedIntensity = UnityEngine.Mathf.InverseLerp(1f, 10f, level);
            Internal.SetInternalIntensity(normalizedIntensity);
        }

        #endregion

        #region Activity Settings Control

        /// <summary>
        /// Gets whether custom activity settings are currently overriding the default day-based settings.
        /// </summary>
        /// <remarks>
        /// When true, the game uses <see cref="OverrideActivitySettings"/> instead of
        /// day-specific settings (Monday, Tuesday, etc.).
        /// </remarks>
        public static bool IsUsingOverrideSettings
        {
            get
            {
                if (Internal == null) return false;
#if (IL2CPPMELON)
                return Internal.OverrideSettings;
#else
                return Internal.OverrideSettings;
#endif
            }
        }

        /// <summary>
        /// Overrides the default day-based activity settings with custom settings.
        /// </summary>
        /// <param name="settings">The custom activity settings to use, or null to clear the override.</param>
        /// <remarks>
        /// WARNING: Advanced feature. The settings object is not part of the public API and must be obtained from game internals.
        /// To simply disable automatic activities, set <see cref="Intensity"/> to 1 instead.
        /// </remarks>
        public static void OverrideActivitySettings(S1Law.LawActivitySettings settings)
        {
            if (Internal == null) return;
            if (settings != null)
            {
                Internal.OverrideSetings(settings); // Note: Typo exists in game code
            }
            else
            {
                Internal.EndOverride();
            }
        }

        /// <summary>
        /// Clears any activity settings override and returns to using day-based settings.
        /// </summary>
        public static void ClearActivitySettingsOverride()
        {
            if (Internal == null) return;
            Internal.EndOverride();
        }

        #endregion

        #region Constants

        /// <summary>
        /// The minimum law enforcement intensity level.
        /// </summary>
        public const int MinIntensity = 1;

        /// <summary>
        /// The maximum law enforcement intensity level.
        /// </summary>
        public const int MaxIntensity = 10;

        /// <summary>
        /// The amount of internal intensity that increases per day naturally.
        /// Note: This constant exists in the game code but the actual increase is controlled by IntensityIncreasePerDay.
        /// </summary>
        public const float DailyIntensityDrain = 0.05f;

        #endregion
    }
}


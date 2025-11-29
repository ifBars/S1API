#if (IL2CPPMELON)
using S1Law = Il2CppScheduleOne.Law;
using S1GameTime = Il2CppScheduleOne.GameTime;
#elif (MONOMELON || MONOBEPINEX || IL2CPPBEPINEX)
using S1Law = ScheduleOne.Law;
using S1GameTime = ScheduleOne.GameTime;
#endif

namespace S1API.Law
{
    /// <summary>
    /// Manages the curfew system, including activation state and timing.
    /// The curfew restricts player movement during nighttime hours (9 PM to 5 AM).
    /// </summary>
    public static class CurfewManager
    {
        /// <summary>
        /// INTERNAL: Provides access to the underlying curfew manager singleton.
        /// </summary>
        private static S1Law.CurfewManager Internal =>
            S1Law.CurfewManager.Instance;

        /// <summary>
        /// The time (in 24-hour format) when curfew warnings begin appearing.
        /// Default: 20:00 (8 PM) - Two hours before curfew starts.
        /// </summary>
        public const int HourBeforeCurfew = 2000;

        /// <summary>
        /// The time (in 24-hour format) when the 30-minute warning is displayed.
        /// Default: 20:30 (8:30 PM).
        /// </summary>
        public const int WarningTime = 2030;

        /// <summary>
        /// The time (in 24-hour format) when the curfew officially begins.
        /// Default: 21:00 (9 PM).
        /// </summary>
        public const int CurfewStartTime = 2100;

        /// <summary>
        /// The time (in 24-hour format) when the hard curfew begins.
        /// During hard curfew, violations result in immediate police response.
        /// Default: 21:15 (9:15 PM).
        /// </summary>
        public const int HardCurfewStartTime = 2115;

        /// <summary>
        /// The time (in 24-hour format) when the curfew ends.
        /// Default: 05:00 (5 AM).
        /// </summary>
        public const int CurfewEndTime = 500;

        /// <summary>
        /// Gets a value indicating whether the curfew system is enabled.
        /// When disabled, no curfew violations will occur.
        /// </summary>
        public static bool IsEnabled =>
            Internal != null && Internal.IsEnabled;

        /// <summary>
        /// Gets a value indicating whether the curfew is currently active.
        /// True between 9 PM and 5 AM when curfew is enabled.
        /// </summary>
        public static bool IsCurrentlyActive =>
            Internal != null && Internal.IsCurrentlyActive;

        /// <summary>
        /// Gets a value indicating whether hard curfew is currently active.
        /// During hard curfew (9:15 PM - 5 AM), violations result in immediate police dispatch.
        /// </summary>
        public static bool IsHardCurfewActive =>
            Internal != null && Internal.IsHardCurfewActive;

        /// <summary>
        /// Enables the curfew system.
        /// VMS boards will display curfew information and violations will be enforced.
        /// </summary>
        public static void EnableCurfew()
        {
            if (Internal == null) return;
            Internal.Enable(null);
        }

        /// <summary>
        /// Disables the curfew system.
        /// VMS boards will be hidden and no curfew violations will occur.
        /// </summary>
        public static void DisableCurfew()
        {
            if (Internal == null) return;
            Internal.Disable();
        }

        /// <summary>
        /// Checks if the current game time is within the curfew period.
        /// </summary>
        /// <returns>True if the current time is between 9 PM and 5 AM.</returns>
        public static bool IsWithinCurfewHours()
        {
            if (!S1GameTime.TimeManager.InstanceExists) return false;
            return S1GameTime.TimeManager.Instance.IsCurrentTimeWithinRange(CurfewStartTime, CurfewEndTime);
        }

        /// <summary>
        /// Checks if the current game time is within the hard curfew period.
        /// </summary>
        /// <returns>True if the current time is between 9:15 PM and 5 AM.</returns>
        public static bool IsWithinHardCurfewHours()
        {
            if (!S1GameTime.TimeManager.InstanceExists) return false;
            return S1GameTime.TimeManager.Instance.IsCurrentTimeWithinRange(HardCurfewStartTime, CurfewEndTime);
        }

        /// <summary>
        /// Gets the number of minutes until curfew starts.
        /// </summary>
        /// <returns>Minutes until curfew, or 0 if curfew is already active or disabled.</returns>
        public static int MinutesUntilCurfew()
        {
            if (!S1GameTime.TimeManager.InstanceExists) return 0;

            var currentTime = S1GameTime.TimeManager.Instance.CurrentTime;
            if (S1GameTime.TimeManager.Instance.IsCurrentTimeWithinRange(CurfewStartTime, CurfewEndTime))
                return 0;

            return S1GameTime.TimeManager.GetMinSumFrom24HourTime(CurfewStartTime) -
                   S1GameTime.TimeManager.GetMinSumFrom24HourTime(currentTime);
        }

        /// <summary>
        /// Gets the number of minutes until curfew ends.
        /// </summary>
        /// <returns>Minutes until curfew ends, or 0 if curfew is not currently active.</returns>
        public static int MinutesUntilCurfewEnds()
        {
            if (!S1GameTime.TimeManager.InstanceExists) return 0;

            var currentTime = S1GameTime.TimeManager.Instance.CurrentTime;
            if (!S1GameTime.TimeManager.Instance.IsCurrentTimeWithinRange(CurfewStartTime, CurfewEndTime))
                return 0;

            return S1GameTime.TimeManager.GetMinSumFrom24HourTime(CurfewEndTime) -
                   S1GameTime.TimeManager.GetMinSumFrom24HourTime(currentTime);
        }
    }
}

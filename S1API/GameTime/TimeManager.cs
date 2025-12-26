#if (IL2CPPMELON)
using S1GameTime = Il2CppScheduleOne.GameTime;
using Il2CppInterop.Runtime;
#elif (MONOMELON || MONOBEPINEX || IL2CPPBEPINEX)
using S1GameTime = ScheduleOne.GameTime;
#endif

using System;
using System.Reflection;

namespace S1API.GameTime
{
    /// <summary>
    /// Provides access to various time management functions in the game.
    /// </summary>
    public static class TimeManager
    {
        /// <summary>
        /// Called when a new in-game day starts.
        /// </summary>
        public static Action OnDayPass = delegate { };

        /// <summary>
        /// Called when a new in-game week starts.
        /// </summary>
        public static Action OnWeekPass = delegate { };

        /// <summary>
        /// Called when the player starts sleeping.
        /// </summary>
        public static Action OnSleepStart = delegate { };

        /// <summary>
        /// Called when the player finishes sleeping.
        /// Parameter: total minutes skipped during sleep.
        /// </summary>
        public static Action<int> OnSleepEnd = delegate { };

        /// <summary>
        /// Called at every tick of gametime.
        /// </summary>
        public static Action OnTick = delegate { };

        private static int _lastSleepSkippedMinutes;
        private static S1GameTime.TimeManager _boundInstance;

        private static readonly Action DayPassHandler = () => OnDayPass();
        private static readonly Action WeekPassHandler = () => OnWeekPass();
        private static readonly Action TickHandler = () => OnTick();
        private static readonly Action SleepStartHandler = () => OnSleepStart();
        private static readonly Action<int> TimeSkipHandler = minutes => _lastSleepSkippedMinutes = minutes;
        private static readonly Action SleepEndHandler = () => OnSleepEnd(_lastSleepSkippedMinutes);

        static TimeManager()
        {
            TryBindToCurrentInstance();
        }

        /// <summary>
        /// INTERNAL: Rebinds S1API time events to the current game TimeManager instance.
        /// Safe to call multiple times; no-op if already bound to the current instance.
        /// </summary>
        internal static void TryBindToCurrentInstance()
        {
            var instance = S1GameTime.TimeManager.Instance;
            if (instance == null || ReferenceEquals(instance, _boundInstance))
                return;

            UnbindFromInstance(_boundInstance);
            _boundInstance = instance;

            instance.onDayPass += DayPassHandler;
            instance.onWeekPass += WeekPassHandler;
            
            AddToActionList(instance.onTick, TickHandler);
            
            instance.onSleepStart += SleepStartHandler;
            instance.onTimeSkip += TimeSkipHandler;
            instance.onSleepEnd += SleepEndHandler;
        }

        /// <summary>
        /// INTERNAL: Clears bindings so the next scene load can attach to a fresh instance.
        /// </summary>
        internal static void ResetBindings()
        {
            UnbindFromInstance(_boundInstance);
            _boundInstance = null;
            _lastSleepSkippedMinutes = 0;
        }

        private static void UnbindFromInstance(S1GameTime.TimeManager instance)
        {
            if (instance == null)
                return;

            instance.onDayPass -= DayPassHandler;
            instance.onWeekPass -= WeekPassHandler;
            
            RemoveFromActionList(instance.onTick, TickHandler);
            
            instance.onSleepStart -= SleepStartHandler;
            instance.onTimeSkip -= TimeSkipHandler;
            instance.onSleepEnd -= SleepEndHandler;
        }

        private static void AddToActionList(object actionList, Action handler)
        {
            if (actionList == null) return;
            var method = actionList.GetType().GetMethod("Add", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (method == null) return;

            object param = handler;
#if IL2CPPMELON
            var parameters = method.GetParameters();
            if (parameters.Length > 0 && parameters[0].ParameterType == typeof(Il2CppSystem.Action))
            {
                param = DelegateSupport.ConvertDelegate<Il2CppSystem.Action>(handler);
            }
#endif
            method.Invoke(actionList, new object[] { param });
        }

        private static void RemoveFromActionList(object actionList, Action handler)
        {
            if (actionList == null) return;
            var method = actionList.GetType().GetMethod("Remove", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (method == null) return;

            object param = handler;
#if IL2CPPMELON
            var parameters = method.GetParameters();
            if (parameters.Length > 0 && parameters[0].ParameterType == typeof(Il2CppSystem.Action))
            {
                param = DelegateSupport.ConvertDelegate<Il2CppSystem.Action>(handler);
            }
#endif
            method.Invoke(actionList, new object[] { param });
        }

        /// <summary>
        /// The current in-game day (Monday, Tuesday, etc.).
        /// </summary>
        public static Day CurrentDay => (Day)S1GameTime.TimeManager.Instance.CurrentDay;

        /// <summary>
        /// The number of in-game days elapsed.
        /// </summary>
        public static int ElapsedDays => S1GameTime.TimeManager.Instance.ElapsedDays;

        /// <summary>
        /// The current 24-hour time (e.g., 1330 for 1:30 PM).
        /// </summary>
        public static int CurrentTime => S1GameTime.TimeManager.Instance.CurrentTime;

        /// <summary>
        /// Whether it is currently nighttime in-game.
        /// </summary>
        public static bool IsNight => S1GameTime.TimeManager.Instance.IsNight;

        /// <summary>
        /// Whether the game is currently at the end of the day (4:00 AM).
        /// </summary>
        public static bool IsEndOfDay => S1GameTime.TimeManager.Instance.IsEndOfDay;

        /// <summary>
        /// Whether the player is currently sleeping.
        /// </summary>
        public static bool SleepInProgress => S1GameTime.TimeManager.Instance.SleepInProgress;

        /// <summary>
        /// Whether the time is currently overridden (frozen or custom).
        /// </summary>
        public static bool TimeOverridden => S1GameTime.TimeManager.Instance.TimeOverridden;

        /// <summary>
        /// The current normalized time of day (0.0 = start, 1.0 = end).
        /// </summary>
        public static float NormalizedTime => S1GameTime.TimeManager.Instance.NormalizedTime;

        /// <summary>
        /// Total playtime (in seconds).
        /// </summary>
        public static float Playtime => S1GameTime.TimeManager.Instance.Playtime;

        /// <summary>
        /// Fast-forwards time to morning wake time (7:00 AM).
        /// </summary>
        public static void FastForwardToWakeTime() => S1GameTime.TimeManager.Instance.FastForwardToWakeTime();

        /// <summary>
        /// Sets the current time manually.
        /// </summary>
        public static void SetTime(int time24h, bool local = false) => S1GameTime.TimeManager.Instance.SetTime(time24h, local);

        /// <summary>
        /// Sets the number of elapsed in-game days.
        /// </summary>
        public static void SetElapsedDays(int days) => S1GameTime.TimeManager.Instance.SetElapsedDays(days);

        /// <summary>
        /// Gets the current time formatted in 12-hour AM/PM format.
        /// </summary>
        public static string GetFormatted12HourTime()
        {
            return S1GameTime.TimeManager.Get12HourTime(CurrentTime, true);
        }

        /// <summary>
        /// Returns true if the current time is within the specified 24-hour range.
        /// </summary>
        public static bool IsCurrentTimeWithinRange(int startTime24h, int endTime24h)
        {
            return S1GameTime.TimeManager.Instance.IsCurrentTimeWithinRange(startTime24h, endTime24h);
        }

        /// <summary>
        /// Converts 24-hour time to total minutes.
        /// </summary>
        public static int GetMinutesFrom24HourTime(int time24h)
        {
            return S1GameTime.TimeManager.GetMinSumFrom24HourTime(time24h);
        }

        /// <summary>
        /// Converts total minutes into 24-hour time format.
        /// </summary>
        public static int Get24HourTimeFromMinutes(int minutes)
        {
            return S1GameTime.TimeManager.Get24HourTimeFromMinSum(minutes);
        }
    }
}

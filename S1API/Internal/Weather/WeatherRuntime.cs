#if IL2CPPMELON
using S1EnvironmentHandler = Il2CppScheduleOne.Core.Weather.EnvironmentHandler;
using S1WeatherChangeHandler = Il2CppScheduleOne.Core.Weather.WeatherChangeHandler;
using S1WeatherConditions = Il2CppScheduleOne.Core.Weather.WeatherConditions;
using S1WeatherSequence = Il2CppScheduleOne.Core.Weather.WeatherSequence;
using S1EnvironmentManager = Il2CppScheduleOne.Weather.EnvironmentManager;
using S1DevUtilities = Il2CppScheduleOne.DevUtilities;
using Il2CppInterop.Runtime;
#elif MONOMELON
using S1EnvironmentHandler = ScheduleOne.Core.Weather.EnvironmentHandler;
using S1WeatherChangeHandler = ScheduleOne.Core.Weather.WeatherChangeHandler;
using S1WeatherConditions = ScheduleOne.Core.Weather.WeatherConditions;
using S1WeatherSequence = ScheduleOne.Core.Weather.WeatherSequence;
using S1EnvironmentManager = ScheduleOne.Weather.EnvironmentManager;
using S1DevUtilities = ScheduleOne.DevUtilities;
#endif

using System;
using System.Collections.Generic;
#if MONOMELON
using System.Reflection;
#endif
using S1API.Logging;

namespace S1API.Internal.Weather
{
    /// <summary>
    /// INTERNAL: Bridges the native weather callback and sequence collection to managed API
    /// snapshots.
    /// </summary>
    internal static class WeatherRuntime
    {
        private const float RefreshIntervalSeconds = 0.1f;
        private static readonly Log Logger = new Log("WeatherRuntime");

#if MONOMELON
        private static readonly FieldInfo? CurrentWeatherConditionsField =
            typeof(S1EnvironmentManager).GetField(
                "_currentWeatherConditions",
                BindingFlags.Instance | BindingFlags.NonPublic);
#endif

#if IL2CPPMELON
        private static readonly S1WeatherChangeHandler WeatherChangeHandler =
            DelegateSupport.ConvertDelegate<S1WeatherChangeHandler>(OnNativeWeatherChanged)
            ?? throw new InvalidOperationException("Could not convert the weather callback delegate.");
#else
        private static readonly S1WeatherChangeHandler WeatherChangeHandler = OnNativeWeatherChanged;
#endif

        private static S1EnvironmentManager? _boundInstance;
        private static bool _isBound;
        private static bool _refreshFailureLogged;
        private static float _nextRefreshTime;

        /// <summary>
        /// INTERNAL: Binds the managed callback to the current Main-scene weather manager.
        /// </summary>
        internal static void TryBindToCurrentInstance()
        {
            try
            {
                S1EnvironmentManager? instance = GetNativeEnvironmentManager();
                if (instance == null)
                    return;

                if (_isBound && ReferenceEquals(instance, _boundInstance))
                    return;

                if (_isBound)
                    Unbind();

                S1EnvironmentHandler.SubscribeToWeatherChange(WeatherChangeHandler);
                _boundInstance = instance;
                _isBound = true;
                PublishCurrentState(instance);
            }
            catch (Exception ex)
            {
                Logger.Warning($"Failed to bind to the native weather callback: {ex.Message}");
            }
        }

        /// <summary>
        /// INTERNAL: Refreshes the managed snapshot from the live weather manager.
        /// </summary>
        /// <remarks>
        /// The game's weather callback is not raised by the current weather implementation, so
        /// polling the manager is required to observe normal weather-volume transitions.
        /// </remarks>
        internal static void Tick()
        {
            if (UnityEngine.Time.unscaledTime < _nextRefreshTime)
                return;

            _nextRefreshTime = UnityEngine.Time.unscaledTime + RefreshIntervalSeconds;
            TryBindToCurrentInstance();

            if (_boundInstance == null)
                return;

            try
            {
                PublishCurrentState(_boundInstance);
                _refreshFailureLogged = false;
            }
            catch (Exception ex)
            {
                if (!_refreshFailureLogged)
                {
                    Logger.Warning($"Failed to refresh the current weather state: {ex.Message}");
                    _refreshFailureLogged = true;
                }
            }
        }

        /// <summary>
        /// INTERNAL: Removes the native callback and clears scene-bound managed state.
        /// </summary>
        internal static void ResetBindings()
        {
            try
            {
                if (_isBound)
                    Unbind();
            }
            catch (Exception ex)
            {
                Logger.Warning($"Failed to unbind the native weather callback: {ex.Message}");
            }
            finally
            {
                _boundInstance = null;
                _isBound = false;
                _refreshFailureLogged = false;
                _nextRefreshTime = 0f;
                global::S1API.Weather.WeatherManager.ResetState();
            }
        }

        /// <summary>
        /// INTERNAL: Returns a managed copy of the native sequence identifiers.
        /// </summary>
        internal static IReadOnlyList<string> GetKnownSequenceIds()
        {
            try
            {
                S1EnvironmentManager? manager = GetNativeEnvironmentManager();
                if (manager?.WeatherSequences == null)
                    return global::S1API.Weather.WeatherManager.SnapshotSequenceIds(
                        Array.Empty<string>());

                var ids = new List<string?>();
#if IL2CPPMELON
                for (int i = 0; i < manager.WeatherSequences.Count; i++)
                {
                    S1WeatherSequence? sequence = manager.WeatherSequences[i];
                    AddSequenceId(ids, sequence);
                }
#else
                foreach (S1WeatherSequence? sequence in manager.WeatherSequences)
                    AddSequenceId(ids, sequence);
#endif

                return global::S1API.Weather.WeatherManager.SnapshotSequenceIds(ids);
            }
            catch
            {
                return global::S1API.Weather.WeatherManager.SnapshotSequenceIds(
                    Array.Empty<string>());
            }
        }

        private static void AddSequenceId(List<string?> ids, S1WeatherSequence? sequence)
        {
            if (sequence == null)
                return;

            ids.Add(sequence.Id);
        }

        private static S1EnvironmentManager? GetNativeEnvironmentManager()
        {
            try
            {
                if (!S1DevUtilities.NetworkSingleton<S1EnvironmentManager>.InstanceExists)
                    return null;

                return S1DevUtilities.NetworkSingleton<S1EnvironmentManager>.Instance;
            }
            catch
            {
                return null;
            }
        }

        private static void Unbind()
        {
            S1EnvironmentHandler.UnsubscribeFromWeatherChange(WeatherChangeHandler);
            _boundInstance = null;
            _isBound = false;
        }

        private static void OnNativeWeatherChanged(S1WeatherConditions? conditions)
        {
            try
            {
                PublishState(conditions);
            }
            catch (Exception ex)
            {
                Logger.Warning($"Failed to process the native weather callback: {ex.Message}");
            }
        }

        private static void PublishCurrentState(S1EnvironmentManager manager)
        {
#if IL2CPPMELON
            PublishState(manager._currentWeatherConditions);
#else
            PublishState(CurrentWeatherConditionsField?.GetValue(manager) as S1WeatherConditions);
#endif
        }

        private static void PublishState(S1WeatherConditions? conditions)
        {
            if (conditions == null)
                return;

            global::S1API.Weather.WeatherManager.NotifyWeatherChanged(
                global::S1API.Weather.WeatherState.FromNativeComponents(
                    conditions.Sunny,
                    conditions.Cloudy,
                    conditions.Rainy,
                    conditions.Stormy,
                    conditions.Snowy,
                    conditions.Foggy,
                    conditions.Windy,
                    conditions.Hail,
                    conditions.Sleet));
        }
    }
}

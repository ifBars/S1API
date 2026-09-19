using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using S1API.Internal.Weather;
using S1API.Logging;

namespace S1API.Weather
{
    /// <summary>
    /// Provides read-only access to the game's current weather and known weather sequences.
    /// </summary>
    /// <remarks>
    /// Weather state is populated after the current Main-scene weather manager and its active
    /// conditions are available. It is <see langword="null"/> outside gameplay or while those
    /// native services are still initializing.
    /// This API does not select weather sequences or trigger weather effects.
    /// </remarks>
    public static class WeatherManager
    {
        private static readonly Log Logger = new Log("WeatherManager");
        private static readonly IReadOnlyList<string> EmptySequenceIds =
            new ReadOnlyCollection<string>(Array.Empty<string>());
        private static WeatherState? _current;

        /// <summary>
        /// Gets the most recently observed weather state, or <see langword="null"/> while the
        /// current scene does not have an initialized weather manager.
        /// </summary>
        public static WeatherState? Current => _current;

        /// <summary>
        /// Raised when weather first becomes available or changes to a distinct snapshot.
        /// </summary>
        public static event Action<WeatherState>? OnWeatherChanged;

        /// <summary>
        /// Gets a fresh, read-only snapshot of the identifiers configured on the current native
        /// weather manager. Identifier order, casing, and duplicates are preserved.
        /// </summary>
        /// <remarks>
        /// An empty list is returned when the weather manager is not available or its sequence
        /// collection cannot be read. The returned list contains strings only and is independent
        /// of the native collection.
        /// </remarks>
        public static IReadOnlyList<string> KnownSequenceIds =>
            WeatherRuntime.GetKnownSequenceIds();

        /// <summary>
        /// INTERNAL: Publishes a managed snapshot after it has been copied from the native
        /// weather callback.
        /// </summary>
        internal static void NotifyWeatherChanged(WeatherState state)
        {
            if (_current.HasValue && _current.Value.Equals(state))
                return;

            _current = state;
            Action<WeatherState>? handlers = OnWeatherChanged;
            if (handlers == null)
                return;

            foreach (Delegate callback in handlers.GetInvocationList())
            {
                try
                {
                    ((Action<WeatherState>)callback)(state);
                }
                catch (Exception ex)
                {
                    TryLogSubscriberFailure(ex);
                }
            }
        }

        private static void TryLogSubscriberFailure(Exception exception)
        {
            try
            {
                Logger.Warning(
                    $"A WeatherManager.OnWeatherChanged subscriber failed: {exception.Message}");
            }
            catch
            {
                // Logging must not break event dispatch when MelonLoader is not initialized.
            }
        }

        /// <summary>
        /// INTERNAL: Clears the scene-bound weather snapshot without raising a change event.
        /// </summary>
        internal static void ResetState()
        {
            _current = null;
        }

        /// <summary>
        /// INTERNAL: Copies sequence identifiers into an immutable managed snapshot.
        /// </summary>
        internal static IReadOnlyList<string> SnapshotSequenceIds(
            IEnumerable<string?> sequenceIds)
        {
            if (sequenceIds == null)
                return EmptySequenceIds;

            var snapshot = new List<string>();
            foreach (string? id in sequenceIds)
            {
                if (!string.IsNullOrEmpty(id))
                    snapshot.Add(id);
            }

            return snapshot.Count == 0
                ? EmptySequenceIds
                : new ReadOnlyCollection<string>(snapshot);
        }
    }
}

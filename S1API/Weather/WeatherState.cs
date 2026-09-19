using System;

namespace S1API.Weather
{
    /// <summary>
    /// An immutable snapshot of the game's active weather-condition weights.
    /// Each value is normally normalized between 0 and 1 by the game.
    /// </summary>
    public readonly struct WeatherState : IEquatable<WeatherState>
    {
        /// <summary>
        /// Creates a weather snapshot from the nine native weather components.
        /// </summary>
        /// <param name="sunny">The sunny-condition weight.</param>
        /// <param name="cloudy">The cloudy-condition weight.</param>
        /// <param name="rainy">The rainy-condition weight.</param>
        /// <param name="stormy">The stormy-condition weight.</param>
        /// <param name="snowy">The snowy-condition weight.</param>
        /// <param name="foggy">The foggy-condition weight.</param>
        /// <param name="windy">The windy-condition weight.</param>
        /// <param name="hail">The hail-condition weight.</param>
        /// <param name="sleet">The sleet-condition weight.</param>
        public WeatherState(
            float sunny,
            float cloudy,
            float rainy,
            float stormy,
            float snowy,
            float foggy,
            float windy,
            float hail,
            float sleet)
        {
            Sunny = sunny;
            Cloudy = cloudy;
            Rainy = rainy;
            Stormy = stormy;
            Snowy = snowy;
            Foggy = foggy;
            Windy = windy;
            Hail = hail;
            Sleet = sleet;
        }

        /// <summary>
        /// Gets the sunny-condition weight.
        /// </summary>
        public float Sunny { get; }

        /// <summary>
        /// Gets the cloudy-condition weight.
        /// </summary>
        public float Cloudy { get; }

        /// <summary>
        /// Gets the rainy-condition weight.
        /// </summary>
        public float Rainy { get; }

        /// <summary>
        /// Gets the stormy-condition weight.
        /// </summary>
        public float Stormy { get; }

        /// <summary>
        /// Gets the snowy-condition weight.
        /// </summary>
        public float Snowy { get; }

        /// <summary>
        /// Gets the foggy-condition weight.
        /// </summary>
        public float Foggy { get; }

        /// <summary>
        /// Gets the windy-condition weight.
        /// </summary>
        public float Windy { get; }

        /// <summary>
        /// Gets the hail-condition weight.
        /// </summary>
        public float Hail { get; }

        /// <summary>
        /// Gets the sleet-condition weight.
        /// </summary>
        public float Sleet { get; }

        /// <summary>
        /// INTERNAL: Creates a managed snapshot from the native weather component order.
        /// </summary>
        internal static WeatherState FromNativeComponents(
            float sunny,
            float cloudy,
            float rainy,
            float stormy,
            float snowy,
            float foggy,
            float windy,
            float hail,
            float sleet)
        {
            return new WeatherState(
                sunny,
                cloudy,
                rainy,
                stormy,
                snowy,
                foggy,
                windy,
                hail,
                sleet);
        }

        /// <inheritdoc />
        public bool Equals(WeatherState other)
        {
            return Sunny.Equals(other.Sunny)
                && Cloudy.Equals(other.Cloudy)
                && Rainy.Equals(other.Rainy)
                && Stormy.Equals(other.Stormy)
                && Snowy.Equals(other.Snowy)
                && Foggy.Equals(other.Foggy)
                && Windy.Equals(other.Windy)
                && Hail.Equals(other.Hail)
                && Sleet.Equals(other.Sleet);
        }

        /// <inheritdoc />
        public override bool Equals(object? obj) =>
            obj is WeatherState other && Equals(other);

        /// <inheritdoc />
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = Sunny.GetHashCode();
                hash = (hash * 397) ^ Cloudy.GetHashCode();
                hash = (hash * 397) ^ Rainy.GetHashCode();
                hash = (hash * 397) ^ Stormy.GetHashCode();
                hash = (hash * 397) ^ Snowy.GetHashCode();
                hash = (hash * 397) ^ Foggy.GetHashCode();
                hash = (hash * 397) ^ Windy.GetHashCode();
                hash = (hash * 397) ^ Hail.GetHashCode();
                return (hash * 397) ^ Sleet.GetHashCode();
            }
        }

        /// <summary>
        /// Determines whether two weather snapshots have the same component values.
        /// </summary>
        /// <param name="left">The first weather snapshot.</param>
        /// <param name="right">The second weather snapshot.</param>
        /// <returns><see langword="true"/> when all condition weights are equal.</returns>
        public static bool operator ==(WeatherState left, WeatherState right) =>
            left.Equals(right);

        /// <summary>
        /// Determines whether two weather snapshots have different component values.
        /// </summary>
        /// <param name="left">The first weather snapshot.</param>
        /// <param name="right">The second weather snapshot.</param>
        /// <returns><see langword="true"/> when any condition weight differs.</returns>
        public static bool operator !=(WeatherState left, WeatherState right) =>
            !left.Equals(right);
    }
}

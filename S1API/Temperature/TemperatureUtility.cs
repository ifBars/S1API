#if IL2CPPMELON
using S1Temperature = Il2CppScheduleOne.Temperature;
#elif MONOMELON
using S1Temperature = ScheduleOne.Temperature;
#endif

namespace S1API.Temperature
{
    /// <summary>
    /// Formats and converts temperatures with the game's native temperature helpers.
    /// </summary>
    public static class TemperatureUtility
    {
        /// <summary>
        /// Converts a Celsius temperature to Fahrenheit.
        /// </summary>
        /// <param name="celsius">The temperature in degrees Celsius.</param>
        /// <returns>The temperature in degrees Fahrenheit.</returns>
        public static float ToFahrenheit(float celsius) =>
            S1Temperature.TemperatureUtility.ToFahrenheit(celsius);

        /// <summary>
        /// Formats a Celsius temperature with the game's Celsius unit display.
        /// </summary>
        /// <param name="celsius">The temperature in degrees Celsius.</param>
        /// <param name="decimalPoints">The number of decimal places to display.</param>
        /// <returns>The formatted Celsius temperature.</returns>
        public static string FormatCelsiusTemperature(float celsius, int decimalPoints) =>
            S1Temperature.TemperatureUtility.FormatCelsiusTemperature(celsius, decimalPoints);

        /// <summary>
        /// Formats a Fahrenheit temperature with the game's Fahrenheit unit display.
        /// </summary>
        /// <param name="fahrenheit">The temperature in degrees Fahrenheit.</param>
        /// <param name="decimalPoints">The number of decimal places to display.</param>
        /// <returns>The formatted Fahrenheit temperature.</returns>
        public static string FormatFahrenheitTemperature(float fahrenheit, int decimalPoints) =>
            S1Temperature.TemperatureUtility.FormatFahrenheitTemperature(fahrenheit, decimalPoints);

        /// <summary>
        /// Formats a Celsius temperature with the unit selected by the game.
        /// </summary>
        /// <param name="celsius">The temperature in degrees Celsius.</param>
        /// <param name="decimalPoints">The number of decimal places to display.</param>
        /// <returns>The formatted temperature.</returns>
        public static string FormatTemperatureWithAppropriateUnit(
            float celsius,
            int decimalPoints = 1) =>
            S1Temperature.TemperatureUtility.FormatTemperatureWithAppropriateUnit(celsius, decimalPoints);

        /// <summary>
        /// Normalizes a Celsius temperature with the game's native temperature range.
        /// </summary>
        /// <param name="celsius">The temperature in degrees Celsius.</param>
        /// <returns>The normalized temperature.</returns>
        public static float NormalizeTemperature(float celsius) =>
            S1Temperature.TemperatureUtility.NormalizeTemperature(celsius);
    }
}

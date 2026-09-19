using System;
using System.Collections.Generic;
using S1API.Weather;

namespace S1API.Tests.Weather;

internal static class WeatherApiCompileFixture
{
    internal static void SubscribeAndRead()
    {
        Action<WeatherState> handler = state =>
        {
            _ = state.Rainy;
        };
        WeatherManager.OnWeatherChanged += handler;

        WeatherState? current = WeatherManager.Current;
        IReadOnlyList<string> sequenceIds = WeatherManager.KnownSequenceIds;
        _ = current;
        _ = sequenceIds;

        WeatherManager.OnWeatherChanged -= handler;
    }
}

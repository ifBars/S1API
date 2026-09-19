# Weather

The `S1API.Weather` module exposes the active weather-condition weights and the weather-change
callback without exposing Schedule One or Unity types.

```csharp
using S1API.Weather;

WeatherManager.OnWeatherChanged += state =>
{
    if (state.Rainy > 0.5f || state.Stormy > 0.5f)
    {
        // React to a rainy or stormy world.
    }
};

WeatherState? current = WeatherManager.Current;
foreach (string sequenceId in WeatherManager.KnownSequenceIds)
{
    // Sequence identifiers retain the game's order and casing.
}
```

`WeatherManager.Current` is nullable outside gameplay and while the current Main scene's weather
manager is still initializing. S1API reads the active conditions as soon as they are available.
`OnWeatherChanged` receives an immutable `WeatherState` when weather first becomes available and
for each distinct later snapshot. It contains the nine native weights: `Sunny`, `Cloudy`, `Rainy`,
`Stormy`, `Snowy`, `Foggy`, `Windy`, `Hail`, and `Sleet`. Repeated identical snapshots are suppressed.

`KnownSequenceIds` is a fresh, read-only managed snapshot. It preserves the configured sequence
order, exact identifier casing, and duplicate entries, and returns an empty list when the native
weather manager is unavailable. Native weather objects and collections are never returned.

The first version is read-only. Selecting sequences, triggering thunder or lightning, custom weather
profiles, persistence, and custom network payloads are intentionally outside this API's scope.

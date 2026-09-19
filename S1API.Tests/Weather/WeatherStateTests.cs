using System;
using System.Collections.Generic;
using S1API.Weather;

namespace S1API.Tests.Weather;

public sealed class WeatherStateTests
{
    [Fact]
    public void ConstructorCopiesAllNativeWeatherComponents()
    {
        var state = new WeatherState(
            sunny: 0.1f,
            cloudy: 0.2f,
            rainy: 0.3f,
            stormy: 0.4f,
            snowy: 0.5f,
            foggy: 0.6f,
            windy: 0.7f,
            hail: 0.8f,
            sleet: 0.9f);

        Assert.Equal(0.1f, state.Sunny);
        Assert.Equal(0.2f, state.Cloudy);
        Assert.Equal(0.3f, state.Rainy);
        Assert.Equal(0.4f, state.Stormy);
        Assert.Equal(0.5f, state.Snowy);
        Assert.Equal(0.6f, state.Foggy);
        Assert.Equal(0.7f, state.Windy);
        Assert.Equal(0.8f, state.Hail);
        Assert.Equal(0.9f, state.Sleet);
    }

    [Fact]
    public void NativeComponentMappingPreservesTheWeatherFieldOrder()
    {
        WeatherState state = WeatherState.FromNativeComponents(
            0.11f,
            0.22f,
            0.33f,
            0.44f,
            0.55f,
            0.66f,
            0.77f,
            0.88f,
            0.99f);

        Assert.Equal(0.11f, state.Sunny);
        Assert.Equal(0.22f, state.Cloudy);
        Assert.Equal(0.33f, state.Rainy);
        Assert.Equal(0.44f, state.Stormy);
        Assert.Equal(0.55f, state.Snowy);
        Assert.Equal(0.66f, state.Foggy);
        Assert.Equal(0.77f, state.Windy);
        Assert.Equal(0.88f, state.Hail);
        Assert.Equal(0.99f, state.Sleet);
    }

    [Fact]
    public void SequenceSnapshotsCopyOrderCasingAndDuplicates()
    {
        var source = new List<string?> { "Sunny", "RAIN", null, string.Empty, "Sunny" };

        IReadOnlyList<string> snapshot = WeatherManager.SnapshotSequenceIds(source);
        source.Add("Changed after snapshot");

        Assert.Equal(new[] { "Sunny", "RAIN", "Sunny" }, snapshot);
        Assert.IsAssignableFrom<IReadOnlyList<string>>(snapshot);
        Assert.Throws<NotSupportedException>(
            () => ((IList<string>)snapshot)[0] = "mutated");
    }

    [Fact]
    public void EqualityIncludesEveryWeatherComponent()
    {
        WeatherState baseline = CreateState();

        Assert.Equal(baseline, CreateState());
        Assert.Equal(baseline.GetHashCode(), CreateState().GetHashCode());
        Assert.NotEqual(baseline, new WeatherState(
            baseline.Sunny,
            baseline.Cloudy,
            baseline.Rainy,
            baseline.Stormy,
            baseline.Snowy,
            baseline.Foggy,
            baseline.Windy,
            baseline.Hail,
            baseline.Sleet + 0.01f));
        Assert.True(baseline == CreateState());
        Assert.True(baseline != new WeatherState(
            baseline.Sunny,
            baseline.Cloudy,
            baseline.Rainy,
            baseline.Stormy,
            baseline.Snowy,
            baseline.Foggy,
            baseline.Windy,
            baseline.Hail,
            baseline.Sleet + 0.01f));
    }

    [Fact]
    public void ManagerSuppressesDuplicateSnapshotsAndKeepsCurrentInSync()
    {
        WeatherManager.ResetState();
        WeatherState first = CreateState();
        WeatherState second = new WeatherState(
            first.Sunny,
            first.Cloudy,
            first.Rainy + 0.1f,
            first.Stormy,
            first.Snowy,
            first.Foggy,
            first.Windy,
            first.Hail,
            first.Sleet);
        int notificationCount = 0;
        Action<WeatherState> handler = _ => notificationCount++;
        WeatherManager.OnWeatherChanged += handler;

        try
        {
            WeatherManager.NotifyWeatherChanged(first);
            WeatherManager.NotifyWeatherChanged(first);
            WeatherManager.NotifyWeatherChanged(second);

            Assert.Equal(2, notificationCount);
            Assert.True(WeatherManager.Current.HasValue);
            Assert.Equal(second, WeatherManager.Current.Value);
        }
        finally
        {
            WeatherManager.OnWeatherChanged -= handler;
            WeatherManager.ResetState();
        }
    }

    [Fact]
    public void ManagerContinuesAfterASubscriberThrows()
    {
        WeatherManager.ResetState();
        WeatherState state = CreateState();
        int notificationCount = 0;
        Action<WeatherState> throwingHandler = _ => throw new InvalidOperationException("test");
        Action<WeatherState> observingHandler = _ => notificationCount++;
        WeatherManager.OnWeatherChanged += throwingHandler;
        WeatherManager.OnWeatherChanged += observingHandler;

        try
        {
            WeatherManager.NotifyWeatherChanged(state);

            Assert.Equal(1, notificationCount);
            Assert.True(WeatherManager.Current.HasValue);
            Assert.Equal(state, WeatherManager.Current.Value);
        }
        finally
        {
            WeatherManager.OnWeatherChanged -= throwingHandler;
            WeatherManager.OnWeatherChanged -= observingHandler;
            WeatherManager.ResetState();
        }
    }

    private static WeatherState CreateState()
    {
        return new WeatherState(
            sunny: 0.1f,
            cloudy: 0.2f,
            rainy: 0.3f,
            stormy: 0.4f,
            snowy: 0.5f,
            foggy: 0.6f,
            windy: 0.7f,
            hail: 0.8f,
            sleet: 0.9f);
    }
}

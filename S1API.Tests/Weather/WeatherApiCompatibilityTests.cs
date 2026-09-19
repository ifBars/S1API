using System;
using System.Collections.Generic;
using System.Reflection;
using S1API.Weather;

namespace S1API.Tests.Weather;

public sealed class WeatherApiCompatibilityTests
{
    [Fact]
    public void WeatherStateExposesAnImmutableManagedShape()
    {
        Assert.True(typeof(WeatherState).IsValueType);
        Assert.True(typeof(WeatherState).IsPublic);
        Assert.NotNull(typeof(WeatherState).GetConstructor(new[]
        {
            typeof(float), typeof(float), typeof(float),
            typeof(float), typeof(float), typeof(float),
            typeof(float), typeof(float), typeof(float)
        }));

        string[] propertyNames =
        {
            nameof(WeatherState.Sunny),
            nameof(WeatherState.Cloudy),
            nameof(WeatherState.Rainy),
            nameof(WeatherState.Stormy),
            nameof(WeatherState.Snowy),
            nameof(WeatherState.Foggy),
            nameof(WeatherState.Windy),
            nameof(WeatherState.Hail),
            nameof(WeatherState.Sleet)
        };

        foreach (string propertyName in propertyNames)
        {
            PropertyInfo? property = typeof(WeatherState).GetProperty(propertyName);
            Assert.NotNull(property);
            Assert.Equal(typeof(float), property!.PropertyType);
            Assert.False(property.CanWrite);
        }
    }

    [Fact]
    public void WeatherManagerSurfaceIsAdditiveAndRuntimeNeutral()
    {
        PropertyInfo? current = typeof(WeatherManager).GetProperty(nameof(WeatherManager.Current));
        PropertyInfo? sequenceIds = typeof(WeatherManager).GetProperty(nameof(WeatherManager.KnownSequenceIds));
        EventInfo? changed = typeof(WeatherManager).GetEvent(nameof(WeatherManager.OnWeatherChanged));

        Assert.NotNull(current);
        Assert.Equal(typeof(WeatherState?), current!.PropertyType);
        Assert.NotNull(sequenceIds);
        Assert.Equal(typeof(IReadOnlyList<string>), sequenceIds!.PropertyType);
        Assert.NotNull(changed);
        Assert.Equal(typeof(Action<WeatherState>), changed!.EventHandlerType);

        Assert.DoesNotContain(
            typeof(WeatherManager).GetProperties(BindingFlags.Public | BindingFlags.Static),
            property => property.PropertyType.Namespace?.StartsWith("ScheduleOne", StringComparison.Ordinal) == true);
    }
}

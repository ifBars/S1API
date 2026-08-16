using System;
using S1API.Temperature;
using UnityEngine;

namespace S1API.Tests.Temperature;

internal static class TemperatureApiCompileFixture
{
    internal static void ConfigureAndQuery(GameObject gameObject)
    {
        TemperatureEmitter emitter = TemperatureEmitter.GetOrAddComponent(gameObject);
        emitter.SetTemperature(20f);
        emitter.SetRange(5f);
        emitter.SetPosition(Vector3.zero);

        Action changed = () => { };
        emitter.OnChanged += changed;
        emitter.OnChanged -= changed;

        TemperatureEmitter? existing = TemperatureEmitter.FromGameObject(gameObject);
        TemperatureEmitterInfo[] emitters = { emitter.ToInfo() };
        float temperature = TemperatureAlgorithm.GetTemperatureAtPoint(
            ambientTemperature: 20f,
            originPoint: Vector3.zero,
            point: Vector3.one,
            emitters: emitters);

        _ = existing;
        _ = temperature;
        _ = TemperatureUtility.TemperatureSystemEnabled;
        _ = TemperatureUtility.ToFahrenheit(20f);
        _ = TemperatureUtility.FormatCelsiusTemperature(20f, decimalPoints: 1);
        _ = TemperatureUtility.FormatFahrenheitTemperature(68f, decimalPoints: 1);
        _ = TemperatureUtility.FormatTemperatureWithAppropriateUnit(20f);
        _ = TemperatureUtility.NormalizeTemperature(20f);
    }
}

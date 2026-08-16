#if IL2CPPMELON
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using S1Temperature = Il2CppScheduleOne.Temperature;
#elif MONOMELON
using S1Temperature = ScheduleOne.Temperature;
#endif

using System;
using S1API.Internal.Temperature;
using UnityEngine;

namespace S1API.Temperature
{
    /// <summary>
    /// Queries temperatures with the game's native temperature algorithm.
    /// </summary>
    public static class TemperatureAlgorithm
    {
        /// <summary>
        /// Calculates the temperature at a world position from an ambient temperature and emitter snapshots.
        /// </summary>
        /// <param name="ambientTemperature">The ambient temperature in degrees Celsius.</param>
        /// <param name="originPoint">
        /// The world origin forwarded to the native API for signature compatibility. The current native implementation
        /// evaluates world-space emitter and query positions directly and does not otherwise use this value.
        /// </param>
        /// <param name="point">The world position to query.</param>
        /// <param name="emitters">The emitter snapshots to include in the calculation.</param>
        /// <returns>The temperature at <paramref name="point"/> in degrees Celsius.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="emitters"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException">A scalar or vector input is not finite.</exception>
        /// <remarks>
        /// This method does not discover scene emitters or register the supplied snapshots with a grid.
        /// </remarks>
        public static float GetTemperatureAtPoint(
            float ambientTemperature,
            Vector3 originPoint,
            Vector3 point,
            TemperatureEmitterInfo[] emitters)
        {
            if (emitters == null)
                throw new ArgumentNullException(nameof(emitters));

            TemperatureValidation.EnsureFinite(ambientTemperature, nameof(ambientTemperature));
            TemperatureValidation.EnsureFinite(originPoint, nameof(originPoint));
            TemperatureValidation.EnsureFinite(point, nameof(point));
#if IL2CPPMELON
            var nativeEmitters = new Il2CppStructArray<S1Temperature.TemperatureEmitterInfo>(emitters.Length);
#else
            var nativeEmitters = new S1Temperature.TemperatureEmitterInfo[emitters.Length];
#endif
            for (int i = 0; i < emitters.Length; i++)
            {
                TemperatureEmitterInfo emitter = emitters[i];
                float temperature = TemperatureValidation.ClampTemperature(
                    emitter.Temperature,
                    $"{nameof(emitters)}[{i}].{nameof(TemperatureEmitterInfo.Temperature)}");
                float range = TemperatureValidation.ClampRange(
                    emitter.Range,
                    $"{nameof(emitters)}[{i}].{nameof(TemperatureEmitterInfo.Range)}");
                TemperatureValidation.EnsureFinite(
                    emitter.Position,
                    $"{nameof(emitters)}[{i}].{nameof(TemperatureEmitterInfo.Position)}");
                nativeEmitters[i] = new S1Temperature.TemperatureEmitterInfo(
                    temperature,
                    range * range,
                    emitter.Position);
            }

            return S1Temperature.TemperatureAlgorithm.GetTemperatureAtPoint(
                ambientTemperature,
                originPoint,
                point,
                nativeEmitters);
        }
    }
}

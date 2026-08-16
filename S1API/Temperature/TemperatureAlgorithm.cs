#if IL2CPPMELON
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using S1Temperature = Il2CppScheduleOne.Temperature;
#elif MONOMELON
using S1Temperature = ScheduleOne.Temperature;
#endif

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
        /// <param name="ambientTemperature">The ambient temperature in the game's native temperature scale.</param>
        /// <param name="originPoint">The origin used by the game's temperature calculation.</param>
        /// <param name="point">The world position to query.</param>
        /// <param name="emitters">The emitter snapshots to include in the calculation.</param>
        /// <returns>The temperature at <paramref name="point"/> in the game's native temperature scale.</returns>
        /// <remarks>
        /// This method does not discover scene emitters or register the supplied snapshots with a grid.
        /// </remarks>
        public static float GetTemperatureAtPoint(
            float ambientTemperature,
            Vector3 originPoint,
            Vector3 point,
            TemperatureEmitterInfo[] emitters)
        {
#if IL2CPPMELON
            var nativeEmitters = new Il2CppStructArray<S1Temperature.TemperatureEmitterInfo>(emitters.Length);
#else
            var nativeEmitters = new S1Temperature.TemperatureEmitterInfo[emitters.Length];
#endif
            for (int i = 0; i < emitters.Length; i++)
            {
                TemperatureEmitterInfo emitter = emitters[i];
                nativeEmitters[i] = new S1Temperature.TemperatureEmitterInfo(
                    emitter.Temperature,
                    emitter.SqrRange,
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

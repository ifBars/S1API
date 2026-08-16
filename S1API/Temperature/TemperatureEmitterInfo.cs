using UnityEngine;

namespace S1API.Temperature
{
    /// <summary>
    /// Describes one temperature emitter for a point-temperature query.
    /// </summary>
    public readonly struct TemperatureEmitterInfo
    {
        /// <summary>
        /// Creates a temperature-emitter snapshot.
        /// </summary>
        /// <param name="temperature">The emitter temperature in the game's native temperature scale.</param>
        /// <param name="sqrRange">The emitter range squared, in world units squared.</param>
        /// <param name="position">The emitter position in world space.</param>
        public TemperatureEmitterInfo(float temperature, float sqrRange, Vector3 position)
        {
            Temperature = temperature;
            SqrRange = sqrRange;
            Position = position;
        }

        /// <summary>
        /// Gets the emitter temperature in the game's native temperature scale.
        /// </summary>
        public float Temperature { get; }

        /// <summary>
        /// Gets the emitter range squared, in world units squared.
        /// </summary>
        public float SqrRange { get; }

        /// <summary>
        /// Gets the emitter position in world space.
        /// </summary>
        public Vector3 Position { get; }
    }
}

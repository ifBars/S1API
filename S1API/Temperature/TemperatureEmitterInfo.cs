using System;
using S1API.Internal.Temperature;
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
        /// <param name="temperature">
        /// The emitter temperature in degrees Celsius. Finite values are clamped to the game's supported range.
        /// </param>
        /// <param name="range">The emitter range in world units. Finite values are clamped to the game's supported range.</param>
        /// <param name="position">The emitter position in world space. Every component must be finite.</param>
        /// <exception cref="ArgumentOutOfRangeException">An argument is not finite.</exception>
        public TemperatureEmitterInfo(float temperature, float range, Vector3 position)
        {
            Temperature = TemperatureValidation.ClampTemperature(temperature, nameof(temperature));
            Range = TemperatureValidation.ClampRange(range, nameof(range));
            TemperatureValidation.EnsureFinite(position, nameof(position));
            Position = position;
        }

        /// <summary>
        /// Gets the emitter temperature in degrees Celsius.
        /// </summary>
        public float Temperature { get; }

        /// <summary>
        /// Gets the emitter range in world units.
        /// </summary>
        public float Range { get; }

        /// <summary>
        /// Gets the emitter position in world space.
        /// </summary>
        public Vector3 Position { get; }
    }
}

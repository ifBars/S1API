#if IL2CPPMELON
using S1Noise = Il2CppScheduleOne.Noise;
#elif MONOMELON
using S1Noise = ScheduleOne.Noise;
#endif

using UnityEngine;

namespace S1API.Entities
{
    /// <summary>
    /// Describes the type of noise an NPC heard.
    /// </summary>
    public enum NPCNoiseType
    {
        /// <summary>
        /// A footstep sound.
        /// </summary>
        Footstep = 0,

        /// <summary>
        /// A gunshot sound.
        /// </summary>
        Gunshot = 1,

        /// <summary>
        /// An explosion sound.
        /// </summary>
        Explosion = 2
    }

    /// <summary>
    /// An immutable snapshot of a noise event heard by an NPC.
    /// </summary>
    public sealed class NPCNoiseEvent
    {
        /// <summary>
        /// The world-space origin of the noise.
        /// </summary>
        public Vector3 Origin { get; }

        /// <summary>
        /// The range of the noise.
        /// </summary>
        public float Range { get; }

        /// <summary>
        /// The type of noise.
        /// </summary>
        public NPCNoiseType Type { get; }

        /// <summary>
        /// The GameObject that emitted the noise, if the native event identified one.
        /// </summary>
        public GameObject? Source { get; }

        /// <summary>
        /// Whether the noise originated in the sewer.
        /// </summary>
        public bool OriginInSewer { get; }

        internal NPCNoiseEvent(S1Noise.NoiseEvent noiseEvent)
            : this(
                noiseEvent.origin,
                noiseEvent.range,
                (NPCNoiseType)(int)noiseEvent.type,
                noiseEvent.source,
                noiseEvent.OriginInSewer)
        {
        }

        internal NPCNoiseEvent(
            Vector3 origin,
            float range,
            NPCNoiseType type,
            GameObject? source,
            bool originInSewer)
        {
            Origin = origin;
            Range = range;
            Type = type;
            Source = source;
            OriginInSewer = originInSewer;
        }
    }
}

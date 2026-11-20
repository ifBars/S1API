#if (IL2CPPMELON)
using S1Combat = Il2CppScheduleOne.Combat;
#elif (MONOMELON)
using S1Combat = ScheduleOne.Combat;
#endif

using UnityEngine;

namespace S1API.Entities
{
    /// <summary>
    /// The kind of hit an NPC received.
    /// </summary>
    public enum NpcImpactType
    {
        /// <summary>A punch.</summary>
        Punch = 0,
        /// <summary>A blunt melee weapon.</summary>
        BluntMetal = 1,
        /// <summary>A sharp melee weapon.</summary>
        SharpMetal = 2,
        /// <summary>A bullet.</summary>
        Bullet = 3,
        /// <summary>A thrown or moving physics object.</summary>
        PhysicsProp = 4,
        /// <summary>An explosion, including lightning.</summary>
        Explosion = 5
    }

    /// <summary>
    /// A hit an NPC received, as passed to <see cref="NPC.OnImpact"/>.
    /// </summary>
    public sealed class NpcImpact
    {
        internal NpcImpact(S1Combat.Impact native, Player? attacker)
        {
            ImpactId = native.ImpactID;
            HitPoint = native.HitPoint;
            ForceDirection = native.ImpactForceDirection;
            Force = native.ImpactForce;
            Damage = native.ImpactDamage;
            Type = (NpcImpactType)(int)native.ImpactType;
            IsLightning = native.ImpactType == S1Combat.EImpactType.Explosion
                          && native.ExplosionType == S1Combat.EExplosionType.Lightning;
            Attacker = attacker;
        }

        /// <summary>The id the game uses to ignore a hit it already processed.</summary>
        public int ImpactId { get; }

        /// <summary>World position of the hit.</summary>
        public Vector3 HitPoint { get; }

        /// <summary>Direction the hit pushes the NPC.</summary>
        public Vector3 ForceDirection { get; }

        /// <summary>Strength of the push, before the NPC's stance scales it.</summary>
        public float Force { get; }

        /// <summary>Damage the hit deals.</summary>
        public float Damage { get; }

        /// <summary>The kind of hit.</summary>
        public NpcImpactType Type { get; }

        /// <summary>Whether the hit is a lightning strike.</summary>
        public bool IsLightning { get; }

        /// <summary>The player who caused the hit, or null when it did not come from a player or S1API has no wrapper for it.</summary>
        public Player? Attacker { get; }
    }
}

#if (IL2CPPMELON)
using S1ItemFramework = Il2CppScheduleOne.ItemFramework;
#elif (MONOMELON || MONOBEPINEX || IL2CPPBEPINEX)
using S1ItemFramework = ScheduleOne.ItemFramework;
#endif

using UnityEngine;

namespace S1API.Items
{
    /// <summary>
    /// Represents an additive item definition.
    /// Extends <see cref="StorableItemDefinition"/> with additive-specific properties.
    /// </summary>
    /// <remarks>
    /// Builder-only: these properties are intentionally read-only to avoid runtime surprises from mutating
    /// globally-registered ScriptableObject definitions mid-session. Use <see cref="AdditiveItemCreator"/> to create
    /// additives with configured effects.
    /// </remarks>
    public sealed class AdditiveDefinition : StorableItemDefinition
    {
        /// <summary>
        /// INTERNAL: Wraps an existing native additive definition.
        /// </summary>
        internal AdditiveDefinition(S1ItemFramework.AdditiveDefinition definition)
            : base(definition)
        {
            S1AdditiveDefinition = definition;
        }

        /// <summary>
        /// INTERNAL: A reference to the native game additive definition.
        /// </summary>
        internal S1ItemFramework.AdditiveDefinition S1AdditiveDefinition { get; }

        /// <summary>
        /// Display material used for the additive (if applicable).
        /// </summary>
        public Material DisplayMaterial => S1AdditiveDefinition.DisplayMaterial;

        /// <summary>
        /// Quality modifier applied by this additive.
        /// </summary>
        public float QualityChange => S1AdditiveDefinition.QualityChange;

        /// <summary>
        /// Yield multiplier applied by this additive.
        /// </summary>
        public float YieldMultiplier => S1AdditiveDefinition.YieldMultiplier;

        /// <summary>
        /// Instant growth fraction applied by this additive (0..1).
        /// </summary>
        public float InstantGrowth => S1AdditiveDefinition.InstantGrowth;
    }
}


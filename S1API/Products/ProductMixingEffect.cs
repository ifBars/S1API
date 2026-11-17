using System;

namespace S1API.Products
{
    /// <summary>
    /// Describes one effect on a generated mix output, as resolved by the native mixing calculation.
    /// </summary>
    /// <remarks>
    /// This is a runtime-independent snapshot. It does not reference the native effect, so it is
    /// safe to inspect from a deterministic mixing-output factory on Mono and IL2CPP alike.
    /// </remarks>
    public sealed class ProductMixingEffect
    {
        /// <summary>
        /// INTERNAL: Creates an effect snapshot.
        /// </summary>
        /// <param name="id">The native effect ID.</param>
        /// <param name="name">The effect display name.</param>
        /// <param name="tier">The native effect tier.</param>
        /// <param name="valueChange">The flat value the effect adds to a product's base value.</param>
        /// <param name="valueMultiplier">The multiplier the effect contributes to a product's value.</param>
        internal ProductMixingEffect(string id, string? name, int tier, float valueChange, float valueMultiplier)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Name = name ?? string.Empty;
            Tier = tier;
            ValueChange = valueChange;
            ValueMultiplier = valueMultiplier;
        }

        /// <summary>Gets the native effect ID.</summary>
        public string Id { get; }
        /// <summary>Gets the effect display name.</summary>
        public string Name { get; }
        /// <summary>Gets the native effect tier.</summary>
        public int Tier { get; }
        /// <summary>Gets the flat value the effect adds to a product's base value.</summary>
        public float ValueChange { get; }
        /// <summary>Gets the multiplier the effect contributes to a product's value.</summary>
        public float ValueMultiplier { get; }
    }
}

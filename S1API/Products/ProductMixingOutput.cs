using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace S1API.Products
{
    /// <summary>
    /// Supplies deterministic, runtime-independent input to a custom mixing-output factory.
    /// </summary>
    public sealed class ProductMixingOutput
    {
        private static readonly IReadOnlyList<ProductMixingEffect> NoProperties =
            new ReadOnlyCollection<ProductMixingEffect>(new ProductMixingEffect[0]);

        private readonly Func<float, float>? _nativeValueCalculator;

        internal ProductMixingOutput(string outputId, string mixName, string sourceProductId, ProductKind sourceKind, float sourcePrice)
            : this(outputId, mixName, sourceProductId, sourceKind, sourcePrice, NoProperties, null)
        {
        }

        internal ProductMixingOutput(
            string outputId,
            string mixName,
            string sourceProductId,
            ProductKind sourceKind,
            float sourcePrice,
            IReadOnlyList<ProductMixingEffect> properties,
            Func<float, float>? nativeValueCalculator)
        {
            if (properties == null)
                throw new ArgumentNullException(nameof(properties));

            OutputId = outputId;
            MixName = mixName;
            SourceProductId = sourceProductId;
            SourceKind = sourceKind;
            SourcePrice = sourcePrice;
            Properties = properties.Count == 0
                ? NoProperties
                : new ReadOnlyCollection<ProductMixingEffect>(new List<ProductMixingEffect>(properties));
            _nativeValueCalculator = nativeValueCalculator;
        }

        /// <summary>Gets the stable ID allocated by the native mixing flow.</summary>
        public string OutputId { get; }
        /// <summary>Gets the player-selected display name for the output.</summary>
        public string MixName { get; }
        /// <summary>Gets the source custom-product ID.</summary>
        public string SourceProductId { get; }
        /// <summary>Gets the source logical product kind.</summary>
        public ProductKind SourceKind { get; }
        /// <summary>Gets the source product's current base price.</summary>
        /// <remarks>
        /// For a source that is itself a generated mix, this is the price its output factory
        /// returned, so adding to it compounds across repeated mixes.
        /// </remarks>
        public float SourcePrice { get; }

        /// <summary>
        /// Gets the effects the generated output will carry, in native order, after the native
        /// mixing calculation and any registered mixing reactions.
        /// </summary>
        /// <remarks>Empty when the input was not produced by the native mixing flow.</remarks>
        public IReadOnlyList<ProductMixingEffect> Properties { get; }

        /// <summary>
        /// Calculates a value for the output's <see cref="Properties"/> using the native
        /// product-value calculation, the same one that prices native mixes.
        /// </summary>
        /// <remarks>
        /// Native mixes are priced from their product family's fixed base price, not from the
        /// source product's price. Pass your kind's fixed base price to match that behavior.
        /// The result is not clamped; <see cref="ProductMixingOutputDefinition"/> accepts prices
        /// from 1 through 999.
        /// </remarks>
        /// <param name="baseValue">The finite base value the effects are applied to.</param>
        /// <returns>The native value of a product with these effects and base value.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="baseValue"/> is NaN or infinite.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the input was not produced by the native mixing flow.</exception>
        public float CalculateNativeValue(float baseValue)
        {
            if (float.IsNaN(baseValue) || float.IsInfinity(baseValue))
                throw new ArgumentOutOfRangeException(nameof(baseValue), "Base value must be finite.");
            if (_nativeValueCalculator == null)
                throw new InvalidOperationException("Native value calculation is only available during the native mixing flow.");

            return _nativeValueCalculator(baseValue);
        }
    }
}

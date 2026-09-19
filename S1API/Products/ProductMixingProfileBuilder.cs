using System;
using S1API.Internal.Products;

namespace S1API.Products
{
    /// <summary>
    /// Builds and registers an opt-in mixing profile for one logical product kind.
    /// </summary>
    public sealed class ProductMixingProfileBuilder
    {
        private readonly ProductKind _productKind;
        private ProductMixingMap _mixerMap;
        private Func<ProductMixingOutput, ProductMixingOutputDefinition>? _outputFactory;
        private string? _outputFactoryIdentity;
        private int _outputFactoryVersion;
        private bool _usePropertyColorMixing;

        /// <summary>Creates a mixing-profile builder for a registered logical product kind.</summary>
        /// <param name="productKind">The stable logical kind that opts into mixing.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="productKind"/> is <see langword="null"/>.</exception>
        public ProductMixingProfileBuilder(ProductKind productKind)
        {
            _productKind = productKind ?? throw new ArgumentNullException(nameof(productKind));
        }

        /// <summary>
        /// Sets the native mixer-map execution strategy.
        /// </summary>
        /// <remarks>
        /// This does not alter the logical product kind or require it to have a matching vanilla
        /// drug type. The selected map is only the explicit native seam used while mixing.
        /// </remarks>
        public ProductMixingProfileBuilder WithMixerMap(ProductMixingMap mixerMap)
        {
            if (!Enum.IsDefined(typeof(ProductMixingMap), mixerMap))
                throw new ArgumentOutOfRangeException(nameof(mixerMap));
            _mixerMap = mixerMap;
            return this;
        }

        /// <summary>Sets the deterministic factory that names, prices, and optionally transforms generated outputs.</summary>
        /// <param name="outputFactory">A deterministic factory invoked for each native mixing output.</param>
        /// <returns>This builder.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="outputFactory"/> is <see langword="null"/>.</exception>
        public ProductMixingProfileBuilder WithOutputFactory(Func<ProductMixingOutput, ProductMixingOutputDefinition> outputFactory)
        {
            _outputFactory = outputFactory ?? throw new ArgumentNullException(nameof(outputFactory));
            return this;
        }

        /// <summary>
        /// Sets the stable compatibility identity for the configured output factory.
        /// </summary>
        /// <remarks>
        /// Register the same identity and version on every peer. This identity is included in the
        /// multiplayer manifest so different output behavior is rejected before a mix can diverge.
        /// </remarks>
        public ProductMixingProfileBuilder WithOutputFactoryCompatibility(
            string identity,
            int version)
        {
            if (string.IsNullOrWhiteSpace(identity))
                throw new ArgumentException("Output-factory compatibility identity cannot be empty or whitespace.", nameof(identity));
            if (version < 0)
                throw new ArgumentOutOfRangeException(nameof(version));

            _outputFactoryIdentity = ProductKindId.Normalize(identity, nameof(identity));
            _outputFactoryVersion = version;
            return this;
        }

        /// <summary>
        /// Colors each generated output from its mixed properties using the selected native
        /// mixer map's primary-color strategy.
        /// </summary>
        /// <remarks>
        /// This is opt-in. It affects only generated mixes and does not recolor the base custom
        /// product. The resulting color is persisted with the generated product so save reloads
        /// and peers render the same appearance.
        /// </remarks>
        public ProductMixingProfileBuilder WithPropertyColorMixing()
        {
            _usePropertyColorMixing = true;
            return this;
        }

        /// <summary>Builds and registers this immutable mixing profile.</summary>
        /// <returns>The registered profile, or the existing equivalent profile.</returns>
        /// <exception cref="InvalidOperationException">Thrown when no output factory was configured.</exception>
        public ProductMixingProfile Build()
        {
            if (_outputFactory == null)
                throw new InvalidOperationException("WithOutputFactory must be called before Build().");
            string compatibilityIdentity = _outputFactoryIdentity ??
                "s1api:factory/" + CustomProductManifestData.ComputeHash(
                    (_outputFactory.Method.DeclaringType?.AssemblyQualifiedName ?? string.Empty) +
                    "|" + _outputFactory.Method.Name);
            return ProductMixingProfileRegistry.Register(new ProductMixingProfile(
                _productKind,
                _mixerMap,
                _outputFactory,
                compatibilityIdentity,
                _outputFactoryVersion,
                _usePropertyColorMixing));
        }
    }
}

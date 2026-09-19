using System;

namespace S1API.Products
{
    /// <summary>
    /// Opts a logical product kind into native mixing through an explicit native map strategy.
    /// </summary>
    public sealed class ProductMixingProfile
    {
        internal ProductMixingProfile(ProductKind productKind, ProductMixingMap mixerMap, Func<ProductMixingOutput, ProductMixingOutputDefinition> outputFactory, string outputFactoryIdentity, int outputFactoryVersion, bool usePropertyColorMixing)
        {
            ProductKind = productKind;
            MixerMap = mixerMap;
            OutputFactory = outputFactory;
            OutputFactoryIdentity = outputFactoryIdentity;
            OutputFactoryVersion = outputFactoryVersion;
            UsePropertyColorMixing = usePropertyColorMixing;
        }

        /// <summary>Gets the opted-in logical product kind.</summary>
        public ProductKind ProductKind { get; }
        /// <summary>Gets the native mixer-map execution strategy selected for this logical kind.</summary>
        public ProductMixingMap MixerMap { get; }
        /// <summary>Gets the stable identity used to compare output-factory behavior across peers.</summary>
        public string OutputFactoryIdentity { get; }
        /// <summary>Gets the output-factory compatibility version used across peers.</summary>
        public int OutputFactoryVersion { get; }
        /// <summary>Gets whether generated outputs use a native-style color derived from their properties.</summary>
        public bool UsePropertyColorMixing { get; }
        internal Func<ProductMixingOutput, ProductMixingOutputDefinition> OutputFactory { get; }
    }
}

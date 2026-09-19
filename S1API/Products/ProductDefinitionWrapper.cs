using System;
using S1API.Internal.Products;
using S1API.Internal.Utils;
#if (IL2CPPMELON)
using S1Product = Il2CppScheduleOne.Product;
#elif MONOMELON
using S1Product = ScheduleOne.Product;
#endif

namespace S1API.Products
{
    /// <summary>
    /// Selects the most specific API wrapper for a registered native product definition.
    /// </summary>
    /// <remarks>
    /// The wrapper exposes native-family types when available and returns
    /// <see cref="CustomProductDefinition"/> only for definitions registered through S1API's
    /// custom-product metadata. It never changes the native definition or its identity.
    /// </remarks>
    public static class ProductDefinitionWrapper
    {
        /// <summary>
        /// Returns the most specific wrapper available for a product definition.
        /// </summary>
        /// <param name="def">The product definition to classify.</param>
        /// <returns>A typed wrapper, or <paramref name="def"/> when no more specific wrapper applies.</returns>
        public static ProductDefinition Wrap(ProductDefinition def)
        {
            return Wrap(def.S1ProductDefinition, def);
        }

        /// <summary>
        /// INTERNAL: Creates the most specific API wrapper for a native product definition.
        /// </summary>
        /// <param name="definition">The native product definition to wrap.</param>
        /// <returns>The most specific available product definition wrapper.</returns>
        internal static ProductDefinition Wrap(S1Product.ProductDefinition definition)
        {
            return Wrap(definition, null);
        }

        private static ProductDefinition Wrap(
            S1Product.ProductDefinition definition,
            ProductDefinition? fallback)
        {
            if (CrossType.Is<S1Product.WeedDefinition>(definition, out var weed))
                return new WeedDefinition(weed);

            if (CrossType.Is<S1Product.MethDefinition>(definition, out var meth))
                return new MethDefinition(meth);

            if (CrossType.Is<S1Product.CocaineDefinition>(definition, out var coke))
                return new CocaineDefinition(coke);

            if (CrossType.Is<S1Product.ShroomDefinition>(definition, out var shroom))
                return new ShroomDefinition(shroom);

            if (CustomProductDefinitionRegistry.TryGetMetadata(
                    definition,
                    out CustomProductDefinitionMetadata? metadata))
            {
                return new CustomProductDefinition(definition, metadata!);
            }

            return fallback ?? new ProductDefinition(definition);
        }
    }
}

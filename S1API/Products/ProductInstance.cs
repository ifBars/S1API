#if (IL2CPPMELON )
using S1Product = Il2CppScheduleOne.Product;
using S1Properties = Il2CppScheduleOne.Effects;
#elif MONOMELON
using S1Product = ScheduleOne.Product;
using S1Properties = ScheduleOne.Effects;
#endif
using System.Collections.Generic;
using S1API.Internal.Utils;
using S1API.Properties.Interfaces;
using S1ItemInstance = S1API.Items.ItemInstance;
namespace S1API.Products
{
    /// <summary>
    /// Represents one product stack or item in the active game runtime.
    /// </summary>
    /// <remarks>
    /// Product instances inherit quantity and base item behavior from <see cref="S1ItemInstance"/>.
    /// This wrapper adds product definition, quality, packaging, and property access without
    /// exposing runtime-specific native types.
    /// </remarks>
    public class ProductInstance : S1ItemInstance
    {
        /// <summary>
        /// INTERNAL: Provides access to the underlying in-game product item instance.
        /// </summary>
        internal S1Product.ProductItemInstance S1ProductInstance =>
            CrossType.As<S1Product.ProductItemInstance>(S1ItemInstance);

        /// <summary>
        /// Creates a wrapper around a native product item instance.
        /// </summary>
        internal ProductInstance(S1Product.ProductItemInstance productInstance)
            : base(productInstance)
        {
        }

        /// <summary>
        /// Gets whether the instance currently has native packaging.
        /// </summary>
        public bool IsPackaged => S1ProductInstance.AppliedPackaging;

        /// <summary>
        /// Gets the packaging applied to this instance.
        /// </summary>
        /// <remarks>
        /// Check <see cref="IsPackaged"/> before reading this property. The native runtime only
        /// provides a packaging definition for packaged instances.
        /// </remarks>
        public PackagingDefinition AppliedPackaging =>
            new PackagingDefinition(S1ProductInstance.AppliedPackaging);

        /// <summary>
        /// Gets the runtime-agnostic quality level assigned to this instance.
        /// </summary>
        /// <remarks>
        /// This value comes from the native instance. Creating an instance through
        /// <see cref="ProductDefinition.CreateInstance(int)"/> uses the native standard quality.
        /// </remarks>
        public Quality Quality => S1ProductInstance.Quality.ToAPI();

        /// <summary>
        /// Gets the definition of the product associated with this instance.
        /// </summary>
        public new ProductDefinition Definition =>
            ProductDefinitionWrapper.Wrap(
                CrossType.As<S1Product.ProductDefinition>(S1ProductInstance.Definition));

        /// <summary>
        /// Gets runtime-agnostic wrappers for the properties on <see cref="Definition"/>.
        /// </summary>
        /// <remarks>
        /// Properties belong to the definition, not the individual stack. The returned list is a
        /// read-only snapshot from <see cref="ProductDefinition.Properties"/>.
        /// </remarks>
        public IReadOnlyList<PropertyBase> Properties => Definition.Properties;
    }
}

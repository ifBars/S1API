#if (IL2CPPMELON)
using Il2CppInterop.Runtime.InteropTypes;
using S1Product = Il2CppScheduleOne.Product;
using ItemFramework = Il2CppScheduleOne.ItemFramework;
using S1Properties = Il2CppScheduleOne.Effects;
#elif MONOMELON
using S1Product = ScheduleOne.Product;
using ItemFramework = ScheduleOne.ItemFramework;
using S1Properties = ScheduleOne.Effects;
#endif

using System.Collections.Generic;
using S1API.Internal.Utils;
using S1API.Items;
using S1API.Properties;
using S1API.Properties.Interfaces;
using S1API.Products.Packaging;
using UnityEngine;

namespace S1API.Products
{
    /// <summary>
    /// Represents one registered product type in the active game runtime.
    /// </summary>
    /// <remarks>
    /// A definition supplies the shared identity, price, properties, icon, and packaging policy
    /// for its instances. It is a wrapper over a native definition, not a new product-registration
    /// mechanism. Use the custom-product or native-family builders when a mod needs to register
    /// a definition.
    /// </remarks>
    public class ProductDefinition : Items.Storable.StorableItemDefinition
    {
        /// <summary>
        /// INTERNAL: Stored reference to the game product definition.
        /// </summary>
        internal S1Product.ProductDefinition S1ProductDefinition =>
            CrossType.As<S1Product.ProductDefinition>(S1ItemDefinition);

        /// <summary>
        /// INTERNAL: Creates a product definition from the in-game product definition.
        /// </summary>
        /// <param name="productDefinition"></param>
        internal ProductDefinition(S1Product.ProductDefinition productDefinition)
            : base(productDefinition)
        {
        }
        /// <summary>
        /// Gets the current price selected by the native product system.
        /// </summary>
        public float Price =>
            S1ProductDefinition.Price;

        /// <summary>
        /// Gets the native base price before market adjustments.
        /// </summary>
        public float BasePrice =>
            S1ProductDefinition.BasePrice;
        
        /// <summary>
        /// Gets the product's native market value.
        /// </summary>
        public float MarketValue =>
            S1ProductDefinition.MarketValue;

        /// <summary>
        /// Creates an unpackaged standard-quality instance of this definition.
        /// </summary>
        /// <param name="quantity">The native product quantity for the new instance.</param>
        /// <returns>A new API wrapper around the native product instance.</returns>
        public override ItemInstance CreateInstance(int quantity = 1) =>
            new ProductInstance(CrossType.As<S1Product.ProductItemInstance>(S1ProductDefinition.GetDefaultInstance(quantity)));

        /// <summary>
        /// Gets the current native inventory icon.
        /// </summary>
        /// <remarks>
        /// The returned sprite is owned by the active Unity runtime. Do not destroy it. A
        /// generated custom-product icon can replace this reference after its capture completes.
        /// </remarks>
        public new Sprite Icon
        {
            get { return S1ProductDefinition.Icon; }
        }

        /// <summary>
        /// Gets runtime-agnostic wrappers for the definition's product properties.
        /// </summary>
        /// <remarks>
        /// Each call creates a read-only snapshot that works on both Mono and IL2CPP. Do not use
        /// the wrapper objects as stable identity keys; use their IDs when identity matters.
        /// </remarks>
        public IReadOnlyList<PropertyBase> Properties
        {
            get
            {
                var s1Properties = S1ProductDefinition.Properties;
                var wrappers = new List<PropertyBase>(s1Properties.Count);
                
                for (int i = 0; i < s1Properties.Count; i++)
                {
                    wrappers.Add(new ProductPropertyWrapper(s1Properties[i]));
                }
                
                return wrappers.AsReadOnly();
            }
        }

        /// <summary>
        /// Gets all drug types associated with this product definition without exposing runtime-specific game types.
        /// </summary>
        public IReadOnlyList<DrugType> DrugTypeValues
        {
            get
            {
                var source = S1ProductDefinition.DrugTypes;
                var converted = new List<DrugType>(source != null ? source.Count : 0);

                if (source != null)
                {
                    for (int i = 0; i < source.Count; i++)
                    {
                        var container = source[i];
                        if (container != null)
                            converted.Add(container.DrugType.ToAPI());
                    }
                }

                return converted.AsReadOnly();
            }
        }

        /// <summary>
        /// Gets the primary drug type for this product without exposing runtime-specific game types.
        /// </summary>
        public DrugType PrimaryDrugType =>
            S1ProductDefinition.DrugType.ToAPI();

        /// <summary>
        /// The list of native drug type containers associated with this product definition.
        /// Returns a C# list for IL2CPP builds to avoid collection type mismatches.
        /// </summary>
        /// <remarks>
        /// This compatibility member exposes native game types whose concrete definitions differ between
        /// Mono and IL2CPP. It is not cross-runtime compatible and is retained only for existing consumers.
        /// Prefer <see cref="DrugTypeValues"/> in cross-runtime mods.
        /// </remarks>
#if (IL2CPPMELON)
        [System.Obsolete("Use DrugTypeValues instead.", false)]
        public System.Collections.Generic.IReadOnlyList<S1Product.DrugTypeContainer> DrugTypes
        {
            get
            {
                var source = S1ProductDefinition.DrugTypes; // Il2CppSystem.Collections.Generic.List<DrugTypeContainer>
                var converted = new System.Collections.Generic.List<S1Product.DrugTypeContainer>(source != null ? source.Count : 0);
                if (source != null)
                {
                    for (int i = 0; i < source.Count; i++)
                        converted.Add(source[i]);
                }
                return converted.AsReadOnly();
            }
        }
#else
        [System.Obsolete("Use DrugTypeValues instead.", false)]
        public System.Collections.Generic.List<S1Product.DrugTypeContainer> DrugTypes =>
            S1ProductDefinition.DrugTypes;
#endif

        /// <summary>
        /// The native primary drug type for this product.
        /// </summary>
        /// <remarks>
        /// This compatibility member exposes a native game enum whose concrete type differs between
        /// Mono and IL2CPP. It is not cross-runtime compatible and is retained only for existing consumers.
        /// Prefer <see cref="PrimaryDrugType"/> in cross-runtime mods.
        /// </remarks>
        [System.Obsolete("Use PrimaryDrugType instead.", false)]
        public S1Product.EDrugType DrugType =>
            S1ProductDefinition.DrugType;

        /// <summary>
        /// Creates a standard-quality instance with the supplied native packaging.
        /// </summary>
        /// <param name="quantity">The native product quantity for the new instance.</param>
        /// <param name="packaging">A live packaging definition from the active game runtime.</param>
        /// <returns>
        /// A packaged product instance, or <see langword="null"/> when the packaging cannot be
        /// converted for the active runtime or native construction fails.
        /// </returns>
        public ProductInstance? CreatePackagedInstance(int quantity, PackagingDefinition packaging)
        {
            try
            {
#if (IL2CPPMELON)
                var s1Packaging = CrossType.As<Il2CppScheduleOne.Product.Packaging.PackagingDefinition>(packaging.S1ItemDefinition);
#elif MONOMELON
                var s1Packaging = CrossType.As<ScheduleOne.Product.Packaging.PackagingDefinition>(packaging.S1ItemDefinition);
#endif

                if (s1Packaging == null)
                {
                    return null;
                }

                var s1ProductInstance = new S1Product.ProductItemInstance(
                    S1ProductDefinition,
                    quantity,
                    ItemFramework.EQuality.Standard,
                    s1Packaging
                );

                return new ProductInstance(s1ProductInstance);
            }
            catch (System.Exception)
            {
                return null;
            }
        }

    }
}

#if (IL2CPPMELON)
using Il2CppInterop.Runtime.InteropTypes;
using S1Product = Il2CppScheduleOne.Product;
using ItemFramework = Il2CppScheduleOne.ItemFramework;
using S1Properties = Il2CppScheduleOne.Properties;
#elif (MONOMELON || MONOBEPINEX || IL2CPPBEPINEX)
using S1Product = ScheduleOne.Product;
using ItemFramework = ScheduleOne.ItemFramework;
using S1Properties = ScheduleOne.Properties;
#endif

using System.Collections.Generic;
using S1API.Internal.Utils;
using S1API.Items;
using S1API.Properties;
using S1API.Properties.Interfaces;
using UnityEngine;

namespace S1API.Products
{
    /// <summary>
    /// Represents a product definition in the game.
    /// </summary>
    public class ProductDefinition : ItemDefinition
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
#if  IL2CPPMELON
        internal ProductDefinition(ItemFramework.ItemDefinition productDefinition) : base(productDefinition) { }
#else
        internal ProductDefinition(ItemFramework.ItemDefinition productDefinition) : base(productDefinition) { }
#endif
        /// <summary>
        /// The price associated with this product.
        /// </summary>
        public float Price =>
            S1ProductDefinition.Price;

        /// <summary>
        /// The base price associated with this product.
        /// </summary>
        public float BasePrice =>
            S1ProductDefinition.BasePrice;
        
        /// <summary>
        /// The market value associated with this product.
        /// </summary>
        public float MarketValue =>
            S1ProductDefinition.MarketValue;

        /// <summary>
        /// Creates an instance of this product in-game.
        /// </summary>
        /// <param name="quantity">The quantity of product.</param>
        /// <returns>An instance of the product.</returns>
        public override ItemInstance CreateInstance(int quantity = 1) =>
            new ProductInstance(CrossType.As<S1Product.ProductItemInstance>(S1ProductDefinition.GetDefaultInstance(quantity)));

        /// <summary>
        /// Gets the in-game icon associated with the product.
        /// </summary>
        public Sprite Icon
        {
            get { return S1ProductDefinition.Icon; }
        }

        /// <summary>
        /// The list of product properties for this definition.
        /// Returns runtime-agnostic property wrappers that work on both Mono and IL2CPP.
        /// </summary>
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

}
}

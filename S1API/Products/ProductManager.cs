#if (IL2CPPMELON)
using S1Product = Il2CppScheduleOne.Product;
#elif (MONOMELON || MONOBEPINEX || IL2CPPBEPINEX)
using S1Product = ScheduleOne.Product;
#endif
using System.Linq;

namespace S1API.Products
{
    /// <summary>
    /// Provides management over all products in the game.
    /// </summary>
    public static class ProductManager
    {
        /// <summary>
        /// Minimum price for any product (1).
        /// </summary>
        public const int MinPrice = 1;

        /// <summary>
        /// Maximum price for any product (999).
        /// </summary>
        public const int MaxPrice = 999;

        /// <summary>
        /// A list of product definitions discovered on this save.
        /// </summary>
        public static ProductDefinition[] DiscoveredProducts => S1Product.ProductManager.DiscoveredProducts.ToArray()
            .Select(productDefinition => ProductDefinitionWrapper.Wrap(new ProductDefinition(productDefinition)))
            .ToArray();

        /// <summary>
        /// A list of products currently listed for sale.
        /// </summary>
        public static ProductDefinition[] ListedProducts => S1Product.ProductManager.ListedProducts.ToArray()
            .Select(productDefinition => ProductDefinitionWrapper.Wrap(new ProductDefinition(productDefinition)))
            .ToArray();

        /// <summary>
        /// A list of favourited products.
        /// </summary>
        public static ProductDefinition[] FavouritedProducts => S1Product.ProductManager.FavouritedProducts.ToArray()
            .Select(productDefinition => ProductDefinitionWrapper.Wrap(new ProductDefinition(productDefinition)))
            .ToArray();

        /// <summary>
        /// Gets whether the player is currently accepting orders.
        /// </summary>
        public static bool IsAcceptingOrders => S1Product.ProductManager.IsAcceptingOrders;

        /// <summary>
        /// Gets whether meth has been discovered.
        /// </summary>
        public static bool MethDiscovered => S1Product.ProductManager.MethDiscovered;

        /// <summary>
        /// Gets whether cocaine has been discovered.
        /// </summary>
        public static bool CocaineDiscovered => S1Product.ProductManager.CocaineDiscovered;

        /// <summary>
        /// Gets whether shrooms have been discovered.
        /// </summary>
        public static bool ShroomsDiscovered => S1Product.ProductManager.ShroomsDiscovered;

        /// <summary>
        /// Gets the current price of a product.
        /// </summary>
        /// <param name="product">The product definition.</param>
        /// <returns>The price of the product.</returns>
        public static float GetPrice(ProductDefinition product) => S1Product.ProductManager.Instance.GetPrice(product.S1ProductDefinition);

        /// <summary>
        /// Calculates the value of a product based on its properties.
        /// </summary>
        /// <param name="product">The product definition.</param>
        /// <param name="baseValue">The base value to calculate from.</param>
        /// <returns>The calculated value.</returns>
        public static float CalculateProductValue(ProductDefinition product, float baseValue) =>
            S1Product.ProductManager.CalculateProductValue(product.S1ProductDefinition, baseValue);
    }
}

#if (IL2CPPMELON)
using S1UIShop = Il2CppScheduleOne.UI.Shop;
#elif (MONOMELON || MONOBEPINEX || IL2CPPBEPINEX)
using S1UIShop = ScheduleOne.UI.Shop;
#endif

using System;
using System.Collections.Generic;
using System.Linq;
using S1API.Items;
using S1API.Logging;

namespace S1API.Shops
{
    /// <summary>
    /// Provides access to all shops in the game and convenience methods for shop integration.
    /// </summary>
    public static class ShopManager
    {
        private static readonly Log Logger = new Log("ShopManager");
        private static readonly List<Shop> _cachedShops = new List<Shop>();
        private static bool _cacheValid = false;

        /// <summary>
        /// Gets all shops currently loaded in the game.
        /// Results are cached until the scene changes.
        /// </summary>
        /// <returns>Array of all available shops.</returns>
        public static Shop[] GetAllShops()
        {
            RefreshCacheIfNeeded();
            return _cachedShops.ToArray();
        }

        /// <summary>
        /// Gets a shop by its display name (case-insensitive).
        /// </summary>
        /// <param name="shopName">The name of the shop to find.</param>
        /// <returns>The shop if found, null otherwise.</returns>
        /// <example>
        /// <code>
        /// var hardwareStore = ShopManager.GetShopByName("Hardware Store");
        /// if (hardwareStore != null)
        /// {
        ///     hardwareStore.AddItem(myCustomItem);
        /// }
        /// </code>
        /// </example>
        public static Shop GetShopByName(string shopName)
        {
            if (string.IsNullOrEmpty(shopName))
                return null;

            return GetAllShops()
                .FirstOrDefault(s => string.Equals(s.Name, shopName, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Finds all shops that sell items in the specified category.
        /// </summary>
        /// <param name="category">The item category to search for.</param>
        /// <returns>Array of shops that sell items in this category.</returns>
        /// <example>
        /// <code>
        /// // Find all shops that sell tools
        /// var toolShops = ShopManager.FindShopsByCategory(ItemCategory.Tools);
        /// </code>
        /// </example>
        public static Shop[] FindShopsByCategory(ItemCategory category)
        {
            return GetAllShops()
                .Where(s => s.SellsCategory(category))
                .ToArray();
        }

        /// <summary>
        /// Finds all shops that sell a specific item.
        /// </summary>
        /// <param name="itemId">The ID of the item to search for.</param>
        /// <returns>Array of shops that sell this item.</returns>
        public static Shop[] FindShopsByItem(string itemId)
        {
            if (string.IsNullOrEmpty(itemId))
                return Array.Empty<Shop>();

            return GetAllShops()
                .Where(s => s.HasItem(itemId))
                .ToArray();
        }

        /// <summary>
        /// Adds an item to all shops that sell items in the same category.
        /// This is a convenience method for making custom items available wherever similar items are sold.
        /// </summary>
        /// <param name="item">The item to add to compatible shops.</param>
        /// <param name="customPrice">Optional custom price override.</param>
        /// <returns>The number of shops the item was added to.</returns>
        /// <example>
        /// <code>
        /// var metalRack = ItemManager.GetItemDefinition("metalstoragerack");
        /// int shopsUpdated = ShopManager.AddToCompatibleShops(metalRack);
        /// Logger.Msg($"Added metal rack to {shopsUpdated} shops");
        /// </code>
        /// </example>
        public static int AddToCompatibleShops(ItemDefinition item, float? customPrice = null)
        {
            if (item == null)
                return 0;

            var compatibleShops = FindShopsByCategory(item.Category);
            int addedCount = 0;

            foreach (var shop in compatibleShops)
            {
                if (shop.AddItem(item, customPrice))
                    addedCount++;
            }

            if (addedCount > 0)
            {
                Logger.Msg($"Added item '{item.Name}' to {addedCount} compatible shop(s)");
            }

            return addedCount;
        }

        /// <summary>
        /// Adds an item to specific shops by name.
        /// </summary>
        /// <param name="item">The item to add.</param>
        /// <param name="shopNames">Names of shops to add the item to.</param>
        /// <returns>The number of shops the item was added to.</returns>
        /// <example>
        /// <code>
        /// ShopManager.AddToShops(myItem, "Hardware Store", "General Store");
        /// </code>
        /// </example>
        public static int AddToShops(ItemDefinition item, params string[] shopNames)
        {
            return AddToShops(item, null, shopNames);
        }

        /// <summary>
        /// Adds an item to specific shops by name with a custom price.
        /// </summary>
        /// <param name="item">The item to add.</param>
        /// <param name="customPrice">Optional custom price override.</param>
        /// <param name="shopNames">Names of shops to add the item to.</param>
        /// <returns>The number of shops the item was added to.</returns>
        public static int AddToShops(ItemDefinition item, float? customPrice, params string[] shopNames)
        {
            if (item == null || shopNames == null || shopNames.Length == 0)
                return 0;

            int addedCount = 0;

            foreach (var shopName in shopNames)
            {
                var shop = GetShopByName(shopName);
                if (shop != null && shop.AddItem(item, customPrice))
                    addedCount++;
            }

            return addedCount;
        }

        /// <summary>
        /// INTERNAL: Invalidates the shop cache.
        /// Called by S1API when scenes change.
        /// </summary>
        internal static void InvalidateCache()
        {
            _cacheValid = false;
            _cachedShops.Clear();
        }

        private static void RefreshCacheIfNeeded()
        {
            if (_cacheValid)
                return;

            _cachedShops.Clear();

            try
            {
                var allShops = S1UIShop.ShopInterface.AllShops;
                if (allShops == null)
                {
                    _cacheValid = true;
                    return;
                }

                foreach (var nativeShop in allShops)
                {
                    if (nativeShop != null)
                        _cachedShops.Add(new Shop(nativeShop));
                }

                _cacheValid = true;
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to refresh shop cache: {ex.Message}");
                _cacheValid = false;
            }
        }
    }
}

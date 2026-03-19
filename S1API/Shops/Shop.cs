#if (IL2CPPMELON)
using S1UIShop = Il2CppScheduleOne.UI.Shop;
using S1ItemFramework = Il2CppScheduleOne.ItemFramework;
using S1CoreItemFramework = Il2CppScheduleOne.Core.Items.Framework;
#elif (MONOMELON || MONOBEPINEX || IL2CPPBEPINEX)
using S1UIShop = ScheduleOne.UI.Shop;
using S1ItemFramework = ScheduleOne.ItemFramework;
using S1CoreItemFramework = ScheduleOne.Core.Items.Framework;
#endif

using System.Collections.Generic;
using System.Linq;
using S1API.Items;

namespace S1API.Shops
{
    /// <summary>
    /// Represents an in-game shop where items can be purchased.
    /// Provides high-level operations without exposing game types.
    /// </summary>
    public sealed class Shop
    {
        /// <summary>
        /// INTERNAL: Reference to the native game shop interface.
        /// </summary>
        internal S1UIShop.ShopInterface S1ShopInterface { get; }

        /// <summary>
        /// INTERNAL: Creates a wrapper around an existing shop interface.
        /// </summary>
        internal Shop(S1UIShop.ShopInterface shopInterface)
        {
            S1ShopInterface = shopInterface;
        }

        /// <summary>
        /// The display name of this shop.
        /// </summary>
        public string Name =>
            S1ShopInterface.ShopName;

        /// <summary>
        /// Checks if this shop currently sells an item with the specified ID.
        /// </summary>
        /// <param name="itemId">The unique ID of the item to check.</param>
        /// <returns>True if the shop sells this item, false otherwise.</returns>
        public bool HasItem(string itemId)
        {
            if (string.IsNullOrEmpty(itemId))
                return false;

            foreach (var listing in S1ShopInterface.Listings)
            {
                if (listing?.Item?.ID == itemId)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Checks if this shop sells items in the specified category.
        /// </summary>
        /// <param name="category">The item category to check.</param>
        /// <returns>True if the shop sells at least one item in this category.</returns>
        public bool SellsCategory(ItemCategory category)
        {
            var s1Category = (S1CoreItemFramework.EItemCategory)category;

            foreach (var listing in S1ShopInterface.Listings)
            {
                if (listing?.Item != null && listing.Item.Category == s1Category)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Gets all item IDs currently sold by this shop.
        /// </summary>
        /// <returns>Array of item IDs.</returns>
        public string[] GetItemIds()
        {
            var ids = new List<string>();
            foreach (var listing in S1ShopInterface.Listings)
            {
                if (listing?.Item != null)
                    ids.Add(listing.Item.ID);
            }
            return ids.ToArray();
        }

        /// <summary>
        /// Adds an item to this shop's inventory with automatic UI creation and event hookup.
        /// The item will appear in the shop's listing and be purchasable by the player.
        /// </summary>
        /// <param name="item">The item definition to add to the shop.</param>
        /// <param name="customPrice">Optional custom price override. If null, uses item's BasePurchasePrice.</param>
        /// <returns>True if the item was added successfully, false if it already exists or addition failed.</returns>
        /// <example>
        /// <code>
        /// var shop = ShopManager.GetShopByName("Hardware Store");
        /// var customItem = ItemManager.GetItemDefinition("my_custom_tool");
        /// shop.AddItem(customItem);
        /// </code>
        /// </example>
        public bool AddItem(ItemDefinition item, float? customPrice = null)
        {
            if (item == null)
                return false;

            // Check if item already exists
            if (HasItem(item.ID))
                return false;

            return Internal.Shops.ShopIntegration.AddItemToShop(this, item, customPrice);
        }

        /// <summary>
        /// Removes an item from this shop's inventory.
        /// </summary>
        /// <param name="itemId">The ID of the item to remove.</param>
        /// <returns>True if the item was removed, false if it wasn't found.</returns>
        public bool RemoveItem(string itemId)
        {
            if (string.IsNullOrEmpty(itemId))
                return false;

            return Internal.Shops.ShopIntegration.RemoveItemFromShop(this, itemId);
        }
    }
}

#if (IL2CPPMELON)
using S1UIShop = Il2CppScheduleOne.UI.Shop;
using Il2CppInterop.Runtime;
#elif (MONOMELON || MONOBEPINEX || IL2CPPBEPINEX)
using S1UIShop = ScheduleOne.UI.Shop;
using System;
#endif

using System.Reflection;
using UnityEngine;
using S1API.Items;
using S1API.Logging;
using S1API.Shops;

namespace S1API.Internal.Shops
{
    /// <summary>
    /// INTERNAL: Handles low-level shop integration including UI creation and event hookup.
    /// This class abstracts away runtime-specific differences and reflection-based method invocation.
    /// </summary>
    internal static class ShopIntegration
    {
        private static readonly Log Logger = new Log("ShopIntegration");

        /// <summary>
        /// Adds an item to a shop with full UI creation and event binding.
        /// </summary>
        internal static bool AddItemToShop(Shop shop, ItemDefinition item, float? customPrice)
        {
            if (shop?.S1ShopInterface == null || item?.S1ItemDefinition == null)
                return false;

            var storable = item as StorableItemDefinition;
            if (storable == null)
            {
                Logger.Warning($"Item '{item.ID}' is not storable and cannot be sold in shops.");
                return false;
            }

            try
            {
                var listing = CreateListing(storable, customPrice);
                shop.S1ShopInterface.Listings.Add(listing);
                listing.Initialize(shop.S1ShopInterface);
                CreateListingUI(shop.S1ShopInterface, listing);

                return true;
            }
            catch (System.Exception ex)
            {
                Logger.Error($"Failed to add item '{item.ID}' to shop '{shop.Name}': {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Removes an item from a shop.
        /// </summary>
        internal static bool RemoveItemFromShop(Shop shop, string itemId)
        {
            if (shop?.S1ShopInterface == null || string.IsNullOrEmpty(itemId))
                return false;

            try
            {
                S1UIShop.ShopListing? listingToRemove = null;

                // Find the listing
                foreach (var listing in shop.S1ShopInterface.Listings)
                {
                    if (listing?.Item?.ID == itemId)
                    {
                        listingToRemove = listing;
                        break;
                    }
                }

                if (listingToRemove == null)
                    return false;

                // Remove from listings
                shop.S1ShopInterface.Listings.Remove(listingToRemove);

                // Clean up UI if it exists
                RemoveListingUI(shop.S1ShopInterface, listingToRemove);

                return true;
            }
            catch (System.Exception ex)
            {
                Logger.Error($"Failed to remove item '{itemId}' from shop '{shop.Name}': {ex.Message}");
                return false;
            }
        }

        private static S1UIShop.ShopListing CreateListing(StorableItemDefinition item, float? customPrice)
        {
            var listing = new S1UIShop.ShopListing
            {
                Item = item.S1StorableItemDefinition,
                name = item.Name
            };

            // Note: Custom pricing would require modifying the item definition
            if (customPrice.HasValue && customPrice.Value != item.BasePurchasePrice)
            {
                Logger.Warning($"Custom price override requested but not fully supported. Using item's BasePurchasePrice.");
            }

            return listing;
        }

        private static void CreateListingUI(S1UIShop.ShopInterface shop, S1UIShop.ShopListing listing)
        {
            if (shop.ListingUIPrefab == null || shop.ListingContainer == null)
            {
                Logger.Warning($"Shop '{shop.ShopName}' missing UI prefab or container");
                return;
            }

            // Instantiate the UI prefab
            var uiObject = UnityEngine.Object.Instantiate(
                shop.ListingUIPrefab.gameObject,
                shop.ListingContainer
            );

            var listingUI = uiObject.GetComponent<S1UIShop.ListingUI>();
            if (listingUI == null)
            {
                Logger.Error("Failed to get ListingUI component from instantiated prefab");
                UnityEngine.Object.Destroy(uiObject);
                return;
            }

            // Initialize the UI with the listing
            listingUI.Initialize(listing);

            // Bind events - runtime-specific implementation
            BindListingUIEvents(shop, listingUI);

            // Add to shop's internal UI list
            AddToListingUICollection(shop, listingUI);
        }

        private static void BindListingUIEvents(S1UIShop.ShopInterface shop, S1UIShop.ListingUI listingUI)
        {
#if (IL2CPPMELON || IL2CPPBEPINEX)
            // Il2Cpp: Direct Action assignment
            listingUI.onClicked = DelegateSupport.ConvertDelegate<Il2CppSystem.Action>(new System.Action(() => shop.OpenAmountSelector(listingUI)));
            listingUI.onDropdownClicked = DelegateSupport.ConvertDelegate<Il2CppSystem.Action>(new System.Action(() => shop.DropdownClicked(listingUI)));
            listingUI.hoverStart = DelegateSupport.ConvertDelegate<Il2CppSystem.Action>(new System.Action(() => shop.EntryHovered(listingUI)));
            listingUI.hoverEnd = DelegateSupport.ConvertDelegate<Il2CppSystem.Action>(new System.Action(() => shop.EntryUnhovered()));
#else
            // Mono: Reflection-based method invocation
            var listingClickedMethod = GetShopMethod(shop, "ListingClicked");
            var dropdownClickedMethod = GetShopMethod(shop, "DropdownClicked");
            var entryHoveredMethod = GetShopMethod(shop, "EntryHovered");
            var entryUnhoveredMethod = GetShopMethod(shop, "EntryUnhovered");

            listingUI.onClicked = (Action)System.Delegate.Combine(
                listingUI.onClicked,
                (Action)(() => listingClickedMethod.Invoke(shop, new object[] { listingUI }))
            );

            listingUI.onDropdownClicked = (Action)System.Delegate.Combine(
                listingUI.onDropdownClicked,
                (Action)(() => dropdownClickedMethod.Invoke(shop, new object[] { listingUI }))
            );

            listingUI.hoverStart = (Action)System.Delegate.Combine(
                listingUI.hoverStart,
                (Action)(() => entryHoveredMethod.Invoke(shop, new object[] { listingUI }))
            );

            listingUI.hoverEnd = (Action)System.Delegate.Combine(
                listingUI.hoverEnd,
                (Action)(() => entryUnhoveredMethod.Invoke(shop, null))
            );
#endif
        }

        private static void AddToListingUICollection(S1UIShop.ShopInterface shop, S1UIShop.ListingUI listingUI)
        {
            try
            {
                // Access the private listingUI field via reflection
                var listingUIField = typeof(S1UIShop.ShopInterface).GetField(
                    "listingUI",
                    BindingFlags.NonPublic | BindingFlags.Instance
                );

                if (listingUIField != null)
                {
#if (IL2CPPMELON || IL2CPPBEPINEX)
                    var listingUIList = listingUIField.GetValue(shop) as Il2CppSystem.Collections.Generic.List<S1UIShop.ListingUI>;
#else
                    var listingUIList = listingUIField.GetValue(shop) as System.Collections.Generic.List<S1UIShop.ListingUI>;
#endif
                    listingUIList?.Add(listingUI);
                }
            }
            catch (System.Exception ex)
            {
                Logger.Warning($"Could not add ListingUI to shop's internal collection: {ex.Message}");
            }
        }

        /// <summary>
        /// Refreshes the icon for an item in all shop listings that display it.
        /// </summary>
        internal static int RefreshItemIconInShops(ItemDefinition item)
        {
            if (item?.S1ItemDefinition == null)
                return 0;

            int updatedCount = 0;

            try
            {
                var shops = ShopManager.GetAllShops();
                foreach (var shop in shops)
                {
                    if (shop?.S1ShopInterface == null)
                        continue;

                    // Find all listings for this item
                    foreach (var listing in shop.S1ShopInterface.Listings)
                    {
                        if (listing?.Item?.ID == item.ID)
                        {
                            // Update the icon in all ListingUI instances for this listing
                            updatedCount += UpdateListingUIcons(shop.S1ShopInterface, listing, item.Icon);
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                Logger.Error($"Failed to refresh item icon for '{item.ID}': {ex.Message}");
            }

            return updatedCount;
        }

        private static int UpdateListingUIcons(S1UIShop.ShopInterface shop, S1UIShop.ShopListing listing, Sprite newIcon)
        {
            if (newIcon == null)
                return 0;

            int updatedCount = 0;

            try
            {
                var listingUIField = typeof(S1UIShop.ShopInterface).GetField(
                    "listingUI",
                    BindingFlags.NonPublic | BindingFlags.Instance
                );

                if (listingUIField != null)
                {
#if (IL2CPPMELON || IL2CPPBEPINEX)
                    var listingUIList = listingUIField.GetValue(shop) as Il2CppSystem.Collections.Generic.List<S1UIShop.ListingUI>;
#else
                    var listingUIList = listingUIField.GetValue(shop) as System.Collections.Generic.List<S1UIShop.ListingUI>;
#endif
                    if (listingUIList != null)
                    {
                        foreach (var listingUI in listingUIList)
                        {
                            if (listingUI?.Listing == listing && listingUI.Icon != null)
                            {
                                listingUI.Icon.sprite = newIcon;
                                updatedCount++;
                            }
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                Logger.Warning($"Could not update ListingUI icons: {ex.Message}");
            }

            return updatedCount;
        }

        private static void RemoveListingUI(S1UIShop.ShopInterface shop, S1UIShop.ShopListing listing)
        {
            try
            {
                var listingUIField = typeof(S1UIShop.ShopInterface).GetField(
                    "listingUI",
                    BindingFlags.NonPublic | BindingFlags.Instance
                );

                if (listingUIField != null)
                {
#if (IL2CPPMELON || IL2CPPBEPINEX)
                    var listingUIList = listingUIField.GetValue(shop) as Il2CppSystem.Collections.Generic.List<S1UIShop.ListingUI>;
                    if (listingUIList != null)
                    {
                        for (int i = listingUIList.Count - 1; i >= 0; i--)
                        {
                            var ui = listingUIList[i];
                            if (ui?.Listing == listing)
                            {
                                if (ui.gameObject != null)
                                    UnityEngine.Object.Destroy(ui.gameObject);
                                listingUIList.RemoveAt(i);
                            }
                        }
                    }
#else
                    var listingUIList = listingUIField.GetValue(shop) as System.Collections.Generic.List<S1UIShop.ListingUI>;
                    if (listingUIList != null)
                    {
                        for (int i = listingUIList.Count - 1; i >= 0; i--)
                        {
                            var ui = listingUIList[i];
                            if (ui?.Listing == listing)
                            {
                                if (ui.gameObject != null)
                                    UnityEngine.Object.Destroy(ui.gameObject);
                                listingUIList.RemoveAt(i);
                            }
                        }
                    }
#endif
                }
            }
            catch (System.Exception ex)
            {
                Logger.Warning($"Could not remove ListingUI from shop: {ex.Message}");
            }
        }

#if (MONOMELON || MONOBEPINEX)
        private static MethodInfo? GetShopMethod(S1UIShop.ShopInterface shop, string methodName)
        {
            return typeof(S1UIShop.ShopInterface).GetMethod(
                methodName,
                BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance
            );
        }
#endif
    }
}

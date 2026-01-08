#if (IL2CPPMELON)
using S1 = Il2CppScheduleOne;
using S1ItemFramework = Il2CppScheduleOne.ItemFramework;
using S1Product = Il2CppScheduleOne.Product;
using S1Registry = Il2CppScheduleOne.Registry;
using S1Clothing = Il2CppScheduleOne.Clothing;
using S1Packaging = Il2CppScheduleOne.Product.Packaging;
#elif (MONOMELON || MONOBEPINEX || IL2CPPBEPINEX)
using S1 = ScheduleOne;
using S1ItemFramework = ScheduleOne.ItemFramework;
using S1Product = ScheduleOne.Product;
using S1Registry = ScheduleOne.Registry;
using S1Clothing = ScheduleOne.Clothing;
using S1Packaging = ScheduleOne.Product.Packaging;
#endif

using S1API.Internal.Utils;
using S1API.Money;
using S1API.Products;
using S1API.Products.Packaging;

namespace S1API.Items
{
    /// <summary>
    /// Provides access to managing items across the game.
    /// </summary>
    public static class ItemManager
    {
        /// <summary>
        /// Gets the definition of an item by its ID.
        /// </summary>
        /// <param name="itemID">The ID of the item.</param>
        /// <returns>An instance of the item definition.</returns>
        public static ItemDefinition GetItemDefinition(string itemID)
        {
            S1ItemFramework.ItemDefinition itemDefinition = S1.Registry.GetItem(itemID);

            if (itemDefinition == null)
                return null;

            // Check for specific types first (most derived to least derived)
            if (CrossType.Is(itemDefinition,
                    out S1Product.ProductDefinition productDefinition))
                return new ProductDefinition(productDefinition);

            if (CrossType.Is(itemDefinition,
                    out S1ItemFramework.CashDefinition cashDefinition))
                return new CashDefinition(cashDefinition);

            if (CrossType.Is(itemDefinition,
                    out S1Clothing.ClothingDefinition clothingDefinition))
                return new ClothingItemDefinition(clothingDefinition);

            if (CrossType.Is(itemDefinition,
                    out S1ItemFramework.BuildableItemDefinition buildableItemDefinition))
                return new BuildableItemDefinition(buildableItemDefinition);

            if (CrossType.Is(itemDefinition,
                    out S1Packaging.PackagingDefinition packagingDefinition))
                return new PackagingDefinition(packagingDefinition);

            if (CrossType.Is(itemDefinition,
                    out S1ItemFramework.StorableItemDefinition storableItemDefinition))
                return new StorableItemDefinition(storableItemDefinition);

            return new ItemDefinition(itemDefinition);
        }

        /// <summary>
        /// Manually registers an item definition with the game's registry.
        /// This is typically handled automatically by <see cref="ItemCreator"/> methods,
        /// but can be used for advanced scenarios.
        /// </summary>
        /// <param name="definition">The item definition to register.</param>
        public static void RegisterItem(ItemDefinition definition)
        {
            if (definition == null)
            {
                throw new System.ArgumentNullException(nameof(definition), "Cannot register null item definition");
            }

            S1Registry.Instance.AddToRegistry(definition.S1ItemDefinition);
        }

        /// <summary>
        /// Gets all item definitions registered in the game's registry.
        /// </summary>
        /// <returns>A list of all registered item definitions, wrapped with S1API types.</returns>
        public static System.Collections.Generic.List<ItemDefinition> GetAllItemDefinitions()
        {
            if (S1Registry.Instance == null)
                return new System.Collections.Generic.List<ItemDefinition>();

            var nativeItems = S1Registry.Instance.GetAllItems();
            if (nativeItems == null)
                return new System.Collections.Generic.List<ItemDefinition>();

            var wrappedItems = new System.Collections.Generic.List<ItemDefinition>();

            foreach (var nativeItem in nativeItems)
            {
                if (nativeItem == null)
                    continue;

                // Get the item ID directly from the native item
                string itemId = null;
                try
                {
                    itemId = nativeItem.ID;
                }
                catch
                {
                    // Skip items where we can't get the ID
                    continue;
                }

                if (string.IsNullOrEmpty(itemId))
                    continue;

                // Use GetItemDefinition to properly wrap the item with the correct type
                var wrappedItem = GetItemDefinition(itemId);
                if (wrappedItem != null)
                {
                    wrappedItems.Add(wrappedItem);
                }
            }

            return wrappedItems;
        }
    }
}

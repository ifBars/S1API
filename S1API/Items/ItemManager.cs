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
using System;
using System.Reflection;

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
                    out S1ItemFramework.AdditiveDefinition additiveDefinition))
                return new AdditiveDefinition(additiveDefinition);

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
        /// Checks whether an item ID is present in the active game registry.
        /// </summary>
        /// <param name="itemID">The ID of the item to look up.</param>
        /// <returns>True if the item is registered; otherwise, false.</returns>
        public static bool IsItemRegistered(string itemID)
        {
            if (string.IsNullOrWhiteSpace(itemID) || S1Registry.Instance == null)
            {
                return false;
            }

            return S1.Registry.ItemExists(itemID);
        }

        /// <summary>
        /// Registers an item only when it is missing from the active game registry.
        /// </summary>
        /// <param name="definition">The item definition to register.</param>
        /// <returns>True if the item is registered after the call; otherwise, false.</returns>
        public static bool EnsureItemRegistered(ItemDefinition definition)
        {
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition), "Cannot register null item definition");
            }

            if (S1Registry.Instance == null)
            {
                return false;
            }

            if (IsItemRegistered(definition.ID))
            {
                return true;
            }

            S1Registry.Instance.AddToRegistry(definition.S1ItemDefinition);
            return IsItemRegistered(definition.ID);
        }

        /// <summary>
        /// Prevents a runtime-registered item from being removed during the next scene transition.
        /// This is useful for items that must survive a menu-to-game load sequence.
        /// </summary>
        /// <param name="definition">The item definition to preserve.</param>
        /// <returns>True if the item was removed from the runtime cleanup queue, false otherwise.</returns>
        public static bool PreserveRuntimeItem(ItemDefinition definition)
        {
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition), "Cannot preserve a null item definition");
            }

            return RemoveFromRuntimeCleanupQueue(definition.S1ItemDefinition, definition.ID);
        }

        /// <summary>
        /// Removes an item definition from the registry by ID.
        /// </summary>
        /// <param name="itemID">The ID of the item to remove.</param>
        /// <returns>True if the item existed and was removed, false otherwise.</returns>
        public static bool UnregisterItem(string itemID)
        {
            if (string.IsNullOrWhiteSpace(itemID) || S1Registry.Instance == null)
            {
                return false;
            }

            ItemDefinition definition = GetItemDefinition(itemID);
            if (definition == null)
            {
                return false;
            }

            RemoveFromRuntimeCleanupQueue(definition.S1ItemDefinition, definition.ID);
            S1Registry.Instance.RemoveFromRegistry(definition.S1ItemDefinition);
            return GetItemDefinition(itemID) == null;
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

        private static bool RemoveFromRuntimeCleanupQueue(S1ItemFramework.ItemDefinition nativeDefinition, string itemId)
        {
            if (S1Registry.Instance == null || nativeDefinition == null || string.IsNullOrWhiteSpace(itemId))
            {
                return false;
            }

            FieldInfo runtimeItemsField = typeof(S1Registry).GetField("ItemsAddedAtRuntime", BindingFlags.NonPublic | BindingFlags.Instance);
            if (runtimeItemsField == null)
            {
                return false;
            }

            object runtimeItems = runtimeItemsField.GetValue(S1Registry.Instance);
            if (runtimeItems == null)
            {
                return false;
            }

            Type runtimeItemsType = runtimeItems.GetType();
            PropertyInfo countProperty = runtimeItemsType.GetProperty("Count", BindingFlags.Public | BindingFlags.Instance);
            PropertyInfo indexerProperty = runtimeItemsType.GetProperty("Item", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo removeAtMethod = runtimeItemsType.GetMethod("RemoveAt", BindingFlags.Public | BindingFlags.Instance);
            if (countProperty == null || indexerProperty == null || removeAtMethod == null)
            {
                return false;
            }

            int count = Convert.ToInt32(countProperty.GetValue(runtimeItems));
            bool removed = false;

            for (int index = count - 1; index >= 0; index--)
            {
                object register = indexerProperty.GetValue(runtimeItems, new object[] { index });
                if (register == null)
                {
                    continue;
                }

                Type registerType = register.GetType();
                FieldInfo idField = registerType.GetField("ID", BindingFlags.Public | BindingFlags.Instance);
                FieldInfo definitionField = registerType.GetField("Definition", BindingFlags.Public | BindingFlags.Instance);

                string registeredId = idField?.GetValue(register) as string;
                object registeredDefinition = definitionField?.GetValue(register);
                if (!string.Equals(registeredId, itemId, StringComparison.OrdinalIgnoreCase) &&
                    !ReferenceEquals(registeredDefinition, nativeDefinition))
                {
                    continue;
                }

                removeAtMethod.Invoke(runtimeItems, new object[] { index });
                removed = true;
            }

            return removed;
        }
    }
}

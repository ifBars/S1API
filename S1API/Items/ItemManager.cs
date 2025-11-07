#if (IL2CPPMELON)
using S1 = Il2CppScheduleOne;
using S1ItemFramework = Il2CppScheduleOne.ItemFramework;
using S1Product = Il2CppScheduleOne.Product;
using S1Registry = Il2CppScheduleOne.Registry;
#elif (MONOMELON || MONOBEPINEX || IL2CPPBEPINEX)
using S1 = ScheduleOne;
using S1ItemFramework = ScheduleOne.ItemFramework;
using S1Product = ScheduleOne.Product;
using S1Registry = ScheduleOne.Registry;
#endif

using S1API.Internal.Utils;
using S1API.Money;
using S1API.Products;

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
    }
}

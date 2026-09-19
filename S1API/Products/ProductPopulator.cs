#if (IL2CPPMELON)
using S1Product = Il2CppScheduleOne.Product;
using S1ProductPackaging = Il2CppScheduleOne.Product.Packaging;
using S1ItemFramework = Il2CppScheduleOne.ItemFramework;
#elif MONOMELON
using S1Product = ScheduleOne.Product;
using S1ProductPackaging = ScheduleOne.Product.Packaging;
using S1ItemFramework = ScheduleOne.ItemFramework;
#endif

using System.Collections.Generic;
using System.Linq;
using S1API.Items;
using S1API.Storages;
using S1API.Internal.Utils;
using S1API.Products.Packaging;
using UnityEngine;

namespace S1API.Products
{
    /// <summary>
    /// Creates product instances and adds them to storage for mod-owned setup flows.
    /// </summary>
    /// <remarks>
    /// Discovery-based helpers read products discovered in the active save. ID-based helpers
    /// resolve registered definitions through the item registry, while direct-creation helpers
    /// use the supplied definition. These APIs do not register products or stock shops. Use
    /// explicit custom-product lifecycle APIs before calling them for mod-owned definitions.
    /// </remarks>
    public static class ProductPopulator
    {
        /// <summary>
        /// Resolves a native packaging definition by its item ID.
        /// </summary>
        /// <param name="packagingId">The ID of the packaging (e.g., "baggie", "jar", "brick").</param>
        /// <returns>The live packaging definition, or <see langword="null"/> when the item is absent or not packaging.</returns>
        public static PackagingDefinition? GetPackaging(string packagingId)
        {
            var packaging = ItemManager.GetDefinition(packagingId);

            if (packaging is PackagingDefinition packagingDef)
            {
                Debug.Log($"[ProductPopulator] Found packaging: {packagingDef.Name} (ID: {packagingId})");
                return packagingDef;
            }

            Debug.LogWarning($"[ProductPopulator] Could not find packaging with ID '{packagingId}'");

            return null;
        }

        /// <summary>
        /// Gets product definitions discovered in the active save.
        /// </summary>
        /// <returns>A new list of discovered product definitions. It is empty when the save has discovered none.</returns>
        public static List<ProductDefinition> GetAllProductDefinitions()
        {
            Debug.Log("[ProductPopulator] Getting all product definitions from ProductManager.DiscoveredProducts");

            var discoveredProducts = ProductManager.DiscoveredProducts;
            Debug.Log($"[ProductPopulator] Found {discoveredProducts.Length} discovered products in save");

            var productDefs = new List<ProductDefinition>();

            foreach (var product in discoveredProducts)
            {
                productDefs.Add(product);
                Debug.Log($"[ProductPopulator] Found product: {product.Name} (ID: {product.ID})");
            }

            Debug.Log($"[ProductPopulator] Total product definitions found: {productDefs.Count}");
            return productDefs;
        }

        /// <summary>
        /// Gets discovered marijuana-family product definitions.
        /// </summary>
        /// <returns>A list of weed product definitions.</returns>
        public static List<WeedDefinition> GetWeedDefinitions()
        {
            return GetAllProductDefinitions()
                .Where(p => p is WeedDefinition)
                .Cast<WeedDefinition>()
                .ToList();
        }

        /// <summary>
        /// Gets discovered methamphetamine-family product definitions.
        /// </summary>
        /// <returns>A list of meth product definitions.</returns>
        public static List<MethDefinition> GetMethDefinitions()
        {
            return GetAllProductDefinitions()
                .Where(p => p is MethDefinition)
                .Cast<MethDefinition>()
                .ToList();
        }

        /// <summary>
        /// Gets discovered cocaine-family product definitions.
        /// </summary>
        /// <returns>A list of cocaine product definitions.</returns>
        public static List<CocaineDefinition> GetCocaineDefinitions()
        {
            return GetAllProductDefinitions()
                .Where(p => p is CocaineDefinition)
                .Cast<CocaineDefinition>()
                .ToList();
        }

        /// <summary>
        /// Gets discovered shroom-family product definitions.
        /// </summary>
        /// <returns>A list of shroom product definitions.</returns>
        public static List<ShroomDefinition> GetShroomDefinitions()
        {
            return GetAllProductDefinitions()
                .Where(p => p is ShroomDefinition)
                .Cast<ShroomDefinition>()
                .ToList();
        }

        /// <summary>
        /// Fills available storage slots with packaged discovered products.
        /// </summary>
        /// <param name="storage">The storage instance to populate.</param>
        /// <param name="packagingId">The ID of the packaging to use (e.g., "baggie", "jar", "brick").</param>
        /// <param name="quantityPerItem">The quantity of each product item.</param>
        /// <returns>The number of stacks added before storage fills, a product cannot fit, or setup fails.</returns>
        public static int PopulateWithPackagedProducts(StorageInstance storage, string packagingId, int quantityPerItem = 1)
        {
            Debug.Log($"[ProductPopulator] PopulateWithPackagedProducts called with packaging: {packagingId}");

            if (storage == null)
            {
                Debug.LogWarning("[ProductPopulator] Cannot populate null storage");
                return 0;
            }

            Debug.Log($"[ProductPopulator] Storage name: '{storage.Name}', SlotCount: {storage.SlotCount}, ItemCount: {storage.ItemCount}");

            var packaging = GetPackaging(packagingId);
            if (packaging == null)
            {
                Debug.LogError($"[ProductPopulator] Failed to get packaging '{packagingId}' - cannot create packaged products");
                return 0;
            }

            var productDefinitions = GetAllProductDefinitions();
            if (productDefinitions.Count == 0)
            {
                Debug.LogWarning("[ProductPopulator] No product definitions found in discovered products");
                return 0;
            }

            Debug.Log($"[ProductPopulator] Found {productDefinitions.Count} product definitions, filling {storage.SlotCount} slots");

            int addedCount = 0;
            int slotIndex = 0;

            while (slotIndex < storage.SlotCount && addedCount < storage.SlotCount)
            {
                var productDef = productDefinitions[slotIndex % productDefinitions.Count];

                Debug.Log($"[ProductPopulator] Slot {slotIndex + 1}/{storage.SlotCount}: Creating {quantityPerItem}g of '{productDef.Name}' in {packaging.Name}");

                var productInstance = CreatePackagedProduct(productDef, packaging, quantityPerItem);

                if (productInstance == null)
                {
                    Debug.LogWarning($"[ProductPopulator] Failed to create packaged instance for '{productDef.Name}'");
                    slotIndex++;
                    continue;
                }

                Debug.Log($"[ProductPopulator] Created packaged product: {productInstance.Definition.Name}, Quality: {productInstance.Quality}, Quantity: {productInstance.Quantity}, Packaged: {productInstance.IsPackaged}");

                if (storage.CanItemFit(productInstance, productInstance.Quantity))
                {
                    Debug.Log($"[ProductPopulator] Item fits, adding to storage...");
                    storage.AddItem(productInstance);
                    addedCount++;
                    Debug.Log($"[ProductPopulator] Successfully added {quantityPerItem}g of '{productDef.Name}' in {packaging.Name} to slot {slotIndex + 1}");
                }
                else
                {
                    Debug.LogWarning($"[ProductPopulator] Item does not fit in storage: {productDef.Name}");
                    break;
                }

                slotIndex++;
            }

            Debug.Log($"[ProductPopulator] Finished populating storage. Added {addedCount}/{storage.SlotCount} items. Storage now has {storage.ItemCount} items");
            return addedCount;
        }

        /// <summary>
        /// Adds one packaged stack for each supplied product ID that resolves and fits.
        /// </summary>
        /// <param name="storage">The storage instance to populate.</param>
        /// <param name="productIds">Product IDs to resolve through the item registry.</param>
        /// <param name="packagingId">The ID of the packaging to use (e.g., "baggie", "jar", "brick").</param>
        /// <param name="quantityPerProduct">Quantity of each product to add (default 1).</param>
        /// <returns>The number of stacks added. Unknown IDs and stacks that do not fit are skipped.</returns>
        public static int PopulateWithSpecificPackagedProducts(StorageInstance storage, List<string> productIds, string packagingId, int quantityPerProduct = 1)
        {
            if (storage == null)
            {
                Debug.LogWarning("[ProductPopulator] Cannot populate null storage");
                return 0;
            }

            if (productIds == null || productIds.Count == 0)
            {
                Debug.LogWarning("[ProductPopulator] No product IDs provided");
                return 0;
            }

            var packaging = GetPackaging(packagingId);
            if (packaging == null)
            {
                Debug.LogError($"[ProductPopulator] Failed to get packaging '{packagingId}'");
                return 0;
            }

            int addedCount = 0;

            foreach (var productId in productIds)
            {
                var itemDef = ItemManager.GetDefinition(productId);

                if (itemDef is ProductDefinition productDef)
                {
                    var productInstance = CreatePackagedProduct(productDef, packaging, quantityPerProduct);

                    if (productInstance != null && storage.CanItemFit(productInstance, quantityPerProduct))
                    {
                        storage.AddItem(productInstance);
                        addedCount++;
                        Debug.Log($"[ProductPopulator] Added {quantityPerProduct}x {productDef.Name} in {packaging.Name} to {storage.Name}");
                    }
                }
                else
                {
                    Debug.LogWarning($"[ProductPopulator] Product '{productId}' not found or is not a product");
                }
            }

            return addedCount;
        }

        /// <summary>
        /// Creates a standard-quality packaged product instance.
        /// </summary>
        /// <param name="productDef">The product definition.</param>
        /// <param name="packaging">The packaging definition.</param>
        /// <param name="quantity">The quantity of the product.</param>
        /// <returns>A new product instance, or <see langword="null"/> when native packaging conversion or construction fails.</returns>
        public static ProductInstance? CreatePackagedProduct(ProductDefinition productDef, PackagingDefinition packaging, int quantity)
        {
            try
            {
                var s1ProductDef = productDef.S1ProductDefinition;
                var s1Packaging = CrossType.As<S1ProductPackaging.PackagingDefinition>(packaging.S1ItemDefinition);

                if (s1Packaging == null)
                {
                    Debug.LogError($"[ProductPopulator] Failed to get S1 packaging definition");
                    return null;
                }

                var s1ProductInstance = new S1Product.ProductItemInstance(
                    s1ProductDef,
                    quantity,
                    S1ItemFramework.EQuality.Standard,
                    s1Packaging
                );

                Debug.Log($"[ProductPopulator] Created {packaging.Name} containing {quantity}g of {productDef.Name}");

                return new ProductInstance(s1ProductInstance);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[ProductPopulator] Exception creating packaged product: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Fills available storage slots with unpackaged discovered products.
        /// </summary>
        /// <param name="storage">The storage instance to populate.</param>
        /// <param name="quantityPerItem">The quantity of each product item.</param>
        /// <returns>The number of stacks added before storage fills, a product cannot fit, or setup fails.</returns>
        public static int PopulateWithUnpackagedProducts(StorageInstance storage, int quantityPerItem = 1)
        {
            Debug.Log("[ProductPopulator] PopulateWithUnpackagedProducts called");

            if (storage == null)
            {
                Debug.LogWarning("[ProductPopulator] Cannot populate null storage");
                return 0;
            }

            Debug.Log($"[ProductPopulator] Storage name: '{storage.Name}', SlotCount: {storage.SlotCount}, ItemCount: {storage.ItemCount}");

            var productDefinitions = GetAllProductDefinitions();
            if (productDefinitions.Count == 0)
            {
                Debug.LogWarning("[ProductPopulator] No product definitions found in discovered products");
                return 0;
            }

            Debug.Log($"[ProductPopulator] Found {productDefinitions.Count} product definitions, filling {storage.SlotCount} slots");

            int addedCount = 0;
            int slotIndex = 0;

            while (slotIndex < storage.SlotCount && addedCount < storage.SlotCount)
            {
                var productDef = productDefinitions[slotIndex % productDefinitions.Count];

                Debug.Log($"[ProductPopulator] Slot {slotIndex + 1}/{storage.SlotCount}: Creating {quantityPerItem}g of unpackaged '{productDef.Name}'");

                var productInstance = productDef.CreateInstance(quantityPerItem) as ProductInstance;

                if (productInstance == null)
                {
                    Debug.LogWarning($"[ProductPopulator] Failed to create instance for '{productDef.Name}'");
                    slotIndex++;
                    continue;
                }

                Debug.Log($"[ProductPopulator] Created unpackaged product: {productInstance.Definition.Name}, Quality: {productInstance.Quality}, Quantity: {productInstance.Quantity}, Packaged: {productInstance.IsPackaged}");

                if (storage.CanItemFit(productInstance, productInstance.Quantity))
                {
                    Debug.Log($"[ProductPopulator] Item fits, adding to storage...");
                    storage.AddItem(productInstance);
                    addedCount++;
                    Debug.Log($"[ProductPopulator] Successfully added {quantityPerItem}g of unpackaged '{productDef.Name}' to slot {slotIndex + 1}");
                }
                else
                {
                    Debug.LogWarning($"[ProductPopulator] Item does not fit in storage: {productDef.Name}");
                    break;
                }

                slotIndex++;
            }

            Debug.Log($"[ProductPopulator] Finished populating storage. Added {addedCount}/{storage.SlotCount} items. Storage now has {storage.ItemCount} items");
            return addedCount;
        }

        /// <summary>
        /// Adds one unpackaged stack for each supplied product ID that resolves and fits.
        /// </summary>
        /// <param name="storage">The storage instance to populate.</param>
        /// <param name="productIds">Product IDs to resolve through the item registry.</param>
        /// <param name="quantityPerProduct">Quantity of each product to add (default 1).</param>
        /// <returns>The number of stacks added. Unknown IDs and stacks that do not fit are skipped.</returns>
        public static int PopulateWithSpecificProducts(StorageInstance storage, List<string> productIds, int quantityPerProduct = 1)
        {
            if (storage == null)
            {
                Debug.LogWarning("[ProductPopulator] Cannot populate null storage");
                return 0;
            }

            if (productIds == null || productIds.Count == 0)
            {
                Debug.LogWarning("[ProductPopulator] No product IDs provided");
                return 0;
            }

            int addedCount = 0;

            foreach (var productId in productIds)
            {
                var itemDef = ItemManager.GetDefinition(productId);

                if (itemDef is ProductDefinition productDef)
                {
                    var productInstance = productDef.CreateInstance(quantityPerProduct) as ProductInstance;

                    if (productInstance != null && storage.CanItemFit(productInstance, quantityPerProduct))
                    {
                        storage.AddItem(productInstance);
                        addedCount++;
                        Debug.Log($"[ProductPopulator] Added {quantityPerProduct}x {productDef.Name} to {storage.Name}");
                    }
                }
                else
                {
                    Debug.LogWarning($"[ProductPopulator] Product '{productId}' not found or is not a product");
                }
            }

            return addedCount;
        }

        /// <summary>
        /// Fills available storage slots with discovered products in native jar packaging.
        /// </summary>
        /// <param name="storage">The storage instance to populate.</param>
        /// <returns>The number of stacks successfully added.</returns>
        public static int PopulateWithWeedProducts(StorageInstance storage)
        {
            return PopulateWithPackagedProducts(storage, "jar", 20);
        }

        /// <summary>
        /// Finds a storage entity on a game object or one of its children, then fills it with packaged products.
        /// </summary>
        /// <param name="gameObject">The GameObject with a StorageEntity component.</param>
        /// <param name="packagingId">The ID of the packaging to use (e.g., "baggie", "jar", "brick").</param>
        /// <param name="quantityPerItem">The quantity of each product item.</param>
        /// <returns>The number of stacks added, or <c>-1</c> when no storage entity is found.</returns>
        public static int PopulateFromGameObject(GameObject gameObject, string packagingId, int quantityPerItem = 1)
        {
            Debug.Log($"[ProductPopulator] PopulateFromGameObject called for '{gameObject?.name}' with packaging '{packagingId}'");

            if (gameObject is null)
            {
                Debug.LogWarning("[ProductPopulator] PopulateFromGameObject called with null GameObject");
                return -1;
            }

            GameObject target = gameObject;
            var storage = StorageInstance.FromGameObject(target);

            if (storage == null)
            {
                Debug.LogWarning($"[ProductPopulator] No StorageEntity found on GameObject '{gameObject?.name}', trying FromGameObjectInChildren...");
                storage = StorageInstance.FromGameObjectInChildren(target);
            }

            if (storage == null)
            {
                Debug.LogError($"[ProductPopulator] Failed to find StorageEntity on '{gameObject?.name}' or its children");
                return -1;
            }

            Debug.Log($"[ProductPopulator] Found storage, proceeding to populate...");
            return PopulateWithPackagedProducts(storage, packagingId, quantityPerItem);
        }
    }
}

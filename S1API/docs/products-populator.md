# ProductPopulator (Storage Helpers)

`S1API.Products.ProductPopulator` contains convenience helpers for creating product instances (optionally packaged) and adding them to a `S1API.Storages.StorageInstance`.

This is mainly useful for:

- shop/vendor inventories
- debug/testing
- scripted rewards and stashes

## Get packaging by ID

```csharp
using S1API.Products;

var jar = ProductPopulator.GetPackaging("jar");
var baggie = ProductPopulator.GetPackaging("baggie");
```

Common IDs depend on the base game (examples mentioned in code include: `baggie`, `jar`, `brick`).

## Enumerate discovered products

`ProductPopulator.GetAllProductDefinitions()` reads `ProductManager.DiscoveredProducts` and returns the discovered product definitions for the current save.

```csharp
using S1API.Products;

var defs = ProductPopulator.GetAllProductDefinitions();
```

There are also typed filters:

- `GetWeedDefinitions()`
- `GetMethDefinitions()`
- `GetCocaineDefinitions()`
- `GetShroomDefinitions()`

## Create a packaged instance

```csharp
using S1API.Products;

var packaging = ProductPopulator.GetPackaging("jar");
if (packaging != null)
{
    var productDef = ProductPopulator.GetAllProductDefinitions()[0];
    var inst = ProductPopulator.CreatePackagedProduct(productDef, packaging, quantity: 20);
}
```

## Populate a storage

### From a StorageInstance

```csharp
using S1API.Products;
using S1API.Storages;

int added = ProductPopulator.PopulateWithPackagedProducts(storage, packagingId: "jar", quantityPerItem: 20);
```

### From a GameObject

If you have a `GameObject` containing a storage entity (or in children), you can populate it directly:

```csharp
using S1API.Products;

int added = ProductPopulator.PopulateFromGameObject(someGameObject, packagingId: "jar", quantityPerItem: 20);
```

### Specific product IDs

```csharp
using S1API.Products;

var ids = new System.Collections.Generic.List<string> { "weed", "cocaine" };
int added = ProductPopulator.PopulateWithSpecificPackagedProducts(storage, ids, packagingId: "baggie", quantityPerProduct: 5);
```

## Notes

- `ProductManager.DiscoveredProducts` is save-dependent; if nothing is discovered yet, populators that rely on it will add nothing.
- The helper methods log a lot via `UnityEngine.Debug` (intended for debugging).

## See Also

- `S1API/docs/products-api.md`
- <xref:S1API.Products.ProductPopulator>
- <xref:S1API.Storages>

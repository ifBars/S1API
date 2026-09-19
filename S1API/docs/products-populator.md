# ProductPopulator

`ProductPopulator` creates product instances and adds them to a
`S1API.Storages.StorageInstance`. Use it for scripted rewards, test setups, or
stock that your mod owns. It does not register products, discover them, or add
them to shops.

## Resolve packaging

```csharp
using S1API.Products;

PackagingDefinition? jar = ProductPopulator.GetPackaging("jar");
```

Packaging IDs come from the installed game. Check for `null` before using one.

## Read discovered products

`GetAllProductDefinitions()` returns products discovered in the current save.
It returns an empty collection until that save has discovered products.

```csharp
using System.Collections.Generic;
using S1API.Products;

IReadOnlyList<ProductDefinition> products =
    ProductPopulator.GetAllProductDefinitions();
```

Use `GetWeedDefinitions()`, `GetMethDefinitions()`, `GetCocaineDefinitions()`,
or `GetShroomDefinitions()` when the native family matters.

## Create a packaged instance

```csharp
PackagingDefinition? packaging = ProductPopulator.GetPackaging("jar");
IReadOnlyList<ProductDefinition> products =
    ProductPopulator.GetAllProductDefinitions();

if (packaging != null && products.Count > 0)
{
    ProductInstance? instance = ProductPopulator.CreatePackagedProduct(
        products[0], packaging, quantity: 20);
}
```

## Populate storage

In this example, `storage` already refers to the target
`S1API.Storages.StorageInstance`.

```csharp
int added = ProductPopulator.PopulateWithPackagedProducts(
    storage,
    packagingId: "jar",
    quantityPerItem: 20);
```

Use `PopulateFromGameObject(...)` when you have a game object that contains a
storage entity. Use `PopulateWithSpecificPackagedProducts(...)` when the mod
owns an explicit list of product IDs.

## See also

- [Products API](products-api.md)
- [Generic custom products](generic-custom-products.md)
- <xref:S1API.Products.ProductPopulator>

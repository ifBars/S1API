# Products API

This page documents the API surface in `S1API/Products/` (definitions, instances, quality, and packaging).

If you want customer preference configuration, see `S1API/docs/products-system.md`.

## Key types

- `S1API.Products.ProductDefinition`: product definition wrapper (inherits `S1API.Items.ItemDefinition`)
- `S1API.Products.ProductInstance`: product instance wrapper (inherits `S1API.Items.ItemInstance`)
- `S1API.Products.ProductManager`: access to products discovered in the current save
- `S1API.Products.ProductDefinitionWrapper`: converts a `ProductDefinition` into a typed subclass when possible
- `S1API.Products.PackagingDefinition`: packaging definition wrapper
- `S1API.Products.Quality`: API-safe quality enum

## Getting product definitions

### From the current save (discovered products)

`ProductManager.DiscoveredProducts` returns product definitions discovered on the current save.

```csharp
using S1API.Products;

foreach (var product in ProductManager.DiscoveredProducts)
{
    // These are already passed through ProductDefinitionWrapper.Wrap(...)
    // so you may see WeedDefinition/MethDefinition/etc.
    MelonLoader.MelonLogger.Msg($"{product.ID}: {product.Name} (${product.Price})");
}
```

### By item ID

Products are also item definitions, so you can look them up by item ID.

```csharp
using S1API.Items;
using S1API.Products;

var def = ItemManager.GetItemDefinition("weed") as ProductDefinition;
if (def != null)
{
    MelonLoader.MelonLogger.Msg(def.MarketValue);
}
```

## Typed product definitions

If you want definition-specific properties, use the typed subclasses:

- `WeedDefinition`
- `MethDefinition`
- `CocaineDefinition`
- `ShroomDefinition`

`ProductDefinitionWrapper.Wrap(...)` is how S1API converts a generic definition into the best matching typed wrapper.

```csharp
using S1API.Products;

var typed = ProductDefinitionWrapper.Wrap(def);
if (typed is ShroomDefinition shroom)
{
    var mat = shroom.ShroomMaterial;
}
```

## Product properties

`ProductDefinition.Properties` returns runtime-agnostic property wrappers (`PropertyBase`) for the definition.

```csharp
using S1API.Products;

foreach (var prop in def.Properties)
{
    MelonLoader.MelonLogger.Msg(prop.ID);
}
```

## Overriding product effect behavior with callbacks

You can register callbacks for both player and NPC product effects.

### Player callbacks

By default, callbacks replace the base effect behavior:

```csharp
using S1API.Products;
using S1API.Properties;

ProductManager.SetEffectCallback(Property.Euphoric, player =>
{
    // Custom behavior instead of the base effect
    player.Heal(10);
});

// Optional: run callback AND keep default effect behavior
ProductManager.SetEffectCallback(Property.Euphoric, player =>
{
    player.Heal(5);
}, allowDefaultEffect: true);

// Remove later if needed
ProductManager.RemoveEffectCallback(Property.Euphoric);
```

Use `ProductManager.ClearEffectCallbacks()` to remove all registered overrides.

### NPC callbacks

You can also intercept effects applied through `ApplyEffectsToNPC`:

```csharp
using S1API.Products;
using S1API.Properties;

ProductManager.SetNpcEffectCallback(Property.Sneaky, npc =>
{
    // Custom NPC effect behavior
    npc.Heal(5f);
});

// Optional: run callback AND keep default effect behavior
ProductManager.SetNpcEffectCallback(Property.Sneaky, npc =>
{
    npc.Heal(2f);
}, allowDefaultEffect: true);

// Remove later if needed
ProductManager.RemoveNpcEffectCallback(Property.Sneaky);
```

Use `ProductManager.ClearNpcEffectCallbacks()` to remove all registered NPC overrides.

## Creating product instances

### Unpackaged

```csharp
using S1API.Products;

var instance = def.CreateInstance(quantity: 10) as ProductInstance;
```

### Packaged

To create packaged product, you need a `PackagingDefinition` (see next section).

```csharp
using S1API.Products;

var packaged = def.CreatePackagedInstance(quantity: 10, packaging);
if (packaged != null)
{
    MelonLoader.MelonLogger.Msg(packaged.IsPackaged);
}
```

## Packaging

Packaging is represented by `PackagingDefinition`.

- `Quantity`: how much the packaging holds
- `StealthLevel`: `None`, `Basic`, or `Advanced`

```csharp
using S1API.Products;

MelonLoader.MelonLogger.Msg($"{packaging.Name}: holds {packaging.Quantity}, stealth={packaging.StealthLevel}");
```

Tip: `S1API.Products.ProductPopulator.GetPackaging("jar")` is a convenient way to fetch common packaging by ID.

## Working with ProductInstance

`ProductInstance` provides:

- `Definition`: the associated `ProductDefinition`
- `Quality`: `S1API.Products.Quality`
- `IsPackaged`
- `AppliedPackaging` (only meaningful when `IsPackaged == true`)
- `Properties` (same as `Definition.Properties`)

```csharp
using S1API.Products;

void Log(ProductInstance inst)
{
    MelonLoader.MelonLogger.Msg($"{inst.Definition.Name} x{inst.Quantity} ({inst.Quality}) packaged={inst.IsPackaged}");
}
```

## See Also

- `S1API/docs/products-system.md` (customer preferences and properties)
- `S1API/docs/products-populator.md` (filling storages with products)
- <xref:S1API.Products> (API reference)

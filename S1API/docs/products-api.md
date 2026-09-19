# Products API

Use this page to inspect existing product definitions, create item instances,
work with properties and packaging, or register effect callbacks.

For a task-based map of the product APIs, see [Products system](products-system.md).
Customer preferences belong in [Customer behavior](customer-behavior.md).

## Key types

- `S1API.Products.ProductDefinition`: product definition wrapper (inherits `S1API.Items.Storable.StorableItemDefinition`)
- `S1API.Products.ProductInstance`: product instance wrapper (inherits `S1API.Items.ItemInstance`)
- `S1API.Products.ProductManager`: access to products discovered in the current save
- `S1API.Products.ProductDefinitionWrapper`: converts a `ProductDefinition` into a typed subclass when possible
- `S1API.Products.WeedItemCreator`: creates native-family marijuana variants
- `S1API.Products.WeedDefinitionBuilder`: validates and builds a weed variant through the native creator
- `S1API.Products.CustomProductItemCreator`: creates generic non-mixable product builders
- `S1API.Products.CustomProductDefinitionBuilder`: validates and lifecycle-registers a fixed generic product
- `S1API.Products.CustomProductDefinition`: typed wrapper for a registered generic custom product
- `S1API.Products.ProductKindMetadata`: immutable logical-kind display, search, and Product Manager metadata
- `S1API.Products.ProductKindMetadataBuilder`: configures logical-kind display, search, and Product Manager behavior
- `S1API.Products.ProductKindMetadataRegistry`: looks up immutable logical-kind metadata
- `S1API.Products.ProductPresentationProfileBuilder`: configures mod-owned loose presentation contexts
- `S1API.Products.ProductPresentationTransform`: overrides a cloned context visual's local transform
- `S1API.Products.ProductPresentationProfileRegistry`: registers profiles by stable product ID or logical product kind
- `S1API.Products.ProductConsumptionProfileBuilder`: configures intrinsic custom-product consumption callbacks
- `S1API.Products.ProductConsumptionProfileRegistry`: registers consumption profiles by stable product ID or logical product kind
- `S1API.Products.PackagingDefinition`: packaging definition wrapper
- `S1API.Products.Quality`: API-safe quality enum

For creation and lifecycle guidance, see [Logical Product Kinds](product-kinds.md),
[Native Weed Variants](weed-variants.md), and
[Generic Custom Products](generic-custom-products.md).

## Getting product definitions

### From the current save

`ProductManager.DiscoveredProducts` returns product definitions discovered on the current save.

```csharp
using S1API.Products;

foreach (var product in ProductManager.DiscoveredProducts)
{
    // ProductManager, ItemManager, and ProductInstance.Definition all use the same typed factory,
    // so this may be a native-family wrapper or a registered CustomProductDefinition.
    MelonLoader.MelonLogger.Msg($"{product.ID}: {product.Name} (${product.Price})");
}
```

### From an item ID

Products are also item definitions, so you can look them up by item ID.

```csharp
using S1API.Items;
using S1API.Products;

var def = ItemManager.GetDefinition("weed") as ProductDefinition;
if (def != null)
{
    MelonLoader.MelonLogger.Msg(def.MarketValue);
}
```

Product definitions preserve the native storable-item inheritance contract, so members such as
`BasePurchasePrice`, `ResellMultiplier`, and `RequiredRank` are also available.

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

## Drug types

Use the API-safe accessors when your mod targets both Mono and IL2CPP:

```csharp
using System.Collections.Generic;
using S1API.Products;

DrugType primaryType = def.PrimaryDrugType;
IReadOnlyList<DrugType> allTypes = def.DrugTypeValues;
```

The older `DrugType` and `DrugTypes` members remain available as non-error obsolete compatibility
members. They expose native game types that differ between Mono and IL2CPP, are not cross-runtime
compatible, and therefore require conditional compilation in cross-runtime mods.

`DrugType.MDMA` and `DrugType.Heroin` mirror values present in the native enum. Their presence does
not mean that every native product system supports those types.

## Product effect callbacks

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

### Player clear callbacks

Register a separate callback to unwind state when the native product lifecycle clears an effect. Clear callbacks
replace the base clear behavior by default, and should be safe if the game clears the same effect more than once.

```csharp
ProductManager.SetEffectClearCallback(Property.Euphoric, player =>
{
    // Remove only state this effect owns.
});

// Optional: run the callback AND keep default clear behavior.
ProductManager.SetEffectClearCallback(Property.Euphoric, player =>
{
    // Custom cleanup.
}, allowDefaultEffect: true);
```

Use `ProductManager.RemoveEffectClearCallback(...)` or `ProductManager.ResetEffectClearCallbacks()` to remove
registered player clear callbacks.

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

### NPC clear callbacks

NPC clear callbacks follow the same lifecycle and default behavior:

```csharp
ProductManager.SetNpcEffectClearCallback(Property.Sneaky, npc =>
{
    // Remove only state this effect owns.
});

// Optional: run the callback AND keep default clear behavior.
ProductManager.SetNpcEffectClearCallback(Property.Sneaky, npc =>
{
    // Custom cleanup.
}, allowDefaultEffect: true);
```

Use `ProductManager.RemoveNpcEffectClearCallback(...)` or `ProductManager.ResetNpcEffectClearCallbacks()` to remove
registered NPC clear callbacks.

## Intrinsic custom-product consumption profiles

Use a consumption profile for behavior that belongs to a registered custom product or logical
`ProductKind`, rather than to a visible product property. Profiles run after ordinary property effects
at the native apply/clear lifecycle points. A product-ID registration overrides a product-kind
registration; kind registrations also cover generated mixed products that retain that kind.

```csharp
using S1API.Products;

var profile = new ProductConsumptionProfileBuilder()
    .WithProviderCompatibility("examplemod:mdma-consumption", 1)
    .OnPlayerApply(context =>
    {
        // Player callbacks are local-only, so camera and audio state stays local.
    })
    .OnPlayerClear(context =>
    {
        // Cleanup must be safe when the game clears a product repeatedly.
    })
    .OnNpcApply(context =>
    {
        // NPC wrappers can be unavailable for game-owned NPCs; TargetId remains available.
    })
    .OnNpcClear(context => { })
    .Build();

ProductConsumptionProfileRegistry.RegisterForProductKind(customKind, profile);
```

The provider ID and version are scalar multiplayer compatibility data. Register the same profile
provider on every peer before custom-product manifest validation; S1API never serializes callbacks,
Unity objects, assets, or transient camera/audio state. Active profile state is not restored across
save/load, reconnect, or late join. If an apply callback fails, S1API immediately attempts its clear
callback and permits the next native apply lifecycle call to retry. `TargetId` is the native stable
player code for players and the native NPC ID for NPCs; it is not a display name.

## Create product instances

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

## See also

- [Products system](products-system.md)
- [ProductPopulator](products-populator.md)
- <xref:S1API.Products> (API reference)

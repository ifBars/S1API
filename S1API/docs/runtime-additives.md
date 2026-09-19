# Runtime additives

Create `AdditiveDefinition` instances with the additive builder. Definitions
are read-only after registration, so configure their effects before `Build()`.

Register additives before save restoration, preferably in
`GameLifecycle.OnPreLoad`. An additive that registers later may not be
available to restored items or grow containers.

## Register an additive

```csharp
using S1API.Items;
using S1API.Lifecycle;

GameLifecycle.OnPreLoad += () =>
{
    AdditiveDefinition growthBooster = AdditiveItemCreator.CreateBuilder()
        .WithBasicInfo(
            id: "my-mod:growth-booster",
            name: "Growth Booster",
            description: "A custom growing additive.",
            category: ItemCategory.Growing)
        .WithStackLimit(10)
        .WithPricing(basePurchasePrice: 150f, resellMultiplier: 0.5f)
        .WithEffects(
            yieldMultiplier: 1.5f,
            instantGrowth: 0.5f,
            qualityChange: 1f)
        .Build();
};
```

## Clone a native additive

```csharp
AdditiveDefinition variant = AdditiveItemCreator.CloneFrom("pgr")
    .WithBasicInfo(
        "my-mod:pgr-variant",
        "PGR Variant",
        "A modified growing additive.",
        ItemCategory.Growing)
    .WithEffects(1.25f, 0.25f, 0f)
    .Build();
```

## Allow an additive in grow containers

Grow containers use a global allowlist. Register the additive first, then add
its stable ID during `OnPreLoad`:

```csharp
using S1API.Growing;

GameLifecycle.OnPreLoad += () =>
    GrowContainerAdditives.AllowAdditive("my-mod:growth-booster");
```

Repeated calls for the same ID do nothing. If S1API cannot resolve the ID to an
`AdditiveDefinition`, it logs one warning and skips it.

## See also

- [Item registration](item-registration-basics.md)
- [Item builder reference](item-builder-reference.md)
- <xref:S1API.Items.AdditiveItemCreator>

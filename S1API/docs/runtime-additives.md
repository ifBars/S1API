# Runtime Additives

Create runtime additives (`AdditiveDefinition`) through the additive builder API.

## Important Notes

- `AdditiveDefinition` is builder-only and intentionally read-only after registration to avoid mid-session mutation issues
- Configure additive effects during build time
- For best results, register additives before save data loads
- Prefer `GameLifecycle.OnPreLoad` when possible

## Example: Recommended Timing

```csharp
using MelonLoader;
using S1API.Items;
using S1API.Lifecycle;

public class MyMod : MelonMod
{
    public override void OnSceneWasLoaded(int buildIndex, string sceneName)
    {
        if (sceneName != "Main")
            return;

        GameLifecycle.OnPreLoad += RegisterItems;
    }

    private static void RegisterItems()
    {
        var growthBooster = AdditiveItemCreator.CreateBuilder()
            .WithBasicInfo(
                id: "mymod_growth_booster",
                name: "Growth Booster",
                description: "A custom growth enhancer additive.",
                category: ItemCategory.Growing
            )
            .WithStackLimit(10)
            .WithPricing(basePurchasePrice: 150f, resellMultiplier: 0.5f)
            .WithEffects(
                yieldMultiplier: 1.5f,
                instantGrowth: 0.5f,
                qualityChange: 1.0f
            )
            .Build();

        MelonLogger.Msg($"Registered additive: {growthBooster.Name} ({growthBooster.ID})");
    }
}
```

## Cloning an Existing Additive

```csharp
var variant = AdditiveItemCreator.CloneFrom("pgr")
    .WithBasicInfo("mymod_pgr_variant", "PGR Variant", "A tweaked PGR.", ItemCategory.Growing)
    .WithEffects(1.25f, 0.25f, 0.0f)
    .Build();
```

## Allowing Additives on Grow Containers

Grow containers have a fixed additive allowlist (`GrowContainer.AllowedAdditives`). S1API can extend that allowlist globally so mods do not need to patch `GrowContainer.InitializeGridItem`.

Notes:

- Applies to all grow containers
- Duplicate `AllowAdditive(...)` calls are a no-op
- If an ID cannot be resolved to an `AdditiveDefinition` at runtime, S1API warns once and skips it

```csharp
using MelonLoader;
using S1API.Growing;
using S1API.Lifecycle;

public class MyMod : MelonMod
{
    public override void OnSceneWasLoaded(int buildIndex, string sceneName)
    {
        if (sceneName != "Main")
            return;

        GameLifecycle.OnPreLoad += () =>
        {
            GrowContainerAdditives.AllowAdditive("mymod_growth_booster");
        };
    }
}
```

## See Also

- [Item Registration & Basics](item-registration-basics.md)
- [Builder API Reference](item-builder-reference.md)
- <xref:S1API.Items.AdditiveItemCreator>
- <xref:S1API.Items.AdditiveDefinitionBuilder>
- <xref:S1API.Items.AdditiveDefinition>

# Mixing Ingredients & Effects

Create custom mixing ingredients (the additives you drop into the Mixing Station),
custom effects (drug properties), and mixing reactions between them.

## Opting a custom logical kind into mixing

`ProductMixingProfile` opts one logical `ProductKind` into the native mixing
lifecycle. `ProductMixingMap` is an explicit native execution strategy, not a
logical-kind alias: the output preserves the logical kind selected by the
factory. This supports mod-created kinds with no game enum as well as kinds
whose optional compatibility metadata is MDMA or another dormant enum.

The input generic product must use the same explicit native map through
`CustomProductDefinitionBuilder.WithNativeMixerMap(...)`; otherwise S1API
rejects the mix before the unsupported native switch. Do not cast or invent
native enum values.

```csharp
const float MdmaBasePrice = 40f;
var kind = new ProductKindBuilder("moredrugs:mdma").Build();

var profile = new ProductMixingProfileBuilder(kind)
    .WithMixerMap(ProductMixingMap.Cocaine)
    .WithOutputFactoryCompatibility("moredrugs:mdma-mixing", 1)
    .WithOutputFactory(input => new ProductMixingOutputDefinition(
        input.MixName,
        input.SourceKind,
        Math.Min(999f, Math.Max(1f, input.CalculateNativeValue(MdmaBasePrice)))))
    .Build();
```

### Pricing a mix from its effects

`ProductMixingOutput.Properties` lists the effects the output will carry, in
native order, after the native mixing calculation and any registered mixing
reactions. Each `ProductMixingEffect` exposes its `Id`, `Name`, `Tier`,
`ValueChange`, and `ValueMultiplier`.

`ProductMixingOutput.CalculateNativeValue(baseValue)` prices those effects with
the game's own product-value calculation, so mods do not need a table of effect
multipliers. Native mixes are priced from their family's fixed base price, so
pass your kind's fixed base price rather than `SourcePrice`. `SourcePrice` is
the source product's price; for a source that is itself a generated mix it is
the price that mix's factory returned, so adding to it compounds across
repeated mixes. Clamp the result to the 1 through 999 range that
`ProductMixingOutputDefinition` accepts.

The factory runs on every peer, and both members depend only on the mix's
effects, so the result stays deterministic. When you change how a factory
prices outputs, bump its `WithOutputFactoryCompatibility` version.

Use the same output-factory compatibility identity and version on every peer.
S1API includes it and the selected map in the multiplayer manifest, rejects a
mismatch before player data, and transports scalar descriptors for mixes created
after the host session starts so late joins and reconnects recreate them.

Generated product IDs are allocated only after the native mix-name sanitizer
has finished, then namespaced deterministically by source product and sanitized
native ID. Saves and FishNet RPCs therefore use the same ID. For packaging
content registered by product ID, add `RegisterForProductKind(...)` as a safe
fallback for generated output IDs; product-specific content still takes
precedence.

## Important Notes

- A mixing ingredient is a builder-only definition. Configure its imprinted effect at build time.
- S1API re-applies your ingredients and effects on every load, so create them once (the recommended
  place is `GameLifecycle.OnPreLoad`, or any time after your mod initializes).
- Custom effects and ingredients are client-local. In multiplayer every player who should see them must
  run the same mod, so the same effects and ingredients exist on every peer (including the host).
- A mixing ingredient does not need a station prefab. It only needs an imprinted effect and to be
  registered, which the builder handles for you.

## Creating a Mixing Ingredient

An ingredient imprints one or more effects onto a product when mixed. The first effect is the one applied.

```csharp
using MelonLoader;
using S1API.Items;
using S1API.Items.Ingredient;
using S1API.Lifecycle;
using S1API.Properties;

public class MyMod : MelonMod
{
    public override void OnSceneWasLoaded(int buildIndex, string sceneName)
    {
        if (sceneName != "Main")
            return;

        GameLifecycle.OnPreLoad += RegisterContent;
    }

    private static void RegisterContent()
    {
        var chili = MixIngredientItemCreator.CreateBuilder()
            .WithBasicInfo(
                id: "mymod_chili",
                name: "Chili",
                description: "A fiery pod that adds a kick to the mix.",
                category: ItemCategory.Ingredient)
            .WithPricing(basePurchasePrice: 12f, resellMultiplier: 0.4f)
            .WithEffect(Property.Spicy)
            .Build();

        MelonLogger.Msg($"Registered ingredient: {chili.Name} ({chili.ID})");
    }
}
```

`WithEffect` accepts any vanilla token from `S1API.Properties.Property` or a custom effect (below).
Use `WithEffects(...)` to carry more than one effect.

### Appearance

- Set the inventory icon with `WithIcon(sprite)`.
- The game requires every item to have a physical "stored" body (with a valid footprint) so it can be placed
  in slots and containers. By default a mixing ingredient reuses a vanilla mixer's body, so it works out of the
  box. Supply your own with `WithStoredItem(prefab)` if you want a custom world model; that is respected and not
  overwritten.

### Cloning an existing ingredient

```csharp
var variant = MixIngredientItemCreator.CloneFrom("cuke")
    .WithBasicInfo("mymod_super_cuke", "Super Cuke", "An enhanced cuke.", ItemCategory.Ingredient)
    .Build();
```

## Creating a Custom Effect

A custom effect is a real game property with configurable metadata, value contribution, and optional
behavior. The behavior runs through the same effect-callback path used to override vanilla effects.

```csharp
using S1API.Products;
using S1API.Properties;
using UnityEngine;

var glow = EffectCreator.CreateBuilder()
    .WithBasicInfo("mymod_glow", "Glow", "A soft, warm glow.")
    .WithTier(2)
    .WithAddictiveness(0.05f)
    .WithValue(valueChange: 10, valueMultiplier: 1.1f)
    .WithColors(productColor: new Color(0.2f, 0.8f, 0.9f), labelColor: Color.white)
    .WithBehavior(player =>
    {
        // Runs when this effect triggers on the local player.
    })
    .WithClearBehavior(player =>
    {
        // Runs when the native product lifecycle clears this effect from the local player.
        // Keep this cleanup safe if it is called more than once.
    })
    .Build();

// Use the custom effect on an ingredient just like a vanilla one:
var sparkPowder = MixIngredientItemCreator.CreateBuilder()
    .WithBasicInfo("mymod_spark_powder", "Spark Powder", "A shimmering powder.", ItemCategory.Ingredient)
    .WithEffect(glow)
    .Build();
```

Notes:

- If you do not set a behavior, the effect is purely cosmetic and value-affecting (it changes the
  product's name, colour, and value, but does nothing to the player).
- Custom effects are automatically given a place on each drug's mixing map so they can be mixed again
  without breaking the game. By default they sit off to the side and do not capture other mixes.
- Use `ForDrugs(...)` to restrict which drugs an effect applies to, and `WithMixMapPlacement(...)` /
  `WithMixGeometry(...)` for advanced, geometry-based reactions.

## Mixing Reactions

Reactions transform the effects a mix produces, letting you build combos and chains. A rule fires when a
product is mixed with an ingredient contributing a specific effect and the result already carries another.

```csharp
using S1API.Products;
using S1API.Properties;

// When an ingredient contributing Spicy is mixed onto a Marijuana product that already has Calming,
// also produce your custom Glow effect.
MixReactions.AddRule(
    mixerEffect: Property.Spicy,
    whenResultContains: Property.Calming,
    addResult: glow,
    drug: DrugType.Marijuana,
    replaceMatched: false);
```

- `mixerEffect` is the effect the mixed-in ingredient contributes.
- `whenResultContains` must already be present in the mix result for the rule to fire.
- `replaceMatched: true` swaps the matched effect for the result; `false` adds it (up to 8 effects).
- Omit `drug` to apply the rule to every drug.

Reactions run after the game's own mixing calculation and are deterministic, so no networking is needed
as long as every peer runs the same rules and effects.

## Making an Ingredient Purchasable

An ingredient carries a price and an optional rank requirement, but that alone does not list it in a shop.
To sell it, add it to a shop's inventory once the shop exists:

```csharp
using S1API.Shops;

GameLifecycle.OnLoadComplete += () =>
{
    var shop = ShopManager.GetShopByName("<shop name>");
    shop?.AddItem(chili, customPrice: 12f);
};
```

Resolve the shop name at runtime with `ShopManager.GetAllShops()`. As with the ingredient itself, adding
to a shop is client-local, so do it on every peer.

# Stations

This page documents station-related APIs (things that are not items themselves, but interact with item definitions and station UI).

## Chemistry Station Recipes

Register **Chemistry Station** recipes (i.e., `StationRecipe`) via a builder API.

Important notes:
- The game-defined `RecipeID` is `"{qty}x{productId}"`. If another recipe with the same ID is already registered, S1API will **warn + skip** (first wins).
- Ingredient items must exist and have a valid `StationItem` (the builder throws if not).
- Recommended timing: register recipes during `GameLifecycle.OnPreLoad` (late registration is supported; it will appear the next time the Chemistry Station UI is opened).

### Example (recommended timing)

```csharp
using MelonLoader;
using S1API.Lifecycle;
using S1API.Stations;
using UnityEngine;

public class MyMod : MelonMod
{
    public override void OnSceneWasLoaded(int buildIndex, string sceneName)
    {
        if (sceneName != "Main")
            return;

        GameLifecycle.OnPreLoad += RegisterChemistryRecipes;
    }

    private static void RegisterChemistryRecipes()
    {
        ChemistryStationRecipes.CreateAndRegister(b => b
            .WithTitle("My Custom Recipe")
            .WithCookTimeMinutes(10)
            .WithFinalLiquidColor(new Color(0.2f, 0.8f, 0.4f, 1f))
            // Product item must already exist in the registry (base-game or custom item)
            .WithProduct(itemId: "mymod_custom_product_item", quantity: 5)
            // Ingredient item(s) must have a StationItem (station-usable items)
            .WithIngredient(itemId: "ingredient_item_id", quantity: 1)
            .WithIngredientOptions(new[] { "ingredient_variant_a", "ingredient_variant_b" }, quantity: 1)
        );
    }
}
```

## Station Items for Custom Ingredients

Some station/minigame tasks (like Chemistry) spawn ingredient props by instantiating `StorableItemDefinition.StationItem`.
If an ingredient item has no StationItem, the game will log errors and may skip that ingredient.

For runtime/custom items, set a StationItem prefab when you build the item:

```csharp
using S1API.Items;

// A prefab GameObject that has a StationItem component (typically loaded from an AssetBundle)
GameObject myIngredientStationItemPrefab = ...;

var ingredient = ItemCreator.CreateBuilder()
    .WithBasicInfo("mymod_custom_ingredient", "Custom Ingredient", "Used in stations.", ItemCategory.Consumable)
    .WithStationItem(myIngredientStationItemPrefab)
    .Build();
```

## See Also

- <xref:S1API.Stations.ChemistryStationRecipeBuilder> - Chemistry Station recipe builder API
- <xref:S1API.Stations.ChemistryStationRecipes> - Chemistry Station recipe registry API


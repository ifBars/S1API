# Item Registration & Basics

S1API provides two approaches for creating custom items: a flexible Builder API and a convenient Creator API. Both approaches automatically register items with the game's registry.

> **Note**: All items in Schedule One are `StorableItemDefinition` instances. The base `ItemDefinition` class is not used directly.

## Important: Timing for Item Registration

Items should be registered after the `Main` scene loads to ensure the game's registry is fully initialized and items persist correctly:

```csharp
using MelonLoader;
using S1API.Items;

public class MyMod : MelonMod
{
    private bool _itemsInitialized = false;

    public override void OnSceneWasLoaded(int buildIndex, string sceneName)
    {
        if (sceneName == "Main" && !_itemsInitialized)
        {
            InitializeItems();
            _itemsInitialized = true;
        }
    }

    private void InitializeItems()
    {
        // Create your items here
    }
}
```

## Simple Item Creation

Use `ItemCreator.CreateItem()` for straightforward item creation:

```csharp
using S1API.Items;

private void InitializeItems()
{
    var myTool = ItemCreator.CreateItem(
        id: "my_custom_tool",
        name: "Custom Tool",
        description: "A special tool for crafting",
        category: ItemCategory.Tools,
        stackLimit: 5,
        basePurchasePrice: 25f,
        resellMultiplier: 0.3f
    );

    MelonLogger.Msg($"Created item: {myTool.Name}");
}
```

## Builder Pattern

For more control, use the builder pattern:

```csharp
using S1API.Items;

var myItem = ItemCreator.CreateBuilder()
    .WithBasicInfo("my_item", "My Item", "Description", ItemCategory.Consumable)
    .WithStackLimit(10)
    .WithPricing(50f, 0.5f)
    .WithLegalStatus(LegalStatus.Legal)
    .Build();
```

## Item Categories

Available item categories:

- `ItemCategory.Product` - Drug products (Cocaine, Weed, etc.)
- `ItemCategory.Packaging` - Baggies, Bricks, Jars
- `ItemCategory.Growing` - Soil, Fertilizer, Pots
- `ItemCategory.Tools` - Clippers, Trash Bags
- `ItemCategory.Furniture` - TV, Trash Can, Bed
- `ItemCategory.Lighting` - Floor Lamps, Halogen Lights
- `ItemCategory.Cash` - Cash items
- `ItemCategory.Consumable` - Cuke, Energy Drink
- `ItemCategory.Equipment` - Drying Rack, Brick Press
- `ItemCategory.Ingredient` - Acid, Banana, Chili
- `ItemCategory.Decoration` - GoldBar, WallClock
- `ItemCategory.Clothing` - Clothing items

## Related: Stations

Some station APIs are item-adjacent because they reference item definitions, but they are documented separately.

See [Stations](stations.md) for Chemistry Station recipe registration.

## Tips

- Register items in `OnSceneWasLoaded` when `sceneName == "Main"` for proper persistence
- Use unique, descriptive IDs such as `mymod_toolname`
- Icons should be square and at least 128x128 pixels
- Consider gameplay balance when setting stack limits and pricing
- Test items in both single-player and multiplayer scenarios
- Items registered during `OnLateInitializeMelon()` may be cleared on scene transitions

## See Also

- [Runtime Additives](runtime-additives.md)
- [Item Icons](item-icons.md)
- [Equippable Items](equippable-items.md)
- <xref:S1API.Items.ItemDefinition>
- <xref:S1API.Items.ItemCreator>

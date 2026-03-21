# Creating Custom Items

S1API provides a comprehensive item system for Schedule One, including standard storable items, additives, equippables, icons, and third-person avatar prefabs.

## Overview

The Items system allows you to:

- Create standard `StorableItemDefinition` instances with the Creator API or Builder API
- Register runtime additives with custom effects
- Attach equippable behavior and viewmodels to items
- Add icons from embedded resources or AssetBundles
- Register custom `AvatarEquippable` prefabs for third-person presentation

> **Note**: All items in Schedule One are storable items (`StorableItemDefinition`). The base `ItemDefinition` class is not used directly for custom items.

## Documentation Structure

The Items system is documented across multiple focused pages:

### Core Concepts
- **[Item Registration & Basics](item-registration-basics.md)** - Registration timing, Creator API, Builder API, categories, and common setup tips
- **[Runtime Additives](runtime-additives.md)** - Creating additive definitions and allowing them on grow containers
- **[Item Icons](item-icons.md)** - Loading item icons from embedded resources and AssetBundles

### Equippables
- **[Equippable Items](equippable-items.md)** - Basic equippables, viewmodels, use callbacks, and custom equippable behaviors
- **[Avatar Equippable Prefabs](avatar-equippable-prefabs.md)** - Creating and registering third-person avatar prefabs from AssetBundles
- **[Creating Custom Clothing Items](clothing-items.md)** - Clothing-specific item setup

### API Reference
- **[Builder API Reference](item-builder-reference.md)** - Builder methods, advanced notes, and item-specific best practices
- <xref:S1API.Items> - Detailed API documentation

## Quick Start

Items should usually be registered after the `Main` scene loads so the game's registry is initialized correctly:

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
}
```

## What To Read First

- Start here: **[Item Registration & Basics](item-registration-basics.md)**
- Then: **[Equippable Items](equippable-items.md)** if the item can be held or used
- As needed: **[Runtime Additives](runtime-additives.md)**, **[Item Icons](item-icons.md)**, **[Avatar Equippable Prefabs](avatar-equippable-prefabs.md)**

## Related Systems

- **[Stations](stations.md)** - Chemistry station recipe registration and other item-adjacent systems
- **[Products & Properties](products-system.md)** - Product definitions and property systems
- **[Save System](save-system.md)** - Persisting item-related state

## Next Steps

1. Register a simple item: [Item Registration & Basics](item-registration-basics.md)
2. Make it holdable or usable: [Equippable Items](equippable-items.md)
3. Add presentation polish: [Item Icons](item-icons.md) or [Avatar Equippable Prefabs](avatar-equippable-prefabs.md)

# Equippable Items

Equippable items can be held by the player and may have custom behavior. Schedule One supports several types of equippables:

- Basic equippables that can be held but have no visual model
- Viewmodel equippables with first-person 3D models
- Usable items that respond to player input while equipped

The game does not have a unified usable-item system. Each equippable handles its own input detection by overriding `Update()` and checking input. S1API provides both a callback-based workflow and a fully custom `MonoBehaviour` workflow.

## Basic Equippable

```csharp
var equippable = ItemCreator.CreateEquippableBuilder()
    .CreateBasicEquippable("MyItemEquippable")
    .WithInteraction(canInteract: true, canPickup: true)
    .Build();

var item = ItemCreator.CreateBuilder()
    .WithBasicInfo("my_equippable", "Equippable Item", "Can be held", ItemCategory.Tools)
    .WithEquippable(equippable)
    .Build();
```

## Viewmodel Equippables

Viewmodel equippables allow items to be held in first-person with a visible 3D model. They can also optionally define third-person avatar behavior.

### Basic Viewmodel Equippable

```csharp
var equippable = ItemCreator.CreateEquippableBuilder()
    .CreateViewmodelEquippable("MyViewmodelItem")
    .WithInteraction(canInteract: true, canPickup: true)
    .WithViewmodelTransform(
        position: new Vector3(0.2f, -0.15f, 0.3f),
        rotation: new Vector3(0f, 0f, 0f),
        scale: Vector3.one
    )
    .Build();

var item = ItemCreator.CreateBuilder()
    .WithBasicInfo("my_viewmodel", "Viewmodel Item", "Has 3D viewmodel", ItemCategory.Tools)
    .WithEquippable(equippable)
    .Build();
```

### Viewmodel Transform Settings

`WithViewmodelTransform()` configures how the item appears in first-person:

- `position` - Local position offset relative to the camera
- `rotation` - Local euler rotation
- `scale` - Local scale, usually `Vector3.one`

## Third-Person Avatar Animations

You can configure how the item appears when other players see you holding it.

### Using Existing Base Game Assets

Schedule One includes many `AvatarEquippable` prefabs. S1API provides `AvatarEquippablePaths` constants for common ones:

```csharp
using S1API.Items;

var equippable = ItemCreator.CreateEquippableBuilder()
    .CreateViewmodelEquippable("MyItem")
    .WithAvatarEquippable(
        assetPath: AvatarEquippablePaths.Knife,
        hand: AvatarHand.Right,
        animationTrigger: "RightArm_Hold_ClosedHand"
    )
    .Build();
```

Available base game paths include:

- `AvatarEquippablePaths.Baton`
- `AvatarEquippablePaths.Beer`
- `AvatarEquippablePaths.BrokenBottle`
- `AvatarEquippablePaths.Coffee`
- `AvatarEquippablePaths.Cuke`
- `AvatarEquippablePaths.Hammer`
- `AvatarEquippablePaths.Joint`
- `AvatarEquippablePaths.Knife`
- `AvatarEquippablePaths.M1911`
- `AvatarEquippablePaths.PhoneLowered`
- `AvatarEquippablePaths.PhoneRaised`
- `AvatarEquippablePaths.Pipe`
- `AvatarEquippablePaths.Revolver`
- `AvatarEquippablePaths.Taser`
- `AvatarEquippablePaths.TrashBag`

You can also use a raw asset path string:

```csharp
.WithAvatarEquippable(
    assetPath: "avatar/equippables/MyCustomItem",
    hand: AvatarHand.Right,
    animationTrigger: "RightArm_Hold_ClosedHand"
)
```

### Creating Your Own AvatarEquippable Prefab

See [Avatar Equippable Prefabs](avatar-equippable-prefabs.md) for the full AssetBundle-based workflow.

### AvatarEquippable Configuration

- `assetPath` - Resources path to the registered prefab
- `hand` - `AvatarHand.Left` or `AvatarHand.Right`
- `animationTrigger` - Trigger or bool name for the third-person animation

### Example: Using a Base Game AvatarEquippable

```csharp
using S1API.Items;
using UnityEngine;

var equippable = ItemCreator.CreateEquippableBuilder()
    .CreateViewmodelEquippable("MyCustomKnife")
    .WithInteraction(canInteract: true, canPickup: true)
    .WithViewmodelTransform(
        position: new Vector3(0.2f, -0.15f, 0.3f),
        rotation: Vector3.zero,
        scale: Vector3.one
    )
    .WithAvatarEquippable(
        assetPath: AvatarEquippablePaths.Knife,
        hand: AvatarHand.Right,
        animationTrigger: "RightArm_Hold_ClosedHand"
    )
    .WithUseCallback((itemInstance) =>
    {
        MelonLogger.Msg("Custom knife used!");
    })
    .Build();

var item = ItemCreator.CreateBuilder()
    .WithBasicInfo(
        id: "my_custom_knife",
        name: "Custom Knife",
        description: "A custom knife with base game animations",
        category: ItemCategory.Tools
    )
    .WithEquippable(equippable)
    .Build();
```

## Usable Items

Items can respond to player input while equipped.

### Option 1: Use Callback

For simple use cases, register a callback that runs when the player clicks while holding the item:

```csharp
var equippable = ItemCreator.CreateEquippableBuilder()
    .CreateViewmodelEquippable("UsableItem")
    .WithUseCallback((itemInstance) =>
    {
        MelonLogger.Msg($"Used item: {itemInstance.Definition.Name}");
    })
    .Build();
```

The callback system automatically:

- Checks for `PrimaryClick`
- Ensures the player is not typing
- Ensures no UI is open
- Only triggers while the item is equipped

### Option 2: Custom MonoBehaviour

For more complex behavior, create your own `MonoBehaviour` derived from `Equippable_Viewmodel`:

```csharp
#if MONO
using ScheduleOne.Equipping;
using ScheduleOne.ItemFramework;
using ScheduleOne.PlayerScripts;
using ScheduleOne;
#else
using Il2CppScheduleOne.Equipping;
using Il2CppScheduleOne.ItemFramework;
using Il2CppScheduleOne.PlayerScripts;
using Il2CppScheduleOne;
#endif

public class MyCustomEquippable : Equippable_Viewmodel
{
    protected override void Update()
    {
        base.Update();

        if (GameInput.GetButtonDown(GameInput.ButtonCode.PrimaryClick) &&
            !GameInput.IsTyping &&
            PlayerSingleton<PlayerCamera>.Instance.activeUIElementCount == 0)
        {
            UseItem();
        }
    }

    private void UseItem()
    {
        MelonLogger.Msg("Custom item used!");
    }
}

var equippable = ItemCreator.CreateEquippableBuilder()
    .CreateEquippable<MyCustomEquippable>("MyCustomEquippable")
    .Build();
```

## Custom Equippable Types

You can also target specific game equippable types directly:

```csharp
#if MONO
using ScheduleOne.Equipping;
#else
using Il2CppScheduleOne.Equipping;
#endif

var equippable = ItemCreator.CreateEquippableBuilder()
    .CreateEquippable<Equippable_Viewmodel>("MyViewmodelItem")
    .WithInteraction(canInteract: true, canPickup: false)
    .Build();

var item = ItemCreator.CreateBuilder()
    .WithBasicInfo("my_viewmodel", "Viewmodel Item", "Has viewmodel", ItemCategory.Tools)
    .WithEquippable(equippable)
    .Build();
```

## Complete Example: Simple Usable Item

```csharp
using MelonLoader;
using S1API.Internal.Utils;
using S1API.Items;
using System.Reflection;
using UnityEngine;

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
        Sprite icon = LoadIconFromResources();
        var equippable = CreateCustomEquippable();

        var scratcherTicket = ItemCreator.CreateBuilder()
            .WithBasicInfo(
                id: "scratcher_ticket",
                name: "Scratcher Ticket",
                description: "A lottery ticket that can be scratched to reveal potential prizes.",
                category: ItemCategory.Consumable
            )
            .WithStackLimit(10)
            .WithPricing(5f, 0.1f)
            .WithLegalStatus(LegalStatus.Legal)
            .WithIcon(icon)
            .WithEquippable(equippable)
            .Build();

        MelonLogger.Msg($"Created custom item: {scratcherTicket.Name}");
    }

    private Sprite LoadIconFromResources()
    {
        var assembly = Assembly.GetExecutingAssembly();
        using (var stream = assembly.GetManifestResourceStream("MyMod.Resources.icon.png"))
        {
            if (stream != null)
            {
                var data = new byte[stream.Length];
                stream.Read(data, 0, data.Length);
                return ImageUtils.LoadImageRaw(data);
            }
        }

        return null;
    }

    private Equippable CreateCustomEquippable()
    {
        return ItemCreator.CreateEquippableBuilder()
            .CreateBasicEquippable("ScratcherEquippable")
            .WithInteraction(canInteract: true, canPickup: true)
            .Build();
    }
}
```

## Complete Example: Viewmodel Equippable With Use Callback

```csharp
using MelonLoader;
using S1API.Internal.Utils;
using S1API.Items;
using System.Reflection;
using UnityEngine;

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
        Sprite icon = LoadIconFromResources();

        var equippable = ItemCreator.CreateEquippableBuilder()
            .CreateViewmodelEquippable("MyUsableItem")
            .WithInteraction(canInteract: true, canPickup: true)
            .WithViewmodelTransform(
                position: new Vector3(0.2f, -0.15f, 0.3f),
                rotation: new Vector3(0f, 0f, 0f),
                scale: Vector3.one
            )
            .WithUseCallback((itemInstance) =>
            {
                MelonLogger.Msg($"Used item: {itemInstance.Definition.Name}");
            })
            .Build();

        var myItem = ItemCreator.CreateBuilder()
            .WithBasicInfo(
                id: "my_usable_item",
                name: "Usable Item",
                description: "An item that can be used by clicking while holding it.",
                category: ItemCategory.Tools
            )
            .WithStackLimit(1)
            .WithPricing(50f, 0.5f)
            .WithLegalStatus(LegalStatus.Legal)
            .WithIcon(icon)
            .WithEquippable(equippable)
            .Build();

        MelonLogger.Msg($"Created usable item: {myItem.Name}");
    }

    private Sprite LoadIconFromResources()
    {
        var assembly = Assembly.GetExecutingAssembly();
        using (var stream = assembly.GetManifestResourceStream("MyMod.Resources.icon.png"))
        {
            if (stream != null)
            {
                var data = new byte[stream.Length];
                stream.Read(data, 0, data.Length);
                return ImageUtils.LoadImageRaw(data);
            }
        }

        return null;
    }
}
```

## Complete Example: Custom Equippable With Full Control

```csharp
#if MONO
using ScheduleOne.Equipping;
using ScheduleOne.ItemFramework;
using ScheduleOne.PlayerScripts;
using ScheduleOne;
#else
using Il2CppScheduleOne.Equipping;
using Il2CppScheduleOne.ItemFramework;
using Il2CppScheduleOne.PlayerScripts;
using Il2CppScheduleOne;
#endif

using UnityEngine;

public class MyAdvancedEquippable : Equippable_Viewmodel
{
    public override void Equip(ItemInstance item)
    {
        base.Equip(item);
        MelonLogger.Msg($"Equipped: {item.Name}");
    }

    public override void Unequip()
    {
        MelonLogger.Msg("Unequipped");
        base.Unequip();
    }

    protected override void Update()
    {
        base.Update();

        if (GameInput.GetButtonDown(GameInput.ButtonCode.PrimaryClick) &&
            !GameInput.IsTyping &&
            PlayerSingleton<PlayerCamera>.Instance.activeUIElementCount == 0)
        {
            UseItem();
        }

        if (GameInput.GetButtonDown(GameInput.ButtonCode.SecondaryClick))
        {
            SecondaryAction();
        }
    }

    private void UseItem()
    {
        MelonLogger.Msg("Primary action!");
    }

    private void SecondaryAction()
    {
        MelonLogger.Msg("Secondary action!");
    }
}

var equippable = ItemCreator.CreateEquippableBuilder()
    .CreateEquippable<MyAdvancedEquippable>("MyAdvancedEquippable")
    .WithInteraction(canInteract: true, canPickup: true)
    .Build();
```

## Tips

- Start viewmodel positioning with small offsets and adjust incrementally
- Use callbacks for simple interactions and custom classes for complex animation or coroutine logic
- If you build a custom equippable, always check typing and UI-open state before reacting to input
- Register avatar prefabs before creating items that reference them

## See Also

- [Avatar Equippable Prefabs](avatar-equippable-prefabs.md)
- [Item Icons](item-icons.md)
- [Builder API Reference](item-builder-reference.md)
- <xref:S1API.Items.AvatarEquippablePaths>
- <xref:S1API.Items.AvatarEquippableRegistry>

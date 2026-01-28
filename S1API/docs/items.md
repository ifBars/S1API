# Creating Custom Items

S1API provides two approaches for creating custom items: a flexible **Builder API** and a convenient **Creator API**. Both approaches automatically register items with the game's registry.

> **Note**: All items in Schedule One are "storable items" (StorableItemDefinition). The base ItemDefinition class is never used directly, so you'll always be creating StorableItemDefinition instances.

## Quick Start

### Important: Timing for Item Registration

Items should be registered **after the Main scene loads** to ensure the game's Registry is fully initialized and items persist correctly:

```csharp
using MelonLoader;
using S1API.Items;

public class MyMod : MelonMod
{
    private bool _itemsInitialized = false;

    public override void OnSceneWasLoaded(int buildIndex, string sceneName)
    {
        // Register items when Main scene loads
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

### Simple Item Creation

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

### Builder Pattern

For more control, use the builder pattern:

```csharp
using S1API.Items;
using UnityEngine;

var myItem = ItemCreator.CreateBuilder()
    .WithBasicInfo("my_item", "My Item", "Description", ItemCategory.Consumable)
    .WithStackLimit(10)
    .WithPricing(50f, 0.5f)
    .WithLegalStatus(LegalStatus.Legal)
    .WithLabelColor(Color.green)
    .WithKeywords("custom", "special", "rare")
    .Build();
```

## Creating Runtime Additives

Create **runtime additives** (i.e., `AdditiveDefinition`) via a builder API.

Important notes:
- **Builder-only:** `AdditiveDefinition` is intentionally **read-only** after registration to avoid mid-session mutation issues. Configure effects during build.
- **Timing:** For best results, register additives before save data loads. Prefer `GameLifecycle.OnPreLoad` when possible.

### Example (recommended timing)

```csharp
using MelonLoader;
using S1API.Items;
using S1API.Lifecycle;
using UnityEngine;

public class MyMod : MelonMod
{
    public override void OnSceneWasLoaded(int buildIndex, string sceneName)
    {
        // S1API initializes lifecycle hooks when Main loads
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
            .WithLabelColor(new Color(0.4f, 0.2f, 0.6f, 1f))
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

### Cloning an existing additive

```csharp
// Clone an existing additive definition by ID and tweak the effects
var variant = AdditiveItemCreator.CloneFrom("pgr")
    .WithBasicInfo("mymod_pgr_variant", "PGR Variant", "A tweaked PGR.", ItemCategory.Growing)
    .WithEffects(1.25f, 0.25f, 0.0f)
    .Build();
```

### Allowing additives on Grow Containers

Grow containers have a fixed allowlist of additives (`GrowContainer.AllowedAdditives`). S1API can extend that allowlist globally so mods don’t need to patch `GrowContainer.InitializeGridItem`.

Notes:
- Applies to **all** grow containers.
- Duplicate `AllowAdditive(...)` calls are a no-op.
- If the ID can’t be resolved to an `AdditiveDefinition` at runtime, S1API will **warn once** and **skip** it.

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
            // Allow a runtime additive you registered earlier in OnPreLoad
            GrowContainerAdditives.AllowAdditive("mymod_growth_booster");
        };
    }
}
```

## Related: Stations

Some station APIs are item-adjacent (they reference item definitions), but are documented separately.

See `S1API/docs/stations.md` for Chemistry Station recipe registration.

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

## Adding Icons

### From Embedded Resources

Load sprites from embedded resources in your mod assembly using S1API's `ImageUtils`:

```csharp
using S1API.Internal.Utils;
using System.Reflection;
using UnityEngine;

var assembly = Assembly.GetExecutingAssembly();
using (var stream = assembly.GetManifestResourceStream("YourMod.Resources.my_icon.png"))
{
    if (stream != null)
    {
        var data = new byte[stream.Length];
        stream.Read(data, 0, data.Length);
        
        // Use S1API's ImageUtils to properly load the sprite
        var icon = ImageUtils.LoadImageRaw(data);
        
        if (icon != null)
        {
            var item = ItemCreator.CreateBuilder()
                .WithBasicInfo("my_item", "My Item", "Description", ItemCategory.Tools)
                .WithIcon(icon)
                .Build();
        }
    }
}
```

### From AssetBundle

```csharp
var bundle = AssetBundle.LoadFromFile("path/to/bundle");
var icon = bundle.LoadAsset<Sprite>("my_icon");

var item = ItemCreator.CreateBuilder()
    .WithBasicInfo("my_item", "My Item", "Description", ItemCategory.Tools)
    .WithIcon(icon)
    .Build();
```

## Creating Equippable Items

Equippable items can be held by the player and may have custom behavior. Schedule One supports several types of equippables:

- **Basic Equippable**: Simple items that can be held but have no visual model
- **Viewmodel Equippable**: Items with 3D models visible in first-person (and optionally third-person)
- **Usable Items**: Items that respond to player input (clicking while holding them)

The game doesn't have a unified "usable" system - each equippable handles its own input detection by overriding `Update()` and checking for input. S1API provides two approaches:
1. **Use Callbacks**: Simple callback-based approach for basic use cases
2. **Custom MonoBehaviour**: Full control by extending `Equippable_Viewmodel` yourself

### Basic Equippable

```csharp
// Create the equippable component
var equippable = ItemCreator.CreateEquippableBuilder()
    .CreateBasicEquippable("MyItemEquippable")
    .WithInteraction(canInteract: true, canPickup: true)
    .Build();

// Create the item with the equippable attached
var item = ItemCreator.CreateBuilder()
    .WithBasicInfo("my_equippable", "Equippable Item", "Can be held", ItemCategory.Tools)
    .WithEquippable(equippable)
    .Build();
```

### Viewmodel Equippables (3D Models)

Viewmodel equippables allow items to be held in first-person with a 3D model visible to the player. They can also have third-person animations when other players see you holding the item.

#### Basic Viewmodel Equippable

```csharp
// Create a viewmodel equippable with a 3D model
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

#### Viewmodel Transform Settings

The `WithViewmodelTransform()` method configures how the item appears in first-person:
- **position**: Local position offset (where the item appears relative to camera)
- **rotation**: Local euler angles (how the item is rotated)
- **scale**: Local scale (size of the item, default: Vector3.one)

#### Third-Person Avatar Animations

Configure how the item appears when other players see you holding it. You can either:

1. **Use an existing game AvatarEquippable** (if it exists in the game's Resources)
2. **Create your own AvatarEquippable prefab** and load it from an AssetBundle

**Option 1: Using Existing Game Assets**

Schedule One includes many AvatarEquippable prefabs you can use. S1API provides constants for easy access:

```csharp
using S1API.Items;

var equippable = ItemCreator.CreateEquippableBuilder()
    .CreateViewmodelEquippable("MyItem")
    .WithAvatarEquippable(
        assetPath: AvatarEquippablePaths.Knife,  // Use predefined path constant
        hand: AvatarHand.Right,
        animationTrigger: "RightArm_Hold_ClosedHand"
    )
    .Build();
```

**Available Base Game AvatarEquippable Prefabs:**

S1API provides constants in `AvatarEquippablePaths` for all base game equippables:
- `AvatarEquippablePaths.Baton` - Police baton
- `AvatarEquippablePaths.Beer` - Beer bottle
- `AvatarEquippablePaths.BrokenBottle` - Broken bottle weapon
- `AvatarEquippablePaths.Coffee` - Coffee cup
- `AvatarEquippablePaths.Cuke` - Energy drink (Cuke)
- `AvatarEquippablePaths.Hammer` - Hammer tool
- `AvatarEquippablePaths.Joint` - Marijuana joint
- `AvatarEquippablePaths.Knife` - Knife weapon
- `AvatarEquippablePaths.M1911` - M1911 pistol
- `AvatarEquippablePaths.PhoneLowered` - Phone (lowered position)
- `AvatarEquippablePaths.PhoneRaised` - Phone (raised position)
- `AvatarEquippablePaths.Pipe` - Smoking pipe
- `AvatarEquippablePaths.Revolver` - Revolver pistol
- `AvatarEquippablePaths.Taser` - Taser weapon
- `AvatarEquippablePaths.TrashBag` - Trash bag

You can also use the raw path string if needed:
```csharp
.WithAvatarEquippable(
    assetPath: "avatar/equippables/MyCustomItem",  // Custom path
    hand: AvatarHand.Right,
    animationTrigger: "RightArm_Hold_ClosedHand"
)
```

**Option 2: Creating Your Own AvatarEquippable Prefab**

See the [Creating AvatarEquippable Prefabs](#creating-avatarequippable-prefabs) section below for detailed instructions.

**AvatarEquippable Configuration:**
- **assetPath**: Resources path to the AvatarEquippable prefab (must be registered via `AvatarEquippableRegistry`)
- **hand**: `AvatarHand.Left` or `AvatarHand.Right` (default: Right)
- **animationTrigger**: Name of the animation trigger/bool to play in third-person

### Example: Using Base Game AvatarEquippable

Here's an example creating an item that uses a base game AvatarEquippable prefab:

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
        assetPath: AvatarEquippablePaths.Knife,  // Uses base game knife animation
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

This creates a custom item that uses the base game's knife AvatarEquippable prefab, so other players will see you holding it like a knife in third-person view.

### Usable Items (Use Callbacks)

Items can respond to player input (left-click) when equipped. There are two approaches:

#### Option 1: Use Callback (Simple)

For simple use cases, register a callback that gets called when the player clicks while holding the item:

```csharp
var equippable = ItemCreator.CreateEquippableBuilder()
    .CreateViewmodelEquippable("UsableItem")
    .WithUseCallback((itemInstance) =>
    {
        // This is called when player clicks while holding the item
        MelonLogger.Msg($"Used item: {itemInstance.Definition.Name}");
        
        // Example: Open a UI, consume the item, etc.
        // itemInstance.Quantity -= 1; // Consume one
    })
    .Build();
```

The callback automatically handles input detection with proper conditions:
- Checks for left-click (`PrimaryClick`)
- Ensures player is not typing
- Ensures no UI is open
- Only triggers when item is equipped

#### Option 2: Custom MonoBehaviour (Advanced)

For complex behavior, create your own MonoBehaviour that extends `Equippable_Viewmodel`:

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

// Create your custom equippable class
public class MyCustomEquippable : Equippable_Viewmodel
{
    protected override void Update()
    {
        base.Update();
        
        // Handle input detection
        if (GameInput.GetButtonDown(GameInput.ButtonCode.PrimaryClick) &&
            !GameInput.IsTyping &&
            PlayerSingleton<PlayerCamera>.Instance.activeUIElementCount == 0)
        {
            UseItem();
        }
    }
    
    private void UseItem()
    {
        // Your custom logic here
        MelonLogger.Msg("Custom item used!");
    }
}

// Then use it in the builder
var equippable = ItemCreator.CreateEquippableBuilder()
    .CreateEquippable<MyCustomEquippable>("MyCustomEquippable")
    .Build();
```

This approach gives you full control over input handling, animations, coroutines, and any other custom behavior.

### Custom Equippable Types

For other advanced equippable behavior, reference the game's equippable types:

```csharp
#if MONO
using ScheduleOne.Equipping;
#else
using Il2CppScheduleOne.Equipping;
#endif

// Create an equippable using a specific game type
var equippable = ItemCreator.CreateEquippableBuilder()
    .CreateEquippable<Equippable_Viewmodel>("MyViewmodelItem")
    .WithInteraction(canInteract: true, canPickup: false)
    .Build();

var item = ItemCreator.CreateBuilder()
    .WithBasicInfo("my_viewmodel", "Viewmodel Item", "Has viewmodel", ItemCategory.Tools)
    .WithEquippable(equippable)
    .Build();
```

## Builder API Reference

### StorableItemDefinitionBuilder Methods

- `WithBasicInfo(id, name, description, category)` - Sets core item properties
- `WithStackLimit(limit)` - Sets maximum stack size (1-999)
- `WithIcon(sprite)` - Sets the item icon
- `WithPricing(basePrice, resellMultiplier)` - Configures economic properties
- `WithLegalStatus(status)` - Sets legal or illegal status
- `WithLabelColor(color)` - Sets UI label color
- `WithKeywords(keywords)` - Sets search/filter keywords
- `WithEquippable(equippable)` - Attaches equippable component
- `WithStoredItem(prefab)` - Assigns a custom StoredItem prefab (optional)
- `WithDemoAvailability(available)` - Sets demo availability
- `Build()` - Registers and returns the item

### EquippableBuilder Methods

- `CreateBasicEquippable(name)` - Creates basic equippable
- `CreateEquippable<T>(name)` - Creates typed equippable (for custom MonoBehaviour classes)
- `CreateViewmodelEquippable(name)` - Creates viewmodel equippable with 3D model support
- `WithInteraction(canInteract, canPickup)` - Configures interaction capabilities
- `WithViewmodelTransform(position, rotation, scale)` - Configures first-person viewmodel transform (viewmodel equippables only)
- `WithAvatarEquippable(assetPath, hand, animationTrigger)` - Configures third-person animation (viewmodel equippables only)
- `WithUseCallback(callback)` - Registers callback for when item is used (viewmodel equippables only)
- `Build()` - Finalizes and returns equippable

## Complete Examples

### Example 1: Simple Usable Item

Here's a complete example creating a usable item with a callback:

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
        // Load icon from embedded resources
        Sprite icon = LoadIconFromResources();
        
        // Create equippable component
        var equippable = CreateCustomEquippable();
        
        // Create the item
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
                
                // Use S1API's ImageUtils for proper cross-platform image loading
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

### Example 2: Viewmodel Equippable with Use Callback

Here's an example creating a viewmodel equippable that can be used:

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
        // Load icon from embedded resources
        Sprite icon = LoadIconFromResources();
        
        // Create viewmodel equippable with use callback
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
                // Called when player clicks while holding the item
                MelonLogger.Msg($"Used item: {itemInstance.Definition.Name}");
                
                // Example: Show a message to the player
                // You can open UI, consume items, trigger effects, etc.
            })
            .Build();
        
        // Create the item
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

### Example 3: Custom Equippable with Full Control

For maximum control, create your own equippable class:

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
        
        // Custom input handling
        if (GameInput.GetButtonDown(GameInput.ButtonCode.PrimaryClick) &&
            !GameInput.IsTyping &&
            PlayerSingleton<PlayerCamera>.Instance.activeUIElementCount == 0)
        {
            UseItem();
        }
        
        // Custom secondary input
        if (GameInput.GetButtonDown(GameInput.ButtonCode.SecondaryClick))
        {
            SecondaryAction();
        }
    }
    
    private void UseItem()
    {
        // Your custom logic
        MelonLogger.Msg("Primary action!");
    }
    
    private void SecondaryAction()
    {
        // Your custom logic
        MelonLogger.Msg("Secondary action!");
    }
}

// Then add it to the item registry:
var equippable = ItemCreator.CreateEquippableBuilder()
    .CreateEquippable<MyAdvancedEquippable>("MyAdvancedEquippable")
    .WithInteraction(canInteract: true, canPickup: true)
    .Build();
```

## Creating AvatarEquippable Prefabs

AvatarEquippable prefabs control how items appear in third-person view when other players see you holding them. You can create your own prefabs in Unity and load them via AssetBundle.

### Step 1: Create the Prefab in Unity

1. **Open Unity** with the Schedule One project (or a compatible Unity version)
2. **Create a new GameObject** in your scene
3. **Add the AvatarEquippable component**:
   - Select the GameObject
   - Add Component → Schedule One → Avatar Framework → Equipping → AvatarEquippable
4. **Configure the AvatarEquippable component**:
   - **Hand**: Left or Right (which hand holds the item)
   - **Animation Trigger**: Name of the animation trigger/bool (e.g., "RightArm_Hold_ClosedHand")
   - **Suspiciousness**: 0.0 to 1.0 (how suspicious the item appears)
   - **Trigger Type**: Trigger or Bool (animation type)
5. **Create an Alignment Point**:
   - Create a child GameObject named "AlignmentPoint"
   - Position it where the hand should grip the item
   - Assign it to the AvatarEquippable's **AlignmentPoint** field
6. **Add your 3D model**:
   - Add your item's 3D model as a child of the GameObject
   - Position and rotate it relative to the AlignmentPoint
7. **Set the AssetPath**:
   - Click the "Recalculate Asset Path" button in the AvatarEquippable component (if available)
   - Or manually set it to something like "Equippables/MyItem"
8. **Save as Prefab**:
   - Drag the GameObject to your Project window to create a prefab
   - Name it appropriately (e.g., "MyItem_AvatarEquippable")

### Step 2: Export to AssetBundle

1. **Select your prefab** in the Project window
2. **In the Inspector**, add an AssetBundle label:
   - At the bottom of the Inspector, find "Asset Labels"
   - Click the dropdown next to "AssetBundle"
   - Create a new AssetBundle name (e.g., "myitem_equippables")
   - Or select an existing bundle
3. **Build the AssetBundle**:
   - Use Unity's AssetBundle Browser or a custom build script
   - Ensure the bundle includes your AvatarEquippable prefab
   - Save the bundle file (e.g., `myitem_equippables`)

### Step 3: Embed the AssetBundle in Your Mod

1. **Add the AssetBundle to your mod project**:
   - Place the `.bundle` file in a `Resources` or `Assets` folder in your mod project
2. **Set it as an Embedded Resource**:
   - In Visual Studio/your IDE, select the bundle file
   - Set "Build Action" to "Embedded Resource"
   - The resource name will be `YourModName.Resources.myitem_equippables` (adjust based on folder structure)

### Step 4: Load and Register with S1API

Load the prefab from your AssetBundle and register it with S1API:

```csharp
using S1API.AssetBundles;
using S1API.Items;
using System.Reflection;

public class MyMod : MelonMod
{
    public override void OnSceneWasLoaded(int buildIndex, string sceneName)
    {
        if (sceneName == "Main")
        {
            InitializeAvatarEquippables();
        }
    }

    private void InitializeAvatarEquippables()
    {
        // Load the AssetBundle
        var bundle = AssetLoader.GetAssetBundleFromStream(
            "MyMod.Resources.myitem_equippables", 
            Assembly.GetExecutingAssembly()
        );

        // Load and register the AvatarEquippable prefab
        AvatarEquippableRegistry.LoadAndRegisterFromBundle(
            bundle: bundle,
            prefabName: "MyItem_AvatarEquippable",  // Name of prefab in bundle
            assetPath: "Equippables/MyItem"          // Path to use in WithAvatarEquippable()
        );

        MelonLogger.Msg("Registered AvatarEquippable prefab");
    }
}
```

**Alternative: Load from Embedded Bundle Directly**

```csharp
// This helper method loads from embedded bundle and registers in one call
AvatarEquippableRegistry.LoadAndRegisterFromEmbeddedBundle(
    bundleName: "myitem_equippables",           // Name of embedded bundle resource
    prefabName: "MyItem_AvatarEquippable",      // Name of prefab in bundle
    assetPath: "Equippables/MyItem"             // Path to use in WithAvatarEquippable()
);
```

### Step 5: Use the Registered Prefab

Once registered, use the `assetPath` you provided in `WithAvatarEquippable()`:

```csharp
var equippable = ItemCreator.CreateEquippableBuilder()
    .CreateViewmodelEquippable("MyItem")
    .WithAvatarEquippable(
        assetPath: "Equippables/MyItem",  // Must match the path used in Register
        hand: AvatarHand.Right,
        animationTrigger: "RightArm_Hold_ClosedHand"
    )
    .Build();
```

### Complete Example

Here's a complete example combining all steps:

```csharp
using MelonLoader;
using S1API.AssetBundles;
using S1API.Items;
using System.Reflection;
using UnityEngine;

public class MyMod : MelonMod
{
    private bool _initialized = false;

    public override void OnSceneWasLoaded(int buildIndex, string sceneName)
    {
        if (sceneName == "Main" && !_initialized)
        {
            InitializeMod();
            _initialized = true;
        }
    }

    private void InitializeMod()
    {
        // Step 1: Register AvatarEquippable prefab
        RegisterAvatarEquippable();

        // Step 2: Create equippable with viewmodel and avatar
        var equippable = ItemCreator.CreateEquippableBuilder()
            .CreateViewmodelEquippable("MyCustomItem")
            .WithInteraction(canInteract: true, canPickup: true)
            .WithViewmodelTransform(
                position: new Vector3(0.2f, -0.15f, 0.3f),
                rotation: Vector3.zero,
                scale: Vector3.one
            )
            .WithAvatarEquippable(
                assetPath: "Equippables/MyCustomItem",  // Must match registered path
                hand: AvatarHand.Right,
                animationTrigger: "RightArm_Hold_ClosedHand"
            )
            .WithUseCallback((itemInstance) =>
            {
                MelonLogger.Msg($"Used: {itemInstance.Definition.Name}");
            })
            .Build();

        // Step 3: Create the item
        var item = ItemCreator.CreateBuilder()
            .WithBasicInfo(
                id: "my_custom_item",
                name: "My Custom Item",
                description: "A custom item with viewmodel and third-person animation",
                category: ItemCategory.Tools
            )
            .WithEquippable(equippable)
            .Build();

        MelonLogger.Msg("Created custom item with AvatarEquippable!");
    }

    private void RegisterAvatarEquippable()
    {
        try
        {
            // Load from embedded AssetBundle and register
            bool success = AvatarEquippableRegistry.LoadAndRegisterFromEmbeddedBundle(
                bundleName: "myitem_equippables",
                prefabName: "MyCustomItem_AvatarEquippable",
                assetPath: "Equippables/MyCustomItem",
                assemblyOverride: Assembly.GetExecutingAssembly()
            );

            if (success)
            {
                MelonLogger.Msg("Successfully registered AvatarEquippable prefab");
            }
            else
            {
                MelonLogger.Error("Failed to register AvatarEquippable prefab");
            }
        }
        catch (System.Exception ex)
        {
            MelonLogger.Error($"Exception registering AvatarEquippable: {ex.Message}");
            MelonLogger.Error(ex.StackTrace);
        }
    }
}
```

### Tips for AvatarEquippable Prefabs

- **Alignment Point**: Position the AlignmentPoint where the hand should grip the item. This is crucial for proper positioning
- **Model Scale**: Ensure your 3D model is properly scaled - it should match the size of other equippable items in the game
- **Animation Triggers**: Use existing animation triggers from the game (like "RightArm_Hold_ClosedHand") or create custom ones
- **AssetPath**: The assetPath you register must exactly match what you use in `WithAvatarEquippable()`
- **Testing**: Test in multiplayer to see how the item appears to other players
- **Hand Selection**: Most items use the right hand, but some (like shields) use the left hand

## Advanced: Custom Item Instances

For items with custom state (like the ScratcherTicket with `IsScratched`, `PrizeAmount`), you'll need to:

1. Create a custom `ItemInstance` class inheriting from the game's `StorableItemInstance`
2. Create a custom `ItemData` class for serialization
3. Create a custom `ItemLoader` class for deserialization
4. Override `GetDefaultInstance()` in your custom definition class

## Tips

- **Registration Timing**: Always register items in `OnSceneWasLoaded` when `sceneName == "Main"` to ensure proper persistence
- **Unique IDs**: Always use unique, descriptive IDs for your items (e.g., "mymod_toolname")
- **Icon Size**: Icons should be square and at least 128x128 pixels
- **Stack Limits**: Consider gameplay balance when setting stack limits
- **Pricing**: Balance purchase price and resell multiplier for fair economy
- **Testing**: Test items in both single-player and multiplayer scenarios
- **Scene Changes**: Items registered during `OnLateInitializeMelon()` may be cleared on scene transitions
- **Viewmodel Positioning**: Adjust viewmodel transform values to position items correctly in first-person view. Start with small values and adjust incrementally
- **AvatarEquippable Prefabs**: Use `AvatarEquippableRegistry` to register prefabs loaded from AssetBundles. The registry automatically patches `Resources.Load` to find your registered prefabs
- **AssetBundle Loading**: Always load and register AvatarEquippable prefabs before creating items that use them. Do this in `OnSceneWasLoaded` when the Main scene loads
- **Use Callbacks**: Use callbacks for simple use cases. For complex behavior (animations, coroutines, multiple inputs), create a custom MonoBehaviour
- **Input Detection**: The callback system automatically handles proper input conditions (not typing, no UI open). If creating custom equippables, always check these conditions

## See Also

- <xref:S1API.Items.ItemDefinition> - ItemDefinition API Reference
- <xref:S1API.Items.ItemCreator> - ItemCreator API Reference
- <xref:S1API.Items.StorableItemDefinitionBuilder> - StorableItemDefinitionBuilder API Reference
- <xref:S1API.Items.AdditiveItemCreator> - Additive item creation API
- <xref:S1API.Items.AdditiveDefinitionBuilder> - AdditiveDefinition builder API
- <xref:S1API.Items.AdditiveDefinition> - AdditiveDefinition wrapper API
- <xref:S1API.Items.AvatarEquippableRegistry> - For registering AvatarEquippable prefabs from AssetBundles
- <xref:S1API.Items.AvatarEquippablePaths> - Constants for base game AvatarEquippable prefab paths

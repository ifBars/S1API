# Creating Custom Clothing Items

Clothing content bridges the avatar rendering system, runtime resource registry, and the clothing catalog. This guide focuses on workflows specific to hats, shirts, shoes, and other wearable pieces. The examples below demonstrate cloning the base game cap, swapping its textures, and registering a custom clothing definition.

## Quick Checklist

1. Wait for the `Main` scene before touching registries (`OnSceneWasLoaded`).
2. (Optional) Clone an existing accessory prefab and override its materials/textures via `AccessoryFactory`.
3. Build or clone the clothing definition with `ClothingItemCreator`, pointing `WithClothingAsset` at your custom accessory path.
4. Register icons and pricing just like other items.
5. Add the new clothing item to compatible shops once initialization succeeds.

## Workflow Walkthrough (Custom Cap Example)

### 1. Clone the Accessory and Swap the Texture

Load a PNG from embedded resources and replace the `_MainTex`, `_BaseMap`, and `_Albedo` texture slots before registering the prefab:

```csharp
// Load custom texture from embedded resources
var assembly = Assembly.GetExecutingAssembly();
var customTexture = TextureUtils.LoadTextureFromResource(
    assembly,
    "MyMod.Resources.CustomCap.custom_cap_texture.png");

var textureReplacements = new Dictionary<string, Texture2D>
{
    { "_MainTex", customTexture },
    { "_BaseMap", customTexture },
    { "_Albedo", customTexture }
};

AccessoryFactory.CreateAndRegisterAccessory(
    sourceResourcePath: "avatar/accessories/head/cap/Cap",
    targetResourcePath: "MyMod/Accessories/CustomCap",
    newName: "CustomCap",
    textureReplacements: textureReplacements,
    colorTint: null);
```

Tips:

- Use `TextureUtils.LoadTextureFromResource(assembly, "Namespace.Resources.path.png")` for embeddeds.
- Keep the prefab active after duplication so Unity instantiates an enabled instance.
- Call `RuntimeResourceRegistry.IsRegistered` to assert that the accessory path is available before wiring the clothing definition.

### 2. Build the Clothing Definition

Clone the base `cap` definition and override only what matters - metadata, asset path, pricing, and optional icon:

```csharp
// Load icon from embedded resources (optional)
var assembly = Assembly.GetExecutingAssembly();
var icon = ImageUtils.LoadImageFromResource(
    assembly,
    "MyMod.Resources.CustomCap.icon.png");

var customCap = ClothingItemCreator.CloneFrom("cap")
    .WithBasicInfo(
        id: "custom_cap",
        name: "Custom Cap",
        description: "A custom cap with unique style.")
    .WithClothingAsset("MyMod/Accessories/CustomCap")
    .WithColorable(false)  // Custom textures, not colorable
    .WithDefaultColor(ClothingColor.White)
    .WithPricing(75f, 0.5f)
    .Build();

// Set icon if custom one was loaded
if (icon != null)
{
    customCap.Icon = icon;
}
```

Guidelines:

- `CloneFrom` preserves slot, application type, blocked slots, etc., so you only override differences.
- Use `WithSlot`, `WithApplicationType`, `WithBlockedSlots`, and `WithColorable` when building from scratch.
- Icons can be swapped post-build to avoid re-running the builder when an icon is optional.

### 3. Surface the Item In-Game

After the item is registered, push it to shops:

```csharp
int shopsAdded = ShopManager.AddToCompatibleShops(itemDefinition);
MelonLogger.Msg($"Added to {shopsAdded} shop(s)");
```

Shop injection should happen after the registry confirms your item exists (e.g., immediately after `CreateClothingItem()` succeeds).

## Slots and Application Types

| Slot (`ClothingSlot`) | Examples |
| --- | --- |
| Head | Hats, helmets, caps |
| Eyes | Glasses, sunglasses |
| Neck | Scarves, necklaces |
| Top | Shirts, hoodies |
| Outerwear | Jackets, vests |
| Hands | Gloves |
| Waist | Belts |
| Bottom | Pants, shorts |
| Feet | Shoes, boots |
| Wrist | Watches, bracelets |

| Application Type (`ClothingApplicationType`) | Purpose |
| --- | --- |
| Accessory | 3D meshes (hats, glasses) |
| BodyLayer | Flat textures projected onto the torso/legs |
| FaceLayer | Face decals, makeup, tattoos |

## Testing and Troubleshooting

- **Initialization timing**: Guard the workflow with `if (sceneName == "Main" && !_initialized)` to avoid duplicate registration across scene loads.
- **Resource paths**: Match the string passed to `WithClothingAsset` with the `targetResourcePath` you registered via `AccessoryFactory`.
- **Texture validation**: Log texture dimensions after loading so you catch mis-sized PNGs early.
- **Shop coverage**: `ShopManager.AddToCompatibleShops` returns how many inventories accepted the item—log the count and ensure it is non-zero for your desired vendors.
- **Multiplayer**: Test in both local and networked sessions when changing accessory meshes so replication issues surface early.

## Complete Example

Here's a complete example combining all steps:

```csharp
using MelonLoader;
using S1API.Items;
using S1API.Rendering;
using S1API.Internal.Utils;
using S1API.Shops;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class MyMod : MelonMod
{
    private bool _itemsInitialized = false;

    public override void OnSceneWasLoaded(int buildIndex, string sceneName)
    {
        if (sceneName == "Main" && !_itemsInitialized)
        {
            InitializeCustomClothing();
            _itemsInitialized = true;
        }
    }

    private void InitializeCustomClothing()
    {
        // Step 1: Create and register custom accessory
        var assembly = Assembly.GetExecutingAssembly();
        var customTexture = TextureUtils.LoadTextureFromResource(
            assembly,
            "MyMod.Resources.CustomCap.custom_cap_texture.png");

        var textureReplacements = new Dictionary<string, Texture2D>
        {
            { "_MainTex", customTexture },
            { "_BaseMap", customTexture },
            { "_Albedo", customTexture }
        };

        bool accessoryRegistered = AccessoryFactory.CreateAndRegisterAccessory(
            sourceResourcePath: "avatar/accessories/head/cap/Cap",
            targetResourcePath: "MyMod/Accessories/CustomCap",
            newName: "CustomCap",
            textureReplacements: textureReplacements,
            colorTint: null);

        if (!accessoryRegistered)
        {
            MelonLogger.Error("Failed to register custom accessory");
            return;
        }

        // Step 2: Create clothing item definition
        var icon = ImageUtils.LoadImageFromResource(
            assembly,
            "MyMod.Resources.CustomCap.icon.png");

        var customCap = ClothingItemCreator.CloneFrom("cap")
            .WithBasicInfo(
                id: "custom_cap",
                name: "Custom Cap",
                description: "A custom cap with unique style.")
            .WithClothingAsset("MyMod/Accessories/CustomCap")
            .WithColorable(false)
            .WithDefaultColor(ClothingColor.White)
            .WithPricing(75f, 0.5f)
            .Build();

        if (icon != null)
        {
            customCap.Icon = icon;
        }

        // Step 3: Add to shops
        int shopsAdded = ShopManager.AddToCompatibleShops(customCap);
        MelonLogger.Msg($"Created custom clothing item: {customCap.Name}");
        MelonLogger.Msg($"Added to {shopsAdded} shop(s)");
    }
}
```

## Reference Links

- [`AccessoryFactory`](../Rendering/AccessoryFactory.cs) – helper for duplicating prefabs and swapping materials.
- [`RuntimeResourceRegistry`](../Rendering/RuntimeResourceRegistry.cs) – tracks registered accessory/resource paths.
- [`ClothingItemCreator`](../Items/ClothingItemCreator.cs) – builder API for clothing definitions.


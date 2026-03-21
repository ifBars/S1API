# Avatar Equippable Prefabs

`AvatarEquippable` prefabs control how items appear in third-person view when other players see you holding them. You can create your own prefabs in Unity and load them through an AssetBundle.

## Step 1: Create the Prefab in Unity

1. Open Unity with the Schedule One project or a compatible Unity version.
2. Create a new `GameObject` in your scene.
3. Add the `AvatarEquippable` component.
4. Configure the component:
   - Hand: left or right
   - Animation Trigger: for example `RightArm_Hold_ClosedHand`
   - Suspiciousness: `0.0` to `1.0`
   - Trigger Type: trigger or bool
5. Create a child `GameObject` named `AlignmentPoint`.
6. Position `AlignmentPoint` where the hand should grip the item and assign it to the component.
7. Add your 3D model as a child and align it relative to `AlignmentPoint`.
8. Set the `AssetPath`.
9. Save the object as a prefab.

## Step 2: Export to AssetBundle

1. Select the prefab in the Project window.
2. Assign an AssetBundle label such as `myitem_equippables`.
3. Build the AssetBundle and ensure the prefab is included.

## Step 3: Embed the AssetBundle in Your Mod

1. Add the bundle file to your mod project.
2. Mark it as an embedded resource.
3. Confirm the final resource name, for example `YourModName.Resources.myitem_equippables`.

## Step 4: Load and Register With S1API

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
        var bundle = AssetLoader.GetAssetBundleFromStream(
            "MyMod.Resources.myitem_equippables",
            Assembly.GetExecutingAssembly()
        );

        AvatarEquippableRegistry.LoadAndRegisterFromBundle(
            bundle: bundle,
            prefabName: "MyItem_AvatarEquippable",
            assetPath: "Equippables/MyItem"
        );

        MelonLogger.Msg("Registered AvatarEquippable prefab");
    }
}
```

### Alternative: Load From Embedded Bundle Directly

```csharp
AvatarEquippableRegistry.LoadAndRegisterFromEmbeddedBundle(
    bundleName: "myitem_equippables",
    prefabName: "MyItem_AvatarEquippable",
    assetPath: "Equippables/MyItem"
);
```

## Step 5: Use the Registered Prefab

```csharp
var equippable = ItemCreator.CreateEquippableBuilder()
    .CreateViewmodelEquippable("MyItem")
    .WithAvatarEquippable(
        assetPath: "Equippables/MyItem",
        hand: AvatarHand.Right,
        animationTrigger: "RightArm_Hold_ClosedHand"
    )
    .Build();
```

## Complete Example

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
        RegisterAvatarEquippable();

        var equippable = ItemCreator.CreateEquippableBuilder()
            .CreateViewmodelEquippable("MyCustomItem")
            .WithInteraction(canInteract: true, canPickup: true)
            .WithViewmodelTransform(
                position: new Vector3(0.2f, -0.15f, 0.3f),
                rotation: Vector3.zero,
                scale: Vector3.one
            )
            .WithAvatarEquippable(
                assetPath: "Equippables/MyCustomItem",
                hand: AvatarHand.Right,
                animationTrigger: "RightArm_Hold_ClosedHand"
            )
            .WithUseCallback((itemInstance) =>
            {
                MelonLogger.Msg($"Used: {itemInstance.Definition.Name}");
            })
            .Build();

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

## Tips

- Position `AlignmentPoint` exactly where the hand should grip the item
- Match your model scale to existing equippables in the game
- Reuse existing animation triggers unless you fully control the animation setup
- The registered `assetPath` must exactly match the path used in `WithAvatarEquippable()`
- Test multiplayer presentation, not just first-person view

## See Also

- [Equippable Items](equippable-items.md)
- <xref:S1API.Items.AvatarEquippableRegistry>
- <xref:S1API.Items.AvatarEquippablePaths>

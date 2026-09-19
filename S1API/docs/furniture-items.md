# Custom Furniture

`FurnitureCreator` turns a mod-provided `GameObject` into a complete native buildable item. S1API
creates the placed prefab, placement ghost source, footprint, bounds, collision, culling metadata,
stored-item prefab, equippable reference, definition, and icon while preserving Schedule One's
serialized build handlers, save lifecycle, and multiplayer initialization.

## Grid furniture

The model can come from any loader that returns a Unity `GameObject`. This example uses an embedded
GLB loaded by S1MAPI:

```csharp
using S1API.Items.Buildable;
using S1MAPI.Gltf;
using S1MAPI.Utils;
using UnityEngine;

byte[] glb = EmbeddedResourceLoader.LoadBytes("MyMod.Assets.SofaChair.glb")
    ?? throw new InvalidOperationException("Embedded chair model is missing.");
GameObject model = GltfLoader.LoadGlb(
        glb,
        Shader.Find("Universal Render Pipeline/Lit"))
    ?? throw new InvalidOperationException("Chair GLB could not be loaded.");

var chair = FurnitureCreator.CreateBuilder()
    .WithBasicInfo("my-mod:sofa-chair", "Sofa Chair", "A compact upholstered chair.")
    .WithModel(model)
    .WithPlacement(FurniturePlacementMode.Grid)
    .WithFootprint(2, 2)
    .WithBuildSound(BuildSoundType.Wood)
    .WithPricing(175f, 0.5f)
    .WithStackLimit(4)
    .WithGeneratedIcon()
    .Build();
```

Grid footprint cells are 0.5 metres. Size the footprint to cover the model's horizontal bounds;
for example, a model just under one metre wide and deep uses `WithFootprint(2, 2)`.

## Native furniture variants

Use `CloneFrom` when a variant should reuse an ordinary native furniture model. S1API accepts only
donors whose placed prefab uses the exact native `GridItem` or `SurfaceItem` type. Machines,
stations, storage, toggleable objects, and other specialized subclasses are rejected because a
presentation clone cannot preserve their runtime behavior.

```csharp
var blueClock = FurnitureCreator.CloneFrom("grandfatherclock")
    .WithBasicInfo(
        "my-mod:blue-grandfather-clock",
        "Blue Grandfather Clock",
        "A grandfather clock with a blue finish.")
    .ConfigureModel(model =>
    {
        foreach (Renderer renderer in model.GetComponentsInChildren<Renderer>(true))
        {
            foreach (Material material in renderer.sharedMaterials)
            {
                if (material != null && material.HasProperty("_BaseColor"))
                    material.SetColor("_BaseColor", new Color(0.08f, 0.2f, 0.65f));
                if (material != null && material.HasProperty("_Color"))
                    material.SetColor("_Color", new Color(0.08f, 0.2f, 0.65f));
            }
        }
    })
    .WithPricing(250f)
    .WithGeneratedIcon()
    .Build();
```

`ConfigureModel` runs once against a builder-owned hierarchy. S1API has already replaced every
renderer material with a private instance, so material edits cannot change the donor or other
native furniture. The final placed, stored, ghost, and icon representations also receive separate
material instances.

The clone path preserves the donor's exact grid cells or surface flags, rotation setting, build
sound, price, resale multiplier, stack limit, and icon fallback. Any corresponding builder method
overrides that default. The variant must use a new stable ID; `Build()` rejects the donor ID even if
only its casing differs.

## Placement ghost

Furniture created with `FurnitureCreator` does not need separate ghost setup. `WithModel(model)`
uses the supplied model for the placed object, stored item, generated icon, and placement ghost.
When the native placement system creates a ghost, S1API clones that model into it and prepares the
clone as a non-interactive placement visual.

`WithGhostVisual(...)` belongs to the lower-level `BuildableItemDefinitionBuilder` path. Use it when
cloning a native non-furniture buildable, such as a machine or station, whose inherited ghost should
show a custom model. See [Custom ghosts for cloned buildables](item-builder-reference.md#custom-ghosts-for-cloned-buildables)
for the complete pattern.

Schedule One exposes native cardboard, wood, and metal placement sounds. `BuildSoundType.Plastic`
uses the native metal sound as its compatibility fallback.

## Surface furniture

Use surface placement for wall or roof-mounted decorations:

```csharp
var wallSign = FurnitureCreator.CreateBuilder()
    .WithBasicInfo("my-mod:wall-sign", "Wall Sign", "A placeable wall sign.")
    .WithModel(signModel)
    .WithPlacement(FurniturePlacementMode.Surface)
    .WithSurfacePlacement(FurnitureSurfaceType.Wall, allowRotation: true)
    .WithBuildSound(BuildSoundType.Wood)
    .WithPricing(45f)
    .WithGeneratedIcon()
    .Build();
```

`FurnitureSurfaceType.All` accepts both walls and roofs. `WithFootprint` applies only to grid
furniture.

## Registration and multiplayer

Build furniture after the vanilla item registry is initialized. The builder deliberately fails with
a clear error if its verified native template is not available yet. S1API retains the resulting
definition across scene transitions.

Every multiplayer peer must load the same mod version and register the same stable item ID, model,
placement mode, and footprint. Placement authority, observer initialization, late joins, and
property save/load then travel through the game's native grid or surface item flow.

Native variants also require every peer to register the same donor ID and apply the same
deterministic `ConfigureModel` changes before save restoration or placement. S1API does not send
models or materials over the network.

The generated icon path is the default. Furniture still registers during pre-load with a temporary
fallback icon: the donor icon for native variants or the generic template icon for supplied models.
S1API replaces that icon after the gameplay rendering rig is ready and refreshes bound
inventory/shop UI. Call `WithIcon(sprite)` when an art-directed icon is preferred.

## Placement scope

The initial API supports ordinary floor-grid furniture and wall/roof surface furniture. S1API does
not expose procedural-grid placement yet because the current game provides no generic serialized
procedural template that can be composed without inheriting gameplay-specific behavior. This keeps
the public enum honest and leaves room for an additive placement family later.

For a complete embedded-GLB mod, see the
[FurnitureMod example](https://github.com/ifBars/S1FurnitureMod).

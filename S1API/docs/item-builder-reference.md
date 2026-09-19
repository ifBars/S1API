# Builder API Reference

This page collects the main builder methods, advanced item-instance notes, and item-specific best practices.

## StorableItemDefinitionBuilder Methods

- `WithBasicInfo(id, name, description, category)` - Sets core item properties
- `WithStackLimit(limit)` - Sets maximum stack size (`1`-`999`)
- `WithIcon(sprite)` - Sets the item icon
- `WithPricing(basePrice, resellMultiplier)` - Configures economic properties
- `WithLegalStatus(status)` - Sets legal or illegal status
- `WithEquippable(equippable)` - Attaches an equippable component
- `WithStoredItem(prefab)` - Assigns a custom `StoredItem` prefab
- `WithDemoAvailability(available)` - Sets demo availability
- `Build()` - Registers and returns the item

## EquippableBuilder Methods

- `CreateBasicEquippable(name)` - Creates a basic equippable
- `CreateEquippable<T>(name)` - Creates a typed equippable for custom `MonoBehaviour` classes
- `CreateViewmodelEquippable(name)` - Creates a viewmodel equippable with 3D model support
- `WithInteraction(canInteract, canPickup)` - Configures interaction capabilities
- `WithViewmodelTransform(position, rotation, scale)` - Configures first-person transform
- `WithAvatarEquippable(assetPath, hand, animationTrigger)` - Configures third-person avatar behavior
- `WithUseCallback(callback)` - Registers a callback when the item is used
- `Build()` - Finalizes and returns the equippable

## FurnitureDefinitionBuilder Methods

- `FurnitureCreator.CloneFrom(donor)` - Starts a presentation-only variant from native grid or surface furniture
- `WithBasicInfo(id, name, description)` - Sets the stable ID and player-facing text
- `WithModel(model)` - Supplies the model cloned into all native furniture representations
- `ConfigureModel(callback)` - Modifies the isolated model owned by a `CloneFrom` builder
- `WithPlacement(mode)` - Selects grid or surface placement
- `WithFootprint(width, depth)` - Sets a grid footprint in 0.5 metre tiles
- `WithSurfacePlacement(types, allowRotation)` - Selects wall/roof compatibility
- `WithBuildSound(soundType)` - Selects the native completion sound; plastic furniture uses the metal fallback
- `WithPricing(basePrice, resellMultiplier)` - Configures economic properties
- `WithStackLimit(limit)` - Sets the inventory stack limit
- `WithIcon(sprite)` / `WithGeneratedIcon(resolution)` - Configures the inventory icon
- `Build()` - Composes the native prefabs, registers, and returns the furniture definition

## Custom ghosts for cloned buildables

Use `WithGhostVisual(visualFactory, replaceExistingVisual)` when a buildable cloned from a native
machine or station needs a different placement model. Furniture created through `FurnitureCreator`
does not call this method: `WithModel(...)` automatically supplies its placed, stored, icon, and
ghost visuals.

```csharp
GameObject ghostModel = LoadMachineModel();
ghostModel.SetActive(false);

var machine = BuildableItemCreator.CloneFrom("brickpress")
    .WithBasicInfo(
        "my-mod:tablet-press",
        "Tablet Press",
        "A compact manual tablet press.",
        ItemCategory.Equipment)
    .WithGhostVisual(
        parent => Object.Instantiate(ghostModel, parent, false),
        replaceExistingVisual: true)
    .Build();
```

The factory runs on Unity's main thread whenever the native grid, procedural-grid, or surface
placement system creates a ghost. It must create and return a fresh `GameObject`; do not return the
shared source object. S1API parents the result when necessary, activates it, and disables its
colliders, navigation, networking, canvases, and lights so it behaves as a placement visual.

Set `replaceExistingVisual: true` when the custom visual replaces the cloned native model. Buildables
that do not call this method retain the game's normal ghost behavior. If the factory throws or
returns `null`, S1API removes the partial visual and restores any inherited renderers it hid.

## Advanced: Custom Item Instances

For items with custom runtime state, such as extra fields that must serialize, you will need to:

1. Create a custom `ItemInstance` class inheriting from the game's `StorableItemInstance`.
2. Create a custom `ItemData` class for serialization.
3. Create a custom `ItemLoader` class for deserialization.
4. Override `GetDefaultInstance()` in your custom definition class.

## Best Practices

- Register regular items after `Main` loads and runtime additives before save data loads when possible
- Use callbacks for simple use behavior and custom equippable types for complex flows
- Load and register avatar prefabs before creating items that depend on them
- Always validate icon loading before attaching the sprite to the builder
- Test both Mono and Il2Cpp environments when changing item behavior

## See Also

- [Item Registration & Basics](item-registration-basics.md)
- [Custom Furniture](furniture-items.md)
- [Runtime Additives](runtime-additives.md)
- [Equippable Items](equippable-items.md)
- [Avatar Equippable Prefabs](avatar-equippable-prefabs.md)
- <xref:S1API.Items.StorableItemDefinitionBuilder>
- <xref:S1API.Items.AdditiveDefinitionBuilder>

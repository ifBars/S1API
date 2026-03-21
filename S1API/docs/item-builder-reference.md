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
- [Runtime Additives](runtime-additives.md)
- [Equippable Items](equippable-items.md)
- [Avatar Equippable Prefabs](avatar-equippable-prefabs.md)
- <xref:S1API.Items.StorableItemDefinitionBuilder>
- <xref:S1API.Items.AdditiveDefinitionBuilder>

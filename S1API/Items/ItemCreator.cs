using UnityEngine;

namespace S1API.Items
{
    /// <summary>
    /// Provides convenient static methods for creating custom items.
    /// Use <see cref="CreateBuilder"/> for flexible configuration or <see cref="CreateItem"/> for quick creation.
    /// </summary>
    /// <remarks>
    /// All items in Schedule One are storable items (StorableItemDefinition), so both methods create the same type.
    /// </remarks>
    public static class ItemCreator
    {
        /// <summary>
        /// Creates a new builder for composing an item definition with full flexibility.
        /// Use fluent methods to configure the item, then call Build() to register it.
        /// </summary>
        /// <returns>A new StorableItemDefinitionBuilder instance for fluent configuration.</returns>
        /// <example>
        /// <code>
        /// var item = ItemCreator.CreateBuilder()
        ///     .WithBasicInfo("my_tool", "Custom Tool", "A custom tool", ItemCategory.Tools)
        ///     .WithStackLimit(5)
        ///     .WithPricing(25f, 0.3f)
        ///     .Build();
        /// </code>
        /// </example>
        public static StorableItemDefinitionBuilder CreateBuilder()
        {
            return new StorableItemDefinitionBuilder();
        }

        /// <summary>
        /// Creates an item with common parameters in a single call.
        /// The item is automatically registered with the game's registry.
        /// </summary>
        /// <param name="id">Unique identifier for the item (e.g., "my_custom_tool").</param>
        /// <param name="name">Display name shown in UI.</param>
        /// <param name="description">Item description shown in tooltips.</param>
        /// <param name="category">Item category for inventory organization.</param>
        /// <param name="stackLimit">Maximum quantity per inventory slot (default: 10).</param>
        /// <param name="basePurchasePrice">Base price when buying from shops (default: 10).</param>
        /// <param name="resellMultiplier">Fraction of purchase price recovered when selling (default: 0.5).</param>
        /// <param name="legalStatus">Whether the item is legal or illegal (default: Legal).</param>
        /// <param name="icon">Optional sprite to use as the item icon.</param>
        /// <param name="equippable">Optional equippable component to attach.</param>
        /// <returns>A wrapper around the created item definition.</returns>
        /// <example>
        /// <code>
        /// var item = ItemCreator.CreateItem(
        ///     id: "my_tool",
        ///     name: "Custom Tool",
        ///     description: "A custom tool for crafting",
        ///     category: ItemCategory.Tools,
        ///     stackLimit: 5,
        ///     basePurchasePrice: 25f
        /// );
        /// </code>
        /// </example>
        public static StorableItemDefinition CreateItem(
            string id,
            string name,
            string description,
            ItemCategory category,
            int stackLimit = 10,
            float basePurchasePrice = 10f,
            float resellMultiplier = 0.5f,
            LegalStatus legalStatus = LegalStatus.Legal,
            Sprite icon = null,
            Equippable equippable = null)
        {
            var builder = new StorableItemDefinitionBuilder()
                .WithBasicInfo(id, name, description, category)
                .WithStackLimit(stackLimit)
                .WithPricing(basePurchasePrice, resellMultiplier)
                .WithLegalStatus(legalStatus);

            if (icon != null)
            {
                builder.WithIcon(icon);
            }

            if (equippable != null)
            {
                builder.WithEquippable(equippable);
            }

            return builder.Build();
        }

        /// <summary>
        /// Creates a new equippable builder for creating custom equippable components.
        /// Use this to create equippable behavior that can be attached to items.
        /// </summary>
        /// <returns>A new EquippableBuilder instance.</returns>
        /// <example>
        /// <code>
        /// var equippable = ItemCreator.CreateEquippableBuilder()
        ///     .CreateBasicEquippable("MyEquippable")
        ///     .WithInteraction(canInteract: true, canPickup: true)
        ///     .Build();
        /// </code>
        /// </example>
        public static EquippableBuilder CreateEquippableBuilder()
        {
            return new EquippableBuilder();
        }
    }
}


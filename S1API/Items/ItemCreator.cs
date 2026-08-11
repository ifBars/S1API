using System;
using S1API.Internal.Utils;
using S1API.Leveling;
using UnityEngine;
#if (IL2CPPMELON)
using S1ItemFramework = Il2CppScheduleOne.ItemFramework;
using S1Registry = Il2CppScheduleOne.Registry;
#elif MONOMELON
using S1ItemFramework = ScheduleOne.ItemFramework;
using S1Registry = ScheduleOne.Registry;
#endif

namespace S1API.Items
{
    /// <summary>
    /// Provides convenient static methods for creating custom items.
    /// Use <see cref="CreateBuilder"/> for flexible configuration or
    /// <see cref="CreateItem(string,string,string,ItemCategory,int,float,float,LegalStatus,bool,FullRank?,Sprite?,Equippable?)"/>
    /// for quick creation.
    /// </summary>
    /// <remarks>
    /// All items in Schedule One are storable items (StorableItemDefinition), so both methods create the same type.
    /// </remarks>
    [Obsolete("Use S1API.Items.Storable.ItemCreator instead")]
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
        /// Creates a new storable item builder by cloning an existing item by ID.
        /// </summary>
        /// <param name="sourceItemId">The ID of the item to clone.</param>
        /// <returns>A builder pre-configured with the source item properties.</returns>
        /// <exception cref="ArgumentException">Thrown if the source item ID is not found or is not a storable item.</exception>
        public static StorableItemDefinitionBuilder CloneFrom(string sourceItemId)
        {
            var sourceDefinition = S1Registry.GetItem(sourceItemId);
            if (sourceDefinition == null)
            {
                throw new ArgumentException($"Source item with ID '{sourceItemId}' not found in registry", nameof(sourceItemId));
            }

            if (!CrossType.Is(sourceDefinition, out S1ItemFramework.StorableItemDefinition storableDef))
            {
                throw new ArgumentException($"Item '{sourceItemId}' is not an StorableItemDefinition", nameof(sourceItemId));
            }

            return new StorableItemDefinitionBuilder(storableDef);
        }

        /// <summary>
        /// Creates a new storable item builder by cloning an existing storable item wrapper.
        /// </summary>
        /// <param name="source">The storable item definition to clone.</param>
        /// <returns>A builder pre-configured with the source item properties.</returns>
        /// <exception cref="ArgumentNullException">Thrown if the source definition is null.</exception>
        public static StorableItemDefinitionBuilder CloneFrom(StorableItemDefinition source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source), "Source storable item definition cannot be null");
            }

            return new StorableItemDefinitionBuilder(source.S1StorableItemDefinition);
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
        /// <param name="requiresLevelToPurchase">Whether purchasing the item requires a certain player rank (default: false).</param>
        /// <param name="requiredRank">The player rank required to purchase the item, if applicable (default: null).</param>
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
            bool requiresLevelToPurchase = false,
            FullRank? requiredRank = null,
            Sprite? icon = null,
            Equippable? equippable = null)
        {
            var builder = new StorableItemDefinitionBuilder()
                .WithBasicInfo(id, name, description, category)
                .WithStackLimit(stackLimit)
                .WithPricing(basePurchasePrice, resellMultiplier)
                .WithRequiredRank(requiresLevelToPurchase ? requiredRank : null)
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
        /// Creates an item using the parameter order from S1API 3.0.0 through 3.0.4, without a rank requirement.
        /// Provided for source compatibility with nine-argument positional calls.
        /// </summary>
        /// <param name="id">Unique identifier for the item (e.g., "my_custom_tool").</param>
        /// <param name="name">Display name shown in UI.</param>
        /// <param name="description">Item description shown in tooltips.</param>
        /// <param name="category">Item category for inventory organization.</param>
        /// <param name="stackLimit">Maximum quantity per inventory slot.</param>
        /// <param name="basePurchasePrice">Base price when buying from shops.</param>
        /// <param name="resellMultiplier">Fraction of purchase price recovered when selling.</param>
        /// <param name="legalStatus">Whether the item is legal or illegal.</param>
        /// <param name="legacyIcon">Optional sprite to use as the item icon.</param>
        /// <returns>A wrapper around the created item definition.</returns>
        /// <remarks>
        /// The distinct parameter name keeps calls that target the current overload with <c>icon:</c> unambiguous.
        /// </remarks>
        [Obsolete("Use the overload with requiresLevelToPurchase and requiredRank. This compatibility overload may be removed in a future S1API version.")]
        public static StorableItemDefinition CreateItem(
            string id,
            string name,
            string description,
            ItemCategory category,
            int stackLimit,
            float basePurchasePrice,
            float resellMultiplier,
            LegalStatus legalStatus,
            Sprite? legacyIcon) =>
            CreateItem(id, name, description, category, stackLimit, basePurchasePrice,
                resellMultiplier, legalStatus, requiresLevelToPurchase: false, requiredRank: null,
                icon: legacyIcon, equippable: null);

        /// <summary>
        /// Creates an item using the parameter order from S1API 3.0.0 through 3.0.4, without a rank requirement.
        /// Provided for binary compatibility with mods compiled against those versions.
        /// </summary>
        /// <param name="id">Unique identifier for the item (e.g., "my_custom_tool").</param>
        /// <param name="name">Display name shown in UI.</param>
        /// <param name="description">Item description shown in tooltips.</param>
        /// <param name="category">Item category for inventory organization.</param>
        /// <param name="stackLimit">Maximum quantity per inventory slot.</param>
        /// <param name="basePurchasePrice">Base price when buying from shops.</param>
        /// <param name="resellMultiplier">Fraction of purchase price recovered when selling.</param>
        /// <param name="legalStatus">Whether the item is legal or illegal.</param>
        /// <param name="icon">Optional sprite to use as the item icon.</param>
        /// <param name="equippable">Optional equippable component to attach.</param>
        /// <returns>A wrapper around the created item definition.</returns>
        /// <remarks>
        /// S1API 3.0.5 inserted <c>requiresLevelToPurchase</c> and <c>requiredRank</c> before <c>icon</c>.
        /// This overload retains the former ten-parameter binary signature. Its parameters remain required
        /// so current calls that use the named <c>icon</c> parameter continue to bind unambiguously.
        /// </remarks>
        [Obsolete("Use the overload with requiresLevelToPurchase and requiredRank. This compatibility overload may be removed in a future S1API version.")]
        public static StorableItemDefinition CreateItem(
            string id,
            string name,
            string description,
            ItemCategory category,
            int stackLimit,
            float basePurchasePrice,
            float resellMultiplier,
            LegalStatus legalStatus,
            Sprite? icon,
            Equippable? equippable) =>
            CreateItem(id, name, description, category, stackLimit, basePurchasePrice,
                resellMultiplier, legalStatus, requiresLevelToPurchase: false, requiredRank: null,
                icon: icon, equippable: equippable);

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


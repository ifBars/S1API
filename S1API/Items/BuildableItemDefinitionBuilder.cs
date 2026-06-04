#if (IL2CPPMELON)
using S1ItemFramework = Il2CppScheduleOne.ItemFramework;
using S1CoreItemFramework = Il2CppScheduleOne.Core.Items.Framework;
using S1Registry = Il2CppScheduleOne.Registry;
#elif (MONOMELON || MONOBEPINEX || IL2CPPBEPINEX)
using S1ItemFramework = ScheduleOne.ItemFramework;
using S1CoreItemFramework = ScheduleOne.Core.Items.Framework;
using S1Registry = ScheduleOne.Registry;
#endif

using System;
using UnityEngine;

namespace S1API.Items
{
    /// <summary>
    /// Builder for composing buildable item definitions at runtime.
    /// Use fluent methods to configure buildable item properties before calling <see cref="Build"/>.
    /// </summary>
    [Obsolete("Use S1API.Items.Buildable.BuildableItemDefinitionBuilder instead")]
    public sealed class BuildableItemDefinitionBuilder
    {
        private readonly S1ItemFramework.BuildableItemDefinition _definition;

        /// <summary>
        /// INTERNAL: Creates a new builder instance with a fresh BuildableItemDefinition.
        /// Only BuildableItemCreator can instantiate this.
        /// </summary>
        internal BuildableItemDefinitionBuilder()
        {
            _definition = ScriptableObject.CreateInstance<S1ItemFramework.BuildableItemDefinition>();

            // Set defaults
            _definition.StackLimit = 10;
            _definition.BasePurchasePrice = 10f;
            _definition.ResellMultiplier = 0.5f;
            _definition.Category = S1CoreItemFramework.EItemCategory.Furniture;
            _definition.legalStatus = S1CoreItemFramework.ELegalStatus.Legal;
            _definition.AvailableInDemo = true;
            _definition.UsableInFilters = true;
            _definition.BuildSoundType = S1ItemFramework.BuildableItemDefinition.EBuildSoundType.Wood;
        }

        /// <summary>
        /// INTERNAL: Creates a builder instance initialized by cloning an existing item.
        /// </summary>
        internal BuildableItemDefinitionBuilder(S1ItemFramework.BuildableItemDefinition source)
        {
            _definition = ScriptableObject.CreateInstance<S1ItemFramework.BuildableItemDefinition>();
            CopyPropertiesFrom(source);
        }

        /// <summary>
        /// Copies all properties from a source BuildableItemDefinition to the current definition.
        /// </summary>
        private void CopyPropertiesFrom(S1ItemFramework.BuildableItemDefinition source)
        {
            // Basic ItemDefinition properties
            _definition.Name = source.Name;
            _definition.Description = source.Description;
            _definition.Category = source.Category;
            _definition.StackLimit = source.StackLimit;
            _definition.AvailableInDemo = source.AvailableInDemo;
            _definition.UsableInFilters = source.UsableInFilters;
            _definition.Icon = source.Icon;
            _definition.legalStatus = source.legalStatus;
            _definition.PickpocketDifficultyMultiplier = source.PickpocketDifficultyMultiplier;
            _definition.CombatUtility = source.CombatUtility;

            // StorableItemDefinition properties
            _definition.BasePurchasePrice = source.BasePurchasePrice;
            _definition.ResellMultiplier = source.ResellMultiplier;
            _definition.ShopCategories = source.ShopCategories;
            _definition.RequiresLevelToPurchase = source.RequiresLevelToPurchase;
            _definition.RequiredRank = source.RequiredRank;

            // BuildableItemDefinition properties
            _definition.BuildSoundType = source.BuildSoundType;
            _definition.BuiltItem = source.BuiltItem;
            _definition.StoredItem = source.StoredItem;
            _definition.Equippable = source.Equippable;
        }

        /// <summary>
        /// Sets the basic information for the buildable item.
        /// </summary>
        /// <param name="id">Unique identifier for the item (e.g., "my_custom_rack").</param>
        /// <param name="name">Display name shown in UI.</param>
        /// <param name="description">Item description shown in tooltips.</param>
        /// <returns>The builder instance for fluent chaining.</returns>
        public BuildableItemDefinitionBuilder WithBasicInfo(string id, string name, string description)
        {
            _definition.ID = id;
            _definition.Name = name;
            _definition.Description = description;

            // Update the underlying ScriptableObject name for clarity
            if (!string.IsNullOrEmpty(name))
            {
                _definition.name = name;
            }

            return this;
        }

        /// <summary>
        /// Sets the sound type played when this item is built.
        /// </summary>
        /// <param name="soundType">The build sound type.</param>
        /// <returns>The builder instance for fluent chaining.</returns>
        public BuildableItemDefinitionBuilder WithBuildSound(BuildSoundType soundType)
        {
            _definition.BuildSoundType = (S1ItemFramework.BuildableItemDefinition.EBuildSoundType)soundType;
            return this;
        }

        /// <summary>
        /// Sets the icon sprite displayed for this item in UI.
        /// </summary>
        /// <param name="icon">The sprite to use as the item icon.</param>
        /// <returns>The builder instance for fluent chaining.</returns>
        public BuildableItemDefinitionBuilder WithIcon(Sprite icon)
        {
            _definition.Icon = icon;
            return this;
        }

        /// <summary>
        /// Configures the economic properties of the item.
        /// </summary>
        /// <param name="basePurchasePrice">Base price when buying from shops.</param>
        /// <param name="resellMultiplier">Fraction of purchase price recovered when selling (0.0 to 1.0).</param>
        /// <returns>The builder instance for fluent chaining.</returns>
        public BuildableItemDefinitionBuilder WithPricing(float basePurchasePrice, float resellMultiplier = 0.5f)
        {
            _definition.BasePurchasePrice = Mathf.Max(0f, basePurchasePrice);
            _definition.ResellMultiplier = Mathf.Clamp01(resellMultiplier);
            return this;
        }

        /// <summary>
        /// Sets the category for inventory organization.
        /// </summary>
        /// <param name="category">The item category.</param>
        /// <returns>The builder instance for fluent chaining.</returns>
        public BuildableItemDefinitionBuilder WithCategory(ItemCategory category)
        {
            _definition.Category = (S1CoreItemFramework.EItemCategory)category;
            return this;
        }

        /// <summary>
        /// Sets the maximum stack size for this item.
        /// </summary>
        /// <param name="limit">Maximum quantity per inventory slot (1-999).</param>
        /// <returns>The builder instance for fluent chaining.</returns>
        public BuildableItemDefinitionBuilder WithStackLimit(int limit)
        {
            _definition.StackLimit = Mathf.Clamp(limit, 1, 999);
            return this;
        }

        /// <summary>
        /// Sets the legal status of the item.
        /// </summary>
        /// <param name="status">Whether the item is legal or illegal.</param>
        /// <returns>The builder instance for fluent chaining.</returns>
        public BuildableItemDefinitionBuilder WithLegalStatus(LegalStatus status)
        {
            _definition.legalStatus = (S1CoreItemFramework.ELegalStatus)status;
            return this;
        }

        /// <summary>
        /// Attaches an equippable component to this item, allowing it to be equipped by the player.
        /// </summary>
        /// <param name="equippable">The equippable wrapper to attach.</param>
        /// <returns>The builder instance for fluent chaining.</returns>
        public BuildableItemDefinitionBuilder WithEquippable(Equippable equippable)
        {
            if (equippable != null)
            {
                _definition.Equippable = equippable.S1Equippable;
            }
            return this;
        }

        /// <summary>
        /// Builds the buildable item definition, registers it with the game's registry, and returns a wrapper.
        /// </summary>
        /// <returns>A wrapper around the created buildable item definition.</returns>
        public BuildableItemDefinition Build()
        {
            // Register with the game's registry
            S1Registry.Instance.AddToRegistry(_definition);

            // Return wrapper
            return new BuildableItemDefinition(_definition);
        }

        /// <summary>
        /// INTERNAL: Builds and returns the raw game item definition without registering.
        /// Used internally by S1API. Modders should use <see cref="Build"/> instead.
        /// </summary>
        internal S1ItemFramework.BuildableItemDefinition BuildInternal() =>
            _definition;
    }
}

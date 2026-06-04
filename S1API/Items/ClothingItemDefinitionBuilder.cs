#if (IL2CPPMELON)
using S1Clothing = Il2CppScheduleOne.Clothing;
using S1ItemFramework = Il2CppScheduleOne.ItemFramework;
using S1CoreItemFramework = Il2CppScheduleOne.Core.Items.Framework;
using S1Registry = Il2CppScheduleOne.Registry;
using S1UiItems = Il2CppScheduleOne.UI.Items;
using Il2CppCollections = Il2CppSystem.Collections.Generic;
#elif (MONOMELON || MONOBEPINEX || IL2CPPBEPINEX)
using S1Clothing = ScheduleOne.Clothing;
using S1ItemFramework = ScheduleOne.ItemFramework;
using S1CoreItemFramework = ScheduleOne.Core.Items.Framework;
using S1Registry = ScheduleOne.Registry;
using S1UiItems = ScheduleOne.UI.Items;
using Il2CppCollections = System.Collections.Generic;
#endif

using System;
using System.Collections.Generic;
using S1API.Internal.Utils;
using S1API.Logging;
using UnityEngine;

namespace S1API.Items
{
    /// <summary>
    /// Builder for composing clothing item definitions at runtime.
    /// Use fluent methods to configure clothing properties before calling <see cref="Build"/>.
    /// </summary>
    [Obsolete("Use S1API.Items.Clothing.ClothingItemDefinitionBuilder instead")]
    public sealed class ClothingItemDefinitionBuilder
    {
        private static readonly Log Logger = new Log("ClothingItemDefinitionBuilder");
        private static readonly HashSet<string> WarnedMissingNativeClothingItemUiReasons = new HashSet<string>();
        private static readonly object WarnedMissingNativeClothingItemUiLock = new object();
        private static S1UiItems.ItemUI? s_cachedNativeCustomItemUI;

        private readonly S1Clothing.ClothingDefinition _definition;

        /// <summary>
        /// INTERNAL: Creates a new builder instance with a fresh ClothingDefinition.
        /// </summary>
        internal ClothingItemDefinitionBuilder()
        {
            _definition = ScriptableObject.CreateInstance<S1Clothing.ClothingDefinition>();
            
            // Set defaults
            _definition.StackLimit = 1;
            _definition.BasePurchasePrice = 10f;
            _definition.ResellMultiplier = 0.5f;
            _definition.Category = S1CoreItemFramework.EItemCategory.Clothing;
            _definition.legalStatus = S1CoreItemFramework.ELegalStatus.Legal;
            _definition.AvailableInDemo = true;
            _definition.UsableInFilters = true;

            // Clothing-specific defaults
            _definition.Slot = S1Clothing.EClothingSlot.Head;
            _definition.ApplicationType = S1Clothing.EClothingApplicationType.Accessory;
            _definition.ClothingAssetPath = "Path/To/Clothing/Asset";
            _definition.Colorable = true;
            _definition.DefaultColor = S1Clothing.EClothingColor.White;
#if (IL2CPPMELON)
            _definition.SlotsToBlock = new Il2CppCollections.List<S1Clothing.EClothingSlot>();
#else
            _definition.SlotsToBlock = new List<S1Clothing.EClothingSlot>();
#endif
        }

        /// <summary>
        /// INTERNAL: Creates a builder from an existing clothing definition (for cloning).
        /// </summary>
        internal ClothingItemDefinitionBuilder(S1Clothing.ClothingDefinition source)
        {
            if (source == null)
            {
                throw new System.ArgumentNullException(nameof(source));
            }

            _definition = ScriptableObject.CreateInstance<S1Clothing.ClothingDefinition>();
            
            // Copy all StorableItemDefinition properties
            _definition.ID = source.ID;
            _definition.Name = source.Name;
            _definition.Description = source.Description;
            _definition.Icon = source.Icon;
            _definition.StackLimit = source.StackLimit;
            _definition.BasePurchasePrice = source.BasePurchasePrice;
            _definition.ResellMultiplier = source.ResellMultiplier;
            _definition.Category = source.Category;
            _definition.legalStatus = source.legalStatus;
            _definition.AvailableInDemo = source.AvailableInDemo;
            _definition.UsableInFilters = source.UsableInFilters;
            _definition.StoredItem = source.StoredItem;
            _definition.Equippable = source.Equippable;
            _definition.CustomItemUI = source.CustomItemUI;
            
            // Copy clothing-specific properties
            _definition.Slot = source.Slot;
            _definition.ApplicationType = source.ApplicationType;
            _definition.ClothingAssetPath = source.ClothingAssetPath;
            _definition.Colorable = source.Colorable;
            _definition.DefaultColor = source.DefaultColor;
#if (IL2CPPMELON)
            _definition.SlotsToBlock = new Il2CppCollections.List<S1Clothing.EClothingSlot>();
            if (source.SlotsToBlock != null)
            {
                foreach (var slot in source.SlotsToBlock)
                {
                    _definition.SlotsToBlock.Add(slot);
                }
            }
#else
            _definition.SlotsToBlock = source.SlotsToBlock == null
                ? new List<S1Clothing.EClothingSlot>()
                : new List<S1Clothing.EClothingSlot>(source.SlotsToBlock);
#endif
        }

        /// <summary>
        /// Sets the basic information for the clothing item.
        /// </summary>
        /// <param name="id">Unique identifier for the item (e.g., "my_custom_hat").</param>
        /// <param name="name">Display name shown in UI.</param>
        /// <param name="description">Item description shown in tooltips.</param>
        /// <returns>The builder instance for fluent chaining.</returns>
        public ClothingItemDefinitionBuilder WithBasicInfo(string id, string name, string description)
        {
            _definition.ID = id;
            _definition.Name = name;
            _definition.Description = description;
            _definition.name = string.IsNullOrEmpty(name) ? id : name;
            return this;
        }

        /// <summary>
        /// Sets the clothing slot this item occupies.
        /// </summary>
        /// <param name="slot">The clothing slot.</param>
        /// <returns>The builder instance for fluent chaining.</returns>
        public ClothingItemDefinitionBuilder WithSlot(ClothingSlot slot)
        {
            _definition.Slot = (S1Clothing.EClothingSlot)slot;
            return this;
        }

        /// <summary>
        /// Sets how this clothing item is applied to the avatar.
        /// </summary>
        /// <param name="applicationType">The application type.</param>
        /// <returns>The builder instance for fluent chaining.</returns>
        public ClothingItemDefinitionBuilder WithApplicationType(ClothingApplicationType applicationType)
        {
            _definition.ApplicationType = (S1Clothing.EClothingApplicationType)applicationType;
            return this;
        }

        /// <summary>
        /// Sets the asset path to the clothing prefab or layer.
        /// </summary>
        /// <param name="assetPath">Resources path to the clothing asset.</param>
        /// <returns>The builder instance for fluent chaining.</returns>
        public ClothingItemDefinitionBuilder WithClothingAsset(string assetPath)
        {
            _definition.ClothingAssetPath = assetPath;
            return this;
        }

        /// <summary>
        /// Sets whether this clothing item can be colored.
        /// </summary>
        /// <param name="colorable">True if colorable, false otherwise.</param>
        /// <returns>The builder instance for fluent chaining.</returns>
        public ClothingItemDefinitionBuilder WithColorable(bool colorable)
        {
            _definition.Colorable = colorable;
            return this;
        }

        /// <summary>
        /// Sets the default color for this clothing item.
        /// </summary>
        /// <param name="color">The default color.</param>
        /// <returns>The builder instance for fluent chaining.</returns>
        public ClothingItemDefinitionBuilder WithDefaultColor(ClothingColor color)
        {
            _definition.DefaultColor = (S1Clothing.EClothingColor)color;
            return this;
        }

        /// <summary>
        /// Sets the list of clothing slots this item blocks when equipped.
        /// </summary>
        /// <param name="slots">Array of slots to block.</param>
        /// <returns>The builder instance for fluent chaining.</returns>
        public ClothingItemDefinitionBuilder WithBlockedSlots(params ClothingSlot[] slots)
        {
#if (IL2CPPMELON)
            _definition.SlotsToBlock = new Il2CppCollections.List<S1Clothing.EClothingSlot>();
            foreach (var slot in slots)
            {
                _definition.SlotsToBlock.Add((S1Clothing.EClothingSlot)slot);
            }
#else
            _definition.SlotsToBlock = new List<S1Clothing.EClothingSlot>();
            foreach (var slot in slots)
            {
                _definition.SlotsToBlock.Add((S1Clothing.EClothingSlot)slot);
            }
#endif
            return this;
        }

        /// <summary>
        /// Sets the icon sprite displayed for this item in UI.
        /// </summary>
        /// <param name="icon">The sprite to use as the item icon.</param>
        /// <returns>The builder instance for fluent chaining.</returns>
        public ClothingItemDefinitionBuilder WithIcon(Sprite icon)
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
        public ClothingItemDefinitionBuilder WithPricing(float basePurchasePrice, float resellMultiplier = 0.5f)
        {
            _definition.BasePurchasePrice = Mathf.Max(0f, basePurchasePrice);
            _definition.ResellMultiplier = Mathf.Clamp01(resellMultiplier);
            return this;
        }

        /// <summary>
        /// Builds the clothing item definition, registers it with the game's registry, and returns a wrapper.
        /// </summary>
        /// <returns>A wrapper around the created clothing item definition.</returns>
        public ClothingItemDefinition Build()
        {
            if (string.IsNullOrWhiteSpace(_definition.ID))
                throw new ArgumentException("Item ID cannot be null, empty, or whitespace.", nameof(_definition.ID));

            EnsureNativeClothingItemUi();

            // Register with the game's registry
            S1Registry.Instance.AddToRegistry(_definition);

            // Return wrapper
            return new ClothingItemDefinition(_definition);
        }

        /// <summary>
        /// INTERNAL: Builds and returns the raw game item definition without registering.
        /// Used internally by S1API.
        /// </summary>
        internal S1Clothing.ClothingDefinition BuildInternal()
        {
            EnsureNativeClothingItemUi();

            return _definition;
        }

        private void EnsureNativeClothingItemUi()
        {
            if (_definition.CustomItemUI != null)
            {
                return;
            }

            if (s_cachedNativeCustomItemUI != null)
            {
                _definition.CustomItemUI = s_cachedNativeCustomItemUI;
                return;
            }

            if (S1Registry.Instance == null)
            {
                WarnMissingNativeClothingItemUi("S1Registry.Instance is null");
                return;
            }

            var allItems = S1Registry.Instance.GetAllItems();
            if (allItems == null)
            {
                WarnMissingNativeClothingItemUi("S1Registry.Instance.GetAllItems() returned null");
                return;
            }

            foreach (var item in allItems)
            {
                if (item == null ||
                    !CrossType.Is(item, out S1Clothing.ClothingDefinition clothingDefinition))
                {
                    continue;
                }

                var customItemUI = clothingDefinition.CustomItemUI;
                if (customItemUI == null)
                {
                    continue;
                }

                // CustomItemUI is a native UI template. Share the existing template instead of
                // cloning it here; listing state is bound per item by the game, and cloning
                // Unity/Il2Cpp UI objects is riskier across runtimes.
                s_cachedNativeCustomItemUI = customItemUI;
                _definition.CustomItemUI = customItemUI;
                return;
            }

            WarnMissingNativeClothingItemUi("no S1Clothing.ClothingDefinition with S1Clothing.ClothingDefinition.CustomItemUI was found");
        }

        private static void WarnMissingNativeClothingItemUi(string reason)
        {
            if (string.IsNullOrWhiteSpace(reason))
            {
                return;
            }

            bool shouldWarn;
            lock (WarnedMissingNativeClothingItemUiLock)
            {
                shouldWarn = WarnedMissingNativeClothingItemUiReasons.Add(reason);
            }

            if (shouldWarn)
            {
                Logger.Warning($"Could not borrow a native clothing CustomItemUI template ({reason}). Custom clothing inventory UI may be incomplete. This usually means Build() was called before any native clothing registered.");
            }
        }
    }
}


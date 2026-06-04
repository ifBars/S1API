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
#endif
using System.Collections.Generic;
using S1API.Internal.Utils;
using S1API.Items.Storable;
using S1API.Logging;
using UnityEngine;

namespace S1API.Items.Clothing
{
    /// <summary>
    /// Builder for composing clothing item definitions at runtime.
    /// Use fluent methods to configure clothing properties before calling <see cref="Build"/>.
    /// </summary>
    public class ClothingItemDefinitionBuilder
        : StorableItemDefinitionBuilderBase<ClothingItemDefinitionBuilder>
    {
        private S1Clothing.ClothingDefinition ClothingDefinition =>
            CrossType.As<S1Clothing.ClothingDefinition>(Definition);

        private static readonly Log Logger = new Log("ClothingItemDefinitionBuilder");
        private static readonly HashSet<string> WarnedMissingNativeClothingItemUiReasons = new HashSet<string>();
        private static readonly object WarnedMissingNativeClothingItemUiLock = new object();
        private static S1UiItems.ItemUI? s_cachedNativeCustomItemUI;

        /// <summary>
        /// INTERNAL: Creates a new builder instance with a fresh ClothingDefinition.
        /// </summary>
        internal ClothingItemDefinitionBuilder()
            : base(ScriptableObject.CreateInstance<S1Clothing.ClothingDefinition>())
        {
            Definition.Category = S1CoreItemFramework.EItemCategory.Clothing;

            // Clothing-specific defaults
            ClothingDefinition.Slot = S1Clothing.EClothingSlot.Head;
            ClothingDefinition.ApplicationType = S1Clothing.EClothingApplicationType.Accessory;
            ClothingDefinition.ClothingAssetPath = "Path/To/Clothing/Asset";
            ClothingDefinition.Colorable = true;
            ClothingDefinition.DefaultColor = S1Clothing.EClothingColor.White;
#if (IL2CPPMELON)
            ClothingDefinition.SlotsToBlock = new Il2CppCollections.List<S1Clothing.EClothingSlot>();
#else
            ClothingDefinition.SlotsToBlock = new List<S1Clothing.EClothingSlot>();
#endif
        }

        /// <summary>
        /// INTERNAL: Creates a builder from an existing clothing definition (for cloning).
        /// </summary>
        internal ClothingItemDefinitionBuilder(S1Clothing.ClothingDefinition source)
            : base(source, ScriptableObject.CreateInstance<S1Clothing.ClothingDefinition>)
        {
        }

        /// <inheritdoc/>
        protected override void CopyPropertiesFrom(
            S1ItemFramework.StorableItemDefinition source)
        {
            base.CopyPropertiesFrom(source);

            var clothingSource = CrossType.As<S1Clothing.ClothingDefinition>(source);

            ClothingDefinition.Slot = clothingSource.Slot;
            ClothingDefinition.ApplicationType = clothingSource.ApplicationType;
            ClothingDefinition.ClothingAssetPath = clothingSource.ClothingAssetPath;
            ClothingDefinition.Colorable = clothingSource.Colorable;
            ClothingDefinition.DefaultColor = clothingSource.DefaultColor;
#if (IL2CPPMELON)
            ClothingDefinition.SlotsToBlock = new Il2CppCollections.List<S1Clothing.EClothingSlot>();
            if (clothingSource.SlotsToBlock != null)
            {
                foreach (var slot in clothingSource.SlotsToBlock)
                {
                    ClothingDefinition.SlotsToBlock.Add(slot);
                }
            }
#else
            ClothingDefinition.SlotsToBlock = clothingSource.SlotsToBlock == null
                ? new List<S1Clothing.EClothingSlot>()
                : new List<S1Clothing.EClothingSlot>(clothingSource.SlotsToBlock);
#endif
        }

        /// <summary>
        /// Sets the clothing slot this item occupies.
        /// </summary>
        /// <param name="slot">The clothing slot.</param>
        /// <returns>The builder instance for fluent chaining.</returns>
        public ClothingItemDefinitionBuilder WithSlot(Clothing.ClothingSlot slot)
        {
            ClothingDefinition.Slot = (S1Clothing.EClothingSlot)slot;
            return this;
        }

        /// <summary>
        /// Sets how this clothing item is applied to the avatar.
        /// </summary>
        /// <param name="applicationType">The application type.</param>
        /// <returns>The builder instance for fluent chaining.</returns>
        public ClothingItemDefinitionBuilder WithApplicationType(Clothing.ClothingApplicationType applicationType)
        {
            ClothingDefinition.ApplicationType = (S1Clothing.EClothingApplicationType)applicationType;
            return this;
        }

        /// <summary>
        /// Sets the asset path to the clothing prefab or layer.
        /// </summary>
        /// <param name="assetPath">Resources path to the clothing asset.</param>
        /// <returns>The builder instance for fluent chaining.</returns>
        public ClothingItemDefinitionBuilder WithClothingAsset(string assetPath)
        {
            ClothingDefinition.ClothingAssetPath = assetPath;
            return this;
        }

        /// <summary>
        /// Sets whether this clothing item can be colored.
        /// </summary>
        /// <param name="colorable">True if colorable, false otherwise.</param>
        /// <returns>The builder instance for fluent chaining.</returns>
        public ClothingItemDefinitionBuilder WithColorable(bool colorable)
        {
            ClothingDefinition.Colorable = colorable;
            return this;
        }

        /// <summary>
        /// Sets the default color for this clothing item.
        /// </summary>
        /// <param name="color">The default color.</param>
        /// <returns>The builder instance for fluent chaining.</returns>
        public ClothingItemDefinitionBuilder WithDefaultColor(Clothing.ClothingColor color)
        {
            ClothingDefinition.DefaultColor = (S1Clothing.EClothingColor)color;
            return this;
        }

        /// <summary>
        /// Sets the list of clothing slots this item blocks when equipped.
        /// </summary>
        /// <param name="slots">Array of slots to block.</param>
        /// <returns>The builder instance for fluent chaining.</returns>
        public ClothingItemDefinitionBuilder WithBlockedSlots(params Clothing.ClothingColor[] slots)
        {
#if (IL2CPPMELON)
            ClothingDefinition.SlotsToBlock = new Il2CppCollections.List<S1Clothing.EClothingSlot>();
            foreach (var slot in slots)
            {
                ClothingDefinition.SlotsToBlock.Add((S1Clothing.EClothingSlot)slot);
            }
#else
            ClothingDefinition.SlotsToBlock = new List<S1Clothing.EClothingSlot>();
            foreach (var slot in slots)
            {
                ClothingDefinition.SlotsToBlock.Add((S1Clothing.EClothingSlot)slot);
            }
#endif
            return this;
        }
    
        /// <summary>
        /// Builds the item definition, registers it with the game's registry, and returns a wrapper.
        /// </summary>
        /// <returns>A wrapper around the created clothing item definition.</returns>
        public new ClothingItemDefinition Build()
        {
            EnsureNativeClothingItemUi();
            return (ClothingItemDefinition)base.Build();
        }

        /// <summary>
        /// INTERNAL: Builds and returns the raw game item definition without registering.
        /// Used internally by S1API. Modders should use <see cref="Build"/> instead.
        /// </summary>
        internal new S1Clothing.ClothingDefinition BuildInternal()
        {
            EnsureNativeClothingItemUi();
            return ClothingDefinition;
        }

        /// <inheritdoc />
        protected override Storable.StorableItemDefinition CreateWrapper(
            S1ItemFramework.StorableItemDefinition definition)
        {
            return new ClothingItemDefinition(ClothingDefinition);
        }


        private void EnsureNativeClothingItemUi()
        {
            if (ClothingDefinition.CustomItemUI != null)
            {
                return;
            }

            if (s_cachedNativeCustomItemUI != null)
            {
                ClothingDefinition.CustomItemUI = s_cachedNativeCustomItemUI;
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
                ClothingDefinition.CustomItemUI = customItemUI;
                return;
            }

            WarnMissingNativeClothingItemUi(
                "no S1Clothing.ClothingDefinition with S1Clothing.ClothingDefinition.CustomItemUI was found");
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
                Logger.Warning(
                    $"Could not borrow a native clothing CustomItemUI template ({reason}). Custom clothing inventory UI may be incomplete. This usually means Build() was called before any native clothing registered.");
            }
        }
    }
}
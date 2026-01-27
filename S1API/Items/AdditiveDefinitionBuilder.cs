#if (IL2CPPMELON)
using S1ItemFramework = Il2CppScheduleOne.ItemFramework;
using S1Registry = Il2CppScheduleOne.Registry;
using S1Storage = Il2CppScheduleOne.Storage;
#elif (MONOMELON || MONOBEPINEX || IL2CPPBEPINEX)
using S1ItemFramework = ScheduleOne.ItemFramework;
using S1Registry = ScheduleOne.Registry;
using S1Storage = ScheduleOne.Storage;
#endif

using System;
using S1API.Internal.Utils;
using S1API.Logging;
using UnityEngine;
using Object = UnityEngine.Object;

namespace S1API.Items
{
    /// <summary>
    /// Builder for composing additive definitions at runtime.
    /// Use fluent methods to configure additive properties before calling <see cref="Build"/>.
    /// </summary>
    public sealed class AdditiveDefinitionBuilder
    {
        private static readonly Log Logger = new Log("AdditiveDefinitionBuilder");

        private readonly S1ItemFramework.AdditiveDefinition _definition;
        private readonly GameObject _storedItemPlaceholder;

        /// <summary>
        /// INTERNAL: Creates a new builder instance with a fresh AdditiveDefinition.
        /// Only <see cref="AdditiveItemCreator"/> can instantiate this.
        /// </summary>
        internal AdditiveDefinitionBuilder()
        {
            _definition = ScriptableObject.CreateInstance<S1ItemFramework.AdditiveDefinition>();

            // Defaults align with other builders; users are expected to call WithBasicInfo before Build().
            _definition.StackLimit = 10;
            _definition.BasePurchasePrice = 10f;
            _definition.ResellMultiplier = 0.5f;
            _definition.Category = S1ItemFramework.EItemCategory.Agriculture;
            _definition.legalStatus = S1ItemFramework.ELegalStatus.Legal;
            _definition.AvailableInDemo = true;
            _definition.UsableInFilters = true;
            _definition.LabelDisplayColor = Color.white;

            // Provide a minimal StoredItem placeholder so the field is never null in tooling/inspectors.
            _storedItemPlaceholder = new GameObject("S1API_DefaultStoredItem");
            _storedItemPlaceholder.SetActive(false);
            _storedItemPlaceholder.hideFlags = HideFlags.HideAndDontSave;
            Object.DontDestroyOnLoad(_storedItemPlaceholder);
            var storedItemComponent = _storedItemPlaceholder.AddComponent<S1Storage.StoredItem>();
            _definition.StoredItem = storedItemComponent;
        }

        /// <summary>
        /// INTERNAL: Creates a builder instance initialized by cloning an existing additive.
        /// </summary>
        internal AdditiveDefinitionBuilder(S1ItemFramework.AdditiveDefinition source)
        {
            _definition = ScriptableObject.CreateInstance<S1ItemFramework.AdditiveDefinition>();

            // Placeholder to keep parity with other builders; overridden by copy if source has StoredItem.
            _storedItemPlaceholder = new GameObject("S1API_DefaultStoredItem");
            _storedItemPlaceholder.SetActive(false);
            _storedItemPlaceholder.hideFlags = HideFlags.HideAndDontSave;
            Object.DontDestroyOnLoad(_storedItemPlaceholder);
            var storedItemComponent = _storedItemPlaceholder.AddComponent<S1Storage.StoredItem>();
            _definition.StoredItem = storedItemComponent;

            CopyPropertiesFrom(source);
        }

        private void CopyPropertiesFrom(S1ItemFramework.AdditiveDefinition source)
        {
            if (source == null)
                return;

            // Basic ItemDefinition properties
            _definition.Name = source.Name;
            _definition.Description = source.Description;
            _definition.Category = source.Category;
            _definition.StackLimit = source.StackLimit;
            _definition.Keywords = source.Keywords;
            _definition.AvailableInDemo = source.AvailableInDemo;
            _definition.UsableInFilters = source.UsableInFilters;
            _definition.LabelDisplayColor = source.LabelDisplayColor;
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
            _definition.StoredItem = source.StoredItem != null ? source.StoredItem : _definition.StoredItem;
            _definition.StationItem = source.StationItem;
            _definition.Equippable = source.Equippable;

            // AdditiveDefinition properties (auto-properties with private set in Mono)
            AutoPropertySetter.TrySet(_definition, nameof(S1ItemFramework.AdditiveDefinition.DisplayMaterial), source.DisplayMaterial);
            AutoPropertySetter.TrySet(_definition, nameof(S1ItemFramework.AdditiveDefinition.QualityChange), source.QualityChange);
            AutoPropertySetter.TrySet(_definition, nameof(S1ItemFramework.AdditiveDefinition.YieldMultiplier), source.YieldMultiplier);
            AutoPropertySetter.TrySet(_definition, nameof(S1ItemFramework.AdditiveDefinition.InstantGrowth), source.InstantGrowth);
        }

        /// <summary>
        /// Sets the basic information for the additive.
        /// </summary>
        public AdditiveDefinitionBuilder WithBasicInfo(string id, string name, string description, ItemCategory category)
        {
            _definition.ID = id;
            _definition.Name = name;
            _definition.Description = description;
            _definition.Category = (S1ItemFramework.EItemCategory)category;

            var displayName = string.IsNullOrEmpty(name) ? id : name;
            if (!string.IsNullOrEmpty(displayName))
            {
                _definition.name = displayName;
                if (_storedItemPlaceholder != null)
                {
                    _storedItemPlaceholder.name = $"{displayName}_StoredItem";
                }
            }

            return this;
        }

        /// <summary>
        /// Sets the maximum stack size for this additive.
        /// </summary>
        public AdditiveDefinitionBuilder WithStackLimit(int limit)
        {
            _definition.StackLimit = Mathf.Clamp(limit, 1, 999);
            return this;
        }

        /// <summary>
        /// Sets the icon sprite displayed for this additive in UI.
        /// </summary>
        public AdditiveDefinitionBuilder WithIcon(Sprite icon)
        {
            _definition.Icon = icon;
            return this;
        }

        /// <summary>
        /// Configures the economic properties of the additive.
        /// </summary>
        public AdditiveDefinitionBuilder WithPricing(float basePurchasePrice, float resellMultiplier = 0.5f)
        {
            _definition.BasePurchasePrice = Mathf.Max(0f, basePurchasePrice);
            _definition.ResellMultiplier = Mathf.Clamp01(resellMultiplier);
            return this;
        }

        /// <summary>
        /// Sets the legal status of the additive.
        /// </summary>
        public AdditiveDefinitionBuilder WithLegalStatus(LegalStatus status)
        {
            _definition.legalStatus = (S1ItemFramework.ELegalStatus)status;
            return this;
        }

        /// <summary>
        /// Sets the color of the label displayed in UI.
        /// </summary>
        public AdditiveDefinitionBuilder WithLabelColor(Color color)
        {
            _definition.LabelDisplayColor = color;
            return this;
        }

        /// <summary>
        /// Sets keywords used for filtering and searching this additive.
        /// </summary>
        public AdditiveDefinitionBuilder WithKeywords(params string[] keywords)
        {
            _definition.Keywords = keywords;
            return this;
        }

        /// <summary>
        /// Sets whether this additive is available in the demo version of the game.
        /// </summary>
        public AdditiveDefinitionBuilder WithDemoAvailability(bool available)
        {
            _definition.AvailableInDemo = available;
            return this;
        }

        /// <summary>
        /// Sets the display material for this additive.
        /// </summary>
        public AdditiveDefinitionBuilder WithDisplayMaterial(Material material)
        {
            if (!AutoPropertySetter.TrySet(_definition, nameof(S1ItemFramework.AdditiveDefinition.DisplayMaterial), material))
            {
                Logger.Warning($"Failed to set DisplayMaterial on AdditiveDefinition '{_definition.ID ?? "<no id>"}'.");
            }
            return this;
        }

        /// <summary>
        /// Sets the effect values for this additive.
        /// </summary>
        public AdditiveDefinitionBuilder WithEffects(float yieldMultiplier, float instantGrowth, float qualityChange)
        {
            if (!AutoPropertySetter.TrySet(_definition, nameof(S1ItemFramework.AdditiveDefinition.YieldMultiplier), yieldMultiplier))
            {
                Logger.Warning($"Failed to set YieldMultiplier on AdditiveDefinition '{_definition.ID ?? "<no id>"}'.");
            }
            if (!AutoPropertySetter.TrySet(_definition, nameof(S1ItemFramework.AdditiveDefinition.InstantGrowth), instantGrowth))
            {
                Logger.Warning($"Failed to set InstantGrowth on AdditiveDefinition '{_definition.ID ?? "<no id>"}'.");
            }
            if (!AutoPropertySetter.TrySet(_definition, nameof(S1ItemFramework.AdditiveDefinition.QualityChange), qualityChange))
            {
                Logger.Warning($"Failed to set QualityChange on AdditiveDefinition '{_definition.ID ?? "<no id>"}'.");
            }
            return this;
        }

        /// <summary>
        /// Builds the additive definition, registers it with the game's registry, and returns a wrapper.
        /// </summary>
        public AdditiveDefinition Build()
        {
            if (string.IsNullOrWhiteSpace(_definition.ID))
            {
                throw new InvalidOperationException("AdditiveDefinitionBuilder requires WithBasicInfo(...) to be called before Build().");
            }

            if (!string.IsNullOrEmpty(_definition.Name) && _storedItemPlaceholder != null)
            {
                _storedItemPlaceholder.name = $"{_definition.Name}_StoredItem";
            }

            S1Registry registry;
            try
            {
                registry = S1Registry.Instance;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    "ScheduleOne.Registry is not available yet. Register additives during S1API.Lifecycle.GameLifecycle.OnPreLoad.",
                    ex);
            }

            if (registry == null)
            {
                throw new InvalidOperationException(
                    "ScheduleOne.Registry is not available yet. Register additives during S1API.Lifecycle.GameLifecycle.OnPreLoad.");
            }

            registry.AddToRegistry(_definition);
            return new AdditiveDefinition(_definition);
        }

        /// <summary>
        /// INTERNAL: Builds and returns the raw game additive definition without registering.
        /// Used internally by S1API. Modders should use <see cref="Build"/> instead.
        /// </summary>
        internal S1ItemFramework.AdditiveDefinition BuildInternal()
        {
            return _definition;
        }
    }
}

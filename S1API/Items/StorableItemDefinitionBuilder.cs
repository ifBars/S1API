#if (IL2CPPMELON)
using S1ItemFramework = Il2CppScheduleOne.ItemFramework;
using S1Registry = Il2CppScheduleOne.Registry;
using S1Storage = Il2CppScheduleOne.Storage;
#elif (MONOMELON || MONOBEPINEX || IL2CPPBEPINEX)
using S1ItemFramework = ScheduleOne.ItemFramework;
using S1Registry = ScheduleOne.Registry;
using S1Storage = ScheduleOne.Storage;
#endif

using UnityEngine;
using Object = UnityEngine.Object;

namespace S1API.Items
{
    /// <summary>
    /// Builder for composing item definitions at runtime.
    /// Use fluent methods to configure item properties before calling <see cref="Build"/>.
    /// </summary>
    /// <remarks>
    /// All items in Schedule One are StorableItemDefinition (or subclasses thereof).
    /// The base ItemDefinition class is never used directly in the game.
    /// </remarks>
    public sealed class StorableItemDefinitionBuilder
    {
        private readonly S1ItemFramework.StorableItemDefinition _definition;
        private readonly GameObject _storedItemPlaceholder;
        private bool _hasCustomStoredItem;

        /// <summary>
        /// INTERNAL: Creates a new builder instance with a fresh StorableItemDefinition.
        /// Only ItemCreator can instantiate this.
        /// </summary>
        internal StorableItemDefinitionBuilder()
        {
            _definition = ScriptableObject.CreateInstance<S1ItemFramework.StorableItemDefinition>();
            
            // Set defaults
            _definition.StackLimit = 10;
            _definition.BasePurchasePrice = 10f;
            _definition.ResellMultiplier = 0.5f;
            _definition.Category = S1ItemFramework.EItemCategory.Tools;
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
        /// Sets the basic information for the item.
        /// </summary>
        /// <param name="id">Unique identifier for the item (e.g., "my_custom_tool").</param>
        /// <param name="name">Display name shown in UI.</param>
        /// <param name="description">Item description shown in tooltips.</param>
        /// <param name="category">Item category for inventory organization.</param>
        /// <returns>The builder instance for fluent chaining.</returns>
        public StorableItemDefinitionBuilder WithBasicInfo(string id, string name, string description, ItemCategory category)
        {
            _definition.ID = id;
            _definition.Name = name;
            _definition.Description = description;
            _definition.Category = (S1ItemFramework.EItemCategory)category;

            // Update the underlying ScriptableObject name for clarity in inspectors/debuggers.
            var displayName = string.IsNullOrEmpty(name) ? id : name;
            if (!string.IsNullOrEmpty(displayName))
            {
                _definition.name = displayName;
                if (_storedItemPlaceholder != null && !_hasCustomStoredItem)
                {
                    _storedItemPlaceholder.name = $"{displayName}_StoredItem";
                }
            }
            return this;
        }

        /// <summary>
        /// Sets the maximum stack size for this item.
        /// </summary>
        /// <param name="limit">Maximum quantity per inventory slot (1-999).</param>
        /// <returns>The builder instance for fluent chaining.</returns>
        public StorableItemDefinitionBuilder WithStackLimit(int limit)
        {
            _definition.StackLimit = Mathf.Clamp(limit, 1, 999);
            return this;
        }

        /// <summary>
        /// Sets the icon sprite displayed for this item in UI.
        /// </summary>
        /// <param name="icon">The sprite to use as the item icon.</param>
        /// <returns>The builder instance for fluent chaining.</returns>
        public StorableItemDefinitionBuilder WithIcon(Sprite icon)
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
        public StorableItemDefinitionBuilder WithPricing(float basePurchasePrice, float resellMultiplier = 0.5f)
        {
            _definition.BasePurchasePrice = Mathf.Max(0f, basePurchasePrice);
            _definition.ResellMultiplier = Mathf.Clamp01(resellMultiplier);
            return this;
        }

        /// <summary>
        /// Sets the legal status of the item.
        /// </summary>
        /// <param name="status">Whether the item is legal or illegal.</param>
        /// <returns>The builder instance for fluent chaining.</returns>
        public StorableItemDefinitionBuilder WithLegalStatus(LegalStatus status)
        {
            _definition.legalStatus = (S1ItemFramework.ELegalStatus)status;
            return this;
        }

        /// <summary>
        /// Sets the color of the label displayed in UI.
        /// </summary>
        /// <param name="color">The color to use for the item label.</param>
        /// <returns>The builder instance for fluent chaining.</returns>
        public StorableItemDefinitionBuilder WithLabelColor(Color color)
        {
            _definition.LabelDisplayColor = color;
            return this;
        }

        /// <summary>
        /// Sets keywords used for filtering and searching this item.
        /// </summary>
        /// <param name="keywords">Array of keywords.</param>
        /// <returns>The builder instance for fluent chaining.</returns>
        public StorableItemDefinitionBuilder WithKeywords(params string[] keywords)
        {
            _definition.Keywords = keywords;
            return this;
        }

        /// <summary>
        /// Attaches an equippable component to this item, allowing it to be equipped by the player.
        /// </summary>
        /// <param name="equippable">The equippable wrapper to attach.</param>
        /// <returns>The builder instance for fluent chaining.</returns>
        public StorableItemDefinitionBuilder WithEquippable(Equippable equippable)
        {
            if (equippable != null)
            {
                _definition.Equippable = equippable.S1Equippable;
            }
            return this;
        }

        /// <summary>
        /// Assigns a custom StoredItem prefab for this definition.
        /// </summary>
        /// <param name="storedItemPrefab">Prefab containing a StoredItem component.</param>
        /// <returns>The builder instance for fluent chaining.</returns>
        public StorableItemDefinitionBuilder WithStoredItem(GameObject storedItemPrefab)
        {
            if (storedItemPrefab == null)
                return this;

            var storedItem = storedItemPrefab.GetComponent<S1Storage.StoredItem>() ?? storedItemPrefab.AddComponent<S1Storage.StoredItem>();
            _definition.StoredItem = storedItem;
            _hasCustomStoredItem = true;
            return this;
        }

        /// <summary>
        /// Sets whether this item is available in the demo version of the game.
        /// </summary>
        /// <param name="available">True if available in demo, false otherwise.</param>
        /// <returns>The builder instance for fluent chaining.</returns>
        public StorableItemDefinitionBuilder WithDemoAvailability(bool available)
        {
            _definition.AvailableInDemo = available;
            return this;
        }

        /// <summary>
        /// Builds the item definition, registers it with the game's registry, and returns a wrapper.
        /// </summary>
        /// <returns>A wrapper around the created storable item definition.</returns>
        public StorableItemDefinition Build()
        {
            if (!_hasCustomStoredItem && _definition.StoredItem != null)
            {
                // Ensure placeholder naming stays in sync after late changes.
                if (!string.IsNullOrEmpty(_definition.Name) && _storedItemPlaceholder != null)
                {
                    _storedItemPlaceholder.name = $"{_definition.Name}_StoredItem";
                }
            }

            // Register with the game's registry
            S1Registry.Instance.AddToRegistry(_definition);

            // Return wrapper
            return new StorableItemDefinition(_definition);
        }

        /// <summary>
        /// INTERNAL: Builds and returns the raw game item definition without registering.
        /// Used internally by S1API. Modders should use <see cref="Build"/> instead.
        /// </summary>
        internal S1ItemFramework.StorableItemDefinition BuildInternal()
        {
            return _definition;
        }
    }
}


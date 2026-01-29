#if (IL2CPPMELON)
using S1ItemFramework = Il2CppScheduleOne.ItemFramework;
using S1Registry = Il2CppScheduleOne.Registry;
using S1StationFramework = Il2CppScheduleOne.StationFramework;
using S1Storage = Il2CppScheduleOne.Storage;
#elif (MONOMELON || MONOBEPINEX || IL2CPPBEPINEX)
using S1ItemFramework = ScheduleOne.ItemFramework;
using S1Registry = ScheduleOne.Registry;
using S1StationFramework = ScheduleOne.StationFramework;
using S1Storage = ScheduleOne.Storage;
#endif

using System;
using System.Collections.Generic;
using S1API.Logging;
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
        private static readonly Log Logger = new Log("StorableItemDefinitionBuilder");
        private static readonly object StationItemGate = new object();
        private static readonly Dictionary<int, S1StationFramework.StationItem> StationItemCache = new Dictionary<int, S1StationFramework.StationItem>();
        private static readonly HashSet<int> WarnedStationItemModuleMissing = new HashSet<int>();
        private static GameObject _stationItemRoot;

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
        /// Assigns a StationItem prefab to this item definition so it can be used as a station/minigame ingredient
        /// (e.g., Chemistry Station).
        /// </summary>
        /// <remarks>
        /// S1API clones and caches the prefab under a hidden <c>DontDestroyOnLoad</c> root by default.
        /// This avoids mutating shared prefabs and helps keep the reference stable across scene loads.
        /// </remarks>
        /// <param name="stationItemPrefab">A prefab GameObject that has a StationItem component.</param>
        /// <returns>The builder instance for fluent chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="stationItemPrefab"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown if <paramref name="stationItemPrefab"/> does not have a StationItem component.</exception>
        public StorableItemDefinitionBuilder WithStationItem(GameObject stationItemPrefab)
        {
            if (stationItemPrefab == null)
                throw new ArgumentNullException(nameof(stationItemPrefab));

            var stationItem = stationItemPrefab.GetComponent<S1StationFramework.StationItem>();
            if (stationItem == null)
                throw new ArgumentException("Station item prefab must have a StationItem component.", nameof(stationItemPrefab));

            var cached = GetOrCreateStationItemPrefab(stationItem);
            _definition.StationItem = cached;

            WarnIfStationItemMissingChemistryModules(cached);
            return this;
        }

        /// <summary>
        /// Clears the StationItem reference for this definition.
        /// </summary>
        public StorableItemDefinitionBuilder WithoutStationItem()
        {
            _definition.StationItem = null;
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

        private static S1StationFramework.StationItem GetOrCreateStationItemPrefab(S1StationFramework.StationItem stationItemPrefab)
        {
            var id = stationItemPrefab.GetInstanceID();

            lock (StationItemGate)
            {
                if (StationItemCache.TryGetValue(id, out var cached) && cached != null)
                    return cached;

                var root = GetStationItemRoot();

                // Clone + cache (final decision): keep a stable hidden prefab reference across scene loads.
                var clone = Object.Instantiate(stationItemPrefab, root.transform);
                clone.gameObject.hideFlags = HideFlags.HideAndDontSave;
                clone.name = $"{stationItemPrefab.name}_S1API_StationItem";

                // Keep the cache far away from gameplay so it doesn't interfere with scenes.
                clone.transform.position = root.transform.position;

                StationItemCache[id] = clone;
                return clone;
            }
        }

        private static GameObject GetStationItemRoot()
        {
            if (_stationItemRoot != null)
                return _stationItemRoot;

            lock (StationItemGate)
            {
                if (_stationItemRoot != null)
                    return _stationItemRoot;

                var root = new GameObject("S1API_StationItemCache");
                root.hideFlags = HideFlags.HideAndDontSave;
                Object.DontDestroyOnLoad(root);

                // Place it far below the world; keep it active so instantiated prefabs remain active by default.
                root.transform.position = new Vector3(0f, -10000f, 0f);

                _stationItemRoot = root;
                return root;
            }
        }

        private static void WarnIfStationItemMissingChemistryModules(S1StationFramework.StationItem stationItemPrefab)
        {
            if (stationItemPrefab == null)
                return;

            var id = stationItemPrefab.GetInstanceID();

            lock (StationItemGate)
            {
                if (!WarnedStationItemModuleMissing.Add(id))
                    return;
            }

            try
            {
                var hasIngredientModule = stationItemPrefab.GetComponentInChildren<S1StationFramework.IngredientModule>(true) != null;
                var hasPourableModule = stationItemPrefab.GetComponentInChildren<S1StationFramework.PourableModule>(true) != null;

                if (hasIngredientModule || hasPourableModule)
                    return;

                Logger.Warning(
                    $"[S1API] StationItem prefab '{stationItemPrefab.name}' does not contain an IngredientModule or PourableModule. " +
                    "Chemistry station tasks may log errors or skip this ingredient at runtime.");
            }
            catch
            {
                // best-effort warning only
            }
        }
    }
}

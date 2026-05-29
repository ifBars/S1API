#if (IL2CPPMELON)
using S1ItemFramework = Il2CppScheduleOne.ItemFramework;
using S1CoreItemFramework = Il2CppScheduleOne.Core.Items.Framework;
using S1Levelling = Il2CppScheduleOne.Levelling;
using S1Registry = Il2CppScheduleOne.Registry;
using S1StationFramework = Il2CppScheduleOne.StationFramework;
using S1Storage = Il2CppScheduleOne.Storage;
#elif (MONOMELON || MONOBEPINEX || IL2CPPBEPINEX)
using S1ItemFramework = ScheduleOne.ItemFramework;
using S1CoreItemFramework = ScheduleOne.Core.Items.Framework;
using S1Levelling = ScheduleOne.Levelling;
using S1Registry = ScheduleOne.Registry;
using S1StationFramework = ScheduleOne.StationFramework;
using S1Storage = ScheduleOne.Storage;
#endif
using System;
using System.Collections.Generic;
using S1API.Logging;
using UnityEngine;
using Object = UnityEngine.Object;

namespace S1API.Items.ItemBuilders
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
        : StorableItemDefinitionBuilder<StorableItemDefinitionBuilder>
    {
        /// <inheritdoc/>
        internal StorableItemDefinitionBuilder(
            S1ItemFramework.StorableItemDefinition source)
            : base(source)
        {
        }

        /// <inheritdoc/>
        internal StorableItemDefinitionBuilder()
            : base()
        {
        }
        
        /// <summary>
        /// Builds the item definition, registers it with the game's registry, and returns a wrapper.
        /// </summary>
        /// <returns>A wrapper around the created storable item definition.</returns>
        public new StorableItemDefinition Build()
        {
            return base.Build();
        }
    }

    /// <summary>
    /// State class for managing shared resources and caches used by <see cref="StorableItemDefinitionBuilder{TSelf}"/>.
    /// </summary>
    internal static class StorableItemDefinitionBuilderState
    {
        internal static readonly Log Logger = new Log("StorableItemDefinitionBuilder");
        internal static readonly object StationItemGate = new object();

        internal static readonly Dictionary<int, S1StationFramework.StationItem> StationItemCache =
            new Dictionary<int, S1StationFramework.StationItem>();

        internal static readonly HashSet<int> WarnedStationItemModuleMissing = new HashSet<int>();
        internal static GameObject _stationItemRoot;
    }

    /// <summary>
    /// Generic base builder for composing item definitions at runtime, with fluent methods returning the correct subclass type.
    /// </summary>
    /// <typeparam name="TSelf">The concrete builder type being implemented (e.g., StorableItemDefinitionBuilder).</typeparam>
    public abstract class StorableItemDefinitionBuilder<TSelf>
        where TSelf : StorableItemDefinitionBuilder<TSelf>
    {
        private static Log Logger => StorableItemDefinitionBuilderState.Logger;
        private static object StationItemGate => StorableItemDefinitionBuilderState.StationItemGate;

        private static Dictionary<int, S1StationFramework.StationItem> StationItemCache =>
            StorableItemDefinitionBuilderState.StationItemCache;

        private static HashSet<int> WarnedStationItemModuleMissing =>
            StorableItemDefinitionBuilderState.WarnedStationItemModuleMissing;

        private static GameObject StationItemRoot
        {
            get => StorableItemDefinitionBuilderState._stationItemRoot;
            set  => StorableItemDefinitionBuilderState._stationItemRoot = value;
        }

        /// <summary>
        /// INTERNAL: The underlying game item definition being composed by this builder.
        /// </summary>
        protected readonly S1ItemFramework.StorableItemDefinition Definition;
        private readonly GameObject _storedItemPlaceholder;
        private bool _hasCustomStoredItem;

        private TSelf Self => (TSelf)this;

        /// <summary>
        /// INTERNAL: Creates a new builder instance with a fresh StorableItemDefinition.
        /// Only ItemCreator can instantiate this.
        /// </summary>
        internal StorableItemDefinitionBuilder(
            Func<S1ItemFramework.StorableItemDefinition>? definitionFactory = null)
        {
            Definition = definitionFactory != null
                ? definitionFactory()
                : ScriptableObject.CreateInstance<S1ItemFramework.StorableItemDefinition>();

            ApplyDefaults();

            _storedItemPlaceholder = CreateStoredItemPlaceholder();
        }

        /// <summary>
        /// INTERNAL: Creates a builder instance initialized by cloning an existing item.
        /// Only ItemCreator can instantiate this.
        /// </summary>
        /// <param name="source">The existing item definition to clone properties from.</param>
        /// <param name="definitionFactory">Optional factory function to create the definition instance. If null, a default StorableItemDefinition will be created.</param>
        internal StorableItemDefinitionBuilder(
            S1ItemFramework.StorableItemDefinition source,
            Func<S1ItemFramework.StorableItemDefinition>? definitionFactory = null)
            : this(definitionFactory)
        {
            CopyPropertiesFrom(source);
        }

        /// <summary>
        /// Copies all properties from a source definition to the current definition.
        /// </summary>
        /// <param name="source">The source definition to copy from.</param>
        protected virtual void CopyPropertiesFrom(S1ItemFramework.StorableItemDefinition source)
        {
            if (source == null) return;

            // Basic ItemDefinition properties
            Definition.Name = source.Name;
            Definition.Description = source.Description;
            Definition.Category = source.Category;
            Definition.StackLimit = source.StackLimit;
            Definition.AvailableInDemo = source.AvailableInDemo;
            Definition.UsableInFilters = source.UsableInFilters;
            Definition.Icon = source.Icon;
            Definition.legalStatus = source.legalStatus;
            Definition.PickpocketDifficultyMultiplier = source.PickpocketDifficultyMultiplier;
            Definition.CombatUtility = source.CombatUtility;

            // StorableItemDefinition properties
            Definition.BasePurchasePrice = source.BasePurchasePrice;
            Definition.ResellMultiplier = source.ResellMultiplier;
            Definition.ShopCategories = source.ShopCategories;
            Definition.RequiresLevelToPurchase = source.RequiresLevelToPurchase;
            Definition.RequiredRank = source.RequiredRank;
            Definition.StoredItem = source.StoredItem != null ? source.StoredItem : Definition.StoredItem;
            Definition.StationItem = source.StationItem;
            Definition.Equippable = source.Equippable;
            Definition.CustomItemUI = source.CustomItemUI;
        }

        private void ApplyDefaults()
        {
            Definition.StackLimit = 10;
            Definition.BasePurchasePrice = 10f;
            Definition.ResellMultiplier = 0.5f;
            Definition.Category = S1CoreItemFramework.EItemCategory.Tools;
            Definition.legalStatus = S1CoreItemFramework.ELegalStatus.Legal;
            Definition.AvailableInDemo = true;
            Definition.UsableInFilters = true;
            Definition.RequiresLevelToPurchase = false;
            Definition.RequiredRank = new S1Levelling.FullRank(S1Levelling.ERank.Street_Rat, 1);
        }

        private GameObject CreateStoredItemPlaceholder()
        {
            var storedItemPlaceholder = new GameObject("S1API_DefaultStoredItem");
            storedItemPlaceholder.SetActive(false);
            storedItemPlaceholder.hideFlags = HideFlags.HideAndDontSave;
            Object.DontDestroyOnLoad(storedItemPlaceholder);
            var storedItemComponent = storedItemPlaceholder.AddComponent<S1Storage.StoredItem>();
            Definition.StoredItem = storedItemComponent;
            return storedItemPlaceholder;
        }

        /// <summary>
        /// Sets the basic information for the item.
        /// </summary>
        /// <param name="id">Unique identifier for the item (e.g., "my_custom_tool").</param>
        /// <param name="name">Display name shown in UI.</param>
        /// <param name="description">Item description shown in tooltips.</param>
        /// <param name="category">Item category for inventory organization.</param>
        /// <returns>The builder instance for fluent chaining.</returns>
        public TSelf WithBasicInfo(string id, string name, string description, ItemCategory category)
        {
            Definition.ID = id;
            Definition.Name = name;
            Definition.Description = description;
            Definition.Category = (S1CoreItemFramework.EItemCategory)category;

            // Update the underlying ScriptableObject name for clarity in inspectors/debuggers.
            var displayName = string.IsNullOrEmpty(name) ? id : name;
            if (!string.IsNullOrEmpty(displayName))
            {
                Definition.name = displayName;
                if (_storedItemPlaceholder != null && !_hasCustomStoredItem)
                {
                    _storedItemPlaceholder.name = $"{displayName}_StoredItem";
                }
            }

            return Self;
        }

        /// <summary>
        /// Sets the maximum stack size for this item.
        /// </summary>
        /// <param name="limit">Maximum quantity per inventory slot (1-999).</param>
        /// <returns>The builder instance for fluent chaining.</returns>
        public TSelf WithStackLimit(int limit)
        {
            Definition.StackLimit = Mathf.Clamp(limit, 1, 999);
            return Self;
        }

        /// <summary>
        /// Sets the icon sprite displayed for this item in UI.
        /// </summary>
        /// <param name="icon">The sprite to use as the item icon.</param>
        /// <returns>The builder instance for fluent chaining.</returns>
        public TSelf WithIcon(Sprite icon)
        {
            Definition.Icon = icon;
            return Self;
        }

        /// <summary>
        /// Configures the economic properties of the item.
        /// </summary>
        /// <param name="basePurchasePrice">Base price when buying from shops.</param>
        /// <param name="resellMultiplier">Fraction of purchase price recovered when selling (0.0 to 1.0).</param>
        /// <returns>The builder instance for fluent chaining.</returns>
        public TSelf WithPricing(float basePurchasePrice, float resellMultiplier = 0.5f)
        {
            Definition.BasePurchasePrice = Mathf.Max(0f, basePurchasePrice);
            Definition.ResellMultiplier = Mathf.Clamp01(resellMultiplier);
            return Self;
        }

        /// <summary>
        /// Sets the legal status of the item.
        /// </summary>
        /// <param name="status">Whether the item is legal or illegal.</param>
        /// <returns>The builder instance for fluent chaining.</returns>
        public TSelf WithLegalStatus(LegalStatus status)
        {
            Definition.legalStatus = (S1CoreItemFramework.ELegalStatus)status;
            return Self;
        }

        /// <summary>
        /// Attaches an equippable component to this item, allowing it to be equipped by the player.
        /// </summary>
        /// <param name="equippable">The equippable wrapper to attach.</param>
        /// <returns>The builder instance for fluent chaining.</returns>
        public TSelf WithEquippable(Equippable equippable)
        {
            if (equippable != null)
            {
                Definition.Equippable = equippable.S1Equippable;
            }

            return Self;
        }

        /// <summary>
        /// Assigns a custom StoredItem prefab for this definition.
        /// </summary>
        /// <param name="storedItemPrefab">Prefab containing a StoredItem component.</param>
        /// <returns>The builder instance for fluent chaining.</returns>
        public TSelf WithStoredItem(GameObject storedItemPrefab)
        {
            if (storedItemPrefab == null)
                return Self;

            var storedItem = storedItemPrefab.GetComponent<S1Storage.StoredItem>() ??
                             storedItemPrefab.AddComponent<S1Storage.StoredItem>();
            Definition.StoredItem = storedItem;
            _hasCustomStoredItem = true;
            return Self;
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
        public TSelf WithStationItem(GameObject stationItemPrefab)
        {
            if (stationItemPrefab == null)
                throw new ArgumentNullException(nameof(stationItemPrefab));

            var stationItem = stationItemPrefab.GetComponent<S1StationFramework.StationItem>();
            if (stationItem == null)
                throw new ArgumentException("Station item prefab must have a StationItem component.",
                    nameof(stationItemPrefab));

            var cached = GetOrCreateStationItemPrefab(stationItem);
            Definition.StationItem = cached;

            WarnIfStationItemMissingChemistryModules(cached);
            return Self;
        }

        /// <summary>
        /// Clears the StationItem reference for this definition.
        /// </summary>
        public TSelf WithoutStationItem()
        {
            Definition.StationItem = null;
            return Self;
        }

        /// <summary>
        /// Sets whether this item is available in the demo version of the game.
        /// </summary>
        /// <param name="available">True if available in demo, false otherwise.</param>
        /// <returns>The builder instance for fluent chaining.</returns>
        public TSelf WithDemoAvailability(bool available)
        {
            Definition.AvailableInDemo = available;
            return Self;
        }

        /// <summary>
        /// Assigns a level requirement for purchasing this item in shops.
        /// </summary>
        /// <param name="rank">The required rank to purchase this item, or null to remove level requirement.</param>
        /// <returns>>The builder instance for fluent chaining.</returns>
        public TSelf WithRequiredRank(Leveling.FullRank? rank)
        {
            if (rank == null)
            {
                Definition.RequiresLevelToPurchase = false;
                return Self;
            }

            Definition.RequiredRank = rank.Value.ToNative();
            Definition.RequiresLevelToPurchase = true;
            return Self;
        }

        /// <summary>
        /// Builds the item definition, registers it with the game's registry, and returns a wrapper.
        /// </summary>
        /// <returns>A wrapper around the created storable item definition.</returns>
        public virtual StorableItemDefinition Build()
        {
            if (!_hasCustomStoredItem && Definition.StoredItem != null)
            {
                // Ensure placeholder naming stays in sync after late changes.
                if (!string.IsNullOrEmpty(Definition.Name) && _storedItemPlaceholder != null)
                {
                    _storedItemPlaceholder.name = $"{Definition.Name}_StoredItem";
                }
            }

            // Register with the game's registry
            S1Registry.Instance.AddToRegistry(Definition);

            // Return wrapper
            return CreateWrapper(Definition);
        }

        /// <summary>
        /// INTERNAL: Builds and returns the raw game item definition without registering.
        /// Used internally by S1API. Modders should use <see cref="Build"/> instead.
        /// </summary>
        internal virtual S1ItemFramework.StorableItemDefinition BuildInternal()
        {
            return Definition;
        }

        /// <summary>
        /// Creates a wrapper around the given item definition.
        /// Subclasses can override this to return a more specific wrapper type.
        /// </summary>
        /// <param name="definition">The item definition to wrap.</param>
        /// <returns>>A wrapper around the given item definition.</returns>
        protected virtual StorableItemDefinition CreateWrapper(
            S1ItemFramework.StorableItemDefinition definition)
        {
            return new StorableItemDefinition(definition);
        }

        private static S1StationFramework.StationItem GetOrCreateStationItemPrefab(
            S1StationFramework.StationItem stationItemPrefab)
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
            if (StationItemRoot != null)
                return StationItemRoot;

            lock (StationItemGate)
            {
                if (StationItemRoot != null)
                    return StationItemRoot;

                var root = new GameObject("S1API_StationItemCache");
                root.hideFlags = HideFlags.HideAndDontSave;
                Object.DontDestroyOnLoad(root);

                // Place it far below the world; keep it active so instantiated prefabs remain active by default.
                root.transform.position = new Vector3(0f, -10000f, 0f);

                StationItemRoot = root;
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
                var hasIngredientModule =
                    stationItemPrefab.GetComponentInChildren<S1StationFramework.IngredientModule>(true) != null;
                var hasPourableModule =
                    stationItemPrefab.GetComponentInChildren<S1StationFramework.PourableModule>(true) != null;

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
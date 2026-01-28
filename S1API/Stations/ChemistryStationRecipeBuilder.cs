#if (IL2CPPMELON)
using S1 = Il2CppScheduleOne;
using S1ItemFramework = Il2CppScheduleOne.ItemFramework;
using S1StationFramework = Il2CppScheduleOne.StationFramework;
#elif (MONOMELON || MONOBEPINEX || IL2CPPBEPINEX)
using S1 = ScheduleOne;
using S1ItemFramework = ScheduleOne.ItemFramework;
using S1StationFramework = ScheduleOne.StationFramework;
#endif

using System;
using System.Collections.Generic;
using S1API.Internal.Utils;
using UnityEngine;

namespace S1API.Stations
{
    /// <summary>
    /// Builder for creating Chemistry Station recipes at runtime.
    /// </summary>
    public sealed class ChemistryStationRecipeBuilder
    {
        private string? _title;
        private int _cookTimeMinutes = 180;
        private Color _finalLiquidColor = Color.white;

        private string? _productItemId;
        private int _productQuantity = 1;
        private S1ItemFramework.ItemDefinition? _productItem;

        private readonly List<IngredientSpec> _ingredients = new List<IngredientSpec>();

        /// <summary>
        /// Sets the UI title for the recipe.
        /// </summary>
        public ChemistryStationRecipeBuilder WithTitle(string title)
        {
            _title = title?.Trim();
            return this;
        }

        /// <summary>
        /// Sets the cook time (minutes).
        /// </summary>
        public ChemistryStationRecipeBuilder WithCookTimeMinutes(int minutes)
        {
            if (minutes <= 0)
                throw new ArgumentOutOfRangeException(nameof(minutes), "Cook time must be > 0 minutes.");

            _cookTimeMinutes = minutes;
            return this;
        }

        /// <summary>
        /// Sets the final liquid UI color.
        /// </summary>
        public ChemistryStationRecipeBuilder WithFinalLiquidColor(Color color)
        {
            _finalLiquidColor = color;
            return this;
        }

        /// <summary>
        /// Adds a single ingredient requirement.
        /// </summary>
        public ChemistryStationRecipeBuilder WithIngredient(string itemId, int quantity)
        {
            return WithIngredientOptions(new[] { itemId }, quantity);
        }

        /// <summary>
        /// Adds an ingredient requirement with multiple acceptable item IDs (variants).
        /// </summary>
        public ChemistryStationRecipeBuilder WithIngredientOptions(IEnumerable<string> itemIds, int quantity)
        {
            if (itemIds == null)
                throw new ArgumentNullException(nameof(itemIds));
            if (quantity <= 0)
                throw new ArgumentOutOfRangeException(nameof(quantity), "Ingredient quantity must be > 0.");

            var resolvedIds = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var resolvedDefs = new List<S1ItemFramework.ItemDefinition>();

            foreach (var rawId in itemIds)
            {
                var id = (rawId ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(id))
                    continue;

                if (!seen.Add(id))
                    continue;

                var def = ResolveIngredientItemOrThrow(id);
                resolvedIds.Add(id);
                resolvedDefs.Add(def);
            }

            if (resolvedIds.Count == 0)
                throw new ArgumentException("Ingredient options must contain at least one valid item ID.", nameof(itemIds));

            _ingredients.Add(new IngredientSpec(resolvedIds, resolvedDefs, quantity));
            return this;
        }

        /// <summary>
        /// Sets the product produced by the recipe.
        /// </summary>
        public ChemistryStationRecipeBuilder WithProduct(string itemId, int quantity)
        {
            var id = (itemId ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Product item ID is required.", nameof(itemId));
            if (quantity <= 0)
                throw new ArgumentOutOfRangeException(nameof(quantity), "Product quantity must be > 0.");

            var def = ResolveProductItemOrThrow(id);
            _productItemId = id;
            _productQuantity = quantity;
            _productItem = def;

            if (string.IsNullOrWhiteSpace(_title))
            {
                try { _title = def.Name; } catch { /* ignore */ }
            }

            return this;
        }

        /// <summary>
        /// Builds and auto-registers the recipe with S1API.
        /// </summary>
        public ChemistryStationRecipe Build()
        {
            if (_productItem == null || string.IsNullOrWhiteSpace(_productItemId))
                throw new InvalidOperationException("ChemistryStationRecipeBuilder requires WithProduct(...) to be called before Build().");

            if (_ingredients.Count == 0)
                throw new InvalidOperationException("ChemistryStationRecipeBuilder requires at least one ingredient (WithIngredient...).");

            var title = string.IsNullOrWhiteSpace(_title) ? _productItemId! : _title!;
            var recipeId = $"{_productQuantity}x{_productItemId}";

            var native = BuildInternal();
            var wrapper = new ChemistryStationRecipe(
                native,
                recipeId: recipeId,
                title: title,
                cookTimeMinutes: _cookTimeMinutes,
                finalLiquidColor: _finalLiquidColor,
                product: new ChemistryStationRecipeProduct(_productItemId!, _productQuantity),
                ingredients: BuildIngredientWrappers());

            return ChemistryStationRecipes.Register(wrapper);
        }

        /// <summary>
        /// INTERNAL: Builds the raw native recipe without registering it.
        /// </summary>
        internal S1StationFramework.StationRecipe BuildInternal()
        {
            if (_productItem == null || string.IsNullOrWhiteSpace(_productItemId))
                throw new InvalidOperationException("WithProduct(...) must be called before BuildInternal().");

            var recipe = ScriptableObject.CreateInstance<S1StationFramework.StationRecipe>();
            recipe.IsDiscovered = true;
            recipe.Unlocked = true;
            recipe.RecipeTitle = string.IsNullOrWhiteSpace(_title) ? _productItemId! : _title!;
            recipe.CookTime_Mins = _cookTimeMinutes;
            recipe.FinalLiquidColor = _finalLiquidColor;
            recipe.QualityCalculationMethod = S1StationFramework.StationRecipe.EQualityCalculationMethod.Additive;

            recipe.Product = new S1StationFramework.StationRecipe.ItemQuantity
            {
                Item = _productItem,
                Quantity = _productQuantity
            };

            recipe.Ingredients.Clear();
            foreach (var ingredient in _ingredients)
            {
                var iq = new S1StationFramework.StationRecipe.IngredientQuantity
                {
                    Quantity = ingredient.Quantity
                };

                iq.Items.Clear();
                foreach (var def in ingredient.NativeItemDefs)
                {
                    if (def != null)
                        iq.Items.Add(def);
                }

                recipe.Ingredients.Add(iq);
            }

            return recipe;
        }

        private IReadOnlyList<ChemistryStationRecipeIngredient> BuildIngredientWrappers()
        {
            var list = new List<ChemistryStationRecipeIngredient>(_ingredients.Count);
            foreach (var ing in _ingredients)
            {
                list.Add(new ChemistryStationRecipeIngredient(ing.ItemIds.AsReadOnly(), ing.Quantity));
            }
            return list.AsReadOnly();
        }

        private static S1ItemFramework.ItemDefinition ResolveProductItemOrThrow(string itemId)
        {
            try
            {
                var item = S1.Registry.GetItem(itemId);
                if (item == null)
                    throw new InvalidOperationException($"Missing item '{itemId}'. Ensure the item is registered before building Chemistry Station recipes.");

                if (!CrossType.Is(item, out S1ItemFramework.StorableItemDefinition _))
                    throw new InvalidOperationException($"Product '{itemId}' is not a storable item. Chemistry Station recipes must produce storable items.");

                return item;
            }
            catch (Exception ex) when (ex is not InvalidOperationException)
            {
                throw new InvalidOperationException(
                    "ScheduleOne.Registry is not available yet. Register chemistry recipes during S1API.Lifecycle.GameLifecycle.OnPreLoad.",
                    ex);
            }
        }

        private static S1ItemFramework.ItemDefinition ResolveIngredientItemOrThrow(string itemId)
        {
            try
            {
                var item = S1.Registry.GetItem(itemId);
                if (item == null)
                    throw new InvalidOperationException($"Missing ingredient item '{itemId}'. Ensure the item is registered before building Chemistry Station recipes.");

                if (!CrossType.Is(item, out S1ItemFramework.StorableItemDefinition storable))
                    throw new InvalidOperationException($"Ingredient '{itemId}' is not a storable item.");

                if (storable.StationItem == null)
                    throw new InvalidOperationException($"Ingredient '{itemId}' does not have a valid StationItem (cannot be used at stations).");

                return item;
            }
            catch (Exception ex) when (ex is not InvalidOperationException)
            {
                throw new InvalidOperationException(
                    "ScheduleOne.Registry is not available yet. Register chemistry recipes during S1API.Lifecycle.GameLifecycle.OnPreLoad.",
                    ex);
            }
        }

        private sealed class IngredientSpec
        {
            public IngredientSpec(List<string> itemIds, List<S1ItemFramework.ItemDefinition> nativeItemDefs, int quantity)
            {
                ItemIds = itemIds;
                NativeItemDefs = nativeItemDefs;
                Quantity = quantity;
            }

            public List<string> ItemIds { get; }
            public List<S1ItemFramework.ItemDefinition> NativeItemDefs { get; }
            public int Quantity { get; }
        }
    }
}

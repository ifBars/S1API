#if (IL2CPPMELON)
using S1ItemFramework = Il2CppScheduleOne.ItemFramework;
using S1StationFramework = Il2CppScheduleOne.StationFramework;
#elif (MONOMELON || MONOBEPINEX || IL2CPPBEPINEX)
using S1ItemFramework = ScheduleOne.ItemFramework;
using S1StationFramework = ScheduleOne.StationFramework;
#endif

using System.Collections.Generic;
using UnityEngine;

namespace S1API.Stations
{
    /// <summary>
    /// Read-only wrapper for a Chemistry Station recipe (<c>StationRecipe</c>).
    /// </summary>
    public sealed class ChemistryStationRecipe
    {
        internal S1StationFramework.StationRecipe S1StationRecipe { get; }

        internal ChemistryStationRecipe(
            S1StationFramework.StationRecipe stationRecipe,
            string recipeId,
            string title,
            int cookTimeMinutes,
            Color finalLiquidColor,
            ChemistryStationRecipeProduct product,
            IReadOnlyList<ChemistryStationRecipeIngredient> ingredients)
        {
            S1StationRecipe = stationRecipe;
            RecipeID = recipeId;
            Title = title;
            CookTimeMinutes = cookTimeMinutes;
            FinalLiquidColor = finalLiquidColor;
            Product = product;
            Ingredients = ingredients;
        }

        /// <summary>
        /// Game-defined recipe identifier (<c>"{qty}x{productId}"</c>).
        /// </summary>
        public string RecipeID { get; }

        /// <summary>
        /// Display title shown in the Chemistry Station UI.
        /// </summary>
        public string Title { get; }

        /// <summary>
        /// Cook time in minutes.
        /// </summary>
        public int CookTimeMinutes { get; }

        /// <summary>
        /// UI liquid color for the final product.
        /// </summary>
        public Color FinalLiquidColor { get; }

        /// <summary>
        /// The product produced by this recipe.
        /// </summary>
        public ChemistryStationRecipeProduct Product { get; }

        /// <summary>
        /// Ingredient groups required by this recipe.
        /// Each group can have multiple acceptable item IDs (variants).
        /// </summary>
        public IReadOnlyList<ChemistryStationRecipeIngredient> Ingredients { get; }

        /// <summary>
        /// Returns the native product item definition.
        /// </summary>
        public S1ItemFramework.ItemDefinition S1ProductItem => S1StationRecipe.Product?.Item;
    }

    /// <summary>
    /// Product specification for a Chemistry Station recipe.
    /// </summary>
    public sealed class ChemistryStationRecipeProduct
    {
        internal ChemistryStationRecipeProduct(string itemId, int quantity)
        {
            ItemId = itemId;
            Quantity = quantity;
        }

        /// <summary>
        /// Product item ID.
        /// </summary>
        public string ItemId { get; }

        /// <summary>
        /// Product quantity.
        /// </summary>
        public int Quantity { get; }
    }

    /// <summary>
    /// Ingredient group specification for a Chemistry Station recipe.
    /// </summary>
    public sealed class ChemistryStationRecipeIngredient
    {
        internal ChemistryStationRecipeIngredient(IReadOnlyList<string> itemIds, int quantity)
        {
            ItemIds = itemIds;
            Quantity = quantity;
        }

        /// <summary>
        /// Acceptable ingredient item IDs (variants).
        /// </summary>
        public IReadOnlyList<string> ItemIds { get; }

        /// <summary>
        /// Required quantity for this ingredient group.
        /// </summary>
        public int Quantity { get; }
    }
}

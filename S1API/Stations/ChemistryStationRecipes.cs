#if (IL2CPPMELON)
using S1StationFramework = Il2CppScheduleOne.StationFramework;
#elif (MONOMELON || MONOBEPINEX || IL2CPPBEPINEX)
using S1StationFramework = ScheduleOne.StationFramework;
#endif

using System;
using System.Collections.Generic;
using S1API.Logging;

namespace S1API.Stations
{
    /// <summary>
    /// Registry for Chemistry Station recipes registered via S1API.
    /// </summary>
    public static class ChemistryStationRecipes
    {
        private static readonly Log Logger = new Log("ChemistryStationRecipes");
        private static readonly object Gate = new object();
        private static readonly Dictionary<string, ChemistryStationRecipe> ById = new Dictionary<string, ChemistryStationRecipe>(StringComparer.OrdinalIgnoreCase);
        private static readonly List<ChemistryStationRecipe> All = new List<ChemistryStationRecipe>();

        /// <summary>
        /// Registers a recipe with S1API (idempotent).
        /// </summary>
        public static ChemistryStationRecipe Register(ChemistryStationRecipe recipe)
        {
            if (recipe == null)
                throw new ArgumentNullException(nameof(recipe));

            if (string.IsNullOrWhiteSpace(recipe.RecipeID))
                throw new ArgumentException("RecipeID is required.", nameof(recipe));

            lock (Gate)
            {
                if (ById.ContainsKey(recipe.RecipeID))
                {
                    // Final decision: warn + skip
                    Logger.Warning($"RecipeID conflict for '{recipe.RecipeID}'. Skipping registration (first wins).");
                    return ById[recipe.RecipeID];
                }

                ById[recipe.RecipeID] = recipe;
                All.Add(recipe);
                return recipe;
            }
        }

        /// <summary>
        /// Convenience helper to create and register a recipe using a builder.
        /// </summary>
        public static ChemistryStationRecipe CreateAndRegister(Action<ChemistryStationRecipeBuilder> configure)
        {
            if (configure == null)
                throw new ArgumentNullException(nameof(configure));

            var builder = new ChemistryStationRecipeBuilder();
            configure(builder);
            return builder.Build();
        }

        /// <summary>
        /// Returns a read-only snapshot of all registered recipes.
        /// </summary>
        public static IReadOnlyList<ChemistryStationRecipe> GetAll()
        {
            lock (Gate)
            {
                return All.ToArray();
            }
        }

        internal static IReadOnlyList<S1StationFramework.StationRecipe> GetAllNative()
        {
            lock (Gate)
            {
                var arr = new S1StationFramework.StationRecipe[All.Count];
                for (int i = 0; i < All.Count; i++)
                {
                    arr[i] = All[i].S1StationRecipe;
                }
                return arr;
            }
        }
    }
}

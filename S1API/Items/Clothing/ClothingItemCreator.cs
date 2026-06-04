#if (IL2CPPMELON)
using S1 = Il2CppScheduleOne;
using S1Clothing = Il2CppScheduleOne.Clothing;
#elif (MONOMELON || MONOBEPINEX || IL2CPPBEPINEX)
using S1 = ScheduleOne;
using S1Clothing = ScheduleOne.Clothing;
#endif
using System;
using S1API.Internal.Utils;
using S1API.Logging;

namespace S1API.Items.Clothing
{
    /// <summary>
    /// Provides convenient static methods for creating custom clothing items.
    /// Use <see cref="CreateBuilder()"/> for flexible configuration or <see cref="CloneFrom(string)"/> for variants.
    /// </summary>
    public static class ClothingItemCreator
    {
        private static readonly Log Logger = new Log("ClothingItemCreator");

        /// <summary>
        /// Creates a new builder for composing a clothing item definition with full flexibility.
        /// Use fluent methods to configure the item, then call Build() to register it.
        /// </summary>
        /// <returns>A new ClothingItemDefinitionBuilder instance for fluent configuration.</returns>
        /// <example>
        /// <code>
        /// var hat = ClothingItemCreator.CreateBuilder()
        ///     .WithBasicInfo("my_hat", "Custom Hat", "A fancy custom hat")
        ///     .WithSlot(ClothingSlot.Head)
        ///     .WithApplicationType(ClothingApplicationType.Accessory)
        ///     .WithClothingAsset("MyMod/Accessories/CustomHat")
        ///     .WithDefaultColor(ClothingColor.Black)
        ///     .Build();
        /// </code>
        /// </example>
        public static ClothingItemDefinitionBuilder CreateBuilder()
        {
            return new ClothingItemDefinitionBuilder();
        }

        /// <summary>
        /// Creates a new clothing item by cloning an existing one by ID.
        /// Returns a builder pre-configured with all properties of the source item.
        /// You can then override specific properties before calling Build().
        /// </summary>
        /// <param name="sourceItemId">The ID of the clothing item to clone.</param>
        /// <returns>A builder pre-configured with the source item's properties.</returns>
        /// <example>
        /// <code>
        /// // Clone the base game cap and customize it
        /// var customCap = ClothingItemCreator.CloneFrom("cap")
        ///     .WithBasicInfo("stay_silly_cap", "Stay Silly Cap", "A silly custom cap")
        ///     .WithClothingAsset("BigWillyMod/Accessories/StaySillyCap")
        ///     .WithColorable(false)
        ///     .Build();
        /// </code>
        /// </example>
        public static ClothingItemDefinitionBuilder CloneFrom(string sourceItemId)
        {
            if (string.IsNullOrWhiteSpace(sourceItemId))
            {
                throw new ArgumentException("Source item ID cannot be null or whitespace", nameof(sourceItemId));
            }

            var sourceDefinition = S1.Registry.GetItem(sourceItemId);
            if (sourceDefinition == null)
            {
                Logger.Error($"Cannot clone clothing item '{sourceItemId}': source item not found in registry");
                return null;
            }

            // Use CrossType for proper IL2CPP/Mono type checking
            if (!CrossType.Is(sourceDefinition, out S1Clothing.ClothingDefinition clothingDef))
            {
                Logger.Error($"Cannot clone item '{sourceItemId}': it is not a clothing item (found type: {sourceDefinition.GetType().Name})");
                return null;
            }

            return new ClothingItemDefinitionBuilder(clothingDef);
        }

        /// <summary>
        /// Creates a new clothing item by cloning an existing one.
        /// Returns a builder pre-configured with all properties of the source item.
        /// </summary>
        /// <param name="source">The clothing item definition to clone.</param>
        /// <returns>A builder pre-configured with the source item's properties.</returns>
        /// <example>
        /// <code>
        /// var existingCap = ItemManager.GetItemDefinition("cap") as ClothingItemDefinition;
        /// var variant = ClothingItemCreator.CloneFrom(existingCap)
        ///     .WithBasicInfo("variant_cap", "Cap Variant", "A variant of the cap")
        ///     .Build();
        /// </code>
        /// </example>
        public static ClothingItemDefinitionBuilder CloneFrom(ClothingItemDefinition source)
        {
            if (source == null)
            {
                Logger.Error("Cannot clone from null clothing item definition");
                return null;
            }

            return new ClothingItemDefinitionBuilder(source.S1ClothingDefinition);
        }
    }
}


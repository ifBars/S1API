#if (IL2CPPMELON)
using S1ItemFramework = Il2CppScheduleOne.ItemFramework;
using S1Registry = Il2CppScheduleOne.Registry;
#elif (MONOMELON || MONOBEPINEX || IL2CPPBEPINEX)
using S1ItemFramework = ScheduleOne.ItemFramework;
using S1Registry = ScheduleOne.Registry;
#endif

using System;
using S1API.Internal.Utils;

namespace S1API.Items
{
    /// <summary>
    /// Provides convenient static methods for creating custom buildable items.
    /// </summary>
    /// <remarks>
    /// Use <see cref="CreateBuilder"/> for creating items from scratch,
    /// or <see cref="CloneFrom"/> for creating variants of existing buildable items.
    /// </remarks>
    [Obsolete("Use S1API.Items.Buildable.BuildableItemCreator instead")]
    public static class BuildableItemCreator
    {
        /// <summary>
        /// Creates a new builder for composing a buildable item definition with full flexibility.
        /// Use fluent methods to configure the item, then call Build() to register it.
        /// </summary>
        /// <returns>A new BuildableItemDefinitionBuilder instance for fluent configuration.</returns>
        /// <example>
        /// <code>
        /// var item = BuildableItemCreator.CreateBuilder()
        ///     .WithBasicInfo("my_rack", "Custom Storage Rack", "A custom storage rack")
        ///     .WithBuildSound(BuildSoundType.Metal)
        ///     .WithPricing(75f, 0.5f)
        ///     .Build();
        /// </code>
        /// </example>
        public static BuildableItemDefinitionBuilder CreateBuilder()
        {
            return new BuildableItemDefinitionBuilder();
        }

        /// <summary>
        /// Creates a new buildable item by cloning an existing item's properties.
        /// This is useful for creating variants of existing items (e.g., different materials, sizes).
        /// </summary>
        /// <param name="sourceItemId">The ID of the existing buildable item to clone from.</param>
        /// <returns>A builder initialized with the source item's properties, ready for customization.</returns>
        /// <example>
        /// <code>
        /// var metalRack = BuildableItemCreator.CloneFrom("StorageRack-1x0.5")
        ///     .WithBasicInfo("metal_rack_small", "Small Metal Storage Rack", "A metal version")
        ///     .WithBuildSound(BuildSoundType.Metal)
        ///     .WithPricing(72f, 0.5f)
        ///     .Build();
        /// </code>
        /// </example>
        public static BuildableItemDefinitionBuilder CloneFrom(string sourceItemId)
        {
            var sourceDefinition = S1Registry.GetItem(sourceItemId);

            if (sourceDefinition == null)
            {
                throw new System.ArgumentException(
                    $"Source item with ID '{sourceItemId}' not found in registry",
                    nameof(sourceItemId)
                );
            }

            // Try to cast to BuildableItemDefinition
            if (!CrossType.Is(sourceDefinition, out S1ItemFramework.BuildableItemDefinition buildableDef))
            {
                throw new System.ArgumentException(
                    $"Item '{sourceItemId}' is not a BuildableItemDefinition",
                    nameof(sourceItemId)
                );
            }

            return new BuildableItemDefinitionBuilder(buildableDef);
        }

        /// <summary>
        /// Creates a new buildable item by cloning from an existing BuildableItemDefinition wrapper.
        /// </summary>
        /// <param name="source">The buildable item definition to clone from.</param>
        /// <returns>A builder initialized with the source item's properties, ready for customization.</returns>
        /// <example>
        /// <code>
        /// var originalRack = ItemManager.GetItemDefinition("StorageRack-1x0.5") as BuildableItemDefinition;
        /// var metalRack = BuildableItemCreator.CloneFrom(originalRack)
        ///     .WithBasicInfo("metal_rack_small", "Small Metal Storage Rack", "A metal version")
        ///     .WithBuildSound(BuildSoundType.Metal)
        ///     .Build();
        /// </code>
        /// </example>
        public static BuildableItemDefinitionBuilder CloneFrom(BuildableItemDefinition source)
        {
            if (source == null)
            {
                throw new System.ArgumentNullException(nameof(source), "Source item definition cannot be null");
            }

            return new BuildableItemDefinitionBuilder(source.S1BuildableItemDefinition);
        }
    }
}

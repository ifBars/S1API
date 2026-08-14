#if (IL2CPPMELON)
using S1ItemFramework = Il2CppScheduleOne.ItemFramework;
using S1Registry = Il2CppScheduleOne.Registry;
#elif MONOMELON
using S1ItemFramework = ScheduleOne.ItemFramework;
using S1Registry = ScheduleOne.Registry;
#endif
using System;
using S1API.Internal.Building;
using S1API.Internal.Utils;

namespace S1API.Items.Buildable
{
    /// <summary>
    /// Creates first-class custom furniture backed by the game's native building system.
    /// </summary>
    public static class FurnitureCreator
    {
        /// <summary>
        /// Creates a builder for a custom placeable furniture item.
        /// </summary>
        /// <returns>A new furniture definition builder with grid-placement defaults.</returns>
        public static FurnitureDefinitionBuilder CreateBuilder()
        {
            return new FurnitureDefinitionBuilder();
        }

        /// <summary>
        /// Creates a presentation-only furniture variant from a registered native donor.
        /// The builder owns an isolated visual clone and independent material instances.
        /// </summary>
        /// <param name="sourceItemId">The stable ID of the native grid or surface furniture donor.</param>
        /// <returns>A builder initialized with the donor's safe placement and item defaults.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown when the ID is invalid, the donor is missing, or the donor has specialized runtime behavior.
        /// </exception>
        public static FurnitureDefinitionBuilder CloneFrom(string sourceItemId)
        {
            if (string.IsNullOrWhiteSpace(sourceItemId))
            {
                throw new ArgumentException(
                    "Source item ID cannot be null or whitespace.",
                    nameof(sourceItemId));
            }

            object? source = S1Registry.GetItem(sourceItemId);
            if (source == null ||
                !CrossType.Is(source, out S1ItemFramework.BuildableItemDefinition definition))
            {
                throw new ArgumentException(
                    $"Furniture donor '{sourceItemId}' was not found or is not buildable.",
                    nameof(sourceItemId));
            }

            return CreateCloneBuilder(definition);
        }

        /// <summary>
        /// Creates a presentation-only furniture variant from an existing buildable definition.
        /// The builder owns an isolated visual clone and independent material instances.
        /// </summary>
        /// <param name="source">The native grid or surface furniture donor.</param>
        /// <returns>A builder initialized with the donor's safe placement and item defaults.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown when the donor has specialized runtime behavior.</exception>
        public static FurnitureDefinitionBuilder CloneFrom(BuildableItemDefinition source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            return CreateCloneBuilder(source.S1BuildableItemDefinition);
        }

        private static FurnitureDefinitionBuilder CreateCloneBuilder(
            S1ItemFramework.BuildableItemDefinition source)
        {
            return new FurnitureDefinitionBuilder(
                FurnitureVisualCloner.CreateSource(source));
        }
    }
}

#if (IL2CPPMELON)
using S1ItemFramework = Il2CppScheduleOne.ItemFramework;
using S1Registry = Il2CppScheduleOne.Registry;
#elif (MONOMELON || MONOBEPINEX || IL2CPPBEPINEX)
using S1ItemFramework = ScheduleOne.ItemFramework;
using S1Registry = ScheduleOne.Registry;
#endif
using System;
using S1API.Internal.Utils;

namespace S1API.Items.Additive
{
    /// <summary>
    /// Provides convenient static methods for creating custom additive items.
    /// Use <see cref="CreateBuilder"/> for creating additives from scratch, or <see cref="CloneFrom"/> for variants.
    /// </summary>
    public static class AdditiveItemCreator
    {
        /// <summary>
        /// Creates a new builder for composing an additive definition with full flexibility.
        /// Use fluent methods to configure the additive, then call Build() to register it.
        /// </summary>
        public static AdditiveDefinitionBuilder CreateBuilder()
        {
            return new AdditiveDefinitionBuilder();
        }

        /// <summary>
        /// Creates a new additive builder by cloning an existing additive by ID.
        /// </summary>
        /// <param name="sourceItemId">The ID of the additive to clone.</param>
        /// <returns>A builder pre-configured with the source additive's properties.</returns>
        /// <exception cref="ArgumentException">Thrown if the source item does not exist or is not an additive.</exception>
        public static AdditiveDefinitionBuilder CloneFrom(string sourceItemId)
        {
            if (string.IsNullOrWhiteSpace(sourceItemId))
            {
                throw new ArgumentException("Source item ID cannot be null or whitespace", nameof(sourceItemId));
            }

            var sourceDefinition = S1Registry.GetItem(sourceItemId);
            if (sourceDefinition == null)
            {
                throw new ArgumentException($"Source item with ID '{sourceItemId}' not found in registry", nameof(sourceItemId));
            }

            if (!CrossType.Is(sourceDefinition, out S1ItemFramework.AdditiveDefinition additiveDef))
            {
                throw new ArgumentException($"Item '{sourceItemId}' is not an AdditiveDefinition", nameof(sourceItemId));
            }

            return new AdditiveDefinitionBuilder(additiveDef);
        }

        /// <summary>
        /// Creates a new additive builder by cloning an existing additive wrapper.
        /// </summary>
        /// <param name="source">The additive definition to clone from.</param>
        /// <returns>A builder pre-configured with the source additive's properties.</returns>
        public static AdditiveDefinitionBuilder CloneFrom(AdditiveDefinition source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source), "Source additive definition cannot be null");
            }

            return new AdditiveDefinitionBuilder(source.S1AdditiveDefinition);
        }
    }
}


#if (IL2CPPMELON)
using S1ItemFramework = Il2CppScheduleOne.ItemFramework;
using S1Registry = Il2CppScheduleOne.Registry;
#elif (MONOMELON || MONOBEPINEX || IL2CPPBEPINEX)
using S1ItemFramework = ScheduleOne.ItemFramework;
using S1Registry = ScheduleOne.Registry;
#endif
using System;
using S1API.Internal.Utils;

namespace S1API.Items.Quality
{
    /// <summary>
    /// Provides convenient static methods for creating custom quality items.
    /// Use <see cref="CreateBuilder"/> for flexible configuration
    /// or <see cref="CloneFrom"/> for quick variants based on existing items.
    /// </summary>
    public class QualityItemCreator
    {
        /// <summary>
        /// Creates a new builder for composing a quality item definition with full flexibility.
        /// Use fluent methods to configure the definition, then call Build() to register it.
        /// </summary>
        public static ItemBuilders.QualityItemDefinitionBuilder CreateBuilder()
        {
            return new ItemBuilders.QualityItemDefinitionBuilder();
        }
        
        /// <summary>
        /// Creates a new quality item builder by cloning an existing quality item by ID.
        /// </summary>
        /// <param name="sourceItemId">The ID of the item to clone.</param>
        /// <returns>A builder pre-configured with the source item properties.</returns>
        /// <exception cref="ArgumentException">Thrown if the source item ID is not found or is not a quality item.</exception>
        public static ItemBuilders.QualityItemDefinitionBuilder CloneFrom(string sourceItemId)
        {
            var sourceDefinition = S1Registry.GetItem(sourceItemId);
            if (sourceDefinition == null)
            {
                throw new ArgumentException($"Source item with ID '{sourceItemId}' not found in registry", nameof(sourceItemId));
            }

            if (!CrossType.Is(sourceDefinition, out S1ItemFramework.QualityItemDefinition qualityDef))
            {
                throw new ArgumentException($"Item '{sourceItemId}' is not an QualityItemDefinition", nameof(sourceItemId));
            }

            return new ItemBuilders.QualityItemDefinitionBuilder(qualityDef);
        }
        
        /// <summary>
        /// Creates a new quality item builder by cloning an existing quality item wrapper.
        /// </summary>
        /// <param name="source">The quality item definition to clone.</param>
        /// <returns>A builder pre-configured with the source item properties.</returns>
        /// <exception cref="ArgumentNullException">Thrown if the source definition is null.</exception>
        public static ItemBuilders.QualityItemDefinitionBuilder CloneFrom(QualityItemDefinition source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source), "Source storable item definition cannot be null");
            }

            return new ItemBuilders.QualityItemDefinitionBuilder(source.S1QualityDefinition);
        }
    }
}
#if (IL2CPPMELON)
using S1ItemFramework = Il2CppScheduleOne.ItemFramework;
#elif (MONOMELON || MONOBEPINEX || IL2CPPBEPINEX)
using S1ItemFramework = ScheduleOne.ItemFramework;
#endif

namespace S1API.Items.Quality
{
    /// <summary>
    /// Represents a quality item definition that can be consumed or used in recipes
    /// Extends <see cref="StorableItemDefinition"/> with quality-specific properties.
    /// </summary>
    /// <remarks>
    /// Use <see cref="Items.QualityItemCreator"/>
    /// </remarks>
    public class QualityItemDefinition : Storable.StorableItemDefinition
    {
        /// <summary>
        /// INTERNAL: Wraps an existing native quality item definition.
        /// </summary>
        internal QualityItemDefinition(S1ItemFramework.QualityItemDefinition definition) : base(definition)
        {
            S1QualityDefinition = definition;
        }
        
        /// <summary>
        /// INTERNAL: The underlying S1 quality item definition instance.
        /// </summary>
        internal S1ItemFramework.QualityItemDefinition S1QualityDefinition { get; }

        /// <summary>
        /// Creates a quality item instance from this definition using the default quality.
        /// </summary>
        /// <param name="quantity">The quantity to apply to the created instance.</param>
        /// <returns>A quality item instance using this definition's default quality.</returns>
        public override ItemInstance CreateInstance(int quantity = 1) => CreateInstance(quantity, DefaultQuality);
        
        /// <summary>
        /// Creates a quality item instance from this definition with the specified quality.
        /// </summary>
        /// <param name="quality">The quality to apply to the created instance.</param>
        /// <returns>A quality item instance using the specified quality.</returns>
        public QualityItemInstance CreateInstance(Products.Quality quality) => CreateInstance(1, quality);
        
        /// <summary>
        /// Creates a quality item instance from this definition with the specified quantity and quality.
        /// </summary>
        /// <param name="quantity">The quantity to apply to the created instance.</param>
        /// <param name="quality">The quality to apply to the created instance.</param>
        /// <returns>A quality item instance using the specified quantity and quality.</returns>
        public QualityItemInstance CreateInstance(int quantity, Products.Quality quality) =>
            new QualityItemInstance(new S1ItemFramework.QualityItemInstance(
                S1QualityDefinition,
                quantity,
                (S1ItemFramework.EQuality)quality));

        /// <summary>
        /// The default quality for this item.
        /// </summary>
        public Products.Quality DefaultQuality
        {
            get => (Products.Quality)S1QualityDefinition.DefaultQuality;
            set => S1QualityDefinition.DefaultQuality = (S1ItemFramework.EQuality)value;
        }
    }
}
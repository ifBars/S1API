#if (IL2CPPMELON)
using S1ItemFramework = Il2CppScheduleOne.ItemFramework;
#elif (MONOMELON || MONOBEPINEX || IL2CPPBEPINEX)
using S1ItemFramework = ScheduleOne.ItemFramework;
#endif
using S1API.Internal.Utils;

namespace S1API.Items.Quality
{
    /// <summary>
    /// Represents a quality item instance in the game world (usable item).
    /// Extends <see cref="ItemInstance"/> with quality information.
    /// </summary>
    public class QualityItemInstance : ItemInstance
    {
        /// <summary>
        /// INTERNAL: Reference to the in-game quality item instance.
        /// </summary>
        internal readonly S1ItemFramework.QualityItemInstance S1QualityInstance;

        /// <summary>
        /// INTERNAL: Creates a QualityItemInstance wrapper.
        /// </summary>
        /// <param name="itemInstance">In-game quality item instance</param>
        internal QualityItemInstance(S1ItemFramework.QualityItemInstance itemInstance) : base(itemInstance)
        {
            S1QualityInstance = itemInstance;
        }

        /// <summary>
        /// The quality of this item.
        /// </summary>
        public Products.Quality Quality
        {
            get => (Products.Quality)S1QualityInstance.Quality;
            set => S1QualityInstance.Quality = (S1ItemFramework.EQuality)value;
        }

        /// <summary>
        /// The quality item definition (template) this instance was created from.
        /// </summary>
        public new QualityItemDefinition Definition =>
            new QualityItemDefinition(
                CrossType.As<S1ItemFramework.QualityItemDefinition>(S1QualityInstance.Definition));
    }
}
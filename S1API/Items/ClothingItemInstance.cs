#if (IL2CPPMELON)
using S1Clothing = Il2CppScheduleOne.Clothing;
#elif (MONOMELON || MONOBEPINEX || IL2CPPBEPINEX)
using S1Clothing = ScheduleOne.Clothing;
#endif
using System;
using S1API.Internal.Utils;

namespace S1API.Items
{
    /// <summary>
    /// Represents a clothing item instance in the game world (physical clothing you own).
    /// Extends <see cref="ItemInstance"/> with color information.
    /// </summary>
    [Obsolete("Use S1API.Items.Clothing.ClothingItemInstance instead")]
    public class ClothingItemInstance : ItemInstance
    {
        /// <summary>
        /// INTERNAL: Reference to the in-game clothing item instance.
        /// </summary>
        internal readonly S1Clothing.ClothingInstance S1ClothingInstance;

        /// <summary>
        /// INTERNAL: Creates a ClothingItemInstance wrapper.
        /// </summary>
        /// <param name="itemInstance">In-game clothing item instance</param>
        internal ClothingItemInstance(S1Clothing.ClothingInstance itemInstance) 
            : base(itemInstance)
        {
            S1ClothingInstance = itemInstance;
        }

        /// <summary>
        /// The color of this clothing instance.
        /// </summary>
        public ClothingColor Color
        {
            get => (ClothingColor)S1ClothingInstance.Color;
            set => S1ClothingInstance.Color = (S1Clothing.EClothingColor)value;
        }

        /// <summary>
        /// The clothing definition (template) this instance was created from.
        /// </summary>
        public new ClothingItemDefinition Definition =>
            new ClothingItemDefinition(
                CrossType.As<S1Clothing.ClothingDefinition>(S1ClothingInstance.Definition));
    }
}


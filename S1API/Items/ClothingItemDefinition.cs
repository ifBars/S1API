#if (IL2CPPMELON)
using S1Clothing = Il2CppScheduleOne.Clothing;
using Il2CppCollections = Il2CppSystem.Collections.Generic;
#elif (MONOMELON || MONOBEPINEX || IL2CPPBEPINEX)
using S1Clothing = ScheduleOne.Clothing;
using Il2CppCollections = System.Collections.Generic;
#endif

using System.Collections.Generic;

namespace S1API.Items
{
    /// <summary>
    /// Represents a clothing item definition that can be worn by the player.
    /// Extends <see cref="StorableItemDefinition"/> with clothing-specific properties.
    /// </summary>
    /// <remarks>
    /// Use <see cref="ClothingItemCreator.CreateBuilder()"/> to create new clothing items,
    /// or <see cref="ClothingItemCreator.CloneFrom(string)"/> to create variants of existing items.
    /// </remarks>
    public sealed class ClothingItemDefinition : StorableItemDefinition
    {
        /// <summary>
        /// INTERNAL: Wraps an existing native clothing item definition.
        /// </summary>
        internal ClothingItemDefinition(S1Clothing.ClothingDefinition definition)
            : base(definition)
        {
            S1ClothingDefinition = definition;
        }

        /// <summary>
        /// INTERNAL: A reference to the native game clothing item definition.
        /// </summary>
        internal S1Clothing.ClothingDefinition S1ClothingDefinition { get; }

        /// <summary>
        /// The underlying game clothing definition (ScheduleOne.Clothing.ClothingDefinition).
        /// </summary>
        public object NativeClothingDefinition => S1ClothingDefinition;

        /// <summary>
        /// Creates a clothing instance from this definition using the default color.
        /// </summary>
        /// <param name="quantity">The quantity to apply to the created clothing instance.</param>
        /// <returns>A clothing item instance using this definition's default color.</returns>
        public override ItemInstance CreateInstance(int quantity = 1) =>
            CreateInstance(quantity, DefaultColor);

        /// <summary>
        /// Creates a clothing instance from this definition with the specified color.
        /// </summary>
        /// <param name="color">The clothing color to apply to the created instance.</param>
        /// <returns>A clothing instance using the specified color.</returns>
        public ClothingItemInstance CreateInstance(ClothingColor color) =>
            CreateInstance(1, color);

        /// <summary>
        /// Creates a clothing instance from this definition with the specified quantity and color.
        /// </summary>
        /// <param name="quantity">The quantity to apply to the created clothing instance.</param>
        /// <param name="color">The clothing color to apply to the created instance.</param>
        /// <returns>A clothing instance using the specified quantity and color.</returns>
        public ClothingItemInstance CreateInstance(int quantity, ClothingColor color) =>
            new ClothingItemInstance(new S1Clothing.ClothingInstance(
                S1ClothingDefinition,
                quantity,
                (S1Clothing.EClothingColor)color));

        /// <summary>
        /// The clothing slot this item occupies.
        /// </summary>
        public ClothingSlot Slot
        {
            get => (ClothingSlot)S1ClothingDefinition.Slot;
            set => S1ClothingDefinition.Slot = (S1Clothing.EClothingSlot)value;
        }

        /// <summary>
        /// How this clothing item is applied to the avatar.
        /// </summary>
        public ClothingApplicationType ApplicationType
        {
            get => (ClothingApplicationType)S1ClothingDefinition.ApplicationType;
            set => S1ClothingDefinition.ApplicationType = (S1Clothing.EClothingApplicationType)value;
        }

        /// <summary>
        /// The asset path to the clothing prefab or layer in Resources.
        /// </summary>
        public string ClothingAssetPath
        {
            get => S1ClothingDefinition.ClothingAssetPath;
            set => S1ClothingDefinition.ClothingAssetPath = value;
        }

        /// <summary>
        /// Whether this clothing item can be colored by the player.
        /// </summary>
        public bool Colorable
        {
            get => S1ClothingDefinition.Colorable;
            set => S1ClothingDefinition.Colorable = value;
        }

        /// <summary>
        /// The default color for this clothing item.
        /// </summary>
        public ClothingColor DefaultColor
        {
            get => (ClothingColor)S1ClothingDefinition.DefaultColor;
            set => S1ClothingDefinition.DefaultColor = (S1Clothing.EClothingColor)value;
        }

        /// <summary>
        /// List of clothing slots this item blocks when equipped.
        /// </summary>
        public List<ClothingSlot> SlotsToBlock
        {
            get
            {
                var result = new List<ClothingSlot>();
                if (S1ClothingDefinition.SlotsToBlock != null)
                {
                    foreach (var slot in S1ClothingDefinition.SlotsToBlock)
                    {
                        result.Add((ClothingSlot)slot);
                    }
                }
                return result;
            }
            set
            {
#if (IL2CPPMELON)
                S1ClothingDefinition.SlotsToBlock = new Il2CppCollections.List<S1Clothing.EClothingSlot>();
#else
                S1ClothingDefinition.SlotsToBlock = new List<S1Clothing.EClothingSlot>();
#endif
                if (value != null)
                {
                    foreach (var slot in value)
                    {
                        S1ClothingDefinition.SlotsToBlock.Add((S1Clothing.EClothingSlot)slot);
                    }
                }
            }
        }
    }
}


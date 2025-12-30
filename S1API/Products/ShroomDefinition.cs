#if (IL2CPPMELON)
using Il2CppScheduleOne.Product;
using S1ShroomDefinition = Il2CppScheduleOne.Product.ShroomDefinition;
using S1Properties = Il2CppScheduleOne.Effects;
#elif (MONOMELON || MONOBEPINEX || IL2CPPBEPINEX)
using ScheduleOne.Product;
using S1ShroomDefinition = ScheduleOne.Product.ShroomDefinition;
using S1Properties = ScheduleOne.Effects;
#endif

using System.Collections.Generic;
using S1API.Internal.Utils;
using S1API.Items;
using S1API.Properties;
using S1API.Properties.Interfaces;
using UnityEngine;

namespace S1API.Products
{
    /// <summary>
    /// Represents a specific type of mushroom (shroom) product definition.
    /// </summary>
    public sealed class ShroomDefinition : ProductDefinition
    {
        /// <summary>
        /// INTERNAL: Strongly typed reference to Schedule One's ShroomDefinition.
        /// </summary>
        internal S1ShroomDefinition S1ShroomDefinition =>
            CrossType.As<S1ShroomDefinition>(S1ItemDefinition);

        /// <summary>
        /// Represents a specific type of mushroom (shroom) product definition.
        /// </summary>
        internal ShroomDefinition(S1ShroomDefinition definition)
            : base(definition)
        {
        }

        /// <summary>
        /// The material used for rendering individual mushrooms.
        /// </summary>
        public Material ShroomMaterial =>
            S1ShroomDefinition.ShroomMaterial;

        /// <summary>
        /// The material used for rendering bulk mushrooms.
        /// </summary>
        public Material BulkMaterial =>
            S1ShroomDefinition.BulkMaterial;

        /// <summary>
        /// The material applied to character eyeballs when consuming mushrooms.
        /// </summary>
        public Material EyeballMaterial =>
            S1ShroomDefinition.EyeballMaterial;

        /// <summary>
        /// The appearance settings for this mushroom definition, including colors and visual properties.
        /// </summary>
        public ShroomAppearanceSettings AppearanceSettings =>
            new ShroomAppearanceSettings(S1ShroomDefinition.AppearanceSettings);

        /// <summary>
        /// Creates an instance of this mushroom product with the specified quantity.
        /// </summary>
        /// <param name="quantity">The quantity of the product instance to create. Defaults to 1 if not specified.</param>
        /// <returns>An <see cref="ItemInstance"/> representing the created mushroom product.</returns>
        public override ItemInstance CreateInstance(int quantity = 1) =>
            new ProductInstance(CrossType.As<ProductItemInstance>(
                S1ShroomDefinition.GetDefaultInstance(quantity)));

        /// <summary>
        /// Retrieves the list of properties associated with the mushroom product definition.
        /// </summary>
        /// <returns>A list of runtime-agnostic property wrappers that work on both Mono and IL2CPP.</returns>
        public List<PropertyBase> GetProperties()
        {
            var result = new List<PropertyBase>();
            var list = S1ShroomDefinition?.Properties;
            if (list != null)
            {
                for (int i = 0; i < list.Count; i++)
                    result.Add(new ProductPropertyWrapper(list[i]));
            }
            return result;
        }
    }
}


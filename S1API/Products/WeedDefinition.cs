#if (IL2CPPMELON)
using Il2CppScheduleOne.Product;
using S1WeedDefinition = Il2CppScheduleOne.Product.WeedDefinition;
using S1Properties = Il2CppScheduleOne.Effects;
#elif (MONOMELON || MONOBEPINEX || IL2CPPBEPINEX)
using ScheduleOne.Product;
using S1WeedDefinition = ScheduleOne.Product.WeedDefinition;
using S1Properties = ScheduleOne.Effects;
#endif

using S1API.Internal.Utils;
using S1API.Items;
using S1API.Properties;
using System.Collections.Generic;
using S1API.Properties.Interfaces;

namespace S1API.Products
{
    /// <summary>
    /// Represents a specific type of weed product definition.
    /// </summary>
    public class WeedDefinition : ProductDefinition
    {
        /// <summary>
        /// INTERNAL: Strongly typed reference to Schedule One's WeedDefinition.
        /// </summary>
        internal S1WeedDefinition S1WeedDefinition =>
            CrossType.As<S1WeedDefinition>(S1ItemDefinition);

        /// <summary>
        /// Represents a specific type of weed product definition.
        /// </summary>
        internal WeedDefinition(S1WeedDefinition definition)
            : base(definition)
        {
        }

        /// <summary>
        /// Creates an instance of the product with the specified quantity.
        /// </summary>
        /// <param name="quantity">The quantity of the product to create. Defaults to 1 if not specified.</param>
        /// <returns>An <see cref="ItemInstance"/> representing the created product instance with the specified quantity.</returns>
        public override ItemInstance CreateInstance(int quantity = 1) =>
            new ProductInstance(CrossType.As<ProductItemInstance>(
                S1WeedDefinition.GetDefaultInstance(quantity)));

        /// <summary>
        /// Retrieves a list of properties associated with this weed definition.
        /// </summary>
        /// <returns>A list of runtime-agnostic property wrappers that work on both Mono and IL2CPP.</returns>
        public List<PropertyBase> GetProperties()
        {
            var result = new List<PropertyBase>();
            var list = S1WeedDefinition?.Properties;

            if (list != null)
            {
                for (int i = 0; i < list.Count; i++)
                    result.Add(new ProductPropertyWrapper(list[i]));
            }

            return result;
        }
    }
}

#if (IL2CPPMELON)
using Il2CppScheduleOne.Product;
using S1CocaineDefinition = Il2CppScheduleOne.Product.CocaineDefinition;
using S1Properties = Il2CppScheduleOne.Effects;
#elif (MONOMELON || MONOBEPINEX || IL2CPPBEPINEX)
using ScheduleOne.Product;
using S1CocaineDefinition = ScheduleOne.Product.CocaineDefinition;
using S1Properties = ScheduleOne.Effects;
#endif

using System.Collections.Generic;
using S1API.Internal.Utils;
using S1API.Items;
using S1API.Properties;
using S1API.Properties.Interfaces;

namespace S1API.Products
{
    /// <summary>
    /// Defines the characteristics and behaviors of a cocaine product within the system.
    /// </summary>
    public class CocaineDefinition : ProductDefinition
    {
        /// <summary>
        /// Provides internal access to the CocaineDefinition type within the Schedule One system.
        /// </summary>
        internal S1CocaineDefinition S1CocaineDefinition =>
            CrossType.As<S1CocaineDefinition>(S1ItemDefinition);

        /// <summary>
        /// Represents the definition of a cocaine product within the system.
        /// </summary>
        internal CocaineDefinition(S1CocaineDefinition definition)
            : base(definition)
        {
        }

        /// <summary>
        /// Creates an instance of this product definition with the specified quantity.
        /// </summary>
        /// <param name="quantity">The quantity of the product to instantiate. Defaults to 1 if not specified.</param>
        /// <returns>An <see cref="ItemInstance"/> representing the instantiated product with the specified quantity.</returns>
        public override ItemInstance CreateInstance(int quantity = 1) =>
            new ProductInstance(CrossType.As<ProductItemInstance>(
                S1CocaineDefinition.GetDefaultInstance(quantity)));

        /// <summary>
        /// Retrieves a list of properties associated with this product definition.
        /// </summary>
        /// <returns>A list of runtime-agnostic property wrappers that work on both Mono and IL2CPP.</returns>
        public List<PropertyBase> GetProperties()
        {
            var result = new List<PropertyBase>();
            var list = S1CocaineDefinition?.Properties;
            if (list != null)
            {
                for (int i = 0; i < list.Count; i++)
                    result.Add(new ProductPropertyWrapper(list[i]));
            }
            return result;
        }

        /// <summary>
        /// DEPRECATED: Use GetProperties() which returns IProperty wrappers.
        /// Retrieves the raw Schedule One properties (may cause assembly reference issues in IL2CPP).
        /// </summary>
        /// <returns>A list of raw Schedule One properties.</returns>
        [System.Obsolete("Use GetProperties() instead for IL2CPP compatibility")]
        public List<S1Properties.Effect> GetRawProperties()
        {
            var result = new List<S1Properties.Effect>();
            var list = S1CocaineDefinition?.Properties;
            if (list != null)
            {
                for (int i = 0; i < list.Count; i++)
                    result.Add(list[i]);
            }
            return result;
        }
    }
}

#if (IL2CPPMELON)
using Il2CppScheduleOne.Product;
using S1MethDefinition = Il2CppScheduleOne.Product.MethDefinition;
using S1Properties = Il2CppScheduleOne.Effects;
#elif (MONOMELON || MONOBEPINEX || IL2CPPBEPINEX)
using ScheduleOne.Product;
using S1MethDefinition = ScheduleOne.Product.MethDefinition;
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
    /// Represents the definition of a meth product within the ScheduleOne product framework.
    /// </summary>
    public class MethDefinition : ProductDefinition
    {
        /// <summary>
        /// INTERNAL: Strongly typed access to S1MethDefinition.
        /// </summary>
        internal S1MethDefinition S1MethDefinition =>
            CrossType.As<S1MethDefinition>(S1ItemDefinition);

        /// <summary>
        /// Represents the definition of a Meth product in the product framework.
        /// </summary>
        internal MethDefinition(S1MethDefinition definition)
            : base(definition)
        {
        }

        /// <summary>
        /// Creates an instance of this meth product with the specified quantity.
        /// </summary>
        /// <param name="quantity">The quantity of the product instance to create. Defaults to 1 if not specified.</param>
        /// <returns>An instance of <see cref="ItemInstance"/> representing the created meth product.</returns>
        public override ItemInstance CreateInstance(int quantity = 1) =>
            new ProductInstance(CrossType.As<ProductItemInstance>(
                S1MethDefinition.GetDefaultInstance(quantity)));

        /// <summary>
        /// Retrieves the list of properties associated with the meth product definition.
        /// </summary>
        /// <returns>A list of runtime-agnostic property wrappers that work on both Mono and IL2CPP.</returns>
        public List<PropertyBase> GetProperties()
        {
            var result = new List<PropertyBase>();
            var list = S1MethDefinition?.Properties;
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
            var list = S1MethDefinition?.Properties;
            if (list != null)
            {
                for (int i = 0; i < list.Count; i++)
                    result.Add(list[i]);
            }
            return result;
        }
    }
}

#if (IL2CPPMELON)
using S1Product = Il2CppScheduleOne.Product;
#elif (MONOMELON || MONOBEPINEX || IL2CPPBEPINEX)
using S1Product = ScheduleOne.Product;
#endif

using S1API.Internal.Utils;
using S1API.Items;

namespace S1API.Products
{
    /// <summary>
    /// Represents an instance of a mushroom (shroom) product.
    /// </summary>
    /// <remarks>
    /// Mushroom products can have psychedelic visual effects when consumed by players.
    /// This class extends ProductInstance to provide shroom-specific functionality.
    /// </remarks>
    public sealed class ShroomInstance : ProductInstance
    {
        /// <summary>
        /// INTERNAL: Strongly typed access to the underlying shroom instance.
        /// </summary>
        internal S1Product.ShroomInstance S1ShroomInstance =>
            CrossType.As<S1Product.ShroomInstance>(S1ItemInstance);

        /// <summary>
        /// Represents an instance of a mushroom product.
        /// </summary>
        /// <param name="shroomInstance">The underlying game shroom instance to wrap.</param>
        internal ShroomInstance(S1Product.ShroomInstance shroomInstance)
            : base(shroomInstance)
        {
        }

        /// <summary>
        /// Gets the shroom-specific definition for this instance.
        /// </summary>
        public new ShroomDefinition Definition =>
            new ShroomDefinition(S1ShroomInstance.Definition as S1Product.ShroomDefinition);

        /// <summary>
        /// Gets the display name of the shroom instance.
        /// Automatically pluralizes "Shroom" to "Shrooms" when quantity is greater than 1.
        /// </summary>
        public new string Name =>
            S1ShroomInstance.Name;
    }
}


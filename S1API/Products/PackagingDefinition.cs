#if (IL2CPPMELON)
using S1Packaging = Il2CppScheduleOne.Product.Packaging;
using S1ItemFramework = Il2CppScheduleOne.ItemFramework;
#elif MONOMELON
using S1Packaging = ScheduleOne.Product.Packaging;
using S1ItemFramework = ScheduleOne.ItemFramework;
#endif

using S1API.Internal.Utils;
using S1API.Items;
using S1API.Products.Packaging;

namespace S1API.Products
{
    /// <summary>
    /// Represents a native packaging type in the active game runtime.
    /// </summary>
    /// <remarks>
    /// Packaging definitions describe capacity and stealth. They do not create packaging assets
    /// or alter a product's allowed packaging policy. Resolve a live definition through
    /// <see cref="ProductPopulator.GetPackaging(string)"/> or the item registry.
    /// </remarks>
    public class PackagingDefinition : ItemDefinition
    {
        /// <summary>
        /// INTERNAL: A reference to the packaging definition in-game.
        /// </summary>
        internal S1Packaging.PackagingDefinition S1PackagingDefinition =>
            CrossType.As<S1Packaging.PackagingDefinition>(S1ItemDefinition);

        /// <summary>
        /// INTERNAL: Creates an instance of this packaging definition from the in-game packaging definition instance.
        /// </summary>
        /// <param name="s1ItemDefinition"></param>
        internal PackagingDefinition(S1ItemFramework.ItemDefinition s1ItemDefinition) :
            base(s1ItemDefinition) { }

        /// <summary>
        /// Gets the native product quantity this packaging can hold.
        /// </summary>
        public int Quantity =>
            S1PackagingDefinition.Quantity;

        /// <summary>
        /// Gets the runtime-agnostic stealth level for this packaging.
        /// </summary>
        public StealthLevel StealthLevel =>
            S1PackagingDefinition.StealthLevel.ToAPI();
    }
}

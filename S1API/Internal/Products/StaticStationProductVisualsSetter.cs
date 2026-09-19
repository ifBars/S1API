#if IL2CPPMELON
using Il2CppInterop.Runtime.Attributes;
using Il2CppInterop.Runtime.Injection;
using S1Product = Il2CppScheduleOne.Product;
#elif MONOMELON
using S1Product = ScheduleOne.Product;
#endif

using System;
using MelonLoader;

namespace S1API.Internal.Products
{
    /// <summary>
    /// INTERNAL: Activates a station visual together with its native interaction root.
    /// </summary>
#if IL2CPPMELON
    [RegisterTypeInIl2Cpp]
#endif
    internal sealed class StaticStationProductVisualsSetter :
        S1Product.ProductVisualsSetter
    {
#if IL2CPPMELON
        public StaticStationProductVisualsSetter(IntPtr pointer)
            : base(pointer)
        {
        }

        public StaticStationProductVisualsSetter()
            : base(
                ClassInjector
                    .DerivedConstructorPointer<StaticStationProductVisualsSetter>())
        {
            ClassInjector.DerivedConstructorBody(this);
        }
#endif

        /// <inheritdoc />
        public override void ApplyVisuals(
            S1Product.ProductDefinition productDefinition)
        {
            if (VisualsContainer == null)
                return;

            if (VisualsContainer.parent != null)
                VisualsContainer.parent.gameObject.SetActive(true);

            VisualsContainer.gameObject.SetActive(true);
        }
    }
}

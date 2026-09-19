using System;
using S1API.Internal.Products;

namespace S1API.Products
{
    /// <summary>Registers process-lifetime providers for custom-product save descriptors.</summary>
    /// <remarks>
    /// Register providers during early mod initialization, before custom-product save restoration.
    /// Provider IDs are durable and case-insensitive. A conflicting provider cannot replace the
    /// provider that already owns that ID.
    /// </remarks>
    public static class CustomProductSaveProviderRegistry
    {
        /// <summary>Registers a save provider, or returns the existing equivalent provider.</summary>
        /// <param name="provider">The provider that reconstructs one family of saved custom products.</param>
        /// <returns>The registered provider, or the existing equivalent provider.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="provider"/> is <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException">Thrown when another provider already owns the same ID.</exception>
        public static ICustomProductSaveProvider Register(ICustomProductSaveProvider provider)
        {
            return CustomProductSavePersistence.RegisterProvider(provider);
        }
    }
}

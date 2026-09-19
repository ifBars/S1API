using System;
using System.Collections.Generic;

namespace S1API.Products
{
    /// <summary>
    /// Immutable scalar data used to recreate a generic custom product while loading a save.
    /// </summary>
    /// <remarks>
    /// This descriptor deliberately contains no Unity objects, asset references, delegates, or
    /// process-local references. A provider may use <see cref="ProviderData"/> for its own
    /// bounded scalar configuration. It must recreate assets and callbacks from local mod resources.
    /// </remarks>
    public sealed class CustomProductSaveDescriptor
    {
        /// <summary>Gets the S1API descriptor format version.</summary>
        public int FormatVersion { get; }
        /// <summary>Gets the stable custom product ID.</summary>
        public string ProductId { get; }
        /// <summary>Gets the product owner's stable ID.</summary>
        public string OwnerId { get; }
        /// <summary>Gets the saved display name.</summary>
        public string ProductName { get; }
        /// <summary>Gets the saved description.</summary>
        public string Description { get; }
        /// <summary>Gets the saved initial price.</summary>
        public float InitialPrice { get; }
        /// <summary>Gets the saved logical product-kind ID.</summary>
        public string ProductKindId { get; }
        /// <summary>Gets the optional content-provider ID.</summary>
        public string? ProviderId { get; }
        /// <summary>Gets the provider-specific descriptor version.</summary>
        public int ProviderVersion { get; }
        /// <summary>Gets the provider-owned scalar payload.</summary>
        public string ProviderData { get; }

        internal Internal.Products.CustomProductSaveDescriptorData? Data { get; set; }

        /// <summary>
        /// Initializes a descriptor supplied to a registered content provider.
        /// </summary>
        public CustomProductSaveDescriptor(
            int formatVersion,
            string productId,
            string ownerId,
            string productName,
            string description,
            float initialPrice,
            string productKindId,
            string? providerId,
            int providerVersion,
            string providerData)
        {
            FormatVersion = formatVersion;
            ProductId = productId ?? throw new ArgumentNullException(nameof(productId));
            OwnerId = ownerId ?? throw new ArgumentNullException(nameof(ownerId));
            ProductName = productName ?? throw new ArgumentNullException(nameof(productName));
            Description = description ?? throw new ArgumentNullException(nameof(description));
            InitialPrice = initialPrice;
            ProductKindId = productKindId ?? throw new ArgumentNullException(nameof(productKindId));
            ProviderId = providerId;
            ProviderVersion = providerVersion;
            ProviderData = providerData ?? throw new ArgumentNullException(nameof(providerData));
        }
    }

    /// <summary>Reconstructs a custom product from a persisted scalar descriptor.</summary>
    /// <remarks>
    /// Register the provider before save restoration. The returned builder must retain the descriptor's
    /// stable product ID. Returning <see langword="null"/> preserves S1API's safe missing-content path.
    /// </remarks>
    public interface ICustomProductSaveProvider
    {
        /// <summary>Gets the stable, namespaced provider ID.</summary>
        string ProviderId { get; }
        /// <summary>Gets the highest provider descriptor version this provider accepts.</summary>
        int MaximumDescriptorVersion { get; }
        /// <summary>Returns the fully configured builder that recreates the descriptor's product.</summary>
        /// <param name="descriptor">The validated scalar descriptor.</param>
        /// <returns>A fully configured, unbuilt definition builder, or <see langword="null"/> to skip restoration safely.</returns>
        CustomProductDefinitionBuilder? Restore(CustomProductSaveDescriptor descriptor);
    }
}

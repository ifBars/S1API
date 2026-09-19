using System;
using S1API.Products;

namespace S1API.Internal.Products
{
    /// <summary>INTERNAL: Allocates stable IDs after the native mix-name sanitizer has run.</summary>
    internal static class CustomProductMixingIdentity
    {
        private const string FallbackOwnerId = "s1api";

        internal static string CreateGeneratedProductId(
            string sourceProductId,
            string nativeMixId)
        {
            string sourceId = ProductKindId.Normalize(sourceProductId, nameof(sourceProductId));
            if (string.IsNullOrWhiteSpace(nativeMixId))
                throw new InvalidOperationException("The native mix-name sanitizer produced an empty ID.");

            int separator = sourceId.IndexOf(':');
            string ownerId = sourceId.Substring(0, separator);
            // Do not embed the prior generated ID here. A generated output can itself
            // be mixed, and retaining that full lineage adds another "/mix/..." segment
            // on every generation until it cannot be advertised in the bounded manifest.
            // Hashing the complete normalized source keeps distinct source products
            // distinct when an overflowing lineage needs a bounded replacement ID.
            string sourceHash = CustomProductManifestData.ComputeHash(sourceId);
            string nativeHash = CustomProductManifestData.ComputeHash(nativeMixId);
            return ProductKindId.Normalize(
                CreateGeneratedPrefix(sourceId, ownerId, sourceHash) + nativeHash,
                nameof(nativeMixId));
        }

        internal static bool IsGeneratedIdForSource(string sourceProductId, string productId)
        {
            string sourceId = ProductKindId.Normalize(sourceProductId, nameof(sourceProductId));
            int separator = sourceId.IndexOf(':');
            string ownerId = sourceId.Substring(0, separator);
            string boundedPrefix = CreateGeneratedPrefix(
                sourceId,
                ownerId,
                CustomProductManifestData.ComputeHash(sourceId));
            if (productId.StartsWith(boundedPrefix, StringComparison.OrdinalIgnoreCase))
                return true;

            // Keep already-persisted generated IDs recognizable even when a source
            // reached the bounded source-hash form in an earlier runtime.
            string sourceHashPrefix = CreateBoundedSourceHashPrefix(
                ownerId,
                CustomProductManifestData.ComputeHash(sourceId));
            if (productId.StartsWith(sourceHashPrefix, StringComparison.OrdinalIgnoreCase))
                return true;

            string legacyPrefix = ownerId + ":mix/" +
                sourceId.Substring(separator + 1) + "/";
            return productId.StartsWith(legacyPrefix, StringComparison.OrdinalIgnoreCase);
        }

        private static string CreateGeneratedPrefix(
            string sourceId,
            string ownerId,
            string sourceHash)
        {
            int separator = sourceId.IndexOf(':');
            string legacyPrefix = ownerId + ":mix/" +
                sourceId.Substring(separator + 1) + "/";
            return legacyPrefix.Length + 64 <=
                   CustomProductManifestData.MaximumIdentifierLength
                ? legacyPrefix
                : CreateBoundedSourceHashPrefix(ownerId, sourceHash);
        }

        private static string CreateBoundedSourceHashPrefix(
            string ownerId,
            string sourceHash)
        {
            string prefix = ownerId + ":mix/" + sourceHash + "/";
            return prefix.Length + 64 <= CustomProductManifestData.MaximumIdentifierLength
                ? prefix
                : FallbackOwnerId + ":mix/" + sourceHash + "/";
        }
    }
}

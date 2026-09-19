#if IL2CPPMELON
using S1Product = Il2CppScheduleOne.Product;
#elif MONOMELON
using S1Product = ScheduleOne.Product;
#endif

using System;
using System.Collections.Generic;
using S1API.Products;
using UnityEngine;

namespace S1API.Internal.Products
{
    /// <summary>
    /// INTERNAL: Retains S1API-specific metadata for a generic custom product.
    /// Native save and network payloads continue to use the product definition's stable ID.
    /// </summary>
    internal sealed class CustomProductDefinitionMetadata
    {
        internal CustomProductDefinitionMetadata(
            ProductKind productKind,
            Quality defaultQuality)
            : this(
                productKind,
                defaultQuality,
                Array.Empty<PackagingDefinition>(),
                null)
        {
        }

        internal CustomProductDefinitionMetadata(
            ProductKind productKind,
            Quality defaultQuality,
            IReadOnlyList<PackagingDefinition> validPackaging)
            : this(productKind, defaultQuality, validPackaging, null)
        {
        }

        internal CustomProductDefinitionMetadata(
            ProductKind productKind,
            Quality defaultQuality,
            IReadOnlyList<PackagingDefinition> validPackaging,
            S1Product.ProductDefinition? representationTemplate,
            Color32? generatedMixColor = null)
        {
            ProductKind = productKind;
            DefaultQuality = defaultQuality;
            if (validPackaging == null)
                throw new ArgumentNullException(nameof(validPackaging));

            ValidPackaging =
                new List<PackagingDefinition>(validPackaging).AsReadOnly();
            RepresentationTemplate = representationTemplate;
            GeneratedMixColor = generatedMixColor;
        }

        internal ProductKind ProductKind { get; }

        internal Quality DefaultQuality { get; }

        internal IReadOnlyList<PackagingDefinition> ValidPackaging { get; }

        internal S1Product.ProductDefinition? RepresentationTemplate { get; }

        internal Color32? GeneratedMixColor { get; }
    }
}

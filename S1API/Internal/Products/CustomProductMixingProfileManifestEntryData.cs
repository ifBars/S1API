using System;
using System.Globalization;
using System.Text;
using S1API.Products;

namespace S1API.Internal.Products
{
    /// <summary>INTERNAL: Scalar compatibility identity for a registered mixing profile.</summary>
    internal sealed class CustomProductMixingProfileManifestEntryData
    {
        public string ProductKindId = string.Empty;
        public int MixerMap;
        public string OutputFactoryIdentity = string.Empty;
        public int OutputFactoryVersion;
        public bool UsePropertyColorMixing;

        internal static CustomProductMixingProfileManifestEntryData Create(ProductMixingProfile profile) =>
            new CustomProductMixingProfileManifestEntryData
            {
                ProductKindId = profile.ProductKind.Id,
                MixerMap = (int)profile.MixerMap,
                OutputFactoryIdentity = profile.OutputFactoryIdentity,
                OutputFactoryVersion = profile.OutputFactoryVersion,
                UsePropertyColorMixing = profile.UsePropertyColorMixing
            };

        internal bool IsValid() =>
            CustomProductManifestData.IsBoundedIdentifier(ProductKindId) &&
            Enum.IsDefined(typeof(ProductMixingMap), MixerMap) &&
            CustomProductManifestData.IsBoundedIdentifier(OutputFactoryIdentity) &&
            OutputFactoryVersion >= 0;

        internal static int Compare(
            CustomProductMixingProfileManifestEntryData left,
            CustomProductMixingProfileManifestEntryData right) =>
            StringComparer.OrdinalIgnoreCase.Compare(left.ProductKindId, right.ProductKindId);

        internal void AppendCanonical(StringBuilder builder)
        {
            builder.Append(ProductKindId.ToUpperInvariant()).Append('|');
            builder.Append(MixerMap.ToString(CultureInfo.InvariantCulture)).Append('|');
            builder.Append(OutputFactoryIdentity.ToUpperInvariant()).Append('|');
            builder.Append(OutputFactoryVersion.ToString(CultureInfo.InvariantCulture)).Append('|');
            builder.Append(UsePropertyColorMixing ? "1|" : "0|");
        }

        internal string? DescribeMismatch(CustomProductMixingProfileManifestEntryData local)
        {
            if (MixerMap != local.MixerMap)
                return "native mixer-map strategy differs";
            if (!string.Equals(OutputFactoryIdentity, local.OutputFactoryIdentity, StringComparison.OrdinalIgnoreCase) ||
                OutputFactoryVersion != local.OutputFactoryVersion)
            {
                return "output-factory compatibility identity or version differs";
            }
            if (UsePropertyColorMixing != local.UsePropertyColorMixing)
                return "property-color mixing strategy differs";
            return null;
        }
    }
}

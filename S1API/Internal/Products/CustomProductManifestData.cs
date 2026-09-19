using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using S1API.Products;

namespace S1API.Internal.Products
{
    /// <summary>
    /// INTERNAL: Defines the scalar-only custom-product compatibility manifest.
    /// </summary>
    internal sealed class CustomProductManifestData
    {
        internal const int CurrentProtocolVersion = 3;
        internal const int MaximumEntryCount = 256;
        internal const int MaximumPayloadLength = 65536;
        internal const int MaximumIdentifierLength = 256;
        internal const int MaximumPackagingCount = 32;

        public int ProtocolVersion = CurrentProtocolVersion;
        public string SessionId = string.Empty;
        public string CompatibilityHash = string.Empty;
        public CustomProductManifestEntryData[] Entries =
            Array.Empty<CustomProductManifestEntryData>();
        public CustomProductMixingProfileManifestEntryData[] MixingProfiles =
            Array.Empty<CustomProductMixingProfileManifestEntryData>();
        public CustomProductSaveDescriptorData[] GeneratedDescriptors =
            Array.Empty<CustomProductSaveDescriptorData>();

        internal static CustomProductManifestData Create(
            IEnumerable<CustomProductDefinitionRegistration> registrations)
        {
            if (registrations == null)
                throw new ArgumentNullException(nameof(registrations));

            var entries = new List<CustomProductManifestEntryData>();
            foreach (CustomProductDefinitionRegistration registration in registrations)
            {
                if (registration.Metadata == null || registration.SaveDescriptor == null)
                    continue;

                CustomProductManifestEntryData entry =
                    CustomProductManifestEntryData.Create(registration);
                if (!entry.IsValid())
                {
                    throw new InvalidOperationException(
                        "Custom product '" + registration.ProductId +
                        "' cannot be represented by the bounded multiplayer manifest.");
                }
                entries.Add(entry);
            }

            entries.Sort(CustomProductManifestEntryData.Compare);
            if (entries.Count > MaximumEntryCount)
            {
                throw new InvalidOperationException(
                    "The custom-product manifest exceeds the supported product count.");
            }

            ProductMixingProfile[] profiles = ProductMixingProfileRegistry.Snapshot();
            var profileEntries = new List<CustomProductMixingProfileManifestEntryData>(profiles.Length);
            for (int i = 0; i < profiles.Length; i++)
                profileEntries.Add(CustomProductMixingProfileManifestEntryData.Create(profiles[i]));
            profileEntries.Sort(CustomProductMixingProfileManifestEntryData.Compare);

            var generatedDescriptors = new List<CustomProductSaveDescriptorData>();
            foreach (CustomProductDefinitionRegistration registration in registrations)
            {
                if (registration.SaveDescriptor != null && registration.SaveDescriptor.IsGeneratedMix)
                    generatedDescriptors.Add(registration.SaveDescriptor);
            }
            generatedDescriptors.Sort((left, right) => StringComparer.OrdinalIgnoreCase.Compare(left.ProductId, right.ProductId));

            var result = new CustomProductManifestData
            {
                Entries = entries.ToArray(),
                MixingProfiles = profileEntries.ToArray(),
                GeneratedDescriptors = generatedDescriptors.ToArray()
            };
            result.CompatibilityHash = ComputeManifestHash(result.Entries, result.MixingProfiles);
            return result;
        }

        internal string Serialize(string sessionId)
        {
            if (!IsSessionId(sessionId))
                throw new InvalidOperationException("The custom-product session ID is invalid.");

            var snapshot = new CustomProductManifestData
            {
                ProtocolVersion = ProtocolVersion,
                SessionId = sessionId,
                CompatibilityHash = CompatibilityHash,
                Entries = Entries,
                MixingProfiles = MixingProfiles,
                GeneratedDescriptors = GeneratedDescriptors
            };
            string payload = JsonConvert.SerializeObject(snapshot, Formatting.None);
            if (payload.Length > MaximumPayloadLength ||
                Encoding.UTF8.GetByteCount(payload) > MaximumPayloadLength)
            {
                throw new InvalidOperationException(
                    "The custom-product manifest exceeds the supported payload size.");
            }

            return payload;
        }

        internal static bool TryDeserialize(
            string? payload,
            out CustomProductManifestData manifest,
            out string failure)
        {
            manifest = null!;
            failure = string.Empty;
            if (payload == null || payload.Length == 0 ||
                payload.Length > MaximumPayloadLength ||
                Encoding.UTF8.GetByteCount(payload) > MaximumPayloadLength)
            {
                failure = "manifest payload size is invalid";
                return false;
            }

            try
            {
                using var textReader = new StringReader(payload);
                using var jsonReader = new JsonTextReader(textReader)
                {
                    DateParseHandling = DateParseHandling.None,
                    MaxDepth = 16
                };
                JObject root = JObject.Load(jsonReader, new JsonLoadSettings
                {
                    DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error
                });
                if (jsonReader.Read())
                {
                    failure = "manifest payload contains trailing data";
                    return false;
                }

                CustomProductManifestData? candidate = root.ToObject<CustomProductManifestData>(
                    JsonSerializer.Create(new JsonSerializerSettings
                    {
                        MissingMemberHandling = MissingMemberHandling.Error,
                        MaxDepth = 16
                    }));
                if (!TryValidate(candidate, out failure))
                    return false;

                manifest = candidate!;
                return true;
            }
            catch (Exception)
            {
                failure = "manifest payload is malformed";
                return false;
            }
        }

        internal static bool TryValidate(
            CustomProductManifestData? manifest,
            out string failure)
        {
            failure = string.Empty;
            if (manifest == null ||
                manifest.ProtocolVersion != CurrentProtocolVersion ||
                !IsSessionId(manifest.SessionId) ||
                !IsHash(manifest.CompatibilityHash) ||
                manifest.Entries == null || manifest.MixingProfiles == null ||
                manifest.GeneratedDescriptors == null ||
                manifest.Entries.Length > MaximumEntryCount ||
                manifest.MixingProfiles.Length > MaximumEntryCount ||
                manifest.GeneratedDescriptors.Length > MaximumEntryCount)
            {
                failure = "manifest protocol, session, hash, or entry count is invalid";
                return false;
            }

            var profileKinds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < manifest.MixingProfiles.Length; i++)
            {
                if (!manifest.MixingProfiles[i].IsValid() ||
                    !profileKinds.Add(manifest.MixingProfiles[i].ProductKindId) ||
                    (i > 0 && CustomProductMixingProfileManifestEntryData.Compare(
                        manifest.MixingProfiles[i - 1], manifest.MixingProfiles[i]) >= 0))
                {
                    failure = "manifest contains an invalid or duplicate mixing profile";
                    return false;
                }
            }

            var generatedIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < manifest.GeneratedDescriptors.Length; i++)
            {
                CustomProductSaveDescriptorData descriptor = manifest.GeneratedDescriptors[i];
                if (!descriptor.IsGeneratedMix || !generatedIds.Add(descriptor.ProductId) ||
                    !CustomProductSavePersistence.IsNetworkGeneratedDescriptorValid(descriptor))
                {
                    failure = "manifest contains an invalid generated descriptor";
                    return false;
                }
            }

            var productIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            CustomProductManifestEntryData[] entries = manifest.Entries;
            for (int i = 0; i < entries.Length; i++)
            {
                if (!entries[i].IsValid() || !productIds.Add(entries[i].ProductId))
                {
                    failure = "manifest contains an invalid or duplicate product entry";
                    return false;
                }

                if (i > 0 && CustomProductManifestEntryData.Compare(
                        entries[i - 1], entries[i]) >= 0)
                {
                    failure = "manifest entries are not in canonical order";
                    return false;
                }
            }

            if (!string.Equals(
                    manifest.CompatibilityHash,
                    ComputeManifestHash(entries, manifest.MixingProfiles),
                    StringComparison.Ordinal))
            {
                failure = "manifest compatibility hash is invalid";
                return false;
            }

            return true;
        }

        internal static string ComputeManifestHash(
            IReadOnlyList<CustomProductManifestEntryData> entries)
        {
            return ComputeManifestHash(entries,
                Array.Empty<CustomProductMixingProfileManifestEntryData>());
        }

        internal static string ComputeManifestHash(
            IReadOnlyList<CustomProductManifestEntryData> entries,
            IReadOnlyList<CustomProductMixingProfileManifestEntryData> mixingProfiles)
        {
            var builder = new StringBuilder();
            builder.Append(CurrentProtocolVersion).Append('|');
            for (int i = 0; i < entries.Count; i++)
                entries[i].AppendCanonical(builder, includeHash: true);
            builder.Append("profiles|");
            for (int i = 0; i < mixingProfiles.Count; i++)
                mixingProfiles[i].AppendCanonical(builder);

            return ComputeHash(builder.ToString());
        }

        internal static string DescribeCompatibilityMismatch(
            CustomProductManifestData host,
            CustomProductManifestData local)
        {
            int hostIndex = 0;
            int localIndex = 0;
            while (hostIndex < host.Entries.Length &&
                   localIndex < local.Entries.Length)
            {
                CustomProductManifestEntryData hostEntry = host.Entries[hostIndex];
                CustomProductManifestEntryData localEntry = local.Entries[localIndex];
                int comparison = CustomProductManifestEntryData.Compare(hostEntry, localEntry);
                if (comparison < 0)
                    return "local content is missing host product '" + hostEntry.ProductId + "'";
                if (comparison > 0)
                    return "local product '" + localEntry.ProductId + "' is not registered by the host";

                string? entryFailure = hostEntry.DescribeMismatch(localEntry);
                if (entryFailure != null)
                    return "product '" + hostEntry.ProductId + "' " + entryFailure;

                hostIndex++;
                localIndex++;
            }

            if (hostIndex < host.Entries.Length)
                return "local content is missing host product '" + host.Entries[hostIndex].ProductId + "'";
            if (localIndex < local.Entries.Length)
                return "local product '" + local.Entries[localIndex].ProductId + "' is not registered by the host";

            int profileIndex = 0;
            while (profileIndex < host.MixingProfiles.Length && profileIndex < local.MixingProfiles.Length)
            {
                CustomProductMixingProfileManifestEntryData hostProfile = host.MixingProfiles[profileIndex];
                CustomProductMixingProfileManifestEntryData localProfile = local.MixingProfiles[profileIndex];
                if (CustomProductMixingProfileManifestEntryData.Compare(hostProfile, localProfile) != 0)
                    return "mixing profile registrations differ";
                string? profileFailure = hostProfile.DescribeMismatch(localProfile);
                if (profileFailure != null)
                    return "mixing profile '" + hostProfile.ProductKindId + "' " + profileFailure;
                profileIndex++;
            }

            if (profileIndex < host.MixingProfiles.Length || profileIndex < local.MixingProfiles.Length)
                return "mixing profile registrations differ";
            return "manifest hash differs despite matching scalar identities";
        }

        internal static string ComputeHash(string value)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(value);
            using SHA256 hash = SHA256.Create();
            byte[] digest = hash.ComputeHash(bytes);
            var result = new StringBuilder(digest.Length * 2);
            for (int i = 0; i < digest.Length; i++)
                result.Append(digest[i].ToString("x2", CultureInfo.InvariantCulture));
            return result.ToString();
        }

        internal static bool IsBoundedIdentifier(string? value)
        {
            if (string.IsNullOrWhiteSpace(value) ||
                value.Length > MaximumIdentifierLength ||
                !string.Equals(value, value.Trim(), StringComparison.Ordinal))
            {
                return false;
            }

            for (int i = 0; i < value.Length; i++)
            {
                if (char.IsControl(value[i]))
                    return false;
            }

            return true;
        }

        internal static bool IsSessionId(string? value) =>
            value != null && value.Length == 32 && IsLowerHex(value);

        internal static bool IsHash(string? value) =>
            value != null && value.Length == 64 && IsLowerHex(value);

        private static bool IsLowerHex(string value)
        {
            for (int i = 0; i < value.Length; i++)
            {
                char character = value[i];
                if ((character < '0' || character > '9') &&
                    (character < 'a' || character > 'f'))
                {
                    return false;
                }
            }

            return true;
        }
    }

    /// <summary>INTERNAL: One deterministic scalar custom-product manifest entry.</summary>
    internal sealed class CustomProductManifestEntryData
    {
        public string ProductId = string.Empty;
        public string OwnerId = string.Empty;
        public string ProductKindId = string.Empty;
        public int CompatibilityDrugType;
        public int DescriptorFormatVersion;
        public string? ProviderId;
        public int ProviderVersion;
        public bool ProviderAvailable;
        public string RepresentationTemplateId = string.Empty;
        public string PresentationProfileId = string.Empty;
        public string ConsumptionProfileProviderId = string.Empty;
        public int ConsumptionProfileProviderVersion;
        public string[] PackagingIds = Array.Empty<string>();
        public string CompatibilityHash = string.Empty;

        internal static CustomProductManifestEntryData Create(
            CustomProductDefinitionRegistration registration)
        {
            CustomProductSaveDescriptorData descriptor = registration.SaveDescriptor
                ?? throw new InvalidOperationException("Custom product has no descriptor.");
            CustomProductDefinitionMetadata metadata = registration.Metadata
                ?? throw new InvalidOperationException("Custom product has no metadata.");

            var packagingIds = new List<string>(metadata.ValidPackaging.Count);
            for (int i = 0; i < metadata.ValidPackaging.Count; i++)
                packagingIds.Add(metadata.ValidPackaging[i].ID);
            packagingIds.Sort(StringComparer.OrdinalIgnoreCase);

            ProductPresentationProfileRegistry.TryGetManifestIdentity(
                registration.ProductId,
                metadata.ProductKind.Id,
                out string presentationProfileId);
            ProductConsumptionProfileRegistrationRegistry.TryGetManifestIdentity(
                registration.ProductId,
                metadata.ProductKind.Id,
                out string consumptionProfileProviderId,
                out int consumptionProfileProviderVersion);

            var entry = new CustomProductManifestEntryData
            {
                ProductId = registration.ProductId,
                OwnerId = registration.OwnerId,
                ProductKindId = metadata.ProductKind.Id,
                CompatibilityDrugType = descriptor.CompatibilityDrugType,
                DescriptorFormatVersion = descriptor.FormatVersion,
                ProviderId = descriptor.ProviderId,
                ProviderVersion = descriptor.ProviderVersion,
                ProviderAvailable = CustomProductSavePersistence.IsProviderAvailable(
                    descriptor.ProviderId,
                    descriptor.ProviderVersion),
                RepresentationTemplateId = descriptor.RepresentationTemplateId,
                PresentationProfileId = presentationProfileId,
                ConsumptionProfileProviderId = consumptionProfileProviderId,
                ConsumptionProfileProviderVersion = consumptionProfileProviderVersion,
                PackagingIds = packagingIds.ToArray()
            };
            entry.CompatibilityHash = ComputeCompatibilityHash(entry, descriptor);
            return entry;
        }

        internal bool IsValid()
        {
            if (!CustomProductManifestData.IsBoundedIdentifier(ProductId) ||
                !CustomProductManifestData.IsBoundedIdentifier(OwnerId) ||
                !CustomProductManifestData.IsBoundedIdentifier(ProductKindId) ||
                 !CustomProductManifestData.IsBoundedIdentifier(RepresentationTemplateId) ||
                 PresentationProfileId == null ||
                 PresentationProfileId.Length > CustomProductManifestData.MaximumIdentifierLength ||
                 ConsumptionProfileProviderId == null ||
                 (ConsumptionProfileProviderId.Length != 0 &&
                  !CustomProductManifestData.IsBoundedIdentifier(ConsumptionProfileProviderId)) ||
                 (ConsumptionProfileProviderId.Length == 0 &&
                  ConsumptionProfileProviderVersion != 0) ||
                 (ConsumptionProfileProviderId.Length != 0 &&
                  ConsumptionProfileProviderVersion <= 0) ||
                 (ProviderId != null && !CustomProductManifestData.IsBoundedIdentifier(ProviderId)) ||
                 !Enum.IsDefined(typeof(DrugType), CompatibilityDrugType) ||
                 (ProviderId == null && ProviderVersion != 0) ||
                 ProviderVersion < 0 ||
                 DescriptorFormatVersion != CustomProductSavePersistence.CurrentFormatVersion ||
                PackagingIds == null ||
                PackagingIds.Length > CustomProductManifestData.MaximumPackagingCount ||
                !CustomProductManifestData.IsHash(CompatibilityHash))
            {
                return false;
            }

            var packaging = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < PackagingIds.Length; i++)
            {
                if (!CustomProductManifestData.IsBoundedIdentifier(PackagingIds[i]) ||
                    !packaging.Add(PackagingIds[i]) ||
                    (i > 0 && StringComparer.OrdinalIgnoreCase.Compare(
                        PackagingIds[i - 1], PackagingIds[i]) >= 0))
                {
                    return false;
                }
            }

            return true;
        }

        internal static int Compare(
            CustomProductManifestEntryData left,
            CustomProductManifestEntryData right) =>
            StringComparer.OrdinalIgnoreCase.Compare(left.ProductId, right.ProductId);

        internal void AppendCanonical(StringBuilder builder, bool includeHash)
        {
            AppendIdentifier(builder, ProductId);
            AppendIdentifier(builder, OwnerId);
            AppendIdentifier(builder, ProductKindId);
            Append(builder, CompatibilityDrugType.ToString(CultureInfo.InvariantCulture));
            Append(builder, DescriptorFormatVersion.ToString(CultureInfo.InvariantCulture));
            AppendIdentifier(builder, ProviderId ?? string.Empty);
            Append(builder, ProviderVersion.ToString(CultureInfo.InvariantCulture));
            Append(builder, ProviderAvailable ? "1" : "0");
            AppendIdentifier(builder, RepresentationTemplateId);
            AppendIdentifier(builder, PresentationProfileId);
            AppendIdentifier(builder, ConsumptionProfileProviderId);
            Append(builder, ConsumptionProfileProviderVersion.ToString(CultureInfo.InvariantCulture));
            for (int i = 0; i < PackagingIds.Length; i++)
                AppendIdentifier(builder, PackagingIds[i]);
            if (includeHash)
                Append(builder, CompatibilityHash);
        }

        internal string? DescribeMismatch(CustomProductManifestEntryData local)
        {
            if (!string.Equals(OwnerId, local.OwnerId, StringComparison.OrdinalIgnoreCase))
                return "owner ID differs";
            if (!string.Equals(ProductKindId, local.ProductKindId, StringComparison.OrdinalIgnoreCase) ||
                CompatibilityDrugType != local.CompatibilityDrugType)
            {
                return "logical kind or native compatibility mapping differs";
            }
            if (DescriptorFormatVersion != local.DescriptorFormatVersion)
            {
                return "descriptor format version differs (host " +
                       DescriptorFormatVersion.ToString(CultureInfo.InvariantCulture) +
                       ", local " +
                       local.DescriptorFormatVersion.ToString(CultureInfo.InvariantCulture) +
                       ")";
            }
            if (!string.Equals(ProviderId, local.ProviderId, StringComparison.OrdinalIgnoreCase) ||
                ProviderVersion != local.ProviderVersion ||
                ProviderAvailable != local.ProviderAvailable)
            {
                return "provider compatibility differs (host '" +
                       (ProviderId ?? "<none>") +
                       "' version " +
                       ProviderVersion.ToString(CultureInfo.InvariantCulture) +
                       " available " +
                       ProviderAvailable.ToString(CultureInfo.InvariantCulture) +
                       ", local '" +
                       (local.ProviderId ?? "<none>") +
                       "' version " +
                       local.ProviderVersion.ToString(CultureInfo.InvariantCulture) +
                       " available " +
                       local.ProviderAvailable.ToString(CultureInfo.InvariantCulture) +
                       ")";
            }
            if (!string.Equals(
                    RepresentationTemplateId,
                    local.RepresentationTemplateId,
                    StringComparison.OrdinalIgnoreCase))
            {
                return "native representation template differs";
            }
            if (!string.Equals(
                    PresentationProfileId,
                    local.PresentationProfileId,
                    StringComparison.OrdinalIgnoreCase))
            {
                return "presentation profile registration differs";
            }
            if (!string.Equals(
                    ConsumptionProfileProviderId,
                    local.ConsumptionProfileProviderId,
                    StringComparison.OrdinalIgnoreCase) ||
                ConsumptionProfileProviderVersion != local.ConsumptionProfileProviderVersion)
            {
                return "consumption profile provider compatibility differs";
            }
            if (PackagingIds.Length != local.PackagingIds.Length)
                return "packaging registration set differs";
            for (int i = 0; i < PackagingIds.Length; i++)
            {
                if (!string.Equals(
                        PackagingIds[i],
                        local.PackagingIds[i],
                        StringComparison.OrdinalIgnoreCase))
                {
                    return "packaging registration set differs";
                }
            }
            if (!string.Equals(
                    CompatibilityHash,
                    local.CompatibilityHash,
                    StringComparison.Ordinal))
            {
                return "descriptor scalar or provider-data hash differs";
            }

            return null;
        }

        private static string ComputeCompatibilityHash(
            CustomProductManifestEntryData entry,
            CustomProductSaveDescriptorData descriptor)
        {
            var builder = new StringBuilder();
            entry.AppendCanonical(builder, includeHash: false);
            Append(builder, descriptor.ProductName);
            Append(builder, descriptor.Description);
            Append(builder, descriptor.InitialPrice.ToString("R", CultureInfo.InvariantCulture));
            Append(builder, descriptor.LegalStatus.ToString(CultureInfo.InvariantCulture));
            Append(builder, descriptor.BaseAddictiveness.ToString("R", CultureInfo.InvariantCulture));
            Append(builder, descriptor.DefaultQuality.ToString(CultureInfo.InvariantCulture));
            Append(builder, descriptor.PlayerEffectDurationSeconds.ToString(CultureInfo.InvariantCulture));
            Append(builder, descriptor.NpcEffectDurationSeconds.ToString(CultureInfo.InvariantCulture));
            Append(builder, descriptor.HasGeneratedMixColor ? "1" : "0");
            if (descriptor.HasGeneratedMixColor)
            {
                Append(builder, descriptor.GeneratedMixColorR.ToString(CultureInfo.InvariantCulture));
                Append(builder, descriptor.GeneratedMixColorG.ToString(CultureInfo.InvariantCulture));
                Append(builder, descriptor.GeneratedMixColorB.ToString(CultureInfo.InvariantCulture));
                Append(builder, descriptor.GeneratedMixColorA.ToString(CultureInfo.InvariantCulture));
            }
            for (int i = 0; i < descriptor.PropertyIds.Length; i++)
                AppendIdentifier(builder, descriptor.PropertyIds[i]);
            // Provider data remains local; only its deterministic digest participates in compatibility.
            Append(builder, CustomProductManifestData.ComputeHash(descriptor.ProviderData));
            return CustomProductManifestData.ComputeHash(builder.ToString());
        }

        private static void Append(StringBuilder builder, string value)
        {
            builder.Append(value.Length).Append(':').Append(value).Append('|');
        }

        private static void AppendIdentifier(StringBuilder builder, string value)
        {
            Append(builder, value.ToUpperInvariant());
        }
    }
}

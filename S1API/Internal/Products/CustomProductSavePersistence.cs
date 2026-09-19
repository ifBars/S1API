#if IL2CPPMELON
using S1Persistence = Il2CppScheduleOne.Persistence;
using S1Product = Il2CppScheduleOne.Product;
using S1Registry = Il2CppScheduleOne.Registry;
using NativeEffect = Il2CppScheduleOne.Effects.Effect;
using NativePackagingDefinition = Il2CppScheduleOne.Product.Packaging.PackagingDefinition;
#elif MONOMELON
using S1Persistence = ScheduleOne.Persistence;
using S1Product = ScheduleOne.Product;
using S1Registry = ScheduleOne.Registry;
using NativeEffect = ScheduleOne.Effects.Effect;
using NativePackagingDefinition = ScheduleOne.Product.Packaging.PackagingDefinition;
#endif

using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using S1API.Products;
using S1API.Internal.Properties;
using UnityEngine;

namespace S1API.Internal.Products
{
    /// <summary>INTERNAL: Owns custom-product descriptor persistence and early restoration.</summary>
    internal static class CustomProductSavePersistence
    {
        internal const string RelativePath = "Modded/CustomProducts.json";
        internal const int CurrentFormatVersion = 1;
        internal const int MaximumDescriptorCount = 256;
        internal const int MaximumStringLength = 4096;
        private static readonly object Gate = new object();
        private static readonly Dictionary<string, ICustomProductSaveProvider> Providers =
            new Dictionary<string, ICustomProductSaveProvider>(StringComparer.OrdinalIgnoreCase);
        private static string? _restoredSaveFolder;

        internal static ICustomProductSaveProvider RegisterProvider(ICustomProductSaveProvider provider)
        {
            if (provider == null) throw new ArgumentNullException(nameof(provider));
            string id = ProductKindId.Normalize(provider.ProviderId, nameof(provider.ProviderId));
            if (provider.MaximumDescriptorVersion < 1)
                throw new ArgumentOutOfRangeException(nameof(provider.MaximumDescriptorVersion));
            lock (Gate)
            {
                if (Providers.TryGetValue(id, out ICustomProductSaveProvider? existing))
                {
                    if (!ReferenceEquals(existing, provider))
                        throw new InvalidOperationException($"Custom-product save provider '{id}' is already registered.");
                    return existing;
                }
                Providers.Add(id, provider);
                return provider;
            }
        }

        internal static bool IsProviderAvailable(
            string? providerId,
            int descriptorVersion)
        {
            if (string.IsNullOrEmpty(providerId))
                return true;

            lock (Gate)
            {
                return Providers.TryGetValue(
                           providerId,
                           out ICustomProductSaveProvider? provider) &&
                       descriptorVersion <= provider.MaximumDescriptorVersion;
            }
        }

        internal static void Save(string saveFolderPath)
        {
            CustomProductSaveDescriptorData[] descriptors = CustomProductDefinitionRegistry.GetSaveDescriptors();
            string path = Path.Combine(saveFolderPath, "Modded", "CustomProducts.json");
            // Do not delete a descriptor file when its content mod is absent. Keeping it makes a
            // later reinstall recoverable and leaves vanilla-only saves untouched.
            if (descriptors.Length == 0) return;
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            string temporaryPath = path + ".tmp";
            File.WriteAllText(temporaryPath, JsonConvert.SerializeObject(new CustomProductSaveFileData { Descriptors = descriptors }, Formatting.None));
            if (File.Exists(path))
                File.Replace(temporaryPath, path, null);
            else
                File.Move(temporaryPath, path);
        }

        internal static void RestoreBeforeBaseLoaders(S1Persistence.LoadManager loadManager)
        {
            if (loadManager == null || string.IsNullOrEmpty(loadManager.LoadedGameFolderPath)) return;
            string saveFolder = loadManager.LoadedGameFolderPath;
            lock (Gate)
            {
                if (string.Equals(
                        _restoredSaveFolder,
                        saveFolder,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                _restoredSaveFolder = saveFolder;
            }
            try
            {
                string path = Path.Combine(loadManager.LoadedGameFolderPath, "Modded", "CustomProducts.json");
                if (!File.Exists(path)) return;
                CustomProductSaveFileData? file = JsonConvert.DeserializeObject<CustomProductSaveFileData>(File.ReadAllText(path));
                if (file == null || file.FormatVersion != CurrentFormatVersion || file.Descriptors == null || file.Descriptors.Length > MaximumDescriptorCount)
                {
                    Warn("descriptor file is incompatible or exceeds the supported descriptor count; custom products were skipped");
                    return;
                }
                var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (CustomProductSaveDescriptorData data in file.Descriptors)
                {
                    if (!TryValidate(data, out CustomProductSaveDescriptor descriptor) || !seen.Add(descriptor.ProductId))
                    {
                        Warn("ignored an invalid or duplicate custom-product descriptor");
                        continue;
                    }
                    RestoreOne(descriptor);
                }
            }
            catch (Exception exception)
            {
                Warn("could not read custom-product descriptors; continuing without them: " + exception.Message);
            }
        }

        private static void RestoreOne(CustomProductSaveDescriptor descriptor)
        {
            if (CustomProductDefinitionRegistry.IsRegistered(descriptor.ProductId))
                return;
            if (string.IsNullOrEmpty(descriptor.ProviderId))
            {
                RestoreFallback(descriptor);
                return;
            }
            ICustomProductSaveProvider? provider;
            lock (Gate) Providers.TryGetValue(descriptor.ProviderId, out provider);
            if (provider == null)
            {
                Warn($"custom product '{descriptor.ProductId}' requires provider '{descriptor.ProviderId}', which is not installed; it was skipped safely");
                RestoreFallback(descriptor);
                return;
            }
            if (descriptor.ProviderVersion > provider.MaximumDescriptorVersion)
            {
                Warn($"custom product '{descriptor.ProductId}' needs a newer '{descriptor.ProviderId}' provider; it was skipped safely");
                RestoreFallback(descriptor);
                return;
            }
            try
            {
                CustomProductDefinitionBuilder? builder = provider.Restore(descriptor);
                if (builder != null)
                {
                    if (!string.Equals(builder.ProductId, descriptor.ProductId, StringComparison.OrdinalIgnoreCase))
                        throw new InvalidOperationException("provider returned a different product ID; automatic renamed-ID migration is not supported");
                    builder.Build();
                }
                else
                {
                    RestoreFallback(descriptor);
                }
            }
            catch (Exception exception)
            {
                Warn($"custom product '{descriptor.ProductId}' could not be reconstructed by '{descriptor.ProviderId}'; it was skipped safely: {exception.Message}");
                RestoreFallback(descriptor);
            }
        }

        private static void RestoreFallback(CustomProductSaveDescriptor descriptor)
        {
            try
            {
                CustomProductSaveDescriptorData data = descriptor.Data
                    ?? throw new InvalidOperationException("descriptor has no internal scalar data");
                S1Product.ProductDefinition? template = S1Registry.GetItem(data.RepresentationTemplateId) as S1Product.ProductDefinition;
                if (template == null)
                    throw new InvalidOperationException("the saved vanilla representation template is unavailable");
                ProductKind kind = ProductKindRegistry.Get(data.ProductKindId) ??
                    ProductKindRegistry.Register(new ProductKind(data.ProductKindId, null));
                List<NativeEffect> properties = PropertyResolver.ResolveToGamePropertiesById(data.PropertyIds ?? Array.Empty<string>());
                var packaging = new List<NativePackagingDefinition>();
                foreach (string packagingId in data.PackagingIds ?? Array.Empty<string>())
                {
                    NativePackagingDefinition? item = S1Registry.GetItem(packagingId) as NativePackagingDefinition;
                    if (item == null)
                    {
                        Warn("saved packaging '" + packagingId + "' is unavailable; restoring the product without that packaging");
                        continue;
                    }
                    packaging.Add(item);
                }
                var native = CustomProductDefinitionFactory.Create(
                    data.ProductId, data.ProductName, data.Description, data.InitialPrice,
                    (global::S1API.Items.LegalStatus)data.LegalStatus, data.BaseAddictiveness,
                    data.PlayerEffectDurationSeconds, data.NpcEffectDurationSeconds,
                    (DrugType)data.CompatibilityDrugType, properties,
                    packaging, template);
                var packagingMetadata = new List<PackagingDefinition>();
                foreach (NativePackagingDefinition item in packaging)
                    packagingMetadata.Add(new PackagingDefinition(item));
                Color32? generatedMixColor = data.HasGeneratedMixColor
                    ? new Color32(
                        data.GeneratedMixColorR,
                        data.GeneratedMixColorG,
                        data.GeneratedMixColorB,
                        data.GeneratedMixColorA)
                    : null;
                var metadata = new CustomProductDefinitionMetadata(
                    kind,
                    (Quality)data.DefaultQuality,
                    packagingMetadata,
                    template,
                    generatedMixColor);
                try
                {
                    CustomProductDefinitionRegistry.Register(data.OwnerId, data.ProductId, data.ProductName, data.InitialPrice, native, metadata, data);
                    Warn($"custom product '{data.ProductId}' used S1API's asset-free fallback; reinstall '{descriptor.ProviderId ?? data.OwnerId}' to restore provider presentation and content");
                }
                catch
                {
                    CustomProductDefinitionFactory.Destroy(native);
                    throw;
                }
            }
            catch (Exception exception)
            {
                Warn($"custom product '{descriptor.ProductId}' fallback was unavailable; load continues safely: {exception.Message}");
            }
        }

        private static bool TryValidate(CustomProductSaveDescriptorData? data, out CustomProductSaveDescriptor descriptor)
        {
            descriptor = null!;
            if (data == null || data.FormatVersion != CurrentFormatVersion || data.ProviderVersion < 0 ||
                !IsBounded(data.ProductId) || !IsBounded(data.OwnerId) || !IsBounded(data.ProviderData) ||
                (data.ProviderId != null && !IsBounded(data.ProviderId))) return false;
            try
            {
                string productId = ProductKindId.Normalize(data.ProductId, nameof(data.ProductId));
                string ownerId = data.OwnerId.Trim();
                string? providerId = data.ProviderId == null ? null : ProductKindId.Normalize(data.ProviderId, nameof(data.ProviderId));
                if (!IsBounded(data.ProductName) || !IsBounded(data.Description) || !IsBounded(data.ProductKindId) || !IsBounded(data.RepresentationTemplateId) ||
                    !float.IsFinite(data.InitialPrice) || !float.IsFinite(data.BaseAddictiveness) || data.InitialPrice < 1 ||
                    data.BaseAddictiveness < 0 || data.BaseAddictiveness > 1 || data.PlayerEffectDurationSeconds < 0 || data.NpcEffectDurationSeconds < 0 ||
                    !Enum.IsDefined(typeof(global::S1API.Items.LegalStatus), data.LegalStatus) || !Enum.IsDefined(typeof(Quality), data.DefaultQuality) || !Enum.IsDefined(typeof(DrugType), data.CompatibilityDrugType) ||
                    !IsBoundedCollection(data.PropertyIds) || !IsBoundedCollection(data.PackagingIds)) return false;
                string kindId = ProductKindId.Normalize(data.ProductKindId, nameof(data.ProductKindId));
                if (string.IsNullOrWhiteSpace(data.RepresentationTemplateId))
                    return false;
                descriptor = new CustomProductSaveDescriptor(data.FormatVersion, productId, ownerId, data.ProductName, data.Description, data.InitialPrice, kindId, providerId, data.ProviderVersion, data.ProviderData) { Data = data };
                return true;
            }
            catch { return false; }
        }
        private static bool IsBounded(string? value) => value != null && value.Length <= MaximumStringLength;
        private static bool IsBoundedCollection(string[]? values)
        {
            if (values == null || values.Length > 32)
                return false;
            foreach (string value in values)
            {
                if (!IsBounded(value))
                    return false;
            }
            return true;
        }

        internal static bool TryRestoreGeneratedDescriptorFromNetwork(
            CustomProductSaveDescriptorData data)
        {
            if (!data.IsGeneratedMix || !string.IsNullOrEmpty(data.ProviderId) ||
                data.ProviderVersion != 0 || !string.IsNullOrEmpty(data.ProviderData) ||
                !TryValidate(data, out CustomProductSaveDescriptor descriptor))
            {
                return false;
            }

            if (!CustomProductDefinitionRegistry.IsRegistered(descriptor.ProductId))
                RestoreFallback(descriptor);
            return CustomProductDefinitionRegistry.IsRegistered(descriptor.ProductId);
        }

        internal static bool IsNetworkGeneratedDescriptorValid(
            CustomProductSaveDescriptorData data)
        {
            return data != null && data.IsGeneratedMix &&
                   string.IsNullOrEmpty(data.ProviderId) &&
                   data.ProviderVersion == 0 &&
                   string.IsNullOrEmpty(data.ProviderData) &&
                   TryValidate(data, out _);
        }
        private static void Warn(string message) { try { MelonLoader.MelonLogger.Warning("[CustomProductSave] " + message); } catch { } }
    }
}

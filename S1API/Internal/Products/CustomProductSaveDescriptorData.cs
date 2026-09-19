using System;

namespace S1API.Internal.Products
{
    [Serializable]
    internal sealed class CustomProductSaveDescriptorData
    {
        public int FormatVersion = 1;
        public string ProductId = string.Empty;
        public string OwnerId = string.Empty;
        public string ProductName = string.Empty;
        public string Description = string.Empty;
        public float InitialPrice;
        public int LegalStatus;
        public float BaseAddictiveness;
        public int DefaultQuality;
        public string ProductKindId = string.Empty;
        public int CompatibilityDrugType;
        public string RepresentationTemplateId = string.Empty;
        public int PlayerEffectDurationSeconds;
        public int NpcEffectDurationSeconds;
        public string[] PropertyIds = Array.Empty<string>();
        public string[] PackagingIds = Array.Empty<string>();
        public string? ProviderId;
        public int ProviderVersion;
        public string ProviderData = string.Empty;
        public bool IsGeneratedMix;
        public bool HasGeneratedMixColor;
        public byte GeneratedMixColorR;
        public byte GeneratedMixColorG;
        public byte GeneratedMixColorB;
        public byte GeneratedMixColorA;
    }

    [Serializable]
    internal sealed class CustomProductSaveFileData
    {
        public int FormatVersion = 1;
        public CustomProductSaveDescriptorData[] Descriptors = Array.Empty<CustomProductSaveDescriptorData>();
    }
}

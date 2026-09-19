using S1API.Internal.Rendering;

namespace S1API.Tests.Entities;

public sealed class AvatarAccessoryDiagnosticTests
{
    [Fact]
    public void OwnerDescriptionIncludesStableNpcContext()
    {
        string description = AvatarAccessoryDiagnostics.FormatOwnerDescription(
            "Bobby Cooley",
            "bobby_cooley",
            "S1API_BobbyCooley(Clone)");

        Assert.Equal(
            "'Bobby Cooley' (ID='bobby_cooley', GameObject='S1API_BobbyCooley')",
            description);
    }

    [Fact]
    public void OwnerDescriptionUsesExplicitPlaceholdersForMissingIdentity()
    {
        string description = AvatarAccessoryDiagnostics.FormatOwnerDescription(null, null, null);

        Assert.Equal(
            "'<unknown>' (ID='<unknown-id>', GameObject='<unknown-object>')",
            description);
    }

    [Fact]
    public void StableOwnerKeyPrefersIdOverMutableObjectContext()
    {
        string prefabKey = AvatarAccessoryDiagnostics.SelectStableOwnerKey(
            "s1api_smoke:accessory_diagnostic",
            "Accessory",
            "runtime description");

        Assert.Equal("id:s1api_smoke:accessory_diagnostic", prefabKey);
    }

    [Fact]
    public void StableOwnerKeyNormalizesPrefabCloneNameWithoutId()
    {
        string prefabKey = AvatarAccessoryDiagnostics.SelectStableOwnerKey(
            null,
            "S1API_DiagnosticNpc(Clone)",
            "runtime description");

        Assert.Equal("prefab:S1API_DiagnosticNpc", prefabKey);
    }
}

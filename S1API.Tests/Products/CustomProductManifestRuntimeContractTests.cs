using S1API.Internal.Products;

namespace S1API.Tests.Products;

public sealed class CustomProductManifestRuntimeContractTests
{
    [Fact]
    public void ClientManifestDeadlineStartsOnlyForFirstDeferredRequest()
    {
        Assert.True(CustomProductManifestRuntime.ShouldStartClientManifestDeadline(
            authorized: false,
            currentDeadline: DateTime.MaxValue));
        Assert.False(CustomProductManifestRuntime.ShouldStartClientManifestDeadline(
            authorized: false,
            currentDeadline: DateTime.UtcNow));
        Assert.False(CustomProductManifestRuntime.ShouldStartClientManifestDeadline(
            authorized: true,
            currentDeadline: DateTime.MaxValue));
    }

    [Fact]
    public void QueuedManifestExemptsClientFromMissingManifestTimeout()
    {
        DateTime deadline = DateTime.UtcNow;

        Assert.False(CustomProductManifestRuntime.ShouldRejectClientForMissingManifest(
            isWaiting: true,
            manifestReceived: true,
            now: deadline,
            deadline: deadline));
        Assert.True(CustomProductManifestRuntime.ShouldRejectClientForMissingManifest(
            isWaiting: true,
            manifestReceived: false,
            now: deadline,
            deadline: deadline));
    }

    [Fact]
    public void EmptyManifestDoesNotRequireValidation()
    {
        Assert.False(CustomProductManifestRuntime.RequiresValidation(
            new CustomProductManifestData()));
    }

    [Fact]
    public void DescriptorBackedProductRequiresValidation()
    {
        var manifest = new CustomProductManifestData
        {
            Entries = [new CustomProductManifestEntryData()]
        };

        Assert.True(CustomProductManifestRuntime.RequiresValidation(manifest));
    }

    [Fact]
    public void MixingOnlyManifestRequiresValidation()
    {
        var manifest = new CustomProductManifestData
        {
            MixingProfiles =
                [new CustomProductMixingProfileManifestEntryData()]
        };

        Assert.True(CustomProductManifestRuntime.RequiresValidation(manifest));
    }
}

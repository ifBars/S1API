using S1API.Internal.Entities;

namespace S1API.Tests.Entities;

public sealed class NPCRelationshipPersistencePolicyTests
{
    [Theory]
    [InlineData(0f)]
    [InlineData(1.5f)]
    [InlineData(2f)]
    [InlineData(3.5f)]
    [InlineData(5f)]
    public void FiniteSavedDeltasAreAuthoritative(float relationDelta)
    {
        Assert.True(
            NPCRelationshipPersistencePolicy.IsValidSavedDelta(relationDelta));
    }

    [Fact]
    public void NonFiniteSavedDeltasAreRejected()
    {
        Assert.False(
            NPCRelationshipPersistencePolicy.IsValidSavedDelta(float.NaN));
        Assert.False(
            NPCRelationshipPersistencePolicy.IsValidSavedDelta(float.PositiveInfinity));
        Assert.False(
            NPCRelationshipPersistencePolicy.IsValidSavedDelta(float.NegativeInfinity));
    }

    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public void DefaultsApplyOnlyWithoutSavedRelationshipState(
        bool relationshipLoadedFromSave,
        bool expected)
    {
        Assert.Equal(
            expected,
            NPCRelationshipPersistencePolicy.ShouldApplyDefaults(
                relationshipLoadedFromSave));
    }
}

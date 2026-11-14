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

    [Theory]
    [InlineData(0f, 0f)]
    [InlineData(1f, 0.2f)]
    [InlineData(2f, 0.4f)]
    [InlineData(2.5f, 0.5f)]
    [InlineData(5f, 1f)]
    public void ConfiguredDeltaMapsToNativeDefaultRelationshipValue(
        float relationDelta,
        float expected)
    {
        Assert.Equal(
            expected,
            NPCRelationshipPersistencePolicy.ToNativeDefaultRelationshipValue(relationDelta),
            precision: 5);
    }

    [Theory]
    [InlineData(-1f, 0f)]
    [InlineData(7f, 1f)]
    public void OutOfRangeDeltaIsClampedLikeTheBuilder(
        float relationDelta,
        float expected)
    {
        Assert.Equal(
            expected,
            NPCRelationshipPersistencePolicy.ToNativeDefaultRelationshipValue(relationDelta),
            precision: 5);
    }
}

namespace S1API.Tests.Entities;

public sealed class BuildingLookupPolicyTests
{
    private const string CasinoObsoleteMessage =
        "Casino is not an enterable building and cannot be resolved. This compatibility identifier may be removed in a future S1API version.";

    [Fact]
    public void CasinoIdentifier_RemainsAnObsoleteCompatibilityShim()
    {
#pragma warning disable CS0618 // Verify legacy callers can still use the typed building lookup.
        System.Type casinoType = typeof(global::S1API.Map.Buildings.Casino);
        System.Func<global::S1API.Map.Building?> getCasino = global::S1API.Map.Building.Get<global::S1API.Map.Buildings.Casino>;
#pragma warning restore CS0618

        var obsolete = Assert.Single(casinoType.GetCustomAttributes(typeof(System.ObsoleteAttribute), inherit: false));
        var obsoleteAttribute = Assert.IsType<System.ObsoleteAttribute>(obsolete);

        Assert.Equal(CasinoObsoleteMessage, obsoleteAttribute.Message);
        Assert.False(obsoleteAttribute.IsError);
        Assert.Contains(typeof(global::S1API.Map.Buildings.IBuildingIdentifier), casinoType.GetInterfaces());
        Assert.NotNull(getCasino);
    }

    [Theory]
    [InlineData(true, false, true)]
    [InlineData(true, true, true)]
    [InlineData(false, false, true)]
    [InlineData(false, true, false)]
    public void TypedBuildingLookup_DefersUntilTheMapIsReady(
        bool isMenuScene,
        bool isMainSceneReady,
        bool expected)
    {
        Assert.Equal(
            expected,
            global::S1API.Map.Building.ShouldDeferTypedLookup(isMenuScene, isMainSceneReady));
    }
}

public sealed class CustomNpcResidenceSummonPolicyTests
{
    [Theory]
    [InlineData(true, true, true, true)]
    [InlineData(false, true, true, false)]
    [InlineData(true, false, true, false)]
    [InlineData(true, true, false, false)]
    public void SummonCompletion_OnlyExitsAnAuthoritativeCustomNpcThatIsInside(
        bool isServer,
        bool isCustomNpc,
        bool isInsideBuilding,
        bool expected)
    {
        Assert.Equal(
            expected,
            global::S1API.Internal.Patches.NPCPatches.ShouldExitCustomNpcAfterSummon(
                isServer,
                isCustomNpc,
                isInsideBuilding));
    }

    [Theory]
    [InlineData(true, true, true, true)]
    [InlineData(false, true, true, false)]
    [InlineData(true, false, true, false)]
    [InlineData(true, true, false, false)]
    public void ResidenceReentry_IsSuppressedOnlyDuringAnAuthoritativeCustomNpcSummon(
        bool isServer,
        bool isCustomNpc,
        bool isSummonBehaviourEnabled,
        bool expected)
    {
        Assert.Equal(
            expected,
            global::S1API.Internal.Patches.NPCPatches.ShouldSuppressResidenceReentry(
                isServer,
                isCustomNpc,
                isSummonBehaviourEnabled));
    }

    [Fact]
    public void SummonLogicLookup_UsesGeneratedNamePrefixAndNativeSignature()
    {
        var method = global::S1API.Internal.Patches.NPCPatches.FindSummonLogicMethod(
            typeof(SummonLogicFixture));

        Assert.NotNull(method);
        Assert.Equal(nameof(SummonLogicFixture.RpcLogic___Summon_123), method!.Name);
    }

    private sealed class SummonLogicFixture
    {
        public void RpcLogic___Summon_123(string buildingGuid, int doorIndex, float duration) { }

        public void RpcLogic___Summon_456(string buildingGuid, int doorIndex) { }
    }
}

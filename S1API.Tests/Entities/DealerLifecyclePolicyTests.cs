using S1API.Entities;
using S1API.Entities.Relation;
using S1API.Internal.Entities;

namespace S1API.Tests.Entities;

public sealed class DealerLifecyclePolicyTests
{
    [Theory]
    [InlineData(true, true, true, true)]
    [InlineData(false, true, true, false)]
    [InlineData(true, false, true, false)]
    [InlineData(true, true, false, false)]
    public void RecruitmentRequiresTheCompleteNativeDealerDialogueSet(
        bool hasRecruitDialogue,
        bool hasCollectCashDialogue,
        bool hasAssignCustomersDialogue,
        bool expected)
    {
        Assert.Equal(
            expected,
            NPCDataAccess.HasCompleteDealerDialogueSet(
                hasRecruitDialogue,
                hasCollectCashDialogue,
                hasAssignCustomersDialogue));
    }

    [Fact]
    public void DealerDialogueFallbackUsesTheNative046AssetNames()
    {
        Assert.Equal("Supplier_Recruitment", NPCDataAccess.DealerRecruitDialogueName);
        Assert.Equal("Dealer_CollectCash", NPCDataAccess.DealerCollectCashDialogueName);
        Assert.Equal("Dealer_AssignCustomers", NPCDataAccess.DealerAssignCustomersDialogueName);
    }

    [Fact]
    public void DealerDealBehaviourMatchesNativePriority()
    {
        Assert.Equal(5, NPCPrefabBuilder.DealerAttendDealPriority);
    }

    [Fact]
    public void BehaviourStackObjectsRemainActiveWhileTheirInternalStateIsManaged()
    {
        Assert.True(NPCPrefabBuilder.BehaviourObjectsRemainActive);
    }

    [Theory]
    [InlineData("DealerHomeEvent", true)]
    [InlineData("HomeEvent", true)]
    [InlineData("StayInBuilding", false)]
    [InlineData("StayInBuilding_1", false)]
    public void DealerHomeEventNeverAliasesAConsumerScheduleAction(string name, bool expected)
    {
        Assert.Equal(expected, NPCPrefabBuilder.IsDealerHomeEventName(name));
    }

    [Fact]
    public void ConnectionIdsAreStableAcrossSpawnOrderReconciliation()
    {
        IReadOnlyList<string> ids = NPCRelationshipDataBuilder.NormalizeConnectionIds(
            new[] { " thomas_elis ", "", "THOMAS_ELIS", "gennaro_salvadore" });

        Assert.Equal(new[] { "thomas_elis", "gennaro_salvadore" }, ids);
    }

    [Fact]
    public void RelationshipReconciliationTreatsOneSidedDeclarationsAsUndirected()
    {
        var declarations = new Dictionary<string, IReadOnlyList<string>>(
            StringComparer.OrdinalIgnoreCase)
        {
            ["dealer_a"] = new[] { "dealer_b" },
            ["dealer_b"] = new[] { "customer_c" },
            ["customer_c"] = Array.Empty<string>()
        };

        Assert.Equal(
            new[] { "customer_c", "dealer_a" },
            NPCRelationshipGraphPolicy.BuildUndirectedConnectionIds("dealer_b", declarations));
        Assert.Equal(
            new[] { "dealer_b" },
            NPCRelationshipGraphPolicy.BuildUndirectedConnectionIds("customer_c", declarations));
    }

    [Fact]
    public void ExplicitlyEmptyConnectionsRemainConfiguredForStaleGraphRemoval()
    {
        var builder = new NPCRelationshipDataBuilder()
            .WithConnectionsById(Array.Empty<string>());

        NPCRelationshipDataBuilder.RelationshipDefaultsData snapshot = builder.CaptureData();

        Assert.True(snapshot.ConnectionsConfigured);
        Assert.Empty(snapshot.ConnectionIDs!);
    }

    [Theory]
    [InlineData(true, false, true)]
    [InlineData(false, true, true)]
    [InlineData(false, false, false)]
    public void ContactIconReadinessIsTrackedPerNpc(
        bool hasExplicitIcon,
        bool generationCompleted,
        bool expected)
    {
        Assert.Equal(
            expected,
            NPCAppearance.IsMugshotReady(hasExplicitIcon, generationCompleted));
    }
}

using System.Reflection;
using NumericsVector3 = System.Numerics.Vector3;
using UnityEngine;

namespace S1API.Tests.Entities;

public sealed class CustomNpcRequestProductPolicyTests
{
    [Fact]
    public void BaseEmployeeNavigation_ExcludesPropertyInteriorOnly()
    {
        const int baseEmployeeMask = 57;
        const int propertyInteriorArea = 5;

        int normalizedMask = global::S1API.Entities.NPC.ExcludeNavMeshArea(
            baseEmployeeMask,
            propertyInteriorArea);

        int civilianMask = global::S1API.Entities.NPC.IncludeNavMeshArea(normalizedMask, 7);

        Assert.Equal(153, civilianMask);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(32)]
    public void BaseEmployeeNavigation_IgnoresInvalidAreaIndices(int areaIndex)
    {
        Assert.Equal(57, global::S1API.Entities.NPC.ExcludeNavMeshArea(57, areaIndex));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(32)]
    public void BaseEmployeeNavigation_IncludeIgnoresInvalidAreaIndices(int areaIndex)
    {
        Assert.Equal(57, global::S1API.Entities.NPC.IncludeNavMeshArea(57, areaIndex));
    }

    [Fact]
    public void FollowDestination_KeepsCustomNpcOutsidePlayerSpace()
    {
        bool overridden = global::S1API.Internal.Patches.NPCPatches.TryCalculateCustomNpcFollowDestination(
            isCustomNpc: true,
            isFollowingPlayer: true,
            playerPosition: NumericsVector3.Zero,
            npcPosition: new NumericsVector3(4f, 0f, 0f),
            fallbackDirection: -NumericsVector3.UnitZ,
            out NumericsVector3 destination);

        Assert.True(overridden);
        Assert.Equal(new NumericsVector3(2.5f, 0f, 0f), destination);
    }

    [Fact]
    public void FollowDestination_UsesFallbackWhenNpcOverlapsPlayer()
    {
        bool overridden = global::S1API.Internal.Patches.NPCPatches.TryCalculateCustomNpcFollowDestination(
            isCustomNpc: true,
            isFollowingPlayer: true,
            playerPosition: NumericsVector3.Zero,
            npcPosition: NumericsVector3.Zero,
            fallbackDirection: -NumericsVector3.UnitZ,
            out NumericsVector3 destination);

        Assert.True(overridden);
        Assert.Equal(new NumericsVector3(0f, 0f, -2.5f), destination);
    }

    [Fact]
    public void FollowDestination_UsesNegativeZWhenNpcAndFallbackDirectionsAreZero()
    {
        bool overridden = global::S1API.Internal.Patches.NPCPatches.TryCalculateCustomNpcFollowDestination(
            isCustomNpc: true,
            isFollowingPlayer: true,
            playerPosition: NumericsVector3.Zero,
            npcPosition: NumericsVector3.Zero,
            fallbackDirection: NumericsVector3.Zero,
            out NumericsVector3 destination);

        Assert.True(overridden);
        Assert.Equal(new NumericsVector3(0f, 0f, -2.5f), destination);
    }

    [Fact]
    public void PropertyApproachDestination_UsesOwnedPropertyExteriorSpawnForCustomInitialApproach()
    {
        var propertyExteriorSpawn = new NumericsVector3(-67f, 0.7f, 81.5f);

        bool overridden = global::S1API.Internal.Patches.NPCPatches
            .TryCalculateCustomNpcPropertyApproachDestination(
                isCustomNpc: true,
                isInitialApproach: true,
                playerInsideOwnedProperty: true,
                propertyExteriorSpawn,
                out NumericsVector3 destination);

        Assert.True(overridden);
        Assert.Equal(propertyExteriorSpawn, destination);
    }

    [Theory]
    [InlineData(false, true, true, true)]
    [InlineData(true, false, true, true)]
    [InlineData(true, true, false, true)]
    [InlineData(true, true, true, false)]
    public void PropertyApproachDestination_PreservesNativeDestinationOutsideCustomOwnedPropertyApproach(
        bool isCustomNpc,
        bool isInitialApproach,
        bool playerInsideOwnedProperty,
        bool hasExteriorSpawnPoint)
    {
        NumericsVector3? propertyExteriorSpawn = hasExteriorSpawnPoint
            ? new NumericsVector3(-67f, 0.7f, 81.5f)
            : null;

        bool overridden = global::S1API.Internal.Patches.NPCPatches
            .TryCalculateCustomNpcPropertyApproachDestination(
                isCustomNpc,
                isInitialApproach,
                playerInsideOwnedProperty,
                propertyExteriorSpawn,
                out _);

        Assert.False(overridden);
    }

    [Theory]
    [InlineData(float.NaN, 0f, 0f)]
    [InlineData(float.PositiveInfinity, 0f, 0f)]
    [InlineData(10001f, 0f, 0f)]
    public void PropertyApproachDestination_RejectsInvalidExteriorSpawn(
        float x,
        float y,
        float z)
    {
        bool overridden = global::S1API.Internal.Patches.NPCPatches
            .TryCalculateCustomNpcPropertyApproachDestination(
                isCustomNpc: true,
                isInitialApproach: true,
                playerInsideOwnedProperty: true,
                new NumericsVector3(x, y, z),
                out _);

        Assert.False(overridden);
    }

    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public void FollowDestination_PreservesNativeBehaviourOutsideCustomFollowPhase(
        bool isCustomNpc,
        bool isFollowingPlayer)
    {
        bool overridden = global::S1API.Internal.Patches.NPCPatches.TryCalculateCustomNpcFollowDestination(
            isCustomNpc,
            isFollowingPlayer,
            NumericsVector3.Zero,
            NumericsVector3.UnitX,
            -NumericsVector3.UnitZ,
            out _);

        Assert.False(overridden);
    }

    [Fact]
    public void DestinationPatchLookup_UsesTheByValueVectorSignature()
    {
        MethodBase? method = global::S1API.Internal.Patches.NPCPatches
            .FindNpcMovementDestinationMethod(typeof(NpcMovementFixture));

        Assert.NotNull(method);
        Assert.Equal(nameof(NpcMovementFixture.SetDestination), method!.Name);
        Assert.False(method.GetParameters()[0].ParameterType.IsByRef);
    }

    [Fact]
    public void DestinationPatch_AvoidsIl2CppOutParameterMethods()
    {
        Type? patchType = typeof(global::S1API.Internal.Patches.NPCPatches).GetNestedType(
            "RequestProductMovementDestinationPatch",
            BindingFlags.NonPublic);
        MethodInfo? prefix = patchType?.GetMethod(
            "Prefix",
            BindingFlags.Static | BindingFlags.NonPublic);

        Assert.NotNull(prefix);
        Assert.Contains(
            prefix!.GetCustomAttributesData(),
            attribute => attribute.AttributeType.FullName == "HarmonyLib.HarmonyPrefix");
        Assert.Null(patchType!.GetMethod("Postfix", BindingFlags.Static | BindingFlags.NonPublic));
    }

    private sealed class NpcMovementFixture
    {
        public void SetDestination(Vector3 destination) { }

        public void SetDestination(Vector3 destination, Action<bool> callback) { }
    }
}

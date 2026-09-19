using System;
using S1API.Entities;
using S1API.Entities.NPCs;
using S1API.Products;
using S1API.Properties.Interfaces;
using Xunit;

namespace S1API.Tests.Products;

public sealed class ProductEffectCallbackTests : IDisposable
{
    public ProductEffectCallbackTests()
    {
        ProductManager.ClearEffectCallbacks();
        ProductManager.ClearNpcEffectCallbacks();
        ProductManager.ResetEffectClearCallbacks();
        ProductManager.ResetNpcEffectClearCallbacks();
    }

    public void Dispose()
    {
        ProductManager.ClearEffectCallbacks();
        ProductManager.ClearNpcEffectCallbacks();
        ProductManager.ResetEffectClearCallbacks();
        ProductManager.ResetNpcEffectClearCallbacks();
    }

    [Fact]
    public void ExistingApplyOnlyPlayerCallbackRemainsIndependentFromClearCallbacks()
    {
        var player = CreatePlayer();
        var applyCount = 0;

        ProductManager.SetEffectCallback("test_apply_only", _ => applyCount++);

        Assert.True(ProductManager.TryInvokeEffectCallback("TEST_APPLY_ONLY", player, out var allowDefaultApply));
        Assert.False(allowDefaultApply);
        Assert.Equal(1, applyCount);

        Assert.False(ProductManager.TryInvokeEffectClearCallback("test_apply_only", player, out var allowDefaultClear));
        Assert.False(allowDefaultClear);
    }

    [Fact]
    public void PlayerClearCallbackInvokesRepeatedlyAndCanAllowNativeFallthrough()
    {
        var player = CreatePlayer();
        var clearCount = 0;

        ProductManager.SetEffectClearCallback("test_player_clear", _ => clearCount++, allowDefaultEffect: true);

        Assert.True(ProductManager.TryInvokeEffectClearCallback("TEST_PLAYER_CLEAR", player, out var allowDefaultFirst));
        Assert.True(ProductManager.TryInvokeEffectClearCallback("test_player_clear", player, out var allowDefaultSecond));

        Assert.True(allowDefaultFirst);
        Assert.True(allowDefaultSecond);
        Assert.Equal(2, clearCount);
    }

    [Fact]
    public void NpcClearCallbackInvokesAndCanAllowNativeFallthrough()
    {
        var npc = (NPC)TestObjectFactory.CreateUninitialized(typeof(DanSamwell));
        var clearCount = 0;

        ProductManager.SetNpcEffectClearCallback("test_npc_clear", _ => clearCount++, allowDefaultEffect: true);

        Assert.True(ProductManager.TryInvokeNpcEffectClearCallback("TEST_NPC_CLEAR", npc, out var allowDefaultClear));
        Assert.True(allowDefaultClear);
        Assert.Equal(1, clearCount);
    }

    [Fact]
    public void MissingClearCallbackUsesNativeFallthrough()
    {
        var player = CreatePlayer();
        var npc = (NPC)TestObjectFactory.CreateUninitialized(typeof(DanSamwell));

        Assert.False(ProductManager.TryInvokeEffectClearCallback("missing", player, out var playerAllowDefault));
        Assert.False(ProductManager.TryInvokeNpcEffectClearCallback("missing", npc, out var npcAllowDefault));

        Assert.False(playerAllowDefault);
        Assert.False(npcAllowDefault);
    }

    [Fact]
    public void ClearCallbackRegistrationReplacesPriorCallbackAndRemovesCleanly()
    {
        var player = CreatePlayer();
        var firstCount = 0;
        var replacementCount = 0;

        ProductManager.SetEffectClearCallback("test_duplicate", _ => firstCount++);
        ProductManager.SetEffectClearCallback("TEST_DUPLICATE", _ => replacementCount++);

        Assert.True(ProductManager.TryInvokeEffectClearCallback("test_duplicate", player, out _));
        Assert.Equal(0, firstCount);
        Assert.Equal(1, replacementCount);
        Assert.True(ProductManager.RemoveEffectClearCallback("test_duplicate"));
        Assert.False(ProductManager.RemoveEffectClearCallback("test_duplicate"));
        Assert.False(ProductManager.TryInvokeEffectClearCallback("test_duplicate", player, out _));
    }

    [Fact]
    public void ResetClearCallbacksRemovesPlayerAndNpcRegistrations()
    {
        var player = CreatePlayer();
        var npc = (NPC)TestObjectFactory.CreateUninitialized(typeof(DanSamwell));

        ProductManager.SetEffectClearCallback("test_reset_player", _ => { });
        ProductManager.SetNpcEffectClearCallback("test_reset_npc", _ => { });

        ProductManager.ResetEffectClearCallbacks();
        ProductManager.ResetNpcEffectClearCallbacks();

        Assert.False(ProductManager.TryInvokeEffectClearCallback("test_reset_player", player, out _));
        Assert.False(ProductManager.TryInvokeNpcEffectClearCallback("test_reset_npc", npc, out _));
    }

    [Fact]
    public void ClearCallbackExceptionsAreSurfacedToTheLifecyclePatchAndDoNotCorruptRegistration()
    {
        var player = CreatePlayer();
        ProductManager.SetEffectClearCallback("test_exception", _ => throw new InvalidOperationException("expected"));

        Assert.Throws<InvalidOperationException>(() =>
            ProductManager.TryInvokeEffectClearCallback("test_exception", player, out _));

        var recoveryCount = 0;
        ProductManager.SetEffectClearCallback("test_exception", _ => recoveryCount++);

        Assert.True(ProductManager.TryInvokeEffectClearCallback("test_exception", player, out _));
        Assert.Equal(1, recoveryCount);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void ClearCallbackRegistrationRejectsInvalidEffectIds(string effectId)
    {
        Assert.Throws<ArgumentException>(() =>
            ProductManager.SetEffectClearCallback(effectId, _ => { }));
        Assert.Throws<ArgumentException>(() =>
            ProductManager.SetNpcEffectClearCallback(effectId, _ => { }));
        Assert.False(ProductManager.RemoveEffectClearCallback(effectId));
        Assert.False(ProductManager.RemoveNpcEffectClearCallback(effectId));
    }

    [Fact]
    public void ClearCallbackRegistrationRejectsNullInputs()
    {
        Assert.Throws<ArgumentNullException>(() =>
            ProductManager.SetEffectClearCallback("test", null!));
        Assert.Throws<ArgumentNullException>(() =>
            ProductManager.SetNpcEffectClearCallback("test", null!));
        Assert.Throws<ArgumentException>(() =>
            ProductManager.SetEffectClearCallback((string)null!, _ => { }));
        Assert.Throws<ArgumentException>(() =>
            ProductManager.SetNpcEffectClearCallback((string)null!, _ => { }));
        Assert.Throws<ArgumentNullException>(() =>
            ProductManager.SetEffectClearCallback((PropertyBase)null!, _ => { }));
        Assert.Throws<ArgumentNullException>(() =>
            ProductManager.SetNpcEffectClearCallback((PropertyBase)null!, _ => { }));
        Assert.Throws<ArgumentNullException>(() =>
            ProductManager.RemoveEffectClearCallback((PropertyBase)null!));
        Assert.Throws<ArgumentNullException>(() =>
            ProductManager.RemoveNpcEffectClearCallback((PropertyBase)null!));
    }

    private static Player CreatePlayer() =>
        TestObjectFactory.CreateUninitialized<Player>();
}

using System;
using System.Reflection;
using S1API.Internal.Products;

namespace S1API.Tests.Products;

public sealed class CustomProductManifestClientGateTests
{
    [Fact]
    public void HostAcknowledgementTimeoutAllowsForClientWorldLoading()
    {
        Assert.Equal(60, CustomProductManifestRuntime.HostAcknowledgementTimeoutSeconds);
    }

    [Fact]
    public void PlayerDataRequestRunsOnlyAfterMatchingManifestIsAccepted()
    {
        var gate = new CustomProductManifestClientGate();
        bool requested = false;
        gate.Begin(requiresValidation: true);

        Assert.False(gate.AuthorizePlayerDataRequest(() => requested = true));
        Assert.False(requested);

        CustomProductManifestAcceptance acceptance = gate.Accept(
            new string('a', 64),
            new string('a', 64),
            out Action? deferred);

        Assert.Equal(CustomProductManifestAcceptance.Accepted, acceptance);
        Assert.NotNull(deferred);
        Assert.False(requested);
        deferred();
        Assert.True(requested);
        Assert.False(gate.IsWaiting);
    }

    [Fact]
    public void IncompatibleManifestLeavesPlayerDataBlockedWithoutPartialState()
    {
        var gate = new CustomProductManifestClientGate();
        bool requested = false;
        gate.Begin(requiresValidation: true);
        Assert.False(gate.AuthorizePlayerDataRequest(() => requested = true));

        CustomProductManifestAcceptance acceptance = gate.Accept(
            new string('a', 64),
            new string('b', 64),
            out Action? deferred);

        Assert.Equal(CustomProductManifestAcceptance.Incompatible, acceptance);
        Assert.Null(deferred);
        Assert.False(requested);
        Assert.True(gate.IsWaiting);

        gate.End();
        Assert.False(gate.IsWaiting);
        Assert.True(gate.AuthorizePlayerDataRequest(() => requested = true));
    }

    [Fact]
    public void ReconnectResetsDeferredStateAndDuplicateAcceptanceIsIdempotent()
    {
        var gate = new CustomProductManifestClientGate();
        int firstSessionRequests = 0;
        gate.Begin(requiresValidation: true);
        Assert.False(gate.AuthorizePlayerDataRequest(() => firstSessionRequests++));
        gate.End();

        gate.Begin(requiresValidation: true);
        Assert.False(gate.AuthorizePlayerDataRequest(() => { }));
        Assert.Equal(
            CustomProductManifestAcceptance.Accepted,
            gate.Accept("hash", "hash", out Action? deferred));
        Assert.NotNull(deferred);

        Assert.Equal(
            CustomProductManifestAcceptance.AlreadyAccepted,
            gate.Accept("hash", "hash", out Action? duplicate));
        Assert.Null(duplicate);
        Assert.Equal(0, firstSessionRequests);
    }

    [Fact]
    public void VanillaSessionBypassesManifestGate()
    {
        var gate = new CustomProductManifestClientGate();
        gate.Begin(requiresValidation: false);

        Assert.False(gate.IsWaiting);
        Assert.True(gate.AuthorizePlayerDataRequest(() => { }));
    }

    [Fact]
    public void RepeatedNativeRequestRetainsOnlyTheFirstDeferredCall()
    {
        var gate = new CustomProductManifestClientGate();
        int first = 0;
        int second = 0;
        gate.Begin(requiresValidation: true);

        Assert.False(gate.AuthorizePlayerDataRequest(() => first++));
        Assert.False(gate.AuthorizePlayerDataRequest(() => second++));
        Assert.Equal(
            CustomProductManifestAcceptance.Accepted,
            gate.Accept("hash", "hash", out Action? deferred));

        deferred!();
        Assert.Equal(1, first);
        Assert.Equal(0, second);
    }

    [Fact]
    public void DescriptorRestorePrecedesHostManifestFinalizationInTheSamePatch()
    {
        Assembly assembly = typeof(CustomProductManifestRuntime).Assembly;
        Type patch = assembly.GetType(
            "S1API.Internal.Patches.CustomProductSavePatches",
            throwOnError: true)!;
        MethodInfo prefix = patch.GetMethod(
            "RestorePrefix",
            BindingFlags.NonPublic | BindingFlags.Static)!;
        MethodInfo restore = typeof(CustomProductSavePersistence).GetMethod(
            "RestoreBeforeBaseLoaders",
            BindingFlags.NonPublic | BindingFlags.Static)!;
        MethodInfo finalize = typeof(CustomProductManifestRuntime).GetMethod(
            "FinalizeHostManifestAfterDescriptorRestore",
            BindingFlags.NonPublic | BindingFlags.Static)!;
        byte[] il = prefix.GetMethodBody()!.GetILAsByteArray()!;

        int restoreCall = FindMetadataToken(il, restore.MetadataToken);
        int finalizeCall = FindMetadataToken(il, finalize.MetadataToken);

        Assert.True(restoreCall >= 0);
        Assert.True(finalizeCall > restoreCall);
    }

    [Fact]
    public void DeferredHostPlayerDataReplaysTheExactInterceptedOverload()
    {
        var target = new OverloadedPlayerDataReceiver();
        MethodInfo selected = typeof(OverloadedPlayerDataReceiver).GetMethod(
            nameof(OverloadedPlayerDataReceiver.ReceivePlayerData),
            new[] { typeof(int) })!;
        Type pendingType = typeof(CustomProductManifestRuntime).GetNestedType(
            "PendingHostData",
            BindingFlags.NonPublic)!;
        object pending = Activator.CreateInstance(
            pendingType,
            BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null,
            args: new object[] { target, new object[] { 7 }, selected },
            culture: null)!;

        pendingType.GetMethod(
            "Invoke",
            BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(pending, null);

        Assert.Equal(7, target.IntegerValue);
        Assert.Null(target.StringValue);
    }

    private static int FindMetadataToken(byte[] il, int token)
    {
        byte[] bytes = BitConverter.GetBytes(token);
        for (int i = 0; i <= il.Length - bytes.Length; i++)
        {
            bool matches = true;
            for (int offset = 0; offset < bytes.Length; offset++)
                matches &= il[i + offset] == bytes[offset];
            if (matches)
                return i;
        }

        return -1;
    }

    private sealed class OverloadedPlayerDataReceiver
    {
        internal int IntegerValue { get; private set; }
        internal string? StringValue { get; private set; }

        public void ReceivePlayerData(int value)
        {
            IntegerValue = value;
        }

        public void ReceivePlayerData(string value)
        {
            StringValue = value;
        }
    }
}

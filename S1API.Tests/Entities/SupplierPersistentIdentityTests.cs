using S1API.Entities.Supplier;
using S1API.Internal.Entities.Suppliers;
using System.Reflection;

namespace S1API.Tests.Entities;

public sealed class SupplierPersistentIdentityTests
{
    [Fact]
    public void PersistentIdIsPublicFluentApi()
    {
        MethodInfo? method = typeof(SupplierDataBuilder).GetMethod(
            nameof(SupplierDataBuilder.WithPersistentId),
            BindingFlags.Instance | BindingFlags.Public,
            binder: null,
            types: [typeof(string)],
            modifiers: null);

        Assert.NotNull(method);
        Assert.Equal(typeof(SupplierDataBuilder), method.ReturnType);
    }

    [Fact]
    public void PersistentIdIsOptInAndPreservesTheConfiguredValue()
    {
        var defaults = new SupplierDataBuilder().BuildInternal();
        var builder = new SupplierDataBuilder();

        SupplierDataBuilder result = builder.WithPersistentId(
            " ifbars.moredrugs:npcs/disco-davey ");

        Assert.Same(builder, result);
        Assert.Null(defaults.PersistentId);
        Assert.Equal(
            "ifbars.moredrugs:npcs/disco-davey",
            builder.BuildInternal().PersistentId);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void PersistentIdRejectsEmptyValues(string persistentId)
    {
        var builder = new SupplierDataBuilder();

        Assert.Throws<ArgumentException>(() =>
            builder.WithPersistentId(persistentId));
    }

    [Fact]
    public void PersistentIdRejectsNullValue()
    {
        var builder = new SupplierDataBuilder();

        Assert.Throws<ArgumentException>(() =>
            builder.WithPersistentId(null!));
    }

    [Fact]
    public void PersistentIdKeepsSupplierInfrastructureIdentitiesStable()
    {
        const string runtimeId = "disco_davey";
        const string persistentId = "ifbars.moredrugs:npcs/disco-davey";
        var configured = new SupplierDataBuilder()
            .WithPersistentId(persistentId)
            .BuildInternal();

        SupplierInfrastructureIdentity migrated = GetInfrastructureIdentity(
            configured.PersistentId ?? runtimeId);
        SupplierInfrastructureIdentity legacy = GetInfrastructureIdentity(persistentId);
        SupplierInfrastructureIdentity currentRuntime = GetInfrastructureIdentity(runtimeId);

        Assert.Equal(legacy, migrated);
        Assert.NotEqual(currentRuntime, migrated);
        Assert.Equal(migrated, GetInfrastructureIdentity(configured.PersistentId ?? runtimeId));
    }

    [Fact]
    public void OmittedPersistentIdFallsBackToRuntimeInfrastructureIdentities()
    {
        const string runtimeId = "disco_davey";
        var configured = new SupplierDataBuilder().BuildInternal();

        Assert.Null(configured.PersistentId);
        SupplierInfrastructureIdentity fallback = GetInfrastructureIdentity(
            configured.PersistentId ?? runtimeId);

        Assert.Equal(GetInfrastructureIdentity(runtimeId), fallback);
        Assert.Equal(fallback, GetInfrastructureIdentity(configured.PersistentId ?? runtimeId));
    }

    private static SupplierInfrastructureIdentity GetInfrastructureIdentity(string stableId)
    {
        return new SupplierInfrastructureIdentity(
            SupplierRuntimeIds.GetShopName(stableId),
            SupplierRuntimeIds.GetDeliveryVehiclePrefabName(stableId),
            SupplierRuntimeIds.GetDeliveryVehicleGuid(stableId),
            SupplierRuntimeIds.GetStashGuid(stableId));
    }

    private readonly record struct SupplierInfrastructureIdentity(
        string ShopName,
        string DeliveryVehiclePrefabName,
        Guid DeliveryVehicleGuid,
        Guid StashGuid);
}

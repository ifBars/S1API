using System.Reflection;
using S1API.Entities;
using S1API.Internal.Entities;

namespace S1API.Tests.Entities;

public sealed class NPCRoleDeclarationTests
{
    [Fact]
    public void IsCustomerIsVirtualReadOnlyBooleanDefaultingToFalse()
    {
        PropertyInfo? property = typeof(NPC).GetProperty(nameof(NPC.IsCustomer));
        MethodInfo? getter = property?.GetMethod;
        Type npcType = typeof(CustomNpcReadinessPolicyTests).GetNestedType(
            "DealerNpc",
            BindingFlags.NonPublic)!;
        var npc = (NPC)System.Runtime.CompilerServices.RuntimeHelpers
            .GetUninitializedObject(npcType);

        Assert.NotNull(property);
        Assert.Equal(typeof(bool), property!.PropertyType);
        Assert.True(getter!.IsVirtual);
        Assert.False(getter.IsFinal);
        Assert.Null(property.SetMethod);
        Assert.False(npc.IsCustomer);
    }

    [Theory]
    [InlineData(false, false, false, false)]
    [InlineData(false, true, false, false)]
    [InlineData(true, false, true, false)]
    [InlineData(true, true, true, false)]
    [InlineData(true, false, false, true)]
    [InlineData(true, true, false, true)]
    public void ValidRoleCombinationsRemainComposable(
        bool isPhysical,
        bool isCustomer,
        bool isDealer,
        bool isSupplier)
    {
        var declaration = new NpcRoleDeclaration(
            isPhysical,
            isCustomer,
            isDealer,
            isSupplier);

        NpcRoleDeclaration validated = declaration.Validate(typeof(NPC));

        Assert.Equal(isCustomer, validated.IsCustomer);
        Assert.Equal(isDealer, validated.IsDealer);
        Assert.Equal(isSupplier, validated.IsSupplier);
    }

    [Fact]
    public void DealerAndSupplierDeclarationFailsEarly()
    {
        var declaration = new NpcRoleDeclaration(
            isPhysical: true,
            isCustomer: false,
            isDealer: true,
            isSupplier: true);

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => declaration.Validate(typeof(NPC)));

        Assert.Contains("cannot be both a dealer and a supplier", exception.Message);
    }

    [Fact]
    public void NonPhysicalSupplierDeclarationFailsEarly()
    {
        var declaration = new NpcRoleDeclaration(
            isPhysical: false,
            isCustomer: false,
            isDealer: false,
            isSupplier: true);

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => declaration.Validate(typeof(NPC)));

        Assert.Contains("must override IsPhysical to return true", exception.Message);
    }

    [Fact]
    public void CompatibilityDeclarationsOnlyAddLegacyCapabilities()
    {
        var properties = new NpcRoleDeclaration(
            isPhysical: true,
            isCustomer: false,
            isDealer: false,
            isSupplier: false);

        NpcRoleDeclaration effective = properties
            .WithCompatibilityRoles(
                isCustomer: true,
                isDealer: true,
                isSupplier: false)
            .Validate(typeof(NPC));

        Assert.True(effective.IsPhysical);
        Assert.True(effective.IsCustomer);
        Assert.True(effective.IsDealer);
        Assert.False(effective.IsSupplier);
        Assert.Equal(NpcRootRole.Dealer, effective.RootRole);
    }

    [Theory]
    [InlineData(nameof(NPCPrefabBuilder.EnsureCustomer), nameof(NPC.IsCustomer))]
    [InlineData(nameof(NPCPrefabBuilder.EnsureDealer), nameof(NPC.IsDealer))]
    [InlineData(nameof(NPCPrefabBuilder.EnsureSupplier), nameof(NPC.IsSupplier))]
    public void LegacyEnsureMethodsRemainFluentNonErrorObsoleteShims(
        string methodName,
        string replacementProperty)
    {
        MethodInfo? method = typeof(NPCPrefabBuilder).GetMethod(
            methodName,
            BindingFlags.Public | BindingFlags.Instance,
            binder: null,
            types: Type.EmptyTypes,
            modifiers: null);
        ObsoleteAttribute? obsolete = method?.GetCustomAttribute<ObsoleteAttribute>();

        Assert.NotNull(method);
        Assert.Equal(typeof(NPCPrefabBuilder), method!.ReturnType);
        Assert.Empty(method.GetParameters());
        Assert.NotNull(obsolete);
        Assert.False(obsolete!.IsError);
        Assert.Contains(replacementProperty, obsolete.Message);
    }

#pragma warning disable CS0618
    private static NPCPrefabBuilder CompileLegacyFluentCalls(NPCPrefabBuilder builder) =>
        builder.EnsureCustomer().EnsureDealer().EnsureSupplier();
#pragma warning restore CS0618
}

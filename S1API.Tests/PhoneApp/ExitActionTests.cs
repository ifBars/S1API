using System.Reflection;
using UnityEngine;
using ExitAction = S1API.PhoneApp.ExitAction;
using PhoneAppBase = S1API.PhoneApp.PhoneApp;

namespace S1API.Tests.PhoneApp;

public sealed class ExitActionTests
{
    [Fact]
    public void UsedForwardsReadsAndWritesToTheNativeAdapter()
    {
        bool nativeUsed = false;
        var exit = new ExitAction(
            () => nativeUsed,
            used => nativeUsed = used);

        Assert.False(exit.Used);

        exit.Used = true;

        Assert.True(nativeUsed);
        Assert.True(exit.Used);
    }

    [Fact]
    public void ConstructorRejectsMissingNativeAccessors()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new ExitAction(null!, _ => { }));
        Assert.Throws<ArgumentNullException>(() =>
            new ExitAction(() => false, null!));
    }

    [Fact]
    public void PhoneAppExitUsesOnlyTheS1ApiOwnedActionType()
    {
        MethodInfo exit = typeof(PhoneAppBase).GetMethod(
            nameof(PhoneAppBase.Exit),
            BindingFlags.Instance | BindingFlags.Public,
            binder: null,
            types: [typeof(ExitAction)],
            modifiers: null)!;

        Assert.NotNull(exit);
        Assert.True(exit.IsVirtual);
        Assert.False(exit.IsFinal);
        Assert.Equal(typeof(void), exit.ReturnType);

        ParameterInfo parameter = Assert.Single(exit.GetParameters());
        Assert.Equal("exit", parameter.Name);
        Assert.Equal("S1API", parameter.ParameterType.Assembly.GetName().Name);
        Assert.Equal("S1API.PhoneApp.ExitAction", parameter.ParameterType.FullName);
    }

    [Fact]
    public void ExitActionCannotBeConstructedByMods()
    {
        ConstructorInfo[] publicConstructors = typeof(ExitAction).GetConstructors(
            BindingFlags.Instance | BindingFlags.Public);

        Assert.Empty(publicConstructors);
    }

    [Fact]
    public void ModsCanOverrideExitUsingTheS1ApiOwnedAction()
    {
        MethodInfo exit = typeof(ExitOverrideCompileFixture).GetMethod(
            nameof(PhoneAppBase.Exit),
            BindingFlags.Instance | BindingFlags.Public,
            binder: null,
            types: [typeof(ExitAction)],
            modifiers: null)!;

        Assert.Equal(typeof(ExitOverrideCompileFixture), exit.DeclaringType);
    }

    private sealed class ExitOverrideCompileFixture : PhoneAppBase
    {
        protected override string AppName => "exit-contract";
        protected override string AppTitle => "Exit Contract";
        protected override string IconLabel => "Exit";
        protected override string IconFileName => "exit.png";

        protected override void OnCreatedUI(GameObject container)
        {
        }

        public override void Exit(ExitAction exit)
        {
            exit.Used = true;
        }
    }
}

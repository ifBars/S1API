using System.ComponentModel;
using System.Reflection;
using S1API.Internal.Abstraction;
using S1API.Saveables;
using Saveable = S1API.Saveables.Saveable;
#pragma warning disable CS0618 // These fixtures intentionally exercise the legacy API.
using LegacySaveable = S1API.Internal.Abstraction.Saveable;

namespace S1API.Tests.Saveables;

public sealed class SaveableCompatibilityTests
{
    [Fact]
    public void LegacySurfaceRemainsAvailableWithWarningOnlyDeprecation()
    {
        ObsoleteAttribute obsolete = typeof(LegacySaveable).GetCustomAttribute<ObsoleteAttribute>()!;
        Assert.False(obsolete.IsError);
        Assert.Contains("S1API.Saveables.Saveable", obsolete.Message);
        Assert.Null(typeof(Saveable).GetCustomAttribute<ObsoleteAttribute>());
        Assert.Equal(EditorBrowsableState.Always, typeof(Saveable).GetCustomAttribute<EditorBrowsableAttribute>()!.State);
        Assert.True(typeof(Saveable).IsAssignableFrom(typeof(LegacySaveable)));
        Assert.Equal(typeof(LegacySaveable), typeof(global::S1API.Entities.NPC).BaseType);
        Assert.Equal(typeof(LegacySaveable), typeof(global::S1API.Quests.Quest).BaseType);

        const BindingFlags declared = BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
        Assert.True(typeof(LegacySaveable).GetProperty("LoadOrder", declared)!.GetMethod!.IsVirtual);
        foreach (string hook in new[] { "OnLoaded", "OnSaved" })
        {
            MethodInfo method = typeof(LegacySaveable).GetMethod(hook, declared)!;
            Assert.True(method.IsFamily);
            Assert.True(method.IsVirtual);
            Assert.False(method.IsFinal);
            Assert.True((method.Attributes & MethodAttributes.NewSlot) != 0);
        }
        Assert.NotNull(typeof(LegacySaveable).GetMethod("RequestGameSave", declared, Type.EmptyTypes));
        MethodInfo request = typeof(LegacySaveable).GetMethod("RequestGameSave", declared, new[] { typeof(bool) })!;
        Assert.Equal("immediate", request.GetParameters()[0].Name);
        Assert.True(typeof(Registerable).IsPublic);
        Assert.Equal(EditorBrowsableState.Never, typeof(Registerable).GetCustomAttribute<EditorBrowsableAttribute>()!.State);
    }

    [Fact]
    public void DiscoveryAcceptsBothDirectBasesAndKeepsIndirectClassesExcluded()
    {
        MethodInfo discover = typeof(SaveableAutoRegistry).GetMethod("IsDirectSaveableInheritor", BindingFlags.NonPublic | BindingFlags.Static)!;
        bool Accepts(Type type) => (bool)discover.Invoke(null, new object[] { type })!;

        Assert.True(Accepts(typeof(CurrentFixture)));
        Assert.True(Accepts(typeof(LegacyFixture)));
        Assert.False(Accepts(typeof(IndirectCurrentFixture)));
        Assert.False(Accepts(typeof(IndirectLegacyFixture)));
        Assert.False(Accepts(typeof(Saveable)));
        Assert.False(Accepts(typeof(LegacySaveable)));
        Assert.False(Accepts(typeof(global::S1API.Entities.NPC)));
        Assert.False(Accepts(typeof(global::S1API.Quests.Quest)));
        Assert.False(Accepts(typeof(string)));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void LifecycleAndLegacyJsonLoadingDispatchThroughBothBases(bool legacy)
    {
        Saveable instance = legacy ? new LegacyFixture() : new CurrentFixture();
        IFixture fixture = (IFixture)instance;
        Assert.Equal(SaveableLoadOrder.AfterBaseGame, instance.LoadOrder);
        fixture.LoadBeforeBaseGame = true;
        Assert.Equal(SaveableLoadOrder.BeforeBaseGame, instance.LoadOrder);
        string folder = Path.Combine(Path.GetTempPath(), "S1API-Saveable-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try
        {
            ((IRegisterable)instance).CreateInternal();
            ((ISaveable)instance).LoadInternal(folder);
            Assert.Equal(1, fixture.Loaded);
            Assert.Equal(7, fixture.Value);
            File.WriteAllText(Path.Combine(folder, "value.json"), "42");
            ((ISaveable)instance).LoadInternal(folder);
            Assert.Equal(42, fixture.Value);
            Assert.Equal(2, fixture.Loaded);
            ((ISaveable)instance).OnSaved();
            ((IRegisterable)instance).DestroyInternal();
            Assert.Equal(1, fixture.Saved);
            Assert.Equal(1, fixture.Created);
            Assert.Equal(1, fixture.Destroyed);
        }
        finally
        {
            Directory.Delete(folder, true);
        }
    }

#if MONOMELON
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SavingPreservesFieldNamesAndNullDeletion(bool legacy)
    {
        Saveable instance = legacy ? new LegacyFixture() : new CurrentFixture();
        IFixture fixture = (IFixture)instance;
        string folder = Path.Combine(Path.GetTempPath(), "S1API-Saveable-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try
        {
            File.WriteAllText(Path.Combine(folder, "optional.json"), "99");
            var extra = new List<string>();
            ((ISaveable)instance).SaveInternal(folder, ref extra);
            Assert.Equal("7", File.ReadAllText(Path.Combine(folder, "value.json")));
            Assert.Equal(new[] { "value.json" }, extra);
            Assert.False(File.Exists(Path.Combine(folder, "optional.json")));
            Assert.Null(fixture.Optional);
            Assert.Equal(1, fixture.Saved);
        }
        finally
        {
            Directory.Delete(folder, true);
        }
    }
#endif

    private interface IFixture
    {
        int Value { get; }
        int? Optional { get; }
        bool LoadBeforeBaseGame { get; set; }
        int Created { get; }
        int Destroyed { get; }
        int Loaded { get; }
        int Saved { get; }
    }

    private class CurrentFixture : Saveable, IFixture
    {
        [SaveableField("value")] private int _value = 7;
        [SaveableField("optional.json")] private int? _optional = null;
        public int Value => _value;
        public int? Optional => _optional;
        public bool LoadBeforeBaseGame { get; set; }
        public override SaveableLoadOrder LoadOrder => LoadBeforeBaseGame ? SaveableLoadOrder.BeforeBaseGame : base.LoadOrder;
        public int Created { get; private set; }
        public int Destroyed { get; private set; }
        public int Loaded { get; private set; }
        public int Saved { get; private set; }
        protected override void OnCreated() => Created++;
        protected override void OnDestroyed() => Destroyed++;
        protected override void OnLoaded() { base.OnLoaded(); Loaded++; }
        protected override void OnSaved() { base.OnSaved(); Saved++; }
    }

    private class LegacyFixture : LegacySaveable, IFixture
    {
        [SaveableField("value")] private int _value = 7;
        [SaveableField("optional.json")] private int? _optional = null;
        public int Value => _value;
        public int? Optional => _optional;
        public bool LoadBeforeBaseGame { get; set; }
        public override SaveableLoadOrder LoadOrder => LoadBeforeBaseGame ? SaveableLoadOrder.BeforeBaseGame : base.LoadOrder;
        public int Created { get; private set; }
        public int Destroyed { get; private set; }
        public int Loaded { get; private set; }
        public int Saved { get; private set; }
        protected override void OnCreated() => Created++;
        protected override void OnDestroyed() => Destroyed++;
        protected override void OnLoaded() { base.OnLoaded(); Loaded++; }
        protected override void OnSaved() { base.OnSaved(); Saved++; }
    }

    private sealed class IndirectCurrentFixture : CurrentFixture { }
    private sealed class IndirectLegacyFixture : LegacyFixture { }
}
#pragma warning restore CS0618

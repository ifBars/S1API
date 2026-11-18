using System.Reflection;
using S1API.Internal.PhoneApps;
using PhoneAppBase = S1API.PhoneApp.PhoneApp;

namespace S1API.Tests.PhoneApp;

[Collection("PhoneAppRuntime")]
public sealed class PhoneAppRegistrationTests : IDisposable
{
    private readonly List<(string Level, string Message)> _log = new();

    public PhoneAppRegistrationTests()
    {
        PhoneAppRuntime.ResetForTesting();
        PhoneAppRuntime.LogSinkForTesting = (level, message) => _log.Add((level, message));
    }

    public void Dispose() => PhoneAppRuntime.ResetForTesting();

    private sealed class RuntimeApp : PhoneAppBase
    {
        private readonly string _name;
        public RuntimeApp(string name) => _name = name;
        protected override string AppName => _name;
        protected override string AppTitle => _name;
        protected override string IconLabel => _name;
        protected override string IconFileName => "";
        protected override void OnCreatedUI(UnityEngine.GameObject container) { }
    }

    [Fact]
    public void RegisterWithoutHomeScreenWaits()
    {
        var app = new RuntimeApp("probe");
        PhoneAppBase.Register(app);

        Assert.True(PhoneAppRuntime.Contains(app));
        Assert.Contains(_log, l => l.Message == "Phone app probe registered at runtime.");
        Assert.Contains(_log, l => l.Message == "Phone app probe waits for the home screen.");
    }

    [Fact]
    public void RegisterTwiceWarnsAndKeepsOne()
    {
        var app = new RuntimeApp("probe");
        PhoneAppBase.Register(app);
        PhoneAppBase.Register(app);

        Assert.Equal(1, PhoneAppRuntime.Count);
        Assert.Single(_log, l => l.Level == "warn" && l.Message.Contains("already registered"));
    }

    [Fact]
    public void SecondAppWithTheSameNameIsRejected()
    {
        var first = new RuntimeApp("notes");
        var second = new RuntimeApp("notes");
        PhoneAppBase.Register(first);
        PhoneAppBase.Register(second);

        Assert.True(PhoneAppRuntime.Contains(first));
        Assert.False(PhoneAppRuntime.Contains(second));
        Assert.Single(_log, l => l.Level == "warn" && l.Message.Contains("uses the same AppName"));
    }

    [Fact]
    public void RegisteredTypeIsKnownToTheHomeScreenScan()
    {
        Assert.False(PhoneAppRuntime.HasInstanceOf(typeof(RuntimeApp)));
        PhoneAppBase.Register(new RuntimeApp("probe"));
        Assert.True(PhoneAppRuntime.HasInstanceOf(typeof(RuntimeApp)));
    }

    [Fact]
    public void UnregisterUnknownAppReturnsFalseWithWarning()
    {
        Assert.False(PhoneAppBase.Unregister(new RuntimeApp("ghost")));
        Assert.Single(_log, l => l.Level == "warn" && l.Message.Contains("was not registered"));
    }

    [Fact]
    public void NullArgumentsThrow()
    {
        Assert.Throws<ArgumentNullException>(() => PhoneAppBase.Register(null!));
        Assert.Throws<ArgumentNullException>(() => PhoneAppBase.Unregister(null!));
    }

    [Fact]
    public void PublicSurfaceExists()
    {
        Assert.NotNull(typeof(PhoneAppBase).GetMethod("Register", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(PhoneAppBase) }, null));
        var unregister = typeof(PhoneAppBase).GetMethod("Unregister", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(PhoneAppBase) }, null);
        Assert.NotNull(unregister);
        Assert.Equal(typeof(bool), unregister!.ReturnType);
    }
}

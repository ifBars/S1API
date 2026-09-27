using S1API.ExternalHosting;
using UnityEngine;

namespace S1API.Tests.ExternalHosting;

[CollectionDefinition("External app catalog", DisableParallelization = true)]
public sealed class ExternalAppCatalogCollection { }

[Collection("External app catalog")]
public sealed class ExternalAppCatalogTests : IDisposable
{
    public ExternalAppCatalogTests()
    {
        ExternalAppCatalog.Clear(ExternalAppFamily.Phone);
        ExternalAppCatalog.Clear(ExternalAppFamily.TV);
    }

    [Fact]
    public void HostablePhoneAndTvAppsHaveSeparateStableIdentities()
    {
        var phone = new SampleHost();
        var tv = new SampleHost();

        ExternalAppCatalog.Register(ExternalAppFamily.Phone, phone, "sample", "Phone sample", () => null);
        ExternalAppCatalog.Register(ExternalAppFamily.TV, tv, "sample", "TV sample", () => null);

        ExternalAppRegistration[] entries = ExternalAppCatalog.GetAll().ToArray();
        Assert.Equal(2, entries.Length);
        Assert.NotEqual(entries[0].Id, entries[1].Id);
        Assert.Contains(entries, entry => entry.Family == ExternalAppFamily.Phone &&
            entry.Title == "Phone sample" && ReferenceEquals(entry.Host, phone));
        Assert.Contains(entries, entry => entry.Family == ExternalAppFamily.TV &&
            entry.Title == "TV sample" && ReferenceEquals(entry.Host, tv));
    }

    [Fact]
    public void LateRegistrationReplacementAndRemovalNotifyConsumers()
    {
        int changes = 0;
        void OnChanged() => changes++;
        ExternalAppCatalog.Changed += OnChanged;
        try
        {
            var first = new SampleHost();
            var next = new SampleHost();
            ExternalAppCatalog.Register(ExternalAppFamily.Phone, first, "sample", "Sample", () => null);
            Assert.Single(ExternalAppCatalog.GetAll());
            Assert.Equal(1, changes);

            ExternalAppCatalog.Register(ExternalAppFamily.Phone, first, "sample", "Sample", () => null);
            Assert.Equal(1, changes);

            ExternalAppCatalog.Register(ExternalAppFamily.Phone, next, "sample", "Sample", () => null);
            Assert.Same(next, Assert.Single(ExternalAppCatalog.GetAll()).Host);
            Assert.Equal(2, changes);

            ExternalAppCatalog.Unregister(first);
            Assert.Single(ExternalAppCatalog.GetAll());
            Assert.Equal(2, changes);

            ExternalAppCatalog.Unregister(next);
            Assert.Empty(ExternalAppCatalog.GetAll());
            Assert.Equal(3, changes);
        }
        finally
        {
            ExternalAppCatalog.Changed -= OnChanged;
        }
    }

    [Fact]
    public void OptedOutAndLegacyAppsDoNotEnterCatalog()
    {
        ExternalAppCatalog.Register(ExternalAppFamily.Phone, new SampleHost { AllowExternalHosting = false },
            "disabled", "Disabled", () => null);
        ExternalAppCatalog.Register(ExternalAppFamily.TV, new object(),
            "legacy", "Legacy", () => null);

        Assert.Empty(ExternalAppCatalog.GetAll());
        Assert.Contains(ExternalAppCatalog.GetDiagnostics(), diagnostic =>
            diagnostic.Family == ExternalAppFamily.TV && diagnostic.Message.Contains("IExternalAppHost"));
    }

    [Fact]
    public void ClearingOneDeviceFamilyPreservesTheOther()
    {
        ExternalAppCatalog.Register(ExternalAppFamily.Phone, new SampleHost(), "phone", "Phone", () => null);
        ExternalAppCatalog.Register(ExternalAppFamily.TV, new SampleHost(), "tv", "TV", () => null);

        ExternalAppCatalog.Clear(ExternalAppFamily.Phone);

        Assert.Equal(ExternalAppFamily.TV, Assert.Single(ExternalAppCatalog.GetAll()).Family);
    }

    [Fact]
    public void SceneChangeClearsRegistrationsAndLegacyDiagnostics()
    {
        ExternalAppCatalog.Register(ExternalAppFamily.Phone, new SampleHost(), "phone", "Phone", () => null);
        ExternalAppCatalog.Register(ExternalAppFamily.TV, new object(), "legacy", "Legacy", () => null);

        ExternalAppCatalog.ClearForSceneChange();

        Assert.Empty(ExternalAppCatalog.GetAll());
        Assert.Empty(ExternalAppCatalog.GetDiagnostics());
    }

    public void Dispose()
    {
        ExternalAppCatalog.Clear(ExternalAppFamily.Phone);
        ExternalAppCatalog.Clear(ExternalAppFamily.TV);
    }

    // A modded phone or TV app can implement this contract without referencing a display mod.
    private sealed class SampleHost : IExternalAppHost
    {
        public bool AllowExternalHosting { get; set; } = true;

        public IExternalAppSession CreateExternalSession(GameObject container, Action requestClose) =>
            new SampleSession();
    }

    private sealed class SampleSession : IExternalAppSession
    {
        public void Open() { }
        public void Tick() { }
        public void Close() { }
        public void Dispose() { }
    }
}

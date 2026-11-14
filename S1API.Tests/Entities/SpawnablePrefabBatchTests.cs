using S1API.Internal.Entities;

namespace S1API.Tests.Entities;

public sealed class SpawnablePrefabBatchTests
{
    [Fact]
    public void SupplierVehiclesKeepTheirDiscoveryPositionBetweenNpcPrefabs()
    {
        var batch = new SpawnablePrefabBatch<string>(Array.Empty<string>());
        batch.Add("npc-a", "first NPC");
        batch.Add("supplier-vehicle", "delivery vehicle");
        batch.Add("npc-b", "second NPC");
        var registered = new List<string>();
        int batchCalls = 0;

        batch.Register(
            prefabs => { batchCalls++; registered.AddRange(prefabs); },
            _ => Assert.Fail("Successful batch must not fall back."),
            _ => Assert.Fail("Unexpected batch failure."),
            (_, _) => Assert.Fail("Unexpected prefab failure."));

        Assert.Equal(1, batchCalls);
        Assert.Equal(new[] { "first NPC", "delivery vehicle", "second NPC" }, registered);
    }

    [Fact]
    public void ExistingAndPendingNamesRetainTheFirstRegistration()
    {
        var batch = new SpawnablePrefabBatch<string>(new[] { "existing" });
        batch.Add("existing", "replacement");
        batch.Add("new", "first");
        batch.Add("new", "duplicate");
        var registered = new List<string>();

        batch.Register(registered.AddRange, _ => Assert.Fail(), _ => Assert.Fail(), (_, _) => Assert.Fail());

        Assert.Equal(1, batch.Count);
        Assert.Equal(new[] { "first" }, registered);
    }

    [Fact]
    public void PrefabNamesRemainCaseSensitive()
    {
        var batch = new SpawnablePrefabBatch<string>(new[] { "NPC" });
        batch.Add("npc", "lowercase");
        batch.Add("Npc", "mixed case");
        var registered = new List<string>();

        batch.Register(registered.AddRange, _ => Assert.Fail(), _ => Assert.Fail(), (_, _) => Assert.Fail());

        Assert.Equal(new[] { "lowercase", "mixed case" }, registered);
    }

    [Fact]
    public void EmptyBatchDoesNotCallNativeRegistration()
    {
        var batch = new SpawnablePrefabBatch<string>(new[] { "existing" });
        batch.Add("existing", "ignored");

        batch.Register(_ => Assert.Fail(), _ => Assert.Fail(), _ => Assert.Fail(), (_, _) => Assert.Fail());
    }

    [Fact]
    public void FailedBatchRetriesEveryPrefabInDiscoveryOrder()
    {
        var batch = CreateThreePrefabBatch();
        var failure = new InvalidOperationException("batch failed");
        var retries = new List<string>();
        Exception? reported = null;

        batch.Register(_ => throw failure, retries.Add, error => reported = error, (_, _) => Assert.Fail());

        Assert.Same(failure, reported);
        Assert.Equal(new[] { "npc-a", "vehicle", "npc-b" }, retries);
    }

    [Fact]
    public void PartialAppendCanRecoverThroughDuplicateAwareFallback()
    {
        var batch = CreateThreePrefabBatch();
        var registered = new List<string>();
        var retries = new List<string>();

        batch.Register(
            prefabs =>
            {
                registered.Add(prefabs[0]);
                throw new InvalidOperationException("initialization failed after append");
            },
            prefab =>
            {
                retries.Add(prefab);
                if (!registered.Contains(prefab))
                    registered.Add(prefab);
            },
            _ => { },
            (_, _) => Assert.Fail());

        Assert.Equal(new[] { "npc-a", "vehicle", "npc-b" }, retries);
        Assert.Equal(new[] { "npc-a", "vehicle", "npc-b" }, registered);
    }

    [Fact]
    public void OneFailedFallbackDoesNotPreventLaterPrefabsFromRegistering()
    {
        var batch = CreateThreePrefabBatch();
        var registered = new List<string>();
        var failure = new InvalidOperationException("vehicle failed");
        var errors = new List<(string Prefab, Exception Error)>();

        batch.Register(
            _ => throw new InvalidOperationException("batch failed"),
            prefab =>
            {
                if (prefab == "vehicle")
                    throw failure;
                registered.Add(prefab);
            },
            _ => { },
            (prefab, error) => errors.Add((prefab, error)));

        Assert.Equal(new[] { "npc-a", "npc-b" }, registered);
        var reported = Assert.Single(errors);
        Assert.Equal("vehicle", reported.Prefab);
        Assert.Same(failure, reported.Error);
    }

    [Fact]
    public void SeparatePassesDoNotSharePendingNames()
    {
        var first = new SpawnablePrefabBatch<string>(Array.Empty<string>());
        first.Add("npc", "first pass");
        var second = new SpawnablePrefabBatch<string>(Array.Empty<string>());
        second.Add("npc", "second pass");
        var registered = new List<string>();

        second.Register(registered.AddRange, _ => Assert.Fail(), _ => Assert.Fail(), (_, _) => Assert.Fail());

        Assert.Equal(new[] { "second pass" }, registered);
    }

    private static SpawnablePrefabBatch<string> CreateThreePrefabBatch()
    {
        var batch = new SpawnablePrefabBatch<string>(Array.Empty<string>());
        batch.Add("npc-a", "npc-a");
        batch.Add("vehicle", "vehicle");
        batch.Add("npc-b", "npc-b");
        return batch;
    }
}

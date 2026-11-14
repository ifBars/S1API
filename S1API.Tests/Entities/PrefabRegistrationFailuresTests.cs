using S1API.Internal.Entities;

namespace S1API.Tests.Entities;

public sealed class PrefabRegistrationFailuresTests
{
    [Fact]
    public void BatchAndFallbackFailureCanRecoverOnLaterRequest()
    {
        var batch = new SpawnablePrefabBatch<string>(Array.Empty<string>());
        var failures = new PrefabRegistrationFailures<string>();
        batch.Add("npc", "npc");
        batch.Register(
            _ => throw new InvalidOperationException("batch"),
            _ => throw new InvalidOperationException("fallback"),
            _ => { },
            (prefab, _) => failures.Record(prefab, prefab));
        var registered = new List<string>();

        failures.Retry("npc", registered.Add);
        failures.Retry("npc", _ => Assert.Fail("Successful registration was retried."));

        Assert.Equal(new[] { "npc" }, registered);
    }

    [Fact]
    public void FailedRetryRemainsEligibleAndPreservesTheException()
    {
        var failures = new PrefabRegistrationFailures<string>();
        failures.Record("npc", "prefab");
        var error = new InvalidOperationException("not ready");

        Assert.Same(error, Assert.Throws<InvalidOperationException>(() =>
            failures.Retry("npc", _ => throw error)));
        string? retried = null;
        failures.Retry("npc", prefab => retried = prefab);

        Assert.Equal("prefab", retried);
    }

    [Fact]
    public void PartialAppendIsRetriedEvenWhenTheObjectIsAlreadyPresent()
    {
        var failures = new PrefabRegistrationFailures<string>();
        var registered = new List<string> { "prefab" };
        failures.Record("npc", "prefab");
        int initializationAttempts = 0;

        failures.Retry("npc", prefab =>
        {
            if (!registered.Contains(prefab))
                registered.Add(prefab);
            initializationAttempts++;
        });

        Assert.Equal(1, initializationAttempts);
        Assert.Single(registered);
    }

    [Fact]
    public void UnfailedAndDifferentlyCasedNamesDoNotTriggerRegistration()
    {
        var failures = new PrefabRegistrationFailures<string>();
        failures.Record("NPC", "prefab");

        failures.Retry("npc", _ => Assert.Fail());
        failures.Retry("other", _ => Assert.Fail());
        string? retried = null;
        failures.Retry("NPC", prefab => retried = prefab);

        Assert.Equal("prefab", retried);
    }
}

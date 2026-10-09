using S1API.Internal.Patches;

namespace S1API.Tests.Internal.Patches;

public sealed class ContactsAppWaitTests
{
    private static Func<string> Id(string id) => () => id;
    private static Func<string> Throws() => () => throw new NullReferenceException("NPC data is gone");

    [Fact]
    public void AnNpcWhoseIdThrowsDoesNotHideTheOthers()
    {
        Assert.True(ContactsAppPatches.AllIdsPresent(
            new[] { Id("big_willy"), Throws(), Id("disco_davey"), Throws() },
            new[] { "big_willy", "disco_davey" }));
    }

    [Fact]
    public void AMissingCustomNpcIsStillWaitedFor()
    {
        Assert.False(ContactsAppPatches.AllIdsPresent(
            new[] { Id("big_willy"), Throws() },
            new[] { "big_willy", "disco_davey" }));
    }

    [Fact]
    public void NothingReadableMeansNothingPresent()
    {
        Assert.False(ContactsAppPatches.AllIdsPresent(new[] { Throws() }, new[] { "big_willy" }));
    }
}

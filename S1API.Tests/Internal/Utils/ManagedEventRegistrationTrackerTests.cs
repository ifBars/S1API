using S1API.Internal.Utils;

namespace S1API.Tests.Internal.Utils;

public sealed class ManagedEventRegistrationTrackerTests
{
    [Fact]
    public void DuplicateAddsAreRemovedOneAtATimeInReverseRegistrationOrder()
    {
        var tracker = new ManagedEventRegistrationTracker<string>();
        Action handler = () => { };

        tracker.Add(handler, "first");
        tracker.Add(handler, "second");

        Assert.True(tracker.TryTakeLast(handler, out string? second));
        Assert.Equal("second", second);
        Assert.True(tracker.TryTakeLast(handler, out string? first));
        Assert.Equal("first", first);
        Assert.False(tracker.TryTakeLast(handler, out _));
    }
}

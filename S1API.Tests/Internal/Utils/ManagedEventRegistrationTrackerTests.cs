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

    [Fact]
    public void TakeAllReturnsEveryRegistrationAndClearsTheTracker()
    {
        var tracker = new ManagedEventRegistrationTracker<string>();
        Action firstHandler = () => { };
        Action<int> secondHandler = _ => { };

        tracker.Add(firstHandler, "first");
        tracker.Add(firstHandler, "second");
        tracker.Add(secondHandler, "third");

        var registrations = tracker.TakeAll();

        Assert.Equal(3, registrations.Count);
        Assert.Contains(registrations, registration =>
            registration.ManagedHandler.Equals(firstHandler) && registration.NativeHandler == "first");
        Assert.Contains(registrations, registration =>
            registration.ManagedHandler.Equals(firstHandler) && registration.NativeHandler == "second");
        Assert.Contains(registrations, registration =>
            registration.ManagedHandler.Equals(secondHandler) && registration.NativeHandler == "third");
        Assert.False(tracker.TryTakeLast(firstHandler, out _));
        Assert.False(tracker.TryTakeLast(secondHandler, out _));
    }
}

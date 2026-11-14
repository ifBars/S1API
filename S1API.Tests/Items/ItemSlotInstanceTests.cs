using S1API.Items;
#if MONOMELON
using System.Reflection;
using NativeItemSlot = ScheduleOne.ItemFramework.ItemSlot;
using NativeItemSlotLock = ScheduleOne.ItemFramework.ItemSlotLock;
#endif

namespace S1API.Tests.Items;

public sealed class ItemSlotInstanceTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  Station task  ")]
    public void LockSnapshotPreservesReasonWithoutNormalization(string? reason)
    {
        Assert.Equal(reason, new ItemSlotLockInfo(reason).LockReason);
    }

#if MONOMELON
    [Fact]
    public void EmptySlotRetainsLegacyDefaultsAndZeroQuantityMutation()
    {
        var slot = new ItemSlotInstance(new NativeItemSlot());

        Assert.Equal(0, slot.Quantity);
        Assert.Null(slot.ItemInstance);
        slot.AddQuantity(amount: 0);
        Assert.Equal(0, slot.Quantity);
        Assert.Null(slot.ItemInstance);
        Assert.False(slot.IsLocked);
        Assert.False(slot.IsAddLocked);
        Assert.False(slot.IsRemovalLocked);
        Assert.Null(slot.ActiveLock);
    }

    [Fact]
    public void NullNativeSlotStillFailsOnAccessRatherThanConstruction()
    {
        var slot = new ItemSlotInstance(null!);

        Assert.Throws<NullReferenceException>(() => slot.Quantity);
        Assert.Throws<NullReferenceException>(() => slot.ItemInstance);
        Assert.Throws<NullReferenceException>(() => slot.AddQuantity(amount: -1));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Station task")]
    public void ActiveLockIsASnapshotAndIndependentFlagsRemainIndependent(string? reason)
    {
        var nativeSlot = new SlotFixture();
        var slot = new ItemSlotInstance(nativeSlot);
        int lockEvents = 0;
        slot.Locked += () => lockEvents++;
        slot.Unlocked += () => lockEvents++;

        nativeSlot.SetIsAddLocked(true);
        nativeSlot.SetIsRemovalLocked(true);
        Assert.True(slot.IsAddLocked);
        Assert.True(slot.IsRemovalLocked);
        Assert.False(slot.IsLocked);
        Assert.Null(slot.ActiveLock);
        Assert.Equal(0, lockEvents);

        nativeSlot.SetLock(reason);
        Assert.True(slot.IsLocked);
        ItemSlotLockInfo snapshot = slot.ActiveLock!.Value;
        Assert.Equal(reason, snapshot.LockReason);

        nativeSlot.SetLock("Replacement");
        Assert.Equal("Replacement", slot.ActiveLock!.Value.LockReason);
        Assert.Equal(reason, snapshot.LockReason);

        nativeSlot.ClearLock();
        Assert.False(slot.IsLocked);
        Assert.Null(slot.ActiveLock);
        Assert.True(slot.IsAddLocked);
        Assert.True(slot.IsRemovalLocked);
        Assert.Equal(reason, snapshot.LockReason);
    }

    [Theory]
    [InlineData(nameof(ItemSlotInstance.Locked), "onLocked")]
    [InlineData(nameof(ItemSlotInstance.Unlocked), "onUnlocked")]
    [InlineData(nameof(ItemSlotInstance.ItemInstanceChanged), "onItemInstanceChanged")]
    [InlineData(nameof(ItemSlotInstance.ItemDataChanged), "onItemDataChanged")]
    [InlineData(nameof(ItemSlotInstance.FilterChanged), "onFilterChange")]
    public void EventsPreserveNativeListenersAndRemoveLastDuplicate(string eventName, string callbackName)
    {
        var nativeSlot = new NativeItemSlot();
        var slot = new ItemSlotInstance(nativeSlot);
        EventInfo managedEvent = typeof(ItemSlotInstance).GetEvent(eventName)!;
        FieldInfo nativeCallback = typeof(NativeItemSlot).GetField(callbackName)!;
        var calls = new List<string>();
        Action handler = () => calls.Add("shared");
        Action otherHandler = () => calls.Add("other");
        nativeCallback.SetValue(nativeSlot, handler);

        managedEvent.AddEventHandler(slot, null);
        managedEvent.RemoveEventHandler(slot, null);
        managedEvent.RemoveEventHandler(slot, otherHandler);
        Assert.Same(handler, nativeCallback.GetValue(nativeSlot));

        managedEvent.AddEventHandler(slot, handler);
        managedEvent.AddEventHandler(slot, otherHandler);
        managedEvent.AddEventHandler(slot, handler);
        InvokeAndAssert("shared", "shared", "other", "shared");

        managedEvent.RemoveEventHandler(slot, handler);
        InvokeAndAssert("shared", "shared", "other");
        managedEvent.RemoveEventHandler(slot, handler);
        InvokeAndAssert("shared", "other");
        managedEvent.RemoveEventHandler(slot, handler);
        InvokeAndAssert("shared", "other");
        managedEvent.RemoveEventHandler(slot, otherHandler);
        Assert.Same(handler, nativeCallback.GetValue(nativeSlot));

        // A fresh wrapper can subscribe after cleanup without retaining old registrations.
        var reusedSlot = new ItemSlotInstance(nativeSlot);
        managedEvent.AddEventHandler(reusedSlot, otherHandler);
        InvokeAndAssert("shared", "other");
        managedEvent.RemoveEventHandler(reusedSlot, otherHandler);
        Assert.Same(handler, nativeCallback.GetValue(nativeSlot));

        void InvokeAndAssert(params string[] expected)
        {
            calls.Clear();
            ((Action?)nativeCallback.GetValue(nativeSlot))?.Invoke();
            Assert.Equal(expected, calls);
        }
    }

    [Fact]
    public void RemovingHandlersFromOneWrapperLeavesOtherWrappersAndEventsIntact()
    {
        var nativeSlot = new NativeItemSlot();
        var first = new ItemSlotInstance(nativeSlot);
        var second = new ItemSlotInstance(nativeSlot);
        var replacementNativeSlot = new NativeItemSlot();
        var replacement = new ItemSlotInstance(replacementNativeSlot);
        int calls = 0;
        Action handler = () => calls++;

        first.Locked += handler;
        second.Locked += handler;
        first.Unlocked += handler;
        second.Locked -= handler;
        replacement.Locked -= handler;
        Assert.Null(replacementNativeSlot.onLocked);

        nativeSlot.onLocked();
        nativeSlot.onUnlocked();
        Assert.Equal(2, calls);
        first.Locked -= handler;
        first.Unlocked -= handler;
        Assert.Null(nativeSlot.onLocked);
        Assert.Null(nativeSlot.onUnlocked);
    }

    private sealed class SlotFixture : NativeItemSlot
    {
        internal void SetLock(string? reason) =>
            ActiveLock = new NativeItemSlotLock(this, null!, reason!);

        internal void ClearLock() =>
            ActiveLock = null!;
    }
#endif
}

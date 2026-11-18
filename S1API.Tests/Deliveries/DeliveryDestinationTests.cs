using System.Reflection;
using S1API.Deliveries;
using S1API.Internal.Deliveries;
using UnityEngine;

#if IL2CPPMELON
using S1Delivery = Il2CppScheduleOne.Delivery;
using S1DeliveryUi = Il2CppScheduleOne.UI.Phone.Delivery;
using S1Property = Il2CppScheduleOne.Property;
#elif MONOMELON
using S1Delivery = ScheduleOne.Delivery;
using S1DeliveryUi = ScheduleOne.UI.Phone.Delivery;
using S1Property = ScheduleOne.Property;
#endif

namespace S1API.Tests.Deliveries;

[Collection("DeliveryDestinations")]
public sealed class DeliveryDestinationTests : IDisposable
{
    private readonly List<string> _log = new();

    public DeliveryDestinationTests()
    {
        DeliveryDestinationRuntime.ResetForTesting();
        DeliveryDestinationRuntime.LogSinkForTesting = _log.Add;
    }

    public void Dispose()
    {
        DeliveryDestinationRuntime.ResetForTesting();
        DeliveryDestinationRuntime.LogSinkForTesting = null;
    }

    [Fact]
    public void PatchTargetsExistInTargetRuntime()
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

        Assert.NotNull(typeof(S1DeliveryUi.DeliveryShop).GetMethod("GetPotentialDestinations", flags));
        Assert.NotNull(typeof(S1Delivery.DeliveryManager).GetMethod("OnTimePass", flags));
        MethodInfo? bay = typeof(S1Delivery.DeliveryManager).GetMethod("IsLoadingBayFree", flags);
        Assert.NotNull(bay);
        Assert.Equal(
            new[] { typeof(S1Property.Property), typeof(int) },
            Array.ConvertAll(bay!.GetParameters(), p => p.ParameterType));
        Assert.Equal(typeof(bool), bay.ReturnType);
    }

    [Fact]
    public void RegistryExposesTheDestinationMethods()
    {
        Type registry = typeof(DeliveryRegistry);

        Assert.Equal(typeof(IDisposable), registry.GetMethod(nameof(DeliveryRegistry.AddDestination))!.ReturnType);
        Assert.Equal(typeof(IDisposable), registry.GetMethod(nameof(DeliveryRegistry.AddLoadingDock))!.ReturnType);
        Assert.Equal(typeof(IDisposable), registry.GetMethod(nameof(DeliveryRegistry.AddArrivalHold))!.ReturnType);
        Assert.NotNull(registry.GetMethod(nameof(DeliveryRegistry.RefreshLoadingDocks)));
        Assert.Equal(
            typeof(Func<Transform?>),
            registry.GetMethod(nameof(DeliveryRegistry.AddLoadingDock))!.GetParameters()[1].ParameterType);
    }

    [Theory]
    [InlineData(0, 0, true)]
    [InlineData(1, 0, false)]
    [InlineData(1, 1, true)]
    [InlineData(3, 2, false)]
    [InlineData(3, -1, true)]
    public void DockIsMissingOutsideTheArray(int count, int index, bool missing)
    {
        Assert.Equal(missing, DeliveryDestinationRuntime.IsDockMissing(count, index));
    }

    [Fact]
    public void NullArgumentsThrow()
    {
        Assert.Throws<ArgumentNullException>(() => DeliveryRegistry.AddDestination(null!, () => true));
        Assert.Throws<ArgumentNullException>(() => DeliveryRegistry.AddLoadingDock(null!, () => null));
        Assert.Throws<ArgumentNullException>(() => DeliveryRegistry.AddArrivalHold(null!));
    }

    [Fact]
    public void ArrivalHoldIsRemovedOnceOnDispose()
    {
        IDisposable first = DeliveryRegistry.AddArrivalHold(_ => true);
        IDisposable second = DeliveryRegistry.AddArrivalHold(_ => false);
        Assert.Equal(2, DeliveryDestinationRuntime.HoldCount);

        first.Dispose();
        first.Dispose();

        Assert.Equal(1, DeliveryDestinationRuntime.HoldCount);
        Assert.Single(_log, line => line.Contains("arrival hold from") && line.Contains("removed"));
        second.Dispose();
        Assert.Equal(0, DeliveryDestinationRuntime.HoldCount);
    }

    [Fact]
    public void HoldNamesTheRegisteringAssembly()
    {
        using IDisposable hold = DeliveryRegistry.AddArrivalHold(_ => false);

        string owner = typeof(DeliveryDestinationTests).Assembly.GetName().Name!;
        Assert.Contains(_log, line => line == $"info: Delivery arrival hold registered by {owner}.");
    }

    [Fact]
    public void RefreshWithoutRegistrationsOnlyLogs()
    {
        DeliveryRegistry.RefreshLoadingDocks();

        Assert.Contains(_log, line => line.StartsWith("debug: RefreshLoadingDocks: 0 loading dock(s) attached"));
    }

    [Fact]
    public void ArrivalWithoutDeliveryIsNotHeld()
    {
        Assert.False(DeliveryDestinationRuntime.TryHold(null, default));
    }

    // --- delivery tick ---------------------------------------------------------------------------------

    [Fact]
    public void TickRestoresHeldDeliveriesInAFinalizer()
    {
        const BindingFlags flags = BindingFlags.Static | BindingFlags.NonPublic;
        Type patches = typeof(global::S1API.Internal.Patches.DeliveryDestinationPatches);

        MethodInfo? finalizer = patches.GetMethod("OnTimePassFinalizer", flags);
        Assert.NotNull(finalizer);
        Assert.NotNull(finalizer!.GetCustomAttribute<HarmonyLib.HarmonyFinalizer>());
        Assert.Equal(typeof(Exception), finalizer.ReturnType);
        Assert.Contains(finalizer.GetParameters(), p => p.Name == "__exception");
        Assert.Contains(finalizer.GetParameters(), p => p.Name == "__state");
        Assert.Null(patches.GetMethod("OnTimePassPostfix", flags));
    }

    [Fact]
    public void PutBackInsertsOnlyTheMissingEntriesThisCallPulled()
    {
        var deliveries = new List<string> { "vanilla", "pulled-1" };
        var pulled = new List<string> { "pulled-1", "pulled-2" };
        var inserted = new List<string>();

        int restored = DeliveryDestinationRuntime.PutBack<string>(
            pulled,
            new[] { 1, 2 },
            deliveries.Contains,
            (index, item) => { inserted.Add(item); deliveries.Insert(index, item); },
            () => deliveries.Count);

        Assert.Equal(1, restored);
        Assert.Equal(new[] { "pulled-2" }, inserted);
        Assert.Equal(new[] { "vanilla", "pulled-1", "pulled-2" }, deliveries);
    }

    [Fact]
    public void PutBackKeepsTheListOrder()
    {
        var deliveries = new List<string> { "a", "c", "e" };
        var pulled = new List<string> { "b", "d", "f" };

        int restored = DeliveryDestinationRuntime.PutBack<string>(
            pulled, new[] { 1, 3, 9 }, deliveries.Contains, deliveries.Insert, () => deliveries.Count);

        Assert.Equal(3, restored);
        Assert.Equal(new[] { "a", "b", "c", "d", "e", "f" }, deliveries);
    }

    [Fact]
    public void ArrivalHoldRunsBeforeTheStateChange()
    {
        const BindingFlags flags = BindingFlags.Static | BindingFlags.NonPublic;
        Type patches = typeof(global::S1API.Internal.Patches.DeliveryDestinationPatches);

        MethodInfo? state = typeof(S1Delivery.DeliveryManager).GetMethod(
            "SetDeliveryState", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
        Assert.NotNull(state);
        Assert.Equal(
            new[] { typeof(string), typeof(S1Delivery.EDeliveryStatus) },
            Array.ConvertAll(state!.GetParameters(), p => p.ParameterType));

        MethodInfo? prefix = patches.GetMethod("SetDeliveryStatePrefix", flags);
        Assert.NotNull(prefix);
        Assert.Equal(typeof(bool), prefix!.ReturnType);
        string[] names = Array.ConvertAll(state.GetParameters(), p => p.Name!);
        Assert.All(prefix.GetParameters(), p => Assert.True(p.Name == "__instance" || Array.IndexOf(names, p.Name) >= 0, p.Name));
        Assert.NotNull(patches.GetMethod("SetDeliveryStateFinalizer", flags)!.GetCustomAttribute<HarmonyLib.HarmonyFinalizer>());
    }

    [Fact]
    public void DeliveryShopDockPatchTargetsExist()
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
        Type shop = typeof(S1DeliveryUi.DeliveryShop);

        Assert.NotNull(shop.GetMethod("RefreshLoadingDockUI", flags));
        Assert.NotNull(shop.GetMethod("SubmitOrder", flags));
        MethodInfo? selected = shop.GetMethod("LoadingDockDropdownSelected", flags);
        Assert.NotNull(selected);
        Assert.Equal("index", selected!.GetParameters().Single().Name);
        Assert.NotNull((MemberInfo?)shop.GetField("loadingDockIndex", flags) ?? shop.GetProperty("loadingDockIndex", flags));
        Assert.NotNull((MemberInfo?)shop.GetField("destinationProperty", flags) ?? shop.GetProperty("destinationProperty", flags));
    }

    [Fact]
    public void DockDropdownSkipsReservedDocks()
    {
        // Docks #1 and #3 are live, dock #2 is a placeholder.
        var offered = new[] { 0, 2 };

        Assert.Equal(1, DeliveryDestinationRuntime.OptionOfDock(offered, 1));
        Assert.Equal(2, DeliveryDestinationRuntime.OptionOfDock(offered, 3));
        Assert.Equal(0, DeliveryDestinationRuntime.OptionOfDock(offered, 2));
        Assert.Equal(0, DeliveryDestinationRuntime.OptionOfDock(offered, 0));
        Assert.Equal(1, DeliveryDestinationRuntime.DockOfOption(offered, 1));
        Assert.Equal(3, DeliveryDestinationRuntime.DockOfOption(offered, 2));
        Assert.Equal(0, DeliveryDestinationRuntime.DockOfOption(offered, 0));
        Assert.Equal(0, DeliveryDestinationRuntime.DockOfOption(offered, 3));
    }

    // --- held arrivals ---------------------------------------------------------------------------------

    [Fact]
    public void HeldArrivalInstallsTheSceneHookAndIsClearedOnSceneChange()
    {
        Assert.False(DeliveryDestinationRuntime.LifecycleHookedForTesting);

        DeliveryDestinationRuntime.Remember("d1", NewArrival());

        Assert.True(DeliveryDestinationRuntime.LifecycleHookedForTesting);
        Assert.Equal(1, DeliveryDestinationRuntime.HeldCount);
        DeliveryDestinationRuntime.SceneChangeForTesting();
        Assert.Equal(0, DeliveryDestinationRuntime.HeldCount);
        Assert.Contains(_log, line => line.StartsWith("info: Scene change: 1 held delivery arrival(s) dropped"));
    }

    [Fact]
    public void OnlyOneRetryClaimsAHeldArrival()
    {
        var arrival = NewArrival();
        DeliveryDestinationRuntime.Remember("d1", arrival);

        Assert.True(DeliveryDestinationRuntime.Claim("d1", arrival));
        Assert.False(DeliveryDestinationRuntime.Claim("d1", arrival));

        var replacement = NewArrival();
        DeliveryDestinationRuntime.Remember("d1", replacement);
        Assert.False(DeliveryDestinationRuntime.Claim("d1", arrival));
        Assert.True(DeliveryDestinationRuntime.IsCurrent("d1", replacement));
    }

    [Fact]
    public void FailedArrivalStaysHeldOnlyWhenItDidNotGoThrough()
    {
        var arrival = NewArrival();
        var error = new InvalidOperationException("boom");

        Assert.True(DeliveryDestinationRuntime.AfterFailedArrival("d1", arrival, arrived: false, "test", error));
        Assert.True(DeliveryDestinationRuntime.IsCurrent("d1", arrival));

        Assert.True(DeliveryDestinationRuntime.Claim("d1", arrival));
        Assert.False(DeliveryDestinationRuntime.AfterFailedArrival("d1", arrival, arrived: true, "test", error));
        Assert.Equal(0, DeliveryDestinationRuntime.HeldCount);

        var heldAgain = NewArrival();
        DeliveryDestinationRuntime.Remember("d1", heldAgain);
        Assert.False(DeliveryDestinationRuntime.AfterFailedArrival("d1", arrival, arrived: false, "test", error));
        Assert.True(DeliveryDestinationRuntime.IsCurrent("d1", heldAgain));
        Assert.Equal(3, _log.Count(line => line.StartsWith("error: Held delivery d1")));
    }

    // --- dock slots ------------------------------------------------------------------------------------

    [Fact]
    public void DisposedDockKeepsTheIndexesOfLaterDocks()
    {
        var book = NewBook();
        var native = new FakeDock("native");
        var a = new FakeDock("a");
        var b = new FakeDock("b");
        object userA = new object(), userB = new object();
        var docks = new List<FakeDock?> { native };

        Assert.Equal(global::S1API.Internal.Deliveries.DockSlotChange.Attached, Apply(book, userA, a, docks, out int indexA));
        Assert.Equal(global::S1API.Internal.Deliveries.DockSlotChange.Attached, Apply(book, userB, b, docks, out int indexB));
        Assert.Equal(1, indexA);
        Assert.Equal(2, indexB);

        Assert.Equal(global::S1API.Internal.Deliveries.DockSlotRelease.Reserved, book.Release("P", userA, docks, Placeholder, out int released));

        Assert.Equal(1, released);
        Assert.Equal(3, docks.Count);
        Assert.Same(native, docks[0]);
        Assert.True(docks[1]!.IsPlaceholder);
        Assert.True(book.IsPlaceholder(docks[1]!));
        Assert.Same(b, docks[2]);
    }

    [Fact]
    public void ReservedSlotIsReusedOnlyByTheSameDock()
    {
        var book = NewBook();
        var a = new FakeDock("a");
        var docks = new List<FakeDock?>();
        object userA = new object();
        Apply(book, userA, a, docks, out _);
        Apply(book, new object(), new FakeDock("b"), docks, out _);
        book.Release("P", userA, docks, Placeholder, out _);

        Apply(book, new object(), new FakeDock("c"), docks, out int other);
        Apply(book, new object(), a, docks, out int same);

        Assert.Equal(2, other);
        Assert.Equal(0, same);
        Assert.Same(a, docks[0]);
        Assert.Equal(3, docks.Count);
    }

    [Fact]
    public void ReservedSlotRecognizesTheDockByGuid()
    {
        var book = NewBook();
        var docks = new List<FakeDock?>();
        object user = new object();
        book.Apply("P", user, new FakeDock("a"), "guid-a", docks, Placeholder, out _);
        book.Apply("P", new object(), new FakeDock("b"), "guid-b", docks, Placeholder, out _);
        book.Release("P", user, docks, Placeholder, out _);

        book.Apply("P", new object(), new FakeDock("a again"), "GUID-A", docks, Placeholder, out int index);

        Assert.Equal(0, index);
        Assert.Equal("a again", docks[0]!.Name);
    }

    [Fact]
    public void ReloadKeepsEverySlotWhateverTheProviderOrder()
    {
        var book = NewBook();
        var native = new FakeDock("native");
        object userA = new object(), userB = new object();
        var docks = new List<FakeDock?> { native };
        Apply(book, userA, new FakeDock("a"), docks, out _);
        Apply(book, userB, new FakeDock("b"), docks, out _);

        book.ResetScene();
        var reloaded = new List<FakeDock?> { new FakeDock("native after reload") };
        var b2 = new FakeDock("b after reload");

        // B's provider is ready first, A's provider still returns null.
        Assert.True(book.Reserve("P", reloaded, Placeholder));
        Assert.Equal(global::S1API.Internal.Deliveries.DockSlotChange.Attached, Apply(book, userB, b2, reloaded, out int indexB));
        Assert.Equal(global::S1API.Internal.Deliveries.DockSlotChange.None, Apply(book, userA, null, reloaded, out int indexA));

        Assert.Equal(2, indexB);
        Assert.Equal(1, indexA);
        Assert.Equal(3, reloaded.Count);
        Assert.True(reloaded[1]!.IsPlaceholder);
        Assert.Same(b2, reloaded[2]);
        Assert.Contains(_log, line => line == "info: Loading dock slots of 'P' restored: 2 reserved slot(s) keep their dock numbers.");
    }

    [Fact]
    public void ProviderResultIsReconciledWithTheSlot()
    {
        var book = NewBook();
        var a = new FakeDock("a");
        var a2 = new FakeDock("a rebuilt");
        object user = new object();
        var docks = new List<FakeDock?>();

        Assert.Equal(global::S1API.Internal.Deliveries.DockSlotChange.Attached, Apply(book, user, a, docks, out _));
        Assert.Equal(global::S1API.Internal.Deliveries.DockSlotChange.None, Apply(book, user, a, docks, out _));
        Assert.Equal(global::S1API.Internal.Deliveries.DockSlotChange.Released, Apply(book, user, null, docks, out int released));
        Assert.True(docks[released]!.IsPlaceholder);
        Assert.Equal(global::S1API.Internal.Deliveries.DockSlotChange.Attached, Apply(book, user, a2, docks, out int attached));

        Assert.Equal(released, attached);
        Assert.Same(a2, docks[attached]);
        Assert.Single(docks);
    }

    [Fact]
    public void DestroyedDockIsReplacedByAPlaceholder()
    {
        var book = NewBook();
        var a = new FakeDock("a");
        object user = new object();
        var docks = new List<FakeDock?>();
        Apply(book, user, a, docks, out _);

        a.Destroyed = true;

        Assert.True(book.Reserve("P", docks, Placeholder));
        Assert.True(docks[0]!.IsPlaceholder);
    }

    [Fact]
    public void DockTheBookDidNotAddIsNeverRemoved()
    {
        var book = NewBook();
        var native = new FakeDock("native");
        object user = new object();
        var docks = new List<FakeDock?> { native };

        Assert.Equal(global::S1API.Internal.Deliveries.DockSlotChange.Shared, Apply(book, user, native, docks, out int index));
        Assert.Equal(0, index);
        Assert.Equal(global::S1API.Internal.Deliveries.DockSlotRelease.NotOwned, book.Release("P", user, docks, Placeholder, out _));

        Assert.Single(docks);
        Assert.Same(native, docks[0]);
    }

    [Fact]
    public void SharedModDockStaysUntilItsLastRegistrationIsDisposed()
    {
        var book = NewBook();
        var shared = new FakeDock("shared");
        object first = new object(), second = new object();
        var docks = new List<FakeDock?>();

        Assert.Equal(global::S1API.Internal.Deliveries.DockSlotChange.Attached, Apply(book, first, shared, docks, out int a));
        Assert.Equal(global::S1API.Internal.Deliveries.DockSlotChange.None, Apply(book, second, shared, docks, out int b));
        Assert.Equal(a, b);

        Assert.Equal(global::S1API.Internal.Deliveries.DockSlotRelease.StillUsed, book.Release("P", first, docks, Placeholder, out _));
        Assert.Same(shared, docks[a]);
        Assert.Equal(global::S1API.Internal.Deliveries.DockSlotRelease.Reserved, book.Release("P", second, docks, Placeholder, out _));
        Assert.True(docks[a]!.IsPlaceholder);
    }

    [Fact]
    public void SlotTakenByAForeignDockMovesToTheEnd()
    {
        var book = NewBook();
        object user = new object();
        var docks = new List<FakeDock?>();
        Apply(book, user, new FakeDock("a"), docks, out _);

        book.ResetScene();
        var reloaded = new List<FakeDock?> { new FakeDock("new native dock") };

        Assert.True(book.Reserve("P", reloaded, Placeholder));
        Assert.Equal(1, book.IndexOf("P", user));
        Assert.True(reloaded[1]!.IsPlaceholder);
        Assert.Contains(_log, line => line.StartsWith("warn: Loading dock slot #1 of 'P' is taken by a dock S1API did not add"));
    }

    [Fact]
    public void AnotherSaveDropsTheReservedSlots()
    {
        var book = NewBook();
        object user = new object();
        Assert.False(book.UseSave("SaveGame_1"));
        var docks = new List<FakeDock?> { new FakeDock("native") };
        Apply(book, user, new FakeDock("a"), docks, out _);
        book.Release("P", user, docks, Placeholder, out _);

        book.ResetScene();
        Assert.False(book.UseSave(""));
        Assert.False(book.UseSave("savegame_1"));
        var sameSave = new List<FakeDock?> { new FakeDock("native") };
        Assert.True(book.Reserve("P", sameSave, Placeholder));
        Assert.Equal(2, sameSave.Count);

        book.ResetScene();
        Assert.True(book.UseSave("SaveGame_2"));
        var otherSave = new List<FakeDock?> { new FakeDock("native") };
        Assert.False(book.Reserve("P", otherSave, Placeholder));
        Assert.Single(otherSave);
        Assert.Contains(_log, line => line.StartsWith("info: Another save is loaded: 1 loading dock slot(s)"));

        Apply(book, new object(), new FakeDock("b"), otherSave, out int index);
        Assert.Equal(1, index);
    }

    private global::S1API.Internal.Deliveries.DockSlotBook<FakeDock> NewBook() =>
        new global::S1API.Internal.Deliveries.DockSlotBook<FakeDock>(
            (x, y) => ReferenceEquals(x, y),
            dock => !dock.Destroyed,
            message => _log.Add("info: " + message),
            message => _log.Add("warn: " + message));

    private static global::S1API.Internal.Deliveries.DockSlotChange Apply(
        global::S1API.Internal.Deliveries.DockSlotBook<FakeDock> book, object user, FakeDock? dock, List<FakeDock?> docks, out int index) =>
        book.Apply("P", user, dock, null, docks, Placeholder, out index);

    private static FakeDock Placeholder(int index) => new FakeDock("placeholder #" + (index + 1)) { IsPlaceholder = true };

    private static DeliveryDestinationRuntime.HeldArrival NewArrival() =>
        new DeliveryDestinationRuntime.HeldArrival(
            TestObjectFactory.CreateUninitialized<S1Delivery.DeliveryInstance>(),
            S1Delivery.EDeliveryStatus.InTransit);

    private sealed class FakeDock
    {
        public FakeDock(string name)
        {
            Name = name;
        }

        public string Name { get; }
        public bool IsPlaceholder { get; init; }
        public bool Destroyed { get; set; }
    }
}

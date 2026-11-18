using System.Reflection;
using S1API.Deliveries;
using S1API.Internal.Deliveries;
using UnityEngine;

#if IL2CPPMELON
using S1Delivery = Il2CppScheduleOne.Delivery;
using S1DeliveryUi = Il2CppScheduleOne.UI.Phone.Delivery;
using S1Property = Il2CppScheduleOne.Property;
using S1Vehicles = Il2CppScheduleOne.Vehicles;
#elif MONOMELON
using S1Delivery = ScheduleOne.Delivery;
using S1DeliveryUi = ScheduleOne.UI.Phone.Delivery;
using S1Property = ScheduleOne.Property;
using S1Vehicles = ScheduleOne.Vehicles;
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
        Assert.NotNull(typeof(S1DeliveryUi.DeliveryShop).GetMethod("RefreshDestinationUI", flags));
        Assert.NotNull(typeof(S1Delivery.DeliveryManager).GetMethod("OnTimePass", flags));
        Assert.Equal(typeof(string), typeof(S1Delivery.DeliveryManager).GetMethod("GetSaveString", flags)!.ReturnType);
        Assert.Single(typeof(S1Vehicles.LandVehicle).GetMethods(flags), m => m.Name == "Load");
        Assert.NotNull(typeof(S1Delivery.DeliveryVehicle).GetProperty("ActiveDelivery"));
#if MONOMELON
        Assert.NotNull(HarmonyLib.AccessTools.PropertySetter(typeof(S1Delivery.DeliveryInstance), "ActiveVehicle"));
#else
        Assert.NotNull(typeof(S1Delivery.DeliveryInstance).GetProperty("_ActiveVehicle_k__BackingField"));
#endif
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
        Assert.NotNull((MemberInfo?)shop.GetField("LoadingDockDropdown", flags) ?? shop.GetProperty("LoadingDockDropdown", flags));
        // The one-line setter LoadingDockDropdownSelected must stay unpatched (IL2CPP folds identical bodies).
        Assert.Null(typeof(global::S1API.Internal.Patches.DeliveryDestinationPatches).GetMethod("LoadingDockDropdownSelectedPrefix",
            BindingFlags.Static | BindingFlags.NonPublic));
#if MONOMELON
        // The Mono runtime binds both members with AccessTools.Field.
        Assert.NotNull(shop.GetField("loadingDockIndex", flags));
        Assert.NotNull(shop.GetField("destinationProperty", flags));
#else
        Assert.NotNull((MemberInfo?)shop.GetField("loadingDockIndex", flags) ?? shop.GetProperty("loadingDockIndex", flags));
        Assert.NotNull((MemberInfo?)shop.GetField("destinationProperty", flags) ?? shop.GetProperty("destinationProperty", flags));
#endif
    }

    [Fact]
    public void DockPickUsesTheListThePlayerSaw()
    {
        // The dropdown showed docks #1 and #3; dock #2 was attached while it stayed open.
        var shown = new[] { 0, 2 };

        Assert.Equal(3, DeliveryDestinationRuntime.PickedDock(shown, 2, _ => true, out bool gone));
        Assert.False(gone);
        Assert.Equal(1, DeliveryDestinationRuntime.PickedDock(shown, 1, _ => true, out gone));
        Assert.False(gone);
        Assert.Equal(0, DeliveryDestinationRuntime.PickedDock(shown, 0, _ => false, out gone));
        Assert.False(gone);

        // Dock #3 is gone since the list was shown: no other dock is picked in its place.
        Assert.Equal(0, DeliveryDestinationRuntime.PickedDock(shown, 2, index => index != 2, out gone));
        Assert.True(gone);
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

    [Fact]
    public void RefreshFromAHoldDoesNotCheckTheSameArrivalAgain()
    {
        var arrival = NewArrival();
        DeliveryDestinationRuntime.Remember("d1", arrival);
        int calls = 0;
        DeliveryDestinationRuntime.RetryReasonForTesting = _ =>
        {
            calls++;
            // Bounded, so a missing guard fails the count instead of overflowing the stack.
            if (calls < 5)
                DeliveryDestinationRuntime.Refresh("nested");
            return "held by test";
        };

        DeliveryDestinationRuntime.Refresh("outer");

        Assert.Equal(1, calls);
        Assert.True(DeliveryDestinationRuntime.IsCurrent("d1", arrival));
        Assert.Contains(_log, line => line == "debug: Held delivery d1 is still being checked by an outer retry; this retry (nested) skips it.");

        DeliveryDestinationRuntime.Refresh("next minute");
        Assert.Equal(2, calls);
    }

    [Fact]
    public void ProviderThatRefreshesIsNotAskedAgainInsideItsOwnCall()
    {
        object registration = new object();
        int calls = 0;
        bool nestedAsked = true;
        Func<Transform?> provider = null!;
        provider = () =>
        {
            calls++;
            if (calls < 5)
                nestedAsked = DeliveryDestinationRuntime.AskProvider(registration, provider, "P", "test", out _);
            return null;
        };

        Assert.True(DeliveryDestinationRuntime.AskProvider(registration, provider, "P", "test", out Transform? dock));

        Assert.Null(dock);
        Assert.Equal(1, calls);
        Assert.False(nestedAsked);
        Assert.Contains(_log, line => line == "debug: Loading dock provider for 'P' from test is still running; the nested refresh does not ask it again.");

        Assert.False(DeliveryDestinationRuntime.AskProvider(registration, () => throw new InvalidOperationException("boom"), "P", "test", out _));
        Assert.True(DeliveryDestinationRuntime.AskProvider(registration, () => null, "P", "test", out _));
        Assert.True(DeliveryDestinationRuntime.AskProvider(new object(), provider, "P", "test", out _));
    }

    [Fact]
    public void HoldThatDisposesItselfDoesNotHold()
    {
        IDisposable? self = null;
        int laterCalls = 0;
        self = DeliveryDestinationRuntime.AddArrivalHold(_ =>
        {
            self!.Dispose();
            return true;
        });
        using IDisposable later = DeliveryDestinationRuntime.AddArrivalHold(_ =>
        {
            laterCalls++;
            return false;
        });

        var wrapped = new Delivery(TestObjectFactory.CreateUninitialized<S1Delivery.DeliveryInstance>());

        Assert.Null(DeliveryDestinationRuntime.AskHolds(wrapped, "d1"));
        Assert.Equal(1, laterCalls);
        Assert.Equal(1, DeliveryDestinationRuntime.HoldCount);
        Assert.Contains(_log, line => line.Contains("was removed while it ran; its answer for delivery d1 does not count"));
    }

    [Fact]
    public void HoldDisposedByAnEarlierHoldIsNotAsked()
    {
        IDisposable? second = null;
        int secondCalls = 0;
        using IDisposable first = DeliveryDestinationRuntime.AddArrivalHold(_ =>
        {
            second!.Dispose();
            return false;
        });
        second = DeliveryDestinationRuntime.AddArrivalHold(_ =>
        {
            secondCalls++;
            return true;
        });
        int thirdCalls = 0;
        using IDisposable third = DeliveryDestinationRuntime.AddArrivalHold(_ =>
        {
            thirdCalls++;
            return false;
        });

        var wrapped = new Delivery(TestObjectFactory.CreateUninitialized<S1Delivery.DeliveryInstance>());

        Assert.Null(DeliveryDestinationRuntime.AskHolds(wrapped, "d1"));
        Assert.Equal(0, secondCalls);
        Assert.Equal(1, thirdCalls);
        Assert.Equal(2, DeliveryDestinationRuntime.HoldCount);
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
    public void SavedDeliveriesFollowAMovedSlotAndTheForeignDockStays()
    {
        var book = NewBook();
        object user = new object();
        var docks = new List<FakeDock?>();
        Apply(book, user, new FakeDock("a"), docks, out _);
        Assert.Equal(0, book.Moves);
        Assert.Empty(book.TakeMoves());

        book.ResetScene();
        var foreign = new FakeDock("new native dock");
        var reloaded = new List<FakeDock?> { foreign };
        Assert.True(book.Reserve("P", reloaded, Placeholder));

        // A delivery saved to dock #1 now goes to dock #2, where the slot waits for dock A.
        Assert.Same(foreign, reloaded[0]);
        Assert.Equal(1, book.Moves);
        var move = Assert.Single(book.TakeMoves());
        Assert.Equal(("P", 0, 1), (move.Code, move.From, move.To));
        Assert.Empty(book.TakeMoves());

        // A second reservation in the same scene moves nothing more.
        Assert.False(book.Reserve("P", reloaded, Placeholder));
        Assert.Equal(1, book.Moves);
        Assert.Empty(book.TakeMoves());

        // The rebuilt dock takes the moved slot.
        Assert.Equal(global::S1API.Internal.Deliveries.DockSlotChange.Attached, Apply(book, user, new FakeDock("a rebuilt"), reloaded, out int index));
        Assert.Equal(1, index);
        Assert.Same(foreign, reloaded[0]);

        // The next scene moves nothing again: the slot already sits behind the foreign dock.
        book.ResetScene();
        var again = new List<FakeDock?> { new FakeDock("native again") };
        book.Reserve("P", again, Placeholder);
        Assert.Empty(book.TakeMoves());
        Assert.Equal(1, book.IndexOf("P", user));
    }

    [Fact]
    public void FollowerMovesOnlyDeliveriesThatTargetedTheOldIndexAtTheMove()
    {
        var follower = new DockMoveFollower();
        follower.Add(new[] { new DockSlotMove("P", 0, 2) });

        var before = new[]
        {
            new DockTarget("saved", "P", 0),
            new DockTarget("other dock", "P", 1),
            new DockTarget("other property", "Q", 0),
        };
        var changes = follower.Apply(before);

        var change = Assert.Single(changes);
        Assert.Equal((0, 0, 2), (change.Position, change.From, change.To));
        Assert.Equal(0, follower.PendingCount);
        Assert.Equal(1, follower.FollowedCount);

        // A delivery ordered later to the dock that now has number 1 stays there; the saved one already moved.
        var later = new[] { new DockTarget("saved", "P", 2), new DockTarget("new order", "P", 0) };
        Assert.Empty(follower.Apply(later));
    }

    [Fact]
    public void FollowerRedirectsAReloadOfAnOlderSaveAndFollowsAChainedMove()
    {
        var follower = new DockMoveFollower();
        follower.Add(new[] { new DockSlotMove("P", 0, 2) });
        follower.Apply(new[] { new DockTarget("saved", "P", 0) });

        // The save written before the move is loaded again: the delivery is back at dock #1.
        var reloaded = new[] { new DockTarget("saved", "P", 0) };
        var change = Assert.Single(follower.Apply(reloaded));
        Assert.Equal(2, change.To);

        // The slot moves once more; the delivery follows it, and so does one saved to the slot's second number.
        follower.Add(new[] { new DockSlotMove("P", 2, 3) });
        var changes = follower.Apply(new[] { new DockTarget("saved", "P", 2), new DockTarget("saved at 3", "P", 2) });
        Assert.Equal(2, changes.Count);
        Assert.All(changes, c => Assert.Equal(3, c.To));

        follower.Clear();
        Assert.False(follower.HasWork);
        Assert.Empty(follower.Apply(new[] { new DockTarget("saved", "P", 0) }));
    }

    [Fact]
    public void RegistrationThatLeavesASharedNativeDockGetsASlotOfItsOwn()
    {
        var book = NewBook();
        object user = new object();
        var native = new FakeDock("native");
        var docks = new List<FakeDock?> { native };
        Assert.Equal(global::S1API.Internal.Deliveries.DockSlotChange.Shared, Apply(book, user, native, docks, out _));

        var own = new FakeDock("own");
        Assert.Equal(global::S1API.Internal.Deliveries.DockSlotChange.Attached, Apply(book, user, own, docks, out int index));

        Assert.Equal(1, index);
        Assert.Same(native, docks[0]);
        Assert.Same(own, docks[1]);
        Assert.Contains(_log, line => line.StartsWith("info: A loading dock registration of 'P' no longer returns dock #1"));
    }

    [Fact]
    public void SlotSharedByTwoRegistrationsKeepsTheLiveDockOfTheOther()
    {
        var book = NewBook();
        var x = new FakeDock("x");
        object first = new object(), second = new object();
        var docks = new List<FakeDock?>();
        Apply(book, first, x, docks, out int slot);
        Apply(book, second, x, docks, out _);

        Assert.Equal(global::S1API.Internal.Deliveries.DockSlotChange.Conflict, Apply(book, second, new FakeDock("y"), docks, out int index));

        Assert.Equal(slot, index);
        Assert.Same(x, docks[slot]);
        Assert.Single(docks);
    }

    [Fact]
    public void SceneResetDropsSlotsOfDocksTheBookDidNotAdd()
    {
        var book = NewBook();
        object nativeUser = new object();
        var docks = new List<FakeDock?> { new FakeDock("native") };
        Assert.Equal(global::S1API.Internal.Deliveries.DockSlotChange.Shared, Apply(book, nativeUser, docks[0], docks, out _));

        book.ResetScene();
        var reloaded = new List<FakeDock?>();
        var modDock = new FakeDock("mod dock");
        object first = new object(), second = new object();

        Assert.Equal(global::S1API.Internal.Deliveries.DockSlotChange.Attached, Apply(book, first, modDock, reloaded, out int index));
        Assert.Equal(0, index);
        Assert.Equal(global::S1API.Internal.Deliveries.DockSlotChange.None, Apply(book, second, modDock, reloaded, out int shared));
        Assert.Equal(0, shared);

        // The second registration still provides the dock, so disposing the first keeps it.
        Assert.Equal(global::S1API.Internal.Deliveries.DockSlotRelease.StillUsed, book.Release("P", first, reloaded, Placeholder, out _));
        Assert.Same(modDock, reloaded[0]);
        Assert.Equal(-1, book.IndexOf("P", nativeUser));
    }

    [Fact]
    public void DockReclaimsItsSlotWhenNoRegistrationProvidesIt()
    {
        var book = NewBook();
        var x = new FakeDock("x");
        object a = new object(), other = new object(), b = new object();
        var docks = new List<FakeDock?>();
        Apply(book, a, x, docks, out int slot);
        Apply(book, other, new FakeDock("y"), docks, out _);

        Assert.Equal(global::S1API.Internal.Deliveries.DockSlotChange.Released, Apply(book, a, null, docks, out _));
        Assert.True(docks[slot]!.IsPlaceholder);

        Assert.Equal(global::S1API.Internal.Deliveries.DockSlotChange.Attached, Apply(book, b, x, docks, out int index));
        Assert.Equal(slot, index);
        Assert.Same(x, docks[slot]);
        Assert.Equal(2, docks.Count);

        // Another dock does not take the slot while no registration provides a live dock there.
        Apply(book, b, null, docks, out _);
        Apply(book, new object(), new FakeDock("z"), docks, out int z);
        Assert.Equal(2, z);
    }

    [Fact]
    public void SharedDockKeepsTheKeyThatMadeTheReservation()
    {
        var book = NewBook();
        var docks = new List<FakeDock?>();
        object first = new object(), second = new object();
        var x = new FakeDock("x");
        book.Apply("P", first, x, "ModA|guid:x", docks, Placeholder, out int slot);
        book.Apply("P", second, x, "ModB|guid:x", docks, Placeholder, out _);
        book.Apply("P", new object(), new FakeDock("later"), "ModC|guid:later", docks, Placeholder, out _);
        book.Release("P", first, docks, Placeholder, out _);
        book.Release("P", second, docks, Placeholder, out _);

        book.Apply("P", new object(), new FakeDock("x rebuilt by A"), "ModA|guid:x", docks, Placeholder, out int index);

        Assert.Equal(slot, index);
        Assert.Equal("x rebuilt by A", docks[slot]!.Name);
        Assert.Equal(2, docks.Count);
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

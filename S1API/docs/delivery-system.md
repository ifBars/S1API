# Deliveries

`S1API.Deliveries` provides read-only, cross-runtime wrappers around supplier deliveries, property loading docks, and order-history receipts. It is intended for mods that need to observe delivery state without depending on Schedule I's native delivery, vehicle, UI, or networking types.

This API describes property/shop deliveries managed by the game's delivery app. Supplier dead-drop orders are a separate flow represented by `NPCSupplier.Status`, `MinutesUntilDeadDropReady`, and `OnDeadDropReady`. Customer drop-off points are another separate system documented in [Delivery Location Registry](delivery-location-registry.md).

## Access a Supplier's Deliveries

The most direct entry point is `NPCSupplier.ActiveDeliveries`:

```csharp
using System;
using S1API.Entities;

NPCSupplier supplier = NPC.Get<WarehouseSupplier>()?.Supplier
    ?? throw new InvalidOperationException("The supplier is not loaded.");

foreach (var delivery in supplier.ActiveDeliveries)
{
    MelonLoader.MelonLogger.Msg($"{delivery.Id}: {delivery.Status}");
}
```

`NPCSupplier.Shop` and `NPCSupplier.Stash` expose the supplier's related public shop and storage wrappers. They may be `null` while a world instance is starting or shutting down.

## DeliveryRegistry

Use `DeliveryRegistry` when you need a world-wide view rather than deliveries for one NPC:

```csharp
using S1API.Deliveries;

var active = DeliveryRegistry.GetAll();
if (active.Count > 0)
{
    Delivery? sameDelivery = DeliveryRegistry.GetById(active[0].Id);
    if (sameDelivery?.Shop is { } shop)
    {
        var forShop = DeliveryRegistry.GetForShop(shop);
    }
}

var receipts = DeliveryRegistry.GetHistory();
```

The read APIs are:

- `GetAll()`: Returns active deliveries currently known to the loaded world.
- `GetById(string)`: Finds an active delivery by its persistent delivery ID, or returns `null` when it is not present.
- `GetForShop(Shop)`: Returns active deliveries associated with a public `Shop` wrapper.
- `GetForShop(string)`: Returns active deliveries whose stable store name exactly matches the supplied value.
- `GetHistory()`: Returns the delivery order receipts displayed by the current save. Receipts are recorded when orders are submitted, not when their vehicles are unloaded.

Registry results reflect the local peer's loaded state. Query after the world has loaded, and do not retain wrappers across scene changes.

## Public Models

The delivery surface is deliberately split by concern:

- `Delivery`: A read-only live view of an active supplier delivery. It exposes `Id`, `StoreName`, `DestinationCode`, the wrapped `Destination` and `LoadingDock`, `LoadingDockIndex`, `Status`, `MinutesUntilArrival`, `Items`, `Shop`, and `ActiveVehicle`. `Shop`, `Destination`, or `LoadingDock` may be unavailable during world setup, while `ActiveVehicle` is normally unavailable until arrival.
- `LoadingDock`: A read-only live view of one property dock, including stable identity, transit slots, accepting/destroyed state, and its dynamic and delivery-vehicle occupants.
- `DeliveryItem`: An immutable `ItemId` and `Quantity` entry from an order.
- `DeliveryReceipt`: An immutable delivery order-details snapshot with the delivery identity, destination, loading dock, and ordered items. `GetHistory()` returns receipts that were recorded in order history.
- `DeliveryStatus`: The public delivery lifecycle state.

`DeliveryStatus` progresses through `InTransit`, `Waiting`, `Arrived`, and `Completed`. Observe the value; do not use an enum cast to invoke native state changes.

`Delivery.Items` returns an immutable snapshot, while its scalar properties reflect the live delivery. Call `Delivery.ToReceipt()` when you need to retain an immutable snapshot of the current order.

The wrappers do not expose Schedule I's native `DeliveryInstance`, delivery shop, or delivery-vehicle component. Native delivery creation and lifecycle transitions remain hidden so mods cannot bypass server authority. Related `Shop`, `PropertyWrapper`, and `LandVehicle` objects use their existing S1API wrappers and retain the capabilities documented by those APIs.

## Loading Docks

Enumerate all docks owned by a property through `PropertyWrapper.LoadingDocks`, or follow the selected dock directly from an active delivery:

```csharp
using S1API.Deliveries;

var deliveries = DeliveryRegistry.GetAll();
Delivery? delivery = deliveries.Count == 0 ? null : deliveries[0];
LoadingDock? dock = delivery?.LoadingDock;
if (dock is not null)
{
    MelonLoader.MelonLogger.Msg(
        $"{dock.Name} ({dock.GUID}): {dock.OutputSlots.Count} output slots");

    dock.DynamicOccupantChanged += (previous, current) =>
        MelonLoader.MelonLogger.Msg(
            $"Detected vehicle: {previous?.GUID ?? "none"} -> {current?.GUID ?? "none"}");
    dock.StaticOccupantChanged += (previous, current) =>
        MelonLoader.MelonLogger.Msg(
            $"Delivery vehicle: {previous?.GUID ?? "none"} -> {current?.GUID ?? "none"}");
    dock.AcceptingItemsChanged += (previous, current) =>
        MelonLoader.MelonLogger.Msg($"Accepting items: {previous} -> {current}");
}
```

`InputSlots` and `OutputSlots` are immutable collection snapshots containing live `ItemSlotInstance` wrappers. `DynamicOccupant` represents a nearby stopped vehicle detected by the dock. `StaticOccupant` represents the delivery vehicle assigned during supplier-delivery arrival. `IsInUse` is true when either occupant exists.

Dock wrappers are cached for the loaded scene, so a dock reached through a property and an active delivery shares event subscriptions. Resolve wrappers again after a scene or save transition. Events report native state observed by the local peer and do not add a new replication channel.

## Lifecycle Events

`DeliveryRegistry` exposes observation-only lifecycle events:

```csharp
DeliveryRegistry.Created += HandleCreated;
DeliveryRegistry.StatusChanged += HandleStatusChanged;
DeliveryRegistry.Completed += HandleCompleted;

private static void HandleCreated(Delivery delivery)
{
    MelonLoader.MelonLogger.Msg($"Delivery {delivery.Id} created.");
}

private static void HandleStatusChanged(
    Delivery delivery,
    DeliveryStatus previous,
    DeliveryStatus current)
{
    MelonLoader.MelonLogger.Msg(
        $"Delivery {delivery.Id}: {previous} -> {current}");
}

private static void HandleCompleted(Delivery delivery)
{
    MelonLoader.MelonLogger.Msg($"Delivery {delivery.Id} completed.");
}
```

Unsubscribe static handlers when your mod shuts down or no longer needs the notifications. Event callbacks observe native transitions; they do not grant authority to modify the delivery.

## Read-Only by Design

Delivery creation, arrival transitions, cargo insertion, vehicle activation, completion, and save hydration are authoritative game operations. S1API owns the internal glue required to keep those operations ordered during normal play, save loading, and late joins.

The initial public API therefore supports observation and lookup only. It does not provide methods to:

- construct an arbitrary delivery;
- force a delivery status;
- assign a native delivery vehicle;
- create, destroy, or mutate a loading dock or its GUID;
- force dock occupancy, accepting state, transit routing, or outline UI;
- invoke native delivery UI or network RPCs.

Use supplier configuration to define what a custom supplier sells, then use `NPCSupplier` and `DeliveryRegistry` to observe the resulting orders.

## See Also

- [Supplier NPCs](supplier-system.md)
- [Delivery Location Registry](delivery-location-registry.md)
- <xref:S1API.Deliveries.DeliveryRegistry>
- <xref:S1API.Deliveries.Delivery>
- <xref:S1API.Deliveries.LoadingDock>

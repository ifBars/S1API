# Supplier NPCs

S1API supports custom physical NPCs that use Schedule I's native supplier flow: dead-drop orders, supplier meetings, debt, delivery unlocks, and phone-shop listings. Configure the role on the prefab, then use `NPCSupplier` for runtime access.

## Create a Supplier

A supplier must be a physical NPC. Set both role properties and configure the supplier's listings in `ConfigurePrefab`:

```csharp
using System;
using S1API.Entities;
using UnityEngine;

public sealed class WarehouseSupplier : NPC
{
    public override bool IsPhysical => true;
    public override bool IsSupplier => true;

    protected override void ConfigurePrefab(NPCPrefabBuilder builder)
    {
        builder.WithIdentity(
                id: "warehouse_supplier",
                firstName: "Morgan",
                lastName: "Reed")
            .WithSpawnPosition(new Vector3(-53.5f, 1.1f, 67.8f))
            .WithSupplierDefaults(supplier => supplier
                .WithOrderLimits(minimum: 250f, maximum: 2500f)
                .WithDeliveryItem("my_registered_supply_item")
                .WithRecommendationMessage(
                    "My friend <NAME> can supply <PRODUCT>. I've passed your number on to them.")
                .WithUnlockHint(
                    "You can now order <PRODUCT> from <NAME>."));
    }
}
```

`IsSupplier` is the role declaration. `WithSupplierDefaults(...)` only supplies optional order, listing, and message configuration; omit it when the native defaults are sufficient.

The string overload of `WithDeliveryItem(...)` is declaration-order safe: S1API
stores the stable ID during NPC prefab discovery and resolves it when supplier
runtime data is materialized. This lets an NPC assembly be discovered before its
mod registers custom items during pre-load. The resolved item must still be
storable to appear in the supplier shop. You can also pass an already registered
`ItemDefinition`, a `StorableItemDefinition`, or multiple item wrappers with
`WithDeliveryItems(...)`.

## Configuration Rules

- A supplier must return `true` from `IsPhysical`. Supplier infrastructure needs
  a world position, its reserved meeting action, stash, shop, and delivery
  vehicle.
- A custom NPC cannot be both a supplier and a dealer. Choose one native root role per NPC type.
- `WithOrderLimits(minimum, maximum)` requires finite values, `minimum >= 0`, `maximum > 0`, and `maximum >= minimum`.
- Every delivery listing must reference a storable item. Register custom items before S1API configures the NPC prefab.
- Use a permanent, unique ID in `WithIdentity(...)`. S1API derives persistent supplier infrastructure from that ID, so changing it breaks continuity with existing saves. When a released supplier must adopt a new runtime NPC ID, opt in to `WithPersistentId("old_supplier_id")` in the supplier defaults. The runtime NPC ID changes while the generated shop, delivery vehicle, and stash keep their former persistent identities. New suppliers should omit this migration-only option.
- Configure supplier defaults in `ConfigurePrefab`, not in `OnCreated`. This keeps host, client, and saved-game prefab data consistent.

S1API creates the native meeting action and supplier-owned stash, shop, and delivery vehicle behind the public API. Do not copy or assign native supplier scene objects yourself.

Do not add an ordinary roaming schedule merely to make a supplier physical.
Native suppliers remain hidden while idle. When the player requests a meeting,
the game activates the reserved location-dialogue action, warps the supplier to
the selected supplier stand point, and makes them visible until the meeting ends.

## Runtime Access

Every `NPC` exposes a `Supplier` wrapper. For a supplier NPC, it provides role state and wrappers for the associated shop, stash, and active property deliveries. Dead-drop orders remain supplier state and are not included in `ActiveDeliveries`:

```csharp
using System;
NPCSupplier supplier = NPC.Get<WarehouseSupplier>()?.Supplier
    ?? throw new InvalidOperationException("The supplier is not loaded.");

MelonLoader.MelonLogger.Msg($"Debt: {supplier.Debt}");
MelonLoader.MelonLogger.Msg($"Dead-drop limit: {supplier.DeadDropLimit}");
MelonLoader.MelonLogger.Msg($"State: {supplier.Status}");

if (supplier.Shop != null)
    MelonLoader.MelonLogger.Msg($"Shop: {supplier.Shop.Name}");

if (supplier.Stash != null)
    MelonLoader.MelonLogger.Msg("Supplier stash is ready.");

foreach (var delivery in supplier.ActiveDeliveries)
    MelonLoader.MelonLogger.Msg($"Delivery {delivery.Id}: {delivery.Status}");
```

| Member | Public type | Purpose |
| --- | --- | --- |
| `Shop` | `S1API.Shops.Shop?` | The isolated shop used for this supplier's order listings. |
| `Stash` | `S1API.Storages.StorageInstance?` | Runtime item and slot access for the supplier's persistent stash. |
| `ActiveDeliveries` | `IReadOnlyList<S1API.Deliveries.Delivery>` | A read-only collection snapshot of live delivery wrappers for this supplier. |

The associated `Shop` and `StorageInstance` wrappers can be unavailable briefly while the world instance is being created or torn down. Read them after `OnCreated`, and still handle `null` during scene transitions.

Other useful members include:

- `IsSupplier`: Whether the wrapped NPC currently has a native supplier root.
- `DeliveriesEnabled`: Whether the player has unlocked delivery orders for this supplier.
- `MinutesUntilDeadDropReady`: Remaining in-game minutes, or `-1` when no dead drop is pending.
- `Unlock()`: Runs the native supplier unlock flow.
- `EndMeeting()`: Ends an active meeting. This is server/host-only and throws when called without server authority.
- `OnDeadDropReady`: Raised when the supplier's dead-drop contents become ready.

```csharp
protected override void OnCreated()
{
    base.OnCreated();
    Supplier.OnDeadDropReady += HandleDeadDropReady;
}

protected override void OnDestroyed()
{
    Supplier.OnDeadDropReady -= HandleDeadDropReady;
    base.OnDestroyed();
}

private void HandleDeadDropReady()
{
    SendTextMessage("Your order is ready.");
}
```

## Server Authority

Supplier state is networked by the game. Run authoritative mutations from the host/server unless the API member explicitly documents a client-safe request flow. Read-only properties and the delivery wrappers are safe for observation on each peer after their world state has loaded.

`EndMeeting()` explicitly enforces server authority. The delivery API intentionally does not expose status mutation or arbitrary delivery creation; see [Deliveries](delivery-system.md).

## Lifecycle and Persistence

S1API handles the native details that make suppliers substantially different from ordinary NPCs:

- fresh supplier framework data per custom NPC type;
- persistent, supplier-owned stash storage;
- isolated phone-shop listings and delivery UI registration;
- a supplier-owned networked delivery vehicle;
- save/load and late-join reconciliation;
- cleanup when the NPC or world scene is destroyed.

These are implementation details, not game types that mods need to coordinate. Use `NPCSupplier`, the existing `Shop` and `StorageInstance` wrappers, and the types in `S1API.Deliveries` instead.

## See Also

- [Custom NPCs](custom-npcs.md)
- [Prefab Configuration](prefab-configuration.md)
- [Deliveries](delivery-system.md)
- [Dealer System](dealer-system.md)
- <xref:S1API.Entities.NPCSupplier>
- <xref:S1API.Entities.Supplier.SupplierDataBuilder>

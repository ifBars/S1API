# Seating Registry

The seating registry exposes world `AvatarSeat` metadata so mods can discover seating positions
and map them to schedule actions.

## Seat Model

Each entry is represented by `S1API.Avatar.Seat` and is populated when `AvatarSeat.Awake` runs.
Key properties:

- `HierarchyPath`: Full transform path (scene root ➝ seat GameObject).
- `SeatSetName`: Parent `AvatarSeatSet` GameObject name (empty if none).
- `IndexInSet`: Seat index within the set's `Seats` array (nullable).
- `SittingPosition` / `SittingRotation`: World pose of the sitting point.
- `AccessPosition` / `AccessRotation`: Entry waypoints NPCs use when approaching.
- `Label`: Friendly label combining set and hierarchy data.

The registry is cleared automatically when gameplay scenes unload to avoid stale references.

## Lookup Helpers

```csharp
foreach (var seat in Seat.GetAll())
{
    MelonLogger.Msg($"Seat: {seat.Label} at {seat.SittingPosition}");
}

var booths = Seat.GetBySeatSet("CafeBooth01");
var boothA = Seat.FindByPathSuffix("Cafe/Booth01/SeatA");
```

Use `ResolveGameSeat()` or `ResolveSeatSet()` when you need the live `AvatarSeat` component.
These methods return `null` if the underlying GameObject has been destroyed.

## Schedule Integration

`SitSpec` continues to target an `AvatarSeatSet`. For seats that are not part of a set, use the
registry data to create your own `AvatarSeatSet` prefab or to place NPCs manually. The `Seat.Label`
and `Seat.HierarchyPath` values make it straightforward to map scene seats to your configuration assets.

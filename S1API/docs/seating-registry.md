# Seating Registry

The seating registry exposes world `AvatarSeat` metadata so mods can discover seating positions
and map them to schedule actions.

## Seat Model

Each entry is represented by `S1API.Avatar.Seat`. Seats are now discovered via a delayed scan
shortly after the `Main` scene initializes (not during `AvatarSeat.Awake`). This avoids early-scene
initialization crashes and ensures all seats are available.
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

`SitSpec` targets an `AvatarSeatSet` and can resolve it by name, hierarchy path, or direct reference.

```csharp
// By name — quick lookup, finds the first matching AvatarSeatSet
plan.SitAtSeatSet("Fast Food Booth", 900, durationMinutes: 60);

// By path — use when multiple seat sets share the same name (e.g. "outdoorbench")
plan.SitAtSeatSet(null, 1650, durationMinutes: 130,
    seatSetPath: "Map/Hyland Point/Region_Docks/WaterFront/OutdoorBench (1)");
```

Use the registry's `HierarchyPath` to discover the correct path for `seatSetPath`. The path is matched
case-insensitively and supports suffix matching, so you can omit leading scene root segments.

**Important:** `durationMinutes` must be positive. The game uses Duration to calculate when the sit
action ends — with duration 0 the action has a zero-width time range and will never trigger.

If the seat set cannot be resolved at runtime, the action is automatically disabled and a warning is
logged. This prevents a NullReferenceException in `NPCEvent_Sit.Started()` that would permanently
break the NPC's schedule.

For seats that are not part of a set, use the registry data to create your own `AvatarSeatSet` prefab
or to place NPCs manually. The `Seat.Label` and `Seat.HierarchyPath` values make it straightforward
to map scene seats to your configuration assets.

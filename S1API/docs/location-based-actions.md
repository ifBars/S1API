# Location-Based Actions

Location-based actions let you schedule an NPC to walk to a world position and **perform an activity when it arrives** — such as taking a smoke break, spray-painting graffiti, drinking, or holding an item. The activity automatically starts on arrival and stops when the action's duration expires.

## Table of Contents

1. [Overview](#overview)
2. [Prefab Requirements](#prefab-requirements)
3. [The LocationBased Builder](#the-locationbased-builder)
4. [Arrive Behaviours](#arrive-behaviours)
   - [SmokeBreak](#smokebreak)
   - [Graffiti](#graffiti)
   - [Drinking](#drinking)
   - [HoldItem](#holditem)
   - [None (walk-only)](#none-walk-only)
5. [EquippablePath Reference](#equippablepath-reference)
6. [Complete Example](#complete-example)
7. [Best Practices](#best-practices)

---

## Overview

```csharp
plan.LocationBased(destination, startTime, durationMinutes)
    // optional modifiers
    .Within(1.5f)
    .Named("MyAction")
    // optional behaviour-specific parameters
    .WithItem(EquippablePath.Phone_Lowered)
    // terminal — commits the action to the plan
    .OnArriveHoldItem();
```

The call to `.LocationBased(...)` returns a `LocationBasedActionSpecBuilder`. You chain modifier methods on it and close with one of the `OnArrive*()` terminals. The terminal commits the action and returns the parent `PrefabScheduleBuilder` so you can keep chaining other schedule actions.

> **Note:** `startTime` uses the same **minutes-from-midnight** format as all other schedule times:
> - `8 * 60` = 8:00 AM (480)
> - `14 * 60` = 2:00 PM (840)

---

## Prefab Requirements

Each arrive behaviour requires a matching component to be added to the NPC's prefab inside `ConfigurePrefab`. Call the appropriate `Ensure*` method before `.WithSchedule(...)`:

| Arrive behaviour | Required `Ensure*` call |
|---|---|
| `OnArriveSmokeBreak()` | `builder.EnsureSmokeBreak()` |
| `OnArriveGraffiti()` | `builder.EnsureGraffiti()` |
| `OnArriveDrinking()` | `builder.EnsureDrinking()` |
| `OnArriveHoldItem()` | `builder.EnsureItemHolding()` |

You can call as many `Ensure*` methods as you need — an NPC may use all four in the same schedule.

```csharp
builder.EnsureSmokeBreak()
       .EnsureGraffiti()
       .EnsureDrinking()
       .EnsureItemHolding()
       .WithSchedule(plan => { ... });
```

---

## The LocationBased Builder

### `plan.LocationBased(destination, startTime, durationMinutes)`

Starts a location-based action sub-builder.

| Parameter | Type | Description |
|---|---|---|
| `destination` | `Vector3` | World position the NPC walks to |
| `startTime` | `int` | Start time in minutes from midnight (e.g. `9 * 60`) |
| `durationMinutes` | `int` | How long the NPC stays at the destination |

### Modifier Methods

All modifiers are optional and can be chained in any order before the terminal.

#### `.Within(float value)`
Distance (world units) within which the NPC is considered to have arrived. Default: `1f`.

```csharp
.LocationBased(spot, 8 * 60, 20)
    .Within(1.5f)
    .OnArriveSmokeBreak()
```

#### `.Named(string value)`
Assigns a debug/event name to this schedule action.

```csharp
.LocationBased(spot, 8 * 60, 20)
    .Named("MorningSmoke")
    .OnArriveSmokeBreak()
```

#### `.FaceDestinationDirection(bool value = true)`
Whether the NPC faces the destination direction while walking. Default: `true`.

#### `.WarpIfSkipped(bool value = true)`
Whether the NPC warps to the destination if this action is skipped (e.g. when loading a save mid-action). Default: `false`.

---

## Arrive Behaviours

### SmokeBreak

The NPC walks to the destination, lights a cigarette, and smokes until the duration expires.

**Prefab requirement:** `builder.EnsureSmokeBreak()`

```csharp
builder.EnsureSmokeBreak()
       .WithSchedule(plan =>
       {
           plan.LocationBased(smokeSpot, 8 * 60, 15)
               .Within(1.5f)
               .Named("MorningSmoke")
               .OnArriveSmokeBreak();
       });
```

**`EnsureSmokeBreak` options:**

```csharp
// Default cigarette prefab, no debug logs
builder.EnsureSmokeBreak();

// Custom cigarette prefab path
builder.EnsureSmokeBreak(cigarettePrefabPath: "MyMod/Cigarette_Lit");

// Enable verbose debug logging for the SmokeBreakBehaviour
builder.EnsureSmokeBreak(debugMode: true);
```

---

### Graffiti

The NPC walks to the destination, finds a spray surface, and tags it until the duration expires. The action uses the game's `GraffitiBehaviour` and awards the appropriate XP/cartel influence when complete.

**Prefab requirement:** `builder.EnsureGraffiti()`

**Spray surface resolution — priority order:**

1. `.WithSpraySurface(guid)` — exact surface by GUID (highest priority)
2. `.WithSpraySurfaceInRegion(region)` — random available surface in the region
3. Nearest available surface to the destination (automatic fallback)

A surface is "available" if `CanBeSprayedByNPCs == true` and `CanBeEdited(checkEditor: true) == true` (not already being edited, not already fully painted).

```csharp
builder.EnsureGraffiti()
       .WithSchedule(plan =>
       {
           // Pick any available surface in Northtown
           plan.LocationBased(graffitiSpot, 14 * 60, 45)
               .WithSpraySurfaceInRegion(Region.Northtown)
               .Named("AfternoonGraffiti")
               .OnArriveGraffiti();

           // Target a specific surface by GUID
           var surface = GraffitiManager.FindNearestUntaggedSurface(somePos);
           plan.LocationBased(graffitiSpot, 14 * 60, 45)
               .WithSpraySurface(surface)
               .OnArriveGraffiti();

           // Or pass the GUID directly
           plan.LocationBased(graffitiSpot, 14 * 60, 45)
               .WithSpraySurface(new System.Guid("xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx"))
               .OnArriveGraffiti();
       });
```

**`EnsureGraffiti` options:**

```csharp
// Default spray paint equippable (resolved from existing scene SprayPaint components)
builder.EnsureGraffiti();

// Explicit spray paint equippable path
builder.EnsureGraffiti(EquippablePath.SprayPaint);

// Custom mod spray paint
builder.EnsureGraffiti(EquippablePath.Custom("MyMod/SprayPaint_AvatarEquippable"));
```

---

### Drinking

The NPC walks to the destination, equips a drink, and plays the drinking animation until the duration expires.

**Prefab requirement:** `builder.EnsureDrinking()`

Use `.WithDrink(EquippablePath)` to specify which drink the NPC holds **for this particular schedule slot**. Each `LocationBased` action can use a different drink; the slot-specific drink overrides the prefab default at runtime.

```csharp
builder.EnsureDrinking()
       .WithSchedule(plan =>
       {
           // Morning coffee
           plan.LocationBased(coffeeSpot, 11 * 60, 15)
               .WithDrink(EquippablePath.Coffee)
               .OnArriveDrinking();

           // Evening beer
           plan.LocationBased(barSpot, 18 * 60, 30)
               .WithDrink(EquippablePath.Beer)
               .OnArriveDrinking();
       });
```

**`EnsureDrinking` options:**

```csharp
// Default drink (Beer)
builder.EnsureDrinking();

// Prefab-level default drink (used when no .WithDrink() is set on the action)
builder.EnsureDrinking(EquippablePath.Coffee);

// Custom drink path
builder.EnsureDrinking(EquippablePath.Custom("MyMod/MyDrink"));
```

---

### HoldItem

The NPC walks to the destination and equips any `AvatarEquippable` item for the duration.

**Prefab requirement:** `builder.EnsureItemHolding()`

Use `.WithItem(EquippablePath)` to specify which item the NPC holds **for this particular schedule slot**. Each slot can use a different item.

```csharp
builder.EnsureItemHolding()
       .WithSchedule(plan =>
       {
           // Hold phone during the day
           plan.LocationBased(phoneSpot, 9 * 60 + 30, 20)
               .WithItem(EquippablePath.Phone_Lowered)
               .OnArriveHoldItem();

           // Hold flashlight at night
           plan.LocationBased(patrolSpot, 22 * 60, 30)
               .WithItem(EquippablePath.Flashlight)
               .OnArriveHoldItem();
       });
```

**`EnsureItemHolding` options:**

```csharp
// Default item (Phone_Lowered)
builder.EnsureItemHolding();

// Prefab-level default (used when no .WithItem() is set on the action)
builder.EnsureItemHolding(EquippablePath.Flashlight);

// Custom item path
builder.EnsureItemHolding(EquippablePath.Custom("MyMod/MyWeapon_AvatarEquippable"));
```

---

### None (walk-only)

Makes the NPC walk to a destination and stand there for the duration without performing any activity.

```csharp
plan.LocationBased(destination, 12 * 60, 30)
    .Within(1f)
    .OnArriveNone();
```

---

## EquippablePath Reference

`EquippablePath` is a type-safe struct that wraps a Resources path. It has an implicit conversion from `string` so existing string constants continue to work.

```csharp
using S1API.Entities.Equippables;
```

### Misc

| Value | Item |
|---|---|
| `EquippablePath.Baton` | Police baton |
| `EquippablePath.Beer` | Beer bottle |
| `EquippablePath.Coffee` | Coffee cup |
| `EquippablePath.Cuke` | Cuke energy drink |
| `EquippablePath.Hammer` | Hammer |
| `EquippablePath.Joint` | Marijuana joint |
| `EquippablePath.Phone_Lowered` | Phone (lowered, natural carry) |
| `EquippablePath.Phone_Raised` | Phone (raised, in use) |
| `EquippablePath.Pipe` | Smoking pipe |
| `EquippablePath.TrashBag` | Trash bag |

### Weapons

| Value | Item |
|---|---|
| `EquippablePath.BrokenBottle` | Broken bottle |
| `EquippablePath.Knife` | Knife |
| `EquippablePath.M1911` | M1911 pistol |
| `EquippablePath.PumpShotgun` | Pump shotgun |
| `EquippablePath.Revolver` | Revolver |
| `EquippablePath.Taser` | Taser |

### Tools

| Value | Item |
|---|---|
| `EquippablePath.Flashlight` | Flashlight |
| `EquippablePath.TrashGrabber` | Trash grabber |
| `EquippablePath.WateringCan` | Watering can |
| `EquippablePath.Trimmers` | Trimmers |

### Drinks

| Value | Item |
|---|---|
| `EquippablePath.Beer` | Beer (also in Misc) |
| `EquippablePath.Coffee` | Coffee (also in Misc) |
| `EquippablePath.EnergyDrink` | Energy drink (ingredient) |

### Graffiti

| Value | Item |
|---|---|
| `EquippablePath.SprayPaint` | Spray paint can |

### Custom paths

```csharp
// For mod-bundled equippables
.WithItem(EquippablePath.Custom("MyMod/Items/Crowbar_AvatarEquippable"))

// Strings are also accepted directly (implicit conversion)
.WithItem("MyMod/Items/Crowbar_AvatarEquippable")
```

---

## Complete Example

The following NPC exercises all four arrive behaviours with varied parameters across a full day schedule.

```csharp
using S1API.Entities;
using S1API.Entities.Equippables;
using S1API.Entities.Schedule;
using S1API.Map;
using S1API.Map.Buildings;
using UnityEngine;

public sealed class MyScheduledNPC : NPC
{
    public override bool IsPhysical => true;

    protected override void ConfigurePrefab(NPCPrefabBuilder builder)
    {
        var northApartments = Building.Get<NorthApartments>();
        Vector3 spawnPos    = new(-53.57f, 1.065f, 67.8f);
        Vector3 smokeSpot   = new(-28.06f, 1.065f, 62.07f);
        Vector3 phoneSpot   = new(-35f,    1.065f, 58f);
        Vector3 coffeeSpot  = new(-42f,    1.065f, 65f);
        Vector3 graffitiSpot = new(-50f,   1.065f, 55f);
        Vector3 nightSpot   = new(-55f,    1.065f, 70f);
        Vector3 barSpot     = new(-60f,    1.065f, 62f);

        builder
            .WithIdentity("my_scheduled_npc", "Sam", "Actions")
            .WithSpawnPosition(spawnPos)
            // Declare which behaviours this NPC can perform
            .EnsureSmokeBreak()
            .EnsureGraffiti()
            .EnsureDrinking()
            .EnsureItemHolding()
            .WithSchedule(plan =>
            {
                plan.EnsureDealSignal()

                    // 08:00 — smoke break for 15 min (no extra params, nearest surface auto-selected)
                    .LocationBased(smokeSpot, 8 * 60, 15)
                        .Within(1.5f)
                        .Named("MorningSmoke")
                        .OnArriveSmokeBreak()

                    // 09:30 — hold phone for 20 min
                    .LocationBased(phoneSpot, 9 * 60 + 30, 20)
                        .WithItem(EquippablePath.Phone_Lowered)
                        .OnArriveHoldItem()

                    // 11:00 — drink coffee for 15 min
                    .LocationBased(coffeeSpot, 11 * 60, 15)
                        .WithDrink(EquippablePath.Coffee)
                        .OnArriveDrinking()

                    // 12:00 — lunch inside a building
                    .StayInBuilding(northApartments, 12 * 60, 60)

                    // 14:00 — graffiti: pick an available surface in Northtown
                    .LocationBased(graffitiSpot, 14 * 60, 45)
                        .WithSpraySurfaceInRegion(Region.Northtown)
                        .Named("AfternoonTag")
                        .OnArriveGraffiti()

                    // 16:00 — hold flashlight for 25 min
                    .LocationBased(nightSpot, 16 * 60, 25)
                        .WithItem(EquippablePath.Flashlight)
                        .OnArriveHoldItem()

                    // 18:00 — drink beer for 30 min
                    .LocationBased(barSpot, 18 * 60, 30)
                        .WithDrink(EquippablePath.Beer)
                        .OnArriveDrinking();
            });
    }

    protected override void OnCreated()
    {
        base.OnCreated();
        Appearance.Build();
        Region = Region.Northtown;
        Schedule.Enable();
    }
}
```

---

## Best Practices

### Always declare components before the schedule

`Ensure*` methods must be called **before** `.WithSchedule(...)`. The components are added to the NPC prefab at configuration time; the schedule actions reference them at runtime.

```csharp
// Correct
builder.EnsureItemHolding().WithSchedule(plan => { ... });

// Incorrect — HoldItem component not present when schedule runs
builder.WithSchedule(plan => { ... }).EnsureItemHolding();
```

### Use `EquippablePath` instead of raw strings

`EquippablePath` provides IDE autocomplete and compile-time safety. Typos in raw strings only appear as silent failures at runtime.

```csharp
// Preferred
.WithItem(EquippablePath.Flashlight)

// Works, but no IDE assistance
.WithItem("Tools/Flashlight/Flashlight_AvatarEquippable")
```

### Graffiti surfaces are consumed

Once a `WorldSpraySurface` is fully painted, `CanBeEdited` returns `false` and it is no longer eligible for selection. If no surface is available (all painted, too close to players, etc.) the behaviour silently skips. Use `WithSpraySurfaceInRegion` rather than a hard-coded GUID to give the action the most options.

### Slot-level drink/item overrides the prefab default

`EnsureDrinking(EquippablePath.Beer)` sets the prefab default. `.WithDrink(EquippablePath.Coffee)` on a specific slot overrides it **only for that action**. Subsequent actions without `.WithDrink()` revert to the prefab default.

### Duration vs. action overlap

Make sure action durations do not overlap with the next scheduled action's start time:

```csharp
// Smoke at 08:00 for 15 min ends at 08:15
// Phone at 09:30 starts at 09:30 — safe gap of 1h15m
.LocationBased(smokeSpot, 8 * 60, 15).OnArriveSmokeBreak()
.LocationBased(phoneSpot, 9 * 60 + 30, 20).OnArriveHoldItem()
```

---

## See Also

- **[Scheduling System](scheduling-system.md)** — Full schedule reference (WalkTo, StayInBuilding, etc.)
- **[Prefab Configuration](prefab-configuration.md)** — All `NPCPrefabBuilder` methods
- **[Graffiti System](../Graffiti/)** — `GraffitiManager` and `SpraySurface` API
- **`S1API.Entities.Equippables.EquippablePath`** — Full equippable path reference
- **`S1API.Entities.Equippables.Misc`** — Legacy string constants (still accepted)

# Scheduling System

The scheduling system allows NPCs to follow complex daily routines, including movement, building visits, and various activities.

## Table of Contents

1. [Overview](#overview)
2. [Schedule Configuration](#schedule-configuration)
3. [Available Schedule Actions](#available-schedule-actions)
4. [Action Specs](#action-specs)
5. [Runtime Schedule Management](#runtime-schedule-management)
6. [Schedule Examples](#schedule-examples)
7. [Best Practices](#best-practices)

## Overview

NPCs can follow complex schedules using the scheduling system. Schedules are defined in `ConfigurePrefab` and can include various actions:

```csharp
protected override void ConfigurePrefab(NPCPrefabBuilder builder)
{
    builder.WithSchedule(plan => {
        // Walk to location at 9:00 AM
        plan.WalkTo(new Vector3(10, 0, 10), 900, faceDestinationDir: true);
        
        // Stay in building from 10:00 AM for 60 minutes (preferred wrapper method)
        plan.StayInBuilding(Building.Get<Buildings.NorthApartments>(), 1000, 60);
        
        // Use vending machine at 2:00 PM
        plan.UseVendingMachine(1400);
        
        // Ensure deal signal exists
        plan.EnsureDealSignal();
    });
}
```

## Schedule Configuration

### Basic Schedule Setup

Schedules are configured in the `ConfigurePrefab` method using the `PrefabScheduleBuilder`:

```csharp
protected override void ConfigurePrefab(NPCPrefabBuilder builder)
{
    builder.WithSchedule(plan => {
        // Schedule actions here
        plan.WalkTo(new Vector3(0, 0, 0), 900);
        plan.EnsureDealSignal();
    });
}
```

### Time Format

All schedule times use 24-hour integer format:
- `900` = 9:00 AM
- `1400` = 2:00 PM
- `1800` = 6:00 PM
- `2359` = 11:59 PM

## Available Schedule Actions

The scheduling system provides both convenient wrapper methods and the flexible `.Add()` method for custom specifications. **Prefer the wrapper methods for common actions** as they provide better type safety and cleaner code.

### WalkTo

Move to a specific location at a given time:

```csharp
plan.WalkTo(destination, startTime, faceDestinationDir, within, warpIfSkipped, name);
```

**Parameters:**
- `destination`: Vector3 world position
- `startTime`: Time to start walking (24h format)
- `faceDestinationDir`: Whether to face the destination (default: true)
- `within`: Distance threshold for arrival (default: 1f)
- `warpIfSkipped`: Whether to warp if time is missed (default: false)
- `name`: Optional name for the action

**Example:**
```csharp
plan.WalkTo(new Vector3(-28.060f, 1.065f, 62.070f), 900, faceDestinationDir: true);
```

### StayInBuilding

Keep the NPC inside a building for a specified duration (preferred wrapper method):

```csharp
plan.StayInBuilding(building, startTime, durationMinutes, doorIndex, name);
```

**Parameters:**
- `building`: Building wrapper object (use `Building.Get<T>()` or `Building.GetByName()`)
- `startTime`: Time to enter the building (24h format)
- `durationMinutes`: How long to stay (default: 60)
- `doorIndex`: Which door to use (optional)
- `name`: Optional name for the action

**Examples:**
```csharp
// Using strongly-typed building identifier (preferred)
plan.StayInBuilding(Building.Get<Buildings.NorthApartments>(), 900, 480);

// Using name-based lookup
plan.StayInBuilding(Building.GetByName("North apartments"), 900, 480);
```

### DriveToCarPark

Drive a vehicle to a parking lot and park it (preferred wrapper method):

```csharp
plan.DriveToCarPark(parkingLot, vehicle, startTime, alignment, overrideParkingType, name);
```

**Parameters:**
- `parkingLot`: ParkingLotWrapper object
- `vehicle`: LandVehicle wrapper object
- `startTime`: Time to start driving (24h format)
- `alignment`: Parking alignment preference (optional)
- `overrideParkingType`: Whether to override parking type (optional)
- `name`: Optional name for the action

**Example:**
```csharp
var parkingLot = ParkingLots.GetByGUID("parking-lot-guid");
var vehicle = VehicleRegistry.GetByGUID("vehicle-guid");
plan.DriveToCarPark(parkingLot, vehicle, 1700, ParkingAlignment.FrontToKerb);
```

### UseVendingMachine

Use a vending machine:

```csharp
plan.UseVendingMachine(startTime, machineGUID, name);
```

**Parameters:**
- `startTime`: Time to use the machine (24h format)
- `machineGUID`: GUID of specific machine (optional)
- `name`: Optional name for the action

**Example:**
```csharp
plan.UseVendingMachine(1400, "vending-machine-guid", "BuySnack");
```

### UseATM

Use an ATM:

```csharp
plan.UseATM(startTime, atmGUID, name);
```

**Parameters:**
- `startTime`: Time to use the ATM (24h format)
- `atmGUID`: GUID of specific ATM (optional)
- `name`: Optional name for the action

### LocationDialogue

Move to a location and enable dialogue:

```csharp
plan.LocationDialogue(destination, startTime, faceDestinationDir, within, warpIfSkipped, greetingOverrideToEnable, choiceToEnable, name);
```

**Parameters:**
- `destination`: Vector3 location for dialogue
- `startTime`: Time to start the dialogue (24h format)
- `faceDestinationDir`: Whether to face the destination (default: true)
- `within`: Distance threshold for arrival (default: 1f)
- `warpIfSkipped`: Whether to warp if time is missed (default: false)
- `greetingOverrideToEnable`: Greeting override ID to enable (default: -1)
- `choiceToEnable`: Choice ID to enable (default: -1)
- `name`: Optional name for the action

### HandleDeal

Handle deals for dealer-type NPCs:

```csharp
plan.HandleDeal(startTime, name);
```

**Parameters:**
- `startTime`: Time to start handling deals (24h format)
- `name`: Optional name for the action

### EnsureDealSignal

Enable customer deal waiting behavior:

```csharp
plan.EnsureDealSignal();
```

**Purpose:**
- Allows the NPC to wait for deals
- Enables customer behavior
- Required for customer NPCs

### LocationBased

Walk to a destination and perform an activity on arrival (smoke break, graffiti, drinking, or item holding). This is a **sub-builder** — chain modifier methods then close with an `OnArrive*()` terminal:

```csharp
plan.LocationBased(destination, startTime, durationMinutes)
    .Within(1.5f)
    .Named("MorningSmoke")
    .OnArriveSmokeBreak();
```

**Terminals:**

| Terminal | Behaviour |
|---|---|
| `.OnArriveSmokeBreak()` | NPC smokes a cigarette |
| `.OnArriveGraffiti()` | NPC spray-paints a surface |
| `.OnArriveDrinking()` | NPC drinks a beverage |
| `.OnArriveHoldItem()` | NPC holds an equippable item |
| `.OnArriveNone()` | NPC stands idle |

**Behaviour-specific parameters (chain before the terminal):**

```csharp
// Graffiti — pick a surface in a region
.WithSpraySurfaceInRegion(Region.Northtown)

// Graffiti — target a specific surface
.WithSpraySurface(mySurface)           // SpraySurface object
.WithSpraySurface(new Guid("..."))     // by GUID

// Drinking — override the drink equippable for this slot
.WithDrink(EquippablePath.Coffee)

// HoldItem — override the held item for this slot
.WithItem(EquippablePath.Phone_Lowered)
```

**Required prefab setup** — call the matching `Ensure*` method in `ConfigurePrefab`:

```csharp
builder.EnsureSmokeBreak()
       .EnsureGraffiti()
       .EnsureDrinking()
       .EnsureItemHolding();
```

> For the full reference, including the `EquippablePath` constant table and a complete day schedule example, see **[Location-Based Actions](location-based-actions.md)**.

### SitAtSeatSet

Seat the NPC at a configured seating area. The NPC will walk to the seat and sit down for the specified duration.

```csharp
// By name — finds the first AvatarSeatSet with this GameObject name
plan.SitAtSeatSet("Fast Food Booth", 900, durationMinutes: 60);

// By path — use when multiple seat sets share the same name
plan.SitAtSeatSet(null, 1650, durationMinutes: 130,
    seatSetPath: "Map/Hyland Point/Region_Docks/WaterFront/OutdoorBench (1)");
```

**Parameters:**
- `seatSetName`: GameObject name of the `AvatarSeatSet`. Can be `null` if `seatSetPath` is provided.
- `startTime`: Time to begin the seating action (24h HHMM format)
- `durationMinutes`: How long the NPC sits in minutes (default: 60). **Must be positive** — with duration 0 the action will never trigger.
- `warpIfSkipped`: Whether to warp the NPC to the seat if the action is skipped (default: false)
- `name`: Optional action name; defaults to "Sit"
- `seatSetPath`: Optional full hierarchy path to the seat set GameObject (e.g. `"Map/Hyland Point/Region_Docks/WaterFront/OutdoorBench (1)"`). Use this when multiple seat sets share the same name.

**Important notes:**
- Name lookups are case-insensitive and return the first match. Use `seatSetPath` when you need a specific seat set among duplicates.
- If the seat set cannot be resolved, the action is automatically disabled and a warning is logged. This prevents the NPC's schedule from breaking permanently.
- The NPC must be able to reach the seat — ensure a preceding action (e.g. `LocationBased`) gets the NPC close enough before the sit starts, or give enough travel time in the duration of the previous action.
- For complex lookups (direct object reference, custom search settings), create a `SitSpec` manually and add it via `plan.Add`

## Action Specs

Action specs provide more complex behaviors than basic schedule actions. **Use `.Add()` with specs only when you need advanced configuration** that the wrapper methods don't support.

### When to Use `.Add()` vs Wrapper Methods

**Prefer wrapper methods for:**
- Simple building stays: `plan.StayInBuilding(Building.Get<T>(), 900, 480)`
- Vehicle parking: `plan.DriveToCarPark(parkingLot, vehicle, 1700)`
- Basic vending machine usage: `plan.UseVendingMachine(1400)`

**Use `.Add()` with specs for:**
- Complex seat set lookups with paths or direct references
- Advanced parking configurations with custom alignment overrides
- Building stays with specific door index requirements
- Custom action specifications not covered by wrapper methods

### StayInBuildingSpec

Remain inside a building for a duration (use wrapper method when possible):

```csharp
// Preferred: Use wrapper method
plan.StayInBuilding(Building.Get<Buildings.NorthApartments>(), 1000, 60);

// Advanced: Use spec for complex requirements
plan.Add(new StayInBuildingSpec { 
    BuildingName = "North apartments", 
    StartTime = 1000, 
    DurationMinutes = 60,
    DoorIndex = 0,
    Name = "WorkShift"
});
```

**Properties:**
- `BuildingName`: Name of the building to stay in
- `StartTime`: Time to enter the building
- `DurationMinutes`: How long to stay (default: 60)
- `DoorIndex`: Which door to use (optional)
- `Name`: Optional name for the action
- Use the building registry (`docs/building-registry.md`) to discover building names and resolve door indices
- Prefer strongly-typed identifiers: `plan.StayInBuilding(Building.Get<Buildings.NorthApartments>(), 900);`

### UseVendingMachineSpec

Use a vending machine (use wrapper method when possible):

```csharp
// Preferred: Use wrapper method
plan.UseVendingMachine(1400, "vending-machine-guid", "BuySnack");

// Advanced: Use spec for complex requirements
plan.Add(new UseVendingMachineSpec { 
    StartTime = 1400,
    MachineGUID = "vending-machine-guid",
    Name = "BuySnack"
});
```

**Properties:**
- `StartTime`: Time to use the machine
- `MachineGUID`: GUID of specific machine (optional)
- `Name`: Optional name for the action

**Notes:**
- If no MachineGUID is provided, uses the nearest machine
- MachineGUID must be a valid vending machine GUID

### LocationDialogueSpec

Trigger dialogue at a specific location (use wrapper method when possible):

```csharp
// Preferred: Use wrapper method
plan.LocationDialogue(new Vector3(20, 0, 20), 1900, true, 1f, false, 1, 2, "MeetPlayer");

// Advanced: Use spec for complex requirements
plan.Add(new LocationDialogueSpec {
    Destination = new Vector3(20, 0, 20),
    StartTime = 1900,
    FaceDestinationDirection = true,
    Within = 1f,
    WarpIfSkipped = false,
    GreetingOverrideToEnable = 1,
    ChoiceToEnable = 2,
    Name = "MeetPlayer"
});
```

**Properties:**
- `Destination`: Vector3 location for dialogue
- `StartTime`: Time to start the dialogue
- `FaceDestinationDirection`: Whether to face the destination
- `Within`: Distance threshold for arrival
- `WarpIfSkipped`: Whether to warp if time is missed
- `GreetingOverrideToEnable`: Greeting override ID to enable
- `ChoiceToEnable`: Choice ID to enable
- `Name`: Optional name for the action

### SitSpec

Seat an NPC using an existing `AvatarSeatSet`. Use the spec directly when you need advanced lookup options beyond what `SitAtSeatSet()` provides.

```csharp
// By name
plan.Add(new SitSpec {
    StartTime = 900,
    DurationMinutes = 60,
    SeatSetName = "Fast Food Booth",
    Name = "Breakfast"
});

// By path (for ambiguous names)
plan.Add(new SitSpec {
    StartTime = 1650,
    DurationMinutes = 130,
    SeatSetPath = "Map/Hyland Point/Region_Docks/WaterFront/OutdoorBench (1)",
    Name = "WatchSunset"
});

// By direct reference
plan.Add(new SitSpec {
    StartTime = 900,
    DurationMinutes = 60,
    SeatSetReference = mySeatSetGameObject,
    Name = "MorningCoffee"
});
```

**Properties:**
- `StartTime`: Time to begin the action (24h HHMM format)
- `DurationMinutes`: How long the NPC sits in minutes. **Must be positive** — with duration 0 the action has a zero-width time range and will never be matched by the schedule manager. Defaults to 60 if not set.
- `SeatSetName`: GameObject name of the target `AvatarSeatSet` (case-insensitive, returns first match)
- `SeatSetPath`: Full hierarchy path to the seat set (e.g. `"Map/Hyland Point/Region_Docks/WaterFront/OutdoorBench (1)"`). Use when multiple seat sets share the same name. Supports suffix matching.
- `SeatSetReference`: Direct Unity object reference — can be an `AvatarSeatSet`, `GameObject`, or any `Component` in the seat set hierarchy
- `WarpIfSkipped`: Whether to warp the NPC directly to the seat when the action is skipped
- `Name`: Optional custom name for the action
- `IncludeInactiveSearch`: Include inactive seat sets during lookup (default: true)

**Resolution order:** Direct reference → path lookup → name lookup → NPC child search.

**Failure handling:** If no seat set can be resolved, the action is disabled and a warning is logged. This prevents `NPCEvent_Sit.Started()` from throwing a NullReferenceException which would permanently break the NPC's schedule.

### DriveToCarParkSpec

Drive to a car park and park a vehicle (use wrapper method when possible):

```csharp
// Preferred: Use wrapper method with wrapper objects
var parkingLot = ParkingLots.GetByGUID("parking-lot-guid");
var vehicle = VehicleRegistry.GetByGUID("vehicle-guid");
plan.DriveToCarPark(parkingLot, vehicle, 1700, ParkingAlignment.FrontToKerb, true, "ParkCar");

// Advanced: Use spec for complex requirements
plan.Add(new DriveToCarParkSpec {
    StartTime = 1700,
    ParkingLotGUID = "parking-lot-guid",
    VehicleGUID = "vehicle-guid",
    OverrideParkingType = true,
    ParkingType = 0,
    Alignment = ParkingAlignment.FrontToKerb,
    Name = "ParkCar"
});
```

**Properties:**
- `StartTime`: Time to start driving
- `ParkingLotGUID`: GUID of the parking lot
- `VehicleGUID`: GUID of the vehicle to park
- `OverrideParkingType`: Whether to override parking type
- `ParkingType`: Parking type override
- `Alignment`: Parking alignment preference
- `Name`: Optional name for the action

**Parking Alignments:**
- `ParkingAlignment.FrontToKerb`: Front of car faces kerb
- `ParkingAlignment.RearToKerb`: Rear of car faces kerb

## Runtime Schedule Management

### Enabling the Schedule

Enable the schedule system in `OnCreated`:

```csharp
protected override void OnCreated()
{
    base.OnCreated();
    
    // Enable schedule system
    Schedule.Enable();
    Schedule.InitializeActions();
}
```

### Schedule Control

Basic schedule control methods:

```csharp
// Check active action
string activeAction = Schedule.GetActiveActionName();

// Disable schedule
Schedule.Disable();

// Enable curfew mode
Schedule.SetCurfewMode(true);
```

### Schedule Events

Subscribe to schedule events:

```csharp
protected override void OnCreated()
{
    base.OnCreated();
    
    // Subscribe to schedule events
    Schedule.OnActionStarted += (actionName) => {
        Debug.Log($"Started action: {actionName}");
    };
    
    Schedule.OnActionCompleted += (actionName) => {
        Debug.Log($"Completed action: {actionName}");
    };
}
```

## Schedule Examples

### Basic Daily Routine

```csharp
protected override void ConfigurePrefab(NPCPrefabBuilder builder)
{
    Vector3 homePos = new Vector3(-53.5701f, 1.065f, 67.7955f);
    Vector3 workPos = new Vector3(-28.060f, 1.065f, 62.070f);
    
    builder.WithSchedule(plan => {
        // Morning routine
        plan.WalkTo(workPos, 800); // 8:00 AM - Go to work
        plan.StayInBuilding(Building.Get<Buildings.NorthApartments>(), 900, 480); // 8 hours
        
        // Evening routine
        plan.WalkTo(homePos, 1800); // 6:00 PM - Go home
        plan.EnsureDealSignal(); // Enable customer behavior
    });
}
```

### Complex Schedule with Multiple Activities

```csharp
protected override void ConfigurePrefab(NPCPrefabBuilder builder)
{
    Vector3 homePos = new Vector3(-53.5701f, 1.065f, 67.7955f);
    Vector3 workPos = new Vector3(-28.060f, 1.065f, 62.070f);
    Vector3 parkPos = new Vector3(-40.0f, 1.065f, 50.0f);
    var northApts = Building.Get<Buildings.NorthApartments>();
    
    builder.WithSchedule(plan => {
        // Morning
        plan.WalkTo(workPos, 800);
        plan.StayInBuilding(northApts, 900, 240); // 4 hours
        
        // Lunch break
        plan.UseVendingMachine(1300, null, "LunchBreak");
        
        // Afternoon work
        plan.StayInBuilding(northApts, 1400, 240); // 4 hours
        
        // Evening activities
        plan.WalkTo(parkPos, 1800);
        plan.LocationDialogue(parkPos, 1900, true, 1f, false, 1, -1, "ParkMeeting");
        
        // Go home
        plan.WalkTo(homePos, 2100);
        plan.EnsureDealSignal();
    });
}
```

### Customer-Focused Schedule

```csharp
protected override void ConfigurePrefab(NPCPrefabBuilder builder)
{
    Vector3 homePos = new Vector3(-53.5701f, 1.065f, 67.7955f);
    Vector3 shopPos = new Vector3(-28.060f, 1.065f, 62.070f);
    var northApts = Building.Get<Buildings.NorthApartments>();
    
    builder.WithSchedule(plan => {
        // Enable customer behavior
        plan.EnsureDealSignal();
        
        // Morning shopping
        plan.WalkTo(shopPos, 900);
        plan.StayInBuilding(northApts, 1000, 60); // 1 hour shopping
        
        // Afternoon activities
        plan.WalkTo(homePos, 1400);
        plan.StayInBuilding(northApts, 1500, 180); // 3 hours at home
        
        // Evening deal opportunity
        plan.LocationDialogue(shopPos, 1900, true, 1f, false, 1, -1, "EveningDeal");
    });
}
```

## Best Practices

### Do's

- **Configure schedules in `ConfigurePrefab`** - required for save/load compatibility
- **Prefer wrapper methods** over `.Add()` for common actions (better type safety and cleaner code)
- **Use strongly-typed building identifiers** like `Building.Get<Buildings.NorthApartments>()`
- **Use wrapper objects** when available (see `docs/building-registry.md`)
- **Use meaningful action names** for debugging and events
- **Test schedule timing** to ensure actions don't overlap
- **Use appropriate durations** for building stays
- **Enable schedule system** in `OnCreated`

### Don'ts

- **Don't modify schedules at runtime** (except through proper APIs)
- **Don't use invalid building names** or GUIDs
- **Don't create overlapping actions** at the same time
- **Don't forget to call `Schedule.Enable()`** and `Schedule.InitializeActions()`
- **Don't use `.Add()` with specs when wrapper methods are available** - reserve for complex scenarios

### Error Handling

Wrap schedule configuration in try-catch blocks:

```csharp
protected override void ConfigurePrefab(NPCPrefabBuilder builder)
{
    try
    {
        builder.WithSchedule(plan => {
            // Schedule configuration using wrapper methods
            plan.WalkTo(new Vector3(0, 0, 0), 900);
            plan.StayInBuilding(Building.Get<Buildings.NorthApartments>(), 1000, 60);
            plan.EnsureDealSignal();
        });
    }
    catch (Exception ex)
    {
        MelonLogger.Error($"Failed to configure schedule for {GetType().Name}: {ex.Message}");
    }
}
```

### Performance Considerations

- **Keep schedules simple** - complex schedules can impact performance
- **Use appropriate action durations** - don't make actions too short or long
- **Test with multiple NPCs** - ensure schedules work well together
- **Monitor schedule performance** in multiplayer environments

## Complete Schedule Examples

For complete schedules (including vending machines, buildings, spec objects, and dealer-ready plans), see the **[S1API NPC Example Repository](https://github.com/ifBars/S1APINPCExample)**:

- **[ExamplePhysicalNPC1](https://github.com/ifBars/S1APINPCExample/blob/master/NPCs/ExamplePhysicalNPC1.cs)** (fluent helpers + vehicles)
- **[ExamplePhysicalNPC2](https://github.com/ifBars/S1APINPCExample/blob/master/NPCs/ExamplePhysicalNPC2.cs)** (`Add(...)` specs)
- **[ExamplePhysicalDealerNPC](https://github.com/ifBars/S1APINPCExample/blob/master/NPCs/ExamplePhysicalDealerNPC.cs)** (dealer schedule requirements)

## Next Steps

Now that you understand the scheduling system, explore:

- **[Location-Based Actions](location-based-actions.md)** - Full reference for SmokeBreak, Graffiti, Drinking, and HoldItem actions with `EquippablePath`
- **[Dialogue System](dialogue-system.md)** - Interactive conversations
- **[Customer Behavior](customer-behavior.md)** - Customer system details
- **[Runtime Management](runtime-management.md)** - NPC lifecycle and properties

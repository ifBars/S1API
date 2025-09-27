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
        
        // Stay in building from 10:00 AM for 60 minutes
        plan.Add(new StayInBuildingSpec { 
            BuildingName = "North apartments", 
            StartTime = 1000, 
            DurationMinutes = 60 
        });
        
        // Use vending machine at 2:00 PM
        plan.Add(new UseVendingMachineSpec { 
            StartTime = 1400,
            MachineGUID = "vending-machine-guid"
        });
        
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

### EnsureDealSignal

Enable customer deal waiting behavior:

```csharp
plan.EnsureDealSignal();
```

**Purpose:**
- Allows the NPC to wait for deals
- Enables customer behavior
- Required for customer NPCs

## Action Specs

Action specs provide more complex behaviors than basic schedule actions.

### StayInBuildingSpec

Remain inside a building for a duration:

```csharp
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

### UseVendingMachineSpec

Use a vending machine:

```csharp
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

Trigger dialogue at a specific location:

```csharp
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

### DriveToCarParkSpec

Drive to a car park and park a vehicle:

```csharp
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
        plan.Add(new StayInBuildingSpec { 
            BuildingName = "North apartments", 
            StartTime = 900, 
            DurationMinutes = 480 // 8 hours
        });
        
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
    
    builder.WithSchedule(plan => {
        // Morning
        plan.WalkTo(workPos, 800);
        plan.Add(new StayInBuildingSpec { 
            BuildingName = "North apartments", 
            StartTime = 900, 
            DurationMinutes = 240 // 4 hours
        });
        
        // Lunch break
        plan.Add(new UseVendingMachineSpec { 
            StartTime = 1300,
            Name = "LunchBreak"
        });
        
        // Afternoon work
        plan.Add(new StayInBuildingSpec { 
            BuildingName = "North apartments", 
            StartTime = 1400, 
            DurationMinutes = 240 // 4 hours
        });
        
        // Evening activities
        plan.WalkTo(parkPos, 1800);
        plan.Add(new LocationDialogueSpec {
            Destination = parkPos,
            StartTime = 1900,
            FaceDestinationDirection = true,
            GreetingOverrideToEnable = 1,
            Name = "ParkMeeting"
        });
        
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
    
    builder.WithSchedule(plan => {
        // Enable customer behavior
        plan.EnsureDealSignal();
        
        // Morning shopping
        plan.WalkTo(shopPos, 900);
        plan.Add(new StayInBuildingSpec { 
            BuildingName = "North apartments", 
            StartTime = 1000, 
            DurationMinutes = 60 // 1 hour shopping
        });
        
        // Afternoon activities
        plan.WalkTo(homePos, 1400);
        plan.Add(new StayInBuildingSpec { 
            BuildingName = "North apartments", 
            StartTime = 1500, 
            DurationMinutes = 180 // 3 hours at home
        });
        
        // Evening deal opportunity
        plan.Add(new LocationDialogueSpec {
            Destination = shopPos,
            StartTime = 1900,
            FaceDestinationDirection = true,
            GreetingOverrideToEnable = 1,
            Name = "EveningDeal"
        });
    });
}
```

## Best Practices

### Do's

- **Configure schedules in `ConfigurePrefab`** - required for save/load compatibility
- **Use meaningful action names** for debugging and events
- **Test schedule timing** to ensure actions don't overlap
- **Use appropriate durations** for building stays
- **Enable schedule system** in `OnCreated`

### Don'ts

- **Don't modify schedules at runtime** (except through proper APIs)
- **Don't use invalid building names** or GUIDs
- **Don't create overlapping actions** at the same time
- **Don't forget to call `Schedule.Enable()`** and `Schedule.InitializeActions()`

### Error Handling

Wrap schedule configuration in try-catch blocks:

```csharp
protected override void ConfigurePrefab(NPCPrefabBuilder builder)
{
    try
    {
        builder.WithSchedule(plan => {
            // Schedule configuration
            plan.WalkTo(new Vector3(0, 0, 0), 900);
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

## Next Steps

Now that you understand the scheduling system, explore:

- **[Dialogue System](dialogue-system.md)** - Interactive conversations
- **[Customer Behavior](customer-behavior.md)** - Customer system details
- **[Runtime Management](runtime-management.md)** - NPC lifecycle and properties

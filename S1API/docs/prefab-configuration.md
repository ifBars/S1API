# Prefab Configuration

The `ConfigurePrefab` method is where you set up your NPC's components and default behavior before the NPC is spawned. This is crucial for proper save/load behavior and network compatibility.

## Table of Contents

1. [Overview](#overview)
2. [NPCPrefabBuilder Methods](#npcprefabbuilder-methods)
3. [Spawn Position Configuration](#spawn-position-configuration)
4. [Customer Configuration](#customer-configuration)
5. [Relationship Configuration](#relationship-configuration)
6. [Schedule Configuration](#schedule-configuration)
7. [Configuration Workflow](#configuration-workflow)
8. [Best Practices](#best-practices)

## Overview

The `ConfigurePrefab` method is called during NPC prefab creation and allows you to:

- Set spawn position and rotation
- Configure customer behavior defaults
- Set relationship parameters
- Define schedule actions
- Add required components

**Important**: Customer, relationship, and schedule configuration must be done in `ConfigurePrefab` to ensure proper save/load behavior and network compatibility.

## NPCPrefabBuilder Methods

### WithSpawnPosition

Sets the spawn position and rotation for the NPC.

```csharp
// Set position only (default rotation)
builder.WithSpawnPosition(new Vector3(0, 0, 0));

// Set position and rotation
builder.WithSpawnPosition(new Vector3(0, 0, 0), Quaternion.Euler(0, 90, 0));
```

**Parameters:**
- `position`: World position where the NPC will spawn
- `rotation`: Optional rotation (defaults to Quaternion.identity)

**Notes:**
- Applied every time the NPC is spawned (new games and loaded games)
- Use world coordinates
- Consider building entrances, roads, and safe spawn areas

### EnsureCustomer

Adds customer behavior component to the NPC.

```csharp
builder.EnsureCustomer();
```

**What it does:**
- Adds the `Customer` component to the NPC
- Enables customer behavior
- Required for `WithCustomerDefaults` to work

**Use when:**
- NPC should act as a business customer
- NPC should buy products from the player
- NPC should participate in the economy

### WithCustomerDefaults

Configures customer behavior using the `CustomerDataBuilder`.

```csharp
builder.WithCustomerDefaults(cd => {
    // Spending behavior
    cd.WithSpending(minWeekly: 150f, maxWeekly: 600f)
      .WithOrdersPerWeek(1, 4)
      .WithPreferredOrderDay(Day.Friday)
      .WithOrderTime(1100); // 11:00 AM
    
    // Customer standards and behavior
    cd.WithStandards(CustomerStandard.Low)
      .AllowDirectApproach(true)
      .GuaranteeFirstSample(true)
      .WithCallPoliceChance(0.15f);
    
    // Relationship requirements
    cd.WithMutualRelationRequirement(minAt50: 2.5f, maxAt100: 4.0f);
    
    // Addiction and dependence
    cd.WithDependence(baseAddiction: 0.1f, dependenceMultiplier: 1.1f);
    
    // Product preferences
    cd.WithAffinities(new[] {
        (DrugType.Marijuana, 0.45f),
        (DrugType.Cocaine, -0.2f)
    });
    
    // Property preferences
    cd.WithPreferredProperties(Property.Munchies, Property.Energizing);
});
```

**Available Methods:**
- `WithSpending(minWeekly, maxWeekly)`: Weekly spending range
- `WithOrdersPerWeek(min, max)`: Number of orders per week
- `WithPreferredOrderDay(day)`: Preferred day for orders
- `WithOrderTime(hhmm)`: Preferred time for orders (24h format)
- `WithStandards(standard)`: Customer quality standards
- `AllowDirectApproach(allow)`: Can be approached directly
- `GuaranteeFirstSample(guarantee)`: First sample always succeeds
- `WithMutualRelationRequirement(minAt50, maxAt100)`: Relationship requirements
- `WithCallPoliceChance(chance)`: Chance to call police (0-1)
- `WithDependence(baseAddiction, multiplier)`: Addiction mechanics
- `WithAffinities(affinities)`: Product type preferences
- `WithPreferredProperties(properties)`: Property preferences

### WithRelationshipDefaults

Configures relationship parameters using the `NPCRelationshipDataBuilder`.

```csharp
builder.WithRelationshipDefaults(r => {
    // Starting relationship level (0-5)
    r.WithDelta(1.5f);
    
    // Unlock settings
    r.SetUnlocked(false)
     .SetUnlockType(NPCRelationship.UnlockType.DirectApproach);
    
    // Connection to other NPCs
    r.WithConnectionsById("kyle_cooley", "ludwig_meyer", "austin_steiner");
});
```

**Available Methods:**
- `WithDelta(delta)`: Starting relationship level (0-5)
- `WithNormalized(normalized)`: Relationship level as 0-1 value
- `SetUnlocked(unlocked)`: Whether NPC is initially unlocked
- `SetUnlockType(type)`: How the NPC can be unlocked
- `WithConnectionsById(ids)`: Connect to other NPCs by ID
- `WithConnections(npcs)`: Connect to other NPCs by reference

### WithSchedule

Defines the NPC's schedule using the `PrefabScheduleBuilder`.

```csharp
builder.WithSchedule(plan => {
    // Basic actions
    plan.EnsureDealSignal();
    plan.WalkTo(new Vector3(10, 0, 10), 900);
    
    // Custom action specs
    plan.Add(new StayInBuildingSpec { 
        BuildingName = "North apartments", 
        StartTime = 1000, 
        DurationMinutes = 60 
    });
    
    plan.Add(new UseVendingMachineSpec { 
        StartTime = 1400,
        MachineGUID = "vending-machine-guid"
    });
    
    plan.Add(new LocationDialogueSpec {
        Destination = new Vector3(20, 0, 20),
        StartTime = 1900,
        FaceDestinationDirection = true,
        GreetingOverrideToEnable = 1
    });
});
```

**Available Methods:**
- `WalkTo(destination, startTime, ...)`: Move to location
- `EnsureDealSignal()`: Enable customer deal waiting
- `Add(spec)`: Add custom action spec

## Spawn Position Configuration

### Choosing Spawn Positions

Consider these factors when choosing spawn positions:

```csharp
// Good: Near building entrance
Vector3 goodSpawn = new Vector3(-28.060f, 1.065f, 62.070f);

// Bad: Inside building or underground
Vector3 badSpawn = new Vector3(-28.060f, -5.0f, 62.070f);

// Good: On road or walkable surface
Vector3 roadSpawn = new Vector3(-53.5701f, 1.065f, 67.7955f);
```

**Best Practices:**
- Use world coordinates from the game
- Ensure the position is on a walkable surface
- Avoid spawning inside buildings or underground
- Consider proximity to buildings, roads, and other NPCs
- Test spawn positions in-game

### Dynamic Spawn Positions

You can calculate spawn positions dynamically:

```csharp
protected override void ConfigurePrefab(NPCPrefabBuilder builder)
{
    // Get a random building
    var buildings = Buildings.GetAll();
    var randomBuilding = buildings[UnityEngine.Random.Range(0, buildings.Count)];
    var buildingPos = randomBuilding.Position;
    
    // Spawn near the building
    var spawnPos = buildingPos + new Vector3(5, 0, 5);
    
    builder.WithSpawnPosition(spawnPos);
}
```

## Customer Configuration

### Spending Behavior

Configure how much the customer spends:

```csharp
cd.WithSpending(minWeekly: 100f, maxWeekly: 500f)
  .WithOrdersPerWeek(1, 3)
  .WithPreferredOrderDay(Day.Friday)
  .WithOrderTime(1400); // 2:00 PM
```

**Spending Ranges:**
- **Low**: 50-200 per week
- **Medium**: 200-500 per week
- **High**: 500-1000+ per week

### Customer Standards

Set quality expectations:

```csharp
cd.WithStandards(CustomerStandard.Low)      // Accepts low quality
  .WithStandards(CustomerStandard.Medium)   // Expects decent quality
  .WithStandards(CustomerStandard.High);    // Demands high quality
```

### Product Preferences

Define what products the customer likes:

```csharp
cd.WithAffinities(new[] {
    (DrugType.Marijuana, 0.6f),    // Likes marijuana
    (DrugType.Cocaine, 0.3f),      // Somewhat likes cocaine
    (DrugType.Heroin, -0.5f)       // Dislikes heroin
});
```

**Affinity Values:**
- `1.0f`: Loves this product type
- `0.5f`: Likes this product type
- `0.0f`: Neutral
- `-0.5f`: Dislikes this product type
- `-1.0f`: Hates this product type

### Property Preferences

Set preferred product properties:

```csharp
cd.WithPreferredProperties(Property.Munchies, Property.Energizing, Property.Cyclopean);
```

## Relationship Configuration

### Starting Relationship

Set the initial relationship level:

```csharp
r.WithDelta(0.0f);    // Stranger
r.WithDelta(1.0f);    // Acquaintance
r.WithDelta(2.5f);    // Friend
r.WithDelta(4.0f);    // Good friend
r.WithDelta(5.0f);    // Best friend
```

### Unlock Settings

Configure how the NPC can be unlocked:

```csharp
r.SetUnlocked(false)  // Must be unlocked
  .SetUnlockType(NPCRelationship.UnlockType.DirectApproach);

r.SetUnlocked(true);  // Already unlocked
```

**Unlock Types:**
- `DirectApproach`: Can be unlocked by talking to them
- `Recommendation`: Must be recommended by another NPC

### Connections

Link NPCs together:

```csharp
// By ID
r.WithConnectionsById("kyle_cooley", "ludwig_meyer");

// By reference (if NPCs are available)
r.WithConnections(Get<KyleCooley>(), Get<LudwigMeyer>());
```

## Schedule Configuration

### Basic Schedule Actions

```csharp
plan.WalkTo(new Vector3(10, 0, 10), 900, faceDestinationDir: true);
plan.EnsureDealSignal();
```

### Custom Action Specs

Use action specs for complex behaviors:

```csharp
// Stay in building
plan.Add(new StayInBuildingSpec { 
    BuildingName = "North apartments", 
    StartTime = 1000, 
    DurationMinutes = 60,
    DoorIndex = 0
});

// Use vending machine
plan.Add(new UseVendingMachineSpec { 
    StartTime = 1400,
    MachineGUID = "vending-machine-guid"
});

// Location-based dialogue
plan.Add(new LocationDialogueSpec {
    Destination = new Vector3(20, 0, 20),
    StartTime = 1900,
    FaceDestinationDirection = true,
    GreetingOverrideToEnable = 1,
    ChoiceToEnable = 2
});

// Drive to car park
plan.Add(new DriveToCarParkSpec {
    StartTime = 1700,
    ParkingLotGUID = "parking-lot-guid",
    VehicleGUID = "vehicle-guid",
    Alignment = ParkingAlignment.FrontToKerb
});
```

## Configuration Workflow

### Step-by-Step Process

1. **Set spawn position**
2. **Add customer component** (if needed)
3. **Configure customer defaults** (if customer)
4. **Set relationship defaults**
5. **Define schedule** (if physical NPC)

### Complete Example

```csharp
protected override void ConfigurePrefab(NPCPrefabBuilder builder)
{
    Vector3 shopPosition = new Vector3(-28.060f, 1.065f, 62.070f);
    Vector3 spawnPosition = new Vector3(-53.5701f, 1.065f, 67.7955f);
    
    builder.WithSpawnPosition(spawnPosition)
           .EnsureCustomer()
           .WithCustomerDefaults(cd => {
               cd.WithSpending(200f, 800f)
                 .WithOrdersPerWeek(2, 5)
                 .WithPreferredOrderDay(Day.Friday)
                 .WithOrderTime(1400)
                 .WithStandards(CustomerStandard.Medium)
                 .AllowDirectApproach(true)
                 .WithAffinities(new[] {
                     (DrugType.Marijuana, 0.6f),
                     (DrugType.Cocaine, 0.3f)
                 });
           })
           .WithRelationshipDefaults(r => {
               r.WithDelta(2.0f)
                .SetUnlocked(true)
                .SetUnlockType(NPCRelationship.UnlockType.DirectApproach);
           })
           .WithSchedule(plan => {
               plan.EnsureDealSignal();
               plan.WalkTo(shopPosition, 800);
               plan.Add(new StayInBuildingSpec { 
                   BuildingName = "North apartments", 
                   StartTime = 900, 
                   DurationMinutes = 480 
               });
               plan.WalkTo(spawnPosition, 1800);
           });
}
```

## Best Practices

### Do's

- **Always configure customer, relationship, and schedule data in `ConfigurePrefab`**
- **Use meaningful spawn positions** that make sense for the NPC's role
- **Test spawn positions in-game** to ensure they work properly
- **Use the builder pattern** for fluent configuration
- **Handle exceptions gracefully** in configuration code

### Don'ts

- **Don't modify customer, relationship, or schedule data at runtime** (except through proper APIs)
- **Don't spawn NPCs in inaccessible locations**
- **Don't use invalid GUIDs** for buildings, vehicles, or machines
- **Don't forget to call `EnsureCustomer()`** before `WithCustomerDefaults()`

### Error Handling

Wrap configuration code in try-catch blocks:

```csharp
protected override void ConfigurePrefab(NPCPrefabBuilder builder)
{
    try
    {
        // Configuration code here
        builder.WithSpawnPosition(spawnPos)
               .EnsureCustomer()
               .WithCustomerDefaults(cd => {
                   // Customer configuration
               });
    }
    catch (Exception ex)
    {
        MelonLogger.Error($"Failed to configure prefab for {GetType().Name}: {ex.Message}");
        // Fallback configuration or re-throw
    }
}
```

## Next Steps

Now that you understand prefab configuration, explore:

- **[Appearance Customization](appearance-customization.md)** - Visual customization
- **[Scheduling System](scheduling-system.md)** - Detailed schedule management
- **[Customer Behavior](customer-behavior.md)** - Customer system details
- **[Relationship Management](relationship-management.md)** - Relationship system

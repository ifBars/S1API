# Custom NPCs

The S1API provides a comprehensive system for creating custom NPCs that integrate seamlessly with the base game's systems. This guide covers creating physical NPCs with full functionality including schedules, dialogue, customer behavior, relationships, and appearance customization.

## Overview

Custom NPCs in S1API are built on a modular architecture that allows you to create both physical NPCs (visible in the world) and non-physical NPCs (contacts, informants, etc.). The system provides:

- **Physical NPCs**: Visible in the game world with 3D models, movement, and direct interaction
- **Non-Physical NPCs**: Invisible contacts for messaging and phone interactions
- **Modular Components**: Appearance, Dialogue, Schedule, Customer, and Relationship systems
- **Save/Load Integration**: Full persistence support with the game's save system
- **Network Compatibility**: Works in both single-player and multiplayer environments
- **Cross-branch Compatibility**: Works in both Mono and Il2Cpp builds

## Documentation Structure

The Custom NPC system is documented across multiple focused pages:

### Core Concepts
- **[Basic NPC Creation](basic-npc-creation.md)** - Fundamental concepts and getting started
- **[Prefab Configuration](prefab-configuration.md)** - Setting up NPC components and behavior
- **[Runtime Management](runtime-management.md)** - NPC lifecycle and runtime properties

### Systems & Features
- **[Appearance Customization](appearance-customization.md)** - Visual appearance and avatar system
- **[Scheduling System](scheduling-system.md)** - NPC schedules and movement patterns
- **[Dialogue System](dialogue-system.md)** - Interactive conversations and dialogue trees
- **[Customer Behavior](customer-behavior.md)** - NPCs as business customers
- **[Relationship Management](relationship-management.md)** - NPC relationships and connections

### API Reference
- **[API Reference](../api/S1API.html)** - Detailed API documentation

## Quick Start

Here's a minimal example to get you started:

```csharp
public sealed class MyFirstNPC : NPC
{
    protected override bool IsPhysical => true;
    
    protected override void ConfigurePrefab(NPCPrefabBuilder builder)
    {
        builder.WithSpawnPosition(new Vector3(0, 0, 0))
               .EnsureCustomer()
               .WithCustomerDefaults(cd => {
                   cd.WithSpending(100f, 500f)
                     .WithOrdersPerWeek(1, 3);
               });
    }
    
    public MyFirstNPC() : base(
        id: "my_first_npc",
        firstName: "John",
        lastName: "Doe")
    {
    }
    
    protected override void OnCreated()
    {
        base.OnCreated();
        
        // Set up appearance
        Appearance
            .Set<CustomizationFields.Gender>(0.5f)
            .Set<CustomizationFields.Height>(1.0f)
            .Build();
        
        // Enable systems
        Schedule.Enable();
        Schedule.InitializeActions();
    }
}
```

## Key Concepts

### Physical vs Non-Physical NPCs

**Physical NPCs** (`IsPhysical = true`):
- Visible in the game world with 3D models
- Can be directly interacted with
- Can move around and follow schedules
- Have collision detection and physics

**Non-Physical NPCs** (`IsPhysical = false`):
- Invisible in the world
- Primarily used for messaging and phone contacts
- Cannot be directly interacted with
- Useful for remote contacts or story NPCs

### Configuration Phases

NPCs are configured in two main phases:

1. **Prefab Configuration** (in `ConfigurePrefab`):
   - Set spawn position
   - Configure customer defaults
   - Set relationship defaults
   - Define schedule actions
   - **Must be done here for save/load compatibility**

2. **Runtime Initialization** (in `OnCreated`):
   - Set up appearance
   - Configure dialogue systems
   - Subscribe to events
   - Enable schedule system
   - Set basic properties

### Component System

Each NPC has access to several component systems:

- **`Appearance`**: Visual customization and avatar management
- **`Dialogue`**: Interactive conversation systems
- **`Schedule`**: Movement and activity scheduling
- **`Customer`**: Business customer behavior
- **`Relationship`**: Social connections and relationships
- **`Inventory`**: Item management
- **`Movement`**: Physical movement control

## Best Practices

1. **Always configure customer, relationship, and schedule data in `ConfigurePrefab`** - these systems require prefab-level configuration for proper save/load behavior.

2. **Use the builder pattern** - All configuration methods return the builder instance, allowing for fluent chaining.

3. **Handle exceptions gracefully** - Wrap configuration code in try-catch blocks to prevent NPC creation failures.

4. **Test in both single-player and multiplayer** - NPCs work in both environments, but test thoroughly.

5. **Use meaningful IDs** - NPC IDs are used for save/load and should be unique and descriptive.

## Getting Help

- Review the [API Reference](../api/S1API.html) for detailed method documentation
- Look at the example projects in the repository for real-world usage patterns

## Next Steps

1. Read [Basic NPC Creation](basic-npc-creation.md) to understand the fundamentals
2. Explore [Prefab Configuration](prefab-configuration.md) to learn about component setup
3. Refer to individual system pages for detailed feature documentation
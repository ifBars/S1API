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
- <xref:S1API> - Detailed API documentation

## Example Repository

For complete, production-ready NPC implementations, see the **[S1API NPC Example Repository](https://github.com/ifBars/S1APINPCExample)**. This repository contains four fully-featured example NPCs covering all major use cases:

- **[ExamplePhysicalNPC1](https://github.com/ifBars/S1APINPCExample/blob/master/NPCs/ExamplePhysicalNPC1.cs)** - Customer with dialogue, inventory, and complex scheduling
- **[ExamplePhysicalNPC2](https://github.com/ifBars/S1APINPCExample/blob/master/NPCs/ExamplePhysicalNPC2.cs)** - Customer events and dealer recommendations
- **[ExamplePhysicalDealerNPC](https://github.com/ifBars/S1APINPCExample/blob/master/NPCs/ExamplePhysicalDealerNPC.cs)** - Complete dealer implementation
- **[CharacterCustomizerNPC](https://github.com/ifBars/S1APINPCExample/blob/master/NPCs/CharacterCustomizerNPC.cs)** - UI integration example

## Quick Start

Here's a minimal example to get you started:

```csharp
public sealed class MyFirstNPC : NPC
{
    protected override bool IsPhysical => true;
    
    protected override void ConfigurePrefab(NPCPrefabBuilder builder)
    {
        builder.WithIdentity(
                id: "my_first_npc",
                firstName: "John",
                lastName: "Doe")
                .WithSpawnPosition(new Vector3(0, 0, 0))
                .EnsureCustomer()
                .WithCustomerDefaults(cd => {
                    cd.WithSpending(100f, 500f)
                      .WithOrdersPerWeek(1, 3);
                });
    }
    
    public MyFirstNPC() : base()
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

## What To Read First

- Start here: **[Basic NPC Creation](basic-npc-creation.md)**
- Then: **[Prefab Configuration](prefab-configuration.md)** (identity, relationships, schedules, customer/dealer defaults)
- As needed: **[Dialogue System](dialogue-system.md)**, **[Scheduling System](scheduling-system.md)**, **[Customer Behavior](customer-behavior.md)**, **[Dealer System](dealer-system.md)**

## Getting Help

- Review the <xref:S1API> for detailed method documentation
- Look at the example projects in the repository for real-world usage patterns

## Next Steps

1. Copy an example NPC and get it spawning: [S1API NPC Example Repository](https://github.com/ifBars/S1APINPCExample)
2. Make it walk somewhere: [Scheduling System](scheduling-system.md)
3. Add interaction: [Dialogue System](dialogue-system.md)

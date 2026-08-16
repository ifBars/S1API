# Custom NPCs

The S1API provides a comprehensive system for creating custom NPCs that integrate seamlessly with the base game's systems. This guide covers creating physical NPCs with full functionality including schedules, dialogue, customer, dealer, and supplier behavior, relationships, and appearance customization.

## Overview

Custom NPCs in S1API are built on a modular architecture that allows you to create both physical NPCs (visible in the world) and non-physical NPCs (contacts, informants, etc.). The system provides:

- **Physical NPCs**: Visible in the game world with 3D models, movement, and direct interaction
- **Non-Physical NPCs**: Invisible contacts for messaging and phone interactions
- **Modular Components**: Appearance, Dialogue, Schedule, Customer, Dealer, Supplier, and Relationship systems
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
- **[Dealer System](dealer-system.md)** - NPCs that distribute products for the player
- **[Supplier NPCs](supplier-system.md)** - Native supplier shops, dead drops, meetings, and deliveries
- **[Deliveries](delivery-system.md)** - Read-only active-delivery and receipt wrappers
- **[Relationship Management](relationship-management.md)** - NPC relationships and connections

### API Reference
- <xref:S1API> - Detailed API documentation

## Example Repository

For complete, production-ready NPC implementations, see the **[S1API NPC Example Repository](https://github.com/ifBars/S1APINPCExample)**. This repository contains four fully-featured example NPCs covering all major use cases:

- **[ExamplePhysicalNPC1](https://github.com/ifBars/S1APINPCExample/blob/master/NPCs/ExamplePhysicalNPC1.cs)** - Customer with dialogue, inventory, and complex scheduling
- **[ExamplePhysicalNPC2](https://github.com/ifBars/S1APINPCExample/blob/master/NPCs/ExamplePhysicalNPC2.cs)** - Customer events and dealer recommendations
- **[ExamplePhysicalDealerNPC](https://github.com/ifBars/S1APINPCExample/blob/master/NPCs/ExamplePhysicalDealerNPC.cs)** - Complete dealer implementation
- **[CharacterCustomizerNPC](https://github.com/ifBars/S1APINPCExample/blob/master/NPCs/CharacterCustomizerNPC.cs)** - UI integration example

## Using AI Assistance?

If you use an AI coding agent to create or review custom NPC mods, point it at the **[schedule-one-custom-npcs agents skill](https://github.com/ifBars/S1API/tree/stable/skills/schedule-one-custom-npcs)**. The skill summarizes the S1API two-phase NPC model, physical versus non-physical NPC choices, customer/dealer setup, appearance constraints, schedules, dialogue, and lifecycle checks that custom NPC implementations should follow.

## Quick Start

Here's a minimal example to get you started:

```csharp
public sealed class MyFirstNPC : NPC
{
    public override bool IsPhysical => true;
    public override bool IsCustomer => true;
    
    protected override void ConfigurePrefab(NPCPrefabBuilder builder)
    {
        builder.WithIdentity(
                id: "my_first_npc",
                firstName: "John",
                lastName: "Doe")
                .WithSpawnPosition(new Vector3(0, 0, 0))
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

## Messaging and read objectives

Use `Messaging` when quest or tutorial logic needs to know whether the player opened an NPC conversation. The read state is conversation-level: Schedule One does not expose per-message read receipts for NPC conversations.

```csharp
protected override void OnCreated()
{
    base.OnCreated();

    Messaging.OnConversationOpened += HandleConversationOpened;
    Messaging.SendTextMessage("Open this conversation to continue.");
}

protected override void OnDestroyed()
{
    Messaging.OnConversationOpened -= HandleConversationOpened;
    base.OnDestroyed();
}

private void HandleConversationOpened()
{
    if (!Messaging.HasUnreadMessages)
    {
        // Complete the related quest entry here.
    }
}
```

Available state:

- `Messaging.IsRead`: whether the native conversation is marked as read. A conversation that does not exist yet is treated as read.
- `Messaging.HasUnreadMessages`: the inverse conversation-level unread state.
- `Messaging.IsOpen`: whether this conversation is currently open in the phone's Messages app.
- `Messaging.OnConversationOpened`: raised whenever the conversation is opened, including later reopenings.

`NPC.SendTextMessage(...)` remains available for compatibility. `Messaging.SendTextMessage(...)` forwards to the same implementation so state checks, events, and sending can live under one API surface.

## Selecting a voice

Configure a custom NPC's voice in `ConfigurePrefab`. Use the typed catalog when possible; the string overload accepts the same case-insensitive identifiers.

```csharp
using S1API.Entities.Voices;

protected override void ConfigurePrefab(NPCPrefabBuilder builder)
{
    builder
        .WithIdentity("my-mod:dispatcher", "Dispatch", "")
        .WithVoice(NPCVoiceCatalog.Tyler, pitch: 0.92f);
}
```

Supported identifiers are `cold`, `crackhead`, `female-1`, `female-2`, `goblin`, `hippie`, `joel`, `monotone`, `redneck`, `timid`, and `tyler`.

These identifiers name reusable voice databases, not individual NPCs. For example, Ray's native configuration combines the `tyler` database with a character-specific pitch; use the pitch overload when reproducing that kind of voice profile.

The pitch overload accepts values from `0.1` through `4.0`. Omitting the pitch preserves the selected base prefab's inherited pitch. Omitting `WithVoice(...)` entirely preserves both the inherited voice database and pitch.

Call `WithVoice(...)` when the NPC should keep a specific voice across game updates. Inherited voices depend on S1API's current donor prefab and may change when the game changes that prefab. A missing donor voice also leaves the NPC silent. Invalid identifiers, unavailable databases, and out-of-range pitch values throw an actionable configuration error.

Voice selection controls which clips normal NPC dialogue and reactions play. It does not play an individual voice line.

## What To Read First

- Start here: **[Basic NPC Creation](basic-npc-creation.md)**
- Then: **[Prefab Configuration](prefab-configuration.md)** (identity, relationships, schedules, customer/dealer defaults)
- As needed: **[Dialogue System](dialogue-system.md)**, **[Scheduling System](scheduling-system.md)**, **[Customer Behavior](customer-behavior.md)**, **[Dealer System](dealer-system.md)**, **[Supplier NPCs](supplier-system.md)**

## Getting Help

- Review the <xref:S1API> for detailed method documentation
- Look at the example projects in the repository for real-world usage patterns
- When working with an AI coding agent, share the [schedule-one-custom-npcs agents skill](https://github.com/ifBars/S1API/tree/stable/skills/schedule-one-custom-npcs) so generated NPC code follows the same S1API patterns as these docs.

## Next Steps

1. Copy an example NPC and get it spawning: [S1API NPC Example Repository](https://github.com/ifBars/S1APINPCExample)
2. Make it walk somewhere: [Scheduling System](scheduling-system.md)
3. Add interaction: [Dialogue System](dialogue-system.md)

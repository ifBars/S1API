# S1API Module Overview

S1API provides **28+ major modules** covering all aspects of Schedule One modding. This page provides a quick reference to help you find the right tools for your project.

## Core Systems

### Entities & NPCs
**Namespace**: `S1API.Entities`

Create custom NPCs with behaviors, schedules, dialogue, and AI.

**Key Classes**:
- `NPC` - Base class for custom NPCs
- `NPCPrefabBuilder` - Configure NPC prefabs
- `NPCSchedule` - Daily routine system
- `NPCCustomer` - Customer behavior
- `Dealer` - Dealer functionality

**Documentation**: [Custom NPCs](custom-npcs.md) | [Dealer System](dealer-system.md)

---

### Quests
**Namespace**: `S1API.Quests`

Build custom quest systems with objectives and tracking.

**Key Classes**:
- `Quest` - Base quest class
- `QuestManager` - Quest registry
- `QuestEntry` - Quest objectives

**Documentation**: [Quests System](quests-system.md)

---

### Items
**Namespace**: `S1API.Items`

Define custom items, equippables, and inventory management.

**Key Classes**:
- `ItemCreator` - Factory for creating items
- `ItemDefinition` - Item definition wrapper
- `StorableItemDefinition` - Stackable items
- `Equippable` - Equippable items
- `AvatarEquippableRegistry` - Third-person item display

**Documentation**: [Items](items.md)

---

### Products & Properties
**Namespace**: `S1API.Products`, `S1API.Properties`

Create sellable products (drugs, goods) with custom properties.

**Key Classes**:
- `ProductDefinition` - Product wrapper
- `WeedDefinition`, `CocaineDefinition`, `MethDefinition` - Specific drugs
- `Property` - Product properties (THC%, Purity, etc.)
- `ProductPropertyWrapper` - Runtime property access

**Documentation**: [Products & Properties](products-system.md)

---

### Phone Apps
**Namespace**: `S1API.PhoneApp`

Build fully-featured phone applications with custom UI.

**Key Classes**:
- `PhoneApp` - Base phone app class
- `PhoneAppButtonHandler` - Button handling

**Documentation**: [Phone Apps](phone-app.md)

---

### Phone Calls
**Namespace**: `S1API.PhoneCalls`

Script multi-stage phone conversations.

**Key Classes**:
- `PhoneCallDefinition` - Call definition
- `CallManager` - Call queue management
- `CallerDefinition` - Caller identity
- `CallStageEntry` - Call stages

**Documentation**: [Phone Calls](phone-calls.md)

---

## World & Interaction

### Map & Buildings
**Namespace**: `S1API.Map`

Access and manage world locations, buildings, and parking lots.

**Key Classes**:
- `Building` - Building wrapper with deferred resolution
- `ParkingLotRegistry` - Parking lot management
- `DeliveryLocation` - Delivery points

**Documentation**: [Building Registry](building-registry.md) | [Delivery Location Registry](delivery-location-registry.md)

---

### Vehicles
**Namespace**: `S1API.Vehicles`

Vehicle spawning and management.

**Key Classes**:
- `LandVehicle` - Land vehicle wrapper
- `VehicleRegistry` - Vehicle registration
- `VehicleColor` - Color configuration
- `ParkingAlignment` - Parking helpers

**Documentation**: In development

---

### Dialogues
**Namespace**: `S1API.Dialogues`

Custom dialogue systems with branching conversations.

**Key Classes**:
- `DialogueInjection` - Dialogue injection
- `DialogueInjector` - Injection system
- `DialogueChoiceListener` - Choice handling

**Documentation**: [Dialogue System](dialogue-system.md)

---

### Schedules
**Namespace**: `S1API.Entities.Schedule`

Daily routine systems for NPCs with time-based actions.

**Key Classes**:
- `NPCSchedule` - Schedule management
- `NPCScheduleBuilder` - Build schedules
- `WalkToSpec`, `StayInBuildingSpec` - Action specs

**Documentation**: [Scheduling System](scheduling-system.md)

---

## Economy & Gameplay

### Economy
**Namespace**: `S1API.Economy`

Contracts, dealers, and customer systems.

**Key Classes**:
- `Contract` - Contract wrapper
- `ContractInfo` / `ContractInfoBuilder` - Contract configuration
- `CustomerStandard` - Quality standards
- `DealerType` - Dealer types

**Documentation**: [Dealer System](dealer-system.md) | [Customer Behavior](customer-behavior.md)

---

### Money
**Namespace**: `S1API.Money`

Currency management utilities.

**Key Classes**:
- `CashDefinition` - Cash item
- Money management helpers

**Documentation**: In development

---

### Growing
**Namespace**: `S1API.Growing`

Custom plants and seed definitions.

**Key Classes**:
- `SeedDefinition` / `SeedInstance` - Seed system
- `SeedCreator` - Seed factory
- `PlantInstance` - Growing plants

**Documentation**: In development

---

### Dead Drops
**Namespace**: `S1API.DeadDrops`

Covert delivery location management.

**Key Classes**:
- `DeadDropManager` - Dead drop management
- `DeadDropInstance` - Individual drops

**Documentation**: In development

---

### Cartel
**Namespace**: `S1API.Cartel`

Track and respond to cartel relationship status.

**Key Classes**:
- `Cartel` - Cartel singleton
- `CartelStatus` - Status enum (Unknown, Truced, Hostile, Defeated)

**Documentation**: [Cartel System](cartel-system.md)

---

### Law Enforcement
**Namespace**: `S1API.Law`

Control checkpoints, police dispatch, wanted levels, and law enforcement intensity.

**Key Classes**:
- `CheckpointManager` - Road checkpoint control
- `CheckpointInfo` - Checkpoint state information
- `LawController` - Law enforcement intensity and automatic activities
- `LawManager` - Police dispatch and wanted levels
- `PursuitLevel` - Wanted level severity
- `PlayerCrimeData` - Player crime tracking

**Documentation**: [Law Enforcement](law-enforcement.md)

---

### Leveling
**Namespace**: `S1API.Leveling`

Player progression and rank management with XP accessors, unlockables, and rank-up events.

**Key Classes**:
- `LevelManager` - XP/rank access plus OnXPChanged/OnRankUp events
- `FullRank` - Rank + tier helper struct
- `Unlockable` - Rank-locked UI entries
- `Rank` - Rank enumeration

**Documentation**: In development

---

## Utilities

### Saveables
**Namespace**: `S1API.Saveables`

Automatic JSON-based save/load system.

**Key Classes**:
- `Saveable` - Base saveable class
- `SaveableField` - Attribute for marking fields
- `SaveableLoadOrder` - Load order control for saveables

**Documentation**: [Save System](save-system.md)

---

### Asset Bundles
**Namespace**: `S1API.AssetBundles`

Cross-runtime asset loading (Mono/IL2CPP).

**Key Classes**:
- `WrappedAssetBundle` - AssetBundle wrapper
- `WrappedAssetBundleRequest` - Async loading

**Documentation**: See [Items](items.md#creating-avatarequippable-prefabs) for AssetBundle usage

---

### UI Factory
**Namespace**: `S1API.UI`

Rapid Unity UI creation with consistent styling.

**Key Classes**:
- `UIFactory` - UI creation methods
- `ButtonUtils` - Button helpers
- `ImageUtils` - Image loading

**Documentation**: [UI](ui.md)

---

### Logging
**Namespace**: `S1API.Logging`

Standardized logging for mods.

**Key Classes**:
- `Log` - Logger wrapper

**Usage**:
```csharp
private static readonly Log Logger = new Log("MyMod");
Logger.Msg("Hello from my mod!");
Logger.Error("Something went wrong!");
```

---

### Console Commands
**Namespace**: `S1API.Console`

Register custom debug commands.

**Key Classes**:
- `BaseConsoleCommand` - Base command class
- `ConsoleHelper` - Console utilities
- `CustomConsoleRegistry` - Command registry

**Documentation**: In development

---

### Input Management
**Namespace**: `S1API.Input`

Cross-runtime input handling.

**Key Classes**:
- `Controls` - Input wrapper
- Input state management

**Documentation**: In development

---

### GameTime
**Namespace**: `S1API.GameTime`

Manage in-game time and date.

**Key Classes**:
- `TimeManager` - Time management
- `GameDateTime` - Date/time representation
- `Day` - Day of week enum

**Documentation**: In development

---

### Avatar
**Namespace**: `S1API.Avatar`

Player avatar and seating systems.

**Key Classes**:
- `Avatar` - Player avatar wrapper
- `AvatarSettings` - Avatar configuration
- `Seat` - Seating system

**Documentation**: [Seating Registry](seating-registry.md)

---

### Storages
**Namespace**: `S1API.Storages`

Manage storage containers (fridges, safes, etc.).

**Key Classes**:
- `StorageInstance` - Storage wrapper

**Documentation**: In development

---

### Messaging
**Namespace**: `S1API.Messaging`

In-game messaging and responses.

**Key Classes**:
- `Response` - Response wrapper

**Documentation**: In development

---

### Utils
**Namespace**: `S1API.Utils`

Reflection and cross-runtime utilities.

**Key Classes**:
- `ReflectionUtils` - Reflection helpers
- `CrossType` - Type utilities
- `ArrayExtensions` - Array helpers

---

## Internal Systems

### S1API.Internal
**Purpose**: Core infrastructure - patches, lifecycle, abstractions

**Do not use directly** - these are internal implementation details that support the public API.

---

## Quick Reference by Task

### "I want to create..."

| Task | Module | Documentation |
|------|--------|---------------|
| Custom NPC | `S1API.Entities` | [Custom NPCs](custom-npcs.md) |
| Dealer NPC | `S1API.Entities` + `S1API.Economy` | [Dealer System](dealer-system.md) |
| Customer NPC | `S1API.Entities` | [Customer Behavior](customer-behavior.md) |
| Quest | `S1API.Quests` | [Quests System](quests-system.md) |
| Item | `S1API.Items` | [Items](items.md) |
| Phone App | `S1API.PhoneApp` | [Phone Apps](phone-app.md) |
| Phone Call | `S1API.PhoneCalls` | [Phone Calls](phone-calls.md) |
| Custom Product | `S1API.Products` | [Products & Properties](products-system.md) |
| UI Element | `S1API.UI` | [UI](ui.md) |

### "I want to work with..."

| Feature | Module | Documentation |
|---------|--------|---------------|
| Schedules & Movement | `S1API.Entities.Schedule` | [Scheduling System](scheduling-system.md) |
| Dialogue Trees | `S1API.Dialogues` | [Dialogue System](dialogue-system.md) |
| Appearance | `S1API.Entities.Appearances` | [Appearance Customization](appearance-customization.md) |
| Relationships | `S1API.Entities` | [Relationship Management](relationship-management.md) |
| Buildings & Locations | `S1API.Map` | [Building Registry](building-registry.md) |
| Save/Load Data | `S1API.Saveables` | [Save System](save-system.md) |
| Cartel Status | `S1API.Cartel` | [Cartel System](cartel-system.md) |

---

## Getting Started

1. **New to S1API?** Start with [Getting Started](getting-started.md)
2. **Want to create NPCs?** Read [Custom NPCs](custom-npcs.md)
3. **Building a phone app?** See [Phone Apps](phone-app.md)
4. **Need to save data?** Check [Save System](save-system.md)

---

## Example Mods

Learn by example:

- **CustomNPCTest** - Physical NPCs with schedules, dialogue, customers
- **More-NPCs** - Additional NPCs with Cartel integration
- **S1NotesApp** - Full phone app with save system and quests
- **TextYourFriends** - Messaging system example

All examples available in the [S1API repository](https://github.com/ifBars/S1API).

---

## Need Help?

- Browse the documentation sidebar for detailed guides
- Check the example mods for real-world usage
- Visit [GitHub Issues](https://github.com/ifBars/S1API/issues) for support

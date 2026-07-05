---
title: "Game Source Summary"
description: "Condensed overview of major Schedule I namespaces, class hierarchies, and common modding patterns"
status: "stable"
version: "2.0.0"
reviewed: "2026-07-03"
applies_to: "Schedule I v0.4.3+"
---

# Game Source Summary

## Project Overview

| Property | Value |
|----------|-------|
| **Game** | Schedule 1 |
| **Type** | Multiplayer (4-Player Co-op) |
| **Engine** | Unity 2022+ with URP |
| **Network** | FishNet + FishySteamworks |
| **Platform** | PC via Steam |
| **Storage** | JSON (Newtonsoft.Json) |

---

## Folder Structure

```
game-source/
├── assemblies/          → DLLs
├── assets/              → Unity Assets (Models, Textures, Animations)
├── packages/            → Unity Packages
├── project-settings/    → Unity Settings
└── scripts/             → 187 folders, ~5000+ C# files
    ├── Scripts/Assembly-CSharp/    ← MAIN GAME CODE
    ├── ScheduleOne.Core/           ← Core Library
    ├── GameKit.Dependencies/       ← Editor Tools
    ├── GameKit.Utilities/          ← Utility Functions
    └── [150+ Third-Party Folders]  ← External Libraries
```

---

## Core Systems

### 1. Registry (Central Item Registration)
- Stores all items with string IDs
- `GetItem(string ID)` - Retrieve item
- `AddToRegistry()` - Add items at runtime
- `RemoveRuntimeItems()` - Remove runtime items on scene change

### 2. GameInput (Input Management)
- Handles keyboard/mouse AND gamepad
- 34 input actions (ButtonCode enum)
- Important: `Back` = Right mouse button / Gamepad B, `Escape` = ESC (separate!)
- Exit system with prioritized listeners

### 3. Console (Developer Console)
- 40+ commands: `settime`, `give`, `spawnvehicle`, `teleport`, `sethealth`, `changecash`
- Only active in Editor/Debug build

### 4. AchievementManager
- 13 Steam achievements (Prologue, Dealer, Cooking, Business, etc.)

---

## Player Systems

### Player.cs (5859 lines) - Main Player Class
- NetworkBehaviour with ISaveable, IDamageable, ISightable
- Contains: Health, CrimeData, Energy, Clothing, Inventory, CharacterController
- `Player.Local` - Reference to local player

### PlayerMovement.cs
- **Walk:** 3.25f
- **Sprint:** 1.9x multiplier
- **Jump:** 5.25f force
- **Stamina:** 12.5f drain, 25f recovery

### PlayerCrimeData.cs
- Wanted System: None → Low → Medium → High → Maximum
- `Escalate()` / `Deescalate()` / `ClearCrimes()`

### PlayerEnergy.cs
- Energy system (0-100)

---

## NPC Systems

### NPC.cs (3176 lines)
- NetworkBehaviour with Awareness, Responses, Actions, Behaviour
- Properties: Aggression, Region, CanOpenDoors

### 54 Behaviours:
| Category | Behaviours |
|----------|------------|
| **Base** | Idle, Flee, Cowering, Dead, Unconscious, Ragdoll |
| **Patrol** | FootPatrol, VehiclePatrol, Checkpoint, Sentry |
| **Pursuit** | Pursuit, VehiclePursuit |
| **Drugs** | ConsumeProduct, DealerAttendDeal, RequestProduct |
| **Growing** | GrowContainer, HarvestPot, WaterPot, SowSeedInPot |
| **Crafting** | StartChemistryStation, StartLabOven, StartMixingStation |
| **Packaging** | PackagingStation, BrickPress |
| **Trash** | BagTrashCan, PickUpTrash, DisposeTrashBag |

### NPCManager
- `GetNPC(string id)` - Find NPC by ID
- `GetNPCsInRegion()` - Find NPCs in region

---

## Product System (CORE GAMEPLAY MECHANIC)

### Product Types
| Type | Description |
|------|-------------|
| **Weed** | Marijuana |
| **Cocaine** | Cocaine |
| **Meth** | Methamphetamine (also liquid) |
| **Shrooms** | Mushrooms |

### Features
- Mix recipes (MixRecipeData)
- Packaging with stealth levels (Baggie=1, Jar=5, Brick=20)
- Quality and quantity system
- Discovery system for new mixes

### Item Hierarchy
```
ItemInstance
  └─ QualityItemInstance (has quality)
       └─ ProductItemInstance (has quality + packaging)
```

---

## Money System

### MoneyManager
- **Cash Balance** and **Online Balance** (SyncVar)
- Transaction ledger
- `ChangeCashBalance()` - Modify cash
- `CreateOnlineTransaction()` - Online transaction

---

## Property System

### Property
- NetworkBehaviour with ISaveable
- Features: EmployeeCapacity, ForSaleSign, LoadingDocks
- Events: `onPropertyAcquired`

### Specific Properties
- Bungalow, Manor, MotelRoom, RV, SewerOffice, Sweatshop

---

## Employee System

### Employee (inherits NPC)
- SigningFee: 500f, DailyWage: 100f
- WorkSpeedController, AssignedProperty

### Employee Types
| Type | Task |
|------|------|
| **Botanist** | Plant cultivation |
| **Chemist** | Chemical production |
| **Cleaner** | Cleaning |
| **Packager** | Packaging |

---

## Quest System

### 30+ Quests
- **Early:** GettingStarted, WeNeedToCook, OnTheGrind
- **Mid:** SecuringSupplies, ExpandingOperations, MovingUp
- **Endgame:** TheDeepEnd, SinkOrSwim, DefeatCartel
- **Employees:** Employees, Botanists, Chemists, Cleaners, Packagers

### Quest States
- Inactive → Active → Completed/Failed

---

## Item Framework (79 files)

- ItemDefinition, ItemInstance, ItemSlot
- StorableItemDefinition with level requirements
- ItemFilter for slot filtering

---

## Third-Party Libraries

| Library | Purpose |
|---------|---------|
| FishNet | Network framework |
| FishySteamworks | Steam transport |
| Steamworks.NET | Steam API |
| Newtonsoft.Json | JSON serialization |
| AstarPathfindingProject | Pathfinding |
| HBAO | Ambient Occlusion |
| ShaderX | Shader utilities |
| Poly2Tri/ClipperLib | Geometry algorithms |
| Ookii.Dialogs | Windows dialogs |

---

## Summary

Schedule 1 is a comprehensive multiplayer game with:
- Complete drug trading system (cultivation, production, sales)
- NPC ecosystem with 54 different behaviours
- Property and employee management
- Quest system with 30+ quests
- Full developer console
- Steam integration (Achievements, Multiplayer)
- JSON-based persistence

---
title: "Growing System"
description: "Reference for the growing system — Plant, GrowContainer, SeedDefinition, and cultivation mechanics"
status: "stable"
version: "2.0.0"
reviewed: "2026-07-03"
applies_to: "Schedule I v0.4.3+"
---

# Growing System

## Plant (`Il2CppScheduleOne.Growing.Plant`)
Abstract base for all growable plants.

### Fields
| Field | Type | Description |
|-------|------|-------------|
| `VisualsContainer` | `Transform` | Container for plant visuals |
| `GrowthStages` | `PlantGrowthStage[]` | Ordered growth stage transforms |
| `Collider` | `Collider` | Plant collider |
| `SeedDefinition` | `SeedDefinition` | The seed this plant came from |
| `GrowthTime` | `int` | Minutes to full growth (default: 48) |
| `BaseYieldQuantity` | `int` | Base harvest yield (default: 12) |
| `HarvestTarget` | `string` | Item ID for harvested product (e.g. `"buds"`) |
| `MinColliderScale` | `float` | Minimum collider scale (0.4) |
| `ColliderScaleThreshold` | `float` | Scale threshold (0.5) |
| `ActiveHarvestables` | `List<int>` | Currently active harvest site indices |
| `PlantScrapPrefab` | `TrashItem` | Prefab dropped on full harvest |

### Properties
| Property | Type | Description |
|----------|------|-------------|
| `Pot` | `Pot` (get) | Parent pot container |
| `NormalizedGrowthProgress` | `float` (get) | 0.0 to 1.0 |
| `IsFullyGrown` | `bool` (get) | `NormalizedGrowthProgress >= 1f` |
| `YieldMultiplier` | `float` (get) | Modified by additives |
| `QualityLevel` | `float` (get) | Modified by additives, base = 0.5 |
| `FinalGrowthStage` | `PlantGrowthStage` (get) | Last growth stage |

### Methods
| Method | Description |
|--------|-------------|
| `Initialize(NetworkObject pot, float growthProgress)` | Setup plant in pot |
| `MinPass(int mins)` | Called each minute: calculates growth |
| `AdditiveApplied(AdditiveDefinition, bool isInitial)` | Apply additive effects |
| `SetNormalizedGrowthProgress(float)` | Set progress + update visuals |
| `SetHarvestableActive(int index, bool active)` | Toggle harvestable site |
| `IsHarvestableActive(int index)` | Check harvestable state |
| `SetVisible(bool vis)` | Toggle visual container |
| `GetHarvestedProduct(int quantity)` | Get harvested item instance (abstract) |
| `GetPlantData()` | Serialize to PlantData |

### Growth Calculation (per minute)
```
growth = (1.0 / (GrowthTime * 60)) * mins
growth *= Pot.GetTemperatureGrowthMultiplier()
growth *= Pot.GetAverageLightExposure()
growth *= Pot.GrowSpeedMultiplier
growth *= GrowSpeedMultiplierFromLight
// If moisture = 0, growth = 0
```

### Growth Stages
- Each stage is a `PlantGrowthStage` with `GrowthSites[]` (transforms for harvestable buds)
- Stage activated by floor: `stageIndex = floor(progress * totalStages)`
- At 100% growth: random subset of GrowthSites become harvestable (based on yield)

---

## PlantGrowthStage (`Il2CppScheduleOne.Growing.PlantGrowthStage`)
Simple component on growth stage visuals.

### Fields
| Field | Type | Description |
|-------|------|-------------|
| `GrowthSites` | `Transform[]` | Harvestable positions in this stage |

---

## SeedDefinition (`Il2CppScheduleOne.Growing.SeedDefinition`)
`StorableItemDefinition` for seeds.

### Fields
| Field | Type | Description |
|-------|------|-------------|
| `FunctionSeedPrefab` | `FunctionalSeed` | Seed usable item prefab |
| `PlantPrefab` | `Plant` | Plant prefab this seed grows |

---

## GrowContainer (`Il2CppScheduleOne.Growing.GrowContainer`)
Abstract base for all grow containers (pots, beds). Extends `GridItem`, implements `IUsable`, `ITransitEntity`.

### Constants
| Constant | Value | Description |
|----------|-------|-------------|
| `DryThreshold` | 0 | Moisture at which growth stops |

### Properties
| Property | Type | Description |
|----------|------|-------------|
| `SoilCapacity` | `float` | Max soil amount (default: 30) |
| `MoistureCapacity` | `float` | Max moisture amount (default: 5) |
| `NormalizedSoilAmount` | `float` (get) | 0.0 to 1.0 |
| `NormalizedMoistureAmount` | `float` (get) | 0.0 to 1.0 |
| `IsFullyFilledWithSoil` | `bool` (get) | At capacity + soil present |
| `CurrentSoil` | `SoilDefinition` (get) | Currently filled soil |
| `AppliedAdditives` | `List<AdditiveDefinition>` (get) | Active additives |
| `NPCUserObject` | `NetworkObject` (SyncVar) | NPC currently using |
| `PlayerUserObject` | `NetworkObject` (SyncVar) | Player currently using |

### Fields
| Field | Type | Description |
|-------|------|-------------|
| `AllowedSoils` | `SoilDefinition[]` | Soils permitted in this container |
| `AllowedAdditives` | `AdditiveDefinition[]` | Additives permitted |
| `_moistureDrainPerHour` | `float` | Moisture drained per hour (default: 1) |
| `SoilContainer` | `Transform` | Visual soil transform |
| `InputSlots` / `OutputSlots` | `List<ItemSlot>` | Transit entity interface |
| `AccessPoints` | `Transform[]` | NPC access points |

### Events
| Event | Description |
|-------|-------------|
| `onMinPass` | `Action` — called each game minute |
| `onTimeSkip` | `Action<int>` — called on time skip |

### Methods
| Method | Description |
|--------|-------------|
| `SetSoil(SoilDefinition)` | Set current soil |
| `ChangeSoilAmount(float)` | Add/remove soil |
| `SetSoilAmount(float)` | Set absolute soil amount |
| `ChangeMoistureAmount(float)` | Add/remove moisture |
| `SetMoistureAmount(float)` | Set absolute moisture |
| `IsSoilAllowed(SoilDefinition)` | Check if soil is permitted |
| `CanApplyAdditive(AdditiveDefinition, out string)` | Check if additive can be applied |
| `ApplyAdditive_Server(string additiveID)` | Server RPC to apply additive |
| `GetAverageLightExposure(out float growSpeedMultiplier)` | Calculate light exposure |
| `GetTemperatureGrowthMultiplier()` | Temperature modifier (default: 1.0) |
| `IsAdditiveApplied(string additiveID)` | Check if additive is active |
| `IsPointAboveGrowSurface(Vector3)` | Abstract — surface check |
| `SetGrowableVisible(bool)` | Abstract — toggle growable visuals |
| `GetGrowthProgressNormalized()` | Abstract — current progress |
| `ContainsGrowable()` | Abstract — has a plant |
| `SetPlayerUser(NetworkObject)` | Server RPC |
| `SetNPCUser(NetworkObject)` | Server RPC |

### Moisture Drain
`_moistureDrainPerHour / 60f` per minute.

---

## Concrete Plants

### WeedPlant (`Il2CppScheduleOne.Growing.WeedPlant`)
Extends `Plant`. Base weed plant — grows buds (`HarvestTarget = "buds"`).

### CocaPlant (`Il2CppScheduleOne.Growing.CocaPlant`)
Extends `Plant`. Coca plant for cocaine production.

### GrowingMushroom (`Il2CppScheduleOne.Growing.GrowingMushroom`)
Extends `Plant`. Grows in mushroom beds.

### ShroomColony (`Il2CppScheduleOne.Growing.ShroomColony`)
Handles shroom colony growth logic.

---

## Additives

### AdditiveDefinition (`Il2CppScheduleOne.Growing.AdditiveDefinition`)
ScriptableObject for growth additives.

### Additive Fields (from registry)
| Property | Effect |
|----------|--------|
| `QualityChange` | Modifies final quality |
| `YieldMultiplier` | Multiplier on yield quantity |
| `InstantGrowth` | Immediate progress boost (0.0-1.0) |

---

## Seed Types

### FunctionalSeed (`Il2CppScheduleOne.Growing.FunctionalSeed`)
Equippable item that represents a seed the player can plant.

---

## Concrete Grow Containers

### Pot (`Il2CppScheduleOne.ObjectScripts.Pot`) extends GrowContainer
- Soil capacity: 30
- Moisture capacity: 5
- Has interaction for planting/harvesting
- `PotInteraction`, `PotMoistureDisplay`

### MushroomBed (`Il2CppScheduleOne.ObjectScripts.MushroomBed`) extends GrowContainer
- `MushroomBedInteraction`
- `MushroomBedMoistureDisplay`

---

## Key Usage Patterns

### Creating a custom plant
```csharp
// 1. Create SeedDefinition ScriptableObject (inherits StorableItemDefinition)
// 2. Set PlantPrefab to your Plant subclass prefab
// 3. Register seed in Registry

// Plant lifecycle:
// 1. Player uses seed on Pot → Pot plants seed
// 2. Plant.Initialize(pot, growthProgress) called
// 3. Each minute: Plant.MinPass(mins) → growth calculation
// 4. At 100%: harvestable sites become active
// 5. Player harvests → Plant.GetHarvestedProduct() called
// 6. After full harvest: plant destroyed
```

### Checking plant state
```csharp
Plant plant = pot.GetComponentInChildren<Plant>();
if (plant != null)
{
    float progress = plant.NormalizedGrowthProgress;
    float quality = plant.QualityLevel;
    int harvestCount = plant.ActiveHarvestables.Count;
}
```

## S1Toolkit API

Instead of raw Il2Cpp classes, the [S1Toolkit API](../api/api-reference.md) can be used:

| Method | Description |
|---|---|---|
| `Api.Growing.Plant()` | Plant a seed |
| `Api.Growing.GetGrowthProgress()` | Get growth progress |
| `Api.Growing.IsReadyToHarvest()` | Check if ready to harvest |
| `Api.Growing.Harvest()` | Harvest plant |
| `Api.Growing.Water()` | Water plant |
| `Api.Growing.AddAdditive()` | Add additive |

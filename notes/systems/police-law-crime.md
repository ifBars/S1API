---
title: "Police, Law & Crime System"
description: "Reference for the police, law, and crime system — LawManager, Crime, PoliceOfficer, Investigation, and curfew"
status: "stable"
version: "2.0.0"
reviewed: "2026-07-03"
applies_to: "Schedule I v0.4.3+"
---

# Police, Law & Crime System

## PoliceOfficer (`Il2CppScheduleOne.Police.PoliceOfficer`)
Extends `NPC`. The police NPC class with pursuit, body search, checkpoint, and patrol behaviours.

### Constants
| Constant | Value | Description |
|----------|-------|-------------|
| `DEACTIVATION_TIME` | 1s | Time before officer deactivates |
| `INVESTIGATION_COOLDOWN` | 60s | Cooldown between body searches |
| `INVESTIGATION_MAX_DISTANCE` | 8f | Max range for investigation |
| `INVESTIGATION_MIN_VISIBILITY` | 0.2f | Min visibility to investigate |
| `INVESTIGATION_CHECK_INTERVAL` | 1f | How often investigation is checked |
| `BODY_SEARCH_CHANCE_DEFAULT` | 0.1f | Base chance of body search |

### Static Members
| Member | Type | Description |
|--------|------|-------------|
| `OnPoliceVisionEvent` | `Action<VisionEventReceipt>` | Global vision event |
| `Officers` | `List<PoliceOfficer>` | All active officers |

### Properties
| Property | Type | Description |
|----------|------|-------------|
| `IgnorePlayers` | `bool` (SyncVar) | If true, ignores all player crimes |
| `PursuitTarget` | `NetworkObject` (get) | Current foot pursuit target |
| `AssignedVehicle` | `LandVehicle` (get/set) | Vehicle assigned to this officer |
| `Suspicion` | `float` (0-1) | Officer suspicion level |
| `Leniency` | `float` (0-1) | Officer leniency level |
| `BodySearchChance` | `float` (0-1) | Chance to body search |
| `BodySearchDuration` | `float` | Duration of body search |

### Behaviours (serialized references)
| Behaviour | Type | Purpose |
|-----------|------|---------|
| `PursuitBehaviour` | `PursuitBehaviour` | Foot chase |
| `VehiclePursuitBehaviour` | `VehiclePursuitBehaviour` | Vehicle chase |
| `BodySearchBehaviour` | `BodySearchBehaviour` | Body search |
| `CheckpointBehaviour` | `CheckpointBehaviour` | Road checkpoint duty |
| `FootPatrolBehaviour` | `FootPatrolBehaviour` | Walking patrol |
| `VehiclePatrolBehaviour` | `VehiclePatrolBehaviour` | Driving patrol |
| `SentryBehaviour` | `SentryBehaviour` | Stationary sentry |

### Methods
| Method | Description |
|--------|-------------|
| `BeginFootPursuit_Networked(string playerCode, bool includeColleagues)` | Start foot chase |
| `BeginVehiclePursuit_Networked(string playerCode, NetworkObject vehicle, bool beginAsSighted)` | Start vehicle chase |
| `BeginBodySearch_Networked(string playerCode)` | Start body search |
| `AssignToCheckpoint(ECheckpointLocation)` | Assign to checkpoint duty |
| `UnassignFromCheckpoint()` | Remove from checkpoint |
| `StartFootPatrol(PatrolGroup, bool warp)` | Start foot patrol |
| `StartVehiclePatrol(VehiclePatrolRoute, LandVehicle)` | Start vehicle patrol |
| `AssignToSentryLocation(SentryLocation)` | Assign to sentry post |
| `Activate()` | Spawn/exited building -> active |
| `Deactivate()` | Return to police station pool |
| `SetIgnorePlayers(bool)` | Toggle player ignoring |
| `GetNearestOfficer(Vector3 pos, out float dist, bool onlyConscious)` | Static — find nearest |
| `ConductBodySearch(Player)` | Execute body search on player |
| `BodySearchLocalPlayer()` | Debug — search local player |

### Deactivation Logic
1. Not in building + server + AutoDeactivate
2. No active pursuit, schedule, or blocking behaviour
3. Must be conscious
4. After 1s ready-to-pool: walk to police station
5. If out of player sight for > 1s: deactivate immediately

### Body Search Investigation
- Officer checks nearby players every 1s
- Chance based on: suspicion, visibility, officer Suspicion stat, BodySearchChance
- Progress builds over time (modified by distance, visibility, suspiciousness)
- At 100%: body search begins
- Cooldown: 60s per player

---

## Investigation (`Il2CppScheduleOne.Police.Investigation`)
Tracks officer body search investigation state.

### Properties
| Property | Type | Description |
|----------|------|-------------|
| `CurrentProgress` | `float` | Investigation progress (0.0-1.0) |
| `Target` | `Player` | Player being investigated |

---

## Offense (`Il2CppScheduleOne.Police.Offense`)
Represents a set of criminal charges.

### Nested Class: Charge
| Field | Type | Description |
|-------|------|-------------|
| `chargeName` | `string` | Name of charge |
| `crimeIndex` | `int` | Crime severity index |
| `quantity` | `int` | Quantity/severity multiplier |

### Fields
| Field | Type | Description |
|-------|------|-------------|
| `charges` | `List<Charge>` | All charges |
| `penalties` | `List<string>` | Penalty descriptions |

---

## Law System

### LawManager (`Il2CppScheduleOne.Law.LawManager`)
`Singleton<LawManager>`. Manages police dispatch and patrols.

### Constants
| Constant | Value | Description |
|----------|-------|-------------|
| `DISPATCH_OFFICER_COUNT` | 2 | Officers sent per dispatch |
| `DISPATCH_VEHICLE_USE_THRESHOLD` | 25f | Distance threshold for vehicle dispatch |

### Methods
| Method | Description |
|--------|-------------|
| `PoliceCalled(Player target, Crime crime)` | Dispatch officers to player |
| `StartFootpatrol(FootPatrolRoute, int members)` | Start foot patrol |
| `StartVehiclePatrol(VehiclePatrolRoute)` | Start vehicle patrol |

### Crime Types (`Il2CppScheduleOne.Law`)
| Class | Description |
|-------|-------------|
| `Crime` | Base crime class |
| `Assault` | Physical assault |
| `AttemptingToSell` | Attempting to sell drugs |
| `BrandishingWeapon` | Wielding weapon in public |
| `DeadlyAssault` | Deadly weapon assault |
| `DischargeFirearm` | Firing a gun |
| `DrugTrafficking` | Major drug crime |
| `Evading` | Running from police |
| `FailureToComply` | Not following orders |
| `PossessingControlledSubstances` | Drug possession |
| `PossessingLowSeverityDrug` | Minor drug possession |
| `PossessingModerateSeverityDrug` | Moderate drug possession |
| `PossessingHighSeverityDrug` | High severity drug possession |
| `Theft` | Stealing |
| `TransportingIllicitItems` | Transporting illegal goods |
| `Vandalism` | Property damage |
| `VehicularAssault` | Vehicle-based assault |
| `ViolatingCurfew` | Out after curfew |

### Crime Base Properties
| Property | Type | Description |
|----------|------|-------------|
| `CrimeName` | `string` | Override in each crime subclass |

---

## PlayerCrimeData (`Il2CppScheduleOne.PlayerScripts.PlayerCrimeData`)
Component on Player tracking wanted level, body search state.

### EPursuitLevel Enum
| Value | Description |
|-------|-------------|
| `None` | No pursuit |
| `Low` | Minor wanted |
| `Medium` | Moderate wanted |
| `High` | Active pursuit |
| `Maximum` | Max wanted level |

### Key Properties/Methods
| Member | Description |
|--------|-------------|
| `CurrentPursuitLevel` | Current pursuit level |
| `Escalate()` / `Deescalate()` | Change wanted level |
| `BodySearchPending` | Whether body search is active |
| `TimeSinceLastBodySearch` | Cooldown tracker |
| `LastKnownPosition` | Where police last spotted player |
| `ClearCrimes()` | Reset all crime data |

### Wanted Level Flow
1. Crime committed → police called via `LawManager.PoliceCalled()`
2. Nearest `PoliceStation.Dispatch(2, player)` → sends 2 officers
3. Officers pursue based on `ShouldNoticeGeneralCrime()` conditions
4. On sight: foot or vehicle pursuit begins
5. On arrest: player crime data reset, jail sequence

### Curfew System
| Class | Description |
|-------|-------------|
| `CurfewManager` | Manages curfew instances |
| `CurfewInstance` | Active curfew times/zones |
| `ViolatingCurfew` | Crime for curfew violation |

### Checkpoint System
| Class | Description |
|-------|-------------|
| `CheckpointManager` | Manages road checkpoints |
| `CheckpointInstance` | Active checkpoint location/data |
| `RoadCheckpoint` | Visual checkpoint in world |

### Patrol System
| Class | Description |
|-------|-------------|
| `PatrolInstance` | Active foot patrol route |
| `VehiclePatrolInstance` | Active vehicle patrol route |
| `SentryInstance` | Static guard post |
| `SentryLocation` | Sentry position data |

### Law Activity Settings
`LawActivitySettings` — configures law enforcement activity levels, spawn rates, and patrol density.

---

## Usage Patterns

### Checking if player is wanted
```csharp
Player player = Player.Local;
if (player.CrimeData.CurrentPursuitLevel != PlayerCrimeData.EPursuitLevel.None)
{
    // Player is being pursued
}
```

### Finding nearest officer
```csharp
PoliceOfficer nearest = PoliceOfficer.GetNearestOfficer(position, out float dist);
```

### Triggering police dispatch
```csharp
Singleton<LawManager>.Instance.PoliceCalled(targetPlayer, new Assault());
```

### Blocking police from noticing specific players
```csharp
if (InstanceFinder.IsServer)
{
    officer.SetIgnorePlayers(true); // This officer ignores all players
}
```

## S1Toolkit API

Instead of raw Il2Cpp classes, the [S1Toolkit API](../api/api-reference.md) can be used:

| Method | Description |
|---|---|---|
| `Api.Police.GetPursuitLevel()` | Get pursuit level |
| `Api.Police.ClearWarrant()` | Clear warrant |
| `Api.Police.Escalate()` | Escalate pursuit |
| `Api.Police.Deescalate()` | De-escalate pursuit |
| `Api.Police.IsWanted()` | Check if wanted |

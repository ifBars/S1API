---
title: "Vehicle System"
description: "Reference for the vehicle system — LandVehicle, VehicleManager, VehicleSeat, and driving mechanics"
status: "stable"
version: "2.0.0"
reviewed: "2026-07-03"
applies_to: "Schedule I v0.4.3+"
---

# Vehicle System

## LandVehicle (`Il2CppScheduleOne.Vehicles.LandVehicle`)
`NetworkBehaviour`, IGUIDRegisterable, ISaveable, IWeatherEntity. Base class for all drivable vehicles.

### Constants
| Constant | Value | Description |
|----------|-------|-------------|
| `KINEMATIC_THRESHOLD_DISTANCE` | 30f | Distance for kinematic mode |
| `MAX_TURNOVER_SPEED` | 5f | Max speed for turnover recovery |
| `TURNOVER_FORCE` | 8f | Flip force |
| `MaxImpactDamage` | 120f | Max damage from collision |
| `MaxImpactDamageSpeed` | 100f | Speed for max damage |

### Properties
| Property | Type | Description |
|----------|------|-------------|
| `vehicleName` | `string` | Display name |
| `vehicleCode` | `string` | Code identifier |
| `vehiclePrice` | `float` | Purchase price |
| `UseHumanoidCollider` | `bool` | Human collision enabled |
| `SpawnAsPlayerOwned` | `bool` | Ownership on spawn |

### Fields
| Field | Type | Description |
|-------|------|-------------|
| `vehicleModel` | `GameObject` | Visual model |
| `driveWheels` | `WheelCollider[]` | Powered wheels |
| `Seats` | `VehicleSeat[]` | All seats |

### Methods
| Method | Description |
|--------|-------------|
| `EnterVehicle(LandVehicle vehicle)` | Enter this vehicle |
| `ExitVehicle()` | Exit and leave |
| `DestroyVehicle()` | Remove from world |
| `SetPlayerOwner(Player)` | Set ownership |

---

## VehicleSeat (`Il2CppScheduleOne.Vehicles.VehicleSeat`)
Seat within a vehicle.

---

## VehicleCamera (`Il2CppScheduleOne.Vehicles.VehicleCamera`)
Camera controller when driving.

---

## VehicleManager (`Il2CppScheduleOne.Vehicles.VehicleManager`)
`NetworkSingleton<VehicleManager>`, ISaveable. Manages all vehicles.

### Fields
| Field | Type | Description |
|-------|------|-------------|
| `AllVehicles` | `List<LandVehicle>` | All vehicles in world |
| `VehiclePrefabs` | `List<LandVehicle>` | Available vehicle prefabs |
| `PlayerOwnedVehicles` | `List<LandVehicle>` | Vehicles owned by players |

### Methods
| Method | Description |
|--------|-------------|
| `SpawnVehicle(LandVehicle prefab, Vector3 pos, Quaternion rot)` | Spawn new vehicle |
| `RegisterVehicle(LandVehicle)` | Register vehicle in manager |
| `DeregisterVehicle(LandVehicle)` | Remove from tracking |

---

## Vehicle Subsystems

### AI / Driving
| Class | Location | Purpose |
|-------|----------|---------|
| `VehicleAI` | `Vehicles/AI/` | Autonomous driving |
| `VehiclePathfinding` | `Vehicles/AI/` | Route finding |

### Modification
| Class | Location | Purpose |
|-------|----------|---------|
| `VehicleModification` | `Vehicles/Modification/` | Vehicle upgrades |
| `VehicleColor` | `Vehicles/` | Color customization |

### Audio
| Class | Location | Purpose |
|-------|----------|---------|
| `VehicleEngineSound` | `Vehicles/Sound/` | Engine audio |
| `VehicleFX` | `Vehicles/` | Visual effects |

### Physics
| Class | Purpose |
|-------|---------|
| `Wheel` | Wheel collider logic |
| `VehicleAxle` | Axle/steering setup |
| `VehicleHumanoidCollider` | Pedestrian collision |
| `ObstructionDetector` | Obstacle detection |

### Controls
| Class | Purpose |
|-------|---------|
| `VehicleLights` | Headlights, brake lights |
| `VehicleRecoveryPoint` | Flip recovery |

### Other
| Class | Description |
|-------|-------------|
| `ParkData` | Parking configuration |
| `SpeedZone` | Speed limit zone |
| `Shitbox` | Beater car variant |
| `LoanSharkCarVisuals` | Loan shark car marking |
| `PlayerPusher` | Player nudging from vehicles |

---

## EParkingAlignment Enum
Defines parking alignment modes.

---

## Usage Pattern
```csharp
// Get nearest vehicle
LandVehicle nearest = null;
float nearestDist = float.MaxValue;
foreach (var v in Singleton<VehicleManager>.Instance.AllVehicles)
{
    float dist = Vector3.Distance(pos, v.transform.position);
    if (dist < nearestDist)
    {
        nearestDist = dist;
        nearest = v;
    }
}

// Check if player owned
bool isMine = Singleton<VehicleManager>.Instance.PlayerOwnedVehicles.Contains(vehicle);
```

## S1Toolkit API

Instead of raw Il2Cpp classes, the [S1Toolkit API](../api/api-reference.md) can be used:

| Method | Description |
|---|---|---|
| `Api.Vehicle.Spawn()` | Spawn vehicle |
| `Api.Vehicle.Enter()` | Enter vehicle |
| `Api.Vehicle.Exit()` | Exit vehicle |
| `Api.Vehicle.GetOwnedVehicles()` | Get owned vehicles |
| `Api.Vehicle.GetSpeed()` | Get speed |

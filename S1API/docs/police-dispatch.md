# Police Dispatch and Wanted Levels

The police dispatch system allows you to call police on players, manage wanted levels, and control pursuit behavior. This guide covers dispatching officers, setting wanted levels, and checking player status.

## Calling Police

Dispatch police officers to pursue a player for a crime:

```csharp
using S1API.Entities;
using S1API.Law;

// Call police on a player
Player player = Player.Local;
LawManager.CallPolice(player);
```

**Note**: This method will not dispatch police during tutorial mode. Officers will be sent from the nearest police station.

## Managing Wanted Levels

### Getting and Setting Wanted Levels

```csharp
// Get current wanted level
PursuitLevel level = LawManager.GetWantedLevel(player);

// Set wanted level
LawManager.SetWantedLevel(player, PursuitLevel.Arresting);

// Clear wanted level
LawManager.ClearWantedLevel(player);
```

### Escalating and De-escalating

```csharp
// Escalate wanted level (increases by one level)
LawManager.EscalateWantedLevel(player);

// De-escalate wanted level (decreases by one level)
LawManager.DeescalateWantedLevel(player);
```

## Pursuit Levels

The `PursuitLevel` enum represents the severity of police response:

- `None` - No wanted level, not pursued
- `Investigating` - Police are investigating, low threat response
- `Arresting` - Police attempting arrest, non-lethal force
- `NonLethal` - Active pursuit, non-lethal weapons authorized
- `Lethal` - Lethal force authorized

### Progression

Wanted levels progress in this order:
- `None` → `Investigating` → `Arresting` → `NonLethal` → `Lethal`

Escalation happens automatically over time if the player remains visible to police, or can be triggered manually.

## Checking Player Status

```csharp
// Check if player is wanted
bool isWanted = LawManager.IsPlayerWanted(player);

// Check if under investigation
bool investigating = LawManager.IsUnderInvestigation(player);

// Check if lethal force is authorized
bool lethalForce = LawManager.IsLethalForceAuthorized(player);
```

## Active Officer Count

Get the total number of active police officers currently in the game world:

```csharp
int activeOfficers = LawManager.ActiveOfficerCount;
```

This is useful for checking officer availability before enabling checkpoints or dispatching additional units.

## Constants

### Police Dispatch

```csharp
// Default number of officers dispatched
int dispatchCount = LawManager.DispatchOfficerCount;  // 2 officers

// Distance threshold for vehicle use
float vehicleThreshold = LawManager.DispatchVehicleUseThreshold;  // 25 units
```

### Search Times by Pursuit Level

```csharp
float searchInvestigating = LawManager.SearchTimeInvestigating;  // 60 seconds
float searchArresting = LawManager.SearchTimeArresting;  // 25 seconds
float searchNonLethal = LawManager.SearchTimeNonLethal;  // 30 seconds
float searchLethal = LawManager.SearchTimeLethal;  // 40 seconds
```

### Escalation Times

```csharp
float escalateArresting = LawManager.EscalationTimeArresting;  // 25 seconds
float escalateNonLethal = LawManager.EscalationTimeNonLethal;  // 120 seconds
```

## Example: Wanted Level Monitor

```csharp
using MelonLoader;
using S1API.Entities;
using S1API.Law;

public class WantedLevelMonitor : MelonMod
{
    private PursuitLevel _lastLevel = PursuitLevel.None;

    public override void OnUpdate()
    {
        Player player = Player.Local;
        if (player == null) return;

        PursuitLevel currentLevel = LawManager.GetWantedLevel(player);
        
        if (currentLevel != _lastLevel)
        {
            LoggerInstance.Msg($"Wanted level changed: {_lastLevel} → {currentLevel}");
            _lastLevel = currentLevel;

            // Handle level changes
            if (currentLevel == PursuitLevel.Lethal)
            {
                LoggerInstance.Warning("Lethal force authorized!");
            }
            else if (currentLevel == PursuitLevel.None)
            {
                LoggerInstance.Msg("Wanted level cleared");
            }
        }
    }
}
```

## Patrol Management

### Starting Foot Patrols

Start a foot patrol using an existing patrol route:

```csharp
using S1API.Law;

// Find a patrol route by name
FootPatrolRoute route = LawManager.FindFootPatrolRoute("East");
if (route != null)
{
    // Start a patrol with 3 officers
    PatrolGroup patrol = LawManager.StartFootPatrol(route, 3);
    if (patrol != null)
    {
        LoggerInstance.Msg($"Started foot patrol with {patrol.MemberCount} officers");
        LoggerInstance.Msg($"Route: {route.RouteName} ({route.WaypointCount} waypoints)");
    }
}

// Get all available routes
FootPatrolRoute[] allRoutes = LawManager.GetAllFootPatrolRoutes();
LoggerInstance.Msg($"Found {allRoutes.Length} foot patrol routes");
foreach (var r in allRoutes)
{
    LoggerInstance.Msg($"  - {r.RouteName}");
}
```

### Starting Vehicle Patrols

Start a vehicle patrol using an existing patrol route:

```csharp
using S1API.Law;

// Find a vehicle patrol route
VehiclePatrolRoute route = LawManager.FindVehiclePatrolRoute("Highway");
if (route != null)
{
    // Start a vehicle patrol (1 officer + vehicle)
    bool started = LawManager.StartVehiclePatrol(route);
    if (started)
    {
        LoggerInstance.Msg($"Started vehicle patrol on route: {route.RouteName}");
    }
}
```

## Example: Custom Police Dispatch

```csharp
using MelonLoader;
using S1API.Entities;
using S1API.Law;
using UnityEngine;

public class CustomDispatch : MelonMod
{
    public override void OnUpdate()
    {
        // Hotkey: Press F10 to call police on local player
        if (Input.GetKeyDown(KeyCode.F10))
        {
            Player player = Player.Local;
            if (player != null)
            {
                LawManager.CallPolice(player);
                LoggerInstance.Msg("Police called!");
            }
        }

        // Hotkey: Press F11 to clear wanted level
        if (Input.GetKeyDown(KeyCode.F11))
        {
            Player player = Player.Local;
            if (player != null)
            {
                LawManager.ClearWantedLevel(player);
                LoggerInstance.Msg("Wanted level cleared");
            }
        }

        // Hotkey: Press F12 to start a random foot patrol
        if (Input.GetKeyDown(KeyCode.F12))
        {
            var routes = LawManager.GetAllFootPatrolRoutes();
            if (routes.Length > 0)
            {
                var route = routes[Random.Range(0, routes.Length)];
                var patrol = LawManager.StartFootPatrol(route, 2);
                if (patrol != null)
                {
                    LoggerInstance.Msg($"Started patrol on route: {route.RouteName}");
                }
            }
        }
    }
}
```


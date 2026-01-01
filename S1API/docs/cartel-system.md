# Cartel System

S1API provides access to the game's Cartel system, allowing mods to track and respond to cartel relationship status changes, spawn and control cartel goons, and manage regional influence.

## Overview

The Cartel system in Schedule One manages the player's relationship with the in-game cartel organization. The cartel can be in different states (Unknown, Truced, Hostile, Defeated), and S1API provides a wrapper to easily access this status, subscribe to status change events, spawn goons, and control regional influence.

## Accessing the Cartel

The `Cartel` class is a singleton that wraps the game's internal cartel system:

```csharp
using S1API.Cartel;

// Access the current cartel instance
var cartel = Cartel.Instance;

if (cartel != null)
{
    // Check the current status
    CartelStatus status = cartel.Status;

    // Check how long the status has been active
    int hours = cartel.HoursSinceStatusChange;
}
```

## Cartel Status

The cartel can be in one of four states:

```csharp
public enum CartelStatus
{
    Unknown,    // Initial/undefined state
    Truced,     // Peaceful relationship
    Hostile,    // Antagonistic relationship
    Defeated    // Cartel has been defeated
}
```

## Changing Cartel Status

You can programmatically change the cartel's status using `SetStatus`:

```csharp
var cartel = Cartel.Instance;
if (cartel != null)
{
    // Make the cartel hostile
    cartel.SetStatus(CartelStatus.Hostile);

    // Set status without resetting the hours-since-change timer
    cartel.SetStatus(CartelStatus.Truced, resetTimer: false);
}
```

> **Note:** `SetStatus` is a server RPC that syncs to all clients in multiplayer.

## Listening for Status Changes

Subscribe to the `OnStatusChange` event to react when the cartel relationship changes:

```csharp
using S1API.Cartel;
using MelonLoader;

public class MyMod : MelonMod
{
    public override void OnInitializeMelon()
    {
        // Subscribe to status changes
        var cartel = Cartel.Instance;
        if (cartel != null)
        {
            cartel.OnStatusChange += OnCartelStatusChanged;
        }
    }

    private void OnCartelStatusChanged(CartelStatus oldStatus, CartelStatus newStatus)
    {
        MelonLogger.Msg($"Cartel status changed from {oldStatus} to {newStatus}");

        switch (newStatus)
        {
            case CartelStatus.Hostile:
                // Cartel is now hostile - prepare defenses
                break;
            case CartelStatus.Truced:
                // Peace has been restored
                break;
            case CartelStatus.Defeated:
                // Victory!
                break;
        }
    }
}
```

## Complete Example: Cartel Status Watcher

Here's a complete example that monitors cartel status and takes action:

```csharp
using S1API.Cartel;
using MelonLoader;

public class CartelStatusWatcher
{
    private CartelStatus? _lastKnownStatus;
    private bool _subscribed = false;

    public void Update()
    {
        var cartel = Cartel.Instance;

        // Cartel might not be available during scene transitions
        if (cartel == null)
        {
            _subscribed = false;
            _lastKnownStatus = null;
            return;
        }

        // Subscribe to events if we haven't yet
        if (!_subscribed)
        {
            cartel.OnStatusChange += OnStatusChanged;
            _subscribed = true;
        }

        // Check if status changed (backup detection)
        if (_lastKnownStatus != cartel.Status)
        {
            if (_lastKnownStatus.HasValue)
            {
                OnStatusChanged(_lastKnownStatus.Value, cartel.Status);
            }
            _lastKnownStatus = cartel.Status;
        }
    }

    private void OnStatusChanged(CartelStatus oldStatus, CartelStatus newStatus)
    {
        MelonLogger.Msg($"Cartel: {oldStatus} → {newStatus}");

        switch (newStatus)
        {
            case CartelStatus.Hostile:
                OnCartelBecameHostile();
                break;
            case CartelStatus.Truced:
                OnCartelBecameTruced();
                break;
            case CartelStatus.Defeated:
                OnCartelDefeated();
                break;
        }
    }

    private void OnCartelBecameHostile()
    {
        MelonLogger.Msg("Cartel is now hostile! Prepare for attacks.");
        // Spawn additional NPCs, send warning messages, etc.
    }

    private void OnCartelBecameTruced()
    {
        MelonLogger.Msg("Peace with the cartel has been restored.");
        // Despawn hostile NPCs, send peace messages, etc.
    }

    private void OnCartelDefeated()
    {
        MelonLogger.Msg("The cartel has been defeated!");
        // Trigger victory events, rewards, etc.
    }
}

// Usage in your MelonMod
public class MyMod : MelonMod
{
    private CartelStatusWatcher _watcher = new CartelStatusWatcher();

    public override void OnUpdate()
    {
        _watcher.Update();
    }
}
```

## Example: NPC Reactions to Cartel Status

Create NPCs that react to cartel status changes:

```csharp
using S1API.Entities;
using S1API.Cartel;
using UnityEngine;

public class CartelReactiveNPC : NPC
{
    public override bool IsPhysical => true;

    protected override void ConfigurePrefab(NPCPrefabBuilder builder)
    {
        builder.WithIdentity("cartel_reactive_npc", "Rico", "Martinez")
            .WithSpawnPosition(new Vector3(0, 0, 0))
            .WithAppearanceDefaults(av =>
            {
                av.Gender = 0.0f;
                av.Height = 1.0f;
            });
    }

    protected override void OnCreated()
    {
        base.OnCreated();
        Appearance.Build();

        // Subscribe to cartel changes
        var cartel = Cartel.Instance;
        if (cartel != null)
        {
            cartel.OnStatusChange += OnCartelStatusChanged;

            // Check initial status
            UpdateBehaviorForStatus(cartel.Status);
        }

        Schedule.Enable();
    }

    protected override void OnDestroyed()
    {
        // Clean up event subscription
        var cartel = Cartel.Instance;
        if (cartel != null)
        {
            cartel.OnStatusChange -= OnCartelStatusChanged;
        }

        base.OnDestroyed();
    }

    private void OnCartelStatusChanged(CartelStatus oldStatus, CartelStatus newStatus)
    {
        UpdateBehaviorForStatus(newStatus);
    }

    private void UpdateBehaviorForStatus(CartelStatus status)
    {
        switch (status)
        {
            case CartelStatus.Hostile:
                // Make NPC aggressive or fearful
                Aggressiveness = 5f;
                SendTextMessage("Things are getting dangerous with the cartel...");
                break;

            case CartelStatus.Truced:
                // Make NPC calm
                Aggressiveness = 1f;
                SendTextMessage("Finally, some peace with the cartel.");
                break;

            case CartelStatus.Defeated:
                // Make NPC celebratory
                SendTextMessage("Did you hear? The cartel's been taken down!");
                break;
        }
    }
}
```

## Spawning Cartel Goons

S1API provides access to the cartel's goon pool for spawning and controlling cartel enemies.

### GoonManager

Access the goon manager through `Cartel.GoonPool`:

```csharp
using S1API.Cartel;
using UnityEngine;

var cartel = Cartel.Instance;
if (cartel?.GoonPool != null)
{
    var goonPool = cartel.GoonPool;

    // Check available goons
    int available = goonPool.AvailableGoonCount;

    // Spawn a single goon at a position
    CartelGoon goon = goonPool.SpawnGoon(new Vector3(10, 0, 20));

    // Spawn multiple goons
    List<CartelGoon> goons = goonPool.SpawnGoons(3);

    // Spawn goons at specific positions
    Vector3[] positions = new Vector3[]
    {
        new Vector3(10, 0, 20),
        new Vector3(12, 0, 22),
        new Vector3(14, 0, 24)
    };
    List<CartelGoon> guards = goonPool.SpawnGoonsAtPositions(positions);
}
```

### CartelGoon

Each spawned goon can be individually controlled:

```csharp
// Attack the player
goon.AttackPlayer();

// Attack a specific entity
goon.AttackEntity(someEntity);

// Teleport the goon
goon.WarpTo(new Vector3(50, 0, 50));

// Check goon state
bool alive = goon.IsConscious;
bool dead = goon.IsDead;
Vector3 pos = goon.Position;

// Set weapon (null for fists)
goon.SetDefaultWeapon("pistol");
goon.SetDefaultWeapon(null); // Fists only

// Remove the goon
goon.Despawn();
```

### Example: Guard Spawning

```csharp
using S1API.Cartel;
using UnityEngine;
using System.Collections.Generic;

public class GuardSpawner
{
    private List<CartelGoon> _guards = new List<CartelGoon>();

    public void SpawnGuards(Vector3[] positions)
    {
        var cartel = Cartel.Instance;
        if (cartel?.GoonPool == null) return;

        _guards = cartel.GoonPool.SpawnGoonsAtPositions(positions);

        foreach (var guard in _guards)
        {
            guard.SetDefaultWeapon(null); // Fists only
        }
    }

    public void AlertGuards()
    {
        foreach (var guard in _guards)
        {
            if (guard != null && guard.IsConscious)
            {
                guard.AttackPlayer();
            }
        }
    }

    public int RemainingGuards => _guards.Count(g => g != null && g.IsConscious);

    public void Cleanup()
    {
        foreach (var guard in _guards)
        {
            guard?.Despawn();
        }
        _guards.Clear();
    }
}
```

## Regional Influence

The cartel has influence levels (0.0 to 1.0) in each map region. Higher influence means stronger cartel presence.

### Accessing Influence

```csharp
using S1API.Cartel;
using S1API.Map;

var cartel = Cartel.Instance;
if (cartel?.Influence != null)
{
    var influence = cartel.Influence;

    // Get influence for a region (0.0 to 1.0)
    float downtownInfluence = influence.GetInfluence(Region.Downtown);

    // Change influence (positive = increase, negative = decrease)
    influence.ChangeInfluence(Region.Docks, 0.1f);   // +10%
    influence.ChangeInfluence(Region.Docks, -0.05f); // -5%
}
```

### Map Regions

```csharp
public enum Region
{
    Northtown,  // Starting area
    Westville,  // Western residential
    Downtown,   // Central business district
    Docks,      // Industrial harbor
    Suburbia,   // Suburban residential
    Uptown      // Wealthy neighborhood
}
```

### Influence Changed Event (Mono Only)

```csharp
// Note: This event is only available in Mono builds
cartel.Influence.OnInfluenceChanged += (region, oldValue, newValue) =>
{
    MelonLogger.Msg($"{region} influence: {oldValue:P0} -> {newValue:P0}");
};
```

### Example: Influence Monitor

```csharp
using S1API.Cartel;
using S1API.Map;
using MelonLoader;

public class InfluenceMonitor
{
    public void LogAllInfluence()
    {
        var cartel = Cartel.Instance;
        if (cartel?.Influence == null) return;

        MelonLogger.Msg("=== Cartel Influence ===");
        foreach (Region region in Enum.GetValues(typeof(Region)))
        {
            float influence = cartel.Influence.GetInfluence(region);
            MelonLogger.Msg($"  {region}: {influence:P0}");
        }
    }

    public void WeakenCartelInRegion(Region region)
    {
        var cartel = Cartel.Instance;
        if (cartel?.Influence == null) return;

        float before = cartel.Influence.GetInfluence(region);
        cartel.Influence.ChangeInfluence(region, -0.2f);
        float after = cartel.Influence.GetInfluence(region);

        MelonLogger.Msg($"Weakened cartel in {region}: {before:P0} -> {after:P0}");
    }
}
```

## Best Practices

1. **Null Checking**: Always check if `Cartel.Instance` is null before accessing it, especially during scene transitions

2. **Event Subscription**: Subscribe to events in `OnCreated()` and unsubscribe in `OnDestroyed()` for NPCs

3. **Backup Polling**: The example `CartelStatusWatcher` uses both events and polling to catch status changes reliably

4. **Scene Management**: The `Cartel.Instance` may be null between scenes, so handle this gracefully

## Technical Notes

- The `Cartel` class uses a caching system to avoid creating new wrapper instances unnecessarily
- Events are properly converted between Mono and IL2CPP using S1API's internal event handling
- Status changes are tracked by the game's internal systems - S1API only provides access to them
- `SetStatus` calls the game's `SetStatus_Server` RPC, which syncs across multiplayer clients
- Goons are spawned from a finite pool managed by the game - check `AvailableGoonCount` before spawning
- Regional influence is persisted with the save game

## See Also

- [More-NPCs Example](https://github.com/ifBars/S1API/tree/main/More-NPCs) - Contains a working `CartelStatusWatcher`
- [Custom NPCs](custom-npcs.md) - For creating NPCs that react to cartel status
- <xref:S1API.Cartel> - Cartel API Reference

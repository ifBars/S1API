# Cartel System

S1API provides access to the game's Cartel system, allowing mods to track and respond to cartel relationship status changes.

## Overview

The Cartel system in Schedule One manages the player's relationship with the in-game cartel organization. The cartel can be in different states (Unknown, Truced, Hostile, Defeated), and S1API provides a wrapper to easily access this status and subscribe to status change events.

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

## Best Practices

1. **Null Checking**: Always check if `Cartel.Instance` is null before accessing it, especially during scene transitions

2. **Event Subscription**: Subscribe to events in `OnCreated()` and unsubscribe in `OnDestroyed()` for NPCs

3. **Backup Polling**: The example `CartelStatusWatcher` uses both events and polling to catch status changes reliably

4. **Scene Management**: The `Cartel.Instance` may be null between scenes, so handle this gracefully

## Technical Notes

- The `Cartel` class uses a caching system to avoid creating new wrapper instances unnecessarily
- Events are properly converted between Mono and IL2CPP using S1API's internal event handling
- Status changes are tracked by the game's internal systems - S1API only provides access to them

## See Also

- [More-NPCs Example](https://github.com/ifBars/S1API/tree/main/More-NPCs) - Contains a working `CartelStatusWatcher`
- [Custom NPCs](custom-npcs.md) - For creating NPCs that react to cartel status
- <xref:S1API.Cartel> - Cartel API Reference

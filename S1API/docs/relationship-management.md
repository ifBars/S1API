# Relationship Management

The relationship system manages NPC social connections, unlock states, and relationship levels with the player and other NPCs.

## Table of Contents

1. [Overview](#overview)
2. [Relationship Configuration](#relationship-configuration)
3. [Relationship Properties](#relationship-properties)
4. [Relationship Events](#relationship-events)
5. [Runtime Relationship Management](#runtime-relationship-management)
6. [Relationship Examples](#relationship-examples)
7. [Best Practices](#best-practices)

## Overview

The relationship system controls how NPCs interact with the player and other NPCs, including unlock states, relationship levels, and social connections.

```csharp
protected override void ConfigurePrefab(NPCPrefabBuilder builder)
{
    builder.WithRelationshipDefaults(r => {
        // Starting relationship level (0-5)
        r.WithDelta(1.5f);
        
        // Unlock settings
        r.SetUnlocked(false)
         .SetUnlockType(NPCRelationship.UnlockType.DirectApproach);
        
        // Connection to other NPCs
        r.WithConnectionsById("kyle_cooley", "ludwig_meyer", "austin_steiner");
    });
}
```

## Relationship Configuration

### Relationship Defaults

Configure relationship parameters using the `NPCRelationshipDataBuilder`:

```csharp
builder.WithRelationshipDefaults(r => {
    // Configuration here
});
```

**Important**: Relationship configuration must be done in `ConfigurePrefab` for proper save/load behavior.

## Relationship Properties

### Starting Relationship Level

Set the initial relationship level:

```csharp
r.WithDelta(1.5f);    // Acquaintance level
r.WithDelta(2.5f);    // Friend level
r.WithDelta(4.0f);    // Good friend level
```

**Relationship Levels:**
- **0.0**: Stranger
- **1.0**: Acquaintance
- **2.5**: Friend
- **4.0**: Good friend
- **5.0**: Best friend

### Normalized Relationship

Set relationship using a 0-1 value:

```csharp
r.WithNormalized(0.3f); // 30% of max relationship (1.5/5.0)
r.WithNormalized(0.8f); // 80% of max relationship (4.0/5.0)
```

### Unlock Settings

Configure how the NPC can be unlocked:

```csharp
r.SetUnlocked(false)  // Must be unlocked
  .SetUnlockType(NPCRelationship.UnlockType.DirectApproach);

r.SetUnlocked(true);  // Already unlocked
```

**Unlock Types:**
- **DirectApproach**: Can be unlocked by talking to them directly
- **Recommendation**: Must be recommended by another NPC

### Connections

Link NPCs together:

```csharp
// By ID
r.WithConnectionsById("kyle_cooley", "ludwig_meyer", "austin_steiner");

// By reference (if NPCs are available)
r.WithConnections(Get<KyleCooley>(), Get<LudwigMeyer>(), Get<AustinSteiner>());
```

**Connection Benefits:**
- Unlocking one NPC can unlock connected NPCs
- Connected NPCs can recommend each other
- Social network effects

## Relationship Events

### Event Subscription

Subscribe to relationship events in `OnCreated`:

```csharp
protected override void OnCreated()
{
    base.OnCreated();
    
    // Subscribe to relationship changes
    Relationship.OnChanged(delta => {
        Debug.Log($"Relationship changed by {delta}");
        if (delta > 0) {
            SendTextMessage("I appreciate your help!");
        } else if (delta < 0) {
            SendTextMessage("I'm not happy about this...");
        }
    });
    
    // Subscribe to unlock events
    Relationship.OnUnlocked((type, notify) => {
        Debug.Log($"NPC unlocked via {type}, notify: {notify}");
        SendTextMessage("Hey, I'm available now!");
    });
}
```

### Available Events

- **OnChanged**: Relationship level changed
- **OnUnlocked**: NPC was unlocked
- **OnConnectionUnlocked**: A connected NPC was unlocked

## Runtime Relationship Management

### Basic Relationship Actions

**Note**: Relationship data configuration should only be done in `ConfigurePrefab` via `WithRelationshipDefaults`. Runtime relationship management is limited to events and basic actions:

```csharp
protected override void OnCreated()
{
    base.OnCreated();
    
    // Basic relationship actions
    Relationship.Add(1.0f); // Increase relationship
    Relationship.Unlock(NPCRelationship.UnlockType.DirectApproach);
    Relationship.UnlockConnections();
}
```

### Relationship State Queries

Check relationship state:

```csharp
// Check if NPC is known
bool isKnown = Relationship.IsKnown;

// Check if NPC is mutually known
bool isMutuallyKnown = Relationship.IsMutuallyKnown;

// Get connection IDs
List<string> connections = Relationship.ConnectionIDs;

// Get current relationship level
float currentDelta = Relationship.Delta;
```

### Relationship Actions

```csharp
// Increase relationship
Relationship.Add(0.5f);  // Small increase
Relationship.Add(1.0f);  // Medium increase
Relationship.Add(2.0f);  // Large increase

// Decrease relationship
Relationship.Add(-0.5f); // Small decrease
Relationship.Add(-1.0f); // Medium decrease

// Unlock NPC
Relationship.Unlock(NPCRelationship.UnlockType.DirectApproach);

// Unlock connected NPCs
Relationship.UnlockConnections();
```

## Relationship Examples

### Basic Relationship

```csharp
protected override void ConfigurePrefab(NPCPrefabBuilder builder)
{
    builder.WithRelationshipDefaults(r => {
        r.WithDelta(1.0f)  // Acquaintance
         .SetUnlocked(false)
         .SetUnlockType(NPCRelationship.UnlockType.DirectApproach);
    });
}
```

### Connected NPCs

```csharp
protected override void ConfigurePrefab(NPCPrefabBuilder builder)
{
    builder.WithRelationshipDefaults(r => {
        r.WithDelta(2.0f)  // Friend level
         .SetUnlocked(false)
         .SetUnlockType(NPCRelationship.UnlockType.Recommendation)
         .WithConnectionsById("kyle_cooley", "ludwig_meyer");
    });
}
```

### Pre-Unlocked NPC

```csharp
protected override void ConfigurePrefab(NPCPrefabBuilder builder)
{
    builder.WithRelationshipDefaults(r => {
        r.WithDelta(3.0f)  // Good friend
         .SetUnlocked(true)  // Already unlocked
         .WithConnectionsById("shopkeeper_alex", "informant_mike");
    });
}
```

### Event-Driven Relationship

```csharp
protected override void OnCreated()
{
    base.OnCreated();
    
    // Set up relationship events
    Relationship.OnChanged(delta => {
        if (delta > 0) {
            SendTextMessage("Thanks for your help!");
            if (Relationship.Delta >= 3.0f) {
                SendTextMessage("You're a good friend!");
            }
        } else if (delta < 0) {
            SendTextMessage("I'm disappointed...");
            if (Relationship.Delta <= 1.0f) {
                SendTextMessage("I don't trust you anymore.");
            }
        }
    });
    
    Relationship.OnUnlocked((type, notify) => {
        SendTextMessage("Hey, I'm available now!");
        if (type == NPCRelationship.UnlockType.Recommendation) {
            SendTextMessage("Thanks for the recommendation!");
        }
    });
}
```

### Dynamic Relationship Management

```csharp
protected override void OnCreated()
{
    base.OnCreated();
    
    // Check relationship and respond accordingly
    if (Relationship.Delta >= 4.0f) {
        SendTextMessage("You're one of my best friends!");
        // Unlock special dialogue or features
    } else if (Relationship.Delta >= 2.0f) {
        SendTextMessage("I consider you a friend.");
    } else if (Relationship.Delta >= 1.0f) {
        SendTextMessage("I know you, but we're not close.");
    } else {
        SendTextMessage("I don't really know you.");
    }
    
    // Unlock connections when relationship is high enough
    if (Relationship.Delta >= 3.0f) {
        Relationship.UnlockConnections();
        SendTextMessage("I can introduce you to some of my friends!");
    }
}
```

### Relationship-Based Dialogue

```csharp
protected override void OnCreated()
{
    base.OnCreated();
    
    // Set up relationship-based dialogue
    Dialogue.BuildAndRegisterContainer("RelationshipDialogue", c => {
        c.AddNode("ENTRY", () => {
            var relationship = Relationship.Delta;
            if (relationship >= 4.0f) {
                return "Hey best friend! What's up?";
            } else if (relationship >= 2.0f) {
                return "Hello friend! How are you?";
            } else if (relationship >= 1.0f) {
                return "Hi there. Do I know you?";
            } else {
                return "Who are you?";
            }
        }, ch => {
            ch.Add("GREET", "Hello!", "RESPONSE");
        });
        
        c.AddNode("RESPONSE", () => {
            var relationship = Relationship.Delta;
            if (relationship >= 3.0f) {
                return "I'm always happy to see you!";
            } else {
                return "Nice to meet you.";
            }
        });
    });
    
    Dialogue.UseContainerOnInteract("RelationshipDialogue");
}
```

## Best Practices

### Do's

- **Configure relationship data in `ConfigurePrefab`** - required for save/load compatibility
- **Use appropriate relationship levels** for the NPC's role and importance
- **Set meaningful connections** between related NPCs
- **Subscribe to relationship events** to provide feedback and interaction
- **Test relationship behavior** with different levels and unlock types

### Don'ts

- **Don't modify relationship data at runtime** (except through proper APIs)
- **Don't forget to call `WithRelationshipDefaults()`** for relationship configuration
- **Don't use extreme relationship values** unless intentional
- **Don't create circular connections** between NPCs

### Error Handling

Wrap relationship configuration in try-catch blocks:

```csharp
protected override void ConfigurePrefab(NPCPrefabBuilder builder)
{
    try
    {
        builder.WithRelationshipDefaults(r => {
            // Relationship configuration
        });
    }
    catch (Exception ex)
    {
        MelonLogger.Error($"Failed to configure relationship for {GetType().Name}: {ex.Message}");
    }
}
```

### Performance Considerations

- **Keep relationship configurations reasonable** - overly complex configurations can impact performance
- **Use efficient event handlers** - avoid expensive operations in event callbacks
- **Test with multiple NPCs** - ensure relationship behavior works well together
- **Monitor relationship performance** in multiplayer environments

## Next Steps

Now that you understand relationship management, explore:

- **[Customer Behavior](customer-behavior.md)** - Customer system details
- **[Scheduling System](scheduling-system.md)** - NPC schedules and activities
- **[Runtime Management](runtime-management.md)** - NPC lifecycle and properties

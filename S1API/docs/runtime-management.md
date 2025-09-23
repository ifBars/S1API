# Runtime Management

This guide covers NPC lifecycle, runtime properties, and management during gameplay.

## Table of Contents

1. [Overview](#overview)
2. [NPC Lifecycle](#npc-lifecycle)
3. [Basic Properties](#basic-properties)
4. [Health Management](#health-management)
5. [Movement Control](#movement-control)
6. [Messaging System](#messaging-system)
7. [Component Access](#component-access)
8. [Runtime Examples](#runtime-examples)
9. [Best Practices](#best-practices)

## Overview

Runtime management covers how NPCs behave and can be controlled during gameplay, including lifecycle events, properties, and interactions.

## NPC Lifecycle

### Lifecycle Methods

NPCs go through several lifecycle phases:

```csharp
public sealed class MyCustomNPC : NPC
{
    protected override void OnCreated()
    {
        base.OnCreated();
        
        // NPC is fully initialized and spawned
        Debug.Log($"NPC {FullName} created at {Position}");
        
        // Set up initial state
        Region = Region.Northtown;
        Aggressiveness = 5f;
    }
    
    protected override void OnLoaded()
    {
        // Called when NPC is loaded from save
        base.OnLoaded();
        Debug.Log($"NPC {FullName} loaded from save");
    }
    
    protected override void OnResponseLoaded(Response response)
    {
        // Called for each saved text message response
        if (response.ID == "DEAL_RESPONSE")
        {
            response.OnSelected(() => {
                // Re-attach callback to loaded response
            });
        }
    }
}
```

### Lifecycle Events

- **OnCreated**: NPC is fully initialized and spawned
- **OnLoaded**: NPC is loaded from save file
- **OnResponseLoaded**: Text message responses are loaded from save

## Basic Properties

### Identity Properties

```csharp
// Basic identity
string name = FirstName;
string fullName = FullName;
string id = ID;
Sprite icon = Icon;

// Display names
Debug.Log($"NPC: {FullName} (ID: {ID})");
```

### Position and Transform

```csharp
// World position
Vector3 position = Position;
Position = new Vector3(10, 0, 10); // Set position

// Transform access
Transform transform = Transform;
// Note: Don't modify transform properties directly
```

### State Properties

```csharp
// Consciousness and awareness
bool isConscious = IsConscious;
bool isPanicking = IsPanicking;
bool isUnsettled = IsUnsettled;
bool isVisible = IsVisible;

// Location states
bool isInBuilding = IsInBuilding;
bool isInVehicle = IsInVehicle;
```

### Behavioral Properties

```csharp
// Aggression level
float aggressiveness = Aggressiveness;
Aggressiveness = 3f; // Set aggression (0-10)

// Region association
Region region = Region;
Region = Region.Northtown; // Set region
```

## Health Management

### Health Properties

```csharp
// Health status
float health = CurrentHealth;
float maxHealth = MaxHealth;
bool isDead = IsDead;
bool isInvincible = IsInvincible;
```

### Health Actions

```csharp
// Damage and healing
Damage(25); // Deal 25 damage
Heal(50);   // Heal 50 health

// Life and death
Kill();     // Kill the NPC
Revive();   // Revive the NPC

// Panic control
Panic();    // Make NPC panic
StopPanicking(); // Stop panic
```

### Health Events

```csharp
protected override void OnCreated()
{
    base.OnCreated();
    
    // Subscribe to health events
    OnHealthChanged += (oldHealth, newHealth) => {
        Debug.Log($"Health changed from {oldHealth} to {newHealth}");
        if (newHealth <= 0) {
            Debug.Log($"{FullName} has died!");
        }
    };
}
```

## Movement Control

### Basic Movement

```csharp
// Move to position
Goto(new Vector3(10, 0, 10));

// Set scale
Scale = 1.2f; // Make NPC 20% larger
```

### Movement Properties

```csharp
// Access movement component
var movement = Movement;

// Movement state
bool isMoving = movement.IsMoving;
Vector3 destination = movement.Destination;
float speed = movement.Speed;
```

## Messaging System

### Sending Messages

```csharp
// Simple text message
SendTextMessage("Hello! How are you today?");

// Message with responses
SendTextMessage("Want to make a deal?", new[] {
    new Response("YES_DEAL", "Yes, let's make a deal!"),
    new Response("NO_DEAL", "Not interested")
}, responseDelay: 2f);
```

### Message Responses

```csharp
// Set up response callbacks
var response = new Response("DEAL_RESPONSE", "Let's make a deal!");
response.OnSelected(() => {
    Debug.Log("Player chose to make a deal!");
    // Handle deal logic
});
```

### Conversation Settings

```csharp
// Set conversation visibility
ConversationCanBeHidden = true;
```

## Component Access

### Available Components

```csharp
// Appearance system
var appearance = Appearance;
appearance.Set<CustomizationFields.Height>(1.1f);
appearance.Build();

// Dialogue system
var dialogue = Dialogue;
dialogue.BuildAndRegisterContainer("MyDialogue", c => {
    // Dialogue configuration
});

// Schedule system
var schedule = Schedule;
schedule.Enable();
schedule.InitializeActions();

// Customer system
var customer = Customer;
customer.ForceDealOffer();

// Relationship system
var relationship = Relationship;
relationship.Add(1.0f);

// Inventory system
var inventory = Inventory;
bool hasItem = inventory.HasItem("SpecialItem");

// Movement system
var movement = Movement;
movement.Goto(new Vector3(10, 0, 10));
```

### Component Events

```csharp
protected override void OnCreated()
{
    base.OnCreated();
    
    // Subscribe to component events
    Customer.OnDealCompleted(() => {
        Debug.Log("Deal completed!");
    });
    
    Relationship.OnChanged(delta => {
        Debug.Log($"Relationship changed by {delta}");
    });
    
    OnInventoryChanged += () => {
        Debug.Log("Inventory changed!");
    };
}
```

## Runtime Examples

### Basic Runtime Management

```csharp
protected override void OnCreated()
{
    base.OnCreated();
    
    // Set basic properties
    Region = Region.Northtown;
    Aggressiveness = 3f;
    
    // Set up messaging
    SendTextMessage("Hello! I'm here to help.");
    
    // Enable systems
    Schedule.Enable();
    Schedule.InitializeActions();
    
    // Set up events
    Customer.OnDealCompleted(() => {
        SendTextMessage("Thanks for the business!");
        Relationship.Add(0.5f);
    });
}
```

### Dynamic Behavior

```csharp
protected override void OnCreated()
{
    base.OnCreated();
    
    // Set up dynamic behavior based on time
    var currentTime = GameTime.CurrentTime;
    if (currentTime.Hour >= 18) { // Evening
        SendTextMessage("Good evening! How can I help?");
        Aggressiveness = 2f; // Less aggressive in evening
    } else { // Daytime
        SendTextMessage("Good day! What do you need?");
        Aggressiveness = 4f; // More aggressive during day
    }
    
    // Set up health monitoring
    OnHealthChanged += (oldHealth, newHealth) => {
        if (newHealth < 50) {
            SendTextMessage("I'm not feeling well...");
            Aggressiveness = 1f; // Less aggressive when injured
        }
    };
}
```

### Event-Driven Interactions

```csharp
protected override void OnCreated()
{
    base.OnCreated();
    
    // Set up event-driven interactions
    Customer.OnUnlocked(() => {
        SendTextMessage("Hey, I'm looking for some products...");
        Relationship.Add(0.5f);
    });
    
    Customer.OnDealCompleted(() => {
        SendTextMessage("Great doing business with you!");
        Relationship.Add(1.0f);
        
        // Unlock connections if relationship is high enough
        if (Relationship.Delta >= 3.0f) {
            Relationship.UnlockConnections();
            SendTextMessage("I can introduce you to some friends!");
        }
    });
    
    Relationship.OnChanged(delta => {
        if (delta > 0) {
            SendTextMessage("I appreciate your help!");
        } else if (delta < 0) {
            SendTextMessage("I'm not happy about this...");
        }
    });
}
```

### Conditional Behavior

```csharp
protected override void OnCreated()
{
    base.OnCreated();
    
    // Set up conditional behavior
    if (IsPhysical) {
        // Physical NPC behavior
        Schedule.Enable();
        Schedule.InitializeActions();
        
        // Set up movement-based interactions
        OnPositionChanged += (oldPos, newPos) => {
            if (Vector3.Distance(newPos, Player.Position) < 5f) {
                SendTextMessage("Hey there!");
            }
        };
    } else {
        // Non-physical NPC behavior
        SendTextMessage("I'm here if you need me.");
        
        // Set up periodic messages
        MelonCoroutines.Start(PeriodicMessages());
    }
}

private IEnumerator PeriodicMessages()
{
    while (true) {
        yield return new WaitForSeconds(300f); // 5 minutes
        
        string[] messages = {
            "Hey, I heard some interesting news...",
            "The police are getting suspicious in the docks area.",
            "New shipment arrived at the warehouse yesterday."
        };
        
        string message = messages[UnityEngine.Random.Range(0, messages.Length)];
        SendTextMessage(message);
    }
}
```

## Best Practices

### Do's

- **Set up runtime behavior in `OnCreated`** - this is where runtime initialization should happen
- **Subscribe to events** to provide dynamic feedback and interaction
- **Use appropriate property values** for the NPC's role and behavior
- **Handle both physical and non-physical NPCs** appropriately
- **Test runtime behavior** with different game states and conditions

### Don'ts

- **Don't modify prefab configuration at runtime** - use proper runtime APIs instead
- **Don't forget to call base methods** in lifecycle overrides
- **Don't use extreme property values** unless intentional
- **Don't create infinite loops** in event handlers or coroutines

### Error Handling

Wrap runtime code in try-catch blocks:

```csharp
protected override void OnCreated()
{
    base.OnCreated();
    
    try
    {
        // Runtime setup
        Region = Region.Northtown;
        Aggressiveness = 3f;
        
        // Set up events
        Customer.OnDealCompleted(() => {
            // Event handling
        });
    }
    catch (Exception ex)
    {
        MelonLogger.Error($"Failed to set up runtime behavior for {FullName}: {ex.Message}");
    }
}
```

### Performance Considerations

- **Keep event handlers efficient** - avoid expensive operations in callbacks
- **Use appropriate update frequencies** for periodic tasks
- **Test with multiple NPCs** - ensure runtime behavior scales well
- **Monitor runtime performance** in multiplayer environments

## Next Steps

Now that you understand runtime management, explore:

- **[Examples](examples.md)** - Complete working examples
- **[API Reference](api-reference.md)** - Detailed API documentation
- **[Troubleshooting](troubleshooting.md)** - Common issues and solutions

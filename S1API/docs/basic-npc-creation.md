# Basic NPC Creation

This guide covers the fundamental concepts and basic setup for creating custom NPCs in S1API.

## Table of Contents

1. [Creating Your First NPC](#creating-your-first-npc)
2. [NPC Class Structure](#npc-class-structure)
3. [Physical vs Non-Physical NPCs](#physical-vs-non-physical-npcs)
4. [Required Methods](#required-methods)
5. [Constructor Parameters](#constructor-parameters)
6. [Lifecycle Methods](#lifecycle-methods)
7. [Basic Example](#basic-example)

## Creating Your First NPC

To create a custom NPC, inherit from the `NPC` base class and implement the required methods:

```csharp
using S1API.Entities;
using UnityEngine;

public sealed class MyCustomNPC : NPC
{
    protected override bool IsPhysical => true; // Make NPC visible in world
    
    protected override void ConfigurePrefab(NPCPrefabBuilder builder)
    {
        // Configure NPC identity
        builder.WithIdentity(
            id: "my_custom_npc",
            firstName: "John",
            lastName: "Doe")
            .WithIcon(null); // Optional icon sprite
        
        // Configure NPC components and behavior
    }
    
    public MyCustomNPC() : base()
    {
    }
    
    protected override void OnCreated()
    {
        base.OnCreated();
        // Initialize NPC after creation
    }
}
```

## NPC Class Structure

### Required Overrides

Every custom NPC must override these methods:

- **`IsPhysical`**: Determines if the NPC is visible in the world
- **`ConfigurePrefab`**: Sets up NPC identity, components, and default behavior

### Optional Overrides

- **`OnCreated`**: Runtime initialization after NPC creation
- **`OnLoaded`**: Called when NPC is loaded from save file
- **`OnResponseLoaded`**: Handle loaded text message responses

## Physical vs Non-Physical NPCs

### Physical NPCs (`IsPhysical = true`)

Physical NPCs are visible in the game world and can be directly interacted with:

```csharp
protected override bool IsPhysical => true;
```

**Characteristics:**
- Visible in the game world
- Have a 3D model and avatar
- Can be interacted with directly
- Can move around and follow schedules
- Have collision detection
- Can be damaged and healed
- Can use vehicles and buildings

**Use Cases:**
- Shopkeepers and vendors
- Quest givers
- Random encounters
- Story characters
- Business partners

### Non-Physical NPCs (`IsPhysical = false`)

Non-physical NPCs are invisible and primarily used for communication:

```csharp
protected override bool IsPhysical => false;
```

**Characteristics:**
- Invisible in the world
- Primarily used for messaging and phone contacts
- Cannot be directly interacted with
- Cannot move or follow schedules
- Cannot be damaged

**Use Cases:**
- Informants and contacts
- Remote business partners
- Story NPCs that don't appear physically
- Phone-only characters
- Background characters

## Required Methods

### ConfigurePrefab Method

**Purpose**: Set up NPC components and default behavior before the NPC is spawned.

**Important**: Customer, relationship, and schedule configuration must be done here for proper save/load behavior.

```csharp
protected override void ConfigurePrefab(NPCPrefabBuilder builder)
{
    // Set spawn position
    builder.WithSpawnPosition(new Vector3(0, 0, 0));
    
    // Add customer behavior
    builder.EnsureCustomer()
           .WithCustomerDefaults(cd => {
               cd.WithSpending(100f, 500f)
                 .WithOrdersPerWeek(1, 3);
           });
    
    // Set relationship defaults
    builder.WithRelationshipDefaults(r => {
        r.WithDelta(2.0f)
         .SetUnlocked(false)
         .SetUnlockType(NPCRelationship.UnlockType.DirectApproach);
    });
    
    // Define schedule
    builder.WithSchedule(plan => {
        plan.WalkTo(new Vector3(10, 0, 10), 900)
            .StayInBuilding(building, 1000, 60);
    });
}
```

### Constructor

**Purpose**: Create the NPC instance. Identity is configured via `ConfigurePrefab` using `WithIdentity` and `WithIcon`.

```csharp
public MyCustomNPC() : base()
{
}
```

**Note**: For new code, use the parameterless constructor and configure identity in `ConfigurePrefab`. The old constructor pattern with identity parameters is obsolete and provided only for backwards compatibility with non-physical NPCs.

## Identity Configuration

NPC identity is configured in `ConfigurePrefab` using the builder methods `WithIdentity` and `WithIcon`.

### WithIdentity Method

**Purpose**: Set the NPC's unique identifier and display name.

```csharp
builder.WithIdentity(
    id: "my_custom_npc",           // Unique identifier
    firstName: "John",             // Display name
    lastName: "Doe");              // Optional last name
```

**Parameters:**

- **`id`**: Unique identifier used for save/load and game systems
  - Must be unique across all NPCs
  - Used for save/load persistence
  - Should be descriptive and consistent
  - Examples: `"shopkeeper_alex"`, `"informant_mike"`

- **`firstName`**: Display name for the NPC
  - Shown in UI elements
  - Used in dialogue and messages
  - Required

- **`lastName`**: Optional last name
  - Combined with firstName for full name
  - Can be null
  - Examples: `"Smith"`, `"Johnson"`

### WithIcon Method

**Purpose**: Set the icon sprite for UI elements.

```csharp
builder.WithIcon(iconSprite); // Optional, can be null
```

**Parameters:**

- **`icon`**: Optional sprite for UI elements
  - Used in messages, contacts, relationships
  - Can be null (uses default icon)
  - Should be a 64x64 or 128x128 sprite

## Lifecycle Methods

### OnCreated Method

**Purpose**: Runtime initialization after the NPC is fully created and spawned.

**When Called**: After the NPC is instantiated and all components are set up.

```csharp
protected override void OnCreated()
{
    base.OnCreated();
    
    // Set up appearance
    Appearance
        .Set<CustomizationFields.Gender>(0.5f)
        .Set<CustomizationFields.Height>(1.0f)
        .Build();
    
    // Configure dialogue systems
    Dialogue.BuildAndRegisterContainer("MyDialogue", c => {
        c.AddNode("ENTRY", "Hello there!", ch => {
            ch.Add("GREET", "Hello!", "RESPONSE");
        });
    });
    
    // Subscribe to events
    Customer.OnDealCompleted(() => {
        Debug.Log("Deal completed!");
    });
    
    // Enable systems
    Schedule.Enable();
    Schedule.InitializeActions();
    
    // Set basic properties
    Region = Region.Northtown;
    Aggressiveness = 2f;
}
```

### OnLoaded Method

**Purpose**: Handle NPC state when loaded from a save file.

**When Called**: When the NPC is loaded from save data.

```csharp
protected override void OnLoaded()
{
    base.OnLoaded();
    
    // Restore any custom state
    Debug.Log($"NPC {FullName} loaded from save");
    
    // Re-subscribe to events if needed
    // Re-enable systems if needed
}
```

### OnResponseLoaded Method

**Purpose**: Handle text message responses loaded from save file.

**When Called**: For each saved text message response.

```csharp
protected override void OnResponseLoaded(Response response)
{
    // Re-attach callbacks to loaded responses
    if (response.ID == "DEAL_RESPONSE")
    {
        response.OnSelected(() => {
            // Handle deal response
        });
    }
}
```

## Basic Example

Here's a complete basic example that demonstrates all the core concepts:

```csharp
using S1API.Entities;
using S1API.Entities.Schedule;
using S1API.Map;
using S1API.Money;
using S1API.Economy;
using S1API.GameTime;
using S1API.Products;
using S1API.Properties;
using UnityEngine;

public sealed class BasicShopkeeper : NPC
{
    protected override bool IsPhysical => true;
    
    protected override void ConfigurePrefab(NPCPrefabBuilder builder)
    {
        Vector3 shopPosition = new Vector3(-28.060f, 1.065f, 62.070f);
        Vector3 spawnPosition = new Vector3(-53.5701f, 1.065f, 67.7955f);
        
        builder.WithIdentity(
                id: "basic_shopkeeper",
                firstName: "Alex",
                lastName: "Shopkeeper")
                .WithIcon(null)
                .WithSpawnPosition(spawnPosition)
                .EnsureCustomer()
                .WithCustomerDefaults(cd => {
                    cd.WithSpending(200f, 800f)
                      .WithOrdersPerWeek(2, 5)
                      .WithPreferredOrderDay(Day.Friday)
                      .WithOrderTime(1400)
                      .WithStandards(CustomerStandard.Low)
                      .AllowDirectApproach(true)
                      .WithAffinities(new[] {
                          (DrugType.Marijuana, 0.6f),
                          (DrugType.Cocaine, 0.3f)
                      });
                })
                .WithRelationshipDefaults(r => {
                    r.WithDelta(2.0f)
                     .SetUnlocked(true)
                     .SetUnlockType(NPCRelationship.UnlockType.DirectApproach);
                })
                .WithSchedule(plan => {
                    plan.EnsureDealSignal();
                    plan.WalkTo(shopPosition, 800);
                    plan.Add(new StayInBuildingSpec { 
                        BuildingName = "North apartments", 
                        StartTime = 900, 
                        DurationMinutes = 480 
                    });
                    plan.WalkTo(spawnPosition, 1800);
                });
    }
    
    public BasicShopkeeper() : base()
    {
    }
    
    protected override void OnCreated()
    {
        base.OnCreated();
        
        // Set up appearance
        Appearance
            .Set<CustomizationFields.Gender>(0.5f)
            .Set<CustomizationFields.Height>(1.0f)
            .Set<CustomizationFields.SkinColor>(new Color32(150, 120, 95, 255))
            .WithBodyLayer<BodyLayerFields.Shirts>("Avatar/Layers/Top/Shirt", Color.white)
            .WithBodyLayer<BodyLayerFields.Pants>("Avatar/Layers/Bottom/Jeans", Color.blue)
            .Build();
        
        // Set up dialogue
        Dialogue.BuildAndRegisterContainer("ShopkeeperDialogue", c => {
            c.AddNode("ENTRY", "Welcome to my shop! What can I do for you?", ch => {
                ch.Add("BUY", "I want to buy something", "BUY_MENU")
                  .Add("SELL", "I want to sell something", "SELL_MENU")
                  .Add("LEAVE", "Nothing, thanks", "EXIT");
            });
            
            c.AddNode("BUY_MENU", "Here's what I have in stock...", ch => {
                ch.Add("ITEM_1", "Buy Item 1 ($50)", "PURCHASE")
                  .Add("BACK", "Back to main menu", "ENTRY");
            });
            
            c.AddNode("PURCHASE", "Excellent choice! Here you go.");
            c.AddNode("EXIT", "Come back anytime!");
        });
        
        // Set up choice callbacks
        Dialogue.OnChoiceSelected("ITEM_1", () => {
            if (Money.GetCashBalance() >= 50f) {
                Money.ChangeCashBalance(-50f, visualizeChange: true);
                Dialogue.JumpTo("ShopkeeperDialogue", "PURCHASE");
            }
        });
        
        // Set up events
        Customer.OnDealCompleted(() => {
            SendTextMessage("Thanks for the business!");
        });
        
        // Enable systems
        Schedule.Enable();
        Schedule.InitializeActions();
        
        // Set properties
        Region = Region.Northtown;
        Aggressiveness = 2f;
    }
}
```

## Next Steps

Now that you understand the basics of NPC creation, you can explore:

- **[Prefab Configuration](prefab-configuration.md)** - Detailed component setup
- **[Appearance Customization](appearance-customization.md)** - Visual customization
- **[Scheduling System](scheduling-system.md)** - Movement and activities
- **[Dialogue System](dialogue-system.md)** - Interactive conversations

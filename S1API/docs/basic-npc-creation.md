## Basic NPC Creation

This guide covers the fundamental concepts and basic setup for creating custom NPCs in S1API. The S1API Entity System provides a robust framework, leveraging an abstraction layer and the Builder Pattern to streamline NPC creation.

### Core Steps for NPC Creation:
1.  **Inherit from `S1API.Entities.NPC`**: All custom NPCs must extend this base class.
2.  **Configure Prefab**: Override the `ConfigurePrefab` method to define the NPC's identity, initial spawn position, and core schedule logic using an `NPCPrefabBuilder`.
    ```csharp
    protected override void ConfigurePrefab(NPCPrefabBuilder builder) {
        builder.WithIdentity("my_unique_npc_id", "John", "Doe")
               .WithSpawnPosition(new Vector3(0,0,0))
               .WithSchedule(plan => plan.WalkTo(someDestination, 900));
    }
    ```
3.  **Initialize on Creation**: Override the `OnCreated` method to set up appearance (e.g., gender, clothing), subscribe to events, or perform other initializations after the NPC object has been instantiated in the game world.
    ```csharp
    protected override void OnCreated() {
        base.OnCreated();
        Appearance.Set<CustomizationFields.Gender>(0f).Build();
        // Additional setup like event subscriptions
    }
    ```

NPCs can be either physical (visible in the world) or non-physical (e.g., contact-only characters). The `S1API.Entities` module provides modular access to various components such as `Appearance`, `Schedule`, `Dialogue`, and `Customer` behaviors to build complex characters.

## Table of Contents

1. [Introduction to NPC Creation](#introduction-to-npc-creation)
2. [The Base `NPC` Class Structure](#the-base-npc-class-structure)
3. [Configuring Core Properties with `NPCPrefabBuilder`](#configuring-core-properties-with-npcrefabbuilder)
    *   `WithIdentity`
    *   `WithSpawnPosition`
    *   `WithSchedule`
4. [NPC Lifecycle Methods & Event Hooks](#npc-lifecycle-methods-and-event-hooks)
    *   `OnCreated`
    *   `OnLoaded`
5. [Differentiating Physical vs. Non-Physical NPCs](#differentiating-physical-vs-non-physical-npcs)
6. [Accessing and Modifying NPC Components (Appearance, Schedule, Dialogue)](#accessing-and-modifying-npc-components)
7. [Complete Basic NPC Example](#complete-basic-npc-example)

## Creating Your First NPC

To create a custom NPC, inherit from the `NPC` base class. You'll primarily override `ConfigurePrefab` for defining the NPC's identity, spawn, and basic schedule, and `OnCreated` for runtime initialization like appearance.

```csharp
using S1API.Entities;
using S1API.Entities.Customization; // Required for CustomizationFields
using UnityEngine; // Required for Vector3

public sealed class MyCustomNPC : NPC
{
    // Determines if the NPC has a physical presence in the world.
    // Set to 'false' for purely contact-based or background characters.
    protected override bool IsPhysical => true; 

    protected override void ConfigurePrefab(NPCPrefabBuilder builder)
    {
        // Configure NPC identity, spawn position, and initial schedule
        builder.WithIdentity("my_custom_npc", "John", "Doe")
               .WithSpawnPosition(new Vector3(0f, 0f, 0f)) // Example: Spawn at world origin
               .WithSchedule(plan => {
                   // Example schedule: Make the NPC walk to a specific destination.
                   // Replace 'Vector3.zero' with an actual target position in your scene.
                   plan.WalkTo(Vector3.zero, 900); // Walk towards (0,0,0) for 900 seconds
               });
        
        // Additional configuration for behaviors, dialogue, etc.
    }
    
    protected override void OnCreated()
    {
        base.OnCreated();
        // Initialize NPC appearance (e.g., set gender or other customization fields)
        Appearance.Set<CustomizationFields.Gender>(0f).Build();
        
        // Subscribe to events, set initial states, etc.
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

**Purpose**: Set up NPC components and default behavior before the NPC is spawned. This includes essential properties like identity and spawn position, as well as optional behaviors such as customer traits, relationship defaults, and scheduling.

**Important**: Configuration of identity, customer, relationship, and schedule *must* be done here for proper save/load behavior.

```csharp
protected override void ConfigurePrefab(NPCPrefabBuilder builder)
{
    // Essential: Set the NPC's unique ID, first name, and last name.
    // This ID is used for saving and referencing the NPC.
    builder.WithIdentity("my_custom_npc_id", "John", "Doe");

    // Essential: Set the NPC's initial spawn position in the game world.
    builder.WithSpawnPosition(new Vector3(0, 0, 0));
    
    // Optional: Add customer behavior, making the NPC able to place orders.
    builder.EnsureCustomer()
           .WithCustomerDefaults(cd => {
               cd.WithSpending(100f, 500f)
                 .WithOrdersPerWeek(1, 3);
           });
    
    // Optional: Set default relationship parameters for the NPC.
    builder.WithRelationshipDefaults(r => {
        r.WithDelta(2.0f)
         .SetUnlocked(false)
         .SetUnlockType(NPCRelationship.UnlockType.DirectApproach);
    });
    
    // Optional: Define the NPC's daily schedule using a fluent API.
    builder.WithSchedule(plan => {
        plan.WalkTo(new Vector3(10, 0, 10), 900)
            .StayInBuilding(building, 1000, 60);
    });
}
```

### Constructor

**Purpose**: Create the NPC instance. Identity is configured via `ConfigurePrefab` using `WithIdentity`.

```csharp
public MyCustomNPC() : base()
{
}
```

**Note**: For new code, always use the parameterless constructor and configure identity in `ConfigurePrefab`. The old constructor pattern with identity parameters is obsolete and provided only for backwards compatibility with non-physical NPCs.

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

- **[Prefab Configuration](prefab-configuration.md)** - Detailed setup of identity, spawn, and initial schedule
- **[Appearance Customization](appearance-customization.md)** - Visual customization and dynamic appearance changes
- **[Scheduling System](scheduling-system.md)** - Defining complex movement patterns and activities
- **[Dialogue System](dialogue-system.md)** - Crafting interactive conversations and branching narratives
- **[Customer Behaviors](customer-behaviors.md)** - Integrating NPCs into the game's economy as customers or vendors
- **[NPC Lifecycle Hooks](npc-lifecycle-hooks.md)** - Understanding 'OnCreated' and 'OnLoaded' for advanced runtime logic and event handling
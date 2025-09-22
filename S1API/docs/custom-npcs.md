# Custom NPCs

The S1API provides a comprehensive system for creating custom NPCs that integrate seamlessly with the base game's systems. This guide covers creating physical NPCs with full functionality including schedules, dialogue, customer behavior, relationships, and appearance customization.

## Table of Contents

1. [Basic NPC Creation](#basic-npc-creation)
2. [Physical vs Non-Physical NPCs](#physical-vs-non-physical-npcs)
3. [Prefab Configuration](#prefab-configuration)
4. [Appearance Customization](#appearance-customization)
5. [Scheduling System](#scheduling-system)
6. [Dialogue System](#dialogue-system)
7. [Customer Behavior](#customer-behavior)
8. [Relationship Management](#relationship-management)
9. [Runtime Management](#runtime-management)
10. [Examples](#examples)

## Basic NPC Creation

To create a custom NPC, inherit from the `NPC` base class and implement the required methods:

```csharp
public sealed class MyCustomNPC : NPC
{
    protected override bool IsPhysical => true; // Make NPC visible in world
    
    protected override void ConfigurePrefab(NPCPrefabBuilder builder)
    {
        // Configure NPC components and behavior
    }
    
    public MyCustomNPC() : base(
        id: "my_custom_npc",
        firstName: "John",
        lastName: "Doe",
        icon: null) // Optional icon sprite
    {
    }
    
    protected override void OnCreated()
    {
        base.OnCreated();
        // Initialize NPC after creation
    }
}
```

### Required Parameters

- **id**: Unique identifier used for save/load and game systems
- **firstName**: Display name for the NPC
- **lastName**: Optional last name
- **icon**: Optional sprite for UI elements (messages, contacts, etc.)

## Physical vs Non-Physical NPCs

### Physical NPCs (`IsPhysical = true`)
- Visible in the game world
- Have a 3D model and avatar
- Can be interacted with directly
- Can move around and follow schedules
- Have collision detection

### Non-Physical NPCs (`IsPhysical = false`)
- Invisible in the world
- Primarily used for messaging and phone contacts
- Cannot be directly interacted with
- Useful for remote contacts or story NPCs

## Prefab Configuration

**Important**: Schedule, customer data, and relationship data should ONLY be configured in `ConfigurePrefab`. These systems are designed to work with save/load persistence and should not be modified at runtime.

Use `ConfigurePrefab` to set up your NPC's components and default behavior:

```csharp
protected override void ConfigurePrefab(NPCPrefabBuilder builder)
{
    builder.WithSpawnPosition(new Vector3(0, 0, 0))
           .EnsureCustomer()
           .WithCustomerDefaults(cd => {
               cd.WithSpending(100f, 500f)
                 .WithOrdersPerWeek(1, 3)
                 .WithPreferredOrderDay(Day.Friday);
           })
           .WithRelationshipDefaults(r => {
               r.WithDelta(2.0f)
                .SetUnlocked(false)
                .SetUnlockType(NPCRelationship.UnlockType.DirectApproach);
           })
           .WithSchedule(plan => {
               plan.WalkTo(new Vector3(10, 0, 10), 900)
                   .StayInBuilding(building, 1000, 60);
    });
}
```

### Available Configuration Methods

- **`WithSpawnPosition(Vector3, Quaternion)`**: Set NPC spawn location
- **`EnsureCustomer()`**: Add customer behavior component
- **`WithCustomerDefaults(Action<CustomerDataBuilder>)`**: Configure customer settings (PREFAB ONLY)
- **`WithRelationshipDefaults(Action<NPCRelationshipDataBuilder>)`**: Set relationship parameters (PREFAB ONLY)
- **`WithSchedule(Action<PrefabScheduleBuilder>)`**: Define NPC schedule (PREFAB ONLY)

**Note**: Customer, relationship, and schedule configuration must be done in `ConfigurePrefab` to ensure proper save/load behavior.

### Configuration Workflow

1. **Prefab Configuration** (in `ConfigurePrefab`):
   - Set spawn position
   - Configure customer defaults (spending, preferences, etc.)
   - Set relationship defaults (delta, unlock type, connections)
   - Define schedule actions (walk to, stay in building, etc.)

2. **Runtime Initialization** (in `OnCreated`):
   - Set up appearance
   - Configure dialogue systems
   - Subscribe to events
   - Enable schedule system
   - Set basic properties (region, aggressiveness, etc.)

## Appearance Customization

Customize your NPC's visual appearance using the `NPCAppearance` system:

```csharp
protected override void OnCreated()
{
    base.OnCreated();
    
    // Apply consistent appearance
    Appearance
        .Set<CustomizationFields.Gender>(0.5f) // 0=male, 1=female
        .Set<CustomizationFields.Height>(1.1f)
        .Set<CustomizationFields.Weight>(0.3f)
        .Set<CustomizationFields.SkinColor>(new Color32(150, 120, 95, 255))
        .Set<CustomizationFields.EyeBallTint>(Color.blue)
        .Set<CustomizationFields.HairColor>(Color.brown)
        .Set<CustomizationFields.HairStyle>("Avatar/Hair/Spiky/Spiky")
        .WithFaceLayer<FaceLayerFields.Eyes>("Avatar/Layers/Face/Eyes_Happy", Color.black)
        .WithBodyLayer<BodyLayerFields.Shirts>("Avatar/Layers/Top/T-Shirt", Color.red)
        .WithBodyLayer<BodyLayerFields.Pants>("Avatar/Layers/Bottom/Jeans", Color.blue)
        .WithAccessoryLayer<AccessoryFields.Feet>("Avatar/Accessories/Feet/Sneakers", Color.white)
        .Build(); // Generates mugshot and applies appearance
}
```

### Appearance Categories

- **Customization Fields**: Basic properties like height, weight, skin color
- **Face Layers**: Eyes, facial hair, face expressions
- **Body Layers**: Shirts, pants, undergarments
- **Accessory Layers**: Shoes, hats, jewelry, etc.

### Random Appearance Generation

```csharp
protected override void OnCreated()
{
    base.OnCreated();
    
    // Generate completely random appearance
Appearance.GenerateRandomAppearance();
    Appearance.Build();
}
```

## Scheduling System

NPCs can follow complex schedules using the scheduling system. Schedules are defined in `ConfigurePrefab` and can include various actions:

```csharp
protected override void ConfigurePrefab(NPCPrefabBuilder builder)
{
    builder.WithSchedule(plan => {
        // Walk to location at 9:00 AM
        plan.WalkTo(new Vector3(10, 0, 10), 900, faceDestinationDir: true);
        
        // Stay in building from 10:00 AM for 60 minutes
        var building = Buildings.GetByGUID("some-building-guid");
        plan.StayInBuilding(building, 1000, 60);
        
        // Use vending machine at 2:00 PM
        plan.Add(new UseVendingMachineSpec { 
            StartTime = 1400,
            MachineGUID = "vending-machine-guid"
        });
        
        // Drive to car park at 5:00 PM
        var parkingLot = ParkingLots.GetByGUID("parking-lot-guid");
        var vehicle = VehicleRegistry.GetByGUID("vehicle-guid");
        plan.DriveToCarPark(parkingLot, vehicle, 1700, ParkingAlignment.FrontToKerb);
        
        // Location-based dialogue at 7:00 PM
        plan.Add(new LocationDialogueSpec {
            Destination = new Vector3(20, 0, 20),
            StartTime = 1900,
            FaceDestinationDirection = true,
            GreetingOverrideToEnable = 1
        });
        
        // Ensure deal signal exists
        plan.EnsureDealSignal();
    });
}
```

### Available Schedule Actions

- **`WalkTo(Vector3, int, bool, float, bool, string)`**: Move to location
- **`StayInBuilding(Building, int, int, int?, string)`**: Stay in building for duration
- **`UseVendingMachineSpec`**: Use vending machine
- **`DriveToCarParkSpec`**: Drive to and park at car park
- **`LocationDialogueSpec`**: Trigger dialogue at location
- **`EnsureDealSignal()`**: Enable customer deal waiting

### Runtime Schedule Management

**Note**: Schedule configuration should only be done in `ConfigurePrefab`. Runtime schedule management is limited to basic control:

```csharp
protected override void OnCreated()
{
    base.OnCreated();
    
    // Enable schedule system
    Schedule.Enable();
    Schedule.InitializeActions();
    
    // Check active action
    string activeAction = Schedule.GetActiveActionName();
    
    // Basic schedule control
    Schedule.Disable(); // Disable schedule
    Schedule.SetCurfewMode(true); // Enable curfew mode
}
```

## Dialogue System

Create interactive dialogue systems for your NPCs:

```csharp
protected override void OnCreated()
{
    base.OnCreated();
    
    // Build dialogue database
    Dialogue.BuildAndSetDatabase(db => {
        db.WithModuleEntry("Reactions", "GREETING", "Hello there!");
        db.WithModuleEntry("Reactions", "ANGRY", "I'm not happy about this!");
    });
    
    // Create dialogue container
    Dialogue.BuildAndRegisterContainer("ShopDialogue", c => {
        c.AddNode("ENTRY", "Welcome to my shop! What can I help you with?", ch => {
            ch.Add("BUY_ITEM", "I'd like to buy something", "ITEM_SELECTION")
              .Add("SELL_ITEM", "I want to sell something", "SELL_DIALOGUE")
              .Add("LEAVE", "Never mind", "EXIT");
        });
        
        c.AddNode("ITEM_SELECTION", "Here's what I have available...", ch => {
            ch.Add("PURCHASE", "I'll take it", "PURCHASE_CONFIRM")
              .Add("BACK", "Let me think", "ENTRY");
        });
        
        c.AddNode("PURCHASE_CONFIRM", "That'll be $100. Deal?", ch => {
            ch.Add("YES", "Yes, deal!", "PURCHASE_COMPLETE")
              .Add("NO", "Too expensive", "ENTRY");
        });
        
        c.AddNode("PURCHASE_COMPLETE", "Pleasure doing business!");
        c.AddNode("EXIT", "Come back anytime!");
    });
    
    // Set up choice callbacks
    Dialogue.OnChoiceSelected("PURCHASE", () => {
        // Handle purchase logic
        var playerCash = Money.GetCashBalance();
        if (playerCash >= 100f) {
            Money.ChangeCashBalance(-100f, visualizeChange: true);
            Dialogue.JumpTo("ShopDialogue", "PURCHASE_COMPLETE");
        } else {
            Dialogue.JumpTo("ShopDialogue", "NOT_ENOUGH_CASH");
        }
    });
    
    // Use container when player interacts
    Dialogue.UseContainerOnInteract("ShopDialogue");
}
```

### Dialogue Features

- **Modular Design**: Separate dialogue modules for different contexts
- **Choice-based Flow**: Branching dialogue with player choices
- **Runtime Callbacks**: Execute code when choices are made
- **Dynamic Navigation**: Jump between dialogue nodes programmatically
- **Worldspace Text**: Show dialogue text above NPC
- **Reactions**: Trigger NPC reactions and animations

## Customer Behavior

Configure NPCs to act as customers for your business:

```csharp
protected override void ConfigurePrefab(NPCPrefabBuilder builder)
{
    builder.EnsureCustomer()
           .WithCustomerDefaults(cd => {
    // Spending behavior
    cd.WithSpending(minWeekly: 150f, maxWeekly: 600f)
      .WithOrdersPerWeek(1, 4)
      .WithPreferredOrderDay(Day.Friday)
                 .WithOrderTime(1100); // 11:00 AM
    
               // Customer standards and behavior
    cd.WithStandards(CustomerStandard.Low)
      .AllowDirectApproach(true)
                 .GuaranteeFirstSample(true)
                 .WithCallPoliceChance(0.15f);
    
    // Relationship requirements
    cd.WithMutualRelationRequirement(minAt50: 2.5f, maxAt100: 4.0f);
    
               // Addiction and dependence
    cd.WithDependence(baseAddiction: 0.1f, dependenceMultiplier: 1.1f);
    
    // Product preferences
    cd.WithAffinities(new[] {
                   (DrugType.Marijuana, 0.45f),
                   (DrugType.Cocaine, -0.2f)
    });
    
    // Property preferences
    cd.WithPreferredPropertiesById("Munchies", "Energizing");
});
}
```

### Customer Events

**Note**: Customer data configuration should only be done in `ConfigurePrefab` via `WithCustomerDefaults`. Runtime customer management is limited to events and basic actions:

```csharp
protected override void OnCreated()
{
    base.OnCreated();
    
    // Subscribe to customer events
    Customer.OnUnlocked(() => {
        Debug.Log("Customer unlocked!");
    });
    
    Customer.OnDealCompleted(() => {
        Debug.Log("Deal completed with customer!");
    });
    
    Customer.OnContractAssigned((payment, quantity, startTime, endTime) => {
        Debug.Log($"Contract: ${payment} for {quantity} items between {startTime}-{endTime}");
    });
    
    // Basic customer actions
    Customer.ForceDealOffer();
    Customer.RequestProduct();
    Customer.SetAwaitingDelivery(true);
}
```

## Relationship Management

Configure NPC relationships and connections:

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

### Runtime Relationship Management

**Note**: Relationship data configuration should only be done in `ConfigurePrefab` via `WithRelationshipDefaults`. Runtime relationship management is limited to events and basic queries:

```csharp
protected override void OnCreated()
{
    base.OnCreated();
    
    // Subscribe to relationship changes
    Relationship.OnChanged(delta => {
        Debug.Log($"Relationship changed by {delta}");
    });
    
    // Subscribe to unlock events
    Relationship.OnUnlocked((type, notify) => {
        Debug.Log($"NPC unlocked via {type}, notify: {notify}");
    });
    
    // Basic relationship actions
    Relationship.Add(1.0f); // Increase relationship
    Relationship.Unlock(NPCRelationship.UnlockType.DirectApproach);
    Relationship.UnlockConnections();
    
    // Check relationship status
    bool isKnown = Relationship.IsKnown;
    bool isMutuallyKnown = Relationship.IsMutuallyKnown;
    List<string> connections = Relationship.ConnectionIDs;
    float currentDelta = Relationship.Delta;
}
```

## Runtime Management

### NPC Lifecycle

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
}
```

### NPC Properties and Methods

```csharp
// Basic properties
string name = FirstName;
string fullName = FullName;
Vector3 position = Position;
bool isConscious = IsConscious;
bool isInBuilding = IsInBuilding;
bool isInVehicle = IsInVehicle;

// Health management
float health = CurrentHealth;
float maxHealth = MaxHealth;
bool isDead = IsDead;
bool isInvincible = IsInvincible;

// Actions
Damage(25); // Deal damage
Heal(50);   // Heal NPC
Kill();     // Kill NPC
Revive();   // Revive NPC
Panic();    // Make NPC panic
StopPanicking(); // Stop panic

// Movement
Goto(new Vector3(10, 0, 10)); // Move to position
Scale = 1.2f; // Set NPC scale
```

### Messaging System

```csharp
// Send text message to player
SendTextMessage("Hello! How are you today?");

// Send message with responses
SendTextMessage("Want to make a deal?", new[] {
    new Response("YES_DEAL", "Yes, let's make a deal!"),
    new Response("NO_DEAL", "Not interested")
}, responseDelay: 2f);

// Set conversation visibility
ConversationCanBeHidden = true;
```

## Examples

### Complete Example: Shopkeeper NPC

```csharp
public sealed class ShopkeeperNPC : NPC
{
    protected override bool IsPhysical => true;
    
    protected override void ConfigurePrefab(NPCPrefabBuilder builder)
    {
        Vector3 shopPosition = new Vector3(-28.060f, 1.065f, 62.070f);
        Vector3 spawnPosition = new Vector3(-53.5701f, 1.065f, 67.7955f);
        var building = Buildings.GetAll().First();
        
        builder.WithSpawnPosition(spawnPosition)
               .EnsureCustomer()
               .WithCustomerDefaults(cd => {
                   cd.WithSpending(200f, 800f)
                     .WithOrdersPerWeek(2, 5)
                     .WithPreferredOrderDay(Day.Friday)
                     .WithOrderTime(1400)
                     .WithStandards(CustomerStandard.Medium)
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
                   plan.StayInBuilding(building, 900, 480); // 8 hours in shop
                   plan.WalkTo(spawnPosition, 1800);
               });
    }
    
    public ShopkeeperNPC() : base(
        id: "shopkeeper_alex",
        firstName: "Alex",
        lastName: "Shopkeeper",
        icon: null)
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
                  .Add("CHAT", "Just chatting", "CHAT")
                  .Add("LEAVE", "Nothing, thanks", "EXIT");
            });
            
            c.AddNode("BUY_MENU", "Here's what I have in stock...", ch => {
                ch.Add("ITEM_1", "Buy Item 1 ($50)", "PURCHASE")
                  .Add("ITEM_2", "Buy Item 2 ($100)", "PURCHASE")
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
        
        Dialogue.OnChoiceSelected("ITEM_2", () => {
            if (Money.GetCashBalance() >= 100f) {
                Money.ChangeCashBalance(-100f, visualizeChange: true);
                Dialogue.JumpTo("ShopkeeperDialogue", "PURCHASE");
            }
        });
        
        // Set up customer events
        Customer.OnDealCompleted(() => {
            SendTextMessage("Thanks for the business!");
        });
        
        // Set up relationship events
        Relationship.OnChanged(delta => {
            if (delta > 0) {
                SendTextMessage("I appreciate your business!");
            }
        });
        
        // Enable systems
        Schedule.Enable();
        Schedule.InitializeActions();
        
        // Set region and properties
        Region = Region.Northtown;
        Aggressiveness = 2f;
    }
}
```

### Simple Contact NPC (Non-Physical)

```csharp
public sealed class ContactNPC : NPC
{
    protected override bool IsPhysical => false; // Invisible contact
    
    public ContactNPC() : base(
        id: "informant_mike",
        firstName: "Mike",
        lastName: "Informant",
        icon: null)
    {
    }
    
    protected override void OnCreated()
    {
        base.OnCreated();
        
        // Send periodic messages
        MelonCoroutines.Start(PeriodicMessages());
        
        // Set up relationship
        Relationship.Unlock(NPCRelationship.UnlockType.DirectApproach);
        Relationship.Delta = 3.0f;
    }
    
    private IEnumerator PeriodicMessages()
    {
        while (true) {
            yield return new WaitForSeconds(300f); // 5 minutes
            
            string[] messages = {
                "Hey, I heard some interesting news...",
                "The police are getting suspicious in the docks area.",
                "New shipment arrived at the warehouse yesterday.",
                "Watch out for undercover cops near the mall."
            };
            
            string message = messages[UnityEngine.Random.Range(0, messages.Length)];
            SendTextMessage(message);
        }
    }
}
```

This documentation covers the complete Custom NPC system in S1API. For more specific examples and advanced usage, refer to the example projects in the repository.

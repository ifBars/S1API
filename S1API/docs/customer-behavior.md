# Customer Behavior

The customer system allows NPCs to act as business customers, buying products from the player and participating in the game's economy.

## Table of Contents

1. [Overview](#overview)
2. [Customer Configuration](#customer-configuration)
3. [Customer Properties](#customer-properties)
4. [Customer Events](#customer-events)
5. [Runtime Customer Management](#runtime-customer-management)
6. [Customer Examples](#customer-examples)
7. [Best Practices](#best-practices)

## Overview

Customer NPCs can buy products from the player, follow spending patterns, and participate in the game's economy. The customer system is configured in `ConfigurePrefab` and managed at runtime.

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
               cd.WithStandards(CustomerStandard.VeryLow)
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
               cd.WithPreferredProperties(Property.Munchies, Property.Energizing);
           });
}
```

## Customer Configuration

### Enabling Customer Behavior

First, ensure the customer component is added:

```csharp
builder.EnsureCustomer();
```

### Customer Defaults

Configure customer behavior using the `CustomerDataBuilder`:

```csharp
builder.WithCustomerDefaults(cd => {
    // Configuration here
});
```

**Important**: Customer configuration must be done in `ConfigurePrefab` for proper save/load behavior.

## Customer Properties

### Spending Behavior

Configure how much the customer spends:

```csharp
cd.WithSpending(minWeekly: 100f, maxWeekly: 500f)
  .WithOrdersPerWeek(1, 3)
  .WithPreferredOrderDay(Day.Friday)
  .WithOrderTime(1400); // 2:00 PM
```

**Spending Ranges:**
- **Low**: 50-200 per week
- **Medium**: 200-500 per week
- **High**: 500-1000+ per week

**Order Frequency:**
- **Light**: 1-2 orders per week
- **Moderate**: 2-4 orders per week
- **Heavy**: 4-7 orders per week

### Customer Standards

Set quality expectations:

```csharp
cd.WithStandards(CustomerStandard.VeryLow);  // Accepts very low quality
cd.WithStandards(CustomerStandard.Low);      // Accepts low quality
cd.WithStandards(CustomerStandard.Moderate); // Expects decent quality
cd.WithStandards(CustomerStandard.High);     // Demands high quality
cd.WithStandards(CustomerStandard.VeryHigh); // Demands very high quality
```

**Standard Levels:**
- **VeryLow**: Accepts any quality, very lenient
- **Low**: Accepts low quality, less picky
- **Moderate**: Expects decent quality, some standards
- **High**: Demands high quality, very picky
- **VeryHigh**: Demands very high quality, extremely picky

### Behavior Settings

Configure customer behavior:

```csharp
cd.AllowDirectApproach(true)           // Can be approached directly
  .GuaranteeFirstSample(true)          // First sample always succeeds
  .WithCallPoliceChance(0.15f);        // 15% chance to call police
```

**Behavior Options:**
- **AllowDirectApproach**: Whether the customer can be approached directly
- **GuaranteeFirstSample**: Whether the first sample always succeeds
- **WithCallPoliceChance**: Chance to call police (0.0-1.0)

### Relationship Requirements

Set relationship requirements for deals:

```csharp
cd.WithMutualRelationRequirement(minAt50: 2.5f, maxAt100: 4.0f);
```

**Relationship Levels:**
- **0.0**: Stranger
- **1.0**: Acquaintance
- **2.5**: Friend
- **4.0**: Good friend
- **5.0**: Best friend

### Addiction and Dependence

Configure addiction mechanics:

```csharp
cd.WithDependence(baseAddiction: 0.1f, dependenceMultiplier: 1.1f);
```

**Parameters:**
- **baseAddiction**: Base addiction level (0.0-1.0)
- **dependenceMultiplier**: How quickly addiction grows (0.0-2.0)

### Product Preferences

Define what products the customer likes:

```csharp
cd.WithAffinities(new[] {
    (DrugType.Marijuana, 0.6f),    // Likes marijuana
    (DrugType.Cocaine, 0.3f),      // Somewhat likes cocaine
    (DrugType.Heroin, -0.5f)       // Dislikes heroin
});
```

**Affinity Values:**
- **1.0f**: Loves this product type
- **0.5f**: Likes this product type
- **0.0f**: Neutral
- **-0.5f**: Dislikes this product type
- **-1.0f**: Hates this product type

### Property Preferences

Set preferred product properties:

```csharp
cd.WithPreferredProperties(Property.Munchies, Property.Energizing, Property.Cyclopean);
```

**Available Properties:**
- **Property.Munchies**: Increases appetite
- **Property.Energizing**: Provides energy
- **Property.Cyclopean**: Visual effects
- **Property.AntiGravity**: Reduces gravity effects
- **Property.BrightEyed**: Improves vision

## Customer Events

### Event Subscription

Subscribe to customer events in `OnCreated`:

```csharp
protected override void OnCreated()
{
    base.OnCreated();
    
    // Subscribe to customer events
    Customer.OnUnlocked(() => {
        Debug.Log("Customer unlocked!");
        SendTextMessage("Hey, I'm looking for some products...");
    });
    
    Customer.OnDealCompleted(() => {
        Debug.Log("Deal completed with customer!");
        SendTextMessage("Thanks for the business!");
    });
    
    Customer.OnContractAssigned((payment, quantity, startTime, endTime) => {
        Debug.Log($"Contract: ${payment} for {quantity} items between {startTime}-{endTime}");
        SendTextMessage($"I need {quantity} items by {endTime}. Can you help?");
    });
}
```

### Available Events

- **OnUnlocked**: Customer becomes available for deals
- **OnDealCompleted**: A deal has been completed
- **OnContractAssigned**: A contract has been assigned
- **OnProductRequested**: Customer requests a specific product
- **OnDeliveryRequested**: Customer requests delivery

## Runtime Customer Management

### Basic Customer Actions

**Note**: Customer data configuration should only be done in `ConfigurePrefab` via `WithCustomerDefaults`. Runtime customer management is limited to events and basic actions:

```csharp
protected override void OnCreated()
{
    base.OnCreated();
    
    // Basic customer actions
    Customer.ForceDealOffer();        // Force a deal offer
    Customer.RequestProduct();        // Request a product
    Customer.SetAwaitingDelivery(true); // Set awaiting delivery status
}
```

### Customer State Queries

Check customer state:

```csharp
// Check if customer is unlocked
bool isUnlocked = Customer.IsUnlocked;

// Check if customer is awaiting delivery
bool awaitingDelivery = Customer.IsAwaitingDelivery;

// Get customer's current contract
var contract = Customer.CurrentContract;
```

## Customer Examples

### Basic Customer

```csharp
protected override void ConfigurePrefab(NPCPrefabBuilder builder)
{
    builder.EnsureCustomer()
           .WithCustomerDefaults(cd => {
               cd.WithSpending(100f, 300f)
                 .WithOrdersPerWeek(1, 2)
                 .WithPreferredOrderDay(Day.Friday)
                 .WithOrderTime(1400)
                 .WithStandards(CustomerStandard.VeryLow)
                 .AllowDirectApproach(true)
                 .WithCallPoliceChance(0.1f)
                 .WithAffinities(new[] {
                     (DrugType.Marijuana, 0.5f)
                 });
           });
}
```

### High-Value Customer

```csharp
protected override void ConfigurePrefab(NPCPrefabBuilder builder)
{
    builder.EnsureCustomer()
           .WithCustomerDefaults(cd => {
               cd.WithSpending(500f, 1000f)
                 .WithOrdersPerWeek(3, 5)
                 .WithPreferredOrderDay(Day.Saturday)
                 .WithOrderTime(1100)
                 .WithStandards(CustomerStandard.Moderate)
                 .AllowDirectApproach(false)
                 .GuaranteeFirstSample(true)
                 .WithCallPoliceChance(0.05f)
                 .WithMutualRelationRequirement(3.0f, 4.5f)
                 .WithDependence(0.2f, 1.2f)
                 .WithAffinities(new[] {
                     (DrugType.Cocaine, 0.8f),
                     (DrugType.Marijuana, 0.6f)
                 })
                 .WithPreferredProperties(Property.Energizing, Property.BrightEyed);
           });
}
```

### Risky Customer

```csharp
protected override void ConfigurePrefab(NPCPrefabBuilder builder)
{
    builder.EnsureCustomer()
           .WithCustomerDefaults(cd => {
               cd.WithSpending(200f, 600f)
                 .WithOrdersPerWeek(2, 4)
                 .WithPreferredOrderDay(Day.Sunday)
                 .WithOrderTime(2000)
                 .WithStandards(CustomerStandard.Low)
                 .AllowDirectApproach(true)
                 .WithCallPoliceChance(0.3f) // High police risk
                 .WithMutualRelationRequirement(1.0f, 2.0f)
                 .WithDependence(0.3f, 1.5f)
                 .WithAffinities(new[] {
                     (DrugType.Heroin, 0.7f),
                     (DrugType.Cocaine, 0.5f)
                 });
           });
}
```

### Event-Driven Customer

```csharp
protected override void OnCreated()
{
    base.OnCreated();
    
    // Set up customer events
    Customer.OnUnlocked(() => {
        SendTextMessage("Hey, I heard you have some products. Can we talk?");
        Relationship.Add(0.5f); // Increase relationship
    });
    
    Customer.OnDealCompleted(() => {
        SendTextMessage("Great doing business with you!");
        Relationship.Add(1.0f); // Increase relationship more
    });
    
    Customer.OnContractAssigned((payment, quantity, startTime, endTime) => {
        var timeLeft = endTime - startTime;
        SendTextMessage($"I need {quantity} items in {timeLeft} hours. ${payment} if you can deliver.");
    });
    
    Customer.OnProductRequested((productType, quantity) => {
        SendTextMessage($"Do you have any {productType}? I need {quantity}.");
    });
}
```

## Best Practices

### Do's

- **Configure customer data in `ConfigurePrefab`** - required for save/load compatibility
- **Use appropriate spending ranges** for the NPC's role and importance
- **Set realistic relationship requirements** based on the customer's risk level
- **Subscribe to customer events** to provide feedback and interaction
- **Test customer behavior** with different product types and qualities

### Don'ts

- **Don't modify customer data at runtime** (except through proper APIs)
- **Don't forget to call `EnsureCustomer()`** before `WithCustomerDefaults()`
- **Don't use extreme values** for spending, addiction, or police chance
- **Don't create customers with impossible requirements** (e.g., high standards with low relationship)

### Error Handling

Wrap customer configuration in try-catch blocks:

```csharp
protected override void ConfigurePrefab(NPCPrefabBuilder builder)
{
    try
    {
        builder.EnsureCustomer()
               .WithCustomerDefaults(cd => {
                   // Customer configuration
               });
    }
    catch (Exception ex)
    {
        MelonLogger.Error($"Failed to configure customer for {GetType().Name}: {ex.Message}");
    }
}
```

### Performance Considerations

- **Keep customer configurations reasonable** - overly complex configurations can impact performance
- **Use efficient event handlers** - avoid expensive operations in event callbacks
- **Test with multiple customers** - ensure customer behavior works well together
- **Monitor customer performance** in multiplayer environments

## Complete Customer Examples

The **[S1API NPC Example Repository](https://github.com/ifBars/S1APINPCExample)** contains working customer NPCs you can copy from:

- **[ExamplePhysicalNPC1](https://github.com/ifBars/S1APINPCExample/blob/master/NPCs/ExamplePhysicalNPC1.cs)** (customer defaults + preferences)
- **[ExamplePhysicalNPC2](https://github.com/ifBars/S1APINPCExample/blob/master/NPCs/ExamplePhysicalNPC2.cs)** (customer events + dealer recommendation)

## Next Steps

Now that you understand customer behavior, explore:

- **[Relationship Management](relationship-management.md)** - Relationship system details
- **[Scheduling System](scheduling-system.md)** - Customer schedules and activities
- **[Runtime Management](runtime-management.md)** - NPC lifecycle and properties
- **[Dealer System](dealer-system.md)** - Creating dealer NPCs that customers can recommend

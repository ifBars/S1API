# Products System

S1API provides a comprehensive system for working with products (drugs, goods) in Schedule One, including product definitions, properties, and market dynamics.

## Overview

The Products system allows you to:
- Access existing product definitions (Weed, Cocaine, Meth, etc.)
- Work with product properties (THC%, Purity, etc.)
- Retrieve product information and pricing
- Integrate products with dealers and customers

## Product Definitions

Products in Schedule One are represented by `ProductDefinition` wrappers that provide access to the game's internal product system.

### Accessing Products

```csharp
using S1API.Products;

// Access a product by type
var weedProduct = ProductDefinition.GetByType(DrugType.Marijuana);
var cokeProduct = ProductDefinition.GetByType(DrugType.Cocaine);

// Check product information
if (weedProduct != null)
{
    string name = weedProduct.Name;
    float marketValue = weedProduct.MarketValue;
}
```

### Drug Types

Available drug types in the game:

```csharp
public enum DrugType
{
    None,
    Marijuana,
    Cocaine,
    Methamphetamine,
    Heroin,
    // ... other types
}
```

## Product Properties

Products have properties that affect their quality, value, and appeal to customers. The Properties system provides a type-safe way to work with product attributes.

### Common Properties

```csharp
using S1API.Properties;
using S1API.Products;

// Properties are accessed through the Property class
Property.Munchies      // Induces hunger
Property.Energizing    // Provides energy
Property.Cyclopean     // Hallucinogenic effect
Property.Relaxing      // Calming effect
Property.Euphoric      // Happiness effect
// ... many more
```

### Working with Properties

```csharp
using S1API.Properties;
using S1API.Products;

// Get a product instance
var weedProduct = ProductDefinition.GetByType(DrugType.Marijuana);

if (weedProduct != null)
{
    // Properties are accessed through the product's internal system
    // You'll typically use properties when configuring customers

    // Example: Customer preferences for properties
    .WithPreferredProperties(Property.Munchies, Property.Energizing, Property.Cyclopean)
}
```

## Product Instances

`ProductInstance` represents an actual instance of a product with specific properties:

```csharp
using S1API.Products;

// Product instances are typically created/managed by the game
// Access them through game systems or events

public void HandleProductSold(ProductInstance instance)
{
    if (instance != null)
    {
        var definition = instance.Definition;  // Get the product definition
        // Work with the specific product instance
    }
}
```

## Using Products with Customers

Products are most commonly used when configuring customer NPCs:

```csharp
using S1API.Entities;
using S1API.Economy;
using S1API.Growing;
using S1API.Properties;

.WithCustomerDefaults(cd =>
{
    // Customer spending and order frequency
    cd.WithSpending(minWeekly: 500f, maxWeekly: 2000f)
      .WithOrdersPerWeek(2, 5)

    // Drug preferences and affinities
    .WithAffinities(new[]
    {
        (DrugType.Marijuana, 0.45f),   // Likes weed
        (DrugType.Cocaine, -0.2f),     // Dislikes cocaine
        (DrugType.Methamphetamine, 0.0f)  // Neutral on meth
    })

    // Preferred product properties
    .WithPreferredProperties(
        Property.Munchies,
        Property.Energizing,
        Property.Cyclopean
    )

    // Quality standards
    .WithStandards(CustomerStandard.High);
});
```

## Product Properties Reference

### THC-Related (Marijuana)
- `Property.Munchies` - Induces hunger
- `Property.Cyclopean` - Visual/hallucinogenic effects
- `Property.Relaxing` - Calming effects
- `Property.Energizing` - Stimulating effects

### Cocaine Properties
- `Property.Euphoric` - Happiness/euphoria
- `Property.Energizing` - Energy boost
- `Property.Focused` - Concentration enhancement
- `Property.Confident` - Confidence boost

### Methamphetamine Properties
- `Property.Energizing` - Extreme stimulation
- `Property.Euphoric` - Intense happiness
- `Property.Focused` - Enhanced focus
- `Property.Tweaking` - Jittery/hyperactive

### Negative Properties
- `Property.Paranoid` - Induces paranoia
- `Property.Anxious` - Causes anxiety
- `Property.Nauseous` - Causes nausea
- `Property.Headache` - Induces headaches
- `Property.Dizzy` - Causes dizziness

### Quality Indicators
- `Property.Pure` - High purity (cocaine/meth)
- `Property.Potent` - High potency (general)
- `Property.Smooth` - Smooth experience
- `Property.Harsh` - Harsh/unpleasant

## Customer Affinities

Customer affinities determine how much a customer likes or dislikes specific drugs:

```csharp
// Affinity values range from -1.0 to 1.0
.WithAffinities(new[]
{
    (DrugType.Marijuana, 0.8f),        // Strongly prefers
    (DrugType.Cocaine, 0.3f),          // Somewhat likes
    (DrugType.Methamphetamine, 0.0f),  // Neutral
    (DrugType.Heroin, -0.5f)           // Dislikes
})
```

- **Positive values (0.0 to 1.0)**: Customer likes this drug type
- **Negative values (-1.0 to 0.0)**: Customer dislikes this drug type
- **Zero (0.0)**: Customer is neutral

## Customer Standards

Quality standards determine what quality products a customer will accept:

```csharp
public enum CustomerStandard
{
    VeryLow,    // Accepts any quality
    Low,        // Accepts poor to good quality
    Medium,     // Accepts average to good quality
    High,       // Only accepts good to excellent quality
    VeryHigh    // Only accepts excellent quality
}

// Usage
.WithCustomerDefaults(cd =>
{
    cd.WithStandards(CustomerStandard.High);  // Picky customer
});
```

## Complete Customer Example

Here's a complete example of an NPC customer with detailed product preferences:

```csharp
using S1API.Entities;
using S1API.Economy;
using S1API.GameTime;
using S1API.Growing;
using S1API.Properties;
using UnityEngine;

public sealed class SelectiveCustomer : NPC
{
    public override bool IsPhysical => true;

    protected override void ConfigurePrefab(NPCPrefabBuilder builder)
    {
        builder.WithIdentity("selective_customer", "Sarah", "Johnson")
            .WithSpawnPosition(new Vector3(0, 0, 0))
            .WithAppearanceDefaults(av =>
            {
                av.Gender = 1.0f;
                av.Height = 0.95f;
            })
            .EnsureCustomer()
            .WithCustomerDefaults(cd =>
            {
                // High spending, selective customer
                cd.WithSpending(minWeekly: 800f, maxWeekly: 3000f)
                  .WithOrdersPerWeek(2, 4)
                  .WithPreferredOrderDay(Day.Friday)
                  .WithOrderTime(1800)  // 6 PM

                // Quality conscious
                .WithStandards(CustomerStandard.High)
                .AllowDirectApproach(false)  // Must be introduced
                .GuaranteeFirstSample(true)

                // Relationship requirements
                .WithMutualRelationRequirement(minAt50: 3.0f, maxAt100: 4.5f)
                .WithCallPoliceChance(0.05f)  // Low risk

                // Addiction profile
                .WithDependence(baseAddiction: 0.2f, dependenceMultiplier: 1.2f)

                // Drug preferences - loves weed, dislikes hard drugs
                .WithAffinities(new[]
                {
                    (DrugType.Marijuana, 0.9f),        // Strongly prefers
                    (DrugType.Cocaine, -0.6f),         // Strongly dislikes
                    (DrugType.Methamphetamine, -0.8f)  // Very much dislikes
                })

                // Property preferences - wants relaxing, quality weed
                .WithPreferredProperties(
                    Property.Relaxing,
                    Property.Smooth,
                    Property.Potent,
                    Property.Munchies
                );
            })
            .WithRelationshipDefaults(r =>
            {
                r.WithDelta(2.0f)
                 .SetUnlocked(false)
                 .SetUnlockType(NPCRelationship.UnlockType.Introduction);
            });
    }

    protected override void OnCreated()
    {
        base.OnCreated();
        Appearance.Build();

        Dialogue.BuildAndSetDatabase(db =>
        {
            db.WithModuleEntry("Reactions", "GREETING",
                "I only deal with quality products. No junk.");
        });

        Aggressiveness = 1f;
        Region = Region.Downtown;
        Schedule.Enable();
    }
}
```

## Best Practices

1. **Balanced Affinities**: Don't make all affinities extreme - mix preferences for realistic customers

2. **Property Consistency**: Match preferred properties with drug affinities
   - Weed lovers → Relaxing, Munchies, Smooth
   - Coke users → Energizing, Euphoric, Confident
   - Meth users → Energizing, Focused, Euphoric

3. **Quality Standards**: Match standards with spending levels
   - High spenders → High/VeryHigh standards
   - Low spenders → Low/Medium standards

4. **Addiction Progression**: Use `WithDependence()` to create realistic addiction dynamics

5. **Risk Assessment**: Balance `WithCallPoliceChance()` with customer value and relationship

## Property Discovery

To discover all available properties, you can enumerate them at runtime:

```csharp
using S1API.Properties;
using System.Reflection;

// Get all static Property fields
var propertyType = typeof(Property);
var properties = propertyType.GetFields(
    BindingFlags.Public | BindingFlags.Static
);

foreach (var field in properties)
{
    if (field.FieldType == typeof(Property))
    {
        var prop = (Property)field.GetValue(null);
        MelonLogger.Msg($"Property: {field.Name}");
    }
}
```

## Common Patterns

### Creating a Weed Enthusiast

```csharp
.WithCustomerDefaults(cd =>
{
    cd.WithAffinities(new[] { (DrugType.Marijuana, 0.9f) })
      .WithPreferredProperties(
          Property.Relaxing,
          Property.Munchies,
          Property.Smooth
      )
      .WithStandards(CustomerStandard.High);
});
```

### Creating a Party Customer

```csharp
.WithCustomerDefaults(cd =>
{
    cd.WithAffinities(new[]
      {
          (DrugType.Cocaine, 0.7f),
          (DrugType.Marijuana, 0.4f)
      })
      .WithPreferredProperties(
          Property.Energizing,
          Property.Euphoric,
          Property.Confident
      )
      .WithStandards(CustomerStandard.Medium);
});
```

### Creating a Desperate Customer

```csharp
.WithCustomerDefaults(cd =>
{
    cd.WithSpending(50f, 200f)  // Low budget
      .WithAffinities(new[]
      {
          (DrugType.Methamphetamine, 0.8f)
      })
      .WithStandards(CustomerStandard.VeryLow)  // Accepts anything
      .WithDependence(0.8f, 1.5f);  // Highly addicted
});
```

## Technical Notes

- Product definitions are wrappers around the game's internal product system
- Properties use a token-based system internally for cross-runtime compatibility
- Customer preferences are saved with the game's save system
- Addiction levels affect order frequency and spending over time

## See Also

- [Customer Behavior](customer-behavior.md) - Detailed customer configuration
- [Dealer System](dealer-system.md) - Creating dealers who distribute products
- [Custom NPCs](custom-npcs.md) - Core NPC creation
- [CustomNPCTest Example](https://github.com/ifBars/S1API/tree/main/CustomNPCTest)
- [Products API Reference](../api/S1API.Products.html)
- [Properties API Reference](../api/S1API.Properties.html)

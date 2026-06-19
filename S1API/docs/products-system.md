# Products System

S1API provides a comprehensive system for working with products (drugs, goods) in Schedule One, including product definitions, properties, and market dynamics.

## Overview

The Products system allows you to:
- Access existing product definitions (Weed, Cocaine, Meth, etc.)
- Work with product property tokens such as `Munchies`, `Energizing`, and `Cyclopean`
- Retrieve product information and pricing
- Integrate products with dealers and customers

## Product Definitions

Products in Schedule One are represented by `ProductDefinition` wrappers that provide access to the game's internal product system.

### Accessing Products

Product definitions are discovered per-save. Use `ProductManager.DiscoveredProducts` to enumerate what's available:

```csharp
using S1API.Products;

foreach (var product in ProductManager.DiscoveredProducts)
{
    MelonLoader.MelonLogger.Msg($"{product.ID}: {product.Name} (${product.MarketValue})");
}
```

If you already know an item ID, you can resolve it via `ItemManager` and cast to `ProductDefinition`.

### Drug Types

S1API exposes an API-safe `S1API.Products.DrugType` enum for use in affinities:

```csharp
using S1API.Products;

// Mirrors the base game's drug types
public enum DrugType
{
    Marijuana,
    Methamphetamine,
    Cocaine,
    MDMA,
    Shrooms,
    Heroin
}
```

## Product Properties

Products have effect properties that affect value, customer preferences, and callbacks. S1API exposes these as `PropertyBase` tokens so mods do not need to reference runtime-specific game effect types directly.

### Common Properties

```csharp
using S1API.Properties;
using S1API.Products;

// Properties are accessed through the Property class
Property.Munchies
Property.Energizing
Property.Cyclopean
Property.Calming
Property.Euphoric
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

The current `Property` helper exposes these built-in tokens:

- `Property.Munchies`
- `Property.AntiGravity`
- `Property.Energizing`
- `Property.Focused`
- `Property.Smelly`
- `Property.Euphoric`
- `Property.Cyclopean`
- `Property.Slippery`
- `Property.Shrinking`
- `Property.Seizure`
- `Property.Electrifying`
- `Property.Zombifying`
- `Property.Disorienting`
- `Property.Sedating`
- `Property.CalorieDense`
- `Property.TropicThunder`
- `Property.Toxic`
- `Property.ThoughtProvoking`
- `Property.Lethal`
- `Property.Calming`
- `Property.Schizophrenic`
- `Property.Spicy`
- `Property.Laxative`
- `Property.BrightEyed`
- `Property.Sneaky`
- `Property.Jennerising`
- `Property.Balding`
- `Property.Glowie`
- `Property.Refreshing`
- `Property.Athletic`
- `Property.LongFaced`
- `Property.Paranoia`
- `Property.Gingeritis`
- `Property.Foggy`
- `Property.Explosive`

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

                // Property preferences
                .WithPreferredProperties(
                    Property.Calming,
                    Property.Euphoric,
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

2. **Property Consistency**: Match preferred property tokens with drug affinities. Use actual `S1API.Properties.Property` constants, not inferred product stats.

3. **Quality Standards**: Match standards with spending levels
   - High spenders → High/VeryHigh standards
   - Low spenders → Low/Medium standards

4. **Addiction Progression**: Use `WithDependence()` to create realistic addiction dynamics

5. **Risk Assessment**: Balance `WithCallPoliceChance()` with customer value and relationship

## Property Discovery

To discover all available properties, you can enumerate them at runtime:

```csharp
using S1API.Properties;
using S1API.Properties.Interfaces;
using System.Reflection;

// Get all static Property fields
var propertyType = typeof(Property);
var properties = propertyType.GetFields(
    BindingFlags.Public | BindingFlags.Static
);

foreach (var field in properties)
{
    if (field.FieldType == typeof(PropertyBase))
    {
        var prop = (PropertyBase)field.GetValue(null);
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
          Property.Calming,
          Property.Munchies,
          Property.Euphoric
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
          Property.Focused
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
- <xref:S1API.Products> - Products API Reference
- <xref:S1API.Property> - Properties API Reference

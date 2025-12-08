## Appearance Customization

The `NPCAppearance` system allows you to customize your NPC's visual appearance, including physical features, clothing, and accessories.

## Table of Contents

1. [Overview](#overview)
2. [Basic Appearance Setup](#basic-appearance-setup)
3. [Customization Fields](#customization-fields)
4. [Face Layers](#face-layers)
5. [Body Layers](#body-layers)
6. [Accessory Layers](#accessory-layers)
7. [Random Appearance Generation](#random-appearance-generation)
8. [Advanced Customization](#advanced-customization)
9. [Best Practices](#best-practices)

## Overview

The appearance system uses a builder pattern to configure various aspects of the NPC's visual appearance:

```csharp
protected override void OnCreated()
{
    base.OnCreated();
    
    Appearance
        .Set<CustomizationFields.Gender>(0.5f)
        .Set<CustomizationFields.Height>(1.1f)
        .Set<CustomizationFields.SkinColor>(new Color32(150, 120, 95, 255))
        .WithFaceLayer<FaceLayerFields.Eyes>("Avatar/Layers/Face/Eyes_Happy", Color.black)
        .WithBodyLayer<BodyLayerFields.Shirts>("Avatar/Layers/Top/T-Shirt", Color.red)
        .Build(); // Generates mugshot and applies appearance
}
```

## Basic Appearance Setup

### Setting Up Appearance

Appearance customization is done in the `OnCreated` method:

```csharp
protected override void OnCreated()
{
    base.OnCreated();
    
    // Configure appearance
    Appearance
        .Set<CustomizationFields.Gender>(0.0f) // 0=male, 1=female
        .Set<CustomizationFields.Height>(1.0f)
        .Set<CustomizationFields.Weight>(0.35f)
        .Set<CustomizationFields.SkinColor>(new Color32(150, 120, 95, 255))
        .Build(); // Always call Build() at the end
}
```

### Building the Appearance

**Important**: Always call `Build()` at the end of your appearance configuration. This:
- Generates the NPC's mugshot
- Applies the appearance to the avatar
- Finalizes the visual changes

```csharp
Appearance
    .Set<CustomizationFields.Gender>(0.5f)
    .Set<CustomizationFields.Height>(1.0f)
    .Build(); // Required!
```

## Customization Fields

### Basic Physical Properties

```csharp
// Gender (0.0 = male, 1.0 = female)
Appearance.Set<CustomizationFields.Gender>(0.0f);

// Height (0.0 = short, 1.0 = tall)
Appearance.Set<CustomizationFields.Height>(1.0f);

// Weight (0.0 = thin, 1.0 = heavy)
Appearance.Set<CustomizationFields.Weight>(0.35f);

// Skin color
Appearance.Set<CustomizationFields.SkinColor>(new Color32(150, 120, 95, 255));

// Eye color
Appearance.Set<CustomizationFields.EyeBallTint>(Color.blue);

// Hair color
Appearance.Set<CustomizationFields.HairColor>(Color.brown);

// Hair style (path to hair asset)
Appearance.Set<CustomizationFields.HairStyle>("Avatar/Hair/Spiky/Spiky");
```

### Facial Features

```csharp
// Eye properties
Appearance.Set<CustomizationFields.PupilDilation>(0.66f);
Appearance.Set<CustomizationFields.EyeBallTint>(Color.white);

// Eyebrow properties
Appearance.Set<CustomizationFields.EyebrowScale>(0.85f);
Appearance.Set<CustomizationFields.EyebrowThickness>(0.6f);
Appearance.Set<CustomizationFields.EyebrowRestingHeight>(0.1f);
Appearance.Set<CustomizationFields.EyebrowRestingAngle>(0.05f);

// Eyelid states (left, right)
Appearance.Set<CustomizationFields.EyeLidRestingStateLeft>((0.5f, 0.5f));
Appearance.Set<CustomizationFields.EyeLidRestingStateRight>((0.5f, 0.5f));
```

### Color Values

Use appropriate color types for different properties:

```csharp
// Color32 for skin (RGBA values 0-255)
Appearance.Set<CustomizationFields.SkinColor>(new Color32(150, 120, 95, 255));

// Color for eyes, hair, etc. (RGB values 0-1)
Appearance.Set<CustomizationFields.EyeBallTint>(Color.blue);
Appearance.Set<CustomizationFields.HairColor>(new Color(0.1f, 0.1f, 0.1f));
```

## Face Layers

Face layers control facial features and expressions:

```csharp
// Eyes with expression and color
Appearance.WithFaceLayer<FaceLayerFields.Eyes>("Avatar/Layers/Face/Face_Agitated", Color.black);

// Facial hair
Appearance.WithFaceLayer<FaceLayerFields.FacialHair>("Avatar/Layers/Face/Beard", Color.brown);

// Face expressions
Appearance.WithFaceLayer<FaceLayerFields.Face>("Avatar/Layers/Face/Face_Happy", Color.white);
```

### Available Face Layer Fields

- `FaceLayerFields.Eyes`: Eye expressions and styles
- `FaceLayerFields.FacialHair`: Beards, mustaches, etc.
- `FaceLayerFields.Face`: Face expressions and overlays

### Face Layer Paths

Common face layer paths:

```csharp
// Eye expressions
"Avatar/Layers/Face/Face_Agitated"
"Avatar/Layers/Face/Face_Happy"
"Avatar/Layers/Face/Face_Neutral"
"Avatar/Layers/Face/Face_Sad"

// Facial hair
"Avatar/Layers/Face/Beard"
"Avatar/Layers/Face/Mustache"
"Avatar/Layers/Face/Goatee"
```

## Body Layers

Body layers control clothing and body appearance:

```csharp
// Shirts
Appearance.WithBodyLayer<BodyLayerFields.Shirts>("Avatar/Layers/Top/T-Shirt", Color.red);
Appearance.WithBodyLayer<BodyLayerFields.Shirts>("Avatar/Layers/Top/Shirt", Color.white);
Appearance.WithBodyLayer<BodyLayerFields.Shirts>("Avatar/Layers/Top/RolledButtonUp", Color.blue);

// Pants
Appearance.WithBodyLayer<BodyLayerFields.Pants>("Avatar/Layers/Bottom/Jeans", Color.blue);
Appearance.WithBodyLayer<BodyLayerFields.Pants>("Avatar/Layers/Bottom/Jorts", new Color(0.15f, 0.2f, 0.3f));

// Undergarments
Appearance.WithBodyLayer<BodyLayerFields.Undergarments>("Avatar/Layers/Underwear/Boxers", Color.white);
```

### Available Body Layer Fields

- `BodyLayerFields.Shirts`: Tops, shirts, jackets
- `BodyLayerFields.Pants`: Bottoms, pants, shorts
- `BodyLayerFields.Undergarments`: Underwear, bras, etc.

### Body Layer Paths

Common body layer paths:

```csharp
// Tops
"Avatar/Layers/Top/T-Shirt"
"Avatar/Layers/Top/Shirt"
"Avatar/Layers/Top/RolledButtonUp"
"Avatar/Layers/Top/Hoodie"

// Bottoms
"Avatar/Layers/Bottom/Jeans"
"Avatar/Layers/Bottom/Jorts"
"Avatar/Layers/Bottom/Shorts"
"Avatar/Layers/Bottom/Sweatpants"
```

## Accessory Layers

Accessory layers control shoes, hats, and other accessories:

```csharp
// Shoes
Appearance.WithAccessoryLayer<AccessoryFields.Feet>("Avatar/Accessories/Feet/Sneakers/Sneakers", Color.red);
Appearance.WithAccessoryLayer<AccessoryFields.Feet>("Avatar/Accessories/Feet/Boots", Color.brown);

// Hats
Appearance.WithAccessoryLayer<AccessoryFields.Head>("Avatar/Accessories/Head/BaseballCap", Color.blue);

// Jewelry
Appearance.WithAccessoryLayer<AccessoryFields.Neck>("Avatar/Accessories/Neck/Necklace", Color.gold);
```

### Available Accessory Fields

- `AccessoryFields.Feet`: Shoes, boots, sneakers
- `AccessoryFields.Head`: Hats, caps, helmets
- `AccessoryFields.Neck`: Necklaces, scarves
- `AccessoryFields.Hands`: Gloves, rings
- `AccessoryFields.Waist`: Belts, holsters

### Accessory Paths

Common accessory paths:

```csharp
// Shoes
"Avatar/Accessories/Feet/Sneakers/Sneakers"
"Avatar/Accessories/Feet/Boots"
"Avatar/Accessories/Feet/Sandals"

// Hats
"Avatar/Accessories/Head/BaseballCap"
"Avatar/Accessories/Head/Beanie"
"Avatar/Accessories/Head/Hat"
```

## Random Appearance Generation

### Generate Random Appearance

For quick testing or random NPCs:

```csharp
protected override void OnCreated()
{
    base.OnCreated();
    
    // Generate completely random appearance
    Appearance.GenerateRandomAppearance();
    Appearance.Build();
}
```

### Partial Random Generation

You can set some properties and randomize others:

```csharp
Appearance
    .Set<CustomizationFields.Gender>(0.0f) // Force male
    .Set<CustomizationFields.Height>(1.0f) // Force height
    .GenerateRandomAppearance() // Randomize everything else
    .Build();
```

## Advanced Customization

### Complex Appearance Example

Here's a complete example with all customization options:

```csharp
protected override void OnCreated()
{
    base.OnCreated();
    
    Appearance
        // Core biometrics
        .Set<CustomizationFields.Gender>(0.0f) // Male
        .Set<CustomizationFields.Height>(1.0f)
        .Set<CustomizationFields.Weight>(0.35f)
        .Set<CustomizationFields.SkinColor>(new Color32(150, 120, 95, 255))
        
        // Eye properties
        .Set<CustomizationFields.EyeBallTint>(Color.white)
        .Set<CustomizationFields.PupilDilation>(0.66f)
        
        // Eyebrow properties
        .Set<CustomizationFields.EyebrowScale>(0.85f)
        .Set<CustomizationFields.EyebrowThickness>(0.6f)
        .Set<CustomizationFields.EyebrowRestingHeight>(0.1f)
        .Set<CustomizationFields.EyebrowRestingAngle>(0.05f)
        
        // Eyelid states
        .Set<CustomizationFields.EyeLidRestingStateLeft>((0.5f, 0.5f))
        .Set<CustomizationFields.EyeLidRestingStateRight>((0.5f, 0.5f))
        
        // Hair
        .Set<CustomizationFields.HairColor>(new Color(0.1f, 0.1f, 0.1f))
        .Set<CustomizationFields.HairStyle>("Avatar/Hair/Spiky/Spiky")
        
        // Face layers
        .WithFaceLayer<FaceLayerFields.Eyes>("Avatar/Layers/Face/Face_Agitated", Color.black)
        
        // Body layers
        .WithBodyLayer<BodyLayerFields.Shirts>("Avatar/Layers/Top/T-Shirt", Color.red)
        .WithBodyLayer<BodyLayerFields.Pants>("Avatar/Layers/Bottom/Jeans", new Color(0.15f, 0.2f, 0.3f))
        
        // Accessories
        .WithAccessoryLayer<AccessoryFields.Feet>("Avatar/Accessories/Feet/Sneakers/Sneakers", Color.red)
        
        .Build(); // Always call Build() at the end
}
```

### Gender-Specific Customization

You can customize based on gender:

```csharp
protected override void OnCreated()
{
    base.OnCreated();
    
    float gender = 0.5f; // 0 = male, 1 = female
    
    Appearance
        .Set<CustomizationFields.Gender>(gender);
    
    if (gender < 0.5f) // Male
    {
        Appearance
            .Set<CustomizationFields.HairStyle>("Avatar/Hair/Spiky/Spiky")
            .WithBodyLayer<BodyLayerFields.Shirts>("Avatar/Layers/Top/T-Shirt", Color.blue);
    }
    else // Female
    {
        Appearance
            .Set<CustomizationFields.HairStyle>("Avatar/Hair/Long/Long")
            .WithBodyLayer<BodyLayerFields.Shirts>("Avatar/Layers/Top/Blouse", Color.pink);
    }
    
    Appearance.Build();
}
```

## Best Practices

### Best Practices for Appearance Customization

Appearance configuration is a dynamic process and must be handled during the runtime initialization phase of the NPC.

### Do's

-   **Configure Appearance in `OnCreated()`**: Always apply appearance changes (using the `Appearance` property) within the `protected override void OnCreated()` method of your custom NPC class.
-   **Finalize Configuration with `Build()`**: The `Build()` method is mandatory to apply all pending changes to the NPC's visual state. Always call it once after defining all customization fields.
-   **Provide Error Fallbacks**: Use `GenerateRandomAppearance()` within a `catch` block to ensure the NPC is visually stable even if customization parameters fail to apply.
-   **Use Appropriate Value Types**: Ensure you pass float or Color types for customization values, as required by the specific `CustomizationFields` enum.

### Don'ts

-   **Don't Configure in `ConfigurePrefab()`**: Never attempt to set appearance or dynamic visual properties within the static `ConfigurePrefab()` method. This method is reserved for identity, schedule, and static defaults.
-   **Don't Omit `Build()`**: Appearance changes will not take effect unless `Build()` is explicitly called.
-   **Don't Use Invalid Assets/Paths**: Ensure all asset bundle paths or direct asset references are validated before deployment.

### Error Handling

Wrapping appearance configuration ensures robustness. If your custom configuration fails (e.g., due to bad parameters or missing assets), the NPC will fall back to a safe, random appearance.

```csharp
protected override void OnCreated()
{
    base.OnCreated();
    
    try
    {
        // Define dynamic appearance here
        Appearance
            .Set<CustomizationFields.Gender>(0.0f)
            .Set<CustomizationFields.Height>(1.0f)
            .Build();
    }
    catch (Exception ex)
    {
        // Use MelonLogger to report failure
        MelonLogger.Error($"Failed to set appearance for {FullName}: {ex.Message}");
        
        // Fallback: Generate a random, safe appearance
        Appearance.GenerateRandomAppearance();
        Appearance.Build();
    }
}
```


## Next Steps

Now that you understand appearance customization, explore:

- **[Scheduling System](scheduling-system.md)** - NPC movement and activities
- **[Dialogue System](dialogue-system.md)** - Interactive conversations
- **[Runtime Management](runtime-management.md)** - NPC lifecycle and properties
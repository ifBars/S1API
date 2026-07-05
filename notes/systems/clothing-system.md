---
title: "Clothing System"
description: "Reference for the clothing system — ClothingDefinition, ClothingInstance, appearance, and equipping"
status: "stable"
version: "2.0.0"
reviewed: "2026-07-03"
applies_to: "Schedule I v0.4.3+"
---

# Clothing System

## ClothingDefinition (`Il2CppScheduleOne.Clothing.ClothingDefinition`)
`StorableItemDefinition`. ScriptableObject defining a clothing item.

### Fields
| Field | Type | Description |
|-------|------|-------------|
| `Slot` | `EClothingSlot` | Which body slot this occupies |
| `ApplicationType` | `EClothingApplicationType` | How clothing is applied |
| `ClothingAssetPath` | `string` | Path to clothing model asset |
| `Colorable` | `bool` | Whether item supports color tinting |
| `DefaultColor` | `EClothingColor` | Default color for new instances |
| `SlotsToBlock` | `List<EClothingSlot>` | Slots hidden when worn |

### Methods
| Method | Description |
|--------|-------------|
| `GetDefaultInstance(int quantity)` | Returns `ClothingInstance` with default color |

---

## ClothingInstance (`Il2CppScheduleOne.Clothing.ClothingInstance`)
`StorableItemInstance`. Runtime instance of a clothing item.

### Fields
| Field | Type | Description |
|-------|------|-------------|
| `Color` | `EClothingColor` | Current color of this garment |

### Properties
| Property | Description |
|----------|-------------|
| `Name` | Base name + `" (ColorName)"` if not White |

### Methods
| Method | Description |
|--------|-------------|
| `GetCopy(int overrideQuantity)` | Clone with same color |
| `GetItemData()` | Serialize to `ClothingData` |
| `Write(Writer)` | FishNet serialization |
| `Read(Reader)` | FishNet deserialization |

---

## EClothingSlot Enum
| Value | Index | Body Area |
|-------|-------|-----------|
| `Feet` | 0 | Shoes |
| `Bottom` | 1 | Pants |
| `Waist` | 2 | Belt/Waist |
| `Top` | 3 | Shirt |
| `Outerwear` | 4 | Jacket/Coat |
| `Hands` | 5 | Gloves |
| `Neck` | 6 | Necklace/Scarf |
| `Eyes` | 7 | Glasses/Goggles |
| `Head` | 8 | Hat/Helmet |
| `Wrist` | 9 | Bracelet/Watch |

---

## EClothingColor Enum
| Value | Index | Value | Index | Value | Index |
|-------|-------|-------|-------|-------|-------|
| White | 0 | Orange | 8 | SkyBlue | 18 |
| LightGrey | 1 | Tan | 9 | Blue | 19 |
| DarkGrey | 2 | Brown | 10 | DeepBlue | 20 |
| Charcoal | 3 | Coral | 11 | Navy | 21 |
| Black | 4 | Beige | 12 | DeepPurple | 22 |
| LightRed | 5 | Yellow | 13 | Purple | 23 |
| Red | 6 | Lime | 14 | Magenta | 24 |
| Crimson | 7 | LightGreen | 15 | BrightPink | 25 |
| | | DarkGreen | 16 | HotPink | 26 |
| | | Cyan | 17 | | |

---

## EClothingApplicationType Enum
Defines how clothing is visually applied to the avatar.

---

## ClothingUtility (`Il2CppScheduleOne.Clothing.ClothingUtility`)
`Singleton<ClothingUtility>`. Maps enums to display data.

### Nested Classes
| Class | Fields | Purpose |
|-------|--------|---------|
| `ColorData` | `ColorType`, `ActualColor`, `LabelColor` | Color → Unity Color mapping |
| `ClothingSlotData` | `Slot`, `Name`, `Icon` | Slot → display name + icon |

### Methods
| Method | Description |
|--------|-------------|
| `GetColorData(EClothingColor)` | Get Unity color for enum |
| `GetSlotData(EClothingSlot)` | Get slot display data |

### Fields
| Field | Type | Description |
|-------|------|-------------|
| `ColorDataList` | `List<ColorData>` | All color mappings |
| `ClothingSlotDataList` | `List<ClothingSlotData>` | All slot display data |

---

## EClothingColor Extension Methods

### `GetLabel()` (in `ClothingColorExtensions`)
Returns human-readable name for each `EClothingColor` value.

---

## Usage Patterns

### Creating a clothing item
```csharp
ClothingDefinition def = Registry.GetItem<ClothingDefinition>("cap");
ClothingInstance item = new ClothingInstance(def, 1, EClothingColor.Red);
```

### Checking player equipped clothing
```csharp
PlayerClothing clothing = Player.Local.Clothing;
// Access via ClothingManager
```

### Checking slot compatibility
```csharp
ClothingDefinition def = Registry.GetItem<ClothingDefinition>("jacket");
if (def.Slot == EClothingSlot.Outerwear)
{
    // It's a jacket
}
// Check blocked slots:
foreach (EClothingSlot blocked in def.SlotsToBlock)
{
    // Slots hidden when wearing this
}
```

## S1Toolkit API

Instead of raw Il2Cpp classes, the [S1Toolkit API](../api/api-reference.md) can be used:

Clothing-API in Entwicklung.

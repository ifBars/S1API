---
title: "Data References"
description: "Structured data references for item IDs, product definitions, mix recipes, and other game constants"
status: "stable"
version: "2.0.0"
reviewed: "2026-07-03"
applies_to: "Schedule I v0.4.3+"
---

# Data References

## Item IDs (Registry)

### Products
| ID | Type | Description |
|----|------|-------------|
| `ogkush` | WeedDefinition | OG Kush |
| `sour_diesel` | WeedDefinition | Sour Diesel |
| `green_crack` | WeedDefinition | Green Crack |
| `grandaddy_purple` | WeedDefinition | Granddaddy Purple |
| `cocaine` | CocaineDefinition | Cocaine |
| `meth` | MethDefinition | Methamphetamine |
| `shrooms` | ShroomDefinition | Shrooms |
| `liquid_meth` | (liquid) | Liquid Meth |

### Packaging
| ID | Type | Description |
|----|------|-------------|
| `baggie` | PackagingDefinition | Baggie (Qty: 1) |
| `jar` | PackagingDefinition | Jar (Qty: 5) |
| `brick` | PackagingDefinition | Brick (Qty: 20) |

### Seeds
| ID | Description |
|----|-------------|
| `weed_seed` | Weed seed |
| `coca_seed` | Coca seed |
| `shroom_spores` | Mushroom spores |

### Additives
| ID | Description | Effect |
|----|-------------|--------|
| `fertilizer_basic` | Basic fertilizer | +Quality |
| `fertilizer_advanced` | Advanced fertilizer | +Quality +Yield |
| `growth_booster` | Growth booster | +Instant growth |

### Tools
| ID | Description |
|----|-------------|
| `trimmers` | Trimming tool |
| `watering_can` | Watering can |
| `skateboard` | Skateboard |

### Weapons
| ID | Description |
|----|-------------|
| `baseball_bat` | Baseball Bat |
| `pistol` | Pistol |
| `revolver` | Revolver |
| `shotgun` | Shotgun |
| `rifle` | Rifle |
| `ak47` | AK-47 |
| `minigun` | Minigun |
| `m1911` | M1911 |
| `m1911_gold` | M1911 (Gold) |

### Clothing (examples)
| ID | Slot |
|----|------|
| `cap` | Head |
| `beanie` | Head |
| `sunglasses` | Eyes |
| `shirt` | Top |
| `jacket` | Outerwear |
| `pants` | Bottom |
| `shoes` | Feet |

---

## NPC IDs

### Main NPCs
| ID | Name | Role |
|----|------|------|
| `uncle_nelson` | Uncle Nelson | Tutorial |
| `billy` | Billy | Tutorial |
| `dan` | Dan | Tutorial |
| `jen` | Jen | Contact |
| `ming` | Ming | Contact |
| `oscar` | Oscar | Contact |
| `sam` | Sam | Contact |
| `thomas_benzies` | Thomas Benzies | Contact |
| `greg_fliggle` | Greg Fliggle | Storage Unit |
| `shirley_watts` | Shirley Watts | Supplier (Pseudo) |
| `fungal_phil` | Fungal Phil | Supplier (Shrooms) |
| `salvador` | Salvador | Supplier |

### Dealer NPCs
| ID Pattern | Example |
|------------|---------|
| `dealer_*` | `dealer_mike`, `dealer_jessica` |

### Customer NPCs
Generated at runtime with unique IDs.

---

## Property Codes

| Code | Name | Type |
|------|------|------|
| `bungalow` | Bungalow | Residential |
| `manor` | Manor | Residential |
| `motel_room` | Motel Room | Residential |
| `rv` | RV | Residential |
| `west_storage` | West Storage Unit | Storage |
| `sewer_office` | Sewer Office | Base of Operations |
| `sweatshop` | Sweatshop | Production |
| (warehouses) | Various | Production/Storage |

---

## Console Commands
Accessible via in-game console (`~` or debug build).

### General
| Command | Args | Description |
|---------|------|-------------|
| `settime` | `HHMM` | Set time of day |
| `give` | `item_id [quantity]` | Spawn item |
| `changecash` | `amount` | Add/remove cash |
| `sethealth` | `amount` | Set player health |
| `teleport` | `x y z` | Teleport player |
| `spawnvehicle` | `vehicle_code` | Spawn vehicle |
| `setspeed` | `multiplier` | Set time speed |

### Cheats
| Command | Description |
|---------|-------------|
| `unlockall` | Unlock all regions |
| `completeallquests` | Complete every quest |
| `setrank` | `rank_name` Set player rank |
| `setwanted` | Set wanted level 0-3 |

### Debug
| Command | Description |
|---------|-------------|
| `debug_ai` | Toggle AI debug |
| `debug_network` | Toggle network debug |
| `dump_inventory` | Print inventory to log |

---

## Quest GUIDs

| GUID | Quest |
|------|-------|
| `welcome_to_hyland_point` | Welcome to Hyland Point |
| `getting_started` | Getting Started |
| `we_need_to_cook` | We Need To Cook |
| `down_to_business` | Down To Business |
| `needing_the_green` | Needing The Green |
| `on_the_grind` | On The Grind |
| `gearing_up` | Gearing Up |
| `connections` | Connections |
| `securing_supplies` | Securing Supplies |
| `expanding_operations` | Expanding Operations |
| `warehouse` | Warehouse |
| `grow_shrooms` | Grow Shrooms |
| `moving_up` | Moving Up |
| `employees` | Employees |
| `botanists` | Botanists |
| `chemists` | Chemists |
| `cleaners` | Cleaners |
| `packagers` | Packagers |
| `clean_cash` | Clean Cash |
| `unfavourable_agreements` | Unfavourable Agreements |
| `deal_for_cartel` | Deal For Cartel |
| `the_deep_end` | The Deep End |
| `sink_or_swim` | Sink Or Swim |
| `defeat_cartel` | Defeat The Cartel |

---

## Achievement IDs (Steam)

| ID | Name | Condition |
|----|------|-----------|
| (13 achievements) | Unlock requirements | Various progression |

---

## Scene Names

| Scene | Description |
|-------|-------------|
| `Main` | Main game world (Hyland Point) |
| `Tutorial` | Tutorial island/scene |
| `MainMenu` | Main menu |
| `LoadingScreen` | Loading transition |

---

## Layer Names (Interaction)

| Layer | Used For |
|-------|----------|
| `Interaction` | Default interactable layer |
| `IgnoreInteraction` | Objects ignoring interaction raycasts |
| `Buildable` | Buildable object detection |
| `Surface` | Surface placement detection |

---

## Key Tags

| Tag | Used On |
|-----|---------|
| `Interactable` | InteractableObject root |
| `Player` | Player GameObject |
| `MainCamera` | Main camera |
| `Buildable` | Buildable preview/ghost |

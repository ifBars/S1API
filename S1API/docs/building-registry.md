# Building Registry

`S1API.Map.Building` wraps enterable structures so mods can look up buildings
without referencing game assemblies directly. Entries are registered automatically when
`NPCEnterableBuilding.Awake` fires and cleared on scene unload.

## Building Model

Each `Building` exposes the following data:

- `Name`: Display name registered by the base game.
- `ResolveGameBuilding()`: Returns the live `NPCEnterableBuilding` (or derived) instance.
- `BuildingGUID`: Not stored directly; resolve the live object and inspect its GUID if required.

The registry is
held in-memory only, so treat lookups as runtime references rather than persistent
identifiers.

## Lookup Helpers

```csharp
// Dump all buildings sorted by name
foreach (var building in Building.GetAll())
{
    MelonLogger.Msg($"Building: {building.Name}");
}

// Grab a specific building by its typed identifier
var building = Building.Get<NorthApartments>();
```

### Typed Identifiers

Typed identifiers annotated with `[Buildings.BuildingName("...")]` are the preferred
way to access buildings. Browsing the `S1API.Map.Buildings` namespace reveals every
identifier shipped with the game.

```csharp
[BuildingName("North apartments")]
public sealed class NorthApartments : IBuildingIdentifier { }
```

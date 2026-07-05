---
title: "S1MAPI Building API"
description: "Complete reference for the S1MAPI building framework — procedural buildings, GLTF imports, terrain modification, and navigation mesh generation"
status: "stable"
version: "2.0.0"
reviewed: "2026-07-03"
applies_to: "Schedule I v0.4.5+ (IL2CPP/Mono)"
---

# S1MAPI Building API

!!! info
    S1MAPI (by ifBars) is a mapping and construction library for Schedule I mods.
    It provides a complete building framework including procedural mesh generation,
    GLTF model import, wall/roof/interior construction, terrain flattening, and
    navigation mesh generation — without requiring Unity AssetBundles.
    
    Official docs: [ifbars.github.io/S1MAPI](https://ifbars.github.io/S1MAPI/)

## Installation

Place the correct DLL in the `UserLibs/` folder (not `Mods/`):

| Branch | File |
|--------|------|
| IL2CPP (default Steam) | `S1MAPI_Il2Cpp.dll` |
| Mono (alternate branch) | `S1MAPI_Mono.dll` |

Other mods can then declare S1MAPI as a dependency and use its API at runtime.

## Core Namespaces

| Namespace | Purpose |
|-----------|---------|
| `S1MAPI.Building` | High-level building creation (`BuildingBuilder`) |
| `S1MAPI.Building.Structural` | Walls, roofs, floors, terrain, decor |
| `S1MAPI.Building.Interior` | Interior rooms, furniture, navigation |
| `S1MAPI.Building.Components` | Lighting, prefab placement, networking |
| `S1MAPI.Building.Config` | Building configuration, palettes, parts |
| `S1MAPI.Gltf` | GLTF/GLB model loading and processing |
| `S1MAPI.ProceduralMesh` | Runtime mesh generation (primitives, organic shapes) |
| `S1MAPI.Extensions` | Unity component/mesh/material extensions |
| `S1MAPI.S1` | Schedule I game asset references (materials, meshes, prefabs) |
| `S1MAPI.Utils` | Embedded resource loading, material presets |

---

## BuildingBuilder — High-Level Construction

The `BuildingBuilder` class provides a fluent API for creating complete buildings.

### Basic Usage

```csharp
using S1MAPI.Building;
using S1MAPI.Building.Config;
using UnityEngine;

// Create a building with default config
var builder = new BuildingBuilder("MyBuilding")
    .WithConfig(BuildingConfig.Default)
    .WithPalette(BuildingPalette.Modern)
    .DefineRoom(width: 8f, height: 3f, depth: 6f);

// Add structural elements
builder.AddFloor(color: new Color(0.3f, 0.3f, 0.3f))
       .AddCeiling()
       .BuildWalls(openingStyle: WallOpeningType.Doorway)
       .AddRoof(parapetPreset: ParapetPreset.Flat);

// Place at world position
var building = builder.Build();
building.transform.position = new Vector3(100f, 0f, 100f);
```

### Fluent Configuration

| Method | Description |
|--------|-------------|
| `WithConfig(BuildingConfig)` | Set building configuration |
| `WithPalette(BuildingPalette)` | Set color/material palette |
| `DefineRoom(width, height, depth)` | Set room dimensions |
| `AddFloor(color, material)` | Add floor plane |
| `AddCeiling(color, material)` | Add ceiling plane |
| `BuildWalls(openingStyle)` | Build walls with optional openings |
| `AddRoof(parapetPreset)` | Add roof with parapet style |
| `AddInteriorWalls(axis, placements)` | Add interior partition walls |
| `Build()` | Finalize and return the root GameObject |

### BuildingConfig

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Width` | `float` | 8 | Room width |
| `Height` | `float` | 3 | Room height |
| `Depth` | `float` | 6 | Room depth |
| `FloorThickness` | `float` | 0.1 | Floor/ceiling thickness |
| `WallThickness` | `float` | 0.15 | Wall thickness |
| `Palette` | `BuildingPalette` | Default | Color/material scheme |
| `Size` | `Vector3` | (8,3,6) | Combined room size |

### BuildingPalette

| Property | Type | Description |
|----------|------|-------------|
| `FloorColor` | `Color` | Floor tint color |
| `WallColor` | `Color` | Wall tint color |
| `CeilingColor` | `Color` | Ceiling tint color |
| `RoofColor` | `Color` | Roof tint color |
| `FloorMaterial` | `Material` | Custom floor material |
| `WallMaterial` | `Material` | Custom wall material |

Predefined palettes: `BuildingPalette.Modern`, `BuildingPalette.Industrial`, `BuildingPalette.Default`

---

## Structural Components

### WallBuilder

Builds exterior walls with configurable openings:

```csharp
// Builder method (recommended)
builder.BuildWalls(openingStyle: WallOpeningType.Doorway);

// Direct usage
var wallBuilder = new WallBuilder(config);
wallBuilder.BuildExteriorWalls(root, roomSize, wallThickness);

// With custom openings
var openings = new WallOpening[]
{
    new() { Position = new Vector3(2f, 0f, 0f), Width = 1f, Height = 2.2f, Type = WallOpeningType.Doorway },
    new() { Position = new Vector3(-1f, 1.5f, 0f), Width = 1f, Height = 1f, Type = WallOpeningType.Window }
};
wallBuilder.BuildWithOpenings(root, roomSize, wallThickness, openings);
```

### InteriorWallBuilder

Adds interior partition walls:

```csharp
builder.AddInteriorWalls(InteriorWallAxis.X, new InteriorWallDefinition[]
{
    new() { Position = 3f, Opening = new DoorwayInfo { Width = 1f, Height = 2.2f } }
});
```

### RoofBuilder

```csharp
builder.AddRoof(parapetPreset: ParapetPreset.Flat);

// Available presets
// ParapetPreset.Flat      — flat roof with low edge
// ParapetPreset.Pitched   — angled roof
// ParapetPreset.None      — no parapet
```

### DecorBuilder

Adds floors, ceilings, and decorative elements:

```csharp
var decor = new DecorBuilder(config);
decor.AddFloor(floorThickness);
decor.AddCeiling(floorThickness);
```

### TerrainFlattener

Flattens terrain under a building footprint:

```csharp
var flattener = new TerrainFlattener();
flattener.Flatten(centerPosition, footprintSize, targetHeight);
```

### TerrainClearer

Removes trees and obstacles from a building footprint:

```csharp
var clearer = new TerrainClearer();
clearer.ClearArea(centerPosition, radius);
```

---

## Interior & Navigation

### InteriorBuilder

Creates interior rooms with furniture placement:

```csharp
var interior = new InteriorBuilder(config);
interior.CreateRoom(root, roomSize, roomName);
```

### FurnitureBuilder

Places furniture items inside rooms:

```csharp
var furniture = new FurnitureBuilder();
furniture.Place(FurnitureType.Table, position, rotation);
furniture.Place(FurnitureType.Chair, position + Vector3.right * 0.5f, rotation);
furniture.Place(FurnitureType.Shelf, wallPosition, rotation);

// Available furniture types
// FurnitureType.Table, Chair, Shelf, Bed, Desk, Counter, Cabinet, Lamp, Plant
```

### NavigationBuilder

Generates navigation mesh for NPC pathfinding:

```csharp
var navBuilder = new NavigationBuilder();
navBuilder.BuildNavMesh(buildingBounds);
navBuilder.AddDoorwayLink(doorPosition, doorWidth);
```

### InteriorNavigator / InteriorPathGrid

Creates a path grid for NPC movement inside the building:

```csharp
var pathGrid = new InteriorPathGrid(roomSize, gridCellSize);
var navigator = new InteriorNavigator(pathGrid);
navigator.RegisterWalkableArea(bounds);
navigator.RegisterObstacle(obstacleBounds);
```

---

## GLTF/GLB Model Import

Full GLTF/GLB parser for loading custom 3D models at runtime:

```csharp
using S1MAPI.Gltf;
using S1MAPI.Utils;

// Load from embedded resource
byte[] data = EmbeddedResourceLoader.Load("MyMod.Models.building.glb");
var importer = new GltfImporter()
    .SetName("CustomBuilding")
    .SetScale(1f)
    .SetShader(Shader.Find("Universal Render Pipeline/Lit"))
    .GenerateNormals(true);

GameObject model = importer.Load(data);
model.transform.position = worldPosition;

// Load from file
GameObject fromFile = new GltfImporter()
    .SetName("ImportedModel")
    .LoadFromFile(@"C:\Models\structure.glb");
```

### GltfImporter Options

| Method | Default | Description |
|--------|---------|-------------|
| `SetName(string)` | "GltfModel" | Root GameObject name |
| `SetScale(float)` | 1.0 | Model scale multiplier |
| `SetShader(Shader)` | URP/Lit | Material shader |
| `ImportAnimations(bool)` | false | Import GLTF animations |
| `ImportSkins(bool)` | false | Import skinned meshes |
| `ImportBlendShapes(bool)` | false | Import blend shapes |
| `ReadableMeshes(bool)` | false | Keep mesh data accessible |
| `GenerateNormals(bool)` | true | Generate missing normals |
| `GenerateTangents(bool)` | false | Generate missing tangents |
| `SetEmissionIntensity(float)` | 1.0 | Emission strength |

### Loading Methods

| Method | Input | Use Case |
|--------|-------|----------|
| `Load(byte[])` | Raw bytes | Embedded resources |
| `LoadGlb(byte[])` | GLB binary | Binary format models |
| `LoadFromFile(string)` | File path | External model files |
| `LoadGltfJson(string, string)` | JSON string | Text format GLTF |

---

## Procedural Mesh Generation

Generate meshes at runtime without external assets:

### PrimitiveBuilder

```csharp
using S1MAPI.ProceduralMesh;

Mesh box = PrimitiveBuilder.CreateBox(width, height, depth);
Mesh cylinder = PrimitiveBuilder.CreateCylinder(radius, height, segments);
Mesh sphere = PrimitiveBuilder.CreateSphere(radius, segments);
```

### CustomMeshBuilder

```csharp
var meshBuilder = new CustomMeshBuilder();
meshBuilder.AddVertex(position, normal, uv);
meshBuilder.AddTriangle(a, b, c);
meshBuilder.AddQuad(a, b, c, d);
Mesh result = meshBuilder.Build();
```

### ProceduralMeshBuilder

Higher-level procedural generation:

```csharp
var proc = new ProceduralMeshBuilder();
proc.AddBox(center, size);
proc.AddCylinder(position, radius, height);
proc.BuildCombined(); // Merge into single mesh
```

### AdvancedMeshOperations

```csharp
Mesh merged = AdvancedMeshOperations.Combine(meshes);
Mesh sliced = AdvancedMeshOperations.Slice(mesh, plane);
Mesh extruded = AdvancedMeshOperations.Extrude(shape, distance);
```

### Organic Shape Generators

```csharp
// Body/Limb profiles for organic shapes
var body = new BodyProfile(width, height, depth);
var limb = new LimbProfile(length, width);
var organic = new OrganicShapeGenerator();
Mesh creature = organic.Generate(body, limbs);
```

---

## Extensions

### MeshExtensions

```csharp
mesh.RecalculateNormals();
mesh.RecalculateTangents();
mesh.Scale(factor);
mesh.Combine(otherMesh);
mesh.Transform(matrix);
```

### MaterialExtensions

```csharp
material.SetBaseColor(color);
material.SetMetallic(value);
material.SetSmoothness(value);
material.SetEmission(color, intensity);
material.MakeTransparent();
```

### GameObjectExtensions

```csharp
gameObject.SetLayerRecursively(layer);
gameObject.SetTagRecursively(tag);
gameObject.DestroyChildren();
gameObject.GetBounds();
```

### NavMeshExtensions

```csharp
NavMeshSurface surface = gameObject.AddNavMeshSurface();
surface.BuildNavMesh();
```

### TransformExtensions

```csharp
transform.ResetTransform();
transform.SetGlobalScale(scale);
transform.FlattenToY(yPosition);
```

---

## Utilities

### EmbeddedResourceLoader

Load embedded resources from the mod assembly:

```csharp
byte[] data = EmbeddedResourceLoader.Load("MyMod.Resources.model.glb");
string text = EmbeddedResourceLoader.LoadText("MyMod.Resources.config.json");
Texture2D tex = EmbeddedResourceLoader.LoadTexture("MyMod.Resources.texture.png");
```

### MaterialPresets

```csharp
Material metal = MaterialPresets.Metal;
Material wood = MaterialPresets.Wood;
Material concrete = MaterialPresets.Concrete;
Material glass = MaterialPresets.Glass;
```

### PrefabPlacer (Networked)

For placing networked prefabs:

```csharp
var placer = new PrefabPlacer();
placer.Place(prefab, position, rotation);
placer.LinkToNetwork(networkObject);
```

---

## Integration with Schedule I

### Using Game Materials

```csharp
using S1MAPI.S1;

// Access game assets
Material floorMat = Materials.FloorTile;
Material wallMat = Materials.WallPlaster;
Mesh windowMesh = Meshes.WindowFrame;
GameObject doorPrefab = Prefabs.InteriorDoor;
```

### Building Part Registry

Track building parts for save/load:

```csharp
var registry = new BuildingPartRegistry();
registry.Register(BuildingPart.Floor, floorGo);
registry.Register(BuildingPart.Wall, wallGo);
registry.Register(BuildingPart.Roof, roofGo);

BuildingPart part = registry.GetPartType(gameObject);
```

---

## Complete Example: Custom Shop Building

```csharp
public GameObject BuildCornerShop(Vector3 position)
{
    var builder = new BuildingBuilder("CornerShop")
        .WithConfig(new BuildingConfig
        {
            Width = 10f, Height = 3.5f, Depth = 8f,
            WallThickness = 0.15f,
            Palette = BuildingPalette.Modern
        })
        .AddFloor(color: new Color(0.4f, 0.4f, 0.4f))
        .AddCeiling(color: Color.white)
        .BuildWalls(openingStyle: WallOpeningType.Doorway)

        // Add a large window on the front
        .AddInteriorWalls(InteriorWallAxis.X, new[]
        {
            new InteriorWallDefinition
            {
                Position = 3f,
                Opening = new DoorwayInfo { Width = 2f, Height = 2.5f }
            }
        })
        .AddRoof(parapetPreset: ParapetPreset.Flat);

    // Build and position
    var building = builder.Build();
    building.transform.position = position;

    // Flatten terrain
    var flattener = new TerrainFlattener();
    flattener.Flatten(position, new Vector3(12f, 0f, 10f), 0f);

    // Add interior furniture
    var furniture = new FurnitureBuilder();
    furniture.Place(FurnitureType.Counter, position + new Vector3(0f, 0f, 2f), Quaternion.identity);
    furniture.Place(FurnitureType.Shelf, position + new Vector3(3f, 0f, -1f), Quaternion.identity);

    // Build navmesh for NPCs
    var navBuilder = new NavigationBuilder();
    navBuilder.BuildNavMesh(new Bounds(position, new Vector3(12f, 4f, 10f)));

    return building;
}
```

# Save System

S1API provides a simple, attribute-based persistence model. Inherit from `S1API.Saveables.Saveable` and annotate fields with `SaveableField`.

## Basics

```csharp
using S1API.Saveables;

public class NotesSave : Saveable
{
    [SaveableField("notes")] private List<Note> _notes = new();

    protected override void OnLoaded()
    {
        // Apply loaded data to runtime managers, UI, etc.
    }

    protected override void OnSaved()
    {
        // Optional: clear transient caches
    }
}
```

S1API uses JSON with standard `JsonSerializerSettings` that ignore reference loops and support GUID references.

## Migration from the internal namespace

`S1API.Internal.Abstraction.Saveable` is obsolete with a compiler warning. Existing compiled mods continue to use the compatibility base, which forwards to `S1API.Saveables.Saveable`. Its removal is planned for a future breaking release; no removal version has been set.

When updating a standalone saveable, replace the old namespace import with `using S1API.Saveables;`. If you need both namespaces, qualify the base as `S1API.Saveables.Saveable` or use an alias to avoid an ambiguous `Saveable` reference. Keep your class name, namespace, and `SaveableField` keys unchanged: changing the base alone does not change existing save folders or JSON keys.

NPC and quest subclasses continue to inherit their persistence support from their existing API base classes and need no migration.

## Load Order Control

By default, saveables load **after** base game ISaveables (NPCs, buildings, vehicles, etc.). Override the `LoadOrder` property to control timing:

```csharp
public class MyEarlySaveable : Saveable
{
    // Override to load BEFORE base game entities
    public override SaveableLoadOrder LoadOrder => SaveableLoadOrder.BeforeBaseGame;
    
    [SaveableField("early_data")]
    private MyData _data = new MyData();
    
    protected override void OnLoaded()
    {
        // Note: base game entities are NOT loaded yet when using BeforeBaseGame
        // Use for early initialization, hooks, or global state setup
    }
}
```

### When to use each load order:

**`SaveableLoadOrder.AfterBaseGame` (default)**
- Recommended for most mods
- Base game entities (NPCs, buildings, vehicles) **are loaded** when `OnLoaded()` is called
- Use when your mod data references or modifies base game entities
- Example: storing NPC relationships, building modifications, inventory extensions

**`SaveableLoadOrder.BeforeBaseGame`**
- Advanced use case only
- Base game entities **are NOT loaded** when `OnLoaded()` is called
- Use when you need to set up state before base game loaders run
- Example: global configuration, early hooks, pre-load initialization

**Important:** All saveables are saved at the same time (after base game save), regardless of load order.

## Dynamic save format

`Saveable` also supports the base game's dynamic consolidated JSON via `SaveToDynamic` and `LoadFromDynamic` for systems that use `DynamicSaveData` (e.g. NPCs).

## Migration from legacy registry

`ModSaveableRegistry` is obsolete, do NOT use it. Subclasses of `Saveable` are auto-discovered and handled by S1API's save pipeline.
Requirements:

- Your saveable class must directly inherit `Saveable` (classes that inherit from `Saveable`, like `NPC`, are handled internally by the API).
- It must be non-abstract and have a parameterless constructor.
- S1API will create one instance per `Saveable` type and call its lifecycle methods during save/load.


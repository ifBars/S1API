# Save System

S1API provides a simple, attribute-based persistence model. Inherit from `Internal.Abstraction.Saveable` and annotate fields with `SaveableField`.

## Basics

```csharp
using S1API.Internal.Abstraction;
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

## Dynamic save format

`Saveable` also supports the base game's dynamic consolidated JSON via `SaveToDynamic` and `LoadFromDynamic` for systems that use `DynamicSaveData` (e.g. NPCs).

## Migration from legacy registry

`ModSaveableRegistry` is obsolete, do NOT use it. Subclasses of `Saveable` are auto-discovered and handled by S1API's save pipeline.
Requirements:

- Your saveable class must directly inherit `Saveable` (classes that inherit from `Saveable`, like `NPC`, are handled internally by the API).
- It must be non-abstract and have a parameterless constructor.
- S1API will create one instance per `Saveable` type and call its lifecycle methods during save/load.


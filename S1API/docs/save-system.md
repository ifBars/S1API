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

`Saveable` also supports the base game's dynamic consolidated JSON via `SaveToDynamic` and `LoadFromDynamic` for systems that use `DynamicSaveData`.

## Migration from legacy registry

`ModSaveableRegistry` is obsolete. Subclasses of `Saveable` are auto-discovered; remove manual registration calls.


# S1API Quickstart

Create a small Schedule One mod with S1API by referencing the package, subscribing to a lifecycle event, and logging from inside the game runtime.

## Create a mod project

Start from [S1APITemplate](https://github.com/ifBars/S1APITemplate) when you want the fastest path. For a manual project, reference `S1API.Forked` and your mod loader packages, then build your mod assembly as usual.

```xml
<ItemGroup>
  <PackageReference Include="S1API.Forked" Version="*" />
</ItemGroup>
```

## Hook into the game lifecycle

Use S1API lifecycle hooks instead of guessing when game systems are ready.

```csharp
using MelonLoader;
using S1API.Lifecycle;
using S1API.Logging;

public sealed class MyFirstS1Mod : MelonMod
{
    public override void OnInitializeMelon()
    {
        GameLifecycle.OnSaveLoaded += OnSaveLoaded;
    }

    private static void OnSaveLoaded()
    {
        Log.Info("S1API is loaded and the save is ready.");
    }
}
```

## Add one real feature

Once the lifecycle hook works, choose the module that matches your mod:

- Create a character with [NPCs](custom-npcs.md).
- Add a quest with [Quests](quests-system.md).
- Register content with [Items](items.md).
- Build an in-game app with [Phone Apps](phone-app.md).
- Persist state with [Saveables](save-system.md).

## Validate in game

Build your mod, place the output DLL in the expected mod folder, and launch Schedule One. Check the MelonLoader console for your S1API log line before adding more behavior.

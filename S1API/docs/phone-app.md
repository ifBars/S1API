# Phone Apps

S1API provides a robust framework for building in-game phone apps via `S1API.PhoneApp.PhoneApp`.
Apps integrate with the native Home Screen, spawn icons, and manage open/close state.

## Lifecycle

- Derive from `PhoneApp`
- Do not manually register; S1API auto-discovers `PhoneApp` subclasses when the phone `HomeScreen` starts
- Implement `OnCreatedUI(GameObject container)` to build your UI
- Optionally override `OnPhoneClosed()` and `Exit(ExitAction exit)` for UX

## Minimal example

```csharp
using UnityEngine;
using UnityEngine.UI;
using S1API.PhoneApp;
using S1API.UI;

public class HelloWorldApp : PhoneApp
{
    public static HelloWorldApp Instance;
    protected override string AppName => "HelloWorld";
    protected override string AppTitle => "Hello World";
    protected override string IconLabel => "Hello";
    protected override string IconFileName => "hello.png"; // put this image next to your mod dll

    protected override void OnCreated()
    {
        base.OnCreated();
        Instance = this;
    }

    protected override void OnCreatedUI(GameObject container)
    {
        var panel = UIFactory.Panel("MainPanel", container.transform, new Color(0.1f, 0.1f, 0.1f), fullAnchor: true);
        UIFactory.Text("Title", "📱 Hello, S1API!", panel.transform, 22, TextAnchor.MiddleCenter);
    }
}
```

Registration is automatic:

- Ensure your app type is `public`.
- S1API will instantiate it, register it, and spawn its UI/icon at runtime.

## Orientation

Override `Orientation` to `Vertical` for portrait-style apps. S1API adjusts phone rotation and camera offset accordingly.

## Icons

- Preferred: provide `IconSprite` dynamically via `SetIconSprite` / `SetIconTexture`
- Fallback: specify `IconFileName` to load from your mods/plugins folder

## Best practices

- Keep `AppName` unique
- Use `UIFactory` helpers to match native look
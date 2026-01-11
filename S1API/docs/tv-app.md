## TV Apps

S1API provides a framework for building custom TV applications via `S1API.TVApp.TVApp`.
Apps integrate with the native TV Home Screen, spawn icons and buttons, and manage open/close/pause states.

## Lifecycle

- Derive from `TVApp`
- Do not manually register; S1API auto-discovers `TVApp` subclasses when the TV `HomeScreen` starts
- Implement the abstract properties: `AppName`, `AppTitle`, and `Icon`
- Implement `OnCreatedUI(GameObject container)` to build your UI
- Optionally override `OnOpened()`, `OnClosed()`, `OnUpdate()`, `OnPaused()`, and `OnResumed()` for app logic

## Abstract Properties

| Property | Type | Description |
|----------|------|-------------|
| `AppName` | `string` | Unique identifier for this TV app |
| `AppTitle` | `string` | Display title shown on the TV app button |
| `Icon` | `Sprite` | Icon sprite displayed on the TV app button |

## Lifecycle Hooks

| Method | Description |
|--------|-------------|
| `OnCreatedUI(GameObject container)` | **Required.** Build your UI inside the provided container |
| `OnOpened()` | Called when the app is opened |
| `OnClosed()` | Called when the app is closed |
| `OnUpdate()` | Called every frame while the app is open and not paused |
| `OnPaused()` | Called when the app is paused |
| `OnResumed()` | Called when the app is resumed from pause |

## Public Methods

| Method | Description |
|--------|-------------|
| `Open()` | Opens this TV application |
| `Close()` | Closes this TV application and returns to the TV home screen |
| `Pause()` | Pauses the TV application |
| `Resume()` | Resumes the TV application from pause |

## Public Properties

| Property | Type | Description |
|----------|------|-------------|
| `IsOpen` | `bool` | Whether the app is currently open |
| `IsPaused` | `bool` | Whether the app is currently paused |

## Minimal Example

```csharp
using UnityEngine;
using UnityEngine.UI;
using S1API.TVApp;
using S1API.UI;

public class HelloWorldTVApp : TVApp
{
    // Define app metadata. These properties are used by S1API to register and display your app.
    protected override string AppName => "HelloWorld";
    protected override string AppTitle => "Hello World";
    protected override Sprite Icon => _cachedIcon ??= CreateIcon();

    private Text? _messageText;
    private static Sprite? _cachedIcon;

    // OnCreatedUI is called when the app's UI is created.
    // Build your UI inside the provided container.
    protected override void OnCreatedUI(GameObject container)
    {
        // Create background with explicit sizeDelta (required for WorldSpace canvas)
        var background = new GameObject("Background");
        background.transform.SetParent(container.transform, false);

        var bgRT = background.AddComponent<RectTransform>();
        bgRT.anchorMin = new Vector2(0.5f, 0.5f);
        bgRT.anchorMax = new Vector2(0.5f, 0.5f);
        bgRT.pivot = new Vector2(0.5f, 0.5f);
        bgRT.sizeDelta = new Vector2(500, 350);
        bgRT.anchoredPosition = Vector2.zero;

        var bgImg = background.AddComponent<RawImage>();
        bgImg.texture = CreateSolidTexture(new Color(0.05f, 0.05f, 0.15f, 1f));
        bgImg.raycastTarget = false;

        // Create "Hello World" text centered on screen
        _messageText = UIFactory.Text(
            "HelloText",
            "Hello World!",
            container.transform,
            48,
            TextAnchor.MiddleCenter,
            FontStyle.Bold
        );

        var textRT = _messageText.GetComponent<RectTransform>();
        textRT.anchorMin = new Vector2(0.5f, 0.5f);
        textRT.anchorMax = new Vector2(0.5f, 0.5f);
        textRT.pivot = new Vector2(0.5f, 0.5f);
        textRT.sizeDelta = new Vector2(400, 100);
        textRT.anchoredPosition = Vector2.zero;
    }

    // Called when the app is opened
    protected override void OnOpened()
    {
        if (_messageText != null)
            _messageText.color = Color.white;
    }

    // Called every frame while the app is open
    protected override void OnUpdate()
    {
        // Your frame update logic here
    }

    // Create a simple icon programmatically
    private static Sprite CreateIcon()
    {
        int size = 256;
        var tex = new Texture2D(size, size);
        Color bgColor = new Color(0.1f, 0.1f, 0.2f);
        Color fgColor = Color.cyan;

        // Fill background
        for (int x = 0; x < size; x++)
            for (int y = 0; y < size; y++)
                tex.SetPixel(x, y, bgColor);

        // Draw a simple shape (H letter)
        int margin = 40;
        int barWidth = 30;

        // Left vertical bar
        for (int x = margin; x < margin + barWidth; x++)
            for (int y = margin; y < size - margin; y++)
                tex.SetPixel(x, y, fgColor);

        // Right vertical bar
        for (int x = size - margin - barWidth; x < size - margin; x++)
            for (int y = margin; y < size - margin; y++)
                tex.SetPixel(x, y, fgColor);

        // Horizontal bar
        int midY = size / 2;
        for (int x = margin; x < size - margin; x++)
            for (int y = midY - barWidth / 2; y < midY + barWidth / 2; y++)
                tex.SetPixel(x, y, fgColor);

        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }

    private static Texture2D CreateSolidTexture(Color color)
    {
        var tex = new Texture2D(4, 4);
        for (int x = 0; x < 4; x++)
            for (int y = 0; y < 4; y++)
                tex.SetPixel(x, y, color);
        tex.Apply();
        return tex;
    }
}
```

## MelonLoader Entry Point

Your mod needs a standard MelonLoader entry point:

```csharp
using MelonLoader;

[assembly: MelonInfo(typeof(HelloWorldTVApp.Core), "HelloWorldTVApp", "1.0.0", "YourName")]
[assembly: MelonGame("TVGS", "Schedule I")]

namespace HelloWorldTVApp
{
    public class Core : MelonMod
    {
        public override void OnInitializeMelon()
        {
            LoggerInstance.Msg("HelloWorldTVApp loaded!");
        }
    }
}
```

Registration is automatic:

- Ensure your app type is `public`
- S1API will discover, instantiate, register, and spawn its UI/button at runtime. No explicit registration code is needed from the modder.

## UI Considerations

TV apps use a WorldSpace canvas, which differs from phone apps:

- **Explicit sizing required**: RectTransform `sizeDelta` must be set explicitly; percentage-based anchors alone won't work
- **Canvas orientation**: The TV canvas is positioned in 3D space to match the in-game TV screen
- **Use UIFactory**: The `S1API.UI.UIFactory` class provides helpers for creating UI elements

## Icons

Create icons programmatically (as shown in the example) or load from texture files:

```csharp
// Load from embedded resource or file
protected override Sprite Icon => LoadIconFromFile();

private Sprite LoadIconFromFile()
{
    // Load your icon texture and create a sprite
    var tex = new Texture2D(256, 256);
    // ... load texture data ...
    return Sprite.Create(tex, new Rect(0, 0, 256, 256), new Vector2(0.5f, 0.5f));
}
```

## Best Practices

- Keep `AppName` unique across all mods
- Use `UIFactory` helpers to create consistent UI elements
- Cache expensive resources like icons using static fields
- Handle the `OnClosed()` lifecycle hook to clean up any running logic
- Use `OnUpdate()` sparingly to avoid performance impact

## Example Mod

A complete example TV app is available at [HelloWorldTVApp](https://github.com/HazDS/HelloWorldTVApp).

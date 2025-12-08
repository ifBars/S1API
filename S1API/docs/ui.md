## UI

`S1API.UI.UIFactory` contains helpers for quickly constructing Unity UI with a consistent style.

## Common primitives

The `UIFactory` class provides a set of common UI primitives to rapidly build elements that match the game's aesthetic, particularly useful for custom phone apps.

### Common Primitives (from `UIFactory`)
- `UIFactory.Panel(name, parent, color, anchorMin?, anchorMax?, fullAnchor?)`: Creates a basic UI panel.
- `UIFactory.Text(name, content, parent, fontSize?, anchor?, style?)`: Displays static text.
- `UIFactory.ScrollableVerticalList(name, parent, out ScrollRect)`: Creates a scrollable list with vertical layout.
- `UIFactory.RoundedButtonWithLabel(name, label, parent, bgColor, width, height, fontSize, textColor)`: Creates a button with rounded corners and a label.
- `UIFactory.ButtonWithLabel(name, label, parent, bgColor, width, height)`: Creates a standard button with a label.
- `UIFactory.ButtonRow(name, parent, spacing?, alignment?)`: Arranges buttons in a horizontal row.
- `UIFactory.VerticalLayoutOnGO(go)`: Adds a vertical layout group to a GameObject.
- `UIFactory.HorizontalLayoutOnGO(go)`: Adds a horizontal layout group to a GameObject.
- `UIFactory.SetLayoutGroupPadding(layoutGroup, left, right, top, bottom)`: Sets padding for a layout group.
- `UIFactory.CreateQuestRow(name, parent, out iconPanel, out textPanel)`: Creates a specialized row for quest display, returning its icon and text panels for customization.

## Example

```csharp
using UnityEngine; // Required for Color, TextAnchor, FontStyle, Transform, GameObject
using S1API.UI;    // Required for UIFactory

// Example of common UI elements built with UIFactory.
// When building a PhoneApp, 'container.transform' from PhoneApp.OnCreatedUI is typically used as the parent.
// For standalone use, 'parent' would be the Transform of a GameObject acting as the UI canvas or container.
var panel = UIFactory.Panel("Main", parent, new Color(0.13f,0.13f,0.13f), fullAnchor: true);
var title = UIFactory.Text("Title", "Notes", panel.transform, 22, TextAnchor.MiddleLeft, FontStyle.Bold);
var list = UIFactory.ScrollableVerticalList("List", panel.transform, out var scroll);
var row = UIFactory.ButtonRow("Controls", panel.transform);
var (mask, addBtn, addLabel) = UIFactory.RoundedButtonWithLabel("Add", "+ New", row.transform, new Color(0.2f,0.5f,0.3f), 140, 40, 18, Color.white);
```
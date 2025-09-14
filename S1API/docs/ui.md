# UI

`S1API.UI.UIFactory` contains helpers for quickly constructing Unity UI with a consistent style.

## Common primitives

- `Panel(name, parent, color, anchorMin?, anchorMax?, fullAnchor?)`
- `Text(name, content, parent, fontSize?, anchor?, style?)`
- `ScrollableVerticalList(name, parent, out ScrollRect)`
- `RoundedButtonWithLabel(name, label, parent, bgColor, width, height, fontSize, textColor)`
- `ButtonWithLabel(name, label, parent, bgColor, width, height)`
- `ButtonRow(name, parent, spacing?, alignment?)`
- `VerticalLayoutOnGO(go)` / `HorizontalLayoutOnGO(go)` / `SetLayoutGroupPadding(...)`
- `CreateQuestRow(name, parent, out iconPanel, out textPanel)`

## Example

```csharp
var panel = UIFactory.Panel("Main", parent, new Color(0.13f,0.13f,0.13f), fullAnchor: true);
var title = UIFactory.Text("Title", "Notes", panel.transform, 22, TextAnchor.MiddleLeft, FontStyle.Bold);
var list = UIFactory.ScrollableVerticalList("List", panel.transform, out var scroll);
var row = UIFactory.ButtonRow("Controls", panel.transform);
var (mask, addBtn, addLabel) = UIFactory.RoundedButtonWithLabel("Add", "+ New", row.transform, new Color(0.2f,0.5f,0.3f), 140, 40, 18, Color.white);
```

See the S1NotesApp implementation for a full in-game UI built with these primitives.


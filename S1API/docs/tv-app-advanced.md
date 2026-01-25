# TV Apps: Notes & Internals

`S1API.TVApp.TVApp` is already documented in `S1API/docs/tv-app.md`. This page focuses on the parts modders usually trip over.

## Registration and discovery

- You do not call a registration API.
- S1API discovers `public` subclasses of `TVApp` when the TV home screen starts and spawns both:
  - the app UI (WorldSpace canvas)
  - the app button on the TV home screen

If your app doesn't appear:

- make the class `public`
- ensure it derives from `S1API.TVApp.TVApp`
- ensure it is included in your compiled mod assembly

## Exit/Back behavior

S1API registers an exit listener (same priority as base-game TV apps). When exit is pressed and your app is open, `Close()` is called.

Practical guidance:

- Put cleanup logic in `OnClosed()` (stop coroutines, detach listeners, etc.)
- Avoid leaving persistent objects under the TV canvas

## WorldSpace UI sizing

TV apps render on a WorldSpace canvas copied from the base game.

- Anchors alone are not enough for many layouts.
- Set `RectTransform.sizeDelta` explicitly for top-level panels you create.

If you're used to phone apps, expect to do a little more `RectTransform` setup.

## Update loop

- `OnUpdate()` is called every frame only while `IsOpen && !IsPaused`.
- Prefer event-driven UI updates where possible.

## Force-close

S1API may force-close your app when the TV home screen opens to avoid orphaned open apps. Treat `OnClosed()` as "always safe to run".

## See Also

- `S1API/docs/tv-app.md`
- <xref:S1API.TVApp.TVApp>

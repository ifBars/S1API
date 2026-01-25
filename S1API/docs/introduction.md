# S1API

S1API is a modular, developer-friendly API for building mods for Schedule One. It provides a stable abstraction layer over the game's internals with a focus on:

- Cross-compatibility between Mono and IL2CPP
- A first-class Phone App framework
- A lightweight UI factory for fast, consistent UI creation
- An ergonomic save system for mod data
- High-level wrappers for common systems (Phone Calls, Quests, NPCs, Items, etc.)
- Clean logging utilities

The goals are predictable behavior, safe integration with the base game, and a smooth developer experience.

## Highlights

- Cross-runtime configurations: `MonoMelon`, `Il2CppMelon`
- Phone App lifecycle: registration, icon spawning, open/close handling, input integration
- UI primitives: panels, lists, buttons, layouts, and helpers via `UIFactory`
- Saveables: annotate fields and get robust JSON persistence per-save-slot
- Phone Calls: define callers, build multi-stage calls, and queue safely into the game's system

## Quick Links

- Getting started: [Getting Started](getting-started.md)
- Custom NPCs: [Custom NPCs](custom-npcs.md)
- Phone Apps: [Phone Apps](phone-app.md)
- UI Factory: [UI](ui.md)
- Save System: [Save System](save-system.md)
- Phone Calls: [Phone Calls](phone-calls.md)

## Example Projects

For practical, production-ready examples:

- **[S1API NPC Examples](https://github.com/ifBars/S1APINPCExample)** - Complete custom NPC implementations covering customers, dealers, dialogue systems, schedules, and UI integration
- **[S1NotesApp](https://github.com/ifBars/S1NotesApp)** - Full in-game Notes application using S1API's PhoneApp and Saveables systems
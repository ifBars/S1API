---
title: "Tools – Modding Tools"
description: "Reference for Tools/ scripts: new-mod, check, scaffold, game-index, and patterns"
status: "stable"
version: "2.0.0"
reviewed: "2026-07-05"
applies_to: "S1Toolkit v2.0.0+"
---

# Tools – Modding Tools

Located in `Tools/` at the workspace root.

## new-mod.ps1 – Project Generator

Creates a complete mod project from the S1Toolkit template.

Usage: `.\Tools\new-mod.ps1 -Name MyMod -Author Dominik`

What it creates:

- `mod.json` (manifest)
- `MyMod.cs` (from ModTemplate)
- `MyMod.csproj` (with all references)
- `RULES.md` + `GAME-API.md` (reference copies)

## check.ps1 – Pre-Build Linter

Checks your mod for 9 common Il2Cpp/ID mistakes before compiling.

Usage: `.\Tools\check.ps1 -Path ..\mod-source\MyMod`

For LLM fix loops, add `-FirstErrorOnly`: it prints exactly one line (`ERROR file:line problem. Fix: ...`) so a small model can apply one fix per iteration. See `Tools/LLM-CONTEXT.md` for the full small-model workflow.

Rules checked:

1. Missing `Il2Cpp` namespace prefix
2. Custom `MonoBehaviour` (not allowed)
3. `StartCoroutine` (use `MelonCoroutines`)
4. `as` casts on Il2Cpp (use `TryCast`)
5. `System.Threading` (use `MelonCoroutines`)
6. `async void`
7. `foreach` on Il2Cpp collections
8. `mod.json` validity
9. Unknown item/NPC IDs — string literals in `Api.Inventory.*` / `Api.NPC.*` calls are validated against `Tools/ids.json`

## scaffold.ps1 – Feature → Mod Generator

Takes a natural language feature description and generates a mod with API calls.

Usage: `.\Tools\scaffold.ps1 "god mode" GodMode`

How it works:

1. Matches your description against `game-index.json` (100+ feature keywords)
2. Finds the best matching API methods
3. Generates a complete mod with the API calls embedded
4. Runs `check.ps1` for validation

## game-index.json – Semantic Feature Map

JSON file mapping 100+ feature descriptions to API methods.
Used by `scaffold.ps1` and LLMs for semantic code generation.

Keywords in both German and English.

## patterns/ – Copy-Paste Snippets

15 pre-built code snippets for common mod patterns:

- `infinite-money`, `full-health-energy`, `give-item`, `spawn-vehicle`
- `teleport`, `time-control`, `instant-grow`, `no-police`
- `npc-friend`, `unlock-recipes`, `freeze-time`, `no-police`
- `kill-npcs`, `complete-quests`, `notify-on-event`

Each pattern is 10–20 lines, only uses `Api.*` calls, no Il2Cpp knowledge required.

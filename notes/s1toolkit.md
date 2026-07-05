---
title: "S1Toolkit – Modding Framework"
description: "3-tier architecture overview: slim utilities, public API, and core framework"
status: "stable"
version: "2.0.0"
reviewed: "2026-07-05"
applies_to: "S1Toolkit v2.0.0+"
---

# S1Toolkit – Modding Framework

## Architecture

S1Toolkit has three layers, from simple to complex:

### Tier 1: S1Toolkit-slim (Utilities + Templates)

- `S1.cs` — Il2Cpp-safe helpers (TryCast, RunAfter, SafeRun, Config)
- `ModTemplate.cs` — Ready-to-use mod skeleton with markers
- Dependency: None (standalone)

Use when: Writing simple mods, first mod, prototyping.

### Tier 2: S1Toolkit-api (Public API)

- 29 subsystems, ~190 methods
- `Api.Player.SetHealth(100)` instead of `Il2CppScheduleOne.PlayerScripts...`
- Dependency: S1Toolkit-slim (S1.cs utilities)

Use when: Any mod that touches game systems. This is what LLMs use.

### Tier 3: S1Toolkit (Core Framework)

- ModuleBase + ModuleRegistry (modular architecture)
- CircuitBreaker (error protection)
- ConfigManager (JSON config with hot-reload)
- SaveManager (ISaveable interface)
- UI Builder (fluent IMGUI)
- CoroutineBuilder (fluent async)
- SessionGate (authority checks)
- PatchManager (conflict detection)

Use when: Complex mods needing modular architecture, save/load, config UI.

## Quick Decision

| You want to... | Use |
|---|---|
| Add money cheat | Tier 2 (`Api.Money.Add`) |
| Write first mod | Tier 1 (`ModTemplate`) |
| Build a multi-feature mod with config | Tier 3 (`ModuleBase`) |
| LLM to generate mod code | Tier 2 (`Api.*`) |
| Check mod for Il2Cpp bugs | `Tools/check.ps1` |

## API Reference

See [api-reference.md](../api/api-reference.md) for the complete API documentation.

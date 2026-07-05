# S1Toolkit — Schedule I Modding Framework

Write Schedule I mods without touching Il2Cpp. 51 API classes, 252 methods.

> **Forked from [S1API](https://github.com/ifBars/S1API) (MIT) by KaBooMa & ifBars.**
> Reduced to Il2Cpp/MelonLoader only, with added LLM tooling and quick-access API.

[![Build Check](https://github.com/domi7602/S1Toolkit/actions/workflows/build.yml/badge.svg)](https://github.com/domi7602/S1Toolkit/actions/workflows/build.yml)
[![Docs](https://github.com/domi7602/S1Toolkit/actions/workflows/docs.yml/badge.svg)](https://github.com/domi7602/S1Toolkit/actions/workflows/docs.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

## Quickstart

```csharp
using S1Toolkit.Api;

// Quick-access static API:
Api.Money.Add(1000);           // +$1000
Api.Player.SetHealth(100);     // full health
Api.Time.SkipToMorning();      // skip to 6 AM
```

## Features

- **28+ API namespaces** — Items, NPCs, Quests, Money, Time, Map, Law, and more
- **80+ pre-defined NPCs** with schedules, dialogue, and appearance
- **60+ buildings**, **45+ delivery locations**, **20+ parking lots**
- **Save system** — persist mod data across save slots
- **Cross-compatible** Mono/Il2Cpp layer removed (Il2Cpp-only for simplicity)
- **LLM-optimized** — `Tools/LLM-CONTEXT.md` has everything a small model needs

## For LLMs / AI agents

```
Point your AI coding agent at:
  Tools/LLM-CONTEXT.md   — complete context file
  Tools/snippets/        — copy-paste mod templates
  Tools/check.ps1        — fix loop script
```

## Documentation

Full API reference and guides are at:  
**[domi7602.github.io/S1Toolkit](https://domi7602.github.io/S1Toolkit)** (coming soon)

## License

MIT — see `LICENSE` and `LICENSE-S1API.txt`.

S1Toolkit is a fork of [S1API](https://github.com/ifBars/S1API) by KaBooMa & ifBars (MIT licensed).

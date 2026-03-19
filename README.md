# S1API - A Schedule One Mono / Il2Cpp Cross Compatibility Layer

[![IL2CPP Build Check](https://github.com/ifBars/S1API/actions/workflows/il2cpp-build-check.yml/badge.svg?branch=stable)](https://github.com/ifBars/S1API/actions/workflows/il2cpp-build-check.yml)
[![Build and Deploy Documentation](https://github.com/ifBars/S1API/actions/workflows/docs.yml/badge.svg)](https://github.com/ifBars/S1API/actions/workflows/docs.yml)
[![GitHub stars](https://img.shields.io/github/stars/ifBars/S1API)](https://github.com/ifBars/S1API/stargazers)
[![GitHub forks](https://img.shields.io/github/forks/ifBars/S1API)](https://github.com/ifBars/S1API/network/members)
[![GitHub issues](https://img.shields.io/github/issues/ifBars/S1API)](https://github.com/ifBars/S1API/issues)
[![GitHub license](https://img.shields.io/github/license/ifBars/S1API)](https://github.com/ifBars/S1API/blob/stable/LICENSE)
[![GitHub last commit](https://img.shields.io/github/last-commit/ifBars/S1API/stable)](https://github.com/ifBars/S1API/commits/stable)

S1API is an open source collaboration project to help standardize Schedule One modding processes.
The goal is to provide a standard place for common functionalities so you can focus on making content versus reverse engineering the game.

## Coverage History

Track S1API's progress in wrapping Schedule One's game types:

[![API Coverage](https://img.shields.io/badge/API%20Coverage-28.4%25-orange)](https://github.com/ifBars/S1API/actions/workflows/coverage.yml)

![Coverage Chart](https://quickchart.io/chart?c=%7B%22type%22%3A%22line%22%2C%22data%22%3A%7B%22labels%22%3A%5B%222025-12-30%22%2C%222025-12-30%22%2C%222026-01-02%22%2C%222026-01-02%22%2C%222026-01-03%22%2C%222026-01-08%22%2C%222026-01-08%22%2C%222026-01-11%22%2C%222026-01-27%22%2C%222026-01-28%22%2C%222026-01-28%22%2C%222026-01-29%22%2C%222026-02-03%22%2C%222026-02-10%22%2C%222026-02-20%22%2C%222026-03-19%22%5D%2C%22datasets%22%3A%5B%7B%22label%22%3A%22Class%20Coverage%20%25%22%2C%22data%22%3A%5B25%2C27.491785323110623%2C28.039430449069002%2C28.148959474260675%2C28.258488499452355%2C27.854855923159015%2C27.628865979381445%2C27.938144329896907%2C28.24742268041237%2C28.45360824742268%2C29.07216494845361%2C29.175257731958766%2C29.303278688524593%2C29.815573770491806%2C30.43032786885246%2C28.383458646616543%5D%2C%22borderColor%22%3A%22rgb%2875%2C%20192%2C%20192%29%22%2C%22backgroundColor%22%3A%22rgba%2875%2C%20192%2C%20192%2C%200.1%29%22%2C%22fill%22%3Afalse%2C%22tension%22%3A0.1%7D%5D%7D%2C%22options%22%3A%7B%22responsive%22%3Atrue%2C%22plugins%22%3A%7B%22title%22%3A%7B%22display%22%3Atrue%2C%22text%22%3A%22S1API%20Coverage%20Over%20Time%22%7D%2C%22legend%22%3A%7B%22display%22%3Atrue%2C%22position%22%3A%22top%22%7D%7D%2C%22scales%22%3A%7B%22y%22%3A%7B%22beginAtZero%22%3Atrue%2C%22max%22%3A100%2C%22title%22%3A%7B%22display%22%3Atrue%2C%22text%22%3A%22Coverage%20%25%22%7D%7D%2C%22x%22%3A%7B%22title%22%3A%7B%22display%22%3Atrue%2C%22text%22%3A%22Date%22%7D%7D%7D%7D%7D&width=800&height=400)

*View detailed coverage reports in the [Coverage Analysis workflow](https://github.com/ifBars/S1API/actions/workflows/coverage.yml)*

## Release Channels

S1API is available through multiple release channels:

### **Stable Releases** (Recommended)
The recommended way to get S1API is through official releases:
- **[GitHub Releases](https://github.com/ifBars/S1API/releases)** - Tagged releases with changelogs
- **[Nexus Mods](https://www.nexusmods.com/schedule1/mods/1194)** - Mod repository with version tracking
- **[Thunderstore](https://thunderstore.io/c/schedule-i/p/ifBars/S1API_Forked/)** - Mod manager integration

These releases are thoroughly tested and recommended for both mod users and developers.

### **Experimental/Bleeding Edge Builds**
For mod developers or users seeking the latest features and fixes before official releases, experimental builds are automatically generated via GitHub Actions:
- **IL2CPP Build**: Available as artifacts from the [IL2CPP Build Check workflow](https://github.com/ifBars/S1API/actions/workflows/il2cpp-build-check.yml)
- **Mono Build**: Available as artifacts from the [Documentation workflow](https://github.com/ifBars/S1API/actions/workflows/docs.yml)

⚠️ **Note**: These artifacts represent the bleeding edge of development and may contain untested features or bugs. Use at your own risk. Stable releases remain the recommended choice for most users.

## What Does it Do?
* Allows creation of new game elements (quests, npcs, etc.)
* Provides an easy-to-use abstraction for save/load of class data
* Eases access to various common game functions without needing to import the assembly themselves, removing the Mono / Il2Cpp dependency for your mod.
* Allow modders to live in the Managed environment, regardless of Mono / Il2Cpp development
* Lower the bearer of entry into S1 modding, especially for Il2Cpp builds
* Support mod developers by allowing them to compile a single assembly that works across both builds
* Who knows what else? We will have to see who all is willing to collaborate on this ❤️

## What Are the Limitations?
* S1API will NOT be the be-all and end-all. It's just not possible.
* Handle Il2Cpp / Mono communication when utilizing game assemblies as dependencies
* Cover all modding needs. It will never be as flexible as writing your own mod referencing game assemblies.

## How It's Designed to Work
S1API is designed to compile for Mono and Il2Cpp separately.
Mod users don't need to worry about this though. 
The standard installation ships with all builds and a plugin to dynamically load the proper version!

Mod developers can develop their mods on whichever build, Mono or Il2Cpp, without having to step into the Il2Cpp environment.
Caveat: If you do utilize Il2Cpp functionality within your mod, you lose cross compatibility.
I can't think of why you would. I wanted to make sure that is clarified though.

S1API is designed to be a tag-along as well.
If you want to do custom content specific to Mono or Il2Cpp, S1API can still assist with some of the common repetitive tasks.

## Want to Contribute?
This is a massive project with so many different areas to specialize in.
If you're interested in contributing, please do!
Look over the [CONTRIBUTING.md](CONTRIBUTING.md) for guidance on code standards and the process.

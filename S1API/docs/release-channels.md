# S1API Release Channels

S1API is distributed through stable releases, experimental builds, and NuGet packages. Choose the channel based on whether you are playing with released mods, developing against the public API, or testing an unreleased fix.

## Stable

Stable releases are recommended for most users and mod developers.

- [GitHub Releases](https://github.com/ifBars/S1API/releases) provides tagged release ZIPs.
- [Nexus Mods](https://www.nexusmods.com/schedule1/mods/1194) provides the mod listing and version tracking.
- [Thunderstore](https://thunderstore.io/c/schedule-i/p/ifBars/S1API_Forked/) supports mod-manager workflows.

## NuGet

Developers should reference `S1API.Forked` from NuGet unless they need an unreleased change.

```bash
dotnet add package S1API.Forked
```

## Experimental builds

Experimental artifacts are produced from GitHub Actions and may include unreleased fixes. Use them only when you need to test a specific change and are ready to replace them with the next stable release.

## Versioning

Release and maintenance branch behavior is documented in the repository's `VERSIONING.md`. For users and mod developers, the important rule is simple: prefer stable releases unless a maintainer has pointed you at a specific experimental artifact.

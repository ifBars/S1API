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

## Beta game pre-releases

When S1API publishes a beta build for Schedule One's beta game branch, it is available as a GitHub pre-release. Find them on the [GitHub Releases page](https://github.com/ifBars/S1API/releases) by looking for a release marked **Pre-release**, such as `S1API 3.1.0-beta.1`. Download the attached `S1API-Forked-...zip` asset and install it like a stable release.

Beta game pre-releases are published only on GitHub. They are separate from the downloadable files shown under **Artifacts** on a GitHub Actions workflow run. Those workflow artifacts are temporary CI outputs, not the packaged beta release. Beta pre-releases are intended for the matching beta game build and may be unstable; use a stable release for normal play.

These beta releases do not publish to Nexus Mods, Thunderstore, or NuGet.

## Versioning

Release and maintenance branch behavior is documented in the repository's `VERSIONING.md`. Prefer stable releases unless you specifically need to test S1API with the beta game build.

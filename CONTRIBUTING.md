# Contributing
Welcome potential contributor! 
I appreciate your interest in this project.
Please read over the below in full to help you get started and set expectations 😊

## Important!!!
- Please thoroughly read over [CODING_STANDARDS.md](CODING_STANDARDS.md) before contributing.
- Do **NOT** alter my GitHub actions unless you have a good reason. 
  I will close your PR and ban you from the project if malicious intent is found.

## Before Opening a Pull Request

Bug fixes, feature requests, and API improvements must have a GitHub issue
opened **before** the pull request, even if you already have a fix ready. The
issue records the problem or request independently of the proposed implementation.

1. Search [existing issues](https://github.com/ifBars/S1API/issues) and open pull
   requests first. Reuse an issue that already tracks the same problem rather
   than opening a duplicate.
2. If there is no matching issue, open a
   [bug report](https://github.com/ifBars/S1API/issues/new?template=bug_report.yml)
   or [feature request](https://github.com/ifBars/S1API/issues/new?template=feature_request.yml).
   For bugs, include reproduction steps, expected and actual behavior, affected
   versions, and the Mono or IL2CPP runtime. For requests, explain the use case
   and what is missing today.
3. Comment on the issue with your intended approach before starting work so
   contributors can coordinate. Discuss substantial API or behavior changes
   with a maintainer before implementing them.
4. Link the issue in the PR description. Use `Fixes #123` when the PR fully
   resolves it, or `Refs #123` when it only addresses part of the work.

A PR description does not replace an issue. Bug-fix and feature PRs without a
linked issue will be asked to add one before review proceeds. Keep unrelated
bugs or requests in separate issues and focused PRs.

Typo corrections, formatting, and documentation-only changes that do not alter
API behavior may be submitted without an issue; explain that exception in the
PR's linked issue section. Maintainers may approve other exceptions explicitly.

## Prerequisites
S1API is available to mod developers of all experience levels, but contributing
game-facing changes assumes working familiarity with Schedule I mod development
across both the public IL2CPP and alternate Mono branches. If you are new to
Schedule I modding, start with the
[Schedule I Modding Wiki](https://s1modding.github.io/docs/moddevs/) and build a
mod before proposing game-facing changes to S1API.

Before building S1API:

1. Install the [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0).
2. Prepare working MelonLoader environments for the public IL2CPP and alternate
   Mono branches.
3. Configure both environments in `local.build.props` using
   `example.build.props` as the template.

## How to Build the Project
1. Clone the project using `git clone https://github.com/ifBars/S1API.git`
2. Copy the `example.build.props` file to a new file named `local.build.props`. This file located in the base repository directory.
3. Update all properties in `local.build.props` to proper paths for your local system.
   - Personally, I have two copies of Schedule I locally. This way I can test all four builds independently. 
     You can swap between just one if you switch. It will just be a bit more of a hassle 😊.
4. Restore, build, and test each runtime with the matching configuration:

   ```powershell
   dotnet restore S1API.sln -p:Configuration=MonoMelon
   dotnet build S1API.sln -c MonoMelon --no-restore -p:AutomateLocalDeployment=false
   dotnet test S1API.Tests/S1API.Tests.csproj -c MonoMelon --no-restore --no-build

   dotnet restore S1API.sln -p:Configuration=Il2CppMelon
   dotnet build S1API.sln -c Il2CppMelon --no-restore -p:AutomateLocalDeployment=false
   dotnet test S1API.Tests/S1API.Tests.csproj -c Il2CppMelon --no-restore --no-build
   ```

   `MonoMelon` and `Il2CppMelon` have different restore graphs. Do not reuse one
   runtime's restore output for the other runtime's `--no-restore` build.

`S1API.Tests/` is the repository's committed test suite. Keep game-facing smoke
mods, launchers, harnesses, disposable saves or installs, logs, screenshots, and
other runtime evidence local; do not commit anything under `tests/Smoke/`.
Summarize the smoke scenario and results in the pull request instead.

## PR Preparations
Verify your changes will successfully build for all **two** build configurations prior to PR please.
Ultimately, this just saves you time and gets your changes into the API faster.

| Build Type    | Description                                    |
|---------------|------------------------------------------------|
| Il2CppMelon   | MelonLoader for Il2Cpp (base game) builds      |
| MonoMelon     | MelonLoader for Mono (alternate branch) builds |

## Proper Contributing Channels
Target regular-game pull requests at `stable` and game-beta pull requests at
`beta`. For release and hotfix preparation, follow [VERSIONING.md](VERSIONING.md).

## Tracking Work & Issues
[GitHub issues](https://github.com/ifBars/S1API/issues) are the source of truth
for bugs and requests. Keep reproduction details, scope decisions, and related
PR links on the issue so the work remains easy to track across releases.

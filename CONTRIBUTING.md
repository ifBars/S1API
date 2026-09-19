# Contributing
Welcome potential contributor! 
I appreciate your interest in this project.
Please read over the below in full to help you get started and set expectations 😊

## Important!!!
- Please thoroughly read over [CODING_STANDARDS.md](CODING_STANDARDS.md) before contributing.
- Do **NOT** alter my GitHub actions unless you have a good reason. 
  I will close your PR and ban you from the project if malicious intent is found.

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
All pull requests **must** go into `bleeding-edge` before `stable`.
If you make a pull request for `stable`, I **will** be changing it to verify build.

## Tracking Work & Issues
We maintain a [Trello board](https://trello.com/b/yuRuBpIg/s1api) where known issues and tasks that need to be done are tracked. 
While the board is not always perfectly up to date, it should typically show the known issues in the project. 
GitHub issues opened with us will typically be added to the Trello board and then archived once closed.

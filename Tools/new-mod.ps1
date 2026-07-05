#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Creates a new Schedule I MelonLoader mod project from the S1Toolkit template.
.PARAMETER Name
    Name des Mods (z.B. "YieldTweak")
.PARAMETER Author
    Autor-Name (Default: "Unknown")
.PARAMETER Description
    Kurzbeschreibung (optional)
.EXAMPLE
    .\new-mod.ps1 -Name YieldTweak -Author Dominik
    Creates mod-source/YieldTweak/ with a complete project.
#>

param(
    [Parameter(Mandatory = $true)]
    [string]$Name,

    [Parameter(Mandatory = $false)]
    [string]$Author = "Unknown",

    [Parameter(Mandatory = $false)]
    [string]$Description = ""
)

# ── Pfade ──────────────────────────────────────────
$workspaceRoot = Resolve-Path "$PSScriptRoot\.."
$modSourceDir = Join-Path $workspaceRoot "mod-source"
$targetDir = Join-Path $modSourceDir $Name
$toolkitDir = Join-Path $modSourceDir "S1Toolkit-slim"
$toolsDir = $PSScriptRoot

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Neuen Mod erstellen: $Name" -ForegroundColor Cyan
Write-Host "  Author: $Author" -ForegroundColor Cyan
Write-Host "  Ziel: $targetDir" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

# ── Check whether the target exists ──────────────────────
if (Test-Path $targetDir)
{
    Write-Host "FEHLER: Zielordner existiert bereits: $targetDir" -ForegroundColor Red
    exit 1
}

# ── Ordner erstellen ──────────────────────────────
New-Item -ItemType Directory -Path $targetDir -Force | Out-Null
Write-Host "[OK] Ordner erstellt" -ForegroundColor Green

# ── 0. mod.json generieren ──────────────────────────
$modId = $Name.ToLower() -replace '[^a-z0-9]', '-'
$modJson = @{
    id = $modId
    name = $Name
    version = "1.0.0"
    author = $Author
    description = $Description
    gameVersion = ">=1.0"
    toolkitVersion = ">=1.0"
    dependencies = @()
    apiUsage = @()
    compatibility = @{ coopSafe = $false; clientOnly = $false; hostOnly = $false }
}
$modJson | ConvertTo-Json -Depth 3 | Set-Content -Path (Join-Path $targetDir "mod.json") -Encoding UTF8
Write-Host "[OK] mod.json erstellt (id=$modId)" -ForegroundColor Green

# ── 1. csproj kopieren + anpassen ─────────────────
$csprojContent = @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net6.0</TargetFramework>
    <AssemblyName>$Name</AssemblyName>
    <RootNamespace>$Name</RootNamespace>
    <ImplicitUsings>disable</ImplicitUsings>
    <Nullable>disable</Nullable>
    <LangVersion>latest</LangVersion>
    <PlatformTarget>x64</PlatformTarget>
    <Deterministic>true</Deterministic>
    <GenerateAssemblyInfo>false</GenerateAssemblyInfo>
    <GameDir Condition="'`$(GameDir)' == ''">C:\Program Files (x86)\Steam\steamapps\common\Schedule I</GameDir>
  </PropertyGroup>

  <ItemGroup>
    <Reference Include="MelonLoader">
      <HintPath>`$(GameDir)\MelonLoader\net6\MelonLoader.dll</HintPath>
      <Private>false</Private>
    </Reference>
    <Reference Include="Il2CppInterop.Runtime">
      <HintPath>`$(GameDir)\MelonLoader\net6\Il2CppInterop.Runtime.dll</HintPath>
      <Private>false</Private>
    </Reference>
    <Reference Include="0Harmony">
      <HintPath>`$(GameDir)\MelonLoader\net6\0Harmony.dll</HintPath>
      <Private>false</Private>
    </Reference>
    <Reference Include="Il2Cppmscorlib">
      <HintPath>`$(GameDir)\MelonLoader\Il2CppAssemblies\Il2Cppmscorlib.dll</HintPath>
      <Private>false</Private>
    </Reference>
    <Reference Include="UnityEngine">
      <HintPath>`$(GameDir)\MelonLoader\Il2CppAssemblies\UnityEngine.dll</HintPath>
      <Private>false</Private>
    </Reference>
    <Reference Include="UnityEngine.CoreModule">
      <HintPath>`$(GameDir)\MelonLoader\Il2CppAssemblies\UnityEngine.CoreModule.dll</HintPath>
      <Private>false</Private>
    </Reference>
    <Reference Include="UnityEngine.UI">
      <HintPath>`$(GameDir)\MelonLoader\Il2CppAssemblies\UnityEngine.UI.dll</HintPath>
      <Private>false</Private>
    </Reference>
    <Reference Include="UnityEngine.IMGUIModule">
      <HintPath>`$(GameDir)\MelonLoader\Il2CppAssemblies\UnityEngine.IMGUIModule.dll</HintPath>
      <Private>false</Private>
    </Reference>
    <Reference Include="UnityEngine.InputLegacyModule">
      <HintPath>`$(GameDir)\MelonLoader\Il2CppAssemblies\UnityEngine.InputLegacyModule.dll</HintPath>
      <Private>false</Private>
    </Reference>
    <Reference Include="Assembly-CSharp">
      <HintPath>`$(GameDir)\MelonLoader\Il2CppAssemblies\Assembly-CSharp.dll</HintPath>
      <Private>false</Private>
    </Reference>
    <Reference Include="Il2CppFishNet.Runtime">
      <HintPath>`$(GameDir)\MelonLoader\Il2CppAssemblies\Il2CppFishNet.Runtime.dll</HintPath>
      <Private>false</Private>
    </Reference>
    <Reference Include="Il2CppNewtonsoft.Json">
      <HintPath>`$(GameDir)\MelonLoader\Il2CppAssemblies\Il2CppNewtonsoft.Json.dll</HintPath>
      <Private>false</Private>
    </Reference>
  </ItemGroup>

  <ItemGroup>
    <Compile Include="..\S1Toolkit-slim\S1.cs" Link="S1.cs" />
  </ItemGroup>

  <!-- Kopiert die gebaute DLL automatisch in den Mods-Ordner -->
  <Target Name="CopyToModsFolder" AfterTargets="Build">
    <Copy SourceFiles="`$(TargetPath)" DestinationFolder="`$(GameDir)\Mods" SkipUnchangedFiles="true" Condition="Exists('`$(GameDir)\Mods')" />
  </Target>
</Project>
"@

$csprojPath = Join-Path $targetDir "$Name.csproj"
Set-Content -Path $csprojPath -Value $csprojContent -Encoding UTF8
Write-Host "[OK] $Name.csproj erstellt" -ForegroundColor Green

# ── 2. ModTemplate kopieren + anpassen ────────────
$templatePath = Join-Path $toolkitDir "ModTemplate.cs"
$mainPath = Join-Path $targetDir "$Name.cs"

if (Test-Path $templatePath)
{
    $content = Get-Content $templatePath -Raw
    $content = $content -replace 'namespace ModTemplate', "namespace $Name"
    $content = $content -replace 'ModTemplate\.MyMod', "${Name}.MyMod"
    $content = $content -replace '>>> ModName', $Name
    $content = $content -replace '>>> Author', $Author
    Set-Content -Path $mainPath -Value $content -Encoding UTF8
    Write-Host "[OK] $Name.cs aus ModTemplate.cs erstellt" -ForegroundColor Green
}
else
{
    Write-Host "WARN: ModTemplate.cs nicht gefunden unter $templatePath" -ForegroundColor Yellow
}

# ── 3. Copy LLM-CONTEXT.md (single-file prompt context) ────────────
$ctxSrc = Join-Path $toolsDir "LLM-CONTEXT.md"
$ctxDst = Join-Path $targetDir "LLM-CONTEXT.md"
if (Test-Path $ctxSrc) { Copy-Item $ctxSrc $ctxDst -Force; Write-Host "[OK] LLM-CONTEXT.md copied" -ForegroundColor Green }

# ── 4. S1.cs is linked via csproj (no copy needed) ──
Write-Host "[OK] S1.cs linked via csproj" -ForegroundColor Green

# ── 5. Run the build ─────────────────────────
Write-Host "`n[BUILD] Starte dotnet build ..." -ForegroundColor Cyan
try
{
    $buildResult = & dotnet build $csprojPath -c Release 2>&1
    if ($LASTEXITCODE -eq 0)
    {
        Write-Host "[BUILD] Erfolgreich!" -ForegroundColor Green
    }
    else
    {
        Write-Host "[BUILD] Fehlgeschlagen (Exit: $LASTEXITCODE). Details:" -ForegroundColor Red
        $buildResult | ForEach-Object { Write-Host $_ -ForegroundColor Red }
    }
}
catch
{
    Write-Host "[BUILD] Fehler beim Build: $_" -ForegroundColor Red
}

# ── Abschluss ───────────────────────────────────
Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "  Mod '$Name' erstellt!" -ForegroundColor Green
Write-Host "  Ordner: $targetDir" -ForegroundColor Green
Write-Host "  Next steps:" -ForegroundColor Cyan
Write-Host "  1. Open '$Name.cs' and fill in the // >>> markers" -ForegroundColor Cyan
Write-Host "  2. Run dotnet build -c Release" -ForegroundColor Cyan
Write-Host "  3. DLL wird automatisch nach Mods/ kopiert" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Builds all S1Toolkit projects locally and stages the release payload in dist/S1Toolkit/.
    Run this BEFORE tagging a release — CI cannot build (game DLLs are local-only),
    so the release job packages the files staged here.
.EXAMPLE
    .\package-release.ps1
    .\package-release.ps1 -GameDir "D:\Games\Schedule I"
.NOTES
    Exit code: 0 = staged, 1 = build or staging failure
#>

param(
    [string]$GameDir = "C:\Program Files (x86)\Steam\steamapps\common\Schedule I"
)

$root = Resolve-Path "$PSScriptRoot\.."
$stage = Join-Path $root "dist\S1Toolkit"

$projects = @(
    @{ Proj = "mod-source\S1Toolkit-slim\S1Toolkit-slim.csproj";       Dll = "mod-source\S1Toolkit-slim\bin\Release\net6.0\S1Toolkit.dll";          Out = "S1Toolkit-slim.dll" },
    @{ Proj = "mod-source\S1Toolkit-api\S1Toolkit.Api.csproj";         Dll = "mod-source\S1Toolkit-api\bin\Release\net6.0\S1Toolkit.Api.dll";       Out = "S1Toolkit.Api.dll" },
    @{ Proj = "mod-source\S1Toolkit\S1Toolkit.csproj";                 Dll = "mod-source\S1Toolkit\bin\Release\net6.0\S1Toolkit.dll";               Out = "S1Toolkit.dll" },
    @{ Proj = "mod-source\S1Toolkit-modules\S1Toolkit.Modules.csproj"; Dll = "mod-source\S1Toolkit-modules\bin\Release\net6.0\S1Toolkit.Modules.dll"; Out = "S1Toolkit.Modules.dll" }
)

foreach ($p in $projects)
{
    Write-Host "[PACKAGE] Building $($p.Proj) ..." -ForegroundColor Cyan
    dotnet build (Join-Path $root $p.Proj) -c Release -p:GameDir="$GameDir" -v q
    if ($LASTEXITCODE -ne 0)
    {
        Write-Host "[PACKAGE] ERROR: build failed for $($p.Proj)" -ForegroundColor Red
        exit 1
    }
}

if (Test-Path $stage) { Remove-Item $stage -Recurse -Force }   # no stale files from earlier stagings
New-Item -ItemType Directory -Path $stage -Force | Out-Null

foreach ($p in $projects)
{
    $src = Join-Path $root $p.Dll
    if (-not (Test-Path $src))
    {
        Write-Host "[PACKAGE] ERROR: expected output missing: $($p.Dll)" -ForegroundColor Red
        exit 1
    }
    Copy-Item $src (Join-Path $stage $p.Out) -Force
}

$docs = @(
    @{ Src = "docs\api-reference.md";                              Out = "api-reference.md" },
    @{ Src = "Tools\LLM-CONTEXT.md";                               Out = "LLM-CONTEXT.md" },
    @{ Src = "Tools\ids.json";                                     Out = "ids.json" },
    @{ Src = "LICENSE";                                            Out = "LICENSE" },
    @{ Src = "mod-source\S1Toolkit-modules\LICENSE-S1API.txt";     Out = "LICENSE-S1API.txt" },
    @{ Src = "README.md";                                          Out = "README.md" }
)
foreach ($d in $docs)
{
    Copy-Item (Join-Path $root $d.Src) (Join-Path $stage $d.Out) -Force
}

Write-Host "`n[PACKAGE] Staged $((Get-ChildItem $stage).Count) files in dist/S1Toolkit/:" -ForegroundColor Green
Get-ChildItem $stage | ForEach-Object { Write-Host "  $($_.Name)" }
Write-Host "`n[PACKAGE] Next: commit dist/S1Toolkit/, then 'git tag vX.Y.Z && git push --tags' to release." -ForegroundColor Cyan
exit 0

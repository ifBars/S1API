#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Validates game-index.json against the real API surface and reports coverage.

    Every "api" entry in the index MUST point to a method that actually exists in
    mod-source/S1Toolkit-api/Api/*.cs. This makes the GetCash/GetBalance-style drift
    (index references a method that was renamed or never existed) structurally
    impossible once wired into CI.

    It also lists real public methods that are NOT yet in the index. That list is
    your curation backlog for "kuratierte Breite" - it does not fail the build.
.EXAMPLE
    .\check-index.ps1
    .\check-index.ps1 -IndexPath Tools\game-index.json -ApiPath mod-source\S1Toolkit-api\Api
.NOTES
    Exit code: 0 = index is in sync, 1 = index references a non-existent method.
#>

param(
    [string]$IndexPath = "$PSScriptRoot/game-index.json",
    [string]$ApiPath   = "$PSScriptRoot/../mod-source/S1Toolkit-api/Api"
)

$ErrorActionPreference = 'Stop'

if (-not (Test-Path $IndexPath)) { Write-Host "[INDEX] Not found: $IndexPath" -ForegroundColor Red; exit 1 }
if (-not (Test-Path $ApiPath))   { Write-Host "[INDEX] Not found: $ApiPath"   -ForegroundColor Red; exit 1 }

# ── 1. Build the real API surface from source: "Api.<Class>.<Method>" ────────────
# Each Api/*.cs file holds exactly one `public static class X` nested in `partial class Api`.
# Method signatures are always single-line, so a per-line regex is reliable here.
$real = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
$methodsByClass = @{}   # class -> list of method names (for "did you mean" hints)

$files = Get-ChildItem -Path $ApiPath -Recurse -Filter '*.cs' |
    Where-Object { $_.Name -ne 'Internal.cs' }   # Internal is `internal`, not public surface

foreach ($f in $files)
{
    $className = $null
    foreach ($line in (Get-Content $f.FullName))
    {
        if ($line -match '^\s*public\s+static\s+class\s+(\w+)')
        {
            $className = $matches[1]
            if (-not $methodsByClass.ContainsKey($className)) { $methodsByClass[$className] = @() }
            continue
        }
        if ($null -eq $className) { continue }

        # public static <returnType> <Method>[<T>](   -> capture <Method>
        if ($line -match '^\s*public\s+static\s+[\w<>,\.\[\]\?]+(?:\s+[\w<>,\.\[\]\?]+)*\s+(\w+)\s*(?:<[^>]*>)?\s*\(')
        {
            $method = $matches[1]
            $full = "Api.$className.$method"
            [void]$real.Add($full)
            if ($methodsByClass[$className] -notcontains $method) { $methodsByClass[$className] += $method }
        }

        # public static event <DelegateType> <Name>;   -> events are valid API surface too
        if ($line -match '^\s*public\s+static\s+event\s+[\w<>,\.\?]+\s+(\w+)\s*;')
        {
            $evt = $matches[1]
            $full = "Api.$className.$evt"
            [void]$real.Add($full)
            if ($methodsByClass[$className] -notcontains $evt) { $methodsByClass[$className] += $evt }
        }
    }
}

Write-Host "[INDEX] Discovered $($real.Count) public API methods across $($methodsByClass.Keys.Count) sub-APIs.`n" -ForegroundColor Cyan

# ── 2. Parse the index ──────────────────────────────────────────────────────────
$index = Get-Content $IndexPath -Raw | ConvertFrom-Json
$features = $index.features.PSObject.Properties
$referenced = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
$errors = @()

foreach ($feature in $features)
{
    $key = $feature.Name
    $api = $feature.Value.api
    if ([string]::IsNullOrWhiteSpace($api)) { continue }         # non-callable entries (concepts) are allowed
    [void]$referenced.Add($api)

    if ($real.Contains($api)) { continue }

    # Build a helpful hint: does the class exist? what methods does it offer?
    $hint = ''
    if ($api -match '^Api\.(\w+)\.(\w+)$')
    {
        $cls = $matches[1]; $mth = $matches[2]
        if ($methodsByClass.ContainsKey($cls))
        {
            $candidates = $methodsByClass[$cls]
            $close = $candidates | Where-Object { $_ -like "*$mth*" -or $mth -like "*$_*" }
            if ($close) { $hint = " -> did you mean: " + (($close | ForEach-Object { "Api.$cls.$_" }) -join ', ') + " ?" }
            else        { $hint = " -> Api.$cls has: " + ($candidates -join ', ') }
        }
        else
        {
            $hint = " -> no sub-API named '$cls'"
        }
    }
    $errors += "  ERROR [$key] '$api' does not exist$hint"
}

# ── 3. Report ─────────────────────────────────────────────────────────────────────
Write-Host "--- Drift check: index -> real API ---" -ForegroundColor Cyan
if ($errors.Count -eq 0)
{
    Write-Host "  OK - all $($referenced.Count) referenced methods exist." -ForegroundColor Green
}
else
{
    $errors | ForEach-Object { Write-Host $_ -ForegroundColor Red }
}

Write-Host "`n--- Coverage: real API not yet in index (curation backlog) ---" -ForegroundColor Cyan
$uncovered = @($real | Where-Object { -not $referenced.Contains($_) } | Sort-Object)
if ($uncovered.Count -eq 0)
{
    Write-Host "  Full coverage - every public method is indexed." -ForegroundColor Green
}
else
{
    Write-Host "  $($uncovered.Count) of $($real.Count) methods are not in the index:" -ForegroundColor Yellow
    $uncovered | Group-Object { ($_ -split '\.')[1] } | Sort-Object Name | ForEach-Object {
        Write-Host ("    {0,-16} {1}" -f $_.Name, (($_.Group | ForEach-Object { ($_ -split '\.')[2] }) -join ', ')) -ForegroundColor DarkYellow
    }
}

# ── 4. Summary ────────────────────────────────────────────────────────────────────
Write-Host "`n========================================" -ForegroundColor Cyan
if ($errors.Count -eq 0)
{
    Write-Host "  INDEX OK - no drift" -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Cyan
    exit 0
}
else
{
    Write-Host "  INDEX FAILED - $($errors.Count) dead reference(s)" -ForegroundColor Red
    Write-Host "========================================" -ForegroundColor Cyan
    exit 1
}

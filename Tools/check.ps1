#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Pre-build linter for Schedule I MelonLoader mods.
    Catches known Il2Cpp traps BEFORE dotnet build.
.PARAMETER Path
    Directory to lint (default: current directory).
.PARAMETER FirstErrorOnly
    Print exactly ONE finding (the first error, or first warning if no errors)
    as a single prescriptive line, then exit. Designed for small-LLM fix loops.
.EXAMPLE
    .\check.ps1
    .\check.ps1 -Path ..\MyMod
    .\check.ps1 -Path ..\MyMod -FirstErrorOnly
.NOTES
    Exit code: 0 = OK, 1 = findings
#>

param(
    [string]$Path = ".",
    [switch]$FirstErrorOnly
)

$findings = [System.Collections.Generic.List[pscustomobject]]::new()

function Add-Finding {
    param([string]$Severity, [string]$File, [int]$Line, [string]$Rule, [string]$Message, [string]$Fix)
    $script:findings.Add([pscustomobject]@{
        Severity = $Severity; File = $File; Line = $Line; Rule = $Rule; Message = $Message; Fix = $Fix
    })
}

$files = Get-ChildItem -Path $Path -Recurse -Filter "*.cs" | Where-Object { $_.FullName -notmatch '\\obj\\' -and $_.FullName -notmatch '\\bin\\' }

if ($files.Count -eq 0)
{
    if ($FirstErrorOnly) { Write-Host "ERROR ${Path}: no .cs files found. Fix: point -Path at a folder containing C# source files." }
    else { Write-Host "[CHECK] No .cs files found in $Path." -ForegroundColor Yellow }
    exit 1
}

# ── Load ids.json for rule 9 (optional — rule is skipped if missing) ──
$validIds = @()
$validNpcs = @()
$npcPatterns = @()
$idsPath = Join-Path $PSScriptRoot "ids.json"
if (Test-Path $idsPath)
{
    try
    {
        $ids = Get-Content $idsPath -Raw | ConvertFrom-Json
        foreach ($cat in $ids.items.PSObject.Properties) { $validIds += $cat.Value }
        $validIds += $ids.properties
        $validIds += $ids.vehicles
        $validNpcs = @($ids.npcs)
        $npcPatterns = @($ids.npcPatterns)
    }
    catch { }
}

function Get-IdSuggestions {
    param([string]$BadId, [string[]]$Pool)
    $prefix = $BadId.Substring(0, [Math]::Min(3, $BadId.Length))
    $close = @($Pool | Where-Object { $_ -like "$prefix*" } | Select-Object -First 3)
    if ($close.Count -eq 0) { $close = @($Pool | Select-Object -First 3) }
    return $close -join ", "
}

# ── Per-line rules over all files ──
foreach ($f in $files)
{
    $content = Get-Content $f.FullName -Raw
    $lines = $content -split "`n"
    for ($i = 0; $i -lt $lines.Count; $i++)
    {
        $line = $lines[$i]
        $n = $i + 1

        # Rule 1: missing Il2Cpp prefix in using
        if ($line -match '^\s*using\s+(ScheduleOne|FishNet|Steamworks)(\.|;)')
        {
            Add-Finding ERROR $f.Name $n "namespace-prefix" "missing Il2Cpp prefix: '$($matches[0].Trim())'" "use 'using Il2Cpp$($matches[1])...' instead"
        }

        # Rule 2: custom MonoBehaviour
        if ($line -match ':\s+MonoBehaviour' -and $line -notmatch '//.*: MonoBehaviour')
        {
            Add-Finding ERROR $f.Name $n "no-monobehaviour" "custom MonoBehaviour subclass" "inherit from MelonMod and use its overrides (OnUpdate etc.) instead"
        }

        # Rule 3: StartCoroutine instead of MelonCoroutines
        if ($line -match 'StartCoroutine\(' -and $line -notmatch '//.*StartCoroutine' -and $line -notmatch 'MelonCoroutines\.Start')
        {
            Add-Finding ERROR $f.Name $n "melon-coroutines" "StartCoroutine() call" "use MelonCoroutines.Start(MyRoutine()) instead"
        }

        # Rule 4: C# casts on Il2Cpp objects
        if ($line -match '\sas\s+Il2Cpp' -and $line -notmatch '//.*as\s+Il2Cpp')
        {
            Add-Finding WARN $f.Name $n "trycast" "'as Il2Cpp...' cast" "use obj.TryCast<T>() instead of 'as'"
        }

        # Rule 5: System.Threading
        if ($line -match 'System\.Threading' -and $line -notmatch '//.*System\.Threading')
        {
            Add-Finding ERROR $f.Name $n "no-threading" "System.Threading usage" "use MelonCoroutines with WaitForSeconds instead of threads/Task.Delay"
        }

        # Rule 6: async void
        if ($line -match 'async\s+void' -and $line -notmatch '//.*async void')
        {
            Add-Finding WARN $f.Name $n "no-async-void" "'async void' method" "avoid async in Unity callbacks; use MelonCoroutines instead"
        }

        # Rule 7: foreach over Il2Cpp lists
        if ($line -match 'foreach\s*\(.*Il2Cpp' -and $line -notmatch '//.*foreach')
        {
            Add-Finding WARN $f.Name $n "il2cpp-foreach" "foreach over Il2Cpp collection (slow)" "use a for loop with index access (list[i]) instead"
        }

        # Rule 9: unknown item/NPC IDs (only if ids.json was loaded)
        if ($validIds.Count -gt 0 -and $line -notmatch '^\s*//')
        {
            foreach ($m in [regex]::Matches($line, 'Api\.(Inventory\.(?:Add|Remove|GetQuantity|HasItem)|Item\.\w+|Growing\.Plant)\(\s*"([^"]+)"'))
            {
                $id = $m.Groups[2].Value
                if ($validIds -notcontains $id)
                {
                    $sug = Get-IdSuggestions $id $validIds
                    Add-Finding ERROR $f.Name $n "unknown-item-id" "unknown item ID `"$id`"" "use a valid ID from Tools/ids.json, e.g.: $sug"
                }
            }
            foreach ($m in [regex]::Matches($line, 'Api\.(?:NPC|NpcBehaviour|Combat\.(?:ApplyDamageTo|IsDead|Kill))[\.\w]*\(\s*"([^"]+)"'))
            {
                $id = $m.Groups[1].Value
                $patternMatch = $false
                foreach ($p in $npcPatterns) { if ($id -like $p) { $patternMatch = $true } }
                if ($id -and $validNpcs -notcontains $id -and -not $patternMatch)
                {
                    $sug = Get-IdSuggestions $id $validNpcs
                    Add-Finding WARN $f.Name $n "unknown-npc-id" "NPC key `"$id`" not in known list (may be a runtime-generated customer)" "known NPC keys include: $sug"
                }
            }
        }
    }
}

# Rule 8: validate mod.json
$modJson = Join-Path $Path "mod.json"
if (Test-Path $modJson)
{
    try
    {
        $manifest = Get-Content $modJson -Raw | ConvertFrom-Json
        if (-not $manifest.id -or $manifest.id -notmatch '^[a-z0-9-]+$') { Add-Finding ERROR "mod.json" 0 "manifest" "id missing or invalid" "set 'id' using only a-z, 0-9, -" }
        if (-not $manifest.name) { Add-Finding ERROR "mod.json" 0 "manifest" "name missing" "add a 'name' field" }
        if (-not $manifest.version -or $manifest.version -notmatch '^\d+\.\d+\.\d+$') { Add-Finding ERROR "mod.json" 0 "manifest" "version missing or not semver" "set 'version' like 1.0.0" }
        if (-not $manifest.gameVersion) { Add-Finding ERROR "mod.json" 0 "manifest" "gameVersion missing" "add 'gameVersion' (e.g. 0.4.3)" }
        if (-not $manifest.toolkitVersion) { Add-Finding ERROR "mod.json" 0 "manifest" "toolkitVersion missing" "add 'toolkitVersion' (e.g. 3.0.0)" }
        if ($manifest.dependencies)
        {
            foreach ($dep in $manifest.dependencies)
            {
                if (-not $dep.id -or $dep.id -notmatch '^[a-z0-9-]+$') { Add-Finding ERROR "mod.json" 0 "manifest" "dependency.id invalid: '$($dep.id)'" "dependency ids use only a-z, 0-9, -" }
            }
        }
    }
    catch
    {
        Add-Finding ERROR "mod.json" 0 "manifest" "not valid JSON: $_" "fix the JSON syntax"
    }
}
elseif (-not $FirstErrorOnly)
{
    # missing mod.json is informational only, never surfaced in fix loops
    Add-Finding WARN "mod.json" 0 "manifest" "mod.json missing" "generate it with new-mod.ps1"
}

# ── Output ──
$errors = @($findings | Where-Object Severity -eq ERROR)
$warnings = @($findings | Where-Object Severity -eq WARN)
$exitCode = if ($errors.Count -gt 0) { 1 } else { 0 }

if ($FirstErrorOnly)
{
    $first = if ($errors.Count -gt 0) { $errors[0] } elseif ($warnings.Count -gt 0) { $warnings[0] } else { $null }
    if ($null -eq $first)
    {
        Write-Host "OK - no findings"
        exit 0
    }
    Write-Host "$($first.Severity) $($first.File):$($first.Line) $($first.Message). Fix: $($first.Fix)"
    exit 1
}

Write-Host "[CHECK] Checked $($files.Count) files in $Path`n" -ForegroundColor Cyan
foreach ($fd in $findings)
{
    $color = if ($fd.Severity -eq "ERROR") { "Red" } else { "Yellow" }
    Write-Host "  $($fd.Severity) $($fd.File):$($fd.Line) [$($fd.Rule)] $($fd.Message)" -ForegroundColor $color
    Write-Host "        Fix: $($fd.Fix)" -ForegroundColor DarkYellow
}

Write-Host "`n========================================" -ForegroundColor Cyan
if ($exitCode -eq 0)
{
    Write-Host "  CHECK PASSED - $($warnings.Count) warning(s), no critical errors" -ForegroundColor Green
}
else
{
    Write-Host "  CHECK FAILED - $($errors.Count) error(s), $($warnings.Count) warning(s)" -ForegroundColor Red
}
Write-Host "========================================" -ForegroundColor Cyan

exit $exitCode

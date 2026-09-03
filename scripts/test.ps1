[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$modelProject = Join-Path $repoRoot 'tests\ModelChecks\SephiriaEnhancements.ModelChecks.csproj'

& (Join-Path $PSScriptRoot 'verify-public.ps1')

# These hooks must be installed by normal startup, not only by a test's PatchAll.
$startup = Get-Content -Raw (Join-Path $repoRoot 'SephiriaEnhancementsMod.cs')
$patchList = [regex]::Match($startup, '(?s)foreach \(Type patchType in new\[\]\s*\{(?<types>.*?)\}\)')
foreach ($patch in @(
    'DefeatRetryPlayerRestorePatch', 'RenderedCombatFloorRetryCheckpointPatch',
    'BossRetryPropRecipePatch', 'BossRetryPreserveFloorPatch',
    'BossEncounterRetryCheckpointPatch', 'SeedBossEncounterRetryCheckpointPatch',
    'NativeBossEncounterCompletedPatch', 'NativeBossEncounterPausedPatch',
    'NativeBossEncounterResumedPatch'
)) {
    if ($patchList.Groups['types'].Value -notmatch ('typeof\(' + $patch + '\)')) {
        throw "Required combat/retry hook is missing from startup: $patch"
    }
}

dotnet restore $modelProject
if ($LASTEXITCODE -ne 0) { throw 'Model check restore failed.' }

dotnet format $modelProject --verify-no-changes --no-restore --verbosity quiet
if ($LASTEXITCODE -ne 0) { throw 'Source formatting verification failed.' }

dotnet run --project $modelProject -c Release --no-restore
if ($LASTEXITCODE -ne 0) { throw 'Model checks failed.' }

Write-Host 'Portable checks passed.'

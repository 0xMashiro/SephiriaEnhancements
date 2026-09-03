[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$modelProject = Join-Path $repoRoot 'tests\ModelChecks\SephiriaEnhancements.ModelChecks.csproj'

& (Join-Path $PSScriptRoot 'verify-public.ps1')

# Automatic text fitting must use the shared size ceiling and minimum-size constraint.
$textSizingOwner = Join-Path $repoRoot 'Integration/NativeLocalizedText.cs'
foreach ($directory in @('Configuration', 'Diagnostics', 'Features', 'Integration', 'Runtime')) {
    foreach ($source in Get-ChildItem -LiteralPath (Join-Path $repoRoot $directory) -Recurse -Filter '*.cs' -File) {
        if ($source.FullName -eq $textSizingOwner) { continue }
        $assignments = [regex]::Matches((Get-Content -LiteralPath $source.FullName -Raw),
            '\b(fontSizeMin|fontSizeMax|enableAutoSizing)\s*=(?!=)\s*([^;]+);')
        foreach ($assignment in $assignments) {
            if ($assignment.Groups[1].Value -eq 'enableAutoSizing' -and
                $assignment.Groups[2].Value.Trim() -eq 'false') { continue }
            $relativePath = [System.IO.Path]::GetRelativePath($repoRoot, $source.FullName)
            throw "Use NativeLocalizedText for automatic text sizing: $relativePath"
        }
    }
}
Write-Host 'Native text sizing boundary passed.'

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

dotnet restore $modelProject --locked-mode
if ($LASTEXITCODE -ne 0) { throw 'Model check restore failed.' }

dotnet format $modelProject --verify-no-changes --no-restore --verbosity quiet
if ($LASTEXITCODE -ne 0) { throw 'Source formatting verification failed.' }

dotnet run --project $modelProject -c Release --no-restore
if ($LASTEXITCODE -ne 0) { throw 'Model checks failed.' }

Write-Host 'Portable checks passed.'

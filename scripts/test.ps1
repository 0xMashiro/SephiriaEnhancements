[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$modelProject = Join-Path $repoRoot 'tests\ModelChecks\SephiriaEnhancements.ModelChecks.csproj'

& (Join-Path $PSScriptRoot 'verify-public.ps1')
& (Join-Path $PSScriptRoot 'verify-feature-isolation.ps1')

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

# Standalone Mod messages share one local native adapter; feature bridges own network results.
foreach ($directory in @('Configuration', 'Features', 'Runtime')) {
    foreach ($source in Get-ChildItem -LiteralPath (Join-Path $repoRoot $directory) -Recurse -Filter '*.cs' -File) {
        if ((Get-Content -LiteralPath $source.FullName -Raw) -match 'GetElement<UI_SystemMessage>|\.SpawnLog\(|\.OpenYes\(') {
            throw "Use NativeModNotifications for standalone messages: $($source.Name)"
        }
    }
}
$notifications = Get-Content -LiteralPath (Join-Path $repoRoot 'Integration/NativeModNotifications.cs') -Raw
if ($notifications -match 'DungeonManager|NetworkClient\.Send|NetworkServer\.Send') {
    # Mentioning the forbidden chat API in a comment is not a network call.
    if (($notifications -replace '(?m)//[^\r\n]*', '') -match 'DungeonManager|NetworkClient\.Send|NetworkServer\.Send') {
        throw 'The local notification adapter must not send network messages.'
    }
}
Write-Host 'Local notification boundary passed.'

# New localization groups must participate in automatic coverage checks.
$modelDirectory = Split-Path -Parent $modelProject
$modelXml = [xml](Get-Content -LiteralPath $modelProject -Raw)
$modelSources = @($modelXml.Project.ItemGroup.Compile | ForEach-Object {
    if ($_.Include) { [System.IO.Path]::GetFullPath((Join-Path $modelDirectory $_.Include)) }
})
foreach ($directory in @('Configuration', 'Diagnostics', 'Features', 'Runtime')) {
    foreach ($source in Get-ChildItem -LiteralPath (Join-Path $repoRoot $directory) -Recurse -Filter '*Localization.cs' -File) {
        if ($source.FullName -notin $modelSources) {
            throw "Include localization source in ModelChecks: $([System.IO.Path]::GetRelativePath($repoRoot, $source.FullName))"
        }
    }
}
Write-Host 'Localization source coverage passed.'

# These hooks must be installed by normal startup, not only by a test's PatchAll.
$startup = Get-Content -Raw (Join-Path $repoRoot 'SephiriaEnhancementsMod.cs')
$patchList = [regex]::Match($startup, '(?s)foreach \(Type patchType in new\[\]\s*\{(?<types>.*?)\}\)')
foreach ($patch in @(
    'ModLanguageLoadPatch',
    'DefeatRetryPlayerRestorePatch', 'DefeatRetryClientNotificationPatch', 'DefeatRetryCutscenePatch', 'NativeRetryRestart', 'DefeatRetryTravelRequestPatch', 'RenderedCombatFloorRetryCheckpointPatch',
    'BossRetryPropRecipePatch', 'BossRetryPreserveFloorPatch',
    'BossEncounterRetryCheckpointPatch', 'SeedBossEncounterRetryCheckpointPatch',
    'NativeBossEncounterCompletedPatch', 'NativeBossEncounterPausedPatch',
    'NativeBossEncounterResumedPatch', 'NativeBossBarValuesPatch',
    'NativeUnitBarValuesPatch', 'NativePropBarValuesPatch',
    'NativePlayerBarValuesPatch', 'NativeCompanionBarValuesPatch', 'NativeManaBarValuesPatch',
    'ModJournalRefreshPatch', 'ModJournalClearPatch',
    'ModJournalCategoryPatch', 'ModJournalTutorialPatch',
    'RewardKeyboardGeneratedSelectionPatch', 'RewardKeyboardControlSelectionPatch',
    'RewardKeyboardInventoryOpenedSelectionPatch', 'RewardKeyboardToggleSelectionPatch',
    'RewardKeyboardClosedSelectionPatch', 'RewardKeyboardCancelSelectionPatch'
    'MapPanelShowPatch', 'MapNavigationUpdatePatch'
    'AutoCastingManualInputPatch', 'AutoCastingPanelPatch', 'AutoCastingSubmitPatch',
    'AutoCastingOptionsPatch', 'CharacterPanelNavigationMovePatch', 'CharacterPanelNavigationTogglePatch'
    'InventoryPanelCancelPatch', 'InventoryPanelTooltipPlacementPatch'
)) {
    if ($patchList.Groups['types'].Value -notmatch ('typeof\(' + $patch + '\)')) {
        throw "Required hook is missing from startup: $patch"
    }
}

$admissionPatches = [regex]::Match($startup, '(?s)MidRunAdmissionPatchTypes\s*=\s*\{(?<types>.*?)\};').Groups['types'].Value
foreach ($source in Get-ChildItem -LiteralPath (Join-Path $repoRoot 'Features/MultiplayerAccess/Integration') -Filter '*.cs' -File) {
    foreach ($declaration in [regex]::Matches((Get-Content -LiteralPath $source.FullName -Raw),
        '(?s)\[HarmonyPatch.*?internal static class\s+(\w+)')) {
        $patch = $declaration.Groups[1].Value
        if ($admissionPatches -notmatch ('typeof\(' + $patch + '\)')) {
            throw "Required admission or joining-supply hook is missing from startup: $patch"
        }
    }
}
Write-Host 'Admission and joining-supply startup hooks passed.'

dotnet restore $modelProject --locked-mode
if ($LASTEXITCODE -ne 0) { throw 'Model check restore failed.' }

dotnet format $modelProject --verify-no-changes --no-restore --verbosity quiet
if ($LASTEXITCODE -ne 0) { throw 'Source formatting verification failed.' }

dotnet run --project $modelProject -c Release --no-restore
if ($LASTEXITCODE -ne 0) { throw 'Model checks failed.' }

Write-Host 'Portable checks passed.'

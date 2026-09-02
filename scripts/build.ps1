[CmdletBinding()]
param(
    [string]$GameDir = $env:SEPHIRIA_GAME_DIR,
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',
    [switch]$DeveloperTools
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$project = Join-Path $repoRoot 'SephiriaEnhancements.csproj'

if ([string]::IsNullOrWhiteSpace($GameDir)) {
    throw 'Provide -GameDir or set SEPHIRIA_GAME_DIR to the folder containing Sephiria.exe.'
}

$gameDirPath = [System.IO.Path]::GetFullPath($GameDir)
if (-not (Test-Path -LiteralPath (Join-Path $gameDirPath 'Sephiria.exe'))) {
    throw 'Sephiria.exe was not found in the supplied game directory.'
}
$gameDirectoryProperty = "-p:SephiriaGameDir=$gameDirPath"
$flavor = if ($DeveloperTools) { 'Development' } else { 'Release' }
$outputPath = Join-Path $repoRoot "artifacts/build/$flavor"
$developerProperty = "-p:DeveloperTools=$($DeveloperTools.IsPresent.ToString().ToLowerInvariant())"

& (Join-Path $PSScriptRoot 'test.ps1')

dotnet clean $project -c $Configuration $gameDirectoryProperty
if ($LASTEXITCODE -ne 0) { throw 'Mod clean failed.' }

dotnet restore $project --locked-mode --force --no-cache
if ($LASTEXITCODE -ne 0) { throw 'Mod restore failed.' }

dotnet build $project -c $Configuration --no-restore --no-incremental `
    $gameDirectoryProperty $developerProperty -o $outputPath `
    -p:ContinuousIntegrationBuild=true -p:Deterministic=true
if ($LASTEXITCODE -ne 0) { throw 'Mod build failed.' }

& (Join-Path $PSScriptRoot 'verify-build.ps1') `
    -AssemblyPath (Join-Path $outputPath 'SephiriaEnhancements.dll') -DeveloperTools:$DeveloperTools

Write-Host "Build passed: $flavor ($Configuration)"

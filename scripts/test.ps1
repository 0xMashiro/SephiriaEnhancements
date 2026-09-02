[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$modelProject = Join-Path $repoRoot 'tests\ModelChecks\SephiriaEnhancements.ModelChecks.csproj'

& (Join-Path $PSScriptRoot 'verify-public.ps1')

dotnet restore $modelProject
if ($LASTEXITCODE -ne 0) { throw 'Model check restore failed.' }

dotnet format $modelProject --verify-no-changes --no-restore --verbosity quiet
if ($LASTEXITCODE -ne 0) { throw 'Source formatting verification failed.' }

dotnet run --project $modelProject -c Release --no-restore
if ($LASTEXITCODE -ne 0) { throw 'Model checks failed.' }

Write-Host 'Portable checks passed.'

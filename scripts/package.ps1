[CmdletBinding()]
param(
    [string]$GameDir = $env:SEPHIRIA_GAME_DIR,
    [string]$OutputDir
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem

$repoRoot = Split-Path -Parent $PSScriptRoot
$projectPath = Join-Path $repoRoot 'SephiriaEnhancements.csproj'
$packagingRoot = Join-Path $repoRoot 'packaging'
$metadataPath = Join-Path $packagingRoot 'metadata.json'

[xml]$project = Get-Content -LiteralPath $projectPath -Raw
$version = [string]($project.Project.PropertyGroup.Version |
    Where-Object { -not [string]::IsNullOrWhiteSpace([string]$_) } |
    Select-Object -First 1)
if ($version -notmatch '^(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)(?:-(?:alpha|beta|rc)\.[1-9]\d*)?$') {
    throw "Project version is not an approved Semantic Version: $version"
}

$metadata = Get-Content -LiteralPath $metadataPath -Raw | ConvertFrom-Json
if ([string]$metadata.modVersion -ne $version) {
    throw "Project version $version does not match metadata $($metadata.modVersion)."
}

& (Join-Path $PSScriptRoot 'build.ps1') -GameDir $GameDir -Configuration Release

$releaseRoot = Join-Path $repoRoot 'bin\Release\netstandard2.1'
$modAssembly = Join-Path $releaseRoot 'SephiriaEnhancements.dll'
if (-not (Test-Path -LiteralPath $modAssembly)) {
    throw 'Release assembly was not produced.'
}

$nugetRoot = if (-not [string]::IsNullOrWhiteSpace($env:NUGET_PACKAGES)) {
    $env:NUGET_PACKAGES
}
else {
    Join-Path ([Environment]::GetFolderPath('UserProfile')) '.nuget\packages'
}
$harmonyRoot = Join-Path $nugetRoot 'lib.harmony\2.4.2\lib\net472'
$harmony = Join-Path $harmonyRoot '0Harmony.dll'
if (-not (Test-Path -LiteralPath $harmony)) {
    throw 'Harmony 2.4.2 net472 runtime was not restored.'
}

if ([string]::IsNullOrWhiteSpace($OutputDir)) {
    $OutputDir = Join-Path $repoRoot "artifacts\$version"
}
$outputPath = [System.IO.Path]::GetFullPath($OutputDir)
$zipPath = Join-Path $outputPath "SephiriaEnhancements-$version.zip"
$checksumPath = Join-Path $outputPath 'SHA256SUMS.txt'

if ((Test-Path -LiteralPath $zipPath) -or (Test-Path -LiteralPath $checksumPath)) {
    throw "Refusing to overwrite existing release output: $outputPath"
}
New-Item -ItemType Directory -Path $outputPath -Force | Out-Null

$files = @(
    [pscustomobject]@{ Source = Join-Path $repoRoot 'README.md'; Entry = 'README.md' }
    [pscustomobject]@{ Source = Join-Path $repoRoot 'README.zh-CN.md'; Entry = 'README.zh-CN.md' }
    [pscustomobject]@{ Source = $metadataPath; Entry = 'AddOns/SephiriaEnhancements/metadata.json' }
    [pscustomobject]@{ Source = Join-Path $packagingRoot 'THIRD-PARTY-NOTICES.txt'; Entry = 'AddOns/SephiriaEnhancements/THIRD-PARTY-NOTICES.txt' }
    [pscustomobject]@{ Source = $modAssembly; Entry = 'AddOns/SephiriaEnhancements/SephiriaEnhancements.dll' }
    [pscustomobject]@{ Source = $harmony; Entry = 'AddOns/SephiriaEnhancements/0Harmony.dll' }
) | Sort-Object Entry

function Add-ArchiveFile {
    param(
        [System.IO.Compression.ZipArchive]$Archive,
        [string]$Source,
        [string]$EntryName
    )

    $entry = $Archive.CreateEntry($EntryName, [System.IO.Compression.CompressionLevel]::Optimal)
    $entry.LastWriteTime = [DateTimeOffset]::new(2000, 1, 1, 0, 0, 0, [TimeSpan]::Zero)
    $inputStream = [System.IO.File]::OpenRead($Source)
    $outputStream = $entry.Open()
    try {
        $inputStream.CopyTo($outputStream)
    }
    finally {
        $outputStream.Dispose()
        $inputStream.Dispose()
    }
}

$archive = [System.IO.Compression.ZipFile]::Open(
    $zipPath,
    [System.IO.Compression.ZipArchiveMode]::Create)
try {
    foreach ($file in $files) {
        Add-ArchiveFile -Archive $archive -Source $file.Source -EntryName $file.Entry
    }
}
finally {
    $archive.Dispose()
}

$archive = [System.IO.Compression.ZipFile]::OpenRead($zipPath)
try {
    $actualEntries = @($archive.Entries.FullName | Sort-Object)
    $expectedEntries = @($files.Entry | Sort-Object)
    if (($actualEntries -join "`n") -ne ($expectedEntries -join "`n")) {
        throw 'Release archive contents do not match the approved package manifest.'
    }
}
finally {
    $archive.Dispose()
}

$hash = (Get-FileHash -LiteralPath $zipPath -Algorithm SHA256).Hash.ToLowerInvariant()
[System.IO.File]::WriteAllText($checksumPath, "$hash  $([System.IO.Path]::GetFileName($zipPath))`n")

Write-Host "Package: $zipPath"
Write-Host "Checksums: $checksumPath"
Write-Host "SHA-256: $hash"

[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot

$requiredFiles = @(
    '.gitattributes'
    '.gitignore'
    'global.json'
    'LICENSE'
    'packages.lock.json'
    'README.md'
    'README.zh-CN.md'
    'SephiriaEnhancements.csproj'
    'packaging/metadata.json'
    'packaging/THIRD-PARTY-NOTICES.txt'
)

foreach ($relativePath in $requiredFiles) {
    if (-not (Test-Path -LiteralPath (Join-Path $repoRoot $relativePath))) {
        throw "Required public file is missing: $relativePath"
    }
}

$forbiddenNames = @(
    'AGENTS.md'
    'ARCHITECTURE.md'
    'COMPATIBILITY.md'
)

foreach ($name in $forbiddenNames) {
    $match = Get-ChildItem -LiteralPath $repoRoot -Recurse -File -Force |
        Where-Object Name -EQ $name |
        Select-Object -First 1
    if ($null -ne $match) {
        throw "Internal file is inside the public boundary: $($match.FullName)"
    }
}

$excludedDirectories = @('.git', 'artifacts', 'bin', 'obj', 'package', 'TestResults')
$allowedRootFiles = @(
    '.gitattributes'
    '.gitignore'
    'global.json'
    'LICENSE'
    'packages.lock.json'
    'README.md'
    'README.zh-CN.md'
    'SephiriaEnhancements.csproj'
    'SephiriaEnhancementsMod.cs'
)
$allowedRootDirectories = @(
    '.github'
    'assets'
    'Configuration'
    'Diagnostics'
    'Features'
    'Integration'
    'Runtime'
    'packaging'
    'scripts'
    'tests'
)
$allowedExtensions = @('.cs', '.csproj', '.json', '.md', '.ps1', '.txt', '.webp', '.yml')

$publicFiles = Get-ChildItem -LiteralPath $repoRoot -Recurse -File -Force |
    Where-Object {
        $relativePath = [System.IO.Path]::GetRelativePath($repoRoot, $_.FullName)
        $segments = $relativePath -split '[\\/]'
        -not ($segments | Where-Object { $_ -in $excludedDirectories })
    }

foreach ($file in $publicFiles) {
    $relativePath = [System.IO.Path]::GetRelativePath($repoRoot, $file.FullName).Replace('\', '/')
    $segments = $relativePath -split '/'
    $rootEntry = $segments[0]

    if ($segments.Count -eq 1) {
        if ($rootEntry -notin $allowedRootFiles) {
            throw "Unexpected file at public repository root: $relativePath"
        }
    }
    elseif ($rootEntry -notin $allowedRootDirectories) {
        throw "Unexpected public directory or file: $relativePath"
    }

    $extension = [System.IO.Path]::GetExtension($file.Name)
    if ($file.Name -ne '.gitattributes' -and $file.Name -ne '.gitignore' -and
        $file.Name -ne 'LICENSE' -and
        $extension -notin $allowedExtensions) {
        throw "File type is not on the public whitelist: $relativePath"
    }
}

[xml]$project = Get-Content -LiteralPath (Join-Path $repoRoot 'SephiriaEnhancements.csproj') -Raw
$version = [string]($project.Project.PropertyGroup.Version |
    Where-Object { -not [string]::IsNullOrWhiteSpace([string]$_) } |
    Select-Object -First 1)
if ($version -notmatch '^(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)(?:-(?:alpha|beta|rc)\.[1-9]\d*)?$') {
    throw "Project version is not an approved Semantic Version: $version"
}

$metadata = Get-Content -LiteralPath (Join-Path $repoRoot 'packaging/metadata.json') -Raw |
    ConvertFrom-Json
if ([string]$metadata.modVersion -ne $version) {
    throw "Project version $version does not match metadata $($metadata.modVersion)."
}

$textExtensions = @('.cs', '.csproj', '.json', '.md', '.ps1', '.txt', '.yml')
$sensitivePatterns = @(
    '(?i)\bClover\b'
    '(?i)[A-Z]:[\\/]Users[\\/][^\\/\s]+'
    '(?i)C:[\\/]Workspace[\\/]'
    '(?i)/home/[^/\s]+'
    '(?i)(?:192\.168|10\.\d{1,3}|172\.(?:1[6-9]|2\d|3[01]))\.\d{1,3}\.\d{1,3}'
    '(?i)(?:api[_-]?key|access[_-]?token|client[_-]?secret|private[_-]?key)\s*[:=]\s*["'']?[^\s"'']+'
)

foreach ($file in $publicFiles) {
    if ($file.FullName -eq $PSCommandPath) {
        continue
    }

    $extension = [System.IO.Path]::GetExtension($file.Name)
    if ($file.Name -ne 'LICENSE' -and $file.Name -ne '.gitignore' -and
        $extension -notin $textExtensions) {
        continue
    }

    $content = Get-Content -LiteralPath $file.FullName -Raw
    foreach ($pattern in $sensitivePatterns) {
        if ($content -match $pattern) {
            $relativePath = [System.IO.Path]::GetRelativePath($repoRoot, $file.FullName)
            throw "Potential private or secret data found in: $relativePath"
        }
    }
}

Write-Host "Public boundary verified: $($publicFiles.Count) files"

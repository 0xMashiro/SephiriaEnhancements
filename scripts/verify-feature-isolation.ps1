[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$registrations = Get-Content -LiteralPath (Join-Path $repoRoot 'Runtime/FeaturePatches.cs') -Raw
$owners = @{}
foreach ($entry in [regex]::Matches($registrations, '\(FeatureId\.(\w+),\s*typeof\(global::([\w.]+)\)\)')) {
    $name = $entry.Groups[2].Value
    if ($owners.ContainsKey($name)) { throw "Patch is registered more than once: $name" }
    $owners[$name] = $entry.Groups[1].Value
}
$declared = [System.Collections.Generic.HashSet[string]]::new()
$patchCount = 0
$callbackCount = 0
foreach ($directory in @('Configuration', 'Diagnostics', 'Features', 'Integration', 'Runtime')) {
    foreach ($source in Get-ChildItem -LiteralPath (Join-Path $repoRoot $directory) -Recurse -Filter '*.cs' -File) {
        $content = Get-Content -LiteralPath $source.FullName -Raw
        foreach ($patch in [regex]::Matches($content,
            '(?ms)^    \[HarmonyPatch.*?^    (?:internal|public) (?:static )?class (?<name>\w+).*?^    \{(?<body>.*?)^    \}')) {
            $name = $patch.Groups['name'].Value
            $body = $patch.Groups['body'].Value
            $body = $body -replace '(?m)^\s*//[^\r\n]*', ''
            $namespace = [regex]::Match($content, '(?m)^namespace ([\w.]+)').Groups[1].Value
            $fullName = "$namespace.$name"
            if (!$owners.ContainsKey($fullName)) { throw "Patch has no installation registration: $fullName" }
            [void]$declared.Add($fullName)
            $patchCount++
            foreach ($callback in [regex]::Matches($body,
                '(?ms)^        (?:private|internal|public) static [\w?.<>]+ (?<name>Prefix|Postfix|Finalizer)\([^;{}]*\)\s*\{(?<body>.*?)^        \}')) {
                $entry = $callback.Groups['name'].Value
                $wrapper = $callback.Groups['body'].Value
                $failureOwner = [regex]::Match($wrapper, 'FeatureFailure.Disable\(FeatureId\.(\w+), exception\)').Groups[1].Value
                if ($failureOwner -ne $owners[$fullName]) {
                    throw "Patch failure owner differs from installation owner: $name.$entry"
                }
                if ($wrapper -notmatch '\btry\b' -or $wrapper -notmatch 'catch \(System.Exception exception\)' -or
                    $wrapper -notmatch 'FeatureFailure.Disable\(FeatureId\.\w+, exception\)' -or
                    $wrapper -notmatch ($entry + 'Core\(')) {
                    throw "Patch callback lacks a feature failure boundary: $name.$entry"
                }
                if ($body -notmatch ('MethodImpl\(System.Runtime.CompilerServices.MethodImplOptions.NoInlining\)\]\s*private static [\w?.<>]+ ' + $entry + 'Core\(')) {
                    throw "Patch callback lacks a JIT isolation boundary: $name.$entry"
                }
                $callbackCount++
            }
        }
    }
}
foreach ($name in $owners.Keys) {
    if (!$declared.Contains($name)) { throw "Registration is not a declared Harmony patch: $name" }
}
if ($patchCount -eq 0 -or $callbackCount -lt $patchCount) {
    throw 'Feature isolation scan did not cover all patch classes.'
}
Write-Host "Feature isolation boundaries passed ($patchCount patch classes, $callbackCount callbacks)."

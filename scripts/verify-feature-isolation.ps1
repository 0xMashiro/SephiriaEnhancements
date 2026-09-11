[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$ownership = Get-Content -LiteralPath (Join-Path $repoRoot 'Runtime/FeaturePatchOwnership.cs') -Raw
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
            if ($ownership -notmatch ('\.' + [regex]::Escape($name) + '"')) {
                throw "Patch has no feature owner: $name"
            }
            $patchCount++
            foreach ($callback in [regex]::Matches($body,
                '(?ms)^        (?:private|internal|public) static [\w?.<>]+ (?<name>Prefix|Postfix|Finalizer)\([^;{}]*\)\s*\{(?<body>.*?)^        \}')) {
                $entry = $callback.Groups['name'].Value
                $wrapper = $callback.Groups['body'].Value
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
if ($patchCount -eq 0 -or $callbackCount -lt $patchCount) {
    throw 'Feature isolation scan did not cover all patch classes.'
}
Write-Host "Feature isolation boundaries passed ($patchCount patch classes, $callbackCount callbacks)."

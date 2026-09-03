[CmdletBinding()]
param(
    [Parameter(Mandatory)] [string]$AssemblyPath,
    [switch]$DeveloperTools
)

$ErrorActionPreference = 'Stop'
$stream = [IO.File]::OpenRead([IO.Path]::GetFullPath($AssemblyPath))
$pe = [Reflection.PortableExecutable.PEReader]::new($stream)
try {
    # Read metadata without loading or executing the Mod and its game dependencies.
    $reader = [Reflection.Metadata.PEReaderExtensions]::GetMetadataReader($pe)
    $types = @($reader.TypeDefinitions | ForEach-Object {
        $definition = $reader.GetTypeDefinition($_)
        $reader.GetString($definition.Namespace) + '.' + $reader.GetString($definition.Name)
    })
    $flavors = @($reader.GetAssemblyDefinition().GetCustomAttributes() | ForEach-Object {
        $attribute = $reader.GetCustomAttribute($_)
        if ($attribute.Constructor.Kind -ne [Reflection.Metadata.HandleKind]::MemberReference) { return }
        $constructor = $reader.GetMemberReference([Reflection.Metadata.MemberReferenceHandle]$attribute.Constructor)
        if ($constructor.Parent.Kind -ne [Reflection.Metadata.HandleKind]::TypeReference) { return }
        $type = $reader.GetTypeReference([Reflection.Metadata.TypeReferenceHandle]$constructor.Parent)
        if ($reader.GetString($type.Name) -ne 'AssemblyMetadataAttribute') { return }
        $blob = $reader.GetBlobReader($attribute.Value)
        if ($blob.ReadUInt16() -ne 1) { throw 'Invalid assembly metadata.' }
        $key = $blob.ReadSerializedString()
        $value = $blob.ReadSerializedString()
        if ($key -eq 'BuildFlavor') { $value }
    })
    $expected = if ($DeveloperTools) { 'Development' } else { 'Release' }
    if ($flavors.Count -ne 1 -or $flavors[0] -cne $expected) {
        throw "Assembly build identity does not match $expected."
    }
    foreach ($required in @('SupportLogger', 'SupportLog', 'RollingLogFile')) {
        if ($types -notcontains "SephiriaEnhancements.Diagnostics.$required") {
            throw "Missing support logging component: $required"
        }
    }
    foreach ($developmentType in @(
        'SephiriaEnhancements.DeveloperTools.DeveloperPlayerDamagePatch'
        'SephiriaEnhancements.DeveloperTools.DeveloperPlayerDamageSettings'
        'SephiriaEnhancements.Diagnostics.NativeStartupProfilingPatch'
        'SephiriaEnhancements.Diagnostics.NativeLoadingOperationProfilingPatch'
        'SephiriaEnhancements.Diagnostics.InventoryReproductionCase'
        'SephiriaEnhancements.Diagnostics.InventoryReproductionEvidence'
        'SephiriaEnhancements.Diagnostics.InventoryReproductionJson'
        'SephiriaEnhancements.Diagnostics.InventoryReproductionLog'
        'SephiriaEnhancements.Diagnostics.InventoryReproductionLocalization'
        'SephiriaEnhancements.DeveloperTools.InventoryReproductionSettings'
        'SephiriaEnhancements.DeveloperTools.InventoryReproductionOption'
    )) {
        if (($types -contains $developmentType) -ne [bool]$DeveloperTools) {
            throw "Unexpected developer component for ${expected}: $developmentType"
        }
    }
    $logger = $reader.TypeDefinitions | Where-Object {
        $definition = $reader.GetTypeDefinition($_)
        $reader.GetString($definition.Name) -eq 'DeveloperLogger'
    } | Select-Object -First 1
    $loggerMethods = @($reader.GetTypeDefinition($logger).GetMethods() | ForEach-Object {
        $reader.GetString($reader.GetMethodDefinition($_).Name)
    })
    if (($loggerMethods -contains 'WriterLoop') -ne [bool]$DeveloperTools) {
        throw "Unexpected detailed diagnostics implementation for $expected."
    }
    Write-Host "Assembly boundary verified: $expected"
}
finally {
    $pe.Dispose()
    $stream.Dispose()
}

param([string]$ManifestPath)

$ErrorActionPreference = "Stop"

function Test-TextContains([string]$Text, [string]$Value, [System.StringComparison]$Comparison = [System.StringComparison]::Ordinal) {
    return $Text.IndexOf($Value, $Comparison) -ge 0
}

if (-not (Test-Path $ManifestPath)) {
    throw "[SFB-COMPOSITION-000] composition-manifest.yaml is missing: $ManifestPath"
}

$manifest = Get-Content -LiteralPath $ManifestPath -Raw

foreach ($field in @("projectName", "storeKey", "sourceStarterPath", "starterContractVersion", "packageVersions", "generatedFileRoot", "assetRoot", "shellComposition", "pageComposition", "slotBindings", "featureDecisions", "fallbackPages", "evidenceReferences", "inferenceReferences")) {
    if (-not (Test-TextContains $manifest $field)) {
        throw "[SFB-COMPOSITION-001] Manifest field '$field' is missing."
    }
}

if ($manifest -match "starterContractVersion:\s*(unknown|$)") {
    throw "[SFB-COMPOSITION-002] Missing Starter contract version."
}

foreach ($packageProperty in @("StorefrontClientPackageVersion", "StorefrontRuntimePackageVersion", "StorefrontPresentationPackageVersion", "StorefrontComponentsPackageVersion", "StorefrontBrowserPackageVersion")) {
    if ($manifest -notmatch "$packageProperty\s*:\s*(?!unknown)([^\r\n]+)") {
        throw "[SFB-COMPOSITION-003] Missing package version '$packageProperty'."
    }
}

Write-Host "StorefrontBuilder composition validation passed for $ManifestPath."

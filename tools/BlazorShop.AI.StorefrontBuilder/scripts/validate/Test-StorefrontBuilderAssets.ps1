param([Parameter(Mandatory = $true)][string]$ProjectRoot)

$ErrorActionPreference = "Stop"

$manifestPath = Join-Path $ProjectRoot "docs\storefront-analysis\asset-manifest.yaml"
if (-not (Test-Path $manifestPath)) {
    throw "[SFB-ASSET-000] asset-manifest.yaml is missing."
}

$manifest = Get-Content -LiteralPath $manifestPath -Raw
foreach ($field in @("sourceUrl", "checksum: sha256:", "contentType", "detectedUsage", "normalizedFilename", "duplicateOf", "allowedToCopy", "replacementNeeded", "replacementList")) {
    if (-not $manifest.Contains($field, [System.StringComparison]::Ordinal)) {
        throw "[SFB-ASSET-001] Asset manifest is missing '$field'."
    }
}

if (-not $manifest.Contains("makes no production licensing claim", [System.StringComparison]::Ordinal)) {
    throw "[SFB-ASSET-002] Asset manifest must not claim reference-site production licensing."
}

$assetMatches = [regex]::Matches($manifest, "replacementPath:\s+([^\r\n]+)")
foreach ($match in $assetMatches) {
    $relative = $match.Groups[1].Value.Trim()
    $assetPath = Join-Path $ProjectRoot $relative
    if (-not (Test-Path $assetPath)) {
        throw "[SFB-ASSET-003] Replacement placeholder asset is missing: $relative"
    }
}

Write-Host "StorefrontBuilder asset validation passed for $ProjectRoot."

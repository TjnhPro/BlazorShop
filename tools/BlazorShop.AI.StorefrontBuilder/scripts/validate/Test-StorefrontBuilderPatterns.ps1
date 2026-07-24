param([string]$PatternsPath)

$ErrorActionPreference = "Stop"
if (-not (Test-Path $PatternsPath)) {
    throw "[SFB-PATTERN-000] Pattern inventory file is missing: $PatternsPath"
}

$content = Get-Content -LiteralPath $PatternsPath -Raw
foreach ($required in @("product-card", "product-purchase-block")) {
    $hasPattern = $content.Contains("patternId: $required", [System.StringComparison]::Ordinal)
    $hasFallback = $content.Contains("fallbackBehavior:", [System.StringComparison]::Ordinal)
    if (-not $hasPattern -and -not $hasFallback) {
        throw "[SFB-PATTERN-001] Required pattern '$required' is missing without fallback reason."
    }
}

foreach ($field in @("evidenceIds", "selectorSamples", "visualProperties", "statesObserved", "responsiveNotes", "targetSlot", "fallbackBehavior")) {
    if (-not $content.Contains($field, [System.StringComparison]::Ordinal)) {
        throw "[SFB-PATTERN-002] Pattern inventory is missing required field '$field'."
    }
}

Write-Host "StorefrontBuilder pattern validation passed for $PatternsPath."

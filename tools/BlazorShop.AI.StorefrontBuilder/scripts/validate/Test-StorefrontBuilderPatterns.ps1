param([string]$PatternsPath)

$ErrorActionPreference = "Stop"
if (-not (Test-Path $PatternsPath)) {
    throw "[SFB-PATTERN-000] Pattern inventory file is missing: $PatternsPath"
}

function Test-TextContains([string]$Text, [string]$Value, [System.StringComparison]$Comparison = [System.StringComparison]::Ordinal) {
    return $Text.IndexOf($Value, $Comparison) -ge 0
}

$content = Get-Content -LiteralPath $PatternsPath -Raw
foreach ($required in @("product-card", "product-purchase-block")) {
    $hasPattern = Test-TextContains $content "patternId: $required"
    $hasFallback = Test-TextContains $content "fallbackBehavior:"
    if (-not $hasPattern -and -not $hasFallback) {
        throw "[SFB-PATTERN-001] Required pattern '$required' is missing without fallback reason."
    }
}

foreach ($field in @("evidenceIds", "selectorSamples", "visualProperties", "statesObserved", "responsiveNotes", "targetSlot", "fallbackBehavior")) {
    if (-not (Test-TextContains $content $field)) {
        throw "[SFB-PATTERN-002] Pattern inventory is missing required field '$field'."
    }
}

Write-Host "StorefrontBuilder pattern validation passed for $PatternsPath."

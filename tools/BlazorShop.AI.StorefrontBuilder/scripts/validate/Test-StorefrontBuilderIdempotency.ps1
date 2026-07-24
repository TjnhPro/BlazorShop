param([Parameter(Mandatory = $true)][string]$ProjectRoot)

$ErrorActionPreference = "Stop"

$manifestPath = Join-Path $ProjectRoot "docs\storefront-analysis\generated-files.yaml"
$reportPath = Join-Path $ProjectRoot "docs\storefront-analysis\regeneration-report.md"
foreach ($path in @($manifestPath, $reportPath)) {
    if (-not (Test-Path $path)) {
        throw "[SFB-IDEMPOTENCY-000] Required regeneration artifact is missing: $path"
    }
}

$manifest = Get-Content -LiteralPath $manifestPath -Raw
foreach ($field in @("filePath", "ownership", "generatorVersion", "sourceArtifactIds", "sourceSpecHash", "generatedHash", "lastGeneratedTimestamp", "manualEditDetected", "conflictStatus")) {
    if (-not $manifest.Contains($field, [System.StringComparison]::Ordinal)) {
        throw "[SFB-IDEMPOTENCY-001] Generated file manifest is missing '$field'."
    }
}

$analysisRoot = Split-Path -Parent $manifestPath
foreach ($match in [regex]::Matches($manifest, "(?m)^\s+sourceArtifactIds:\s+(.+)$")) {
    $artifactIds = $match.Groups[1].Value.Split(" ", [System.StringSplitOptions]::RemoveEmptyEntries)
    foreach ($artifactId in $artifactIds) {
        if ($artifactId -eq "none") {
            continue
        }

        $artifactPath = Join-Path $analysisRoot $artifactId
        if (-not (Test-Path $artifactPath)) {
            throw "[SFB-IDEMPOTENCY-005] Generated file manifest references missing source artifact '$artifactId'."
        }
    }
}

if ($manifest.Contains("ownership: protected", [System.StringComparison]::Ordinal) -and $manifest.Contains("conflictStatus: modified", [System.StringComparison]::Ordinal)) {
    throw "[SFB-IDEMPOTENCY-002] Protected files must never be modified."
}

if ($manifest.Contains("manualEditDetected: true", [System.StringComparison]::Ordinal) -and -not $manifest.Contains("conflictStatus:", [System.StringComparison]::Ordinal)) {
    throw "[SFB-IDEMPOTENCY-003] Manual changes must produce a conflict status."
}

$report = Get-Content -LiteralPath $reportPath -Raw
foreach ($command in @("Scope all", "Scope page", "Scope component", "Scope css", "Scope validate", "Scope conflicts", "no unexpected file changes")) {
    if (-not $report.Contains($command, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "[SFB-IDEMPOTENCY-004] Regeneration report is missing command proof '$command'."
    }
}

Write-Host "StorefrontBuilder idempotency validation passed for $ProjectRoot."

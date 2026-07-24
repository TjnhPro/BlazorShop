param(
    [string]$AnalysisRoot
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($AnalysisRoot)) {
    throw "[SFB-INFERENCE-000] AnalysisRoot is required."
}

$logPath = Join-Path $AnalysisRoot "ai-inference-log.json"
if (-not (Test-Path $logPath)) {
    throw "[SFB-INFERENCE-001] AI inference log is required at '$logPath'."
}

$log = Get-Content -LiteralPath $logPath -Raw | ConvertFrom-Json
if ("inferences" -notin $log.PSObject.Properties.Name) {
    throw "[SFB-INFERENCE-002] AI inference log must contain an 'inferences' array."
}

$requiredFields = @(
    "inferenceId",
    "decision",
    "evidenceIds",
    "confidence",
    "alternativesConsidered",
    "impactIfWrong",
    "humanReviewStatus"
)

$inferenceIds = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
foreach ($entry in $log.inferences) {
    $names = $entry.PSObject.Properties.Name
    foreach ($field in $requiredFields) {
        if ($field -notin $names) {
            throw "[SFB-INFERENCE-003] Inference log entry is missing '$field'."
        }
    }

    $inferenceIds.Add([string]$entry.inferenceId) | Out-Null
}

$artifactFiles = Get-ChildItem -LiteralPath $AnalysisRoot -Recurse -Filter "*.json" -File |
    Where-Object { $_.Name -ne "ai-inference-log.json" -and $_.FullName -notmatch "\\evidence\\" }

foreach ($file in $artifactFiles) {
    $artifact = Get-Content -LiteralPath $file.FullName -Raw | ConvertFrom-Json
    $isInferredDecision = "source" -in $artifact.PSObject.Properties.Name `
        -and [string]$artifact.source -eq "inference"
    if ($isInferredDecision) {
        $hasLoggedInference = "inferenceId" -in $artifact.PSObject.Properties.Name `
            -and $inferenceIds.Contains([string]$artifact.inferenceId)
        if (-not $hasLoggedInference) {
            throw "[SFB-INFERENCE-004] Generation decision '$($file.FullName)' marks source inference without a matching inference-log entry."
        }
    }
}

Write-Host "StorefrontBuilder AI inference validation passed for $AnalysisRoot."

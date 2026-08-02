param([string]$PlanPath)

$ErrorActionPreference = "Stop"

if (-not (Test-Path $PlanPath)) {
    throw "[SFB-GENPLAN-000] generation-plan.yaml is missing: $PlanPath"
}

$plan = Get-Content -LiteralPath $PlanPath -Raw

foreach ($field in @("filePath", "targetPath", "ownership", "action", "sourceArtifactIds", "sourceHandoffArtifacts", "sourceEvidenceReferences", "expectedSlot", "validationRuleIds", "conflictBehavior", "sourceSpecHash", "generatedHash", "rationale")) {
    if (-not $plan.Contains($field, [System.StringComparison]::Ordinal)) {
        throw "[SFB-GENPLAN-001] File plan field '$field' is missing."
    }
}

if (-not $plan.Contains("generate-from-starter", [System.StringComparison]::Ordinal) -or -not $plan.Contains("apply-visual-files", [System.StringComparison]::Ordinal)) {
    throw "[SFB-GENPLAN-002] New project must be generated from Starter before visual files are applied."
}

foreach ($field in @("sourceHandoffPackageHash", "sourceHandoffReadinessHash", "sourceStarterContractHash", "generationMode", "slots", "assets", "copyBlocks", "tokens", "warnings", "blockedItems")) {
    if (-not $plan.Contains($field, [System.StringComparison]::Ordinal)) {
        throw "[SFB-GENPLAN-004] Handoff generation-plan field '$field' is missing."
    }
}

if ($plan -match "sourceEvidenceReferences:[\s\S]*?(captures/|analysis/pages/|analysis/resolved/|presentation-catalog/|review/|reports/)") {
    throw "[SFB-GENPLAN-005] Generation plan contains raw or source-only evidence references."
}

$lines = $plan -split "`r?`n"
for ($index = 0; $index -lt $lines.Count; $index++) {
    if ($lines[$index].Contains("ownership: protected", [System.StringComparison]::Ordinal)) {
        $end = [Math]::Min($index + 8, $lines.Count - 1)
        $window = ($lines[$index..$end] -join "`n")
        if ($window -match "action:\s+(create|replace|patch)") {
            throw "[SFB-GENPLAN-003] Protected files cannot have create, replace, or patch actions."
        }
    }
}

Write-Host "StorefrontBuilder generation plan validation passed for $PlanPath."

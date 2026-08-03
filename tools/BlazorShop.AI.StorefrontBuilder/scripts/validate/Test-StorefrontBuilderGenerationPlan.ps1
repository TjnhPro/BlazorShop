param([string]$PlanPath)

$ErrorActionPreference = "Stop"

function Test-TextContains([string]$Text, [string]$Value, [System.StringComparison]$Comparison = [System.StringComparison]::Ordinal) {
    return $Text.IndexOf($Value, $Comparison) -ge 0
}

if (-not (Test-Path $PlanPath)) {
    throw "[SFB-GENPLAN-000] generation-plan.yaml is missing: $PlanPath"
}

$plan = Get-Content -LiteralPath $PlanPath -Raw

foreach ($field in @("filePath", "targetPath", "ownership", "action", "sourceArtifactIds", "sourceHandoffArtifacts", "sourceEvidenceReferences", "expectedSlot", "validationRuleIds", "conflictBehavior", "sourceSpecHash", "generatedHash", "rationale")) {
    if (-not (Test-TextContains $plan $field)) {
        throw "[SFB-GENPLAN-001] File plan field '$field' is missing."
    }
}

if (-not (Test-TextContains $plan "generate-from-starter") -or -not (Test-TextContains $plan "apply-visual-files")) {
    throw "[SFB-GENPLAN-002] New project must be generated from Starter before visual files are applied."
}

foreach ($field in @("sourceHandoffPackageHash", "sourceHandoffReadinessHash", "sourceStarterContractHash", "generationMode", "slots", "assets", "copyBlocks", "tokens", "warnings", "blockedItems")) {
    if (-not (Test-TextContains $plan $field)) {
        throw "[SFB-GENPLAN-004] Handoff generation-plan field '$field' is missing."
    }
}

if ($plan -match "sourceEvidenceReferences:[\s\S]*?(captures/|analysis/pages/|analysis/resolved/|presentation-catalog/|review/|reports/)") {
    throw "[SFB-GENPLAN-005] Generation plan contains raw or source-only evidence references."
}

$lines = $plan -split "`r?`n"
for ($index = 0; $index -lt $lines.Count; $index++) {
    if (Test-TextContains $lines[$index] "ownership: protected") {
        $end = [Math]::Min($index + 8, $lines.Count - 1)
        $window = ($lines[$index..$end] -join "`n")
        if ($window -match "action:\s+(create|replace|patch)") {
            throw "[SFB-GENPLAN-003] Protected files cannot have create, replace, or patch actions."
        }
    }
}

Write-Host "StorefrontBuilder generation plan validation passed for $PlanPath."

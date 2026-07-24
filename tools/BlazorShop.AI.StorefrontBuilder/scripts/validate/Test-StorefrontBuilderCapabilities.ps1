param(
    [string]$CapabilitiesPath,
    [string]$ContractPath = "BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/starter-generation.contract.yaml"
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path $CapabilitiesPath)) {
    throw "[SFB-CAPABILITY-000] capability-decisions.yaml is missing: $CapabilitiesPath"
}

if (-not (Test-Path $ContractPath)) {
    throw "[SFB-CAPABILITY-001] Starter generation contract is missing: $ContractPath"
}

$capabilities = Get-Content -LiteralPath $CapabilitiesPath -Raw
$contract = Get-Content -LiteralPath $ContractPath -Raw
$slots = [regex]::Matches($contract, "(?m)^\s+- id:\s+([a-z0-9.-]+)\s*$") | ForEach-Object { $_.Groups[1].Value }

foreach ($inputName in @("Starter feature manifest", "Backend public configuration feature map", "Store module manifest if available", "Target visual detections", "Starter generation contract slots")) {
    if (-not $capabilities.Contains($inputName, [System.StringComparison]::Ordinal)) {
        throw "[SFB-CAPABILITY-002] Capability input '$inputName' is missing."
    }
}

foreach ($decision in @("target", "target-with-starter-binding", "starter", "hidden", "unsupported")) {
    if (-not $capabilities.Contains($decision, [System.StringComparison]::Ordinal)) {
        throw "[SFB-CAPABILITY-003] Decision value '$decision' is missing."
    }
}

$lines = $capabilities -split "`r?`n"
for ($index = 0; $index -lt $lines.Count; $index++) {
    if ($lines[$index].Contains("decision: target-with-starter-binding", [System.StringComparison]::Ordinal)) {
        $start = [Math]::Max($index - 5, 0)
        $end = [Math]::Min($index + 6, $lines.Count - 1)
        $window = ($lines[$start..$end] -join "`n")
        $slotMatch = [regex]::Match($window, "slotId:\s+([a-z0-9.-]+)")
        if (-not $slotMatch.Success -or -not ($slots -contains $slotMatch.Groups[1].Value)) {
            throw "[SFB-CAPABILITY-004] target-with-starter-binding references a missing Starter slot."
        }
    }

    if ($lines[$index].Contains("decision: unsupported", [System.StringComparison]::Ordinal)) {
        $window = ($lines[$index..([Math]::Min($index + 8, $lines.Count - 1))] -join "`n")
        if (-not $window.Contains("fallbackDecision:", [System.StringComparison]::Ordinal)) {
            throw "[SFB-CAPABILITY-005] Unsupported feature has no user-facing fallback decision."
        }
    }
}

Write-Host "StorefrontBuilder capability validation passed for $CapabilitiesPath."

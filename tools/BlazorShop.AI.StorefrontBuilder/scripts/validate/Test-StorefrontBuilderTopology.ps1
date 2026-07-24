param(
    [string]$TopologyPath,
    [string]$ContractPath = "BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/starter-generation.contract.yaml"
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path $TopologyPath)) {
    throw "[SFB-TOPOLOGY-000] page-topology.yaml is missing: $TopologyPath"
}

if (-not (Test-Path $ContractPath)) {
    throw "[SFB-TOPOLOGY-001] Starter generation contract is missing: $ContractPath"
}

$topology = Get-Content -LiteralPath $TopologyPath -Raw
$contract = Get-Content -LiteralPath $ContractPath -Raw

foreach ($topologyName in @("global-shell", "home-page-sections", "catalog-page-regions", "search-result-page-regions", "product-detail-regions", "cart-fallback-style-regions", "checkout-fallback-style-regions", "account-fallback-style-regions", "content-error-system-page-shell")) {
    if (-not $topology.Contains("regionId: $topologyName", [System.StringComparison]::Ordinal)) {
        throw "[SFB-TOPOLOGY-002] Topology '$topologyName' is missing."
    }
}

foreach ($field in @("regionId", "parentRegion", "slotId", "renderOwner", "hydrationMode", "source", "evidenceIds", "responsiveBehavior")) {
    if (-not $topology.Contains($field, [System.StringComparison]::Ordinal)) {
        throw "[SFB-TOPOLOGY-003] Region metadata field '$field' is missing."
    }
}

$requiredSlots = [regex]::Matches($contract, "(?m)^\s{4}- id:\s+([a-z0-9.-]+)\s*$") | ForEach-Object { $_.Groups[1].Value }
foreach ($slot in $requiredSlots) {
    $isMapped = $topology.Contains("slotId: $slot", [System.StringComparison]::Ordinal)
    $isSkipped = $topology.Contains("slotId: $slot", [System.StringComparison]::Ordinal) -and $topology.Contains("reason:", [System.StringComparison]::Ordinal)
    if (-not $isMapped -and -not $isSkipped) {
        throw "[SFB-TOPOLOGY-004] Starter slot '$slot' is neither mapped nor skipped with reason."
    }
}

Write-Host "StorefrontBuilder topology validation passed for $TopologyPath."

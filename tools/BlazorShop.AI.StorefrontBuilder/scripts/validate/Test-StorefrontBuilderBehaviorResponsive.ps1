param(
    [string]$BehaviorsPath,
    [string]$ResponsivePath
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path $BehaviorsPath)) {
    throw "[SFB-BEHAVIOR-000] behaviors.yaml is missing: $BehaviorsPath"
}

if (-not (Test-Path $ResponsivePath)) {
    throw "[SFB-RESPONSIVE-000] responsive.yaml is missing: $ResponsivePath"
}

$behaviors = Get-Content -LiteralPath $BehaviorsPath -Raw
$responsive = Get-Content -LiteralPath $ResponsivePath -Raw

foreach ($class in @("CSS-only", "Hover-driven", "Focus-driven", "Click-driven visual-only", "Scroll-driven visual-only", "Starter-feature-driven", "BFF-action-driven", "Approved JS interop", "Unsupported")) {
    if (-not $behaviors.Contains($class, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "[SFB-BEHAVIOR-001] Behavior class '$class' is missing."
    }
}

foreach ($field in @("breakpoint", "layoutChange", "headerNavBehavior", "productGridColumns", "productDetailMediaActionStacking", "footerStacking", "stickyFixedElements", "drawerMenuBehavior")) {
    if (-not $responsive.Contains($field, [System.StringComparison]::Ordinal)) {
        throw "[SFB-RESPONSIVE-001] Responsive field '$field' is missing."
    }
}

$hasAddToCart = $behaviors.Contains("behaviorId: add-to-cart", [System.StringComparison]::Ordinal)
$hasSafeOwner = $behaviors.Contains("interactionOwner: BFF-action-driven", [System.StringComparison]::Ordinal) -or $behaviors.Contains("interactionOwner: Starter-feature-driven", [System.StringComparison]::Ordinal)
$hasDirectJs = $behaviors.Contains("behaviorId: add-to-cart", [System.StringComparison]::Ordinal) -and ($behaviors.Contains("direct JS", [System.StringComparison]::OrdinalIgnoreCase) -or $behaviors.Contains("direct HTTP", [System.StringComparison]::OrdinalIgnoreCase))

if (-not $hasAddToCart) {
    throw "[SFB-BEHAVIOR-002] Add-to-cart behavior is missing."
}

if (-not $hasSafeOwner -or $hasDirectJs) {
    throw "[SFB-BEHAVIOR-003] Add-to-cart must be Starter-feature-driven or BFF-action-driven, never direct JS or direct HTTP."
}

Write-Host "StorefrontBuilder behavior/responsive validation passed."

param([Parameter(Mandatory = $true)][string]$ProjectRoot)

$ErrorActionPreference = "Stop"

function Assert-ContainsText([string]$RelativePath, [string]$Text, [string]$RuleId) {
    $path = Join-Path $ProjectRoot $RelativePath
    if (-not (Test-Path $path)) {
        throw "[$RuleId] Missing generated composition file: $RelativePath"
    }

    $content = Get-Content -LiteralPath $path -Raw
    if (-not $content.Contains($Text, [System.StringComparison]::Ordinal)) {
        throw "[$RuleId] '$RelativePath' is missing '$Text'."
    }
}

foreach ($check in @(
    @("Components\Layout\MainLayout.razor", "sfb-shell-header", "SFB-COMPOSITION-001"),
    @("Components\Layout\MainLayout.razor", "sfb-mobile-nav", "SFB-COMPOSITION-002"),
    @("Components\Layout\MainLayout.razor", "sfb-cart-badge", "SFB-COMPOSITION-003"),
    @("Pages\Ssr\Home\HomePage.razor", "sfb-hero", "SFB-COMPOSITION-004"),
    @("Pages\Ssr\Home\HomePage.razor", "sfb-featured-grid", "SFB-COMPOSITION-005"),
    @("Pages\Hybrid\Catalog\CategoryPage.razor", "sfb-catalog-toolbar", "SFB-COMPOSITION-006"),
    @("Components\Catalog\ProductSummaryCard.razor", "sfb-product-card", "SFB-COMPOSITION-007"),
    @("Pages\Hybrid\Catalog\ProductPage.razor", "sfb-product-page", "SFB-COMPOSITION-008"),
    @("Components\Catalog\ProductGalleryPlaceholder.razor", "sfb-product-gallery", "SFB-COMPOSITION-009"),
    @("Components\Catalog\PurchasePanelPlaceholder.razor", "data-action=`"cart.add-line`"", "SFB-COMMERCE-001"),
    @("Components\Catalog\PurchasePanelPlaceholder.razor", "sfb-quantity-control", "SFB-COMPOSITION-010"),
    @("Pages\Hybrid\Commerce\CartPage.razor", "sfb-fallback-page", "SFB-COMPOSITION-011"),
    @("Pages\Hybrid\Commerce\CheckoutPage.razor", "sfb-fallback-page", "SFB-COMPOSITION-012"),
    @("Pages\WasmHost\Account\AccountHostPage.razor", "sfb-fallback-page", "SFB-COMPOSITION-013")
)) {
    Assert-ContainsText -RelativePath $check[0] -Text $check[1] -RuleId $check[2]
}

$purchasePanel = Get-Content -LiteralPath (Join-Path $ProjectRoot "Components\Catalog\PurchasePanelPlaceholder.razor") -Raw
if ($purchasePanel.Contains("HttpClient", [System.StringComparison]::Ordinal) -or $purchasePanel.Contains("fetch(", [System.StringComparison]::Ordinal)) {
    throw "[SFB-COMMERCE-002] Product purchase must not call direct HTTP or JS."
}

Write-Host "StorefrontBuilder composition file validation passed for $ProjectRoot."

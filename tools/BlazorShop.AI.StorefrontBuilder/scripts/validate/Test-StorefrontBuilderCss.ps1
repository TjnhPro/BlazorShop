param(
    [Parameter(Mandatory = $true)]
    [string]$ProjectRoot
)

$ErrorActionPreference = "Stop"

$cssPath = Join-Path $ProjectRoot "wwwroot\css\storefront-builder.generated.css"
if (-not (Test-Path $cssPath)) {
    throw "[SFB-CSS-000] Generated CSS is missing under generated storefront wwwroot: $cssPath"
}

$css = Get-Content -LiteralPath $cssPath -Raw
foreach ($required in @("--sfb-color-", "--sfb-font-", "--sfb-text-", "--sfb-space-", "--sfb-container", "--sfb-border-width", "--sfb-radius", "--sfb-shadow", "--sfb-motion", "--sfb-ease", "button", "input", "starter-product-card", "aspect-ratio: 1 / 1", ":focus-visible", "@media")) {
    if (-not $css.Contains($required, [System.StringComparison]::Ordinal)) {
        throw "[SFB-CSS-001] Generated CSS is missing '$required'."
    }
}

if ($css.Contains("<script", [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "[SFB-CSS-002] Generated visual foundation must not inject third-party scripts."
}

$openBraces = ([regex]::Matches($css, "\{")).Count
$closeBraces = ([regex]::Matches($css, "\}")).Count
if ($openBraces -ne $closeBraces) {
    throw "[SFB-CSS-003] Generated CSS has unbalanced braces."
}

Write-Host "StorefrontBuilder CSS validation passed for $cssPath."

param(
    [Parameter(Mandatory = $true)]
    [string]$ProjectRoot
)

$ErrorActionPreference = "Stop"

$workspaceLeaf = Split-Path -Leaf ([System.IO.Path]::GetFullPath($ProjectRoot))
$serverProjectRoot = Join-Path $ProjectRoot $workspaceLeaf
$sourceRoot = if (Test-Path -LiteralPath (Join-Path $serverProjectRoot "$workspaceLeaf.csproj")) { $serverProjectRoot } else { $ProjectRoot }

function Test-TextContains([string]$Text, [string]$Value, [System.StringComparison]$Comparison = [System.StringComparison]::Ordinal) {
    return $Text.IndexOf($Value, $Comparison) -ge 0
}

$cssPath = Join-Path $sourceRoot "wwwroot\css\storefront-builder.generated.css"
if (-not (Test-Path $cssPath)) {
    throw "[SFB-CSS-000] Generated CSS is missing under generated storefront wwwroot: $cssPath"
}

$css = Get-Content -LiteralPath $cssPath -Raw
foreach ($required in @("--sfb-color-", "--sfb-font-", "--sfb-text-", "--sfb-space-", "--sfb-container", "--sfb-border-width", "--sfb-radius", "--sfb-shadow", "--sfb-motion", "--sfb-ease", "button", "input", "starter-product-card", "aspect-ratio: 1 / 1", ":focus-visible", "@media")) {
    if (-not (Test-TextContains $css $required)) {
        throw "[SFB-CSS-001] Generated CSS is missing '$required'."
    }
}

if (Test-TextContains $css "<script" ([System.StringComparison]::OrdinalIgnoreCase)) {
    throw "[SFB-CSS-002] Generated visual foundation must not inject third-party scripts."
}

$openBraces = ([regex]::Matches($css, "\{")).Count
$closeBraces = ([regex]::Matches($css, "\}")).Count
if ($openBraces -ne $closeBraces) {
    throw "[SFB-CSS-003] Generated CSS has unbalanced braces."
}

Write-Host "StorefrontBuilder CSS validation passed for $cssPath."

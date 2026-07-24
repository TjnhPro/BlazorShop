param(
    [string]$ProjectRoot = "BlazorShop.PresentationV2/BlazorShop.Storefront.BuilderDemo",
    [ValidateSet("all", "page", "component", "css", "validate", "conflicts")]
    [string]$Scope = "all",
    [string]$Target = "",
    [switch]$WhatIf
)

$ErrorActionPreference = "Stop"

if ($Scope -eq "validate") {
    & "$PSScriptRoot/validate-storefront.ps1" -ProjectRoot $ProjectRoot
    exit 0
}

if ($Scope -eq "conflicts") {
    & "$PSScriptRoot/scripts/validate/Test-StorefrontBuilderIdempotency.ps1" -ProjectRoot $ProjectRoot
    exit 0
}

if ($WhatIf) {
    Write-Host "Validate without writing: scope=$Scope target=$Target"
    exit 0
}

if ($Scope -in @("all", "css")) {
    node "$PSScriptRoot/scripts/generate/apply-visual-foundation.mjs" --project-root $ProjectRoot
}

if ($Scope -in @("all", "page", "component")) {
    node "$PSScriptRoot/scripts/generate/apply-composition.mjs" --project-root $ProjectRoot --target $Target
}

node "$PSScriptRoot/scripts/generate/update-generated-files-manifest.mjs" --project-root $ProjectRoot
& "$PSScriptRoot/scripts/validate/Test-StorefrontBuilderIdempotency.ps1" -ProjectRoot $ProjectRoot

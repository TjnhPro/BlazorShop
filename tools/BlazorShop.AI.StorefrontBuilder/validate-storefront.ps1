param(
    [string]$ProjectRoot = "BlazorShop.PresentationV2/BlazorShop.Storefront.BuilderDemo",
    [string]$Name = "BlazorShop.Storefront.BuilderDemo",
    [string]$StoreKey = "builder-demo"
)

$ErrorActionPreference = "Stop"

# validate-storefront command entrypoint.
& "$PSScriptRoot/scripts/validate/Test-StorefrontBuilderStaticGate.ps1" -ProjectRoot $ProjectRoot -Name $Name -StoreKey $StoreKey

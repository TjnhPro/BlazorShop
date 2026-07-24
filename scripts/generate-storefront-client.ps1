param(
    [string] $ConfigurationPath = "BlazorShop.PresentationV2/BlazorShop.Storefront.Client/nswag.storefront.client.json"
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$resolvedConfigurationPath = Join-Path $repoRoot $ConfigurationPath

if (-not (Test-Path -LiteralPath $resolvedConfigurationPath)) {
    throw "NSwag configuration was not found: $resolvedConfigurationPath"
}

Push-Location $repoRoot
try {
    dotnet tool restore
    dotnet nswag run $resolvedConfigurationPath
}
finally {
    Pop-Location
}

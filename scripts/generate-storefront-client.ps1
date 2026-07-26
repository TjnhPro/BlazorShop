param(
    [string] $ConfigurationPath = "BlazorShop.PresentationV2/BlazorShop.Storefront.Client/nswag.storefront.client.json"
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$resolvedConfigurationPath = Join-Path $repoRoot $ConfigurationPath

if (-not (Test-Path -LiteralPath $resolvedConfigurationPath)) {
    throw "NSwag configuration was not found: $resolvedConfigurationPath"
}

$configuration = Get-Content -LiteralPath $resolvedConfigurationPath -Raw | ConvertFrom-Json
$contractPath = $configuration.documentGenerator.fromDocument.url
if ([string]::IsNullOrWhiteSpace($contractPath)) {
    throw "NSwag configuration does not define documentGenerator.fromDocument.url: $resolvedConfigurationPath"
}

$resolvedContractPath = [System.IO.Path]::GetFullPath((Join-Path (Split-Path -Parent $resolvedConfigurationPath) $contractPath))
if (-not (Test-Path -LiteralPath $resolvedContractPath)) {
    throw "Canonical Storefront OpenAPI contract was not found: $resolvedContractPath. Refresh or restore contracts/storefront/storefront.openapi.json before regenerating BlazorShop.Storefront.Client."
}

Push-Location $repoRoot
try {
    dotnet tool restore
    dotnet nswag run $resolvedConfigurationPath
}
finally {
    Pop-Location
}

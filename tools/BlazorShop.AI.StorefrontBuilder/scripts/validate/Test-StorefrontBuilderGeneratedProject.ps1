param(
    [Parameter(Mandatory = $true)]
    [string]$ProjectRoot,
    [Parameter(Mandatory = $true)]
    [string]$Name,
    [Parameter(Mandatory = $true)]
    [string]$StoreKey
)

$ErrorActionPreference = "Stop"

$projectFile = Join-Path $ProjectRoot "$Name.csproj"
$metadata = Join-Path $ProjectRoot "docs\storefront-analysis\metadata.yaml"
$featureManifest = Join-Path $ProjectRoot "Features\feature-manifest.json"

foreach ($path in @($projectFile, $metadata, $featureManifest)) {
    if (-not (Test-Path $path)) {
        throw "[SFB-PROJECT-003] Generated project required file is missing: $path"
    }
}

$project = Get-Content -LiteralPath $projectFile -Raw
foreach ($package in @("BlazorShop.Storefront.Client", "BlazorShop.Storefront.Runtime")) {
    if (-not $project.Contains("PackageReference Include=`"$package`"", [System.StringComparison]::Ordinal)) {
        throw "[SFB-PROJECT-004] Generated project is missing package reference '$package'."
    }
}

$metadataText = Get-Content -LiteralPath $metadata -Raw
foreach ($required in @("projectName: $Name", "storeKey: $StoreKey", "sourceStarterPath:", "protectedFiles:", "BlazorShop.Storefront.Client", "BlazorShop.Storefront.Runtime")) {
    if (-not $metadataText.Contains($required, [System.StringComparison]::Ordinal)) {
        throw "[SFB-PROJECT-005] metadata.yaml is missing '$required'."
    }
}

Write-Host "StorefrontBuilder generated project validation passed for $ProjectRoot."

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
foreach ($package in @("BlazorShop.Storefront.Runtime", "BlazorShop.Storefront.Presentation", "BlazorShop.Storefront.Components")) {
    if (-not $project.Contains("PackageReference Include=`"$package`"", [System.StringComparison]::Ordinal)) {
        throw "[SFB-PROJECT-004] Generated project is missing package reference '$package'."
    }
}

$packageVersions = Get-Content -LiteralPath (Join-Path $ProjectRoot "StorefrontPackageVersions.props") -Raw
if (-not $packageVersions.Contains("StorefrontClientPackageVersion", [System.StringComparison]::Ordinal)) {
    throw "[SFB-PROJECT-004] Generated project is missing Client package compatibility metadata."
}

$metadataText = Get-Content -LiteralPath $metadata -Raw
foreach ($required in @("projectName: $Name", "storeKey: $StoreKey", "sourceStarterPath:", "protectedFiles:", "BlazorShop.Storefront.Client", "BlazorShop.Storefront.Runtime", "BlazorShop.Storefront.Components")) {
    if (-not $metadataText.Contains($required, [System.StringComparison]::Ordinal)) {
        throw "[SFB-PROJECT-005] metadata.yaml is missing '$required'."
    }
}

$forbidden = @("ProjectReference", "BlazorShop.Storefront.V2", "BlazorShop.Web.SharedV2", "Web.SharedV2", "BlazorShop.Application", "BlazorShop.Domain", "BlazorShop.Infrastructure", "BlazorShop.CommerceNode.API", "BlazorShop.ControlPlane.API", "BlazorShop.ControlPlane.Web")
Get-ChildItem -LiteralPath $ProjectRoot -Recurse -File |
    Where-Object { $_.FullName -notmatch "\\(bin|obj)\\" } |
    ForEach-Object {
        $content = Get-Content -LiteralPath $_.FullName -Raw
        foreach ($pattern in $forbidden) {
            if ($content.Contains($pattern, [System.StringComparison]::OrdinalIgnoreCase)) {
                throw "[SFB-PROJECT-006] Forbidden dependency '$pattern' found in $($_.FullName)."
            }
        }
    }

Write-Host "StorefrontBuilder generated project validation passed for $ProjectRoot."

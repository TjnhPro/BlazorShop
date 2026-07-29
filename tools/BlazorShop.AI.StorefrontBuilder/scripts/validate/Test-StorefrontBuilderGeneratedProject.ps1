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
foreach ($package in @("BlazorShop.Storefront.Presentation", "BlazorShop.Storefront.Components")) {
    if (-not $project.Contains("PackageReference Include=`"$package`"", [System.StringComparison]::Ordinal)) {
        throw "[SFB-PROJECT-004] Generated project is missing package reference '$package'."
    }
}

foreach ($package in @("BlazorShop.Storefront.Runtime", "BlazorShop.Storefront.Client")) {
    if ($project.Contains("PackageReference Include=`"$package`"", [System.StringComparison]::Ordinal)) {
        throw "[SFB-PROJECT-004] Generated project must not direct-reference '$package'. Presentation/Runtime own application transport."
    }
}

$packageVersions = Get-Content -LiteralPath (Join-Path $ProjectRoot "StorefrontPackageVersions.props") -Raw
if (-not $packageVersions.Contains("StorefrontClientPackageVersion", [System.StringComparison]::Ordinal)) {
    throw "[SFB-PROJECT-004] Generated project is missing Client package compatibility metadata."
}

$metadataText = Get-Content -LiteralPath $metadata -Raw
$canonicalContractPath = "contracts/storefront/storefront.openapi.json"
foreach ($required in @("projectName: $Name", "storeKey: $StoreKey", "storefrontContractPath: $canonicalContractPath", "storefrontContractSha256:", "sourceStarterPath:", "starterContractPath: BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/starter-generation.contract.yaml", "starterContractVersion:", "protectedFiles:", "packageVersions:", "BlazorShop.Storefront.Presentation", "BlazorShop.Storefront.Components")) {
    if (-not $metadataText.Contains($required, [System.StringComparison]::Ordinal)) {
        throw "[SFB-PROJECT-005] metadata.yaml is missing '$required'."
    }
}

$contractHashMatch = [regex]::Match($metadataText, "(?m)^storefrontContractSha256:\s*([a-f0-9]{64})\s*$")
if (-not $contractHashMatch.Success) {
    throw "[SFB-PROJECT-007] metadata.yaml must contain lowercase SHA-256 storefrontContractSha256 for the canonical Storefront OpenAPI contract."
}

$starterVersionMatch = [regex]::Match($metadataText, "(?m)^starterContractVersion:\s*\S+\s*$")
if (-not $starterVersionMatch.Success) {
    throw "[SFB-PROJECT-008] metadata.yaml must contain starterContractVersion from the Starter generation contract."
}

foreach ($packageVersionMarker in @("BlazorShop.Storefront.Client:", "BlazorShop.Storefront.Runtime:", "BlazorShop.Storefront.Presentation:", "BlazorShop.Storefront.Components:")) {
    if (-not $metadataText.Contains($packageVersionMarker, [System.StringComparison]::Ordinal)) {
        throw "[SFB-PROJECT-009] metadata.yaml is missing package version marker '$packageVersionMarker'."
    }
}

$forbiddenDirectories = @("Security", "Services", "Middleware")
foreach ($directory in $forbiddenDirectories) {
    if (Test-Path (Join-Path $ProjectRoot $directory)) {
        throw "[SFB-PROJECT-006] Generated project must not contain application/security folder '$directory'."
    }
}

$forbidden = @("ProjectReference", "BlazorShop.Storefront.V2", "BlazorShop.Web.SharedV2", "Web.SharedV2", "BlazorShop.Application", "BlazorShop.Domain", "BlazorShop.Infrastructure", "BlazorShop.CommerceNode.API", "BlazorShop.ControlPlane.API", "BlazorShop.ControlPlane.Web", "PackageReference Include=`"BlazorShop.Storefront.Runtime`"", "PackageReference Include=`"BlazorShop.Storefront.Client`"")
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

param(
    [Parameter(Mandatory = $true)]
    [string]$Name,
    [Parameter(Mandatory = $true)]
    [string]$StoreKey,
    [string]$OutputRoot = "artifacts/storefront-builder/generated",
    [string]$CommerceNodeBaseUrl = "http://localhost:5180",
    [string]$PublicBaseUrl = "http://localhost:18600",
    [switch]$Force
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..\..\..")
function Resolve-RepoPath {
    param([string]$Path)

    if ([System.IO.Path]::IsPathRooted($Path)) {
        return [System.IO.Path]::GetFullPath($Path)
    }

    return [System.IO.Path]::GetFullPath((Join-Path $repoRoot $Path))
}

$outputRootPath = Resolve-RepoPath $OutputRoot
$projectRoot = Join-Path $outputRootPath $Name
$generator = Join-Path $repoRoot "scripts\generate-storefront-sample.ps1"
$featureManifest = Join-Path $projectRoot "Features\feature-manifest.json"
$storefrontContractPath = "contracts/storefront/storefront.openapi.json"
$storefrontContractFullPath = Resolve-RepoPath $storefrontContractPath
$starterContractPath = "BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/starter-generation.contract.yaml"
$starterContractFullPath = Resolve-RepoPath $starterContractPath

if ($Name -notmatch "^BlazorShop\.Storefront\.[A-Z][A-Za-z0-9]*$") {
    throw "[SFB-PROJECT-001] Name must match BlazorShop.Storefront.{Name} with a safe PascalCase suffix."
}

if (-not (Test-Path -LiteralPath $storefrontContractFullPath)) {
    throw "[SFB-PROJECT-007] Canonical Storefront OpenAPI contract is missing: $storefrontContractPath"
}

if (-not (Test-Path -LiteralPath $starterContractFullPath)) {
    throw "[SFB-PROJECT-008] Starter generation contract is missing: $starterContractPath"
}

$resolvedOutput = [System.IO.Path]::GetFullPath($projectRoot)
$resolvedOutputRoot = [System.IO.Path]::GetFullPath($outputRootPath)
if (-not $resolvedOutput.StartsWith($resolvedOutputRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "[SFB-PROJECT-002] Refusing to generate outside configured StorefrontBuilder output root: $resolvedOutput"
}

$arguments = @{
    Name = $Name
    StoreKey = $StoreKey
    OutputRoot = $OutputRoot
    CommerceNodeBaseUrl = $CommerceNodeBaseUrl
    PublicBaseUrl = $PublicBaseUrl
}

if ($Force) {
    # Copy Starter template through the deterministic generator, then layer StorefrontBuilder metadata.
    & $generator @arguments -Force
} else {
    # Copy Starter template through the deterministic generator, then layer StorefrontBuilder metadata.
    & $generator @arguments
}

$analysisRoot = Join-Path $projectRoot "docs\storefront-analysis"
New-Item -ItemType Directory -Force -Path $analysisRoot | Out-Null
$storefrontContractSha256 = (Get-FileHash -LiteralPath $storefrontContractFullPath -Algorithm SHA256).Hash.ToLowerInvariant()
$starterContract = Get-Content -LiteralPath $starterContractFullPath -Raw
$starterContractVersionMatch = [regex]::Match($starterContract, "(?m)^contractVersion:\s*(\S+)\s*$")
if (-not $starterContractVersionMatch.Success) {
    throw "[SFB-PROJECT-008] Starter generation contract must declare contractVersion."
}

$versionPropsPath = Join-Path $projectRoot "StorefrontPackageVersions.props"
if (-not (Test-Path -LiteralPath $versionPropsPath)) {
    throw "[SFB-PROJECT-009] Generated project is missing StorefrontPackageVersions.props."
}

[xml]$packageVersionDocument = Get-Content -LiteralPath $versionPropsPath -Raw
$packageVersions = $packageVersionDocument.Project.PropertyGroup

$metadata = @(
    "schemaVersion: 1.0.0",
    "artifactKind: generated-storefront-metadata",
    "projectName: $Name",
    "storeKey: $StoreKey",
    "storefrontContractPath: $storefrontContractPath",
    "storefrontContractSha256: $storefrontContractSha256",
    "sourceStarterPath: BlazorShop.PresentationV2/BlazorShop.Storefront.Starter",
    "starterContractPath: $starterContractPath",
    "starterContractVersion: $($starterContractVersionMatch.Groups[1].Value)",
    "generationMode: starter-copy-before-visual-generation",
    "protectedFiles:",
    "  - BlazorShop.Storefront.Presentation",
    "  - StorefrontPackageVersions.props",
    "featureManifest: Features\feature-manifest.json",
    "packageReferences:",
    "  - BlazorShop.Storefront.Presentation",
    "  - BlazorShop.Storefront.Components",
    "packageVersions:",
    "  BlazorShop.Storefront.Client: $($packageVersions.StorefrontClientPackageVersion)",
    "  BlazorShop.Storefront.Runtime: $($packageVersions.StorefrontRuntimePackageVersion)",
    "  BlazorShop.Storefront.Presentation: $($packageVersions.StorefrontPresentationPackageVersion)",
    "  BlazorShop.Storefront.Components: $($packageVersions.StorefrontComponentsPackageVersion)"
) -join [Environment]::NewLine

Set-Content -LiteralPath (Join-Path $analysisRoot "metadata.yaml") -Value $metadata -Encoding UTF8

Write-Host "StorefrontBuilder generated $Name for store '$StoreKey' at $projectRoot."

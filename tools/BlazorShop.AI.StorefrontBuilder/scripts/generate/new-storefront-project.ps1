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

if ($Name -notmatch "^BlazorShop\.Storefront\.[A-Z][A-Za-z0-9]*$") {
    throw "[SFB-PROJECT-001] Name must match BlazorShop.Storefront.{Name} with a safe PascalCase suffix."
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

$metadata = @(
    "schemaVersion: 1.0.0",
    "artifactKind: generated-storefront-metadata",
    "projectName: $Name",
    "storeKey: $StoreKey",
    "sourceStarterPath: BlazorShop.PresentationV2/BlazorShop.Storefront.Starter",
    "starterContractPath: BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/starter-generation.contract.yaml",
    "generationMode: starter-copy-before-visual-generation",
    "protectedFiles:",
    "  - Endpoints/StarterBffEndpoints.cs",
    "  - Security/StarterReturnUrlValidator.cs",
    "  - StorefrontPackageVersions.props",
    "featureManifest: Features\feature-manifest.json",
    "packageReferences:",
    "  - BlazorShop.Storefront.Client",
    "  - BlazorShop.Storefront.Runtime"
) -join [Environment]::NewLine

Set-Content -LiteralPath (Join-Path $analysisRoot "metadata.yaml") -Value $metadata -Encoding UTF8

Write-Host "StorefrontBuilder generated $Name for store '$StoreKey' at $projectRoot."

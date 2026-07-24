param(
    [string[]]$ReferenceUrls,
    [string]$Name,
    [string]$StoreKey,
    [string]$StarterSourcePath = "BlazorShop.PresentationV2\BlazorShop.Storefront.Starter",
    [string]$StarterContractPath = "BlazorShop.PresentationV2\BlazorShop.Storefront.Starter\starter-generation.contract.yaml",
    [string]$StorefrontClientPackageVersion = "1.0.0-local",
    [string]$StorefrontRuntimePackageVersion = "1.0.0-local",
    [string]$CapabilitySnapshotSource = "backend-public-configuration",
    [string]$FeatureManifestPath = "BlazorShop.PresentationV2\BlazorShop.Storefront.Starter\Features\feature-manifest.json",
    [string]$OutputRoot = "BlazorShop.PresentationV2",
    [ValidateSet("analyze-only", "plan-only", "generate", "update", "validate-only", "full")]
    [string]$Mode = "validate-only",
    [switch]$Force
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..\..\..")

function Fail-Preflight {
    param(
        [string]$RuleId,
        [string]$Problem,
        [string]$Cause,
        [string]$Fix
    )

    throw "[$RuleId] $Problem Cause: $Cause Fix: $Fix"
}

function Resolve-RepoPath {
    param([string]$Path)
    return [System.IO.Path]::GetFullPath((Join-Path $repoRoot $Path))
}

function Assert-InsidePath {
    param(
        [string]$ResolvedPath,
        [string]$AllowedRoot,
        [string]$RuleId
    )

    $normalizedRoot = [System.IO.Path]::GetFullPath($AllowedRoot)
    if (-not $ResolvedPath.StartsWith($normalizedRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
        Fail-Preflight $RuleId "Output path is outside the approved workspace." $ResolvedPath "Choose an output root under '$normalizedRoot' or explicitly configure an external target root."
    }
}

if ($ReferenceUrls.Count -eq 0) {
    Fail-Preflight "SFB-PRE-001" "Reference URL list is required." "No reference URL was provided." "Pass at least one http/https URL."
}

foreach ($url in $ReferenceUrls) {
    $parsedUri = $null
    if (-not [Uri]::TryCreate($url, [UriKind]::Absolute, [ref]$parsedUri)) {
        Fail-Preflight "SFB-PRE-002" "Reference URL is invalid." $url "Use an absolute http/https URL."
    }

    $uri = [Uri]$parsedUri
    if ($uri.Scheme -notin @("http", "https")) {
        Fail-Preflight "SFB-PRE-003" "Reference URL must use http or https." $url "Use an http:// or https:// reference URL."
    }
}

if ([string]::IsNullOrWhiteSpace($Name) -or $Name -notmatch '^[A-Z][A-Za-z0-9]*$') {
    Fail-Preflight "SFB-PRE-004" "Storefront project name is unsafe." $Name "Use a safe identifier such as 'Demo' that can become BlazorShop.Storefront.Demo."
}

if ([string]::IsNullOrWhiteSpace($StoreKey)) {
    Fail-Preflight "SFB-PRE-005" "Store key is required." "Store key was empty." "Pass the Commerce Node Storefront store key."
}

$resolvedOutputRoot = Resolve-RepoPath $OutputRoot
$approvedRoot = Resolve-RepoPath "BlazorShop.PresentationV2"
Assert-InsidePath $resolvedOutputRoot $approvedRoot "SFB-PRE-006"

$targetProjectRoot = Join-Path $resolvedOutputRoot "BlazorShop.Storefront.$Name"
$starterRoot = Resolve-RepoPath $StarterSourcePath
if ($targetProjectRoot.Equals($starterRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
    Fail-Preflight "SFB-PRE-007" "Generated output cannot target Starter." $targetProjectRoot "Use BlazorShop.Storefront.$Name under the approved output root."
}

if ((Test-Path $targetProjectRoot) -and $Mode -ne "update" -and -not $Force) {
    Fail-Preflight "SFB-PRE-008" "Output project already exists." $targetProjectRoot "Use -Mode update or -Force after reviewing the existing project."
}

$resolvedStarterContract = Resolve-RepoPath $StarterContractPath
if (-not (Test-Path $resolvedStarterContract)) {
    Fail-Preflight "SFB-PRE-009" "Starter generation contract is missing." $resolvedStarterContract "Create or pass the Starter contract path."
}

$contract = Get-Content -LiteralPath $resolvedStarterContract -Raw
foreach ($required in @("contractVersion:", "protectedZones:", "slots:", "routes:")) {
    if (-not $contract.Contains($required, [System.StringComparison]::Ordinal)) {
        Fail-Preflight "SFB-PRE-010" "Starter generation contract failed validation." "Missing '$required'." "Regenerate or repair starter-generation.contract.yaml."
    }
}

if ([string]::IsNullOrWhiteSpace($StorefrontClientPackageVersion) -or [string]::IsNullOrWhiteSpace($StorefrontRuntimePackageVersion)) {
    Fail-Preflight "SFB-PRE-011" "Package versions must be resolvable." "Client or Runtime package version was empty." "Pass package versions or restore StorefrontPackageVersions.props."
}

foreach ($gate in @("scripts\qa\run-storefront-starter-isolation-gate.ps1", "scripts\qa\run-storefront-sample-release-gate.ps1")) {
    if (-not (Test-Path (Resolve-RepoPath $gate))) {
        Fail-Preflight "SFB-PRE-012" "Required gate script is missing." $gate "Restore the Storefront Starter/Sample gate scripts."
    }
}

if (-not (Test-Path (Resolve-RepoPath $FeatureManifestPath))) {
    Fail-Preflight "SFB-PRE-013" "Feature manifest path is missing." $FeatureManifestPath "Pass a valid Starter feature manifest path."
}

$hasGeneratedClientProtection = $contract.IndexOf("BlazorShop.Storefront.Client/Generated", [System.StringComparison]::OrdinalIgnoreCase) -ge 0
$hasRuntimeProtection = $contract.IndexOf("BlazorShop.Storefront.Runtime", [System.StringComparison]::OrdinalIgnoreCase) -ge 0
$hasBffProtection = $contract.IndexOf("Endpoints/StarterBffEndpoints.cs", [System.StringComparison]::OrdinalIgnoreCase) -ge 0
if (-not $hasGeneratedClientProtection -or -not $hasRuntimeProtection -or -not $hasBffProtection) {
    Fail-Preflight "SFB-PRE-014" "Protected path list is incomplete." "Generated client, Runtime, or BFF protected path is missing." "Update the Starter generation contract protectedZones."
}

Write-Host "StorefrontBuilder preflight passed for BlazorShop.Storefront.$Name in mode '$Mode'."

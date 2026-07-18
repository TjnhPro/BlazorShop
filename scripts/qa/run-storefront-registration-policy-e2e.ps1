param(
    [string] $StorefrontBaseUrl = $env:STOREFRONT_QA_BASE_URL,
    [string] $ControlPlaneApiUrl = $env:CONTROLPLANE_QA_API_URL,
    [string] $ControlPlaneWebUrl = $env:CONTROLPLANE_QA_WEB_URL,
    [string] $CommerceNodeApiUrl = $env:COMMERCENODE_QA_API_URL,
    [switch] $Headless
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$playwrightModule = Join-Path $repoRoot ".gstack\playwright-qa\node_modules\playwright"
if (-not (Test-Path $playwrightModule)) {
    throw "Playwright package was not found at $playwrightModule. Run the Storefront Playwright QA setup first."
}

if ([string]::IsNullOrWhiteSpace($StorefrontBaseUrl)) {
    $StorefrontBaseUrl = "http://localhost:18598"
}

if ([string]::IsNullOrWhiteSpace($ControlPlaneApiUrl)) {
    $ControlPlaneApiUrl = "http://localhost:5280"
}

if ([string]::IsNullOrWhiteSpace($ControlPlaneWebUrl)) {
    $ControlPlaneWebUrl = "http://localhost:5281"
}

if ([string]::IsNullOrWhiteSpace($CommerceNodeApiUrl)) {
    $CommerceNodeApiUrl = "http://localhost:5180"
}

$env:STOREFRONT_QA_BASE_URL = $StorefrontBaseUrl
$env:CONTROLPLANE_QA_API_URL = $ControlPlaneApiUrl
$env:CONTROLPLANE_QA_WEB_URL = $ControlPlaneWebUrl
$env:COMMERCENODE_QA_API_URL = $CommerceNodeApiUrl
$env:HEADLESS = if ($Headless) { "true" } else { "false" }

node (Join-Path $PSScriptRoot "storefront-registration-policy-e2e.js")
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

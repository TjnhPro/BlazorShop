param(
    [string] $StorefrontBaseUrl = $env:STOREFRONT_QA_BASE_URL,
    [string] $CommerceNodeApiUrl = $env:COMMERCENODE_QA_API_URL,
    [string] $MailpitApiUrl = $env:MAILPIT_API_URL,
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

if ([string]::IsNullOrWhiteSpace($CommerceNodeApiUrl)) {
    $CommerceNodeApiUrl = "http://localhost:5180"
}

if ([string]::IsNullOrWhiteSpace($MailpitApiUrl)) {
    $MailpitApiUrl = "http://localhost:8025/api/v1"
}

$env:STOREFRONT_QA_BASE_URL = $StorefrontBaseUrl
$env:COMMERCENODE_QA_API_URL = $CommerceNodeApiUrl
$env:MAILPIT_API_URL = $MailpitApiUrl
$env:HEADLESS = if ($Headless) { "true" } else { "false" }

node (Join-Path $PSScriptRoot "storefront-order-email-e2e.js")
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

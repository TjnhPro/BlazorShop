param(
    [string] $StorefrontBaseUrl = $env:STOREFRONT_QA_BASE_URL,
    [string] $CommerceNodeApiUrl = $env:COMMERCENODE_QA_API_URL,
    [string] $MailpitApiUrl = $env:MAILPIT_API_URL,
    [ValidateRange(1, 7200)]
    [int] $TimeoutSeconds = 900,
    [string] $RunnerScript,
    [switch] $Headless
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$playwrightModule = Join-Path $repoRoot ".gstack\playwright-qa\node_modules\playwright"
if (-not (Test-Path $playwrightModule)) {
    throw "Playwright package was not found at $playwrightModule. Run the Storefront Playwright QA setup first."
}

function Stop-NodeProcessTree {
    param([int] $ProcessId)

    if ($IsWindows -or [System.Runtime.InteropServices.RuntimeInformation]::IsOSPlatform([System.Runtime.InteropServices.OSPlatform]::Windows)) {
        & taskkill.exe /PID $ProcessId /T /F | Out-Null
        return
    }

    Stop-Process -Id $ProcessId -Force -ErrorAction SilentlyContinue
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
$env:STOREFRONT_QA_RUN_TIMEOUT_MS = ([int64] $TimeoutSeconds * 1000).ToString([System.Globalization.CultureInfo]::InvariantCulture)

if ([string]::IsNullOrWhiteSpace($RunnerScript)) {
    $RunnerScript = Join-Path $PSScriptRoot "storefront-order-email-e2e.js"
}

$resolvedRunnerScript = Resolve-Path $RunnerScript
$startInfo = [System.Diagnostics.ProcessStartInfo]::new()
$startInfo.FileName = "node"
$startInfo.ArgumentList.Add($resolvedRunnerScript.Path)
$startInfo.UseShellExecute = $false

$process = [System.Diagnostics.Process]::Start($startInfo)
if ($null -eq $process) {
    throw "Could not start Storefront order email E2E runner."
}

try {
    if (-not $process.WaitForExit($TimeoutSeconds * 1000)) {
        Stop-NodeProcessTree -ProcessId $process.Id
        throw "Storefront order email E2E runner timed out after $TimeoutSeconds seconds."
    }

    exit $process.ExitCode
}
finally {
    $process.Dispose()
}

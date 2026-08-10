param(
    [string] $StorefrontBaseUrl = "http://127.0.0.1:18641",
    [string] $Configuration = "Debug",
    [int] $RuntimeTimeoutSeconds = 120,
    [bool] $RequireCurrentStore = $false,
    [switch] $NoBuild,
    [switch] $UseExisting,
    [switch] $Describe
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "../..")
$storefrontProject = Join-Path $repoRoot "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/BlazorShop.Storefront.V2.csproj"
$storefrontProjectRoot = Split-Path -Parent $storefrontProject
$nodeScript = Join-Path $repoRoot "scripts/qa/storefront-reference-components-v2-proof.js"

if ($Describe) {
    Write-Host "Storefront reference components V2 proof"
    Write-Host "- Starts BlazorShop.Storefront.V2 on $StorefrontBaseUrl"
    Write-Host "- Use -UseExisting to run against an already started V2 runtime"
    Write-Host "- StoreResolution__RequireCurrentStore defaults to $RequireCurrentStore for deterministic component proof"
    Write-Host "- Verifies browser-visible brand logo, contact form, and discounted rail flows"
    Write-Host "- Verifies contact validation, backend failure/retry, success state, and antiforgery header"
    Write-Host "- Verifies no direct Commerce browser calls, console errors, or page errors"
    Write-Host "- Evidence: output/playwright/storefront-reference-components-phase14"
    exit 0
}

function Invoke-ReferenceComponentStep {
    param(
        [string] $Name,
        [scriptblock] $Action
    )

    Write-Host "== $Name =="
    & $Action
}

function Wait-ForStorefront {
    param([System.Diagnostics.Process] $Process)

    $deadline = [DateTimeOffset]::UtcNow.AddSeconds($RuntimeTimeoutSeconds)
    while ([DateTimeOffset]::UtcNow -lt $deadline) {
        if ($Process.HasExited) {
            throw "Storefront V2 exited before reference component proof with exit code $($Process.ExitCode)."
        }

        try {
            Invoke-WebRequest -Uri "$($StorefrontBaseUrl.TrimEnd('/'))/health" -UseBasicParsing -TimeoutSec 5 | Out-Null
            return
        }
        catch {
            Start-Sleep -Milliseconds 500
        }
    }

    throw "Storefront V2 did not become ready at $StorefrontBaseUrl within $RuntimeTimeoutSeconds seconds."
}

function Start-Storefront {
    $storefrontDll = Join-Path $storefrontProjectRoot "bin/$Configuration/net10.0/BlazorShop.Storefront.V2.dll"
    if (-not (Test-Path $storefrontDll)) {
        throw "Storefront V2 build output is missing: $storefrontDll"
    }

    $startInfo = [System.Diagnostics.ProcessStartInfo]::new()
    $startInfo.FileName = "dotnet"
    $startInfo.WorkingDirectory = $storefrontProjectRoot
    $startInfo.RedirectStandardOutput = $false
    $startInfo.RedirectStandardError = $false
    $startInfo.UseShellExecute = $false
    $startInfo.CreateNoWindow = $true
    $startInfo.Environment["ASPNETCORE_ENVIRONMENT"] = "Development"
    $startInfo.Environment["DOTNET_ENVIRONMENT"] = "Development"
    $startInfo.Environment["Api__BaseUrl"] = "http://localhost:5180/api/"
    $startInfo.Environment["Api__StoreKey"] = "default"
    $startInfo.Environment["ClientApp__BaseUrl"] = $StorefrontBaseUrl
    $startInfo.Environment["PublicUrl__BaseUrl"] = $StorefrontBaseUrl
    $startInfo.Environment["StoreResolution__RequireCurrentStore"] = $RequireCurrentStore.ToString().ToLowerInvariant()

    $arguments = @(
        $storefrontDll,
        "--urls",
        $StorefrontBaseUrl
    )

    $startInfo.Arguments = ($arguments | ForEach-Object { ConvertTo-ProcessArgument $_ }) -join " "

    return [System.Diagnostics.Process]::Start($startInfo)
}

function ConvertTo-ProcessArgument {
    param([string] $Value)

    if ($Value.IndexOfAny([char[]] " `t`r`n`"") -lt 0) {
        return $Value
    }

    return '"' + $Value.Replace('"', '\"') + '"'
}

if (-not (Test-Path $nodeScript)) {
    throw "Reference component Playwright script not found: $nodeScript"
}

if (-not (Test-Path (Join-Path $repoRoot ".gstack/playwright-qa/node_modules/playwright"))) {
    throw "Playwright dependency is missing. Expected .gstack/playwright-qa/node_modules/playwright."
}

if ($UseExisting) {
    Invoke-ReferenceComponentStep "Wait for existing Storefront V2" {
        $deadline = [DateTimeOffset]::UtcNow.AddSeconds($RuntimeTimeoutSeconds)
        while ([DateTimeOffset]::UtcNow -lt $deadline) {
            try {
                Invoke-WebRequest -Uri "$($StorefrontBaseUrl.TrimEnd('/'))/health" -UseBasicParsing -TimeoutSec 5 | Out-Null
                break
            }
            catch {
                Start-Sleep -Milliseconds 500
            }
        }

        if ([DateTimeOffset]::UtcNow -ge $deadline) {
            throw "Existing Storefront V2 did not become ready at $StorefrontBaseUrl within $RuntimeTimeoutSeconds seconds."
        }
    }

    Invoke-ReferenceComponentStep "Run reference component browser proof" {
        $env:STOREFRONT_BASE_URL = $StorefrontBaseUrl
        node $nodeScript
        if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    }

    exit 0
}

if (-not $NoBuild) {
    Invoke-ReferenceComponentStep "Build Storefront V2" {
        dotnet build $storefrontProject --configuration $Configuration --no-restore
        if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    }
}

Invoke-ReferenceComponentStep "Start Storefront V2" {
    $storefrontProcess = Start-Storefront
    try {
        Wait-ForStorefront $storefrontProcess
        Invoke-ReferenceComponentStep "Run reference component browser proof" {
            $env:STOREFRONT_BASE_URL = $StorefrontBaseUrl
            node $nodeScript
            if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
        }
    }
    finally {
        if ($storefrontProcess -and -not $storefrontProcess.HasExited) {
            try {
                $storefrontProcess.Kill($true)
            }
            catch {
                $storefrontProcess.Kill()
            }

            $storefrontProcess.WaitForExit(5000) | Out-Null
        }

        if ($storefrontProcess) {
            $storefrontProcess.Dispose()
        }
    }
}

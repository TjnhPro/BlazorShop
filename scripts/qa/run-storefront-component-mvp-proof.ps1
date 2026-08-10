param(
    [ValidateSet("RawHtml", "Hybrid", "Rail")]
    [string] $Phase = "RawHtml",
    [string] $StorefrontBaseUrl = "http://127.0.0.1:18640",
    [string] $Configuration = "Debug",
    [int] $RuntimeTimeoutSeconds = 60,
    [switch] $NoBuild,
    [switch] $Describe
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "../..")
$storefrontProject = Join-Path $repoRoot "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/BlazorShop.Storefront.V2.csproj"
$storefrontProjectRoot = Split-Path -Parent $storefrontProject
$nodeScript = Join-Path $repoRoot "scripts/qa/storefront-component-mvp-proof.js"

if ($Describe) {
    Write-Host "Storefront Component MVP proof"
    Write-Host "- Starts BlazorShop.Storefront.V2 on $StorefrontBaseUrl"
    Write-Host "- Phase RawHtml: request /__qa/component-mvp and assert SSR/prerender/noindex markers before WASM startup"
    Write-Host "- Phase Hybrid: hydrate /__qa/component-mvp in Chromium, assert WebAssembly interactive marker and C# click state"
    Write-Host "- Phase Rail: mock same-origin BFF and assert WasmHost rail loading/success/empty/error/retry states"
    Write-Host "- Evidence: output/playwright/storefront-component-mvp"
    exit 0
}

function Invoke-ComponentMvpStep {
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
            throw "Storefront V2 exited before Component MVP proof with exit code $($Process.ExitCode)."
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
    $startInfo.Environment["StoreResolution__RequireCurrentStore"] = "true"

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

function ConvertTo-NodePhase {
    param([string] $Value)

    switch ($Value) {
        "RawHtml" { return "raw-html" }
        "Hybrid" { return "hybrid" }
        "Rail" { return "rail" }
        default { return $Value.ToLowerInvariant() }
    }
}

if (-not (Test-Path $nodeScript)) {
    throw "Component MVP Playwright script not found: $nodeScript"
}

if (-not (Test-Path (Join-Path $repoRoot ".gstack/playwright-qa/node_modules/playwright"))) {
    throw "Playwright dependency is missing. Expected .gstack/playwright-qa/node_modules/playwright."
}

if (-not $NoBuild) {
    Invoke-ComponentMvpStep "Build Storefront V2" {
        dotnet build $storefrontProject --configuration $Configuration --no-restore
        if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    }
}

Invoke-ComponentMvpStep "Start Storefront V2" {
    $storefrontProcess = Start-Storefront
    try {
        Wait-ForStorefront $storefrontProcess
        Invoke-ComponentMvpStep "Run Component MVP $Phase proof" {
            $env:STOREFRONT_BASE_URL = $StorefrontBaseUrl
            $env:STOREFRONT_COMPONENT_MVP_PHASE = ConvertTo-NodePhase $Phase
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

param(
    [switch]$SkipStorefrontBuilderSmoke
)

$ErrorActionPreference = "Stop"

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
$toolProject = Join-Path $repoRoot "tools\BlazorShop.AI.StorefrontReverseEngineering\BlazorShop.AI.StorefrontReverseEngineering.csproj"
$testProject = Join-Path $repoRoot "tools\BlazorShop.AI.StorefrontReverseEngineering\tests\BlazorShop.AI.StorefrontReverseEngineering.Tests\BlazorShop.AI.StorefrontReverseEngineering.Tests.csproj"
$reportRoot = Join-Path $repoRoot "obj\storefront-reverse-engineering\reports"
$projectOutputRoot = Join-Path $repoRoot "obj\storefront-reverse-engineering\projects\phase3a-gate"
$fixturePath = Join-Path $repoRoot "tools\BlazorShop.AI.StorefrontReverseEngineering\tests\BlazorShop.AI.StorefrontReverseEngineering.Tests\Fixtures\static-storefront.html"
$steps = New-Object System.Collections.Generic.List[string]

function Invoke-GateStep {
    param(
        [Parameter(Mandatory = $true)][string]$Name,
        [Parameter(Mandatory = $true)][scriptblock]$Script
    )

    Write-Host "== $Name =="
    & $Script
    $steps.Add($Name)
}

function Assert-RgNoMatches {
    param(
        [Parameter(Mandatory = $true)][string]$Pattern,
        [Parameter(Mandatory = $true)][string[]]$Paths,
        [string[]]$ExtraArgs = @()
    )

    $args = @("-n", $Pattern) + $Paths + $ExtraArgs
    & rg @args
    if ($LASTEXITCODE -eq 0) {
        throw "rg found forbidden matches for pattern: $Pattern"
    }
    if ($LASTEXITCODE -gt 1) {
        throw "rg failed for pattern: $Pattern"
    }
}

try {
    Set-Location $repoRoot
    New-Item -ItemType Directory -Force -Path $reportRoot | Out-Null

    Invoke-GateStep "build reverse-engineering tool" {
        dotnet build $toolProject
    }

    Invoke-GateStep "check playwright chromium installation" {
        $playwrightScript = Join-Path $repoRoot "tools\BlazorShop.AI.StorefrontReverseEngineering\bin\Debug\net10.0\playwright.ps1"
        if (-not (Test-Path $playwrightScript)) {
            throw "Playwright script was not found. Run: dotnet build $toolProject"
        }

        $browserRoot = Join-Path $env:LOCALAPPDATA "ms-playwright"
        if (-not (Test-Path $browserRoot)) {
            throw "Playwright browsers are not installed. Run: $playwrightScript install chromium"
        }
    }

    Invoke-GateStep "run fast and schema tests" {
        dotnet test $testProject --filter "Schema|Readiness|Validation|Workflow|Cli|Lifecycle|Security|Browser|Evidence|Interaction|StableCapture|Stitch|Quality"
    }

    Invoke-GateStep "run real local Playwright HTTP fixture tests" {
        dotnet test $testProject --filter "Playwright|EndToEnd"
    }

    Invoke-GateStep "run CLI full workflow with no AI" {
        $fixtureUrl = [Uri]::new((Resolve-Path $fixturePath).Path).AbsoluteUri
        dotnet run --project $toolProject -- run --url $fixtureUrl --name Phase3AGate --output-root $projectOutputRoot --no-ai --force --run-id phase3a-gate
    }

    Invoke-GateStep "validate CLI artifacts" {
        dotnet run --project $toolProject -- validate --project (Join-Path $projectOutputRoot "phase3agate")
        dotnet run --project $toolProject -- inspect --project (Join-Path $projectOutputRoot "phase3agate")
    }

    Invoke-GateStep "boundary scan" {
        Assert-RgNoMatches `
            -Pattern "BlazorShop.AI.StorefrontReverseEngineering|StorefrontReverseEngineering" `
            -Paths @("BlazorShop.PresentationV2", "BlazorShop.Domain", "BlazorShop.Application", "BlazorShop.Infrastructure", "BlazorShop.ServiceDefaults", "BlazorShop.Tests.V2", "BlazorShop.sln") `
            -ExtraArgs @("--glob", "!bin/**", "--glob", "!obj/**")
    }

    Invoke-GateStep "prototype marker scan" {
        Assert-RgNoMatches `
            -Pattern 'OnePixelPng|BuildStyleSamples|BuildBoxes|afterDom = before\.DomHtml|DomChanged: true|NotSupportedException|CaptureMethod = "stitched"' `
            -Paths @("tools\BlazorShop.AI.StorefrontReverseEngineering") `
            -ExtraArgs @("--glob", "!bin/**", "--glob", "!obj/**")
    }

    if (-not $SkipStorefrontBuilderSmoke) {
        $pwsh = (Get-Command pwsh -ErrorAction SilentlyContinue).Source
        if (-not $pwsh) {
            throw "PowerShell 7 (pwsh) is required for StorefrontBuilder compatibility smoke. Install pwsh or rerun with -SkipStorefrontBuilderSmoke for reverse-engineering-only validation."
        }

        Invoke-GateStep "StorefrontBuilder plan-only smoke" {
            & $pwsh -ExecutionPolicy Bypass -File (Join-Path $repoRoot "tools\BlazorShop.AI.StorefrontBuilder\build-storefront.ps1") -Url "https://example.test" -Name "Demo" -StoreKey "sample" -OutputRoot "obj/storefront-builder/generated/reverse-engineering-gate" -Mode "plan-only"
        }

        Invoke-GateStep "StorefrontBuilder create hardening smoke" {
            & $pwsh -ExecutionPolicy Bypass -File (Join-Path $repoRoot "tools\BlazorShop.AI.StorefrontBuilder\tests\generation\Test-StorefrontBuilderCreateHardening.ps1")
        }
    }

    $reportPath = Join-Path $reportRoot ("phase3a-hardening-gate-" + (Get-Date -Format "yyyyMMddHHmmss") + ".md")
    @(
        "# Storefront Reverse Engineering Phase 3A Hardening Gate",
        "",
        "Status: passed",
        "",
        "Steps:",
        ($steps | ForEach-Object { "- $_" })
    ) | Set-Content -Path $reportPath -Encoding UTF8
    Write-Host "Gate passed. Report: $reportPath"
}
catch {
    $reportPath = Join-Path $reportRoot ("phase3a-hardening-gate-failed-" + (Get-Date -Format "yyyyMMddHHmmss") + ".md")
    @(
        "# Storefront Reverse Engineering Phase 3A Hardening Gate",
        "",
        "Status: failed",
        "",
        "Error:",
        '```text',
        $_.Exception.Message,
        '```',
        "",
        "Completed steps:",
        ($steps | ForEach-Object { "- $_" })
    ) | Set-Content -Path $reportPath -Encoding UTF8
    Write-Error "Gate failed. Report: $reportPath. Error: $($_.Exception.Message)"
    exit 1
}

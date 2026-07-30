param(
    [switch]$SkipStorefrontBuilderSmoke
)

$ErrorActionPreference = "Stop"

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
$toolProject = Join-Path $repoRoot "tools\BlazorShop.AI.StorefrontReverseEngineering\BlazorShop.AI.StorefrontReverseEngineering.csproj"
$testProject = Join-Path $repoRoot "tools\BlazorShop.AI.StorefrontReverseEngineering\tests\BlazorShop.AI.StorefrontReverseEngineering.Tests\BlazorShop.AI.StorefrontReverseEngineering.Tests.csproj"
$reportRoot = Join-Path $repoRoot "obj\storefront-reverse-engineering\reports"
$projectOutputRoot = Join-Path $repoRoot "obj\storefront-reverse-engineering\projects\phase3a-gate"
$artifactProjectRoot = Join-Path $projectOutputRoot "phase3agate"
$workflowRunId = "phase3a-gate"
$readinessReportPath = Join-Path $artifactProjectRoot "reports\readiness-report.json"
$fixturePath = Join-Path $repoRoot "tools\BlazorShop.AI.StorefrontReverseEngineering\tests\BlazorShop.AI.StorefrontReverseEngineering.Tests\Fixtures\static-storefront.html"
$commands = New-Object System.Collections.Generic.List[string]
$stepResults = New-Object System.Collections.Generic.List[object]
$testSummaries = New-Object System.Collections.Generic.List[string]
$failedStep = $null
$playwrightChromiumInstalled = $false

function Format-CommandArgument {
    param([Parameter(Mandatory = $true)][string]$Value)

    if ($Value.Contains(" ")) {
        return '"' + $Value + '"'
    }

    return $Value
}

function Invoke-LoggedCommand {
    param(
        [Parameter(Mandatory = $true)][string]$CommandLine,
        [Parameter(Mandatory = $true)][scriptblock]$Script
    )

    $commands.Add($CommandLine)
    & $Script
    if ($LASTEXITCODE -ne 0) {
        throw "Command failed with exit code ${LASTEXITCODE}: $CommandLine"
    }
}

function Invoke-LoggedDotNetTest {
    param(
        [Parameter(Mandatory = $true)][string]$Name,
        [Parameter(Mandatory = $true)][string]$Filter
    )

    $commandLine = "dotnet test $(Format-CommandArgument $testProject) --filter $(Format-CommandArgument $Filter)"
    $commands.Add($commandLine)
    $rawOutput = & dotnet test $testProject --filter $Filter 2>&1
    $exitCode = $LASTEXITCODE
    $lines = @($rawOutput | ForEach-Object { $_.ToString() })
    $lines | ForEach-Object { Write-Host $_ }

    $summaryLine = ($lines | Select-String -Pattern "Passed!|Failed!" | Select-Object -Last 1).Line
    if (-not [string]::IsNullOrWhiteSpace($summaryLine)) {
        $testSummaries.Add("${Name}: $summaryLine")
    }

    if ($exitCode -ne 0) {
        throw "Command failed with exit code ${exitCode}: $commandLine"
    }
}

function Invoke-GateStep {
    param(
        [Parameter(Mandatory = $true)][string]$Name,
        [Parameter(Mandatory = $true)][scriptblock]$Script
    )

    Write-Host "== $Name =="
    $startedUtc = [DateTimeOffset]::UtcNow
    try {
        & $Script
        $duration = [DateTimeOffset]::UtcNow - $startedUtc
        $stepResults.Add([pscustomobject]@{
            Name = $Name
            Status = "passed"
            DurationSeconds = [Math]::Round($duration.TotalSeconds, 2)
        })
    }
    catch {
        $duration = [DateTimeOffset]::UtcNow - $startedUtc
        $script:failedStep = $Name
        $stepResults.Add([pscustomobject]@{
            Name = $Name
            Status = "failed"
            DurationSeconds = [Math]::Round($duration.TotalSeconds, 2)
        })
        throw
    }
}

function Assert-RgNoMatches {
    param(
        [Parameter(Mandatory = $true)][string]$Pattern,
        [Parameter(Mandatory = $true)][string[]]$Paths,
        [string[]]$ExtraArgs = @()
    )

    $rgArgs = @("-n", $Pattern) + $Paths + $ExtraArgs
    $commands.Add("rg " + (($rgArgs | ForEach-Object { Format-CommandArgument $_ }) -join " "))
    & rg @rgArgs
    if ($LASTEXITCODE -eq 0) {
        throw "rg found forbidden matches for pattern: $Pattern"
    }
    if ($LASTEXITCODE -gt 1) {
        throw "rg failed for pattern: $Pattern"
    }
}

function New-GateReportLines {
    param(
        [Parameter(Mandatory = $true)][string]$Status,
        [string]$ErrorMessage = ""
    )

    $gitCommit = (& git rev-parse HEAD).Trim()
    $gitBranch = (& git branch --show-current).Trim()
    $dotnetVersion = (& dotnet --version).Trim()
    $osDescription = [System.Runtime.InteropServices.RuntimeInformation]::OSDescription
    $utcTimestamp = [DateTimeOffset]::UtcNow.ToString("u", [System.Globalization.CultureInfo]::InvariantCulture)

    $lines = New-Object System.Collections.Generic.List[string]
    $lines.Add("# Storefront Reverse Engineering Phase 3A Final Fix Gate")
    $lines.Add("")
    $lines.Add("Status: $Status")
    $lines.Add("Commit SHA: $gitCommit")
    $lines.Add("Branch: $gitBranch")
    $lines.Add("UTC timestamp: $utcTimestamp")
    $lines.Add(".NET version: $dotnetVersion")
    $lines.Add("Playwright Chromium installed: $playwrightChromiumInstalled")
    $lines.Add("OS: $osDescription")
    $lines.Add("Artifact project root: $artifactProjectRoot")
    $lines.Add("Workflow run ID: $workflowRunId")
    $lines.Add("Readiness report path: $readinessReportPath")
    if (-not [string]::IsNullOrWhiteSpace($failedStep)) {
        $lines.Add("Failed step: $failedStep")
    }
    $lines.Add("")
    $lines.Add("Executed commands:")
    if ($commands.Count -eq 0) {
        $lines.Add("- (none)")
    }
    else {
        foreach ($command in $commands) {
            $lines.Add("- " + [char]96 + $command + [char]96)
        }
    }
    $lines.Add("")
    $lines.Add("Steps:")
    if ($stepResults.Count -eq 0) {
        $lines.Add("- (none)")
    }
    else {
        foreach ($step in $stepResults) {
            $lines.Add("- $($step.Name): $($step.Status) ($($step.DurationSeconds)s)")
        }
    }
    $lines.Add("")
    $lines.Add("Test summaries:")
    if ($testSummaries.Count -eq 0) {
        $lines.Add("- (not available)")
    }
    else {
        foreach ($summary in $testSummaries) {
            $lines.Add("- $summary")
        }
    }

    if (-not [string]::IsNullOrWhiteSpace($ErrorMessage)) {
        $lines.Add("")
        $lines.Add("Error:")
        $lines.Add('```text')
        $lines.Add($ErrorMessage)
        $lines.Add('```')
    }

    return $lines
}

try {
    Set-Location $repoRoot
    New-Item -ItemType Directory -Force -Path $reportRoot | Out-Null

    Invoke-GateStep "build reverse-engineering tool" {
        Invoke-LoggedCommand `
            -CommandLine "dotnet build $(Format-CommandArgument $toolProject)" `
            -Script { dotnet build $toolProject }
    }

    Invoke-GateStep "check playwright chromium installation" {
        $playwrightScript = Join-Path $repoRoot "tools\BlazorShop.AI.StorefrontReverseEngineering\bin\Debug\net10.0\playwright.ps1"
        if (-not (Test-Path $playwrightScript)) {
            throw "Playwright script was not found. Run: dotnet build $toolProject"
        }

        $browserRoot = Join-Path $env:LOCALAPPDATA "ms-playwright"
        $script:playwrightChromiumInstalled = Test-Path $browserRoot
        if (-not $script:playwrightChromiumInstalled) {
            throw "Playwright browsers are not installed. Run: $playwrightScript install chromium"
        }
    }

    Invoke-GateStep "run final fix fast tests" {
        Invoke-LoggedDotNetTest `
            -Name "final fix fast tests" `
            -Filter "StableCapture|Stitch|Quality|Readiness|Validation|Workflow|Cli|Lifecycle|Security|Browser|Boundary|Evidence|Interaction|Schema"
    }

    Invoke-GateStep "run real local Playwright HTTP fixture tests" {
        Invoke-LoggedDotNetTest `
            -Name "real local Playwright fixture tests" `
            -Filter "Playwright|EndToEnd"
    }

    Invoke-GateStep "run CLI full workflow with no AI" {
        $fixtureUrl = [Uri]::new((Resolve-Path $fixturePath).Path).AbsoluteUri
        Invoke-LoggedCommand `
            -CommandLine "dotnet run --project $(Format-CommandArgument $toolProject) -- run --url $fixtureUrl --name Phase3AGate --output-root $(Format-CommandArgument $projectOutputRoot) --no-ai --force --run-id $workflowRunId" `
            -Script { dotnet run --project $toolProject -- run --url $fixtureUrl --name Phase3AGate --output-root $projectOutputRoot --no-ai --force --run-id $workflowRunId }
    }

    Invoke-GateStep "validate CLI artifacts" {
        Invoke-LoggedCommand `
            -CommandLine "dotnet run --project $(Format-CommandArgument $toolProject) -- validate --project $(Format-CommandArgument $artifactProjectRoot)" `
            -Script { dotnet run --project $toolProject -- validate --project $artifactProjectRoot }
        Invoke-LoggedCommand `
            -CommandLine "dotnet run --project $(Format-CommandArgument $toolProject) -- inspect --project $(Format-CommandArgument $artifactProjectRoot)" `
            -Script { dotnet run --project $toolProject -- inspect --project $artifactProjectRoot }
        if (-not (Test-Path $readinessReportPath)) {
            throw "Readiness report was not found: $readinessReportPath"
        }
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
            -Paths @(
                "tools\BlazorShop.AI.StorefrontReverseEngineering\Application",
                "tools\BlazorShop.AI.StorefrontReverseEngineering\Browser",
                "tools\BlazorShop.AI.StorefrontReverseEngineering\Cli",
                "tools\BlazorShop.AI.StorefrontReverseEngineering\Contracts",
                "tools\BlazorShop.AI.StorefrontReverseEngineering\Evidence",
                "tools\BlazorShop.AI.StorefrontReverseEngineering\Interactions",
                "tools\BlazorShop.AI.StorefrontReverseEngineering\Storage",
                "tools\BlazorShop.AI.StorefrontReverseEngineering\Validation",
                "tools\BlazorShop.AI.StorefrontReverseEngineering\Workflows"
            ) `
            -ExtraArgs @("--glob", "!bin/**", "--glob", "!obj/**")
    }

    if (-not $SkipStorefrontBuilderSmoke) {
        $pwsh = (Get-Command pwsh -ErrorAction SilentlyContinue).Source
        if (-not $pwsh) {
            throw "PowerShell 7 (pwsh) is required for StorefrontBuilder compatibility smoke. Install pwsh or rerun with -SkipStorefrontBuilderSmoke for reverse-engineering-only validation."
        }

        Invoke-GateStep "StorefrontBuilder plan-only smoke" {
            Invoke-LoggedCommand `
                -CommandLine "$pwsh -ExecutionPolicy Bypass -File $(Format-CommandArgument (Join-Path $repoRoot "tools\BlazorShop.AI.StorefrontBuilder\build-storefront.ps1")) -Url https://example.test -Name Demo -StoreKey sample -OutputRoot obj/storefront-builder/generated/reverse-engineering-gate -Mode plan-only" `
                -Script { & $pwsh -ExecutionPolicy Bypass -File (Join-Path $repoRoot "tools\BlazorShop.AI.StorefrontBuilder\build-storefront.ps1") -Url "https://example.test" -Name "Demo" -StoreKey "sample" -OutputRoot "obj/storefront-builder/generated/reverse-engineering-gate" -Mode "plan-only" }
        }

        Invoke-GateStep "StorefrontBuilder create hardening smoke" {
            Invoke-LoggedCommand `
                -CommandLine "$pwsh -ExecutionPolicy Bypass -File $(Format-CommandArgument (Join-Path $repoRoot "tools\BlazorShop.AI.StorefrontBuilder\tests\generation\Test-StorefrontBuilderCreateHardening.ps1"))" `
                -Script { & $pwsh -ExecutionPolicy Bypass -File (Join-Path $repoRoot "tools\BlazorShop.AI.StorefrontBuilder\tests\generation\Test-StorefrontBuilderCreateHardening.ps1") }
        }
    }

    $reportPath = Join-Path $reportRoot ("phase3a-final-fix-gate-" + (Get-Date -Format "yyyyMMddHHmmss") + ".md")
    New-GateReportLines -Status "passed" | Set-Content -Path $reportPath -Encoding UTF8
    Write-Host "Gate passed. Report: $reportPath"
}
catch {
    $reportPath = Join-Path $reportRoot ("phase3a-final-fix-gate-failed-" + (Get-Date -Format "yyyyMMddHHmmss") + ".md")
    New-GateReportLines -Status "failed" -ErrorMessage $_.Exception.Message | Set-Content -Path $reportPath -Encoding UTF8
    Write-Error "Gate failed. Report: $reportPath. Error: $($_.Exception.Message)"
    exit 1
}

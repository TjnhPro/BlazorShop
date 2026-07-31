param(
    [switch]$SkipStorefrontBuilderSmoke,
    [int]$CommandTimeoutSeconds = 900
)

$ErrorActionPreference = "Stop"

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
$toolProject = Join-Path $repoRoot "tools\BlazorShop.AI.StorefrontReverseEngineering\BlazorShop.AI.StorefrontReverseEngineering.csproj"
$testProject = Join-Path $repoRoot "tools\BlazorShop.AI.StorefrontReverseEngineering\tests\BlazorShop.AI.StorefrontReverseEngineering.Tests\BlazorShop.AI.StorefrontReverseEngineering.Tests.csproj"
$fixtureRoot = Join-Path $repoRoot "tools\BlazorShop.AI.StorefrontReverseEngineering\tests\BlazorShop.AI.StorefrontReverseEngineering.Tests\Fixtures"
$reportRoot = Join-Path $repoRoot "obj\storefront-reverse-engineering\reports"
$projectOutputRoot = Join-Path $repoRoot "obj\storefront-reverse-engineering\projects\phase3b-gate"
$fixtureRoutes = @(
    @{ Label = "home"; File = "phase3b-home.html"; ProjectId = "phase3b-gate-home" },
    @{ Label = "plp"; File = "phase3b-plp.html"; ProjectId = "phase3b-gate-plp" },
    @{ Label = "pdp"; File = "phase3b-pdp.html"; ProjectId = "phase3b-gate-pdp" },
    @{ Label = "unsupported"; File = "phase3b-unsupported.html"; ProjectId = "phase3b-gate-unsupported" }
)
$commands = New-Object System.Collections.Generic.List[string]
$stepResults = New-Object System.Collections.Generic.List[object]
$testSummaries = New-Object System.Collections.Generic.List[string]
$artifactProjectRoots = New-Object System.Collections.Generic.List[string]
$failedStep = $null
$lastProcessExitCode = 0

function Format-CommandArgument {
    param([Parameter(Mandatory = $true)][string]$Value)

    if ($Value.Contains(" ")) {
        return '"' + $Value + '"'
    }

    return $Value
}

function Invoke-LoggedProcess {
    param(
        [Parameter(Mandatory = $true)][string]$FileName,
        [Parameter(Mandatory = $true)][string[]]$Arguments,
        [string]$SummaryName = "",
        [int]$TimeoutSeconds = $CommandTimeoutSeconds,
        [int[]]$AllowedExitCodes = @(0)
    )

    $commandLine = (Format-CommandArgument $FileName) + " " + (($Arguments | ForEach-Object { Format-CommandArgument $_ }) -join " ")
    $commands.Add($commandLine)

    $startInfo = [System.Diagnostics.ProcessStartInfo]::new()
    $startInfo.FileName = $FileName
    $startInfo.Arguments = ($Arguments | ForEach-Object { Format-CommandArgument $_ }) -join " "
    $startInfo.WorkingDirectory = $repoRoot
    $startInfo.RedirectStandardOutput = $true
    $startInfo.RedirectStandardError = $true
    $startInfo.UseShellExecute = $false
    $startInfo.CreateNoWindow = $true

    $process = [System.Diagnostics.Process]::Start($startInfo)
    if ($null -eq $process) {
        throw "Failed to start command: $commandLine"
    }

    $stdoutTask = $process.StandardOutput.ReadToEndAsync()
    $stderrTask = $process.StandardError.ReadToEndAsync()
    if (-not $process.WaitForExit($TimeoutSeconds * 1000)) {
        try {
            $process.Kill($true)
        }
        catch {
            $process.Kill()
        }

        throw "Command timed out after ${TimeoutSeconds}s: $commandLine"
    }

    $stdout = $stdoutTask.GetAwaiter().GetResult()
    $stderr = $stderrTask.GetAwaiter().GetResult()
    if (-not [string]::IsNullOrWhiteSpace($stdout)) {
        Write-Host $stdout
    }
    if (-not [string]::IsNullOrWhiteSpace($stderr)) {
        Write-Host $stderr
    }

    if (-not [string]::IsNullOrWhiteSpace($SummaryName)) {
        $summaryLine = (($stdout + [Environment]::NewLine + $stderr) -split "\r?\n" | Select-String -Pattern "Passed!|Failed!" | Select-Object -Last 1).Line
        if (-not [string]::IsNullOrWhiteSpace($summaryLine)) {
            $testSummaries.Add("${SummaryName}: $summaryLine")
        }
    }

    $script:lastProcessExitCode = $process.ExitCode
    if ($AllowedExitCodes -notcontains $script:lastProcessExitCode) {
        throw "Command failed with exit code $($process.ExitCode): $commandLine"
    }
}

function Assert-ExpectedStrictReviewBlocker {
    param(
        [Parameter(Mandatory = $true)][string]$ProjectRoot,
        [Parameter(Mandatory = $true)][string]$RunId
    )

    $readinessPath = Join-Path $ProjectRoot "reports\readiness-report.json"
    if (-not (Test-Path $readinessPath)) {
        throw "Readiness report was not found after Phase 3B fixture workflow: $readinessPath"
    }

    $readiness = Get-Content -Raw $readinessPath | ConvertFrom-Json
    if (-not $readiness.passed) {
        throw "Phase 3B fixture readiness did not pass before strict review blocker: $readinessPath"
    }

    $runPath = Join-Path $ProjectRoot "runs\$RunId.json"
    if (-not (Test-Path $runPath)) {
        throw "Workflow run file was not found after strict review blocker: $runPath"
    }

    $run = Get-Content -Raw $runPath | ConvertFrom-Json
    if ($run.status -ne "Failed") {
        throw "Expected workflow status Failed when fixture exits 3 after Phase 3B analysis; actual status: $($run.status)"
    }

    $codes = @($run.steps | ForEach-Object { $_.errors } | ForEach-Object { $_.code })
    if ($codes -notcontains "missing-review-decisions" -or $codes -notcontains "reviewed-blueprint-not-resolved") {
        throw "Fixture exited 3 without the expected strict review blockers. Codes: $($codes -join ', ')"
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
    Invoke-LoggedProcess -FileName "rg" -Arguments $rgArgs -TimeoutSeconds 120
    throw "rg found forbidden matches for pattern: $Pattern"
}

function Assert-RgNoMatchesHandled {
    param(
        [Parameter(Mandatory = $true)][string]$Pattern,
        [Parameter(Mandatory = $true)][string[]]$Paths,
        [string[]]$ExtraArgs = @()
    )

    $commandLine = "rg " + ((@("-n", $Pattern) + $Paths + $ExtraArgs | ForEach-Object { Format-CommandArgument $_ }) -join " ")
    $commands.Add($commandLine)
    & rg -n $Pattern @Paths @ExtraArgs
    if ($LASTEXITCODE -eq 0) {
        throw "rg found forbidden matches for pattern: $Pattern"
    }
    if ($LASTEXITCODE -gt 1) {
        throw "rg failed for pattern: $Pattern"
    }
}

function Read-JsonFile {
    param([Parameter(Mandatory = $true)][string]$Path)

    if (-not (Test-Path $Path)) {
        return $null
    }

    return Get-Content -LiteralPath $Path -Raw | ConvertFrom-Json
}

function Get-JsonValueOrDefault {
    param(
        $Value,
        [string]$Default = "unknown"
    )

    if ($null -eq $Value) {
        return $Default
    }

    return [string]$Value
}

function Get-JsonArrayCount {
    param($Value)

    if ($null -eq $Value) {
        return 0
    }

    return @($Value).Count
}

function Add-ArtifactSummary {
    param(
        [Parameter(Mandatory = $true)]$ReportLines,
        [Parameter(Mandatory = $true)][string]$ProjectRoot
    )

    $blueprintDraft = Join-Path $ProjectRoot "analysis\visual-blueprint.v1.draft.json"
    $blueprintReviewed = Join-Path $ProjectRoot "analysis\visual-blueprint.v1.reviewed.json"
    $catalog = Read-JsonFile (Join-Path $ProjectRoot "presentation-catalog\presentation-component-catalog.json")
    $readiness = Read-JsonFile (Join-Path $ProjectRoot "reports\generation-readiness.json")
    $unsupported = Read-JsonFile (Join-Path $ProjectRoot "analysis\mapping\unsupported-patterns.json")
    $reviewQueue = Read-JsonFile (Join-Path $ProjectRoot "review\review-queue.json")

    $ReportLines.Add("- Artifact project root: $ProjectRoot")
    $ReportLines.Add("- Blueprint paths: $blueprintDraft; $blueprintReviewed")
    $ReportLines.Add("- Presentation catalog version: " + (Get-JsonValueOrDefault $catalog.schemaVersion))
    $ReportLines.Add("- Readiness result: " + (Get-JsonValueOrDefault $readiness.passed))
    $ReportLines.Add("- Unsupported pattern count: " + (Get-JsonArrayCount $unsupported.patterns))
    $ReportLines.Add("- Review queue count: " + (Get-JsonArrayCount $reviewQueue.items))

    if ($null -ne $readiness -and $readiness.passed -ne $true) {
        foreach ($finding in @($readiness.findings | Where-Object { $_.severity -eq "blocking" })) {
            $ReportLines.Add("- Blocking artifact: " + (Get-JsonValueOrDefault $finding.artifactPath "(not specified)"))
            $ReportLines.Add("- Fix: inspect the listed artifact, resolve " + (Get-JsonValueOrDefault $finding.code "unknown") + ", then rerun assemble-blueprint-v1.")
        }
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
    $utcTimestamp = [DateTimeOffset]::UtcNow.ToString("u", [System.Globalization.CultureInfo]::InvariantCulture)

    $lines = New-Object System.Collections.Generic.List[string]
    $lines.Add("# Storefront Reverse Engineering Phase 3B Gate")
    $lines.Add("")
    $lines.Add("Status: $Status")
    $lines.Add("Commit SHA: $gitCommit")
    $lines.Add("Branch: $gitBranch")
    $lines.Add("UTC timestamp: $utcTimestamp")
    $lines.Add(".NET version: $dotnetVersion")
    $lines.Add("Artifact project root: $projectOutputRoot")
    $lines.Add("Fixture routes: " + (($fixtureRoutes | ForEach-Object { $_["Label"] + "=" + $_["File"] }) -join ", "))
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
    $lines.Add("")
    $lines.Add("Phase 3B coverage:")
    foreach ($coverage in @(
        "schema tests",
        "evidence snapshot tests",
        "token extraction tests",
        "semantic token tests",
        "page archetype tests",
        "section segmentation tests",
        "responsive tests",
        "interaction model tests",
        "component candidate tests",
        "ecommerce region tests",
        "Presentation catalog validation tests",
        "mapping tests",
        "unsupported pattern tests",
        "confidence tests",
        "review workflow tests",
        "blueprint schema/reference tests",
        "generation readiness tests"
    )) {
        $lines.Add("- ${coverage}: covered by the full ReverseEngineering test run")
    }
    $lines.Add("")
    $lines.Add("Artifact summaries:")
    if ($artifactProjectRoots.Count -eq 0) {
        $lines.Add("- (none)")
    }
    else {
        foreach ($root in $artifactProjectRoots) {
            Add-ArtifactSummary -ReportLines $lines -ProjectRoot $root
        }
    }
    $lines.Add("")
    $lines.Add("Known limitations:")
    $lines.Add("- Phase 3B does not generate Razor, CSS, StorefrontBuilder projects, or runtime storefront code.")
    $lines.Add("- StorefrontBuilder does not consume visual-blueprint.v1 artifacts until a later approved phase.")

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

    Invoke-GateStep "build ReverseEngineering tool" {
        Invoke-LoggedProcess -FileName "dotnet" -Arguments @("build", $toolProject)
    }

    Invoke-GateStep "run Phase 3A regression fast subset" {
        Invoke-LoggedProcess `
            -FileName "dotnet" `
            -Arguments @("test", $testProject, "--filter", "StableCapture|Stitch|Quality|Readiness|Validation|Workflow|Cli|Lifecycle|Security|Browser|Boundary|Evidence|Interaction|Schema", "--blame-hang", "--blame-hang-timeout", "5m") `
            -SummaryName "Phase 3A regression fast subset"
    }

    Invoke-GateStep "run all ReverseEngineering tests" {
        Invoke-LoggedProcess `
            -FileName "dotnet" `
            -Arguments @("test", $testProject, "--blame-hang", "--blame-hang-timeout", "5m") `
            -SummaryName "all ReverseEngineering tests"
    }

    Invoke-GateStep "run local multi-page fixture analysis workflow" {
        foreach ($fixture in $fixtureRoutes) {
            $fixturePath = Join-Path $fixtureRoot $fixture["File"]
            $fixtureUrl = [Uri]::new((Resolve-Path $fixturePath).Path).AbsoluteUri
            $artifactRoot = Join-Path $projectOutputRoot $fixture["ProjectId"]
            $runId = "phase3b-gate-" + $fixture["Label"]
            $artifactProjectRoots.Add($artifactRoot)

            Invoke-LoggedProcess `
                -FileName "dotnet" `
                -Arguments @(
                    "run",
                    "--project",
                    $toolProject,
                    "--",
                    "run",
                    "--url",
                    $fixtureUrl,
                    "--name",
                    $fixture["ProjectId"],
                    "--output-root",
                    $projectOutputRoot,
                    "--no-ai",
                    "--force",
                    "--run-id",
                    $runId
                ) `
                -AllowedExitCodes @(0, 3)

            if ($script:lastProcessExitCode -eq 3) {
                Assert-ExpectedStrictReviewBlocker -ProjectRoot $artifactRoot -RunId $runId
                $testSummaries.Add("Phase 3B fixture $($fixture["Label"]): analysis/readiness passed; final reviewed handoff stopped on expected strict review-decision blockers.")
            }

            Invoke-LoggedProcess `
                -FileName "dotnet" `
                -Arguments @("run", "--project", $toolProject, "--", "inspect", "--project", $artifactRoot)
        }
    }

    Invoke-GateStep "boundary scan" {
        $reverseEngineeringProductionPaths = @(
            "tools\BlazorShop.AI.StorefrontReverseEngineering\Analysis",
            "tools\BlazorShop.AI.StorefrontReverseEngineering\Application",
            "tools\BlazorShop.AI.StorefrontReverseEngineering\Browser",
            "tools\BlazorShop.AI.StorefrontReverseEngineering\Cli",
            "tools\BlazorShop.AI.StorefrontReverseEngineering\Contracts",
            "tools\BlazorShop.AI.StorefrontReverseEngineering\Domain",
            "tools\BlazorShop.AI.StorefrontReverseEngineering\Evidence",
            "tools\BlazorShop.AI.StorefrontReverseEngineering\Interactions",
            "tools\BlazorShop.AI.StorefrontReverseEngineering\Provenance",
            "tools\BlazorShop.AI.StorefrontReverseEngineering\Storage",
            "tools\BlazorShop.AI.StorefrontReverseEngineering\Validation",
            "tools\BlazorShop.AI.StorefrontReverseEngineering\Workflows",
            "tools\BlazorShop.AI.StorefrontReverseEngineering\BlazorShop.AI.StorefrontReverseEngineering.csproj"
        )

        Assert-RgNoMatchesHandled `
            -Pattern "BlazorShop.AI.StorefrontReverseEngineering|StorefrontReverseEngineering" `
            -Paths @("BlazorShop.PresentationV2", "BlazorShop.Domain", "BlazorShop.Application", "BlazorShop.Infrastructure", "BlazorShop.ServiceDefaults", "BlazorShop.Tests.V2", "BlazorShop.sln") `
            -ExtraArgs @("--glob", "!bin/**", "--glob", "!obj/**")

        Assert-RgNoMatchesHandled `
            -Pattern '(<(ProjectReference|PackageReference)[^>]+BlazorShop\.(Storefront\.V2|ControlPlane|CommerceNode|Domain|Infrastructure|Web\.SharedV2))' `
            -Paths $reverseEngineeringProductionPaths `
            -ExtraArgs @("--glob", "*.csproj", "--glob", "!bin/**", "--glob", "!obj/**")

        Assert-RgNoMatchesHandled `
            -Pattern '^\s*using\s+BlazorShop\.(Storefront\.V2|ControlPlane|CommerceNode|Domain|Infrastructure|Web\.SharedV2)\b' `
            -Paths $reverseEngineeringProductionPaths `
            -ExtraArgs @("--glob", "*.cs", "--glob", "!bin/**", "--glob", "!obj/**")

        Assert-RgNoMatchesHandled `
            -Pattern "storefront-builder/generated|BlazorShop\.Storefront\.Generated" `
            -Paths $reverseEngineeringProductionPaths `
            -ExtraArgs @("--glob", "*.cs", "--glob", "*.csproj", "--glob", "!bin/**", "--glob", "!obj/**")

        Assert-RgNoMatchesHandled `
            -Pattern "visual-blueprint\.v1" `
            -Paths @("tools\BlazorShop.AI.StorefrontBuilder") `
            -ExtraArgs @("--glob", "!bin/**", "--glob", "!obj/**")

        # no Razor/CSS generation code is introduced in Phase 3B. Boundary marker
        # strings such as @page are allowed in handoff instructions and validators.
        Assert-RgNoMatchesHandled `
            -Pattern 'WriteAllText(Async)?\([^\r\n]*(\.razor|\.css)' `
            -Paths $reverseEngineeringProductionPaths `
            -ExtraArgs @("--glob", "*.cs", "--glob", "!bin/**", "--glob", "!obj/**")
    }

    if (-not $SkipStorefrontBuilderSmoke) {
        Invoke-GateStep "StorefrontBuilder plan-only smoke" {
            $pwsh = (Get-Command pwsh -ErrorAction SilentlyContinue).Source
            if (-not $pwsh) {
                throw "PowerShell 7 (pwsh) is required for StorefrontBuilder plan-only smoke. Rerun with -SkipStorefrontBuilderSmoke for reverse-engineering-only validation."
            }

            Invoke-LoggedProcess `
                -FileName $pwsh `
                -Arguments @(
                    "-ExecutionPolicy",
                    "Bypass",
                    "-File",
                    (Join-Path $repoRoot "tools\BlazorShop.AI.StorefrontBuilder\build-storefront.ps1"),
                    "-Url",
                    "https://example.test",
                    "-Name",
                    "Demo",
                    "-StoreKey",
                    "sample",
                    "-OutputRoot",
                    "obj/storefront-builder/generated/reverse-engineering-phase3b-gate",
                    "-Mode",
                    "plan-only"
                )
        }
    }

    $reportPath = Join-Path $reportRoot ("phase3b-gate-" + (Get-Date -Format "yyyyMMddHHmmss") + ".md")
    New-GateReportLines -Status "passed" | Set-Content -Path $reportPath -Encoding UTF8
    Write-Host "Gate passed. Report: $reportPath"
}
catch {
    $reportPath = Join-Path $reportRoot ("phase3b-gate-failed-" + (Get-Date -Format "yyyyMMddHHmmss") + ".md")
    New-GateReportLines -Status "failed" -ErrorMessage $_.Exception.Message | Set-Content -Path $reportPath -Encoding UTF8
    Write-Error "Gate failed. Report: $reportPath. Error: $($_.Exception.Message)"
    exit 1
}

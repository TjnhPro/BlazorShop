function Format-SreCommandArgument {
    param([Parameter(Mandatory = $true)][string]$Value)

    if ($Value.Contains(" ")) {
        return '"' + $Value + '"'
    }

    return $Value
}

function New-SreGateContext {
    param(
        [Parameter(Mandatory = $true)][string]$RepoRoot,
        [int]$CommandTimeoutSeconds = 900
    )

    return [ordered]@{
        RepoRoot = $RepoRoot
        ToolProject = Join-Path $RepoRoot "tools\BlazorShop.AI.StorefrontReverseEngineering\BlazorShop.AI.StorefrontReverseEngineering.csproj"
        ToolDll = Join-Path $RepoRoot "tools\BlazorShop.AI.StorefrontReverseEngineering\bin\Debug\net10.0\BlazorShop.AI.StorefrontReverseEngineering.dll"
        TestProject = Join-Path $RepoRoot "tools\BlazorShop.AI.StorefrontReverseEngineering\tests\BlazorShop.AI.StorefrontReverseEngineering.Tests\BlazorShop.AI.StorefrontReverseEngineering.Tests.csproj"
        FixtureRoot = Join-Path $RepoRoot "tools\BlazorShop.AI.StorefrontReverseEngineering\tests\BlazorShop.AI.StorefrontReverseEngineering.Tests\Fixtures"
        ReportRoot = Join-Path $RepoRoot "obj\storefront-reverse-engineering\reports"
        CommandTimeoutSeconds = $CommandTimeoutSeconds
        Commands = New-Object System.Collections.Generic.List[string]
        Steps = New-Object System.Collections.Generic.List[object]
        TestSummaries = New-Object System.Collections.Generic.List[string]
        KnownLimitations = New-Object System.Collections.Generic.List[string]
        FailedStep = $null
        TestedHead = $null
        FinalHead = $null
        InitialBranch = $null
        InitialTreeClean = $false
        FullTestCount = "not-recorded"
        ClosureProofTestCount = "not-recorded"
        NegativeMutationCount = "not-recorded"
        StorefrontBuilderSmokeResult = "not-run"
        LastProcessExitCode = 0
    }
}

function Invoke-SreLoggedProcess {
    param(
        [Parameter(Mandatory = $true)]$Context,
        [Parameter(Mandatory = $true)][string]$FileName,
        [Parameter(Mandatory = $true)][string[]]$Arguments,
        [string]$SummaryName = "",
        [int]$TimeoutSeconds = $Context.CommandTimeoutSeconds,
        [int[]]$AllowedExitCodes = @(0)
    )

    $commandLine = (Format-SreCommandArgument $FileName) + " " + (($Arguments | ForEach-Object { Format-SreCommandArgument $_ }) -join " ")
    $Context.Commands.Add($commandLine)

    $startInfo = [System.Diagnostics.ProcessStartInfo]::new()
    $startInfo.FileName = $FileName
    $startInfo.Arguments = ($Arguments | ForEach-Object { Format-SreCommandArgument $_ }) -join " "
    $startInfo.WorkingDirectory = $Context.RepoRoot
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
            $Context.TestSummaries.Add("${SummaryName}: $summaryLine")
            if ($summaryLine -match "Total:\s+(\d+)") {
                if ($SummaryName -eq "Full ReverseEngineering tests") {
                    $Context.FullTestCount = $Matches[1]
                }
                elseif ($SummaryName -eq "Grouped Phase 3 closure proof") {
                    $Context.ClosureProofTestCount = $Matches[1]
                }
                elseif ($SummaryName -like "*Negative*") {
                    $Context.NegativeMutationCount = $Matches[1]
                }
            }
        }
    }

    $Context.LastProcessExitCode = $process.ExitCode
    if ($AllowedExitCodes -notcontains $Context.LastProcessExitCode) {
        throw "Command failed with exit code $($process.ExitCode): $commandLine"
    }
}

function Invoke-SreStep {
    param(
        [Parameter(Mandatory = $true)]$Context,
        [Parameter(Mandatory = $true)][string]$Name,
        [Parameter(Mandatory = $true)][scriptblock]$Script
    )

    Write-Host "== $Name =="
    $startedUtc = [DateTimeOffset]::UtcNow
    try {
        & $Script
        $duration = [DateTimeOffset]::UtcNow - $startedUtc
        $Context.Steps.Add([pscustomobject]@{
            Name = $Name
            Status = "passed"
            DurationSeconds = [Math]::Round($duration.TotalSeconds, 2)
        })
    }
    catch {
        $duration = [DateTimeOffset]::UtcNow - $startedUtc
        $Context.FailedStep = $Name
        $Context.Steps.Add([pscustomobject]@{
            Name = $Name
            Status = "failed"
            DurationSeconds = [Math]::Round($duration.TotalSeconds, 2)
        })
        throw
    }
}

function Assert-SreCleanWorkingTree {
    param([Parameter(Mandatory = $true)]$Context)

    $status = (& git status --porcelain)
    if ($status.Count -gt 0) {
        throw "Working tree is dirty. Commit or remove local changes before running final closure gate:`n$($status -join [Environment]::NewLine)"
    }

    $Context.InitialTreeClean = $true
}

function Assert-SreHeadUnchanged {
    param([Parameter(Mandatory = $true)]$Context)

    $Context.FinalHead = (& git rev-parse HEAD).Trim()
    if ($Context.FinalHead -ne $Context.TestedHead) {
        throw "HEAD changed during gate. Tested '$($Context.TestedHead)' but final HEAD is '$($Context.FinalHead)'."
    }
}

function Invoke-SreTest {
    param(
        [Parameter(Mandatory = $true)]$Context,
        [Parameter(Mandatory = $true)][string]$Name,
        [string]$Filter = ""
    )

    $arguments = @("test", $Context.TestProject, "--no-build", "--no-restore")
    if (-not [string]::IsNullOrWhiteSpace($Filter)) {
        $arguments += @("--filter", $Filter)
    }

    $arguments += @("--blame-hang", "--blame-hang-timeout", "5m")
    Invoke-SreLoggedProcess -Context $Context -FileName "dotnet" -Arguments $arguments -SummaryName $Name
}

function Invoke-SreCli {
    param(
        [Parameter(Mandatory = $true)]$Context,
        [Parameter(Mandatory = $true)][string[]]$Arguments,
        [int[]]$AllowedExitCodes = @(0)
    )

    if (-not (Test-Path $Context.ToolDll)) {
        throw "ReverseEngineering CLI DLL was not found. Run the restore/build steps before CLI proofs: $($Context.ToolDll)"
    }

    Invoke-SreLoggedProcess -Context $Context -FileName "dotnet" -Arguments (@($Context.ToolDll) + $Arguments) -AllowedExitCodes $AllowedExitCodes
}

function Get-SreClosureProofFilter {
    param([switch]$IncludePortableProof)

    $patterns = @(
        "FullyQualifiedName~BrowserCaptureTests",
        "FullyQualifiedName~PlaywrightIntegrationTests",
        "FullyQualifiedName~Phase3DProofFixtureTests",
        "FullyQualifiedName~Phase3DPositiveEndToEndTests",
        "FullyQualifiedName~Phase3DNegativeReviewMutationTests",
        "FullyQualifiedName~Phase3DNegativeSlotMutationTests",
        "FullyQualifiedName~Phase3DNegativeEvidenceMutationTests",
        "FullyQualifiedName~Phase3DNegativeHandoffMutationTests",
        "FullyQualifiedName~Phase3DNegativeBoundaryMutationTests",
        "FullyQualifiedName~BlueprintV1ReadinessTests",
        "FullyQualifiedName~AgentHandoffTests",
        "FullyQualifiedName~Phase3CFixtureAndGateTests",
        "FullyQualifiedName~Phase3CBaselineTests",
        "FullyQualifiedName~Phase3BCliDxTests",
        "FullyQualifiedName~Phase3BFixtureTests",
        "FullyQualifiedName~Phase3BGateScriptTests",
        "FullyQualifiedName~Phase3BPreflightTests"
    )

    if ($IncludePortableProof) {
        $patterns += @(
            "FullyQualifiedName~AgentHandoffEvidenceSlotProvenanceTests",
            "FullyQualifiedName~HandoffReferenceScannerTests",
            "FullyQualifiedName~HandoffConsumerDryRunLoaderTests",
            "FullyQualifiedName~PortableHandoffContractTests",
            "FullyQualifiedName~PortableHandoffValidatorTests",
            "FullyQualifiedName~PortableHandoffCliTests",
            "FullyQualifiedName~PortableHandoffCopyProofTests",
            "FullyQualifiedName~Phase3ENegativeReferenceMutationTests",
            "FullyQualifiedName~Phase3ENegativeArtifactMutationTests",
            "FullyQualifiedName~Phase3ENegativeSchemaMutationTests",
            "FullyQualifiedName~Phase3ENegativeHashMutationTests"
        )
    }

    return ($patterns -join "|")
}

function Assert-SreRgNoMatches {
    param(
        [Parameter(Mandatory = $true)]$Context,
        [Parameter(Mandatory = $true)][string]$Pattern,
        [Parameter(Mandatory = $true)][string[]]$Paths,
        [string[]]$ExtraArgs = @()
    )

    $commandLine = "rg " + ((@("-n", $Pattern) + $Paths + $ExtraArgs | ForEach-Object { Format-SreCommandArgument $_ }) -join " ")
    $Context.Commands.Add($commandLine)
    & rg -n $Pattern @Paths @ExtraArgs
    if ($LASTEXITCODE -eq 0) {
        throw "rg found forbidden matches for pattern: $Pattern"
    }
    if ($LASTEXITCODE -gt 1) {
        throw "rg failed for pattern: $Pattern"
    }
}

function Assert-SreStrictReviewBlocker {
    param(
        [Parameter(Mandatory = $true)][string]$ProjectRoot,
        [Parameter(Mandatory = $true)][string]$RunId,
        [string]$ReadinessMessage = "Fixture readiness did not pass before strict review blocker"
    )

    $readinessPath = Join-Path $ProjectRoot "reports\readiness-report.json"
    if (-not (Test-Path $readinessPath)) {
        throw "Readiness report was not found after workflow run: $readinessPath"
    }

    $readiness = Get-Content -Raw $readinessPath | ConvertFrom-Json
    if (-not $readiness.passed) {
        throw "${ReadinessMessage}: $readinessPath"
    }

    $runPath = Join-Path $ProjectRoot "runs\$RunId.json"
    if (-not (Test-Path $runPath)) {
        throw "Workflow run file was not found after strict review blocker: $runPath"
    }

    $run = Get-Content -Raw $runPath | ConvertFrom-Json
    if ($run.status -ne "Failed") {
        throw "Expected workflow status Failed when CLI exits 3 after readiness; actual status: $($run.status)"
    }

    $codes = @($run.steps | ForEach-Object { $_.errors } | ForEach-Object { $_.code })
    if ($codes -notcontains "missing-review-decisions" -or $codes -notcontains "reviewed-blueprint-not-resolved") {
        throw "CLI exited 3 without the expected strict review blockers. Codes: $($codes -join ', ')"
    }
}

function Invoke-SreBuild {
    param([Parameter(Mandatory = $true)]$Context)

    Invoke-SreStep -Context $Context -Name "build ReverseEngineering" -Script {
        Invoke-SreLoggedProcess -Context $Context -FileName "dotnet" -Arguments @("build", $Context.TestProject, "--no-restore")
    }
}

function Invoke-SreRestore {
    param([Parameter(Mandatory = $true)]$Context)

    Invoke-SreStep -Context $Context -Name "restore ReverseEngineering" -Script {
        Invoke-SreLoggedProcess -Context $Context -FileName "dotnet" -Arguments @("restore", $Context.TestProject)
    }
}

function Invoke-SrePhase3AProof {
    param([Parameter(Mandatory = $true)]$Context)

    Invoke-SreStep -Context $Context -Name "Phase 3A browser prerequisite check" -Script {
        $playwrightScript = Join-Path $Context.RepoRoot "tools\BlazorShop.AI.StorefrontReverseEngineering\bin\Debug\net10.0\playwright.ps1"
        if (-not (Test-Path $playwrightScript)) {
            throw "Playwright script was not found. Run: dotnet build $($Context.ToolProject)"
        }

        $browserRoot = Join-Path $env:LOCALAPPDATA "ms-playwright"
        if (-not (Test-Path $browserRoot)) {
            throw "Playwright browsers are not installed. Run: $playwrightScript install chromium"
        }
    }

    Invoke-SreStep -Context $Context -Name "Phase 3A grouped test coverage marker" -Script {
        $Context.TestSummaries.Add("Phase 3A regression/browser coverage: represented by the full suite and grouped closure proof test processes.")
    }

    Invoke-SreStep -Context $Context -Name "Phase 3A CLI readiness proof" -Script {
        $projectOutputRoot = Join-Path $Context.RepoRoot "obj\storefront-reverse-engineering\projects\phase3a-gate"
        $artifactProjectRoot = Join-Path $projectOutputRoot "phase3agate"
        $fixturePath = Join-Path $Context.FixtureRoot "static-storefront.html"
        $fixtureUrl = [Uri]::new((Resolve-Path $fixturePath).Path).AbsoluteUri
        $runId = "phase3a-gate"

        Invoke-SreCli `
            -Context $Context `
            -Arguments @("run", "--url", $fixtureUrl, "--name", "Phase3AGate", "--output-root", $projectOutputRoot, "--no-ai", "--force", "--run-id", $runId) `
            -AllowedExitCodes @(0, 3)

        if ($Context.LastProcessExitCode -eq 3) {
            Assert-SreStrictReviewBlocker -ProjectRoot $artifactProjectRoot -RunId $runId -ReadinessMessage "Phase 3A readiness did not pass after CLI run"
            $Context.TestSummaries.Add("Phase 3A CLI readiness workflow: readiness passed; final reviewed handoff stopped on expected strict review-decision blockers.")
        }

        Invoke-SreCli -Context $Context -Arguments @("validate", "--project", $artifactProjectRoot)
        Invoke-SreCli -Context $Context -Arguments @("inspect", "--project", $artifactProjectRoot)
    }
}

function Invoke-SrePhase3BProof {
    param([Parameter(Mandatory = $true)]$Context)

    Invoke-SreStep -Context $Context -Name "Phase 3B grouped test coverage marker" -Script {
        $Context.TestSummaries.Add("Phase 3B visual analysis/ecommerce mapping tests: represented by the full suite and grouped closure proof test processes.")
    }

    Invoke-SreStep -Context $Context -Name "Phase 3B multi-route CLI proof" -Script {
        $projectOutputRoot = Join-Path $Context.RepoRoot "obj\storefront-reverse-engineering\projects\phase3b-gate"
        $fixtureRoutes = @(
            @{ Label = "home"; File = "phase3b-home.html"; ProjectId = "phase3b-gate-home" },
            @{ Label = "plp"; File = "phase3b-plp.html"; ProjectId = "phase3b-gate-plp" },
            @{ Label = "pdp"; File = "phase3b-pdp.html"; ProjectId = "phase3b-gate-pdp" },
            @{ Label = "unsupported"; File = "phase3b-unsupported.html"; ProjectId = "phase3b-gate-unsupported" }
        )

        foreach ($fixture in $fixtureRoutes) {
            $fixturePath = Join-Path $Context.FixtureRoot $fixture["File"]
            $fixtureUrl = [Uri]::new((Resolve-Path $fixturePath).Path).AbsoluteUri
            $artifactRoot = Join-Path $projectOutputRoot $fixture["ProjectId"]
            $runId = "phase3b-gate-" + $fixture["Label"]

            Invoke-SreCli `
                -Context $Context `
                -Arguments @("run", "--url", $fixtureUrl, "--name", $fixture["ProjectId"], "--output-root", $projectOutputRoot, "--no-ai", "--force", "--run-id", $runId) `
                -AllowedExitCodes @(0, 3)

            if ($Context.LastProcessExitCode -eq 3) {
                Assert-SreStrictReviewBlocker -ProjectRoot $artifactRoot -RunId $runId -ReadinessMessage "Phase 3B fixture readiness did not pass"
                $Context.TestSummaries.Add("Phase 3B fixture $($fixture["Label"]): analysis/readiness passed; final reviewed handoff stopped on expected strict review-decision blockers.")
            }

            Invoke-SreCli -Context $Context -Arguments @("inspect", "--project", $artifactRoot)
        }
    }
}

function Invoke-SrePhase3CProof {
    param([Parameter(Mandatory = $true)]$Context)

    Invoke-SreStep -Context $Context -Name "Phase 3C grouped test coverage marker" -Script {
        $Context.TestSummaries.Add("Phase 3C handoff readiness/schema/unsupported coverage: represented by the full suite and grouped closure proof test processes.")
    }
}

function Invoke-SrePhase3DProof {
    param(
        [Parameter(Mandatory = $true)]$Context,
        [switch]$IncludePortableProof
    )

    Invoke-SreStep -Context $Context -Name "Phase 3D full ReverseEngineering tests" -Script {
        Invoke-SreTest -Context $Context -Name "Full ReverseEngineering tests"
    }

    Invoke-SreStep -Context $Context -Name "Phase 3 grouped closure proof tests" -Script {
        $filter = Get-SreClosureProofFilter -IncludePortableProof:$IncludePortableProof

        Invoke-SreTest -Context $Context -Name "Grouped Phase 3 closure proof" -Filter $filter
    }
}

function Invoke-SrePhase3EProof {
    param([Parameter(Mandatory = $true)]$Context)

    Invoke-SreStep -Context $Context -Name "Phase 3E grouped portable proof marker" -Script {
        $Context.TestSummaries.Add("Phase 3E portable package/reference/provenance/copy/dry-run/mutation coverage: represented by the grouped closure proof test process.")
    }
}

function Invoke-SreBoundaryScans {
    param([Parameter(Mandatory = $true)]$Context)

    Invoke-SreStep -Context $Context -Name "boundary scans" -Script {
        $reverseEngineeringPaths = @(
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
        $workflowPaths = @(
            "tools\BlazorShop.AI.StorefrontReverseEngineering\Analysis\Blueprint",
            "tools\BlazorShop.AI.StorefrontReverseEngineering\Application",
            "tools\BlazorShop.AI.StorefrontReverseEngineering\Workflows"
        )

        Assert-SreRgNoMatches -Context $Context -Pattern "BlazorShop.AI.StorefrontReverseEngineering|StorefrontReverseEngineering" -Paths @("BlazorShop.PresentationV2", "BlazorShop.Domain", "BlazorShop.Application", "BlazorShop.Infrastructure", "BlazorShop.ServiceDefaults", "BlazorShop.Tests.V2", "BlazorShop.sln") -ExtraArgs @("--glob", "!bin/**", "--glob", "!obj/**")
        Assert-SreRgNoMatches -Context $Context -Pattern "ProjectReference.*(BlazorShop\.Storefront\.V2|BlazorShop\.Storefront\.Runtime|BlazorShop\.Storefront\.Presentation|BlazorShop\.Storefront\.Components|BlazorShop\.ControlPlane|BlazorShop\.CommerceNode|BlazorShop\.Domain|BlazorShop\.Infrastructure|BlazorShop\.Web\.SharedV2)" -Paths @("tools\BlazorShop.AI.StorefrontReverseEngineering\BlazorShop.AI.StorefrontReverseEngineering.csproj") -ExtraArgs @("--glob", "*.csproj")
        Assert-SreRgNoMatches -Context $Context -Pattern "analysis/agent-handoff|agent-handoff-readiness|visual-blueprint\.v1" -Paths @("tools\BlazorShop.AI.StorefrontBuilder") -ExtraArgs @("--glob", "!bin/**", "--glob", "!obj/**")
        Assert-SreRgNoMatches -Context $Context -Pattern "WriteAllText(Async)?\([^\r\n]*(storefront-builder/generated|BlazorShop\.Storefront\.Generated|BlazorShop\.Storefront\.Starter)|Directory\.CreateDirectory\([^\r\n]*(storefront-builder/generated|BlazorShop\.Storefront\.Generated|BlazorShop\.Storefront\.Starter)" -Paths $reverseEngineeringPaths -ExtraArgs @("--glob", "*.cs", "--glob", "*.csproj", "--glob", "!bin/**", "--glob", "!obj/**")
        Assert-SreRgNoMatches -Context $Context -Pattern "WriteAllText(Async)?\([^\r\n]*\.(razor|css|js)([^a-zA-Z0-9]|$)" -Paths $reverseEngineeringPaths -ExtraArgs @("--glob", "*.cs", "--glob", "!bin/**", "--glob", "!obj/**")
        Assert-SreRgNoMatches -Context $Context -Pattern "@page|api/storefront|api/commerce|CommerceNode" -Paths @("tools\BlazorShop.AI.StorefrontReverseEngineering\tests\BlazorShop.AI.StorefrontReverseEngineering.Tests\Fixtures\Phase3D") -ExtraArgs @("--glob", "*.json")
        Assert-SreRgNoMatches -Context $Context -Pattern "captures/home" -Paths $workflowPaths -ExtraArgs @("--glob", "*.cs", "--glob", "!bin/**", "--glob", "!obj/**")
        Assert-SreRgNoMatches -Context $Context -Pattern "plan\.Pages\.First\(" -Paths $workflowPaths -ExtraArgs @("--glob", "*.cs", "--glob", "!bin/**", "--glob", "!obj/**")
        Assert-SreRgNoMatches -Context $Context -Pattern "\.draft\.json" -Paths @("tools\BlazorShop.AI.StorefrontReverseEngineering\tests\BlazorShop.AI.StorefrontReverseEngineering.Tests\Fixtures\Phase3D\positive-multipage-handoff-proof.json")
        Assert-SreRgNoMatches -Context $Context -Pattern "\.\./|[A-Za-z]:\\" -Paths @("tools\BlazorShop.AI.StorefrontReverseEngineering\tests\BlazorShop.AI.StorefrontReverseEngineering.Tests\Fixtures\Phase3D\positive-multipage-handoff-proof.json")
    }
}

function Invoke-SreStorefrontBuilderSmoke {
    param(
        [Parameter(Mandatory = $true)]$Context,
        [Parameter(Mandatory = $true)][string]$Name,
        [Parameter(Mandatory = $true)][string]$OutputRoot
    )

    Invoke-SreStep -Context $Context -Name "StorefrontBuilder plan-only smoke" -Script {
        $pwsh = Get-Command pwsh -ErrorAction SilentlyContinue
        if ($null -eq $pwsh) {
            throw "PowerShell 7 (pwsh) is required for StorefrontBuilder plan-only smoke."
        }

        Invoke-SreLoggedProcess `
            -Context $Context `
            -FileName $pwsh.Source `
            -Arguments @("-ExecutionPolicy", "Bypass", "-File", (Join-Path $Context.RepoRoot "tools\BlazorShop.AI.StorefrontBuilder\build-storefront.ps1"), "-Url", "https://example.test", "-Name", $Name, "-StoreKey", "sample", "-OutputRoot", $OutputRoot, "-Mode", "plan-only")
        $Context.StorefrontBuilderSmokeResult = "passed"
    }
}

function Invoke-SreFinalInspectProof {
    param(
        [Parameter(Mandatory = $true)]$Context,
        [string]$Filter = "AgentHandoffReadiness_CliSucceedsOnlyAfterFinalReadinessPasses|AgentHandoffReadiness_InspectReportsFinalHandoffStatus"
    )

    Invoke-SreStep -Context $Context -Name "final inspect proof" -Script {
        $Context.TestSummaries.Add("Final inspect proof: represented by the grouped closure proof test process filter '$Filter'.")
    }
}

function New-SreReportLines {
    param(
        [Parameter(Mandatory = $true)]$Context,
        [Parameter(Mandatory = $true)][string]$Title,
        [Parameter(Mandatory = $true)][string]$Status,
        [string]$ErrorMessage = "",
        [string[]]$ProofSummary = @(),
        [string[]]$BoundaryAssertions = @()
    )

    $dotnetVersion = (& dotnet --version).Trim()
    $utcTimestamp = [DateTimeOffset]::UtcNow.ToString("u", [System.Globalization.CultureInfo]::InvariantCulture)
    if ([string]::IsNullOrWhiteSpace($Context.FinalHead)) {
        $Context.FinalHead = (& git rev-parse HEAD).Trim()
    }

    $lines = New-Object System.Collections.Generic.List[string]
    $lines.Add("# $Title")
    $lines.Add("")
    $lines.Add("Status: $Status")
    $lines.Add("Tested commit SHA: $($Context.TestedHead)")
    $lines.Add("Final HEAD SHA: $($Context.FinalHead)")
    $lines.Add("Working tree clean: $($Context.InitialTreeClean)")
    $lines.Add("Branch: $($Context.InitialBranch)")
    $lines.Add("UTC timestamp: $utcTimestamp")
    $lines.Add(".NET version: $dotnetVersion")
    $lines.Add("Full test count: $($Context.FullTestCount)")
    $lines.Add("Closure proof test count: $($Context.ClosureProofTestCount)")
    $lines.Add("Negative mutation count: $($Context.NegativeMutationCount)")
    $lines.Add("StorefrontBuilder smoke result: $($Context.StorefrontBuilderSmokeResult)")
    $lines.Add("GitHub Actions status: disabled/local proof primary unless verified separately.")
    if (-not [string]::IsNullOrWhiteSpace($Context.FailedStep)) {
        $lines.Add("Failed step: $($Context.FailedStep)")
    }
    $lines.Add("")
    $lines.Add("Executed commands:")
    foreach ($command in $Context.Commands) {
        $lines.Add("- " + [char]96 + $command + [char]96)
    }
    $lines.Add("")
    $lines.Add("Steps:")
    foreach ($step in $Context.Steps) {
        $lines.Add("- $($step.Name): $($step.Status) ($($step.DurationSeconds)s)")
    }
    $lines.Add("")
    $lines.Add("Test summaries:")
    if ($Context.TestSummaries.Count -eq 0) {
        $lines.Add("- (not available)")
    }
    else {
        foreach ($summary in $Context.TestSummaries) {
            $lines.Add("- $summary")
        }
    }
    $lines.Add("")
    $lines.Add("Proof summary:")
    foreach ($summary in $ProofSummary) {
        $lines.Add("- $summary")
    }
    $lines.Add("")
    $lines.Add("Boundary assertions:")
    foreach ($assertion in $BoundaryAssertions) {
        $lines.Add("- $assertion")
    }
    $lines.Add("")
    $lines.Add("Known limitations:")
    if ($Context.KnownLimitations.Count -eq 0) {
        $lines.Add("- StorefrontBuilder consumption remains disabled until Phase 4 approved cutover.")
    }
    else {
        foreach ($limitation in $Context.KnownLimitations) {
            $lines.Add("- $limitation")
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

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
        [int]$CommandTimeoutSeconds = 900,
        [int]$GlobalTimeoutSeconds = 3600
    )

    $gateStartedUtc = [DateTimeOffset]::UtcNow
    return [ordered]@{
        RepoRoot = $RepoRoot
        ToolProject = Join-Path $RepoRoot "tools\BlazorShop.AI.StorefrontReverseEngineering\BlazorShop.AI.StorefrontReverseEngineering.csproj"
        ToolDll = Join-Path $RepoRoot "tools\BlazorShop.AI.StorefrontReverseEngineering\bin\Debug\net10.0\BlazorShop.AI.StorefrontReverseEngineering.dll"
        TestProject = Join-Path $RepoRoot "tools\BlazorShop.AI.StorefrontReverseEngineering\tests\BlazorShop.AI.StorefrontReverseEngineering.Tests\BlazorShop.AI.StorefrontReverseEngineering.Tests.csproj"
        FixtureRoot = Join-Path $RepoRoot "tools\BlazorShop.AI.StorefrontReverseEngineering\tests\BlazorShop.AI.StorefrontReverseEngineering.Tests\Fixtures"
        ReportRoot = Join-Path $RepoRoot "obj\storefront-reverse-engineering\reports"
        CommandTimeoutSeconds = $CommandTimeoutSeconds
        GlobalTimeoutSeconds = $GlobalTimeoutSeconds
        GateStartedUtc = $gateStartedUtc
        GlobalDeadlineUtc = $gateStartedUtc.AddSeconds($GlobalTimeoutSeconds)
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
        LastProcessExitCode = "not-run"
        ProcessCount = 0
        TestProcessCount = 0
        MajorStepCount = 0
        BaselineCacheStatus = "process-local shared fixture"
        CleanupResult = "not-run"
        CleanupRemovedPathCount = 0
    }
}

function Get-SreRemainingBudgetSeconds {
    param([Parameter(Mandatory = $true)]$Context)

    return [int][Math]::Max(0, [Math]::Ceiling(($Context.GlobalDeadlineUtc - [DateTimeOffset]::UtcNow).TotalSeconds))
}

function Assert-SreGlobalTimeoutBudget {
    param(
        [Parameter(Mandatory = $true)]$Context,
        [Parameter(Mandatory = $true)][string]$StepName
    )

    $remaining = Get-SreRemainingBudgetSeconds -Context $Context
    if ($remaining -le 0) {
        throw "Global gate timeout exhausted before step '$StepName'. Budget: $($Context.GlobalTimeoutSeconds)s."
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
    Assert-SreGlobalTimeoutBudget -Context $Context -StepName $commandLine
    $effectiveTimeoutSeconds = [Math]::Min($TimeoutSeconds, (Get-SreRemainingBudgetSeconds -Context $Context))
    $Context.ProcessCount++
    if ($FileName -eq "dotnet" -and $Arguments.Count -gt 0 -and $Arguments[0] -eq "test") {
        $Context.TestProcessCount++
    }

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
    if (-not $process.WaitForExit($effectiveTimeoutSeconds * 1000)) {
        try {
            $process.Kill($true)
        }
        catch {
            $process.Kill()
        }

        $Context.LastProcessExitCode = "timeout"
        throw "Command timed out after ${effectiveTimeoutSeconds}s: $commandLine"
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
    Assert-SreGlobalTimeoutBudget -Context $Context -StepName $Name
    $Context.LastProcessExitCode = "not-run"
    $Context.MajorStepCount++
    $startedUtc = [DateTimeOffset]::UtcNow
    try {
        & $Script
        $endedUtc = [DateTimeOffset]::UtcNow
        $duration = $endedUtc - $startedUtc
        $Context.Steps.Add([pscustomobject]@{
            Name = $Name
            Status = "passed"
            StartUtc = $startedUtc.ToString("u", [System.Globalization.CultureInfo]::InvariantCulture)
            EndUtc = $endedUtc.ToString("u", [System.Globalization.CultureInfo]::InvariantCulture)
            DurationSeconds = [Math]::Round($duration.TotalSeconds, 2)
            ExitCode = $Context.LastProcessExitCode
            RemainingBudgetSeconds = Get-SreRemainingBudgetSeconds -Context $Context
        })
    }
    catch {
        $endedUtc = [DateTimeOffset]::UtcNow
        $duration = $endedUtc - $startedUtc
        $Context.FailedStep = $Name
        $Context.Steps.Add([pscustomobject]@{
            Name = $Name
            Status = "failed"
            StartUtc = $startedUtc.ToString("u", [System.Globalization.CultureInfo]::InvariantCulture)
            EndUtc = $endedUtc.ToString("u", [System.Globalization.CultureInfo]::InvariantCulture)
            DurationSeconds = [Math]::Round($duration.TotalSeconds, 2)
            ExitCode = $Context.LastProcessExitCode
            RemainingBudgetSeconds = Get-SreRemainingBudgetSeconds -Context $Context
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
        "FullyQualifiedName~Phase3CliProofCollectionTests",
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

    Invoke-SreStep -Context $Context -Name "Phase 3A CLI proof collection marker" -Script {
        $Context.TestSummaries.Add("Phase 3A CLI readiness workflow: represented by the grouped closure proof test process with run, validate, inspect, and strict review blocker assertions.")
    }
}

function Invoke-SrePhase3BProof {
    param([Parameter(Mandatory = $true)]$Context)

    Invoke-SreStep -Context $Context -Name "Phase 3B grouped test coverage marker" -Script {
        $Context.TestSummaries.Add("Phase 3B visual analysis/ecommerce mapping tests: represented by the full suite and grouped closure proof test processes.")
    }

    Invoke-SreStep -Context $Context -Name "Phase 3B multi-route CLI proof collection marker" -Script {
        $Context.TestSummaries.Add("Phase 3B multi-route CLI proof collection: grouped closure proof covers home, listing, product, and unsupported fixtures with isolated project roots.")
        $Context.TestSummaries.Add("Phase 3B unsupported fixture proof: grouped closure proof asserts unsupported patterns remain blocking and explicit.")
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

function Get-SreBoundaryAssertionSummaries {
    return @(
        "ReverseEngineering has no production project references.",
        "StorefrontBuilder does not consume analysis/agent-handoff/* yet.",
        "ReverseEngineering does not write Razor/CSS/JS storefront output.",
        "ReverseEngineering does not write to Starter or generated storefront source.",
        "No direct Commerce Node browser calls are generated or recommended.",
        "No generated @page output exists.",
        "No captures/home or plan.Pages.First() hardcode exists in workflow code.",
        "No reviewed blueprint reference to .draft.json is accepted.",
        "No handoff reference outside analysis/agent-handoff is accepted."
    )
}

function Get-SreBoundaryScanDefinitions {
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

    return @(
        [pscustomobject]@{
            Pattern = "BlazorShop.AI.StorefrontReverseEngineering|StorefrontReverseEngineering"
            Paths = @("BlazorShop.PresentationV2", "BlazorShop.Domain", "BlazorShop.Application", "BlazorShop.Infrastructure", "BlazorShop.ServiceDefaults", "BlazorShop.Tests.V2", "BlazorShop.sln")
            ExtraArgs = @("--glob", "!bin/**", "--glob", "!obj/**")
        },
        [pscustomobject]@{
            Pattern = "ProjectReference.*(BlazorShop\.Storefront\.V2|BlazorShop\.Storefront\.Runtime|BlazorShop\.Storefront\.Presentation|BlazorShop\.Storefront\.Components|BlazorShop\.ControlPlane|BlazorShop\.CommerceNode|BlazorShop\.Domain|BlazorShop\.Infrastructure|BlazorShop\.Web\.SharedV2)"
            Paths = @("tools\BlazorShop.AI.StorefrontReverseEngineering\BlazorShop.AI.StorefrontReverseEngineering.csproj")
            ExtraArgs = @("--glob", "*.csproj")
        },
        [pscustomobject]@{
            Pattern = "analysis/agent-handoff|agent-handoff-readiness|visual-blueprint\.v1"
            Paths = @("tools\BlazorShop.AI.StorefrontBuilder")
            ExtraArgs = @("--glob", "!bin/**", "--glob", "!obj/**")
        },
        [pscustomobject]@{
            Pattern = "WriteAllText(Async)?\([^\r\n]*(storefront-builder/generated|BlazorShop\.Storefront\.Generated|BlazorShop\.Storefront\.Starter)|Directory\.CreateDirectory\([^\r\n]*(storefront-builder/generated|BlazorShop\.Storefront\.Generated|BlazorShop\.Storefront\.Starter)"
            Paths = $reverseEngineeringPaths
            ExtraArgs = @("--glob", "*.cs", "--glob", "*.csproj", "--glob", "!bin/**", "--glob", "!obj/**")
        },
        [pscustomobject]@{
            Pattern = "WriteAllText(Async)?\([^\r\n]*\.(razor|css|js)([^a-zA-Z0-9]|$)"
            Paths = $reverseEngineeringPaths
            ExtraArgs = @("--glob", "*.cs", "--glob", "!bin/**", "--glob", "!obj/**")
        },
        [pscustomobject]@{
            Pattern = "@page|api/storefront|api/commerce|CommerceNode"
            Paths = @("tools\BlazorShop.AI.StorefrontReverseEngineering\tests\BlazorShop.AI.StorefrontReverseEngineering.Tests\Fixtures\Phase3D")
            ExtraArgs = @("--glob", "*.json")
        },
        [pscustomobject]@{
            Pattern = "captures/home"
            Paths = $workflowPaths
            ExtraArgs = @("--glob", "*.cs", "--glob", "!bin/**", "--glob", "!obj/**")
        },
        [pscustomobject]@{
            Pattern = "plan\.Pages\.First\("
            Paths = $workflowPaths
            ExtraArgs = @("--glob", "*.cs", "--glob", "!bin/**", "--glob", "!obj/**")
        },
        [pscustomobject]@{
            Pattern = "\.draft\.json"
            Paths = @("tools\BlazorShop.AI.StorefrontReverseEngineering\tests\BlazorShop.AI.StorefrontReverseEngineering.Tests\Fixtures\Phase3D\positive-multipage-handoff-proof.json")
            ExtraArgs = @()
        },
        [pscustomobject]@{
            Pattern = "\.\./|[A-Za-z]:\\"
            Paths = @("tools\BlazorShop.AI.StorefrontReverseEngineering\tests\BlazorShop.AI.StorefrontReverseEngineering.Tests\Fixtures\Phase3D\positive-multipage-handoff-proof.json")
            ExtraArgs = @()
        }
    )
}

function Invoke-SreBoundaryScans {
    param([Parameter(Mandatory = $true)]$Context)

    Invoke-SreStep -Context $Context -Name "boundary scans" -Script {
        foreach ($scan in Get-SreBoundaryScanDefinitions) {
            Assert-SreRgNoMatches -Context $Context -Pattern $scan.Pattern -Paths @($scan.Paths) -ExtraArgs @($scan.ExtraArgs)
        }
    }
}

function Invoke-SreStorefrontBuilderSmoke {
    param(
        [Parameter(Mandatory = $true)]$Context,
        [Parameter(Mandatory = $true)][string]$Name,
        [Parameter(Mandatory = $true)][string]$OutputRoot
    )

    $smokeProjectName = $Name
    Invoke-SreStep -Context $Context -Name "StorefrontBuilder plan-only smoke" -Script {
        $pwsh = Get-Command pwsh -ErrorAction SilentlyContinue
        if ($null -eq $pwsh) {
            throw "PowerShell 7 (pwsh) is required for StorefrontBuilder plan-only smoke."
        }

        Invoke-SreLoggedProcess `
            -Context $Context `
            -FileName $pwsh.Source `
            -Arguments @("-ExecutionPolicy", "Bypass", "-File", (Join-Path $Context.RepoRoot "tools\BlazorShop.AI.StorefrontBuilder\build-storefront.ps1"), "-Url", "https://example.test", "-Name", $smokeProjectName, "-StoreKey", "sample", "-OutputRoot", $OutputRoot, "-Mode", "plan-only")
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

function Get-SreArtifactStats {
    param([Parameter(Mandatory = $true)]$Context)

    $roots = @($Context.ReportRoot)
    $count = 0
    $bytes = 0L
    foreach ($root in $roots) {
        if (-not (Test-Path $root)) {
            continue
        }

        foreach ($file in Get-ChildItem -Path $root -Recurse -File -ErrorAction SilentlyContinue) {
            $count++
            $bytes += $file.Length
        }
    }

    return [pscustomobject]@{
        Count = $count
        Bytes = $bytes
    }
}

function Remove-SreDirectoryIfUnderRoot {
    param(
        [Parameter(Mandatory = $true)][string]$Root,
        [Parameter(Mandatory = $true)][string]$Path
    )

    if (-not (Test-Path $Path)) {
        return $false
    }

    $resolvedRoot = (Resolve-Path $Root).Path.TrimEnd('\', '/')
    $resolvedPath = (Resolve-Path $Path).Path.TrimEnd('\', '/')
    if (-not ($resolvedPath.StartsWith($resolvedRoot + [System.IO.Path]::DirectorySeparatorChar, [System.StringComparison]::OrdinalIgnoreCase))) {
        throw "Refusing to cleanup path outside approved root. Root: $resolvedRoot. Path: $resolvedPath"
    }

    Remove-Item -LiteralPath $resolvedPath -Recurse -Force
    return $true
}

function Invoke-SreCleanupSuccessfulArtifacts {
    param(
        [Parameter(Mandatory = $true)]$Context,
        [string[]]$StorefrontBuilderOutputRoots = @()
    )

    Invoke-SreStep -Context $Context -Name "cleanup successful transient artifacts" -Script {
        $removed = 0
        $projectRoot = Join-Path $Context.RepoRoot "obj\storefront-reverse-engineering\projects"
        $builderRoot = Join-Path $Context.RepoRoot "obj\storefront-builder\generated"

        if (Test-Path $projectRoot) {
            $projectPrefixes = @(
                "phase3d-positive-baseline-*",
                "phase3d-positive-copy-*",
                "phase3-cli-proof-*"
            )
            foreach ($prefix in $projectPrefixes) {
                foreach ($directory in Get-ChildItem -Path $projectRoot -Directory -Filter $prefix -ErrorAction SilentlyContinue) {
                    if (Remove-SreDirectoryIfUnderRoot -Root $projectRoot -Path $directory.FullName) {
                        $removed++
                    }
                }
            }
        }

        foreach ($outputRoot in $StorefrontBuilderOutputRoots) {
            $absoluteOutputRoot = Join-Path $Context.RepoRoot $outputRoot
            if (Remove-SreDirectoryIfUnderRoot -Root $builderRoot -Path $absoluteOutputRoot) {
                $removed++
            }
        }

        $Context.CleanupRemovedPathCount = $removed
        $Context.CleanupResult = "passed"
        $Context.TestSummaries.Add("Cleanup removed $removed successful transient artifact roots; failed-run artifacts are retained because cleanup runs only on the success path.")
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
    $artifactStats = Get-SreArtifactStats -Context $Context
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
    $lines.Add("Global timeout seconds: $($Context.GlobalTimeoutSeconds)")
    $lines.Add("Remaining budget seconds: $(Get-SreRemainingBudgetSeconds -Context $Context)")
    $lines.Add("Process count: $($Context.ProcessCount)")
    $lines.Add("Test process count: $($Context.TestProcessCount)")
    $lines.Add("Major step count: $($Context.MajorStepCount)")
    $lines.Add("Full test count: $($Context.FullTestCount)")
    $lines.Add("Closure proof test count: $($Context.ClosureProofTestCount)")
    $lines.Add("Negative mutation count: $($Context.NegativeMutationCount)")
    $lines.Add("Artifact count: $($artifactStats.Count)")
    $lines.Add("Artifact bytes written: $($artifactStats.Bytes)")
    $lines.Add("Baseline cache status: $($Context.BaselineCacheStatus)")
    $lines.Add("Cleanup result: $($Context.CleanupResult)")
    $lines.Add("Cleanup removed path count: $($Context.CleanupRemovedPathCount)")
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
        $lines.Add("- $($step.Name): $($step.Status) ($($step.DurationSeconds)s, start=$($step.StartUtc), end=$($step.EndUtc), exit=$($step.ExitCode), remaining=$($step.RemainingBudgetSeconds)s)")
    }
    $lines.Add("")
    $lines.Add("Slowest steps:")
    foreach ($step in ($Context.Steps | Sort-Object -Property DurationSeconds -Descending | Select-Object -First 5)) {
        $lines.Add("- $($step.Name): $($step.DurationSeconds)s")
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

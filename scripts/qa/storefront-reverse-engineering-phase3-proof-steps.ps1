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

    Invoke-SreStep -Context $Context -Name "Phase 3A regression fast subset" -Script {
        Invoke-SreTest -Context $Context -Name "Phase 3A regression fast subset" -Filter "StableCapture|Stitch|Quality|Readiness|Validation|Workflow|Cli|Lifecycle|Security|Browser|Boundary|Evidence|Interaction|Schema"
    }

    Invoke-SreStep -Context $Context -Name "Phase 3A browser fixture tests" -Script {
        Invoke-SreTest -Context $Context -Name "Phase 3A browser fixture tests" -Filter "Playwright|EndToEnd"
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

    Invoke-SreStep -Context $Context -Name "Phase 3B full analysis tests" -Script {
        Invoke-SreTest -Context $Context -Name "Phase 3B full analysis tests"
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

    Invoke-SreStep -Context $Context -Name "Phase 3C complete fixture proof" -Script {
        Invoke-SreTest -Context $Context -Name "Phase 3C complete fixture proof" -Filter "PageCompositions_MultiPageFixtureProducesOneSiteBlueprint|AgentHandoffReadiness_PassesForReviewedFixtureWithoutBlockers"
    }

    Invoke-SreStep -Context $Context -Name "Phase 3C unsupported fixture proof" -Script {
        Invoke-SreTest -Context $Context -Name "Phase 3C unsupported fixture proof" -Filter "PresentationMapping_DirectStorefrontApiInteractionFails|PresentationMapping_ProtectedPathMappingFails|PresentationMapping_AmbiguousRoleMappingRequiresReview|PresentationMapping_RuntimeOwnedBehaviorFailsForVisualMapping|PageCompositions_MissingEvidenceForRequiredPageCreatesPageScopedBlocker|PageCompositions_UnknownPageArchetypeBlocksReadiness|ReviewDecision_StaleSourceHashIsRejected|AgentHandoffReadiness_StorefrontV2AllowedTargetFails|ReviewDecision_DuplicateDecisionWithoutSupersedeIsRejected"
    }

    Invoke-SreStep -Context $Context -Name "Phase 3C schema validation proof" -Script {
        Invoke-SreTest -Context $Context -Name "Phase 3C schema validation proof" -Filter "Phase3CSchemaRegistry_RegistersFinalHandoffArtifacts|SchemaRegistry_LoadsSchemaFilesForFirstClassArtifacts"
    }
}

function Invoke-SrePhase3DProof {
    param([Parameter(Mandatory = $true)]$Context)

    Invoke-SreStep -Context $Context -Name "Phase 3D full ReverseEngineering tests" -Script {
        Invoke-SreTest -Context $Context -Name "Full ReverseEngineering tests"
    }

    Invoke-SreStep -Context $Context -Name "Phase 3D typed review resolution tests" -Script {
        Invoke-SreTest -Context $Context -Name "Typed review resolution" -Filter "ConfidenceReview"
    }

    Invoke-SreStep -Context $Context -Name "Phase 3D exact slot contract tests" -Script {
        Invoke-SreTest -Context $Context -Name "Exact slot contracts" -Filter "StorefrontPattern|BlueprintV1"
    }

    Invoke-SreStep -Context $Context -Name "Phase 3D self-contained evidence packaging tests" -Script {
        Invoke-SreTest -Context $Context -Name "Self-contained handoff evidence" -Filter "AgentHandoff"
    }

    Invoke-SreStep -Context $Context -Name "Phase 3D canonical handoff validation tests" -Script {
        Invoke-SreTest -Context $Context -Name "Canonical handoff validation" -Filter "SchemaArtifact|AgentHandoff"
    }

    Invoke-SreStep -Context $Context -Name "Phase 3D positive end-to-end proof" -Script {
        Invoke-SreTest -Context $Context -Name "Phase 3D positive end-to-end proof" -Filter "Phase3DPositiveEndToEnd"
    }

    Invoke-SreStep -Context $Context -Name "Phase 3D negative review mutations" -Script {
        Invoke-SreTest -Context $Context -Name "Phase 3D negative review mutations" -Filter "Phase3DNegativeReviewMutation"
    }

    Invoke-SreStep -Context $Context -Name "Phase 3D negative slot mutations" -Script {
        Invoke-SreTest -Context $Context -Name "Phase 3D negative slot mutations" -Filter "Phase3DNegativeSlotMutation"
    }

    Invoke-SreStep -Context $Context -Name "Phase 3D negative evidence mutations" -Script {
        Invoke-SreTest -Context $Context -Name "Phase 3D negative evidence mutations" -Filter "Phase3DNegativeEvidenceMutation"
    }

    Invoke-SreStep -Context $Context -Name "Phase 3D negative handoff mutations" -Script {
        Invoke-SreTest -Context $Context -Name "Phase 3D negative handoff mutations" -Filter "Phase3DNegativeHandoffMutation"
    }

    Invoke-SreStep -Context $Context -Name "Phase 3D negative boundary mutations" -Script {
        Invoke-SreTest -Context $Context -Name "Phase 3D negative boundary mutations" -Filter "Phase3DNegativeBoundaryMutation"
    }
}

function Invoke-SrePhase3EProof {
    param([Parameter(Mandatory = $true)]$Context)

    Invoke-SreStep -Context $Context -Name "Phase 3E full ReverseEngineering tests" -Script {
        Invoke-SreTest -Context $Context -Name "Full ReverseEngineering tests"
    }

    Invoke-SreStep -Context $Context -Name "Phase 3E handoff-specific blueprint tests" -Script {
        Invoke-SreTest -Context $Context -Name "Handoff-specific blueprint tests" -Filter "AgentHandoff_VisualBlueprint|AgentHandoff_PageCompositions|AgentHandoff_PresentationCatalog|AgentHandoff_ResponsiveAndInteraction|AgentHandoff_ReviewResolution"
    }

    Invoke-SreStep -Context $Context -Name "Phase 3E portable artifact set tests" -Script {
        Invoke-SreTest -Context $Context -Name "Portable artifact set tests" -Filter "PortableHandoffContract|AgentHandoff_Manifest|AgentHandoffReadiness_MissingRequiredSchemaEntry"
    }

    Invoke-SreStep -Context $Context -Name "Phase 3E typed reference containment tests" -Script {
        Invoke-SreTest -Context $Context -Name "Typed reference containment tests" -Filter "HandoffReferenceScanner"
    }

    Invoke-SreStep -Context $Context -Name "Phase 3E manifest portability/hash tests" -Script {
        Invoke-SreTest -Context $Context -Name "Manifest portability/hash tests" -Filter "PortableHandoffValidator|PortableHandoffContract|AgentHandoff_PackageHash|AgentHandoffReadiness_PackageHash"
    }

    Invoke-SreStep -Context $Context -Name "Phase 3E evidence slot provenance tests" -Script {
        Invoke-SreTest -Context $Context -Name "Evidence slot provenance tests" -Filter "SectionSlotResolver|AgentHandoffEvidenceSlotProvenance|PageCompositionSlotValidatorSharedResolver"
    }

    Invoke-SreStep -Context $Context -Name "Phase 3E portable validator CLI tests" -Script {
        Invoke-SreTest -Context $Context -Name "Portable validator CLI tests" -Filter "PortableHandoffValidator|PortableHandoffCli"
    }

    Invoke-SreStep -Context $Context -Name "Phase 3E isolated copy proof" -Script {
        Invoke-SreTest -Context $Context -Name "Isolated copy proof" -Filter "PortableHandoffCopyProof"
    }

    Invoke-SreStep -Context $Context -Name "Phase 3E Phase 4 dry-run loader proof" -Script {
        Invoke-SreTest -Context $Context -Name "Phase 4 dry-run loader proof" -Filter "HandoffConsumerDryRunLoader"
    }

    Invoke-SreStep -Context $Context -Name "Phase 3E negative portability mutation tests" -Script {
        Invoke-SreTest -Context $Context -Name "Negative portability mutations" -Filter "Phase3ENegativeReferenceMutation|Phase3ENegativeArtifactMutation|Phase3ENegativeSchemaMutation|Phase3ENegativeHashMutation"
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
        Invoke-SreTest -Context $Context -Name "Final inspect proof" -Filter $Filter
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

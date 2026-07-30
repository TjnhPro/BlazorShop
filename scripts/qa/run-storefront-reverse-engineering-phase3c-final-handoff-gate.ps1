param(
    [switch]$SkipPhase3BGate,
    [switch]$SkipStorefrontBuilderSmoke,
    [int]$CommandTimeoutSeconds = 900
)

$ErrorActionPreference = "Stop"

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
$toolProject = Join-Path $repoRoot "tools\BlazorShop.AI.StorefrontReverseEngineering\BlazorShop.AI.StorefrontReverseEngineering.csproj"
$testProject = Join-Path $repoRoot "tools\BlazorShop.AI.StorefrontReverseEngineering\tests\BlazorShop.AI.StorefrontReverseEngineering.Tests\BlazorShop.AI.StorefrontReverseEngineering.Tests.csproj"
$reportRoot = Join-Path $repoRoot "obj\storefront-reverse-engineering\reports"
$commands = New-Object System.Collections.Generic.List[string]
$steps = New-Object System.Collections.Generic.List[object]
$testSummaries = New-Object System.Collections.Generic.List[string]
$failedStep = $null

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
        [int]$TimeoutSeconds = $CommandTimeoutSeconds
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

    if ($process.ExitCode -ne 0) {
        throw "Command failed with exit code $($process.ExitCode): $commandLine"
    }
}

function Invoke-Step {
    param(
        [Parameter(Mandatory = $true)][string]$Name,
        [Parameter(Mandatory = $true)][scriptblock]$Script
    )

    Write-Host "== $Name =="
    $startedUtc = [DateTimeOffset]::UtcNow
    try {
        & $Script
        $duration = [DateTimeOffset]::UtcNow - $startedUtc
        $steps.Add([pscustomobject]@{
            Name = $Name
            Status = "passed"
            DurationSeconds = [Math]::Round($duration.TotalSeconds, 2)
        })
    }
    catch {
        $duration = [DateTimeOffset]::UtcNow - $startedUtc
        $script:failedStep = $Name
        $steps.Add([pscustomobject]@{
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

function New-ReportLines {
    param(
        [Parameter(Mandatory = $true)][string]$Status,
        [string]$ErrorMessage = ""
    )

    $gitCommit = (& git rev-parse HEAD).Trim()
    $gitBranch = (& git branch --show-current).Trim()
    $dotnetVersion = (& dotnet --version).Trim()
    $utcTimestamp = [DateTimeOffset]::UtcNow.ToString("u", [System.Globalization.CultureInfo]::InvariantCulture)

    $lines = New-Object System.Collections.Generic.List[string]
    $lines.Add("# Storefront Reverse Engineering Phase 3C Final Handoff Gate")
    $lines.Add("")
    $lines.Add("Status: $Status")
    $lines.Add("Commit SHA: $gitCommit")
    $lines.Add("Branch: $gitBranch")
    $lines.Add("UTC timestamp: $utcTimestamp")
    $lines.Add(".NET version: $dotnetVersion")
    if (-not [string]::IsNullOrWhiteSpace($failedStep)) {
        $lines.Add("Failed step: $failedStep")
    }
    $lines.Add("")
    $lines.Add("Executed commands:")
    foreach ($command in $commands) {
        $lines.Add("- " + [char]96 + $command + [char]96)
    }
    $lines.Add("")
    $lines.Add("Steps:")
    foreach ($step in $steps) {
        $lines.Add("- $($step.Name): $($step.Status) ($($step.DurationSeconds)s)")
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
    $lines.Add("Phase 3C boundary assertions:")
    $lines.Add("- StorefrontBuilder does not consume Phase 3C agent handoff artifacts.")
    $lines.Add("- Production projects do not reference ReverseEngineering.")
    $lines.Add("- ReverseEngineering does not reference production Storefront runtime/API projects.")
    $lines.Add("- ReverseEngineering does not write generated storefront source or Starter source.")
    $lines.Add("- Workflow code does not contain hardcoded captures/home or plan.Pages.First() single-page assumptions.")
    $lines.Add("")
    $lines.Add("Artifact paths:")
    $lines.Add("- analysis/storefront-pattern/storefront-pattern.json")
    $lines.Add("- analysis/resolved/page-compositions.reviewed.json")
    $lines.Add("- analysis/agent-handoff/manifest.json")
    $lines.Add("- analysis/agent-handoff/handoff-readiness.json")
    $lines.Add("- obj/storefront-reverse-engineering/reports/")
    $lines.Add("")
    $lines.Add("Next action: keep StorefrontBuilder consumption disabled until a separate approved Phase 4 plan consumes analysis/agent-handoff/*.")
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

    Invoke-Step "build ReverseEngineering tool" {
        Invoke-LoggedProcess -FileName "dotnet" -Arguments @("build", $toolProject)
    }

    Invoke-Step "run ReverseEngineering tests" {
        Invoke-LoggedProcess `
            -FileName "dotnet" `
            -Arguments @("test", $testProject, "--blame-hang", "--blame-hang-timeout", "5m") `
            -SummaryName "ReverseEngineering tests"
    }

    Invoke-Step "fixture run for complete multi-page handoff" {
        Invoke-LoggedProcess `
            -FileName "dotnet" `
            -Arguments @("test", $testProject, "--no-restore", "--filter", "PageCompositions_MultiPageFixtureProducesOneSiteBlueprint|AgentHandoffReadiness_PassesForReviewedFixtureWithoutBlockers", "--blame-hang", "--blame-hang-timeout", "5m") `
            -SummaryName "Phase 3C complete fixture"
    }

    Invoke-Step "fixture run for unsupported pattern blockers" {
        Invoke-LoggedProcess `
            -FileName "dotnet" `
            -Arguments @("test", $testProject, "--no-restore", "--filter", "PresentationMapping_DirectStorefrontApiInteractionFails|PresentationMapping_ProtectedPathMappingFails|PresentationMapping_AmbiguousRoleMappingRequiresReview|PresentationMapping_RuntimeOwnedBehaviorFailsForVisualMapping|PageCompositions_MissingEvidenceForRequiredPageCreatesPageScopedBlocker|PageCompositions_UnknownPageArchetypeBlocksReadiness|ReviewDecision_StaleSourceHashIsRejected|AgentHandoffReadiness_StorefrontV2AllowedTargetFails|ReviewDecision_DuplicateDecisionWithoutSupersedeIsRejected", "--blame-hang", "--blame-hang-timeout", "5m") `
            -SummaryName "Phase 3C unsupported fixtures"
    }

    Invoke-Step "schema validation for Phase 3C artifacts" {
        Invoke-LoggedProcess `
            -FileName "dotnet" `
            -Arguments @("test", $testProject, "--no-restore", "--filter", "Phase3CSchemaRegistry_RegistersFinalHandoffArtifacts|SchemaRegistry_LoadsSchemaFilesForFirstClassArtifacts", "--blame-hang", "--blame-hang-timeout", "5m") `
            -SummaryName "Phase 3C schema validation"
    }

    if (-not $SkipPhase3BGate) {
        Invoke-Step "run Phase 3B baseline gate" {
            $phase3BArgs = @("-ExecutionPolicy", "Bypass", "-File", (Join-Path $repoRoot "scripts\qa\run-storefront-reverse-engineering-phase3b-gate.ps1"))
            if ($SkipStorefrontBuilderSmoke) {
                $phase3BArgs += "-SkipStorefrontBuilderSmoke"
            }

            Invoke-LoggedProcess `
                -FileName "powershell" `
                -Arguments $phase3BArgs
        }
    }

    Invoke-Step "boundary scan" {
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
        $workflowCodePaths = @(
            "tools\BlazorShop.AI.StorefrontReverseEngineering\Analysis\Blueprint",
            "tools\BlazorShop.AI.StorefrontReverseEngineering\Application",
            "tools\BlazorShop.AI.StorefrontReverseEngineering\Workflows"
        )

        Assert-RgNoMatches `
            -Pattern "BlazorShop.AI.StorefrontReverseEngineering|StorefrontReverseEngineering" `
            -Paths @("BlazorShop.PresentationV2", "BlazorShop.Domain", "BlazorShop.Application", "BlazorShop.Infrastructure", "BlazorShop.ServiceDefaults", "BlazorShop.Tests.V2", "BlazorShop.sln") `
            -ExtraArgs @("--glob", "!bin/**", "--glob", "!obj/**")

        Assert-RgNoMatches `
            -Pattern "ProjectReference.*(BlazorShop\.Storefront\.V2|BlazorShop\.Storefront\.Runtime|BlazorShop\.Storefront\.Presentation|BlazorShop\.Storefront\.Components|BlazorShop\.ControlPlane|BlazorShop\.CommerceNode|BlazorShop\.Domain|BlazorShop\.Infrastructure|BlazorShop\.Web\.SharedV2)" `
            -Paths @("tools\BlazorShop.AI.StorefrontReverseEngineering\BlazorShop.AI.StorefrontReverseEngineering.csproj") `
            -ExtraArgs @("--glob", "*.csproj")

        Assert-RgNoMatches `
            -Pattern "analysis/agent-handoff|agent-handoff-readiness|visual-blueprint\.v1" `
            -Paths @("tools\BlazorShop.AI.StorefrontBuilder") `
            -ExtraArgs @("--glob", "!bin/**", "--glob", "!obj/**")

        Assert-RgNoMatches `
            -Pattern "WriteAllText(Async)?\([^\r\n]*(storefront-builder/generated|BlazorShop\.Storefront\.Generated|BlazorShop\.Storefront\.Starter)|Directory\.CreateDirectory\([^\r\n]*(storefront-builder/generated|BlazorShop\.Storefront\.Generated|BlazorShop\.Storefront\.Starter)" `
            -Paths $reverseEngineeringProductionPaths `
            -ExtraArgs @("--glob", "*.cs", "--glob", "*.csproj", "--glob", "!bin/**", "--glob", "!obj/**")

        Assert-RgNoMatches `
            -Pattern "WriteAllText(Async)?\([^\r\n]*\.(razor|css|js)([^a-zA-Z0-9]|$)" `
            -Paths $reverseEngineeringProductionPaths `
            -ExtraArgs @("--glob", "*.cs", "--glob", "!bin/**", "--glob", "!obj/**")

        Assert-RgNoMatches `
            -Pattern "captures/home" `
            -Paths $workflowCodePaths `
            -ExtraArgs @("--glob", "*.cs", "--glob", "!bin/**", "--glob", "!obj/**")

        Assert-RgNoMatches `
            -Pattern "plan\.Pages\.First\(" `
            -Paths $workflowCodePaths `
            -ExtraArgs @("--glob", "*.cs", "--glob", "!bin/**", "--glob", "!obj/**")
    }

    $reportPath = Join-Path $reportRoot ("phase3c-final-handoff-gate-" + (Get-Date -Format "yyyyMMddHHmmss") + ".md")
    New-ReportLines -Status "passed" | Set-Content -Path $reportPath -Encoding UTF8
    Write-Host "Gate passed. Report: $reportPath"
}
catch {
    $reportPath = Join-Path $reportRoot ("phase3c-final-handoff-gate-failed-" + (Get-Date -Format "yyyyMMddHHmmss") + ".md")
    New-ReportLines -Status "failed" -ErrorMessage $_.Exception.Message | Set-Content -Path $reportPath -Encoding UTF8
    Write-Error "Gate failed. Report: $reportPath. Error: $($_.Exception.Message)"
    exit 1
}

param(
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
$knownLimitations = New-Object System.Collections.Generic.List[string]
$failedStep = $null
$testedHead = $null
$finalHead = $null
$initialBranch = $null
$initialTreeClean = $false

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

function Assert-CleanWorkingTree {
    $status = (& git status --porcelain)
    if ($status.Count -gt 0) {
        throw "Working tree is dirty. Resolve local changes before running final Phase 3D closure gate:`n$($status -join [Environment]::NewLine)"
    }
}

function Assert-HeadUnchanged {
    $script:finalHead = (& git rev-parse HEAD).Trim()
    if ($script:finalHead -ne $script:testedHead) {
        throw "HEAD changed during gate. Tested '$script:testedHead' but final HEAD is '$script:finalHead'."
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

function Invoke-TestFilter {
    param(
        [Parameter(Mandatory = $true)][string]$Name,
        [Parameter(Mandatory = $true)][string]$Filter
    )

    Invoke-LoggedProcess `
        -FileName "dotnet" `
        -Arguments @("test", $testProject, "--no-restore", "--filter", $Filter, "--blame-hang", "--blame-hang-timeout", "5m") `
        -SummaryName $Name
}

function New-ReportLines {
    param(
        [Parameter(Mandatory = $true)][string]$Status,
        [string]$ErrorMessage = ""
    )

    $dotnetVersion = (& dotnet --version).Trim()
    $utcTimestamp = [DateTimeOffset]::UtcNow.ToString("u", [System.Globalization.CultureInfo]::InvariantCulture)
    if ([string]::IsNullOrWhiteSpace($script:finalHead)) {
        $script:finalHead = (& git rev-parse HEAD).Trim()
    }

    $lines = New-Object System.Collections.Generic.List[string]
    $lines.Add("# Storefront Reverse Engineering Phase 3D Final Closure Gate")
    $lines.Add("")
    $lines.Add("Status: $Status")
    $lines.Add("Tested commit SHA: $script:testedHead")
    $lines.Add("Final HEAD SHA: $script:finalHead")
    $lines.Add("Working tree clean: $script:initialTreeClean")
    $lines.Add("Branch: $script:initialBranch")
    $lines.Add("UTC timestamp: $utcTimestamp")
    $lines.Add(".NET version: $dotnetVersion")
    $lines.Add("GitHub Actions status: disabled/local proof primary unless verified separately.")
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
    $lines.Add("Proof summary:")
    $lines.Add("- Phase 3A gate result: recorded in steps.")
    $lines.Add("- Phase 3B gate result: recorded in steps.")
    $lines.Add("- Phase 3C gate result: recorded in steps.")
    $lines.Add("- Review resolution proof: ConfidenceReview tests.")
    $lines.Add("- Required slot proof: StorefrontPattern and BlueprintV1 tests.")
    $lines.Add("- Handoff evidence proof: AgentHandoff tests.")
    $lines.Add("- Positive end-to-end proof: Phase3DPositiveEndToEnd tests.")
    $lines.Add("- Negative mutation proof: Phase3DNegative review, slot, evidence, handoff, and boundary tests.")
    $lines.Add("- StorefrontBuilder smoke result: plan-only smoke step.")
    $lines.Add("- Phase 3 closure decision: close only when this gate status is passed on final HEAD.")
    $lines.Add("")
    $lines.Add("Boundary assertions:")
    $lines.Add("- ReverseEngineering has no production project references.")
    $lines.Add("- StorefrontBuilder does not consume analysis/agent-handoff/* yet.")
    $lines.Add("- ReverseEngineering does not write Razor/CSS/JS storefront output.")
    $lines.Add("- ReverseEngineering does not write to Starter or generated storefront source.")
    $lines.Add("- No direct Commerce Node browser calls are generated or recommended.")
    $lines.Add("- No generated @page output exists.")
    $lines.Add("- No captures/home or plan.Pages.First() hardcode exists in workflow code.")
    $lines.Add("- No reviewed blueprint reference to .draft.json is accepted.")
    $lines.Add("- No handoff reference outside analysis/agent-handoff is accepted.")
    $lines.Add("")
    $lines.Add("Known limitations:")
    if ($knownLimitations.Count -eq 0) {
        $lines.Add("- StorefrontBuilder consumption remains disabled until Phase 4 approved cutover.")
    }
    else {
        foreach ($limitation in $knownLimitations) {
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

try {
    Set-Location $repoRoot
    New-Item -ItemType Directory -Force -Path $reportRoot | Out-Null

    $script:testedHead = (& git rev-parse HEAD).Trim()
    $script:finalHead = $script:testedHead
    $script:initialBranch = (& git branch --show-current).Trim()

    Invoke-Step "clean tree check" {
        Assert-CleanWorkingTree
        $script:initialTreeClean = $true
    }

    Invoke-Step "build ReverseEngineering" {
        Invoke-LoggedProcess -FileName "dotnet" -Arguments @("build", $toolProject)
    }

    Invoke-Step "Phase 3A regression gate" {
        Invoke-LoggedProcess -FileName "powershell" -Arguments @("-ExecutionPolicy", "Bypass", "-File", (Join-Path $repoRoot "scripts\qa\run-storefront-reverse-engineering-phase3a-gate.ps1"))
    }

    Invoke-Step "Phase 3B gate" {
        Invoke-LoggedProcess -FileName "powershell" -Arguments @("-ExecutionPolicy", "Bypass", "-File", (Join-Path $repoRoot "scripts\qa\run-storefront-reverse-engineering-phase3b-gate.ps1"))
    }

    Invoke-Step "Phase 3C gate" {
        Invoke-LoggedProcess -FileName "powershell" -Arguments @("-ExecutionPolicy", "Bypass", "-File", (Join-Path $repoRoot "scripts\qa\run-storefront-reverse-engineering-phase3c-final-handoff-gate.ps1"))
    }

    Invoke-Step "full ReverseEngineering tests" {
        Invoke-LoggedProcess `
            -FileName "dotnet" `
            -Arguments @("test", $testProject, "--no-restore", "--blame-hang", "--blame-hang-timeout", "5m") `
            -SummaryName "Full ReverseEngineering tests"
    }

    Invoke-Step "typed review resolution tests" {
        Invoke-TestFilter -Name "Typed review resolution" -Filter "ConfidenceReview"
    }

    Invoke-Step "exact slot contract tests" {
        Invoke-TestFilter -Name "Exact slot contracts" -Filter "StorefrontPattern|BlueprintV1"
    }

    Invoke-Step "self-contained evidence packaging tests" {
        Invoke-TestFilter -Name "Self-contained handoff evidence" -Filter "AgentHandoff"
    }

    Invoke-Step "canonical handoff validation tests" {
        Invoke-TestFilter -Name "Canonical handoff validation" -Filter "SchemaArtifact|AgentHandoff"
    }

    Invoke-Step "positive end-to-end proof" {
        Invoke-TestFilter -Name "Phase 3D positive end-to-end proof" -Filter "Phase3DPositiveEndToEnd"
    }

    Invoke-Step "negative review mutations" {
        Invoke-TestFilter -Name "Phase 3D negative review mutations" -Filter "Phase3DNegativeReviewMutation"
    }

    Invoke-Step "negative slot mutations" {
        Invoke-TestFilter -Name "Phase 3D negative slot mutations" -Filter "Phase3DNegativeSlotMutation"
    }

    Invoke-Step "negative evidence mutations" {
        Invoke-TestFilter -Name "Phase 3D negative evidence mutations" -Filter "Phase3DNegativeEvidenceMutation"
    }

    Invoke-Step "negative handoff mutations" {
        Invoke-TestFilter -Name "Phase 3D negative handoff mutations" -Filter "Phase3DNegativeHandoffMutation"
    }

    Invoke-Step "negative boundary mutations" {
        Invoke-TestFilter -Name "Phase 3D negative boundary mutations" -Filter "Phase3DNegativeBoundaryMutation"
    }

    Invoke-Step "boundary scans" {
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

        Assert-RgNoMatches -Pattern "BlazorShop.AI.StorefrontReverseEngineering|StorefrontReverseEngineering" -Paths @("BlazorShop.PresentationV2", "BlazorShop.Domain", "BlazorShop.Application", "BlazorShop.Infrastructure", "BlazorShop.ServiceDefaults", "BlazorShop.Tests.V2", "BlazorShop.sln") -ExtraArgs @("--glob", "!bin/**", "--glob", "!obj/**")
        Assert-RgNoMatches -Pattern "ProjectReference.*(BlazorShop\.Storefront\.V2|BlazorShop\.Storefront\.Runtime|BlazorShop\.Storefront\.Presentation|BlazorShop\.Storefront\.Components|BlazorShop\.ControlPlane|BlazorShop\.CommerceNode|BlazorShop\.Domain|BlazorShop\.Infrastructure|BlazorShop\.Web\.SharedV2)" -Paths @("tools\BlazorShop.AI.StorefrontReverseEngineering\BlazorShop.AI.StorefrontReverseEngineering.csproj") -ExtraArgs @("--glob", "*.csproj")
        Assert-RgNoMatches -Pattern "analysis/agent-handoff|agent-handoff-readiness|visual-blueprint\.v1" -Paths @("tools\BlazorShop.AI.StorefrontBuilder") -ExtraArgs @("--glob", "!bin/**", "--glob", "!obj/**")
        Assert-RgNoMatches -Pattern "WriteAllText(Async)?\([^\r\n]*(storefront-builder/generated|BlazorShop\.Storefront\.Generated|BlazorShop\.Storefront\.Starter)|Directory\.CreateDirectory\([^\r\n]*(storefront-builder/generated|BlazorShop\.Storefront\.Generated|BlazorShop\.Storefront\.Starter)" -Paths $reverseEngineeringPaths -ExtraArgs @("--glob", "*.cs", "--glob", "*.csproj", "--glob", "!bin/**", "--glob", "!obj/**")
        Assert-RgNoMatches -Pattern "WriteAllText(Async)?\([^\r\n]*\.(razor|css|js)([^a-zA-Z0-9]|$)" -Paths $reverseEngineeringPaths -ExtraArgs @("--glob", "*.cs", "--glob", "!bin/**", "--glob", "!obj/**")
        Assert-RgNoMatches -Pattern "@page|api/storefront|api/commerce|CommerceNode" -Paths @("tools\BlazorShop.AI.StorefrontReverseEngineering\tests\BlazorShop.AI.StorefrontReverseEngineering.Tests\Fixtures\Phase3D") -ExtraArgs @("--glob", "*.json")
        Assert-RgNoMatches -Pattern "captures/home" -Paths $workflowPaths -ExtraArgs @("--glob", "*.cs", "--glob", "!bin/**", "--glob", "!obj/**")
        Assert-RgNoMatches -Pattern "plan\.Pages\.First\(" -Paths $workflowPaths -ExtraArgs @("--glob", "*.cs", "--glob", "!bin/**", "--glob", "!obj/**")
        Assert-RgNoMatches -Pattern "\.draft\.json" -Paths @("tools\BlazorShop.AI.StorefrontReverseEngineering\tests\BlazorShop.AI.StorefrontReverseEngineering.Tests\Fixtures\Phase3D\positive-multipage-handoff-proof.json")
        Assert-RgNoMatches -Pattern "\.\./|[A-Za-z]:\\" -Paths @("tools\BlazorShop.AI.StorefrontReverseEngineering\tests\BlazorShop.AI.StorefrontReverseEngineering.Tests\Fixtures\Phase3D\positive-multipage-handoff-proof.json")
    }

    Invoke-Step "StorefrontBuilder plan-only smoke" {
        $pwsh = Get-Command pwsh -ErrorAction SilentlyContinue
        if ($null -eq $pwsh) {
            throw "PowerShell 7 (pwsh) is required for StorefrontBuilder plan-only smoke."
        }

        Invoke-LoggedProcess `
            -FileName $pwsh.Source `
            -Arguments @("-ExecutionPolicy", "Bypass", "-File", (Join-Path $repoRoot "tools\BlazorShop.AI.StorefrontBuilder\build-storefront.ps1"), "-Url", "https://example.test", "-Name", "Phase3DClosure", "-StoreKey", "sample", "-OutputRoot", "obj/storefront-builder/generated/reverse-engineering-phase3d-gate", "-Mode", "plan-only")
    }

    Invoke-Step "final inspect proof" {
        Invoke-TestFilter -Name "Final inspect proof" -Filter "AgentHandoffReadiness_CliSucceedsOnlyAfterFinalReadinessPasses|AgentHandoffReadiness_InspectReportsFinalHandoffStatus"
    }

    Invoke-Step "final HEAD check" {
        Assert-HeadUnchanged
        Assert-CleanWorkingTree
    }

    $reportPath = Join-Path $reportRoot ("phase3d-final-closure-gate-" + (Get-Date -Format "yyyyMMddHHmmss") + ".md")
    New-ReportLines -Status "passed" | Set-Content -Path $reportPath -Encoding UTF8
    Write-Host "Gate passed. Report: $reportPath"
}
catch {
    $script:finalHead = (& git rev-parse HEAD).Trim()
    $reportPath = Join-Path $reportRoot ("phase3d-final-closure-gate-failed-" + (Get-Date -Format "yyyyMMddHHmmss") + ".md")
    New-ReportLines -Status "failed" -ErrorMessage $_.Exception.Message | Set-Content -Path $reportPath -Encoding UTF8
    Write-Error "Gate failed. Report: $reportPath. Error: $($_.Exception.Message)"
    exit 1
}

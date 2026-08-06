[CmdletBinding()]
param(
    [string]$Url = "https://www.kindredcoast.com/",
    [string]$Name = "KindredCoast",
    [string]$OutputRoot = "artifacts\storefront-reverse-engineering\projects",
    [string]$ReportRoot = "artifacts\storefront-reverse-engineering\reports",
    [int]$CommandTimeoutSeconds = 900,
    [switch]$Force,
    [switch]$Resume,
    [switch]$UseAi,
    [switch]$ResolveSafeReviewItems,
    [switch]$InstallPlaywright,
    [switch]$FailOnBlockers,
    [switch]$Help
)

$ErrorActionPreference = "Stop"

function Write-Usage {
    Write-Host "StorefrontReverseEngineering production runner"
    Write-Host ""
    Write-Host "Usage:"
    Write-Host "  powershell -NoProfile -ExecutionPolicy Bypass -File scripts\reverse-engineering\run-storefront-reverse-engineering-production.ps1 [options]"
    Write-Host ""
    Write-Host "Options:"
    Write-Host "  -Url <url>                    Reference storefront URL. Defaults to https://www.kindredcoast.com/"
    Write-Host "  -Name <name>                  ReverseEngineering project name. Defaults to KindredCoast."
    Write-Host "  -OutputRoot <path>            Output project root. Defaults to artifacts\storefront-reverse-engineering\projects."
    Write-Host "  -ReportRoot <path>            Report output root. Defaults to artifacts\storefront-reverse-engineering\reports."
    Write-Host "  -CommandTimeoutSeconds <sec>  Timeout for each external command. Defaults to 900."
    Write-Host "  -Force                        Replace the single resolved project root under the approved output root."
    Write-Host "  -Resume                       Resume an existing resolved project root instead of starting a new run."
    Write-Host "  -UseAi                        Do not pass --no-ai to the CLI."
    Write-Host "  -ResolveSafeReviewItems       Materialize safe visual-only review decisions, then rerun from assemble-blueprint-v1."
    Write-Host "  -InstallPlaywright            Install Chromium through the built Playwright script when missing."
    Write-Host "  -FailOnBlockers               Return exit code 3 when the workflow completes with readiness/handoff blockers."
    Write-Host "  -Help                         Show this help text."
}

if ($Help) {
    Write-Usage
    return
}

function Get-ProductionRepositoryRoot {
    param([Parameter(Mandatory = $true)][string]$StartPath)

    $candidate = [System.IO.DirectoryInfo]::new([System.IO.Path]::GetFullPath($StartPath))
    while ($null -ne $candidate) {
        $toolProjectCandidate = Join-Path $candidate.FullName "tools\BlazorShop.AI.StorefrontReverseEngineering\BlazorShop.AI.StorefrontReverseEngineering.csproj"
        $gitRootCandidate = Join-Path $candidate.FullName ".git"
        if ((Test-Path -LiteralPath $gitRootCandidate) -and (Test-Path -LiteralPath $toolProjectCandidate)) {
            return $candidate.FullName
        }

        $candidate = $candidate.Parent
    }

    throw "Could not find repository root from '$StartPath'. Expected to find .git and tools\BlazorShop.AI.StorefrontReverseEngineering."
}

$repoRoot = Get-ProductionRepositoryRoot -StartPath $PSScriptRoot
$referenceUrl = $Url
$visualProjectName = $Name
$toolProject = Join-Path $repoRoot "tools\BlazorShop.AI.StorefrontReverseEngineering\BlazorShop.AI.StorefrontReverseEngineering.csproj"
$toolDll = Join-Path $repoRoot "tools\BlazorShop.AI.StorefrontReverseEngineering\bin\Debug\net10.0\BlazorShop.AI.StorefrontReverseEngineering.dll"
$commands = New-Object System.Collections.Generic.List[string]
$steps = New-Object System.Collections.Generic.List[object]
$notes = New-Object System.Collections.Generic.List[string]
$failedStep = ""
$lastExitCode = "not-run"
$runExitCode = "not-run"
$resolveReviewExitCode = "not-run"
$inspectExitCode = "not-run"
$validateExitCode = "not-run"
$handoffValidationExitCode = "not-run"
$handoffDryRunExitCode = "not-run"
$sourceUrlStatus = "not-run"
$projectRoot = ""
$reportPath = ""

function Format-CommandArgument {
    param([Parameter(Mandatory = $true)][string]$Value)

    if ($Value.Contains(" ") -or $Value.Contains('"')) {
        return '"' + $Value.Replace('"', '\"') + '"'
    }

    return $Value
}

function Get-ProductionProjectId {
    param([Parameter(Mandatory = $true)][string]$Value)

    $builder = New-Object System.Text.StringBuilder
    $pendingSeparator = $false
    foreach ($character in $Value.Trim().ToLowerInvariant().ToCharArray()) {
        if ($character -match "[a-z0-9]") {
            if ($pendingSeparator -and $builder.Length -gt 0) {
                [void]$builder.Append("-")
            }

            [void]$builder.Append($character)
            $pendingSeparator = $false
            continue
        }

        $pendingSeparator = $builder.Length -gt 0
    }

    $projectId = $builder.ToString().Trim("-")
    if ([string]::IsNullOrWhiteSpace($projectId)) {
        throw "Name must contain at least one ASCII letter or digit."
    }

    if ($projectId.Length -gt 80) {
        $projectId = $projectId.Substring(0, 80).Trim("-")
    }

    return $projectId
}

function Resolve-RepoPath {
    param([Parameter(Mandatory = $true)][string]$Path)

    if ([System.IO.Path]::IsPathRooted($Path)) {
        return [System.IO.Path]::GetFullPath($Path)
    }

    return [System.IO.Path]::GetFullPath((Join-Path $repoRoot $Path))
}

function Invoke-ProductionProcess {
    param(
        [Parameter(Mandatory = $true)][string]$FileName,
        [Parameter(Mandatory = $true)][string[]]$Arguments,
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
    if (-not $process.WaitForExit($CommandTimeoutSeconds * 1000)) {
        try {
            $process.Kill($true)
        }
        catch {
            $process.Kill()
        }

        $script:lastExitCode = "timeout"
        throw "Command timed out after $CommandTimeoutSeconds seconds: $commandLine"
    }

    $stdout = $stdoutTask.GetAwaiter().GetResult()
    $stderr = $stderrTask.GetAwaiter().GetResult()
    if (-not [string]::IsNullOrWhiteSpace($stdout)) {
        Write-Host $stdout
    }
    if (-not [string]::IsNullOrWhiteSpace($stderr)) {
        Write-Host $stderr
    }

    $script:lastExitCode = $process.ExitCode
    if ($AllowedExitCodes -notcontains $process.ExitCode) {
        throw "Command failed with exit code $($process.ExitCode): $commandLine"
    }

    return [pscustomobject]@{
        ExitCode = $process.ExitCode
        Stdout = $stdout
        Stderr = $stderr
    }
}

function Invoke-ProductionStep {
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
            ExitCode = $script:lastExitCode
        })
    }
    catch {
        $duration = [DateTimeOffset]::UtcNow - $startedUtc
        $script:failedStep = $Name
        $steps.Add([pscustomobject]@{
            Name = $Name
            Status = "failed"
            DurationSeconds = [Math]::Round($duration.TotalSeconds, 2)
            ExitCode = $script:lastExitCode
        })
        throw
    }
}

function New-ProductionReportLines {
    param(
        [Parameter(Mandatory = $true)][string]$Status,
        [string]$ErrorMessage = ""
    )

    $head = (& git rev-parse HEAD).Trim()
    $branch = (& git branch --show-current).Trim()
    $dotnetVersion = (& dotnet --version).Trim()
    $utcTimestamp = [DateTimeOffset]::UtcNow.ToString("u", [System.Globalization.CultureInfo]::InvariantCulture)
    $handoffRoot = if ([string]::IsNullOrWhiteSpace($projectRoot)) { "" } else { Join-Path $projectRoot "analysis\agent-handoff" }
    $readinessReport = if ([string]::IsNullOrWhiteSpace($projectRoot)) { "" } else { Join-Path $projectRoot "reports\readiness-report.json" }
    $humanReadinessReport = if ([string]::IsNullOrWhiteSpace($projectRoot)) { "" } else { Join-Path $projectRoot "reports\readiness-report.md" }

    $lines = New-Object System.Collections.Generic.List[string]
    $lines.Add("# StorefrontReverseEngineering Production Run")
    $lines.Add("")
    $lines.Add("Status: $Status")
    $lines.Add("URL: $referenceUrl")
    $lines.Add("Name: $visualProjectName")
    $lines.Add("Project root: $projectRoot")
    $lines.Add("Report path: $reportPath")
    $lines.Add("Source URL probe: $sourceUrlStatus")
    $lines.Add("Run exit code: $runExitCode")
    $lines.Add("Resolve safe review exit code: $resolveReviewExitCode")
    $lines.Add("Inspect exit code: $inspectExitCode")
    $lines.Add("Validate exit code: $validateExitCode")
    $lines.Add("Handoff validation exit code: $handoffValidationExitCode")
    $lines.Add("Handoff dry-run exit code: $handoffDryRunExitCode")
    $lines.Add("Readiness report: $readinessReport")
    $lines.Add("Readiness report markdown: $humanReadinessReport")
    $lines.Add("Handoff root: $handoffRoot")
    $lines.Add("Commit SHA: $head")
    $lines.Add("Branch: $branch")
    $lines.Add("UTC timestamp: $utcTimestamp")
    $lines.Add(".NET version: $dotnetVersion")
    if (-not [string]::IsNullOrWhiteSpace($failedStep)) {
        $lines.Add("Failed step: $failedStep")
    }
    $lines.Add("")
    $lines.Add("Steps:")
    foreach ($step in $steps) {
        $lines.Add("- $($step.Name): $($step.Status) ($($step.DurationSeconds)s, exit=$($step.ExitCode))")
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
    $lines.Add("Notes:")
    if ($notes.Count -eq 0) {
        $lines.Add("- (none)")
    }
    else {
        foreach ($note in $notes) {
            $lines.Add("- $note")
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

    $uri = [Uri]::new($referenceUrl)
    if (-not $uri.IsAbsoluteUri -or ($uri.Scheme -ne "https" -and $uri.Scheme -ne "http")) {
        throw "Url must be an absolute http or https URL."
    }

    $resolvedOutputRoot = Resolve-RepoPath -Path $OutputRoot
    $resolvedReportRoot = Resolve-RepoPath -Path $ReportRoot
    $projectId = Get-ProductionProjectId -Value $visualProjectName
    $projectRoot = Join-Path $resolvedOutputRoot $projectId
    New-Item -ItemType Directory -Force -Path $resolvedReportRoot | Out-Null
    $reportPath = Join-Path $resolvedReportRoot ("storefront-reverse-engineering-production-" + $projectId + "-" + (Get-Date -Format "yyyyMMddHHmmss") + ".md")

    Invoke-ProductionStep -Name "probe source URL" -Script {
        try {
            $response = Invoke-WebRequest -Uri $referenceUrl -Method Head -MaximumRedirection 5 -TimeoutSec 30 -UseBasicParsing
            $script:sourceUrlStatus = "$($response.StatusCode) $($response.StatusDescription)"
        }
        catch {
            $response = Invoke-WebRequest -Uri $referenceUrl -Method Get -MaximumRedirection 5 -TimeoutSec 30 -UseBasicParsing
            $script:sourceUrlStatus = "$($response.StatusCode) $($response.StatusDescription)"
        }

        $script:lastExitCode = 0
    }

    Invoke-ProductionStep -Name "build ReverseEngineering tool" -Script {
        Invoke-ProductionProcess -FileName "dotnet" -Arguments @("build", $toolProject) | Out-Null
    }

    Invoke-ProductionStep -Name "check Playwright Chromium" -Script {
        $playwrightScript = Join-Path $repoRoot "tools\BlazorShop.AI.StorefrontReverseEngineering\bin\Debug\net10.0\playwright.ps1"
        if (-not (Test-Path $playwrightScript)) {
            throw "Playwright install script was not found after build: $playwrightScript"
        }

        $localAppData = [Environment]::GetFolderPath([Environment+SpecialFolder]::LocalApplicationData)
        $browserRoot = Join-Path $localAppData "ms-playwright"
        if (-not (Test-Path $browserRoot)) {
            if (-not $InstallPlaywright) {
                throw "Playwright Chromium is not installed. Rerun with -InstallPlaywright or run: $playwrightScript install chromium"
            }

            Invoke-ProductionProcess -FileName "powershell" -Arguments @("-NoProfile", "-ExecutionPolicy", "Bypass", "-File", $playwrightScript, "install", "chromium") | Out-Null
        }
        else {
            $script:lastExitCode = 0
        }
    }

    Invoke-ProductionStep -Name "run production reverse-engineering workflow" -Script {
        $noAiArgs = @()
        if (-not $UseAi) {
            $noAiArgs += "--no-ai"
        }

        if ($Resume) {
            if (-not (Test-Path $projectRoot)) {
                throw "Cannot resume because project root does not exist: $projectRoot"
            }

            $result = Invoke-ProductionProcess -FileName "dotnet" -Arguments (@($toolDll, "resume", "--project", $projectRoot) + $noAiArgs) -AllowedExitCodes @(0, 3)
            $script:runExitCode = $result.ExitCode
            if ($result.ExitCode -eq 3) {
                $notes.Add("Workflow reached CLI exit code 3. This is usually a produced-but-blocked review/readiness state; inspect output and readiness report are collected below.")
            }
            return
        }

        if ((Test-Path $projectRoot) -and -not $Force) {
            throw "Project root already exists: $projectRoot. Rerun with -Resume to continue or -Force to replace this single project root."
        }

        $forceArgs = @()
        if ($Force) {
            $forceArgs += "--force"
        }

        $result = Invoke-ProductionProcess -FileName "dotnet" -Arguments (@($toolDll, "run", "--url", $referenceUrl, "--name", $visualProjectName, "--output-root", $resolvedOutputRoot) + $noAiArgs + $forceArgs) -AllowedExitCodes @(0, 3)
        $script:runExitCode = $result.ExitCode
        if ($result.ExitCode -eq 3) {
            $notes.Add("Workflow reached CLI exit code 3. This is usually a produced-but-blocked review/readiness state; inspect output and readiness report are collected below.")
        }
    }

    if ($ResolveSafeReviewItems) {
        Invoke-ProductionStep -Name "materialize safe review decisions" -Script {
            $result = Invoke-ProductionProcess -FileName "dotnet" -Arguments @($toolDll, "resolve-safe-review", "--project", $projectRoot) -AllowedExitCodes @(0, 3)
            $script:resolveReviewExitCode = $result.ExitCode
            if ($result.ExitCode -eq 3) {
                $notes.Add("Safe review materialization left manual blockers. The rerun still executes so generation readiness records the remaining blocker set.")
            }
        }

        Invoke-ProductionStep -Name "rerun blueprint and handoff after safe review decisions" -Script {
            $noAiArgs = @()
            if (-not $UseAi) {
                $noAiArgs += "--no-ai"
            }

            $result = Invoke-ProductionProcess -FileName "dotnet" -Arguments (@($toolDll, "resume", "--project", $projectRoot, "--force-step", "assemble-blueprint-v1") + $noAiArgs) -AllowedExitCodes @(0, 3)
            $script:runExitCode = $result.ExitCode
            if ($result.ExitCode -eq 3) {
                $notes.Add("Post-review workflow rerun returned exit code 3. Inspect generation readiness for remaining blockers.")
            }
        }
    }

    Invoke-ProductionStep -Name "inspect production project" -Script {
        $result = Invoke-ProductionProcess -FileName "dotnet" -Arguments @($toolDll, "inspect", "--project", $projectRoot)
        $script:inspectExitCode = $result.ExitCode
    }

    Invoke-ProductionStep -Name "validate production project readiness" -Script {
        $result = Invoke-ProductionProcess -FileName "dotnet" -Arguments @($toolDll, "validate", "--project", $projectRoot) -AllowedExitCodes @(0, 3)
        $script:validateExitCode = $result.ExitCode
        if ($result.ExitCode -eq 3) {
            $notes.Add("Readiness validation returned exit code 3. The report records blocking findings for the generated production evidence.")
        }
    }

    Invoke-ProductionStep -Name "validate portable handoff when present" -Script {
        $handoffRoot = Join-Path $projectRoot "analysis\agent-handoff"
        $handoffManifest = Join-Path $handoffRoot "manifest.json"
        if (-not (Test-Path $handoffManifest)) {
            $script:handoffValidationExitCode = "skipped"
            $notes.Add("Portable handoff manifest was not produced yet: $handoffManifest")
            $script:lastExitCode = 0
            return
        }

        $schemaRoot = Join-Path $repoRoot "tools\BlazorShop.AI.StorefrontReverseEngineering\Schemas"
        $result = Invoke-ProductionProcess -FileName "dotnet" -Arguments @($toolDll, "validate-handoff", "--handoff-root", $projectRoot, "--schema-root", $schemaRoot) -AllowedExitCodes @(0, 3)
        $script:handoffValidationExitCode = $result.ExitCode
        if ($result.ExitCode -eq 3) {
            $notes.Add("Portable handoff validation returned blocking findings.")
        }
    }

    Invoke-ProductionStep -Name "dry-run portable handoff when valid enough to load" -Script {
        $handoffRoot = Join-Path $projectRoot "analysis\agent-handoff"
        $handoffManifest = Join-Path $handoffRoot "manifest.json"
        if (-not (Test-Path $handoffManifest)) {
            $script:handoffDryRunExitCode = "skipped"
            $script:lastExitCode = 0
            return
        }

        $schemaRoot = Join-Path $repoRoot "tools\BlazorShop.AI.StorefrontReverseEngineering\Schemas"
        $result = Invoke-ProductionProcess -FileName "dotnet" -Arguments @($toolDll, "dry-run-handoff", "--handoff-root", $projectRoot, "--schema-root", $schemaRoot) -AllowedExitCodes @(0, 3)
        $script:handoffDryRunExitCode = $result.ExitCode
    }

    $finalStatus = "passed"
    if ($runExitCode -eq 3 -or $validateExitCode -eq 3 -or $handoffValidationExitCode -eq 3 -or $handoffDryRunExitCode -eq 3) {
        $finalStatus = "completed-with-blockers"
    }

    New-ProductionReportLines -Status $finalStatus | Set-Content -Path $reportPath -Encoding UTF8
    Write-Host "Production ReverseEngineering run completed. Status: $finalStatus. Report: $reportPath"
    if ($finalStatus -eq "completed-with-blockers" -and $FailOnBlockers) {
        exit 3
    }
}
catch {
    if ([string]::IsNullOrWhiteSpace($reportPath)) {
        $resolvedReportRoot = Resolve-RepoPath -Path $ReportRoot
        New-Item -ItemType Directory -Force -Path $resolvedReportRoot | Out-Null
        $reportPath = Join-Path $resolvedReportRoot ("storefront-reverse-engineering-production-failed-" + (Get-Date -Format "yyyyMMddHHmmss") + ".md")
    }

    New-ProductionReportLines -Status "failed" -ErrorMessage $_.Exception.Message | Set-Content -Path $reportPath -Encoding UTF8
    Write-Error "Production ReverseEngineering run failed. Report: $reportPath. Error: $($_.Exception.Message)"
    exit 1
}

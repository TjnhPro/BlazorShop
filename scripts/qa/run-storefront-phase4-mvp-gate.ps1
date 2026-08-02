param(
    [string]$GeneratedProjectRoot = "",
    [string]$FixtureRoot = "",
    [string]$HandoffRoot = "",
    [string]$ScreenshotRoot = "",
    [int]$MaxRepairAttempts = 2,
    [switch]$SkipRepair,
    [string]$BaseUrl = "",
    [string]$Configuration = "Debug",
    [int]$CommandTimeoutSeconds = 600,
    [switch]$Help
)

$ErrorActionPreference = "Stop"

function Show-Help {
    Write-Host "Storefront Phase 4 MVP gate"
    Write-Host ""
    Write-Host "Usage:"
    Write-Host "  powershell -NoProfile -ExecutionPolicy Bypass -File scripts\qa\run-storefront-phase4-mvp-gate.ps1 -GeneratedProjectRoot <path> [options]"
    Write-Host ""
    Write-Host "Options:"
    Write-Host "  -GeneratedProjectRoot <path>   Generated storefront project root."
    Write-Host "  -FixtureRoot <path>            Optional file fixture root for visual QA."
    Write-Host "  -HandoffRoot <path>            Optional portable handoff root used for metadata hashing."
    Write-Host "  -ScreenshotRoot <path>         Optional screenshot/evidence root. Defaults under generated project docs."
    Write-Host "  -MaxRepairAttempts <number>    Optional bounded repair cap. Defaults to 2."
    Write-Host "  -SkipRepair                    Skip bounded repair attempts, but still run visual QA."
    Write-Host "  -BaseUrl <url>                 Optional running storefront base URL for visual QA."
    Write-Host "  -Configuration <name>          Build configuration. Defaults to Debug."
    Write-Host "  -CommandTimeoutSeconds <sec>   Timeout for each external command. Defaults to 600."
    Write-Host "  -Help                          Show this help text."
    Write-Host ""
    Write-Host "This gate is local-only and does not invoke GitHub Actions."
}

if ($Help) {
    Show-Help
    exit 0
}

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
$builderRoot = Join-Path $repoRoot "tools\BlazorShop.AI.StorefrontBuilder"
$visualRoot = Join-Path $repoRoot "tools\BlazorShop.AI.Visual"

function Resolve-RepoPath {
    param([string]$Path)

    if ([string]::IsNullOrWhiteSpace($Path)) {
        return ""
    }

    if ([System.IO.Path]::IsPathRooted($Path)) {
        return [System.IO.Path]::GetFullPath($Path)
    }

    return [System.IO.Path]::GetFullPath((Join-Path $repoRoot $Path))
}

function Convert-ToRepoRelativePath {
    param([string]$Path)

    if ([string]::IsNullOrWhiteSpace($Path)) {
        return ""
    }

    $fullPath = [System.IO.Path]::GetFullPath($Path)
    if ($fullPath.StartsWith($repoRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
        return [System.IO.Path]::GetRelativePath($repoRoot, $fullPath).Replace("\", "/")
    }

    return $fullPath.Replace("\", "/")
}

function Get-NormalizedFileSha256 {
    param([string]$Path)

    if (-not (Test-Path -LiteralPath $Path)) {
        return "missing"
    }

    $content = (Get-Content -LiteralPath $Path -Raw).Replace("`r`n", "`n").Replace("`r", "`n")
    $bytes = [System.Text.Encoding]::UTF8.GetBytes($content)
    $hash = [System.Security.Cryptography.SHA256]::HashData($bytes)
    return [System.BitConverter]::ToString($hash).Replace("-", "").ToLowerInvariant()
}

function Get-DirectorySha256 {
    param([string]$Path)

    if ([string]::IsNullOrWhiteSpace($Path) -or -not (Test-Path -LiteralPath $Path)) {
        return "not-specified"
    }

    $builder = [System.Text.StringBuilder]::new()
    Get-ChildItem -LiteralPath $Path -Recurse -File |
        Sort-Object FullName |
        ForEach-Object {
            $relative = [System.IO.Path]::GetRelativePath($Path, $_.FullName).Replace("\", "/")
            [void]$builder.AppendLine($relative)
            [void]$builder.AppendLine((Get-NormalizedFileSha256 -Path $_.FullName))
        }

    $bytes = [System.Text.Encoding]::UTF8.GetBytes($builder.ToString())
    $hash = [System.Security.Cryptography.SHA256]::HashData($bytes)
    return [System.BitConverter]::ToString($hash).Replace("-", "").ToLowerInvariant()
}

function Read-SimpleYamlValue {
    param(
        [string]$Text,
        [string]$Key,
        [string]$Default = ""
    )

    foreach ($line in $Text -split "\r?\n") {
        $match = [regex]::Match($line, "^\s*$([regex]::Escape($Key)):\s*(.*?)\s*$")
        if ($match.Success) {
            return $match.Groups[1].Value.Trim().Trim('"')
        }
    }

    return $Default
}

$steps = [System.Collections.Generic.List[object]]::new()
$artifactPaths = [System.Collections.Generic.List[string]]::new()
$startedUtc = [DateTimeOffset]::UtcNow.ToString("o")
$resolvedProjectRoot = Resolve-RepoPath $GeneratedProjectRoot

if ([string]::IsNullOrWhiteSpace($resolvedProjectRoot)) {
    throw "GeneratedProjectRoot is required. Rerun with -Help for usage."
}

$analysisRoot = Join-Path $resolvedProjectRoot "docs\storefront-analysis"
$metadataPath = Join-Path $analysisRoot "metadata.yaml"
$generationPlanPath = Join-Path $analysisRoot "generation-plan.json"
$taskPackageManifestPath = Join-Path $analysisRoot "agent-task-package\manifest.json"
$agentWrittenFilesPath = Join-Path $analysisRoot "agent-written-files.json"
$reportJsonPath = Join-Path $analysisRoot "phase4-mvp-gate-report.json"
$reportMdPath = Join-Path $analysisRoot "phase4-mvp-gate-report.md"
$visualQaReportPath = Join-Path $analysisRoot "visual-qa-report.md"
$resolvedScreenshotRoot = if ([string]::IsNullOrWhiteSpace($ScreenshotRoot)) {
    Join-Path $analysisRoot "visual-qa"
} else {
    Resolve-RepoPath $ScreenshotRoot
}
$resolvedFixtureRoot = Resolve-RepoPath $FixtureRoot
$resolvedHandoffRoot = Resolve-RepoPath $HandoffRoot
$projectName = Split-Path -Leaf $resolvedProjectRoot
$storeKey = "sample"
$finalDecision = "failed"

function New-RerunCommand {
    return "powershell -NoProfile -ExecutionPolicy Bypass -File scripts\qa\run-storefront-phase4-mvp-gate.ps1 -GeneratedProjectRoot `"$GeneratedProjectRoot`""
}

function Add-GateStep {
    param(
        [string]$Name,
        [string]$Status,
        [string]$Command,
        [string]$ReportPath = "",
        [string]$Problem = "",
        [string]$LikelyCause = "",
        [string]$RerunCommand = ""
    )

    $entry = [ordered]@{
        name = $Name
        status = $Status
        command = $Command
    }

    if (-not [string]::IsNullOrWhiteSpace($ReportPath)) { $entry.reportPath = Convert-ToRepoRelativePath $ReportPath }
    if (-not [string]::IsNullOrWhiteSpace($Problem)) { $entry.problem = $Problem }
    if (-not [string]::IsNullOrWhiteSpace($LikelyCause)) { $entry.likelyCause = $LikelyCause }
    if (-not [string]::IsNullOrWhiteSpace($RerunCommand)) { $entry.rerunCommand = $RerunCommand }
    $steps.Add([pscustomobject]$entry)
}

function Invoke-GateCommand {
    param(
        [string]$Name,
        [string]$FileName,
        [string[]]$Arguments,
        [string]$LikelyCause = "The command failed; inspect stdout/stderr in the terminal output.",
        [switch]$AllowFailure
    )

    $commandText = "$FileName $($Arguments -join ' ')"
    Write-Host "== $Name =="

    $process = [System.Diagnostics.Process]::new()
    $process.StartInfo.FileName = $FileName
    $process.StartInfo.WorkingDirectory = $repoRoot
    $process.StartInfo.UseShellExecute = $false
    $process.StartInfo.RedirectStandardOutput = $false
    $process.StartInfo.RedirectStandardError = $false
    foreach ($argument in $Arguments) {
        [void]$process.StartInfo.ArgumentList.Add($argument)
    }

    [void]$process.Start()
    if (-not $process.WaitForExit($CommandTimeoutSeconds * 1000)) {
        try { $process.Kill($true) } catch { }
        $problem = "Command timed out after $CommandTimeoutSeconds seconds."
        Add-GateStep -Name $Name -Status "failed" -Command $commandText -Problem $problem -LikelyCause $LikelyCause -RerunCommand (New-RerunCommand)
        if ($AllowFailure) { return $false }
        throw $problem
    }

    if ($process.ExitCode -ne 0) {
        $problem = "Command exited with code $($process.ExitCode)."
        Add-GateStep -Name $Name -Status "failed" -Command $commandText -Problem $problem -LikelyCause $LikelyCause -RerunCommand (New-RerunCommand)
        if ($AllowFailure) { return $false }
        throw $problem
    }

    Add-GateStep -Name $Name -Status "passed" -Command $commandText
    return $true
}

function Invoke-AssertionStep {
    param(
        [string]$Name,
        [string]$Command,
        [scriptblock]$Assertion,
        [string]$LikelyCause
    )

    Write-Host "== $Name =="
    try {
        & $Assertion
        Add-GateStep -Name $Name -Status "passed" -Command $Command
    }
    catch {
        Add-GateStep -Name $Name -Status "failed" -Command $Command -Problem $_.Exception.Message -LikelyCause $LikelyCause -RerunCommand (New-RerunCommand)
        throw
    }
}

function Save-GateReports {
    param([string]$Status)

    New-Item -ItemType Directory -Force -Path $analysisRoot | Out-Null
    $artifactPaths.Clear()
    foreach ($path in @($reportJsonPath, $reportMdPath, $visualQaReportPath, $resolvedScreenshotRoot)) {
        if (-not [string]::IsNullOrWhiteSpace($path)) {
            $artifactPaths.Add((Convert-ToRepoRelativePath $path))
        }
    }

    $report = [ordered]@{
        schemaVersion = "0.1.0"
        commandMetadata = [ordered]@{
            command = (New-RerunCommand)
            startedUtc = $startedUtc
            finishedUtc = [DateTimeOffset]::UtcNow.ToString("o")
        }
        generatedProjectRoot = (Convert-ToRepoRelativePath $resolvedProjectRoot)
        inputHandoffMetadata = [ordered]@{
            handoffRoot = if ([string]::IsNullOrWhiteSpace($resolvedHandoffRoot)) { "not-specified" } else { Convert-ToRepoRelativePath $resolvedHandoffRoot }
            handoffHash = Get-DirectorySha256 -Path $resolvedHandoffRoot
            generationPlanHash = Get-NormalizedFileSha256 -Path $generationPlanPath
            taskPackageHash = Get-NormalizedFileSha256 -Path $taskPackageManifestPath
        }
        gateSteps = @($steps)
        artifactPaths = @($artifactPaths)
        finalDecision = $Status
    }

    $json = $report | ConvertTo-Json -Depth 20
    Set-Content -LiteralPath $reportJsonPath -Value $json -Encoding UTF8

    $lines = [System.Collections.Generic.List[string]]::new()
    $lines.Add("# Storefront Phase 4 MVP Gate Report")
    $lines.Add("")
    $lines.Add("- Decision: $Status")
    $lines.Add("- Generated project root: $(Convert-ToRepoRelativePath $resolvedProjectRoot)")
    $lines.Add("- Report JSON: $(Convert-ToRepoRelativePath $reportJsonPath)")
    $lines.Add("- Evidence root: $(Convert-ToRepoRelativePath $resolvedScreenshotRoot)")
    $lines.Add("")
    $lines.Add("## Steps")
    foreach ($step in $steps) {
        $lines.Add("")
        $lines.Add("- $($step.status): $($step.name)")
        $lines.Add(('  - command: `{0}`' -f $step.command))
        if ($step.PSObject.Properties.Name -contains "problem") { $lines.Add("  - problem: $($step.problem)") }
        if ($step.PSObject.Properties.Name -contains "likelyCause") { $lines.Add("  - likely cause: $($step.likelyCause)") }
        if ($step.PSObject.Properties.Name -contains "rerunCommand") { $lines.Add(('  - rerun: `{0}`' -f $step.rerunCommand)) }
        if ($step.PSObject.Properties.Name -contains "reportPath") { $lines.Add("  - report: $($step.reportPath)") }
    }

    Set-Content -LiteralPath $reportMdPath -Value $lines -Encoding UTF8
}

try {
    if (Test-Path -LiteralPath $metadataPath) {
        $metadata = Get-Content -LiteralPath $metadataPath -Raw
        $projectName = Read-SimpleYamlValue -Text $metadata -Key "projectName" -Default $projectName
        $storeKey = Read-SimpleYamlValue -Text $metadata -Key "storeKey" -Default $storeKey
    }

    $projectFile = Join-Path $resolvedProjectRoot "$projectName.csproj"

    Invoke-AssertionStep -Name "validate generated project metadata" -Command "metadata/csproj/feature manifest checks" -LikelyCause "The generated project is incomplete or metadata.yaml is stale." -Assertion {
        foreach ($path in @($resolvedProjectRoot, $projectFile, $metadataPath, (Join-Path $resolvedProjectRoot "Features\feature-manifest.json"))) {
            if (-not (Test-Path -LiteralPath $path)) {
                throw "Required generated project artifact is missing: $path"
            }
        }
    }

    Invoke-AssertionStep -Name "validate generation plan presence" -Command "Test-Path docs/storefront-analysis/generation-plan.json" -LikelyCause "Run StorefrontBuilder with a reviewed handoff package before the MVP gate." -Assertion {
        if (-not (Test-Path -LiteralPath $generationPlanPath)) {
            throw "Generation plan is missing: $generationPlanPath"
        }
    }

    Invoke-AssertionStep -Name "validate agent task package presence" -Command "Test-Path docs/storefront-analysis/agent-task-package/manifest.json" -LikelyCause "Run write-agent-task-package.mjs or regenerate the handoff project." -Assertion {
        if (-not (Test-Path -LiteralPath $taskPackageManifestPath)) {
            throw "Agent task package manifest is missing: $taskPackageManifestPath"
        }
    }

    Invoke-AssertionStep -Name "validate visual schemas when present" -Command "ConvertFrom-Json visual artifacts" -LikelyCause "One of the generated visual JSON artifacts is malformed or missing required top-level fields." -Assertion {
        foreach ($artifact in @(
            @{ Path = Join-Path $analysisRoot "visual-plan.json"; Required = @("schemaVersion", "inputs", "plannedTasks") },
            @{ Path = Join-Path $analysisRoot "visual-implementation-report.json"; Required = @("schemaVersion", "appliedTasks", "changedFiles", "validation") },
            @{ Path = Join-Path $analysisRoot "visual-qa-report.json"; Required = @("schemaVersion", "viewportCaptures", "evidencePaths", "issues", "repairAttempts", "passed") }
        )) {
            if (-not (Test-Path -LiteralPath $artifact.Path)) {
                continue
            }

            $json = Get-Content -LiteralPath $artifact.Path -Raw | ConvertFrom-Json
            foreach ($required in $artifact.Required) {
                if ($json.PSObject.Properties.Name -notcontains $required) {
                    throw "$($artifact.Path) is missing required field '$required'."
                }
            }
        }
    }

    Invoke-GateCommand -Name "run StorefrontBuilder handoff boundary validation" -FileName "node" -Arguments @(
        (Join-Path $builderRoot "scripts\validate\Test-StorefrontBuilderHandoffBoundary.mjs"),
        "--project-root", $resolvedProjectRoot,
        "--name", $projectName
    ) -LikelyCause "Generated handoff artifacts, allowed outputs, or protected boundary metadata drifted."

    Invoke-GateCommand -Name "restore generated project" -FileName "dotnet" -Arguments @(
        "restore", $projectFile, "--no-cache", "--force-evaluate"
    ) -LikelyCause "Generated package references or local NuGet package availability are invalid."

    Invoke-GateCommand -Name "build generated project" -FileName "dotnet" -Arguments @(
        "build", $projectFile, "--configuration", $Configuration, "--no-restore"
    ) -LikelyCause "Generated visual files do not compile against Storefront Presentation packages."

    Invoke-AssertionStep -Name "run visual write ownership validation" -Command "agent-written-files.json checksum and allowlist check" -LikelyCause "Run record-agent-visual-writes.mjs after visual implementation or repair." -Assertion {
        if (-not (Test-Path -LiteralPath $agentWrittenFilesPath)) {
            throw "Agent visual write record is missing: $agentWrittenFilesPath"
        }

        $written = Get-Content -LiteralPath $agentWrittenFilesPath -Raw | ConvertFrom-Json
        if ($null -eq $written.files -or @($written.files).Count -lt 1) {
            throw "Agent visual write record has no files."
        }
    }

    $qaArguments = @((Join-Path $builderRoot "scripts\qa\run-visual-qa.mjs"), "--project-root", $resolvedProjectRoot, "--screenshot-root", $resolvedScreenshotRoot)
    if (-not [string]::IsNullOrWhiteSpace($resolvedFixtureRoot)) {
        $qaArguments += @("--fixture-root", $resolvedFixtureRoot)
    }
    if (-not [string]::IsNullOrWhiteSpace($BaseUrl)) {
        $qaArguments += @("--base-url", $BaseUrl)
    }

    $qaPassed = Invoke-GateCommand -Name "run visual QA" -FileName "node" -Arguments $qaArguments -LikelyCause "Browser evidence found a visual defect or the generated storefront was not reachable." -AllowFailure

    if (-not $qaPassed -and -not $SkipRepair -and $MaxRepairAttempts -gt 0) {
        for ($attempt = 1; $attempt -le $MaxRepairAttempts -and -not $qaPassed; $attempt++) {
            $repairPassed = Invoke-GateCommand -Name "run bounded repair attempt $attempt" -FileName "node" -Arguments @(
                (Join-Path $builderRoot "scripts\qa\repair-visual-generation.mjs"),
                "--project-root", $resolvedProjectRoot,
                "--failure-report", $visualQaReportPath,
                "--max-attempts", $MaxRepairAttempts
            ) -LikelyCause "The visual failure is outside bounded generated-owned repair patterns." -AllowFailure

            if (-not $repairPassed) {
                break
            }

            $qaPassed = Invoke-GateCommand -Name "rerun visual QA after repair $attempt" -FileName "node" -Arguments $qaArguments -LikelyCause "Visual QA still reports issues after bounded repair." -AllowFailure
        }
    }
    elseif (-not $qaPassed) {
        Add-GateStep -Name "run bounded repair" -Status "skipped" -Command "repair skipped" -Problem "Visual QA failed and repair was disabled." -LikelyCause "Rerun without -SkipRepair or fix the reported visual issue manually." -RerunCommand (New-RerunCommand)
    }
    else {
        Add-GateStep -Name "run bounded repair" -Status "skipped" -Command "repair skipped" -ReportPath $visualQaReportPath
        Add-GateStep -Name "rerun visual QA after repair" -Status "skipped" -Command "visual QA already passed"
    }

    if (-not $qaPassed) {
        throw "Visual QA did not pass. Report: $visualQaReportPath. Evidence: $resolvedScreenshotRoot"
    }

    Invoke-GateCommand -Name "run regeneration WhatIf" -FileName "powershell" -Arguments @(
        "-NoProfile", "-ExecutionPolicy", "Bypass",
        "-File", (Join-Path $builderRoot "regenerate-storefront.ps1"),
        "-ProjectRoot", $resolvedProjectRoot,
        "-Scope", "all",
        "-WhatIf"
    ) -LikelyCause "Generated ownership, regeneration metadata, or handoff plan identity drifted."

    $finalDecision = "passed"
    Save-GateReports -Status $finalDecision
    Write-Host "Phase 4 MVP gate passed. Report: $(Convert-ToRepoRelativePath $reportMdPath)"
}
catch {
    Save-GateReports -Status $finalDecision
    Write-Error "Phase 4 MVP gate failed. Problem: $($_.Exception.Message). Likely cause: see $(Convert-ToRepoRelativePath $reportMdPath). Rerun: $(New-RerunCommand). Report: $(Convert-ToRepoRelativePath $reportMdPath). Evidence: $(Convert-ToRepoRelativePath $resolvedScreenshotRoot)"
    exit 1
}

param(
    [string]$PilotGeneratedProjectRoot = "obj\storefront-builder\generated\BlazorShop.Storefront.Phase4VisualPilot",
    [string]$PilotFixtureRoot = "obj\storefront-builder\generated\BlazorShop.Storefront.Phase4VisualPilot\docs\storefront-analysis\visual-fixtures",
    [string]$PilotHandoffRoot = "obj\storefront-reverse-engineering\portable-handoff\root-006c38f3058b44fc8791e7298a99c36e",
    [string]$GeneratedProofOutputRoot = "obj\storefront-builder\generated\phase4-final-closure",
    [int]$CommandTimeoutSeconds = 900,
    [switch]$Help
)

$ErrorActionPreference = "Stop"

function Show-Help {
    Write-Host "Storefront Phase 4 final closure gate"
    Write-Host ""
    Write-Host "Usage:"
    Write-Host "  powershell -NoProfile -ExecutionPolicy Bypass -File scripts\qa\run-storefront-phase4-final-closure-gate.ps1 [options]"
    Write-Host ""
    Write-Host "Options:"
    Write-Host "  -PilotGeneratedProjectRoot <path>  Pilot generated storefront root."
    Write-Host "  -PilotFixtureRoot <path>           Pilot visual fixture root."
    Write-Host "  -PilotHandoffRoot <path>           Pilot portable handoff root."
    Write-Host "  -GeneratedProofOutputRoot <path>   Disposable generated proof output root."
    Write-Host "  -CommandTimeoutSeconds <sec>       Timeout for each external command. Defaults to 900."
    Write-Host "  -Help                              Show this help text."
    Write-Host ""
    Write-Host "This gate is local-only and does not invoke GitHub Actions."
}

if ($Help) {
    Show-Help
    exit 0
}

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
$visualRoot = Join-Path $repoRoot "tools\BlazorShop.AI.Visual"
$builderRoot = Join-Path $repoRoot "tools\BlazorShop.AI.StorefrontBuilder"
$reportRoot = Join-Path $repoRoot "obj\storefront-builder\reports"
$startedUtc = [DateTimeOffset]::UtcNow.ToString("o")
$steps = [System.Collections.Generic.List[object]]::new()
$testedHead = ""
$finalHead = ""
$finalDecision = "failed"

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
    $rootWithSeparator = $repoRoot.TrimEnd("\", "/") + [System.IO.Path]::DirectorySeparatorChar
    if ($fullPath.Equals($repoRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
        return "."
    }

    if ($fullPath.StartsWith($rootWithSeparator, [System.StringComparison]::OrdinalIgnoreCase)) {
        return $fullPath.Substring($rootWithSeparator.Length).Replace("\", "/")
    }

    return $fullPath.Replace("\", "/")
}

function Convert-ToProcessArgument {
    param([string]$Argument)

    if ($null -eq $Argument) {
        return '""'
    }

    if ($Argument -notmatch '[\s"]') {
        return $Argument
    }

    return '"' + $Argument.Replace('\', '\\').Replace('"', '\"') + '"'
}

function Get-PreferredPowerShell {
    $pwsh = Get-Command "pwsh" -ErrorAction SilentlyContinue
    if ($null -ne $pwsh) {
        return $pwsh.Source
    }

    return "powershell"
}

function Add-GateStep {
    param(
        [string]$Name,
        [string]$Status,
        [string]$Command,
        [string]$Problem = "",
        [string]$LikelyCause = ""
    )

    $entry = [ordered]@{
        name = $Name
        status = $Status
        command = $Command
    }

    if (-not [string]::IsNullOrWhiteSpace($Problem)) { $entry.problem = $Problem }
    if (-not [string]::IsNullOrWhiteSpace($LikelyCause)) { $entry.likelyCause = $LikelyCause }
    $steps.Add([pscustomobject]$entry)
}

function Assert-CleanWorkingTree {
    $status = (& git status --porcelain=v1)
    if (-not [string]::IsNullOrWhiteSpace(($status -join "`n"))) {
        throw "Working tree must be clean before running the Phase 4 final closure gate. Dirty entries: $($status -join '; ')"
    }
}

function Assert-HeadUnchanged {
    $finalHead = (& git rev-parse HEAD).Trim()
    if ($finalHead -ne $testedHead) {
        throw "HEAD changed during the gate. Started at $testedHead and ended at $finalHead."
    }
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
        Add-GateStep -Name $Name -Status "failed" -Command $Command -Problem $_.Exception.Message -LikelyCause $LikelyCause
        throw
    }
}

function Invoke-GateCommand {
    param(
        [string]$Name,
        [string]$FileName,
        [string[]]$Arguments,
        [string]$LikelyCause
    )

    $commandText = "$FileName $($Arguments -join ' ')"
    Write-Host "== $Name =="

    $process = [System.Diagnostics.Process]::new()
    $process.StartInfo.FileName = $FileName
    $process.StartInfo.WorkingDirectory = $repoRoot
    $process.StartInfo.UseShellExecute = $false
    $process.StartInfo.RedirectStandardOutput = $false
    $process.StartInfo.RedirectStandardError = $false
    $process.StartInfo.Arguments = (($Arguments | ForEach-Object { Convert-ToProcessArgument $_ }) -join " ")

    [void]$process.Start()
    if (-not $process.WaitForExit($CommandTimeoutSeconds * 1000)) {
        try { $process.Kill() } catch { }
        $problem = "Command timed out after $CommandTimeoutSeconds seconds."
        Add-GateStep -Name $Name -Status "failed" -Command $commandText -Problem $problem -LikelyCause $LikelyCause
        throw $problem
    }

    if ($process.ExitCode -ne 0) {
        $problem = "Command exited with code $($process.ExitCode)."
        Add-GateStep -Name $Name -Status "failed" -Command $commandText -Problem $problem -LikelyCause $LikelyCause
        throw $problem
    }

    Add-GateStep -Name $Name -Status "passed" -Command $commandText
}

function Save-GateReports {
    param([string]$Status, [string]$ErrorMessage = "")

    New-Item -ItemType Directory -Force -Path $reportRoot | Out-Null
    $suffix = if ($Status -eq "passed") { "" } else { "-failed" }
    $stamp = Get-Date -Format "yyyyMMddHHmmss"
    $jsonPath = Join-Path $reportRoot "phase4-final-closure-gate$suffix-$stamp.json"
    $mdPath = Join-Path $reportRoot "phase4-final-closure-gate$suffix-$stamp.md"

    $report = [ordered]@{
        schemaVersion = "0.1.0"
        commandMetadata = [ordered]@{
            command = "powershell -NoProfile -ExecutionPolicy Bypass -File scripts\qa\run-storefront-phase4-final-closure-gate.ps1"
            startedUtc = $startedUtc
            finishedUtc = [DateTimeOffset]::UtcNow.ToString("o")
        }
        testedHead = $testedHead
        finalHead = $finalHead
        gateSteps = @($steps)
        finalDecision = $Status
        errorMessage = $ErrorMessage
    }

    Set-Content -LiteralPath $jsonPath -Value ($report | ConvertTo-Json -Depth 20) -Encoding UTF8

    $lines = [System.Collections.Generic.List[string]]::new()
    $lines.Add("# Storefront Phase 4 Final Closure Gate")
    $lines.Add("")
    $lines.Add("- Decision: $Status")
    $lines.Add("- Tested HEAD: $testedHead")
    $lines.Add("- Final HEAD: $finalHead")
    $lines.Add("- GitHub Actions: not required")
    if (-not [string]::IsNullOrWhiteSpace($ErrorMessage)) {
        $lines.Add("- Error: $ErrorMessage")
    }
    $lines.Add("")
    $lines.Add("## Steps")
    foreach ($step in $steps) {
        $lines.Add("")
        $lines.Add("- $($step.status): $($step.name)")
        $lines.Add(('  - command: `{0}`' -f $step.command))
        if ($step.PSObject.Properties.Name -contains "problem") { $lines.Add("  - problem: $($step.problem)") }
        if ($step.PSObject.Properties.Name -contains "likelyCause") { $lines.Add("  - likely cause: $($step.likelyCause)") }
    }

    Set-Content -LiteralPath $mdPath -Value $lines -Encoding UTF8
    return $mdPath
}

try {
    Set-Location $repoRoot
    $testedHead = (& git rev-parse HEAD).Trim()
    $finalHead = $testedHead

    Invoke-AssertionStep -Name "clean working tree at start" -Command "git status --porcelain=v1" -LikelyCause "Commit, stash, or remove pending source changes before running the final closure gate." -Assertion {
        Assert-CleanWorkingTree
    }

    Invoke-AssertionStep -Name "visual workspace static checks" -Command "tools/BlazorShop.AI.Visual static assertions" -LikelyCause "The visual workspace contract drifted." -Assertion {
        if (Get-ChildItem -LiteralPath $visualRoot -Recurse -Filter "*.csproj" -File) {
            throw "tools/BlazorShop.AI.Visual must not contain a .csproj."
        }

        $runtimeReferenceMatches = & rg -n "ProjectReference|PackageReference|dotnet add reference|BlazorShop\.Domain|BlazorShop\.Application|BlazorShop\.Infrastructure|BlazorShop\.PresentationV2" $visualRoot -S
        if ($LASTEXITCODE -eq 0) {
            throw "tools/BlazorShop.AI.Visual contains runtime reference text: $($runtimeReferenceMatches -join '; ')"
        }
        elseif ($LASTEXITCODE -gt 1) {
            throw "Static search failed while checking runtime references."
        }

        foreach ($path in @(
            "skills\storefront-visual-plan\SKILL.md",
            "skills\storefront-visual-implement\SKILL.md",
            "skills\storefront-visual-qa\SKILL.md",
            "adapters\codex\README.md",
            "adapters\claude\README.md",
            "schemas\visual-plan.schema.json",
            "schemas\visual-implementation-checklist.schema.json",
            "schemas\visual-implementation-report.schema.json",
            "schemas\visual-checkpoint.schema.json",
            "schemas\visual-qa-report.schema.json",
            "schemas\phase4-mvp-gate-report.schema.json"
        )) {
            $fullPath = Join-Path $visualRoot $path
            if (-not (Test-Path -LiteralPath $fullPath)) {
                throw "Required visual workspace file is missing: $(Convert-ToRepoRelativePath $fullPath)"
            }
        }

        foreach ($adapter in @("adapters\codex\README.md", "adapters\claude\README.md")) {
            $content = Get-Content -LiteralPath (Join-Path $visualRoot $adapter) -Raw
            if ($content.IndexOf("tools/BlazorShop.AI.Visual/skills/", [System.StringComparison]::Ordinal) -lt 0) {
                throw "Adapter does not point to canonical skill path: $adapter"
            }
        }
    }

    Invoke-GateCommand -Name "validate visual schema examples" -FileName "node" -Arguments @(
        "tools\BlazorShop.AI.Visual\scripts\validate-visual-examples.mjs"
    ) -LikelyCause "A visual schema or example artifact is invalid."

    Invoke-AssertionStep -Name "StorefrontBuilder visual helper availability" -Command "StorefrontBuilder helper file checks" -LikelyCause "A required StorefrontBuilder Phase 4 helper is missing." -Assertion {
        foreach ($path in @(
            "scripts\generate\record-agent-visual-writes.mjs",
            "scripts\qa\run-visual-qa.mjs",
            "scripts\qa\repair-visual-generation.mjs",
            "scripts\validate\Test-StorefrontBuilderHandoffBoundary.mjs"
        )) {
            $fullPath = Join-Path $builderRoot $path
            if (-not (Test-Path -LiteralPath $fullPath)) {
                throw "Required StorefrontBuilder helper is missing: $(Convert-ToRepoRelativePath $fullPath)"
            }
        }
    }

    Invoke-GateCommand -Name "run StorefrontBuilder generated proof" -FileName (Get-PreferredPowerShell) -Arguments @(
        "-NoProfile", "-ExecutionPolicy", "Bypass",
        "-File", "scripts\qa\run-storefront-builder-generated-proof.ps1",
        "-Name", "BlazorShop.Storefront.Phase4FinalProof",
        "-OutputRoot", $GeneratedProofOutputRoot,
        "-ProofLevel", "Structure"
    ) -LikelyCause "Generated proof, package boundary, isolation, or regeneration lifecycle failed."

    Invoke-GateCommand -Name "run StorefrontBuilder regeneration ownership gate" -FileName (Get-PreferredPowerShell) -Arguments @(
        "-NoProfile", "-ExecutionPolicy", "Bypass",
        "-File", "scripts\qa\run-storefront-builder-regeneration-gate.ps1"
    ) -LikelyCause "Regeneration/no-op ownership safety failed."

    Invoke-GateCommand -Name "run Phase 4 MVP pilot gate" -FileName (Get-PreferredPowerShell) -Arguments @(
        "-NoProfile", "-ExecutionPolicy", "Bypass",
        "-File", "scripts\qa\run-storefront-phase4-mvp-gate.ps1",
        "-GeneratedProjectRoot", $PilotGeneratedProjectRoot,
        "-FixtureRoot", $PilotFixtureRoot,
        "-HandoffRoot", $PilotHandoffRoot,
        "-SkipRepair",
        "-CommandTimeoutSeconds", $CommandTimeoutSeconds
    ) -LikelyCause "The pilot generated storefront no longer proves the Phase 4 MVP workflow."

    Invoke-AssertionStep -Name "final HEAD and clean tree check" -Command "git rev-parse HEAD; git status --porcelain=v1" -LikelyCause "A gate step changed tracked source files or HEAD." -Assertion {
        Assert-HeadUnchanged
        Assert-CleanWorkingTree
    }

    $finalDecision = "passed"
    $reportPath = Save-GateReports -Status $finalDecision
    Write-Host "Phase 4 final closure gate passed. Report: $(Convert-ToRepoRelativePath $reportPath)"
}
catch {
    $finalHead = (& git rev-parse HEAD).Trim()
    $reportPath = Save-GateReports -Status $finalDecision -ErrorMessage $_.Exception.Message
    Write-Error "Phase 4 final closure gate failed. Report: $(Convert-ToRepoRelativePath $reportPath). Error: $($_.Exception.Message)"
    exit 1
}

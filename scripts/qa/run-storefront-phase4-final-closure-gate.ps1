param(
    [string]$ClosureFixtureRoot = "tools\BlazorShop.AI.StorefrontBuilder\tests\generation\fixtures\phase4-11-closure",
    [string]$PilotGeneratedOutputRoot = "obj\storefront-builder\generated\phase4-11-closure-pilot",
    [string]$PilotProjectName = "BlazorShop.Storefront.Phase411ClosurePilot",
    [string]$PilotStoreKey = "sample",
    [string]$PilotGeneratedProjectRoot = "",
    [string]$PilotHandoffRoot = "",
    [string]$PilotHandoffSchemaRoot = "",
    [string]$PilotBaseUrl = "http://127.0.0.1:18620",
    [string]$GeneratedProofOutputRoot = "obj\storefront-builder\generated\phase4-final-closure",
    [ValidateSet("FoundationFunctionalFast", "FoundationFunctionalFull")]
    [string]$FunctionalProofLevel = "FoundationFunctionalFast",
    [switch]$SkipFullFixtureProof,
    [switch]$RequireCommerceRegression,
    [switch]$KeepGeneratedPilot,
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
    Write-Host "  -ClosureFixtureRoot <path>         Tracked Phase 4.11 closure fixture root."
    Write-Host "  -PilotGeneratedOutputRoot <path>   Disposable fresh pilot output root under obj/storefront-builder/generated."
    Write-Host "  -PilotProjectName <name>           Fresh pilot project name."
    Write-Host "  -PilotStoreKey <key>               Fresh pilot store key."
    Write-Host "  -PilotGeneratedProjectRoot <path>  Optional override for pilot generated storefront root."
    Write-Host "  -PilotHandoffRoot <path>           Optional override for tracked portable handoff package root."
    Write-Host "  -PilotHandoffSchemaRoot <path>     Optional override for portable handoff schema root."
    Write-Host "  -PilotBaseUrl <url>                Running pilot generated storefront URL for runtime MVP visual proof."
    Write-Host "  -GeneratedProofOutputRoot <path>   Disposable generated proof output root."
    Write-Host "  -FunctionalProofLevel <level>      FoundationFunctionalFast or FoundationFunctionalFull. Defaults to FoundationFunctionalFast."
    Write-Host "  -SkipFullFixtureProof              Local-development escape hatch; invalid with FoundationFunctionalFull or -RequireCommerceRegression."
    Write-Host "  -RequireCommerceRegression         Require the full fixture wrapper, which runs run-commerce-regression.mjs."
    Write-Host "  -KeepGeneratedPilot                Keep disposable fresh pilot output after success."
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
$evidencePaths = [System.Collections.Generic.List[string]]::new()
$testedHead = ""
$finalHead = ""
$finalDecision = "failed"
$runFullFixtureProof = $FunctionalProofLevel -eq "FoundationFunctionalFull" -or $RequireCommerceRegression
$generatedPilotRetained = $true
$runtimeHostProcess = $null
$runtimeCommerceFixtureProcess = $null
$runtimeCommerceFixtureUrl = ""

if ($SkipFullFixtureProof -and $runFullFixtureProof) {
    throw "-SkipFullFixtureProof cannot be combined with -FunctionalProofLevel FoundationFunctionalFull or -RequireCommerceRegression."
}

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

function Get-FreeTcpPort {
    $listener = [System.Net.Sockets.TcpListener]::new([System.Net.IPAddress]::Parse("127.0.0.1"), 0)
    try {
        $listener.Start()
        return $listener.LocalEndpoint.Port
    }
    finally {
        $listener.Stop()
    }
}

function Test-TcpEndpoint {
    param([string]$Url)

    $uri = [System.Uri]::new($Url)
    $client = [System.Net.Sockets.TcpClient]::new()
    try {
        $asyncResult = $client.BeginConnect($uri.Host, $uri.Port, $null, $null)
        if (-not $asyncResult.AsyncWaitHandle.WaitOne(1000)) {
            return $false
        }

        $client.EndConnect($asyncResult)
        return $true
    }
    catch {
        return $false
    }
    finally {
        $client.Close()
    }
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

function Add-EvidencePath {
    param([string]$Path)

    if ([string]::IsNullOrWhiteSpace($Path)) {
        return
    }

    $relative = Convert-ToRepoRelativePath (Resolve-RepoPath $Path)
    if (-not $evidencePaths.Contains($relative)) {
        $evidencePaths.Add($relative)
    }
}

function Copy-DirectoryContents {
    param(
        [Parameter(Mandatory = $true)][string]$Source,
        [Parameter(Mandatory = $true)][string]$Destination
    )

    if (-not (Test-Path -LiteralPath $Source)) {
        throw "Source directory is missing: $Source"
    }

    New-Item -ItemType Directory -Force -Path $Destination | Out-Null
    Get-ChildItem -LiteralPath $Source -Force | ForEach-Object {
        Copy-Item -LiteralPath $_.FullName -Destination $Destination -Recurse -Force
    }
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

function Start-RuntimeCommerceFixture {
    if ($null -ne $script:runtimeCommerceFixtureProcess -and -not $script:runtimeCommerceFixtureProcess.HasExited) {
        return $script:runtimeCommerceFixtureUrl
    }

    New-Item -ItemType Directory -Force -Path $pilotAnalysisRoot | Out-Null
    foreach ($path in @($runtimeCommerceFixtureReadyPath, $runtimeCommerceFixtureOutputPath, $runtimeCommerceFixtureErrorPath)) {
        if (Test-Path -LiteralPath $path) {
            Remove-Item -LiteralPath $path -Force
        }
    }

    $fixturePort = Get-FreeTcpPort
    $fixtureScript = Join-Path $builderRoot "scripts\qa\start-fast-commerce-fixture.mjs"
    $arguments = @(
        $fixtureScript,
        "--store-key", $PilotStoreKey,
        "--port", "$fixturePort",
        "--ready-file", $runtimeCommerceFixtureReadyPath
    )
    $argumentText = (($arguments | ForEach-Object { Convert-ToProcessArgument $_ }) -join " ")

    $script:runtimeCommerceFixtureProcess = Start-Process `
        -FilePath "node" `
        -ArgumentList $argumentText `
        -WorkingDirectory $repoRoot `
        -WindowStyle Hidden `
        -RedirectStandardOutput $runtimeCommerceFixtureOutputPath `
        -RedirectStandardError $runtimeCommerceFixtureErrorPath `
        -PassThru

    $deadline = [DateTimeOffset]::UtcNow.AddSeconds([Math]::Min($CommandTimeoutSeconds, 45))
    do {
        if ($script:runtimeCommerceFixtureProcess.HasExited) {
            throw "Runtime Commerce fixture exited early with code $($script:runtimeCommerceFixtureProcess.ExitCode). Error log: $(Convert-ToRepoRelativePath $runtimeCommerceFixtureErrorPath)"
        }

        if (Test-Path -LiteralPath $runtimeCommerceFixtureReadyPath) {
            $ready = Get-Content -LiteralPath $runtimeCommerceFixtureReadyPath -Raw | ConvertFrom-Json
            $script:runtimeCommerceFixtureUrl = [string]$ready.url
            return $script:runtimeCommerceFixtureUrl
        }

        Start-Sleep -Milliseconds 250
    } while ([DateTimeOffset]::UtcNow -lt $deadline)

    throw "Runtime Commerce fixture did not become ready before timeout. Error log: $(Convert-ToRepoRelativePath $runtimeCommerceFixtureErrorPath)"
}

function Start-GeneratedRuntimeHost {
    param(
        [string]$ProjectFile,
        [string]$Url,
        [string]$CommerceNodeBaseUrl
    )

    if ($null -ne $script:runtimeHostProcess -and -not $script:runtimeHostProcess.HasExited) {
        return
    }

    New-Item -ItemType Directory -Force -Path $pilotAnalysisRoot | Out-Null
    foreach ($path in @($runtimeHostOutputPath, $runtimeHostErrorPath)) {
        if (Test-Path -LiteralPath $path) {
            Remove-Item -LiteralPath $path -Force
        }
    }

    $arguments = @(
        "run",
        "--project", $ProjectFile,
        "--configuration", "Debug",
        "--no-build",
        "--no-launch-profile",
        "--urls", $Url
    )
    $argumentText = (($arguments | ForEach-Object { Convert-ToProcessArgument $_ }) -join " ")

    $previousEnvironment = @{
        ASPNETCORE_ENVIRONMENT = $env:ASPNETCORE_ENVIRONMENT
        DOTNET_ENVIRONMENT = $env:DOTNET_ENVIRONMENT
        Storefront__CommerceNodeBaseUrl = $env:Storefront__CommerceNodeBaseUrl
        Storefront__StoreKey = $env:Storefront__StoreKey
        Storefront__PublicBaseUrl = $env:Storefront__PublicBaseUrl
        PublicUrl__BaseUrl = $env:PublicUrl__BaseUrl
        ClientApp__BaseUrl = $env:ClientApp__BaseUrl
    }

    try {
        $env:ASPNETCORE_ENVIRONMENT = "Development"
        $env:DOTNET_ENVIRONMENT = "Development"
        $env:Storefront__CommerceNodeBaseUrl = $CommerceNodeBaseUrl
        $env:Storefront__StoreKey = $PilotStoreKey
        $env:Storefront__PublicBaseUrl = $Url
        $env:PublicUrl__BaseUrl = $Url
        $env:ClientApp__BaseUrl = $Url

        $script:runtimeHostProcess = Start-Process `
            -FilePath "dotnet" `
            -ArgumentList $argumentText `
            -WorkingDirectory $resolvedPilotGeneratedProjectRoot `
            -WindowStyle Hidden `
            -RedirectStandardOutput $runtimeHostOutputPath `
            -RedirectStandardError $runtimeHostErrorPath `
            -PassThru
    }
    finally {
        foreach ($key in $previousEnvironment.Keys) {
            if ($null -eq $previousEnvironment[$key]) {
                Remove-Item -Path "Env:$key" -ErrorAction SilentlyContinue
            }
            else {
                Set-Item -Path "Env:$key" -Value $previousEnvironment[$key]
            }
        }
    }

    $deadline = [DateTimeOffset]::UtcNow.AddSeconds([Math]::Min($CommandTimeoutSeconds, 90))
    do {
        if ($script:runtimeHostProcess.HasExited) {
            throw "Generated runtime host exited early with code $($script:runtimeHostProcess.ExitCode). Error log: $(Convert-ToRepoRelativePath $runtimeHostErrorPath)"
        }

        if (Test-TcpEndpoint -Url $Url) {
            return
        }

        Start-Sleep -Seconds 1
    } while ([DateTimeOffset]::UtcNow -lt $deadline)

    throw "Generated runtime host did not become reachable at $Url before timeout. Error log: $(Convert-ToRepoRelativePath $runtimeHostErrorPath)"
}

function Stop-GeneratedRuntimeHost {
    if ($null -eq $script:runtimeHostProcess -or $script:runtimeHostProcess.HasExited) {
        return
    }

    try {
        $script:runtimeHostProcess.Kill()
        $script:runtimeHostProcess.WaitForExit(10000) | Out-Null
    }
    catch {
    }
}

function Stop-RuntimeCommerceFixture {
    if ($null -eq $script:runtimeCommerceFixtureProcess -or $script:runtimeCommerceFixtureProcess.HasExited) {
        return
    }

    try {
        $script:runtimeCommerceFixtureProcess.Kill()
        $script:runtimeCommerceFixtureProcess.WaitForExit(10000) | Out-Null
    }
    catch {
    }
}

function Read-RequiredJsonArtifact {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$ArtifactName
    )

    if (-not (Test-Path -LiteralPath $Path)) {
        throw "$ArtifactName is missing: $(Convert-ToRepoRelativePath $Path)"
    }

    try {
        return Get-Content -LiteralPath $Path -Raw | ConvertFrom-Json
    }
    catch {
        throw "$ArtifactName is not valid JSON: $(Convert-ToRepoRelativePath $Path). $($_.Exception.Message)"
    }
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

function Get-NormalizedFileSha256 {
    param([string]$Path)

    if (-not (Test-Path -LiteralPath $Path)) {
        throw "Hash input is missing: $(Convert-ToRepoRelativePath $Path)"
    }

    $content = (Get-Content -LiteralPath $Path -Raw).Replace("`r`n", "`n").Replace("`r", "`n")
    $bytes = [System.Text.Encoding]::UTF8.GetBytes($content)
    $sha256 = [System.Security.Cryptography.SHA256]::Create()
    try {
        $hash = $sha256.ComputeHash($bytes)
        return "sha256:" + [System.BitConverter]::ToString($hash).Replace("-", "").ToLowerInvariant()
    }
    finally {
        $sha256.Dispose()
    }
}

function Assert-HandoffGeneratedArtifacts {
    param([string]$ProjectRoot)

    $analysisRoot = Join-Path $ProjectRoot "docs\storefront-analysis"
    $metadataPath = Join-Path $analysisRoot "metadata.yaml"
    $generationPlanPath = Join-Path $analysisRoot "generation-plan.json"
    $taskPackageManifestPath = Join-Path $analysisRoot "agent-task-package\manifest.json"

    if (-not (Test-Path -LiteralPath $metadataPath)) {
        throw "Generated metadata.yaml is missing: $(Convert-ToRepoRelativePath $metadataPath)"
    }

    $metadata = Get-Content -LiteralPath $metadataPath -Raw
    $generationMode = Read-SimpleYamlValue -Text $metadata -Key "generationMode"
    if ($generationMode -ne "handoff-project-skeleton") {
        throw "metadata.yaml generationMode must be handoff-project-skeleton for final closure, but was '$generationMode'."
    }

    foreach ($required in @("planPath:", "sourceHandoffPackageHash:", "sourceHandoffReadinessHash:")) {
        if ($metadata.IndexOf($required, [System.StringComparison]::Ordinal) -lt 0) {
            throw "metadata.yaml handoffGeneration is missing '$required'."
        }
    }

    $generationPlan = Read-RequiredJsonArtifact -Path $generationPlanPath -ArtifactName "generation-plan.json"
    if ([string]$generationPlan.generationMode -ne "handoff") {
        throw "generation-plan.json generationMode must be handoff for final closure, but was '$($generationPlan.generationMode)'."
    }

    $taskPackage = Read-RequiredJsonArtifact -Path $taskPackageManifestPath -ArtifactName "agent-task-package/manifest.json"
    if ([string]$taskPackage.artifactKind -ne "agent-visual-task-package") {
        throw "agent-task-package/manifest.json artifactKind must be agent-visual-task-package, but was '$($taskPackage.artifactKind)'."
    }

    $actualPlanHash = Get-NormalizedFileSha256 -Path $generationPlanPath
    if ([string]$taskPackage.generationPlanHash -ne $actualPlanHash) {
        throw "agent-task-package generationPlanHash '$($taskPackage.generationPlanHash)' does not match actual generation plan hash '$actualPlanHash'."
    }

    Add-EvidencePath $metadataPath
    Add-EvidencePath $generationPlanPath
    Add-EvidencePath $taskPackageManifestPath
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
        functionalProofLevel = $FunctionalProofLevel
        requireCommerceRegression = [bool]$RequireCommerceRegression
        skipFullFixtureProof = [bool]$SkipFullFixtureProof
        closureFixtureRoot = Convert-ToRepoRelativePath $resolvedClosureFixtureRoot
        pilotGeneratedProjectRoot = Convert-ToRepoRelativePath $resolvedPilotGeneratedProjectRoot
        pilotHandoffRoot = Convert-ToRepoRelativePath $resolvedPilotHandoffRoot
        generatedPilotRetained = $generatedPilotRetained
        evidencePaths = @($evidencePaths)
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
    $lines.Add("- Functional proof level: $FunctionalProofLevel")
    $lines.Add("- Require commerce regression: $([bool]$RequireCommerceRegression)")
    $lines.Add("- Skip full fixture proof: $([bool]$SkipFullFixtureProof)")
    $lines.Add("- Closure fixture root: $(Convert-ToRepoRelativePath $resolvedClosureFixtureRoot)")
    $lines.Add("- Pilot generated project root: $(Convert-ToRepoRelativePath $resolvedPilotGeneratedProjectRoot)")
    $lines.Add("- Pilot handoff root: $(Convert-ToRepoRelativePath $resolvedPilotHandoffRoot)")
    $lines.Add("- Generated pilot retained: $generatedPilotRetained")
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
    $lines.Add("")
    $lines.Add("## Evidence Paths")
    if ($evidencePaths.Count -eq 0) {
        $lines.Add("")
        $lines.Add("- None recorded.")
    }
    else {
        foreach ($path in $evidencePaths) {
            $lines.Add("")
            $lines.Add("- $path")
        }
    }

    Set-Content -LiteralPath $mdPath -Value $lines -Encoding UTF8
    return $mdPath
}

$resolvedClosureFixtureRoot = Resolve-RepoPath $ClosureFixtureRoot
$resolvedPilotGeneratedOutputRoot = Resolve-RepoPath $PilotGeneratedOutputRoot
$resolvedPilotGeneratedProjectRoot = if ([string]::IsNullOrWhiteSpace($PilotGeneratedProjectRoot)) {
    Join-Path $resolvedPilotGeneratedOutputRoot $PilotProjectName
} else {
    Resolve-RepoPath $PilotGeneratedProjectRoot
}
$resolvedPilotHandoffRoot = if ([string]::IsNullOrWhiteSpace($PilotHandoffRoot)) {
    Join-Path $resolvedClosureFixtureRoot "portable-handoff"
} else {
    Resolve-RepoPath $PilotHandoffRoot
}
$resolvedPilotHandoffSchemaRoot = if ([string]::IsNullOrWhiteSpace($PilotHandoffSchemaRoot)) {
    Join-Path $repoRoot "tools\BlazorShop.AI.StorefrontReverseEngineering\Schemas"
} else {
    Resolve-RepoPath $PilotHandoffSchemaRoot
}
$pilotAnalysisRoot = Join-Path $resolvedPilotGeneratedProjectRoot "docs\storefront-analysis"
$pilotProjectFile = Join-Path $resolvedPilotGeneratedProjectRoot "$PilotProjectName.csproj"
$pilotScreenshotRoot = Join-Path $pilotAnalysisRoot "visual-qa"
$runtimeHostOutputPath = Join-Path $pilotAnalysisRoot "phase4-final-runtime-host.out.log"
$runtimeHostErrorPath = Join-Path $pilotAnalysisRoot "phase4-final-runtime-host.err.log"
$runtimeCommerceFixtureReadyPath = Join-Path $pilotAnalysisRoot "phase4-final-commerce-fixture.ready.json"
$runtimeCommerceFixtureOutputPath = Join-Path $pilotAnalysisRoot "phase4-final-commerce-fixture.out.log"
$runtimeCommerceFixtureErrorPath = Join-Path $pilotAnalysisRoot "phase4-final-commerce-fixture.err.log"

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

    Invoke-AssertionStep -Name "validate tracked Phase 4.11 closure fixture" -Command "Test-Path tracked closure fixture artifacts" -LikelyCause "The tracked StorefrontBuilder Phase 4.11 closure fixture was moved or is incomplete." -Assertion {
        foreach ($path in @(
            "closure-fixture.json",
            "visual-artifacts\visual-plan.json",
            "visual-artifacts\visual-implementation-checklist.json",
            "visual-artifacts\visual-implementation-report.json",
            "visual-artifacts\agent-written-files.json",
            "visual-artifacts\visual-checkpoints\phase4-11-closure-pilot\visual-checkpoint.json",
            "reference\home-desktop.reference.md",
            "portable-handoff\analysis\agent-handoff\manifest.json"
        )) {
            $fullPath = Join-Path $resolvedClosureFixtureRoot $path
            if (-not (Test-Path -LiteralPath $fullPath)) {
                throw "Required tracked closure fixture artifact is missing: $(Convert-ToRepoRelativePath $fullPath)"
            }
        }
    }

    Invoke-GateCommand -Name "run StorefrontBuilder handoff preflight" -FileName (Get-PreferredPowerShell) -Arguments @(
        "-NoProfile", "-ExecutionPolicy", "Bypass",
        "-File", "tools\BlazorShop.AI.StorefrontBuilder\build-storefront.ps1",
        "-Name", $PilotProjectName,
        "-StoreKey", $PilotStoreKey,
        "-Mode", "preflight-only",
        "-HandoffRoot", $resolvedPilotHandoffRoot,
        "-HandoffSchemaRoot", $resolvedPilotHandoffSchemaRoot
    ) -LikelyCause "Tracked portable handoff fixture failed StorefrontBuilder preflight."

    $preflightReportRoot = Join-Path $repoRoot "obj\storefront-builder\handoff-preflight"
    $safePilotName = $PilotProjectName -replace "[^A-Za-z0-9_.-]", "_"
    $handoffPreflightReport = Get-ChildItem -LiteralPath $preflightReportRoot -Filter "handoff-preflight-$safePilotName-*.md" -File -ErrorAction SilentlyContinue |
        Sort-Object LastWriteTime -Descending |
        Select-Object -First 1
    if ($null -ne $handoffPreflightReport) {
        Add-EvidencePath $handoffPreflightReport.FullName
    }

    Invoke-AssertionStep -Name "StorefrontBuilder visual helper availability" -Command "StorefrontBuilder helper file checks" -LikelyCause "A required StorefrontBuilder Phase 4 helper is missing." -Assertion {
        foreach ($path in @(
            "scripts\generate\record-agent-visual-writes.mjs",
            "scripts\generate\apply-final-closure-visual-fixture-edit.mjs",
            "scripts\qa\run-visual-qa.mjs",
            "scripts\qa\materialize-reference-visual-qa-report.mjs",
            "scripts\qa\repair-visual-generation.mjs",
            "scripts\validate\Test-StorefrontBuilderHandoffBoundary.mjs"
        )) {
            $fullPath = Join-Path $builderRoot $path
            if (-not (Test-Path -LiteralPath $fullPath)) {
                throw "Required StorefrontBuilder helper is missing: $(Convert-ToRepoRelativePath $fullPath)"
            }
        }
    }

    Invoke-AssertionStep -Name "prepare fresh generated pilot output" -Command "remove stale obj pilot output" -LikelyCause "The disposable pilot output root could not be cleaned before fresh generation." -Assertion {
        if (Test-Path -LiteralPath $resolvedPilotGeneratedOutputRoot) {
            Remove-Item -LiteralPath $resolvedPilotGeneratedOutputRoot -Recurse -Force
        }

        if (Test-Path -LiteralPath $resolvedPilotGeneratedOutputRoot) {
            throw "Fresh pilot output root still exists after cleanup: $(Convert-ToRepoRelativePath $resolvedPilotGeneratedOutputRoot)"
        }
    }

    Invoke-GateCommand -Name "generate fresh Phase 4.11 pilot from tracked portable handoff fixture" -FileName (Get-PreferredPowerShell) -Arguments @(
        "-NoProfile", "-ExecutionPolicy", "Bypass",
        "-File", "tools\BlazorShop.AI.StorefrontBuilder\build-storefront.ps1",
        "-Name", $PilotProjectName,
        "-StoreKey", $PilotStoreKey,
        "-OutputRoot", $resolvedPilotGeneratedOutputRoot,
        "-Mode", "generate",
        "-HandoffRoot", $resolvedPilotHandoffRoot,
        "-HandoffSchemaRoot", $resolvedPilotHandoffSchemaRoot,
        "-Force"
    ) -LikelyCause "Fresh pilot handoff generation from Starter failed."

    Invoke-AssertionStep -Name "assert generated handoff metadata and task package" -Command "metadata/generation-plan/agent-task-package handoff checks" -LikelyCause "Generated pilot was not produced through the official handoff path." -Assertion {
        Assert-HandoffGeneratedArtifacts -ProjectRoot $resolvedPilotGeneratedProjectRoot
    }

    Invoke-AssertionStep -Name "seed tracked closure reference evidence into fresh pilot" -Command "copy tracked fixture reference artifacts" -LikelyCause "Tracked closure fixture reference artifacts could not be copied into disposable generated output." -Assertion {
        $analysisRoot = Join-Path $resolvedPilotGeneratedProjectRoot "docs\storefront-analysis"
        if (-not (Test-Path -LiteralPath $analysisRoot)) {
            throw "Generated pilot analysis root is missing: $(Convert-ToRepoRelativePath $analysisRoot)"
        }

        Copy-DirectoryContents -Source (Join-Path $resolvedClosureFixtureRoot "reference") -Destination (Join-Path $analysisRoot "reference")
        Set-Content -LiteralPath (Join-Path $analysisRoot "fresh-generation-marker.txt") -Value "fresh generated during Phase 4.11 final closure gate" -Encoding UTF8
    }

    Invoke-GateCommand -Name "apply deterministic final closure visual edit" -FileName "node" -Arguments @(
        "tools\BlazorShop.AI.StorefrontBuilder\scripts\generate\apply-final-closure-visual-fixture-edit.mjs",
        "--project-root", $resolvedPilotGeneratedProjectRoot,
        "--operation-id", "phase4-12-final-closure-pilot"
    ) -LikelyCause "The deterministic closure visual edit could not create real checkpoint evidence from generated source."
    Add-EvidencePath (Join-Path $resolvedPilotGeneratedProjectRoot "docs\storefront-analysis\visual-plan.json")
    Add-EvidencePath (Join-Path $resolvedPilotGeneratedProjectRoot "docs\storefront-analysis\visual-implementation-checklist.json")
    Add-EvidencePath (Join-Path $resolvedPilotGeneratedProjectRoot "docs\storefront-analysis\visual-checkpoints\phase4-12-final-closure-pilot\visual-checkpoint.json")
    Add-EvidencePath (Join-Path $resolvedPilotGeneratedProjectRoot "docs\storefront-analysis\visual-implementation-report.json")

    Invoke-GateCommand -Name "run automatic pilot changed-file detection" -FileName "node" -Arguments @(
        "tools\BlazorShop.AI.StorefrontBuilder\scripts\generate\record-agent-visual-writes.mjs",
        "--project-root", $resolvedPilotGeneratedProjectRoot,
        "--from-checkpoint", "docs\storefront-analysis\visual-checkpoints\phase4-12-final-closure-pilot\visual-checkpoint.json",
        "--implementation-report", "docs\storefront-analysis\visual-implementation-report.json",
        "--closure-mode"
    ) -LikelyCause "Automatic changed-file detection failed for the fresh pilot visual checkpoint."
    Add-EvidencePath (Join-Path $resolvedPilotGeneratedProjectRoot "docs\storefront-analysis\agent-written-files.json")

    Invoke-GateCommand -Name "restore generated pilot before runtime visual QA" -FileName "dotnet" -Arguments @(
        "restore", $pilotProjectFile, "--no-cache", "--force-evaluate"
    ) -LikelyCause "Generated pilot package references could not be restored before runtime visual QA."

    Invoke-GateCommand -Name "build generated pilot before runtime visual QA" -FileName "dotnet" -Arguments @(
        "build", $pilotProjectFile, "--configuration", "Debug", "--no-restore"
    ) -LikelyCause "Generated pilot visual files do not compile before runtime visual QA."

    Invoke-AssertionStep -Name "start generated pilot runtime host" -Command "dotnet run --project generated pilot --no-build" -LikelyCause "The generated pilot runtime host could not start for browser visual QA." -Assertion {
        $commerceNodeBaseUrl = Start-RuntimeCommerceFixture
        Start-GeneratedRuntimeHost -ProjectFile $pilotProjectFile -Url $PilotBaseUrl -CommerceNodeBaseUrl $commerceNodeBaseUrl
    }

    Invoke-GateCommand -Name "run runtime visual QA for current closure operation" -FileName "node" -Arguments @(
        "tools\BlazorShop.AI.StorefrontBuilder\scripts\qa\run-visual-qa.mjs",
        "--proof-mode", "runtime",
        "--project-root", $resolvedPilotGeneratedProjectRoot,
        "--screenshot-root", $pilotScreenshotRoot,
        "--base-url", $PilotBaseUrl,
        "--operation-id", "phase4-12-final-closure-pilot"
    ) -LikelyCause "Runtime browser visual QA failed before Reference QA materialization."
    Add-EvidencePath (Join-Path $pilotAnalysisRoot "visual-qa-runtime-summary.json")
    Add-EvidencePath $pilotScreenshotRoot

    Invoke-GateCommand -Name "materialize Reference QA from current runtime evidence" -FileName "node" -Arguments @(
        "tools\BlazorShop.AI.StorefrontBuilder\scripts\qa\materialize-reference-visual-qa-report.mjs",
        "--project-root", $resolvedPilotGeneratedProjectRoot,
        "--base-url", $PilotBaseUrl,
        "--operation-id", "phase4-12-final-closure-pilot"
    ) -LikelyCause "Runtime screenshots and reference evidence could not be bound into visual-qa-report.json."
    Add-EvidencePath (Join-Path $pilotAnalysisRoot "visual-qa-report.json")
    Add-EvidencePath (Join-Path $pilotAnalysisRoot "visual-qa-report.md")

    if ($FunctionalProofLevel -eq "FoundationFunctionalFast") {
        Invoke-GateCommand -Name "run StorefrontBuilder generated fast functional proof" -FileName (Get-PreferredPowerShell) -Arguments @(
            "-NoProfile", "-ExecutionPolicy", "Bypass",
            "-File", "scripts\qa\run-storefront-builder-generated-proof.ps1",
            "-Name", "BlazorShop.Storefront.Phase4FinalProof",
            "-OutputRoot", $GeneratedProofOutputRoot,
            "-ProofLevel", "FoundationFunctionalFast"
        ) -LikelyCause "Generated proof, package boundary, isolation, regeneration lifecycle, or fast browser behavior failed."
        Add-EvidencePath (Join-Path (Join-Path (Resolve-RepoPath $GeneratedProofOutputRoot) "BlazorShop.Storefront.Phase4FinalProof") "docs\storefront-analysis\fast-foundation-functional-report.md")
    }

    if ($runFullFixtureProof) {
        Invoke-GateCommand -Name "run StorefrontBuilder full fixture commerce proof" -FileName (Get-PreferredPowerShell) -Arguments @(
            "-NoProfile", "-ExecutionPolicy", "Bypass",
            "-File", "scripts\qa\run-storefront-builder-full-proof-with-fixture.ps1",
            "-Name", "BlazorShop.Storefront.Phase4FinalProof",
            "-OutputRoot", $GeneratedProofOutputRoot
        ) -LikelyCause "Full fixture runtime, generated visual QA, COD/test payment flow, or run-commerce-regression.mjs failed."
        $fullProofAnalysisRoot = Join-Path (Join-Path (Resolve-RepoPath $GeneratedProofOutputRoot) "BlazorShop.Storefront.Phase4FinalProof") "docs\storefront-analysis"
        Add-EvidencePath (Join-Path $fullProofAnalysisRoot "full-proof-with-fixture-report.md")
        Add-EvidencePath (Join-Path $fullProofAnalysisRoot "visual-qa-report.md")
        Add-EvidencePath (Join-Path $fullProofAnalysisRoot "functional-commerce-report.md")
    }

    Invoke-GateCommand -Name "run StorefrontBuilder regeneration ownership gate" -FileName (Get-PreferredPowerShell) -Arguments @(
        "-NoProfile", "-ExecutionPolicy", "Bypass",
        "-File", "scripts\qa\run-storefront-builder-regeneration-gate.ps1"
    ) -LikelyCause "Regeneration/no-op ownership safety failed."

    Invoke-GateCommand -Name "run Phase 4 MVP pilot gate" -FileName (Get-PreferredPowerShell) -Arguments @(
        "-NoProfile", "-ExecutionPolicy", "Bypass",
        "-File", "scripts\qa\run-storefront-phase4-mvp-gate.ps1",
        "-GeneratedProjectRoot", $resolvedPilotGeneratedProjectRoot,
        "-ProofMode", "Runtime",
        "-BaseUrl", $PilotBaseUrl,
        "-HandoffRoot", $resolvedPilotHandoffRoot,
        "-SkipRepair",
        "-CommandTimeoutSeconds", $CommandTimeoutSeconds
    ) -LikelyCause "The pilot generated storefront no longer proves the Phase 4 MVP workflow."
    $pilotAnalysisRoot = Join-Path $resolvedPilotGeneratedProjectRoot "docs\storefront-analysis"
    Add-EvidencePath (Join-Path $pilotAnalysisRoot "phase4-mvp-gate-report.md")
    Add-EvidencePath (Join-Path $pilotAnalysisRoot "visual-qa-report.md")

    Invoke-AssertionStep -Name "final HEAD and clean tree check" -Command "git rev-parse HEAD; git status --porcelain=v1" -LikelyCause "A gate step changed tracked source files or HEAD." -Assertion {
        Assert-HeadUnchanged
        Assert-CleanWorkingTree
    }

    Invoke-AssertionStep -Name "cleanup disposable generated pilot output" -Command "Remove-Item obj/storefront-builder/generated/phase4-11-closure-pilot" -LikelyCause "The disposable generated pilot output could not be cleaned after success." -Assertion {
        if ($KeepGeneratedPilot) {
            return
        }

        if (Test-Path -LiteralPath $resolvedPilotGeneratedOutputRoot) {
            Remove-Item -LiteralPath $resolvedPilotGeneratedOutputRoot -Recurse -Force
        }

        if (Test-Path -LiteralPath $resolvedPilotGeneratedOutputRoot) {
            throw "Disposable pilot output still exists after cleanup: $(Convert-ToRepoRelativePath $resolvedPilotGeneratedOutputRoot)"
        }

        $script:generatedPilotRetained = $false
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
finally {
    Stop-GeneratedRuntimeHost
    Stop-RuntimeCommerceFixture
}

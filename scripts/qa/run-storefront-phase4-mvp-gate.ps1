param(
    [string]$GeneratedProjectRoot = "",
    [string]$FixtureRoot = "",
    [string]$HandoffRoot = "",
    [string]$ScreenshotRoot = "",
    [int]$MaxRepairAttempts = 2,
    [switch]$SkipRepair,
    [switch]$SkeletonProof,
    [string]$ProofMode = "",
    [string]$BaseUrl = "",
    [switch]$StartRuntimeHost,
    [string]$RuntimeCommerceNodeBaseUrl = "",
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
    Write-Host "  -SkeletonProof                 Compatibility mode for early file-fixture skeleton checks; not valid for release closure."
    Write-Host "  -ProofMode <Skeleton|Runtime>  Visual QA proof mode. Closure requires Runtime; Skeleton is for early fixture proof only."
    Write-Host "  -BaseUrl <url>                 Running storefront base URL for runtime visual QA."
    Write-Host "  -StartRuntimeHost              Start and stop the generated storefront host around runtime visual QA."
    Write-Host "  -RuntimeCommerceNodeBaseUrl    Optional Commerce Node URL for the generated runtime host; defaults to a deterministic local fake fixture when -StartRuntimeHost is used."
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
        $rootWithSeparator = $repoRoot.TrimEnd("\", "/") + [System.IO.Path]::DirectorySeparatorChar
        if ($fullPath.Equals($repoRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
            return "."
        }

        if ($fullPath.StartsWith($rootWithSeparator, [System.StringComparison]::OrdinalIgnoreCase)) {
            return $fullPath.Substring($rootWithSeparator.Length).Replace("\", "/")
        }
    }

    return $fullPath.Replace("\", "/")
}

function Convert-ToRelativePath {
    param(
        [string]$BasePath,
        [string]$Path
    )

    $baseFullPath = [System.IO.Path]::GetFullPath($BasePath).TrimEnd("\", "/") + [System.IO.Path]::DirectorySeparatorChar
    $fullPath = [System.IO.Path]::GetFullPath($Path)
    if ($fullPath.StartsWith($baseFullPath, [System.StringComparison]::OrdinalIgnoreCase)) {
        return $fullPath.Substring($baseFullPath.Length).Replace("\", "/")
    }

    return $fullPath.Replace("\", "/")
}

function Get-NormalizedFileSha256 {
    param([string]$Path)

    if (-not (Test-Path -LiteralPath $Path)) {
        return "missing"
    }

    $fileBytes = [System.IO.File]::ReadAllBytes((Resolve-Path -LiteralPath $Path).Path)
    $content = ([System.Text.Encoding]::UTF8.GetString($fileBytes)).Replace("`r`n", "`n").Replace("`r", "`n")
    $bytes = [System.Text.Encoding]::UTF8.GetBytes($content)
    $sha256 = [System.Security.Cryptography.SHA256]::Create()
    try {
        $hash = $sha256.ComputeHash($bytes)
        return [System.BitConverter]::ToString($hash).Replace("-", "").ToLowerInvariant()
    }
    finally {
        $sha256.Dispose()
    }
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
            $relative = Convert-ToRelativePath -BasePath $Path -Path $_.FullName
            [void]$builder.AppendLine($relative)
            [void]$builder.AppendLine((Get-NormalizedFileSha256 -Path $_.FullName))
        }

    $bytes = [System.Text.Encoding]::UTF8.GetBytes($builder.ToString())
    $sha256 = [System.Security.Cryptography.SHA256]::Create()
    try {
        $hash = $sha256.ComputeHash($bytes)
        return [System.BitConverter]::ToString($hash).Replace("-", "").ToLowerInvariant()
    }
    finally {
        $sha256.Dispose()
    }
}

function Get-PreferredPowerShell {
    $pwsh = Get-Command "pwsh" -ErrorAction SilentlyContinue
    if ($null -ne $pwsh) {
        return $pwsh.Source
    }

    return "powershell"
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
$visualPlanPath = Join-Path $analysisRoot "visual-plan.json"
$visualImplementationChecklistPath = Join-Path $analysisRoot "visual-implementation-checklist.json"
$visualImplementationReportJsonPath = Join-Path $analysisRoot "visual-implementation-report.json"
$visualQaReportJsonPath = Join-Path $analysisRoot "visual-qa-report.json"
$visualQaRuntimeSummaryPath = Join-Path $analysisRoot "visual-qa-runtime-summary.json"
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
$runtimeHostProcess = $null
$runtimeCommerceFixtureProcess = $null
$runtimeCommerceFixtureUrl = ""
$runtimeHostOutputPath = Join-Path $analysisRoot "phase4-mvp-runtime-host.out.log"
$runtimeHostErrorPath = Join-Path $analysisRoot "phase4-mvp-runtime-host.err.log"
$runtimeCommerceFixtureReadyPath = Join-Path $analysisRoot "phase4-mvp-commerce-fixture.ready.json"
$runtimeCommerceFixtureOutputPath = Join-Path $analysisRoot "phase4-mvp-commerce-fixture.out.log"
$runtimeCommerceFixtureErrorPath = Join-Path $analysisRoot "phase4-mvp-commerce-fixture.err.log"
$materializerScript = Join-Path $builderRoot "scripts\qa\materialize-reference-visual-qa-report.mjs"

$effectiveProofMode = if (-not [string]::IsNullOrWhiteSpace($ProofMode)) {
    $ProofMode.Trim()
} elseif ($SkeletonProof) {
    "Skeleton"
} else {
    "Runtime"
}

if ($effectiveProofMode -notin @("Skeleton", "Runtime")) {
    throw "ProofMode must be Skeleton or Runtime, but was '$effectiveProofMode'. Rerun with -Help for usage."
}

if ($SkeletonProof -and $effectiveProofMode -ne "Skeleton") {
    throw "-SkeletonProof cannot be combined with -ProofMode $effectiveProofMode."
}

if ($effectiveProofMode -eq "Runtime") {
    if ([string]::IsNullOrWhiteSpace($BaseUrl)) {
        throw "Runtime closure proof requires -BaseUrl. Start the generated storefront with the generated proof/full fixture wrapper and rerun the Phase 4 MVP gate."
    }

    if (-not [string]::IsNullOrWhiteSpace($resolvedFixtureRoot)) {
        throw "Runtime closure proof must not use -FixtureRoot. Remove -FixtureRoot and rerun with -ProofMode Runtime -BaseUrl <generated-host-url>."
    }
}

if ($effectiveProofMode -eq "Skeleton" -and [string]::IsNullOrWhiteSpace($resolvedFixtureRoot)) {
    throw "Skeleton proof requires -FixtureRoot."
}

function New-RerunCommand {
    return "powershell -NoProfile -ExecutionPolicy Bypass -File scripts\qa\run-storefront-phase4-mvp-gate.ps1 -GeneratedProjectRoot `"$GeneratedProjectRoot`""
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

function Start-GeneratedRuntimeHost {
    param(
        [string]$ProjectFile,
        [string]$Url,
        [string]$CommerceNodeBaseUrl
    )

    if ($null -ne $script:runtimeHostProcess -and -not $script:runtimeHostProcess.HasExited) {
        return
    }

    New-Item -ItemType Directory -Force -Path $analysisRoot | Out-Null
    foreach ($path in @($runtimeHostOutputPath, $runtimeHostErrorPath)) {
        if (Test-Path -LiteralPath $path) {
            Remove-Item -LiteralPath $path -Force
        }
    }

    $arguments = @(
        "run",
        "--project", $ProjectFile,
        "--configuration", $Configuration,
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
        $env:Storefront__StoreKey = $storeKey
        $env:Storefront__PublicBaseUrl = $Url
        $env:PublicUrl__BaseUrl = $Url
        $env:ClientApp__BaseUrl = $Url

        $script:runtimeHostProcess = Start-Process `
            -FilePath "dotnet" `
            -ArgumentList $argumentText `
            -WorkingDirectory $resolvedProjectRoot `
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

        try {
            if (Test-TcpEndpoint -Url $Url) {
                return
            }
        }
        catch {
            Start-Sleep -Seconds 1
        }
    } while ([DateTimeOffset]::UtcNow -lt $deadline)

    throw "Generated runtime host did not become reachable at $Url before timeout. Error log: $(Convert-ToRepoRelativePath $runtimeHostErrorPath)"
}

function Start-RuntimeCommerceFixture {
    param([string]$RequestedCommerceNodeBaseUrl)

    if (-not [string]::IsNullOrWhiteSpace($RequestedCommerceNodeBaseUrl)) {
        return $RequestedCommerceNodeBaseUrl.TrimEnd("/")
    }

    if ($null -ne $script:runtimeCommerceFixtureProcess -and -not $script:runtimeCommerceFixtureProcess.HasExited) {
        return $script:runtimeCommerceFixtureUrl
    }

    New-Item -ItemType Directory -Force -Path $analysisRoot | Out-Null
    foreach ($path in @($runtimeCommerceFixtureReadyPath, $runtimeCommerceFixtureOutputPath, $runtimeCommerceFixtureErrorPath)) {
        if (Test-Path -LiteralPath $path) {
            Remove-Item -LiteralPath $path -Force
        }
    }

    $fixturePort = Get-FreeTcpPort
    $fixtureScript = Join-Path $builderRoot "scripts\qa\start-fast-commerce-fixture.mjs"
    $arguments = @(
        $fixtureScript,
        "--store-key", $storeKey,
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

function Stop-GeneratedRuntimeHost {
    if ($null -eq $script:runtimeHostProcess) {
        return
    }

    if ($script:runtimeHostProcess.HasExited) {
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
    if ($null -eq $script:runtimeCommerceFixtureProcess) {
        return
    }

    if ($script:runtimeCommerceFixtureProcess.HasExited) {
        return
    }

    try {
        $script:runtimeCommerceFixtureProcess.Kill()
        $script:runtimeCommerceFixtureProcess.WaitForExit(10000) | Out-Null
    }
    catch {
    }
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
    $process.StartInfo.Arguments = (($Arguments | ForEach-Object { Convert-ToProcessArgument $_ }) -join " ")

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

function Read-RequiredJsonArtifact {
    param(
        [string]$Path,
        [string]$ArtifactName,
        [string]$FixCommand
    )

    if (-not (Test-Path -LiteralPath $Path)) {
        throw "$ArtifactName is required for closure mode but is missing: $Path. Fix: create the artifact and rerun $FixCommand."
    }

    try {
        return Get-Content -LiteralPath $Path -Raw | ConvertFrom-Json
    }
    catch {
        throw "$ArtifactName is not valid JSON: $Path. Fix: regenerate or repair the artifact and rerun $FixCommand. $($_.Exception.Message)"
    }
}

function Assert-RequiredFields {
    param(
        [object]$Json,
        [string]$ArtifactName,
        [string[]]$Fields
    )

    foreach ($field in $Fields) {
        if ($Json.PSObject.Properties.Name -notcontains $field) {
            throw "$ArtifactName is missing required field '$field'. Fix: recreate this artifact from the Phase 4 visual skill workflow and rerun $(New-RerunCommand)."
        }
    }
}

function Normalize-BaseUrl {
    param([string]$Url)

    return ([string]$Url).Trim().TrimEnd("/")
}

function Convert-ToCanonicalViewport {
    param([string]$Viewport)

    $value = ([string]$Viewport).Trim()
    if ($value.StartsWith("desktop", [System.StringComparison]::OrdinalIgnoreCase)) {
        return "desktop"
    }

    if ($value.StartsWith("tablet", [System.StringComparison]::OrdinalIgnoreCase)) {
        return "tablet"
    }

    if ($value.StartsWith("mobile", [System.StringComparison]::OrdinalIgnoreCase)) {
        return "mobile"
    }

    if ($value -in @("desktop", "tablet", "mobile")) {
        return $value
    }

    throw "Unsupported viewport '$Viewport'."
}

function Resolve-EvidencePath {
    param([string]$Path)

    if ([string]::IsNullOrWhiteSpace($Path)) {
        return ""
    }

    if ([System.IO.Path]::IsPathRooted($Path)) {
        return [System.IO.Path]::GetFullPath($Path)
    }

    $projectRelative = [System.IO.Path]::GetFullPath((Join-Path $resolvedProjectRoot $Path))
    if (Test-Path -LiteralPath $projectRelative) {
        return $projectRelative
    }

    $repoRelative = [System.IO.Path]::GetFullPath((Join-Path $repoRoot $Path))
    if (Test-Path -LiteralPath $repoRelative) {
        return $repoRelative
    }

    if (-not [string]::IsNullOrWhiteSpace($resolvedHandoffRoot)) {
        $handoffRelative = [System.IO.Path]::GetFullPath((Join-Path $resolvedHandoffRoot $Path))
        if (Test-Path -LiteralPath $handoffRelative) {
            return $handoffRelative
        }
    }

    return $projectRelative
}

function Get-PageIdFromCapture {
    param([object]$Capture)

    if (-not [string]::IsNullOrWhiteSpace([string]$Capture.pageId)) {
        return [string]$Capture.pageId
    }

    switch ([string]$Capture.pageName) {
        "shell-home" { return "home" }
        "catalog" { return "category" }
        "product" { return "product" }
        "cart" { return "cart" }
        "checkout" { return "checkout" }
        "sign-in" { return "auth" }
        default { return [string]$Capture.pageName }
    }
}

function Assert-NoPlaceholderHashText {
    param(
        [string]$Path,
        [string]$ArtifactName
    )

    $content = Get-Content -LiteralPath $Path -Raw
    if ($content -match "sha256:(phase4-11-|before|after|visual-plan|checklist|placeholder|fake|hash)") {
        throw "$ArtifactName contains placeholder hash text. Fix: recreate runtime closure evidence from current generated source and rerun $(New-RerunCommand)."
    }
}

function Assert-AgentWrittenFileChecksums {
    param([object]$Written)

    if ([string]$Written.detectionMode -ne "checkpoint-auto-detect") {
        throw "agent-written-files.json detectionMode must be 'checkpoint-auto-detect' for runtime closure, but was '$($Written.detectionMode)'. Fix: rerun record-agent-visual-writes.mjs with --from-checkpoint and then $(New-RerunCommand)."
    }

    foreach ($file in @($Written.files)) {
        $filePath = [string]$file.filePath
        $fullPath = Resolve-EvidencePath $filePath
        if (-not (Test-Path -LiteralPath $fullPath)) {
            throw "agent-written-files.json references a missing generated source file: $filePath. Evidence path: $fullPath."
        }

        $currentChecksum = "sha256:$(Get-NormalizedFileSha256 -Path $fullPath)"
        if ([string]$file.checksum -ne $currentChecksum) {
            throw "agent-written-files.json checksum for '$filePath' does not match current source file hash. Expected current checksum $currentChecksum, recorded $($file.checksum)."
        }
    }
}

function Assert-HandoffRuntimeGenerationMetadata {
    $metadata = Get-Content -LiteralPath $metadataPath -Raw
    $generationMode = Read-SimpleYamlValue -Text $metadata -Key "generationMode"
    if ($generationMode -ne "handoff-project-skeleton") {
        throw "metadata.yaml generationMode must be handoff-project-skeleton for runtime closure, but was '$generationMode'."
    }

    $generationPlan = Read-RequiredJsonArtifact -Path $generationPlanPath -ArtifactName "generation-plan.json" -FixCommand "build-storefront.ps1 -Mode generate -HandoffRoot"
    if ([string]$generationPlan.generationMode -ne "handoff") {
        throw "generation-plan.json generationMode must be handoff for runtime closure, but was '$($generationPlan.generationMode)'."
    }
}

function Assert-RuntimeEvidenceBinding {
    $visualPlan = Read-RequiredJsonArtifact -Path $visualPlanPath -ArtifactName "visual-plan.json" -FixCommand "storefront-visual-plan"
    $runtimeSummary = Read-RequiredJsonArtifact -Path $visualQaRuntimeSummaryPath -ArtifactName "visual-qa-runtime-summary.json" -FixCommand "run-visual-qa.mjs --proof-mode runtime"
    $qaReport = Read-RequiredJsonArtifact -Path $visualQaReportJsonPath -ArtifactName "visual-qa-report.json" -FixCommand "materialize-reference-visual-qa-report.mjs"
    $written = Read-RequiredJsonArtifact -Path $agentWrittenFilesPath -ArtifactName "agent-written-files.json" -FixCommand "record-agent-visual-writes.mjs"

    Assert-RequiredFields -Json $runtimeSummary -ArtifactName "visual-qa-runtime-summary.json" -Fields @("schemaVersion", "artifactKind", "operationId", "proofMode", "baseUrl", "startedUtc", "finishedUtc", "captures", "runtimeNetworkAudit", "passed")
    Assert-RequiredFields -Json $qaReport -ArtifactName "visual-qa-report.json" -Fields @("schemaVersion", "operationId", "referenceEvidenceReviewed", "runtimeEvidencePaths", "referenceEvidencePaths", "pageViewportCoverage", "independentReviewer", "comparisonDimensions", "acceptedDifferences", "unacceptedCriticalCount", "unacceptedMajorCount", "finalDecision", "viewportCaptures", "evidencePaths", "issues", "repairAttempts", "passed")
    Assert-RequiredFields -Json $written -ArtifactName "agent-written-files.json" -Fields @("schemaVersion", "artifactKind", "artifactId", "detectionMode", "generationPlanHash", "files")

    if ([string]$runtimeSummary.artifactKind -ne "storefront-builder.visual-qa-runtime-summary") {
        throw "visual-qa-runtime-summary.json artifactKind must be storefront-builder.visual-qa-runtime-summary, but was '$($runtimeSummary.artifactKind)'."
    }

    if ([string]$runtimeSummary.proofMode -ne "runtime") {
        throw "visual-qa-runtime-summary.json proofMode must be runtime, but was '$($runtimeSummary.proofMode)'."
    }

    if ((Normalize-BaseUrl $runtimeSummary.baseUrl) -ne (Normalize-BaseUrl $BaseUrl)) {
        throw "visual-qa-runtime-summary.json baseUrl '$($runtimeSummary.baseUrl)' does not match MVP gate BaseUrl '$BaseUrl'."
    }

    $operationId = [string]$visualPlan.operationId
    if ([string]$runtimeSummary.operationId -ne $operationId -or [string]$qaReport.operationId -ne $operationId) {
        throw "Runtime evidence operationId mismatch. visual-plan='$operationId', runtime-summary='$($runtimeSummary.operationId)', visual-qa-report='$($qaReport.operationId)'."
    }

    if ($qaReport.referenceEvidenceReviewed -ne $true) {
        throw "visual-qa-report.json referenceEvidenceReviewed must be true for runtime closure."
    }

    $summaryStartedUtc = [DateTimeOffset]::Parse([string]$runtimeSummary.startedUtc, [System.Globalization.CultureInfo]::InvariantCulture)
    $summaryFinishedUtc = [DateTimeOffset]::Parse([string]$runtimeSummary.finishedUtc, [System.Globalization.CultureInfo]::InvariantCulture)
    if ($summaryFinishedUtc -lt $summaryStartedUtc) {
        throw "visual-qa-runtime-summary.json finishedUtc is earlier than startedUtc."
    }

    $summaryCapturePaths = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
    $summaryCoverage = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
    foreach ($capture in @($runtimeSummary.captures)) {
        $capturePath = [string]$capture.screenshot
        $resolvedCapturePath = Resolve-EvidencePath $capturePath
        if (-not (Test-Path -LiteralPath $resolvedCapturePath)) {
            throw "visual-qa-runtime-summary.json references a missing screenshot: $capturePath. Evidence path: $resolvedCapturePath."
        }

        $captureItem = Get-Item -LiteralPath $resolvedCapturePath
        $captureWriteTime = [DateTimeOffset]$captureItem.LastWriteTimeUtc
        if ($captureWriteTime.AddSeconds(2) -lt $summaryStartedUtc) {
            throw "Runtime screenshot is older than visual-qa-runtime-summary.json startedUtc and cannot be current-run evidence: $capturePath."
        }

        [void]$summaryCapturePaths.Add($resolvedCapturePath)
        $pageId = Get-PageIdFromCapture -Capture $capture
        $viewportValue = if (-not [string]::IsNullOrWhiteSpace([string]$capture.viewport)) { [string]$capture.viewport } else { [string]$capture.viewportName }
        $viewport = Convert-ToCanonicalViewport $viewportValue
        [void]$summaryCoverage.Add(("{0}|{1}" -f $pageId, $viewport))
    }

    if ($summaryCapturePaths.Count -lt 1) {
        throw "visual-qa-runtime-summary.json must contain at least one current summary capture path."
    }

    $requiredCoverage = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
    foreach ($coverage in @($visualPlan.pageViewportCoverage)) {
        foreach ($viewport in @($coverage.viewports)) {
            [void]$requiredCoverage.Add(("{0}|{1}" -f $coverage.pageId, (Convert-ToCanonicalViewport $viewport)))
        }
    }

    foreach ($required in $requiredCoverage) {
        if (-not $summaryCoverage.Contains($required)) {
            throw "visual-qa-runtime-summary.json captures are missing visual-plan coverage '$required'."
        }
    }

    $reportRuntimePaths = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
    foreach ($path in @($qaReport.runtimeEvidencePaths)) {
        $resolvedPath = Resolve-EvidencePath ([string]$path)
        if (-not (Test-Path -LiteralPath $resolvedPath)) {
            throw "visual-qa-report.json runtimeEvidencePaths references missing evidence: $path."
        }

        [void]$reportRuntimePaths.Add($resolvedPath)
    }

    $missingFromReport = @($summaryCapturePaths | Where-Object { -not $reportRuntimePaths.Contains($_) })
    $notFromSummary = @($reportRuntimePaths | Where-Object { -not $summaryCapturePaths.Contains($_) })
    if ($missingFromReport.Count -gt 0 -or $notFromSummary.Count -gt 0) {
        throw "visual-qa-report.json runtimeEvidencePaths must match current summary capture paths. Missing from report: $($missingFromReport -join ', '). Not from summary: $($notFromSummary -join ', ')."
    }

    $reportCapturePaths = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
    $reportCaptureCoverage = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
    foreach ($capture in @($qaReport.viewportCaptures)) {
        $resolvedCapturePath = Resolve-EvidencePath ([string]$capture.screenshotPath)
        if (-not (Test-Path -LiteralPath $resolvedCapturePath)) {
            throw "visual-qa-report.json viewportCaptures references a missing screenshot: $($capture.screenshotPath)."
        }

        if (-not $summaryCapturePaths.Contains($resolvedCapturePath)) {
            throw "visual-qa-report.json viewportCaptures screenshot is not one of the current summary capture paths: $($capture.screenshotPath)."
        }

        [void]$reportCapturePaths.Add($resolvedCapturePath)
        [void]$reportCaptureCoverage.Add(("{0}|{1}" -f $capture.pageId, (Convert-ToCanonicalViewport $capture.viewport)))
    }

    foreach ($required in $requiredCoverage) {
        if (-not $reportCaptureCoverage.Contains($required)) {
            throw "visual-qa-report.json viewportCaptures is missing visual-plan coverage '$required'."
        }
    }

    foreach ($coverage in @($qaReport.pageViewportCoverage)) {
        foreach ($viewport in @($coverage.viewports)) {
            $key = "{0}|{1}" -f $coverage.pageId, (Convert-ToCanonicalViewport $viewport)
            if (-not $requiredCoverage.Contains($key)) {
                throw "visual-qa-report.json pageViewportCoverage includes coverage not required by visual-plan.json: $key."
            }
        }
    }

    foreach ($path in @($qaReport.referenceEvidencePaths)) {
        $resolvedReferencePath = Resolve-EvidencePath ([string]$path)
        if (-not (Test-Path -LiteralPath $resolvedReferencePath)) {
            throw "visual-qa-report.json referenceEvidencePaths references missing evidence: $path."
        }
    }

    $unacceptedCriticalCount = [int]$qaReport.unacceptedCriticalCount
    $unacceptedMajorCount = [int]$qaReport.unacceptedMajorCount
    if (($qaReport.passed -eq $true -or [string]$qaReport.finalDecision -eq "passed") -and ($unacceptedCriticalCount -gt 0 -or $unacceptedMajorCount -gt 0)) {
        throw "visual-qa-report.json says pass but unaccepted critical/major counters are nonzero."
    }

    if ($unacceptedCriticalCount -ne 0 -or $unacceptedMajorCount -ne 0) {
        throw "visual-qa-report.json has unaccepted critical/major issues. Critical=$unacceptedCriticalCount Major=$unacceptedMajorCount."
    }

    if ($qaReport.passed -ne $true -or [string]$qaReport.finalDecision -ne "passed") {
        throw "visual-qa-report.json must have passed=true and finalDecision='passed' for runtime closure."
    }

    $checkpointPath = Join-Path $analysisRoot ("visual-checkpoints\{0}\visual-checkpoint.json" -f $operationId)
    Assert-NoPlaceholderHashText -Path $checkpointPath -ArtifactName "visual-checkpoint.json"
    Assert-NoPlaceholderHashText -Path $visualImplementationReportJsonPath -ArtifactName "visual-implementation-report.json"
    Assert-AgentWrittenFileChecksums -Written $written
    Assert-HandoffRuntimeGenerationMetadata
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

function Save-GateReports {
    param([string]$Status)

    New-Item -ItemType Directory -Force -Path $analysisRoot | Out-Null
    $artifactPaths.Clear()
    foreach ($path in @($reportJsonPath, $reportMdPath, $visualQaReportPath, $resolvedScreenshotRoot, $runtimeHostOutputPath, $runtimeHostErrorPath, $runtimeCommerceFixtureReadyPath, $runtimeCommerceFixtureOutputPath, $runtimeCommerceFixtureErrorPath)) {
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

    if ($effectiveProofMode -eq "Skeleton") {
        Add-GateStep -Name "validate mandatory visual artifact chain" -Status "skipped" -Command "skeleton proof mode" -Problem "SkeletonProof mode does not prove the closure artifact chain." -LikelyCause "This mode is only for early generated skeleton feedback, not release closure." -RerunCommand (New-RerunCommand)
    }
    else {
        Invoke-AssertionStep -Name "validate mandatory visual artifact chain" -Command "Test-Path closure visual artifacts" -LikelyCause "Run storefront-visual-plan, storefront-visual-implement, record-agent-visual-writes.mjs, and storefront-visual-qa before the MVP gate." -Assertion {
            $visualPlan = Read-RequiredJsonArtifact -Path $visualPlanPath -ArtifactName "visual-plan.json" -FixCommand "storefront-visual-plan"
            Assert-RequiredFields -Json $visualPlan -ArtifactName "visual-plan.json" -Fields @("schemaVersion", "operationId", "projectName", "storeKey", "handoffHash", "generationPlanHash", "taskPackageHash", "pages", "pageViewportCoverage", "visualSlots", "allowedFiles", "plannedGeneratedOwnedFiles", "protectedFiles", "implementationOrder", "risks", "blockers")

            $checklist = Read-RequiredJsonArtifact -Path $visualImplementationChecklistPath -ArtifactName "visual-implementation-checklist.json" -FixCommand "storefront-visual-plan"
            Assert-RequiredFields -Json $checklist -ArtifactName "visual-implementation-checklist.json" -Fields @("schemaVersion", "checklistId", "sourceVisualPlanHash", "fileTasks", "acceptanceChecks", "requiredScreenshots", "forbiddenEdits")

            $operationId = [string]$visualPlan.operationId
            if ([string]::IsNullOrWhiteSpace($operationId)) {
                throw "visual-plan.json operationId is empty. Fix: rerun storefront-visual-plan and then $(New-RerunCommand)."
            }

            $checkpointPath = Join-Path $analysisRoot ("visual-checkpoints\{0}\visual-checkpoint.json" -f $operationId)
            $checkpoint = Read-RequiredJsonArtifact -Path $checkpointPath -ArtifactName "visual-checkpoint.json" -FixCommand "storefront-visual-implement"
            Assert-RequiredFields -Json $checkpoint -ArtifactName "visual-checkpoint.json" -Fields @("schemaVersion", "checkpointId", "operationId", "visualPlanHash", "checklistHash", "preEditSnapshotHash", "postEditSnapshotHash", "changedFiles", "unexpectedFiles", "sourceTreeSnapshotScope", "preEditFileHashes", "postEditFileHashes", "diffSummary")
            if ([string]$checkpoint.operationId -ne $operationId) {
                throw "visual-checkpoint.json operationId '$($checkpoint.operationId)' does not match visual-plan.json operationId '$operationId'. Fix: rerun storefront-visual-implement and then $(New-RerunCommand)."
            }

            $implementationReport = Read-RequiredJsonArtifact -Path $visualImplementationReportJsonPath -ArtifactName "visual-implementation-report.json" -FixCommand "storefront-visual-implement"
            Assert-RequiredFields -Json $implementationReport -ArtifactName "visual-implementation-report.json" -Fields @("schemaVersion", "operationId", "checkpointPath", "changedFiles", "recorderResultPath", "boundaryResult", "buildResult", "unresolvedItems")

            $requiredCoverage = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
            foreach ($coverage in @($visualPlan.pageViewportCoverage)) {
                foreach ($viewport in @($coverage.viewports)) {
                    [void]$requiredCoverage.Add(("{0}|{1}" -f $coverage.pageId, $viewport))
                }
            }

            if ($requiredCoverage.Count -lt 1) {
                throw "visual-plan.json pageViewportCoverage must require at least one page/viewport for closure."
            }

            $written = Read-RequiredJsonArtifact -Path $agentWrittenFilesPath -ArtifactName "agent-written-files.json" -FixCommand "record-agent-visual-writes.mjs"
            Assert-RequiredFields -Json $written -ArtifactName "agent-written-files.json" -Fields @("schemaVersion", "artifactKind", "artifactId", "detectionMode", "generationPlanHash", "files")
            if ([string]$written.detectionMode -ne "checkpoint-auto-detect") {
                throw "agent-written-files.json detectionMode must be 'checkpoint-auto-detect' for closure mode, but was '$($written.detectionMode)'. Fix: rerun record-agent-visual-writes.mjs with --from-checkpoint and then $(New-RerunCommand)."
            }
        }
    }

    $null = Invoke-GateCommand -Name "run StorefrontBuilder handoff boundary validation" -FileName "node" -Arguments @(
        (Join-Path $builderRoot "scripts\validate\Test-StorefrontBuilderHandoffBoundary.mjs"),
        "--project-root", $resolvedProjectRoot,
        "--name", $projectName
    ) -LikelyCause "Generated handoff artifacts, allowed outputs, or protected boundary metadata drifted."

    $null = Invoke-GateCommand -Name "restore generated project" -FileName "dotnet" -Arguments @(
        "restore", $projectFile, "--no-cache", "--force-evaluate"
    ) -LikelyCause "Generated package references or local NuGet package availability are invalid."

    $null = Invoke-GateCommand -Name "build generated project" -FileName "dotnet" -Arguments @(
        "build", $projectFile, "--configuration", $Configuration, "--no-restore"
    ) -LikelyCause "Generated visual files do not compile against Storefront Presentation packages."

    if ($effectiveProofMode -eq "Runtime" -and $StartRuntimeHost) {
        Invoke-AssertionStep -Name "start generated runtime host" -Command "dotnet run --project generated storefront --no-build" -LikelyCause "The generated storefront runtime could not start for browser visual QA." -Assertion {
            $commerceNodeBaseUrl = Start-RuntimeCommerceFixture -RequestedCommerceNodeBaseUrl $RuntimeCommerceNodeBaseUrl
            Start-GeneratedRuntimeHost -ProjectFile $projectFile -Url $BaseUrl -CommerceNodeBaseUrl $commerceNodeBaseUrl
        }
    }

    Invoke-AssertionStep -Name "run visual write ownership validation" -Command "agent-written-files.json checksum and allowlist check" -LikelyCause "Run record-agent-visual-writes.mjs after visual implementation or repair." -Assertion {
        if (-not (Test-Path -LiteralPath $agentWrittenFilesPath)) {
            throw "Agent visual write record is missing: $agentWrittenFilesPath"
        }

        $written = Get-Content -LiteralPath $agentWrittenFilesPath -Raw | ConvertFrom-Json
        if ($null -eq $written.files -or @($written.files).Count -lt 1) {
            throw "Agent visual write record has no files."
        }
    }

    $qaOperationId = ""
    if (Test-Path -LiteralPath $visualPlanPath) {
        $qaOperationId = [string]((Get-Content -LiteralPath $visualPlanPath -Raw | ConvertFrom-Json).operationId)
    }

    $qaArguments = @((Join-Path $builderRoot "scripts\qa\run-visual-qa.mjs"), "--proof-mode", $effectiveProofMode.ToLowerInvariant(), "--project-root", $resolvedProjectRoot, "--screenshot-root", $resolvedScreenshotRoot)
    if (-not [string]::IsNullOrWhiteSpace($qaOperationId)) {
        $qaArguments += @("--operation-id", $qaOperationId)
    }
    if ($effectiveProofMode -eq "Skeleton" -and -not [string]::IsNullOrWhiteSpace($resolvedFixtureRoot)) {
        $qaArguments += @("--fixture-root", $resolvedFixtureRoot)
    }
    if ($effectiveProofMode -eq "Runtime") {
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

    if ($effectiveProofMode -eq "Runtime") {
        $materializerArguments = @(
            $materializerScript,
            "--project-root", $resolvedProjectRoot,
            "--base-url", $BaseUrl
        )
        if (-not [string]::IsNullOrWhiteSpace($qaOperationId)) {
            $materializerArguments += @("--operation-id", $qaOperationId)
        }

        $null = Invoke-GateCommand -Name "materialize Reference QA report from current runtime summary" -FileName "node" -Arguments $materializerArguments -LikelyCause "visual-qa-runtime-summary.json, screenshots, or reference evidence could not be bound to visual-qa-report.json."

        Invoke-AssertionStep -Name "validate runtime evidence binding" -Command "visual-qa-runtime-summary.json and visual-qa-report.json current summary capture paths" -LikelyCause "Runtime visual evidence is stale, missing, copied from another run, or not tied to the current operation." -Assertion {
            Assert-RuntimeEvidenceBinding
        }
    }

    $currentGenerationPlan = Get-Content -LiteralPath $generationPlanPath -Raw | ConvertFrom-Json
    if ([string]$currentGenerationPlan.generationMode -eq "handoff") {
        $null = Invoke-GateCommand -Name "run regeneration WhatIf" -FileName (Get-PreferredPowerShell) -Arguments @(
            "-NoProfile", "-ExecutionPolicy", "Bypass",
            "-File", (Join-Path $builderRoot "regenerate-storefront.ps1"),
            "-ProjectRoot", $resolvedProjectRoot,
            "-Scope", "all",
            "-WhatIf"
        ) -LikelyCause "Generated ownership, regeneration metadata, or handoff plan identity drifted."
    }
    else {
        Invoke-AssertionStep -Name "run static generation no-op ownership validation" -Command "generation-plan.json static ownership proof" -LikelyCause "Static pilot generation metadata is incomplete or no longer records protected ownership." -Assertion {
            if ([string]$currentGenerationPlan.generationMode -ne "static") {
                throw "Unsupported generationMode '$($currentGenerationPlan.generationMode)' for non-handoff MVP regeneration proof."
            }

            $plannedFiles = @($currentGenerationPlan.files)
            if ($plannedFiles.Count -lt 1) {
                throw "Static generation plan must record at least one planned file."
            }

            $protectedFiles = @($plannedFiles | Where-Object { [string]$_.ownership -eq "protected" -and ([string]$_.allowedOperation -eq "skip" -or [string]$_.action -eq "skip") })
            if ($protectedFiles.Count -lt 1) {
                throw "Static generation plan must record protected skip entries for platform-owned files."
            }
        }
    }

    $finalDecision = "passed"
    Save-GateReports -Status $finalDecision
    Write-Host "Phase 4 MVP gate passed. Report: $(Convert-ToRepoRelativePath $reportMdPath)"
}
catch {
    Save-GateReports -Status $finalDecision
    Write-Error "Phase 4 MVP gate failed. Problem: $($_.Exception.Message). Likely cause: see $(Convert-ToRepoRelativePath $reportMdPath). Rerun: $(New-RerunCommand). Report: $(Convert-ToRepoRelativePath $reportMdPath). Evidence: $(Convert-ToRepoRelativePath $resolvedScreenshotRoot)"
    exit 1
}
finally {
    Stop-GeneratedRuntimeHost
    Stop-RuntimeCommerceFixture
}

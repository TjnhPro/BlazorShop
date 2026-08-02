param(
    [Parameter(Mandatory = $true)][string]$RepoRoot,
    [string]$HandoffRoot = "",
    [string]$SchemaRoot = "",
    [string]$ProjectName = "",
    [string]$StoreKey = "",
    [string]$ReportRoot = ""
)

$ErrorActionPreference = "Stop"

function New-HandoffPreflightFailure {
    param(
        [Parameter(Mandatory = $true)][string]$Code,
        [Parameter(Mandatory = $true)][string]$Problem,
        [Parameter(Mandatory = $true)][string]$Cause,
        [Parameter(Mandatory = $true)][string]$Fix
    )

    return "[$Code] StorefrontBuilder handoff preflight failed. Problem: $Problem Cause: $Cause Fix: $Fix"
}

function Test-ForbiddenRawHandoffFolder {
    param(
        [Parameter(Mandatory = $true)][string]$ResolvedPath,
        [Parameter(Mandatory = $true)][string]$ResolvedRepoRoot
    )

    $relative = [System.IO.Path]::GetRelativePath($ResolvedRepoRoot, $ResolvedPath).Replace("\", "/")
    return $relative -match "(^|/)captures(/|$)|(^|/)analysis/(pages|resolved)(/|$)|(^|/)presentation-catalog(/|$)|(^|/)review(/|$)|(^|/)reports(/|$)"
}

function Resolve-HandoffPackageRoot {
    param(
        [string]$RequestedRoot = "",
        [Parameter(Mandatory = $true)][string]$ResolvedRepoRoot
    )

    if ([string]::IsNullOrWhiteSpace($RequestedRoot)) {
        throw (New-HandoffPreflightFailure `
            -Code "SFB-HANDOFF-001" `
            -Problem "HandoffRoot was not provided." `
            -Cause "Phase 4 preflight requires an explicit portable handoff package." `
            -Fix "Pass -HandoffRoot pointing at a package root or analysis/agent-handoff folder.")
    }

    if (-not (Test-Path -LiteralPath $RequestedRoot)) {
        throw (New-HandoffPreflightFailure `
            -Code "SFB-HANDOFF-002" `
            -Problem "HandoffRoot '$RequestedRoot' does not exist." `
            -Cause "The package path was deleted, misspelled, or not copied locally." `
            -Fix "Copy the portable handoff package and rerun preflight.")
    }

    $resolved = (Resolve-Path -LiteralPath $RequestedRoot).Path
    $projectShapeManifest = Join-Path $resolved "analysis\agent-handoff\manifest.json"
    if (Test-Path -LiteralPath $projectShapeManifest) {
        return $resolved
    }

    $directManifest = Join-Path $resolved "manifest.json"
    if (Test-Path -LiteralPath $directManifest) {
        $directory = Get-Item -LiteralPath $resolved
        if ($directory.Name -eq "agent-handoff" -and $null -ne $directory.Parent -and $directory.Parent.Name -eq "analysis" -and $null -ne $directory.Parent.Parent) {
            return $directory.Parent.Parent.FullName
        }

        throw (New-HandoffPreflightFailure `
            -Code "SFB-HANDOFF-003" `
            -Problem "HandoffRoot '$resolved' contains manifest.json but is not analysis/agent-handoff." `
            -Cause "StorefrontBuilder only accepts the portable package root or its analysis/agent-handoff folder." `
            -Fix "Pass the package root that contains analysis/agent-handoff/manifest.json.")
    }

    if (Test-ForbiddenRawHandoffFolder -ResolvedPath $resolved -ResolvedRepoRoot $ResolvedRepoRoot) {
        throw (New-HandoffPreflightFailure `
            -Code "SFB-HANDOFF-004" `
            -Problem "HandoffRoot '$resolved' points at a raw source-only artifact folder." `
            -Cause "Phase 4 must not fall back to captures, resolved analysis, presentation-catalog, review, or reports folders." `
            -Fix "Pass the portable analysis/agent-handoff package instead.")
    }

    throw (New-HandoffPreflightFailure `
        -Code "SFB-HANDOFF-005" `
        -Problem "HandoffRoot '$resolved' is not a portable handoff package." `
        -Cause "The expected analysis/agent-handoff/manifest.json file is missing." `
        -Fix "Pass a copied portable package root or the analysis/agent-handoff folder.")
}

function Invoke-HandoffCommand {
    param(
        [Parameter(Mandatory = $true)][string[]]$Arguments
    )

    $output = & dotnet @Arguments 2>&1
    return [pscustomobject]@{
        ExitCode = $LASTEXITCODE
        Output = ($output -join [Environment]::NewLine)
    }
}

function Assert-RequiredHandoffArtifacts {
    param([Parameter(Mandatory = $true)][string]$PackageRoot)

    $required = @(
        "manifest.json",
        "handoff-readiness.json",
        "page-compositions.json",
        "storefront-pattern.json",
        "presentation-catalog.json",
        "presentation-mappings.json",
        "allowed-files.json",
        "protected-files.json",
        "design-tokens.json",
        "visual-style.json",
        "responsive-behavior.json",
        "interaction-models.json",
        "originality-restrictions.json",
        "evidence-manifest.json",
        "unresolved-regions.json"
    )

    foreach ($fileName in $required) {
        $path = Join-Path $PackageRoot "analysis\agent-handoff\$fileName"
        if (-not (Test-Path -LiteralPath $path)) {
            throw (New-HandoffPreflightFailure `
                -Code "SFB-HANDOFF-006" `
                -Problem "Required handoff artifact is missing: analysis/agent-handoff/$fileName." `
                -Cause "The portable package is incomplete." `
                -Fix "Copy the full analysis/agent-handoff package and rerun preflight.")
        }
    }
}

function Assert-NoBlockingUnresolvedRegions {
    param([Parameter(Mandatory = $true)][string]$PackageRoot)

    $path = Join-Path $PackageRoot "analysis\agent-handoff\unresolved-regions.json"
    $unresolved = Get-Content -LiteralPath $path -Raw | ConvertFrom-Json
    $blockingCount = @($unresolved.blockingRegions).Count
    if ($blockingCount -gt 0) {
        throw (New-HandoffPreflightFailure `
            -Code "SFB-HANDOFF-012" `
            -Problem "Handoff package contains $blockingCount unresolved blocking region(s)." `
            -Cause "Phase 4 generation cannot continue while reviewed analysis still has blocking regions." `
            -Fix "Resolve blocking regions in ReverseEngineering and reassemble the handoff.")
    }
}

function Write-HandoffPreflightReport {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$Status,
        [string]$ErrorMessage = "",
        [string]$PackageRoot = "",
        [string]$ResolvedSchemaRoot = "",
        [string]$ValidationOutput = "",
        [string]$DryRunOutput = ""
    )

    $lines = [System.Collections.Generic.List[string]]::new()
    $lines.Add("# StorefrontBuilder Handoff Preflight")
    $lines.Add("")
    $lines.Add("Status: $Status")
    $lines.Add("Project name: $ProjectName")
    $lines.Add("Store key: $StoreKey")
    $lines.Add("Package root: $PackageRoot")
    $lines.Add("Schema root: $ResolvedSchemaRoot")
    $lines.Add("UTC timestamp: $((Get-Date).ToUniversalTime().ToString('yyyy-MM-dd HH:mm:ssZ'))")
    if (-not [string]::IsNullOrWhiteSpace($ErrorMessage)) {
        $lines.Add("Error: $ErrorMessage")
    }

    $lines.Add("")
    $lines.Add("Validate handoff output:")
    $lines.Add('```text')
    $lines.Add($ValidationOutput)
    $lines.Add('```')
    $lines.Add("")
    $lines.Add("Dry-run handoff output:")
    $lines.Add('```text')
    $lines.Add($DryRunOutput)
    $lines.Add('```')

    New-Item -ItemType Directory -Force -Path (Split-Path -Parent $Path) | Out-Null
    $lines | Set-Content -LiteralPath $Path -Encoding UTF8
}

$resolvedRepoRoot = (Resolve-Path -LiteralPath $RepoRoot).Path
$resolvedSchemaRoot = if ([string]::IsNullOrWhiteSpace($SchemaRoot)) {
    Join-Path $resolvedRepoRoot "tools\BlazorShop.AI.StorefrontReverseEngineering\Schemas"
} else {
    $SchemaRoot
}
$resolvedReportRoot = if ([string]::IsNullOrWhiteSpace($ReportRoot)) {
    Join-Path $resolvedRepoRoot "obj\storefront-builder\handoff-preflight"
} else {
    $ReportRoot
}
$safeProjectName = if ([string]::IsNullOrWhiteSpace($ProjectName)) { "UnknownProject" } else { $ProjectName -replace "[^A-Za-z0-9_.-]", "_" }
$reportPath = Join-Path $resolvedReportRoot ("handoff-preflight-" + $safeProjectName + "-" + (Get-Date -Format "yyyyMMddHHmmss") + ".md")
$packageRoot = ""
$validationOutput = ""
$dryRunOutput = ""
$status = "failed"
$errorMessage = ""

try {
    if (-not (Test-Path -LiteralPath $resolvedSchemaRoot) -or -not @(Get-ChildItem -LiteralPath $resolvedSchemaRoot -Filter "*.schema.json" -File).Count) {
        throw (New-HandoffPreflightFailure `
            -Code "SFB-HANDOFF-007" `
            -Problem "Schema root '$resolvedSchemaRoot' is missing or contains no schema files." `
            -Cause "The portable validator cannot verify handoff contracts." `
            -Fix "Pass -HandoffSchemaRoot or restore tools/BlazorShop.AI.StorefrontReverseEngineering/Schemas.")
    }

    $resolvedSchemaRoot = (Resolve-Path -LiteralPath $resolvedSchemaRoot).Path
    $packageRoot = Resolve-HandoffPackageRoot -RequestedRoot $HandoffRoot -ResolvedRepoRoot $resolvedRepoRoot
    Assert-RequiredHandoffArtifacts -PackageRoot $packageRoot

    $reverseEngineeringProject = Join-Path $resolvedRepoRoot "tools\BlazorShop.AI.StorefrontReverseEngineering\BlazorShop.AI.StorefrontReverseEngineering.csproj"
    $validate = Invoke-HandoffCommand -Arguments @(
        "run", "--project", $reverseEngineeringProject, "--",
        "validate-handoff", "--handoff-root", $packageRoot, "--schema-root", $resolvedSchemaRoot
    )
    $validationOutput = $validate.Output
    if ($validate.ExitCode -ne 0) {
        throw (New-HandoffPreflightFailure `
            -Code "SFB-HANDOFF-008" `
            -Problem "validate-handoff failed with exit code $($validate.ExitCode)." `
            -Cause "The portable package failed schema, hash, readiness, or reference validation." `
            -Fix "Inspect the preflight report and reassemble the handoff package.")
    }

    $dryRun = Invoke-HandoffCommand -Arguments @(
        "run", "--project", $reverseEngineeringProject, "--",
        "dry-run-handoff", "--handoff-root", $packageRoot, "--schema-root", $resolvedSchemaRoot
    )
    $dryRunOutput = $dryRun.Output
    if ($dryRun.ExitCode -ne 0) {
        throw (New-HandoffPreflightFailure `
            -Code "SFB-HANDOFF-009" `
            -Problem "dry-run-handoff failed with exit code $($dryRun.ExitCode)." `
            -Cause "The package validated but could not be loaded by the Phase 4 consumer preflight." `
            -Fix "Inspect the dry-run output and repair the handoff package.")
    }

    Assert-NoBlockingUnresolvedRegions -PackageRoot $packageRoot
    $status = "passed"
}
catch {
    $errorMessage = $_.Exception.Message
    throw
}
finally {
    Write-HandoffPreflightReport `
        -Path $reportPath `
        -Status $status `
        -ErrorMessage $errorMessage `
        -PackageRoot $packageRoot `
        -ResolvedSchemaRoot $resolvedSchemaRoot `
        -ValidationOutput $validationOutput `
        -DryRunOutput $dryRunOutput

    Write-Host "Handoff preflight report: $reportPath"
}

function Read-StorefrontBuilderGeneratorVersion {
    param([string]$VersionPath = "")

    if ([string]::IsNullOrWhiteSpace($VersionPath)) {
        $VersionPath = Join-Path $PSScriptRoot "..\..\version.json"
    }

    $resolvedVersionPath = [System.IO.Path]::GetFullPath($VersionPath)
    if (-not (Test-Path -LiteralPath $resolvedVersionPath)) {
        throw "[SFB-PROJECT-012] StorefrontBuilder version.json is missing. Problem: generatorVersion cannot be resolved from '$resolvedVersionPath'. Cause: the shared version source was deleted or the tool layout is invalid. Fix: restore tools/BlazorShop.AI.StorefrontBuilder/version.json with a generatorVersion value."
    }

    try {
        $versionDocument = Get-Content -LiteralPath $resolvedVersionPath -Raw | ConvertFrom-Json
    }
    catch {
        throw "[SFB-PROJECT-013] StorefrontBuilder version.json is malformed. Problem: generatorVersion cannot be parsed from '$resolvedVersionPath'. Cause: the file is not valid JSON. Fix: keep version.json as { `"generatorVersion`": `"x.y.z`" }."
    }

    $generatorVersion = $versionDocument.generatorVersion
    if ([string]::IsNullOrWhiteSpace($generatorVersion)) {
        throw "[SFB-PROJECT-013] StorefrontBuilder version.json is malformed. Problem: generatorVersion is missing or empty in '$resolvedVersionPath'. Cause: the shared version source does not define generatorVersion. Fix: set generatorVersion to a non-empty version string."
    }

    return [string]$generatorVersion
}

$script:StorefrontBuilderGeneratorVersion = Read-StorefrontBuilderGeneratorVersion

function Normalize-StorefrontProjectName {
    param([Parameter(Mandatory = $true)][string]$Name)

    $trimmed = $Name.Trim()
    if ([string]::IsNullOrWhiteSpace($trimmed)) {
        throw "[SFB-PROJECT-001] Name must not be empty."
    }

    if ($trimmed.IndexOf("..", [System.StringComparison]::Ordinal) -ge 0 `
        -or $trimmed.IndexOf("\", [System.StringComparison]::Ordinal) -ge 0 `
        -or $trimmed.IndexOf("/", [System.StringComparison]::Ordinal) -ge 0 `
        -or $trimmed.IndexOf(":", [System.StringComparison]::Ordinal) -ge 0) {
        throw "[SFB-PROJECT-001] Name must not contain traversal, separators, or drive markers."
    }

    $prefix = "BlazorShop.Storefront."
    $suffix = if ($trimmed.StartsWith($prefix, [System.StringComparison]::Ordinal)) {
        $trimmed.Substring($prefix.Length)
    } else {
        if ($trimmed.IndexOf(".", [System.StringComparison]::Ordinal) -ge 0) {
            throw "[SFB-PROJECT-001] Friendly name must be a single PascalCase suffix or the full BlazorShop.Storefront.{Name} project name."
        }

        $trimmed
    }

    if ([string]::IsNullOrWhiteSpace($suffix) -or $suffix.IndexOf(".", [System.StringComparison]::Ordinal) -ge 0) {
        throw "[SFB-PROJECT-001] Name must have one non-empty project suffix segment."
    }

    if ($suffix -cnotmatch "^[A-Z][A-Za-z0-9]*$") {
        throw "[SFB-PROJECT-001] Project suffix must be PascalCase alphanumeric and start with an uppercase letter."
    }

    $reservedSuffixes = @("Starter", "V2", "Runtime", "Client", "Components", "Presentation", "Browser", "ControlPlane", "CommerceNode")
    if ($reservedSuffixes | Where-Object { $_.Equals($suffix, [System.StringComparison]::OrdinalIgnoreCase) } | Select-Object -First 1) {
        throw "[SFB-PROJECT-001] Project suffix '$suffix' is reserved."
    }

    return "$prefix$suffix"
}

function Normalize-StorefrontStoreKey {
    param([Parameter(Mandatory = $true)][string]$StoreKey)

    $trimmed = $StoreKey.Trim()
    if ([string]::IsNullOrWhiteSpace($trimmed)) {
        throw "[SFB-PROJECT-010] StoreKey must not be empty."
    }

    if ($trimmed.IndexOf("..", [System.StringComparison]::Ordinal) -ge 0 `
        -or $trimmed.IndexOf("\", [System.StringComparison]::Ordinal) -ge 0 `
        -or $trimmed.IndexOf("/", [System.StringComparison]::Ordinal) -ge 0 `
        -or $trimmed.IndexOf(":", [System.StringComparison]::Ordinal) -ge 0) {
        throw "[SFB-PROJECT-010] StoreKey must not contain traversal, separators, or drive markers."
    }

    if ($trimmed -cnotmatch "^[a-z0-9][a-z0-9-]{0,61}[a-z0-9]$") {
        throw "[SFB-PROJECT-010] StoreKey must be lowercase DNS-label style text using letters, digits, and hyphens."
    }

    return $trimmed
}

function Resolve-StorefrontBuilderRepoPath {
    param(
        [Parameter(Mandatory = $true)][string]$RepoRoot,
        [Parameter(Mandatory = $true)][string]$Path
    )

    if ([System.IO.Path]::IsPathRooted($Path)) {
        return [System.IO.Path]::GetFullPath($Path)
    }

    return [System.IO.Path]::GetFullPath((Join-Path $RepoRoot $Path))
}

function Assert-StorefrontBuilderPathUnderRoot {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$Root,
        [string]$ErrorCode = "SFB-PROJECT-002"
    )

    $resolvedPath = [System.IO.Path]::GetFullPath($Path)
    $resolvedRoot = [System.IO.Path]::GetFullPath($Root).TrimEnd([System.IO.Path]::DirectorySeparatorChar, [System.IO.Path]::AltDirectorySeparatorChar)
    $rootWithSeparator = "$resolvedRoot$([System.IO.Path]::DirectorySeparatorChar)"

    if ($resolvedPath.Equals($resolvedRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
        return
    }

    if (-not $resolvedPath.StartsWith($rootWithSeparator, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "[$ErrorCode] Path must stay under approved StorefrontBuilder output root: $resolvedPath"
    }
}

function Resolve-ApprovedStorefrontBuilderOutputRoot {
    param(
        [Parameter(Mandatory = $true)][string]$RepoRoot,
        [Parameter(Mandatory = $true)][string]$OutputRoot
    )

    $resolvedOutputRoot = Resolve-StorefrontBuilderRepoPath -RepoRoot $RepoRoot -Path $OutputRoot
    $approvedRoots = @(
        (Join-Path $RepoRoot "artifacts\storefront-builder"),
        (Join-Path $RepoRoot "artifacts\storefront-builder\generated"),
        (Join-Path $RepoRoot "obj\storefront-builder\generated")
    ) | ForEach-Object { [System.IO.Path]::GetFullPath($_) }

    $isApproved = $false
    foreach ($approvedRoot in $approvedRoots) {
        try {
            Assert-StorefrontBuilderPathUnderRoot -Path $resolvedOutputRoot -Root $approvedRoot
            $isApproved = $true
            break
        }
        catch {
            $isApproved = $false
        }
    }

    if (-not $isApproved) {
        throw "[SFB-PROJECT-002] OutputRoot must be under artifacts/storefront-builder, artifacts/storefront-builder/generated, or obj/storefront-builder/generated."
    }

    return $resolvedOutputRoot
}

function Assert-StorefrontBuilderPathUnderApprovedOutputRoots {
    param(
        [Parameter(Mandatory = $true)][string]$RepoRoot,
        [Parameter(Mandatory = $true)][string]$Path,
        [string]$ErrorCode = "SFB-PROJECT-002"
    )

    $approvedRoots = @(
        (Join-Path $RepoRoot "artifacts\storefront-builder"),
        (Join-Path $RepoRoot "artifacts\storefront-builder\generated"),
        (Join-Path $RepoRoot "obj\storefront-builder\generated")
    ) | ForEach-Object { [System.IO.Path]::GetFullPath($_) }

    foreach ($approvedRoot in $approvedRoots) {
        try {
            Assert-StorefrontBuilderPathUnderRoot -Path $Path -Root $approvedRoot -ErrorCode $ErrorCode
            return
        }
        catch {
        }
    }

    throw "[$ErrorCode] Path must be under artifacts/storefront-builder, artifacts/storefront-builder/generated, or obj/storefront-builder/generated: $Path"
}

function Resolve-StorefrontBuilderWorkspacePaths {
    param(
        [Parameter(Mandatory = $true)][string]$RepoRoot,
        [string]$ProjectName = "",
        [string]$OutputRoot = "",
        [string]$WorkspaceRoot = "",
        [string]$ProjectRoot = "",
        [switch]$WarnOnProjectRootAlias
    )

    $resolvedWorkspaceRoot = ""
    $resolvedProjectRootAlias = ""

    if (-not [string]::IsNullOrWhiteSpace($WorkspaceRoot)) {
        $resolvedWorkspaceRoot = Resolve-StorefrontBuilderRepoPath -RepoRoot $RepoRoot -Path $WorkspaceRoot
    }

    if (-not [string]::IsNullOrWhiteSpace($ProjectRoot)) {
        $resolvedProjectRootAlias = Resolve-StorefrontBuilderRepoPath -RepoRoot $RepoRoot -Path $ProjectRoot
        if ($WarnOnProjectRootAlias) {
            Write-Warning "-ProjectRoot is a temporary compatibility alias for -WorkspaceRoot. Pass -WorkspaceRoot to avoid ambiguity."
        }
    }

    if (-not [string]::IsNullOrWhiteSpace($resolvedWorkspaceRoot) -and -not [string]::IsNullOrWhiteSpace($resolvedProjectRootAlias)) {
        if (-not $resolvedWorkspaceRoot.Equals($resolvedProjectRootAlias, [System.StringComparison]::OrdinalIgnoreCase)) {
            throw "[SFB-PROJECT-014] ProjectRoot and WorkspaceRoot differ. Problem: ProjectRoot '$resolvedProjectRootAlias' does not equal WorkspaceRoot '$resolvedWorkspaceRoot'. Cause: ProjectRoot is now only a compatibility alias for WorkspaceRoot. Fix: pass only -WorkspaceRoot, or pass matching values."
        }
    }
    elseif ([string]::IsNullOrWhiteSpace($resolvedWorkspaceRoot) -and -not [string]::IsNullOrWhiteSpace($resolvedProjectRootAlias)) {
        $resolvedWorkspaceRoot = $resolvedProjectRootAlias
    }

    $normalizedProjectName = ""
    if (-not [string]::IsNullOrWhiteSpace($ProjectName)) {
        $normalizedProjectName = Normalize-StorefrontProjectName -Name $ProjectName
    }

    if ([string]::IsNullOrWhiteSpace($resolvedWorkspaceRoot)) {
        if ([string]::IsNullOrWhiteSpace($normalizedProjectName)) {
            throw "[SFB-PROJECT-015] ProjectName is required when WorkspaceRoot is not supplied."
        }

        $resolvedOutputRoot = Resolve-ApprovedStorefrontBuilderOutputRoot -RepoRoot $RepoRoot -OutputRoot $OutputRoot
        $resolvedWorkspaceRoot = Join-Path $resolvedOutputRoot $normalizedProjectName
    }
    else {
        Assert-StorefrontBuilderPathUnderApprovedOutputRoots -RepoRoot $RepoRoot -Path $resolvedWorkspaceRoot
        if ([string]::IsNullOrWhiteSpace($normalizedProjectName)) {
            $normalizedProjectName = Normalize-StorefrontProjectName -Name (Split-Path -Leaf $resolvedWorkspaceRoot)
        }
    }

    if ([string]::IsNullOrWhiteSpace($normalizedProjectName)) {
        throw "[SFB-PROJECT-015] ProjectName could not be resolved for workspace '$resolvedWorkspaceRoot'."
    }

    $serverProjectRoot = Join-Path $resolvedWorkspaceRoot $normalizedProjectName
    $wasmProjectRoot = Join-Path $resolvedWorkspaceRoot "$normalizedProjectName.WASM"
    $solutionPath = Join-Path $resolvedWorkspaceRoot "$normalizedProjectName.sln"
    $analysisRoot = Join-Path $resolvedWorkspaceRoot "docs\storefront-analysis"

    [pscustomobject]@{
        ProjectName = $normalizedProjectName
        OutputRoot = if ([string]::IsNullOrWhiteSpace($OutputRoot)) { Split-Path -Parent $resolvedWorkspaceRoot } else { Resolve-ApprovedStorefrontBuilderOutputRoot -RepoRoot $RepoRoot -OutputRoot $OutputRoot }
        WorkspaceRoot = [System.IO.Path]::GetFullPath($resolvedWorkspaceRoot)
        ProjectRoot = [System.IO.Path]::GetFullPath($resolvedWorkspaceRoot)
        ServerProjectRoot = [System.IO.Path]::GetFullPath($serverProjectRoot)
        WasmProjectRoot = [System.IO.Path]::GetFullPath($wasmProjectRoot)
        SolutionPath = [System.IO.Path]::GetFullPath($solutionPath)
        AnalysisRoot = [System.IO.Path]::GetFullPath($analysisRoot)
        MetadataPath = [System.IO.Path]::GetFullPath((Join-Path $analysisRoot "metadata.yaml"))
        ContractPath = [System.IO.Path]::GetFullPath((Join-Path $analysisRoot "starter-generation.contract.yaml"))
    }
}

function Write-StorefrontBuilderWorkspacePaths {
    param([Parameter(Mandatory = $true)]$Paths)

    Write-Host "StorefrontBuilder paths:"
    Write-Host "- workspace: $($Paths.WorkspaceRoot)"
    Write-Host "- server: $($Paths.ServerProjectRoot)"
    Write-Host "- wasm: $($Paths.WasmProjectRoot)"
    Write-Host "- solution: $($Paths.SolutionPath)"
    Write-Host "- analysis: $($Paths.AnalysisRoot)"
}

function Remove-StorefrontBuilderPath {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$ApprovedRoot
    )

    if (Test-Path -LiteralPath $Path) {
        Assert-StorefrontBuilderPathUnderRoot -Path $Path -Root $ApprovedRoot
        Remove-Item -LiteralPath $Path -Recurse -Force
    }
}

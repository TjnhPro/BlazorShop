$script:StorefrontBuilderGeneratorVersion = "2.4.0"

function Normalize-StorefrontProjectName {
    param([Parameter(Mandatory = $true)][string]$Name)

    $trimmed = $Name.Trim()
    if ([string]::IsNullOrWhiteSpace($trimmed)) {
        throw "[SFB-PROJECT-001] Name must not be empty."
    }

    if ($trimmed.Contains("..", [System.StringComparison]::Ordinal) `
        -or $trimmed.Contains("\", [System.StringComparison]::Ordinal) `
        -or $trimmed.Contains("/", [System.StringComparison]::Ordinal) `
        -or $trimmed.Contains(":", [System.StringComparison]::Ordinal)) {
        throw "[SFB-PROJECT-001] Name must not contain traversal, separators, or drive markers."
    }

    $prefix = "BlazorShop.Storefront."
    $suffix = if ($trimmed.StartsWith($prefix, [System.StringComparison]::Ordinal)) {
        $trimmed.Substring($prefix.Length)
    } else {
        if ($trimmed.Contains(".", [System.StringComparison]::Ordinal)) {
            throw "[SFB-PROJECT-001] Friendly name must be a single PascalCase suffix or the full BlazorShop.Storefront.{Name} project name."
        }

        $trimmed
    }

    if ([string]::IsNullOrWhiteSpace($suffix) -or $suffix.Contains(".", [System.StringComparison]::Ordinal)) {
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

    if ($trimmed.Contains("..", [System.StringComparison]::Ordinal) `
        -or $trimmed.Contains("\", [System.StringComparison]::Ordinal) `
        -or $trimmed.Contains("/", [System.StringComparison]::Ordinal) `
        -or $trimmed.Contains(":", [System.StringComparison]::Ordinal)) {
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
        throw "[SFB-PROJECT-002] OutputRoot must be under artifacts/storefront-builder/generated or obj/storefront-builder/generated."
    }

    return $resolvedOutputRoot
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

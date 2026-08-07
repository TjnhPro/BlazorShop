[CmdletBinding()]
param(
    [ValidateSet("analyze-only", "preflight-only", "plan-only", "generate", "update", "validate-only", "full")]
    [string]$Mode = "plan-only",

    [string]$Url = "https://reference.example",
    [string]$Name = "BlazorShop.Storefront.GeneratedProof",
    [string]$StoreKey = "sample",
    [string]$OutputRoot = "artifacts/storefront-builder",
    [string]$HandoffRoot = "",
    [string]$HandoffSchemaRoot = "tools/BlazorShop.AI.StorefrontReverseEngineering/Schemas",
    [string]$Configuration = "Debug",

    [switch]$Force,
    [switch]$SkipVisualQa,
    [switch]$SkipCommerceRegression,
    [switch]$SkipPackageRefresh,
    [switch]$InstallNodeDependencies,
    [switch]$Describe
)

$ErrorActionPreference = "Stop"

function Find-RepoRoot {
    param([Parameter(Mandatory = $true)][string]$StartPath)

    $current = [System.IO.Path]::GetFullPath($StartPath)
    while (-not [string]::IsNullOrWhiteSpace($current)) {
        $builderScript = Join-Path $current "tools\BlazorShop.AI.StorefrontBuilder\build-storefront.ps1"
        if ((Test-Path -LiteralPath (Join-Path $current ".git")) -and (Test-Path -LiteralPath $builderScript)) {
            return $current
        }

        $parent = Split-Path -Parent $current
        if ($parent -eq $current) {
            break
        }

        $current = $parent
    }

    throw "Could not find BlazorShop repo root from '$StartPath'."
}

function Format-CommandPart {
    param([string]$Value)

    if ($Value -match "\s|#|'|`"") {
        return "'" + $Value.Replace("'", "''") + "'"
    }

    return $Value
}

function Format-CommandLine {
    param([string]$ScriptPath, [string[]]$Arguments)

    $parts = @("powershell", "-NoProfile", "-ExecutionPolicy", "Bypass", "-File", (Format-CommandPart $ScriptPath))
    foreach ($argument in $Arguments) {
        $parts += Format-CommandPart $argument
    }

    return ($parts -join " ")
}

function Assert-NodeTooling {
    param([Parameter(Mandatory = $true)][string]$BuilderRoot)

    if (-not (Get-Command node -ErrorAction SilentlyContinue)) {
        throw "Node.js is required for StorefrontBuilder, but 'node' is not available on PATH."
    }

    $packageLock = Join-Path $BuilderRoot "package-lock.json"
    $nodeModules = Join-Path $BuilderRoot "node_modules"
    if ((Test-Path -LiteralPath $packageLock) -and -not (Test-Path -LiteralPath $nodeModules)) {
        if (-not $InstallNodeDependencies) {
            throw "StorefrontBuilder node_modules is missing. Re-run with -InstallNodeDependencies or run 'npm ci' in tools\BlazorShop.AI.StorefrontBuilder."
        }

        Write-Host "== install StorefrontBuilder npm dependencies =="
        Push-Location $BuilderRoot
        try {
            npm ci
            if ($LASTEXITCODE -ne 0) {
                throw "npm ci failed with exit code $LASTEXITCODE."
            }
        }
        finally {
            Pop-Location
        }
    }
}

function Invoke-CheckedCommand {
    param(
        [Parameter(Mandatory = $true)][string]$Description,
        [Parameter(Mandatory = $true)][scriptblock]$Command
    )

    Write-Host "== $Description =="
    & $Command
    if ($LASTEXITCODE -ne 0) {
        throw "$Description failed with exit code $LASTEXITCODE."
    }
}

function Get-SourceHead {
    param([Parameter(Mandatory = $true)][string]$RepoRoot)

    $head = (& git -C $RepoRoot rev-parse HEAD 2>$null)
    if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($head)) {
        throw "Could not resolve source HEAD for Storefront package identity."
    }

    return [string]$head
}

function Clear-StorefrontPackageFeed {
    param(
        [Parameter(Mandatory = $true)][string]$RepoRoot,
        [Parameter(Mandatory = $true)][string]$PackageRoot
    )

    $resolvedPackageRoot = [System.IO.Path]::GetFullPath($PackageRoot)
    $approvedPackageRoot = [System.IO.Path]::GetFullPath((Join-Path $RepoRoot "artifacts\storefront-packages"))
    if (-not $resolvedPackageRoot.Equals($approvedPackageRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to clean unapproved package feed: $resolvedPackageRoot"
    }

    if (Test-Path -LiteralPath $resolvedPackageRoot) {
        Get-ChildItem -LiteralPath $resolvedPackageRoot -Force | Remove-Item -Recurse -Force
    }

    New-Item -ItemType Directory -Force -Path $resolvedPackageRoot | Out-Null
}

function Clear-StorefrontLocalPackageCache {
    param(
        [Parameter(Mandatory = $true)][string[]]$Versions
    )

    $globalPackageRoot = Join-Path ([Environment]::GetFolderPath("UserProfile")) ".nuget\packages"
    $resolvedGlobalPackageRoot = [System.IO.Path]::GetFullPath($globalPackageRoot)
    $packageIds = @(
        "blazorshop.storefront.client",
        "blazorshop.storefront.runtime",
        "blazorshop.storefront.presentation",
        "blazorshop.storefront.components",
        "blazorshop.storefront.browser"
    )

    foreach ($version in $Versions | Select-Object -Unique) {
        foreach ($packageId in $packageIds) {
            $versionPath = Join-Path $globalPackageRoot "$packageId\$version"
            $resolvedVersionPath = [System.IO.Path]::GetFullPath($versionPath)
            if (-not $resolvedVersionPath.StartsWith($resolvedGlobalPackageRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
                throw "Refusing to clean NuGet cache path outside global package root: $resolvedVersionPath"
            }

            if (Test-Path -LiteralPath $resolvedVersionPath) {
                Remove-Item -LiteralPath $resolvedVersionPath -Recurse -Force
            }
        }
    }
}

function Get-StorefrontPackageHash {
    param(
        [Parameter(Mandatory = $true)][string]$PackageRoot,
        [Parameter(Mandatory = $true)][string]$PackageId,
        [Parameter(Mandatory = $true)][string]$Version
    )

    $packagePath = Join-Path $PackageRoot "$PackageId.$Version.nupkg"
    if (-not (Test-Path -LiteralPath $packagePath)) {
        throw "Expected package missing from local feed: $packagePath"
    }

    return (Get-FileHash -LiteralPath $packagePath -Algorithm SHA256).Hash.ToLowerInvariant()
}

function New-StorefrontPackageSet {
    param(
        [Parameter(Mandatory = $true)][string]$RepoRoot,
        [Parameter(Mandatory = $true)][string]$PackageRoot,
        [Parameter(Mandatory = $true)][string]$Configuration
    )

    $sourceHead = Get-SourceHead -RepoRoot $RepoRoot
    $packageBuildIdentity = $sourceHead.Substring(0, 12)
    $version = "1.0.0-local.$packageBuildIdentity"

    Write-Host "Source HEAD: $sourceHead"
    Write-Host "Package build identity: $packageBuildIdentity"
    Write-Host "Storefront package version: $version"

    Clear-StorefrontPackageFeed -RepoRoot $RepoRoot -PackageRoot $PackageRoot
    Clear-StorefrontLocalPackageCache -Versions @($version)

    $projects = @(
        @{ Name = "Storefront.Client"; Id = "BlazorShop.Storefront.Client"; Path = "BlazorShop.PresentationV2\BlazorShop.Storefront.Client\BlazorShop.Storefront.Client.csproj" },
        @{ Name = "Storefront.Runtime"; Id = "BlazorShop.Storefront.Runtime"; Path = "BlazorShop.PresentationV2\BlazorShop.Storefront.Runtime\BlazorShop.Storefront.Runtime.csproj" },
        @{ Name = "Storefront.Presentation"; Id = "BlazorShop.Storefront.Presentation"; Path = "BlazorShop.PresentationV2\BlazorShop.Storefront.Presentation\BlazorShop.Storefront.Presentation.csproj" },
        @{ Name = "Storefront.Components"; Id = "BlazorShop.Storefront.Components"; Path = "BlazorShop.PresentationV2\BlazorShop.Storefront.Components\BlazorShop.Storefront.Components.csproj" },
        @{ Name = "Storefront.Browser"; Id = "BlazorShop.Storefront.Browser"; Path = "BlazorShop.PresentationV2\BlazorShop.Storefront.Browser\BlazorShop.Storefront.Browser.csproj" }
    )

    foreach ($project in $projects) {
        $projectPath = Join-Path $RepoRoot $project.Path
        Invoke-CheckedCommand -Description "Pack $($project.Name)" -Command {
            dotnet pack $projectPath --configuration $Configuration --output $PackageRoot "/p:PackageVersion=$version"
        }
    }

    return @{
        SourceHead = $sourceHead
        PackageBuildIdentity = $packageBuildIdentity
        Version = $version
        PackageFeedPath = $PackageRoot
        Hashes = @{
            Client = Get-StorefrontPackageHash -PackageRoot $PackageRoot -PackageId "BlazorShop.Storefront.Client" -Version $version
            Runtime = Get-StorefrontPackageHash -PackageRoot $PackageRoot -PackageId "BlazorShop.Storefront.Runtime" -Version $version
            Presentation = Get-StorefrontPackageHash -PackageRoot $PackageRoot -PackageId "BlazorShop.Storefront.Presentation" -Version $version
            Components = Get-StorefrontPackageHash -PackageRoot $PackageRoot -PackageId "BlazorShop.Storefront.Components" -Version $version
            Browser = Get-StorefrontPackageHash -PackageRoot $PackageRoot -PackageId "BlazorShop.Storefront.Browser" -Version $version
        }
    }
}

function ConvertTo-StorefrontBuilderProjectName {
    param([Parameter(Mandatory = $true)][string]$InputName)

    $trimmed = $InputName.Trim()
    if ([string]::IsNullOrWhiteSpace($trimmed)) {
        throw "-Name must not be empty."
    }

    if ($trimmed.IndexOf("..", [System.StringComparison]::Ordinal) -ge 0 `
        -or $trimmed.IndexOf("\", [System.StringComparison]::Ordinal) -ge 0 `
        -or $trimmed.IndexOf("/", [System.StringComparison]::Ordinal) -ge 0 `
        -or $trimmed.IndexOf(":", [System.StringComparison]::Ordinal) -ge 0) {
        throw "-Name must not contain traversal, separators, or drive markers."
    }

    $prefix = "BlazorShop.Storefront."
    $suffix = if ($trimmed.StartsWith($prefix, [System.StringComparison]::Ordinal)) {
        $trimmed.Substring($prefix.Length)
    }
    else {
        if ($trimmed.IndexOf(".", [System.StringComparison]::Ordinal) -ge 0) {
            throw "-Name must be a friendly suffix or the full BlazorShop.Storefront.{Name} project name."
        }

        $trimmed
    }

    if ($suffix -cmatch "^[A-Z][A-Za-z0-9]*$") {
        return "$prefix$suffix"
    }

    $parts = @($suffix -split "[^A-Za-z0-9]+" | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
    if ($parts.Count -eq 0) {
        throw "-Name must contain at least one alphanumeric segment."
    }

    $normalizedSuffix = (($parts | ForEach-Object {
        $part = $_
        if ($part.Length -eq 1) {
            return $part.ToUpperInvariant()
        }

        return $part.Substring(0, 1).ToUpperInvariant() + $part.Substring(1).ToLowerInvariant()
    }) -join "")

    if ($normalizedSuffix -cnotmatch "^[A-Z][A-Za-z0-9]*$") {
        throw "-Name could not be normalized to a PascalCase project suffix. Input: '$InputName'."
    }

    return "$prefix$normalizedSuffix"
}

$repoRoot = Find-RepoRoot -StartPath $PSScriptRoot
$builderRoot = Join-Path $repoRoot "tools\BlazorShop.AI.StorefrontBuilder"
$builderScript = Join-Path $builderRoot "build-storefront.ps1"
$packageRoot = Join-Path $repoRoot "artifacts\storefront-packages"
$projectName = ConvertTo-StorefrontBuilderProjectName -InputName $Name

if ($projectName -ne $Name) {
    Write-Host "Normalized StorefrontBuilder project name: $Name -> $projectName"
}

if ($Mode -eq "preflight-only" -and [string]::IsNullOrWhiteSpace($HandoffRoot)) {
    throw "-Mode preflight-only requires -HandoffRoot <portable-handoff-root>."
}

Assert-NodeTooling -BuilderRoot $builderRoot

$packageSet = $null
if (($Mode -eq "generate" -or $Mode -eq "full") -and -not $SkipPackageRefresh -and -not $Describe) {
    $packageSet = New-StorefrontPackageSet -RepoRoot $repoRoot -PackageRoot $packageRoot -Configuration $Configuration
}

$builderArgs = @(
    "-Url", $Url,
    "-Name", $projectName,
    "-StoreKey", $StoreKey,
    "-OutputRoot", $OutputRoot,
    "-Mode", $Mode
)

$builderParams = @{
    Url = $Url
    Name = $projectName
    StoreKey = $StoreKey
    OutputRoot = $OutputRoot
    Mode = $Mode
}

if (-not [string]::IsNullOrWhiteSpace($HandoffRoot)) {
    $builderArgs += @("-HandoffRoot", $HandoffRoot)
    $builderParams.HandoffRoot = $HandoffRoot
    if (-not [string]::IsNullOrWhiteSpace($HandoffSchemaRoot)) {
        $builderArgs += @("-HandoffSchemaRoot", $HandoffSchemaRoot)
        $builderParams.HandoffSchemaRoot = $HandoffSchemaRoot
    }
}

if ($Force) {
    $builderArgs += "-Force"
    $builderParams.Force = $true
}

if ($null -ne $packageSet) {
    $builderArgs += @(
        "-SourceHead", $packageSet.SourceHead,
        "-PackageBuildIdentity", $packageSet.PackageBuildIdentity,
        "-StorefrontClientPackageVersion", $packageSet.Version,
        "-StorefrontRuntimePackageVersion", $packageSet.Version,
        "-StorefrontPresentationPackageVersion", $packageSet.Version,
        "-StorefrontComponentsPackageVersion", $packageSet.Version,
        "-StorefrontBrowserPackageVersion", $packageSet.Version,
        "-StorefrontClientPackageHash", $packageSet.Hashes.Client,
        "-StorefrontRuntimePackageHash", $packageSet.Hashes.Runtime,
        "-StorefrontPresentationPackageHash", $packageSet.Hashes.Presentation,
        "-StorefrontComponentsPackageHash", $packageSet.Hashes.Components,
        "-StorefrontBrowserPackageHash", $packageSet.Hashes.Browser,
        "-PackageFeedPath", $packageSet.PackageFeedPath
    )
    $builderParams.SourceHead = $packageSet.SourceHead
    $builderParams.PackageBuildIdentity = $packageSet.PackageBuildIdentity
    $builderParams.StorefrontClientPackageVersion = $packageSet.Version
    $builderParams.StorefrontRuntimePackageVersion = $packageSet.Version
    $builderParams.StorefrontPresentationPackageVersion = $packageSet.Version
    $builderParams.StorefrontComponentsPackageVersion = $packageSet.Version
    $builderParams.StorefrontBrowserPackageVersion = $packageSet.Version
    $builderParams.StorefrontClientPackageHash = $packageSet.Hashes.Client
    $builderParams.StorefrontRuntimePackageHash = $packageSet.Hashes.Runtime
    $builderParams.StorefrontPresentationPackageHash = $packageSet.Hashes.Presentation
    $builderParams.StorefrontComponentsPackageHash = $packageSet.Hashes.Components
    $builderParams.StorefrontBrowserPackageHash = $packageSet.Hashes.Browser
    $builderParams.PackageFeedPath = $packageSet.PackageFeedPath
}

if ($SkipVisualQa) {
    $builderArgs += "-SkipVisualQa"
    $builderParams.SkipVisualQa = $true
}

if ($SkipCommerceRegression) {
    $builderArgs += "-SkipCommerceRegression"
    $builderParams.SkipCommerceRegression = $true
}

$commandLine = Format-CommandLine -ScriptPath $builderScript -Arguments $builderArgs
Write-Host "== StorefrontBuilder command =="
Write-Host $commandLine

if ($Describe) {
    return
}

Push-Location $repoRoot
try {
    & $builderScript @builderParams
    if ($LASTEXITCODE -ne 0) {
        throw "StorefrontBuilder failed with exit code $LASTEXITCODE."
    }
}
finally {
    Pop-Location
}

Write-Host "StorefrontBuilder completed successfully."

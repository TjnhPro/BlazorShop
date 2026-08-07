param(
    [string]$Name = "BlazorShop.Storefront.GeneratedProof",
    [string]$WorkspaceRoot = "",
    [string]$ProjectRoot = "",
    [string]$Configuration = "Debug",
    [string]$StorefrontClientPackageVersion = "1.0.0-local",
    [string]$StorefrontRuntimePackageVersion = "1.0.0-local",
    [string]$StorefrontPresentationPackageVersion = "1.0.0-local",
    [string]$StorefrontComponentsPackageVersion = "1.0.0-local",
    [string]$StorefrontBrowserPackageVersion = "1.0.0-local",
    [switch]$Describe
)

$ErrorActionPreference = "Stop"
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
. (Join-Path $repoRoot "tools\BlazorShop.AI.StorefrontBuilder\scripts\generate\StorefrontBuilderProjectSafety.ps1")
function Resolve-RepoPath {
    param([string]$Path)

    if ([System.IO.Path]::IsPathRooted($Path)) {
        return [System.IO.Path]::GetFullPath($Path)
    }

    return [System.IO.Path]::GetFullPath((Join-Path $repoRoot $Path))
}

function Test-TextContains([string]$Text, [string]$Value, [System.StringComparison]$Comparison = [System.StringComparison]::Ordinal) {
    return $Text.IndexOf($Value, $Comparison) -ge 0
}

function New-IsolationGateError {
    param(
        [Parameter(Mandatory = $true)][string]$Code,
        [Parameter(Mandatory = $true)][string]$Problem,
        [Parameter(Mandatory = $true)][string]$Cause,
        [Parameter(Mandatory = $true)][string]$Fix
    )

    return "[$Code] Problem: $Problem Cause: $Cause Fix: $Fix"
}

function Get-RelativePathCompat([string]$BasePath, [string]$TargetPath) {
    $baseFullPath = [System.IO.Path]::GetFullPath($BasePath).TrimEnd([System.IO.Path]::DirectorySeparatorChar, [System.IO.Path]::AltDirectorySeparatorChar) + [System.IO.Path]::DirectorySeparatorChar
    $targetFullPath = [System.IO.Path]::GetFullPath($TargetPath)
    $baseUri = [System.Uri]::new($baseFullPath)
    $targetUri = [System.Uri]::new($targetFullPath)
    return [System.Uri]::UnescapeDataString($baseUri.MakeRelativeUri($targetUri).ToString()).Replace("/", [System.IO.Path]::DirectorySeparatorChar)
}

$workspacePaths = Resolve-StorefrontBuilderWorkspacePaths -RepoRoot $repoRoot -ProjectName $Name -WorkspaceRoot $WorkspaceRoot -ProjectRoot $ProjectRoot -OutputRoot "artifacts/storefront-builder/generated" -WarnOnProjectRootAlias
$Name = $workspacePaths.ProjectName
$projectRoot = $workspacePaths.WorkspaceRoot
$serverProjectRoot = $workspacePaths.ServerProjectRoot
$wasmProjectRoot = $workspacePaths.WasmProjectRoot
$solutionFile = $workspacePaths.SolutionPath
$projectFile = Join-Path $serverProjectRoot "$Name.csproj"
$wasmProjectFile = Join-Path $wasmProjectRoot "$Name.WASM.csproj"
$packageRoot = Join-Path (Join-Path $repoRoot "artifacts\storefront-packages") $Name
$clientProject = Join-Path $repoRoot "BlazorShop.PresentationV2\BlazorShop.Storefront.Client\BlazorShop.Storefront.Client.csproj"
$runtimeProject = Join-Path $repoRoot "BlazorShop.PresentationV2\BlazorShop.Storefront.Runtime\BlazorShop.Storefront.Runtime.csproj"
$presentationProject = Join-Path $repoRoot "BlazorShop.PresentationV2\BlazorShop.Storefront.Presentation\BlazorShop.Storefront.Presentation.csproj"
$componentsProject = Join-Path $repoRoot "BlazorShop.PresentationV2\BlazorShop.Storefront.Components\BlazorShop.Storefront.Components.csproj"
$browserProject = Join-Path $repoRoot "BlazorShop.PresentationV2\BlazorShop.Storefront.Browser\BlazorShop.Storefront.Browser.csproj"
$sourceHead = ""
$packageBuildIdentity = ""

function Initialize-StorefrontPackageIdentity {
    $head = (& git -C $repoRoot rev-parse HEAD 2>$null)
    if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($head)) {
        throw (New-IsolationGateError "SFB-ISOLATION-HEAD-001" "Cannot resolve source HEAD for package identity." "git rev-parse HEAD failed or returned empty output." "Run the gate from a git worktree with a valid HEAD.")
    }

    $identity = ([string]$head).Substring(0, 12)
    $script:sourceHead = [string]$head
    $script:packageBuildIdentity = $identity
    $derivedVersion = "1.0.0-local.$identity"
    if ($StorefrontClientPackageVersion -eq "1.0.0-local") { $script:StorefrontClientPackageVersion = $derivedVersion }
    if ($StorefrontRuntimePackageVersion -eq "1.0.0-local") { $script:StorefrontRuntimePackageVersion = $derivedVersion }
    if ($StorefrontPresentationPackageVersion -eq "1.0.0-local") { $script:StorefrontPresentationPackageVersion = $derivedVersion }
    if ($StorefrontComponentsPackageVersion -eq "1.0.0-local") { $script:StorefrontComponentsPackageVersion = $derivedVersion }
    if ($StorefrontBrowserPackageVersion -eq "1.0.0-local") { $script:StorefrontBrowserPackageVersion = $derivedVersion }
    Write-Host "Storefront package identity: $identity"
}

if ($Describe) {
    Write-Host "StorefrontBuilder isolation gate:"
    Write-StorefrontBuilderWorkspacePaths -Paths $workspacePaths
    Write-Host "- restore generated storefront"
    Write-Host "- build generated storefront"
    Write-Host "- pack BlazorShop.Storefront.Client"
    Write-Host "- pack BlazorShop.Storefront.Runtime"
    Write-Host "- pack BlazorShop.Storefront.Presentation"
    Write-Host "- pack BlazorShop.Storefront.Components"
    Write-Host "- pack BlazorShop.Storefront.Browser"
    Write-Host "- confirm visual package references, no direct Runtime/Client or Storefront.V2/Web.SharedV2/backend/core/API references"
    exit 0
}

if (-not (Test-Path $projectFile)) {
    throw (New-IsolationGateError "SFB-ISOLATION-000" "Generated storefront server project is missing: $projectFile" "The workspace does not match the starter-first sibling layout." "Regenerate the storefront workspace or pass the correct -WorkspaceRoot and -Name.")
}

if (-not (Test-Path $wasmProjectFile)) {
    throw (New-IsolationGateError "SFB-ISOLATION-000" "Generated storefront WASM project is missing: $wasmProjectFile" "The workspace does not contain the sibling WASM project." "Regenerate the storefront workspace or pass the correct -WorkspaceRoot and -Name.")
}

if (-not (Test-Path $solutionFile)) {
    throw (New-IsolationGateError "SFB-ISOLATION-000" "Generated storefront solution is missing: $solutionFile" "The workspace was generated with an old shape or the solution was deleted." "Regenerate the storefront workspace so the solution exists at the workspace root.")
}

Initialize-StorefrontPackageIdentity

function Clear-StorefrontLocalPackageCache {
    $globalPackageRoot = Join-Path ([Environment]::GetFolderPath("UserProfile")) ".nuget\packages"
    $resolvedGlobalPackageRoot = [System.IO.Path]::GetFullPath($globalPackageRoot)
    $packages = @(
        @{ Id = "blazorshop.storefront.client"; Version = $StorefrontClientPackageVersion },
        @{ Id = "blazorshop.storefront.runtime"; Version = $StorefrontRuntimePackageVersion },
        @{ Id = "blazorshop.storefront.presentation"; Version = $StorefrontPresentationPackageVersion },
        @{ Id = "blazorshop.storefront.components"; Version = $StorefrontComponentsPackageVersion },
        @{ Id = "blazorshop.storefront.browser"; Version = $StorefrontBrowserPackageVersion }
    )

    foreach ($package in $packages) {
        $versionPath = Join-Path $globalPackageRoot "$($package.Id)\$($package.Version)"
        $resolvedVersionPath = [System.IO.Path]::GetFullPath($versionPath)
        if (-not $resolvedVersionPath.StartsWith($resolvedGlobalPackageRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
            throw (New-IsolationGateError "SFB-ISOLATION-004" "Refusing to clean NuGet cache path outside global package root: $resolvedVersionPath" "The computed package cache path escaped the NuGet global package root." "Check package id/version inputs before rerunning the gate.")
        }

        if (Test-Path $resolvedVersionPath) {
            Remove-Item -LiteralPath $resolvedVersionPath -Recurse -Force
        }
    }
}

function Write-GeneratedNuGetConfig {
    $packageFeed = $packageRoot
    $relativePackageFeed = (Get-RelativePathCompat $projectRoot $packageFeed).Replace('\', '/')
    $nugetConfig = @(
        '<?xml version="1.0" encoding="utf-8"?>',
        '<configuration>',
        '  <packageSources>',
        '    <clear />',
        "    <add key=`"local-storefront-packages`" value=`"$relativePackageFeed`" />",
        '    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />',
        '  </packageSources>',
        '</configuration>'
    ) -join [Environment]::NewLine
    Set-Content -LiteralPath (Join-Path $projectRoot "nuget.config") -Value $nugetConfig -Encoding UTF8
}

function Update-GeneratedPackageVersionProps {
    $propsPath = Join-Path $projectRoot "StorefrontPackageVersions.props"
    if (-not (Test-Path -LiteralPath $propsPath)) {
        throw (New-IsolationGateError "SFB-ISOLATION-003" "Package compatibility metadata is missing: $propsPath" "The workspace does not have shared StorefrontPackageVersions.props at its root." "Regenerate the storefront workspace with the starter-first generator.")
    }

    [xml]$props = Get-Content -LiteralPath $propsPath -Raw
    $propertyGroup = @($props.Project.PropertyGroup)[0]
    $propertyGroup.StorefrontClientPackageVersion = $StorefrontClientPackageVersion
    $propertyGroup.StorefrontRuntimePackageVersion = $StorefrontRuntimePackageVersion
    $propertyGroup.StorefrontPresentationPackageVersion = $StorefrontPresentationPackageVersion
    $propertyGroup.StorefrontComponentsPackageVersion = $StorefrontComponentsPackageVersion
    $propertyGroup.StorefrontBrowserPackageVersion = $StorefrontBrowserPackageVersion
    $props.Save($propsPath)
}

function Get-ExpectedStorefrontPackages {
    return @(
        @{ Id = "BlazorShop.Storefront.Client"; Version = $StorefrontClientPackageVersion },
        @{ Id = "BlazorShop.Storefront.Runtime"; Version = $StorefrontRuntimePackageVersion },
        @{ Id = "BlazorShop.Storefront.Presentation"; Version = $StorefrontPresentationPackageVersion },
        @{ Id = "BlazorShop.Storefront.Components"; Version = $StorefrontComponentsPackageVersion },
        @{ Id = "BlazorShop.Storefront.Browser"; Version = $StorefrontBrowserPackageVersion }
    )
}

function Get-LocalPackageHash {
    param(
        [Parameter(Mandatory = $true)][hashtable]$Package
    )

    $packagePath = Join-Path $packageRoot "$($Package.Id).$($Package.Version).nupkg"
    if (-not (Test-Path -LiteralPath $packagePath)) {
        throw (New-IsolationGateError "SFB-ISOLATION-006" "Packed Storefront package is missing from local feed: $packagePath" "The pack step did not produce the package expected by metadata validation." "Review the preceding dotnet pack output and rerun the gate after the package builds.")
    }

    return (Get-FileHash -LiteralPath $packagePath -Algorithm SHA256).Hash.ToLowerInvariant()
}

function Update-GeneratedMetadataPackageProvenance {
    $metadataPath = Join-Path $projectRoot "docs\storefront-analysis\metadata.yaml"
    if (-not (Test-Path -LiteralPath $metadataPath)) {
        throw (New-IsolationGateError "SFB-ISOLATION-006" "Generated metadata is missing: $metadataPath" "The generated workspace does not contain analysis metadata." "Regenerate the storefront workspace before running package isolation.")
    }

    $metadata = Get-Content -LiteralPath $metadataPath -Raw
    $relativePackageFeed = (Get-RelativePathCompat $projectRoot $packageRoot).Replace('\', '/')
    $metadata = [regex]::Replace($metadata, "(?m)^sourceHead:\s*.*$", "sourceHead: $sourceHead")
    $metadata = [regex]::Replace($metadata, "(?m)^packageBuildIdentity:\s*.*$", "packageBuildIdentity: $packageBuildIdentity")
    $metadata = [regex]::Replace($metadata, "(?m)^  feedPath:\s*.*$", "  feedPath: $relativePackageFeed")

    foreach ($package in Get-ExpectedStorefrontPackages) {
        $packageId = $package.Id
        $version = $package.Version
        $hash = Get-LocalPackageHash -Package $package
        $escapedId = [regex]::Escape($packageId)
        $metadata = [regex]::Replace($metadata, "(?m)^  ${escapedId}:\s*.*$", "  ${packageId}: $version")
        $metadata = [regex]::Replace(
            $metadata,
            "(?ms)(\s+- id:\s*$escapedId\s*\r?\n\s+version:\s*)\S+(\s*\r?\n\s+sha256:\s*)\S+",
            "`${1}$version`${2}$hash")
    }

    Set-Content -LiteralPath $metadataPath -Value $metadata -Encoding UTF8
}

function Read-GeneratedMetadataPackageProvenance {
    $metadataPath = Join-Path $projectRoot "docs\storefront-analysis\metadata.yaml"
    $metadata = Get-Content -LiteralPath $metadataPath -Raw
    $relativePackageFeed = (Get-RelativePathCompat $projectRoot $packageRoot).Replace('\', '/')

    foreach ($requiredMarker in @("sourceHead: $sourceHead", "packageBuildIdentity: $packageBuildIdentity", "feedPath: $relativePackageFeed")) {
        if (-not (Test-TextContains $metadata $requiredMarker)) {
            throw (New-IsolationGateError "SFB-ISOLATION-006" "Generated metadata package provenance is missing '$requiredMarker'." "Package provenance was not written or was overwritten after packing." "Rerun the isolation gate so metadata and generated-files.yaml are refreshed together.")
        }
    }

    $packagesById = @{}
    foreach ($package in Get-ExpectedStorefrontPackages) {
        $packageId = $package.Id
        $version = $package.Version
        $hash = Get-LocalPackageHash -Package $package
        if (-not (Test-TextContains $metadata "  ${packageId}: $version")) {
            throw (New-IsolationGateError "SFB-ISOLATION-006" "Generated metadata packageVersions does not match restore package '$packageId/$version'." "StorefrontPackageVersions.props and metadata packageVersions are out of sync." "Rerun the isolation gate or regenerate the workspace with matching package version arguments.")
        }

        $escapedId = [regex]::Escape($packageId)
        $blockPattern = "(?ms)\s+- id:\s*${escapedId}\s*\r?\n\s+version:\s*(?<version>\S+)\s*\r?\n\s+sha256:\s*(?<hash>\S+)"
        $blockMatch = [regex]::Match($metadata, $blockPattern)
        if (-not $blockMatch.Success) {
            throw (New-IsolationGateError "SFB-ISOLATION-006" "Generated metadata packageProvenance is missing '$packageId'." "The package provenance block is incomplete." "Rerun the isolation gate so all package hashes are recorded.")
        }

        if ($blockMatch.Groups["version"].Value -ne $version -or $blockMatch.Groups["hash"].Value -ne $hash) {
            throw (New-IsolationGateError "SFB-ISOLATION-006" "Generated metadata packageProvenance does not match local package '$packageId/$version'." "The local package hash/version differs from metadata." "Rerun the isolation gate after rebuilding packages, or regenerate the workspace with the intended package feed.")
        }

        $packagesById[$packageId] = @{
            Id = $packageId
            Version = $version
            Sha256 = $hash
        }
    }

    return $packagesById
}

function Assert-RestoredProjectAssets {
    param(
        [Parameter(Mandatory = $true)][string]$AssetsPath,
        [Parameter(Mandatory = $true)][array]$ExpectedPackages
    )

    if (-not (Test-Path -LiteralPath $AssetsPath)) {
        throw (New-IsolationGateError "SFB-ISOLATION-005" "Restore did not create project.assets.json: $AssetsPath" "dotnet restore did not complete for the generated project." "Review restore output and rerun after package source or project errors are fixed.")
    }

    $assets = Get-Content -LiteralPath $AssetsPath -Raw
    foreach ($package in $ExpectedPackages) {
        $marker = "$($package.Id)/$($package.Version)"
        if (-not (Test-TextContains $assets $marker ([System.StringComparison]::OrdinalIgnoreCase))) {
            throw (New-IsolationGateError "SFB-ISOLATION-005" "project.assets.json is missing restored package '$marker': $AssetsPath" "The generated project did not restore the expected Storefront package from the shared version source." "Check StorefrontPackageVersions.props, nuget.config, and package feed contents, then rerun the gate.")
        }
    }
}

New-Item -ItemType Directory -Force -Path $packageRoot | Out-Null
Clear-StorefrontLocalPackageCache
dotnet pack $clientProject --configuration $Configuration --output $packageRoot "/p:PackageVersion=$StorefrontClientPackageVersion"
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
dotnet pack $runtimeProject --configuration $Configuration --output $packageRoot "/p:PackageVersion=$StorefrontRuntimePackageVersion"
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
dotnet pack $presentationProject --configuration $Configuration --output $packageRoot "/p:PackageVersion=$StorefrontPresentationPackageVersion"
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
dotnet pack $componentsProject --configuration $Configuration --output $packageRoot "/p:PackageVersion=$StorefrontComponentsPackageVersion"
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
dotnet pack $browserProject --configuration $Configuration --output $packageRoot "/p:PackageVersion=$StorefrontBrowserPackageVersion"
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
Update-GeneratedPackageVersionProps
Update-GeneratedMetadataPackageProvenance
node (Join-Path $repoRoot "tools\BlazorShop.AI.StorefrontBuilder\scripts\generate\update-generated-files-manifest.mjs") --workspace-root $projectRoot --intentional-changes "__all__"
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
$metadataPackagesById = Read-GeneratedMetadataPackageProvenance
Write-GeneratedNuGetConfig
dotnet restore $solutionFile --no-cache --force-evaluate
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
Assert-RestoredProjectAssets -AssetsPath (Join-Path $serverProjectRoot "obj\project.assets.json") -ExpectedPackages @(
    $metadataPackagesById["BlazorShop.Storefront.Presentation"],
    $metadataPackagesById["BlazorShop.Storefront.Components"],
    $metadataPackagesById["BlazorShop.Storefront.Browser"]
)
Assert-RestoredProjectAssets -AssetsPath (Join-Path $wasmProjectRoot "obj\project.assets.json") -ExpectedPackages @(
    $metadataPackagesById["BlazorShop.Storefront.Components"],
    $metadataPackagesById["BlazorShop.Storefront.Browser"]
)
dotnet build $solutionFile --configuration $Configuration --no-restore
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$project = Get-Content -LiteralPath $projectFile -Raw
$wasmProject = Get-Content -LiteralPath $wasmProjectFile -Raw
foreach ($package in @("Microsoft.AspNetCore.Components.WebAssembly.Server", "BlazorShop.Storefront.Presentation", "BlazorShop.Storefront.Components", "BlazorShop.Storefront.Browser")) {
    if (-not (Test-TextContains $project "PackageReference Include=`"$package`"")) {
        throw (New-IsolationGateError "SFB-ISOLATION-001" "Generated storefront server must consume '$package' as a package reference." "The server project does not match the package-boundary contract." "Regenerate the workspace or restore the required server PackageReference.")
    }
}

foreach ($package in @("Microsoft.AspNetCore.Components.WebAssembly", "BlazorShop.Storefront.Components", "BlazorShop.Storefront.Browser")) {
    if (-not (Test-TextContains $wasmProject "PackageReference Include=`"$package`"")) {
        throw (New-IsolationGateError "SFB-ISOLATION-001" "Generated storefront WASM must consume '$package' as a package reference." "The WASM project does not match the package-boundary contract." "Regenerate the workspace or restore the required WASM PackageReference.")
    }
}

foreach ($package in @("BlazorShop.Storefront.Runtime", "BlazorShop.Storefront.Client")) {
    if (Test-TextContains $project "PackageReference Include=`"$package`"") {
        throw (New-IsolationGateError "SFB-ISOLATION-001" "Generated storefront server must not direct-reference '$package'." "The generated server is bypassing Presentation/Runtime transport ownership." "Remove the direct package reference and regenerate from the starter-first workspace contract.")
    }

    if (Test-TextContains $wasmProject "PackageReference Include=`"$package`"") {
        throw (New-IsolationGateError "SFB-ISOLATION-001" "Generated storefront WASM must not direct-reference '$package'." "The generated WASM project is bypassing Browser/Presentation transport ownership." "Remove the direct package reference and regenerate from the starter-first workspace contract.")
    }
}

[xml]$serverProjectDocument = Get-Content -LiteralPath $projectFile -Raw
[xml]$wasmProjectDocument = Get-Content -LiteralPath $wasmProjectFile -Raw
$serverProjectReferences = @(@($serverProjectDocument.Project.ItemGroup.ProjectReference) |
    ForEach-Object { [string]$_.Include } |
    Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
$wasmProjectReferences = @(@($wasmProjectDocument.Project.ItemGroup.ProjectReference) |
    ForEach-Object { [string]$_.Include } |
    Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
$expectedWasmReference = "..\$Name.WASM\$Name.WASM.csproj"
$expectedWasmReferenceFullPath = [System.IO.Path]::GetFullPath($wasmProjectFile)
if ($serverProjectReferences.Count -ne 1 -or -not ([string]$serverProjectReferences[0]).Equals($expectedWasmReference, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw (New-IsolationGateError "SFB-ISOLATION-002" "Generated server must reference only sibling WASM '$expectedWasmReference'. Actual: $($serverProjectReferences -join ', ')" "The server project reference is missing, external, or still uses the retired nested shape." "Regenerate the workspace so the server references only the sibling WASM project.")
}

$resolvedProjectRoot = [System.IO.Path]::GetFullPath($projectRoot).TrimEnd([System.IO.Path]::DirectorySeparatorChar, [System.IO.Path]::AltDirectorySeparatorChar)
$resolvedProjectRootWithSeparator = "$resolvedProjectRoot$([System.IO.Path]::DirectorySeparatorChar)"
foreach ($reference in $serverProjectReferences) {
    $resolvedReference = [System.IO.Path]::GetFullPath((Join-Path $serverProjectRoot $reference))
    if (-not $resolvedReference.StartsWith($resolvedProjectRootWithSeparator, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw (New-IsolationGateError "SFB-ISOLATION-002" "Generated server ProjectReference leaves generated root: $reference" "The generated project references monorepo or external source instead of generated sibling source." "Replace the ProjectReference with the generated sibling WASM reference or regenerate the workspace.")
    }

    if (-not $resolvedReference.Equals($expectedWasmReferenceFullPath, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw (New-IsolationGateError "SFB-ISOLATION-002" "Generated server ProjectReference must resolve to generated sibling WASM: $reference" "The ProjectReference path resolves to the wrong target." "Regenerate the workspace or fix the ProjectReference to '$expectedWasmReference'.")
    }
}

if ($wasmProjectReferences.Count -ne 0) {
    throw (New-IsolationGateError "SFB-ISOLATION-002" "Generated WASM project must not contain ProjectReference entries. Actual: $($wasmProjectReferences -join ', ')" "WASM must consume Browser/Components through packages and must not reference server or monorepo projects." "Remove WASM ProjectReference entries and regenerate if needed.")
}

$forbidden = @("BlazorShop.Storefront.V2", "BlazorShop.Storefront.V2.WASM", "BlazorShop.Storefront.Starter", "BlazorShop.Storefront.Starter.WASM", "BlazorShop.Web.SharedV2", "Web.SharedV2", "BlazorShop.Application", "BlazorShop.Domain", "BlazorShop.Infrastructure", "BlazorShop.CommerceNode.API", "BlazorShop.ControlPlane.API", "PackageReference Include=`"BlazorShop.Storefront.Runtime`"", "PackageReference Include=`"BlazorShop.Storefront.Client`"")
Get-ChildItem -LiteralPath $projectRoot -Recurse -File |
    ForEach-Object {
        $relativeToProject = (Get-RelativePathCompat $projectRoot $_.FullName).Replace("\", "/")
        if ($relativeToProject -match "(^|/)(bin|obj)/" -or $relativeToProject.StartsWith("docs/storefront-analysis/", [System.StringComparison]::OrdinalIgnoreCase) -or $relativeToProject -eq "README.md") {
            return
        }

        $content = Get-Content -LiteralPath $_.FullName -Raw
        foreach ($pattern in $forbidden) {
            if (Test-TextContains $content $pattern ([System.StringComparison]::OrdinalIgnoreCase)) {
                throw (New-IsolationGateError "SFB-ISOLATION-002" "Forbidden dependency '$pattern' found in $($_.FullName)." "Generated source/project files crossed StorefrontBuilder package boundaries." "Remove the forbidden reference and regenerate from the starter-first workspace contract.")
            }
        }
    }

$metadata = Get-Content -LiteralPath (Join-Path $projectRoot "StorefrontPackageVersions.props") -Raw
if (-not (Test-TextContains $metadata "StorefrontClientPackageVersion") -or -not (Test-TextContains $metadata "StorefrontRuntimePackageVersion") -or -not (Test-TextContains $metadata "StorefrontPresentationPackageVersion") -or -not (Test-TextContains $metadata "StorefrontComponentsPackageVersion") -or -not (Test-TextContains $metadata "StorefrontBrowserPackageVersion")) {
    throw (New-IsolationGateError "SFB-ISOLATION-003" "Package compatibility metadata is missing." "StorefrontPackageVersions.props does not contain all required Storefront package version properties." "Restore the shared props file from Starter or regenerate the workspace.")
}

Write-Host "StorefrontBuilder isolation gate passed for $Name."

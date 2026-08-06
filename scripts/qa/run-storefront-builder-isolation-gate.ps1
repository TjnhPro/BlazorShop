param(
    [string]$Name = "BlazorShop.Storefront.GeneratedProof",
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

function Get-RelativePathCompat([string]$BasePath, [string]$TargetPath) {
    $baseFullPath = [System.IO.Path]::GetFullPath($BasePath).TrimEnd([System.IO.Path]::DirectorySeparatorChar, [System.IO.Path]::AltDirectorySeparatorChar) + [System.IO.Path]::DirectorySeparatorChar
    $targetFullPath = [System.IO.Path]::GetFullPath($TargetPath)
    $baseUri = [System.Uri]::new($baseFullPath)
    $targetUri = [System.Uri]::new($targetFullPath)
    return [System.Uri]::UnescapeDataString($baseUri.MakeRelativeUri($targetUri).ToString()).Replace("/", [System.IO.Path]::DirectorySeparatorChar)
}

$projectRoot = if ([string]::IsNullOrWhiteSpace($ProjectRoot)) {
    Join-Path $repoRoot "artifacts\storefront-builder\generated\$Name"
} else {
    Resolve-RepoPath $ProjectRoot
}
$projectFile = Join-Path $projectRoot "$Name.csproj"
$packageRoot = Join-Path $repoRoot "artifacts\storefront-packages"
$clientProject = Join-Path $repoRoot "BlazorShop.PresentationV2\BlazorShop.Storefront.Client\BlazorShop.Storefront.Client.csproj"
$runtimeProject = Join-Path $repoRoot "BlazorShop.PresentationV2\BlazorShop.Storefront.Runtime\BlazorShop.Storefront.Runtime.csproj"
$presentationProject = Join-Path $repoRoot "BlazorShop.PresentationV2\BlazorShop.Storefront.Presentation\BlazorShop.Storefront.Presentation.csproj"
$componentsProject = Join-Path $repoRoot "BlazorShop.PresentationV2\BlazorShop.Storefront.Components\BlazorShop.Storefront.Components.csproj"
$browserProject = Join-Path $repoRoot "BlazorShop.PresentationV2\BlazorShop.Storefront.Browser\BlazorShop.Storefront.Browser.csproj"

function Initialize-StorefrontPackageIdentity {
    $head = (& git -C $repoRoot rev-parse HEAD 2>$null)
    if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($head)) {
        throw "[SFB-ISOLATION-HEAD-001] Cannot resolve source HEAD for package identity."
    }

    $identity = ([string]$head).Substring(0, 12)
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
    throw "[SFB-ISOLATION-000] Generated storefront project is missing: $projectFile"
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
            throw "[SFB-ISOLATION-004] Refusing to clean NuGet cache path outside global package root: $resolvedVersionPath"
        }

        if (Test-Path $resolvedVersionPath) {
            Remove-Item -LiteralPath $resolvedVersionPath -Recurse -Force
        }
    }
}

function Write-GeneratedNuGetConfig {
    $packageFeed = Join-Path $repoRoot "artifacts\storefront-packages"
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
Write-GeneratedNuGetConfig
dotnet restore $projectFile --no-cache --force-evaluate
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
dotnet build $projectFile --configuration $Configuration --no-restore
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$project = Get-Content -LiteralPath $projectFile -Raw
foreach ($package in @("BlazorShop.Storefront.Presentation", "BlazorShop.Storefront.Components", "BlazorShop.Storefront.Browser")) {
    if (-not (Test-TextContains $project "PackageReference Include=`"$package`"")) {
        throw "[SFB-ISOLATION-001] Generated storefront must consume '$package' as a package reference."
    }
}

foreach ($package in @("BlazorShop.Storefront.Runtime", "BlazorShop.Storefront.Client")) {
    if (Test-TextContains $project "PackageReference Include=`"$package`"") {
        throw "[SFB-ISOLATION-001] Generated storefront must not direct-reference '$package'."
    }
}

$forbidden = @("ProjectReference", "BlazorShop.Storefront.V2", "BlazorShop.Web.SharedV2", "Web.SharedV2", "BlazorShop.Application", "BlazorShop.Domain", "BlazorShop.Infrastructure", "BlazorShop.CommerceNode.API", "BlazorShop.ControlPlane.API", "PackageReference Include=`"BlazorShop.Storefront.Runtime`"", "PackageReference Include=`"BlazorShop.Storefront.Client`"")
Get-ChildItem -LiteralPath $projectRoot -Recurse -File |
    Where-Object { $_.FullName -notmatch "\\(bin|obj)\\" } |
    ForEach-Object {
        $content = Get-Content -LiteralPath $_.FullName -Raw
        foreach ($pattern in $forbidden) {
            if (Test-TextContains $content $pattern ([System.StringComparison]::OrdinalIgnoreCase)) {
                throw "[SFB-ISOLATION-002] Forbidden dependency '$pattern' found in $($_.FullName)."
            }
        }
    }

$metadata = Get-Content -LiteralPath (Join-Path $projectRoot "StorefrontPackageVersions.props") -Raw
if (-not (Test-TextContains $metadata "StorefrontClientPackageVersion") -or -not (Test-TextContains $metadata "StorefrontRuntimePackageVersion") -or -not (Test-TextContains $metadata "StorefrontPresentationPackageVersion") -or -not (Test-TextContains $metadata "StorefrontComponentsPackageVersion") -or -not (Test-TextContains $metadata "StorefrontBrowserPackageVersion")) {
    throw "[SFB-ISOLATION-003] Package compatibility metadata is missing."
}

Write-Host "StorefrontBuilder isolation gate passed for $Name."

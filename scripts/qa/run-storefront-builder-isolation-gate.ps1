param(
    [string]$Name = "BlazorShop.Storefront.GeneratedProof",
    [string]$ProjectRoot = "",
    [string]$Configuration = "Debug",
    [string]$StorefrontClientPackageVersion = "1.0.0-local",
    [string]$StorefrontRuntimePackageVersion = "1.0.0-local",
    [string]$StorefrontPresentationPackageVersion = "1.0.0-local",
    [string]$StorefrontComponentsPackageVersion = "1.0.0-local",
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

if ($Describe) {
    Write-Host "StorefrontBuilder isolation gate:"
    Write-Host "- restore generated storefront"
    Write-Host "- build generated storefront"
    Write-Host "- pack BlazorShop.Storefront.Client"
    Write-Host "- pack BlazorShop.Storefront.Runtime"
    Write-Host "- pack BlazorShop.Storefront.Presentation"
    Write-Host "- pack BlazorShop.Storefront.Components"
    Write-Host "- confirm visual package references, no direct Runtime/Client or Storefront.V2/Web.SharedV2/backend/core/API references"
    exit 0
}

if (-not (Test-Path $projectFile)) {
    throw "[SFB-ISOLATION-000] Generated storefront project is missing: $projectFile"
}

function Clear-StorefrontLocalPackageCache {
    $globalPackageRoot = Join-Path ([Environment]::GetFolderPath("UserProfile")) ".nuget\packages"
    $packages = @(
        @{ Id = "blazorshop.storefront.client"; Version = $StorefrontClientPackageVersion },
        @{ Id = "blazorshop.storefront.runtime"; Version = $StorefrontRuntimePackageVersion },
        @{ Id = "blazorshop.storefront.presentation"; Version = $StorefrontPresentationPackageVersion },
        @{ Id = "blazorshop.storefront.components"; Version = $StorefrontComponentsPackageVersion }
    )

    foreach ($package in $packages) {
        $versionPath = Join-Path $globalPackageRoot "$($package.Id)\$($package.Version)"
        if (Test-Path $versionPath) {
            Remove-Item -LiteralPath $versionPath -Recurse -Force
        }
    }
}

function Write-GeneratedNuGetConfig {
    $packageFeed = Join-Path $repoRoot "artifacts\storefront-packages"
    $relativePackageFeed = [System.IO.Path]::GetRelativePath($projectRoot, $packageFeed).Replace('\', '/')
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
Write-GeneratedNuGetConfig
dotnet restore $projectFile --no-cache --force-evaluate
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
dotnet build $projectFile --configuration $Configuration --no-restore
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$project = Get-Content -LiteralPath $projectFile -Raw
foreach ($package in @("BlazorShop.Storefront.Presentation", "BlazorShop.Storefront.Components")) {
    if (-not $project.Contains("PackageReference Include=`"$package`"", [System.StringComparison]::Ordinal)) {
        throw "[SFB-ISOLATION-001] Generated storefront must consume '$package' as a package reference."
    }
}

foreach ($package in @("BlazorShop.Storefront.Runtime", "BlazorShop.Storefront.Client")) {
    if ($project.Contains("PackageReference Include=`"$package`"", [System.StringComparison]::Ordinal)) {
        throw "[SFB-ISOLATION-001] Generated storefront must not direct-reference '$package'."
    }
}

$forbidden = @("ProjectReference", "BlazorShop.Storefront.V2", "BlazorShop.Web.SharedV2", "Web.SharedV2", "BlazorShop.Application", "BlazorShop.Domain", "BlazorShop.Infrastructure", "BlazorShop.CommerceNode.API", "BlazorShop.ControlPlane.API", "PackageReference Include=`"BlazorShop.Storefront.Runtime`"", "PackageReference Include=`"BlazorShop.Storefront.Client`"")
Get-ChildItem -LiteralPath $projectRoot -Recurse -File |
    Where-Object { $_.FullName -notmatch "\\(bin|obj)\\" } |
    ForEach-Object {
        $content = Get-Content -LiteralPath $_.FullName -Raw
        foreach ($pattern in $forbidden) {
            if ($content.Contains($pattern, [System.StringComparison]::OrdinalIgnoreCase)) {
                throw "[SFB-ISOLATION-002] Forbidden dependency '$pattern' found in $($_.FullName)."
            }
        }
    }

$metadata = Get-Content -LiteralPath (Join-Path $projectRoot "StorefrontPackageVersions.props") -Raw
if (-not $metadata.Contains("StorefrontClientPackageVersion", [System.StringComparison]::Ordinal) -or -not $metadata.Contains("StorefrontRuntimePackageVersion", [System.StringComparison]::Ordinal) -or -not $metadata.Contains("StorefrontPresentationPackageVersion", [System.StringComparison]::Ordinal) -or -not $metadata.Contains("StorefrontComponentsPackageVersion", [System.StringComparison]::Ordinal)) {
    throw "[SFB-ISOLATION-003] Package compatibility metadata is missing."
}

Write-Host "StorefrontBuilder isolation gate passed for $Name."

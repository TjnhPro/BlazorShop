param(
    [string]$Configuration = "Release",
    [string]$StorefrontClientPackageVersion = "1.0.0-local",
    [string]$StorefrontRuntimePackageVersion = "1.0.0-local",
    [string]$StorefrontPresentationPackageVersion = "1.0.0-local",
    [string]$StorefrontComponentsPackageVersion = "1.0.0-local",
    [string]$StorefrontBrowserPackageVersion = "1.0.0-local",
    [switch]$Describe
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$clientProject = Join-Path $repoRoot "BlazorShop.PresentationV2\BlazorShop.Storefront.Client\BlazorShop.Storefront.Client.csproj"
$runtimeProject = Join-Path $repoRoot "BlazorShop.PresentationV2\BlazorShop.Storefront.Runtime\BlazorShop.Storefront.Runtime.csproj"
$presentationProject = Join-Path $repoRoot "BlazorShop.PresentationV2\BlazorShop.Storefront.Presentation\BlazorShop.Storefront.Presentation.csproj"
$componentsProject = Join-Path $repoRoot "BlazorShop.PresentationV2\BlazorShop.Storefront.Components\BlazorShop.Storefront.Components.csproj"
$browserProject = Join-Path $repoRoot "BlazorShop.PresentationV2\BlazorShop.Storefront.Browser\BlazorShop.Storefront.Browser.csproj"
$starterSource = Join-Path $repoRoot "BlazorShop.PresentationV2\BlazorShop.Storefront.Starter"
$isolationRoot = Join-Path $repoRoot "obj\storefront-starter-isolation"
$feedRoot = Join-Path $isolationRoot "feed"
$sampleRoot = Join-Path $isolationRoot "Storefront.Sample"
$starterProject = Join-Path $sampleRoot "BlazorShop.Storefront.Starter.csproj"
$publishRoot = Join-Path $isolationRoot "publish"

$forbiddenPatterns = @(
    "ProjectReference",
    "BlazorShop.Application",
    "BlazorShop.Domain",
    "BlazorShop.Infrastructure",
    "BlazorShop.CommerceNode.API",
    "BlazorShop.ControlPlane.API",
    "BlazorShop.ControlPlane.Web",
    "BlazorShop.Storefront.V2",
    "BlazorShop.Web.SharedV2",
    "Web.SharedV2",
    "..\BlazorShop.",
    "../BlazorShop."
)

if ($Describe) {
    Write-Host "Storefront Starter isolation gate"
    Write-Host "- Pack Storefront.Client to local feed"
    Write-Host "- Pack Storefront.Runtime to local feed"
    Write-Host "- Pack Storefront.Presentation to local feed"
    Write-Host "- Pack Storefront.Components to local feed"
    Write-Host "- Pack Storefront.Browser to local feed"
    Write-Host "- Copy Starter source to obj/storefront-starter-isolation/Storefront.Sample"
    Write-Host "- Rewrite Starter Presentation ProjectReference to a PackageReference"
    Write-Host "- Restore from local package feed"
    Write-Host "- Build isolated Starter/Sample copy"
    Write-Host "- Publish isolated Starter/Sample copy"
    Write-Host "- Fail on backend/V2/Web.SharedV2/ProjectReference source paths"
    exit 0
}

function Assert-UnderRepoObj {
    param([string]$Path)

    $resolved = [System.IO.Path]::GetFullPath($Path)
    $expectedPrefix = [System.IO.Path]::GetFullPath((Join-Path $repoRoot "obj"))
    if (-not $resolved.StartsWith($expectedPrefix, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to modify path outside repo obj directory: $resolved"
    }
}

function Invoke-Step {
    param(
        [string]$Name,
        [scriptblock]$Action
    )

    Write-Host "== $Name =="
    & $Action
}

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
            throw "Refusing to clean NuGet cache path outside global package root: $resolvedVersionPath"
        }

        if (Test-Path $resolvedVersionPath) {
            Remove-Item -LiteralPath $resolvedVersionPath -Recurse -Force
        }
    }
}

Assert-UnderRepoObj $isolationRoot

Invoke-Step "Clean isolation directory" {
    if (Test-Path $isolationRoot) {
        Remove-Item -LiteralPath $isolationRoot -Recurse -Force
    }

    New-Item -ItemType Directory -Force -Path $feedRoot | Out-Null
    New-Item -ItemType Directory -Force -Path $sampleRoot | Out-Null
    New-Item -ItemType Directory -Force -Path $publishRoot | Out-Null
}

Invoke-Step "Clear local package cache" {
    Clear-StorefrontLocalPackageCache
}

Invoke-Step "Pack Storefront.Client" {
    dotnet pack $clientProject --configuration $Configuration --no-restore --output $feedRoot "/p:PackageVersion=$StorefrontClientPackageVersion"
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}

Invoke-Step "Pack Storefront.Runtime" {
    dotnet pack $runtimeProject --configuration $Configuration --no-restore --output $feedRoot "/p:PackageVersion=$StorefrontRuntimePackageVersion"
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}

Invoke-Step "Pack Storefront.Presentation" {
    dotnet pack $presentationProject --configuration $Configuration --no-restore --output $feedRoot "/p:PackageVersion=$StorefrontPresentationPackageVersion"
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}

Invoke-Step "Pack Storefront.Components" {
    dotnet pack $componentsProject --configuration $Configuration --no-restore --output $feedRoot "/p:PackageVersion=$StorefrontComponentsPackageVersion"
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}

Invoke-Step "Pack Storefront.Browser" {
    dotnet pack $browserProject --configuration $Configuration --no-restore --output $feedRoot "/p:PackageVersion=$StorefrontBrowserPackageVersion"
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}

Invoke-Step "Copy Starter source into isolated sample directory" {
    Get-ChildItem -LiteralPath $starterSource -Force |
        Where-Object { $_.Name -notin @("bin", "obj") } |
        ForEach-Object {
            Copy-Item -LiteralPath $_.FullName -Destination $sampleRoot -Recurse -Force
        }
}

Invoke-Step "Rewrite isolated Starter to package mode" {
    $projectContent = Get-Content -LiteralPath $starterProject -Raw
    $projectContent = $projectContent.Replace(
        '    <ProjectReference Include="..\BlazorShop.Storefront.Presentation\BlazorShop.Storefront.Presentation.csproj" />',
        '    <PackageReference Include="BlazorShop.Storefront.Presentation" Version="$(StorefrontPresentationPackageVersion)" />')
    Set-Content -LiteralPath $starterProject -Value $projectContent -Encoding UTF8
}

Invoke-Step "Write isolated local feed config" {
    @"
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="local-storefront-packages" value="$feedRoot" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
</configuration>
"@ | Set-Content -LiteralPath (Join-Path $sampleRoot "nuget.config") -Encoding UTF8
}

Invoke-Step "Check isolated source has no forbidden monorepo dependencies" {
    $sourceFiles = Get-ChildItem -LiteralPath $sampleRoot -Recurse -File |
        Where-Object {
            $_.FullName -notmatch "\\(bin|obj)\\"
        }

    $violations = foreach ($file in $sourceFiles) {
        $content = Get-Content -LiteralPath $file.FullName -Raw
        foreach ($pattern in $forbiddenPatterns) {
            if ($content.Contains($pattern, [System.StringComparison]::OrdinalIgnoreCase)) {
                "$($file.FullName): $pattern"
            }
        }
    }

    if ($violations) {
        $violations | ForEach-Object { Write-Error $_ }
        throw "Isolated Starter/Sample source contains forbidden monorepo dependency references."
    }
}

Invoke-Step "Restore isolated Starter/Sample" {
    dotnet restore $starterProject --no-cache --force-evaluate
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}

Invoke-Step "Build isolated Starter/Sample" {
    dotnet build $starterProject --configuration $Configuration --no-restore
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}

Invoke-Step "Publish isolated Starter/Sample" {
    dotnet publish $starterProject --configuration $Configuration --no-restore --output $publishRoot
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}

Invoke-Step "Docker build check" {
    $dockerfile = Join-Path $sampleRoot "Dockerfile"
    if (Test-Path $dockerfile) {
        docker build -f $dockerfile $sampleRoot
        if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    }
    else {
        Write-Host "No Starter Dockerfile present; Docker build is n/a."
    }
}

Write-Host "Storefront Starter isolation gate passed."

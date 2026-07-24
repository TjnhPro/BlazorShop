param(
    [string]$Configuration = "Release",
    [string]$StorefrontClientPackageVersion = "1.0.0-local",
    [string]$StorefrontRuntimePackageVersion = "1.0.0-local",
    [switch]$Describe
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$clientProject = Join-Path $repoRoot "BlazorShop.PresentationV2\BlazorShop.Storefront.Client\BlazorShop.Storefront.Client.csproj"
$runtimeProject = Join-Path $repoRoot "BlazorShop.PresentationV2\BlazorShop.Storefront.Runtime\BlazorShop.Storefront.Runtime.csproj"
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
    "..\BlazorShop.",
    "../BlazorShop."
)

if ($Describe) {
    Write-Host "Storefront Starter isolation gate"
    Write-Host "- Pack Storefront.Client to local feed"
    Write-Host "- Pack Storefront.Runtime to local feed"
    Write-Host "- Copy Starter source to obj/storefront-starter-isolation/Storefront.Sample"
    Write-Host "- Restore from local package feed"
    Write-Host "- Build isolated Starter/Sample copy"
    Write-Host "- Publish isolated Starter/Sample copy"
    Write-Host "- Fail on backend/V2/ProjectReference source paths"
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

Assert-UnderRepoObj $isolationRoot

Invoke-Step "Clean isolation directory" {
    if (Test-Path $isolationRoot) {
        Remove-Item -LiteralPath $isolationRoot -Recurse -Force
    }

    New-Item -ItemType Directory -Force -Path $feedRoot | Out-Null
    New-Item -ItemType Directory -Force -Path $sampleRoot | Out-Null
    New-Item -ItemType Directory -Force -Path $publishRoot | Out-Null
}

Invoke-Step "Pack Storefront.Client" {
    dotnet pack $clientProject --configuration $Configuration --no-restore --output $feedRoot "/p:PackageVersion=$StorefrontClientPackageVersion"
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}

Invoke-Step "Pack Storefront.Runtime" {
    dotnet pack $runtimeProject --configuration $Configuration --no-restore --output $feedRoot "/p:PackageVersion=$StorefrontRuntimePackageVersion"
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}

Invoke-Step "Copy Starter source into isolated sample directory" {
    Get-ChildItem -LiteralPath $starterSource -Force |
        Where-Object { $_.Name -notin @("bin", "obj") } |
        ForEach-Object {
            Copy-Item -LiteralPath $_.FullName -Destination $sampleRoot -Recurse -Force
        }
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
    dotnet restore $starterProject
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

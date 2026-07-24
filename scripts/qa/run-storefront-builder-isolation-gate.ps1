param(
    [string]$Name = "BlazorShop.Storefront.GeneratedProof",
    [string]$ProjectRoot = "",
    [string]$Configuration = "Debug",
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
$packageRoot = Join-Path $repoRoot "artifacts\storefront-builder-packages"

if ($Describe) {
    Write-Host "StorefrontBuilder isolation gate:"
    Write-Host "- restore generated storefront"
    Write-Host "- build generated storefront"
    Write-Host "- pack BlazorShop.Storefront.Client"
    Write-Host "- pack BlazorShop.Storefront.Runtime"
    Write-Host "- confirm package references, no Storefront.V2/backend/core/API references"
    exit 0
}

if (-not (Test-Path $projectFile)) {
    throw "[SFB-ISOLATION-000] Generated storefront project is missing: $projectFile"
}

New-Item -ItemType Directory -Force -Path $packageRoot | Out-Null
dotnet restore $projectFile
dotnet build $projectFile --configuration $Configuration --no-restore
dotnet pack (Join-Path $repoRoot "BlazorShop.PresentationV2\BlazorShop.Storefront.Client\BlazorShop.Storefront.Client.csproj") --configuration $Configuration --no-build --output $packageRoot
dotnet pack (Join-Path $repoRoot "BlazorShop.PresentationV2\BlazorShop.Storefront.Runtime\BlazorShop.Storefront.Runtime.csproj") --configuration $Configuration --no-build --output $packageRoot

$project = Get-Content -LiteralPath $projectFile -Raw
foreach ($package in @("BlazorShop.Storefront.Client", "BlazorShop.Storefront.Runtime")) {
    if (-not $project.Contains("PackageReference Include=`"$package`"", [System.StringComparison]::Ordinal)) {
        throw "[SFB-ISOLATION-001] Generated storefront must consume '$package' as a package reference."
    }
}

$forbidden = @("ProjectReference", "BlazorShop.Storefront.V2", "BlazorShop.Application", "BlazorShop.Domain", "BlazorShop.Infrastructure", "BlazorShop.CommerceNode.API", "BlazorShop.ControlPlane.API")
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
if (-not $metadata.Contains("StorefrontClientPackageVersion", [System.StringComparison]::Ordinal) -or -not $metadata.Contains("StorefrontRuntimePackageVersion", [System.StringComparison]::Ordinal)) {
    throw "[SFB-ISOLATION-003] Package compatibility metadata is missing."
}

Write-Host "StorefrontBuilder isolation gate passed for $Name."

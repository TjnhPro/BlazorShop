param(
    [Parameter(Mandatory = $true)]
    [string]$ProjectRoot,
    [Parameter(Mandatory = $true)]
    [string]$Name,
    [Parameter(Mandatory = $true)]
    [string]$StoreKey
)

$ErrorActionPreference = "Stop"
$toolRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$repoRoot = Resolve-Path (Join-Path $toolRoot "..\..")

& (Join-Path $toolRoot "scripts\validate\Test-StorefrontBuilderSchemas.ps1")
& (Join-Path $toolRoot "scripts\validate\Test-StorefrontBuilderGeneratedProject.ps1") -ProjectRoot $ProjectRoot -Name $Name -StoreKey $StoreKey
& (Join-Path $toolRoot "scripts\validate\Test-StorefrontBuilderAssets.ps1") -ProjectRoot $ProjectRoot
& (Join-Path $toolRoot "scripts\validate\Test-StorefrontBuilderCss.ps1") -ProjectRoot $ProjectRoot
& (Join-Path $toolRoot "scripts\validate\Test-StorefrontBuilderCompositionFiles.ps1") -ProjectRoot $ProjectRoot
& (Join-Path $toolRoot "scripts\validate\Test-StorefrontBuilderGuard.ps1") -ProjectRoot $ProjectRoot
& (Join-Path $toolRoot "scripts\validate\Test-StorefrontBuilderIdempotency.ps1") -ProjectRoot $ProjectRoot

$analysisRoot = Join-Path $ProjectRoot "docs\storefront-analysis"
foreach ($artifact in @("metadata.yaml", "asset-manifest.yaml", "generated-files.yaml")) {
    if (-not (Test-Path (Join-Path $analysisRoot $artifact))) {
        throw "[SFB-STATIC-001] Generated file manifest or analysis artifact is missing: $artifact"
    }
}

$routes = @{}
Get-ChildItem -LiteralPath $ProjectRoot -Recurse -File -Include *.razor |
    Where-Object { $_.FullName -notmatch "\\(bin|obj)\\" } |
    ForEach-Object {
        $content = Get-Content -LiteralPath $_.FullName -Raw
        foreach ($match in [regex]::Matches($content, "(?m)^@page\s+`"([^`"]+)`"")) {
            $route = $match.Groups[1].Value
            if ($routes.ContainsKey($route)) {
                throw "[SFB-STATIC-002] Duplicate route '$route' in '$($_.FullName)' and '$($routes[$route])'."
            }

            $routes[$route] = $_.FullName
        }
    }

$versions = Get-Content -LiteralPath (Join-Path $ProjectRoot "StorefrontPackageVersions.props") -Raw
$project = Get-Content -LiteralPath (Join-Path $ProjectRoot "$Name.csproj") -Raw
foreach ($package in @("BlazorShop.Storefront.Client", "BlazorShop.Storefront.Runtime")) {
    if (-not $project.Contains("PackageReference Include=`"$package`"", [System.StringComparison]::Ordinal)) {
        throw "[SFB-STATIC-003] Package version mismatch or missing package reference: $package"
    }
}

if (-not $versions.Contains("StorefrontClientPackageVersion", [System.StringComparison]::Ordinal) -or -not $versions.Contains("StorefrontRuntimePackageVersion", [System.StringComparison]::Ordinal)) {
    throw "[SFB-STATIC-004] Package compatibility metadata is missing."
}

Write-Host "StorefrontBuilder static validation gate passed for $ProjectRoot."

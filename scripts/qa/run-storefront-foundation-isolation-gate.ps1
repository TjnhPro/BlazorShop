param(
    [string] $Configuration = "Release",
    [string] $OutputRoot = "obj/storefront-foundation-isolation",
    [string] $StorefrontClientPackageVersion = "1.0.0-local",
    [switch] $RunSmoke,
    [switch] $Describe
)

$ErrorActionPreference = "Stop"

$repositoryRoot = Resolve-Path (Join-Path $PSScriptRoot "../..")
$outputRootPath = Join-Path $repositoryRoot $OutputRoot
$feedRoot = Join-Path $outputRootPath "nuget-feed"
$commerceNodePublish = Join-Path $outputRootPath "publish/commercenode-api"
$storefrontPublish = Join-Path $outputRootPath "publish/storefront-v2"

$clientProject = Join-Path $repositoryRoot "BlazorShop.PresentationV2/BlazorShop.Storefront.Client/BlazorShop.Storefront.Client.csproj"
$commerceNodeProject = Join-Path $repositoryRoot "BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/BlazorShop.CommerceNode.API.csproj"
$storefrontProject = Join-Path $repositoryRoot "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/BlazorShop.Storefront.V2.csproj"
$releaseSmokeScript = Join-Path $repositoryRoot "scripts/qa/run-v2-production-release-smoke.ps1"

if ($Describe) {
    Write-Host "Pack Storefront client: $clientProject -> $feedRoot"
    Write-Host "Publish Commerce Node API: $commerceNodeProject -> $commerceNodePublish"
    Write-Host "Publish Storefront V2: $storefrontProject -> $storefrontPublish"
    Write-Host "Optional smoke for already-running services: $releaseSmokeScript -SkipNginx"
    exit 0
}

if (Test-Path $outputRootPath) {
    Remove-Item -LiteralPath $outputRootPath -Recurse -Force
}

New-Item -ItemType Directory -Path $feedRoot -Force | Out-Null
New-Item -ItemType Directory -Path $commerceNodePublish -Force | Out-Null
New-Item -ItemType Directory -Path $storefrontPublish -Force | Out-Null

dotnet pack $clientProject `
    --configuration $Configuration `
    --no-restore `
    --output $feedRoot `
    /p:PackageVersion=$StorefrontClientPackageVersion

dotnet publish $commerceNodeProject `
    --configuration $Configuration `
    --no-restore `
    --output $commerceNodePublish `
    /p:UseAppHost=false

dotnet publish $storefrontProject `
    --configuration $Configuration `
    --no-restore `
    --output $storefrontPublish `
    /p:UseAppHost=false

if (-not (Test-Path (Join-Path $feedRoot "BlazorShop.Storefront.Client.$StorefrontClientPackageVersion.nupkg"))) {
    throw "Storefront client package was not created in $feedRoot."
}

if (-not (Test-Path (Join-Path $commerceNodePublish "BlazorShop.CommerceNode.API.dll"))) {
    throw "Commerce Node publish output is missing BlazorShop.CommerceNode.API.dll."
}

if (-not (Test-Path (Join-Path $storefrontPublish "BlazorShop.Storefront.V2.dll"))) {
    throw "Storefront V2 publish output is missing BlazorShop.Storefront.V2.dll."
}

Write-Host "PASS Storefront client package and isolated publish outputs were created under $outputRootPath"

if ($RunSmoke) {
    & $releaseSmokeScript -SkipNginx
}

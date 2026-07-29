param(
    [Parameter(Mandatory = $true)]
    [string]$Name,
    [Parameter(Mandatory = $true)]
    [string]$StoreKey,
    [string]$OutputRoot = "artifacts/storefront-builder/generated",
    [string]$CommerceNodeBaseUrl = "http://localhost:5180",
    [string]$PublicBaseUrl = "http://localhost:18600",
    [ValidateSet("generate", "full")]
    [string]$CommandMode = "generate",
    [switch]$Force
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..\..\..")
. (Join-Path $PSScriptRoot "StorefrontBuilderProjectSafety.ps1")

$projectName = Normalize-StorefrontProjectName -Name $Name
$normalizedStoreKey = Normalize-StorefrontStoreKey -StoreKey $StoreKey
$outputRootPath = Resolve-ApprovedStorefrontBuilderOutputRoot -RepoRoot $repoRoot -OutputRoot $OutputRoot
$projectRoot = Join-Path $outputRootPath $projectName
$generator = Join-Path $repoRoot "scripts\generate-storefront-sample.ps1"
$storefrontContractPath = "contracts/storefront/storefront.openapi.json"
$storefrontContractFullPath = Resolve-StorefrontBuilderRepoPath -RepoRoot $repoRoot -Path $storefrontContractPath
$starterContractPath = "BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/starter-generation.contract.yaml"
$starterContractFullPath = Resolve-StorefrontBuilderRepoPath -RepoRoot $repoRoot -Path $starterContractPath
$projectValidator = Join-Path $PSScriptRoot "..\validate\Test-StorefrontBuilderGeneratedProject.ps1"

if (-not (Test-Path -LiteralPath $storefrontContractFullPath)) {
    throw "[SFB-PROJECT-007] Canonical Storefront OpenAPI contract is missing: $storefrontContractPath"
}

if (-not (Test-Path -LiteralPath $starterContractFullPath)) {
    throw "[SFB-PROJECT-008] Starter generation contract is missing: $starterContractPath"
}

Assert-StorefrontBuilderPathUnderRoot -Path $projectRoot -Root $outputRootPath
if ((Test-Path -LiteralPath $projectRoot) -and -not $Force) {
    throw "[SFB-PROJECT-011] Output '$projectRoot' already exists. Re-run with -Force to replace it atomically."
}

$starterContract = Get-Content -LiteralPath $starterContractFullPath -Raw
$starterContractVersionMatch = [regex]::Match($starterContract, "(?m)^contractVersion:\s*(\S+)\s*$")
if (-not $starterContractVersionMatch.Success) {
    throw "[SFB-PROJECT-008] Starter generation contract must declare contractVersion."
}

$starterVersionMatch = [regex]::Match($starterContract, "(?m)^starterVersion:\s*(\S+)\s*$")
if (-not $starterVersionMatch.Success) {
    throw "[SFB-PROJECT-008] Starter generation contract must declare starterVersion."
}

$operationId = [System.Guid]::NewGuid().ToString("N")
$stagingOutputRoot = Join-Path (Join-Path $outputRootPath ".staging") $operationId
$stagedProjectRoot = Join-Path $stagingOutputRoot $projectName
$backupProjectRoot = Join-Path (Join-Path $outputRootPath ".replace-backup") "$projectName-$operationId"
$movedExistingTarget = $false

try {
    New-Item -ItemType Directory -Force -Path $stagingOutputRoot | Out-Null

    $arguments = @{
        Name = $projectName
        StoreKey = $normalizedStoreKey
        OutputRoot = $stagingOutputRoot
        CommerceNodeBaseUrl = $CommerceNodeBaseUrl
        PublicBaseUrl = $PublicBaseUrl
    }

    & $generator @arguments -Force

    $analysisRoot = Join-Path $stagedProjectRoot "docs\storefront-analysis"
    New-Item -ItemType Directory -Force -Path $analysisRoot | Out-Null
    $storefrontContractSha256 = (Get-FileHash -LiteralPath $storefrontContractFullPath -Algorithm SHA256).Hash.ToLowerInvariant()
    $versionPropsPath = Join-Path $stagedProjectRoot "StorefrontPackageVersions.props"
    if (-not (Test-Path -LiteralPath $versionPropsPath)) {
        throw "[SFB-PROJECT-009] Generated project is missing StorefrontPackageVersions.props."
    }

    [xml]$packageVersionDocument = Get-Content -LiteralPath $versionPropsPath -Raw
    $packageVersions = $packageVersionDocument.Project.PropertyGroup

    $metadata = @(
        "schemaVersion: 1.0.0",
        "artifactKind: generated-storefront-metadata",
        "generatorVersion: $script:StorefrontBuilderGeneratorVersion",
        "createdUtc: $([System.DateTime]::UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ"))",
        "commandMode: $CommandMode",
        "projectName: $projectName",
        "normalizedProjectName: $projectName",
        "storeKey: $normalizedStoreKey",
        "outputRoot: $([System.IO.Path]::GetRelativePath($repoRoot, $outputRootPath).Replace('\', '/'))",
        "storefrontContractPath: $storefrontContractPath",
        "storefrontContractSha256: $storefrontContractSha256",
        "sourceStarterPath: BlazorShop.PresentationV2/BlazorShop.Storefront.Starter",
        "sourceStarterVersion: $($starterVersionMatch.Groups[1].Value)",
        "starterContractPath: $starterContractPath",
        "starterContractVersion: $($starterContractVersionMatch.Groups[1].Value)",
        "generationMode: starter-copy-before-visual-generation",
        "protectedFiles:",
        "  - BlazorShop.Storefront.Presentation",
        "  - StorefrontPackageVersions.props",
        "featureManifest: Features\feature-manifest.json",
        "packageReferences:",
        "  - BlazorShop.Storefront.Presentation",
        "  - BlazorShop.Storefront.Components",
        "packageVersions:",
        "  BlazorShop.Storefront.Client: $($packageVersions.StorefrontClientPackageVersion)",
        "  BlazorShop.Storefront.Runtime: $($packageVersions.StorefrontRuntimePackageVersion)",
        "  BlazorShop.Storefront.Presentation: $($packageVersions.StorefrontPresentationPackageVersion)",
        "  BlazorShop.Storefront.Components: $($packageVersions.StorefrontComponentsPackageVersion)"
    ) -join [Environment]::NewLine

    Set-Content -LiteralPath (Join-Path $analysisRoot "metadata.yaml") -Value $metadata -Encoding UTF8

    & $projectValidator -ProjectRoot $stagedProjectRoot -Name $projectName -StoreKey $normalizedStoreKey

    if (Test-Path -LiteralPath $projectRoot) {
        New-Item -ItemType Directory -Force -Path (Split-Path -Parent $backupProjectRoot) | Out-Null
        Assert-StorefrontBuilderPathUnderRoot -Path $backupProjectRoot -Root $outputRootPath
        Move-Item -LiteralPath $projectRoot -Destination $backupProjectRoot
        $movedExistingTarget = $true
    }

    Move-Item -LiteralPath $stagedProjectRoot -Destination $projectRoot

    if ($movedExistingTarget) {
        Remove-StorefrontBuilderPath -Path $backupProjectRoot -ApprovedRoot $outputRootPath
    }
}
catch {
    if ($movedExistingTarget -and -not (Test-Path -LiteralPath $projectRoot) -and (Test-Path -LiteralPath $backupProjectRoot)) {
        Move-Item -LiteralPath $backupProjectRoot -Destination $projectRoot
    }

    throw
}
finally {
    Remove-StorefrontBuilderPath -Path $stagingOutputRoot -ApprovedRoot $outputRootPath
}

Write-Host "StorefrontBuilder generated $projectName for store '$normalizedStoreKey' at $projectRoot."

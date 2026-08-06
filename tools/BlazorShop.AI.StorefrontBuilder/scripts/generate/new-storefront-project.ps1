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
    [string]$HandoffRoot = "",
    [string]$HandoffSchemaRoot = "",
    [string]$SourceHead = "",
    [string]$PackageBuildIdentity = "",
    [string]$StorefrontClientPackageVersion = "",
    [string]$StorefrontRuntimePackageVersion = "",
    [string]$StorefrontPresentationPackageVersion = "",
    [string]$StorefrontComponentsPackageVersion = "",
    [string]$StorefrontBrowserPackageVersion = "",
    [string]$StorefrontClientPackageHash = "",
    [string]$StorefrontRuntimePackageHash = "",
    [string]$StorefrontPresentationPackageHash = "",
    [string]$StorefrontComponentsPackageHash = "",
    [string]$StorefrontBrowserPackageHash = "",
    [string]$PackageFeedPath = "",
    [switch]$Force
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..\..\..")
. (Join-Path $PSScriptRoot "StorefrontBuilderProjectSafety.ps1")

# Accept a friendly {Name} suffix and emit BlazorShop.Storefront.{Name}; Copy Starter through scripts\generate-storefront-sample.ps1 before StorefrontBuilder metadata is layered.
$projectName = Normalize-StorefrontProjectName -Name $Name
$normalizedStoreKey = Normalize-StorefrontStoreKey -StoreKey $StoreKey
$outputRootPath = Resolve-ApprovedStorefrontBuilderOutputRoot -RepoRoot $repoRoot -OutputRoot $OutputRoot
$projectRoot = Join-Path $outputRootPath $projectName
$generator = Join-Path $repoRoot "scripts\generate-storefront-sample.ps1"
$storefrontContractPath = "contracts/storefront/storefront.openapi.json"
$storefrontContractFullPath = Resolve-StorefrontBuilderRepoPath -RepoRoot $repoRoot -Path $storefrontContractPath
$starterContractPath = "BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/starter-generation.contract.yaml"
$starterContractFullPath = Resolve-StorefrontBuilderRepoPath -RepoRoot $repoRoot -Path $starterContractPath
$starterWasmContractPath = "BlazorShop.PresentationV2/BlazorShop.Storefront.Starter.WASM/BlazorShop.Storefront.Starter.WASM.csproj"
$starterWasmContractFullPath = Resolve-StorefrontBuilderRepoPath -RepoRoot $repoRoot -Path $starterWasmContractPath
$projectValidator = Join-Path $PSScriptRoot "..\validate\Test-StorefrontBuilderGeneratedProject.ps1"
$isHandoffGeneration = -not [string]::IsNullOrWhiteSpace($HandoffRoot)

if (-not (Test-Path -LiteralPath $storefrontContractFullPath)) {
    throw "[SFB-PROJECT-007] Canonical Storefront OpenAPI contract is missing: $storefrontContractPath"
}

if (-not (Test-Path -LiteralPath $starterContractFullPath)) {
    throw "[SFB-PROJECT-008] Starter generation contract is missing: $starterContractPath"
}

if (-not (Test-Path -LiteralPath $starterWasmContractFullPath)) {
    throw "[SFB-PROJECT-008] Starter.WASM generation contract is missing: $starterWasmContractPath"
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

function Get-PortableRelativePath {
    param(
        [Parameter(Mandatory = $true)][string]$BasePath,
        [Parameter(Mandatory = $true)][string]$TargetPath
    )

    $baseFullPath = [System.IO.Path]::GetFullPath($BasePath).TrimEnd('\', '/') + [System.IO.Path]::DirectorySeparatorChar
    $targetFullPath = [System.IO.Path]::GetFullPath($TargetPath)
    $baseUri = [System.Uri]::new($baseFullPath)
    $targetUri = [System.Uri]::new($targetFullPath)
    return [System.Uri]::UnescapeDataString($baseUri.MakeRelativeUri($targetUri).ToString()).Replace('\', '/')
}

function Remove-StorefrontBuilderDirectoryIfEmpty {
    param([Parameter(Mandatory = $true)][string]$Path)

    if (-not (Test-Path -LiteralPath $Path)) {
        return
    }

    Assert-StorefrontBuilderPathUnderRoot -Path $Path -Root $outputRootPath
    if (-not (Get-ChildItem -LiteralPath $Path -Force | Select-Object -First 1)) {
        Remove-Item -LiteralPath $Path -Force
    }
}

function Set-GeneratedNuGetConfig {
    param([Parameter(Mandatory = $true)][string]$ProjectRoot)

    $nugetConfigPath = Join-Path $ProjectRoot "nuget.config"
    if (-not (Test-Path -LiteralPath $nugetConfigPath)) {
        return
    }

    $packageFeed = Join-Path $repoRoot "artifacts\storefront-packages"
    $relativePackageFeed = Get-PortableRelativePath -BasePath $ProjectRoot -TargetPath $packageFeed
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
    Set-Content -LiteralPath $nugetConfigPath -Value $nugetConfig -Encoding UTF8
}

try {
    New-Item -ItemType Directory -Force -Path $stagingOutputRoot | Out-Null

    $arguments = @{
        Name = $projectName
        StoreKey = $normalizedStoreKey
        OutputRoot = $stagingOutputRoot
        CommerceNodeBaseUrl = $CommerceNodeBaseUrl
        PublicBaseUrl = $PublicBaseUrl
        StorefrontClientPackageVersion = $StorefrontClientPackageVersion
        StorefrontRuntimePackageVersion = $StorefrontRuntimePackageVersion
        StorefrontPresentationPackageVersion = $StorefrontPresentationPackageVersion
        StorefrontComponentsPackageVersion = $StorefrontComponentsPackageVersion
        StorefrontBrowserPackageVersion = $StorefrontBrowserPackageVersion
    }

    & $generator @arguments -Force

    $analysisRoot = Join-Path $stagedProjectRoot "docs\storefront-analysis"
    New-Item -ItemType Directory -Force -Path $analysisRoot | Out-Null
    $storefrontContractSha256 = (Get-FileHash -LiteralPath $storefrontContractFullPath -Algorithm SHA256).Hash.ToLowerInvariant()
    $starterContractSha256 = (Get-FileHash -LiteralPath $starterContractFullPath -Algorithm SHA256).Hash.ToLowerInvariant()
    $starterWasmContractSha256 = (Get-FileHash -LiteralPath $starterWasmContractFullPath -Algorithm SHA256).Hash.ToLowerInvariant()
    $handoffPlanJsonPath = Join-Path $analysisRoot "generation-plan.json"
    $handoffPlanYamlPath = Join-Path $analysisRoot "generation-plan.yaml"
    $handoffSummaryPath = Join-Path $analysisRoot "handoff-generation-summary.md"
    $handoffPlaceholderManifestPath = Join-Path $analysisRoot "handoff-placeholders.json"
    $handoffPlan = $null
    $handoffPlanSha256 = ""

    if ($isHandoffGeneration) {
        $planArguments = @(
            (Join-Path $PSScriptRoot "plan-generation-files.mjs"),
            "--project-name", $projectName,
            "--store-key", $normalizedStoreKey,
            "--output-root", $OutputRoot,
            "--repo-root", $repoRoot,
            "--handoff-root", $HandoffRoot,
            "--output", $handoffPlanYamlPath,
            "--json-output", $handoffPlanJsonPath,
            "--dry-run"
        )
        node @planArguments
        if ($LASTEXITCODE -ne 0) {
            throw "[SFB-HANDOFF-GEN-010] Handoff generation plan compiler failed with exit code $LASTEXITCODE."
        }

        & node (Join-Path $PSScriptRoot "apply-handoff-project-skeleton.mjs") `
            --project-root $stagedProjectRoot `
            --plan-json $handoffPlanJsonPath `
            --summary-output $handoffSummaryPath `
            --placeholder-manifest-output $handoffPlaceholderManifestPath
        if ($LASTEXITCODE -ne 0) {
            throw "[SFB-HANDOFF-GEN-011] Handoff project skeleton failed with exit code $LASTEXITCODE."
        }

        $handoffPlanSha256 = (Get-FileHash -LiteralPath $handoffPlanJsonPath -Algorithm SHA256).Hash.ToLowerInvariant()
        $handoffPlan = Get-Content -LiteralPath $handoffPlanJsonPath -Raw | ConvertFrom-Json
    }

    $versionPropsPath = Join-Path $stagedProjectRoot "StorefrontPackageVersions.props"
    if (-not (Test-Path -LiteralPath $versionPropsPath)) {
        throw "[SFB-PROJECT-009] Generated project is missing StorefrontPackageVersions.props."
    }

    [xml]$packageVersionDocument = Get-Content -LiteralPath $versionPropsPath -Raw
    $packageVersions = $packageVersionDocument.Project.PropertyGroup
    $metadataSourceHead = if ([string]::IsNullOrWhiteSpace($SourceHead)) { "unknown" } else { $SourceHead }
    $metadataPackageBuildIdentity = if ([string]::IsNullOrWhiteSpace($PackageBuildIdentity)) { "unknown" } else { $PackageBuildIdentity }
    $metadataPackageFeedPath = if ([string]::IsNullOrWhiteSpace($PackageFeedPath)) { "unknown" } else { Get-PortableRelativePath -BasePath $repoRoot -TargetPath $PackageFeedPath }
    $metadataTimestampUtc = [System.DateTime]::UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ")

    $metadataLines = @(
        "schemaVersion: 1.0.0",
        "artifactKind: generated-storefront-metadata",
        "generatorVersion: $script:StorefrontBuilderGeneratorVersion",
        "createdUtc: $metadataTimestampUtc",
        "updatedUtc: $metadataTimestampUtc",
        "commandMode: $CommandMode",
        "projectName: $projectName",
        "normalizedProjectName: $projectName",
        "storeKey: $normalizedStoreKey",
        "outputRoot: $(Get-PortableRelativePath -BasePath $repoRoot -TargetPath $outputRootPath)",
        "storefrontContractPath: $storefrontContractPath",
        "storefrontContractSha256: $storefrontContractSha256",
        "sourceStarterPath: BlazorShop.PresentationV2/BlazorShop.Storefront.Starter",
        "sourceStarterWasmPath: BlazorShop.PresentationV2/BlazorShop.Storefront.Starter.WASM",
        "sourceStarterVersion: $($starterVersionMatch.Groups[1].Value)",
        "sourceHead: $metadataSourceHead",
        "packageBuildIdentity: $metadataPackageBuildIdentity",
        "starterContractPath: $starterContractPath",
        "starterContractVersion: $($starterContractVersionMatch.Groups[1].Value)",
        "starterContractSha256: $starterContractSha256",
        "starterWasmContractPath: $starterWasmContractPath",
        "starterWasmContractSha256: $starterWasmContractSha256",
        "generationMode: $(if ($isHandoffGeneration) { "handoff-project-skeleton" } else { "starter-copy-before-visual-generation" })",
        "projects:",
        "  server:",
        "    name: $projectName",
        "    path: $projectName.csproj",
        "  wasm:",
        "    name: $projectName.WASM",
        "    path: $projectName.WASM/$projectName.WASM.csproj",
        "protectedFiles:",
        "  - BlazorShop.Storefront.Presentation",
        "  - StorefrontPackageVersions.props",
        "featureManifest: Features\feature-manifest.json",
        "packageReferences:",
        "  server:",
        "    - Microsoft.AspNetCore.Components.WebAssembly.Server",
        "    - BlazorShop.Storefront.Presentation",
        "    - BlazorShop.Storefront.Components",
        "    - BlazorShop.Storefront.Browser",
        "  wasm:",
        "    - Microsoft.AspNetCore.Components.WebAssembly",
        "    - BlazorShop.Storefront.Components",
        "    - BlazorShop.Storefront.Browser",
        "packageVersions:",
        "  BlazorShop.Storefront.Client: $($packageVersions.StorefrontClientPackageVersion)",
        "  BlazorShop.Storefront.Runtime: $($packageVersions.StorefrontRuntimePackageVersion)",
        "  BlazorShop.Storefront.Presentation: $($packageVersions.StorefrontPresentationPackageVersion)",
        "  BlazorShop.Storefront.Components: $($packageVersions.StorefrontComponentsPackageVersion)",
        "  BlazorShop.Storefront.Browser: $($packageVersions.StorefrontBrowserPackageVersion)",
        "packageProvenance:",
        "  feedPath: $metadataPackageFeedPath",
        "  packages:",
        "    - id: BlazorShop.Storefront.Client",
        "      version: $($packageVersions.StorefrontClientPackageVersion)",
        "      sha256: $(if ([string]::IsNullOrWhiteSpace($StorefrontClientPackageHash)) { "unknown" } else { $StorefrontClientPackageHash })",
        "    - id: BlazorShop.Storefront.Runtime",
        "      version: $($packageVersions.StorefrontRuntimePackageVersion)",
        "      sha256: $(if ([string]::IsNullOrWhiteSpace($StorefrontRuntimePackageHash)) { "unknown" } else { $StorefrontRuntimePackageHash })",
        "    - id: BlazorShop.Storefront.Presentation",
        "      version: $($packageVersions.StorefrontPresentationPackageVersion)",
        "      sha256: $(if ([string]::IsNullOrWhiteSpace($StorefrontPresentationPackageHash)) { "unknown" } else { $StorefrontPresentationPackageHash })",
        "    - id: BlazorShop.Storefront.Components",
        "      version: $($packageVersions.StorefrontComponentsPackageVersion)",
        "      sha256: $(if ([string]::IsNullOrWhiteSpace($StorefrontComponentsPackageHash)) { "unknown" } else { $StorefrontComponentsPackageHash })",
        "    - id: BlazorShop.Storefront.Browser",
        "      version: $($packageVersions.StorefrontBrowserPackageVersion)",
        "      sha256: $(if ([string]::IsNullOrWhiteSpace($StorefrontBrowserPackageHash)) { "unknown" } else { $StorefrontBrowserPackageHash })"
    )

    if ($isHandoffGeneration) {
        $metadataLines += @(
            "handoffGeneration:",
            "  planPath: docs/storefront-analysis/generation-plan.json",
            "  planSha256: $handoffPlanSha256",
            "  summaryPath: docs/storefront-analysis/handoff-generation-summary.md",
            "  placeholderManifestPath: docs/storefront-analysis/handoff-placeholders.json",
            "  sourceHandoffPackageHash: $($handoffPlan.sourceHandoffPackageHash)",
            "  sourceHandoffReadinessHash: $($handoffPlan.sourceHandoffReadinessHash)",
            "  sourceStarterContractHash: $($handoffPlan.sourceStarterContractHash)",
            "  warnings: $(@($handoffPlan.warnings).Count)",
            "  blockedItems: $(@($handoffPlan.blockedItems).Count)"
        )
    }

    $metadata = $metadataLines -join [Environment]::NewLine

    Set-Content -LiteralPath (Join-Path $analysisRoot "metadata.yaml") -Value $metadata -Encoding UTF8

    & node (Join-Path $PSScriptRoot "update-generated-files-manifest.mjs") --project-root $stagedProjectRoot
    if ($LASTEXITCODE -ne 0) {
        throw "[SFB-HANDOFF-GEN-012] Generated file manifest update failed with exit code $LASTEXITCODE."
    }

    if ($isHandoffGeneration) {
        & node (Join-Path $PSScriptRoot "write-agent-task-package.mjs") `
            --project-root $stagedProjectRoot `
            --handoff-root $HandoffRoot `
            --plan-json $handoffPlanJsonPath
        if ($LASTEXITCODE -ne 0) {
            throw "[SFB-AGENT-PACKAGE-010] Agent task package writer failed with exit code $LASTEXITCODE."
        }
    }

    & $projectValidator -ProjectRoot $stagedProjectRoot -Name $projectName -StoreKey $normalizedStoreKey

    if (Test-Path -LiteralPath $projectRoot) {
        New-Item -ItemType Directory -Force -Path (Split-Path -Parent $backupProjectRoot) | Out-Null
        Assert-StorefrontBuilderPathUnderRoot -Path $backupProjectRoot -Root $outputRootPath
        Move-Item -LiteralPath $projectRoot -Destination $backupProjectRoot
        $movedExistingTarget = $true
    }

    Move-Item -LiteralPath $stagedProjectRoot -Destination $projectRoot
    Set-GeneratedNuGetConfig -ProjectRoot $projectRoot

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
    Remove-StorefrontBuilderDirectoryIfEmpty -Path (Split-Path -Parent $stagingOutputRoot)
    Remove-StorefrontBuilderDirectoryIfEmpty -Path (Split-Path -Parent $backupProjectRoot)
}

Write-Host "StorefrontBuilder generated $projectName for store '$normalizedStoreKey' at $projectRoot."

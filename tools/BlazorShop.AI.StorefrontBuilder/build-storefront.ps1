param(
    [string]$Url = "https://reference.example",
    [string]$Name = "GeneratedProof",
    [string]$StoreKey = "sample",
    [string]$OutputRoot = "artifacts/storefront-builder/generated",
    [ValidateSet("analyze-only", "preflight-only", "plan-only", "generate", "update", "validate-only", "full")]
    [string]$Mode = "validate-only",
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
    [switch]$Force,
    [switch]$SkipVisualQa,
    [switch]$SkipCommerceRegression
)

$ErrorActionPreference = "Stop"
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
. (Join-Path $PSScriptRoot "scripts\generate\StorefrontBuilderProjectSafety.ps1")

$projectName = Normalize-StorefrontProjectName -Name $Name
$normalizedStoreKey = Normalize-StorefrontStoreKey -StoreKey $StoreKey
$resolvedOutputRoot = Resolve-ApprovedStorefrontBuilderOutputRoot -RepoRoot $repoRoot -OutputRoot $OutputRoot
$projectRoot = Join-Path $resolvedOutputRoot $projectName

Write-Host "StorefrontBuilder mode=$Mode url=$Url name=$projectName storeKey=$normalizedStoreKey output=$projectRoot"

if ($Mode -eq "preflight-only" -or -not [string]::IsNullOrWhiteSpace($HandoffRoot)) {
    & "$PSScriptRoot/scripts/generate/Test-HandoffPreflight.ps1" `
        -RepoRoot $repoRoot `
        -HandoffRoot $HandoffRoot `
        -SchemaRoot $HandoffSchemaRoot `
        -ProjectName $projectName `
        -StoreKey $normalizedStoreKey

    if ($Mode -eq "preflight-only") {
        return
    }
}

switch ($Mode) {
    "analyze-only" {
        node "$PSScriptRoot/scripts/generate/write-review-artifacts.mjs" --project-root $projectRoot --url $Url
    }
    "plan-only" {
        $planArgs = @(
            "$PSScriptRoot/scripts/generate/plan-generation-files.mjs",
            "--project-name", $projectName,
            "--store-key", $normalizedStoreKey,
            "--output-root", $OutputRoot,
            "--repo-root", $repoRoot,
            "--dry-run"
        )

        if (-not [string]::IsNullOrWhiteSpace($HandoffRoot)) {
            $planArgs += @("--handoff-root", $HandoffRoot)
        }

        node @planArgs
    }
    "generate" {
        $generationArgs = @{
            Name = $projectName
            StoreKey = $normalizedStoreKey
            OutputRoot = $OutputRoot
            CommandMode = "generate"
            Force = $Force
            SourceHead = $SourceHead
            PackageBuildIdentity = $PackageBuildIdentity
            StorefrontClientPackageVersion = $StorefrontClientPackageVersion
            StorefrontRuntimePackageVersion = $StorefrontRuntimePackageVersion
            StorefrontPresentationPackageVersion = $StorefrontPresentationPackageVersion
            StorefrontComponentsPackageVersion = $StorefrontComponentsPackageVersion
            StorefrontBrowserPackageVersion = $StorefrontBrowserPackageVersion
            StorefrontClientPackageHash = $StorefrontClientPackageHash
            StorefrontRuntimePackageHash = $StorefrontRuntimePackageHash
            StorefrontPresentationPackageHash = $StorefrontPresentationPackageHash
            StorefrontComponentsPackageHash = $StorefrontComponentsPackageHash
            StorefrontBrowserPackageHash = $StorefrontBrowserPackageHash
            PackageFeedPath = $PackageFeedPath
        }

        if (-not [string]::IsNullOrWhiteSpace($HandoffRoot)) {
            $generationArgs.HandoffRoot = $HandoffRoot
            $generationArgs.HandoffSchemaRoot = $HandoffSchemaRoot
        }

        & "$PSScriptRoot/scripts/generate/new-storefront-project.ps1" @generationArgs
        if ([string]::IsNullOrWhiteSpace($HandoffRoot)) {
            node "$PSScriptRoot/scripts/generate/write-review-artifacts.mjs" --project-root $projectRoot --url $Url
            node "$PSScriptRoot/scripts/generate/build-asset-manifest.mjs" --project-root $projectRoot
            node "$PSScriptRoot/scripts/generate/apply-visual-foundation.mjs" --project-root $projectRoot
            node "$PSScriptRoot/scripts/generate/apply-composition.mjs" --project-root $projectRoot
        }
        node "$PSScriptRoot/scripts/generate/update-generated-files-manifest.mjs" --project-root $projectRoot --intentional-changes "__all__"
    }
    "update" {
        & "$PSScriptRoot/regenerate-storefront.ps1" -ProjectRoot $projectRoot -Scope all
    }
    "validate-only" {
        & "$PSScriptRoot/validate-storefront.ps1" -ProjectRoot $projectRoot -Name $projectName -StoreKey $normalizedStoreKey
    }
    "full" {
        $generationArgs = @{
            Name = $projectName
            StoreKey = $normalizedStoreKey
            OutputRoot = $OutputRoot
            CommandMode = "full"
            Force = $Force
            SourceHead = $SourceHead
            PackageBuildIdentity = $PackageBuildIdentity
            StorefrontClientPackageVersion = $StorefrontClientPackageVersion
            StorefrontRuntimePackageVersion = $StorefrontRuntimePackageVersion
            StorefrontPresentationPackageVersion = $StorefrontPresentationPackageVersion
            StorefrontComponentsPackageVersion = $StorefrontComponentsPackageVersion
            StorefrontBrowserPackageVersion = $StorefrontBrowserPackageVersion
            StorefrontClientPackageHash = $StorefrontClientPackageHash
            StorefrontRuntimePackageHash = $StorefrontRuntimePackageHash
            StorefrontPresentationPackageHash = $StorefrontPresentationPackageHash
            StorefrontComponentsPackageHash = $StorefrontComponentsPackageHash
            StorefrontBrowserPackageHash = $StorefrontBrowserPackageHash
            PackageFeedPath = $PackageFeedPath
        }

        if (-not [string]::IsNullOrWhiteSpace($HandoffRoot)) {
            $generationArgs.HandoffRoot = $HandoffRoot
            $generationArgs.HandoffSchemaRoot = $HandoffSchemaRoot
        }

        & "$PSScriptRoot/scripts/generate/new-storefront-project.ps1" @generationArgs
        if ([string]::IsNullOrWhiteSpace($HandoffRoot)) {
            node "$PSScriptRoot/scripts/generate/write-review-artifacts.mjs" --project-root $projectRoot --url $Url
            node "$PSScriptRoot/scripts/generate/build-asset-manifest.mjs" --project-root $projectRoot
            node "$PSScriptRoot/scripts/generate/apply-visual-foundation.mjs" --project-root $projectRoot
            node "$PSScriptRoot/scripts/generate/apply-composition.mjs" --project-root $projectRoot
        }
        node "$PSScriptRoot/scripts/generate/update-generated-files-manifest.mjs" --project-root $projectRoot --intentional-changes "__all__"
        if ([string]::IsNullOrWhiteSpace($HandoffRoot)) {
            & "$PSScriptRoot/validate-storefront.ps1" -ProjectRoot $projectRoot -Name $projectName -StoreKey $normalizedStoreKey
        } else {
            & "$PSScriptRoot/scripts/validate/Test-StorefrontBuilderGeneratedProject.ps1" -ProjectRoot $projectRoot -Name $projectName -StoreKey $normalizedStoreKey
        }
        if (-not $SkipVisualQa) { Write-Host "Visual QA runner: scripts/qa/run-visual-qa.mjs" }
        if (-not $SkipCommerceRegression) { Write-Host "Commerce regression runner: scripts/qa/run-commerce-regression.mjs" }
    }
}

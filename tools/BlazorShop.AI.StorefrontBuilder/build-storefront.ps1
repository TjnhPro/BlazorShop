param(
    [string]$Url = "https://reference.example",
    [string]$Name = "GeneratedProof",
    [string]$StoreKey = "sample",
    [string]$OutputRoot = "artifacts/storefront-builder/generated",
    [ValidateSet("analyze-only", "plan-only", "generate", "update", "validate-only", "full")]
    [string]$Mode = "validate-only",
    [switch]$Force,
    [switch]$SkipVisualQa,
    [switch]$SkipCommerceRegression
)

$ErrorActionPreference = "Stop"
$projectName = if ($Name.StartsWith("BlazorShop.Storefront.", [System.StringComparison]::Ordinal)) { $Name } else { "BlazorShop.Storefront.$Name" }
$projectRoot = Join-Path $OutputRoot $projectName

Write-Host "StorefrontBuilder mode=$Mode url=$Url name=$projectName storeKey=$StoreKey output=$projectRoot"

switch ($Mode) {
    "analyze-only" {
        node "$PSScriptRoot/scripts/generate/write-review-artifacts.mjs" --project-root $projectRoot --url $Url
    }
    "plan-only" {
        node "$PSScriptRoot/scripts/generate/plan-generation-files.mjs" --project-name $projectName --output-root $OutputRoot --dry-run
    }
    "generate" {
        & "$PSScriptRoot/scripts/generate/new-storefront-project.ps1" -Name $projectName -StoreKey $StoreKey -OutputRoot $OutputRoot -Force:$Force
        node "$PSScriptRoot/scripts/generate/write-review-artifacts.mjs" --project-root $projectRoot --url $Url
        node "$PSScriptRoot/scripts/generate/build-asset-manifest.mjs" --project-root $projectRoot
        node "$PSScriptRoot/scripts/generate/apply-visual-foundation.mjs" --project-root $projectRoot
        node "$PSScriptRoot/scripts/generate/apply-composition.mjs" --project-root $projectRoot
        node "$PSScriptRoot/scripts/generate/update-generated-files-manifest.mjs" --project-root $projectRoot
    }
    "update" {
        & "$PSScriptRoot/regenerate-storefront.ps1" -ProjectRoot $projectRoot -Scope all
    }
    "validate-only" {
        & "$PSScriptRoot/validate-storefront.ps1" -ProjectRoot $projectRoot -Name $projectName -StoreKey $StoreKey
    }
    "full" {
        & "$PSScriptRoot/scripts/generate/new-storefront-project.ps1" -Name $projectName -StoreKey $StoreKey -OutputRoot $OutputRoot -Force:$Force
        node "$PSScriptRoot/scripts/generate/write-review-artifacts.mjs" --project-root $projectRoot --url $Url
        node "$PSScriptRoot/scripts/generate/build-asset-manifest.mjs" --project-root $projectRoot
        node "$PSScriptRoot/scripts/generate/apply-visual-foundation.mjs" --project-root $projectRoot
        node "$PSScriptRoot/scripts/generate/apply-composition.mjs" --project-root $projectRoot
        node "$PSScriptRoot/scripts/generate/update-generated-files-manifest.mjs" --project-root $projectRoot
        & "$PSScriptRoot/validate-storefront.ps1" -ProjectRoot $projectRoot -Name $projectName -StoreKey $StoreKey
        if (-not $SkipVisualQa) { Write-Host "Visual QA runner: scripts/qa/run-visual-qa.mjs" }
        if (-not $SkipCommerceRegression) { Write-Host "Commerce regression runner: scripts/qa/run-commerce-regression.mjs" }
    }
}

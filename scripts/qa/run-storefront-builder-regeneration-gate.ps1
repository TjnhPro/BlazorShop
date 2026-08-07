param(
    [switch]$Describe
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$regenerationSafetyTests = Join-Path $repoRoot "tools\BlazorShop.AI.StorefrontBuilder\tests\generation\Test-StorefrontBuilderRegenerationSafety.ps1"

if ($Describe) {
    Write-Host "StorefrontBuilder regeneration ownership gate"
    Write-Host "- Runs under obj/storefront-builder/generated so generated artifacts stay disposable."
    Write-Host "- Generates a fresh starter-first workspace with a solution plus sibling server and WASM projects."
    Write-Host "- Verifies no-op regeneration is deterministic using the workspace root."
    Write-Host "- Verifies css/page/component scopes only touch workspace-relative declared generated files plus manifest/report files."
    Write-Host "- Verifies manual generated-file edits become conflicts."
    Write-Host "- Verifies user-owned custom files are preserved."
    Write-Host "- Verifies protected file modification fails idempotency validation."
    Write-Host "- Verifies obsolete generated files are reported and not deleted silently."
    Write-Host "- Does not require live Commerce Node data."
    exit 0
}

if (-not (Test-Path -LiteralPath $regenerationSafetyTests)) {
    throw "StorefrontBuilder regeneration safety test script was not found: $regenerationSafetyTests"
}

& $regenerationSafetyTests
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

Write-Host "PASS StorefrontBuilder regeneration ownership gate completed without live Commerce Node data."

param(
    [switch]$Describe
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$workspaceGuardrailTests = Join-Path $repoRoot "tools\BlazorShop.AI.StorefrontBuilder\tests\generation\Test-StorefrontBuilderWorkspaceGuardrails.ps1"
$regenerationSafetyTests = Join-Path $repoRoot "tools\BlazorShop.AI.StorefrontBuilder\tests\generation\Test-StorefrontBuilderRegenerationSafety.ps1"

if ($Describe) {
    Write-Host "StorefrontBuilder regeneration ownership gate"
    Write-Host "- Runs under obj/storefront-builder/generated so generated artifacts stay disposable."
    Write-Host "- Generates a fresh starter-first workspace with a solution plus sibling server and WASM projects."
    Write-Host "- Runs starter-first workspace guardrails and active source scans before regeneration safety tests."
    Write-Host "- Verifies no-op regeneration is deterministic using the workspace root."
    Write-Host "- Verifies css/page/component scopes only touch workspace-relative declared generated files plus manifest/report files."
    Write-Host "- Verifies manual generated-file edits become conflicts."
    Write-Host "- Verifies user-owned custom files are preserved."
    Write-Host "- Verifies protected file modification fails idempotency validation."
    Write-Host "- Verifies obsolete generated files are reported and not deleted silently."
    Write-Host "- Does not require live Commerce Node data."
    exit 0
}

if (-not (Test-Path -LiteralPath $workspaceGuardrailTests)) {
    throw "StorefrontBuilder workspace guardrail test script was not found: $workspaceGuardrailTests"
}

if (-not (Test-Path -LiteralPath $regenerationSafetyTests)) {
    throw "StorefrontBuilder regeneration safety test script was not found: $regenerationSafetyTests"
}

& $workspaceGuardrailTests
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

& $regenerationSafetyTests
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

Write-Host "PASS StorefrontBuilder regeneration ownership gate completed without live Commerce Node data."

param(
    [switch] $SkipCanonicalContractDiff
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "../..")
$generatorScript = Join-Path $repoRoot "scripts/generate-storefront-client.ps1"
$generatedClientPath = "BlazorShop.PresentationV2/BlazorShop.Storefront.Client/Generated"
$canonicalContractPath = "contracts/storefront/storefront.openapi.json"

if (-not (Test-Path -LiteralPath $generatorScript)) {
    throw "Storefront client generator script was not found: $generatorScript"
}

Push-Location $repoRoot
try {
    & $generatorScript
    if ($LASTEXITCODE -ne 0) {
        exit $LASTEXITCODE
    }

    $diffTargets = @($generatedClientPath)
    if (-not $SkipCanonicalContractDiff) {
        $diffTargets += $canonicalContractPath
    }

    foreach ($target in $diffTargets) {
        git diff --exit-code -- $target
        if ($LASTEXITCODE -ne 0) {
            throw "Storefront client regeneration drift detected in '$target'. Review the diff and commit regenerated source when the contract change is intentional."
        }
    }

    Write-Host "PASS Storefront client regeneration gate completed without drift."
}
finally {
    Pop-Location
}

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..\..\..")
$toolRoot = Join-Path $repoRoot "tools\BlazorShop.AI.StorefrontBuilder"
. (Join-Path $toolRoot "scripts\generate\StorefrontBuilderProjectSafety.ps1")

function Assert-Throws {
    param(
        [Parameter(Mandatory = $true)][scriptblock]$Action,
        [Parameter(Mandatory = $true)][string]$ExpectedCode
    )

    try {
        & $Action
    }
    catch {
        if (-not $_.Exception.Message.Contains($ExpectedCode, [System.StringComparison]::Ordinal)) {
            throw "Expected '$ExpectedCode' but saw: $($_.Exception.Message)"
        }

        return
    }

    throw "Expected '$ExpectedCode' failure."
}

function Assert-Condition {
    param(
        [Parameter(Mandatory = $true)][bool]$Condition,
        [Parameter(Mandatory = $true)][string]$Message
    )

    if (-not $Condition) {
        throw $Message
    }
}

$testOutputRoot = Join-Path $repoRoot "obj\storefront-builder\generated\create-hardening-tests"
if (Test-Path -LiteralPath $testOutputRoot) {
    Remove-Item -LiteralPath $testOutputRoot -Recurse -Force
}

New-Item -ItemType Directory -Force -Path $testOutputRoot | Out-Null

Assert-Condition `
    -Condition ((Normalize-StorefrontProjectName -Name "Demo") -eq "BlazorShop.Storefront.Demo") `
    -Message "Friendly suffix should normalize to full storefront project name."

Assert-Condition `
    -Condition ((Normalize-StorefrontProjectName -Name "BlazorShop.Storefront.Demo") -eq "BlazorShop.Storefront.Demo") `
    -Message "Full storefront project name should remain stable."

Assert-Throws -ExpectedCode "SFB-PROJECT-001" -Action { Normalize-StorefrontProjectName -Name "demo" | Out-Null }
Assert-Throws -ExpectedCode "SFB-PROJECT-001" -Action { Normalize-StorefrontProjectName -Name "..\Demo" | Out-Null }
Assert-Throws -ExpectedCode "SFB-PROJECT-001" -Action { Normalize-StorefrontProjectName -Name "BlazorShop.Storefront." | Out-Null }
Assert-Throws -ExpectedCode "SFB-PROJECT-001" -Action { Normalize-StorefrontProjectName -Name "Starter" | Out-Null }
Assert-Throws -ExpectedCode "SFB-PROJECT-010" -Action { Normalize-StorefrontStoreKey -StoreKey "Sample" | Out-Null }
Assert-Throws -ExpectedCode "SFB-PROJECT-010" -Action { Normalize-StorefrontStoreKey -StoreKey "../sample" | Out-Null }
Assert-Throws -ExpectedCode "SFB-PROJECT-002" -Action {
    Resolve-ApprovedStorefrontBuilderOutputRoot -RepoRoot $repoRoot -OutputRoot (Join-Path $repoRoot "artifacts\outside") | Out-Null
}

$existingOutputRoot = Join-Path $testOutputRoot "existing"
$existingProjectRoot = Join-Path $existingOutputRoot "BlazorShop.Storefront.DemoExisting"
New-Item -ItemType Directory -Force -Path $existingProjectRoot | Out-Null
$sentinel = Join-Path $existingProjectRoot "sentinel.txt"
Set-Content -LiteralPath $sentinel -Value "unchanged" -Encoding UTF8

Assert-Throws -ExpectedCode "SFB-PROJECT-011" -Action {
    & (Join-Path $toolRoot "scripts\generate\new-storefront-project.ps1") `
        -Name DemoExisting `
        -StoreKey sample `
        -OutputRoot $existingOutputRoot
}

Assert-Condition `
    -Condition ((Get-Content -LiteralPath $sentinel -Raw).Trim() -eq "unchanged") `
    -Message "Existing target changed when -Force was not set."

$planOutputRoot = Join-Path $testOutputRoot "plan-only"
& (Join-Path $toolRoot "build-storefront.ps1") `
    -Url "https://example.test" `
    -Name DemoPlan `
    -StoreKey sample `
    -OutputRoot $planOutputRoot `
    -Mode plan-only

Assert-Condition `
    -Condition (-not (Test-Path -LiteralPath (Join-Path $planOutputRoot "BlazorShop.Storefront.DemoPlan"))) `
    -Message "plan-only created generated project files."

Write-Host "StorefrontBuilder create hardening tests passed."

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..\..\..")
$toolRoot = Join-Path $repoRoot "tools\BlazorShop.AI.StorefrontBuilder"
$outputRoot = Join-Path $repoRoot "obj\storefront-builder\generated\regeneration-safety-tests"
$projectName = "BlazorShop.Storefront.RegenSafety"
$projectRoot = Join-Path $outputRoot $projectName

function Assert-Condition {
    param(
        [Parameter(Mandatory = $true)][bool]$Condition,
        [Parameter(Mandatory = $true)][string]$Message
    )

    if (-not $Condition) {
        throw $Message
    }
}

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

function Get-TreeHashes {
    param([Parameter(Mandatory = $true)][string]$Root)

    $hashes = @{}
    Get-ChildItem -LiteralPath $Root -Recurse -File |
        Where-Object { $_.FullName -notmatch "\\(bin|obj|\.regeneration-staging|\.regeneration-backup)\\" } |
        ForEach-Object {
            $relative = [System.IO.Path]::GetRelativePath($Root, $_.FullName).Replace('\', '/')
            $content = (Get-Content -LiteralPath $_.FullName -Raw).Replace("`r`n", "`n").Replace("`r", "`n")
            $bytes = [System.Text.Encoding]::UTF8.GetBytes($content)
            $hash = [System.Security.Cryptography.SHA256]::HashData($bytes)
            $hashes[$relative] = [System.BitConverter]::ToString($hash).Replace("-", "").ToLowerInvariant()
        }

    return $hashes
}

function Compare-Hashes {
    param($Before, $After)

    $paths = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
    foreach ($key in $Before.Keys) { [void]$paths.Add($key) }
    foreach ($key in $After.Keys) { [void]$paths.Add($key) }

    return @($paths | Where-Object {
        -not $Before.ContainsKey($_) -or -not $After.ContainsKey($_) -or $Before[$_] -ne $After[$_]
    } | Sort-Object)
}

function Set-TextFileContent {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$Content
    )

    New-Item -ItemType Directory -Force -Path (Split-Path -Parent $Path) | Out-Null
    [System.IO.File]::WriteAllText($Path, $Content, [System.Text.UTF8Encoding]::new($false))
}

function Invoke-WithTemporaryTextEdit {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][scriptblock]$Transform,
        [Parameter(Mandatory = $true)][scriptblock]$Action
    )

    $exists = Test-Path -LiteralPath $Path
    $original = if ($exists) { Get-Content -LiteralPath $Path -Raw } else { $null }
    try {
        $current = if ($exists) { $original } else { "" }
        $updated = & $Transform $current
        if ($null -eq $updated) {
            throw "Transform for $Path returned null."
        }

        Set-TextFileContent -Path $Path -Content $updated
        & $Action
    }
    finally {
        if ($exists) {
            Set-TextFileContent -Path $Path -Content $original
        }
        elseif (Test-Path -LiteralPath $Path) {
            Remove-Item -LiteralPath $Path -Force
        }
    }
}

function Invoke-StorefrontRegeneration {
    param(
        [Parameter(Mandatory = $true)][string]$ProjectRoot,
        [Parameter(Mandatory = $true)][string[]]$Arguments,
        [switch]$PreserveCandidateArtifacts
    )

    $previousKeep = $env:SFB_KEEP_REGENERATION_CANDIDATE_ARTIFACTS
    if ($PreserveCandidateArtifacts) {
        $env:SFB_KEEP_REGENERATION_CANDIDATE_ARTIFACTS = "1"
    }

    try {
        $commandArguments = @("-ProjectRoot", $ProjectRoot) + $Arguments
        $output = @(& (Join-Path $toolRoot "regenerate-storefront.ps1") @commandArguments 2>&1)
        if ($LASTEXITCODE -ne 0) {
            throw ($output -join [System.Environment]::NewLine)
        }

        $candidateRoot = $null
        foreach ($line in $output) {
            if ($line -match "output=(.+)$") {
                $candidateRoot = $Matches[1]
                break
            }
        }

        return [pscustomobject]@{
            Output = $output
            CandidateRoot = $candidateRoot
        }
    }
    finally {
        if ($null -ne $previousKeep) {
            $env:SFB_KEEP_REGENERATION_CANDIDATE_ARTIFACTS = $previousKeep
        }
        else {
            Remove-Item Env:SFB_KEEP_REGENERATION_CANDIDATE_ARTIFACTS -ErrorAction SilentlyContinue
        }
    }
}

function New-TestProject {
    if (Test-Path -LiteralPath $outputRoot) {
        Remove-Item -LiteralPath $outputRoot -Recurse -Force
    }

    & (Join-Path $toolRoot "build-storefront.ps1") `
        -Url "https://example.test" `
        -Name RegenSafety `
        -StoreKey sample `
        -OutputRoot $outputRoot `
        -Mode generate `
        -Force
}

New-TestProject

$starterHomePath = Join-Path $repoRoot "BlazorShop.PresentationV2\BlazorShop.Storefront.Starter\Pages\Ssr\Home\HomePage.razor"
$starterProductPath = Join-Path $repoRoot "BlazorShop.PresentationV2\BlazorShop.Storefront.Starter\Components\Catalog\ProductSummaryCard.razor"
$starterLayoutPath = Join-Path $repoRoot "BlazorShop.PresentationV2\BlazorShop.Storefront.Starter\Components\Layout\MainLayout.razor"
$starterPackagePropsPath = Join-Path $repoRoot "BlazorShop.PresentationV2\BlazorShop.Storefront.Starter\StorefrontPackageVersions.props"

$starterHomeOriginal = Get-Content -LiteralPath $starterHomePath -Raw
try {
    Set-TextFileContent -Path $starterHomePath -Content ($starterHomeOriginal.Replace("Featured products", "Featured products updated"))
    $beforeHomePage = Get-Content -LiteralPath (Join-Path $projectRoot "Pages\Ssr\Home\HomePage.razor") -Raw
    $beforeProductPage = Get-Content -LiteralPath (Join-Path $projectRoot "Pages\Hybrid\Catalog\ProductPage.razor") -Raw
    & (Join-Path $toolRoot "regenerate-storefront.ps1") -ProjectRoot $projectRoot -Scope page -Target HomePage
    $afterHomePage = Get-Content -LiteralPath (Join-Path $projectRoot "Pages\Ssr\Home\HomePage.razor") -Raw
    $afterProductPage = Get-Content -LiteralPath (Join-Path $projectRoot "Pages\Hybrid\Catalog\ProductPage.razor") -Raw
    Assert-Condition -Condition ($beforeHomePage -ne $afterHomePage) -Message "HomePage was not updated by page-scope regeneration."
    Assert-Condition -Condition ($beforeProductPage -eq $afterProductPage) -Message "Page-scope regeneration touched ProductPage."
}
finally {
    Set-TextFileContent -Path $starterHomePath -Content $starterHomeOriginal
}

$starterProductOriginal = Get-Content -LiteralPath $starterProductPath -Raw
try {
    Set-TextFileContent -Path $starterProductPath -Content ($starterProductOriginal.Replace(">View</a>", ">View details</a>"))
    $beforeComponentPage = Get-Content -LiteralPath (Join-Path $projectRoot "Components\Catalog\ProductSummaryCard.razor") -Raw
    $beforeComponentProductPage = Get-Content -LiteralPath (Join-Path $projectRoot "Pages\Hybrid\Catalog\ProductPage.razor") -Raw
    & (Join-Path $toolRoot "regenerate-storefront.ps1") -ProjectRoot $projectRoot -Scope component -Target ProductSummaryCard
    $afterComponentPage = Get-Content -LiteralPath (Join-Path $projectRoot "Components\Catalog\ProductSummaryCard.razor") -Raw
    $afterComponentProductPage = Get-Content -LiteralPath (Join-Path $projectRoot "Pages\Hybrid\Catalog\ProductPage.razor") -Raw
    Assert-Condition -Condition ($beforeComponentPage -ne $afterComponentPage) -Message "ProductSummaryCard was not updated by component-scope regeneration."
    Assert-Condition -Condition ($beforeComponentProductPage -eq $afterComponentProductPage) -Message "Component-scope regeneration touched ProductPage."
}
finally {
    Set-TextFileContent -Path $starterProductPath -Content $starterProductOriginal
}

$beforeNoop = Get-TreeHashes -Root $projectRoot
& (Join-Path $toolRoot "regenerate-storefront.ps1") -ProjectRoot $projectRoot -Scope all
$afterNoop = Get-TreeHashes -Root $projectRoot
Assert-Condition -Condition ((Compare-Hashes -Before $beforeNoop -After $afterNoop).Count -eq 0) -Message "No-op regeneration produced file diffs."

Remove-Item -LiteralPath (Join-Path $projectRoot "wwwroot\css\storefront-builder.generated.css") -Force
$beforeCss = Get-TreeHashes -Root $projectRoot
& (Join-Path $toolRoot "regenerate-storefront.ps1") -ProjectRoot $projectRoot -Scope css
$afterCss = Get-TreeHashes -Root $projectRoot
$cssChanged = Compare-Hashes -Before $beforeCss -After $afterCss
Assert-Condition -Condition (((@($cssChanged | Where-Object { $_ -notin @("wwwroot/css/storefront-builder.generated.css", "docs/storefront-analysis/generated-files.yaml", "docs/storefront-analysis/regeneration-report.md") })).Count) -eq 0) -Message "CSS scope touched unrelated files."

New-TestProject
$starterHomeOriginal = Get-Content -LiteralPath $starterHomePath -Raw
$starterLayoutOriginal = Get-Content -LiteralPath $starterLayoutPath -Raw
$starterPackagePropsOriginal = Get-Content -LiteralPath $starterPackagePropsPath -Raw
$projectLayoutPath = Join-Path $projectRoot "Components\Layout\MainLayout.razor"
$projectReadmePath = Join-Path $projectRoot "README.md"
$starterCreatedPath = Join-Path $repoRoot "BlazorShop.PresentationV2\BlazorShop.Storefront.Starter\Pages\Ssr\Home\WhatIfCreated.razor"
$starterCreatedContent = @"
@namespace BlazorShop.Storefront.Starter.Pages.Catalog

<h1>WhatIf created page</h1>
<p>Generated for regeneration planning tests.</p>
"@
$baselineHashes = Get-TreeHashes -Root $projectRoot
try {
    Set-TextFileContent -Path $starterHomePath -Content ($starterHomeOriginal.Replace("Featured products", "Featured products whatif"))
    Set-TextFileContent -Path $starterLayoutPath -Content ($starterLayoutOriginal.Replace("Deals", "Specials"))
    Set-TextFileContent -Path $projectLayoutPath -Content ((Get-Content -LiteralPath $projectLayoutPath -Raw) + "`n<!-- manual edit -->")
    Set-TextFileContent -Path $projectReadmePath -Content ((Get-Content -LiteralPath $projectReadmePath -Raw) + "`nHuman-owned note.")
    Set-TextFileContent -Path $starterCreatedPath -Content $starterCreatedContent
    & (Join-Path $toolRoot "scripts\generate\update-generated-files-manifest.mjs") --project-root $projectRoot

    $baselineHashes = Get-TreeHashes -Root $projectRoot
    $env:SFB_KEEP_REGENERATION_CANDIDATE_ARTIFACTS = "1"
    $env:SFB_DROP_CANDIDATE_FILE_PATHS = "Pages/Hybrid/Catalog/SearchPage.razor"
    & (Join-Path $toolRoot "regenerate-storefront.ps1") -ProjectRoot $projectRoot -Scope all -WhatIf
    $whatIfCandidateRoot = (Get-ChildItem -LiteralPath (Join-Path $outputRoot ".regeneration-candidate") -Directory |
        Sort-Object LastWriteTime -Descending |
        Select-Object -First 1).FullName
    $whatIfCandidateRoot = Join-Path $whatIfCandidateRoot $projectName
    $reportPath = Join-Path $whatIfCandidateRoot "docs\storefront-analysis\regeneration-report.md"
    $report = Get-Content -LiteralPath $reportPath -Raw
    Assert-Condition -Condition $report.Contains("Pages/Ssr/Home/WhatIfCreated.razor: create", [System.StringComparison]::Ordinal) -Message "WhatIf report did not include created candidate."
    Assert-Condition -Condition $report.Contains("Pages/Ssr/Home/HomePage.razor: update", [System.StringComparison]::Ordinal) -Message "WhatIf report did not include updated HomePage."
    Assert-Condition -Condition $report.Contains("Components/Layout/MainLayout.razor: conflict manual edit", [System.StringComparison]::Ordinal) -Message "WhatIf report did not include manual-edit conflict."
    Assert-Condition -Condition $report.Contains("Pages/Hybrid/Catalog/SearchPage.razor", [System.StringComparison]::Ordinal) -Message "WhatIf report did not include obsolete candidate path."
    Assert-Condition -Condition $report.Contains("obsolete candidate", [System.StringComparison]::Ordinal) -Message "WhatIf report did not include obsolete candidate action."
    Assert-Condition -Condition $report.Contains("README.md: skip user-owned", [System.StringComparison]::Ordinal) -Message "WhatIf report did not preserve user-owned README."
    Assert-Condition -Condition ((Compare-Hashes -Before $baselineHashes -After (Get-TreeHashes -Root $projectRoot)).Count -eq 0) -Message "WhatIf modified the target tree."
}
finally {
    if (Test-Path -LiteralPath $starterCreatedPath) {
        Remove-Item -LiteralPath $starterCreatedPath -Force
    }

    Set-TextFileContent -Path $starterHomePath -Content $starterHomeOriginal
    Set-TextFileContent -Path $starterLayoutPath -Content $starterLayoutOriginal
    Set-TextFileContent -Path $starterPackagePropsPath -Content $starterPackagePropsOriginal
    Set-TextFileContent -Path $projectLayoutPath -Content (Get-Content -LiteralPath $projectLayoutPath -Raw).Replace("`n<!-- manual edit -->", "")
    Set-TextFileContent -Path $projectReadmePath -Content (Get-Content -LiteralPath $projectReadmePath -Raw).Replace("`nHuman-owned note.", "")
    if ($whatIfCandidateRoot -and (Test-Path -LiteralPath $whatIfCandidateRoot)) {
        Remove-Item -LiteralPath $whatIfCandidateRoot -Recurse -Force
    }
    Remove-Item Env:SFB_KEEP_REGENERATION_CANDIDATE_ARTIFACTS -ErrorAction SilentlyContinue
    Remove-Item Env:SFB_DROP_CANDIDATE_FILE_PATHS -ErrorAction SilentlyContinue
}

New-TestProject
$missingHomePath = Join-Path $projectRoot "Pages\Ssr\Home\HomePage.razor"
$missingHomeOriginal = Get-Content -LiteralPath $missingHomePath -Raw
Remove-Item -LiteralPath $missingHomePath -Force
& (Join-Path $toolRoot "regenerate-storefront.ps1") -ProjectRoot $projectRoot -Scope page -Target HomePage
Assert-Condition -Condition (Test-Path -LiteralPath $missingHomePath) -Message "Missing HomePage was not recreated."
Assert-Condition -Condition ((Compare-Hashes -Before @{ "Pages/Ssr/Home/HomePage.razor" = "missing" } -After (Get-TreeHashes -Root $projectRoot)).Contains("Pages/Ssr/Home/HomePage.razor")) -Message "Missing HomePage regeneration did not register as a change."
Set-TextFileContent -Path $missingHomePath -Content $missingHomeOriginal

New-TestProject
$missingProductPath = Join-Path $projectRoot "Components\Catalog\ProductSummaryCard.razor"
$missingProductOriginal = Get-Content -LiteralPath $missingProductPath -Raw
Remove-Item -LiteralPath $missingProductPath -Force
& (Join-Path $toolRoot "regenerate-storefront.ps1") -ProjectRoot $projectRoot -Scope component -Target ProductSummaryCard
Assert-Condition -Condition (Test-Path -LiteralPath $missingProductPath) -Message "Missing ProductSummaryCard was not recreated."
Set-TextFileContent -Path $missingProductPath -Content $missingProductOriginal

    New-TestProject
    $starterPackagePropsOriginal = Get-Content -LiteralPath $starterPackagePropsPath -Raw
    try {
        Set-TextFileContent -Path $starterPackagePropsPath -Content ($starterPackagePropsOriginal.Replace("1.0.0-local", "9.9.9-test"))
        $beforeFoundation = Get-TreeHashes -Root $projectRoot
        $env:SFB_KEEP_REGENERATION_CANDIDATE_ARTIFACTS = "1"
        & (Join-Path $toolRoot "regenerate-storefront.ps1") -ProjectRoot $projectRoot -Scope css -WhatIf
        $cssWhatIfCandidateRoot = (Get-ChildItem -LiteralPath (Join-Path $outputRoot ".regeneration-candidate") -Directory |
            Sort-Object LastWriteTime -Descending |
            Select-Object -First 1).FullName
        $cssWhatIfCandidateRoot = Join-Path $cssWhatIfCandidateRoot $projectName
        $cssReport = Get-Content -LiteralPath (Join-Path $cssWhatIfCandidateRoot "docs\storefront-analysis\regeneration-report.md") -Raw
        Assert-Condition -Condition $cssReport.Contains("StorefrontPackageVersions.props: skip out-of-scope", [System.StringComparison]::Ordinal) -Message "CSS scope did not keep StorefrontPackageVersions.props out of scope."
        if ($cssWhatIfCandidateRoot -and (Test-Path -LiteralPath $cssWhatIfCandidateRoot)) {
            Remove-Item -LiteralPath $cssWhatIfCandidateRoot -Recurse -Force
        }
        Remove-Item Env:SFB_KEEP_REGENERATION_CANDIDATE_ARTIFACTS -ErrorAction SilentlyContinue

        & (Join-Path $toolRoot "regenerate-storefront.ps1") -ProjectRoot $projectRoot -Scope foundation -ValidateAfterApply
        $foundationReport = Get-Content -LiteralPath (Join-Path $projectRoot "docs\storefront-analysis\regeneration-report.md") -Raw
        Assert-Condition -Condition $foundationReport.Contains("StorefrontPackageVersions.props: platform metadata update", [System.StringComparison]::Ordinal) -Message "Foundation update did not plan StorefrontPackageVersions.props."
        Assert-Condition -Condition ((Get-Content -LiteralPath (Join-Path $projectRoot "StorefrontPackageVersions.props") -Raw).Contains("9.9.9-test", [System.StringComparison]::Ordinal)) -Message "Foundation update did not apply StorefrontPackageVersions.props."
    }
    finally {
        Set-TextFileContent -Path $starterPackagePropsPath -Content $starterPackagePropsOriginal
    }

New-TestProject
$rollbackHomeOriginal = Get-Content -LiteralPath $starterHomePath -Raw
try {
    Set-TextFileContent -Path $starterHomePath -Content ($rollbackHomeOriginal.Replace("Featured products", "Featured products broken`n@{ invalid }"))
    $beforeRollback = Get-TreeHashes -Root $projectRoot
    Assert-Throws -ExpectedCode "SFB-REGEN-010" -Action {
        & (Join-Path $toolRoot "regenerate-storefront.ps1") -ProjectRoot $projectRoot -Scope page -Target HomePage -BuildAfterApply
    }
    $afterRollback = Get-TreeHashes -Root $projectRoot
    Assert-Condition -Condition ((Compare-Hashes -Before $beforeRollback -After $afterRollback).Count -eq 0) -Message "Rollback did not restore the target tree after build failure."
}
finally {
    Set-TextFileContent -Path $starterHomePath -Content $rollbackHomeOriginal
}

$regenerator = Get-Content -LiteralPath (Join-Path $toolRoot "regenerate-storefront.ps1") -Raw
Assert-Condition -Condition $regenerator.Contains('Copy-Item -LiteralPath $backupRoot -Destination $resolvedProjectRoot', [System.StringComparison]::Ordinal) -Message "Rollback restore path is missing."
Assert-Condition -Condition $regenerator.Contains("delete only if explicitly allowed", [System.StringComparison]::OrdinalIgnoreCase) -Message "Obsolete delete-safety marker is missing."
Assert-Condition -Condition $regenerator.Contains("SkipIdempotency", [System.StringComparison]::Ordinal) -Message "Validation skip marker is missing."
Assert-Condition -Condition $regenerator.Contains("SFB_KEEP_REGENERATION_CANDIDATE_ARTIFACTS", [System.StringComparison]::Ordinal) -Message "Candidate preservation marker is missing."

Write-Host "StorefrontBuilder regeneration safety tests passed."

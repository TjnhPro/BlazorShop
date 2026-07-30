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

function Assert-ContainsText {
    param(
        [Parameter(Mandatory = $true)][string]$Text,
        [Parameter(Mandatory = $true)][string]$Expected,
        [Parameter(Mandatory = $true)][string]$Message
    )

    Assert-Condition -Condition $Text.Contains($Expected, [System.StringComparison]::Ordinal) -Message $Message
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
        [Parameter(Mandatory = $true)][string[]]$RegeneratorArguments,
        [switch]$PreserveCandidateArtifacts,
        [string]$DropCandidateFilePaths = ""
    )

    $previousKeep = $env:SFB_KEEP_REGENERATION_CANDIDATE_ARTIFACTS
    $previousDrop = $env:SFB_DROP_CANDIDATE_FILE_PATHS
    if ($PreserveCandidateArtifacts) {
        $env:SFB_KEEP_REGENERATION_CANDIDATE_ARTIFACTS = "1"
    }
    elseif ($null -ne $env:SFB_KEEP_REGENERATION_CANDIDATE_ARTIFACTS) {
        Remove-Item Env:SFB_KEEP_REGENERATION_CANDIDATE_ARTIFACTS -ErrorAction SilentlyContinue
    }

    if (-not [string]::IsNullOrWhiteSpace($DropCandidateFilePaths)) {
        $env:SFB_DROP_CANDIDATE_FILE_PATHS = $DropCandidateFilePaths
    }

    try {
        $commandArguments = @("-ProjectRoot", $ProjectRoot) + $RegeneratorArguments
        $powerShellPath = (Get-Process -Id $PID).Path
        $output = @(& $powerShellPath -NoProfile -ExecutionPolicy Bypass -File (Join-Path $toolRoot "regenerate-storefront.ps1") @commandArguments 2>&1)
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

        if ($null -ne $previousDrop) {
            $env:SFB_DROP_CANDIDATE_FILE_PATHS = $previousDrop
        }
        else {
            Remove-Item Env:SFB_DROP_CANDIDATE_FILE_PATHS -ErrorAction SilentlyContinue
        }
    }
}

function Get-WhatIfReportPathFromOutput {
    param([Parameter(Mandatory = $true)][array]$Output)

    foreach ($line in $Output) {
        if ($line -match "^WhatIf report: (.+)$") {
            return $Matches[1]
        }
    }

    throw "WhatIf output did not include a stable report path."
}

function Test-CandidateArtifactsCleaned {
    param([Parameter(Mandatory = $true)][string]$OutputRoot)

    $candidateRoot = Join-Path $OutputRoot ".regeneration-candidate"
    return (-not (Test-Path -LiteralPath $candidateRoot)) -or @((Get-ChildItem -LiteralPath $candidateRoot -Force)).Count -eq 0
}

function Get-YamlScalarValue {
    param(
        [Parameter(Mandatory = $true)][string]$Text,
        [Parameter(Mandatory = $true)][string]$Key
    )

    $match = [regex]::Match($Text, "(?m)^$([regex]::Escape($Key)):\s*(\S+)\s*$")
    if (-not $match.Success) {
        throw "YAML scalar '$Key' was not found."
    }

    return $match.Groups[1].Value.Trim('"')
}

function Get-ManifestGeneratorVersions {
    param([Parameter(Mandatory = $true)][string]$Manifest)

    return @([regex]::Matches($Manifest, "(?m)^\s+generatorVersion:\s*(\S+)\s*$") |
        ForEach-Object { $_.Groups[1].Value.Trim('"') } |
        Sort-Object -Unique)
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

$metadataText = Get-Content -LiteralPath (Join-Path $projectRoot "docs\storefront-analysis\metadata.yaml") -Raw
$manifestPath = Join-Path $projectRoot "docs\storefront-analysis\generated-files.yaml"
$manifestText = Get-Content -LiteralPath $manifestPath -Raw
$metadataGeneratorVersion = Get-YamlScalarValue -Text $metadataText -Key "generatorVersion"
$generatedFileManifestVersions = @(Get-ManifestGeneratorVersions -Manifest $manifestText)
Assert-Condition -Condition ($metadataGeneratorVersion -eq "2.5.0") -Message "Generated metadata did not use the shared StorefrontBuilder generatorVersion."
Assert-Condition -Condition ($generatedFileManifestVersions.Count -gt 0) -Message "Generated file manifest did not include generatorVersion entries."
Assert-Condition -Condition ($generatedFileManifestVersions.Count -eq 1 -and $generatedFileManifestVersions[0] -eq $metadataGeneratorVersion) -Message "Generated metadata and manifest generatorVersion values did not match."

try {
    Set-TextFileContent -Path $manifestPath -Content ($manifestText -replace "generatorVersion:\s*$([regex]::Escape($metadataGeneratorVersion))", "generatorVersion: 9.9.9-test")
    Assert-Throws -ExpectedCode "SFB-IDEMPOTENCY-012" -Action {
        & (Join-Path $toolRoot "scripts\validate\Test-StorefrontBuilderIdempotency.ps1") -ProjectRoot $projectRoot
    }
}
finally {
    Set-TextFileContent -Path $manifestPath -Content $manifestText
}

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
$whatIfCandidateRoot = $null
try {
    Set-TextFileContent -Path $starterHomePath -Content ($starterHomeOriginal.Replace("Featured products", "Featured products whatif"))
    Set-TextFileContent -Path $starterLayoutPath -Content ($starterLayoutOriginal.Replace("Deals", "Specials"))
    Set-TextFileContent -Path $projectLayoutPath -Content ((Get-Content -LiteralPath $projectLayoutPath -Raw) + "`n<!-- manual edit -->")
    Set-TextFileContent -Path $projectReadmePath -Content ((Get-Content -LiteralPath $projectReadmePath -Raw) + "`nHuman-owned note.")
    Set-TextFileContent -Path $starterCreatedPath -Content $starterCreatedContent
    & (Join-Path $toolRoot "scripts\generate\update-generated-files-manifest.mjs") --project-root $projectRoot

    $baselineHashes = Get-TreeHashes -Root $projectRoot
    $whatIfResult = Invoke-StorefrontRegeneration -ProjectRoot $projectRoot -RegeneratorArguments @("-Scope", "all", "-WhatIf") -DropCandidateFilePaths "Pages/Hybrid/Catalog/SearchPage.razor"
    $whatIfConsole = $whatIfResult.Output -join [System.Environment]::NewLine
    $stableReportPath = Get-WhatIfReportPathFromOutput -Output $whatIfResult.Output
    Assert-Condition -Condition (Test-Path -LiteralPath $stableReportPath) -Message "Normal WhatIf did not leave a stable report."
    Assert-Condition -Condition (Test-CandidateArtifactsCleaned -OutputRoot $outputRoot) -Message "Normal WhatIf left temporary candidate artifacts behind."
    Assert-Condition -Condition ((Compare-Hashes -Before $baselineHashes -After (Get-TreeHashes -Root $projectRoot)).Count -eq 0) -Message "Normal WhatIf modified the target tree."

    $report = Get-Content -LiteralPath $stableReportPath -Raw
    Assert-ContainsText -Text $report -Expected "Pages/Ssr/Home/WhatIfCreated.razor: create" -Message "Stable WhatIf report did not include created candidate."
    Assert-ContainsText -Text $report -Expected "Pages/Ssr/Home/HomePage.razor: update" -Message "Stable WhatIf report did not include updated HomePage."
    Assert-ContainsText -Text $report -Expected "Components/Layout/MainLayout.razor: conflict manual edit" -Message "Stable WhatIf report did not include manual-edit conflict."
    Assert-ContainsText -Text $report -Expected "Pages/Hybrid/Catalog/SearchPage.razor" -Message "Stable WhatIf report did not include obsolete candidate path."
    Assert-ContainsText -Text $report -Expected "obsolete candidate" -Message "Stable WhatIf report did not include obsolete candidate action."
    Assert-Condition -Condition ($report.Contains("README.md: skip user-owned", [System.StringComparison]::Ordinal) -or $report.Contains("README.md: skip protected", [System.StringComparison]::Ordinal)) -Message "Stable WhatIf report did not preserve user-owned or protected README."

    Assert-ContainsText -Text $whatIfConsole -Expected "WhatIf report: $stableReportPath" -Message "WhatIf console did not print the stable report path."
    Assert-ContainsText -Text $whatIfConsole -Expected "WhatIf summary: create=" -Message "WhatIf console did not print summary counts."
    Assert-ContainsText -Text $whatIfConsole -Expected "Pages/Ssr/Home/HomePage.razor: update - " -Message "WhatIf console did not print a meaningful action line."
    Assert-ContainsText -Text $whatIfConsole -Expected "WhatIf next action: resolve conflicts manually, rerun -Scope conflicts, then rerun the desired update scope." -Message "WhatIf console did not print conflict next-action guidance."

    $internalPlannerResult = Invoke-StorefrontRegeneration -ProjectRoot $projectRoot -RegeneratorArguments @("-Scope", "all", "-WhatIf") -PreserveCandidateArtifacts -DropCandidateFilePaths "Pages/Hybrid/Catalog/SearchPage.razor"
    $whatIfCandidateRoot = $internalPlannerResult.CandidateRoot
    $candidateReportPath = Join-Path $whatIfCandidateRoot "docs\storefront-analysis\regeneration-report.md"
    Assert-Condition -Condition (Test-Path -LiteralPath $candidateReportPath) -Message "Internal preserved WhatIf candidate report was not available for planner inspection."
    $candidateReport = Get-Content -LiteralPath $candidateReportPath -Raw
    Assert-ContainsText -Text $candidateReport -Expected "Pages/Ssr/Home/WhatIfCreated.razor: create" -Message "Internal WhatIf planner report did not include created candidate."
    Assert-ContainsText -Text $candidateReport -Expected "Pages/Ssr/Home/HomePage.razor: update" -Message "Internal WhatIf planner report did not include updated HomePage."
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
$customWhatIfReportPath = Join-Path $repoRoot "obj\storefront-builder\whatif\regeneration-safety-custom.md"
if (Test-Path -LiteralPath $customWhatIfReportPath) {
    Remove-Item -LiteralPath $customWhatIfReportPath -Force
}

$customWhatIfResult = Invoke-StorefrontRegeneration -ProjectRoot $projectRoot -RegeneratorArguments @("-Scope", "css", "-WhatIf", "-WhatIfReportPath", $customWhatIfReportPath)
$customWhatIfConsole = $customWhatIfResult.Output -join [System.Environment]::NewLine
Assert-Condition -Condition (Test-Path -LiteralPath $customWhatIfReportPath) -Message "Custom WhatIf report path was not created."
Assert-ContainsText -Text $customWhatIfConsole -Expected "WhatIf report: $customWhatIfReportPath" -Message "WhatIf console did not print the custom report path."
Assert-Condition -Condition (Test-CandidateArtifactsCleaned -OutputRoot $outputRoot) -Message "Custom WhatIf left temporary candidate artifacts behind."

New-TestProject
$targetReportPath = Join-Path $projectRoot "docs\storefront-analysis\whatif-report.md"
Assert-Throws -ExpectedCode "SFB-REGEN-020" -Action {
    & (Join-Path $toolRoot "regenerate-storefront.ps1") -ProjectRoot $projectRoot -Scope css -WhatIf -WhatIfReportPath $targetReportPath
}
Assert-Condition -Condition (-not (Test-Path -LiteralPath $targetReportPath)) -Message "Rejected target-scoped WhatIf report path was written."
Assert-Condition -Condition (Test-CandidateArtifactsCleaned -OutputRoot $outputRoot) -Message "Rejected target-scoped WhatIf report path generated a candidate before failing."

$unsafeWhatIfReportPath = Join-Path ([System.IO.Path]::GetTempPath()) "storefront-builder-unsafe-whatif-report.md"
if (Test-Path -LiteralPath $unsafeWhatIfReportPath) {
    Remove-Item -LiteralPath $unsafeWhatIfReportPath -Force
}

Assert-Throws -ExpectedCode "SFB-REGEN-021" -Action {
    & (Join-Path $toolRoot "regenerate-storefront.ps1") -ProjectRoot $projectRoot -Scope css -WhatIf -WhatIfReportPath $unsafeWhatIfReportPath
}
Assert-Condition -Condition (-not (Test-Path -LiteralPath $unsafeWhatIfReportPath)) -Message "Rejected unsafe WhatIf report path was written."
Assert-Condition -Condition (Test-CandidateArtifactsCleaned -OutputRoot $outputRoot) -Message "Rejected unsafe WhatIf report path generated a candidate before failing."

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

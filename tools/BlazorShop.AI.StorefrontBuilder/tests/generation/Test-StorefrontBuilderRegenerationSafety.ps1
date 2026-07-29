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

$beforeWhatIf = Get-TreeHashes -Root $projectRoot
& (Join-Path $toolRoot "regenerate-storefront.ps1") -ProjectRoot $projectRoot -Scope all -WhatIf
$afterWhatIf = Get-TreeHashes -Root $projectRoot
Assert-Condition -Condition ((Compare-Hashes -Before $beforeWhatIf -After $afterWhatIf).Count -eq 0) -Message "WhatIf wrote files."

& (Join-Path $toolRoot "regenerate-storefront.ps1") -ProjectRoot $projectRoot -Scope all
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

$beforePage = Get-TreeHashes -Root $projectRoot
& (Join-Path $toolRoot "regenerate-storefront.ps1") -ProjectRoot $projectRoot -Scope page -Target HomePage
$afterPage = Get-TreeHashes -Root $projectRoot
Assert-Condition -Condition (((@(Compare-Hashes -Before $beforePage -After $afterPage | Where-Object { $_ -notlike "docs/storefront-analysis/*" })).Count) -eq 0) -Message "Page scope touched unrelated files."

$beforeComponent = Get-TreeHashes -Root $projectRoot
& (Join-Path $toolRoot "regenerate-storefront.ps1") -ProjectRoot $projectRoot -Scope component -Target ProductSummaryCard
$afterComponent = Get-TreeHashes -Root $projectRoot
Assert-Condition -Condition (((@(Compare-Hashes -Before $beforeComponent -After $afterComponent | Where-Object { $_ -notlike "docs/storefront-analysis/*" })).Count) -eq 0) -Message "Component scope touched unrelated files."

Add-Content -LiteralPath (Join-Path $projectRoot "Components\Layout\MainLayout.razor") -Value "`n<!-- manual edit -->"
& (Join-Path $toolRoot "regenerate-storefront.ps1") -ProjectRoot $projectRoot -Scope all
$report = Get-Content -LiteralPath (Join-Path $projectRoot "docs\storefront-analysis\regeneration-report.md") -Raw
Assert-Condition -Condition $report.Contains("conflict manual edit", [System.StringComparison]::Ordinal) -Message "Manual Razor edit was not reported as conflict."

$customFile = Join-Path $projectRoot "CustomNotes.md"
Set-Content -LiteralPath $customFile -Value "human-owned" -Encoding UTF8
& (Join-Path $toolRoot "regenerate-storefront.ps1") -ProjectRoot $projectRoot -Scope all
Assert-Condition -Condition ((Get-Content -LiteralPath $customFile -Raw).Trim() -eq "human-owned") -Message "User-owned custom file was overwritten."

Add-Content -LiteralPath (Join-Path $projectRoot "StorefrontPackageVersions.props") -Value "`n<!-- protected edit -->"
Assert-Throws -ExpectedCode "SFB-IDEMPOTENCY-002" -Action {
    & (Join-Path $toolRoot "regenerate-storefront.ps1") -ProjectRoot $projectRoot -Scope conflicts
}

$regenerator = Get-Content -LiteralPath (Join-Path $toolRoot "regenerate-storefront.ps1") -Raw
Assert-Condition -Condition $regenerator.Contains('Copy-Item -LiteralPath $backupRoot -Destination $resolvedProjectRoot', [System.StringComparison]::Ordinal) -Message "Rollback restore path is missing."
Assert-Condition -Condition $regenerator.Contains("delete only if explicitly allowed", [System.StringComparison]::OrdinalIgnoreCase) -Message "Obsolete delete-safety marker is missing."

Write-Host "StorefrontBuilder regeneration safety tests passed."

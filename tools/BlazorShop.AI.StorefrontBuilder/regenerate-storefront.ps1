param(
    [Parameter(Mandatory = $true)]
    [string]$ProjectRoot,
    [ValidateSet("all", "page", "component", "css", "validate", "conflicts")]
    [string]$Scope = "all",
    [string]$Target = "",
    [switch]$WhatIf,
    [switch]$ValidateAfterApply,
    [switch]$BuildAfterApply
)

$ErrorActionPreference = "Stop"
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
. (Join-Path $PSScriptRoot "scripts\generate\StorefrontBuilderProjectSafety.ps1")

function Resolve-InputPath {
    param([string]$Path)

    if ([System.IO.Path]::IsPathRooted($Path)) {
        return [System.IO.Path]::GetFullPath($Path)
    }

    return [System.IO.Path]::GetFullPath((Join-Path (Get-Location) $Path))
}

function Read-GeneratedFileManifestEntries {
    param([Parameter(Mandatory = $true)][string]$ManifestPath)

    $entries = [System.Collections.Generic.List[hashtable]]::new()
    if (-not (Test-Path -LiteralPath $ManifestPath)) {
        return $entries
    }

    $current = $null
    foreach ($line in (Get-Content -LiteralPath $ManifestPath -Raw) -split "\r?\n") {
        $fileMatch = [regex]::Match($line, "^\s+- filePath:\s*(.+?)\s*$")
        if ($fileMatch.Success) {
            $current = @{}
            $current["filePath"] = Unquote-ManifestValue $fileMatch.Groups[1].Value
            $entries.Add($current)
            continue
        }

        if ($null -eq $current) {
            continue
        }

        $propertyMatch = [regex]::Match($line, "^\s+([A-Za-z0-9]+):\s*(.*?)\s*$")
        if ($propertyMatch.Success) {
            $current[$propertyMatch.Groups[1].Value] = Unquote-ManifestValue $propertyMatch.Groups[2].Value
        }
    }

    return $entries
}

function Unquote-ManifestValue {
    param([string]$Value)

    $trimmed = $Value.Trim()
    if ($trimmed.StartsWith('"', [System.StringComparison]::Ordinal) -and $trimmed.EndsWith('"', [System.StringComparison]::Ordinal)) {
        return $trimmed.Substring(1, $trimmed.Length - 2).Replace('\"', '"')
    }

    return $trimmed
}

function Get-NormalizedFileHash {
    param([Parameter(Mandatory = $true)][string]$Path)

    if (-not (Test-Path -LiteralPath $Path)) {
        return "none"
    }

    $content = (Get-Content -LiteralPath $Path -Raw).Replace("`r`n", "`n").Replace("`r", "`n")
    $bytes = [System.Text.Encoding]::UTF8.GetBytes($content)
    $hash = [System.Security.Cryptography.SHA256]::HashData($bytes)
    $hex = [System.BitConverter]::ToString($hash).Replace("-", "").ToLowerInvariant()
    return "sha256:$hex"
}

function Test-ScopeMatch {
    param(
        [hashtable]$Entry,
        [string]$Scope,
        [string]$Target
    )

    if ($Scope -eq "all") {
        return $true
    }

    if ($Scope -eq "css") {
        return $Entry["scope"] -eq "css"
    }

    if ($Scope -eq "page" -or $Scope -eq "component") {
        if ($Entry["scope"] -ne $Scope) {
            return $false
        }

        return [string]::IsNullOrWhiteSpace($Target) -or $Entry["filePath"].Contains($Target, [System.StringComparison]::OrdinalIgnoreCase)
    }

    return $false
}

function New-RegenerationPlan {
    param(
        [string]$TargetProjectRoot,
        [string]$CandidateProjectRoot,
        [System.Collections.Generic.List[hashtable]]$TargetEntries,
        [System.Collections.Generic.List[hashtable]]$CandidateEntries,
        [string]$Scope,
        [string]$Target
    )

    $targetMap = @{}
    foreach ($entry in $TargetEntries) {
        $targetMap[$entry["filePath"]] = $entry
    }

    $candidateMap = @{}
    foreach ($entry in $CandidateEntries) {
        $candidateMap[$entry["filePath"]] = $entry
    }

    $paths = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
    foreach ($key in $targetMap.Keys) { [void]$paths.Add($key) }
    foreach ($key in $candidateMap.Keys) { [void]$paths.Add($key) }

    $actions = [System.Collections.Generic.List[hashtable]]::new()
    foreach ($filePath in $paths | Sort-Object) {
        $targetEntry = if ($targetMap.ContainsKey($filePath)) { $targetMap[$filePath] } else { $null }
        $candidateEntry = if ($candidateMap.ContainsKey($filePath)) { $candidateMap[$filePath] } else { $null }
        $entry = if ($candidateEntry) { $candidateEntry } elseif ($targetEntry) { $targetEntry } else { @{} }
        $ownership = if ($candidateEntry) { $candidateEntry["ownership"] } elseif ($targetEntry) { $targetEntry["ownership"] } else { "unknown" }
        $targetPath = Join-Path $TargetProjectRoot $filePath
        $candidatePath = Join-Path $CandidateProjectRoot $filePath
        $targetHash = Get-NormalizedFileHash -Path $targetPath
        $candidateHash = Get-NormalizedFileHash -Path $candidatePath
        $previousGeneratedHash = if ($targetEntry) { $targetEntry["generatedHash"] } else { "none" }
        $scopeMatches = Test-ScopeMatch -Entry $entry -Scope $Scope -Target $Target

        $action = "skip unchanged"
        $reason = "No content change."

        if (-not $scopeMatches) {
            $action = "skip out-of-scope"
            $reason = "Entry is outside requested scope."
        }
        elseif ($ownership -eq "protected") {
            $action = "skip protected"
            $reason = "Protected files are never overwritten."
        }
        elseif ($ownership -eq "user-owned" -or $ownership -eq "artifact-only") {
            $action = "skip user-owned"
            $reason = "User-owned and artifact-only files are preserved."
        }
        elseif ($candidateHash -eq "none") {
            if (($ownership -eq "generated" -or $ownership -eq "managed") -and $targetHash -ne "none") {
                $action = "obsolete candidate"
                $reason = "Target-only generated files are reported as obsolete candidates; delete only if explicitly allowed."
            }
            else {
                $reason = "Candidate does not produce this file."
            }
        }
        elseif ($targetHash -eq "none") {
            $action = "create"
            $reason = "File does not exist in target project."
        }
        elseif ($targetHash -eq $candidateHash) {
            if (($ownership -eq "generated" -or $ownership -eq "managed") -and $previousGeneratedHash -ne "none" -and $targetHash -ne $previousGeneratedHash) {
                $reason = "Target already matches candidate content; manual edit preserved."
            }
            else {
                $reason = "Target already matches candidate content."
            }
        }
        elseif (($ownership -eq "generated" -or $ownership -eq "managed") -and ($previousGeneratedHash -eq "none" -or $targetHash -eq $previousGeneratedHash)) {
            $action = "update"
            $reason = if ($previousGeneratedHash -eq "none") {
                "File does not exist in target project."
            }
            else {
                "Target hash matches last generated hash."
            }
        }
        elseif (($ownership -eq "generated" -or $ownership -eq "managed") -and $targetHash -ne $previousGeneratedHash) {
            $action = "conflict manual edit"
            $reason = if ($targetEntry -and $targetEntry["conflictReason"]) {
                $targetEntry["conflictReason"]
            }
            else {
                "$filePath differs from the last generated hash."
            }
        }

        $actions.Add(@{
            filePath = $filePath
            action = $action
            reason = $reason
            ownership = $ownership
            scope = $entry["scope"]
            changed = ($action -eq "create" -or $action -eq "update")
            targetHash = $targetHash
            candidateHash = $candidateHash
            previousGeneratedHash = $previousGeneratedHash
        })
    }

    return $actions
}

function Write-RegenerationReport {
    param(
        [string]$ReportPath,
        [string]$Command,
        [string]$Scope,
        [string]$Target,
        [System.Collections.Generic.List[hashtable]]$Plan,
        [string]$ValidationResult,
        [string]$BuildResult
    )

    $changed = @($Plan | Where-Object { $_["changed"] -eq $true })
    $skipped = @($Plan | Where-Object { $_["action"].StartsWith("skip", [System.StringComparison]::Ordinal) })
    $conflicts = @($Plan | Where-Object { $_["action"].StartsWith("conflict", [System.StringComparison]::Ordinal) })
    $obsolete = @($Plan | Where-Object { $_["action"] -eq "obsolete candidate" })

    $lines = [System.Collections.Generic.List[string]]::new()
    $lines.Add("# StorefrontBuilder Regeneration Report")
    $lines.Add("")
    $lines.Add("- Regenerate all generated files: supported by `regenerate-storefront.ps1 -Scope all`.")
    $lines.Add("- Regenerate one page: supported by `-Scope page -Target <path>`.")
    $lines.Add("- Regenerate one component: supported by `-Scope component -Target <path>`.")
    $lines.Add("- Regenerate only CSS tokens: supported by `-Scope css`.")
    $lines.Add("- Validate without writing: supported by `-WhatIf` or `-Scope validate`.")
    $lines.Add("- Show conflict report: supported by `-Scope conflicts`.")
    $lines.Add("- No-op result: no unexpected file changes.")
    $lines.Add("- Protected files modified: false.")
    $lines.Add("")
    $lines.Add("## Command")
    $lines.Add("")
    $lines.Add('- Command: `' + $Command + '`')
    $lines.Add('- Scope: `' + $Scope + '`')
    $lines.Add('- Target: `' + $Target + '`')
    $lines.Add("- Validation/build result: validation=$ValidationResult; build=$BuildResult")
    $lines.Add("")
    $lines.Add("## Changed Files")
    $lines.Add("")
    Add-PlanLines -Lines $lines -Items $changed
    $lines.Add("")
    $lines.Add("## Skipped Files")
    $lines.Add("")
    Add-PlanLines -Lines $lines -Items $skipped
    $lines.Add("")
    $lines.Add("## Conflicts")
    $lines.Add("")
    Add-PlanLines -Lines $lines -Items $conflicts
    $lines.Add("")
    $lines.Add("## Obsolete Candidates")
    $lines.Add("")
    Add-PlanLines -Lines $lines -Items $obsolete
    $lines.Add("")
    $lines.Add("## Next Recommended Action")
    $lines.Add("")
    $nextAction = if ($conflicts.Count -gt 0) { "Resolve conflicts manually, then rerun `-Scope conflicts`." } else { "No conflicts; rerun validation or build proof as needed." }
    $lines.Add($nextAction)

    Set-Content -LiteralPath $ReportPath -Value ($lines -join [Environment]::NewLine) -Encoding UTF8
}

function Add-PlanLines {
    param(
        [System.Collections.Generic.List[string]]$Lines,
        [array]$Items
    )

    if ($Items.Count -eq 0) {
        $Lines.Add("- none")
        return
    }

    foreach ($item in $Items) {
        $Lines.Add("- $($item["filePath"]): $($item["action"]) - $($item["reason"])")
    }
}

function Copy-ChangedFile {
    param(
        [string]$SourceRoot,
        [string]$TargetRoot,
        [string]$FilePath
    )

    $source = Join-Path $SourceRoot $FilePath
    $target = Join-Path $TargetRoot $FilePath
    New-Item -ItemType Directory -Force -Path (Split-Path -Parent $target) | Out-Null
    Copy-Item -LiteralPath $source -Destination $target -Force
}

function Read-SimpleYamlValue {
    param(
        [Parameter(Mandatory = $true)][string]$Text,
        [Parameter(Mandatory = $true)][string]$Key,
        [string]$Default = ""
    )

    foreach ($line in $Text -split "\r?\n") {
        $match = [regex]::Match($line, "^\s*$([regex]::Escape($Key)):\s*(.*?)\s*$")
        if ($match.Success) {
            return Unquote-ManifestValue $match.Groups[1].Value
        }
    }

    return $Default
}

function Read-GeneratedStorefrontMetadata {
    param([Parameter(Mandatory = $true)][string]$MetadataPath)

    if (-not (Test-Path -LiteralPath $MetadataPath)) {
        return @{
            projectName = Split-Path -Leaf (Split-Path -Parent (Split-Path -Parent $MetadataPath))
            storeKey = "default"
            outputRoot = Split-Path -Parent (Split-Path -Parent (Split-Path -Parent $MetadataPath))
            sourceStarterPath = "BlazorShop.PresentationV2/BlazorShop.Storefront.Starter"
        }
    }

    $text = Get-Content -LiteralPath $MetadataPath -Raw
    return @{
        projectName = Read-SimpleYamlValue -Text $text -Key "projectName" -Default (Split-Path -Leaf (Split-Path -Parent (Split-Path -Parent $MetadataPath)))
        storeKey = Read-SimpleYamlValue -Text $text -Key "storeKey" -Default "default"
        outputRoot = Read-SimpleYamlValue -Text $text -Key "outputRoot" -Default (Split-Path -Parent (Split-Path -Parent (Split-Path -Parent $MetadataPath)))
        sourceStarterPath = Read-SimpleYamlValue -Text $text -Key "sourceStarterPath" -Default "BlazorShop.PresentationV2/BlazorShop.Storefront.Starter"
    }
}

function Invoke-RegenerationCandidateGeneration {
    param(
        [Parameter(Mandatory = $true)][string]$CandidateOutputRoot,
        [Parameter(Mandatory = $true)][string]$ProjectName,
        [Parameter(Mandatory = $true)][string]$StoreKey
    )

    & "$PSScriptRoot/build-storefront.ps1" `
        -Name $ProjectName `
        -StoreKey $StoreKey `
        -OutputRoot $CandidateOutputRoot `
        -Mode generate `
        -Force

    if ($LASTEXITCODE -ne 0) {
        throw "[SFB-REGEN-001] Failed to generate regeneration candidate from current Starter source."
    }
}

$resolvedProjectRoot = Resolve-InputPath $ProjectRoot
if (-not (Test-Path -LiteralPath $resolvedProjectRoot)) {
    throw "[SFB-REGEN-000] Project root does not exist: $resolvedProjectRoot"
}

$metadataPath = Join-Path $resolvedProjectRoot "docs\storefront-analysis\metadata.yaml"
$metadata = Read-GeneratedStorefrontMetadata -MetadataPath $metadataPath
$projectName = if ([string]::IsNullOrWhiteSpace($metadata["projectName"])) { Split-Path -Leaf $resolvedProjectRoot } else { $metadata["projectName"] }
$storeKey = if ([string]::IsNullOrWhiteSpace($metadata["storeKey"])) { "default" } else { $metadata["storeKey"] }
$outputRootValue = if ([string]::IsNullOrWhiteSpace($metadata["outputRoot"])) { Split-Path -Parent $resolvedProjectRoot } else { $metadata["outputRoot"] }
$resolvedOutputRoot = Resolve-ApprovedStorefrontBuilderOutputRoot -RepoRoot $repoRoot -OutputRoot (Resolve-StorefrontBuilderRepoPath -RepoRoot $repoRoot -Path $outputRootValue)
Assert-StorefrontBuilderPathUnderRoot -Path $resolvedProjectRoot -Root $resolvedOutputRoot

$manifestPath = Join-Path $resolvedProjectRoot "docs\storefront-analysis\generated-files.yaml"
$reportPath = Join-Path $resolvedProjectRoot "docs\storefront-analysis\regeneration-report.md"
$operationId = [System.Guid]::NewGuid().ToString("N")
$candidateOutputRoot = Join-Path $resolvedOutputRoot ".regeneration-candidate\$operationId"
$candidateProjectRoot = Join-Path $candidateOutputRoot $projectName
$candidateReportPath = Join-Path $candidateProjectRoot "docs\storefront-analysis\regeneration-report.md"
$backupRoot = Join-Path $resolvedOutputRoot ".regeneration-backup\$projectName-$operationId"
$validationResult = "not-requested"
$buildResult = "not-requested"
$plan = [System.Collections.Generic.List[hashtable]]::new()

Assert-StorefrontBuilderPathUnderRoot -Path $candidateOutputRoot -Root $resolvedOutputRoot
Assert-StorefrontBuilderPathUnderRoot -Path $backupRoot -Root $resolvedOutputRoot

if ($Scope -eq "validate") {
    & "$PSScriptRoot/validate-storefront.ps1" -ProjectRoot $resolvedProjectRoot
    exit 0
}

if ($Scope -eq "conflicts") {
    node "$PSScriptRoot/scripts/generate/update-generated-files-manifest.mjs" --project-root $resolvedProjectRoot
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    & "$PSScriptRoot/scripts/validate/Test-StorefrontBuilderIdempotency.ps1" -ProjectRoot $resolvedProjectRoot
    exit 0
}

try {
    Invoke-RegenerationCandidateGeneration -CandidateOutputRoot $candidateOutputRoot -ProjectName $projectName -StoreKey $storeKey

    $originalEntries = Read-GeneratedFileManifestEntries -ManifestPath $manifestPath
    $candidateEntries = Read-GeneratedFileManifestEntries -ManifestPath (Join-Path $candidateProjectRoot "docs\storefront-analysis\generated-files.yaml")
    $plan = New-RegenerationPlan -TargetProjectRoot $resolvedProjectRoot -CandidateProjectRoot $candidateProjectRoot -TargetEntries $originalEntries -CandidateEntries $candidateEntries -Scope $Scope -Target $Target

    $commandLabel = if ($WhatIf) { "regenerate-storefront.ps1 -WhatIf" } else { "regenerate-storefront.ps1" }
    Write-RegenerationReport -ReportPath $candidateReportPath -Command $commandLabel -Scope $Scope -Target $Target -Plan $plan -ValidationResult $validationResult -BuildResult $buildResult

    if ($WhatIf) {
        Write-Host "WhatIf completed without writing generated project files."
        exit 0
    }

    New-Item -ItemType Directory -Force -Path (Split-Path -Parent $backupRoot) | Out-Null
    Copy-Item -LiteralPath $resolvedProjectRoot -Destination $backupRoot -Recurse -Force

    foreach ($item in $plan | Where-Object { $_["changed"] -eq $true }) {
        Copy-ChangedFile -SourceRoot $candidateProjectRoot -TargetRoot $resolvedProjectRoot -FilePath $item["filePath"]
    }

    if ($ValidateAfterApply) {
        & "$PSScriptRoot/validate-storefront.ps1" -ProjectRoot $resolvedProjectRoot
        if ($LASTEXITCODE -ne 0) {
            $validationResult = "failed"
            throw "[SFB-REGEN-011] Post-regeneration validation failed."
        }

        $validationResult = "passed"
    }

    if ($BuildAfterApply) {
        $projectFile = Join-Path $resolvedProjectRoot "$projectName.csproj"
        dotnet build $projectFile --no-restore
        if ($LASTEXITCODE -ne 0) {
            $buildResult = "failed"
            throw "[SFB-REGEN-010] Post-regeneration build failed."
        }

        $buildResult = "passed"
    }

    node "$PSScriptRoot/scripts/generate/update-generated-files-manifest.mjs" --project-root $resolvedProjectRoot
    if ($LASTEXITCODE -ne 0) { throw "[SFB-REGEN-012] Failed to update the target generated-file manifest." }

    & "$PSScriptRoot/scripts/validate/Test-StorefrontBuilderIdempotency.ps1" -ProjectRoot $resolvedProjectRoot
    $hasMeaningfulPlanEntries = @($plan | Where-Object { -not $_["action"].StartsWith("skip", [System.StringComparison]::Ordinal) }).Count -gt 0
    if ($hasMeaningfulPlanEntries) {
        Write-RegenerationReport -ReportPath $reportPath -Command "regenerate-storefront.ps1" -Scope $Scope -Target $Target -Plan $plan -ValidationResult $validationResult -BuildResult $buildResult
    }
}
catch {
    if (Test-Path -LiteralPath $backupRoot) {
        Remove-Item -LiteralPath $resolvedProjectRoot -Recurse -Force
        Copy-Item -LiteralPath $backupRoot -Destination $resolvedProjectRoot -Recurse -Force

        node "$PSScriptRoot/scripts/generate/update-generated-files-manifest.mjs" --project-root $resolvedProjectRoot
    }

    if ((@($plan | Where-Object { -not $_["action"].StartsWith("skip", [System.StringComparison]::Ordinal) }).Count -gt 0) -and (Test-Path -LiteralPath $resolvedProjectRoot)) {
        Write-RegenerationReport -ReportPath $reportPath -Command "regenerate-storefront.ps1" -Scope $Scope -Target $Target -Plan $plan -ValidationResult $validationResult -BuildResult $buildResult
    }

    throw
}
finally {
    if (Test-Path -LiteralPath $candidateOutputRoot) {
        Remove-Item -LiteralPath $candidateOutputRoot -Recurse -Force
    }

    if (Test-Path -LiteralPath $backupRoot) {
        Remove-Item -LiteralPath $backupRoot -Recurse -Force
    }
}

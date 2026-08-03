param(
    [Parameter(Mandatory = $true)]
    [string]$ProjectRoot,
    [ValidateSet("all", "page", "component", "css", "foundation", "validate", "conflicts")]
    [string]$Scope = "all",
    [string]$Target = "",
    [switch]$WhatIf,
    [string]$WhatIfReportPath = "",
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

function Test-TextContains([string]$Text, [string]$Value, [System.StringComparison]$Comparison = [System.StringComparison]::Ordinal) {
    return $Text.IndexOf($Value, $Comparison) -ge 0
}

function Get-Sha256Hex([byte[]]$Bytes) {
    $sha = [System.Security.Cryptography.SHA256]::Create()
    try {
        return [System.BitConverter]::ToString($sha.ComputeHash($Bytes)).Replace("-", "").ToLowerInvariant()
    }
    finally {
        $sha.Dispose()
    }
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

    $content = [System.Text.Encoding]::UTF8.GetString([System.IO.File]::ReadAllBytes($Path)).Replace("`r`n", "`n").Replace("`r", "`n")
    $bytes = [System.Text.Encoding]::UTF8.GetBytes($content)
    $hex = Get-Sha256Hex $bytes
    return "sha256:$hex"
}

function Get-NormalizedTextSha256 {
    param([Parameter(Mandatory = $true)][string]$Text)

    $normalized = $Text.Replace("`r`n", "`n").Replace("`r", "`n")
    $bytes = [System.Text.Encoding]::UTF8.GetBytes($normalized)
    return Get-Sha256Hex $bytes
}

function Get-NormalizedFileSha256 {
    param([Parameter(Mandatory = $true)][string]$Path)

    if (-not (Test-Path -LiteralPath $Path)) {
        return ""
    }

    $content = [System.Text.Encoding]::UTF8.GetString([System.IO.File]::ReadAllBytes($Path))
    return Get-NormalizedTextSha256 -Text $content
}

function Normalize-RecordedHash {
    param([string]$Hash)

    $value = [string]$Hash
    if ([string]::IsNullOrWhiteSpace($value)) {
        return ""
    }

    return $value.Trim().Replace("sha256:", "")
}

function Test-PlatformMetadataPath {
    param([string]$FilePath)

    $normalized = $FilePath.Replace("\", "/")
    return $normalized -eq "docs/storefront-analysis/metadata.yaml" `
        -or $normalized -eq "StorefrontPackageVersions.props" `
        -or $normalized -eq "starter-generation.contract.yaml"
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

    if ($Scope -eq "foundation") {
        return Test-PlatformMetadataPath -FilePath $Entry["filePath"]
    }

    if ($Scope -eq "css") {
        return $Entry["scope"] -eq "css"
    }

    if ($Scope -eq "page" -or $Scope -eq "component") {
        if ($Entry["scope"] -ne $Scope) {
            return $false
        }

        return [string]::IsNullOrWhiteSpace($Target) -or (Test-TextContains $Entry["filePath"] $Target ([System.StringComparison]::OrdinalIgnoreCase))
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
        elseif ($Scope -eq "foundation" -and (Test-PlatformMetadataPath -FilePath $filePath)) {
            if ($candidateHash -eq "none") {
                $action = "skip protected"
                $reason = "Current Starter candidate does not produce this platform file."
            }
            elseif ($targetHash -eq $candidateHash) {
                $reason = "Platform metadata already matches the current Starter/template source."
            }
            else {
                $action = "platform metadata update"
                $reason = "Explicit foundation update refreshes generated metadata, package compatibility metadata, or Starter contract copy."
            }
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
                $action = "conflict manual edit"
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
            changed = ($action -eq "create" -or $action -eq "update" -or $action -eq "platform metadata update")
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
    $platformUpdates = @($Plan | Where-Object { $_["action"] -eq "platform metadata update" })
    $skipped = @($Plan | Where-Object { $_["action"].StartsWith("skip", [System.StringComparison]::Ordinal) })
    $conflicts = @($Plan | Where-Object { $_["action"].StartsWith("conflict", [System.StringComparison]::Ordinal) })
    $obsolete = @($Plan | Where-Object { $_["action"] -eq "obsolete candidate" })

    $lines = [System.Collections.Generic.List[string]]::new()
    $lines.Add("# StorefrontBuilder Regeneration Report")
    $lines.Add("")
    $lines.Add('- Regenerate all generated files: supported by `regenerate-storefront.ps1 -Scope all`.')
    $lines.Add('- Regenerate one page: supported by `-Scope page -Target <path>`.')
    $lines.Add('- Regenerate one component: supported by `-Scope component -Target <path>`.')
    $lines.Add('- Regenerate only CSS tokens: supported by `-Scope css`.')
    $lines.Add('- Update platform metadata/package contract files: supported by `-Scope foundation`.')
    $lines.Add('- Validate without writing: supported by `-WhatIf` or `-Scope validate`.')
    $lines.Add('- Show conflict report: supported by `-Scope conflicts`.')
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
    $lines.Add("## Platform Metadata Updates")
    $lines.Add("")
    Add-PlanLines -Lines $lines -Items $platformUpdates
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
    $nextAction = if ($conflicts.Count -gt 0) { 'Resolve conflicts manually, then rerun `-Scope conflicts`.' } else { "No conflicts; rerun validation or build proof as needed." }
    $lines.Add($nextAction)

    Set-Content -LiteralPath $ReportPath -Value ($lines -join [Environment]::NewLine) -Encoding UTF8
}

function Test-MeaningfulWhatIfAction {
    param([hashtable]$Item)

    $action = [string]$Item["action"]
    return $action -eq "create" `
        -or $action -eq "update" `
        -or $action -eq "platform metadata update" `
        -or $action -eq "obsolete candidate" `
        -or $action.StartsWith("conflict", [System.StringComparison]::Ordinal) `
        -or $action -eq "skip protected" `
        -or $action -eq "skip user-owned"
}

function Write-RegenerationPlanSummary {
    param(
        [System.Collections.Generic.List[hashtable]]$Plan,
        [string]$StableReportPath
    )

    $creates = @($Plan | Where-Object { $_["action"] -eq "create" })
    $updates = @($Plan | Where-Object { $_["action"] -eq "update" })
    $platformUpdates = @($Plan | Where-Object { $_["action"] -eq "platform metadata update" })
    $conflicts = @($Plan | Where-Object { ([string]$_["action"]).StartsWith("conflict", [System.StringComparison]::Ordinal) })
    $obsolete = @($Plan | Where-Object { $_["action"] -eq "obsolete candidate" })
    $preservedSkips = @($Plan | Where-Object { $_["action"] -eq "skip protected" -or $_["action"] -eq "skip user-owned" })
    $meaningful = @($Plan | Where-Object { Test-MeaningfulWhatIfAction -Item $_ })

    Write-Host "WhatIf completed without writing generated project files."
    Write-Host "WhatIf report: $StableReportPath"
    Write-Host "WhatIf summary: create=$($creates.Count); update=$($updates.Count); platformMetadataUpdate=$($platformUpdates.Count); conflict=$($conflicts.Count); obsolete=$($obsolete.Count); protectedOrUserOwnedSkip=$($preservedSkips.Count)"

    if ($meaningful.Count -eq 0) {
        Write-Host "WhatIf plan: no-op; every file is unchanged, out-of-scope, or already aligned."
    }
    else {
        Write-Host "WhatIf actions:"
        foreach ($item in $meaningful) {
            Write-Host "- $($item["filePath"]): $($item["action"]) - $($item["reason"])"
        }
    }

    if ($conflicts.Count -gt 0) {
        Write-Host "WhatIf next action: resolve conflicts manually, rerun -Scope conflicts, then rerun the desired update scope."
    }
}

function Test-StorefrontBuilderPathUnderRoot {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$Root
    )

    $resolvedPath = [System.IO.Path]::GetFullPath($Path)
    $resolvedRoot = [System.IO.Path]::GetFullPath($Root).TrimEnd([System.IO.Path]::DirectorySeparatorChar, [System.IO.Path]::AltDirectorySeparatorChar)
    $rootWithSeparator = "$resolvedRoot$([System.IO.Path]::DirectorySeparatorChar)"

    return $resolvedPath.Equals($resolvedRoot, [System.StringComparison]::OrdinalIgnoreCase) `
        -or $resolvedPath.StartsWith($rootWithSeparator, [System.StringComparison]::OrdinalIgnoreCase)
}

function Resolve-WhatIfReportPath {
    param(
        [string]$RequestedPath,
        [string]$OutputRoot,
        [string]$ProjectRoot,
        [string]$ProjectName,
        [string]$OperationId
    )

    $reportPath = if ([string]::IsNullOrWhiteSpace($RequestedPath)) {
        Join-Path $OutputRoot ".regeneration-reports\$ProjectName-$OperationId.md"
    }
    elseif ([System.IO.Path]::IsPathRooted($RequestedPath)) {
        [System.IO.Path]::GetFullPath($RequestedPath)
    }
    else {
        [System.IO.Path]::GetFullPath((Join-Path (Get-Location) $RequestedPath))
    }

    $reportPath = [System.IO.Path]::GetFullPath($reportPath)
    $targetRoot = [System.IO.Path]::GetFullPath($ProjectRoot)
    if (Test-StorefrontBuilderPathUnderRoot -Path $reportPath -Root $targetRoot) {
        throw "[SFB-REGEN-020] WhatIfReportPath must not be under the generated target project. Problem: report path '$reportPath' would mutate target-owned files during -WhatIf. Cause: -WhatIfReportPath points inside '$targetRoot'. Fix: use the default report path or choose a path under obj, artifacts/storefront-builder, or the output root report folder."
    }

    $allowedRoots = @(
        (Join-Path $OutputRoot ".regeneration-reports"),
        (Join-Path $repoRoot "obj"),
        (Join-Path $repoRoot "artifacts\storefront-builder")
    ) | ForEach-Object { [System.IO.Path]::GetFullPath($_) }

    $isAllowed = $false
    foreach ($allowedRoot in $allowedRoots) {
        if (Test-StorefrontBuilderPathUnderRoot -Path $reportPath -Root $allowedRoot) {
            $isAllowed = $true
            break
        }
    }

    if (-not $isAllowed) {
        throw "[SFB-REGEN-021] WhatIfReportPath must stay under an approved StorefrontBuilder report root. Problem: '$reportPath' is outside the output report folder, repo obj, and artifacts/storefront-builder. Cause: custom report paths may otherwise write arbitrary files. Fix: omit -WhatIfReportPath or choose a path under '$OutputRoot\.regeneration-reports', 'obj', or 'artifacts\storefront-builder'."
    }

    $parent = Split-Path -Parent $reportPath
    if ([string]::IsNullOrWhiteSpace($parent)) {
        throw "[SFB-REGEN-022] WhatIfReportPath must include a file name. Problem: '$reportPath' has no parent directory. Cause: report path resolved to an invalid file location. Fix: pass a full markdown file path."
    }

    foreach ($allowedRoot in $allowedRoots) {
        if ($parent.StartsWith($allowedRoot, [System.StringComparison]::OrdinalIgnoreCase) -or $parent.Equals($allowedRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
            New-Item -ItemType Directory -Force -Path $parent | Out-Null
            return $reportPath
        }
    }

    throw "[SFB-REGEN-023] WhatIfReportPath parent is not approved. Problem: '$parent' cannot be created safely. Cause: path validation did not match an approved report root. Fix: choose a report path under the output report folder, obj, or artifacts/storefront-builder."
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

function Remove-DirectoryIfExists {
    param([string]$Path)

    try {
        if ([System.IO.Directory]::Exists($Path)) {
            [System.IO.Directory]::Delete($Path, $true)
        }
    }
    catch {
        Write-Verbose "Ignoring cleanup failure for '$Path': $($_.Exception.Message)"
    }
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
        generationMode = Read-SimpleYamlValue -Text $text -Key "generationMode" -Default ""
        starterContractSha256 = Read-SimpleYamlValue -Text $text -Key "starterContractSha256" -Default ""
        planSha256 = Read-SimpleYamlValue -Text $text -Key "planSha256" -Default ""
        sourceHandoffPackageHash = Read-SimpleYamlValue -Text $text -Key "sourceHandoffPackageHash" -Default ""
        sourceHandoffReadinessHash = Read-SimpleYamlValue -Text $text -Key "sourceHandoffReadinessHash" -Default ""
        sourceStarterContractHash = Read-SimpleYamlValue -Text $text -Key "sourceStarterContractHash" -Default ""
    }
}

function Test-HandoffGeneratedStorefront {
    param(
        [hashtable]$Metadata,
        [string]$ProjectRoot
    )

    return $Metadata["generationMode"] -eq "handoff-project-skeleton" `
        -or (Test-Path -LiteralPath (Join-Path $ProjectRoot "docs\storefront-analysis\generation-plan.json"))
}

function Read-HandoffGenerationPlan {
    param([Parameter(Mandatory = $true)][string]$ProjectRoot)

    $planPath = Join-Path $ProjectRoot "docs\storefront-analysis\generation-plan.json"
    if (-not (Test-Path -LiteralPath $planPath)) {
        throw "[SFB-REGEN-HANDOFF-001] Handoff generation plan is missing. Problem: '$planPath' does not exist. Cause: handoff regeneration requires the compiled generation plan stored with the generated project. Fix: rerun handoff generation or restore docs/storefront-analysis/generation-plan.json."
    }

    $planText = Get-Content -LiteralPath $planPath -Raw
    return @{
        path = $planPath
        text = $planText
        hash = Get-NormalizedTextSha256 -Text $planText
        json = $planText | ConvertFrom-Json
    }
}

function Get-HandoffPlannedIntentionalChanges {
    param([Parameter(Mandatory = $true)]$Plan)

    $paths = [System.Collections.Generic.List[string]]::new()
    foreach ($file in @($Plan.files)) {
        $ownership = [string]$file.ownership
        $actionValue = $file.allowedOperation
        if ($null -eq $actionValue) {
            $actionValue = $file.action
        }

        $action = [string]$actionValue
        $targetPath = [string]$file.targetPath
        if ([string]::IsNullOrWhiteSpace($targetPath)) {
            continue
        }

        if ($ownership -eq "protected" -or $action -eq "skip") {
            continue
        }

        $paths.Add($targetPath.Replace("\", "/").TrimStart("/"))
    }

    $paths.Add("Components/Layout/ApplicationHead.razor")
    return @($paths | Sort-Object -Unique)
}

function Assert-HandoffPlanTargetsAreRegeneratable {
    param([Parameter(Mandatory = $true)]$Plan)

    foreach ($file in @($Plan.files)) {
        $targetPath = [string]$file.targetPath
        if ([string]::IsNullOrWhiteSpace($targetPath)) {
            continue
        }

        $normalized = $targetPath.Replace("\", "/").TrimStart("/")
        $ownership = [string]$file.ownership
        $actionValue = $file.allowedOperation
        if ($null -eq $actionValue) {
            $actionValue = $file.action
        }

        $action = [string]$actionValue
        $protectedTarget = $normalized -eq "StorefrontPackageVersions.props" `
            -or $normalized -eq "starter-generation.contract.yaml" `
            -or (Test-TextContains $normalized "BlazorShop.Storefront.Starter" ([System.StringComparison]::OrdinalIgnoreCase)) `
            -or (Test-TextContains $normalized "BlazorShop.Storefront.Presentation" ([System.StringComparison]::OrdinalIgnoreCase)) `
            -or (Test-TextContains $normalized "BlazorShop.Storefront.Runtime" ([System.StringComparison]::OrdinalIgnoreCase)) `
            -or (Test-TextContains $normalized "BlazorShop.Storefront.Client" ([System.StringComparison]::OrdinalIgnoreCase)) `
            -or (Test-TextContains $normalized "BlazorShop.Storefront.V2" ([System.StringComparison]::OrdinalIgnoreCase))

        if ($protectedTarget -and $ownership -ne "protected" -and $action -ne "skip") {
            throw "[SFB-REGEN-HANDOFF-020] Handoff generation plan targets a protected file. Problem: '$normalized' is planned as '$ownership/$action'. Cause: handoff regeneration can only rewrite generated-owned visual files; platform/package files require an explicit foundation path. Fix: re-plan the handoff generation so protected targets are skipped or run a reviewed foundation upgrade."
        }
    }
}

function Assert-HandoffRegenerationState {
    param(
        [hashtable]$Metadata,
        [string]$ProjectRoot
    )

    if (-not (Test-HandoffGeneratedStorefront -Metadata $Metadata -ProjectRoot $ProjectRoot)) {
        return $null
    }

    $planInfo = Read-HandoffGenerationPlan -ProjectRoot $ProjectRoot
    $plan = $planInfo["json"]
    if ($plan.generationMode -ne "handoff") {
        throw "[SFB-REGEN-HANDOFF-002] Handoff regeneration requires a handoff generation plan. Problem: generationMode is '$($plan.generationMode)'. Cause: this target is marked as handoff-generated but the stored plan is not a handoff plan. Fix: restore the matching handoff generation plan or regenerate from the reviewed handoff package."
    }

    $recordedPlanHash = Normalize-RecordedHash -Hash $Metadata["planSha256"]
    if (-not [string]::IsNullOrWhiteSpace($recordedPlanHash) -and $recordedPlanHash -ne $planInfo["hash"]) {
        throw "[SFB-REGEN-HANDOFF-003] Handoff generation plan hash drift requires explicit re-plan/update. Problem: metadata records '$recordedPlanHash' but generation-plan.json hashes to '$($planInfo["hash"])'. Cause: the generation plan changed after project generation. Fix: rerun handoff planning/generation from the reviewed package before regeneration."
    }

    $recordedPackageHash = Normalize-RecordedHash -Hash $Metadata["sourceHandoffPackageHash"]
    $planPackageHash = Normalize-RecordedHash -Hash $plan.sourceHandoffPackageHash
    if (-not [string]::IsNullOrWhiteSpace($recordedPackageHash) -and $recordedPackageHash -ne $planPackageHash) {
        throw "[SFB-REGEN-HANDOFF-010] Handoff package hash drift requires explicit re-plan/update. Problem: metadata records package '$recordedPackageHash' but the generation plan records '$planPackageHash'. Cause: handoff source identity changed without a new generation plan. Fix: rerun handoff preflight and planning before regeneration."
    }

    $recordedReadinessHash = Normalize-RecordedHash -Hash $Metadata["sourceHandoffReadinessHash"]
    $planReadinessHash = Normalize-RecordedHash -Hash $plan.sourceHandoffReadinessHash
    if (-not [string]::IsNullOrWhiteSpace($recordedReadinessHash) -and $recordedReadinessHash -ne $planReadinessHash) {
        throw "[SFB-REGEN-HANDOFF-010] Handoff readiness hash drift requires explicit re-plan/update. Problem: metadata records readiness '$recordedReadinessHash' but the generation plan records '$planReadinessHash'. Cause: handoff readiness changed without a new generation plan. Fix: rerun handoff preflight and planning before regeneration."
    }

    $recordedStarterHash = Normalize-RecordedHash -Hash $Metadata["sourceStarterContractHash"]
    $planStarterHash = Normalize-RecordedHash -Hash $plan.sourceStarterContractHash
    if (-not [string]::IsNullOrWhiteSpace($recordedStarterHash) -and $recordedStarterHash -ne $planStarterHash) {
        throw "[SFB-REGEN-HANDOFF-011] Starter contract drift requires an explicit foundation upgrade. Problem: metadata records Starter contract '$recordedStarterHash' but the generation plan records '$planStarterHash'. Cause: platform foundation identity changed without a reviewed foundation update. Fix: run a foundation regeneration/upgrade path after reviewing Starter contract changes."
    }

    $starterContractPath = Join-Path $repoRoot "BlazorShop.PresentationV2\BlazorShop.Storefront.Starter\starter-generation.contract.yaml"
    $currentStarterHash = Get-NormalizedFileSha256 -Path $starterContractPath
    if (-not [string]::IsNullOrWhiteSpace($planStarterHash) -and $currentStarterHash -ne $planStarterHash) {
        throw "[SFB-REGEN-HANDOFF-011] Starter contract drift requires an explicit foundation upgrade. Problem: current Starter contract hashes to '$currentStarterHash' but the handoff plan records '$planStarterHash'. Cause: the shared Starter foundation changed after this handoff project was generated. Fix: run a reviewed foundation upgrade/re-plan before regenerating visual files."
    }

    Assert-HandoffPlanTargetsAreRegeneratable -Plan $plan
    return $planInfo
}

function Get-YamlTopLevelBlock {
    param(
        [string[]]$Lines,
        [string]$Key
    )

    for ($index = 0; $index -lt $Lines.Count; $index++) {
        if ($Lines[$index] -match "^$([regex]::Escape($Key)):\s*") {
            $end = $index + 1
            while ($end -lt $Lines.Count -and ($Lines[$end] -match "^\s+" -or [string]::IsNullOrWhiteSpace($Lines[$end]))) {
                $end++
            }

            return @{
                Start = $index
                End = $end
                Lines = @($Lines[$index..($end - 1)])
            }
        }
    }

    return $null
}

function Set-YamlTopLevelBlock {
    param(
        [string]$BaseText,
        [string]$SourceText,
        [string]$Key
    )

    $baseLines = @($BaseText -split "\r?\n")
    $sourceLines = @($SourceText -split "\r?\n")
    if ($baseLines.Count -gt 0 -and $baseLines[$baseLines.Count - 1] -eq "") {
        $baseLines = if ($baseLines.Count -gt 1) { @($baseLines[0..($baseLines.Count - 2)]) } else { @() }
    }
    if ($sourceLines.Count -gt 0 -and $sourceLines[$sourceLines.Count - 1] -eq "") {
        $sourceLines = if ($sourceLines.Count -gt 1) { @($sourceLines[0..($sourceLines.Count - 2)]) } else { @() }
    }

    $baseBlock = Get-YamlTopLevelBlock -Lines $baseLines -Key $Key
    $sourceBlock = Get-YamlTopLevelBlock -Lines $sourceLines -Key $Key
    if ($null -eq $sourceBlock) {
        return $BaseText
    }

    if ($null -eq $baseBlock) {
        return (($baseLines + $sourceBlock.Lines) -join "`n") + "`n"
    }

    $merged = [System.Collections.Generic.List[string]]::new()
    for ($index = 0; $index -lt $baseBlock.Start; $index++) {
        $merged.Add($baseLines[$index])
    }
    foreach ($line in $sourceBlock.Lines) {
        $merged.Add($line)
    }
    for ($index = $baseBlock.End; $index -lt $baseLines.Count; $index++) {
        $merged.Add($baseLines[$index])
    }

    return ($merged -join "`n") + "`n"
}

function Update-CandidateFoundationMetadata {
    param(
        [Parameter(Mandatory = $true)][string]$TargetMetadataPath,
        [Parameter(Mandatory = $true)][string]$CandidateMetadataPath
    )

    $targetText = Get-Content -LiteralPath $TargetMetadataPath -Raw
    $candidateText = Get-Content -LiteralPath $CandidateMetadataPath -Raw
    $merged = $targetText
    foreach ($key in @(
        "generatorVersion",
        "storefrontContractSha256",
        "storefrontContractPath",
        "sourceStarterVersion",
        "starterContractVersion",
        "packageVersions",
        "updatedUtc"
    )) {
        $merged = Set-YamlTopLevelBlock -BaseText $merged -SourceText $candidateText -Key $key
    }

    Set-Content -LiteralPath $CandidateMetadataPath -Value $merged -Encoding UTF8
}

function Invoke-RegenerationCandidateGeneration {
    param(
        [Parameter(Mandatory = $true)][string]$CandidateOutputRoot,
        [Parameter(Mandatory = $true)][string]$ProjectName,
        [Parameter(Mandatory = $true)][string]$StoreKey,
        [Parameter(Mandatory = $true)][string]$TargetProjectRoot,
        [hashtable]$HandoffPlanInfo
    )

    if ($null -ne $HandoffPlanInfo) {
        New-Item -ItemType Directory -Force -Path $CandidateOutputRoot | Out-Null
        Copy-Item -LiteralPath $TargetProjectRoot -Destination $CandidateOutputRoot -Recurse -Force
        $candidateProjectRoot = Join-Path $CandidateOutputRoot $ProjectName
        $candidatePlanPath = Join-Path $candidateProjectRoot "docs\storefront-analysis\generation-plan.json"
        & node "$PSScriptRoot/scripts/generate/apply-handoff-project-skeleton.mjs" `
            --project-root $candidateProjectRoot `
            --plan-json $candidatePlanPath `
            --regeneration-candidate
        if ($LASTEXITCODE -ne 0) {
            throw "[SFB-REGEN-HANDOFF-030] Failed to apply handoff generation plan to regeneration candidate."
        }

        $intentionalChanges = Get-HandoffPlannedIntentionalChanges -Plan $HandoffPlanInfo["json"]
        $intentionalChangesArgument = [string]::Join(",", $intentionalChanges)
        & node "$PSScriptRoot/scripts/generate/update-generated-files-manifest.mjs" `
            --project-root $candidateProjectRoot `
            --intentional-changes $intentionalChangesArgument
        if ($LASTEXITCODE -ne 0) {
            throw "[SFB-REGEN-HANDOFF-031] Failed to update handoff regeneration candidate manifest."
        }

        return
    }

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

function Remove-CandidateManifestEntries {
    param(
        [Parameter(Mandatory = $true)][string]$ManifestPath,
        [Parameter(Mandatory = $true)][string[]]$FilePaths
    )

    if (-not (Test-Path -LiteralPath $ManifestPath)) {
        return
    }

    $normalizedPaths = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
    foreach ($filePath in $FilePaths) {
        if (-not [string]::IsNullOrWhiteSpace($filePath)) {
            [void]$normalizedPaths.Add($filePath.Replace("\", "/"))
        }
    }

    if ($normalizedPaths.Count -eq 0) {
        return
    }

    $lines = [System.Collections.Generic.List[string]]::new()
    $currentBlock = [System.Collections.Generic.List[string]]::new()
    $skipBlock = $false

    $flushBlock = {
        if ($currentBlock.Count -gt 0 -and -not $skipBlock) {
            foreach ($line in $currentBlock) {
                $lines.Add($line)
            }
        }

        $currentBlock.Clear()
        $skipBlock = $false
    }

    foreach ($line in [System.IO.File]::ReadAllLines($ManifestPath)) {
        if ($line -match '^\s+- filePath:\s*(.+?)\s*$') {
            & $flushBlock
            $currentBlock.Add($line)
            $pathValue = $Matches[1].Trim().Trim('"')
            $skipBlock = $normalizedPaths.Contains($pathValue.Replace("\", "/"))
            continue
        }

        if ($currentBlock.Count -gt 0) {
            $currentBlock.Add($line)
            continue
        }

        $lines.Add($line)
    }

    & $flushBlock
    [System.IO.File]::WriteAllLines($ManifestPath, $lines, [System.Text.Encoding]::UTF8)
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
$stableWhatIfReportPath = Resolve-WhatIfReportPath -RequestedPath $WhatIfReportPath -OutputRoot $resolvedOutputRoot -ProjectRoot $resolvedProjectRoot -ProjectName $projectName -OperationId $operationId
$backupRoot = Join-Path $resolvedOutputRoot ".regeneration-backup\$projectName-$operationId"
$preserveCandidateArtifacts = $env:SFB_KEEP_REGENERATION_CANDIDATE_ARTIFACTS -eq "1"
$candidateDropPaths = @()
if (-not [string]::IsNullOrWhiteSpace($env:SFB_DROP_CANDIDATE_FILE_PATHS)) {
    $candidateDropPaths = @($env:SFB_DROP_CANDIDATE_FILE_PATHS -split "," | ForEach-Object { $_.Trim().Replace("\", "/") } | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
}
$validationResult = "not-requested"
$buildResult = "not-requested"
$plan = [System.Collections.Generic.List[hashtable]]::new()
$handoffPlanInfo = Assert-HandoffRegenerationState -Metadata $metadata -ProjectRoot $resolvedProjectRoot

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
    Invoke-RegenerationCandidateGeneration -CandidateOutputRoot $candidateOutputRoot -ProjectName $projectName -StoreKey $storeKey -TargetProjectRoot $resolvedProjectRoot -HandoffPlanInfo $handoffPlanInfo

    foreach ($dropPath in $candidateDropPaths) {
        $resolvedDropPath = Join-Path $candidateProjectRoot $dropPath
        if (Test-Path -LiteralPath $resolvedDropPath) {
            Remove-Item -LiteralPath $resolvedDropPath -Force
        }
    }

    $candidateMetadataPath = Join-Path $candidateProjectRoot "docs\storefront-analysis\metadata.yaml"
    if ($Scope -eq "foundation" -and (Test-Path -LiteralPath $metadataPath) -and (Test-Path -LiteralPath $candidateMetadataPath)) {
        Update-CandidateFoundationMetadata -TargetMetadataPath $metadataPath -CandidateMetadataPath $candidateMetadataPath
        node "$PSScriptRoot/scripts/generate/update-generated-files-manifest.mjs" --project-root $candidateProjectRoot --intentional-changes "docs/storefront-analysis/metadata.yaml"
        if ($LASTEXITCODE -ne 0) { throw "[SFB-REGEN-014] Failed to prepare foundation metadata in regeneration candidate." }
    }
    elseif ($null -eq $handoffPlanInfo -and $Scope -ne "foundation" -and (Test-Path -LiteralPath $metadataPath)) {
        New-Item -ItemType Directory -Force -Path (Split-Path -Parent $candidateMetadataPath) | Out-Null
        Copy-Item -LiteralPath $metadataPath -Destination $candidateMetadataPath -Force
        node "$PSScriptRoot/scripts/generate/update-generated-files-manifest.mjs" --project-root $candidateProjectRoot
        if ($LASTEXITCODE -ne 0) { throw "[SFB-REGEN-013] Failed to preserve target metadata in regeneration candidate." }
    }

    if ($candidateDropPaths.Count -gt 0) {
        $candidateManifestPath = Join-Path $candidateProjectRoot "docs\storefront-analysis\generated-files.yaml"
        Remove-CandidateManifestEntries -ManifestPath $candidateManifestPath -FilePaths $candidateDropPaths
    }

    $originalEntries = Read-GeneratedFileManifestEntries -ManifestPath $manifestPath
    $candidateEntries = Read-GeneratedFileManifestEntries -ManifestPath (Join-Path $candidateProjectRoot "docs\storefront-analysis\generated-files.yaml")
    $plan = New-RegenerationPlan -TargetProjectRoot $resolvedProjectRoot -CandidateProjectRoot $candidateProjectRoot -TargetEntries $originalEntries -CandidateEntries $candidateEntries -Scope $Scope -Target $Target

    $commandLabel = if ($WhatIf) { "regenerate-storefront.ps1 -WhatIf" } else { "regenerate-storefront.ps1" }
    Write-RegenerationReport -ReportPath $candidateReportPath -Command $commandLabel -Scope $Scope -Target $Target -Plan $plan -ValidationResult $validationResult -BuildResult $buildResult

    if ($WhatIf) {
        Copy-Item -LiteralPath $candidateReportPath -Destination $stableWhatIfReportPath -Force
        Write-RegenerationPlanSummary -Plan $plan -StableReportPath $stableWhatIfReportPath
        exit 0
    }

    New-Item -ItemType Directory -Force -Path (Split-Path -Parent $backupRoot) | Out-Null
    Copy-Item -LiteralPath $resolvedProjectRoot -Destination $backupRoot -Recurse -Force

    foreach ($item in $plan | Where-Object { $_["changed"] -eq $true }) {
        Copy-ChangedFile -SourceRoot $candidateProjectRoot -TargetRoot $resolvedProjectRoot -FilePath $item["filePath"]
    }

    if ($ValidateAfterApply) {
        & "$PSScriptRoot/validate-storefront.ps1" -ProjectRoot $resolvedProjectRoot -SkipIdempotency
        if ($LASTEXITCODE -ne 0) {
            $validationResult = "failed"
            throw "[SFB-REGEN-011] Post-regeneration validation failed."
        }

        $validationResult = "passed"
    }

    if ($BuildAfterApply) {
        $projectFile = Join-Path $resolvedProjectRoot "$projectName.csproj"
        dotnet build $projectFile
        if ($LASTEXITCODE -ne 0) {
            $buildResult = "failed"
            throw "[SFB-REGEN-010] Post-regeneration build failed."
        }

        $buildResult = "passed"
    }

    $intentionalChanges = @($plan | Where-Object { $_["changed"] -eq $true } | ForEach-Object { $_["filePath"] })
    $intentionalChangesArgument = [string]::Join(",", $intentionalChanges)
    node "$PSScriptRoot/scripts/generate/update-generated-files-manifest.mjs" --project-root $resolvedProjectRoot --intentional-changes $intentionalChangesArgument
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
    if (-not $preserveCandidateArtifacts) {
        Remove-DirectoryIfExists -Path $candidateOutputRoot
    }

    Remove-DirectoryIfExists -Path $backupRoot
}

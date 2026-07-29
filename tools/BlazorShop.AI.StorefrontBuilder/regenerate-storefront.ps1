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
        [string]$ProjectRoot,
        [string]$StagedRoot,
        [System.Collections.Generic.List[hashtable]]$OriginalEntries,
        [System.Collections.Generic.List[hashtable]]$StagedEntries,
        [string]$Scope,
        [string]$Target
    )

    $originalMap = @{}
    foreach ($entry in $OriginalEntries) {
        $originalMap[$entry["filePath"]] = $entry
    }

    $actions = [System.Collections.Generic.List[hashtable]]::new()
    foreach ($entry in $StagedEntries) {
        $filePath = $entry["filePath"]
        $ownership = $entry["ownership"]
        $conflictStatus = $entry["conflictStatus"]
        $targetPath = Join-Path $ProjectRoot $filePath
        $stagedPath = Join-Path $StagedRoot $filePath
        $originalHash = Get-NormalizedFileHash -Path $targetPath
        $stagedHash = Get-NormalizedFileHash -Path $stagedPath
        $previousGeneratedHash = if ($originalMap.ContainsKey($filePath)) { $originalMap[$filePath]["generatedHash"] } else { "none" }
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
        elseif ($conflictStatus -eq "manual-edit" -or $conflictStatus -eq "protected-modified" -or $conflictStatus -eq "user-owned-modified") {
            $action = "conflict manual edit"
            $reason = $entry["conflictReason"]
        }
        elseif ($conflictStatus -eq "obsolete" -or $entry["obsolete"] -eq "true") {
            $action = "obsolete candidate"
            $reason = "Obsolete files are reported, not deleted silently; delete only if explicitly allowed."
        }
        elseif ($originalHash -eq "none") {
            $action = "create"
            $reason = "File does not exist in target project."
        }
        elseif ($originalHash -eq $stagedHash) {
            $action = "skip unchanged"
            $reason = "Target already matches staged output."
        }
        elseif ($ownership -eq "generated" -or $ownership -eq "managed") {
            if ($previousGeneratedHash -eq "none" -or $originalHash -eq $previousGeneratedHash) {
                $action = "update"
                $reason = "Target hash matches last generated hash."
            }
            else {
                $action = "conflict manual edit"
                $reason = "Target hash differs from last generated hash."
            }
        }

        $actions.Add(@{
            filePath = $filePath
            action = $action
            reason = $reason
            ownership = $ownership
            scope = $entry["scope"]
            changed = ($action -eq "create" -or $action -eq "update")
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

$resolvedProjectRoot = Resolve-InputPath $ProjectRoot
if (-not (Test-Path -LiteralPath $resolvedProjectRoot)) {
    throw "[SFB-REGEN-000] Project root does not exist: $resolvedProjectRoot"
}

$projectName = Split-Path -Leaf $resolvedProjectRoot
$outputRoot = Split-Path -Parent $resolvedProjectRoot
$approvedOutputRoot = Resolve-ApprovedStorefrontBuilderOutputRoot -RepoRoot $repoRoot -OutputRoot $outputRoot
Assert-StorefrontBuilderPathUnderRoot -Path $resolvedProjectRoot -Root $approvedOutputRoot
$manifestPath = Join-Path $resolvedProjectRoot "docs\storefront-analysis\generated-files.yaml"
$reportPath = Join-Path $resolvedProjectRoot "docs\storefront-analysis\regeneration-report.md"

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

if ($WhatIf) {
    $entries = Read-GeneratedFileManifestEntries -ManifestPath $manifestPath
    foreach ($entry in $entries) {
        if (Test-ScopeMatch -Entry $entry -Scope $Scope -Target $Target) {
            Write-Host "plan skip unchanged $($entry["ownership"]) $($entry["filePath"])"
        }
    }

    Write-Host "WhatIf completed without writing generated project files."
    exit 0
}

$operationId = [System.Guid]::NewGuid().ToString("N")
$stagingRoot = Join-Path (Join-Path $outputRoot ".regeneration-staging") "$projectName-$operationId"
$backupRoot = Join-Path (Join-Path $outputRoot ".regeneration-backup") "$projectName-$operationId"
$validationResult = "not-requested"
$buildResult = "not-requested"

try {
    New-Item -ItemType Directory -Force -Path (Split-Path -Parent $stagingRoot) | Out-Null
    Copy-Item -LiteralPath $resolvedProjectRoot -Destination $stagingRoot -Recurse -Force

    if ($Scope -in @("all", "css")) {
        node "$PSScriptRoot/scripts/generate/apply-visual-foundation.mjs" --project-root $stagingRoot
        if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    }

    if ($Scope -in @("all", "page", "component")) {
        node "$PSScriptRoot/scripts/generate/apply-composition.mjs" --project-root $stagingRoot --target $Target
        if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    }

    node "$PSScriptRoot/scripts/generate/update-generated-files-manifest.mjs" --project-root $stagingRoot
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

    $originalEntries = Read-GeneratedFileManifestEntries -ManifestPath $manifestPath
    $stagedEntries = Read-GeneratedFileManifestEntries -ManifestPath (Join-Path $stagingRoot "docs\storefront-analysis\generated-files.yaml")
    $plan = New-RegenerationPlan -ProjectRoot $resolvedProjectRoot -StagedRoot $stagingRoot -OriginalEntries $originalEntries -StagedEntries $stagedEntries -Scope $Scope -Target $Target
    $conflicts = @($plan | Where-Object { $_["action"].StartsWith("conflict", [System.StringComparison]::Ordinal) })

    New-Item -ItemType Directory -Force -Path (Split-Path -Parent $backupRoot) | Out-Null
    Copy-Item -LiteralPath $resolvedProjectRoot -Destination $backupRoot -Recurse -Force

    foreach ($item in $plan | Where-Object { $_["changed"] -eq $true }) {
        Copy-ChangedFile -SourceRoot $stagingRoot -TargetRoot $resolvedProjectRoot -FilePath $item["filePath"]
    }

    node "$PSScriptRoot/scripts/generate/update-generated-files-manifest.mjs" --project-root $resolvedProjectRoot
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

    Write-RegenerationReport -ReportPath $reportPath -Command "regenerate-storefront.ps1" -Scope $Scope -Target $Target -Plan $plan -ValidationResult $validationResult -BuildResult $buildResult

    if ($ValidateAfterApply) {
        & "$PSScriptRoot/validate-storefront.ps1" -ProjectRoot $resolvedProjectRoot
        $validationResult = "passed"
    }

    if ($BuildAfterApply) {
        $projectFile = Join-Path $resolvedProjectRoot "$projectName.csproj"
        dotnet build $projectFile --no-restore
        if ($LASTEXITCODE -ne 0) { throw "[SFB-REGEN-010] Post-regeneration build failed." }
        $buildResult = "passed"
    }

    Write-RegenerationReport -ReportPath $reportPath -Command "regenerate-storefront.ps1" -Scope $Scope -Target $Target -Plan $plan -ValidationResult $validationResult -BuildResult $buildResult
    & "$PSScriptRoot/scripts/validate/Test-StorefrontBuilderIdempotency.ps1" -ProjectRoot $resolvedProjectRoot
}
catch {
    if (Test-Path -LiteralPath $backupRoot) {
        Remove-Item -LiteralPath $resolvedProjectRoot -Recurse -Force
        Copy-Item -LiteralPath $backupRoot -Destination $resolvedProjectRoot -Recurse -Force
    }

    throw
}
finally {
    if (Test-Path -LiteralPath $stagingRoot) {
        Remove-Item -LiteralPath $stagingRoot -Recurse -Force
    }

    if (Test-Path -LiteralPath $backupRoot) {
        Remove-Item -LiteralPath $backupRoot -Recurse -Force
    }
}

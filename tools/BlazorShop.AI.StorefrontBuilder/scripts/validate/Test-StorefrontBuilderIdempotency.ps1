param([Parameter(Mandatory = $true)][string]$ProjectRoot)

$ErrorActionPreference = "Stop"

function Read-GeneratedFileManifestEntries {
    param([Parameter(Mandatory = $true)][string]$Manifest)

    $entries = [System.Collections.Generic.List[hashtable]]::new()
    $current = $null
    foreach ($line in $Manifest -split "\r?\n") {
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

function Read-YamlScalarValue {
    param(
        [Parameter(Mandatory = $true)][string]$Text,
        [Parameter(Mandatory = $true)][string]$Key
    )

    $match = [regex]::Match($Text, "(?m)^$([regex]::Escape($Key)):\s*(\S+)\s*$")
    if (-not $match.Success) {
        return $null
    }

    return $match.Groups[1].Value.Trim('"')
}

$manifestPath = Join-Path $ProjectRoot "docs\storefront-analysis\generated-files.yaml"
$reportPath = Join-Path $ProjectRoot "docs\storefront-analysis\regeneration-report.md"
$metadataPath = Join-Path $ProjectRoot "docs\storefront-analysis\metadata.yaml"
foreach ($path in @($manifestPath, $reportPath, $metadataPath)) {
    if (-not (Test-Path $path)) {
        throw "[SFB-IDEMPOTENCY-000] Required regeneration artifact is missing: $path"
    }
}

$manifest = Get-Content -LiteralPath $manifestPath -Raw
$metadata = Get-Content -LiteralPath $metadataPath -Raw
$metadataGeneratorVersion = Read-YamlScalarValue -Text $metadata -Key "generatorVersion"
if ([string]::IsNullOrWhiteSpace($metadataGeneratorVersion)) {
    throw "[SFB-IDEMPOTENCY-012] metadata.yaml must contain generatorVersion."
}

$requiredFields = @(
    "filePath",
    "ownership",
    "capability",
    "scope",
    "generatorVersion",
    "sourceArtifactIds",
    "sourceSpecHash",
    "generatedHash",
    "currentHash",
    "lastGeneratedTimestamp",
    "manualEditDetected",
    "conflictStatus",
    "conflictReason",
    "protected",
    "obsolete",
    "templateVersion"
)

foreach ($field in $requiredFields) {
    if (-not $manifest.Contains($field, [System.StringComparison]::Ordinal)) {
        throw "[SFB-IDEMPOTENCY-001] Generated file manifest is missing '$field'."
    }
}

$entries = Read-GeneratedFileManifestEntries -Manifest $manifest
if ($entries.Count -eq 0) {
    throw "[SFB-IDEMPOTENCY-001] Generated file manifest must contain at least one file entry."
}

$validOwnership = @("generated", "managed", "user-owned", "protected", "artifact-only")
$seenPaths = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
$analysisRoot = Split-Path -Parent $manifestPath
$resolvedProjectRoot = [System.IO.Path]::GetFullPath($ProjectRoot)

foreach ($entry in $entries) {
    foreach ($field in $requiredFields) {
        if (-not $entry.ContainsKey($field) -or [string]::IsNullOrWhiteSpace($entry[$field])) {
            throw "[SFB-IDEMPOTENCY-001] Manifest entry '$($entry["filePath"])' is missing '$field'."
        }
    }

    $filePath = $entry["filePath"]
    if (-not $seenPaths.Add($filePath)) {
        throw "[SFB-IDEMPOTENCY-007] Duplicate generated file manifest entry: $filePath"
    }

    if ([System.IO.Path]::IsPathRooted($filePath) -or $filePath.Contains("..", [System.StringComparison]::Ordinal) -or $filePath.Contains(":", [System.StringComparison]::Ordinal)) {
        throw "[SFB-IDEMPOTENCY-008] Manifest file path must be project-relative and traversal-free: $filePath"
    }

    $resolvedFilePath = [System.IO.Path]::GetFullPath((Join-Path $resolvedProjectRoot $filePath))
    $rootWithSeparator = $resolvedProjectRoot.TrimEnd([System.IO.Path]::DirectorySeparatorChar, [System.IO.Path]::AltDirectorySeparatorChar) + [System.IO.Path]::DirectorySeparatorChar
    if (-not $resolvedFilePath.StartsWith($rootWithSeparator, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "[SFB-IDEMPOTENCY-008] Manifest file path resolves outside project root: $filePath"
    }

    $actualHash = Get-NormalizedFileHash -Path $resolvedFilePath
    if ($entry["currentHash"] -ne "none" -and $actualHash -ne $entry["currentHash"]) {
        if ($entry["ownership"] -eq "protected") {
            throw "[SFB-IDEMPOTENCY-002] Protected files must never be modified: $filePath"
        }

        throw "[SFB-IDEMPOTENCY-011] Manifest currentHash is stale for '$filePath'. Expected '$($entry["currentHash"])' but found '$actualHash'. Rerun conflict detection."
    }

    if ($validOwnership -notcontains $entry["ownership"]) {
        throw "[SFB-IDEMPOTENCY-006] Invalid ownership '$($entry["ownership"])' for '$filePath'."
    }

    if ($entry["generatorVersion"] -ne $metadataGeneratorVersion) {
        throw "[SFB-IDEMPOTENCY-012] Manifest generatorVersion '$($entry["generatorVersion"])' must match metadata.yaml generatorVersion '$metadataGeneratorVersion' for '$filePath'."
    }

    if ($entry["protected"] -eq "true" -and $entry["ownership"] -eq "generated") {
        throw "[SFB-IDEMPOTENCY-009] Protected file cannot be marked generated: $filePath"
    }

    $generatedHash = $entry["generatedHash"]
    $currentHash = $entry["currentHash"]
    $conflictStatus = $entry["conflictStatus"]
    if ($generatedHash -ne "none" -and $currentHash -ne "none" -and $generatedHash -ne $currentHash -and $conflictStatus -eq "none") {
        throw "[SFB-IDEMPOTENCY-003] Hash mismatch must produce a conflict status for '$filePath'."
    }

    if ($entry["ownership"] -eq "protected" -and $generatedHash -ne "none" -and $currentHash -ne "none" -and $generatedHash -ne $currentHash) {
        throw "[SFB-IDEMPOTENCY-002] Protected files must never be modified: $filePath"
    }

    $artifactIds = $entry["sourceArtifactIds"].Split(" ", [System.StringSplitOptions]::RemoveEmptyEntries)
    foreach ($artifactId in $artifactIds) {
        if ($artifactId -eq "none") {
            continue
        }

        $artifactPath = Join-Path $analysisRoot $artifactId
        if (-not (Test-Path $artifactPath)) {
            throw "[SFB-IDEMPOTENCY-005] Generated file manifest references missing source artifact '$artifactId'."
        }
    }
}

$report = Get-Content -LiteralPath $reportPath -Raw
foreach ($command in @("Scope all", "Scope page", "Scope component", "Scope css", "Scope validate", "Scope conflicts", "no unexpected file changes")) {
    if (-not $report.Contains($command, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "[SFB-IDEMPOTENCY-004] Regeneration report is missing command proof '$command'."
    }
}

foreach ($conflictEntry in $entries | Where-Object { $_["conflictStatus"] -ne "none" }) {
    if (-not $report.Contains($conflictEntry["filePath"], [System.StringComparison]::Ordinal)) {
        throw "[SFB-IDEMPOTENCY-010] Conflict report is missing '$($conflictEntry["filePath"])'."
    }
}

Write-Host "StorefrontBuilder idempotency validation passed for $ProjectRoot."

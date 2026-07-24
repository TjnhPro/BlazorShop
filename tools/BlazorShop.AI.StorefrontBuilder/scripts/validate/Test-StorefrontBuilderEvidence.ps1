param(
    [string]$AnalysisRoot
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($AnalysisRoot)) {
    throw "[SFB-EVIDENCE-000] AnalysisRoot is required."
}

$resolvedAnalysisRoot = [System.IO.Path]::GetFullPath($AnalysisRoot)
$evidenceRoot = Join-Path $resolvedAnalysisRoot "evidence"
$requiredFolders = @("home", "catalog", "product", "cart", "checkout", "account", "content", "shared")
$requiredMetadata = @(
    "url",
    "timestampUtc",
    "viewport",
    "browser",
    "screenshotFile",
    "domSnapshotFile",
    "computedStyleSampleFile",
    "assetListFile",
    "interactionState"
)

foreach ($folder in $requiredFolders) {
    $path = Join-Path $evidenceRoot $folder
    if (-not (Test-Path $path)) {
        throw "[SFB-EVIDENCE-001] Missing evidence folder '$folder' at '$path'."
    }
}

$evidenceFiles = Get-ChildItem -LiteralPath $evidenceRoot -Recurse -Filter "*.evidence.json" -File
$evidenceIds = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)

foreach ($file in $evidenceFiles) {
    $evidence = Get-Content -LiteralPath $file.FullName -Raw | ConvertFrom-Json
    $names = $evidence.PSObject.Properties.Name
    foreach ($field in $requiredMetadata) {
        if ($field -notin $names) {
            throw "[SFB-EVIDENCE-002] Evidence '$($file.FullName)' is missing metadata field '$field'."
        }
    }

    if ("evidenceId" -notin $names) {
        throw "[SFB-EVIDENCE-003] Evidence '$($file.FullName)' is missing metadata field 'evidenceId'."
    }

    $evidenceIds.Add([string]$evidence.evidenceId) | Out-Null
    foreach ($assetField in @("screenshotFile", "domSnapshotFile", "computedStyleSampleFile", "assetListFile")) {
        $assetPath = Join-Path $file.DirectoryName ([string]$evidence.$assetField)
        if (-not (Test-Path $assetPath)) {
            throw "[SFB-EVIDENCE-004] Evidence '$($file.FullName)' references missing '$assetField' file '$assetPath'."
        }
    }
}

$artifactFiles = Get-ChildItem -LiteralPath $resolvedAnalysisRoot -Recurse -Filter "*.json" -File |
    Where-Object { $_.FullName -notmatch "\\evidence\\" }

foreach ($artifactFile in $artifactFiles) {
    $artifact = Get-Content -LiteralPath $artifactFile.FullName -Raw | ConvertFrom-Json
    if ("evidenceIds" -in $artifact.PSObject.Properties.Name) {
        foreach ($evidenceId in $artifact.evidenceIds) {
            if (-not $evidenceIds.Contains([string]$evidenceId)) {
                throw "[SFB-EVIDENCE-005] Artifact '$($artifactFile.FullName)' references missing evidence ID '$evidenceId'."
            }
        }
    }
}

Write-Host "StorefrontBuilder evidence validation passed for $AnalysisRoot."

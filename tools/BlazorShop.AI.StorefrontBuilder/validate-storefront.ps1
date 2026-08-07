param(
    [string]$WorkspaceRoot = "",
    [string]$ProjectRoot = "",
    [string]$Name = "",
    [string]$StoreKey = "",
    [switch]$SkipIdempotency
)

$ErrorActionPreference = "Stop"
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
. (Join-Path $PSScriptRoot "scripts\generate\StorefrontBuilderProjectSafety.ps1")

$workspacePaths = Resolve-StorefrontBuilderWorkspacePaths -RepoRoot $repoRoot -ProjectName $Name -WorkspaceRoot $WorkspaceRoot -ProjectRoot $ProjectRoot -WarnOnProjectRootAlias
$resolvedProjectRoot = $workspacePaths.WorkspaceRoot
if (-not (Test-Path $resolvedProjectRoot)) {
    throw "[SFB-VALIDATE-000] Workspace root does not exist: $resolvedProjectRoot"
}

if ([string]::IsNullOrWhiteSpace($Name)) {
    $Name = $workspacePaths.ProjectName
}

if ([string]::IsNullOrWhiteSpace($Name)) {
    $projectFile = Get-ChildItem -LiteralPath $resolvedProjectRoot -Filter "*.csproj" -File | Select-Object -First 1
    if (-not $projectFile) {
        throw "[SFB-VALIDATE-001] Could not derive project name because no .csproj exists under $resolvedProjectRoot."
    }

    $Name = [System.IO.Path]::GetFileNameWithoutExtension($projectFile.Name)
}

if ([string]::IsNullOrWhiteSpace($StoreKey)) {
    $appSettingsPath = Join-Path $resolvedProjectRoot "appsettings.json"
    if (-not (Test-Path $appSettingsPath)) {
        throw "[SFB-VALIDATE-002] StoreKey was not supplied and appsettings.json is missing under $resolvedProjectRoot."
    }

    $appSettings = Get-Content -LiteralPath $appSettingsPath -Raw | ConvertFrom-Json
    $StoreKey = $appSettings.Storefront.StoreKey
    if ([string]::IsNullOrWhiteSpace($StoreKey)) {
        throw "[SFB-VALIDATE-003] StoreKey was not supplied and Storefront:StoreKey is missing in $appSettingsPath."
    }
}

Write-StorefrontBuilderWorkspacePaths -Paths $workspacePaths

# validate-storefront command entrypoint.
& "$PSScriptRoot/scripts/validate/Test-StorefrontBuilderStaticGate.ps1" -ProjectRoot $resolvedProjectRoot -Name $Name -StoreKey $StoreKey -SkipIdempotency:$SkipIdempotency

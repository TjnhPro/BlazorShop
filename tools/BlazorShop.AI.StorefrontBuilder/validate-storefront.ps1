param(
    [Parameter(Mandatory = $true)]
    [string]$ProjectRoot,
    [string]$Name = "",
    [string]$StoreKey = ""
)

$ErrorActionPreference = "Stop"

function Resolve-InputPath {
    param([string]$Path)

    if ([System.IO.Path]::IsPathRooted($Path)) {
        return [System.IO.Path]::GetFullPath($Path)
    }

    return [System.IO.Path]::GetFullPath((Join-Path (Get-Location) $Path))
}

$resolvedProjectRoot = Resolve-InputPath $ProjectRoot
if (-not (Test-Path $resolvedProjectRoot)) {
    throw "[SFB-VALIDATE-000] Project root does not exist: $resolvedProjectRoot"
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

# validate-storefront command entrypoint.
& "$PSScriptRoot/scripts/validate/Test-StorefrontBuilderStaticGate.ps1" -ProjectRoot $resolvedProjectRoot -Name $Name -StoreKey $StoreKey

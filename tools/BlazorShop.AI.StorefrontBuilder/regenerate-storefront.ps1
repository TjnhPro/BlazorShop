param(
    [Parameter(Mandatory = $true)]
    [string]$ProjectRoot,
    [ValidateSet("all", "page", "component", "css", "validate", "conflicts")]
    [string]$Scope = "all",
    [string]$Target = "",
    [switch]$WhatIf
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
    throw "[SFB-REGEN-000] Project root does not exist: $resolvedProjectRoot"
}

if ($Scope -eq "validate") {
    & "$PSScriptRoot/validate-storefront.ps1" -ProjectRoot $resolvedProjectRoot
    exit 0
}

if ($Scope -eq "conflicts") {
    & "$PSScriptRoot/scripts/validate/Test-StorefrontBuilderIdempotency.ps1" -ProjectRoot $resolvedProjectRoot
    exit 0
}

if ($WhatIf) {
    Write-Host "Validate without writing: scope=$Scope target=$Target"
    exit 0
}

if ($Scope -in @("all", "css")) {
    node "$PSScriptRoot/scripts/generate/apply-visual-foundation.mjs" --project-root $resolvedProjectRoot
}

if ($Scope -in @("all", "page", "component")) {
    node "$PSScriptRoot/scripts/generate/apply-composition.mjs" --project-root $resolvedProjectRoot --target $Target
}

node "$PSScriptRoot/scripts/generate/update-generated-files-manifest.mjs" --project-root $resolvedProjectRoot
& "$PSScriptRoot/scripts/validate/Test-StorefrontBuilderIdempotency.ps1" -ProjectRoot $resolvedProjectRoot

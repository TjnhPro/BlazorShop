[CmdletBinding()]
param(
    [ValidateSet("analyze-only", "preflight-only", "plan-only", "generate", "update", "validate-only", "full")]
    [string]$Mode = "plan-only",

    [string]$Url = "https://reference.example",
    [string]$Name = "BlazorShop.Storefront.GeneratedProof",
    [string]$StoreKey = "sample",
    [string]$OutputRoot = "obj/storefront-builder/generated",
    [string]$HandoffRoot = "",
    [string]$HandoffSchemaRoot = "tools/BlazorShop.AI.StorefrontReverseEngineering/Schemas",

    [switch]$Force,
    [switch]$SkipVisualQa,
    [switch]$SkipCommerceRegression,
    [switch]$InstallNodeDependencies,
    [switch]$Describe
)

$ErrorActionPreference = "Stop"

function Find-RepoRoot {
    param([Parameter(Mandatory = $true)][string]$StartPath)

    $current = [System.IO.Path]::GetFullPath($StartPath)
    while (-not [string]::IsNullOrWhiteSpace($current)) {
        $builderScript = Join-Path $current "tools\BlazorShop.AI.StorefrontBuilder\build-storefront.ps1"
        if ((Test-Path -LiteralPath (Join-Path $current ".git")) -and (Test-Path -LiteralPath $builderScript)) {
            return $current
        }

        $parent = Split-Path -Parent $current
        if ($parent -eq $current) {
            break
        }

        $current = $parent
    }

    throw "Could not find BlazorShop repo root from '$StartPath'."
}

function Format-CommandPart {
    param([string]$Value)

    if ($Value -match "\s|#|'|`"") {
        return "'" + $Value.Replace("'", "''") + "'"
    }

    return $Value
}

function Format-CommandLine {
    param([string]$ScriptPath, [string[]]$Arguments)

    $parts = @("powershell", "-NoProfile", "-ExecutionPolicy", "Bypass", "-File", (Format-CommandPart $ScriptPath))
    foreach ($argument in $Arguments) {
        $parts += Format-CommandPart $argument
    }

    return ($parts -join " ")
}

function Assert-NodeTooling {
    param([Parameter(Mandatory = $true)][string]$BuilderRoot)

    if (-not (Get-Command node -ErrorAction SilentlyContinue)) {
        throw "Node.js is required for StorefrontBuilder, but 'node' is not available on PATH."
    }

    $packageLock = Join-Path $BuilderRoot "package-lock.json"
    $nodeModules = Join-Path $BuilderRoot "node_modules"
    if ((Test-Path -LiteralPath $packageLock) -and -not (Test-Path -LiteralPath $nodeModules)) {
        if (-not $InstallNodeDependencies) {
            throw "StorefrontBuilder node_modules is missing. Re-run with -InstallNodeDependencies or run 'npm ci' in tools\BlazorShop.AI.StorefrontBuilder."
        }

        Write-Host "== install StorefrontBuilder npm dependencies =="
        Push-Location $BuilderRoot
        try {
            npm ci
            if ($LASTEXITCODE -ne 0) {
                throw "npm ci failed with exit code $LASTEXITCODE."
            }
        }
        finally {
            Pop-Location
        }
    }
}

$repoRoot = Find-RepoRoot -StartPath $PSScriptRoot
$builderRoot = Join-Path $repoRoot "tools\BlazorShop.AI.StorefrontBuilder"
$builderScript = Join-Path $builderRoot "build-storefront.ps1"

if ($Mode -eq "preflight-only" -and [string]::IsNullOrWhiteSpace($HandoffRoot)) {
    throw "-Mode preflight-only requires -HandoffRoot <portable-handoff-root>."
}

Assert-NodeTooling -BuilderRoot $builderRoot

$builderArgs = @(
    "-Url", $Url,
    "-Name", $Name,
    "-StoreKey", $StoreKey,
    "-OutputRoot", $OutputRoot,
    "-Mode", $Mode
)

$builderParams = @{
    Url = $Url
    Name = $Name
    StoreKey = $StoreKey
    OutputRoot = $OutputRoot
    Mode = $Mode
}

if (-not [string]::IsNullOrWhiteSpace($HandoffRoot)) {
    $builderArgs += @("-HandoffRoot", $HandoffRoot)
    $builderParams.HandoffRoot = $HandoffRoot
    if (-not [string]::IsNullOrWhiteSpace($HandoffSchemaRoot)) {
        $builderArgs += @("-HandoffSchemaRoot", $HandoffSchemaRoot)
        $builderParams.HandoffSchemaRoot = $HandoffSchemaRoot
    }
}

if ($Force) {
    $builderArgs += "-Force"
    $builderParams.Force = $true
}

if ($SkipVisualQa) {
    $builderArgs += "-SkipVisualQa"
    $builderParams.SkipVisualQa = $true
}

if ($SkipCommerceRegression) {
    $builderArgs += "-SkipCommerceRegression"
    $builderParams.SkipCommerceRegression = $true
}

$commandLine = Format-CommandLine -ScriptPath $builderScript -Arguments $builderArgs
Write-Host "== StorefrontBuilder command =="
Write-Host $commandLine

if ($Describe) {
    return
}

Push-Location $repoRoot
try {
    & $builderScript @builderParams
    if ($LASTEXITCODE -ne 0) {
        throw "StorefrontBuilder failed with exit code $LASTEXITCODE."
    }
}
finally {
    Pop-Location
}

Write-Host "StorefrontBuilder completed successfully."

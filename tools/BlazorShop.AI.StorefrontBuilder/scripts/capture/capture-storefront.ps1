param(
    [Parameter(Mandatory = $true)]
    [string]$Url,
    [Parameter(Mandatory = $true)]
    [string]$OutputRoot
)

$ErrorActionPreference = "Stop"

node (Join-Path $PSScriptRoot "capture-storefront.mjs") `
    --url $Url `
    --outputRoot $OutputRoot

if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

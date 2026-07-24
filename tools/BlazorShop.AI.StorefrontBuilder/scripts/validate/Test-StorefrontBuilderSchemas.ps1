param(
    [string]$SchemaRoot = (Join-Path $PSScriptRoot "..\..\schemas"),
    [string]$FixtureRoot = (Join-Path $PSScriptRoot "..\..\tests\schemas\fixtures")
)

$ErrorActionPreference = "Stop"

function Read-JsonFile {
    param([string]$Path)
    return Get-Content -LiteralPath $Path -Raw | ConvertFrom-Json
}

function Assert-RequiredFields {
    param(
        [string]$SchemaPath,
        [string]$ArtifactPath,
        [bool]$ShouldPass
    )

    $schema = Read-JsonFile $SchemaPath
    $artifact = Read-JsonFile $ArtifactPath
    $names = $artifact.PSObject.Properties.Name
    $missing = @($schema.required | Where-Object { $_ -notin $names })

    if ($ShouldPass -and $missing.Count -gt 0) {
        throw "Schema validation failed for '$ArtifactPath' against '$SchemaPath'. Missing required field(s): $($missing -join ', ')."
    }

    if (-not $ShouldPass -and $missing.Count -eq 0) {
        throw "Invalid fixture '$ArtifactPath' unexpectedly passed required-field validation for '$SchemaPath'."
    }
}

$schemas = Get-ChildItem -LiteralPath $SchemaRoot -Filter "*.schema.json" -File
if ($schemas.Count -lt 14) {
    throw "Expected at least 14 StorefrontBuilder schema files, found $($schemas.Count)."
}

foreach ($schema in $schemas) {
    $parsed = Read-JsonFile $schema.FullName
    foreach ($field in @('$schema', 'title', 'type', 'required', 'properties')) {
        if ($field -notin $parsed.PSObject.Properties.Name) {
            throw "Schema '$($schema.FullName)' is missing '$field'."
        }
    }
}

Assert-RequiredFields `
    -SchemaPath (Join-Path $SchemaRoot "metadata.schema.json") `
    -ArtifactPath (Join-Path $FixtureRoot "valid\metadata.json") `
    -ShouldPass $true

Assert-RequiredFields `
    -SchemaPath (Join-Path $SchemaRoot "generation-plan.schema.json") `
    -ArtifactPath (Join-Path $FixtureRoot "valid\generation-plan.json") `
    -ShouldPass $true

Assert-RequiredFields `
    -SchemaPath (Join-Path $SchemaRoot "metadata.schema.json") `
    -ArtifactPath (Join-Path $FixtureRoot "invalid\metadata.missing-generated-project.json") `
    -ShouldPass $false

Assert-RequiredFields `
    -SchemaPath (Join-Path $SchemaRoot "generation-plan.schema.json") `
    -ArtifactPath (Join-Path $FixtureRoot "invalid\generation-plan.missing-files.json") `
    -ShouldPass $false

Write-Host "StorefrontBuilder schema validation passed."

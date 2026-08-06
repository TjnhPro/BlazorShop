param(
    [Parameter(Mandatory = $true)]
    [string]$ProjectRoot,
    [Parameter(Mandatory = $true)]
    [string]$Name,
    [Parameter(Mandatory = $true)]
    [string]$StoreKey
)

$ErrorActionPreference = "Stop"

$projectFile = Join-Path $ProjectRoot "$Name.csproj"
$wasmProjectRoot = Join-Path $ProjectRoot "$Name.WASM"
$wasmProjectFile = Join-Path $wasmProjectRoot "$Name.WASM.csproj"
$metadata = Join-Path $ProjectRoot "docs\storefront-analysis\metadata.yaml"
$featureManifest = Join-Path $ProjectRoot "Features\feature-manifest.json"

function Test-TextContains {
    param(
        [string]$Text,
        [string]$Needle,
        [System.StringComparison]$Comparison = [System.StringComparison]::Ordinal
    )

    return $Text.IndexOf($Needle, $Comparison) -ge 0
}

foreach ($path in @($projectFile, $wasmProjectFile, $metadata, $featureManifest)) {
    if (-not (Test-Path $path)) {
        throw "[SFB-PROJECT-003] Generated project required file is missing: $path"
    }
}

$project = Get-Content -LiteralPath $projectFile -Raw
$wasmProject = Get-Content -LiteralPath $wasmProjectFile -Raw
foreach ($package in @("BlazorShop.Storefront.Presentation", "BlazorShop.Storefront.Components", "BlazorShop.Storefront.Browser")) {
    if (-not (Test-TextContains -Text $project -Needle "PackageReference Include=`"$package`"")) {
        throw "[SFB-PROJECT-004] Generated server project is missing package reference '$package'."
    }
}

foreach ($package in @("BlazorShop.Storefront.Components", "BlazorShop.Storefront.Browser")) {
    if (-not (Test-TextContains -Text $wasmProject -Needle "PackageReference Include=`"$package`"")) {
        throw "[SFB-PROJECT-004] Generated WASM project is missing package reference '$package'."
    }
}

foreach ($package in @("BlazorShop.Storefront.Runtime", "BlazorShop.Storefront.Client")) {
    if (Test-TextContains -Text $project -Needle "PackageReference Include=`"$package`"") {
        throw "[SFB-PROJECT-004] Generated server project must not direct-reference '$package'. Presentation/Runtime own application transport."
    }

    if (Test-TextContains -Text $wasmProject -Needle "PackageReference Include=`"$package`"") {
        throw "[SFB-PROJECT-004] Generated WASM project must not direct-reference '$package'. Browser/Presentation own application transport."
    }
}

[xml]$serverProjectDocument = Get-Content -LiteralPath $projectFile -Raw
[xml]$wasmProjectDocument = Get-Content -LiteralPath $wasmProjectFile -Raw
$serverProjectReferences = @(@($serverProjectDocument.Project.ItemGroup.ProjectReference) |
    ForEach-Object { [string]$_.Include } |
    Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
$wasmProjectReferences = @(@($wasmProjectDocument.Project.ItemGroup.ProjectReference) |
    ForEach-Object { [string]$_.Include } |
    Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
$expectedWasmReference = "$Name.WASM\$Name.WASM.csproj"
if ($serverProjectReferences.Count -ne 1 -or -not ([string]$serverProjectReferences[0]).Equals($expectedWasmReference, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "[SFB-PROJECT-004] Generated server must reference only generated sibling WASM '$expectedWasmReference'. Actual: $($serverProjectReferences -join ', ')"
}

if ($wasmProjectReferences.Count -ne 0) {
    throw "[SFB-PROJECT-004] Generated WASM project must not contain ProjectReference entries. Actual: $($wasmProjectReferences -join ', ')"
}

$packageVersions = Get-Content -LiteralPath (Join-Path $ProjectRoot "StorefrontPackageVersions.props") -Raw
if (-not (Test-TextContains -Text $packageVersions -Needle "StorefrontClientPackageVersion") -or -not (Test-TextContains -Text $packageVersions -Needle "StorefrontBrowserPackageVersion")) {
    throw "[SFB-PROJECT-004] Generated project is missing Client package compatibility metadata."
}

$metadataText = Get-Content -LiteralPath $metadata -Raw
$canonicalContractPath = "contracts/storefront/storefront.openapi.json"
foreach ($required in @("generatorVersion:", "createdUtc:", "updatedUtc:", "commandMode:", "projectName: $Name", "normalizedProjectName: $Name", "storeKey: $StoreKey", "outputRoot:", "storefrontContractPath: $canonicalContractPath", "storefrontContractSha256:", "sourceStarterPath:", "sourceStarterWasmPath:", "sourceStarterVersion:", "sourceHead:", "packageBuildIdentity:", "starterContractPath: BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/starter-generation.contract.yaml", "starterContractVersion:", "projects:", "server:", "path: $Name.csproj", "wasm:", "path: $Name.WASM/$Name.WASM.csproj", "protectedFiles:", "packageVersions:", "packageProvenance:", "BlazorShop.Storefront.Presentation", "BlazorShop.Storefront.Components", "BlazorShop.Storefront.Browser")) {
    if (-not (Test-TextContains -Text $metadataText -Needle $required)) {
        throw "[SFB-PROJECT-005] metadata.yaml is missing '$required'."
    }
}

$contractHashMatch = [regex]::Match($metadataText, "(?m)^storefrontContractSha256:\s*([a-f0-9]{64})\s*$")
if (-not $contractHashMatch.Success) {
    throw "[SFB-PROJECT-007] metadata.yaml must contain lowercase SHA-256 storefrontContractSha256 for the canonical Storefront OpenAPI contract."
}

$starterVersionMatch = [regex]::Match($metadataText, "(?m)^starterContractVersion:\s*\S+\s*$")
if (-not $starterVersionMatch.Success) {
    throw "[SFB-PROJECT-008] metadata.yaml must contain starterContractVersion from the Starter generation contract."
}

$createdUtcMatch = [regex]::Match($metadataText, "(?m)^createdUtc:\s*\d{4}-\d{2}-\d{2}T.+Z\s*$")
if (-not $createdUtcMatch.Success) {
    throw "[SFB-PROJECT-009] metadata.yaml must contain an ISO-8601 UTC createdUtc timestamp."
}

$updatedUtcMatch = [regex]::Match($metadataText, "(?m)^updatedUtc:\s*\d{4}-\d{2}-\d{2}T.+Z\s*$")
if (-not $updatedUtcMatch.Success) {
    throw "[SFB-PROJECT-009] metadata.yaml must contain an ISO-8601 UTC updatedUtc timestamp."
}

foreach ($packageVersionMarker in @("BlazorShop.Storefront.Client:", "BlazorShop.Storefront.Runtime:", "BlazorShop.Storefront.Presentation:", "BlazorShop.Storefront.Components:", "BlazorShop.Storefront.Browser:")) {
    if (-not (Test-TextContains -Text $metadataText -Needle $packageVersionMarker)) {
        throw "[SFB-PROJECT-009] metadata.yaml is missing package version marker '$packageVersionMarker'."
    }
}

$forbiddenDirectories = @("Security", "Services", "Middleware")
foreach ($directory in $forbiddenDirectories) {
    if (Test-Path (Join-Path $ProjectRoot $directory)) {
        throw "[SFB-PROJECT-006] Generated project must not contain application/security folder '$directory'."
    }
}

$forbidden = @("BlazorShop.Storefront.V2", "BlazorShop.Web.SharedV2", "Web.SharedV2", "BlazorShop.Application", "BlazorShop.Domain", "BlazorShop.Infrastructure", "BlazorShop.CommerceNode.API", "BlazorShop.ControlPlane.API", "BlazorShop.ControlPlane.Web", "PackageReference Include=`"BlazorShop.Storefront.Runtime`"", "PackageReference Include=`"BlazorShop.Storefront.Client`"")
Get-ChildItem -LiteralPath $ProjectRoot -Recurse -File |
    Where-Object { $_.FullName -notmatch "\\(bin|obj)\\" } |
    ForEach-Object {
        $content = Get-Content -LiteralPath $_.FullName -Raw
        foreach ($pattern in $forbidden) {
            if (Test-TextContains -Text $content -Needle $pattern -Comparison ([System.StringComparison]::OrdinalIgnoreCase)) {
                throw "[SFB-PROJECT-006] Forbidden dependency '$pattern' found in $($_.FullName)."
            }
        }
    }

Get-ChildItem -LiteralPath $ProjectRoot -Recurse -File -Include *.cs,*.razor,*.csproj |
    Where-Object { $_.FullName -notmatch "\\(bin|obj)\\" } |
    ForEach-Object {
        $content = Get-Content -LiteralPath $_.FullName -Raw
        if (Test-TextContains -Text $content -Needle "BlazorShop.Storefront.Starter" -Comparison ([System.StringComparison]::OrdinalIgnoreCase)) {
            throw "[SFB-PROJECT-006] Starter namespace/source reference found in generated code/project file $($_.FullName)."
        }
    }

Write-Host "StorefrontBuilder generated project validation passed for $ProjectRoot."

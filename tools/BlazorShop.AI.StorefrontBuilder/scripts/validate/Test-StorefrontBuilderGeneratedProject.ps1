param(
    [Parameter(Mandatory = $true)]
    [string]$ProjectRoot,
    [Parameter(Mandatory = $true)]
    [string]$Name,
    [Parameter(Mandatory = $true)]
    [string]$StoreKey
)

$ErrorActionPreference = "Stop"

$serverProjectRoot = Join-Path $ProjectRoot $Name
$projectFile = Join-Path $serverProjectRoot "$Name.csproj"
$wasmProjectRoot = Join-Path $ProjectRoot "$Name.WASM"
$wasmProjectFile = Join-Path $wasmProjectRoot "$Name.WASM.csproj"
$serverProgram = Join-Path $serverProjectRoot "Program.cs"
$wasmProgram = Join-Path $wasmProjectRoot "Program.cs"
$solutionFile = Join-Path $ProjectRoot "$Name.sln"
$metadata = Join-Path $ProjectRoot "docs\storefront-analysis\metadata.yaml"
$generatedFilesManifest = Join-Path $ProjectRoot "docs\storefront-analysis\generated-files.yaml"
$generatedStarterContract = Join-Path $ProjectRoot "docs\storefront-analysis\starter-generation.contract.yaml"
$featureManifest = Join-Path $serverProjectRoot "Features\feature-manifest.json"

function Test-TextContains {
    param(
        [string]$Text,
        [string]$Needle,
        [System.StringComparison]$Comparison = [System.StringComparison]::Ordinal
    )

    return $Text.IndexOf($Needle, $Comparison) -ge 0
}

function Get-RelativePathCompat([string]$BasePath, [string]$TargetPath) {
    $baseFullPath = [System.IO.Path]::GetFullPath($BasePath).TrimEnd([System.IO.Path]::DirectorySeparatorChar, [System.IO.Path]::AltDirectorySeparatorChar) + [System.IO.Path]::DirectorySeparatorChar
    $targetFullPath = [System.IO.Path]::GetFullPath($TargetPath)
    $baseUri = [System.Uri]::new($baseFullPath)
    $targetUri = [System.Uri]::new($targetFullPath)
    return [System.Uri]::UnescapeDataString($baseUri.MakeRelativeUri($targetUri).ToString()).Replace("/", [System.IO.Path]::DirectorySeparatorChar)
}

function Validate-PackageProvenanceHashes {
    param(
        [Parameter(Mandatory = $true)][string]$MetadataText,
        [Parameter(Mandatory = $true)][string]$ProjectRoot
    )

    $feedPathMatch = [regex]::Match($MetadataText, "(?m)^\s*feedPath:\s*(\S+)\s*$")
    if (-not $feedPathMatch.Success -or $feedPathMatch.Groups[1].Value -eq "unknown") {
        return
    }

    $feedPath = $feedPathMatch.Groups[1].Value
    $resolvedFeedPath = if ([System.IO.Path]::IsPathRooted($feedPath)) {
        [System.IO.Path]::GetFullPath($feedPath)
    }
    else {
        [System.IO.Path]::GetFullPath((Join-Path $ProjectRoot $feedPath))
    }

    if (-not (Test-Path -LiteralPath $resolvedFeedPath)) {
        return
    }

    $packageMatches = [regex]::Matches(
        $MetadataText,
        "(?ms)^\s+- id:\s*(?<id>\S+)\s*\r?\n\s+version:\s*(?<version>\S+)\s*\r?\n\s+sha256:\s*(?<sha>\S+)\s*$")

    foreach ($packageMatch in $packageMatches) {
        $expectedHash = $packageMatch.Groups["sha"].Value
        if ($expectedHash -eq "unknown") {
            continue
        }

        if (-not [regex]::IsMatch($expectedHash, "^[a-f0-9]{64}$")) {
            throw "[SFB-PROJECT-010] Package provenance hash must be lowercase SHA-256 for $($packageMatch.Groups["id"].Value)."
        }

        $packageId = $packageMatch.Groups["id"].Value
        $packageVersion = $packageMatch.Groups["version"].Value
        $packagePath = Join-Path $resolvedFeedPath "$packageId.$packageVersion.nupkg"
        if (-not (Test-Path -LiteralPath $packagePath)) {
            throw "[SFB-PROJECT-010] Package provenance points to missing local package: $packagePath"
        }

        $actualHash = (Get-FileHash -LiteralPath $packagePath -Algorithm SHA256).Hash.ToLowerInvariant()
        if (-not $actualHash.Equals($expectedHash, [System.StringComparison]::OrdinalIgnoreCase)) {
            throw "[SFB-PROJECT-010] Package hash mismatch for $packageId $packageVersion. Expected $expectedHash but saw $actualHash."
        }
    }
}

foreach ($path in @($solutionFile, $projectFile, $wasmProjectFile, $serverProgram, $wasmProgram, $metadata, $generatedFilesManifest, $generatedStarterContract, $featureManifest)) {
    if (-not (Test-Path $path)) {
        throw "[SFB-PROJECT-003] Generated project required file is missing: $path. Problem: a required starter-first workspace file is absent. Cause: the output is incomplete, uses the retired nested shape, or the wrong root was passed. Fix: regenerate fresh or pass the generated workspace root."
    }
}

if (Test-Path -LiteralPath (Join-Path $serverProjectRoot "starter-generation.contract.yaml")) {
    throw "[SFB-PROJECT-003] Starter generation contract must be a workspace analysis artifact, not a server source file."
}

if (Test-Path -LiteralPath (Join-Path $serverProjectRoot "$Name.WASM")) {
    throw "[SFB-PROJECT-003] Generated server project must not contain nested WASM folder '$Name.WASM'. Problem: this is the retired nested output shape. Cause: the storefront was generated before the starter-first workspace migration. Fix: regenerate the storefront into a fresh workspace."
}

$solutionText = Get-Content -LiteralPath $solutionFile -Raw
$expectedSolutionProjects = @("$Name\$Name.csproj", "$Name.WASM\$Name.WASM.csproj")
foreach ($expectedSolutionProject in $expectedSolutionProjects) {
    if (-not (Test-TextContains -Text $solutionText -Needle "`"$expectedSolutionProject`"")) {
        throw "[SFB-PROJECT-004] Generated solution is missing expected project '$expectedSolutionProject'. Problem: solution must contain only the generated server and WASM sibling projects. Cause: the output is incomplete or was generated with an old shape. Fix: regenerate the storefront workspace."
    }
}

$solutionProjectMatches = [regex]::Matches($solutionText, "(?m)^Project\([^)]+\)\s*=\s*`"[^`"]+`",\s*`"(?<path>[^`"]+\.csproj)`"")
foreach ($solutionProjectMatch in $solutionProjectMatches) {
    $projectPath = $solutionProjectMatch.Groups["path"].Value
    if ($expectedSolutionProjects -notcontains $projectPath) {
        throw "[SFB-PROJECT-004] Generated solution contains unexpected project '$projectPath'. Problem: generated workspace solutions must not include V2, Starter, backend, Control Plane, Commerce Node, or other generated outputs. Cause: the solution was edited manually or generated with a non-isolated shape. Fix: regenerate the storefront workspace or remove the unexpected solution entry."
    }
}

$project = Get-Content -LiteralPath $projectFile -Raw
$wasmProject = Get-Content -LiteralPath $wasmProjectFile -Raw
foreach ($package in @("Microsoft.AspNetCore.Components.WebAssembly.Server", "BlazorShop.Storefront.Presentation", "BlazorShop.Storefront.Components", "BlazorShop.Storefront.Browser")) {
    if (-not (Test-TextContains -Text $project -Needle "PackageReference Include=`"$package`"")) {
        throw "[SFB-PROJECT-004] Generated server project is missing package reference '$package'."
    }
}

foreach ($package in @("Microsoft.AspNetCore.Components.WebAssembly", "BlazorShop.Storefront.Components", "BlazorShop.Storefront.Browser")) {
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

foreach ($excludeMarker in @(
    "<Compile Remove=`"$Name.WASM\**`" />",
    "<Content Remove=`"$Name.WASM\**`" />",
    "<EmbeddedResource Remove=`"$Name.WASM\**`" />",
    "<None Remove=`"$Name.WASM\**`" />"
)) {
    if (Test-TextContains -Text $project -Needle $excludeMarker) {
        throw "[SFB-PROJECT-004] Generated server project must not include nested WASM exclusion marker: $excludeMarker. Problem: exclusion ItemGroups are only needed for the retired nested WASM shape. Cause: the server project was generated by old logic or edited to hide a nested WASM folder. Fix: regenerate the storefront workspace so WASM is a sibling project."
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
$expectedWasmReference = "..\$Name.WASM\$Name.WASM.csproj"
$expectedWasmReferenceFullPath = [System.IO.Path]::GetFullPath($wasmProjectFile)
$serverReferenceFullPaths = @($serverProjectReferences |
    ForEach-Object { [System.IO.Path]::GetFullPath((Join-Path $serverProjectRoot $_)) })
if ($serverProjectReferences.Count -ne 1 -or -not ([string]$serverProjectReferences[0]).Equals($expectedWasmReference, [System.StringComparison]::OrdinalIgnoreCase) -or -not ([string]$serverReferenceFullPaths[0]).Equals($expectedWasmReferenceFullPath, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "[SFB-PROJECT-004] Generated server must reference only generated sibling WASM '$expectedWasmReference'. Actual: $($serverProjectReferences -join ', ')"
}

if ($wasmProjectReferences.Count -ne 0) {
    throw "[SFB-PROJECT-004] Generated WASM project must not contain ProjectReference entries. Actual: $($wasmProjectReferences -join ', ')"
}

$serverProgramText = Get-Content -LiteralPath $serverProgram -Raw
foreach ($requiredServerProgramMarker in @(
    "AddStorefrontApplication",
    "AddStorefrontBrowserControllers",
    "UseStorefrontApplication",
    "MapStorefrontApplication",
    "typeof($Name.WASM.StarterWasmAssemblyMarker).Assembly"
)) {
    if (-not (Test-TextContains -Text $serverProgramText -Needle $requiredServerProgramMarker)) {
        throw "[SFB-PROJECT-004] Generated server Program.cs is missing '$requiredServerProgramMarker'."
    }
}

$wasmProgramText = Get-Content -LiteralPath $wasmProgram -Raw
foreach ($requiredWasmProgramMarker in @(
    "WebAssemblyHostBuilder.CreateDefault(args)",
    "AddStorefrontBrowserRuntime(builder.HostEnvironment)"
)) {
    if (-not (Test-TextContains -Text $wasmProgramText -Needle $requiredWasmProgramMarker)) {
        throw "[SFB-PROJECT-004] Generated WASM Program.cs is missing '$requiredWasmProgramMarker'."
    }
}

$packageVersions = Get-Content -LiteralPath (Join-Path $ProjectRoot "StorefrontPackageVersions.props") -Raw
if (-not (Test-TextContains -Text $packageVersions -Needle "StorefrontClientPackageVersion") -or -not (Test-TextContains -Text $packageVersions -Needle "StorefrontBrowserPackageVersion")) {
    throw "[SFB-PROJECT-004] Generated project is missing Client package compatibility metadata."
}

$metadataText = Get-Content -LiteralPath $metadata -Raw
$canonicalContractPath = "contracts/storefront/storefront.openapi.json"
foreach ($required in @("generatorVersion:", "createdUtc:", "updatedUtc:", "commandMode:", "projectName: $Name", "normalizedProjectName: $Name", "storeKey: $StoreKey", "outputRoot:", "workspaceLayoutVersion: starter-first-sibling-wasm-1", "workspaceRoot:", "serverProjectRoot: $Name", "wasmProjectRoot: $Name.WASM", "solutionPath: $Name.sln", "analysisRoot: docs/storefront-analysis", "storefrontContractPath: $canonicalContractPath", "storefrontContractSha256:", "sourceStarterPath:", "sourceStarterWasmPath:", "sourceStarterVersion:", "sourceHead:", "packageBuildIdentity:", "starterContractPath: BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/starter-generation.contract.yaml", "starterContractVersion:", "starterContractSha256:", "starterWasmContractPath: BlazorShop.PresentationV2/BlazorShop.Storefront.Starter.WASM/BlazorShop.Storefront.Starter.WASM.csproj", "starterWasmContractSha256:", "projects:", "server:", "path: $Name/$Name.csproj", "wasm:", "path: $Name.WASM/$Name.WASM.csproj", "protectedFiles:", "packageReferences:", "Microsoft.AspNetCore.Components.WebAssembly.Server", "Microsoft.AspNetCore.Components.WebAssembly", "packageVersions:", "packageProvenance:", "BlazorShop.Storefront.Presentation", "BlazorShop.Storefront.Components", "BlazorShop.Storefront.Browser")) {
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

foreach ($hashField in @("starterContractSha256", "starterWasmContractSha256")) {
    if (-not [regex]::IsMatch($metadataText, "(?m)^$($hashField):\s*[a-f0-9]{64}\s*$")) {
        throw "[SFB-PROJECT-008] metadata.yaml must contain lowercase SHA-256 $hashField."
    }
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

Validate-PackageProvenanceHashes -MetadataText $metadataText -ProjectRoot $ProjectRoot

$generatedFilesText = Get-Content -LiteralPath $generatedFilesManifest -Raw
$manifestEntries = [regex]::Matches($generatedFilesText, "(?ms)^\s+- filePath:\s*(?<file>[^\r\n]+)\r?\n(?<body>.*?)(?=^\s+- filePath:|\z)")
if ($manifestEntries.Count -eq 0) {
    throw "[SFB-PROJECT-011] generated-files.yaml must contain tracked file entries."
}

$hasServerManifestEntry = $false
$hasWasmManifestEntry = $false
$hasWorkspaceManifestEntry = $false
$manifestBodiesByPath = @{}
foreach ($entry in $manifestEntries) {
    $filePath = $entry.Groups["file"].Value.Trim()
    $body = $entry.Groups["body"].Value
    $manifestBodiesByPath[$filePath] = $body
    $projectMatch = [regex]::Match($body, "(?m)^\s+project:\s*(workspace|server|wasm)\s*$")
    if (-not $projectMatch.Success) {
        throw "[SFB-PROJECT-011] generated-files.yaml entry '$filePath' is missing project ownership."
    }

    foreach ($field in @("workspaceRelativePath", "projectKind", "projectName", "projectRelativePath")) {
        if (-not [regex]::IsMatch($body, "(?m)^\s+$($field):\s*\S+")) {
            throw "[SFB-PROJECT-011] generated-files.yaml entry '$filePath' is missing '$field'."
        }
    }

    if (-not [regex]::IsMatch($body, "(?m)^\s+ownership:\s*(generated|managed|user-owned|protected|artifact-only)\s*$")) {
        throw "[SFB-PROJECT-011] generated-files.yaml entry '$filePath' is missing ownership."
    }

    if ($projectMatch.Groups[1].Value -eq "workspace") { $hasWorkspaceManifestEntry = $true }
    if ($projectMatch.Groups[1].Value -eq "server") { $hasServerManifestEntry = $true }
    if ($projectMatch.Groups[1].Value -eq "wasm") { $hasWasmManifestEntry = $true }
}

foreach ($requiredManifestMarker in @(
    "filePath: StorefrontPackageVersions.props",
    "project: workspace",
    "projectKind: workspace",
    "ownership: protected",
    "filePath: $Name.WASM/Program.cs",
    "project: wasm",
    "projectKind: wasm"
)) {
    if (-not (Test-TextContains -Text $generatedFilesText -Needle $requiredManifestMarker)) {
        throw "[SFB-PROJECT-011] generated-files.yaml is missing '$requiredManifestMarker'."
    }
}

$cssManifestBody = $manifestBodiesByPath["$Name/wwwroot/css/storefront-builder.generated.css"]
if ($null -ne $cssManifestBody -and -not [regex]::IsMatch($cssManifestBody, "(?m)^\s+ownership:\s*generated\s*$")) {
    throw "[SFB-PROJECT-011] generated-files.yaml must mark generated visual CSS ownership as generated."
}

if (-not $hasWorkspaceManifestEntry -or -not $hasServerManifestEntry -or -not $hasWasmManifestEntry) {
    throw "[SFB-PROJECT-011] generated-files.yaml must include workspace, server, and WASM project entries."
}

$forbiddenDirectories = @("Security", "Services", "Middleware")
foreach ($directory in $forbiddenDirectories) {
    if (Test-Path (Join-Path $serverProjectRoot $directory)) {
        throw "[SFB-PROJECT-006] Generated project must not contain application/security folder '$directory'."
    }
}

$forbidden = @("BlazorShop.Storefront.V2", "BlazorShop.Web.SharedV2", "Web.SharedV2", "BlazorShop.Application", "BlazorShop.Domain", "BlazorShop.Infrastructure", "BlazorShop.CommerceNode.API", "BlazorShop.ControlPlane.API", "BlazorShop.ControlPlane.Web", "PackageReference Include=`"BlazorShop.Storefront.Runtime`"", "PackageReference Include=`"BlazorShop.Storefront.Client`"")
Get-ChildItem -LiteralPath $ProjectRoot -Recurse -File |
    ForEach-Object {
        $relativeToProject = (Get-RelativePathCompat $ProjectRoot $_.FullName).Replace("\", "/")
        if ($relativeToProject -match "(^|/)(bin|obj)/") {
            return
        }

        $content = Get-Content -LiteralPath $_.FullName -Raw
        foreach ($pattern in $forbidden) {
            if (Test-TextContains -Text $content -Needle $pattern -Comparison ([System.StringComparison]::OrdinalIgnoreCase)) {
                throw "[SFB-PROJECT-006] Forbidden dependency '$pattern' found in $($_.FullName)."
            }
        }
    }

Get-ChildItem -LiteralPath $ProjectRoot -Recurse -File -Include *.cs,*.razor,*.csproj |
    ForEach-Object {
        $relativeToProject = (Get-RelativePathCompat $ProjectRoot $_.FullName).Replace("\", "/")
        if ($relativeToProject -match "(^|/)(bin|obj)/") {
            return
        }

        if ($relativeToProject -notmatch "\.(cs|razor|csproj)$") {
            return
        }

        $content = Get-Content -LiteralPath $_.FullName -Raw
        if (Test-TextContains -Text $content -Needle "BlazorShop.Storefront.Starter" -Comparison ([System.StringComparison]::OrdinalIgnoreCase)) {
            throw "[SFB-PROJECT-006] Starter namespace/source reference found in generated code/project file $($_.FullName)."
        }
    }

Write-Host "StorefrontBuilder generated project validation passed for $ProjectRoot."

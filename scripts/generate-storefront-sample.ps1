param(
    [string]$Name = "BlazorShop.Storefront.GeneratedProof",
    [string]$StoreKey = "sample",
    [string]$OutputRoot = "artifacts/storefront-builder/generated",
    [string]$CommerceNodeBaseUrl = "http://localhost:5180",
    [string]$PublicBaseUrl = "http://localhost:18600",
    [string]$StorefrontClientPackageVersion = "",
    [string]$StorefrontRuntimePackageVersion = "",
    [string]$StorefrontPresentationPackageVersion = "",
    [string]$StorefrontComponentsPackageVersion = "",
    [string]$StorefrontBrowserPackageVersion = "",
    [switch]$Force
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path $PSScriptRoot\..
$starterRoot = Join-Path $repoRoot "BlazorShop.PresentationV2\BlazorShop.Storefront.Starter"
$starterWasmRoot = Join-Path $repoRoot "BlazorShop.PresentationV2\BlazorShop.Storefront.Starter.WASM"
function Resolve-RepoPath {
    param([string]$Path)

    if ([System.IO.Path]::IsPathRooted($Path)) {
        return [System.IO.Path]::GetFullPath($Path)
    }

    return [System.IO.Path]::GetFullPath((Join-Path $repoRoot $Path))
}

$generatedRoot = Resolve-RepoPath $OutputRoot
$targetProjectRoot = Join-Path $generatedRoot $Name
$operationId = [System.Guid]::NewGuid().ToString("N")
$stagingOutputRoot = Join-Path (Join-Path $generatedRoot ".staging") $operationId
$backupProjectRoot = Join-Path (Join-Path $generatedRoot ".replace-backup") "$Name-$operationId"
$projectRoot = Join-Path $stagingOutputRoot $Name
$starterProject = Join-Path $projectRoot "BlazorShop.Storefront.Starter.csproj"
$generatedProject = Join-Path $projectRoot "$Name.csproj"
$starterWasmProjectRoot = Join-Path $projectRoot "BlazorShop.Storefront.Starter.WASM"
$generatedWasmProjectRoot = Join-Path $projectRoot "$Name.WASM"
$starterWasmProject = Join-Path $starterWasmProjectRoot "BlazorShop.Storefront.Starter.WASM.csproj"
$generatedWasmProject = Join-Path $generatedWasmProjectRoot "$Name.WASM.csproj"

$forbiddenPatterns = @(
    "BlazorShop.Storefront.V2",
    "BlazorShop.Application",
    "BlazorShop.Domain",
    "BlazorShop.Infrastructure",
    "BlazorShop.CommerceNode.API",
    "BlazorShop.ControlPlane.API",
    "BlazorShop.ControlPlane.Web",
    "BlazorShop.Web.SharedV2",
    "Web.SharedV2",
    "Generated\StorefrontClient.g.cs",
    "Generated/StorefrontClient.g.cs"
)

$forbiddenTemplateDirectories = @(
    "Security",
    "Services",
    "Middleware"
)

function Assert-OutputPath {
    $resolved = [System.IO.Path]::GetFullPath($projectRoot)
    $expectedPrefix = [System.IO.Path]::GetFullPath($generatedRoot)
    if (-not $resolved.StartsWith($expectedPrefix, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to generate outside StorefrontBuilder generated output root: $resolved"
    }
}

function Assert-GeneratedRootPath {
    param([Parameter(Mandatory = $true)][string]$Path)

    $resolved = [System.IO.Path]::GetFullPath($Path)
    $expectedPrefix = [System.IO.Path]::GetFullPath($generatedRoot)
    if (-not $resolved.StartsWith($expectedPrefix, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to modify path outside StorefrontBuilder generated output root: $resolved"
    }
}

function Remove-GeneratedDirectoryIfEmpty {
    param([Parameter(Mandatory = $true)][string]$Path)

    if (-not (Test-Path -LiteralPath $Path)) {
        return
    }

    Assert-GeneratedRootPath -Path $Path
    if (-not (Get-ChildItem -LiteralPath $Path -Force | Select-Object -First 1)) {
        Remove-Item -LiteralPath $Path -Force
    }
}

function Copy-StarterTemplate {
    if (Test-Path $projectRoot) {
        if (-not $Force) {
            throw "Output '$projectRoot' already exists. Re-run with -Force to replace deterministic generated output."
        }

        Remove-Item -LiteralPath $projectRoot -Recurse -Force
    }

    New-Item -ItemType Directory -Force -Path $projectRoot | Out-Null
    Get-ChildItem -LiteralPath $starterRoot -Force |
        Where-Object { $_.Name -notin @("bin", "obj") -and (-not $_.PSIsContainer -or $_.Name -notin $forbiddenTemplateDirectories) } |
        ForEach-Object {
            Copy-Item -LiteralPath $_.FullName -Destination $projectRoot -Recurse -Force
        }

    New-Item -ItemType Directory -Force -Path $starterWasmProjectRoot | Out-Null
    Get-ChildItem -LiteralPath $starterWasmRoot -Force |
        Where-Object { $_.Name -notin @("bin", "obj") } |
        ForEach-Object {
            Copy-Item -LiteralPath $_.FullName -Destination $starterWasmProjectRoot -Recurse -Force
        }
}

function Get-PortableRelativePath {
    param(
        [Parameter(Mandatory = $true)][string]$BasePath,
        [Parameter(Mandatory = $true)][string]$TargetPath
    )

    $baseFullPath = [System.IO.Path]::GetFullPath($BasePath).TrimEnd('\', '/') + [System.IO.Path]::DirectorySeparatorChar
    $targetFullPath = [System.IO.Path]::GetFullPath($TargetPath)
    $baseUri = [System.Uri]::new($baseFullPath)
    $targetUri = [System.Uri]::new($targetFullPath)
    return [System.Uri]::UnescapeDataString($baseUri.MakeRelativeUri($targetUri).ToString()).Replace('\', '/')
}

function Rewrite-GeneratedSource {
    if (Test-Path $starterProject) {
        Rename-Item -LiteralPath $starterProject -NewName "$Name.csproj"
    }

    if (Test-Path $starterWasmProject) {
        Rename-Item -LiteralPath $starterWasmProject -NewName "$Name.WASM.csproj"
    }

    if (Test-Path $starterWasmProjectRoot) {
        Rename-Item -LiteralPath $starterWasmProjectRoot -NewName "$Name.WASM"
    }

    $textFiles = Get-ChildItem -LiteralPath $projectRoot -Recurse -File |
        Where-Object {
            $_.Extension -in @(".cs", ".razor", ".csproj", ".props", ".json", ".md", ".config", ".css")
        }

    foreach ($file in $textFiles) {
        $content = Get-Content -LiteralPath $file.FullName -Raw
        $content = $content.Replace("BlazorShop.Storefront.Starter", $Name)
        $content = $content.Replace('"StoreKey": "default"', "`"StoreKey`": `"$StoreKey`"")
        $content = $content.Replace('"CommerceNodeBaseUrl": "http://localhost:5180"', "`"CommerceNodeBaseUrl`": `"$CommerceNodeBaseUrl`"")
        $content = $content.Replace('"PublicBaseUrl": "http://localhost:18599"', "`"PublicBaseUrl`": `"$PublicBaseUrl`"")
        $content = $content.Replace('"BaseUrl": "http://localhost:18599"', "`"BaseUrl`": `"$PublicBaseUrl`"")
        Set-Content -LiteralPath $file.FullName -Value $content -Encoding UTF8
    }

    $versionPropsPath = Join-Path $projectRoot "StorefrontPackageVersions.props"
    if (Test-Path $versionPropsPath) {
        [xml]$versionDocument = Get-Content -LiteralPath $versionPropsPath -Raw
        $properties = $versionDocument.Project.PropertyGroup
        if (-not [string]::IsNullOrWhiteSpace($StorefrontClientPackageVersion)) { $properties.StorefrontClientPackageVersion = $StorefrontClientPackageVersion }
        if (-not [string]::IsNullOrWhiteSpace($StorefrontRuntimePackageVersion)) { $properties.StorefrontRuntimePackageVersion = $StorefrontRuntimePackageVersion }
        if (-not [string]::IsNullOrWhiteSpace($StorefrontPresentationPackageVersion)) { $properties.StorefrontPresentationPackageVersion = $StorefrontPresentationPackageVersion }
        if (-not [string]::IsNullOrWhiteSpace($StorefrontComponentsPackageVersion)) { $properties.StorefrontComponentsPackageVersion = $StorefrontComponentsPackageVersion }
        if (-not [string]::IsNullOrWhiteSpace($StorefrontBrowserPackageVersion)) { $properties.StorefrontBrowserPackageVersion = $StorefrontBrowserPackageVersion }
        $versionDocument.Save($versionPropsPath)
    }

    $nugetConfigPath = Join-Path $projectRoot "nuget.config"
    if (Test-Path $nugetConfigPath) {
        $packageFeed = Join-Path $repoRoot "artifacts\storefront-packages"
        $relativePackageFeed = Get-PortableRelativePath -BasePath $projectRoot -TargetPath $packageFeed
        $nugetConfig = @(
            '<?xml version="1.0" encoding="utf-8"?>',
            '<configuration>',
            '  <packageSources>',
            '    <clear />',
            "    <add key=`"local-storefront-packages`" value=`"$relativePackageFeed`" />",
            '    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />',
            '  </packageSources>',
            '</configuration>'
        ) -join [Environment]::NewLine
        Set-Content -LiteralPath $nugetConfigPath -Value $nugetConfig -Encoding UTF8
    }

    $readmeContent = @(
        "# $Name",
        "",
        "Generated deterministic storefront sample.",
        "",
        "- Source: BlazorShop.Storefront.Starter",
        "- Store key: $StoreKey",
        "- Commerce Node base URL is configured server-side.",
        "- Package versions are pinned in StorefrontPackageVersions.props.",
        "",
        "Build after packing local packages:",
        "",
        "dotnet restore $Name.csproj",
        "dotnet build $Name.csproj --no-restore"
    ) -join [Environment]::NewLine
    Set-Content -LiteralPath (Join-Path $projectRoot "README.md") -Value $readmeContent -Encoding UTF8

    $projectContent = Get-Content -LiteralPath $generatedProject -Raw
    $projectContent = $projectContent.Replace(
        '    <ProjectReference Include="..\BlazorShop.Storefront.Presentation\BlazorShop.Storefront.Presentation.csproj" />',
        '    <PackageReference Include="BlazorShop.Storefront.Presentation" Version="$(StorefrontPresentationPackageVersion)" />')
    $projectContent = $projectContent.Replace(
        '    <ProjectReference Include="..\BlazorShop.Storefront.Components\BlazorShop.Storefront.Components.csproj" />',
        '    <PackageReference Include="BlazorShop.Storefront.Components" Version="$(StorefrontComponentsPackageVersion)" />')
    $projectContent = $projectContent.Replace(
        '    <ProjectReference Include="..\BlazorShop.Storefront.Browser\BlazorShop.Storefront.Browser.csproj" />',
        '    <PackageReference Include="BlazorShop.Storefront.Browser" Version="$(StorefrontBrowserPackageVersion)" />')
    $projectContent = $projectContent.Replace(
        "..\$Name.WASM\$Name.WASM.csproj",
        "$Name.WASM\$Name.WASM.csproj")
    Set-Content -LiteralPath $generatedProject -Value $projectContent -Encoding UTF8

    if (Test-Path $generatedWasmProject) {
        $wasmProjectContent = Get-Content -LiteralPath $generatedWasmProject -Raw
        $wasmProjectContent = $wasmProjectContent.Replace(
            '<Import Project="StorefrontPackageVersions.props" Condition="Exists(''StorefrontPackageVersions.props'')" />',
            '<Import Project="..\StorefrontPackageVersions.props" />')
        $wasmProjectContent = $wasmProjectContent.Replace(
            '    <ProjectReference Include="..\BlazorShop.Storefront.Components\BlazorShop.Storefront.Components.csproj" />',
            '    <PackageReference Include="BlazorShop.Storefront.Components" Version="$(StorefrontComponentsPackageVersion)" />')
        $wasmProjectContent = $wasmProjectContent.Replace(
            '    <ProjectReference Include="..\BlazorShop.Storefront.Browser\BlazorShop.Storefront.Browser.csproj" />',
            '    <PackageReference Include="BlazorShop.Storefront.Browser" Version="$(StorefrontBrowserPackageVersion)" />')
        Set-Content -LiteralPath $generatedWasmProject -Value $wasmProjectContent -Encoding UTF8
    }
}

function Assert-GeneratedOutput {
    if (-not (Test-Path $generatedProject)) {
        throw "Generated project file was not created: $generatedProject"
    }

    if (-not (Test-Path $generatedWasmProject)) {
        throw "Generated WASM project file was not created: $generatedWasmProject"
    }

    Assert-GeneratedProjectReferences

    $sourceFiles = Get-ChildItem -LiteralPath $projectRoot -Recurse -File |
        Where-Object { $_.FullName -notmatch "\\(bin|obj)\\" }

    $violations = foreach ($file in $sourceFiles) {
        $content = Get-Content -LiteralPath $file.FullName -Raw
        foreach ($pattern in $forbiddenPatterns) {
            if ($content.IndexOf($pattern, [System.StringComparison]::OrdinalIgnoreCase) -ge 0) {
                "$($file.FullName): $pattern"
            }
        }
    }

    if ($violations) {
        $violations | ForEach-Object { Write-Error $_ }
        throw "Generated storefront contains forbidden dependency/source references."
    }
}

function Assert-GeneratedProjectReferences {
    [xml]$serverProject = Get-Content -LiteralPath $generatedProject -Raw
    [xml]$wasmProject = Get-Content -LiteralPath $generatedWasmProject -Raw
    $expectedServerReference = "$Name.WASM\$Name.WASM.csproj"
    $serverReferences = @(@($serverProject.Project.ItemGroup.ProjectReference) |
        ForEach-Object { [string]$_.Include } |
        Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
    $wasmReferences = @(@($wasmProject.Project.ItemGroup.ProjectReference) |
        ForEach-Object { [string]$_.Include } |
        Where-Object { -not [string]::IsNullOrWhiteSpace($_) })

    if ($serverReferences.Count -ne 1 -or -not ([string]$serverReferences[0]).Equals($expectedServerReference, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Generated server must reference only generated sibling WASM project '$expectedServerReference'. Actual: $($serverReferences -join ', ')"
    }

    if ($wasmReferences.Count -ne 0) {
        throw "Generated WASM project must not contain ProjectReference entries. Actual: $($wasmReferences -join ', ')"
    }
}

Assert-OutputPath
Assert-GeneratedRootPath -Path $targetProjectRoot
Assert-GeneratedRootPath -Path $stagingOutputRoot
Assert-GeneratedRootPath -Path $backupProjectRoot

$movedExistingTarget = $false

try {
    if ((Test-Path $targetProjectRoot) -and -not $Force) {
        throw "Output '$targetProjectRoot' already exists. Re-run with -Force to replace deterministic generated output."
    }

    if (Test-Path $stagingOutputRoot) {
        Remove-Item -LiteralPath $stagingOutputRoot -Recurse -Force
    }

    Copy-StarterTemplate
    Rewrite-GeneratedSource
    Assert-GeneratedOutput

    if (Test-Path $targetProjectRoot) {
        New-Item -ItemType Directory -Force -Path (Split-Path -Parent $backupProjectRoot) | Out-Null
        Move-Item -LiteralPath $targetProjectRoot -Destination $backupProjectRoot
        $movedExistingTarget = $true
    }

    Move-Item -LiteralPath $projectRoot -Destination $targetProjectRoot

    if ($movedExistingTarget) {
        Remove-Item -LiteralPath $backupProjectRoot -Recurse -Force
    }
}
catch {
    if ($movedExistingTarget -and -not (Test-Path $targetProjectRoot) -and (Test-Path $backupProjectRoot)) {
        Move-Item -LiteralPath $backupProjectRoot -Destination $targetProjectRoot
    }

    throw
}
finally {
    if (Test-Path $stagingOutputRoot) {
        Remove-Item -LiteralPath $stagingOutputRoot -Recurse -Force
    }
    Remove-GeneratedDirectoryIfEmpty -Path (Split-Path -Parent $stagingOutputRoot)
    Remove-GeneratedDirectoryIfEmpty -Path (Split-Path -Parent $backupProjectRoot)
}

Write-Host "Generated $Name at $targetProjectRoot"

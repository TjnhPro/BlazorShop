param(
    [string]$Name = "BlazorShop.Storefront.GeneratedProof",
    [string]$StoreKey = "sample",
    [string]$OutputRoot = "artifacts/storefront-builder/generated",
    [string]$CommerceNodeBaseUrl = "http://localhost:5180",
    [string]$PublicBaseUrl = "http://localhost:18600",
    [switch]$Force
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path $PSScriptRoot\..
$starterRoot = Join-Path $repoRoot "BlazorShop.PresentationV2\BlazorShop.Storefront.Starter"
function Resolve-RepoPath {
    param([string]$Path)

    if ([System.IO.Path]::IsPathRooted($Path)) {
        return [System.IO.Path]::GetFullPath($Path)
    }

    return [System.IO.Path]::GetFullPath((Join-Path $repoRoot $Path))
}

$generatedRoot = Resolve-RepoPath $OutputRoot
$projectRoot = Join-Path $generatedRoot $Name
$starterProject = Join-Path $projectRoot "BlazorShop.Storefront.Starter.csproj"
$generatedProject = Join-Path $projectRoot "$Name.csproj"

$forbiddenPatterns = @(
    "ProjectReference",
    "BlazorShop.Storefront.V2",
    "BlazorShop.Application",
    "BlazorShop.Domain",
    "BlazorShop.Infrastructure",
    "BlazorShop.CommerceNode.API",
    "BlazorShop.ControlPlane.API",
    "BlazorShop.ControlPlane.Web",
    "Generated\StorefrontClient.g.cs",
    "Generated/StorefrontClient.g.cs"
)

function Assert-OutputPath {
    $resolved = [System.IO.Path]::GetFullPath($projectRoot)
    $expectedPrefix = [System.IO.Path]::GetFullPath($generatedRoot)
    if (-not $resolved.StartsWith($expectedPrefix, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to generate outside StorefrontBuilder generated output root: $resolved"
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
        Where-Object { $_.Name -notin @("bin", "obj") } |
        ForEach-Object {
            Copy-Item -LiteralPath $_.FullName -Destination $projectRoot -Recurse -Force
        }
}

function Rewrite-GeneratedSource {
    if (Test-Path $starterProject) {
        Rename-Item -LiteralPath $starterProject -NewName "$Name.csproj"
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
        Set-Content -LiteralPath $file.FullName -Value $content -Encoding UTF8
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
}

function Assert-GeneratedOutput {
    if (-not (Test-Path $generatedProject)) {
        throw "Generated project file was not created: $generatedProject"
    }

    $sourceFiles = Get-ChildItem -LiteralPath $projectRoot -Recurse -File |
        Where-Object { $_.FullName -notmatch "\\(bin|obj)\\" }

    $violations = foreach ($file in $sourceFiles) {
        $content = Get-Content -LiteralPath $file.FullName -Raw
        foreach ($pattern in $forbiddenPatterns) {
            if ($content.Contains($pattern, [System.StringComparison]::OrdinalIgnoreCase)) {
                "$($file.FullName): $pattern"
            }
        }
    }

    if ($violations) {
        $violations | ForEach-Object { Write-Error $_ }
        throw "Generated storefront contains forbidden dependency/source references."
    }
}

Assert-OutputPath
Copy-StarterTemplate
Rewrite-GeneratedSource
Assert-GeneratedOutput

Write-Host "Generated $Name at $projectRoot"

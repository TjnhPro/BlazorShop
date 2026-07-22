param(
    [ValidateSet("Inventory", "ActiveStrict")]
    [string] $Mode = "Inventory",

    [string] $AllowListPath = (Join-Path $PSScriptRoot "..\docs\refactor-control-Commerce-storefront\legacy-removal-allowlist.json")
)

$ErrorActionPreference = "Stop"

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$textExtensions = @(
    ".cs",
    ".csproj",
    ".json",
    ".md",
    ".ps1",
    ".razor",
    ".sln",
    ".slnf",
    ".txt",
    ".yml",
    ".yaml"
)
$forbiddenPatterns = @(
    "BlazorShop\.Presentation(?!V2)",
    "BlazorShop\.AppHost\b",
    "ConnectionStrings__DefaultConnection\b",
    "\bDefaultConnection\b",
    "\bAppDbContext\b",
    "AddInfrastructure\(",
    "AddSharedAuthenticationInfrastructure\(",
    "UseInfrastructure\("
)
$activeRoots = @(
    "BlazorShop.PresentationV2",
    "BlazorShop.sln",
    "BlazorShop.V2.slnf",
    ".github/workflows/ci.yml",
    "compose.v2.production.yml",
    "scripts/run-v2-local.ps1",
    "scripts/stop-v2-local.ps1"
)
$ignoredSegments = @(
    "bin",
    "obj",
    "node_modules",
    ".git",
    ".vs",
    ".idea",
    "TestResults"
)

function Get-RelativePath {
    param([Parameter(Mandatory = $true)][string] $Path)

    $resolvedPath = (Resolve-Path -LiteralPath $Path).Path
    $normalizedRoot = $repoRoot.TrimEnd([System.IO.Path]::DirectorySeparatorChar, [System.IO.Path]::AltDirectorySeparatorChar)
    $rootPrefix = $normalizedRoot + [System.IO.Path]::DirectorySeparatorChar

    if ($resolvedPath.StartsWith($rootPrefix, [StringComparison]::OrdinalIgnoreCase)) {
        return $resolvedPath.Substring($rootPrefix.Length).Replace([System.IO.Path]::DirectorySeparatorChar, '/')
    }

    return $resolvedPath.Replace([System.IO.Path]::DirectorySeparatorChar, '/')
}

function Get-TextTargets {
    param(
        [Parameter(Mandatory = $true)]
        [string[]] $Roots
    )

    foreach ($relativeRoot in $Roots) {
        $absoluteRoot = Join-Path $repoRoot $relativeRoot
        if (-not (Test-Path -LiteralPath $absoluteRoot)) {
            continue
        }

        $item = Get-Item -LiteralPath $absoluteRoot
        if ($item.PSIsContainer) {
            Get-ChildItem -LiteralPath $absoluteRoot -Recurse -File |
                Where-Object {
                    $segments = $_.FullName.Split([System.IO.Path]::DirectorySeparatorChar, [System.IO.Path]::AltDirectorySeparatorChar)
                    if ($segments | Where-Object { $ignoredSegments -contains $_ }) {
                        return $false
                    }

                    $extension = [System.IO.Path]::GetExtension($_.FullName)
                    $textExtensions -contains $extension.ToLowerInvariant()
                }
        }
        else {
            $extension = [System.IO.Path]::GetExtension($item.FullName)
            if ($textExtensions -contains $extension.ToLowerInvariant()) {
                $item
            }
        }
    }
}

function Read-AllowList {
    if (-not (Test-Path -LiteralPath $AllowListPath)) {
        return [pscustomobject]@{
            activeStrictIgnoreFiles = @()
        }
    }

    return Get-Content -Raw -LiteralPath $AllowListPath | ConvertFrom-Json
}

$allowList = Read-AllowList
$targets = if ($Mode -eq "Inventory") {
    Get-TextTargets -Roots @(".")
}
else {
    Get-TextTargets -Roots $activeRoots
}

$hits = New-Object System.Collections.Generic.List[object]

foreach ($pattern in $forbiddenPatterns) {
    foreach ($target in $targets) {
        $relativePath = Get-RelativePath -Path $target.FullName

        if ($Mode -eq "ActiveStrict" -and $allowList.activeStrictIgnoreFiles -contains $relativePath) {
            continue
        }

        $matches = Select-String -LiteralPath $target.FullName -Pattern $pattern -ErrorAction SilentlyContinue
        foreach ($match in $matches) {
            $hits.Add([pscustomobject]@{
                Path = $relativePath
                LineNumber = $match.LineNumber
                Pattern = $pattern
                Line = $match.Line.TrimEnd()
            })
        }
    }
}

$sortedHits = $hits |
    Sort-Object Path, LineNumber, Pattern

if ($sortedHits.Count -eq 0) {
    if ($Mode -eq "Inventory") {
        Write-Host "Inventory: no legacy references found."
    }
    else {
        Write-Host "ActiveStrict: no active legacy references found."
    }

    exit 0
}

$grouped = $sortedHits | Group-Object Path | Sort-Object Name

if ($Mode -eq "Inventory") {
    Write-Host "Inventory: legacy references found."
    foreach ($group in $grouped) {
        Write-Host $group.Name
        foreach ($hit in $group.Group) {
            Write-Host ("  {0}:{1} [{2}] {3}" -f $hit.Path, $hit.LineNumber, $hit.Pattern, $hit.Line)
        }
    }

    exit 0
}

Write-Host "ActiveStrict: legacy references found."
foreach ($group in $grouped) {
    Write-Host $group.Name
    foreach ($hit in $group.Group) {
        Write-Host ("  {0}:{1} [{2}] {3}" -f $hit.Path, $hit.LineNumber, $hit.Pattern, $hit.Line)
    }
}

exit 1

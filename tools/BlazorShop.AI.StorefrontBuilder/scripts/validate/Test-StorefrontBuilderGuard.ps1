param(
    [Parameter(Mandatory = $true)]
    [string]$ProjectRoot
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..\..\..")
function Resolve-RepoPath {
    param([string]$Path)

    if ([System.IO.Path]::IsPathRooted($Path)) {
        return [System.IO.Path]::GetFullPath($Path)
    }

    return [System.IO.Path]::GetFullPath((Join-Path $repoRoot $Path))
}

$resolvedProjectRoot = Resolve-RepoPath $ProjectRoot

if (-not (Test-Path $resolvedProjectRoot)) {
    throw "[SFB-GUARD-000] Project root does not exist: $resolvedProjectRoot"
}

function Fail-Guard {
    param(
        [string]$RuleId,
        [string]$Path,
        [string]$Message
    )

    throw "[$RuleId] $Path $Message"
}

function Get-SourceFiles {
    Get-ChildItem -LiteralPath $resolvedProjectRoot -Recurse -File |
        Where-Object {
            $_.FullName -notmatch "\\(bin|obj)\\" -and
            $_.Extension -in @(".cs", ".razor", ".csproj", ".props", ".json", ".css", ".js")
        }
}

foreach ($file in Get-SourceFiles) {
    $relative = [System.IO.Path]::GetRelativePath($repoRoot, $file.FullName)
    $content = Get-Content -LiteralPath $file.FullName -Raw
    $isBrowserFile = $file.Extension -in @(".razor", ".css", ".js")

    $usesDirectTransport = $content.Contains("HttpClient", [System.StringComparison]::Ordinal) `
        -or $content.Contains("fetch(""http", [System.StringComparison]::OrdinalIgnoreCase) `
        -or $content.Contains("fetch('http", [System.StringComparison]::OrdinalIgnoreCase)
    if ($isBrowserFile -and $usesDirectTransport) {
        Fail-Guard "SFB-GUARD-001" $relative "Generated presentation must not use HttpClient or direct HTTP transport."
    }

    $exposesCommerceNode = $content.Contains("CommerceNodeBaseUrl", [System.StringComparison]::OrdinalIgnoreCase) `
        -or $content.Contains("localhost:5180", [System.StringComparison]::OrdinalIgnoreCase) `
        -or $content.Contains("/api/storefront/stores/", [System.StringComparison]::OrdinalIgnoreCase)
    if ($isBrowserFile -and $exposesCommerceNode) {
        Fail-Guard "SFB-GUARD-002" $relative "Browser presentation must not know Commerce Node URL or protected Storefront API paths."
    }

    $handlesBrowserCredential = $content.Contains("accessToken", [System.StringComparison]::OrdinalIgnoreCase) `
        -or $content.Contains("refreshToken", [System.StringComparison]::OrdinalIgnoreCase) `
        -or $content.Contains("localStorage", [System.StringComparison]::OrdinalIgnoreCase) `
        -or $content.Contains("sessionStorage", [System.StringComparison]::OrdinalIgnoreCase)
    if ($isBrowserFile -and $handlesBrowserCredential) {
        Fail-Guard "SFB-GUARD-003" $relative "Browser presentation must not handle credentials or browser token storage."
    }

    if ($file.Extension -eq ".csproj" -and $content.Contains("ProjectReference", [System.StringComparison]::OrdinalIgnoreCase)) {
        Fail-Guard "SFB-GUARD-004" $relative "Generated storefront must not use ProjectReference to backend/core/API/V2 projects."
    }

    foreach ($namespace in @(
        "using BlazorShop.Application",
        "using BlazorShop.Domain",
        "using BlazorShop.Infrastructure",
        "using BlazorShop.PresentationV2.BlazorShop.CommerceNode.API"
    )) {
        if ($content.Contains($namespace, [System.StringComparison]::Ordinal)) {
            Fail-Guard "SFB-GUARD-005" $relative "Generated source must not import backend/core/API namespaces: $namespace."
        }
    }

    foreach ($dtoName in @(
        "CommerceNodeApiResponse",
        "StorefrontCartResponse",
        "StorefrontCheckoutSessionResponse",
        "StorefrontProductResponse"
    )) {
        $duplicatesDto = $content.Contains("class $dtoName", [System.StringComparison]::Ordinal) `
            -or $content.Contains("record $dtoName", [System.StringComparison]::Ordinal)
        if ($duplicatesDto) {
            Fail-Guard "SFB-GUARD-006" $relative "Generated source must not duplicate generated API DTO: $dtoName."
        }
    }

    foreach ($businessTerm in @(
        "CalculatePrice",
        "IsSellable",
        "ValidateCart",
        "ValidateCheckout",
        "PlaceOrder",
        "CapturePayment"
    )) {
        if ($isBrowserFile -and $content.Contains($businessTerm, [System.StringComparison]::OrdinalIgnoreCase)) {
            Fail-Guard "SFB-GUARD-007" $relative "Generated presentation must not own ecommerce business validation logic: $businessTerm."
        }
    }
}

Write-Host "StorefrontBuilder protected file and dependency guard passed for $ProjectRoot."

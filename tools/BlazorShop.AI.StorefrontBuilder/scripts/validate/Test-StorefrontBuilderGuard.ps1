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

function Test-TextContains([string]$Text, [string]$Value, [System.StringComparison]$Comparison = [System.StringComparison]::Ordinal) {
    return $Text.IndexOf($Value, $Comparison) -ge 0
}

function Get-RelativePathCompat([string]$BasePath, [string]$TargetPath) {
    $baseFullPath = [System.IO.Path]::GetFullPath($BasePath).TrimEnd([System.IO.Path]::DirectorySeparatorChar, [System.IO.Path]::AltDirectorySeparatorChar) + [System.IO.Path]::DirectorySeparatorChar
    $targetFullPath = [System.IO.Path]::GetFullPath($TargetPath)
    $baseUri = [System.Uri]::new($baseFullPath)
    $targetUri = [System.Uri]::new($targetFullPath)
    return [System.Uri]::UnescapeDataString($baseUri.MakeRelativeUri($targetUri).ToString()).Replace("/", [System.IO.Path]::DirectorySeparatorChar)
}

function Get-SourceFiles {
    Get-ChildItem -LiteralPath $resolvedProjectRoot -Recurse -File |
        Where-Object {
            $_.FullName -notmatch "\\(bin|obj)\\" -and
            $_.Extension -in @(".cs", ".razor", ".csproj", ".props", ".json", ".css", ".js")
        }
}

foreach ($file in Get-SourceFiles) {
    $relative = Get-RelativePathCompat $repoRoot $file.FullName
    $content = Get-Content -LiteralPath $file.FullName -Raw
    $isBrowserFile = $file.Extension -in @(".razor", ".css", ".js")

    $usesDirectTransport = (Test-TextContains $content "HttpClient") `
        -or (Test-TextContains $content "fetch(""http" ([System.StringComparison]::OrdinalIgnoreCase)) `
        -or (Test-TextContains $content "fetch('http" ([System.StringComparison]::OrdinalIgnoreCase))
    if ($isBrowserFile -and $usesDirectTransport) {
        Fail-Guard "SFB-GUARD-001" $relative "Generated presentation must not use HttpClient or direct HTTP transport."
    }

    $exposesCommerceNode = (Test-TextContains $content "CommerceNodeBaseUrl" ([System.StringComparison]::OrdinalIgnoreCase)) `
        -or (Test-TextContains $content "localhost:5180" ([System.StringComparison]::OrdinalIgnoreCase)) `
        -or (Test-TextContains $content "/api/storefront/stores/" ([System.StringComparison]::OrdinalIgnoreCase))
    if ($isBrowserFile -and $exposesCommerceNode) {
        Fail-Guard "SFB-GUARD-002" $relative "Browser presentation must not know Commerce Node URL or protected Storefront API paths."
    }

    $handlesBrowserCredential = (Test-TextContains $content "accessToken" ([System.StringComparison]::OrdinalIgnoreCase)) `
        -or (Test-TextContains $content "refreshToken" ([System.StringComparison]::OrdinalIgnoreCase)) `
        -or (Test-TextContains $content "localStorage" ([System.StringComparison]::OrdinalIgnoreCase)) `
        -or (Test-TextContains $content "sessionStorage" ([System.StringComparison]::OrdinalIgnoreCase))
    if ($isBrowserFile -and $handlesBrowserCredential) {
        Fail-Guard "SFB-GUARD-003" $relative "Browser presentation must not handle credentials or browser token storage."
    }

    if ($file.Extension -eq ".csproj" -and (Test-TextContains $content "ProjectReference" ([System.StringComparison]::OrdinalIgnoreCase))) {
        Fail-Guard "SFB-GUARD-004" $relative "Generated storefront must not use ProjectReference to backend/core/API/V2 projects."
    }

    foreach ($namespace in @(
        "using BlazorShop.Application",
        "using BlazorShop.Domain",
        "using BlazorShop.Infrastructure",
        "using BlazorShop.PresentationV2.BlazorShop.CommerceNode.API"
    )) {
        if (Test-TextContains $content $namespace) {
            Fail-Guard "SFB-GUARD-005" $relative "Generated source must not import backend/core/API namespaces: $namespace."
        }
    }

    foreach ($dtoName in @(
        "CommerceNodeApiResponse",
        "StorefrontCartResponse",
        "StorefrontCheckoutSessionResponse",
        "StorefrontProductResponse"
    )) {
        $duplicatesDto = (Test-TextContains $content "class $dtoName") `
            -or (Test-TextContains $content "record $dtoName")
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
        if ($isBrowserFile -and (Test-TextContains $content $businessTerm ([System.StringComparison]::OrdinalIgnoreCase))) {
            Fail-Guard "SFB-GUARD-007" $relative "Generated presentation must not own ecommerce business validation logic: $businessTerm."
        }
    }
}

Write-Host "StorefrontBuilder protected file and dependency guard passed for $ProjectRoot."

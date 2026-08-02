param(
    [Parameter(Mandatory = $true)]
    [string]$ProjectRoot,
    [Parameter(Mandatory = $true)]
    [string]$Name,
    [Parameter(Mandatory = $true)]
    [string]$StoreKey,
    [switch]$SkipIdempotency
)

$ErrorActionPreference = "Stop"
$toolRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$repoRoot = Resolve-Path (Join-Path $toolRoot "..\..")

& (Join-Path $toolRoot "scripts\validate\Test-StorefrontBuilderSchemas.ps1")
& (Join-Path $toolRoot "scripts\validate\Test-StorefrontBuilderGeneratedProject.ps1") -ProjectRoot $ProjectRoot -Name $Name -StoreKey $StoreKey
$analysisRoot = Join-Path $ProjectRoot "docs\storefront-analysis"
$isHandoffProject = Test-Path (Join-Path $analysisRoot "generation-plan.json")
if ($isHandoffProject) {
    node (Join-Path $toolRoot "scripts\validate\Test-StorefrontBuilderHandoffBoundary.mjs") --project-root $ProjectRoot --name $Name
    if ($LASTEXITCODE -ne 0) {
        throw "[SFB-STATIC-010] Handoff boundary validation failed with exit code $LASTEXITCODE."
    }
}
else {
    & (Join-Path $toolRoot "scripts\validate\Test-StorefrontBuilderAssets.ps1") -ProjectRoot $ProjectRoot
    & (Join-Path $toolRoot "scripts\validate\Test-StorefrontBuilderCss.ps1") -ProjectRoot $ProjectRoot
    & (Join-Path $toolRoot "scripts\validate\Test-StorefrontBuilderCompositionFiles.ps1") -ProjectRoot $ProjectRoot
}
& (Join-Path $toolRoot "scripts\validate\Test-StorefrontBuilderGuard.ps1") -ProjectRoot $ProjectRoot
if (-not $SkipIdempotency) {
    & (Join-Path $toolRoot "scripts\validate\Test-StorefrontBuilderIdempotency.ps1") -ProjectRoot $ProjectRoot
}

$requiredAnalysisArtifacts = if ($isHandoffProject) {
    @("metadata.yaml", "generation-plan.json", "generation-plan.yaml", "generated-files.yaml", "regeneration-report.md")
}
else {
    @("metadata.yaml", "asset-manifest.yaml", "generated-files.yaml")
}

foreach ($artifact in $requiredAnalysisArtifacts) {
    if (-not (Test-Path (Join-Path $analysisRoot $artifact))) {
        throw "[SFB-STATIC-001] Generated file manifest or analysis artifact is missing: $artifact"
    }
}

$routeDirectives = @()
Get-ChildItem -LiteralPath $ProjectRoot -Recurse -File -Include *.razor |
    Where-Object { $_.FullName -notmatch "\\(bin|obj)\\" } |
    ForEach-Object {
        $content = Get-Content -LiteralPath $_.FullName -Raw
        foreach ($match in [regex]::Matches($content, "(?m)^@page\s+`"([^`"]+)`"")) {
            $route = $match.Groups[1].Value
            $routeDirectives += "'$route' in '$($_.FullName)'"
        }
    }

if ($routeDirectives.Count -gt 0) {
    throw "[SFB-STATIC-002] Generated storefront visual files must not declare @page routes. Register Presentation view slots instead: $($routeDirectives -join '; ')"
}

$versions = Get-Content -LiteralPath (Join-Path $ProjectRoot "StorefrontPackageVersions.props") -Raw
$project = Get-Content -LiteralPath (Join-Path $ProjectRoot "$Name.csproj") -Raw
foreach ($package in @("BlazorShop.Storefront.Presentation", "BlazorShop.Storefront.Components")) {
    if (-not $project.Contains("PackageReference Include=`"$package`"", [System.StringComparison]::Ordinal)) {
        throw "[SFB-STATIC-003] Package version mismatch or missing package reference: $package"
    }
}

foreach ($package in @("BlazorShop.Storefront.Runtime", "BlazorShop.Storefront.Client")) {
    if ($project.Contains("PackageReference Include=`"$package`"", [System.StringComparison]::Ordinal)) {
        throw "[SFB-STATIC-003] Generated project must not direct-reference application transport package: $package"
    }
}

if (-not $versions.Contains("StorefrontClientPackageVersion", [System.StringComparison]::Ordinal) -or -not $versions.Contains("StorefrontRuntimePackageVersion", [System.StringComparison]::Ordinal) -or -not $versions.Contains("StorefrontComponentsPackageVersion", [System.StringComparison]::Ordinal)) {
    throw "[SFB-STATIC-004] Package compatibility metadata is missing."
}

$sourceFiles = Get-ChildItem -LiteralPath $ProjectRoot -Recurse -File -Include *.cs,*.razor,*.js,*.mjs,*.ts |
    Where-Object { $_.FullName -notmatch "\\(bin|obj)\\" }

$forbiddenSourceTokens = @(
    "StorefrontLocalApiClient",
    "GetAsync<",
    "PostJsonAsync<",
    "PutJsonAsync<",
    "DeleteAsync<",
    "IServiceProvider",
    "GetService(",
    "GetService<",
    "GetRequiredService(",
    "GetRequiredService<",
    "IdempotencyKey",
    "ExpectedCartVersion",
    "ExpectedCheckoutVersion",
    "HttpClient",
    "fetch(",
    "XMLHttpRequest"
)

foreach ($sourceFile in $sourceFiles) {
    $relativeToProject = [System.IO.Path]::GetRelativePath($ProjectRoot, $sourceFile.FullName).Replace("\", "/")
    $content = Get-Content -LiteralPath $sourceFile.FullName -Raw
    foreach ($token in $forbiddenSourceTokens) {
        if ($content.Contains($token, [System.StringComparison]::Ordinal)) {
            throw "[SFB-STATIC-008] Generated visual source must not own browser transport, service location, or Browser request DTO orchestration: $relativeToProject contains $token"
        }
    }

    if ([regex]::IsMatch($content, "\bStorefrontBrowser[A-Za-z0-9_]*Request\b")) {
        throw "[SFB-STATIC-008] Generated visual source must not construct Browser request DTOs: $relativeToProject contains StorefrontBrowser*Request"
    }
}

$forbiddenBootstrapTokens = @(
    "AddHttpClient",
    "AddScoped<",
    "AddScoped(",
    "AddSingleton<",
    "AddSingleton(",
    "AddTransient<",
    "AddTransient(",
    "MapGet(",
    "MapPost(",
    "MapPut(",
    "MapDelete(",
    "MapMethods(",
    "MapGroup(",
    "UseMiddleware",
    "UseWhen(",
    "AddStorefrontRuntime",
    "AddStorefrontPlatformRuntime",
    "AddStorefrontPresentation(",
    "UseStorefrontPresentation(",
    "MapStorefrontPresentation(",
    "MapRazorComponents<"
)

foreach ($sourceFile in $sourceFiles | Where-Object { $_.Name -eq "Program.cs" -or $_.Name.EndsWith("FoundationViewRegistration.cs", [System.StringComparison]::Ordinal) }) {
    $relativeToProject = [System.IO.Path]::GetRelativePath($ProjectRoot, $sourceFile.FullName).Replace("\", "/")
    $content = Get-Content -LiteralPath $sourceFile.FullName -Raw
    foreach ($token in $forbiddenBootstrapTokens) {
        if ($content.Contains($token, [System.StringComparison]::Ordinal)) {
            throw "[SFB-STATIC-009] Generated bootstrap files may only compose Storefront application and view registrations: $relativeToProject contains $token"
        }
    }
}

$functionalScript = Join-Path $ProjectRoot "wwwroot\js\storefront-builder.functional.js"
if (Test-Path $functionalScript) {
    throw "[SFB-STATIC-005] Generated storefront must not emit copied browser application controller JS: wwwroot/js/storefront-builder.functional.js"
}

$jsRoot = Join-Path $ProjectRoot "wwwroot\js"
if (Test-Path $jsRoot) {
    Get-ChildItem -LiteralPath $jsRoot -Recurse -File -Filter *.js |
        Where-Object { $_.FullName -notmatch "\\(bin|obj)\\" } |
        ForEach-Object {
            $relativeToProject = [System.IO.Path]::GetRelativePath($ProjectRoot, $_.FullName).Replace("\", "/")
            if (-not $relativeToProject.StartsWith("wwwroot/js/visual/", [System.StringComparison]::Ordinal)) {
                throw "[SFB-STATIC-006] Generated storefront JS is only allowed in wwwroot/js/visual for event-only visuals: $relativeToProject"
            }

            $content = Get-Content -LiteralPath $_.FullName -Raw
            foreach ($token in @(
                ".application.cart.",
                ".application.consent.",
                ".application.productSelection.",
                "application.cart",
                "application.consent",
                "application.productSelection",
                "cart.addLine",
                "productSelection.preview",
                "ProductId:",
                "ProductVariantId:",
                "SelectedAttributes:",
                "CurrencyCode:",
                "productId:",
                "productVariantId:",
                "selectedAttributes:",
                "currencyCode:"
            )) {
                if ($content.Contains($token, [System.StringComparison]::Ordinal)) {
                    throw "[SFB-STATIC-007] Generated visual JS must not invoke application commands or construct command payloads: $relativeToProject contains $token"
                }
            }
        }
}

Write-Host "StorefrontBuilder static validation gate passed for $ProjectRoot."

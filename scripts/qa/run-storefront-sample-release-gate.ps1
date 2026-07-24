param(
    [string]$Configuration = "Release",
    [string]$StorefrontClientPackageVersion = "1.0.0-local",
    [string]$StorefrontRuntimePackageVersion = "1.0.0-local",
    [string]$SampleUrl = "http://127.0.0.1:18610",
    [int]$RuntimeTimeoutSeconds = 45,
    [switch]$SkipRuntime,
    [switch]$Describe
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$clientProject = Join-Path $repoRoot "BlazorShop.PresentationV2\BlazorShop.Storefront.Client\BlazorShop.Storefront.Client.csproj"
$runtimeProject = Join-Path $repoRoot "BlazorShop.PresentationV2\BlazorShop.Storefront.Runtime\BlazorShop.Storefront.Runtime.csproj"
$sampleRoot = Join-Path $repoRoot "BlazorShop.PresentationV2\BlazorShop.Storefront.Sample"
$sampleProject = Join-Path $sampleRoot "BlazorShop.Storefront.Sample.csproj"
$feedRoot = Join-Path $repoRoot "artifacts\storefront-packages"
$publishRoot = Join-Path $repoRoot "artifacts\storefront-sample-release"
$generatedClient = Join-Path $repoRoot "BlazorShop.PresentationV2\BlazorShop.Storefront.Client\Generated\StorefrontClient.g.cs"
$todoFile = Join-Path $repoRoot "docs\refactor-control-Commerce-storefront\Storefront Starter Foundation.todo.md"

$forbiddenSourcePatterns = @(
    "ProjectReference",
    "BlazorShop.Application",
    "BlazorShop.Domain",
    "BlazorShop.Infrastructure",
    "BlazorShop.CommerceNode.API",
    "BlazorShop.ControlPlane.API",
    "BlazorShop.ControlPlane.Web",
    "BlazorShop.Storefront.V2",
    "StorefrontApiClient",
    "Generated\StorefrontClient.g.cs",
    "Generated/StorefrontClient.g.cs"
)

$forbiddenBrowserPatterns = @(
    "CommerceNodeBaseUrl",
    "http://localhost:5180",
    "https://localhost:5180",
    "accessToken",
    "refreshToken",
    "provider credentials",
    "store secret"
)

if ($Describe) {
    Write-Host "Storefront Sample release gate"
    Write-Host "- Pack Storefront.Client and Storefront.Runtime to local feed"
    Write-Host "- Restore/build/publish generated Storefront.Sample from package references"
    Write-Host "- Verify generated client compatibility and provider callback/webhook exclusion"
    Write-Host "- Verify Sample has no backend/core/V2/generated-source copy"
    Write-Host "- Verify required storefront routes, SEO/security conventions, BFF CSRF, and browser token boundaries"
    Write-Host "- Optionally assert live Sample route responses unless -SkipRuntime is supplied"
    exit 0
}

function Invoke-Step {
    param(
        [string]$Name,
        [scriptblock]$Action
    )

    Write-Host "== $Name =="
    & $Action
}

function Assert-ContainsText {
    param(
        [string]$Path,
        [string]$Text
    )

    $content = Get-Content -LiteralPath $Path -Raw
    if (-not $content.Contains($Text, [System.StringComparison]::Ordinal)) {
        throw "Expected '$Path' to contain '$Text'."
    }
}

function Assert-DoesNotContainText {
    param(
        [string]$Path,
        [string]$Text
    )

    $content = Get-Content -LiteralPath $Path -Raw
    if ($content.Contains($Text, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Expected '$Path' not to contain '$Text'."
    }
}

function Get-SampleSourceFiles {
    Get-ChildItem -LiteralPath $sampleRoot -Recurse -File |
        Where-Object {
            $_.FullName -notmatch "\\(bin|obj)\\" -and
            $_.Extension -in @(".cs", ".razor", ".csproj", ".props", ".json", ".md", ".config", ".css", ".js", ".html")
        }
}

function Assert-SourceDoesNotContain {
    param([string[]]$Patterns)

    $violations = foreach ($file in Get-SampleSourceFiles) {
        $content = Get-Content -LiteralPath $file.FullName -Raw
        foreach ($pattern in $Patterns) {
            if ($content.Contains($pattern, [System.StringComparison]::OrdinalIgnoreCase)) {
                "$($file.FullName): $pattern"
            }
        }
    }

    if ($violations) {
        $violations | ForEach-Object { Write-Error $_ }
        throw "Storefront.Sample source contains forbidden pattern."
    }
}

function Assert-Route {
    param(
        [string]$RelativePath,
        [string]$Route
    )

    Assert-ContainsText (Join-Path $sampleRoot $RelativePath) $Route
}

function Start-DotnetSample {
    $runtimeRoot = Join-Path $repoRoot "obj\storefront-sample-release-runtime"
    New-Item -ItemType Directory -Force -Path $runtimeRoot | Out-Null

    $startInfo = [System.Diagnostics.ProcessStartInfo]::new()
    $startInfo.FileName = "dotnet"
    $startInfo.WorkingDirectory = $repoRoot
    $startInfo.RedirectStandardOutput = $true
    $startInfo.RedirectStandardError = $true
    $startInfo.UseShellExecute = $false
    $startInfo.CreateNoWindow = $true

    foreach ($argument in @(
        "run",
        "--project",
        $sampleProject,
        "--configuration",
        $Configuration,
        "--no-build",
        "--no-launch-profile",
        "--urls",
        $SampleUrl
    )) {
        $startInfo.ArgumentList.Add($argument)
    }

    return [System.Diagnostics.Process]::Start($startInfo)
}

function Wait-ForSample {
    param([System.Diagnostics.Process]$Process)

    $deadline = [DateTimeOffset]::UtcNow.AddSeconds($RuntimeTimeoutSeconds)
    while ([DateTimeOffset]::UtcNow -lt $deadline) {
        if ($Process.HasExited) {
            $stdout = $Process.StandardOutput.ReadToEnd()
            $stderr = $Process.StandardError.ReadToEnd()
            throw "Storefront.Sample exited before route smoke. stdout: $stdout stderr: $stderr"
        }

        try {
            Invoke-WebRequest -Uri "$SampleUrl/robots.txt" -UseBasicParsing -TimeoutSec 5 | Out-Null
            return
        }
        catch {
            Start-Sleep -Milliseconds 500
        }
    }

    throw "Storefront.Sample did not become ready at $SampleUrl within $RuntimeTimeoutSeconds seconds."
}

function Assert-HttpContains {
    param(
        [string]$Path,
        [string]$Expected
    )

    $response = Invoke-WebRequest -Uri "$SampleUrl$Path" -UseBasicParsing -TimeoutSec 15
    if ($response.StatusCode -lt 200 -or $response.StatusCode -gt 399) {
        throw "Route '$Path' returned status $($response.StatusCode)."
    }

    if (-not $response.Content.Contains($Expected, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Route '$Path' did not contain expected text '$Expected'."
    }
}

Invoke-Step "Prepare local package feed" {
    New-Item -ItemType Directory -Force -Path $feedRoot | Out-Null
    New-Item -ItemType Directory -Force -Path $publishRoot | Out-Null
}

Invoke-Step "Pack Storefront.Client" {
    dotnet pack $clientProject --configuration $Configuration --no-restore --output $feedRoot "/p:PackageVersion=$StorefrontClientPackageVersion"
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}

Invoke-Step "Pack Storefront.Runtime" {
    dotnet pack $runtimeProject --configuration $Configuration --no-restore --output $feedRoot "/p:PackageVersion=$StorefrontRuntimePackageVersion"
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}

Invoke-Step "Restore Storefront.Sample from local packages" {
    dotnet restore $sampleProject
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}

Invoke-Step "Build Storefront.Sample" {
    dotnet build $sampleProject --configuration $Configuration --no-restore
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}

Invoke-Step "Publish Storefront.Sample" {
    dotnet publish $sampleProject --configuration $Configuration --no-restore --output $publishRoot
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}

Invoke-Step "Verify contract and source boundaries" {
    Assert-ContainsText $sampleProject '<PackageReference Include="BlazorShop.Storefront.Client"'
    Assert-ContainsText $sampleProject '<PackageReference Include="BlazorShop.Storefront.Runtime"'
    Assert-SourceDoesNotContain $forbiddenSourcePatterns
    Assert-ContainsText $generatedClient "IStorefrontCartClient"
    Assert-ContainsText $generatedClient "IStorefrontCheckoutClient"
    Assert-ContainsText $generatedClient "Place a COD order from a checkout session."
    Assert-DoesNotContainText $generatedClient "/providers/"
    Assert-DoesNotContainText $generatedClient "/callback"
    Assert-DoesNotContainText $generatedClient "/webhook"
}

Invoke-Step "Verify functional route and commerce conventions" {
    Assert-Route "Pages\Ssr\Home\HomePage.razor" '@page "/"'
    Assert-Route "Pages\Hybrid\Catalog\CategoryPage.razor" '@page "/category/{Slug}"'
    Assert-Route "Pages\Hybrid\Catalog\ProductPage.razor" '@page "/product/{Slug}"'
    Assert-Route "Pages\Hybrid\Commerce\CartPage.razor" '@page "/cart"'
    Assert-Route "Pages\Hybrid\Commerce\CheckoutPage.razor" '@page "/checkout"'
    Assert-Route "Pages\Hybrid\Commerce\PaymentResultPage.razor" '@page "/payment/result"'
    Assert-Route "Pages\Ssr\Auth\AuthShellPage.razor" '@page "/signin"'
    Assert-Route "Pages\WasmHost\Account\AccountHostPage.razor" '@page "/account"'
    Assert-Route "Pages\Ssr\System\MaintenancePage.razor" '@page "/maintenance"'
    Assert-Route "Pages\Ssr\System\NotFoundPage.razor" '@page "/not-found"'
    Assert-ContainsText (Join-Path $sampleRoot "Services\StorefrontBootstrapService.cs") "GetCurrentAsync"
    Assert-ContainsText (Join-Path $sampleRoot "Services\StorefrontBootstrapService.cs") "QueryProductsAsync"
    Assert-ContainsText (Join-Path $sampleRoot "Components\Catalog\PurchasePanelPlaceholder.razor") "Add to cart"
    Assert-ContainsText (Join-Path $sampleRoot "Components\Commerce\CartLineList.razor") "Remove"
    Assert-ContainsText (Join-Path $sampleRoot "Components\Commerce\CheckoutStepShell.razor") "COD payment"
    Assert-ContainsText (Join-Path $sampleRoot "Components\Account\AccountShell.razor") "Profile"
    Assert-ContainsText (Join-Path $sampleRoot "Components\Account\AccountShell.razor") "Addresses"
    Assert-ContainsText (Join-Path $sampleRoot "Components\Account\AccountShell.razor") "Orders"
    Assert-ContainsText (Join-Path $sampleRoot "Features\feature-manifest.json") '"checkout"'
    Assert-ContainsText (Join-Path $sampleRoot "Features\feature-manifest.json") '"payments"'
}

Invoke-Step "Verify rendering, SEO, and hydration conventions" {
    Assert-ContainsText (Join-Path $sampleRoot "Components\App.razor") "<HeadOutlet />"
    Assert-ContainsText (Join-Path $sampleRoot "Pages\Hybrid\Catalog\ProductPage.razor") '<link rel="canonical"'
    Assert-ContainsText (Join-Path $sampleRoot "Pages\Hybrid\Catalog\ProductPage.razor") 'application/ld+json'
    Assert-ContainsText (Join-Path $sampleRoot "Pages\Hybrid\Catalog\CategoryPage.razor") '<link rel="canonical"'
    Assert-ContainsText (Join-Path $sampleRoot "Pages\Ssr\System\MaintenancePage.razor") "noindex"
    Assert-ContainsText (Join-Path $sampleRoot "Pages\WasmHost\Account\AccountHostPage.razor") "noindex"
    Assert-ContainsText (Join-Path $sampleRoot "Composition\StarterHydrationMode.cs") "InitialSnapshot"
    Assert-ContainsText (Join-Path $sampleRoot "Composition\StarterHydrationMode.cs") "ShouldFetchOnFirstLoad"
    Assert-ContainsText (Join-Path $sampleRoot "Pages\README.md") "must not duplicate the first fetch"
    Assert-ContainsText (Join-Path $sampleRoot "Endpoints\StarterSeoEndpoints.cs") '"/robots.txt"'
    Assert-ContainsText (Join-Path $sampleRoot "Endpoints\StarterSeoEndpoints.cs") '"/sitemap.xml"'
}

Invoke-Step "Verify security and browser boundaries" {
    Assert-ContainsText (Join-Path $sampleRoot "Endpoints\StarterBffEndpoints.cs") "ValidateRequestAsync"
    Assert-ContainsText (Join-Path $sampleRoot "Endpoints\StarterBffEndpoints.cs") "security.csrf"
    Assert-ContainsText (Join-Path $sampleRoot "Endpoints\StarterBffEndpoints.cs") "HttpOnly = true"
    Assert-ContainsText (Join-Path $sampleRoot "Endpoints\StarterBffEndpoints.cs") "SameSite = SameSiteMode.Lax"
    Assert-ContainsText (Join-Path $sampleRoot "Endpoints\StarterBffEndpoints.cs") '"/api/cart/lines"'
    Assert-ContainsText (Join-Path $sampleRoot "Security\StarterReturnUrlValidator.cs") "IsSafeLocalReturnUrl"
    Assert-ContainsText (Join-Path $sampleRoot "Pages\Ssr\Auth\AuthShellPage.razor") "security.return_url"
    Assert-ContainsText (Join-Path $sampleRoot "Pages\Hybrid\Commerce\CartPage.razor") "same-origin /api/cart/*"
    Assert-ContainsText (Join-Path $repoRoot "BlazorShop.PresentationV2\BlazorShop.Storefront.Runtime\StorefrontRuntimeError.cs") "StorefrontRuntimeErrorMapper"
    Assert-ContainsText (Join-Path $repoRoot "BlazorShop.Tests.V2\Architecture\StorefrontStarterFoundationBoundaryTests.cs") "[InlineData(401"
    Assert-ContainsText (Join-Path $repoRoot "BlazorShop.Tests.V2\Architecture\StorefrontStarterFoundationBoundaryTests.cs") "[InlineData(403"
    Assert-ContainsText (Join-Path $repoRoot "BlazorShop.Tests.V2\Architecture\StorefrontStarterFoundationBoundaryTests.cs") "[InlineData(409"
    Assert-ContainsText (Join-Path $repoRoot "BlazorShop.Tests.V2\Architecture\StorefrontStarterFoundationBoundaryTests.cs") "[InlineData(422"

    $browserRoots = @(
        (Join-Path $sampleRoot "Components"),
        (Join-Path $sampleRoot "Pages"),
        (Join-Path $sampleRoot "wwwroot")
    )

    $violations = foreach ($root in $browserRoots) {
        Get-ChildItem -LiteralPath $root -Recurse -File |
            Where-Object { $_.Extension -in @(".razor", ".css", ".js", ".html") } |
            ForEach-Object {
                $content = Get-Content -LiteralPath $_.FullName -Raw
                foreach ($pattern in $forbiddenBrowserPatterns) {
                    if ($content.Contains($pattern, [System.StringComparison]::OrdinalIgnoreCase)) {
                        "$($_.FullName): $pattern"
                    }
                }
            }
    }

    if ($violations) {
        $violations | ForEach-Object { Write-Error $_ }
        throw "Browser-facing Sample output contains a forbidden runtime secret or Commerce Node URL."
    }
}

Invoke-Step "Verify performance guard conventions" {
    Assert-ContainsText (Join-Path $sampleRoot "Pages\Hybrid\Catalog\ProductPage.razor") "StarterHydrationMode.InitialSnapshot"
    Assert-ContainsText (Join-Path $sampleRoot "Components\Catalog\PurchasePanelPlaceholder.razor") "disabled"
    Assert-ContainsText (Join-Path $sampleRoot "Pages\WasmHost\Account\AccountHostPage.razor") "StarterHydrationMode.BrowserFetch"
    Assert-ContainsText $todoFile "account assemblies are not loaded on public pages unless required by Blazor packaging constraints"
}

if ($SkipRuntime) {
    Write-Host "Runtime route smoke skipped by request."
}
else {
    Invoke-Step "Run Storefront.Sample route smoke" {
        $process = Start-DotnetSample
        try {
            Wait-ForSample $process
            Assert-HttpContains "/robots.txt" "Sitemap:"
            Assert-HttpContains "/sitemap.xml" "<urlset"
            Assert-HttpContains "/product/sample-product" "Product detail"
            Assert-HttpContains "/category/sample-category" "Category"
            Assert-HttpContains "/cart" "Cart"
            Assert-HttpContains "/checkout" "Checkout"
            Assert-HttpContains "/account" "Account"
            Assert-HttpContains "/maintenance" "Maintenance"
            Assert-HttpContains "/not-found" "Not found"
        }
        finally {
            if ($process -and -not $process.HasExited) {
                $process.Kill($true)
                $process.WaitForExit(5000) | Out-Null
            }

            if ($process) {
                $process.Dispose()
            }
        }
    }
}

Write-Host "Storefront Sample release gate passed."

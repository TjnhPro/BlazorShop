param(
    [string]$Name = "BlazorShop.Storefront.GeneratedProof",
    [string]$StoreKey = "default",
    [string]$Url = "https://reference.example",
    [string]$OutputRoot = "artifacts/storefront-builder/generated",
    [string]$Configuration = "Debug",
    [string]$CommerceNodeBaseUrl = "http://localhost:5180",
    [string]$PublicBaseUrl = "http://localhost:18620",
    [string]$ProofUrl = "http://127.0.0.1:18620",
    [int]$RuntimeTimeoutSeconds = 45,
    [string]$StorefrontClientPackageVersion = "1.0.0-local",
    [string]$StorefrontRuntimePackageVersion = "1.0.0-local",
    [string]$StorefrontPresentationPackageVersion = "1.0.0-local",
    [string]$StorefrontComponentsPackageVersion = "1.0.0-local",
    [ValidateSet("Structure", "FoundationFunctionalFast", "FoundationFunctionalFull", "FoundationFunctional")]
    [string]$ProofLevel = "Structure",
    [string]$FixtureCategorySlug = "apparel",
    [string]$FixtureProductSlug = "qa-simple-product-100",
    [string]$FixturePageSlug = "customer-service",
    [string]$RequiredPaymentMethodKey = "cod",
    [switch]$RunBrowserQa,
    [switch]$Describe
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$toolRoot = Join-Path $repoRoot "tools\BlazorShop.AI.StorefrontBuilder"
$packageRoot = Join-Path $repoRoot "artifacts\storefront-packages"
$clientProject = Join-Path $repoRoot "BlazorShop.PresentationV2\BlazorShop.Storefront.Client\BlazorShop.Storefront.Client.csproj"
$runtimeProject = Join-Path $repoRoot "BlazorShop.PresentationV2\BlazorShop.Storefront.Runtime\BlazorShop.Storefront.Runtime.csproj"
$presentationProject = Join-Path $repoRoot "BlazorShop.PresentationV2\BlazorShop.Storefront.Presentation\BlazorShop.Storefront.Presentation.csproj"
$componentsProject = Join-Path $repoRoot "BlazorShop.PresentationV2\BlazorShop.Storefront.Components\BlazorShop.Storefront.Components.csproj"

function Resolve-RepoPath {
    param([string]$Path)

    if ([System.IO.Path]::IsPathRooted($Path)) {
        return [System.IO.Path]::GetFullPath($Path)
    }

    return [System.IO.Path]::GetFullPath((Join-Path $repoRoot $Path))
}

function Invoke-Step {
    param(
        [string]$StepName,
        [scriptblock]$Action
    )

    Write-Host "== $StepName =="
    & $Action
}

function Assert-UnderRoot {
    param(
        [string]$Path,
        [string]$Root
    )

    $resolvedPath = [System.IO.Path]::GetFullPath($Path)
    $resolvedRoot = [System.IO.Path]::GetFullPath($Root)
    if (-not $resolvedPath.StartsWith($resolvedRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "[SFB-PROOF-001] Refusing to clean outside generated output root: $resolvedPath"
    }
}

function Clear-StorefrontLocalPackageCache {
    $globalPackageRoot = Join-Path ([Environment]::GetFolderPath("UserProfile")) ".nuget\packages"
    foreach ($package in @("blazorshop.storefront.client", "blazorshop.storefront.runtime", "blazorshop.storefront.presentation", "blazorshop.storefront.components")) {
        $versionPath = Join-Path $globalPackageRoot "$package\$StorefrontClientPackageVersion"
        if ($package -eq "blazorshop.storefront.runtime") {
            $versionPath = Join-Path $globalPackageRoot "$package\$StorefrontRuntimePackageVersion"
        }
        elseif ($package -eq "blazorshop.storefront.presentation") {
            $versionPath = Join-Path $globalPackageRoot "$package\$StorefrontPresentationPackageVersion"
        }
        elseif ($package -eq "blazorshop.storefront.components") {
            $versionPath = Join-Path $globalPackageRoot "$package\$StorefrontComponentsPackageVersion"
        }

        if (Test-Path $versionPath) {
            Remove-Item -LiteralPath $versionPath -Recurse -Force
        }
    }
}

function Start-ProofStorefront {
    param([string]$ProjectFile)

    $startInfo = [System.Diagnostics.ProcessStartInfo]::new()
    $startInfo.FileName = "dotnet"
    $startInfo.WorkingDirectory = $repoRoot
    $startInfo.RedirectStandardOutput = $false
    $startInfo.RedirectStandardError = $false
    $startInfo.UseShellExecute = $false
    $startInfo.CreateNoWindow = $true
    $startInfo.Environment["ASPNETCORE_ENVIRONMENT"] = "Development"

    foreach ($argument in @(
        "run",
        "--project",
        $ProjectFile,
        "--configuration",
        $Configuration,
        "--no-build",
        "--no-launch-profile",
        "--urls",
        $ProofUrl
    )) {
        $startInfo.ArgumentList.Add($argument)
    }

    return [System.Diagnostics.Process]::Start($startInfo)
}

function Wait-ForProofStorefront {
    param([System.Diagnostics.Process]$Process)

    $deadline = [DateTimeOffset]::UtcNow.AddSeconds($RuntimeTimeoutSeconds)
    while ([DateTimeOffset]::UtcNow -lt $deadline) {
        if ($Process.HasExited) {
            throw "Generated proof exited before browser QA with exit code $($Process.ExitCode)."
        }

        try {
            Invoke-WebRequest -Uri "$ProofUrl/robots.txt" -UseBasicParsing -TimeoutSec 5 -SkipHttpErrorCheck | Out-Null
            return
        }
        catch {
            Start-Sleep -Milliseconds 500
        }
    }

    throw "Generated proof did not become ready at $ProofUrl within $RuntimeTimeoutSeconds seconds."
}

function Get-EnvelopeData {
    param($Response)

    if ($null -eq $Response) {
        return $null
    }

    if ($null -ne $Response.data) {
        return $Response.data
    }

    if ($null -ne $Response.Data) {
        return $Response.Data
    }

    return $Response
}

function Invoke-StorefrontFixtureEndpoint {
    param([string]$Path)

    $uri = "$($CommerceNodeBaseUrl.TrimEnd('/'))/api/storefront/stores/$([uri]::EscapeDataString($StoreKey))/$Path"
    try {
        return Get-EnvelopeData (Invoke-RestMethod -Uri $uri -Method Get -TimeoutSec 15)
    }
    catch {
        throw "[SFB-PROOF-FIXTURE-001] Fixture endpoint failed: $uri. Start Commerce Node with the '$StoreKey' fixture store before running -ProofLevel FoundationFunctionalFull. $($_.Exception.Message)"
    }
}

function Assert-StorefrontFixtureData {
    param([bool]$RequirePaymentMethod = $false)

    Write-Host "Checking fixture store '$StoreKey' at $CommerceNodeBaseUrl"

    $configuration = Invoke-StorefrontFixtureEndpoint "configuration"
    if ($null -eq $configuration) {
        throw "[SFB-PROOF-FIXTURE-002] Fixture store '$StoreKey' is missing public configuration."
    }

    $categories = @(Invoke-StorefrontFixtureEndpoint "catalog/categories")
    if ($categories.Count -lt 1) {
        throw "[SFB-PROOF-FIXTURE-003] Fixture store '$StoreKey' must expose at least one published category."
    }

    $category = Invoke-StorefrontFixtureEndpoint "catalog/categories/slug/$([uri]::EscapeDataString($FixtureCategorySlug))"
    if ($null -eq $category) {
        throw "[SFB-PROOF-FIXTURE-004] Fixture category '$FixtureCategorySlug' is missing."
    }

    $product = Invoke-StorefrontFixtureEndpoint "catalog/products/slug/$([uri]::EscapeDataString($FixtureProductSlug))"
    if ($null -eq $product) {
        throw "[SFB-PROOF-FIXTURE-005] Fixture product '$FixtureProductSlug' is missing."
    }

    $mediaGallery = @($product.mediaGallery)
    $hasImage = -not [string]::IsNullOrWhiteSpace([string]$product.image)
    if (-not $hasImage) {
        foreach ($media in $mediaGallery) {
            if (-not [string]::IsNullOrWhiteSpace([string]$media.imageUrl) -or -not [string]::IsNullOrWhiteSpace([string]$media.thumbnailUrl)) {
                $hasImage = $true
                break
            }
        }
    }

    if (-not $hasImage) {
        throw "[SFB-PROOF-FIXTURE-006] Fixture product '$FixtureProductSlug' must include a product image or media gallery item."
    }

    if ($product.purchasable -ne $true -or ($null -ne $product.inStock -and $product.inStock -ne $true)) {
        throw "[SFB-PROOF-FIXTURE-007] Fixture product '$FixtureProductSlug' must be published, purchasable, and in stock."
    }

    $page = Invoke-StorefrontFixtureEndpoint "pages/$([uri]::EscapeDataString($FixturePageSlug))"
    if ($null -eq $page) {
        throw "[SFB-PROOF-FIXTURE-008] Fixture content page '$FixturePageSlug' is missing."
    }

    if ($RequirePaymentMethod) {
        $paymentMethods = @(Invoke-StorefrontFixtureEndpoint "payments/methods")
        $matchingMethod = $paymentMethods | Where-Object {
            [string]$_.key -eq $RequiredPaymentMethodKey -or [string]$_.providerKey -eq $RequiredPaymentMethodKey
        } | Select-Object -First 1
        if ($null -eq $matchingMethod) {
            throw "[SFB-PROOF-FIXTURE-009] Fixture store '$StoreKey' must expose '$RequiredPaymentMethodKey' payment capability before full functional proof."
        }
    }
}

function Get-ProofFileHashes {
    param([string]$Root)

    $hashes = @{}
    Get-ChildItem -LiteralPath $Root -Recurse -File |
        Where-Object {
            $relative = [System.IO.Path]::GetRelativePath($Root, $_.FullName).Replace("\", "/")
            $relative -notmatch "(^|/)(bin|obj|\.regeneration-staging|\.regeneration-backup)/"
        } |
        ForEach-Object {
            $relative = [System.IO.Path]::GetRelativePath($Root, $_.FullName).Replace("\", "/")
            $content = (Get-Content -LiteralPath $_.FullName -Raw).Replace("`r`n", "`n").Replace("`r", "`n")
            $bytes = [System.Text.Encoding]::UTF8.GetBytes($content)
            $hash = [System.Security.Cryptography.SHA256]::HashData($bytes)
            $hashes[$relative] = [System.BitConverter]::ToString($hash).Replace("-", "").ToLowerInvariant()
        }

    return $hashes
}

function Compare-ProofFileHashes {
    param(
        [hashtable]$Before,
        [hashtable]$After
    )

    $changes = [System.Collections.Generic.List[string]]::new()
    foreach ($key in ($Before.Keys + $After.Keys | Sort-Object -Unique)) {
        if (-not $Before.ContainsKey($key)) {
            $changes.Add("created $key")
        }
        elseif (-not $After.ContainsKey($key)) {
            $changes.Add("deleted $key")
        }
        elseif ($Before[$key] -ne $After[$key]) {
            $changes.Add("modified $key")
        }
    }

    return $changes
}

function Invoke-GeneratedProofRegenerationLifecycle {
    $regenerator = Join-Path $toolRoot "regenerate-storefront.ps1"
    $manifestPath = Join-Path $projectRoot "docs\storefront-analysis\generated-files.yaml"
    $manualConflictFile = Join-Path $projectRoot "Components\Catalog\PurchasePanelPlaceholder.razor"

    Invoke-Step "Run post-regeneration build proof" {
        & $regenerator -ProjectRoot $projectRoot -Scope all -ValidateAfterApply -BuildAfterApply
    }

    Invoke-Step "Run regenerate no-op proof" {
        $before = Get-ProofFileHashes -Root $projectRoot
        & $regenerator -ProjectRoot $projectRoot -Scope all -ValidateAfterApply -BuildAfterApply
        $after = Get-ProofFileHashes -Root $projectRoot
        $diff = Compare-ProofFileHashes -Before $before -After $after
        if ($diff.Count -gt 0) {
            throw "[SFB-PROOF-REGEN-001] No-op regeneration changed files: $($diff -join ', ')"
        }
    }

    Invoke-Step "Run manual-edit conflict fixture proof" {
        $original = Get-Content -LiteralPath $manualConflictFile -Raw
        try {
            Add-Content -LiteralPath $manualConflictFile -Value "`n<!-- StorefrontBuilder manual conflict proof -->"
            & $regenerator -ProjectRoot $projectRoot -Scope conflicts
            $manifest = Get-Content -LiteralPath $manifestPath -Raw
            foreach ($marker in @(
                "Components/Catalog/PurchasePanelPlaceholder.razor",
                "manualEditDetected: true",
                "conflictStatus: manual-edit"
            )) {
                if (-not $manifest.Contains($marker, [System.StringComparison]::Ordinal)) {
                    throw "[SFB-PROOF-REGEN-002] Manual-edit conflict proof did not record marker '$marker'."
                }
            }
        }
        finally {
            [System.IO.File]::WriteAllText($manualConflictFile, $original, [System.Text.UTF8Encoding]::new($false))
        }

        & $regenerator -ProjectRoot $projectRoot -Scope all -ValidateAfterApply -BuildAfterApply
    }
}

$generatedRoot = Resolve-RepoPath $OutputRoot
$projectRoot = Join-Path $generatedRoot $Name
$projectFile = Join-Path $projectRoot "$Name.csproj"

if ($Describe) {
    Write-Host "StorefrontBuilder generated proof workflow"
    Write-Host "- Proof levels: Structure, FoundationFunctionalFast, FoundationFunctionalFull"
    Write-Host "- Clean $projectRoot"
    Write-Host "- Pack Storefront.Client, Storefront.Runtime, Storefront.Presentation, and Storefront.Components"
    Write-Host "- Generate $Name from Storefront.Starter"
    Write-Host "- Write StorefrontBuilder review, asset, CSS, and generated-file artifacts"
    Write-Host "- Restore/build generated proof from local packages"
    Write-Host "- Run static validation, isolation, and shared visual boundary gates"
    Write-Host "- Run post-regeneration validate/build proof"
    Write-Host "- Run deterministic no-op regeneration proof"
    Write-Host "- Run manual-edit conflict fixture proof"
    Write-Host "- Structure proof stops after project/package/boundary/regeneration lifecycle validation"
    Write-Host "- FoundationFunctionalFast uses deterministic generated markup plus mocked same-origin Presentation BFF routes to exercise browser commerce behavior"
    Write-Host "- FoundationFunctionalFull adds visual smoke QA and full payment capability fixture checks for manual/scheduled/release gates"
    Write-Host "- FoundationFunctional remains a compatibility alias for FoundationFunctionalFull"
    Write-Host "- -RunBrowserQa is treated as -ProofLevel FoundationFunctionalFull for compatibility"
    exit 0
}

Assert-UnderRoot $projectRoot $generatedRoot

Invoke-Step "Clean generated proof output" {
    if (Test-Path $projectRoot) {
        Remove-Item -LiteralPath $projectRoot -Recurse -Force
    }
}

Invoke-Step "Prepare local package feed" {
    New-Item -ItemType Directory -Force -Path $packageRoot | Out-Null
    Clear-StorefrontLocalPackageCache
}

Invoke-Step "Pack Storefront.Client" {
    dotnet pack $clientProject --configuration $Configuration --output $packageRoot "/p:PackageVersion=$StorefrontClientPackageVersion"
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}

Invoke-Step "Pack Storefront.Runtime" {
    dotnet pack $runtimeProject --configuration $Configuration --output $packageRoot "/p:PackageVersion=$StorefrontRuntimePackageVersion"
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}

Invoke-Step "Pack Storefront.Presentation" {
    dotnet pack $presentationProject --configuration $Configuration --output $packageRoot "/p:PackageVersion=$StorefrontPresentationPackageVersion"
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}

Invoke-Step "Pack Storefront.Components" {
    dotnet pack $componentsProject --configuration $Configuration --output $packageRoot "/p:PackageVersion=$StorefrontComponentsPackageVersion"
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}

Invoke-Step "Generate proof storefront" {
    & "$toolRoot\scripts\generate\new-storefront-project.ps1" `
        -Name $Name `
        -StoreKey $StoreKey `
        -OutputRoot $OutputRoot `
        -CommerceNodeBaseUrl $CommerceNodeBaseUrl `
        -PublicBaseUrl $PublicBaseUrl `
        -Force
}

Invoke-Step "Write StorefrontBuilder artifacts" {
    node "$toolRoot\scripts\generate\write-review-artifacts.mjs" --project-root $projectRoot --url $Url
    node "$toolRoot\scripts\generate\build-asset-manifest.mjs" --project-root $projectRoot
    node "$toolRoot\scripts\generate\apply-visual-foundation.mjs" --project-root $projectRoot
    node "$toolRoot\scripts\generate\apply-composition.mjs" --project-root $projectRoot
    node "$toolRoot\scripts\generate\update-generated-files-manifest.mjs" --project-root $projectRoot
}

Invoke-Step "Restore generated proof" {
    dotnet restore $projectFile --no-cache --force-evaluate
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}

Invoke-Step "Build generated proof" {
    dotnet build $projectFile --configuration $Configuration --no-restore
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}

Invoke-Step "Run static StorefrontBuilder validation" {
    & "$toolRoot\validate-storefront.ps1" -ProjectRoot $projectRoot -Name $Name -StoreKey $StoreKey
}

Invoke-Step "Run StorefrontBuilder isolation gate" {
    & "$PSScriptRoot\run-storefront-builder-isolation-gate.ps1" `
        -ProjectRoot $projectRoot `
        -Name $Name `
        -Configuration $Configuration `
        -StorefrontClientPackageVersion $StorefrontClientPackageVersion `
        -StorefrontRuntimePackageVersion $StorefrontRuntimePackageVersion `
        -StorefrontPresentationPackageVersion $StorefrontPresentationPackageVersion `
        -StorefrontComponentsPackageVersion $StorefrontComponentsPackageVersion
}

Invoke-Step "Run shared visual consumer boundary validator" {
    dotnet test "$repoRoot\BlazorShop.Tests.V2\BlazorShop.Tests.V2.csproj" --filter "FullyQualifiedName~StorefrontVisualConsumerBoundaryValidatorTests.F1_51_SharedValidator_PassesGeneratedProofWhenPresent" -v:minimal
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}

Invoke-GeneratedProofRegenerationLifecycle

$runFastFunctionalProof = $ProofLevel -eq "FoundationFunctionalFast"
$runLiveFunctionalProof = $RunBrowserQa -or $ProofLevel -in @("FoundationFunctionalFull", "FoundationFunctional")
if ($runFastFunctionalProof) {
    Invoke-Step "Run fast foundation functional browser proof" {
        node "$toolRoot\scripts\qa\run-fast-foundation-functional.mjs" --project-root $projectRoot
        if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    }
}

if ($runLiveFunctionalProof) {
    Invoke-Step "Check fixture data for foundation functional proof" {
        Assert-StorefrontFixtureData -RequirePaymentMethod:$true
    }

    Invoke-Step "Run foundation functional browser proof" {
        $process = Start-ProofStorefront $projectFile
        try {
            Wait-ForProofStorefront $process
            if ($runLiveFunctionalProof) {
                node "$toolRoot\scripts\qa\run-visual-qa.mjs" --base-url $ProofUrl --project-root $projectRoot --category-slug $FixtureCategorySlug --product-slug $FixtureProductSlug
                if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
            }

            node "$toolRoot\scripts\qa\run-commerce-regression.mjs" --base-url $ProofUrl --project-root $projectRoot --category-slug $FixtureCategorySlug --product-slug $FixtureProductSlug --page-slug $FixturePageSlug
            if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
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

Write-Host "StorefrontBuilder generated proof completed at $projectRoot."

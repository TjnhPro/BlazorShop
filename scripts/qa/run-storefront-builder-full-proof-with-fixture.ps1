[CmdletBinding()]
param(
    [string]$Name = "BlazorShop.Storefront.GeneratedProof",
    [string]$StoreKey = "default",
    [string]$EnvFile = "scripts/env/v2-local.env",
    [string]$OutputRoot = "artifacts/storefront-builder/generated",
    [string]$Configuration = "Debug",
    [string]$CommerceNodeBaseUrl = "http://localhost:5180",
    [string]$PublicBaseUrl = "http://localhost:18620",
    [string]$ProofUrl = "http://127.0.0.1:18620",
    [int]$RuntimeTimeoutSeconds = 180,
    [string]$FixtureCategorySlug = "apparel",
    [string]$FixtureProductSlug = "qa-simple-product-100",
    [string]$FixturePageSlug = "customer-service",
    [string]$RequiredPaymentMethodKey = "cod",
    [switch]$KeepRuntime,
    [switch]$KeepDocker,
    [switch]$Describe
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$runV2Local = Join-Path $repoRoot "scripts\run-v2-local.ps1"
$stopV2Local = Join-Path $repoRoot "scripts\stop-v2-local.ps1"
$generatedProof = Join-Path $PSScriptRoot "run-storefront-builder-generated-proof.ps1"
$logDir = Join-Path $repoRoot ".gstack\run-v2-local"

function Resolve-RepoPath {
    param([string]$Path)

    if ([System.IO.Path]::IsPathRooted($Path)) {
        return [System.IO.Path]::GetFullPath($Path)
    }

    return [System.IO.Path]::GetFullPath((Join-Path $repoRoot $Path))
}

function Read-EnvFile {
    param([string]$Path)

    $values = [ordered]@{}
    if (-not (Test-Path -LiteralPath $Path)) {
        return $values
    }

    foreach ($line in Get-Content -LiteralPath $Path) {
        $trimmed = $line.Trim()
        if ($trimmed.Length -eq 0 -or $trimmed.StartsWith("#")) {
            continue
        }

        $separator = $trimmed.IndexOf("=")
        if ($separator -lt 1) {
            continue
        }

        $key = $trimmed.Substring(0, $separator).Trim()
        $value = $trimmed.Substring($separator + 1).Trim()
        if (($value.StartsWith('"') -and $value.EndsWith('"')) -or ($value.StartsWith("'") -and $value.EndsWith("'"))) {
            $value = $value.Substring(1, $value.Length - 2)
        }

        $values[$key] = $value
    }

    return $values
}

function Get-EnvValue {
    param(
        [hashtable]$Values,
        [string]$Key,
        [string]$Default = ""
    )

    if ($Values.Contains($Key) -and -not [string]::IsNullOrWhiteSpace([string]$Values[$Key])) {
        return [string]$Values[$Key]
    }

    return $Default
}

function Invoke-Step {
    param(
        [string]$StepName,
        [scriptblock]$Action
    )

    Write-Host "== $StepName =="
    & $Action
}

function Wait-HttpOk {
    param(
        [string]$Url,
        [hashtable]$Headers = @{},
        [int]$TimeoutSeconds = 120
    )

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    $lastError = $null
    while ((Get-Date) -lt $deadline) {
        try {
            $response = Invoke-WebRequest -Uri $Url -Headers $Headers -TimeoutSec 5 -UseBasicParsing
            if ($response.StatusCode -ge 200 -and $response.StatusCode -lt 500) {
                return
            }
        }
        catch {
            $lastError = $_.Exception.Message
            Start-Sleep -Milliseconds 750
        }
    }

    throw "[SFB-FULL-PROOF-HEALTH-001] Commerce Node fixture runtime did not become healthy at $Url within $TimeoutSeconds seconds. Problem: health endpoint unavailable. Cause: Docker dependencies, migrations, or CommerceNode API startup may have failed. Fix: inspect '$logDir' process logs and rerun this wrapper. Last error: $lastError"
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
        return Get-EnvelopeData (Invoke-RestMethod -Uri $uri -Method Get -TimeoutSec 20)
    }
    catch {
        throw "[SFB-FULL-PROOF-FIXTURE-001] Fixture endpoint failed: $uri. Problem: full proof cannot verify the '$StoreKey' store fixture. Cause: fixture store data is missing or Commerce Node is not serving Storefront APIs. Fix: ensure scripts/run-v2-local.ps1 can bootstrap the default store and inspect '$logDir'. $($_.Exception.Message)"
    }
}

function Assert-StorefrontFixtureData {
    Write-Host "Checking fixture store '$StoreKey' at $CommerceNodeBaseUrl"

    $configuration = Invoke-StorefrontFixtureEndpoint "configuration"
    if ($null -eq $configuration) {
        throw "[SFB-FULL-PROOF-FIXTURE-002] Missing public configuration for fixture store '$StoreKey'. Problem: generated proof has no store configuration. Cause: store registry/bootstrap did not publish Storefront configuration. Fix: rerun scripts/run-v2-local.ps1 -StopExisting -NoOpenBrowser and inspect ControlPlane/CommerceNode logs."
    }

    $categories = @(Invoke-StorefrontFixtureEndpoint "catalog/categories")
    if ($categories.Count -lt 1) {
        throw "[SFB-FULL-PROOF-FIXTURE-003] Missing published categories for fixture store '$StoreKey'. Problem: catalog browser proof has no category fixture. Cause: Commerce Node development seed did not create fixture catalog data. Fix: verify Development seeding and the '$StoreKey' store fixture."
    }

    $category = Invoke-StorefrontFixtureEndpoint "catalog/categories/slug/$([uri]::EscapeDataString($FixtureCategorySlug))"
    if ($null -eq $category) {
        throw "[SFB-FULL-PROOF-FIXTURE-004] Missing fixture category '$FixtureCategorySlug'. Problem: category route proof cannot run. Cause: configured fixture slug does not exist. Fix: update fixture seed data or pass -FixtureCategorySlug."
    }

    $product = Invoke-StorefrontFixtureEndpoint "catalog/products/slug/$([uri]::EscapeDataString($FixtureProductSlug))"
    if ($null -eq $product) {
        throw "[SFB-FULL-PROOF-FIXTURE-005] Missing fixture product '$FixtureProductSlug'. Problem: product detail and commerce regression proof cannot run. Cause: configured fixture slug does not exist. Fix: update fixture seed data or pass -FixtureProductSlug."
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
        throw "[SFB-FULL-PROOF-FIXTURE-006] Fixture product '$FixtureProductSlug' has no image/media URL. Problem: visual proof cannot verify product media rendering. Cause: fixture media seed is incomplete. Fix: add a product image/media gallery item to the fixture product."
    }

    if ($product.purchasable -ne $true -or ($null -ne $product.inStock -and $product.inStock -ne $true)) {
        throw "[SFB-FULL-PROOF-FIXTURE-007] Fixture product '$FixtureProductSlug' is not purchasable and in stock. Problem: cart/checkout regression cannot run. Cause: fixture product publish, inventory, or purchasable flags are wrong. Fix: repair the Commerce Node fixture seed."
    }

    $page = Invoke-StorefrontFixtureEndpoint "pages/$([uri]::EscapeDataString($FixturePageSlug))"
    if ($null -eq $page) {
        throw "[SFB-FULL-PROOF-FIXTURE-008] Missing fixture page '$FixturePageSlug'. Problem: content page route proof cannot run. Cause: configured fixture page slug does not exist. Fix: update fixture seed data or pass -FixturePageSlug."
    }

    $paymentMethods = @(Invoke-StorefrontFixtureEndpoint "payments/methods")
    $matchingMethod = $paymentMethods | Where-Object {
        [string]$_.key -eq $RequiredPaymentMethodKey -or [string]$_.providerKey -eq $RequiredPaymentMethodKey
    } | Select-Object -First 1
    if ($null -eq $matchingMethod) {
        throw "[SFB-FULL-PROOF-FIXTURE-009] Missing '$RequiredPaymentMethodKey' payment method. Problem: checkout regression proof cannot submit the test payment path. Cause: payment fixture is disabled or absent. Fix: enable the COD/test payment method for store '$StoreKey' or pass -RequiredPaymentMethodKey."
    }
}

function Write-ProofSummary {
    param(
        [string]$Status,
        [string]$ErrorMessage = ""
    )

    $generatedRoot = Resolve-RepoPath $OutputRoot
    $projectRoot = Join-Path $generatedRoot $Name
    $analysisDir = Join-Path $projectRoot "docs\storefront-analysis"
    if (-not (Test-Path -LiteralPath $analysisDir)) {
        New-Item -ItemType Directory -Path $analysisDir -Force | Out-Null
    }

    $summaryPath = Join-Path $analysisDir "full-proof-with-fixture-report.md"
    $lines = @(
        "# StorefrontBuilder Full Proof With Fixture",
        "",
        "- Status: $Status",
        "- Store key: $StoreKey",
        "- Commerce Node: $CommerceNodeBaseUrl",
        "- Generated proof URL: $ProofUrl",
        "- Public base URL: $PublicBaseUrl",
        "- Fixture category: $FixtureCategorySlug",
        "- Fixture product: $FixtureProductSlug",
        "- Fixture page: $FixturePageSlug",
        "- Payment method: $RequiredPaymentMethodKey",
        "- Runtime logs: .gstack/run-v2-local/",
        "- Visual QA report: docs/storefront-analysis/visual-qa-report.md",
        "- Commerce regression report: docs/storefront-analysis/functional-commerce-report.md"
    )

    if (-not [string]::IsNullOrWhiteSpace($ErrorMessage)) {
        $lines += @(
            "",
            "## Failure",
            "",
            $ErrorMessage
        )
    }

    $utf8NoBom = [System.Text.UTF8Encoding]::new($false)
    [System.IO.File]::WriteAllText($summaryPath, ($lines -join "`n") + "`n", $utf8NoBom)
    Write-Host "Full proof summary: $summaryPath"
}

$envFilePath = Resolve-RepoPath $EnvFile
$envValues = Read-EnvFile $envFilePath
$CommerceNodeBaseUrl = Get-EnvValue $envValues "RUN__COMMERCE_NODE_API_URL" $CommerceNodeBaseUrl
$StoreKey = Get-EnvValue $envValues "STOREFRONT_V2__StoreKey" (Get-EnvValue $envValues "STOREFRONT_V2__Api__StoreKey" $StoreKey)
$nodeKey = Get-EnvValue $envValues "COMMERCENODE_API__CommerceNode__NodeKey" "dev-node"
$nodeSecret = Get-EnvValue $envValues "COMMERCENODE_API__CommerceNode__NodeSecret" "dev-node-secret"
$healthHeaders = @{
    "X-Node-Key" = $nodeKey
    "X-Node-Secret" = $nodeSecret
}
$healthUrl = "$($CommerceNodeBaseUrl.TrimEnd('/'))/api/commerce/healthz"

if ($Describe) {
    Write-Host "StorefrontBuilder full proof with fixture workflow"
    Write-Host "- Stops any existing V2 local runtime."
    Write-Host "- Starts Docker dependencies and Control Plane API/Web, Commerce Node API, and Storefront V2 through scripts/run-v2-local.ps1 -StopExisting -NoOpenBrowser."
    Write-Host "- Waits for Commerce Node health: $healthUrl."
    Write-Host "- Verifies Storefront fixture endpoints for store '$StoreKey': configuration, categories, category '$FixtureCategorySlug', product '$FixtureProductSlug', page '$FixturePageSlug', and payment '$RequiredPaymentMethodKey'."
    Write-Host "- Runs scripts/qa/run-storefront-builder-generated-proof.ps1 -ProofLevel FoundationFunctionalFull."
    Write-Host "- Writes reports under $OutputRoot/$Name/docs/storefront-analysis and keeps V2 process logs under .gstack/run-v2-local."
    Write-Host "- Fixture runtime ports from scripts/env/v2-local.env: Control Plane API 5280, Control Plane Web 5281, Commerce Node API 5180, Storefront V2 18598."
    Write-Host "- Generated proof host uses $ProofUrl / $PublicBaseUrl, normally port 18620, so it does not conflict with Storefront V2."
    Write-Host "- Teardown runs in finally; use -KeepRuntime or -KeepDocker only for local debugging."
    exit 0
}

$completed = $false
$failure = $null

try {
    Invoke-Step "Stop existing V2 runtime" {
        & $stopV2Local
    }

    Invoke-Step "Start V2 fixture runtime" {
        & $runV2Local -EnvFile $EnvFile -StopExisting -NoOpenBrowser
    }

    Invoke-Step "Wait for Commerce Node health" {
        Wait-HttpOk -Url $healthUrl -Headers $healthHeaders -TimeoutSeconds $RuntimeTimeoutSeconds
    }

    Invoke-Step "Verify Storefront fixture data" {
        Assert-StorefrontFixtureData
    }

    Invoke-Step "Run generated FoundationFunctionalFull proof" {
        & $generatedProof `
            -Name $Name `
            -StoreKey $StoreKey `
            -OutputRoot $OutputRoot `
            -Configuration $Configuration `
            -CommerceNodeBaseUrl $CommerceNodeBaseUrl `
            -PublicBaseUrl $PublicBaseUrl `
            -ProofUrl $ProofUrl `
            -RuntimeTimeoutSeconds $RuntimeTimeoutSeconds `
            -FixtureCategorySlug $FixtureCategorySlug `
            -FixtureProductSlug $FixtureProductSlug `
            -FixturePageSlug $FixturePageSlug `
            -RequiredPaymentMethodKey $RequiredPaymentMethodKey `
            -ProofLevel FoundationFunctionalFull
    }

    Invoke-Step "Collect generated reports" {
        Write-ProofSummary -Status "Passed"
    }

    $completed = $true
}
catch {
    $failure = $_
    try {
        Write-ProofSummary -Status "Failed" -ErrorMessage ($failure.Exception.Message)
    }
    catch {
        Write-Warning "Unable to write full proof failure summary: $($_.Exception.Message)"
    }

    throw
}
finally {
    if (-not $KeepRuntime) {
        Invoke-Step "Stop V2 fixture runtime" {
            if ($KeepDocker) {
                & $stopV2Local
            }
            else {
                & $stopV2Local -StopDocker
            }
        }
    }
    elseif (-not $completed) {
        Write-Warning "Fixture runtime was left running because -KeepRuntime was set after a failed proof."
    }
}

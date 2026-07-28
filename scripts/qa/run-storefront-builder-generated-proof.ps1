param(
    [string]$Name = "BlazorShop.Storefront.GeneratedProof",
    [string]$StoreKey = "sample",
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
    $startInfo.RedirectStandardOutput = $true
    $startInfo.RedirectStandardError = $true
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
            $stdout = $Process.StandardOutput.ReadToEnd()
            $stderr = $Process.StandardError.ReadToEnd()
            throw "Generated proof exited before browser QA. stdout: $stdout stderr: $stderr"
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

$generatedRoot = Resolve-RepoPath $OutputRoot
$projectRoot = Join-Path $generatedRoot $Name
$projectFile = Join-Path $projectRoot "$Name.csproj"

if ($Describe) {
    Write-Host "StorefrontBuilder generated proof workflow"
    Write-Host "- Clean $projectRoot"
    Write-Host "- Pack Storefront.Client, Storefront.Runtime, Storefront.Presentation, and Storefront.Components"
    Write-Host "- Generate $Name from Storefront.Starter"
    Write-Host "- Write StorefrontBuilder review, asset, CSS, and generated-file artifacts"
    Write-Host "- Restore/build generated proof from local packages"
    Write-Host "- Run static validation and isolation gates"
    Write-Host "- Optionally run browser QA with -RunBrowserQa"
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
        -StorefrontComponentsPackageVersion $StorefrontComponentsPackageVersion
}

if ($RunBrowserQa) {
    Invoke-Step "Run browser QA" {
        $process = Start-ProofStorefront $projectFile
        try {
            Wait-ForProofStorefront $process
            node "$toolRoot\scripts\qa\run-visual-qa.mjs" --base-url $ProofUrl --project-root $projectRoot
            if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
            node "$toolRoot\scripts\qa\run-commerce-regression.mjs" --base-url $ProofUrl --project-root $projectRoot
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

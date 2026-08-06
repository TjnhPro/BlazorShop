$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..\..\..")
$toolRoot = Join-Path $repoRoot "tools\BlazorShop.AI.StorefrontBuilder"
$outputRoot = Join-Path $repoRoot "obj\storefront-builder\generated\multi-project-validation-tests"
$baselineName = "BlazorShop.Storefront.MultiProjectValidation"
$baselineRoot = Join-Path $outputRoot $baselineName
$validator = Join-Path $toolRoot "scripts\validate\Test-StorefrontBuilderGeneratedProject.ps1"
$staticGate = Join-Path $toolRoot "scripts\validate\Test-StorefrontBuilderStaticGate.ps1"

function Test-TextContains([string]$Text, [string]$Value, [System.StringComparison]$Comparison = [System.StringComparison]::Ordinal) {
    return $Text.IndexOf($Value, $Comparison) -ge 0
}

function Assert-Throws {
    param(
        [Parameter(Mandatory = $true)][scriptblock]$Action,
        [Parameter(Mandatory = $true)][string]$ExpectedCode
    )

    try {
        & $Action
    }
    catch {
        if (-not (Test-TextContains $_.Exception.Message $ExpectedCode)) {
            throw "Expected '$ExpectedCode' but saw: $($_.Exception.Message)"
        }

        return
    }

    throw "Expected '$ExpectedCode' failure."
}

function New-CaseRoot([string]$CaseName) {
    $caseRoot = Join-Path $outputRoot $CaseName
    if (Test-Path -LiteralPath $caseRoot) {
        Remove-Item -LiteralPath $caseRoot -Recurse -Force
    }

    Copy-Item -LiteralPath $baselineRoot -Destination $caseRoot -Recurse -Force
    return $caseRoot
}

function Invoke-Validator([string]$ProjectRoot) {
    & $validator -ProjectRoot $ProjectRoot -Name $baselineName -StoreKey "multi-project-validation"
}

function Invoke-StaticGate([string]$ProjectRoot) {
    & $staticGate -ProjectRoot $ProjectRoot -Name $baselineName -StoreKey "multi-project-validation" -SkipIdempotency
}

function Replace-Text {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$Old,
        [string]$New = ""
    )

    $content = Get-Content -LiteralPath $Path -Raw
    if (-not (Test-TextContains $content $Old)) {
        throw "Test fixture file '$Path' did not contain expected text '$Old'."
    }

    Set-Content -LiteralPath $Path -Value ($content.Replace($Old, $New)) -Encoding UTF8
}

if (Test-Path -LiteralPath $outputRoot) {
    Remove-Item -LiteralPath $outputRoot -Recurse -Force
}

New-Item -ItemType Directory -Force -Path $outputRoot | Out-Null

& (Join-Path $toolRoot "scripts\generate\new-storefront-project.ps1") `
    -Name $baselineName `
    -StoreKey "multi-project-validation" `
    -OutputRoot $outputRoot `
    -Force

$serverProject = Join-Path $baselineRoot "$baselineName.csproj"
$wasmProject = Join-Path $baselineRoot "$baselineName.WASM\$baselineName.WASM.csproj"
$serverProgram = Join-Path $baselineRoot "Program.cs"
$wasmProgram = Join-Path $baselineRoot "$baselineName.WASM\Program.cs"
$manifestPath = Join-Path $baselineRoot "docs\storefront-analysis\generated-files.yaml"

Assert-Throws -ExpectedCode "SFB-PROJECT-003" -Action {
    $case = New-CaseRoot "missing-wasm"
    Remove-Item -LiteralPath (Join-Path $case "$baselineName.WASM") -Recurse -Force
    Invoke-Validator $case
}

Assert-Throws -ExpectedCode "SFB-PROJECT-003" -Action {
    $case = New-CaseRoot "missing-server"
    Remove-Item -LiteralPath (Join-Path $case "$baselineName.csproj") -Force
    Invoke-Validator $case
}

Assert-Throws -ExpectedCode "SFB-PROJECT-004" -Action {
    $case = New-CaseRoot "server-missing-browser"
    Replace-Text -Path (Join-Path $case "$baselineName.csproj") -Old '    <PackageReference Include="BlazorShop.Storefront.Browser" Version="$(StorefrontBrowserPackageVersion)" />' -New ''
    Invoke-Validator $case
}

Assert-Throws -ExpectedCode "SFB-PROJECT-004" -Action {
    $case = New-CaseRoot "wasm-missing-browser"
    Replace-Text -Path (Join-Path $case "$baselineName.WASM\$baselineName.WASM.csproj") -Old '    <PackageReference Include="BlazorShop.Storefront.Browser" Version="$(StorefrontBrowserPackageVersion)" />' -New ''
    Invoke-Validator $case
}

Assert-Throws -ExpectedCode "SFB-PROJECT-004" -Action {
    $case = New-CaseRoot "server-missing-browser-controllers"
    Replace-Text -Path (Join-Path $case "Program.cs") -Old "builder.Services.AddStorefrontBrowserControllers();" -New ""
    Invoke-Validator $case
}

Assert-Throws -ExpectedCode "SFB-PROJECT-004" -Action {
    $case = New-CaseRoot "wasm-missing-browser-runtime"
    Replace-Text -Path (Join-Path $case "$baselineName.WASM\Program.cs") -Old "builder.Services.AddStorefrontBrowserRuntime(builder.HostEnvironment);" -New ""
    Invoke-Validator $case
}

Assert-Throws -ExpectedCode "SFB-PROJECT-004" -Action {
    $case = New-CaseRoot "server-missing-wasm-assembly"
    Replace-Text -Path (Join-Path $case "Program.cs") -Old "typeof($baselineName.WASM.Components.Account.StorefrontAccountApp).Assembly" -New "typeof(Program).Assembly"
    Invoke-Validator $case
}

Assert-Throws -ExpectedCode "SFB-PROJECT-004" -Action {
    $case = New-CaseRoot "server-external-project-reference"
    Replace-Text -Path (Join-Path $case "$baselineName.csproj") -Old "$baselineName.WASM\$baselineName.WASM.csproj" -New "..\..\..\BlazorShop.PresentationV2\BlazorShop.Storefront.Starter.WASM\BlazorShop.Storefront.Starter.WASM.csproj"
    Invoke-Validator $case
}

Assert-Throws -ExpectedCode "SFB-PROJECT-004" -Action {
    $case = New-CaseRoot "wasm-project-reference"
    $path = Join-Path $case "$baselineName.WASM\$baselineName.WASM.csproj"
    $content = Get-Content -LiteralPath $path -Raw
    Set-Content -LiteralPath $path -Value ($content.Replace("</Project>", "  <ItemGroup>`n    <ProjectReference Include=`"..\External\External.csproj`" />`n  </ItemGroup>`n</Project>")) -Encoding UTF8
    Invoke-Validator $case
}

foreach ($package in @("BlazorShop.Storefront.Runtime", "BlazorShop.Storefront.Client")) {
    Assert-Throws -ExpectedCode "SFB-PROJECT-004" -Action {
        $case = New-CaseRoot "direct-$($package.Split('.')[-1].ToLowerInvariant())"
        $path = Join-Path $case "$baselineName.csproj"
        $content = Get-Content -LiteralPath $path -Raw
        Set-Content -LiteralPath $path -Value ($content.Replace("</Project>", "  <ItemGroup>`n    <PackageReference Include=`"$package`" Version=`"1.0.0-local`" />`n  </ItemGroup>`n</Project>")) -Encoding UTF8
        Invoke-Validator $case
    }
}

Assert-Throws -ExpectedCode "SFB-PROJECT-006" -Action {
    $case = New-CaseRoot "v2-namespace"
    Add-Content -LiteralPath (Join-Path $case "Program.cs") -Value "// BlazorShop.Storefront.V2"
    Invoke-Validator $case
}

Assert-Throws -ExpectedCode "SFB-PROJECT-006" -Action {
    $case = New-CaseRoot "starter-namespace"
    Add-Content -LiteralPath (Join-Path $case "Program.cs") -Value "// BlazorShop.Storefront.Starter"
    Invoke-Validator $case
}

Assert-Throws -ExpectedCode "SFB-PROJECT-010" -Action {
    $case = New-CaseRoot "browser-hash-mismatch"
    $packageRoot = Join-Path $case "packages"
    New-Item -ItemType Directory -Force -Path $packageRoot | Out-Null
    Set-Content -LiteralPath (Join-Path $packageRoot "BlazorShop.Storefront.Browser.1.0.0-local.nupkg") -Value "fake package" -Encoding UTF8
    $path = Join-Path $case "docs\storefront-analysis\metadata.yaml"
    $content = Get-Content -LiteralPath $path -Raw
    $content = $content.Replace("  feedPath: unknown", "  feedPath: packages")
    $content = [regex]::Replace(
        $content,
        "(?ms)(- id: BlazorShop\.Storefront\.Browser\s*\r?\n\s+version: 1\.0\.0-local\s*\r?\n\s+sha256: )unknown",
        "`${1}0000000000000000000000000000000000000000000000000000000000000000")
    Set-Content -LiteralPath $path -Value $content -Encoding UTF8
    Invoke-Validator $case
}

Assert-Throws -ExpectedCode "SFB-PROJECT-011" -Action {
    $case = New-CaseRoot "missing-manifest-project"
    $path = Join-Path $case "docs\storefront-analysis\generated-files.yaml"
    $content = Get-Content -LiteralPath $path -Raw
    Set-Content -LiteralPath $path -Value ([regex]::Replace($content, "(?m)^\s+project:\s*wasm\s*\r?\n", "", 1)) -Encoding UTF8
    Invoke-Validator $case
}

Assert-Throws -ExpectedCode "SFB-STATIC-002" -Action {
    $case = New-CaseRoot "generated-page-route"
    Add-Content -LiteralPath (Join-Path $case "Components\Layout\MainLayout.razor") -Value '@page "/generated-route"'
    Invoke-StaticGate $case
}

Invoke-Validator $baselineRoot
Invoke-StaticGate $baselineRoot

Write-Host "StorefrontBuilder multi-project validation tests passed."

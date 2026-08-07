$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..\..\..")
$toolRoot = Join-Path $repoRoot "tools\BlazorShop.AI.StorefrontBuilder"
$outputRoot = Join-Path $repoRoot "obj\storefront-builder\generated\wg"
$projectName = "BlazorShop.Storefront.WorkspaceGuardrails"
$workspaceRoot = Join-Path $outputRoot $projectName
$serverProjectRoot = Join-Path $workspaceRoot $projectName
$wasmProjectRoot = Join-Path $workspaceRoot "$projectName.WASM"
$serverProjectFile = Join-Path $serverProjectRoot "$projectName.csproj"
$solutionFile = Join-Path $workspaceRoot "$projectName.sln"

function Assert-Condition {
    param(
        [Parameter(Mandatory = $true)][bool]$Condition,
        [Parameter(Mandatory = $true)][string]$Message
    )

    if (-not $Condition) {
        throw $Message
    }
}

function Test-TextContains([string]$Text, [string]$Value, [System.StringComparison]$Comparison = [System.StringComparison]::Ordinal) {
    return $Text.IndexOf($Value, $Comparison) -ge 0
}

function Assert-Throws {
    param(
        [Parameter(Mandatory = $true)][scriptblock]$Action,
        [Parameter(Mandatory = $true)][string]$ExpectedCode,
        [string[]]$ExpectedText = @()
    )

    try {
        & $Action
    }
    catch {
        $message = $_.Exception.Message
        if (-not (Test-TextContains $message $ExpectedCode)) {
            throw "Expected '$ExpectedCode' but saw: $message"
        }

        foreach ($text in $ExpectedText) {
            if (-not (Test-TextContains $message $text)) {
                throw "Expected error text '$text' but saw: $message"
            }
        }

        return
    }

    throw "Expected '$ExpectedCode' failure."
}

function Set-TextFileContent {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][AllowEmptyString()][string]$Content
    )

    New-Item -ItemType Directory -Force -Path (Split-Path -Parent $Path) | Out-Null
    [System.IO.File]::WriteAllText($Path, $Content, [System.Text.UTF8Encoding]::new($false))
}

function Invoke-WithTemporaryFileContent {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][scriptblock]$Transform,
        [Parameter(Mandatory = $true)][scriptblock]$Action
    )

    $exists = Test-Path -LiteralPath $Path
    $original = if ($exists) { Get-Content -LiteralPath $Path -Raw } else { $null }
    try {
        $updated = & $Transform $(if ($exists) { $original } else { "" })
        Set-TextFileContent -Path $Path -Content $updated
        & $Action
    }
    finally {
        if ($exists) {
            Set-TextFileContent -Path $Path -Content $original
        }
        elseif (Test-Path -LiteralPath $Path) {
            Remove-Item -LiteralPath $Path -Force
        }
    }
}

function Invoke-GeneratedProjectValidation {
    & (Join-Path $toolRoot "validate-storefront.ps1") -WorkspaceRoot $workspaceRoot -Name $projectName -StoreKey sample
}

function New-TestProject {
    if (Test-Path -LiteralPath $outputRoot) {
        Remove-Item -LiteralPath $outputRoot -Recurse -Force
    }

    & (Join-Path $toolRoot "build-storefront.ps1") `
        -Url "https://example.test" `
        -Name WorkspaceGuardrails `
        -StoreKey sample `
        -OutputRoot $outputRoot `
        -Mode generate `
        -Force

    foreach ($path in @($workspaceRoot, $serverProjectRoot, $wasmProjectRoot, $solutionFile, $serverProjectFile, (Join-Path $wasmProjectRoot "$projectName.WASM.csproj"))) {
        Assert-Condition -Condition (Test-Path -LiteralPath $path) -Message "Generated guardrail fixture missing required path: $path"
    }
}

function Assert-StaticSourceGuardrails {
    $generatorFiles = @(
        Get-ChildItem -LiteralPath (Join-Path $toolRoot "scripts\generate") -Recurse -File -Include *.ps1,*.mjs
        Get-Item -LiteralPath (Join-Path $toolRoot "build-storefront.ps1")
        Get-Item -LiteralPath (Join-Path $toolRoot "regenerate-storefront.ps1")
        Get-Item -LiteralPath (Join-Path $toolRoot "validate-storefront.ps1")
        Get-Item -LiteralPath (Join-Path $repoRoot "scripts\generate-storefront-sample.ps1")
    )

    foreach ($file in $generatorFiles) {
        $content = Get-Content -LiteralPath $file.FullName -Raw
        if ($content -match "(?i)<(Compile|Content|EmbeddedResource|None)\s+Remove\s*=\s*[""'][^""']*\.WASM") {
            throw "[SFB-WORKSPACE-GUARD-001] Active generator source emits nested WASM exclusion ItemGroups: $($file.FullName)"
        }

        if ($content -match '(?im)^\s*(New-Item|Copy-Item|Move-Item).*\$serverProjectRoot.*\.WASM') {
            throw "[SFB-WORKSPACE-GUARD-002] Active generator source creates old nested WASM output under the server project root: $($file.FullName)"
        }
    }

    $manifestCode = Get-Content -LiteralPath (Join-Path $toolRoot "scripts\generate\generated-file-manifest.mjs") -Raw
    foreach ($required in @("describeWorkspacePath", "projectKind", "projectName", "projectRelativePath", "workspaceRelativePath")) {
        Assert-Condition -Condition (Test-TextContains $manifestCode $required) -Message "[SFB-WORKSPACE-GUARD-003] Manifest code is missing workspace-aware field/helper '$required'."
    }

    if ($manifestCode -match "ownership\s*[:=][^\r\n]*\.WASM/") {
        throw "[SFB-WORKSPACE-GUARD-003] Manifest ownership must not be inferred only from a .WASM path prefix."
    }
}

Assert-StaticSourceGuardrails
New-TestProject
Invoke-GeneratedProjectValidation

$nestedWasmPath = Join-Path $serverProjectRoot "$projectName.WASM"
New-Item -ItemType Directory -Force -Path $nestedWasmPath | Out-Null
Assert-Throws -ExpectedCode "SFB-PROJECT-003" -ExpectedText @("Problem:", "Cause:", "Fix:", "nested") -Action {
    Invoke-GeneratedProjectValidation
}
Remove-Item -LiteralPath $nestedWasmPath -Recurse -Force

Invoke-WithTemporaryFileContent -Path $serverProjectFile -Transform {
    param([string]$content)
    $content.Replace("</Project>", @"
  <ItemGroup>
    <Compile Remove="$projectName.WASM\**" />
    <Content Remove="$projectName.WASM\**" />
    <EmbeddedResource Remove="$projectName.WASM\**" />
    <None Remove="$projectName.WASM\**" />
  </ItemGroup>
</Project>
"@)
} -Action {
    Assert-Throws -ExpectedCode "SFB-PROJECT-004" -ExpectedText @("Problem:", "Cause:", "Fix:", "exclusion ItemGroups") -Action {
        Invoke-GeneratedProjectValidation
    }
}

$solutionOriginal = Get-Content -LiteralPath $solutionFile -Raw
try {
    Remove-Item -LiteralPath $solutionFile -Force
    Assert-Throws -ExpectedCode "SFB-PROJECT-003" -ExpectedText @("Problem:", "Cause:", "Fix:", "workspace") -Action {
        Invoke-GeneratedProjectValidation
    }
}
finally {
    Set-TextFileContent -Path $solutionFile -Content $solutionOriginal
}

Invoke-WithTemporaryFileContent -Path $solutionFile -Transform {
    param([string]$content)
    $content + @"

Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "Unexpected", "Unexpected\Unexpected.csproj", "{11111111-1111-1111-1111-111111111111}"
EndProject
"@
} -Action {
    Assert-Throws -ExpectedCode "SFB-PROJECT-004" -ExpectedText @("Problem:", "Cause:", "Fix:", "unexpected project") -Action {
        Invoke-GeneratedProjectValidation
    }
}

$oldNestedProjectName = "BlazorShop.Storefront.OldNestedGuard"
$oldNestedRoot = Join-Path $outputRoot $oldNestedProjectName
if (Test-Path -LiteralPath $oldNestedRoot) {
    Remove-Item -LiteralPath $oldNestedRoot -Recurse -Force
}

New-Item -ItemType Directory -Force -Path (Join-Path $oldNestedRoot "docs\storefront-analysis") | Out-Null
New-Item -ItemType Directory -Force -Path (Join-Path $oldNestedRoot "$oldNestedProjectName\$oldNestedProjectName.WASM") | Out-Null
Set-TextFileContent -Path (Join-Path $oldNestedRoot "docs\storefront-analysis\metadata.yaml") -Content @"
projectName: $oldNestedProjectName
storeKey: sample
outputRoot: obj/storefront-builder/generated/wg
"@
Assert-Throws -ExpectedCode "SFB-REGEN-033" -ExpectedText @("Problem:", "Cause:", "Fix:", "WorkspaceRoot") -Action {
    & (Join-Path $toolRoot "regenerate-storefront.ps1") -WorkspaceRoot $oldNestedRoot -Scope css
}
Remove-Item -LiteralPath $oldNestedRoot -Recurse -Force

Write-Host "StorefrontBuilder starter-first workspace guardrail tests passed."

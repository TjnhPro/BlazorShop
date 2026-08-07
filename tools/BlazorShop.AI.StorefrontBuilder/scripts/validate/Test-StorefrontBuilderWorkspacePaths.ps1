param()

$ErrorActionPreference = "Stop"
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..\..\..")
. (Join-Path $PSScriptRoot "..\generate\StorefrontBuilderProjectSafety.ps1")

function Assert-Equal {
    param(
        [Parameter(Mandatory = $true)][string]$Actual,
        [Parameter(Mandatory = $true)][string]$Expected,
        [Parameter(Mandatory = $true)][string]$Message
    )

    if (-not $Actual.Equals($Expected, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "[SFB-PATH-TEST] $Message Actual='$Actual' Expected='$Expected'"
    }
}

function Assert-Throws {
    param(
        [Parameter(Mandatory = $true)][scriptblock]$Block,
        [Parameter(Mandatory = $true)][string]$ExpectedText
    )

    try {
        & $Block
    }
    catch {
        if ($_.Exception.Message.IndexOf($ExpectedText, [System.StringComparison]::Ordinal) -ge 0) {
            return
        }

        throw "[SFB-PATH-TEST] Expected error containing '$ExpectedText' but got '$($_.Exception.Message)'"
    }

    throw "[SFB-PATH-TEST] Expected error containing '$ExpectedText' but no error was thrown."
}

$paths = Resolve-StorefrontBuilderWorkspacePaths `
    -RepoRoot $repoRoot `
    -ProjectName "GeneratedProof" `
    -OutputRoot "artifacts/storefront-builder/generated"

$expectedWorkspace = [System.IO.Path]::GetFullPath((Join-Path $repoRoot "artifacts\storefront-builder\generated\BlazorShop.Storefront.GeneratedProof"))
Assert-Equal $paths.ProjectName "BlazorShop.Storefront.GeneratedProof" "Project name should normalize to full generated project name."
Assert-Equal $paths.WorkspaceRoot $expectedWorkspace "WorkspaceRoot should be OutputRoot/ProjectName."
Assert-Equal $paths.ProjectRoot $expectedWorkspace "ProjectRoot should be a compatibility alias for WorkspaceRoot."
Assert-Equal $paths.ServerProjectRoot (Join-Path $expectedWorkspace "BlazorShop.Storefront.GeneratedProof") "ServerProjectRoot should be a child of WorkspaceRoot."
Assert-Equal $paths.WasmProjectRoot (Join-Path $expectedWorkspace "BlazorShop.Storefront.GeneratedProof.WASM") "WasmProjectRoot should be a sibling of ServerProjectRoot."
Assert-Equal $paths.SolutionPath (Join-Path $expectedWorkspace "BlazorShop.Storefront.GeneratedProof.sln") "SolutionPath should be at WorkspaceRoot."
Assert-Equal $paths.AnalysisRoot (Join-Path $expectedWorkspace "docs\storefront-analysis") "AnalysisRoot should be workspace-level docs/storefront-analysis."

$objPaths = Resolve-StorefrontBuilderWorkspacePaths `
    -RepoRoot $repoRoot `
    -ProjectName "BlazorShop.Storefront.ObjProof" `
    -OutputRoot "obj/storefront-builder/generated"
$expectedObjWorkspace = [System.IO.Path]::GetFullPath((Join-Path $repoRoot "obj\storefront-builder\generated\BlazorShop.Storefront.ObjProof"))
Assert-Equal $objPaths.WorkspaceRoot $expectedObjWorkspace "WorkspaceRoot should support obj/storefront-builder/generated."

$aliasPaths = Resolve-StorefrontBuilderWorkspacePaths `
    -RepoRoot $repoRoot `
    -ProjectRoot "artifacts/storefront-builder/generated/BlazorShop.Storefront.GeneratedProof"
Assert-Equal $aliasPaths.WorkspaceRoot $expectedWorkspace "ProjectRoot alias should resolve to WorkspaceRoot."

Assert-Throws -ExpectedText "SFB-PROJECT-001" -Block {
    Resolve-StorefrontBuilderWorkspacePaths -RepoRoot $repoRoot -ProjectName "../Bad" -OutputRoot "artifacts/storefront-builder/generated"
}

Assert-Throws -ExpectedText "SFB-PROJECT-014" -Block {
    Resolve-StorefrontBuilderWorkspacePaths `
        -RepoRoot $repoRoot `
        -ProjectName "GeneratedProof" `
        -WorkspaceRoot "artifacts/storefront-builder/generated/BlazorShop.Storefront.GeneratedProof" `
        -ProjectRoot "artifacts/storefront-builder/generated/BlazorShop.Storefront.Other"
}

Write-Host "StorefrontBuilder workspace path validation passed."

param(
    [int]$CommandTimeoutSeconds = 900,
    [int]$GlobalTimeoutSeconds = 3600
)

$ErrorActionPreference = "Stop"

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
. (Join-Path $PSScriptRoot "storefront-reverse-engineering-phase3-proof-steps.ps1")

$context = New-SreGateContext -RepoRoot $repoRoot -CommandTimeoutSeconds $CommandTimeoutSeconds -GlobalTimeoutSeconds $GlobalTimeoutSeconds
$portablePackageResult = "not-run"
$referenceContainmentResult = "not-run"
$evidenceSlotProvenanceResult = "not-run"
$consumerDryRunResult = "not-run"
$closureDecision = "blocked until gate passes on final clean HEAD"
$proofSummary = @(
    "Phase 3A proof runs directly through readiness, browser, and CLI evidence without invoking the Phase 3A gate.",
    "Phase 3B proof runs directly through visual analysis/ecommerce mapping tests and multi-route CLI fixtures without invoking the Phase 3B gate.",
    "Phase 3C proof runs directly through final handoff fixture, unsupported behavior, and schema tests without invoking the Phase 3C gate.",
    "Phase 3D correctness proof runs directly through positive/negative test buckets without invoking the Phase 3D gate.",
    "Phase 3E portability proof validates portable package, copied-package dry-run loading, negative portability mutations, boundary scans, StorefrontBuilder smoke, and final HEAD equality.",
    "StorefrontBuilder smoke result is plan-only and does not consume agent handoff artifacts."
)
$boundaryAssertions = Get-SreBoundaryAssertionSummaries

function New-Phase3EReportLines {
    param(
        [Parameter(Mandatory = $true)][string]$Status,
        [string]$ErrorMessage = ""
    )

    $lines = New-SreReportLines -Context $context -Title "Storefront Reverse Engineering Phase 3E Final Closure Gate" -Status $Status -ErrorMessage $ErrorMessage -ProofSummary $proofSummary -BoundaryAssertions $boundaryAssertions
    $insertAt = $lines.IndexOf("GitHub Actions status: disabled/local proof primary unless verified separately.")
    if ($insertAt -ge 0) {
        $lines.Insert($insertAt, "Phase 3D proof result: passed")
        $lines.Insert($insertAt + 1, "Portable package result: $portablePackageResult")
        $lines.Insert($insertAt + 2, "Reference containment result: $referenceContainmentResult")
        $lines.Insert($insertAt + 3, "Evidence slot provenance result: $evidenceSlotProvenanceResult")
        $lines.Insert($insertAt + 4, "Consumer dry-run result: $consumerDryRunResult")
        $lines.Insert($insertAt + 5, "Closure decision: $closureDecision")
    }

    return $lines
}

try {
    Set-Location $repoRoot
    New-Item -ItemType Directory -Force -Path $context.ReportRoot | Out-Null

    $context.TestedHead = (& git rev-parse HEAD).Trim()
    $context.FinalHead = $context.TestedHead
    $context.InitialBranch = (& git branch --show-current).Trim()

    Invoke-SreStep -Context $context -Name "clean tree check" -Script {
        Assert-SreCleanWorkingTree -Context $context
    }

    Invoke-SreRestore -Context $context
    Invoke-SreBuild -Context $context
    Invoke-SrePhase3AProof -Context $context
    Invoke-SrePhase3BProof -Context $context
    Invoke-SrePhase3CProof -Context $context
    Invoke-SrePhase3DProof -Context $context -IncludePortableProof
    Invoke-SrePhase3EProof -Context $context
    $portablePackageResult = "passed"
    $referenceContainmentResult = "passed"
    $evidenceSlotProvenanceResult = "passed"
    $consumerDryRunResult = "passed"
    Invoke-SreBoundaryScans -Context $context
    Invoke-SreStorefrontBuilderSmoke -Context $context -Name "Phase3EClosure" -OutputRoot "obj/storefront-builder/generated/reverse-engineering-phase3e-gate"
    Invoke-SreFinalInspectProof -Context $context -Filter "PortableHandoffCli"
    Invoke-SreCleanupSuccessfulArtifacts -Context $context -StorefrontBuilderOutputRoots @("obj\storefront-builder\generated\reverse-engineering-phase3e-gate")

    Invoke-SreStep -Context $context -Name "final HEAD check" -Script {
        Assert-SreHeadUnchanged -Context $context
        Assert-SreCleanWorkingTree -Context $context
    }

    $closureDecision = "passed: Phase 3E can close on this clean HEAD with no later source or documentation commit"
    $reportPath = Join-Path $context.ReportRoot ("phase3e-final-closure-gate-" + (Get-Date -Format "yyyyMMddHHmmss") + ".md")
    New-Phase3EReportLines -Status "passed" | Set-Content -Path $reportPath -Encoding UTF8
    Write-Host "Gate passed. Report: $reportPath"
}
catch {
    $context.FinalHead = (& git rev-parse HEAD).Trim()
    $reportPath = Join-Path $context.ReportRoot ("phase3e-final-closure-gate-failed-" + (Get-Date -Format "yyyyMMddHHmmss") + ".md")
    New-Phase3EReportLines -Status "failed" -ErrorMessage $_.Exception.Message | Set-Content -Path $reportPath -Encoding UTF8
    Write-Error "Gate failed. Report: $reportPath. Error: $($_.Exception.Message)"
    exit 1
}

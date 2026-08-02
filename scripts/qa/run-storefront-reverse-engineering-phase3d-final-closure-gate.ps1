param(
    [int]$CommandTimeoutSeconds = 900
)

$ErrorActionPreference = "Stop"

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
. (Join-Path $PSScriptRoot "storefront-reverse-engineering-phase3-proof-steps.ps1")

$context = New-SreGateContext -RepoRoot $repoRoot -CommandTimeoutSeconds $CommandTimeoutSeconds
$proofSummary = @(
    "Phase 3A proof runs directly through regression, browser fixture, CLI readiness, validate, and inspect steps.",
    "Phase 3B proof runs directly through the full analysis suite and multi-route CLI fixture workflows.",
    "Phase 3C proof runs directly through complete fixture, unsupported fixture, and schema validation tests.",
    "Phase 3D proof runs directly through full suite, positive end-to-end, exact slot, handoff, and negative mutation tests.",
    "No Phase 3A, Phase 3B, or Phase 3C full gate script is invoked from this final Phase 3D closure path.",
    "StorefrontBuilder smoke result is plan-only and does not consume agent handoff artifacts.",
    "Phase 3 closure decision: close only when this gate status is passed on final HEAD."
)
$boundaryAssertions = @(
    "ReverseEngineering has no production project references.",
    "StorefrontBuilder does not consume analysis/agent-handoff/* yet.",
    "ReverseEngineering does not write Razor/CSS/JS storefront output.",
    "ReverseEngineering does not write to Starter or generated storefront source.",
    "No direct Commerce Node browser calls are generated or recommended.",
    "No generated @page output exists.",
    "No captures/home or plan.Pages.First() hardcode exists in workflow code.",
    "No reviewed blueprint reference to .draft.json is accepted.",
    "No handoff reference outside analysis/agent-handoff is accepted."
)

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
    Invoke-SrePhase3DProof -Context $context
    Invoke-SreBoundaryScans -Context $context
    Invoke-SreStorefrontBuilderSmoke -Context $context -Name "Phase3DClosure" -OutputRoot "obj/storefront-builder/generated/reverse-engineering-phase3d-gate"
    Invoke-SreFinalInspectProof -Context $context

    Invoke-SreStep -Context $context -Name "final HEAD check" -Script {
        Assert-SreHeadUnchanged -Context $context
        Assert-SreCleanWorkingTree -Context $context
    }

    $reportPath = Join-Path $context.ReportRoot ("phase3d-final-closure-gate-" + (Get-Date -Format "yyyyMMddHHmmss") + ".md")
    New-SreReportLines -Context $context -Title "Storefront Reverse Engineering Phase 3D Final Closure Gate" -Status "passed" -ProofSummary $proofSummary -BoundaryAssertions $boundaryAssertions | Set-Content -Path $reportPath -Encoding UTF8
    Write-Host "Gate passed. Report: $reportPath"
}
catch {
    $context.FinalHead = (& git rev-parse HEAD).Trim()
    $reportPath = Join-Path $context.ReportRoot ("phase3d-final-closure-gate-failed-" + (Get-Date -Format "yyyyMMddHHmmss") + ".md")
    New-SreReportLines -Context $context -Title "Storefront Reverse Engineering Phase 3D Final Closure Gate" -Status "failed" -ErrorMessage $_.Exception.Message -ProofSummary $proofSummary -BoundaryAssertions $boundaryAssertions | Set-Content -Path $reportPath -Encoding UTF8
    Write-Error "Gate failed. Report: $reportPath. Error: $($_.Exception.Message)"
    exit 1
}

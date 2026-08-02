# StorefrontReverseEngineering Phase 3E - QA Gate Optimization

Status: In Progress  
Owner area: `tools/BlazorShop.AI.StorefrontReverseEngineering`  
Target folder: `docs/visual-reverse-engineering-skill`  
Depends on: Phase 3D correctness proof and Phase 3E portable handoff proof being present  
Primary goal: reduce final Phase 3 QA time by removing nested gate execution, repeated builds, repeated test-host startup, repeated StorefrontBuilder smoke runs, and repeated positive baseline creation while preserving the same closure coverage and blocker evidence.

## Why This File Exists

The current Phase 3E gate is slow for structural reasons, not because a single test is flaky:

- `scripts/qa/run-storefront-reverse-engineering-phase3e-final-closure-gate.ps1` still invokes the Phase 3D gate.
- `scripts/qa/run-storefront-reverse-engineering-phase3d-final-closure-gate.ps1` still invokes Phase 3A, Phase 3B, and Phase 3C as nested gates.
- The final gate repeats the full ReverseEngineering suite and then runs many focused filters on top of it.
- Negative mutation tests repeatedly rebuild the same positive project baseline.
- `PortableHandoffTestFixture.CreateAsync()` recreates a fresh positive source project for every portable proof case.
- GitHub Actions verification is intentionally out of scope here because Actions are disabled in development and local proof is the source of truth.

This plan keeps coverage intact and removes duplicated work only.

## Current Codebase Evidence

The plan is grounded in the current codebase:

- Phase 3E final gate still calls the Phase 3D final gate once.
- Phase 3D final gate still calls Phase 3A, Phase 3B, and Phase 3C.
- Phase 3D and Phase 3E each run full ReverseEngineering tests plus multiple focused filters.
- `Phase3DPositiveEndToEndTests.CreatePositiveProjectAsync()` is the current positive baseline factory used by multiple mutation tests.
- `PortableHandoffTestFixture.CreateAsync()` calls that same positive baseline factory and then copies portable artifacts.
- `Phase3DProofFixtureTests` and related phase gate tests still verify script source text and exact gate strings instead of proving a single non-recursive runtime graph.
- The repo does not currently show a shared PowerShell module pattern for QA gates; the common style is self-contained scripts with local helper functions.

## Locked Decisions

- Final closure must stay local and deterministic.
- GitHub Actions evidence is not part of this phase because Actions are disabled in development.
- Coverage must not shrink.
- Exact blocker codes must not change.
- Phase 3D and Phase 3E standalone gates may remain usable, but the final closure path must stop nesting gates.
- No changes to StorefrontBuilder consumption, handoff format, or Phase 4 boundaries are allowed in this optimization plan.

## Not In Scope

- Any Phase 4 consumer cutover.
- Any GitHub Actions re-enablement work.
- Any business logic changes in catalog, cart, checkout, payment, account, or storefront runtime.
- Any change to reviewed artifact semantics or blocker codes.
- Any reduction in browser, CLI, portability, or boundary coverage.
- Any rewrite of the ReverseEngineering feature set beyond the orchestration and fixture reuse needed to remove duplication.

## Target Final Shape

The optimized final gate should follow this shape:

```text
clean tree check
restore once
build once
unit/regression suite once
closure proof suite once
one CLI proof collection
one StorefrontBuilder plan-only smoke
one canonical boundary scan
final HEAD check
cleanup and report
```

Expected outcome:

- same assertions
- same blocker codes
- same reviewed handoff proof
- same portable copy proof
- same boundary scan coverage
- fewer processes and less duplicated work

## Phase O0 - Baseline Measurement And Scope Lock

Goal: capture the exact current runtime shape before changing orchestration.

Implementation checklist:

Baseline evidence captured on 2026-08-02 before optimization changes:

- Branch: `master`
- Tested `HEAD`: `9bdb4d4be4019392360ab08796cf067422aa9597`
- Working tree: one related untracked plan file, `docs/visual-reverse-engineering-skill/16-StorefrontReverseEngineering-Phase3E-QA-Gate-Optimization.todo.md`; no unrelated tracked changes.
- Baseline final gate report: `obj/storefront-reverse-engineering/reports/phase3e-final-closure-gate-20260802122027.md`
- Baseline Phase 3D nested report: `obj/storefront-reverse-engineering/reports/phase3d-final-closure-gate-20260802121241.md`
- Baseline runtime: Phase 3E final gate 2586.9s, with nested Phase 3D gate 2113.67s.
- Nested gate shape: Phase 3E invokes Phase 3D; Phase 3D invokes Phase 3A, Phase 3B, and Phase 3C.
- Baseline process count across Phase 3E plus nested reports: 31 `dotnet test`, 11 `dotnet run`, and 4 StorefrontBuilder smoke/build-storefront invocations or mentions.
- Repeated positive baseline creation points: `Phase3DPositiveEndToEndTests.CreatePositiveProjectAsync()` and `PortableHandoffTestFixture.CreateAsync()`.
- Proof sections that must remain covered: clean tree, restore/build, Phase 3A readiness/browser/CLI, Phase 3B visual analysis/ecommerce mapping, Phase 3C handoff readiness, Phase 3D positive/negative correctness, Phase 3E portable validation/copy/dry-run, boundary scans, StorefrontBuilder plan-only smoke, final inspect, final HEAD equality.

- [x] Record current branch and tested `HEAD`.
- [x] Capture current working tree status and note any pre-existing unrelated changes.
- [x] Run the current Phase 3E gate once and record which child gates it invokes.
- [x] Record the number of `dotnet test`, `dotnet run`, and StorefrontBuilder smoke invocations produced by the current final gate.
- [x] Record the current repeated positive baseline creation points:
  - [x] `Phase3DPositiveEndToEndTests.CreatePositiveProjectAsync()`
  - [x] `PortableHandoffTestFixture.CreateAsync()`
- [x] Record the current final gate proof sections that must remain covered after optimization.

Done when:

- [x] The optimization work has a measured baseline that can be compared against the final gate.

## Phase O1 - Remove Nested Historical Gates From The Final Gate

Goal: Phase 3E must orchestrate proofs directly instead of calling the entire Phase 3D gate as a black box.

Implementation checklist:

- [x] Split the Phase 3D and Phase 3E scripts into two layers:
  - [x] a reusable proof layer
  - [x] a top-level orchestration layer
- [x] Make the Phase 3E final gate call proof steps directly rather than invoking `run-storefront-reverse-engineering-phase3d-final-closure-gate.ps1`.
- [x] Keep the standalone Phase 3D final gate usable, but prevent it from being a nested dependency inside Phase 3E.
- [x] If skip flags are needed for compatibility, keep them narrow and explicit:
  - [x] `-SkipBuild`
  - [x] `-SkipFullTests`
  - [x] `-SkipNestedGates`
  - [x] `-SkipStorefrontBuilderSmoke`
- [x] Prefer a reusable helper surface over a web of new gate scripts.
- [x] Avoid introducing a new recursive gate chain.

Acceptance criteria:

- [x] Phase 3E no longer calls Phase 3D as a nested gate.
- [x] No proof path calls another full gate recursively.
- [x] Standalone Phase 3A/3B/3C/3D scripts still work when run directly.

## Phase O2 - Build Once And Propagate No-Build Execution

Goal: restore and build once, then reuse the same outputs for every later command.

Implementation checklist:

- [x] Add one explicit restore step at the top of the final gate.
- [x] Add one explicit build step at the top of the final gate.
- [x] Ensure every later `dotnet test` call uses `--no-build --no-restore`.
- [x] Ensure every later `dotnet run` call uses `--no-build --no-restore` or a direct DLL invocation when appropriate.
- [x] Ensure StorefrontBuilder smoke does not trigger hidden rebuilds.
- [x] Ensure helper functions do not call build internally during the final gate.

Acceptance criteria:

- [x] Exactly one build occurs in the final closure path.
- [x] No later test or CLI step rebuilds the same projects.
- [x] Process count drops without changing coverage.

O2 evidence:

- `run-storefront-reverse-engineering-phase3d-final-closure-gate.ps1` and `run-storefront-reverse-engineering-phase3e-final-closure-gate.ps1` now execute `Invoke-SreRestore` once followed by `Invoke-SreBuild` once before proof steps.
- `Invoke-SreTest` now passes `--no-build --no-restore` to every later ReverseEngineering test invocation.
- ReverseEngineering CLI proofs now use direct DLL invocation through `dotnet <ToolDll> ...`; no final closure helper path contains `dotnet run --project` for the ReverseEngineering tool.
- StorefrontBuilder smoke remains `build-storefront.ps1 -Mode plan-only`, which runs the plan generator dry-run path and does not build generated storefront projects.
- Verification: `dotnet build tools/BlazorShop.AI.StorefrontReverseEngineering/tests/BlazorShop.AI.StorefrontReverseEngineering.Tests/BlazorShop.AI.StorefrontReverseEngineering.Tests.csproj --no-restore`; `dotnet test tools/BlazorShop.AI.StorefrontReverseEngineering/tests/BlazorShop.AI.StorefrontReverseEngineering.Tests/BlazorShop.AI.StorefrontReverseEngineering.Tests.csproj --no-build --no-restore --filter "Phase3DFinalClosureGate|Phase3EFinalClosureGate" --blame-hang --blame-hang-timeout 5m`.

## Phase O3 - Consolidate Proof Test Execution

Goal: stop starting many test hosts for reports that can be grouped.

Implementation checklist:

- [x] Add or standardize traits for the ReverseEngineering tests:
  - [x] `Phase`
  - [x] `Proof`
  - [x] `Browser`
  - [x] `ClosureProof`
  - [x] `PortableProof`
- [x] Group the closure proof tests into one or two test invocations instead of many tiny filter runs.
- [x] Keep the full suite run only once if it is still needed for closure evidence.
- [x] Parse TRX once and summarize the results rather than rerunning the same tests to produce a different report section.
- [x] Keep exact blocker assertions visible in the report.

Recommended proof buckets:

- [x] `Unit/Regression`
- [x] `ClosureProof`
- [x] `Browser`
- [x] `PortableProof`

Acceptance criteria:

- [x] The final gate runs at most one full suite process and one grouped closure proof process.
- [x] Report sections still show phase/proof coverage.
- [x] No test process exists only to decorate the report.

O3 evidence:

- Traits were standardized on the ReverseEngineering proof classes for `Phase`, `Proof`, `Browser`, `ClosureProof`, and `PortableProof`.
- `Get-SreClosureProofFilter` now groups the closure bucket by `FullyQualifiedName~...` patterns instead of many tiny filter runs.
- Final-gate execution now uses one full ReverseEngineering suite process and one grouped closure proof process; `Phase 3A`, `Phase 3B`, `Phase 3C`, and portable proof markers are summarized instead of rerun separately.
- Verification: `dotnet test tools/BlazorShop.AI.StorefrontReverseEngineering/tests/BlazorShop.AI.StorefrontReverseEngineering.Tests/BlazorShop.AI.StorefrontReverseEngineering.Tests.csproj --no-build --no-restore --filter "<grouped filter>" --blame-hang --blame-hang-timeout 5m` passed with `217` selected tests.

## Phase O4 - Reuse A Shared Positive Baseline

Goal: create one immutable positive baseline and reuse copies of it across mutation proofs.

Implementation checklist:

- [ ] Introduce a shared positive project baseline fixture.
- [ ] Create the positive baseline once per test collection.
- [ ] Give each mutation proof a private copy of that baseline.
- [ ] Stop calling `CreatePositiveProjectAsync()` independently in every negative test.
- [ ] Preserve deterministic cleanup of private copies.
- [ ] Keep the baseline immutable after creation.

Recommended fixture shape:

```csharp
public sealed class Phase3PositiveProjectBaseline : IAsyncLifetime
{
    public string SourceProjectRoot { get; private set; } = null!;
    public string PortablePackageRoot { get; private set; } = null!;
    public string SchemaRoot { get; private set; } = null!;

    public Task InitializeAsync();
    public string CreateProjectCopy(string purpose);
    public string CreatePortableCopy(string purpose);
    public Task DisposeAsync();
}
```

Acceptance criteria:

- [ ] The positive workflow runs once per collection, not once per mutation test.
- [ ] Mutation tests use private copies.
- [ ] Fixture cleanup removes generated copies.

## Phase O5 - Reuse A Shared Portable Baseline

Goal: avoid rebuilding a fresh portable handoff source project for every portable proof case.

Implementation checklist:

- [ ] Change `PortableHandoffTestFixture.CreateAsync()` so it reuses the shared positive baseline.
- [ ] Keep schema copies local to the portable proof fixture.
- [ ] Copy only the portable handoff subtree that must be validated.
- [ ] Ensure negative portable mutation cases mutate copies, not the shared source.
- [ ] Keep a dedicated source-deletion proof for the case where the source project must be removed.

Acceptance criteria:

- [ ] Portable validator and copy proofs no longer recreate the full positive project each time.
- [ ] Portable negative cases still exercise exact blocker codes.
- [ ] The portable package remains self-contained.

## Phase O6 - Reduce CLI And Browser Proof Process Count

Goal: preserve browser and CLI coverage while shrinking the number of independent processes.

Implementation checklist:

- [ ] Convert the CLI proof to a multi-route fixture where one workflow covers home, category/listing, product detail, and unsupported behavior.
- [ ] Keep unsupported-path behavior proven explicitly.
- [ ] Use one browser lifecycle for all browser-required proofs when safe.
- [ ] Keep isolated browser contexts/pages per case.
- [ ] Avoid separate `dotnet run` invocations whose only purpose is to repeat the same proof family.
- [ ] Keep CLI/browser proof evidence readable in the final report.

Acceptance criteria:

- [ ] The CLI proof family uses fewer processes without losing page coverage.
- [ ] Browser evidence remains real and isolated.
- [ ] No proof class is silently dropped.

## Phase O7 - Run StorefrontBuilder Smoke Once

Goal: preserve StorefrontBuilder compatibility proof, but only once per final gate.

Implementation checklist:

- [ ] Keep plan-only StorefrontBuilder smoke in the final gate exactly once.
- [ ] Do not run StorefrontBuilder smoke inside nested historical gates during the final closure path.
- [ ] Keep standalone historical gates usable if someone runs them manually.
- [ ] Keep the smoke result visible in the final report.

Acceptance criteria:

- [ ] StorefrontBuilder plan-only smoke runs once in the final closure path.
- [ ] StorefrontBuilder smoke is not duplicated by nested gates.

## Phase O8 - Make Boundary Scans Canonical

Goal: run one canonical boundary scan instead of repeating similar scans across nested paths.

Implementation checklist:

- [ ] Centralize the boundary regex and path list used by final closure.
- [ ] Run one canonical ReverseEngineering boundary scan in the final gate.
- [ ] Keep exact blocker strings stable.
- [ ] Remove duplicate boundary scans that only restate the same rule in another script layer.
- [ ] Keep all previous coverage represented once in the final report.

Acceptance criteria:

- [ ] One canonical boundary scan remains in the final closure path.
- [ ] The scan still proves no forbidden cross-boundary references.

## Phase O9 - Add A Global Timeout And Step Telemetry

Goal: make long final runs diagnosable without increasing duplication.

Implementation checklist:

- [ ] Track a global timeout for the final gate, not only per-process timeouts.
- [ ] Track remaining budget before each step.
- [ ] Record start/end/duration for each major step.
- [ ] Record the slowest steps in the final report.
- [ ] Record process count, test process count, and major proof counts.

Suggested telemetry fields:

- [ ] step name
- [ ] start UTC
- [ ] end UTC
- [ ] duration
- [ ] exit code
- [ ] test count
- [ ] artifact count
- [ ] bytes written
- [ ] baseline cache hit/miss

Acceptance criteria:

- [ ] The final gate can report where time went.
- [ ] Slow steps are visible without adding repeated test runs.

## Phase O10 - Keep Cleanup Strict And Predictable

Goal: remove transient artifacts cleanly while preserving failed-run inspectability.

Implementation checklist:

- [ ] Delete passing test copies immediately after use.
- [ ] Retain failed test copies long enough to inspect them.
- [ ] Keep the positive baseline only for the current process.
- [ ] Keep gate reports and closure reports.
- [ ] Prevent GUID-based temp trees from accumulating indefinitely.

Acceptance criteria:

- [ ] The final gate leaves the workspace clean on success.
- [ ] Failed artifacts remain inspectable.
- [ ] Disk usage does not grow without bound.

## Phase O11 - Documentation And Report Alignment

Goal: make the plan and QA docs match the optimized execution model.

Implementation checklist:

- [ ] Update the relevant Phase 3 QA notes to explain that the final gate is non-recursive.
- [ ] Document the one-build / no-build-later rule.
- [ ] Document the shared-baseline reuse rule.
- [ ] Document that GitHub Actions is intentionally not part of this closure path while disabled.
- [ ] Keep the phase plan and QA notes consistent with the actual scripts after the refactor.

Acceptance criteria:

- [ ] The docs describe the optimized gate accurately.
- [ ] The docs do not promise repeated work that the scripts no longer do.

## Recommended Implementation Order

1. O0 Baseline Measurement And Scope Lock
2. O1 Remove Nested Historical Gates From The Final Gate
3. O2 Build Once And Propagate No-Build Execution
4. O3 Consolidate Proof Test Execution
5. O4 Reuse A Shared Positive Baseline
6. O5 Reuse A Shared Portable Baseline
7. O6 Reduce CLI And Browser Proof Process Count
8. O7 Run StorefrontBuilder Smoke Once
9. O8 Make Boundary Scans Canonical
10. O9 Add A Global Timeout And Step Telemetry
11. O10 Keep Cleanup Strict And Predictable
12. O11 Documentation And Report Alignment

## Test Matrix

The optimized plan must still cover:

- [ ] phase 3A readiness and evidence hardening
- [ ] phase 3B visual analysis and ecommerce mapping
- [ ] phase 3C final handoff readiness
- [ ] phase 3D correctness proof
- [ ] phase 3E portable handoff proof
- [ ] exact blocker codes
- [ ] browser proof
- [ ] CLI proof
- [ ] portability proof
- [ ] boundary scan proof
- [ ] StorefrontBuilder smoke proof

## Definition Of Done

The optimization is complete when all of the following are true:

- [ ] The final Phase 3E gate no longer nests the Phase 3D gate.
- [ ] The Phase 3D gate no longer nests Phase 3A, Phase 3B, and Phase 3C in the final closure path.
- [ ] Build happens once.
- [ ] The full suite, closure suite, CLI proof, browser proof, StorefrontBuilder smoke, and boundary scan each run once or in the minimum grouped form that preserves coverage.
- [ ] Shared positive and portable baselines are reused.
- [ ] Cleanup is deterministic.
- [ ] Local reports clearly show counts, timings, and closure evidence.
- [ ] GitHub Actions remains out of this phase because it is disabled in development.

## Decision Notes

This plan intentionally favors:

- direct proof orchestration over nested gate orchestration
- shared immutable fixtures over per-test project creation
- one-time build over hidden rebuilds
- local proof evidence over CI evidence while GitHub Actions is disabled

These choices match the current repo shape and remove duplicated work without changing the underlying proof surface.

# StorefrontVisualSkills Phase 4.11 Closure Hardening.todo

Status: In Progress
Owner: Storefront Platform
Created: 2026-08-02
Target folder: `docs/visual-reverse-engineering-skill`
Depends on:

- `19-StorefrontVisualSkills-Phase4-10-MVP-EndToEnd-Closure.todo.md`.
- `docs/architecture/11-storefront-builder.md`.
- `docs/visual-reverse-engineering-skill/README.md`.
- `docs/agents/storefront-builder.md`.
- `scripts/qa/run-storefront-phase4-mvp-gate.ps1`.
- `scripts/qa/run-storefront-phase4-final-closure-gate.ps1`.
- `tools/BlazorShop.AI.StorefrontBuilder/scripts/qa/run-visual-qa.mjs`.
- `tools/BlazorShop.AI.StorefrontBuilder/scripts/generate/record-agent-visual-writes.mjs`.

Primary goal: harden Phase 4 visual-skill closure from "architecture and tooling implemented" to deterministic end-to-end proof: portable handoff, generated Blazor storefront runtime, visual implementation artifacts, independent reference visual QA, functional commerce proof, safe regeneration, and clean-HEAD final closure.

## Why This File Exists

Phase 4.10 created the visual skill workspace, report schemas, MVP gate, final closure gate, and one pilot workflow. The remaining problem is closure strength. Current gates can still pass with a static fixture, optional visual artifacts, no real reference visual comparison, structure-only generated proof, and an agent-supplied written-file list.

This phase closes those gaps without rewriting StorefrontBuilder or moving ecommerce behavior into the visual workspace.

Final target flow:

```text
portable handoff
  -> deterministic generation plan
  -> generated project from Starter
  -> visual plan
  -> visual implementation
  -> automatic changed-file detection
  -> implementation checkpoint
  -> generated Blazor runtime visual capture
  -> independent reference visual QA
  -> bounded visual repair when allowed
  -> generated Blazor runtime re-capture
  -> functional commerce QA
  -> regeneration proof
  -> clean-HEAD final closure
```

## Codebase Baseline Findings

- `scripts/qa/run-storefront-phase4-final-closure-gate.ps1` defaults the pilot generated project, fixture root, and handoff root to `obj/...` paths.
- `scripts/qa/run-storefront-phase4-final-closure-gate.ps1` currently runs `run-storefront-builder-generated-proof.ps1 -ProofLevel Structure`.
- `scripts/qa/run-storefront-phase4-final-closure-gate.ps1` calls the Phase 4 MVP gate with `-FixtureRoot` and `-SkipRepair`.
- `scripts/qa/run-storefront-phase4-mvp-gate.ps1` treats `-BaseUrl` as optional and still supports `-FixtureRoot`.
- `scripts/qa/run-storefront-phase4-mvp-gate.ps1` validates visual JSON artifacts only when they are present.
- `scripts/qa/run-storefront-phase4-mvp-gate.ps1` currently checks `visual-plan.json`, `visual-implementation-report.json`, and `visual-qa-report.json`, but does not require the full visual checklist/checkpoint chain.
- `tools/BlazorShop.AI.StorefrontBuilder/scripts/qa/run-visual-qa.mjs` supports file-based fixture proof through `--fixture-root` and `file://` URLs.
- `tools/BlazorShop.AI.StorefrontBuilder/scripts/qa/run-visual-qa.mjs` explicitly reports `Reference visual diff: not implemented`.
- `tools/BlazorShop.AI.StorefrontBuilder/scripts/qa/run-visual-qa.mjs` allows pass when critical count is zero and major count is at most three.
- `tools/BlazorShop.AI.StorefrontBuilder/scripts/generate/record-agent-visual-writes.mjs` requires `--written-files`; current write evidence depends on the agent-supplied list.
- `scripts/qa/run-storefront-builder-generated-proof.ps1` already supports `Structure`, `FoundationFunctionalFast`, and `FoundationFunctionalFull`. This should be reused.
- `scripts/qa/run-storefront-builder-full-proof-with-fixture.ps1` already owns fixture runtime bootstrap and teardown for full proof. This should be reused when live fixture proof is required.
- The path `tools/BlazorShop.AI.StorefrontReverseEngineering/tests/fixtures/` is not the active fixture path. Current ReverseEngineering test fixtures live under `tools/BlazorShop.AI.StorefrontReverseEngineering/tests/BlazorShop.AI.StorefrontReverseEngineering.Tests/Fixtures/`.

## Locked Decisions

- Keep `tools/BlazorShop.AI.Visual` as a docs/schema/reference/skill workspace only.
- Do not add a `.csproj`, runtime service, generator, model orchestration, or production dependency to `tools/BlazorShop.AI.Visual`.
- StorefrontBuilder remains the only owner of generated project creation, regeneration, write recording, browser QA execution, repair wrappers, and closure gates.
- ReverseEngineering remains the only owner of reference-site capture, reviewed evidence, portable handoff packaging, and handoff schema validation.
- Generated storefronts remain disposable under `obj/storefront-builder/generated/{ProjectName}` or `artifacts/storefront-builder/generated/{ProjectName}`.
- Final closure input must come from tracked, reproducible fixtures or handoff packages, not from pre-existing transient `obj` contents.
- The final closure gate must run locally. GitHub Actions evidence is intentionally out of scope while Actions are disabled during development.
- Runtime visual QA must use a generated Blazor storefront host. Static fixture proof may remain for skeleton proof, but it cannot close Phase 4.11.
- Visual QA must compare generated runtime output with reference evidence at a review-contract level. Pixel-perfect scoring is still deferred.
- Agent changed-file truth must come from source diff/checkpoint detection. `--written-files` may remain as a hint or compatibility input only.

## Out Of Scope

- Pixel-perfect image scoring as a hard automated gate.
- New AI AgentRunner or model provider orchestration.
- New production services.
- New Commerce Node, Control Plane, Storefront API, cart, checkout, account, payment, or order behavior.
- Replacing existing ecommerce functional QA with visual QA.
- Promoting generated projects into `BlazorShop.sln`.
- Committing transient screenshots, generated storefront artifacts, or `obj` reports.
- Reopening Phase 3 ReverseEngineering capture or handoff correctness work unless a tracked fixture is missing required portable handoff data.

## Autoplan Review Decisions

| Decision | Classification | Chosen direction | Rationale |
| --- | --- | --- | --- |
| Closure strength | Auto-decided | Treat Phase 4.11 as required before final MVP closure. | Current final gate proves structure and local pilot existence more than runtime generated storefront correctness. |
| Runtime proof | Auto-decided | Require generated Blazor host proof for closure, keep static fixture mode only for skeleton checks. | File fixture QA can miss runtime routing, CSS asset, BFF, hydration, and browser network failures. |
| Reference QA | Auto-decided | Add reference visual QA contract with evidence review and severity counters. | Current visual QA explicitly says reference diff is not implemented. |
| Functional proof | Auto-decided | Reuse existing `FoundationFunctionalFast` and full fixture proof scripts instead of inventing another ecommerce gate. | The codebase already has generated functional and commerce-regression scripts. |
| Artifact chain | Auto-decided | Make visual plan, checklist, checkpoint, implementation report, QA report, and write record mandatory for closure. | Optional artifacts do not prove the visual skill workflow was followed. |
| Changed files | Auto-decided | Derive changed files from source diff/checkpoint snapshots. | Agent-supplied written files are useful UX but not strong evidence. |
| Fresh checkout | Auto-decided | Store closure fixture inputs in tracked test/docs fixture locations and regenerate into `obj` during the gate. | `obj` is disposable and cannot be the source of reproducible release evidence. |
| GitHub Actions | Auto-decided | Keep Actions out of the Phase 4.11 DoD. | The user has disabled Actions during active development; local gates are authoritative. |
| Pixel diff | Deferred | Do not add strict pixel-perfect scoring in this phase. | Reference comparison is needed, but strict scoring would be noisy before stable baselines and acceptance rules exist. |

## Phase Order

Implement in this order:

1. Phase 4.11.0 - Closure Contract Baseline
2. Phase 4.11.1 - Mandatory Visual Artifact Chain
3. Phase 4.11.2 - Automatic Changed-File Detection
4. Phase 4.11.3 - Runtime Visual Proof Mode
5. Phase 4.11.4 - Reference Visual QA Contract
6. Phase 4.11.5 - Functional And Commerce Closure Gate
7. Phase 4.11.6 - Fresh Checkout Reproducible Pilot
8. Phase 4.11.7 - Final End-To-End Closure
9. Phase 4.11.8 - Documentation And Agent Guide Updates

Do not implement runtime visual proof before the mandatory artifact chain and changed-file detection are in place. Otherwise a runtime pass could still hide missing visual workflow evidence.

## Phase 4.11.0 - Closure Contract Baseline

Goal: lock the exact closure contract and test the current gaps before changing scripts.

Tasks:

- [x] Reconfirm the current final closure gate still uses `-ProofLevel Structure`.
- [x] Reconfirm the current MVP gate accepts file fixture visual QA.
- [x] Reconfirm the current MVP gate validates visual artifacts only when present.
- [x] Reconfirm `run-visual-qa.mjs` still reports reference visual diff as not implemented.
- [x] Reconfirm `record-agent-visual-writes.mjs` still requires `--written-files`.
- [x] Reconfirm `run-storefront-builder-generated-proof.ps1` supports `FoundationFunctionalFast` and `FoundationFunctionalFull`.
- [x] Reconfirm `run-storefront-builder-full-proof-with-fixture.ps1` owns fixture runtime bootstrap and teardown.
- [x] Decide the tracked fixture location for Phase 4.11:
  - [x] StorefrontBuilder closure fixture under `tools/BlazorShop.AI.StorefrontBuilder/tests/...` when the fixture is consumed only by StorefrontBuilder gates.
  - [x] ReverseEngineering fixture under `tools/BlazorShop.AI.StorefrontReverseEngineering/tests/BlazorShop.AI.StorefrontReverseEngineering.Tests/Fixtures/...` only when the source handoff package must be produced or validated by ReverseEngineering tests.
- [x] Add a short baseline note to the implementation PR or commit message.

Checks:

```powershell
Select-String -Path scripts\qa\run-storefront-phase4-final-closure-gate.ps1 -Pattern "ProofLevel|PilotGeneratedProjectRoot|PilotFixtureRoot|PilotHandoffRoot"
Select-String -Path scripts\qa\run-storefront-phase4-mvp-gate.ps1 -Pattern "validate visual schemas when present|FixtureRoot|BaseUrl|visual-plan|visual-implementation|visual-qa"
Select-String -Path tools\BlazorShop.AI.StorefrontBuilder\scripts\qa\run-visual-qa.mjs -Pattern "Reference visual diff|fixtureRoot|pathToFileURL|Major threshold"
Select-String -Path tools\BlazorShop.AI.StorefrontBuilder\scripts\generate\record-agent-visual-writes.mjs -Pattern "written-files"
Select-String -Path scripts\qa\run-storefront-builder-generated-proof.ps1 -Pattern "FoundationFunctionalFast|FoundationFunctionalFull|run-commerce-regression"
```

DoD:

- [x] Baseline findings are documented.
- [x] No behavior changes are made before the closure contract is locked.
- [x] The fixture ownership decision is recorded.

Phase 4.11.0 evidence:

- `scripts/qa/run-storefront-phase4-final-closure-gate.ps1` still defaulted pilot input to `obj/...` paths and invoked `run-storefront-builder-generated-proof.ps1 -ProofLevel Structure`.
- `scripts/qa/run-storefront-phase4-mvp-gate.ps1` still accepted optional `-FixtureRoot` and `-BaseUrl`; the visual QA call passed either `--fixture-root` or `--base-url` when supplied.
- MVP visual JSON validation was still labelled `validate visual schemas when present` and checked only `visual-plan.json`, `visual-implementation-report.json`, and `visual-qa-report.json` when present.
- `run-visual-qa.mjs` still emitted `Reference visual diff: not implemented`, supported file URLs through `fixtureRoot`/`pathToFileURL`, and documented `Major threshold: 3`.
- `record-agent-visual-writes.mjs` still required at least one `--written-files` value.
- `run-storefront-builder-generated-proof.ps1` supports `FoundationFunctionalFast`, `FoundationFunctionalFull`, and invokes `run-commerce-regression.mjs` for live proof.
- `run-storefront-builder-full-proof-with-fixture.ps1` owns `scripts/run-v2-local.ps1` startup, Commerce Node fixture endpoint checks, `FoundationFunctionalFull`, report writing, and `finally` teardown.
- Fixture ownership decision: Phase 4.11 closure fixtures belong under `tools/BlazorShop.AI.StorefrontBuilder/tests/...` because the hardened closure gate consumes them directly. Use the ReverseEngineering test fixture path only if a future change needs ReverseEngineering test code to produce or validate the source handoff package.

## Phase 4.11.1 - Mandatory Visual Artifact Chain

Goal: make the Phase 4 MVP gate prove the full visual workflow happened, not only that a generated project builds.

Tasks:

- [x] Update `tools/BlazorShop.AI.Visual/schemas/visual-plan.schema.json` if needed so closure-required fields are explicit:
  - [x] `operationId`.
  - [x] `projectName`.
  - [x] `storeKey`.
  - [x] `handoffHash`.
  - [x] `generationPlanHash`.
  - [x] `taskPackageHash`.
  - [x] page and viewport coverage.
  - [x] planned generated-owned files.
  - [x] protected files.
  - [x] risks and blockers.
- [x] Update `visual-implementation-checklist.schema.json` so status values are closure-friendly and unambiguous:
  - [x] `completed`.
  - [x] `blocked`.
  - [x] `not-applicable`.
- [x] Update `visual-checkpoint.schema.json` to support a strong chain:
  - [x] `operationId`.
  - [x] `visualPlanHash`.
  - [x] `checklistHash`.
  - [x] `preEditSnapshotHash`.
  - [x] `postEditSnapshotHash`.
  - [x] `changedFiles`.
  - [x] `unexpectedFiles`.
  - [x] `sourceTreeSnapshotScope`.
- [x] Update `visual-implementation-report.schema.json` to require:
  - [x] operation ID.
  - [x] checkpoint path.
  - [x] changed files.
  - [x] recorder result path.
  - [x] boundary validation result.
  - [x] build result.
  - [x] unresolved items.
- [x] Update `visual-qa-report.schema.json` for closure:
  - [x] operation ID.
  - [x] runtime evidence paths.
  - [x] reference evidence paths.
  - [x] page and viewport coverage.
  - [x] independent reviewer field.
  - [x] comparison dimensions.
  - [x] unaccepted critical count.
  - [x] unaccepted major count.
  - [x] pass/fail decision.
- [x] Update example JSON artifacts under `tools/BlazorShop.AI.Visual/examples/`.
- [x] Update `tools/BlazorShop.AI.Visual/scripts/validate-visual-examples.mjs` only as needed to validate the stronger required fields.
- [x] Update `scripts/qa/run-storefront-phase4-mvp-gate.ps1` so closure mode requires:
  - [x] `docs/storefront-analysis/visual-plan.json`.
  - [x] `docs/storefront-analysis/visual-implementation-checklist.json`.
  - [x] `docs/storefront-analysis/visual-checkpoints/{operationId}/visual-checkpoint.json`.
  - [x] `docs/storefront-analysis/visual-implementation-report.json`.
  - [x] `docs/storefront-analysis/visual-qa-report.json`.
  - [x] `docs/storefront-analysis/agent-written-files.json`.
- [x] Keep a compatibility or skeleton mode only if needed, but name it clearly so it cannot be confused with release closure.
- [x] Fail with problem/cause/fix when any mandatory artifact is missing.

Checks:

```powershell
node tools\BlazorShop.AI.Visual\scripts\validate-visual-examples.mjs
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\qa\run-storefront-phase4-mvp-gate.ps1 -Help
rg -n "visual-implementation-checklist|visual-checkpoint|unacceptedMajor|unacceptedCritical|operationId" tools\BlazorShop.AI.Visual scripts\qa\run-storefront-phase4-mvp-gate.ps1
```

DoD:

- [x] Missing visual plan, checklist, checkpoint, implementation report, QA report, or write record fails the closure gate.
- [x] Example artifacts validate against the stronger schemas.
- [x] Error messages tell the agent exactly which artifact to create and which command to rerun.

Phase 4.11.1 evidence:

- Strengthened visual plan/checklist/checkpoint/implementation/QA schemas and matching valid examples.
- Kept the Visual workspace dependency-free; `validate-visual-examples.mjs` required no code changes because it already enforces required fields and additional properties.
- Added MVP gate `-SkeletonProof` as the explicit non-release compatibility mode.
- Default MVP gate closure mode now requires `visual-plan.json`, `visual-implementation-checklist.json`, `visual-checkpoints/{operationId}/visual-checkpoint.json`, `visual-implementation-report.json`, `visual-qa-report.json`, and `agent-written-files.json`.
- Updated visual skill instructions so closure checklist JSON is the gate contract and `.todo.md` is only a human-readable mirror.
- `node tools\BlazorShop.AI.Visual\scripts\validate-visual-examples.mjs` passed with `Visual schema examples validated: 6.`
- `powershell -NoProfile -ExecutionPolicy Bypass -File scripts\qa\run-storefront-phase4-mvp-gate.ps1 -Help` passed and documents `-SkeletonProof`.
- A closure-mode run against the older Phase 4.10 pilot failed before build/browser because `visual-plan.json` lacked required `operationId`, proving missing/stale closure artifacts now block the gate with problem/fix/rerun output.

## Phase 4.11.2 - Automatic Changed-File Detection

Goal: stop trusting the agent-supplied file list as the source of truth.

Tasks:

- [x] Add a StorefrontBuilder helper to compute changed generated visual files from a source snapshot or diff:
  - [x] before snapshot from planned generated-owned visual files.
  - [x] after snapshot from the same scope plus newly created allowed files.
  - [x] changed, created, deleted, and unexpected files.
  - [x] normalized repo-relative or generated-project-relative paths.
- [x] Update `record-agent-visual-writes.mjs` to support automatic detection:
  - [x] Keep `--written-files` as optional hint/backcompat.
  - [x] Add `--from-checkpoint <path>` or `--detect-from-snapshot`.
  - [x] Reject files changed outside task-package allowed visual files.
  - [x] Reject protected generated files.
  - [x] Reject route declarations, transport, auth, SEO, backend/API calls, and business logic leaks as today.
  - [x] Write detected files into `agent-written-files.json`.
  - [x] Record whether each file came from auto-detection, hint agreement, or hint mismatch.
- [x] Add mismatch handling:
  - [x] If hint omits a changed file, fail unless the file is user-owned and outside closure scope.
  - [x] If hint includes an unchanged file, warn or fail based on closure mode.
  - [x] If auto-detection finds no changes but implementation report claims changes, fail.
- [x] Update visual checkpoint creation docs and skill instructions so the implement skill captures pre/post snapshots.
- [x] Add unit tests or script-level tests for:
  - [x] automatic detection success.
  - [x] extra unexpected file failure.
  - [x] omitted changed file failure.
  - [x] protected file failure.
  - [x] unchanged hint handling.
- [x] Ensure the MVP gate reads `agent-written-files.json` and verifies it was produced by auto-detection in closure mode.

Checks:

```powershell
node --check tools\BlazorShop.AI.StorefrontBuilder\scripts\generate\record-agent-visual-writes.mjs
node tools\BlazorShop.AI.StorefrontBuilder\scripts\generate\record-agent-visual-writes.mjs --help
dotnet test tools\BlazorShop.AI.StorefrontReverseEngineering\tests\BlazorShop.AI.StorefrontReverseEngineering.Tests\BlazorShop.AI.StorefrontReverseEngineering.Tests.csproj --no-restore --filter "FullyQualifiedName~StorefrontBuilderHandoffBoundaryValidationTests|FullyQualifiedName~StorefrontBuilderHandoffVisualQaTests" --blame-hang --blame-hang-timeout 5m
rg -n "auto-detect|checkpoint|unexpectedFiles|hintMismatch|agent-written-files" tools\BlazorShop.AI.StorefrontBuilder tools\BlazorShop.AI.Visual scripts\qa
```

DoD:

- [x] Closure write evidence is derived from actual generated project source state.
- [x] `--written-files` is no longer the only truth source.
- [x] Unexpected visual, protected, platform, or behavior files fail before browser QA.

Evidence:

- `record-agent-visual-writes.mjs` now supports `--from-checkpoint`, optional `--written-files` hints, closure-mode mismatch failures, deleted-file detection, protected-file rejection, and per-file `detectionSource`.
- `agent-written-files.json` now records `detectionMode`, `checkpointPath`, `hintMismatch`, `hintFiles`, `detectedFiles`, `deletedFiles`, and `unexpectedFiles`.
- The Phase 4 MVP gate requires `agent-written-files.json` to have `detectionMode: checkpoint-auto-detect` in closure mode.
- Visual implement/QA skill docs and the checkpoint contract now require checkpoint pre/post snapshots and checkpoint-driven recorder execution.
- `node --check tools\BlazorShop.AI.StorefrontBuilder\scripts\generate\record-agent-visual-writes.mjs` passed.
- `node tools\BlazorShop.AI.StorefrontBuilder\scripts\generate\record-agent-visual-writes.mjs --help` passed and lists `--from-checkpoint`, `--implementation-report`, and `--closure-mode`.
- `dotnet test tools\BlazorShop.AI.StorefrontReverseEngineering\tests\BlazorShop.AI.StorefrontReverseEngineering.Tests\BlazorShop.AI.StorefrontReverseEngineering.Tests.csproj --no-restore --filter "FullyQualifiedName~StorefrontBuilderHandoffBoundaryValidationTests|FullyQualifiedName~StorefrontBuilderHandoffVisualQaTests|FullyQualifiedName~StorefrontBuilderAgentTaskPackageTests" --blame-hang --blame-hang-timeout 5m` passed: 33 tests in 3m 50s.
- `rg -n "auto-detect|checkpoint|unexpectedFiles|hintMismatch|agent-written-files" tools\BlazorShop.AI.StorefrontBuilder tools\BlazorShop.AI.Visual scripts\qa` returned the expected recorder, gate, docs, and test references.

## Phase 4.11.3 - Runtime Visual Proof Mode

Goal: separate static skeleton proof from generated Blazor runtime proof and make runtime proof mandatory for closure.

Tasks:

- [x] Update `run-visual-qa.mjs` to expose explicit proof modes:
  - [x] `--proof-mode skeleton` for file fixture proof.
  - [x] `--proof-mode runtime` for running generated Blazor storefront proof.
  - [x] optional default remains backward-compatible, but final gates must pass `runtime`.
- [x] In runtime mode:
  - [x] Require `--base-url`.
  - [x] Reject `--fixture-root`.
  - [x] Capture HTTP status for each route.
  - [x] Fail on unresolved page errors.
  - [x] Fail on unaccepted console errors.
  - [x] Fail on unaccepted failed network requests.
  - [x] Verify generated CSS and assets load from the runtime host.
  - [x] Verify body nonblank and required slots visible.
  - [x] Verify no direct Commerce Node, Control Plane, Commerce Admin, or legacy API calls from browser.
- [x] Add or extract a small generated-host startup wrapper only if current generated proof scripts cannot be reused directly.
  - [x] Prefer reusing `run-storefront-builder-generated-proof.ps1` and `run-storefront-builder-full-proof-with-fixture.ps1`.
  - [x] If adding `start-generated-storefront.ps1`, keep it in `scripts/qa/` or StorefrontBuilder scripts and make it a thin wrapper.
  - [x] Ensure startup wrapper has deterministic port handling and teardown.
- [x] Update `run-storefront-phase4-mvp-gate.ps1`:
  - [x] Add an explicit `-ProofMode` or `-RequireRuntime` switch.
  - [x] In closure mode, require `-BaseUrl` or start the generated host itself.
  - [x] Do not pass `-FixtureRoot` for release closure.
- [x] Keep skeleton fixture mode available only for early visual plan/placeholder proof.
- [x] Add report fields distinguishing skeleton proof from runtime proof.

Checks:

```powershell
node tools\BlazorShop.AI.StorefrontBuilder\scripts\qa\run-visual-qa.mjs --help
node --check tools\BlazorShop.AI.StorefrontBuilder\scripts\qa\run-visual-qa.mjs
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\qa\run-storefront-phase4-mvp-gate.ps1 -Help
rg -n "proof-mode|RequireRuntime|fixture-root|base-url|requestfailed|console.error|direct Commerce" tools\BlazorShop.AI.StorefrontBuilder scripts\qa
```

DoD:

- [x] Runtime proof cannot accidentally fall back to file fixture proof.
- [x] Closure mode requires generated Blazor host evidence.
- [x] Browser console, page error, and network failures are visible in the report and fail when unaccepted.

Evidence:

- `run-visual-qa.mjs` now supports `--proof-mode skeleton|runtime`; skeleton requires `--fixture-root`, runtime requires `--base-url` and rejects `--fixture-root`.
- Runtime visual QA records route HTTP statuses, runtime network audit entries, CSS responses, browser events, same-origin runtime asset checks, and forbidden direct browser API calls.
- Runtime mode fails on page errors, console errors, request failures, invalid/off-origin CSS, off-origin runtime assets, non-2xx/3xx route statuses, blank body, missing required slots, and direct `/api/storefront/stores/*`, `/api/commerce/*`, `/api/control-plane/*`, `/api/admin/*`, `/api/public/*`, or `/api/internal/*` calls.
- No new startup wrapper was added; runtime bootstrap remains owned by `run-storefront-builder-generated-proof.ps1` and `run-storefront-builder-full-proof-with-fixture.ps1`.
- `run-storefront-phase4-mvp-gate.ps1` now exposes `-ProofMode`, defaults closure to `Runtime`, requires `-BaseUrl` in runtime mode, rejects `-FixtureRoot` in runtime mode, and passes `--proof-mode` to visual QA.
- `node --check tools\BlazorShop.AI.StorefrontBuilder\scripts\qa\run-visual-qa.mjs` passed.
- `node tools\BlazorShop.AI.StorefrontBuilder\scripts\qa\run-visual-qa.mjs --help` passed and lists `--proof-mode`.
- `powershell -NoProfile -ExecutionPolicy Bypass -File scripts\qa\run-storefront-phase4-mvp-gate.ps1 -Help` passed and documents `-ProofMode <Skeleton|Runtime>`.
- `dotnet test tools\BlazorShop.AI.StorefrontReverseEngineering\tests\BlazorShop.AI.StorefrontReverseEngineering.Tests\BlazorShop.AI.StorefrontReverseEngineering.Tests.csproj --no-restore --filter "FullyQualifiedName~StorefrontBuilderHandoffVisualQaTests" --blame-hang --blame-hang-timeout 5m` passed: 10 tests in 3m 29s.
- `rg -n "proof-mode|Runtime Route Statuses|Runtime Network Audit|SFB-VISUAL-QA-00|ProofMode|SkeletonProof|BaseUrl|fixture-root" tools\BlazorShop.AI.StorefrontBuilder\scripts\qa\run-visual-qa.mjs scripts\qa\run-storefront-phase4-mvp-gate.ps1 tools\BlazorShop.AI.StorefrontReverseEngineering\tests\BlazorShop.AI.StorefrontReverseEngineering.Tests\StorefrontBuilderHandoffVisualQaTests.cs` returned the expected runtime/skeleton mode references.

## Phase 4.11.4 - Reference Visual QA Contract

Goal: make independent QA compare runtime output against approved reference evidence without requiring fragile pixel-perfect automation.

Tasks:

- [x] Update `tools/BlazorShop.AI.Visual/schemas/visual-qa-report.schema.json` to require:
  - [x] `referenceEvidenceReviewed`.
  - [x] `referenceEvidencePaths`.
  - [x] `runtimeEvidencePaths`.
  - [x] `pageViewportCoverage`.
  - [x] `comparisonDimensions`.
  - [x] `acceptedDifferences`.
  - [x] `unacceptedCriticalCount`.
  - [x] `unacceptedMajorCount`.
  - [x] `independentReviewer`.
  - [x] `finalDecision`.
- [x] Define severity vocabulary for closure:
  - [x] `Critical`: blank route, broken core layout, missing checkout/cart/account entry, blocked main flow, fatal runtime browser error.
  - [x] `Major`: visible mismatch against reference that harms ecommerce use, important responsive break, missing visual slot, broken gallery or product action area.
  - [x] `Minor`: polish difference that does not block release.
- [x] Update `storefront-visual-qa/SKILL.md`:
  - [x] Read `agent-task-package/manifest.json`, reference evidence paths, visual plan, implementation checklist, implementation report, checkpoint, and runtime visual QA report.
  - [x] Compare reference and runtime screenshots per required page/viewport.
  - [x] Record accepted differences with reason.
  - [x] Require zero unaccepted critical and zero unaccepted major for closure.
  - [x] Allow minor issues only if recorded with follow-up and not a release blocker.
- [x] Update `run-visual-qa.mjs`:
  - [x] Stop writing `Reference visual diff: not implemented` for closure mode.
  - [x] Either emit a JSON evidence report or produce machine-readable summary consumed by the QA skill.
  - [x] Include screenshot paths in stable generated-project-local or report-root-relative form.
- [x] Update MVP gate:
  - [x] Read `visual-qa-report.json`.
  - [x] Fail when `referenceEvidenceReviewed` is false.
  - [x] Fail when required page/viewport coverage is missing.
  - [x] Fail when unaccepted critical or major counts are nonzero.
- [x] Add negative fixture/test cases:
  - [x] reference evidence missing.
  - [x] runtime capture missing.
  - [x] major issue left unaccepted.
  - [x] QA report says pass but counters disagree.

Checks:

```powershell
node tools\BlazorShop.AI.Visual\scripts\validate-visual-examples.mjs
node tools\BlazorShop.AI.StorefrontBuilder\scripts\qa\run-visual-qa.mjs --help
rg -n "Reference visual diff|referenceEvidenceReviewed|unacceptedMajor|unacceptedCritical|acceptedDifferences|independentReviewer" tools\BlazorShop.AI.Visual tools\BlazorShop.AI.StorefrontBuilder scripts\qa
```

DoD:

- [x] Visual QA closure cannot pass without reference evidence review.
- [x] Closure requires zero unaccepted critical and zero unaccepted major issues.
- [x] Accepted differences are explicit and reviewable.
- [x] Pixel-perfect scoring remains deferred and clearly out of scope.

Evidence:

- `visual-qa-report.schema.json` now requires `referenceEvidenceReviewed`, evidence paths, coverage, comparison dimensions, `acceptedDifferences`, independent reviewer, final decision, and unaccepted critical/major counters.
- QA report issue severity now uses closure vocabulary: `Critical`, `Major`, and `Minor`; accepted differences require page, viewport, severity, reviewer, and reason.
- `storefront-visual-qa/SKILL.md` now requires reading reference evidence, comparing reference/runtime screenshots per page/viewport, recording accepted differences, and blocking closure on any unaccepted critical or major issue.
- `run-visual-qa.mjs` now writes `visual-qa-runtime-summary.json` with proof mode, route statuses, captures, CSS responses, browser events, runtime network audit, discrepancies, counts, and pass status.
- Runtime visual QA report no longer writes `Reference visual diff: not implemented`; it states independent reference review is required and pixel-perfect scoring is deferred.
- MVP gate now validates `visual-qa-report.json` reference review, runtime/reference evidence presence, required coverage from `visual-plan.json`, viewport capture coverage, zero unaccepted critical/major counters, and pass/counter consistency.
- Added negative MVP gate tests for missing reference review, missing runtime evidence, unaccepted major issue, pass flag with nonzero counters, and missing required viewport coverage.
- `node tools\BlazorShop.AI.Visual\scripts\validate-visual-examples.mjs` passed: 6 examples.
- `node --check tools\BlazorShop.AI.StorefrontBuilder\scripts\qa\run-visual-qa.mjs` passed.
- `node tools\BlazorShop.AI.StorefrontBuilder\scripts\qa\run-visual-qa.mjs --help` passed.
- `dotnet test tools\BlazorShop.AI.StorefrontReverseEngineering\tests\BlazorShop.AI.StorefrontReverseEngineering.Tests\BlazorShop.AI.StorefrontReverseEngineering.Tests.csproj --no-restore --filter "FullyQualifiedName~StorefrontBuilderHandoffVisualQaTests|FullyQualifiedName~StorefrontPhase4MvpGateVisualQaContractTests" --blame-hang --blame-hang-timeout 5m` passed: 15 tests in 3m 29s.
- `rg -n "Reference visual diff|referenceEvidenceReviewed|unacceptedMajor|unacceptedCritical|acceptedDifferences|independentReviewer" tools\BlazorShop.AI.Visual tools\BlazorShop.AI.StorefrontBuilder scripts\qa` returned the expected schema, skill, gate, example, and skeleton-only reference-diff references.

## Phase 4.11.5 - Functional And Commerce Closure Gate

Goal: make final closure prove generated storefront behavior, not only structure.

Tasks:

- [ ] Update `run-storefront-phase4-final-closure-gate.ps1` so it no longer relies only on `-ProofLevel Structure`.
- [ ] Decide the minimum default proof:
  - [ ] `FoundationFunctionalFast` for deterministic PR/local closure without live Commerce Node.
  - [ ] `FoundationFunctionalFull` or `run-storefront-builder-full-proof-with-fixture.ps1` for release closure when fixture runtime is available.
- [ ] Add gate parameters:
  - [ ] `-FunctionalProofLevel FoundationFunctionalFast|FoundationFunctionalFull`.
  - [ ] `-SkipFullFixtureProof` only if explicitly needed for local development, not for final release closure.
  - [ ] `-RequireCommerceRegression` for final closure when fixture data is available.
- [ ] In final closure mode, run:
  - [ ] visual workspace static checks.
  - [ ] generated project isolation gate.
  - [ ] generated proof at `FoundationFunctionalFast` minimum.
  - [ ] Phase 4 MVP gate in runtime closure mode.
  - [ ] commerce regression against generated runtime when fixture runtime is available.
  - [ ] regeneration/no-op ownership proof.
  - [ ] clean HEAD and clean working tree check.
- [ ] Reuse `run-commerce-regression.mjs` instead of duplicating commerce checks.
- [ ] Ensure COD/test payment flow can place an order against the fixture when full fixture proof is selected.
- [ ] Record which proof level was run in the final closure report.

Checks:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\qa\run-storefront-phase4-final-closure-gate.ps1 -Help
.\scripts\qa\run-storefront-builder-generated-proof.ps1 -ProofLevel FoundationFunctionalFast
.\scripts\qa\run-storefront-builder-full-proof-with-fixture.ps1 -Describe
rg -n "FoundationFunctionalFast|FoundationFunctionalFull|run-commerce-regression|RequireCommerceRegression|ProofLevel Structure" scripts\qa
```

DoD:

- [ ] Final closure gate no longer closes on structure-only proof.
- [ ] Fast functional proof is the minimum default.
- [ ] Full fixture commerce proof is available and documented for release closure.
- [ ] Final closure report records functional and commerce evidence paths.

## Phase 4.11.6 - Fresh Checkout Reproducible Pilot

Goal: remove hidden dependency on existing `obj` artifacts.

Tasks:

- [ ] Create a tracked Phase 4.11 closure fixture package:
  - [ ] portable handoff input or minimal fixture able to produce it.
  - [ ] expected project name and store key.
  - [ ] expected page/viewport coverage.
  - [ ] expected visual artifact manifest.
  - [ ] reference evidence paths or approved reference evidence fixture.
- [ ] Place fixture under the correct ownership path:
  - [ ] StorefrontBuilder test fixture path when consumed directly by StorefrontBuilder closure.
  - [ ] ReverseEngineering test fixture path only when it must be validated by ReverseEngineering test code.
- [ ] Update final closure gate defaults:
  - [ ] Do not default `PilotGeneratedProjectRoot` to a pre-existing generated project.
  - [ ] Generate the pilot into `obj/storefront-builder/generated/...` during the gate.
  - [ ] Clean/recreate the generated pilot output before use.
  - [ ] Derive `PilotHandoffRoot` from the tracked fixture or copy it into `obj` from tracked source during the gate.
- [ ] Ensure the gate fails if required tracked fixture files are missing.
- [ ] Ensure the gate does not write generated output into tracked source.
- [ ] Add deterministic cleanup rules:
  - [ ] success cleans transient generated output unless report retention is explicitly configured.
  - [ ] failure keeps enough report paths for investigation.
- [ ] Add tests or scripted checks proving:
  - [ ] missing fixture fails clearly.
  - [ ] fresh generation happens during the gate.
  - [ ] stale `obj` content is not reused.

Checks:

```powershell
Test-Path tools\BlazorShop.AI.StorefrontBuilder
Test-Path tools\BlazorShop.AI.StorefrontReverseEngineering\tests\BlazorShop.AI.StorefrontReverseEngineering.Tests\Fixtures
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\qa\run-storefront-phase4-final-closure-gate.ps1 -Help
rg -n "Phase4VisualPilot|PilotGeneratedProjectRoot|PilotHandoffRoot|obj\\storefront-reverse-engineering|fresh" scripts\qa docs\visual-reverse-engineering-skill tools
```

DoD:

- [ ] A clean checkout can run the final closure gate without pre-existing `obj` pilot artifacts.
- [ ] Tracked fixture input is the only source of pilot truth.
- [ ] Generated pilot output remains disposable.

## Phase 4.11.7 - Final End-To-End Closure

Goal: make one no-skip local command prove the whole Phase 4.11 closure contract.

Tasks:

- [ ] Update `run-storefront-phase4-final-closure-gate.ps1` to execute the final flow:
  - [ ] assert clean working tree at start.
  - [ ] capture tested HEAD.
  - [ ] validate Visual workspace remains docs/schema/skill-only.
  - [ ] validate Visual examples and stronger schemas.
  - [ ] validate StorefrontBuilder closure fixture exists.
  - [ ] preflight portable handoff.
  - [ ] plan deterministic generation.
  - [ ] generate fresh project from Starter into `obj`.
  - [ ] verify generated project isolation.
  - [ ] verify mandatory visual artifacts or create deterministic fixture artifacts for the pilot.
  - [ ] run automatic changed-file detection.
  - [ ] run generated Blazor runtime visual proof.
  - [ ] run independent reference visual QA contract checks.
  - [ ] run bounded visual repair only if configured and allowed.
  - [ ] rerun runtime visual QA after repair.
  - [ ] run `FoundationFunctionalFast` minimum.
  - [ ] run full fixture commerce proof when release closure mode is selected.
  - [ ] run regeneration/no-op ownership proof.
  - [ ] assert final HEAD unchanged.
  - [ ] assert clean working tree at end.
  - [ ] write final JSON and Markdown reports under `obj/storefront-builder/reports/`.
- [ ] Remove or rename any skip flags that would allow release closure without runtime visual proof or functional proof.
- [ ] Keep local-development switches clearly named as non-release modes.
- [ ] Ensure failure output includes:
  - [ ] problem.
  - [ ] likely cause.
  - [ ] exact rerun command.
  - [ ] report path.
  - [ ] evidence path.
  - [ ] whether generated artifacts were retained.
- [ ] Add final report fields:
  - [ ] tested HEAD.
  - [ ] final HEAD.
  - [ ] fixture source path.
  - [ ] generated project path.
  - [ ] proof mode.
  - [ ] visual artifact paths.
  - [ ] visual QA report path.
  - [ ] functional proof report path.
  - [ ] commerce proof report path when applicable.
  - [ ] regeneration report path.
  - [ ] final decision.

Checks:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\qa\run-storefront-phase4-final-closure-gate.ps1 -Help
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\qa\run-storefront-phase4-final-closure-gate.ps1 -CommandTimeoutSeconds 900
git status --porcelain=v1
```

DoD:

- [ ] Final closure gate passes from a clean unchanged `HEAD`.
- [ ] Final closure gate is reproducible from tracked inputs.
- [ ] Final closure gate does not require GitHub Actions.
- [ ] Final closure gate proves generated runtime visual QA and functional generated storefront behavior.
- [ ] Final report is enough to decide whether Phase 4 is complete.

## Phase 4.11.8 - Documentation And Agent Guide Updates

Goal: update the source-of-truth docs after the hardening behavior exists.

Tasks:

- [ ] Update `docs/architecture/11-storefront-builder.md`:
  - [ ] final closure now requires runtime visual proof.
  - [ ] static fixture proof is skeleton-only.
  - [ ] final closure uses tracked fixture input and fresh generated output.
  - [ ] final closure minimum functional proof is `FoundationFunctionalFast`.
  - [ ] full fixture commerce proof is the release-level proof when fixture runtime is available.
- [ ] Update `docs/visual-reverse-engineering-skill/README.md`:
  - [ ] add this plan to historical plans.
  - [ ] document Phase 4.11 closure flow.
  - [ ] document local gates and proof modes.
- [ ] Update `docs/visual-reverse-engineering-skill/reference.md`:
  - [ ] document mandatory visual artifacts.
  - [ ] document runtime visual QA.
  - [ ] document reference QA contract.
  - [ ] document automatic changed-file detection.
- [ ] Update `docs/visual-reverse-engineering-skill/how-to-generate-and-validate.md`:
  - [ ] show the no-skip closure workflow.
  - [ ] show skeleton proof only as early feedback.
- [ ] Update `docs/agents/storefront-builder.md`:
  - [ ] explain that final closure cannot rely on `obj` artifacts.
  - [ ] explain changed-file detection requirements.
  - [ ] explain mandatory runtime visual and functional proof.
- [ ] Update `AGENTS.md` only if agent required reading or closure commands change.
- [ ] Do not rewrite completed Phase 4.10 history except for factual corrections if any doc has become misleading.

Checks:

```powershell
rg -n "Phase 4.11|runtime visual proof|Reference visual|FoundationFunctionalFast|changed-file|tracked fixture|GitHub Actions" docs\architecture docs\visual-reverse-engineering-skill docs\agents AGENTS.md
```

DoD:

- [ ] Docs match the implemented closure behavior.
- [ ] Agents can run the closure workflow without conversation context.
- [ ] Historical Phase 4.10 remains implementation history, not the current closure source of truth.

## Required QA Gates By Phase

Use focused checks per commit.

After schema/artifact changes:

```powershell
node tools\BlazorShop.AI.Visual\scripts\validate-visual-examples.mjs
rg -n "visual-plan|visual-implementation-checklist|visual-checkpoint|visual-qa-report" tools\BlazorShop.AI.Visual\schemas tools\BlazorShop.AI.Visual\examples
```

After changed-file detection changes:

```powershell
node --check tools\BlazorShop.AI.StorefrontBuilder\scripts\generate\record-agent-visual-writes.mjs
node tools\BlazorShop.AI.StorefrontBuilder\scripts\generate\record-agent-visual-writes.mjs --help
dotnet test tools\BlazorShop.AI.StorefrontReverseEngineering\tests\BlazorShop.AI.StorefrontReverseEngineering.Tests\BlazorShop.AI.StorefrontReverseEngineering.Tests.csproj --no-restore --filter "FullyQualifiedName~StorefrontBuilderHandoffBoundaryValidationTests|FullyQualifiedName~StorefrontBuilderHandoffVisualQaTests" --blame-hang --blame-hang-timeout 5m
```

After runtime visual QA changes:

```powershell
node --check tools\BlazorShop.AI.StorefrontBuilder\scripts\qa\run-visual-qa.mjs
node tools\BlazorShop.AI.StorefrontBuilder\scripts\qa\run-visual-qa.mjs --help
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\qa\run-storefront-phase4-mvp-gate.ps1 -Help
```

After functional/final gate changes:

```powershell
.\scripts\qa\run-storefront-builder-generated-proof.ps1 -ProofLevel FoundationFunctionalFast
.\scripts\qa\run-storefront-builder-full-proof-with-fixture.ps1 -Describe
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\qa\run-storefront-phase4-final-closure-gate.ps1 -Help
```

Before closing Phase 4.11:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\qa\run-storefront-phase4-final-closure-gate.ps1 -CommandTimeoutSeconds 900
git status --porcelain=v1
```

Run the full fixture proof when release-level fixture services are available:

```powershell
.\scripts\qa\run-storefront-builder-full-proof-with-fixture.ps1
```

## Release Definition Of Done

- [ ] `tools/BlazorShop.AI.Visual` remains docs/schema/reference/skill-only.
- [ ] No `.csproj` exists under `tools/BlazorShop.AI.Visual`.
- [ ] No production project references `tools/BlazorShop.AI.Visual`.
- [ ] Phase 4 MVP gate requires the complete visual artifact chain in closure mode.
- [ ] `agent-written-files.json` is produced from automatic changed-file detection in closure mode.
- [ ] Runtime visual QA mode exists and rejects `--fixture-root`.
- [ ] Skeleton/static fixture mode is clearly non-release.
- [ ] Visual QA report records reference evidence reviewed.
- [ ] Visual QA closure requires zero unaccepted critical and zero unaccepted major issues.
- [ ] Final closure gate uses tracked fixture input and regenerates pilot output fresh.
- [ ] Final closure gate does not depend on pre-existing `obj` generated projects or handoff packages.
- [ ] Final closure gate runs `FoundationFunctionalFast` minimum.
- [ ] Full fixture commerce proof is documented and runnable when fixture runtime is available.
- [ ] Final closure gate starts and ends on the same clean `HEAD`.
- [ ] Final reports are written under ignored report/output folders.
- [ ] Documentation reflects the hardened closure behavior.
- [ ] GitHub Actions are not required for closure while disabled in development.

## Risk Register

| Risk | Impact | Mitigation |
| --- | --- | --- |
| Runtime proof duplicates existing generated proof scripts | Harder maintenance | Reuse `run-storefront-builder-generated-proof.ps1` and full fixture wrapper before adding helpers. |
| Reference QA becomes fake or subjective | Closure still misses visual defects | Require reference evidence paths, runtime evidence paths, coverage, severity counters, and accepted-difference records. |
| Strict pixel diff blocks useful progress | Noisy false failures | Defer pixel-perfect scoring; use structured review contract first. |
| Artifact chain becomes too heavy for early skeleton feedback | Slower iteration | Keep skeleton mode separate from closure mode. |
| Changed-file detection misses generated-owned new files | Protected drift or missing evidence | Snapshot the planned scope and scan for new files under allowed generated visual roots. |
| Final gate mutates tracked source | Dirty closure state | Assert clean working tree at start and end; write reports only to ignored locations. |
| Fresh fixture ownership is placed in the wrong tool | Confusing maintenance | Put closure fixture where the gate consumes it; use ReverseEngineering fixture path only for ReverseEngineering-owned validation. |
| Commerce fixture proof is slow locally | Developers avoid running it | Make `FoundationFunctionalFast` the minimum local closure and reserve full fixture proof for release-level closure when services are available. |

## Implementation Commit Plan

Keep each commit small enough to review.

1. `Phase 4.11.0: lock closure contract baseline`.
2. `Phase 4.11.1: require visual artifact chain`.
3. `Phase 4.11.2: add visual checkpoint changed-file detection`.
4. `Phase 4.11.3: separate skeleton and runtime visual proof`.
5. `Phase 4.11.4: add reference visual QA contract`.
6. `Phase 4.11.5: require functional generated proof in closure gate`.
7. `Phase 4.11.6: add tracked fresh-checkout closure fixture`.
8. `Phase 4.11.7: wire final no-skip end-to-end closure`.
9. `Phase 4.11.8: update architecture, visual docs, and agent guide`.

Do not combine runtime proof, reference QA, and fresh-checkout fixture into one commit. Those changes affect different failure modes and should be reviewable separately.

## Handoff To Implementation

Start with Phase 4.11.0 and Phase 4.11.1. The first implementation pass should not touch generated runtime startup or reference visual comparison. It should make the closure contract visible and make missing artifacts fail deterministically. Once the artifact chain is mandatory, implement automatic changed-file detection, then runtime visual proof, then reference QA, then final gate composition.

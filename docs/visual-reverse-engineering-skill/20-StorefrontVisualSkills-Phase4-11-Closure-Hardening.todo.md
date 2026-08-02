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

- [ ] Update `tools/BlazorShop.AI.Visual/schemas/visual-plan.schema.json` if needed so closure-required fields are explicit:
  - [ ] `operationId`.
  - [ ] `projectName`.
  - [ ] `storeKey`.
  - [ ] `handoffHash`.
  - [ ] `generationPlanHash`.
  - [ ] `taskPackageHash`.
  - [ ] page and viewport coverage.
  - [ ] planned generated-owned files.
  - [ ] protected files.
  - [ ] risks and blockers.
- [ ] Update `visual-implementation-checklist.schema.json` so status values are closure-friendly and unambiguous:
  - [ ] `completed`.
  - [ ] `blocked`.
  - [ ] `not-applicable`.
- [ ] Update `visual-checkpoint.schema.json` to support a strong chain:
  - [ ] `operationId`.
  - [ ] `visualPlanHash`.
  - [ ] `checklistHash`.
  - [ ] `preEditSnapshotHash`.
  - [ ] `postEditSnapshotHash`.
  - [ ] `changedFiles`.
  - [ ] `unexpectedFiles`.
  - [ ] `sourceTreeSnapshotScope`.
- [ ] Update `visual-implementation-report.schema.json` to require:
  - [ ] operation ID.
  - [ ] checkpoint path.
  - [ ] changed files.
  - [ ] recorder result path.
  - [ ] boundary validation result.
  - [ ] build result.
  - [ ] unresolved items.
- [ ] Update `visual-qa-report.schema.json` for closure:
  - [ ] operation ID.
  - [ ] runtime evidence paths.
  - [ ] reference evidence paths.
  - [ ] page and viewport coverage.
  - [ ] independent reviewer field.
  - [ ] comparison dimensions.
  - [ ] unaccepted critical count.
  - [ ] unaccepted major count.
  - [ ] pass/fail decision.
- [ ] Update example JSON artifacts under `tools/BlazorShop.AI.Visual/examples/`.
- [ ] Update `tools/BlazorShop.AI.Visual/scripts/validate-visual-examples.mjs` only as needed to validate the stronger required fields.
- [ ] Update `scripts/qa/run-storefront-phase4-mvp-gate.ps1` so closure mode requires:
  - [ ] `docs/storefront-analysis/visual-plan.json`.
  - [ ] `docs/storefront-analysis/visual-implementation-checklist.json`.
  - [ ] `docs/storefront-analysis/visual-checkpoints/{operationId}/visual-checkpoint.json`.
  - [ ] `docs/storefront-analysis/visual-implementation-report.json`.
  - [ ] `docs/storefront-analysis/visual-qa-report.json`.
  - [ ] `docs/storefront-analysis/agent-written-files.json`.
- [ ] Keep a compatibility or skeleton mode only if needed, but name it clearly so it cannot be confused with release closure.
- [ ] Fail with problem/cause/fix when any mandatory artifact is missing.

Checks:

```powershell
node tools\BlazorShop.AI.Visual\scripts\validate-visual-examples.mjs
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\qa\run-storefront-phase4-mvp-gate.ps1 -Help
rg -n "visual-implementation-checklist|visual-checkpoint|unacceptedMajor|unacceptedCritical|operationId" tools\BlazorShop.AI.Visual scripts\qa\run-storefront-phase4-mvp-gate.ps1
```

DoD:

- [ ] Missing visual plan, checklist, checkpoint, implementation report, QA report, or write record fails the closure gate.
- [ ] Example artifacts validate against the stronger schemas.
- [ ] Error messages tell the agent exactly which artifact to create and which command to rerun.

## Phase 4.11.2 - Automatic Changed-File Detection

Goal: stop trusting the agent-supplied file list as the source of truth.

Tasks:

- [ ] Add a StorefrontBuilder helper to compute changed generated visual files from a source snapshot or diff:
  - [ ] before snapshot from planned generated-owned visual files.
  - [ ] after snapshot from the same scope plus newly created allowed files.
  - [ ] changed, created, deleted, and unexpected files.
  - [ ] normalized repo-relative or generated-project-relative paths.
- [ ] Update `record-agent-visual-writes.mjs` to support automatic detection:
  - [ ] Keep `--written-files` as optional hint/backcompat.
  - [ ] Add `--from-checkpoint <path>` or `--detect-from-snapshot`.
  - [ ] Reject files changed outside task-package allowed visual files.
  - [ ] Reject protected generated files.
  - [ ] Reject route declarations, transport, auth, SEO, backend/API calls, and business logic leaks as today.
  - [ ] Write detected files into `agent-written-files.json`.
  - [ ] Record whether each file came from auto-detection, hint agreement, or hint mismatch.
- [ ] Add mismatch handling:
  - [ ] If hint omits a changed file, fail unless the file is user-owned and outside closure scope.
  - [ ] If hint includes an unchanged file, warn or fail based on closure mode.
  - [ ] If auto-detection finds no changes but implementation report claims changes, fail.
- [ ] Update visual checkpoint creation docs and skill instructions so the implement skill captures pre/post snapshots.
- [ ] Add unit tests or script-level tests for:
  - [ ] automatic detection success.
  - [ ] extra unexpected file failure.
  - [ ] omitted changed file failure.
  - [ ] protected file failure.
  - [ ] unchanged hint handling.
- [ ] Ensure the MVP gate reads `agent-written-files.json` and verifies it was produced by auto-detection in closure mode.

Checks:

```powershell
node --check tools\BlazorShop.AI.StorefrontBuilder\scripts\generate\record-agent-visual-writes.mjs
node tools\BlazorShop.AI.StorefrontBuilder\scripts\generate\record-agent-visual-writes.mjs --help
dotnet test tools\BlazorShop.AI.StorefrontReverseEngineering\tests\BlazorShop.AI.StorefrontReverseEngineering.Tests\BlazorShop.AI.StorefrontReverseEngineering.Tests.csproj --no-restore --filter "FullyQualifiedName~StorefrontBuilderHandoffBoundaryValidationTests|FullyQualifiedName~StorefrontBuilderHandoffVisualQaTests" --blame-hang --blame-hang-timeout 5m
rg -n "auto-detect|checkpoint|unexpectedFiles|hintMismatch|agent-written-files" tools\BlazorShop.AI.StorefrontBuilder tools\BlazorShop.AI.Visual scripts\qa
```

DoD:

- [ ] Closure write evidence is derived from actual generated project source state.
- [ ] `--written-files` is no longer the only truth source.
- [ ] Unexpected visual, protected, platform, or behavior files fail before browser QA.

## Phase 4.11.3 - Runtime Visual Proof Mode

Goal: separate static skeleton proof from generated Blazor runtime proof and make runtime proof mandatory for closure.

Tasks:

- [ ] Update `run-visual-qa.mjs` to expose explicit proof modes:
  - [ ] `--proof-mode skeleton` for file fixture proof.
  - [ ] `--proof-mode runtime` for running generated Blazor storefront proof.
  - [ ] optional default remains backward-compatible, but final gates must pass `runtime`.
- [ ] In runtime mode:
  - [ ] Require `--base-url`.
  - [ ] Reject `--fixture-root`.
  - [ ] Capture HTTP status for each route.
  - [ ] Fail on unresolved page errors.
  - [ ] Fail on unaccepted console errors.
  - [ ] Fail on unaccepted failed network requests.
  - [ ] Verify generated CSS and assets load from the runtime host.
  - [ ] Verify body nonblank and required slots visible.
  - [ ] Verify no direct Commerce Node, Control Plane, Commerce Admin, or legacy API calls from browser.
- [ ] Add or extract a small generated-host startup wrapper only if current generated proof scripts cannot be reused directly.
  - [ ] Prefer reusing `run-storefront-builder-generated-proof.ps1` and `run-storefront-builder-full-proof-with-fixture.ps1`.
  - [ ] If adding `start-generated-storefront.ps1`, keep it in `scripts/qa/` or StorefrontBuilder scripts and make it a thin wrapper.
  - [ ] Ensure startup wrapper has deterministic port handling and teardown.
- [ ] Update `run-storefront-phase4-mvp-gate.ps1`:
  - [ ] Add an explicit `-ProofMode` or `-RequireRuntime` switch.
  - [ ] In closure mode, require `-BaseUrl` or start the generated host itself.
  - [ ] Do not pass `-FixtureRoot` for release closure.
- [ ] Keep skeleton fixture mode available only for early visual plan/placeholder proof.
- [ ] Add report fields distinguishing skeleton proof from runtime proof.

Checks:

```powershell
node tools\BlazorShop.AI.StorefrontBuilder\scripts\qa\run-visual-qa.mjs --help
node --check tools\BlazorShop.AI.StorefrontBuilder\scripts\qa\run-visual-qa.mjs
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\qa\run-storefront-phase4-mvp-gate.ps1 -Help
rg -n "proof-mode|RequireRuntime|fixture-root|base-url|requestfailed|console.error|direct Commerce" tools\BlazorShop.AI.StorefrontBuilder scripts\qa
```

DoD:

- [ ] Runtime proof cannot accidentally fall back to file fixture proof.
- [ ] Closure mode requires generated Blazor host evidence.
- [ ] Browser console, page error, and network failures are visible in the report and fail when unaccepted.

## Phase 4.11.4 - Reference Visual QA Contract

Goal: make independent QA compare runtime output against approved reference evidence without requiring fragile pixel-perfect automation.

Tasks:

- [ ] Update `tools/BlazorShop.AI.Visual/schemas/visual-qa-report.schema.json` to require:
  - [ ] `referenceEvidenceReviewed`.
  - [ ] `referenceEvidencePaths`.
  - [ ] `runtimeEvidencePaths`.
  - [ ] `pageViewportCoverage`.
  - [ ] `comparisonDimensions`.
  - [ ] `acceptedDifferences`.
  - [ ] `unacceptedCriticalCount`.
  - [ ] `unacceptedMajorCount`.
  - [ ] `independentReviewer`.
  - [ ] `finalDecision`.
- [ ] Define severity vocabulary for closure:
  - [ ] `Critical`: blank route, broken core layout, missing checkout/cart/account entry, blocked main flow, fatal runtime browser error.
  - [ ] `Major`: visible mismatch against reference that harms ecommerce use, important responsive break, missing visual slot, broken gallery or product action area.
  - [ ] `Minor`: polish difference that does not block release.
- [ ] Update `storefront-visual-qa/SKILL.md`:
  - [ ] Read `agent-task-package/manifest.json`, reference evidence paths, visual plan, implementation checklist, implementation report, checkpoint, and runtime visual QA report.
  - [ ] Compare reference and runtime screenshots per required page/viewport.
  - [ ] Record accepted differences with reason.
  - [ ] Require zero unaccepted critical and zero unaccepted major for closure.
  - [ ] Allow minor issues only if recorded with follow-up and not a release blocker.
- [ ] Update `run-visual-qa.mjs`:
  - [ ] Stop writing `Reference visual diff: not implemented` for closure mode.
  - [ ] Either emit a JSON evidence report or produce machine-readable summary consumed by the QA skill.
  - [ ] Include screenshot paths in stable generated-project-local or report-root-relative form.
- [ ] Update MVP gate:
  - [ ] Read `visual-qa-report.json`.
  - [ ] Fail when `referenceEvidenceReviewed` is false.
  - [ ] Fail when required page/viewport coverage is missing.
  - [ ] Fail when unaccepted critical or major counts are nonzero.
- [ ] Add negative fixture/test cases:
  - [ ] reference evidence missing.
  - [ ] runtime capture missing.
  - [ ] major issue left unaccepted.
  - [ ] QA report says pass but counters disagree.

Checks:

```powershell
node tools\BlazorShop.AI.Visual\scripts\validate-visual-examples.mjs
node tools\BlazorShop.AI.StorefrontBuilder\scripts\qa\run-visual-qa.mjs --help
rg -n "Reference visual diff|referenceEvidenceReviewed|unacceptedMajor|unacceptedCritical|acceptedDifferences|independentReviewer" tools\BlazorShop.AI.Visual tools\BlazorShop.AI.StorefrontBuilder scripts\qa
```

DoD:

- [ ] Visual QA closure cannot pass without reference evidence review.
- [ ] Closure requires zero unaccepted critical and zero unaccepted major issues.
- [ ] Accepted differences are explicit and reviewable.
- [ ] Pixel-perfect scoring remains deferred and clearly out of scope.

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

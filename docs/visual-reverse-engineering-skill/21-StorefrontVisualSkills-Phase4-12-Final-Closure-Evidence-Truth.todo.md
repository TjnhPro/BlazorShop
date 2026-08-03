# StorefrontVisualSkills Phase 4.12 Final Closure Evidence Truth.todo

Status: In Progress
Owner: Storefront Platform
Created: 2026-08-03
Target folder: `docs/visual-reverse-engineering-skill`

Depends on:

- `docs/architecture/11-storefront-builder.md`
- `docs/visual-reverse-engineering-skill/README.md`
- `docs/agents/storefront-builder.md`
- `docs/visual-reverse-engineering-skill/20-StorefrontVisualSkills-Phase4-11-Closure-Hardening.todo.md`
- `scripts/qa/run-storefront-phase4-final-closure-gate.ps1`
- `scripts/qa/run-storefront-phase4-mvp-gate.ps1`
- `tools/BlazorShop.AI.StorefrontBuilder/build-storefront.ps1`
- `tools/BlazorShop.AI.StorefrontBuilder/scripts/generate/Test-HandoffPreflight.ps1`
- `tools/BlazorShop.AI.StorefrontBuilder/scripts/generate/new-storefront-project.ps1`
- `tools/BlazorShop.AI.StorefrontBuilder/scripts/generate/write-agent-task-package.mjs`
- `tools/BlazorShop.AI.StorefrontBuilder/scripts/generate/record-agent-visual-writes.mjs`
- `tools/BlazorShop.AI.StorefrontBuilder/scripts/qa/run-visual-qa.mjs`
- `tools/BlazorShop.AI.StorefrontReverseEngineering/Schemas`

Primary goal: close the remaining Phase 4 final closure evidence gap. The final local gate must prove that the current repository can start from a tracked portable handoff fixture, generate a fresh StorefrontBuilder pilot through the official handoff path, apply a deterministic generated-source visual edit, record real source hashes, run runtime browser visual QA, materialize a Reference QA report from current-run evidence, pass the MVP gate, pass generated functional proof, pass regeneration ownership proof, and finish on the same clean `HEAD`.

This phase is a closure patch, not a new architecture phase.

## Why This File Exists

Phase 4.11 hardened most of the StorefrontBuilder visual workflow. The remaining closure blocker is evidence truthfulness. The current final closure gate can still pass with seeded or manually produced proof artifacts:

- The tracked `phase4-11-closure/portable-handoff` fixture is only a marker folder with `README.md`.
- The final closure gate runs `build-storefront.ps1 -Mode generate` without `-HandoffRoot` or `-HandoffSchemaRoot`.
- The final closure gate writes `generation-plan.json` through the non-handoff `plan-generation-files.mjs` path after generation.
- The final closure gate writes an agent task package manually through `Write-PilotAgentTaskPackage`.
- Fixture checkpoint and implementation report hashes can be placeholders instead of SHA-256 values from current generated source.
- `record-agent-visual-writes.mjs` detects changed files from checkpoint data, but it does not yet prove that checkpoint `postEditFileHashes` match the current file contents.
- Runtime visual QA writes `visual-qa-runtime-summary.json`, but the final Reference QA contract is not strongly bound to that current-run runtime evidence.
- `visual-qa-report.json` can be seeded before runtime proof instead of materialized from the runtime summary and screenshot files produced by the current run.

Final target flow:

```text
clean HEAD
  -> tracked portable handoff fixture
  -> StorefrontBuilder handoff preflight
  -> StorefrontBuilder handoff generation plan
  -> fresh generated pilot from Starter
  -> StorefrontBuilder-generated agent task package
  -> deterministic allowed visual edit
  -> real pre/post checkpoint hashes
  -> automatic changed-file recorder with current source hash verification
  -> generated runtime host
  -> runtime visual QA screenshots and runtime summary
  -> materialized Reference QA report from current-run evidence
  -> MVP gate verifies evidence binding
  -> FoundationFunctionalFast generated browser proof
  -> regeneration ownership proof
  -> final clean HEAD
```

## Locked Scope

Implement only closure evidence truthfulness:

- valid portable handoff fixture input;
- handoff-first final gate generation;
- StorefrontBuilder-generated plan and task package;
- deterministic visual edit on allowed generated source;
- real current-source SHA-256 checkpoint;
- runtime screenshot and runtime summary binding;
- Reference QA report materialized from current-run runtime evidence;
- positive and negative tests for the proof chain;
- docs that describe the final local closure command.

## Explicitly Out Of Scope

- Pixel-perfect visual diff scoring.
- Requiring `FoundationFunctionalFull` for normal Phase 4 MVP closure.
- GitHub Actions closure evidence while Actions are disabled during development.
- New AI model orchestration, multi-agent runner, workflow database, job service, or queue.
- New backend, Presentation, Storefront Runtime, cart, checkout, account, payment, order, or Commerce Node behavior.
- Refactoring StorefrontBuilder beyond the scripts needed to make the final proof honest.
- Adding generated pilot projects, runtime screenshots, `obj` output, or transient generated assets to Git.
- Multiple reference websites.
- Reopening ReverseEngineering Phase 3 capture unless the tracked handoff fixture cannot be assembled from existing schema contracts.

## Autoplan Review Decisions

| Decision | Classification | Chosen direction | Rationale |
| --- | --- | --- | --- |
| Closure patch size | Auto-decided | Keep this as a small Phase 4.12 closure patch. | The architecture and most scripts exist; the gap is evidence provenance, not missing core architecture. |
| Portable handoff source | Auto-decided | Replace marker fixture with a minimal valid portable handoff package. | Final closure cannot claim handoff consumption if the source fixture is only `README.md`. |
| Generation source | Auto-decided | Final closure must call `build-storefront.ps1` with `-HandoffRoot` and `-HandoffSchemaRoot`. | This is the official StorefrontBuilder Phase 4 consumption surface. |
| Task package ownership | Auto-decided | Remove the manual `Write-PilotAgentTaskPackage` path from final closure. | The task package must be produced by StorefrontBuilder from handoff inputs, not by the gate. |
| Checkpoint hashes | Auto-decided | Compute hashes from actual generated files before and after deterministic edit. | Placeholder hash strings do not prove source state. |
| Runtime QA evidence | Auto-decided | Bind `visual-qa-report.json` to `visual-qa-runtime-summary.json` and screenshot files from the same current run. | A pre-copied pass report can otherwise satisfy schema checks without proving browser evidence. |
| Reference QA | Auto-decided | Materialize structured Reference QA from runtime summary and tracked reference evidence. | Pixel scoring is deferred, but the reviewer/report contract must be tied to current screenshots. |
| Functional proof level | Auto-decided | Keep `FoundationFunctionalFast` as the minimum final closure proof. | It is fast enough for local closure and already covers generated browser behavior; full fixture proof remains release-level optional. |
| CI evidence | Auto-decided | Do not require GitHub Actions in this phase. | The user has disabled Actions during active development; local gates are authoritative. |

## Phase Order

1. Phase 4.12.0 - Baseline And Failing Guardrails
2. Phase 4.12.1 - Valid Portable Handoff Fixture
3. Phase 4.12.2 - Handoff-First Final Gate Generation
4. Phase 4.12.3 - Deterministic Visual Edit And Real Checkpoint
5. Phase 4.12.4 - Runtime Evidence Binding And Reference QA Materializer
6. Phase 4.12.5 - MVP Gate Evidence Enforcement
7. Phase 4.12.6 - Final Closure Orchestration
8. Phase 4.12.7 - Positive And Negative Test Suite
9. Phase 4.12.8 - Documentation And Agent Guide Updates
10. Phase 4.12.9 - Final Local Closure Evidence

Do not implement Phase 4.12.4 before Phase 4.12.3. Runtime evidence must be tied to a checkpoint generated from actual source state.

## Phase 4.12.0 - Baseline And Failing Guardrails

Goal: prove the current closure gaps with focused tests before changing behavior.

Tasks:

- [x] Add or update focused tests that fail against the current implementation:
  - [x] final closure fixture with only `portable-handoff/README.md` must fail preflight;
  - [x] final closure gate must fail if `build-storefront.ps1` is invoked without `-HandoffRoot`;
  - [x] final closure gate must fail if `generation-plan.json` has static/non-handoff generation mode;
  - [x] final closure gate must fail if `agent-task-package/manifest.json` is manually written by the gate instead of produced by `write-agent-task-package.mjs`;
  - [x] recorder must fail if checkpoint post hash does not match the current generated file;
  - [x] MVP gate must fail if `visual-qa-report.json` references screenshots that do not exist;
  - [x] MVP gate must fail if `visual-qa-report.json` is older than the current runtime QA summary or not tied to the current operation;
  - [x] MVP gate must fail if `visual-qa-runtime-summary.json` proof mode is not `runtime`;
  - [x] MVP gate must fail if runtime summary `baseUrl` does not match the gate `-BaseUrl`;
  - [x] MVP gate must fail if report coverage uses screenshots not present in runtime summary captures.
- [x] Keep these tests narrow. They should inspect scripts/artifacts and not require a live Commerce Node.
- [x] Add a small test fixture mutation helper only if existing test patterns need it.
- [x] Name tests after the behavior being protected, not after implementation details.

Implementation notes:

- Prefer tests under the existing StorefrontBuilder-focused test areas.
- Use local temp directories under `obj` or test temp roots.
- Do not add a new production project or service.
- Do not make tests depend on committed `obj` output.

Checks:

```powershell
dotnet test BlazorShop.Tests.V2\BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~StorefrontBuilder"
dotnet test tools\BlazorShop.AI.StorefrontReverseEngineering\tests\BlazorShop.AI.StorefrontReverseEngineering.Tests\BlazorShop.AI.StorefrontReverseEngineering.Tests.csproj --no-restore --filter "FullyQualifiedName~StorefrontBuilderHandoff"
node --check tools\BlazorShop.AI.StorefrontBuilder\scripts\generate\record-agent-visual-writes.mjs
node --check tools\BlazorShop.AI.StorefrontBuilder\scripts\qa\run-visual-qa.mjs
```

DoD:

- [x] At least one failing test captures each current blocker.
- [x] Tests explain problem/cause/fix in assertion names or failure messages.
- [x] No generated output is committed.

Evidence:

- Added `StorefrontBuilderFinalClosureEvidenceTruthTests` with script/fixture guardrails for marker handoff, missing handoff generation inputs, static/manual proof artifacts, stale checkpoint hashes, runtime summary binding, and materialized Reference QA.
- `dotnet test BlazorShop.Tests.V2\BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~StorefrontBuilder"` fails as expected on the new guardrails before implementation. It also surfaces pre-existing StorefrontBuilder guidance/visual QA assertions to be reconciled later.
- `dotnet test tools\BlazorShop.AI.StorefrontReverseEngineering\tests\BlazorShop.AI.StorefrontReverseEngineering.Tests\BlazorShop.AI.StorefrontReverseEngineering.Tests.csproj --no-restore --filter "FullyQualifiedName~StorefrontBuilderHandoff"` fails on existing Visual QA fixture coverage drift before implementation.
- `node --check tools\BlazorShop.AI.StorefrontBuilder\scripts\generate\record-agent-visual-writes.mjs` passed.
- `node --check tools\BlazorShop.AI.StorefrontBuilder\scripts\qa\run-visual-qa.mjs` passed.

## Phase 4.12.1 - Valid Portable Handoff Fixture

Goal: replace the marker handoff fixture with a minimal valid portable handoff package that StorefrontBuilder can validate and consume.

Tasks:

- [x] Replace `tools/BlazorShop.AI.StorefrontBuilder/tests/generation/fixtures/phase4-11-closure/portable-handoff/README.md` marker-only content with a real portable package shape.
- [x] The fixture root must be accepted as a package root by `Test-HandoffPreflight.ps1`.
- [x] Include `analysis/agent-handoff/manifest.json`.
- [x] Include all required handoff artifacts:
  - [x] `handoff-readiness.json`;
  - [x] `page-compositions.json`;
  - [x] `storefront-pattern.json`;
  - [x] `presentation-catalog.json`;
  - [x] `presentation-mappings.json`;
  - [x] `allowed-files.json`;
  - [x] `protected-files.json`;
  - [x] `design-tokens.json`;
  - [x] `visual-style.json`;
  - [x] `responsive-behavior.json`;
  - [x] `interaction-models.json`;
  - [x] `originality-restrictions.json`;
  - [x] `evidence-manifest.json`;
  - [x] `unresolved-regions.json`.
- [x] Include at least one page composition for home page and the required viewport coverage:
  - [x] desktop;
  - [x] tablet;
  - [x] mobile.
- [x] Include at least one allowed generated visual output that maps to an actual generated file in the pilot project.
- [x] Include protected files that prove the agent cannot edit platform/runtime files.
- [x] Include no blocking unresolved regions.
- [x] Include only handoff-local evidence references, such as `analysis/agent-handoff/screenshots/*` or `analysis/agent-handoff/section-screenshots/*`.
- [x] If image evidence is too heavy for Git, use tiny deterministic placeholder PNGs or text fixtures only where existing schema/tests accept them. Do not use real reference-site copyrighted assets.
- [x] Update `closure-fixture.json` so it no longer says the handoff directory is intentionally minimal.
- [x] Add fixture hash/provenance fields that the final gate can report.

Validation commands:

```powershell
dotnet run --project tools\BlazorShop.AI.StorefrontReverseEngineering\BlazorShop.AI.StorefrontReverseEngineering.csproj -- validate-handoff --handoff-root tools\BlazorShop.AI.StorefrontBuilder\tests\generation\fixtures\phase4-11-closure\portable-handoff --schema-root tools\BlazorShop.AI.StorefrontReverseEngineering\Schemas
dotnet run --project tools\BlazorShop.AI.StorefrontReverseEngineering\BlazorShop.AI.StorefrontReverseEngineering.csproj -- dry-run-handoff --handoff-root tools\BlazorShop.AI.StorefrontBuilder\tests\generation\fixtures\phase4-11-closure\portable-handoff --schema-root tools\BlazorShop.AI.StorefrontReverseEngineering\Schemas
powershell -NoProfile -ExecutionPolicy Bypass -File tools\BlazorShop.AI.StorefrontBuilder\build-storefront.ps1 -Mode preflight-only -HandoffRoot tools\BlazorShop.AI.StorefrontBuilder\tests\generation\fixtures\phase4-11-closure\portable-handoff -HandoffSchemaRoot tools\BlazorShop.AI.StorefrontReverseEngineering\Schemas
```

DoD:

- [x] The fixture is a valid portable handoff package, not a marker.
- [x] StorefrontBuilder preflight passes on the tracked fixture.
- [x] Dry-run handoff passes on the tracked fixture.
- [x] The fixture contains no raw source-only folders such as `captures/`, `analysis/pages/`, `analysis/resolved/`, `presentation-catalog/`, `review/`, or `reports/`.
- [x] The fixture is small and deterministic enough for Git.

Evidence:

- Replaced marker `portable-handoff/README.md` with tracked `portable-handoff/analysis/agent-handoff`.
- Package hash: `8d968f35b91ed44b192dee850d80f9e5e86de9bcf8d59d577d13b41ecdfbadb2`.
- `validate-handoff` passed: readiness true, artifact count 114, schema count 21, no blocking finding.
- `dry-run-handoff` passed: 7 pages, 10 allowed target files, 9 protected files, 90 evidence files, 0 unresolved regions.
- StorefrontBuilder preflight passed; report: `obj/storefront-builder/handoff-preflight/handoff-preflight-BlazorShop.Storefront.GeneratedProof-20260803102001.md`.

## Phase 4.12.2 - Handoff-First Final Gate Generation

Goal: make the final closure gate generate the pilot through the same handoff path that real Phase 4 consumers use.

Tasks:

- [ ] Update `scripts/qa/run-storefront-phase4-final-closure-gate.ps1` defaults:
  - [ ] `PilotHandoffRoot` resolves to the tracked fixture package root, not an output copy created after generation;
  - [ ] add `PilotHandoffSchemaRoot`, defaulting to `tools\BlazorShop.AI.StorefrontReverseEngineering\Schemas`;
  - [ ] keep copied handoff output only as optional retained evidence if useful, not as the source for generation.
- [ ] Before generation, run StorefrontBuilder preflight:
  - [ ] `build-storefront.ps1 -Mode preflight-only -HandoffRoot ... -HandoffSchemaRoot ...`;
  - [ ] record the preflight report path in final gate evidence.
- [ ] Generate pilot with:
  - [ ] `build-storefront.ps1 -Mode generate`;
  - [ ] `-Name $PilotProjectName`;
  - [ ] `-StoreKey $PilotStoreKey`;
  - [ ] `-OutputRoot $resolvedPilotGeneratedOutputRoot`;
  - [ ] `-HandoffRoot $resolvedPilotHandoffRoot`;
  - [ ] `-HandoffSchemaRoot $resolvedPilotHandoffSchemaRoot`;
  - [ ] `-Force`.
- [ ] Remove `Write-PilotAgentTaskPackage` from the final closure gate.
- [ ] Remove the final-gate direct call to non-handoff `plan-generation-files.mjs`.
- [ ] Assert generated metadata:
  - [ ] `metadata.yaml` has `generationMode: handoff-project-skeleton`;
  - [ ] `metadata.yaml` has `handoffGeneration.planPath`;
  - [ ] `metadata.yaml` has `handoffGeneration.sourceHandoffPackageHash`;
  - [ ] `docs/storefront-analysis/generation-plan.json` exists;
  - [ ] `generation-plan.json` has `generationMode: handoff`;
  - [ ] `docs/storefront-analysis/agent-task-package/manifest.json` exists;
  - [ ] `agent-task-package/manifest.json` has `artifactKind: agent-visual-task-package`;
  - [ ] task package `generationPlanHash` matches the actual generation plan SHA-256.
- [ ] Fail if generation plan mode is `static`.
- [ ] Fail if `agent-task-package/manifest.json` contains the old manual `artifactKind: agent-task-package`.
- [ ] Fail if final gate tries to seed `generation-plan.json` or task package into the pilot.

Checks:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\qa\run-storefront-phase4-final-closure-gate.ps1 -Help
rg -n "Write-PilotAgentTaskPackage|plan-generation-files.mjs|HandoffSchemaRoot|handoff-project-skeleton|agent-visual-task-package" scripts\qa\run-storefront-phase4-final-closure-gate.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File tools\BlazorShop.AI.StorefrontBuilder\build-storefront.ps1 -Mode generate -Name BlazorShop.Storefront.Phase412Probe -StoreKey sample -OutputRoot obj\storefront-builder\generated\phase4-12-probe -HandoffRoot tools\BlazorShop.AI.StorefrontBuilder\tests\generation\fixtures\phase4-11-closure\portable-handoff -HandoffSchemaRoot tools\BlazorShop.AI.StorefrontReverseEngineering\Schemas -Force
```

DoD:

- [ ] Final closure pilot is generated from the tracked handoff fixture.
- [ ] No final gate path manually writes generation plan or task package.
- [ ] Generated metadata proves handoff-project skeleton generation.
- [ ] Static generation is rejected for final closure.

## Phase 4.12.3 - Deterministic Visual Edit And Real Checkpoint

Goal: replace placeholder checkpoint proof with actual source mutations and real hashes from the generated pilot.

Tasks:

- [ ] Add a deterministic final-closure helper script if needed, for example:
  - [ ] `tools/BlazorShop.AI.StorefrontBuilder/scripts/generate/apply-final-closure-visual-fixture-edit.mjs`, or;
  - [ ] a tightly scoped PowerShell helper under `scripts/qa`.
- [ ] The helper must read `docs/storefront-analysis/agent-task-package/manifest.json`.
- [ ] Select exactly one allowed generated visual output from `allowedOutputFiles`.
- [ ] Verify selected file exists under the generated project root.
- [ ] Verify selected file is not protected and does not declare a route.
- [ ] Compute normalized SHA-256 before edit.
- [ ] Apply one deterministic, behavior-safe visual-only edit:
  - [ ] prefer CSS class/text/markup decoration inside the selected generated visual file;
  - [ ] do not add `@page`;
  - [ ] do not add HTTP clients, BFF endpoints, DTOs, auth, SEO, cart/checkout/payment/order logic, or appsettings;
  - [ ] preserve existing `data-storefront-*` descriptors if present.
- [ ] Compute normalized SHA-256 after edit.
- [ ] Write `docs/storefront-analysis/visual-checkpoints/{operationId}/visual-checkpoint.json` with:
  - [ ] `operationId` matching `visual-plan.json`;
  - [ ] real `preEditFileHashes`;
  - [ ] real `postEditFileHashes`;
  - [ ] real `changedFiles`;
  - [ ] empty `unexpectedFiles`;
  - [ ] `sourceTreeSnapshotScope` containing the selected file and relevant allowed files;
  - [ ] `diffSummary` explaining the deterministic visual fixture edit.
- [ ] Write or update `docs/storefront-analysis/visual-implementation-report.json` from the same helper:
  - [ ] same `operationId`;
  - [ ] checkpoint path;
  - [ ] changed files;
  - [ ] real before/after SHA-256 values;
  - [ ] unresolved items empty;
  - [ ] boundary/build status initially pending if validation happens later.
- [ ] Update or generate `visual-plan.json` and `visual-implementation-checklist.json` only if current generated output requires alignment with the handoff plan.
- [ ] Remove placeholder hashes from tracked closure fixture artifacts.
- [ ] If fixture artifacts remain tracked, they must be valid templates with explicit `fixtureTemplate` markers and must not be copied as final proof.

Recorder hardening:

- [ ] Update `record-agent-visual-writes.mjs` to verify checkpoint `postEditFileHashes` against actual current file content.
- [ ] Use the same normalized text hash algorithm as checkpoint creation.
- [ ] Fail when:
  - [ ] post hash differs from current file content;
  - [ ] checkpoint claims a changed file that does not exist;
  - [ ] current file changed but checkpoint did not list it;
  - [ ] implementation report changed files differ from checkpoint changed files;
  - [ ] implementation report before/after hashes differ from checkpoint hashes;
  - [ ] placeholder hash strings such as `sha256:phase4-11-*` are present in closure mode.

Checks:

```powershell
node --check tools\BlazorShop.AI.StorefrontBuilder\scripts\generate\record-agent-visual-writes.mjs
node tools\BlazorShop.AI.StorefrontBuilder\scripts\generate\record-agent-visual-writes.mjs --help
rg -n "postEditFileHashes|current file|placeholder|phase4-11-before|phase4-11-after|checksum" tools\BlazorShop.AI.StorefrontBuilder\scripts\generate scripts\qa tools\BlazorShop.AI.StorefrontBuilder\tests
```

DoD:

- [ ] Final closure checkpoint hashes are computed from generated source at runtime.
- [ ] Recorder refuses fake or stale checkpoint hashes.
- [ ] `agent-written-files.json` checksum matches the current generated file content.
- [ ] Placeholder checkpoint hashes cannot pass closure mode.

## Phase 4.12.4 - Runtime Evidence Binding And Reference QA Materializer

Goal: make `visual-qa-report.json` a current-run report derived from runtime screenshots and tracked reference evidence.

Tasks:

- [ ] Keep `run-visual-qa.mjs` as the browser screenshot/runtime evidence collector.
- [ ] Ensure `run-visual-qa.mjs` writes `visual-qa-runtime-summary.json` with:
  - [ ] `artifactKind: storefront-builder.visual-qa-runtime-summary`;
  - [ ] `proofMode: runtime`;
  - [ ] `baseUrl`;
  - [ ] `startedUtc`;
  - [ ] `finishedUtc`;
  - [ ] `operationId` or closure run marker passed from the gate;
  - [ ] capture list with page, viewport, route, and screenshot path;
  - [ ] runtime network audit;
  - [ ] pass/fail status.
- [ ] If `run-visual-qa.mjs` does not currently record `operationId`, add an optional `--operation-id` argument.
- [ ] If it does not record timestamps, add `startedUtc` and `finishedUtc`.
- [ ] Add a materializer script, for example:
  - [ ] `tools/BlazorShop.AI.StorefrontBuilder/scripts/qa/materialize-reference-visual-qa-report.mjs`.
- [ ] The materializer must read:
  - [ ] generated project root;
  - [ ] `visual-plan.json`;
  - [ ] `visual-qa-runtime-summary.json`;
  - [ ] tracked or generated reference evidence paths;
  - [ ] operation ID.
- [ ] The materializer must write `docs/storefront-analysis/visual-qa-report.json`.
- [ ] The materialized report must include:
  - [ ] same operation ID as `visual-plan.json`;
  - [ ] `referenceEvidenceReviewed: true`;
  - [ ] `runtimeEvidencePaths` from runtime summary captures;
  - [ ] `referenceEvidencePaths` from tracked fixture/handoff evidence;
  - [ ] `pageViewportCoverage` matching the visual plan;
  - [ ] `viewportCaptures` mapped from runtime summary captures;
  - [ ] normalized viewport mapping, for example `desktop-1440 -> desktop`, `tablet-768 -> tablet`, `mobile-390 -> mobile`;
  - [ ] zero unaccepted critical issues;
  - [ ] zero unaccepted major issues;
  - [ ] `finalDecision: passed` only when all coverage and evidence checks pass;
  - [ ] `passed: true` only when all checks pass;
  - [ ] evidence paths that are repo-relative or generated-project-relative consistently.
- [ ] The materializer must fail if:
  - [ ] runtime summary is missing;
  - [ ] runtime summary proof mode is not `runtime`;
  - [ ] runtime summary base URL does not match the expected base URL;
  - [ ] runtime summary operation ID does not match visual plan;
  - [ ] any screenshot path is missing on disk;
  - [ ] screenshot files are older than runtime summary `startedUtc`;
  - [ ] any visual-plan page/viewport coverage is missing from runtime captures;
  - [ ] reference evidence paths are missing;
  - [ ] runtime evidence was copied from a previous run;
  - [ ] the materialized report would pass with unaccepted critical or major issues.
- [ ] Update final closure gate so it runs the materializer after runtime visual QA and before the MVP gate's final artifact validation.
- [ ] Do not pre-copy `visual-qa-report.json` from the tracked fixture into the generated project as final proof.

Checks:

```powershell
node --check tools\BlazorShop.AI.StorefrontBuilder\scripts\qa\run-visual-qa.mjs
node --check tools\BlazorShop.AI.StorefrontBuilder\scripts\qa\materialize-reference-visual-qa-report.mjs
rg -n "visual-qa-runtime-summary|operation-id|startedUtc|finishedUtc|materialize-reference|runtimeEvidencePaths|referenceEvidencePaths" tools\BlazorShop.AI.StorefrontBuilder scripts\qa tools\BlazorShop.AI.Visual
```

DoD:

- [ ] `visual-qa-report.json` is produced after runtime browser proof.
- [ ] `visual-qa-report.json` cannot pass with stale, missing, or copied screenshots.
- [ ] Runtime summary and Reference QA report share the same operation ID and base URL.
- [ ] Coverage in the report matches the visual plan.

## Phase 4.12.5 - MVP Gate Evidence Enforcement

Goal: make the MVP gate reject stale or unbound visual QA evidence.

Tasks:

- [ ] Update `scripts/qa/run-storefront-phase4-mvp-gate.ps1` closure mode.
- [ ] Read `docs/storefront-analysis/visual-qa-runtime-summary.json` when `-ProofMode Runtime`.
- [ ] Assert:
  - [ ] runtime summary exists;
  - [ ] runtime summary `artifactKind` is correct;
  - [ ] runtime summary `proofMode` is `runtime`;
  - [ ] runtime summary `baseUrl` matches `-BaseUrl` after normalization;
  - [ ] runtime summary `operationId` matches `visual-plan.json`;
  - [ ] runtime summary captures contain every page/viewport required by `visual-plan.json`;
  - [ ] every summary screenshot path exists on disk;
  - [ ] every report runtime evidence path exists on disk;
  - [ ] every report viewport capture screenshot exists on disk;
  - [ ] every report viewport capture screenshot belongs to runtime summary captures;
  - [ ] every screenshot timestamp is at or after gate start or runtime summary start;
  - [ ] report `runtimeEvidencePaths` are exactly the current summary capture paths or a documented normalized subset;
  - [ ] report `referenceEvidencePaths` exist in the tracked fixture or copied handoff evidence;
  - [ ] report operation ID matches visual plan and runtime summary;
  - [ ] no placeholder hash strings remain in checkpoint or implementation report;
  - [ ] `agent-written-files.json` detection mode is `checkpoint-auto-detect`;
  - [ ] `agent-written-files.json` file checksums match current source file hashes;
  - [ ] generated metadata uses `generationMode: handoff-project-skeleton`;
  - [ ] `generation-plan.json` uses `generationMode: handoff`.
- [ ] Improve failure output:
  - [ ] problem;
  - [ ] likely cause;
  - [ ] exact rerun command;
  - [ ] report path;
  - [ ] evidence path.
- [ ] Keep `-SkeletonProof` as a clearly non-release mode and avoid applying runtime evidence requirements there.

Checks:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\qa\run-storefront-phase4-mvp-gate.ps1 -Help
rg -n "visual-qa-runtime-summary|checkpoint-auto-detect|handoff-project-skeleton|generationMode|BaseUrl|screenshot" scripts\qa\run-storefront-phase4-mvp-gate.ps1
```

DoD:

- [ ] MVP gate cannot pass with a preseeded `visual-qa-report.json`.
- [ ] MVP gate cannot pass when runtime summary or screenshots are stale.
- [ ] MVP gate cannot pass when runtime evidence is not from the current operation.
- [ ] MVP gate still supports skeleton proof for non-release feedback.

## Phase 4.12.6 - Final Closure Orchestration

Goal: update the final closure command so the whole proof chain runs in the correct order without skip-based loopholes.

Tasks:

- [ ] Update `scripts/qa/run-storefront-phase4-final-closure-gate.ps1` order to:
  - [ ] assert clean working tree at start;
  - [ ] capture tested `HEAD`;
  - [ ] validate Visual workspace is docs/schema/skill-only;
  - [ ] validate Visual examples;
  - [ ] validate tracked portable handoff fixture exists;
  - [ ] run StorefrontBuilder handoff preflight;
  - [ ] delete stale pilot output root;
  - [ ] generate fresh pilot with `-HandoffRoot` and `-HandoffSchemaRoot`;
  - [ ] assert generated metadata and generation plan are handoff-based;
  - [ ] assert generated agent task package was produced by StorefrontBuilder;
  - [ ] apply deterministic final-closure visual edit;
  - [ ] write real checkpoint and implementation report;
  - [ ] run automatic changed-file recorder;
  - [ ] restore and build the generated pilot;
  - [ ] start runtime Commerce fixture if needed;
  - [ ] start generated runtime host;
  - [ ] run runtime visual QA with operation ID and base URL;
  - [ ] materialize Reference QA report from runtime summary;
  - [ ] run MVP gate in runtime closure mode;
  - [ ] run `FoundationFunctionalFast` generated proof minimum;
  - [ ] optionally run full fixture commerce proof when `-FunctionalProofLevel FoundationFunctionalFull` or `-RequireCommerceRegression`;
  - [ ] run regeneration ownership gate;
  - [ ] assert `HEAD` unchanged;
  - [ ] assert clean working tree at end;
  - [ ] clean disposable pilot output on success unless `-KeepGeneratedPilot`;
  - [ ] write final JSON and Markdown reports under ignored `obj/storefront-builder/reports`.
- [ ] Remove final-gate seeding of:
  - [ ] `generation-plan.json`;
  - [ ] `agent-task-package`;
  - [ ] final `visual-qa-report.json`.
- [ ] Seed only reference fixture/handoff evidence if the generated project needs a local copy for report materialization.
- [ ] Record final report fields:
  - [ ] tested HEAD;
  - [ ] final HEAD;
  - [ ] closure fixture root;
  - [ ] handoff schema root;
  - [ ] handoff preflight report path;
  - [ ] generated pilot root;
  - [ ] generated metadata path;
  - [ ] generation plan path and hash;
  - [ ] task package path and hash;
  - [ ] checkpoint path and hash;
  - [ ] implementation report path;
  - [ ] agent-written-files path;
  - [ ] runtime summary path;
  - [ ] screenshot root;
  - [ ] materialized QA report path;
  - [ ] MVP gate report path;
  - [ ] functional proof report path;
  - [ ] regeneration gate report path;
  - [ ] final decision.
- [ ] Make local-development bypasses explicit:
  - [ ] `-KeepGeneratedPilot` keeps output only for inspection;
  - [ ] `-SkipFullFixtureProof` does not skip fast functional proof or runtime MVP proof;
  - [ ] no switch may skip handoff preflight, runtime visual QA, materialized QA report, MVP gate, fast functional proof, regeneration gate, start clean tree, or end clean tree.

Checks:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\qa\run-storefront-phase4-final-closure-gate.ps1 -Help
rg -n "Write-PilotAgentTaskPackage|plan-generation-files.mjs|visual-qa-report.json|HandoffSchemaRoot|preflight-only|materialize-reference|FoundationFunctionalFast|run-storefront-builder-regeneration-gate" scripts\qa\run-storefront-phase4-final-closure-gate.ps1
```

DoD:

- [ ] One local final gate command proves the full evidence chain.
- [ ] No final closure step relies on pre-existing `obj` output.
- [ ] No final closure step writes generated artifacts to tracked source.
- [ ] Failure output identifies the broken evidence link.

## Phase 4.12.7 - Positive And Negative Test Suite

Goal: protect the closure patch from regressing back into seeded proof.

Positive tests:

- [ ] valid portable handoff fixture passes StorefrontBuilder preflight.
- [ ] final gate generates a pilot with `metadata.yaml` `generationMode: handoff-project-skeleton`.
- [ ] generated `generation-plan.json` has `generationMode: handoff`.
- [ ] generated `agent-task-package/manifest.json` has `artifactKind: agent-visual-task-package`.
- [ ] deterministic edit creates exactly one allowed changed file.
- [ ] recorder writes `agent-written-files.json` with `detectionMode: checkpoint-auto-detect`.
- [ ] recorder checksum matches current generated file content.
- [ ] runtime visual QA writes `visual-qa-runtime-summary.json`.
- [ ] materializer writes `visual-qa-report.json` from runtime summary.
- [ ] MVP gate passes with valid current runtime evidence.
- [ ] final closure gate passes from clean `HEAD`.

Negative tests:

- [ ] marker-only handoff fixture fails.
- [ ] missing `analysis/agent-handoff/manifest.json` fails.
- [ ] missing required handoff artifact fails.
- [ ] blocking unresolved region fails.
- [ ] raw source-only handoff path fails.
- [ ] final gate without `-HandoffRoot` fails.
- [ ] static generation plan fails final closure.
- [ ] manual task package artifact kind fails final closure.
- [ ] checkpoint placeholder hash fails.
- [ ] checkpoint post hash not equal to current file hash fails.
- [ ] implementation report changed files not equal checkpoint changed files fails.
- [ ] runtime summary missing fails.
- [ ] runtime summary `proofMode: skeleton` fails runtime closure.
- [ ] runtime summary base URL mismatch fails.
- [ ] runtime summary operation ID mismatch fails.
- [ ] `visual-qa-report.json` operation ID mismatch fails.
- [ ] `visual-qa-report.json` screenshot path missing fails.
- [ ] `visual-qa-report.json` screenshot not in runtime summary fails.
- [ ] screenshot older than runtime summary start fails.
- [ ] missing reference evidence fails.
- [ ] unaccepted critical issue fails.
- [ ] unaccepted major issue fails.
- [ ] `passed: true` with nonzero issue counters fails.
- [ ] changed file outside allowed outputs fails.
- [ ] protected file edit fails.
- [ ] final gate leaves dirty tracked files fails.

Suggested test commands:

```powershell
dotnet test BlazorShop.Tests.V2\BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~StorefrontBuilder"
dotnet test tools\BlazorShop.AI.StorefrontReverseEngineering\tests\BlazorShop.AI.StorefrontReverseEngineering.Tests\BlazorShop.AI.StorefrontReverseEngineering.Tests.csproj --no-restore --filter "FullyQualifiedName~StorefrontBuilderHandoffPreflightTests|FullyQualifiedName~StorefrontBuilderHandoffGenerationPlanTests|FullyQualifiedName~StorefrontBuilderHandoffProjectGenerationTests|FullyQualifiedName~StorefrontBuilderAgentTaskPackageTests|FullyQualifiedName~StorefrontBuilderHandoffVisualQaTests|FullyQualifiedName~StorefrontBuilderHandoffRegenerationSafetyTests" --blame-hang --blame-hang-timeout 5m
node --check tools\BlazorShop.AI.StorefrontBuilder\scripts\generate\record-agent-visual-writes.mjs
node --check tools\BlazorShop.AI.StorefrontBuilder\scripts\qa\run-visual-qa.mjs
node --check tools\BlazorShop.AI.StorefrontBuilder\scripts\qa\materialize-reference-visual-qa-report.mjs
```

DoD:

- [ ] Positive path proves the new handoff/runtime chain.
- [ ] Negative tests prove seeded or stale evidence cannot pass.
- [ ] Test names make future regressions obvious.

## Phase 4.12.8 - Documentation And Agent Guide Updates

Goal: make docs match the final closure behavior so future agents do not reintroduce seeded proof.

Tasks:

- [ ] Update `docs/architecture/11-storefront-builder.md`:
  - [ ] final closure consumes tracked portable handoff fixture;
  - [ ] final closure must call `build-storefront.ps1` with `-HandoffRoot`;
  - [ ] final closure must use StorefrontBuilder-generated task package;
  - [ ] final closure must materialize Reference QA from current-run runtime summary;
  - [ ] GitHub Actions are not required while disabled.
- [ ] Update `docs/visual-reverse-engineering-skill/README.md`:
  - [ ] add this plan to historical/current plans;
  - [ ] document final Phase 4.12 closure command;
  - [ ] explain seeded reports are not closure evidence.
- [ ] Update `docs/visual-reverse-engineering-skill/reference.md`:
  - [ ] document valid handoff fixture path;
  - [ ] document runtime summary to Reference QA materialization;
  - [ ] document required evidence paths.
- [ ] Update `docs/visual-reverse-engineering-skill/how-to-generate-and-validate.md`:
  - [ ] show final closure workflow command;
  - [ ] show how to inspect failure reports;
  - [ ] show that `obj` artifacts are disposable.
- [ ] Update `docs/agents/storefront-builder.md`:
  - [ ] forbid final closure from writing task package manually;
  - [ ] forbid final closure from accepting marker-only handoff;
  - [ ] require current-run runtime summary binding.
- [ ] Update `AGENTS.md` only if required reading or closure command wording changes.

Checks:

```powershell
rg -n "Phase 4.12|handoff-project-skeleton|agent-visual-task-package|visual-qa-runtime-summary|materialize|seeded|GitHub Actions" docs\architecture docs\visual-reverse-engineering-skill docs\agents AGENTS.md
```

DoD:

- [ ] Docs describe implemented behavior, not planned behavior.
- [ ] A future agent can run final closure without conversation context.
- [ ] Historical Phase 4.11 remains history and Phase 4.12 is the current closure patch.

## Phase 4.12.9 - Final Local Closure Evidence

Goal: run the final proof and record exactly what passed.

Required final commands:

```powershell
git status --porcelain=v1
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\qa\run-storefront-phase4-final-closure-gate.ps1 -CommandTimeoutSeconds 900
git status --porcelain=v1
```

Focused supporting commands:

```powershell
node tools\BlazorShop.AI.Visual\scripts\validate-visual-examples.mjs
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\qa\run-storefront-phase4-mvp-gate.ps1 -Help
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\qa\run-storefront-phase4-final-closure-gate.ps1 -Help
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\qa\run-storefront-builder-generated-proof.ps1 -ProofLevel FoundationFunctionalFast
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\qa\run-storefront-builder-regeneration-gate.ps1
```

Optional release-level proof when local fixture runtime is acceptable:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\qa\run-storefront-builder-full-proof-with-fixture.ps1
```

DoD:

- [ ] Final closure gate passes from clean `HEAD`.
- [ ] Final closure report records tested `HEAD` and final `HEAD`.
- [ ] Final closure report records handoff preflight report path.
- [ ] Final closure report records generated metadata and generation plan hash.
- [ ] Final closure report records task package hash.
- [ ] Final closure report records real checkpoint path and hash.
- [ ] Final closure report records runtime summary path.
- [ ] Final closure report records screenshot root.
- [ ] Final closure report records materialized Reference QA report path.
- [ ] Final closure report records MVP gate report path.
- [ ] Final closure report records functional proof report path.
- [ ] Final closure report records regeneration proof path.
- [ ] Final closure report says GitHub Actions are not required for this local dev closure.
- [ ] `git status --porcelain=v1` is clean after the gate.

## Release Definition Of Done

- [ ] Tracked closure fixture contains a valid portable handoff package.
- [ ] `validate-handoff` passes on the fixture.
- [ ] `dry-run-handoff` passes on the fixture.
- [ ] StorefrontBuilder preflight passes on the fixture.
- [ ] Final closure generation uses `-HandoffRoot`.
- [ ] Final closure generation uses `-HandoffSchemaRoot`.
- [ ] Generated metadata says `generationMode: handoff-project-skeleton`.
- [ ] Generated plan says `generationMode: handoff`.
- [ ] Agent task package is generated by `write-agent-task-package.mjs`.
- [ ] Final closure gate has no `Write-PilotAgentTaskPackage` helper.
- [ ] Final closure gate does not call non-handoff `plan-generation-files.mjs` after generation.
- [ ] Deterministic visual edit modifies only allowed generated visual source.
- [ ] Checkpoint hashes are real SHA-256 values from current source.
- [ ] Placeholder closure hashes cannot pass.
- [ ] Recorder verifies checkpoint post hash against current file content.
- [ ] Recorder verifies implementation report and checkpoint agree.
- [ ] Runtime visual QA writes current `visual-qa-runtime-summary.json`.
- [ ] Runtime summary contains proof mode, base URL, operation ID, timestamps, and screenshot captures.
- [ ] Reference QA report is materialized after runtime QA.
- [ ] Reference QA report is bound to runtime summary and existing screenshot files.
- [ ] MVP gate rejects stale, missing, or unbound visual QA evidence.
- [ ] MVP gate rejects static generation for final runtime closure.
- [ ] MVP gate rejects non-auto-detected agent write records.
- [ ] Final closure runs `FoundationFunctionalFast` minimum.
- [ ] Final closure runs regeneration ownership proof.
- [ ] Final closure starts and ends on the same clean `HEAD`.
- [ ] GitHub Actions are explicitly not required while disabled.
- [ ] No generated pilot, screenshots, or transient `obj` evidence is committed.

## Risk Register

| Risk | Impact | Mitigation |
| --- | --- | --- |
| Minimal handoff fixture is hard to assemble | Phase stalls on schema details | Start from required preflight artifact list and existing valid handoff tests; keep fixture small and deterministic. |
| Runtime evidence binding becomes too strict across Windows paths | False failures | Normalize paths to `/`, compare resolved full paths when checking disk existence, and store repo-relative report paths. |
| Screenshot timestamp checks are flaky on fast filesystems | False failures | Compare against runtime summary `startedUtc` with a small tolerance or require operation marker if timestamp precision is unreliable. |
| Deterministic edit changes visible behavior too much | Browser QA noise | Edit one allowed visual shell in a minimal, layout-safe way. Avoid route, data, command, and component behavior changes. |
| Materialized Reference QA appears to be visual scoring | Misleading closure claim | Document that this is structured reference evidence review, not pixel-perfect diff. |
| Full fixture proof is slow | Developers avoid closure | Keep `FoundationFunctionalFast` as required; keep full fixture proof optional/release-level. |
| Gate cleanup hides failure evidence | Hard debugging | Clean output only on success; retain generated/report paths on failure. |

## Implementation Commit Plan

Keep commits small and reviewable:

1. `Phase 4.12.0: add closure evidence guardrail tests`.
2. `Phase 4.12.1: replace marker fixture with valid portable handoff`.
3. `Phase 4.12.2: generate final pilot through handoff path`.
4. `Phase 4.12.3: create real checkpoint and verify current hashes`.
5. `Phase 4.12.4: materialize Reference QA from runtime evidence`.
6. `Phase 4.12.5: enforce runtime evidence binding in MVP gate`.
7. `Phase 4.12.6: wire final no-skip closure orchestration`.
8. `Phase 4.12.7: complete positive and negative tests`.
9. `Phase 4.12.8: update architecture and agent docs`.
10. `Phase 4.12.9: run final local closure and record evidence`.

Do not combine the handoff fixture replacement, final gate rewrite, and runtime evidence materializer in one commit. They protect different failure modes and should be reviewable independently.

## Agent Implementation Handoff

When implementing this plan:

- Start with the failing guardrail tests.
- Make the tracked handoff fixture valid before touching final gate generation.
- After generation uses handoff, remove all manual seeding of generation plan and task package from final closure.
- Do not copy `visual-qa-report.json` as final evidence; produce it after runtime QA.
- Do not weaken MVP gate runtime closure checks to make the final gate pass.
- Keep all transient generated output under `obj` or `artifacts` according to existing StorefrontBuilder rules.
- Before closing, run the final checklist in `Release Definition Of Done` line by line.

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

- [x] Update `scripts/qa/run-storefront-phase4-final-closure-gate.ps1` defaults:
  - [x] `PilotHandoffRoot` resolves to the tracked fixture package root, not an output copy created after generation;
  - [x] add `PilotHandoffSchemaRoot`, defaulting to `tools\BlazorShop.AI.StorefrontReverseEngineering\Schemas`;
  - [x] keep copied handoff output only as optional retained evidence if useful, not as the source for generation.
- [x] Before generation, run StorefrontBuilder preflight:
  - [x] `build-storefront.ps1 -Mode preflight-only -HandoffRoot ... -HandoffSchemaRoot ...`;
  - [x] record the preflight report path in final gate evidence.
- [x] Generate pilot with:
  - [x] `build-storefront.ps1 -Mode generate`;
  - [x] `-Name $PilotProjectName`;
  - [x] `-StoreKey $PilotStoreKey`;
  - [x] `-OutputRoot $resolvedPilotGeneratedOutputRoot`;
  - [x] `-HandoffRoot $resolvedPilotHandoffRoot`;
  - [x] `-HandoffSchemaRoot $resolvedPilotHandoffSchemaRoot`;
  - [x] `-Force`.
- [x] Remove `Write-PilotAgentTaskPackage` from the final closure gate.
- [x] Remove the final-gate direct call to non-handoff `plan-generation-files.mjs`.
- [x] Assert generated metadata:
  - [x] `metadata.yaml` has `generationMode: handoff-project-skeleton`;
  - [x] `metadata.yaml` has `handoffGeneration.planPath`;
  - [x] `metadata.yaml` has `handoffGeneration.sourceHandoffPackageHash`;
  - [x] `docs/storefront-analysis/generation-plan.json` exists;
  - [x] `generation-plan.json` has `generationMode: handoff`;
  - [x] `docs/storefront-analysis/agent-task-package/manifest.json` exists;
  - [x] `agent-task-package/manifest.json` has `artifactKind: agent-visual-task-package`;
  - [x] task package `generationPlanHash` matches the actual generation plan SHA-256.
- [x] Fail if generation plan mode is `static`.
- [x] Fail if `agent-task-package/manifest.json` contains the old manual `artifactKind: agent-task-package`.
- [x] Fail if final gate tries to seed `generation-plan.json` or task package into the pilot.

Checks:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\qa\run-storefront-phase4-final-closure-gate.ps1 -Help
rg -n "Write-PilotAgentTaskPackage|plan-generation-files.mjs|HandoffSchemaRoot|handoff-project-skeleton|agent-visual-task-package" scripts\qa\run-storefront-phase4-final-closure-gate.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File tools\BlazorShop.AI.StorefrontBuilder\build-storefront.ps1 -Mode generate -Name BlazorShop.Storefront.Phase412Probe -StoreKey sample -OutputRoot obj\storefront-builder\generated\phase4-12-probe -HandoffRoot tools\BlazorShop.AI.StorefrontBuilder\tests\generation\fixtures\phase4-11-closure\portable-handoff -HandoffSchemaRoot tools\BlazorShop.AI.StorefrontReverseEngineering\Schemas -Force
```

DoD:

- [x] Final closure pilot is generated from the tracked handoff fixture.
- [x] No final gate path manually writes generation plan or task package.
- [x] Generated metadata proves handoff-project skeleton generation.
- [x] Static generation is rejected for final closure.

Evidence:

- `run-storefront-phase4-final-closure-gate.ps1 -Help` passed and now documents `-PilotHandoffSchemaRoot`.
- `rg -n "Write-PilotAgentTaskPackage|plan-generation-files.mjs|HandoffSchemaRoot|handoff-project-skeleton|agent-visual-task-package" scripts\qa\run-storefront-phase4-final-closure-gate.ps1` shows schema/assertion markers and no manual helper/non-handoff plan call.
- `build-storefront.ps1 -Mode generate ... -HandoffRoot ... -HandoffSchemaRoot ... -Force` passed for `BlazorShop.Storefront.Phase412Probe`.
- Probe metadata contained `generationMode: handoff-project-skeleton`, generation plan `generationMode` was `handoff`, and task package `artifactKind` was `agent-visual-task-package`.

## Phase 4.12.3 - Deterministic Visual Edit And Real Checkpoint

Goal: replace placeholder checkpoint proof with actual source mutations and real hashes from the generated pilot.

Tasks:

- [x] Add a deterministic final-closure helper script if needed, for example:
  - [x] `tools/BlazorShop.AI.StorefrontBuilder/scripts/generate/apply-final-closure-visual-fixture-edit.mjs`, or;
  - [x] a tightly scoped PowerShell helper under `scripts/qa` was not needed because the final gate calls the Node helper directly.
- [x] The helper must read `docs/storefront-analysis/agent-task-package/manifest.json`.
- [x] Select exactly one allowed generated visual output from `allowedOutputFiles`.
- [x] Verify selected file exists under the generated project root.
- [x] Verify selected file is not protected and does not declare a route.
- [x] Compute normalized SHA-256 before edit.
- [x] Apply one deterministic, behavior-safe visual-only edit:
  - [x] prefer CSS class/text/markup decoration inside the selected generated visual file;
  - [x] do not add `@page`;
  - [x] do not add HTTP clients, BFF endpoints, DTOs, auth, SEO, cart/checkout/payment/order logic, or appsettings;
  - [x] preserve existing `data-storefront-*` descriptors if present.
- [x] Compute normalized SHA-256 after edit.
- [x] Write `docs/storefront-analysis/visual-checkpoints/{operationId}/visual-checkpoint.json` with:
  - [x] `operationId` matching `visual-plan.json`;
  - [x] real `preEditFileHashes`;
  - [x] real `postEditFileHashes`;
  - [x] real `changedFiles`;
  - [x] empty `unexpectedFiles`;
  - [x] `sourceTreeSnapshotScope` containing the selected file and relevant allowed files;
  - [x] `diffSummary` explaining the deterministic visual fixture edit.
- [x] Write or update `docs/storefront-analysis/visual-implementation-report.json` from the same helper:
  - [x] same `operationId`;
  - [x] checkpoint path;
  - [x] changed files;
  - [x] real before/after SHA-256 values;
  - [x] unresolved items empty;
  - [x] boundary/build status initially pending if validation happens later.
- [x] Update or generate `visual-plan.json` and `visual-implementation-checklist.json` only if current generated output requires alignment with the handoff plan.
- [x] Remove placeholder hashes from tracked closure fixture artifacts.
- [x] If fixture artifacts remain tracked, they must be valid templates with explicit `fixtureTemplate` markers and must not be copied as final proof.

Recorder hardening:

- [x] Update `record-agent-visual-writes.mjs` to verify checkpoint `postEditFileHashes` against actual current file content.
- [x] Use the same normalized text hash algorithm as checkpoint creation.
- [x] Fail when:
  - [x] post hash differs from current file content;
  - [x] checkpoint claims a changed file that does not exist;
  - [x] current file changed but checkpoint did not list it;
  - [x] implementation report changed files differ from checkpoint changed files;
  - [x] implementation report before/after hashes differ from checkpoint hashes;
  - [x] placeholder hash strings such as `sha256:phase4-11-*` are present in closure mode.

Checks:

```powershell
node --check tools\BlazorShop.AI.StorefrontBuilder\scripts\generate\record-agent-visual-writes.mjs
node tools\BlazorShop.AI.StorefrontBuilder\scripts\generate\record-agent-visual-writes.mjs --help
rg -n "postEditFileHashes|current file|placeholder|phase4-11-before|phase4-11-after|checksum" tools\BlazorShop.AI.StorefrontBuilder\scripts\generate scripts\qa tools\BlazorShop.AI.StorefrontBuilder\tests
```

DoD:

- [x] Final closure checkpoint hashes are computed from generated source at runtime.
- [x] Recorder refuses fake or stale checkpoint hashes.
- [x] `agent-written-files.json` checksum matches the current generated file content.
- [x] Placeholder checkpoint hashes cannot pass closure mode.

Evidence:

- Added `apply-final-closure-visual-fixture-edit.mjs`, which reads the StorefrontBuilder-generated task package and handoff generation plan, selects one allowed generated Razor visual output, rejects protected/route targets, applies a deterministic `sfb-phase412-proof` class edit, and writes real visual plan/checklist/checkpoint/implementation-report artifacts from generated source.
- Hardened `record-agent-visual-writes.mjs` closure mode to reject placeholder checkpoint hashes, verify checkpoint post hashes against current file contents, require changed source to appear in checkpoint `changedFiles`, and compare implementation report file hashes against checkpoint hashes.
- Updated the final closure gate to apply the deterministic edit and run automatic changed-file recording after handoff generation instead of copying tracked visual proof artifacts.
- `node --check tools\BlazorShop.AI.StorefrontBuilder\scripts\generate\apply-final-closure-visual-fixture-edit.mjs` passed.
- `node --check tools\BlazorShop.AI.StorefrontBuilder\scripts\generate\record-agent-visual-writes.mjs` passed.
- `node tools\BlazorShop.AI.StorefrontBuilder\scripts\generate\apply-final-closure-visual-fixture-edit.mjs --help` passed.
- `node tools\BlazorShop.AI.StorefrontBuilder\scripts\generate\record-agent-visual-writes.mjs --help` passed.
- Fresh handoff probe generation passed for `BlazorShop.Storefront.Phase412Probe`.
- Probe deterministic edit changed exactly `Components/Catalog/ProductGalleryPlaceholder.razor`.
- Probe recorder passed in `--closure-mode` and recorded one `checkpoint-auto-detect` agent-written file.

## Phase 4.12.4 - Runtime Evidence Binding And Reference QA Materializer

Goal: make `visual-qa-report.json` a current-run report derived from runtime screenshots and tracked reference evidence.

Tasks:

- [x] Keep `run-visual-qa.mjs` as the browser screenshot/runtime evidence collector.
- [x] Ensure `run-visual-qa.mjs` writes `visual-qa-runtime-summary.json` with:
  - [x] `artifactKind: storefront-builder.visual-qa-runtime-summary`;
  - [x] `proofMode: runtime`;
  - [x] `baseUrl`;
  - [x] `startedUtc`;
  - [x] `finishedUtc`;
  - [x] `operationId` or closure run marker passed from the gate;
  - [x] capture list with page, viewport, route, and screenshot path;
  - [x] runtime network audit;
  - [x] pass/fail status.
- [x] If `run-visual-qa.mjs` does not currently record `operationId`, add an optional `--operation-id` argument.
- [x] If it does not record timestamps, add `startedUtc` and `finishedUtc`.
- [x] Add a materializer script, for example:
  - [x] `tools/BlazorShop.AI.StorefrontBuilder/scripts/qa/materialize-reference-visual-qa-report.mjs`.
- [x] The materializer must read:
  - [x] generated project root;
  - [x] `visual-plan.json`;
  - [x] `visual-qa-runtime-summary.json`;
  - [x] tracked or generated reference evidence paths;
  - [x] operation ID.
- [x] The materializer must write `docs/storefront-analysis/visual-qa-report.json`.
- [x] The materialized report must include:
  - [x] same operation ID as `visual-plan.json`;
  - [x] `referenceEvidenceReviewed: true`;
  - [x] `runtimeEvidencePaths` from runtime summary captures;
  - [x] `referenceEvidencePaths` from tracked fixture/handoff evidence;
  - [x] `pageViewportCoverage` matching the visual plan;
  - [x] `viewportCaptures` mapped from runtime summary captures;
  - [x] normalized viewport mapping, for example `desktop-1440 -> desktop`, `tablet-768 -> tablet`, `mobile-390 -> mobile`;
  - [x] zero unaccepted critical issues;
  - [x] zero unaccepted major issues;
  - [x] `finalDecision: passed` only when all coverage and evidence checks pass;
  - [x] `passed: true` only when all checks pass;
  - [x] evidence paths that are repo-relative or generated-project-relative consistently.
- [x] The materializer must fail if:
  - [x] runtime summary is missing;
  - [x] runtime summary proof mode is not `runtime`;
  - [x] runtime summary base URL does not match the expected base URL;
  - [x] runtime summary operation ID does not match visual plan;
  - [x] any screenshot path is missing on disk;
  - [x] screenshot files are older than runtime summary `startedUtc`;
  - [x] any visual-plan page/viewport coverage is missing from runtime captures;
  - [x] reference evidence paths are missing;
  - [x] runtime evidence was copied from a previous run;
  - [x] the materialized report would pass with unaccepted critical or major issues.
- [x] Update final closure gate so it runs the materializer after runtime visual QA and before the MVP gate's final artifact validation.
- [x] Do not pre-copy `visual-qa-report.json` from the tracked fixture into the generated project as final proof.

Checks:

```powershell
node --check tools\BlazorShop.AI.StorefrontBuilder\scripts\qa\run-visual-qa.mjs
node --check tools\BlazorShop.AI.StorefrontBuilder\scripts\qa\materialize-reference-visual-qa-report.mjs
rg -n "visual-qa-runtime-summary|operation-id|startedUtc|finishedUtc|materialize-reference|runtimeEvidencePaths|referenceEvidencePaths" tools\BlazorShop.AI.StorefrontBuilder scripts\qa tools\BlazorShop.AI.Visual
```

DoD:

- [x] `visual-qa-report.json` is produced after runtime browser proof.
- [x] `visual-qa-report.json` cannot pass with stale, missing, or copied screenshots.
- [x] Runtime summary and Reference QA report share the same operation ID and base URL.
- [x] Coverage in the report matches the visual plan.

Evidence:

- `run-visual-qa.mjs` now accepts `--operation-id` and writes `operationId`, `startedUtc`, `finishedUtc`, `screenshotRoot`, canonical `viewport`, `pageId`, route, screenshot path, runtime network audit, counts, and pass/fail into `visual-qa-runtime-summary.json`.
- Added `materialize-reference-visual-qa-report.mjs`, which reads the generated project root, visual plan, runtime summary, reference evidence root, expected operation ID, and expected base URL; it writes schema-shaped `visual-qa-report.json` plus Markdown from current runtime evidence.
- The materializer validates runtime proof mode, operation ID, base URL, timestamp order, screenshot existence and mtime, visual-plan coverage, reference evidence presence, and rejects Critical/Major runtime discrepancies instead of producing a passing report.
- Updated final closure gate to run runtime visual QA with `--operation-id phase4-12-final-closure-pilot`, then run the materializer, and only seed tracked reference artifacts into the pilot.
- Removed final gate dependency on tracked `visual-artifacts/visual-qa-report.json`.
- `node --check tools\BlazorShop.AI.StorefrontBuilder\scripts\qa\run-visual-qa.mjs` passed.
- `node --check tools\BlazorShop.AI.StorefrontBuilder\scripts\qa\materialize-reference-visual-qa-report.mjs` passed.
- `node tools\BlazorShop.AI.StorefrontBuilder\scripts\qa\materialize-reference-visual-qa-report.mjs --help` passed.
- `powershell -NoProfile -ExecutionPolicy Bypass -File scripts\qa\run-storefront-phase4-final-closure-gate.ps1 -Help` passed.
- Probe materializer run passed against current-mtime runtime summary/screenshots under `obj/storefront-builder/generated/phase4-12-probe/...`.
- `rg -n "visual-artifacts[\\/]visual-qa-report\.json|visual-artifacts\\visual-qa-report\.json" scripts\qa\run-storefront-phase4-final-closure-gate.ps1` returned no matches.

## Phase 4.12.5 - MVP Gate Evidence Enforcement

Goal: make the MVP gate reject stale or unbound visual QA evidence.

Tasks:

- [x] Update `scripts/qa/run-storefront-phase4-mvp-gate.ps1` closure mode.
- [x] Read `docs/storefront-analysis/visual-qa-runtime-summary.json` when `-ProofMode Runtime`.
- [x] Assert:
  - [x] runtime summary exists;
  - [x] runtime summary `artifactKind` is correct;
  - [x] runtime summary `proofMode` is `runtime`;
  - [x] runtime summary `baseUrl` matches `-BaseUrl` after normalization;
  - [x] runtime summary `operationId` matches `visual-plan.json`;
  - [x] runtime summary captures contain every page/viewport required by `visual-plan.json`;
  - [x] every summary screenshot path exists on disk;
  - [x] every report runtime evidence path exists on disk;
  - [x] every report viewport capture screenshot exists on disk;
  - [x] every report viewport capture screenshot belongs to runtime summary captures;
  - [x] every screenshot timestamp is at or after gate start or runtime summary start;
  - [x] report `runtimeEvidencePaths` are exactly the current summary capture paths or a documented normalized subset;
  - [x] report `referenceEvidencePaths` exist in the tracked fixture or copied handoff evidence;
  - [x] report operation ID matches visual plan and runtime summary;
  - [x] no placeholder hash strings remain in checkpoint or implementation report;
  - [x] `agent-written-files.json` detection mode is `checkpoint-auto-detect`;
  - [x] `agent-written-files.json` file checksums match current source file hashes;
  - [x] generated metadata uses `generationMode: handoff-project-skeleton`;
  - [x] `generation-plan.json` uses `generationMode: handoff`.
- [x] Improve failure output:
  - [x] problem;
  - [x] likely cause;
  - [x] exact rerun command;
  - [x] report path;
  - [x] evidence path.
- [x] Keep `-SkeletonProof` as a clearly non-release mode and avoid applying runtime evidence requirements there.

Checks:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\qa\run-storefront-phase4-mvp-gate.ps1 -Help
rg -n "visual-qa-runtime-summary|checkpoint-auto-detect|handoff-project-skeleton|generationMode|BaseUrl|screenshot" scripts\qa\run-storefront-phase4-mvp-gate.ps1
```

DoD:

- [x] MVP gate cannot pass with a preseeded `visual-qa-report.json`.
- [x] MVP gate cannot pass when runtime summary or screenshots are stale.
- [x] MVP gate cannot pass when runtime evidence is not from the current operation.
- [x] MVP gate still supports skeleton proof for non-release feedback.

Evidence:

- `powershell -NoProfile -ExecutionPolicy Bypass -File scripts\qa\run-storefront-phase4-mvp-gate.ps1 -Help`
- `node --check tools\BlazorShop.AI.StorefrontBuilder\scripts\qa\run-visual-qa.mjs`
- `node --check tools\BlazorShop.AI.StorefrontBuilder\scripts\generate\record-agent-visual-writes.mjs`
- `powershell -NoProfile -ExecutionPolicy Bypass -File scripts\qa\run-storefront-phase4-mvp-gate.ps1 -GeneratedProjectRoot obj\storefront-builder\generated\phase4-12-probe\BlazorShop.Storefront.Phase412Probe -ProofMode Runtime -BaseUrl http://127.0.0.1:18621 -StartRuntimeHost -HandoffRoot tools\BlazorShop.AI.StorefrontBuilder\tests\generation\fixtures\phase4-11-closure\portable-handoff -SkipRepair -CommandTimeoutSeconds 300`

## Phase 4.12.6 - Final Closure Orchestration

Goal: update the final closure command so the whole proof chain runs in the correct order without skip-based loopholes.

Tasks:

- [x] Update `scripts/qa/run-storefront-phase4-final-closure-gate.ps1` order to:
  - [x] assert clean working tree at start;
  - [x] capture tested `HEAD`;
  - [x] validate Visual workspace is docs/schema/skill-only;
  - [x] validate Visual examples;
  - [x] validate tracked portable handoff fixture exists;
  - [x] run StorefrontBuilder handoff preflight;
  - [x] delete stale pilot output root;
  - [x] generate fresh pilot with `-HandoffRoot` and `-HandoffSchemaRoot`;
  - [x] assert generated metadata and generation plan are handoff-based;
  - [x] assert generated agent task package was produced by StorefrontBuilder;
  - [x] apply deterministic final-closure visual edit;
  - [x] write real checkpoint and implementation report;
  - [x] run automatic changed-file recorder;
  - [x] restore and build the generated pilot;
  - [x] start runtime Commerce fixture if needed;
  - [x] start generated runtime host;
  - [x] run runtime visual QA with operation ID and base URL;
  - [x] materialize Reference QA report from runtime summary;
  - [x] run MVP gate in runtime closure mode;
  - [x] run `FoundationFunctionalFast` generated proof minimum;
  - [x] optionally run full fixture commerce proof when `-FunctionalProofLevel FoundationFunctionalFull` or `-RequireCommerceRegression`;
  - [x] run regeneration ownership gate;
  - [x] assert `HEAD` unchanged;
  - [x] assert clean working tree at end;
  - [x] clean disposable pilot output on success unless `-KeepGeneratedPilot`;
  - [x] write final JSON and Markdown reports under ignored `obj/storefront-builder/reports`.
- [x] Remove final-gate seeding of:
  - [x] `generation-plan.json`;
  - [x] `agent-task-package`;
  - [x] final `visual-qa-report.json`.
- [x] Seed only reference fixture/handoff evidence if the generated project needs a local copy for report materialization.
- [x] Record final report fields:
  - [x] tested HEAD;
  - [x] final HEAD;
  - [x] closure fixture root;
  - [x] handoff schema root;
  - [x] handoff preflight report path;
  - [x] generated pilot root;
  - [x] generated metadata path;
  - [x] generation plan path and hash;
  - [x] task package path and hash;
  - [x] checkpoint path and hash;
  - [x] implementation report path;
  - [x] agent-written-files path;
  - [x] runtime summary path;
  - [x] screenshot root;
  - [x] materialized QA report path;
  - [x] MVP gate report path;
  - [x] functional proof report path;
  - [x] regeneration gate report path;
  - [x] final decision.
- [x] Make local-development bypasses explicit:
  - [x] `-KeepGeneratedPilot` keeps output only for inspection;
  - [x] `-SkipFullFixtureProof` does not skip fast functional proof or runtime MVP proof;
  - [x] no switch may skip handoff preflight, runtime visual QA, materialized QA report, MVP gate, fast functional proof, regeneration gate, start clean tree, or end clean tree.

Checks:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\qa\run-storefront-phase4-final-closure-gate.ps1 -Help
rg -n "Write-PilotAgentTaskPackage|plan-generation-files.mjs|visual-qa-report.json|HandoffSchemaRoot|preflight-only|materialize-reference|FoundationFunctionalFast|run-storefront-builder-regeneration-gate" scripts\qa\run-storefront-phase4-final-closure-gate.ps1
```

DoD:

- [x] One local final gate command proves the full evidence chain.
- [x] No final closure step relies on pre-existing `obj` output.

Evidence:

- `powershell -NoProfile -ExecutionPolicy Bypass -File scripts\qa\run-storefront-phase4-final-closure-gate.ps1 -Help`
- `rg -n "Write-PilotAgentTaskPackage|plan-generation-files\.mjs|visual-qa-report\.json|HandoffSchemaRoot|preflight-only|materialize-reference|FoundationFunctionalFast|run-storefront-builder-regeneration-gate" scripts\qa\run-storefront-phase4-final-closure-gate.ps1`
- `powershell -NoProfile -ExecutionPolicy Bypass -File scripts\qa\run-storefront-phase4-final-closure-gate.ps1 -CommandTimeoutSeconds 900` passed from clean `HEAD`; report `obj/storefront-builder/reports/phase4-final-closure-gate-20260803110012.md`.
- [x] No final closure step writes generated artifacts to tracked source.
- [x] Failure output identifies the broken evidence link.

## Phase 4.12.7 - Positive And Negative Test Suite

Goal: protect the closure patch from regressing back into seeded proof.

Positive tests:

- [x] valid portable handoff fixture passes StorefrontBuilder preflight.
- [x] final gate generates a pilot with `metadata.yaml` `generationMode: handoff-project-skeleton`.
- [x] generated `generation-plan.json` has `generationMode: handoff`.
- [x] generated `agent-task-package/manifest.json` has `artifactKind: agent-visual-task-package`.
- [x] deterministic edit creates exactly one allowed changed file.
- [x] recorder writes `agent-written-files.json` with `detectionMode: checkpoint-auto-detect`.
- [x] recorder checksum matches current generated file content.
- [x] runtime visual QA writes `visual-qa-runtime-summary.json`.
- [x] materializer writes `visual-qa-report.json` from runtime summary.
- [x] MVP gate passes with valid current runtime evidence.
- [x] final closure gate passes from clean `HEAD`.

Negative tests:

- [x] marker-only handoff fixture fails.
- [x] missing `analysis/agent-handoff/manifest.json` fails.
- [x] missing required handoff artifact fails.
- [x] blocking unresolved region fails.
- [x] raw source-only handoff path fails.
- [x] final gate without `-HandoffRoot` fails.
- [x] static generation plan fails final closure.
- [x] manual task package artifact kind fails final closure.
- [x] checkpoint placeholder hash fails.
- [x] checkpoint post hash not equal to current file hash fails.
- [x] implementation report changed files not equal checkpoint changed files fails.
- [x] runtime summary missing fails.
- [x] runtime summary `proofMode: skeleton` fails runtime closure.
- [x] runtime summary base URL mismatch fails.
- [x] runtime summary operation ID mismatch fails.
- [x] `visual-qa-report.json` operation ID mismatch fails.
- [x] `visual-qa-report.json` screenshot path missing fails.
- [x] `visual-qa-report.json` screenshot not in runtime summary fails.
- [x] screenshot older than runtime summary start fails.
- [x] missing reference evidence fails.
- [x] unaccepted critical issue fails.
- [x] unaccepted major issue fails.
- [x] `passed: true` with nonzero issue counters fails.
- [x] changed file outside allowed outputs fails.
- [x] protected file edit fails.
- [x] final gate leaves dirty tracked files fails.

Suggested test commands:

```powershell
dotnet test BlazorShop.Tests.V2\BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~StorefrontBuilder"
dotnet test tools\BlazorShop.AI.StorefrontReverseEngineering\tests\BlazorShop.AI.StorefrontReverseEngineering.Tests\BlazorShop.AI.StorefrontReverseEngineering.Tests.csproj --no-restore --filter "FullyQualifiedName~StorefrontBuilderHandoffPreflightTests|FullyQualifiedName~StorefrontBuilderHandoffGenerationPlanTests|FullyQualifiedName~StorefrontBuilderHandoffProjectGenerationTests|FullyQualifiedName~StorefrontBuilderAgentTaskPackageTests|FullyQualifiedName~StorefrontBuilderHandoffVisualQaTests|FullyQualifiedName~StorefrontBuilderHandoffRegenerationSafetyTests" --blame-hang --blame-hang-timeout 5m
node --check tools\BlazorShop.AI.StorefrontBuilder\scripts\generate\record-agent-visual-writes.mjs
node --check tools\BlazorShop.AI.StorefrontBuilder\scripts\qa\run-visual-qa.mjs
node --check tools\BlazorShop.AI.StorefrontBuilder\scripts\qa\materialize-reference-visual-qa-report.mjs
```

DoD:

- [x] Positive path proves the new handoff/runtime chain.
- [x] Negative tests prove seeded or stale evidence cannot pass.
- [x] Test names make future regressions obvious.

Evidence:

- Added positive/negative contract coverage in `StorefrontPhase4MvpGateVisualQaContractTests` for runtime summary binding, seeded/stale Reference QA rejection, bad QA decisions, placeholder hashes, and final closure no-skip orchestration.
- Hardened `StorefrontBuilderAgentTaskPackageTests` with current-file checksum assertions and a negative checkpoint post-hash mismatch case.
- Updated Visual QA fixture tests so skeleton fixtures cover the current handoff page/slot set, including `sign-in`, layout slots, catalog controls, product gallery/purchase, cart/checkout/account, and state pages.
- Updated architecture contract tests to match the current StorefrontBuilder docs and runtime/skeleton Visual QA fidelity wording.
- `dotnet test BlazorShop.Tests.V2\BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~StorefrontBuilder"` passed: 46/46.
- `dotnet test tools\BlazorShop.AI.StorefrontReverseEngineering\tests\BlazorShop.AI.StorefrontReverseEngineering.Tests\BlazorShop.AI.StorefrontReverseEngineering.Tests.csproj --no-restore --filter "FullyQualifiedName~StorefrontPhase4MvpGateVisualQaContractTests|FullyQualifiedName~StorefrontBuilderAgentTaskPackageTests"` passed: 27/27.
- `dotnet test tools\BlazorShop.AI.StorefrontReverseEngineering\tests\BlazorShop.AI.StorefrontReverseEngineering.Tests\BlazorShop.AI.StorefrontReverseEngineering.Tests.csproj --no-restore --filter "FullyQualifiedName~StorefrontBuilderHandoffPreflightTests|FullyQualifiedName~StorefrontBuilderHandoffGenerationPlanTests|FullyQualifiedName~StorefrontBuilderHandoffProjectGenerationTests|FullyQualifiedName~StorefrontBuilderAgentTaskPackageTests|FullyQualifiedName~StorefrontBuilderHandoffVisualQaTests|FullyQualifiedName~StorefrontBuilderHandoffRegenerationSafetyTests" --blame-hang --blame-hang-timeout 5m` passed: 61/61.
- `node --check tools\BlazorShop.AI.StorefrontBuilder\scripts\generate\record-agent-visual-writes.mjs` passed.
- `node --check tools\BlazorShop.AI.StorefrontBuilder\scripts\qa\run-visual-qa.mjs` passed.
- `node --check tools\BlazorShop.AI.StorefrontBuilder\scripts\qa\materialize-reference-visual-qa-report.mjs` passed.

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

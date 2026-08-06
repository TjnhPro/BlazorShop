# StorefrontBuilder Reference

## Tool Layout

| Path | Purpose |
| --- | --- |
| `tools/BlazorShop.AI.StorefrontBuilder/build-storefront.ps1` | Main orchestration command. |
| `tools/BlazorShop.AI.StorefrontBuilder/validate-storefront.ps1` | Static validation entrypoint for generated storefronts. |
| `tools/BlazorShop.AI.StorefrontBuilder/regenerate-storefront.ps1` | Regenerates generated CSS, pages, components, manifests, or conflict checks. |
| `tools/BlazorShop.AI.StorefrontBuilder/scripts/capture/` | Playwright capture and page discovery helpers. |
| `tools/BlazorShop.AI.StorefrontBuilder/scripts/generate/` | Generation, planning, token extraction, topology, capability, and manifest scripts. |
| `tools/BlazorShop.AI.StorefrontBuilder/scripts/validate/` | Static validation scripts and guardrails. |
| `tools/BlazorShop.AI.StorefrontBuilder/scripts/qa/` | Browser visual QA and commerce regression runners. |
| `tools/BlazorShop.AI.StorefrontReverseEngineering/Skills/reverse-engineering-skills.json` | Phase 3A reverse-engineering skill catalog manifest. It documents deterministic, hybrid, and review-required steps; it is not an executable skill runtime. |
| `tools/BlazorShop.AI.Visual/skills/storefront-visual-plan/SKILL.md` | Canonical Phase 4 visual planning skill. |
| `tools/BlazorShop.AI.Visual/skills/storefront-visual-implement/SKILL.md` | Canonical Phase 4 generated visual implementation skill. |
| `tools/BlazorShop.AI.Visual/skills/storefront-visual-qa/SKILL.md` | Canonical Phase 4 browser evidence and visual QA skill. |
| `tools/BlazorShop.AI.Visual/references/` | Shared architecture, ownership, Razor, CSS, handoff input, checkpoint, and browser QA references for the visual skills. |
| `tools/BlazorShop.AI.Visual/schemas/` | Schema contracts for visual plan, checklist, checkpoint, implementation, QA, and MVP gate reports. |
| `scripts/qa/run-storefront-reverse-engineering-phase3a-gate.ps1` | Phase 3A hardening gate for the ReverseEngineering executable, local fixture browser tests, readiness validation, boundary scan, and StorefrontBuilder compatibility smoke. |
| `scripts/qa/run-storefront-reverse-engineering-phase3b-gate.ps1` | Phase 3B gate for visual analysis, ecommerce mapping, confidence review, Visual Blueprint v1, local multi-page fixture workflows, boundary scans, and StorefrontBuilder plan-only smoke. |
| `scripts/qa/run-storefront-reverse-engineering-phase3c-final-handoff-gate.ps1` | Phase 3C final handoff gate for site-level fixtures, mutation blockers, schema validation, final handoff readiness, and StorefrontBuilder non-consumption boundary scans. |
| `scripts/qa/run-storefront-reverse-engineering-phase3d-final-closure-gate.ps1` | Phase 3D no-skip final closure gate for clean-tree proof, direct Phase 3A/3B/3C proof markers, one restore/build pair, one full ReverseEngineering suite, one grouped closure proof bucket, canonical boundary scans, StorefrontBuilder plan-only smoke, timeout telemetry, cleanup, and final HEAD verification. |
| `scripts/qa/run-storefront-reverse-engineering-phase3e-final-closure-gate.ps1` | Phase 3E no-skip final closure gate. It is non-recursive, runs direct proof steps instead of invoking Phase 3D as a child gate, proves portable validation/copy/dry-run/mutation coverage through the grouped closure proof, runs one canonical boundary scan, runs StorefrontBuilder plan-only smoke once, records timeout/process telemetry, cleans success artifacts, and verifies final HEAD. |
| `scripts/qa/run-storefront-builder-generated-proof.ps1` | Canonical generated proof workflow. |
| `scripts/qa/run-storefront-builder-full-proof-with-fixture.ps1` | Self-contained CI/manual/release wrapper for full fixture proof. |
| `scripts/qa/run-storefront-builder-regeneration-gate.ps1` | CI-friendly regeneration ownership gate. |
| `scripts/qa/run-storefront-builder-isolation-gate.ps1` | Generated storefront build/package/reference isolation gate. |
| `scripts/qa/run-storefront-phase4-mvp-gate.ps1` | Target-specific local Phase 4 MVP gate for one handoff-generated project after visual plan, implementation, recorder, build, and browser evidence. In skeleton mode it is early feedback only; runtime mode is required for closure. |
| `scripts/qa/run-storefront-phase4-final-closure-gate.ps1` | Clean-HEAD Phase 4.12 final closure gate. It runs visual workspace static checks, tracked portable handoff fixture validation, fresh handoff pilot generation, changed-file detection, generated runtime visual proof, Reference visual QA materialization from the current runtime summary, generated functional proof, regeneration ownership proof, and final HEAD/clean-tree verification without GitHub Actions. |

## ReverseEngineering Handoff

`BlazorShop.AI.StorefrontReverseEngineering` writes neutral evidence and draft artifacts under `artifacts/storefront-reverse-engineering/projects/{ProjectId}` or `obj/storefront-reverse-engineering/projects/{ProjectId}`. Phase 3A writes `analysis/visual-blueprint.draft.json`; Phase 3B adds `analysis/visual-blueprint.v1.draft.json`, `analysis/visual-blueprint.v1.reviewed.json`, and `reports/generation-readiness.json` for later handoff review. Phase 3C adds strict Storefront pattern contracts, reviewed page compositions, constrained agent handoff files under `analysis/agent-handoff/`, and final handoff readiness under `analysis/agent-handoff/handoff-readiness.json`. Phase 3D hardens that handoff so reviewed page compositions read resolved artifacts, ecommerce slots come from reviewed mappings or exact contracts, crops use per-viewport bounds, and closure proof uses real positive/negative behavior tests. Phase 3E makes `analysis/agent-handoff/*` portable by adding handoff-local consumer contracts, canonical artifact/schema membership checks, file-level hashes, typed reference containment, manifest/readiness agreement, reviewed slot provenance, portable validator/inspect commands, a read-only dry-run loader, isolated copy proof, negative portability mutations, and the final Phase 3E clean-HEAD gate.

StorefrontBuilder Phase 4 consumes ReverseEngineering artifacts only through portable `analysis/agent-handoff/*` packages and registered schemas. The supported StorefrontBuilder surface is `build-storefront.ps1 -Mode preflight-only|plan-only|generate|full -HandoffRoot <path>`. It must not consume `analysis/visual-blueprint.v1.*.json`, raw source analysis, captures, review folders, or reports as fallback input. Existing non-handoff generation commands, `regenerate-storefront.ps1`, and generated proof gates continue to use current StorefrontBuilder capture, analysis, generation, and validation artifacts.

The tracked Phase 4.12 closure fixture is `tools/BlazorShop.AI.StorefrontBuilder/tests/generation/fixtures/phase4-11-closure/portable-handoff`. It is a real portable handoff package rooted at `analysis/agent-handoff/`, not a marker folder. Final closure must pass that path to StorefrontBuilder through `-HandoffRoot` and `-HandoffSchemaRoot`; it must not hand-write `generation-plan.json`, `agent-task-package/manifest.json`, or generated pilot proof files.

Phase 3B is not a visual generator. It performs design-token extraction, ecommerce region mapping, confidence review, and blueprint assembly, but it does not produce component source, Razor, CSS, generated projects, or blueprint-driven StorefrontBuilder output. Reference assets, logos, copy, and brand-specific visual material are reference-only by default unless later human review and approved workflow clear reuse.

Phase 3B starts from Phase 3A runtime evidence and adds design-token extraction, semantic token normalization, section segmentation, responsive comparison, component detection, ecommerce region mapping, confidence scoring, human review, and reviewed blueprint assembly for later handoff planning. Phase 4 StorefrontBuilder consumption is now limited to the portable handoff package, deterministic generation plan, generated project boundary manifest, constrained visual write recorder, visual proof, repair loop, and handoff-aware regeneration.

Final Phase 3A capture flow:

1. Open one browser session per viewport.
2. Navigate and stabilize the page.
3. Extract rendered DOM, computed styles, bounding boxes, and asset evidence.
4. Attempt native full-page screenshot.
5. Evaluate native quality and persist fallback decision details.
6. Reuse the same session for stitched fallback when native output is missing, invalid, blank, dimension-mismatched, or otherwise blocking.
7. Persist one correlated snapshot across raw capture, quality report, viewport manifest, element evidence, asset evidence, and page capture manifest.

`CapturePolicy` defaults are timeout `30000ms`, maximum page height `12000`, maximum pages `1`, preserve viewport segments `false`, strict warnings `false`, automatic stitched fallback `true`, maximum single-color ratio `0.98`, evidence element/asset limits `80`, maximum text length `160`, maximum segment count `50`, segment overlap `80px`, scroll settle `100ms`, final settle `150ms`, and noise selectors `.cookie-banner` plus `[data-capture-noise]`.

## ReverseEngineering Commands

```powershell
dotnet run --project tools\BlazorShop.AI.StorefrontReverseEngineering\BlazorShop.AI.StorefrontReverseEngineering.csproj -- --help
dotnet run --project tools\BlazorShop.AI.StorefrontReverseEngineering\BlazorShop.AI.StorefrontReverseEngineering.csproj -- init --url https://reference.example --name Demo --output-root artifacts/storefront-reverse-engineering/projects
dotnet run --project tools\BlazorShop.AI.StorefrontReverseEngineering\BlazorShop.AI.StorefrontReverseEngineering.csproj -- run --url https://reference.example --name Demo --output-root artifacts/storefront-reverse-engineering/projects --no-ai
dotnet run --project tools\BlazorShop.AI.StorefrontReverseEngineering\BlazorShop.AI.StorefrontReverseEngineering.csproj -- resume --project artifacts/storefront-reverse-engineering/projects/demo --force-step capture
dotnet run --project tools\BlazorShop.AI.StorefrontReverseEngineering\BlazorShop.AI.StorefrontReverseEngineering.csproj -- inspect --project artifacts/storefront-reverse-engineering/projects/demo
dotnet run --project tools\BlazorShop.AI.StorefrontReverseEngineering\BlazorShop.AI.StorefrontReverseEngineering.csproj -- validate --project artifacts/storefront-reverse-engineering/projects/demo
dotnet run --project tools\BlazorShop.AI.StorefrontReverseEngineering\BlazorShop.AI.StorefrontReverseEngineering.csproj -- resolve-safe-review --project artifacts/storefront-reverse-engineering/projects/demo
dotnet run --project tools\BlazorShop.AI.StorefrontReverseEngineering\BlazorShop.AI.StorefrontReverseEngineering.csproj -- validate-handoff --handoff-root artifacts/storefront-reverse-engineering/projects/demo --schema-root tools/BlazorShop.AI.StorefrontReverseEngineering/Schemas
dotnet run --project tools\BlazorShop.AI.StorefrontReverseEngineering\BlazorShop.AI.StorefrontReverseEngineering.csproj -- inspect-handoff --handoff-root artifacts/storefront-reverse-engineering/projects/demo --schema-root tools/BlazorShop.AI.StorefrontReverseEngineering/Schemas
dotnet run --project tools\BlazorShop.AI.StorefrontReverseEngineering\BlazorShop.AI.StorefrontReverseEngineering.csproj -- dry-run-handoff --handoff-root artifacts/storefront-reverse-engineering/projects/demo --schema-root tools/BlazorShop.AI.StorefrontReverseEngineering/Schemas
```

Manual artifacts should use `artifacts/storefront-reverse-engineering/projects/{ProjectId}`. Automated tests and gates should use `obj/storefront-reverse-engineering/projects/{ProjectId}`.

Readiness is reported in `reports/readiness-report.json`; that JSON file is the source of truth used by `inspect` and gate checks. `reports/readiness-report.md` is only the human-readable companion. A passing readiness report means the current artifacts are schema-valid, quality-aware, linked by capture correlation IDs, tied to workflow run state, and constrained by originality/provenance. It does not mean AI analysis is complete or that a generated storefront can be produced.

Phase 3B step reruns:

```powershell
dotnet run --project tools/BlazorShop.AI.StorefrontReverseEngineering/BlazorShop.AI.StorefrontReverseEngineering.csproj -- inspect --project obj/storefront-reverse-engineering/projects/fixturedemo
dotnet run --project tools/BlazorShop.AI.StorefrontReverseEngineering/BlazorShop.AI.StorefrontReverseEngineering.csproj -- resume --project obj/storefront-reverse-engineering/projects/fixturedemo --force-step aggregate-evidence
dotnet run --project tools/BlazorShop.AI.StorefrontReverseEngineering/BlazorShop.AI.StorefrontReverseEngineering.csproj -- resume --project obj/storefront-reverse-engineering/projects/fixturedemo --force-step assemble-blueprint-v1
```

Phase 3C final handoff commands:

```powershell
dotnet run --project tools/BlazorShop.AI.StorefrontReverseEngineering/BlazorShop.AI.StorefrontReverseEngineering.csproj -- inspect --project obj/storefront-reverse-engineering/projects/fixturedemo
dotnet run --project tools/BlazorShop.AI.StorefrontReverseEngineering/BlazorShop.AI.StorefrontReverseEngineering.csproj -- resolve-safe-review --project obj/storefront-reverse-engineering/projects/fixturedemo
dotnet run --project tools/BlazorShop.AI.StorefrontReverseEngineering/BlazorShop.AI.StorefrontReverseEngineering.csproj -- resume --project obj/storefront-reverse-engineering/projects/fixturedemo --force-step apply-review-decisions
dotnet run --project tools/BlazorShop.AI.StorefrontReverseEngineering/BlazorShop.AI.StorefrontReverseEngineering.csproj -- resume --project obj/storefront-reverse-engineering/projects/fixturedemo --force-step validate-agent-handoff-readiness
powershell -ExecutionPolicy Bypass -File scripts\qa\run-storefront-reverse-engineering-phase3c-final-handoff-gate.ps1
```

Strict real-site Phase 3B/3C proof:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\reverse-engineering\run-storefront-reverse-engineering-production.ps1 -Url "https://www.kindredcoast.com/" -Name "KindredCoast" -Force -ResolveSafeReviewItems -FailOnBlockers -CommandTimeoutSeconds 900
```

Use this command for closure evidence, not `-Resume`, when proving that current code can regenerate a production handoff from the reference site. The production runner builds the tool, runs the workflow, optionally materializes safe review decisions, reruns from reviewed blueprint assembly, inspects, validates, validates the portable package from the project root, and dry-run loads the handoff as a future consumer. `-FailOnBlockers` returns non-zero for any remaining readiness, portable validation, or dry-run handoff blocker.

Phase 3D final closure command:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\qa\run-storefront-reverse-engineering-phase3d-final-closure-gate.ps1
```

The Phase 3D gate has no skip flags and must be run from a clean working tree. It remains the Phase 3D correctness proof where the tested SHA equals final `HEAD`; see `docs/qa/phase3d-final-closure.md` for the current Phase 3D proof. Phase 3 final closure after Phase 3E requires the Phase 3E gate below.

Phase 3E final closure command:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\qa\run-storefront-reverse-engineering-phase3e-final-closure-gate.ps1
```

Phase 3E remains in progress until the final Phase 3E runtime gate passes on this same clean HEAD. The ignored gate report is authoritative final proof; tracked docs must not require a post-gate source commit.

The Phase 3E gate restores and builds once, then later `dotnet test` invocations run with `--no-build --no-restore`. CLI/browser/portable proof coverage is represented by the grouped closure proof test process, including the shared positive baseline, shared portable baseline, multi-route CLI proof collection, and Playwright browser reuse with isolated contexts/pages. The portable proof checks copied-package canonical artifacts, canonical schema requirements, package hashes, typed reference categories, and `manifest.json` readiness agreement with `handoff-readiness.json`. The report includes global timeout budget, remaining budget, process/test-process counts, step start/end/duration/exit code, slowest steps, cleanup result, and local-proof/GitHub Actions status. GitHub Actions remains intentionally excluded from the development closure path while workflows are disabled.

Review decisions are edited in `review/review-decisions.json`. Apply approved, modified, rejected, or deferred decisions by rerunning `apply-review-decisions` or any downstream step. Decisions must include reviewer metadata, source artifact ID, source artifact hash, and a stable decision ID; stale or duplicate decisions fail before reviewed artifacts are emitted.

`resolve-safe-review --project <project>` is the only non-interactive review materialization path. It may approve deterministic safe visual-only items when the source artifact ID and hash still match the current review queue, and it writes `review/review-decision-summary.json` with approved, modified, blocked, skipped, and stale counts. It must not approve direct Storefront API calls, protected-path changes, runtime-owned behavior, stale hashes, unsupported critical patterns, or unknown unsafe provenance. If the summary reports blocked or stale items, inspect those items and write explicit manual decisions before rerunning `assemble-blueprint-v1` or `validate-agent-handoff-readiness`.

`inspect` reads `project.json`, `runs/{runId}.json`, `reports/readiness-report.json`, Phase 3B analysis JSON, review queue JSON, `reports/generation-readiness.json`, and Phase 3C handoff readiness without launching a browser. Its output includes latest run status, readiness pass/fail/unknown, blocking and warning counts, the latest blocking finding, blueprint path, readiness report path, Phase 3B artifact status, review queue count, generation readiness, latest Phase 3B blocker, final handoff readiness, final handoff blocker/warning counts, agent handoff path, and step status rows when a valid run file exists.

Common Phase 3B failures are reported as problem/cause/fix lines:

| Problem | Typical fix |
| --- | --- |
| Missing Phase 3A readiness | Run `validate` or a successful no-AI workflow before Phase 3B steps. |
| Missing evidence snapshot | Rerun `--force-step aggregate-evidence`. |
| Invalid token schema | Rerun `--force-step extract-raw-tokens` or `--force-step normalize-semantic-tokens`. |
| Presentation catalog drift | Update catalog extraction against current Presentation/Starter contracts and rerun `--force-step build-presentation-catalog`. |
| Unresolved blocking review item | Run `resolve-safe-review --project <project>` for deterministic safe visual-only items, or write `review/review-decisions.json` manually for unsafe/manual items, then rerun confidence review and blueprint assembly. |
| Unsupported critical pattern | Resolve the unsupported mapping before the reviewed handoff can be approved as future generation input. |
| Failed final handoff readiness | Inspect `analysis/agent-handoff/handoff-readiness.json`, resolve blocking codes, and rerun `validate-agent-handoff-readiness`. |

## Phase 3C Artifact Interpretation

| Artifact | Role |
| --- | --- |
| `analysis/agent-handoff/task.md` | Human-readable implementation brief for a later Phase 4 agent. |
| `analysis/agent-handoff/manifest.json` | Machine-readable package index and consumer contract. |
| `analysis/agent-handoff/allowed-files.json` | Machine-readable allowlist for future generated visual files. |
| `analysis/agent-handoff/protected-files.json` | Machine-readable protected path and behavior boundary manifest. |
| `analysis/agent-handoff/page-compositions.json` | Source-of-truth page/section composition input for future generation. |
| `analysis/agent-handoff/storefront-pattern.json` | Source-of-truth Storefront Presentation/Starter pattern contract. |
| `analysis/agent-handoff/visual-blueprint.json` | Reviewed evidence index for handoff traceability. |
| `analysis/agent-handoff/presentation-catalog.json` | Handoff-local Presentation component catalog used for exact slot and target-path validation. |
| `analysis/agent-handoff/presentation-mappings.json` | Handoff-local reviewed mapping contract used for authoritative slot provenance. |
| `analysis/agent-handoff/component-candidates.json` and `component-instances.json` | Handoff-local component analysis contracts for future generated visual planning. |
| `analysis/agent-handoff/responsive-behavior.json` and `interaction-models.json` | Evidence-derived responsive and interaction behavior summaries. |
| `analysis/agent-handoff/design-tokens.json` and `visual-style.json` | Reviewed visual token/style evidence for future implementation. |
| `analysis/agent-handoff/evidence-manifest.json` | Packaged screenshot/crop index with hashes, viewport data, bounds, interaction state, and reviewed slot provenance. |
| `analysis/agent-handoff/screenshots/` and `section-screenshots/` | Portable visual evidence copied into the package; future consumers must not read raw `captures/*` as fallback input. |
| `analysis/agent-handoff/unresolved-regions.json` | Machine-readable blocker/warning summary. |
| `analysis/agent-handoff/handoff-readiness.json` | Final machine-readable readiness gate; Phase 4 must fail when this is not passed. |
| Raw `captures/*`, `analysis/pages/*`, and screenshots/crops | Evidence-only inputs; Phase 4 must not reinterpret them unless explicitly running a new ReverseEngineering pass. |

## Phase 4 Consumption Contract

Phase 4 may read only `analysis/agent-handoff/*` and schemas as input. It must not reinterpret raw reference evidence unless explicitly running a new ReverseEngineering pass. It must not write into `BlazorShop.Storefront.Starter`. It must not change protected Storefront runtime behavior. It must fail if `analysis/agent-handoff/handoff-readiness.json` is missing, not passed, or disagrees with `manifest.json` readiness. The portable preflight surface is `build-storefront.ps1 -Mode preflight-only -HandoffRoot <path>`, `validate-handoff`, `inspect-handoff`, and the read-only `HandoffConsumerDryRunLoader`; none of these may read the original source project as a fallback. A reviewed mapping is authoritative for slot proof only when its source page and source section belong to the active reviewed page composition; orphan mappings fail with `reviewed-slot-mapping-orphan`.

`tools/BlazorShop.AI.Visual` is the Phase 4 skill/report workspace. It is documentation, schemas, examples, and optional host adapter instructions only. It has no `.csproj`, no runtime references, and no authority to generate or mutate projects by itself. StorefrontBuilder still owns project creation, regeneration, recorder validation, browser QA scripts, and generated artifact layout. ReverseEngineering still owns reference evidence and portable handoff packages.

Visual skills are used in this order:

1. `storefront-visual-plan`: read the StorefrontBuilder generation plan and `agent-task-package/manifest.json`, hash inputs, map slots to allowed files, and emit `docs/storefront-analysis/visual-plan.json` plus `visual-implementation-checklist.todo.md`.
2. `storefront-visual-implement`: edit only allowed generated visual files, preserve Presentation descriptors, emit checkpoints, run the recorder, and write `visual-implementation-report.json` plus `.md`.
3. `storefront-visual-qa`: run browser evidence through `run-visual-qa.mjs`, inspect screenshots, run bounded generated-owned repair when needed, and write `visual-qa-report.json` plus `.md`.

Handoff generation commands:

```powershell
.\tools\BlazorShop.AI.StorefrontBuilder\build-storefront.ps1 -Mode preflight-only -HandoffRoot <portable-handoff-root> -HandoffSchemaRoot tools\BlazorShop.AI.StorefrontReverseEngineering\Schemas
.\tools\BlazorShop.AI.StorefrontBuilder\build-storefront.ps1 -Mode plan-only -Name Demo -StoreKey sample -HandoffRoot <portable-handoff-root> -HandoffSchemaRoot tools\BlazorShop.AI.StorefrontReverseEngineering\Schemas
.\tools\BlazorShop.AI.StorefrontBuilder\build-storefront.ps1 -Mode generate -Name Demo -StoreKey sample -OutputRoot obj/storefront-builder/generated -HandoffRoot <portable-handoff-root> -HandoffSchemaRoot tools\BlazorShop.AI.StorefrontReverseEngineering\Schemas -Force
```

Handoff-generated project artifacts under `docs/storefront-analysis/`:

| Artifact | Role |
| --- | --- |
| `generation-plan.json` and `generation-plan.yaml` | Deterministic compiled plan from reviewed handoff artifacts. |
| `handoff-generation-summary.md` | Human-readable plan/skeleton summary. |
| `handoff-placeholders.json` | Placeholder skeleton write manifest. |
| `agent-task-package/` | Handoff-local task package for constrained visual generation. |
| `agent-written-files.json` | Recorded generated visual files after constrained agent writes. |
| `repair-history.md` | Durable bounded repair attempt history. |
| `visual-plan.json` | Schema-backed visual planning output from `storefront-visual-plan`. |
| `visual-implementation-checklist.todo.md` | Reviewable visual implementation checklist created before edits. |
| `visual-checkpoints/{operationId}/visual-checkpoint.json` | File-hash checkpoint for a visual implementation operation. |
| `visual-implementation-report.json` and `.md` | Changed-file, recorder, build, boundary, and unresolved-item report from `storefront-visual-implement`. |
| `visual-qa-runtime-summary.json` | Current-run runtime browser evidence from `run-visual-qa.mjs`, including proof mode, operation ID, base URL, timestamps, captures, route status, and network audit. |
| `visual-qa-report.json` and `.md` | Browser capture, issue, repair-attempt, and pass/fail report from `storefront-visual-qa` or Phase 4.12 materialization. Final closure must materialize JSON from the current runtime summary instead of copying a seeded report. |
| `phase4-mvp-gate-report.json` and `.md` | Target-specific MVP gate evidence for the generated project. |

Mandatory Phase 4 closure visual artifacts:

- `generation-plan.json` and `generation-plan.yaml`.
- `agent-task-package/manifest.json`.
- `visual-plan.json`.
- `visual-implementation-checklist.todo.md`.
- `visual-checkpoints/{operationId}/visual-checkpoint.json`.
- `agent-written-files.json` produced by `record-agent-visual-writes.mjs`; in closure mode this must come from automatic checkpoint comparison or explicit recorded generated visual paths, not from a hand-written placeholder.
- `visual-implementation-report.json` and `.md`.
- `visual-qa-runtime-summary.json` produced by the current runtime visual QA run.
- `visual-qa-report.json` and `.md`.
- Reference visual QA evidence, including reviewed reference evidence paths, runtime evidence paths bound to the current runtime summary, severity counters, accepted differences, and the final pass/fail decision.

## ReverseEngineering Browser Setup

Install .NET Playwright Chromium once before running browser tests or the hardening gate:

```powershell
dotnet build tools\BlazorShop.AI.StorefrontReverseEngineering\BlazorShop.AI.StorefrontReverseEngineering.csproj
.\tools\BlazorShop.AI.StorefrontReverseEngineering\bin\Debug\net10.0\playwright.ps1 install chromium
```

The browser integration tests use .NET Playwright against a local HTTP fixture server, not an external website:

```powershell
dotnet test tools\BlazorShop.AI.StorefrontReverseEngineering\tests\BlazorShop.AI.StorefrontReverseEngineering.Tests\BlazorShop.AI.StorefrontReverseEngineering.Tests.csproj --filter "Playwright|EndToEnd"
```

ReverseEngineering runtime adapter selection is fixed: `file://` URLs use fixture capture, `.test` hosts use synthetic deterministic capture, and other HTTP/HTTPS URLs use the .NET Playwright adapter. StorefrontBuilder Node Playwright scripts remain StorefrontBuilder capture and QA baselines only; they are not recommended as ReverseEngineering Phase 3A runtime capture.

Run the full Phase 3A hardening gate with:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\qa\run-storefront-reverse-engineering-phase3a-gate.ps1
```

The gate writes a commit-linked local report under `obj/storefront-reverse-engineering/reports` with status, commit SHA, branch, UTC timestamp, .NET version, Playwright state, OS, executed commands, passed steps, artifact root, workflow run ID, readiness report path, and test summaries. While GitHub Actions are disabled during development, the gate report plus `docs/qa/phase3a-final-fix-closure.md` are the Phase 3A closure evidence.

Run the Phase 3B gate with:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\qa\run-storefront-reverse-engineering-phase3b-gate.ps1
```

The Phase 3B gate writes a commit-linked report under `obj/storefront-reverse-engineering/reports` with commit SHA, branch, UTC timestamp, .NET version, fixture routes, test summaries, blueprint paths, Presentation catalog version, generation readiness result, unsupported pattern count, review queue count, known limitations, and blocking artifact/fix details when readiness remains intentionally review-blocked.

## Generated Project Shape

Generated storefront projects use this naming pattern:

```text
artifacts/storefront-builder/generated/BlazorShop.Storefront.{Name}
obj/storefront-builder/generated/BlazorShop.Storefront.{Name}
```

Required generated project files include:

- `{ProjectName}.csproj`
- `StorefrontPackageVersions.props`
- `starter-generation.contract.yaml`
- `docs/storefront-analysis/metadata.yaml`
- `docs/storefront-analysis/asset-manifest.yaml`
- `docs/storefront-analysis/generated-files.yaml`

Generated proof projects are ignored artifacts, not committed source projects. The canonical proof name for local validation is `BlazorShop.Storefront.GeneratedProof`.

Generated/custom storefront compatibility rules:

- Use Runtime-backed Presentation contexts and BFF contracts instead of direct generated-client references in generated visual source.
- Treat `contracts/storefront/storefront.openapi.json` as the canonical Storefront API contract behind the Runtime-owned `BlazorShop.Storefront.Client` package; run `scripts/qa/run-storefront-client-regeneration-gate.ps1` before package proof when the contract or generated client changes.
- Use `BlazorShop.Storefront.Presentation` package contracts for shared App/Routes/page services/BFF/SEO/media composition.
- Register generated visual components as Presentation view slots; generated source must not declare `@page` routes or add route assemblies.
- Use Storefront Presentation for server-side storefront application registration. Presentation composes Runtime internally for generated-client registration, store context, capability/error primitives, and BFF integration primitives.
- Use `BlazorShop.Storefront.Components` contracts/headless behavior and Browser local API primitives only when reusable browser-safe UI components are needed; local presentation components can stay inside the generated storefront.
- Treat `BlazorShop.Storefront.Components.Features` as retired. Normal generation consumes `Contracts`, `Headless`, and `Browser` primitives and emits project-local visual templates.
- `BlazorShop.Storefront.{Name}` owns generated markup, generated CSS, store-specific assets, pages, and analysis artifacts.
- StorefrontBuilder may replace product card/grid/gallery/purchase/cart/checkout/account visual templates without changing shared behavior contracts.
- Route protected browser actions through same-origin BFF endpoints.
- Do not generate route/BFF/SEO/media application logic from scratch when Presentation already owns it.
- Never reference `BlazorShop.Storefront.V2`, backend/API/core projects, Control Plane Web, or `BlazorShop.Web.SharedV2`/`Web.SharedV2`.
- Do not use Storefront V2 visual markup as the generated/custom storefront presentation source.
- Do not copy Components `Features` wrappers as generated visual templates or stable presentation contracts.
- Do not guess API response shapes; use generated package contracts through Runtime, Presentation BFF contracts, or explicitly documented host-local extensions.

## Main Command

```powershell
.\tools\BlazorShop.AI.StorefrontBuilder\build-storefront.ps1 `
  -Url https://reference.example `
  -Name Demo `
  -StoreKey sample `
  -Mode validate-only
```

Parameters:

| Parameter | Default | Notes |
| --- | --- | --- |
| `Url` | `https://reference.example` | Reference storefront URL used for analysis artifacts. |
| `Name` | `Demo` | Normalized to `BlazorShop.Storefront.{Name}` unless the full project name is already supplied. |
| `StoreKey` | `sample` | Storefront API route scope for generated configuration. |
| `OutputRoot` | `artifacts/storefront-builder/generated` | Generated artifact root. |
| `Mode` | `validate-only` | One of `analyze-only`, `plan-only`, `generate`, `update`, `validate-only`, `full`. |
| `HandoffRoot` | empty | Portable handoff package root or its `analysis/agent-handoff` folder for Phase 4 preflight/planning/generation. |
| `HandoffSchemaRoot` | `tools/BlazorShop.AI.StorefrontReverseEngineering/Schemas` | Registered schema root for handoff validation. |
| `Force` | off | Allows project generation to overwrite an existing generated target when the generation script permits it. |
| `SkipVisualQa` | off | Suppresses visual QA runner reporting in `full` mode. |
| `SkipCommerceRegression` | off | Suppresses commerce regression runner reporting in `full` mode. |

Modes:

| Mode | Result |
| --- | --- |
| `analyze-only` | Runs `write-review-artifacts.mjs`. |
| `preflight-only` | Validates a portable handoff package without generating a project. |
| `plan-only` | Runs `plan-generation-files.mjs --dry-run`; with `-HandoffRoot`, compiles a handoff generation plan. |
| `generate` | Creates a new storefront project and writes review artifacts; with `-HandoffRoot`, creates a Starter-based handoff skeleton and task package. |
| `update` | Runs regeneration with `Scope all`. |
| `validate-only` | Runs `validate-storefront.ps1`. |
| `full` | Generates, writes artifacts, validates, and prints browser QA runner names; with `-HandoffRoot`, runs the handoff project path. |

## Regeneration Command

```powershell
.\tools\BlazorShop.AI.StorefrontBuilder\regenerate-storefront.ps1 `
  -ProjectRoot artifacts/storefront-builder/generated/BlazorShop.Storefront.GeneratedProof `
  -Scope all
```

Scopes:

| Scope | Behavior |
| --- | --- |
| `all` | Generates a candidate, plans all generated/managed visual file actions, applies safe changes, updates generated manifest, and checks idempotency. |
| `page` | Plans and applies page/composition output for the optional `Target`. |
| `component` | Plans and applies component/composition output for the optional `Target`. |
| `css` | Plans and applies generated visual foundation CSS. |
| `foundation` | Explicitly refreshes generated platform metadata, package compatibility metadata, and the copied Starter contract. |
| `validate` | Runs the static storefront validation gate. |
| `conflicts` | Runs idempotency/conflict validation. |

Use `-WhatIf` to run the same candidate planning pipeline as apply mode without copying changed files into the generated target. The console prints a stable `WhatIf report:` path, summary counts, meaningful `filePath: action - reason` lines, and conflict next-action guidance when needed. By default the report is written outside the target under `{OutputRoot}/.regeneration-reports/{ProjectName}-{operationId}.md`; `-WhatIfReportPath <path>` can redirect it to an approved report path under the output report folder, repo `obj`, or `artifacts/storefront-builder`. The report records create, update, skip unchanged, skip user-owned, skip protected, manual-edit conflict, platform metadata update, and obsolete candidate actions.

For non-handoff projects, regeneration candidates come from the current Starter/template inputs. For handoff-generated projects, candidates preserve stored handoff metadata, copy the target project, reapply stored `docs/storefront-analysis/generation-plan.json`, and then compare the candidate against the target. Handoff package/readiness hash drift fails with an explicit re-plan/update requirement. Starter contract drift fails with an explicit foundation upgrade requirement. Protected target paths in a handoff generation plan fail before candidate writes.

StorefrontBuilder generator provenance comes from `tools/BlazorShop.AI.StorefrontBuilder/version.json`. Generated `metadata.yaml` and `generated-files.yaml` entries must agree on the same `generatorVersion`.

Use `-ValidateAfterApply` and `-BuildAfterApply` when a regeneration must prove the generated project still validates and builds before the change is accepted.

Refresh platform metadata intentionally:

```powershell
.\tools\BlazorShop.AI.StorefrontBuilder\regenerate-storefront.ps1 `
  -ProjectRoot artifacts/storefront-builder/generated/BlazorShop.Storefront.GeneratedProof `
  -Scope foundation `
  -ValidateAfterApply `
  -BuildAfterApply
```

## Validation Commands

Static gate:

```powershell
.\tools\BlazorShop.AI.StorefrontBuilder\validate-storefront.ps1 `
  -ProjectRoot artifacts/storefront-builder/generated/BlazorShop.Storefront.GeneratedProof `
  -Name BlazorShop.Storefront.GeneratedProof `
  -StoreKey sample
```

Isolation gate:

```powershell
.\scripts\qa\run-storefront-builder-isolation-gate.ps1 -ProjectRoot artifacts/storefront-builder/generated/BlazorShop.Storefront.GeneratedProof -Name BlazorShop.Storefront.GeneratedProof
```

Canonical structure proof:

```powershell
.\scripts\qa\run-storefront-builder-generated-proof.ps1 -ProofLevel Structure
```

Canonical fast foundation functional proof:

```powershell
.\scripts\qa\run-storefront-builder-generated-proof.ps1 -ProofLevel FoundationFunctionalFast
```

Canonical full foundation functional proof:

```powershell
.\scripts\qa\run-storefront-builder-generated-proof.ps1 -ProofLevel FoundationFunctionalFull
```

Self-contained full fixture proof:

```powershell
.\scripts\qa\run-storefront-builder-full-proof-with-fixture.ps1 -Describe
.\scripts\qa\run-storefront-builder-full-proof-with-fixture.ps1
```

CI-friendly regeneration ownership gate:

```powershell
.\scripts\qa\run-storefront-builder-regeneration-gate.ps1
```

Phase 4 visual MVP gate:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\qa\run-storefront-phase4-mvp-gate.ps1 -GeneratedProjectRoot <generated-project-root> -FixtureRoot <fixture-root> -HandoffRoot <portable-handoff-root> -CommandTimeoutSeconds 600
```

Runtime visual MVP proof:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\qa\run-storefront-phase4-mvp-gate.ps1 -GeneratedProjectRoot <generated-project-root> -ProofMode Runtime -BaseUrl http://127.0.0.1:18620 -StartRuntimeHost -HandoffRoot <portable-handoff-root> -CommandTimeoutSeconds 600
```

Phase 4 final closure gate:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\qa\run-storefront-phase4-final-closure-gate.ps1 -CommandTimeoutSeconds 900
```

`Structure` generates/restores/builds the proof project, runs static validation, runs isolation, runs the shared visual consumer boundary validator, proves post-regeneration build, proves deterministic no-op regeneration, and proves manual-edit conflict reporting. Generated proof and isolation runners pack five Storefront packages from the current source `HEAD`: Client, Runtime, Presentation, Components, and Browser. Unless explicit package versions are passed, runners derive `1.0.0-local.{shortSha}`, clean only exact Storefront package cache folders for those versions, restore with `--no-cache --force-evaluate`, and record package hashes/provenance in metadata. `run-storefront-builder-regeneration-gate.ps1` separately proves no-op determinism, scoped CSS/page/component updates, real `-WhatIf` planning, platform metadata update, manual generated-file conflicts, user-owned preservation, protected-file rejection, obsolete-file reporting, and rollback without live Commerce Node data. `FoundationFunctionalFast` uses mocked same-origin Presentation BFF routes in Playwright and writes `fast-foundation-functional-report.md` under the generated artifact. Phase 4.12 final closure runs `FoundationFunctionalFast` as the minimum generated functional proof. `FoundationFunctionalFull` verifies fixture data, starts the generated storefront in Development, runs visual smoke QA and commerce-regression network checks, and writes `visual-qa-report.md` plus `functional-commerce-report.md` under the generated artifact. Use `run-storefront-builder-full-proof-with-fixture.ps1` for scheduled/manual/release validation because it starts Docker dependencies and the local V2 fixture runtime, checks health and fixture endpoints, runs the full proof, writes `full-proof-with-fixture-report.md`, and tears down services. `FoundationFunctional` and `-RunBrowserQa` remain compatibility aliases for the full proof.

Generated storefront validation must fail when generated source declares `@page`, imports `BlazorShop.Storefront.Components.Features`, or recreates protected Presentation-owned application logic; normal generation consumes Presentation plus `Contracts`, `Headless`, and `Browser` primitives and renders project-local DOM.

Focused test filter:

```powershell
dotnet test BlazorShop.Tests.V2\BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~StorefrontBuilder"
```

## Browser QA

Install Node dependencies once:

```powershell
Push-Location tools\BlazorShop.AI.StorefrontBuilder
npm ci
Pop-Location
```

Run the generated storefront before browser QA:

```powershell
dotnet run --no-build --project artifacts/storefront-builder/generated/BlazorShop.Storefront.GeneratedProof/BlazorShop.Storefront.GeneratedProof.csproj --urls http://127.0.0.1:18991
```

Then run:

```powershell
node tools\BlazorShop.AI.StorefrontBuilder\scripts\qa\run-visual-qa.mjs --base-url http://127.0.0.1:18991 --project-root artifacts/storefront-builder/generated/BlazorShop.Storefront.GeneratedProof --category-slug apparel --product-slug qa-simple-product-100
node tools\BlazorShop.AI.StorefrontBuilder\scripts\qa\run-commerce-regression.mjs --base-url http://127.0.0.1:18991 --project-root artifacts/storefront-builder/generated/BlazorShop.Storefront.GeneratedProof --category-slug apparel --product-slug qa-simple-product-100 --page-slug customer-service
```

Browser QA writes `visual-qa-report.md` and `functional-commerce-report.md` under the generated artifact. Do not commit generated proof output by default.

Runtime visual QA uses a running generated host:

```powershell
node tools\BlazorShop.AI.StorefrontBuilder\scripts\qa\run-visual-qa.mjs --base-url http://127.0.0.1:18991 --project-root artifacts/storefront-builder/generated/BlazorShop.Storefront.GeneratedProof
```

Do not pass `--fixture-root` in runtime visual proof. File-based `--fixture-root` proof is for skeleton/static validation and early feedback only; it is not final release closure.

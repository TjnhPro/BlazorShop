# StorefrontBuilder Phase 4 Agent-Assisted Visual Generation.todo

Status: In Progress
Owner: Storefront Platform
Created: 2026-08-02
Target folder: `docs/visual-reverse-engineering-skill`
Depends on:

- Phase 3E portable handoff final closure on a clean unchanged `HEAD`.
- `analysis/agent-handoff/handoff-readiness.json` passed and matching `analysis/agent-handoff/manifest.json`.
- Existing StorefrontBuilder generated proof, regeneration, isolation, and browser gates.

Primary goal: wire reviewed portable `analysis/agent-handoff/*` packages into StorefrontBuilder so an agent can generate a visual-only `BlazorShop.Storefront.{Name}` project without rereading raw evidence, without writing into Starter, and without recreating Storefront Presentation, Runtime, BFF, SEO, cart, checkout, account, auth, or commerce API behavior.

## Why This File Exists

Phase 3A through Phase 3E made reverse-engineering output portable and reviewable, but StorefrontBuilder still does not consume that handoff package. Current StorefrontBuilder generation is still based on the neutral Starter and deterministic static composition scripts.

The next step is not free-form AI editing. The next step is a controlled Phase 4 pipeline:

```text
portable reviewed handoff
  -> preflight validation
  -> deterministic generation plan
  -> Starter-based generated project
  -> constrained visual file generation
  -> boundary/build/browser proof
  -> regeneration safety proof
```

The phase must preserve the current V2 architecture: generated storefronts own visual markup, CSS, copy, assets, and local view-slot implementations only. Shared application behavior remains in `BlazorShop.Storefront.Presentation`, server transport remains in `BlazorShop.Storefront.Runtime`, generated API contracts remain in `BlazorShop.Storefront.Client`, and browser-safe behavior remains in `BlazorShop.Storefront.Browser` / `BlazorShop.Storefront.Components`.

## Codebase Baseline Findings

- `docs/architecture/11-storefront-builder.md` already defines the Phase 4 input boundary: Phase 4 may read only `analysis/agent-handoff/*` and registered schemas after Phase 3E passes.
- `docs/visual-reverse-engineering-skill/README.md`, `reference.md`, and `how-to-generate-and-validate.md` all state that StorefrontBuilder does not yet consume `analysis/agent-handoff/*`.
- `tools/BlazorShop.AI.StorefrontBuilder/build-storefront.ps1` currently supports `analyze-only`, `plan-only`, `generate`, `update`, `validate-only`, and `full`, but it does not accept a handoff package path.
- `tools/BlazorShop.AI.StorefrontBuilder/scripts/generate/plan-generation-files.mjs` currently writes a static/default dry-run plan. It is not a handoff-to-generation-plan compiler.
- `tools/BlazorShop.AI.StorefrontBuilder/scripts/generate/apply-composition.mjs` currently applies deterministic Starter composition to known files. It does not read a compiled handoff generation plan.
- `tools/BlazorShop.AI.StorefrontBuilder/regenerate-storefront.ps1` already has stable `-WhatIf` report behavior and `-WhatIfReportPath`; Phase 4 should reuse this instead of replanning it.
- `tools/BlazorShop.AI.StorefrontBuilder/version.json` is already the canonical `generatorVersion` source; Phase 4 should verify compatibility, not create another version source.
- `tools/BlazorShop.AI.StorefrontReverseEngineering/Analysis/Handoff/HandoffConsumerDryRunLoader.cs` already provides the right read-only portable preflight foundation.
- `tools/BlazorShop.AI.StorefrontReverseEngineering/Schemas/*` already contains the registered handoff schemas that Phase 4 must use.
- `BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation` owns route, BFF, SEO, media, app shell, and `StorefrontFoundationViewSet` contracts.
- `BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/starter-generation.contract.yaml` already defines generated zones, protected zones, slot metadata, route metadata, browser action policy, and the `BlazorShop.Storefront.{Name}` naming convention.
- `tools/BlazorShop.AI.StorefrontBuilder/scripts/qa/run-visual-qa.mjs` currently proves basic rendering and screenshots, but visual fidelity comparison is not implemented yet.

## Locked Decisions

- Use `BlazorShop.Storefront.{Name}` as the generated project naming convention unless a disposable proof explicitly requires a generated-proof name.
- Keep generated projects out of `BlazorShop.sln` by default.
- StorefrontBuilder remains development-time tooling only.
- Phase 4 consumes only `analysis/agent-handoff/*` plus registered handoff schemas.
- Phase 4 must fail closed when the handoff readiness is missing, failed, stale, or inconsistent with manifest readiness.
- Do not read raw `captures/*`, `analysis/pages/*`, `analysis/resolved/*`, `presentation-catalog/*`, `review/*`, `reports/*`, Storefront V2 source, Commerce Node source, backend source, or Control Plane source as fallback generation input.
- Do not write generated visual changes into `BlazorShop.Storefront.Starter`.
- Do not let generated files declare `@page`.
- Do not generate direct Commerce Node, Commerce Admin, Control Plane, or legacy API calls.
- Do not generate functional browser command JavaScript. Browser functionality must use Presentation descriptors and same-origin BFF/browser primitives.
- Cart, checkout, account, auth, payment result, maintenance, not-found, and service-unavailable behavior stay owned by Presentation/Runtime/Browser contracts; generated projects may only provide visual shell/template overrides in approved slots.
- Existing StorefrontBuilder static proof, regeneration proof, isolation proof, and browser proof must remain green while Phase 4 is introduced.
- GitHub Actions evidence is out of scope while Actions are disabled during development; local deterministic gates are authoritative for this plan.

## Out Of Scope

- Rewriting StorefrontBuilder from scratch.
- Reopening Phase 3A-3E handoff schema design unless a Phase 4 consumer exposes a concrete schema defect.
- Copying Storefront V2 markup or transport internals into generated storefronts.
- Creating new Commerce Node APIs or changing Storefront API contracts.
- Changing cart, checkout, account, auth, payment, sellability, pricing, inventory, or order business logic.
- Adding marketplace packaging or production deployment automation.
- Full pixel-perfect visual diff as a release blocker in the first Phase 4 cut. Phase 4 should add structured visual proof first, then enable stricter visual fidelity later.

## Autoplan Review Decisions

| Decision | Classification | Chosen Direction | Rationale |
| --- | --- | --- | --- |
| Phase 4 scope | Auto-decided | Implement handoff-driven visual generation only. | This is the smallest useful cut that activates Phase 3E output without changing commerce behavior. |
| First technical milestone | Auto-decided | Build the handoff-to-generation-plan compiler before agent editing. | Current plan generation is static; letting an agent edit before a deterministic plan would weaken boundaries. |
| AI editing model | Auto-decided | Constrained writes to generated-owned files only. | Prevents accidental rewrites of Presentation, Runtime, Starter, API contracts, and business logic. |
| Visual QA strictness | Taste decision | Start with structural screenshot proof and layout evidence, defer pixel diff. | Current QA script does not implement visual fidelity diff; making it a blocker immediately would overload Phase 4. |
| Project output | Auto-decided | Use existing generated output roots for proofs and `BlazorShop.Storefront.{Name}` for promoted custom projects. | Matches `starter-generation.contract.yaml` and architecture docs. |
| Regeneration behavior | Auto-decided | Reuse existing candidate/WhatIf/regeneration engine. | The current engine already protects manual edits, reports conflicts, and supports stable WhatIf reports. |

## Phase Order

1. Phase 4.0 - Baseline And Phase 3E Closure Lock
2. Phase 4.1 - Handoff Preflight In StorefrontBuilder
3. Phase 4.2 - Handoff-To-Generation-Plan Compiler
4. Phase 4.3 - Starter-Based Handoff Project Generation
5. Phase 4.4 - Constrained Agent Visual Implementation
6. Phase 4.5 - Boundary, Manifest, And Ownership Gates
7. Phase 4.6 - Visual Proof V1
8. Phase 4.7 - Repair Loop V1
9. Phase 4.8 - Regeneration And WhatIf Safety
10. Phase 4.9 - Documentation And Closure Evidence

Implement in this order. Do not start agent visual generation before Phase 4.2 produces a deterministic plan and Phase 4.3 proves the project can be generated from that plan.

## Phase 4.0 - Baseline And Phase 3E Closure Lock

Goal: prove Phase 4 starts from a valid Phase 3E handoff and does not reopen closed foundation work.

Tasks:

- [x] Confirm current `HEAD` is clean before Phase 4 implementation.
- [x] Confirm Phase 3E local closure report exists or rerun the local Phase 3E gate if the code changed since the last proof.
- [x] Confirm `docs/architecture/11-storefront-builder.md` still states that Phase 4 may read only `analysis/agent-handoff/*`.
- [x] Confirm `BlazorShop.Storefront.Starter/starter-generation.contract.yaml` still declares:
  - [x] generated project naming convention.
  - [x] allowed generated zones.
  - [x] protected zones.
  - [x] route metadata.
  - [x] browser action policy.
  - [x] view slot metadata.
- [x] Confirm `tools/BlazorShop.AI.StorefrontBuilder/version.json` is still the single generator version source.
- [x] Confirm `regenerate-storefront.ps1 -WhatIf` still writes a stable report outside the target project.
- [x] Capture the current list of StorefrontBuilder scripts touched by Phase 4.

Checks:

- [x] `git status --short`
- [x] `rg -n "Phase 4 may read only|agent-handoff|StorefrontBuilder generation does not yet consume" docs\architecture docs\visual-reverse-engineering-skill`
- [x] `rg -n "generatorVersion|WhatIfReportPath|plan-generation-files|apply-composition" tools\BlazorShop.AI.StorefrontBuilder`

Exit criteria:

- [x] Phase 4 starts from documented current architecture.
- [x] No hidden requirement to update GitHub Actions.
- [x] No decision in this phase requires changing Storefront API contracts.

Phase 4.0 intake evidence:

- Initial working tree before Phase 4 implementation contained only this new Phase 4 plan file and the README historical-plan link to it.
- Latest discovered Phase 3E final closure report before Phase 4.0 intake: `obj/storefront-reverse-engineering/reports/phase3e-final-closure-gate-20260802122027.md`, status `passed`, tested SHA `9bdb4d4be4019392360ab08796cf067422aa9597`.
- Current `HEAD` at Phase 4.0 intake was `8e20d68c`; because source commits exist after the Phase 3E report, the Phase 3E gate must be rerun from a clean tree before Phase 4.0 closes.
- The gate rerun requires this plan/README intake to be committed first so the clean-tree check can run honestly.
- Phase 4.0 intake committed in `48cb2009`.
- Phase 4.0 baseline repair commits:
  - `0f2c7fae` fixed the Phase 3E dry-run negative test so it mutates the registered consumer reference field and rehashes the portable manifest, and fixed failed-report writing in the Phase 3E gate.
  - `3ea54006` fixed StorefrontBuilder smoke name shadowing in the shared Phase 3 proof helper so the gate passes `Phase3EClosure` to the PascalCase project-name validator.
- Focused verification passed: `dotnet test tools\BlazorShop.AI.StorefrontReverseEngineering\tests\BlazorShop.AI.StorefrontReverseEngineering.Tests\BlazorShop.AI.StorefrontReverseEngineering.Tests.csproj --no-restore --filter "FullyQualifiedName~HandoffConsumerDryRunLoaderTests" --blame-hang --blame-hang-timeout 5m`.
- StorefrontBuilder smoke verification passed: `pwsh -NoProfile -ExecutionPolicy Bypass -File tools\BlazorShop.AI.StorefrontBuilder\build-storefront.ps1 -Url https://example.test -Name Phase3EClosure -StoreKey sample -OutputRoot obj/storefront-builder/generated/reverse-engineering-phase3e-gate -Mode plan-only`.
- Final Phase 3E closure gate passed on clean `HEAD` `3ea54006bb90230a756c55a061ef0b88e6952cd6`: `obj/storefront-reverse-engineering/reports/phase3e-final-closure-gate-20260802171238.md`.
- Phase 4.0 checklist commands were run after the passing gate:
  - `git status --short` returned clean.
  - `rg -n "Phase 4 may read only|agent-handoff|StorefrontBuilder generation does not yet consume" docs\architecture docs\visual-reverse-engineering-skill`.
  - `rg -n "generatorVersion|WhatIfReportPath|plan-generation-files|apply-composition" tools\BlazorShop.AI.StorefrontBuilder`.
  - `rg -n "namingConvention|allowedGeneratedZones|protectedZones|routes|browserActionPolicy|slots" BlazorShop.PresentationV2\BlazorShop.Storefront.Starter\starter-generation.contract.yaml`.
- Phase 4 StorefrontBuilder script surface captured for later phases: `build-storefront.ps1`, `regenerate-storefront.ps1`, `scripts/generate/plan-generation-files.mjs`, `scripts/generate/apply-composition.mjs`, `scripts/generate/new-storefront-project.ps1`, existing `scripts/validate/*`, and `scripts/qa/run-visual-qa.mjs`.

## Phase 4.1 - Handoff Preflight In StorefrontBuilder

Goal: add a StorefrontBuilder-facing preflight that can load and reject/accept a portable handoff package before generation.

Implementation targets:

- `tools/BlazorShop.AI.StorefrontBuilder/build-storefront.ps1`
- New or extended StorefrontBuilder script under `tools/BlazorShop.AI.StorefrontBuilder/scripts/generate/`
- Existing ReverseEngineering preflight surface:
  - `validate-handoff`
  - `inspect-handoff`
  - `HandoffConsumerDryRunLoader`

Tasks:

- [x] Add a handoff input parameter to StorefrontBuilder generation entrypoints, for example `-HandoffRoot <path>`.
- [x] Support a preflight-only mode without generating files.
- [x] Validate that `-HandoffRoot` points to an `analysis/agent-handoff` folder or a copied portable package root with the same internal shape.
- [x] Reject handoff roots that are inside raw source-only artifact folders unless they expose the portable handoff package.
- [x] Load and validate:
  - [x] `manifest.json`
  - [x] `handoff-readiness.json`
  - [x] `page-compositions.json`
  - [x] `storefront-pattern.json`
  - [x] `presentation-catalog.json`
  - [x] `presentation-mappings.json`
  - [x] `allowed-files.json`
  - [x] `protected-files.json`
  - [x] `design-tokens.json`
  - [x] `visual-style.json`
  - [x] `responsive-behavior.json`
  - [x] `interaction-models.json`
  - [x] `originality-restrictions.json`
  - [x] `evidence-manifest.json`
  - [x] `unresolved-regions.json`
- [x] Fail when `handoff-readiness.json` is missing or not passed.
- [x] Fail when manifest readiness disagrees with `handoff-readiness.json`.
- [x] Fail when schema hashes or artifact hashes drift according to the portable validator.
- [x] Fail when required schemas are missing.
- [x] Fail when any consumer dependency points outside `analysis/agent-handoff/*`.
- [x] Fail when unresolved blocking regions exist.
- [x] Print problem/cause/fix messages consistent with current StorefrontBuilder error style.
- [x] Produce a preflight report under `obj/storefront-builder/handoff-preflight/` or the generated artifact's `docs/storefront-analysis/` only after a generated project exists.

Forbidden behavior:

- [x] Do not read raw `captures/*` as fallback.
- [x] Do not read source `analysis/pages/*` as fallback.
- [x] Do not read source `analysis/resolved/*` as fallback.
- [x] Do not read Storefront V2 source as fallback.
- [x] Do not infer routes from screenshots.

Tests:

- [x] Positive: valid copied portable handoff passes preflight.
- [x] Negative: missing `handoff-readiness.json` fails.
- [x] Negative: readiness failed fails.
- [x] Negative: manifest/readiness mismatch fails.
- [x] Negative: artifact hash drift fails.
- [x] Negative: missing schema fails.
- [x] Negative: consumer reference outside handoff fails.
- [x] Negative: raw capture fallback attempt fails.

Exit criteria:

- [x] StorefrontBuilder can perform handoff preflight without generating a project.
- [x] The preflight is portable and does not require the original reverse-engineering source project.

Phase 4.1 evidence:

- Added `build-storefront.ps1 -Mode preflight-only -HandoffRoot <path> -HandoffSchemaRoot <path>` and StorefrontBuilder preflight report output under `obj/storefront-builder/handoff-preflight/`.
- Added `scripts/generate/Test-HandoffPreflight.ps1` with package-root/direct-`analysis/agent-handoff` resolution, required artifact checks, raw-folder rejection, `validate-handoff`, `dry-run-handoff`, unresolved-blocker rejection, and problem/cause/fix error codes `SFB-HANDOFF-001` through `SFB-HANDOFF-009` plus `SFB-HANDOFF-012`.
- Added `dry-run-handoff` CLI command over `HandoffConsumerDryRunLoader` for StorefrontBuilder-facing portable preflight output.
- Updated Phase 3/4 boundary docs and gate wording so approved Phase 4 preflight can read portable `analysis/agent-handoff/*` packages while handoff-driven generation remains gated behind later phases.
- Focused verification passed: `dotnet test tools\BlazorShop.AI.StorefrontReverseEngineering\tests\BlazorShop.AI.StorefrontReverseEngineering.Tests\BlazorShop.AI.StorefrontReverseEngineering.Tests.csproj --no-restore --filter "FullyQualifiedName~AgentHandoffTests|FullyQualifiedName~PortableHandoffCliTests|FullyQualifiedName~StorefrontBuilderHandoffPreflightTests" --blame-hang --blame-hang-timeout 5m` (`63` passed).
- StorefrontBuilder preflight verification passed through `StorefrontBuilderHandoffPreflightTests` (`9` passed) including positive copied-package/direct-folder cases and negative readiness, manifest mismatch, hash drift, missing schema, consumer-reference escape, and raw folder fallback cases.
- Script syntax verification passed: `pwsh -NoProfile -Command "& { `$script = Get-Content -LiteralPath 'scripts\qa\storefront-reverse-engineering-phase3-proof-steps.ps1' -Raw; [void][scriptblock]::Create(`$script); Write-Output 'phase3 proof steps syntax ok' }"`.

## Phase 4.2 - Handoff-To-Generation-Plan Compiler

Goal: replace the static Phase 4 planning gap with a deterministic compiler from reviewed handoff artifacts into a generation plan.

Implementation targets:

- `tools/BlazorShop.AI.StorefrontBuilder/scripts/generate/plan-generation-files.mjs`
- New module, for example `scripts/generate/handoff-generation-plan.mjs`
- `tools/BlazorShop.AI.StorefrontBuilder/schemas/generation-plan.schema.json`
- Existing `starter-generation.contract.yaml`

Tasks:

- [x] Add a handoff-aware generation plan mode that accepts the validated handoff package.
- [x] Keep existing static `plan-only` behavior available for current generated proof until handoff mode is proven.
- [x] Extend the generation plan schema from broad file list to an explicit contract:
  - [x] `schemaVersion`
  - [x] `artifactKind`
  - [x] `artifactId`
  - [x] `generatorVersion`
  - [x] `sourceHandoffPackageHash`
  - [x] `sourceHandoffReadinessHash`
  - [x] `sourceStarterContractHash`
  - [x] `projectName`
  - [x] `storeKey`
  - [x] `generationMode`
  - [x] `files`
  - [x] `slots`
  - [x] `assets`
  - [x] `copyBlocks`
  - [x] `tokens`
  - [x] `warnings`
  - [x] `blockedItems`
- [x] For each planned file, include:
  - [x] normalized target path.
  - [x] ownership classification: generated, managed, protected, user-owned candidate, obsolete candidate.
  - [x] source handoff artifact references.
  - [x] source evidence references, only by handoff-local paths.
  - [x] allowed operation: create, replace, patch, skip, conflict, obsolete.
  - [x] rationale.
  - [x] slot id or page id when applicable.
  - [x] checksum/provenance fields for deterministic regeneration.
- [x] Map handoff `page-compositions.json` to Presentation view slots.
- [x] Map handoff `storefront-pattern.json` and Starter contract slots to generated file targets.
- [x] Map handoff `design-tokens.json` and `visual-style.json` to generated CSS token files.
- [x] Map handoff `responsive-behavior.json` to responsive layout instructions without generating functional application logic.
- [x] Map handoff `interaction-models.json` only to visual affordance metadata or allowed semantic descriptors.
- [x] Map `originality-restrictions.json` to asset/copy reuse policy:
  - [x] block disallowed copied assets.
  - [x] mark assets needing replacement.
  - [x] mark copy that must be rewritten.
  - [x] preserve evidence references without copying restricted originals.
- [x] Map `unresolved-regions.json` to blocking or warning plan entries.
- [x] Ensure cart/checkout/account/auth/payment result plans are visual shell plans only.
- [x] Ensure no planned generated file targets Starter.
- [x] Ensure no planned generated file declares route ownership.
- [x] Sort all arrays and output deterministically.
- [x] Add readable dry-run output that summarizes create/update/skip/conflict/blocked entries.

Tests:

- [x] Positive: same handoff package produces byte-stable generation plan across two runs.
- [x] Positive: valid ecommerce page composition maps to expected Presentation slots.
- [x] Positive: product gallery, product purchase, product grid, layout, footer, and state pages map to generated-owned zones.
- [x] Positive: cart/checkout/account map only to shell/template zones.
- [x] Negative: forbidden target path fails.
- [x] Negative: protected target path fails.
- [x] Negative: missing required slot fails.
- [x] Negative: unsupported interaction requiring business logic fails or becomes manual blocker.
- [x] Negative: restricted copied asset is blocked or marked replacement-required.
- [x] Negative: raw evidence path in plan fails validation.

Exit criteria:

- [x] StorefrontBuilder has a deterministic handoff-driven generation plan.
- [x] The plan is reviewable before any file writes.
- [x] The plan contains enough provenance to debug why each generated file exists.

Phase 4.2 evidence:

- Added `scripts/generate/handoff-generation-plan.mjs` and extended `plan-generation-files.mjs` so `--handoff-root` compiles reviewed portable `analysis/agent-handoff/*` artifacts into deterministic JSON/YAML generation plans while preserving static `plan-only` behavior.
- Expanded `generation-plan.schema.json`, the valid schema fixture, and `Test-StorefrontBuilderGenerationPlan.ps1` to require handoff hashes, Starter contract hash, store/project metadata, file ownership, target paths, handoff-local evidence references, provenance, slots, assets, copy blocks, token summaries, warnings, and blockers.
- Added `StorefrontBuilderHandoffGenerationPlanTests` for deterministic output, ecommerce slot mapping, visual-shell cart/checkout/account mapping, forbidden/protected target rejection, missing required slots, unsupported functional interactions, restricted assets, and raw evidence rejection.
- Focused verification passed: `dotnet test tools\BlazorShop.AI.StorefrontReverseEngineering\tests\BlazorShop.AI.StorefrontReverseEngineering.Tests\BlazorShop.AI.StorefrontReverseEngineering.Tests.csproj --no-restore --filter "FullyQualifiedName~StorefrontBuilderHandoffPreflightTests|FullyQualifiedName~StorefrontBuilderHandoffGenerationPlanTests|FullyQualifiedName~PortableHandoffCliTests|FullyQualifiedName~AgentHandoffTests" --blame-hang --blame-hang-timeout 5m` (`72` passed).
- Schema verification passed: `pwsh -NoProfile -ExecutionPolicy Bypass -File tools\BlazorShop.AI.StorefrontBuilder\scripts\validate\Test-StorefrontBuilderSchemas.ps1`.
- Static plan compatibility passed: `node tools\BlazorShop.AI.StorefrontBuilder\scripts\generate\plan-generation-files.mjs --project-name BlazorShop.Storefront.GeneratedProof --store-key sample --output-root obj/storefront-builder/generated/static-plan-check --output obj/storefront-builder/static-plan-check/generation-plan.yaml --dry-run` followed by `pwsh -NoProfile -ExecutionPolicy Bypass -File tools\BlazorShop.AI.StorefrontBuilder\scripts\validate\Test-StorefrontBuilderGenerationPlan.ps1 -PlanPath obj\storefront-builder\static-plan-check\generation-plan.yaml`.
- Handoff entrypoint proof passed: `pwsh -NoProfile -ExecutionPolicy Bypass -File tools\BlazorShop.AI.StorefrontBuilder\build-storefront.ps1 -Url https://example.test -Name Phase4PlanEntrypoint -StoreKey sample -OutputRoot obj/storefront-builder/generated/phase4-plan-entrypoint -Mode plan-only -HandoffRoot <portable-root> -HandoffSchemaRoot tools\BlazorShop.AI.StorefrontReverseEngineering\Schemas`.
- Generated handoff plan validation passed: `pwsh -NoProfile -ExecutionPolicy Bypass -File tools\BlazorShop.AI.StorefrontBuilder\scripts\validate\Test-StorefrontBuilderGenerationPlan.ps1 -PlanPath obj\storefront-builder\generation-plan.yaml`.

## Phase 4.3 - Starter-Based Handoff Project Generation

Goal: create or update a generated storefront project from Starter using the compiled handoff generation plan.

Implementation targets:

- `tools/BlazorShop.AI.StorefrontBuilder/build-storefront.ps1`
- `tools/BlazorShop.AI.StorefrontBuilder/scripts/generate/new-storefront-project.ps1`
- `tools/BlazorShop.AI.StorefrontBuilder/scripts/generate/apply-composition.mjs`
- Generated artifact docs under `docs/storefront-analysis/`

Tasks:

- [x] Add a handoff generation mode that starts from the existing Starter copy process.
- [x] Keep Starter immutable; all project-specific output must go to generated output root or promoted `BlazorShop.Storefront.{Name}` project.
- [x] Write generation plan into generated project `docs/storefront-analysis/generation-plan.json` or equivalent review artifact.
- [x] Write a readable `docs/storefront-analysis/handoff-generation-summary.md`.
- [x] Write metadata linking:
  - [x] generator version.
  - [x] Starter contract hash.
  - [x] Storefront OpenAPI hash.
  - [x] handoff package hash.
  - [x] handoff readiness hash.
  - [x] plan hash.
- [x] Create visual file placeholders according to the plan before agent implementation.
- [x] Preserve Presentation package/reference behavior already used by generated proof.
- [x] Preserve Components/Browser dependency rules already used by Starter.
- [x] Do not add generated project to `BlazorShop.sln`.
- [x] Ensure generated project can restore/build before AI-tuned visuals are applied.
- [x] Ensure missing optional pages/components are represented as explicit warnings, not silent omissions.

Tests:

- [x] Positive: valid handoff creates a generated project under `artifacts/storefront-builder/generated/{ProjectName}`.
- [x] Positive: valid handoff can create a disposable project under `obj/storefront-builder/generated/{ProjectName}` for automated proof.
- [x] Positive: generated project restores and builds before agent visual fill.
- [x] Negative: unsafe project name fails before files are created.
- [x] Negative: handoff plan targeting Starter fails.
- [x] Negative: attempt to add generated project to solution is absent.

Exit criteria:

- [x] A valid handoff can produce a buildable project skeleton from Starter.
- [x] The skeleton contains plan and provenance artifacts.
- [x] No AI implementation is required for this phase to pass.

Phase 4.3 evidence:

- Extended `build-storefront.ps1 -Mode generate/full -HandoffRoot <path>` to route through the Starter copy process and handoff skeleton path while preserving the existing static generation path when no handoff root is supplied.
- Extended `scripts/generate/new-storefront-project.ps1` to compile the handoff plan into generated `docs/storefront-analysis/generation-plan.json` and `.yaml`, write handoff metadata/provenance into `metadata.yaml`, apply placeholders in staging, validate the generated project, and publish atomically.
- Added `scripts/generate/apply-handoff-project-skeleton.mjs` to write visual-only CSS/markup placeholders from plan entries, add the generated CSS link in the generated project copy, write `handoff-generation-summary.md`, and write `handoff-placeholders.json`.
- Adjusted handoff generation plans so protected package zones are validation inputs, not published `plan.files` entries, preventing generated analysis artifacts from containing forbidden Storefront V2/backend package references.
- Added `StorefrontBuilderHandoffProjectGenerationTests` covering artifact-root generation, disposable obj-root generation, restore/build before visual fill, unsafe name rejection, Starter target rejection, and solution-file non-registration.
- Generated artifact-root proof passed under `artifacts/storefront-builder/generated/phase4-project-tests/.../BlazorShop.Storefront.Phase4ArtifactProject`.
- Generated disposable proof passed under `obj/storefront-builder/generated/phase4-skeleton-probe/BlazorShop.Storefront.Phase4SkeletonProbe`.
- Restore/build proof passed: `dotnet restore obj\storefront-builder\generated\phase4-skeleton-probe\BlazorShop.Storefront.Phase4SkeletonProbe\BlazorShop.Storefront.Phase4SkeletonProbe.csproj` and `dotnet build obj\storefront-builder\generated\phase4-skeleton-probe\BlazorShop.Storefront.Phase4SkeletonProbe\BlazorShop.Storefront.Phase4SkeletonProbe.csproj --no-restore`.
- Focused verification passed: `dotnet test tools\BlazorShop.AI.StorefrontReverseEngineering\tests\BlazorShop.AI.StorefrontReverseEngineering.Tests\BlazorShop.AI.StorefrontReverseEngineering.Tests.csproj --no-restore --filter "FullyQualifiedName~StorefrontBuilderHandoffGenerationPlanTests|FullyQualifiedName~StorefrontBuilderHandoffProjectGenerationTests" --blame-hang --blame-hang-timeout 5m` (`13` passed).
- StorefrontBuilder architecture verification passed: `dotnet test BlazorShop.Tests.V2\BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~StorefrontBuilderVisualGenerationTests|FullyQualifiedName~StorefrontBuilderQaRegenerationTests|FullyQualifiedName~StorefrontBuilderFoundationTests" --blame-hang --blame-hang-timeout 5m` (`39` passed; existing MessagePack vulnerability warnings only).
- Published generation-plan validation passed: `pwsh -NoProfile -ExecutionPolicy Bypass -File tools\BlazorShop.AI.StorefrontBuilder\scripts\validate\Test-StorefrontBuilderGenerationPlan.ps1 -PlanPath <generated-project>\docs\storefront-analysis\generation-plan.yaml`.

## Phase 4.4 - Constrained Agent Visual Implementation

Goal: let an agent fill visual-only files from the generation plan while preventing free-form project rewrites.

Implementation targets:

- New StorefrontBuilder agent task package writer.
- Existing generated file manifest tooling.
- Existing ownership/protected-file regeneration logic.

Tasks:

- [x] Generate an agent task package from the generation plan.
- [x] Include only these allowed inputs in the task package:
  - [x] generation plan.
  - [x] handoff-local evidence references.
  - [x] approved section screenshots/crops.
  - [x] design token/style summaries.
  - [x] Starter/Presentation slot contract summaries.
  - [x] allowed/protected file manifests.
  - [x] originality restrictions.
- [x] Exclude raw captures, source project folders, Storefront V2 source, backend source, and hidden fallback artifacts.
- [x] Write explicit instructions that generated code may only modify generated-owned files.
- [x] Allow generated visual outputs for:
  - [x] layout/header/footer/navigation view slot files.
  - [x] home/category/search/deals/new releases/content visual templates.
  - [x] product summary card.
  - [x] product gallery.
  - [x] product purchase visual template.
  - [x] cart visual shell.
  - [x] checkout visual shell.
  - [x] account visual shell.
  - [x] state pages.
  - [x] CSS tokens/theme files.
  - [x] generated local assets.
- [x] Forbid generated outputs for:
  - [x] route declarations.
  - [x] BFF endpoints.
  - [x] HTTP clients.
  - [x] DTOs.
  - [x] payment/cart/checkout/account business commands.
  - [x] authentication flow logic.
  - [x] SEO route/canonical logic.
  - [x] server configuration.
  - [x] appsettings secrets.
  - [x] Storefront Runtime registration.
- [x] Require generated components to use existing Presentation descriptors and semantic `data-storefront-*` attributes where applicable.
- [x] Require copy to be store-owned and localizable later; do not embed backend error copy as final UX.
- [x] Require image outputs to respect asset originality restrictions.
- [x] Update generated file manifest after agent writes.
- [x] Record every agent-written file with source plan entry id and checksum.

Tests:

- [x] Positive: agent task package contains only handoff-local references.
- [x] Positive: generated visual files land only in allowed generated zones.
- [x] Positive: generated file manifest records agent-written files.
- [x] Negative: attempted `@page` directive fails gate.
- [x] Negative: attempted `HttpClient`, `fetch`, or direct Commerce Node URL fails gate.
- [x] Negative: attempted write to Starter fails gate.
- [x] Negative: attempted write to Presentation/Runtime/Client/Browser/Components packages fails gate.
- [x] Negative: generated functional JS outside approved visual zone fails gate.

Exit criteria:

- [x] Agent work is constrained by the plan and file ownership model.
- [x] Generated visuals can be inspected and regenerated safely.

Phase 4.4 evidence:

- Added `scripts/generate/write-agent-task-package.mjs` and wired handoff project generation to publish an agent task package under generated `docs/storefront-analysis/agent-task-package`.
- Added `scripts/generate/record-agent-visual-writes.mjs` to validate agent-written files against `allowedOutputFiles`, reject route/transport/business/auth/SEO/server tokens, preserve required Presentation descriptors for product purchase visuals, and write `agent-written-files.json`.
- Updated generated `docs/storefront-analysis/generated-files.yaml` after agent writes with `agentWrittenFiles` entries containing `filePath`, `sourcePlanEntryId`, checksum, and ownership.
- Added `StorefrontBuilderAgentTaskPackageTests` covering package input boundaries, allowed visual writes, manifest recording, `@page`, transport/direct Commerce Node calls, DTO/business token rejection, protected package writes, Starter writes, and unplanned functional JS writes.
- Manual probe passed for handoff-generated project under `obj/storefront-builder/generated/phase4-agent-package-probe/.../BlazorShop.Storefront.Phase4AgentPackageProbe`.
- Manual write-recording probe passed for `Components/Catalog/ProductSummaryCard.razor`, producing `agent-written-files.json` and `agentWrittenFiles` manifest entries.
- Task package forbidden-reference scan passed: no `captures/`, source analysis folders, Storefront V2 package references, or backend API package references were present in generated task package text files.
- Focused verification passed: `dotnet test tools\BlazorShop.AI.StorefrontReverseEngineering\tests\BlazorShop.AI.StorefrontReverseEngineering.Tests\BlazorShop.AI.StorefrontReverseEngineering.Tests.csproj --no-restore --filter "FullyQualifiedName~StorefrontBuilderAgentTaskPackageTests" --blame-hang --blame-hang-timeout 5m` (`14` passed).

## Phase 4.5 - Boundary, Manifest, And Ownership Gates

Goal: harden validation so a generated visual project cannot accidentally become an application/runtime fork.

Implementation targets:

- `tools/BlazorShop.AI.StorefrontBuilder/scripts/validate/*`
- `scripts/qa/run-storefront-builder-generated-proof.ps1`
- `scripts/qa/run-storefront-builder-isolation-gate.ps1`
- Generated `docs/storefront-analysis/generated-files.yaml`

Tasks:

- [x] Extend static validation for handoff-generated projects.
- [x] Validate the handoff package hash recorded in metadata.
- [x] Validate generation plan hash recorded in metadata.
- [x] Validate generated file manifest includes plan entry ids for handoff-generated files.
- [x] Validate no generated file references raw evidence paths.
- [x] Validate no generated file references Storefront V2.
- [x] Validate no generated file references backend/core/API projects.
- [x] Validate no generated file declares `@page`.
- [x] Validate no generated browser file calls Commerce Node directly.
- [x] Validate no generated project references `BlazorShop.Web.SharedV2`.
- [x] Validate no generated project directly references Runtime or Client unless a documented low-level extension is explicitly approved.
- [x] Validate generated project consumes Presentation/Components according to current isolation gate.
- [x] Validate protected files are unchanged unless scope is explicitly `foundation`.
- [x] Validate manual edits are not overwritten during regeneration.
- [x] Validate obsolete generated files are reported.
- [x] Validate `docs/storefront-analysis` records plan/report/proof lineage.

Tests:

- [x] Add focused tests for every new validator rule.
- [x] Add fixture generated project with a valid handoff plan.
- [x] Add fixture generated project with forbidden route declaration.
- [x] Add fixture generated project with forbidden direct transport.
- [x] Add fixture generated project with forbidden V2 reference.
- [x] Add fixture generated project with forbidden raw evidence reference.
- [x] Add fixture generated project with protected file mutation.

Exit criteria:

- [x] Static gates can distinguish valid visual generation from boundary leaks.
- [x] Existing non-handoff generated proof remains green.

Phase 4.5 evidence:

- Added `scripts/validate/Test-StorefrontBuilderHandoffBoundary.mjs` for handoff-aware static validation of metadata hashes, generation-plan hash, agent task package hash, generated manifest plan ids, lineage artifacts, raw evidence references, Storefront V2/backend/Web.SharedV2 leaks, route declarations, direct browser transport, protected-file mutation, manual edit visibility, and obsolete/missing manifest reporting.
- Updated `scripts/validate/Test-StorefrontBuilderStaticGate.ps1` to detect handoff-generated projects by `docs/storefront-analysis/generation-plan.json`, run the handoff boundary validator, and keep existing visual proof validators for non-handoff projects.
- Updated `generated-file-manifest.mjs` so handoff-generated files record `sourcePlanEntryId` and source artifacts that exist under generated `docs/storefront-analysis`.
- Updated handoff project generation to refresh `generated-files.yaml` and `regeneration-report.md` during staging before publish.
- Added `StorefrontBuilderHandoffBoundaryValidationTests` covering valid handoff static gate, forbidden `@page`, forbidden direct transport, forbidden Storefront V2 reference, forbidden raw evidence reference, protected file mutation, and missing source plan entry id.
- Handoff static gate probe passed: `pwsh -NoProfile -ExecutionPolicy Bypass -File tools\BlazorShop.AI.StorefrontBuilder\scripts\validate\Test-StorefrontBuilderStaticGate.ps1 -ProjectRoot obj\storefront-builder\generated\phase4-boundary-probe\BlazorShop.Storefront.Phase4BoundaryProbe -Name BlazorShop.Storefront.Phase4BoundaryProbe -StoreKey sample`.
- Focused verification passed: `dotnet test tools\BlazorShop.AI.StorefrontReverseEngineering\tests\BlazorShop.AI.StorefrontReverseEngineering.Tests\BlazorShop.AI.StorefrontReverseEngineering.Tests.csproj --no-restore --filter "FullyQualifiedName~StorefrontBuilderHandoffBoundaryValidationTests" --blame-hang --blame-hang-timeout 5m` (`7` passed).
- Existing non-handoff StorefrontBuilder architecture verification passed: `dotnet test BlazorShop.Tests.V2\BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~StorefrontBuilderVisualGenerationTests|FullyQualifiedName~StorefrontBuilderQaRegenerationTests|FullyQualifiedName~StorefrontBuilderFoundationTests" --blame-hang --blame-hang-timeout 5m` (`39` passed; existing MessagePack vulnerability and Browserslist warnings only).

## Phase 4.6 - Visual Proof V1

Goal: prove generated storefront visuals render in real browsers without making pixel-perfect matching a first release blocker.

Implementation targets:

- `tools/BlazorShop.AI.StorefrontBuilder/scripts/qa/run-visual-qa.mjs`
- Optional new handoff visual proof script under `tools/BlazorShop.AI.StorefrontBuilder/scripts/qa/`
- Generated `docs/storefront-analysis/visual-qa-report.md`

Tasks:

- [ ] Extend visual QA report to understand handoff-generated projects.
- [ ] Capture desktop and mobile screenshots for required routes.
- [ ] Verify generated CSS is loaded.
- [ ] Verify generated assets resolve.
- [ ] Verify body is nonblank.
- [ ] Verify required slots are visible for:
  - [ ] layout/header/footer/navigation.
  - [ ] home sections.
  - [ ] category/product grid.
  - [ ] search result state.
  - [ ] deals/new releases if planned.
  - [ ] product gallery.
  - [ ] product purchase panel.
  - [ ] cart shell.
  - [ ] checkout shell.
  - [ ] account shell.
  - [ ] state pages.
- [ ] Verify no obvious viewport overflow for generated primary regions.
- [ ] Verify no known placeholder text remains in generated-owned visual files unless explicitly planned.
- [ ] Verify semantic descriptors needed by browser actions remain present.
- [ ] Verify product image gallery uses stable square media containers where product gallery is generated.
- [ ] Record screenshots and report paths in generated analysis docs.
- [ ] Clearly label visual fidelity diff as not yet a hard gate unless implemented in this phase.

Tests:

- [ ] Positive: handoff-generated project passes visual proof with seeded/mock data.
- [ ] Negative: missing CSS fails.
- [ ] Negative: blank page fails.
- [ ] Negative: missing required slot fails.
- [ ] Negative: broken asset fails.
- [ ] Negative: removed browser action descriptor fails.

Exit criteria:

- [ ] Generated visual project has browser evidence across desktop and mobile.
- [ ] Failures identify the route, viewport, selector/slot, cause, and fix.

## Phase 4.7 - Repair Loop V1

Goal: allow bounded repair of generated-owned visual files based on build, boundary, and visual proof failures.

Tasks:

- [ ] Add a repair report format under generated `docs/storefront-analysis/repair-history.md` or equivalent.
- [ ] Record each repair attempt:
  - [ ] timestamp.
  - [ ] failure source.
  - [ ] failing file.
  - [ ] plan entry id.
  - [ ] attempted fix.
  - [ ] result.
  - [ ] remaining blockers.
- [ ] Limit repair inputs to failed validation output, generation plan, and handoff package.
- [ ] Limit repair writes to generated-owned files only.
- [ ] Stop after a bounded number of repair attempts.
- [ ] Escalate unresolved protected-file, route, transport, or business-logic issues to manual blockers.
- [ ] Ensure repair cannot broaden scope or change generation plan without explicit re-plan.

Tests:

- [ ] Positive: CSS/layout failure can be repaired in generated-owned file.
- [ ] Positive: missing slot markup can be repaired in generated-owned file.
- [ ] Negative: repair attempt to modify protected file is blocked.
- [ ] Negative: repair attempt to add `@page` is blocked.
- [ ] Negative: repair attempt to add direct API call is blocked.
- [ ] Negative: repeated repair failure stops with manual blocker.

Exit criteria:

- [ ] Repair loop improves generated visuals without weakening architectural boundaries.
- [ ] All unresolved issues are visible in a durable report.

## Phase 4.8 - Regeneration And WhatIf Safety

Goal: prove handoff-generated storefronts can be regenerated without losing manual work or hiding conflicts.

Implementation targets:

- `tools/BlazorShop.AI.StorefrontBuilder/regenerate-storefront.ps1`
- Existing regeneration ownership gate
- Generated file manifest and metadata

Tasks:

- [ ] Extend regeneration candidate creation to preserve handoff generation metadata.
- [ ] Ensure no-op regeneration from the same handoff is deterministic.
- [ ] Ensure scoped regeneration works for:
  - [ ] `css`
  - [ ] `component`
  - [ ] `page`
  - [ ] `foundation`
  - [ ] `validate`
  - [ ] `conflicts`
- [ ] Ensure manual edits to generated-owned files are detected.
- [ ] Ensure user-owned files are preserved.
- [ ] Ensure protected files are skipped unless `foundation` scope explicitly permits metadata update.
- [ ] Ensure obsolete planned files are reported.
- [ ] Ensure `-WhatIf` prints meaningful action lines and writes a stable report outside the target.
- [ ] Ensure handoff package hash drift produces an explicit re-plan/update requirement.
- [ ] Ensure Starter contract drift produces an explicit foundation upgrade requirement.

Tests:

- [ ] Positive: no-op regeneration produces no diff.
- [ ] Positive: scoped CSS regeneration touches only planned CSS files.
- [ ] Positive: scoped component regeneration touches only planned component files.
- [ ] Positive: manual edit conflict is reported, not overwritten.
- [ ] Positive: obsolete generated file is reported.
- [ ] Positive: `-WhatIf` report is visible after candidate cleanup.
- [ ] Negative: changed handoff hash without re-plan fails.
- [ ] Negative: protected-file target in plan fails.

Exit criteria:

- [ ] Handoff-generated storefronts are safe to regenerate.
- [ ] Developer/agent can review update actions before applying them.

## Phase 4.9 - Documentation And Closure Evidence

Goal: update operator docs and record local proof for Phase 4 without relying on GitHub Actions.

Docs to update:

- [ ] `docs/architecture/11-storefront-builder.md`
- [ ] `docs/visual-reverse-engineering-skill/README.md`
- [ ] `docs/visual-reverse-engineering-skill/reference.md`
- [ ] `docs/visual-reverse-engineering-skill/how-to-generate-and-validate.md`
- [ ] `docs/agents/storefront-builder.md`

Documentation tasks:

- [ ] Document handoff generation command.
- [ ] Document handoff preflight command.
- [ ] Document generation plan artifact format.
- [ ] Document allowed/forbidden inputs.
- [ ] Document generated project output roots.
- [ ] Document visual-only boundaries.
- [ ] Document repair loop behavior.
- [ ] Document regeneration/WhatIf behavior for handoff-generated projects.
- [ ] Document QA commands for local proof.
- [ ] Document that GitHub Actions evidence is intentionally out of scope while disabled.

Closure report:

- [ ] Add a tracked closure summary only after implementation and local gates pass.
- [ ] Record tested commit SHA.
- [ ] Record exact commands run.
- [ ] Record generated project path.
- [ ] Record handoff package path and hash.
- [ ] Record plan hash.
- [ ] Record build result.
- [ ] Record static gate result.
- [ ] Record isolation gate result.
- [ ] Record browser visual proof result.
- [ ] Record regeneration/WhatIf proof result.
- [ ] Record known deferred scope.

Exit criteria:

- [ ] Future agents can run the Phase 4 path without guessing commands or inputs.
- [ ] Docs match actual scripts and validation behavior.

## Required Local QA Gate

Run this gate before closing Phase 4. Exact command names may be finalized during implementation, but the proof must cover these categories.

```powershell
dotnet build tools\BlazorShop.AI.StorefrontReverseEngineering\BlazorShop.AI.StorefrontReverseEngineering.csproj
dotnet build tools\BlazorShop.AI.StorefrontBuilder\BlazorShop.AI.StorefrontBuilder.csproj
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\qa\run-storefront-builder-regeneration-gate.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\qa\run-storefront-builder-isolation-gate.ps1 -ProjectRoot artifacts\storefront-builder\generated\BlazorShop.Storefront.GeneratedProof -Name BlazorShop.Storefront.GeneratedProof
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\qa\run-storefront-builder-generated-proof.ps1 -ProofLevel Structure
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\qa\run-storefront-builder-generated-proof.ps1 -ProofLevel FoundationFunctionalFast
```

Add the final handoff-generation proof command after Phase 4.1-4.3 define the concrete script surface, for example:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools\BlazorShop.AI.StorefrontBuilder\build-storefront.ps1 -Mode plan-only -Name Demo -StoreKey sample -HandoffRoot <portable-handoff-root>
powershell -NoProfile -ExecutionPolicy Bypass -File tools\BlazorShop.AI.StorefrontBuilder\build-storefront.ps1 -Mode full -Name Demo -StoreKey sample -HandoffRoot <portable-handoff-root>
```

## Definition Of Done

- [ ] Phase 4 consumes only `analysis/agent-handoff/*` plus registered schemas.
- [ ] Phase 4 fails when final handoff readiness is not passed.
- [ ] Phase 4 has a deterministic handoff-to-generation-plan compiler.
- [ ] Generation plan is reviewable before file writes.
- [ ] Generated project uses `BlazorShop.Storefront.{Name}` naming when promoted.
- [ ] Disposable proof outputs remain under `artifacts/storefront-builder/generated` or `obj/storefront-builder/generated`.
- [ ] Generated project is not added to `BlazorShop.sln` by default.
- [ ] Generated project is created from Starter without mutating Starter.
- [ ] Generated project does not reference Storefront V2.
- [ ] Generated project does not reference backend/core/API projects.
- [ ] Generated files do not declare `@page`.
- [ ] Generated browser code does not call Commerce Node directly.
- [ ] Generated visuals keep Presentation semantic descriptors required for browser actions.
- [ ] Cart, checkout, account, auth, payment result, SEO, BFF, and business behavior remain Presentation/Runtime/Browser-owned.
- [ ] Generated file manifest records ownership, source plan ids, hashes, and provenance.
- [ ] Static validation covers handoff-generated projects.
- [ ] Isolation gate passes.
- [ ] Generated project restores and builds.
- [ ] Browser visual proof passes on desktop and mobile.
- [ ] Regeneration no-op proof passes.
- [ ] Manual-edit conflict proof passes.
- [ ] `-WhatIf` proof passes and report survives candidate cleanup.
- [ ] Docs describe the implemented command surface.
- [ ] Closure evidence records tested SHA and exact local commands.
- [ ] Working tree is clean after final closure commit.

## Deferred After Phase 4

- Pixel-level visual fidelity diff against reference screenshots.
- AI-generated functional JavaScript zones.
- New commerce API capability generation.
- Automated marketplace publishing.
- Production deployment of generated stores.
- GitHub Actions closure evidence while Actions are disabled.

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

- [ ] Confirm current `HEAD` is clean before Phase 4 implementation.
- [ ] Confirm Phase 3E local closure report exists or rerun the local Phase 3E gate if the code changed since the last proof.
- [ ] Confirm `docs/architecture/11-storefront-builder.md` still states that Phase 4 may read only `analysis/agent-handoff/*`.
- [ ] Confirm `BlazorShop.Storefront.Starter/starter-generation.contract.yaml` still declares:
  - [ ] generated project naming convention.
  - [ ] allowed generated zones.
  - [ ] protected zones.
  - [ ] route metadata.
  - [ ] browser action policy.
  - [ ] view slot metadata.
- [ ] Confirm `tools/BlazorShop.AI.StorefrontBuilder/version.json` is still the single generator version source.
- [ ] Confirm `regenerate-storefront.ps1 -WhatIf` still writes a stable report outside the target project.
- [ ] Capture the current list of StorefrontBuilder scripts touched by Phase 4.

Checks:

- [ ] `git status --short`
- [ ] `rg -n "Phase 4 may read only|agent-handoff|StorefrontBuilder generation does not yet consume" docs\architecture docs\visual-reverse-engineering-skill`
- [ ] `rg -n "generatorVersion|WhatIfReportPath|plan-generation-files|apply-composition" tools\BlazorShop.AI.StorefrontBuilder`

Exit criteria:

- [ ] Phase 4 starts from documented current architecture.
- [ ] No hidden requirement to update GitHub Actions.
- [ ] No decision in this phase requires changing Storefront API contracts.

Phase 4.0 intake evidence:

- Initial working tree before Phase 4 implementation contained only this new Phase 4 plan file and the README historical-plan link to it.
- Latest discovered Phase 3E final closure report before Phase 4.0 intake: `obj/storefront-reverse-engineering/reports/phase3e-final-closure-gate-20260802122027.md`, status `passed`, tested SHA `9bdb4d4be4019392360ab08796cf067422aa9597`.
- Current `HEAD` at Phase 4.0 intake was `8e20d68c`; because source commits exist after the Phase 3E report, the Phase 3E gate must be rerun from a clean tree before Phase 4.0 closes.
- The gate rerun requires this plan/README intake to be committed first so the clean-tree check can run honestly.

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

- [ ] Add a handoff input parameter to StorefrontBuilder generation entrypoints, for example `-HandoffRoot <path>`.
- [ ] Support a preflight-only mode without generating files.
- [ ] Validate that `-HandoffRoot` points to an `analysis/agent-handoff` folder or a copied portable package root with the same internal shape.
- [ ] Reject handoff roots that are inside raw source-only artifact folders unless they expose the portable handoff package.
- [ ] Load and validate:
  - [ ] `manifest.json`
  - [ ] `handoff-readiness.json`
  - [ ] `page-compositions.json`
  - [ ] `storefront-pattern.json`
  - [ ] `presentation-catalog.json`
  - [ ] `presentation-mappings.json`
  - [ ] `allowed-files.json`
  - [ ] `protected-files.json`
  - [ ] `design-tokens.json`
  - [ ] `visual-style.json`
  - [ ] `responsive-behavior.json`
  - [ ] `interaction-models.json`
  - [ ] `originality-restrictions.json`
  - [ ] `evidence-manifest.json`
  - [ ] `unresolved-regions.json`
- [ ] Fail when `handoff-readiness.json` is missing or not passed.
- [ ] Fail when manifest readiness disagrees with `handoff-readiness.json`.
- [ ] Fail when schema hashes or artifact hashes drift according to the portable validator.
- [ ] Fail when required schemas are missing.
- [ ] Fail when any consumer dependency points outside `analysis/agent-handoff/*`.
- [ ] Fail when unresolved blocking regions exist.
- [ ] Print problem/cause/fix messages consistent with current StorefrontBuilder error style.
- [ ] Produce a preflight report under `obj/storefront-builder/handoff-preflight/` or the generated artifact's `docs/storefront-analysis/` only after a generated project exists.

Forbidden behavior:

- [ ] Do not read raw `captures/*` as fallback.
- [ ] Do not read source `analysis/pages/*` as fallback.
- [ ] Do not read source `analysis/resolved/*` as fallback.
- [ ] Do not read Storefront V2 source as fallback.
- [ ] Do not infer routes from screenshots.

Tests:

- [ ] Positive: valid copied portable handoff passes preflight.
- [ ] Negative: missing `handoff-readiness.json` fails.
- [ ] Negative: readiness failed fails.
- [ ] Negative: manifest/readiness mismatch fails.
- [ ] Negative: artifact hash drift fails.
- [ ] Negative: missing schema fails.
- [ ] Negative: consumer reference outside handoff fails.
- [ ] Negative: raw capture fallback attempt fails.

Exit criteria:

- [ ] StorefrontBuilder can perform handoff preflight without generating a project.
- [ ] The preflight is portable and does not require the original reverse-engineering source project.

## Phase 4.2 - Handoff-To-Generation-Plan Compiler

Goal: replace the static Phase 4 planning gap with a deterministic compiler from reviewed handoff artifacts into a generation plan.

Implementation targets:

- `tools/BlazorShop.AI.StorefrontBuilder/scripts/generate/plan-generation-files.mjs`
- New module, for example `scripts/generate/handoff-generation-plan.mjs`
- `tools/BlazorShop.AI.StorefrontBuilder/schemas/generation-plan.schema.json`
- Existing `starter-generation.contract.yaml`

Tasks:

- [ ] Add a handoff-aware generation plan mode that accepts the validated handoff package.
- [ ] Keep existing static `plan-only` behavior available for current generated proof until handoff mode is proven.
- [ ] Extend the generation plan schema from broad file list to an explicit contract:
  - [ ] `schemaVersion`
  - [ ] `artifactKind`
  - [ ] `artifactId`
  - [ ] `generatorVersion`
  - [ ] `sourceHandoffPackageHash`
  - [ ] `sourceHandoffReadinessHash`
  - [ ] `sourceStarterContractHash`
  - [ ] `projectName`
  - [ ] `storeKey`
  - [ ] `generationMode`
  - [ ] `files`
  - [ ] `slots`
  - [ ] `assets`
  - [ ] `copyBlocks`
  - [ ] `tokens`
  - [ ] `warnings`
  - [ ] `blockedItems`
- [ ] For each planned file, include:
  - [ ] normalized target path.
  - [ ] ownership classification: generated, managed, protected, user-owned candidate, obsolete candidate.
  - [ ] source handoff artifact references.
  - [ ] source evidence references, only by handoff-local paths.
  - [ ] allowed operation: create, replace, patch, skip, conflict, obsolete.
  - [ ] rationale.
  - [ ] slot id or page id when applicable.
  - [ ] checksum/provenance fields for deterministic regeneration.
- [ ] Map handoff `page-compositions.json` to Presentation view slots.
- [ ] Map handoff `storefront-pattern.json` and Starter contract slots to generated file targets.
- [ ] Map handoff `design-tokens.json` and `visual-style.json` to generated CSS token files.
- [ ] Map handoff `responsive-behavior.json` to responsive layout instructions without generating functional application logic.
- [ ] Map handoff `interaction-models.json` only to visual affordance metadata or allowed semantic descriptors.
- [ ] Map `originality-restrictions.json` to asset/copy reuse policy:
  - [ ] block disallowed copied assets.
  - [ ] mark assets needing replacement.
  - [ ] mark copy that must be rewritten.
  - [ ] preserve evidence references without copying restricted originals.
- [ ] Map `unresolved-regions.json` to blocking or warning plan entries.
- [ ] Ensure cart/checkout/account/auth/payment result plans are visual shell plans only.
- [ ] Ensure no planned generated file targets Starter.
- [ ] Ensure no planned generated file declares route ownership.
- [ ] Sort all arrays and output deterministically.
- [ ] Add readable dry-run output that summarizes create/update/skip/conflict/blocked entries.

Tests:

- [ ] Positive: same handoff package produces byte-stable generation plan across two runs.
- [ ] Positive: valid ecommerce page composition maps to expected Presentation slots.
- [ ] Positive: product gallery, product purchase, product grid, layout, footer, and state pages map to generated-owned zones.
- [ ] Positive: cart/checkout/account map only to shell/template zones.
- [ ] Negative: forbidden target path fails.
- [ ] Negative: protected target path fails.
- [ ] Negative: missing required slot fails.
- [ ] Negative: unsupported interaction requiring business logic fails or becomes manual blocker.
- [ ] Negative: restricted copied asset is blocked or marked replacement-required.
- [ ] Negative: raw evidence path in plan fails validation.

Exit criteria:

- [ ] StorefrontBuilder has a deterministic handoff-driven generation plan.
- [ ] The plan is reviewable before any file writes.
- [ ] The plan contains enough provenance to debug why each generated file exists.

## Phase 4.3 - Starter-Based Handoff Project Generation

Goal: create or update a generated storefront project from Starter using the compiled handoff generation plan.

Implementation targets:

- `tools/BlazorShop.AI.StorefrontBuilder/build-storefront.ps1`
- `tools/BlazorShop.AI.StorefrontBuilder/scripts/generate/new-storefront-project.ps1`
- `tools/BlazorShop.AI.StorefrontBuilder/scripts/generate/apply-composition.mjs`
- Generated artifact docs under `docs/storefront-analysis/`

Tasks:

- [ ] Add a handoff generation mode that starts from the existing Starter copy process.
- [ ] Keep Starter immutable; all project-specific output must go to generated output root or promoted `BlazorShop.Storefront.{Name}` project.
- [ ] Write generation plan into generated project `docs/storefront-analysis/generation-plan.json` or equivalent review artifact.
- [ ] Write a readable `docs/storefront-analysis/handoff-generation-summary.md`.
- [ ] Write metadata linking:
  - [ ] generator version.
  - [ ] Starter contract hash.
  - [ ] Storefront OpenAPI hash.
  - [ ] handoff package hash.
  - [ ] handoff readiness hash.
  - [ ] plan hash.
- [ ] Create visual file placeholders according to the plan before agent implementation.
- [ ] Preserve Presentation package/reference behavior already used by generated proof.
- [ ] Preserve Components/Browser dependency rules already used by Starter.
- [ ] Do not add generated project to `BlazorShop.sln`.
- [ ] Ensure generated project can restore/build before AI-tuned visuals are applied.
- [ ] Ensure missing optional pages/components are represented as explicit warnings, not silent omissions.

Tests:

- [ ] Positive: valid handoff creates a generated project under `artifacts/storefront-builder/generated/{ProjectName}`.
- [ ] Positive: valid handoff can create a disposable project under `obj/storefront-builder/generated/{ProjectName}` for automated proof.
- [ ] Positive: generated project restores and builds before agent visual fill.
- [ ] Negative: unsafe project name fails before files are created.
- [ ] Negative: handoff plan targeting Starter fails.
- [ ] Negative: attempt to add generated project to solution is absent.

Exit criteria:

- [ ] A valid handoff can produce a buildable project skeleton from Starter.
- [ ] The skeleton contains plan and provenance artifacts.
- [ ] No AI implementation is required for this phase to pass.

## Phase 4.4 - Constrained Agent Visual Implementation

Goal: let an agent fill visual-only files from the generation plan while preventing free-form project rewrites.

Implementation targets:

- New StorefrontBuilder agent task package writer.
- Existing generated file manifest tooling.
- Existing ownership/protected-file regeneration logic.

Tasks:

- [ ] Generate an agent task package from the generation plan.
- [ ] Include only these allowed inputs in the task package:
  - [ ] generation plan.
  - [ ] handoff-local evidence references.
  - [ ] approved section screenshots/crops.
  - [ ] design token/style summaries.
  - [ ] Starter/Presentation slot contract summaries.
  - [ ] allowed/protected file manifests.
  - [ ] originality restrictions.
- [ ] Exclude raw captures, source project folders, Storefront V2 source, backend source, and hidden fallback artifacts.
- [ ] Write explicit instructions that generated code may only modify generated-owned files.
- [ ] Allow generated visual outputs for:
  - [ ] layout/header/footer/navigation view slot files.
  - [ ] home/category/search/deals/new releases/content visual templates.
  - [ ] product summary card.
  - [ ] product gallery.
  - [ ] product purchase visual template.
  - [ ] cart visual shell.
  - [ ] checkout visual shell.
  - [ ] account visual shell.
  - [ ] state pages.
  - [ ] CSS tokens/theme files.
  - [ ] generated local assets.
- [ ] Forbid generated outputs for:
  - [ ] route declarations.
  - [ ] BFF endpoints.
  - [ ] HTTP clients.
  - [ ] DTOs.
  - [ ] payment/cart/checkout/account business commands.
  - [ ] authentication flow logic.
  - [ ] SEO route/canonical logic.
  - [ ] server configuration.
  - [ ] appsettings secrets.
  - [ ] Storefront Runtime registration.
- [ ] Require generated components to use existing Presentation descriptors and semantic `data-storefront-*` attributes where applicable.
- [ ] Require copy to be store-owned and localizable later; do not embed backend error copy as final UX.
- [ ] Require image outputs to respect asset originality restrictions.
- [ ] Update generated file manifest after agent writes.
- [ ] Record every agent-written file with source plan entry id and checksum.

Tests:

- [ ] Positive: agent task package contains only handoff-local references.
- [ ] Positive: generated visual files land only in allowed generated zones.
- [ ] Positive: generated file manifest records agent-written files.
- [ ] Negative: attempted `@page` directive fails gate.
- [ ] Negative: attempted `HttpClient`, `fetch`, or direct Commerce Node URL fails gate.
- [ ] Negative: attempted write to Starter fails gate.
- [ ] Negative: attempted write to Presentation/Runtime/Client/Browser/Components packages fails gate.
- [ ] Negative: generated functional JS outside approved visual zone fails gate.

Exit criteria:

- [ ] Agent work is constrained by the plan and file ownership model.
- [ ] Generated visuals can be inspected and regenerated safely.

## Phase 4.5 - Boundary, Manifest, And Ownership Gates

Goal: harden validation so a generated visual project cannot accidentally become an application/runtime fork.

Implementation targets:

- `tools/BlazorShop.AI.StorefrontBuilder/scripts/validate/*`
- `scripts/qa/run-storefront-builder-generated-proof.ps1`
- `scripts/qa/run-storefront-builder-isolation-gate.ps1`
- Generated `docs/storefront-analysis/generated-files.yaml`

Tasks:

- [ ] Extend static validation for handoff-generated projects.
- [ ] Validate the handoff package hash recorded in metadata.
- [ ] Validate generation plan hash recorded in metadata.
- [ ] Validate generated file manifest includes plan entry ids for handoff-generated files.
- [ ] Validate no generated file references raw evidence paths.
- [ ] Validate no generated file references Storefront V2.
- [ ] Validate no generated file references backend/core/API projects.
- [ ] Validate no generated file declares `@page`.
- [ ] Validate no generated browser file calls Commerce Node directly.
- [ ] Validate no generated project references `BlazorShop.Web.SharedV2`.
- [ ] Validate no generated project directly references Runtime or Client unless a documented low-level extension is explicitly approved.
- [ ] Validate generated project consumes Presentation/Components according to current isolation gate.
- [ ] Validate protected files are unchanged unless scope is explicitly `foundation`.
- [ ] Validate manual edits are not overwritten during regeneration.
- [ ] Validate obsolete generated files are reported.
- [ ] Validate `docs/storefront-analysis` records plan/report/proof lineage.

Tests:

- [ ] Add focused tests for every new validator rule.
- [ ] Add fixture generated project with a valid handoff plan.
- [ ] Add fixture generated project with forbidden route declaration.
- [ ] Add fixture generated project with forbidden direct transport.
- [ ] Add fixture generated project with forbidden V2 reference.
- [ ] Add fixture generated project with forbidden raw evidence reference.
- [ ] Add fixture generated project with protected file mutation.

Exit criteria:

- [ ] Static gates can distinguish valid visual generation from boundary leaks.
- [ ] Existing non-handoff generated proof remains green.

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

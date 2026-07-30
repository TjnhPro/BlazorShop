# StorefrontReverseEngineering Phase 3C - Final Handoff Hardening

Status: Proposed  
Owner area: `tools/BlazorShop.AI.StorefrontReverseEngineering`  
Target folder: `docs/visual-reverse-engineering-skill`  
Depends on: Phase 3A evidence foundation and Phase 3B visual analysis/ecommerce mapping  
Primary goal: turn Phase 3B analysis artifacts into a strict, site-level, agent-ready handoff package for later visual generation without changing StorefrontBuilder generation behavior yet.

## Current Codebase Facts

- `BlazorShop.AI.StorefrontReverseEngineering` is a development-time executable under `tools/`.
- Phase 3B currently produces analysis artifacts such as semantic tokens, ecommerce regions, presentation component catalog, presentation mappings, review decisions, Visual Blueprint v1, and generation-readiness reports.
- Phase 3B remains handoff evidence only. StorefrontBuilder generation does not consume ReverseEngineering artifacts yet.
- Existing StorefrontBuilder/Starter boundary rules require generated storefronts to consume Storefront Presentation/Runtime/Components contracts through packages and keep generated visual output inside generated/custom storefront projects.
- Existing docs state generated visual files must not declare `@page`, must not recreate route/BFF/SEO/media application logic, and must not reference Storefront V2 or backend projects.
- `starter-generation.contract.yaml` and `StorefrontFoundationViewSet` are the current Presentation/Starter mapping sources of truth.
- The current Phase 3B Presentation catalog is useful but not complete enough as a final handoff contract. It must be derived from the explicit Starter contract and Presentation foundation slots, not from fragile text patterns only.
- The current Visual Blueprint v1 is still closer to a reviewed analysis index than a final generator/agent contract. It lacks enough page composition, generation-zone, protected-file, and agent task constraints for Phase 4 to consume safely.
- Current workflow still contains single-page assumptions in places such as selecting the first planned page or referencing `captures/home/*`. Phase 3C must move to a site-level multi-page blueprint.
- Current reviewed blueprint and generation readiness should be treated as gating artifacts. A reviewed handoff must not be considered ready when blocking readiness issues remain.

## Why This Phase Exists

Phase 3B proves that the tool can analyze visual evidence and map ecommerce sections to Storefront Presentation concepts. That is not enough for production-grade generation because a generator or AI agent still needs to know:

- Which pages are part of the same storefront site.
- Which visual sections map to which Storefront Presentation slots.
- Which files are allowed to be created or updated.
- Which files, routes, BFF endpoints, action descriptors, and runtime behavior are protected.
- Which visual patterns are unsupported and require human action.
- Which reviewed decisions are final and which are still unresolved.
- Whether the handoff is safe to consume without guessing.

Phase 3C is the final hardening layer between analysis and generation.

## Scope

In scope:

- Structured Storefront pattern contract loading and validation.
- Complete Presentation/Starter catalog extraction for visual handoff.
- Site-level multi-page blueprint support.
- Strict Presentation mapping validation.
- Strict review-decision resolution.
- Agent-ready handoff package generation.
- Generation-zone and protected-file manifest generation.
- Readiness gate that fails the workflow when final handoff is not safe.
- Fixture, mutation, schema, and architecture tests for Phase 3C.
- Documentation updates for operators and future Phase 4 implementers.

Not in scope:

- Generating Razor, CSS, Tailwind, JavaScript, images, or complete storefront projects.
- Wiring StorefrontBuilder to consume Phase 3C output.
- Changing Storefront V2, Storefront Starter, Commerce Node, Control Plane, Runtime, Presentation, or Components runtime behavior.
- Adding AI image generation or visual imitation logic.
- Changing ecommerce business behavior such as cart, checkout, payment, catalog, or account flows.
- Copying reference-site assets, brand copy, logo, product images, or licensed design details into production output.

## Architecture Target

```text
Reference ecommerce site
  -> Phase 3A browser evidence
  -> Phase 3B visual analysis and ecommerce mapping
  -> Phase 3C strict reviewed handoff package
  -> Later Phase 4 visual generator or human/AI implementation agent

Starter / Presentation / Components contracts
  -> Phase 3C Storefront pattern catalog
  -> allowed slots, behavior boundaries, protected files, generated zones
  -> later generated Storefront.{Name} visual implementation
```

Phase 3C must keep this boundary:

```text
StorefrontReverseEngineering
  produces analysis and handoff artifacts only

StorefrontBuilder
  remains unchanged until an approved Phase 4 cutover
```

## Artifact Target

Create these artifacts only when the corresponding phase lands:

```text
analysis/
  storefront-pattern/
    storefront-pattern.json
    page-contracts.json
    behavior-boundaries.json
    generation-zones.json
  resolved/
    semantic-tokens.reviewed.json
    page-compositions.reviewed.json
    presentation-mappings.reviewed.json
    ecommerce-regions.reviewed.json
    unsupported-pattern-decisions.json
  agent-handoff/
    manifest.json
    task.md
    visual-blueprint.json
    storefront-pattern.json
    page-compositions.json
    visual-style.json
    design-tokens.json
    allowed-files.json
    protected-files.json
    unresolved-regions.json
    generation-readiness.json
reports/
  generation-readiness.json
  generation-readiness.md
  agent-handoff-readiness.json
  agent-handoff-readiness.md
```

Schema files:

```text
Schemas/
  storefront-pattern.schema.json
  page-contracts.schema.json
  behavior-boundaries.schema.json
  generation-zones.schema.json
  reviewed-semantic-tokens.schema.json
  reviewed-page-compositions.schema.json
  reviewed-presentation-mappings.schema.json
  reviewed-ecommerce-regions.schema.json
  unsupported-pattern-decisions.schema.json
  agent-handoff-manifest.schema.json
  allowed-files.schema.json
  protected-files.schema.json
  unresolved-regions.schema.json
  agent-handoff-readiness.schema.json
```

## Phase 3C.0 - Baseline And Boundary Lock

Goal: prove the current Phase 3B baseline before changing handoff behavior.

Implementation checklist:

- [x] Record current git SHA in the Phase 3C closure report.
- [x] Run the existing ReverseEngineering test project before implementation.
- [x] Run the existing Phase 3B gate before implementation, or document local-only blockers.
- [x] Confirm StorefrontBuilder still does not consume `analysis/visual-blueprint.v1.*.json`.
- [x] Confirm ReverseEngineering still does not reference production Storefront V2, Commerce Node, Control Plane, Runtime, Presentation, or Components projects.
- [x] Confirm no Phase 3C code creates generated storefront projects.
- [x] Confirm no Phase 3C code writes into `BlazorShop.Storefront.Starter`.
- [x] Capture known current gaps in `docs/qa/phase3c-final-handoff-closure.md`.

Files likely touched:

- [x] `docs/qa/phase3c-final-handoff-closure.md`
- [x] `scripts/qa/run-storefront-reverse-engineering-phase3c-final-handoff-gate.ps1`
- [x] `tools/BlazorShop.AI.StorefrontReverseEngineering/tests`

Tests:

- [x] Existing ReverseEngineering tests pass.
- [x] Existing Phase 3B gate passes or skip reason is documented.
- [x] Boundary scan proves StorefrontBuilder generation remains unchanged.

Done when:

- [x] Phase 3C starts from a known green or explicitly documented baseline.

## Phase 3C.1 - Structured Storefront Pattern Contract

Goal: replace fragile Starter/Presentation contract discovery with a typed, validated Storefront pattern model.

Implementation checklist:

- [x] Add contract models under `Analysis/StorefrontPattern`:
  - [x] `StorefrontPatternContract`
  - [x] `StorefrontPatternMetadata`
  - [x] `StorefrontGenerationZones`
  - [x] `StorefrontBehaviorBoundary`
  - [x] `StorefrontPageContract`
  - [x] `StorefrontRouteContract`
  - [x] `StorefrontSlotContract`
  - [x] `StorefrontActionContract`
  - [x] `StorefrontProtectedFileContract`
  - [x] `StorefrontGeneratedFileContract`
- [x] Parse `starter-generation.contract.yaml` with a structured parser or a minimal scoped parser that understands indentation and sections.
- [x] Stop treating every `- id:` line as a slot.
- [x] Load and validate:
  - [x] contract version.
  - [x] starter template version.
  - [x] package version metadata.
  - [x] generated project ownership.
  - [x] managed zones.
  - [x] generated zones.
  - [x] protected zones.
  - [x] asset zones.
  - [x] analysis artifact zones.
  - [x] browser action policy.
  - [x] same-origin BFF action rules.
  - [x] action descriptors.
  - [x] route ownership rules.
  - [x] slot ownership rules.
  - [x] file overwrite policy.
- [x] Add page contracts for:
  - [x] home page.
  - [x] category/listing page.
  - [x] search results page.
  - [x] product detail page.
  - [x] cart visual shell.
  - [x] checkout visual shell.
  - [x] account/auth visual shell.
  - [x] content page.
  - [x] maintenance page.
  - [x] not-found page.
  - [x] service-unavailable page.
  - [x] error state.
- [x] Each page contract must declare:
  - [x] stable page archetype.
  - [x] route ownership.
  - [x] allowed visual slots.
  - [x] required visual regions.
  - [x] optional visual regions.
  - [x] prohibited behavior.
  - [x] protected action descriptors.
  - [x] target generated path rules.
  - [x] supported responsive zones.
- [x] Write `analysis/storefront-pattern/storefront-pattern.json`.
- [x] Write `analysis/storefront-pattern/page-contracts.json`.
- [x] Write `analysis/storefront-pattern/behavior-boundaries.json`.
- [x] Write `analysis/storefront-pattern/generation-zones.json`.
- [x] Add JSON schemas for all four artifacts.

Validation checklist:

- [x] Duplicate slot IDs are blocking.
- [x] Duplicate page IDs are blocking.
- [x] Unknown generated zones are blocking.
- [x] Generated paths outside allowed zones are blocking.
- [x] Protected paths in generated zones are blocking.
- [x] Browser action endpoints that are not same-origin BFF routes are blocking.
- [x] Storefront API direct browser URLs are blocking.
- [x] Missing required page contract fields are blocking.
- [x] Unknown optional metadata remains allowed only when explicitly captured in `extensions`.

Tests:

- [x] Valid Starter contract loads into a typed pattern contract.
- [x] `- id:` under a non-slot section is not interpreted as a slot.
- [x] Duplicate slot IDs fail validation.
- [x] Protected path collision fails validation.
- [x] Same-origin BFF action policy is preserved.
- [x] Schema validation passes for generated artifacts.

Done when:

- [x] Phase 3C has a typed Storefront pattern artifact that can be consumed without reading Starter YAML ad hoc.

## Phase 3C.2 - Complete Presentation Catalog Coverage

Goal: build a complete Presentation/Starter catalog that represents all required foundation slots and visual targets.

Implementation checklist:

- [ ] Replace regex-only `required Type` discovery for foundation slots.
- [ ] Extract required slots from the authoritative Presentation foundation model, including slots returned by `StorefrontFoundationViewSet.GetRequiredSlots()`.
- [ ] Include every required foundation slot in the catalog:
  - [ ] `ApplicationHead`
  - [ ] `VisualScripts`
  - [ ] `MainLayout`
  - [ ] `ConsentBanner`
  - [ ] `HomePage`
  - [ ] `CategoryPage`
  - [ ] `ProductPage`
  - [ ] `SearchPage`
  - [ ] `DealsPage`
  - [ ] `NewReleasesPage`
  - [ ] `ContentPage`
  - [ ] `CartPage`
  - [ ] `CheckoutPage`
  - [ ] `PaymentResultPage`
  - [ ] `AuthPage`
  - [ ] `AccountPage`
  - [ ] `MaintenanceState`
  - [ ] `NotFoundState`
  - [ ] `ServiceUnavailableState`
  - [ ] `ErrorState`
- [ ] Split catalog entry intent into explicit categories:
  - [ ] visual generation target.
  - [ ] foundation view slot.
  - [ ] starter visual slot.
  - [ ] presentation action binding.
  - [ ] component data contract.
  - [ ] headless behavior contract.
  - [ ] runtime-owned behavior.
- [ ] Include capability ownership metadata:
  - [ ] visual-only.
  - [ ] browser-safe action.
  - [ ] BFF-owned behavior.
  - [ ] Presentation-owned routing/SEO/media behavior.
  - [ ] Runtime-owned Commerce Node transport behavior.
- [ ] Include allowed file patterns for generated visual implementation.
- [ ] Include protected file patterns for contracts, BFF, Runtime, generated client, package metadata, and shared application logic.
- [ ] Include required evidence types for each slot.
- [ ] Include acceptable fallback behavior when a reference page does not contain a region.
- [ ] Write updated `presentation-component-catalog` schema with semantic categories.

Tests:

- [ ] Catalog includes all required foundation slots.
- [ ] Catalog fails if `StorefrontFoundationViewSet.GetRequiredSlots()` gains a new slot not represented in the generated catalog.
- [ ] Catalog fails if Starter contract declares a slot that is not mapped to a known category or explicit extension.
- [ ] Visual-only slots do not claim BFF/runtime ownership.
- [ ] Runtime-owned behavior cannot be targeted for visual generation.

Done when:

- [ ] A future Phase 4 generator can inspect the catalog and know exactly which visual slots exist, which behavior belongs elsewhere, and which files are protected.

## Phase 3C.3 - Site-Level Multi-Page Blueprint

Goal: remove remaining single-page assumptions and model the reference as one storefront site with multiple page archetypes.

Implementation checklist:

- [ ] Replace `plan.Pages.First()` assumptions with iteration over all capture-plan pages.
- [ ] Replace hardcoded `captures/home/*` analysis input with page-aware paths.
- [ ] Add `SiteBlueprint` or equivalent model:
  - [ ] site ID.
  - [ ] source URL set.
  - [ ] store archetype summary.
  - [ ] shared visual language.
  - [ ] shared layout system.
  - [ ] shared responsive rules.
  - [ ] page collection.
  - [ ] unresolved site-level issues.
- [ ] Add `PageBlueprint` model:
  - [ ] page ID.
  - [ ] page archetype.
  - [ ] source URL.
  - [ ] capture artifact paths.
  - [ ] viewport coverage.
  - [ ] page-level ecommerce regions.
  - [ ] page-level presentation mappings.
  - [ ] page-level composition tree.
  - [ ] target view slot.
  - [ ] target generated file path.
  - [ ] unsupported or blocked regions.
- [ ] Aggregate shared tokens across all pages.
- [ ] Preserve page-specific token overrides.
- [ ] Detect inconsistent navigation/header/footer patterns across pages.
- [ ] Detect page archetype drift when a URL's visual structure does not match its declared role.
- [ ] Support a single fixture project containing home, category/listing, and product detail pages.
- [ ] Keep unsupported fixture pages as explicit negative cases, not as separate happy-path projects.

Output artifacts:

- [ ] `analysis/resolved/page-compositions.reviewed.json`
- [ ] updated `analysis/visual-blueprint.v1.draft.json`
- [ ] updated `analysis/visual-blueprint.v1.reviewed.json`
- [ ] page-aware `reports/generation-readiness.json`

Tests:

- [ ] One project with home, listing, and product pages produces one site-level blueprint.
- [ ] Missing evidence for one required page creates a page-scoped blocker.
- [ ] Shared header/footer tokens are deduplicated at site level.
- [ ] Page-specific product detail composition remains page-scoped.
- [ ] No artifact path is hardcoded to `captures/home`.

Done when:

- [ ] Phase 3C output describes a full storefront site, not only the first captured page.

## Phase 3C.4 - Strict Presentation Mapping Validation

Goal: make mappings reliable enough that an agent can use them without inventing ecommerce behavior.

Matching strategy:

1. Reviewed human override with matching source artifact version.
2. Exact preferred Presentation/Starter slot ID.
3. Exact ecommerce role plus compatible page archetype.
4. Compatible visual target and generated zone.
5. Alternative mapping requiring human review.
6. No-match with explicit unresolved reason.

Implementation checklist:

- [ ] Extend mapping contracts with:
  - [ ] source candidate ID.
  - [ ] source page ID.
  - [ ] source section ID.
  - [ ] ecommerce region ID.
  - [ ] page archetype.
  - [ ] Presentation target ID.
  - [ ] Starter slot ID.
  - [ ] target generated path.
  - [ ] generated zone.
  - [ ] route ownership.
  - [ ] variant.
  - [ ] slot assignments.
  - [ ] token bindings.
  - [ ] responsive bindings.
  - [ ] interaction bindings.
  - [ ] data requirements.
  - [ ] behavior ownership.
  - [ ] evidence IDs.
  - [ ] reason codes.
  - [ ] confidence.
  - [ ] review state.
- [ ] Validate page archetype compatibility.
- [ ] Validate ecommerce role compatibility.
- [ ] Validate target path is inside an allowed generated zone.
- [ ] Validate target path does not overlap a protected file or protected folder.
- [ ] Validate required child slots are present.
- [ ] Validate unsupported critical interactions are not silently dropped.
- [ ] Validate action descriptors are preserved and not rewritten.
- [ ] Validate no mapping asks generated visual code to call Commerce Node directly.
- [ ] Validate runtime-owned behavior is not assigned to generated visual code.
- [ ] Validate low-confidence mappings require review.
- [ ] Validate ambiguous mappings require review.
- [ ] Validate a rejected mapping is excluded from the reviewed handoff.

Tests:

- [ ] Preferred ID mapping succeeds.
- [ ] Role-only mapping succeeds only when page archetype and target zone match.
- [ ] Ambiguous role mapping becomes human-review-required.
- [ ] Protected path mapping fails.
- [ ] Browser direct Storefront API mapping fails.
- [ ] Runtime-owned behavior mapping fails.
- [ ] Rejected mapping is not emitted into agent handoff.

Done when:

- [ ] Every mapping in the reviewed handoff is either approved, safely derived, or explicitly blocked.

## Phase 3C.5 - Strict Review Resolution

Goal: make human review decisions deterministic, auditable, and impossible to apply stale or incomplete decisions silently.

Implementation checklist:

- [ ] Define review decision states:
  - [ ] `Approved`
  - [ ] `Modified`
  - [ ] `Rejected`
  - [ ] `Deferred`
- [ ] Require `modifiedValue` for `Modified`.
- [ ] Require `reason` for `Rejected` and `Deferred`.
- [ ] Require reviewer metadata:
  - [ ] reviewer.
  - [ ] reviewed UTC.
  - [ ] source artifact ID.
  - [ ] source artifact hash.
  - [ ] decision ID.
- [ ] Reject unknown decision targets.
- [ ] Reject duplicate decisions for the same target unless superseded explicitly.
- [ ] Reject stale decisions when source artifact hash changed.
- [ ] Preserve original proposed value beside reviewed value.
- [ ] Write resolved artifacts only from draft plus valid review decisions.
- [ ] Keep draft artifacts immutable.
- [ ] Mark unresolved blocking review items in generation readiness.
- [ ] Do not write `visual-blueprint.v1.reviewed.json` as ready when blocking review items remain.

Resolved artifacts:

- [ ] `analysis/resolved/semantic-tokens.reviewed.json`
- [ ] `analysis/resolved/page-compositions.reviewed.json`
- [ ] `analysis/resolved/presentation-mappings.reviewed.json`
- [ ] `analysis/resolved/ecommerce-regions.reviewed.json`
- [ ] `analysis/resolved/unsupported-pattern-decisions.json`

Tests:

- [ ] Approved decision copies draft value.
- [ ] Modified decision writes reviewed value and preserves original.
- [ ] Rejected decision removes the target from ready handoff and records blocker or explicit exclusion.
- [ ] Deferred critical decision blocks readiness.
- [ ] Stale decision is rejected.
- [ ] Duplicate decision is rejected.
- [ ] Unknown target decision is rejected.

Done when:

- [ ] Reviewed artifacts are a deterministic product of draft artifacts plus valid review decisions.

## Phase 3C.6 - Agent-Ready Page Composition And Section Evidence

Goal: give a future generator enough reviewed composition detail to create files without reinterpreting raw screenshots.

Implementation checklist:

- [ ] Add `PageComposition` model:
  - [ ] page ID.
  - [ ] page archetype.
  - [ ] target view slot.
  - [ ] section tree.
  - [ ] layout zones.
  - [ ] repeated group definitions.
  - [ ] responsive transformation rules.
  - [ ] source evidence links.
  - [ ] unresolved issues.
- [ ] Add `PageSection` model:
  - [ ] section ID.
  - [ ] stable fingerprint.
  - [ ] semantic role.
  - [ ] ecommerce role.
  - [ ] parent section ID.
  - [ ] child section IDs.
  - [ ] viewport bounding boxes.
  - [ ] visual style token references.
  - [ ] component mapping reference.
  - [ ] target file path.
  - [ ] target generated zone.
  - [ ] allowed operations.
  - [ ] protected behavior markers.
- [ ] Add section screenshot or crop references when available.
- [ ] Link every section to source DOM/style/screenshot evidence IDs.
- [ ] Record repeated component patterns such as product card grids, menu branches, thumbnail rails, badges, footer columns, and promotion bands.
- [ ] Record empty, loading, error, disabled, and unavailable state expectations when evidence exists.
- [ ] Preserve mobile/tablet/desktop differences without encoding viewport-width CSS.
- [ ] Flag sections that require visual generation but lack sufficient evidence.
- [ ] Flag sections that require ecommerce behavior but have no Presentation/Starter binding.

Tests:

- [ ] Page composition tree is stable across deterministic fixture runs.
- [ ] Repeated product-card section is grouped instead of emitted as unrelated one-off sections.
- [ ] Missing section evidence blocks readiness for required regions.
- [ ] Optional section missing from reference does not block readiness.
- [ ] A section cannot target a protected path.

Done when:

- [ ] Page compositions are specific enough for an implementation agent to generate visual files without rereading raw capture artifacts.

## Phase 3C.7 - Constrained Agent Handoff Package

Goal: create the final file bundle that Phase 4 or a human/AI implementation agent can consume safely.

Implementation checklist:

- [ ] Add an `agent-handoff` assembler.
- [ ] Write `analysis/agent-handoff/manifest.json` with:
  - [ ] project ID.
  - [ ] source project path.
  - [ ] source run ID.
  - [ ] source commit SHA when available.
  - [ ] generated UTC.
  - [ ] handoff schema version.
  - [ ] readiness status.
  - [ ] artifact list.
  - [ ] required consumer contract.
  - [ ] unsupported pattern summary.
- [ ] Write `analysis/agent-handoff/task.md` with:
  - [ ] concrete implementation objective.
  - [ ] allowed file areas.
  - [ ] protected file areas.
  - [ ] required page list.
  - [ ] required visual slots.
  - [ ] required responsive states.
  - [ ] BFF/action constraints.
  - [ ] no-copy and asset originality constraints.
  - [ ] no-StorefrontBuilder-consumption warning until Phase 4.
  - [ ] QA commands expected after generation.
- [ ] Write `analysis/agent-handoff/allowed-files.json`.
- [ ] Write `analysis/agent-handoff/protected-files.json`.
- [ ] Write `analysis/agent-handoff/page-compositions.json`.
- [ ] Write `analysis/agent-handoff/visual-style.json`.
- [ ] Write `analysis/agent-handoff/design-tokens.json`.
- [ ] Write `analysis/agent-handoff/storefront-pattern.json`.
- [ ] Write `analysis/agent-handoff/visual-blueprint.json`.
- [ ] Write `analysis/agent-handoff/unresolved-regions.json`.
- [ ] Write `analysis/agent-handoff/generation-readiness.json`.
- [ ] Do not include raw reference images or original brand assets unless they are explicitly classified as allowed reference-only evidence.
- [ ] Include screenshots and crops only as evidence references, not as assets to copy.
- [ ] Make the handoff stable under deterministic reruns.

Protected file examples:

- [ ] generated client files.
- [ ] Runtime transport files.
- [ ] Presentation BFF endpoints.
- [ ] Presentation route assemblies.
- [ ] package version props.
- [ ] Starter generation contract.
- [ ] Storefront V2 files.
- [ ] Commerce Node and Control Plane files.
- [ ] shared ecommerce business DTOs outside allowed contracts.

Allowed file examples for future generated storefronts:

- [ ] project-local visual Razor files.
- [ ] project-local CSS.
- [ ] project-local static assets.
- [ ] project-local view registrations.
- [ ] project-local copy/localization resources.
- [ ] project-local visual configuration.

Tests:

- [ ] Handoff manifest lists every required artifact.
- [ ] Handoff package is deterministic across two runs.
- [ ] Protected file manifest blocks V2/backend/Runtime/Presentation transport targets.
- [ ] Allowed file manifest permits only generated storefront visual areas.
- [ ] `task.md` includes enough context to implement without reading raw Phase 3B internals.
- [ ] Unresolved critical regions block readiness.

Done when:

- [ ] Phase 4 can be designed to read only `analysis/agent-handoff/*` plus schemas, not raw evidence folders.

## Phase 3C.8 - Strict Readiness And Workflow Failure

Goal: make readiness enforceable instead of informational.

Implementation checklist:

- [ ] Add final handoff readiness validator.
- [ ] Validate evidence prerequisites:
  - [ ] Phase 3A readiness passed.
  - [ ] evidence snapshot exists.
  - [ ] required screenshots and element evidence exist.
  - [ ] correlation IDs still match.
- [ ] Validate Phase 3B prerequisites:
  - [ ] semantic tokens exist.
  - [ ] ecommerce regions exist.
  - [ ] Presentation catalog exists.
  - [ ] presentation mappings exist.
  - [ ] review queue was evaluated.
  - [ ] Visual Blueprint v1 draft exists.
- [ ] Validate Phase 3C prerequisites:
  - [ ] Storefront pattern contract exists.
  - [ ] complete page contracts exist.
  - [ ] reviewed page compositions exist.
  - [ ] reviewed mappings exist.
  - [ ] agent handoff manifest exists.
  - [ ] allowed/protected file manifests exist.
  - [ ] unresolved regions are classified.
- [ ] Fail readiness for blocking codes:
  - [ ] `missing-required-page`
  - [ ] `missing-required-foundation-slot`
  - [ ] `missing-visualscripts-slot`
  - [ ] `unresolved-critical-region`
  - [ ] `ambiguous-presentation-mapping`
  - [ ] `protected-path-target`
  - [ ] `unsafe-browser-action`
  - [ ] `runtime-behavior-assigned-to-visual-code`
  - [ ] `stale-review-decision`
  - [ ] `schema-validation-failed`
  - [ ] `single-page-hardcode-detected`
  - [ ] `missing-agent-handoff-artifact`
- [ ] Keep warnings for non-blocking issues:
  - [ ] optional section missing.
  - [ ] low-confidence visual token.
  - [ ] unavailable reference asset.
  - [ ] page-specific style conflict.
- [ ] Change workflow behavior so final handoff steps return failure when readiness has blockers.
- [ ] Change CLI behavior so final handoff validation exits non-zero when blockers exist.
- [ ] Update `inspect` to show:
  - [ ] final handoff readiness.
  - [ ] blocker count.
  - [ ] warning count.
  - [ ] latest blocker.
  - [ ] agent handoff path.
  - [ ] reviewed artifact paths.
  - [ ] next recommended command.

Tests:

- [ ] Readiness passes for a complete reviewed multi-page fixture.
- [ ] Readiness fails for missing agent handoff manifest.
- [ ] Readiness fails for protected path target.
- [ ] Readiness fails for unsafe browser action.
- [ ] Readiness fails for stale review decision.
- [ ] Workflow step fails when readiness fails.
- [ ] CLI exits non-zero when final handoff validation fails.
- [ ] `inspect` reports final handoff readiness accurately.

Done when:

- [ ] A failed final handoff cannot be mistaken for a safe generation input.

## Phase 3C.9 - Fixtures, Mutation Tests, And Closure Gate

Goal: prove Phase 3C behavior with realistic ecommerce browser fixtures and adversarial malformed artifacts.

Implementation checklist:

- [ ] Add a site-level fixture with at least:
  - [ ] home page.
  - [ ] category/listing page.
  - [ ] product detail page with 1:1 gallery.
  - [ ] cart shell visual region.
  - [ ] checkout shell visual region.
  - [ ] account/auth shell visual region.
  - [ ] content/system state sample.
- [ ] Keep unsupported pattern fixtures:
  - [ ] direct API mutation from browser.
  - [ ] checkout/payment behavior embedded in visual script.
  - [ ] protected file target.
  - [ ] ambiguous ecommerce region.
  - [ ] missing required page.
  - [ ] stale review decision.
- [ ] Add mutation tests for:
  - [ ] removing one required slot.
  - [ ] adding unknown page archetype.
  - [ ] changing source artifact hash after review.
  - [ ] adding `captures/home` hardcode back.
  - [ ] making generator target Storefront V2.
  - [ ] assigning Runtime-owned behavior to visual component.
  - [ ] creating duplicate mapping decisions.
- [ ] Add `scripts/qa/run-storefront-reverse-engineering-phase3c-final-handoff-gate.ps1`.
- [ ] Gate should run:
  - [ ] `dotnet build tools/BlazorShop.AI.StorefrontReverseEngineering/BlazorShop.AI.StorefrontReverseEngineering.csproj`
  - [ ] `dotnet test tools/BlazorShop.AI.StorefrontReverseEngineering/tests/BlazorShop.AI.StorefrontReverseEngineering.Tests/BlazorShop.AI.StorefrontReverseEngineering.Tests.csproj --blame-hang-timeout 5m`
  - [ ] fixture run for complete multi-page handoff.
  - [ ] fixture run for unsupported pattern blockers.
  - [ ] schema validation for all Phase 3C artifacts.
  - [ ] boundary scan for StorefrontBuilder non-consumption.
  - [ ] boundary scan for no production references to ReverseEngineering.
  - [ ] scan for hardcoded `captures/home` in workflow code.
  - [ ] scan for `plan.Pages.First()` in workflow code.
- [ ] Gate writes reports under `obj/storefront-reverse-engineering/reports/`.
- [ ] Track closure summary under `docs/qa/phase3c-final-handoff-closure.md`.

Tests:

- [ ] Positive fixture passes full Phase 3C gate.
- [ ] Each negative fixture fails with the expected blocking code.
- [ ] Gate report includes command, commit SHA, passed/failed status, artifact paths, and next action.

Done when:

- [ ] Phase 3C has a repeatable local gate that proves final handoff quality without requiring external websites.

## Phase 3C.10 - Documentation And Phase 4 Consumption Contract

Goal: document the final handoff contract so Phase 4 can be implemented without reopening Phase 3C decisions.

Implementation checklist:

- [ ] Update `docs/visual-reverse-engineering-skill/README.md`.
- [ ] Update `docs/visual-reverse-engineering-skill/reference.md`.
- [ ] Update `docs/visual-reverse-engineering-skill/how-to-generate-and-validate.md` only to clarify that generation still does not consume Phase 3C output.
- [ ] Update `docs/visual-reverse-engineering-skill/explanation-boundaries-and-regeneration.md`.
- [ ] Update `docs/architecture/11-storefront-builder.md` if artifact ownership or handoff boundaries change.
- [ ] Update `docs/agents/storefront-builder.md`.
- [ ] Add a short Phase 4 contract section:
  - [ ] Phase 4 may read only `analysis/agent-handoff/*` and schemas as input.
  - [ ] Phase 4 must not reinterpret raw reference evidence unless explicitly running a new ReverseEngineering pass.
  - [ ] Phase 4 must not write into Starter.
  - [ ] Phase 4 must not modify StorefrontBuilder generation until a separate implementation plan is approved.
  - [ ] Phase 4 must fail if `agent-handoff-readiness` is not passed.
- [ ] Document operator commands:
  - [ ] run Phase 3C full flow.
  - [ ] inspect handoff readiness.
  - [ ] apply or reject review decisions.
  - [ ] rerun final handoff validation.
  - [ ] run the Phase 3C gate.
- [ ] Document artifact interpretation:
  - [ ] which artifact is human-readable.
  - [ ] which artifact is machine-readable.
  - [ ] which artifact is source-of-truth for generation.
  - [ ] which artifact is evidence-only.

Done when:

- [ ] A new agent can read docs and know exactly how to produce and validate a final handoff, and exactly what not to consume yet.

## Implementation Order

Recommended commit order:

1. Phase 3C.0 baseline and gate shell.
2. Phase 3C.1 typed Storefront pattern contract.
3. Phase 3C.2 complete Presentation catalog coverage.
4. Phase 3C.3 site-level multi-page blueprint.
5. Phase 3C.4 strict Presentation mapping validation.
6. Phase 3C.5 strict review resolution.
7. Phase 3C.6 page composition and section evidence.
8. Phase 3C.7 agent handoff package.
9. Phase 3C.8 readiness and workflow failure.
10. Phase 3C.9 fixtures and final gate.
11. Phase 3C.10 documentation and closure evidence.

Do not combine phases 3C.1 through 3C.8 into one large implementation commit. The blast radius is high because contracts, workflow, schemas, and readiness are coupled.

## Compatibility Rules

- [ ] Do not modify StorefrontBuilder generation behavior.
- [ ] Do not make StorefrontBuilder consume `analysis/agent-handoff/*` in this phase.
- [ ] Do not add ReverseEngineering projects to production runtime dependency graphs.
- [ ] Do not add references from Storefront V2, Starter, Runtime, Presentation, Components, Commerce Node, or Control Plane to ReverseEngineering.
- [ ] Do not write generated visual output into Starter.
- [ ] Do not generate `@page` route files.
- [ ] Do not let generated visual targets own route/BFF/SEO/media behavior.
- [ ] Do not let browser code call Commerce Node Storefront API directly.
- [ ] Do not classify copied reference assets as production-safe without explicit human review metadata.
- [ ] Keep all Phase 3C artifacts under the ReverseEngineering project output root.

## Test Matrix

Unit tests:

- [ ] Storefront pattern parser.
- [ ] Storefront pattern validator.
- [ ] Presentation catalog builder.
- [ ] Presentation catalog drift detector.
- [ ] Multi-page blueprint assembler.
- [ ] Page composition builder.
- [ ] Presentation mapping validator.
- [ ] Review decision resolver.
- [ ] Agent handoff assembler.
- [ ] Final readiness validator.
- [ ] Inspect output model.

Schema tests:

- [ ] `storefront-pattern.schema.json`
- [ ] `page-contracts.schema.json`
- [ ] `behavior-boundaries.schema.json`
- [ ] `generation-zones.schema.json`
- [ ] `reviewed-semantic-tokens.schema.json`
- [ ] `reviewed-page-compositions.schema.json`
- [ ] `reviewed-presentation-mappings.schema.json`
- [ ] `reviewed-ecommerce-regions.schema.json`
- [ ] `unsupported-pattern-decisions.schema.json`
- [ ] `agent-handoff-manifest.schema.json`
- [ ] `allowed-files.schema.json`
- [ ] `protected-files.schema.json`
- [ ] `unresolved-regions.schema.json`
- [ ] `agent-handoff-readiness.schema.json`

Workflow tests:

- [ ] Full fixture run writes all Phase 3C artifacts.
- [ ] `resume --force-step` works for each new Phase 3C step.
- [ ] Final handoff validation fails with blockers.
- [ ] Final handoff validation passes only when required review decisions are resolved.
- [ ] `inspect` reports Phase 3C readiness and artifact paths.

Boundary tests:

- [ ] StorefrontBuilder does not read Phase 3C artifacts.
- [ ] ReverseEngineering does not reference production runtime projects.
- [ ] Generated artifact paths are not written to Starter.
- [ ] Protected path targets are rejected.
- [ ] Browser action policy rejects direct Commerce Node calls.

Fixture tests:

- [ ] Complete multi-page ecommerce fixture passes.
- [ ] Unsupported interaction fixture fails.
- [ ] Missing page fixture fails.
- [ ] Missing required slot fixture fails.
- [ ] Stale review fixture fails.
- [ ] Ambiguous mapping fixture requires review.

## Risk Register

- Risk: Phase 3C becomes a hidden generator.
  - Mitigation: Gate scans prove StorefrontBuilder does not consume handoff artifacts and no Razor/CSS generation happens.

- Risk: Typed Storefront pattern parsing drifts from Starter contract.
  - Mitigation: Catalog drift tests compare parsed contract, foundation slot source, and emitted catalog.

- Risk: Multi-page support causes a large workflow rewrite.
  - Mitigation: First add page-aware artifact paths behind existing workflow steps, then move single-page code to collection-based models.

- Risk: Review decisions become stale after evidence changes.
  - Mitigation: Require source artifact IDs and hashes on every decision.

- Risk: Agents still edit protected behavior files in Phase 4.
  - Mitigation: Handoff includes machine-readable allowed/protected manifests and task text with explicit constraints.

- Risk: Readiness blocks too aggressively.
  - Mitigation: Use explicit blocker/warning/info severity and fixture tests for expected optional-missing cases.

## Definition Of Done

Phase 3C can close only when:

- [ ] Storefront pattern contract is typed and schema-validated.
- [ ] Presentation catalog includes every required foundation slot, including `VisualScripts` and system states.
- [ ] Phase 3C supports a single multi-page site blueprint.
- [ ] No workflow code depends on `captures/home` or `plan.Pages.First()` for final analysis.
- [ ] Presentation mappings include page, section, target path, generated zone, behavior ownership, confidence, and review state.
- [ ] Review decisions are deterministic, hash-bound, and stale-safe.
- [ ] Reviewed artifacts are generated only from draft artifacts plus valid review decisions.
- [ ] Final handoff package exists under `analysis/agent-handoff/`.
- [ ] Final readiness fails the workflow and CLI when blockers exist.
- [ ] `inspect` reports final handoff readiness and artifact paths.
- [ ] Positive and negative ecommerce fixtures cover the final handoff contract.
- [ ] Phase 3C gate passes locally.
- [ ] Docs explain that StorefrontBuilder still does not consume Phase 3C artifacts until a later approved phase.
- [ ] Closure evidence is recorded in `docs/qa/phase3c-final-handoff-closure.md`.

## Autoplan Decision Audit

- Decision: name the remaining work Phase 3C instead of reopening Phase 3B.
  - Reason: Phase 3B analysis can remain valid while Phase 3C hardens the consumption boundary.

- Decision: produce `analysis/agent-handoff/*` as the only future generator input.
  - Reason: Phase 4 must not inspect raw screenshots, raw DOM snapshots, or partial Phase 3B internals to infer contract behavior.

- Decision: keep StorefrontBuilder generation unchanged in this phase.
  - Reason: this phase closes handoff quality first; consumption belongs to a later approved cutover.

- Decision: move to site-level multi-page blueprint before generation.
  - Reason: ecommerce storefronts need home, listing, product, cart/account/checkout shells, and system states to preserve layout consistency.

- Decision: treat protected files and behavior ownership as first-class artifacts.
  - Reason: visual generation must not rewrite BFF, routing, Runtime transport, payment/cart/checkout behavior, or shared contracts.

- Decision: final readiness must fail the workflow when blocked.
  - Reason: a reviewed blueprint that still contains blockers is not safe input for a generator or AI implementation agent.

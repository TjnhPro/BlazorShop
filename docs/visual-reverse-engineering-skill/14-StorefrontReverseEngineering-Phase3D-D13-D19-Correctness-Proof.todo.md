# StorefrontReverseEngineering Phase 3D - D13-D19 Correctness Proof

Status: Complete
Owner area: `tools/BlazorShop.AI.StorefrontReverseEngineering`
Target folder: `docs/visual-reverse-engineering-skill`
Depends on: Phase 3D D0-D12 implementation being present
Primary goal: close the remaining correctness and proof gaps so Phase 3 can finish with a real clean-head end-to-end proof, not static declarations.

## Why This File Exists

`13-StorefrontReverseEngineering-Phase3D-Final-Closure-Fix.todo.md` covers the first Phase 3D closure plan and much of D0-D12 is now implemented. The follow-up review found a smaller but important correction round: D13-D19.

This file is intentionally separate so implementation can focus on the remaining blockers without rewriting the earlier Phase 3D plan history.

## Current Codebase Evidence

The review points are confirmed against current code:

- `BuildReviewedPageCompositions()` still reads draft/raw inputs through `ReadPageArchetype()`, `ReadPageSections()`, `ReadSharedTokens()`, `ReadPresentationMappings()`, and `ReadEcommerceRegionsBySection()` in `tools/BlazorShop.AI.StorefrontReverseEngineering/Analysis/Blueprint/BlueprintV1Assembler.cs`.
- `ReadPageSections()` reads `analysis/pages/{pageId}/sections.draft.json`.
- `ReadSharedTokens()` reads `analysis/tokens/semantic-tokens.draft.json`.
- `ReadPresentationMappings()` reads `analysis/mapping/presentation-mappings.draft.json`.
- `PageCompositionSlotValidator` still treats role inference as authoritative slot presence through `InferSlot(...)` and `AddPresence(...)`.
- `PageCompositionSlotValidator` still stores observed slots as `Dictionary<string, int>`, which is not enough to audit node/mapping sources precisely.
- `AgentHandoffEvidencePackager.CropSectionAsync()` still uses `node.ViewportBoundingBoxes.Values.FirstOrDefault()` instead of `node.ViewportBoundingBoxes[viewport.ViewportId]`.
- `Phase3DProofFixtureTests` currently verifies static JSON fixtures and marker-to-blocker mapping instead of running the actual pipeline or mutating real artifacts.
- `run-storefront-reverse-engineering-phase3d-final-closure-gate.ps1` is no-skip and clean-head oriented, but still invokes the same static negative fixture test for review, slot, and handoff proof.
- `docs/qa/phase3d-final-closure.md` kept Phase 3D and Phase 3 overall in progress until the final clean-head gate passed; final proof is now recorded there.

## Locked Decisions

- Phase 3D remains correctness and closure proof only.
- No StorefrontBuilder handoff consumption in this phase.
- No generated Razor/CSS/JS storefront output in this phase.
- Phase 4 input remains `analysis/agent-handoff/*` plus schemas only.
- Reviewed artifacts must be authoritative after review decisions are applied.
- Reviewed page composition must be built from resolved artifacts, not draft artifacts.
- Slot presence must come from reviewed mappings and exact Storefront slot contracts, not inferred role text.
- Section crops must use bounds for the same viewport being cropped.
- Final closure proof must run on a clean working tree without skip flags.

## Scope

In scope:

- Reviewed composition readers and builder separation.
- Reviewed decision propagation through page composition and handoff.
- Authoritative slot observation and duplicate/extra-section validation.
- Viewport-specific bounding-box handling.
- Real positive end-to-end proof.
- Real negative mutation proofs.
- Phase 3D gate hardening to call real proof filters.
- Documentation/status alignment for final closure.

Not in scope:

- Storefront visual generation.
- StorefrontBuilder consumption of `analysis/agent-handoff/*`.
- Changes to Commerce Node, Control Plane, Storefront V2, Runtime, Presentation, Components, Starter runtime behavior, or ecommerce APIs.
- Refactoring Phase 3A/3B capture internals unless needed to expose reviewed evidence correctly.
- Solving the pre-existing `.gitignore` dirty-tree issue by hiding or bypassing the clean-tree gate.

## P0 Blockers

- [x] P0-1: Reviewed page composition reads draft/raw inputs.
- [x] P0-2: Slot validator can pass required slot presence from role text inference.
- [x] P0-3: Duplicate slot counting does not preserve distinct node/mapping sources.
- [x] P0-4: Unmapped extra sections can be ignored instead of blocked.
- [x] P0-5: Section crop uses first available bounds instead of viewport-specific bounds.
- [x] P0-6: Positive/negative proofs are static fixture assertions, not real pipeline/mutation proofs.
- [x] P0-7: Full clean-head Phase 3D gate has passed.

## D13 - Build Reviewed Composition From Resolved Artifacts

Goal: `analysis/resolved/page-compositions.reviewed.json` must reflect resolved review decisions.

### D13.1 Reviewed Input Reader

Implementation checklist:

- [x] Add `ReviewedCompositionInputReader`.
- [x] Read page archetypes from `analysis/resolved/page-archetypes.reviewed.json`.
- [x] Read sections from `analysis/resolved/page-sections.reviewed.json`.
- [x] Read semantic tokens from `analysis/resolved/semantic-tokens.reviewed.json`.
- [x] Read Presentation mappings from `analysis/resolved/presentation-mappings.reviewed.json`.
- [x] Read ecommerce regions from `analysis/resolved/ecommerce-regions.reviewed.json`.
- [x] Read originality restrictions from `analysis/resolved/originality-restrictions.reviewed.json`.
- [x] Read `analysis/resolved/review-resolution-manifest.json`.
- [x] Validate artifact kind for every reviewed input.
- [x] Validate project ID for every reviewed input.
- [x] Validate each reviewed input is listed in the resolution manifest.
- [x] Validate source artifact hash or reviewed artifact hash when available.
- [x] Fail when a required reviewed input is missing.
- [x] Do not fallback to draft artifacts.

Blocking codes:

- [x] `missing-reviewed-composition-input`
- [x] `reviewed-composition-input-kind-mismatch`
- [x] `reviewed-composition-project-id-mismatch`
- [x] `reviewed-composition-hash-stale`
- [x] `reviewed-composition-uses-draft-input`

### D13.2 Draft And Reviewed Builders

Implementation checklist:

- [x] Split composition building into:
  - [x] `BuildDraftPageCompositions(...)`
  - [x] `BuildReviewedPageCompositions(...)`
- [x] Draft builder may read:
  - [x] `analysis/pages/*/page-archetype.json`
  - [x] `analysis/pages/*/sections.draft.json`
  - [x] `analysis/tokens/semantic-tokens.draft.json`
  - [x] `analysis/mapping/presentation-mappings.draft.json`
  - [x] `analysis/pages/*/ecommerce-regions.json`
- [x] Reviewed builder must read only resolved artifacts plus stable non-draft evidence metadata.
- [x] Reviewed builder must not call the draft reader helpers.
- [x] Add static guard test that reviewed builder source does not contain:
  - [x] `sections.draft.json`
  - [x] `semantic-tokens.draft.json`
  - [x] `presentation-mappings.draft.json`
  - [x] `analysis/pages/*/ecommerce-regions.json` as authoritative input.

### D13.3 Reviewed Composition Provenance

Add to reviewed composition metadata:

- [x] `reviewResolutionManifestPath`
- [x] `reviewBundleHash`
- [x] `sourceResolvedArtifactHashes`
- [x] `reviewedInputArtifactPaths`
- [x] `reviewedInputArtifactKinds`

### D13.4 Modified Decision Propagation Proof

Tests:

- [x] Modified mapping target path appears in:
  - [x] `analysis/resolved/presentation-mappings.reviewed.json`
  - [x] `analysis/resolved/page-compositions.reviewed.json`
  - [x] `analysis/agent-handoff/page-compositions.json`
  - [x] `analysis/agent-handoff/allowed-files.json`
- [x] Modified section role appears in:
  - [x] `analysis/resolved/page-sections.reviewed.json`
  - [x] `analysis/resolved/page-compositions.reviewed.json`
  - [x] `analysis/agent-handoff/page-compositions.json`
  - [x] `analysis/agent-handoff/task.md`
- [x] Modified page archetype is used to select exact page contract.
- [x] Modified semantic token is used by:
  - [x] `analysis/resolved/page-compositions.reviewed.json`
  - [x] `analysis/agent-handoff/design-tokens.json`
  - [x] `analysis/agent-handoff/visual-style.json`

Done when:

- [x] Reviewed page composition has no authoritative draft input dependency.
- [x] Modified review decisions propagate into final handoff.

## D14 - Authoritative Slot Mapping

Goal: required slot validation must be contract-driven, not heuristic-driven.

### D14.1 Slot Observation Model

Replace:

```text
Dictionary<string, int>
```

with:

```text
Dictionary<string, HashSet<SlotObservationSource>>
```

`SlotObservationSource` fields:

- [x] `SourceKind`: `page-target`, `reviewed-mapping`, `catalog-target`, `approved-extension`
- [x] `SourceId`
- [x] `PageId`
- [x] `SectionNodeId`
- [x] `MappingId`
- [x] `SlotId`
- [x] `TargetPath`

### D14.2 Authoritative Slot Sources

Slot presence can be added only from:

- [x] `PageComposition.TargetViewSlot` when it is exact, valid, and mapped to a page contract.
- [x] Reviewed `PresentationMapping.StarterSlotId`.
- [x] Valid Presentation catalog target path matching an exact slot.
- [x] Explicit reviewed visual-only extension slot.

Slot presence must not be added from:

- [x] `InferSlot(pageArchetype, node.Role)`
- [x] text role labels.
- [x] source HTML labels.
- [x] section type names without reviewed mapping.

### D14.3 Role Inference Downgrade

Implementation checklist:

- [x] Rename or wrap `InferSlot(...)` as `SuggestSlotFromRole(...)`.
- [x] Use suggestion only for diagnostics.
- [x] Emit `section-slot-suggestion-unreviewed` when role text suggests a slot but no reviewed mapping exists.
- [x] Emit `required-slot-unmapped` when a required slot is only suggested and not reviewed.
- [x] Do not count suggestions as observed slots.

### D14.4 Required Slot Validation

For every required slot:

- [x] slot has at least one authoritative source.
- [x] source is reviewed where mapping is required.
- [x] target path is valid.
- [x] Presentation catalog component exists.
- [x] Starter slot ID matches catalog slots.
- [x] behavior ownership is valid.

Blocking codes:

- [x] `missing-required-slot`
- [x] `required-slot-unmapped`
- [x] `invalid-section-slot-mapping`
- [x] `slot-target-path-mismatch`
- [x] `slot-behavior-ownership-conflict`

### D14.5 Duplicate Slot Validation

Rules:

- [x] Count unique source IDs, not only slot names.
- [x] Non-repeatable slot with more than one unique source fails.
- [x] Repeatable slot passes when count is at least required minimum.
- [x] `catalog.product-card` remains repeatable.
- [x] `product.gallery`, `product.purchase`, `product.information`, `layout.header`, `layout.footer`, `cart.page`, `checkout.page`, and `account.shell` are non-repeatable unless a page contract explicitly says otherwise.

Blocking code:

- [x] `duplicate-non-repeatable-slot`

### D14.6 Extra Section Validation

Every reviewed section node must resolve to one of:

- [x] required slot.
- [x] optional slot.
- [x] repeatable slot.
- [x] allowed additional slot.
- [x] explicit approved visual-only extension.

Otherwise emit:

- [x] `unapproved-extra-section`

Unknown or unmapped nodes must not be silently ignored.

### D14.7 Approved Visual Extensions

Add optional reviewed fields:

- [x] `approvedVisualExtensionId`
- [x] `approvedVisualExtensionReason`

Rules:

- [x] Extension must be human-reviewed.
- [x] Extension must not own protected behavior.
- [x] Extension must target an allowed generated zone.
- [x] Extension must not replace required slots.
- [x] Extension must be listed in `AllowedAdditionalSlotIds` or an extension registry.

Tests:

- [x] Role `purchase panel` without reviewed mapping fails.
- [x] Role `gallery` without reviewed mapping fails.
- [x] Valid reviewed `product.purchase` mapping passes.
- [x] Two gallery nodes fail duplicate validation.
- [x] Two product cards pass repeatable validation.
- [x] Unknown unmapped section fails as extra section.
- [x] Approved visual extension passes.
- [x] Runtime/headless mapping fails.
- [x] Missing target path fails.
- [x] Invalid catalog target fails.

Done when:

- [x] Slot presence is source-auditable and never based on role inference alone.

## D15 - Per-Viewport Bounding Boxes

Goal: screenshot crops must use the bounding box for the same viewport being processed.

### D15.1 Evidence Model

Implementation checklist:

- [x] Ensure `PageSectionInfo` preserves `ViewportBoundingBoxes`.
- [x] Ensure `PageCompositionNode.ViewportBoundingBoxes` preserves the full dictionary.
- [x] Parse per-viewport boxes from Phase 3A/3B evidence when available.
- [x] Preserve exact viewport IDs such as `desktop-1440`, `tablet-768`, `mobile-390`.
- [x] Do not collapse bounds to `base`.
- [x] Do not copy desktop bounds to mobile automatically.
- [x] If only one viewport exists, use the actual viewport ID.

### D15.2 Crop Lookup

Replace:

```csharp
node.ViewportBoundingBoxes.Values.FirstOrDefault()
```

with:

```csharp
node.ViewportBoundingBoxes.TryGetValue(viewport.ViewportId, out var bounds)
```

Rules:

- [x] Missing required viewport bounds is blocking.
- [x] Invalid bounds format is blocking.
- [x] Zero-size bounds is blocking.
- [x] Bounds outside image may clamp if final width/height remains non-zero.
- [x] Crops are deterministic.
- [x] Error message must include problem, cause, and fix.

Blocking codes:

- [x] `missing-section-viewport-bounds`
- [x] `invalid-section-viewport-bounds`
- [x] `section-crop-out-of-range`
- [x] `missing-required-section-crop`

### D15.3 Required Viewport Policy

For representative pages:

- [x] desktop crop required.
- [x] tablet crop required.
- [x] mobile crop required.

Optional section policy:

- [x] If section is hidden in a viewport according to reviewed responsive model, missing crop is allowed.
- [x] If section renders in a viewport but lacks bounds, readiness fails.

Tests:

- [x] Desktop crop uses desktop bounds.
- [x] Tablet crop uses tablet bounds.
- [x] Mobile crop uses mobile bounds.
- [x] Desktop and mobile different bounds produce different crop hashes.
- [x] Missing mobile bounds blocks.
- [x] Hidden-on-mobile optional section does not block.
- [x] Zero-size bounds blocks.
- [x] Out-of-range bounds clamp when still non-zero.
- [x] Crop hash is deterministic.

Done when:

- [x] Every section crop is tied to the correct viewport ID.

## D16 - Real Positive End-To-End Proof

Goal: prove the actual pipeline can produce a passing handoff.

### D16.1 Fixture Site

Use a local fixture site or fixture reference browser that includes:

- [x] home page.
- [x] PLP/category page.
- [x] PDP with 1:1 product gallery.
- [x] cart shell.
- [x] checkout shell.
- [x] account/auth shell.
- [x] system state.
- [x] desktop/tablet/mobile layouts.
- [x] shared header/footer.
- [x] reused product cards.
- [x] PDP gallery, information, and purchase sections.
- [x] at least one responsive reorder.
- [x] at least one interaction state.

### D16.2 Real Pipeline

Test must execute the actual pipeline:

```text
create project
-> create multi-page capture plan
-> capture fixture pages
-> extract evidence
-> run analysis
-> write valid review decisions
-> include at least one Modified decision
-> apply review
-> build storefront pattern
-> build catalog/mappings
-> assemble reviewed blueprint
-> build reviewed compositions
-> package screenshots/crops
-> assemble handoff
-> validate generation readiness
-> validate handoff readiness
```

Do not replace this with a static JSON declaration.

### D16.3 Assertions

- [x] Workflow succeeds.
- [x] CLI returns zero.
- [x] Reviewed blueprint exists.
- [x] Reviewed blueprint has no draft references.
- [x] Modified decision appears in reviewed artifacts.
- [x] Modified decision appears in reviewed page composition.
- [x] Modified decision appears in handoff copy.
- [x] Home exact slots pass.
- [x] PLP exact slots pass.
- [x] PDP exact slots pass.
- [x] Cart shell exact slots pass.
- [x] Checkout shell exact slots pass.
- [x] Account/auth shell exact slots pass.
- [x] System state exact slots pass.
- [x] All configured viewports have screenshots.
- [x] Major sections have viewport-specific crops.
- [x] Evidence hashes validate.
- [x] Manifest hashes validate.
- [x] Handoff readiness passed.
- [x] `inspect` reports final pass.

### D16.4 Determinism

Run proof twice and assert:

- [x] composition IDs stable.
- [x] section crop filenames stable.
- [x] handoff schema stable.
- [x] artifact lists stable.
- [x] hashes stable except intentionally timestamped fields.
- [x] no unexpected artifact drift.

Tests:

- [x] `Phase3DPositiveEndToEndTests.PositivePipeline_ProducesReadySelfContainedHandoff`
- [x] `Phase3DPositiveEndToEndTests.PositivePipeline_PropagatesModifiedDecisionsToHandoff`
- [x] `Phase3DPositiveEndToEndTests.PositivePipeline_IsDeterministicForStableInputs`

Done when:

- [x] A real positive pipeline proof replaces static positive declaration proof.

## D17 - Real Negative Mutation Proofs

Goal: every critical blocker must be emitted by real validators over mutated artifacts.

### D17.1 Review Mutation Tests

Each test must:

```text
create or run valid fixture project
-> mutate real review artifacts
-> run actual review resolver or workflow step
-> assert exact blocker/exception code
```

Cases:

- [x] stale decision -> `decision-source-hash-mismatch`.
- [x] unknown status -> `SRE-WORKFLOW-REVIEW-DECISIONS-INVALID`.
- [x] Modified without value -> `SRE-WORKFLOW-REVIEW-DECISIONS-INVALID`.
- [x] duplicate without supersede -> `SRE-WORKFLOW-REVIEW-DECISIONS-INVALID`.
- [x] Deferred critical -> reviewed blueprint absent and `reviewed-blueprint-not-resolved`.
- [x] Rejected critical -> reviewed mapping removed and readiness blocked.

### D17.2 Slot Mutation Tests

Each test must mutate reviewed composition or reviewed mapping, not static marker JSON.

Cases:

- [x] remove reviewed `product.purchase` mapping/node -> `missing-required-slot` or `required-slot-unmapped`.
- [x] remove reviewed `product.gallery` mapping/node -> `missing-required-slot`.
- [x] clone reviewed mapped gallery node -> `duplicate-non-repeatable-slot`.
- [x] add reviewed node without approved slot -> `unapproved-extra-section`.
- [x] set runtime/headless target as visual slot -> `slot-behavior-ownership-conflict`.
- [x] set protected target path -> `protected-path-target`.
- [x] set missing target path -> `slot-target-path-mismatch` or `invalid-section-slot-mapping`.
- [x] set invalid catalog target -> `invalid-section-slot-mapping`.

### D17.3 Evidence Mutation Tests

Cases:

- [x] remove mobile bounds -> `missing-section-viewport-bounds`.
- [x] set invalid bounds -> `invalid-section-viewport-bounds`.
- [x] set zero-size bounds -> `invalid-section-viewport-bounds`.
- [x] delete section crop -> `missing-section-screenshot`.
- [x] corrupt crop bytes -> `evidence-hash-mismatch`.
- [x] set crop path outside handoff -> `handoff-path-escape`.

### D17.4 Handoff Mutation Tests

Cases:

- [x] delete `task.md` -> `missing-agent-handoff-artifact`.
- [x] delete `design-tokens.json` -> `missing-agent-handoff-artifact`.
- [x] delete `evidence-manifest.json` -> `missing-agent-handoff-artifact`.
- [x] remove manifest artifact entry -> `missing-agent-handoff-artifact`.
- [x] add `../` path -> `handoff-path-escape`.
- [x] add absolute path -> `absolute-source-dependency` or `handoff-path-escape`.
- [x] overlap allowed and protected paths -> `allowed-protected-overlap`.
- [x] point visual blueprint to draft -> `reviewed-blueprint-references-draft`.
- [x] change artifact kind -> `artifact-kind-mismatch`.
- [x] change project ID -> `project-id-mismatch`.
- [x] change artifact bytes without hash update -> `handoff-hash-mismatch`.

### D17.5 Browser Boundary Mutation Tests

Inject real generated-intent markers and run the actual static boundary validator.

Cases:

- [x] `@page` route marker -> `generated-route-ownership`.
- [x] `/api/storefront` direct call -> `unsafe-browser-action`.
- [x] `CommerceNode` direct marker -> `unsafe-browser-action`.
- [x] functional checkout/payment JavaScript marker -> `unsafe-browser-action` or `slot-behavior-ownership-conflict`.
- [x] route reimplementation marker -> `generated-route-ownership`.
- [x] BFF reimplementation marker -> `slot-behavior-ownership-conflict`.
- [x] SEO/media reimplementation marker -> `slot-behavior-ownership-conflict`.

Tests:

- [x] `Phase3DNegativeReviewMutationTests`
- [x] `Phase3DNegativeSlotMutationTests`
- [x] `Phase3DNegativeEvidenceMutationTests`
- [x] `Phase3DNegativeHandoffMutationTests`
- [x] `Phase3DNegativeBoundaryMutationTests`

Done when:

- [x] Static marker-to-blocker tests are no longer the primary negative proof.

## D18 - Gate Hardening

Goal: final Phase 3D gate must prove implementation behavior, not static fixture declarations.

### D18.1 Replace Static Proof Filters

Remove repeated gate calls to:

```text
Phase3DProofFixtureTests.Phase3DNegativeFixtures_MapToExactExpectedBlockers
```

Replace with focused real proof filters:

- [x] `Phase3DPositiveEndToEnd`
- [x] `Phase3DNegativeReviewMutation`
- [x] `Phase3DNegativeSlotMutation`
- [x] `Phase3DNegativeEvidenceMutation`
- [x] `Phase3DNegativeHandoffMutation`
- [x] `Phase3DNegativeBoundaryMutation`

### D18.2 Final Gate Order

Gate order:

```text
clean working tree
-> record tested SHA
-> build ReverseEngineering
-> Phase 3A gate
-> Phase 3B gate
-> Phase 3C gate
-> full ReverseEngineering tests
-> reviewed composition propagation tests
-> authoritative slot tests
-> per-viewport crop tests
-> positive E2E proof
-> negative review mutations
-> negative slot mutations
-> negative evidence mutations
-> negative handoff mutations
-> negative boundary mutations
-> boundary scans
-> StorefrontBuilder plan-only smoke
-> final inspect proof
-> assert HEAD unchanged
-> assert working tree clean
-> write report
```

### D18.3 Dirty Tree Resolution

Implementation checklist:

- [x] Keep clean-tree check strict.
- [x] Do not add bypass or skip flag.
- [x] Resolve the pre-existing `.gitignore` dirty-tree issue outside the gate by committing the intentional `Skills/` ignore entry.
- [x] Do not hide dirty tree by changing gate exclusions.
- [x] Gate report should print exact dirty entries on failure.

### D18.4 Report Requirements

Report must include:

- [x] tested SHA.
- [x] final HEAD.
- [x] working tree clean status.
- [x] full gate command.
- [x] full test count.
- [x] reviewed composition proof.
- [x] authoritative slot proof.
- [x] per-viewport crop proof.
- [x] positive E2E result.
- [x] negative mutation result counts.
- [x] StorefrontBuilder smoke result.
- [x] GitHub Actions status.
- [x] known limitations.
- [x] Phase 3 closure decision.

### D18.5 No Post-Proof Commit Rule

Preferred closure flow:

1. [x] Complete code and docs.
2. [x] Commit final candidate.
3. [x] Run full gate.
4. [x] Gate writes ignored runtime report under `obj`.
5. [x] Do not change source/docs after pass.
6. [x] Closure doc must point to ignored gate report as final authoritative proof if exact self-referential final SHA cannot be tracked before the run.

If tracked closure doc must contain exact final SHA:

1. [x] Commit implementation.
2. [x] Run gate.
3. [x] Commit closure doc with implementation SHA.
4. [x] Rerun gate on closure-doc HEAD.
5. [x] Closure doc records both implementation SHA and final closure SHA strategy.

Done when:

- [x] Phase 3D final gate proves real behavior and cannot pass because static marker fixtures exist.

## D19 - Documentation Status Alignment

Goal: documentation must match actual closure state and handoff semantics.

Files to update when implementation lands:

- [x] `docs/visual-reverse-engineering-skill/13-StorefrontReverseEngineering-Phase3D-Final-Closure-Fix.todo.md`
- [x] `docs/visual-reverse-engineering-skill/14-StorefrontReverseEngineering-Phase3D-D13-D19-Correctness-Proof.todo.md`
- [x] `docs/qa/phase3d-final-closure.md`
- [x] `docs/visual-reverse-engineering-skill/README.md`
- [x] `docs/visual-reverse-engineering-skill/reference.md`
- [x] `docs/visual-reverse-engineering-skill/how-to-generate-and-validate.md`
- [x] `docs/architecture/11-storefront-builder.md`
- [x] `docs/agents/storefront-builder.md`

Before final gate pass:

- [x] Phase 3D: In progress.
- [x] Phase 3 overall: In progress.

After final gate pass:

- [x] Phase 3A: Complete.
- [x] Phase 3B: Complete.
- [x] Phase 3C: Complete.
- [x] Phase 3D: Complete.
- [x] Phase 3 overall: Complete.

Final closure statement:

```text
Phase 3 is closed because the final clean-HEAD Phase 3D gate proved that
reviewed decisions propagate into site-level page compositions and a
self-contained handoff; exact ecommerce slots are enforced from reviewed
Presentation mappings; per-viewport evidence is packaged correctly; and
positive/negative end-to-end proofs pass without enabling StorefrontBuilder
consumption or storefront source generation.
```

Done when:

- [x] Docs do not claim Phase 3 complete before the no-skip clean-head gate passes.
- [x] Docs do not suggest Phase 4 can consume draft artifacts.
- [x] Docs keep `analysis/agent-handoff/` as the only approved Phase 4 input.

## Recommended Implementation Order

1. [x] D13.1 Add reviewed input readers.
2. [x] D13.2 Split draft and reviewed composition builders.
3. [x] D13.3 Add reviewed composition provenance.
4. [x] D13.4 Add modified decision propagation tests.
5. [x] D14.1 Replace slot count dictionary with source-aware observations.
6. [x] D14.2 Remove role inference from authoritative slot presence.
7. [x] D14.3 Add required-slot-unmapped and suggestion diagnostics.
8. [x] D14.4 Fix duplicate and extra-section validation.
9. [x] D14.5 Add approved visual extension support.
10. [x] D15.1 Preserve per-viewport section bounds.
11. [x] D15.2 Crop using `viewport.ViewportId` bounds.
12. [x] D16 Add positive end-to-end proof.
13. [x] D17 Add negative mutation proof suites.
14. [x] D18 Replace Phase 3D gate static proof filters.
15. [x] D19 Update docs/status.
16. [x] Resolve dirty tree blocker.
17. [x] Run final no-skip clean-head gate.

Suggested commits:

1. [x] `phase 3d: read reviewed compositions from resolved artifacts`
2. [x] `phase 3d: enforce authoritative slot observations`
3. [x] `phase 3d: crop handoff evidence per viewport`
4. [x] `phase 3d: add positive end-to-end proof`
5. [x] `phase 3d: add negative mutation proof suites`
6. [x] `phase 3d: harden final closure gate`
7. [x] `phase 3d: align closure docs`
8. [x] `phase 3d: align phase3a gate with strict review semantics`
9. [x] `phase 3d: align phase3b gate with strict review semantics`
10. [x] `phase 3d: record final clean-head proof`

## Test Matrix

Reviewed composition:

- [x] `ReviewedComposition_UsesResolvedPageArchetypes`
- [x] `ReviewedComposition_UsesResolvedSections`
- [x] `ReviewedComposition_UsesResolvedMappings`
- [x] `ReviewedComposition_UsesResolvedEcommerceRegions`
- [x] `ReviewedComposition_UsesResolvedTokens`
- [x] `ReviewedComposition_DoesNotReadDraftInputs`
- [x] `ReviewedComposition_ModifiedMappingPropagatesToHandoff`
- [x] `ReviewedComposition_ModifiedSectionPropagatesToTask`

Slot enforcement:

- [x] `SlotValidation_RoleSuggestionWithoutMappingDoesNotSatisfyRequiredSlot`
- [x] `SlotValidation_ReviewedMappingSatisfiesRequiredSlot`
- [x] `SlotValidation_DuplicateNonRepeatableSlotFails`
- [x] `SlotValidation_RepeatableProductCardsPass`
- [x] `SlotValidation_UnknownUnmappedSectionFails`
- [x] `SlotValidation_ApprovedVisualExtensionPasses`
- [x] `SlotValidation_RuntimeOwnershipFails`

Viewport crop:

- [x] `HandoffEvidence_DesktopCropUsesDesktopBounds`
- [x] `HandoffEvidence_TabletCropUsesTabletBounds`
- [x] `HandoffEvidence_MobileCropUsesMobileBounds`
- [x] `HandoffEvidence_MissingViewportBoundsBlocks`
- [x] `HandoffEvidence_InvalidViewportBoundsBlocks`
- [x] `HandoffEvidence_CropHashIsDeterministic`

Proof:

- [x] `Phase3DPositiveEndToEnd`
- [x] `Phase3DNegativeReviewMutation`
- [x] `Phase3DNegativeSlotMutation`
- [x] `Phase3DNegativeEvidenceMutation`
- [x] `Phase3DNegativeHandoffMutation`
- [x] `Phase3DNegativeBoundaryMutation`

Gate:

- [x] Phase 3D gate has no skip flags.
- [x] Phase 3D gate does not call static negative marker proof as primary proof.
- [x] Phase 3D gate fails dirty tree.
- [x] Phase 3D gate records tested SHA and final HEAD.

## Definition Of Done

Reviewed composition:

- [x] Reads resolved archetypes.
- [x] Reads resolved sections.
- [x] Reads resolved mappings.
- [x] Reads resolved ecommerce regions.
- [x] Reads resolved tokens.
- [x] Does not read authoritative draft inputs.
- [x] Modified decisions propagate to handoff.

Slot enforcement:

- [x] Required slots require reviewed mapping or other approved authoritative source.
- [x] Role inference is diagnostic only.
- [x] Duplicate non-repeatable slot fails.
- [x] Repeatable product cards pass.
- [x] Unknown/unmapped section fails.
- [x] Approved visual extension is explicit.
- [x] Protected behavior ownership fails.

Visual evidence:

- [x] Per-viewport boxes exist.
- [x] Crop uses the matching viewport box.
- [x] Desktop/tablet/mobile crops differ when layout differs.
- [x] Missing required viewport bounds fails.
- [x] Missing/corrupt crop fails.
- [x] Hashes are deterministic.

Proof:

- [x] Positive proof runs the full pipeline.
- [x] Positive proof includes Modified decision propagation.
- [x] Negative review tests mutate real artifacts.
- [x] Negative slot tests mutate real compositions/mappings.
- [x] Negative evidence tests mutate real files.
- [x] Negative handoff tests run actual validator.
- [x] Exact blocker codes are asserted.

Gate:

- [x] No skip flags.
- [x] Clean tree check passes.
- [x] Phase 3A gate passes.
- [x] Phase 3B gate passes.
- [x] Phase 3C gate passes.
- [x] Full tests pass.
- [x] StorefrontBuilder plan-only smoke passes.
- [x] Final inspect proof passes.
- [x] HEAD unchanged.
- [x] Final tree clean.

Boundary:

- [x] ReverseEngineering remains development-time only.
- [x] No Razor/CSS/JS storefront generation.
- [x] No StorefrontBuilder handoff consumption.
- [x] No writes into Starter.
- [x] No direct Commerce Node browser calls.
- [x] No generated routes.
- [x] No protected behavior reimplementation.

Closure:

- [x] Phase 3D status complete only after final gate pass.
- [x] Phase 3 overall complete only after final gate pass.
- [x] Closure report reflects final clean-head gate.
- [x] No source/docs commit after tested SHA unless gate is rerun.
- [x] Phase 4 may begin implementation planning.

## Final Phase 3 Closure Condition

Phase 3 can close only when this statement is true:

```text
Human-reviewed visual decisions are applied into resolved artifacts and
site-level page compositions; every ecommerce-critical section maps to an
exact reviewed Storefront Presentation slot; duplicate, missing, unknown and
unapproved sections are blocked; desktop, tablet and mobile evidence is
packaged using viewport-specific bounds; and a full clean-HEAD end-to-end gate
proves both successful and failing scenarios without generating storefront
source or enabling StorefrontBuilder handoff consumption.
```

Approved Phase 4 input remains:

```text
analysis/agent-handoff/
```

Next phase:

```text
Phase 4 - Agent-Assisted Storefront Visual Generation
```

## Decision Audit Trail

| # | Phase | Decision | Classification | Principle | Rationale | Rejected |
|---|-------|----------|----------------|-----------|-----------|----------|
| 1 | CEO | Create a new D13-D19 correction plan instead of rewriting Phase 3D D0-D12 history. | Auto-decided | Keep history reviewable | Existing Phase 3D file already records D0-D12 implementation evidence; the new review is a focused correction round. | Merge new blockers into old sections and blur closure evidence. |
| 2 | Eng | Build reviewed composition only from resolved artifacts. | Auto-decided | Source-of-truth correctness | Phase 4 must not receive handoff content derived from stale draft values after human review. | Keep draft readers and trust reviewed blueprint references. |
| 3 | Eng | Downgrade role inference from authoritative validation to diagnostics. | Auto-decided | Contract precision | Text role matching can make required slots appear present without reviewed mappings. | Continue counting inferred slots. |
| 4 | Eng | Track slot presence by source IDs, not integer counters. | Auto-decided | Testability and auditability | Duplicate validation needs to know which nodes or mappings created the slot observation. | Keep `Dictionary<string,int>`. |
| 5 | Eng | Use viewport-specific bounds for crop generation. | Auto-decided | Evidence fidelity | Mobile/tablet crops can be wrong if the first bounds entry belongs to desktop. | Use first available bounds as fallback. |
| 6 | QA | Replace static fixture marker proof with real pipeline and mutation proof. | Auto-decided | Production-grade verification | Static marker mapping does not prove validators catch real broken artifacts. | Keep static JSON proof as closure evidence. |
| 7 | DX | Keep final gate strict and require dirty tree resolution outside the gate. | Auto-decided | Release evidence integrity | Clean-head closure proof loses meaning if the gate can bypass local changes. | Add gate exclusions for dirty files. |

## GSTACK REVIEW REPORT

### Plan Summary

This plan focuses Phase 3D D13-D19 on the remaining closure blockers: reviewed composition must read resolved artifacts, slot validation must be mapping-driven, evidence crops must use matching viewport bounds, and final proof must run real positive and negative scenarios.

### Review Scores

- CEO: Pass. The plan protects Phase 4 from consuming ambiguous or stale handoff data.
- Design: Skipped. No UI visual design implementation is planned.
- Eng: Pass with required corrections. The plan addresses concrete source-level gaps found in `BlueprintV1Assembler`, `PageCompositionSlotValidator`, `AgentHandoffEvidencePackager`, and Phase 3D gate tests.
- DX: Pass with required proof improvements. The plan turns handoff consumption into a clear package contract and replaces static proof with behavior proof.

### Cross-Phase Themes

- Source-of-truth integrity: resolved artifacts, reviewed mappings, and exact slot IDs must drive Phase 4 input.
- Evidence fidelity: per-viewport crops and self-contained handoff assets must prevent later agents from guessing from raw capture state.
- Release proof quality: no-skip clean-head gate and real mutation tests are required before marking Phase 3 complete.

### Implementation Tasks

- [x] Implement D13 reviewed composition from resolved artifacts.
- [x] Implement D14 authoritative slot mapping.
- [x] Implement D15 per-viewport crop bounds.
- [x] Implement D16 real positive end-to-end proof.
- [x] Implement D17 real negative mutation proofs.
- [x] Implement D18 final gate hardening.
- [x] Implement D19 documentation/status alignment.

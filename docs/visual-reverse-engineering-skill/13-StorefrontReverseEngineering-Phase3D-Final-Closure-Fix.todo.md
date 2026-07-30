# StorefrontReverseEngineering Phase 3D - Final Closure Fix

Status: In progress  
Owner area: `tools/BlazorShop.AI.StorefrontReverseEngineering`  
Target folder: `docs/visual-reverse-engineering-skill`  
Depends on: Phase 3A complete, Phase 3B analysis complete, Phase 3C final handoff hardening implemented  
Primary goal: fix the remaining correctness, handoff, and closure-proof gaps so Phase 3 can close and Phase 4 can consume `analysis/agent-handoff/*` without reinterpreting raw evidence or guessing Storefront architecture.

## Current Codebase Evidence

The Phase 3D review is grounded in the current codebase:

- `ReviewDecisionApplier` validates status, modified values, stale hashes, duplicate decisions, and reviewer metadata, but `ResolvedReviewArtifactWriter` still copies draft artifacts to reviewed artifacts. See `tools/BlazorShop.AI.StorefrontReverseEngineering/Analysis/Review/ConfidenceScorer.cs`.
- `BlueprintV1Assembler` writes `analysis/visual-blueprint.v1.reviewed.json` unconditionally and still references draft token/mapping/section/ecommerce artifacts inside the reviewed blueprint. See `tools/BlazorShop.AI.StorefrontReverseEngineering/Analysis/Blueprint/BlueprintV1Assembler.cs`.
- `AssembleBlueprintV1Step` returns success even when generation readiness contains blockers, because it only emits warning messages. See `tools/BlazorShop.AI.StorefrontReverseEngineering/Application/VisualProjectWorkflowSteps.cs`.
- `AgentHandoffAssembler` and `AgentHandoffReadinessValidator` each define their own required artifact list, and those lists do not match. See `Analysis/Handoff/AgentHandoffAssembler.cs` and `Analysis/Handoff/AgentHandoffReadinessValidator.cs`.
- The handoff package writes JSON/text artifacts but does not package self-contained screenshots, section screenshots, or an evidence manifest under `analysis/agent-handoff/`.
- Page contracts currently expose `RequiredVisualRegions` and `OptionalVisualRegions`; exact per-page slot contracts such as `RequiredSlotIds`, `OptionalSlotIds`, and `RepeatableSlotIds` are not yet first-class contract fields.
- Phase 3C closure evidence says `reports/agent-handoff-readiness.json`, while code writes `analysis/agent-handoff/handoff-readiness.json`.
- Phase 3C final gate still accepts skip flags. That is acceptable for local development, but not for final Phase 3 closure proof.
- `docs/visual-reverse-engineering-skill/12-StorefrontReverseEngineering-Phase3C-Final-Handoff-Hardening.todo.md` has completed checkboxes but still says `Status: Proposed`, while `docs/qa/phase3c-final-handoff-closure.md` says complete.

## Architecture Decisions Already Locked

- Phase 3A owns rendered evidence capture.
- Phase 3B owns visual analysis and ecommerce mapping.
- Phase 3C owns strict site-level agent handoff.
- Phase 3D owns correctness fixes and final closure proof only.
- Phase 4 may read only `analysis/agent-handoff/*` and related schemas.
- Phase 4 must not read raw captures to reinterpret Storefront behavior.
- Phase 4 must not bypass reviewed decisions.
- Phase 4 must not generate routes, BFF behavior, SEO/media behavior, cart/checkout/account business logic, direct Commerce Node calls, or functional JavaScript.
- StorefrontBuilder consumption remains disabled until a separate approved Phase 4 implementation plan.

## Scope

In scope:

- Typed review artifact resolution.
- Reviewed blueprint lifecycle hardening.
- Exact page slot contracts.
- Page composition slot enforcement.
- Self-contained handoff evidence packaging.
- Canonical handoff artifact contract.
- Handoff task contract hardening.
- Strict workflow failure semantics.
- Schema and semantic validation.
- Positive and negative proof fixtures.
- Final no-skip local closure gate.
- Closure docs and status alignment.

Not in scope:

- Razor generation.
- CSS generation.
- StorefrontBuilder consumption of Phase 3D output.
- AI model API integration.
- Visual screenshot diff scoring.
- Autonomous visual repair.
- Cart, checkout, account, payment, catalog, Commerce Node, Control Plane, Runtime, Presentation, Components, or Starter runtime behavior changes.
- Refactoring Phase 3A/3B internals unless directly required to fix a Phase 3D blocker.

## Final Output Shape

After Phase 3D, a complete successful project should have this chain:

```text
Reference site
  -> multi-page evidence
  -> reviewed visual analysis
  -> exact ecommerce page contracts
  -> resolved review artifacts
  -> exact Presentation targets
  -> complete self-contained agent handoff
  -> strict handoff readiness pass
  -> clean commit-linked closure proof
```

The only approved future Phase 4 input root is:

```text
analysis/agent-handoff/
```

## Phase 3D.0 - Baseline Confirmation

Goal: establish the exact current behavior and prevent accidental expansion.

Implementation checklist:

- [x] Record current branch and HEAD before edits.
- [x] Run `git status --short` and document pre-existing unrelated changes.
- [x] Run focused ReverseEngineering tests before fixes:
  - [x] `dotnet test tools/BlazorShop.AI.StorefrontReverseEngineering/tests/BlazorShop.AI.StorefrontReverseEngineering.Tests/BlazorShop.AI.StorefrontReverseEngineering.Tests.csproj --no-restore`
- [x] Run current Phase 3C gate if feasible:
  - [x] `powershell -ExecutionPolicy Bypass -File scripts/qa/run-storefront-reverse-engineering-phase3c-final-handoff-gate.ps1`
  - [x] If local tools make full gate impossible, record the blocker; do not mark closure complete.
- [x] Add or update a closure tracking file:
  - [x] `docs/qa/phase3d-final-closure.md`
- [x] Record the known Phase 3D blockers before code changes.

Done when:

- [x] The phase starts from a documented baseline and the fix scope is locked to ReverseEngineering tooling, QA scripts, and docs.

## Phase 3D.1 - Typed Review Artifact Resolution

Goal: reviewed artifacts must be the actual result of applying valid review decisions, not direct copies of draft artifacts.

Implementation checklist:

- [x] Add an internal resolver abstraction:

```csharp
public interface IReviewArtifactResolver
{
    bool CanResolve(ReviewQueueItem item);

    Task ResolveAsync(
        ReviewResolutionContext context,
        ReviewQueueItem item,
        ReviewDecision decision,
        CancellationToken cancellationToken);
}
```

- [x] Add `ReviewResolutionContext` with:
  - [x] project root.
  - [x] artifact store.
  - [x] source artifact ID/hash.
  - [x] decision bundle hash.
  - [x] output artifact collector.
  - [x] blocker collector.
- [x] Implement resolvers for existing review item families:
  - [x] `SemanticTokenReviewResolver`
  - [x] `PresentationMappingReviewResolver`
  - [x] `EcommerceRegionReviewResolver`
  - [x] `PageArchetypeReviewResolver`
  - [x] `PageSectionReviewResolver`
  - [x] `ComponentCandidateReviewResolver`
  - [x] `UnsupportedPatternReviewResolver`
  - [x] `OriginalityRestrictionReviewResolver`
- [x] Replace `ResolvedReviewArtifactWriter.CopyIfExistsAsync(...)` with typed artifact resolution.
- [x] Keep draft artifacts immutable.
- [x] Write approved and modified values into typed reviewed artifacts.
- [x] Preserve original proposal beside reviewed value.
- [x] Exclude rejected critical items from approved outputs.
- [x] Emit deferred critical items into unresolved/blocking state.
- [x] Emit rejected/deferred non-critical items into manual disposition artifacts.
- [x] Validate `ModifiedValue` shape per artifact type before writing.
- [x] Fail on unknown review item family instead of silently preserving draft data.

Required resolved artifacts:

- [x] `analysis/resolved/semantic-tokens.reviewed.json`
- [x] `analysis/resolved/page-archetypes.reviewed.json`
- [x] `analysis/resolved/page-sections.reviewed.json`
- [x] `analysis/resolved/component-candidates.reviewed.json`
- [x] `analysis/resolved/presentation-mappings.reviewed.json`
- [x] `analysis/resolved/ecommerce-regions.reviewed.json`
- [x] `analysis/resolved/unsupported-pattern-decisions.json`
- [x] `analysis/resolved/originality-restrictions.reviewed.json`
- [x] `analysis/resolved/review-resolution-manifest.json`

Resolution manifest fields:

- [x] `schemaVersion`
- [x] `artifactKind`
- [x] `artifactId`
- [x] `projectId`
- [x] `createdUtc`
- [x] `sourceReviewQueueId`
- [x] `sourceReviewQueueHash`
- [x] `decisionBundleHash`
- [x] `resolvedItemCount`
- [x] `blockingUnresolvedCount`
- [x] `resolvedArtifacts[]`
- [x] `blockedItems[]`

Tests:

- [x] Approved semantic token preserves proposal.
- [x] Modified semantic token changes reviewed value.
- [x] Modified mapping changes target/variant in `presentation-mappings.reviewed.json`.
- [x] Modified ecommerce role changes reviewed region.
- [x] Modified page archetype changes reviewed page archetype.
- [x] Modified page section changes reviewed section metadata.
- [x] Rejected critical mapping is excluded and blocks readiness.
- [x] Deferred critical item blocks readiness.
- [x] Invalid `ModifiedValue` shape fails.
- [x] Unknown decision status fails.
- [x] Unknown review item family fails.
- [x] Stale decision fails.
- [x] Duplicate unsuperseded decision fails.
- [x] Resolution manifest hash changes when decision bundle changes.

Done when:

- [x] No code path copies draft artifacts directly into reviewed artifacts as the final source of truth.

## Phase 3D.2 - Reviewed Blueprint Lifecycle

Goal: `analysis/visual-blueprint.v1.reviewed.json` must exist only when it is safe and must reference resolved artifacts only.

Implementation checklist:

- [x] Keep draft blueprint generation unchanged:
  - [x] Always write `analysis/visual-blueprint.v1.draft.json`.
  - [x] Draft may reference draft analysis artifacts.
- [x] Change reviewed blueprint generation:
  - [x] Generate reviewed blueprint only after review resolution completes with zero blocking unresolved items.
  - [x] Reviewed blueprint must reference resolved artifacts, not draft artifacts.
  - [x] Reviewed blueprint must include review bundle hash.
  - [x] Reviewed blueprint must include Storefront pattern hash.
  - [x] Reviewed blueprint must include Presentation catalog hash.
  - [x] Reviewed blueprint must include page contract version/hash.
- [x] If a reviewed blueprint already exists and current resolution has blockers:
  - [x] delete it, or
  - [x] atomically replace it with an invalidated marker artifact that cannot be consumed.
- [x] Add blocking readiness code:
  - [x] `reviewed-blueprint-not-resolved`
  - [x] `reviewed-blueprint-references-draft`
  - [x] `reviewed-blueprint-hash-stale`
- [x] Update `BlueprintV1Assembler` so reviewed path selection is based on draft/reviewed mode.
- [x] Update schema for reviewed blueprint if separate semantics are needed.
- [x] Update tests so reviewed blueprint cannot pass while referencing `.draft.json`.

Reviewed blueprint allowed authoritative references:

- [x] `analysis/resolved/semantic-tokens.reviewed.json`
- [x] `analysis/resolved/page-archetypes.reviewed.json`
- [x] `analysis/resolved/page-sections.reviewed.json`
- [x] `analysis/resolved/page-compositions.reviewed.json`
- [x] `analysis/resolved/component-candidates.reviewed.json`
- [x] `analysis/resolved/presentation-mappings.reviewed.json`
- [x] `analysis/resolved/ecommerce-regions.reviewed.json`
- [x] `analysis/resolved/unsupported-pattern-decisions.json`
- [x] `analysis/resolved/originality-restrictions.reviewed.json`
- [x] `analysis/resolved/review-resolution-manifest.json`

Tests:

- [x] Draft blueprint always exists.
- [x] Reviewed blueprint does not exist when a deferred critical item remains.
- [x] Reviewed blueprint does not exist when a rejected critical item remains.
- [x] Reviewed blueprint references only resolved artifacts.
- [x] Reviewed blueprint has no authoritative `.draft.json` paths.
- [x] Stale review bundle hash fails readiness.
- [x] Existing stale reviewed blueprint is invalidated or deleted.

Done when:

- [x] A Phase 4 consumer can treat reviewed blueprint as a real reviewed source of truth.

## Phase 3D.3 - Exact Storefront Page Contracts

Goal: page contracts must validate exact Storefront slots, not loose natural-language visual region labels.

Implementation checklist:

- [x] Extend `StorefrontPageContract` with:
  - [x] `RequiredSlotIds`
  - [x] `OptionalSlotIds`
  - [x] `RepeatableSlotIds`
  - [x] `AllowedAdditionalSlotIds`
  - [x] `ForbiddenBehaviorIds`
- [x] Keep `RequiredVisualRegions` and `OptionalVisualRegions` only as human-readable descriptions.
- [x] Ensure validation uses exact slot ID fields only.
- [x] Derive page slot contracts from typed Storefront pattern/Starter contract, not from free-form labels.
- [x] Fail if a page contract references a slot ID not present in `StorefrontPatternContract.Slots`.
- [x] Fail if a page contract omits required global slots when applicable.
- [x] Fail if repeatable slot IDs are not present in allowed or required slots.
- [x] Fail if a page contract tries to make a Runtime-owned/headless-only behavior a visual required slot.

MVP page contract requirements:

- [x] Home:
  - [x] required: `layout.header`, `home.sections`, `layout.footer`
  - [x] optional: `layout.main-navigation`, `layout.mobile-navigation`, `layout.cart-badge`, `layout.account-menu`
- [x] Product listing:
  - [x] required: `layout.header`, `catalog.product-card`, `layout.footer`
  - [x] optional: `catalog.filters`, `catalog.sorting`, `catalog.pagination`, `layout.main-navigation`, `layout.mobile-navigation`, `layout.cart-badge`, `layout.account-menu`
  - [x] repeatable: `catalog.product-card`
- [x] Search results:
  - [x] required: `layout.header`, `catalog.product-card`, `layout.footer`
  - [x] optional: `catalog.filters`, `catalog.sorting`, `catalog.pagination`
  - [x] repeatable: `catalog.product-card`
- [x] Product detail:
  - [x] required: `layout.header`, `product.gallery`, `product.information`, `product.purchase`, `layout.footer`
  - [x] optional: `product.reviews`, `product.related-products`, `layout.main-navigation`, `layout.mobile-navigation`, `layout.cart-badge`, `layout.account-menu`
- [x] Cart shell:
  - [x] required: `layout.header`, `cart.page`, `layout.footer`
- [x] Checkout shell:
  - [x] required: `layout.header`, `checkout.page`, `layout.footer`
- [x] Account/auth shell:
  - [x] required: `layout.header`, `account.shell`, `layout.footer`
- [x] System state:
  - [x] required: exact state slot when available, otherwise `system.error`

Blocking codes:

- [x] `missing-required-slot`
- [x] `duplicate-required-slot`
- [x] `duplicate-non-repeatable-slot`
- [x] `unknown-slot`
- [x] `unapproved-extra-section`
- [x] `invalid-section-slot-mapping`
- [x] `slot-target-path-mismatch`
- [x] `slot-behavior-ownership-conflict`

Tests:

- [x] Removing `product.purchase` fails.
- [x] Removing `product.gallery` fails.
- [x] Duplicating `product.gallery` fails.
- [x] Removing optional `product.reviews` passes.
- [x] Multiple `catalog.product-card` slots pass.
- [x] Unknown slot fails.
- [x] Extra unapproved PDP section fails.
- [x] Runtime/headless behavior as required visual slot fails.

Done when:

- [x] Phase 4 can render required page sections from exact slot IDs without interpreting free-form labels.

## Phase 3D.4 - Page Composition Slot Enforcement

Goal: reviewed page composition must satisfy exact page contracts.

Implementation checklist:

- [ ] Add a page composition validator that reads:
  - [ ] `analysis/storefront-pattern/page-contracts.json`
  - [ ] `analysis/resolved/page-compositions.reviewed.json`
  - [ ] `analysis/resolved/presentation-mappings.reviewed.json`
  - [ ] `presentation-catalog/presentation-component-catalog.json`
- [ ] For each page:
  - [ ] assert every required slot exists.
  - [ ] assert optional missing slots are allowed.
  - [ ] assert repeatable slot count rules.
  - [ ] assert unknown slot is blocking.
  - [ ] assert extra visual-only section has explicit reviewed approval.
  - [ ] assert target generated file path is allowed.
  - [ ] assert target generated file path matches slot target path/rule.
  - [ ] assert protected path target is blocking.
  - [ ] assert behavior owner remains Presentation/Runtime when required.
  - [ ] assert no generated visual section owns BFF/SEO/media/cart/checkout/account logic.
- [ ] Add validator findings to generation readiness and handoff readiness.
- [ ] Ensure missing required page evidence and missing required slot are separate blocker codes.

Tests:

- [ ] PDP composition missing `product.purchase` fails with `missing-required-slot`.
- [ ] PDP composition missing evidence for `product.purchase` fails with `missing-section-evidence`.
- [ ] Product listing with several product-card nodes passes repeatable validation.
- [ ] Cart shell cannot own checkout placement behavior.
- [ ] Checkout shell cannot own payment provider behavior.
- [ ] Account shell cannot own authentication/token behavior.

Done when:

- [ ] Reviewed page composition is contract-validated before handoff packaging.

## Phase 3D.5 - Self-Contained Visual Evidence Handoff

Goal: Phase 4 can inspect visual evidence inside `analysis/agent-handoff/` without reading raw capture folders.

Implementation checklist:

- [ ] Add handoff evidence packaging service.
- [ ] Copy full-page screenshots into:

```text
analysis/agent-handoff/screenshots/{pageId}/{viewportId}.png
```

- [ ] Copy or generate section crops into:

```text
analysis/agent-handoff/section-screenshots/{pageId}/{sectionId}.{viewportId}.png
```

- [ ] Generate crops for major sections when bounds and screenshot are available:
  - [ ] header.
  - [ ] navigation.
  - [ ] hero.
  - [ ] product grid.
  - [ ] product card group.
  - [ ] product gallery.
  - [ ] product information.
  - [ ] product purchase.
  - [ ] cart shell.
  - [ ] checkout shell.
  - [ ] account shell.
  - [ ] footer.
  - [ ] system state.
- [ ] Clamp section crop bounds to image dimensions.
- [ ] Fail if crop width or height is less than or equal to zero.
- [ ] Preserve viewport scale metadata.
- [ ] Use deterministic output names.
- [ ] Do not copy reference assets into production-safe asset folders.
- [ ] Mark screenshots/crops as evidence only.
- [ ] Write `analysis/agent-handoff/evidence-manifest.json`.

Evidence manifest fields:

- [ ] `schemaVersion`
- [ ] `artifactKind`
- [ ] `artifactId`
- [ ] `projectId`
- [ ] `createdUtc`
- [ ] `pages[]`
- [ ] `pageId`
- [ ] `sourceUrl`
- [ ] `screenshots[]`
- [ ] `viewportId`
- [ ] `handoffPath`
- [ ] `sourcePath`
- [ ] `sha256`
- [ ] `sections[]`
- [ ] `sectionId`
- [ ] `slotId`
- [ ] `viewportId`
- [ ] `bounds`
- [ ] `interactionState`
- [ ] `originalityRestrictions`

Tests:

- [ ] Handoff contains desktop/mobile screenshots for Home, PLP, and PDP.
- [ ] Major PDP sections have crops.
- [ ] Missing required section crop blocks readiness.
- [ ] Invalid bounds block crop generation.
- [ ] Copied screenshot hash matches bytes.
- [ ] All Phase 4 evidence paths are under `analysis/agent-handoff`.
- [ ] No evidence file is labeled production-safe by default.

Done when:

- [ ] Agent handoff is self-contained for visual evidence.

## Phase 3D.6 - Canonical Agent Handoff Contract

Goal: assembler, manifest, validator, schemas, and tests use one required-artifact source of truth.

Implementation checklist:

- [ ] Add `AgentHandoffContract`:

```csharp
public static class AgentHandoffContract
{
    public static IReadOnlyList<RequiredHandoffArtifact> RequiredArtifacts { get; }
}
```

- [ ] Add `RequiredHandoffArtifact` fields:
  - [ ] relative path.
  - [ ] artifact kind.
  - [ ] schema name.
  - [ ] content type.
  - [ ] required condition.
  - [ ] hash required flag.
- [ ] Replace `AgentHandoffAssembler.RequiredArtifacts()`.
- [ ] Replace `AgentHandoffReadinessValidator.RequiredArtifacts()`.
- [ ] Ensure manifest artifact list comes from `AgentHandoffContract`.
- [ ] Ensure readiness validation comes from `AgentHandoffContract`.
- [ ] Add directories as required entries when directory content matters:
  - [ ] `analysis/agent-handoff/screenshots/`
  - [ ] `analysis/agent-handoff/section-screenshots/`
- [ ] Add `handoff-readiness.json` to canonical required list.
- [ ] Add `evidence-manifest.json` to canonical required list.

Required artifacts:

- [ ] `analysis/agent-handoff/manifest.json`
- [ ] `analysis/agent-handoff/task.md`
- [ ] `analysis/agent-handoff/allowed-files.json`
- [ ] `analysis/agent-handoff/protected-files.json`
- [ ] `analysis/agent-handoff/page-compositions.json`
- [ ] `analysis/agent-handoff/visual-style.json`
- [ ] `analysis/agent-handoff/design-tokens.json`
- [ ] `analysis/agent-handoff/storefront-pattern.json`
- [ ] `analysis/agent-handoff/visual-blueprint.json`
- [ ] `analysis/agent-handoff/unresolved-regions.json`
- [ ] `analysis/agent-handoff/generation-readiness.json`
- [ ] `analysis/agent-handoff/handoff-readiness.json`
- [ ] `analysis/agent-handoff/evidence-manifest.json`
- [ ] `analysis/agent-handoff/screenshots/`
- [ ] `analysis/agent-handoff/section-screenshots/`

Manifest improvements:

- [ ] remove absolute source project path from portable contract, or mark it diagnostics-only.
- [ ] add `handoffRoot`.
- [ ] add `reviewBundleHash`.
- [ ] add `storefrontPatternHash`.
- [ ] add `presentationCatalogHash`.
- [ ] add `visualBlueprintHash`.
- [ ] add `pageCompositionsHash`.
- [ ] add `evidenceManifestHash`.
- [ ] add `artifactEntries[]`.
- [ ] every artifact entry includes path, artifact kind, SHA-256, size, and required flag.

Validation:

- [ ] check every required artifact exists.
- [ ] check JSON schema.
- [ ] check artifact kind.
- [ ] check project ID consistency.
- [ ] check manifest hashes.
- [ ] check referenced paths remain under handoff root.
- [ ] reject `..` traversal.
- [ ] reject absolute required evidence dependencies.
- [ ] check reviewed blueprint uses resolved artifacts only.
- [ ] check evidence files exist and hashes match.
- [ ] check allowed/protected lists do not overlap.
- [ ] check target paths are under allowed generated zones.
- [ ] check unresolved blocking count is zero.
- [ ] check generation readiness passed.

Blocking codes:

- [ ] `missing-agent-handoff-artifact`
- [ ] `invalid-agent-handoff-schema`
- [ ] `artifact-kind-mismatch`
- [ ] `project-id-mismatch`
- [ ] `handoff-hash-mismatch`
- [ ] `handoff-path-escape`
- [ ] `absolute-source-dependency`
- [ ] `reviewed-blueprint-references-draft`
- [ ] `missing-handoff-evidence`
- [ ] `evidence-hash-mismatch`
- [ ] `allowed-protected-overlap`
- [ ] `invalid-generated-target`
- [ ] `blocking-unresolved-region`

Done when:

- [ ] Adding or removing a required handoff artifact requires changing exactly one canonical contract list.

## Phase 3D.7 - Handoff Task Contract

Goal: `analysis/agent-handoff/task.md` must be clear enough that an implementation agent does not guess.

Implementation checklist:

- [ ] Add task markdown generator sections:
  - [ ] Objective.
  - [ ] Inputs.
  - [ ] Source of truth priority.
  - [ ] Allowed file operations.
  - [ ] Protected files.
  - [ ] Required page slots.
  - [ ] Optional page slots.
  - [ ] Section order.
  - [ ] Responsive evidence.
  - [ ] Interaction evidence.
  - [ ] Originality restrictions.
  - [ ] Forbidden behavior.
  - [ ] Unsupported handling.
  - [ ] Validation commands.
  - [ ] Stop conditions.
- [ ] Include exact required slots per representative page.
- [ ] Include exact protected file patterns.
- [ ] Include exact allowed target paths.
- [ ] Include no-copy and reference-only asset instructions.
- [ ] Include StorefrontBuilder non-consumption warning until Phase 4 cutover.
- [ ] Include Phase 4 fail condition when handoff readiness is false.

Source of truth priority:

```text
1. handoff-readiness.json
2. visual-blueprint.json
3. storefront-pattern.json
4. page-compositions.json
5. allowed-files.json / protected-files.json
6. design-tokens.json / visual-style.json
7. screenshots / section-screenshots
```

Explicit stop conditions:

- [ ] Stop if handoff readiness is false.
- [ ] Stop if required page slot is missing.
- [ ] Stop if visual evidence is missing for a required major section.
- [ ] Stop if target path is missing, outside allowed zones, or protected.
- [ ] Stop if unsupported critical pattern remains.
- [ ] Stop if implementation would require routes, BFF, SEO/media, cart/checkout/account/auth logic, payment logic, or functional JavaScript.

Tests:

- [ ] `task.md` contains all mandatory headings.
- [ ] `task.md` lists exact required slots for Home, PLP, PDP, cart, checkout, account/auth, and system state.
- [ ] `task.md` contains stop conditions.
- [ ] `task.md` contains validation command placeholders or approved commands.
- [ ] Missing mandatory section blocks handoff readiness.

Done when:

- [ ] An implementation agent can follow `task.md` without opening raw capture folders or Phase 3 internals.

## Phase 3D.8 - Strict Workflow Failure Semantics

Goal: workflow success must mean final reviewed handoff is safe.

Recommended final workflow order:

```text
build-storefront-pattern
build-presentation-catalog
map-presentation-components
score-confidence-review
apply-review-decisions
build-resolved-artifacts
assemble-reviewed-blueprint
build-page-compositions
package-handoff-evidence
assemble-agent-handoff
validate-generation-readiness
validate-agent-handoff-readiness
```

Implementation checklist:

- [ ] Split `assemble-blueprint-v1` if needed so review resolution and reviewed blueprint assembly are separately testable.
- [ ] `apply-review-decisions` fails on invalid or stale decisions.
- [ ] `assemble-reviewed-blueprint` fails when blocking review is unresolved.
- [ ] `package-handoff-evidence` fails when required evidence is missing.
- [ ] `validate-generation-readiness` fails on page slot composition blockers.
- [ ] `assemble-agent-handoff` must not report success when readiness blockers exist.
- [ ] `validate-agent-handoff-readiness` remains the final success gate.
- [ ] `run` returns non-zero on final blockers.
- [ ] `resume` returns non-zero on final blockers for forced final steps.
- [ ] `inspect` shows exact blocker, cause, and next fix command.

Inspect output must include:

- [ ] review decision totals: approved, modified, rejected, deferred, stale.
- [ ] resolved artifact status and bundle hash.
- [ ] reviewed blueprint present/missing/invalid.
- [ ] page slot contract status.
- [ ] missing required slots.
- [ ] duplicate slots.
- [ ] unapproved extra sections.
- [ ] handoff screenshot count.
- [ ] handoff section crop count.
- [ ] missing evidence count.
- [ ] handoff package hash.
- [ ] latest blocker and suggested fix.

Tests:

- [ ] Workflow fails when reviewed blueprint cannot be assembled.
- [ ] Workflow fails when handoff evidence cannot be packaged.
- [ ] Workflow fails when handoff readiness has blockers.
- [ ] `inspect` reports exact blocker code and artifact path.
- [ ] Successful fixture exits zero only after final readiness passes.

Done when:

- [ ] A successful CLI run means Phase 4-safe handoff exists.

## Phase 3D.9 - Schema And Semantic Validation

Goal: schemas and semantic validators must match the new reviewed/handoff contract.

Required schema additions or updates:

- [ ] `review-resolution-manifest.schema.json`
- [ ] `reviewed-page-archetypes.schema.json`
- [ ] `reviewed-page-sections.schema.json`
- [ ] `reviewed-component-candidates.schema.json`
- [ ] `reviewed-originality-restrictions.schema.json`
- [ ] `agent-handoff-evidence-manifest.schema.json`
- [ ] `agent-handoff-manifest.schema.json`
- [ ] `page-contracts.schema.json`
- [ ] `reviewed-visual-blueprint.schema.json`

Semantic validations:

- [ ] exact slot ID exists in Storefront pattern.
- [ ] page contract slot is allowed for page archetype.
- [ ] mapping target exists in Presentation catalog.
- [ ] target path matches slot target path or approved variant.
- [ ] reviewed artifact source hash matches current draft.
- [ ] decision source hash matches queue item.
- [ ] handoff artifacts share project ID.
- [ ] reviewed blueprint does not reference `.draft.json`.
- [ ] handoff references stay under handoff root.
- [ ] required evidence file hash matches bytes.
- [ ] allowed paths do not intersect protected paths.
- [ ] schema artifact kind matches expected artifact kind.
- [ ] manifest declared hashes match file hashes.

Tests:

- [ ] Every Phase 3D JSON artifact validates against schema.
- [ ] Schema tests fail on artifact kind mismatch.
- [ ] Semantic validation fails on path escape.
- [ ] Semantic validation fails on stale source hash.
- [ ] Semantic validation fails on reviewed blueprint draft reference.

Done when:

- [ ] Structural and semantic validation both guard the handoff package.

## Phase 3D.10 - Positive And Negative Proof Fixtures

Goal: prove final closure with realistic positive and adversarial negative cases.

Positive multi-page fixture must include:

- [ ] home page.
- [ ] PLP/category page.
- [ ] PDP with 1:1 product gallery.
- [ ] cart shell.
- [ ] checkout shell.
- [ ] account/auth shell.
- [ ] system state.
- [ ] desktop/tablet/mobile evidence.
- [ ] shared header/footer.
- [ ] reused product cards.
- [ ] valid review decisions.
- [ ] at least one Modified decision.
- [ ] complete screenshots and section crops in handoff.

Positive expected result:

- [ ] review blocking unresolved count is zero.
- [ ] reviewed blueprint exists.
- [ ] reviewed blueprint references resolved artifacts only.
- [ ] generation readiness passed.
- [ ] handoff readiness passed.
- [ ] modified review value appears in reviewed artifact.
- [ ] PDP contains exact required slots.
- [ ] handoff contains screenshots and section crops.
- [ ] manifest hashes validate.

Negative review fixtures:

- [ ] stale decision.
- [ ] unknown status.
- [ ] modified without value.
- [ ] duplicate without supersede.
- [ ] deferred critical.
- [ ] rejected critical.

Negative page contract fixtures:

- [ ] missing `product.purchase`.
- [ ] missing `product.gallery`.
- [ ] duplicate `product.gallery`.
- [ ] extra unapproved PDP section.
- [ ] runtime/headless target as visual slot.
- [ ] protected path target.

Negative handoff fixtures:

- [ ] missing `task.md`.
- [ ] missing `design-tokens.json`.
- [ ] missing `evidence-manifest.json`.
- [ ] missing section screenshot.
- [ ] invalid screenshot hash.
- [ ] allowed/protected overlap.
- [ ] reviewed blueprint references draft.
- [ ] manifest path escape.
- [ ] missing handoff artifact entry.

Negative browser/behavior fixtures:

- [ ] direct Commerce Node API mutation.
- [ ] functional checkout/payment JavaScript.
- [ ] generated `@page`.
- [ ] route reimplementation marker.
- [ ] BFF reimplementation marker.
- [ ] SEO/media reimplementation marker.

Done when:

- [ ] Every negative fixture fails with the exact expected blocker code.

## Phase 3D.11 - Final No-Skip Closure Gate

Goal: produce clean local evidence that Phase 3 is complete.

Create:

```text
scripts/qa/run-storefront-reverse-engineering-phase3d-final-closure-gate.ps1
```

Rules:

- [ ] Do not expose `-SkipPhase3BGate`.
- [ ] Do not expose `-SkipStorefrontBuilderSmoke`.
- [ ] If GitHub Actions are disabled during development, report local gate as the primary proof and do not claim CI pass.
- [ ] Fail when the working tree is dirty.
- [ ] Record HEAD SHA before running.
- [ ] Record HEAD SHA after running.
- [ ] Fail if HEAD changes during gate.
- [ ] Fail if tested SHA is not final HEAD.

Gate order:

```text
clean tree check
-> build ReverseEngineering
-> Phase 3A regression gate
-> Phase 3B gate
-> Phase 3C gate
-> full ReverseEngineering tests
-> typed review resolution tests
-> exact slot contract tests
-> self-contained evidence packaging tests
-> canonical handoff validation tests
-> positive multi-page fixture
-> negative review fixtures
-> negative slot fixtures
-> negative handoff fixtures
-> boundary scans
-> StorefrontBuilder plan-only smoke
-> final inspect proof
-> write commit-linked report
```

Boundary scans:

- [ ] ReverseEngineering has no production project references.
- [ ] StorefrontBuilder does not consume `analysis/agent-handoff/*` yet.
- [ ] ReverseEngineering does not write Razor/CSS/JS storefront output.
- [ ] ReverseEngineering does not write to Starter.
- [ ] ReverseEngineering does not write generated storefront source.
- [ ] No direct Commerce Node browser calls are generated or recommended.
- [ ] No generated `@page` output exists.
- [ ] No `captures/home` hardcode in workflow code.
- [ ] No `plan.Pages.First()` hardcode in workflow code.
- [ ] No reviewed blueprint reference to `.draft.json`.
- [ ] No handoff reference outside `analysis/agent-handoff`.

Report fields:

- [ ] status.
- [ ] tested commit SHA.
- [ ] final HEAD SHA.
- [ ] working tree clean.
- [ ] branch.
- [ ] UTC.
- [ ] .NET version.
- [ ] test summaries.
- [ ] Phase 3A gate result.
- [ ] Phase 3B gate result.
- [ ] Phase 3C gate result.
- [ ] review resolution proof.
- [ ] required slot proof.
- [ ] handoff evidence proof.
- [ ] positive fixture readiness.
- [ ] negative fixture summaries.
- [ ] StorefrontBuilder smoke result.
- [ ] GitHub Actions status: disabled/local proof primary, when applicable.
- [ ] known limitations.
- [ ] Phase 3 closure decision.

Done when:

- [ ] Full gate passes on a clean working tree without skip flags.

## Phase 3D.12 - Documentation Closure And Status Alignment

Goal: docs must match actual code paths and closure state.

Implementation checklist:

- [ ] Update `docs/visual-reverse-engineering-skill/12-StorefrontReverseEngineering-Phase3C-Final-Handoff-Hardening.todo.md`.
- [ ] Update `docs/qa/phase3c-final-handoff-closure.md`.
- [ ] Update `docs/qa/phase3d-final-closure.md`.
- [ ] Update `docs/visual-reverse-engineering-skill/README.md`.
- [ ] Update `docs/visual-reverse-engineering-skill/reference.md`.
- [ ] Update `docs/visual-reverse-engineering-skill/how-to-generate-and-validate.md`.
- [ ] Update `docs/visual-reverse-engineering-skill/explanation-boundaries-and-regeneration.md` if boundary wording changes.
- [ ] Update `docs/architecture/11-storefront-builder.md`.
- [ ] Update `docs/agents/storefront-builder.md`.
- [ ] Correct handoff readiness path everywhere:
  - [ ] use `analysis/agent-handoff/handoff-readiness.json`.
  - [ ] do not use `reports/agent-handoff-readiness.json`.
- [ ] Before full gate passes, statuses must say:
  - [ ] Phase 3D: In progress.
  - [ ] Phase 3 overall: In progress.
- [ ] After full gate passes, statuses may say:
  - [ ] Phase 3A: Complete.
  - [ ] Phase 3B: Complete.
  - [ ] Phase 3C: Complete.
  - [ ] Phase 3D: Complete.
  - [ ] Phase 3 overall: Complete.
- [ ] Do not mark Phase 3D complete before final no-skip clean-head gate passes.

Closure doc must include:

- [ ] tested SHA.
- [ ] clean tree proof.
- [ ] full gate command.
- [ ] test counts.
- [ ] positive fixture details.
- [ ] negative fixture details.
- [ ] handoff schema versions.
- [ ] handoff hashes.
- [ ] known limitations.
- [ ] statement that GitHub Actions are disabled in dev if still true.
- [ ] explicit statement that Phase 4 may begin implementation planning after closure.
- [ ] explicit statement that StorefrontBuilder consumption is still disabled until Phase 4 approved cutover.

Done when:

- [ ] Documentation, closure evidence, and code artifact paths agree.

## Recommended Commit Order

1. [x] `Phase 3D.1 apply typed review decisions`
2. [x] `Phase 3D.2 enforce reviewed blueprint lifecycle`
3. [x] `Phase 3D.3 add exact page slot contracts`
4. [ ] `Phase 3D.4 enforce page composition slots`
5. [ ] `Phase 3D.5 package screenshots and section crops`
6. [ ] `Phase 3D.6 centralize handoff contract`
7. [ ] `Phase 3D.7 harden handoff task contract`
8. [ ] `Phase 3D.8 enforce strict workflow failure`
9. [ ] `Phase 3D.9 add semantic schemas`
10. [ ] `Phase 3D.10 add positive and negative fixtures`
11. [ ] `Phase 3D.11 add final no-skip closure gate`
12. [ ] `Phase 3D.12 update closure docs and statuses`
13. [ ] `Phase 3D final clean-head gate proof`

## Definition Of Done

Review correctness:

- [ ] Modified decisions are applied to reviewed artifact values.
- [ ] Approved decisions preserve proposals.
- [ ] Rejected and Deferred critical items block readiness.
- [ ] Stale and duplicate decisions fail.
- [ ] Resolution manifest records source and decision hashes.

Reviewed blueprint:

- [ ] Draft always exists.
- [ ] Reviewed exists only after blocking review completion.
- [ ] Reviewed references only resolved artifacts.
- [ ] Reviewed has no authoritative `.draft.json` references.
- [ ] Review bundle hash is current.

Page contracts:

- [ ] Exact required slot IDs exist per page.
- [ ] Missing required slot fails.
- [ ] Duplicate non-repeatable slot fails.
- [ ] Extra unapproved section fails.
- [ ] Optional missing slot passes.
- [ ] Behavior ownership conflicts fail.

Visual evidence:

- [ ] Handoff contains page screenshots.
- [ ] Handoff contains major section crops.
- [ ] Evidence manifest validates.
- [ ] Hashes match copied bytes.
- [ ] Required evidence missing fails.

Handoff contract:

- [ ] One canonical required-artifact list exists.
- [ ] Manifest contains all artifact entries and hashes.
- [ ] All references remain under handoff root.
- [ ] Allowed/protected paths do not overlap.
- [ ] Task instructions are complete.
- [ ] Handoff readiness validates every required artifact.

Workflow:

- [ ] Final blockers cause non-zero CLI exit.
- [ ] Inspect reports exact blocker/cause/fix.
- [ ] Positive fixture passes.
- [ ] All negative fixtures fail with expected blocker code.

Boundary:

- [ ] ReverseEngineering remains development-time only.
- [ ] No Razor/CSS/JS storefront generation happens in ReverseEngineering.
- [ ] StorefrontBuilder does not consume Phase 3D output yet.
- [ ] No production project references ReverseEngineering.
- [ ] No Starter writes.
- [ ] No direct Commerce Node browser calls are generated or recommended.

Closure:

- [ ] Full Phase 3D gate passes without skip flags.
- [ ] Working tree is clean.
- [ ] Tested SHA equals final HEAD.
- [ ] No commit exists after tested SHA.
- [ ] Phase 3D status is Complete.
- [ ] Phase 3 overall status is Complete.
- [ ] Final closure doc is committed.

## Final Phase 3 Closure Statement

Phase 3 may be closed only when this statement is true:

```text
A complete multi-page ecommerce reference can be captured, analyzed,
reviewed, mapped to exact Storefront Presentation slots, packaged with
self-contained visual evidence and protected-file constraints, and validated
as a safe Phase 4 input without requiring an agent to reinterpret architecture,
apply review decisions, or invent ecommerce behavior.
```

At closure, the approved handoff root is:

```text
analysis/agent-handoff/
```

The next phase remains:

```text
Phase 4 - Agent-Assisted Storefront Visual Generation
```

Phase 4 must consume the reviewed handoff contract, not raw Phase 3 internals.

## Decision Audit Trail

| # | Phase | Decision | Classification | Principle | Rationale | Rejected |
|---|-------|----------|----------------|-----------|-----------|----------|
| 1 | CEO | Keep Phase 3D as a closure/correctness phase, not a new generation phase. | Auto-decided | Outcome over scope creep | The user wants Phase 3 closure quality; generation belongs to Phase 4. | Combining Phase 3D with Phase 4 consumption. |
| 2 | Eng | Add typed review resolvers instead of copying draft artifacts. | Auto-decided | Fix root cause | Current reviewed artifacts can preserve draft values after Modified decisions. | Keep copy-based reviewed writer. |
| 3 | Eng | Make reviewed blueprint conditional on zero blocking review/readiness issues. | Auto-decided | Correctness before convenience | A reviewed blueprint with blockers is unsafe as Phase 4 input. | Always write reviewed blueprint and rely on later validation. |
| 4 | Eng | Use exact page slot IDs for validation while keeping visual labels as descriptions only. | Auto-decided | Contract stability | Free-form labels are ambiguous for generated storefront implementation. | Use `RequiredVisualRegions` as validation source of truth. |
| 5 | Eng | Centralize handoff required artifacts in one contract. | Auto-decided | Single source of truth | Assembler and validator lists already drifted. | Maintain separate lists. |
| 6 | DX | Package screenshots and section crops inside handoff. | Auto-decided | Reduce consumer guesswork | Phase 4 should not chase raw capture paths or infer what evidence matters. | Store only references to raw capture folders. |
| 7 | DX | Final gate must have no skip flags and must record clean HEAD. | Auto-decided | Release evidence integrity | Closure proof cannot depend on skipped validations or a stale SHA. | Reuse Phase 3C gate command with skip flags. |

## GSTACK REVIEW REPORT

### Plan Summary

This plan converts the Phase 3D review into a concrete closure checklist. It fixes reviewed artifact correctness, exact slot contracts, self-contained handoff evidence, canonical artifact validation, strict workflow failures, and no-skip closure proof.

### Review Scores

- CEO: Pass with focus. The phase is scoped to closure blockers and avoids adding generation capability.
- Design: Skipped. No UI design implementation is planned in this phase.
- Eng: Pass with required fixes. The plan addresses the actual code gaps found in review resolution, blueprint lifecycle, handoff validation, and gate semantics.
- DX: Pass with required fixes. The plan makes Phase 4 input self-contained and reduces agent/developer guesswork.

### Cross-Phase Themes

- Contract precision: exact slot IDs, canonical handoff artifacts, and hash-bound reviewed outputs appear across architecture, engineering, and DX concerns.
- Closure evidence: the plan treats clean HEAD, no-skip gate, and path/status alignment as release-quality blockers, not documentation polish.

### Implementation Tasks

- [x] Implement D1 typed review artifact resolution.
- [x] Implement D2 reviewed blueprint lifecycle.
- [x] Implement D3 exact page slot contracts.
- [ ] Implement D4 page composition slot enforcement.
- [ ] Implement D5 self-contained visual evidence packaging.
- [ ] Implement D6 canonical handoff contract.
- [ ] Implement D7 handoff task contract.
- [ ] Implement D8 strict workflow failure semantics.
- [ ] Implement D9 schema and semantic validation.
- [ ] Implement D10 positive and negative proof fixtures.
- [ ] Implement D11 no-skip final closure gate.
- [ ] Implement D12 docs/status closure.

# StorefrontReverseEngineering Phase 3B/3C Production Blocker Closure

Status: In progress
Owner area: `tools/BlazorShop.AI.StorefrontReverseEngineering`
Target folder: `docs/visual-reverse-engineering-skill`
Created: 2026-08-06

Production verification URL:

```text
https://www.kindredcoast.com/
```

Primary goal: close the remaining Phase 3B/3C blockers that keep the KindredCoast production reverse-engineering run at `completed-with-blockers` even after Phase 3A readiness passes. The fix must make reviewed Visual Blueprint v1 and agent handoff readiness truthful enough for Phase 4 StorefrontBuilder consumption, without bypassing review, weakening slot validators, mutating Starter, or hand-editing production artifacts.

This plan was prepared with the `/autoplan` review method: apply the CEO, Design, Eng, and DX review lenses, auto-decide routine implementation choices using the 6 decision principles, and surface the few tradeoffs in the decision audit trail.

## Current Evidence

Latest observed KindredCoast state:

- Phase 3A readiness: passed.
- Validation: passed.
- Production runner final status: `completed-with-blockers`.
- Generation readiness: `false`.
- Review queue count: 22.
- Review decisions: empty.
- Review decision totals: `approved=0; modified=0; rejected=0; deferred=0; stale=0`.
- Review resolution unresolved blocking count: 12.

Blocking findings in `artifacts/storefront-reverse-engineering/projects/kindredcoast/reports/generation-readiness.json`:

- `missing-review-decisions`
- `reviewed-blueprint-not-resolved`
- `missing-mapping-for-critical-region`
- `missing-required-slot` for `layout.footer`
- `duplicate-non-repeatable-slot` for `home.sections`
- `unapproved-extra-section` for `section-04`
- `reviewed-slot-mapping-orphan` for `family-hero`
- final `reviewed-blueprint-not-resolved` because the reviewed blueprint was not assembled

Root-cause artifacts:

- `review/review-decisions.json` exists but has `decisions: []`.
- `analysis/resolved/review-resolution-manifest.json` reports 12 blocking unresolved items.
- `analysis/resolved/presentation-mappings.reviewed.json` maps `family-hero` to `layout.header`, with `sourceSectionId: section-03`, even though the page composition uses it from `section-04`.
- `family-footer` maps to `layout.footer`, but has `sourcePageId: unknown` and `sourceSectionId: unknown`.
- `analysis/resolved/page-compositions.reviewed.json` has no authoritative footer section, repeats home content as independent `home.sections` observations, and leaves product/announcement content without reviewed slot ownership.

## Problem Statement

The production run is no longer failing because of browser capture or offscreen evidence. It is correctly failing because Phase 3B/3C artifacts are not ready for generation handoff:

1. Missing decisions are converted to deferred review items by `ReviewDecisionApplier`.
2. Unsupported ecommerce-critical patterns remain unresolved.
3. Presentation mapping source provenance is not section-accurate.
4. Home composition is not normalizing page body sections into a valid `home.sections` container.
5. Footer evidence exists but is not source-bound into the page composition.

This is a pipeline correctness issue. It must be fixed at the artifact producer and review workflow layers. The validators should remain strict.

## Autoplan Review

### CEO Review

Premises accepted:

- The target problem is Phase 3B/3C production artifact truth, not Phase 3A capture readiness.
- The correct outcome is a truthful pass through reviewed blueprint and handoff readiness, not a runner status override.
- KindredCoast is the production smoke proof because it exposed the real artifact mismatch.

Premise challenged:

- "Write review decisions and rerun" is necessary but incomplete. It will not fix hero/header/footer/source mismatches by itself.

What already exists:

| Area | Existing behavior |
| --- | --- |
| Review queue | `ConfidenceScorer` writes `review/review-queue.json` and creates an empty `review/review-decisions.json` only when missing. |
| Decision validation | `ReviewDecisionApplier` validates status, metadata, duplicates, and source hashes. |
| Reviewed artifacts | `ResolvedReviewArtifactWriter` applies Approved/Modified/Rejected/Deferred decisions to resolved artifacts. |
| Blueprint lifecycle | `BlueprintV1Assembler` only writes reviewed blueprint when generation readiness has no blockers. |
| Slot validation | `PageCompositionSlotValidator` blocks missing required slots, duplicate non-repeatable slots, unapproved sections, orphan mappings, protected targets, and behavior ownership leaks. |
| Slot resolution | `SectionSlotResolver` resolves reviewed mappings, exact Storefront contract targets, and approved visual extensions. |
| Contract source | `StorefrontPatternContractBuilder` defines required slots including `layout.header`, `home.sections`, and `layout.footer` for `home`. |

Not in scope:

- Storefront runtime, Commerce Node, Control Plane, cart, checkout, account, payment, or order behavior changes.
- StorefrontBuilder Phase 4 generation changes.
- Mutating `BlazorShop.Storefront.Starter` with KindredCoast-specific output.
- Hand-editing ignored production artifacts as the fix.
- Relaxing or deleting `reviewed-slot-mapping-orphan`, `missing-required-slot`, `duplicate-non-repeatable-slot`, or `unapproved-extra-section`.

### Design Review

There is no end-user UI scope. This work affects machine-readable visual interpretation artifacts and developer/operator reports. Design concerns are limited to report clarity and review-pack ergonomics.

Design decisions:

- Keep problem/cause/fix output explicit in `inspect` and production runner reports.
- Prefer a review decision report that says which items were auto-resolved, manually required, or still blocked.
- Do not add a UI for review decisions in this phase.

### Engineering Review

Architecture graph:

```text
Phase 3A evidence
  -> analysis/pages/* sections + ecommerce regions
  -> component candidates
  -> PresentationMapper
      -> presentation-mappings.draft.json
      -> unsupported-patterns.json
  -> ConfidenceScorer
      -> review-queue.json
      -> review-decisions.json
  -> ReviewDecisionApplier
      -> analysis/resolved/*
  -> BlueprintV1Assembler
      -> page-compositions.reviewed.json
      -> visual-blueprint.v1.reviewed.json
      -> reports/generation-readiness.json
  -> AgentHandoffAssembler
      -> analysis/agent-handoff/*
```

Primary engineering risks:

- A broad auto-approve path would make bad mappings look reviewed.
- Mapping by the first ecommerce region is too coarse for pages where header, hero, product cards, and footer share evidence families or viewport ordering.
- Making `home.sections` globally repeatable is simpler but may weaken the site-level page contract. A better fix is to represent it once as the home body container and let child sections be repeated visual nodes inside that slot.
- Production `-Resume` can preserve stale artifacts. Final proof must include a fresh run or explicit forced downstream rerun.

### DX Review

Developer/operator concerns:

- The current failure says "complete review decisions", but not which artifacts are structurally wrong after decisions are applied.
- Operators need a command that can prove whether blockers are from missing decisions, unsupported runtime behavior, slot provenance, or stale resume state.
- Production proof should fail non-zero when blockers remain, through `-FailOnBlockers`, so CI/manual scripts cannot mistake `completed-with-blockers` for success.

## Decision Audit Trail

| # | Phase | Decision | Classification | Principle | Rationale | Rejected |
|---|-------|----------|----------------|-----------|-----------|----------|
| 1 | Scope | Fix Phase 3B/3C artifact producers, not the production runner final-status logic. | Auto-decided | Explicit over clever | The runner is reporting the truth. The bad state is in reviewed decisions, mapping provenance, and page composition. | Treating `completed-with-blockers` as success. |
| 2 | Review | Add a controlled safe-review materialization path, not blanket auto-approval. | Auto-decided | Choose completeness | Production smoke needs non-interactive proof, but unsupported runtime behavior and stale hashes must still block. | Auto-approving every review queue item. |
| 3 | Mapping | Make mapping source selection evidence/section-aware. | Auto-decided | DRY | Existing section/ecommerce artifacts already contain the source data; reuse them instead of inventing external override files. | KindredCoast-specific artifact patching. |
| 4 | Slot validation | Keep `PageCompositionSlotValidator` strict and make producers satisfy it. | Auto-decided | Choose completeness | The validator caught real handoff risks: orphan mappings, missing footer, duplicate slots, and unapproved sections. | Weakening validator severity. |
| 5 | Home body | Treat `home.sections` as the single home content container, with child sections mapped or approved inside it. | Taste | Explicit over clever | This preserves the page contract and avoids converting the page-level slot into an unbounded repeatable slot. | Making `home.sections` repeatable globally. |
| 6 | Production proof | Final proof must run KindredCoast with `-FailOnBlockers` after a fresh forced run or forced downstream rebuild. | Auto-decided | Bias toward action | A zero exit code with blockers is useful for Phase 3A smoke, but not for this closure. | Only running `-Resume` and inspecting old artifacts. |

## Phase Order

1. Phase 3B/3C.0 - Baseline And Root-Cause Lock
2. Phase 3B/3C.1 - Regression Tests For Current Production Blockers
3. Phase 3B/3C.2 - Source-Aware Presentation Mapping
4. Phase 3B/3C.3 - Safe Review Decision Materialization
5. Phase 3B/3C.4 - Home Composition And Footer Slot Closure
6. Phase 3B/3C.5 - Handoff Readiness And Inspect DX
7. Phase 3B/3C.6 - Production KindredCoast Proof
8. Phase 3B/3C.7 - Docs, Checklist, And Final Closure Evidence

Each implementation phase must be committed separately. Stage only intentional tracked files. Do not stage ignored production artifacts under `artifacts/` or `obj/` unless a phase explicitly promotes a small tracked evidence document.

## Phase 3B/3C.0 - Baseline And Root-Cause Lock

Goal: preserve the current production failure and lock the exact blocker set before changing behavior.

Implementation checklist:

- [x] Run `git status --short` and record unrelated changes.
- [x] Confirm latest HEAD and branch before edits.
- [x] Read the latest KindredCoast production report.
- [x] Read `artifacts/storefront-reverse-engineering/projects/kindredcoast/reports/generation-readiness.json`.
- [x] Read `review/review-queue.json`, `review/review-decisions.json`, and `review/reviewed-items.json` if present.
- [x] Record review queue counts by item type and blocking state.
- [x] Read `analysis/resolved/review-resolution-manifest.json`.
- [x] Read `analysis/resolved/page-compositions.reviewed.json`.
- [x] Read `analysis/resolved/presentation-mappings.reviewed.json`.
- [x] Confirm the specific source mismatch for `family-hero`.
- [x] Confirm `family-footer` has `unknown` source page/section.
- [x] Confirm `layout.footer` is missing from observed authoritative slots.
- [x] Confirm the current blocker set is Phase 3B/3C only and Phase 3A readiness still passes.
- [x] Add a short baseline section to this file with exact report paths and blocker counts.

Baseline evidence:

| Item | Observed value |
| --- | --- |
| Branch / HEAD | `master` / `ec84f398` |
| Unrelated pre-existing change | `?? scripts/reverse-engineering/readme.md` |
| Latest production report | `artifacts/storefront-reverse-engineering/reports/storefront-reverse-engineering-production-kindredcoast-20260806115937.md` |
| Production status | `completed-with-blockers` |
| Run / inspect / validate exit codes | `3` / `0` / `0` |
| Phase 3A readiness | `passed: true` in `reports/readiness-report.json` |
| Phase 3B generation readiness | `passed: false` in `reports/generation-readiness.json` |
| Generation blocker counts | `missing-review-decisions=1`, `reviewed-blueprint-not-resolved=2`, `missing-mapping-for-critical-region=1`, `missing-required-slot=1`, `duplicate-non-repeatable-slot=1`, `unapproved-extra-section=1`, `reviewed-slot-mapping-orphan=1` |
| Review queue | `22` items, `12` blocking |
| Review queue by type | `component families false=1`, `ecommerce roles true=1`, `page archetype false=1`, `sections true=1`, `semantic tokens false=8`, `semantic tokens true=4`, `unsupported patterns true=6` |
| Review decisions | `0` decisions |
| Reviewed items | `22` deferred, `blocksReadiness=true` |
| Review resolution | `resolvedItemCount=0`, `blockingUnresolvedCount=12` |
| Blocking unresolved items | `region:home:region-02`, `section:home:section-05`, `token:accent-primary`, `token:accent-secondary`, `token:surface-elevated`, `token:text-muted`, `unsupported:family-account-trigger`, `unsupported:family-announcement-bar`, `unsupported:family-cart-trigger`, `unsupported:family-price-display`, `unsupported:family-product-card`, `unsupported:family-product-image` |
| Hero mapping mismatch | `family-hero` maps to `layout.header`, `sourcePageId=home`, `sourceSectionId=section-03`, while composition uses it for `section-04` |
| Footer mapping mismatch | `family-footer` maps to `layout.footer` but has `sourcePageId=unknown`, `sourceSectionId=unknown` |
| Navigation source gap | `family-navigation` maps to `layout.main-navigation` but has `sourcePageId=unknown`, `sourceSectionId=unknown` |

Checks:

```powershell
Get-Content artifacts\storefront-reverse-engineering\projects\kindredcoast\reports\generation-readiness.json -Raw
Get-Content artifacts\storefront-reverse-engineering\projects\kindredcoast\analysis\resolved\review-resolution-manifest.json -Raw
Get-Content artifacts\storefront-reverse-engineering\projects\kindredcoast\analysis\resolved\page-compositions.reviewed.json -Raw
Get-Content artifacts\storefront-reverse-engineering\projects\kindredcoast\analysis\resolved\presentation-mappings.reviewed.json -Raw
```

Done when:

- [x] The plan contains baseline evidence paths and exact blocker counts.
- [x] No production artifact has been hand-edited.

Commit:

```powershell
git add docs/visual-reverse-engineering-skill/23-StorefrontReverseEngineering-Phase3B3C-Production-Blocker-Closure.todo.md
git commit -m "Phase 3B3C.0: record production blocker baseline"
```

## Phase 3B/3C.1 - Regression Tests For Current Production Blockers

Goal: add failing tests that reproduce the KindredCoast blocker shape without requiring the external website.

Implementation checklist:

- [x] Add a fixture or test builder that creates a home page with header, announcement, hero, product-card content, and footer evidence.
- [x] Add a mapping test proving hero evidence must not map to `layout.header`.
- [x] Add a mapping test proving footer evidence with known section evidence resolves to `layout.footer` with non-unknown `sourcePageId` and `sourceSectionId`.
- [x] Add a mapping test proving account/cart trigger visual candidates map only to existing Presentation-safe layout slots or remain unsupported with blocking status.
- [x] Add a readiness test reproducing `reviewed-slot-mapping-orphan` when a mapping source section differs from the composition node.
- [x] Add a readiness test reproducing missing footer from unknown footer provenance.
- [x] Add a readiness test for multiple home body sections under one `home.sections` container.
- [x] Add a review workflow test proving missing decisions still block.
- [x] Add a review workflow test proving safe generated decisions include source artifact ID, hash, reviewer metadata, and stable decision ID.
- [x] Keep all new tests deterministic and fixture/local only.

Candidate test files:

- `tools/BlazorShop.AI.StorefrontReverseEngineering/tests/BlazorShop.AI.StorefrontReverseEngineering.Tests/PresentationMappingTests.cs`
- `tools/BlazorShop.AI.StorefrontReverseEngineering/tests/BlazorShop.AI.StorefrontReverseEngineering.Tests/BlueprintV1ReadinessTests.cs`
- `tools/BlazorShop.AI.StorefrontReverseEngineering/tests/BlazorShop.AI.StorefrontReverseEngineering.Tests/ConfidenceReviewTests.cs`
- `tools/BlazorShop.AI.StorefrontReverseEngineering/tests/BlazorShop.AI.StorefrontReverseEngineering.Tests/PageCompositionSlotValidatorSharedResolverTests.cs`

Checks:

```powershell
dotnet test tools\BlazorShop.AI.StorefrontReverseEngineering\tests\BlazorShop.AI.StorefrontReverseEngineering.Tests\BlazorShop.AI.StorefrontReverseEngineering.Tests.csproj --no-restore --filter "FullyQualifiedName~PresentationMappingTests|FullyQualifiedName~BlueprintV1ReadinessTests|FullyQualifiedName~ConfidenceReviewTests|FullyQualifiedName~PageCompositionSlotValidatorSharedResolverTests" --blame-hang --blame-hang-timeout 5m
```

Phase 1 regression evidence:

```powershell
dotnet test tools\BlazorShop.AI.StorefrontReverseEngineering\tests\BlazorShop.AI.StorefrontReverseEngineering.Tests\BlazorShop.AI.StorefrontReverseEngineering.Tests.csproj --filter "FullyQualifiedName~PresentationMappingTests|FullyQualifiedName~ConfidenceReviewTests|FullyQualifiedName~PageCompositionSlotValidatorSharedResolverTests" --no-restore
```

Expected pre-fix result: failed with 3 blocker reproductions:

- `PresentationMapping_UsesEvidenceOverlapBeforeFirstRegion`: actual `section-header`, expected `section-product`.
- `PresentationMapping_FooterFallsBackToSectionEvidenceWhenRegionMissing`: actual `sourcePageId=unknown`, expected `home`.
- `HomeBodyChildSectionsDoNotDuplicateHomeSectionsContainer`: actual `duplicate-non-repeatable-slot` for `home.sections`.

Done when:

- [x] New tests fail for the current behavior before implementation changes.
- [x] Existing unrelated tests are not modified to hide the blocker.

Commit:

```powershell
git add tools/BlazorShop.AI.StorefrontReverseEngineering/tests/BlazorShop.AI.StorefrontReverseEngineering.Tests
git commit -m "Phase 3B3C.1: cover production blocker regressions"
```

## Phase 3B/3C.2 - Source-Aware Presentation Mapping

Goal: make `PresentationMapper` choose the correct source page, section, ecommerce region, target slot, and generated path for each visual candidate.

Implementation checklist:

- [x] Replace first-region source selection with deterministic source scoring.
- [x] Score candidate-to-region matches using evidence ID overlap, source component family ID, section ID, page ID, viewport coverage, and role compatibility.
- [x] Prefer exact evidence overlap over role-only matches.
- [x] Prevent layout mappings from absorbing page body evidence.
- [x] Ensure hero/home content maps to `home.sections` or an approved visual extension path, not `layout.header`.
- [x] Ensure footer maps to `layout.footer` with the actual source page and source section when footer section evidence exists.
- [x] Ensure navigation, cart badge, and account menu use `layout.main-navigation`, `layout.cart-badge`, and `layout.account-menu` only when catalog support and source section are compatible.
- [x] Keep unsupported runtime-owned behavior blocking.
- [x] Keep direct Storefront API/browser action detection blocking.
- [x] Add reason codes for evidence overlap, source-section binding, and fallback role match.
- [x] Update unsupported pattern output so supported ecommerce visual patterns do not remain in `unsupported-patterns.json`.
- [x] Update or add tests proving no `unknown` source is emitted when a known section exists.

Candidate files:

- `tools/BlazorShop.AI.StorefrontReverseEngineering/Analysis/Mapping/PresentationMapper.cs`
- `tools/BlazorShop.AI.StorefrontReverseEngineering/Analysis/Presentation/PresentationComponentCatalogBuilder.cs`
- `tools/BlazorShop.AI.StorefrontReverseEngineering/Analysis/Components/VisualComponentCandidateDetector.cs`
- `tools/BlazorShop.AI.StorefrontReverseEngineering/Analysis/Ecommerce/EcommerceRegionClassifier.cs`

Checks:

```powershell
dotnet test tools\BlazorShop.AI.StorefrontReverseEngineering\tests\BlazorShop.AI.StorefrontReverseEngineering.Tests\BlazorShop.AI.StorefrontReverseEngineering.Tests.csproj --no-restore --filter "FullyQualifiedName~PresentationMappingTests|FullyQualifiedName~PresentationCatalogBuilderTests|FullyQualifiedName~Phase3BFixtureTests" --blame-hang --blame-hang-timeout 5m
```

Phase 2 evidence:

```powershell
dotnet test tools\BlazorShop.AI.StorefrontReverseEngineering\tests\BlazorShop.AI.StorefrontReverseEngineering.Tests\BlazorShop.AI.StorefrontReverseEngineering.Tests.csproj --filter "FullyQualifiedName~PresentationMappingTests|FullyQualifiedName~PresentationCatalogBuilderTests|FullyQualifiedName~Phase3BFixtureTests" --no-restore
```

Result: passed, 21/21.

Done when:

- [x] Hero no longer maps to `layout.header`.
- [x] Footer no longer has `sourcePageId: unknown` when source evidence is page-bound.
- [x] Cart/account/product-card/price-display patterns either have valid Presentation mappings or remain explicitly blocked with actionable reason.
- [x] New mapping tests pass.

Commit:

```powershell
git add tools/BlazorShop.AI.StorefrontReverseEngineering/Analysis tools/BlazorShop.AI.StorefrontReverseEngineering/tests/BlazorShop.AI.StorefrontReverseEngineering.Tests
git commit -m "Phase 3B3C.2: make presentation mapping source aware"
```

## Phase 3B/3C.3 - Safe Review Decision Materialization

Goal: add a deterministic, auditable way to resolve safe review items for non-interactive production proof while keeping unsafe/unsupported items blocking.

Implementation checklist:

- [x] Add a review decision materializer service or command that reads `review/review-queue.json`.
- [x] Generate decisions only for safe visual-only items that pass source hash validation.
- [x] Preserve exact `sourceArtifactId`, `sourceArtifactHash`, `reviewer`, `reviewerNote`, and stable `decisionId`.
- [x] Use `Approved` only when the original proposal is already valid.
- [x] Use `Modified` only when the modified value is deterministic, schema-valid, and narrower than the original proposal.
- [x] Never auto-approve direct Storefront API calls, runtime-owned behavior, stale hashes, protected paths, or unknown source provenance.
- [x] Emit a machine-readable review decision summary with counts: approved, modified, blocked, skipped, stale.
- [x] Emit human-readable problem/cause/fix output for skipped blocking items.
- [x] Add CLI help text for the new command or flag.
- [x] Wire the production script only if an explicit flag is provided.
- [x] Prefer a flag name that makes the boundary clear, such as `-ResolveSafeReviewItems`.
- [x] Keep default behavior unchanged for users who want manual review.
- [x] Add tests for safe approvals, deterministic modifications, stale hash failure, duplicate decision prevention, and unsafe item refusal.

Candidate files:

- `tools/BlazorShop.AI.StorefrontReverseEngineering/Analysis/Review/ConfidenceScorer.cs`
- `tools/BlazorShop.AI.StorefrontReverseEngineering/Application/VisualProjectService.cs`
- `tools/BlazorShop.AI.StorefrontReverseEngineering/Program.cs`
- `scripts/reverse-engineering/run-storefront-reverse-engineering-production.ps1`
- `tools/BlazorShop.AI.StorefrontReverseEngineering/tests/BlazorShop.AI.StorefrontReverseEngineering.Tests/ConfidenceReviewTests.cs`
- `tools/BlazorShop.AI.StorefrontReverseEngineering/tests/BlazorShop.AI.StorefrontReverseEngineering.Tests/EndToEndCliTests.cs`

Checks:

```powershell
dotnet test tools\BlazorShop.AI.StorefrontReverseEngineering\tests\BlazorShop.AI.StorefrontReverseEngineering.Tests\BlazorShop.AI.StorefrontReverseEngineering.Tests.csproj --no-restore --filter "FullyQualifiedName~ConfidenceReviewTests|FullyQualifiedName~EndToEndCliTests|FullyQualifiedName~Phase3CliProofCollectionTests" --blame-hang --blame-hang-timeout 5m
```

Phase 3 evidence:

```powershell
dotnet test tools\BlazorShop.AI.StorefrontReverseEngineering\tests\BlazorShop.AI.StorefrontReverseEngineering.Tests\BlazorShop.AI.StorefrontReverseEngineering.Tests.csproj --filter "FullyQualifiedName~ConfidenceReviewTests|FullyQualifiedName~EndToEndCliTests|FullyQualifiedName~Phase3CliProofCollectionTests" --no-restore
```

Result: passed, 40/40.

Done when:

- [x] Missing review decisions still block by default.
- [x] Safe review materialization can write valid decisions for safe items.
- [x] Unsafe/unsupported items still block and explain why.
- [x] The production runner can opt in without changing default semantics.

Commit:

```powershell
git add tools/BlazorShop.AI.StorefrontReverseEngineering scripts/reverse-engineering/run-storefront-reverse-engineering-production.ps1
git commit -m "Phase 3B3C.3: add safe review decision materialization"
```

## Phase 3B/3C.4 - Home Composition And Footer Slot Closure

Goal: make reviewed page composition satisfy exact slot contracts without weakening slot enforcement.

Implementation checklist:

- [x] Update page composition assembly so `home.sections` is represented as one page body slot.
- [x] Keep individual hero, announcement, product rail, promo, newsletter, and editorial sections as child visual nodes under the home body slot when appropriate.
- [x] Do not count each child section as a separate non-repeatable `home.sections` source.
- [x] Resolve footer as a required shared layout section when footer evidence exists.
- [x] Add or update approved visual extension handling for presentation-only announcement/promo sections.
- [x] Ensure approved visual extensions require ID, reason, visual-only operations, and no protected behavior markers.
- [x] Make product-card/product-image/price-display on home resolve as visual content inside `home.sections` or as explicit allowed/repeatable catalog children only when the contract allows it.
- [x] Do not make `home.sections` globally repeatable.
- [x] Keep `catalog.product-card` repeatable for listing pages.
- [x] Add tests proving valid home sections do not trigger `duplicate-non-repeatable-slot`.
- [x] Add tests proving true duplicate non-repeatable slots still fail.
- [x] Add tests proving missing footer still fails when footer evidence is absent and the contract requires it.

Candidate files:

- `tools/BlazorShop.AI.StorefrontReverseEngineering/Analysis/Blueprint/BlueprintV1Assembler.cs`
- `tools/BlazorShop.AI.StorefrontReverseEngineering/Analysis/Blueprint/PageCompositionSlotValidator.cs`
- `tools/BlazorShop.AI.StorefrontReverseEngineering/Analysis/Handoff/SectionSlotResolver.cs`
- `tools/BlazorShop.AI.StorefrontReverseEngineering/Analysis/StorefrontPattern/StorefrontPatternContractBuilder.cs`
- `tools/BlazorShop.AI.StorefrontReverseEngineering/tests/BlazorShop.AI.StorefrontReverseEngineering.Tests/BlueprintV1ReadinessTests.cs`
- `tools/BlazorShop.AI.StorefrontReverseEngineering/tests/BlazorShop.AI.StorefrontReverseEngineering.Tests/SectionSlotResolverTests.cs`

Checks:

```powershell
dotnet test tools\BlazorShop.AI.StorefrontReverseEngineering\tests\BlazorShop.AI.StorefrontReverseEngineering.Tests\BlazorShop.AI.StorefrontReverseEngineering.Tests.csproj --no-restore --filter "FullyQualifiedName~BlueprintV1ReadinessTests|FullyQualifiedName~SectionSlotResolverTests|FullyQualifiedName~PageCompositionSlotValidatorSharedResolverTests|FullyQualifiedName~Phase3DPositiveEndToEndTests|FullyQualifiedName~Phase3DNegativeMutationTests" --blame-hang --blame-hang-timeout 5m
```

Phase 4 evidence:

```powershell
dotnet test tools\BlazorShop.AI.StorefrontReverseEngineering\tests\BlazorShop.AI.StorefrontReverseEngineering.Tests\BlazorShop.AI.StorefrontReverseEngineering.Tests.csproj --filter "FullyQualifiedName~BlueprintV1ReadinessTests|FullyQualifiedName~SectionSlotResolverTests|FullyQualifiedName~PageCompositionSlotValidatorSharedResolverTests|FullyQualifiedName~Phase3DPositiveEndToEndTests|FullyQualifiedName~Phase3DNegativeMutationTests" --no-restore
```

Result: passed, 46/46.

Done when:

- [x] Valid home composition contains `layout.header`, one `home.sections`, and `layout.footer`.
- [x] Valid KindredCoast-like home content no longer trips `duplicate-non-repeatable-slot`.
- [x] Hero no longer targets `Components/Layout/MainLayout.razor` through `layout.header`.
- [x] Footer is authoritative and source-bound.
- [x] Negative slot mutation tests still fail for real contract violations.

Commit:

```powershell
git add tools/BlazorShop.AI.StorefrontReverseEngineering
git commit -m "Phase 3B3C.4: close home composition slot blockers"
```

## Phase 3B/3C.5 - Handoff Readiness And Inspect DX

Goal: make inspect/report output explain Phase 3B/3C blockers and prove the final handoff path can pass only after reviewed artifacts are clean.

Implementation checklist:

- [ ] Update `inspect` output for review decision materialization state.
- [ ] Show separate statuses for Phase 3A readiness, Phase 3B generation readiness, and Phase 3C handoff readiness.
- [ ] Add problem/cause/fix guidance for:
  - [ ] missing safe review materialization flag;
  - [ ] unsafe unsupported pattern;
  - [ ] orphan reviewed mapping;
  - [ ] missing footer slot;
  - [ ] duplicate non-repeatable home slot;
  - [ ] stale resume artifacts.
- [ ] Ensure `assemble-blueprint-v1` fails step status when readiness blockers remain.
- [ ] Ensure `validate-agent-handoff-readiness` does not run against missing reviewed blueprint as if it were a Phase 3A issue.
- [ ] Add CLI tests for the improved messages.
- [ ] Add report assertions for production runner problem/cause/fix sections.
- [ ] Preserve `completed-with-blockers` for non-`-FailOnBlockers` Phase 3A smoke runs.
- [ ] Ensure `-FailOnBlockers` exits non-zero when any Phase 3B/3C blocker remains.

Candidate files:

- `tools/BlazorShop.AI.StorefrontReverseEngineering/Application/VisualProjectService.cs`
- `tools/BlazorShop.AI.StorefrontReverseEngineering/Application/VisualProjectWorkflowSteps.cs`
- `scripts/reverse-engineering/run-storefront-reverse-engineering-production.ps1`
- `tools/BlazorShop.AI.StorefrontReverseEngineering/tests/BlazorShop.AI.StorefrontReverseEngineering.Tests/EndToEndCliTests.cs`
- `tools/BlazorShop.AI.StorefrontReverseEngineering/tests/BlazorShop.AI.StorefrontReverseEngineering.Tests/Phase3BCliDxTests.cs`

Checks:

```powershell
dotnet test tools\BlazorShop.AI.StorefrontReverseEngineering\tests\BlazorShop.AI.StorefrontReverseEngineering.Tests\BlazorShop.AI.StorefrontReverseEngineering.Tests.csproj --no-restore --filter "FullyQualifiedName~EndToEndCliTests|FullyQualifiedName~Phase3BCliDxTests|FullyQualifiedName~Phase3CliProofCollectionTests" --blame-hang --blame-hang-timeout 5m
```

Done when:

- [ ] Inspect distinguishes Phase 3A pass from Phase 3B/3C blockers.
- [ ] Reports say exactly what failed, why, and how to fix it.
- [ ] `-FailOnBlockers` can be used as the strict production closure gate.

Commit:

```powershell
git add tools/BlazorShop.AI.StorefrontReverseEngineering scripts/reverse-engineering/run-storefront-reverse-engineering-production.ps1
git commit -m "Phase 3B3C.5: improve blocker inspect diagnostics"
```

## Phase 3B/3C.6 - Production KindredCoast Proof

Goal: prove the fix on the real KindredCoast site with strict blocker failure enabled.

Implementation checklist:

- [ ] Build the ReverseEngineering tool.
- [ ] Run focused tests after all code changes.
- [ ] Run the Phase 3B gate.
- [ ] Run the Phase 3C final handoff gate if feasible in the current environment.
- [ ] Run a fresh KindredCoast production workflow, not only stale `-Resume`.
- [ ] Use the safe review materialization flag only after mapping/composition fixes are in place.
- [ ] Run strict production proof with `-FailOnBlockers`.
- [ ] Inspect final project state.
- [ ] Verify `reports/readiness-report.json` passed.
- [ ] Verify `reports/generation-readiness.json` passed.
- [ ] Verify `analysis/visual-blueprint.v1.reviewed.json` exists.
- [ ] Verify `analysis/agent-handoff/handoff-readiness.json` exists and passed.
- [ ] Verify unresolved blocking review count is zero.
- [ ] Verify no unsupported critical pattern remains unreviewed.
- [ ] Verify no `reviewed-slot-mapping-orphan`, `missing-required-slot`, `duplicate-non-repeatable-slot`, or `unapproved-extra-section` findings remain.
- [ ] Record final report path and command output summary in this plan.

Checks:

```powershell
dotnet build tools\BlazorShop.AI.StorefrontReverseEngineering\BlazorShop.AI.StorefrontReverseEngineering.csproj

dotnet test tools\BlazorShop.AI.StorefrontReverseEngineering\tests\BlazorShop.AI.StorefrontReverseEngineering.Tests\BlazorShop.AI.StorefrontReverseEngineering.Tests.csproj --no-restore --filter "FullyQualifiedName~PresentationMappingTests|FullyQualifiedName~ConfidenceReviewTests|FullyQualifiedName~BlueprintV1ReadinessTests|FullyQualifiedName~EndToEndCliTests|FullyQualifiedName~Phase3BCliDxTests|FullyQualifiedName~Phase3DPositiveEndToEndTests|FullyQualifiedName~Phase3DNegativeMutationTests" --blame-hang --blame-hang-timeout 5m

powershell -NoProfile -ExecutionPolicy Bypass -File scripts\qa\run-storefront-reverse-engineering-phase3b-gate.ps1

powershell -NoProfile -ExecutionPolicy Bypass -File scripts\qa\run-storefront-reverse-engineering-phase3c-final-handoff-gate.ps1

powershell -NoProfile -ExecutionPolicy Bypass -File scripts\reverse-engineering\run-storefront-reverse-engineering-production.ps1 -Url "https://www.kindredcoast.com/" -Name "KindredCoast" -Force -ResolveSafeReviewItems -FailOnBlockers -CommandTimeoutSeconds 900
```

If the exact production script flag name differs after implementation, update this plan before marking the checkbox complete.

Done when:

- [ ] Strict KindredCoast production run exits `0`.
- [ ] Final status is success, not `completed-with-blockers`.
- [ ] All Phase 3A, Phase 3B, and Phase 3C readiness gates pass for the final KindredCoast artifact.
- [ ] The final report path is recorded.

Commit:

```powershell
git add docs/visual-reverse-engineering-skill/23-StorefrontReverseEngineering-Phase3B3C-Production-Blocker-Closure.todo.md
git commit -m "Phase 3B3C.6: record KindredCoast production proof"
```

## Phase 3B/3C.7 - Docs, Checklist, And Final Closure Evidence

Goal: update operator docs and close this plan without claiming Phase 4 generation work.

Implementation checklist:

- [ ] Update `docs/visual-reverse-engineering-skill/README.md` with the strict Phase 3B/3C production proof command.
- [ ] Update `docs/visual-reverse-engineering-skill/reference.md` with review decision materialization guidance.
- [ ] Update `docs/architecture/11-storefront-builder.md` only if public architecture or command surface changed.
- [ ] Add a tracked QA summary under `docs/qa/` if this phase produces final closure evidence that should survive ignored artifacts.
- [ ] Mark every completed checklist item in this file.
- [ ] Record exact final commands and outcomes.
- [ ] Record any skipped checks with reason.
- [ ] Confirm no ignored production artifacts are accidentally staged.
- [ ] Confirm unrelated `scripts/reverse-engineering/readme.md` remains untouched unless the user explicitly asks otherwise.
- [ ] Run final `git status --short`.

Checks:

```powershell
git status --short
rg -n "ResolveSafeReviewItems|review decision materialization|completed-with-blockers|generation readiness" docs\visual-reverse-engineering-skill docs\architecture scripts\reverse-engineering tools\BlazorShop.AI.StorefrontReverseEngineering
```

Done when:

- [ ] Docs describe the new production proof path.
- [ ] This plan reflects real completed state.
- [ ] Final working tree contains only intentional tracked changes plus any pre-existing unrelated files.

Commit:

```powershell
git add docs/visual-reverse-engineering-skill docs/architecture docs/qa
git commit -m "Phase 3B3C.7: document production blocker closure"
```

## Final Closure Criteria

The work is complete only when all of the following are true:

- [ ] Phase 3A readiness passes for KindredCoast.
- [ ] Phase 3B generation readiness passes for KindredCoast.
- [ ] Phase 3C handoff readiness passes for KindredCoast.
- [ ] `analysis/visual-blueprint.v1.reviewed.json` is assembled.
- [ ] `analysis/agent-handoff/handoff-readiness.json` passes.
- [ ] Review queue blocking unresolved count is zero.
- [ ] No unsupported critical pattern remains unreviewed.
- [ ] No orphan reviewed mapping remains.
- [ ] No missing required slot remains.
- [ ] No duplicate non-repeatable slot remains.
- [ ] No unapproved extra section remains.
- [ ] Strict production proof with `-FailOnBlockers` exits `0`.
- [ ] Phase 3B gate passes.
- [ ] Phase 3C final handoff gate passes or a concrete environment blocker is recorded.
- [ ] No StorefrontBuilder Phase 4 generation behavior is changed in this closure unless a checklist item explicitly requires docs for a command surface.

## Final Evidence Log

Fill this during implementation:

| Evidence | Result | Path or command |
| --- | --- | --- |
| Baseline report | Pending |  |
| Focused regression tests | Pending |  |
| Phase 3B gate | Pending |  |
| Phase 3C gate | Pending |  |
| KindredCoast strict production proof | Pending |  |
| Final report | Pending |  |

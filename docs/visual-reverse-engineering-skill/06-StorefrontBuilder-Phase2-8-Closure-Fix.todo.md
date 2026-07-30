# StorefrontBuilder Phase 2.8 Closure Fix.todo

Status: Complete
Owner: Storefront Platform
Created: 2026-07-29
Completed: 2026-07-29
Scope: final StorefrontBuilder Phase 2 blockers after review

## Purpose

Close the remaining Phase 2 blockers without reopening the entire StorefrontBuilder foundation. This plan focuses on the verified gaps that still matter for production readiness:

- the Starter route contract must match the Presentation route surface;
- regeneration must update an existing generated storefront from the current Starter/template candidate, not from a copy of itself;
- `-WhatIf` must produce the same action plan as apply mode without writing files;
- platform metadata and package compatibility updates must be explicit and safe;
- tests must prove positive updates, missing files, obsolete files, and real rollback;
- scheduled full proof must bootstrap its fixture runtime on a clean CI runner.

Do not start AI Generator integration until this file is complete and verified.

## Closure Findings

These findings record the final codebase state after Phase 2.8A-2.8F.

- `starter-generation.contract.yaml` already contains more routes than the review claimed: `/`, `/pages/{Slug}`, auth/recovery routes, `/maintenance`, `/{*Path:nonfile}`, `/category/{Slug}`, `/product/{Slug}`, `/search`, `/cart`, `/my-cart`, `/checkout`, `/payment/result`, `/payment-success`, `/payment-cancel`, `/todays-deals`, `/new-releases`, and `/account`.
- Presentation owns additional account route shape through `@page "/account/{*Path}"` and route constants such as `/account/profile`, `/account/addresses`, `/account/orders`, and `/account/change-password`.
- Payment result routes in the current code are `/payment-success`, `/payment-cancel`, and `/payment/result`; do not introduce alternate slash-style payment route names unless Presentation changes first.
- `new-storefront-project.ps1` already uses staging and atomic replacement for first-time generation.
- `regenerate-storefront.ps1` now builds regeneration from a fresh current Starter/template candidate, then diffs candidate entries against the generated target.
- `apply-composition.mjs` now describes its deterministic Starter transform accurately instead of claiming it reads `generation-plan.yaml`.
- `-WhatIf` now runs candidate generation and the same action planner as apply mode, then exits before copying target files.
- `generated-file-manifest.mjs` computes hashes, current hash, manual-edit flags, missing entries, obsolete candidates, and conflict state. File-specific `sourceSpecHash` remains deferred for future AI Generator semantics.
- Regeneration tests now prove positive page/component updates, missing file recreation, obsolete candidate reporting, real `-WhatIf` planning, protected foundation metadata behavior, and rollback after a failed build.
- `storefront-builder.yml` now keeps full proof out of normal PR gates and uses `run-storefront-builder-full-proof-with-fixture.ps1` for scheduled/manual fixture runtime bootstrap.

Phase 2.9 follow-up polished developer-facing `-WhatIf` output by leaving a stable report outside the generated target/candidate and unified StorefrontBuilder `generatorVersion` provenance through `tools/BlazorShop.AI.StorefrontBuilder/version.json`.

## Corrected Blocker List

| Blocker | Current verdict | Closure approach |
| --- | --- | --- |
| Starter contract route inventory incomplete | Closed | Route parity checks and account wildcard/subroute metadata are in place without duplicating stale payment route names. |
| Regenerate is not a real update engine | Closed | Regeneration creates a fresh candidate from current Starter/template plus existing analysis inputs, then diffs candidate against target. |
| `-WhatIf` is not a real plan | Closed | `-WhatIf` runs the full candidate/planning pipeline and stops before apply. |
| Package versions and contract hash not synced on update | Closed | Explicit foundation/platform update path plans managed metadata changes. |
| Missing/obsolete file handling weak | Closed | Create/obsolete decisions are based on fresh candidate vs target. |
| Regeneration tests mostly prove shape | Closed | Positive update, missing recreate, obsolete, and rollback integration tests are in place. |
| Scheduled full proof not self-contained | Closed | CI wrapper starts fixture runtime, runs full proof, uploads artifacts, and stops runtime. |
| Phase 2 closure document still proposed | Closed | Phase 2 closure document is marked complete after Phase 2.8 gates passed. |

## Phase Order

1. Phase 2.8A - Starter Route Contract Truth
2. Phase 2.8B - Fresh Candidate Regeneration Pipeline
3. Phase 2.8C - Platform Metadata Upgrade Path
4. Phase 2.8D - Regeneration Test Hardening
5. Phase 2.8E - Self-Contained CI Full Proof
6. Phase 2.8F - Documentation And Closure Evidence

These phases should be implemented in order. Phase 2.8B depends on route truth from 2.8A. Phase 2.8D depends on the new pipeline. Phase 2.8E should run after local proof is stable.

## Phase 2.8A - Starter Route Contract Truth

Goal: make `starter-generation.contract.yaml` a truthful generator contract for the Presentation route surface.

Tasks:

- [x] Treat Presentation `@page` directives and `StorefrontRoutes` constants as the source of route truth.
- [x] Keep the current payment routes:
  - [x] `/payment-success`;
  - [x] `/payment-cancel`;
  - [x] `/payment/result`.
- [x] Do not add stale slash-style payment routes that are not owned by Presentation.
- [x] Add Starter contract route metadata for account wildcard behavior:
  - [x] `/account`;
  - [x] `/account/{*Path}` or an equivalent account route group/wildcard declaration.
- [x] Add account subroute metadata if the generator needs explicit visual state awareness:
  - [x] `/account/profile`;
  - [x] `/account/addresses`;
  - [x] `/account/orders`;
  - [x] `/account/change-password`.
- [x] Decide whether account subroutes are represented as concrete `routes` entries or as `routeAliases`/`childRoutes`.
- [x] Keep all account route entries pointing to the same account WASM host visual surface unless Presentation actually creates separate account route pages.
- [x] Verify catalog routes remain present:
  - [x] `/category/{Slug}`;
  - [x] `/product/{Slug}`;
  - [x] `/search`;
  - [x] `/todays-deals`;
  - [x] `/new-releases`.
- [x] Verify commerce routes remain present:
  - [x] `/cart`;
  - [x] `/my-cart`;
  - [x] `/checkout`;
  - [x] payment result routes.
- [x] Verify system routes remain present:
  - [x] `/maintenance`;
  - [x] `/{*Path:nonfile}`.
- [x] Add a route parity test that parses Presentation `@page` directives and compares them to Starter contract routes or route groups.
- [x] Add a route constants parity test for public route constants that are generator-relevant but may not appear as `@page` directives.
- [x] Ensure route parity tests fail when Presentation adds a generated-storefront-relevant route but Starter contract is not updated.
- [x] Ensure the test intentionally ignores non-page same-origin BFF endpoints such as `/api/cart`, `/api/checkout`, `/api/account/*`, `/robots.txt`, and `/sitemap.xml`.
- [x] Update route documentation in `docs/architecture/11-storefront-builder.md` only after tests define the final route inventory rule.

Implementation notes:

- Prefer a small parser/test helper over hand-maintaining a duplicate route list in multiple test files.
- If YAML parsing is not already available in tests, use a minimal route extraction helper for `route:` lines and documented route group fields.
- Keep generated visual files route-less; route declarations remain in Presentation.

QA:

```powershell
dotnet test BlazorShop.Tests.V2\BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~StorefrontStarterFoundationBoundaryTests"
dotnet test BlazorShop.Tests.V2\BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~StorefrontPageCompositionGuardrailTests"
.\scripts\qa\run-storefront-builder-generated-proof.ps1 -ProofLevel Structure
```

Exit gate:

- [x] Starter contract and Presentation route surface agree.
- [x] Account wildcard/subroute behavior is explicit enough for the generator.
- [x] Payment route names match current Presentation code.
- [x] Adding a new Presentation storefront page route without Starter contract metadata fails a test.

## Phase 2.8B - Fresh Candidate Regeneration Pipeline

Goal: make regeneration compare the current target project against a fresh candidate generated from current Starter/template source.

Tasks:

- [x] Replace old target-derived staging as the primary candidate source.
- [x] Introduce a fresh candidate generation step:
  - [x] read current generated project metadata;
  - [x] resolve project name;
  - [x] resolve store key;
  - [x] resolve output root;
  - [x] create candidate under an approved `.regeneration-candidate` root;
  - [x] generate from current Starter using the same project name and store key;
  - [x] copy or reuse existing analysis artifacts that are legitimate generation inputs;
  - [x] run visual foundation generation against candidate;
  - [x] run composition generation against candidate;
  - [x] build candidate manifest from candidate files.
- [x] Define which existing generated artifact files are inputs vs outputs.
  - Generated analysis artifacts are now produced in the candidate tree first and written back to the target tree only after planning/apply.
- [x] Stop claiming `apply-composition.mjs` reads `generation-plan.yaml` unless it actually does.
- [x] Either make `apply-composition.mjs` read the generation plan or rename/log it as deterministic Starter transform.
- [x] Add a single planning function used by both `-WhatIf` and apply mode.
- [x] Plan actions by comparing:
  - [x] original manifest generated hash;
  - [x] current target file hash;
  - [x] fresh candidate file hash;
  - [x] candidate manifest ownership;
  - [x] target-only generated files;
  - [x] candidate-only generated files.
- [x] Produce these action types:
  - [x] `create`;
  - [x] `update`;
  - [x] `skip unchanged`;
  - [x] `skip out-of-scope`;
  - [x] `skip user-owned`;
  - [x] `skip protected`;
  - [x] `conflict manual edit`;
  - [x] `obsolete candidate`;
  - [x] `platform metadata update`;
  - [x] `validation failed`.
- [x] Treat target-only generated/managed files missing from fresh candidate as obsolete candidates.
- [x] Treat candidate-only generated/managed files as create candidates.
- [x] Treat manually edited generated files as conflicts when candidate content differs.
- [x] Preserve manually edited files when candidate content is unchanged but report the manual edit state.
- [x] Ensure all path decisions stay under the generated project root.
- [x] Keep rollback backup under an approved generated output root.
- [x] Write a regeneration report before apply and after apply.
- [x] `-WhatIf` must run the full candidate/planning pipeline and exit before copying any changed file into target.
- [x] Apply mode must copy only planned safe changes from candidate to target.
- [x] Apply mode must update target `generated-files.yaml` only after successful apply and optional validation/build.
- [x] If validation/build fails, restore target from backup and leave a failure report if possible.

Implementation notes:

- Do not semantic-merge Razor or CSS in this phase.
- Do not let visual regeneration overwrite platform protected files unless Phase 2.8C explicitly requests a foundation update.
- Keep `build-storefront.ps1 -Mode update` as the high-level entrypoint, but route it through the fresh candidate planner.

QA:

```powershell
.\tools\BlazorShop.AI.StorefrontBuilder\regenerate-storefront.ps1 -ProjectRoot artifacts/storefront-builder/generated/BlazorShop.Storefront.GeneratedProof -Scope all -WhatIf
.\tools\BlazorShop.AI.StorefrontBuilder\regenerate-storefront.ps1 -ProjectRoot artifacts/storefront-builder/generated/BlazorShop.Storefront.GeneratedProof -Scope all -ValidateAfterApply -BuildAfterApply
.\scripts\qa\run-storefront-builder-regeneration-gate.ps1
.\scripts\qa\run-storefront-builder-generated-proof.ps1 -ProofLevel Structure
```

Exit gate:

- [x] Regeneration candidate comes from current Starter/template source, not from copying the target project.
- [x] `-WhatIf` reports real create/update/conflict/obsolete actions and writes no target files.
- [x] Apply mode uses the same plan as `-WhatIf`.
- [x] Missing generated Razor/component files can be recreated from candidate.
- [x] Removed template files become obsolete candidates.

## Phase 2.8C - Platform Metadata Upgrade Path

Goal: keep visual regeneration separate from platform foundation upgrades while still allowing generated projects to update package and contract metadata.

Tasks:

- [x] Add an explicit platform update operation. Preferred shape:
  - [x] `regenerate-storefront.ps1 -Scope foundation`;
  - [x] or `regenerate-storefront.ps1 -Scope all -UpdatePlatformMetadata`.
- [x] Use one clear command shape; avoid supporting two overlapping public APIs unless compatibility requires it.
- [x] Update generated project metadata from current sources:
  - [x] `metadata.yaml.storefrontContractSha256`;
  - [x] `metadata.yaml.storefrontContractPath`;
  - [x] `metadata.yaml.sourceStarterVersion`;
  - [x] `metadata.yaml.starterContractVersion`;
  - [x] `metadata.yaml.packageVersions`;
  - [x] `metadata.yaml.generatorVersion`;
  - [x] `metadata.yaml.updatedUtc`.
- [x] Update `StorefrontPackageVersions.props` only in the explicit platform update operation.
- [x] Keep `StorefrontPackageVersions.props` protected for visual scopes.
- [x] Represent platform-updated files in the plan as `platform metadata update`, not as ordinary visual `update`.
- [x] Copy current `starter-generation.contract.yaml` into generated project only during explicit foundation update.
- [x] Ensure foundation update still respects safe path/backup/rollback rules.
- [x] Ensure metadata updates are schema validated.
- [x] Ensure package version updates do not introduce direct Runtime/Client source references in generated projects.
- [x] Ensure generated projects continue to consume Presentation/Components directly and Runtime/Client only through package metadata/proof expectations.

Implementation notes:

- If a new ownership value is needed, prefer a narrow value such as `platform-managed` over weakening all `protected` handling.
- The default `-Scope all` should remain a visual/content regeneration unless the explicit platform update switch/scope is supplied.

QA:

```powershell
.\tools\BlazorShop.AI.StorefrontBuilder\regenerate-storefront.ps1 -ProjectRoot artifacts/storefront-builder/generated/BlazorShop.Storefront.GeneratedProof -Scope foundation -WhatIf
.\tools\BlazorShop.AI.StorefrontBuilder\regenerate-storefront.ps1 -ProjectRoot artifacts/storefront-builder/generated/BlazorShop.Storefront.GeneratedProof -Scope foundation -ValidateAfterApply -BuildAfterApply
.\tools\BlazorShop.AI.StorefrontBuilder\validate-storefront.ps1 -ProjectRoot artifacts/storefront-builder/generated/BlazorShop.Storefront.GeneratedProof -Name BlazorShop.Storefront.GeneratedProof -StoreKey sample
.\scripts\qa\run-storefront-builder-isolation-gate.ps1 -ProjectRoot artifacts/storefront-builder/generated/BlazorShop.Storefront.GeneratedProof -Name BlazorShop.Storefront.GeneratedProof
```

Exit gate:

- [x] Generated metadata can be intentionally upgraded to current contract/package/starter versions.
- [x] Visual regeneration cannot silently change platform metadata.
- [x] Foundation update is planned, reported, validated, and rollback-safe.
- [x] Isolation gate still passes after metadata/package update.

## Phase 2.8D - Regeneration Test Hardening

Goal: replace shape-only checks with tests that prove real update behavior.

Tasks:

- [x] Add a test fixture that modifies a Starter source file or controlled template source, then proves a generated target file updates.
- [x] Add page-scope positive test:
  - [x] modify candidate HomePage source/input;
  - [x] run `-Scope page -Target HomePage`;
  - [x] prove HomePage updates;
  - [x] prove ProductPage and unrelated files do not update.
- [x] Add component-scope positive test:
  - [x] modify candidate ProductSummaryCard source/input;
  - [x] run `-Scope component -Target ProductSummaryCard`;
  - [x] prove ProductSummaryCard updates;
  - [x] prove page files do not update.
- [x] Add `WhatIf` plan correctness test:
  - [x] prepare candidate with create/update/obsolete/conflict cases;
  - [x] run `-WhatIf`;
  - [x] assert report contains those exact planned actions;
  - [x] assert target tree hash does not change.
- [x] Add missing generated Razor page test:
  - [x] delete generated HomePage;
  - [x] run regeneration;
  - [x] prove file is recreated from fresh candidate.
- [x] Add missing generated component test:
  - [x] delete generated product component;
  - [x] run regeneration;
  - [x] prove file is recreated from fresh candidate.
- [x] Add obsolete file test:
  - [x] simulate Starter/template no longer producing a generated file;
  - [x] run `-WhatIf`;
  - [x] prove obsolete candidate is reported;
  - [x] prove file is not deleted by default.
- [x] Add manual-edit conflict test with candidate change:
  - [x] manually edit target generated file;
  - [x] change candidate for same file;
  - [x] prove conflict is reported and target edit is preserved.
- [x] Add user-owned preservation test with candidate present.
- [x] Add platform protected test:
  - [x] visual regeneration cannot update `StorefrontPackageVersions.props`;
  - [x] foundation update can plan and apply it.
- [x] Replace rollback string check with a real integration test:
  - [x] prepare candidate update;
  - [x] force post-apply build failure;
  - [x] run with `-BuildAfterApply`;
  - [x] assert command fails;
  - [x] assert full target tree hash matches pre-update hash;
  - [x] assert failure report exists or stderr has actionable code.
- [x] Add test for `sourceSpecHash` semantics if file-specific source identity is implemented in this phase.
- [x] Keep CI-friendly tests independent of live Commerce Node.

Implementation notes:

- Tests may use temporary copies of Starter and generated proof under `obj/storefront-builder/generated`.
- Avoid tests that mutate tracked Starter files.
- Prefer real script invocation for update pipeline tests because PowerShell path safety is part of the behavior.

QA:

```powershell
.\scripts\qa\run-storefront-builder-regeneration-gate.ps1
dotnet test BlazorShop.Tests.V2\BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~StorefrontBuilder"
.\scripts\qa\run-storefront-builder-generated-proof.ps1 -ProofLevel Structure
```

Exit gate:

- [x] Tests fail against old target-derived regeneration.
- [x] Tests pass only when fresh candidate planning is active.
- [x] Tests prove positive updates, not only absence of unexpected writes.
- [x] Rollback is proven by real failed build, not by source string inspection.

## Phase 2.8E - Self-Contained CI Full Proof

Goal: make scheduled/manual `FoundationFunctionalFull` proof run on a clean runner without manual local setup.

Tasks:

- [x] Add a CI-safe wrapper script, preferred name:
  - [x] `scripts/qa/run-storefront-builder-full-proof-with-fixture.ps1`.
- [x] Wrapper responsibilities:
  - [x] stop existing V2 processes if present;
  - [x] start required Docker dependencies;
  - [x] start Control Plane/Commerce Node/Storefront fixture runtime through the existing local runner or a dedicated fixture runner;
  - [x] wait for Commerce Node health;
  - [x] verify fixture store configuration;
  - [x] verify fixture category/product/page data;
  - [x] verify COD/test payment method;
  - [x] run `run-storefront-builder-generated-proof.ps1 -ProofLevel FoundationFunctionalFull`;
  - [x] collect generated reports;
  - [x] stop services in `finally`.
- [x] Prefer reusing `scripts/run-v2-local.ps1 -StopExisting -NoOpenBrowser` if it is stable on GitHub Windows runners.
- [x] If `run-v2-local.ps1` is too broad for CI, add a narrower fixture runtime script and document why. Not needed: Phase 2.8E reuses `run-v2-local.ps1 -StopExisting -NoOpenBrowser` successfully.
- [x] Update `.github/workflows/storefront-builder.yml`:
  - [x] PR keeps `Structure`, regeneration ownership, and `FoundationFunctionalFast`;
  - [x] scheduled/manual full proof calls the new fixture wrapper;
  - [x] full proof uploads reports as artifacts;
  - [x] failure logs include process output and fixture endpoint checks.
- [x] Ensure workflow installs .NET, Node, npm dependencies, and any Docker prerequisites before full proof.
- [x] Ensure ports used by fixture runtime are documented and not conflicting with generated proof host.
- [x] Ensure workflow teardown runs even when proof fails.
- [x] Add a describe mode for the wrapper so agents can inspect what it does without starting services.

Implementation notes:

- Keep the expensive live/full proof out of normal PR unless manually requested.
- Do not make scheduled CI depend on developer machine state.
- Do not mark Phase 2 closed based only on a local manual `run-v2-local.ps1` proof.

QA:

```powershell
.\scripts\qa\run-storefront-builder-full-proof-with-fixture.ps1 -Describe
.\scripts\qa\run-storefront-builder-full-proof-with-fixture.ps1
```

Manual release verification:

```powershell
.\scripts\qa\run-storefront-client-regeneration-gate.ps1
.\scripts\qa\run-storefront-builder-regeneration-gate.ps1
.\scripts\qa\run-storefront-builder-generated-proof.ps1 -ProofLevel Structure
.\scripts\qa\run-storefront-builder-generated-proof.ps1 -ProofLevel FoundationFunctionalFast
.\scripts\qa\run-storefront-builder-full-proof-with-fixture.ps1
```

Exit gate:

- [x] Scheduled workflow can run full proof on a clean runner.
- [x] Full proof starts and stops its own fixture runtime.
- [x] Proof reports are uploaded as CI artifacts.
- [x] A failed fixture bootstrap fails with problem, cause, and fix.

## Phase 2.8F - Documentation And Closure Evidence

Goal: align docs, historical plans, and closure status with the fixed implementation.

Tasks:

- [x] Update `docs/architecture/11-storefront-builder.md` with:
  - [x] fresh candidate regeneration model;
  - [x] real `-WhatIf` behavior;
  - [x] foundation/platform metadata update command;
  - [x] CI full proof bootstrap expectation;
  - [x] route contract truth rule.
- [x] Update `docs/agents/storefront-builder.md` with:
  - [x] new required commands;
  - [x] when to run regeneration gate vs full fixture proof;
  - [x] no target-derived regeneration assumption.
- [x] Update visual reverse engineering docs:
  - [x] `README.md`;
  - [x] `reference.md`;
  - [x] `how-to-generate-and-validate.md`;
  - [x] `tutorial-generated-proof.md`;
  - [x] `explanation-boundaries-and-regeneration.md`.
- [x] Update `05-StorefrontBuilder-Phase2-Closure.todo.md` factual status:
  - [x] remove stale known-gap lines if they contradict current implementation evidence;
  - [x] keep historical evidence where accurate;
  - [x] mark `Status: Complete` only after all Phase 2.8 exit gates pass;
  - [x] add `Completed: <date>`;
  - [x] add `Evidence commit: <sha>` after commit exists.
- [x] Keep this Phase 2.8 todo file as the final closure-fix evidence.
- [x] Add a final verification section with exact command outputs summarized.
- [x] Ensure generated proof artifacts under `artifacts/` or `obj/` remain uncommitted unless a phase explicitly promotes them.

QA:

```powershell
rg -n "target-copy|payment/success|payment/cancel|Status: Proposed" docs/visual-reverse-engineering-skill docs/architecture/11-storefront-builder.md docs/agents/storefront-builder.md --glob "!06-StorefrontBuilder-Phase2-8-Closure-Fix.todo.md"
git status --short
```

Exit gate:

- [x] Docs describe the implementation that actually exists.
- [x] Phase 2 closure status is not marked complete until all gates pass.
- [x] Another agent can run generate, WhatIf, update, validation, fast proof, and full proof without guessing.

## Final Release Gate

Run the full release gate only after Phases 2.8A-2.8F are complete:

```powershell
.\scripts\qa\run-storefront-client-regeneration-gate.ps1
dotnet test BlazorShop.Tests.V2\BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~StorefrontBuilder"
.\scripts\qa\run-storefront-builder-regeneration-gate.ps1
.\scripts\qa\run-storefront-builder-generated-proof.ps1 -ProofLevel Structure
.\scripts\qa\run-storefront-builder-generated-proof.ps1 -ProofLevel FoundationFunctionalFast
.\scripts\qa\run-storefront-builder-full-proof-with-fixture.ps1
```

Phase 2 can be closed only when:

- [x] route contract parity passes;
- [x] fresh candidate regeneration passes;
- [x] real `-WhatIf` plan passes;
- [x] platform metadata update path passes;
- [x] positive update tests pass;
- [x] missing/obsolete tests pass;
- [x] real rollback test passes;
- [x] CI-friendly regeneration ownership gate passes;
- [x] full fixture proof runs self-contained;
- [x] docs and closure status are updated.

## Final Verification

Final release gate rerun on 2026-07-29:

- `.\scripts\qa\run-storefront-client-regeneration-gate.ps1`: passed; final output `PASS Storefront client regeneration gate completed without drift.`
- `dotnet test BlazorShop.Tests.V2\BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~StorefrontBuilder"`: passed `37/37` after updating the workflow assertion to the new full fixture wrapper job.
- `.\scripts\qa\run-storefront-builder-regeneration-gate.ps1`: passed; final output `PASS StorefrontBuilder regeneration ownership gate completed without live Commerce Node data.` The run covered fresh-candidate regeneration, real `-WhatIf`, positive updates, missing/obsolete handling, platform metadata behavior, protected/user-owned handling, and rollback after failed build.
- `.\scripts\qa\run-storefront-builder-generated-proof.ps1 -ProofLevel Structure`: passed; generated proof restored, built, validated, passed isolation, passed shared visual consumer boundary validation, and passed regeneration lifecycle checks.
- `.\scripts\qa\run-storefront-builder-generated-proof.ps1 -ProofLevel FoundationFunctionalFast`: initially exposed a QA harness issue where the script accepted consent and then clicked a hidden revoke button. After updating the harness to reopen the banner before revoke, the command passed and wrote `fast-foundation-functional-report.md`.
- `.\scripts\qa\run-storefront-builder-full-proof-with-fixture.ps1`: passed; wrapper stopped old runtime, started Docker dependencies plus Control Plane API/Web, Commerce Node API, and Storefront V2, verified fixture configuration/category/product/page/COD payment data, ran `FoundationFunctionalFull`, wrote `visual-qa-report.md`, `functional-commerce-report.md`, and `full-proof-with-fixture-report.md`, then stopped services in `finally`.
- `rg -n "target-copy|payment/success|payment/cancel|Status: Proposed" docs/visual-reverse-engineering-skill docs/architecture/11-storefront-builder.md docs/agents/storefront-builder.md --glob "!06-StorefrontBuilder-Phase2-8-Closure-Fix.todo.md"`: returned no matches.
- `git diff --check`: passed with only Git line-ending warnings.
- `actionlint .github/workflows/storefront-builder.yml`: not available in this local environment, so workflow syntax was checked by repo tests and `git diff --check`.

## Not In Scope

- [ ] AI visual generation prompt orchestration.
- [ ] Semantic Razor/CSS merge.
- [ ] React/Next/Vue storefront skeletons.
- [ ] Production deployment of generated stores.
- [ ] New Commerce Node domain features.
- [ ] Payment/shipping/tax provider expansion.
- [ ] Storefront V2 visual redesign.

## Autoplan Review Report

CEO review:

- Keep this as a closure-fix sprint, not a second Phase 2 rewrite.
- The commercial risk is overwriting edited generated stores or claiming CI confidence without a clean-runner full proof.
- The most valuable outcome is boring and verifiable: generate, edit, WhatIf, update, build, isolate, browser-test, full-proof.

Engineering review:

- The key architectural correction is fresh candidate generation. Without it, missing/obsolete/update planning cannot be trusted.
- Route contract truth should be test-driven from Presentation route ownership, because hand-maintained route lists already drifted once.
- Platform metadata updates should be explicit because `StorefrontPackageVersions.props` is intentionally protected for visual regeneration.

DX review:

- `-WhatIf` must show exact create/update/conflict/obsolete actions, otherwise developers cannot trust update mode.
- Error messages in new scripts should include problem, cause, and fix.
- The full proof wrapper should have `-Describe` and clear fixture bootstrap failures.

Design review:

- No visual redesign is needed.
- Starter remains neutral; generated projects own visual output.
- The generator should preserve component mobility by binding to Presentation semantic descriptors, not by recreating application behavior.

Decision audit:

| # | Decision | Rationale | Rejected |
| --- | --- | --- | --- |
| 1 | Create a new Phase 2.8 file instead of rewriting 05 immediately | Preserve historical plan/evidence and isolate closure fixes. | Editing 05 as if the original plan was still current. |
| 2 | Use current Presentation route names, not stale review route examples | `/payment-success` and `/payment-cancel` are current code. | Adding alternate slash-style payment route names. |
| 3 | Make fresh candidate generation the center of regeneration | This is the root cause behind WhatIf, missing, obsolete, and update gaps. | More patching around old target-derived staging. |
| 4 | Separate platform metadata update from visual regeneration | Package/contract upgrades should be explicit and reviewable. | Letting `-Scope all` silently rewrite protected platform files. |
| 5 | Add self-contained full proof wrapper | Scheduled CI must not depend on manual local setup. | Documenting manual pre-run as sufficient for scheduled proof. |

## Suggested Commit Slices

1. Route contract parity and Starter contract update.
2. Fresh candidate planner and true `-WhatIf`.
3. Foundation/platform metadata update path.
4. Positive regeneration and rollback integration tests.
5. CI full proof fixture wrapper and workflow update.
6. Docs and Phase 2 closure evidence cleanup.

Each commit should update this checklist from `[ ]` to `[x]` only after the listed QA for that phase passes.

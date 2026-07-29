# StorefrontBuilder Phase 2 Closure.todo

Status: Proposed
Owner: Storefront Platform
Created: 2026-07-29
Scope: StorefrontBuilder, Storefront Starter, generated Storefront projects, Storefront Client/Runtime/Presentation/Browseable boundaries

## Purpose

Close Phase 2 in implementation order without reopening completed architecture work. This plan turns the current StorefrontBuilder foundation into a production-usable generated storefront workflow:

- keep the canonical Storefront API contract and generated client stable;
- keep Storefront V2, Starter, and generated stores as visual hosts, not API transport owners;
- make create/update/regenerate safe for real edited generated projects;
- prove package isolation, browser behavior, and regeneration in CI before starting AI Generator work.

The user requirement for this file is completeness. Every item from the original Phase 2 direction is cross-checked below and mapped to a phase, an exit gate, or a deliberate deferral.

## Current Codebase Baseline

Verified anchors from the current repository:

- `contracts/storefront/storefront.openapi.json` is the canonical Storefront API contract.
- `scripts/generate-storefront-client.ps1` reads the canonical contract from `BlazorShop.Storefront.Client/nswag.storefront.client.json`.
- `scripts/qa/run-storefront-client-regeneration-gate.ps1` exists and checks generated client drift.
- `BlazorShop.Storefront.Client` is an SDK-style package project and has no backend project dependency.
- `BlazorShop.Storefront.Runtime` uses typed generated-client factories and capability-scoped registration.
- `BlazorShop.Storefront.Presentation` owns shared App/Routes/page services/BFF/SEO/media composition.
- `BlazorShop.Storefront.Browser` owns browser-safe controllers and same-origin local API primitives.
- `BlazorShop.Storefront.Components.Features` is retired; shared components are Contracts, Headless, Browser, and WASM diagnostics.
- `BlazorShop.Storefront.Starter` uses `AddStorefrontApplication(...)`, `UseStorefrontApplication(...)`, `MapStorefrontApplication(...)`, and `starter-generation.contract.yaml`.
- `tools/BlazorShop.AI.StorefrontBuilder` has create, validate, regenerate, capture, generation, and QA scripts.
- `scripts/qa/run-storefront-builder-generated-proof.ps1` exists for `Structure`, `FoundationFunctionalFast`, and `FoundationFunctionalFull`.
- `scripts/qa/run-storefront-builder-isolation-gate.ps1` exists and proves generated package/reference isolation.

Known gaps from the current repository:

- `new-storefront-project.ps1` creates a project directly in the output path; creation needs staging, atomic replacement, and stronger failure cleanup.
- `regenerate-storefront.ps1` supports scopes, but update/regenerate is still mostly script orchestration and not a full ownership-aware update engine.
- `update-generated-files-manifest.mjs` writes a mostly fixed manifest and always sets `manualEditDetected: false`; it does not compute real file hashes, detect edits, classify conflicts, or handle obsolete/new generated files.
- Existing idempotency validation checks manifest shape and simple conflict markers, not end-to-end safe regeneration on edited generated projects.
- Generated proof validates one-shot generation and browser behavior, but it does not yet prove the full generate -> edit -> regenerate -> conflict/no-loss path.

## Original Phase 2 Coverage Matrix

| Original area | Current state | Required closure |
| --- | --- | --- |
| 1. Canonical OpenAPI contract | Mostly complete | Keep canonical contract outside tests, keep regeneration gate, add contract hash into generated metadata. |
| 2. Generated Storefront.Client | Mostly complete | Keep deterministic generation, package proof, and no backend source dependency guard. |
| 3. Storefront.Runtime | Mostly complete | Keep typed factories, typed envelope mapping, cancellation semantics, capability registration, server-only guardrails. |
| 4. Storefront.Presentation | Mostly complete | Keep BFF/page services/action descriptors as the shared host-independent application layer. |
| 5. Storefront.Browser | Mostly complete | Keep same-origin only browser primitives and prove re-entry/loading/error behavior. |
| 6. Storefront.Components | Mostly complete | Keep only Contracts/Headless/Browser primitives; block Features, visual class bags, and V2 route defaults from returning. |
| 7. Storefront.Starter | Mostly complete | Freeze the starter contract as the generator input and prove slot/action/route completeness. |
| 8. File ownership model | Partial | Implement real `generated-files.yaml` ownership, hash, manual edit, conflict, and obsolete-file tracking. |
| 9. Create generator | Partial | Make project creation staged, atomic, validated, and rollback-safe. |
| 10. Update/regenerate generator | Partial | Implement ownership-aware regeneration with dry-run, scoped updates, conflict reports, and rollback. |
| 11. Generated artifacts | Partial | Make metadata, assets, manifests, reports, package versions, and contract hash accurate and schema-validated. |
| 12. Static validation | Mostly present | Tighten validation around protected files, no direct transport, no forbidden references, no missing descriptors. |
| 13. Isolation gate | Present | Keep generated projects consuming packages only and not referencing V2/backend/core/API projects. |
| 14. Generated proof | Present | Extend proof to include regeneration safety and manual-edit conflict fixtures. |
| 15. CI release gates | Present | Add/update CI gates for regeneration safety, ownership manifest validity, and full release proof. |
| 16. Docs/DX | Partial | Update how-to, reference, tutorial, architecture, and agent guide with final create/update workflow. |

## Implementation Order

The phases must be implemented in this order:

1. Phase 2.1 - Contract and Client Gate Closure
2. Phase 2.2 - Runtime, Presentation, Browser Boundary Freeze
3. Phase 2.3 - Starter Contract Freeze
4. Phase 2.4 - Create Generator Hardening
5. Phase 2.5 - File Ownership Manifest Engine
6. Phase 2.6 - Safe Update and Regenerate Engine
7. Phase 2.7 - Generated Proof, QA, CI, and Documentation Closure

Do not start AI Generator integration until Phase 2.7 passes.

## Phase 2.1 - Contract And Client Gate Closure

Goal: make the Storefront API contract and generated client an owned production artifact, not a test snapshot or implicit V2 transport.

Tasks:

- [x] Keep `contracts/storefront/storefront.openapi.json` as the only canonical Storefront OpenAPI input.
- [x] Verify no generator reads `BlazorShop.Tests.V2/PresentationV2/CommerceNode/Snapshots` as a production contract source.
- [x] Keep `BlazorShop.Storefront.Client/nswag.storefront.client.json` on `useBaseUrl: false` and `generateBaseUrlProperty: false`.
- [x] Keep generated clients depending on `HttpClient.BaseAddress` through Runtime factories.
- [x] Add or verify a contract hash recorded in generated StorefrontBuilder metadata, so generated projects can tell which API contract produced them.
- [x] Add a validation check that generated StorefrontBuilder metadata fails when the contract hash is missing or malformed.
- [x] Add a validation check that generated projects do not copy or manually define Storefront API DTOs that belong to `BlazorShop.Storefront.Client`.
- [x] Keep package metadata for Client stable enough for package proof.
- [x] Keep the generated client deterministic by running generation and failing on git diff.

2026-07-29 Phase 2.1 implementation notes:
- `new-storefront-project.ps1` now records `storefrontContractPath: contracts/storefront/storefront.openapi.json` and lowercase SHA-256 `storefrontContractSha256` in generated metadata.
- `metadata.schema.json` and schema fixtures require canonical contract identity.
- `Test-StorefrontBuilderGeneratedProject.ps1` rejects generated metadata with missing or malformed contract hash.
- Guardrail tests verify production generation does not use Storefront OpenAPI test snapshots.

Implementation notes:

- Prefer improving existing scripts:
  - `scripts/generate-storefront-client.ps1`
  - `scripts/qa/run-storefront-client-regeneration-gate.ps1`
  - `BlazorShop.Storefront.Client/nswag.storefront.client.json`
  - `tools/BlazorShop.AI.StorefrontBuilder/scripts/generate/write-review-artifacts.mjs`
  - `tools/BlazorShop.AI.StorefrontBuilder/scripts/validate/Test-StorefrontBuilderSchemas.ps1`
- Do not create a second DTO package for this phase.
- Do not move Storefront API contracts into V2.

QA:

```powershell
.\scripts\qa\run-storefront-client-regeneration-gate.ps1
dotnet test BlazorShop.Tests.V2\BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~StorefrontGeneratedClient"
dotnet test BlazorShop.Tests.V2\BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~StorefrontBuilder"
```

Exit gate:

- [x] Client regeneration produces no git diff.
- [x] Generated client still has no backend/Core/API project reference.
- [x] Generated StorefrontBuilder metadata records canonical contract identity.
- [x] StorefrontBuilder validation fails if a generated project has stale or missing contract metadata.

2026-07-29 Phase 2.1 verification:
- `.\scripts\qa\run-storefront-client-regeneration-gate.ps1` passed without drift.
- `dotnet test BlazorShop.Tests.V2\BlazorShop.Tests.V2.csproj -c Release --no-restore --filter "FullyQualifiedName~StorefrontGeneratedClient|FullyQualifiedName~StorefrontBuilder" --logger "trx;LogFileName=storefront-builder-phase21-rerun-built.trx" --blame-hang --blame-hang-timeout 5m` passed `41/41`.
- `.\tools\BlazorShop.AI.StorefrontBuilder\scripts\validate\Test-StorefrontBuilderSchemas.ps1` passed.

## Phase 2.2 - Runtime, Presentation, Browser Boundary Freeze

Goal: lock the host-independent application boundary before changing generator behavior.

Tasks:

- [x] Keep Runtime as server/BFF-only; WASM/browser projects must not reference Runtime directly.
- [x] Keep Runtime typed generated-client factories; block `Activator.CreateInstance` from returning.
- [x] Keep Runtime typed response mapping; block reflection envelope mapping such as `GetProperty("Success")`.
- [x] Keep Runtime cancellation behavior that propagates caller cancellation and maps only real timeout/network failures.
- [x] Keep capability-scoped registration methods such as `AddStorefrontCatalogRuntime`, `AddStorefrontCartRuntime`, `AddStorefrontCheckoutRuntime`, and `AddStorefrontPlatformRuntime`.
- [x] Keep Presentation as the shared application package for App/Routes/page services/BFF/SEO/media composition.
- [x] Keep browser mutations flowing through same-origin Presentation BFF endpoints, not direct Commerce Node API calls.
- [x] Keep `BlazorShop.Storefront.Browser` as the browser-safe project name and browser-safe WASM/reference surface.
- [x] Keep V2 visual-only: no `StorefrontApiClient`, no class implementing `IStorefront*Client`, no manual Commerce Node Storefront transport.
- [x] Add or tighten architecture tests that scan V2, Starter, generated proof, Browser, Runtime, Client, and Presentation boundaries.

2026-07-29 Phase 2.2 implementation notes:
- Added `Phase2BoundaryFreeze_KeepsRuntimePresentationBrowserAndVisualHostsInTheirRoles` to lock Browser/WASM Runtime exclusion, V2/Starter transport exclusion, Browser same-origin-only behavior, and Presentation application registration ownership.
- Existing shared platform tests continue to block Runtime `Activator.CreateInstance`, reflection envelope mapping, removed Runtime aliases, and invalid package references.

Implementation notes:

- Prefer tests and guardrails over large refactors unless a current violation is found.
- If a violation exists, remove the direct dependency and route it through Presentation/Runtime/Browser as already designed.
- Do not add compatibility aliases for old Runtime registration unless an external package consumer exists and a removal version is documented.

QA:

```powershell
dotnet test BlazorShop.Tests.V2\BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~StorefrontVisualOnlyBoundary"
dotnet test BlazorShop.Tests.V2\BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~StorefrontBrowser"
dotnet test BlazorShop.Tests.V2\BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~StorefrontRuntime"
```

Exit gate:

- [x] No V2 source contains `StorefrontApiClient`.
- [x] No V2 source implements `IStorefront*Client`.
- [x] No WASM/browser project references Runtime.
- [x] Runtime still owns generated transport.
- [x] Presentation still owns BFF/routes/SEO/media composition.
- [x] Browser controllers only call same-origin local endpoints.

2026-07-29 Phase 2.2 verification:
- `dotnet test BlazorShop.Tests.V2\BlazorShop.Tests.V2.csproj -c Release --no-restore --filter "FullyQualifiedName~StorefrontVisualOnlyBoundary|FullyQualifiedName~StorefrontBrowser|FullyQualifiedName~StorefrontRuntime" --logger "trx;LogFileName=storefront-builder-phase22.trx" --blame-hang --blame-hang-timeout 5m` passed `110/110`.
- Source scans found no `StorefrontApiClient` in V2/Starter/V2.WASM/StorefrontBuilder generated surfaces, no `Activator.CreateInstance` or `GetProperty("Success")` in Runtime, and no direct Commerce Node Storefront route use in Browser/V2/Starter visual source.

## Phase 2.3 - Starter Contract Freeze

Goal: make `BlazorShop.Storefront.Starter` a complete, neutral, generator-ready skeleton without making it a real store design.

Tasks:

- [x] Treat `starter-generation.contract.yaml` as the authoritative generator contract.
- [x] Verify Starter exposes slots/routes for:
  - [x] home;
  - [x] category;
  - [x] product detail;
  - [x] search;
  - [x] content/page;
  - [x] cart;
  - [x] checkout;
  - [x] account;
  - [x] auth/recovery;
  - [x] consent;
  - [x] payment completion/failure;
  - [x] maintenance/store closed;
  - [x] not found;
  - [x] error.
- [x] Verify Starter action descriptors cover:
  - [x] product selection preview;
  - [x] add to cart;
  - [x] cart line update/remove;
  - [x] checkout start/review/place order;
  - [x] account profile/password/address/order operations;
  - [x] auth login/logout/register/recovery;
  - [x] consent save/revoke.
- [x] Verify Starter route metadata distinguishes SSR, hybrid, and WASM host usage clearly enough for future generated stores.
- [x] Verify Starter local visual components are store-neutral and do not copy Storefront V2 visual styling.
- [x] Verify generated visual files are not allowed to declare `@page` routes or add their own route assemblies.
- [x] Verify package references and generation contract versions are captured in metadata.
- [x] Update docs if the Starter contract currently promises a route/slot/action that is not implemented.

2026-07-29 Phase 2.3 implementation notes:
- `starter-generation.contract.yaml` now records the authoritative slot, route, action descriptor, generated constraint, and metadata contract for Starter generation.
- Contract routes were aligned with Presentation-owned routes, including `/pages/{Slug}`, `/my-cart` plus `/cart`, auth/recovery routes, payment success/cancel/result routes, and the not-found catch-all `/{*Path:nonfile}`.
- Action descriptors are recorded as Presentation-owned symbolic `routeSource` entries so Starter does not contain forbidden `/api/*` endpoint literals.
- Generated StorefrontBuilder metadata now records `starterContractVersion` and package versions sourced from `StorefrontPackageVersions.props`; validation/schema fixtures require those fields.

Implementation notes:

- Keep Starter minimal but complete. It is a skeleton for later generated stores, not a showcase storefront.
- Do not push store-specific images, CSS, copy, or page content back into Starter.
- Do not add redundant account pages beyond what the BFF/account component model needs.

QA:

```powershell
dotnet test BlazorShop.Tests.V2\BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~Starter"
.\scripts\qa\run-storefront-builder-generated-proof.ps1 -ProofLevel Structure
```

Exit gate:

- [x] Starter contract has no promised-but-missing slot/action/route.
- [x] Starter stays neutral and package-consumable.
- [x] Generated proof can be created from Starter without V2 references.
- [x] Any missing optional route is explicitly documented as deferred, not silently absent.

2026-07-29 Phase 2.3 verification:
- `dotnet test BlazorShop.Tests.V2\BlazorShop.Tests.V2.csproj -c Release --no-restore --filter "FullyQualifiedName~Starter" --logger "trx;LogFileName=storefront-builder-phase23-starter-rerun.trx" --blame-hang --blame-hang-timeout 5m` passed `59/59`.
- `.\scripts\qa\run-storefront-builder-generated-proof.ps1 -ProofLevel Structure` passed; generated proof restored, built, validated, passed isolation, and passed the shared visual consumer boundary validator.

## Phase 2.4 - Create Generator Hardening

Goal: make first-time generated project creation safe, deterministic, and recoverable.

Tasks:

- [x] Harden project name normalization in `build-storefront.ps1` and `new-storefront-project.ps1`.
- [x] Support the accepted naming model:
  - [x] user may pass a friendly suffix such as `Demo`;
  - [x] generator emits `BlazorShop.Storefront.Demo`;
  - [x] user may also pass the full `BlazorShop.Storefront.Demo` name;
  - [x] unsafe names, traversal, separators, empty segments, lowercase project suffix, and reserved names fail before file writes.
- [x] Validate `StoreKey` before file writes.
- [x] Generate into a temporary staging directory first.
- [x] Validate staged project structure before replacing or creating the final output path.
- [x] If target exists and `-Force` is false, fail without changing target.
- [x] If target exists and `-Force` is true, replace only after staging succeeds.
- [x] Ensure deletion/replacement only happens under approved generated output roots.
- [x] On failure, clean staging and leave the previous target unchanged.
- [x] Write metadata with:
  - [x] generator version;
  - [x] timestamp;
  - [x] source Starter path/version;
  - [x] Starter contract version;
  - [x] Storefront contract hash;
  - [x] package versions;
  - [x] command mode;
  - [x] normalized project name;
  - [x] store key;
  - [x] output root.
- [x] Ensure `plan-only` and `validate-only` do not write generated project files.
- [x] Ensure `generate` and `full` modes have the same project creation semantics.

2026-07-29 Phase 2.4 implementation notes:
- Added `StorefrontBuilderProjectSafety.ps1` for shared project name normalization, store key validation, approved output-root checks, and guarded deletes.
- `new-storefront-project.ps1` now generates into an approved-root `.staging` directory, validates the staged project, then moves it into the final path; forced replacement uses a `.replace-backup` restore path.
- `build-storefront.ps1` now uses the same normalization and passes command mode into generation for both `generate` and `full`.
- Generated metadata now records generator version, UTC timestamp, command mode, normalized project name, output root, source Starter version, Starter contract version, Storefront contract hash, and package versions.

Implementation notes:

- Prefer adding reusable helper functions instead of scattering path checks across scripts.
- Keep generated projects out of `BlazorShop.sln` by default.
- Do not require network or live Commerce Node API for structure generation.

QA:

```powershell
.\tools\BlazorShop.AI.StorefrontBuilder\build-storefront.ps1 -Url https://example.test -Name Demo -StoreKey sample -Mode plan-only
.\tools\BlazorShop.AI.StorefrontBuilder\build-storefront.ps1 -Url https://example.test -Name Demo -StoreKey sample -Mode generate -Force
.\tools\BlazorShop.AI.StorefrontBuilder\validate-storefront.ps1 -ProjectRoot artifacts/storefront-builder/generated/BlazorShop.Storefront.Demo -Name BlazorShop.Storefront.Demo -StoreKey sample
.\scripts\qa\run-storefront-builder-isolation-gate.ps1 -ProjectRoot artifacts/storefront-builder/generated/BlazorShop.Storefront.Demo -Name BlazorShop.Storefront.Demo
```

Add focused negative tests for:

- [x] unsafe project name;
- [x] unsafe store key;
- [x] existing target without `-Force`;
- [x] target outside generated root;
- [x] staged failure leaves old target unchanged;
- [x] `plan-only` writes no generated project files.

Exit gate:

- [x] Generated project creation is atomic from the user's point of view.
- [x] Failed creation cannot leave a half-generated project in the final path.
- [x] `-Force` cannot delete outside approved generated artifact roots.
- [x] Metadata is accurate and schema-valid.

2026-07-29 Phase 2.4 verification:
- `.\tools\BlazorShop.AI.StorefrontBuilder\tests\generation\Test-StorefrontBuilderCreateHardening.ps1` passed.
- `.\tools\BlazorShop.AI.StorefrontBuilder\build-storefront.ps1 -Url https://example.test -Name Demo -StoreKey sample -Mode plan-only` passed without creating generated project files.
- `.\tools\BlazorShop.AI.StorefrontBuilder\build-storefront.ps1 -Url https://example.test -Name Demo -StoreKey sample -Mode generate -Force` passed through staging and final publish.
- `.\tools\BlazorShop.AI.StorefrontBuilder\validate-storefront.ps1 -ProjectRoot artifacts/storefront-builder/generated/BlazorShop.Storefront.Demo -Name BlazorShop.Storefront.Demo -StoreKey sample` passed.
- `.\scripts\qa\run-storefront-builder-isolation-gate.ps1 -ProjectRoot artifacts/storefront-builder/generated/BlazorShop.Storefront.Demo -Name BlazorShop.Storefront.Demo` passed.
- `dotnet test BlazorShop.Tests.V2\BlazorShop.Tests.V2.csproj -c Release --no-restore --filter "FullyQualifiedName~StorefrontBuilderQaRegenerationTests" --logger "trx;LogFileName=storefront-builder-phase24-guardrails.trx" --blame-hang --blame-hang-timeout 5m` passed `14/14`.

## Phase 2.5 - File Ownership Manifest Engine

Goal: make `docs/storefront-analysis/generated-files.yaml` a real regeneration safety contract.

Tasks:

- [ ] Define the final manifest schema for each generated file:
  - [ ] `filePath`;
  - [ ] `ownership`;
  - [ ] `capability`;
  - [ ] `scope`;
  - [ ] `generatorVersion`;
  - [ ] `sourceArtifactIds`;
  - [ ] `sourceSpecHash`;
  - [ ] `generatedHash`;
  - [ ] `currentHash`;
  - [ ] `lastGeneratedTimestamp`;
  - [ ] `manualEditDetected`;
  - [ ] `conflictStatus`;
  - [ ] `conflictReason`;
  - [ ] `protected`;
  - [ ] `obsolete`;
  - [ ] `templateVersion`.
- [ ] Standardize ownership values:
  - [ ] `generated`;
  - [ ] `managed`;
  - [ ] `user-owned`;
  - [ ] `protected`;
  - [ ] `artifact-only`.
- [ ] Add manifest reader/writer helper scripts.
- [ ] Replace fixed manifest generation in `update-generated-files-manifest.mjs` with actual file scanning and hashing.
- [ ] Detect manual edits by comparing stored `generatedHash` to current file hash.
- [ ] Mark user-owned/protected files so regeneration never overwrites them.
- [ ] Detect missing generated files and report whether they should be recreated or left removed.
- [ ] Detect obsolete files from old templates and report cleanup actions instead of deleting blindly.
- [ ] Record capability ownership for pages/components/CSS/assets:
  - [ ] shell/layout;
  - [ ] home;
  - [ ] catalog;
  - [ ] product;
  - [ ] cart;
  - [ ] checkout;
  - [ ] account;
  - [ ] auth/recovery;
  - [ ] content;
  - [ ] SEO/media/consent support.
- [ ] Make manifest validation reject:
  - [ ] missing required fields;
  - [ ] invalid ownership values;
  - [ ] hash mismatch without conflict status;
  - [ ] protected file marked generated;
  - [ ] file path outside project root;
  - [ ] duplicate file entries.

Implementation notes:

- Store hashes in a deterministic format such as lowercase SHA-256 hex.
- Treat line ending normalization explicitly so Windows/CI do not create false conflicts.
- Do not attempt semantic Razor merge in this phase.

QA:

```powershell
.\tools\BlazorShop.AI.StorefrontBuilder\regenerate-storefront.ps1 -ProjectRoot artifacts/storefront-builder/generated/BlazorShop.Storefront.GeneratedProof -Scope conflicts
.\tools\BlazorShop.AI.StorefrontBuilder\validate-storefront.ps1 -ProjectRoot artifacts/storefront-builder/generated/BlazorShop.Storefront.GeneratedProof -Name BlazorShop.Storefront.GeneratedProof -StoreKey sample
dotnet test BlazorShop.Tests.V2\BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~StorefrontBuilder"
```

Add fixtures for:

- [ ] unchanged generated file;
- [ ] manually edited generated file;
- [ ] manually edited user-owned file;
- [ ] protected file modified;
- [ ] missing generated file;
- [ ] obsolete generated file;
- [ ] duplicate manifest entry;
- [ ] manifest path traversal.

Exit gate:

- [ ] Manifest is computed from actual project files.
- [ ] Manual edits are detected reliably.
- [ ] Protected and user-owned files cannot be overwritten by regeneration.
- [ ] Conflict reports identify exact files, reason, and next action.

## Phase 2.6 - Safe Update And Regenerate Engine

Goal: make generated storefronts safely updatable after humans tune them.

Tasks:

- [ ] Replace the current simplistic regeneration flow with an ownership-aware staged update engine.
- [ ] Keep public command shape:
  - [ ] `-Scope all`;
  - [ ] `-Scope page`;
  - [ ] `-Scope component`;
  - [ ] `-Scope css`;
  - [ ] `-Scope validate`;
  - [ ] `-Scope conflicts`;
  - [ ] `-WhatIf`.
- [ ] For every scope, compute a planned file action list before writing:
  - [ ] create;
  - [ ] update;
  - [ ] skip unchanged;
  - [ ] skip user-owned;
  - [ ] skip protected;
  - [ ] conflict manual edit;
  - [ ] obsolete candidate;
  - [ ] delete only if explicitly allowed.
- [ ] Stage regenerated output before applying it to the generated project.
- [ ] Apply changes only when:
  - [ ] target file is generated/managed;
  - [ ] current hash matches last generated hash; or
  - [ ] conflict policy explicitly allows replacement.
- [ ] Preserve human-edited files and emit conflicts instead of overwriting.
- [ ] Keep `-WhatIf` as a true no-write dry run.
- [ ] Run validate/build after applying changes when requested by the caller or proof script.
- [ ] Roll back applied changes if post-regeneration build/validation fails.
- [ ] Write a regeneration report with:
  - [ ] command;
  - [ ] scope;
  - [ ] changed files;
  - [ ] skipped files;
  - [ ] conflicts;
  - [ ] obsolete candidates;
  - [ ] validation/build result;
  - [ ] next recommended action.
- [ ] Update `generated-files.yaml` only after successful apply.
- [ ] Keep generated project package versions and contract hash in sync with metadata.

Implementation notes:

- MVP conflict policy should be conservative: no automatic merge for manually edited Razor/CSS.
- It is acceptable to require a human to resolve conflicts manually and rerun `-Scope conflicts`.
- Keep generated behavior tied to Starter/Preset/Presentation descriptors, not direct Commerce Node calls.

QA:

```powershell
.\tools\BlazorShop.AI.StorefrontBuilder\regenerate-storefront.ps1 -ProjectRoot artifacts/storefront-builder/generated/BlazorShop.Storefront.GeneratedProof -Scope all -WhatIf
.\tools\BlazorShop.AI.StorefrontBuilder\regenerate-storefront.ps1 -ProjectRoot artifacts/storefront-builder/generated/BlazorShop.Storefront.GeneratedProof -Scope all
.\tools\BlazorShop.AI.StorefrontBuilder\regenerate-storefront.ps1 -ProjectRoot artifacts/storefront-builder/generated/BlazorShop.Storefront.GeneratedProof -Scope conflicts
.\scripts\qa\run-storefront-builder-generated-proof.ps1 -ProofLevel Structure
```

Add end-to-end regeneration tests for:

- [ ] no-op regeneration produces no file diff;
- [ ] CSS-only regeneration touches only CSS and related manifest/report files;
- [ ] page-only regeneration touches only selected page files and manifest/report files;
- [ ] component-only regeneration touches only selected component files and manifest/report files;
- [ ] manually edited generated Razor file becomes conflict;
- [ ] manually edited CSS becomes conflict or managed update according to declared ownership;
- [ ] user-owned custom file is preserved;
- [ ] protected file modification fails validation;
- [ ] obsolete generated file is reported, not deleted silently;
- [ ] failed post-update build rolls back generated changes.

Exit gate:

- [ ] Regeneration is safe for real edited generated stores.
- [ ] No user-owned or protected file can be overwritten.
- [ ] No-op regeneration is deterministic.
- [ ] Conflict output is specific enough for a developer or AI agent to resolve without guessing.

## Phase 2.7 - Generated Proof, QA, CI, And Documentation Closure

Goal: make release confidence depend on real generated project behavior, not only source project smoke tests.

Tasks:

- [ ] Extend generated proof to cover full lifecycle:
  - [ ] clean previous proof output under safe root;
  - [ ] generate project from Starter;
  - [ ] restore from local packages;
  - [ ] build generated project;
  - [ ] static validation;
  - [ ] package/reference isolation gate;
  - [ ] visual consumer boundary gate;
  - [ ] fast browser proof;
  - [ ] full fixture-backed browser proof;
  - [ ] regenerate no-op proof;
  - [ ] manual-edit conflict fixture proof;
  - [ ] post-regeneration build proof.
- [ ] Add a CI-friendly regeneration ownership gate that does not require live Commerce Node data.
- [ ] Keep `FoundationFunctionalFast` as PR-safe browser proof.
- [ ] Keep `FoundationFunctionalFull` as manual/scheduled/release proof with fixture-backed store/category/product/page/payment data.
- [ ] Ensure COD/browser network regression remains covered by generated proof where payment is safe to place real test orders.
- [ ] Ensure direct Commerce Node browser-call rejection remains covered.
- [ ] Ensure generated proof reports are written under generated artifact root and are not committed by default.
- [ ] Update docs:
  - [ ] `docs/architecture/11-storefront-builder.md`;
  - [ ] `docs/agents/storefront-builder.md`;
  - [ ] `docs/visual-reverse-engineering-skill/README.md`;
  - [ ] `docs/visual-reverse-engineering-skill/reference.md`;
  - [ ] `docs/visual-reverse-engineering-skill/how-to-generate-and-validate.md`;
  - [ ] `docs/visual-reverse-engineering-skill/tutorial-generated-proof.md`.
- [ ] Update CI workflow references if new gates are added.
- [ ] Update QA docs to say generated Storefront proof is the release proof, while V2 remains a canonical product host QA surface.

QA:

```powershell
.\scripts\qa\run-storefront-client-regeneration-gate.ps1
.\scripts\qa\run-storefront-builder-generated-proof.ps1 -ProofLevel Structure
.\scripts\qa\run-storefront-builder-generated-proof.ps1 -ProofLevel FoundationFunctionalFast
.\scripts\qa\run-storefront-builder-generated-proof.ps1 -ProofLevel FoundationFunctionalFull
dotnet test BlazorShop.Tests.V2\BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~StorefrontBuilder"
```

Exit gate:

- [ ] Generated proof can be recreated from source.
- [ ] Generated proof can be safely regenerated.
- [ ] Generated proof preserves user edits and reports conflicts.
- [ ] Generated proof has no forbidden references.
- [ ] Generated browser flows pass for cart, checkout entry, account route, SEO, consent, missing slug, and direct Commerce Node call rejection.
- [ ] Docs explain create/update/validate/release workflows clearly enough for another agent to run them without guessing.

## Final Definition Of Done

Phase 2 is complete only when all checks below pass:

- [ ] Canonical OpenAPI contract is outside test project ownership.
- [ ] Generated Storefront.Client regeneration is deterministic.
- [ ] Storefront.Client has no backend source dependency.
- [ ] Runtime owns generated transport and remains server/BFF-only.
- [ ] Presentation owns shared app/page/BFF/SEO/media composition.
- [ ] Browser owns browser-safe same-origin primitives.
- [ ] Storefront.Components contains no shared visual wrappers, no `Features` folder, no visual class bags, and no V2 route defaults.
- [ ] V2 owns V2 visual markup/CSS/layout only.
- [ ] Starter owns neutral markup/CSS/layout and the starter generation contract.
- [ ] Generated `BlazorShop.Storefront.{Name}` owns generated visual markup/CSS/layout/assets/copy.
- [ ] Generated projects do not reference Storefront V2, backend/core/API projects, Control Plane Web, Commerce Node API, or `Web.SharedV2` business contracts.
- [ ] Create generator is staged, atomic, and rollback-safe.
- [ ] Regenerate engine is ownership-aware and conflict-safe.
- [ ] `generated-files.yaml` is computed from real files and detects manual edits.
- [ ] Static validation catches protected-file, forbidden-reference, direct-transport, missing-artifact, and invalid-manifest failures.
- [ ] Structure proof passes.
- [ ] FoundationFunctionalFast proof passes.
- [ ] FoundationFunctionalFull proof passes before release closure.
- [ ] CI includes deterministic client generation, StorefrontBuilder structure proof, fast generated browser proof, isolation gate, and regeneration ownership gate.
- [ ] Documentation and agent guidance describe the final workflow.

## Not In Scope For Phase 2

- [ ] AI visual generator prompt orchestration.
- [ ] Semantic Razor merge.
- [ ] Multi-framework React/Next/Vue generated starter.
- [ ] Live production deployment of generated stores.
- [ ] Commerce domain feature expansion unrelated to generated storefront safety.
- [ ] New payment/shipping/tax providers.
- [ ] Rewriting V2 visuals.

## Autoplan Review Audit

CEO review:

- The plan focuses on the highest-risk commercial outcome: generated stores must be safe to create, edit, regenerate, validate, and release.
- AI Generator is deliberately deferred until deterministic generation and regeneration are proven.
- The phase avoids adding broad new ecommerce features while boundary and generator safety remain unresolved.

Engineering review:

- The biggest risk is unsafe regeneration overwriting human-tuned storefronts; Phases 2.5 and 2.6 make ownership and conflict handling the center of the work.
- The second risk is hidden transport coupling; Phase 2.2 keeps V2/Starter/generated projects visual-only and locks Runtime/Presentation/Browser boundaries.
- The third risk is unreproducible generated artifacts; Phase 2.4 makes creation atomic and Phase 2.7 proves full lifecycle behavior.

Design review:

- Starter remains neutral and generated stores own their visual output.
- No shared visual wrapper or Components `Features` folder is allowed to become the design contract.
- Visual customization remains movable because generated components bind to semantic descriptors and same-origin BFF actions.

Developer experience review:

- Scripts must fail before writes on invalid inputs.
- Dry-run output must show planned actions, skipped files, conflicts, and next commands.
- Validation errors must identify file, rule, cause, and repair path.
- Docs must let another agent generate, validate, regenerate, and release proof without reading implementation scripts first.

## Suggested Commit Slices

1. Contract/client metadata and validation closure.
2. Runtime/Presentation/Browser/V2 boundary guardrails.
3. Starter contract completeness and validation.
4. Atomic create generator.
5. Ownership manifest engine.
6. Safe regenerate engine.
7. Proof/CI/docs closure.

Each commit should include focused tests and update this todo file from `[ ]` to `[x]` only for completed, verified items.

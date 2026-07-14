# Remove Legacy Internal Storefront API - Autoplan

Date: 2026-07-14
Status: Ready for phased implementation
Mode: HOLD SCOPE cleanup plan
Primary boundary: `BlazorShop.PresentationV2/BlazorShop.CommerceNode.API`

## Objective

Remove the legacy Commerce Node `api/internal/*` compatibility surface now that Storefront V2 uses scoped Storefront routes under `api/storefront/stores/{storeKey}/*` and the scoped QA pass has completed.

This plan removes active runtime support, Swagger exposure, and active architecture/QA guidance for legacy Internal. Historical migration documents remain as history unless they are likely to mislead future work.

## Premises

| Premise | Evidence | Risk if wrong | Plan response |
|---|---|---|---|
| Storefront V2 no longer depends on `api/internal/*`. | 2026-07-14 Storefront client tests assert scoped `api/storefront/stores/default/*` auth/catalog calls. `QA-StorefrontV2.todo.md` records scoped API/client verification. | Removing Internal would break Storefront runtime flows. | Phase 1 adds route/client guard tests before deleting runtime routes. |
| `api/internal/*` is compatibility-only and approved for removal after scoped QA. | `AGENTS.md`, architecture docs, and `BlazorShop.CommerceNode.ApiSwaggerRescope.todo.md` all state removal after scoped Storefront migration and QA. | Keeping it increases public surface and future agent confusion. | Phases 3-5 remove runtime, Swagger, and active guidance together. |
| Control Plane Web/API should not call `api/internal/*`. | Existing boundary guidance forbids Control Plane Web direct CommerceNode calls; Control Plane gateway target is `api/commerce/*`. | Removal could expose a hidden gateway dependency. | Phase 1 searches active code/tests and includes boundary tests in verification. |
| `X-Store-Key` is still useful outside legacy Internal for generic non-route resolution. | `CommerceStoreContext` uses `X-Store-Key`, `X-Store-Host`, forwarded host, and request host outside specific route groups. | Removing all header resolution would break media/debug or host-scoped flows. | Phase 3 removes only the `/api/internal` branch and its legacy-only error. |

## What Already Exists

### Runtime

- Scoped Storefront controllers live in `BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/Controllers/StorefrontScopedControllers.cs`.
- Legacy Internal controllers live in separate files:
  - `StorefrontAuthController.cs`
  - `StorefrontCartController.cs`
  - `StorefrontCatalogController.cs`
  - `StorefrontNewsletterController.cs`
  - `StorefrontOrdersController.cs`
  - `StorefrontPagesController.cs`
  - `StorefrontPaymentsController.cs`
  - `StorefrontRecommendationsController.cs`
  - `StorefrontSeoController.cs`
  - `StorefrontStoreController.cs`
- Both scoped and legacy controllers currently inherit `StorefrontInternalControllerBase`.
- Store scope compatibility for `api/internal/*` lives in `BlazorShop.Infrastructure/Data/CommerceNode/Services/CommerceStoreContext.cs`.
- Legacy Swagger document and `X-Store-Key` operation filter live in `BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/Swagger/CommerceNodeSwaggerExtensions.cs`.

### Tests And QA

- Storefront V2 client tests already assert scoped catalog/auth routes.
- Current focused test targets:
  - `BlazorShop.Tests/PresentationV2/Storefront/StorefrontV2ApiClientTests.cs`
  - `BlazorShop.Tests/PresentationV2/Storefront/StorefrontV2AuthClientTests.cs`
  - `BlazorShop.Tests/PresentationV2/Storefront/StorefrontV2HostSmokeTests.cs`
- There is not yet a CommerceNode API route boundary test that fails on `api/internal` route attributes.

### Active Docs

- `AGENTS.md` and `docs/architecture/*` still describe `api/internal/*` as temporary compatibility.
- `QA-CommerceNode.todo.md` still tracks Internal endpoint checks.
- `QA-StorefrontV2.todo.md` still contains historical Internal setup notes.
- `BlazorShop.CommerceNode.ApiSwaggerRescope.todo.md` explicitly leaves Internal removal as the follow-up.

## Not In Scope

- Do not remove legacy `BlazorShop.Presentation/*` projects.
- Do not change Commerce Admin `api/commerce/*` routes.
- Do not change scoped Storefront route contract except where tests prove a missed dependency.
- Do not delete fields or config names containing `InternalUrl`; those belong to deployment internals, not `api/internal/*`.
- Do not rewrite every historical plan file just to erase old route references.
- Do not add a new auth scheme, gateway, feature flag, or migration for this cleanup.

## Architecture Target

```text
Storefront V2
  -> Commerce Node API
     -> api/storefront/stores/{storeKey}/*

Control Plane Web
  -> Control Plane API
     -> api/commerce/* gateway calls to Commerce Node API

Removed from active runtime:
  x api/internal/*
  x /swagger/legacy-internal/swagger.json
  x LegacyInternal* Swagger symbols
  x StorefrontInternalControllerBase naming
  x api/internal-only X-Store-Key branch
```

## Implementation Alternatives Reviewed

| Approach | Summary | Effort | Risk | Decision |
|---|---|---:|---:|---|
| A. Minimal deletion | Delete legacy controllers and Swagger in one pass, fix compiler errors. | S | High | Rejected. Too easy to miss hidden dependency or keep misleading base names. |
| B. Guarded phased removal | Add tests/search gates first, rename shared base, remove runtime, then Swagger/docs. | M | Low | Selected. Best balance of small commits and low regression risk. |
| C. Feature-flagged retirement | Keep Internal behind config and disable by default. | M | Medium | Rejected. Leaves dead route surface and future agent confusion. |

## Phase Plan

### Phase 0 - Baseline And Worktree Guard

Goal:

- Confirm the implementation starts from the current dirty worktree without overwriting unrelated changes.
- Record the baseline for plan execution.

Tasks:

- Run `git status --short`.
- Read current diffs for files already modified before this plan:
  - `BlazorShop.Tests/Application/Services/Payment/CartServiceTests.cs`
  - `BlazorShop.Tests/Infrastructure/Repositories/ProductRecommendationRepositoryTests.cs`
  - `BlazorShop.Tests/PresentationV2/Storefront/StorefrontV2HostSmokeTests.cs`
  - `docs/refactor-control-Commerce-storefront/QA-StorefrontV2.todo.md`
- Do not revert or rewrite unrelated changes.

Exit criteria:

- Worktree state is understood.
- Implementation commit scope can be separated from pre-existing changes.

Commit boundary:

- No commit unless this plan file itself is committed as a planning commit.
- Suggested message if committed alone: `docs: plan legacy internal storefront removal`.

### Phase 1 - Prove No Active Internal Dependency

Goal:

- Make reintroduction or hidden active use of `api/internal/*` visible before runtime removal.

Tasks:

- Search active code/tests for:
  - `api/internal`
  - `legacy-internal`
  - `LegacyInternal`
  - `StorefrontInternal`
- Confirm Storefront V2 URL construction uses scoped base addresses:
  - `Program.cs` `ConfigureStorefrontHttpClient`
  - `StorefrontApiClient`
  - `StorefrontAuthClient`
  - SEO redirects
  - sitemap
  - cart/product refresh
  - checkout/payment methods
- Add or update Storefront V2 tests so PresentationV2 Storefront clients fail if they emit `api/internal/*`.
- Add CommerceNode API route boundary tests that enumerate controller route attributes and fail on any `api/internal` route.
- If test project needs a CommerceNode API project reference for route reflection, add it intentionally with an alias if required by existing test project conventions.

Candidate files:

- `BlazorShop.Tests/PresentationV2/Storefront/StorefrontV2ApiClientTests.cs`
- `BlazorShop.Tests/PresentationV2/Storefront/StorefrontV2AuthClientTests.cs`
- New test file under `BlazorShop.Tests/PresentationV2/CommerceNode/`
- `BlazorShop.Tests/BlazorShop.Tests.csproj` if a CommerceNode API project reference is required.

Exit criteria:

- No active Storefront V2 client uses `api/internal/*`.
- Tests fail if any CommerceNode API controller keeps or reintroduces an `api/internal` route after Phase 3.

Verification:

```powershell
dotnet test BlazorShop.Tests/BlazorShop.Tests.csproj --no-restore --filter "FullyQualifiedName~PresentationV2.Storefront|FullyQualifiedName~CommerceNode"
```

Commit boundary:

- Commit only tests/project reference changes if they pass independently.
- Suggested message: `test: guard against legacy internal storefront routes`.

### Phase 2 - Rename Shared Storefront Controller Base

Goal:

- Remove misleading `Internal` naming from code that remains after legacy controllers are deleted.

Tasks:

- Rename `StorefrontInternalControllerBase.cs` to `StorefrontApiControllerBase.cs`.
- Rename type `StorefrontInternalControllerBase` to `StorefrontApiControllerBase`.
- Update `StorefrontScopedControllers.cs` to inherit from the neutral base.
- Keep response helper behavior unchanged.
- Do not mix this rename with behavior changes.

Candidate files:

- `BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/Controllers/StorefrontInternalControllerBase.cs`
- `BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/Controllers/StorefrontApiControllerBase.cs`
- `BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/Controllers/StorefrontScopedControllers.cs`

Exit criteria:

- Scoped controllers compile with no `StorefrontInternalControllerBase` references.
- `rg "StorefrontInternalControllerBase" BlazorShop.PresentationV2/BlazorShop.CommerceNode.API` returns no hits.

Verification:

```powershell
dotnet build BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/BlazorShop.CommerceNode.API.csproj --no-restore
```

Commit boundary:

- Commit only the neutral base rename.
- Suggested message: `refactor: rename storefront api controller base`.

### Phase 3 - Remove Legacy Internal Runtime Surface

Goal:

- Delete active `api/internal/*` endpoints from CommerceNode API runtime.

Tasks:

- Delete legacy Internal controller files:
  - `StorefrontAuthController.cs`
  - `StorefrontCartController.cs`
  - `StorefrontCatalogController.cs`
  - `StorefrontNewsletterController.cs`
  - `StorefrontOrdersController.cs`
  - `StorefrontPagesController.cs`
  - `StorefrontPaymentsController.cs`
  - `StorefrontRecommendationsController.cs`
  - `StorefrontSeoController.cs`
  - `StorefrontStoreController.cs`
- Remove the `/api/internal` branch from `CommerceStoreContext`.
- Keep default non-route store resolution using:
  - `X-Store-Key`
  - `X-Store-Host`
  - `X-Forwarded-Host`
  - request host
- Do not change `api/storefront/stores/{storeKey}/*` route-scope behavior.

Candidate files:

- `BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/Controllers/Storefront*Controller.cs`
- `BlazorShop.Infrastructure/Data/CommerceNode/Services/CommerceStoreContext.cs`

Exit criteria:

- Build has zero `api/internal` controller routes.
- Store context no longer has a legacy-only `X-Store-Key header is required for legacy internal API.` error.
- Phase 1 route boundary test passes.

Verification:

```powershell
rg -n "api/internal|legacy internal API|StorefrontInternal" BlazorShop.PresentationV2/BlazorShop.CommerceNode.API BlazorShop.Infrastructure/Data/CommerceNode/Services/CommerceStoreContext.cs
dotnet build BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/BlazorShop.CommerceNode.API.csproj --no-restore
dotnet test BlazorShop.Tests/BlazorShop.Tests.csproj --no-restore --filter "FullyQualifiedName~CommerceNode"
```

Commit boundary:

- Commit runtime deletion and store-context cleanup together.
- Suggested message: `refactor: remove legacy internal storefront controllers`.

### Phase 4 - Remove Legacy Internal Swagger

Goal:

- Remove `legacy-internal` from generated Swagger docs and UI.

Tasks:

- Remove `LegacyInternalDocumentName`.
- Remove Legacy Internal Swagger doc registration.
- Remove Legacy Internal Swagger UI endpoint.
- Remove `LegacyInternalStoreKeyHeaderOperationFilter`.
- Keep Commerce Admin Swagger unchanged:
  - `/swagger/commerce-admin/swagger.json`
  - node credential headers
  - `storeKey` query for store-scoped admin endpoints
- Keep Storefront Swagger unchanged:
  - `/swagger/storefront/swagger.json`
  - `api/storefront/stores/{storeKey}/*` only
  - no node credentials or `X-Store-Key`.

Candidate files:

- `BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/Swagger/CommerceNodeSwaggerExtensions.cs`

Exit criteria:

- `rg "LegacyInternal|legacy-internal|api/internal" BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/Swagger` returns no hits.
- Remaining Swagger document predicates still include only their intended route groups.

Verification:

```powershell
dotnet build BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/BlazorShop.CommerceNode.API.csproj --no-restore
dotnet test BlazorShop.Tests/BlazorShop.Tests.csproj --no-restore --filter "FullyQualifiedName~CommerceNode"
```

Optional HTTP smoke when CommerceNode API is running:

```text
GET /swagger/storefront/swagger.json -> 200, paths start with api/storefront/stores/{storeKey}/
GET /swagger/commerce-admin/swagger.json -> 200, paths start with api/commerce/
GET /swagger/legacy-internal/swagger.json -> 404
GET /api/internal/catalog/categories -> 404
```

Commit boundary:

- Commit Swagger cleanup separately from controller deletion.
- Suggested message: `docs: remove legacy internal swagger surface`.

### Phase 5 - Update Active Documentation And QA

Goal:

- Make active guidance match the new runtime state.

Tasks:

- Update current guidance:
  - `AGENTS.md`
  - `docs/architecture/03-runtime-boundaries.md`
  - `docs/architecture/05-project-and-folder-guide.md`
  - `docs/architecture/06-feature-map.md`
  - `docs/architecture/08-agent-decision-rules.md`
  - `docs/architecture/BlazorShop.ArchitectureDocumentation.todo.md`
  - `docs/refactor-control-Commerce-storefront/QA-CommerceNode.todo.md`
  - `docs/refactor-control-Commerce-storefront/QA-StorefrontV2.todo.md`
  - `docs/refactor-control-Commerce-storefront/BlazorShop.CommerceNode.ApiSwaggerRescope.todo.md`
- Active architecture and QA docs should say `api/internal/*` has been removed.
- Update QA items that previously expected legacy Internal 200s to now expect 404/removal where appropriate.
- Add superseded notes only to historical docs that are likely to mislead current implementation work.

Historical-doc policy:

- Do not erase migration history wholesale.
- Prefer a short note such as: `Superseded on 2026-07-14 by BlazorShop.CommerceNode.RemoveLegacyInternal.autoplan.md; api/internal/* is no longer active runtime guidance.`

Exit criteria:

- Active docs no longer instruct agents to use, add, or verify live `api/internal/*`.
- `api/internal/*` references remaining under older plan files are clearly historical or superseded.

Verification:

```powershell
rg -n "api/internal|legacy-internal|LegacyInternal|StorefrontInternal" AGENTS.md docs/architecture docs/refactor-control-Commerce-storefront/QA-CommerceNode.todo.md docs/refactor-control-Commerce-storefront/QA-StorefrontV2.todo.md docs/refactor-control-Commerce-storefront/BlazorShop.CommerceNode.ApiSwaggerRescope.todo.md
```

Commit boundary:

- Commit active doc/QA updates separately.
- Suggested message: `docs: mark legacy internal storefront api removed`.

### Phase 6 - End-To-End Verification And Final Commit Hygiene

Goal:

- Prove scoped Storefront routes still work and old Internal routes are gone.

Automated checks:

```powershell
dotnet build BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/BlazorShop.CommerceNode.API.csproj --no-restore
dotnet test BlazorShop.Tests/BlazorShop.Tests.csproj --no-restore --filter "FullyQualifiedName~PresentationV2.Storefront|FullyQualifiedName~CommerceNode|FullyQualifiedName~ControlPlaneArchitectureBoundaryTests"
```

HTTP smoke against running CommerceNode API:

```text
GET /swagger/storefront/swagger.json
  expect 200
  expect only api/storefront/stores/{storeKey}/* paths

GET /swagger/commerce-admin/swagger.json
  expect 200
  expect api/commerce/* paths

GET /swagger/legacy-internal/swagger.json
  expect 404

GET /api/internal/catalog/categories
  expect 404
```

Storefront V2 smoke:

- Home/category/product/cart/checkout/auth flows still call scoped Storefront APIs.
- Browser/network/log scan shows zero `api/internal`.
- If browser behavior changes, run Playwright; otherwise document why HTTP/client tests are sufficient.

Final checks:

- `git status --short`
- `git diff --stat`
- Confirm each phase commit excludes unrelated pre-existing changes unless they were intentionally part of that phase.

Commit boundary:

- If prior phases were committed independently, Phase 6 may be no-op or a docs-only QA-result commit.
- Suggested message if QA notes are updated: `test: verify legacy internal storefront removal`.

## Failure Modes Registry

| Failure mode | Severity | Detection | Mitigation |
|---|---:|---|---|
| Hidden Storefront V2 service still calls `api/internal/*`. | High | Phase 1 search and client tests; Phase 6 network/log scan. | Add guard tests before deleting runtime routes. |
| Scoped controllers lose shared response helper during base rename. | High | Phase 2 build. | Rename base first; do not inline helper logic into controllers. |
| Route boundary test accidentally scans legacy `BlazorShop.Presentation/*`. | Medium | Test false positives from legacy projects. | Scope test to CommerceNode API assembly only. |
| Swagger cleanup breaks Admin or Storefront docs. | Medium | Phase 4 build/test and Phase 6 HTTP smoke. | Keep existing Admin/Storefront predicates and filters intact. |
| Docs cleanup erases useful migration history. | Medium | Diff review. | Update active docs; add superseded notes instead of rewriting old plans. |
| Removing `/api/internal` branch removes generic `X-Store-Key` support. | Medium | Code review and media/debug smoke. | Remove only the legacy route branch; keep generic header/host fallback. |
| Commit accidentally includes unrelated dirty worktree changes. | Medium | Phase 0 and final `git diff --stat`. | Stage phase files explicitly; do not use broad `git add .` while unrelated changes exist. |

## Test Coverage Map

```text
Storefront V2 clients
  -> scoped base URL construction
  -> tests: StorefrontV2ApiClientTests, StorefrontV2AuthClientTests

CommerceNode API controllers
  -> route attributes
  -> tests: new PresentationV2 CommerceNode route boundary test

CommerceStoreContext
  -> store scope source:
       api/storefront/stores/{storeKey}/* => route value
       api/commerce/admin/*              => storeKey query
       other/runtime/media paths         => header/host fallback
  -> tests: focused unit or route smoke if existing seams allow

Swagger
  -> commerce-admin document
  -> storefront document
  -> legacy-internal document absent
  -> tests/smoke: HTTP smoke when API is running
```

## Autoplan Review Summary

CEO review:

- This is the right problem to solve now: removing a completed compatibility path reduces maintenance and security surface.
- The 12-month ideal is a CommerceNode API with two active route families only: `api/commerce/*` for admin/control and `api/storefront/stores/{storeKey}/*` for public/customer storefront.
- Scope expansion is not recommended; this is a cleanup after a completed migration, not a new capability.

Design review:

- Skipped. No UI or visual behavior is in scope.

Engineering review:

- Use guarded phased removal rather than one large deletion.
- Rename the shared controller base before deleting legacy controllers.
- Add negative route tests before removing runtime routes.
- Keep generic header/host store resolution outside Internal.

DX review:

- Developer-facing surface is Swagger/docs/API route clarity.
- Removing `legacy-internal` from Swagger and active docs is part of the product quality of this cleanup, not optional polish.
- Active docs should be explicit that historical plans may mention `api/internal/*` as past migration context.

## Decision Audit Trail

| # | Phase | Decision | Classification | Principle | Rationale | Rejected |
|---|---|---|---|---|---|---|
| 1 | Scope | Remove runtime `api/internal/*` now that scoped QA passed. | Auto-decided | Complete migration, reduce surface area | The compatibility window has ended and keeping the route family creates future ambiguity. | Keep Internal until an unspecified later phase. |
| 2 | Approach | Use guarded phased removal. | Auto-decided | Small reversible commits | Route guards and separate commits make the cleanup reviewable. | Delete everything in one pass. |
| 3 | Tests | Add negative route/client guards before runtime deletion. | Auto-decided | Make regressions visible | Removal is only reliable if reintroduction fails tests. | Build-only verification. |
| 4 | Base class | Rename `StorefrontInternalControllerBase` before deleting legacy controllers. | Auto-decided | Explicit naming | Scoped controllers still need helpers, but `Internal` naming becomes false after removal. | Inline response helpers into every scoped controller. |
| 5 | Store scope | Remove only `/api/internal` special-case from `CommerceStoreContext`. | Auto-decided | Preserve working boundaries | Generic header/host fallback may still be used by non-Internal flows. | Remove all `X-Store-Key` handling. |
| 6 | Docs | Update active docs and QA; preserve historical docs with superseded notes only when needed. | Taste decision, recommended | Low churn with clear guidance | Active guidance must be clean, while historical docs explain migration context. | Rewrite every old plan file to erase all mentions. |
| 7 | Commits | Commit each implementation phase separately. | Auto-decided | Reviewability | The repo already has unrelated dirty changes; phase commits reduce accidental staging risk. | One broad cleanup commit. |

## Final Approval Gate

Recommendation: implement Approach B, guarded phased removal, with separate commits per phase.

No user challenge remains. The only taste choice is doc cleanup depth:

- Recommended: update active docs and add superseded notes to misleading historical docs only.
- Alternative: rewrite every historical `api/internal` mention. This gives textual completeness but creates high churn and loses migration context.

## GSTACK REVIEW REPORT

| Run | Status | Findings | Resolution |
|---|---|---:|---|
| CEO | Complete | 1 | Hold scope; remove completed compatibility surface. |
| Design | Skipped | 0 | No UI scope. |
| Engineering | Complete | 4 | Add guards, rename base first, remove runtime, remove Swagger/docs. |
| DX | Complete | 1 | Remove obsolete Swagger/docs guidance as part of API clarity. |

VERDICT: APPROVED FOR PHASED IMPLEMENTATION.

NO UNRESOLVED DECISIONS

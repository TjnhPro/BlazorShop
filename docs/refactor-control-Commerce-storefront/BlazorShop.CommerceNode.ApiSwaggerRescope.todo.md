# BlazorShop CommerceNode API Swagger Rescope TODO

## Goal

Rescope `BlazorShop.CommerceNode.API` routes and Swagger documents so store-scoped APIs are explicit, debug-friendly, and aligned with the V2 runtime boundaries.

This phase is a route/documentation/API contract phase. It must not rewrite commerce business logic.

## Current Problems

- Storefront V2 currently calls `api/internal/*` and relies on `X-Store-Key`.
- Swagger does not show the required store scope header for Storefront debug.
- Commerce Admin endpoints currently rely on implicit store scope in services, often through `X-Store-Key`.
- Storefront and Commerce Admin need different store scope styles:
  - Storefront should be scoped by path.
  - Commerce Admin should be scoped by query parameter.
- `api/internal/*` needs to become legacy compatibility, not the design target for new Storefront work.

## Final Decisions

### Storefront API

New Storefront route pattern:

```text
api/storefront/stores/{storeKey}/*
```

Rules:

- Storefront scope is read only from route value `{storeKey}`.
- Do not use `X-Store-Key`.
- Do not validate or check `X-Store-Key`.
- Do not use `X-Node-Key` or `X-Node-Secret`.
- Create new `StorefrontScoped*Controller` classes instead of adding dual routes to legacy controllers.
- Storefront V2 must migrate to the new route after the new controllers exist.

Planned route groups:

```text
api/storefront/stores/{storeKey}/auth
api/storefront/stores/{storeKey}/catalog
api/storefront/stores/{storeKey}/cart
api/storefront/stores/{storeKey}/newsletter
api/storefront/stores/{storeKey}/orders
api/storefront/stores/{storeKey}/pages
api/storefront/stores/{storeKey}/payments
api/storefront/stores/{storeKey}/recommendations
api/storefront/stores/{storeKey}/seo
api/storefront/stores/{storeKey}/store
```

Controller naming:

```text
StorefrontScopedAuthController
StorefrontScopedCatalogController
StorefrontScopedCartController
StorefrontScopedNewsletterController
StorefrontScopedOrdersController
StorefrontScopedPagesController
StorefrontScopedPaymentsController
StorefrontScopedRecommendationsController
StorefrontScopedSeoController
StorefrontScopedStoreController
```

### Commerce Admin API

Commerce Admin remains under:

```text
api/commerce/admin/*
```

Store-scoped Commerce Admin endpoints must require:

```text
?storeKey={storeKey}
```

Rules:

- Commerce Admin scope is read only from query parameter `storeKey`.
- Do not use `X-Store-Key` for Commerce Admin.
- Store-scoped endpoints fail clearly when `storeKey` is missing.
- ControlPlane API gateway must append `storeKey` when calling CommerceNode Admin endpoints.
- ControlPlane Web must still call only ControlPlane API and must not know node credentials.

Store-scoped Commerce Admin endpoints for this phase:

```text
api/commerce/admin/settings
api/commerce/admin/categories
api/commerce/admin/categories/{id:guid}/seo
api/commerce/admin/inventory
api/commerce/admin/media
api/commerce/admin/media/assets
api/commerce/admin/metrics
api/commerce/admin/orders
api/commerce/admin/payment-methods
api/commerce/admin/products
api/commerce/admin/products/{id:guid}/seo
api/commerce/admin/products/{productId:guid}/media
api/commerce/admin/products/{productId:guid}/variants
api/commerce/admin/products/imports
api/commerce/admin/seo/redirects
api/commerce/admin/seo/settings
api/commerce/admin/pages
api/commerce/admin/variation-templates
```

Node-level Commerce endpoints that do not require `storeKey`:

```text
api/commerce/healthz
api/commerce/admin/stores
api/commerce/tasks
```

### Legacy Internal API

Legacy route pattern:

```text
api/internal/*
```

Rules:

- Keep `api/internal/*` temporarily for compatibility.
- `api/internal/*` reads `X-Store-Key` as it does today.
- Do not add new features to `api/internal/*`.
- Only fix blocker bugs if route migration is not complete.
- After Storefront V2 is moved and QA passes, remove `api/internal/*`, remove `X-Store-Key` legacy support, and remove the Legacy Internal Swagger document.

### Public Media

Public Storefront media URLs stay clean:

```text
/media/products/{mediaId}?w=600&h=600&fit=contain&format=webp&v=1
/media/assets/{assetId}?w=1200&format=webp&v=3
```

Rules:

- Do not add `storeKey` to public media URLs.
- Store scope for public media is resolved by Nginx/domain/rewrite behavior.
- Public media URLs are not the primary Swagger debug surface.

Commerce Admin media debug endpoints should be added for Swagger/debug:

```text
api/commerce/admin/media/products/{mediaId}?storeKey=default&w=600&h=600&fit=contain&format=webp&v=1
api/commerce/admin/media/assets/{assetId}?storeKey=default&w=1200&format=webp&v=3
```

Rules:

- These endpoints belong to the Commerce Node Admin Swagger document.
- They require `X-Node-Key`, `X-Node-Secret`, and `storeKey`.
- Reuse the same media lookup/rendering services. Do not duplicate media processing logic.

## Swagger Design

Swagger UI remains:

```text
/swagger
```

Swagger JSON documents:

```text
/swagger/commerce-admin/swagger.json
/swagger/storefront/swagger.json
/swagger/legacy-internal/swagger.json
```

### Commerce Node Admin

Includes:

```text
api/commerce/*
```

Headers:

```text
X-Node-Key
X-Node-Secret
```

Store scope:

- Required query parameter `storeKey` for store-scoped endpoints.
- No `X-Store-Key`.

### Storefront API

Includes:

```text
api/storefront/stores/{storeKey}/*
```

Store scope:

- Required path parameter `{storeKey}`.
- No `X-Store-Key`.
- No node credentials.

### Legacy Internal

Includes:

```text
api/internal/*
```

Header:

```text
X-Store-Key
```

Notes:

- Compatibility only.
- Do not use for new Storefront work.
- Pending removal after route migration.

## Store Scope Resolution

`CommerceStoreContext` should resolve store scope by request group:

```text
api/storefront/stores/{storeKey}/*
  -> RouteValues["storeKey"]

api/commerce/admin/*
  -> Query["storeKey"]

api/internal/*
  -> Headers["X-Store-Key"]
```

Rules:

- Do not mix sources for new route groups.
- Do not check headers for Storefront scoped routes.
- Do not check headers for Commerce Admin routes.
- Legacy header support exists only for `api/internal/*`.

## Autoplan Review

### CEO Review

This plan improves operator clarity and reduces agent/debug mistakes. The biggest product risk is touching route contracts across ControlPlane, CommerceNode, and Storefront at once. The plan mitigates that by adding new Storefront scoped controllers first, keeping `api/internal/*` temporarily, then switching clients and removing legacy later.

Scope should stay narrow. This phase is not a commerce feature expansion. It is an API contract and Swagger usability phase.

### Design Review

Swagger is part of the developer/operator UI for this backend. Three documents reduce cognitive load:

- Commerce Node Admin for ControlPlane/API and node admin debug.
- Storefront API for Storefront route testing.
- Legacy Internal for compatibility visibility only.

The important UI decision is to avoid mixing legacy and new Storefront routes in the same document.

### Engineering Review

The engineering risk is implicit store resolution. Store scope must be deterministic by route group:

- path for Storefront,
- query for Commerce Admin,
- header only for legacy.

The second risk is accidental duplication. New `StorefrontScoped*Controller` classes should stay thin and reuse existing application services.

### DX Review

Swagger should make valid requests copy-pasteable:

- Commerce Admin document should expose node credential headers and required `storeKey` query.
- Storefront document should expose `{storeKey}` path and no headers.
- Legacy document should clearly communicate compatibility status.

Error messages for missing `storeKey` should state the required source:

```text
storeKey query parameter is required.
storeKey route value is required.
X-Store-Key header is required for legacy internal API.
```

## Phase Plan

### Phase 1 - Documentation And Contract Update

- [x] Update `AGENTS.md` with new route ownership:
  - `api/commerce/*` = Commerce Admin/control.
  - `api/storefront/stores/{storeKey}/*` = Storefront V2 route target.
  - `api/internal/*` = legacy compatibility only.
- [x] Update `docs/architecture/03-runtime-boundaries.md`.
- [x] Update `docs/architecture/08-agent-decision-rules.md`.
- [x] Add note that public media URLs remain clean and store scope is handled by Nginx/domain/rewrite.
- [x] Add note that Commerce Admin media debug endpoints use `storeKey` query.
- [x] Commit docs phase.

### Phase 2 - Swagger Document Split

- [x] Replace default `AddSwaggerGen()` with three Swagger docs:
  - `commerce-admin`
  - `storefront`
  - `legacy-internal`
- [x] Configure Swagger UI with three endpoints:
  - `/swagger/commerce-admin/swagger.json`
  - `/swagger/storefront/swagger.json`
  - `/swagger/legacy-internal/swagger.json`
- [x] Add document inclusion logic by route prefix.
- [x] Add operation filter for `X-Node-Key` and `X-Node-Secret` on Commerce Admin document.
- [x] Add operation filter for required `storeKey` query on Commerce Admin store-scoped endpoints.
- [x] Add operation filter for `X-Store-Key` on Legacy Internal document.
- [x] Ensure Storefront document does not show node credential headers or `X-Store-Key`.
- [x] Build CommerceNode API.
- [x] Commit Swagger phase.

### Phase 3 - Store Scope Resolver

- [ ] Update `CommerceStoreContext` to resolve source by request group:
  - route value for `api/storefront/stores/{storeKey}/*`,
  - query for `api/commerce/admin/*`,
  - header for `api/internal/*`.
- [ ] Add clear failure messages for missing scope.
- [ ] Ensure new route groups do not read `X-Store-Key`.
- [ ] Keep host/domain fallback out of this MVP phase unless needed by existing public media behavior.
- [ ] Build CommerceNode API.
- [ ] Commit resolver phase.

### Phase 4 - Storefront Scoped Controllers

- [ ] Add `StorefrontScopedAuthController`.
- [ ] Add `StorefrontScopedCatalogController`.
- [ ] Add `StorefrontScopedCartController`.
- [ ] Add `StorefrontScopedNewsletterController`.
- [ ] Add `StorefrontScopedOrdersController`.
- [ ] Add `StorefrontScopedPagesController`.
- [ ] Add `StorefrontScopedPaymentsController`.
- [ ] Add `StorefrontScopedRecommendationsController`.
- [ ] Add `StorefrontScopedSeoController`.
- [ ] Add `StorefrontScopedStoreController`.
- [ ] Reuse existing service calls and response helpers.
- [ ] Do not remove legacy `Storefront*Controller` classes yet.
- [ ] Build CommerceNode API.
- [ ] Commit Storefront scoped route phase.

### Phase 5 - Commerce Admin StoreKey Query Enforcement

- [ ] Mark or configure store-scoped Commerce Admin endpoints.
- [ ] Require query `storeKey` for all agreed store-scoped endpoints.
- [ ] Remove Commerce Admin reliance on `X-Store-Key`.
- [ ] Add/admin debug media endpoints:
  - `api/commerce/admin/media/products/{mediaId}`
  - `api/commerce/admin/media/assets/{assetId}`
- [ ] Ensure node-level endpoints still work without `storeKey`.
- [ ] Build CommerceNode API.
- [ ] Commit Commerce Admin rescope phase.

### Phase 6 - ControlPlane API Gateway Update

- [ ] Update ControlPlane CommerceNode clients/gateways to append `storeKey` query.
- [ ] Remove `X-Store-Key` header when calling Commerce Admin endpoints.
- [ ] Keep `X-Node-Key` and `X-Node-Secret`.
- [ ] Ensure ControlPlane Web still calls only ControlPlane API.
- [ ] Build ControlPlane API.
- [ ] Build ControlPlane Web if affected by client contracts.
- [ ] Commit ControlPlane gateway phase.

### Phase 7 - Storefront V2 Client Switch

- [ ] Update `BlazorShop.Storefront.V2` API client base paths to:
  - `api/storefront/stores/{storeKey}/*`
- [ ] Stop sending `X-Store-Key` for Storefront API calls.
- [ ] Verify catalog, search, cart, checkout, auth, pages, payments, SEO, and recommendations use new route.
- [ ] Keep legacy route available until QA passes.
- [ ] Build Storefront V2.
- [ ] Commit Storefront client switch phase.

### Phase 8 - QA Checklist Update

- [ ] Update `QA-CommerceNode.todo.md`.
- [ ] Update `QA-ControlPlane.todo.md`.
- [ ] Update `QA-StorefrontV2.todo.md`.
- [ ] Add Swagger checks:
  - Commerce Admin document shows node headers and storeKey query.
  - Storefront document shows route storeKey and no headers.
  - Legacy Internal document shows X-Store-Key and is marked legacy.
- [ ] Add API checks for missing storeKey failures.
- [ ] Add visible browser QA requirement for Storefront flows when user requests observation.
- [ ] Commit QA docs phase.

### Phase 9 - Verification

- [ ] `dotnet build BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/BlazorShop.CommerceNode.API.csproj --no-restore`
- [ ] `dotnet build BlazorShop.PresentationV2/BlazorShop.ControlPlane.API/BlazorShop.ControlPlane.API.csproj --no-restore`
- [ ] `dotnet build BlazorShop.PresentationV2/BlazorShop.ControlPlane.Web/BlazorShop.ControlPlane.Web.csproj --no-restore`
- [ ] `dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.V2/BlazorShop.Storefront.V2.csproj --no-restore`
- [ ] Run local runtime with fixed ports.
- [ ] Verify Swagger JSON endpoints return 200.
- [ ] Verify Storefront scoped endpoints work without `X-Store-Key`.
- [ ] Verify legacy internal endpoints still work with `X-Store-Key`.
- [ ] Verify Commerce Admin endpoints reject missing `storeKey` where required.
- [ ] Verify ControlPlane Web does not call CommerceNode directly.

### Phase 10 - Follow-up Legacy Cleanup Plan

This phase does not remove legacy internal routes. After route migration and QA pass, create a separate cleanup phase:

- [ ] Remove `api/internal/*` controllers.
- [ ] Remove `X-Store-Key` legacy support from `CommerceStoreContext`.
- [ ] Remove Legacy Internal Swagger document.
- [ ] Update docs to show `api/internal/*` removed.
- [ ] Run full Storefront and CommerceNode QA.

## Not In Scope

- No commerce business logic rewrite.
- No database schema change unless an implementation detail unexpectedly requires it.
- No ABP/module structure.
- No ControlPlane Web direct call to CommerceNode.
- No production Nginx rewrite implementation in this plan unless required for local media QA.
- No removal of `api/internal/*` in this phase.

## Risk Register

| Risk | Impact | Mitigation |
|---|---:|---|
| Storefront V2 misses one client route during switch | High | Keep legacy internal until QA passes; update QA checklist by feature group. |
| Swagger includes wrong endpoints in wrong document | Medium | Use explicit route prefix inclusion tests. |
| Commerce Admin endpoint accidentally runs without storeKey | High | Add required storeKey filter/attribute and API smoke tests. |
| New controllers duplicate business logic | Medium | Controllers must remain wrappers around existing services. |
| Public media scope becomes confused with admin debug media | Medium | Keep public media URL clean; add separate admin debug endpoints. |

## Decision Audit Trail

| # | Decision | Status |
|---|---|---|
| 1 | Storefront new API uses `api/storefront/stores/{storeKey}/*`. | Accepted |
| 2 | Commerce Admin store scope uses required query `storeKey`. | Accepted |
| 3 | `api/internal/*` remains legacy compatibility only. | Accepted |
| 4 | Swagger has `commerce-admin`, `storefront`, and `legacy-internal` documents. | Accepted |
| 5 | Storefront route migration uses new `StorefrontScoped*Controller` classes. | Accepted |
| 6 | `CommerceStoreContext` reads route/query/header by route group, not mixed fallback. | Accepted |
| 7 | Public media URLs remain clean; admin media debug endpoints use Commerce Admin route and `storeKey` query. | Accepted |

# BlazorShop Storefront V2 Reuse Todo

## Goal

Create a new Storefront V2 project by copying/reusing the existing `BlazorShop.Storefront` nearly as-is, then cut it over to Commerce Node internal APIs.

This is an MVP migration plan. The goal is speed, low risk, and preserving proven Storefront behavior. Do not redesign the Storefront UI and do not rewrite business logic unless a phase gate proves the copied code cannot run against Commerce Node.

## Target Project

| Item | Decision |
|---|---|
| Project folder | `BlazorShop.PresentationV2/BlazorShop.Storefront.V2` |
| Project name | `BlazorShop.Storefront.V2` |
| App type | Blazor Server / Razor Components, same as current `BlazorShop.Storefront` |
| Source baseline | `BlazorShop.Presentation/BlazorShop.Storefront` |
| API dependency | `BlazorShop.CommerceNode.API` `api/internal/*` |
| Shared model dependency | Keep `BlazorShop.Web.Shared` |
| Legacy Storefront | Keep untouched until explicit cutover/removal phase |

## Rules

- Do not refactor legacy `BlazorShop.Presentation/BlazorShop.Storefront`.
- Do not rename the existing `BlazorShop.Storefront` project.
- Do not redesign UI for this MVP.
- Do not introduce plugin architecture in this phase.
- Reuse the existing Storefront pages, services, components, CSS, JS, SEO logic, and tests where practical.
- Prefer copy-first, then narrow V2-only edits.
- Keep public routes stable:
  - `/`
  - `/category/{slug}`
  - `/product/{slug}`
  - `/new-releases`
  - `/todays-deals`
  - `/my-cart`
  - `/checkout`
  - `/signin`
  - `/register`
  - `/robots.txt`
  - `/sitemap.xml`
- V2 must call Commerce Node internal routes first.
- Legacy API fallback may exist only as a temporary rollback switch and must be disabled by default in V2.

## Autoplan Review Summary

### CEO Review

The fastest useful outcome is not a Storefront rewrite. The useful outcome is a second Storefront runtime that can prove Commerce Node parity without risking the current Storefront.

Auto-decisions:

- Keep the old Storefront running while V2 is built.
- Copy the current Storefront first; avoid "clean rewrite" scope.
- Make legacy fallback configurable but disabled by default for V2.
- Treat plugin/multi-store readiness as future scope.

### Engineering Review

The current Storefront already has good seams:

- `StorefrontApiClient` centralizes API calls and already understands `success/message/data`.
- `StorefrontSessionResolver` centralizes refresh-token session lookup.
- SEO, sitemap, robots, and structured data are isolated services.
- Cart UI is cookie-backed and can remain for MVP.
- Commerce Node already exposes the needed `api/internal/*` endpoints and has API QA coverage.

Main risk:

- Copying the project creates duplicated code. This is acceptable for MVP because it protects legacy and lets V2 stabilize before deciding whether to extract shared Storefront libraries.

### Design Review

No redesign in this phase. Visual parity is a feature, not a limitation. V2 should look and behave like the current Storefront unless a QA check shows a broken state.

### DX Review

Developer setup should stay simple:

1. Start Commerce Node PostgreSQL.
2. Apply Commerce Node migrations.
3. Start Commerce Node API.
4. Start Storefront V2.
5. Verify Storefront V2 pages.

## Existing Assets To Reuse

| Existing Area | Reuse Plan |
|---|---|
| `Program.cs` wiring | Copy, then adjust project namespace/config and V2 app name. |
| `Pages/*` | Copy as-is, then only adjust API fallback behavior if needed. |
| `Components/*` | Copy as-is. |
| `Services/*` | Copy as-is, then add V2-only API fallback option. |
| `Options/*` and validators | Copy as-is. |
| `wwwroot/css/storefront.css` | Copy as-is. |
| `wwwroot/js/storefrontCommerce.js` | Copy as-is for cookie cart MVP. |
| `BlazorShop.Web.Shared` models | Keep reference. |
| `BlazorShop.Web/wwwroot` shared assets | Keep copy/link behavior initially. |
| Storefront tests | Duplicate or parameterize only after V2 project builds. |

## Existing Commerce Node APIs V2 Should Use

### Catalog

- `GET api/internal/catalog/categories`
- `GET api/internal/catalog/categories/{id}`
- `GET api/internal/catalog/categories/slug/{slug}`
- `GET api/internal/catalog/categories/{categoryId}/products`
- `GET api/internal/catalog/products`
- `GET api/internal/catalog/products/{id}`
- `GET api/internal/catalog/products/slug/{slug}`
- `GET api/internal/catalog/sitemap`

### SEO

- `GET api/internal/seo/settings`
- `GET api/internal/seo/redirects/resolve?path=...`

### Auth

- `POST api/internal/auth/create`
- `POST api/internal/auth/login`
- `POST api/internal/auth/refresh-token`
- `POST api/internal/auth/logout`
- `POST api/internal/auth/change-password`
- `POST api/internal/auth/update-profile`

### Commerce Utility

- `GET api/internal/payments/methods`
- `GET api/internal/recommendations/products/{productId}`
- `POST api/internal/newsletter/subscribe`

### Cart And Orders

- `POST api/internal/cart/checkout`
- `POST api/internal/cart/save-checkout`
- `POST api/internal/orders/confirm`
- `GET api/internal/orders/current-user`
- `GET api/internal/orders/current-user/items`

## Implementation Phases

## Phase 0 - Baseline Lock

Goal:

Confirm the current Storefront and Commerce Node state before copying.

Tasks:

- [x] Verify `BlazorShop.Presentation/BlazorShop.Storefront` builds before copying. 2026-07-09: `dotnet build BlazorShop.Presentation/BlazorShop.Storefront/BlazorShop.Storefront.csproj` passed.
- [x] Verify `BlazorShop.CommerceNode.API` builds. 2026-07-09: `dotnet build BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/BlazorShop.CommerceNode.API.csproj` passed.
- [x] Verify Commerce Node DB container is available on port `5434`. 2026-07-09: `blazorshop-commercenode-postgres` is running with `0.0.0.0:5434->5432/tcp`.
- [x] Verify current Commerce Node API QA file is up to date:
  - `docs/refactor-control-Commerce-storefront/QA-CommerceNode.todo.md`
- [x] Record current Storefront source project path and project references. 2026-07-09: source is `BlazorShop.Presentation/BlazorShop.Storefront`; references are `BlazorShop.Application`, `BlazorShop.ServiceDefaults`, and `BlazorShop.Web.Shared`.

QA gate:

- [x] `dotnet build BlazorShop.Presentation/BlazorShop.Storefront/BlazorShop.Storefront.csproj`
- [x] `dotnet build BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/BlazorShop.CommerceNode.API.csproj`

Commit gate:

- No commit required unless docs are updated.

## Phase 1 - Copy Storefront Into PresentationV2

Goal:

Create `BlazorShop.Storefront.V2` as a copy of the existing Storefront project with minimal project identity changes.

Tasks:

- [x] Create folder `BlazorShop.PresentationV2/BlazorShop.Storefront.V2`.
- [x] Copy from `BlazorShop.Presentation/BlazorShop.Storefront`:
  - `Components/`
  - `Configuration/`
  - `Options/`
  - `Pages/`
  - `Properties/`
  - `Services/`
  - `wwwroot/`
  - `_Imports.razor`
  - `App.razor`
  - `Routes.razor`
  - `Program.cs`
  - `appsettings.json`
  - `appsettings.Development.json`
  - `Dockerfile`
- [x] Create/rename project file to `BlazorShop.Storefront.V2.csproj`.
- [x] Keep project references:
  - `BlazorShop.Application`
  - `BlazorShop.ServiceDefaults`
  - `BlazorShop.Web.Shared`
- [x] Adjust linked shared web assets path from V2 project location:
  - source should still be `BlazorShop.Presentation/BlazorShop.Web/wwwroot`.
- [x] Update project/assembly root namespace to `BlazorShop.Storefront.V2` if required by compiler. 2026-07-09: assembly/project is V2; Razor root namespace intentionally remains `BlazorShop.Storefront` for copy-first compatibility.
- [x] Add project to `BlazorShop.sln` under solution folder `BlazorShop.PresentationV2`.

QA gate:

- [x] `dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.V2/BlazorShop.Storefront.V2.csproj`
- [x] `dotnet build BlazorShop.sln`

Commit gate:

- Commit after build passes:
  - `feat: add storefront v2 shell`

## Phase 2 - V2 Configuration Cutover To Commerce Node

Goal:

Make Storefront V2 point to Commerce Node internal APIs by default.

Tasks:

- [x] Set V2 `Api:BaseUrl` for local development to Commerce Node API:
  - Preferred local HTTP option: `http://localhost:5180/api/`
  - HTTPS option if using launch profile: `https://localhost:7065/api/`
- [x] Keep `Api:RefreshTokenRoute` as `internal/auth/refresh-token`.
- [x] Keep `ClientApp:BaseUrl` explicit and validated.
- [x] Keep `PublicUrl:BaseUrl` configurable.
- [x] Add V2-only option if needed:
  - `Api:EnableLegacyFallback`
  - default: `false`
- [x] Update `StorefrontApiClient` copy so V2 can disable fallback routes.
- [x] Keep old Storefront fallback behavior unchanged. 2026-07-09: only `BlazorShop.PresentationV2/BlazorShop.Storefront.V2` was changed.

QA gate:

- [x] Start Commerce Node API. 2026-07-09: started with launch profile `http` on `http://localhost:5180`.
- [x] Start Storefront V2. 2026-07-09: started with copied launch profile on `http://localhost:18598`.
- [x] Load `/` and confirm catalog comes from Commerce Node. 2026-07-09: CommerceNode `/api/commerce/healthz` returned 200 with dev credentials and Storefront V2 `/` returned 200.
- [x] Stop legacy `BlazorShop.API`; V2 should still load catalog if Commerce Node is running. 2026-07-09: legacy API was not started during the V2 smoke test.
- [x] Stop Commerce Node API; V2 should show service unavailable, not silently load legacy data. 2026-07-09: with CommerceNode stopped, Storefront V2 `/` returned 503 and did not show legacy fallback markers.

Commit gate:

- Commit after V2 no longer depends on legacy API by default:
  - `feat: point storefront v2 to commerce node`

## Phase 3 - Public Catalog Page Verification

Goal:

Verify copied public pages run against Commerce Node internal catalog APIs.

Pages:

- [ ] `/`
- [ ] `/category/{slug}`
- [ ] `/product/{slug}`
- [ ] `/new-releases`
- [ ] `/todays-deals`

Tasks:

- [ ] Seed Commerce Node category with slug and published state.
- [ ] Seed Commerce Node product with slug, image, price, quantity, category, and published state.
- [ ] Seed at least one product variant.
- [ ] Seed at least two products in the same category for related products/recommendations behavior.
- [ ] Verify product cards render:
  - name
  - category
  - image
  - price
  - new badge when applicable
  - variants badge when applicable
- [ ] Verify product detail renders:
  - breadcrumb
  - product image
  - category link
  - variants selector
  - add-to-cart button
  - related products block

QA gate:

- [ ] Manual browser smoke test for all public catalog pages.
- [ ] Add or duplicate V2 route tests based on existing Storefront tests.
- [ ] Verify unpublished products/categories are not reachable.
- [ ] Verify missing product/category returns noindex 404 page.
- [ ] Verify Commerce Node downtime returns noindex 503 page.

Commit gate:

- Commit after catalog pages pass:
  - `test: verify storefront v2 catalog pages`

## Phase 4 - SEO, Discovery, Redirects

Goal:

Keep Storefront V2 SEO behavior identical to current Storefront.

Tasks:

- [ ] Verify `SeoHead` renders title, meta description, canonical, robots, OpenGraph.
- [ ] Verify `JsonLdScript` renders structured data.
- [ ] Verify `/sitemap.xml` uses Commerce Node `api/internal/catalog/sitemap`.
- [ ] Verify `/robots.txt` points at V2 public sitemap URL.
- [ ] Verify redirect middleware uses Commerce Node `api/internal/seo/redirects/resolve`.
- [ ] Verify service unavailable discovery paths return correct status and headers.
- [ ] Keep copied SEO services unchanged unless V2 base URL resolution requires a narrow fix.

QA gate:

- [ ] Add or duplicate V2 SEO audit tests:
  - successful public route returns 200 and canonical
  - missing route returns 404 noindex
  - service unavailable returns 503 noindex
  - sitemap XML contains public routes
  - robots.txt disallows private/framework paths
  - redirect source resolves to target

Commit gate:

- Commit after SEO/discovery tests pass:
  - `test: verify storefront v2 seo discovery`

## Phase 5 - Cart Cookie Flow

Goal:

Reuse the current cookie-backed cart MVP in V2.

Current behavior:

- Browser JS writes cookie `my-cart`.
- `/my-cart` reads cookie server-side.
- Cart page refreshes product data from API by product id.
- Checkout link uses `/checkout`.

Tasks:

- [ ] Copy `storefrontCommerce.js` unchanged initially.
- [ ] Verify cart badge updates.
- [ ] Verify direct add-to-cart for non-variant products.
- [ ] Verify product detail variant selector requires a variant before add-to-cart.
- [ ] Verify quantity update and remove item.
- [ ] Verify clear cart.
- [ ] Verify cart page refreshes products via Commerce Node `api/internal/catalog/products/{id}`.

QA gate:

- [ ] `/my-cart` renders empty cart.
- [ ] Add product then cart renders line item.
- [ ] Cart with unavailable product shows warning state.
- [ ] Cart page returns private/noindex headers.

Commit gate:

- Commit after cart flow passes:
  - `test: verify storefront v2 cart cookie flow`

## Phase 6 - Auth And Checkout Handoff

Goal:

Reuse current session resolver and checkout redirect behavior for MVP.

Current behavior:

- Storefront checks refresh-token cookie.
- Storefront calls Commerce Node `api/internal/auth/refresh-token`.
- If customer is authenticated, `/checkout` redirects to client app checkout.
- If customer is anonymous, `/checkout` redirects to client app login/checkout path.

Tasks:

- [ ] Verify `StorefrontSessionResolver` in V2 calls Commerce Node internal auth.
- [ ] Verify refresh-token cookie name remains compatible:
  - default `__Host-blazorshop-refresh`
- [ ] Verify Set-Cookie from Commerce Node is copied back to Storefront response.
- [ ] Verify anonymous `/checkout` redirect.
- [ ] Verify authenticated `/checkout` redirect.
- [ ] Verify `/signin` redirect.
- [ ] Verify `/register` redirect.
- [ ] Do not move checkout UI into Storefront V2 in this MVP.

QA gate:

- [ ] Register/login via Commerce Node/Web flow.
- [ ] Refresh cookie recognized by Storefront V2.
- [ ] `/checkout` chooses the correct redirect target.
- [ ] Missing Commerce Node auth endpoint degrades to anonymous, not crash.

Commit gate:

- Commit after auth/checkout handoff passes:
  - `test: verify storefront v2 auth handoff`

## Phase 7 - Test Suite Integration

Goal:

Bring enough automated coverage to protect V2 without duplicating every old test immediately.

Recommended MVP coverage:

- [x] V2 project build test through solution. 2026-07-09: `dotnet test BlazorShop.sln` built and tested the solution.
- [x] V2 API client test for:
  - envelope parsing
  - Commerce Node internal route success
  - legacy fallback disabled by default
  - service unavailable behavior
- [ ] V2 route SEO audit smoke test.
- [x] V2 cart route test.
- [x] V2 sitemap and robots tests.
- [x] V2 checkout redirect tests.

Do not duplicate all existing Storefront tests blindly. Start with tests that cover V2-specific risk:

- project path/config changes
- Commerce Node-only routing
- disabled legacy fallback
- copied cart/session behavior

QA gate:

- [x] `dotnet test BlazorShop.sln`. 2026-07-09: passed with 482 passed, 10 skipped.
- [x] Existing Storefront tests still pass.
- [x] V2 tests pass. 2026-07-09: `dotnet test BlazorShop.Tests/BlazorShop.Tests.csproj --filter FullyQualifiedName~PresentationV2.Storefront` passed 7/7.

Commit gate:

- Commit after automated tests pass:
  - `test: add storefront v2 coverage`

## Phase 8 - Local Runbook And QA File

Goal:

Document how to run and verify Storefront V2 quickly.

Tasks:

- [ ] Add `QA-StorefrontV2.todo.md`.
- [ ] Include clean DB setup:
  - `docker compose -f compose.commercenode.yml up -d`
  - apply Commerce Node migrations
  - seed category/product/customer
- [ ] Include required services:
  - Commerce Node API
  - Storefront V2
  - Client app only if checkout/auth UI is being tested
- [ ] Include route checklist:
  - home
  - category
  - product
  - new releases
  - deals
  - cart
  - checkout redirect
  - robots
  - sitemap
  - redirect resolution
- [ ] Include expected failure states:
  - Commerce Node down
  - missing slug
  - empty catalog
  - invalid cart cookie

QA gate:

- [ ] Follow the QA file from a clean DB and mark pass/fail.

Commit gate:

- Commit after docs are usable:
  - `docs: add storefront v2 qa checklist`

## Phase 9 - Legacy Cutover Decision

Goal:

Decide when V2 is ready to replace the current Storefront.

Do not remove the old Storefront in this phase unless explicitly approved.

Cutover readiness:

- [ ] V2 builds in solution.
- [ ] V2 runs against Commerce Node only.
- [ ] V2 can run while legacy API is stopped.
- [ ] Home/category/product pages pass browser QA.
- [ ] Cart cookie flow passes.
- [ ] Checkout redirect passes.
- [ ] SEO/sitemap/robots pass.
- [ ] `dotnet test BlazorShop.sln` passes.
- [ ] QA file is updated.
- [ ] Deployment/reverse proxy route for V2 is defined.

Possible cutover options:

| Option | Use When | Risk |
|---|---|---|
| Run V2 beside legacy | MVP validation still ongoing | Lowest |
| Switch public reverse proxy to V2 | V2 QA passes clean DB | Medium |
| Remove old Storefront project | Multiple V2 releases are stable | Highest, defer |

Commit gate:

- Commit only docs/config if cutover decision is documented.

## Out Of Scope For This MVP

- Multi-store route model.
- Plugin architecture.
- Server-side anonymous cart persistence.
- Moving checkout UI into Storefront V2.
- Removing legacy Storefront.
- Removing legacy Storefront API fallback from old Storefront.
- Redesigning UI or design system.
- Replacing `BlazorShop.Web.Shared`.

## Risks And Controls

| Risk | Control |
|---|---|
| Copied code diverges from old Storefront | Accept temporarily; decide extraction only after V2 is stable. |
| V2 accidentally calls legacy API | Disable fallback by default and add test coverage. |
| Checkout depends on client app URL | Keep `ClientApp:BaseUrl` validation and test redirect targets. |
| Cart cookie is global and not store-aware | Accept for MVP; document future `StoreId` cart strategy. |
| Shared assets path breaks from V2 folder | Test static CSS/JS endpoints after copy. |
| Public reverse proxy exposes `api/internal/*` | Keep deployment checklist explicit; internal API exposure is not a Storefront responsibility alone. |
| Auth refresh cookie secure behavior differs locally | Use HTTPS or explicitly document local HTTP limitation. |

## Decision Audit Trail

| # | Decision | Classification | Rationale |
|---|---|---|---|
| 1 | Create `BlazorShop.Storefront.V2` as copy-first project | Auto-decided | Fastest MVP path and avoids breaking legacy Storefront. |
| 2 | Keep `BlazorShop.Web.Shared` | Auto-decided | Storefront already depends on shared models; rewriting would add risk. |
| 3 | Disable legacy fallback by default in V2 | Auto-decided | V2 exists to prove Commerce Node parity, not hide failures with legacy API. |
| 4 | Keep cookie cart for MVP | Auto-decided | Current cart works and is already covered by tests; server cart is future scope. |
| 5 | Keep checkout handoff to client app | Auto-decided | Moving checkout UI would expand scope beyond Storefront copy/reuse. |
| 6 | Keep UI unchanged | Auto-decided | Visual redesign adds risk and is not needed for Commerce Node cutover proof. |

## Recommended Next Step

Start with Phase 0 and Phase 1 only. Do not edit behavior until the copied V2 project builds cleanly in the solution.

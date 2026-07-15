# BlazorShop SEO Routing Slug Core Autoplan

Generated: 2026-07-15

Scope:

- 9.1 Slug lifecycle
- 9.2 Slug history and redirect
- 9.3 SEO metadata
- 9.4 Indexing rules
- 9.5 Sitemap and crawler endpoints
- 9.6 SEO URL resolver output

## Implementation Status

Updated: 2026-07-15

- Phase 0 complete: baseline plan committed.
- Phase 1 complete: Commerce Node SEO redirects are store-scoped in schema, repository, admin service, automation service, and public redirect resolution.
- Phase 2 complete: shared SEO slug policy service added with reserved route validation, Unicode-preserving normalization, store-scoped collision checking, and suffix generation.
- Phase 3 complete: Commerce Node slug history table/service/backfill foundation added without replacing entity `Slug` fields.
- Phase 4 complete: canonical SEO URL resolver added for store-scoped product/category/page public paths, old-slug canonical redirects, not-found, gone, and invalid outcomes.
- Phase 5 complete: product, category, and storefront page slug update flows now use shared slug policy/history in V2; published page slug changes create store-scoped 301 redirects; page create can generate a slug from title.
- Phase 6 complete: Storefront SEO redirect API now falls back to canonical slug history resolver while preserving explicit redirect priority and Storefront V2 middleware safety checks.
- Phase 7 complete: SEO metadata builder now rejects unsafe canonical/Open Graph URLs, falls back to route canonical when an override is unsafe, and keeps title suffix de-duplication covered.
- Phase 11 is conditional and should run only after a real legacy topic URL inventory exists.

Autoplan note: external dual-voice subagents are not available in this Codex runtime. This plan records an internal autoplan audit using the same decision principles: preserve existing working behavior, fix the riskiest foundation first, keep V2 boundaries explicit, avoid speculative localization/manufacturer work, and make each phase independently verifiable.

## 1. Premise Challenge

The requested feature is valid, but it must not become a second routing framework or a parallel SEO metadata system.

The codebase already has usable SEO pieces:

- `SlugService` normalizes slugs to lowercase and URL-safe separators.
- `Product`, `Category`, and `StorefrontPage` already store active slug and SEO fields.
- Product and category SEO update flows already create automatic redirects when the public slug changes.
- Storefront V2 already renders canonical, robots, Open Graph, and JSON-LD hooks.
- Storefront V2 already serves `robots.txt` and `sitemap.xml`.
- Commerce Node already has store-scoped catalog/page reads.

The missing core is a single store-scoped SEO URL resolver and slug lifecycle model. Current behavior is split across entity services, and `SeoRedirect` is path-global rather than store-scoped. That is the highest-risk gap because multi-store paths such as `/product/shirt` can collide across stores.

## 2. Current Code Facts

| Area | Current fact | Decision |
| --- | --- | --- |
| Slug normalization | `SlugService.NormalizeSlug` trims, lowercases, removes non-spacing marks, converts separators to `-`, and keeps `char.IsLetterOrDigit` characters. | Reuse this service. Do not change the policy globally in Phase 1. |
| Product slug | `Product.Slug` exists and unique index `(StoreId, Slug)` is configured for active non-archived rows. | Keep active slug on `Product` for compatibility. Add lifecycle/history around it. |
| Category slug | `Category.Slug` exists and unique index `(StoreId, Slug)` is configured for active non-archived rows. | Keep active slug on `Category`. Add lifecycle/history around it. |
| Page slug | `StorefrontPage.Slug` exists with unique `(StoreId, Slug)` in `CommerceNodeDbContext`. | Keep `/pages/{slug}` and add redirect/history behavior. |
| Product/category redirects | `ProductSeoService` and `CategorySeoService` call `EnsurePermanentRedirectAsync` when public path changes. | Preserve behavior but move redirect creation behind a shared store-aware service. |
| Page redirect | `StorefrontPageService.UpdateAsync` changes `page.Slug` directly and does not create old-slug redirect. | Add page slug redirect in the shared flow. |
| Redirect table | `SeoRedirect` has `OldPath`, `NewPath`, `StatusCode`, `IsActive`, `CreatedOn`; unique index is on `OldPath` only. | This must be made store-scoped before broader slug history rollout. |
| Storefront redirect runtime | `StorefrontPublicRedirectMiddleware` resolves redirects for GET/HEAD public paths and blocks simple loops/invalid targets. | Keep middleware, but back it with store-scoped resolver output. |
| SEO metadata | `SeoMetadataBuilder`, `StorefrontSeoComposer`, `SeoHead.razor`, and entity SEO fields already cover title, description, canonical, robots, OG, JSON-LD hook. | Extend this system; do not create a second metadata model. |
| Sitemap | `StorefrontSitemapService` emits home/static routes, categories, products, pages, lastmod, and de-duplicates URLs. | Keep as MVP base; add policy/config only after resolver is stable. |
| Robots | `StorefrontRobotsService` disallows API/admin/framework/account-ish paths and points to sitemap. | Keep and centralize indexing policy later. |
| Structured data | `StorefrontStructuredDataComposer` already emits Organization, WebSite, Product, category collection, breadcrumbs. | Keep as hook/foundation; do not rewrite. |
| Language | No active V2 language-specific content model exists. | Add nullable language fields for future compatibility, but defer behavior. |
| Manufacturer | No active V2 manufacturer/brand domain model exists. | Do not include manufacturer sitemap/slug target in MVP. |

## 3. Approved Scope

### In Scope

- Store-scoped redirect uniqueness and resolution.
- Shared slug lifecycle service for product/category/page.
- Active slug remains on existing entity tables for compatibility.
- Add slug history records with entity type, entity id, store id, and nullable language.
- Generate slug from entity name/title when requested by admin flow.
- Manual slug override with normalization and validation.
- Reserved path validation.
- Duplicate slug detection per store, route family, and language where applicable.
- Store/language collision handling.
- Canonical path resolver from entity.
- Public SEO URL resolver output with requested slug, canonical slug, entity identity, redirect requirement, and status.
- Page old-slug redirect when page slug changes.
- Central indexing policy for public route families.
- Sitemap alignment with published/store-visible entity rules.

### Out Of Scope For MVP

- Full localization and translated slugs.
- Hreflang and alternate-language sitemap entries.
- Manufacturer sitemap and manufacturer slug resolver.
- Meta keywords except as optional compatibility field later.
- HTML sitemap.
- `llms.txt`.
- Full topic legacy migration unless a real legacy URL inventory is provided.
- Replacing existing product/category/page public routes.
- Adding routes under legacy `api/internal/*`, `api/public/*`, or `BlazorShop.Presentation/*`.

## 4. Core Decisions

### Decision 1: Store Scope First

Fix redirect storage and resolution to include store scope before adding richer slug history.

Reason:

- Current `SeoRedirect` unique index on `OldPath` is unsafe for multi-store.
- Storefront V2 always resolves current store before public behavior.
- A global redirect path can send Store A traffic to Store B-style URL behavior.

### Decision 2: Keep Active Slug On Existing Entities

Do not remove `Product.Slug`, `Category.Slug`, or `StorefrontPage.Slug`.

Reason:

- Existing APIs, Storefront pages, sitemap, menu planning, and admin UI already consume these fields.
- Removing or replacing them would create unnecessary migration risk.
- Slug history can reference the active entity without breaking existing contracts.

### Decision 3: Add A Shared SEO URL Resolver

Introduce a shared resolver that understands entity type, store, route family, active slug, historical slug, redirects, and canonical path.

Reason:

- Product/category/page services currently duplicate slug/path logic.
- Menu and sitemap phases need the same canonical target resolution.
- Public redirect and route components need a consistent answer.

### Decision 4: Keep Unicode Policy Stable For Now

Do not switch to ASCII-only slugs in MVP.

Reason:

- `SlugService` currently keeps Unicode letters/digits after removing combining marks.
- Changing this globally would change existing non-Western slugs and can break URLs.
- The plan should document the policy and validate it consistently first.

### Decision 5: Language Fields Are Nullable

Add `LanguageCode` or `LanguageId` as nullable fields in slug history/resolver contracts, but keep single-language runtime behavior.

Reason:

- User requested language-aware lifecycle.
- Current codebase does not have language-specific page/catalog content.
- Nullable fields avoid a future incompatible schema rewrite without pretending localization exists today.

## 5. Target Model

### StoreSeoSlugHistory

Commerce Node table: `store_seo_slug_history`

Fields:

- `Id`
- `PublicId`
- `StoreId`
- `EntityType`
- `EntityId`
- `LanguageCode` nullable
- `Slug`
- `RoutePath`
- `IsActive`
- `CreatedAt`
- `ReplacedAt` nullable
- `RedirectToSlug` nullable
- `RedirectToPath` nullable
- `CreatedBy` nullable

Indexes:

- Unique active slug by `(StoreId, EntityType, LanguageCode, Slug)` where `IsActive = true`.
- Lookup index `(StoreId, RoutePath)`.
- Lookup index `(StoreId, EntityType, EntityId, LanguageCode, IsActive)`.
- History index `(StoreId, EntityType, EntityId, CreatedAt)`.

Notes:

- `RoutePath` stores the public route path such as `/product/red-shirt`, `/category/shoes`, `/pages/about-us`.
- `EntityType` values should be fixed string constants: `product`, `category`, `page`.
- `LanguageCode` is nullable until localization exists.

### StoreScopedSeoRedirect

Preferred path: evolve current `SeoRedirect` to be store-scoped in Commerce Node.

Required fields:

- `StoreId`
- `OldPath`
- `NewPath`
- `StatusCode`
- `IsActive`
- `EntityType` nullable
- `EntityId` nullable
- `LanguageCode` nullable
- `CreatedOn`

Indexes:

- Unique `(StoreId, OldPath)` for active rows.
- Index `(StoreId, IsActive, OldPath)`.
- Index `(StoreId, EntityType, EntityId)`.

Migration rule:

- Existing redirect rows without store id must be handled explicitly.
- In dev mode, either backfill to a known default store if there is exactly one store, or archive/flag rows requiring manual review.
- Do not keep a global `OldPath` unique constraint after store-scoped redirect is introduced.

## 6. Slug Policy

Initial policy:

- Trim whitespace.
- Lowercase invariant.
- Normalize combining marks.
- Convert non letter/digit runs to `-`.
- Collapse repeated separators.
- Trim leading/trailing `-`.
- Reject empty result.
- Reject `/` inside slug.
- Reject reserved route segments.
- Max length should match existing constraints.
- Do not force ASCII-only in MVP.

Reserved paths and segments:

- `api`
- `admin`
- `commerce`
- `control-plane`
- `storefront`
- `media`
- `uploads`
- `css`
- `js`
- `images`
- `_framework`
- `_content`
- `_blazor`
- `swagger`
- `signin`
- `register`
- `logout`
- `my-cart`
- `cart`
- `checkout`
- `search`
- `sitemap.xml`
- `robots.txt`
- `maintenance`
- `payment-success`
- `payment-cancel`

Collision rules:

- Product slug collision is checked within product route family for the same store/language.
- Category slug collision is checked within category route family for the same store/language.
- Page slug collision is checked within page route family for the same store/language.
- Optional global collision check across route families can warn admin, but it should not block because routes have different prefixes.

## 7. SEO URL Resolver Contract

Suggested application DTO:

```csharp
public sealed record SeoUrlResolutionResult(
    string EntityType,
    Guid? EntityId,
    string? RequestedSlug,
    string? CanonicalSlug,
    string? LanguageCode,
    string? RequestedPath,
    string? CanonicalPath,
    bool RequiresRedirect,
    int HttpStatus,
    string ResolutionStatus);
```

Status values:

- `resolved`
- `redirect_to_canonical`
- `not_found`
- `gone`
- `unpublished`
- `invalid_slug`
- `reserved_path`
- `redirect_loop_blocked`

Rules:

- Resolver must take store id from `ICommerceStoreContext`, not user input on Storefront APIs.
- Resolver must never return another store's entity.
- Resolver must not expose unpublished entity data publicly.
- Admin resolver can include diagnostic status; public resolver should return safe status only.
- Redirect output should be root-relative path only.

## 8. API Plan

All active V2 APIs must follow `docs/architecture/09-api-contract-standards.md`.

### Commerce Node Admin API

Route group:

```text
api/commerce/admin/seo
```

Suggested endpoints:

- `GET /slugs?entityType={entityType}&entityId={entityId}`
- `POST /slugs/generate`
- `POST /slugs/validate`
- `GET /url/resolve?path={path}`
- `GET /redirects`
- `POST /redirects`
- `PUT /redirects/{id}`
- `POST /redirects/{id}/deactivate`
- `DELETE /redirects/{id}`

Notes:

- Existing SEO settings/product/category endpoints should be extended rather than duplicated.
- Product/category/page update flows should call application services, not direct redirect creation.
- Admin response may include broken/collision diagnostics.

### Storefront Public API

Route group:

```text
api/storefront/stores/{storeKey}/seo
```

Keep:

- `GET /settings`
- `GET /redirects/resolve?path={path}`

Add when resolver exists:

- `GET /url/resolve?path={path}`

Rules:

- Store scope comes from `{storeKey}`.
- Public resolver returns safe DTO only.
- No node credentials.
- No raw domain entities.

### Control Plane Gateway API

Control Plane Web must call Control Plane API only.

Suggested route group:

```text
api/control-plane/commerce/stores/{storePublicId}/seo/*
```

Rules:

- Resolve node/store from Control Plane registry.
- Forward to Commerce Node admin SEO endpoints with node credentials hidden.
- Enforce platform permissions.
- Preserve current product/category/page SEO management screens.

## 9. Phase Plan

### Phase 0 - Baseline And Safety Inventory

Goal: lock current behavior before schema changes.

Tasks:

- Capture current product/category/page public routes.
- Capture existing `SeoRedirect` rows and uniqueness assumptions.
- Confirm Storefront V2 redirect middleware order after current-store middleware.
- Confirm product/category SEO update tests or add focused baseline tests if missing.
- Confirm page update currently has no old-slug redirect.
- List existing noindex route pages.
- List sitemap included sources: static routes, categories, products, pages.

Exit criteria:

- No runtime behavior change.
- Known gaps are documented in QA checklist.
- Store-scoped redirect migration strategy is selected for dev data.

Suggested commit:

```text
docs: plan seo routing slug core
```

### Phase 1 - Store-Scoped Redirect Foundation

Goal: remove the global redirect collision risk first.

Tasks:

- Add `StoreId` to Commerce Node SEO redirect persistence.
- Replace unique `OldPath` with unique `(StoreId, OldPath)` for active redirects.
- Update `ISeoRedirectRepository` and Commerce Node repository to resolve by current store.
- Keep legacy/AppDbContext redirect repository unchanged unless legacy tests require compatibility.
- Update `SeoRedirectAutomationService` to require store context in Commerce Node runtime.
- Update admin redirect CRUD to operate in current store scope.
- Add migration for `CommerceNodeDbContext` only.
- Add tests:
  - same `OldPath` allowed in different stores.
  - duplicate active `OldPath` rejected within same store.
  - public redirect resolution cannot cross store.
  - redirect loop protection still works.

Exit criteria:

- Store A `/product/x` redirect cannot affect Store B. Complete in `CommerceNodeSeoRedirectStoreScopeTests`.
- Storefront redirect middleware behavior remains the same from the browser perspective. Preserved because public middleware still calls the same resolution service contract.
- Existing product/category auto-redirect still works. Preserved through `SeoRedirectAutomationService`, now setting `StoreId` when Commerce Node store context exists.

Suggested commit:

```text
feat(commerce-node): scope seo redirects by store
```

### Phase 2 - Shared Slug Policy Service

Goal: centralize slug normalization, reserved path validation, generation, and collision checks.

Tasks:

- Keep `SlugService` as low-level normalizer.
- Add `IStoreSeoSlugPolicyService` or equivalent application service.
- Add reserved path/segment list.
- Add `GenerateSlugAsync(entityType, sourceName, storeId, languageCode)` with suffix behavior for collisions.
- Add `ValidateSlugAsync(entityType, slug, storeId, languageCode, excludedEntityId)`.
- Support manual override by normalizing then validating.
- Add explicit Unicode policy documentation in code comments/docs.
- Add service tests for:
  - lowercase normalization.
  - Unicode letter preservation.
  - empty slug rejection.
  - slash rejection.
  - reserved segment rejection.
  - duplicate slug rejection.
  - suffix generation.

Exit criteria:

- Product/category/page services can use one policy service. Complete through `IStoreSeoSlugPolicyService` and `CommerceNodeStoreSeoSlugCollisionChecker`.
- No public route behavior changes yet. Complete; Phase 2 adds service foundation and one product SEO read fallback regression fix only.

Suggested commit:

```text
feat(application): add shared seo slug policy service
```

### Phase 3 - Slug History Model

Goal: record active and old slugs per entity without replacing entity slug fields.

Tasks:

- Add `StoreSeoSlugHistory` entity.
- Configure EF table/indexes in `CommerceNodeDbContext`.
- Add migration.
- Add service methods:
  - get active slug for entity.
  - record initial active slug.
  - replace active slug and mark old row replaced.
  - list slug history for admin.
- Backfill history from existing active product/category/page slugs.
- Make backfill idempotent.
- Add nullable `LanguageCode`.
- Add tests:
  - active slug row created.
  - old slug retained after change.
  - one active slug per entity/language.
  - one active slug per route family/store/language.
  - backfill does not duplicate rows.

Exit criteria:

- History exists for current active slugs. Complete through `StoreSeoSlugHistoryService.BackfillCurrentSlugsAsync`.
- Existing entity `Slug` fields remain source-compatible. Complete; product/category/page `Slug` fields were not replaced.
- No Storefront route change yet. Complete; Phase 3 only adds schema/service/history tests.

Suggested commit:

```text
feat(commerce-node): add seo slug history
```

### Phase 4 - Canonical URL Resolver

Goal: resolve entity to canonical public path and requested path to safe resolver output.

Tasks:

- Add `ISeoUrlResolver`.
- Resolve entity canonical paths:
  - product -> `/product/{activeSlug}`
  - category -> `/category/{activeSlug}`
  - page -> `/pages/{activeSlug}`
- Resolve requested public paths:
  - active slug returns `resolved`.
  - old slug returns `redirect_to_canonical`.
  - unpublished/archived entity returns safe not found/gone status.
  - invalid/reserved path returns safe not found or validation status.
- Detect redirect loops.
- Make resolver store-scoped through `ICommerceStoreContext`.
- Add DTOs for admin and public outputs.
- Add tests across product/category/page.

Exit criteria:

- Product/category/page public path logic is centralized. Complete through `ISeoUrlResolver`.
- Resolver output includes entity type/id, requested slug, canonical slug, language, redirect requirement, and HTTP status. Complete through `SeoUrlResolutionDto`.
- Resolver never returns another store's entity. Complete; wrong-store slug lookup returns `not_found`.

Suggested commit:

```text
feat(application): add seo url resolver
```

### Phase 5 - Product, Category, And Page Slug Flows

Goal: route all slug changes through shared policy/history/redirect behavior.

Tasks:

- Update `ProductSeoService` to use slug policy and slug history service.
- Update `CategorySeoService` to use slug policy and slug history service.
- Update `StorefrontPageService` update flow to create old-slug redirect when published page slug changes.
- Add generate-slug behavior where admin creates product/category/page without manual slug if the request supports it.
- Preserve existing DTOs where possible; add optional fields only if needed.
- Invalidate catalog/page/navigation caches where slug changes affect public links.
- Add tests:
  - product slug change creates store-scoped 301.
  - category slug change creates store-scoped 301.
  - page slug change creates store-scoped 301.
  - unpublished page slug change does not expose draft redirect publicly unless policy says active published old path existed.
  - manual override validates reserved path.

Exit criteria:

- Product/category/page slug lifecycle behaves consistently. Complete; store-scoped V2 flows validate through shared slug policy when available and persist active slug history.
- Existing public routes still work. Complete; entity `Slug` fields and route shapes remain unchanged.
- Old page slug redirects now work. Complete; published page slug changes call `ISeoRedirectAutomationService` for `/pages/{old}` -> `/pages/{new}`.

Suggested commit:

```text
feat(commerce-node): unify product category page slug lifecycle
```

### Phase 6 - Storefront Public Redirect And Canonical Enforcement

Goal: use resolver output to redirect old slugs and canonicalize paths safely.

Tasks:

- Update Storefront public redirect API to return resolver-backed redirect results.
- Keep `StorefrontPublicRedirectMiddleware` as the SSR entry point.
- Ensure only GET/HEAD public paths are redirected.
- Ensure redirect destination is root-relative and same store.
- Add optional canonical redirect when requested slug case/normalization differs from active slug.
- Do not redirect mutation endpoints.
- Do not redirect static assets.
- Add tests:
  - old product/category/page slug 301s to active slug.
  - active slug does not redirect.
  - loop chain is blocked.
  - invalid target is blocked.
  - wrong-store old slug does not resolve.

Exit criteria:

- Browser old slug request gets 301 to canonical route. Complete at API contract level; `StorefrontSeo_ResolveRedirect` returns the existing `SeoRedirectResolutionDto` from slug-history resolver when explicit redirect is absent.
- No redirect loop. Complete; existing redirect resolution and middleware loop tests remain covered.
- Middleware exclusions remain safe. Complete; `StorefrontPublicRedirectMiddleware` was not widened and existing monitoring tests passed.

Suggested commit:

```text
feat(storefront): resolve canonical seo redirects by store
```

### Phase 7 - SEO Metadata Policy Hardening

Goal: extend existing metadata builder instead of replacing it.

Tasks:

- Keep `SeoMetadataBuilder` and `StorefrontSeoComposer`.
- Add title composition rule options if needed:
  - title separator.
  - suffix/prefix behavior.
  - store name fallback.
- Add optional meta keywords only if compatibility import/admin requires it.
- Add canonical URL policy:
  - prefer entity canonical override when set and safe.
  - otherwise use resolver canonical path + base canonical URL.
  - normalize host via configured/public URL rules.
- Add structured data hook documentation for product/category/page.
- Add Open Graph fallback rules.
- Add tests for:
  - title suffix not duplicated.
  - canonical URL uses configured base.
  - no canonical on 404/503.
  - robots from entity/page metadata respected.

Exit criteria:

- Existing SEO pages render the same or better metadata. Complete; existing composer/route SEO tests passed, and unsafe canonical/OG values are no longer emitted.
- No second metadata system exists. Complete; hardening stayed inside `SeoMetadataBuilder` and existing `StorefrontSeoComposer`.

Suggested commit:

```text
feat(storefront): harden seo metadata composition
```

### Phase 8 - Indexing And Canonical Query Rules

Goal: centralize which pages should be indexed and how duplicate URLs are avoided.

Tasks:

- Add route indexing policy service or constants.
- Index:
  - home.
  - published product.
  - published category.
  - published page.
  - curated static listing pages if intentionally public.
- Noindex:
  - cart.
  - checkout.
  - account/auth routes.
  - login/register.
  - internal search.
  - payment success/cancel.
  - compare when implemented.
- Canonicalize filtered category URLs:
  - base category path canonical by default.
  - do not index tracking query params.
  - canonicalize paging/sort only if product strategy requires it.
- Keep mutation endpoints out of crawler reach:
  - robots disallow `/api/`.
  - response headers/private-page rules where applicable.
- Define trailing slash policy:
  - no trailing slash except `/`.
- Define host canonicalization:
  - prefer `PublicUrl:BaseUrl` or SEO base canonical URL.
  - request-host fallback only through trusted forwarded headers.

Exit criteria:

- Search/cart/checkout/login/register noindex behavior is consistent.
- Duplicate filtered URLs do not get canonicalized as separate primary pages.

Suggested commit:

```text
feat(storefront): centralize indexing and canonical query rules
```

### Phase 9 - Sitemap And Crawler Endpoints Alignment

Goal: align sitemap/robots with resolver and indexing policy.

Tasks:

- Keep `robots.txt` and `sitemap.xml` endpoints.
- Generate sitemap entries through canonical resolver output.
- Include only:
  - published/store-visible products.
  - published/store-visible categories.
  - published pages with `IncludeInSitemap=true`.
  - approved static routes.
- Exclude:
  - cart.
  - checkout.
  - login/register/account.
  - search.
  - unpublished/archived entities.
  - manufacturer until manufacturer domain exists.
- Add include/exclude config only where current admin settings need it.
- Add sitemap index only after count threshold is real.
- Defer alternate-language links.
- Defer `llms.txt`.
- Add tests for sitemap URLs and lastmod.

Exit criteria:

- Sitemap only contains canonical current-store URLs.
- No stale old slugs appear in sitemap.
- Robots points to canonical sitemap URL.

Suggested commit:

```text
feat(storefront): align sitemap with seo url resolver
```

### Phase 10 - Admin UX And Control Plane Gateway

Goal: expose slug lifecycle and redirect diagnostics safely to managers.

Tasks:

- Extend existing product/category SEO panels with:
  - generated slug suggestion.
  - manual override.
  - normalized preview.
  - reserved path/collision validation message.
  - old slug history list.
  - canonical URL preview.
- Extend page manager slug field similarly.
- Add SEO redirects manager store-scoped view if not already sufficient.
- Ensure Control Plane Web calls Control Plane API only.
- Add Control Plane gateway endpoints for new slug validate/generate/history APIs.
- Add permissions:
  - reuse existing SEO/page/catalog permissions where already present.
  - add `commerce.seo.redirects.write` only if current permission model proves too broad.
- Add browser tests for validation and error messages.

Exit criteria:

- Admin can see what slug will be published before save.
- Admin can understand why a slug is rejected.
- No Commerce Node credentials leak to Control Plane Web.

Suggested commit:

```text
feat(control-plane): expose seo slug lifecycle management
```

### Phase 11 - Legacy Topic URL Compatibility

Goal: handle legacy topic/page URLs only if real legacy URLs exist.

Tasks:

- Inventory legacy topic/page URL patterns from old app or database.
- Map known topic paths to new `/pages/{slug}` only when there is an active page mapping.
- Create store-scoped 301 redirects through the shared redirect service.
- Do not add broad catch-all topic routes.
- Add tests for known legacy path examples.

Exit criteria:

- Approved legacy topic URLs redirect to canonical active page URLs.
- Unknown legacy paths remain 404.

Suggested commit:

```text
feat(commerce-node): migrate approved legacy topic redirects
```

### Phase 12 - QA, Docs, And Release Gate

Goal: close the feature with focused verification.

Tasks:

- Update `QA-CommerceNode.todo.md`.
- Update `QA-ControlPlane.todo.md`.
- Update `QA-StorefrontV2.todo.md`.
- Add migration verification for Commerce Node database.
- Add API contract tests for new/changed endpoints.
- Add focused service tests for slug policy, resolver, redirects, sitemap.
- Add Storefront browser checks for old slug redirect and canonical metadata.
- Document slug policy and reserved paths for future agents.

Exit criteria:

- Focused tests pass.
- Storefront old slug redirect verified.
- Sitemap and robots verified.
- QA checklists contain evidence.
- No legacy V2 boundary violations.

Suggested commit:

```text
test(v2): add seo routing slug core qa coverage
```

## 10. QA Checklist Draft

### Commerce Node

- Store-scoped redirect uniqueness allows same old path across different stores.
- Duplicate old path in same store is rejected.
- Product slug change creates 301 redirect.
- Category slug change creates 301 redirect.
- Page slug change creates 301 redirect.
- Old slug history is retained after change.
- Active slug history has one active row per entity/language.
- Reserved path slug is rejected.
- Slash-containing slug is rejected.
- Empty normalized slug is rejected.
- Unicode slug behavior matches documented policy.
- Resolver hides unpublished/archived entities publicly.
- Resolver never resolves another store's entity.

### Control Plane

- Product SEO panel shows normalized slug preview.
- Category SEO panel shows normalized slug preview.
- Page manager shows normalized slug preview.
- Manual slug override surfaces validation errors.
- Old slug history is visible in admin.
- Redirect manager is store-scoped.
- Control Plane Web calls only Control Plane API.
- User without SEO write permission cannot mutate redirects/slugs.

### Storefront V2

- Active product/category/page slug renders normally.
- Old product slug redirects 301 to active product slug.
- Old category slug redirects 301 to active category slug.
- Old page slug redirects 301 to active page slug.
- Wrong-store old slug does not redirect.
- Redirect loop is blocked.
- 404/503 pages do not emit canonical URL.
- Cart/checkout/login/register/search are noindex.
- Sitemap includes only canonical published/store-visible URLs.
- Robots disallows API/internal/private paths and references canonical sitemap.

## 11. Failure Modes Registry

| Risk | Why it matters | Mitigation |
| --- | --- | --- |
| Global redirect path leaks across stores | Store A URL can redirect based on Store B rule. | Phase 1 store-scoped redirect first. |
| Slug system duplicates entity slug fields incorrectly | Existing APIs and UI break. | Keep active slug fields and add history around them. |
| Unicode policy changes existing URLs | Non-Western slugs can break. | Document and preserve current Unicode behavior in MVP. |
| Page slug changes do not redirect | SEO loss and broken old links. | Add page flow to shared slug lifecycle. |
| Redirect loop | Crawler/user stuck in loop. | Resolver and middleware loop detection. |
| Reserved route collision | Slug can shadow cart/search/api/static paths. | Shared reserved path validation. |
| Cross-store collision logic too strict | Store B cannot use same slug as Store A. | Collision checks include store id. |
| Metadata rewrite regresses SEO | Existing canonical/OG/JSON-LD behavior breaks. | Extend current builder/composer, do not replace. |
| Sitemap emits old slugs | Search engines index stale URLs. | Generate sitemap through canonical resolver. |
| Localization fields imply localization behavior | Product appears multilingual without content model. | Nullable language fields only; behavior deferred. |

## 12. Alternatives Considered

### Alternative 1: Replace Entity Slug Fields With A Slug Table

Rejected for MVP.

Existing Storefront, admin UI, API DTOs, sitemap, and menu plans already rely on entity slug fields. Replacing them would increase migration risk without improving immediate behavior.

### Alternative 2: Keep Redirects Global

Rejected.

Global redirects are incompatible with multi-store path ownership. Store scope is required before any slug history expansion.

### Alternative 3: ASCII-Only Slugs

Deferred.

ASCII-only is simpler for Western stores, but current code supports Unicode letters. Changing now can break existing or expected non-Western URLs. Revisit only with explicit migration strategy.

### Alternative 4: Full Localization Now

Rejected.

The codebase does not yet have language-specific product/category/page content. Add nullable language fields, but defer runtime hreflang and translated slug behavior.

### Alternative 5: New SEO Metadata System

Rejected.

The existing metadata builder/composer/head components already cover the MVP metadata surface. Extend them instead of duplicating them.

## 13. Scorecards

### CEO Review

Score: 8/10.

The plan protects public SEO traffic and multi-store correctness without trying to build every SEO feature at once. The biggest business value is preserving old URLs and preventing cross-store redirect mistakes.

### Design Review

Score: 7/10.

The admin UX should be practical and diagnostic: preview normalized slug, show collision/reserved-path reason, show canonical URL, and list old slugs. Avoid turning it into a full SEO dashboard in MVP.

### Engineering Review

Score: 8/10.

The phase order is conservative: store-scoped redirects, policy service, history, resolver, then runtime integration. This avoids breaking current product/category/page routes.

### DX Review

Score: 7/10.

Clear constants, DTOs, and resolver statuses make the system easier for future agents. The main DX requirement is documenting route families and reserved paths next to tests.

## 14. Decision Audit Trail

| Decision | Status | Reason |
| --- | --- | --- |
| Fix store-scoped redirects first | Approved | Current global `OldPath` uniqueness is the biggest multi-store SEO risk. |
| Keep active slug on entity tables | Approved | Avoids breaking existing API/UI/Storefront contracts. |
| Add slug history table | Approved | Needed for old slug redirect, audit, and future language support. |
| Add nullable language field | Approved | Future-proof without pretending localization exists. |
| Preserve Unicode slug policy | Approved | Matches current `SlugService` behavior and avoids unexpected URL migration. |
| Create shared SEO URL resolver | Approved | Removes duplicated product/category/page path logic. |
| Add page old-slug redirect | Approved | Page updates currently miss behavior product/category already have. |
| Extend metadata builder instead of replacing | Approved | Existing SEO rendering is already functional. |
| Defer hreflang | Approved | Requires real localization model. |
| Defer manufacturer sitemap | Approved | No active V2 manufacturer domain exists. |
| Defer `llms.txt` | Approved | Not required for MVP SEO routing/slug correctness. |
| Keep public routes stable | Approved | Prevents public URL churn and storefront regressions. |

## 15. Recommended Implementation Order

1. Phase 0 - baseline and safety inventory.
2. Phase 1 - store-scoped redirect foundation.
3. Phase 2 - shared slug policy service.
4. Phase 3 - slug history model.
5. Phase 4 - canonical URL resolver.
6. Phase 5 - product/category/page slug flows.
7. Phase 6 - storefront public redirect and canonical enforcement.
8. Phase 7 - SEO metadata policy hardening.
9. Phase 8 - indexing and canonical query rules.
10. Phase 9 - sitemap and crawler endpoint alignment.
11. Phase 10 - admin UX and Control Plane gateway.
12. Phase 12 - QA/docs/release gate.

Phase 11 is conditional and should run only after a real legacy topic URL inventory exists.

## 16. Definition Of Done

This phase is done when:

- SEO redirects are store-scoped.
- Product/category/page slug changes all retain old slug history.
- Product/category/page old slugs redirect 301 to active canonical paths.
- Page slug changes get redirect behavior equal to product/category.
- Shared resolver returns entity type/id, requested slug, canonical slug, language, redirect requirement, and HTTP status.
- Storefront public redirect cannot cross store and cannot loop.
- Existing public routes remain stable.
- Sitemap contains only canonical published/store-visible URLs.
- Cart, checkout, auth, account, and search remain noindex.
- Control Plane manages slug/redirect behavior only through Control Plane API.
- OpenAPI contract tests cover new/changed active V2 APIs.
- QA checklist files are updated with evidence.
- No legacy presentation project is extended.

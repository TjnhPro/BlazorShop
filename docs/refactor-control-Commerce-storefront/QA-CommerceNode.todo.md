# QA Commerce Node Todo

## Scope

QA verifies `BlazorShop.PresentationV2/BlazorShop.CommerceNode.API` through HTTP against a local PostgreSQL container.

Database target:

- Container: `blazorshop-commercenode-postgres`
- Compose file: `compose.commercenode.yml`
- Host port: `5434`
- Database: `blazorshop_commerce_node`
- User: `blazorshop_commerce_node`

Last verified: 2026-07-10

Current route state:

- `api/internal/*` was removed from the active CommerceNode runtime on 2026-07-14 by `BlazorShop.CommerceNode.RemoveLegacyInternal.autoplan.md`.
- Remaining `api/internal/*` entries in this file are historical QA evidence from the migration window unless explicitly marked as a current negative check.
- Current Storefront API QA targets `api/storefront/stores/{storeKey}/*`; old Internal routes should return 404.

## Environment Checklist

- [x] Start `blazorshop-commercenode-postgres` on port `5434`.
- [x] Apply `CommerceNodeDbContext` migrations through CommerceNode API startup.
- [x] Start `BlazorShop.CommerceNode.API` in Development.
- [x] Verify API base URL: `http://localhost:5180`.
- [x] Verify Swagger/API host is reachable.
- [x] Verify response envelope shape: `success`, `message`, `data`.

## Swagger And Store Scope Rescope

- [x] `GET /swagger/commerce-admin/swagger.json` returns 200. 2026-07-14: fixed duplicate media asset debug route conflict; document returned `Commerce Node Admin`.
- [~] Commerce Admin Swagger includes `api/commerce/*` endpoints only. 2026-07-14: document generated; detailed route membership still pending a full path audit.
- [x] Commerce Admin Swagger shows required `X-Node-Key` and `X-Node-Secret` headers. 2026-07-14: `GET /api/commerce/admin/products` operation included both required headers.
- [x] Commerce Admin Swagger shows required `storeKey` query for store-scoped admin endpoints. 2026-07-14: `GET /api/commerce/admin/products` and asset preview operations included required `storeKey`.
- [x] Commerce Admin Swagger does not show `X-Store-Key`. 2026-07-14: checked sampled admin operations.
- [x] `GET /swagger/storefront/swagger.json` returns 200. 2026-07-14: document returned `Storefront API`.
- [~] Storefront Swagger includes `api/storefront/stores/{storeKey}/*` endpoints only. 2026-07-14: document generated; detailed route membership still pending a full path audit.
- [x] Storefront Swagger shows required `{storeKey}` path parameter. 2026-07-14: sampled scoped categories operation included required path parameter.
- [x] Storefront Swagger does not show `X-Node-Key`, `X-Node-Secret`, or `X-Store-Key`. 2026-07-14: checked sampled scoped categories operation.
- [x] `GET /swagger/legacy-internal/swagger.json` returns 404 after removal. 2026-07-14: Legacy Internal Swagger was removed by `BlazorShop.CommerceNode.RemoveLegacyInternal.autoplan.md`.
- [x] Legacy Internal Swagger no longer appears in Swagger UI after removal.
- [x] Legacy Internal Swagger no longer shows `X-Store-Key` because the document was removed.
- [x] Store-scoped Commerce Admin endpoint without `storeKey` query returns a clear `success=false` response. 2026-07-14: `GET /api/commerce/admin/products` returned HTTP 400 with `storeKey query parameter is required.`
- [x] Store-scoped Commerce Admin endpoint with `X-Store-Key` but no `storeKey` query still fails. 2026-07-14: middleware source only reads query for `api/commerce/admin/*`; sampled missing-query request failed.
- [x] Storefront scoped endpoint reads `storeKey` from route and works without `X-Store-Key`. 2026-07-14: `GET /api/storefront/stores/default/catalog/categories` returned HTTP 200 without headers.
- [ ] Storefront scoped endpoint ignores/rejects header-only store scope and does not require node credentials.
- [x] Legacy `api/internal/*` is removed after Storefront scoped route QA passed; sampled old routes should return 404.

## Storefront API Contract Foundation

Baseline recorded 2026-07-14 for `BlazorShop.CommerceNode.ApiContractFoundationStorefrontHardening.autoplan.md`.

- [x] Storefront Swagger has stable `operationId` and summary metadata for every scoped operation. 2026-07-14: enforced by `CommerceNodeStorefrontOpenApiContractTests`.
- [x] Storefront Swagger declares request schemas for every operation with a request body. 2026-07-14: request-body presence and required flag covered by contract tests.
- [x] Storefront Swagger declares response schemas for every success and error response. 2026-07-14: contract tests assert every declared response has an `application/json` schema.
- [x] Storefront Swagger declares Bearer/cookie security requirements for protected operations. 2026-07-14: protected operation IDs are asserted in contract tests; Swagger declares Bearer and refresh-cookie schemes.
- [x] Storefront Swagger has no operation that only declares HTTP 200. 2026-07-14: contract tests require more than one response per operation.
- [x] Storefront Swagger does not expose domain entities or admin-only DTOs as public schemas. 2026-07-14: contract tests reject unsafe schema names and unsafe public fields.
- [x] Storefront Swagger validates as an OpenAPI document. 2026-07-14: Swagger is fetched, parsed as OpenAPI JSON, and checked for required document sections.
- [x] Storefront Swagger can generate a C# or TypeScript client in a smoke test. 2026-07-14: contract tests generate a TypeScript client stub from paths and operation IDs.
- [x] Storefront Swagger snapshot is stored under the test project for breaking-change detection. 2026-07-14: `storefront-openapi.snapshot.json` and path snapshot are committed.
- [x] Storefront public save-checkout request does not accept `userId`; authenticated identity comes from JWT. 2026-07-14: `CreateOrderItem.UserId` removed and contract tests reject `userId`.
- [x] Storefront order confirm request does not accept client-supplied `status`. 2026-07-14: scoped confirm action no longer has `status`; contract tests assert no such parameter.
- [x] Storefront product catalog query does not expose `IsPublished`. 2026-07-14: scoped query uses `StorefrontProductCatalogQuery`; contract tests reject `isPublished`.
- [x] Storefront PayPal capture uses POST for the capture side effect. 2026-07-14: `StorefrontPayments_CapturePayPal` is POST with a required body.
- [x] Storefront quantity request fields publish `minimum: 1` and reject invalid values. 2026-07-14: request DTOs use `[Range(1, int.MaxValue)]`; contract tests assert OpenAPI minimum.
- [x] Storefront product catalog `pageSize` publishes minimum/maximum bounds. 2026-07-14: `pageSize` range is `1..100` and asserted in contract tests.
- [x] Storefront auth and checkout request models publish email, password, and shipping-address validation metadata. 2026-07-14: public Storefront DTOs carry DataAnnotations for email/password/shipping address.
- [x] Storefront sort values are named strings, not numeric enum values. 2026-07-14: Storefront V2 emits lower-camel sort values; API contract uses a string pattern.
- [x] Storefront POST request bodies are required in OpenAPI. 2026-07-14: Storefront Swagger operation filter marks request bodies required and tests assert it.

## Catalog Product Search

- [x] Phase 0 baseline confirms public product catalog query is store-scoped through `CommerceNodeDbContext` and current store context. 2026-07-16: `CommerceNodeProductStoreScopeTests` covers current-store catalog reads and wrong-store exclusion.
- [x] Phase 0 baseline confirms public product catalog excludes unpublished, archived, scheduled, expired, slugless, hidden-category, and wrong-store products. 2026-07-16: existing repository/service guardrails reviewed before search changes.
- [x] Phase 0 baseline confirms category slug scope can include descendant categories and invalid category slugs return an empty page. 2026-07-16: `CommerceNodeProductStoreScopeTests` covers descendant category scope; invalid-slug empty-page behavior remains part of focused repository verification.
- [x] Phase 0 baseline confirms Storefront OpenAPI has `StorefrontCatalog_QueryProducts` with typed paged response, `pageSize` bounds, and named string `sortBy` metadata. 2026-07-16: `CommerceNodeStorefrontOpenApiContractTests` remains the contract guard.
- [x] Phase 0 makes no schema, route, or endpoint changes. 2026-07-16: docs/test-only baseline commit.
- [x] Public search uses normalized term policy with minimum length `2`. 2026-07-16 Phase 1: `CatalogSearchPolicy` and `CommerceNodeProductStoreScopeTests.GetPublishedCatalogPageAsync_TooShortSearchTermReturnsEmptyPage` passed.
- [x] Public search covers product name, SKU, short description, and description. 2026-07-16 Phase 1: `CommerceNodeProductStoreScopeTests.GetPublishedCatalogPageAsync_SearchesPublicProductFields` passed for all four public fields.
- [x] Public search has a PostgreSQL FTS expression index over name, short description, description, and SKU. 2026-07-16 Phase 1: migration `CommerceNodeCatalogSearchPublicFields` adds a CommerceNode-only GIN index.
- [x] Product filter metadata endpoint returns only supported facets. 2026-07-16 Phase 3: `GET /api/storefront/stores/{storeKey}/catalog/product-filter-metadata` returns category, availability, new-arrival, price range, page sizes, and named sort options; no brand/rating/delivery/spec facets were added.
- [x] Product filter metadata endpoint is generator-safe in Storefront OpenAPI. 2026-07-16 Phase 3: `CommerceNodeStorefrontOpenApiContractTests.StorefrontSwagger_ProductFilterMetadataHasGeneratorSafeContract` passed and snapshots were refreshed.
- [x] Product filter metadata price range is store/category/search scoped. 2026-07-16 Phase 3: `CommerceNodeProductStoreScopeTests.GetPublishedProductFilterMetadataAsync_ReturnsScopedPriceRange` passed.
- [x] Search suggestions endpoint is store-scoped and returns safe display fields only. 2026-07-16 Phase 4: `GET /api/storefront/stores/{storeKey}/catalog/search-suggestions` returns typed safe suggestion items; Storefront OpenAPI contract and scoped repository visibility tests passed in focused 54/54 run.
- [x] Search suggestions endpoint caps limit at 10 and returns empty results for too-short terms. 2026-07-16 Phase 4: endpoint uses `CatalogSearchPolicy` default/max/minimum rules and OpenAPI publishes `limit` range metadata.
- [x] Catalog query cache invalidates by store for catalog/inventory/media mutations. 2026-07-16 Phase 6: product/category/inventory/media/variant services call `InvalidateStoreCatalogAsync`; `MemoryCatalogQueryCacheTests` verifies target-store entries are evicted without clearing other stores.

## Storefront API Contract Final Hardening

Final hardening recorded 2026-07-14 for `BlazorShop.CommerceNode.ApiContractFinalHardening.autoplan.md`.

- [x] Storefront Swagger has no `ObjectCommerceNodeApiResponse` public schema. 2026-07-14: enforced by `CommerceNodeStorefrontOpenApiContractTests`.
- [x] Storefront auth login/refresh no longer double-envelope `success` and `message` inside `data`. 2026-07-14: login/refresh return `StorefrontTokenResponse` payload with access token metadata only.
- [x] Storefront error response schema requires `success`, `code`, `message`, and `traceId`. 2026-07-14: OpenAPI reader and schema assertions passed.
- [x] Storefront validation errors return `CommerceNodeApiErrorResponse` with `fieldErrors`. 2026-07-14: PayPal missing-token integration test verified `validation.failed`.
- [x] Protected Storefront routes return typed JSON 401 errors instead of empty challenges. 2026-07-14: change-password and current-user orders integration tests verified `auth.unauthenticated`.
- [x] Refresh without refresh cookie returns typed 401. 2026-07-14: integration test verified `auth.refresh_cookie_missing`.
- [x] Protected Storefront operations declare `Bearer` or `RefreshCookie` security requirements. 2026-07-14: OpenAPI contract tests assert operation-level security scheme names.
- [x] Public response arrays are required and non-null in OpenAPI. 2026-07-14: category tree, paged items, order lines, variants, sitemap lists, and variation options/values are guarded by contract tests.
- [x] Storefront product sort values are emitted as a named string enum. 2026-07-14: `sortBy` OpenAPI enum contains `newest`, `priceLowToHigh`, and related lower-camel values.
- [x] Storefront public order item history exposes `amountPaid`, not `amountPayed`. 2026-07-14: public OpenAPI and SharedV2 model were updated; internal Application spelling remains mapped only at the boundary.
- [x] PayPal capture failure no longer returns HTTP 200 `success=true`. 2026-07-14: provider failure now returns HTTP 409 with `payment.paypal_capture_failed`.
- [x] PayPal capture remains POST-only. 2026-07-14: integration test verified GET capture returns 405.
- [x] Storefront Swagger passes Microsoft.OpenApi reader parsing and has no broken schema references. 2026-07-14: `StorefrontSwagger_PassesOpenApiReaderValidation` and broken-ref traversal tests passed.
- [x] Storefront Swagger snapshot is updated after final hardening. 2026-07-14: `storefront-openapi.snapshot.json` refreshed and contract suite passed 16/16.

## Store Lifecycle

- [x] CommerceNode store lifecycle schema contains status, maintenance flag/message, display order, and company/contact profile fields. 2026-07-15: `CommerceNodeStoreLifecycleProfile` migration and model build passed.
- [x] Storefront current-store contract exposes lifecycle readiness and contact profile data without exposing admin-only entities. 2026-07-15: `CommerceNodeStorefrontOpenApiContractTests` passed after snapshot update.
- [x] Commerce Admin runtime store update accepts active/inactive and maintenance state through explicit request DTOs. 2026-07-15: focused lifecycle/control tests passed with the new request/response contracts.
- [x] Provisioning stores remain valid runtime records and can be reported as not ready to Storefront V2. 2026-07-15: Storefront V2 browser QA used a fake scoped current-store API returning `status=provisioning`; storefront rendered the not-ready maintenance state.

## Basic Page Content Core

- [x] CommerceNode `StorefrontPage` schema includes nullable `PageKey`, `DisplayOrder`, `IncludeInNavigation`, and nullable `NavigationLocation`. 2026-07-15: migration `CommerceNodeStorefrontPageContentMetadata` and CommerceNode API build passed.
- [x] Active page key is unique per store while custom pages can keep `PageKey = null`. 2026-07-15: `StorefrontPageServiceStoreScopeTests` covered duplicate active page-key rejection and same page-key allowed across stores.
- [x] Page key and navigation location are allowlisted before save. 2026-07-15: focused Storefront page service tests passed for invalid page key/location and required location when `IncludeInNavigation=true`.
- [x] Template catalog includes required legal/support/company keys and excludes `generic`, `contact`, `cart`, `checkout`, and `account`. 2026-07-15: `StorefrontPageTemplateServiceTests` passed.
- [x] Template status reports missing, mapped draft, mapped published, and suggested existing pages by default slug/title. 2026-07-15: `StorefrontPageTemplateServiceTests` passed.
- [x] Creating a draft from template creates an unpublished shell with sitemap/navigation disabled. 2026-07-15: `StorefrontPageTemplateServiceTests` passed.
- [x] Mapping and clearing existing pages updates `PageKey` without recreating page content. 2026-07-15: `StorefrontPageTemplateServiceTests` passed.
- [x] Storefront public navigation returns only published pages with `IncludeInNavigation=true`, non-null location, and known page key. 2026-07-15: `StorefrontPageTemplateServiceTests.ListNavigationLinksAsync_ReturnsPublishedNavigationPagesOnly` passed.
- [x] Storefront scoped OpenAPI contains `GET /api/storefront/stores/{storeKey}/pages/navigation` with typed response schema. 2026-07-15: Storefront OpenAPI contract tests passed and snapshots include `StorefrontPages_ListNavigation`.
- [x] Commerce Admin/ControlPlane page template endpoints use explicit request/response DTOs and preserve store-key scoped forwarding. 2026-07-15: `ControlPlaneCommerceCatalogServiceStoreMappingTests` passed for template status/navigation gateway routes.

## Menu Navigation Core

- [x] CommerceNode schema contains store-owned navigation menus and items with active unique menu system name per store. 2026-07-15: `CommerceNodeStoreNavigationCore` migration and `CommerceNodeDbContextModelTests` passed.
- [x] Navigation validation rejects unknown menu names, unsafe external URLs, invalid target requirements, cross-menu parents, and parent cycles. 2026-07-15: `StoreNavigationRulesTests` and `StoreNavigationServiceTests` passed.
- [x] Admin projection returns target health status while public projection hides broken, invalid, unpublished, archived, and cross-store targets. 2026-07-15: `StoreNavigationServiceTests.GetPublicMenuAsync_ProjectsOnlyValidCurrentStorePublishedTargets` passed.
- [x] Commerce Admin navigation endpoints and Storefront public `GET /api/storefront/stores/{storeKey}/navigation/{systemName}` endpoint are exposed with V2 DTOs and Swagger metadata. 2026-07-15: CommerceNode API build passed after Phase 3.
- [x] Menu changes invalidate public navigation cache. 2026-07-15: `StoreNavigationServiceTests.CreateItemAsync_InvalidatesPublicMenuCache` passed.
- [x] Category/product/page updates and archive operations invalidate navigation cache for the affected store. 2026-07-15: focused `CategoryServiceTests`, `ProductServiceTests`, and `StorefrontPageServiceStoreScopeTests` passed.
- [x] Cached public menu links follow page slug changes after Storefront page service invalidation. 2026-07-15: `StoreNavigationServiceTests.GetPublicMenuAsync_UsesNewPageSlugAfterPageServiceInvalidatesCache` passed.
- [x] Final focused CommerceNode verification passed. 2026-07-15: CommerceNode API build passed and `StoreNavigation`, `ControlPlaneCommerceCatalogServiceStoreMappingTests`, and `CommerceNodeDbContextModelTests` focused run passed 33/33.
- [ ] API smoke: create menu and items through `api/commerce/admin/navigation/*` against PostgreSQL with node credentials.
- [ ] API smoke: `GET /api/storefront/stores/{storeKey}/navigation/main` returns only safe public fields and no internal IDs.

## SEO Routing Slug Core

- [x] CommerceNode `SeoRedirects` schema is store-scoped and no longer has a global unique `OldPath` index. 2026-07-15: migration `CommerceNodeStoreScopedSeoRedirects` adds nullable `StoreId`, future entity/language columns, FK to `commerce_store`, and active unique `(StoreId, OldPath)`.
- [x] Existing unscoped redirect rows are handled safely. 2026-07-15: migration backfills only when there is exactly one non-archived store; runtime store-scoped resolution ignores remaining `StoreId = null` rows.
- [x] Storefront redirect resolution cannot cross stores. 2026-07-15: `CommerceNodeSeoRedirectStoreScopeTests.ResolvePublicPathAsync_DoesNotResolveOtherStoreRedirect` passed.
- [x] Same old path is allowed across different stores while duplicate lookup is scoped to current store. 2026-07-15: `CommerceNodeSeoRedirectStoreScopeTests` passed.
- [x] Focused SEO redirect regression tests passed. 2026-07-15: `dotnet test BlazorShop.Tests/BlazorShop.Tests.csproj --no-restore --filter "FullyQualifiedName~SeoRedirect"` passed 28/28.
- [x] Shared SEO slug policy validates lowercase normalization, Unicode letter preservation, empty/slash/reserved rejection, duplicate rejection, and suffix generation. 2026-07-15: `StoreSeoSlugPolicyServiceTests` passed.
- [x] CommerceNode slug collision checker scopes product, category, and page slug lookup by store. 2026-07-15: `CommerceNodeStoreSeoSlugCollisionCheckerTests` passed.
- [x] Product SEO redirect regression remains covered after policy foundation. 2026-07-15: `ProductSeoServiceTests` passed 9/9 after read-detail fallback fix.
- [x] CommerceNode schema contains `store_seo_slug_history` with active entity/slug indexes and FK to `commerce_store`. 2026-07-15: migration `CommerceNodeStoreSeoSlugHistory` generated and CommerceNode API build passed.
- [x] Slug history service creates active rows, replaces active slugs while retaining old rows, rejects second active entity/route-family slugs, and backfills idempotently. 2026-07-15: `StoreSeoSlugHistoryServiceTests` passed 5/5.
- [x] SEO URL resolver resolves active canonical product/category/page paths and returns redirect metadata for old slugs. 2026-07-15: `SeoUrlResolverTests` passed 4/4.
- [x] SEO URL resolver does not leak another store's slug and returns `gone` for unpublished page history. 2026-07-15: `SeoUrlResolverTests.ResolvePublicPathAsync_SlugBelongsToAnotherStore_ReturnsNotFound` and `ResolvePublicPathAsync_UnpublishedPage_ReturnsGone` passed.
- [x] Product and category SEO updates use shared store-scoped slug policy/history when V2 services are available while preserving existing public redirect behavior. 2026-07-15: `ProductSeoServiceTests` and `CategorySeoServiceTests` focused run passed.
- [x] Storefront page create/update uses shared slug lifecycle, generates a slug from title when create slug is omitted, and creates a 301 for published page slug changes. 2026-07-15: `StorefrontPageServiceStoreScopeTests` focused run passed.
- [x] Storefront SEO redirect API preserves explicit redirect priority and falls back to slug-history canonical redirects when no explicit redirect exists. 2026-07-15: `StorefrontScopedSeoControllerTests` passed.
- [x] Storefront redirect loop/invalid target safety remains covered after resolver-backed redirect API change. 2026-07-15: `SeoRedirectResolutionServiceTests`, `SeoUrlResolverTests`, and `StorefrontRedirectMonitoringTests` focused run passed 15/15.
- [x] Shared SEO metadata builder does not emit unsafe canonical or Open Graph URLs and falls back to route canonical when an override is unsafe. 2026-07-15: `SeoMetadataBuilderTests` passed.
- [x] Commerce Admin exposes store-scoped slug lifecycle endpoints for manager diagnostics. 2026-07-15 SEO Routing Slug Phase 10: added `POST /api/commerce/admin/seo/slugs/generate`, `POST /api/commerce/admin/seo/slugs/validate`, and `GET /api/commerce/admin/seo/slugs/history`; `dotnet build BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/BlazorShop.CommerceNode.API.csproj --no-restore` passed.
- [x] Commerce Admin slug lifecycle OpenAPI metadata has stable operation IDs, response schemas, error responses, and required request bodies. 2026-07-15 SEO Routing Slug Phase 12: `CommerceNodeAdminStoreOpenApiMetadataTests` passed 4/4 and guards `CommerceSeoSlugs_Generate`, `CommerceSeoSlugs_Validate`, and `CommerceSeoSlugs_ListHistory`.
- [x] SEO slug policy/history/resolver release-gate suite passed. 2026-07-15 SEO Routing Slug Phase 12: focused `StoreSeoSlugPolicyServiceTests`, `CommerceNodeStoreSeoSlugCollisionCheckerTests`, `StoreSeoSlugHistoryServiceTests`, and `SeoUrlResolverTests` run passed 17/17.
- [x] SEO redirect/page lifecycle release-gate suite passed. 2026-07-15 SEO Routing Slug Phase 12: focused `CommerceNodeSeoRedirectStoreScopeTests`, `SeoRedirectResolutionServiceTests`, `StorefrontScopedSeoControllerTests`, and `StorefrontPageServiceStoreScopeTests` run passed 29/29.
- [x] CommerceNode API builds after SEO Routing Slug release gate. 2026-07-15 Phase 12: `dotnet build BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/BlazorShop.CommerceNode.API.csproj --no-restore` passed.
- [ ] Slug lifecycle endpoints require Commerce Admin node credentials and `storeKey` query in live HTTP QA.
- [ ] Slug validate returns a 200 envelope with payload `success=false` and shared-policy rejection message for reserved/slash/collision input.
- [ ] Slug history endpoint returns only the current store's entity history and does not expose another store's slugs.
- [x] Approved legacy static page routes are converted to store-scoped redirects only when a published page mapping exists. 2026-07-15 SEO Routing Slug Phase 11: `StorefrontPageServiceStoreScopeTests.CreateAsync_PublishedMappedLegacyPage_CreatesApprovedLegacyRedirect` passed for `/about-us -> /pages/about-us`.
- [x] Draft or unknown legacy page paths do not create broad redirects. 2026-07-15 SEO Routing Slug Phase 11: focused StorefrontPageService tests passed for draft mapped page and custom page no-redirect cases.
- [x] CommerceNode API builds after legacy topic redirect compatibility. 2026-07-15 SEO Routing Slug Phase 11: `dotnet build BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/BlazorShop.CommerceNode.API.csproj --no-restore` passed.
- [ ] API smoke: create two active redirects with the same `OldPath` in two different stores through `api/commerce/admin/seo/redirects`.
- [ ] API smoke: duplicate active `OldPath` in the same store returns conflict/validation failure.
- [ ] API smoke: `GET /api/storefront/stores/{storeKey}/seo/redirects/resolve` ignores another store's redirect.

## Store Config Consumption And Hardening

- [x] Commerce Admin store create/update accepts safe absolute `http`/`https` asset URLs for logo/favicon/icon fields. 2026-07-15: live CommerceNode admin HTTP QA accepted safe absolute logo/favicon/icon URLs.
- [x] Commerce Admin store create/update accepts safe root-relative public asset paths for logo/favicon/icon fields. 2026-07-15: live CommerceNode admin HTTP QA accepted `/images/banner-bg.jpg`, `/favicon.ico`, `/icon-192.png`, and `/apple-touch-icon.png`.
- [x] Commerce Admin store create/update rejects `javascript:`, `data:`, protocol-relative, malformed, and backslash asset URLs. 2026-07-15: live CommerceNode admin HTTP QA returned 400 for unsafe asset URL cases; ControlPlane UI also surfaced the unsafe logo validation message.
- [x] Commerce Admin store create/update rejects malformed `CdnHost` and invalid MS tile colors. 2026-07-15: live CommerceNode admin HTTP QA returned 400 for malformed CDN host and invalid MS tile color.
- [x] Commerce Admin store Swagger documents stable operation IDs and summaries for store endpoints. 2026-07-15: `/swagger/commerce/swagger.json` store operations were inspected for stable operation IDs and summaries.
- [x] Commerce Admin store Swagger declares response schemas for success and expected error responses. 2026-07-15: store operations exposed response codes beyond bare 200 and schemas for success/error results.
- [x] Commerce Admin store Swagger keeps required `X-Node-Key` and `X-Node-Secret` header metadata. 2026-07-15: Swagger inspection confirmed required node credential header metadata on Commerce Admin store operations.
- [x] Storefront current-store response exposes only safe public runtime profile fields. 2026-07-15: live current-store response included public branding/contact/runtime fields and did not expose `metadataJson` or `controlPlaneStorePublicId`.
- [~] Checkout preview/place-order fallback currency uses the current store default when cart/session currency is missing. 2026-07-15: Storefront cart/checkout HTTP smoke rendered current `GBP`; deeper missing-session-currency service/API fallback coverage is still pending.

## Configuration And Feature State Core

- [x] Public configuration-adjacent Storefront schemas do not expose raw settings, metadata, secrets, node credentials, audit fields, or internal store linkage. 2026-07-15: `CommerceNodeStorefrontOpenApiContractTests.StorefrontSwagger_PublicConfigurationSchemasDoNotExposeSecretsOrInternalFields` passed for current-store, payment method, and SEO settings schemas.
- [x] Admin payment method DTO raw `SettingsJson` exposure is documented as a provider hardening risk, not treated as a public Storefront contract. 2026-07-15: Phase 0 inventory recorded `StorePaymentMethodDto.SettingsJson` for Phase 5 hardening.
- [x] `GET /api/storefront/stores/{storeKey}/configuration` has stable OpenAPI metadata and typed success/error response schemas. 2026-07-15: `StorefrontConfiguration_Get` was added to Storefront Swagger and `CommerceNodeStorefrontOpenApiContractTests` passed 23/23 after snapshot refresh.
- [x] Public configuration projection is allowlist-only and excludes `MetadataJson`, provider `SettingsJson`, credentials, secrets, and internal store linkage. 2026-07-15: public configuration schema guard covers nested config response DTOs.
- [x] Existing current-store, payment-method, and SEO Storefront endpoints remain available after adding the consolidated configuration endpoint. 2026-07-15: endpoint was additive; Storefront OpenAPI snapshot still contains existing operations.
- [x] CommerceNode owns typed store-scoped SEO settings schema. 2026-07-15: migration `CommerceNodeStoreSeoSettings` adds `store_seo_settings` with unique `store_id` and FK to `commerce_store`.
- [x] Store SEO defaults resolve through store override before singleton SEO fallback. 2026-07-15: `StoreSeoSettingsServiceTests` covered no-override fallback and override precedence.
- [x] Store SEO override save path validates input and invalidates cached resolved settings. 2026-07-15: focused service tests covered invalid canonical URL rejection and update-then-read cache invalidation.
- [x] Storefront public configuration and scoped SEO settings reads use store-scoped SEO resolver. 2026-07-15: controllers now depend on `IStoreSeoSettingsService`; CommerceNode API build passed.
- [x] Storefront public configuration response is cached by `store-public-config:{storeKey}` after scoped store resolution. 2026-07-15: `StorefrontScopedConfigurationController` uses `IStorefrontPublicConfigurationCache`; CommerceNode API build passed.
- [x] Commerce store profile updates invalidate the affected Storefront public configuration cache. 2026-07-15: `CommerceStoreServiceValidationTests.UpdateAsync_InvalidatesPublicConfigurationCache` passed.
- [x] Store SEO override saves invalidate both resolved SEO settings cache and Storefront public configuration cache. 2026-07-15: `StoreSeoSettingsServiceTests.SaveOverrideAsync_InvalidatesPublicConfigurationCacheForStore` passed.
- [x] Payment method metadata updates invalidate Storefront public configuration cache. 2026-07-15: `CommerceNodePaymentMethodServiceCacheTests.UpdateAsync_InvalidatesPublicConfigurationCacheForStore` passed.
- [x] Admin payment method DTO does not echo raw provider `SettingsJson` or secret values. 2026-07-15: `CommerceNodePaymentMethodSecretBoundaryTests.GetAsync_ReturnsSettingsStatusWithoutRawSettingsJson` passed.
- [x] Payment method metadata update with omitted `SettingsJson` preserves existing provider settings. 2026-07-15: `CommerceNodePaymentMethodSecretBoundaryTests.UpdateAsync_WhenSettingsJsonIsNull_PreservesExistingSettings` passed.
- [x] Provider settings removal requires explicit `ClearSettings` and never echoes the old secret value. 2026-07-15: `CommerceNodePaymentMethodSecretBoundaryTests.UpdateAsync_WhenClearSettingsIsTrue_RemovesSettingsWithoutEchoingSecret` passed.
- [x] Payment method audit metadata records settings status only, not provider settings JSON. 2026-07-15: secret boundary tests asserted audit metadata does not contain `SettingsJson` or secret values.
- [x] Store feature state schema is store-scoped with one state per feature per store. 2026-07-15: migration `CommerceNodeStoreFeatureStateCore` and `CommerceNodeDbContextModelTests.StoreFeatureState_HasOneStatePerFeaturePerStore` passed.
- [x] Store feature state service lists only allowlisted feature keys and does not write default rows on read. 2026-07-15: `StoreFeatureStateServiceTests.GetAsync_ReturnsAllowlistedDefaultsWithoutPersistingRows` passed.
- [x] Store feature state update validates feature keys, persists overrides, and invalidates public configuration cache. 2026-07-15: `StoreFeatureStateServiceTests` covered unknown-key rejection, checkout disable snapshot, and `store-public-config:{storeKey}` invalidation.
- [x] Storefront public configuration uses store feature state for public flags. 2026-07-15: `StorefrontScopedConfigurationController` now resolves `IStoreFeatureStateService`; CommerceNode API build and Storefront OpenAPI contract tests passed.

### Security Privacy Settings

- [x] Commerce Admin security/privacy settings are store-scoped under `api/commerce/admin/security-privacy` and require `storeKey` query. 2026-07-16: Phase 6 added `StoreSecurityPrivacySettings` in `CommerceNodeDbContext` and focused `SecurityPrivacyPhase6AdminManagementTests` passed.
- [x] Captcha secret reference is stored server-side only and response DTO exposes only `SecretConfigured`, `LastRotatedAt`, and provider display name. 2026-07-16: guarded by `SecurityPrivacyPhase6AdminManagementTests`.
- [x] Security/privacy updates invalidate Storefront public configuration cache and Storefront runtime resolves consent/captcha from the store-scoped service. 2026-07-16: service invalidates `IStorefrontPublicConfigurationCache`; focused tests passed.
- [x] Commerce Admin Swagger metadata covers security/privacy settings operation ids, summaries, request body required flag, and response schemas. 2026-07-16: `CommerceSecurityPrivacy_*` operation metadata added and focused admin OpenAPI metadata tests passed.
- [x] Checkout server behavior enforces the `checkout` feature state, not only UI hiding. 2026-07-15: `StorefrontCheckoutServiceTests.CheckoutAsync_WhenCheckoutFeatureDisabled_RejectsPreviewAndPlaceOrder` passed.
- [x] Store payment methods support safe public metadata without exposing provider settings JSON. 2026-07-15: migration `CommerceNodePaymentProviderAvailability` adds icon, short display text, supported currency/country code JSON, and order total limit fields; `CommerceNodePaymentMethodSecretBoundaryTests.GetPaymentMethodsAsync_ReturnsSafePublicMetadata` passed.
- [x] Checkout preview enforces payment method country availability. 2026-07-15: `StorefrontCheckoutServiceTests.PreviewAsync_WhenPaymentMethodUnavailableForCountry_ReturnsValidationIssue` passed.
- [x] Checkout place-order enforces payment method order-total availability after preview. 2026-07-15: `StorefrontCheckoutServiceTests.PlaceOrderAsync_WhenPaymentMethodUnavailableForTotal_RejectsOrder` passed.
- [x] Storefront payment method OpenAPI schema includes safe metadata fields and no provider settings JSON. 2026-07-15: Storefront Swagger snapshot refreshed and `CommerceNodeStorefrontOpenApiContractTests` passed 23/23.

## Store Mapping

- [x] Product admin catalog page is scoped to the current Commerce store. 2026-07-15: `CommerceNodeProductStoreScopeTests.GetCatalogPageForCurrentStoreAsync_ReturnsOnlyCurrentStoreProducts` passed.
- [x] Product admin detail returns not found/null for a product belonging to another store. 2026-07-15: repository and service guardrails passed for cross-store product detail/update/delete.
- [x] Product update/delete rejects cross-store IDs before mutating data. 2026-07-15: focused `ProductServiceTests` cross-store update/delete guardrails passed.
- [x] Product create/update rejects a category from another current store. 2026-07-15: `AddAsync_WhenCategoryBelongsToDifferentCurrentStore_ReturnsValidationFailure` passed.
- [x] Product SEO slug duplicate check is scoped by store. 2026-07-15: `ProductSeoServiceTests.UpdateAsync_WhenSlugExistsOnlyInAnotherStore_AllowsUpdate` passed and verified `ProductSlugExistsInStoreAsync`.
- [x] Category admin list/query/detail/update/delete are scoped to the current Commerce store. 2026-07-15: `CommerceNodeCategoryStoreScopeTests` plus focused `CategoryServiceTests` cross-store update/delete guardrails passed.
- [x] Category parent/child update rejects cross-store parent assignment through current-store scoped lookup. 2026-07-15: `CategoryServiceTests.UpdateAsync_WhenParentBelongsToDifferentStore_ReturnsValidationFailure` passed.
- [x] Category SEO slug duplicate check is scoped by store. 2026-07-15: `CategorySeoServiceTests.UpdateAsync_WhenSlugExistsOnlyInAnotherStore_AllowsUpdate` passed and verified `CategorySlugExistsInStoreAsync`.
- [x] StorefrontPage list/detail/slug/sitemap scope has dedicated store mapping guardrails. 2026-07-15: `StorefrontPageServiceStoreScopeTests` passed 7/7 for list, detail, update, archive, public slug, sitemap, and per-store duplicate slug behavior.
- [x] Product/Category `StoreId` is required in the CommerceNode EF model without changing the shared domain CLR nullability. 2026-07-15: `CommerceNodeDbContextModelTests.CatalogStoreId_IsRequiredInCommerceNode` passed.
- [x] Product/Category Commerce store FK uses restrict delete behavior. 2026-07-15: `CommerceNodeDbContextModelTests.CatalogStoreForeignKey_RestrictsCommerceStoreDelete` passed.
- [x] CommerceNode migration backfills null catalog `StoreId` only when ownership can be derived safely or there is exactly one active store; otherwise it fails with a manual mapping error. 2026-07-15: migration `CommerceNodeRequiredCatalogStoreOwnership` reviewed and model tests passed.
- [x] Store Mapping release gate passes for Product, Category, StorefrontPage, CommerceNode model constraints, and ControlPlane gateway storeKey propagation. 2026-07-15: focused release-gate test filter passed 21/21; CommerceNode API, ControlPlane API, and ControlPlane Web builds passed.

## Cart, Checkout & Payment Provider MVP

Baseline plan: `BlazorShop.CommerceNode.CartCheckoutPaymentProviderMvp.autoplan.md`.

- [x] Phase 0 records pending guardrail tests for server-cart checkout product visibility. 2026-07-14: `CartServiceTests.StorefrontCheckoutAsync_ShouldRejectUnpublishedProducts_WhenServerCartValidationIsImplemented` added as an intentional skipped guardrail.
- [x] Phase 0 records pending guardrail tests for idempotent place-order. 2026-07-14: `CartServiceTests.StorefrontPlaceOrderAsync_ShouldReturnSameOrder_ForDuplicateIdempotencyKey` added as an intentional skipped guardrail.
- [x] Phase 0 records pending OpenAPI guardrail for future cart/checkout/payment provider endpoints. 2026-07-14: `CommerceNodeStorefrontOpenApiContractTests.StorefrontSwagger_CartCheckoutPaymentProviderEndpointsHaveGeneratorSafeContracts` added as an intentional skipped guardrail.
- [x] Server cart session tables are created in `CommerceNodeDbContext` only. 2026-07-14: `CartSession`/`CartLine` entities and migration `CommerceNodeServerCartSessions` added; `StorefrontCartSessionServiceTests` passed 5/5.
- [x] Store-scoped commerce customer profile is unique by `(StoreId, NormalizedEmail)` and does not require adding `StoreId` to `AppUser`. 2026-07-14: `CommerceCustomer`/`StorefrontCustomerService` added with Commerce Node migration `CommerceNodeStorefrontCustomers`; `StorefrontCustomerServiceTests` passed 5/5.
- [x] Cart token lookup refuses cross-store token reuse without revealing another store's cart. 2026-07-14: `ResolveAsync_ReturnsNotFound_WhenTokenBelongsToDifferentStore` covers store + token hash lookup.
- [x] Cart application service validates published/current-store product availability before mutating server cart. 2026-07-14: `StorefrontCartServiceTests` covers published add, unpublished/unavailable, wrong-store, invalid variant, minimum quantity, custom selected attributes, personalization line splitting, and revalidation after product unavailability.
- [x] Cart API rejects unpublished, archived, wrong-store, and invalid-variant products. 2026-07-14: server-cart Storefront API routes call `IStorefrontCartService`; OpenAPI contract test `StorefrontSwagger_ServerCartEndpointsHaveGeneratorSafeContracts` covers generator-safe cart endpoints and quantity minimums.
- [x] Checkout preview validates cart version, customer email/name, payment method, and shipping address without creating an order. 2026-07-14: `StorefrontCheckoutServiceTests` covers stale version conflict and invalid email/shipping issues with no order creation.
- [x] Place-order requires idempotency and returns the original result for duplicate retry. 2026-07-14: `StorefrontCheckoutServiceTests.PlaceOrderAsync_DuplicateIdempotencyKey_ReturnsSameOrder` covers duplicate retry with one order.
- [x] COD checkout creates order from server cart snapshot and marks the cart ordered. 2026-07-14: `PlaceOrderAsync` creates COD orders from checkout session/cart snapshot, expires the cart, snapshots personalization fields, and contract tests cover `/checkout/place-order`.
- [x] Payment attempts are persisted before online provider redirect/callback work. 2026-07-14: `PaymentAttempt`/`PaymentProviderEvent` ledger and migration `CommerceNodePaymentAttemptLedger` added; `PaymentAttemptServiceTests` covers idempotent create, state transitions, failure details, and event dedupe.
- [x] Provider callback/webhook endpoints are POST-only and idempotent. 2026-07-14: Storefront payment callback/webhook endpoints added as POST routes backed by `PaymentAttemptService.RecordProviderEventAsync`; event ID dedupe is covered by `PaymentAttemptServiceTests`.
- [x] First online provider creates a hosted redirect session without creating an order before capture. 2026-07-14: Stripe provider MVP added; `PlaceOrderAsync_StripeCreatesRedirectAttemptWithoutOrder` covers `requires_action`, provider session metadata, idempotent retry, `order_pending` checkout, and active cart.
- [x] Captured online payment attempt creates exactly one order and is replay-safe. 2026-07-14: `PaymentAttemptServiceTests.TransitionAsync_CapturedOnlineAttemptCreatesOrderExactlyOnce` covers order creation, cart ordered state, stock deduction, and replay idempotency.
- [x] Checkout payment method step is explicit and server-owned. 2026-07-17 Checkout Core Phase 5: `POST /api/storefront/stores/{storeKey}/checkout/{checkoutSessionId}/payment-method` selects from server-filtered `StorePaymentMethod` options and stores only the selected key on the checkout session.
- [x] Checkout session payment projection applies enabled, currency, country, cart-total, min/max, and display-order rules without exposing provider settings. 2026-07-17: `StorefrontCheckoutServiceTests.SelectPaymentMethodAsync_ProjectsOnlyMethodsAllowedForCurrencyCountryAndTotal` passed.
- [x] Storefront payment-method OpenAPI contract is generator-safe. 2026-07-17: request schema only accepts `paymentMethodKey`, session response exposes `selectedPaymentMethod` and non-null `paymentMethods`, and `CommerceNodeStorefrontOpenApiContractTests` passed after snapshot refresh.
- [x] Missing online provider configuration returns a safe failure without creating an order. 2026-07-14: Stripe provider config checks return conflict and checkout marks the attempt failed with safe `provider_session_failed` details.
- [x] Storefront Swagger includes generator-safe request/response/error schemas for every new cart/checkout/payment endpoint. 2026-07-14: server cart, checkout preview, place-order, payment attempt polling, provider callback, and webhook endpoints covered by contract tests.
- [x] Storefront Swagger snapshot is refreshed after cart/checkout/payment API cutover. 2026-07-14: refreshed through Phase 10 first online provider result-shape update; focused checkout/payment/contract/Storefront suite passed 63/63 before Storefront host redirect test, then Storefront host suite passed 27/27.
- [x] Direct raw Storefront checkout path is retired after Storefront V2 cutover. 2026-07-14: `POST /cart/checkout` removed from active Storefront API contract/client; focused contract/Storefront suite passed 51/51.

## Currency

- [x] Phase 0 baseline guardrails document the old client-supplied cart currency and Stripe two-decimal minor-unit assumptions. 2026-07-15: focused cart/checkout/Stripe tests passed before Phase 1 hardening.
- [x] Cart add-line snapshots use server-resolved `CommerceStore.DefaultCurrencyCode` instead of client `CurrencyCode`. 2026-07-15: `StorefrontCartServiceTests.AddLineAsync_UsesServerDefaultCurrency_WhenClientSendsDifferentCurrency` and `AddLineAsync_UsesResolvedStoreCurrency_AsLineSnapshot` passed.
- [x] Cart validation returns the store default currency and does not let mixed line snapshots drive totals. 2026-07-15: covered by Phase 1 cart/checkout service tests.
- [x] Checkout preview and place-order use server-resolved store default currency for payment availability, order, and payment attempts in current single-currency mode. 2026-07-15: focused checkout service tests passed after resolver hardening.
- [x] Store-supported currency schema is store-scoped with one row per `(StoreId, CurrencyCode)` and CommerceNode migration backfills one base currency row per existing store. 2026-07-15: migration `CommerceNodeStoreCurrencies` and `CommerceNodeDbContextModelTests.StoreCurrency_HasOneCurrencyPerStore` passed.
- [x] Commerce Admin currency service creates the base currency row from `CommerceStore.DefaultCurrencyCode` and prevents disabling the base currency. 2026-07-15: `StoreCurrencyServiceTests` passed.
- [x] Commerce Admin currency updates invalidate Storefront public configuration cache. 2026-07-15: `StoreCurrencyServiceTests.UpdateAsync_KeepsBaseCurrencyEnabledAndInvalidatesPublicConfiguration` passed.
- [x] Storefront public configuration `currencyOptions.supportedCurrencyCodes` resolves from enabled `StoreCurrency` rows and always includes the base currency. 2026-07-15: `StoreCurrencyServiceTests.ResolveSupportedCurrencyCodesAsync_ReturnsEnabledCurrenciesWithBaseFirst` passed.
- [x] Commerce Admin currency endpoints have stable Swagger operation IDs, summaries, required body metadata, and typed response schemas. 2026-07-15: `CommerceCurrencyAdminOperationMetadataFilter` covers `CommerceCurrencies_List` and `CommerceCurrencies_Update`; full admin Swagger smoke still pending.
- [x] Unit price, line total, order total, and payment amount use central rounding services in current cart/checkout flow. 2026-07-15: focused `MoneyServicesTests`, `StorefrontCartServiceTests`, and `StorefrontCheckoutServiceTests` passed.
- [x] Stripe minor-unit conversion uses currency decimal metadata instead of assuming two decimal places. 2026-07-15: `StripeStorefrontPaymentProviderTests.CreateHostedSessionAsync_UsesCurrencyDecimalDigitsForMinorUnits` and `MoneyServicesTests.ToMinorUnits_UsesCurrencyDecimalDigits` passed for USD and JPY.
- [x] Store-supported currency metadata is exposed through public configuration without enabling checkout conversion. 2026-07-15: supported non-base currency hints are recognized by `StorefrontWorkingCurrencyResolver` but checkout currency remains base until conversion is enabled.
- [x] Storefront working-currency resolver validates client hints against enabled `StoreCurrency` rows and rejects unsupported spoofing by falling back to base currency. 2026-07-15: `StorefrontWorkingCurrencyResolverTests` passed.
- [x] Storefront cart add-line uses the working-currency resolver and cannot persist a supported non-base currency before conversion is enabled. 2026-07-15: `StorefrontCartServiceTests.AddLineAsync_WhenSupportedNonBaseHintIsRequestedBeforeConversion_SnapshotsBaseCurrency` passed.
- [x] Storefront currency preference command is POST-only with typed OpenAPI request/response metadata and no side-effecting GET. 2026-07-15: `StorefrontCurrency_SetPreference` added to Storefront Swagger and `CommerceNodeStorefrontOpenApiContractTests` passed after snapshot refresh.
- [x] Manual exchange-rate schema is store-scoped with one row per `(StoreId, BaseCurrencyCode, TargetCurrencyCode, ProviderKey)`. 2026-07-15: migration `CommerceNodeStoreCurrencyExchangeRates` and `CommerceNodeDbContextModelTests.StoreCurrencyExchangeRate_HasOneRatePerProviderPair` passed.
- [x] Commerce Admin manual exchange-rate endpoints have stable Swagger operation IDs and typed response schemas. 2026-07-15: `CommerceCurrencies_ListExchangeRates`, `CommerceCurrencies_UpsertExchangeRate`, and `CommerceCurrencies_DisableExchangeRate` are covered by `CommerceCurrencyAdminSwaggerFilter_DefinesStableOperationMetadata`.
- [x] Manual exchange-rate upsert requires the target currency to be enabled for the store and rejects base-currency rows. 2026-07-15: `StoreCurrencyExchangeRateServiceTests` passed.
- [x] Money conversion service resolves same-currency conversion as rate `1` and fails non-base conversion when rate is missing, disabled, stale, or not configured. 2026-07-15: focused `StoreCurrencyExchangeRateServiceTests` passed; checkout still remains base-currency until Storefront selector/display conversion phase.
- [x] Storefront working-currency resolver accepts supported non-base currency only when conversion is configured. 2026-07-15: `StorefrontWorkingCurrencyResolverTests.ResolveAsync_WhenHintIsSupportedNonBaseWithRate_AcceptsWorkingCurrency` passed; missing-rate supported currency still falls back to base with `conversion_not_configured`.
- [x] Storefront cart add-line converts base unit price before snapshotting a non-base working currency. 2026-07-15: `StorefrontCartServiceTests.AddLineAsync_WhenSupportedNonBaseHasConversion_SnapshotsConvertedCurrencyAndPrice` passed for USD -> EUR.
- [x] Storefront checkout preview/place-order/order/payment use the converted cart snapshot currency consistently. 2026-07-15: `StorefrontCheckoutServiceTests.PlaceOrderAsync_WhenCartUsesConvertedCurrency_UsesSnapshotCurrencyForOrderAndPayment` passed for EUR.
- [x] Storefront catalog/product contracts expose additive display-price fields for working currency without replacing base `price`/`comparePrice`. 2026-07-15: `StorefrontCatalogProductResponse`, `StorefrontProductResponse`, and `StorefrontProductVariantResponse` now include display money fields; Storefront OpenAPI snapshot was refreshed.
- [x] Storefront catalog/product APIs accept optional `currencyCode` query metadata and keep generator-safe OpenAPI contracts. 2026-07-15: `CommerceNodeStorefrontOpenApiContractTests` passed 23/23 after snapshot refresh.
- [x] Storefront product display conversion uses server-side working-currency resolution and money conversion services, not browser-side conversion math. 2026-07-15: conversion is resolved inside `StorefrontScopedCatalogController` through `IStorefrontWorkingCurrencyResolver` and `IMoneyConversionService`.

## Cart Core

Plan: `Cart Core.todo.md`.

- [x] Phase 0 baseline confirms cart runtime persistence is owned by `CommerceNodeDbContext` through `CartSessions` and `CartLines`. 2026-07-16: source review and existing session service tests cover token/session writes in Commerce Node only.
- [x] Public Storefront cart routes stay under `api/storefront/stores/{storeKey}/cart` and do not add `api/internal/*`. 2026-07-16: `CartCorePhase0InventoryTests` guards scoped route markers.
- [x] Public Storefront cart request contracts do not expose customer id, app user id, store id, browser-supplied price, discount, tax, or order status fields. 2026-07-16: `CartCorePhase0InventoryTests.CommerceNode_PublicCartRequestContractsDoNotExposeServerOwnedFields` added.
- [x] Cart token lookup remains store-scoped and token hashes are stored instead of plaintext tokens. 2026-07-16: existing `StorefrontCartSessionServiceTests` cover hash-only storage and wrong-store token rejection.
- [x] Cart add/update keeps server-side product, variant, selected-attribute, quantity, availability, stock, and price snapshot validation. 2026-07-16: existing `StorefrontCartServiceTests` remain in the Phase 0 focused verification gate.
- [x] Storefront OpenAPI keeps generator-safe cart operation metadata, `X-Cart-Token`, required request body, and quantity minimum metadata. 2026-07-16: existing `CommerceNodeStorefrontOpenApiContractTests` remain in the Phase 0 focused verification gate.
- [x] Cart projection includes display fields, totals, warnings, checkout eligibility, and badge summary. 2026-07-16 Phase 1: `StorefrontCartServiceTests` asserts line projection/totals and cart summary; Storefront OpenAPI contract tests assert response schema fields.
- [x] Recalculate command refreshes stale snapshots through POST and keeps validate non-mutating. 2026-07-16 Phase 2: `StorefrontCartServiceTests` covers stale price refresh, stale expected version conflict, and non-mutating validate; `CommerceNodeStorefrontOpenApiContractTests` guards POST metadata and refreshed snapshot.
- [x] Authenticated cart merge derives customer identity from trusted auth context only. 2026-07-16 Phase 3: merge endpoint is Bearer-protected, accepts no identity body, derives `AppUserId` from claims, and session tests cover attach/merge/conflict behavior.
- [x] Cart quantity constraints and cart limits are enforced consistently. 2026-07-16 Phase 4: `StorefrontCartOptions` adds max lines/default max quantity/personalization limits; focused cart/OpenAPI/client tests passed 78/78.
- [x] Cart expiration policy is configurable and defaults safely. 2026-07-16 Phase 6: `StorefrontCartOptions.ExpirationDays` defaults to 30 and `StorefrontCartSessionServiceTests.CreateAsync_UsesConfiguredExpirationPolicy_WhenRequestDoesNotSpecifyExpiration` passed.
- [x] Expired active carts are marked expired and cannot be used. 2026-07-16 Phase 6: session service expiration tests passed; expired cart mutation returns conflict and changes state to `expired`.
- [x] Cart cleanup does not hard delete or corrupt active, merged, ordered, or another-store carts. 2026-07-16 Phase 6: `ExpireStaleActiveSessionsAsync_ExpiresOnlyMatchingActiveExpiredSessions` and batch-size cleanup tests passed.
- [x] Cart Core release gate passed. 2026-07-16 Phase 6: focused `StorefrontCartSessionServiceTests|StorefrontCartServiceTests|CommerceNodeStorefrontOpenApiContractTests|StorefrontCheckoutServiceTests|StorefrontV2ApiClientTests|CartCorePhase0InventoryTests` run passed 107/107.

## Store Resolution Hardening

- [x] CommerceNode Nginx runtime config has an explicit default/catch-all server returning 403 for unmatched hosts. 2026-07-15: `NginxRuntimeConfigTests` passed and `00-default-deny.conf` contains `default_server` plus `return 403`.
- [x] Run local Nginx smoke: `docker compose -f compose.commercenode.yml up -d commercenode-nginx`, `docker exec blazorshop-commercenode-nginx nginx -t`, then `curl.exe -i -H "Host: unknown.invalid" http://localhost:8088/` must return `403 Forbidden`. 2026-07-15: real Docker Nginx returned 403 for unmatched `Host` after runtime restart/reload; no fallback host served the request.
- [x] Verify at least one known generated store host still proxies to the intended Storefront container after the default/catch-all deny file is mounted. 2026-07-15: temporary `qa-storefront.local` server block proxied to a temporary upstream and returned 200 while unknown hosts still returned 403; temporary config/container were removed after verification.

2026-07-15 local API QA plan:

- [x] Start CommerceNode dependencies from `compose.commercenode.yml` and run CommerceNode API on `http://localhost:5180`. Result: PostgreSQL/Nginx/imgproxy were running; API started and applied 5 pending checkout/payment migrations.
- [x] Verify Storefront Swagger is reachable and contains scoped cart, checkout, payment-attempt, callback, and webhook contracts. Result: `/swagger/storefront/swagger.json` returned 200 and included `/cart/session`, `/cart`, `/cart/lines`, `/checkout/preview`, `/checkout/place-order`, `/payments/attempts/{attemptId}`, `/payments/provider-callback/{providerKey}`, and `/payments/webhooks/{providerKey}`.
- [x] Verify retired raw checkout route `POST /api/storefront/stores/default/cart/checkout` returns 404 or 405. Result: returned 404; raw checkout path was absent from Swagger.
- [x] Verify removed `api/internal/*` sample route returns 404. Result: `GET /api/internal/catalog/categories` returned 404.
- [x] Verify scoped Storefront catalog/payment/cart routes do not require node credentials or `X-Store-Key`. Result: categories and payment methods returned 200 without headers; `/cart/session` returned 200 without node credentials or `X-Store-Key`.
- [x] Record HTTP smoke results in `.gstack/qa-reports/qa-report-localhost-2026-07-15.md`.

## Startup Database Migration

- [x] Clean `CommerceNodeConnection` database is created/migrated by `BlazorShop.CommerceNode.API` startup when `CommerceNode:Database:MigrateOnStartup=true`. 2026-07-11: startup smoke passed against disposable DB `blazorshop_commerce_node_startup_qa_20260711`.
- [ ] Existing migrated Commerce Node database restarts without duplicate migration or seed side effects.
- [x] Startup migration logs context name, connection name, applied count, pending count, and pending migration names. 2026-07-11: verified in `.gstack/startup-migration-qa/commercenode-startup-migration.log`.
- [x] Startup migration logs do not expose passwords or raw connection strings. 2026-07-11: smoke assertion checked logs did not contain `Password=`.
- [ ] Invalid `CommerceNodeConnection` fails API startup when `CommerceNode:Database:FailStartupOnMigrationError=true`.
- [ ] `CommerceNode:Database:LogMigrationState=false` still runs migration without state log noise.
- [x] `CommerceNodeDbContext` startup migration never touches `ControlPlaneConnection` or `AppDbContext`. 2026-07-11: smoke used only `ConnectionStrings__CommerceNodeConnection`; architecture/code path resolves only `CommerceNodeDbContext`.
- [x] `CommerceTaskWorker` starts only after startup migration completes. 2026-07-11: migration runs before `app.Run()` and smoke reached `api/commerce/healthz` after startup completed.

## Credential Boundary

### `api/commerce/*`

- [x] Missing `X-Node-Key` / `X-Node-Secret` returns `success=false`.
- [x] Invalid node credential returns `success=false`.
- [x] Valid node credential allows `api/commerce/healthz`.
- [x] Valid node credential allows `api/commerce/admin/*`.

### Historical `api/internal/*` Pre-Removal Evidence

- [x] Historical: Internal catalog routes did not require node key before removal.
- [x] Historical: Internal SEO routes did not require node key before removal.
- [x] Historical: Internal auth create/login/refresh/logout did not require node key before removal.
- [x] Historical: Customer routes required JWT before removal:
  - [x] `api/internal/cart/checkout`
  - [x] `api/internal/cart/save-checkout`
  - [x] `api/internal/orders/confirm`
  - [x] `api/internal/orders/current-user`
  - [x] `api/internal/orders/current-user/items`

### `api/storefront/stores/{storeKey}/*`

- [x] Scoped catalog routes do not require node key. 2026-07-14: scoped categories smoke returned 200 without node headers.
- [ ] Scoped SEO routes do not require node key.
- [ ] Scoped auth register/login/refresh/logout do not require node key.
- [ ] Scoped Storefront routes do not require `X-Store-Key`.
- [ ] Customer routes require JWT:
  - [ ] `api/storefront/stores/{storeKey}/cart/save-checkout`
  - [ ] `api/storefront/stores/{storeKey}/orders/confirm`
  - [ ] `api/storefront/stores/{storeKey}/orders/current-user`
  - [ ] `api/storefront/stores/{storeKey}/orders/current-user/items`
- [x] Direct raw cart checkout route is retired from active Storefront API. 2026-07-14: `POST /api/storefront/stores/{storeKey}/cart/checkout` removed from controller, Storefront V2 client, and Storefront Swagger snapshot; session checkout uses `/checkout/preview` + `/checkout/place-order`.

## Seed Data

- [x] Create admin seed category through `api/commerce/admin/categories`.
- [x] Create admin seed product through `api/commerce/admin/products`.
- [x] Create product variant through `api/commerce/admin/products/{productId}/variants`.
- [x] Update product SEO through `api/commerce/admin/products/{id}/seo`.
- [x] Update category SEO through `api/commerce/admin/categories/{id}/seo`.
- [x] Update global SEO settings through `api/commerce/admin/seo/settings`.
- [x] Create SEO redirect through `api/commerce/admin/seo/redirects`.
- [x] Historical: registered Storefront customer through `api/internal/auth/create` before removal.
- [x] Historical: logged in Storefront customer through `api/internal/auth/login` before removal.

## Commerce Admin API Checklist

### Health

- [x] `GET /api/commerce/healthz`

### Categories

- [x] `GET /api/commerce/admin/categories`
- [x] `POST /api/commerce/admin/categories`
- [x] `GET /api/commerce/admin/categories/{id}`
- [x] `PUT /api/commerce/admin/categories/{id}`
- [x] `DELETE /api/commerce/admin/categories/{id}` with disposable category only.

### Products

- [x] `GET /api/commerce/admin/products`
- [x] `POST /api/commerce/admin/products`
- [x] `GET /api/commerce/admin/products/{id}`
- [x] `PUT /api/commerce/admin/products/{id}`
- [x] `DELETE /api/commerce/admin/products/{id}` with disposable product only.

### Product Variants

- [x] `GET /api/commerce/admin/products/{productId}/variants`
- [x] `POST /api/commerce/admin/products/{productId}/variants`
- [x] `PUT /api/commerce/admin/products/{productId}/variants/{variantId}`
- [x] `DELETE /api/commerce/admin/products/{productId}/variants/{variantId}` with disposable variant only.

### Variation Templates

- [x] CommerceNode API builds after Variation Template Foundation changes. 2026-07-10: `dotnet build BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/BlazorShop.CommerceNode.API.csproj --no-restore` passed.
- [x] ControlPlane API builds after product import proxy changes. 2026-07-10: `dotnet build BlazorShop.PresentationV2/BlazorShop.ControlPlane.API/BlazorShop.ControlPlane.API.csproj --no-restore` passed.
- [x] Apply `CommerceNodeVariationTemplateFoundation` migration to clean CommerceNode PostgreSQL on port `5434`. 2026-07-10: `dotnet ef database update --context CommerceNodeDbContext` applied pending migrations.
- [x] `GET /api/commerce/admin/variation-templates` returns list. 2026-07-10: list returned created QA templates.
- [x] `POST /api/commerce/admin/variation-templates` creates template. 2026-07-10: created `qa-template-20260710222346`.
- [x] Duplicate template slug in same store returns `success=false`. 2026-07-10: duplicate slug returned HTTP 409 envelope.
- [ ] Same template slug in another store is allowed.
- [x] `GET /api/commerce/admin/variation-templates/{id}` returns options/values. 2026-07-10: returned `Color -> Red`.
- [ ] `PUT /api/commerce/admin/variation-templates/{id}` updates name/slug/active state.
- [x] `POST /api/commerce/admin/variation-templates/{id}/options` creates option. 2026-07-10: fixed EF child-row state bug; `Color` option created.
- [ ] `PUT /api/commerce/admin/variation-templates/{id}/options/{optionId}` updates/disables option.
- [x] `POST /api/commerce/admin/variation-templates/{id}/options/{optionId}/values` creates value. 2026-07-10: fixed EF child-row state bug; `Red` and disabled `Blue` values created.
- [x] `PUT /api/commerce/admin/variation-templates/{id}/options/{optionId}/values/{valueId}` updates/disables value. 2026-07-10: `Red` disabled then re-enabled successfully.
- [x] Delete unreferenced variation template succeeds. 2026-07-10: disposable template deleted.
- [x] Delete referenced variation template fails. 2026-07-10: template referenced by product returned HTTP 409 envelope.
- [x] Create `CustomVariations` product without template fails. 2026-07-10: returned validation error.
- [x] Create `CustomVariations` product with active template succeeds. 2026-07-10: admin create succeeded with active template id.
- [x] Storefront product detail for `CustomVariations` returns active option/value `name` and `value` only. 2026-07-10: internal catalog product detail returned `Color -> Red`.
- [x] Disabled option/value is hidden from Storefront product detail. 2026-07-10: disabled `Blue` was not returned by Storefront detail.
- [ ] Cart/order accepts selected attributes for `CustomVariations`.
- [ ] Cart/order rejects more than 5 selected attributes.
- [ ] Cart/order stores selected attributes in `OrderLine.VariantAttributesJson`.
- [ ] Existing `ProductVariant` endpoints still work for `VariantInventory`.

### Product Media

- [~] `GET /api/commerce/admin/products/{productId}/media` returns an empty list before import. Existing QA DB already contained media rows after this run; list endpoint itself verified.
- [x] `POST /api/commerce/admin/products/{productId}/media/import` queues a valid public image URL. 2026-07-10: queued `picsum.photos` image.
- [x] `POST /api/commerce/admin/products/{productId}/media/import` rejects unsupported URL schemes. 2026-07-10: `ftp://` returned `success=false`.
- [x] `POST /api/commerce/admin/products/{productId}/media/import` blocks localhost/private IP source URLs. 2026-07-10: `127.0.0.1` source failed with safe private/local host message.
- [x] Media task transitions imported image to `stored`. 2026-07-10: retry after storage fix stored media `973caf94-14c9-4c12-9376-7101d17e061a`.
- [x] Failed media records a safe error message. 2026-07-10: failed source returned safe unsuccessful/private-host messages.
- [x] Primary stored media updates `Product.Image` to `/media/products/{mediaPublicId}`. 2026-07-10: admin product detail returned Product.Image media URL.
- [x] `GET /media/products/{mediaPublicId}` returns optimized image content. 2026-07-10: public resolver returned `image/webp` through imgproxy when store scope was provided.
- [x] `GET /media/products/{mediaPublicId}?w=320&fit=contain&format=webp` returns image content. 2026-07-10: returned 200, `image/webp`, immutable cache headers.
- [x] `GET /media/products/{mediaPublicId}?w=3000` rejects or clamps invalid dimensions so rendered output never exceeds `2000`. 2026-07-10: controller clamps to max `2000`.
- [x] Set primary media succeeds. 2026-07-10: imported primary became stored primary.
- [x] Retry failed media succeeds when the media is retryable. 2026-07-10: retry converted failed storage row to stored after fix.
- [ ] Delete primary media chooses next stored image or clears `Product.Image`.
- [x] Store-scoped media cannot be read from another store host. 2026-07-10: `X-Store-Key: other` returned 404 for default store media.

### Media Library

- [x] CommerceNode API builds after Media Library MVP backend changes. 2026-07-13: `dotnet build BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/BlazorShop.CommerceNode.API.csproj` passed.
- [x] `CommerceNodeMediaLibraryMvp` migration was generated for `CommerceNodeDbContext`. 2026-07-13: `dotnet ef migrations add CommerceNodeMediaLibraryMvp` succeeded after stopping locked local dev processes.
- [x] Apply `CommerceNodeMediaLibraryMvp` migration to local CommerceNode PostgreSQL on port `5434`. 2026-07-13: local CommerceNode startup migration applied; browser upload/list used the new table.
- [x] `GET /api/commerce/admin/media/assets?pageNumber=1&pageSize=25` returns paged media assets for current store. 2026-07-13: ControlPlane browser list loaded page 1 with one uploaded asset, then empty state after delete.
- [~] `POST /api/commerce/admin/media/assets` uploads jpg/png/webp/gif/ico up to 10MB. 2026-07-13: PNG upload path verified; remaining supported formats still pending.
- [x] Upload auto-generates `displayName`, `altText`, and `titleText` from file name. 2026-07-13: `summer-sale-banner.png` generated `Summer Sale Banner` for all three fields.
- [x] Upload rejects unsupported extensions and mismatched file signatures. 2026-07-13: `.txt` upload returned validation error through the visible ControlPlane page.
- [x] `PUT /api/commerce/admin/media/assets/{assetPublicId}` updates metadata and bumps datetime version. 2026-07-13: metadata save updated generated link version and drawer values.
- [ ] Blank display name is rejected.
- [ ] Blank alt text falls back to display name.
- [x] `POST /api/commerce/admin/media/assets/{assetPublicId}/replace` replaces original bytes while keeping the public id and canonical file name. 2026-07-13: replacement kept the same public id and `summer-sale-banner.png`.
- [x] `DELETE /api/commerce/admin/media/assets/{assetPublicId}` hard-deletes the row and asset directory. 2026-07-13: visible browser delete returned the grid to the empty state.
- [ ] `GET /media/assets/{assetPublicId}/{canonicalFileName}` serves original content with store scope.
- [ ] Wrong canonical filename redirects permanently to the canonical URL while preserving query.
- [~] Transform query supports `w`, `h`, `fit=cover|contain|inside`, `format=original|webp|jpg|png`, and `v`. 2026-07-13: `w=320&h=180&fit=cover&format=webp&v=...` returned `200 image/webp`; other fit/format combinations still pending.
- [ ] Transform query for gif/ico returns 400.
- [ ] Store A cannot read Store B media asset.

### Media Core

- [x] Media Core Phase 0 baseline inventory is captured before shared policy/storage changes. 2026-07-15: reviewed ProductMedia, CommerceMediaAsset, public controllers, Storefront media proxy, ControlPlane gateway routes, compose/imgproxy/Nginx config, query limits, size limits, and current public URL shapes in `Media Core.md`.
- [x] Product media route shape remains `/media/products/{mediaPublicId}` with `w`, `h`, `fit`, `format`, and `v` query support. 2026-07-15 Phase 0: documented current controller behavior before runtime changes.
- [x] Generic asset route shape remains `/media/assets/{assetPublicId}/{canonicalFileName}` with transform query support. 2026-07-15 Phase 0: documented current controller behavior before runtime changes.
- [x] Current product media and generic asset file-size limits are documented. 2026-07-15 Phase 0: product import/download and generic upload both default to `10MB`.
- [x] Media Core Phase 1 shared policy tests cover image type constants, MIME/signature validation, fit/format normalization, dimension clamping, and named presets. 2026-07-15: `MediaFilePolicyTests` and `MediaTransformPolicyTests` passed 20/20.
- [x] Product media and generic asset public controllers build after switching query normalization to shared policy. 2026-07-15 Media Core Phase 1: CommerceNode API build passed.
- [x] Existing image signature validator compatibility remains covered after moving logic behind shared policy. 2026-07-15 Media Core Phase 1: `ImageFileSignatureValidatorTests` focused run passed 3/3.
- [x] Media Core Phase 2 local storage provider tests cover path traversal prevention and preserved effective storage layout. 2026-07-15: `LocalMediaStorageProviderTests` passed with shared policy tests, focused run 25/25.
- [x] Product media import and public original rendering now resolve local paths through `IMediaStorageProvider` without changing URL shape or storage layout. 2026-07-15 Media Core Phase 2: CommerceNode API build passed.
- [x] Generic media asset upload/replace/delete and public original rendering now resolve local paths through `IMediaStorageProvider` without changing URL shape or storage layout. 2026-07-15 Media Core Phase 2: CommerceNode API build passed.
- [x] Media Core Phase 3: delivery tests cover versioned/unversioned cache headers, `nosniff`, ETag, and Storefront proxy header copying. 2026-07-15: `MediaDeliveryHardeningTests` passed in focused media run.
- [x] Media Core Phase 3 URL builder tests cover product presets, generic asset presets, configured-public-base absolute URLs, and unsafe absolute URL rejection. 2026-07-15: `MediaUrlBuilderTests` passed in focused media run.
- [x] CommerceNode API builds after Media Core Phase 3 delivery hardening. 2026-07-15: `dotnet build BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/BlazorShop.CommerceNode.API.csproj --no-restore` passed.
- [~] Media Core placeholder/default image policy remains deferred until a real product/category/page placeholder asset is selected. 2026-07-15: repo inspection found no semantic placeholder/no-image asset; Phase 3 avoids introducing broken media URLs.
- [x] Media Core Phase 4 product media service tests cover order-change catalog invalidation, delete invalidation, primary delete fallback/clear behavior, and admin alt text preservation. 2026-07-15: `ProductMediaServiceTests` passed 5/5 and focused media run passed 40/40.
- [x] CommerceNode API builds after Media Core Phase 4 product media hardening. 2026-07-15: `dotnet build BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/BlazorShop.CommerceNode.API.csproj --no-restore` passed.
- [x] Media Core Phase 5 category media assignment validates current-store category and asset, syncs `Category.Image`, clears assignment/image, and invalidates catalog cache. 2026-07-15: `CategoryMediaServiceTests` passed in focused run.
- [x] Media Core Phase 5 prevents deleting a generic asset while it is assigned to a category. 2026-07-15: `CommerceMediaAssetService.DeleteAsync` checks `CategoryMediaAssignments` and returns conflict before deleting.
- [x] Media Core Phase 5 Commerce Admin category media endpoints have stable OpenAPI operation IDs and response schemas. 2026-07-15: `CommerceNodeAdminStoreOpenApiMetadataTests` passed with `CommerceCategoryMedia_GetPrimary`, `CommerceCategoryMedia_SetPrimary`, and `CommerceCategoryMedia_ClearPrimary`.
- [x] CommerceNode API builds after Media Core Phase 5 category media assignment API and migration. 2026-07-15: CommerceNode API build passed.
- [x] Media Core Phase 6 generic media assets default to `content` usage and validate allowlisted usage values. 2026-07-15: `CommerceMediaAssetUsageTypeTests` covered normalization, list filtering, metadata update, and invalid usage rejection.
- [x] CommerceNode API builds after Media Core Phase 6 media asset usage classification migration/API changes. 2026-07-15: CommerceNode API build passed.
- [x] Media Core Phase 7 assigned generic media asset delete is blocked instead of silently breaking category image. 2026-07-15: `CommerceMediaAssetUsageTypeTests.DeleteAsync_WhenAssetIsAssignedToCategory_ReturnsConflict` passed.

### Product Import

- [x] CommerceNode API builds after Product Import Task changes. 2026-07-10: `dotnet build BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/BlazorShop.CommerceNode.API.csproj --no-restore` passed.
- [x] ControlPlane API product import proxy builds. 2026-07-10: `dotnet build BlazorShop.PresentationV2/BlazorShop.ControlPlane.API/BlazorShop.ControlPlane.API.csproj --no-restore` passed.
- [x] Apply `CommerceNodeProductImport` migration to clean CommerceNode PostgreSQL on port `5434`. 2026-07-10: migration applied by `dotnet ef database update`.
- [x] Apply `CommerceNodeNullableProductCategory` migration to clean CommerceNode PostgreSQL on port `5434`. 2026-07-10: migration applied by `dotnet ef database update`.
- [x] `POST /api/commerce/admin/products/import` uploads valid CSV in `create_only` mode. 2026-07-10: job `8c004bac-546a-40bb-b8aa-41c478988e03` completed with 2 created rows.
- [x] `POST /api/commerce/admin/products/import` uploads valid CSV in `upsert` mode. 2026-07-10: job `014eb127-9827-410c-af5a-c9a8756d6e23` completed with 1 updated row.
- [x] `GET /api/commerce/admin/products/imports` lists import jobs. 2026-07-10: returned latest import jobs with counts and statuses.
- [x] `GET /api/commerce/admin/products/imports/{jobPublicId}` returns job detail. 2026-07-10: detail polling used until jobs reached terminal status.
- [x] `GET /api/commerce/admin/products/imports/{jobPublicId}/rows` returns row results. 2026-07-10: row list returned status/action/ErrorJson/media status.
- [x] Duplicate same file hash returns existing job and does not enqueue another task. 2026-07-10: duplicate upload returned the same job public id.
- [x] Same file cannot be imported again for same store/mode. 2026-07-10: same file hash/mode/store returned existing job.
- [x] Missing required CSV header fails the async import job. 2026-07-10: upload accepted the file, worker marked job `Failed` with missing header details.
- [x] Missing SKU row writes `sku` column error. 2026-07-10: row ErrorJson contained `sku`.
- [x] Missing name on create writes `name` column error. 2026-07-10: row ErrorJson contained `name`.
- [x] Missing description on create writes `description` column error. 2026-07-10: row ErrorJson contained `description`.
- [x] Missing price on create writes `price` column error. 2026-07-10: row ErrorJson contained `price`.
- [ ] Duplicate SKU in `create_only` writes row error.
- [x] `upsert` blank cells do not overwrite existing values. 2026-07-10: upsert blank `short_description` kept existing `Import simple`.
- [x] `__clear__` clears allowed nullable fields. 2026-07-10: upsert `compare_price=__clear__` returned product `comparePrice=null`.
- [ ] Create with blank `category_slug` succeeds and leaves product uncategorized.
- [x] Update with blank `category_slug` keeps existing category. 2026-07-10: upsert blank category kept `t-shirts`.
- [x] Unknown `category_slug` writes row error. 2026-07-10: row ErrorJson contained `category_slug`.
- [x] `CustomVariations` without `variation_template_slug` writes row error. 2026-07-10: row ErrorJson contained `variation_template_slug`.
- [ ] Unknown/inactive `variation_template_slug` writes row error.
- [x] Valid `variation_template_slug` sets product template reference. 2026-07-10: Storefront detail for imported custom product returned the expected `variationTemplateId`.
- [ ] `VariantInventory` import does not create `ProductVariant` rows.
- [x] `image_urls` with more than 10 URLs writes row error. 2026-07-10: row ErrorJson contained `image_urls`.
- [x] Valid `image_urls` queues one `product.media.import` task per product row. 2026-07-10: fixed background store scope; import row returned `mediaStatus=Queued`, media task succeeded.
- [x] Product import completes with `CompletedWithErrors` when some rows fail. 2026-07-10: row-errors job completed with 6 failed rows.
- [x] CommerceTask result contains product import summary counts. 2026-07-10: import task detail/list exposed created/updated/failed/media counts through job and task correlation.
- [x] ProductImportRows contain `ErrorJson` with column names. 2026-07-10: row ErrorJson included `sku`, `name`, `description`, `price`, `category_slug`, `variation_template_slug`, and `image_urls`.
- [ ] ControlPlane upload route `POST /api/control-plane/stores/{storePublicId}/catalog/products/import` proxies through ControlPlane API only.
- [ ] ControlPlane import list/detail/rows routes proxy through ControlPlane API only.

### Inventory

- [x] `GET /api/commerce/admin/inventory`
- [x] `PUT /api/commerce/admin/inventory/products/{productId}`
- [x] `PUT /api/commerce/admin/inventory/variants/{variantId}`

### SEO

- [x] `GET /api/commerce/admin/products/{id}/seo`
- [x] `PUT /api/commerce/admin/products/{id}/seo`
- [x] `GET /api/commerce/admin/categories/{id}/seo`
- [x] `PUT /api/commerce/admin/categories/{id}/seo`
- [x] `GET /api/commerce/admin/seo/settings`
- [x] `PUT /api/commerce/admin/seo/settings`
- [x] `GET /api/commerce/admin/seo/redirects`
- [x] `POST /api/commerce/admin/seo/redirects`
- [x] `GET /api/commerce/admin/seo/redirects/{id}`
- [x] `PUT /api/commerce/admin/seo/redirects/{id}`
- [x] `POST /api/commerce/admin/seo/redirects/{id}/deactivate`
- [x] `DELETE /api/commerce/admin/seo/redirects/{id}` with disposable redirect only.

### Orders

- [x] `GET /api/commerce/admin/orders`
- [x] `GET /api/commerce/admin/orders/{id}` after creating an order through Storefront flow.
- [x] `PUT /api/commerce/admin/orders/{id}/tracking`
- [x] `PUT /api/commerce/admin/orders/{id}/shipping-status`
- [x] `PUT /api/commerce/admin/orders/{id}/admin-note`

### Shipments

- [x] CommerceNode API builds after shipment foundation changes. 2026-07-10: `dotnet build BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/BlazorShop.CommerceNode.API.csproj --no-restore` passed.
- [ ] Apply `CommerceNodeShipments` migration to clean CommerceNode PostgreSQL on port `5434`.
- [ ] `GET /api/commerce/admin/orders/{orderId}/shipment` returns `success=false`/not found before shipment exists.
- [ ] `PUT /api/commerce/admin/orders/{orderId}/shipment` creates a shipment for an existing store-scoped order.
- [ ] `GET /api/commerce/admin/orders/{orderId}/shipment` returns created shipment data.
- [ ] Second `PUT /api/commerce/admin/orders/{orderId}/shipment` updates/replaces the existing shipment instead of creating a duplicate.
- [ ] Database unique index `(StoreId, OrderId)` prevents duplicate shipment rows.
- [ ] Shipment create/update syncs order fields:
  - [ ] `ShippingStatus = Shipped`
  - [ ] `ShippedOn = ShipDate`
  - [ ] `ShippingCarrier = CarrierName`
  - [ ] `TrackingNumber = TrackingNumber`
  - [ ] `TrackingUrl = TrackingUrl`
  - [ ] `LastTrackingUpdate` is updated.
- [ ] Shipment request with empty `CarrierName` returns `success=false`.
- [ ] Shipment request with empty `TrackingNumber` returns `success=false`.
- [ ] Shipment request with over-length text fields returns `success=false`.
- [ ] Store isolation: a request scoped to another store cannot read the shipment.
- [ ] Store isolation: a request scoped to another store cannot update the shipment.
- [ ] Audit log includes `Order.ShipmentUpserted` after shipment upsert.
- [ ] Existing `PUT /api/commerce/admin/orders/{orderId}/tracking` still works after shipment migration.
- [ ] Existing `PUT /api/commerce/admin/orders/{orderId}/shipping-status` still works after shipment migration.
- [ ] Storefront order detail still reads shipping info from existing order fields.
- [x] No Storefront shipment endpoint is exposed under removed `api/internal/*`.

### Settings, Audit, Metrics

- [x] `GET /api/commerce/admin/settings`
- [x] `PUT /api/commerce/admin/settings/store`
- [x] `PUT /api/commerce/admin/settings/orders`
- [x] `PUT /api/commerce/admin/settings/notifications`
- [x] `GET /api/commerce/admin/audit`
- [x] `GET /api/commerce/admin/audit/{id}` if audit entries exist.
- [x] `GET /api/commerce/admin/metrics/sales`
- [x] `GET /api/commerce/admin/metrics/traffic`

## Historical Storefront Internal API Checklist

This section records pre-removal `api/internal/*` QA evidence. Do not use it as the current CommerceNode Storefront API target; use the Storefront Scoped API Checklist instead.

### Catalog

- [x] `GET /api/internal/catalog/categories`
- [x] `GET /api/internal/catalog/categories/tree` returns parent/child tree. 2026-07-10: endpoint added; admin tree smoke verified same hierarchy.
- [x] `GET /api/internal/catalog/categories/{id}`
- [x] `GET /api/internal/catalog/categories/slug/{slug}`
- [x] `GET /api/internal/catalog/categories/{categoryId}/products`
- [x] `GET /api/internal/catalog/products`
- [x] `GET /api/internal/catalog/products?minPrice=&maxPrice=&inStock=&sortBy=DisplayOrder` filters expanded catalog. 2026-07-10: smoke returned filtered product page.
- [x] `GET /api/internal/catalog/products/{id}`
- [x] `GET /api/internal/catalog/products/slug/{slug}`
- [x] `GET /api/internal/catalog/sitemap`

## Storefront Scoped API Checklist

### Catalog

- [ ] `GET /api/storefront/stores/{storeKey}/catalog/categories`
- [ ] `GET /api/storefront/stores/{storeKey}/catalog/categories/tree`
- [ ] `GET /api/storefront/stores/{storeKey}/catalog/categories/{id}`
- [ ] `GET /api/storefront/stores/{storeKey}/catalog/categories/slug/{slug}`
- [ ] `GET /api/storefront/stores/{storeKey}/catalog/categories/{categoryId}/products`
- [ ] `GET /api/storefront/stores/{storeKey}/catalog/products`
- [ ] `GET /api/storefront/stores/{storeKey}/catalog/products?categorySlug=t-shirts&searchTerm=shirt`
- [ ] `GET /api/storefront/stores/{storeKey}/catalog/products/{id}`
- [ ] `GET /api/storefront/stores/{storeKey}/catalog/products/slug/{slug}`
- [ ] `GET /api/storefront/stores/{storeKey}/catalog/sitemap`

### Store, SEO, Pages

- [ ] `GET /api/storefront/stores/{storeKey}/store/current`
- [ ] `GET /api/storefront/stores/{storeKey}/store/maintenance`
- [ ] `GET /api/storefront/stores/{storeKey}/seo/settings`
- [ ] `GET /api/storefront/stores/{storeKey}/seo/redirects/resolve?path=/legacy-qa-product`
- [ ] `GET /api/storefront/stores/{storeKey}/pages/{slug}`

### Auth, Payments, Newsletter, Recommendations

- [ ] `POST /api/storefront/stores/{storeKey}/auth/register`
- [ ] `POST /api/storefront/stores/{storeKey}/auth/login`
- [ ] `POST /api/storefront/stores/{storeKey}/auth/refresh-token` with login cookie.
- [ ] `POST /api/storefront/stores/{storeKey}/auth/logout` revokes refresh cookie.
- [ ] `GET /api/storefront/stores/{storeKey}/payments/methods`
- [ ] `POST /api/storefront/stores/{storeKey}/newsletter/subscribe`
- [ ] `GET /api/storefront/stores/{storeKey}/recommendations/products/{productId}`

### Cart And Orders

- [n/a] `POST /api/storefront/stores/{storeKey}/cart/checkout` retired after server-cart checkout cutover. 2026-07-14: use `/checkout/preview` and `/checkout/place-order`.
- [ ] `POST /api/storefront/stores/{storeKey}/cart/save-checkout`
- [ ] `POST /api/storefront/stores/{storeKey}/orders/confirm`
- [ ] `GET /api/storefront/stores/{storeKey}/orders/current-user`
- [ ] `GET /api/storefront/stores/{storeKey}/orders/current-user/items`

### Catalog Search MVP

Historical `api/internal/*` checks in this subsection should be ported to scoped `api/storefront/stores/{storeKey}/*` checks when catalog search QA is rerun.

- [x] CommerceNode API builds after catalog search/cache changes. 2026-07-10: `dotnet build BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/BlazorShop.CommerceNode.API.csproj --no-restore` passed.
- [x] Legacy `BlazorShop.API` still builds after optional catalog cache dependencies were added. 2026-07-10: `dotnet build BlazorShop.Presentation/BlazorShop.API/BlazorShop.API.csproj --no-restore` passed with existing `Microsoft.OpenApi` advisory warning.
- [~] Full test suite attempted after CommerceNode catalog search/cache changes. 2026-07-10: `dotnet test BlazorShop.Tests/BlazorShop.Tests.csproj --no-restore` failed 11/512. Failures were in migration model consistency, existing Product/Category delete unit expectations, CartService tests, and sitemap timestamp tests; CommerceNode API and StorefrontV2 builds passed.
- [x] Storefront published catalog search uses PostgreSQL FTS over `Products.Name`, not `Contains` over SKU/description. 2026-07-10: code review verified `CommerceNodeProductReadRepository.GetPublishedCatalogPageAsync`.
- [x] Migration adds `ix_products_name_fts_simple` GIN expression index. 2026-07-10: migration `20260710120000_CommerceNodeCatalogSearchMvp` added.
- [ ] Apply `CommerceNodeCatalogSearchMvp` migration to local PostgreSQL on port `5434`.
- [ ] `GET /api/internal/catalog/products?searchTerm=shirt` returns title matches.
- [ ] `GET /api/internal/catalog/products?searchTerm=<sku-only-term>` does not match SKU-only text.
- [ ] `GET /api/internal/catalog/products?searchTerm=<description-only-term>` does not match description-only text.
- [ ] `GET /api/internal/catalog/products?categorySlug=t-shirts` returns category-scoped products.
- [ ] `GET /api/internal/catalog/products?categorySlug=apparel` includes child category products.
- [ ] `GET /api/internal/catalog/products?categorySlug=missing-category` returns `success=true` with empty page data.
- [ ] `GET /api/internal/catalog/products?categorySlug=t-shirts&searchTerm=shirt` combines category and title search.
- [ ] Empty `searchTerm` does not use FTS and returns browse listing.
- [ ] `pageNumber` greater than 10 is clamped by backend.
- [ ] `TotalCount` is capped to `PageSize * 10`.
- [ ] Catalog query cache returns stable repeated results.
- [ ] Product create/update/delete invalidates store catalog cache.
- [ ] Category create/update/delete invalidates store catalog cache.
- [ ] Variant create/update/delete invalidates store catalog cache.
- [ ] Inventory stock updates invalidate store catalog cache.
- [ ] Primary product media change invalidates store catalog cache.

### Catalog Expansion

- [x] Development seeder creates `default` store if missing. 2026-07-10: verified in DB.
- [x] Development seeder creates parent category `Apparel`. 2026-07-10: seeder added.
- [x] Development seeder creates child category `T-Shirts`. 2026-07-10: seeder added.
- [x] Development seeder creates product `QA-TSHIRT` with SKU, short/full description, compare price, display order. 2026-07-10: verified in DB.
- [x] Development seeder creates variant `Color=Red, Size=M`. 2026-07-10: verified in DB.
- [x] Development seeder creates variant `Color=Red, Size=XL`. 2026-07-10: verified in DB.
- [x] Development seeder creates variant `Color=Black, Size=M` with zero stock. 2026-07-10: verified in DB.
- [x] Development seeder creates low-stock product `QA-LOW-STOCK`. 2026-07-10: verified in DB.
- [x] Development seeder creates sample order `QA-CATALOG-SNAPSHOT` with product/variant snapshot fields. 2026-07-10: verified in DB.
- [x] `GET /api/commerce/admin/products/query` searches by SKU. 2026-07-10: `searchTerm=QA-TSHIRT` returned seeded product.
- [x] `GET /api/commerce/admin/categories/tree` returns category hierarchy. 2026-07-10: returned `Apparel -> T-Shirts`.
- [ ] Duplicate variant combination is rejected.
- [ ] Second default variant is rejected.
- [ ] Checkout with out-of-stock variant is rejected.
- [ ] Successful order deducts product/variant stock.
- [ ] Admin order detail prefers order line snapshot fields.

### SEO

- [x] `GET /api/internal/seo/settings`
- [x] `GET /api/internal/seo/redirects/resolve?path=/legacy-qa-product`

### Storefront Pages

- [x] CommerceNode API builds after StorefrontPage schema/service/API changes. 2026-07-11: `dotnet build BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/BlazorShop.CommerceNode.API.csproj --no-restore` passed.
- [x] Apply `CommerceNodeStorefrontPage` migration to CommerceNode PostgreSQL on port `5434`. 2026-07-12: `run-v2-local.ps1` startup migration applied `20260711155908_CommerceNodeStorefrontPage` to local `blazorshop_commerce_node_v2_local`; `storefront_page` table exists.
- [x] `GET /api/commerce/admin/pages?pageNumber=1&pageSize=25` returns paged response. 2026-07-12: direct API with node credentials returned `success=true`, `pageNumber=1`, `pageSize=25`, and `totalPages=1`.
- [x] Admin page list search matches title. 2026-07-12: `search=Dynamic` returned `qa-dynamic-page-20260712034014`.
- [x] Admin page list search matches slug. 2026-07-12: `search=qa-dynamic-page` returned `qa-dynamic-page-20260712034014`.
- [x] Status filter `all` includes draft and published non-archived pages. 2026-07-12: ControlPlane/CommerceNode list showed both `QA Dynamic Page 20260712034014` and `QA Draft Page 20260712034014`.
- [x] Status filter `published` includes published only. 2026-07-12: CommerceNode `status=published` returned the published page and excluded the draft page.
- [x] Status filter `draft` includes draft only. 2026-07-12: CommerceNode `status=draft` returned the draft page and excluded the published page.
- [x] `POST /api/commerce/admin/pages` creates draft page by default. 2026-07-12: direct API created `qa-draft-page-20260712034014` with `isPublished=false`.
- [ ] Create page requires slug.
- [ ] Slug is normalized before save.
- [ ] Duplicate slug in same store is rejected.
- [ ] Duplicate slug is rejected even if old page is archived.
- [x] Dangerous HTML `<script>` is rejected. 2026-07-12: direct API returned 400 with `success=false` and message `Page body HTML contains a disallowed tag.`
- [ ] Dangerous HTML `javascript:` is rejected.
- [ ] Dangerous inline event such as `onerror=` is rejected.
- [ ] External image URL in `<img src>` is rejected.
- [ ] Local image URL in `<img src="/media/...">` is accepted.
- [ ] External HTTPS link in `<a href>` is accepted.
- [ ] Body above 100 KB is rejected.
- [x] Draft page is not returned from `GET /api/internal/pages/{slug}`. 2026-07-12: `GET /api/internal/pages/qa-draft-page-20260712034014` with `X-Store-Key=default` returned 404.
- [ ] Archived page is not returned from `GET /api/internal/pages/{slug}`.
- [x] Published page is returned from `GET /api/internal/pages/{slug}`. 2026-07-12: `GET /api/internal/pages/qa-dynamic-page-20260712034014` returned title, intro, body HTML, and SEO data.
- [ ] Archive hides page from admin list.
- [ ] Archive reserves slug.
- [x] Sitemap entries include only published pages with `include_in_sitemap=true`. 2026-07-12: `GET /api/internal/catalog/sitemap` included `qa-dynamic-page-20260712034014` and Storefront `/sitemap.xml` excluded `qa-draft-page-20260712034014`.
- [ ] Store A cannot read Store B page by slug.

### Auth

- [x] `POST /api/internal/auth/create`
- [x] `POST /api/internal/auth/login`
- [x] Login wrong password returns `success=false`.
- [x] `POST /api/internal/auth/refresh-token` with login cookie.
- [x] `POST /api/internal/auth/change-password` with JWT.
- [x] `POST /api/internal/auth/update-profile` with JWT.
- [x] `POST /api/internal/auth/logout` revokes refresh cookie.

### Payments, Newsletter, Recommendations

- [x] `GET /api/internal/payments/methods`
- [x] `GET /api/internal/recommendations/products/{productId}`
- [x] `POST /api/internal/newsletter/subscribe`
- [x] Duplicate newsletter subscription returns a stable response.

### Cart And Orders

- [x] Unauthenticated cart/order routes reject missing JWT.
- [x] `POST /api/internal/cart/checkout` with Cash on Delivery.
- [x] `POST /api/internal/cart/save-checkout`.
- [x] `POST /api/internal/orders/confirm`.
- [x] `GET /api/internal/orders/current-user`.
- [x] `GET /api/internal/orders/current-user/items`.
- [x] Admin order list sees the created order.

## Deferred/Manual Checks

- [x] `POST /api/commerce/admin/media/images` with multipart image upload. 2026-07-09: curl multipart upload returned `success=true`; uploaded PNG was reachable under `/uploads`.
- [n/a] PayPal capture redirect, because current PayPal service is a stub in this MVP.
- [n/a] Email delivery for newsletter/bank transfer, because SMTP is not configured for local MVP QA.

## QA Notes

- Use `X-Node-Key: dev-node`.
- Use `X-Node-Secret: dev-node-secret`.
- Current customer JWT comes from scoped `api/storefront/stores/{storeKey}/auth/login`; historical Internal QA used `api/internal/auth/login` response `data.token`.
- Refresh token is stored in `Set-Cookie` from login.
- Local HTTP clients may not resend the refresh cookie automatically because it is `Secure=true`; QA verified refresh by replaying the `Set-Cookie` value as a `Cookie` header.
- Missing JWT on `[Authorize]` Storefront routes returns HTTP `401` with `CommerceNodeApiErrorResponse` code `auth.unauthenticated`.
- Scoped cart save-checkout and order confirm routes expect a top-level JSON array. When using PowerShell, send raw JSON for single-item arrays to avoid `ConvertTo-Json` collapsing the array.
- Historical `api/internal/cart/save-checkout` also required a `userId` field in the request body even though the authenticated customer was resolved from JWT; verify whether the scoped contract still needs cleanup before changing behavior.
- Scoped recommendations route requires at least one related published product; a single product in a category correctly returns a not-found response.

## Fixes Applied During QA

- Fixed `GET /api/commerce/admin/products/{id}` serialization cycle by preventing `Category.Products` from being mapped back into `GetProduct.Category`.
- Fixed Commerce Node transaction execution with PostgreSQL retry strategy by wrapping manual transactions in `Database.CreateExecutionStrategy()`.
- Fixed Variation Template option/value creation returning HTTP 500 by marking new child rows as `Added` before `SaveChanges`.
- Fixed Product Import media queueing from background worker by adding store-scoped media import, avoiding dependency on HTTP `X-Store-Key`.

## Verification Commands

- `docker compose -f compose.commercenode.yml up -d`
- `dotnet run --project BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/BlazorShop.CommerceNode.API.csproj --urls http://localhost:5180`
- `dotnet test BlazorShop.sln`

CommerceNode migrations are applied by API startup when `CommerceNode:Database:MigrateOnStartup=true`. Use `dotnet ef database update` only as a manual diagnostic fallback, not as the normal V2 run path.

Latest ProductMedia QA result: 2026-07-10 CommerceNode API smoke passed for import queue, retry, worker storage, Product.Image sync, public imgproxy rendering, invalid scheme rejection, private/local source blocking, and cross-store 404. Fixed EF projection and temp-file length bugs found during QA.

Latest test result: 2026-07-09 full solution test passed: 485 passed, 10 skipped. Independent API smoke passed for ControlPlane -> CommerceNode health probe, Commerce admin catalog/media, Storefront internal auth/cart/order, and admin order visibility.

Latest Storefront API contract foundation result: 2026-07-14 CommerceNode API build passed, Storefront V2 build passed, CommerceNode OpenAPI contract tests passed, CartService contract tests passed, legacy CartController tests passed, and Storefront V2 API client tests passed. Known warnings were existing NuGet vulnerability warnings and Browserslist freshness notice.

Latest startup migration QA result: 2026-07-11 CommerceNode API build passed, `run-v2-local.ps1 -DryRun` passed, and startup migration created/migrated disposable DB `blazorshop_commerce_node_startup_qa_20260711` with safe migration logs. Failure-policy and restart-idempotency checks remain open.

## Checkout And Payment Foundation

- [x] Commerce currency admin API exposes safe exchange-rate provider status without raw config/secrets. 2026-07-15: `StoreCurrencyExchangeRateProviderServiceTests.GetProvidersAsync_ReturnsSafeProviderStatus` passed.
- [x] Configuration exchange-rate provider fetches rates into `store_currency_exchange_rates` with `provider_key=configuration` and `is_manual=false`. 2026-07-15: focused provider service test passed.
- [x] Provider stale-rate handling rejects stale configured rates without persisting rows. 2026-07-15: focused provider service test passed.
- [x] Manual exchange-rate upsert remains available and provider-fetched rows do not replace manual provider rows. 2026-07-15: existing `StoreCurrencyExchangeRateServiceTests` passed with Phase 8 focused run.
- [ ] API smoke lists exchange-rate providers via `GET /api/commerce/admin/currencies/exchange-rate-providers`.
- [ ] API smoke fetches configuration provider rates via `POST /api/commerce/admin/currencies/exchange-rates/fetch` against PostgreSQL.
- [x] Converted cart line persists base currency, base unit price, exchange rate, provider/source, and rate timestamp snapshot. 2026-07-15: `StorefrontCartServiceTests` focused run passed.
- [x] Converted checkout/order/payment persists auditable base and working currency snapshots. 2026-07-15: `StorefrontCheckoutServiceTests` focused run passed.
- [x] `Order.TotalAmount` and `PaymentAttempt.Amount` remain charged working-currency amounts for converted orders. 2026-07-15: focused checkout test asserted both are `EUR 9.00` while base total is `USD 10.00`.
- [x] CommerceNode EF model contains nullable snapshot columns for cart lines, checkout sessions, orders, order lines, and payment attempts. 2026-07-15: `CommerceNodeCurrencyRateSnapshots` migration generated and `CommerceNodeDbContextModelTests` focused run passed.
- [ ] API smoke creates a converted order against PostgreSQL and verifies persisted snapshot columns with SQL.
- [ ] Admin order API returns conversion snapshot fields for converted orders.
- [x] CommerceNode API builds after checkout/payment foundation changes. 2026-07-13: `dotnet build BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/BlazorShop.CommerceNode.API.csproj --no-restore` passed.
- [x] `PaymentMethods` seed contains `cod`, `stripe`, and `paypal`. 2026-07-13: ControlPlane payment admin page loaded all three seeded methods for default store.
- [x] `bank_transfer` is not returned by Storefront payment methods. 2026-07-13: `GET /api/internal/payments/methods` returned COD only.
- [x] Default store has `cod` enabled and `stripe/paypal` disabled. 2026-07-13: payment admin page showed COD checked, Stripe/PayPal unchecked.
- [x] `GET /api/internal/payments/methods` returns only enabled methods for current store. 2026-07-13: request with `X-Store-Key=default` returned COD only.
- [x] `POST /api/internal/cart/checkout` with COD creates an order. 2026-07-13: visible Storefront checkout created `ORD-20260713-6672B965`.
- [x] Created COD order has `order_status=processing`. 2026-07-13: order list showed `processing` immediately after checkout before completion.
- [x] Created COD order has `payment_status=paid`. 2026-07-13: order list and detail API showed `paid`.
- [x] Created COD order has `payment_method_key=cod`. 2026-07-13: order detail API and DB query showed `cod`.
- [x] Created COD order has `payment_at`. 2026-07-13: order detail API and DB query showed non-null `payment_at`.
- [x] Created COD order has `payment_metadata_json`. 2026-07-13: DB query showed COD handler metadata JSON.
- [x] Created COD order has customer snapshot fields. 2026-07-13: order detail API returned customer name/email snapshot.
- [x] Created COD order has shipping address snapshot fields. 2026-07-13: order detail API and DB query returned shipping name/email/address fields.
- [x] Disabled `stripe` checkout request returns `success=false`. 2026-07-13: direct API checkout with `paymentMethodKey=stripe` returned `success=false`.
- [x] Unknown payment method key returns `success=false`. 2026-07-13: direct API checkout with `paymentMethodKey=unknown` returned `success=false`.
- [ ] Checkout creates customer when email does not exist.
- [ ] Checkout attaches existing customer when email exists.
- [x] Admin order detail returns payment fields. 2026-07-13: order detail API returned payment status, method, payment date, and completed date.
- [x] Admin mark complete succeeds for paid/shipped order. 2026-07-13: ControlPlane Orders drawer updated shipping to `shipped` and marked order complete.
- [ ] Admin mark complete rejects unpaid order.
- [ ] Store isolation blocks another store from reading/completing the order.
- [ ] Audit log includes `Order.Completed`.

## Catalog Structure Core

- [x] Category create/update contract carries nullable `description` without replacing SEO metadata. 2026-07-16: `CategoryServiceTests.AddAsync_NormalizesDescriptionBeforePersisting` and `UpdateAsync_NormalizesBlankDescriptionToNull` passed.
- [x] Storefront category response schema includes nullable `description`. 2026-07-16: `CommerceNodeStorefrontOpenApiContractTests` passed after snapshot update.
- [x] CommerceNode migration adds only nullable `Categories.Description`. 2026-07-16: `CommerceNodeCategoryDescription` migration generated and `CommerceNodeDbContextModelTests` passed.
- [x] Storefront category page response exposes non-null `breadcrumbs`, `products`, `directProductCount`, and `descendantProductCount`. 2026-07-16: `CommerceNodeStorefrontOpenApiContractTests` passed 23/23 after snapshot update.
- [x] Category breadcrumb projection is ordered root-to-current and excludes unpublished category branches. 2026-07-16: `PublicCatalogServiceTests.GetPublishedCategoryPageBySlugAsync_AddsBreadcrumbsAndProductCounts` passed.
- [x] Storefront product query has explicit `includeSubcategories` behavior for both category slug and category id filters. 2026-07-16: `CommerceNodeProductStoreScopeTests` covered direct-only default plus descendant slug/id queries.
- [x] Category product counts exclude draft products, unpublished categories, and products from another store. 2026-07-16: `CountPublishedProductsByCategoryIdsAsync_ExcludesHiddenCategoriesAndOtherStores` passed.
- [x] Storefront Swagger snapshot includes `includeSubcategories` query metadata and category breadcrumb/count schemas. 2026-07-16: snapshot refreshed and contract tests passed.
- [x] Product availability migration adds nullable start/end UTC fields without changing existing published semantics. 2026-07-16: `CommerceNodeProductAvailabilityWindow` migration generated and `CommerceNodeDbContextModelTests` passed.
- [x] Public catalog/detail/sitemap/count queries exclude scheduled, expired, archived, draft, hidden-category, and other-store products. 2026-07-16: `CommerceNodeProductStoreScopeTests` passed in focused Phase 4 run.
- [x] Cart and checkout server-side validation enforce product availability windows. 2026-07-16: `StorefrontCartServiceTests.AddLineAsync_RejectsScheduledProduct` passed; checkout/payment attempt path uses matching start/end-window predicate.
- [x] Product import accepts optional `available_start_utc` and `available_end_utc`, validates ordering, and keeps old CSV headers compatible. 2026-07-16: `ProductImportCsvParserTests` and ControlPlane template test passed.
- [x] Product identity migration adds nullable GTIN/barcode/MPN/condition/weight/dimensions fields. 2026-07-16 Phase 5: `CommerceNodeProductIdentityFields` migration generated and `CommerceNodeDbContextModelTests` passed.
- [x] Product identity validation rejects invalid condition and negative dimensions while preserving optional create/update compatibility. 2026-07-16 Phase 5: `ProductServiceTests` focused run passed.
- [x] Product import accepts optional identity/dimension columns and keeps previous CSV shape compatible. 2026-07-16 Phase 5: `ProductImportCsvParserTests` and ControlPlane template test passed.
- [x] Product variant service rejects duplicate SKU within the same product and duplicate default variants. 2026-07-16 Phase 6: `ProductVariantServiceTests` passed.
- [x] Product variant service trims SKU before persisting. 2026-07-16 Phase 6: `ProductVariantServiceTests.AddAsync_WhenSkuIsUnique_TrimsBeforePersisting` passed.
- [x] Cart/checkout variant and selected-attribute flows still pass after hardening. 2026-07-16 Phase 6: focused `StorefrontCartServiceTests` and `StorefrontCheckoutServiceTests` run passed.
- [x] Unsupported product types are rejected by admin service and product import resolver. 2026-07-16 Phase 9: `CatalogProductTypeGateTests|ProductServiceTests` passed 29/29.
- [x] Catalog Structure Core final focused release gate passed. 2026-07-16 Phase 10: Commerce Node catalog/service/repository/import/model/OpenAPI focused run passed 110/110 after stabilizing timestamp-based catalog ordering seed.
- [ ] Live API smoke creates a category with description, updates it, and reads it back through Commerce admin API.

## Product Variant Attribute

- [x] Phase 0 baseline confirms existing variation template, product variant, cart, and public catalog behavior before schema changes. 2026-07-16: `ProductVariantServiceTests|StorefrontCartServiceTests|PublicCatalogServiceTests|CommerceNodeStorefrontOpenApiContractTests` passed 48/48.
- [x] Duplicate product variant attribute signature is rejected. 2026-07-16 Phase 0: `ProductVariantServiceTests.AddAsync_WhenAttributeSignatureAlreadyExistsForProduct_ReturnsFailure` passed.
- [x] Storefront product detail mapping exposes only active variation template options and values. 2026-07-16 Phase 0: `PublicCatalogServiceTests.GetPublishedProductBySlugAsync_MapsActiveVariationTemplateOptionsAndValues` passed.
- [x] Variation template option control type defaults to `dropdown` for existing data. 2026-07-16 Phase 1: `CommerceNodeVariationOptionMetadata` migration and `CommerceNodeDbContextModelTests.VariationTemplateOption_MetadataFieldsHaveDefaults` passed.
- [x] Variation template option required state defaults to `true` for existing data. 2026-07-16 Phase 1: migration/model test passed.
- [x] Unknown variation control type is rejected. 2026-07-16 Phase 1: `VariationTemplateServiceTests.CreateOptionAsync_RejectsUnknownControlType` passed.
- [x] Invalid color hex is rejected. 2026-07-16 Phase 1: `VariationTemplateServiceTests.CreateValueAsync_RejectsInvalidColorHex` passed.
- [x] Storefront product detail response includes variation option control metadata. 2026-07-16 Phase 1: `PublicCatalogServiceTests.GetPublishedProductBySlugAsync_MapsActiveVariationTemplateOptionsAndValues` and Storefront OpenAPI snapshot passed.
- [x] Product variant active state defaults to `true`. 2026-07-16 Phase 2: `CommerceNodeProductVariantActiveState` migration and `CommerceNodeDbContextModelTests.ProductVariant_IsActiveDefaultsToTrue` passed.
- [x] Inactive variant cannot be set as default. 2026-07-16 Phase 2: `ProductVariantServiceTests.AddAsync_WhenDefaultVariantIsInactive_ReturnsFailure` passed.
- [x] Variant combination validation rejects unknown template option names. 2026-07-16 Phase 2: `ProductVariantServiceTests.AddAsync_WhenTemplateOptionIsUnknown_ReturnsFailure` passed.
- [x] Variant combination validation rejects unknown template values. 2026-07-16 Phase 2: `ProductVariantServiceTests.AddAsync_WhenTemplateValueIsUnknown_ReturnsFailure` passed.
- [x] Shared product selection resolver rejects missing required options. 2026-07-16 Phase 3: `ProductSelectionResolverTests.ResolveAsync_RejectsMissingRequiredOption` passed.
- [x] Shared product selection resolver rejects inactive variants. 2026-07-16 Phase 3: `ProductSelectionResolverTests.ResolveAsync_RejectsInactiveVariant` passed.
- [x] Shared product selection resolver resolves active variants from selected template attributes. 2026-07-16 Phase 5: `ProductSelectionResolverTests.ResolveAsync_ResolvesVariantFromSelectedTemplateAttributes` passed.
- [x] Shared product selection resolver rejects selected attributes that do not match an explicit variant. 2026-07-16 Phase 5: `ProductSelectionResolverTests.ResolveAsync_RejectsSelectedAttributesThatDoNotMatchExplicitVariant` passed.
- [x] Storefront selection-preview endpoint is store-scoped. 2026-07-16 Phase 4: `StorefrontScopedCatalogControllerSelectionPreviewTests.PreviewProductSelection_WhenStoreCannotBeResolved_ReturnsNotFoundAndSkipsResolver` passed; resolver remains store-scoped by `StoreId`.
- [x] Storefront selection-preview endpoint rejects quantity below 1. 2026-07-16 Phase 4: Storefront OpenAPI contract asserts `StorefrontProductSelectionPreviewRequest.quantity minimum: 1`; `[ApiController]` model validation enforces runtime 400.
- [x] Product Variant Attribute automated release gate passed. 2026-07-16 Phase 7: active V2 builds passed; focused CommerceNode/Storefront contract/client/cart/checkout/control-plane release-gate tests passed 140/140, and Storefront host smoke passed 34/34. Existing warnings remain NuGet vulnerability advisories and Browserslist freshness notices.

## Availability Quantity

- [x] Phase 0 baseline confirms current catalog visibility, stock, cart, checkout, and Storefront contract behavior before sellability changes. 2026-07-16: active V2 focused run passed 67/67.
- [x] Published store-visible products appear in public catalog. 2026-07-16 Phase 0: `CommerceNodeProductStoreScopeTests` passed.
- [x] Unpublished, archived, scheduled, expired, and wrong-store products do not appear in public catalog. 2026-07-16 Phase 0: `CommerceNodeProductStoreScopeTests` and `PublicCatalogServiceTests` passed.
- [x] Public catalog `InStock` still reflects product quantity or variant stock. 2026-07-16 Phase 0: Storefront contract/OpenAPI guardrails passed; stronger projection-specific assertions are reserved for Phase 3.
- [x] Storefront cart rejects quantity below 1 before product lookup. 2026-07-16 Phase 0: `StorefrontCartServiceTests` passed.
- [x] Storefront cart rejects stock shortage and unavailable variants. 2026-07-16 Phase 0: `StorefrontCartServiceTests` passed.
- [x] Storefront checkout validates cart lines before order creation. 2026-07-16 Phase 0: `StorefrontCheckoutServiceTests` passed.
- [x] Storefront OpenAPI preserves `InStock`, `Quantity`, and variant `Stock`. 2026-07-16 Phase 0: `CommerceNodeStorefrontOpenApiContractTests` passed.
- [x] Product purchase fields are additive and persisted on CommerceNode `Products`. 2026-07-16 Phase 1: `CommerceNodeProductPurchaseFields` migration adds min/max/step, purchase-disabled, manage-stock, hide-when-out-of-stock, shipping, free-shipping, and delivery estimate fields only.
- [x] Product purchase fields have safe defaults and length limits. 2026-07-16 Phase 1: `CommerceNodeDbContextModelTests.ProductPurchaseFields_HaveSafeDefaultsAndMaxLengths` passed.
- [x] Product service rejects invalid min/max/step and overlong purchase-disabled reason. 2026-07-16 Phase 1: focused `ProductServiceTests` passed inside 52/52 run.
- [x] Phase 1 does not add stock ledger, reservation, backorder, warehouse, or shipping charge tables. 2026-07-16: migration inspection confirmed only additive `Products` columns.
- [x] Product sellability resolver covers every planned block reason and stock status. 2026-07-16 Phase 2: `ProductSellabilityResolverTests` passed inside focused 36/36 run.
- [x] Product selection/cart flow consumes centralized sellability without regressing existing cart behavior. 2026-07-16 Phase 2: `ProductSelectionResolverTests|StorefrontCartServiceTests` passed inside focused 36/36 run.
- [x] Unmanaged stock/POD products can be purchasable with zero quantity. 2026-07-16 Phase 2: `ProductSellabilityResolverTests.Resolve_WhenStockIsUnmanaged_AllowsZeroQuantityProduct` passed.
- [x] Storefront catalog/detail/variant schemas expose product sellability projection without exposing domain entities. 2026-07-16 Phase 3: `CommerceNodeStorefrontOpenApiContractTests.StorefrontSwagger_ProductSellabilityProjectionHasGeneratorSafeContract` passed.
- [x] Storefront OpenAPI snapshot refreshed for availability quantity sellability fields. 2026-07-16 Phase 3: focused contract/client/catalog run passed 41/41.
- [x] Storefront cart add-line rejects purchase-disabled products without mutating the cart. 2026-07-16 Phase 4: `StorefrontCartServiceTests.AddLineAsync_RejectsPurchaseDisabledProduct` passed.
- [x] Storefront cart validation returns stable sellability reason codes for min/max/step quantity rules. 2026-07-16 Phase 4: `ValidateAsync_ReturnsStableSellabilityReasonCode_ForQuantityRules` passed.
- [x] Storefront product detail OpenAPI exposes delivery metadata for generator-safe clients. 2026-07-16 Phase 6: `StorefrontProductResponse` includes `shippingRequired`, `freeShipping`, `deliveryEstimateText`, `weight`, `length`, `width`, and `height`; `CommerceNodeStorefrontOpenApiContractTests` passed 25/25 after snapshot refresh.
- [x] Checkout keeps delivery metadata display-only and does not calculate shipping charges. 2026-07-16 Phase 6: `StorefrontCheckoutServiceTests.PreviewAsync_KeepsDeliveryMetadataDisplayOnly` passed with `ShippingTotal = 0m` and `GrandTotal == Subtotal`.
- [x] Availability Quantity CommerceNode release gate passed. 2026-07-16 Phase 8: CommerceNode API build passed; focused `ProductServiceTests|ProductSellabilityResolverTests|PublicCatalogServiceTests|CommerceNodeProductStoreScopeTests|CommerceNodeStorefrontOpenApiContractTests|StorefrontCartServiceTests|StorefrontCheckoutServiceTests` passed 122/122.
- [x] Storefront cart allows unmanaged-stock product with zero stored quantity. 2026-07-16 Phase 4: `AddLineAsync_AllowsUnmanagedStockProductWithZeroQuantity` passed.
- [x] Checkout preview blocks non-purchasable cart lines with line/product-aware reason codes. 2026-07-16 Phase 4: `PreviewAsync_ReturnsSellabilityIssue_WhenProductPurchaseIsDisabledAfterAdd` passed.
- [x] Checkout place-order allows unmanaged-stock products without decrementing quantity below zero. 2026-07-16 Phase 4: `PlaceOrderAsync_AllowsUnmanagedStockProductWithoutDeductingQuantity` passed.

## Address Core

- [x] Phase 0 baseline confirms checkout still accepts direct shipping address and snapshots it into `CheckoutSession` and `Order`. 2026-07-17: `StorefrontCheckoutServiceTests.PlaceOrderAsync_CopiesCheckoutAddressSnapshotAndDoesNotReadMutatedCustomerProfile` passed.
- [x] Order address snapshot remains independent from later customer profile mutation. 2026-07-17: focused checkout service test mutated `CommerceCustomer` before place-order and asserted order kept the session snapshot.
- [x] Public checkout/address request contracts do not expose `customerId`, `storeId`, auth-owned IDs, audit fields, or order snapshot fields. 2026-07-17: `AddressCorePhase0InventoryTests` passed.
- [x] Checkout controller derives store scope from Storefront route/current store context and not from request body. 2026-07-17: `AddressCorePhase0InventoryTests.CommerceNode_CheckoutControllerDerivesStoreScopeFromRoute` passed.
- [x] Storefront OpenAPI checkout contracts remain generator-safe after Address Core Phase 0 guardrails. 2026-07-17: `CommerceNodeStorefrontOpenApiContractTests` passed inside focused 61/61 run.
- [x] No CommerceNode schema, migration, `AppDbContext`, or legacy runtime change was introduced in Phase 0. 2026-07-17: Phase 0 changed tests/docs only.
- [x] `CommerceCustomerAddress` is stored only in `CommerceNodeDbContext`. 2026-07-17 Phase 1: entity, DbSet, mapping, and migration were added under active CommerceNode paths.
- [x] Address rows are store-scoped and customer-scoped. 2026-07-17 Phase 1: model test asserts `StoreId`/`CustomerId` indexes and cascade FKs to `CommerceStore`/`CommerceCustomer`.
- [x] Address public IDs are unique for future API responses. 2026-07-17 Phase 1: `IX_commerce_customer_addresses_public_id` unique index generated.
- [x] Soft delete column exists for customer addresses. 2026-07-17 Phase 1: migration adds nullable `deleted_at_utc`.
- [x] Default shipping uniqueness is enforced for active rows. 2026-07-17 Phase 1: filtered unique index `is_default_shipping = true AND deleted_at_utc IS NULL` generated and covered by model test.
- [x] Default billing uniqueness is enforced for active rows. 2026-07-17 Phase 1: filtered unique index `is_default_billing = true AND deleted_at_utc IS NULL` generated and covered by model test.
- [x] Existing checkout/order snapshot tables are unchanged by address persistence migration. 2026-07-17 Phase 1: migration inspection confirmed only `commerce_customer_addresses` is created/dropped.
- [x] Address persistence model and existing checkout service tests pass together. 2026-07-17 Phase 1: focused run passed 38/38.
- [x] Address validation returns stable issue codes. 2026-07-17 Phase 2: `AddressValidationServiceTests` covers required fields, invalid country/email, state-required countries, and length errors.
- [x] Address normalization is deterministic server-side. 2026-07-17 Phase 2: tests assert trim, uppercase country/state code, optional blank-to-null, and preserved name/address casing.
- [x] Address validation can run independently from checkout/UI code. 2026-07-17 Phase 2: `IAddressValidationService` lives in Application and is registered for CommerceNode runtime.
- [x] Address validation field max lengths align with Phase 1 persistence mapping. 2026-07-17 Phase 2: service and tests cover phone/address length limits; model tests cover DB max lengths.
- [x] Storefront address country lookup endpoint returns public-safe metadata. 2026-07-17 Phase 3: `AddressLookupServiceTests` covers deterministic country catalog and `CommerceNodeStorefrontOpenApiContractTests` covers response schemas.
- [x] Storefront address state/province lookup endpoint returns public-safe metadata. 2026-07-17 Phase 3: tests cover US state/province metadata and empty known-country state lists.
- [x] Unknown address lookup country returns a deterministic not-found response. 2026-07-17 Phase 3: `AddressLookupServiceTests.GetStatesAsync_WhenCountryIsUnknown_ReturnsNotFound` passed.
- [x] Storefront address lookup/config endpoints are anonymous and generator-safe. 2026-07-17 Phase 3: OpenAPI metadata and snapshots include address lookup operations with no Bearer requirement.
- [x] Address field configuration response is explicit and non-secret. 2026-07-17 Phase 3: configuration exposes field enablement/limits and state-required country codes only.
- [x] Address book endpoints require Bearer auth. 2026-07-17 Phase 4: Storefront OpenAPI contract tests assert Bearer security for list/create/update/delete/default operations.
- [x] Address request DTOs do not expose `customerId`, `storeId`, audit fields, or order snapshot fields. 2026-07-17 Phase 4: `StorefrontSwagger_CustomerAddressBookHasGeneratorSafeContract` passed.
- [x] Customer cannot read/update/delete another customer's address by public ID. 2026-07-17 Phase 4: `StorefrontCustomerAddressServiceTests.UpdateAsync_WhenAddressBelongsToAnotherCustomer_ReturnsNotFound` passed.
- [x] Soft-deleted address is excluded from active list and selection. 2026-07-17 Phase 4: `DeleteAsync_SoftDeletesAddressAndListExcludesIt` passed.
- [x] Default shipping uniqueness is enforced. 2026-07-17 Phase 4: service tests cover clearing previous default shipping.
- [x] Default billing uniqueness is enforced. 2026-07-17 Phase 4: service tests cover `SetDefaultBillingAsync` clearing previous default billing.
- [x] Storefront customer address CRUD/default OpenAPI is generator-safe. 2026-07-17 Phase 4: snapshots refreshed and focused contract/service/static guard run passed 38/38.
- [x] Checkout with saved address snapshots address data. 2026-07-17 Phase 5: `PlaceOrderAsync_UsesSavedShippingAddressAndSnapshotsIt` passed and proves later saved-address mutation does not alter order snapshot.
- [x] Guest direct checkout address still works. 2026-07-17 Phase 5: existing checkout service suite stayed green after additive `ShippingAddressId` fields and direct-address validation-core wiring.
- [x] Anonymous saved-address checkout selection is rejected. 2026-07-17 Phase 5: `PreviewAsync_RejectsSavedAddressSelection_WhenCustomerIsAnonymous` passed.
- [x] Checkout preview OpenAPI exposes saved-address fields additively. 2026-07-17 Phase 5: contract test asserts `shippingAddressId`, `billingAddressId`, and `useShippingAddressAsBillingAddress`.
- [x] Storefront V2 consumes the CommerceNode address lookup/config and customer address-book contracts through scoped routes. 2026-07-17 Phase 6: Storefront client tests assert `/api/storefront/stores/default/address/*` and `/api/storefront/stores/default/customer/addresses` with Bearer auth.
- [x] Storefront V2 checkout continues to use CommerceNode snapshot-safe preview/place-order flow after saved-address UI integration. 2026-07-17 Phase 6: Storefront host smoke passed 34/34 and Address Core static guard confirms no browser-owned `customerId`, `storeId`, or audit fields are posted.
- [x] Address field configuration contract is stable for future store-specific overrides without adding Control Plane/Admin UI yet. 2026-07-17 Phase 7: `AddressCorePhase7ConfigurationTests` guards the config response fields, anonymous scoped route, and no ControlPlane Web dependency.
- [x] Address Core automated release gate passed. 2026-07-17 Phase 8: focused application/service/OpenAPI/Storefront client/static/host smoke run passed 134/134.

## Checkout Core

- [x] Phase 0 baseline confirms existing checkout service behavior before stateful checkout changes. 2026-07-17: focused `StorefrontCheckoutServiceTests` plus Storefront checkout host smoke cases passed 22/22.
- [x] Hosted payment redirect is not treated as a completed order. 2026-07-17 Phase 0: `StorefrontCheckoutServiceTests.PlaceOrderAsync_StripeCreatesRedirectAttemptWithoutOrder` proves no order is created, checkout is `order_pending`, and cart remains active.
- [x] Expired checkout session blocks place-order and marks checkout expired. 2026-07-17 Phase 0: `PlaceOrderAsync_WhenCheckoutSessionExpired_BlocksOrderAndMarksExpired` passed.
- [x] Checkout session state/version fields are additive and CommerceNode-only. 2026-07-17 Phase 1: migration `CommerceNodeCheckoutSessionResume` adds only checkout session progress columns and backfills existing rows from current state/cart version.
- [x] Checkout start can create and resume the active session for the same store/cart context. 2026-07-17 Phase 1: `StartAsync_CreatesAndResumesCheckoutSession_ForSameStoreAndCart` passed.
- [x] Checkout resume is store-scoped and cart-scoped. 2026-07-17 Phase 1: `LoadAsync_IsStoreAndCartScoped` passed for wrong cart token and wrong store context.
- [x] Expired checkout cannot resume as active. 2026-07-17 Phase 1: `LoadAsync_WhenExpired_MarksSessionExpiredAndRejectsResume` passed.
- [x] Cancelled checkout cannot resume as active and increments checkout version. 2026-07-17 Phase 1: `CancelAsync_MarksSessionCancelledAndIncrementsVersion` passed.
- [x] Storefront checkout start/load/cancel OpenAPI contracts are generator-safe. 2026-07-17 Phase 1: `CommerceNodeStorefrontOpenApiContractTests` passed with refreshed Storefront swagger snapshots.
- [x] Checkout start rejects empty carts before creating a session. 2026-07-17 Phase 2: `StartAsync_RejectsEmptyCart` passed.
- [x] Checkout entry validation is centralized for start and preview. 2026-07-17 Phase 2: focused checkout service run passed 26/26 after replacing duplicated preview cart checks with shared entry validation.
- [x] Cart version changes reset downstream checkout state. 2026-07-17 Phase 2: `LoadAsync_WhenCartVersionChanged_ResetsDownstreamCheckoutState` passed and asserts issue code `cart.version_changed`.
- [x] Storefront OpenAPI remains stable after entry validation/cart drift changes. 2026-07-17 Phase 2: `CommerceNodeStorefrontOpenApiContractTests` passed 29/29 with unchanged snapshots.
- [x] Checkout address step supports direct guest billing/shipping address snapshot. 2026-07-17 Phase 3: `UpdateAddressesAsync_WithDirectAddresses_SnapshotsAndResetsPaymentStep` passed.
- [x] Checkout address step rejects anonymous saved-address selection. 2026-07-17 Phase 3: `UpdateAddressesAsync_RejectsSavedAddressSelection_WhenCustomerIsAnonymous` passed.
- [x] Checkout address step enforces saved address ownership by store/customer. 2026-07-17 Phase 3: `UpdateAddressesAsync_UsesSavedAddress_WhenCustomerOwnsIt` passed.
- [x] Checkout address step OpenAPI is generator-safe and avoids server-owned request fields. 2026-07-17 Phase 3: `CommerceNodeStorefrontOpenApiContractTests` passed 29/29 after snapshot refresh.
- [x] Checkout shipping method step stores deterministic `free_standard` option without trusting browser price/total fields. 2026-07-17 Phase 4: `SelectShippingMethodAsync_SelectsFreeStandardAndResetsPayment` passed.
- [x] Checkout shipping method step requires shipping address first. 2026-07-17 Phase 4: `SelectShippingMethodAsync_BlocksWhenShippingAddressMissing` passed.
- [x] Checkout shipping method OpenAPI is generator-safe and exposes server-owned shipping option response metadata. 2026-07-17 Phase 4: `CommerceNodeStorefrontOpenApiContractTests` passed 29/29 after snapshot refresh.

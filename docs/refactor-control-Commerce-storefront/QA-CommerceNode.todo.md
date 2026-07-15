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

## Store Mapping

- [x] Product admin catalog page is scoped to the current Commerce store. 2026-07-15: `CommerceNodeProductStoreScopeTests.GetCatalogPageForCurrentStoreAsync_ReturnsOnlyCurrentStoreProducts` passed.
- [x] Product admin detail returns not found/null for a product belonging to another store. 2026-07-15: repository and service guardrails passed for cross-store product detail/update/delete.
- [x] Product update/delete rejects cross-store IDs before mutating data. 2026-07-15: focused `ProductServiceTests` cross-store update/delete guardrails passed.
- [x] Product create/update rejects a category from another current store. 2026-07-15: `AddAsync_WhenCategoryBelongsToDifferentCurrentStore_ReturnsValidationFailure` passed.
- [x] Product SEO slug duplicate check is scoped by store. 2026-07-15: `ProductSeoServiceTests.UpdateAsync_WhenSlugExistsOnlyInAnotherStore_AllowsUpdate` passed and verified `ProductSlugExistsInStoreAsync`.
- [ ] Category admin list/query/detail/update/delete are scoped to the current Commerce store.
- [ ] Category parent/child update rejects cross-store parent assignment through current-store scoped lookup.
- [ ] StorefrontPage list/detail/slug/sitemap scope has dedicated store mapping guardrails.

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
- [x] Missing online provider configuration returns a safe failure without creating an order. 2026-07-14: Stripe provider config checks return conflict and checkout marks the attempt failed with safe `provider_session_failed` details.
- [x] Storefront Swagger includes generator-safe request/response/error schemas for every new cart/checkout/payment endpoint. 2026-07-14: server cart, checkout preview, place-order, payment attempt polling, provider callback, and webhook endpoints covered by contract tests.
- [x] Storefront Swagger snapshot is refreshed after cart/checkout/payment API cutover. 2026-07-14: refreshed through Phase 10 first online provider result-shape update; focused checkout/payment/contract/Storefront suite passed 63/63 before Storefront host redirect test, then Storefront host suite passed 27/27.
- [x] Direct raw Storefront checkout path is retired after Storefront V2 cutover. 2026-07-14: `POST /cart/checkout` removed from active Storefront API contract/client; focused contract/Storefront suite passed 51/51.

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

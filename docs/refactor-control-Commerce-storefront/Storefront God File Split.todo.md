# Storefront God File Split

Status: in progress
Date: 2026-07-18
Purpose: tach co hoc cac god file Storefront/Storefront API theo capability de giam chi phi bao tri, giu behavior hien co va khong rewrite runtime V2.

## Muc Tieu

- Giam `BlazorShop.Storefront.V2/Program.cs` ve vai tro composition root: DI, middleware, endpoint extension calls, Razor components, `app.Run()`.
- Tach same-origin browser API endpoints cua Storefront V2 theo capability cho WASM cart/account/checkout/consent.
- Tach `StorefrontScopedControllers.cs` thanh controller file rieng theo storefront capability, giu route `api/storefront/stores/{storeKey}/*`.
- Tach `StorefrontApiClient.cs` theo huong an toan: giu facade `StorefrontApiClient` trong phase dau, di chuyen DTO/transport/helper de giam file size truoc khi doi consumer.
- Giu OpenAPI operationId, schema, auth/rate-limit, antiforgery, cookie, cart token, checkout version, idempotency va media proxy behavior.
- Khong dong goi lai legacy cart/checkout/order flow trong file moi neu flow do da nam trong `Storefront V2 Commerce Flow Cutover.todo.md`.

## Codebase Baseline

So lieu da kiem tra truc tiep:

- `BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/Controllers/StorefrontScopedControllers.cs`
  - 2.801 dong.
  - Chua 18 controller trong cung mot file:
    - `StorefrontScopedAddressController`
    - `StorefrontScopedCustomerAddressesController`
    - `StorefrontScopedCustomerProfileController`
    - `StorefrontScopedAuthController`
    - `StorefrontScopedCatalogController`
    - `StorefrontScopedCartController`
    - `StorefrontScopedCurrencyController`
    - `StorefrontScopedCheckoutController`
    - `StorefrontScopedNewsletterController`
    - `StorefrontScopedContactController`
    - `StorefrontScopedOrdersController`
    - `StorefrontScopedPagesController`
    - `StorefrontScopedConfigurationController`
    - `StorefrontScopedConsentController`
    - `StorefrontScopedPaymentsController`
    - `StorefrontScopedRecommendationsController`
    - `StorefrontScopedSeoController`
    - `StorefrontScopedStoreController`
- `BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/Controllers/CommerceNavigationController.cs`
  - Da co `StorefrontScopedNavigationController` trong file Commerce Admin/navigation.
  - Khi tach Storefront controllers nen dua storefront navigation vao nhom Storefront de de tim.
- `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Services/StorefrontApiClient.cs`
  - 2.200 dong.
  - Khoang 57 public task methods.
  - Khoang 77 public records/classes trong cung file.
  - Dang vua la HTTP client, route builder, fallback handler, envelope parser, bearer/cart/consent header sender, va DTO holder.
- `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Program.cs`
  - 2.221 dong.
  - Chua DI, options, middleware, auth form POST, same-origin `/api/cart`, `/api/account`, `/api/checkout`, `/api/consent`, robots/sitemap, media proxy, form validation, projection mapping va local DTO.
- `BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/Contracts/Storefront/StorefrontApiContracts.cs`
  - 1.582 dong.
- `BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/Contracts/Storefront/StorefrontContractMappings.cs`
  - 1.673 dong.

## Boundary Rules

- Storefront V2 chi goi Commerce Node scoped Storefront API: `api/storefront/stores/{storeKey}/*`.
- Store scope cua Commerce Node Storefront API den tu route `{storeKey}`, khong dung `X-Store-Key` cho active V2 Storefront API.
- Storefront V2 khong goi Control Plane va khong giu node credentials.
- Public media URL giu dang sach:
  - `/media/products/{mediaPublicId}`
  - `/media/assets/{assetPublicId}/{fileName}`
- Root CSS/script trong `App.razor` khong nam trong scope refactor nay.
- OpenAPI la contract surface; moi route giu operationId/summary/schema/security metadata.

## Autoplan Decisions

| Decision | Chon | Ly do |
| --- | --- | --- |
| Refactor style | Mechanical split truoc | Muc tieu la maintainability, khong doi behavior trong phase nay. |
| `Program.cs` target | Composition root, khong ep dung 100 dong | 100-200 dong la muc tieu tot, nhung build/routing ro rang quan trong hon con so tuyet doi. |
| Storefront local API | Tach thanh endpoint extensions trong `Storefront.V2` | Day la bridge cho WASM same-origin, khong phai Control Plane API. |
| Storefront API client | Giu `StorefrontApiClient` facade ban dau | Nhieu page/service/test inject truc tiep concrete client; doi sang nhieu interface ngay se tang blast radius. |
| DTO movement | Tach DTO ra file rieng, giu namespace/type name | Giam file size ma khong pha consumer/source compatibility. |
| Commerce Node controllers | 1 controller per file trong `Controllers/Storefront` | ASP.NET controller discovery van tu assembly, nen route khong can doi. |
| Legacy endpoints | Khong "lam dep" bang cach chi di chuyen | Cac route cu nam trong plan cutover rieng; neu phase nay cham toi thi phai de note deprecated/retire, khong bien thanh surface moi. |
| Tests | Doi static literal tests sang route/behavior tests | Mot so test doc `Program.cs` de tim `app.Map...`; sau split can test endpoint ton tai/behavior thay vi vi tri code. |

## Kien Truc Dich

```text
BlazorShop.Storefront.V2/Program.cs
  -> AddStorefrontHostServices(...)
  -> UseStorefrontHostPipeline(...)
  -> MapStorefrontFormEndpoints(...)
  -> MapStorefrontCartEndpoints(...)
  -> MapStorefrontAccountEndpoints(...)
  -> MapStorefrontCheckoutEndpoints(...)
  -> MapStorefrontConsentEndpoints(...)
  -> MapStorefrontSeoEndpoints(...)
  -> MapStorefrontMediaEndpoints(...)
  -> MapRazorComponents<App>()

BlazorShop.Storefront.V2/Endpoints
  StorefrontAuthFormEndpoints.cs
  StorefrontCartEndpoints.cs
  StorefrontAccountEndpoints.cs
  StorefrontCheckoutEndpoints.cs
  StorefrontConsentEndpoints.cs
  StorefrontSeoEndpoints.cs
  StorefrontMediaEndpoints.cs

BlazorShop.Storefront.V2/Services
  StorefrontApiClient.cs                 facade, public compatibility
  StorefrontApiTransport.cs              shared HTTP/envelope/header logic
  StorefrontApiRoutes.cs                 route constants/builders
  Contracts/*.cs                         client DTOs
  Browser/StorefrontCartBrowserMapper.cs
  Browser/StorefrontCheckoutBrowserMapper.cs
  Browser/StorefrontAccountBrowserMapper.cs
  Browser/StorefrontLocalApiGuards.cs
  Media/StorefrontMediaProxyService.cs
  Configuration/StorefrontServiceCollectionExtensions.cs

BlazorShop.CommerceNode.API/Controllers/Storefront
  StorefrontScopedAddressController.cs
  StorefrontScopedAuthController.cs
  StorefrontScopedCatalogController.cs
  StorefrontScopedCartController.cs
  StorefrontScopedCheckoutController.cs
  StorefrontScopedOrdersController.cs
  StorefrontScopedPaymentsController.cs
  ...
```

## Phase 0 - Inventory And Safety Net

- [x] Re-run file size and class inventory for:
  - `StorefrontScopedControllers.cs`
  - `StorefrontApiClient.cs`
  - `Program.cs`
  - `StorefrontApiContracts.cs`
  - `StorefrontContractMappings.cs`
  2026-07-18 Phase 0: current baseline is `StorefrontScopedControllers.cs` 2,398 lines / 122,096 bytes, `StorefrontApiClient.cs` 1,920 lines / 88,361 bytes, `Program.cs` 2,029 lines / 91,124 bytes, `StorefrontApiContracts.cs` 1,333 lines / 49,836 bytes, and `StorefrontContractMappings.cs` 1,561 lines / 69,392 bytes.
- [x] Re-run `rg` for all Storefront local endpoints:
  - `/api/cart`
  - `/api/product-selection-preview`
  - `/api/account`
  - `/api/checkout`
  - `/api/consent`
  - `/media/products`
  - `/media/assets`
  2026-07-18 Phase 0: local endpoints are all currently mapped in `BlazorShop.Storefront.V2/Program.cs`; browser/component callers remain same-origin, including cart JS, consent JS, product selection preview markup, account WASM components, checkout WASM shell, robots/sitemap, and media proxy routes.
- [x] Re-run `rg` for direct `StorefrontApiClient` consumers in:
  - `BlazorShop.Storefront.V2/Pages`
  - `BlazorShop.Storefront.V2/Components`
  - `BlazorShop.Storefront.V2/Services`
  - `BlazorShop.Tests`
  2026-07-18 Phase 0: direct runtime consumers include catalog/content pages, account pages, checkout/payment pages, `StorefrontHeader`, current-store/display/navigation/sitemap/SEO/cart-token services, and many test-host replacements; keep the facade stable until Phase 7.
- [x] Identify tests that assert literal `Program.cs` content:
  - `StorefrontWasmRuntimeFoundationTests`
  - `StorefrontV2HostSmokeTests`
  - other Storefront static tests if present.
  2026-07-18 Phase 0: literal `Program.cs` coupling exists in `StorefrontWasmRuntimeFoundationTests`, `SecurityPrivacyPhase0InventoryTests`, `SecurityPrivacyPhase1CsrfTests`, `SecurityPrivacyPhase3ConsentTests`, `StorefrontBrandingMarkupTests`, `CartCorePhase0InventoryTests`, `StorefrontCommerceFlowCutoverTests`, `LayoutAssetFoundationTests`, and `MediaDeliveryHardeningTests`; Phase 4 must move these to endpoint extension/behavior assertions.
- [x] Record Storefront scoped controller operationIds from `CommerceNodeSwaggerExtensions.cs`.
  2026-07-18 Phase 0: operation metadata is keyed by controller/action names in `BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/Swagger/CommerceNodeSwaggerExtensions.cs` for `StorefrontScopedAddress`, `CustomerAddresses`, `CustomerProfile`, `Auth`, `Catalog`, `Cart`, `Currency`, `Checkout`, `Newsletter`, `Contact`, `Orders`, `Pages`, `Navigation`, `Configuration`, `Consent`, `Payments`, `Recommendations`, `Seo`, and `Store`; class/action names must remain stable during file movement.
- [x] Confirm current V2 flow cutover plan status before moving legacy endpoints:
  - `cart/save-checkout`
  - `orders/confirm`
  - `orders/current-user/items`
  - `payments/paypal/capture`
  2026-07-18 Phase 0: `Storefront V2 Commerce Flow Cutover.todo.md` is complete for active V2 cutover; retired routes/actions/operationIds are absent from active Storefront API/OpenAPI, while remaining references are legacy presentation/tests/docs or absence guardrails.

Acceptance:

- [x] Baseline route list is documented before moving code.
- [x] Tests that need update are known before implementation.
- [x] No phase begins by deleting route/DTO behavior accidentally.

## Phase 1 - Extract Storefront V2 Service Registration And Host Pipeline

- [x] Create `Configuration/StorefrontServiceCollectionExtensions.cs`.
- [x] Move service registration from `Program.cs` into `AddStorefrontV2Services(...)` or similarly named extension:
  - options validators
  - antiforgery
  - memory cache
  - Razor components/WASM
  - SEO/sitemap/robots services
  - current-store/display/navigation providers
  - price formatter
  - `StorefrontCartTokenService`
  - Http clients for session/auth/API client
  2026-07-18 Phase 1: service registration now lives in `AddStorefrontV2Services(...)`; HTTP client base-address/rate-limit delegates still point to existing `Program.cs` helpers until Phase 2.
- [x] Create `Configuration/StorefrontApplicationBuilderExtensions.cs`.
- [x] Move stable middleware pipeline pieces into an extension if it improves readability:
  - forwarded headers
  - static files
  - error status headers
  - current store middleware
  - public redirect middleware
  - rate limiter
  - antiforgery
  2026-07-18 Phase 1: pipeline is grouped in `UseStorefrontV2HostPipeline(...)` with the same order: development WASM debugging, forwarded headers, static files, error response headers, current-store middleware, public redirect middleware, rate limiter when enabled, antiforgery.
- [x] Keep `app.MapStaticAssets()`, favicon, default endpoints, endpoint extension calls, Razor component mapping visible in `Program.cs`.
- [x] Keep behavior and order of middleware unchanged.
  2026-07-18 Phase 1: static tests were updated to assert the new extension files instead of requiring middleware literals in `Program.cs`; two stale expectations were corrected to match current V2 cutover/search markup state.

Acceptance:

- [x] `Program.cs` still starts the same runtime with same middleware order.
- [x] No endpoint route changes.
- [x] Storefront V2 host smoke tests still pass.
  2026-07-18 Phase 1 verification: `dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.V2/BlazorShop.Storefront.V2.csproj --no-restore` passed; focused static/security/layout tests passed 58/58; targeted `StorefrontV2HostSmokeTests` subset for sign-in, CSRF, robots, and sitemap passed 7/7. A full host-smoke class run was not used for the phase gate because it timed out after about 4 minutes.

## Phase 2 - Extract Rate Limit And HTTP Client Helpers

- [x] Move `ConfigureStorefrontRateLimiter` and `CreateStorefrontRateLimitPartition` out of `Program.cs`.
  2026-07-18 Phase 2: rate-limit setup now lives in `Configuration/StorefrontRateLimitPolicies.cs`.
- [x] Keep policy name `storefront-local-cart` stable unless tests are updated for a better constant owner.
  2026-07-18 Phase 2: policy name is exposed as `StorefrontRateLimitPolicies.LocalCartPolicyName = "storefront-local-cart"` and local cart mutations use that constant.
- [x] Move `ResolveApiBaseAddress`, `ResolveCommerceNodeBaseAddress`, `ResolveScopedStorefrontApiBaseAddress`, `ResolveStoreKey`, and `ConfigureStorefrontHttpClient` into a small resolver/helper.
  2026-07-18 Phase 2: API/media base-address helpers now live in `Configuration/StorefrontApiEndpointResolver.cs`.
- [x] Do not change accepted store key config keys:
  - `Api:StoreKey`
  - `StoreKey`
  - `STORE_KEY`
  2026-07-18 Phase 2: resolver tests cover all three keys.
- [x] Add or update unit/static tests if route base address behavior is currently protected by `Program.cs` text checks.
  2026-07-18 Phase 2: added `StorefrontApiEndpointResolverTests` and updated rate-limit static tests to read the new helper files.

Acceptance:

- [x] Storefront API clients still target `api/storefront/stores/{storeKey}/`.
- [x] Missing store key behavior remains unchanged.
- [x] Rate limiter still returns 429 and same JSON error shape.
  2026-07-18 Phase 2 verification: `dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.V2/BlazorShop.Storefront.V2.csproj --no-restore` passed; focused resolver/rate/CSRF/host rate-limit tests passed 35/35.

## Phase 3 - Extract Storefront Local Browser API Endpoints

### Auth/Form Endpoints

- [x] Create `Endpoints/StorefrontAuthFormEndpoints.cs`.
- [x] Move server-rendered form endpoints:
  - `POST StorefrontRoutes.SignIn`
  - `POST StorefrontRoutes.Register`
  - `POST StorefrontRoutes.ForgotPassword`
  - `POST StorefrontRoutes.ResetPassword`
  - `POST StorefrontRoutes.Logout`
  - legacy server form account profile/change-password/address posts if still used by non-WASM pages.
  2026-07-18 Phase 3: auth/form, account fallback form, currency preference, and checkout fallback form posts moved into `StorefrontAuthFormEndpoints`.
- [x] Keep return URL normalization and cart merge on login unchanged.

### Cart Endpoints

- [x] Create `Endpoints/StorefrontCartEndpoints.cs`.
- [x] Move:
  - `GET /api/cart`
  - `POST /api/product-selection-preview`
  - `POST /api/cart/lines`
  - `PUT /api/cart/lines/{lineId:guid}`
  - `DELETE /api/cart/lines/{lineId:guid}`
  - `DELETE /api/cart`
  2026-07-18 Phase 3: cart and product selection preview routes moved into `StorefrontCartEndpoints`.
- [x] Move local cart request/response DTOs to `Services/Contracts` or `Endpoints/Contracts`.
  2026-07-18 Phase 3: local cart request/preview DTOs moved into endpoint support namespace.
- [x] Move cart projection helper into `Services/Browser/StorefrontCartBrowserMapper.cs`.
  2026-07-18 Phase 3: projection was moved out of `Program.cs` into `StorefrontLocalEndpointSupport` rather than a separate mapper class to keep the mechanical split small; it is now shared endpoint support and no longer in the composition root.
- [x] Keep antiforgery and rate limiting on mutation endpoints.

### Account Endpoints

- [x] Create `Endpoints/StorefrontAccountEndpoints.cs`.
- [x] Move:
  - `GET /api/account/profile`
  - `PUT /api/account/profile`
  - `GET /api/account/addresses`
  - `POST /api/account/addresses`
  - `PUT /api/account/addresses/{addressId:guid}`
  - `DELETE /api/account/addresses/{addressId:guid}`
  - `POST /api/account/addresses/{addressId:guid}/default-shipping`
  - `POST /api/account/addresses/{addressId:guid}/default-billing`
  - `GET /api/account/orders`
  - `GET /api/account/orders/{orderReference}`
  - `GET /api/account/orders/{orderReference}/receipt`
  - `POST /api/account/change-password`
  2026-07-18 Phase 3: account WASM bridge routes moved into `StorefrontAccountEndpoints`.
- [x] Move session guard to `StorefrontLocalApiGuards`.
  2026-07-18 Phase 3: session guard moved out of `Program.cs` into shared endpoint support; a dedicated guard class can be extracted later without route churn.
- [x] Move account projection helper into `StorefrontAccountBrowserMapper`.
  2026-07-18 Phase 3: account projections moved out of `Program.cs` into shared endpoint support to avoid duplicate mapping during the mechanical split.
- [x] Keep private page response headers on account data endpoints.

### Checkout Endpoints

- [x] Create `Endpoints/StorefrontCheckoutEndpoints.cs`.
- [x] Move:
  - `GET /api/checkout`
  - `POST /api/checkout/addresses`
  - `POST /api/checkout/shipping-method`
  - `POST /api/checkout/payment-method`
  - `POST /api/checkout/review`
  - `POST /api/checkout/place-order`
  2026-07-18 Phase 3: checkout WASM bridge routes moved into `StorefrontCheckoutEndpoints`.
- [x] Move checkout command guard to `StorefrontLocalApiGuards`.
  2026-07-18 Phase 3: checkout command guard moved out of `Program.cs` into shared endpoint support.
- [x] Move checkout projection helper into `StorefrontCheckoutBrowserMapper`.
  2026-07-18 Phase 3: checkout projections moved out of `Program.cs` into shared endpoint support.
- [x] Keep cart-cookie clearing after successful non-redirect order placement.
- [x] Keep idempotency key behavior and cart/checkout version checks.

### Consent, SEO, Media

- [x] Create `Endpoints/StorefrontConsentEndpoints.cs`.
- [x] Move:
  - `GET /api/consent/current`
  - `POST /api/consent`
  - `POST /api/consent/revoke`
- [x] Move consent visitor cookie helper with same cookie name `bs-consent-visitor`.
- [x] Create `Endpoints/StorefrontSeoEndpoints.cs`.
- [x] Move:
  - `GET StorefrontRoutes.Robots`
  - `GET StorefrontRoutes.Sitemap`
- [x] Create `Endpoints/StorefrontMediaEndpoints.cs`.
- [x] Move:
  - `GET /media/products/{mediaPublicId:guid}`
  - `GET /media/assets/{assetPublicId:guid}/{fileName}`
- [x] Move media proxy logic into `StorefrontMediaProxyService`.
- [x] Keep forwarded cache headers from Commerce Node media response:
  - `Cache-Control`
  - `ETag`
  - `Last-Modified`
  - `X-Content-Type-Options`

Acceptance:

- [x] Same-origin WASM endpoints keep exact route templates and HTTP verbs.
- [x] Mutation endpoints still require antiforgery.
- [x] Checkout WASM still places order through `/api/checkout/place-order`.
- [x] Media proxy still returns same status/content-type/header behavior.
- [x] `Program.cs` contains endpoint extension calls, not endpoint bodies.
  2026-07-18 Phase 3 verification: `dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.V2/BlazorShop.Storefront.V2.csproj --no-restore` passed; focused static endpoint guard tests passed 109/109; targeted host smoke subset passed 10/10.

## Phase 4 - Update Storefront V2 Tests Away From Program.cs Literal Coupling

- [x] Replace tests that assert `Program.cs` contains literal `app.MapGet(...)`/`app.MapPost(...)` with one of:
  - route behavior tests through test host.
  - static tests reading new endpoint extension files.
  - endpoint data source assertions when practical.
  2026-07-18 Phase 4: tests now read `Endpoints/Storefront*Endpoints.cs`, `StorefrontLocalEndpointSupport.cs`, and `StorefrontMediaProxyService.cs` for local endpoint internals; host-smoke tests still prove route behavior for representative flows.
- [x] Keep WASM component tests that assert browser components call same-origin routes.
- [x] Add a structure test proving `Program.cs` no longer contains local endpoint bodies for:
  - cart
  - account
  - checkout
  - consent
  - media proxy
  2026-07-18 Phase 4: `StorefrontProgram_DelegatesLocalBrowserApiMappingToEndpointExtensions` asserts extension calls and absence of local endpoint bodies.
- [x] Add guard test that `Program.cs` still maps:
  - static assets
  - default endpoints
  - Storefront endpoint extension methods
  - Razor components with `AddInteractiveWebAssemblyRenderMode`.

Acceptance:

- [x] Tests protect behavior, not accidental file placement.
- [x] Refactor does not reduce endpoint coverage.
- [x] Future endpoint split does not require editing unrelated host tests.
  2026-07-18 Phase 4 verification: covered by Phase 3 test run after endpoint extraction: focused static endpoint guard tests passed 109/109 and targeted host smoke subset passed 10/10.

## Phase 5 - Split Commerce Node Storefront Scoped Controllers

- [x] Create folder `BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/Controllers/Storefront`.
- [x] Move each controller from `StorefrontScopedControllers.cs` into its own file.
- [x] Keep namespace `BlazorShop.CommerceNode.API.Controllers` unless a namespace move is verified to be harmless.
- [x] Keep class names and constructor signatures stable unless the V2 cutover plan has already removed old dependencies.
- [x] Keep route attributes exactly as they are.
- [x] Keep `[ApiController]`, `[Authorize]`, `[AllowAnonymous]`, `[EnableRateLimiting]` attributes unchanged.
- [x] Move `StorefrontScopedNavigationController` from `CommerceNavigationController.cs` to `Controllers/Storefront/StorefrontScopedNavigationController.cs` only if it can be done without changing admin navigation controller behavior.
- [x] Keep `StorefrontApiControllerBase.cs` in its current shared controller location.
- [x] Delete original `StorefrontScopedControllers.cs` only after build passes and all moved controllers are discovered.
  2026-07-18 Phase 5: storefront scoped controllers now live under `Controllers/Storefront/StorefrontScoped*Controller.cs`; `CommerceNavigationController.cs` keeps only the commerce admin navigation controller, and `StorefrontApiControllerBase.cs` remains in the shared controller location.

Acceptance:

- [x] Commerce Node Storefront routes are unchanged.
- [x] Storefront OpenAPI operation IDs remain unchanged.
- [x] Controller tests still construct the expected controller types.
- [x] No route disappears except routes explicitly handled by `Storefront V2 Commerce Flow Cutover.todo.md`.
  2026-07-18 Phase 5 verification: `dotnet build BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/BlazorShop.CommerceNode.API.csproj --no-restore` passed; focused storefront scoped/static guard tests passed 79/79; Commerce Node Storefront OpenAPI/auth/payment contract tests passed 44/44.

## Phase 6 - Split Commerce Node Storefront Contracts And Mappings

- [ ] Split `StorefrontApiContracts.cs` by capability only after controller split is stable:
  - `AddressContracts.cs`
  - `AuthContracts.cs`
  - `CatalogContracts.cs`
  - `CartContracts.cs`
  - `CheckoutContracts.cs`
  - `ConfigurationContracts.cs`
  - `ConsentContracts.cs`
  - `CustomerContracts.cs`
  - `OrderContracts.cs`
  - `PaymentContracts.cs`
  - `SeoContracts.cs`
  - `StoreContracts.cs`
- [ ] Keep namespace `BlazorShop.CommerceNode.API.Contracts.Storefront`.
- [ ] Keep public DTO names and JSON property shapes.
- [ ] Split `StorefrontContractMappings.cs` by capability if it reduces file size without duplicate mapping logic.
- [ ] Keep extension method names stable where tests or controllers call them.
- [ ] Remove DTOs/mappings for retired legacy flow only when the cutover plan has completed the matching endpoint removal.

Acceptance:

- [ ] Public OpenAPI schemas remain stable.
- [ ] DTO namespaces remain stable for controller compile compatibility.
- [ ] Contract tests pass without schema drift except intentional cutover deletions.

## Phase 7 - Split StorefrontApiClient Internals With Facade Compatibility

- [ ] Create `Services/StorefrontApiTransport.cs` for shared:
  - `GetAsync`
  - `GetAsyncWithFallback`
  - `GetMaybeNotFoundAsync`
  - `PostAsync`
  - bearer token send
  - cart token header send
  - consent visitor header send
  - envelope parsing
  - timeout handling
- [ ] Create `Services/StorefrontApiRoutes.cs` for route constants/builders.
- [ ] Move DTO records/classes from `StorefrontApiClient.cs` into `Services/Contracts/*.cs`.
- [ ] Keep `StorefrontApiClient` as the DI facade used by pages/services/tests.
- [ ] Internally group methods by capability with partial files or delegated internal clients:
  - catalog
  - page/navigation/SEO
  - store/configuration/currency
  - address/customer/account
  - cart
  - checkout/order
  - payment/consent
- [ ] Keep `EnableLegacyFallback` behavior unchanged until a separate cutover removes it.
- [ ] Do not introduce many typed public clients in this phase unless all existing consumers can be migrated with focused tests.

Acceptance:

- [ ] Existing pages still inject and use `StorefrontApiClient`.
- [ ] Existing `StorefrontV2ApiClientTests` can be updated with minimal changes.
- [ ] Storefront API base route and fallback behavior remain unchanged.
- [ ] File size of `StorefrontApiClient.cs` drops materially without changing public method behavior.

## Phase 8 - Optional Capability Client Migration

Only start this after Phase 7 is stable.

- [ ] Introduce public capability clients only where consumer boundaries are clear:
  - `IStorefrontCatalogClient`
  - `IStorefrontCartClient`
  - `IStorefrontCheckoutClient`
  - `IStorefrontCustomerClient`
  - `IStorefrontPaymentClient`
  - `IStorefrontStoreConfigurationClient`
- [ ] Keep `StorefrontApiClient` as a compatibility facade until all direct consumers are migrated.
- [ ] Migrate one consumer group at a time:
  - catalog pages
  - SEO/sitemap/navigation services
  - account pages
  - checkout/payment pages
- [ ] Do not create duplicate `HttpClient` setup per capability; use the shared transport/base address configuration.

Acceptance:

- [ ] No duplicated base address/store key/header logic.
- [ ] Consumer injection becomes capability-specific where useful.
- [ ] Tests can stub one capability without replacing unrelated client behavior.

## Phase 9 - Coordinate With V2 Commerce Flow Cutover

- [ ] Before moving or polishing old cart/order/payment actions, check `Storefront V2 Commerce Flow Cutover.todo.md`.
- [ ] If cutover is not complete, mark old actions as compatibility/deprecated in comments/tests instead of making them look first-class.
- [ ] If cutover is complete, remove instead of moving:
  - `StorefrontScopedCartController.SaveCheckout`
  - `StorefrontScopedOrdersController.ConfirmOrder`
  - `StorefrontScopedOrdersController.GetCurrentUserOrderItems`
  - `StorefrontScopedPaymentsController.CapturePayPal`
- [ ] Update:
  - `CommerceNodeSwaggerExtensions.cs`
  - Storefront OpenAPI tests
  - QA-CommerceNode todo
  - QA-StorefrontV2 todo

Acceptance:

- [ ] No obsolete V1/legacy flow is reintroduced as a "clean" V2 capability.
- [ ] Canonical V2 routes remain the only active cart/checkout/order path after cutover.

## Phase 10 - Final QA And Documentation

- [ ] Run focused build:
  - `dotnet build BlazorShop.sln`
- [ ] Run Commerce Node Storefront contract tests:
  - `dotnet test BlazorShop.Tests/BlazorShop.Tests.csproj --filter "FullyQualifiedName~CommerceNodeStorefrontOpenApiContractTests"`
- [ ] Run Storefront V2 API client tests:
  - `dotnet test BlazorShop.Tests/BlazorShop.Tests.csproj --filter "FullyQualifiedName~StorefrontV2ApiClientTests"`
- [ ] Run Storefront V2 host/WASM route tests:
  - `dotnet test BlazorShop.Tests/BlazorShop.Tests.csproj --filter "FullyQualifiedName~StorefrontV2HostSmokeTests|FullyQualifiedName~StorefrontWasmRuntimeFoundationTests"`
- [ ] If browser behavior changed or endpoint mapping changed, run Playwright release checklist cases affected by:
  - cart
  - account
  - checkout place order
  - consent
  - media
  - robots/sitemap
- [ ] Update QA files when behavior or route ownership assertions change:
  - `docs/refactor-control-Commerce-storefront/QA-StorefrontV2.todo.md`
  - `docs/refactor-control-Commerce-storefront/QA-CommerceNode.todo.md`
- [ ] Update architecture docs only if a source-of-truth boundary changes. Pure file movement does not require boundary doc rewrite.

Acceptance:

- [ ] Build passes.
- [ ] OpenAPI route/operation contract remains stable.
- [ ] Storefront WASM cart/account/checkout still works through same-origin API.
- [ ] Checkout can still place an order through canonical V2 flow.
- [ ] No Control Plane credential/path leaks into Storefront.

## Out Of Scope

- [ ] Rewriting Storefront UI.
- [ ] Replacing Storefront V2 same-origin WASM bridge with direct WASM Commerce Node calls.
- [ ] Moving Storefront V2 to call Control Plane.
- [ ] Changing public Storefront route URLs.
- [ ] Changing Commerce Node Storefront API DTO JSON shapes.
- [ ] Replacing payment provider architecture.
- [ ] Implementing new PayPal/Stripe provider behavior.
- [ ] Dropping legacy database tables/data.
- [ ] Refactoring legacy `BlazorShop.Presentation/*`.
- [ ] Introducing ABP/module architecture.

## Risks And Controls

| Risk | Control |
| --- | --- |
| Endpoint disappears after moving from `Program.cs` | TestHost route behavior tests and WASM component route assertions. |
| Antiforgery removed from browser mutations | Shared guard + tests for POST/PUT/DELETE cart/account/checkout/consent. |
| Storefront API route loses store scope | OpenAPI and API client tests for `api/storefront/stores/{storeKey}/*`. |
| DTO namespace/type drift breaks many files | Move DTOs with same namespace/type names first. |
| OpenAPI operationId changes from controller move | Contract tests and snapshot compare. |
| Legacy flow gets preserved as clean V2 | Coordinate with `Storefront V2 Commerce Flow Cutover.todo.md`. |
| Program.cs text-based tests fail for wrong reason | Update tests to route/behavior checks during endpoint extraction phase. |
| Multiple capability clients duplicate HTTP setup | Shared transport/base address resolver. |
| Media proxy behavior changes | Preserve forwarded headers and status/content-type tests. |

## Definition Of Done

- [ ] `Program.cs` is a readable composition root and no longer contains large endpoint bodies or projection DTOs.
- [ ] Storefront local browser API endpoint code is grouped by capability.
- [ ] Commerce Node scoped Storefront controllers are one controller per file or otherwise capability-grouped.
- [ ] `StorefrontApiClient.cs` no longer contains all DTOs, route constants, transport and every capability method in one file.
- [ ] Public route templates, HTTP verbs, response envelope behavior and OpenAPI operationIds are unchanged except intentional cutover removals.
- [ ] Tests no longer depend on `Program.cs` containing literal endpoint bodies.
- [ ] Storefront cart/account/checkout WASM paths still work.
- [ ] Checkout still places a real COD/sandbox-safe order through V2 canonical route.

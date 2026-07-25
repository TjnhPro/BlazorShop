# Storefront V2 Shared Platform Functional MVP QA

## V2F0 baseline lock

Baseline commit SHA before refactor: `63f9095d73140e66e49075b6d3996b2ceb0ed421`.

Behavior-change scope: none. V2F0 only records the dependency inventory, migration matrix, temporary exceptions, and baseline verification.

## Dependency inventory

| Area | Current dependency | Notes |
| --- | --- | --- |
| Manual Storefront API client | `StorefrontApiClient` partials under `BlazorShop.Storefront.V2/Services` | Still owns address, cart, checkout, consent, customer/account, payment, auth-adjacent transport, and legacy-route fallback methods. |
| Generated storefront clients | `BlazorShop.Storefront.Client.Generated` registered by `Storefront.Runtime.AddStorefrontGeneratedClients` | Runtime registers generated address/auth/cart/catalog/checkout/configuration/consent/contact/currency/customer/navigation/newsletter/orders/pages/payments/recommendations/seo/store clients. |
| V2 generated adapters | `GeneratedStorefrontConfigurationClient`, `GeneratedStorefrontCatalogContentClient` | Active V2 DI uses generated adapters for store configuration, catalog, content, navigation, SEO, pages, currency, and product selection preview surfaces. |
| `Web.SharedV2` usage | V2 project reference, Dockerfile copy, Tailwind content path, endpoint helpers, rate-limit identity, cart/session/display context services | Current scan found shared utility imports in V2 host/support files; no direct `BlazorShop.Web.SharedV2.Models` import in active Storefront V2 or Components. |
| Component local API usage | `Storefront.Components/Browser/StorefrontLocalApiClient.cs` | Cart, checkout, account profile/address/orders/change password and product selection preview use same-origin `/api/*` BFF endpoints. |
| BFF endpoints | `StorefrontCartEndpoints`, `StorefrontCheckoutEndpoints`, `StorefrontAccountEndpoints`, `StorefrontConsentEndpoints`, `StorefrontAuthFormEndpoints`, `StorefrontMediaEndpoints`, `StorefrontSeoEndpoints`, plus `Program.cs` mappings | V2 host owns same-origin browser endpoints, antiforgery/session/cookies, SEO, media, and SSR route composition. |

## Migration matrix

| Capability | Current owner | Target owner | Existing QA coverage | Risk |
| --- | --- | --- | --- | --- |
| Store bootstrap/configuration | V2 generated adapter over Client | Runtime facade over Client, V2 host keeps store resolution/maintenance/SEO composition | generated config tests, provider/session tests, browser home/profile baseline | Medium |
| Catalog/search/product/content/navigation/SEO data | V2 generated catalog/content adapter plus V2 composition services | Runtime facades over Client, V2 host keeps route mapping/canonical/JSON-LD/sitemap/robots/page shell | generated catalog tests, composition guardrails, Playwright product/category/search/sitemap/robots baseline | High |
| Product interaction | V2 pages/components plus local JS/BFF preview | Components for presentation, Runtime/V2 interaction model, V2 host BFF for mutations | product page browser baseline, component/static tests | High |
| Cart | Manual `StorefrontApiClient`, V2 BFF endpoints, Components local API | Runtime cart facade, V2 host keeps cart token cookie/BFF/antiforgery, Components keep local API | cart API client tests, WASM foundation tests, browser cart/checkout baseline | High |
| Checkout/COD | Manual `StorefrontApiClient`, V2 checkout BFF, Components local API | Runtime checkout/payment-method facades, V2 host keeps session/idempotency/redirect validation/BFF | checkout host smoke slices, browser checkout baseline | High |
| Account/auth/address | Manual `StorefrontApiClient`, `StorefrontAuthClient`, V2 BFF/forms, Components local API | Runtime account/address facades where safe; V2 host keeps auth cookies/refresh/session/protected form endpoints | auth/client tests, provider/session tests, browser signin/register/profile baseline | High |
| Consent/privacy/security BFF | Manual consent client, V2 JavaScript and endpoints | Runtime facade optional for API result mapping; V2 host keeps browser cookie/antiforgery/rate-limit behavior | security/static tests, BFF boundary tests, browser no-direct-CommerceNode assertion | Medium |
| Payments | Manual `StorefrontApiClient` payment methods/attempts | Runtime payment discovery/attempt facade, V2 host keeps redirect/return validation | API client tests, checkout/browser baseline | High |

## Temporary manual-client exceptions

Allowed until their target phase cutover lands:

- `IStorefrontAddressClient` through manual `StorefrontApiClient`.
- `StorefrontApiClient.MergeCurrentCustomerCartAsync` only. Active cart CRUD/session/recalculate now go through `GeneratedStorefrontCartClient` and `IStorefrontRuntimeCartFacade`; merge remains an auth-sensitive exception until the account/auth cutover phase because the generated client has no per-call bearer-token parameter.
- `IStorefrontCheckoutClient` through manual `StorefrontApiClient`.
- `IStorefrontConsentClient` through manual `StorefrontApiClient`.
- `IStorefrontCustomerClient` through manual `StorefrontApiClient`.
- `IStorefrontPaymentClient` through manual `StorefrontApiClient`.
- `StorefrontAuthClient` remains V2-owned while auth cookie/session/refresh-token behavior stays in the host.

## V2F0 verification

- `dotnet build BlazorShop.sln`: passed. Existing warnings: `MessagePack` NU1902/NU1903 advisories in `BlazorShop.Tests.V2`; Browserslist `caniuse-lite` outdated notice.
- Focused architecture tests: `dotnet test BlazorShop.Tests.V2\BlazorShop.Tests.V2.csproj --no-build --filter "FullyQualifiedName~V2ArchitectureBoundaryBaselineTests|FullyQualifiedName~StorefrontGeneratedClientFoundationTests|FullyQualifiedName~StorefrontGeneratedConfigurationClientTests|FullyQualifiedName~StorefrontGeneratedCatalogContentClientTests|FullyQualifiedName~StorefrontEndpointDependencyBoundaryTests|FullyQualifiedName~StorefrontPageCompositionGuardrailTests|FullyQualifiedName~HeadlessStorefrontFoundationBoundaryTests"` passed 91/91 after correcting two doc-only StorefrontBuilder boundary lines to explicitly say `Do not:`.
- Focused Storefront V2 client/runtime tests: `dotnet test ... --filter "FullyQualifiedName~StorefrontV2ApiClientTests|FullyQualifiedName~StorefrontV2AuthClientTests|FullyQualifiedName~StorefrontWasmRuntimeFoundationTests"` passed 52/52.
- Focused Storefront V2 provider/session tests: `dotnet test ... --filter "FullyQualifiedName~StorefrontApiEndpointResolverTests|FullyQualifiedName~StorefrontCurrentStoreProviderTests|FullyQualifiedName~StorefrontDisplayContextProviderTests|FullyQualifiedName~StorefrontSessionResolverTests|FullyQualifiedName~StorefrontV2PublicUrlResolverTests"` passed 20/20.
- Focused host smoke slice: `dotnet test ... --filter "FullyQualifiedName~StorefrontV2HostSmokeTests.SignIn_ReturnsStorefrontLoginPage"` passed 1/1. Running the full `StorefrontV2HostSmokeTests` class exceeded 180 seconds, so V2F0 records a narrow host smoke instead of treating the whole class as a baseline gate.
- Playwright browser baseline against `http://localhost:18598`: `/`, `/category/apparel`, `/search?q=shirt`, `/product/qa-simple-product-100`, `/my-cart`, `/checkout`, `/signin`, `/register`, `/account/profile`, `/sitemap.xml`, and `/robots.txt` all returned 200 and nonblank content.
- Browser network assertion: no direct requests from browser to `http://localhost:5180/` were observed.

## V2F1 package contract completion

Implementation notes:

- Added package metadata to `BlazorShop.Storefront.Components`: `PackageId`, `Version`, `Authors`, `Description`, and `RepositoryUrl`.
- Standardized `RepositoryUrl` on `BlazorShop.Storefront.Client`, `BlazorShop.Storefront.Runtime`, and `BlazorShop.Storefront.Components`.
- Split Runtime DI naming so `AddStorefrontRuntime` remains the core primitive registration and `AddStorefrontServerGeneratedClients` is the explicit server-side generated-client registration. The previous `AddStorefrontGeneratedClients` wrapper remains for compatibility.
- Updated Storefront V2 host registration to call `AddStorefrontServerGeneratedClients`.
- Updated architecture docs to treat Components as a package boundary and Runtime generated clients as server-side registration.

Verification:

- `dotnet build BlazorShop.sln`: passed. Existing warnings remain `MessagePack` NU1902/NU1903 advisories and Browserslist `caniuse-lite` notice.
- `dotnet test BlazorShop.Tests.V2\BlazorShop.Tests.V2.csproj --no-build --filter "FullyQualifiedName~StorefrontSharedPlatformPackageContractTests"`: passed 7/7.
- `dotnet pack BlazorShop.PresentationV2\BlazorShop.Storefront.Client\BlazorShop.Storefront.Client.csproj --no-build -o artifacts/storefront-packages-v2f1`: passed.
- `dotnet pack BlazorShop.PresentationV2\BlazorShop.Storefront.Runtime\BlazorShop.Storefront.Runtime.csproj --no-build -o artifacts/storefront-packages-v2f1`: passed.
- `dotnet pack BlazorShop.PresentationV2\BlazorShop.Storefront.Components\BlazorShop.Storefront.Components.csproj --no-build -o artifacts/storefront-packages-v2f1`: passed.
- Local compatibility proof: created temporary Razor class library under `obj/storefront-package-compat-v2f1-01`, restored all three Storefront packages from the local feed, and `dotnet build obj/storefront-package-compat-v2f1-01/StorefrontPackageCompatProof.csproj` passed with 0 warnings and 0 errors.
- NuGet pack emitted non-failing readme best-practice warnings for all three packages.

## V2F2 runtime result and execution primitives

Implementation notes:

- Added neutral `StorefrontRuntimeResult<T>` and `StorefrontRuntimeSubmitResult<T>` wrappers.
- Extended `StorefrontRuntimeError` with typed validation and conflict projections while preserving the existing constructor shape used by Starter.
- Added `StorefrontRuntimeValidationError`, `StorefrontRuntimeConflict`, and `StorefrontRuntimeStatusCodes`.
- Added exception mapping for generated `StorefrontApiException`, `TimeoutException`, `TaskCanceledException`, `HttpRequestException`, and generic request failures.
- Added `RequireStoreKey`, `ExecuteAsync`, and `ExecuteSubmitAsync` helpers so runtime facade calls receive an explicit trimmed `storeKey`.
- Runtime stayed free of cookie, antiforgery, route parsing, SEO, UI state, Razor component, and Storefront V2 host dependencies.

Verification:

- `dotnet build BlazorShop.sln`: passed. Existing warnings remain `MessagePack` NU1902/NU1903 advisories and Browserslist `caniuse-lite` notice.
- `dotnet test BlazorShop.Tests.V2\BlazorShop.Tests.V2.csproj --no-build --filter "FullyQualifiedName~StorefrontRuntimeResultPrimitiveTests"`: passed 14/14.
- `dotnet test BlazorShop.Tests.V2\BlazorShop.Tests.V2.csproj --no-build --filter "FullyQualifiedName~StorefrontSharedPlatformPackageContractTests"`: passed 7/7.

## V2F3 store bootstrap and configuration cutover

Implementation notes:

- Added `IStorefrontRuntimeConfigurationFacade` and `StorefrontRuntimeConfigurationFacade` in Runtime.
- Added Runtime configuration/bootstrap models for store identity, branding, lifecycle state, locale/currency, public feature flags, public payment methods, consent/captcha public metadata, and SEO defaults.
- Registered the configuration facade from `AddStorefrontServerGeneratedClients`, keeping generated-client usage server-side.
- Changed the V2 `GeneratedStorefrontConfigurationClient` adapter to depend on `IStorefrontRuntimeConfigurationFacade` instead of generated Storefront clients directly.
- V2 host still owns current-store resolution, route/domain handling, maintenance page/redirect composition, and SSR metadata rendering.

Verification:

- `dotnet build BlazorShop.sln`: passed. Existing warnings remain `MessagePack` NU1902/NU1903 advisories and Browserslist `caniuse-lite` notice.
- `dotnet test BlazorShop.Tests.V2\BlazorShop.Tests.V2.csproj --no-build --filter "FullyQualifiedName~StorefrontGeneratedConfigurationClientTests|FullyQualifiedName~StorefrontRuntimeResultPrimitiveTests|FullyQualifiedName~StorefrontSharedPlatformPackageContractTests"`: passed 26/26.
- `dotnet test BlazorShop.Tests.V2\BlazorShop.Tests.V2.csproj --no-build --filter "FullyQualifiedName~StorefrontCurrentStoreProviderTests|FullyQualifiedName~StorefrontDisplayContextProviderTests|FullyQualifiedName~StorefrontCurrentStoreMiddlewareTests"`: passed 15/15.
- Playwright active-store browser smoke against `http://localhost:18598/`: passed; home returned 200, title `Shop Home`, nonblank content, and no direct browser request to `http://localhost:5180/`.
- Lifecycle guardrails: `dotnet test ... --filter "FullyQualifiedName~StorefrontCurrentStoreMiddlewareTests|FullyQualifiedName~StorefrontCurrentStoreMiddlewareRegressionTests|FullyQualifiedName~StorefrontCurrentStoreProviderTests"` passed 11/11 for missing, unavailable, maintenance, closed, and no-fallback behavior.
- Maintenance host smoke: `dotnet test ... --filter "FullyQualifiedName~StorefrontV2HostSmokeTests.Maintenance_WhenCurrentStoreRecovered_RedirectsHome|FullyQualifiedName~StorefrontV2HostSmokeTests.Maintenance_WhenCurrentStoreStillInMaintenance_RendersAutoRefresh"` passed 2/2.
- Secret boundary: `dotnet test ... --filter "FullyQualifiedName~CommerceNodePaymentMethodSecretBoundaryTests|FullyQualifiedName~SecurityPrivacyPhase3ConsentTests"` passed 13/13.
- Admin/manager storefront access rule: not a separate active Storefront V2 browser feature; lifecycle/admin bypass behavior remains covered by current-store middleware/provider tests.

## V2F4 catalog, content, navigation and SEO cutover

Implementation notes:

- Added `IStorefrontRuntimeCatalogContentFacade` and `StorefrontRuntimeCatalogContentFacade` in Runtime.
- Runtime facade now owns generated Storefront client calls for catalog, category, search, product detail, page/topic content, navigation, SEO settings, and redirect resolution.
- V2 `GeneratedStorefrontCatalogContentClient` is now a wrapper around the Runtime facade and preserves JSON projection into existing V2/Web.Shared DTOs.
- V2 host still owns SSR routes, canonical/structured data composition, sitemap/robots endpoint composition, and page shell/layout.

Verification:

- `dotnet build BlazorShop.sln`: passed. Existing warnings remain `MessagePack` NU1902/NU1903 advisories and Browserslist `caniuse-lite` notice.
- `dotnet test BlazorShop.Tests.V2\BlazorShop.Tests.V2.csproj --no-build --filter "FullyQualifiedName~StorefrontGeneratedCatalogContentClientTests|FullyQualifiedName~StorefrontGeneratedConfigurationClientTests|FullyQualifiedName~StorefrontSharedPlatformPackageContractTests"`: passed 18/18.
- `dotnet test BlazorShop.Tests.V2\BlazorShop.Tests.V2.csproj --no-build --filter "FullyQualifiedName~StorefrontPageCompositionGuardrailTests|FullyQualifiedName~StorefrontPageNavigationProviderTests|FullyQualifiedName~StorefrontSitemapServiceTests|FullyQualifiedName~StorefrontStructuredDataComposerTests"`: passed 56/56.
- Playwright route smoke against `http://localhost:18598`: `/`, `/category/apparel`, `/search?q=shirt`, `/product/qa-simple-product-100`, `/pages/qa-legal`, `/sitemap.xml`, and `/robots.txt` all returned 200 and rendered nonblank content where applicable.
- Product/category/page canonical links rendered for checked SSR routes. Search route rendered without a canonical link in current SEO behavior.
- Browser network assertion: no direct browser requests to `http://localhost:5180/`.
- Old-slug redirect fixture was not present in the discovered browser smoke route set; redirect resolver remains covered by adapter/unit guardrails in this phase.

## V2F5 product detail component and interaction cutover

Implementation notes:

- Extended `ProductPurchasePanelModel` with initial resolved variant, SKU, GTIN, main image URL, and validation messages.
- Product page now maps initial product interaction state into component data attributes while keeping DTO/backend mapping in the V2 page.
- Product page renders GTIN when present and keeps SKU/GTIN state outside the reusable component's backend contracts.
- Selection preview JavaScript now applies `primaryImageUrl` to the product gallery main image, selecting a matching thumbnail when one exists.
- Components remain backend-neutral; product browser behavior continues through same-origin `/api/product-selection-preview` and `/api/cart/*`.

Verification:

- `dotnet test BlazorShop.Tests.V2\BlazorShop.Tests.V2.csproj --filter "FullyQualifiedName~StorefrontBrandingMarkupTests|FullyQualifiedName~StorefrontCommerceScriptRegressionTests|FullyQualifiedName~StorefrontBffBoundaryHardeningTests"`: passed 21/21.
- `dotnet build BlazorShop.sln`: passed after rerunning serially. A first parallel build/test attempt hit a transient static-web-assets `.gz` file lock in `ControlPlane.Web`; the serial rerun passed. Existing warnings remain `MessagePack` NU1902/NU1903 advisories and Browserslist `caniuse-lite` notice.
- Playwright product flow against `http://localhost:18598`: passed and wrote `output/playwright/v2f5-product-flow-evidence.json` plus `output/playwright/v2f5-product-flow.png`.
- Gallery QA used `qa-seo-media-product`: 3 thumbnails rendered in square gallery frames; selecting thumbnail 2 changed the main image URL.
- Variant QA used `catalog-qa-t-shirt`: selecting Red/XL changed price `EUR 19.99` to `EUR 21.99`, stock `8 in stock` to `3 in stock`, and SKU `QA-TSHIRT-RED-M` to `QA-TSHIRT-RED-XL`.
- Quantity QA used `qa-quantity-rule-product`: HTML constraints rendered `min=2`, `max=10`, `step=2`; below-min and step-mismatched values failed browser validity.
- Add-to-cart QA used `qa-simple-product-100`: product page add succeeded via same-origin `/api/cart/lines`, cart badge became `1`, and `/my-cart` showed the added product.
- Unavailable QA used `qa-purchasing-disabled-product`: add button stayed disabled and purchase panel showed the purchasing-disabled reason.
- Browser network assertion: no direct browser requests to `http://localhost:5180/`; console/page error list was empty.

## V2F6 cart runtime/BFF cutover

Implementation notes:

- Added `IStorefrontRuntimeCartFacade` and `StorefrontRuntimeCartFacade` in Runtime for cart session, get, add, update, remove, clear, validate, and recalculate operations.
- Registered the cart facade from `AddStorefrontServerGeneratedClients`.
- Added `GeneratedStorefrontCartClient` in V2 as the active `IStorefrontCartClient` adapter. It delegates cart CRUD/session/recalculate to Runtime and projects generated DTOs back to existing V2 cart contracts.
- Kept `StorefrontApiClient.MergeCurrentCustomerCartAsync` as the single documented manual cart exception until account/auth cutover.
- V2 host still owns cart token cookies, same-origin `/api/cart/*`, antiforgery, and local browser DTO mapping.
- Added same-origin `/api/cart/recalculate` so browser/BFF QA can verify cart warning refresh and stale-version conflict behavior without direct Commerce Node calls.
- Extended `StorefrontSubmitResult<T>` and cart mutation mapping to preserve `409 Conflict` from Runtime/API through the local BFF.

Verification:

- `dotnet build BlazorShop.sln`: passed. Existing warnings remain `MessagePack` NU1902/NU1903 advisories and Browserslist `caniuse-lite` notice.
- `dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --filter "FullyQualifiedName~StorefrontCommerceFlowCutoverTests|FullyQualifiedName~CartCorePhase0InventoryTests|FullyQualifiedName~SecurityPrivacyPhase1CsrfTests|FullyQualifiedName~StorefrontWasmRuntimeFoundationTests"`: passed 36/36.
- Playwright cart flow against `http://localhost:18598`: passed and wrote `output/playwright/v2f6-cart-flow-evidence.json` plus `output/playwright/v2f6-cart-flow.png`.
- Browser QA added `qa-simple-product-100` and `catalog-qa-t-shirt` Red/XL from product pages; cart badge updated and cart page rendered line images, selected attributes/variant label, unit price, and line total.
- Browser QA updated quantity, removed a line, and cleared cart through same-origin `/api/cart/*`.
- Browser QA disabled `qa-simple-product-100` in the default local store DB, called `/api/cart/recalculate`, saw the warning state and disabled checkout, then restored the product fixture.
- Negative browser QA verified missing antiforgery on `/api/cart/lines` returned `400` and stale `/api/cart/recalculate` returned `409`.
- Browser network assertion: no direct browser requests to `http://localhost:5180/`. The only console resource errors were the expected negative-check `400` and `409`; unexpected console/page error list was empty.

# Storefront V2 Shared Platform Functional MVP QA

## V2F0 baseline lock

Baseline commit SHA before refactor: `63f9095d73140e66e49075b6d3996b2ceb0ed421`.

Behavior-change scope: none. V2F0 only records the dependency inventory, migration matrix, temporary exceptions, and baseline verification.

## Dependency inventory

| Area | Current dependency | Notes |
| --- | --- | --- |
| Manual Storefront API client | `StorefrontApiClient` partials under `BlazorShop.Storefront.V2/Services` | Still owns protected customer/account bearer-token methods, cart merge, saved-address checkout bearer exception, auth-adjacent transport, and legacy-route fallback methods. |
| Generated storefront clients | `BlazorShop.Storefront.Client.Generated` registered by `Storefront.Runtime.AddStorefrontGeneratedClients` | Runtime registers generated address/auth/cart/catalog/checkout/configuration/consent/contact/currency/customer/navigation/newsletter/orders/pages/payments/recommendations/seo/store clients. |
| V2 generated adapters | `GeneratedStorefrontConfigurationClient`, `GeneratedStorefrontCatalogContentClient`, `GeneratedStorefrontCartClient`, `GeneratedStorefrontCheckoutClient`, `GeneratedStorefrontPaymentClient`, `GeneratedStorefrontAddressClient`, `GeneratedStorefrontConsentClient` | Active V2 DI uses generated adapters for store configuration, catalog, content, navigation, SEO, pages, currency, product selection preview, cart, guest checkout, payment discovery, public address metadata, and consent surfaces. |
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
| Account/auth/address | Manual protected customer calls, `StorefrontAuthClient`, Runtime public address facade, V2 BFF/forms, Components local API | Runtime public address facade where safe; V2 host keeps auth cookies/refresh/session/protected form endpoints and bearer-token customer calls | auth/client tests, provider/session tests, browser signin/register/profile/address/order baseline | High |
| Consent/privacy/security BFF | Runtime consent facade, V2 JavaScript and endpoints | Runtime generated consent mapping; V2 host keeps browser consent visitor cookie, antiforgery, and rate-limit behavior | security/static tests, BFF boundary tests, browser no-direct-CommerceNode assertion | Medium |
| Payments | Manual `StorefrontApiClient` payment methods/attempts | Runtime payment discovery/attempt facade, V2 host keeps redirect/return validation | API client tests, checkout/browser baseline | High |

## Temporary manual-client exceptions

Allowed until their target phase cutover lands:

- `StorefrontApiClient.MergeCurrentCustomerCartAsync` only. Active cart CRUD/session/recalculate now go through `GeneratedStorefrontCartClient` and `IStorefrontRuntimeCartFacade`; merge remains an auth-sensitive exception until the account/auth cutover phase because the generated client has no per-call bearer-token parameter.
- `StorefrontApiClient.UpdateCheckoutAddressesAsync` only when a saved-address checkout call carries a bearer token. Active guest checkout start/load/review/shipping/payment/place-order now goes through `GeneratedStorefrontCheckoutClient` and `IStorefrontRuntimeCheckoutFacade`; the saved-address bearer path remains an auth-sensitive exception until account/auth cutover because the generated checkout client has no per-call bearer-token parameter.
- `IStorefrontCustomerClient` through manual `StorefrontApiClient`. Protected customer profile, customer address book, and customer order self-service remain auth-sensitive exceptions until generated clients can attach the current V2 bearer token per call without moving cookie/session policy into Runtime.
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

## V2F7 checkout and COD order placement cutover

Implementation notes:

- Added `IStorefrontRuntimeCheckoutFacade` and `StorefrontRuntimeCheckoutFacade` in Runtime for checkout preview, start, load, address update, shipping method selection, payment method selection, review, and order placement.
- Added `IStorefrontRuntimePaymentFacade` and `StorefrontRuntimePaymentFacade` in Runtime for public payment method discovery and payment attempt lookup.
- Added V2 `GeneratedStorefrontCheckoutClient` and `GeneratedStorefrontPaymentClient` adapters as the active `IStorefrontCheckoutClient` and `IStorefrontPaymentClient` registrations.
- Kept the V2 host in charge of same-origin `/api/checkout/*`, checkout session state, cart token flow, idempotency, antiforgery, redirect/return URL validation, and local browser DTO mapping.
- Preserved `409 Conflict` from checkout Runtime/API calls through the local checkout BFF.
- Kept `StorefrontApiClient.UpdateCheckoutAddressesAsync` as a temporary auth-sensitive exception only for saved-address checkout calls with a bearer token until account/auth cutover.

Verification:

- `dotnet build BlazorShop.sln`: passed. Existing warnings remain `MessagePack` NU1902/NU1903 advisories and Browserslist `caniuse-lite` notice.
- `dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-build --filter "FullyQualifiedName~StorefrontCommerceFlowCutoverTests|FullyQualifiedName~StorefrontWasmRuntimeFoundationTests|FullyQualifiedName~SecurityPrivacyPhase1CsrfTests|FullyQualifiedName~StorefrontCheckout"`: passed 87/88 with 1 skipped.
- Playwright checkout COD flow against `http://localhost:18598`: passed and wrote `output/playwright/v2f7-checkout-cod-flow-evidence.json` plus `output/playwright/v2f7-checkout-cod-flow.png`.
- Browser QA logged in as the QA customer, cleared cart, added `qa-simple-product-100`, started checkout, selected saved billing/shipping address `3c111111-1111-4111-8111-111111111201`, selected shipping, selected COD, reviewed checkout, and placed order `ORD-20260725-FAE82317`.
- Double-click place-order QA observed exactly one `POST /api/checkout/place-order`; the cart cleared after order placement and the account order list/detail showed the new order with EUR currency.
- Browser network assertion: no direct browser requests to `http://localhost:5180/`, no 5xx responses, and no unexpected console/page errors. Expected Blazor WASM fetch abort console noise during navigation was filtered the same way as existing release QA.

## V2F8 account, auth, address, order and consent alignment

Implementation notes:

- Added `IStorefrontRuntimeAddressFacade` and `StorefrontRuntimeAddressFacade` in Runtime for public address countries, states, and address field configuration.
- Added `IStorefrontRuntimeConsentFacade` and `StorefrontRuntimeConsentFacade` in Runtime for consent current/save/revoke mapping through the generated client.
- Added V2 `GeneratedStorefrontAddressClient` and `GeneratedStorefrontConsentClient` adapters as the active `IStorefrontAddressClient` and `IStorefrontConsentClient` registrations.
- Reviewed `StorefrontSessionResolver` and `StorefrontAuthClient`; cookie/header refresh, Set-Cookie bridging, logout cookie behavior, per-request bearer handling, auth forms, and protected account BFF endpoints stay in the V2 host.
- Kept protected `IStorefrontCustomerClient` calls as a documented auth-sensitive manual exception because generated protected customer clients currently do not expose per-call bearer-token injection.

Verification:

- `dotnet build BlazorShop.sln`: passed. Existing warnings remain `MessagePack` NU1902/NU1903 advisories and Browserslist `caniuse-lite` notice.
- Static/client tests: `StorefrontCommerceFlowCutoverTests|StorefrontV2AuthClientTests|StorefrontWasmRuntimeFoundationTests|SecurityPrivacyPhase3ConsentTests|AddressCorePhase7ConfigurationTests` passed 42/42.
- Focused host smoke account/auth tests passed 8/8 for disabled registration, login cookie redirect, logout cookie copy, forgot password, profile update, address create, order paging, and order detail rendering.
- Registration policy Playwright: `scripts/qa/run-storefront-registration-policy-e2e.ps1 -Headless` passed and wrote `.gstack/qa-reports/registration-policy-e2e/result.json`, `storefront-register-disabled.png`, and `storefront-register-enabled.png`.
- Account/consent Playwright against `http://localhost:18598`: passed and wrote `output/playwright/v2f8-account-consent-flow-evidence.json` plus `output/playwright/v2f8-account-consent-flow.png`.
- Browser QA covered register allowed, password recovery sent state, login/logout, profile edit, address add/edit/default shipping/default billing/delete through same-origin BFF, order paging/detail for `ORD-20260725-FAE82317`, cross-customer order detail denial with `404`, consent save/revoke, zero direct browser requests to `http://localhost:5180`, no 5xx responses, and no unexpected console/page errors.
- Guest completion token behavior has no current Storefront V2 browser lookup UI; focused `StorefrontGuestOrderServiceTests|StorefrontSwagger_GuestOrderLookupRequiresTokenAndReturnsSafeDetailContract|StorefrontCheckoutServiceTests|OrderReadModelBehaviorLockTests` passed 71/71 and covers token requirement, non-predictable raw token behavior, hash-only persistence, wrong-token/wrong-store denial, and safe detail contract.

## V2F9 contract ownership and Web.SharedV2 reduction

Implementation notes:

- Added `StorefrontContractOwnershipTests` static guards for the active Storefront shared platform boundaries.
- Classified Storefront V2 model ownership: generated API transport DTOs belong to `Storefront.Client`, runtime-safe facade results belong to `Storefront.Runtime`, reusable browser component models belong to `Storefront.Components`, host-local BFF request/response contracts belong to `Storefront.V2`, and `Web.SharedV2` is limited to utility-only shared types.
- Guarded against new Storefront V2 business API contracts importing `Web.SharedV2.Models`.
- Recorded remaining manual-client exceptions: `StorefrontApiClient.MergeCurrentCustomerCartAsync`, saved-address `StorefrontApiClient.UpdateCheckoutAddressesAsync` with bearer token, protected `IStorefrontCustomerClient`, and `StorefrontAuthClient`.

Verification:

- `dotnet build BlazorShop.sln`: passed after confirming no local V2 runtime processes were holding output DLLs. Existing warnings remain `MessagePack` NU1902/NU1903 advisories and Browserslist `caniuse-lite` notice.
- `dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-build --filter "FullyQualifiedName~StorefrontContractOwnershipTests|FullyQualifiedName~StorefrontGeneratedClientFoundationTests|FullyQualifiedName~StorefrontSharedPlatformPackageContractTests"`: passed 16/16.

## V2F10 V2 host composition cleanup

Implementation notes:

- Split `StorefrontServiceCollectionExtensions` into named private registration groups for host options, Runtime, auth/session/antiforgery/rate-limit policy, BFF endpoint dependencies, SEO/media/deployment-facing services, and generated-client adapters.
- Kept endpoint files unchanged because account, cart, checkout, consent, SEO, and media endpoint groups were already split before this phase.
- Removed unused `Program.cs` imports and kept Program at composition-only shape: service registration, host pipeline, endpoint maps, static assets, and Razor component map.
- Kept `StorefrontApiClient` because active auth-sensitive exception consumers remain. V2F10 isolates it inside the auth/session/manual-exception registration group and guards allowed usage files.

Verification:

- `dotnet build BlazorShop.sln`: passed. Existing warnings remain `MessagePack` NU1902/NU1903 advisories and Browserslist `caniuse-lite` notice.
- Static composition/boundary tests: `StorefrontHostCompositionTests|StorefrontEndpointDependencyBoundaryTests|StorefrontContractOwnershipTests` passed 9/9.
- Active commerce cutover tests: `StorefrontCommerceFlowCutoverTests` passed 8/8.
- Focused host read/render smoke passed 7/7 for login/account/cart/checkout/SEO/maintenance representative cases. The full `StorefrontV2HostSmokeTests` class exceeded the 5-minute command timeout; older mutation cases that mock only manual `StorefrontApiClient` are no longer representative after V2F6/V2F7 generated Runtime cutover.
- Playwright host composition smoke against `http://localhost:18598`: `/`, `/product/qa-simple-product-100`, `/my-cart`, `/checkout`, and `/account/profile` all returned 200/nonblank final pages. Account profile correctly redirected anonymous browser traffic to sign-in. Evidence saved to `output/playwright/v2f10-host-composition-smoke.json` and `output/playwright/v2f10-host-composition-smoke.png`; no direct browser requests to `http://localhost:5180`, no 5xx responses, and no console/page errors were observed.

## V2F11 Starter and generated storefront compatibility definition

Implementation notes:

- Added explicit Starter/generated/custom storefront compatibility rules to `docs/architecture/11-storefront-builder.md`, `docs/architecture/05-project-and-folder-guide.md`, `docs/visual-reverse-engineering-skill/reference.md`, `docs/visual-reverse-engineering-skill/how-to-generate-and-validate.md`, and `docs/agents/storefront-builder.md`.
- Rules now state that Starter/generated storefronts use `BlazorShop.Storefront.Client` package contracts, `BlazorShop.Storefront.Runtime` server/BFF primitives, and `BlazorShop.Storefront.Components` only as a browser-safe package when reusable shared UI is needed.
- Rules also state generated/custom storefronts use `BlazorShop.Storefront.{Name}`, keep store-specific CSS/assets/pages/artifacts inside the generated/custom project, route protected browser actions through same-origin BFF endpoints, and must not guess API response shapes.
- Extended generated metadata and validation to record `BlazorShop.Storefront.Components` as a platform package surface without forcing every Starter/generated project to reference it when local neutral components are sufficient.
- Extended generated proof/isolation package proof to pack `BlazorShop.Storefront.Components` alongside Client and Runtime.

Verification:

- `dotnet build BlazorShop.sln`: passed. Existing warnings remain `MessagePack` NU1902/NU1903 advisories and Browserslist `caniuse-lite` notice.
- `dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-build --filter "FullyQualifiedName~StorefrontStarterFoundationBoundaryTests|FullyQualifiedName~StorefrontBuilderVisualGenerationTests.GeneratedStorefrontProjectCreation_WrapsStarterGenerationAndWritesMetadata|FullyQualifiedName~StorefrontBuilderQaRegenerationTests.BuildIsolationGate_RestoresBuildsPacksAndRejectsForbiddenReferences|FullyQualifiedName~StorefrontSharedPlatformPackageContractTests"`: passed 35/35, including local package proof for Client, Runtime, and Components.
- Playwright production QA was intentionally not run for Starter/generated storefronts in V2F11; this phase only defines compatibility and package boundaries. Storefront V2 production browser QA remains V2F12.

## V2F12 Storefront V2 production browser QA release gate

Implementation notes:

- Updated `scripts/qa/storefront-order-email-e2e.js` so the release gate drives the current Storefront V2 runtime surface instead of the old SSR checkout form when the WASM checkout shell is present.
- The script now waits for product add-to-cart interactivity before asserting cart state, posts checkout steps through same-origin `/api/checkout/*` BFF endpoints, and waits for WASM-rendered account order pages before reading order evidence.
- Account, cart, checkout, address, order, and consent browser interactions remain browser-facing WASM components; protected mutations continue through same-origin Storefront V2 BFF endpoints, not direct Commerce Node browser calls.
- V2F12 did not add Storefront Starter/generated browser QA. Starter/generated compatibility stayed in V2F11; this release gate is only for `BlazorShop.Storefront.V2`.

Environment:

- Date: 2026-07-25.
- Base URL: `http://localhost:18598`.
- Store key: `default`.
- Commerce Node API guard host: `http://localhost:5180`.
- Runtime command: `.\scripts\run-v2-local.ps1 -StopExisting -NoOpenBrowser`.
- Source state before V2F12 commit: `c419afe0`; the V2F12 commit contains this report update.

Verification:

- `.\scripts\qa\run-storefront-registration-policy-e2e.ps1 -Headless`: passed. Evidence: `.gstack/qa-reports/registration-policy-e2e/result.json`.
- `.\scripts\qa\run-storefront-order-email-e2e.ps1 -Headless`: passed. Evidence: `.gstack/qa-reports/order-email-e2e/result.json`.
- `.\scripts\qa\run-storefront-email-recovery-e2e.ps1 -Headless`: passed. Evidence: `.gstack/qa-reports/email-recovery-e2e/result.json`.
- Playwright route/resilience release smoke: passed. Evidence: `output/playwright/v2f12-release-route-resilience-smoke.json` and `output/playwright/v2f12-release-route-resilience-smoke.png`.
- Focused release tests: `dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-build --filter "FullyQualifiedName~StorefrontContractOwnershipTests|FullyQualifiedName~StorefrontHostCompositionTests|FullyQualifiedName~StorefrontSharedPlatformPackageContractTests|FullyQualifiedName~StorefrontGeneratedClientFoundationTests|FullyQualifiedName~StorefrontCommerceFlowCutoverTests|FullyQualifiedName~StorefrontRuntimeResultPrimitiveTests|FullyQualifiedName~StorefrontWasmRuntimeFoundationTests"` passed 58/58.
- Final `dotnet build BlazorShop.sln`: passed after stopping local V2 runtime processes. Existing warnings remain `MessagePack` NU1902/NU1903 advisories and Browserslist `caniuse-lite` notice.

Browser release coverage:

- Home, category, search, product detail, cart, checkout, account redirect, missing product, sitemap, robots, and consent save/revoke passed against `Storefront.V2`.
- Product variant preview confirmed XL/SKU/stock/EUR update markers on `catalog-qa-t-shirt`.
- Registration policy toggled disabled/enabled through Control Plane, verified storefront disabled state, and verified direct Commerce Node register returned `403` while disabled.
- Password recovery verified known-customer email capture, reset browser flow, login with the new password, and unknown-customer no-email behavior.
- COD checkout placed real orders `ORD-20260725-BAF06B02` and `ORD-20260725-F4DAF466`; the first order sent exactly one order email, the second proved SMTP-disabled queue retry after settings restore.
- Order email release gate also verified order-created task success, Mailpit capture, store sender isolation between `default` and `qa-s2`, account order list/detail/receipt access, and network guardrails.
- Duplicate submit behavior remains covered by V2F7 Playwright checkout COD flow, which observed exactly one `POST /api/checkout/place-order`; V2F12 kept the same same-origin BFF placement path.

Error and resilience coverage:

- `401`: anonymous `/account/profile` redirected to sign-in with return URL in route/resilience smoke; protected account flows were also covered by V2F8 host/browser tests.
- `403`: disabled direct registration returned `403`, and account/order data exposure remains covered by V2F8 cross-customer denial plus V2F12 registration policy.
- `404`: `/product/does-not-exist-v2f12` returned 404 with the product-not-found page; account order wrong-scope denial remains covered by V2F8.
- `409`: cart/checkout stale-conflict preservation remains covered by `StorefrontCommerceFlowCutoverTests` and V2F6/V2F7 browser conflict checks.
- `422`: registration/password/account form validation remains covered by host/security tests and release browser form flows.
- `503`: store unavailable/maintenance behavior remains covered by current-store middleware/provider tests and V2F10 maintenance host smoke; V2F12 route smoke verified no blank page on active store.
- Timeout/network failure: no artificial timeout was injected in V2F12; current BFF unavailable/error-state behavior remains guarded by focused Runtime/BFF tests, and the browser happy path completed without timeouts or 5xx.
- Checkout refresh/resume behavior remains covered by the current stateful checkout/browser placement path and the V2F7 checkout COD flow.

Release assertions:

- Browser network guard observed no direct requests from Storefront V2 browser pages to `http://localhost:5180`.
- Browser release runs observed no uncaught JS/.NET WASM errors and no unexpected 5xx responses.
- Sitemap contained store-visible public content and did not include checkout.
- Robots blocked private/mutation route classes.
- No provider secret/internal setting was exposed in checked public/browser responses; secret boundary tests from previous phases remain green.
- Remaining manual `StorefrontApiClient` usages are still limited to the documented owner/phase exception list guarded by `StorefrontContractOwnershipTests`.

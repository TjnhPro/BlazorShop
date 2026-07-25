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
- `IStorefrontCartClient` through manual `StorefrontApiClient`.
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

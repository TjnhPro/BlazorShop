# Headless Storefront Platform Foundation Dependency Audit

Date: 2026-07-24
Phase: F1 Current Dependency Audit
Scope: `BlazorShop.PresentationV2/BlazorShop.Storefront.V2`, `BlazorShop.Storefront.Components`, `BlazorShop.Storefront.WASM`, and Commerce Node Storefront public contract/mapping surface.

## Audit Commands

```powershell
rg -n "using BlazorShop\.Application|BlazorShop\.Application" BlazorShop.PresentationV2\BlazorShop.Storefront.V2
rg -n "BlazorShop\.Web\.SharedV2|Web\.SharedV2\.Models" BlazorShop.PresentationV2\BlazorShop.Storefront.V2
rg -n "Application|Domain|Infrastructure|CommerceNode.API|ControlPlane.API" BlazorShop.PresentationV2\BlazorShop.Storefront.Components BlazorShop.PresentationV2\BlazorShop.Storefront.WASM
rg -n "BlazorShop\.Application|Application\.DTOs|ApplicationResult|Domain|Infrastructure|Web\.SharedV2" BlazorShop.PresentationV2\BlazorShop.CommerceNode.API\Contracts\Storefront BlazorShop.PresentationV2\BlazorShop.CommerceNode.API\Controllers\Storefront
```

## Project Reference Inventory

| Reference | Current classification | Current use | Problem | Replacement | Target owner | Migration phase |
| --- | --- | --- | --- | --- | --- | --- |
| `BlazorShop.Application` | E. Backend-only business logic and A. transitional public API contract source | DTO imports, service interfaces, options, diagnostics event names, current handwritten client and endpoint support | Direct backend/core compile-time dependency from Storefront V2 | Generated Storefront client DTOs plus Storefront V2 presentation view models; keep backend services only behind Commerce Node API | `Storefront.Client`, `Storefront.V2`, Commerce Node API mapping layer | F3, F5.1-F5.6, final F5 cleanup |
| `BlazorShop.Web.SharedV2` | F. shared hosting/browser utility and A. transitional business DTO bucket | shared API response helpers, storage/session utilities, product/category/page/SEO/payment/discovery models | Mixes frontend utility sharing with Storefront business model coupling | Keep only genuinely shared utilities; move business read models to generated client or Storefront V2 view models | `Web.SharedV2` utilities, `Storefront.Client`, `Storefront.V2` | F4, F5.1-F5.6, final F5 cleanup |
| `BlazorShop.ServiceDefaults` | F. shared hosting/observability | common hosting defaults | Allowed runtime infrastructure dependency | Keep unless a packaging/isolation phase chooses a different hosting package model | `ServiceDefaults` | F7 review |
| `BlazorShop.Storefront.Components` | D. frontend presentation | portable Razor feature components | Allowed; already backend-independent | Keep as presentation-only component dependency | `Storefront.Components` | F5/F7 guard |
| `BlazorShop.Storefront.WASM` | D. frontend presentation | interactive browser assembly | Allowed; already depends only on Components | Keep behind same-origin BFF | `Storefront.WASM` | F4/F7 guard |

## Storefront V2 Backend Namespace Inventory

Current `BlazorShop.Application` namespaces/tokens in Storefront V2:

- `BlazorShop.Application.CommerceNode.Catalog`
- `BlazorShop.Application.CommerceNode.Navigation`
- `BlazorShop.Application.CommerceNode.StorefrontPages`
- `BlazorShop.Application.CommerceNode.VariationTemplates`
- `BlazorShop.Application.Diagnostics`
- `BlazorShop.Application.DTOs.Category`
- `BlazorShop.Application.DTOs.Payment`
- `BlazorShop.Application.DTOs.Seo`
- `BlazorShop.Application.DTOs.UserIdentity`
- `BlazorShop.Application.Options`
- `BlazorShop.Application.Services`
- `BlazorShop.Application.Services.Contracts`

Current `BlazorShop.Web.SharedV2` model namespaces/tokens in Storefront V2:

- `BlazorShop.Web.SharedV2`
- `BlazorShop.Web.SharedV2.Models`
- `BlazorShop.Web.SharedV2.Models.Category`
- `BlazorShop.Web.SharedV2.Models.Discovery`
- `BlazorShop.Web.SharedV2.Models.Pages`
- `BlazorShop.Web.SharedV2.Models.Payment.ProcessCart`
- `BlazorShop.Web.SharedV2.Models.Product`
- `BlazorShop.Web.SharedV2.Models.Seo`

Browser projects:

- `BlazorShop.Storefront.Components` has no backend/runtime project reference hit. The only text hit is `Features/README.md`, which is a boundary rule.
- `BlazorShop.Storefront.WASM` has no `Application`, `Domain`, `Infrastructure`, Commerce Node API, or Control Plane API hits.

## Storefront V2 Source Inventory

| Current type/service | Current owner | Used by | Problem | Replacement | Target owner | Migration phase |
| --- | --- | --- | --- | --- | --- | --- |
| `BlazorShop.Storefront.V2.csproj` references | Storefront V2 host | Storefront V2 build | Direct `Application` and business-model `Web.SharedV2` references block independent frontend build | Add `Storefront.Client`, remove backend refs after capability migration, keep only allowed shared utilities if justified | Storefront V2 | F5 final cleanup |
| `Dockerfile` copies `BlazorShop.Application` and `Web.SharedV2` | Storefront V2 deployment | container build | Storefront image build still needs backend/core source | Copy generated client/runtime/component projects only after refs are removed | Storefront V2 deployment | F5 final cleanup, F7 |
| `_Imports.razor` | Storefront V2 host | Razor pages/components | Globalizes backend DTO namespaces into presentation | Narrow imports to Storefront-owned view models/generated client types | Storefront V2 | F5.1-F5.6 |
| `Program.cs` and `Configuration/*` | Storefront V2 host | DI/options/hosting | Uses Application options, diagnostics, and service contracts directly | Move options that are storefront-host concerns locally; consume generated client registration | Storefront V2, optional Runtime | F3, F5.1, F6 |
| `StorefrontApiClient.*` | Storefront V2 services | SSR and BFF capability clients | Handwritten transport mirrors Storefront API and aliases backend DTOs | Generated C# client from Storefront OpenAPI; optional thin typed facades | `Storefront.Client` | F3, F5 capability migration |
| `StorefrontApiTransport` | Storefront V2 services | handwritten client transport | Transport/error parsing is tied to Storefront V2 | Generated client transport/error handling with hooks | `Storefront.Client` | F3 |
| `StorefrontApiRoutes` | Storefront V2 services | handwritten route construction | Duplicates API route knowledge | Generated route methods include `storeKey`; keep only local route helpers if needed | `Storefront.Client`, Storefront V2 | F3 |
| `Services/Contracts/*Contracts.cs` | Storefront V2 contracts | client interfaces, pages, endpoints | Many files duplicate public API request/response DTOs and import Application/Web.SharedV2 models | Generated DTOs for public API; keep local view models only for presentation/composition | `Storefront.Client`, Storefront V2 | F5.1-F5.6 |
| `Services/Contracts/IStorefront*Client.cs` | Storefront V2 contracts | DI abstractions and pages/endpoints | Capability interfaces currently expose backend/shared DTOs | Facades over generated client with generated DTOs or Storefront presentation models | `Storefront.Client`, Storefront V2 | F3, F5 capability migration |
| `Services/Contracts/IStorefrontAuthClient.cs` and `StorefrontAuthClient` | Storefront V2 auth service | auth form endpoints/pages | Uses Application auth DTOs and token response shape | Generated Storefront auth client plus local form models and HttpOnly cookie handling | Storefront V2 BFF/auth, `Storefront.Client` | F5.3 |
| `StorefrontCartTokenService` | Storefront V2 BFF service | cart/session bridge | Uses `ProcessCart` and cart DTOs from Web.SharedV2/Application-linked contracts | Generated cart DTOs plus local cart token/session result models | Storefront V2 BFF | F5.4 |
| `StorefrontNavigationProvider`, `StorefrontPageNavigationProvider`, `StorefrontPagePresentationResolver` | Storefront V2 composition | layout/footer/content pages | Uses Application navigation/page contracts and Web.Shared page models | Generated navigation/page DTOs; local presentation-only navigation/page models if composition needs them | Storefront V2 | F5.2 |
| `StorefrontSeoComposer`, `StorefrontSeoSettingsProvider`, `StorefrontStructuredDataComposer`, `StorefrontIndexingPolicy` | Storefront V2 SEO | page head, sitemap, structured data | Uses Application SEO DTOs and Web.SharedV2 product/category/page models | Generated SEO/catalog/page DTOs mapped to presentation metadata models | Storefront V2 | F5.2 |
| `StorefrontSitemapService` and SEO endpoints | Storefront V2 SEO endpoints | sitemap/robots/discovery | Uses Web.SharedV2 discovery models and Application diagnostics | Generated discovery/SEO data plus local sitemap models; local diagnostics constants if needed | Storefront V2 | F5.2 |
| `StorefrontProductSummaryMapper` | Storefront V2 presentation mapper | catalog/product cards | Uses Web.SharedV2 product models | Map generated catalog/product DTOs to `Storefront.Components` presentation models | Storefront V2 | F5.2 |
| BFF endpoint groups `/api/cart`, `/api/account/*`, `/api/checkout/*`, `/api/consent/*`, `/api/media/*`, SEO helpers | Storefront V2 endpoints | browser/WASM same-origin calls | Endpoint files import Application/Web.SharedV2 support helpers and local DTOs are mixed into support files | Capability-specific local contracts; generated client behind capability abstractions; central error mapping | Storefront V2 BFF | F4, F5.3-F5.6 |
| Razor pages `CategoryPage`, `ProductPage`, `SearchPage`, `CheckoutPage` and `StorefrontHeader` | Storefront V2 pages/layout | SSR/hybrid UI | Direct backend/shared DTO imports in presentation | Generated DTOs via services or local view models; keep routes/design stable | Storefront V2 | F5.2, F5.5 |

## Handwritten Storefront API Client Inventory

Current files:

- `StorefrontApiClient.cs`
- `StorefrontApiClient.Address.cs`
- `StorefrontApiClient.Cart.cs`
- `StorefrontApiClient.Catalog.cs`
- `StorefrontApiClient.Checkout.cs`
- `StorefrontApiClient.Configuration.cs`
- `StorefrontApiClient.Consent.cs`
- `StorefrontApiClient.Content.cs`
- `StorefrontApiClient.Customer.cs`
- `StorefrontApiClient.Payment.cs`
- `StorefrontApiTransport.cs`
- `StorefrontApiRoutes.cs`

Current capability interfaces:

- `IStorefrontAddressClient`
- `IStorefrontAuthClient`
- `IStorefrontCartClient`
- `IStorefrontCatalogClient`
- `IStorefrontCheckoutClient`
- `IStorefrontConsentClient`
- `IStorefrontContentClient`
- `IStorefrontCustomerClient`
- `IStorefrontPaymentClient`
- `IStorefrontStoreConfigurationClient`

Migration order:

1. Keep the current interfaces while generated client compiles.
2. Add generated client facades by capability.
3. Replace each interface method return/request type with generated DTOs or presentation view models in the matching F5 subphase.
4. Delete handwritten route/transport/client code after no capability depends on it.

## Local BFF Endpoint Contract Inventory

Endpoint groups currently present:

- `/api/account/*` in `StorefrontAccountEndpoints.cs` and `StorefrontAuthFormEndpoints.cs`
- `/api/cart` and `/api/product-selection-preview` in `StorefrontCartEndpoints.cs`
- `/api/checkout/*` in `StorefrontCheckoutEndpoints.cs`
- `/api/consent/*` in `StorefrontConsentEndpoints.cs`
- `/api/media/*` in `StorefrontMediaEndpoints.cs`
- SEO/sitemap/robots helpers in `StorefrontSeoEndpoints.cs`

Local DTOs still mixed into endpoint support:

- `StorefrontLocalCartLineRequest`
- `StorefrontLocalProductSelectionPreviewRequest`
- `StorefrontLocalProductSelectionPreviewResponse`
- `StorefrontLocalCartQuantityRequest`

Migration target:

- Move local endpoint request/response shapes to capability-specific local contract files.
- Keep browser response shapes stable for WASM.
- Keep BFF responsibilities limited to store/session/token/antiforgery/error normalization.

## Commerce Node Storefront Contract Inventory

Commerce Node already owns public Storefront contracts under `BlazorShop.CommerceNode.API/Contracts/Storefront`.

Current split:

- `*Contracts.cs` files define public API request/response schemas.
- `*Mappings.cs` files map to/from Application service contracts and DTOs.
- Storefront controllers call Application services and return public Storefront contract DTOs.

Observed transitional coupling:

- Each `*Contracts.cs` file still imports shared Application namespaces for aliases/common types.
- Each `*Mappings.cs` file depends on Application DTOs/service results and `BlazorShop.Domain.Contracts` for backend result handling.
- Storefront controllers use Application services and `ApplicationResult<T>` behind the API boundary.

Migration rule:

- F2 should keep Application DTO usage behind Commerce Node mapping code where needed.
- Public schemas generated from Storefront OpenAPI must remain Commerce Node API boundary contracts, not Application/domain entities.
- Backend-only Application/Domain references inside Commerce Node controllers and mapping code are allowed because Commerce Node is the backend boundary.

## Capability Migration Table

| Capability | Current types/services | Current owner | Used by | Problem | Replacement | Target owner | Migration phase |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Store bootstrap/configuration | `ConfigurationContracts`, `IStorefrontStoreConfigurationClient`, `StorefrontApiClient.Configuration`, `StorefrontDisplayContextProvider`, `StorefrontClientAppUrlResolver` | Storefront V2 + Application options | app startup, layout, maintenance/currency/culture | Flat flags and Application options couple frontend host to backend/shared contracts | Generated public configuration DTOs with capability projection; local host options | Commerce Node API contracts, `Storefront.Client`, Storefront V2 | F2.4, F5.1 |
| Catalog/product/search | `CatalogContracts`, `IStorefrontCatalogClient`, `StorefrontApiClient.Catalog`, catalog pages, product summary mapper | Storefront V2 + Web.SharedV2/Application DTOs | home/category/product/search/deals | API read models and presentation models are mixed | Generated catalog/product DTOs plus component presentation models | `Storefront.Client`, Storefront V2, Storefront.Components | F5.2 |
| Content/navigation/SEO | `StorefrontNavigationProvider`, `StorefrontPageNavigationProvider`, `StorefrontPagePresentationResolver`, SEO services, sitemap | Storefront V2 + Web.SharedV2/Application DTOs | header/footer/pages/metadata/sitemap | Navigation/page/SEO read contracts come from Application/Web.SharedV2 | Generated page/navigation/SEO DTOs; local SEO metadata models | `Storefront.Client`, Storefront V2 | F5.2 |
| Auth/customer/account | `IStorefrontAuthClient`, `StorefrontAuthClient`, `CustomerContracts`, account endpoints | Storefront V2 + Application auth DTOs | SSR auth forms and WASM account BFF | Auth contracts and token response are backend/shared DTOs | Generated auth/customer DTOs; local form and HttpOnly session models | `Storefront.Client`, Storefront V2 BFF | F5.3 |
| Cart | `CartContracts`, `IStorefrontCartClient`, `StorefrontCartTokenService`, cart endpoints/local DTOs | Storefront V2 + Web.SharedV2 payment cart model | guest/customer cart and product-selection preview | Cart token/session bridge mixes BFF state with public cart API DTOs | Generated cart DTOs plus local BFF request/result models | `Storefront.Client`, Storefront V2 BFF | F5.4 |
| Checkout/orders/payment result | `CheckoutContracts`, `PaymentContracts`, `OrderContracts`, checkout endpoints/pages, payment client | Storefront V2 + Application payment DTOs | checkout, place order, order history/detail, payment result | Checkout/order/payment API DTOs are handwritten and tied to backend/shared models | Generated checkout/order/payment DTOs; local checkout UI state only | `Storefront.Client`, Storefront V2 BFF | F5.5 |
| Consent/newsletter/contact/recommendations | `ConsentContracts`, consent endpoints, Commerce Node communications/recommendations controllers | Storefront V2 + Commerce Node API contracts | browser consent and optional public flows | Remaining public contracts need generated coverage and capability flags | Generated consent/communication/recommendation DTOs; local cookie bridge | `Storefront.Client`, Storefront V2 BFF | F5.6 |
| Hosting/observability | `ServiceDefaults`, Application diagnostics names, rate limit identity/session helpers | shared hosting + Storefront V2 | startup, logging, rate limiting | Some diagnostics constants come from Application solely for naming | Keep ServiceDefaults; move storefront-only diagnostics constants locally if needed | Storefront V2, optional Runtime | F5 cleanup/F6 |
| Browser utilities | `Web.SharedV2` root helpers, storage/session/toast/api helpers | Web.SharedV2 | endpoint helpers and session/response handling | Allowed only if genuinely shared, but business models must be removed | Keep utility dependency only if architecture docs allow; otherwise move local | Web.SharedV2 utilities or Storefront V2 | F4/F5 cleanup |

## Migration Order

1. Harden OpenAPI first so generated clients are safe.
2. Generate the C# client and keep it independent.
3. Harden BFF/local endpoint contracts without changing browser routes.
4. Migrate Storefront V2 capability-by-capability in the order defined by F5.1-F5.6.
5. Remove `Application` project reference, then remove or justify `Web.SharedV2`.
6. Update Dockerfile after project references are removed.
7. Enforce final dependency rules in F7.

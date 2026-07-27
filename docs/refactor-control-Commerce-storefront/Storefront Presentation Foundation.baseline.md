# Storefront Presentation Foundation Baseline

Date: 2026-07-26

Scope: SPF0 baseline before creating `BlazorShop.Storefront.Presentation`.

## Git Status Before Project Creation

```text
?? docs/refactor-control-Commerce-storefront/Storefront Presentation Foundation.todo.md
```

The todo file was already present as an untracked planning artifact at the start of SPF0. No runtime source files were changed before the baseline inventory.

## Route Page Inventory

Command:

```powershell
rg -n "^@page" BlazorShop.PresentationV2/BlazorShop.Storefront.V2 BlazorShop.PresentationV2/BlazorShop.Storefront.Starter -g "*.razor"
```

V2 route ownership baseline:

| Responsibility | Route(s) | File |
| --- | --- | --- |
| home | `/` | `BlazorShop.Storefront.V2/Pages/Hybrid/Catalog/Home.razor` |
| category | `/category/{Slug}` | `BlazorShop.Storefront.V2/Pages/Hybrid/Catalog/CategoryPage.razor` |
| product | `/product/{Slug}` | `BlazorShop.Storefront.V2/Pages/Hybrid/Catalog/ProductPage.razor` |
| search | `/search` | `BlazorShop.Storefront.V2/Pages/Hybrid/Catalog/SearchPage.razor` |
| deals | `/todays-deals` | `BlazorShop.Storefront.V2/Pages/Hybrid/Catalog/TodaysDeals.razor` |
| new releases | `/new-releases` | `BlazorShop.Storefront.V2/Pages/Hybrid/Catalog/NewReleases.razor` |
| content | `/pages/{Slug}` | `BlazorShop.Storefront.V2/Pages/Ssr/Content/StorefrontPage.razor` |
| auth | `/signin`, `/register`, `/forgot-password`, `/reset-password` | `BlazorShop.Storefront.V2/Pages/Ssr/Auth/*` |
| cart | `/my-cart` | `BlazorShop.Storefront.Presentation/Pages/Ssr/Cart/CartRoutePage.razor` |
| checkout | `/checkout` | `BlazorShop.Storefront.Presentation/Pages/Hybrid/Commerce/CheckoutRoutePage.razor` |
| payment | `/payment-success`, `/payment-cancel`, `/payment/result` | `BlazorShop.Storefront.Presentation/Pages/Hybrid/Commerce/PaymentResultRoutePage.razor` |
| account host | `/account`, `/account/{*Path}` | `BlazorShop.Storefront.Presentation/Pages/WasmHost/Account/AccountRoutePage.razor`; V2 supplies `AccountPage` view |
| system | `/maintenance`, `/{*Path:nonfile}` | `BlazorShop.Storefront.V2/Pages/Ssr/System/*` |

Starter route ownership baseline:

| Responsibility | Route(s) | File |
| --- | --- | --- |
| home | `/` | `BlazorShop.Storefront.Starter/Pages/Ssr/Home/HomePage.razor` |
| category | `/category/{Slug}` | `BlazorShop.Storefront.Starter/Pages/Hybrid/Catalog/CategoryPage.razor` |
| product | `/product/{Slug}` | `BlazorShop.Storefront.Starter/Pages/Hybrid/Catalog/ProductPage.razor` |
| search | `/search` | `BlazorShop.Storefront.Starter/Pages/Hybrid/Catalog/SearchPage.razor` |
| deals | `/deals` | `BlazorShop.Storefront.Starter/Pages/Hybrid/Commerce/DealsPage.razor` |
| content | `/content/{Slug}` | `BlazorShop.Storefront.Starter/Pages/Ssr/Content/ContentPage.razor` |
| auth | `/signin` | `BlazorShop.Storefront.Starter/Pages/Ssr/Auth/AuthShellPage.razor` |
| cart | `/cart` | `BlazorShop.Storefront.Starter/Pages/Hybrid/Commerce/CartPage.razor` |
| checkout | `/checkout` | `BlazorShop.Storefront.Presentation/Pages/Hybrid/Commerce/CheckoutRoutePage.razor`; Starter supplies `CheckoutPage` view |
| payment | `/payment/result` | `BlazorShop.Storefront.Presentation/Pages/Hybrid/Commerce/PaymentResultRoutePage.razor`; Starter supplies `PaymentResultPage` view |
| account host | `/account`, `/account/{*Path}` | `BlazorShop.Storefront.Presentation/Pages/WasmHost/Account/AccountRoutePage.razor`; Starter supplies `AccountPage` view |
| system | `/maintenance`, `/not-found` | `BlazorShop.Storefront.Starter/Pages/Ssr/System/*` |

## Endpoint Mapping Inventory

Command:

```powershell
rg -n "MapStorefront|MapStarter|MapGet|MapPost" BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Endpoints BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/Endpoints
```

V2 endpoint ownership baseline:

- Auth form endpoints: `POST /signin`, `/register`, `/forgot-password`, `/reset-password`, `/logout`, and currency preference through `StorefrontAuthFormEndpoints`; checkout/account form posts are retired in favor of Presentation BFF endpoints.
- Cart BFF endpoints: `GET /api/cart`, `POST /api/product-selection-preview`, `POST /api/cart/lines`, `PUT /api/cart/lines/{lineId:guid}`, `DELETE /api/cart/lines/{lineId:guid}`, `DELETE /api/cart`, `POST /api/cart/recalculate` now live in `BlazorShop.Storefront.Presentation`.
- Checkout BFF endpoints: `GET /api/checkout`, `POST /api/checkout/addresses`, `/shipping-method`, `/payment-method`, `/review`, `/place-order` now live in `BlazorShop.Storefront.Presentation`.
- Account BFF endpoints: profile, addresses, orders, order receipt, and change password under `/api/account/*` now live in `BlazorShop.Storefront.Presentation/Endpoints/StorefrontPresentationAccountEndpoints.cs`.
- Consent endpoints: `GET /api/consent/current`, `POST /api/consent`, `POST /api/consent/revoke`.
- SEO endpoints: `GET /robots.txt`, `GET /sitemap.xml`.
- Media endpoints: product and asset media proxy routes under `/media/*`.

Starter endpoint ownership baseline:

- `StarterSeoEndpoints`: `GET /robots.txt`, `GET /sitemap.xml`.
- `StarterBffEndpoints`: `POST /api/starter/interaction`.

## Guardrail Tests Added

- `StorefrontPageCompositionGuardrailTests.StarterPageInventory_RecordsCurrentSecondConsumerBaseline` locks the Starter page/route baseline before route migration.
- `StorefrontBffBoundaryHardeningTests.LocalEndpointRouteInventory_RecordsCurrentBrowserContracts` locks browser-facing local endpoint route literals, HTTP methods, and local response envelope types.

Existing characterization coverage remains active for:

- V2 route URL declarations.
- noindex/private route policy.
- product not-found and service-unavailable status behavior through `StorefrontV2HostSmokeTests`.
- cart/checkout/account endpoint antiforgery and local envelope behavior through focused Storefront V2 tests.

## QA Fixture Availability

`docs/refactor-control-Commerce-storefront/QA-StorefrontV2.todo.md` records current real browser fixtures for COD order placement, registration policy, account profile/address/order flows, product/cart flows, robots, sitemap, and browser network guardrails. The latest recorded release checks were completed on 2026-07-26.

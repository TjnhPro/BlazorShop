# Storefront Presentation Foundation

Muc tieu Phase 1: tao `BlazorShop.Storefront.Presentation` lam application engine chay that cho storefront, trong khi `Storefront.V2`, `Storefront.Starter`, va `Storefront.{Name}` chi con la host + visual implementation.

Phase nay khong tao theme engine hoan chinh. Phase nay chi tao foundation view adapter co dinh de Presentation khong reference V2/Starter nhung van render duoc visual cua tung host.

## Current Verified Context

- [x] Chua co project `BlazorShop.Storefront.Presentation`.
- [x] Active Storefront projects hien co:
  - `BlazorShop.Storefront.Client`
  - `BlazorShop.Storefront.Runtime`
  - `BlazorShop.Storefront.Components`
  - `BlazorShop.Storefront.V2`
  - `BlazorShop.Storefront.V2.WASM`
  - `BlazorShop.Storefront.Starter`
- [x] `BlazorShop.Storefront.Components` hien da dung `Microsoft.NET.Sdk`, khong con Razor SDK, va chi reference `Microsoft.JSInterop`.
- [x] `BlazorShop.Storefront.V2.WASM` chi reference `BlazorShop.Storefront.Components`; no khong reference Runtime/Client/Presentation.
- [x] Runtime compatibility aliases `AddStorefrontServerGeneratedClients` va `AddStorefrontGeneratedClients` da bi remove; active surface la `AddStorefrontPlatformRuntime` va `AddStorefront{Capability}Runtime`.
- [x] `Storefront.V2` van reference truc tiep:
  - `ServiceDefaults`
  - `Storefront.Client`
  - `Storefront.Components`
  - `Storefront.Runtime`
  - `Storefront.V2.WASM`
- [x] `Storefront.V2/Program.cs` van map rieng:
  - auth form endpoints
  - cart endpoints
  - account endpoints
  - checkout endpoints
  - consent endpoints
  - SEO endpoints
  - media endpoints
  - Razor Components root `App`
- [x] `Storefront.V2` van co `App.razor`, `Routes.razor`, route pages trong `Pages/Ssr`, `Pages/Hybrid`, `Pages/WasmHost`.
- [x] `Storefront.Starter` van co `Components/App.razor`, `Components/Routes.razor`, route pages, `StarterBffEndpoints`, va `StarterSeoEndpoints`.
- [x] `ProductPage.razor` V2 la ung vien tach dau tien vi dang tron route, API call, SEO/status, structured data, breadcrumbs, gallery/purchase mapping, related products, markup, CSS classes va final copy trong cung file.

## Target Ownership

```text
BlazorShop.Storefront.Presentation
  -> BlazorShop.Storefront.Runtime
      -> BlazorShop.Storefront.Client

BlazorShop.Storefront.Presentation
  -> BlazorShop.Storefront.Components

BlazorShop.Storefront.V2
  -> BlazorShop.Storefront.Presentation
  -> BlazorShop.Storefront.V2.WASM
      -> BlazorShop.Storefront.Components

BlazorShop.Storefront.Starter
  -> BlazorShop.Storefront.Presentation

BlazorShop.Storefront.{Name}
  -> BlazorShop.Storefront.Presentation
```

Forbidden:

```text
Presentation -> Storefront.V2
Presentation -> Storefront.V2.WASM
Presentation -> ServiceDefaults
Presentation -> CommerceNode.API / ControlPlane.API / Application / Domain / Infrastructure

Storefront.V2.WASM -> Presentation
Storefront.V2.WASM -> Runtime
Storefront.V2.WASM -> Client

Components -> Presentation
Components -> Runtime
Components -> Client
```

## Non Goals

- [ ] Khong doi Commerce Node Storefront API contract.
- [ ] Khong doi cart/checkout/order/payment business truth.
- [ ] Khong redesign V2.
- [ ] Khong copy V2 visual sang Starter.
- [ ] Khong tao theme manifest, template hierarchy, override resolution, hooks, analyzer trong phase nay.
- [ ] Khong dua `ServiceDefaults` vao Presentation.
- [ ] Khong dua Runtime/Client vao WASM.
- [ ] Khong dua visual Razor wrappers tro lai `Storefront.Components`.

## Phase SPF0 - Baseline and Characterization Lock

- [x] Ghi lai `git status --short` truoc khi tao project.
- [x] Inventory route pages hien tai:

```powershell
rg -n "^@page" BlazorShop.PresentationV2/BlazorShop.Storefront.V2 BlazorShop.PresentationV2/BlazorShop.Storefront.Starter -g "*.razor"
```

- [x] Inventory endpoint mappings hien tai:

```powershell
rg -n "MapStorefront|MapStarter|MapGet|MapPost" BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Endpoints BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/Endpoints
```

- [x] Inventory V2 page responsibilities:
  - product
  - category
  - search
  - deals
  - new releases
  - content
  - auth
  - cart
  - checkout
  - payment success/cancel
  - account host
  - maintenance/not-found
- [x] Add characterization tests before moving behavior:
  - route URL still exists;
  - noindex pages still emit noindex;
  - product 404 still sets 404;
  - service unavailable still sets 503;
  - cart/checkout/account endpoints keep same routes and envelopes.
- [x] Confirm current QA fixtures for real COD order placement and account flows are available.

2026-07-26 SPF0 evidence:

- Baseline recorded in `docs/refactor-control-Commerce-storefront/Storefront Presentation Foundation.baseline.md`.
- Added `StorefrontPageCompositionGuardrailTests.StarterPageInventory_RecordsCurrentSecondConsumerBaseline`.
- Added `StorefrontBffBoundaryHardeningTests.LocalEndpointRouteInventory_RecordsCurrentBrowserContracts`.
- QA fixture availability confirmed from `QA-StorefrontV2.todo.md` release evidence for COD order placement and account flows.
- Verification: `dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --filter "FullyQualifiedName~StorefrontPageCompositionGuardrailTests|FullyQualifiedName~StorefrontBffBoundaryHardeningTests" --no-restore` passed 57/57 with existing MessagePack vulnerability and Browserslist warnings.

Exit criteria:

- [x] Co route/endpoint inventory lam baseline.
- [x] Test suite co guardrail fail neu route/endpoint bi doi vo y.

## Phase SPF1 - Create Presentation Project and Dependency Guardrails

- [x] Tao project:

```text
BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/
```

- [x] Project SDK:

```xml
<Project Sdk="Microsoft.NET.Sdk.Razor">
```

- [x] Target framework va package metadata dong bo voi Storefront packages:
  - `net10.0`
  - nullable enabled
  - implicit usings enabled
  - package id `BlazorShop.Storefront.Presentation`
- [x] Add project references chi toi:
  - `BlazorShop.Storefront.Runtime`
  - `BlazorShop.Storefront.Components`
- [x] Add framework/package references can thiet cho server-side Razor components va ASP.NET Core endpoint extensions.
- [x] Add project vao solution neu solution file dang quan ly active V2 projects.
- [x] Tao thu muc ban dau:

```text
App/
Routing/
Pages/
PagePatterns/
Services/
Seo/
Endpoints/
Security/
Hosting/
Views/Foundation/
DependencyInjection/
```

- [x] Them architecture tests:
  - Presentation references Runtime and Components.
  - Presentation does not reference V2, V2.WASM, Starter, ServiceDefaults, backend/core/API projects.
  - V2.WASM does not reference Presentation, Runtime, or Client.
  - Components does not reference Presentation/Runtime/Client and has no `.razor`.

2026-07-26 SPF1 evidence:

- Created `BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/BlazorShop.Storefront.Presentation.csproj` with `Microsoft.NET.Sdk.Razor`, `net10.0`, package metadata, `Microsoft.AspNetCore.App`, and only Runtime/Components project references.
- Added foundation folders and initial `AddStorefrontPresentation` registration surface.
- Added `StorefrontPresentationFoundationBoundaryTests` for solution inclusion, folder skeleton, dependency direction, and Components/WASM boundary.
- Verification: `dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/BlazorShop.Storefront.Presentation.csproj --no-restore` passed with 0 warnings.
- Verification: `dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --filter "FullyQualifiedName~StorefrontPresentationFoundationBoundaryTests" --no-restore` passed 5/5 with existing MessagePack vulnerability and Browserslist warnings.

Exit criteria:

- [x] `dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/BlazorShop.Storefront.Presentation.csproj` pass.
- [x] Dependency guardrails pass.

## Phase SPF2 - Foundation View Adapter

Muc tieu: Presentation route pages co the render V2/Starter views ma khong reference V2/Starter.

- [ ] Tao `StorefrontFoundationViewSet` trong `Views/Foundation` voi typed slots toi thieu:
  - `ApplicationHead`
  - `ApplicationScripts`
  - `MainLayout`
  - `HomePage`
  - `CategoryPage`
  - `ProductPage`
  - `SearchPage`
  - `DealsPage`
  - `NewReleasesPage`
  - `ContentPage`
  - `CartPage`
  - `CheckoutPage`
  - `PaymentResultPage`
  - `AuthPage`
  - `AccountPage`
  - `MaintenanceState`
  - `NotFoundState`
  - `ServiceUnavailableState`
  - `ErrorState`
- [ ] Tao `StorefrontFoundationViewOptions`.
- [ ] Tao `AddStorefrontFoundationViews(...)` cho host dang ky view set.
- [ ] Tao validator fail-fast khi missing required views.
- [ ] Tao `StorefrontFoundationViewOutlet.razor` dung `DynamicComponent`.
- [ ] Context type validation:
  - view nhan dung parameter name chuan, vi du `Context`;
  - fail ro neu view type khong phu hop.
- [ ] Them V2 registration:

```text
BlazorShop.Storefront.V2/V2FoundationViewRegistration.cs
```

- [ ] Them Starter registration:

```text
BlazorShop.Storefront.Starter/StarterFoundationViewRegistration.cs
```

- [ ] Chua doi route trong phase nay; chi prove host co the dang ky view set.

Exit criteria:

- [ ] V2 va Starter build pass khi dang ky empty/minimal view set.
- [ ] Test fail neu view set thieu slot bat buoc.
- [ ] Presentation khong reference V2/Starter.

## Phase SPF3 - Application Root and Routing Foundation

Muc tieu: Presentation so huu root app lifecycle va router; host so huu assets/visual shell.

- [ ] Tao trong Presentation:

```text
App/StorefrontApp.razor
App/StorefrontRoutes.razor
App/_Imports.razor
Routing/StorefrontRoutePatterns.cs
Routing/StorefrontRouteNames.cs
Routing/StorefrontNavigationPolicy.cs
```

- [ ] `StorefrontApp.razor` so huu:
  - DOCTYPE
  - `html/head/body`
  - `base href`
  - `HeadOutlet`
  - antiforgery head hook
  - asset/head outlet hook
  - script outlet hook
  - Blazor bootstrap position
- [ ] Khong hardcode V2-only assets trong Presentation:
  - no `css/storefront.css`
  - no `js/storefrontCommerce.js`
  - no V2 favicon/icon assumptions ngoai generic slot
- [ ] `StorefrontRoutes.razor` so huu:
  - route discovery tren Presentation assembly;
  - default layout type tu view set;
  - focus-on-navigation;
  - not-found composition qua state/view outlet.
- [ ] V2 `Program.cs` doi root mapping sang:

```csharp
app.MapRazorComponents<StorefrontApp>()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(BlazorShop.Storefront.V2.WASM.Components.Account.StorefrontAccountApp).Assembly);
```

- [ ] Starter `Program.cs` doi root mapping sang `StorefrontApp`.
- [ ] Sau khi V2/Starter dung Presentation root, remove hoac retire:
  - `Storefront.V2/App.razor`
  - `Storefront.V2/Routes.razor`
  - `Storefront.Starter/Components/App.razor`
  - `Storefront.Starter/Components/Routes.razor`
- [ ] Them guardrail:
  - V2/Starter khong co `App.razor`;
  - V2/Starter khong co `Routes.razor`;
  - Presentation root does not hardcode V2 CSS/script paths.

Exit criteria:

- [ ] V2 va Starter dung chung Presentation App/Routes.
- [ ] Header/footer/layout visual van do V2/Starter view set cung cap.
- [ ] Root CSS/script cua V2/Starter van render dung qua host-provided view slots.

## Phase SPF4 - Page State and HTTP Status Foundation

Muc tieu: route pages khong con tu nho SEO/status/error rendering.

- [ ] Tao page-state primitives:

```text
PagePatterns/StorefrontPageState.cs
PagePatterns/StorefrontPageKind.cs
PagePatterns/StorefrontPageStatus.cs
PagePatterns/StorefrontPageProblem.cs
PagePatterns/StorefrontPageContext.cs
PagePatterns/StorefrontPageResultMapper.cs
PagePatterns/StorefrontHttpStatusPolicy.cs
PagePatterns/StorefrontPage.razor
```

- [ ] State toi thieu:
  - `Loading`
  - `Ready<TContext>`
  - `Empty`
  - `NotFound`
  - `ServiceUnavailable`
  - `Unauthorized`
  - `Maintenance`
  - `Error`
- [ ] `Ready<TContext>` bat buoc co:
  - `StorefrontPageKind`
  - `TContext`
  - SEO document
  - HTTP status intent
- [ ] `StorefrontPage<TContext>` so huu:
  - SEO head rendering;
  - robots meta;
  - HTTP status application;
  - not-found/service-unavailable/error visual outlet;
  - retryable flag;
  - trace/problem metadata.
- [ ] Move/generalize tu V2:
  - `StorefrontResponseHeaders`
  - noindex/private-page header policy
  - status precedence
- [ ] Them tests:
  - cannot create ready state without SEO;
  - not found applies 404;
  - service unavailable applies 503;
  - private pages apply noindex/no-store policy.

Exit criteria:

- [ ] Page-state foundation build pass.
- [ ] Mot test page proof render duoc Ready/NotFound/ServiceUnavailable qua outlet.

## Phase SPF5 - SEO, Discovery, and Route Policy Migration

Muc tieu: SEO/discovery la application concern dung chung V2 va Starter.

- [ ] Move/generalize tu V2 sang Presentation:
  - `StorefrontRoutes`
  - `StorefrontSeoComposer`
  - `StorefrontSeoSettingsProvider`
  - `StorefrontStructuredDataComposer`
  - `StorefrontStructuredDataDocument`
  - `StorefrontIndexingPolicy`
  - `StorefrontRobotsService`
  - `StorefrontSitemapService`
  - `SeoRuntimeLogger`
  - public URL resolver neu khong host-specific
- [ ] Giu V2-only head visuals o V2:
  - brand head visual
  - theme color
  - icons
  - font preload
  - theme CSS/script references
- [ ] Tao Presentation endpoint mapping:

```csharp
app.MapStorefrontPresentationSeoEndpoints();
```

- [ ] Replace V2:
  - `app.MapStorefrontSeoEndpoints()` -> `app.MapStorefrontPresentationSeoEndpoints()` hoac aggregated `MapStorefrontPresentation()`.
- [ ] Replace Starter:
  - remove `StarterSeoEndpoints`;
  - use Presentation robots/sitemap behavior.
- [ ] Tests:
  - robots content type and sitemap link;
  - sitemap XML content type;
  - service unavailable returns 503 when SEO document cannot be generated;
  - product/category/content canonical;
  - search/cart/checkout/account noindex.

Exit criteria:

- [ ] SEO/discovery implementation khong con duplicated giua V2 va Starter.
- [ ] V2 visual page views khong tu render `SeoHead`; Presentation page state lam viec do.

## Phase SPF6 - Product Page Vertical Slice

Muc tieu: tach page phuc tap nhat dau tien de prove architecture.

- [ ] Tao Presentation route page:

```text
Pages/Hybrid/Catalog/ProductRoutePage.razor
```

- [ ] Tao service/context:

```text
Services/Product/StorefrontProductPageService.cs
Services/Product/StorefrontProductPageContext.cs
Services/Product/StorefrontProductPageMapper.cs
```

- [ ] Move vao Presentation:
  - route `/product/{Slug}`;
  - slug validation;
  - catalog facade/client call;
  - display context;
  - service unavailable mapping;
  - product 404 mapping;
  - response status intent;
  - SEO;
  - structured data;
  - canonical;
  - breadcrumbs data;
  - gallery item mapping;
  - purchase panel model/context mapping;
  - related products loading;
  - purchase/sellability reason codes.
- [ ] Giu trong V2 view:
  - product page markup;
  - gallery markup;
  - purchase panel markup;
  - badges;
  - CSS classes;
  - typography;
  - final copy such as out-of-stock text;
  - section ordering.
- [ ] Tao V2 view:

```text
Storefront.V2/Theme/Pages/Product/V2ProductPageView.razor
```

- [ ] Product route page render V2 view qua `StorefrontFoundationViewOutlet`.
- [ ] Remove `@page` tu V2 `ProductPage.razor` hoac replace bang V2 view without route directive.
- [ ] Tests:
  - `/product/{slug}` still renders same major DOM markers;
  - missing product returns HTTP 404;
  - Commerce unavailable returns HTTP 503;
  - product structured data present;
  - gallery 1x1 visual remains V2-owned;
  - add-to-cart action descriptors unchanged.

Exit criteria:

- [ ] Product application logic nam trong Presentation.
- [ ] V2 product view khong inject catalog client, SEO composer, status header service.
- [ ] Product route URL va behavior parity pass.

## Phase SPF7 - Catalog Listing Pages

Muc tieu: move application orchestration cho public catalog pages sau khi product slice pass.

- [ ] Move route pages vao Presentation:
  - home `/`
  - category `/category/{Slug}`
  - search `/search`
  - today's deals `/todays-deals`
  - new releases `/new-releases`
- [ ] Tao services/contexts:
  - `StorefrontHomePageService`
  - `StorefrontCategoryPageService`
  - `StorefrontSearchPageService`
  - `StorefrontDealsPageService`
  - `StorefrontNewReleasesPageService`
- [ ] Presentation owns:
  - query normalization;
  - paging;
  - sorting/filter parameter interpretation;
  - catalog calls;
  - empty/not-found/unavailable state;
  - SEO/canonical;
  - search noindex;
  - product summary context mapping.
- [ ] V2 owns:
  - filter panel markup;
  - product grid;
  - product card;
  - empty-state visual;
  - responsive layout;
  - final text.
- [ ] Starter owns neutral views cho same contexts.
- [ ] Remove `@page` tu V2/Starter catalog visual files after cutover.
- [ ] Tests:
  - home/category/search/deals/new releases route parity;
  - search noindex;
  - category not found;
  - product summary links/currency still correct.

Exit criteria:

- [ ] Public catalog route files nam trong Presentation.
- [ ] V2/Starter catalog files are views, not route pages.

## Phase SPF8 - Content, System, and Auth SSR Pages

- [ ] Move content route logic:
  - `/pages/{Slug}` from V2;
  - Starter content route mapping if still needed.
- [ ] Move system route logic:
  - maintenance;
  - not found/catch-all;
  - service unavailable state.
- [ ] Move auth SSR route logic:
  - sign in;
  - register;
  - forgot password;
  - reset password;
  - logout route/form handling.
- [ ] Move auth form endpoint mapping into Presentation:

```csharp
app.MapStorefrontPresentationAuthEndpoints();
```

- [ ] Presentation owns:
  - return URL validation;
  - register disabled policy handling;
  - form endpoint security;
  - auth cookies/session orchestration;
  - redirect result mapping;
  - noindex metadata.
- [ ] V2/Starter owns:
  - auth form visual;
  - field classes;
  - copy;
  - layout.
- [ ] Tests:
  - sign in/register/recovery routes;
  - register disabled cannot submit;
  - unsafe return URL rejected/normalized;
  - noindex on auth pages;
  - logout remains side-effecting POST.

Exit criteria:

- [ ] Auth SSR routes/endpoints no longer live in V2/Starter.
- [ ] Visual auth templates remain host-owned.

## Phase SPF9 - Cart Route and BFF Migration

- [ ] Move cart route `/my-cart` into Presentation.
- [ ] Decide route compatibility for Starter current `/cart`:
  - preferred: Presentation supports V2 canonical `/my-cart` and optional host route alias;
  - if Starter keeps `/cart`, define alias explicitly in host route options.
- [ ] Move cart application services:
  - cart token resolution;
  - display context;
  - initial snapshot mapping;
  - warning mapping;
  - product URL mapping;
  - price formatting orchestration;
  - cart page context.
- [ ] Move cart BFF endpoint mapping:
  - `GET /api/cart`
  - `POST /api/cart/lines`
  - update quantity
  - remove line
  - clear
  - recalculate
  - `POST /api/product-selection-preview`
- [ ] Presentation owns:
  - antiforgery validation;
  - rate-limit policy names;
  - local BFF envelope;
  - cart/customer cookie policy;
  - same-origin boundary.
- [ ] WASM/V2 owns:
  - cart interactive component;
  - line markup;
  - quantity controls;
  - loading/toast/empty visual.
- [ ] Remove Starter cart BFF duplicate after Presentation endpoint pass.
- [ ] Tests:
  - add item;
  - update quantity;
  - remove;
  - clear;
  - recalculate;
  - product selection preview;
  - antiforgery required for mutations;
  - browser does not call Commerce Node directly.

Exit criteria:

- [ ] V2 and Starter use same cart route/BFF application logic.
- [ ] WASM still only calls same-origin `/api/cart/*`.

## Phase SPF10 - Checkout and Payment Result Migration

- [ ] Move checkout route `/checkout` into Presentation.
- [ ] Move payment result routes:
  - `/payment-success`
  - `/payment-cancel`
  - Starter payment result route compatibility if needed.
- [ ] Move checkout initial context:
  - cart resolution;
  - session/customer resolution;
  - address config;
  - shipping/payment method loading;
  - cart version;
  - idempotency key;
  - checkout initial state.
- [ ] Move checkout BFF endpoint mapping:
  - `GET /api/checkout`
  - addresses
  - shipping method
  - payment method
  - review
  - place order
- [ ] Presentation owns:
  - antiforgery;
  - cart version guard;
  - checkout command validation mapping;
  - order placement orchestration through Commerce Node Storefront API;
  - payment redirect/client action result mapping;
  - private/noindex headers;
  - cart cookie clear after place-order when current behavior does so.
- [ ] V2 owns:
  - checkout form markup;
  - order summary visual;
  - payment card visual;
  - button/copy/classes.
- [ ] WASM owns:
  - browser-side step state;
  - interactive validation feedback;
  - same-origin BFF calls.
- [ ] Tests:
  - empty cart state;
  - guest checkout if enabled;
  - saved address path if fixture exists;
  - COD payment method;
  - place real test order;
  - duplicate submit/idempotency guard unchanged;
  - checkout noindex.

Exit criteria:

- [ ] Checkout application orchestration nam trong Presentation.
- [ ] V2/Starter checkout files are views, not route/BFF owners.

## Phase SPF11 - Account WasmHost and Account BFF Migration

- [ ] Move account host routes:
  - `/account`
  - `/account/{*Path}`
- [ ] Move account host application logic:
  - session authorization;
  - sign-in redirect;
  - return URL;
  - antiforgery token bootstrap;
  - noindex/private metadata;
  - account route context.
- [ ] Move account BFF endpoints:
  - profile get/update;
  - addresses get/create/update/delete/default;
  - orders list;
  - order detail;
  - receipt;
  - change password.
- [ ] Presentation owns:
  - access-token/session resolution;
  - local browser-safe response mapping;
  - antiforgery validation;
  - authorization failure mapping.
- [ ] `Storefront.V2.WASM` owns:
  - `StorefrontAccountApp`;
  - navigation;
  - profile editor;
  - address book;
  - order list/detail;
  - receipt view;
  - change password form;
  - classes/copy.
- [ ] V2 host registers account view type from `Storefront.V2.WASM`.
- [ ] Starter either:
  - provides neutral account visual;
  - or explicitly marks account visual unavailable while still sharing auth/account route policies.
- [ ] Tests:
  - direct `/account/orders` unauthenticated redirects to sign-in with safe `returnUrl`;
  - authenticated account route hydrates;
  - antiforgery token available before WASM call;
  - account BFF denies unauthenticated;
  - profile/address/order/change-password browser flows pass.

Exit criteria:

- [ ] Account route/security/bootstrap belongs to Presentation.
- [ ] WASM still does not reference Presentation/Runtime/Client.

## Phase SPF12 - Media, Consent, and Host Pipeline Aggregation

- [ ] Move/shared endpoint mappings:
  - consent current/save/revoke;
  - media proxy endpoints if behavior is storefront-application-owned;
  - favicon remains host-owned unless standardized.
- [ ] Create aggregation:

```csharp
app.UseStorefrontPresentation();
app.MapStorefrontPresentation();
```

- [ ] Presentation owns:
  - current-store guard integration points;
  - public redirect policy if it is route/application concern;
  - private page headers;
  - local BFF error envelope;
  - trace/correlation propagation into local endpoint responses.
- [ ] Host still owns:
  - `builder.AddServiceDefaults()`;
  - environment logging;
  - HSTS/HTTPS;
  - static file hosting;
  - deployment config;
  - `MapDefaultEndpoints()`;
  - `MapStaticAssets()`.
- [ ] V2 `Program.cs` target shape:

```csharp
builder.AddServiceDefaults();

builder.Services
    .AddStorefrontRuntime(...)
    .AddStorefrontPlatformRuntime(...)
    .AddStorefrontPresentation(...)
    .AddV2FoundationViews();

var app = builder.Build();

app.UseStorefrontV2HostPipeline(...);
app.UseStorefrontPresentation();
app.MapStaticAssets();
app.MapDefaultEndpoints();
app.MapStorefrontPresentation();
app.MapRazorComponents<StorefrontApp>()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(StorefrontAccountApp).Assembly);

app.Run();
```

- [ ] Starter target shape:

```csharp
builder.Services
    .AddStorefrontRuntime(...)
    .AddStorefrontPlatformRuntime()
    .AddStorefrontPresentation(...)
    .AddStarterFoundationViews();

app.UseStorefrontPresentation();
app.MapStorefrontPresentation();
app.MapRazorComponents<StorefrontApp>();
```

Exit criteria:

- [ ] V2 `Program.cs` no longer maps individual Storefront BFF/SEO endpoint groups.
- [ ] Starter no longer owns duplicate BFF/SEO endpoint groups.

## Phase SPF13 - Starter Consumer Migration

Muc tieu: prove Presentation has at least two real consumers.

- [ ] Starter references Presentation as package/project, not V2.
- [ ] Starter removes route pages after equivalent Presentation route is active.
- [ ] Starter keeps only:
  - neutral visual views;
  - neutral layout;
  - starter CSS/assets;
  - feature manifest;
  - copy;
  - optional unavailable placeholders.
- [ ] Starter views accept Presentation contexts.
- [ ] Starter does not copy V2 visual components.
- [ ] Starter does not copy BFF/SEO/page-service logic.
- [ ] Update Starter generation contract:
  - generated storefronts consume Presentation;
  - generated storefronts provide views/assets/copy;
  - generated storefronts do not generate route/BFF/SEO logic from scratch.
- [ ] Tests:
  - Starter build;
  - Starter no `@page`;
  - Starter no BFF/SEO endpoint duplicate;
  - Starter no V2 reference;
  - generated proof isolation still passes.

Exit criteria:

- [ ] V2 and Starter both use same Presentation App/Routes/page services/BFF/SEO.
- [ ] A fix in Presentation route logic benefits both.

## Phase SPF14 - Documentation and Architecture Update

- [ ] Update `AGENTS.md` active project shape to include `BlazorShop.Storefront.Presentation`.
- [ ] Update `docs/architecture/03-runtime-boundaries.md`:
  - Storefront Presentation application engine;
  - V2 host/visual boundary;
  - Starter/generated host/visual boundary.
- [ ] Update `docs/architecture/05-project-and-folder-guide.md`.
- [ ] Update `docs/architecture/10-v2-contract-ownership.md`.
- [ ] Update `docs/architecture/11-storefront-builder.md`.
- [ ] Update `docs/agents/storefront-builder.md`.
- [ ] Update `docs/visual-reverse-engineering-skill/README.md` and reference docs.
- [ ] Update QA checklist:
  - `QA-StorefrontV2.todo.md`
  - StorefrontBuilder isolation/release checklist if affected.
- [ ] Add ADR:

```text
docs/architecture/adr/YYYY-MM-DD-storefront-presentation-foundation.md
```

Exit criteria:

- [ ] Current docs no longer say V2 owns route composition/BFF/SEO after migration.
- [ ] Generated storefront docs no longer imply AI generator must recreate application logic.

## Phase SPF15 - Build, Test, Package, and Browser QA

Focused builds:

- [ ] `dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/BlazorShop.Storefront.Presentation.csproj`
- [ ] `dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM/BlazorShop.Storefront.V2.WASM.csproj`
- [ ] `dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.V2/BlazorShop.Storefront.V2.csproj`
- [ ] `dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/BlazorShop.Storefront.Starter.csproj`

Focused tests:

- [ ] Architecture dependency tests.
- [ ] Storefront shared platform package tests.
- [ ] Storefront page composition guardrail tests.
- [ ] Storefront endpoint dependency tests.
- [ ] Storefront WASM runtime boundary tests.
- [ ] Storefront Starter foundation tests.
- [ ] StorefrontBuilder isolation tests.

Scripts:

- [ ] `./scripts/qa/run-storefront-builder-isolation-gate.ps1`
- [ ] `./scripts/qa/run-storefront-foundation-isolation-gate.ps1` if packaging changed.

Playwright browser QA:

- [ ] home renders.
- [ ] category renders.
- [ ] search renders and noindex.
- [ ] product renders gallery/purchase panel.
- [ ] product missing returns 404.
- [ ] service unavailable path returns 503 where fixture supports it.
- [ ] cart add/update/remove/clear.
- [ ] checkout COD real test order placement.
- [ ] payment success/cancel route.
- [ ] sign in/register/recovery.
- [ ] register disabled policy.
- [ ] account profile/address/orders/order detail/change password.
- [ ] robots.txt.
- [ ] sitemap.xml.
- [ ] maintenance page.
- [ ] no browser console errors.
- [ ] no browser direct call to Commerce Node.

Exit criteria:

- [ ] V2 passes production-facing browser QA.
- [ ] Starter builds and runs as second consumer.
- [ ] Generated storefront proof still respects package/isolation boundaries.

## Final Definition of Done

Ownership:

- [ ] Presentation owns `App`, `Routes`, route pages, page services, SEO, BFF endpoint mappings, route policy, page-state/status policy.
- [ ] V2 owns V2 visual views, V2 assets, V2 layout, V2 copy, V2 host deployment.
- [ ] Starter owns neutral visual views/assets/copy only.
- [ ] `Storefront.V2.WASM` owns interactive browser visual components only.

Dependency:

- [ ] Presentation does not reference V2, V2.WASM, Starter, ServiceDefaults, Application, Domain, Infrastructure, CommerceNode.API, ControlPlane.API.
- [ ] V2.WASM does not reference Presentation, Runtime, or Client.
- [ ] Components remains logic/browser-safe with no Razor visual wrappers.
- [ ] Starter does not reference V2.

Functional parity:

- [ ] Same public route URLs, or explicitly documented compatible aliases.
- [ ] Same cart/checkout/account BFF route behavior.
- [ ] Same auth redirect and return URL behavior.
- [ ] Same SEO/status behavior.
- [ ] Same V2 visual output within acceptable non-redesign diff.

Consumer proof:

- [ ] V2 and Starter both use Presentation.
- [ ] Fixing route/SEO/BFF logic in Presentation requires no duplicate change in V2/Starter.

## Risk Controls

- [ ] Move one vertical slice at a time; start with Product page.
- [ ] Keep old V2 route page until Presentation replacement has characterization tests.
- [ ] Do not remove Starter duplicate page until Presentation equivalent works.
- [ ] Do not move visual/copy into Presentation to make migration easier.
- [ ] Do not pull WASM into Presentation just to render account/cart/checkout.
- [ ] Do not collapse Runtime into Presentation; Runtime remains generated-client/server primitive package.
- [ ] Do not let generated storefronts reference V2 to satisfy view set quickly.

## Decision Audit Trail

| # | Phase | Decision | Classification | Principle | Rationale | Rejected |
|---|-------|----------|----------------|-----------|-----------|----------|
| 1 | Scope | Create `BlazorShop.Storefront.Presentation` as application engine | Auto-decided | Boundary clarity | V2 and Starter currently duplicate App/Routes/BFF/SEO/page routes; Presentation is the right shared owner for application orchestration. | Keep V2 as canonical and copy behavior into Starter/generated storefronts |
| 2 | Scope | Keep Phase 1 to fixed view adapter, not full theme engine | Auto-decided | Blast-radius control | The immediate problem is application ownership, not template override mechanics. A fixed view set solves dependency direction without introducing manifest/analyzer/template hierarchy. | Build complete theme platform in Phase 1 |
| 3 | Dependencies | Presentation references Runtime and Components only | Auto-decided | Layer isolation | Runtime already owns server generated-client primitives; Components owns browser-safe state/contracts. Presentation must not depend on V2/WASM/backend/core. | Let Presentation reference V2 views directly |
| 4 | Migration order | Product page first vertical slice | Auto-decided | Highest-signal proof | Product page currently mixes route, API, SEO/status, structured data, mapping, related products, and visual markup in 515 lines. It proves the architecture before moving every route. | Move all route pages in one pass |
| 5 | Starter | Starter must become second real consumer before closing Phase 1 | Auto-decided | Consumer proof | A shared Presentation package is only proven when both V2 and Starter consume the same App/Routes/BFF/SEO behavior. | Ship Presentation only for V2 |
| 6 | QA | Require Playwright real browser flows including COD order placement | Auto-decided | Production readiness | This migration touches cart/checkout/account/browser handoff; build-only or smoke tests would miss real regressions. | Only run build and route smoke tests |

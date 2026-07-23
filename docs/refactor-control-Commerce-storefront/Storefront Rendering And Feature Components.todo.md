# Storefront Rendering And Feature Components Todo

Status: Proposed
Source: autoplan review on 2026-07-23 after Storefront rendering/componentization discussion
Purpose: make Storefront V2 rendering ownership visible in code, reduce account route-shell duplication, and move reusable storefront capabilities into portable feature components without rewriting current V2 behavior.

## Current Verified Codebase Context

- [x] `BlazorShop.Storefront.V2/Pages` is currently grouped by business area: `Catalog`, `Content`, `Commerce`, `Auth`, `Account`, and `System`.
- [x] `BlazorShop.Storefront.V2/Program.cs` is already a composition file and maps endpoint groups through extension methods. This plan must not re-expand Program.cs.
- [x] `BlazorShop.Storefront.Components` currently contains `Account`, `Cart`, `Checkout`, and `Browser`. The old development WASM diagnostics probe has been retired, and it does not yet have a `Features/*` convention.
- [x] `BlazorShop.Storefront.Components` has no direct reference to `Application`, `Domain`, or `Infrastructure`.
- [x] `BlazorShop.Storefront.WASM` references `BlazorShop.Storefront.Components` and currently provides browser bootstrapping/local API services, not a full route app.
- [x] Account routes originally existed as several server Razor pages under `Pages/Account`, each rendering a WASM component with `InteractiveWebAssembly`.
- [x] Phase 2 consolidated repeated account route-shell responsibilities into `AccountHostPage` plus `Storefront.Components/Features/Account/AccountApp`.
- [x] Some WASM components receive server `Initial*` data and still refetch after first browser render. This creates duplicate fetch risk.
- [x] Catalog pages such as home, product, category, new releases, and today's deals are still SEO-sensitive SSR route pages.

## Goal

Make Storefront rendering easy to understand by looking at the folder structure and component boundaries:

- SSR pages are visibly separate from hybrid pages and WASM-hosted features.
- Page files become route composition only.
- Reusable capabilities live as feature components that can be placed on multiple pages.
- A feature such as deals can be rendered on the home page, a dedicated deals page, or the product detail footer without duplicating query/render logic.
- Account becomes one server-hosted WASM feature route instead of many nearly identical server page shells.
- The current ASP.NET Core Blazor Web App host remains the single Storefront host.
- Routes, Storefront API paths, SEO behavior, auth behavior, cart/checkout/order behavior, and payment behavior stay stable.

## Non-goals

- [ ] Do not move Storefront into a separate React app in this phase.
- [ ] Do not split Storefront into a separate repository in this phase.
- [ ] Do not remove the ASP.NET Core server host for account, cart, checkout, auth, SEO, sitemap, robots, or media proxy behavior.
- [ ] Do not convert product/category/home/search pages into DB content pages.
- [ ] Do not move checkout place-order/payment logic fully into the browser.
- [ ] Do not introduce a generic page builder.
- [ ] Do not create a second Storefront API route shape.
- [ ] Do not add runtime dependency from `Storefront.Components` or `Storefront.WASM` to `Application`, `Domain`, `Infrastructure`, Control Plane, EF, or node credentials.
- [ ] Do not mix broad behavior changes with mechanical file moves.

## Target Rendering Ownership Model

| Ownership | Meaning | Storefront examples | Rule |
| --- | --- | --- | --- |
| `Ssr` | Server renders the usable page. WASM is not required for primary function. | content pages, system pages, auth forms where applicable | Keep data load and crawler/status behavior server-owned. |
| `Hybrid` | Server renders SEO/snapshot/page composition; feature components own interaction. | home, category, product, search, new releases, today's deals, cart, checkout | Page loads or receives route context, then composes feature components. |
| `WasmHost` | Server owns route/security/bootstrap; the feature app owns sub-navigation and UI state. | account portal | Keep server host route for deep links, noindex, auth redirect, and antiforgery/bootstrap. |

## Target Page Folder Shape

This folder shape is intentionally about render ownership, not ecommerce domain.

```text
BlazorShop.Storefront.V2/
  Pages/
    Ssr/
      Auth/
      Content/
      System/
    Hybrid/
      Catalog/
      Commerce/
    WasmHost/
      Account/
        AccountHostPage.razor
```

Route URLs must remain unchanged by this move.

## Target Feature Component Shape

Feature components should be grouped by reusable capability, not by the first page that uses them.

```text
BlazorShop.Storefront.Components/
  Features/
    Account/
      AccountApp.razor
      AccountNavigation.razor
      AccountProfilePanel.razor
      AccountAddressBook.razor
      AccountOrderList.razor
      AccountOrderDetail.razor
      AccountChangePasswordPanel.razor
    Cart/
      CartView.razor
      CartSummary.razor
      CartLineList.razor
    Checkout/
      CheckoutShell.razor
      CheckoutStepNavigation.razor
      CheckoutReviewPanel.razor
    Deals/
      DealsBlock.razor
      DealsGrid.razor
      DealsCarousel.razor
    Product/
      ProductGallery.razor
      ProductPurchasePanel.razor
      ProductPricePanel.razor
      ProductAvailabilityPanel.razor
    Catalog/
      ProductSummaryGrid.razor
      ProductSummaryCard.razor
      CatalogPager.razor
    Navigation/
      MenuTree.razor
      BreadcrumbTrail.razor
  Browser/
```

Folder names may be adjusted during implementation if existing component names make a smaller mechanical move safer.

## Feature Component Contract Rules

- [ ] A `Pages/*` file may resolve route parameters, SEO metadata, HTTP status, auth redirect, noindex/noarchive metadata, and initial snapshot data.
- [ ] A `Pages/*` file must not contain reusable feature UI logic such as deals grid rendering, product summary cards, account sub-navigation state, cart line rendering, or checkout step UI.
- [ ] A `Features/*` component must accept explicit input parameters such as `StoreKey`, `ProductId`, `CategoryId`, `Placement`, `MaxItems`, `InitialData`, and `Mode`.
- [ ] A feature component may support both:
  - [ ] server-provided initial snapshot.
  - [ ] browser fetch through `StorefrontLocalApiClient` after hydration.
- [ ] A feature component must not refetch on first browser render when a complete initial snapshot is already supplied.
- [ ] A feature component must expose loading, empty, error, and ready states.
- [ ] A feature component must not depend on a specific page route.
- [ ] A feature component must keep browser-only behavior behind `Storefront.Components/Browser` abstractions.
- [ ] New Storefront business contracts must not be added to `Web.SharedV2/Models`.

## Phase Dependency Map

```text
Phase 0: Baseline inventory and guardrails
  -> Phase 1: Rendering ownership convention and mechanical folder plan
      -> Phase 2: AccountHostPage and AccountApp consolidation
          -> Phase 3: Hydration and duplicate-fetch policy
              -> Phase 4: Feature component foundation
                  -> Phase 5: Deals/New Releases portable component extraction
                      -> Phase 6: Catalog/Product portable component extraction
                          -> Phase 7: Contract boundary cleanup
                              -> Phase 8: Playwright and release QA
```

## Phase 0 - Baseline Inventory And Guardrails

Goal: freeze the current route list, render modes, and project boundaries before moving files or changing account composition.

### Tasks

- [x] Record current Storefront page inventory:
  - [x] `Pages/Hybrid/Catalog/Home.razor`
  - [x] `Pages/Hybrid/Catalog/CategoryPage.razor`
  - [x] `Pages/Hybrid/Catalog/ProductPage.razor`
  - [x] `Pages/Hybrid/Catalog/SearchPage.razor`
  - [x] `Pages/Hybrid/Catalog/NewReleases.razor`
  - [x] `Pages/Hybrid/Catalog/TodaysDeals.razor`
  - [x] `Pages/Ssr/Content/StorefrontPage.razor`
  - [x] `Pages/Hybrid/Commerce/CartPage.razor`
  - [x] `Pages/Hybrid/Commerce/CheckoutPage.razor`
  - [x] `Pages/Hybrid/Commerce/PaymentSuccessPage.razor`
  - [x] `Pages/Hybrid/Commerce/PaymentCancelPage.razor`
  - [x] `Pages/Ssr/Auth/SignInPage.razor`
  - [x] `Pages/Ssr/Auth/RegisterPage.razor`
  - [x] `Pages/Ssr/Auth/ForgotPasswordPage.razor`
  - [x] `Pages/Ssr/Auth/ResetPasswordPage.razor`
  - [x] current account pages.
  - [x] system pages.
- [x] Add or update static tests that verify route URLs before the folder move:
  - [x] `/`
  - [x] `/category/{Slug}`
  - [x] `/product/{Slug}`
  - [x] `/search`
  - [x] `/new-releases`
  - [x] `/todays-deals`
  - [x] `/pages/{Slug}`
  - [x] `/my-cart`
  - [x] `/checkout`
  - [x] `/account`
  - [x] `/account/profile`
  - [x] `/account/addresses`
  - [x] `/account/orders`
  - [x] `/account/orders/{OrderReference}`
  - [x] `/account/change-password`
- [x] Add project-boundary tests:
  - [x] `Storefront.Components` does not reference `Application`, `Domain`, `Infrastructure`, Control Plane, or Commerce Node API projects.
  - [x] `Storefront.WASM` does not reference `Application`, `Domain`, `Infrastructure`, Control Plane, or Commerce Node API projects.
  - [x] `Storefront.WASM` references `Storefront.Components`.
- [x] Add a render-ownership manifest test or snapshot that records each page route as `Ssr`, `Hybrid`, or `WasmHost`.
- [x] Update `docs/refactor-control-Commerce-storefront/QA-StorefrontV2.todo.md` with a new render-ownership section.

### Files likely touched

- `BlazorShop.Tests.V2/PresentationV2/Storefront/*`
- `BlazorShop.Tests.V2/Architecture/*`
- `docs/refactor-control-Commerce-storefront/QA-StorefrontV2.todo.md`

### Verification

```powershell
dotnet test BlazorShop.Tests.V2\BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~Storefront|FullyQualifiedName~Architecture"
```

### Done when

- [x] Current route behavior is documented by tests.
- [x] Current project dependencies are protected by tests.
- [x] No runtime behavior has changed.

## Phase 1 - Rendering Ownership Convention And Mechanical Folder Move

Goal: make SSR, Hybrid, and WasmHost visible in the Storefront V2 source tree.

### Tasks

- [x] Create target folders:
  - [x] `Pages/Ssr/Auth`
  - [x] `Pages/Ssr/Content`
  - [x] `Pages/Ssr/System`
  - [x] `Pages/Hybrid/Catalog`
  - [x] `Pages/Hybrid/Commerce`
  - [x] `Pages/WasmHost/Account`
- [x] Move files mechanically without changing `@page` route declarations.
- [x] Keep existing route names and URLs unchanged.
- [x] Update namespaces/usings generated by the move only where required.
- [x] Update route inventory tests to look at new paths.
- [x] Update `docs/architecture/05-project-and-folder-guide.md` to describe the new render-ownership folders.
- [x] Add a short `README.md` or comment-free documentation section explaining:
  - [x] `Ssr` pages.
  - [x] `Hybrid` pages.
  - [x] `WasmHost` pages.
  - [x] when a new page belongs in each folder.
- [x] Do not refactor page internals in this phase.

### Suggested mapping

| Current | Target |
| --- | --- |
| `Pages/Content/StorefrontPage.razor` | `Pages/Ssr/Content/StorefrontPage.razor` |
| `Pages/System/*` | `Pages/Ssr/System/*` |
| `Pages/Auth/*` | `Pages/Ssr/Auth/*` |
| `Pages/Catalog/*` | `Pages/Hybrid/Catalog/*` |
| `Pages/Commerce/*` | `Pages/Hybrid/Commerce/*` |
| `Pages/Account/*` | temporarily `Pages/WasmHost/Account/*`, then consolidated in Phase 2 |

### Verification

```powershell
dotnet build BlazorShop.PresentationV2\BlazorShop.Storefront.V2\BlazorShop.Storefront.V2.csproj --no-restore
dotnet test BlazorShop.Tests.V2\BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~Storefront"
```

### Done when

- [x] A developer can identify render ownership from the folder path.
- [x] All public/private route URLs still work.
- [x] The change is mechanical and separately reviewable.

## Phase 2 - AccountHostPage And AccountApp Consolidation

Goal: replace many duplicated account route pages with one server-hosted WASM account boundary.

### Design decision

Account should be a WASM-owned feature, but not a pure WASM route. The server must still own the route boundary for direct refresh, noindex, auth redirect, antiforgery/bootstrap, and safe return URL behavior.

### Target shape

```text
BlazorShop.Storefront.V2/
  Pages/WasmHost/Account/
    AccountHostPage.razor

BlazorShop.Storefront.Components/
  Features/Account/
    AccountApp.razor
    AccountNavigation.razor
    AccountProfilePanel.razor
    AccountAddressBook.razor
    AccountOrderList.razor
    AccountOrderDetail.razor
    AccountChangePasswordPanel.razor
```

### AccountHostPage responsibilities

- [x] Declare account routes:
  - [x] `@page "/account"`
  - [x] `@page "/account/{*Path}"`
- [x] Emit `noindex,nofollow`.
- [x] Resolve current Storefront session.
- [x] Redirect unauthenticated users to sign-in with a safe return URL.
- [x] Create/provide antiforgery token bootstrap data.
- [x] Provide current account path to the component.
- [x] Render `AccountApp` with `InteractiveWebAssembly`.
- [x] Avoid loading profile/address/order detail data server-side unless needed for a transition compatibility window.

### AccountApp responsibilities

- [x] Interpret account sub-paths:
  - [x] empty path or `profile`.
  - [x] `addresses`.
  - [x] `orders`.
  - [x] `orders/{orderReference}`.
  - [x] `change-password`.
- [x] Own account sub-navigation and active item state.
- [x] Fetch account data through current local account endpoints.
- [x] Show loading, empty, error, and ready states.
- [x] Redirect or surface sign-in-required state on 401 responses.
- [x] Preserve browser back/forward behavior for account sub-routes.

### Migration tasks

- [x] Create `AccountHostPage.razor` while keeping old account pages in place behind tests.
- [x] Add route resolution tests proving all old account URLs are covered by the catch-all route.
- [x] Move account shared layout/navigation from `AccountPageShell` into `Features/Account/AccountApp` or `AccountNavigation`.
- [x] Move current account components into `Features/Account` with compatibility names or small wrapper components.
- [x] Switch account navigation links to route through the host path.
- [x] Delete old account route pages only after:
  - [x] direct deep link tests pass.
  - [x] unauthenticated redirect tests pass.
  - [x] noindex tests pass.
  - [x] account WASM hydration tests pass.
- [x] Remove obsolete account page tests and replace them with host/app tests.

### Verification

```powershell
dotnet test BlazorShop.Tests.V2\BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~Account"
```

### Done when

- [x] Storefront has one account server host page instead of many duplicate page shells.
- [x] Account feature UI lives in reusable components.
- [x] All old account URLs continue to work.
- [x] No account route is indexable.

## Phase 3 - Hydration And Duplicate-fetch Policy

Goal: eliminate accidental double fetch while keeping components reusable across SSR and browser-only usage.

### Design decision

Every interactive feature component must use one explicit data mode:

| Mode | Meaning |
| --- | --- |
| `InitialSnapshot` | Server/page already provided complete data. Component must not refetch on first browser render. |
| `BrowserFetch` | Component intentionally fetches after WASM starts. It must render skeleton/empty/error state first. |
| `RefreshAfterHydration` | Component renders server snapshot first, then explicitly refreshes because freshness matters. This must be opt-in and tested. |

### Tasks

- [x] Add a small shared enum/model in `Storefront.Components` for feature data mode if useful.
- [x] Audit current interactive components:
  - [x] `AccountProfileEditor`
  - [x] `AccountAddressBook`
  - [x] `AccountOrderList`
  - [x] `AccountOrderDetail`
  - [x] `AccountChangePasswordForm`
  - [x] `CartView`
  - [x] `CheckoutShell`
- [x] Change components that receive complete `Initial*` data to skip first-render refetch.
- [x] Keep manual refresh/retry actions where useful.
- [x] For account after Phase 2, prefer `BrowserFetch` because server host should only bootstrap the account app, not load each account screen.
- [x] For cart, keep `InitialSnapshot` when SSR page already has cart state.
- [x] For checkout, decide per screen:
  - [x] use `InitialSnapshot` for initial review state.
  - [x] use explicit refresh only after cart/address/shipping/payment changes.
- [x] Add tests that fail if a component performs a browser GET despite a complete initial snapshot.
- [ ] Add Playwright network assertions for at least account profile, cart, and checkout.

### Verification

```powershell
dotnet test BlazorShop.Tests.V2\BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~Hydration|FullyQualifiedName~Account|FullyQualifiedName~Cart|FullyQualifiedName~Checkout"
```

### Done when

- [x] Hydration behavior is explicit.
- [x] Duplicate first-render fetches are removed or intentionally documented.
- [x] Loading/empty/error/ready states are visible in tests.

## Phase 4 - Feature Component Foundation

Goal: introduce a reusable `Features/*` convention before extracting larger catalog blocks.

### Tasks

- [x] Create `Storefront.Components/Features`.
- [x] Move existing component folders into the new convention with minimal behavior change:
  - [x] `Account/*` -> `Features/Account/*` during or after Phase 2.
  - [x] `Cart/CartView.razor` -> `Features/Cart/CartView.razor` with compatibility wrapper if needed.
  - [x] `Checkout/CheckoutShell.razor` -> `Features/Checkout/CheckoutShell.razor` with compatibility wrapper if needed.
- [x] Add `Features/README.md` or architecture doc guidance:
  - [x] feature components are reusable storefront capability blocks.
  - [x] page files compose features.
  - [x] no EF/Application/Domain/Control Plane dependencies.
  - [x] no hidden route assumptions.
- [x] Add or update `_Imports.razor` for clean namespaces.
- [x] Keep old component names temporarily through thin wrappers if route pages/tests depend on them.
- [x] Remove wrappers only after all call sites are migrated.
- [x] Add architecture tests to block new root-level business component folders outside `Features` unless explicitly approved.

### Verification

```powershell
dotnet build BlazorShop.PresentationV2\BlazorShop.Storefront.Components\BlazorShop.Storefront.Components.csproj --no-restore
dotnet build BlazorShop.PresentationV2\BlazorShop.Storefront.WASM\BlazorShop.Storefront.WASM.csproj --no-restore
dotnet test BlazorShop.Tests.V2\BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~Storefront"
```

### Done when

- [x] The component tree communicates feature ownership.
- [x] Existing pages still compile and render.
- [x] New reusable features have a clear home.

## Phase 5 - Deals And New Releases Portable Feature Extraction

Goal: prove the component model with a useful, reusable business feature.

### Target reusable features

```text
Features/Deals/
  DealsBlock.razor
  DealsGrid.razor
  DealsCarousel.razor
  DealsContracts.cs

Features/Catalog/
  ProductSummaryGrid.razor
  ProductSummaryCard.razor
```

### Placement examples

```razor
<DealsBlock Placement="DealsPlacement.Home" MaxItems="8" />
<DealsBlock Placement="DealsPlacement.ProductDetailFooter" ProductId="@productId" MaxItems="4" />
<DealsBlock Placement="DealsPlacement.DedicatedPage" PageSize="24" />
```

### Tasks

- [ ] Extract product summary card/list markup from catalog pages into `Features/Catalog`.
- [ ] Extract today's deals rendering into `Features/Deals`.
- [ ] Keep `TodaysDeals.razor` as a route page under `Pages/Hybrid/Catalog`; it should compose `DealsBlock` instead of duplicating grid logic.
- [ ] Add `DealsPlacement` enum or equivalent model:
  - [ ] `Home`
  - [ ] `DedicatedPage`
  - [ ] `ProductDetailFooter`
  - [ ] `CategorySidebar` only if already needed.
- [ ] Support `InitialData` for SSR route pages.
- [ ] Support browser fetch only when the component is placed without initial data.
- [ ] Do not add a new Commerce Node API unless existing storefront catalog endpoints cannot support the needed data.
- [ ] If a new endpoint is required, make it store-scoped under `api/storefront/stores/{storeKey}/*` and comply with V2 API contract standards.
- [ ] Add tests proving:
  - [ ] home can render a deals block.
  - [ ] product page can render a deals block.
  - [ ] dedicated deals page still renders products and SEO metadata.
  - [ ] component does not require a specific route.

### Verification

```powershell
dotnet test BlazorShop.Tests.V2\BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~Deals|FullyQualifiedName~Product|FullyQualifiedName~Home"
```

### Done when

- [ ] The same deals component can be used by at least two page contexts.
- [ ] The dedicated deals route is only composition.
- [ ] Product summary UI is not duplicated between deals/new releases/catalog contexts.

## Phase 6 - Catalog And Product Portable Component Extraction

Goal: continue reducing page-level UI logic without weakening SSR SEO behavior.

### Candidate components

- [ ] `Features/Product/ProductGallery`
- [ ] `Features/Product/ProductPurchasePanel`
- [ ] `Features/Product/ProductPricePanel`
- [ ] `Features/Product/ProductAvailabilityPanel`
- [ ] `Features/Catalog/ProductSummaryGrid`
- [ ] `Features/Catalog/CatalogPager`
- [ ] `Features/Catalog/CatalogSortSelector`
- [ ] `Features/Navigation/BreadcrumbTrail`

### Tasks

- [ ] Keep product/category/search/home route pages responsible for route parameters, SEO, structured data, status handling, and initial query.
- [ ] Move reusable markup/state into feature components one group at a time.
- [ ] Product gallery:
  - [ ] keep 1x1 image ratio requirement.
  - [ ] support product detail main image + thumbnail list.
  - [ ] avoid product-page-only assumptions.
- [ ] Product purchase panel:
  - [ ] receive product/variant/sellability snapshot.
  - [ ] support WASM interaction for variant selection and add-to-cart when available.
  - [ ] keep server validation as source of truth.
- [ ] Catalog grid:
  - [ ] support category, search, new releases, and deals product summaries.
  - [ ] support empty state.
  - [ ] support paging/sorting inputs without owning route query parsing.
- [ ] Breadcrumb component:
  - [ ] accept resolved breadcrumb items.
  - [ ] do not call backend by itself unless explicitly placed in browser-fetch mode.
- [ ] Add tests for each extracted component before deleting duplicated page markup.

### Verification

```powershell
dotnet test BlazorShop.Tests.V2\BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~Catalog|FullyQualifiedName~Product|FullyQualifiedName~Storefront"
```

### Done when

- [ ] Catalog/product pages are easier to scan.
- [ ] Reusable components can be placed in multiple page contexts.
- [ ] SEO-critical SSR output remains intact.

## Phase 7 - Contract Boundary Cleanup For Portable Components

Goal: make feature components portable without forcing a big-bang OpenAPI migration.

### Tasks

- [ ] Inventory models used by Storefront pages/components:
  - [ ] account.
  - [ ] cart.
  - [ ] checkout.
  - [ ] catalog product summary.
  - [ ] deals/new releases.
  - [ ] product detail/gallery.
  - [ ] SEO/navigation.
- [ ] Keep Storefront browser/local endpoint contracts under `BlazorShop.Storefront.V2/Services/Contracts`.
- [ ] Keep component-facing browser models under `Storefront.Components/Browser` or a new `Storefront.Components/Features/*/*Models.cs` where they are presentation-only.
- [ ] Do not add new business DTO folders to `Web.SharedV2/Models`.
- [ ] Where server page and WASM component need the same model, choose one of:
  - [ ] move a presentation-only model into `Storefront.Components`.
  - [ ] add a narrow Storefront-local contract in `Storefront.V2/Services/Contracts`.
  - [ ] defer to generated Storefront OpenAPI client when the API boundary is ready.
- [ ] Avoid leaking admin-only fields to public/component contracts.
- [ ] Add tests covering contract boundaries:
  - [ ] public schemas do not contain admin fields.
  - [ ] components project dependency stays clean.
  - [ ] Web.SharedV2 business model freeze stays intact.

### Verification

```powershell
dotnet test BlazorShop.Tests.V2\BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~Contract|FullyQualifiedName~Boundary|FullyQualifiedName~OpenApi"
```

### Done when

- [ ] Component portability does not depend on backend/domain DTOs.
- [ ] Storefront contract ownership remains aligned with architecture docs.
- [ ] OpenAPI migration remains possible without blocking this phase.

## Phase 8 - Playwright And Release QA

Goal: prove the refactor works in a real browser, not just static/unit tests.

### Browser QA cases

- [ ] Home page:
  - [ ] renders SSR content before WASM.
  - [ ] includes reusable deals block if enabled.
  - [ ] no layout overlap desktop/mobile.
- [ ] Product detail:
  - [ ] renders SSR product title/price/gallery/SEO.
  - [ ] reusable deals block can appear at footer without duplicate page logic.
  - [ ] add-to-cart still works.
- [ ] Today's deals:
  - [ ] route still works.
  - [ ] uses same feature block/grid contract as home/product placement.
- [ ] Category/search:
  - [ ] product grid renders.
  - [ ] paging/sorting still work.
  - [ ] search remains noindex.
- [ ] Cart:
  - [ ] cart page loads.
  - [ ] quantity update/remove/recalculate still work.
  - [ ] no duplicate first-load cart fetch when initial snapshot is used.
- [ ] Checkout:
  - [ ] start/review/place order still works with COD in test store.
  - [ ] payment result routes still work.
  - [ ] no duplicate refresh unless an upstream checkout state changes.
- [ ] Account:
  - [ ] `/account`, `/account/profile`, `/account/addresses`, `/account/orders`, `/account/orders/{reference}`, `/account/change-password` deep links work.
  - [ ] unauthenticated account URL redirects to sign-in safely.
  - [ ] account routes are noindex.
  - [ ] browser back/forward inside account app works.
- [ ] Content/system:
  - [ ] `/pages/{slug}` still renders without WASM.
  - [ ] maintenance and not-found behavior remains correct.

### Suggested commands

```powershell
.\scripts\run-v2-local.ps1 -StopExisting
dotnet test BlazorShop.Tests.V2\BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~Storefront"
npx playwright test --config .\playwright.config.ts
```

Use the repository's current Playwright command/config if it differs.

### Done when

- [ ] Focused Storefront tests pass.
- [ ] Playwright browser tests pass on desktop and mobile viewports.
- [ ] Screenshots/network evidence are saved under the existing output convention.
- [ ] `QA-StorefrontV2.todo.md` includes release evidence.

## Implementation Order And Commit Plan

- [x] Commit 1: baseline inventory, route/render ownership guardrails, QA checklist updates.
- [x] Commit 2: mechanical folder move to `Ssr`, `Hybrid`, and `WasmHost`.
- [x] Commit 3: account host route and account app consolidation.
- [x] Commit 4: hydration mode and duplicate-fetch cleanup.
- [x] Commit 5: `Features/*` component convention and move existing components.
- [ ] Commit 6: deals/new releases portable component extraction.
- [ ] Commit 7: catalog/product portable component extraction.
- [ ] Commit 8: contract boundary cleanup and architecture docs.
- [ ] Commit 9: Playwright QA evidence and release checklist updates.

Each commit should build independently. Mechanical moves should not be mixed with behavior changes.

## Risk Controls

- [ ] Keep route URLs stable.
- [ ] Keep account server host route for deep links, auth redirect, noindex, and antiforgery/bootstrap.
- [ ] Keep checkout server-side command validation and payment redirect behavior.
- [ ] Keep product/category/home SSR output for SEO.
- [ ] Do not rely on WASM for content pages or crawler-facing SEO pages.
- [ ] Do not add component dependencies on backend/domain/infrastructure projects.
- [ ] Do not introduce duplicate browser fetch after complete SSR snapshot.
- [ ] Do not expose provider secrets, admin fields, internal IDs, or private customer/order data in public/component contracts.
- [ ] Update architecture docs and QA docs in the same phase that changes ownership.

## Autoplan Decision Audit Trail

| # | Phase | Decision | Classification | Principle | Rationale | Rejected |
|---|---|---|---|---|---|---|
| 1 | Scope | Use render ownership folders `Ssr`, `Hybrid`, `WasmHost` | Auto-decided | Maintainability over hidden convention | The user identified that current folders make render mode hard to recognize. Folder ownership makes intent visible in code review. | Keeping only business-area folders and relying on docs. |
| 2 | Account | Consolidate account to one `AccountHostPage` plus `AccountApp` | Auto-decided | Remove duplication without losing server guarantees | Current account pages duplicate shell/security/bootstrap work. A server host route keeps deep links and noindex while AccountApp owns feature UI. | Keeping many account route shell pages. |
| 3 | Account | Do not make account pure WASM route in this phase | Auto-decided | Preserve routing/security behavior | Current Blazor router and server host provide direct refresh, noindex, auth redirect, and antiforgery/bootstrap. | Removing server route boundary entirely. |
| 4 | Components | Move reusable capability UI into `Storefront.Components/Features` | Auto-decided | Component portability | Features like deals should be usable on home, product detail, and dedicated pages without rewriting logic. | Leaving feature UI embedded in page files. |
| 5 | Hydration | Require explicit data mode to prevent duplicate fetch | Auto-decided | Observable behavior and performance | Current components can receive initial data and still refetch after first render. A mode contract makes fetch behavior testable. | Letting each component invent its own hydration behavior. |
| 6 | Contracts | Keep component contracts narrow and Storefront-owned | Auto-decided | Respect V2 contract ownership | Architecture says new Storefront business models should not go into `Web.SharedV2`; generated OpenAPI is future direction, not a prerequisite. | Big-bang OpenAPI/client rewrite in this phase. |
| 7 | Catalog | Extract deals/new releases first as the proving feature | Auto-decided | Small useful tracer bullet | Deals is a clear reusable block that can appear on home, product detail, and a dedicated page. | Starting with checkout/account as the first portability proof. |

## Final Recommendation

Approve this plan as a staged Storefront rendering architecture refactor. The most important early wins are:

1. Make render ownership visible through folders.
2. Collapse account route-shell duplication into one server-hosted WASM account app.
3. Establish hydration rules so components do not duplicate fetch.
4. Introduce `Features/*` and prove portability with deals/new releases.

This should make the Storefront easier to scan and maintain while preserving the current V2 runtime boundaries.

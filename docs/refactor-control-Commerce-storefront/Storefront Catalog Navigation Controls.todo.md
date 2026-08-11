# Storefront Catalog Navigation Controls

Status: planned
Track: Phase 3.3 - V2 Component Extraction
Owner boundary: Storefront V2 / Storefront Components
Primary goal: extract reusable catalog navigation/control rendering from V2 without moving route ownership, runtime behavior, final visual styling, backend behavior, cart/checkout/account logic, or SEO placement.

## Decision

Phase 3.3 extracts exactly these components:

- [ ] `StorefrontPagination`
- [ ] `StorefrontCatalogFilterPanel`
- [ ] `StorefrontBreadcrumb`

Target ownership:

```text
BlazorShop.Storefront.Components
└── Contracts/Navigation
    ├── StorefrontPaginationItem.cs
    ├── StorefrontPaginationClasses.cs
    └── StorefrontPaginationLabels.cs

BlazorShop.Storefront.Components.Primitives
└── Navigation
    └── StorefrontPagination.razor

BlazorShop.Storefront.Components.Ssr
├── Catalog
│   ├── StorefrontCatalogFilterPanel.razor
│   ├── StorefrontCatalogFilterPanelClasses.cs
│   └── StorefrontCatalogFilterPanelLabels.cs
└── Navigation
    ├── StorefrontBreadcrumb.razor
    ├── StorefrontBreadcrumbClasses.cs
    └── StorefrontBreadcrumbLabels.cs
```

V2 remains owner of:

- [ ] page composition;
- [ ] final Tailwind classes;
- [ ] final storefront copy;
- [ ] category/search URL generation;
- [ ] query string values;
- [ ] `@rendermode` placement;
- [ ] search validation text;
- [ ] SEO placement;
- [ ] product grid placement.

## Codebase Evidence

Current reusable-looking V2 components:

- [ ] `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Components/Catalog/CatalogFilterPanel.razor`
- [ ] `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Components/Shared/BreadcrumbNav.razor`

Current Category page:

- [ ] `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/Hybrid/Catalog/CategoryPage.razor`
- [ ] uses `<BreadcrumbNav Items="Context.Breadcrumbs" />`;
- [ ] uses `<CatalogFilterPanel ... />`;
- [ ] owns manual pagination loop;
- [ ] calls `Context.Links.CategoryUrl(...)`;
- [ ] owns `GetPageLinkClass`.

Current Search page:

- [ ] `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/Hybrid/Catalog/SearchPage.razor`
- [ ] uses `<CatalogFilterPanel ... />`;
- [ ] owns manual pagination loop;
- [ ] calls `Context.Links.SearchUrl(...)`;
- [ ] owns duplicated `GetPageLinkClass`.

Current breadcrumb consumers found in V2:

- [ ] `Pages/Hybrid/Catalog/CategoryPage.razor`
- [ ] `Pages/Ssr/Content/StorefrontPage.razor`
- [ ] `Pages/Product/V2ProductPageView.razor`

Current contract locations:

- [ ] `CatalogFilterCategoryOption` already lives in `BlazorShop.Storefront.Components/Contracts/Catalog`.
- [ ] `ProductCatalogSortBy` lives in `BlazorShop.Storefront.Presentation/Models/StorefrontCatalogContentModels.cs`.
- [ ] `StorefrontBreadcrumbItem` lives in `BlazorShop.Storefront.Presentation/Services/StorefrontBreadcrumbItem.cs`.
- [ ] `Components.Ssr` currently references `Components` and `Presentation`.
- [ ] `Components.Primitives` currently references only `Components`.

## Architecture Rules

### Pagination

- [ ] `StorefrontPagination` must live in `Components.Primitives/Navigation`.
- [ ] It may reference only `BlazorShop.Storefront.Components`.
- [ ] It must consume prepared pagination items with final `Href` values.
- [ ] It must not reference `Presentation`.
- [ ] It must not call `Context.Links.CategoryUrl`.
- [ ] It must not call `Context.Links.SearchUrl`.
- [ ] It must not import `StorefrontRoutes`.
- [ ] It must not parse or build query strings.
- [ ] It must not contain `/api/`, `api/storefront`, Commerce Node URLs, or localhost URLs.
- [ ] It must not use `HttpClient`, `IJSRuntime`, Browser controllers, Runtime, Client, V2, V2.WASM, Starter, backend projects, Control Plane, Domain, Application, or Infrastructure.
- [ ] It must not use `@rendermode`.

### Catalog Filter

- [ ] `StorefrontCatalogFilterPanel` must live in `Components.Ssr/Catalog`.
- [ ] It may use approved Presentation catalog contracts, including `ProductCatalogSortBy`.
- [ ] It may use browser-safe component contracts, including `CatalogFilterCategoryOption`.
- [ ] It must not reference Browser, Runtime, Client, V2, V2.WASM, Starter, backend projects, Control Plane, Domain, Application, or Infrastructure.
- [ ] It must not use `HttpClient`.
- [ ] It must not use `IJSRuntime`.
- [ ] It must not use `@rendermode`.
- [ ] It must render a normal SSR-safe `method="get"` form.
- [ ] It must preserve current query input names exactly: `category`, `q`, `minPrice`, `maxPrice`, `sortBy`, `pageSize`, `inStock`.
- [ ] It may render the host-supplied `Action`, but it must not construct routes.

### Breadcrumb

- [ ] `StorefrontBreadcrumb` must live in `Components.Ssr/Navigation`.
- [ ] It may consume the current `StorefrontBreadcrumbItem` from `Presentation`.
- [ ] Do not move `StorefrontBreadcrumbItem` into `Components` during this phase unless compilation forces it and the dependency graph is reviewed again.
- [ ] It must not reference Browser, Runtime, Client, V2, V2.WASM, Starter, backend projects, Control Plane, Domain, Application, or Infrastructure.
- [ ] It must not use `@rendermode`.

### Visual Ownership

- [ ] Reusable component packages must not own final V2 Tailwind class literals.
- [ ] Reusable components must expose class slots through records or parameters.
- [ ] V2 must provide the final classes.
- [ ] Reusable components must not own store-specific copy.
- [ ] V2 must provide final labels and placeholder text.
- [ ] Semantic `data-storefront-*` hooks are allowed.
- [ ] Accessibility attributes are allowed.

## Explicit Non-Goals

Do not change or extract:

- [ ] `StorefrontProductSummaryGrid`;
- [ ] `StorefrontDealsSection`;
- [ ] `HeroBanner`;
- [ ] `StorefrontHeader`;
- [ ] `StorefrontFooter`;
- [ ] `StorefrontProductPurchasePanel`;
- [ ] product detail page layout;
- [ ] cart;
- [ ] checkout;
- [ ] account;
- [ ] consent;
- [ ] SEO content components;
- [ ] Storefront Runtime;
- [ ] Storefront Client;
- [ ] Browser controllers;
- [ ] Commerce Node;
- [ ] Control Plane;
- [ ] database schema;
- [ ] StorefrontBuilder;
- [ ] Starter;
- [ ] generated storefronts.

Do not introduce:

- [ ] a new component project;
- [ ] a design system package;
- [ ] a route registry;
- [ ] a generic form framework;
- [ ] a pagination service;
- [ ] a query-string builder inside reusable components;
- [ ] route ownership inside reusable components;
- [ ] visual polish beyond functional preservation.

## Phase 3.3.0 - Baseline Lock

Goal: record the current behavior and prevent scope drift before moving files.

Tasks:

- [x] Record `git status --short`.
- [x] Confirm any existing untracked plan files and do not modify unrelated docs.
- [x] Read `AGENTS.md`.
- [x] Read `BlazorShop.PresentationV2/COMPONENT-MODES.md`.
- [x] Read `docs/architecture/05-project-and-folder-guide.md`.
- [x] Read `docs/architecture/10-v2-contract-ownership.md`.
- [x] Read `docs/refactor-control-Commerce-storefront/Storefront Product Summary Primitives.todo.md`.
- [x] Read `docs/refactor-control-Commerce-storefront/Storefront Product Detail Display Components.todo.md`.
- [x] Inspect `CategoryPage.razor`.
- [x] Inspect `SearchPage.razor`.
- [x] Inspect `CatalogFilterPanel.razor`.
- [x] Inspect `BreadcrumbNav.razor`.
- [x] Run `rg -n "CatalogFilterPanel|BreadcrumbNav|GetPageLinkClass|CategoryUrl\\(|SearchUrl\\(" BlazorShop.PresentationV2 BlazorShop.Tests.V2`.
- [x] Record all active `BreadcrumbNav` consumers.
- [x] Record all active `CatalogFilterPanel` consumers.
- [x] Record all duplicated pagination loops.
- [x] Record current filter query input names.
- [x] Record current sort option values.
- [x] Record current page-size options.
- [x] Record current category/search actions.
- [x] Record current breadcrumb behavior for zero, one, and multiple items.

Exit criteria:

- [x] baseline evidence exists in commit notes or closure notes;
- [x] current consumers are known;
- [x] no implementation edits made before baseline is complete.

Baseline evidence (2026-08-11): the worktree contained only this untracked plan. `BreadcrumbNav` is consumed by Category, Product, and Content; it emits nothing for zero or one item and emits linked ancestors, a current-item span with `aria-current="page"`, and `/` separators for two or more items. `CatalogFilterPanel` is consumed by Category and Search. Category and Search each have a manual loop plus `GetPageLinkClass`; Category calls `Context.Links.CategoryUrl(...)`, Search calls `Context.Links.SearchUrl(...)`. The filter GET fields are `category`, `q`, `minPrice`, `maxPrice`, `sortBy`, `pageSize`, and `inStock`; sort values come from `DisplayOrder`, `Updated`, `PriceLowToHigh`, `PriceHighToLow`, and `Newest` through `ToApiValue()`, with page sizes `12`, `24`, and `48`. Category uses the current URL action; Search supplies `Context.Links.Search.Href` and `role="search"`.

## Phase 3.3.1 - Define Pagination Contracts

Goal: create the smallest browser-safe pagination render contract.

Files:

- [x] Add `BlazorShop.PresentationV2/BlazorShop.Storefront.Components/Contracts/Navigation/StorefrontPaginationItem.cs`.
- [x] Add `BlazorShop.PresentationV2/BlazorShop.Storefront.Components/Contracts/Navigation/StorefrontPaginationClasses.cs`.
- [x] Add `BlazorShop.PresentationV2/BlazorShop.Storefront.Components/Contracts/Navigation/StorefrontPaginationLabels.cs`.

Contract shape:

- [x] `StorefrontPaginationItem` contains `int PageNumber`.
- [x] `StorefrontPaginationItem` contains `string Href`.
- [x] `StorefrontPaginationItem` contains `bool IsCurrent`.
- [x] `StorefrontPaginationItem` contains optional `string? Label`.
- [x] `StorefrontPaginationItem` does not contain category slug.
- [x] `StorefrontPaginationItem` does not contain search term.
- [x] `StorefrontPaginationItem` does not contain sort/filter values.
- [x] `StorefrontPaginationItem` does not contain Presentation link context.
- [x] `StorefrontPaginationItem` does not contain route service references.
- [x] `StorefrontPaginationClasses` has a small slot surface, preferably `Root`, `Link`, `CurrentLink`, and `InactiveLink`.
- [x] `StorefrontPaginationLabels` includes `AriaLabel`.
- [x] Use neutral defaults only where they are semantic, not visual.

Test expectations:

- [x] Component contract source contains no `BlazorShop.Storefront.Presentation`.
- [x] Component contract source contains no `StorefrontRoutes`.
- [x] Component contract source contains no `CategoryUrl`.
- [x] Component contract source contains no `SearchUrl`.

Exit criteria:

- [x] contracts compile in base `Components`;
- [x] contracts are browser-safe;
- [x] no route-specific fields were added.

## Phase 3.3.2 - Implement Pagination Primitive

Goal: move duplicated pagination rendering into one browser-safe primitive.

Files:

- [x] Add `BlazorShop.PresentationV2/BlazorShop.Storefront.Components.Primitives/Navigation/StorefrontPagination.razor`.

Inputs:

- [x] `IReadOnlyList<StorefrontPaginationItem> Items`.
- [x] `StorefrontPaginationClasses Classes`.
- [x] `StorefrontPaginationLabels Labels`.
- [x] Optional `string? DataStorefrontId` only if consistent with existing primitive hooks.

Rendering behavior:

- [x] Render nothing when `Items.Count == 0`.
- [x] Render `<nav>` when at least one item exists.
- [x] Use `Labels.AriaLabel` for `aria-label`.
- [x] Render one link per item.
- [x] Use `item.Href` exactly as supplied.
- [x] Use `item.Label` when present.
- [x] Fall back to `item.PageNumber` when label is missing.
- [x] Apply `aria-current="page"` only when `item.IsCurrent`.
- [x] Apply current and inactive class slots correctly.
- [x] Do not compute URL values.
- [x] Do not mutate `Href`.

Forbidden:

- [x] no literal Tailwind classes;
- [x] no `Presentation` namespace;
- [x] no V2 namespace;
- [x] no route helpers;
- [x] no API calls;
- [x] no render mode directives;
- [x] no JS interop.

Exit criteria:

- [x] primitive compiles;
- [x] primitive dependency guardrails pass;
- [x] primitive visual-neutrality tests cover it.

## Phase 3.3.3 - Adopt Pagination In Category

Goal: replace Category page manual pagination without changing URLs.

File:

- [x] Update `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/Hybrid/Catalog/CategoryPage.razor`.

Tasks:

- [x] Add required namespace import for `Components.Primitives.Navigation`.
- [x] Add required namespace import for `Components.Contracts.Navigation`.
- [x] Build `StorefrontPaginationItem[]` or `IReadOnlyList<StorefrontPaginationItem>` in V2 page code.
- [x] Preserve URL call exactly:

```csharp
Context.Links.CategoryUrl(
    Context.Slug,
    pageNumber,
    Context.PageSize,
    Context.SortBy,
    Context.MinPrice,
    Context.MaxPrice,
    Context.InStock ? true : null)
```

- [x] Pass prepared items to `StorefrontPagination`.
- [x] Pass V2-owned classes to `StorefrontPagination`.
- [x] Pass V2-owned `AriaLabel` value equivalent to `Category product pages`.
- [x] Remove manual `<nav>` loop.
- [x] Remove `GetPageLinkClass` from Category if unused.
- [x] Keep product grid placement unchanged.
- [x] Keep category heading/description/result count unchanged.
- [x] Keep SEO content placement unchanged.

Exit criteria:

- [x] category pagination route behavior unchanged;
- [x] category query state preserved across page links;
- [x] reusable primitive owns rendering only;
- [x] V2 still owns URL generation and final classes.

## Phase 3.3.4 - Adopt Pagination In Search

Goal: replace Search page manual pagination without changing URLs.

File:

- [ ] Update `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/Hybrid/Catalog/SearchPage.razor`.

Tasks:

- [ ] Add required namespace import for `Components.Primitives.Navigation`.
- [ ] Add required namespace import for `Components.Contracts.Navigation`.
- [ ] Build `StorefrontPaginationItem[]` or `IReadOnlyList<StorefrontPaginationItem>` in V2 page code.
- [ ] Preserve URL call exactly:

```csharp
Context.Links.SearchUrl(
    Context.Q,
    Context.Category,
    pageNumber,
    Context.PageSize,
    Context.SortBy,
    Context.MinPrice,
    Context.MaxPrice,
    Context.InStock ? true : null)
```

- [ ] Pass prepared items to `StorefrontPagination`.
- [ ] Pass V2-owned classes to `StorefrontPagination`.
- [ ] Pass V2-owned `AriaLabel` value equivalent to `Search result pages`.
- [ ] Remove manual `<nav>` loop.
- [ ] Remove `GetPageLinkClass` from Search if unused.
- [ ] Keep search heading/scope/result count unchanged.
- [ ] Keep short-search validation unchanged.
- [ ] Keep product grid placement unchanged.

Exit criteria:

- [ ] search pagination route behavior unchanged;
- [ ] search query state preserved across page links;
- [ ] duplicated pagination loop removed.

## Phase 3.3.5 - Define Catalog Filter Visual And Label Contracts

Goal: prepare the SSR filter component so it is reusable but not visually opinionated.

Files:

- [ ] Add `BlazorShop.PresentationV2/BlazorShop.Storefront.Components.Ssr/Catalog/StorefrontCatalogFilterPanelClasses.cs`.
- [ ] Add `BlazorShop.PresentationV2/BlazorShop.Storefront.Components.Ssr/Catalog/StorefrontCatalogFilterPanelLabels.cs`.

Class slots:

- [ ] `Root`.
- [ ] `Input`.
- [ ] `Select`.
- [ ] `CheckboxLabel`.
- [ ] `SubmitButton`.

Optional slots only if required by current markup:

- [ ] `Checkbox`.
- [ ] `SubmitIcon`.

Label slots:

- [ ] `AllCategories`.
- [ ] `SearchPlaceholder`.
- [ ] `MinPricePlaceholder`.
- [ ] `MaxPricePlaceholder`.
- [ ] `SortAriaLabel`.
- [ ] `CategoryAriaLabel`.
- [ ] `PageSizeAriaLabel`.
- [ ] `FeaturedSort`.
- [ ] `RecentlyUpdatedSort`.
- [ ] `PriceLowSort`.
- [ ] `PriceHighSort`.
- [ ] `NewestSort`.
- [ ] `InStock`.
- [ ] `Submit`.
- [ ] `PerPageSuffix` or formatter if needed.

Rules:

- [ ] Do not create a broad localization framework.
- [ ] Do not create a generic form framework.
- [ ] Do not create per-store service dependencies.
- [ ] Keep labels simple and host-supplied.
- [ ] Keep default labels only as neutral technical fallback, if needed for tests.

Exit criteria:

- [ ] reusable filter can render without V2 class literals;
- [ ] V2 can keep the exact current visible copy by supplying labels.

## Phase 3.3.6 - Extract Catalog Filter To SSR

Goal: move the existing V2 filter implementation into the reusable SSR project.

Source:

- [ ] `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Components/Catalog/CatalogFilterPanel.razor`

Target:

- [ ] `BlazorShop.PresentationV2/BlazorShop.Storefront.Components.Ssr/Catalog/StorefrontCatalogFilterPanel.razor`

Tasks:

- [ ] Rename component from `CatalogFilterPanel` to `StorefrontCatalogFilterPanel`.
- [ ] Set namespace to `BlazorShop.Storefront.Components.Ssr.Catalog`.
- [ ] Keep `method="get"`.
- [ ] Keep host-supplied `Action`.
- [ ] Keep optional `Role`.
- [ ] Preserve `ShowCategory`.
- [ ] Preserve `Categories`.
- [ ] Preserve `CategorySlug`.
- [ ] Preserve `ShowSearch`.
- [ ] Preserve `SearchTerm`.
- [ ] Preserve `ShowPriceRange`.
- [ ] Preserve `MinPrice`.
- [ ] Preserve `MaxPrice`.
- [ ] Preserve `ShowSort`.
- [ ] Preserve `SortBy`.
- [ ] Preserve `ShowPageSize`.
- [ ] Preserve `PageSize`.
- [ ] Preserve `PageSizeOptions`.
- [ ] Preserve `ShowStock`.
- [ ] Preserve `InStock`.
- [ ] Preserve `RenderFragment? SubmitIcon`.
- [ ] Replace individual class string parameters with `StorefrontCatalogFilterPanelClasses` unless the local pattern strongly favors flat parameters.
- [ ] Replace direct copy literals with `StorefrontCatalogFilterPanelLabels`.
- [ ] Keep query input names exactly:
  - [ ] `category`
  - [ ] `q`
  - [ ] `minPrice`
  - [ ] `maxPrice`
  - [ ] `sortBy`
  - [ ] `pageSize`
  - [ ] `inStock`
- [ ] Keep `ProductCatalogSortBy.*.ToApiValue()` values.
- [ ] Do not add client-side submit logic.
- [ ] Do not add JS.
- [ ] Do not add API/BFF calls.

Cleanup:

- [ ] Delete old V2 `Components/Catalog/CatalogFilterPanel.razor` after both Category and Search compile against the new component.
- [ ] Remove obsolete V2 namespace import if unused.

Exit criteria:

- [ ] filter component lives in `Components.Ssr`;
- [ ] query contract unchanged;
- [ ] V2 owns final classes/copy through supplied records;
- [ ] no Browser/runtime/client/backend dependency introduced.

## Phase 3.3.7 - Adopt Catalog Filter In Category

Goal: make Category use the reusable SSR filter.

File:

- [ ] Update `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/Hybrid/Catalog/CategoryPage.razor`.

Tasks:

- [ ] Replace `<CatalogFilterPanel` with `<StorefrontCatalogFilterPanel`.
- [ ] Preserve `ShowPriceRange="true"`.
- [ ] Preserve `MinPrice="Context.MinPrice"`.
- [ ] Preserve `MaxPrice="Context.MaxPrice"`.
- [ ] Preserve `ShowSort="true"`.
- [ ] Preserve `SortBy="Context.SortBy"`.
- [ ] Preserve `ShowPageSize="true"`.
- [ ] Preserve `PageSize="Context.PageSize"`.
- [ ] Preserve `ShowStock="true"`.
- [ ] Preserve `InStock="Context.InStock"`.
- [ ] Supply V2-owned filter classes matching current visual output.
- [ ] Supply V2-owned filter labels matching current visible copy.
- [ ] Do not add search/category fields to Category unless already present.
- [ ] Do not change form action behavior.

Exit criteria:

- [ ] Category filter GET behavior unchanged;
- [ ] Category filter visual class ownership moved to V2 call site/config;
- [ ] no query regression.

## Phase 3.3.8 - Adopt Catalog Filter In Search

Goal: make Search use the reusable SSR filter.

File:

- [ ] Update `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/Hybrid/Catalog/SearchPage.razor`.

Tasks:

- [ ] Replace `<CatalogFilterPanel` with `<StorefrontCatalogFilterPanel`.
- [ ] Preserve `Action="@Context.Links.Search.Href"`.
- [ ] Preserve `Role="search"`.
- [ ] Preserve `ShowCategory="true"`.
- [ ] Preserve `Categories="Context.SearchCategories"`.
- [ ] Preserve `CategorySlug="@Context.Category"`.
- [ ] Preserve `ShowSearch="true"`.
- [ ] Preserve `SearchTerm="@Context.Q"`.
- [ ] Preserve `ShowPriceRange="true"`.
- [ ] Preserve `MinPrice="Context.MinPrice"`.
- [ ] Preserve `MaxPrice="Context.MaxPrice"`.
- [ ] Preserve `ShowSort="true"`.
- [ ] Preserve `SortBy="Context.SortBy"`.
- [ ] Preserve `ShowPageSize="true"`.
- [ ] Preserve `PageSize="Context.PageSize"`.
- [ ] Preserve `ShowStock="true"`.
- [ ] Preserve `InStock="Context.InStock"`.
- [ ] Preserve `SubmitIcon` inline SVG or move it to V2-owned local helper.
- [ ] Preserve current visible submit label `Search`.
- [ ] Supply V2-owned filter classes matching current visual output.
- [ ] Supply V2-owned filter labels matching current visible copy.

Exit criteria:

- [ ] Search filter GET behavior unchanged;
- [ ] Search category/search/price/sort/page-size/stock fields remain present;
- [ ] Search short-term validation remains outside the reusable filter.

## Phase 3.3.9 - Define Breadcrumb Visual And Label Contracts

Goal: make breadcrumb reusable without moving Presentation breadcrumb data.

Files:

- [ ] Add `BlazorShop.PresentationV2/BlazorShop.Storefront.Components.Ssr/Navigation/StorefrontBreadcrumbClasses.cs`.
- [ ] Add `BlazorShop.PresentationV2/BlazorShop.Storefront.Components.Ssr/Navigation/StorefrontBreadcrumbLabels.cs` only if needed.

Class slots:

- [ ] `Root`.
- [ ] `List`.
- [ ] `Item`.
- [ ] `Link`.
- [ ] `Current`.
- [ ] `Separator`.

Label/semantic slots:

- [ ] `AriaLabel`, defaulting to `Breadcrumb` if a fallback is needed.
- [ ] `SeparatorText`, defaulting to `/` only if allowed by visual neutrality tests.

Rules:

- [ ] Keep `IReadOnlyList<StorefrontBreadcrumbItem>` input.
- [ ] Do not move `StorefrontBreadcrumbItem`.
- [ ] Do not add route generation.
- [ ] Do not inspect URL/current route.
- [ ] Current item remains last item in the supplied list.

Exit criteria:

- [ ] breadcrumb can render current V2 output from supplied classes/labels;
- [ ] Presentation breadcrumb contract remains unchanged.

## Phase 3.3.10 - Extract Breadcrumb To SSR

Goal: move existing breadcrumb rendering from V2 to the reusable SSR project.

Source:

- [ ] `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Components/Shared/BreadcrumbNav.razor`

Target:

- [ ] `BlazorShop.PresentationV2/BlazorShop.Storefront.Components.Ssr/Navigation/StorefrontBreadcrumb.razor`

Tasks:

- [ ] Rename component from `BreadcrumbNav` to `StorefrontBreadcrumb`.
- [ ] Set namespace to `BlazorShop.Storefront.Components.Ssr.Navigation`.
- [ ] Preserve behavior of rendering only when `Items.Count > 1`.
- [ ] Preserve linked ancestor behavior.
- [ ] Preserve current item span behavior.
- [ ] Preserve `aria-current="page"` on current item.
- [ ] Preserve separator rendering between items.
- [ ] Replace V2 Tailwind literals with class slots.
- [ ] Use supplied `AriaLabel`.
- [ ] Do not add route matching or active-item detection.

Cleanup:

- [ ] Delete old V2 `Components/Shared/BreadcrumbNav.razor` after all active consumers migrate.
- [ ] Remove obsolete V2 namespace import if unused.

Exit criteria:

- [ ] one breadcrumb implementation remains;
- [ ] V2 still owns final classes/copy;
- [ ] no route/runtime dependency added.

## Phase 3.3.11 - Migrate Breadcrumb Consumers

Goal: update all active V2 breadcrumb consumers in the same phase.

Files:

- [ ] Update `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/Hybrid/Catalog/CategoryPage.razor`.
- [ ] Update `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/Ssr/Content/StorefrontPage.razor`.
- [ ] Update `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/Product/V2ProductPageView.razor`.

Tasks:

- [ ] Replace `<BreadcrumbNav Items="...">` with `<StorefrontBreadcrumb Items="...">`.
- [ ] Supply V2-owned breadcrumb classes.
- [ ] Supply V2-owned breadcrumb labels if needed.
- [ ] Keep `StorefrontPageShell` breadcrumb slot usage unchanged.
- [ ] Keep Product breadcrumb data mapping unchanged.
- [ ] Keep Content breadcrumb data mapping unchanged.
- [ ] Keep Category breadcrumb data mapping unchanged.

Exit criteria:

- [ ] no active V2 page uses `<BreadcrumbNav`;
- [ ] Product breadcrumb still renders;
- [ ] Content breadcrumb still renders;
- [ ] Category breadcrumb still renders.

## Phase 3.3.12 - V2 Visual Configuration

Goal: keep final styling and visible copy centralized enough for maintainability without inventing a design system.

Preferred file:

- [ ] `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Components/Catalog/CatalogNavigationVisuals.cs`

Allowed split if the single file becomes noisy:

- [ ] `Components/Catalog/CatalogFilterVisuals.cs`
- [ ] `Components/Navigation/StorefrontNavigationVisuals.cs`

Responsibilities:

- [ ] Provide `StorefrontPaginationClasses`.
- [ ] Provide Category pagination labels.
- [ ] Provide Search pagination labels.
- [ ] Provide `StorefrontCatalogFilterPanelClasses`.
- [ ] Provide Category filter labels.
- [ ] Provide Search filter labels.
- [ ] Provide `StorefrontBreadcrumbClasses`.
- [ ] Provide breadcrumb labels.

Rules:

- [ ] Do not create one visual class file per tiny component unless readability requires it.
- [ ] Do not move page headings/result-count copy into reusable packages.
- [ ] Do not move search validation copy into reusable packages.
- [ ] Keep V2 CSS classes readable at call sites or in a small V2 config class.

Exit criteria:

- [ ] reusable packages contain no final V2 Tailwind literals;
- [ ] V2 controls visual output.

## Phase 3.3.13 - Imports And Project References

Goal: keep compile graph explicit and minimal.

Tasks:

- [ ] Confirm `BlazorShop.Storefront.Components.Primitives` already references `BlazorShop.Storefront.Components`.
- [ ] Confirm `BlazorShop.Storefront.Components.Ssr` already references `BlazorShop.Storefront.Components`.
- [ ] Confirm `BlazorShop.Storefront.Components.Ssr` already references `BlazorShop.Storefront.Presentation`.
- [ ] Add namespace imports only where needed.
- [ ] Prefer `_Imports.razor` only if multiple V2 files consume the namespace.
- [ ] Do not add `Components.Ssr` reference to V2.WASM.
- [ ] Do not add `Presentation` reference to `Components.Primitives`.
- [ ] Do not add Browser/Runtime/Client references to SSR or Primitives.

Exit criteria:

- [ ] project graph remains unchanged except necessary compile references already present;
- [ ] V2.WASM remains isolated from Presentation/SSR where existing tests require it.

## Phase 3.3.14 - Update Existing Characterization Tests

Goal: replace tests that lock old V2 component names/paths with tests that lock the new ownership and unchanged behavior.

File:

- [ ] `BlazorShop.Tests.V2/PresentationV2/LayoutAssetFoundationTests.cs`

Current tests to update:

- [ ] `StorefrontCatalogFilterPanel_PreservesQueryStringContract`.
- [ ] `StorefrontCategoryAndSearchPages_UseCatalogFilterPanelWithoutRouteChanges`.
- [ ] tests asserting `<BreadcrumbNav`.

Required new assertions:

- [ ] Filter source is read from `BlazorShop.Storefront.Components.Ssr/Catalog/StorefrontCatalogFilterPanel.razor`.
- [ ] Filter source still contains `method="get"`.
- [ ] Filter source still contains `name="category"`.
- [ ] Filter source still contains `name="q"`.
- [ ] Filter source still contains `name="minPrice"`.
- [ ] Filter source still contains `name="maxPrice"`.
- [ ] Filter source still contains `name="sortBy"`.
- [ ] Filter source still contains `name="pageSize"`.
- [ ] Filter source still contains `name="inStock"`.
- [ ] Filter source still uses `ProductCatalogSortBy.DisplayOrder.ToApiValue()`.
- [ ] Filter source still uses `ProductCatalogSortBy.PriceLowToHigh.ToApiValue()`.
- [ ] Filter source still uses `ProductCatalogSortBy.PriceHighToLow.ToApiValue()`.
- [ ] Filter source does not contain `onclick`.
- [ ] Category page contains `<StorefrontCatalogFilterPanel`.
- [ ] Search page contains `<StorefrontCatalogFilterPanel`.
- [ ] Category page still contains `Context.Links.CategoryUrl(`.
- [ ] Search page still contains `Context.Links.SearchUrl(`.
- [ ] Category page contains `<StorefrontPagination`.
- [ ] Search page contains `<StorefrontPagination`.
- [ ] Category page no longer contains manual pagination `<nav>` loop text.
- [ ] Search page no longer contains manual pagination `<nav>` loop text.
- [ ] Category page no longer contains `GetPageLinkClass`.
- [ ] Search page no longer contains `GetPageLinkClass`.
- [ ] Product/Content/Category page tests assert `<StorefrontBreadcrumb`.
- [ ] Tests do not assert `<BreadcrumbNav`.

Exit criteria:

- [ ] tests lock behavior and ownership, not stale V2 filenames.

## Phase 3.3.15 - Add Focused Component Tests

Goal: cover the reusable components directly.

Suggested files:

- [ ] `BlazorShop.Tests.V2/PresentationV2/Storefront/StorefrontPaginationPrimitiveTests.cs`.
- [ ] `BlazorShop.Tests.V2/PresentationV2/Storefront/StorefrontCatalogFilterPanelSsrTests.cs`.
- [ ] `BlazorShop.Tests.V2/PresentationV2/Storefront/StorefrontBreadcrumbSsrTests.cs`.

Pagination tests:

- [ ] no items renders no navigation or no page links, matching chosen component behavior.
- [ ] one item renders one link when provided.
- [ ] multiple items render all links.
- [ ] current item receives `aria-current="page"`.
- [ ] non-current items do not receive `aria-current`.
- [ ] current class slot is used.
- [ ] inactive class slot is used.
- [ ] host-supplied `Href` is emitted unchanged.
- [ ] host-supplied label is used.
- [ ] page number fallback is used when label is missing.
- [ ] `aria-label` comes from labels.

Catalog filter tests:

- [ ] category select can be hidden.
- [ ] category select can be shown.
- [ ] selected category is marked selected.
- [ ] search input can be hidden.
- [ ] search input can be shown.
- [ ] search term is preserved.
- [ ] price inputs render decimal values.
- [ ] sort select renders allowed values.
- [ ] selected sort is marked selected.
- [ ] page-size select renders configured options.
- [ ] selected page size is marked selected.
- [ ] in-stock checkbox is checked when `InStock=true`.
- [ ] submit label renders.
- [ ] submit icon slot renders.
- [ ] query names are unchanged.
- [ ] class slots are applied.
- [ ] no JavaScript event handler appears.

Breadcrumb tests:

- [ ] zero items render nothing.
- [ ] one item render behavior matches existing behavior.
- [ ] two or more items render `<nav>`.
- [ ] ancestor item with `Href` renders as link.
- [ ] current item renders as span.
- [ ] current item receives `aria-current="page"`.
- [ ] separator renders between items.
- [ ] class slots are applied.
- [ ] no route helper usage.

Exit criteria:

- [ ] new components have semantic coverage independent of V2 pages;
- [ ] no browser/runtime setup required for component tests.

## Phase 3.3.16 - Architecture Guardrail Tests

Goal: prevent the extraction from weakening component boundaries.

Update or add tests under:

- [ ] `BlazorShop.Tests.V2/PresentationV2/Storefront/StorefrontPrimitiveDependencyTests.cs`.
- [ ] `BlazorShop.Tests.V2/PresentationV2/Storefront/StorefrontComponentModeDependencyTests.cs`.
- [ ] `BlazorShop.Tests.V2/PresentationV2/Storefront/StorefrontRenderModeOwnershipTests.cs`.
- [ ] `BlazorShop.Tests.V2/PresentationV2/Storefront/StorefrontServerInteractiveTransportGuardrailTests.cs`.

Assertions:

- [ ] `Components.Primitives/Navigation/StorefrontPagination.razor` contains no `BlazorShop.Storefront.Presentation`.
- [ ] `Components.Primitives/Navigation/StorefrontPagination.razor` contains no `CategoryUrl`.
- [ ] `Components.Primitives/Navigation/StorefrontPagination.razor` contains no `SearchUrl`.
- [ ] `Components.Primitives/Navigation/StorefrontPagination.razor` contains no `StorefrontRoutes`.
- [ ] `Components.Primitives/Navigation/StorefrontPagination.razor` contains no `/api/`.
- [ ] `Components.Primitives/Navigation/StorefrontPagination.razor` contains no `@rendermode`.
- [ ] `Components.Ssr/Catalog/StorefrontCatalogFilterPanel.razor` contains no Browser/Runtime/Client namespace.
- [ ] `Components.Ssr/Catalog/StorefrontCatalogFilterPanel.razor` contains no `HttpClient`.
- [ ] `Components.Ssr/Catalog/StorefrontCatalogFilterPanel.razor` contains no `IJSRuntime`.
- [ ] `Components.Ssr/Catalog/StorefrontCatalogFilterPanel.razor` contains no `@rendermode`.
- [ ] `Components.Ssr/Navigation/StorefrontBreadcrumb.razor` contains no Browser/Runtime/Client namespace.
- [ ] `Components.Ssr/Navigation/StorefrontBreadcrumb.razor` contains no route construction.
- [ ] `Components.Ssr/Navigation/StorefrontBreadcrumb.razor` contains no `@rendermode`.

Exit criteria:

- [ ] component boundary regression fails tests immediately.

## Phase 3.3.17 - Visual Neutrality Tests

Goal: ensure reusable packages do not inherit V2 visual ownership.

Update:

- [ ] `BlazorShop.Tests.V2/PresentationV2/Storefront/StorefrontComponentVisualNeutralityTests.cs`.
- [ ] `BlazorShop.Tests.V2/PresentationV2/Storefront/StorefrontBrandingMarkupTests.cs` if it tracks component literals.

Assertions:

- [ ] include `Components.Primitives/Navigation/StorefrontPagination.razor` in scanned paths.
- [ ] include `Components.Ssr/Catalog/StorefrontCatalogFilterPanel.razor` in scanned paths.
- [ ] include `Components.Ssr/Navigation/StorefrontBreadcrumb.razor` in scanned paths.
- [ ] reusable components contain no literal Tailwind class strings such as `rounded-`, `bg-`, `text-`, `shadow`, `px-`, `mx-`, `sm:`, `md:`, `lg:`, `xl:`.
- [ ] reusable components use `class="@..."` or computed host-supplied classes.
- [ ] V2 files may contain final classes.

Negative fixtures:

- [ ] fixture fails if pagination primitive includes a literal Tailwind class.
- [ ] fixture fails if SSR filter includes a literal V2 class.
- [ ] fixture fails if SSR breadcrumb includes a literal V2 class.

Exit criteria:

- [ ] reusable packages remain visually neutral;
- [ ] V2 remains final visual owner.

## Phase 3.3.18 - Functional Browser QA

Goal: prove the extraction did not break real browser catalog behavior.

Start local runtime:

```powershell
.\scripts\run-v2-local.ps1 -StopExisting
```

Use Playwright browser verification.

Category checks:

- [ ] open a seeded category page, for example `/category/t-shirts`.
- [ ] breadcrumb is visible.
- [ ] category heading is visible.
- [ ] filter panel is visible.
- [ ] submit price filter `minPrice` and `maxPrice`.
- [ ] verify URL keeps `minPrice` and `maxPrice`.
- [ ] select sort option.
- [ ] verify URL keeps `sortBy`.
- [ ] select page size.
- [ ] verify URL keeps `pageSize`.
- [ ] toggle in-stock.
- [ ] verify URL keeps `inStock=true`.
- [ ] use pagination link.
- [ ] verify URL keeps category filter state across pagination.
- [ ] verify product cards still render or valid empty state renders.

Search checks:

- [ ] open `/search`.
- [ ] enter valid query.
- [ ] submit search form.
- [ ] verify URL keeps `q`.
- [ ] select category filter.
- [ ] verify URL keeps `category`.
- [ ] apply price/sort/page-size/in-stock filters.
- [ ] verify URL keeps all active query state.
- [ ] use pagination link.
- [ ] verify query state survives pagination.
- [ ] open a too-short search term.
- [ ] verify short-search validation still appears.
- [ ] verify product cards still render or valid empty state renders.

Product/content breadcrumb checks:

- [ ] open a seeded product page.
- [ ] verify product breadcrumb renders.
- [ ] open a content page with breadcrumb.
- [ ] verify content breadcrumb renders.

Network/runtime checks:

- [ ] no browser request goes directly to Commerce Node Storefront API.
- [ ] no direct `api/storefront/stores/*` browser request appears unless it is same-origin BFF-owned and already expected.
- [ ] no `/_blazor` public server UI circuit appears.
- [ ] browser console has no errors.
- [ ] page errors are absent.

Stop runtime:

```powershell
.\scripts\stop-v2-local.ps1
```

Exit criteria:

- [ ] real browser behavior preserved;
- [ ] no direct Commerce browser transport introduced.

## Phase 3.3.19 - Build Gate

Focused builds:

```powershell
dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.Components/BlazorShop.Storefront.Components.csproj --no-restore
dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.Components.Primitives/BlazorShop.Storefront.Components.Primitives.csproj --no-restore
dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.Components.Ssr/BlazorShop.Storefront.Components.Ssr.csproj --no-restore
dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.V2/BlazorShop.Storefront.V2.csproj --no-restore
```

Conditional build:

- [ ] build `BlazorShop.Storefront.V2.WASM` only if base contracts/imports affect WASM graph.

Full build before closure:

```powershell
dotnet build BlazorShop.sln --no-restore
```

Exit criteria:

- [ ] focused builds pass;
- [ ] full solution build passes;
- [ ] no new warnings accepted without note.

## Phase 3.3.20 - Test Gate

Focused tests:

```powershell
dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~StorefrontPagination|FullyQualifiedName~CatalogFilter|FullyQualifiedName~Breadcrumb|FullyQualifiedName~PrimitiveDependency|FullyQualifiedName~ComponentModeDependency|FullyQualifiedName~ComponentVisualNeutrality|FullyQualifiedName~RenderModeOwnership|FullyQualifiedName~VisualOnlyBoundary|FullyQualifiedName~LayoutAssetFoundation"
```

Full tests before closure:

```powershell
dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-restore
```

Required passing areas:

- [ ] pagination primitive tests;
- [ ] catalog filter SSR tests;
- [ ] breadcrumb SSR tests;
- [ ] layout asset foundation tests;
- [ ] primitive dependency tests;
- [ ] component mode dependency tests;
- [ ] visual-neutrality tests;
- [ ] render-mode ownership tests;
- [ ] visual-only boundary tests;
- [ ] search page regression tests;
- [ ] V2 host smoke tests relevant to Category/Search/Product/Content.

Exit criteria:

- [ ] focused tests pass;
- [ ] full tests pass or unrelated failures are documented with exact names.

## Phase 3.3.21 - Duplication Removal Audit

Run:

```powershell
rg -n "GetPageLinkClass|<BreadcrumbNav|<CatalogFilterPanel|aria-label=\"Category product pages\"|aria-label=\"Search result pages\"" BlazorShop.PresentationV2 BlazorShop.Tests.V2
```

Expected:

- [ ] no active V2 source uses `GetPageLinkClass`.
- [ ] no active V2 source uses `<BreadcrumbNav`.
- [ ] no active V2 source uses `<CatalogFilterPanel`.
- [ ] no active V2 source contains old manual pagination loops.

Allowed:

- [ ] new tests may mention old names only inside negative assertions.
- [ ] docs may mention old names as migration history.
- [ ] V2 pages may still contain `Context.Links.CategoryUrl`.
- [ ] V2 pages may still contain `Context.Links.SearchUrl`.
- [ ] V2 visual config may contain final classes and labels.

Exit criteria:

- [ ] duplicate render implementation removed;
- [ ] stale old component implementation deleted.

## Phase 3.3.22 - V2 Composition Audit

Goal: confirm V2 page ownership remains intact.

Category must still own:

- [ ] `StorefrontPageShell` placement;
- [ ] breadcrumb slot placement;
- [ ] category title;
- [ ] category description/meta-description fallback;
- [ ] result count text;
- [ ] Back to Home CTA;
- [ ] filter placement;
- [ ] product grid placement;
- [ ] pagination item construction;
- [ ] SEO content placement.

Search must still own:

- [ ] page section shell;
- [ ] search title;
- [ ] scope text;
- [ ] filter placement;
- [ ] short-term validation;
- [ ] result heading;
- [ ] result count;
- [ ] product grid placement;
- [ ] pagination item construction.

Product/content must still own:

- [ ] page shell placement;
- [ ] breadcrumb slot placement;
- [ ] page-specific content around breadcrumb.

Exit criteria:

- [ ] Phase 3.3 did not turn reusable components into page shells;
- [ ] V2 still controls page composition.

## Phase 3.3.23 - Documentation And QA Checklist

Update docs:

- [ ] `BlazorShop.PresentationV2/COMPONENT-MODES.md`
- [ ] `docs/architecture/05-project-and-folder-guide.md`
- [ ] `docs/architecture/10-v2-contract-ownership.md`
- [ ] `docs/refactor-control-Commerce-storefront/QA-StorefrontV2.todo.md`

Document:

- [ ] `StorefrontPagination` lives in `Components.Primitives/Navigation`.
- [ ] `StorefrontCatalogFilterPanel` lives in `Components.Ssr/Catalog`.
- [ ] `StorefrontBreadcrumb` lives in `Components.Ssr/Navigation`.
- [ ] Pagination primitive consumes prepared links only.
- [ ] Filter preserves existing GET query contract.
- [ ] Breadcrumb consumes Presentation breadcrumb item for now.
- [ ] V2 owns final classes/copy and route-specific URL generation.

QA checklist additions:

- [ ] Category filter query preservation.
- [ ] Search filter query preservation.
- [ ] Category pagination query preservation.
- [ ] Search pagination query preservation.
- [ ] Product breadcrumb rendering.
- [ ] Content breadcrumb rendering.
- [ ] No direct Commerce browser transport after extraction.

Exit criteria:

- [ ] docs align with implemented graph;
- [ ] QA checklist can be executed by another agent.

## Phase 3.3.24 - Scope Drift Audit

Expected changed areas:

- [ ] `BlazorShop.Storefront.Components/Contracts/Navigation`
- [ ] `BlazorShop.Storefront.Components.Primitives/Navigation`
- [ ] `BlazorShop.Storefront.Components.Ssr/Catalog`
- [ ] `BlazorShop.Storefront.Components.Ssr/Navigation`
- [ ] `BlazorShop.Storefront.V2/Pages/Hybrid/Catalog`
- [ ] `BlazorShop.Storefront.V2/Pages/Ssr/Content`
- [ ] `BlazorShop.Storefront.V2/Pages/Product`
- [ ] small V2 visual config/helper files
- [ ] tests under `BlazorShop.Tests.V2/PresentationV2`
- [ ] relevant architecture docs and QA checklist

Unexpected changed areas:

- [ ] Product purchase panel;
- [ ] cart;
- [ ] checkout;
- [ ] account;
- [ ] consent;
- [ ] Runtime;
- [ ] Client;
- [ ] Browser;
- [ ] V2.WASM unless conditional import/build requires it;
- [ ] Commerce Node;
- [ ] Control Plane;
- [ ] Application;
- [ ] Domain;
- [ ] Infrastructure;
- [ ] database migrations;
- [ ] StorefrontBuilder;
- [ ] Starter;
- [ ] generated storefront artifacts.

Exit criteria:

- [ ] no unrelated work entered the phase;
- [ ] any unexpected change has a written reason and is approved before commit.

## Phase 3.3.25 - Closure Review

Answer these before closing:

- [ ] Did pagination become one reusable primitive?
- [ ] Does pagination consume prepared `Href` values only?
- [ ] Does Category still generate Category URLs outside the primitive?
- [ ] Does Search still generate Search URLs outside the primitive?
- [ ] Did Category/Search preserve filter query names?
- [ ] Did Catalog filter move to SSR without Browser/runtime coupling?
- [ ] Did Breadcrumb move to SSR without unnecessary contract migration?
- [ ] Did all active breadcrumb consumers migrate?
- [ ] Did old V2 `CatalogFilterPanel` get deleted?
- [ ] Did old V2 `BreadcrumbNav` get deleted?
- [ ] Did final visual values remain in V2?
- [ ] Did reusable components avoid literal V2 Tailwind classes?
- [ ] Did reusable components avoid `@rendermode`?
- [ ] Did reusable components avoid API/BFF/HttpClient/JS behavior?
- [ ] Did functional browser QA pass?
- [ ] Did focused build pass?
- [ ] Did focused tests pass?
- [ ] Did full build pass?
- [ ] Did full tests pass or document unrelated failures?
- [ ] Did docs and QA checklist update?

Closure notes must include:

- [ ] changed files;
- [ ] moved/deleted files;
- [ ] final component graph;
- [ ] test command outputs and counts;
- [ ] browser QA evidence;
- [ ] any known unrelated failures;
- [ ] remaining visual debt deferred to final V2 visual sweep.

Exit criteria:

- [ ] Phase 3.3 can be marked closed;
- [ ] Phase 3.4 is not selected until a fresh review is run.

## Definition Of Done

Pagination:

- [ ] `StorefrontPaginationItem` exists in base `Components`.
- [ ] `StorefrontPaginationClasses` exists in base `Components`.
- [ ] `StorefrontPaginationLabels` exists in base `Components`.
- [ ] `StorefrontPagination` exists in `Components.Primitives/Navigation`.
- [ ] Category uses `StorefrontPagination`.
- [ ] Search uses `StorefrontPagination`.
- [ ] route generation stays outside primitive.
- [ ] duplicate pagination loops removed.
- [ ] duplicate `GetPageLinkClass` removed.

Catalog filter:

- [ ] `StorefrontCatalogFilterPanel` exists in `Components.Ssr/Catalog`.
- [ ] old V2 `CatalogFilterPanel.razor` removed.
- [ ] Category uses `StorefrontCatalogFilterPanel`.
- [ ] Search uses `StorefrontCatalogFilterPanel`.
- [ ] query field names unchanged.
- [ ] sort API values unchanged.
- [ ] category option contract reused.
- [ ] V2 owns final classes/copy/icon.

Breadcrumb:

- [ ] `StorefrontBreadcrumb` exists in `Components.Ssr/Navigation`.
- [ ] old V2 `BreadcrumbNav.razor` removed.
- [ ] Category breadcrumb migrated.
- [ ] Product breadcrumb migrated.
- [ ] Content breadcrumb migrated.
- [ ] existing Presentation breadcrumb item reused.
- [ ] no unnecessary contract migration.

Architecture:

- [ ] `Components.Primitives` still references only `Components`.
- [ ] `Components.Ssr` remains Browser-free.
- [ ] no reusable component owns `@rendermode`.
- [ ] no reusable component constructs Storefront routes.
- [ ] no reusable component calls API/BFF/backend.
- [ ] no direct Commerce browser request added.

Visual ownership:

- [ ] final V2 classes remain in V2.
- [ ] final host copy remains in V2.
- [ ] reusable packages use class/label contracts.
- [ ] no broad design system introduced.

QA:

- [ ] Category filtering works in browser.
- [ ] Search filtering works in browser.
- [ ] Category pagination works in browser.
- [ ] Search pagination works in browser.
- [ ] query state is preserved across pagination.
- [ ] breadcrumbs render on Category/Product/Content.
- [ ] no browser console errors.
- [ ] no page errors.

Scope:

- [ ] no ProductPurchasePanel extraction.
- [ ] no Header/Footer/Hero extraction.
- [ ] no cart/checkout/account changes.
- [ ] no backend changes.
- [ ] no Builder/Starter changes.
- [ ] no visual polish work.

## Suggested Commit Breakdown

Use small commits if implementing manually:

```text
refactor(storefront): add pagination render contracts
refactor(storefront): extract storefront pagination primitive
refactor(storefront): adopt shared pagination in catalog pages
refactor(storefront): extract catalog filter panel to ssr components
refactor(storefront): adopt shared catalog filter in v2 pages
refactor(storefront): extract storefront breadcrumb to ssr components
refactor(storefront): migrate breadcrumb consumers
test(storefront): cover catalog navigation component boundaries
docs(storefront): document phase 3.3 catalog navigation extraction
```

## GSTACK REVIEW REPORT

CEO review:

- Scope is intentionally small and matches the user goal of maintainable V2/V2.WASM component extraction.
- The plan avoids broad design-system or route-registry work.
- The strongest product value is reducing duplicated pagination and moving reusable semantic catalog controls out of V2 while preserving behavior.
- Deferred work is explicit: product grid, header/footer, cart/checkout/account, and visual sweep.

Design review:

- The plan does not perform visual redesign.
- It preserves current Category/Search/Product/Content appearance by keeping final classes/copy in V2.
- Visual neutrality guardrails are required because the source components currently contain Tailwind classes and visible English copy.
- Browser QA checks functional preservation rather than pixel-perfect redesign.

Engineering review:

- `StorefrontPagination` belongs in `Components.Primitives` only if it receives prepared `Href` values and never references Presentation.
- `StorefrontCatalogFilterPanel` belongs in `Components.Ssr` because it uses `ProductCatalogSortBy` from Presentation.
- `StorefrontBreadcrumb` belongs in `Components.Ssr` because it currently consumes `StorefrontBreadcrumbItem` from Presentation.
- Existing tests must be updated because they currently lock old paths and component names.
- Boundary tests must prevent route generation, render modes, API calls, JS, Browser, Runtime, and Client dependencies in reusable packages.

DX review:

- The implementation path is phaseable and agent-friendly because each phase has explicit files, checks, and exit criteria.
- The plan keeps generated/starter/headless direction clear by not making V2 route/query logic part of reusable primitives.
- The cleanup audit prevents future agents from accidentally reusing old `CatalogFilterPanel` or `BreadcrumbNav`.
- Browser QA is concrete enough to validate production-facing behavior, not just smoke.

Autonomous decision audit:

| # | Decision | Classification | Principle | Rationale | Rejected |
|---|---|---|---|---|---|
| 1 | Put pagination in `Components.Primitives` | Auto-decided | Boundary clarity | Pagination can be browser-safe if links are prepared by host | SSR pagination with Presentation dependency |
| 2 | Put filter in `Components.Ssr` | Auto-decided | Fit current graph | Filter uses `ProductCatalogSortBy` from Presentation | Forcing sort contract down into base Components |
| 3 | Put breadcrumb in `Components.Ssr` | Auto-decided | Minimal migration | Breadcrumb item is currently Presentation-owned | Moving breadcrumb contract before real browser-safe need |
| 4 | Keep final classes/copy in V2 | Auto-decided | Visual ownership | Reusable packages must remain neutral | Shared component owning V2 Tailwind/copy |
| 5 | Preserve query input names exactly | Auto-decided | Behavioral compatibility | Existing routes/tests rely on current GET query contract | Renaming fields during extraction |
| 6 | Defer visual polish | Auto-decided | Scope control | Extraction should preserve behavior first | Mixing refactor and redesign |

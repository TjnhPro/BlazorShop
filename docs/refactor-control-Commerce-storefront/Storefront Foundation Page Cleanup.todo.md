# Storefront Foundation Page Cleanup

## Goal

Remove the incorrectly mandatory `DealsPage` and `NewReleasesPage` foundation pages from the Storefront Presentation contract, V2 host registration, Starter contract, shell links, route metadata, navigation targets, tests, and QA docs.

The end state should make the foundation page inventory easy to understand:

- Public pages:
  - `HomePage`
  - `CategoryPage`
  - `ProductPage`
  - `SearchPage`
  - `ContentPage`
- Commerce workflow pages:
  - `CartPage`
  - `CheckoutPage`
  - `PaymentResultPage`
- Customer pages:
  - `AuthPage`
  - `AccountPage`
- State views:
  - `MaintenanceState`
  - `NotFoundState`
  - `ServiceUnavailableState`
  - `ErrorState`

`Deals` and `New Releases` remain valid ecommerce concepts for later feature placement, but they must not be route-owned foundation pages. Future implementations should render them as componentized product collections or menu/content placements on Home/Product/Category/Content pages, not as mandatory platform routes.

## Scope

In scope:

- `BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation`
- `BlazorShop.PresentationV2/BlazorShop.Storefront.V2`
- `BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM`
- `BlazorShop.PresentationV2/BlazorShop.Storefront.Starter`
- `BlazorShop.PresentationV2/BlazorShop.Storefront.Starter.WASM` only if tests or route metadata require confirmation.
- `BlazorShop.PresentationV2/BlazorShop.Storefront.Components` contracts that still encode a dedicated deals page placement.
- `BlazorShop.Application` and `BlazorShop.Infrastructure` navigation system-target rules when they point to removed storefront routes.
- `BlazorShop.Tests.V2` guardrails and route smoke tests.
- StorefrontBuilder / visual reverse engineering docs only where they list `/todays-deals` or `/new-releases` as required Starter/generated routes.
- `docs/refactor-control-Commerce-storefront/QA-StorefrontV2.todo.md`
- `docs/refactor-control-Commerce-storefront/QA-StorefrontStarter.todo.md` if Starter route proof expectations change.

Out of scope:

- Replacing `DealsPage` with a new `ProductCollectionPage`.
- Adding new deals/new-release components in this phase.
- Adding compatibility redirects from `/todays-deals` or `/new-releases`.
- Keeping ghost route aliases for old links.
- Changing Commerce Node catalog search/product-list API behavior.
- Changing pricing, discount, product publication, inventory, checkout, payment, or order behavior.
- Changing external SEO redirect storage semantics except test fixture paths that only use the deleted URLs as examples.
- Redesigning V2 header/footer/home/product/cart visuals beyond replacing deleted links with valid destinations.

## Codebase Evidence

- `StorefrontFoundationViewSet` currently requires `DealsPage` and `NewReleasesPage`.
- `StorefrontFoundationViewOptionsValidator` maps those slots to `StorefrontDealsPageContext` and `StorefrontNewReleasesPageContext`.
- Storefront Presentation owns route pages:
  - `Pages/Hybrid/Catalog/TodaysDealsRoutePage.razor`
  - `Pages/Hybrid/Catalog/NewReleasesRoutePage.razor`
- Storefront Presentation owns page services:
  - `StorefrontDealsPageService`
  - `StorefrontNewReleasesPageService`
- Storefront Presentation owns route constants and sitemap entries for:
  - `/todays-deals`
  - `/new-releases`
- V2 registers both visual pages in `V2FoundationViewRegistration`.
- Starter registers both visual pages in `StarterFoundationViewRegistration`.
- Starter generation metadata lists both routes in `starter-generation.contract.yaml`.
- V2 header/footer/hero/home/product/cart/checkout surfaces link to `Context.Links.NewReleases` or `Context.Links.TodaysDeals`.
- V2.WASM cart has default URL parameters for `/new-releases` and `/todays-deals`.
- `DealsPlacement.DedicatedPage` still implies a dedicated deals page.
- `StoreNavigationSystemTargets` and `StoreNavigationInternalRoutes` still expose `new_releases` and `todays_deals`.
- Existing tests explicitly assert both routes, both foundation slots, and both Starter pages.

## Architecture Decision

Delete the dedicated pages now. Do not keep compatibility aliases because the project is still in development mode and these routes are not a stable production contract.

Canonical replacement destinations:

- Header/footer primary shopping links should prefer:
  - Home: `/`
  - Search: `/search`
  - Current category/menu-driven links when available.
- Empty cart and checkout fallback links should prefer:
  - Home: `/`
  - Search: `/search`
- Product detail fallback CTAs should prefer:
  - Category/search links if context provides them.
  - Home/search when category context is unavailable.

Do not introduce a new product collection page abstraction until there is a real neutral capability model for reusable product collections, placement rules, cache behavior, SEO/indexing, and menu ownership.

## Phase 0 - Baseline And Ownership Snapshot

- [x] Confirm the worktree state before edits:

```powershell
git status --short
```

- [x] Read and note current foundation slots:
  - [x] `BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Views/Foundation/StorefrontFoundationViewSet.cs`
  - [x] `BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Views/Foundation/StorefrontFoundationViewOptionsValidator.cs`
  - [x] `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/V2FoundationViewRegistration.cs`
  - [x] `BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/StarterFoundationViewRegistration.cs`
- [x] Read and note current route/page service ownership:
  - [x] `BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Pages/Hybrid/Catalog/TodaysDealsRoutePage.razor`
  - [x] `BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Pages/Hybrid/Catalog/NewReleasesRoutePage.razor`
  - [x] `BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Services/Catalog/StorefrontDealsPageService.cs`
  - [x] `BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Services/Catalog/StorefrontNewReleasesPageService.cs`
  - [x] `BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Services/Catalog/StorefrontCatalogProductsPageContext.cs`
- [x] Read and note current shell/navigation dependencies:
  - [x] `BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Services/StorefrontRoutes.cs`
  - [x] `BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Routing/StorefrontRoutePatterns.cs`
  - [x] `BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Routing/StorefrontRouteNames.cs`
  - [x] `BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Services/Contracts/IStorefrontShellContextService.cs`
  - [x] `BlazorShop.Application/CommerceNode/Navigation/StoreNavigationRules.cs`
  - [x] `BlazorShop.Infrastructure/Data/CommerceNode/Services/StoreNavigationService.cs`
- [x] Run a focused search and save the hit list into the implementation notes:

```powershell
rg -n "DealsPage|NewReleasesPage|todays-deals|new-releases|TodaysDeals|NewReleases|DedicatedPage|StorefrontDealsPage|StorefrontNewReleasesPage" BlazorShop.PresentationV2 BlazorShop.Application BlazorShop.Infrastructure BlazorShop.Tests.V2 docs
```

Definition of done:

- [x] The implementation notes identify every source, test, and docs area that references the deleted routes.
- [x] No code has been changed before the baseline inventory is complete.

Implementation notes:

- 2026-08-08 baseline worktree: `BlazorShop.sln` was already modified outside this task; this cleanup plan file was untracked before Phase 0 commit.
- Foundation contract hits: `StorefrontFoundationViewSet` requires `DealsPage`/`NewReleasesPage`; validator maps them to `StorefrontDealsPageContext`/`StorefrontNewReleasesPageContext`; V2 and Starter register those slots.
- Presentation route/service hits: `TodaysDealsRoutePage.razor`, `NewReleasesRoutePage.razor`, `StorefrontDealsPageService`, `StorefrontNewReleasesPageService`, `StorefrontCatalogProductsPageContext`, `StorefrontRoutes`, `StorefrontRoutePatterns`, `StorefrontRouteNames`, `StorefrontPageKind`, service DI registration, sitemap static pages.
- V2/Starter visual hits: dedicated V2 pages, dedicated Starter pages, V2 header/footer/hero/home/product/cart/checkout links, V2.WASM cart parameters/default URLs, Starter layout link, `starter-generation.contract.yaml` route entries.
- Shared/domain hits: `DealsPlacement.DedicatedPage`; `StoreNavigationSystemTargets`, `StoreNavigationInternalRoutes`, and `StoreNavigationService.StaticRouteMap` expose deleted route targets.
- Test/doc hits: Storefront foundation tests, page composition tests, branding/headless tests, starter foundation tests, navigation tests, SEO redirect fixture tests, StorefrontBuilder/visual reverse engineering active docs, QA Storefront V2/Starter docs. Completed historical plans also contain expected historical references and should not be blindly rewritten unless they are active guidance.

## Phase 1 - Remove Foundation Slots And Required Context Mapping

- [x] Update `StorefrontFoundationViewSet`.
  - [x] Remove required property `DealsPage`.
  - [x] Remove required property `NewReleasesPage`.
  - [x] Remove both entries from `GetRequiredSlots()`.
  - [x] Keep all remaining foundation page/state slots unchanged.
- [x] Update `StorefrontFoundationViewOptionsValidator`.
  - [x] Remove the `DealsPage` context mapping.
  - [x] Remove the `NewReleasesPage` context mapping.
  - [x] Keep validation behavior unchanged for remaining slots.
- [x] Update any helper/test factory that constructs `StorefrontFoundationViewSet`.
  - [x] Remove assignments for `DealsPage`.
  - [x] Remove assignments for `NewReleasesPage`.
  - [x] Do not replace them with another slot.

Tests to update:

- [x] `StorefrontPresentationFoundationBoundaryTests`
  - [x] Remove expectations that `DealsPage` is required.
  - [x] Remove expectations that `NewReleasesPage` is required.
  - [x] Keep coverage proving all real foundation slots are still required.
  - [x] Add a negative assertion that `StorefrontFoundationViewSet` no longer contains `DealsPage` or `NewReleasesPage`.

Definition of done:

- [x] Foundation view set no longer makes deals/new releases mandatory.
- [x] Existing hosts can only register real foundation slots.
- [x] Tests fail if the deleted slots are reintroduced.

Implementation notes:

- 2026-08-08: removed the deleted slots from the foundation contract and validator. V2/Starter registration assignments were also removed in Phase 1 as direct compile fallout of the reduced `StorefrontFoundationViewSet`; the dedicated visual pages and route metadata remain for the later ordered cleanup phases.

## Phase 2 - Remove Presentation Route Pages, Services, And Page Kinds

- [x] Delete Presentation route pages:
  - [x] `Pages/Hybrid/Catalog/TodaysDealsRoutePage.razor`
  - [x] `Pages/Hybrid/Catalog/NewReleasesRoutePage.razor`
- [x] Delete Presentation page services:
  - [x] `Services/Catalog/StorefrontDealsPageService.cs`
  - [x] `Services/Catalog/StorefrontNewReleasesPageService.cs`
- [x] Update `StorefrontPresentationServiceCollectionExtensions`.
  - [x] Remove `AddScoped<StorefrontDealsPageService>()`.
  - [x] Remove `AddScoped<StorefrontNewReleasesPageService>()`.
- [x] Update `StorefrontCatalogProductsPageContext.cs`.
  - [x] Remove `StorefrontDealsPageContext`.
  - [x] Remove `StorefrontNewReleasesPageContext`.
  - [x] Remove `StorefrontCatalogProductsPageContext` if no other source uses it after the cleanup.
  - [x] Keep any generic catalog/search/category contexts that are still used.
- [x] Update `StorefrontPageKind`.
  - [x] Remove `Deals`.
  - [x] Remove `NewReleases`.
  - [x] Keep enum values for real route/state kinds unchanged where possible.
- [x] Confirm `StorefrontPageResultMapper` does not require any changes beyond compile fixes.

Tests to update:

- [x] `StorefrontPageCompositionGuardrailTests`
  - [x] Remove expectations for `TodaysDealsRoutePage.razor`.
  - [x] Remove expectations for `NewReleasesRoutePage.razor`.
  - [x] Add assertions that Presentation route pages do not declare `/todays-deals` or `/new-releases`.
- [x] `StorefrontBrandingMarkupTests`
  - [x] Remove `DealsAndNewReleases_ComposePortableFeatureComponents` or rewrite it as a Home/product-section component composition check if still relevant.
- [x] `StorefrontComponentsHeadlessPresentationRefactorTests`
  - [x] Remove tests that require dedicated deals/new-release pages.
  - [x] Keep tests that guard reusable component/headless boundaries if they are still valid.

Definition of done:

- [x] No Presentation `@page` route exists for `/todays-deals`.
- [x] No Presentation `@page` route exists for `/new-releases`.
- [x] No Presentation service or page state depends on `StorefrontDealsPageContext` or `StorefrontNewReleasesPageContext`.

Implementation notes:

- 2026-08-08: removed the Presentation-owned ghost route pages, page services, deleted collection contexts, and page-kind enum values. `StorefrontPageResultMapper` did not require changes after the enum cleanup. V2/Starter visual pages still reference the removed contexts until Phase 3/4 delete those host templates.

## Phase 3 - Remove V2 Dedicated Visual Pages And Replace V2 Links

- [x] Update `V2FoundationViewRegistration`.
  - [x] Remove `DealsPage = typeof(TodaysDeals)`.
  - [x] Remove `NewReleasesPage = typeof(NewReleases)`.
- [x] Delete V2 dedicated visual pages:
  - [x] `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/Hybrid/Catalog/TodaysDeals.razor`
  - [x] `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/Hybrid/Catalog/NewReleases.razor`
- [x] Replace V2 links that point to deleted shell links.
  - [x] `Components/Layout/StorefrontHeader.razor`
    - [x] Remove hardcoded navigation entries for new releases/deals.
    - [x] Prefer menu-driven entries or Home/Search links.
  - [x] `Components/Layout/StorefrontFooter.razor`
    - [x] Remove footer links to deleted routes.
    - [x] Replace with Home/Search/Content links that actually exist.
  - [x] `Components/Public/HeroBanner.razor`
    - [x] Replace `Links.NewReleases` and `Links.TodaysDeals` CTAs with valid Home/Search/category links.
  - [x] `Pages/Hybrid/Catalog/Home.razor`
    - [x] Remove page CTA to `/new-releases`.
    - [x] Keep any inline deals section only if it does not depend on `DealsPlacement.DedicatedPage` or deleted routes.
  - [x] `Pages/Product/V2ProductPageView.razor`
    - [x] Replace fallback links to new releases/deals with valid Home/Search/category links.
  - [x] `Pages/Hybrid/Commerce/CheckoutPage.razor`
    - [x] Replace empty/invalid checkout CTA with Home/Search.
  - [x] `Pages/Hybrid/Commerce/CartPage.razor`
    - [x] Remove parameters that pass deleted route URLs into the WASM cart component.
    - [x] Pass Home/Search URLs or a single `ContinueShoppingUrl` if the context is simplified in Phase 5.
- [ ] Confirm no V2 visual source contains the deleted route strings:

```powershell
rg -n "todays-deals|new-releases|TodaysDeals|NewReleases|DealsPage|NewReleasesPage" BlazorShop.PresentationV2/BlazorShop.Storefront.V2
```

Definition of done:

- [x] V2 owns no dedicated visual page for deals/new releases.
- [x] V2 UI renders no links to deleted routes.
- [x] V2 still has valid shopping CTAs for empty states and hero/product fallbacks.

Implementation notes:

- 2026-08-08: V2 dedicated visual pages were deleted. Header/footer/hero/home/product/checkout now point fallback CTAs to existing Home/Search/CustomerService links. Cart still uses the old WASM parameter names until Phase 5, but V2 now passes Search/Home URLs instead of deleted route URLs.

## Phase 4 - Remove Starter Pages And Generation Route Contract

- [x] Update `StarterFoundationViewRegistration`.
  - [x] Remove `DealsPage = typeof(DealsPage)`.
  - [x] Remove `NewReleasesPage = typeof(NewReleasesPage)`.
- [x] Delete Starter visual pages:
  - [x] `BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/Pages/Hybrid/Commerce/DealsPage.razor`
  - [x] `BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/Pages/Hybrid/Catalog/NewReleasesPage.razor`
- [x] Update Starter layout/navigation source.
  - [x] Remove `Context.Links.TodaysDeals` and `Context.Links.NewReleases` usage.
  - [x] Replace with valid Home/Search/Content links.
- [x] Update `starter-generation.contract.yaml`.
  - [x] Remove route entry for `/todays-deals`.
  - [x] Remove route entry for `/new-releases`.
  - [x] Ensure the remaining route list still includes the real route truth:
    - [x] `/`
    - [x] `/pages/{Slug}`
    - [x] auth/recovery routes
    - [x] `/maintenance`
    - [x] `/{*Path:nonfile}`
    - [x] `/category/{Slug}`
    - [x] `/product/{Slug}`
    - [x] `/search`
    - [x] `/cart`
    - [x] `/my-cart`
    - [x] `/checkout`
    - [x] `/payment/result`
    - [x] `/payment-success`
    - [x] `/payment-cancel`
    - [x] `/account`
- [x] Confirm generated visual files are still not route owners and do not use `@page`.

Tests to update:

- [x] `StorefrontStarterFoundationBoundaryTests`
  - [x] Remove expected Starter page files for deals/new releases.
  - [x] Remove expected Starter route entries `/todays-deals` and `/new-releases`.
  - [x] Add assertions that Starter registration does not mention `DealsPage` or `NewReleasesPage`.
  - [x] Add assertions that `starter-generation.contract.yaml` does not list deleted routes.

Definition of done:

- [x] Starter is still a valid second consumer of the reduced foundation contract.
- [x] StorefrontBuilder will not generate storefront route expectations for deleted pages.
- [x] Generated storefronts cannot inherit ghost deals/new-release route metadata from Starter.

Implementation notes:

- 2026-08-08: removed Starter's dedicated collection page templates, deleted the two route metadata entries from `starter-generation.contract.yaml`, and removed the Starter layout link to the deleted deals route. Starter registration cleanup was completed as Phase 1 compile fallout and is guarded here.

## Phase 5 - Simplify Shell Link And Cart Context Contracts

- [x] Update `StorefrontRoutes`.
  - [x] Remove `NewReleases`.
  - [x] Remove `TodaysDeals`.
  - [x] Remove both from `SitemapStaticPages`.
  - [x] Keep Home/Search/Cart/Checkout/Account/Content/System routes intact.
- [x] Update `StorefrontRoutePatterns`.
  - [x] Remove `Deals`.
  - [x] Remove `NewReleases`.
- [x] Update `StorefrontRouteNames`.
  - [x] Remove `Deals`.
  - [x] Remove `NewReleases`.
- [x] Update `IStorefrontShellContextService` / `StorefrontLinkContext`.
  - [x] Remove `NewReleases`.
  - [x] Remove `TodaysDeals`.
  - [x] Add no replacement unless there is already a neutral, existing route link.
  - [x] If a replacement is needed for CTA ergonomics, prefer existing links such as `Home`, `Search`, `Cart`, `Checkout`, `Account`, or content links.
- [x] Update `StorefrontCartPageContext`.
  - [x] Remove `NewReleasesUrl`.
  - [x] Remove `TodaysDealsUrl`.
  - [x] Add `ContinueShoppingUrl` only if cart/empty state still needs a route value.
  - [x] Prefer `StorefrontRoutes.Home` or `StorefrontRoutes.Search` for `ContinueShoppingUrl`.
- [x] Update `StorefrontCartPageService`.
  - [x] Stop reading deleted route constants.
  - [x] Populate the simplified cart context.
- [x] Update V2.WASM `StorefrontCartView`.
  - [x] Remove `NewReleasesUrl`.
  - [x] Remove `TodaysDealsUrl`.
  - [x] Replace two empty-state CTAs with one or two valid same-origin links.
  - [x] Do not hardcode `/new-releases` or `/todays-deals`.
- [ ] Confirm no shell/cart/source contract keeps deleted link names:

```powershell
rg -n "NewReleases|TodaysDeals|new-releases|todays-deals" BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM
```

Tests to update:

- [x] Host smoke tests that post currency preference using `StorefrontRoutes.NewReleases` should use an existing route such as `/search` or `/cart`.
- [x] Cart/WASM runtime tests should expect the new empty-state link contract.
- [x] Sitemap tests should not expect deleted static URLs.

Definition of done:

- [x] Shell link context contains only real shared storefront links.
- [x] Cart context no longer leaks deleted collection routes.
- [x] Sitemap no longer advertises deleted pages.

Implementation notes:

- 2026-08-08: removed deleted route constants/patterns/names and shell links. Cart page context now exposes `ContinueShoppingUrl` only; V2.WASM cart parameter rename was completed in Phase 3 as a required V2 ghost-token cleanup, and Phase 5 wired the Presentation context/service to it.

## Phase 6 - Remove Or Reclassify Navigation System Targets

- [ ] Update `StoreNavigationRules`.
  - [ ] Remove `StoreNavigationSystemTargets.NewReleases`.
  - [ ] Remove `StoreNavigationSystemTargets.TodaysDeals`.
  - [ ] Remove corresponding entries from `All`.
  - [ ] Remove corresponding `StoreNavigationInternalRoutes` entries if they only map these deleted routes.
- [ ] Update `StoreNavigationService`.
  - [ ] Remove `StaticRouteMap` entries for `new_releases` and `todays_deals`.
  - [ ] Ensure invalid/deleted system targets are rejected clearly instead of resolving to stale URLs.
- [ ] Check Development seeding or fixture setup for persisted menu items targeting deleted system names.
  - [ ] If seed data creates either target, update it to Home/Search/category.
  - [ ] If no seed data creates them, note that no migration is needed for dev-mode cleanup.
- [ ] Decide whether database cleanup is needed.
  - [ ] For current dev-mode local DB, manual cleanup or fixture re-seed is acceptable.
  - [ ] Do not add a production migration solely for deleted dev-only route aliases unless existing production data must be preserved.
- [ ] Keep product/category/page/external URL menu item behavior unchanged.

Tests to update:

- [ ] `StoreNavigationRulesTests`
  - [ ] Remove assertions that deleted system targets exist.
  - [ ] Add assertions that deleted system targets are not in `All`.
- [ ] `StoreNavigationServiceTests`
  - [ ] Remove fixture menu item resolving `TodaysDeals`.
  - [ ] Replace with an existing target such as Home/Search or a real category/page target.
  - [ ] Add negative coverage for unsupported/deleted system target if service behavior supports it.

Definition of done:

- [ ] Control/admin navigation cannot create new menu items that resolve to deleted routes.
- [ ] Runtime navigation output contains no `/todays-deals` or `/new-releases`.
- [ ] Existing product/category/page/external menu behavior is not broken.

## Phase 7 - Clean Component Contract Placement Leak

- [ ] Update `BlazorShop.Storefront.Components/Contracts/Deals/DealsPlacement.cs`.
  - [ ] Remove `DedicatedPage`.
  - [ ] Keep only placements that are still valid after cleanup, for example `Home` and `ProductDetailFooter`.
  - [ ] Add no new placement unless V2 currently needs it and it maps to a real component placement.
- [ ] Update V2 `StorefrontDealsSection`.
  - [ ] Change the default placement away from `DedicatedPage`.
  - [ ] Remove layout/style branching that assumes a dedicated page.
  - [ ] Keep inline Home/Product placement behavior if currently used.
- [ ] Update tests/docs that still use `DealsPlacement.DedicatedPage`.
  - [ ] Replace with `Home` or `ProductDetailFooter` only when the component is still rendered in those locations.
  - [ ] Delete tests that only existed to prove dedicated page composition.
- [ ] Confirm `BlazorShop.Storefront.Components` still does not own visual wrappers or V2 route defaults.

Definition of done:

- [ ] Components contracts no longer imply a deleted dedicated deals page.
- [ ] V2 deals section, if retained, is clearly an inline/placement component.
- [ ] No shared component contract references `/todays-deals` or a dedicated deals route.

## Phase 8 - Reserved Slug, SEO Redirect, And Generic Test Fixture Audit

- [ ] Review `StoreSeoSlugPolicyService`.
  - [ ] Decide whether `new-releases` and `todays-deals` should remain reserved path slugs.
  - [ ] Recommended for this phase: remove them from reserved paths if the routes are deleted and no future route is approved.
  - [ ] If kept reserved, document the reason explicitly as future compatibility, not active route ownership.
- [ ] Review SEO redirect tests that use `/todays-deals` as sample redirect target.
  - [ ] If the test is generic redirect behavior, replace the sample path with an existing neutral route such as `/search` or `/pages/some-page`.
  - [ ] Do not remove SEO redirect capability.
- [ ] Review `StorefrontScopedSeoControllerTests`.
  - [ ] Replace deleted route examples with neutral existing route examples.
- [ ] Confirm robots/sitemap/indexing behavior.
  - [ ] Sitemap must not include deleted pages.
  - [ ] Robots/indexing rules must not mention deleted pages.
  - [ ] Missing deleted route should follow normal not-found behavior.

Definition of done:

- [ ] SEO redirect tests no longer depend on deleted route names as fixture data.
- [ ] Slug policy accurately reflects active reserved paths.
- [ ] SEO/discovery behavior remains intact for real routes.

## Phase 9 - Documentation And QA Checklist Update

- [ ] Update `docs/architecture/05-project-and-folder-guide.md` only if it lists the deleted pages as foundation route examples.
- [ ] Update `docs/architecture/11-storefront-builder.md`.
  - [ ] Remove `/todays-deals` and `/new-releases` from required Starter/generated route examples.
  - [ ] Ensure it still states Presentation owns route truth and visual hosts register view slots only.
- [ ] Update `docs/agents/storefront-builder.md` if it lists these routes as required route proof.
- [ ] Update `docs/visual-reverse-engineering-skill/*` references that freeze `/todays-deals` or `/new-releases` as current expected Starter/generated routes.
  - [ ] Prefer adding a note in the new phase evidence rather than rewriting completed historical plans, unless a current source-of-truth doc is wrong.
- [ ] Update `docs/refactor-control-Commerce-storefront/QA-StorefrontV2.todo.md`.
  - [ ] Add a new checklist entry for Foundation Page Cleanup.
  - [ ] Record that `/todays-deals` and `/new-releases` are intentionally removed.
  - [ ] Require browser QA to verify V2 has no visible links to deleted routes.
  - [ ] Require browser QA to verify deleted routes return the standard not-found route behavior, not a broken exception.
- [ ] Update `docs/refactor-control-Commerce-storefront/QA-StorefrontStarter.todo.md` if Starter route proof changes.
  - [ ] Record that Starter no longer declares dedicated deals/new-release routes.
  - [ ] Record the remaining required route list.

Definition of done:

- [ ] Current architecture docs no longer describe deleted collection pages as required routes.
- [ ] QA checklists tell future agents how to verify the cleanup.
- [ ] Historical docs are not rewritten unless they are active guidance.

## Phase 10 - Static Ghost Audit

- [ ] Run repo-wide source scan:

```powershell
rg -n "DealsPage|NewReleasesPage|StorefrontDealsPage|StorefrontNewReleasesPage|StorefrontDealsPageContext|StorefrontNewReleasesPageContext|DealsPlacement\\.DedicatedPage|TodaysDeals|NewReleases|todays-deals|new-releases" BlazorShop.PresentationV2 BlazorShop.Application BlazorShop.Infrastructure BlazorShop.Tests.V2 docs
```

- [ ] Classify every remaining hit:
  - [ ] Valid historical completed plan entry.
  - [ ] Valid unrelated SEO redirect fixture only if path was intentionally kept as arbitrary test data.
  - [ ] Invalid active source reference.
  - [ ] Invalid active test reference.
  - [ ] Invalid active docs/reference expectation.
- [ ] Remove all invalid active source/test/docs references.
- [ ] Add explicit guardrail tests:
  - [ ] V2 source must not contain `StorefrontApiClient`-style manual transport is already covered; add a route cleanup guard specific to `/todays-deals` and `/new-releases`.
  - [ ] Storefront Presentation source must not declare deleted routes.
  - [ ] Starter `starter-generation.contract.yaml` must not list deleted routes.
  - [ ] StorefrontBuilder active route inventory must not list deleted routes.
  - [ ] V2 visible shell components must not link to deleted routes.
- [ ] Ensure tests do not simply scan all historical docs and fail on completed plan history unless those docs are current guidance.

Definition of done:

- [ ] Active code has zero invalid hits for deleted page slots/routes.
- [ ] Active tests prevent accidental reintroduction.
- [ ] Remaining historical references are documented as historical only or left in completed plans that are not current source of truth.

## Phase 11 - Focused Build And Test Gate

Run focused build checks:

```powershell
dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/BlazorShop.Storefront.Presentation.csproj --no-restore
dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.V2/BlazorShop.Storefront.V2.csproj --no-restore
dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM/BlazorShop.Storefront.V2.WASM.csproj --no-restore
dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/BlazorShop.Storefront.Starter.csproj --no-restore
```

Run focused test slices:

```powershell
dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~StorefrontPresentationFoundationBoundaryTests|FullyQualifiedName~StorefrontStarterFoundationBoundaryTests|FullyQualifiedName~StorefrontPageCompositionGuardrailTests"
```

```powershell
dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~StorefrontBrandingMarkupTests|FullyQualifiedName~StorefrontComponentsHeadlessPresentationRefactorTests|FullyQualifiedName~StorefrontV2HostSmokeTests"
```

```powershell
dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~StoreNavigationRulesTests|FullyQualifiedName~StoreNavigationServiceTests|FullyQualifiedName~SeoRedirectResolutionServiceTests|FullyQualifiedName~StorefrontScopedSeoControllerTests"
```

Run broader Storefront boundary tests if the focused slices pass:

```powershell
dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~Storefront"
```

Definition of done:

- [ ] Presentation build passes.
- [ ] V2 build passes.
- [ ] V2.WASM build passes.
- [ ] Starter build passes.
- [ ] Focused foundation/page/navigation/SEO tests pass.
- [ ] Broader Storefront test slice passes or any unrelated existing skip/failure is documented precisely.

## Phase 12 - Browser QA And StorefrontBuilder Proof

- [ ] Start local V2 stack:

```powershell
.\scripts\run-v2-local.ps1 -StopExisting -NoOpenBrowser
```

- [ ] Run Playwright browser QA against Storefront V2.
  - [ ] Home page loads.
  - [ ] Category page loads.
  - [ ] Search page loads.
  - [ ] Product detail loads.
  - [ ] Cart page empty state loads and links only to valid destinations.
  - [ ] Checkout empty/invalid state loads and links only to valid destinations.
  - [ ] Header has no `/new-releases` or `/todays-deals` links.
  - [ ] Footer has no `/new-releases` or `/todays-deals` links.
  - [ ] Hero/home/product CTAs do not point to deleted routes.
  - [ ] Visiting `/new-releases` returns standard not-found behavior.
  - [ ] Visiting `/todays-deals` returns standard not-found behavior.
  - [ ] Sitemap does not include `/new-releases` or `/todays-deals`.
  - [ ] Browser network has zero direct Commerce Node, Control Plane, Commerce Admin, or `api/internal/*` calls.
  - [ ] Browser console has no unexpected JS/.NET/WASM errors.
- [ ] Capture evidence under `output/playwright` or `.gstack/qa-reports`.
- [ ] If Starter/Builder contract changed, run at least Structure proof:

```powershell
.\scripts\qa\run-storefront-builder-generated-proof.ps1 -ProofLevel Structure
```

- [ ] If generated functional route inventory changed, run the fast foundation functional proof:

```powershell
.\scripts\qa\run-storefront-builder-generated-proof.ps1 -ProofLevel FoundationFunctionalFast
```

- [ ] If the proof reveals generated route assumptions for deleted pages, update StorefrontBuilder validation/tests in the same phase.

Definition of done:

- [ ] Real browser QA proves no visible V2 link points to deleted routes.
- [ ] Real browser QA proves deleted routes do not throw unexpected errors.
- [ ] Generated storefront proof no longer expects deleted routes.
- [ ] QA evidence paths are recorded in the QA checklist.

## Final Acceptance Checklist

- [ ] `DealsPage` removed from `StorefrontFoundationViewSet`.
- [ ] `NewReleasesPage` removed from `StorefrontFoundationViewSet`.
- [ ] `StorefrontFoundationViewOptionsValidator` has no deals/new-release context mapping.
- [ ] Presentation has no `/todays-deals` route page.
- [ ] Presentation has no `/new-releases` route page.
- [ ] Presentation has no `StorefrontDealsPageService`.
- [ ] Presentation has no `StorefrontNewReleasesPageService`.
- [ ] Presentation has no `StorefrontDealsPageContext`.
- [ ] Presentation has no `StorefrontNewReleasesPageContext`.
- [ ] V2 registration has no deals/new-release foundation slots.
- [ ] V2 has no dedicated deals/new-release visual pages.
- [ ] Starter registration has no deals/new-release foundation slots.
- [ ] Starter has no dedicated deals/new-release visual pages.
- [ ] `starter-generation.contract.yaml` has no `/todays-deals` route.
- [ ] `starter-generation.contract.yaml` has no `/new-releases` route.
- [ ] `StorefrontRoutes` has no deleted route constants or sitemap entries.
- [ ] Shell link context has no `NewReleases` or `TodaysDeals` properties.
- [ ] Cart page context/WASM cart no longer defaults to deleted route URLs.
- [ ] Navigation system targets no longer resolve to deleted routes.
- [ ] `DealsPlacement.DedicatedPage` is removed or replaced with a valid non-route placement.
- [ ] Reserved slug policy is aligned with active routes.
- [ ] SEO redirect tests use neutral active route examples.
- [ ] V2 visible UI has no link to `/todays-deals` or `/new-releases`.
- [ ] Sitemap does not include `/todays-deals` or `/new-releases`.
- [ ] Deleted routes use the normal not-found path.
- [ ] StorefrontBuilder/Starter docs no longer treat deleted routes as required generated pages.
- [ ] Focused builds pass.
- [ ] Focused tests pass.
- [ ] Storefront browser QA passes.
- [ ] StorefrontBuilder Structure proof passes if Starter route metadata changed.

## Decision Audit Trail

| # | Phase | Decision | Classification | Principle | Rationale | Rejected |
|---|-------|----------|----------------|-----------|-----------|----------|
| 1 | Planning | Delete `DealsPage` and `NewReleasesPage` instead of replacing them with `ProductCollectionPage`. | Auto-decided | Keep foundation small and route truth explicit. | These are feature placements, not mandatory foundation pages. A replacement page abstraction would preserve the wrong mental model. | Add generic `ProductCollectionPage` now. |
| 2 | Planning | Include Starter and StorefrontBuilder route contract in cleanup. | Auto-decided | Two-consumer contract integrity. | `StorefrontFoundationViewSet` is consumed by V2 and Starter; removing slots only in V2 would break Starter/generated proof or leave ghost route metadata. | V2-only cleanup. |
| 3 | Planning | Remove navigation system targets for deleted static collection routes. | Auto-decided | Avoid stale admin-selectable routes. | Navigation targets are route contract, not catalog capability. Keeping them would allow menu items to resolve to deleted pages. | Keep `new_releases` and `todays_deals` system targets. |
| 4 | Planning | Replace visible CTAs with existing valid destinations instead of preserving redirects. | Auto-decided | No ghost links in development mode. | The project is still in dev mode; hard cleanup is safer than teaching the UI and tests that deleted pages are valid aliases. | Add compatibility redirects. |
| 5 | Planning | Defer reusable deals/new-release component work. | Auto-decided | Avoid scope inflation. | The current problem is a foundation route contract cleanup. A later feature module can add product collection placements when requirements are clearer. | Build a new deals/new-releases component library in this phase. |

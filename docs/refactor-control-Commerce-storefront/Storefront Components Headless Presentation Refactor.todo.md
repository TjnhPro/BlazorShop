# Storefront Components Headless Presentation Refactor.todo

Status: in progress

Goal: đổi `BlazorShop.Storefront.Components` từ shared visual implementation mang theme/layout V2 thành headless presentation contracts, browser-safe behavior/state, accessibility hooks và event contracts. Visual markup/layout/theme phải thuộc từng storefront: `Storefront.V2`, `Storefront.Starter`, hoặc generated/custom `Storefront.{Name}`.

This plan intentionally covers every current file under `BlazorShop.PresentationV2/BlazorShop.Storefront.Components/Features` so implementation can proceed without missing components.

## Current verified inventory

### Razor feature components

| # | Component | Current role | Main issue | Target |
| --- | --- | --- | --- | --- |
| 1 | `Features/Catalog/ProductSummaryCard.razor` | Product card visual and action surface | Locks V2 card design, badges, image ratio, buttons, colors | Move V2 visual to `Storefront.V2`; keep product summary model/context contract |
| 2 | `Features/Catalog/ProductSummaryGrid.razor` | Product grid composition | Locks responsive grid and empty state | Move V2 grid visual to `Storefront.V2`; keep list/empty contract |
| 3 | `Features/Deals/DealsBlock.razor` | Deals section composition | Locks section layout, CTA, container, product grid composition | Move visual section to `Storefront.V2`; keep optional placement/data contract only if reused |
| 4 | `Features/Product/ProductGallery.razor` | Product gallery visual and selected image behavior | Locks square layout, thumbnail placement, dimensions, object fit | Keep gallery state/selection contract; move visual gallery template to each storefront |
| 5 | `Features/Product/ProductPurchasePanel.razor` | Purchase panel visual plus product selection hooks | Locks form layout/theme and hardcodes preview host conventions | Split selection/quantity/add-to-cart behavior from visual template |
| 6 | `Features/Cart/CartView.razor` | Cart page visual plus browser API behavior | Locks full cart page layout and hardcodes `/api/cart` routes | Split cart state/actions from store-owned cart view |
| 7 | `Features/Checkout/CheckoutShell.razor` | Checkout page shell plus browser API behavior | Locks checkout panel/step layout and hardcodes `/api/checkout` routes | Split checkout state machine/view model from store-owned checkout shell |
| 8 | `Features/Account/AccountNavigation.razor` | Account subnavigation visual | Locks account nav layout/routes | Move visual nav to store; keep route/nav item contract if needed |
| 9 | `Features/Account/AccountProfileEditor.razor` | Profile form visual plus browser API behavior | Locks form layout/theme and endpoint routes | Split profile form state/actions from visual form |
| 10 | `Features/Account/AccountChangePasswordForm.razor` | Password form visual plus browser API behavior | Locks form layout/theme and endpoint route | Split password form state/actions from visual form |
| 11 | `Features/Account/AccountAddressBook.razor` | Address book visual plus browser API behavior | Locks add/edit/card layout and endpoint routes | Split address state/actions from visual address templates |
| 12 | `Features/Account/AccountOrderList.razor` | Order list visual plus browser API behavior | Locks table layout and endpoint route | Split order-list state/actions from visual list/table/card template |
| 13 | `Features/Account/AccountOrderDetail.razor` | Order detail visual plus browser API behavior | Locks detail/totals/address layout and endpoint route | Split order-detail state/actions from visual detail/receipt template |
| 14 | `Features/Account/AccountApp.razor` | Account feature composition shell | Composes all account leaf components and locks account shell layout | Do last after all account leaves have headless contracts |

### Feature model and enum files

| File | Current role | Target |
| --- | --- | --- |
| `Features/Catalog/ProductSummaryItem.cs` | Product card/list render-facing model | Keep, move/rename to `Contracts/Catalog` only if folder cleanup is part of implementation |
| `Features/Deals/DealsPlacement.cs` | Deals placement enum | Move to V2 if only V2 owns deals composition; keep as contract only if multiple hosts use it |
| `Features/Product/ProductGalleryItem.cs` | Gallery item render-facing model | Keep as product gallery contract |
| `Features/Product/ProductPurchasePanelModels.cs` | Purchase panel render/selection model | Keep, split into purchase snapshot and interaction state if needed |

Note: inventory has 4 feature model/enum files under `Features`. Browser support models below are outside `Features` but must be included in interactive phases.

### Browser support artifacts

| File | Current role | Target |
| --- | --- | --- |
| `Browser/StorefrontBrowserCartModels.cs` | Cart local BFF response/request models | Keep browser-safe, but separate from visual cart layout |
| `Browser/StorefrontBrowserCheckoutModels.cs` | Checkout local BFF state/command models | Keep browser-safe, but separate from visual checkout layout |
| `Browser/StorefrontBrowserAccountModels.cs` | Account local BFF state/command models | Keep browser-safe, but separate from visual account layout |
| `Browser/StorefrontFeatureDataMode.cs` | Initial snapshot/browser fetch mode | Keep as behavior primitive |
| `Browser/StorefrontLocalApiClient.cs` | Same-origin BFF client | Keep, but shared feature components must receive route/action descriptors instead of hardcoding `/api/*` |
| `Browser/StorefrontLocalApiResult.cs` | Same-origin local API result | Keep |
| `Browser/IStorefrontAntiforgeryTokenReader.cs` | Antiforgery abstraction | Keep |
| `Browser/StorefrontAntiforgeryTokenReader.cs` | WASM JS interop implementation | Keep in browser area |
| `Browser/StorefrontAntiforgeryToken.cs` | Antiforgery token model | Keep |

## Target architecture

```text
Storefront.Components
  Contracts/
    Catalog/
    Product/
    Cart/
    Checkout/
    Account/
  Headless/
    Product/
    Cart/
    Checkout/
    Account/
  Browser/
    same-origin BFF primitives

Storefront.V2
  Components/
    Catalog/
    Product/
    Deals/
    Cart/
    Checkout/
    Account/
  Pages/
    route, SEO, auth, initial snapshot, BFF composition

Storefront.Starter
  Components/
    neutral/basic visual templates

Storefront.{Name}
  Components/
    generated/custom visual templates
```

## Non-goals

- [x] Do not change Commerce Node API behavior.
- [x] Do not move backend business rules into `Storefront.Components`.
- [x] Do not introduce a new shared theme package in this phase.
- [x] Do not force Starter or generated storefronts to reuse V2 visual markup.
- [x] Do not remove same-origin BFF security pattern.
- [x] Do not add generated Commerce Node API clients to WASM/browser.
- [x] Do not migrate every interactive component in one commit.
- [x] Do not make component guardrails so strict that accessibility-only classes such as `sr-only` become impossible.

## Phase HPR0 - Baseline inventory and guardrail design

Goal: freeze the component inventory before moving any markup.

- [x] Record current Feature inventory:
  - [x] `Account/AccountAddressBook.razor`
  - [x] `Account/AccountApp.razor`
  - [x] `Account/AccountChangePasswordForm.razor`
  - [x] `Account/AccountNavigation.razor`
  - [x] `Account/AccountOrderDetail.razor`
  - [x] `Account/AccountOrderList.razor`
  - [x] `Account/AccountProfileEditor.razor`
  - [x] `Cart/CartView.razor`
  - [x] `Catalog/ProductSummaryCard.razor`
  - [x] `Catalog/ProductSummaryGrid.razor`
  - [x] `Checkout/CheckoutShell.razor`
  - [x] `Deals/DealsBlock.razor`
  - [x] `Product/ProductGallery.razor`
  - [x] `Product/ProductPurchasePanel.razor`
- [x] Record current Feature model/enum inventory:
  - [x] `Catalog/ProductSummaryItem.cs`
  - [x] `Deals/DealsPlacement.cs`
  - [x] `Product/ProductGalleryItem.cs`
  - [x] `Product/ProductPurchasePanelModels.cs`
- [x] Record Browser support inventory:
  - [x] `StorefrontBrowserCartModels.cs`
  - [x] `StorefrontBrowserCheckoutModels.cs`
  - [x] `StorefrontBrowserAccountModels.cs`
  - [x] `StorefrontFeatureDataMode.cs`
  - [x] `StorefrontLocalApiClient.cs`
  - [x] `StorefrontLocalApiResult.cs`
  - [x] antiforgery reader/token files.
- [x] Add an inventory test or static assertion that fails when a new `Features/*.razor` file appears without being classified.
- [x] Add a component neutrality guard design:
  - [x] Forbidden in shared visual/headless components after migration: `bg-*`, `text-neutral-*`, `text-rose-*`, `text-amber-*`, `text-emerald-*`, `rounded-*`, `shadow-*`, `max-w-*`, `grid-cols-*`, `sm:*`, `md:*`, `lg:*`, `hover:*`, page containers.
  - [x] Allowed: `sr-only`, `hidden`, ARIA attributes, `data-storefront-*`, semantic `bs-*`, minimal state classes required for behavior.
  - [x] Route strings such as `/api/*`, `#purchase`, `#product-cart-feedback` must be parameterized or host-owned.
- [x] Do not enable strict failure until a component group has been migrated, otherwise every current component will fail immediately.

### HPR0 QA gate

- [x] `dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.Components/BlazorShop.Storefront.Components.csproj`.
- [x] Existing Storefront component boundary tests pass.
- [x] New inventory test captures all 14 current Razor components and 4 Feature model/enum files.

## Phase HPR1 - Headless contract folder and naming foundation

Goal: create a place for reusable contracts without moving all visual files at once.

- [x] Create target namespace/folder convention:
  - [x] `Contracts/Catalog`.
  - [x] `Contracts/Product`.
  - [x] `Contracts/Cart`.
  - [x] `Contracts/Checkout`.
  - [x] `Contracts/Account`.
  - [x] `Headless/Product`.
  - [x] `Headless/Cart`.
  - [x] `Headless/Checkout`.
  - [x] `Headless/Account`.
- [x] Decide temporary compatibility strategy:
  - [x] Keep old model type names with forwarding wrappers if needed.
  - [x] Avoid broad namespace rename if it would touch every V2 page in one commit.
- [x] Update `Features/README.md`:
  - [x] `Features` is no longer a place for shared V2 visual implementation.
  - [x] Shared visual components are allowed only as temporary compatibility wrappers during migration.
  - [x] Store-owned visual templates belong in `Storefront.V2`, `Starter`, or `{Name}`.
- [x] Update architecture docs if the meaning of `Storefront.Components` changes from portable visual components to headless contracts/behavior.

### HPR1 QA gate

- [x] `dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.Components/BlazorShop.Storefront.Components.csproj`.
- [x] Docs/guardrail tests confirm new folder convention.

## Phase HPR2 - Catalog leaf: ProductSummaryCard

Goal: remove V2 product-card visual lock from shared components.

- [x] Keep `ProductSummaryItem` as component-facing contract.
- [x] Reconcile current V2-local `Components/Catalog/ProductCard.razor` with shared `ProductSummaryCard.razor`:
  - [x] If V2-local product card already covers the visual behavior, switch V2 pages to V2 card/grid.
  - [x] If shared card has behavior not present in V2-local card, port that behavior into V2 card before deleting shared visual use.
- [x] Move or duplicate V2 visual markup into `Storefront.V2/Components/Catalog`.
- [x] Leave behind only:
  - [x] `ProductSummaryItem`.
  - [x] optional headless product card context/state.
  - [x] optional deprecated wrapper if needed for one transition commit.
- [x] Remove theme/layout utility classes from shared `ProductSummaryCard` or delete the shared Razor file when no consumer remains.

### HPR2 QA gate

- [x] Category page renders product cards.
- [x] Search page renders product cards.
- [x] New releases page renders product cards.
- [x] Product direct add-to-cart still works where enabled.
- [x] Static guard: shared `ProductSummaryCard.razor` has no V2 theme/layout classes or no longer exists.

## Phase HPR3 - Catalog composition: ProductSummaryGrid

Goal: make grid layout store-owned.

- [x] Move `ProductSummaryGrid` visual markup to V2.
- [x] Keep a neutral list/collection contract if useful:
  - [x] items.
  - [x] empty message.
  - [x] optional item context.
- [x] V2 owns:
  - [x] column count.
  - [x] gap.
  - [x] empty-state visual.
  - [x] list/card wrapper markup.
- [x] Update V2 pages using `ProductSummaryGrid`:
  - [x] `CategoryPage.razor`.
  - [x] `SearchPage.razor`.
  - [x] `NewReleases.razor`.

### HPR3 QA gate

- [x] Category/search/new releases still render with same V2 visual output.
- [x] Shared `ProductSummaryGrid.razor` removed or reduced to headless collection contract.

## Phase HPR4 - Deals composition: DealsBlock

Goal: remove V2 deals section composition from shared components.

- [x] Move `DealsBlock` visual to V2, or replace it with V2-owned `DealsSection`.
- [x] Remove dependency chain `DealsBlock -> ProductSummaryGrid -> ProductSummaryCard` from shared package.
- [x] Decide `DealsPlacement` owner:
  - [x] Move to V2 if only V2 deals composition uses it.
  - [x] Keep in components contract only if Starter/generated storefront also needs the same placement enum.
- [x] Update V2 pages:
  - [x] `Home.razor`.
  - [x] `TodaysDeals.razor`.
  - [x] Any product detail footer deals placement if enabled later.

### HPR4 QA gate

- [x] Home deals section renders.
- [x] Today's deals route renders.
- [x] Shared `DealsBlock.razor` removed or headless-only.
- [x] No shared component composes catalog card/grid visually.

## Phase HPR5 - Product gallery headless state

Goal: keep gallery behavior but make visual layout store-owned.

- [x] Keep `ProductGalleryItem`.
- [x] Add headless gallery state/context:
  - [x] selected index.
  - [x] selected item.
  - [x] next/previous availability.
  - [x] select thumbnail action.
  - [x] alt text/fallback state.
  - [x] stable `data-storefront-gallery-*` hook names.
- [x] Move V2 visual gallery markup to `Storefront.V2/Components/Product`.
- [x] Preserve V2 requirement that product images render square/1x1 in V2 visual template.
- [x] Do not force square layout in shared headless contract.
- [x] Keep semantic hooks possible but not layout-defining.

### HPR5 QA gate

- [x] Product detail gallery renders.
- [x] Thumbnail selection changes main image.
- [x] Missing/broken image fallback still works.
- [x] Static guard: shared gallery has no `aspect-square`, thumbnail size, `rounded-*`, `bg-neutral-*`, or store-specific layout classes.

## Phase HPR6 - Product purchase headless behavior

Goal: split purchase state/actions from purchase panel visual markup.

- [x] Keep `ProductPurchasePanelModel` but split if needed:
  - [x] `ProductPurchaseSnapshot`.
  - [x] `ProductPurchaseSelectionState`.
  - [x] `ProductPurchaseOptionItem`.
  - [x] `ProductPurchaseVariantItem`.
- [x] Create headless selection behavior:
  - [x] selected variant.
  - [x] selected attributes.
  - [x] quantity.
  - [x] validation messages.
  - [x] can add to cart.
  - [x] preview pending/error state.
  - [x] add-to-cart pending/error/success state.
- [x] Replace hardcoded host assumptions:
  - [x] `/api/product-selection-preview`.
  - [x] `#purchase`.
  - [x] `#product-cart-feedback`.
  - [x] `data-feedback-target`.
- [x] Host/V2 provides route/action descriptors.
- [x] Move V2 purchase panel visual to `Storefront.V2/Components/Product`.

### HPR6 QA gate

- [x] Product detail selection preview still works.
- [x] Variant/attribute selection updates price/availability/image where fixture supports it.
- [x] Add-to-cart still works.
- [x] Static guard: shared purchase behavior has no hardcoded `/api/*` route or V2 visual classes.

## Phase HPR7 - Cart headless state/actions

Goal: split cart browser behavior from V2 cart page visual.

- [x] Keep `StorefrontBrowserCartModels.cs` browser-safe contracts.
- [x] Define cart route/action descriptors supplied by host:
  - [x] get current cart.
  - [x] update line.
  - [x] remove line.
  - [x] clear cart.
- [x] Create headless cart state/actions:
  - [x] loading.
  - [x] empty.
  - [x] error.
  - [x] alerts/warnings.
  - [x] quantity edit.
  - [x] remove.
  - [x] clear.
  - [x] checkout allowed.
- [x] Move V2 cart page visual to `Storefront.V2/Components/Cart`.
- [x] Keep `CartPage.razor` as route/initial-snapshot composition.
- [x] Remove hardcoded `/api/cart` route strings from shared cart component.

### HPR7 QA gate

- [x] Cart initial snapshot renders without duplicate fetch.
- [x] Quantity update works.
- [x] Remove line works.
- [x] Clear cart works.
- [x] Empty cart state renders.
- [x] Static guard: shared cart components contain no `/api/cart` literals or V2 layout classes.

## Phase HPR8 - Checkout headless state/actions

Goal: split checkout command/state behavior from V2 checkout visual shell.

- [x] Keep `StorefrontBrowserCheckoutModels.cs` browser-safe contracts.
- [x] Define checkout route/action descriptors supplied by host:
  - [x] get checkout.
  - [x] shipping method.
  - [x] payment method.
  - [x] review.
  - [x] place order.
- [x] Create headless checkout state/actions:
  - [x] load state.
  - [x] select shipping.
  - [x] select payment.
  - [x] review terms.
  - [x] place order with idempotency key.
  - [x] conflict/validation/error state.
- [x] Move V2 checkout shell visual to `Storefront.V2/Components/Checkout`.
- [x] Keep `CheckoutPage.razor` as route/initial-snapshot composition.
- [x] Remove hardcoded `/api/checkout*` route strings from shared checkout component.

### HPR8 QA gate

- [x] Checkout initial snapshot renders.
- [x] Shipping/payment selection works.
- [x] Review works.
- [x] COD place order works in focused browser QA if checkout behavior changed.
- [x] Static guard: shared checkout components contain no `/api/checkout` literals or V2 layout classes.

## Phase HPR9 - Account leaf: navigation

Goal: make account navigation store-owned.

- [x] Move `AccountNavigation` visual markup to V2 account components.
- [x] Keep optional account nav contract:
  - [x] route key.
  - [x] label.
  - [x] href.
  - [x] active state.
- [x] Do not hardcode account visual style in shared components.

### HPR9 QA gate

- [x] Account profile/addresses/orders/password nav works.
- [x] Active item state works.

## Phase HPR10 - Account leaf: profile and password forms

Goal: split simpler account forms first.

- [x] `AccountProfileEditor.razor`:
  - [x] Keep `StorefrontBrowserCustomerProfile`.
  - [x] Keep `StorefrontBrowserCustomerProfileUpdateRequest`.
  - [x] Extract load/save state and action descriptor.
  - [x] Move visual form to V2.
  - [x] Remove hardcoded `/api/account/profile`.
- [x] `AccountChangePasswordForm.razor`:
  - [x] Extract form state and submit action descriptor.
  - [x] Move visual form to V2.
  - [x] Remove hardcoded `/api/account/change-password`.

### HPR10 QA gate

- [x] Account profile loads and saves.
- [x] Change password submits and renders validation/success/error states.
- [x] Shared profile/password components have no endpoint literals or V2 visual classes.

## Phase HPR11 - Account leaf: address book

Goal: split the highest-write account leaf before order views.

- [x] Keep `StorefrontBrowserCustomerAddress` and request models.
- [x] Extract address state/actions:
  - [x] load addresses.
  - [x] create.
  - [x] update.
  - [x] delete.
  - [x] set default shipping.
  - [x] set default billing.
  - [x] success/error state.
- [x] Move visual address book/cards/forms to V2.
- [x] Remove hardcoded `/api/account/addresses*` literals from shared address behavior.

### HPR11 QA gate

- [x] Add/edit/delete address works.
- [x] Set default shipping/billing works.
- [x] Empty address state renders.
- [x] Shared address behavior has no endpoint literals or V2 visual classes.

## Phase HPR12 - Account leaf: order list and order detail

Goal: split read-only order self-service views.

- [x] `AccountOrderList.razor`:
  - [x] Keep `StorefrontBrowserAccountOrderList`.
  - [x] Extract paging/load state and action descriptor.
  - [x] Move table/card visual to V2.
  - [x] Remove hardcoded `/api/account/orders?page=`.
- [x] `AccountOrderDetail.razor`:
  - [x] Keep `StorefrontBrowserAccountOrderDetail`.
  - [x] Extract detail load state and action descriptor.
  - [x] Move detail/receipt visual to V2.
  - [x] Remove hardcoded `/api/account/orders/{reference}`.

### HPR12 QA gate

- [x] Order history list loads.
- [x] Order detail deep link loads.
- [x] Receipt mode still works if supported.
- [x] Authorization/forbidden state still renders safely.

## Phase HPR13 - Account composition shell

Goal: migrate `AccountApp` only after all account leaf components have headless contracts.

- [x] Keep account route interpretation behavior:
  - [x] profile.
  - [x] addresses.
  - [x] orders.
  - [x] orders/{reference}.
  - [x] change-password.
- [x] Move account shell visual to V2.
- [x] Shared account headless app may keep:
  - [x] active panel resolution.
  - [x] route parsing helper if route-independent enough.
  - [x] sign-in-required/error state contract.
- [x] Remove direct composition of V2 visual leaf components from shared package.

### HPR13 QA gate

- [x] `/account`.
- [x] `/account/profile`.
- [x] `/account/addresses`.
- [x] `/account/orders`.
- [x] `/account/orders/{reference}`.
- [x] `/account/change-password`.
- [x] Unauthenticated account redirect still works.
- [x] Account routes remain noindex.

## Phase HPR14 - Browser support cleanup

Goal: make `Browser/*` primitives clearly behavior-only.

- [x] Keep `StorefrontLocalApiClient` same-origin enforcement.
- [x] Ensure `StorefrontLocalApiClient` still rejects absolute URLs.
- [x] Ensure route strings are passed by host/action descriptors, not embedded in reusable visual components.
- [x] Keep antiforgery reader/token browser-safe.
- [x] Review browser models for visual fields:
  - [x] Keep display values required by UI.
  - [x] Do not add layout/theme fields.
  - [x] Do not add admin/internal fields.
- [x] Update docs to say browser support is for BFF communication, not visual ownership.

### HPR14 QA gate

- [x] Storefront WASM build passes.
- [x] Components build passes.
- [x] Static guard no absolute Commerce Node/Control Plane browser calls.
- [x] Static guard no endpoint literals in migrated shared components.

## Phase HPR15 - Starter and generated storefront alignment

Goal: ensure future storefronts can create their own visual templates.

- [x] Update Starter guidance:
  - [x] Starter may use `Storefront.Components` contracts/headless behavior.
  - [x] Starter owns its neutral visual templates.
  - [x] Starter must not copy V2 visual components.
- [x] Update generated storefront guidance:
  - [x] `Storefront.{Name}` owns generated markup/CSS.
  - [x] Generated storefront uses contracts/behavior, not V2 visual markup.
  - [x] AI Generator can replace product card/grid/gallery/purchase/cart/checkout/account visual templates without changing core behavior.
- [x] Do not add production browser QA for generated storefront in this phase unless implementation touches StorefrontBuilder.

### HPR15 QA gate

- [x] Starter build/package proof still passes.
- [x] StorefrontBuilder static isolation tests still pass if related docs/gates are changed.

## Phase HPR16 - Final Storefront.V2 QA

Goal: prove V2 keeps existing behavior after moving visual implementation out of shared components.

- [x] Unit/static tests:
  - [x] Components boundary tests.
  - [x] Component theme/layout neutrality tests.
  - [x] Component host assumption tests.
  - [x] Storefront V2 composition tests.
  - [x] WASM build.
- [x] Browser QA with Playwright for affected flows:
  - [x] Home deals.
  - [x] Category product grid.
  - [x] Search product grid/empty state.
  - [x] New releases.
  - [x] Today's deals.
  - [x] Product gallery.
  - [x] Product purchase/add-to-cart.
  - [x] Cart load/update/remove/clear.
  - [x] Checkout COD path if checkout was changed.
  - [x] Account profile.
  - [x] Account password.
  - [x] Account addresses.
  - [x] Account orders list/detail.
- [x] Browser network assertions:
  - [x] Browser calls same-origin `/api/*` only.
  - [x] No direct Commerce Node host from browser.
  - [x] No Control Plane calls from storefront browser.
- [x] Update `QA-StorefrontV2.todo.md` with evidence if implementation changes behavior/rendering.

Evidence: `dotnet build BlazorShop.sln` passed; `dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --filter "FullyQualifiedName~Storefront"` passed `731/733` with `2` existing skipped tests. Playwright evidence: `output/playwright/hpr16-storefront-route-probe.json`, `output/playwright/hpr16-storefront-cart-mutation-probe.json`, `output/playwright/hpr16-storefront-account-probe.json`, plus `scripts/qa/run-storefront-order-email-e2e.ps1 -Headless` and `scripts/qa/run-storefront-registration-policy-e2e.ps1 -Headless`.

### HPR16 QA gate

- [x] `dotnet build BlazorShop.sln`.
- [x] `dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --filter "FullyQualifiedName~Storefront"`.
- [x] Targeted Playwright Storefront V2 pass for changed flows.

## Implementation order summary

1. HPR0 inventory and guardrail design.
2. HPR1 contract/headless folder foundation.
3. HPR2 `ProductSummaryCard`.
4. HPR3 `ProductSummaryGrid`.
5. HPR4 `DealsBlock`.
6. HPR5 `ProductGallery`.
7. HPR6 `ProductPurchasePanel`.
8. HPR7 `CartView`.
9. HPR8 `CheckoutShell`.
10. HPR9 `AccountNavigation`.
11. HPR10 `AccountProfileEditor` and `AccountChangePasswordForm`.
12. HPR11 `AccountAddressBook`.
13. HPR12 `AccountOrderList` and `AccountOrderDetail`.
14. HPR13 `AccountApp`.
15. HPR14 browser support cleanup.
16. HPR15 Starter/generated alignment.
17. HPR16 final Storefront V2 QA.

## Completeness checklist

- [x] `ProductSummaryCard.razor` covered.
- [x] `ProductSummaryGrid.razor` covered.
- [x] `DealsBlock.razor` covered.
- [x] `ProductGallery.razor` covered.
- [x] `ProductPurchasePanel.razor` covered.
- [x] `CartView.razor` covered.
- [x] `CheckoutShell.razor` covered.
- [x] `AccountNavigation.razor` covered.
- [x] `AccountProfileEditor.razor` covered.
- [x] `AccountChangePasswordForm.razor` covered.
- [x] `AccountAddressBook.razor` covered.
- [x] `AccountOrderList.razor` covered.
- [x] `AccountOrderDetail.razor` covered.
- [x] `AccountApp.razor` covered.
- [x] `ProductSummaryItem.cs` covered.
- [x] `DealsPlacement.cs` covered.
- [x] `ProductGalleryItem.cs` covered.
- [x] `ProductPurchasePanelModels.cs` covered.
- [x] Browser cart models covered.
- [x] Browser checkout models covered.
- [x] Browser account models covered.
- [x] Feature data mode covered.
- [x] Local API/antiforgery browser primitives covered.

## Risk controls

- [x] Do not migrate cart and checkout in the same commit.
- [x] Do not migrate account shell before account leaf components.
- [x] Do not delete shared model contracts until all V2 consumers have replacement imports.
- [x] Do not remove same-origin BFF security behavior.
- [x] Do not loosen existing backend dependency guardrails.
- [x] Do not introduce V2 route helpers into `Storefront.Components`.
- [x] Do not make Starter depend on V2 visual components.
- [x] Do not mark the phase complete until an inventory script proves every current `Features/*.razor` and `Features/*.cs` file is represented.

## Suggested verification commands

```powershell
dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.Components/BlazorShop.Storefront.Components.csproj
dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.WASM/BlazorShop.Storefront.WASM.csproj
dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.V2/BlazorShop.Storefront.V2.csproj
dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --filter "FullyQualifiedName~Storefront"
```

## Autoplan decision audit trail

| # | Decision | Rationale | Rejected |
| --- | --- | --- | --- |
| 1 | Refactor catalog/deals/product before cart/checkout/account | These components have fewer browser API dependencies and prove the headless visual split first | Starting with account or checkout |
| 2 | Account shell is last | `AccountApp` composes all account leaf components; moving it first creates churn and likely broken paths | Migrating AccountApp before leaves |
| 3 | Keep Browser same-origin primitives | They enforce BFF security and are not the visual problem | Removing browser BFF primitives |
| 4 | Route strings become host/action descriptors | Shared components should not assume V2 local endpoint names | Hardcoding `/api/*` in headless package |
| 5 | V2 owns current visual markup during migration | Preserves existing production storefront while freeing Starter/generated storefronts to render differently | Deleting V2 visual behavior during extraction |

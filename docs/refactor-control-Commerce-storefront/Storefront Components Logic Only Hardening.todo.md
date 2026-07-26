# Storefront Components Logic Only Hardening.todo

Status: in progress

Source: autoplan after repository review of `BlazorShop.Storefront.Components` current CSS-neutral compatibility state, remaining `Features` Razor wrappers, reverse dependency from `Headless` to `Features`, V2-specific defaults in shared descriptors, visual class-bag schemas, copywriting/localization ownership, account route parsing, package metadata, and browser local API error primitives.

## Goal

Move `BlazorShop.Storefront.Components` from CSS-neutral compatibility components toward true browser-safe Storefront contracts and headless interaction primitives.

The intended result:

- `Contracts/{Capability}` owns render/input contracts that can be reused by V2, Starter, and generated/custom `Storefront.{Name}`.
- `Headless/{Capability}` owns browser-safe state, behavior, action descriptors, command handlers, and stable semantic hooks.
- `Features/*` Razor components are explicitly temporary compatibility wrappers and are not treated as stable presentation contracts.
- Shared Headless code no longer depends on `Features`.
- V2-specific defaults live in `Storefront.V2`.
- Store-owned visual templates own DOM composition, region/class schemas, copywriting, account route interpretation, and localization.
- Browser local BFF primitives carry structured error semantics instead of only user-facing message strings.

## Current Verified Context

- [x] `docs/architecture/05-project-and-folder-guide.md` says `BlazorShop.Storefront.Components` is for headless Storefront presentation contracts, browser-safe behavior/state primitives, same-origin browser/BFF support, and temporary compatibility feature components while V2 visual markup is migrated into host-owned templates.
- [x] `docs/architecture/10-v2-contract-ownership.md` says portable component models are not public HTTP contracts and must not reference backend/core/API projects, Storefront route helpers, or admin-owned fields.
- [x] `docs/refactor-control-Commerce-storefront/Storefront Components Headless Presentation Refactor.todo.md` already tracks HPR0-HPR16 and has moved part of the visual work to V2.
- [x] `BlazorShop.Storefront.Components/Features/README.md` explicitly says `Features` is a temporary compatibility area.
- [x] There is no root `README.md` under `BlazorShop.Storefront.Components`; current package role is visible through folder READMEs and csproj metadata.
- [x] `BlazorShop.Storefront.Components.csproj` still describes the package as `Portable Blazor storefront presentation components for Storefront V2 and generated storefronts.`
- [x] Current `Features` inventory still contains 14 Razor components:
  - [x] account navigation, account app, profile editor, password form, address book, order list, order detail.
  - [x] cart view.
  - [x] checkout shell.
  - [x] product summary card and grid.
  - [x] deals block.
  - [x] product gallery.
  - [x] product purchase panel.
- [x] `Contracts/{Account,Cart,Catalog,Checkout,Product}` folders currently contain README files only; primary reusable models still live under `Features`.
- [x] `ProductSummaryItem` currently lives in `Features/Catalog/ProductSummaryItem.cs`.
- [x] `ProductGalleryItem` currently lives in `Features/Product/ProductGalleryItem.cs`.
- [x] `ProductPurchasePanelModel` and related purchase option/variant records currently live in `Features/Product/ProductPurchasePanelModels.cs`.
- [x] `Headless/Product/ProductPurchaseBehavior.cs` imports `BlazorShop.Storefront.Components.Features.Product`.
- [x] `Headless/Product/ProductGalleryState.cs` imports `BlazorShop.Storefront.Components.Features.Product`.
- [x] `ProductPurchaseActionDescriptor.StorefrontV2Default` currently lives in shared Headless and contains `/api/product-selection-preview`.
- [x] V2 `StorefrontProductPurchasePanel.razor` currently uses `ProductPurchaseActionDescriptor.StorefrontV2Default`.
- [x] Shared Razor files are mostly CSS-neutral but still define DOM order and visible labels.
- [x] `ProductSummaryCard.razor` defines header, price, image, description, footer, badges, and actions order.
- [x] `ProductPurchasePanel.razor` defines message, shipping, options/variant selection, quantity, add button, cart link, and feedback order.
- [x] `StorefrontCartViewClasses` lives in `Headless/Cart/StorefrontCartBehavior.cs` and defines visual regions such as page section, layout, header card, line card, summary aside, and checkout button.
- [x] Similar class bags exist for checkout and account under Headless account/checkout behavior.
- [x] Shared package still owns copywriting such as `Add to Cart`, `View Cart`, `Image unavailable`, `Selection ready.`, account page titles, and success messages.
- [x] `ProductPurchaseSnapshot.InitialSelectionMessage` returns `Selection ready.` from Headless.
- [x] `ProductGalleryState.FallbackAltText` returns `Image unavailable for {DisplayProductName}` from Headless.
- [x] `AccountNavigation.razor` already receives host-provided navigation items and no longer hardcodes `/account/*` routes.
- [x] `AccountApp.razor` still parses account route segments and contains `/account/profile`, `/account/addresses`, `/account/orders`, and `/account/change-password`.
- [x] `StorefrontLocalApiClient` correctly rejects absolute and protocol-relative routes for same-origin BFF calls.
- [x] `StorefrontLocalApiClient` treats only `ContentLength == 0` as empty success body; `ContentLength == null` with empty body can still attempt JSON parsing.
- [x] `StorefrontLocalApiResult<T>` only carries `Success`, `StatusCode`, `Data`, and `Message`.
- [x] `StorefrontLocalApiErrorResponse` only carries `Message`.
- [x] V2 local endpoint support currently emits `StorefrontLocalApiErrorResponse(NormalizeLocalErrorMessage(message))`.

## Relationship To Existing Plans

- [x] This plan extends `Storefront Components Headless Presentation Refactor.todo.md`.
- [x] The HPR plan removes V2 CSS/theme values from shared visual components and moves V2 visual templates into `Storefront.V2`.
- [x] This CLH plan removes the remaining headless blockers after CSS neutrality:
  - [x] contract models still under `Features`.
  - [x] `Headless` depending on `Features`.
  - [x] host defaults inside shared descriptors.
  - [x] class bags acting as visual schemas.
  - [x] shared package owning copy and routes.
  - [x] browser local API errors too message-only.
- [x] This plan should be implemented after, or as a tightly scoped continuation of, the HPR phases that already created V2 visual components.
- [x] Do not collapse this work into `Storefront Runtime Hardening.todo.md`; Runtime and Components have different boundaries.

## Non-goals

- [x] Do not change Commerce Node Storefront API contracts in this plan.
- [x] Do not move business rules into `Storefront.Components`.
- [x] Do not move generated Storefront API clients into `Storefront.Components`.
- [x] Do not make WASM call Commerce Node directly.
- [x] Do not redesign Storefront V2 visual output during logic-only hardening.
- [x] Do not remove all `Features/*.razor` in one commit unless all V2/Starter consumers already have host-owned replacements.
- [x] Do not force Starter or generated storefronts to consume V2 visual templates.
- [x] Do not create a theme/design-system package.
- [x] Do not remove same-origin BFF route protection.
- [x] Do not remove existing compatibility types before V2, Starter, tests, and StorefrontBuilder docs have a migration path.

## Target Ownership

```text
Storefront.Components
  Contracts/
    Catalog/
      ProductSummaryItem
    Product/
      ProductGalleryItem
      ProductPurchasePanelModel
      ProductPurchaseOptionItem
      ProductPurchaseOptionValueItem
      ProductPurchaseVariantItem
      ProductPurchaseLabels
      ProductGalleryLabels
    Cart/
      StorefrontCartViewState
      StorefrontCartActionDescriptor
      StorefrontCartCommandHandler contracts
    Checkout/
      StorefrontCheckoutViewState
      StorefrontCheckoutActionDescriptor
    Account/
      AccountNavigationItem
      AccountRouteDescriptor
      AccountRouteParser contract
      Account labels/messages
  Headless/
    browser-safe state and behavior only
    no V2 route defaults
    no visual class-bag schema as stable API
  Browser/
    same-origin BFF client
    structured local API result/error
  Features/
    temporary compatibility wrappers only

Storefront.V2
  Components/
    owns DOM composition
    owns class bags/options
    owns V2 route defaults
    owns visible English copy until localization is introduced

Storefront.Starter / Storefront.{Name}
  consume Contracts/Headless/Browser
  render their own components/templates
```

## Phase Dependency Map

```text
CLH0 Baseline and compatibility lock
  -> CLH1 Contract model relocation
      -> CLH2 Headless dependency direction guardrails
          -> CLH3 Host-owned action defaults
              -> CLH4 Copywriting and localization descriptors
                  -> CLH5 Account route descriptor and composition boundary
                      -> CLH6 Visual class-bag migration strategy
                          -> CLH7 Browser local API structured errors
                              -> CLH8 Package metadata and docs
                                  -> CLH9 V2, Starter, StorefrontBuilder QA
```

## Phase CLH0 - Baseline And Compatibility Lock

Goal: freeze current Components state before moving contracts or route/copy ownership.

### Tasks

- [x] Record current working tree and avoid overwriting unrelated docs/OpenAPI hardening changes.
- [x] Record all current `Features/*.razor` files and their active consumers.
- [x] Record all model files currently under `Features`:
  - [x] `Features/Catalog/ProductSummaryItem.cs`
  - [x] `Features/Product/ProductGalleryItem.cs`
  - [x] `Features/Product/ProductPurchasePanelModels.cs`
  - [x] `Features/Deals/DealsPlacement.cs`
- [x] Record all `Headless -> Features` namespace dependencies.
- [x] Record all V2 usages of feature model namespaces.
- [x] Record all shared copywriting strings in `Features`, `Headless`, and `Browser`.
- [x] Record all hardcoded host routes in shared Components:
  - [x] same-origin BFF routes.
  - [x] account page routes.
  - [x] product selection preview route.
  - [x] cart and checkout routes.
- [x] Record current tests that intentionally allow CSS-neutral compatibility wrappers.
- [x] Decide phase label in docs:
  - [x] Current state: `CSS-neutral compatibility components`.
  - [x] Target state: `headless contracts and browser-safe behavior`.

### Files Likely Read

- `BlazorShop.PresentationV2/BlazorShop.Storefront.Components/**`
- `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Components/**`
- `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/**`
- `BlazorShop.Tests.V2/PresentationV2/Storefront/StorefrontComponentsHeadlessPresentationRefactorTests.cs`
- `docs/refactor-control-Commerce-storefront/Storefront Components Headless Presentation Refactor.todo.md`

### Verification

```powershell
git status --short
rg -n "using BlazorShop.Storefront.Components.Features|StorefrontV2Default|Selection ready|Image unavailable|/account/|Storefront request failed" BlazorShop.PresentationV2/BlazorShop.Storefront.Components BlazorShop.PresentationV2/BlazorShop.Storefront.V2 BlazorShop.Tests.V2/PresentationV2/Storefront
dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~StorefrontComponentsHeadlessPresentationRefactorTests|FullyQualifiedName~StorefrontWasmRuntimeFoundationTests"
```

### Done When

- [x] All current compatibility wrappers and blockers are inventoried.
- [x] The phase starts from observed code, not assumptions.
- [x] No behavior has changed yet.

### CLH0 Notes - 2026-07-26

- Working tree at phase start only had this new plan file untracked after Storefront Runtime Hardening completed.
- Current compatibility Razor wrappers: account navigation/app/profile/password/address/order list/order detail, cart view, checkout shell, product summary card/grid, deals block, product gallery, and product purchase panel.
- Current model files under `Features`: `ProductSummaryItem.cs`, `ProductGalleryItem.cs`, `ProductPurchasePanelModels.cs`, and `DealsPlacement.cs`.
- Current `Headless -> Features` dependencies are `Headless/Product/ProductPurchaseBehavior.cs` and `Headless/Product/ProductGalleryState.cs`.
- V2 still imports shared feature namespaces through `_Imports.razor`, `StorefrontProductSummaryMapper.cs`, and account/product host pages/components.
- Shared copy/route blockers include `Selection ready.`, `Image unavailable`, `Storefront request failed.`, `ProductPurchaseActionDescriptor.StorefrontV2Default`, `/api/product-selection-preview`, and `/account/*` constants in `AccountApp`.
- Baseline verification passed: focused `StorefrontComponentsHeadlessPresentationRefactorTests|StorefrontWasmRuntimeFoundationTests` ran `39/39` passing with existing MessagePack/Browserslist warnings.

## Phase CLH1 - Move Feature Models Into Contracts

Goal: make reusable render/input contracts live under `Contracts`, not `Features`.

### Tasks

- [x] Create or populate contract files:
  - [x] `Contracts/Catalog/ProductSummaryItem.cs`
  - [x] `Contracts/Product/ProductGalleryItem.cs`
  - [x] `Contracts/Product/ProductPurchasePanelModel.cs`
  - [x] `Contracts/Product/ProductPurchaseOptionItem.cs`
  - [x] `Contracts/Product/ProductPurchaseOptionValueItem.cs`
  - [x] `Contracts/Product/ProductPurchaseVariantItem.cs`
- [x] Move namespaces from:
  - [x] `BlazorShop.Storefront.Components.Features.Catalog`
  - [x] `BlazorShop.Storefront.Components.Features.Product`
  - [x] into:
  - [x] `BlazorShop.Storefront.Components.Contracts.Catalog`
  - [x] `BlazorShop.Storefront.Components.Contracts.Product`
- [x] Update V2 usages:
  - [x] product summary mapper.
  - [x] category/search/new releases/home/todays deals pages.
  - [x] V2 product card/grid/deals section.
  - [x] product page gallery/purchase mappings.
  - [x] V2 product gallery/purchase panel.
- [x] Update shared compatibility Razor wrappers to import Contracts.
- [x] Update tests that read old model paths.
- [x] Keep compatibility forwarding types only if the namespace move causes too much immediate churn.
- [n/a] If forwarding is used:
  - [n/a] mark it as temporary in comments/tests.
  - [n/a] add a removal phase.
  - [n/a] do not let Headless depend on forwarding types.
- [x] Decide owner for `DealsPlacement`:
  - [n/a] move to V2 if only V2 uses it.
  - [x] move to `Contracts/Catalog` or `Contracts/Deals` only if multiple storefront hosts need the same placement enum.

### Files Likely Touched

- `BlazorShop.PresentationV2/BlazorShop.Storefront.Components/Contracts/Catalog/*`
- `BlazorShop.PresentationV2/BlazorShop.Storefront.Components/Contracts/Product/*`
- `BlazorShop.PresentationV2/BlazorShop.Storefront.Components/Features/Catalog/*`
- `BlazorShop.PresentationV2/BlazorShop.Storefront.Components/Features/Product/*`
- `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/**/*`
- `BlazorShop.Tests.V2/PresentationV2/Storefront/*`

### QA Gate

```powershell
dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.Components/BlazorShop.Storefront.Components.csproj --no-restore
dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.V2/BlazorShop.Storefront.V2.csproj --no-restore
dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~StorefrontComponentsHeadlessPresentationRefactorTests|FullyQualifiedName~StorefrontBrandingMarkupTests"
```

### Done When

- [x] Primary reusable product/catalog models live under `Contracts`.
- [x] Headless and V2 can consume contract namespaces.
- [x] `Features` no longer owns reusable model definitions except temporary wrappers if explicitly documented.

### CLH1 Notes - 2026-07-26

- Moved `ProductSummaryItem`, `ProductGalleryItem`, `ProductPurchasePanelModel`, `ProductPurchaseOptionItem`, `ProductPurchaseOptionValueItem`, and `ProductPurchaseVariantItem` into `Contracts`.
- Moved `DealsPlacement` into `Contracts/Deals` because the compatibility deals wrapper and V2 deals section both use the same placement enum.
- No compatibility forwarding types were kept; callers now consume contract namespaces directly.
- Verification passed:
  - `dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.Components/BlazorShop.Storefront.Components.csproj --no-restore`
  - `dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.V2/BlazorShop.Storefront.V2.csproj --no-restore`
  - `dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~StorefrontComponentsHeadlessPresentationRefactorTests|FullyQualifiedName~StorefrontBrandingMarkupTests"` passed `37/37`.

## Phase CLH2 - Enforce Dependency Direction: Contracts -> Headless -> Features

Goal: prevent `Headless` from depending on compatibility `Features`.

### Tasks

- [x] Update `ProductPurchaseBehavior.cs` to import product contracts, not feature models.
- [x] Update `ProductGalleryState.cs` to import product contracts, not feature models.
- [x] Scan all `Headless/**/*.cs` files for:
  - [x] `BlazorShop.Storefront.Components.Features`
  - [x] `.Features.`
- [x] Add architecture test:
  - [x] `Contracts` must not reference `Headless`, `Browser`, or `Features`.
  - [x] `Headless` may reference `Contracts` and `Browser` only where browser-local models are intentionally part of behavior.
  - [x] `Headless` must not reference `Features`.
  - [x] `Features` may reference `Contracts`, `Headless`, and `Browser` as temporary compatibility wrappers.
- [x] Update `Contracts/*/README.md` to say contract models are stable presentation contracts, not API DTOs.
- [x] Update `Headless/*/README.md` to say Headless must not import `Features`.

### Files Likely Touched

- `BlazorShop.PresentationV2/BlazorShop.Storefront.Components/Headless/Product/ProductPurchaseBehavior.cs`
- `BlazorShop.PresentationV2/BlazorShop.Storefront.Components/Headless/Product/ProductGalleryState.cs`
- `BlazorShop.PresentationV2/BlazorShop.Storefront.Components/Contracts/*/README.md`
- `BlazorShop.PresentationV2/BlazorShop.Storefront.Components/Headless/*/README.md`
- `BlazorShop.Tests.V2/PresentationV2/Storefront/StorefrontComponentsHeadlessPresentationRefactorTests.cs`

### QA Gate

```powershell
rg -n "BlazorShop.Storefront.Components.Features|\.Features\." BlazorShop.PresentationV2/BlazorShop.Storefront.Components/Headless
dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~StorefrontComponentsHeadlessPresentationRefactorTests"
```

### Done When

- [x] `Headless` has no dependency on `Features`.
- [x] Tests enforce the intended dependency direction.
- [x] Future compatibility wrappers cannot become the stable contract layer accidentally.

### CLH2 Notes - 2026-07-26

- Added `ComponentsDependencyDirection_KeepsContractsAndHeadlessBelowFeatures` to block `Contracts -> Headless/Browser/Features` and `Headless -> Features` dependencies while allowing compatibility wrappers to consume lower layers.
- Added `Contracts/Deals/README.md` and updated contract/headless README files with stable presentation-contract and no-Features dependency rules.
- Verification passed:
  - `rg -n "BlazorShop.Storefront.Components.Features|\.Features\." BlazorShop.PresentationV2/BlazorShop.Storefront.Components/Headless` returned no matches.
  - `dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~StorefrontComponentsHeadlessPresentationRefactorTests"` passed `23/23`.

## Phase CLH3 - Move Host-Specific Action Defaults Out Of Shared Headless

Goal: remove V2 route defaults from shared descriptors.

### Tasks

- [x] Remove or obsolete `ProductPurchaseActionDescriptor.StorefrontV2Default` from shared Headless.
- [x] Keep `ProductPurchaseActionDescriptor.Empty` in shared Headless.
- [x] Add V2-owned product purchase action factory:
  - [n/a] `StorefrontProductPurchasePanelOptions.Actions`
  - [x] or `StorefrontProductPurchaseActionOptions.Default`
  - [x] located under `BlazorShop.Storefront.V2/Components/Product`.
- [x] Move `/api/product-selection-preview` into V2-owned options.
- [x] Update `StorefrontProductPurchasePanel.razor` to use V2-owned options by default.
- [x] Update product page composition to pass explicit actions if that is clearer.
- [x] Add guardrail:
  - [x] shared Components source must not contain `/api/product-selection-preview`.
  - [x] shared Headless must not contain `StorefrontV2Default`.
  - [x] V2 options must contain the route default.
- [x] Repeat scan for other host-specific route defaults under shared Headless.

### Files Likely Touched

- `BlazorShop.PresentationV2/BlazorShop.Storefront.Components/Headless/Product/ProductPurchaseBehavior.cs`
- `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Components/Product/*`
- `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/Hybrid/Catalog/ProductPage.razor`
- `BlazorShop.Tests.V2/PresentationV2/Storefront/StorefrontComponentsHeadlessPresentationRefactorTests.cs`

### QA Gate

```powershell
rg -n "StorefrontV2Default|/api/product-selection-preview" BlazorShop.PresentationV2/BlazorShop.Storefront.Components
dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.V2/BlazorShop.Storefront.V2.csproj --no-restore
dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~StorefrontComponentsHeadlessPresentationRefactorTests|FullyQualifiedName~StorefrontBrandingMarkupTests"
```

### Done When

- [x] Shared descriptors have no V2-specific route defaults.
- [x] V2 owns its BFF route defaults.
- [x] Product selection preview still works in V2.

### CLH3 Notes - 2026-07-26

- Removed `ProductPurchaseActionDescriptor.StorefrontV2Default` from shared Headless.
- Added V2-owned `StorefrontProductPurchaseActionOptions.Default` with `/api/product-selection-preview`.
- `StorefrontProductPurchasePanel.razor` now defaults to the V2 action options while shared `ProductPurchasePanel.razor` keeps `ProductPurchaseActionDescriptor.Empty`.
- Verification passed:
  - `rg -n "StorefrontV2Default|/api/product-selection-preview" BlazorShop.PresentationV2/BlazorShop.Storefront.Components` returned no matches.
  - `dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.V2/BlazorShop.Storefront.V2.csproj --no-restore`
  - `dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~StorefrontComponentsHeadlessPresentationRefactorTests|FullyQualifiedName~StorefrontBrandingMarkupTests"` passed `38/38`.

## Phase CLH4 - Move Copywriting Into Host Labels And Localization Descriptors

Goal: stop shared Components from owning final user-facing English copy.

### Tasks

- [x] Introduce small label descriptors under Contracts or Headless:
  - [x] `ProductSummaryLabels`
  - [x] `ProductGalleryLabels`
  - [x] `ProductPurchaseLabels`
  - [x] `CartLabels`
  - [x] `CheckoutLabels`
  - [x] `AccountLabels`
  - [n/a] `LocalApiErrorLabels` only if browser fallback copy remains needed.
- [x] Keep descriptors browser-safe and plain data.
- [x] Move V2 English defaults into V2 options:
  - [x] add to cart.
  - [x] added.
  - [x] view product.
  - [x] view cart.
  - [x] free shipping.
  - [x] choose/select variant.
  - [x] quantity.
  - [x] selection ready.
  - [x] image unavailable.
  - [x] account section titles.
  - [x] profile/address/password success messages.
  - [x] empty/error state copy for compatibility wrappers.
- [x] Update shared compatibility components to receive labels as parameters with empty/technical fallback only where required for accessibility.
- [x] Avoid localizing business/API validation messages inside shared components; show host-provided message or structured error code.
- [x] Update `ProductPurchaseSnapshot` so it does not hardcode `Selection ready.`
- [x] Update `ProductGalleryState` so fallback alt text comes from descriptor or a host-supplied function.
- [x] Add guardrail scans for known hardcoded copy in shared `Features`, `Headless`, and `Browser`.
- [x] Keep accessibility labels required by semantics, but make their wording host-provided when user-visible.

### Files Likely Touched

- `BlazorShop.PresentationV2/BlazorShop.Storefront.Components/Contracts/**`
- `BlazorShop.PresentationV2/BlazorShop.Storefront.Components/Headless/**`
- `BlazorShop.PresentationV2/BlazorShop.Storefront.Components/Features/**`
- `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Components/**`
- `BlazorShop.Tests.V2/PresentationV2/Storefront/*`

### QA Gate

```powershell
dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.Components/BlazorShop.Storefront.Components.csproj --no-restore
dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.V2/BlazorShop.Storefront.V2.csproj --no-restore
dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~StorefrontComponentsHeadlessPresentationRefactorTests|FullyQualifiedName~StorefrontWasmRuntimeFoundationTests"
```

### Done When

- [x] Shared Components no longer owns final storefront copy for product/cart/checkout/account flows.
- [x] V2 owns current English defaults until localization is introduced.
- [x] Starter/generated storefronts can provide their own labels without rewriting behavior.

### CLH4 Notes - 2026-07-26

- Added browser-safe label descriptors: `ProductSummaryLabels`, `ProductGalleryLabels`, `ProductPurchaseLabels`, `CartLabels`, `CheckoutLabels`, and `AccountLabels`.
- Migrated product summary, product gallery, and product purchase compatibility wrappers to receive labels as parameters with empty defaults.
- `ProductPurchaseSnapshot` now uses host-supplied `ProductPurchasePanelModel.PurchaseMessage` for the ready state instead of hardcoding `Selection ready.`
- `ProductGalleryState` no longer hardcodes fallback alt text; shared and V2 gallery templates own their own fallback wording.
- `Storefront request failed.` remains intentionally deferred to CLH7 structured browser local API errors.
- Verification passed:
  - `dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.Components/BlazorShop.Storefront.Components.csproj --no-restore`
  - `dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.V2/BlazorShop.Storefront.V2.csproj --no-restore`
  - `dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~StorefrontComponentsHeadlessPresentationRefactorTests|FullyQualifiedName~StorefrontWasmRuntimeFoundationTests"` passed `41/41`.
  - Product/headless copy scan found no `Selection ready`, `Image unavailable for`, hardcoded Add/View Cart, or Free shipping copy in migrated product compatibility/headless sources.

## Phase CLH5 - Move Account Route Interpretation To Host Boundary

Goal: prevent shared `AccountApp` from owning V2 account route structure.

### Tasks

- [x] Introduce account route contract:
  - [x] `AccountRouteDescriptor`
  - [x] `AccountRouteMatch`
  - [x] optional `IAccountRouteParser` or pure `AccountRouteParser` data-driven helper.
- [x] Host-owned descriptor should define:
  - [x] route keys.
  - [x] URL paths.
  - [x] display labels.
  - [x] order detail pattern.
  - [x] receipt mode pattern.
  - [x] default route.
  - [x] unknown route behavior.
- [x] Move V2 `/account/*` constants into `StorefrontAccountViewOptions`.
- [x] Update `AccountApp.razor` compatibility wrapper to receive route descriptor/parser from host.
- [x] If `AccountApp` remains too opinionated:
  - [x] keep it as V2 compatibility wrapper only.
  - [x] move it out of shared Components in a later phase.
  - [x] use shared leaf contracts/headless behavior for generated storefronts.
- [x] Do not make account page count grow again; prefer fewer host pages that compose leaf components.
- [x] Add guardrail:
  - [x] shared `AccountNavigation` must not hardcode `/account/*`.
  - [x] shared `AccountApp` must not hardcode `/account/*` after migration.
  - [x] V2 options own route constants.
- [x] Update tests currently expecting `AccountApp` to parse hardcoded `profile`, `addresses`, `orders`, and `change-password`.

### Files Likely Touched

- `BlazorShop.PresentationV2/BlazorShop.Storefront.Components/Contracts/Account/*`
- `BlazorShop.PresentationV2/BlazorShop.Storefront.Components/Headless/Account/*`
- `BlazorShop.PresentationV2/BlazorShop.Storefront.Components/Features/Account/AccountApp.razor`
- `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Components/Account/StorefrontAccountViewOptions.cs`
- `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/WasmHost/Account/AccountHostPage.razor`
- `BlazorShop.Tests.V2/PresentationV2/Storefront/*`

### QA Gate

```powershell
rg -n "/account/profile|/account/addresses|/account/orders|/account/change-password" BlazorShop.PresentationV2/BlazorShop.Storefront.Components
dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.WASM/BlazorShop.Storefront.WASM.csproj --no-restore
dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.V2/BlazorShop.Storefront.V2.csproj --no-restore
dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~StorefrontWasmRuntimeFoundationTests|FullyQualifiedName~StorefrontComponentsHeadlessPresentationRefactorTests"
```

### Done When

- [x] Account route interpretation is host-owned.
- [x] Shared account components can be reused by stores with different account URLs.
- [x] V2 account routes continue to work.

### CLH5 Notes - 2026-07-26

- Added `AccountRouteDescriptor`, `AccountRouteMatch`, `AccountRouteKind`, and data-driven `AccountRouteParser` under `Contracts/Account`.
- `AccountApp.razor` now receives `RouteDescriptor` and calls `AccountRouteParser.Resolve(Path, RouteDescriptor)` instead of hardcoding `/account/*` route constants or segment comparisons.
- `StorefrontAccountViewOptions` owns the current V2 route descriptor and `AccountHostPage` passes it into `AccountApp`.
- Verification passed:
  - `rg -n "/account/profile|/account/addresses|/account/orders|/account/change-password" BlazorShop.PresentationV2/BlazorShop.Storefront.Components/Features/Account/AccountApp.razor` returned no matches.
  - `dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.WASM/BlazorShop.Storefront.WASM.csproj --no-restore`
  - `dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.V2/BlazorShop.Storefront.V2.csproj --no-restore`
  - `dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~StorefrontWasmRuntimeFoundationTests|FullyQualifiedName~StorefrontComponentsHeadlessPresentationRefactorTests"` passed `41/41`.

## Phase CLH6 - Move Visual Class-Bag Schemas Out Of Stable Headless API

Goal: keep shared Headless behavior from defining store layout regions as stable contracts.

### Tasks

- [ ] Classify every class bag currently under `Headless`:
  - [ ] behavior-required state hooks.
  - [ ] accessibility/semantic hooks.
  - [ ] visual DOM region schema.
- [ ] Keep minimal semantic hook descriptors in shared package:
  - [ ] data attributes.
  - [ ] IDs/selectors needed by browser behavior.
  - [ ] action route descriptors.
  - [ ] state snapshots.
- [ ] Move visual class bags to V2 options where they only serve current V2 Razor wrappers:
  - [ ] `StorefrontCartViewClasses`
  - [ ] `StorefrontCheckoutViewClasses`
  - [ ] `StorefrontAccountFormClasses`
  - [ ] `StorefrontAccountAddressBookClasses`
  - [ ] `StorefrontAccountOrderListClasses`
  - [ ] `StorefrontAccountOrderDetailClasses`
  - [ ] `StorefrontAccountShellClasses`
  - [ ] `AccountNavigationClasses`
- [ ] If compatibility wrappers still need class bags:
  - [ ] move wrappers and class schemas to V2, or
  - [ ] mark wrappers/classes as compatibility-only with a removal trigger.
- [ ] Define stable shared state/action types:
  - [ ] `StorefrontCartViewState`
  - [ ] `StorefrontCartActionDescriptor`
  - [ ] `StorefrontCheckoutViewState`
  - [ ] `StorefrontCheckoutActionDescriptor`
  - [ ] account form action descriptors.
- [ ] Add tests to prevent new layout-region class bags in `Headless`.
- [ ] Do not break browser component behavior while moving visual schemas.

### Files Likely Touched

- `BlazorShop.PresentationV2/BlazorShop.Storefront.Components/Headless/Cart/StorefrontCartBehavior.cs`
- `BlazorShop.PresentationV2/BlazorShop.Storefront.Components/Headless/Checkout/StorefrontCheckoutBehavior.cs`
- `BlazorShop.PresentationV2/BlazorShop.Storefront.Components/Headless/Account/StorefrontAccountFormBehavior.cs`
- `BlazorShop.PresentationV2/BlazorShop.Storefront.Components/Headless/Account/AccountNavigationContracts.cs`
- V2 option files under `BlazorShop.Storefront.V2/Components/*`
- `BlazorShop.Tests.V2/PresentationV2/Storefront/*`

### QA Gate

```powershell
dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.Components/BlazorShop.Storefront.Components.csproj --no-restore
dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.V2/BlazorShop.Storefront.V2.csproj --no-restore
dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~StorefrontComponentsHeadlessPresentationRefactorTests|FullyQualifiedName~StorefrontWasmRuntimeFoundationTests"
```

### Done When

- [ ] Shared Headless no longer defines V2 cart/checkout/account layout region class schema as stable API.
- [ ] V2 visual templates own visual region/class options.
- [ ] Shared package retains only state/actions/hooks needed for behavior reuse.

## Phase CLH7 - Upgrade Browser Local API Result And Error Semantics

Goal: align browser BFF primitives with Runtime-style structured error semantics.

### Tasks

- [ ] Introduce structured browser local API error model:
  - [ ] `StatusCode`
  - [ ] `Code`
  - [ ] `TraceId`
  - [ ] `FieldErrors`
  - [ ] `Retryable`
  - [ ] `DefaultMessage` or technical fallback.
- [ ] Update `StorefrontLocalApiResult<T>`:
  - [ ] keep `Message` temporarily if active components read it.
  - [ ] add `Error` or structured error properties.
  - [ ] avoid forcing UI copy into the result model.
- [ ] Update `StorefrontLocalApiErrorResponse`:
  - [ ] accept optional `code`.
  - [ ] accept optional `traceId`.
  - [ ] accept optional `fieldErrors`.
  - [ ] accept optional `retryable`.
  - [ ] keep `message` compatibility during migration.
- [ ] Update `StorefrontLocalApiClient` success handling:
  - [ ] if content length is zero, return success default.
  - [ ] if content length is null, read as string first or handle empty stream safely.
  - [ ] do not throw `JsonException` for empty successful body.
- [ ] Update `StorefrontLocalApiClient` error handling:
  - [ ] parse structured error when available.
  - [ ] fallback to status/code/default message if body is empty or invalid.
  - [ ] do not turn all errors into `Storefront request failed.`.
- [ ] Update V2 local endpoint support:
  - [ ] include status code semantic.
  - [ ] include code where Runtime or BFF layer knows it.
  - [ ] include trace ID if available.
  - [ ] include field errors for validation.
- [ ] Add browser primitive tests:
  - [ ] same-origin route rejection still works.
  - [ ] mutating request still sends antiforgery.
  - [ ] empty success body with `ContentLength = null` does not fail JSON parsing.
  - [ ] structured error body preserves code/trace/field errors.
  - [ ] invalid error body falls back to technical default.
- [ ] Keep local BFF errors browser-safe; do not expose provider secrets/internal settings.

### Files Likely Touched

- `BlazorShop.PresentationV2/BlazorShop.Storefront.Components/Browser/StorefrontLocalApiClient.cs`
- `BlazorShop.PresentationV2/BlazorShop.Storefront.Components/Browser/StorefrontLocalApiResult.cs`
- `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Endpoints/StorefrontLocalEndpointSupport.cs`
- Storefront V2 local endpoint files that create `StorefrontLocalApiErrorResponse`.
- `BlazorShop.Tests.V2/PresentationV2/Storefront/StorefrontWasmRuntimeFoundationTests.cs`
- `BlazorShop.Tests.V2/PresentationV2/Storefront/StorefrontBffBoundaryHardeningTests.cs`

### QA Gate

```powershell
dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.Components/BlazorShop.Storefront.Components.csproj --no-restore
dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.WASM/BlazorShop.Storefront.WASM.csproj --no-restore
dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.V2/BlazorShop.Storefront.V2.csproj --no-restore
dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~StorefrontWasmRuntimeFoundationTests|FullyQualifiedName~StorefrontBffBoundaryHardeningTests|FullyQualifiedName~StorefrontCommerceFlowCutoverTests"
```

### Done When

- [ ] Browser BFF result supports structured errors.
- [ ] Empty successful BFF responses do not fail JSON parsing.
- [ ] Browser components can distinguish validation, auth, conflict, timeout, and generic failure states without parsing English copy.

## Phase CLH8 - Package Metadata, Docs, And Generator Guidance

Goal: make package role discoverable and prevent Starter/AI Generator from treating compatibility wrappers as required visual contracts.

### Tasks

- [ ] Update `BlazorShop.Storefront.Components.csproj` description to:

```text
Browser-safe Storefront contracts, headless interaction state, and compatibility component primitives.
```

- [ ] Add root `README.md` under `BlazorShop.Storefront.Components` if useful:
  - [ ] package purpose.
  - [ ] folder ownership.
  - [ ] current compatibility status.
  - [ ] what generated storefronts should consume.
  - [ ] what generated storefronts should not copy.
- [ ] Update `Features/README.md`:
  - [ ] call current wrappers `CSS-neutral compatibility wrappers`.
  - [ ] warn they are not stable presentation contracts.
  - [ ] direct Starter/AI Generator to `Contracts`, `Headless`, and `Browser`.
- [ ] Update `docs/architecture/05-project-and-folder-guide.md`:
  - [ ] distinguish `Contracts`, `Headless`, `Browser`, and `Features`.
  - [ ] document that class bags/copy/routes belong to host/storefront project.
- [ ] Update `docs/architecture/10-v2-contract-ownership.md`:
  - [ ] component contracts are presentation contracts, not public HTTP contracts.
  - [ ] generated storefronts should not infer API shapes from component models.
- [ ] Update StorefrontBuilder docs:
  - [ ] generated storefront uses `Contracts`/`Headless`/`Browser`.
  - [ ] generated storefront renders its own DOM.
  - [ ] generator must not import `Features` unless explicitly in compatibility mode.
  - [ ] generator must not treat V2 class bags as portable schema.
- [ ] Update QA checklist:
  - [ ] `QA-StorefrontV2.todo.md` includes browser/local BFF structured error checks.
  - [ ] StorefrontBuilder QA includes no `Features` dependency unless compatibility exception exists.

### Files Likely Touched

- `BlazorShop.PresentationV2/BlazorShop.Storefront.Components/BlazorShop.Storefront.Components.csproj`
- `BlazorShop.PresentationV2/BlazorShop.Storefront.Components/README.md`
- `BlazorShop.PresentationV2/BlazorShop.Storefront.Components/Features/README.md`
- `docs/architecture/05-project-and-folder-guide.md`
- `docs/architecture/10-v2-contract-ownership.md`
- `docs/architecture/11-storefront-builder.md`
- `docs/agents/storefront-builder.md`
- `docs/visual-reverse-engineering-skill/*`
- `docs/refactor-control-Commerce-storefront/QA-StorefrontV2.todo.md`

### QA Gate

```powershell
dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~StorefrontSharedPlatformPackageContractTests|FullyQualifiedName~StorefrontBuilderFoundationTests|FullyQualifiedName~StorefrontComponentsHeadlessPresentationRefactorTests"
```

### Done When

- [ ] Package metadata reflects the new role.
- [ ] Docs tell generators to use contracts/headless/browser primitives, not compatibility wrappers.
- [ ] Future contributors can identify CSS-neutral compatibility versus headless API.

## Phase CLH9 - V2, Starter, StorefrontBuilder QA And Release Proof

Goal: prove the logic-only hardening did not break active Storefront V2 behavior and does not mislead generated storefront consumers.

### Static And Build Verification

- [ ] Build shared packages:

```powershell
dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.Components/BlazorShop.Storefront.Components.csproj --no-restore
dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.WASM/BlazorShop.Storefront.WASM.csproj --no-restore
dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.V2/BlazorShop.Storefront.V2.csproj --no-restore
dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/BlazorShop.Storefront.Starter.csproj --no-restore
```

- [ ] Run focused tests:

```powershell
dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~StorefrontComponentsHeadlessPresentationRefactorTests|FullyQualifiedName~StorefrontWasmRuntimeFoundationTests|FullyQualifiedName~StorefrontBffBoundaryHardeningTests|FullyQualifiedName~StorefrontSharedPlatformPackageContractTests|FullyQualifiedName~StorefrontBuilderFoundationTests"
```

- [ ] Run source scans:

```powershell
rg -n "BlazorShop.Storefront.Components.Features" BlazorShop.PresentationV2/BlazorShop.Storefront.Components/Headless BlazorShop.PresentationV2/BlazorShop.Storefront.Starter tools/BlazorShop.AI.StorefrontBuilder
rg -n "StorefrontV2Default|/api/product-selection-preview|/account/profile|/account/orders|Selection ready|Image unavailable|Storefront request failed" BlazorShop.PresentationV2/BlazorShop.Storefront.Components
```

### Browser Verification If Active V2 Components Changed

- [ ] Start V2 local runtime:

```powershell
.\scripts\run-v2-local.ps1 -StopExisting
```

- [ ] Run targeted Playwright Storefront V2 cases:
  - [ ] product card renders and add-to-cart path still works.
  - [ ] product gallery switches images.
  - [ ] product purchase panel selection preview still works.
  - [ ] cart WASM component loads/updates/removes/clears through same-origin BFF.
  - [ ] checkout WASM component can progress through review/place-order COD if touched.
  - [ ] account WASM profile/address/order/password flows still work if touched.
  - [ ] browser network calls same-origin BFF only.
  - [ ] no direct Commerce Node browser call.
  - [ ] structured BFF errors render usable validation/auth/conflict states.

### StorefrontBuilder Verification

- [ ] Run generated storefront static proof if StorefrontBuilder docs or generator rules changed:

```powershell
.\scripts\qa\run-storefront-builder-isolation-gate.ps1
```

- [ ] If available and relevant, run generated proof:

```powershell
.\scripts\qa\run-storefront-builder-generated-proof.ps1
```

### Done When

- [ ] Components package builds.
- [ ] V2 and WASM build.
- [ ] Starter builds.
- [ ] Focused tests pass.
- [ ] Browser QA passes for changed flows.
- [ ] Generated storefront guidance remains package-first and does not depend on shared visual wrappers.

## Suggested Implementation Order

1. CLH0 baseline and compatibility lock.
2. CLH1 move reusable models into Contracts.
3. CLH2 enforce dependency direction.
4. CLH3 move V2 action defaults into V2.
5. CLH4 move copywriting into host label descriptors.
6. CLH5 move account route interpretation to host boundary.
7. CLH6 remove visual class-bag schemas from stable Headless API.
8. CLH7 upgrade browser local API error semantics.
9. CLH8 update package metadata/docs/generator guidance.
10. CLH9 run focused QA and release proof.

## Final Verification Commands

```powershell
dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.Components/BlazorShop.Storefront.Components.csproj --no-restore
dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.WASM/BlazorShop.Storefront.WASM.csproj --no-restore
dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.V2/BlazorShop.Storefront.V2.csproj --no-restore
dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/BlazorShop.Storefront.Starter.csproj --no-restore
dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~StorefrontComponentsHeadlessPresentationRefactorTests|FullyQualifiedName~StorefrontWasmRuntimeFoundationTests|FullyQualifiedName~StorefrontBffBoundaryHardeningTests|FullyQualifiedName~StorefrontSharedPlatformPackageContractTests|FullyQualifiedName~StorefrontBuilderFoundationTests"
```

If active V2 browser behavior changes:

```powershell
.\scripts\run-v2-local.ps1 -StopExisting
```

Then run the relevant Storefront V2 Playwright release cases for product, cart, checkout, and account flows.

## Completion Checklist

- [ ] Reusable product/catalog models live under `Contracts`.
- [ ] `Headless` no longer references `Features`.
- [ ] Shared Headless contains no V2 route defaults.
- [ ] V2 owns `/api/product-selection-preview` default.
- [ ] Shared Components does not own final storefront copy for migrated flows.
- [ ] Account route interpretation is host-owned or clearly compatibility-only.
- [ ] Visual class-bag schemas are host-owned or explicitly temporary compatibility-only.
- [ ] Browser local API result carries structured error semantics.
- [ ] Empty success body handling is safe when `ContentLength` is `null`.
- [ ] Package metadata reflects contracts/headless/browser role.
- [ ] Starter and generated storefront docs do not treat `Features` wrappers as required presentation contracts.
- [ ] V2/Components/WASM/Starter builds pass.
- [ ] Focused tests pass.
- [ ] Browser QA passes where flows changed.

## Autoplan Decision Audit Trail

| # | Decision | Classification | Principle | Rationale | Rejected |
| --- | --- | --- | --- | --- | --- |
| 1 | Treat current Components state as CSS-neutral compatibility, not complete headless | Auto-decided | Name the real system state | The code still contains 14 shared Razor wrappers and DOM/copy/route/class-schema decisions | Claim Components is already fully logic-only |
| 2 | Create a follow-up hardening plan instead of rewriting the existing HPR plan | Auto-decided | Preserve useful historical plan state | HPR already records CSS-neutral migration phases; this plan targets remaining blockers | Merge all new work into the long in-progress HPR file |
| 3 | Move models from `Features` to `Contracts` before further headless cleanup | Auto-decided | Dependency direction first | Headless currently imports `Features`; contracts must be the stable base layer | Move Razor wrappers first |
| 4 | Move `StorefrontV2Default` to V2 instead of adding more shared defaults | Auto-decided | Host owns routes | `/api/product-selection-preview` is V2/BFF-specific and should not live in shared Headless | Keep V2 defaults in shared descriptors |
| 5 | Use label descriptors for copy rather than hardcoding English in Headless | Auto-decided | Storefront owns final UX/copy | V2 can keep English defaults, but generated/custom storefronts need their own labels and localization | Keep shared package as the copy owner |
| 6 | Move account route interpretation to host boundary gradually | Auto-decided | Avoid account page sprawl and preserve WASM behavior | `AccountNavigation` is already host-driven, but `AccountApp` still parses V2 account routes | Add more shared account route constants |
| 7 | Treat class bags as compatibility visual schemas, not stable Headless contracts | Auto-decided | Headless should expose state/actions, not layout regions | Class bags define V2 DOM regions even without Tailwind values | Stabilize current class bags as public API |
| 8 | Upgrade browser local API errors to structured semantics | Auto-decided | Components should not force UI copy parsing | Browser flows need status/code/trace/field errors like Runtime, not only `Message` | Keep message-only local API result |

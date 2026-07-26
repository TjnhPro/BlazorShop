# Storefront Components Compatibility Removal

Muc tieu: ket thuc vung compatibility `BlazorShop.Storefront.Components/Features` va khoa lai kien truc Storefront reusable theo dung ownership hien tai:

```text
Storefront.Client
  -> generated Storefront API contracts/transport

Storefront.Runtime
  -> server/BFF-only generated client registration, facade, error/result primitives

Storefront.Components
  -> browser-safe Contracts, Headless state/behavior, Browser same-origin primitives

Storefront.V2 / Starter / Storefront.{Name}
  -> markup, CSS, layout, page composition, copywriting, route interpretation
```

Plan nay chi la refactor co hoc va cleanup boundary. Khong doi behavior ecom, khong rewrite checkout/cart/account flow, khong them API moi.

## Current Verified Context

- [x] `BlazorShop.Storefront.Components` van dung `Microsoft.NET.Sdk.Razor`.
- [x] `BlazorShop.Storefront.Components/Features` van co 14 Razor wrappers:
  - `Account/AccountAddressBook.razor`
  - `Account/AccountApp.razor`
  - `Account/AccountChangePasswordForm.razor`
  - `Account/AccountNavigation.razor`
  - `Account/AccountOrderDetail.razor`
  - `Account/AccountOrderList.razor`
  - `Account/AccountProfileEditor.razor`
  - `Cart/CartView.razor`
  - `Catalog/ProductSummaryCard.razor`
  - `Catalog/ProductSummaryGrid.razor`
  - `Checkout/CheckoutShell.razor`
  - `Deals/DealsBlock.razor`
  - `Product/ProductGallery.razor`
  - `Product/ProductPurchasePanel.razor`
- [x] V2 van import `BlazorShop.Storefront.Components.Features.*` trong `BlazorShop.Storefront.V2/_Imports.razor`.
- [x] V2 van dung shared wrappers tai:
  - `Pages/Hybrid/Commerce/CartPage.razor` voi `CartView`
  - `Pages/Hybrid/Commerce/CheckoutPage.razor` voi `CheckoutShell`
  - `Pages/WasmHost/Account/AccountHostPage.razor` voi `AccountApp`
- [x] V2 da co visual replacements cho catalog/product/deals:
  - `Components/Catalog/StorefrontProductSummaryCard.razor`
  - `Components/Catalog/StorefrontProductSummaryGrid.razor`
  - `Components/Catalog/StorefrontDealsSection.razor`
  - `Components/Product/StorefrontProductGallery.razor`
  - `Components/Product/StorefrontProductPurchasePanel.razor`
- [x] `Headless` khong con dependency nguoc vao `Features` cho product contracts.
- [x] V2-specific product selection endpoint default da nam trong V2:
  - `Components/Product/StorefrontProductPurchaseActionOptions.cs`
- [x] Visual class bags van nam trong `Components/Headless`:
  - `StorefrontCartViewClasses`
  - `StorefrontCheckoutViewClasses`
  - `AccountNavigationClasses`
  - `StorefrontAccountFormClasses`
  - `StorefrontAccountAddressBookClasses`
  - `StorefrontAccountOrderListClasses`
  - `StorefrontAccountOrderDetailClasses`
  - `StorefrontAccountShellClasses`
- [x] `Storefront.Components/Browser/StorefrontAntiforgeryTokenReader.cs` dang import static JS tu `_content/BlazorShop.Storefront.Components/js/storefrontWasmInterop.js`, nen viec doi khoi Razor SDK phai xu ly static asset ownership truoc.
- [x] Runtime da co `AddStorefrontPlatformRuntime` va cac `AddStorefront{Capability}Runtime`, nhung van giu compatibility wrappers:
  - `AddStorefrontGeneratedClients`
  - `AddStorefrontServerGeneratedClients`

## Non Goals

- [x] Khong doi Commerce Node Storefront API contract.
- [x] Khong doi checkout/cart/order/payment business behavior.
- [x] Khong doi UI/UX cua Storefront V2 ngoai cac thay doi bat buoc do move component.
- [x] Khong them shared visual design system moi.
- [x] Khong bat Starter hoac generated storefront dung V2 markup.
- [x] Khong copy DTO tu backend vao Components.

## Phase SCR0 - Baseline and Safety Lock

- [x] Chay `git status --short` va ghi lai file dang dirty de khong ghi de thay doi khong lien quan.
  - 2026-07-26: dirty baseline chi co plan file nay dang untracked trong scope yeu cau.
- [x] Lap inventory cuoi cung cua shared Razor wrappers:

```powershell
Get-ChildItem BlazorShop.PresentationV2/BlazorShop.Storefront.Components/Features -Recurse -Filter *.razor
```

- [x] Lap inventory consumer con lai:

```powershell
rg -n "BlazorShop\.Storefront\.Components\.Features|<CartView|<CheckoutShell|<AccountApp|ProductSummaryCard|ProductSummaryGrid|DealsBlock|ProductGallery|ProductPurchasePanel" BlazorShop.PresentationV2 BlazorShop.Tests.V2 docs -g "!bin" -g "!obj"
```

- [x] Lap inventory visual class bags:

```powershell
rg -n "StorefrontCartViewClasses|StorefrontCheckoutViewClasses|AccountNavigationClasses|StorefrontAccount.*Classes" BlazorShop.PresentationV2/BlazorShop.Storefront.Components BlazorShop.PresentationV2/BlazorShop.Storefront.V2 BlazorShop.Tests.V2
```

- [x] Confirm `Storefront.V2` va `Storefront.Starter` dang dung `AddStorefrontPlatformRuntime`, khong con dung compatibility wrappers trong host code.
- [x] Confirm tests nao dang doc path `Components/Features/*` de thay bang guardrail moi, khong chi sua cho compile.
  - 2026-07-26: tests doc path cu gom `StorefrontComponentsHeadlessPresentationRefactorTests`, `StorefrontWasmRuntimeFoundationTests`, `StorefrontCommerceFlowCutoverTests`, `StorefrontBrandingMarkupTests`, `StorefrontPageCompositionGuardrailTests`, `StorefrontStarterFoundationBoundaryTests`.
- [x] Tao branch/commit checkpoint neu dang co thay doi lon tu phase truoc.

Exit criteria:

- [x] Co danh sach consumer chinh xac truoc khi move/delete.
- [x] Khong co file ngoai scope bi dua vao phase nay.

## Phase SCR1 - Move Cart Visual Wrapper Ownership to V2

Muc tieu: `CartView` khong con nam trong shared `Components/Features`, nhung cart browser behavior hien tai khong doi.

- [x] Tao V2-owned interactive component:
  - 2026-07-26 final path: `BlazorShop.Storefront.WASM/Components/Cart/StorefrontCartView.razor`
  - Historical SCR1 staging path was `BlazorShop.Storefront.V2/Components/Cart/StorefrontCartView.razor`; SCR12 moved it to WASM for browser hydration.
- [x] Chuyen visual markup tu shared `Features/Cart/CartView.razor` sang V2 component.
- [x] Doi namespace/import sang V2 component namespace.
- [x] Giu shared dependencies chi o muc:
  - `Contracts/Cart`
  - `Headless/Cart`
  - `Browser`
- [x] Chuyen class/style defaults tu `StorefrontCartViewOptions` sang V2-local type, khong dung `StorefrontCartViewClasses` tu Headless nua.
- [x] Giu same-origin browser action behavior qua `StorefrontLocalApiClient`.
- [x] Update `Pages/Hybrid/Commerce/CartPage.razor` dung `<StorefrontCartView ...>`.
- [x] Remove cart-related `Features` import khoi `_Imports.razor` neu khong con consumer.
- [x] Update tests dang doc `Features/Cart/CartView.razor` sang:
  - doc V2 component de guard markup V2;
  - doc Headless/Browser contracts de guard reusable behavior;
  - them test khong con shared cart Razor wrapper.

Exit criteria:

- [x] `CartPage.razor` render bang V2 host/WASM-owned cart component.
- [x] Khong con active V2 source reference `BlazorShop.Storefront.Components.Features.Cart`.
- [x] Cart flow tests/guardrails van cover quantity update, remove item, clear cart, checkout action, error state.
  - 2026-07-26: `dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.V2/BlazorShop.Storefront.V2.csproj` pass.
  - 2026-07-26: focused `StorefrontWasmRuntimeFoundationTests|StorefrontComponentsHeadlessPresentationRefactorTests|StorefrontCommerceFlowCutoverTests` pass 53/53.
  - 2026-07-26 SCR12 final state: interactive cart root moved from server V2 assembly to `BlazorShop.Storefront.WASM/Components/Cart` so WebAssembly hydration can find the root component; V2 still owns route/BFF/static asset composition.

## Phase SCR2 - Move Checkout Visual Wrapper Ownership to V2

Muc tieu: `CheckoutShell` thuoc V2, shared package chi con state/action/label contracts.

- [x] Tao V2-owned interactive component:
  - 2026-07-26 final path: `BlazorShop.Storefront.WASM/Components/Checkout/StorefrontCheckoutShell.razor`
  - Historical SCR2 staging path was `BlazorShop.Storefront.V2/Components/Checkout/StorefrontCheckoutShell.razor`; SCR12 moved it to WASM for browser hydration.
- [x] Chuyen markup tu `Features/Checkout/CheckoutShell.razor` sang V2.
- [x] Chuyen checkout visual option/class records tu Headless sang V2-local options.
- [x] Giu shared contracts/headless:
  - checkout step state
  - action descriptors
  - validation state
  - browser-safe labels
- [x] Update `Pages/Hybrid/Commerce/CheckoutPage.razor` dung `<StorefrontCheckoutShell ...>`.
- [x] Remove checkout `Features` import khoi `_Imports.razor` neu khong con consumer.
- [x] Update tests dang doc `Features/Checkout/CheckoutShell.razor`.
- [x] Bao dam checkout khong doi endpoint:
  - `checkout/start`
  - `checkout/review`
  - `checkout/place-order`
- [x] Bao dam test van guard:
  - address step
  - shipping method step
  - payment method COD
  - review
  - place order real browser flow

Exit criteria:

- [x] `CheckoutPage.razor` render bang V2 host/WASM-owned checkout component.
- [x] Khong con active V2 source reference `BlazorShop.Storefront.Components.Features.Checkout`.
- [x] Shared Components khong con checkout Razor visual wrapper.
  - 2026-07-26: shared wrapper con ton tai tam thoi den SCR6, nhung khong con duoc V2 source/test positive path consume.
  - 2026-07-26: `dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.V2/BlazorShop.Storefront.V2.csproj` pass.
  - 2026-07-26: focused `StorefrontWasmRuntimeFoundationTests|StorefrontComponentsHeadlessPresentationRefactorTests|StorefrontCommerceFlowCutoverTests` pass 53/53.
  - 2026-07-26 SCR12 final state: interactive checkout root moved from server V2 assembly to `BlazorShop.Storefront.WASM/Components/Checkout`; V2 host continues to own checkout route, BFF endpoints, antiforgery, and static script ownership.

## Phase SCR3 - Move Account Visual Wrapper Ownership to V2

Muc tieu: account khong con la shared visual app. V2 so huu page composition, routes va copy.

- [x] Tao V2-owned interactive folder:
  - 2026-07-26 final path: `BlazorShop.Storefront.WASM/Components/Account/`
  - Historical SCR3 staging path was `BlazorShop.Storefront.V2/Components/Account/`; SCR12 moved it to WASM for browser hydration.
- [x] Chuyen cac visual components sau vao V2:
  - `StorefrontAccountApp.razor`
  - `StorefrontAccountNavigation.razor`
  - `StorefrontAccountProfileEditor.razor`
  - `StorefrontAccountChangePasswordForm.razor`
  - `StorefrontAccountAddressBook.razor`
  - `StorefrontAccountOrderList.razor`
  - `StorefrontAccountOrderDetail.razor`
- [x] Doi route interpretation tu shared component sang V2:
  - `/account/profile`
  - `/account/addresses`
  - `/account/orders`
  - `/account/change-password`
- [x] Giu shared account contracts/headless chi cho:
  - account labels
  - account route descriptor/parser primitives neu can
  - form state/validation state
  - action descriptors
  - data hooks
- [x] Doi visual class bags account thanh V2-local options.
- [x] Update `Pages/WasmHost/Account/AccountHostPage.razor` dung `<StorefrontAccountApp ...>`.
- [x] Remove direct `@using BlazorShop.Storefront.Components.Features.Account`.
- [x] Update tests dang doc `Features/Account/*`.
- [x] Giu account page surface gon:
  - khong tao them page account moi;
  - chi move nhung page/section hien co;
  - account page composition van co the chua profile, addresses, orders, password trong cung host.

Exit criteria:

- [x] Account WASM host render bang V2 host/WASM-owned account components.
- [x] Khong con active V2 source reference `BlazorShop.Storefront.Components.Features.Account`.
- [x] Shared Components khong con account Razor visual wrappers.
  - 2026-07-26: `dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.V2/BlazorShop.Storefront.V2.csproj` pass.
  - 2026-07-26: focused `StorefrontWasmRuntimeFoundationTests|StorefrontComponentsHeadlessPresentationRefactorTests|StorefrontCommerceFlowCutoverTests|StorefrontBrandingMarkupTests` pass 68/68.
  - 2026-07-26 SCR12 final state: interactive account root moved from server V2 assembly to `BlazorShop.Storefront.WASM/Components/Account`; this fixes browser hydration while preserving V2 account route/security/BFF ownership.

## Phase SCR4 - Delete Orphan Catalog, Product, and Deals Visual Wrappers

Muc tieu: xoa cac wrapper shared da duoc thay bang V2-local component, tranh de Starter/AI Generator nham chung la contract visual bat buoc.

- [x] Confirm runtime V2 source da dung:
  - `StorefrontProductSummaryCard`
  - `StorefrontProductSummaryGrid`
  - `StorefrontDealsSection`
  - `StorefrontProductGallery`
  - `StorefrontProductPurchasePanel`
- [x] Confirm khong co Starter/generated source import `BlazorShop.Storefront.Components.Features.*`.
- [x] Xoa shared Razor wrappers neu khong con consumer:
  - `Features/Catalog/ProductSummaryCard.razor`
  - `Features/Catalog/ProductSummaryGrid.razor`
  - `Features/Deals/DealsBlock.razor`
  - `Features/Product/ProductGallery.razor`
  - `Features/Product/ProductPurchasePanel.razor`
- [x] Update tests dang doc cac file tren:
  - guard V2 owns markup/CSS;
  - guard shared Contracts/Headless contain only data/state/action;
  - guard no shared visual wrappers.
- [x] Update `QA-StorefrontV2.todo.md` cac dong cu cu the noi product/deals/catalog con compose shared `Features`.
- [x] Neu historical plan da co `[x]` ve old Features, khong rewrite lich su; them note vao plan/QA active noi no da duoc retired.

Exit criteria:

- [x] Catalog/product/deals visual wrappers khong con nam trong shared package.
- [x] V2 product detail, catalog grid, deals, new releases van build va render tu V2-local components.
  - 2026-07-26: Components build pass; Storefront V2 build pass.
  - 2026-07-26: focused `StorefrontComponentsHeadlessPresentationRefactorTests|StorefrontBrandingMarkupTests|StorefrontStarterFoundationBoundaryTests` pass 65/65.

## Phase SCR5 - Remove Visual Class Bags from Headless

Muc tieu: `Headless` khong con quy dinh DOM regions/layout/class schema.

- [x] Chuyen cac record class bag ra V2-local options/contracts:
  - `StorefrontCartViewClasses`
  - `StorefrontCheckoutViewClasses`
  - `AccountNavigationClasses`
  - `StorefrontAccountFormClasses`
  - `StorefrontAccountAddressBookClasses`
  - `StorefrontAccountOrderListClasses`
  - `StorefrontAccountOrderDetailClasses`
  - `StorefrontAccountShellClasses`
- [x] Dat namespace moi trong V2, vi du:
  - `BlazorShop.Storefront.V2.Components.Cart`
  - `BlazorShop.Storefront.V2.Components.Checkout`
  - `BlazorShop.Storefront.V2.Components.Account`
- [x] Sua V2 option files dung V2-local class records.
- [x] Giu trong `Headless` chi cac object sau:
  - `ViewState`
  - `SelectionState`
  - `ActionDescriptor`
  - `RouteDescriptor`
  - `ValidationState`
  - semantic/data hooks khong co class/layout property
- [x] Update guardrail test `StorefrontComponentsHeadlessPresentationRefactorTests` de fail neu `Headless` co record/type ket thuc bang `Classes` hoac property ten `Class`.
- [x] Neu co truong semantic nhu `CssClass` that su can cho browser interoperability, phai doi ten hoac dua ve host-specific model.

Exit criteria:

- [x] `rg -n "Classes|CssClass|class=\"" BlazorShop.PresentationV2/BlazorShop.Storefront.Components/Headless` khong tra ve visual class bag moi.
- [x] V2 van co class options local va build pass.
  - 2026-07-26: Components build pass; Storefront V2 build pass.
  - 2026-07-26: focused `StorefrontComponentsHeadlessPresentationRefactorTests|StorefrontWasmRuntimeFoundationTests|StorefrontCommerceFlowCutoverTests` pass 53/53.

## Phase SCR6 - Delete Features Folder and Remove Feature Imports

Muc tieu: khong con `Features` folder trong `Storefront.Components`, khong con empty compatibility README.

- [x] Xoa tat ca file con lai duoi:

```text
BlazorShop.PresentationV2/BlazorShop.Storefront.Components/Features
```

- [x] Xoa folder `Features`, khong de lai folder rong hoac README compatibility.
- [x] Sua `BlazorShop.Storefront.V2/_Imports.razor` de chi import:
  - `Contracts`
  - `Headless`
  - `Browser`
  - V2-local component namespaces
- [x] Scan va sua active code:

```powershell
rg -n "BlazorShop\.Storefront\.Components\.Features|Components/Features|Features\\\\" BlazorShop.PresentationV2 BlazorShop.Tests.V2 docs/architecture docs/agents docs/visual-reverse-engineering-skill -g "!bin" -g "!obj"
```

- [x] Phan biet `Features/feature-manifest.json` cua Starter/StorefrontBuilder voi retired `Storefront.Components/Features`; khong xoa Starter feature manifest.
- [x] Update architecture docs:
  - `docs/architecture/05-project-and-folder-guide.md`
  - `docs/architecture/10-v2-contract-ownership.md`
  - ADR nao dang noi `Features/*` la active compatibility surface
- [x] Update StorefrontBuilder docs de noi generated storefronts consume `Contracts`, `Headless`, `Browser`; khong co compatibility exception mac dinh cho `Components.Features`.

Exit criteria:

- [x] `Test-Path BlazorShop.PresentationV2/BlazorShop.Storefront.Components/Features` tra ve `False`.
- [x] Khong con active source/test/doc architecture nao coi `Components/Features` la current active surface.
  - 2026-07-26: Components build pass; Storefront V2 build pass.
  - 2026-07-26: focused `StorefrontComponentsHeadlessPresentationRefactorTests|StorefrontPageCompositionGuardrailTests|StorefrontIndependenceBoundaryTests|StorefrontStarterFoundationBoundaryTests` pass 116/116.

## Phase SCR7 - Browser Static Asset and Razor SDK Cleanup

Muc tieu: `Storefront.Components` khong con la Razor component library neu khong con `.razor`, nhung khong pha JS interop/browser action.

- [x] Inventory `.razor` con lai:

```powershell
Get-ChildItem BlazorShop.PresentationV2/BlazorShop.Storefront.Components -Recurse -Filter *.razor
```

- [x] Xoa `BlazorShop.Storefront.Components/_Imports.razor` neu khong con Razor file.
- [x] Xu ly `storefrontWasmInterop.js` truoc khi doi SDK:
  - [x] Kiem tra consumer cua `_content/BlazorShop.Storefront.Components/js/storefrontWasmInterop.js`.
  - [x] Chon ownership moi cho static JS:
    - preferred: move concrete JS asset va module path vao host browser/WASM/V2 layer;
    - acceptable: keep shared browser primitive with host-provided configurable module path;
    - khong acceptable: doi SDK lam mat static web asset nhung giu hardcoded `_content/BlazorShop.Storefront.Components`.
  - [x] Update `StorefrontAntiforgeryTokenReader` de khong hardcode shared package static asset neu package khong con phat static web assets.
  - [x] Update `CartView` moved-to-V2 JS import path neu component van can direct JS module.
- [x] Doi project SDK:

```xml
<Project Sdk="Microsoft.NET.Sdk">
```

- [x] Doi package reference toi dependency toi thieu:
  - giu `Microsoft.JSInterop` neu can;
  - chi giu `Microsoft.AspNetCore.Components.Web` neu co type dung that su va build can.
- [x] Update package description neu can:

```text
Browser-safe Storefront contracts, headless interaction state, and browser primitives.
```

- [x] Them/update test guard:
  - no `.razor` under `BlazorShop.Storefront.Components`;
  - csproj no longer uses `Microsoft.NET.Sdk.Razor`;
  - no shared static web asset hardcoded path unless ownership is intentionally documented and tested.

Exit criteria:

- [x] Components project build pass voi `Microsoft.NET.Sdk`.
- [x] Browser/WASM flows van lay anti-forgery token dung.
- [x] Khong con hidden dependency vao Razor static web assets cua Components.
  - 2026-07-26: Components, Storefront WASM, va Storefront V2 build pass.
  - 2026-07-26: focused `StorefrontComponentsHeadlessPresentationRefactorTests|StorefrontWasmRuntimeFoundationTests|StorefrontSharedPlatformPackageContractTests|StorefrontPageCompositionGuardrailTests` pass 111/111.

## Phase SCR8 - Runtime Compatibility API Cleanup

Muc tieu: runtime DI surface chi con ten chinh thuc, hoac co deprecation policy ro rang neu can giu compatibility.

- [x] Confirm package chua public external consumer. Neu chua public:
  - [x] Remove `AddStorefrontGeneratedClients`.
  - [x] Remove `AddStorefrontServerGeneratedClients`.
  - [x] Keep `AddStorefrontPlatformRuntime`.
  - [x] Keep `AddStorefront{Capability}Runtime`.
- [x] Neu package da public hoac can migration window:
  - [x] Mark `[Obsolete("Use AddStorefrontPlatformRuntime or AddStorefront{Capability}Runtime. This compatibility alias will be removed in <version>.")]`.
  - [x] Them TODO removal version vao docs.
  - 2026-07-26: N/A; alias da remove vi repo khong co external public consumer.
- [x] Update tests:
  - old tests khong assert wrappers la preferred path;
  - neu wrappers removed, test fail khi wrappers con ton tai;
  - neu wrappers obsolete, test assert obsolete message va removal version.
- [x] Update docs:
  - `docs/architecture/05-project-and-folder-guide.md`
  - `docs/architecture/10-v2-contract-ownership.md`
  - `docs/agents/storefront-builder.md`
  - `docs/visual-reverse-engineering-skill/README.md`

Exit criteria:

- [x] V2 va Starter van dung `AddStorefrontPlatformRuntime`.
- [x] Runtime API khong con ten moi/ten cu song song ma khong co policy.
  - 2026-07-26: Runtime, Storefront V2, va Starter build pass.
  - 2026-07-26: focused `StorefrontSharedPlatformPackageContractTests|StorefrontRuntimeResultPrimitiveTests` pass 35/35.

## Phase SCR9 - Test Suite Refactor and Guardrail Upgrade

Muc tieu: tests khong con verify temporary implementation, ma verify final architecture.

- [x] Refactor cac test dang read file `Components/Features/*`:
  - `StorefrontComponentsHeadlessPresentationRefactorTests`
  - `StorefrontWasmRuntimeFoundationTests`
  - `StorefrontCommerceFlowCutoverTests`
  - `StorefrontBrandingMarkupTests`
  - `StorefrontPageCompositionGuardrailTests`
  - `StorefrontStarterFoundationBoundaryTests`
  - `StorefrontSharedPlatformPackageContractTests`
- [x] Them guardrails moi:
  - [x] `StorefrontComponents_HasNoFeaturesFolder`
  - [x] `StorefrontComponents_HasNoRazorFiles`
  - [x] `StorefrontComponents_UsesClassLibrarySdk`
  - [x] `StorefrontComponents_HeadlessHasNoVisualClassBags`
  - [x] `StorefrontV2_DoesNotImportComponentsFeatures`
  - [x] `StarterAndGeneratedTemplates_DoNotImportComponentsFeatures`
  - [x] `Runtime_UsesOfficialCapabilityRegistrationSurface`
- [x] Giu guardrails hien co ve:
  - no `Web.SharedV2` business dependency in Storefront source;
  - no backend/core references in Client/Runtime/Components;
  - Runtime server-only boundary;
  - generated storefront isolation.
- [x] Sua tests nao dang xem shared visual wrapper la positive requirement thanh negative requirement.

Exit criteria:

- [x] Tests mo ta dung final architecture, khong con compatibility expectation cu.
- [x] Test fail neu ai do them lai shared visual Razor wrapper vao `Storefront.Components`.
  - 2026-07-26: focused `StorefrontComponentsHeadlessPresentationRefactorTests|StorefrontSharedPlatformPackageContractTests|StorefrontPageCompositionGuardrailTests|StorefrontIndependenceBoundaryTests|StorefrontBrandingMarkupTests|StorefrontWasmRuntimeFoundationTests` pass 144/144.

## Phase SCR10 - Documentation and QA Checklist Cleanup

Muc tieu: docs hien tai khong con noi `Features` la active migration surface sau khi phase xong.

- [x] Update `docs/architecture/05-project-and-folder-guide.md`:
  - `Storefront.Components` chi gom `Contracts`, `Headless`, `Browser`;
  - visual templates thuoc V2/Starter/generated/custom storefront.
- [x] Update `docs/architecture/10-v2-contract-ownership.md`:
  - remove "Features may keep temporary compatibility wrappers";
  - add guardrail final state.
- [x] Update ADRs lien quan:
  - `2026-07-24-storefront-starter-foundation.md`
  - `2026-07-24-headless-storefront-platform-foundation.md`
- [x] Update `docs/agents/storefront-builder.md` va `docs/visual-reverse-engineering-skill/reference.md`.
- [x] Update `docs/refactor-control-Commerce-storefront/QA-StorefrontV2.todo.md`:
  - replace old entries noi V2 compose `Features`;
  - add final QA rows for V2 host/WASM-owned cart/checkout/account markup and V2-owned product/deals markup;
  - add Playwright release rows cho browser flows.
- [x] Khong sua lich su plan da completed neu chi la record cu; neu can, them "retired by Storefront Components Compatibility Removal" note thay vi rewrite qua khu.

Exit criteria:

- [x] Current architecture docs va QA checklist khong con conflict voi final boundary.
- [x] Historical docs neu nhac `Features` thi ro la old migration state.
  - 2026-07-26: focused docs/static guardrails `StorefrontComponentsHeadlessPresentationRefactorTests|StorefrontSharedPlatformPackageContractTests|StorefrontPageCompositionGuardrailTests` pass 92/92.

## Phase SCR11 - Focused Build and Unit/Architecture Verification

Chay tu hep den rong de bat loi ngay tai boundary vua doi.

- [x] Build Components:

```powershell
dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.Components/BlazorShop.Storefront.Components.csproj
```

- [x] Build Storefront WASM:

```powershell
dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.WASM/BlazorShop.Storefront.WASM.csproj
```

- [x] Build Storefront V2:

```powershell
dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.V2/BlazorShop.Storefront.V2.csproj
```

- [x] Build Starter:

```powershell
dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/BlazorShop.Storefront.Starter.csproj
```

- [x] Run focused architecture/component/runtime tests:

```powershell
dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --filter "FullyQualifiedName~StorefrontComponentsHeadlessPresentationRefactorTests|FullyQualifiedName~StorefrontWasmRuntimeFoundationTests|FullyQualifiedName~StorefrontSharedPlatformPackageContractTests|FullyQualifiedName~StorefrontStarterFoundationBoundaryTests"
```

- [x] Run StorefrontBuilder isolation gate:

```powershell
./scripts/qa/run-storefront-builder-isolation-gate.ps1
```

- [x] Run storefront foundation isolation gate neu co lien quan package boundary:

```powershell
./scripts/qa/run-storefront-foundation-isolation-gate.ps1
```

Exit criteria:

- [x] Focused builds pass.
- [x] Focused tests pass.
- [x] Package/isolation gates pass hoac co fail ro do fixture/env khong lien quan va duoc ghi lai.
  - 2026-07-26: Components, WASM, Storefront V2, and Starter builds pass. Initial parallel WASM build hit an obj file lock while V2 was building; rerun sequential WASM build passed.
  - 2026-07-26: focused `StorefrontComponentsHeadlessPresentationRefactorTests|StorefrontWasmRuntimeFoundationTests|StorefrontSharedPlatformPackageContractTests|StorefrontStarterFoundationBoundaryTests` pass 89/89.
  - 2026-07-26: `run-storefront-builder-isolation-gate.ps1` pass for `BlazorShop.Storefront.GeneratedProof`.
  - 2026-07-26: `run-storefront-foundation-isolation-gate.ps1` pass.

## Phase SCR12 - Browser Playwright Release Verification

Muc tieu: chung minh viec move component khong lam hong flow that cua Storefront V2.

- [x] Start local V2 runtime:

```powershell
./scripts/run-v2-local.ps1 -StopExisting
```

- [x] Dung Playwright browser test cac flow V2 that:
  - home page render;
  - product list/category/search render;
  - product detail gallery 1x1 va purchase panel;
  - add to cart;
  - cart quantity update/remove/clear;
  - checkout COD place order that;
  - account login;
  - account profile;
  - account addresses;
  - account orders;
  - order detail;
  - account password/change-password route neu fixture cho phep;
  - recovery UI neu dang nam trong release checklist.
  - 2026-07-26: `run-storefront-order-email-e2e.ps1 -Headless` pass, proving login, add-to-cart, checkout COD, order confirmation, account orders, order detail, receipt, order email, queued retry behavior, and same-origin network guard.
  - 2026-07-26: `run-storefront-registration-policy-e2e.ps1 -Headless` pass.
  - 2026-07-26: `.gstack/qa-reports/scr12-browser-qa/result.json` pass for `/`, `/category/apparel`, `/search?q=qa`, `/product/qa-simple-product-100`, add-to-cart, `/my-cart` quantity update, remove, clear, `/checkout`, static JS, console, and direct Commerce Node browser request guard.
- [x] Capture screenshots/trace cho loi UI neu co.
  - 2026-07-26: initial account hydration failure screenshot captured at `.gstack/qa-reports/order-email-e2e/failure.png`; fixed by moving interactive root components into WASM. Passing screenshots are under `.gstack/qa-reports/order-email-e2e/` and `.gstack/qa-reports/scr12-browser-qa/cart-clear-empty.png`.
- [x] Verify browser console khong co JS module import error lien quan `storefrontWasmInterop.js`.
  - 2026-07-26: `.gstack/qa-reports/scr12-browser-qa/result.json` has empty console list and `storefrontWasmInterop.js` returned HTTP 200 `text/javascript`.
- [x] Verify anti-forgery/browser same-origin actions van submit duoc.
  - 2026-07-26: cart clear/update/remove and checkout/order placement submitted through same-origin BFF with antiforgery-backed browser actions; no direct browser requests to Commerce Node were recorded.
- [x] Update release QA checklist voi ket qua pass/fail that.

Exit criteria:

- [x] Storefront V2 browser e2e pass cho cart/account/checkout.
- [x] Place order bang COD pass trong store test.
- [x] Khong co console error do missing static asset sau khi Components doi SDK.

## Final Cleanup Checklist

- [x] `BlazorShop.Storefront.Components/Features` khong ton tai.
- [x] `BlazorShop.Storefront.Components` khong co `.razor`.
- [x] `BlazorShop.Storefront.Components.csproj` dung `Microsoft.NET.Sdk`.
- [x] `BlazorShop.Storefront.Components` chi co cac thu muc stable:
  - `Contracts`
  - `Headless`
  - `Browser`
- [x] `Headless` khong co visual class bags.
- [x] Shared package khong hardcode V2 route default.
- [x] Shared package khong own final English storefront copy.
- [x] Shared package khong own layout/DOM visual composition.
- [x] V2 host/WASM client owns V2 markup/CSS/layout.
- [x] Starter owns neutral markup/CSS/layout.
- [x] StorefrontBuilder/generated storefronts own generated markup/CSS/layout.
- [x] Runtime registration surface da duoc cleanup hoac obsolete policy ro rang.
- [x] Architecture docs, StorefrontBuilder docs, QA checklist da dong bo.
- [x] No active source import:

```text
BlazorShop.Storefront.Components.Features
```

## Production Definition of Done

Client:

- [x] Canonical OpenAPI remains at `contracts/storefront/storefront.openapi.json`.
- [x] Deterministic regeneration gate still passes.
- [x] No backend source dependency.
- [x] Independent package consumer proof still passes.

Runtime:

- [x] Typed generated-client factories remain.
- [x] Typed envelope mapping remains.
- [x] Caller cancellation behavior remains correct.
- [x] Capability-scoped registration remains.
- [x] Server-only boundary remains guarded.
- [x] No obsolete compatibility aliases, or aliases have explicit removal policy.

Components:

- [x] Contracts only.
- [x] Headless behavior/state only.
- [x] Browser same-origin primitives only.
- [x] No `Features` folder.
- [x] No shared visual Razor wrappers.
- [x] No visual class bags.
- [x] No V2 route defaults.
- [x] No final copy/design/layout ownership.

Stores:

- [x] V2 host/WASM client owns V2 markup/CSS/layout.
- [x] Starter owns neutral markup/CSS/layout.
- [x] `Storefront.{Name}` owns generated/custom markup/CSS/layout.

## Risk Controls

- [x] Move one capability at a time: cart, checkout, account, then orphan catalog/product/deals deletion.
- [x] Do not delete shared wrapper before V2 consumer is cut over and tests pass.
- [x] Do not switch Components SDK until static JS asset ownership is proven.
- [x] Do not keep compatibility aliases silently; remove or mark obsolete with version.
- [x] Do not treat old docs as source of truth when they conflict with architecture docs.
- [x] Browser QA must include real cart/account/checkout flows, not only smoke tests.

## Decision Audit Trail

| # | Phase | Decision | Classification | Principle | Rationale | Rejected |
|---|-------|----------|----------------|-----------|-----------|----------|
| 1 | Scope | Complete final compatibility removal as one tracked plan with phased implementation | Auto-decided | Boundary clarity | Current code still has temporary shared visual wrappers and tests that treat them as active surface. A dedicated cleanup plan prevents half-migration. | Leave `Features` as permanent compatibility area |
| 2 | Components | Move visual wrappers to V2 instead of making shared wrappers more configurable | Auto-decided | Ownership | Store-specific markup/CSS/layout must belong to V2, Starter, or generated stores. Making shared wrappers configurable would still lock DOM regions. | Add more class bags/render fragments to shared components |
| 3 | Headless | Remove visual class bags from Headless | Auto-decided | Maintainability | Class bags define DOM/layout shape and make Headless not truly headless. V2-local options can preserve existing design without leaking schema. | Keep `*Classes` records in shared Headless |
| 4 | SDK cleanup | Switch Components away from Razor SDK only after JS static asset ownership is resolved | Auto-decided | Production safety | `storefrontWasmInterop.js` is currently served via RCL static web assets. Changing SDK first can break browser actions at runtime. | Change SDK first and hope build/browser catches it later |
| 5 | Runtime cleanup | Remove or obsolete compatibility registration aliases | Auto-decided | API clarity | Runtime already has official platform/capability registration. Keeping old all-in aliases without policy preserves ambiguity. | Keep both naming generations indefinitely |
| 6 | QA | Use focused builds, architecture tests, package gates, and Playwright real flows | Auto-decided | Verification quality | This phase touches browser component ownership and package boundaries; smoke tests alone will not catch missing JS imports, cart action regressions, or checkout placement failures. | Only run build or route smoke tests |

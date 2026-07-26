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

- [x] Tao V2-owned component:
  - `BlazorShop.Storefront.V2/Components/Cart/StorefrontCartView.razor`
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

- [x] `CartPage.razor` render bang V2-owned cart component.
- [x] Khong con active V2 source reference `BlazorShop.Storefront.Components.Features.Cart`.
- [x] Cart flow tests/guardrails van cover quantity update, remove item, clear cart, checkout action, error state.
  - 2026-07-26: `dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.V2/BlazorShop.Storefront.V2.csproj` pass.
  - 2026-07-26: focused `StorefrontWasmRuntimeFoundationTests|StorefrontComponentsHeadlessPresentationRefactorTests|StorefrontCommerceFlowCutoverTests` pass 53/53.

## Phase SCR2 - Move Checkout Visual Wrapper Ownership to V2

Muc tieu: `CheckoutShell` thuoc V2, shared package chi con state/action/label contracts.

- [x] Tao V2-owned component:
  - `BlazorShop.Storefront.V2/Components/Checkout/StorefrontCheckoutShell.razor`
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

- [x] `CheckoutPage.razor` render bang V2-owned checkout component.
- [x] Khong con active V2 source reference `BlazorShop.Storefront.Components.Features.Checkout`.
- [x] Shared Components khong con checkout Razor visual wrapper.
  - 2026-07-26: shared wrapper con ton tai tam thoi den SCR6, nhung khong con duoc V2 source/test positive path consume.
  - 2026-07-26: `dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.V2/BlazorShop.Storefront.V2.csproj` pass.
  - 2026-07-26: focused `StorefrontWasmRuntimeFoundationTests|StorefrontComponentsHeadlessPresentationRefactorTests|StorefrontCommerceFlowCutoverTests` pass 53/53.

## Phase SCR3 - Move Account Visual Wrapper Ownership to V2

Muc tieu: account khong con la shared visual app. V2 so huu page composition, routes va copy.

- [ ] Tao V2-owned folder:
  - `BlazorShop.Storefront.V2/Components/Account/`
- [ ] Chuyen cac visual components sau vao V2:
  - `StorefrontAccountApp.razor`
  - `StorefrontAccountNavigation.razor`
  - `StorefrontAccountProfileEditor.razor`
  - `StorefrontAccountChangePasswordForm.razor`
  - `StorefrontAccountAddressBook.razor`
  - `StorefrontAccountOrderList.razor`
  - `StorefrontAccountOrderDetail.razor`
- [ ] Doi route interpretation tu shared component sang V2:
  - `/account/profile`
  - `/account/addresses`
  - `/account/orders`
  - `/account/change-password`
- [ ] Giu shared account contracts/headless chi cho:
  - account labels
  - account route descriptor/parser primitives neu can
  - form state/validation state
  - action descriptors
  - data hooks
- [ ] Doi visual class bags account thanh V2-local options.
- [ ] Update `Pages/WasmHost/Account/AccountHostPage.razor` dung `<StorefrontAccountApp ...>`.
- [ ] Remove direct `@using BlazorShop.Storefront.Components.Features.Account`.
- [ ] Update tests dang doc `Features/Account/*`.
- [ ] Giu account page surface gon:
  - khong tao them page account moi;
  - chi move nhung page/section hien co;
  - account page composition van co the chua profile, addresses, orders, password trong cung host.

Exit criteria:

- [ ] Account WASM host render bang V2-owned account components.
- [ ] Khong con active V2 source reference `BlazorShop.Storefront.Components.Features.Account`.
- [ ] Shared Components khong con account Razor visual wrappers.

## Phase SCR4 - Delete Orphan Catalog, Product, and Deals Visual Wrappers

Muc tieu: xoa cac wrapper shared da duoc thay bang V2-local component, tranh de Starter/AI Generator nham chung la contract visual bat buoc.

- [ ] Confirm runtime V2 source da dung:
  - `StorefrontProductSummaryCard`
  - `StorefrontProductSummaryGrid`
  - `StorefrontDealsSection`
  - `StorefrontProductGallery`
  - `StorefrontProductPurchasePanel`
- [ ] Confirm khong co Starter/generated source import `BlazorShop.Storefront.Components.Features.*`.
- [ ] Xoa shared Razor wrappers neu khong con consumer:
  - `Features/Catalog/ProductSummaryCard.razor`
  - `Features/Catalog/ProductSummaryGrid.razor`
  - `Features/Deals/DealsBlock.razor`
  - `Features/Product/ProductGallery.razor`
  - `Features/Product/ProductPurchasePanel.razor`
- [ ] Update tests dang doc cac file tren:
  - guard V2 owns markup/CSS;
  - guard shared Contracts/Headless contain only data/state/action;
  - guard no shared visual wrappers.
- [ ] Update `QA-StorefrontV2.todo.md` cac dong cu cu the noi product/deals/catalog con compose shared `Features`.
- [ ] Neu historical plan da co `[x]` ve old Features, khong rewrite lich su; them note vao plan/QA active noi no da duoc retired.

Exit criteria:

- [ ] Catalog/product/deals visual wrappers khong con nam trong shared package.
- [ ] V2 product detail, catalog grid, deals, new releases van build va render tu V2-local components.

## Phase SCR5 - Remove Visual Class Bags from Headless

Muc tieu: `Headless` khong con quy dinh DOM regions/layout/class schema.

- [ ] Chuyen cac record class bag ra V2-local options/contracts:
  - `StorefrontCartViewClasses`
  - `StorefrontCheckoutViewClasses`
  - `AccountNavigationClasses`
  - `StorefrontAccountFormClasses`
  - `StorefrontAccountAddressBookClasses`
  - `StorefrontAccountOrderListClasses`
  - `StorefrontAccountOrderDetailClasses`
  - `StorefrontAccountShellClasses`
- [ ] Dat namespace moi trong V2, vi du:
  - `BlazorShop.Storefront.V2.Components.Cart`
  - `BlazorShop.Storefront.V2.Components.Checkout`
  - `BlazorShop.Storefront.V2.Components.Account`
- [ ] Sua V2 option files dung V2-local class records.
- [ ] Giu trong `Headless` chi cac object sau:
  - `ViewState`
  - `SelectionState`
  - `ActionDescriptor`
  - `RouteDescriptor`
  - `ValidationState`
  - semantic/data hooks khong co class/layout property
- [ ] Update guardrail test `StorefrontComponentsHeadlessPresentationRefactorTests` de fail neu `Headless` co record/type ket thuc bang `Classes` hoac property ten `Class`.
- [ ] Neu co truong semantic nhu `CssClass` that su can cho browser interoperability, phai doi ten hoac dua ve host-specific model.

Exit criteria:

- [ ] `rg -n "Classes|CssClass|class=\"" BlazorShop.PresentationV2/BlazorShop.Storefront.Components/Headless` khong tra ve visual class bag moi.
- [ ] V2 van co class options local va build pass.

## Phase SCR6 - Delete Features Folder and Remove Feature Imports

Muc tieu: khong con `Features` folder trong `Storefront.Components`, khong con empty compatibility README.

- [ ] Xoa tat ca file con lai duoi:

```text
BlazorShop.PresentationV2/BlazorShop.Storefront.Components/Features
```

- [ ] Xoa folder `Features`, khong de lai folder rong hoac README compatibility.
- [ ] Sua `BlazorShop.Storefront.V2/_Imports.razor` de chi import:
  - `Contracts`
  - `Headless`
  - `Browser`
  - V2-local component namespaces
- [ ] Scan va sua active code:

```powershell
rg -n "BlazorShop\.Storefront\.Components\.Features|Components/Features|Features\\\\" BlazorShop.PresentationV2 BlazorShop.Tests.V2 docs/architecture docs/agents docs/visual-reverse-engineering-skill -g "!bin" -g "!obj"
```

- [ ] Phan biet `Features/feature-manifest.json` cua Starter/StorefrontBuilder voi retired `Storefront.Components/Features`; khong xoa Starter feature manifest.
- [ ] Update architecture docs:
  - `docs/architecture/05-project-and-folder-guide.md`
  - `docs/architecture/10-v2-contract-ownership.md`
  - ADR nao dang noi `Features/*` la active compatibility surface
- [ ] Update StorefrontBuilder docs de noi generated storefronts consume `Contracts`, `Headless`, `Browser`; khong co compatibility exception mac dinh cho `Components.Features`.

Exit criteria:

- [ ] `Test-Path BlazorShop.PresentationV2/BlazorShop.Storefront.Components/Features` tra ve `False`.
- [ ] Khong con active source/test/doc architecture nao coi `Components/Features` la current active surface.

## Phase SCR7 - Browser Static Asset and Razor SDK Cleanup

Muc tieu: `Storefront.Components` khong con la Razor component library neu khong con `.razor`, nhung khong pha JS interop/browser action.

- [ ] Inventory `.razor` con lai:

```powershell
Get-ChildItem BlazorShop.PresentationV2/BlazorShop.Storefront.Components -Recurse -Filter *.razor
```

- [ ] Xoa `BlazorShop.Storefront.Components/_Imports.razor` neu khong con Razor file.
- [ ] Xu ly `storefrontWasmInterop.js` truoc khi doi SDK:
  - [ ] Kiem tra consumer cua `_content/BlazorShop.Storefront.Components/js/storefrontWasmInterop.js`.
  - [ ] Chon ownership moi cho static JS:
    - preferred: move concrete JS asset va module path vao host browser/WASM/V2 layer;
    - acceptable: keep shared browser primitive with host-provided configurable module path;
    - khong acceptable: doi SDK lam mat static web asset nhung giu hardcoded `_content/BlazorShop.Storefront.Components`.
  - [ ] Update `StorefrontAntiforgeryTokenReader` de khong hardcode shared package static asset neu package khong con phat static web assets.
  - [ ] Update `CartView` moved-to-V2 JS import path neu component van can direct JS module.
- [ ] Doi project SDK:

```xml
<Project Sdk="Microsoft.NET.Sdk">
```

- [ ] Doi package reference toi dependency toi thieu:
  - giu `Microsoft.JSInterop` neu can;
  - chi giu `Microsoft.AspNetCore.Components.Web` neu co type dung that su va build can.
- [ ] Update package description neu can:

```text
Browser-safe Storefront contracts, headless interaction state, and browser primitives.
```

- [ ] Them/update test guard:
  - no `.razor` under `BlazorShop.Storefront.Components`;
  - csproj no longer uses `Microsoft.NET.Sdk.Razor`;
  - no shared static web asset hardcoded path unless ownership is intentionally documented and tested.

Exit criteria:

- [ ] Components project build pass voi `Microsoft.NET.Sdk`.
- [ ] Browser/WASM flows van lay anti-forgery token dung.
- [ ] Khong con hidden dependency vao Razor static web assets cua Components.

## Phase SCR8 - Runtime Compatibility API Cleanup

Muc tieu: runtime DI surface chi con ten chinh thuc, hoac co deprecation policy ro rang neu can giu compatibility.

- [ ] Confirm package chua public external consumer. Neu chua public:
  - [ ] Remove `AddStorefrontGeneratedClients`.
  - [ ] Remove `AddStorefrontServerGeneratedClients`.
  - [ ] Keep `AddStorefrontPlatformRuntime`.
  - [ ] Keep `AddStorefront{Capability}Runtime`.
- [ ] Neu package da public hoac can migration window:
  - [ ] Mark `[Obsolete("Use AddStorefrontPlatformRuntime or AddStorefront{Capability}Runtime. This compatibility alias will be removed in <version>.")]`.
  - [ ] Them TODO removal version vao docs.
- [ ] Update tests:
  - old tests khong assert wrappers la preferred path;
  - neu wrappers removed, test fail khi wrappers con ton tai;
  - neu wrappers obsolete, test assert obsolete message va removal version.
- [ ] Update docs:
  - `docs/architecture/05-project-and-folder-guide.md`
  - `docs/architecture/10-v2-contract-ownership.md`
  - `docs/agents/storefront-builder.md`
  - `docs/visual-reverse-engineering-skill/README.md`

Exit criteria:

- [ ] V2 va Starter van dung `AddStorefrontPlatformRuntime`.
- [ ] Runtime API khong con ten moi/ten cu song song ma khong co policy.

## Phase SCR9 - Test Suite Refactor and Guardrail Upgrade

Muc tieu: tests khong con verify temporary implementation, ma verify final architecture.

- [ ] Refactor cac test dang read file `Components/Features/*`:
  - `StorefrontComponentsHeadlessPresentationRefactorTests`
  - `StorefrontWasmRuntimeFoundationTests`
  - `StorefrontCommerceFlowCutoverTests`
  - `StorefrontBrandingMarkupTests`
  - `StorefrontPageCompositionGuardrailTests`
  - `StorefrontStarterFoundationBoundaryTests`
  - `StorefrontSharedPlatformPackageContractTests`
- [ ] Them guardrails moi:
  - `StorefrontComponents_HasNoFeaturesFolder`
  - `StorefrontComponents_HasNoRazorFiles`
  - `StorefrontComponents_UsesClassLibrarySdk`
  - `StorefrontComponents_HeadlessHasNoVisualClassBags`
  - `StorefrontV2_DoesNotImportComponentsFeatures`
  - `StarterAndGeneratedTemplates_DoNotImportComponentsFeatures`
  - `Runtime_UsesOfficialCapabilityRegistrationSurface`
- [ ] Giu guardrails hien co ve:
  - no `Web.SharedV2` business dependency in Storefront source;
  - no backend/core references in Client/Runtime/Components;
  - Runtime server-only boundary;
  - generated storefront isolation.
- [ ] Sua tests nao dang xem shared visual wrapper la positive requirement thanh negative requirement.

Exit criteria:

- [ ] Tests mo ta dung final architecture, khong con compatibility expectation cu.
- [ ] Test fail neu ai do them lai shared visual Razor wrapper vao `Storefront.Components`.

## Phase SCR10 - Documentation and QA Checklist Cleanup

Muc tieu: docs hien tai khong con noi `Features` la active migration surface sau khi phase xong.

- [ ] Update `docs/architecture/05-project-and-folder-guide.md`:
  - `Storefront.Components` chi gom `Contracts`, `Headless`, `Browser`;
  - visual templates thuoc V2/Starter/generated/custom storefront.
- [ ] Update `docs/architecture/10-v2-contract-ownership.md`:
  - remove "Features may keep temporary compatibility wrappers";
  - add guardrail final state.
- [ ] Update ADRs lien quan:
  - `2026-07-24-storefront-starter-foundation.md`
  - `2026-07-24-headless-storefront-platform-foundation.md`
- [ ] Update `docs/agents/storefront-builder.md` va `docs/visual-reverse-engineering-skill/reference.md`.
- [ ] Update `docs/refactor-control-Commerce-storefront/QA-StorefrontV2.todo.md`:
  - replace old entries noi V2 compose `Features`;
  - add final QA rows for V2-owned cart/checkout/account/product/deals markup;
  - add Playwright release rows cho browser flows.
- [ ] Khong sua lich su plan da completed neu chi la record cu; neu can, them "retired by Storefront Components Compatibility Removal" note thay vi rewrite qua khu.

Exit criteria:

- [ ] Current architecture docs va QA checklist khong con conflict voi final boundary.
- [ ] Historical docs neu nhac `Features` thi ro la old migration state.

## Phase SCR11 - Focused Build and Unit/Architecture Verification

Chay tu hep den rong de bat loi ngay tai boundary vua doi.

- [ ] Build Components:

```powershell
dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.Components/BlazorShop.Storefront.Components.csproj
```

- [ ] Build Storefront WASM:

```powershell
dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.WASM/BlazorShop.Storefront.WASM.csproj
```

- [ ] Build Storefront V2:

```powershell
dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.V2/BlazorShop.Storefront.V2.csproj
```

- [ ] Build Starter:

```powershell
dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/BlazorShop.Storefront.Starter.csproj
```

- [ ] Run focused architecture/component/runtime tests:

```powershell
dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --filter "FullyQualifiedName~StorefrontComponentsHeadlessPresentationRefactorTests|FullyQualifiedName~StorefrontWasmRuntimeFoundationTests|FullyQualifiedName~StorefrontSharedPlatformPackageContractTests|FullyQualifiedName~StorefrontStarterFoundationBoundaryTests"
```

- [ ] Run StorefrontBuilder isolation gate:

```powershell
./scripts/qa/run-storefront-builder-isolation-gate.ps1
```

- [ ] Run storefront foundation isolation gate neu co lien quan package boundary:

```powershell
./scripts/qa/run-storefront-foundation-isolation-gate.ps1
```

Exit criteria:

- [ ] Focused builds pass.
- [ ] Focused tests pass.
- [ ] Package/isolation gates pass hoac co fail ro do fixture/env khong lien quan va duoc ghi lai.

## Phase SCR12 - Browser Playwright Release Verification

Muc tieu: chung minh viec move component khong lam hong flow that cua Storefront V2.

- [ ] Start local V2 runtime:

```powershell
./scripts/run-v2-local.ps1 -StopExisting
```

- [ ] Dung Playwright browser test cac flow V2 that:
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
- [ ] Capture screenshots/trace cho loi UI neu co.
- [ ] Verify browser console khong co JS module import error lien quan `storefrontWasmInterop.js`.
- [ ] Verify anti-forgery/browser same-origin actions van submit duoc.
- [ ] Update release QA checklist voi ket qua pass/fail that.

Exit criteria:

- [ ] Storefront V2 browser e2e pass cho cart/account/checkout.
- [ ] Place order bang COD pass trong store test.
- [ ] Khong co console error do missing static asset sau khi Components doi SDK.

## Final Cleanup Checklist

- [ ] `BlazorShop.Storefront.Components/Features` khong ton tai.
- [ ] `BlazorShop.Storefront.Components` khong co `.razor`.
- [ ] `BlazorShop.Storefront.Components.csproj` dung `Microsoft.NET.Sdk`.
- [ ] `BlazorShop.Storefront.Components` chi co cac thu muc stable:
  - `Contracts`
  - `Headless`
  - `Browser`
- [ ] `Headless` khong co visual class bags.
- [ ] Shared package khong hardcode V2 route default.
- [ ] Shared package khong own final English storefront copy.
- [ ] Shared package khong own layout/DOM visual composition.
- [ ] V2 owns V2 markup/CSS/layout.
- [ ] Starter owns neutral markup/CSS/layout.
- [ ] StorefrontBuilder/generated storefronts own generated markup/CSS/layout.
- [ ] Runtime registration surface da duoc cleanup hoac obsolete policy ro rang.
- [ ] Architecture docs, StorefrontBuilder docs, QA checklist da dong bo.
- [ ] No active source import:

```text
BlazorShop.Storefront.Components.Features
```

## Production Definition of Done

Client:

- [ ] Canonical OpenAPI remains at `contracts/storefront/storefront.openapi.json`.
- [ ] Deterministic regeneration gate still passes.
- [ ] No backend source dependency.
- [ ] Independent package consumer proof still passes.

Runtime:

- [ ] Typed generated-client factories remain.
- [ ] Typed envelope mapping remains.
- [ ] Caller cancellation behavior remains correct.
- [ ] Capability-scoped registration remains.
- [ ] Server-only boundary remains guarded.
- [ ] No obsolete compatibility aliases, or aliases have explicit removal policy.

Components:

- [ ] Contracts only.
- [ ] Headless behavior/state only.
- [ ] Browser same-origin primitives only.
- [ ] No `Features` folder.
- [ ] No shared visual Razor wrappers.
- [ ] No visual class bags.
- [ ] No V2 route defaults.
- [ ] No final copy/design/layout ownership.

Stores:

- [ ] V2 owns V2 markup/CSS/layout.
- [ ] Starter owns neutral markup/CSS/layout.
- [ ] `Storefront.{Name}` owns generated/custom markup/CSS/layout.

## Risk Controls

- [ ] Move one capability at a time: cart, checkout, account, then orphan catalog/product/deals deletion.
- [ ] Do not delete shared wrapper before V2 consumer is cut over and tests pass.
- [ ] Do not switch Components SDK until static JS asset ownership is proven.
- [ ] Do not keep compatibility aliases silently; remove or mark obsolete with version.
- [ ] Do not treat old docs as source of truth when they conflict with architecture docs.
- [ ] Browser QA must include real cart/account/checkout flows, not only smoke tests.

## Decision Audit Trail

| # | Phase | Decision | Classification | Principle | Rationale | Rejected |
|---|-------|----------|----------------|-----------|-----------|----------|
| 1 | Scope | Complete final compatibility removal as one tracked plan with phased implementation | Auto-decided | Boundary clarity | Current code still has temporary shared visual wrappers and tests that treat them as active surface. A dedicated cleanup plan prevents half-migration. | Leave `Features` as permanent compatibility area |
| 2 | Components | Move visual wrappers to V2 instead of making shared wrappers more configurable | Auto-decided | Ownership | Store-specific markup/CSS/layout must belong to V2, Starter, or generated stores. Making shared wrappers configurable would still lock DOM regions. | Add more class bags/render fragments to shared components |
| 3 | Headless | Remove visual class bags from Headless | Auto-decided | Maintainability | Class bags define DOM/layout shape and make Headless not truly headless. V2-local options can preserve existing design without leaking schema. | Keep `*Classes` records in shared Headless |
| 4 | SDK cleanup | Switch Components away from Razor SDK only after JS static asset ownership is resolved | Auto-decided | Production safety | `storefrontWasmInterop.js` is currently served via RCL static web assets. Changing SDK first can break browser actions at runtime. | Change SDK first and hope build/browser catches it later |
| 5 | Runtime cleanup | Remove or obsolete compatibility registration aliases | Auto-decided | API clarity | Runtime already has official platform/capability registration. Keeping old all-in aliases without policy preserves ambiguity. | Keep both naming generations indefinitely |
| 6 | QA | Use focused builds, architecture tests, package gates, and Playwright real flows | Auto-decided | Verification quality | This phase touches browser component ownership and package boundaries; smoke tests alone will not catch missing JS imports, cart action regressions, or checkout placement failures. | Only run build or route smoke tests |

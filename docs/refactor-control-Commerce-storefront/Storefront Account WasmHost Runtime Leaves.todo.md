# Storefront Account WasmHost Runtime Leaves

Status: in progress
Track: Phase 3.6 - Account browser runtime leaves
Target area: Storefront V2 / V2.WASM component mode architecture

## Purpose

Move the five account browser-interactive leaf components out of `BlazorShop.Storefront.V2.WASM` into the reusable `BlazorShop.Storefront.Components.WasmHost` layer without changing customer-facing behavior.

This phase is intentionally narrow. It does not redesign account pages, account routing, account API contracts, authentication, checkout, orders, or customer identity. It only moves browser lifecycle and mutation behavior for account leaves to the correct reusable WASM host package while V2 keeps page placement, route composition, final classes, and final copy.

Final architecture target:

```text
BlazorShop.Storefront.Components
  -> browser-safe account contracts, labels, class slot contracts, action contracts

BlazorShop.Storefront.Browser
  -> same-origin BFF browser account controller and browser action primitives

BlazorShop.Storefront.Components.WasmHost
  -> reusable account leaf components that inject IStorefrontBrowserAccountController

BlazorShop.Storefront.V2.WASM
  -> V2 account app shell, V2 account navigation, V2 options, final V2 copy/classes

BlazorShop.Storefront.V2
  -> account host page, @rendermode placement, server route ownership
```

## Autoplan Review Summary

CEO review:

- The scope is valid because V2.WASM still owns reusable account browser runtime behavior after cart/checkout were extracted.
- The phase must not expand into a full account UX redesign or Starter extraction.
- The key product outcome is maintainability and future frontend flexibility, not new account features.

Design review:

- No visible account layout redesign is allowed in this phase.
- V2 must remain the owner of final English copy, final Tailwind class values, route placement, and visual composition.
- Shared WasmHost leaves may keep the current DOM shape as a compatibility implementation, but the host must supply class and label contracts.

Engineering review:

- The five runtime leaves should move to `Components.WasmHost` because they inject browser controllers and own lifecycle/mutation handlers.
- Account class contracts used by moved leaves must move from V2.WASM into `Components/Contracts/Account`; otherwise WasmHost would need an invalid V2.WASM reference.
- `StorefrontAccountApp` and `StorefrontAccountNavigation` should stay in V2.WASM for this phase because they are route/panel composition and render-only navigation, not reusable account runtime leaves.
- Tests must be updated from "V2.WASM owns account runtime" to "WasmHost owns account runtime and V2.WASM owns account composition/options".

DX review:

- The file/folder names must make ownership obvious to later agents.
- The checklist must include guardrails against accidental direct Commerce Node transport, Presentation contracts, or `@rendermode` inside WasmHost.
- Browser QA must use real Playwright flows against account pages and same-origin BFF network behavior, not only smoke tests.

## Current Code Evidence

- [ ] `BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM/Components/Account/StorefrontAccountProfileEditor.razor` currently injects `IStorefrontBrowserAccountController`.
- [ ] `StorefrontAccountProfileEditor.razor` owns `InitializeProfile`, `HydrateProfileAsync`, and `SaveProfileAsync`.
- [ ] `StorefrontAccountProfileEditor.razor` renders semantic hook `data-storefront-account-profile`.
- [ ] `BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM/Components/Account/StorefrontAccountChangePasswordForm.razor` currently injects `IStorefrontBrowserAccountController`.
- [ ] `StorefrontAccountChangePasswordForm.razor` owns `InitializePassword` and `ChangePasswordAsync`.
- [ ] `StorefrontAccountChangePasswordForm.razor` renders semantic hook `data-storefront-account-password`.
- [ ] `BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM/Components/Account/StorefrontAccountAddressBook.razor` currently injects `IStorefrontBrowserAccountController`.
- [ ] `StorefrontAccountAddressBook.razor` owns `InitializeAddresses`, `HydrateAddressesAsync`, `CreateAddressAsync`, `UpdateAddressAsync`, `DeleteAddressAsync`, and `SetDefaultAddressAsync`.
- [ ] `StorefrontAccountAddressBook.razor` renders semantic hook `data-storefront-account-addresses`.
- [ ] `BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM/Components/Account/StorefrontAccountOrderList.razor` currently injects `IStorefrontBrowserAccountController`.
- [ ] `StorefrontAccountOrderList.razor` owns `InitializeOrders` and `HydrateOrdersAsync`.
- [ ] `StorefrontAccountOrderList.razor` renders semantic hook `data-storefront-account-orders`.
- [ ] `BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM/Components/Account/StorefrontAccountOrderDetail.razor` currently injects `IStorefrontBrowserAccountController`.
- [ ] `StorefrontAccountOrderDetail.razor` owns `InitializeOrderDetail` and `HydrateOrderDetailAsync`.
- [ ] `StorefrontAccountOrderDetail.razor` renders semantic hook `data-storefront-account-order-detail`.
- [ ] `BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM/Components/Account/StorefrontAccountApp.razor` currently stays in V2.WASM and owns account route/panel composition.
- [ ] `StorefrontAccountApp.razor` does not directly inject `IStorefrontBrowserAccountController`.
- [ ] `StorefrontAccountApp.razor` currently contains final V2 copy such as `Customer account`, success messages, and unknown-section text.
- [ ] `BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM/Components/Account/StorefrontAccountNavigation.razor` currently stays in V2.WASM and owns render-only account navigation.
- [ ] `StorefrontAccountNavigation.razor` renders `data-storefront-account-navigation` and `data-storefront-account-nav-item`.
- [ ] `BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM/Components/Account/StorefrontAccountViewClasses.cs` currently defines both leaf class contracts and V2 shell/navigation class contracts.
- [ ] `BlazorShop.PresentationV2/BlazorShop.Storefront.Components/Contracts/Account/AccountLabels.cs` exists but is too narrow for the five leaves.
- [ ] `BlazorShop.PresentationV2/BlazorShop.Storefront.Components/Contracts/Account/AccountRouteDescriptor.cs` already owns account route descriptors and route parsing contracts.
- [ ] `BlazorShop.PresentationV2/BlazorShop.Storefront.Components/Headless/Account/StorefrontAccountFormBehavior.cs` already owns account action descriptors.
- [ ] `BlazorShop.PresentationV2/BlazorShop.Storefront.Components.WasmHost/BlazorShop.Storefront.Components.WasmHost.csproj` references only `BlazorShop.Storefront.Components` and `BlazorShop.Storefront.Browser`.
- [ ] `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/Hybrid/Account/AccountHostPage.razor` renders `StorefrontAccountApp` with `@rendermode="InteractiveWebAssembly"`.
- [ ] `BlazorShop.PresentationV2/BlazorShop.Storefront.Starter.WASM/Components/Account/StorefrontAccountApp.razor` also contains account browser logic, but Starter.WASM is out of scope for this phase.

## Final Ownership Target

`BlazorShop.Storefront.Components`:

- [ ] Owns account leaf class contracts used by reusable WasmHost components:
  - [ ] `StorefrontAccountFormClasses`
  - [ ] `StorefrontAccountAddressBookClasses`
  - [ ] `StorefrontAccountOrderListClasses`
  - [ ] `StorefrontAccountOrderDetailClasses`
- [ ] Owns account leaf label contracts:
  - [ ] `StorefrontAccountProfileLabels`
  - [ ] `StorefrontAccountPasswordLabels`
  - [ ] `StorefrontAccountAddressBookLabels`
  - [ ] `StorefrontAccountOrderListLabels`
  - [ ] `StorefrontAccountOrderDetailLabels`
- [ ] Keeps existing account route/action/headless contracts.
- [ ] Does not reference Browser, Runtime, Client, V2, V2.WASM, Starter, Starter.WASM, backend/core/API projects, Control Plane projects, or `Web.SharedV2`.
- [ ] Does not own final V2 copy.
- [ ] Does not own final V2 Tailwind class values.

`BlazorShop.Storefront.Components.WasmHost`:

- [ ] Owns reusable account leaf components:
  - [ ] `Components/Account/StorefrontAccountProfileEditor.razor`
  - [ ] `Components/Account/StorefrontAccountChangePasswordForm.razor`
  - [ ] `Components/Account/StorefrontAccountAddressBook.razor`
  - [ ] `Components/Account/StorefrontAccountOrderList.razor`
  - [ ] `Components/Account/StorefrontAccountOrderDetail.razor`
- [ ] Injects `IStorefrontBrowserAccountController` inside these leaves.
- [ ] Accepts leaf classes, labels, route/action descriptors, and callbacks as parameters from host composition.
- [ ] Renders existing semantic `data-storefront-account-*` hooks.
- [ ] Does not declare `@rendermode`, `InteractiveWebAssembly`, `InteractiveServer`, or `InteractiveAuto`.
- [ ] Does not reference V2, V2.WASM, Starter, Starter.WASM, Presentation, Runtime, Client, backend/core/API projects, Control Plane projects, or `Web.SharedV2`.
- [ ] Does not make direct `HttpClient`, direct `/api/storefront/*`, direct Commerce Node, or localhost backend calls.

`BlazorShop.Storefront.V2.WASM`:

- [ ] Keeps `StorefrontAccountApp.razor`.
- [ ] Keeps `StorefrontAccountNavigation.razor`.
- [ ] Keeps `StorefrontAccountViewOptions.cs`.
- [ ] Keeps `AccountNavigationClasses` and `StorefrontAccountShellClasses` unless a later phase moves the shell/navigation composition.
- [ ] Supplies final V2 classes and labels to the moved WasmHost leaf components.
- [ ] Does not inject `IStorefrontBrowserAccountController` in account leaf implementations.
- [ ] Does not own account leaf lifecycle/mutation methods.
- [ ] Does not implement Presentation `IStorefront*Client` contracts.

`BlazorShop.Storefront.V2`:

- [ ] Keeps `AccountHostPage.razor` route placement.
- [ ] Keeps `@rendermode="InteractiveWebAssembly"` on the V2 page/composition boundary.
- [ ] Keeps current account route URLs and navigation behavior.
- [ ] Does not move account render-mode directives into WasmHost.

## Hard Scope Lock

Allowed production areas:

- [ ] `BlazorShop.PresentationV2/BlazorShop.Storefront.Components/Contracts/Account/`
- [ ] `BlazorShop.PresentationV2/BlazorShop.Storefront.Components/Headless/Account/` only if existing descriptors need non-visual contract adjustment.
- [ ] `BlazorShop.PresentationV2/BlazorShop.Storefront.Components.WasmHost/Components/Account/`
- [ ] `BlazorShop.PresentationV2/BlazorShop.Storefront.Components.WasmHost/_Imports.razor`
- [ ] `BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM/Components/Account/`
- [ ] `BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM/_Imports.razor` only if namespace imports require cleanup.
- [ ] `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/_Imports.razor` only if namespace imports require cleanup.
- [ ] Architecture docs and QA checklist files listed in this plan.
- [ ] Focused tests under `BlazorShop.Tests.V2`.

Explicit non-goals:

- [ ] Do not redesign the account UX.
- [ ] Do not change account URLs.
- [ ] Do not change `AccountHostPage.razor` route ownership.
- [ ] Do not change account authentication/session behavior.
- [ ] Do not change BFF endpoint routes.
- [ ] Do not change the `IStorefrontBrowserAccountController` public behavior unless required by compile errors and approved by existing patterns.
- [ ] Do not change Commerce Node account APIs.
- [ ] Do not change Storefront Client/Runtime account contracts.
- [ ] Do not move `StorefrontAccountApp.razor` in this phase.
- [ ] Do not move `StorefrontAccountNavigation.razor` in this phase.
- [ ] Do not move header account menu, logout form, or account links in this phase.
- [ ] Do not change cart/checkout extraction completed in Phase 3.5.
- [ ] Do not update Starter.WASM account implementation in this phase.
- [ ] Do not create new shared visual wrapper packages.
- [ ] Do not recreate `BlazorShop.Storefront.Components/Features`.

## Phase 0 - Preflight And Baseline

Goal: confirm current state before moving files.

- [x] Read `AGENTS.md`.
- [x] Read `docs/architecture/README.md`.
- [x] Read `docs/architecture/03-runtime-boundaries.md`.
- [x] Read `docs/architecture/05-project-and-folder-guide.md`.
- [x] Read `docs/architecture/10-v2-contract-ownership.md`.
- [x] Read `BlazorShop.PresentationV2/COMPONENT-MODES.md`.
- [x] Read completed related plan `docs/refactor-control-Commerce-storefront/Storefront Cart Checkout WasmHost Extraction.todo.md`.
- [x] Run source search:
  - [x] `rg -n "StorefrontAccount(ProfileEditor|ChangePasswordForm|AddressBook|OrderList|OrderDetail)" BlazorShop.PresentationV2 BlazorShop.Tests.V2`
  - [x] `rg -n "IStorefrontBrowserAccountController" BlazorShop.PresentationV2 BlazorShop.Tests.V2`
  - [x] `rg -n "StorefrontAccount(FormClasses|AddressBookClasses|OrderListClasses|OrderDetailClasses|ShellClasses|NavigationClasses)" BlazorShop.PresentationV2 BlazorShop.Tests.V2`
  - [x] `rg -n "data-storefront-account" BlazorShop.PresentationV2 BlazorShop.Tests.V2`
- [x] Record current consumer list before editing. Evidence: V2.WASM `StorefrontAccountApp` is the sole V2 composition consumer; the server `AccountHostPage` supplies its route/render-mode boundary. Existing source-assertion tests are the only other consumers that require updates.
- [x] Confirm working tree status with `git status --short`.
- [x] If unrelated user changes exist, do not revert them; work around them or stop only if they block the phase. Evidence: the supplied plan was the only pre-existing untracked file.

Stop conditions:

- [x] Stop if the current account components have already moved and this plan no longer matches the code. The five leaves were still V2.WASM implementations at baseline.
- [x] Stop if V2.WASM no longer references `StorefrontAccountApp` or account route composition. It remains the V2 account panel orchestrator.
- [x] Stop if WasmHost project gained invalid references before this phase starts. Its project references remain Components and Browser only.

## Phase 1 - Account Contracts Split

Goal: make the shared account contract layer usable by WasmHost without referencing V2.WASM.

Tasks:

- [ ] Create or update account leaf class contract files under `BlazorShop.Storefront.Components/Contracts/Account/`.
- [ ] Move `StorefrontAccountFormClasses` from V2.WASM class file into Components contracts.
- [ ] Move `StorefrontAccountAddressBookClasses` from V2.WASM class file into Components contracts.
- [ ] Move `StorefrontAccountOrderListClasses` from V2.WASM class file into Components contracts.
- [ ] Move `StorefrontAccountOrderDetailClasses` from V2.WASM class file into Components contracts.
- [ ] Keep `AccountNavigationClasses` in V2.WASM for now.
- [ ] Keep `StorefrontAccountShellClasses` in V2.WASM for now.
- [ ] Preserve property names and defaults unless a compile error requires a mechanical namespace adjustment.
- [ ] Avoid changing class slot semantics.

Label contract tasks:

- [ ] Review existing `AccountLabels.cs` and decide whether to keep it as navigation/common labels or replace it with a compatible aggregate.
- [ ] Do not create two competing account label systems with overlapping purpose.
- [ ] Add `StorefrontAccountProfileLabels`.
- [ ] Add `StorefrontAccountPasswordLabels`.
- [ ] Add `StorefrontAccountAddressBookLabels`.
- [ ] Add `StorefrontAccountOrderListLabels`.
- [ ] Add `StorefrontAccountOrderDetailLabels`.
- [ ] Add `StorefrontAccountAppLabels` only if `StorefrontAccountApp` copy needs a typed host-owned label bag in this phase.
- [ ] Ensure moved WasmHost leaves receive labels through parameters, not hardcoded final V2 copy.
- [ ] Keep default label values neutral/fallback only; V2 final copy must be supplied by `StorefrontAccountViewOptions`.

Validation:

- [ ] `BlazorShop.Storefront.Components` still has no reference to Browser, Presentation, Runtime, Client, V2, V2.WASM, Starter, backend/core/API, Control Plane, or `Web.SharedV2`.
- [ ] No account leaf class contracts remain exclusively defined in V2.WASM.
- [ ] V2.WASM still owns navigation/shell class contracts.

## Phase 2 - Move Profile Leaf To WasmHost

Goal: extract profile editor runtime behavior first as the smallest account mutation leaf.

Tasks:

- [ ] Create `BlazorShop.PresentationV2/BlazorShop.Storefront.Components.WasmHost/Components/Account/StorefrontAccountProfileEditor.razor`.
- [ ] Move current markup and behavior from V2.WASM profile editor mechanically.
- [ ] Keep `IStorefrontBrowserAccountController` injection inside the WasmHost leaf.
- [ ] Keep `InitializeProfile`, `HydrateProfileAsync`, and `SaveProfileAsync` behavior equivalent.
- [ ] Preserve `data-storefront-account-profile`.
- [ ] Preserve form validation behavior.
- [ ] Replace hardcoded leaf copy with `StorefrontAccountProfileLabels` parameters.
- [ ] Replace V2.WASM class type reference with `StorefrontAccountFormClasses` from Components contracts.
- [ ] Add optional host callback/event if `StorefrontAccountApp` currently depends on save success.
- [ ] Remove or convert the old V2.WASM profile file into a thin wrapper only if needed by existing namespaces.
- [ ] Prefer no V2.WASM leaf wrapper if `StorefrontAccountApp` can import the WasmHost component cleanly.

Validation:

- [ ] V2.WASM no longer owns profile lifecycle methods.
- [ ] The moved file has no `@rendermode`.
- [ ] The moved file has no V2 namespace import.
- [ ] The moved file has no direct `/api/*` or `HttpClient`.

## Phase 3 - Move Password Leaf To WasmHost

Goal: extract password change runtime behavior without changing password policy or account security.

Tasks:

- [ ] Create `BlazorShop.PresentationV2/BlazorShop.Storefront.Components.WasmHost/Components/Account/StorefrontAccountChangePasswordForm.razor`.
- [ ] Move current markup and behavior from V2.WASM password form mechanically.
- [ ] Keep `IStorefrontBrowserAccountController` injection inside the WasmHost leaf.
- [ ] Keep `InitializePassword` and `ChangePasswordAsync` behavior equivalent.
- [ ] Preserve `data-storefront-account-password`.
- [ ] Preserve validation and error behavior.
- [ ] Replace hardcoded leaf copy with `StorefrontAccountPasswordLabels` parameters.
- [ ] Use `StorefrontAccountFormClasses` from Components contracts.
- [ ] Preserve password field names and form model semantics.
- [ ] Do not change password hashing, password policy, recovery policy, or backend commands.

Validation:

- [ ] V2.WASM no longer owns password lifecycle methods.
- [ ] Password change still goes through same-origin browser controller.
- [ ] No direct Commerce Node or direct account API route is introduced in the component.

## Phase 4 - Move Address Book Leaf To WasmHost

Goal: extract the account address browser CRUD leaf while keeping address behavior unchanged.

Tasks:

- [ ] Create `BlazorShop.PresentationV2/BlazorShop.Storefront.Components.WasmHost/Components/Account/StorefrontAccountAddressBook.razor`.
- [ ] Move current markup and behavior from V2.WASM address book mechanically.
- [ ] Keep `IStorefrontBrowserAccountController` injection inside the WasmHost leaf.
- [ ] Keep `InitializeAddresses` behavior equivalent.
- [ ] Keep `HydrateAddressesAsync` behavior equivalent.
- [ ] Keep `CreateAddressAsync` behavior equivalent.
- [ ] Keep `UpdateAddressAsync` behavior equivalent.
- [ ] Keep `DeleteAddressAsync` behavior equivalent.
- [ ] Keep `SetDefaultAddressAsync` behavior equivalent.
- [ ] Preserve `data-storefront-account-addresses`.
- [ ] Preserve validation, editing, cancel, delete, and default-address behavior.
- [ ] Replace hardcoded leaf copy with `StorefrontAccountAddressBookLabels` parameters.
- [ ] Use `StorefrontAccountAddressBookClasses` from Components contracts.
- [ ] Do not change address DTOs, country/state lookup, order snapshot behavior, or account backend validation.

Validation:

- [ ] V2.WASM no longer owns address lifecycle methods.
- [ ] Create/update/delete/default operations still call the browser account controller.
- [ ] Address book retains current semantic hooks for Playwright.

## Phase 5 - Move Order List Leaf To WasmHost

Goal: extract account order list browser loading behavior while preserving list routes and display.

Tasks:

- [ ] Create `BlazorShop.PresentationV2/BlazorShop.Storefront.Components.WasmHost/Components/Account/StorefrontAccountOrderList.razor`.
- [ ] Move current markup and behavior from V2.WASM order list mechanically.
- [ ] Keep `IStorefrontBrowserAccountController` injection inside the WasmHost leaf.
- [ ] Keep `InitializeOrders` and `HydrateOrdersAsync` behavior equivalent.
- [ ] Preserve `data-storefront-account-orders`.
- [ ] Preserve paging/list display behavior if present.
- [ ] Preserve order reference link generation through host-provided route/action descriptors.
- [ ] Replace hardcoded leaf copy with `StorefrontAccountOrderListLabels` parameters.
- [ ] Use `StorefrontAccountOrderListClasses` from Components contracts.
- [ ] Do not change customer order API shape.
- [ ] Do not change guest order access behavior.

Validation:

- [ ] V2.WASM no longer owns order list lifecycle methods.
- [ ] Order list links still resolve to the same V2 account order detail route.
- [ ] The moved leaf does not hardcode V2 route assumptions that should come from descriptors.

## Phase 6 - Move Order Detail Leaf To WasmHost

Goal: extract account order detail browser loading behavior while preserving order visibility rules.

Tasks:

- [ ] Create `BlazorShop.PresentationV2/BlazorShop.Storefront.Components.WasmHost/Components/Account/StorefrontAccountOrderDetail.razor`.
- [ ] Move current markup and behavior from V2.WASM order detail mechanically.
- [ ] Keep `IStorefrontBrowserAccountController` injection inside the WasmHost leaf.
- [ ] Keep `InitializeOrderDetail` and `HydrateOrderDetailAsync` behavior equivalent.
- [ ] Preserve `data-storefront-account-order-detail`.
- [ ] Preserve order item, address, total, payment status, shipment status, and tracking display behavior as currently implemented.
- [ ] Replace hardcoded leaf copy with `StorefrontAccountOrderDetailLabels` parameters.
- [ ] Use `StorefrontAccountOrderDetailClasses` from Components contracts.
- [ ] Do not change order authorization, order query service, guest order access, receipt, retry payment, reorder, return request, or downloadable item hooks.

Validation:

- [ ] V2.WASM no longer owns order detail lifecycle methods.
- [ ] Order detail still only displays orders authorized for the current customer/session.
- [ ] Existing tests that use `nameof(StorefrontAccountOrderDetail.OrderReference)` still compile after namespace updates.

## Phase 7 - V2.WASM Account Composition And Options

Goal: reconnect V2 account composition to the moved WasmHost leaves and keep V2 ownership of final visual values.

Tasks:

- [ ] Update `StorefrontAccountApp.razor` to use the WasmHost leaf components.
- [ ] Keep `StorefrontAccountApp.razor` in `BlazorShop.Storefront.V2.WASM`.
- [ ] Keep `StorefrontAccountNavigation.razor` in `BlazorShop.Storefront.V2.WASM`.
- [ ] Update `StorefrontAccountViewOptions.cs` to expose final V2 labels for:
  - [ ] profile editor;
  - [ ] password form;
  - [ ] address book;
  - [ ] order list;
  - [ ] order detail;
  - [ ] account app shell only if needed.
- [ ] Keep final V2 Tailwind class values in `StorefrontAccountViewOptions.cs`.
- [ ] Keep `AccountNavigationClasses` in V2.WASM.
- [ ] Keep `StorefrontAccountShellClasses` in V2.WASM.
- [ ] Remove leaf class definitions from V2.WASM once all callers use Components contracts.
- [ ] If `StorefrontAccountViewClasses.cs` remains, shrink it to only V2-owned shell/navigation class definitions.
- [ ] If a V2.WASM `_Imports.razor` namespace becomes ambiguous, update imports explicitly.
- [ ] Ensure `AccountHostPage.razor` does not need behavior changes beyond namespace import updates.

Copy ownership tasks:

- [ ] Move leaf success/loading/error/empty labels into `StorefrontAccountViewOptions`.
- [ ] Keep `Customer account` and account app route titles in V2.WASM options or V2.WASM app shell.
- [ ] Avoid final user-facing copy inside `Components.WasmHost`.
- [ ] Allow technical fallback labels only when a host fails to provide labels.

Validation:

- [ ] Account routes render the same sections as before.
- [ ] `StorefrontAccountApp` remains the V2 route/panel orchestrator.
- [ ] `StorefrontAccountNavigation` remains render-only.
- [ ] No V2.WASM account leaf injects `IStorefrontBrowserAccountController`.

## Phase 8 - WasmHost Imports And Boundary Guardrails

Goal: make account leaves compile inside WasmHost and lock the project boundary.

Tasks:

- [ ] Update `BlazorShop.Storefront.Components.WasmHost/_Imports.razor` with only required account imports:
  - [ ] `@using BlazorShop.Storefront.Browser.Account`
  - [ ] `@using BlazorShop.Storefront.Components.Contracts.Account`
  - [ ] `@using BlazorShop.Storefront.Components.Headless.Account`
  - [ ] `@using Microsoft.AspNetCore.Components.Forms`
  - [ ] `@using Microsoft.AspNetCore.Components.Rendering` if required by moved components.
- [ ] Do not add V2/V2.WASM imports to WasmHost.
- [ ] Do not add Presentation imports to WasmHost.
- [ ] Do not add Runtime/Client imports to WasmHost.
- [ ] Do not add backend/core/API imports to WasmHost.
- [ ] Do not add new project references to WasmHost except if a compile-time evidence proves an existing approved browser-safe reference is missing.

Guardrail checks:

- [ ] `rg -n "BlazorShop.Storefront.V2" BlazorShop.PresentationV2/BlazorShop.Storefront.Components.WasmHost`
- [ ] `rg -n "BlazorShop.Storefront.Presentation" BlazorShop.PresentationV2/BlazorShop.Storefront.Components.WasmHost`
- [ ] `rg -n "BlazorShop.Storefront.Runtime|BlazorShop.Storefront.Client" BlazorShop.PresentationV2/BlazorShop.Storefront.Components.WasmHost`
- [ ] `rg -n "BlazorShop.(Domain|Application|Infrastructure)|ControlPlane|CommerceNode|Web.SharedV2" BlazorShop.PresentationV2/BlazorShop.Storefront.Components.WasmHost`
- [ ] `rg -n "@rendermode|InteractiveWebAssembly|InteractiveServer|InteractiveAuto" BlazorShop.PresentationV2/BlazorShop.Storefront.Components.WasmHost`
- [ ] `rg -n "HttpClient|api/storefront|localhost|CommerceNodeBaseUrl" BlazorShop.PresentationV2/BlazorShop.Storefront.Components.WasmHost`

Expected result:

- [ ] All guardrail searches return no invalid production references.
- [ ] Any false positive is documented in the implementation notes with a file/line reason.

## Phase 9 - Update Architecture Tests

Goal: adjust tests so they prove the new ownership instead of preserving old V2.WASM behavior.

Focused test update targets:

- [ ] `StorefrontComponentsHeadlessPresentationRefactorTests.cs`
  - [ ] Stop expecting account leaf runtime implementation under V2.WASM.
  - [ ] Assert account leaves exist under Components.WasmHost.
  - [ ] Assert account leaves inject Browser controller only from WasmHost.
  - [ ] Assert Components contracts own account leaf class/label contracts.
- [ ] `StorefrontV2WASMRuntimeFoundationTests.cs`
  - [ ] Update account file path assertions to current ownership.
  - [ ] Keep assertions that V2.WASM owns AccountApp and AccountNavigation.
  - [ ] Add assertion that V2.WASM account leaves do not inject `IStorefrontBrowserAccountController`.
- [ ] `StorefrontCommerceFlowCutoverTests.cs`
  - [ ] Update order list/detail file paths and namespace references.
  - [ ] Preserve business assertions for account order flows.
- [ ] `StorefrontRequiredVisualContractsHardeningTests.cs`
  - [ ] Keep root AccountApp visual contract assertions in V2.WASM.
  - [ ] Add WasmHost leaf semantic hook assertions.
- [ ] `StorefrontBrandingMarkupTests.cs`
  - [ ] Update `nameof(StorefrontAccountOrderDetail.OrderReference)` namespace import if needed.
  - [ ] Keep branding/markup intent unchanged.
- [ ] `StorefrontPageCompositionGuardrailTests.cs`
  - [ ] Keep `AccountHostPage.razor` route and render-mode ownership assertions.
- [ ] `StorefrontRenderModeOwnershipTests.cs`
  - [ ] Assert `@rendermode` remains in V2 page/composition only.
  - [ ] Assert WasmHost account leaves have no render-mode directive.

New/updated guardrail assertions:

- [ ] V2.WASM account leaves no longer exist as runtime implementation files, or are thin wrappers only if necessary.
- [ ] V2.WASM does not contain account lifecycle method names:
  - [ ] `InitializeProfile`
  - [ ] `HydrateProfileAsync`
  - [ ] `SaveProfileAsync`
  - [ ] `InitializePassword`
  - [ ] `ChangePasswordAsync`
  - [ ] `InitializeAddresses`
  - [ ] `HydrateAddressesAsync`
  - [ ] `CreateAddressAsync`
  - [ ] `UpdateAddressAsync`
  - [ ] `DeleteAddressAsync`
  - [ ] `SetDefaultAddressAsync`
  - [ ] `InitializeOrders`
  - [ ] `HydrateOrdersAsync`
  - [ ] `InitializeOrderDetail`
  - [ ] `HydrateOrderDetailAsync`
- [ ] Components.WasmHost contains those account lifecycle method names.
- [ ] Components.WasmHost account files contain `IStorefrontBrowserAccountController`.
- [ ] Components.WasmHost account files contain existing semantic hooks.
- [ ] Components.WasmHost account files do not contain final V2 option class names as dependencies except through contract types.
- [ ] Starter.WASM is explicitly not used as pass/fail ownership source for this phase.

## Phase 10 - Documentation Updates

Goal: keep architecture source-of-truth aligned with the new split.

Update `BlazorShop.PresentationV2/COMPONENT-MODES.md`:

- [ ] Add account leaves to the WasmHost examples beside cart and checkout.
- [ ] Document that AccountApp and AccountNavigation are still V2.WASM composition in this phase.
- [ ] State that WasmHost account leaves use Browser controllers and host-supplied contracts.
- [ ] State that V2 pages own `@rendermode`.

Update `docs/architecture/03-runtime-boundaries.md`:

- [ ] Clarify Storefront Components.WasmHost account leaves may use Browser controllers only.
- [ ] Clarify WasmHost must not call Commerce Node directly.
- [ ] Clarify V2 keeps render-mode placement.

Update `docs/architecture/05-project-and-folder-guide.md`:

- [ ] Add `Components.WasmHost/Components/Account` as current reusable browser account leaf location.
- [ ] Add `Components/Contracts/Account` as current account contract/class/label ownership location.
- [ ] Keep V2.WASM account composition ownership documented.

Update `docs/architecture/10-v2-contract-ownership.md`:

- [ ] Clarify account visual/runtime contracts live in Components contracts and WasmHost, not Presentation transport contracts.
- [ ] Clarify final V2 copy/classes remain V2-owned.

Update `docs/refactor-control-Commerce-storefront/QA-StorefrontV2.todo.md`:

- [ ] Add account Playwright regression section if missing.
- [ ] Include account profile, password, addresses, orders list/detail.
- [ ] Include same-origin BFF network assertions.
- [ ] Include no direct Commerce Node network assertion.

## Phase 11 - Compile Verification

Goal: prove the mechanical move compiles through the correct graph.

Run focused builds:

```powershell
dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.Components/BlazorShop.Storefront.Components.csproj --no-restore
dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.Browser/BlazorShop.Storefront.Browser.csproj --no-restore
dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.Components.WasmHost/BlazorShop.Storefront.Components.WasmHost.csproj --no-restore
dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM/BlazorShop.Storefront.V2.WASM.csproj --no-restore
dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.V2/BlazorShop.Storefront.V2.csproj --no-restore
```

Run full solution build:

```powershell
dotnet build BlazorShop.sln --no-restore
```

Expected result:

- [ ] Components builds independently.
- [ ] Browser builds independently.
- [ ] WasmHost builds without V2 references.
- [ ] V2.WASM builds with moved leaf imports.
- [ ] V2 builds with AccountHostPage render-mode placement unchanged.
- [ ] Full solution build passes.

If build fails:

- [ ] Fix namespace/import issues first.
- [ ] Do not add invalid project references to make compilation pass.
- [ ] Do not move `@rendermode` into WasmHost.
- [ ] Do not duplicate account leaves in both V2.WASM and WasmHost as a long-term workaround.

## Phase 12 - Focused Test Verification

Goal: prove the architecture guardrails and existing account expectations were updated correctly.

Run focused tests with filters that match existing test names:

```powershell
dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~Account"
dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~WasmHost"
dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~ComponentModeDependency"
dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~RenderModeOwnership"
dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~VisualOnlyBoundary"
dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~V2WASMRuntimeFoundation"
dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~CommerceFlowCutover"
dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~RequiredVisualContracts"
dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~BrowserActionBoundary"
dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~StorefrontIndependence"
```

Run full V2 test suite:

```powershell
dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-restore
```

Expected result:

- [ ] Account ownership tests pass.
- [ ] WasmHost boundary tests pass.
- [ ] Render-mode ownership tests pass.
- [ ] Commerce flow cutover tests pass.
- [ ] Required visual contract tests pass.
- [ ] Full V2 tests pass.

If tests fail:

- [ ] Determine whether failure is a stale path assertion or a behavior regression.
- [ ] Fix stale tests only when code behavior is intentionally moved.
- [ ] Fix behavior regression in implementation before continuing.
- [ ] Do not weaken guardrail tests to hide invalid references.

## Phase 13 - Browser Playwright QA

Goal: verify real browser account behavior, not smoke-only rendering.

Runtime setup:

- [ ] Start V2 local stack with `.\scripts\run-v2-local.ps1 -StopExisting`.
- [ ] Confirm storefront URL and configured test store from local script output.
- [ ] Use an existing QA account that is allowed to sign in.
- [ ] Use a dedicated QA address name/phone/postal code marker so cleanup is safe.
- [ ] Use an order fixture already associated with the QA account for order list/detail checks.

Profile flow:

- [ ] Navigate to `/account/profile`.
- [ ] Sign in if redirected.
- [ ] Wait for `data-storefront-account-profile`.
- [ ] Confirm profile fields load.
- [ ] Edit first/last name or a safe profile field.
- [ ] Submit profile update.
- [ ] Assert success state appears.
- [ ] Reload `/account/profile`.
- [ ] Assert saved value persists.
- [ ] Restore original value if the QA account should remain stable.

Password flow:

- [ ] Navigate to `/account/change-password`.
- [ ] Wait for `data-storefront-account-password`.
- [ ] Submit invalid current password and assert validation/error state without logout.
- [ ] If fixture supports password rotation, perform one valid change and immediately change back.
- [ ] If fixture does not support password rotation, document the reason and keep validation coverage only.
- [ ] Do not leave the QA account with an unknown password.

Address flow:

- [ ] Navigate to `/account/addresses`.
- [ ] Wait for `data-storefront-account-addresses`.
- [ ] Create a new QA address with a unique marker.
- [ ] Assert the new address appears.
- [ ] Edit the QA address.
- [ ] Assert the edit appears.
- [ ] Set the QA address as default billing if supported.
- [ ] Set the QA address as default shipping if supported.
- [ ] Assert default markers/state update.
- [ ] Delete the QA address.
- [ ] Assert the deleted marker no longer appears.

Order list flow:

- [ ] Navigate to `/account/orders`.
- [ ] Wait for `data-storefront-account-orders`.
- [ ] Assert order list loads without console/page errors.
- [ ] Assert at least one order row if the QA fixture has orders.
- [ ] If no fixture order exists, create an order through the normal cart/checkout COD flow before rerunning this check.
- [ ] Open an order detail link from the list.

Order detail flow:

- [ ] Wait for `data-storefront-account-order-detail`.
- [ ] Assert public order reference is visible.
- [ ] Assert order items are visible.
- [ ] Assert totals are visible.
- [ ] Assert billing/shipping info renders according to current implementation.
- [ ] Assert payment status renders.
- [ ] Assert shipment/tracking area renders according to current implementation.
- [ ] Assert unauthorized or unknown order reference does not expose another customer's order.

Account navigation flow:

- [ ] Navigate between `/account/profile`, `/account/addresses`, `/account/orders`, and `/account/change-password`.
- [ ] Assert `data-storefront-account-navigation` remains visible.
- [ ] Assert active nav item state updates.
- [ ] Assert browser back/forward keeps correct active panel.
- [ ] Assert unknown account section shows the existing account app unknown-section state without crashing.

Network and runtime assertions:

- [ ] Capture browser console errors and fail on unexpected errors.
- [ ] Capture page errors and fail on any unhandled exception.
- [ ] Assert protected account mutations use same-origin BFF routes.
- [ ] Assert no browser request goes directly to Commerce Node host/port.
- [ ] Assert no browser request goes directly to `api/storefront/stores/{storeKey}` unless routed through allowed same-origin BFF behavior.
- [ ] Assert no unexpected `/_blazor` WebSocket/EventSource dependency is introduced.
- [ ] Assert no duplicate profile/address/password mutation request is sent per single submit.
- [ ] Assert account pages remain usable after WASM hydration.

Responsive checks:

- [ ] Run account profile at desktop viewport.
- [ ] Run account profile at mobile viewport.
- [ ] Run address book at desktop viewport.
- [ ] Run address book at mobile viewport.
- [ ] Run order list/detail at desktop viewport.
- [ ] Run order list/detail at mobile viewport.
- [ ] Assert no obvious overlapping text, broken buttons, or unusable form controls.

Evidence:

- [ ] Save Playwright trace or screenshots for failed cases.
- [ ] Record exact storefront base URL, store key, account fixture, and command used.
- [ ] Update QA checklist status after browser QA.

## Phase 14 - Cleanup And Obsolete Code Removal

Goal: remove temporary duplication and close the architecture gap cleanly.

Tasks:

- [ ] Delete old V2.WASM account leaf implementation files after WasmHost equivalents are active.
- [ ] If thin wrappers are required during migration, remove them before closing the phase unless a test-proven compatibility need remains.
- [ ] Remove obsolete namespace imports from V2.WASM and V2.
- [ ] Remove duplicate labels or class bags from V2.WASM after the contract move.
- [ ] Remove any TODO comments created during this phase unless they point to an approved future phase.
- [ ] Ensure no `Features` folder was recreated.
- [ ] Ensure no Starter files were changed unintentionally.
- [ ] Ensure no generated storefront artifacts were written to source directories.

Search cleanup:

```powershell
rg -n "InitializeProfile|HydrateProfileAsync|SaveProfileAsync|InitializePassword|ChangePasswordAsync|InitializeAddresses|HydrateAddressesAsync|CreateAddressAsync|UpdateAddressAsync|DeleteAddressAsync|SetDefaultAddressAsync|InitializeOrders|HydrateOrdersAsync|InitializeOrderDetail|HydrateOrderDetailAsync" BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM
rg -n "IStorefrontBrowserAccountController" BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM
rg -n "StorefrontAccountApiClient|IStorefront.*Client" BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM/Components/Account
rg -n "BlazorShop.Storefront.V2|BlazorShop.Storefront.Presentation|BlazorShop.Storefront.Runtime|BlazorShop.Storefront.Client|BlazorShop.CommerceNode|BlazorShop.ControlPlane|Web.SharedV2" BlazorShop.PresentationV2/BlazorShop.Storefront.Components.WasmHost
```

Expected result:

- [ ] V2.WASM has no account browser controller injection.
- [ ] V2.WASM has no account leaf runtime lifecycle methods.
- [ ] WasmHost has no invalid references.
- [ ] Account shell/navigation remain in V2.WASM.

## Phase 15 - Final Release Gate

Goal: prove the phase is complete and did not break existing codebase boundaries.

Required command gate:

```powershell
dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.Components/BlazorShop.Storefront.Components.csproj --no-restore
dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.Browser/BlazorShop.Storefront.Browser.csproj --no-restore
dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.Components.WasmHost/BlazorShop.Storefront.Components.WasmHost.csproj --no-restore
dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM/BlazorShop.Storefront.V2.WASM.csproj --no-restore
dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.V2/BlazorShop.Storefront.V2.csproj --no-restore
dotnet build BlazorShop.sln --no-restore
dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-restore
git diff --check
```

Required architecture closure:

- [ ] `Components.WasmHost/Components/Account` contains the five account leaves.
- [ ] `V2.WASM/Components/Account` contains only V2 account app shell, V2 navigation, and V2 options/classes that are still V2-owned.
- [ ] `Components/Contracts/Account` contains account leaf class/label contracts.
- [ ] `StorefrontAccountFormClasses` is not V2.WASM-owned.
- [ ] `StorefrontAccountAddressBookClasses` is not V2.WASM-owned.
- [ ] `StorefrontAccountOrderListClasses` is not V2.WASM-owned.
- [ ] `StorefrontAccountOrderDetailClasses` is not V2.WASM-owned.
- [ ] `AccountNavigationClasses` remains V2.WASM-owned.
- [ ] `StorefrontAccountShellClasses` remains V2.WASM-owned.
- [ ] `StorefrontAccountProfileLabels` exists.
- [ ] `StorefrontAccountPasswordLabels` exists.
- [ ] `StorefrontAccountAddressBookLabels` exists.
- [ ] `StorefrontAccountOrderListLabels` exists.
- [ ] `StorefrontAccountOrderDetailLabels` exists.
- [ ] `AccountAppLabels` or equivalent V2-owned account app labels exist if AccountApp copy was parameterized.
- [ ] `AccountHostPage.razor` still owns `@rendermode`.
- [ ] WasmHost account components contain no `@rendermode`.
- [ ] WasmHost account components contain no direct backend transport.
- [ ] V2.WASM account composition still renders all current account sections.
- [ ] Starter.WASM remains untouched unless an explicit compile-only namespace update was unavoidable.

Required Playwright closure:

- [ ] Profile browser flow passes.
- [ ] Password validation browser flow passes.
- [ ] Address create/update/default/delete browser flow passes.
- [ ] Order list browser flow passes.
- [ ] Order detail browser flow passes.
- [ ] Account navigation browser flow passes.
- [ ] Network guardrail confirms same-origin BFF only for protected actions.
- [ ] Browser console/page error checks pass.
- [ ] Desktop and mobile account checks pass.

Required docs closure:

- [ ] `COMPONENT-MODES.md` documents account WasmHost leaves.
- [ ] Runtime boundary docs are updated.
- [ ] Project/folder guide is updated.
- [ ] Contract ownership docs are updated.
- [ ] QA checklist is updated.
- [ ] This plan file is updated from planned to complete only after implementation and verification pass.

## Implementation Order

Recommended commit sequence:

1. [ ] Contract move and labels only.
2. [ ] Move profile/password leaves and compile.
3. [ ] Move address leaf and compile.
4. [ ] Move order list/detail leaves and compile.
5. [ ] Update V2.WASM composition/options and cleanup old files.
6. [ ] Update architecture tests and docs.
7. [ ] Run command gate and browser Playwright QA.
8. [ ] Final cleanup and commit.

Do not combine all work into one unverified edit. Each pair of leaf moves should compile before continuing.

## Risk Register

- [ ] Risk: WasmHost cannot compile because moved components depend on V2.WASM class bags.
  - Mitigation: move leaf class contracts into Components contracts first.
- [ ] Risk: AccountApp success messages are still hardcoded in V2.WASM.
  - Mitigation: allowed if V2 owns final copy; prefer `StorefrontAccountViewOptions` or `StorefrontAccountAppLabels` for consistency.
- [ ] Risk: test updates weaken architecture guardrails.
  - Mitigation: replace old path assertions with stricter ownership assertions.
- [ ] Risk: account order detail namespace breaks `nameof(StorefrontAccountOrderDetail.OrderReference)`.
  - Mitigation: update imports and keep public parameter name stable.
- [ ] Risk: Playwright password test changes QA account credentials.
  - Mitigation: use validation-only password test unless fixture explicitly supports rotate-back.
- [ ] Risk: duplicate account components remain in V2.WASM and WasmHost.
  - Mitigation: cleanup phase must delete or justify wrappers before closure.
- [ ] Risk: future agents confuse Starter.WASM account logic with this phase.
  - Mitigation: document Starter.WASM as deferred and keep tests scoped to V2/V2.WASM.

## Deferred Follow-Up Review

After Phase 3.6 closes, run a fresh review before opening another extraction phase:

- [ ] Review remaining `StorefrontAccountApp.razor`.
- [ ] Review remaining `StorefrontAccountNavigation.razor`.
- [ ] Review `StorefrontAccountViewOptions.cs`.
- [ ] Review `AccountHostPage.razor`.
- [ ] Review remaining V2.WASM components for browser controller injection.
- [ ] Review product grid/card/detail leaves for remaining V2.WASM behavior ownership.
- [ ] Decide whether the next phase is another targeted extraction or a final V2 visual sweep.

Do not treat this deferred review as part of Phase 3.6 implementation.

## Definition Of Done

The phase is done only when all of these are true:

- [ ] Account profile/password/address/order list/order detail runtime leaves live in `BlazorShop.Storefront.Components.WasmHost`.
- [ ] V2.WASM account shell/navigation/options still own V2 composition and final visual values.
- [ ] Account leaf class and label contracts live in `BlazorShop.Storefront.Components/Contracts/Account`.
- [ ] WasmHost account leaves use `IStorefrontBrowserAccountController` and same-origin browser actions.
- [ ] V2.WASM account files do not inject `IStorefrontBrowserAccountController`.
- [ ] WasmHost has no invalid references to V2, Presentation, Runtime, Client, backend/core/API, Control Plane, Starter, or `Web.SharedV2`.
- [ ] `@rendermode` remains outside WasmHost.
- [ ] Existing account URLs and visible behavior remain unchanged.
- [ ] Focused builds pass.
- [ ] Full solution build passes.
- [ ] Focused and full V2 tests pass.
- [ ] Playwright account browser QA passes.
- [ ] Docs and QA checklist are updated.
- [ ] `git diff --check` passes.

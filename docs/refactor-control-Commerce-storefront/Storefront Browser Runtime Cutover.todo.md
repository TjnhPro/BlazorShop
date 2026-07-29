# Storefront Browser Runtime Cutover Todo

Status: In Progress
Owner: Storefront Platform
Created: 2026-07-29
Related plans:
- `Storefront Browser Semantics Boundary Closure.todo.md`
- `Storefront Browser Action Boundary Closure.todo.md`
- `Storefront Browser Boundary Final Closure.todo.md`
- `Storefront Components Logic Only Hardening.todo.md`
- `Storefront Components Compatibility Removal.todo.md`
- `Storefront V2 Shared Platform Functional MVP.todo.md`
- `Storefront Playwright E2E Release.todo.md`

Scope: create `BlazorShop.Storefront.Browser` as the browser-safe application runtime for cart, checkout, and account flows, then cut `BlazorShop.Storefront.V2.WASM` back to visual components. This closes the remaining P0 boundary where V2.WASM still owns same-origin API orchestration, request DTO construction, mutation workflow, idempotency, and error mapping.

## Naming Decision

Use:

```text
BlazorShop.Storefront.Browser
```

Do not use:

```text
BlazorShop.Storefront.Presentation.Browser
BlazorShop.Storefront.Presentation.WASM
```

Reason: `BlazorShop.Storefront.Presentation` is a server/BFF presentation host package. The browser runtime must be safe for WebAssembly and must not accidentally pull server-only ASP.NET Core APIs, endpoint routing, middleware, `HttpContext`, Runtime facades, generated clients, or Commerce Node configuration into a browser assembly.

## Target Boundary

```text
BlazorShop.Storefront.Components
    Contracts
    Headless state/value models
    Browser-safe pure descriptors only
        ^
        |
BlazorShop.Storefront.Browser
    StorefrontLocalApiClient
    antiforgery reader
    cart controller
    checkout controller
    account controller
    mutation/idempotency/error/state orchestration
        ^
        |
BlazorShop.Storefront.V2.WASM
    V2 visual components
    V2 classes/copy/layout
    render fragments and event projection only
```

Forbidden references:

```text
BlazorShop.Storefront.Browser
    -/-> BlazorShop.Storefront.Presentation
    -/-> BlazorShop.Storefront.Runtime
    -/-> BlazorShop.Storefront.Client
    -/-> BlazorShop.Storefront.V2
    -/-> BlazorShop.Storefront.V2.WASM
    -/-> BlazorShop.ServiceDefaults
    -/-> BlazorShop.Application / Domain / Infrastructure
    -/-> BlazorShop.CommerceNode.API / ControlPlane.*
```

Allowed references:

```text
BlazorShop.Storefront.Browser
    -> BlazorShop.Storefront.Components
    -> framework packages that are browser/WASM safe
```

## Current Codebase Findings

Verified current files before writing this plan:

- [x] `BlazorShop.Storefront.V2.WASM/Program.cs` registers `HttpClient`, `IStorefrontAntiforgeryTokenReader`, and `StorefrontLocalApiClient`.
- [x] `StorefrontLocalApiClient` currently lives under `BlazorShop.Storefront.Components/Browser`.
- [x] `StorefrontCartView.razor` injects `IServiceProvider`, resolves `StorefrontLocalApiClient`, loads cart, validates quantity, creates `StorefrontBrowserCartQuantityRequest`, calls `PutJsonAsync`, `DeleteAsync`, and publishes cart events.
- [x] `StorefrontCheckoutShell.razor` injects `IServiceProvider`, selects shipping, selects payment, reviews checkout, places order, creates checkout request DTOs, owns `_idempotencyKey`, and redirects after order placement.
- [x] Account components under `V2.WASM/Components/Account` resolve `StorefrontLocalApiClient`, create account request DTOs, load/save profile, manage addresses, read orders, and map errors/success copy.
- [x] Visual boundary validator has profiles for V2 and Starter, but V2 profile treats `BlazorShop.Storefront.V2.WASM` as an allowed reference rather than validating V2.WASM as its own visual consumer.
- [x] Existing tests such as `StorefrontV2WASMRuntimeFoundationTests` currently assert V2.WASM owns local API orchestration, so tests must be inverted during cutover.
- [x] `StorefrontHostCompositionTests` is stale and already fails against the current shared application bootstrap.
- [x] Presentation browser script still publishes raw `preview` and raw cart `summary`, which should be addressed with the browser runtime/event closure phases.

## Non-goals

- [x] Do not change Commerce Node Storefront API route shape.
- [x] Do not change server `BlazorShop.Storefront.Presentation` BFF endpoint behavior except where tests need clearer browser contracts.
- [x] Do not redesign V2 cart, checkout, or account visuals.
- [x] Do not move V2 CSS/classes/copy into `BlazorShop.Storefront.Browser`.
- [x] Do not create a generic JavaScript framework SDK in this phase.
- [x] Do not remove interactive WASM rendering from V2; only move application controller ownership.
- [x] Do not add React/Vue/Next skeletons in this phase.

## Phase F1.65 - Browser Runtime Project Foundation

Goal: add the `BlazorShop.Storefront.Browser` project and move browser-safe transport primitives out of visual/component ownership.

### Implementation

- [x] Create `BlazorShop.PresentationV2/BlazorShop.Storefront.Browser/BlazorShop.Storefront.Browser.csproj`.
- [x] Use a browser-safe SDK/project shape:
  - [x] Prefer `Microsoft.NET.Sdk` if the project contains only C# services and no `.razor` files.
  - [x] Use `Microsoft.NET.Sdk.Razor` only if controller components are implemented as Razor components.
- [x] Target the same TFM as active V2 projects.
- [x] Reference `BlazorShop.Storefront.Components`.
- [x] Do not reference `BlazorShop.Storefront.Presentation`, `Runtime`, `Client`, V2, or backend projects.
- [x] Move or wrap these browser primitives from `Components.Browser` into the new Browser project:
  - [x] `StorefrontLocalApiClient`
  - [x] `StorefrontLocalApiResult<T>`
  - [x] `IStorefrontAntiforgeryTokenReader`
  - [x] `StorefrontAntiforgeryTokenReader`
  - [x] same-origin route validation helpers
  - [x] browser error semantic model
  - [x] cart badge/event publisher interop currently needed by WASM
- [x] Keep temporary type-forwarding or adapter classes only if required for incremental build; mark them internal or obsolete and schedule deletion in F1.72.
- [x] Add `StorefrontBrowserServiceCollectionExtensions` with:
  - [x] `AddStorefrontBrowserRuntime()`
  - [x] `AddStorefrontBrowserCart()`
  - [x] `AddStorefrontBrowserCheckout()`
  - [x] `AddStorefrontBrowserAccount()`
- [x] Update `BlazorShop.Storefront.V2.WASM/Program.cs` to call `builder.Services.AddStorefrontBrowserRuntime()` instead of manually registering local API primitives.
- [x] Add the project to `BlazorShop.sln`.
- [x] Add project reference from `BlazorShop.Storefront.V2.WASM` to `BlazorShop.Storefront.Browser`.

### Tests

- [x] Add architecture test proving `BlazorShop.Storefront.Browser` has only allowed references.
- [x] Add architecture test proving Browser does not reference server Presentation, Runtime, Client, V2, Commerce Node, Control Plane, Application, Domain, or Infrastructure.
- [x] Add test proving `V2.WASM/Program.cs` uses `AddStorefrontBrowserRuntime()`.
- [x] Add test proving `V2.WASM/Program.cs` no longer contains `StorefrontLocalApiClient` manual registration.
- [x] Move existing local API client tests from `StorefrontV2WASMRuntimeFoundationTests` into a Browser-specific test class.

### Acceptance Criteria

- [x] `BlazorShop.Storefront.Browser` builds independently.
- [x] `V2.WASM` references Browser and Components only for storefront browser logic.
- [x] `Components` no longer needs to own same-origin API orchestration.
- [x] No server-only dependency enters the WASM graph.

## Phase F1.66 - Cart Browser Controller Cutover

Goal: move cart application controller logic from `StorefrontCartView.razor` into Browser runtime.

### Implementation

- [x] Create cart controller/state services in `BlazorShop.Storefront.Browser`, for example:
  - [x] `IStorefrontBrowserCartController`
  - [x] `StorefrontBrowserCartController`
  - [x] `StorefrontBrowserCartState`
  - [x] `StorefrontBrowserCartMutationState`
  - [x] `StorefrontBrowserCartError`
- [x] Move cart orchestration from V2.WASM into Browser:
  - [x] `CurrentCartRoute` load.
  - [x] initial snapshot vs browser fetch vs refresh-after-hydration behavior.
  - [x] quantity parsing and min quantity validation.
  - [x] update quantity command request creation.
  - [x] remove line command.
  - [x] clear cart command.
  - [x] busy state and mutation concurrency guard.
  - [x] warning/error mapping.
  - [x] cart count event publishing.
- [x] Keep `StorefrontCartActionDescriptor` in Components if it is a contract shared by browser and views.
- [x] Convert `V2.WASM/Components/Cart/StorefrontCartView.razor` to a visual view:
  - [x] accepts controller state as parameter or consumes a controller cascading context.
  - [x] calls high-level controller actions such as `UpdateQuantityAsync(lineId, value)`.
  - [x] does not create request DTOs.
  - [x] does not call `StorefrontLocalApiClient`.
  - [x] does not know route strings except visual navigation URLs.
- [x] Choose the component integration style:
  - [x] Preferred: C# controller service injected directly into V2 visual component if no Razor controller wrapper is needed.
  - [x] Acceptable: Browser-owned `<StorefrontCartController>` wrapper with render fragments if it keeps V2 visual file pure.
- [x] Keep visual classes/copy in V2.WASM.

### Tests

- [x] Add Browser unit tests for cart controller:
  - [x] load current cart.
  - [x] update quantity creates `StorefrontBrowserCartQuantityRequest`.
  - [x] min quantity failure does not call API.
  - [x] remove line respects busy state.
  - [x] clear cart respects busy state.
  - [x] failed API result maps to cart error state.
  - [x] successful mutation publishes cart count.
- [x] Invert V2.WASM tests:
  - [x] `StorefrontCartView.razor` must not contain `StorefrontLocalApiClient`.
  - [x] must not contain `GetAsync<`, `PutJsonAsync<`, `DeleteAsync<`.
  - [x] must not contain `StorefrontBrowserCartQuantityRequest`.
  - [x] must not inject `IServiceProvider`.
- [x] Keep V2 visual tests for DOM descriptors and button states.

### Acceptance Criteria

- [x] Cart API orchestration is in Browser runtime.
- [x] V2 cart component is visual and delegates to Browser controller.
- [x] Cart behavior remains identical for current V2 user flows.
- [x] Existing cart badge updates still work.

## Phase F1.67 - Checkout Browser Controller Cutover

Goal: move checkout workflow orchestration from `StorefrontCheckoutShell.razor` into Browser runtime.

### Implementation

- [x] Create checkout controller/state services in Browser:
  - [x] `IStorefrontBrowserCheckoutController`
  - [x] `StorefrontBrowserCheckoutController`
  - [x] `StorefrontBrowserCheckoutStateMachine`
  - [x] `StorefrontBrowserCheckoutCommandState`
  - [x] `StorefrontBrowserCheckoutError`
- [x] Move workflow ownership out of V2.WASM:
  - [x] load checkout state.
  - [x] select shipping method.
  - [x] select payment method.
  - [x] review checkout.
  - [x] place order.
  - [x] create `StorefrontBrowserCheckoutSelectionRequest`.
  - [x] create `StorefrontBrowserCheckoutReviewRequest`.
  - [x] create `StorefrontBrowserCheckoutPlaceOrderRequest`.
  - [x] maintain idempotency key.
  - [x] pass expected cart/checkout versions.
  - [x] loading/error state.
  - [x] redirect result handling.
- [x] Decide redirect ownership:
  - [x] Browser controller returns `RedirectUrl` in a command result.
  - [x] V2 visual shell may call `NavigationManager.NavigateTo(...)` only as a visual host navigation effect.
  - [x] No request DTO or checkout version logic remains in V2 visual shell.
- [x] Keep `StorefrontCheckoutActionDescriptor` in Components if it remains a stable browser contract.
- [x] Convert `V2.WASM/Components/Checkout/StorefrontCheckoutShell.razor` to render state and call high-level Browser controller methods.
- [x] Keep V2 classes/copy/button labels in V2.WASM.

### Tests

- [x] Add Browser unit tests for checkout controller:
  - [x] select shipping command uses session ID and expected cart version.
  - [x] select payment command uses session ID and expected cart version.
  - [x] review command includes terms accepted and expected cart version.
  - [x] place order includes expected checkout version, expected cart version, and idempotency key.
  - [x] idempotency key remains stable across a single checkout placement attempt.
  - [x] failed result maps error state and does not redirect.
  - [x] successful result returns redirect URL.
- [x] Invert V2.WASM tests:
  - [x] checkout shell must not contain `StorefrontLocalApiClient`.
  - [x] must not contain `PostJsonAsync<`.
  - [x] must not contain `StorefrontBrowserCheckoutSelectionRequest`.
  - [x] must not contain `StorefrontBrowserCheckoutReviewRequest`.
  - [x] must not contain `StorefrontBrowserCheckoutPlaceOrderRequest`.
  - [x] must not contain `ExpectedCartVersion`, `ExpectedCheckoutVersion`, or `IdempotencyKey`.
  - [x] must not inject `IServiceProvider`.
- [x] Add browser regression for checkout interactive shell after cutover.

### Acceptance Criteria

- [x] Checkout workflow is owned by Browser runtime.
- [x] V2 checkout shell is visual and delegates commands.
- [x] Place-order still uses same-origin BFF and keeps idempotency guarantees.
- [x] Current COD checkout path remains testable.

## Phase F1.68 - Account Browser Controller Cutover

Goal: move account profile, address book, order list/detail, and password mutation orchestration from V2.WASM to Browser runtime.

### Implementation

- [x] Create Browser account controllers/services:
  - [x] `IStorefrontBrowserAccountController`
  - [x] `StorefrontBrowserAccountController`
  - [x] optional smaller services: `ProfileController`, `AddressBookController`, `OrderHistoryController`, `PasswordController`.
- [x] Move profile orchestration:
  - [x] load profile.
  - [x] create/update `StorefrontBrowserCustomerProfileUpdateRequest`.
  - [x] map success/error state.
- [x] Move address book orchestration:
  - [x] load addresses.
  - [x] create/update/delete/default address.
  - [x] create `StorefrontBrowserCustomerAddressRequest`.
  - [x] maintain edit/new form state.
- [x] Move order list/detail loading:
  - [x] paging request/route execution.
  - [x] selected order detail load.
  - [x] error state.
- [x] Move password mutation:
  - [x] command execution.
  - [x] success/error mapping.
- [x] Convert V2.WASM account components to visual views:
  - [x] render account state.
  - [x] bind input values to Browser-owned form state or view model.
  - [x] call high-level controller methods.
  - [x] no local API calls.
  - [x] no request DTO construction.
- [x] Keep V2 account route composition and visual navigation in V2.WASM unless route interpretation is later moved to a host descriptor.

### Tests

- [x] Add Browser unit tests for profile load/save.
- [x] Add Browser unit tests for address create/update/delete/default.
- [x] Add Browser unit tests for order list/detail load.
- [x] Add Browser unit tests for change password command.
- [x] Invert V2.WASM account tests:
  - [x] account components must not contain `StorefrontLocalApiClient`.
  - [x] must not contain `GetAsync<`, `PostJsonAsync<`, `PutJsonAsync<`, `DeleteAsync<`.
  - [x] must not contain `StorefrontBrowserCustomerProfileUpdateRequest`.
  - [x] must not contain `StorefrontBrowserCustomerAddressRequest`.
  - [x] must not inject `IServiceProvider`.
- [x] Keep V2 account visual tests for layout, classes, and rendered states.

### Acceptance Criteria

- [x] Account application logic is owned by Browser runtime.
- [x] V2 account components are visual composition only.
- [x] Account profile/address/order/password browser behavior remains functional.

## Phase F1.69 - V2.WASM Visual Consumer Guardrail

Goal: make `V2.WASM` subject to the same visual-consumer constraints as V2, Starter, and generated storefronts.

### Implementation

- [x] Add a dedicated `StorefrontV2WasmProfile()` to `StorefrontVisualConsumerBoundaryValidatorTests`.
- [x] Allow only these V2.WASM project references:
  - [x] `BlazorShop.Storefront.Components`
  - [x] `BlazorShop.Storefront.Browser`
- [x] Allow framework packages required for WASM hosting only.
- [x] For V2.WASM source, forbid:
  - [x] `StorefrontLocalApiClient`
  - [x] `GetAsync<`
  - [x] `PostJsonAsync<`
  - [x] `PutJsonAsync<`
  - [x] `DeleteAsync<`
  - [x] `IServiceProvider`
  - [x] `GetService(`
  - [x] `GetRequiredService<`
  - [x] `IdempotencyKey`
  - [x] `ExpectedCartVersion`
  - [x] `ExpectedCheckoutVersion`
  - [x] `StorefrontBrowser*Request`
  - [x] `HttpClient`
  - [x] `fetch(`
  - [x] `XMLHttpRequest`
- [x] Do not blanket-skip `Program.cs`.
- [x] Add bootstrap-specific validation:
  - [x] V2.WASM `Program.cs` may create builder.
  - [x] may configure base-address `HttpClient` only through Browser runtime registration if Browser runtime owns it.
  - [x] may call `AddStorefrontBrowserRuntime()`.
  - [x] must not register custom application services, endpoints, middleware, Runtime, Client, or Commerce Node options.
- [x] Add bootstrap-specific validation for `*FoundationViewRegistration.cs` in V2 and Starter:
  - [x] may call view registration helpers.
  - [x] may map visual component types.
  - [x] must not register services, middleware, callbacks, or endpoint lambdas.
- [x] Apply the bootstrap guardrail to future generated `Storefront.{Name}` projects through the shared validator.

### Tests

- [x] `StorefrontVisualConsumerBoundaryValidatorTests` passes V2.WASM profile after cutover.
- [x] Negative fixture proves V2.WASM-style forbidden tokens fail.
- [x] Negative fixture proves `Program.cs` with `AddHttpClient`, `MapPost`, `UseMiddleware`, or `AddScoped` application services fails.
- [x] Negative fixture proves `FoundationViewRegistration.cs` with service registration fails.
- [x] Update StorefrontBuilder static gate to reuse or mirror bootstrap rules.

### Acceptance Criteria

- [x] V2.WASM is no longer an unvalidated visual consumer.
- [x] Bootstrap files are not validator escape hatches.
- [x] Future generated storefronts cannot hide application logic in `Program.cs`.

## Phase F1.70 - Semantic Event Encapsulation

Goal: ensure public browser events expose only visual projections and not raw application payloads.

### Implementation

- [x] Update Presentation `storefront.application.js` product purchase events:
  - [x] remove `preview` from `storefront:product-purchase:selection-changed`.
  - [x] do not publish raw generic `{ preview }` unless a real non-visual consumer still exists.
  - [x] split internal selection state from public visual selection projection.
- [x] Internal selection state remains only in closure:
  - [x] product ID.
  - [x] product variant ID.
  - [x] selected attributes.
  - [x] quantity.
  - [x] currency code.
  - [x] unit price.
  - [x] command readiness.
- [x] Public visual selection exposes only:
  - [x] `ready`
  - [x] `valid`
  - [x] `priceText`
  - [x] `comparePriceText`
  - [x] `stockText`
  - [x] `skuText`
  - [x] `gtinText`
  - [x] `mainImageUrl`
  - [x] `message`
- [x] Update add-line success/failure events:
  - [x] do not publish raw cart summary.
  - [x] do not publish internal selection state.
  - [x] publish visual message and count if needed.
- [x] Update cart changed event:
  - [x] canonical event should prefer `{ count }`.
  - [x] remove or explicitly deprecate `summary`.
  - [x] remove legacy `blazorshop:cart-changed` if no consumer remains.
- [x] Update V2 visual JS to consume only the public visual projection.

### Tests

- [x] Add JS/source regression proving `productPurchaseSelectionChanged` does not include `preview`.
- [x] Add JS/source regression proving public event selection does not include `productId`, `productVariantId`, `selectedAttributes`, `quantity`, `currencyCode`, or `unitPrice`.
- [x] Add V2 visual script test proving it reads only `selection.*Text`, `selection.ready`, `selection.valid`, `selection.mainImageUrl`, and `selection.message`.
- [x] Add Playwright/browser test proving variant change still updates price, stock, image, SKU, and GTIN.
- [x] Add cart badge test proving canonical `{ count }` event still updates badges.

### Acceptance Criteria

- [x] Raw preview data is never exposed to visual hosts.
- [x] Public visual events cannot be used as command payload sources.
- [x] Cart events expose only visual-safe data.

## Phase F1.71 - Enhanced Navigation And Alias Cleanup

Goal: make browser bindings safe across Blazor enhanced navigation and remove stale browser aliases.

### Implementation

- [x] Refactor Presentation browser initialization:
  - [x] `initializeGlobalListenersOnce()`
  - [x] `refreshPageBindings(document)`
- [x] Keep global delegated event listeners idempotent.
- [x] Run page refresh binding on:
  - [x] initial DOM load.
  - [x] `enhancedload`.
- [x] Use `WeakSet` or `data-storefront-bound` for node-specific listener bindings.
- [x] Refresh cart badges after enhanced navigation if a new badge appears.
- [x] Run product initial preview for new product roots after enhanced navigation.
- [x] Initialize consent banner for a newly inserted banner after enhanced navigation.
- [x] Remove remaining browser aliases when no consumer exists:
  - [x] `data-storefront-selection-quantity`.
  - [x] `dataset.previewRoute`.
  - [x] `dataset.variantSelect`.
  - [x] `dataset.attributeName`.
  - [x] legacy `blazorshop:cart-changed`.
- [x] Add Starter cart badge descriptor directly in `MainLayout.razor`.
- [x] Remove generator string replacement that adds cart badge descriptor.

### Tests

- [x] Playwright test: Home -> Product via enhanced navigation runs initial product preview.
- [x] Playwright test: Product -> Cart -> Product does not double-submit add-to-cart.
- [x] Playwright test: new cart badge after navigation receives count refresh.
- [x] Static tests prove removed aliases no longer exist in Presentation, V2, Starter, generator, or generated proof.
- [x] StorefrontBuilder composition tests prove Starter already owns cart badge descriptor.

### Acceptance Criteria

- [x] Enhanced navigation does not leave new DOM unbound.
- [x] Binding initialization stays idempotent.
- [x] Browser contract has one canonical selector set.
- [x] Starter is a stronger functional baseline without generator behavior patching.

## Phase F1.72 - Test Cleanup And Full Suite Closure

Goal: remove stale tests, invert old expectations, and prove full unfiltered test suite is meaningful again.

### Implementation

- [x] Delete `StorefrontHostCompositionTests.cs` or move useful assertions into `StorefrontApplicationBootstrapTests`.
- [x] Remove tests that assert V2.WASM owns local API orchestration.
- [x] Add tests that assert Browser runtime owns local API orchestration.
- [x] Update architecture docs:
  - [x] `docs/architecture/03-runtime-boundaries.md`
  - [x] `docs/architecture/05-project-and-folder-guide.md`
  - [x] `docs/architecture/10-v2-contract-ownership.md`
  - [x] `docs/architecture/11-storefront-builder.md`
- [x] Update QA todo files:
  - [x] `QA-StorefrontV2.todo.md`
  - [x] `QA-StorefrontStarter.todo.md`
  - [x] `Storefront Playwright E2E Release.todo.md`
- [x] Add `BlazorShop.Storefront.Browser` to AGENTS active project list after implementation.
- [x] Ensure no plan doc is marked complete until full verification passes.

### Verification Commands

- [x] `dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --filter StorefrontBrowser`
- [x] `dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --filter StorefrontVisualConsumerBoundaryValidatorTests`
- [x] `dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --filter StorefrontApplicationBootstrapTests`
- [x] `dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --configuration Release`
- [x] Focused Playwright browser run for V2 cart/account/checkout after Browser runtime cutover.
- [x] StorefrontBuilder generated proof after Starter cart badge and browser contract cleanup.

### Acceptance Criteria

- [x] Full unfiltered test suite passes.
- [x] No stale architecture test expects removed V2 host composition.
- [x] Browser runtime ownership is locked by tests.
- [x] V2.WASM visual-only status is locked by shared validator.

## Execution Order

1. [x] F1.65 Browser runtime project foundation.
2. [x] F1.66 Cart browser controller cutover.
3. [x] F1.67 Checkout browser controller cutover.
4. [x] F1.68 Account browser controller cutover.
5. [x] F1.69 V2.WASM visual consumer guardrail.
6. [x] F1.70 Semantic event encapsulation.
7. [x] F1.71 Enhanced navigation and alias cleanup.
8. [x] F1.72 Test cleanup and full suite closure.

Reasoning: create the browser-safe owner first, move the largest active flows one capability at a time, then lock the boundary with validator/tests and close browser event/enhanced navigation issues.

## Failure Modes Registry

| Failure mode | Cause | Prevention | Proof |
| --- | --- | --- | --- |
| WASM pulls server Presentation dependency | Browser runtime references server package | Forbid `BlazorShop.Storefront.Presentation` reference from Browser | Architecture test |
| V2.WASM keeps hidden app logic | Validator does not scan V2.WASM as visual consumer | Add dedicated V2.WASM profile | Shared validator test |
| Cart mutation behavior regresses | Controller extraction changes busy/error handling | Port behavior with unit tests before visual cutover | Browser cart controller tests |
| Checkout duplicate order risk | Idempotency key moves incorrectly | Browser controller owns stable idempotency state | Checkout controller tests |
| Account forms lose state | Request/form state moved too aggressively | Move account capability in smaller profile/address/order/password slices | Account controller tests |
| Visual host reads raw preview | Public event still includes raw payload | Split internal and visual event models | JS regression tests |
| Enhanced navigation creates stale DOM | one-time initialization never refreshes new nodes | Add `enhancedload` page refresh binding | Playwright navigation proof |
| Full suite stays red | stale architecture tests remain compiled | Delete/invert stale tests in closure phase | Release `dotnet test` |

## Decision Audit Trail

| # | Phase | Decision | Classification | Principle | Rationale | Rejected |
|---|-------|----------|----------------|-----------|-----------|----------|
| 1 | F1.65 | Name the project `BlazorShop.Storefront.Browser`. | User-approved | Boundary clarity | The user approved the shorter name and it avoids implying a server Presentation dependency. | `BlazorShop.Storefront.Presentation.Browser`, because it can mislead future agents into referencing server Presentation. |
| 2 | F1.65 | Browser runtime may reference Components but not server Presentation/Runtime/Client. | Auto-decided | Browser safety | WASM must not pull server-only APIs or Commerce Node transport. | Letting Browser reuse server Presentation services directly. |
| 3 | F1.66-F1.68 | Cut over cart, checkout, and account separately. | Auto-decided | Small blast radius | Each flow has different state/mutation rules and needs focused tests. | Big-bang rewrite of all V2.WASM components. |
| 4 | F1.69 | Add V2.WASM to the shared visual consumer validator. | Auto-decided | Same rule for same role | V2.WASM is a visual consumer after cutover, so it must be scanned like V2/Starter/generated. | Keeping bespoke V2.WASM tests only. |
| 5 | F1.70 | Public events expose visual projections only. | Auto-decided | Least data exposure | Raw preview/cart payloads let visual hosts reconstruct application decisions. | Relying on validator to prevent all aliasing/bypass patterns. |
| 6 | F1.72 | Full unfiltered test suite is a closure requirement. | Auto-decided | Production confidence | Focused filters can hide stale failing tests and broken architecture assumptions. | Closing the phase with filtered test runs only. |

## GSTACK REVIEW REPORT

### CEO Review

This plan addresses the largest remaining MVP boundary blocker: V2.WASM still functions as an application controller. The work is worth doing before public MVP because generated or named storefronts will otherwise inherit an unclear split where server Presentation owns BFF routes but visual WASM owns workflow decisions.

### Design Review

The plan preserves design flexibility. V2.WASM keeps layout, copy, classes, and component placement, while Browser runtime supplies state and command behavior. This lets future `Storefront.{Name}` components look different without rewriting cart, checkout, or account application logic.

### Engineering Review

The plan is compatible with the current codebase if it is done incrementally. The key technical constraint is that `BlazorShop.Storefront.Browser` must stay browser-safe and must not reference server `Storefront.Presentation`; the project exists to host client-side orchestration above same-origin BFF routes, not to call Commerce Node or Runtime directly.

### DX Review

The naming and boundary improve future developer experience. A developer building a new storefront can reference Browser for behavior and Components for contracts/headless state, then write only visual components. The guardrail phases make this discoverable through tests instead of tribal knowledge.

### Cross-phase Themes

- Keep browser application behavior reusable and store-agnostic.
- Keep visual projects free of request DTOs, local API calls, idempotency, and workflow state.
- Use full-suite verification because stale tests currently mask architecture drift.

NO UNRESOLVED DECISIONS

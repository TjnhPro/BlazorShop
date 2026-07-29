# Storefront Browser Boundary Final Closure Todo

Status: In Progress
Owner: Storefront Platform
Created: 2026-07-29
Related plans:
- `Storefront Browser Action Boundary Closure.todo.md`
- `Storefront Browser Runtime Cutover.todo.md`
- `Storefront Browser Semantics Boundary Closure.todo.md`
- `Storefront Components Headless Presentation Refactor.todo.md`
- `Storefront Components Compatibility Removal.todo.md`
- `Storefront Presentation Foundation.todo.md`
- `Storefront Playwright E2E Release.todo.md`

Scope: close the remaining Storefront Browser boundary leaks after the Browser project and controller cutover. This plan keeps the current V2 architecture, keeps Commerce Node Storefront APIs unchanged, and moves only ownership/lifetime/reliability concerns that still make V2.WASM behave like an application transport owner.

## Current Codebase Findings

Verified before writing this plan:

- [x] `BlazorShop.Storefront.Browser` exists and owns browser-side cart, checkout, and account controllers.
- [x] `BlazorShop.Storefront.V2.WASM/Program.cs` already calls `builder.Services.AddStorefrontBrowserRuntime(builder.HostEnvironment)`.
- [x] V2.WASM cart/checkout/account components inject high-level browser controllers instead of directly injecting `StorefrontLocalApiClient`.
- [x] `StorefrontCartViewOptions.Actions` still hardcodes `/api/cart` and `/api/cart/lines/{lineId}` routes in V2.WASM.
- [x] `StorefrontCheckoutShellOptions.Actions` still hardcodes `/api/checkout`, shipping method, payment method, review, and place-order routes in V2.WASM.
- [x] `StorefrontAccountViewOptions` still hardcodes account page routes, same-origin `/api/account/*` routes, account navigation items, and account route parsing in V2.WASM.
- [x] V2 host pages still pass static V2.WASM descriptors:
  - [x] `CartPage.razor` passes `Actions="StorefrontCartViewOptions.Actions"`.
  - [x] `CheckoutPage.razor` passes `Actions="StorefrontCheckoutShellOptions.Actions"`.
  - [x] `AccountHostPage.razor` passes `StorefrontAccountViewOptions.*Actions`, `NavigationItems`, and `RouteDescriptor`.
- [x] Presentation contexts currently do not expose action descriptors:
  - [x] `StorefrontCartPageContext` exposes cart snapshot, alerts, and links only.
  - [x] `StorefrontCheckoutPageContext` exposes checkout state, totals, payment/address data, and links only.
  - [x] `StorefrontAccountPageContext` exposes path/page/error/saved/antiforgery only.
- [x] Browser controller registrations use `TryAddScoped` for cart, checkout, and account.
- [x] Browser controllers use `_initialized` flags and ignore later initial snapshots after the first initialization.
- [x] Checkout browser controller stores `_idempotencyKey` as a controller field and does not rotate it when `CheckoutSessionId` changes.
- [x] `StorefrontLocalApiClient.SendAsync()` does not normalize network failure, timeout, caller cancellation, or malformed success JSON into browser-safe error results.
- [x] Checkout and account mutation methods set loading/saving flags without `try/finally`, so thrown transport exceptions can leave state stuck.
- [x] V2 server `Program.cs` directly calls `AddStorefrontBrowserCart`, `AddStorefrontBrowserCheckout`, and `AddStorefrontBrowserAccount`.
- [x] `StorefrontVisualConsumerBoundaryValidator` still has `AllowedRouteDescriptorRelativePaths`, allowing selected V2.WASM files to contain route tokens.
- [x] Several tests still assert that `/api/cart` and `/api/checkout` live in V2.WASM options, so tests must be inverted during implementation.
- [x] The visual dependency validator mostly blocks known forbidden references; it should become a strict allowlist so unknown project/package references cannot slip through.

## Architecture Decision

Use this final boundary:

```text
Storefront Presentation
    owns page contexts
    owns same-origin BFF route/action descriptors
    owns account route interpretation and navigation contract
        |
        v
Storefront V2 / Starter / generated hosts
    render supplied contexts
    pass descriptors through to visual WASM components
    own classes, copy, icons, and layout only
        |
        v
Storefront Browser
    owns browser controller state
    owns same-origin local API calls
    owns mutation request construction
    owns idempotency and browser error semantics
```

Rules:

- [x] Presentation page services create or receive action descriptors and attach them to page contexts.
- [ ] V2.WASM options must not contain same-origin `/api/*` routes.
- [ ] V2.WASM visual components may accept descriptors as parameters but must not create default route descriptors with live endpoints.
- [ ] `Storefront.Components` may keep descriptor contract types, but not V2 default route values.
- [ ] Browser controllers must not behave like app-wide mutable singletons for page snapshots.
- [ ] Browser transport failures must return stable semantic errors unless the caller cancellation token requested cancellation.
- [ ] V2 server host may compose Browser controller registration through one aggregate extension, but must not use the WASM runtime extension.

## Non-goals

- [ ] Do not change Commerce Node Storefront API route shape.
- [ ] Do not change Presentation BFF endpoint route shape except through a deliberate follow-up API migration.
- [ ] Do not redesign V2 cart, checkout, or account markup.
- [ ] Do not move V2 CSS/classes/copy into Presentation or Browser.
- [ ] Do not add React/Vue/Next skeletons in this phase.
- [ ] Do not change checkout business rules, payment providers, cart pricing, stock, order placement, or customer authorization logic.
- [ ] Do not rewrite all Storefront Browser controllers. Harden the existing controllers in place.

## Phase F1.79 - Presentation Action Descriptor Ownership

Goal: make Presentation the single owner of same-origin browser action descriptors for cart, checkout, and account.

### Implementation

- [x] Add descriptor properties to `StorefrontCartPageContext`:
  - [x] `StorefrontCartActionDescriptor CartActions`.
- [x] Add descriptor properties to `StorefrontCheckoutPageContext`:
  - [x] `StorefrontCheckoutActionDescriptor CheckoutActions`.
- [x] Add descriptor/navigation properties to `StorefrontAccountPageContext`:
  - [x] `StorefrontAccountProfileActionDescriptor ProfileActions`.
  - [x] `StorefrontAccountPasswordActionDescriptor PasswordActions`.
  - [x] `StorefrontAccountAddressActionDescriptor AddressActions`.
  - [x] `StorefrontAccountOrderActionDescriptor OrderActions`.
  - [x] `AccountRouteDescriptor RouteDescriptor`.
  - [x] `IReadOnlyList<AccountNavigationItem> NavigationItems`.
- [x] Keep descriptor contract types in `BlazorShop.Storefront.Components` headless/contracts namespaces if they are already consumed by Browser and V2.WASM.
- [x] Create a Presentation-owned descriptor provider/factory to avoid duplicating route string construction in multiple page services.
  - [x] Suggested name: `StorefrontBrowserActionDescriptorProvider`.
  - [x] Suggested location: `BlazorShop.Storefront.Presentation/Services/Browser` or a capability-specific Presentation services folder.
  - [x] It may expose explicit methods such as `CreateCartActions()`, `CreateCheckoutActions()`, and `CreateAccountDescriptors()`.
- [x] Use the provider from:
  - [x] `StorefrontCartPageService`.
  - [x] `StorefrontCheckoutPageService`.
  - [x] `StorefrontAccountPageService`.
- [x] Preserve existing route values exactly:
  - [x] `/api/cart`.
  - [x] `/api/cart/lines/{lineId}`.
  - [x] `/api/checkout`.
  - [x] `/api/checkout/shipping-method`.
  - [x] `/api/checkout/payment-method`.
  - [x] `/api/checkout/review`.
  - [x] `/api/checkout/place-order`.
  - [x] `/api/account/profile`.
  - [x] `/api/account/change-password`.
  - [x] `/api/account/addresses`.
  - [x] `/api/account/orders`.
- [x] Preserve existing account page routes exactly:
  - [x] `/account/profile`.
  - [x] `/account/orders`.
  - [x] `/account/addresses`.
  - [x] `/account/change-password`.
  - [x] `/account/orders/{orderReference}`.
- [x] Do not introduce configurable route strings until a real multi-host route customization requirement appears.

### Tests

- [x] Add focused tests proving `StorefrontCartPageContext` includes `CartActions` with current route values.
- [x] Add focused tests proving `StorefrontCheckoutPageContext` includes `CheckoutActions` with current route values.
- [x] Add focused tests proving `StorefrontAccountPageContext` includes profile/password/address/order actions, route descriptor, and navigation items.
- [x] Add a regression test proving all cart/checkout/account descriptors are created by Presentation code, not V2.WASM options.
- [x] Update existing constructor call sites in tests when contexts gain required properties.
  - No constructor call-site churn was required; descriptors are `init` properties with `Empty` defaults and Presentation services populate them.
  - Verification: `dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj -c Release --no-build --filter "FullyQualifiedName~StorefrontBrowserActionDescriptorProviderTests" --logger "trx;LogFileName=browser-boundary-f179.trx" --blame-hang --blame-hang-timeout 3m` passed 4/4.

### Acceptance Criteria

- [x] Presentation contexts carry all browser action descriptors needed by V2.WASM visual components.
- [x] No same-origin action route needs to be edited in V2.WASM when a Presentation BFF route changes.
- [x] Existing cart, checkout, and account page render behavior remains unchanged.

## Phase F1.80 - V2 Visual Options Cleanup

Goal: remove application route defaults from V2.WASM options and make V2 pages pass descriptors from Presentation contexts.

### Implementation

- [x] Update `BlazorShop.Storefront.V2/Pages/Hybrid/Commerce/CartPage.razor`:
  - [x] Replace `Actions="StorefrontCartViewOptions.Actions"` with `Actions="@Context.CartActions"`.
- [x] Update `BlazorShop.Storefront.V2/Pages/Hybrid/Commerce/CheckoutPage.razor`:
  - [x] Replace `Actions="StorefrontCheckoutShellOptions.Actions"` with `Actions="@Context.CheckoutActions"` in both shell render sites.
- [x] Update `BlazorShop.Storefront.V2/Pages/WasmHost/Account/AccountHostPage.razor`:
  - [x] Replace `NavigationItems="StorefrontAccountViewOptions.NavigationItems"` with `NavigationItems="@Context.NavigationItems"`.
  - [x] Replace `RouteDescriptor="StorefrontAccountViewOptions.RouteDescriptor"` with `RouteDescriptor="@Context.RouteDescriptor"`.
  - [x] Replace `ProfileActions`, `PasswordActions`, `AddressActions`, and `OrderActions` with context properties.
- [x] Remove `StorefrontCartViewOptions.Actions`.
- [x] Remove `StorefrontCheckoutShellOptions.Actions`.
- [x] Remove these members from `StorefrontAccountViewOptions`:
  - [x] `ProfileActions`.
  - [x] `PasswordActions`.
  - [x] `AddressActions`.
  - [x] `OrderActions`.
  - [x] `NavigationItems`.
  - [x] `RouteDescriptor`.
- [x] Keep visual members in V2.WASM options:
  - [x] cart classes.
  - [x] checkout classes.
  - [x] account navigation/form/address/order/shell classes.
  - [x] V2 copy/icon/visual defaults if present.
- [x] Do not move V2 visual classes to Presentation.

### Tests

- [x] Invert `StorefrontV2WASMRuntimeFoundationTests` expectations:
  - [x] cart options must not contain `"/api/cart"`.
  - [x] checkout options must not contain `"/api/checkout"`.
  - [x] account options must not contain `"/api/account"` or hardcoded `/account/*` navigation routes.
- [x] Update cart page tests to require `Actions="@Context.CartActions"`.
- [x] Update checkout page tests to require `Actions="@Context.CheckoutActions"`.
- [x] Update account host page tests to require context-provided actions/navigation/routes.
- [x] Keep tests proving V2.WASM components accept descriptor parameters and delegate to Browser controllers.
  - Verification: `dotnet build BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj -c Release --no-restore` passed with known MessagePack/Browserslist warnings.
  - Verification: `dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj -c Release --no-build --filter "FullyQualifiedName~StorefrontV2WASMRuntimeFoundationTests|FullyQualifiedName~StorefrontComponentsHeadlessPresentationRefactorTests|FullyQualifiedName~StorefrontBrowserActionDescriptorProviderTests" --logger "trx;LogFileName=browser-boundary-f180.trx" --blame-hang --blame-hang-timeout 5m` passed 47/47.

### Acceptance Criteria

- [x] V2.WASM options contain visual defaults only.
- [x] V2 server pages pass browser action descriptors from Presentation contexts.
- [x] No V2.WASM static option owns a BFF route or account route parsing rule.

## Phase F1.81 - Browser Controller Lifetime Hardening

Goal: prevent stale SSR snapshots, stale account identity, and checkout idempotency reuse across browser navigation/session changes.

### Implementation

- [x] Decide implementation style based on current component injection:
  - [x] Preferred narrow change: register browser controllers as transient.
  - [x] Alternative if component sharing is needed: use component-owned scope through an owning wrapper/component pattern.
- [x] Change controller registrations in `StorefrontBrowserServiceCollectionExtensions`:
  - [x] `IStorefrontBrowserCartController`.
  - [x] `IStorefrontBrowserCheckoutController`.
  - [x] `IStorefrontBrowserAccountController`.
- [x] Keep `StorefrontLocalApiClient`, antiforgery token reader, and browser event publisher scoped if that remains appropriate for WASM runtime infrastructure.
- [x] Cart controller hardening:
  - [x] Replace single `_initialized` behavior with snapshot-aware initialization.
  - [x] Accept a newer initial snapshot when cart version/count/line identity proves it changed.
  - [x] If version comparison is unavailable or ambiguous, prefer the latest component-provided snapshot over stale in-memory state.
  - [x] Keep browser mutation state if the same component instance is mid-mutation.
- [x] Checkout controller hardening:
  - [x] Track the current `CheckoutSessionId`.
  - [x] Rotate `_idempotencyKey` when `CheckoutSessionId` changes.
  - [x] Keep the same key for retries within the same place-order attempt/session.
  - [x] Clear or rotate the key after successful order placement.
  - [x] Reinitialize from a newer SSR snapshot when the checkout session or version changes.
- [x] Account controller hardening:
  - [x] Define an identity key from current profile/customer data when available.
  - [x] Reset profile/address/order state when the identity key changes.
  - [x] Reset order detail state when order reference changes.
  - [x] Reset orders state when page number changes.
  - [x] Do not show profile/address/order state from a previous signed-in user after logout/login.
- [x] Keep current public controller interfaces unless a method needs an identity/version parameter to support safe reinitialization.
  - Kept the public controller interfaces unchanged.
  - Fixed account navigation payload materialization to `AccountNavigationItem[]` so Presentation context values serialize across the WASM render boundary without compiler collection types.

### Tests

- [x] Cart controller test: initialize with snapshot A, initialize again with newer snapshot B, state uses B.
- [x] Cart controller test: current same component mutation is not overwritten by an older snapshot.
- [x] Checkout controller test: same `CheckoutSessionId` retry keeps idempotency key.
- [x] Checkout controller test: new `CheckoutSessionId` rotates idempotency key.
- [x] Checkout controller test: successful place order clears or rotates key for the next checkout session.
- [x] Account controller test: profile identity change clears previous profile state.
- [x] Account controller test: orders page change accepts new initial order list.
- [x] Account controller test: order reference change accepts new order detail snapshot.
- [x] DI test: Browser runtime registers controllers with the chosen lifetime.
  - Verification: `dotnet build BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj -c Release --no-restore` passed with known MessagePack/Browserslist warnings.
  - Verification: `dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj -c Release --no-build --filter "FullyQualifiedName~StorefrontBrowserCartControllerTests|FullyQualifiedName~StorefrontBrowserCheckoutControllerTests|FullyQualifiedName~StorefrontBrowserAccountControllerTests|FullyQualifiedName~StorefrontBrowserRuntimeFoundationTests|FullyQualifiedName~StorefrontBrowserActionDescriptorProviderTests" --logger "trx;LogFileName=browser-boundary-f181.trx" --blame-hang --blame-hang-timeout 5m` passed 49/49.

### Browser QA

- [x] Playwright: open cart, navigate away, add product, return to cart, verify new line appears.
- [x] Playwright: start checkout, navigate back to cart, change cart, return to checkout, verify checkout reflects current cart/version.
- [x] Playwright: logout/login as another customer in same browser session, verify account data is not stale.
  - Browser QA: `scripts/run-v2-local.ps1 -StopExisting -NoOpenBrowser` started `http://localhost:18598`; inline Playwright cart/checkout probe passed with same-origin `/api/cart`, `/api/product-selection-preview`, and `/api/cart/lines` calls and zero direct Commerce Node browser calls.
  - Browser QA: inline Playwright account probe registered/signed in `qa.boundary.first.*`, logged out, registered/signed in `qa.boundary.second.*` in the same browser session, verified profile email changed to the second customer, and recorded zero direct Commerce Node browser calls.

### Acceptance Criteria

- [x] Browser controllers no longer behave like app-wide mutable singletons for page state.
- [x] SSR snapshot refresh is not ignored after enhanced navigation.
- [x] Checkout idempotency key lifetime matches checkout session/attempt behavior.
- [x] Account state cannot leak across customer/session changes.

## Phase F1.82 - Browser Transport Reliability Closure

Goal: normalize browser transport failures and guarantee mutation flags reset on all exit paths.

### Implementation

- [ ] Extend `StorefrontLocalApiError` or related browser error model with semantic codes:
  - [ ] `network_error`.
  - [ ] `timeout`.
  - [ ] `invalid_response`.
  - [ ] `request_cancelled` if a non-propagated cancellation result is needed.
- [ ] Update `StorefrontLocalApiClient.SendAsync()`:
  - [ ] Propagate `OperationCanceledException` when the caller cancellation token is cancelled.
  - [ ] Map timeout `TaskCanceledException` not caused by caller cancellation to a retryable timeout result.
  - [ ] Map `HttpRequestException` to a retryable network error result.
  - [ ] Map malformed JSON on success responses to an invalid-response result.
  - [ ] Preserve current same-origin route validation behavior.
  - [ ] Preserve current non-success HTTP error body parsing behavior.
- [ ] Update cart controller mutation methods:
  - [ ] Use `try/finally` for line mutation busy states where a throw can leave state stuck.
  - [ ] Ensure failed transport results set user-safe error state and return.
- [ ] Update checkout controller:
  - [ ] Wrap `PlaceOrderAsync` loading state in `try/finally`.
  - [ ] Wrap `LoadAsync` loading state in `try/finally`.
  - [ ] Do not swallow caller cancellation as a user-facing timeout.
- [ ] Update account controller:
  - [ ] Wrap profile save, password change, address mutations, and order load busy/saving flags in `try/finally`.
  - [ ] Surface semantic browser errors through existing state fields without hardcoding final storefront copy where practical.
- [ ] Keep technical fallback message only as a non-final default. Storefront visual host/localization remains responsible for final copy.

### Tests

- [ ] `StorefrontLocalApiClient` test: `HttpRequestException` returns `network_error` and retryable metadata.
- [ ] `StorefrontLocalApiClient` test: timeout not caused by caller cancellation returns `timeout`.
- [ ] `StorefrontLocalApiClient` test: caller cancellation propagates `OperationCanceledException`.
- [ ] `StorefrontLocalApiClient` test: HTTP 200 with malformed JSON returns `invalid_response`.
- [ ] `StorefrontLocalApiClient` test: empty successful body stays supported.
- [ ] Checkout controller test: exception/result failure always resets `State.Loading`.
- [ ] Account controller tests: save/address mutation failures always reset saving flags.
- [ ] Cart controller tests: line mutation failures always reset busy flags.

### Acceptance Criteria

- [ ] Browser transport failures produce stable semantic errors instead of unhandled exceptions where appropriate.
- [ ] Caller cancellation is not mislabeled as timeout.
- [ ] Loading/saving/busy flags cannot remain stuck after failed browser requests.

## Phase F1.83 - Browser Registration And Host Composition Cleanup

Goal: keep V2 server host thin while preserving server prerender support for Browser controller injection.

### Implementation

- [ ] Add a server-safe aggregate extension in `BlazorShop.Storefront.Browser`:

```csharp
services.AddStorefrontBrowserControllers();
```

- [ ] `AddStorefrontBrowserControllers()` registers only:
  - [ ] cart controller.
  - [ ] checkout controller.
  - [ ] account controller.
- [ ] `AddStorefrontBrowserControllers()` must not register:
  - [ ] `HttpClient` with `IWebAssemblyHostEnvironment.BaseAddress`.
  - [ ] `StorefrontLocalApiClient`.
  - [ ] antiforgery token reader.
  - [ ] Browser-only JS/event transport primitives unless they are safe and required for server prerender.
- [ ] Update `AddStorefrontBrowserRuntime(builder.HostEnvironment)` to call:
  - [ ] browser local transport registration.
  - [ ] `AddStorefrontBrowserControllers()`.
- [ ] Update V2 server `Program.cs`:
  - [ ] Replace `AddStorefrontBrowserCart()`, `AddStorefrontBrowserCheckout()`, and `AddStorefrontBrowserAccount()` with `AddStorefrontBrowserControllers()`.
- [ ] Keep V2.WASM `Program.cs` using `AddStorefrontBrowserRuntime(builder.HostEnvironment)`.
- [ ] Do not call `AddStorefrontBrowserRuntime()` from V2 server host.

### Tests

- [ ] Add Browser DI test proving `AddStorefrontBrowserControllers()` registers all three controllers.
- [ ] Add Browser DI test proving `AddStorefrontBrowserControllers()` does not register `StorefrontLocalApiClient` or WASM-only dependencies.
- [ ] Update visual boundary validator to allow aggregate server registration and forbid individual controller registration from V2 server `Program.cs`.
- [ ] Keep `V2.WASM Program.cs` test requiring `AddStorefrontBrowserRuntime(builder.HostEnvironment)`.
- [ ] Add/adjust host composition test proving V2 server uses only:
  - [ ] `AddStorefrontApplication`.
  - [ ] `AddStorefrontBrowserControllers`.
  - [ ] `AddV2FoundationViews`.
  - [ ] `UseStorefrontApplication`.
  - [ ] `MapStorefrontApplication`.

### Acceptance Criteria

- [ ] V2 server Program no longer knows individual cart/checkout/account Browser capabilities.
- [ ] Browser has separate server-prerender and WASM-runtime registration paths.
- [ ] No WASM-only HttpClient setup enters the server host.

## Phase F1.84 - Visual Boundary Guardrail Tightening

Goal: make static tests enforce the final boundary instead of legalizing the old exceptions.

### Implementation

- [ ] Remove `AllowedRouteDescriptorRelativePaths` from `StorefrontVisualConsumerProfile`.
- [ ] Remove `IsAllowedSourceToken()` exception logic for route descriptors.
- [ ] Keep `/api/cart`, `/api/checkout`, `/api/consent`, and `/api/product-selection-preview` forbidden in visual consumer source.
- [ ] Apply the no-`/api/` visual rule to:
  - [ ] `BlazorShop.Storefront.V2`.
  - [ ] `BlazorShop.Storefront.V2.WASM`.
  - [ ] `BlazorShop.Storefront.Starter`.
  - [ ] generated visual source scanned by StorefrontBuilder proof.
- [ ] Keep allowed route tokens only in Presentation, Browser tests, endpoint tests, or explicitly non-visual source.
- [ ] Convert project reference validation to strict allowlist:
  - [ ] if a `ProjectReference` is not explicitly allowed for the profile, fail.
  - [ ] do not only fail known forbidden names.
- [ ] Convert package reference validation to strict allowlist where practical:
  - [ ] allow framework packages needed by the visual host.
  - [ ] allow `BlazorShop.Storefront.Presentation`, `BlazorShop.Storefront.Components`, and `BlazorShop.Storefront.Browser` only where that profile needs them.
  - [ ] keep Runtime/Client package metadata outside visual `.csproj` as already required.
- [ ] Update tests that currently assert route ownership in V2.WASM options:
  - [ ] `StorefrontV2WASMRuntimeFoundationTests`.
  - [ ] `StorefrontCommerceFlowCutoverTests`.
  - [ ] `StorefrontComponentsHeadlessPresentationRefactorTests`.
  - [ ] Any visual-only boundary test that still treats V2.WASM options as descriptor owners.
- [ ] Add negative validator fixture:
  - [ ] unknown `Customer.Storefront.Services.csproj` reference fails.
  - [ ] unknown `MyCompany.Application.csproj` reference fails.
  - [ ] `/api/cart` in any V2.WASM option file fails.
- [ ] Add positive validator fixture:
  - [ ] V2.WASM can reference Browser and Components.
  - [ ] V2 server can reference Presentation, Browser, Components, ServiceDefaults if currently needed.
  - [ ] Starter can reference Presentation and Components under current architecture.

### Tests

- [ ] Run focused architecture tests:

```powershell
dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --filter "FullyQualifiedName~StorefrontVisualOnlyBoundaryTests|FullyQualifiedName~StorefrontVisualConsumerBoundaryValidatorTests|FullyQualifiedName~StorefrontV2WASMRuntimeFoundationTests" --no-restore
```

- [ ] Run focused Presentation/browser tests:

```powershell
dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --filter "FullyQualifiedName~StorefrontBrowser|FullyQualifiedName~StorefrontPresentation|FullyQualifiedName~StorefrontV2WASMRuntimeFoundationTests" --no-restore
```

### Acceptance Criteria

- [ ] Visual boundary validator no longer contains route descriptor exceptions.
- [ ] Visual projects fail if they contain same-origin `/api/*` browser action routes.
- [ ] Unknown project/package references fail by default unless explicitly allowed.
- [ ] Tests protect the new ownership model rather than preserving V2.WASM route ownership.

## Phase F1.85 - Browser Re-entry Playwright QA

Goal: prove the boundary closure works in a real browser, not only through static tests.

### Preconditions

- [ ] Local V2 environment can be started with:

```powershell
.\scripts\run-v2-local.ps1 -StopExisting
```

- [ ] Store test fixture has:
  - [ ] a published product that can be added to cart.
  - [ ] COD payment enabled for test order placement if checkout E2E is run.
  - [ ] an account test user or documented guest flow.

### Playwright Cases

- [ ] Cart re-entry:
  - [ ] Open product page.
  - [ ] Add product to cart.
  - [ ] Open cart page and verify line exists.
  - [ ] Navigate away.
  - [ ] Add another product or change quantity through a browser action.
  - [ ] Return to cart.
  - [ ] Verify cart shows current server state, not stale initial snapshot.
- [ ] Checkout session re-entry:
  - [ ] Open cart with active item.
  - [ ] Start checkout.
  - [ ] Record visible checkout/session/cart version data if exposed.
  - [ ] Return to cart and mutate cart.
  - [ ] Return to checkout.
  - [ ] Verify checkout reflects changed cart and blocking warnings/state are current.
- [ ] Checkout idempotency:
  - [ ] Submit place order with COD in test store.
  - [ ] Prevent double submit in UI.
  - [ ] Confirm one successful order reference is produced.
  - [ ] Retry only where the same attempt semantics are expected.
- [ ] Account identity re-entry:
  - [ ] Login as user A.
  - [ ] Open profile/address/orders.
  - [ ] Logout.
  - [ ] Login as user B in same browser session.
  - [ ] Verify user A profile/address/order data is not visible.
- [ ] Browser transport failure visual state:
  - [ ] Simulate or intercept one cart/checkout/account BFF request failure.
  - [ ] Verify loading/saving/busy state resets.
  - [ ] Verify user sees a recoverable error state.

### Artifacts

- [ ] Save Playwright screenshots or traces for failures.
- [ ] Update `Storefront Playwright E2E Release.todo.md` if any release checklist steps need new cases.
- [ ] Update `QA-StorefrontV2.todo.md` if the manual QA matrix needs the re-entry checks.

### Acceptance Criteria

- [ ] Real browser re-entry does not show stale cart, checkout, or account state.
- [ ] COD order placement remains functional.
- [ ] Browser mutation failure does not leave stuck loading/busy UI.
- [ ] Existing Storefront V2 smoke flow still passes.

## Phase F1.86 - Documentation And Closure

Goal: update architecture docs and close the Browser boundary as an intentional final state.

### Documentation

- [ ] Update `docs/architecture/03-runtime-boundaries.md` if implementation adds `AddStorefrontBrowserControllers()` or changes Browser registration language.
- [ ] Update `docs/architecture/10-v2-contract-ownership.md` to state:
  - [ ] Presentation owns cart/checkout/account browser action descriptors.
  - [ ] V2.WASM options are visual-only.
  - [ ] Browser controllers are not app-wide snapshot owners.
- [ ] Update related Browser plans:
  - [ ] Mark this plan phases complete as they land.
  - [ ] Cross-link this plan from `Storefront Browser Runtime Cutover.todo.md`.
- [ ] Update QA checklists:
  - [ ] `QA-StorefrontV2.todo.md`.
  - [ ] `Storefront Playwright E2E Release.todo.md` if browser release gates change.

### Verification

- [ ] Run focused tests from phases F1.79-F1.84.
- [ ] Run full Storefront-focused test group:

```powershell
dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --filter "FullyQualifiedName~Storefront" --no-restore
```

- [ ] Run full suite if the implementation touches shared Presentation/Browser registrations broadly:

```powershell
dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-restore
```

- [ ] Run Playwright re-entry proof before production/MVP closure.

### Acceptance Criteria

- [ ] Architecture docs match implemented Browser/Presentation/V2 boundaries.
- [ ] Historical plans no longer claim completed boundary while these final blockers remain open.
- [ ] QA checklist contains browser re-entry cases.
- [ ] The closure commit can be reviewed without relying on old V2.WASM route exceptions.

## Final Definition Of Done

- [ ] No `/api/cart`, `/api/checkout`, `/api/account`, `/api/consent`, or `/api/product-selection-preview` action route is present in V2 or V2.WASM visual source.
- [ ] Cart actions come from `StorefrontCartPageContext`.
- [ ] Checkout actions come from `StorefrontCheckoutPageContext`.
- [ ] Account actions, navigation, and route parsing come from `StorefrontAccountPageContext`.
- [ ] `StorefrontCartViewOptions.Actions` is removed.
- [ ] `StorefrontCheckoutShellOptions.Actions` is removed.
- [ ] `StorefrontAccountViewOptions.*Actions`, `NavigationItems`, and `RouteDescriptor` are removed.
- [ ] Browser controllers are transient or component-owned, and tests prove they do not keep stale page snapshots.
- [ ] Checkout idempotency key rotates when checkout session changes and does not duplicate orders.
- [ ] Account state resets when customer/session identity changes.
- [ ] `StorefrontLocalApiClient` normalizes network, timeout, and invalid-response failures.
- [ ] Controller loading/saving/busy flags reset through `try/finally`.
- [ ] V2 server Program uses aggregate Browser controller registration.
- [ ] V2.WASM Program remains the only place that calls `AddStorefrontBrowserRuntime(builder.HostEnvironment)`.
- [ ] `AllowedRouteDescriptorRelativePaths` is removed from the visual boundary validator.
- [ ] Visual dependency validator uses strict allowlist behavior.
- [ ] Focused architecture tests pass.
- [ ] Focused Browser/Presentation controller tests pass.
- [ ] Storefront V2 browser re-entry Playwright proof passes.
- [ ] Documentation and QA checklists are updated.

## Suggested Commit Slices

Keep implementation reviewable in this order:

1. [ ] Commit 1: Presentation descriptors and V2 option cleanup.
2. [ ] Commit 2: Browser controller lifetime and idempotency hardening.
3. [ ] Commit 3: Browser transport reliability and loading flag cleanup.
4. [ ] Commit 4: aggregate Browser registration and visual validator tightening.
5. [ ] Commit 5: Playwright QA checklist/docs closure.

Do not combine all phases into one large commit unless the branch is explicitly being squashed before review.

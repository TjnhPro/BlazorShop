# Storefront Required Visual Contracts Hardening

Status: In Progress
Owner: Storefront V2 / V2.WASM
Scope: V2 interactive root visual contracts only

## Goal

Root visual components may own presentation state, markup, CSS classes, and browser interaction placement, but they must not silently create application wiring defaults.

This plan hardens the current V2/V2.WASM roots so missing page context, action descriptors, route descriptors, classes, URLs, and initial browser state fail clearly during development instead of rendering with fake fallback state.

## Current Code Evidence

- `BlazorShop.Storefront.V2/Pages/Hybrid/Commerce/CartPage.razor` currently creates a fallback `StorefrontCartPageContext` with default checkout/search/home links.
- `BlazorShop.Storefront.V2.WASM/Components/Cart/StorefrontCartView.razor` currently defaults `DataMode`, `Actions`, `Classes`, and cart URLs.
- `BlazorShop.Storefront.V2.WASM/Components/Checkout/StorefrontCheckoutShell.razor` currently defaults `InitialState`, `ShowPanel`, `DataMode`, `Actions`, and `Classes`.
- `BlazorShop.Storefront.V2.WASM/Components/Account/StorefrontAccountApp.razor` currently defaults route, navigation, page number, action descriptors, and visual class contracts.
- `BlazorShop.Storefront.V2/Pages/Hybrid/Commerce/CheckoutPage.razor` already passes checkout shell wiring explicitly and has `[Parameter, EditorRequired]` on `Context`.
- `BlazorShop.Storefront.V2/Pages/WasmHost/Account/AccountHostPage.razor` already passes account app wiring explicitly and has `[Parameter, EditorRequired]` on `Context`.

## Non-Goals

- Do not refactor leaf components such as account profile editor, address book, order list/detail, product selection, or individual buttons.
- Do not move browser controllers or change `BlazorShop.Storefront.Browser`.
- Do not change Commerce Node Storefront API contracts.
- Do not introduce new route ownership.
- Do not move V2 visual components into shared packages.
- Do not reject `.Empty` descriptors as illegal. A caller may intentionally pass `.Empty` in tests or unsupported states.
- Do not add a new general validation framework. Use narrow `ArgumentNullException.ThrowIfNull` / `ArgumentException.ThrowIfNullOrWhiteSpace` style checks.
- Do not broaden this phase to Starter unless a shared contract change requires build/test adjustment.

## Contract Classification Rules

### Required Root Wiring

These inputs must be supplied by the caller with `[Parameter, EditorRequired]`:

- Page route context.
- Initial cart/checkout/account state when the component renders from server-owned route context.
- Browser data mode.
- Action descriptors.
- Route descriptors.
- Navigation items.
- Visual class contracts.
- URLs used by root navigation/actions.
- Antiforgery field/token parameters when the root passes them to protected browser forms.

### Nullable But Required Presence

These may remain nullable values, but the caller must still pass them explicitly:

- `StorefrontBrowserCart? InitialCart`.
- `string? Path`.
- `string? AntiforgeryFieldName`.
- `string? AntiforgeryRequestToken`.

Reason: `null` can be a valid state, but the root component should not decide the absence silently.

### Optional UI State

These may remain optional and nullable:

- `Error`.
- `Saved`.
- Optional success/failure display messages.
- Optional non-critical action URLs that are genuinely feature-dependent.

### Behavioral Choices

These must be explicit at the root because omission changes runtime behavior:

- `StorefrontFeatureDataMode DataMode`.
- `bool ShowPanel`.
- `int PageNumber`.

## Phase 0 - Baseline And Guardrail Inventory

- [x] Read `AGENTS.md`, `docs/architecture/README.md`, `docs/architecture/05-project-and-folder-guide.md`, and `docs/architecture/08-agent-decision-rules.md`.
- [x] Confirm active target projects:
  - [x] `BlazorShop.PresentationV2/BlazorShop.Storefront.V2`
  - [x] `BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM`
  - [x] `BlazorShop.PresentationV2/BlazorShop.Storefront.Browser`
  - [x] `BlazorShop.Tests.V2`
- [x] Confirm this phase does not touch:
  - [x] `BlazorShop.Storefront.Presentation` service behavior.
  - [x] Commerce Node API.
  - [x] Control Plane.
  - [x] StorefrontBuilder.
  - [x] Starter visual components unless compile tests expose an impact.
- [x] Run source inventory:
  - [x] `rg -n "new\\(\\s*null|StorefrontLinkContext.Default|StorefrontCartPageContext" BlazorShop.PresentationV2/BlazorShop.Storefront.V2`
  - [x] `rg -n "StorefrontFeatureDataMode.BrowserFetch|ActionDescriptor.Empty|Classes.Empty|= \"/checkout\"|= \"/search\"|= \"/\"" BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM/Components`
  - [x] `rg -n "\\[Parameter\\]" BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM/Components/Cart BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM/Components/Checkout BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM/Components/Account`
- [x] Record baseline test files that already inspect these roots:
  - [x] `BlazorShop.Tests.V2/PresentationV2/Storefront/StorefrontV2WASMRuntimeFoundationTests.cs`
  - [x] `BlazorShop.Tests.V2/PresentationV2/Storefront/StorefrontComponentsHeadlessPresentationRefactorTests.cs`
  - [x] `BlazorShop.Tests.V2/PresentationV2/Storefront/StorefrontCommerceFlowCutoverTests.cs`
  - [x] `BlazorShop.Tests.V2/PresentationV2/Storefront/StorefrontPresentationCutoverGuardrailTests.cs`
- [x] Decide whether to add tests to an existing foundation test file or create a focused `StorefrontRequiredVisualContractsHardeningTests.cs`.

Definition of done:

- [x] The implementation agent has a complete source map before editing.
- [x] No unrelated root, leaf, API, or Browser controller scope has been added.

Implementation notes:

- 2026-08-09: source inventory found the planned root wiring defaults in `CartPage.razor`, `StorefrontCartView.razor`, `StorefrontCheckoutShell.razor`, and `StorefrontAccountApp.razor`. Other `StorefrontLinkContext.Default` hits are shared non-ready state components, and other account component `.Empty`/`BrowserFetch` hits are leaf components to classify in Phase 5 rather than change in the baseline phase.
- 2026-08-09: active target projects and baseline tests exist. Guardrails will be placed in a new focused `StorefrontRequiredVisualContractsHardeningTests.cs` file under `BlazorShop.Tests.V2/PresentationV2/Storefront`.

## Phase 1 - Harden `CartPage` Context Contract

Target:

- `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/Hybrid/Commerce/CartPage.razor`

Tasks:

- [x] Replace the fallback `new StorefrontCartPageContext(...)` with `default!`.
- [x] Change the parameter declaration to:

```csharp
[Parameter, EditorRequired]
public StorefrontCartPageContext Context { get; set; } = default!;
```

- [x] Add `OnParametersSet` with `ArgumentNullException.ThrowIfNull(Context);`.
- [x] Keep all current explicit `<StorefrontCartView ...>` attribute bindings:
  - [x] `InitialCart="Context.Cart"`
  - [x] `InitialAlerts="Context.Alerts"`
  - [x] `DataMode="StorefrontFeatureDataMode.InitialSnapshot"`
  - [x] `Actions="@Context.CartActions"`
  - [x] `Classes="StorefrontCartViewOptions.Classes"`
  - [x] `CheckoutUrl="@Context.CheckoutUrl"`
  - [x] `ContinueShoppingUrl="@Context.ContinueShoppingUrl"`
  - [x] `SecondaryShoppingUrl="@Context.Links.Home.Href"`
- [x] Do not introduce fallback to `StorefrontLinkContext.Default`.
- [x] Do not default to `/checkout`, `/search`, or `/`.

Tests:

- [x] Add/adjust source test proving `CartPage.razor` contains `[Parameter, EditorRequired]` for `Context`.
- [x] Add/adjust source test proving `CartPage.razor` no longer contains `new(` fallback context construction.
- [x] Add/adjust source test proving `CartPage.razor` no longer contains `StorefrontLinkContext.Default`.
- [x] Add/adjust source test proving every required `StorefrontCartView` root parameter is explicitly passed by `CartPage`.

Definition of done:

- [x] Missing cart page context fails clearly.
- [x] Cart route no longer renders with fake checkout/search/home links.
- [x] Existing cart page visual output remains behaviorally equivalent when a valid context is supplied.

Implementation notes:

- 2026-08-09: `CartPage.razor` now requires `StorefrontCartPageContext` with `[Parameter, EditorRequired]`, initializes it with `default!`, and throws clearly in `OnParametersSet` when the route context is missing. Existing explicit `StorefrontCartView` bindings are unchanged.
- 2026-08-09: added `StorefrontRequiredVisualContractsHardeningTests.CartPage_RequiresPresentationOwnedContextAndPassesCartRootContracts`. Focused test command passed: `dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~StorefrontRequiredVisualContractsHardeningTests"` with 1/1 tests passing. Known existing warnings: MessagePack NU1902/NU1903 and Browserslist.

## Phase 2 - Harden `StorefrontCartView` Root Parameters

Target:

- `BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM/Components/Cart/StorefrontCartView.razor`

Required parameters:

- `StorefrontBrowserCart? InitialCart`
- `IReadOnlyList<StorefrontBrowserCartAlert> InitialAlerts`
- `StorefrontFeatureDataMode DataMode`
- `StorefrontCartActionDescriptor Actions`
- `StorefrontCartViewClasses Classes`
- `string CheckoutUrl`
- `string ContinueShoppingUrl`
- `string SecondaryShoppingUrl`

Tasks:

- [x] Add `[EditorRequired]` to every required parameter.
- [x] Remove default assignment from `InitialAlerts`.
- [x] Remove default assignment from `DataMode`.
- [x] Remove default assignment from `Actions`.
- [x] Remove default assignment from `Classes`.
- [x] Remove hard-coded URL defaults:
  - [x] `/checkout`
  - [x] `/search`
  - [x] `/`
- [x] Keep `InitialCart` nullable but `[EditorRequired]`.
- [x] Add runtime validation in `OnParametersSet` before controller initialization:
  - [x] `ArgumentNullException.ThrowIfNull(InitialAlerts);`
  - [x] `ArgumentNullException.ThrowIfNull(Actions);`
  - [x] `ArgumentNullException.ThrowIfNull(Classes);`
  - [x] `ArgumentException.ThrowIfNullOrWhiteSpace(CheckoutUrl);`
  - [x] `ArgumentException.ThrowIfNullOrWhiteSpace(ContinueShoppingUrl);`
  - [x] `ArgumentException.ThrowIfNullOrWhiteSpace(SecondaryShoppingUrl);`
- [x] Do not throw when `InitialCart` is `null`.
- [x] Do not throw when `Actions == StorefrontCartActionDescriptor.Empty`.
- [x] Do not throw when `Classes == StorefrontCartViewClasses.Empty`.
- [x] Keep `CartController.Initialize(InitialCart, InitialAlerts, DataMode, Actions);`.
- [x] Do not move browser fetch/hydration behavior into the component.

Tests:

- [x] Add source test proving each required cart view parameter has `[EditorRequired]`.
- [x] Add source test proving cart view no longer declares URL fallbacks.
- [x] Add source test proving cart view no longer defaults to `StorefrontFeatureDataMode.BrowserFetch`.
- [x] Add source test proving cart view no longer defaults action/classes to `.Empty`.
- [x] Add source test proving runtime null/whitespace validation exists for required references and URLs.
- [x] Add source test proving `.Empty` is not rejected by validation logic.

Definition of done:

- [x] Cart root callers must explicitly choose data mode, action descriptor, classes, and URLs.
- [x] Component still supports `InitialCart = null` as an intentional empty/unknown cart state.
- [x] Component does not own fallback navigation routes.

Implementation notes:

- 2026-08-09: `StorefrontCartView.razor` now marks all root wiring parameters `[EditorRequired]`, removes browser-fetch/action/class/URL defaults, keeps nullable `InitialCart`, and validates required reference/URL inputs before `CartController.Initialize(...)`.
- 2026-08-09: expanded `StorefrontRequiredVisualContractsHardeningTests` with cart view guardrails. Focused test command passed 2/2 with the same known MessagePack and Browserslist warnings.

## Phase 3 - Harden `StorefrontCheckoutShell` Root Parameters

Target:

- `BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM/Components/Checkout/StorefrontCheckoutShell.razor`

Required parameters:

- `StorefrontBrowserCheckoutState InitialState`
- `bool ShowPanel`
- `StorefrontFeatureDataMode DataMode`
- `StorefrontCheckoutActionDescriptor Actions`
- `StorefrontCheckoutViewClasses Classes`

Tasks:

- [x] Add `[EditorRequired]` to every required parameter.
- [x] Remove default `StorefrontBrowserCheckoutDefaults.EmptyState("Checkout is not available yet.")`.
- [x] Remove default `ShowPanel = true`.
- [x] Remove default `DataMode = StorefrontFeatureDataMode.BrowserFetch`.
- [x] Remove default `Actions = StorefrontCheckoutActionDescriptor.Empty`.
- [x] Remove default `Classes = StorefrontCheckoutViewClasses.Empty`.
- [x] Add runtime validation in `OnParametersSet` before controller initialization:
  - [x] `ArgumentNullException.ThrowIfNull(InitialState);`
  - [x] `ArgumentNullException.ThrowIfNull(Actions);`
  - [x] `ArgumentNullException.ThrowIfNull(Classes);`
- [x] Do not reject `StorefrontCheckoutActionDescriptor.Empty`.
- [x] Do not reject `StorefrontCheckoutViewClasses.Empty`.
- [x] Keep `CheckoutController.Initialize(InitialState, ShowPanel, DataMode, Actions);`.
- [x] Keep existing `OnAfterRenderAsync` hydration condition behavior unless tests prove it depends on removed defaults.

Callsite checks:

- [x] Verify `CheckoutPage.razor` passes `InitialState`.
- [x] Verify `CheckoutPage.razor` passes `ShowPanel`.
- [x] Verify `CheckoutPage.razor` passes `DataMode`.
- [x] Verify `CheckoutPage.razor` passes `Actions`.
- [x] Verify `CheckoutPage.razor` passes `Classes`.
- [x] Add `ArgumentNullException.ThrowIfNull(Context);` to `CheckoutPage.razor` for consistency if not already present.

Tests:

- [x] Add source test proving each checkout shell parameter has `[EditorRequired]`.
- [x] Add source test proving the fake empty checkout state default is removed.
- [x] Add source test proving checkout shell no longer defaults to browser fetch or `.Empty`.
- [x] Add source test proving `CheckoutPage.razor` explicitly passes all required shell parameters.
- [x] Add source test proving `CheckoutPage.razor` has required context and null guard.

Definition of done:

- [x] Checkout shell can no longer hide missing route-owned checkout state.
- [x] Checkout page continues to choose `InitialSnapshot` and `ShowPanel=false` explicitly.
- [x] Checkout hydration behavior remains unchanged for valid root wiring.

Implementation notes:

- 2026-08-09: `StorefrontCheckoutShell.razor` now requires `InitialState`, `ShowPanel`, `DataMode`, `Actions`, and `Classes` from the host and validates required references before controller initialization. The hydration condition remains based on `ShowPanel`, first render, browser runtime, and non-`InitialSnapshot` mode.
- 2026-08-09: `CheckoutPage.razor` already passed every checkout shell contract in both render branches and now also guards missing `Context` in `OnParametersSet`.
- 2026-08-09: expanded required visual contract tests for checkout and updated the existing foundation assertion that previously expected `ShowPanel=true`. Focused command passed 23/23 for `StorefrontRequiredVisualContractsHardeningTests|StorefrontV2WASMRuntimeFoundationTests`.

## Phase 4 - Harden `StorefrontAccountApp` Root Parameters

Target:

- `BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM/Components/Account/StorefrontAccountApp.razor`

Required parameters:

- `string? Path`
- `int PageNumber`
- `string? AntiforgeryFieldName`
- `string? AntiforgeryRequestToken`
- `IReadOnlyList<AccountNavigationItem> NavigationItems`
- `AccountRouteDescriptor RouteDescriptor`
- `AccountNavigationClasses NavigationClasses`
- `StorefrontAccountProfileActionDescriptor ProfileActions`
- `StorefrontAccountPasswordActionDescriptor PasswordActions`
- `StorefrontAccountFormClasses AccountFormClasses`
- `StorefrontAccountAddressActionDescriptor AddressActions`
- `StorefrontAccountAddressBookClasses AddressClasses`
- `StorefrontAccountOrderActionDescriptor OrderActions`
- `StorefrontAccountOrderListClasses OrderListClasses`
- `StorefrontAccountOrderDetailClasses OrderDetailClasses`
- `StorefrontAccountShellClasses ShellClasses`

Optional parameters:

- `string? Error`
- `string? Saved`

Tasks:

- [x] Add `[EditorRequired]` to every required account root parameter.
- [x] Keep `Path` nullable but required by presence.
- [x] Keep `AntiforgeryFieldName` nullable but required by presence.
- [x] Keep `AntiforgeryRequestToken` nullable but required by presence.
- [x] Remove `PageNumber = 1` default from `StorefrontAccountApp`.
- [x] Move the page-number default upstream if needed:
  - [x] Confirm `AccountHostPage.razor` currently passes `Context.PageNumber`.
  - [x] Confirm the Presentation/page context already normalizes or supplies `PageNumber`.
  - [x] If upstream does not normalize, add normalization in the route/page context creation layer, not in `StorefrontAccountApp`.
- [x] Remove default `NavigationItems = []`.
- [x] Remove default `RouteDescriptor = AccountRouteDescriptor.Empty`.
- [x] Remove default visual/action `.Empty` assignments for required root contracts.
- [x] Add runtime validation in `OnParametersSet`:
  - [x] `ArgumentNullException.ThrowIfNull(NavigationItems);`
  - [x] `ArgumentNullException.ThrowIfNull(RouteDescriptor);`
  - [x] `ArgumentNullException.ThrowIfNull(NavigationClasses);`
  - [x] `ArgumentNullException.ThrowIfNull(ProfileActions);`
  - [x] `ArgumentNullException.ThrowIfNull(PasswordActions);`
  - [x] `ArgumentNullException.ThrowIfNull(AccountFormClasses);`
  - [x] `ArgumentNullException.ThrowIfNull(AddressActions);`
  - [x] `ArgumentNullException.ThrowIfNull(AddressClasses);`
  - [x] `ArgumentNullException.ThrowIfNull(OrderActions);`
  - [x] `ArgumentNullException.ThrowIfNull(OrderListClasses);`
  - [x] `ArgumentNullException.ThrowIfNull(OrderDetailClasses);`
  - [x] `ArgumentNullException.ThrowIfNull(ShellClasses);`
- [x] Do not throw when `Path` is `null`.
- [x] Do not throw when antiforgery values are `null`.
- [x] Do not throw when descriptors/classes are `.Empty`.
- [x] Keep `Error` and `Saved` optional.
- [x] Keep child component `StorefrontFeatureDataMode.BrowserFetch` behavior unless a separate phase explicitly makes child data modes host-configurable.

Callsite checks:

- [x] Verify `AccountHostPage.razor` passes `Path`.
- [x] Verify `AccountHostPage.razor` passes `PageNumber`.
- [x] Verify `AccountHostPage.razor` passes antiforgery field/token values.
- [x] Verify `AccountHostPage.razor` passes navigation items and route descriptor.
- [x] Verify `AccountHostPage.razor` passes all account class/action descriptors.
- [x] Add `ArgumentNullException.ThrowIfNull(Context);` to `AccountHostPage.razor` for consistency if not already present.

Tests:

- [x] Add source test proving each required account app parameter has `[EditorRequired]`.
- [x] Add source test proving `Error` and `Saved` are not marked required.
- [x] Add source test proving account app no longer defaults `PageNumber = 1`.
- [x] Add source test proving account app no longer defaults route/navigation/action/class contracts.
- [x] Add source test proving nullable required-presence parameters are not runtime rejected.
- [x] Add source test proving `AccountHostPage.razor` passes every required root parameter.

Definition of done:

- [x] Account root caller owns route and form wiring explicitly.
- [x] Account root no longer silently turns missing page number into page 1.
- [x] Account route parsing still receives the host-provided route descriptor.

Implementation notes:

- 2026-08-09: `StorefrontAccountApp.razor` now requires every root route/form/navigation/action/class contract by presence, while leaving `Error` and `Saved` optional.
- 2026-08-09: nullable presence parameters (`Path`, `AntiforgeryFieldName`, `AntiforgeryRequestToken`) remain nullable and are not runtime-rejected; required reference contracts are guarded before account route resolution.
- 2026-08-09: page number normalization already lives upstream in `StorefrontAccountPageService`, so `StorefrontAccountApp` now consumes the host-provided page number directly.
- 2026-08-09: expanded required visual contract tests for account root and host page wiring. Focused command passed 25/25 for `StorefrontRequiredVisualContractsHardeningTests|StorefrontV2WASMRuntimeFoundationTests`.

## Phase 5 - Foundation Root Audit

Purpose:

Confirm no other V2 foundation root has the same anti-pattern before closing the phase.

Tasks:

- [x] Search V2 root pages and host components for fallback page contexts:
  - [x] `rg -n "public .*Context .* = new\\(" BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages`
  - [x] `rg -n "StorefrontLinkContext.Default|AccountRouteDescriptor.Empty|ActionDescriptor.Empty|Classes.Empty" BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages`
- [x] Search V2.WASM root components for hard-coded root wiring defaults:
  - [x] `rg -n "DataMode .* = StorefrontFeatureDataMode.BrowserFetch" BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM/Components`
  - [x] `rg -n "ActionDescriptor.Empty|Classes.Empty|= \"/checkout\"|= \"/search\"|= \"/\"" BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM/Components`
- [x] Classify each hit:
  - [x] Root application wiring fallback: fix in this phase.
  - [x] Leaf visual compatibility default: leave unchanged unless it blocks root hardening.
  - [x] Test fixture or options class: leave unchanged unless it masks root behavior.
- [x] If another root application wiring fallback is found, add it to this file before implementing it.
- [x] Do not expand to all leaf components.

Definition of done:

- [x] The plan closes all root-level application wiring defaults found in V2/V2.WASM.
- [x] Any remaining defaults are deliberately classified as leaf/UI optional defaults or out of scope.

Implementation notes:

- 2026-08-09: V2 Pages scans returned no fallback page-context construction and no root `StorefrontLinkContext.Default`/`.Empty` hits.
- 2026-08-09: V2.WASM `DataMode = StorefrontFeatureDataMode.BrowserFetch` hits remain only in account leaf components (`StorefrontAccountChangePasswordForm`, `StorefrontAccountAddressBook`, `StorefrontAccountOrderList`, `StorefrontAccountProfileEditor`, `StorefrontAccountOrderDetail`).
- 2026-08-09: remaining `.Empty` hits are static factory/property definitions, CSS-class property references such as `Classes.EmptyState`, or account leaf component compatibility defaults. No additional root application wiring fallback was found.

## Phase 6 - Source Guardrail Tests

Preferred location:

- Add focused tests to `BlazorShop.Tests.V2/PresentationV2/Storefront/StorefrontV2WASMRuntimeFoundationTests.cs`, or create `StorefrontRequiredVisualContractsHardeningTests.cs` under the same folder if the existing file becomes too broad.

Required assertions:

- [x] `CartPage.razor`:
  - [x] `Context` has `[Parameter, EditorRequired]`.
  - [x] `Context` is initialized with `default!`.
  - [x] `ArgumentNullException.ThrowIfNull(Context)` exists.
  - [x] No fallback `new StorefrontCartPageContext(...)` exists.
  - [x] No `StorefrontLinkContext.Default` fallback exists.
  - [x] Required `StorefrontCartView` attributes are passed explicitly.
- [x] `StorefrontCartView.razor`:
  - [x] Required parameters have `[EditorRequired]`.
  - [x] URL defaults are absent.
  - [x] Browser fetch default is absent.
  - [x] action/classes `.Empty` defaults are absent.
  - [x] Required reference and URL validation exists.
  - [x] `.Empty` values are not rejected.
- [x] `StorefrontCheckoutShell.razor`:
  - [x] Required parameters have `[EditorRequired]`.
  - [x] fake empty checkout state default is absent.
  - [x] `ShowPanel`, `DataMode`, `Actions`, and `Classes` defaults are absent.
  - [x] Required reference validation exists.
  - [x] `.Empty` values are not rejected.
- [x] `CheckoutPage.razor`:
  - [x] Required shell attributes are passed explicitly in every shell render branch.
  - [x] Context is required and guarded.
- [x] `StorefrontAccountApp.razor`:
  - [x] Required parameters have `[EditorRequired]`.
  - [x] `Error` and `Saved` remain optional.
  - [x] `PageNumber = 1` default is absent.
  - [x] route/navigation/action/class defaults are absent.
  - [x] required reference validation exists.
  - [x] nullable required-presence parameters are not runtime rejected.
- [x] `AccountHostPage.razor`:
  - [x] Required account app attributes are passed explicitly.
  - [x] Context is required and guarded.
- [x] Broad guardrail:
  - [x] V2 root pages do not create fallback `Storefront*PageContext` instances.
  - [x] V2.WASM root components do not hard-code cart/checkout/account route/action/class defaults.

Definition of done:

- [x] Tests fail if a future agent reintroduces silent root application wiring defaults.
- [x] Tests distinguish root wiring defaults from acceptable optional/leaf UI defaults.

Implementation notes:

- 2026-08-09: `StorefrontRequiredVisualContractsHardeningTests` now covers cart, checkout, and account root required parameters, explicit host callsites, runtime guards, and removed root defaults.
- 2026-08-09: added broad source guardrails for all V2 page `.razor` files and the three V2.WASM root components while leaving leaf compatibility defaults out of scope.
- 2026-08-09: focused command passed 27/27 for `StorefrontRequiredVisualContractsHardeningTests|StorefrontV2WASMRuntimeFoundationTests`.

## Phase 7 - Compile And Focused Test Verification

Commands:

```powershell
dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~StorefrontV2WASMRuntimeFoundationTests|FullyQualifiedName~StorefrontRequiredVisualContractsHardeningTests"
```

If a new focused test class is not created, run the existing foundation tests:

```powershell
dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~StorefrontV2WASMRuntimeFoundationTests"
```

Then run compile/build coverage:

```powershell
dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.V2/BlazorShop.Storefront.V2.csproj --no-restore
dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM/BlazorShop.Storefront.V2.WASM.csproj --no-restore
dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~Storefront"
```

Starter check, only if shared references or shared tests are affected:

```powershell
dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/BlazorShop.Storefront.Starter.csproj --no-restore
dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.Starter.WASM/BlazorShop.Storefront.Starter.WASM.csproj --no-restore
```

Definition of done:

- [x] V2 builds.
- [x] V2.WASM builds.
- [x] Focused Storefront tests pass.
- [x] No unintended Starter breakage if shared contracts/tests were touched.

Implementation notes:

- 2026-08-09: focused command passed 27/27 for `StorefrontRequiredVisualContractsHardeningTests|StorefrontV2WASMRuntimeFoundationTests`.
- 2026-08-09: `dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.V2/BlazorShop.Storefront.V2.csproj --no-restore` passed with 0 warnings/errors.
- 2026-08-09: `dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM/BlazorShop.Storefront.V2.WASM.csproj --no-restore` passed with 0 warnings/errors.
- 2026-08-09: broad `FullyQualifiedName~Storefront` slice ran 942 tests: 933 passed, 2 skipped, 7 failed from existing out-of-scope historical doc/Starter/package-boundary assertions. The failures do not reference the required visual contract changes.
- 2026-08-09: Starter and Starter.WASM builds passed with 0 warnings/errors, confirming no compile breakage from this hardening.

## Phase 8 - Browser Regression QA

Use the preferred local V2 runner if browser behavior is changed:

```powershell
.\scripts\run-v2-local.ps1 -StopExisting
```

Required Playwright browser checks:

- [x] Cart route:
  - [x] Navigate to cart route.
  - [x] Confirm cart root renders from provided server context.
  - [x] Confirm continue-shopping link is present and uses host-provided URL.
  - [x] Confirm checkout link/button uses host-provided URL.
  - [x] Confirm no console error from missing root parameters.
- [x] Checkout route:
  - [x] Navigate to checkout route with valid cart/session fixture.
  - [x] Confirm checkout shell renders from provided `InitialState`.
  - [x] Confirm `ShowPanel=false` route mode still renders expected page layout.
  - [x] Confirm no fake "Checkout is not available yet." fallback appears for a valid context.
  - [x] Confirm no console error from missing root parameters.
- [x] Account route:
  - [x] Navigate to account profile route.
  - [x] Confirm account navigation renders from host-provided `NavigationItems`.
  - [x] Confirm active route resolves from host-provided `RouteDescriptor`.
  - [x] Confirm page number behavior is host/context-owned.
  - [x] Confirm no console error from missing root parameters.
- [x] Negative/development evidence:
  - [x] Confirm removing a required root attribute in a temporary local test produces compile/analyzer failure or a clear runtime null guard failure.
  - [x] Revert the temporary negative change before committing.

Definition of done:

- [x] Cart, checkout, and account browser flows render with real host wiring.
- [x] No route falls back to fake context/state.
- [x] No hydration/runtime console error is introduced.

Implementation notes:

- 2026-08-09: `.\scripts\run-v2-local.ps1 -StopExisting -NoOpenBrowser` started the local V2 stack at `http://localhost:18598`.
- 2026-08-09: Playwright evidence passed at `output/playwright/storefront-required-visual-contracts-hardening-phase8/evidence.json`; screenshots were captured for `cart-empty.png`, `cart-with-item.png`, `checkout.png`, and `account-profile.png`.
- 2026-08-09: cart browser checks verified `/my-cart` empty state receives host-provided `ContinueShoppingUrl=/search` and `Links.Home.Href=/`, then after adding `qa-simple-product-100`, checkout navigation receives `CheckoutUrl=/checkout`.
- 2026-08-09: checkout browser check used a valid cart fixture and verified SSR checkout layout, form, address inputs, `/api/checkout` state with checkout/session versions, and no fake `Checkout is not available yet.` fallback. The visible checkout shell panel is intentionally hidden because `CheckoutPage.razor` passes `ShowPanel=false`; source guardrails prove `InitialState` is still supplied.
- 2026-08-09: account browser check signed in and verified `/account/profile` renders `[data-storefront-account-app]` with navigation items `Profile`, `Orders`, `Addresses`, and `Password`.
- 2026-08-09: negative development check temporarily removed `CheckoutUrl` from `CartPage.razor`; `dotnet build` emitted `RZ2012` for missing required `StorefrontCartView.CheckoutUrl`. The temporary edit was reverted before committing. The same build also failed to copy the running Storefront exe because local QA was active, which is unrelated to the analyzer evidence.

## Phase 9 - QA Checklist And Documentation Update

Target QA file:

- `docs/refactor-control-Commerce-storefront/QA-StorefrontV2.todo.md`

Tasks:

- [x] Add or update QA checklist items for root visual contract hardening:
  - [x] Cart page must receive `StorefrontCartPageContext` from Presentation route context.
  - [x] Cart view must receive explicit data mode, action descriptor, classes, and URLs.
  - [x] Checkout shell must receive explicit initial state, data mode, actions, classes, and panel mode.
  - [x] Account app must receive explicit route/navigation/action/class descriptors and page number.
  - [x] Missing root contracts must fail clearly during development.
- [x] Add note that `.Empty` descriptors may be intentionally passed by callers and are not validation failures.
- [x] Add browser QA cases for cart/checkout/account after hardening.
- [x] Do not update architecture docs unless implementation discovers a boundary rule not already captured by `docs/architecture/05-project-and-folder-guide.md`.

Definition of done:

- [x] QA checklist reflects this hardening so future release checks cover it.
- [x] Documentation does not imply leaf components must be swept in this phase.

Implementation notes:

- 2026-08-09: updated `QA-StorefrontV2.todo.md` with root visual contract static/build expectations and browser QA evidence for cart, checkout, and account.
- 2026-08-09: no architecture docs were updated because no new boundary rule was discovered; the change applies existing Presentation-owned route context and V2-owned visual root boundaries.

## Phase 10 - Final Closure Checklist

Before closing the implementation commit:

- [ ] `rg -n "StorefrontLinkContext.Default" BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/Hybrid/Commerce/CartPage.razor` returns no result.
- [ ] `rg -n "new\\(" BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/Hybrid/Commerce/CartPage.razor` does not show fallback context construction.
- [ ] `rg -n "StorefrontFeatureDataMode.BrowserFetch|ActionDescriptor.Empty|Classes.Empty|= \"/checkout\"|= \"/search\"|= \"/\"" BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM/Components/Cart/StorefrontCartView.razor` returns no root parameter default.
- [ ] `rg -n "EmptyState\\(|StorefrontFeatureDataMode.BrowserFetch|ActionDescriptor.Empty|Classes.Empty" BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM/Components/Checkout/StorefrontCheckoutShell.razor` returns no root parameter default.
- [ ] `rg -n "PageNumber .* = 1|AccountRouteDescriptor.Empty|ActionDescriptor.Empty|Classes.Empty|NavigationItems .* = \\[\\]" BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM/Components/Account/StorefrontAccountApp.razor` returns no root parameter default.
- [ ] Focused Storefront tests pass.
- [ ] V2 and V2.WASM build pass.
- [ ] Browser cart/checkout/account regression is captured.
- [ ] `QA-StorefrontV2.todo.md` updated.
- [ ] `git diff` shows only V2/V2.WASM/tests/QA docs changes expected by this plan.

Completion criteria:

- [ ] Root visual components no longer silently own application wiring defaults.
- [ ] Valid host-provided wiring still renders cart, checkout, and account flows.
- [ ] Guardrail tests prevent regression.
- [ ] No unrelated StorefrontBuilder, Commerce Node, Control Plane, Runtime, Client, or Browser controller behavior changed.

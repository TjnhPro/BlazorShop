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

- [ ] Add `[EditorRequired]` to every required parameter.
- [ ] Remove default `StorefrontBrowserCheckoutDefaults.EmptyState("Checkout is not available yet.")`.
- [ ] Remove default `ShowPanel = true`.
- [ ] Remove default `DataMode = StorefrontFeatureDataMode.BrowserFetch`.
- [ ] Remove default `Actions = StorefrontCheckoutActionDescriptor.Empty`.
- [ ] Remove default `Classes = StorefrontCheckoutViewClasses.Empty`.
- [ ] Add runtime validation in `OnParametersSet` before controller initialization:
  - [ ] `ArgumentNullException.ThrowIfNull(InitialState);`
  - [ ] `ArgumentNullException.ThrowIfNull(Actions);`
  - [ ] `ArgumentNullException.ThrowIfNull(Classes);`
- [ ] Do not reject `StorefrontCheckoutActionDescriptor.Empty`.
- [ ] Do not reject `StorefrontCheckoutViewClasses.Empty`.
- [ ] Keep `CheckoutController.Initialize(InitialState, ShowPanel, DataMode, Actions);`.
- [ ] Keep existing `OnAfterRenderAsync` hydration condition behavior unless tests prove it depends on removed defaults.

Callsite checks:

- [ ] Verify `CheckoutPage.razor` passes `InitialState`.
- [ ] Verify `CheckoutPage.razor` passes `ShowPanel`.
- [ ] Verify `CheckoutPage.razor` passes `DataMode`.
- [ ] Verify `CheckoutPage.razor` passes `Actions`.
- [ ] Verify `CheckoutPage.razor` passes `Classes`.
- [ ] Add `ArgumentNullException.ThrowIfNull(Context);` to `CheckoutPage.razor` for consistency if not already present.

Tests:

- [ ] Add source test proving each checkout shell parameter has `[EditorRequired]`.
- [ ] Add source test proving the fake empty checkout state default is removed.
- [ ] Add source test proving checkout shell no longer defaults to browser fetch or `.Empty`.
- [ ] Add source test proving `CheckoutPage.razor` explicitly passes all required shell parameters.
- [ ] Add source test proving `CheckoutPage.razor` has required context and null guard.

Definition of done:

- [ ] Checkout shell can no longer hide missing route-owned checkout state.
- [ ] Checkout page continues to choose `InitialSnapshot` and `ShowPanel=false` explicitly.
- [ ] Checkout hydration behavior remains unchanged for valid root wiring.

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

- [ ] Add `[EditorRequired]` to every required account root parameter.
- [ ] Keep `Path` nullable but required by presence.
- [ ] Keep `AntiforgeryFieldName` nullable but required by presence.
- [ ] Keep `AntiforgeryRequestToken` nullable but required by presence.
- [ ] Remove `PageNumber = 1` default from `StorefrontAccountApp`.
- [ ] Move the page-number default upstream if needed:
  - [ ] Confirm `AccountHostPage.razor` currently passes `Context.PageNumber`.
  - [ ] Confirm the Presentation/page context already normalizes or supplies `PageNumber`.
  - [ ] If upstream does not normalize, add normalization in the route/page context creation layer, not in `StorefrontAccountApp`.
- [ ] Remove default `NavigationItems = []`.
- [ ] Remove default `RouteDescriptor = AccountRouteDescriptor.Empty`.
- [ ] Remove default visual/action `.Empty` assignments for required root contracts.
- [ ] Add runtime validation in `OnParametersSet`:
  - [ ] `ArgumentNullException.ThrowIfNull(NavigationItems);`
  - [ ] `ArgumentNullException.ThrowIfNull(RouteDescriptor);`
  - [ ] `ArgumentNullException.ThrowIfNull(NavigationClasses);`
  - [ ] `ArgumentNullException.ThrowIfNull(ProfileActions);`
  - [ ] `ArgumentNullException.ThrowIfNull(PasswordActions);`
  - [ ] `ArgumentNullException.ThrowIfNull(AccountFormClasses);`
  - [ ] `ArgumentNullException.ThrowIfNull(AddressActions);`
  - [ ] `ArgumentNullException.ThrowIfNull(AddressClasses);`
  - [ ] `ArgumentNullException.ThrowIfNull(OrderActions);`
  - [ ] `ArgumentNullException.ThrowIfNull(OrderListClasses);`
  - [ ] `ArgumentNullException.ThrowIfNull(OrderDetailClasses);`
  - [ ] `ArgumentNullException.ThrowIfNull(ShellClasses);`
- [ ] Do not throw when `Path` is `null`.
- [ ] Do not throw when antiforgery values are `null`.
- [ ] Do not throw when descriptors/classes are `.Empty`.
- [ ] Keep `Error` and `Saved` optional.
- [ ] Keep child component `StorefrontFeatureDataMode.BrowserFetch` behavior unless a separate phase explicitly makes child data modes host-configurable.

Callsite checks:

- [ ] Verify `AccountHostPage.razor` passes `Path`.
- [ ] Verify `AccountHostPage.razor` passes `PageNumber`.
- [ ] Verify `AccountHostPage.razor` passes antiforgery field/token values.
- [ ] Verify `AccountHostPage.razor` passes navigation items and route descriptor.
- [ ] Verify `AccountHostPage.razor` passes all account class/action descriptors.
- [ ] Add `ArgumentNullException.ThrowIfNull(Context);` to `AccountHostPage.razor` for consistency if not already present.

Tests:

- [ ] Add source test proving each required account app parameter has `[EditorRequired]`.
- [ ] Add source test proving `Error` and `Saved` are not marked required.
- [ ] Add source test proving account app no longer defaults `PageNumber = 1`.
- [ ] Add source test proving account app no longer defaults route/navigation/action/class contracts.
- [ ] Add source test proving nullable required-presence parameters are not runtime rejected.
- [ ] Add source test proving `AccountHostPage.razor` passes every required root parameter.

Definition of done:

- [ ] Account root caller owns route and form wiring explicitly.
- [ ] Account root no longer silently turns missing page number into page 1.
- [ ] Account route parsing still receives the host-provided route descriptor.

## Phase 5 - Foundation Root Audit

Purpose:

Confirm no other V2 foundation root has the same anti-pattern before closing the phase.

Tasks:

- [ ] Search V2 root pages and host components for fallback page contexts:
  - [ ] `rg -n "public .*Context .* = new\\(" BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages`
  - [ ] `rg -n "StorefrontLinkContext.Default|AccountRouteDescriptor.Empty|ActionDescriptor.Empty|Classes.Empty" BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages`
- [ ] Search V2.WASM root components for hard-coded root wiring defaults:
  - [ ] `rg -n "DataMode .* = StorefrontFeatureDataMode.BrowserFetch" BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM/Components`
  - [ ] `rg -n "ActionDescriptor.Empty|Classes.Empty|= \"/checkout\"|= \"/search\"|= \"/\"" BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM/Components`
- [ ] Classify each hit:
  - [ ] Root application wiring fallback: fix in this phase.
  - [ ] Leaf visual compatibility default: leave unchanged unless it blocks root hardening.
  - [ ] Test fixture or options class: leave unchanged unless it masks root behavior.
- [ ] If another root application wiring fallback is found, add it to this file before implementing it.
- [ ] Do not expand to all leaf components.

Definition of done:

- [ ] The plan closes all root-level application wiring defaults found in V2/V2.WASM.
- [ ] Any remaining defaults are deliberately classified as leaf/UI optional defaults or out of scope.

## Phase 6 - Source Guardrail Tests

Preferred location:

- Add focused tests to `BlazorShop.Tests.V2/PresentationV2/Storefront/StorefrontV2WASMRuntimeFoundationTests.cs`, or create `StorefrontRequiredVisualContractsHardeningTests.cs` under the same folder if the existing file becomes too broad.

Required assertions:

- [ ] `CartPage.razor`:
  - [ ] `Context` has `[Parameter, EditorRequired]`.
  - [ ] `Context` is initialized with `default!`.
  - [ ] `ArgumentNullException.ThrowIfNull(Context)` exists.
  - [ ] No fallback `new StorefrontCartPageContext(...)` exists.
  - [ ] No `StorefrontLinkContext.Default` fallback exists.
  - [ ] Required `StorefrontCartView` attributes are passed explicitly.
- [ ] `StorefrontCartView.razor`:
  - [ ] Required parameters have `[EditorRequired]`.
  - [ ] URL defaults are absent.
  - [ ] Browser fetch default is absent.
  - [ ] action/classes `.Empty` defaults are absent.
  - [ ] Required reference and URL validation exists.
  - [ ] `.Empty` values are not rejected.
- [ ] `StorefrontCheckoutShell.razor`:
  - [ ] Required parameters have `[EditorRequired]`.
  - [ ] fake empty checkout state default is absent.
  - [ ] `ShowPanel`, `DataMode`, `Actions`, and `Classes` defaults are absent.
  - [ ] Required reference validation exists.
  - [ ] `.Empty` values are not rejected.
- [ ] `CheckoutPage.razor`:
  - [ ] Required shell attributes are passed explicitly in every shell render branch.
  - [ ] Context is required and guarded.
- [ ] `StorefrontAccountApp.razor`:
  - [ ] Required parameters have `[EditorRequired]`.
  - [ ] `Error` and `Saved` remain optional.
  - [ ] `PageNumber = 1` default is absent.
  - [ ] route/navigation/action/class defaults are absent.
  - [ ] required reference validation exists.
  - [ ] nullable required-presence parameters are not runtime rejected.
- [ ] `AccountHostPage.razor`:
  - [ ] Required account app attributes are passed explicitly.
  - [ ] Context is required and guarded.
- [ ] Broad guardrail:
  - [ ] V2 root pages do not create fallback `Storefront*PageContext` instances.
  - [ ] V2.WASM root components do not hard-code cart/checkout/account route/action/class defaults.

Definition of done:

- [ ] Tests fail if a future agent reintroduces silent root application wiring defaults.
- [ ] Tests distinguish root wiring defaults from acceptable optional/leaf UI defaults.

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

- [ ] V2 builds.
- [ ] V2.WASM builds.
- [ ] Focused Storefront tests pass.
- [ ] No unintended Starter breakage if shared contracts/tests were touched.

## Phase 8 - Browser Regression QA

Use the preferred local V2 runner if browser behavior is changed:

```powershell
.\scripts\run-v2-local.ps1 -StopExisting
```

Required Playwright browser checks:

- [ ] Cart route:
  - [ ] Navigate to cart route.
  - [ ] Confirm cart root renders from provided server context.
  - [ ] Confirm continue-shopping link is present and uses host-provided URL.
  - [ ] Confirm checkout link/button uses host-provided URL.
  - [ ] Confirm no console error from missing root parameters.
- [ ] Checkout route:
  - [ ] Navigate to checkout route with valid cart/session fixture.
  - [ ] Confirm checkout shell renders from provided `InitialState`.
  - [ ] Confirm `ShowPanel=false` route mode still renders expected page layout.
  - [ ] Confirm no fake "Checkout is not available yet." fallback appears for a valid context.
  - [ ] Confirm no console error from missing root parameters.
- [ ] Account route:
  - [ ] Navigate to account profile route.
  - [ ] Confirm account navigation renders from host-provided `NavigationItems`.
  - [ ] Confirm active route resolves from host-provided `RouteDescriptor`.
  - [ ] Confirm page number behavior is host/context-owned.
  - [ ] Confirm no console error from missing root parameters.
- [ ] Negative/development evidence:
  - [ ] Confirm removing a required root attribute in a temporary local test produces compile/analyzer failure or a clear runtime null guard failure.
  - [ ] Revert the temporary negative change before committing.

Definition of done:

- [ ] Cart, checkout, and account browser flows render with real host wiring.
- [ ] No route falls back to fake context/state.
- [ ] No hydration/runtime console error is introduced.

## Phase 9 - QA Checklist And Documentation Update

Target QA file:

- `docs/refactor-control-Commerce-storefront/QA-StorefrontV2.todo.md`

Tasks:

- [ ] Add or update QA checklist items for root visual contract hardening:
  - [ ] Cart page must receive `StorefrontCartPageContext` from Presentation route context.
  - [ ] Cart view must receive explicit data mode, action descriptor, classes, and URLs.
  - [ ] Checkout shell must receive explicit initial state, data mode, actions, classes, and panel mode.
  - [ ] Account app must receive explicit route/navigation/action/class descriptors and page number.
  - [ ] Missing root contracts must fail clearly during development.
- [ ] Add note that `.Empty` descriptors may be intentionally passed by callers and are not validation failures.
- [ ] Add browser QA cases for cart/checkout/account after hardening.
- [ ] Do not update architecture docs unless implementation discovers a boundary rule not already captured by `docs/architecture/05-project-and-folder-guide.md`.

Definition of done:

- [ ] QA checklist reflects this hardening so future release checks cover it.
- [ ] Documentation does not imply leaf components must be swept in this phase.

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

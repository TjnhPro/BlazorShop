# Storefront Cart Checkout WasmHost Extraction

Status: complete
Track: Phase 3.5 - Cart and Checkout WasmHost Extraction
Target area: Storefront component mode architecture

## Purpose

Move cart and checkout browser-interactive component implementation out of `BlazorShop.Storefront.V2.WASM` into the reusable WasmHost layer without changing current storefront behavior.

The goal is not to redesign cart or checkout. The goal is to make ownership clear:

- `BlazorShop.Storefront.Components` owns browser-safe contracts, labels, state contracts, and class contract shapes.
- `BlazorShop.Storefront.Components.WasmHost` owns reusable browser interaction components that call `BlazorShop.Storefront.Browser`.
- `BlazorShop.Storefront.V2.WASM` owns V2 wrappers, V2 options, V2 labels, and final Tailwind class values.
- `BlazorShop.Storefront.V2` owns page placement, render mode, V2 layout, and the current checkout SSR form flow.

## Autoplan Review Summary

CEO review:

- The scope is valid because it removes a real architecture boundary leak without expanding product behavior.
- The implementation must stay mechanical and avoid account/header/product extraction in this phase.
- The checkout page must not be rewritten to make the extracted shell the visible place-order UI unless that is approved as a separate behavior phase.

Design review:

- No customer-facing layout redesign is allowed.
- Cart and checkout visual output must remain owned by V2, not by shared WasmHost.
- Shared components may render semantic hooks and bind provided class/label contracts, but must not introduce final V2 copy, final Tailwind values, or new DOM/layout opinions beyond the current migrated structure.

Engineering review:

- The class contract move is mandatory, not optional.
- WasmHost must own controller injection and lifecycle logic.
- V2.WASM must stop owning runtime behavior and should keep only thin wrappers/options.
- Tests must be rewritten where they currently assert V2.WASM owns controller behavior.
- Checkout browser E2E must respect the current `ShowPanel=false` page behavior.

DX review:

- Agents must be able to tell from folder names which project owns contracts, runtime behavior, wrappers, and page placement.
- The plan must include exact verification commands, guardrails, and stop conditions so a later implementation session does not need a cleanup phase.
- Browser QA must be real Playwright flow testing, not only smoke tests.

## Current Code Evidence

- [x] `BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM/Components/Cart/StorefrontCartView.razor` currently owns cart browser behavior.
- [x] `StorefrontCartView.razor` injects `IStorefrontBrowserCartController`.
- [x] `StorefrontCartView.razor` owns lifecycle methods such as `Initialize`, `HydrateAsync`, `UpdateQuantityAsync`, `RemoveLineAsync`, and `ClearAsync`.
- [x] `StorefrontCartView.razor` renders cart semantic hooks such as `data-storefront-cart-quantity`, `data-line-id`, `data-product-id`, `data-variant-id`, `data-product-name`, `data-size-value`, `data-storefront-cart-remove`, and `data-storefront-cart-clear`.
- [x] `StorefrontCartView.razor` currently includes hardcoded English copy such as cart title, empty cart text, button text, and loading/error text.
- [x] `BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM/Components/Cart/StorefrontCartViewClasses.cs` currently owns the cart class contract shape.
- [x] `BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM/Components/Cart/StorefrontCartViewOptions.cs` currently owns final V2 class values.
- [x] `BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM/Components/Checkout/StorefrontCheckoutShell.razor` currently owns checkout browser shell behavior.
- [x] `StorefrontCheckoutShell.razor` injects `IStorefrontBrowserCheckoutController` and `NavigationManager`.
- [x] `StorefrontCheckoutShell.razor` owns methods such as `RefreshAsync`, `SelectShippingAsync`, `SelectPaymentAsync`, `ReviewAsync`, and `PlaceOrderAsync`.
- [x] `StorefrontCheckoutShell.razor` renders `data-storefront-checkout-shell` and `data-storefront-checkout-cart-version`.
- [x] `StorefrontCheckoutShell.razor` currently includes hardcoded English copy such as checkout state, refresh, review, and place-order labels.
- [x] `BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM/Components/Checkout/StorefrontCheckoutViewClasses.cs` currently owns the checkout class contract shape.
- [x] `BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM/Components/Checkout/StorefrontCheckoutShellOptions.cs` currently owns final V2 class values.
- [x] `BlazorShop.PresentationV2/BlazorShop.Storefront.Components.WasmHost/BlazorShop.Storefront.Components.WasmHost.csproj` already references `BlazorShop.Storefront.Components` and `BlazorShop.Storefront.Browser`.
- [x] `BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM/BlazorShop.Storefront.V2.WASM.csproj` already references `BlazorShop.Storefront.Components.WasmHost`.
- [x] `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/Hybrid/Commerce/CartPage.razor` currently renders the cart WASM view with `@rendermode="InteractiveWebAssembly"`.
- [x] `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/Hybrid/Commerce/CheckoutPage.razor` currently renders `StorefrontCheckoutShell` with `ShowPanel="false"`.
- [x] `CheckoutPage.razor` currently uses SSR Presentation components for the visible checkout form and real order placement: `StorefrontCheckoutForm`, `StorefrontCheckoutAddressFields`, `StorefrontCheckoutPaymentFields`, and `StorefrontCheckoutSubmit`.
- [x] `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/_Imports.razor` currently imports V2.WASM cart and checkout namespaces and must be updated after extraction.

## Final Ownership Target

`BlazorShop.Storefront.Components`:

- [x] Owns `Contracts/Cart/StorefrontCartViewClasses.cs`.
- [x] Owns `Contracts/Cart/StorefrontCartViewLabels.cs`.
- [x] Owns `Contracts/Checkout/StorefrontCheckoutViewClasses.cs`.
- [x] Owns `Contracts/Checkout/StorefrontCheckoutViewLabels.cs`.
- [x] Does not reference `BlazorShop.Storefront.Browser`.
- [x] Does not reference `BlazorShop.Storefront.V2`.
- [x] Does not reference `BlazorShop.Storefront.V2.WASM`.
- [x] Does not own final V2 CSS class values.
- [x] Does not own final V2 copy.

`BlazorShop.Storefront.Components.WasmHost`:

- [x] Owns reusable `Components/Cart/StorefrontCartView.razor`.
- [x] Owns reusable `Components/Checkout/StorefrontCheckoutShell.razor`.
- [x] Injects browser controllers from `BlazorShop.Storefront.Browser`.
- [x] Accepts class and label contract parameters from the host.
- [x] Renders semantic `data-storefront-*` hooks needed by QA and future frontend generation.
- [x] Does not contain `@rendermode`, `InteractiveServer`, `InteractiveWebAssembly`, or `InteractiveAuto`.
- [x] Does not contain V2 final Tailwind values.
- [x] Does not contain V2 route defaults that are not passed in by host options.

`BlazorShop.Storefront.V2.WASM`:

- [x] Owns `Components/Cart/StorefrontCartSection.razor` thin wrapper.
- [x] Owns `Components/Checkout/StorefrontCheckoutSection.razor` thin wrapper.
- [x] Owns `StorefrontCartViewOptions` with final V2 class values and labels.
- [x] Owns `StorefrontCheckoutShellOptions` with final V2 class values and labels.
- [x] Does not inject `IStorefrontBrowserCartController` directly.
- [x] Does not inject `IStorefrontBrowserCheckoutController` directly.
- [x] Does not own cart or checkout lifecycle methods.
- [x] Does not implement shared Presentation `IStorefront*Client` contracts.

`BlazorShop.Storefront.V2`:

- [x] Keeps page placement and render-mode ownership.
- [x] Keeps cart page behavior unchanged.
- [x] Keeps checkout page behavior unchanged, including the current visible SSR form flow.
- [x] Keeps `StorefrontCheckoutShell` hidden with `ShowPanel=false` unless a separate approved behavior phase changes that.

## Hard Scope Lock

Allowed production areas:

- [x] `BlazorShop.PresentationV2/BlazorShop.Storefront.Components/Contracts/Cart/`
- [x] `BlazorShop.PresentationV2/BlazorShop.Storefront.Components/Contracts/Checkout/`
- [x] `BlazorShop.PresentationV2/BlazorShop.Storefront.Components.WasmHost/Components/Cart/`
- [x] `BlazorShop.PresentationV2/BlazorShop.Storefront.Components.WasmHost/Components/Checkout/`
- [x] `BlazorShop.PresentationV2/BlazorShop.Storefront.Components.WasmHost/_Imports.razor`
- [x] `BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM/Components/Cart/`
- [x] `BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM/Components/Checkout/`
- [x] `BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM/_Imports.razor`
- [x] `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/Hybrid/Commerce/CartPage.razor`
- [x] `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/Hybrid/Commerce/CheckoutPage.razor`
- [x] `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/_Imports.razor`

Allowed tests:

- [x] `BlazorShop.Tests.V2/PresentationV2/Storefront/*ComponentMode*Tests.cs`
- [x] `BlazorShop.Tests.V2/PresentationV2/Storefront/*RenderMode*Tests.cs`
- [x] `BlazorShop.Tests.V2/PresentationV2/Storefront/*VisualOnlyBoundary*Tests.cs`
- [x] `BlazorShop.Tests.V2/PresentationV2/Storefront/*V2WASMRuntimeFoundation*Tests.cs`
- [x] `BlazorShop.Tests.V2/PresentationV2/Storefront/*CommerceFlowCutover*Tests.cs`
- [x] `BlazorShop.Tests.V2/PresentationV2/Storefront/*RequiredVisualContracts*Tests.cs`
- [x] Optional new focused tests under `BlazorShop.Tests.V2/PresentationV2/Storefront/`

Allowed docs:

- [x] `BlazorShop.PresentationV2/COMPONENT-MODES.md`
- [x] `docs/architecture/03-runtime-boundaries.md`
- [x] `docs/architecture/05-project-and-folder-guide.md`
- [x] `docs/architecture/10-v2-contract-ownership.md`
- [x] `docs/refactor-control-Commerce-storefront/QA-StorefrontV2.todo.md`
- [x] This plan file.

Forbidden changes:

- [x] Do not change `BlazorShop.Storefront.Browser` controller public APIs.
- [x] Do not change BFF endpoint paths.
- [x] Do not change Commerce Node Storefront API paths.
- [x] Do not change checkout order placement business logic.
- [x] Do not change payment provider behavior.
- [x] Do not change account components in this phase.
- [x] Do not change header, footer, product detail, gallery, auth, payment, or order components in this phase.
- [x] Do not touch StorefrontBuilder tooling.
- [x] Do not touch Starter or generated storefront projects.
- [x] Do not introduce React, JS framework code, or a new frontend package.
- [x] Do not make WasmHost depend on V2, V2.WASM, Runtime, Client, Commerce Node, Control Plane, or domain/application/infrastructure projects.

## Naming Decision

Use V2.WASM wrapper names to avoid component ambiguity:

- [x] `BlazorShop.Storefront.Components.WasmHost.Components.Cart.StorefrontCartView`
- [x] `BlazorShop.Storefront.V2.WASM.Components.Cart.StorefrontCartSection`
- [x] `BlazorShop.Storefront.Components.WasmHost.Components.Checkout.StorefrontCheckoutShell`
- [x] `BlazorShop.Storefront.V2.WASM.Components.Checkout.StorefrontCheckoutSection`

Rationale:

- The reusable component keeps the current semantic component name.
- V2.WASM wrapper names make it obvious that V2 owns final presentation values.
- V2 pages do not need fragile namespace aliases to distinguish shared implementation from V2 wrapper.

## Phase 0 - Baseline Audit

- [x] Read `AGENTS.md`.
- [x] Read `docs/architecture/README.md`.
- [x] Read `docs/architecture/03-runtime-boundaries.md`.
- [x] Read `docs/architecture/05-project-and-folder-guide.md`.
- [x] Read `docs/architecture/10-v2-contract-ownership.md`.
- [x] Read `BlazorShop.PresentationV2/COMPONENT-MODES.md`.
- [x] Read this plan from top to bottom before editing.
- [x] Run `git status --short` and confirm unrelated user changes are not touched. Evidence: the supplied plan was the sole pre-existing untracked file and remains intentional task input.
- [x] Search for current cart component usage:

```powershell
rg "StorefrontCartView|StorefrontCartViewClasses|StorefrontCartViewOptions|StorefrontCartSection" BlazorShop.PresentationV2 BlazorShop.Tests.V2
```

- [x] Search for current checkout component usage:

```powershell
rg "StorefrontCheckoutShell|StorefrontCheckoutViewClasses|StorefrontCheckoutShellOptions|StorefrontCheckoutSection" BlazorShop.PresentationV2 BlazorShop.Tests.V2
```

- [x] Search for current controller injection expectations:

```powershell
rg "IStorefrontBrowserCartController|IStorefrontBrowserCheckoutController|HydrateAsync|PlaceOrderAsync|ReviewAsync" BlazorShop.PresentationV2 BlazorShop.Tests.V2
```

- [x] Record any consumer outside the allowed scope and stop before editing if the consumer requires a behavior change. Evidence: in-scope consumers are `StorefrontCartView` on V2 `CartPage.razor` and `StorefrontCheckoutShell` on V2 `CheckoutPage.razor`, with their V2.WASM options/classes and source-assertion tests. `StorefrontCartApp` and `StorefrontCheckoutApp` are independently named Starter apps; no behavior change is required. Browser controllers, Presentation BFF endpoints, Runtime/Client transport, Commerce Node, and Control Plane are already their correct owners and require no change.

Exit criteria:

- [x] All current consumers are identified. Evidence: the three prescribed searches found V2 pages, V2.WASM cart/checkout components and options, source-based V2 tests, Browser controller contracts/registration, and independently named Starter apps.
- [x] No hidden route or service dependency requires changing Browser, BFF, Runtime, Client, Commerce Node, or Control Plane. Evidence: controller calls terminate at existing Browser controllers, which use established same-origin Presentation endpoint/facade paths; Phase 0 changes only the plan record.

## Phase 1 - Move Class Contracts Into Components Contracts

Tasks:

- [x] Create `BlazorShop.PresentationV2/BlazorShop.Storefront.Components/Contracts/Cart/`. Evidence: added `StorefrontCartViewClasses.cs` at the contract path.
- [x] Move or recreate `StorefrontCartViewClasses` under `Contracts/Cart`. Evidence: the cart class record now has the shared Cart contract namespace.
- [x] Keep the public shape needed by the existing cart markup. Evidence: all public properties and `Empty` were preserved verbatim.
- [x] Create `BlazorShop.PresentationV2/BlazorShop.Storefront.Components/Contracts/Checkout/`. Evidence: added `StorefrontCheckoutViewClasses.cs` at the contract path.
- [x] Move or recreate `StorefrontCheckoutViewClasses` under `Contracts/Checkout`. Evidence: the checkout class record now has the shared Checkout contract namespace.
- [x] Keep the public shape needed by the existing checkout shell markup. Evidence: all public properties and `Empty` were preserved verbatim.
- [x] Delete the old class contract definitions from V2.WASM after all references are migrated. Evidence: both former V2.WASM definition files were deleted.
- [x] Update namespaces to `BlazorShop.Storefront.Components.Contracts.Cart` and `BlazorShop.Storefront.Components.Contracts.Checkout`. Evidence: the new contract files and V2.WASM imports use those namespaces.
- [x] Ensure Components contracts remain browser-safe and do not depend on V2.WASM. Evidence: contract files contain only records/properties and the Components project remains a plain class library.

Guardrails:

- [x] Do not move final Tailwind values into Components. Evidence: final values remain in `StorefrontCartViewOptions` and `StorefrontCheckoutShellOptions` in V2.WASM.
- [x] Do not move V2 options into Components. Evidence: both options classes remain in their V2.WASM component folders.
- [x] Do not add service registrations in Components. Evidence: no Components project/service registration changes were made.
- [x] Do not add `Microsoft.AspNetCore.Components.WebAssembly` dependencies to Components. Evidence: Components project package references are unchanged.

Focused checks:

```powershell
rg "class StorefrontCartViewClasses|record StorefrontCartViewClasses" BlazorShop.PresentationV2
rg "class StorefrontCheckoutViewClasses|record StorefrontCheckoutViewClasses" BlazorShop.PresentationV2
rg "StorefrontCartViewClasses" BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM
rg "StorefrontCheckoutViewClasses" BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM
```

Exit criteria:

- [x] Exactly one cart class contract definition remains, under Components contracts. Evidence: prescribed `rg` and the focused ownership test find only `Contracts/Cart/StorefrontCartViewClasses.cs`.
- [x] Exactly one checkout class contract definition remains, under Components contracts. Evidence: prescribed `rg` and the focused ownership test find only `Contracts/Checkout/StorefrontCheckoutViewClasses.cs`.
- [x] V2.WASM references the shared contracts but does not define them. Evidence: V2.WASM options and Razor imports consume the shared types; old definitions were deleted.

## Phase 2 - Add Label Contracts

Tasks:

- [x] Add `StorefrontCartViewLabels` under `BlazorShop.Storefront.Components/Contracts/Cart/`. Evidence: `StorefrontCartViewLabels.cs` is the sole shared cart label contract definition.
- [x] Include labels for all current hardcoded cart copy, including title, heading, empty state, loading state, error state, quantity label, remove action, clear action, checkout action, continue shopping action, product link label where applicable, cart summary labels, and fallback item text. Evidence: focused contract test asserts the complete cart label property set, including current cart copy and future-safe loading/error slots.
- [x] Add `StorefrontCheckoutViewLabels` under `BlazorShop.Storefront.Components/Contracts/Checkout/`. Evidence: `StorefrontCheckoutViewLabels.cs` is the sole shared checkout label contract definition.
- [x] Include labels for all current hardcoded checkout shell copy, including heading, state label, refresh action, refreshing state, cart version label, shipping-not-required message, review action, place-order action, placing-order state, error/failure fallback, loading state, and selected option labels. Evidence: focused contract test asserts the complete checkout label property set, including current shell copy and future-safe loading/error/selection slots.
- [x] Provide safe defaults only if existing components require non-null values to render during tests. Evidence: both contracts expose non-null `string.Empty` property defaults and an `Empty` instance without storefront copy.
- [x] Keep defaults neutral and technical if defaults are needed. Evidence: shared contract defaults are `string.Empty`; no final English copy is defined in Components.
- [x] Put V2 final English copy in V2.WASM options, not in shared Components contracts. Evidence: `StorefrontCartViewOptions.Labels` and `StorefrontCheckoutShellOptions.Labels` contain the current English tokens; the focused test asserts representative ownership and shared contracts exclude those tokens.

Guardrails:

- [x] Shared labels are a contract shape, not the final storefront copy source. Evidence: the two Components records contain only neutral string defaults; V2.WASM options own final values.
- [x] Do not add localization infrastructure in this phase. Evidence: no localization services, resources, registrations, or dependencies changed.
- [x] Do not add database-backed labels in this phase. Evidence: only Components contract and V2.WASM option source changed; no persistence/API changes were made.
- [x] Do not move route labels or page titles outside current scope. Evidence: cart and checkout Razor/page files remain untouched.

Exit criteria:

- [x] All cart user-facing text in the reusable cart component can be supplied by labels. Evidence: the cart label contract covers each current hardcoded token and required loading/error fallbacks; component wiring is deferred to Phase 3.
- [x] All checkout shell user-facing text in the reusable checkout shell can be supplied by labels. Evidence: the checkout label contract covers each current hardcoded shell token and required loading/error/selection fallbacks; component wiring is deferred to Phase 4.
- [x] V2.WASM owns the current final text values. Evidence: both V2.WASM option classes expose `Labels` with the exact current cart/checkout English tokens; focused ownership test passes.

## Phase 3 - Move Cart Implementation Into WasmHost

Tasks:

- [x] Create `BlazorShop.PresentationV2/BlazorShop.Storefront.Components.WasmHost/Components/Cart/`.
- [x] Move the runtime markup and behavior from V2.WASM `StorefrontCartView.razor` into WasmHost `StorefrontCartView.razor`.
- [x] Update the namespace to `BlazorShop.Storefront.Components.WasmHost.Components.Cart`.
- [x] Inject `IStorefrontBrowserCartController` in the WasmHost component.
- [x] Accept `StorefrontCartViewClasses` as a parameter.
- [x] Accept `StorefrontCartViewLabels` as a parameter.
- [x] Preserve `Initialize`, `HydrateAsync`, `UpdateQuantityAsync`, `RemoveLineAsync`, `ClearAsync`, and existing state update behavior.
- [x] Preserve existing quantity validation and mutation call behavior.
- [x] Preserve existing line rendering and item identity values.
- [x] Preserve existing semantic hooks:
  - `data-storefront-cart-quantity`
  - `data-line-id`
  - `data-product-id`
  - `data-variant-id`
  - `data-product-name`
  - `data-size-value`
  - `data-storefront-cart-remove`
  - `data-storefront-cart-clear`
- [x] Replace hardcoded copy with label contract usage.
- [x] Ensure the component does not contain `@rendermode`.
- [x] Ensure the component does not contain final V2 Tailwind class literals unless they already come through the supplied `Classes` contract.
- [x] Update `BlazorShop.Storefront.Components.WasmHost/_Imports.razor` as needed.

Guardrails:

- [x] WasmHost may reference `BlazorShop.Storefront.Browser`.
- [x] WasmHost may reference `BlazorShop.Storefront.Components`.
- [x] WasmHost must not reference V2 or V2.WASM.
- [x] WasmHost must not create a new HTTP client transport.
- [x] WasmHost must keep using Browser controllers, not generated backend clients.

Focused checks:

```powershell
rg "IStorefrontBrowserCartController" BlazorShop.PresentationV2/BlazorShop.Storefront.Components.WasmHost BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM
rg "@rendermode|InteractiveServer|InteractiveWebAssembly|InteractiveAuto" BlazorShop.PresentationV2/BlazorShop.Storefront.Components.WasmHost
rg "data-storefront-cart" BlazorShop.PresentationV2/BlazorShop.Storefront.Components.WasmHost
```

Exit criteria:

- [x] WasmHost owns the cart behavior implementation.
- [x] V2.WASM no longer owns cart controller injection or mutation lifecycle code.
- [x] Cart semantic hooks remain available for browser QA.

## Phase 4 - Move Checkout Shell Implementation Into WasmHost

Tasks:

- [x] Create `BlazorShop.PresentationV2/BlazorShop.Storefront.Components.WasmHost/Components/Checkout/`. Evidence: the moved shell now exists at `Components/Checkout/StorefrontCheckoutShell.razor`.
- [x] Move the runtime markup and behavior from V2.WASM `StorefrontCheckoutShell.razor` into WasmHost `StorefrontCheckoutShell.razor`. Evidence: the Phase 4 commit records an 87% rename and the focused ownership test passes.
- [x] Update the namespace to `BlazorShop.Storefront.Components.WasmHost.Components.Checkout`. Evidence: the moved Razor file declares the required namespace.
- [x] Inject `IStorefrontBrowserCheckoutController` in the WasmHost component. Evidence: the only checkout controller injection across WasmHost and V2.WASM is in the moved shell.
- [x] Keep `NavigationManager` usage only if it is required for current redirect behavior. Evidence: it remains injected solely for the preserved `NavigateTo(outcome.RedirectUrl, forceLoad: true)` path.
- [x] Accept `StorefrontCheckoutViewClasses` as a parameter. Evidence: `Classes` is an editor-required parameter and all class attributes remain dynamic.
- [x] Accept `StorefrontCheckoutViewLabels` as a parameter. Evidence: `Labels` is an editor-required parameter and replaces the prior UI literals.
- [x] Preserve `RefreshAsync`, `SelectShippingAsync`, `SelectPaymentAsync`, `ReviewAsync`, `PlaceOrderAsync`, and current state update behavior. Evidence: source-parity and focused ownership checks confirm every controller call and `StateHasChanged` flow remains.
- [x] Preserve the `ShowPanel` parameter behavior. Evidence: `ShowPanel` remains editor-required with the default `false` value-type behavior.
- [x] Preserve current hidden-shell behavior when `ShowPanel=false`. Evidence: the shell markup and browser hydration remain guarded by `ShowPanel`.
- [x] Preserve semantic hooks. Evidence: source parity confirms one occurrence before and after for each hook:
  - `data-storefront-checkout-shell`
  - `data-storefront-checkout-cart-version`
- [x] Replace hardcoded copy with label contract usage. Evidence: the old checkout UI-copy scan returns zero matches and the focused test verifies all used label slots.
- [x] Ensure the component does not contain `@rendermode`. Evidence: the WasmHost render-mode scan returns zero matches.
- [x] Ensure the component does not contain final V2 Tailwind class literals unless they come through the supplied `Classes` contract. Evidence: the reusable literal-class/V2-token neutrality test passes.
- [x] Update `BlazorShop.Storefront.Components.WasmHost/_Imports.razor` as needed. Evidence: Checkout Browser, contract, and headless namespaces were added without other dependency changes.

Checkout-specific caution:

- [x] Do not make the WasmHost shell the visible production checkout UI in this phase. Evidence: `CheckoutPage.razor` has zero diff and still passes `ShowPanel="false"` twice.
- [x] Do not remove the SSR Presentation checkout form from `CheckoutPage.razor`. Evidence: the unchanged page still contains form, address, payment, and submit components once each.
- [x] Do not change order placement semantics. Evidence: place-order dispatch, pending refresh, redirect, and changed-outcome refresh have exact source parity.
- [x] Do not change payment method selection semantics. Evidence: `SelectPaymentAsync(key)` and its state-refresh branch have exact source parity.
- [x] Do not add tax UI. Evidence: the Phase 4 diff contains only the shell rename/neutralization, required imports, focused test, and this checklist evidence.

Focused checks:

```powershell
rg "IStorefrontBrowserCheckoutController" BlazorShop.PresentationV2/BlazorShop.Storefront.Components.WasmHost BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM
rg "@rendermode|InteractiveServer|InteractiveWebAssembly|InteractiveAuto" BlazorShop.PresentationV2/BlazorShop.Storefront.Components.WasmHost
rg "data-storefront-checkout" BlazorShop.PresentationV2/BlazorShop.Storefront.Components.WasmHost
```

Exit criteria:

- [x] WasmHost owns the checkout shell behavior implementation. Evidence: the focused ownership test passes and the WasmHost project builds with zero warnings/errors.
- [x] V2.WASM no longer owns checkout controller injection or shell lifecycle code. Evidence: the old V2.WASM shell path is absent and only the WasmHost source injects the controller.
- [x] Current V2 checkout page still uses the visible SSR checkout form for real browser order placement. Evidence: the page is unchanged and retains `StorefrontCheckoutForm`, address, payment, and submit composition.

## Phase 5 - Create V2.WASM Cart Wrapper

Tasks:

- [x] Add `BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM/Components/Cart/StorefrontCartSection.razor`.
- [x] Make the wrapper render `BlazorShop.Storefront.Components.WasmHost.Components.Cart.StorefrontCartView`.
- [x] Pass V2 cart classes from `StorefrontCartViewOptions`.
- [x] Pass V2 cart labels from `StorefrontCartViewOptions`.
- [x] Keep V2 final class values and V2 final English copy in the V2 options object.
- [x] Keep wrapper markup minimal.
- [x] Do not inject `IStorefrontBrowserCartController` in the wrapper.
- [x] Delete or stop using the old V2.WASM `StorefrontCartView.razor`.
- [x] Update namespaces and imports so V2 pages can render `StorefrontCartSection` without ambiguity.

Guardrails:

- [x] Wrapper may configure V2 options.
- [x] Wrapper may expose V2 page parameters if currently needed.
- [x] Wrapper must not duplicate cart behavior logic.
- [x] Wrapper must not duplicate the reusable cart markup.

Exit criteria:

- [x] V2.WASM owns cart presentation values only.
- [x] `CartPage.razor` renders the wrapper, not the shared component directly unless an explicit namespace alias is used. Completed in Phase 7 because page placement is Phase 7 ownership.

## Phase 6 - Create V2.WASM Checkout Wrapper

Tasks:

- [x] Add `BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM/Components/Checkout/StorefrontCheckoutSection.razor`.
- [x] Make the wrapper render `BlazorShop.Storefront.Components.WasmHost.Components.Checkout.StorefrontCheckoutShell`.
- [x] Pass V2 checkout classes from `StorefrontCheckoutShellOptions`.
- [x] Pass V2 checkout labels from `StorefrontCheckoutShellOptions`.
- [x] Preserve current `ShowPanel` parameter behavior.
- [x] Keep V2 final class values and V2 final English copy in the V2 options object.
- [x] Keep wrapper markup minimal.
- [x] Do not inject `IStorefrontBrowserCheckoutController` in the wrapper.
- [x] Delete or stop using the old V2.WASM `StorefrontCheckoutShell.razor`.
- [x] Update namespaces and imports so V2 pages can render `StorefrontCheckoutSection` without ambiguity.

Guardrails:

- [x] Wrapper may configure V2 options.
- [x] Wrapper may expose `ShowPanel`.
- [x] Wrapper must not duplicate checkout shell behavior logic.
- [x] Wrapper must not duplicate the reusable checkout shell markup.

Exit criteria:

- [x] V2.WASM owns checkout presentation values only.
- [x] `CheckoutPage.razor` renders the V2 wrapper with `ShowPanel=false`. Completed in Phase 7 because page placement is Phase 7 ownership.

## Phase 7 - Update V2 Page Integration

Cart page:

- [x] Update `BlazorShop.Storefront.V2/Pages/Hybrid/Commerce/CartPage.razor` to render `StorefrontCartSection`.
- [x] Keep `@rendermode="InteractiveWebAssembly"` on the page component usage.
- [x] Preserve route, page title, metadata, and surrounding V2 layout.
- [x] Preserve current empty-cart and loaded-cart behavior.

Checkout page:

- [x] Update `BlazorShop.Storefront.V2/Pages/Hybrid/Commerce/CheckoutPage.razor` to render `StorefrontCheckoutSection`.
- [x] Keep `@rendermode="InteractiveWebAssembly"` on the page component usage.
- [x] Keep `ShowPanel=false` in the same places it is currently used unless explicitly approved otherwise.
- [x] Preserve the visible SSR checkout form components.
- [x] Preserve current checkout redirects.
- [x] Preserve COD place-order flow.

Imports:

- [x] Update `BlazorShop.Storefront.V2/_Imports.razor` to reference V2.WASM wrapper namespaces.
- [x] Remove imports that point V2 server pages directly at old V2.WASM implementation namespaces if no longer needed.
- [x] Avoid ambiguous component names between WasmHost and V2.WASM.

Exit criteria:

- [x] Cart page behavior is unchanged.
- [x] Checkout page behavior is unchanged.
- [x] Render mode remains owned by V2 page placement, not WasmHost. V2 build passed with 0 warnings/errors.

## Phase 8 - Rewrite Architecture Tests

Tests to inspect and update:

- [x] `BlazorShop.Tests.V2/PresentationV2/Storefront/StorefrontComponentsHeadlessPresentationRefactorTests.cs`
- [x] `BlazorShop.Tests.V2/PresentationV2/Storefront/StorefrontV2WASMRuntimeFoundationTests.cs`
- [x] `BlazorShop.Tests.V2/PresentationV2/Storefront/StorefrontCommerceFlowCutoverTests.cs`
- [x] `BlazorShop.Tests.V2/PresentationV2/Storefront/StorefrontRequiredVisualContractsHardeningTests.cs`
- [x] Any test found by `rg "StorefrontCartView|StorefrontCheckoutShell|StorefrontCartViewClasses|StorefrontCheckoutViewClasses" BlazorShop.Tests.V2`

Required new assertions:

- [x] WasmHost project contains cart and checkout implementation components.
- [x] WasmHost cart component injects `IStorefrontBrowserCartController`.
- [x] WasmHost checkout shell injects `IStorefrontBrowserCheckoutController`.
- [x] WasmHost components do not contain `@rendermode` or interactive render-mode directives.
- [x] WasmHost components do not reference V2 or V2.WASM namespaces.
- [x] Components contracts own `StorefrontCartViewClasses` and `StorefrontCheckoutViewClasses`.
- [x] Components contracts own `StorefrontCartViewLabels` and `StorefrontCheckoutViewLabels`.
- [x] V2.WASM wrappers do not inject browser controllers.
- [x] V2.WASM wrappers do not contain lifecycle methods such as `HydrateAsync`, `UpdateQuantityAsync`, `ClearAsync`, `RefreshAsync`, `ReviewAsync`, and `PlaceOrderAsync`.
- [x] V2.WASM wrappers render WasmHost components and pass V2 options.
- [x] V2 pages own `InteractiveWebAssembly` render mode.
- [x] Checkout page still renders the shell with `ShowPanel=false`.
- [x] Checkout page still renders SSR checkout form components for real checkout.

Required removed or corrected assertions:

- [x] Remove assertions that V2.WASM implementation files own browser controller injection.
- [x] Remove assertions that V2.WASM implementation files own runtime lifecycle methods.
- [x] Remove assertions that checkout shell visible actions are part of the public page when `ShowPanel=false`.

Focused test filters:

```powershell
dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~StorefrontComponentsHeadlessPresentationRefactorTests"
dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~StorefrontV2WASMRuntimeFoundationTests"
dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~StorefrontCommerceFlowCutoverTests"
dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~StorefrontRequiredVisualContractsHardeningTests"
```

Exit criteria:

- [x] Tests describe the new ownership accurately. Focused regression suite passed 66/66 on 2026-08-11.
- [x] Tests no longer preserve the old boundary leak.
- [x] Tests fail if a later agent moves browser runtime behavior back into V2.WASM.

## Phase 9 - Add Boundary Guardrails

Add or update tests to enforce:

- [x] `BlazorShop.Storefront.Components.WasmHost` references only allowed Storefront shared projects.
- [x] `BlazorShop.Storefront.Components.WasmHost` does not reference V2, V2.WASM, Runtime, Client, Commerce Node, Control Plane, Domain, Application, or Infrastructure.
- [x] `BlazorShop.Storefront.Components.WasmHost` does not contain `@rendermode`.
- [x] `BlazorShop.Storefront.Components.WasmHost` does not contain `InteractiveServer`, `InteractiveWebAssembly`, or `InteractiveAuto`.
- [x] `BlazorShop.Storefront.Components.WasmHost` does not contain final V2 route defaults that belong to V2 options.
- [x] `BlazorShop.Storefront.V2.WASM` wrappers do not inject `IStorefrontBrowserCartController`.
- [x] `BlazorShop.Storefront.V2.WASM` wrappers do not inject `IStorefrontBrowserCheckoutController`.
- [x] `BlazorShop.Storefront.V2.WASM` does not contain duplicated cart or checkout mutation methods after extraction.
- [x] `BlazorShop.Storefront.Components` does not reference Browser, V2, V2.WASM, Runtime, Client, Commerce Node, Control Plane, Domain, Application, or Infrastructure.

Recommended test names:

- [x] `StorefrontWasmHostComponentOwnershipTests`
- [x] `StorefrontV2WasmWrapperBoundaryTests`
- [x] `StorefrontSharedContractOwnershipTests`

Exit criteria:

- [x] Boundary tests fail on the most likely future regressions. Focused suite passed 4/4 on 2026-08-11.
- [x] Boundary tests are path based and deterministic.
- [x] Boundary tests do not require running a browser.

## Phase 10 - Update Documentation

Update `BlazorShop.PresentationV2/COMPONENT-MODES.md`:

- [x] Document that WasmHost owns browser-interactive reusable components.
- [x] Document that V2.WASM owns wrappers and final V2 presentation values.
- [x] Document that V2 pages own render mode.
- [x] Add cart and checkout as concrete examples.
- [x] Clarify that checkout shell extraction does not make the shell the visible checkout form in V2.

Update `docs/architecture/03-runtime-boundaries.md`:

- [x] Clarify Storefront browser actions go through Browser/BFF primitives and same-origin routes.
- [x] Clarify WasmHost must not call Commerce Node directly.
- [x] Clarify V2 server pages own render-mode placement.

Update `docs/architecture/05-project-and-folder-guide.md`:

- [x] Add or update `BlazorShop.Storefront.Components.WasmHost` responsibilities.
- [x] Add or update `BlazorShop.Storefront.V2.WASM` responsibilities.
- [x] Add or update `BlazorShop.Storefront.Components` contract responsibilities.

Update `docs/architecture/10-v2-contract-ownership.md`:

- [x] Clarify class and label contracts for cart/checkout live in shared Components contracts.
- [x] Clarify final copy and final class values live in store-specific wrapper/options.

Update `docs/refactor-control-Commerce-storefront/QA-StorefrontV2.todo.md`:

- [x] Add Phase 3.5 evidence checklist.
- [x] Add Playwright cart browser QA cases.
- [x] Add Playwright checkout browser QA cases.
- [x] Add boundary verification evidence commands.

Exit criteria:

- [x] Documentation matches actual code ownership.
- [x] No doc claims that V2.WASM owns reusable cart/checkout behavior.
- [x] No doc claims that checkout shell is the visible production checkout form while `ShowPanel=false`.

## Phase 11 - Build Verification

Run focused builds first:

```powershell
dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.Components/BlazorShop.Storefront.Components.csproj --no-restore
dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.Browser/BlazorShop.Storefront.Browser.csproj --no-restore
dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.Components.WasmHost/BlazorShop.Storefront.Components.WasmHost.csproj --no-restore
dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM/BlazorShop.Storefront.V2.WASM.csproj --no-restore
dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.V2/BlazorShop.Storefront.V2.csproj --no-restore
```

Then run solution build:

```powershell
dotnet build BlazorShop.sln --no-restore
```

Exit criteria:

- [x] Components builds.
- [x] Browser builds.
- [x] WasmHost builds.
- [x] V2.WASM builds.
- [x] V2 builds.
- [x] Full solution builds. `dotnet build BlazorShop.sln --no-restore` passed on 2026-08-11 with 0 errors (11 pre-existing MessagePack advisory warnings).

Failure handling:

- [n/a] If a build fails because of namespace ambiguity, prefer wrapper naming/import cleanup over direct fully-qualified markup everywhere. No Phase 11 ambiguity failure.
- [n/a] If a build fails because of missing label/class properties, update the shared contract shape to match existing markup. No Phase 11 contract-shape failure.
- [x] If a build fails because WasmHost needs a Browser type, confirm the Browser dependency already exists in WasmHost csproj before adding anything. Confirmed existing Browser reference; no new reference was added.

## Phase 12 - Unit And Architecture Test Verification

Run focused tests:

```powershell
dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~Cart"
dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~Checkout"
dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~WasmHost"
dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~ComponentModeDependency"
dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~RenderModeOwnership"
dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~VisualOnlyBoundary"
dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~V2WASMRuntimeFoundation"
dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~CommerceFlowCutover"
dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~RequiredVisualContracts"
```

Run full V2 test project:

```powershell
dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-restore
```

Exit criteria:

- [x] Focused cart tests pass.
- [x] Focused checkout tests pass.
- [x] WasmHost boundary tests pass.
- [x] Component mode dependency tests pass.
- [x] Render mode ownership tests pass.
- [x] Visual-only boundary tests pass.
- [x] Commerce flow cutover tests pass.
- [x] Required visual contract tests pass.
- [x] Full `BlazorShop.Tests.V2` passes: 1971 passed, 2 pre-existing skipped, 0 failed on 2026-08-11.

## Phase 13 - Playwright Browser QA

Use the local V2 runner unless an implementation session has an existing equivalent runtime:

```powershell
.\scripts\run-v2-local.ps1 -StopExisting -NoOpenBrowser
```

Cart browser QA:

- [x] Open the cart page when the cart is empty.
- [x] Confirm empty state renders without console errors.
- [x] Add a product to cart from product detail or existing supported path.
- [x] Open cart page with one item.
- [x] Confirm product name, quantity, unit/line pricing, image/fallback, and checkout link render.
- [x] Confirm `data-storefront-cart-quantity` is present.
- [x] Confirm each line has `data-line-id`.
- [x] Confirm each line has `data-product-id`.
- [n/a] Confirm variant products include `data-variant-id` where available. The selected simple/digital fixtures have no variant.
- [n/a] Confirm selected attributes include `data-size-value` where available. The selected simple/digital fixtures have no size selection.
- [x] Increase quantity.
- [x] Confirm server-backed state updates and total/count refresh.
- [x] Decrease quantity.
- [x] Confirm state updates and no duplicate mutation occurs.
- [x] Remove one line.
- [x] Confirm line disappears and summary updates.
- [x] Add multiple items if fixture supports it.
- [x] Clear cart.
- [x] Confirm empty state returns.
- [x] Confirm product links still navigate.
- [x] Confirm checkout link still navigates.
- [x] Confirm no direct Commerce Node URL appears in browser network requests.
- [x] Confirm browser actions use same-origin BFF routes.
- [x] Confirm no `/_blazor` server circuit connection appears for cart page.
- [x] Confirm no unexpected WebSocket or EventSource connection appears.
- [x] Confirm no page errors.
- [x] Confirm no console errors except known benign framework diagnostics already accepted by QA.

Checkout browser QA:

- [x] Create a valid cart with an active item.
- [x] Navigate to checkout.
- [x] Confirm current visible checkout form still renders through SSR Presentation components.
- [x] Confirm hidden `StorefrontCheckoutSection` / shell behavior does not create visible duplicate checkout controls while `ShowPanel=false`.
- [x] Confirm `data-storefront-checkout-shell` exists only if the hidden shell currently rendered it before extraction; otherwise document the unchanged behavior. The hidden `ShowPanel=false` shell emits no element, matching the current behavior.
- [x] Fill billing/shipping fields according to current V2 fixture.
- [x] Select COD payment.
- [x] Submit real place-order flow.
- [x] Confirm order completion page or expected provider redirect behavior.
- [x] Confirm order number/reference is displayed if current V2 flow displays it.
- [x] Confirm cart is closed or cleared according to existing checkout rule.
- [x] Confirm browser network does not call direct Commerce Node URL.
- [x] Confirm browser actions use same-origin BFF routes.
- [x] Confirm no `/_blazor` server circuit connection appears for checkout page.
- [x] Confirm no unexpected WebSocket or EventSource connection appears.
- [x] Confirm no page errors.
- [x] Confirm no console errors except known benign framework diagnostics already accepted by QA.

Optional shell behavior QA:

- [n/a] If a component test page or fixture supports `ShowPanel=true`, test `RefreshAsync` through the UI. No such test page or safe fixture is exposed.
- [n/a] If a component test page or fixture supports `ShowPanel=true`, test shipping option selection through the UI. No such test page or safe fixture is exposed.
- [n/a] If a component test page or fixture supports `ShowPanel=true`, test payment option selection through the UI. No such test page or safe fixture is exposed.
- [n/a] If a component test page or fixture supports `ShowPanel=true`, test review and place-order command dispatch with a mocked or fixture-safe Browser controller. No such test page or safe fixture is exposed.
- [x] Do not treat missing visible shell controls on production checkout page as a failure while `ShowPanel=false` remains current behavior.

Evidence to capture:

- [x] Storefront URL used: `http://localhost:18598`.
- [x] Store key used: `default`.
- [x] Browser mode used: visible Chromium through Playwright CLI.
- [x] Product fixtures used: `QA Simple Product 100` and `QA Digital No Shipping Product`.
- [x] Payment method used: Cash on Delivery.
- [x] Order reference from real COD/sandbox order placement: `ORD-20260811-7A4002FC`.
- [x] Console error summary: final cart page reports 0 errors and 0 warnings.
- [x] Network guardrail summary: browser actions used same-origin `http://localhost:18598/api/*`; no Commerce Node, `/_blazor`, WebSocket, or EventSource browser request appeared.

## Phase 14 - Final Cleanup

- [x] Run `rg "StorefrontCartView.razor" BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM` and confirm no old implementation remains.
- [x] Run `rg "StorefrontCheckoutShell.razor" BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM` and confirm no old implementation remains.
- [x] Run `rg "IStorefrontBrowserCartController" BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM` and confirm no direct V2.WASM injection remains.
- [x] Run `rg "IStorefrontBrowserCheckoutController" BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM` and confirm no direct V2.WASM injection remains.
- [x] Run `rg "StorefrontCartViewClasses" BlazorShop.PresentationV2/BlazorShop.Storefront.Components BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM` and confirm the contract definition is in Components only.
- [x] Run `rg "StorefrontCheckoutViewClasses" BlazorShop.PresentationV2/BlazorShop.Storefront.Components BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM` and confirm the contract definition is in Components only.
- [x] Run `rg "StorefrontCartViewLabels|StorefrontCheckoutViewLabels" BlazorShop.PresentationV2` and confirm labels flow from V2 options to WasmHost components.
- [x] Run `rg "@rendermode|InteractiveServer|InteractiveWebAssembly|InteractiveAuto" BlazorShop.PresentationV2/BlazorShop.Storefront.Components.WasmHost` and confirm no results.
- [x] Run `rg "BlazorShop.Storefront.V2|BlazorShop.Storefront.V2.WASM" BlazorShop.PresentationV2/BlazorShop.Storefront.Components.WasmHost` and confirm no results.
- [x] Run `rg "BlazorShop.CommerceNode|BlazorShop.ControlPlane|BlazorShop.Domain|BlazorShop.Application|BlazorShop.Infrastructure" BlazorShop.PresentationV2/BlazorShop.Storefront.Components.WasmHost` and confirm no results.
- [x] Run `git diff --check`.
- [x] Review `git diff --stat`.
- [x] Review every modified file before commit.

Exit criteria:

- [x] Old runtime ownership has been removed from V2.WASM.
- [x] New shared runtime ownership is in WasmHost.
- [x] V2 behavior has not changed.
- [x] The implementation leaves no temporary duplicate component files.

## Phase 15 - Commit Checklist

Only commit after all verification is complete.

- [x] Confirm `git status --short` contains only intentional files.
- [x] Confirm no unrelated user changes were modified or reverted.
- [x] Confirm build verification results are recorded in the implementation summary.
- [x] Confirm test verification results are recorded in the implementation summary.
- [x] Confirm Playwright QA evidence is recorded in `QA-StorefrontV2.todo.md`.
- [x] Commit with a message similar to:

```text
refactor: move cart checkout wasm behavior to wasmhost
```

## Risk Register

Risk: V2 checkout page behavior changes accidentally.

- Mitigation: Keep `ShowPanel=false` and keep SSR checkout form components.
- Verification: Browser place-order flow with COD/sandbox payment.

Risk: V2.WASM and WasmHost component names collide.

- Mitigation: Use V2.WASM wrapper names `StorefrontCartSection` and `StorefrontCheckoutSection`.
- Verification: Build V2 and inspect imports.

Risk: Shared Components accidentally owns V2 copy or styling.

- Mitigation: Put final values in V2.WASM options only.
- Verification: Contract tests and source grep.

Risk: WasmHost accidentally depends on V2 or backend projects.

- Mitigation: Keep project references limited to Components and Browser.
- Verification: csproj/path-based tests.

Risk: Browser QA becomes a smoke test only.

- Mitigation: Require real cart mutation and real COD/sandbox order placement.
- Verification: Capture order reference and network guardrail evidence.

Risk: Tests preserve old architecture.

- Mitigation: Rewrite old tests that expect V2.WASM controller ownership.
- Verification: Add negative assertions against V2.WASM direct injection and lifecycle methods.

## Stop Conditions

Stop and re-review before continuing if:

- [n/a] A required cart or checkout behavior requires changing Browser controller APIs. No such change was required.
- [n/a] A required behavior requires changing BFF route contracts. No such change was required.
- [n/a] A required behavior requires changing Commerce Node Storefront API contracts. No such change was required.
- [n/a] Checkout shell must become visible on the production checkout page to pass tests. The SSR form remains visible and `ShowPanel=false` remains unchanged.
- [n/a] Any direct dependency from WasmHost to V2, V2.WASM, Runtime, Client, Commerce Node, Control Plane, Domain, Application, or Infrastructure appears necessary. No such dependency was required.
- [x] A Playwright order placement failure appears unrelated to the component move. It was re-reviewed and resolved as two Presentation serialization defects; no API or business-contract change was required.
- [n/a] The implementation touches account, product, header, footer, payment provider, order placement service, or StorefrontBuilder code. No such component/tooling change was made.

## Definition Of Done

- [x] Cart class contract lives in `BlazorShop.Storefront.Components/Contracts/Cart`.
- [x] Checkout class contract lives in `BlazorShop.Storefront.Components/Contracts/Checkout`.
- [x] Cart label contract lives in `BlazorShop.Storefront.Components/Contracts/Cart`.
- [x] Checkout label contract lives in `BlazorShop.Storefront.Components/Contracts/Checkout`.
- [x] Cart reusable implementation lives in `BlazorShop.Storefront.Components.WasmHost`.
- [x] Checkout shell reusable implementation lives in `BlazorShop.Storefront.Components.WasmHost`.
- [x] V2.WASM cart wrapper exists and owns V2 classes/labels.
- [x] V2.WASM checkout wrapper exists and owns V2 classes/labels.
- [x] V2.WASM no longer owns cart runtime controller injection.
- [x] V2.WASM no longer owns checkout runtime controller injection.
- [x] WasmHost has no render-mode directives.
- [x] V2 pages still own `InteractiveWebAssembly` placement.
- [x] V2 checkout page still uses the visible SSR checkout form for real order placement.
- [x] V2 checkout shell remains hidden with `ShowPanel=false` where it was hidden before.
- [x] Architecture docs are updated.
- [x] QA checklist is updated.
- [x] Focused builds pass.
- [x] Full solution build passes.
- [x] Focused tests pass.
- [x] Full V2 tests pass.
- [x] Playwright cart browser QA passes.
- [x] Playwright checkout real COD/sandbox place-order QA passes.
- [x] Network guardrails pass.
- [x] No unrelated files are changed.

## Decision Audit Trail

| # | Phase | Decision | Classification | Principle | Rationale | Rejected |
|---|-------|----------|----------------|-----------|-----------|----------|
| 1 | Autoplan CEO | Keep Phase 3.5 scoped to cart and checkout only. | Auto-decided | Preserve working product behavior | The current issue is ownership drift, not a need to redesign broader storefront features. | Extracting account/header/product in the same phase. |
| 2 | Autoplan Design | Keep V2 final copy and Tailwind values in V2.WASM options. | Auto-decided | Ownership clarity | Shared WasmHost must not become the source of final storefront presentation. | Moving final copy/classes into Components or WasmHost. |
| 3 | Autoplan Engineering | Move class contracts to Components contracts as mandatory work. | Auto-decided | Compile-time contract ownership | Without this move, V2.WASM still owns reusable contract shape and the extraction remains incomplete. | Leaving class bags in V2.WASM. |
| 4 | Autoplan Engineering | Use V2.WASM wrapper names `StorefrontCartSection` and `StorefrontCheckoutSection`. | Auto-decided | Maintainability | Wrapper names avoid ambiguity between reusable implementation and store-specific composition. | Keeping duplicate `StorefrontCartView` and `StorefrontCheckoutShell` names in V2.WASM. |
| 5 | Autoplan Engineering | Keep checkout `ShowPanel=false` behavior unchanged. | Auto-decided | No behavior rewrite | Current production checkout uses SSR form components for visible place-order flow. | Treating the extracted shell as the new visible checkout UI. |
| 6 | Autoplan DX | Require real Playwright cart and checkout flow QA, including COD/sandbox order placement. | Auto-decided | Production readiness | Smoke tests would not catch integration regressions in cart mutation, checkout submission, or browser network boundaries. | Relying on build/tests only. |

## Post Phase Requirement

- [x] After implementation and QA pass, run one fresh review against the resulting diff before starting the next component extraction phase. 2026-08-11: reviewed the baseline diff, all changed files, static ownership scans, browser evidence, and final test result.
- [x] The fresh review must specifically verify that no old V2.WASM manual runtime ownership remains for cart or checkout. 2026-08-11: cart/checkout paths have no V2.WASM controller injection or lifecycle methods; WasmHost owns both implementations.
- [x] Do not start Account extraction until this phase is closed.

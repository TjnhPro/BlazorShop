# Storefront Cart Checkout WasmHost Extraction

Status: planned
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

- [ ] `BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM/Components/Cart/StorefrontCartView.razor` currently owns cart browser behavior.
- [ ] `StorefrontCartView.razor` injects `IStorefrontBrowserCartController`.
- [ ] `StorefrontCartView.razor` owns lifecycle methods such as `Initialize`, `HydrateAsync`, `UpdateQuantityAsync`, `RemoveLineAsync`, and `ClearAsync`.
- [ ] `StorefrontCartView.razor` renders cart semantic hooks such as `data-storefront-cart-quantity`, `data-line-id`, `data-product-id`, `data-variant-id`, `data-product-name`, `data-size-value`, `data-storefront-cart-remove`, and `data-storefront-cart-clear`.
- [ ] `StorefrontCartView.razor` currently includes hardcoded English copy such as cart title, empty cart text, button text, and loading/error text.
- [ ] `BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM/Components/Cart/StorefrontCartViewClasses.cs` currently owns the cart class contract shape.
- [ ] `BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM/Components/Cart/StorefrontCartViewOptions.cs` currently owns final V2 class values.
- [ ] `BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM/Components/Checkout/StorefrontCheckoutShell.razor` currently owns checkout browser shell behavior.
- [ ] `StorefrontCheckoutShell.razor` injects `IStorefrontBrowserCheckoutController` and `NavigationManager`.
- [ ] `StorefrontCheckoutShell.razor` owns methods such as `RefreshAsync`, `SelectShippingAsync`, `SelectPaymentAsync`, `ReviewAsync`, and `PlaceOrderAsync`.
- [ ] `StorefrontCheckoutShell.razor` renders `data-storefront-checkout-shell` and `data-storefront-checkout-cart-version`.
- [ ] `StorefrontCheckoutShell.razor` currently includes hardcoded English copy such as checkout state, refresh, review, and place-order labels.
- [ ] `BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM/Components/Checkout/StorefrontCheckoutViewClasses.cs` currently owns the checkout class contract shape.
- [ ] `BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM/Components/Checkout/StorefrontCheckoutShellOptions.cs` currently owns final V2 class values.
- [ ] `BlazorShop.PresentationV2/BlazorShop.Storefront.Components.WasmHost/BlazorShop.Storefront.Components.WasmHost.csproj` already references `BlazorShop.Storefront.Components` and `BlazorShop.Storefront.Browser`.
- [ ] `BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM/BlazorShop.Storefront.V2.WASM.csproj` already references `BlazorShop.Storefront.Components.WasmHost`.
- [ ] `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/Hybrid/Commerce/CartPage.razor` currently renders the cart WASM view with `@rendermode="InteractiveWebAssembly"`.
- [ ] `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/Hybrid/Commerce/CheckoutPage.razor` currently renders `StorefrontCheckoutShell` with `ShowPanel="false"`.
- [ ] `CheckoutPage.razor` currently uses SSR Presentation components for the visible checkout form and real order placement: `StorefrontCheckoutForm`, `StorefrontCheckoutAddressFields`, `StorefrontCheckoutPaymentFields`, and `StorefrontCheckoutSubmit`.
- [ ] `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/_Imports.razor` currently imports V2.WASM cart and checkout namespaces and must be updated after extraction.

## Final Ownership Target

`BlazorShop.Storefront.Components`:

- [ ] Owns `Contracts/Cart/StorefrontCartViewClasses.cs`.
- [ ] Owns `Contracts/Cart/StorefrontCartViewLabels.cs`.
- [ ] Owns `Contracts/Checkout/StorefrontCheckoutViewClasses.cs`.
- [ ] Owns `Contracts/Checkout/StorefrontCheckoutViewLabels.cs`.
- [ ] Does not reference `BlazorShop.Storefront.Browser`.
- [ ] Does not reference `BlazorShop.Storefront.V2`.
- [ ] Does not reference `BlazorShop.Storefront.V2.WASM`.
- [ ] Does not own final V2 CSS class values.
- [ ] Does not own final V2 copy.

`BlazorShop.Storefront.Components.WasmHost`:

- [ ] Owns reusable `Components/Cart/StorefrontCartView.razor`.
- [ ] Owns reusable `Components/Checkout/StorefrontCheckoutShell.razor`.
- [ ] Injects browser controllers from `BlazorShop.Storefront.Browser`.
- [ ] Accepts class and label contract parameters from the host.
- [ ] Renders semantic `data-storefront-*` hooks needed by QA and future frontend generation.
- [ ] Does not contain `@rendermode`, `InteractiveServer`, `InteractiveWebAssembly`, or `InteractiveAuto`.
- [ ] Does not contain V2 final Tailwind values.
- [ ] Does not contain V2 route defaults that are not passed in by host options.

`BlazorShop.Storefront.V2.WASM`:

- [ ] Owns `Components/Cart/StorefrontCartSection.razor` thin wrapper.
- [ ] Owns `Components/Checkout/StorefrontCheckoutSection.razor` thin wrapper.
- [ ] Owns `StorefrontCartViewOptions` with final V2 class values and labels.
- [ ] Owns `StorefrontCheckoutShellOptions` with final V2 class values and labels.
- [ ] Does not inject `IStorefrontBrowserCartController` directly.
- [ ] Does not inject `IStorefrontBrowserCheckoutController` directly.
- [ ] Does not own cart or checkout lifecycle methods.
- [ ] Does not implement shared Presentation `IStorefront*Client` contracts.

`BlazorShop.Storefront.V2`:

- [ ] Keeps page placement and render-mode ownership.
- [ ] Keeps cart page behavior unchanged.
- [ ] Keeps checkout page behavior unchanged, including the current visible SSR form flow.
- [ ] Keeps `StorefrontCheckoutShell` hidden with `ShowPanel=false` unless a separate approved behavior phase changes that.

## Hard Scope Lock

Allowed production areas:

- [ ] `BlazorShop.PresentationV2/BlazorShop.Storefront.Components/Contracts/Cart/`
- [ ] `BlazorShop.PresentationV2/BlazorShop.Storefront.Components/Contracts/Checkout/`
- [ ] `BlazorShop.PresentationV2/BlazorShop.Storefront.Components.WasmHost/Components/Cart/`
- [ ] `BlazorShop.PresentationV2/BlazorShop.Storefront.Components.WasmHost/Components/Checkout/`
- [ ] `BlazorShop.PresentationV2/BlazorShop.Storefront.Components.WasmHost/_Imports.razor`
- [ ] `BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM/Components/Cart/`
- [ ] `BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM/Components/Checkout/`
- [ ] `BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM/_Imports.razor`
- [ ] `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/Hybrid/Commerce/CartPage.razor`
- [ ] `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/Hybrid/Commerce/CheckoutPage.razor`
- [ ] `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/_Imports.razor`

Allowed tests:

- [ ] `BlazorShop.Tests.V2/PresentationV2/Storefront/*ComponentMode*Tests.cs`
- [ ] `BlazorShop.Tests.V2/PresentationV2/Storefront/*RenderMode*Tests.cs`
- [ ] `BlazorShop.Tests.V2/PresentationV2/Storefront/*VisualOnlyBoundary*Tests.cs`
- [ ] `BlazorShop.Tests.V2/PresentationV2/Storefront/*V2WASMRuntimeFoundation*Tests.cs`
- [ ] `BlazorShop.Tests.V2/PresentationV2/Storefront/*CommerceFlowCutover*Tests.cs`
- [ ] `BlazorShop.Tests.V2/PresentationV2/Storefront/*RequiredVisualContracts*Tests.cs`
- [ ] Optional new focused tests under `BlazorShop.Tests.V2/PresentationV2/Storefront/`

Allowed docs:

- [ ] `BlazorShop.PresentationV2/COMPONENT-MODES.md`
- [ ] `docs/architecture/03-runtime-boundaries.md`
- [ ] `docs/architecture/05-project-and-folder-guide.md`
- [ ] `docs/architecture/10-v2-contract-ownership.md`
- [ ] `docs/refactor-control-Commerce-storefront/QA-StorefrontV2.todo.md`
- [ ] This plan file.

Forbidden changes:

- [ ] Do not change `BlazorShop.Storefront.Browser` controller public APIs.
- [ ] Do not change BFF endpoint paths.
- [ ] Do not change Commerce Node Storefront API paths.
- [ ] Do not change checkout order placement business logic.
- [ ] Do not change payment provider behavior.
- [ ] Do not change account components in this phase.
- [ ] Do not change header, footer, product detail, gallery, auth, payment, or order components in this phase.
- [ ] Do not touch StorefrontBuilder tooling.
- [ ] Do not touch Starter or generated storefront projects.
- [ ] Do not introduce React, JS framework code, or a new frontend package.
- [ ] Do not make WasmHost depend on V2, V2.WASM, Runtime, Client, Commerce Node, Control Plane, or domain/application/infrastructure projects.

## Naming Decision

Use V2.WASM wrapper names to avoid component ambiguity:

- [ ] `BlazorShop.Storefront.Components.WasmHost.Components.Cart.StorefrontCartView`
- [ ] `BlazorShop.Storefront.V2.WASM.Components.Cart.StorefrontCartSection`
- [ ] `BlazorShop.Storefront.Components.WasmHost.Components.Checkout.StorefrontCheckoutShell`
- [ ] `BlazorShop.Storefront.V2.WASM.Components.Checkout.StorefrontCheckoutSection`

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

- [ ] Create `BlazorShop.PresentationV2/BlazorShop.Storefront.Components/Contracts/Cart/`.
- [ ] Move or recreate `StorefrontCartViewClasses` under `Contracts/Cart`.
- [ ] Keep the public shape needed by the existing cart markup.
- [ ] Create `BlazorShop.PresentationV2/BlazorShop.Storefront.Components/Contracts/Checkout/`.
- [ ] Move or recreate `StorefrontCheckoutViewClasses` under `Contracts/Checkout`.
- [ ] Keep the public shape needed by the existing checkout shell markup.
- [ ] Delete the old class contract definitions from V2.WASM after all references are migrated.
- [ ] Update namespaces to `BlazorShop.Storefront.Components.Contracts.Cart` and `BlazorShop.Storefront.Components.Contracts.Checkout`.
- [ ] Ensure Components contracts remain browser-safe and do not depend on V2.WASM.

Guardrails:

- [ ] Do not move final Tailwind values into Components.
- [ ] Do not move V2 options into Components.
- [ ] Do not add service registrations in Components.
- [ ] Do not add `Microsoft.AspNetCore.Components.WebAssembly` dependencies to Components.

Focused checks:

```powershell
rg "class StorefrontCartViewClasses|record StorefrontCartViewClasses" BlazorShop.PresentationV2
rg "class StorefrontCheckoutViewClasses|record StorefrontCheckoutViewClasses" BlazorShop.PresentationV2
rg "StorefrontCartViewClasses" BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM
rg "StorefrontCheckoutViewClasses" BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM
```

Exit criteria:

- [ ] Exactly one cart class contract definition remains, under Components contracts.
- [ ] Exactly one checkout class contract definition remains, under Components contracts.
- [ ] V2.WASM references the shared contracts but does not define them.

## Phase 2 - Add Label Contracts

Tasks:

- [ ] Add `StorefrontCartViewLabels` under `BlazorShop.Storefront.Components/Contracts/Cart/`.
- [ ] Include labels for all current hardcoded cart copy, including title, heading, empty state, loading state, error state, quantity label, remove action, clear action, checkout action, continue shopping action, product link label where applicable, cart summary labels, and fallback item text.
- [ ] Add `StorefrontCheckoutViewLabels` under `BlazorShop.Storefront.Components/Contracts/Checkout/`.
- [ ] Include labels for all current hardcoded checkout shell copy, including heading, state label, refresh action, refreshing state, cart version label, shipping-not-required message, review action, place-order action, placing-order state, error/failure fallback, loading state, and selected option labels.
- [ ] Provide safe defaults only if existing components require non-null values to render during tests.
- [ ] Keep defaults neutral and technical if defaults are needed.
- [ ] Put V2 final English copy in V2.WASM options, not in shared Components contracts.

Guardrails:

- [ ] Shared labels are a contract shape, not the final storefront copy source.
- [ ] Do not add localization infrastructure in this phase.
- [ ] Do not add database-backed labels in this phase.
- [ ] Do not move route labels or page titles outside current scope.

Exit criteria:

- [ ] All cart user-facing text in the reusable cart component can be supplied by labels.
- [ ] All checkout shell user-facing text in the reusable checkout shell can be supplied by labels.
- [ ] V2.WASM owns the current final text values.

## Phase 3 - Move Cart Implementation Into WasmHost

Tasks:

- [ ] Create `BlazorShop.PresentationV2/BlazorShop.Storefront.Components.WasmHost/Components/Cart/`.
- [ ] Move the runtime markup and behavior from V2.WASM `StorefrontCartView.razor` into WasmHost `StorefrontCartView.razor`.
- [ ] Update the namespace to `BlazorShop.Storefront.Components.WasmHost.Components.Cart`.
- [ ] Inject `IStorefrontBrowserCartController` in the WasmHost component.
- [ ] Accept `StorefrontCartViewClasses` as a parameter.
- [ ] Accept `StorefrontCartViewLabels` as a parameter.
- [ ] Preserve `Initialize`, `HydrateAsync`, `UpdateQuantityAsync`, `RemoveLineAsync`, `ClearAsync`, and existing state update behavior.
- [ ] Preserve existing quantity validation and mutation call behavior.
- [ ] Preserve existing line rendering and item identity values.
- [ ] Preserve existing semantic hooks:
  - `data-storefront-cart-quantity`
  - `data-line-id`
  - `data-product-id`
  - `data-variant-id`
  - `data-product-name`
  - `data-size-value`
  - `data-storefront-cart-remove`
  - `data-storefront-cart-clear`
- [ ] Replace hardcoded copy with label contract usage.
- [ ] Ensure the component does not contain `@rendermode`.
- [ ] Ensure the component does not contain final V2 Tailwind class literals unless they already come through the supplied `Classes` contract.
- [ ] Update `BlazorShop.Storefront.Components.WasmHost/_Imports.razor` as needed.

Guardrails:

- [ ] WasmHost may reference `BlazorShop.Storefront.Browser`.
- [ ] WasmHost may reference `BlazorShop.Storefront.Components`.
- [ ] WasmHost must not reference V2 or V2.WASM.
- [ ] WasmHost must not create a new HTTP client transport.
- [ ] WasmHost must keep using Browser controllers, not generated backend clients.

Focused checks:

```powershell
rg "IStorefrontBrowserCartController" BlazorShop.PresentationV2/BlazorShop.Storefront.Components.WasmHost BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM
rg "@rendermode|InteractiveServer|InteractiveWebAssembly|InteractiveAuto" BlazorShop.PresentationV2/BlazorShop.Storefront.Components.WasmHost
rg "data-storefront-cart" BlazorShop.PresentationV2/BlazorShop.Storefront.Components.WasmHost
```

Exit criteria:

- [ ] WasmHost owns the cart behavior implementation.
- [ ] V2.WASM no longer owns cart controller injection or mutation lifecycle code.
- [ ] Cart semantic hooks remain available for browser QA.

## Phase 4 - Move Checkout Shell Implementation Into WasmHost

Tasks:

- [ ] Create `BlazorShop.PresentationV2/BlazorShop.Storefront.Components.WasmHost/Components/Checkout/`.
- [ ] Move the runtime markup and behavior from V2.WASM `StorefrontCheckoutShell.razor` into WasmHost `StorefrontCheckoutShell.razor`.
- [ ] Update the namespace to `BlazorShop.Storefront.Components.WasmHost.Components.Checkout`.
- [ ] Inject `IStorefrontBrowserCheckoutController` in the WasmHost component.
- [ ] Keep `NavigationManager` usage only if it is required for current redirect behavior.
- [ ] Accept `StorefrontCheckoutViewClasses` as a parameter.
- [ ] Accept `StorefrontCheckoutViewLabels` as a parameter.
- [ ] Preserve `RefreshAsync`, `SelectShippingAsync`, `SelectPaymentAsync`, `ReviewAsync`, `PlaceOrderAsync`, and current state update behavior.
- [ ] Preserve the `ShowPanel` parameter behavior.
- [ ] Preserve current hidden-shell behavior when `ShowPanel=false`.
- [ ] Preserve semantic hooks:
  - `data-storefront-checkout-shell`
  - `data-storefront-checkout-cart-version`
- [ ] Replace hardcoded copy with label contract usage.
- [ ] Ensure the component does not contain `@rendermode`.
- [ ] Ensure the component does not contain final V2 Tailwind class literals unless they come through the supplied `Classes` contract.
- [ ] Update `BlazorShop.Storefront.Components.WasmHost/_Imports.razor` as needed.

Checkout-specific caution:

- [ ] Do not make the WasmHost shell the visible production checkout UI in this phase.
- [ ] Do not remove the SSR Presentation checkout form from `CheckoutPage.razor`.
- [ ] Do not change order placement semantics.
- [ ] Do not change payment method selection semantics.
- [ ] Do not add tax UI.

Focused checks:

```powershell
rg "IStorefrontBrowserCheckoutController" BlazorShop.PresentationV2/BlazorShop.Storefront.Components.WasmHost BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM
rg "@rendermode|InteractiveServer|InteractiveWebAssembly|InteractiveAuto" BlazorShop.PresentationV2/BlazorShop.Storefront.Components.WasmHost
rg "data-storefront-checkout" BlazorShop.PresentationV2/BlazorShop.Storefront.Components.WasmHost
```

Exit criteria:

- [ ] WasmHost owns the checkout shell behavior implementation.
- [ ] V2.WASM no longer owns checkout controller injection or shell lifecycle code.
- [ ] Current V2 checkout page still uses the visible SSR checkout form for real browser order placement.

## Phase 5 - Create V2.WASM Cart Wrapper

Tasks:

- [ ] Add `BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM/Components/Cart/StorefrontCartSection.razor`.
- [ ] Make the wrapper render `BlazorShop.Storefront.Components.WasmHost.Components.Cart.StorefrontCartView`.
- [ ] Pass V2 cart classes from `StorefrontCartViewOptions`.
- [ ] Pass V2 cart labels from `StorefrontCartViewOptions`.
- [ ] Keep V2 final class values and V2 final English copy in the V2 options object.
- [ ] Keep wrapper markup minimal.
- [ ] Do not inject `IStorefrontBrowserCartController` in the wrapper.
- [ ] Delete or stop using the old V2.WASM `StorefrontCartView.razor`.
- [ ] Update namespaces and imports so V2 pages can render `StorefrontCartSection` without ambiguity.

Guardrails:

- [ ] Wrapper may configure V2 options.
- [ ] Wrapper may expose V2 page parameters if currently needed.
- [ ] Wrapper must not duplicate cart behavior logic.
- [ ] Wrapper must not duplicate the reusable cart markup.

Exit criteria:

- [ ] V2.WASM owns cart presentation values only.
- [ ] `CartPage.razor` renders the wrapper, not the shared component directly unless an explicit namespace alias is used.

## Phase 6 - Create V2.WASM Checkout Wrapper

Tasks:

- [ ] Add `BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM/Components/Checkout/StorefrontCheckoutSection.razor`.
- [ ] Make the wrapper render `BlazorShop.Storefront.Components.WasmHost.Components.Checkout.StorefrontCheckoutShell`.
- [ ] Pass V2 checkout classes from `StorefrontCheckoutShellOptions`.
- [ ] Pass V2 checkout labels from `StorefrontCheckoutShellOptions`.
- [ ] Preserve current `ShowPanel` parameter behavior.
- [ ] Keep V2 final class values and V2 final English copy in the V2 options object.
- [ ] Keep wrapper markup minimal.
- [ ] Do not inject `IStorefrontBrowserCheckoutController` in the wrapper.
- [ ] Delete or stop using the old V2.WASM `StorefrontCheckoutShell.razor`.
- [ ] Update namespaces and imports so V2 pages can render `StorefrontCheckoutSection` without ambiguity.

Guardrails:

- [ ] Wrapper may configure V2 options.
- [ ] Wrapper may expose `ShowPanel`.
- [ ] Wrapper must not duplicate checkout shell behavior logic.
- [ ] Wrapper must not duplicate the reusable checkout shell markup.

Exit criteria:

- [ ] V2.WASM owns checkout presentation values only.
- [ ] `CheckoutPage.razor` renders the V2 wrapper with `ShowPanel=false`.

## Phase 7 - Update V2 Page Integration

Cart page:

- [ ] Update `BlazorShop.Storefront.V2/Pages/Hybrid/Commerce/CartPage.razor` to render `StorefrontCartSection`.
- [ ] Keep `@rendermode="InteractiveWebAssembly"` on the page component usage.
- [ ] Preserve route, page title, metadata, and surrounding V2 layout.
- [ ] Preserve current empty-cart and loaded-cart behavior.

Checkout page:

- [ ] Update `BlazorShop.Storefront.V2/Pages/Hybrid/Commerce/CheckoutPage.razor` to render `StorefrontCheckoutSection`.
- [ ] Keep `@rendermode="InteractiveWebAssembly"` on the page component usage.
- [ ] Keep `ShowPanel=false` in the same places it is currently used unless explicitly approved otherwise.
- [ ] Preserve the visible SSR checkout form components.
- [ ] Preserve current checkout redirects.
- [ ] Preserve COD place-order flow.

Imports:

- [ ] Update `BlazorShop.Storefront.V2/_Imports.razor` to reference V2.WASM wrapper namespaces.
- [ ] Remove imports that point V2 server pages directly at old V2.WASM implementation namespaces if no longer needed.
- [ ] Avoid ambiguous component names between WasmHost and V2.WASM.

Exit criteria:

- [ ] Cart page behavior is unchanged.
- [ ] Checkout page behavior is unchanged.
- [ ] Render mode remains owned by V2 page placement, not WasmHost.

## Phase 8 - Rewrite Architecture Tests

Tests to inspect and update:

- [ ] `BlazorShop.Tests.V2/PresentationV2/Storefront/StorefrontComponentsHeadlessPresentationRefactorTests.cs`
- [ ] `BlazorShop.Tests.V2/PresentationV2/Storefront/StorefrontV2WASMRuntimeFoundationTests.cs`
- [ ] `BlazorShop.Tests.V2/PresentationV2/Storefront/StorefrontCommerceFlowCutoverTests.cs`
- [ ] `BlazorShop.Tests.V2/PresentationV2/Storefront/StorefrontRequiredVisualContractsHardeningTests.cs`
- [ ] Any test found by `rg "StorefrontCartView|StorefrontCheckoutShell|StorefrontCartViewClasses|StorefrontCheckoutViewClasses" BlazorShop.Tests.V2`

Required new assertions:

- [ ] WasmHost project contains cart and checkout implementation components.
- [ ] WasmHost cart component injects `IStorefrontBrowserCartController`.
- [ ] WasmHost checkout shell injects `IStorefrontBrowserCheckoutController`.
- [ ] WasmHost components do not contain `@rendermode` or interactive render-mode directives.
- [ ] WasmHost components do not reference V2 or V2.WASM namespaces.
- [ ] Components contracts own `StorefrontCartViewClasses` and `StorefrontCheckoutViewClasses`.
- [ ] Components contracts own `StorefrontCartViewLabels` and `StorefrontCheckoutViewLabels`.
- [ ] V2.WASM wrappers do not inject browser controllers.
- [ ] V2.WASM wrappers do not contain lifecycle methods such as `HydrateAsync`, `UpdateQuantityAsync`, `ClearAsync`, `RefreshAsync`, `ReviewAsync`, and `PlaceOrderAsync`.
- [ ] V2.WASM wrappers render WasmHost components and pass V2 options.
- [ ] V2 pages own `InteractiveWebAssembly` render mode.
- [ ] Checkout page still renders the shell with `ShowPanel=false`.
- [ ] Checkout page still renders SSR checkout form components for real checkout.

Required removed or corrected assertions:

- [ ] Remove assertions that V2.WASM implementation files own browser controller injection.
- [ ] Remove assertions that V2.WASM implementation files own runtime lifecycle methods.
- [ ] Remove assertions that checkout shell visible actions are part of the public page when `ShowPanel=false`.

Focused test filters:

```powershell
dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~StorefrontComponentsHeadlessPresentationRefactorTests"
dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~StorefrontV2WASMRuntimeFoundationTests"
dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~StorefrontCommerceFlowCutoverTests"
dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~StorefrontRequiredVisualContractsHardeningTests"
```

Exit criteria:

- [ ] Tests describe the new ownership accurately.
- [ ] Tests no longer preserve the old boundary leak.
- [ ] Tests fail if a later agent moves browser runtime behavior back into V2.WASM.

## Phase 9 - Add Boundary Guardrails

Add or update tests to enforce:

- [ ] `BlazorShop.Storefront.Components.WasmHost` references only allowed Storefront shared projects.
- [ ] `BlazorShop.Storefront.Components.WasmHost` does not reference V2, V2.WASM, Runtime, Client, Commerce Node, Control Plane, Domain, Application, or Infrastructure.
- [ ] `BlazorShop.Storefront.Components.WasmHost` does not contain `@rendermode`.
- [ ] `BlazorShop.Storefront.Components.WasmHost` does not contain `InteractiveServer`, `InteractiveWebAssembly`, or `InteractiveAuto`.
- [ ] `BlazorShop.Storefront.Components.WasmHost` does not contain final V2 route defaults that belong to V2 options.
- [ ] `BlazorShop.Storefront.V2.WASM` wrappers do not inject `IStorefrontBrowserCartController`.
- [ ] `BlazorShop.Storefront.V2.WASM` wrappers do not inject `IStorefrontBrowserCheckoutController`.
- [ ] `BlazorShop.Storefront.V2.WASM` does not contain duplicated cart or checkout mutation methods after extraction.
- [ ] `BlazorShop.Storefront.Components` does not reference Browser, V2, V2.WASM, Runtime, Client, Commerce Node, Control Plane, Domain, Application, or Infrastructure.

Recommended test names:

- [ ] `StorefrontWasmHostComponentOwnershipTests`
- [ ] `StorefrontV2WasmWrapperBoundaryTests`
- [ ] `StorefrontSharedContractOwnershipTests`

Exit criteria:

- [ ] Boundary tests fail on the most likely future regressions.
- [ ] Boundary tests are path based and deterministic.
- [ ] Boundary tests do not require running a browser.

## Phase 10 - Update Documentation

Update `BlazorShop.PresentationV2/COMPONENT-MODES.md`:

- [ ] Document that WasmHost owns browser-interactive reusable components.
- [ ] Document that V2.WASM owns wrappers and final V2 presentation values.
- [ ] Document that V2 pages own render mode.
- [ ] Add cart and checkout as concrete examples.
- [ ] Clarify that checkout shell extraction does not make the shell the visible checkout form in V2.

Update `docs/architecture/03-runtime-boundaries.md`:

- [ ] Clarify Storefront browser actions go through Browser/BFF primitives and same-origin routes.
- [ ] Clarify WasmHost must not call Commerce Node directly.
- [ ] Clarify V2 server pages own render-mode placement.

Update `docs/architecture/05-project-and-folder-guide.md`:

- [ ] Add or update `BlazorShop.Storefront.Components.WasmHost` responsibilities.
- [ ] Add or update `BlazorShop.Storefront.V2.WASM` responsibilities.
- [ ] Add or update `BlazorShop.Storefront.Components` contract responsibilities.

Update `docs/architecture/10-v2-contract-ownership.md`:

- [ ] Clarify class and label contracts for cart/checkout live in shared Components contracts.
- [ ] Clarify final copy and final class values live in store-specific wrapper/options.

Update `docs/refactor-control-Commerce-storefront/QA-StorefrontV2.todo.md`:

- [ ] Add Phase 3.5 evidence checklist.
- [ ] Add Playwright cart browser QA cases.
- [ ] Add Playwright checkout browser QA cases.
- [ ] Add boundary verification evidence commands.

Exit criteria:

- [ ] Documentation matches actual code ownership.
- [ ] No doc claims that V2.WASM owns reusable cart/checkout behavior.
- [ ] No doc claims that checkout shell is the visible production checkout form while `ShowPanel=false`.

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

- [ ] Components builds.
- [ ] Browser builds.
- [ ] WasmHost builds.
- [ ] V2.WASM builds.
- [ ] V2 builds.
- [ ] Full solution builds.

Failure handling:

- [ ] If a build fails because of namespace ambiguity, prefer wrapper naming/import cleanup over direct fully-qualified markup everywhere.
- [ ] If a build fails because of missing label/class properties, update the shared contract shape to match existing markup.
- [ ] If a build fails because WasmHost needs a Browser type, confirm the Browser dependency already exists in WasmHost csproj before adding anything.

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

- [ ] Focused cart tests pass.
- [ ] Focused checkout tests pass.
- [ ] WasmHost boundary tests pass.
- [ ] Component mode dependency tests pass.
- [ ] Render mode ownership tests pass.
- [ ] Visual-only boundary tests pass.
- [ ] Commerce flow cutover tests pass.
- [ ] Required visual contract tests pass.
- [ ] Full `BlazorShop.Tests.V2` passes.

## Phase 13 - Playwright Browser QA

Use the local V2 runner unless an implementation session has an existing equivalent runtime:

```powershell
.\scripts\run-v2-local.ps1 -StopExisting -NoOpenBrowser
```

Cart browser QA:

- [ ] Open the cart page when the cart is empty.
- [ ] Confirm empty state renders without console errors.
- [ ] Add a product to cart from product detail or existing supported path.
- [ ] Open cart page with one item.
- [ ] Confirm product name, quantity, unit/line pricing, image/fallback, and checkout link render.
- [ ] Confirm `data-storefront-cart-quantity` is present.
- [ ] Confirm each line has `data-line-id`.
- [ ] Confirm each line has `data-product-id`.
- [ ] Confirm variant products include `data-variant-id` where available.
- [ ] Confirm selected attributes include `data-size-value` where available.
- [ ] Increase quantity.
- [ ] Confirm server-backed state updates and total/count refresh.
- [ ] Decrease quantity.
- [ ] Confirm state updates and no duplicate mutation occurs.
- [ ] Remove one line.
- [ ] Confirm line disappears and summary updates.
- [ ] Add multiple items if fixture supports it.
- [ ] Clear cart.
- [ ] Confirm empty state returns.
- [ ] Confirm product links still navigate.
- [ ] Confirm checkout link still navigates.
- [ ] Confirm no direct Commerce Node URL appears in browser network requests.
- [ ] Confirm browser actions use same-origin BFF routes.
- [ ] Confirm no `/_blazor` server circuit connection appears for cart page.
- [ ] Confirm no unexpected WebSocket or EventSource connection appears.
- [ ] Confirm no page errors.
- [ ] Confirm no console errors except known benign framework diagnostics already accepted by QA.

Checkout browser QA:

- [ ] Create a valid cart with an active item.
- [ ] Navigate to checkout.
- [ ] Confirm current visible checkout form still renders through SSR Presentation components.
- [ ] Confirm hidden `StorefrontCheckoutSection` / shell behavior does not create visible duplicate checkout controls while `ShowPanel=false`.
- [ ] Confirm `data-storefront-checkout-shell` exists only if the hidden shell currently rendered it before extraction; otherwise document the unchanged behavior.
- [ ] Fill billing/shipping fields according to current V2 fixture.
- [ ] Select COD payment.
- [ ] Submit real place-order flow.
- [ ] Confirm order completion page or expected provider redirect behavior.
- [ ] Confirm order number/reference is displayed if current V2 flow displays it.
- [ ] Confirm cart is closed or cleared according to existing checkout rule.
- [ ] Confirm browser network does not call direct Commerce Node URL.
- [ ] Confirm browser actions use same-origin BFF routes.
- [ ] Confirm no `/_blazor` server circuit connection appears for checkout page.
- [ ] Confirm no unexpected WebSocket or EventSource connection appears.
- [ ] Confirm no page errors.
- [ ] Confirm no console errors except known benign framework diagnostics already accepted by QA.

Optional shell behavior QA:

- [ ] If a component test page or fixture supports `ShowPanel=true`, test `RefreshAsync` through the UI.
- [ ] If a component test page or fixture supports `ShowPanel=true`, test shipping option selection through the UI.
- [ ] If a component test page or fixture supports `ShowPanel=true`, test payment option selection through the UI.
- [ ] If a component test page or fixture supports `ShowPanel=true`, test review and place-order command dispatch with a mocked or fixture-safe Browser controller.
- [ ] Do not treat missing visible shell controls on production checkout page as a failure while `ShowPanel=false` remains current behavior.

Evidence to capture:

- [ ] Storefront URL used.
- [ ] Store key used.
- [ ] Browser mode used, headless or visible.
- [ ] Product fixture used.
- [ ] Payment method used.
- [ ] Order reference from real COD/sandbox order placement.
- [ ] Console error summary.
- [ ] Network guardrail summary.

## Phase 14 - Final Cleanup

- [ ] Run `rg "StorefrontCartView.razor" BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM` and confirm no old implementation remains.
- [ ] Run `rg "StorefrontCheckoutShell.razor" BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM` and confirm no old implementation remains.
- [ ] Run `rg "IStorefrontBrowserCartController" BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM` and confirm no direct V2.WASM injection remains.
- [ ] Run `rg "IStorefrontBrowserCheckoutController" BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM` and confirm no direct V2.WASM injection remains.
- [ ] Run `rg "StorefrontCartViewClasses" BlazorShop.PresentationV2/BlazorShop.Storefront.Components BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM` and confirm the contract definition is in Components only.
- [ ] Run `rg "StorefrontCheckoutViewClasses" BlazorShop.PresentationV2/BlazorShop.Storefront.Components BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM` and confirm the contract definition is in Components only.
- [ ] Run `rg "StorefrontCartViewLabels|StorefrontCheckoutViewLabels" BlazorShop.PresentationV2` and confirm labels flow from V2 options to WasmHost components.
- [ ] Run `rg "@rendermode|InteractiveServer|InteractiveWebAssembly|InteractiveAuto" BlazorShop.PresentationV2/BlazorShop.Storefront.Components.WasmHost` and confirm no results.
- [ ] Run `rg "BlazorShop.Storefront.V2|BlazorShop.Storefront.V2.WASM" BlazorShop.PresentationV2/BlazorShop.Storefront.Components.WasmHost` and confirm no results.
- [ ] Run `rg "BlazorShop.CommerceNode|BlazorShop.ControlPlane|BlazorShop.Domain|BlazorShop.Application|BlazorShop.Infrastructure" BlazorShop.PresentationV2/BlazorShop.Storefront.Components.WasmHost` and confirm no results.
- [ ] Run `git diff --check`.
- [ ] Review `git diff --stat`.
- [ ] Review every modified file before commit.

Exit criteria:

- [ ] Old runtime ownership has been removed from V2.WASM.
- [ ] New shared runtime ownership is in WasmHost.
- [ ] V2 behavior has not changed.
- [ ] The implementation leaves no temporary duplicate component files.

## Phase 15 - Commit Checklist

Only commit after all verification is complete.

- [ ] Confirm `git status --short` contains only intentional files.
- [ ] Confirm no unrelated user changes were modified or reverted.
- [ ] Confirm build verification results are recorded in the implementation summary.
- [ ] Confirm test verification results are recorded in the implementation summary.
- [ ] Confirm Playwright QA evidence is recorded in `QA-StorefrontV2.todo.md`.
- [ ] Commit with a message similar to:

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

- [ ] A required cart or checkout behavior requires changing Browser controller APIs.
- [ ] A required behavior requires changing BFF route contracts.
- [ ] A required behavior requires changing Commerce Node Storefront API contracts.
- [ ] Checkout shell must become visible on the production checkout page to pass tests.
- [ ] Any direct dependency from WasmHost to V2, V2.WASM, Runtime, Client, Commerce Node, Control Plane, Domain, Application, or Infrastructure appears necessary.
- [ ] A Playwright order placement failure appears unrelated to the component move.
- [ ] The implementation touches account, product, header, footer, payment provider, order placement service, or StorefrontBuilder code.

## Definition Of Done

- [ ] Cart class contract lives in `BlazorShop.Storefront.Components/Contracts/Cart`.
- [ ] Checkout class contract lives in `BlazorShop.Storefront.Components/Contracts/Checkout`.
- [ ] Cart label contract lives in `BlazorShop.Storefront.Components/Contracts/Cart`.
- [ ] Checkout label contract lives in `BlazorShop.Storefront.Components/Contracts/Checkout`.
- [ ] Cart reusable implementation lives in `BlazorShop.Storefront.Components.WasmHost`.
- [ ] Checkout shell reusable implementation lives in `BlazorShop.Storefront.Components.WasmHost`.
- [ ] V2.WASM cart wrapper exists and owns V2 classes/labels.
- [ ] V2.WASM checkout wrapper exists and owns V2 classes/labels.
- [ ] V2.WASM no longer owns cart runtime controller injection.
- [ ] V2.WASM no longer owns checkout runtime controller injection.
- [ ] WasmHost has no render-mode directives.
- [ ] V2 pages still own `InteractiveWebAssembly` placement.
- [ ] V2 checkout page still uses the visible SSR checkout form for real order placement.
- [ ] V2 checkout shell remains hidden with `ShowPanel=false` where it was hidden before.
- [ ] Architecture docs are updated.
- [ ] QA checklist is updated.
- [ ] Focused builds pass.
- [ ] Full solution build passes.
- [ ] Focused tests pass.
- [ ] Full V2 tests pass.
- [ ] Playwright cart browser QA passes.
- [ ] Playwright checkout real COD/sandbox place-order QA passes.
- [ ] Network guardrails pass.
- [ ] No unrelated files are changed.

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

- [ ] After implementation and QA pass, run one fresh review against the resulting diff before starting the next component extraction phase.
- [ ] The fresh review must specifically verify that no old V2.WASM manual runtime ownership remains for cart or checkout.
- [ ] Do not start Account extraction until this phase is closed.

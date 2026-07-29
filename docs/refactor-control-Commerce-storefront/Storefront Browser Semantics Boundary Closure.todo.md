# Storefront Browser Semantics Boundary Closure Todo

Status: Proposed
Owner: Storefront Platform
Created: 2026-07-28
Related plans:
- `Storefront Browser Action Boundary Closure.todo.md`
- `Storefront Foundation Blocker Closure.todo.md`
- `Storefront Components Headless Presentation Refactor.todo.md`
- `Storefront Starter Foundation.todo.md`
- `Storefront Playwright E2E Release.todo.md`

Scope: close the remaining browser semantics boundary after browser command transport moved into `BlazorShop.Storefront.Presentation`. The goal is to make V2, Starter, and generated storefronts visual consumers only: they render descriptors and presentation-ready event values, while Presentation owns product selection semantics, checkout form semantics, command dispatch, and proof gates.

## Current Codebase Findings

These findings were verified against the current codebase before writing this plan.

- [x] `BlazorShop.Storefront.V2/Components/Product/StorefrontProductPurchasePanel.razor` still chooses the first required option value through `ShouldSelectOptionValue(...)`. 2026-07-29 F1.57 verified and replaced with `value.IsSelected`.
- [x] `ProductPurchaseOptionItem` only carries `Name`, `IsRequired`, `ControlType`, and `Values`. 2026-07-29 F1.57 verified; selected state belongs to value items.
- [x] `ProductPurchaseOptionValueItem` only carries `Value` and `ColorHex`. 2026-07-29 F1.57 extended with `IsSelected`.
- [x] `StorefrontProductPageMapper` builds option values without default/selected state. 2026-07-29 F1.57 maps selected values from valid default-variant attributes.
- [ ] `BlazorShop.Storefront.V2/wwwroot/js/storefrontCommerce.js` still owns saved-address/manual-address show-hide and disabled field behavior.
- [ ] No active Presentation checkout markup currently emits `data-storefront-address-select`, so the V2 checkout address selector code appears stale or dead.
- [ ] Presentation `storefront.application.js` builds semantic `skuText` and `gtinText` from product-selection preview.
- [ ] V2 product page renders `data-storefront-selection-sku` and `data-storefront-selection-gtin`.
- [ ] V2 selection listener updates price, compare price, stock, image, and button state, but not SKU/GTIN targets.
- [ ] Presentation product purchase binder selects `[data-storefront-product-purchase-submit]` and legacy aliases without enforcing `data-storefront-command="cart.add-line"`.
- [ ] Presentation script publicly exposes command-capable methods under `window.blazorShopStorefront.application` and `window.blazorShopStorefront.bindings`.
- [ ] `root.bindings.addToCart.addPurchaseLine` and `root.bindings.productSelection.previewPurchase` have no verified visual consumer and can re-enable command calls from visual hosts.
- [ ] Starter product page passes only `ProductName` into `ProductDetailShell`.
- [ ] Starter `PurchasePanelPlaceholder` renders a permanently disabled button and does not emit product purchase descriptors.
- [ ] StorefrontBuilder still patches Starter by string replacement to add `PurchasePanel`, `PurchaseActions`, and product purchase descriptors.
- [ ] Fast foundation functional proof uses mocked `pageHtml()` and injects Presentation script manually instead of launching an actual generated host.
- [ ] Visual boundary validator still relies on broad substring tokens such as `sku` and `gtin`, causing false positives for presentation-ready `selection.skuText` and `selection.gtinText`.
- [ ] Presentation still supports legacy selector aliases such as `data-storefront-selection-preview`, `data-storefront-add-to-cart`, and `data-storefront-generated-quantity`.

## Architecture Decision

Use this final browser semantics boundary:

```text
Commerce Node Storefront API
    returns raw product/cart/checkout state
        |
        v
Storefront.Runtime / Presentation services
    map raw state into safe contracts and descriptors
        |
        v
Presentation browser binders
    read descriptors
    enforce command names
    build command payloads
    call same-origin BFF routes
    publish semantic browser events
        |
        v
V2 / Starter / generated visual layer
    render descriptors
    listen to events
    update DOM with presentation-ready text
    animate, focus, style, and toast only
```

Visual hosts may:

- [x] render `checked` or `selected` from explicit contract state. 2026-07-29 F1.57 V2 purchase panel renders from `ProductPurchaseOptionValueItem.IsSelected`.
- [ ] display presentation-ready event values such as `selection.priceText`, `selection.stockText`, `selection.skuText`, and `selection.gtinText`.
- [ ] toggle CSS classes and visual visibility from semantic event values.
- [ ] manage gallery keyboard navigation and visual thumbnail state.
- [ ] focus fields, animate sections, and show toast copy supplied by the host or event descriptor.

Visual hosts must not:

- [x] choose the first required product option as a business default. 2026-07-29 F1.57 removed V2 first-value selection fallback.
- [x] infer selected variant or purchasability from DOM option order. 2026-07-29 F1.57 initial attributes now come from Presentation-selected value state.
- [ ] decide which checkout form fields participate in form submission.
- [ ] read raw preview fields such as `preview.sku`, `preview.gtin`, `preview.stockQuantity`, or `preview.canAddToCart`.
- [ ] call command-capable `application.*` or `bindings.*` methods from visual scripts.
- [ ] rely on legacy selectors after canonical descriptors are available.
- [ ] patch behavior contracts into Starter through fragile string replacement.

## Non-goals

- [ ] Do not change Commerce Node product-selection preview response shape in this phase unless a contract bug blocks selected-state mapping.
- [ ] Do not redesign the V2 product page layout.
- [ ] Do not add full saved-address checkout UX unless an active endpoint and markup already exist.
- [ ] Do not move gallery visual behavior into Presentation; gallery interaction can stay visual as long as it consumes semantic image URLs.
- [ ] Do not replace all validator logic with AST in the first implementation phase if targeted token tightening is enough to unblock P0/P1.
- [ ] Do not run live COD checkout as part of the fast generated proof; keep full live Commerce proof for release/nightly.

## Phase F1.57 - Product Option Selection Contract

Goal: remove product option default-selection semantics from V2 Razor and make selected state explicit in shared contracts.

### Implementation

- [x] Extend `ProductPurchaseOptionValueItem` with `bool IsSelected`.
- [x] Keep the constructor change source-compatible only where practical; update all construction sites explicitly rather than adding hidden behavior.
- [x] In `StorefrontProductPageMapper`, compute selected option values from the current product/variant data.
- [x] Use the product's default variant when a variation template exists and the selected attributes can be inferred.
- [x] If no default variant can be resolved, allow no selected value rather than choosing `Values[0]` in V2.
- [x] Preserve the existing non-template variant select behavior that uses `ProductPurchaseVariantItem.IsDefault`.
- [x] Update `ProductPurchaseSelectionState.FromSnapshot(...)` so initial selected attributes match explicit selected option values.
- [x] Replace `ShouldSelectOptionValue(...)` in V2 with direct `value.IsSelected`.
- [x] Ensure optional options do not auto-select a value unless the mapper explicitly sets `IsSelected`.
- [x] Ensure required options can render with no selected value if the backend has no valid default; the Presentation binder must then preview/validate normally.

### Tests

- [x] Add or update a mapper test proving selected option values come from the default variant, not collection order.
- [x] Add a V2 markup regression test proving radio and select render `checked`/`selected` from `value.IsSelected`.
- [x] Add a negative test proving required option values are not selected merely because they are first.
- [x] Run focused Storefront tests for product page mapping and V2 markup. 2026-07-29 `dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --filter "FullyQualifiedName~StorefrontProductDecisionContextTests|FullyQualifiedName~StorefrontBrandingMarkupTests" --no-restore` passed 24/24; existing MessagePack vulnerability warnings remain.

### Acceptance Criteria

- [x] `StorefrontProductPurchasePanel.razor` no longer contains `values[0]` selection logic.
- [x] `ProductPurchaseOptionValueItem` exposes explicit selected state.
- [x] Initial payload descriptors reflect Presentation-selected option state.
- [x] Product selection preview still runs after initial render and after user changes option.

## Phase F1.58 - Checkout Address Form Behavior Boundary

Goal: remove checkout form submission semantics from V2 JavaScript.

### Implementation

- [ ] Confirm whether any active checkout markup emits `data-storefront-address-select`.
- [ ] If no active markup emits it, delete the V2 address select/manual address functions and listeners.
- [ ] If active saved-address markup is reintroduced before this phase lands, move the binder into Presentation `storefront.application.js` instead.
- [ ] For the Presentation binder path, define canonical descriptors:
  - [ ] `data-storefront-checkout-address-mode`
  - [ ] `data-storefront-address-select`
  - [ ] `data-storefront-manual-address`
  - [ ] `data-storefront-manual-address-field`
- [ ] Presentation must own field enabling/disabling and required behavior because disabled fields change POST payload semantics.
- [ ] V2 may listen to `storefront:checkout:address-mode-changed` only for animation, focus, and CSS class updates.
- [ ] Update V2 visual script tests to forbid `data-storefront-address-select`, `manualAddressFieldSelector`, and field `.disabled` behavior when no active visual-only need exists.

### Tests

- [ ] Add a test proving V2 script does not contain checkout form field disabling logic.
- [ ] If Presentation binder is added, add a browser/unit test proving saved-address mode disables manual fields and manual mode enables them.
- [ ] Add a regression check that checkout POST form field names remain owned by Presentation components.

### Acceptance Criteria

- [ ] V2 JavaScript no longer decides which checkout address inputs are submitted.
- [ ] Dead checkout selector code is removed instead of left as compatibility.
- [ ] Any future saved-address behavior has a Presentation-owned binder and semantic event.

## Phase F1.59 - Product Selection Visual Projection Completeness

Goal: make V2 update all visible product selection targets from presentation-ready event values.

### Implementation

- [ ] Keep raw preview interpretation in Presentation `normalizePreview(...)`.
- [ ] Ensure semantic selection result includes:
  - [ ] `priceText`
  - [ ] `comparePriceText`
  - [ ] `stockText`
  - [ ] `skuText`
  - [ ] `gtinText`
  - [ ] `mainImageUrl`
  - [ ] `message`
  - [ ] `ready`
  - [ ] `valid`
- [ ] Update V2 `applySelectionVisual(...)` to find:
  - [ ] `data-storefront-selection-sku`
  - [ ] `data-storefront-selection-gtin`
- [ ] Set SKU/GTIN text from `selection.skuText` and `selection.gtinText`.
- [ ] Hide SKU/GTIN visual targets when the presentation-ready value is empty.
- [ ] Do not read `preview.sku`, `preview.gtin`, raw `sku`, raw `gtin`, or SKU/GTIN values from response payload in V2.
- [ ] Keep gallery image switching from `selection.mainImageUrl`.

### Tests

- [ ] Update `StorefrontCommerceScriptRegressionTests` to require V2 updates SKU and GTIN from `selection.skuText` / `selection.gtinText`.
- [ ] Update visual boundary validator so `selection.skuText` and `selection.gtinText` are allowed.
- [ ] Keep raw business tokens forbidden with more precise tokens:
  - [ ] `preview.sku`
  - [ ] `preview.gtin`
  - [ ] `preview.stockQuantity`
  - [ ] `preview.canAddToCart`
- [ ] Add a Playwright/browser regression where changing variant updates price, stock, image, SKU, and GTIN together.

### Acceptance Criteria

- [ ] SKU/GTIN no longer stay stale after variant selection changes.
- [ ] V2 consumes presentation-ready selection values only.
- [ ] Guardrail does not block `skuText` / `gtinText` false positives.

## Phase F1.60 - Command Descriptor Enforcement And Private Binder Surface

Goal: make Presentation command dispatch explicit and stop exposing command-capable internals to visual hosts.

### Implementation

- [ ] Change product purchase submit selector to require the canonical command descriptor:
  - [ ] `[data-storefront-command="cart.add-line"][data-storefront-product-purchase-submit]`
- [ ] Alternatively implement a small dispatcher that switches on `element.dataset.storefrontCommand`.
- [ ] Unknown command values must be ignored and publish a contract error event rather than executing add-to-cart.
- [ ] Remove legacy submit alias support from command execution.
- [ ] Make `previewPurchase`, `addPurchaseLine`, payload builders, and request helpers private inside the IIFE.
- [ ] Replace public `root.bindings` command surface with:
  - [ ] `root.events`
  - [ ] optional `root.initialize()`
- [ ] Add idempotency guard:
  - [ ] repeated initialize calls do not register document listeners multiple times.
- [ ] Decide whether `root.application` remains public:
  - [ ] If no required external consumer exists, remove it.
  - [ ] If it must remain for compatibility, expose read-only event names only and mark command methods unavailable to visual consumers.
- [ ] Update all tests that currently assert `root.bindings` exists.

### Tests

- [ ] Add a test proving a button with `data-storefront-product-purchase-submit` but no `data-storefront-command="cart.add-line"` does not call `/api/cart/lines`.
- [ ] Add a test proving wrong command values are ignored or produce a contract error event.
- [ ] Add a test proving double initialization does not double-submit commands.
- [ ] Update visual boundary validator to forbid:
  - [ ] `blazorShopStorefront.bindings.addToCart`
  - [ ] `blazorShopStorefront.bindings.productSelection`
  - [ ] `addPurchaseLine(`
  - [ ] `previewPurchase(`
  - [ ] direct command-capable `root.application` use by visual consumers.

### Acceptance Criteria

- [ ] A descriptor typo cannot silently execute add-to-cart.
- [ ] Visual scripts cannot call Presentation command internals.
- [ ] Presentation initialization is idempotent.
- [ ] Existing V2 add-to-cart behavior still works through canonical descriptors.

## Phase F1.61 - Starter Functional Reference Cutover

Goal: make Starter the canonical minimal functional reference instead of relying on generator string replacement to add behavior contracts.

### Implementation

- [ ] Update Starter `Pages/Hybrid/Catalog/ProductPage.razor` to pass:
  - [ ] `PurchasePanel="@Context.PurchasePanel"`
  - [ ] `PurchaseActions="@Context.PurchaseActions"`
- [ ] Update Starter `ProductDetailShell.razor` to accept and forward `ProductPurchasePanelModel` and `ProductPurchaseActionDescriptor`.
- [ ] Update Starter `PurchasePanelPlaceholder.razor` to emit canonical product purchase descriptors:
  - [ ] `data-storefront-product-purchase`
  - [ ] `data-selection-preview-route`
  - [ ] `data-product-id`
  - [ ] `data-product-name`
  - [ ] `data-resolved-variant-id`
  - [ ] `data-currency-code`
  - [ ] `data-storefront-command="cart.add-line"`
  - [ ] `data-storefront-product-purchase-submit`
  - [ ] `data-storefront-purchase-quantity`
  - [ ] `data-storefront-purchase-feedback`
- [ ] Keep Starter UI visually neutral and minimal.
- [ ] Remove generator transforms that patch missing `PurchasePanel` and `PurchaseActions` into Starter.
- [ ] Keep generator transforms limited to layout, classes, copy, placement, and generated visual CSS.
- [ ] Update `starter-generation.contract.yaml` to state these descriptors are part of Starter baseline, not generated behavior injection.

### Tests

- [ ] Update Starter foundation tests to assert product page passes purchase contracts.
- [ ] Update StorefrontBuilder regeneration tests to assert generator no longer string-replaces missing purchase contracts.
- [ ] Update composition validation to require descriptors in Starter baseline and generated output.
- [ ] Build Starter and generated proof project.

### Acceptance Criteria

- [ ] Starter can demonstrate product purchase descriptor pattern without generator mutation.
- [ ] Generated storefront behavior no longer depends on exact Starter source text for functional contract insertion.
- [ ] Generator remains visual/composition-only for product purchase.

## Phase F1.62 - Actual Generated Host Fast Proof

Goal: make the required fast proof run a real generated storefront host with Presentation static web assets and Runtime registration.

### Implementation

- [ ] Replace `run-fast-foundation-functional.mjs` document fulfillment with an actual generated host launch.
- [ ] Use deterministic fake same-origin BFF responses for `/api/cart`, `/api/cart/lines`, `/api/product-selection-preview`, `/api/consent/*`, and required checkout/account endpoints.
- [ ] Let the generated host render Razor markup and load static web assets normally.
- [ ] Do not manually inject `storefront.application.js` with `page.addScriptTag`.
- [ ] Verify the rendered page contains generated product purchase descriptors from the actual generated Razor output.
- [ ] Verify the browser loads Presentation core script via static web asset path.
- [ ] Keep live Commerce Node/COD regression as nightly/release or explicit full proof.
- [ ] Make PR proof fail if generated host cannot start, cannot load Presentation assets, or actual DOM does not match binder expectations.

### Tests

- [ ] Update the fast proof script to record host URL, startup logs, requests, and screenshots on failure.
- [ ] Add assertions for:
  - [ ] no direct Commerce Node browser calls.
  - [ ] same-origin BFF calls happen for preview and add-to-cart.
  - [ ] cart badge updates.
  - [ ] SKU/GTIN update when preview response changes.
  - [ ] command descriptor is required.
  - [ ] checkout/account shell routes render without direct Commerce transport.
- [ ] Update CI workflow so PR proof runs structure plus actual generated host fast proof.

### Acceptance Criteria

- [ ] Fast proof verifies actual generated host behavior, not hand-built HTML.
- [ ] Static web asset loading from Presentation is proven.
- [ ] Generated Razor DOM, view registration, and browser binders are tested together.
- [ ] PR gate can catch missing core script, wrong static asset path, bad descriptors, or runtime host startup failures.

## Phase F1.63 - Guardrail Precision And Legacy Alias Removal

Goal: remove stale compatibility selectors and make visual boundary validation precise enough to block raw business interpretation without blocking semantic display values.

### Implementation

- [ ] Remove Presentation legacy selector aliases:
  - [ ] `data-storefront-selection-preview`
  - [ ] `data-storefront-add-to-cart`
  - [ ] `data-storefront-generated-quantity`
  - [ ] `data-storefront-attribute-control`
  - [ ] `data-storefront-variant-select`
- [ ] Remove tests that assert compatibility alias comments or alias support.
- [ ] Add tests that assert legacy aliases are absent from Presentation script, V2, Starter, generated output, and StorefrontBuilder transforms.
- [ ] Replace broad visual JS forbidden tokens for `sku` and `gtin` with precise raw-data patterns.
- [ ] Keep semantic projection tokens allowed:
  - [ ] `selection.skuText`
  - [ ] `selection.gtinText`
  - [ ] `selection.stockText`
  - [ ] `selection.priceText`
- [ ] Add validator coverage for bracket/dynamic forms where practical:
  - [ ] `preview["sku"]`
  - [ ] `preview['gtin']`
  - [ ] `preview["stockQuantity"]`
  - [ ] `preview['canAddToCart']`
- [ ] Open a follow-up task for AST/ESLint validator if substring rules remain insufficient.

### Tests

- [ ] Update `StorefrontVisualConsumerBoundaryValidatorTests`.
- [ ] Update `StorefrontCommerceScriptRegressionTests`.
- [ ] Run Storefront V2 architecture/visual boundary tests.
- [ ] Run StorefrontBuilder static gate and composition validation.

### Acceptance Criteria

- [ ] Legacy selector aliases are gone.
- [ ] Visual consumers can render semantic event values without false positives.
- [ ] Visual consumers cannot read raw preview business fields.
- [ ] Validator covers the known bypass patterns used by simple bracket notation.

## Phase F1.64 - QA And Documentation Closure

Goal: close the work with browser proof, architecture docs, and QA checklist updates.

### Implementation

- [ ] Update `docs/architecture/03-runtime-boundaries.md` with the browser semantics boundary.
- [ ] Update `docs/architecture/10-v2-contract-ownership.md` to state selected product option state belongs to Presentation/storefront contracts, not V2 visual markup.
- [ ] Update StorefrontBuilder docs to say generated stores inherit functional descriptors from Starter and Presentation binders.
- [ ] Update `QA-StorefrontV2.todo.md` with product variant visual projection checks.
- [ ] Update `QA-StorefrontStarter.todo.md` with Starter functional purchase descriptor checks.
- [ ] Update `Storefront Playwright E2E Release.todo.md` with SKU/GTIN variant update and command descriptor negative case.
- [ ] Mark this todo complete only after all tests and docs are updated.

### Verification Commands

- [ ] `dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --filter StorefrontCommerceScriptRegressionTests`
- [ ] `dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --filter StorefrontVisualConsumerBoundaryValidatorTests`
- [ ] `dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --filter StorefrontBuilderQaRegenerationTests`
- [ ] `dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --filter StorefrontStarterFoundationBoundaryTests`
- [ ] `pwsh tools/BlazorShop.AI.StorefrontBuilder/scripts/validate/Test-StorefrontBuilderStaticGate.ps1 -ProjectRoot <generated-project>`
- [ ] `node tools/BlazorShop.AI.StorefrontBuilder/scripts/qa/run-fast-foundation-functional.mjs --project-root <generated-project>`
- [ ] Focused Playwright browser run against V2 product page proving option change updates price, stock, image, SKU, and GTIN.

### Acceptance Criteria

- [ ] V2, Starter, and generated storefronts are visual consumers for product purchase semantics.
- [ ] Presentation owns browser command dispatch and checkout form submission semantics.
- [ ] Fast generated proof launches an actual generated host.
- [ ] QA checklist covers the real browser failures this phase is intended to catch.
- [ ] Documentation reflects the final ownership model.

## Execution Order

1. [ ] F1.57 Product option selected-state contract.
2. [ ] F1.59 SKU/GTIN visual projection.
3. [ ] F1.60 Command descriptor enforcement and private binder surface.
4. [ ] F1.58 Checkout address form behavior cleanup.
5. [ ] F1.61 Starter functional reference.
6. [ ] F1.62 Actual generated host fast proof.
7. [ ] F1.63 Guardrail precision and legacy alias removal.
8. [ ] F1.64 QA and documentation closure.

This order intentionally fixes customer-visible product correctness before proof infrastructure. Starter and generator cleanup come after the canonical browser binder behavior is stable.

## Failure Modes Registry

| Failure mode | Cause | Prevention | Test/proof |
| --- | --- | --- | --- |
| Wrong initial variant is added to cart | V2 chooses first required option by DOM order | Explicit selected state from Presentation mapper | Mapper and V2 markup regression |
| SKU/GTIN stale after variant change | V2 ignores semantic `skuText`/`gtinText` | Update visual targets from event detail | Playwright variant-change test |
| Add-to-cart fires on malformed button | Submit selector ignores command descriptor | Require `data-storefront-command="cart.add-line"` | Negative browser test |
| Visual host calls command internals | `root.bindings` exposes command methods | Keep command methods private, expose events/init only | Static guardrail |
| Checkout posts wrong address fields | V2 disables/enables fields | Remove dead code or move binder to Presentation | Checkout form behavior test |
| Generated proof passes despite bad host | Test uses synthetic `pageHtml()` | Launch actual generated host | Fast proof update |
| Guardrail blocks valid visual rendering | Substring token `sku` catches `skuText` | Precise raw-field tokens | Validator tests |
| Legacy descriptors hide incomplete cutover | Presentation accepts old aliases | Remove alias selectors | Static tests |

## Decision Audit Trail

| # | Phase | Decision | Classification | Principle | Rationale | Rejected |
|---|-------|----------|----------------|-----------|-----------|----------|
| 1 | F1.57 | Use `ProductPurchaseOptionValueItem.IsSelected` as the primary selected-state contract. | Auto-decided | Keep behavior near data owner | Value-level selection maps directly to radio/select rendering and avoids V2 deriving defaults from order. | `SelectedValue` only on option, because each visual value still needs local selected state checks. |
| 2 | F1.58 | Delete V2 checkout address selector behavior if no active markup emits `data-storefront-address-select`. | Auto-decided | Remove dead compatibility | Current code search shows no active selector markup, so keeping behavior increases confusion and risk. | Keeping stale V2 code for a future saved-address feature. |
| 3 | F1.60 | Require `data-storefront-command="cart.add-line"` for product purchase submit handling. | Auto-decided | Explicit contract beats implicit selector | A submit marker alone should not execute commerce commands. | Continuing to treat `data-storefront-product-purchase-submit` as sufficient. |
| 4 | F1.61 | Make Starter functional by default instead of generator-patching behavior into it. | Auto-decided | Starter must be the reference implementation | Generated storefronts should inherit a real baseline contract, not depend on brittle string replacements. | Leaving Starter as disabled placeholder. |
| 5 | F1.62 | Fast proof must launch actual generated host while mocking same-origin BFF. | Auto-decided | Test the real integration boundary | Browser tests should catch asset, DOM, host, and binder failures together. | Synthetic HTML proof with manual script injection. |
| 6 | F1.63 | Tighten substring guardrails now and defer full AST validator if needed. | Auto-decided | Reduce risk incrementally | Precise token rules solve current false positives quickly; AST can follow if bypass risk remains. | Blocking this phase on a full JS parser integration. |

## GSTACK REVIEW REPORT

### CEO Review

The plan protects the MVP production path because the remaining failures are customer-visible or release-gate-visible: wrong variant selection, stale SKU/GTIN, malformed command execution, and false generated proof confidence. The scope is appropriately narrow because it does not reopen Commerce Node APIs, cart totals, checkout core, payment, or visual redesign.

### Design Review

The plan keeps visual ownership in V2/Starter/generated stores. Presentation supplies semantic state, while visual hosts still control layout, spacing, animation, gallery affordances, toasts, and copy. This preserves design flexibility for generated storefronts and future named storefronts.

### Engineering Review

The plan matches current project boundaries: `BlazorShop.Storefront.Presentation` owns browser binders and BFF semantics; `BlazorShop.Storefront.Components` owns contracts/headless state; V2 and Starter own visual markup. The highest-risk implementation is selected-state mapping because it must align product variants and variation templates; therefore it is first and must have mapper tests.

### DX Review

The Starter and generated-host proof phases improve developer experience directly. After these phases, a future storefront implementer can inspect Starter and understand the functional descriptor contract without reading generator patches or V2-specific visual code.

### Cross-phase Themes

- Browser tests must exercise real rendered hosts, not synthetic HTML fragments.
- Visual consumers can render semantic values but must not interpret raw commerce state.
- Compatibility aliases should be short-lived and removed once generated markup migrates.

NO UNRESOLVED DECISIONS

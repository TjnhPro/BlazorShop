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
- [x] `BlazorShop.Storefront.V2/wwwroot/js/storefrontCommerce.js` still owns saved-address/manual-address show-hide and disabled field behavior. 2026-07-29 F1.58 removed stale V2 address selector/disabling behavior.
- [x] No active Presentation checkout markup currently emits `data-storefront-address-select`, so the V2 checkout address selector code appears stale or dead. 2026-07-29 F1.58 reconfirmed with `rg`.
- [x] Presentation `storefront.application.js` builds semantic `skuText` and `gtinText` from product-selection preview. 2026-07-29 F1.59 verified by regression test.
- [x] V2 product page renders `data-storefront-selection-sku` and `data-storefront-selection-gtin`. 2026-07-29 F1.59 verified by markup regression.
- [x] V2 selection listener updates price, compare price, stock, image, and button state, but not SKU/GTIN targets. 2026-07-29 F1.59 now updates SKU/GTIN targets from semantic event values.
- [x] Presentation product purchase binder selects `[data-storefront-product-purchase-submit]` and legacy aliases without enforcing `data-storefront-command="cart.add-line"`. 2026-07-29 F1.60 submit handling now requires `data-storefront-command="cart.add-line"`.
- [x] Presentation script publicly exposes command-capable methods under `window.blazorShopStorefront.application` and `window.blazorShopStorefront.bindings`. 2026-07-29 F1.60 removed command-capable public exports.
- [x] `root.bindings.addToCart.addPurchaseLine` and `root.bindings.productSelection.previewPurchase` have no verified visual consumer and can re-enable command calls from visual hosts. 2026-07-29 F1.60 removed `root.bindings`.
- [x] Starter product page passes only `ProductName` into `ProductDetailShell`. 2026-07-29 F1.61 now passes `PurchasePanel` and `PurchaseActions`.
- [x] Starter `PurchasePanelPlaceholder` renders a permanently disabled button and does not emit product purchase descriptors. 2026-07-29 F1.61 Starter baseline now emits canonical purchase descriptors.
- [x] StorefrontBuilder still patches Starter by string replacement to add `PurchasePanel`, `PurchaseActions`, and product purchase descriptors. 2026-07-29 F1.61 removed behavior injection transforms; generator keeps visual class transforms only.
- [x] Fast foundation functional proof uses mocked `pageHtml()` and injects Presentation script manually instead of launching an actual generated host. 2026-07-29 F1.62 replaced the synthetic HTML proof with an actual generated host plus fake Commerce Node/server-side fixture responses.
- [x] Visual boundary validator still relies on broad substring tokens such as `sku` and `gtin`, causing false positives for presentation-ready `selection.skuText` and `selection.gtinText`. 2026-07-29 F1.59 narrowed browser business tokens to raw `preview.*` and bracket forms.
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
- [x] display presentation-ready event values such as `selection.priceText`, `selection.stockText`, `selection.skuText`, and `selection.gtinText`. 2026-07-29 F1.59 V2 consumes these semantic values in `applySelectionVisual(...)`.
- [ ] toggle CSS classes and visual visibility from semantic event values.
- [ ] manage gallery keyboard navigation and visual thumbnail state.
- [ ] focus fields, animate sections, and show toast copy supplied by the host or event descriptor.

Visual hosts must not:

- [x] choose the first required product option as a business default. 2026-07-29 F1.57 removed V2 first-value selection fallback.
- [x] infer selected variant or purchasability from DOM option order. 2026-07-29 F1.57 initial attributes now come from Presentation-selected value state.
- [x] decide which checkout form fields participate in form submission. 2026-07-29 F1.58 removed V2 field disabling/submission semantics.
- [x] read raw preview fields such as `preview.sku`, `preview.gtin`, `preview.stockQuantity`, or `preview.canAddToCart`. 2026-07-29 F1.59 V2 regression tests forbid raw preview reads.
- [x] call command-capable `application.*` or `bindings.*` methods from visual scripts. 2026-07-29 F1.60 visual validator forbids direct application/bindings command use.
- [ ] rely on legacy selectors after canonical descriptors are available.
- [x] patch behavior contracts into Starter through fragile string replacement. 2026-07-29 F1.61 moved purchase descriptors into Starter baseline.

## Non-goals

- [x] Do not change Commerce Node product-selection preview response shape in this phase unless a contract bug blocks selected-state mapping. 2026-07-29 F1.59 kept response shape unchanged; Presentation still normalizes raw preview fields.
- [ ] Do not redesign the V2 product page layout.
- [x] Do not add full saved-address checkout UX unless an active endpoint and markup already exist. 2026-07-29 F1.58 did not add saved-address UX; no active selector markup exists.
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

- [x] Confirm whether any active checkout markup emits `data-storefront-address-select`.
- [x] If no active markup emits it, delete the V2 address select/manual address functions and listeners.
- [x] If active saved-address markup is reintroduced before this phase lands, move the binder into Presentation `storefront.application.js` instead. N/A for F1.58 because no active saved-address selector markup exists.
- [x] For the Presentation binder path, define canonical descriptors:
  - [x] `data-storefront-checkout-address-mode` - deferred/N/A until saved-address mode is active.
  - [x] `data-storefront-address-select` - deferred/N/A until saved-address mode is active.
  - [x] `data-storefront-manual-address` - existing Presentation markup keeps manual address grouping.
  - [x] `data-storefront-manual-address-field` - existing Presentation markup keeps manual field markers.
- [x] Presentation must own field enabling/disabling and required behavior because disabled fields change POST payload semantics.
- [x] V2 may listen to `storefront:checkout:address-mode-changed` only for animation, focus, and CSS class updates. N/A for F1.58 because no saved-address event is active.
- [x] Update V2 visual script tests to forbid `data-storefront-address-select`, `manualAddressFieldSelector`, and field `.disabled` behavior when no active visual-only need exists.

### Tests

- [x] Add a test proving V2 script does not contain checkout form field disabling logic.
- [x] If Presentation binder is added, add a browser/unit test proving saved-address mode disables manual fields and manual mode enables them. N/A for F1.58 because the binder was not added.
- [x] Add a regression check that checkout POST form field names remain owned by Presentation components.

### Acceptance Criteria

- [x] V2 JavaScript no longer decides which checkout address inputs are submitted.
- [x] Dead checkout selector code is removed instead of left as compatibility.
- [x] Any future saved-address behavior has a Presentation-owned binder and semantic event.

2026-07-29 F1.58 evidence:
- `node --check BlazorShop.PresentationV2/BlazorShop.Storefront.V2/wwwroot/js/storefrontCommerce.js` passed.
- `dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --filter "FullyQualifiedName~StorefrontCommerceScriptRegressionTests|FullyQualifiedName~StorefrontBrandingMarkupTests" --no-restore` passed 18/18; existing MessagePack and Browserslist warnings remain.
- `rg -n "data-storefront-address-select|manualAddressFieldSelector|syncManualAddressFields|initCheckoutAddressSelection|field\\.disabled|addressSelectSelector" -S BlazorShop.PresentationV2/BlazorShop.Storefront.V2 BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation` returned no production matches.

## Phase F1.59 - Product Selection Visual Projection Completeness

Goal: make V2 update all visible product selection targets from presentation-ready event values.

### Implementation

- [x] Keep raw preview interpretation in Presentation `normalizePreview(...)`.
- [x] Ensure semantic selection result includes:
  - [x] `priceText`
  - [x] `comparePriceText`
  - [x] `stockText`
  - [x] `skuText`
  - [x] `gtinText`
  - [x] `mainImageUrl`
  - [x] `message`
  - [x] `ready`
  - [x] `valid`
- [x] Update V2 `applySelectionVisual(...)` to find:
  - [x] `data-storefront-selection-sku`
  - [x] `data-storefront-selection-gtin`
- [x] Set SKU/GTIN text from `selection.skuText` and `selection.gtinText`.
- [x] Hide SKU/GTIN visual targets when the presentation-ready value is empty.
- [x] Do not read `preview.sku`, `preview.gtin`, raw `sku`, raw `gtin`, or SKU/GTIN values from response payload in V2.
- [x] Keep gallery image switching from `selection.mainImageUrl`.

### Tests

- [x] Update `StorefrontCommerceScriptRegressionTests` to require V2 updates SKU and GTIN from `selection.skuText` / `selection.gtinText`.
- [x] Update visual boundary validator so `selection.skuText` and `selection.gtinText` are allowed.
- [x] Keep raw business tokens forbidden with more precise tokens:
  - [x] `preview.sku`
  - [x] `preview.gtin`
  - [x] `preview.stockQuantity`
  - [x] `preview.canAddToCart`
- [x] Add a Playwright/browser regression where changing variant updates price, stock, image, SKU, and GTIN together. 2026-07-29 `node scripts/qa/storefront-browser-semantics-visual-proof.js` passed.

### Acceptance Criteria

- [x] SKU/GTIN no longer stay stale after variant selection changes.
- [x] V2 consumes presentation-ready selection values only.
- [x] Guardrail does not block `skuText` / `gtinText` false positives.

## Phase F1.60 - Command Descriptor Enforcement And Private Binder Surface

Goal: make Presentation command dispatch explicit and stop exposing command-capable internals to visual hosts.

### Implementation

- [x] Change product purchase submit selector to require the canonical command descriptor:
  - [x] `[data-storefront-command="cart.add-line"][data-storefront-product-purchase-submit]`
- [x] Alternatively implement a small dispatcher that switches on `element.dataset.storefrontCommand`.
- [x] Unknown command values must be ignored and publish a contract error event rather than executing add-to-cart.
- [x] Remove legacy submit alias support from command execution.
- [x] Make `previewPurchase`, `addPurchaseLine`, payload builders, and request helpers private inside the IIFE.
- [x] Replace public `root.bindings` command surface with:
  - [x] `root.events`
  - [x] optional `root.initialize()`
- [x] Add idempotency guard:
  - [x] repeated initialize calls do not register document listeners multiple times.
- [x] Decide whether `root.application` remains public:
  - [x] If no required external consumer exists, remove it.
  - [x] If it must remain for compatibility, expose read-only event names only and mark command methods unavailable to visual consumers.
- [x] Update all tests that currently assert `root.bindings` exists.

### Tests

- [x] Add a test proving a button with `data-storefront-product-purchase-submit` but no `data-storefront-command="cart.add-line"` does not call `/api/cart/lines`.
- [x] Add a test proving wrong command values are ignored or produce a contract error event.
- [x] Add a test proving double initialization does not double-submit commands.
- [x] Update visual boundary validator to forbid:
  - [x] `blazorShopStorefront.bindings.addToCart`
  - [x] `blazorShopStorefront.bindings.productSelection`
  - [x] `addPurchaseLine(`
  - [x] `previewPurchase(`
  - [x] direct command-capable `root.application` use by visual consumers.

### Acceptance Criteria

- [x] A descriptor typo cannot silently execute add-to-cart.
- [x] Visual scripts cannot call Presentation command internals.
- [x] Presentation initialization is idempotent.
- [x] Existing V2 add-to-cart behavior still works through canonical descriptors.

2026-07-29 F1.60 evidence:
- `node --check BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/wwwroot/js/storefront.application.js` passed.
- `node --check scripts/qa/storefront-application-js-split-proof.js` passed.
- `dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --filter "FullyQualifiedName~StorefrontCommerceScriptRegressionTests|FullyQualifiedName~StorefrontVisualConsumerBoundaryValidatorTests|FullyQualifiedName~StorefrontBrandingMarkupTests|FullyQualifiedName~SecurityPrivacyPhase1CsrfTests|FullyQualifiedName~SecurityPrivacyPhase3ConsentTests" --no-restore` passed 29/29; existing MessagePack and Browserslist warnings remain.
- `node scripts/qa/storefront-application-js-split-proof.js` passed and covered missing command, wrong command, canonical command, and repeated initialize calls.

## Phase F1.61 - Starter Functional Reference Cutover

Goal: make Starter the canonical minimal functional reference instead of relying on generator string replacement to add behavior contracts.

### Implementation

- [x] Update Starter `Pages/Hybrid/Catalog/ProductPage.razor` to pass:
  - [x] `PurchasePanel="@Context.PurchasePanel"`
  - [x] `PurchaseActions="@Context.PurchaseActions"`
- [x] Update Starter `ProductDetailShell.razor` to accept and forward `ProductPurchasePanelModel` and `ProductPurchaseActionDescriptor`.
- [x] Update Starter `PurchasePanelPlaceholder.razor` to emit canonical product purchase descriptors:
  - [x] `data-storefront-product-purchase`
  - [x] `data-selection-preview-route`
  - [x] `data-product-id`
  - [x] `data-product-name`
  - [x] `data-resolved-variant-id`
  - [x] `data-currency-code`
  - [x] `data-storefront-command="cart.add-line"`
  - [x] `data-storefront-product-purchase-submit`
  - [x] `data-storefront-purchase-quantity`
  - [x] `data-storefront-purchase-feedback`
- [x] Keep Starter UI visually neutral and minimal.
- [x] Remove generator transforms that patch missing `PurchasePanel` and `PurchaseActions` into Starter.
- [x] Keep generator transforms limited to layout, classes, copy, placement, and generated visual CSS.
- [x] Update `starter-generation.contract.yaml` to state these descriptors are part of Starter baseline, not generated behavior injection.

### Tests

- [x] Update Starter foundation tests to assert product page passes purchase contracts.
- [x] Update StorefrontBuilder regeneration tests to assert generator no longer string-replaces missing purchase contracts.
- [x] Update composition validation to require descriptors in Starter baseline and generated output.
- [x] Build Starter and generated proof project.

### Acceptance Criteria

- [x] Starter can demonstrate product purchase descriptor pattern without generator mutation.
- [x] Generated storefront behavior no longer depends on exact Starter source text for functional contract insertion.
- [x] Generator remains visual/composition-only for product purchase.

2026-07-29 F1.61 evidence:
- `node --check tools/BlazorShop.AI.StorefrontBuilder/scripts/generate/apply-composition.mjs` passed.
- `dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/BlazorShop.Storefront.Starter.csproj --no-restore` passed with 0 warnings/errors.
- `dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --filter "FullyQualifiedName~StorefrontStarterFoundationBoundaryTests|FullyQualifiedName~StorefrontBuilderFoundationTests|FullyQualifiedName~StorefrontBuilderQaRegenerationTests|FullyQualifiedName~StorefrontBuilderVisualGenerationTests|FullyQualifiedName~StorefrontVisualConsumerBoundaryValidatorTests" --no-restore` passed 64/64 on rerun; the first attempt timed out before result.
- `.\scripts\qa\run-storefront-builder-generated-proof.ps1 -ProofLevel Structure` passed after stopping stale `testhost` process `9944` from the prior timed-out test run; generated proof build, static validation, isolation gate, and visual boundary validator all passed.

## Phase F1.62 - Actual Generated Host Fast Proof

Goal: make the required fast proof run a real generated storefront host with Presentation static web assets and Runtime registration.

### Implementation

- [x] Replace `run-fast-foundation-functional.mjs` document fulfillment with an actual generated host launch.
- [x] Use deterministic fake same-origin BFF responses for `/api/cart`, `/api/cart/lines`, `/api/product-selection-preview`, `/api/consent/*`, and required checkout/account endpoints.
- [x] Let the generated host render Razor markup and load static web assets normally.
- [x] Do not manually inject `storefront.application.js` with `page.addScriptTag`.
- [x] Verify the rendered page contains generated product purchase descriptors from the actual generated Razor output.
- [x] Verify the browser loads Presentation core script via static web asset path.
- [x] Keep live Commerce Node/COD regression as nightly/release or explicit full proof.
- [x] Make PR proof fail if generated host cannot start, cannot load Presentation assets, or actual DOM does not match binder expectations.

### Tests

- [x] Update the fast proof script to record host URL, startup logs, requests, and screenshots on failure.
- [x] Add assertions for:
  - [x] no direct Commerce Node browser calls.
  - [x] same-origin BFF calls happen for preview and add-to-cart.
  - [x] cart badge updates.
  - [x] SKU/GTIN update when preview response changes.
  - [x] command descriptor is required.
  - [x] checkout/account shell routes render without direct Commerce transport.
- [x] Update CI workflow so PR proof runs structure plus actual generated host fast proof. Existing workflow already runs `-ProofLevel Structure` and `-ProofLevel FoundationFunctionalFast`; F1.62 updated the fast proof implementation behind that gate.

### Acceptance Criteria

- [x] Fast proof verifies actual generated host behavior, not hand-built HTML.
- [x] Static web asset loading from Presentation is proven.
- [x] Generated Razor DOM, view registration, and browser binders are tested together.
- [x] PR gate can catch missing core script, wrong static asset path, bad descriptors, or runtime host startup failures.

2026-07-29 F1.62 evidence:
- `node --check tools/BlazorShop.AI.StorefrontBuilder/scripts/qa/run-fast-foundation-functional.mjs` passed.
- `dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/BlazorShop.Storefront.Starter.csproj --no-restore` passed with 0 warnings/errors.
- `dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --filter "FullyQualifiedName~StorefrontBuilderQaRegenerationTests|FullyQualifiedName~StorefrontStarterFoundationBoundaryTests" --no-restore` passed 38/38; existing MessagePack vulnerability warnings and Browserslist notice remain.
- `node tools/BlazorShop.AI.StorefrontBuilder/scripts/qa/run-fast-foundation-functional.mjs --project-root artifacts/storefront-builder/generated/BlazorShop.Storefront.GeneratedProof` passed.
- `.\scripts\qa\run-storefront-builder-generated-proof.ps1 -ProofLevel FoundationFunctionalFast` passed after regenerating the proof storefront, restoring/building it, running static validation, isolation gate, visual boundary validator, and the actual generated host browser proof.

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
6. [x] F1.62 Actual generated host fast proof.
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

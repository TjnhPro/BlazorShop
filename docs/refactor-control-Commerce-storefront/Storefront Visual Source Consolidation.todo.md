# Storefront Visual Source Consolidation

Status: in-progress
Scope: `BlazorShop.Storefront.V2` and `BlazorShop.Storefront.V2.WASM`
Intent: make visual ownership readable and enforceable without changing ecommerce behavior.

## Goal

Consolidate the remaining Storefront V2 visual ownership leaks so a maintainer can tell where each kind of behavior belongs:

- Razor owns structure, semantic markup, accessibility attributes, explicit inline SVG icons, and data hooks.
- CSS owns colors, typography, spacing polish, animation, transition, and visual state selectors.
- JavaScript owns browser events, timers, semantic state transitions, text updates from event payloads, button state, gallery selection, and local/session storage coordination.

This phase is intentionally narrow. It must not change backend APIs, cart/checkout/order contracts, pricing, sellability, media contracts, Storefront Presentation routing, Runtime, Client generation, Starter, or StorefrontBuilder.

## Current Evidence

- `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/wwwroot/js/storefrontCommerce.js`
  - `resolveToastTheme(level)` owns toast color values.
  - `resolveToastIcon(level)` owns SVG strings.
  - `showToast` writes `backgroundColor`, `color`, `opacity`, `transform`, and `innerHTML`.
  - `setFeedback` toggles Tailwind color classes `text-emerald-700` and `text-red-700`.
  - Existing gallery fallback writes a transparent `data:image/svg+xml` into `image.src`; this is a known broken-image fallback and is not the same concern as toast/icon ownership.
- `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Components/Layout/MainLayout.razor`
  - Toast template exists with semantic hooks, but the toast root still has inline opacity/transform/transition style.
  - Toast icon slot is empty and currently filled by JavaScript.
- `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Components/Layout/StorefrontHeader.razor`
  - Uses Font Awesome classes for search and check icons:
    - `fa-solid fa-magnifying-glass`
    - `fa-solid fa-check`
- `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/Hybrid/Catalog/SearchPage.razor`
  - Passes `SubmitIconCssClass="fa-solid fa-magnifying-glass"`.
- `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Components/Catalog/CatalogFilterPanel.razor`
  - Exposes `SubmitIconCssClass` and renders `<i class="@SubmitIconCssClass">`.
- `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Components/Product/StorefrontProductPurchasePanel.razor`
  - Purchase feedback starts with `text-emerald-700`.
  - `ColorSwatchStyle(value)` uses dynamic inline `background-color` from product data. This is allowed because it is product data rendering, not theme ownership.
- `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/package.json` and `BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM/package.json`
  - No Font Awesome package dependency exists.
- Generated CSS files such as `wwwroot/css/site.css` and `wwwroot/css/wasm-site.css` contain many color utilities by design. Guardrail tests must not treat generated CSS output as source ownership evidence.

## Non-Goals

- [ ] Do not add Font Awesome, lucide, or another icon framework.
- [ ] Do not redesign the component library.
- [ ] Do not rewrite Tailwind or the CSS build system.
- [ ] Do not move CSS asset ownership or root asset load order.
- [ ] Do not change Storefront Presentation slots, routes, BFF endpoints, or generated-client contracts.
- [ ] Do not refactor product gallery behavior beyond preserving existing fallback behavior.
- [ ] Do not ban all inline Razor styles. Dynamic product-data styles such as color swatches remain valid.
- [ ] Do not touch Control Plane Font Awesome usage. Control Plane has its own asset rule and is outside this Storefront V2 phase.

## Ownership Rules To Enforce

Allowed in Storefront V2 JavaScript:

- [ ] Set text from event payloads with `textContent`.
- [ ] Toggle structural/semantic classes such as `hidden`.
- [ ] Set `dataset` state such as `data-level`, `data-state`, `data-selected`, and `data-dismissed`.
- [ ] Set `disabled`, `aria-*`, `hidden`, `src`, and `alt` when those values are runtime content or behavior state.
- [ ] Use timers, `requestAnimationFrame`, `sessionStorage`, and event listeners.
- [ ] Keep the known gallery transparent fallback `data:image/svg+xml` only as a documented allowlist entry.

Forbidden in Storefront V2 JavaScript after this phase:

- [ ] Toast theme color maps.
- [ ] SVG icon strings.
- [ ] `innerHTML` for icon rendering.
- [ ] Inline visual styling for toast color, opacity, transform, or transition.
- [ ] Tailwind color utility selection such as `text-emerald-700` and `text-red-700`.

Allowed in Razor:

- [ ] Explicit inline SVG icons with `aria-hidden="true"`, `viewBox`, and currentColor.
- [ ] Semantic hooks such as `data-storefront-toast`, `data-storefront-toast-icon`, `data-level`, and `data-state`.
- [ ] Product-data inline style only where the value comes from trusted product display data, for example a color swatch.

CSS must own:

- [ ] Toast background/accent/icon colors by `[data-level]`.
- [ ] Toast enter/open/closing visual states by `[data-state]`.
- [ ] Purchase feedback success/error colors by data attributes.
- [ ] Icon sizing and display polish where not already obvious from existing component classes.

## Phase 0 - Baseline Inventory And Test Target

- [x] Run a focused source search before editing:

```powershell
rg -n "resolveToastTheme|resolveToastIcon|style\.backgroundColor|style\.color|style\.opacity|style\.transform|innerHTML|text-emerald-700|text-red-700|fa-solid|fa-magnifying-glass|fa-check|SubmitIconCssClass|data:image/svg\+xml" `
  BlazorShop.PresentationV2/BlazorShop.Storefront.V2 `
  BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM `
  --glob "!node_modules/**" `
  --glob "!bin/**" `
  --glob "!obj/**"
```

- [x] Record the active source files that are allowed to change:
  - `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/wwwroot/js/storefrontCommerce.js`
  - `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/wwwroot/css/storefront.css`
  - `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Components/Layout/MainLayout.razor`
  - `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Components/Layout/StorefrontHeader.razor`
  - `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Components/Catalog/CatalogFilterPanel.razor`
  - `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/Hybrid/Catalog/SearchPage.razor`
  - `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Components/Product/StorefrontProductPurchasePanel.razor`
  - focused tests under `BlazorShop.Tests.V2`
  - `docs/refactor-control-Commerce-storefront/QA-StorefrontV2.todo.md`
- [x] Confirm `CategoryPage` and other catalog callers do not use `SubmitIconCssClass`.
- [x] Confirm `BlazorShop.Storefront.V2.WASM` has no Font Awesome source dependency. Ignore `wwwroot/css/wasm-site.css` color utility output.
- [x] Do not proceed if a hidden V2 consumer requires `SubmitIconCssClass`; convert that consumer in the same phase instead of keeping the old string API.

Acceptance:

- [x] Inventory is based on actual `rg` output.
- [x] Generated CSS, `node_modules`, `bin`, and `obj` are explicitly excluded from guardrail evidence.
- [x] Product color swatch inline style and gallery transparent image fallback are documented as allowlisted cases.

Implementation notes:

- 2026-08-09: baseline `rg` found toast visual ownership in `storefrontCommerce.js` (`resolveToastTheme`, `resolveToastIcon`, inline toast style writes, `innerHTML`), feedback Tailwind color toggling in `setFeedback`, Font Awesome class usage in `StorefrontHeader.razor`, and `SubmitIconCssClass` only in `CatalogFilterPanel.razor` plus `SearchPage.razor`.
- 2026-08-09: `CategoryPage.razor` uses `CatalogFilterPanel` without `SubmitIconCssClass`; no other active V2 catalog caller requires the old string icon API.
- 2026-08-09: `BlazorShop.Storefront.V2.WASM` has no Font Awesome source dependency; generated `wwwroot/css/wasm-site.css` color utility matches were excluded from guardrail evidence.
- 2026-08-09: allowlisted cases remain product-data swatch inline styles and the transparent `data:image/svg+xml` product image fallback in gallery handling.

## Phase 1 - Move Toast Markup Ownership Into Razor

Update `MainLayout.razor`.

- [x] Add a stable class to the toast root, for example `bs-storefront-toast`.
- [x] Remove inline `style="opacity: 0; transform: ...; transition: ..."` from the toast root.
- [x] Add default semantic state on the template root:
  - `data-level="info"`
  - `data-state="entering"`
- [x] Keep existing toast data hooks:
  - `data-storefront-toast`
  - `data-storefront-toast-accent`
  - `data-storefront-toast-heading`
  - `data-storefront-toast-message`
  - `data-storefront-toast-close`
- [x] Replace the empty toast accent region with explicit inline SVG icon elements owned by Razor:
  - `data-storefront-toast-icon="info"`
  - `data-storefront-toast-icon="success"`
  - `data-storefront-toast-icon="warning"`
  - `data-storefront-toast-icon="error"`
- [x] Each icon must use `aria-hidden="true"` and currentColor.
- [x] Do not create icons through `MarkupString`.
- [x] Keep the close button SVG as explicit Razor markup.

Acceptance:

- [x] Toast template contains all four icon variants.
- [x] Toast template contains no inline opacity/transform/transition style.
- [x] Toast template remains accessible through `aria-live="polite"` and the close button keeps an accessible label.

Implementation notes:

- 2026-08-09: `MainLayout.razor` now renders `bs-storefront-toast` with default `data-level="info"` and `data-state="entering"`.
- 2026-08-09: Razor owns explicit currentColor SVG variants for `info`, `success`, `warning`, and `error`; no `MarkupString` or JS-provided icon markup is used.
- 2026-08-09: toast root no longer contains inline opacity, transform, or transition style; existing `aria-live="polite"` region and dismiss button label remain unchanged.

## Phase 2 - Move Toast Visual State Into CSS

Update `wwwroot/css/storefront.css`, not generated `site.css` or `wasm-site.css`.

- [x] Define toast base styling for `.bs-storefront-toast`.
- [x] Define toast visual states:
  - `[data-state="entering"]`
  - `[data-state="open"]`
  - `[data-state="closing"]`
- [x] Move transition, opacity, and transform into CSS.
- [x] Define toast level colors through selectors:
  - `.bs-storefront-toast[data-level="info"]`
  - `.bs-storefront-toast[data-level="success"]`
  - `.bs-storefront-toast[data-level="warning"]`
  - `.bs-storefront-toast[data-level="error"]`
- [x] Use CSS custom properties for toast background, accent background, and accent color if that matches existing `storefront.css` style.
- [x] Hide inactive icon variants through CSS:
  - default hidden for `[data-storefront-toast-icon]`
  - display only the matching icon for the current `[data-level]`
- [x] Keep the existing V2 visual identity; do not restyle the whole header/shell.

Acceptance:

- [x] Toast looks the same or materially equivalent in info/success/warning/error states.
- [x] CSS owns the color and animation values.
- [x] No root asset order changes are needed.

Implementation notes:

- 2026-08-09: `storefront.css` now defines `.bs-storefront-toast` base animation, `[data-state]` enter/open/closing states, and `[data-level]` info/success/warning/error color custom properties.
- 2026-08-09: toast accent colors and visible icon variant are selected by CSS selectors; generated CSS and root asset order were not changed.

## Phase 3 - Make Toast JavaScript Semantic Only

Update `storefrontCommerce.js`.

- [x] Delete `resolveToastTheme(level)`.
- [x] Delete `resolveToastIcon(level)`.
- [x] Add a small semantic normalizer such as `normalizeToastLevel(level)` that returns one of:
  - `info`
  - `success`
  - `warning`
  - `error`
- [x] In `showToast`, set only semantic state:
  - `toast.dataset.level = normalizeToastLevel(level)`
  - `toast.dataset.state = "entering"` before append if needed
  - `toast.dataset.state = "open"` inside `requestAnimationFrame`
  - `toast.dataset.state = "closing"` during dismiss
  - keep `toast.dataset.dismissed = "true"`
- [x] Keep heading and message assignment via `textContent`.
- [x] Keep close-button event listener and duration behavior.
- [x] Keep pending toast session storage behavior.
- [x] Do not write `backgroundColor`, `color`, `opacity`, `transform`, or transition styles.
- [x] Do not write SVG strings or `innerHTML`.

Acceptance:

- [x] Toast still displays from queued session toast and direct `showToast`.
- [x] Dismiss works by close button and timeout.
- [x] JS owns no toast visual values beyond semantic `data-level` and `data-state`.

Implementation notes:

- 2026-08-09: `storefrontCommerce.js` now uses `normalizeToastLevel` and sets only `toast.dataset.level`, `toast.dataset.state`, `toast.dataset.dismissed`, text, event listeners, and timers for toast behavior.
- 2026-08-09: source scan found no remaining `resolveToastTheme`, `resolveToastIcon`, toast visual style writes, SVG string injection, or `innerHTML` in the toast path.

## Phase 4 - Move Purchase Feedback Color State Out Of JavaScript

Update `StorefrontProductPurchasePanel.razor`, `storefrontCommerce.js`, and `storefront.css`.

- [x] Change the purchase feedback element from hardcoded success text color to a semantic data state:
  - keep `data-storefront-purchase-feedback`
  - add `data-level="success"` or an equivalent semantic attribute for initial ready state
  - remove `text-emerald-700` from the feedback element's base class
- [x] In `setFeedback`, set:
  - `feedbackElement.textContent = message || ""`
  - `feedbackElement.dataset.level = isError ? "error" : "success"`
- [x] If the message is empty, either keep the last level or reset to a neutral state, but document the chosen behavior in the test name.
- [x] Remove JS class toggling for:
  - `text-emerald-700`
  - `text-red-700`
- [x] Add CSS selectors for feedback:
  - `[data-storefront-purchase-feedback][data-level="success"]`
  - `[data-storefront-purchase-feedback][data-level="error"]`
  - include `[data-storefront-selection-message]` only if the same JS path can target it in current source.
- [x] Do not alter product stock badge colors in `V2ProductPageView.razor`; those are static Razor display classes, not JS ownership leakage.
- [x] Do not alter `ColorSwatchStyle`; dynamic product swatch colors remain allowed.

Acceptance:

- [x] Product selection ready/unready feedback still changes text and visual color.
- [x] Add-to-cart success and failure feedback still appears.
- [x] JS no longer imports visual Tailwind color utilities.

Implementation notes:

- 2026-08-09: purchase feedback root keeps `data-storefront-purchase-feedback`, starts at `data-level="success"`, and no longer carries the hardcoded `text-emerald-700` feedback class.
- 2026-08-09: `setFeedback` now assigns `textContent` and `dataset.level` only. Empty messages still set the semantic level from the current success/error result so the next visible message has deterministic styling.
- 2026-08-09: `storefront.css` owns success/error colors for `[data-storefront-purchase-feedback]` and `[data-storefront-selection-message]` because the same JS path can target both selectors.
- 2026-08-09: static product badges and `ColorSwatchStyle` were intentionally unchanged; source scan shows remaining `text-emerald-700` only in static Razor display classes outside JS feedback ownership.

## Phase 5 - Remove Font Awesome From Storefront Header

Update `StorefrontHeader.razor`.

- [x] Replace both desktop and mobile search `<i class="fa-solid fa-magnifying-glass">` elements with explicit inline SVG markup.
- [x] Replace both currency submit `<i class="fa-solid fa-check">` elements with explicit inline SVG markup.
- [x] Use the same icon conventions already present in the header:
  - `aria-hidden="true"`
  - `viewBox="0 0 24 24"`
  - `fill="none"`
  - `stroke="currentColor"`
  - `stroke-width="2"`
- [x] Keep visible labels and accessible labels unchanged.
- [x] Keep cart/menu/account markup unchanged unless required by formatting.

Acceptance:

- [x] No `fa-solid`, `fa-magnifying-glass`, or `fa-check` remains in `StorefrontHeader.razor`.
- [x] Desktop and mobile header still show search/currency actions.
- [x] No Font Awesome CSS, script, font, or CDN is added.

Implementation notes:

- 2026-08-09: desktop and mobile search buttons now render explicit currentColor search SVG markup.
- 2026-08-09: desktop and mobile currency preference submit content now renders explicit currentColor check SVG markup.
- 2026-08-09: no Font Awesome package, CSS, script, font, or CDN dependency was added; `fa-*` source scan for the header/package files returned no forbidden matches.

## Phase 6 - Replace Catalog Filter Icon Class API

Update `CatalogFilterPanel.razor` and `SearchPage.razor`.

- [x] Replace `SubmitIconCssClass` with a render-slot API:

```csharp
[Parameter]
public RenderFragment? SubmitIcon { get; set; }
```

- [x] Render `@SubmitIcon` before `@SubmitLabel` when present.
- [x] Remove the `<i class="@SubmitIconCssClass">` branch.
- [x] Update `SearchPage.razor` to pass explicit inline search SVG through the `SubmitIcon` slot.
- [x] Keep search form query semantics unchanged:
  - `q`
  - `category`
  - `minPrice`
  - `maxPrice`
  - `sortBy`
  - `pageSize`
  - `inStock`
- [x] Confirm `CategoryPage` does not require an icon slot update.
- [x] Do not introduce a global icon component in this phase.

Acceptance:

- [x] `SubmitIconCssClass` no longer exists.
- [x] Search page filter button still renders a search icon.
- [x] Search/category filter submit behavior is unchanged.

Implementation notes:

- 2026-08-09: `CatalogFilterPanel.razor` now exposes `RenderFragment? SubmitIcon` and renders it before `SubmitLabel`; the old `<i class="@SubmitIconCssClass">` branch was removed.
- 2026-08-09: `SearchPage.razor` passes an explicit inline search SVG through `<SubmitIcon>` while keeping the existing GET fields (`q`, `category`, `minPrice`, `maxPrice`, `sortBy`, `pageSize`, and `inStock`) unchanged.
- 2026-08-09: `CategoryPage.razor` still calls `CatalogFilterPanel` without an icon slot and needs no update; no global icon component was introduced.

## Phase 7 - Add Source Ownership Guardrail Tests

Add or extend tests under `BlazorShop.Tests.V2`. Suggested new file:

`BlazorShop.Tests.V2/PresentationV2/Storefront/StorefrontVisualSourceOwnershipTests.cs`

Guardrail design:

- [x] Use a curated source file list instead of scanning all generated CSS output.
- [x] Exclude:
  - `node_modules`
  - `bin`
  - `obj`
  - generated CSS files `wwwroot/css/site.css` and `wwwroot/css/wasm-site.css`
  - docs
  - fixtures
- [x] Assert `storefrontCommerce.js` does not contain:
  - `resolveToastTheme`
  - `resolveToastIcon`
  - `style.backgroundColor`
  - `style.color`
  - `style.opacity`
  - `style.transform`
  - `innerHTML`
  - `text-emerald-700`
  - `text-red-700`
- [x] If `data:image/svg+xml` remains in `storefrontCommerce.js`, assert it appears only in the known gallery fallback block and not in toast/icon code.
- [x] Assert V2 active Razor sources do not contain Font Awesome tokens:
  - `fa-solid`
  - `fa-regular`
  - `fa-brands`
  - `fa-magnifying-glass`
  - `fa-check`
- [x] Assert `SubmitIconCssClass` no longer exists in active V2 source.
- [x] Assert `MainLayout.razor` contains:
  - `data-storefront-toast-icon="info"`
  - `data-storefront-toast-icon="success"`
  - `data-storefront-toast-icon="warning"`
  - `data-storefront-toast-icon="error"`
  - no inline toast `style=` visual state
- [x] Assert `storefront.css` contains toast level/state selectors and purchase feedback selectors.

Acceptance:

- [x] Tests fail if JavaScript reclaims toast visual color/icon/animation ownership.
- [x] Tests fail if Storefront V2 reintroduces Font Awesome class-based icon usage.
- [x] Tests do not fail because Tailwind generated CSS contains expected utility classes.

Implementation notes:

- 2026-08-09: added `StorefrontVisualSourceOwnershipTests` with curated active V2 source files and explicit generated CSS/build-output/docs/fixtures exclusions.
- 2026-08-09: guardrails cover toast JS visual ownership, known gallery `data:image/svg+xml` allowlist, Font Awesome tokens, removed `SubmitIconCssClass`, Razor toast icon markup, and `storefront.css` visual selectors.
- 2026-08-09: `dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~StorefrontVisualSourceOwnershipTests"` passed 7/7. Existing warnings: MessagePack NU1902/NU1903 and Browserslist.

## Phase 8 - Preserve Existing Regression Tests

Run or update focused tests that already cover adjacent behavior.

- [x] Locate current tests with:

```powershell
rg -n "StorefrontCommerce|CatalogFilterPanel|SearchPage|LayoutAsset|toast|purchase-feedback|Font Awesome|fa-solid|SubmitIconCssClass" BlazorShop.Tests.V2
```

- [x] Keep existing `LayoutAssetFoundationTests` expectations for asset ownership/order unless the test name proves an update is required.
- [x] If any existing script regression test expects inline toast style or icon strings, update it to expect semantic `data-level`/`data-state` behavior.
- [x] If search page snapshot/string tests exist, update them for the `SubmitIcon` slot and explicit SVG.
- [x] Do not weaken tests by replacing exact ownership checks with broad smoke assertions.

Acceptance:

- [x] Existing V2 script, layout, and search tests still prove behavior, not just compilation.
- [x] New tests are focused on ownership and do not duplicate browser QA.

Implementation notes:

- 2026-08-09: located related tests with the requested `rg` command. No existing test expected old inline toast style or JS icon strings.
- 2026-08-09: strengthened `LayoutAssetFoundationTests.StorefrontCategoryAndSearchPages_UseCatalogFilterPanelWithoutRouteChanges` to assert `SearchPage.razor` uses `<SubmitIcon>` and no longer uses `SubmitIconCssClass`.
- 2026-08-09: `dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~LayoutAssetFoundationTests|FullyQualifiedName~StorefrontSearchPageRegressionTests|FullyQualifiedName~StorefrontCommerceScriptRegressionTests"` passed 26/26. Existing warnings: MessagePack NU1902/NU1903 and Browserslist.

## Phase 9 - Focused Build And Test Gate

Run focused verification before browser QA.

- [x] Build Storefront V2:

```powershell
dotnet build BlazorShop.PresentationV2\BlazorShop.Storefront.V2\BlazorShop.Storefront.V2.csproj --no-restore
```

- [x] Build Storefront V2 WASM:

```powershell
dotnet build BlazorShop.PresentationV2\BlazorShop.Storefront.V2.WASM\BlazorShop.Storefront.V2.WASM.csproj --no-restore
```

- [x] Run focused tests:

```powershell
dotnet test BlazorShop.Tests.V2\BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~StorefrontVisualSourceOwnershipTests|FullyQualifiedName~StorefrontCommerceScriptRegressionTests|FullyQualifiedName~LayoutAssetFoundationTests|FullyQualifiedName~StorefrontSearch"
```

- [x] If test names differ, use the closest exact V2 Storefront test filters found by `rg`, and record the executed filter in the implementation notes.
- [x] Do not require Tailwind rebuild unless `input.css`, Tailwind config, or generated CSS sources are changed. This plan expects authored CSS changes in `storefront.css`.

Acceptance:

- [x] Both projects build.
- [x] Focused tests pass.
- [x] No unrelated test failures are hidden; if a failure is pre-existing, document the exact failing test and reason.

Implementation notes:

- 2026-08-09: initial parallel Phase 9 run produced a transient `CS2012` file lock for `BlazorShop.Storefront.Presentation.dll` while build/test processes overlapped; `Storefront.V2.WASM` build and focused tests still passed in that run.
- 2026-08-09: reran `dotnet build BlazorShop.PresentationV2\BlazorShop.Storefront.V2\BlazorShop.Storefront.V2.csproj --no-restore` separately; it passed with 0 warnings and 0 errors.
- 2026-08-09: `dotnet build BlazorShop.PresentationV2\BlazorShop.Storefront.V2.WASM\BlazorShop.Storefront.V2.WASM.csproj --no-restore` passed with 0 warnings and 0 errors.
- 2026-08-09: `dotnet test BlazorShop.Tests.V2\BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~StorefrontVisualSourceOwnershipTests|FullyQualifiedName~StorefrontCommerceScriptRegressionTests|FullyQualifiedName~LayoutAssetFoundationTests|FullyQualifiedName~StorefrontSearch"` passed 33/33. Existing warnings: MessagePack NU1902/NU1903 and Browserslist.
- 2026-08-09: no Tailwind rebuild was required for Storefront V2 because only authored `storefront.css` changed; Control Plane Tailwind still ran as an existing test-project build side effect.

## Phase 10 - Browser QA With Playwright

Because this changes real browser visual behavior, run Playwright against V2.

- [ ] Start the local V2 stack:

```powershell
.\scripts\run-v2-local.ps1 -StopExisting
```

- [ ] Verify desktop header:
  - search icon visible
  - currency submit check icon visible when currency selector is enabled
  - cart icon and account menu unchanged
  - no console error
- [ ] Verify mobile header:
  - search icon visible
  - menu opens
  - currency check icon visible in mobile panel when currency selector is enabled
  - no layout overlap
- [ ] Verify search page:
  - search/filter submit icon visible
  - submitting `q` and filters preserves expected query string behavior
  - results page loads without JavaScript error
- [ ] Verify product page:
  - initial purchase feedback color is correct
  - invalid selection or failed add-to-cart shows error color
  - successful add-to-cart shows success feedback and success toast
  - product gallery fallback still works for a broken image
- [ ] Verify toast levels:
  - success
  - error
  - warning, if an existing browser flow can trigger it
  - info, if an existing browser flow can trigger it
- [ ] Verify browser network does not load Font Awesome CSS, JS, font files, or CDN URLs from Storefront V2.
- [ ] Capture screenshots only if a visual regression needs evidence.

Acceptance:

- [ ] Browser QA exercises real DOM behavior, not only smoke-load.
- [ ] No Font Awesome network dependency exists.
- [ ] Toast enter/dismiss animation still works through CSS state.
- [ ] Purchase feedback still responds to browser events.

## Phase 11 - QA Checklist Update

Update `docs/refactor-control-Commerce-storefront/QA-StorefrontV2.todo.md`.

- [ ] Add a checklist item for Storefront visual source ownership:
  - JS does not own toast colors/icons/animation.
  - CSS owns toast and purchase feedback visual states.
  - Razor owns explicit SVG icons.
  - Storefront V2 has no Font Awesome dependency.
- [ ] Add Playwright browser checks for:
  - desktop/mobile header icons
  - search filter icon
  - toast success/error
  - product purchase feedback success/error
  - no Font Awesome network requests
- [ ] Do not add Control Plane checks to Storefront V2 QA.

Acceptance:

- [ ] Future release QA can catch regressions without rereading this plan.
- [ ] Checklist names the affected V2 surfaces explicitly.

## Phase 12 - Final Closure Gate

Run final ownership search with curated exclusions:

```powershell
rg -n "resolveToastTheme|resolveToastIcon|style\.backgroundColor|style\.color|style\.opacity|style\.transform|innerHTML|text-emerald-700|text-red-700|fa-solid|fa-magnifying-glass|fa-check|SubmitIconCssClass" `
  BlazorShop.PresentationV2/BlazorShop.Storefront.V2 `
  BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM `
  --glob "!node_modules/**" `
  --glob "!bin/**" `
  --glob "!obj/**" `
  --glob "!wwwroot/css/site.css" `
  --glob "!wwwroot/css/wasm-site.css"
```

Expected notes:

- [ ] Any remaining `text-emerald-700` in static Razor badges may be acceptable only if the implementation explicitly keeps them outside JS feedback ownership. Prefer source ownership tests over broad grep for static Tailwind utility classes.
- [ ] Any remaining `data:image/svg+xml` must be the known product image fallback only.
- [ ] There must be no remaining Font Awesome class-based icons in Storefront V2/V2.WASM source.
- [ ] There must be no remaining `SubmitIconCssClass`.
- [ ] `storefrontCommerce.js` must contain no visual theme/icon ownership.

Run final diff review:

```powershell
git diff -- BlazorShop.PresentationV2/BlazorShop.Storefront.V2 BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM BlazorShop.Tests.V2 docs/refactor-control-Commerce-storefront/QA-StorefrontV2.todo.md
```

Acceptance:

- [ ] Diff is limited to the files in this plan or an implementation note explains any extra file.
- [ ] No backend, API contract, Runtime, Client, Starter, StorefrontBuilder, or Control Plane files changed.
- [ ] All focused build/test/browser gates have a recorded result.

## Definition Of Done

- [ ] Toast visual colors, icons, and animation state are owned by Razor/CSS, not JavaScript.
- [ ] Purchase feedback success/error color is owned by CSS state selectors, not JavaScript Tailwind class toggles.
- [ ] Storefront V2 no longer uses Font Awesome class-based icons in header or catalog filter controls.
- [ ] `CatalogFilterPanel` uses an explicit `RenderFragment` icon slot instead of a CSS-class icon string.
- [ ] Source ownership tests prevent regression while avoiding generated CSS false positives.
- [ ] `QA-StorefrontV2.todo.md` contains browser QA cases for this visual ownership rule.
- [ ] Focused builds and tests pass.
- [ ] Playwright verifies real desktop/mobile browser behavior and no Font Awesome network dependency.

## Decision Audit Trail

| # | Decision | Classification | Rationale | Rejected |
|---|---|---|---|---|
| 1 | Keep scope limited to Storefront V2/V2.WASM visual source ownership. | Auto-decided | The issue is presentation/source ownership, not ecommerce behavior. | Backend/API/Runtime/Starter changes. |
| 2 | Use explicit inline SVG instead of adding an icon package. | Auto-decided | Current header already uses inline SVG and package files have no Font Awesome dependency. | Adding Font Awesome/lucide/global icon system. |
| 3 | Replace icon class string API with a Razor `RenderFragment` slot. | Auto-decided | It removes Font Awesome coupling without creating a new design-system abstraction. | Keeping `SubmitIconCssClass` or introducing global icon registry. |
| 4 | Guardrail tests must scan curated source files, not generated CSS output. | Auto-decided | Tailwind output legitimately contains color utilities and will produce false positives. | Broad directory grep as the only release gate. |
| 5 | Allow product-data swatch inline styles and gallery transparent fallback. | Auto-decided | They are runtime content/fallback behavior, not theme/icon ownership leaks. | Blanket ban on all inline style or all data URI usage. |

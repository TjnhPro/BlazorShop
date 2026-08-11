# Storefront V2 Final Visual Cleanup Closure

Status: planned
Track: Phase 3.7 - Final V2 visual sweep and cleanup
Target area: Storefront V2 / V2.WASM component extraction closure
Predecessor: Phase 3.6 - Account Browser Runtime Leaves
Successor: none inside Phase 3 unless this phase discovers a concrete new reusable behavior leak

## Purpose

Close Phase 3 after the V2 component extraction work by making the remaining V2 and V2.WASM surfaces simpler, removing proven-dead wrappers and stale tests, cleaning final visual debt, splitting active QA from historical evidence, and proving the final state with architecture tests, build/test gates, and real Playwright browser journeys.

This is not another broad extraction phase. The target after Phase 3.6 is already visible in code:

```text
V2
  -> theme
  -> layout
  -> page composition
  -> final CSS
  -> final copy
  -> render-mode placement

V2.WASM
  -> downloadable theme-facing composition
  -> final visual options
  -> thin wrappers

Components.*
  -> reusable contracts
  -> reusable render primitives
  -> reusable server-rendered components
  -> reusable browser-interactive WasmHost leaves
```

Phase 3.7 succeeds when the repository gets easier to understand. It should reduce duplicate code, duplicate tests, stale QA history, and dead CSS/JS. It should not add a new design system, new platform abstraction, or new ecommerce behavior.

## Autoplan Review Summary

CEO review:

- The plan is valid because it changes the final quality bar from "extract one more thing" to "close the extraction loop".
- The product risk is scope creep: visual sweep can turn into a redesign and test cleanup can turn into deleting useful coverage.
- The right closure rule is evidence-based: remove only wrappers, selectors, tests, and QA steps that are proven redundant or stale.

Design review:

- A visual sweep is useful, but it must be a regression and consistency pass, not a new brand direction.
- Fix spacing, alignment, responsive overflow, state visibility, and inconsistent component composition only where screenshots or browser use show a concrete issue.
- Preserve current V2 visual identity unless a local defect makes a targeted correction necessary.

Engineering review:

- The current codebase supports this phase: account leaves are already in WasmHost, V2.WASM now has thin wrappers and account composition, and ProductCard/ProductGrid show real cleanup opportunities.
- `ProductCard` is a pure pass-through wrapper and can be removed after all references are migrated.
- `ProductGrid` duplicates `StorefrontProductSummaryGrid` behavior but currently lacks the canonical semantic hooks, so migration must preserve `data-storefront-product-summary-grid` and `data-storefront-product-summary-empty`.
- Test consolidation is appropriate because several tests now protect the same invariants from different files.

DX review:

- Future agents need one source of truth for each invariant: dependency graph, render mode, visual neutrality, cart/checkout ownership, account ownership, root required contracts, component behavior, and browser release journeys.
- Active QA should be runnable and current; historical execution evidence should be archived.
- The final closure report must make the Phase 3 end state explicit so future work does not reopen extraction by default.

## Current Code Evidence

- [ ] `BlazorShop.Storefront.Components.WasmHost/Components/Account` contains the five account runtime leaves:
  - [ ] `StorefrontAccountProfileEditor.razor`
  - [ ] `StorefrontAccountChangePasswordForm.razor`
  - [ ] `StorefrontAccountAddressBook.razor`
  - [ ] `StorefrontAccountOrderList.razor`
  - [ ] `StorefrontAccountOrderDetail.razor`
- [ ] `BlazorShop.Storefront.V2.WASM/Components/Account` contains only account composition/options files:
  - [ ] `StorefrontAccountApp.razor`
  - [ ] `StorefrontAccountNavigation.razor`
  - [ ] `StorefrontAccountViewOptions.cs`
  - [ ] `StorefrontAccountViewClasses.cs`
- [ ] Current source search shows no `IStorefrontBrowser*`, `HttpClient`, or account lifecycle/mutation methods in `BlazorShop.Storefront.V2` and `BlazorShop.Storefront.V2.WASM`.
- [ ] `BlazorShop.Storefront.V2/Components/Catalog/ProductCard.razor` is a pure pass-through to `StorefrontProductSummaryCard`.
- [ ] `BlazorShop.Storefront.V2/Components/Catalog/ProductGrid.razor` renders `ProductCard` and duplicates `StorefrontProductSummaryGrid` layout/empty-state behavior.
- [ ] `BlazorShop.Storefront.V2/Components/Catalog/StorefrontProductSummaryGrid.razor` is the canonical V2 product summary grid and includes semantic hooks.
- [ ] `BlazorShop.Storefront.V2/Pages/Product/V2ProductPageView.razor` still uses `ProductGrid` for related products.
- [ ] `StorefrontAccountApp.razor` still owns final copy such as `Customer account`, route titles, and unknown-section text directly in Razor.
- [ ] V2.WASM wrapper inventory currently includes:
  - [ ] `Cart/StorefrontCartSection.razor`
  - [ ] `Checkout/StorefrontCheckoutSection.razor`
  - [ ] `Catalog/StorefrontDiscountedProductRailSection.razor`
  - [ ] `Content/StorefrontContactFormSection.razor`
  - [ ] `System/StorefrontHybridRuntimeProbeSection.razor`
  - [ ] `Account/StorefrontAccountApp.razor`
  - [ ] `Account/StorefrontAccountNavigation.razor`
- [ ] Current project graph matches the approved mode split:
  - [ ] `Components` has no project references.
  - [ ] `Components.Primitives -> Components`.
  - [ ] `Components.Ssr -> Components + Presentation`.
  - [ ] `Components.WasmHost -> Components + Browser`.
  - [ ] `V2.WASM -> Browser + Components + Primitives + WasmHost`.
- [ ] Active QA file `docs/refactor-control-Commerce-storefront/QA-StorefrontV2.todo.md` is large and includes historical execution notes.
- [ ] No `docs/refactor-control-Commerce-storefront/archive/QA-StorefrontV2-History.md` file is currently present.

## Final Closure Target

After this phase:

```text
BlazorShop.Storefront.V2
  owns final visual host, final CSS/copy, page composition, layout, and render-mode placement

BlazorShop.Storefront.V2.WASM
  owns only downloadable visual/browser host composition and V2 options

BlazorShop.Storefront.Components
  owns browser-safe contracts and headless state/action/label contracts

BlazorShop.Storefront.Components.Primitives
  owns reusable render-only Razor primitives

BlazorShop.Storefront.Components.Ssr
  owns reusable server-rendered components over Presentation-prepared contexts

BlazorShop.Storefront.Components.WasmHost
  owns reusable browser-interactive components through Browser controllers
```

No further Phase 3 extraction work is planned unless Phase 3.7 discovers a concrete behavior leak that violates this ownership model.

## Hard Scope Lock

Allowed production areas:

- [x] `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Components/`
- [x] `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/`
- [x] `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/wwwroot/css/`
- [x] `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/wwwroot/js/`
- [x] `BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM/Components/`
- [x] `BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM/wwwroot/css/`
- [x] `BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM/_Imports.razor`
- [x] `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/_Imports.razor`
- [x] Focused tests under `BlazorShop.Tests.V2/PresentationV2/Storefront/`
- [x] Architecture tests under `BlazorShop.Tests.V2/Architecture/` only when they own Phase 3 boundary invariants.
- [x] `docs/architecture/03-runtime-boundaries.md`
- [x] `docs/architecture/05-project-and-folder-guide.md`
- [x] `docs/architecture/10-v2-contract-ownership.md`
- [x] `BlazorShop.PresentationV2/COMPONENT-MODES.md`
- [x] `docs/refactor-control-Commerce-storefront/QA-StorefrontV2.todo.md`
- [x] `docs/refactor-control-Commerce-storefront/archive/`
- [x] This plan file.

Explicit non-goals:

- [x] Do not move `StorefrontAccountApp`.
- [x] Do not move `StorefrontAccountNavigation`.
- [x] Do not move Header/Footer/Hero solely for reuse.
- [x] Do not create more shared component projects.
- [x] Do not change Browser controller contracts.
- [x] Do not change BFF routes.
- [x] Do not change Runtime/Client APIs.
- [x] Do not change Commerce Node.
- [x] Do not change Control Plane.
- [x] Do not change database schema.
- [x] Do not change StorefrontBuilder.
- [x] Do not change Starter.
- [x] Do not build a generic design system.
- [x] Do not introduce a new localization framework.
- [x] Do not rewrite Storefront V2 architecture.
- [x] Do not replace real Playwright browser journeys with smoke-only checks.

## Phase 0 - Baseline And Closure Scope Lock

Goal: record the exact current state before cleanup so the phase can prove it simplified the repo without hiding regressions.

Required reading:

- [x] Read `AGENTS.md`.
- [x] Read `docs/architecture/README.md`.
- [x] Read `docs/architecture/03-runtime-boundaries.md`.
- [x] Read `docs/architecture/05-project-and-folder-guide.md`.
- [x] Read `docs/architecture/10-v2-contract-ownership.md`.
- [x] Read `BlazorShop.PresentationV2/COMPONENT-MODES.md`.
- [x] Read `docs/refactor-control-Commerce-storefront/Storefront Account WasmHost Runtime Leaves.todo.md`.

Baseline commands:

```powershell
git branch --show-current
git rev-parse HEAD
git status --short
rg --files BlazorShop.PresentationV2/BlazorShop.Storefront.V2 BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM | rg "\.(razor|cs|css|js)$"
rg -n "ProductCard|ProductGrid|StorefrontProductSummaryGrid|StorefrontProductSummaryCard" BlazorShop.PresentationV2 BlazorShop.Tests.V2 docs/refactor-control-Commerce-storefront
rg -n "IStorefrontBrowser|HttpClient|IJSRuntime|HydrateAsync|InitializeProfile|InitializePassword|InitializeAddresses|InitializeOrders|InitializeOrderDetail|CreateAddressAsync|UpdateAddressAsync|DeleteAddressAsync|SetDefaultAddressAsync|SaveProfileAsync|ChangePasswordAsync" BlazorShop.PresentationV2/BlazorShop.Storefront.V2 BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM
```

Baseline artifacts:

- [x] Record current branch.
- [x] Record current HEAD.
- [x] Record current `git status --short`.
- [x] Record focused architecture test command list.
- [x] Record current full V2 test count if tests are run.
- [x] Record known skipped tests.
- [x] Record known solution warnings.
- [x] Record V2 and V2.WASM Razor file inventory.
- [x] Record current V2 CSS/JS entrypoints.
- [x] Record current QA checklist location and whether archive exists.
- [x] Capture current desktop screenshots for release surfaces if browser environment is available.
- [x] Capture current mobile screenshots for release surfaces if browser environment is available.

Baseline visual surfaces:

- [x] Home.
- [x] Category.
- [x] Search.
- [x] Product detail.
- [x] Cart.
- [x] Checkout.
- [x] Payment result.
- [x] Login.
- [x] Register.
- [x] Recovery.
- [x] Account profile.
- [x] Account addresses.
- [x] Account orders.
- [x] Account order detail.
- [x] Change password.
- [x] Content standard page.
- [x] Policy page.
- [x] FAQ/support page.
- [x] 404.
- [x] Consent.
- [x] Toast.

Stop conditions:

- [x] Stop if Phase 3.6 account extraction is not complete.
- [x] Stop if V2.WASM account leaves still own controller lifecycle behavior.
- [x] Stop if current project graph differs from approved component mode graph.
- [x] Stop if unrelated working tree changes touch the same files and cannot be safely worked around.

### Phase 0 Evidence (2026-08-11)

- Branch: `fea-component-extraction`; baseline HEAD: `2d8c36def15b80715fd10a8ccbc57306e335a2ae`.
- Baseline worktree contained only this user-supplied, then-untracked plan file; there were no overlapping source changes.
- Focused boundary command: `dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~StorefrontV2WASMRuntimeFoundationTests|FullyQualifiedName~StorefrontAccountWasmHostOwnershipTests|FullyQualifiedName~StorefrontComponentModeDependencyTests|FullyQualifiedName~StorefrontRenderModeOwnershipTests"`; it passed 48/48 on 2026-08-11.
- The preceding Phase 3.6 closure baseline ran 1,979 V2 tests: 1,977 passed, 2 intentionally skipped, 0 failed. The skips remain documented as pre-existing fixture-dependent cases.
- Known solution warnings are the existing `MessagePack` `NU1902`/`NU1903` advisories and the existing Browserslist/caniuse-lite update notice; no new warning was introduced by this phase.
- Source inventory: 63 V2/V2.WASM Razor/CSS/JS files in the first baseline scan; V2.WASM has no forbidden runtime-ownership hit. V2's only `localhost` matches are development configuration and launch settings.
- Entry points: V2 `wwwroot/css/input.css`, `wwwroot/css/site.css`, `wwwroot/css/storefront.css`, `wwwroot/js/storefrontCommerce.js`; V2.WASM `wwwroot/css/input.css`, `wwwroot/css/wasm-site.css`.
- Active checklist is `docs/refactor-control-Commerce-storefront/QA-StorefrontV2.todo.md`; no `archive/` directory existed at baseline.
- Playwright baseline artifacts cover all 21 listed surfaces at desktop and mobile under ignored `output/playwright/phase37-baseline-{desktop,mobile}-*.png`; account images used the approved QA customer and the existing fixture order `ORD-20260729-2686F072`.
- The account leaf plan is complete, V2.WASM leaves have no controller lifecycle/mutation ownership, the project graph matches `COMPONENT-MODES.md`, and no stop condition fired.

## Phase 1 - Extraction Stop Condition Audit

Goal: prove that Phase 3 extraction is closed unless a new concrete reusable behavior leak is found.

Audit V2 and V2.WASM for disallowed runtime ownership:

```powershell
rg -n "IStorefrontBrowser|HttpClient|IJSRuntime|HydrateAsync|Initialize|Controller\.|PostJsonAsync|GetAsync<|api/storefront|CommerceNode|localhost" BlazorShop.PresentationV2/BlazorShop.Storefront.V2 BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM
```

Expected V2 result:

- [ ] No Browser controller injection.
- [ ] No direct Commerce Node or Storefront API transport.
- [ ] No reusable browser lifecycle/mutation logic.
- [ ] Approved page/composition `@rendermode` remains in V2 pages only.
- [ ] Approved theme JavaScript/CSS behavior remains V2-owned.

Expected V2.WASM result:

- [ ] Thin wrappers may render WasmHost/primitive components with V2 labels/classes/actions.
- [ ] Account app may route/select panels and render account navigation.
- [ ] No account leaf lifecycle/mutation methods.
- [ ] No direct `HttpClient`.
- [ ] No direct `/api/storefront/*`.
- [ ] No Presentation, Runtime, Client, Commerce Node, Control Plane, or backend references.

Special account review:

- [ ] `StorefrontAccountApp` has no `IStorefrontBrowserAccountController`.
- [ ] `StorefrontAccountNavigation` has no browser controller injection.
- [ ] `StorefrontAccountApp` has no account leaf lifecycle methods.
- [ ] `StorefrontAccountNavigation` has no account mutation methods.
- [ ] `StorefrontAccountApp` still owns composition and route/panel selection only.
- [ ] `StorefrontAccountNavigation` still owns render-only navigation.

Decision:

- [ ] If all checks pass, mark extraction loop closed and continue as cleanup only.
- [ ] If a new leak is found, document the exact file, dependency, and behavior.
- [ ] Fix only the smallest boundary violation needed.
- [ ] Do not open another broad extraction phase without a separate approved plan.

## Phase 2 - Source Cleanup

Goal: remove proven-dead wrappers and stale imports without changing visible behavior.

### Phase 2A - Remove `ProductCard`

Current evidence:

- [ ] `ProductCard.razor` only renders `StorefrontProductSummaryCard`.
- [ ] It supplies `ProductSummaryCardVisuals.Labels` and `ProductSummaryCardVisuals.Classes`.
- [ ] It has only an `Item` parameter.

Tasks:

- [ ] Find all active `ProductCard` usages.
- [ ] Replace each usage with direct `StorefrontProductSummaryCard`.
- [ ] If the usage is inside a grid, prefer `StorefrontProductSummaryGrid` instead of hand-duplicating grid markup.
- [ ] Preserve `ProductSummaryCardVisuals.Labels`.
- [ ] Preserve `ProductSummaryCardVisuals.Classes`.
- [ ] Preserve product card semantic hooks from the primitive.
- [ ] Delete `BlazorShop.Storefront.V2/Components/Catalog/ProductCard.razor`.
- [ ] Remove obsolete namespace imports.
- [ ] Update tests that still read the removed path.
- [ ] Remove dead CSS selectors only if they were proven wrapper-specific.

Exit criteria:

- [ ] No active source file references `<ProductCard`.
- [ ] No active test reads `V2/Components/Catalog/ProductCard.razor` except a deliberate retired-path assertion.
- [ ] Direct product card rendering still uses `StorefrontProductSummaryCard`.

### Phase 2B - Consolidate `ProductGrid`

Current evidence:

- [ ] `ProductGrid.razor` renders a grid of `ProductCard`.
- [ ] `StorefrontProductSummaryGrid.razor` renders the same grid shape directly with `StorefrontProductSummaryCard`.
- [ ] `StorefrontProductSummaryGrid.razor` owns semantic hooks `data-storefront-product-summary-grid` and `data-storefront-product-summary-empty`.
- [ ] `V2ProductPageView.razor` uses `ProductGrid` for related products.

Tasks:

- [ ] Find all active `ProductGrid` usages.
- [ ] Replace `ProductGrid` usages with `StorefrontProductSummaryGrid`.
- [ ] Preserve `Items` binding.
- [ ] Preserve `EmptyMessage`.
- [ ] Preserve related-products layout intent.
- [ ] Ensure related products now get `data-storefront-product-summary-grid`.
- [ ] Delete `BlazorShop.Storefront.V2/Components/Catalog/ProductGrid.razor` if no unique responsibility remains.
- [ ] Update tests that still read or assert `ProductGrid`.
- [ ] Update historical docs only if active docs claim `ProductGrid` is current V2 canonical behavior.
- [ ] Do not touch Starter `ProductGrid` in this phase.

Exit criteria:

- [ ] One active V2 product summary grid implementation remains.
- [ ] V2 uses `StorefrontProductSummaryGrid` for category, search, deals, and related products.
- [ ] No active V2 source references `<ProductGrid`.

### Phase 2C - Account Composition Copy Cleanup

Current evidence:

- [ ] `StorefrontAccountApp.razor` correctly stays in V2.WASM.
- [ ] `StorefrontAccountNavigation.razor` correctly stays in V2.WASM.
- [ ] `StorefrontAccountViewOptions.cs` already owns leaf labels and final classes.
- [ ] `StorefrontAccountApp.razor` still contains final copy directly in Razor.

Tasks:

- [ ] Keep `StorefrontAccountApp.razor` in V2.WASM.
- [ ] Keep `StorefrontAccountNavigation.razor` in V2.WASM.
- [ ] Keep `StorefrontAccountShellClasses` in V2.WASM.
- [ ] Keep `AccountNavigationClasses` in V2.WASM.
- [ ] Add a V2-owned account app label structure if useful, for example `StorefrontAccountAppLabels`.
- [ ] Or add a clearly named account app labels section inside `StorefrontAccountViewOptions`.
- [ ] Move final account app copy from Razor into V2-owned options:
  - [ ] `Customer account`.
  - [ ] `Profile`.
  - [ ] `Addresses`.
  - [ ] `Orders`.
  - [ ] `Receipt`.
  - [ ] `Order`.
  - [ ] `Change password`.
  - [ ] `Account section not found`.
  - [ ] `The account section could not be found.`
- [ ] Do not move route parsing out of current account contracts.
- [ ] Do not move account navigation into shared packages.
- [ ] Do not change account URLs.
- [ ] Do not change account browser controller APIs.

Exit criteria:

- [ ] `StorefrontAccountApp` still composes the same panels.
- [ ] Final account app copy is V2-owned and centralized.
- [ ] No new shared visual ownership is introduced.

### Phase 2D - V2.WASM Wrapper Audit

Classify each wrapper:

```text
A. meaningful visual/config wrapper
B. pure pass-through wrapper
C. dead wrapper
```

Wrappers to inspect:

- [ ] `Cart/StorefrontCartSection.razor`.
- [ ] `Checkout/StorefrontCheckoutSection.razor`.
- [ ] `Catalog/StorefrontDiscountedProductRailSection.razor`.
- [ ] `Content/StorefrontContactFormSection.razor`.
- [ ] `System/StorefrontHybridRuntimeProbeSection.razor`.
- [ ] `Account/StorefrontAccountApp.razor`.
- [ ] `Account/StorefrontAccountNavigation.razor`.

Keep wrappers that supply any of:

- [ ] Final V2 labels.
- [ ] Final V2 classes.
- [ ] V2 action descriptors.
- [ ] V2 route/panel composition.
- [ ] Theme-specific template.
- [ ] Host semantic composition.

Remove wrappers only when:

- [ ] They are pure pass-through.
- [ ] Direct host composition remains clearer.
- [ ] Tests and page imports can be simplified.
- [ ] No V2 final class/copy/action ownership is lost.

Exit criteria:

- [ ] Each remaining wrapper has a documented responsibility.
- [ ] Each removed wrapper is proven redundant.
- [ ] No behavior changes are introduced.

### Phase 2E - Import, Namespace, And Dead Source Cleanup

Tasks:

- [ ] Remove unused `@using` directives in V2 and V2.WASM.
- [ ] Remove unused C# `using` directives in touched files.
- [ ] Remove stale component aliases.
- [ ] Remove retired Hybrid project references.
- [ ] Remove stale source comments that describe old ownership.
- [ ] Prefer clean namespace imports over pervasive fully-qualified Razor type names when it improves readability.
- [ ] Do not introduce ambiguous component names.

Searches:

```powershell
rg -n "Components.Hybrid|StorefrontCartView.razor|StorefrontCheckoutShell.razor|V2.WASM/.*/StorefrontAccount(ProfileEditor|ChangePasswordForm|AddressBook|OrderList|OrderDetail)" BlazorShop.PresentationV2 BlazorShop.Tests.V2 docs
rg -n "ProductCard|ProductGrid" BlazorShop.PresentationV2/BlazorShop.Storefront.V2 BlazorShop.Tests.V2
```

Exit criteria:

- [ ] No stale active-code path references remain.
- [ ] Removed paths appear only in deliberate retired-path tests or historical archive notes.

## Phase 3 - Visual, CSS, JS, And Semantic Hook Sweep

Goal: fix concrete visual and browser-surface debt after extraction without redesigning the storefront.

### Phase 3A - Visual Sweep Triage

Before editing:

- [ ] Capture desktop screenshots for baseline pages.
- [ ] Capture mobile screenshots for baseline pages.
- [ ] Record all visible defects in a short triage table.
- [ ] Classify each item as:
  - [ ] regression from extraction;
  - [ ] inconsistent V2 component styling;
  - [ ] responsive overflow/usability defect;
  - [ ] dead markup/CSS cleanup;
  - [ ] cosmetic improvement to defer.

Only fix:

- [ ] extraction regressions;
- [ ] obvious responsive usability defects;
- [ ] broken alignment or spacing that blocks production polish;
- [ ] inconsistent state styling that makes behavior unclear;
- [ ] dead CSS/JS/hook issues tied to removed source.

Defer:

- [ ] new design direction;
- [ ] new typography system;
- [ ] new color palette;
- [ ] full homepage redesign;
- [ ] redesign of checkout/account flows;
- [ ] pixel-perfect baseline automation unless already stable.

### Phase 3B - Product Summary And Catalog Visual Debt

Review:

- [ ] Home product summary sections.
- [ ] Category grid.
- [ ] Search grid.
- [ ] Deals/discounted rail.
- [ ] Related products on product detail.
- [ ] Empty state.
- [ ] Pagination.
- [ ] Breadcrumb.
- [ ] Filter panel.

Check:

- [ ] Product image aspect is stable.
- [ ] Card height is consistent enough for grid scan.
- [ ] Category/title grouping reads clearly.
- [ ] Price label hierarchy is clear.
- [ ] Action buttons do not wrap badly.
- [ ] Badges do not overlap image/title.
- [ ] Empty state spacing matches surrounding surface.
- [ ] Mobile card content does not overflow.
- [ ] Semantic hooks remain present.

Implementation rules:

- [ ] Do not blindly restore old DOM.
- [ ] Prefer the canonical `StorefrontProductSummaryGrid` and primitive card path.
- [ ] Do not duplicate card markup.
- [ ] Keep product image fallback behavior.
- [ ] Keep `loading="lazy"` where currently expected.

### Phase 3C - Product Detail Visual Sweep

Review:

- [ ] Gallery.
- [ ] Pricing.
- [ ] Availability.
- [ ] Purchase panel.
- [ ] Variant/attribute controls.
- [ ] Quantity control.
- [ ] Add-to-cart feedback.
- [ ] Related products.
- [ ] Support callout.

Check:

- [ ] Desktop two-column balance.
- [ ] Mobile stacking order.
- [ ] Gallery 1x1/product image framing where required by current product image policy.
- [ ] Thumbnail spacing.
- [ ] Pricing hierarchy.
- [ ] Variant selected state.
- [ ] Blocked purchase state.
- [ ] Feedback placement.
- [ ] Button sizing.

### Phase 3D - Cart And Checkout Visual Sweep

Cart:

- [ ] Empty cart.
- [ ] One line.
- [ ] Multiple lines.
- [ ] Alerts.
- [ ] Quantity input.
- [ ] Remove action.
- [ ] Summary.
- [ ] Checkout CTA.
- [ ] Mobile layout.

Checkout:

- [ ] Checkout form.
- [ ] Address fields.
- [ ] Shipping section.
- [ ] Payment section.
- [ ] Summary.
- [ ] Place-order action.
- [ ] Validation states.
- [ ] Success/result branch.
- [ ] Empty-cart branch.
- [ ] Hidden WasmHost shell remains visually hidden when `ShowPanel=false`.

Rules:

- [ ] Do not change cart runtime behavior.
- [ ] Do not change checkout state machine.
- [ ] Do not change order placement.
- [ ] Do not change payment provider behavior.

### Phase 3E - Auth, Account, Content, Payment, Error Visual Sweep

Auth/account:

- [ ] Login.
- [ ] Register.
- [ ] Recovery.
- [ ] Profile.
- [ ] Change password.
- [ ] Address book.
- [ ] Orders.
- [ ] Order detail.
- [ ] Account navigation.

Content/payment/error:

- [ ] Content standard.
- [ ] Policy.
- [ ] FAQ/support.
- [ ] Payment success.
- [ ] Payment pending/cancelled/failure if deterministic.
- [ ] 404.
- [ ] Service unavailable if deterministic.

Check:

- [ ] Form width.
- [ ] Labels.
- [ ] Alerts.
- [ ] Buttons.
- [ ] Table readability.
- [ ] Address card stacking.
- [ ] Account navigation.
- [ ] Heading hierarchy.
- [ ] CTA consistency.
- [ ] Mobile rendering.

### Phase 3F - Accessibility Cleanup

Review active surfaces for:

- [ ] `aria-label`.
- [ ] `aria-current`.
- [ ] `aria-live`.
- [ ] `role=alert`.
- [ ] `role=status`.
- [ ] button labels.
- [ ] form labels.
- [ ] heading order.
- [ ] keyboard reachability.
- [ ] focus visibility.
- [ ] color-independent state cues.

Scope:

- [ ] Fix regressions introduced or exposed by component extraction.
- [ ] Do not redesign all accessibility architecture in this phase.

### Phase 3G - CSS, JS, And Semantic Hook Cleanup

CSS entrypoints:

- [ ] `BlazorShop.Storefront.V2/wwwroot/css/storefront.css`.
- [ ] `BlazorShop.Storefront.V2/wwwroot/css/site.css`.
- [ ] `BlazorShop.Storefront.V2/wwwroot/css/input.css`.
- [ ] `BlazorShop.Storefront.V2.WASM/wwwroot/css/wasm-site.css` if touched by V2.WASM options.

JS entrypoint:

- [ ] `BlazorShop.Storefront.V2/wwwroot/js/storefrontCommerce.js`.

CSS tasks:

- [ ] Search selectors tied to removed `ProductCard` or `ProductGrid`.
- [ ] Search old wrapper selectors.
- [ ] Search retired component selectors.
- [ ] Search duplicate selectors.
- [ ] Remove confirmed dead selectors.
- [ ] Merge duplicate rules only when behavior remains identical.
- [ ] Do not hand-edit generated CSS if a source pipeline owns it.

JS tasks:

- [ ] Search selectors tied to removed wrappers/components.
- [ ] Remove obsolete fallback code only when no active markup uses it.
- [ ] Preserve progressive enhancement.
- [ ] Preserve purchase hooks.
- [ ] Preserve toast behavior.
- [ ] Preserve consent behavior.
- [ ] Preserve gallery behavior.
- [ ] Verify no duplicate event ownership after WASM activation.

Semantic hook audit:

- [ ] `data-storefront-product-*`.
- [ ] `data-storefront-cart-*`.
- [ ] `data-storefront-checkout-*`.
- [ ] `data-storefront-account-*`.
- [ ] `data-storefront-consent-*`.
- [ ] `data-storefront-toast-*`.

Resolve:

- [ ] orphan JS selector;
- [ ] orphan markup hook;
- [ ] duplicate hook owner;
- [ ] missing canonical hook after wrapper removal.

## Phase 4 - Test Consolidation

Goal: one authoritative test owner per invariant, with behavior and security coverage preserved.

### Phase 4A - Build Test Ownership Matrix

Create a temporary matrix during implementation:

```text
Test file
Invariant
Behavior
Duplicate with
Keep
Merge
Delete
Reason
```

Review at minimum:

- [ ] `StorefrontV2WASMRuntimeFoundationTests`.
- [ ] `StorefrontCartCheckoutWasmHostBoundaryTests`.
- [ ] `StorefrontAccountWasmHostOwnershipTests`.
- [ ] `StorefrontComponentModeDependencyTests`.
- [ ] `StorefrontRenderModeOwnershipTests`.
- [ ] `StorefrontComponentVisualNeutralityTests`.
- [ ] `StorefrontVisualSourceOwnershipTests`.
- [ ] `StorefrontRequiredVisualContractsHardeningTests`.
- [ ] `StorefrontVisualOnlyBoundaryTests`.
- [ ] `StorefrontBrandingMarkupTests`.
- [ ] `StorefrontComponentsHeadlessPresentationRefactorTests`.
- [ ] Commerce flow tests.
- [ ] Component-specific tests.

### Phase 4B - Authoritative Owners

Project graph owner:

- [ ] `StorefrontComponentModeDependencyTests`.
- [ ] Owns exact project references.
- [ ] Owns forbidden dependencies.
- [ ] Owns transitive reachability.
- [ ] Owns cycles.
- [ ] Owns retired physical Hybrid absence.

Render mode owner:

- [ ] `StorefrontRenderModeOwnershipTests`.
- [ ] Owns no reusable `@rendermode`.
- [ ] Owns no `InteractiveServer`.
- [ ] Owns no `InteractiveAuto`.
- [ ] Owns approved `InteractiveWebAssembly` owners.

Visual neutrality owner:

- [ ] `StorefrontComponentVisualNeutralityTests`.
- [ ] Owns no literal final classes in reusable projects.
- [ ] Owns no V2 visual assets in reusable projects.
- [ ] Owns no final V2 copy tokens in reusable projects.
- [ ] Owns no theme CSS assets in reusable projects.

Cart/checkout owner:

- [ ] `StorefrontCartCheckoutWasmHostBoundaryTests`.
- [ ] Owns cart/checkout WasmHost runtime ownership.
- [ ] Owns V2.WASM cart/checkout wrapper boundary.
- [ ] Owns V2 page render-mode ownership for cart/checkout.
- [ ] Owns shared cart/checkout contract ownership.

Account owner:

- [ ] `StorefrontAccountWasmHostOwnershipTests`.
- [ ] Owns five account leaves in WasmHost.
- [ ] Owns account browser controller lifecycle ownership.
- [ ] Owns V2.WASM account shell/navigation only.
- [ ] Owns shared account contract ownership.

Root contract owner:

- [ ] `StorefrontRequiredVisualContractsHardeningTests`.
- [ ] Owns required root contexts.
- [ ] Owns no fallback routes/actions/classes where host must supply contracts.
- [ ] Owns host-supplied root contract requirements.

Component behavior owners:

- [ ] Component-specific tests own DOM and behavior semantics for primitives/SSR/WasmHost components.
- [ ] Browser controller tests own same-origin controller command behavior.
- [ ] Browser Playwright release journeys own visible end-to-end browser behavior.

### Phase 4C - Trim Runtime Foundation Tests

Keep in `StorefrontV2WASMRuntimeFoundationTests`:

- [ ] WASM startup.
- [ ] same-origin browser runtime registration.
- [ ] no Commerce Node configuration.
- [ ] V2.WASM project identity.
- [ ] core project dependency contract if not owned elsewhere.

Move/delete from `StorefrontV2WASMRuntimeFoundationTests` when duplicated:

- [ ] exact cart hook counts;
- [ ] exact checkout hook counts;
- [ ] cart wrapper wiring;
- [ ] checkout wrapper wiring;
- [ ] exact cart labels property inventory;
- [ ] exact checkout labels property inventory;
- [ ] account leaf ownership.

Goal:

- [ ] RuntimeFoundation becomes small, stable, and architectural.

### Phase 4D - Trim Required Visual Contracts Tests

Keep:

- [ ] root page context required;
- [ ] no fallback page contexts;
- [ ] root action/classes required;
- [ ] no fallback routes/actions/classes;
- [ ] host-supplied contracts.

Remove duplicates for:

- [ ] component ownership path;
- [ ] WasmHost project ownership;
- [ ] controller ownership;
- [ ] render-mode ownership;
- [ ] source inventory that another scanner owns.

### Phase 4E - Trim Render Mode And Visual Neutrality Tests

Render mode:

- [ ] Keep global reusable package scanner.
- [ ] Keep public no-server/no-auto scanner.
- [ ] Keep approved InteractiveWebAssembly owner scanner.
- [ ] Keep scanner self-tests.
- [ ] Remove per-component theory tests when the global scanner covers the same source set.

Visual neutrality:

- [ ] Keep global reusable render project scanner.
- [ ] Keep scanner positive fixtures.
- [ ] Keep scanner negative fixtures.
- [ ] Keep forbidden-copy scanner.
- [ ] Keep visual asset scanner.
- [ ] Remove brittle inventory tests such as `VisualNeutralityScanIncludesCurrentPrimitiveWasmHostAndContactComponents` if dynamic directory enumeration already provides coverage.
- [ ] Reduce component-name-specific regex fixtures to representative examples.

### Phase 4F - Improve Visual Source Ownership Scanner

Tasks:

- [ ] Prefer dynamic curated enumeration over hardcoded source file lists where possible.
- [ ] Enumerate active V2 and V2.WASM source files.
- [ ] Exclude `bin`, `obj`, generated output, artifacts, fixtures, docs, node modules, and temporary folders.
- [ ] Use global checks for:
  - [ ] FontAwesome classes;
  - [ ] retired visual tokens;
  - [ ] old wrapper names;
  - [ ] `SubmitIconCssClass`;
  - [ ] stale component source names.
- [ ] Keep targeted file-specific tests where the invariant is genuinely file-specific.

### Phase 4G - Remove Stale Path Tests

Search:

```powershell
rg -n "V2.WASM.*/StorefrontCartView.razor|V2.WASM.*/StorefrontCheckoutShell.razor|V2.WASM.*/StorefrontAccount(ProfileEditor|ChangePasswordForm|AddressBook|OrderList|OrderDetail).razor|ProductCard.razor|ProductGrid.razor|Components.Hybrid" BlazorShop.Tests.V2
```

Classify each reference:

- [ ] Intentional retired-path assertion.
- [ ] Stale test.
- [ ] Historical fixture.

Remove:

- [ ] stale tests that only check old file paths;
- [ ] file-existence tests made redundant by stronger ownership tests;
- [ ] brittle exact counts where semantic coverage already exists elsewhere.

Keep:

- [ ] architecture boundary tests;
- [ ] security tests;
- [ ] render-mode tests;
- [ ] same-origin browser transport tests;
- [ ] business-critical mutation semantics;
- [ ] semantic DOM hook tests;
- [ ] accessibility contract tests;
- [ ] root required contract tests.

### Phase 4H - Test Helper Cleanup

Consolidate only where useful:

- [ ] `RepositoryRoot`.
- [ ] `RepositoryPath`.
- [ ] `ReadRepositoryFile`.
- [ ] `CountOccurrences`.
- [ ] `ReadDirectory`.
- [ ] `NormalizePath`.

Allowed:

- [ ] A small shared helper under `BlazorShop.Tests.V2/PresentationV2/Storefront/TestSupport`.

Rules:

- [ ] Do not over-abstract tests.
- [ ] Do not make tests harder to read.
- [ ] Do not create a large testing framework.
- [ ] Keep helper extraction optional unless it meaningfully reduces duplication.

## Phase 5 - QA Consolidation And Browser Release Journeys

Goal: replace historical checklist sprawl with a current, runnable release gate.

### Phase 5A - Split Active QA From History

Preferred final structure:

```text
docs/refactor-control-Commerce-storefront/
  QA-StorefrontV2.todo.md
  archive/
    QA-StorefrontV2-History.md
```

Active QA file should contain:

- [ ] current setup;
- [ ] current build/test gate;
- [ ] canonical browser journeys;
- [ ] network assertions;
- [ ] visual screenshot checklist;
- [ ] known skips/warnings;
- [ ] release sign-off checklist.

Archive should contain:

- [ ] Phase 3.1 to 3.6 execution evidence;
- [ ] V2F6/V2F7/V2F8 historical notes if present;
- [ ] old screenshots references;
- [ ] old timestamps;
- [ ] old order references;
- [ ] old execution diary entries.

Rules:

- [ ] Do not delete useful evidence.
- [ ] Do not keep active QA as an execution diary.
- [ ] Do not keep obsolete old route assertions in the active release checklist.
- [ ] Preserve historical context in archive.

### Phase 5B - Global Browser Instrumentation

For Playwright release journeys:

- [ ] Collect console errors.
- [ ] Collect page errors.
- [ ] Collect request URLs.
- [ ] Collect response status codes.
- [ ] Fail on unexpected 5xx.
- [ ] Fail on direct Commerce Node browser requests.
- [ ] Fail on unexpected `api/storefront/stores/*` direct browser calls unless explicitly routed through same-origin Presentation/BFF behavior.
- [ ] Fail on `/_blazor` server UI circuit.
- [ ] Fail on WebSocket/EventSource UI transport unless a future approved architecture reopens it.
- [ ] Record browser trace or screenshots for failures.

### Phase 5C - Canonical Journey 1: Public Catalog

Flow:

```text
Home -> Category or Search -> Product -> select variant if applicable -> Add to Cart
```

Assert:

- [ ] Header renders.
- [ ] Main navigation works.
- [ ] Product summary grid renders.
- [ ] Product cards render images, price, status, and actions.
- [ ] Product detail gallery renders.
- [ ] Purchase panel renders.
- [ ] Variant controls work if product has variants.
- [ ] Add-to-cart sends one expected mutation.
- [ ] Cart badge updates.
- [ ] Toast/feedback appears.
- [ ] No console/page errors.
- [ ] Desktop full behavior passes.
- [ ] Mobile critical interaction passes.

### Phase 5D - Canonical Journey 2: Cart

Flow:

```text
Cart with items -> quantity update -> remove -> empty state
```

Assert:

- [ ] Cart page loads.
- [ ] Cart lines render.
- [ ] Quantity update sends one expected mutation.
- [ ] Line total/summary updates.
- [ ] Remove sends one expected mutation.
- [ ] Empty state renders when cart is empty.
- [ ] Checkout CTA state is correct.
- [ ] No direct Commerce Node browser request.
- [ ] No console/page errors.

Optional:

- [ ] Clear cart only if it adds coverage beyond remove-to-empty.

### Phase 5E - Canonical Journey 3: Checkout

Flow:

```text
Product -> Cart -> Checkout -> contact/address -> shipping if required -> COD/payment fixture -> place order -> success
```

Assert:

- [ ] Checkout page loads with valid cart.
- [ ] Address/contact fields validate.
- [ ] Shipping method is selected or skipped according to current rules.
- [ ] Payment method uses COD or sandbox fixture.
- [ ] Place-order sends exactly one mutation per submit.
- [ ] Redirect/result is correct.
- [ ] Cart clears/closes where expected.
- [ ] Order reference appears.
- [ ] No direct Commerce Node browser request.
- [ ] No console/page errors.

### Phase 5F - Canonical Journey 4: Account

Flow:

```text
login -> profile -> address create/update/default/delete -> orders -> order detail -> invalid password validation
```

Assert:

- [ ] Login works with QA account.
- [ ] Profile loads.
- [ ] Profile update works and can be restored if needed.
- [ ] Address create works with a unique QA marker.
- [ ] Address update works.
- [ ] Default billing/shipping works if supported.
- [ ] Address delete removes the QA marker.
- [ ] Orders list loads.
- [ ] Order detail loads from a list link.
- [ ] Invalid password validation returns visible error and does not log out.
- [ ] Password rotation is not required unless fixture management is safe.
- [ ] Desktop full behavior passes.
- [ ] Mobile account navigation and one critical form interaction pass.

### Phase 5G - Canonical Journey 5: Content And Security

Flow:

```text
content page -> consent save -> consent revoke -> auth redirect -> 404
```

Assert:

- [ ] Content standard page renders.
- [ ] Policy/FAQ/support page renders if present in fixture.
- [ ] Consent save works.
- [ ] Consent revoke/change works.
- [ ] Auth redirect protects account routes.
- [ ] Unknown route returns the expected not-found UI.
- [ ] Optional service-unavailable/maintenance page only if deterministic.
- [ ] No console/page errors.

### Phase 5H - Canonical Journey 6: SEO, Network, And Runtime

Verify:

- [ ] `robots.txt`.
- [ ] sitemap.
- [ ] canonical/meta tags.
- [ ] noindex rules for cart/checkout/account/internal search if applicable.
- [ ] same-origin browser requests.
- [ ] no direct Commerce Node.
- [ ] no `/_blazor`.
- [ ] no WebSocket UI transport.
- [ ] no EventSource UI transport.
- [ ] no unexpected 5xx.
- [ ] no unexpected console errors.
- [ ] no page errors.

### Phase 5I - Mobile QA Strategy

Desktop:

- [ ] Full functional E2E for all six canonical journeys.

Mobile:

- [ ] Responsive visual plus critical interaction checks.

Mobile surfaces:

- [ ] Header/menu.
- [ ] Catalog grid.
- [ ] Product purchase.
- [ ] Cart.
- [ ] Checkout form.
- [ ] Account navigation.
- [ ] Content page.
- [ ] Consent/toast.

Rule:

- [ ] Do not duplicate every CRUD branch on mobile unless a mobile-specific bug is found.

### Phase 5J - Visual Screenshot Matrix

Capture:

- [ ] Desktop.
- [ ] Tablet if tooling/time allows.
- [ ] Mobile.

Minimum pages:

- [ ] Home.
- [ ] Category.
- [ ] Search.
- [ ] Product.
- [ ] Cart.
- [ ] Checkout.
- [ ] Account.
- [ ] Content.

Additional states:

- [ ] Empty cart.
- [ ] Blocked/unavailable product if deterministic.
- [ ] Toast.
- [ ] Consent.
- [ ] Account address cards.
- [ ] Order detail.
- [ ] Payment result.

Rule:

- [ ] Use screenshots for visual review, not brittle pixel-perfect automation unless stable baseline tooling already exists.

## Phase 6 - Final Architecture, Build, Test, Browser, And Closure Gate

Goal: prove Phase 3 is complete and leave an explicit closure report.

### Phase 6A - Architecture Re-Audit

Final graph:

```text
Components
  -> browser-safe contracts only

Components.Primitives
  -> Components only

Components.Ssr
  -> Components + Presentation

Components.WasmHost
  -> Components + Browser

V2.WASM
  -> Browser + Components + Primitives + WasmHost

V2
  -> host composition + Presentation + V2.WASM + shared component projects
```

Checks:

- [ ] No cycles.
- [ ] No Presentation reachable from V2.WASM.
- [ ] No Runtime/Client/backend reachable from reusable packages.
- [ ] No Control Plane reachable from Storefront components.
- [ ] No `Web.SharedV2` business contracts in reusable component packages.
- [ ] No physical `Components.Hybrid` project returns.
- [ ] No `Features` folder returns.

### Phase 6B - Render Mode Re-Audit

Confirm:

- [ ] no reusable package owns `@rendermode`;
- [ ] no `InteractiveServer`;
- [ ] no `InteractiveAuto`;
- [ ] only approved V2 files own `InteractiveWebAssembly` placement.

Approved current concept:

```text
server prerender
  + useful HTML/JS
  + InteractiveWebAssembly takeover
```

Do not reintroduce public server circuit UI.

### Phase 6C - Focused Architecture Gate

Run focused tests:

```powershell
dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~ComponentModeDependency"
dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~RenderModeOwnership"
dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~VisualNeutrality"
dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~VisualOnlyBoundary"
dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~CartCheckoutWasmHost"
dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~AccountWasmHost"
dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~RequiredVisualContracts"
dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~VisualSourceOwnership"
dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~ServerInteractiveTransport"
dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~BrowserActionBoundary"
```

Record:

- [ ] passed count;
- [ ] failed count;
- [ ] removed test count;
- [ ] new consolidated test count;
- [ ] known skips.

Goal:

- [ ] Maximum useful invariant coverage with minimum duplication.

### Phase 6D - Focused Visual And Component Gate

Run focused component tests for:

- [ ] Product summary.
- [ ] Product detail.
- [ ] Product gallery.
- [ ] Product purchase panel.
- [ ] Pagination.
- [ ] Breadcrumb.
- [ ] Catalog filter.
- [ ] Consent.
- [ ] Toast.
- [ ] Cart.
- [ ] Checkout.
- [ ] Account leaves.

Use filters matching current test names:

```powershell
dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~ProductSummary|FullyQualifiedName~ProductDetail|FullyQualifiedName~ProductGallery|FullyQualifiedName~PurchasePanel"
dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~Pagination|FullyQualifiedName~Breadcrumb|FullyQualifiedName~CatalogFilter"
dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~Consent|FullyQualifiedName~Toast"
dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~Cart|FullyQualifiedName~Checkout|FullyQualifiedName~Account"
```

### Phase 6E - Full Build And Full Test Gate

Run:

```powershell
dotnet build BlazorShop.sln --no-restore
dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-restore
git diff --check
```

Record:

- [ ] build result;
- [ ] warnings;
- [ ] known unrelated warnings;
- [ ] test passed count;
- [ ] test skipped count;
- [ ] test failed count;
- [ ] `git diff --check` result.

Rules:

- [ ] A lower raw test count is acceptable only if duplicate tests were consolidated and invariant coverage remains documented.
- [ ] Do not close with failing architecture tests.
- [ ] Do not close with failing browser-boundary tests.

### Phase 6F - Final Browser Gate

Run all six canonical browser journeys:

- [ ] Public Catalog.
- [ ] Cart.
- [ ] Checkout.
- [ ] Account.
- [ ] Content/Security.
- [ ] SEO/Network/Runtime.

Collect:

- [ ] screenshots;
- [ ] browser errors;
- [ ] console errors;
- [ ] request log;
- [ ] unexpected status codes;
- [ ] network guardrail report.

Closure conditions:

- [ ] No direct Commerce Node browser calls.
- [ ] No `/_blazor` public server UI circuit.
- [ ] No unexpected console errors.
- [ ] No unexpected page errors.
- [ ] Place-order works with COD/sandbox fixture.
- [ ] Account flow works with QA account.
- [ ] Desktop and mobile checks pass according to Phase 5 strategy.

### Phase 6G - Documentation Cleanup

Update:

- [ ] `BlazorShop.PresentationV2/COMPONENT-MODES.md`.
- [ ] `docs/architecture/03-runtime-boundaries.md`.
- [ ] `docs/architecture/05-project-and-folder-guide.md`.
- [ ] `docs/architecture/10-v2-contract-ownership.md`.
- [ ] `docs/refactor-control-Commerce-storefront/QA-StorefrontV2.todo.md`.
- [ ] `docs/refactor-control-Commerce-storefront/archive/QA-StorefrontV2-History.md`.
- [ ] This plan file.

Document final state:

- [ ] Phase 3 extraction closed.
- [ ] V2 is visual/theme host.
- [ ] V2.WASM is visual/browser host composition.
- [ ] Shared component packages own reusable implementation.
- [ ] ProductCard/ProductGrid decision.
- [ ] Wrapper retention/removal decisions.
- [ ] Active QA release gate.
- [ ] Historical QA evidence archived.

### Phase 6H - Closure Report

Write final report section in this plan or a sibling closure file with:

- [ ] final project graph;
- [ ] final V2/V2.WASM responsibility split;
- [ ] components extracted across Phase 3.1 to 3.6;
- [ ] components intentionally retained;
- [ ] dead wrappers removed;
- [ ] wrappers intentionally retained;
- [ ] visual debt fixed;
- [ ] CSS/JS cleanup summary;
- [ ] tests removed/merged;
- [ ] final test counts;
- [ ] final browser journeys;
- [ ] network evidence;
- [ ] known warnings/skips;
- [ ] remaining non-Phase-3 debt.

## Suggested Commit Breakdown

Use small commits so cleanup remains reviewable:

1. [ ] `docs(storefront): record phase 3.7 final cleanup plan`
2. [ ] `refactor(storefront): remove redundant product card and grid wrappers`
3. [ ] `refactor(storefront): consolidate v2 account presentation copy`
4. [ ] `refactor(storefront): clean v2 wasm wrappers imports and dead source`
5. [ ] `fix(storefront): resolve phase 3 visual and responsive debt`
6. [ ] `test(storefront): consolidate component architecture ownership tests`
7. [ ] `test(storefront): trim duplicated runtime and visual contract assertions`
8. [ ] `qa(storefront): split storefront v2 release gate from history`
9. [ ] `qa(storefront): run final browser visual and network proof`
10. [ ] `docs(storefront): close phase 3 component extraction`

Do not make a single giant cleanup commit unless the implementation is purely documentation.

## Risk Register

- [ ] Risk: visual sweep becomes a redesign.
  - Mitigation: require screenshot evidence and classify cosmetic redesign as deferred.
- [ ] Risk: deleting tests removes real boundary coverage.
  - Mitigation: build an invariant ownership matrix before deletion.
- [ ] Risk: removing `ProductGrid` loses semantic hooks or empty-state behavior.
  - Mitigation: migrate to `StorefrontProductSummaryGrid`, not hand-written markup.
- [ ] Risk: removing wrappers hides where V2 final labels/classes come from.
  - Mitigation: keep wrappers with meaningful V2 options/composition responsibility.
- [ ] Risk: active QA history is lost.
  - Mitigation: archive historical execution evidence before shrinking the active file.
- [ ] Risk: Playwright release gate becomes too broad and slow.
  - Mitigation: desktop full E2E, mobile critical interaction/visual checks only.
- [ ] Risk: new architecture leak is discovered late.
  - Mitigation: document exact file and fix smallest boundary only; do not reopen broad extraction.

## Definition Of Done - Source Cleanup

- [ ] `ProductCard` removed if no meaningful responsibility remains.
- [ ] `ProductGrid` removed or consolidated if redundant.
- [ ] No dead V2.WASM runtime leaf remains.
- [ ] No dead wrapper remains.
- [ ] No obsolete imports/namespaces remain.
- [ ] No orphan CSS from deleted wrappers.
- [ ] No orphan JS selector/hook.
- [ ] All retained wrappers have a reason.

## Definition Of Done - Account

- [ ] `StorefrontAccountApp` stays V2.WASM.
- [ ] `StorefrontAccountNavigation` stays V2.WASM.
- [ ] Neither injects Browser controllers.
- [ ] Neither owns hydration/mutation methods.
- [ ] Final Account app copy is consolidated into V2-owned options/labels where useful.
- [ ] Five Account runtime leaves remain WasmHost-owned.

## Definition Of Done - Visual

- [ ] Product Summary debt reviewed and fixed or explicitly deferred.
- [ ] Shell reviewed desktop/mobile.
- [ ] Catalog reviewed desktop/mobile.
- [ ] Product Detail reviewed desktop/mobile.
- [ ] Cart reviewed desktop/mobile.
- [ ] Checkout reviewed desktop/mobile.
- [ ] Auth/Account reviewed desktop/mobile.
- [ ] Content/Payment/Error states reviewed.
- [ ] No known Phase 3 visual regression remains unexplained.

## Definition Of Done - Tests

- [ ] One authoritative test owner exists per architecture invariant.
- [ ] RuntimeFoundation trimmed to foundation concerns.
- [ ] RequiredVisualContracts trimmed to root-contract concerns.
- [ ] Duplicate render-mode tests removed.
- [ ] Brittle visual-neutrality inventory tests removed or justified.
- [ ] VisualSourceOwnership uses dynamic source enumeration where appropriate.
- [ ] Stale path assertions removed.
- [ ] Component behavior and semantic coverage retained.
- [ ] Security and browser-boundary tests retained.
- [ ] Raw test-count reduction documented and accepted.

## Definition Of Done - QA

- [ ] Active QA file contains current release gate only.
- [ ] Historical phase evidence archived.
- [ ] Six canonical browser journeys defined.
- [ ] Global network/error collector preferred.
- [ ] Mobile suite reduced to visual/critical interaction checks.
- [ ] Low-level API negatives not redundantly replayed in Playwright.
- [ ] Final screenshots recorded.

## Definition Of Done - Architecture

- [ ] Components has no project references.
- [ ] Primitives references Components only.
- [ ] SSR references Components + Presentation only.
- [ ] WasmHost references Components + Browser only.
- [ ] V2.WASM cannot reach Presentation/Runtime/Client/backend.
- [ ] No project cycles.
- [ ] No reusable `@rendermode`.
- [ ] No `InteractiveServer`.
- [ ] No `InteractiveAuto`.
- [ ] No browser direct Commerce Node.
- [ ] No `/_blazor` server UI circuit.

## Final Closure Gate

Only close Phase 3 when:

- [ ] source cleanup passes;
- [ ] visual sweep passes;
- [ ] CSS/JS cleanup passes;
- [ ] test consolidation passes;
- [ ] QA consolidation passes;
- [ ] focused tests pass;
- [ ] full build passes;
- [ ] full V2 tests pass;
- [ ] browser journeys pass;
- [ ] network guardrail passes;
- [ ] desktop/mobile visual checks pass;
- [ ] docs are updated;
- [ ] closure report is written.

Final status wording:

```text
PHASE 3 - V2 COMPONENT EXTRACTION AND VISUAL CLEANUP
CLOSED
```

## Post-Phase State

```text
BlazorShop.Storefront.V2
  -> theme layout
  -> Header/Footer
  -> page composition
  -> final CSS
  -> final copy
  -> render-mode placement

BlazorShop.Storefront.V2.WASM
  -> visual options
  -> AccountApp
  -> AccountNavigation
  -> meaningful thin wrappers

BlazorShop.Storefront.Components
  -> contracts and headless state/actions

BlazorShop.Storefront.Components.Primitives
  -> reusable browser-safe render-only Razor

BlazorShop.Storefront.Components.Ssr
  -> reusable server-rendered Razor

BlazorShop.Storefront.Components.WasmHost
  -> reusable browser-interactive components
```

## Final Principle

Phase 3.7 succeeds when the repository becomes simpler.

Success is not:

- [ ] more abstractions;
- [ ] more tests;
- [ ] more QA checkboxes;
- [ ] more wrappers.

Success is:

- [ ] clear ownership;
- [ ] fewer duplicated tests;
- [ ] fewer dead components;
- [ ] fewer stale QA cases;
- [ ] cleaner V2/V2.WASM;
- [ ] resolved concrete visual debt;
- [ ] repeatable release proof.

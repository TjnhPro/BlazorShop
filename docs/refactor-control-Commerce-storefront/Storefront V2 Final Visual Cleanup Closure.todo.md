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

- [x] `BlazorShop.Storefront.Components.WasmHost/Components/Account` contains the five account runtime leaves:
  - [x] `StorefrontAccountProfileEditor.razor`
  - [x] `StorefrontAccountChangePasswordForm.razor`
  - [x] `StorefrontAccountAddressBook.razor`
  - [x] `StorefrontAccountOrderList.razor`
  - [x] `StorefrontAccountOrderDetail.razor`
- [x] `BlazorShop.Storefront.V2.WASM/Components/Account` contains only account composition/options files:
  - [x] `StorefrontAccountApp.razor`
  - [x] `StorefrontAccountNavigation.razor`
  - [x] `StorefrontAccountViewOptions.cs`
  - [x] `StorefrontAccountViewClasses.cs`
- [x] Current source search shows no `IStorefrontBrowser*`, `HttpClient`, or account lifecycle/mutation methods in `BlazorShop.Storefront.V2` and `BlazorShop.Storefront.V2.WASM`.
- [x] `BlazorShop.Storefront.V2/Components/Catalog/ProductCard.razor` is a pure pass-through to `StorefrontProductSummaryCard`.
- [x] `BlazorShop.Storefront.V2/Components/Catalog/ProductGrid.razor` renders `ProductCard` and duplicates `StorefrontProductSummaryGrid` layout/empty-state behavior.
- [x] `BlazorShop.Storefront.V2/Components/Catalog/StorefrontProductSummaryGrid.razor` is the canonical V2 product summary grid and includes semantic hooks.
- [x] `BlazorShop.Storefront.V2/Pages/Product/V2ProductPageView.razor` still uses `ProductGrid` for related products.
- [x] `StorefrontAccountApp.razor` still owns final copy such as `Customer account`, route titles, and unknown-section text directly in Razor.
- [x] V2.WASM wrapper inventory currently includes:
  - [x] `Cart/StorefrontCartSection.razor`
  - [x] `Checkout/StorefrontCheckoutSection.razor`
  - [x] `Catalog/StorefrontDiscountedProductRailSection.razor`
  - [x] `Content/StorefrontContactFormSection.razor`
  - [x] `System/StorefrontHybridRuntimeProbeSection.razor`
  - [x] `Account/StorefrontAccountApp.razor`
  - [x] `Account/StorefrontAccountNavigation.razor`
- [x] Current project graph matches the approved mode split:
  - [x] `Components` has no project references.
  - [x] `Components.Primitives -> Components`.
  - [x] `Components.Ssr -> Components + Presentation`.
  - [x] `Components.WasmHost -> Components + Browser`.
  - [x] `V2.WASM -> Browser + Components + Primitives + WasmHost`.
- [x] Active QA file `docs/refactor-control-Commerce-storefront/QA-StorefrontV2.todo.md` is large and includes historical execution notes.
- [x] No `docs/refactor-control-Commerce-storefront/archive/QA-StorefrontV2-History.md` file is currently present.

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

- [x] No Browser controller injection.
- [x] No direct Commerce Node or Storefront API transport.
- [x] No reusable browser lifecycle/mutation logic.
- [x] Approved page/composition `@rendermode` remains in V2 pages only.
- [x] Approved theme JavaScript/CSS behavior remains V2-owned.

Expected V2.WASM result:

- [x] Thin wrappers may render WasmHost/primitive components with V2 labels/classes/actions.
- [x] Account app may route/select panels and render account navigation.
- [x] No account leaf lifecycle/mutation methods.
- [x] No direct `HttpClient`.
- [x] No direct `/api/storefront/*`.
- [x] No Presentation, Runtime, Client, Commerce Node, Control Plane, or backend references.

Special account review:

- [x] `StorefrontAccountApp` has no `IStorefrontBrowserAccountController`.
- [x] `StorefrontAccountNavigation` has no browser controller injection.
- [x] `StorefrontAccountApp` has no account leaf lifecycle methods.
- [x] `StorefrontAccountNavigation` has no account mutation methods.
- [x] `StorefrontAccountApp` still owns composition and route/panel selection only.
- [x] `StorefrontAccountNavigation` still owns render-only navigation.

Decision:

- [x] If all checks pass, mark extraction loop closed and continue as cleanup only.
- [x] If a new leak is found, document the exact file, dependency, and behavior.
- [x] Fix only the smallest boundary violation needed.
- [x] Do not open another broad extraction phase without a separate approved plan.

### Phase 1 Evidence (2026-08-11)

- The V2 scan found no Browser controller, direct Commerce Node/Storefront transport, or reusable browser lifecycle/mutation logic. The only `localhost` matches are allowed development configuration and launch settings.
- All nine `@rendermode="InteractiveWebAssembly"` sites are V2 page/composition sites. V2's application head and scripts load the approved theme assets.
- V2.WASM references only Browser, Components, Components.Primitives, and Components.WasmHost. Its source scan has no forbidden runtime/project dependency and no direct transport.
- `StorefrontAccountApp` and `StorefrontAccountNavigation` have no controller/lifecycle/mutation match; the app selects the route panel and navigation renders links. The controller lifecycle/mutations remain in the five approved WasmHost account leaves.
- Decision: no new leak was found. The extraction loop is closed; remaining work is cleanup only.

## Phase 2 - Source Cleanup

Goal: remove proven-dead wrappers and stale imports without changing visible behavior.

### Phase 2A - Remove `ProductCard`

Current evidence:

- [x] `ProductCard.razor` only renders `StorefrontProductSummaryCard`.
- [x] It supplies `ProductSummaryCardVisuals.Labels` and `ProductSummaryCardVisuals.Classes`.
- [x] It has only an `Item` parameter.

Tasks:

- [x] Find all active `ProductCard` usages.
- [x] Replace each usage with direct `StorefrontProductSummaryCard`.
- [x] If the usage is inside a grid, prefer `StorefrontProductSummaryGrid` instead of hand-duplicating grid markup.
- [x] Preserve `ProductSummaryCardVisuals.Labels`.
- [x] Preserve `ProductSummaryCardVisuals.Classes`.
- [x] Preserve product card semantic hooks from the primitive.
- [x] Delete `BlazorShop.Storefront.V2/Components/Catalog/ProductCard.razor`.
- [x] Remove obsolete namespace imports.
- [x] Update tests that still read the removed path.
- [x] Remove dead CSS selectors only if they were proven wrapper-specific.

Exit criteria:

- [x] No active source file references `<ProductCard`.
- [x] No active test reads `V2/Components/Catalog/ProductCard.razor` except a deliberate retired-path assertion.
- [x] Direct product card rendering still uses `StorefrontProductSummaryCard`.

### Phase 2B - Consolidate `ProductGrid`

Current evidence:

- [x] `ProductGrid.razor` renders a grid of `ProductCard`.
- [x] `StorefrontProductSummaryGrid.razor` renders the same grid shape directly with `StorefrontProductSummaryCard`.
- [x] `StorefrontProductSummaryGrid.razor` owns semantic hooks `data-storefront-product-summary-grid` and `data-storefront-product-summary-empty`.
- [x] `V2ProductPageView.razor` uses `ProductGrid` for related products.

Tasks:

- [x] Find all active `ProductGrid` usages.
- [x] Replace `ProductGrid` usages with `StorefrontProductSummaryGrid`.
- [x] Preserve `Items` binding.
- [x] Preserve `EmptyMessage`.
- [x] Preserve related-products layout intent.
- [x] Ensure related products now get `data-storefront-product-summary-grid`.
- [x] Delete `BlazorShop.Storefront.V2/Components/Catalog/ProductGrid.razor` if no unique responsibility remains.
- [x] Update tests that still read or assert `ProductGrid`.
- [x] Update historical docs only if active docs claim `ProductGrid` is current V2 canonical behavior.
- [x] Do not touch Starter `ProductGrid` in this phase.

Exit criteria:

- [x] One active V2 product summary grid implementation remains.
- [x] V2 uses `StorefrontProductSummaryGrid` for category, search, deals, and related products.
- [x] No active V2 source references `<ProductGrid`.

### Phase 2C - Account Composition Copy Cleanup

Current evidence:

- [x] `StorefrontAccountApp.razor` correctly stays in V2.WASM.
- [x] `StorefrontAccountNavigation.razor` correctly stays in V2.WASM.
- [x] `StorefrontAccountViewOptions.cs` already owns leaf labels and final classes.
- [x] `StorefrontAccountApp.razor` still contains final copy directly in Razor.

Tasks:

- [x] Keep `StorefrontAccountApp.razor` in V2.WASM.
- [x] Keep `StorefrontAccountNavigation.razor` in V2.WASM.
- [x] Keep `StorefrontAccountShellClasses` in V2.WASM.
- [x] Keep `AccountNavigationClasses` in V2.WASM.
- [x] Add a V2-owned account app label structure if useful, for example `StorefrontAccountAppLabels`.
- [x] Or add a clearly named account app labels section inside `StorefrontAccountViewOptions`.
- [x] Move final account app copy from Razor into V2-owned options:
  - [x] `Customer account`.
  - [x] `Profile`.
  - [x] `Addresses`.
  - [x] `Orders`.
  - [x] `Receipt`.
  - [x] `Order`.
  …5938 tokens truncated…ped according to current rules.
- [x] Payment method uses COD or sandbox fixture.
- [x] Place-order sends exactly one mutation per submit.
- [x] Redirect/result is correct.
- [x] Cart clears/closes where expected.
- [x] Order reference appears.
- [x] No direct Commerce Node browser request.
- [x] No console/page errors.

### Phase 5F - Canonical Journey 4: Account

Flow:

```text
login -> profile -> address create/update/default/delete -> orders -> order detail -> invalid password validation
```

Assert:

- [x] Login works with QA account.
- [x] Profile loads.
- [x] Profile update works and can be restored if needed.
- [x] Address create works with a unique QA marker.
- [x] Address update works.
- [x] Default billing/shipping works if supported.
- [x] Address delete removes the QA marker.
- [x] Orders list loads.
- [x] Order detail loads from a list link.
- [x] Invalid password validation returns visible error and does not log out.
- [x] Password rotation is not required unless fixture management is safe.
- [x] Desktop full behavior passes.
- [x] Mobile account navigation and one critical form interaction pass.

### Phase 5G - Canonical Journey 5: Content And Security

Flow:

```text
content page -> consent save -> consent revoke -> auth redirect -> 404
```

Assert:

- [x] Content standard page renders.
- [x] Policy/FAQ/support page renders if present in fixture.
- [x] Consent save works.
- [x] Consent revoke/change works.
- [x] Auth redirect protects account routes.
- [x] Unknown route returns the expected not-found UI.
- [x] Optional service-unavailable/maintenance page only if deterministic.
- [x] No console/page errors.

### Phase 5H - Canonical Journey 6: SEO, Network, And Runtime

Verify:

- [x] `robots.txt`.
- [x] sitemap.
- [x] canonical/meta tags.
- [x] noindex rules for cart/checkout/account/internal search if applicable.
- [x] same-origin browser requests.
- [x] no direct Commerce Node.
- [x] no `/_blazor`.
- [x] no WebSocket UI transport.
- [x] no EventSource UI transport.
- [x] no unexpected 5xx.
- [x] no unexpected console errors.
- [x] no page errors.

### Phase 5I - Mobile QA Strategy

Desktop:

- [x] Full functional E2E for all six canonical journeys.

Mobile:

- [x] Responsive visual plus critical interaction checks.

Mobile surfaces:

- [x] Header/menu.
- [x] Catalog grid.
- [x] Product purchase.
- [x] Cart.
- [x] Checkout form.
- [x] Account navigation.
- [x] Content page.
- [x] Consent/toast.

Rule:

- [x] Do not duplicate every CRUD branch on mobile unless a mobile-specific bug is found.

### Phase 5J - Visual Screenshot Matrix

Capture:

- [x] Desktop.
- [x] Tablet if tooling/time allows.
- [x] Mobile.

Minimum pages:

- [x] Home.
- [x] Category.
- [x] Search.
- [x] Product.
- [x] Cart.
- [x] Checkout.
- [x] Account.
- [x] Content.

Additional states:

- [x] Empty cart.
- [x] Blocked/unavailable product if deterministic.
- [x] Toast.
- [x] Consent.
- [x] Account address cards.
- [x] Order detail.
- [x] Payment result.

Rule:

- [x] Use screenshots for visual review, not brittle pixel-perfect automation unless stable baseline tooling already exists.

### Phase 5 Completion Record

The active checklist was split from history without discarding evidence: `QA-StorefrontV2.todo.md` is the runnable release gate and `archive/QA-StorefrontV2-History.md` retains the previous 922-line execution diary, including Phase 3.1–3.6, V2F6/V2F7/V2F8, screenshot paths, timestamps, and historic order references.

Current runtime proof: `storefront-browser-action-boundary-proof.js` passed against `qa-seo-media-product` (one same-origin cart mutation, badge feedback, no direct Commerce request); `storefront-order-email-e2e.js` passed, placed COD order `ORD-20260811-F3FBE66B`, verified one confirmation email, retry recovery, and 0 browser 5xx; `storefront-registration-policy-e2e.js` passed and restored the original policy. Mobile Playwright QA authenticated the QA customer, rendered account, content, FAQ, `robots.txt`, sitemap, and the expected HTTP 404 unknown route with 0 direct Commerce, circuit, 5xx, or unexpected browser errors (known WASM fetch-abort messages caused by deliberate rapid navigation were filtered). Consent save was separately exercised under a temporary enabled configuration and the configuration was restored; its same-origin `GET /api/consent/current` and `POST /api/consent` both returned 200.

Visual evidence includes the Phase 0 desktop/mobile matrix for all listed surfaces, current checkout/order evidence under `output/playwright/phase37-order-email-e2e`, policy evidence under `output/playwright/phase37-registration-policy-e2e`, and a tablet home capture at `.playwright-cli/page-2026-08-11T12-57-39-731Z.png`. Visual review is intentionally screenshot-based rather than pixel-diff based.

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

- [x] No cycles.
- [x] No Presentation reachable from V2.WASM.
- [x] No Runtime/Client/backend reachable from reusable packages.
- [x] No Control Plane reachable from Storefront components.
- [x] No `Web.SharedV2` business contracts in reusable component packages.
- [x] No physical `Components.Hybrid` project returns.
- [x] No `Features` folder returns.

### Phase 6B - Render Mode Re-Audit

Confirm:

- [x] no reusable package owns `@rendermode`;
- [x] no `InteractiveServer`;
- [x] no `InteractiveAuto`;
- [x] only approved V2 files own `InteractiveWebAssembly` placement.

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

- [x] passed count;
- [x] failed count;
- [x] removed test count;
- [x] new consolidated test count;
- [x] known skips.

Goal:

- [x] Maximum useful invariant coverage with minimum duplication.

### Phase 6D - Focused Visual And Component Gate

Run focused component tests for:

- [x] Product summary.
- [x] Product detail.
- [x] Product gallery.
- [x] Product purchase panel.
- [x] Pagination.
- [x] Breadcrumb.
- [x] Catalog filter.
- [x] Consent.
- [x] Toast.
- [x] Cart.
- [x] Checkout.
- [x] Account leaves.

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

- [x] build result;
- [x] warnings;
- [x] known unrelated warnings;
- [x] test passed count;
- [x] test skipped count;
- [x] test failed count;
- [x] `git diff --check` result.

Rules:

- [x] A lower raw test count is acceptable only if duplicate tests were consolidated and invariant coverage remains documented.
- [x] Do not close with failing architecture tests.
- [x] Do not close with failing browser-boundary tests.

### Phase 6F - Final Browser Gate

Run all six canonical browser journeys:

- [x] Public Catalog.
- [x] Cart.
- [x] Checkout.
- [x] Account.
- [x] Content/Security.
- [x] SEO/Network/Runtime.

Collect:

- [x] screenshots;
- [x] browser errors;
- [x] console errors;
- [x] request log;
- [x] unexpected status codes;
- [x] network guardrail report.

Closure conditions:

- [x] No direct Commerce Node browser calls.
- [x] No `/_blazor` public server UI circuit.
- [x] No unexpected console errors.
- [x] No unexpected page errors.
- [x] Place-order works with COD/sandbox fixture.
- [x] Account flow works with QA account.
- [x] Desktop and mobile checks pass according to Phase 5 strategy.

### Phase 6G - Documentation Cleanup

Update:

- [x] `BlazorShop.PresentationV2/COMPONENT-MODES.md`.
- [x] `docs/architecture/03-runtime-boundaries.md`.
- [x] `docs/architecture/05-project-and-folder-guide.md`.
- [x] `docs/architecture/10-v2-contract-ownership.md`.
- [x] `docs/refactor-control-Commerce-storefront/QA-StorefrontV2.todo.md`.
- [x] `docs/refactor-control-Commerce-storefront/archive/QA-StorefrontV2-History.md`.
- [x] This plan file.

Document final state:

- [x] Phase 3 extraction closed.
- [x] V2 is visual/theme host.
- [x] V2.WASM is visual/browser host composition.
- [x] Shared component packages own reusable implementation.
- [x] ProductCard/ProductGrid decision.
- [x] Wrapper retention/removal decisions.
- [x] Active QA release gate.
- [x] Historical QA evidence archived.

### Phase 6H - Closure Report

Write final report section in this plan or a sibling closure file with:

- [x] final project graph;
- [x] final V2/V2.WASM responsibility split;
- [x] components extracted across Phase 3.1 to 3.6;
- [x] components intentionally retained;
- [x] dead wrappers removed;
- [x] wrappers intentionally retained;
- [x] visual debt fixed;
- [x] CSS/JS cleanup summary;
- [x] tests removed/merged;
- [x] final test counts;
- [x] final browser journeys;
- [x] network evidence;
- [x] known warnings/skips;
- [x] remaining non-Phase-3 debt.

### Phase 6 Completion Record

**PHASE 3 - V2 COMPONENT EXTRACTION AND VISUAL CLEANUP: CLOSED**

Final graph is unchanged from the approved architecture: Components is contracts/headless only; Primitives references Components; SSR references Components + Presentation; WasmHost references Components + Browser; V2.WASM composes Browser/Components/Primitives/WasmHost; V2 is the Presentation-backed visual/theme host. The component-mode and render-mode architecture suites prove no cycles, no Presentation reachability from V2.WASM, no reusable render-mode directive, and no `InteractiveServer`/`InteractiveAuto` surface.

Phase 3.1–3.6 extracted reusable product summary, gallery/purchase, navigation, consent/toast, cart/checkout, and account leaves. `ProductCard` and `ProductGrid` were deleted because `StorefrontProductSummaryCard`/`StorefrontProductSummaryGrid` provide the canonical hooks and empty state. Meaningful V2 wrappers remain only where they supply final V2 options, labels, classes, template slots, or page composition. `StorefrontAccountApp` and `StorefrontAccountNavigation` stay V2.WASM; five stateful leaves stay WasmHost.

Final verification: `dotnet build BlazorShop.sln --no-restore` passed with **0 errors** and **11 baseline warnings** (MessagePack `NU1902`/`NU1903` plus Browserslist); `dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-restore` passed **1957**, skipped **2** pre-existing tests, failed **0**. The focused ownership suite passed 75/75 after the deliberate removal of duplicated tests. `git diff --check` passed.

Final browser proof is recorded in Phase 5: public catalog/cart, COD checkout, authenticated account, content/security, SEO/network, and responsive desktop/mobile/tablet checks. It found no direct Commerce Node request, `/_blazor` circuit, unexpected 5xx, page error, or unexpected console error. The only filtered messages are known WASM fetch-abort noise created by intentionally navigating before downloads complete. COD proof created `ORD-20260811-F3FBE66B`; the test restored the email/settings changes it made.

Documentation audit: `COMPONENT-MODES.md` and architecture documents 03, 05, and 10 already describe the final ownership split and required no semantic revision. The active QA gate was rewritten; its full historical execution diary is archived. Remaining non-Phase-3 debt: update MessagePack and Browserslist independently of this refactor.

## Suggested Commit Breakdown

Use small commits so cleanup remains reviewable:

1. [x] `docs(storefront): record phase 3.7 final cleanup plan`
2. [x] `refactor(storefront): remove redundant product card and grid wrappers`
3. [x] `refactor(storefront): consolidate v2 account presentation copy`
4. [x] `refactor(storefront): clean v2 wasm wrappers imports and dead source`
5. [x] `fix(storefront): resolve phase 3 visual and responsive debt`
6. [x] `test(storefront): consolidate component architecture ownership tests`
7. [x] `test(storefront): trim duplicated runtime and visual contract assertions`
8. [x] `qa(storefront): split storefront v2 release gate from history`
9. [x] `qa(storefront): run final browser visual and network proof`
10. [x] `docs(storefront): close phase 3 component extraction`

Do not make a single giant cleanup commit unless the implementation is purely documentation.

## Risk Register

- [x] Risk: visual sweep becomes a redesign.
  - Mitigation: require screenshot evidence and classify cosmetic redesign as deferred.
- [x] Risk: deleting tests removes real boundary coverage.
  - Mitigation: build an invariant ownership matrix before deletion.
- [x] Risk: removing `ProductGrid` loses semantic hooks or empty-state behavior.
  - Mitigation: migrate to `StorefrontProductSummaryGrid`, not hand-written markup.
- [x] Risk: removing wrappers hides where V2 final labels/classes come from.
  - Mitigation: keep wrappers with meaningful V2 options/composition responsibility.
- [x] Risk: active QA history is lost.
  - Mitigation: archive historical execution evidence before shrinking the active file.
- [x] Risk: Playwright release gate becomes too broad and slow.
  - Mitigation: desktop full E2E, mobile critical interaction/visual checks only.
- [x] Risk: new architecture leak is discovered late.
  - Mitigation: document exact file and fix smallest boundary only; do not reopen broad extraction.

## Definition Of Done - Source Cleanup

- [x] `ProductCard` removed if no meaningful responsibility remains.
- [x] `ProductGrid` removed or consolidated if redundant.
- [x] No dead V2.WASM runtime leaf remains.
- [x] No dead wrapper remains.
- [x] No obsolete imports/namespaces remain.
- [x] No orphan CSS from deleted wrappers.
- [x] No orphan JS selector/hook.
- [x] All retained wrappers have a reason.

## Definition Of Done - Account

- [x] `StorefrontAccountApp` stays V2.WASM.
- [x] `StorefrontAccountNavigation` stays V2.WASM.
- [x] Neither injects Browser controllers.
- [x] Neither owns hydration/mutation methods.
- [x] Final Account app copy is consolidated into V2-owned options/labels where useful.
- [x] Five Account runtime leaves remain WasmHost-owned.

## Definition Of Done - Visual

- [x] Product Summary debt reviewed and fixed or explicitly deferred.
- [x] Shell reviewed desktop/mobile.
- [x] Catalog reviewed desktop/mobile.
- [x] Product Detail reviewed desktop/mobile.
- [x] Cart reviewed desktop/mobile.
- [x] Checkout reviewed desktop/mobile.
- [x] Auth/Account reviewed desktop/mobile.
- [x] Content/Payment/Error states reviewed.
- [x] No known Phase 3 visual regression remains unexplained.

## Definition Of Done - Tests

- [x] One authoritative test owner exists per architecture invariant.
- [x] RuntimeFoundation trimmed to foundation concerns.
- [x] RequiredVisualContracts trimmed to root-contract concerns.
- [x] Duplicate render-mode tests removed.
- [x] Brittle visual-neutrality inventory tests removed or justified.
- [x] VisualSourceOwnership uses dynamic source enumeration where appropriate.
- [x] Stale path assertions removed.
- [x] Component behavior and semantic coverage retained.
- [x] Security and browser-boundary tests retained.
- [x] Raw test-count reduction documented and accepted.

## Definition Of Done - QA

- [x] Active QA file contains current release gate only.
- [x] Historical phase evidence archived.
- [x] Six canonical browser journeys defined.
- [x] Global network/error collector preferred.
- [x] Mobile suite reduced to visual/critical interaction checks.
- [x] Low-level API negatives not redundantly replayed in Playwright.
- [x] Final screenshots recorded.

## Definition Of Done - Architecture

- [x] Components has no project references.
- [x] Primitives references Components only.
- [x] SSR references Components + Presentation only.
- [x] WasmHost references Components + Browser only.
- [x] V2.WASM cannot reach Presentation/Runtime/Client/backend.
- [x] No project cycles.
- [x] No reusable `@rendermode`.
- [x] No `InteractiveServer`.
- [x] No `InteractiveAuto`.
- [x] No browser direct Commerce Node.
- [x] No `/_blazor` server UI circuit.

## Final Closure Gate

Only close Phase 3 when:

- [x] source cleanup passes;
- [x] visual sweep passes;
- [x] CSS/JS cleanup passes;
- [x] test consolidation passes;
- [x] QA consolidation passes;
- [x] focused tests pass;
- [x] full build passes;
- [x] full V2 tests pass;
- [x] browser journeys pass;
- [x] network guardrail passes;
- [x] desktop/mobile visual checks pass;
- [x] docs are updated;
- [x] closure report is written.

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

- [x] more abstractions;
- [x] more tests;
- [x] more QA checkboxes;
- [x] more wrappers.

Success is:

- [x] clear ownership;
- [x] fewer duplicated tests;
- [x] fewer dead components;
- [x] fewer stale QA cases;
- [x] cleaner V2/V2.WASM;
- [x] resolved concrete visual debt;
- [x] repeatable release proof.

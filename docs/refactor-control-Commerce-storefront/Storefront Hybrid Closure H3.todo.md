# Storefront Hybrid Closure H3

Branch: `Hybrid-Architecture`

Status: in progress

Predecessor:

- `Storefront Hybrid Architecture Clarification.todo.md`
- `Storefront Component Mode Foundation v2.todo.md`
- `Storefront Reference Component MVP.todo.md`

Successor:

- Phase 3 - V2 Component Extraction

Primary goal: close the Storefront Hybrid architecture after H2 evidence, retire the transitional `BlazorShop.Storefront.Components.Hybrid` project if no active consumer remains, and lock durable guardrails so future component extraction cannot drift back to the old nested Hybrid bridge model.

This is a closure/refactor phase. It must not rewrite Storefront architecture, Commerce Node behavior, Control Plane behavior, checkout/order/payment logic, Starter generation, or AI StorefrontBuilder output.

## Current Codebase Facts

These facts were verified from the current repository before writing this plan.

- H2 Component MVP evidence is already recorded in `BlazorShop.PresentationV2/COMPONENT-MODES.md` and `docs/refactor-control-Commerce-storefront/QA-StorefrontV2.todo.md`.
- The canonical H2 Hybrid runtime proof is `StorefrontHybridRuntimeProbe` in `BlazorShop.Storefront.Components.WasmHost/System`, with semantic descriptor mode `Hybrid`.
- `/__qa/component-mvp` is Presentation-owned in `BlazorShop.Storefront.Presentation/Pages/Ssr/System/ComponentMvpRoutePage.razor`.
- `StorefrontComponentMvpLab` is V2-owned visual markup and applies `@rendermode="InteractiveWebAssembly"` to V2.WASM wrapper components.
- Visible contact currently uses `BlazorShop.Storefront.V2.WASM/Components/Content/StorefrontContactFormSection.razor`.
- Visible contact route currently renders `StorefrontContactFormSection @rendermode="InteractiveWebAssembly"` from `BlazorShop.Storefront.V2/Pages/Ssr/Content/StorefrontPage.razor`.
- `BlazorShop.Storefront.Components.Hybrid` still exists in the solution and test project.
- `BlazorShop.Storefront.Components.Hybrid` currently contains only:
  - `_Imports.razor`
  - `BlazorShop.Storefront.Components.Hybrid.csproj`
  - `README.md`
  - `Content/StorefrontContactForm.razor`
  - `Content/StorefrontContactFormDescriptor.cs`
- The historical Hybrid bridge `Content/StorefrontContactForm.razor` still owns an internal `@rendermode="InteractiveWebAssembly"` placement around `StorefrontContactFormApp`.
- `StorefrontContactFormDescriptor` still publishes key `contact-form`, mode `Hybrid`, category `Content`, component type `StorefrontContactForm`.
- `StorefrontComponentDescriptorTests` still imports `BlazorShop.Storefront.Components.Hybrid.Content`, expects the Hybrid descriptor path, and resolves assembly names from mode project paths.
- `StorefrontComponentModeDependencyTests` still includes the `Components.Hybrid` csproj in mode project scans and expects the transitional dependency graph.
- `StorefrontComponentVisualNeutralityTests`, `StorefrontIndependenceBoundaryTests`, and source-reference tests still mention `Components.Hybrid`.
- `Contracts.System` exists under `BlazorShop.Storefront.Components/Contracts/System` and is imported by V2.WASM and WasmHost. This namespace previously caused direct `System.*` shadowing.
- H2 browser network proof records no direct Commerce browser calls, no public `/_blazor` server UI circuit, and no credential leaks.

## Canonical H3 Decision

`Hybrid` is a semantic runtime classification:

```text
useful server-produced or prerendered HTML
  + InteractiveWebAssembly hydration
  + browser-side C# interaction
  + optional lightweight progressive enhancement
```

`Hybrid` is not:

```text
InteractiveServer
InteractiveAuto
SignalR public storefront UI circuit
WebSocket-owned component UI state
mandatory Components.Hybrid project
mandatory server shell wrapping a WasmHost child
```

Preferred H3 end state:

```text
BlazorShop.Storefront.Components.Hybrid
  -> removed from active solution

StorefrontContactFormApp
  -> remains in Components.WasmHost

contact-form descriptor
  -> moves to Components.WasmHost.Content
  -> Mode = Hybrid
  -> ComponentType = StorefrontContactFormApp

Visible contact
  -> V2 route composition
  -> V2.WASM StorefrontContactFormSection
  -> Components.WasmHost StorefrontContactFormApp
  -> Browser contact controller
  -> same-origin /api/contact
```

Fallback H3 end state is allowed only if a real consumer blocks removal:

```text
BlazorShop.Storefront.Components.Hybrid
  -> compatibility-only
  -> no new components
  -> no new descriptors
  -> no route/render-mode expansion
  -> blocker documented with owner and removal condition
```

## Not In Scope

- No Commerce Node API behavior changes.
- No Control Plane behavior changes.
- No checkout, payment, order, cart, catalog, pricing, inventory, or customer account feature changes.
- No Starter migration.
- No generated storefront migration.
- No StorefrontBuilder work.
- No new component registry/runtime reflection framework.
- No new `Components.Common`, `ComponentRuntime`, or capability package unless a later phase explicitly approves it.
- No switch to `InteractiveServer` or `InteractiveAuto`.
- No direct Commerce Node browser transport.
- No broad visual redesign.

## Architecture Target

```text
Presentation
  owns routes, page shells, SSR page contexts, same-origin BFF endpoints

V2
  owns visual markup, final classes, copy, layout, route composition

V2.WASM
  owns host-specific browser wrappers and InteractiveWebAssembly roots

Components.Ssr
  owns reusable SSR components

Components.WasmHost
  owns reusable browser-executed components
  may contain semantic Hybrid descriptors when runtime is prerender + WASM

Components
  owns browser-safe contracts, descriptors, headless states

Browser
  owns same-origin local API primitives and Browser controllers

Runtime
  server-only BFF/runtime integration, never referenced by browser components
```

## Phase H3.0 - Baseline And Evidence Lock

Goal: prove H3 starts from a completed H2 baseline and record the exact cleanup scope before deleting anything.

Tasks:

- [x] Confirm current branch is `Hybrid-Architecture`.
- [x] Record `git status --short`.
- [x] Confirm H2 evidence is present in `BlazorShop.PresentationV2/COMPONENT-MODES.md`.
- [x] Confirm H2 evidence is present in `docs/refactor-control-Commerce-storefront/QA-StorefrontV2.todo.md`.
- [x] Confirm `/__qa/component-mvp` route exists only under `BlazorShop.Storefront.Presentation`.
- [x] Confirm `StorefrontHybridRuntimeProbe` exists in `Components.WasmHost/System`.
- [x] Confirm `StorefrontHybridRuntimeProbeDescriptor` has `Mode = StorefrontComponentMode.Hybrid`.
- [x] Confirm visible contact route uses V2.WASM `StorefrontContactFormSection`, not the historical `Components.Hybrid` shell.
- [x] Inventory all source references to:
  - [x] `BlazorShop.Storefront.Components.Hybrid`
  - [x] `StorefrontContactForm`
  - [x] `StorefrontContactFormDescriptor`
  - [x] `StorefrontContactFormAppDoesNotPublishPublicDescriptor`
  - [x] `Components/Contracts/System`
  - [x] `InteractiveServer`
  - [x] `InteractiveAuto`
  - [x] `/_blazor`
  - [x] `HubConnection`
  - [x] `WebSocket`
  - [x] direct Commerce Node URLs or `api/storefront/stores`
- [x] Record current `BlazorShop.Storefront.Components.Hybrid` source file list excluding `bin` and `obj`.
- [x] Record current test files that must change if Hybrid project is removed.

Suggested commands:

```powershell
git branch --show-current
git status --short
rg -n "BlazorShop.Storefront.Components.Hybrid|StorefrontContactFormDescriptor|StorefrontContactFormAppDoesNotPublishPublicDescriptor|Contracts.System|InteractiveServer|InteractiveAuto|/_blazor|HubConnection|WebSocket|api/storefront/stores" BlazorShop.PresentationV2 BlazorShop.Tests.V2 docs -g "!*bin*" -g "!*obj*"
Get-ChildItem -Recurse BlazorShop.PresentationV2\BlazorShop.Storefront.Components.Hybrid -File | Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' }
```

Exit criteria:

- [x] No H3 implementation starts before the inventory is complete.
- [x] Every known consumer of the transitional Hybrid project is classified.
- [x] H2 evidence is treated as source of truth, not recreated by guessing.

Implementation notes:

- 2026-08-10: branch is `Hybrid-Architecture`.
- 2026-08-10: initial status showed only this H3 plan file as untracked.
- 2026-08-10: H2 evidence is present in `COMPONENT-MODES.md` under `H2 Runtime Proof Evidence` and in `QA-StorefrontV2.todo.md` under `Component MVP Runtime Proof`.
- 2026-08-10: `/__qa/component-mvp` route is declared only by `BlazorShop.Storefront.Presentation/Pages/Ssr/System/ComponentMvpRoutePage.razor`.
- 2026-08-10: `StorefrontHybridRuntimeProbe` and `StorefrontHybridRuntimeProbeDescriptor` live in `BlazorShop.Storefront.Components.WasmHost/System`, and the descriptor uses `StorefrontComponentMode.Hybrid`.
- 2026-08-10: visible content page contact composition uses `BlazorShop.Storefront.V2.WASM/Components/Content/StorefrontContactFormSection.razor`, rendered from V2 `Pages/Ssr/Content/StorefrontPage.razor` with `@rendermode="InteractiveWebAssembly"`. The visible route does not use the historical `Components.Hybrid.Content.StorefrontContactForm` shell.
- 2026-08-10: `Components.Hybrid` source inventory excluding `bin`/`obj`: `_Imports.razor`, `BlazorShop.Storefront.Components.Hybrid.csproj`, `README.md`, `Content/StorefrontContactForm.razor`, and `Content/StorefrontContactFormDescriptor.cs`.
- 2026-08-10: test/project consumers to migrate before removal include `BlazorShop.Tests.V2.csproj`, `StorefrontComponentDescriptorTests`, `StorefrontComponentModeDependencyTests`, `StorefrontComponentModeFoundationTests`, `StorefrontComponentModeBoundaryValidator`, `StorefrontComponentModeBoundaryValidatorTests`, `StorefrontContactFormComponentTests`, `StorefrontComponentVisualNeutralityTests`, `StorefrontIndependenceBoundaryTests`, `StorefrontVisualOnlyBoundaryTests`, and `BlazorShop.sln`.
- 2026-08-10: `Contracts.System` active references are in `Components/Contracts/System`, WasmHost and V2.WASM imports, and `StorefrontHybridRuntimeProbeComponentTests`; these are H3.8 rename targets.

## Phase H3.1 - Move Contact Public Descriptor Out Of Components.Hybrid

Goal: keep the semantic `contact-form` descriptor while removing its dependency on the historical Hybrid shell.

Decision:

- Move public `contact-form` descriptor ownership from `Components.Hybrid.Content.StorefrontContactFormDescriptor` to `Components.WasmHost.Content`.
- Descriptor remains semantic `Hybrid`.
- Descriptor should point to `StorefrontContactFormApp` unless implementation reveals a stronger reason to keep a wrapper component.
- Do not make V2.WASM wrapper a reusable descriptor target because V2.WASM is host-specific and owns V2 copy/classes.

Tasks:

- [ ] Create or move `StorefrontContactFormDescriptor` into `BlazorShop.Storefront.Components.WasmHost/Content`.
- [ ] Set descriptor key to `contact-form`.
- [ ] Set descriptor mode to `StorefrontComponentMode.Hybrid`.
- [ ] Set descriptor category to `StorefrontComponentCategory.Content`.
- [ ] Set descriptor component type to `typeof(StorefrontContactFormApp)`.
- [ ] Remove `using BlazorShop.Storefront.Components.Hybrid.Content` from descriptor tests.
- [ ] Update descriptor inventory expected path from `Components.Hybrid/Content/StorefrontContactFormDescriptor.cs` to `Components.WasmHost/Content/StorefrontContactFormDescriptor.cs`.
- [ ] Update descriptor tests that currently expect `typeof(StorefrontContactForm)` to expect `typeof(StorefrontContactFormApp)`.
- [ ] Re-evaluate and rename `StorefrontContactFormAppDoesNotPublishPublicDescriptor`; after this phase, the app is the public descriptor target, so the old assertion is obsolete.
- [ ] Add a new assertion that no descriptor points at V2.WASM `StorefrontContactFormSection`.
- [ ] Add a new assertion that no descriptor points at deleted/compatibility `Components.Hybrid` types.
- [ ] Keep existing Browser controller and same-origin `/api/contact` behavior unchanged.

Files expected to change:

- `BlazorShop.PresentationV2/BlazorShop.Storefront.Components.WasmHost/Content/StorefrontContactFormDescriptor.cs`
- `BlazorShop.PresentationV2/BlazorShop.Storefront.Components.Hybrid/Content/StorefrontContactFormDescriptor.cs`
- `BlazorShop.Tests.V2/PresentationV2/Storefront/StorefrontComponentDescriptorTests.cs`
- Any source-reference tests that name the old descriptor path.

Exit criteria:

- [ ] `rg -n "StorefrontContactFormDescriptor" BlazorShop.PresentationV2 BlazorShop.Tests.V2` shows the descriptor under WasmHost, not Hybrid.
- [ ] Public descriptor inventory has no dependency on `Components.Hybrid`.
- [ ] Contact descriptor still validates as semantic `Hybrid`.
- [ ] Visible V2 contact route remains unchanged.

## Phase H3.2 - Remove Historical Contact Shell

Goal: remove the unused historical shell that still owns `@rendermode` inside a reusable library.

Tasks:

- [ ] Confirm no production source references `BlazorShop.Storefront.Components.Hybrid.Content.StorefrontContactForm`.
- [ ] Delete `BlazorShop.Storefront.Components.Hybrid/Content/StorefrontContactForm.razor`.
- [ ] Remove tests that inspect the deleted historical bridge.
- [ ] Replace historical bridge tests with tests for the current visible V2.WASM wrapper path:
  - [ ] `StorefrontContactFormSection` wraps `StorefrontContactFormApp`.
  - [ ] `StorefrontContactFormSection` does not own `@rendermode`.
  - [ ] V2 `StorefrontPage.razor` owns `@rendermode="InteractiveWebAssembly"` placement.
  - [ ] V2.WASM wrapper supplies V2 labels/classes/action descriptor.
  - [ ] Wrapper action stays same-origin `/api/contact`.
- [ ] Keep `StorefrontContactFormApp` component tests for validation, submit request, success, failure, and Browser controller invocation.

Do not:

- [ ] Do not move `StorefrontContactFormSection` into shared `Components`.
- [ ] Do not put V2 labels/classes into shared package.
- [ ] Do not introduce direct `HttpClient` into `StorefrontContactFormApp`.

Exit criteria:

- [ ] No reusable component file contains `@rendermode` for the old contact bridge.
- [ ] Visible contact composition remains V2 route -> V2.WASM wrapper -> WasmHost app.
- [ ] Existing contact contracts remain browser-safe.

## Phase H3.3 - Remove Components.Hybrid Project

Goal: retire the physical transitional project after descriptor and shell consumers are gone.

Tasks:

- [ ] Delete `BlazorShop.PresentationV2/BlazorShop.Storefront.Components.Hybrid/_Imports.razor`.
- [ ] Delete `BlazorShop.PresentationV2/BlazorShop.Storefront.Components.Hybrid/README.md`.
- [ ] Delete `BlazorShop.PresentationV2/BlazorShop.Storefront.Components.Hybrid/BlazorShop.Storefront.Components.Hybrid.csproj`.
- [ ] Delete the now-empty `BlazorShop.Storefront.Components.Hybrid` folder.
- [ ] Remove `BlazorShop.Storefront.Components.Hybrid` project entry from `BlazorShop.sln`.
- [ ] Remove test project reference from `BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj`.
- [ ] Remove all active build commands for `Components.Hybrid` from plan/docs/QA current gates.
- [ ] Keep historical plan files as history, but update source-of-truth docs to say the project is retired.
- [ ] Ensure no active project references `BlazorShop.Storefront.Components.Hybrid`.
- [ ] Ensure no active test imports `BlazorShop.Storefront.Components.Hybrid`.

Fallback if removal is blocked:

- [ ] Keep the project only if a real consumer remains and cannot be migrated in H3.
- [ ] Document the blocker in this plan with exact file/line and required future removal step.
- [ ] Add an architecture test that the project contains no `.razor` component except the explicitly blocked compatibility file.
- [ ] Add an architecture test that the project publishes no new descriptors.

Preferred exit criteria:

- [ ] `rg -n "BlazorShop.Storefront.Components.Hybrid" BlazorShop.PresentationV2 BlazorShop.Tests.V2 BlazorShop.sln` returns no active source/test/project references.
- [ ] Historical docs may still mention the retired project only as historical context.
- [ ] Active docs do not list `Components.Hybrid` as a current project.

## Phase H3.4 - Descriptor Discovery Decoupling

Goal: make descriptor tests reflect semantic mode rather than mode-project topology.

Current problem:

- `StorefrontComponentDescriptorTests` scans fixed mode project directories.
- It resolves assembly names from path prefixes.
- It includes `Components.Hybrid` as a physical mode project.
- This contradicts the H2 decision that semantic `Hybrid` may live in WasmHost/capability assemblies.

Tasks:

- [ ] Replace `ModeProjectDirectories` with `ReusableDescriptorSourceDirectories` or equivalent.
- [ ] Include active reusable component source directories explicitly:
  - [ ] `BlazorShop.Storefront.Components.Ssr`
  - [ ] `BlazorShop.Storefront.Components.WasmHost`
  - [ ] Future capability directories only when added by a later phase.
- [ ] Remove special path mapping for `Components.Hybrid`.
- [ ] Keep deterministic source discovery.
- [ ] Keep duplicate key validation.
- [ ] Keep descriptor semantic validation.
- [ ] Add a positive assertion that `StorefrontHybridRuntimeProbeDescriptor` is semantic `Hybrid` while physically in WasmHost.
- [ ] Add a positive assertion that `StorefrontContactFormDescriptor` is semantic `Hybrid` while physically in WasmHost.
- [ ] Add a negative fixture or source-level assertion that no test derives mode from project name.
- [ ] Do not create runtime descriptor discovery.
- [ ] Do not add DI registry for descriptors in H3.

Exit criteria:

- [ ] Descriptor tests pass without `Components.Hybrid`.
- [ ] Semantic mode and physical project are visibly decoupled.
- [ ] Future capability packaging remains possible.

## Phase H3.5 - Component Dependency Matrix Hardening

Goal: replace transitional mode-project dependency assumptions with final active package rules.

Final matrix:

```text
Components
  allowed: browser-safe contracts, descriptors, headless state
  forbidden: Presentation, Browser, Runtime, Client, V2, V2.WASM, Starter, backend/core/API, Web.SharedV2

Components.Ssr
  allowed: Components, Presentation prepared contexts/contracts
  forbidden: Browser, Runtime, Client, V2, V2.WASM, Starter, backend/core/API, HttpClient, IJSRuntime, @rendermode

Components.WasmHost
  allowed: Components, Browser, browser-safe framework APIs
  forbidden: Presentation, Runtime, Client, V2, Starter, backend/core/API, HttpContext, IHttpContextAccessor, direct HttpClient backend transport

V2
  allowed: visual markup, final CSS/classes/copy, host composition, @rendermode InteractiveWebAssembly
  forbidden: reusable package ownership, shared descriptor ownership, direct Commerce browser transport

V2.WASM
  allowed: host-specific browser wrappers, labels/classes/action descriptors for V2
  forbidden: backend/core/API, Runtime, Client, direct Commerce URLs, @page route ownership
```

Tasks:

- [ ] Update `StorefrontComponentModeDependencyTests` to remove `Components.Hybrid`.
- [ ] Add direct dependency tests for base `Components`.
- [ ] Keep SSR exact references test.
- [ ] Keep WasmHost exact references test.
- [ ] Add test that WasmHost does not reference Presentation.
- [ ] Add test that WasmHost does not reference Runtime/Client/backend/core/API projects.
- [ ] Add test that base Components does not reference Browser.
- [ ] Add test that V2.WASM does not reference Runtime/Client/backend/core/API projects.
- [ ] Add test that V2 does not reference `Components.Hybrid`.
- [ ] Update `StorefrontComponentModeBoundaryValidator` messages so they no longer mention "until H2".
- [ ] Remove old remediation text that describes `Components.Hybrid` as current dependency graph.

Exit criteria:

- [ ] Architecture dependency tests express current target, not historical transition.
- [ ] Error messages explain problem, cause, and correct destination.

## Phase H3.6 - Render Mode Ownership Guardrails

Goal: enforce that reusable components do not self-own render modes and public Storefront cannot regress to server-interactive modes.

Rules:

- Reusable SSR components must not contain `@rendermode`, `InteractiveWebAssembly`, `InteractiveServer`, or `InteractiveAuto`.
- Reusable WasmHost components must not contain `@rendermode`.
- V2/V2.WASM host composition may use `@rendermode="InteractiveWebAssembly"` only at approved boundaries.
- Public Storefront code must not use `InteractiveServer` or `InteractiveAuto`.
- Deleted `Components.Hybrid` cannot remain as an exception.

Tasks:

- [ ] Add or update source scanner tests for reusable packages.
- [ ] Scan `BlazorShop.Storefront.Components`.
- [ ] Scan `BlazorShop.Storefront.Components.Ssr`.
- [ ] Scan `BlazorShop.Storefront.Components.WasmHost`.
- [ ] Scan `BlazorShop.Storefront.V2`.
- [ ] Scan `BlazorShop.Storefront.V2.WASM`.
- [ ] Fail if any active source contains `InteractiveServer`.
- [ ] Fail if any active source contains `InteractiveAuto`.
- [ ] Fail if reusable component files contain `@rendermode`.
- [ ] Allow `@rendermode="InteractiveWebAssembly"` only in approved V2 composition files.
- [ ] Add negative fixtures in tests so the scanner proves violations are caught.
- [ ] Exclude docs and historical plan files from production-source checks.

Approved current render-mode owners:

- `BlazorShop.Storefront.V2/Pages/Ssr/Content/StorefrontPage.razor`
- `BlazorShop.Storefront.V2/Pages/Hybrid/Catalog/Home.razor`
- `BlazorShop.Storefront.V2/Pages/Hybrid/Commerce/CartPage.razor`
- `BlazorShop.Storefront.V2/Pages/Hybrid/Commerce/CheckoutPage.razor`
- `BlazorShop.Storefront.V2/Pages/WasmHost/Account/AccountHostPage.razor`
- `BlazorShop.Storefront.V2/Components/System/StorefrontComponentMvpLab.razor`

Exit criteria:

- [ ] Render mode ownership is mechanically enforced.
- [ ] No reusable component owns `@rendermode`.
- [ ] No public Storefront source uses `InteractiveServer` or `InteractiveAuto`.

## Phase H3.7 - Server-Interactive And Browser Transport Guardrails

Goal: preserve H2 runtime proof without overfailing on unrelated development tooling.

Correct network rule:

```text
fail on:
  /_blazor public UI circuit
  direct Commerce Node browser request
  direct Control Plane browser request
  node credential leak
  unexpected console/page errors

do not fail solely on:
  unrelated development-tooling WebSocket
```

Tasks:

- [ ] Keep `scripts/qa/run-storefront-component-mvp-proof.ps1`.
- [ ] Keep `scripts/qa/storefront-component-mvp-proof.js`.
- [ ] Review the Network phase classification.
- [ ] Ensure Network phase fails on `/_blazor`.
- [ ] Ensure Network phase fails on direct Commerce host/path.
- [ ] Ensure Network phase fails on credential leak.
- [ ] Ensure Network phase records WebSocket/EventSource counts for evidence.
- [ ] Do not require WebSocket count to be zero unless the recorded URL is Storefront UI/circuit related.
- [ ] Add source-level guard for `HubConnection`, `AddSignalR`, `MapHub`, `ClientWebSocket`, and `WebSocket.CreateFromStream` in public Storefront UI source.
- [ ] Scope this guard to Storefront UI packages, not the entire solution.

Exit criteria:

- [ ] H2 Network proof remains reproducible.
- [ ] Guardrail catches actual Storefront server-interactive drift.
- [ ] Guardrail does not fail on unrelated dev-tooling sockets.

## Phase H3.8 - Rename Contracts.System Namespace

Goal: remove avoidable namespace collision from the shared contracts package.

Problem:

- H2 added `BlazorShop.Storefront.Components.Contracts.System`.
- That namespace can shadow `System.*` and already forced a `global::System` workaround in descriptor validation.

Preferred target:

```text
BlazorShop.Storefront.Components.Contracts.Diagnostics
```

Tasks:

- [ ] Move `StorefrontHybridRuntimeProbeLabels.cs` from `Contracts/System` to `Contracts/Diagnostics`.
- [ ] Move `StorefrontHybridRuntimeProbeClasses.cs` from `Contracts/System` to `Contracts/Diagnostics`.
- [ ] Rename namespace to `BlazorShop.Storefront.Components.Contracts.Diagnostics`.
- [ ] Update WasmHost `_Imports.razor`.
- [ ] Update V2.WASM `_Imports.razor`.
- [ ] Update `StorefrontHybridRuntimeProbeComponentTests`.
- [ ] Update any source-reference tests that list `System/StorefrontHybridRuntimeProbe*.cs`.
- [ ] Remove unnecessary `global::System` workaround only if it becomes unnecessary and tests prove no regression.
- [ ] If keeping `global::System` improves clarity with no downside, document why it remains.

Exit criteria:

- [ ] `rg -n "Contracts.System|namespace BlazorShop.Storefront.Components.Contracts.System" BlazorShop.PresentationV2 BlazorShop.Tests.V2` returns no active source/test matches.
- [ ] H2 probe continues to build and render.

## Phase H3.9 - Visual Neutrality And Copy Ownership Recheck

Goal: ensure H3 cleanup does not move visual ownership into reusable packages.

Tasks:

- [ ] Run current visual neutrality tests.
- [ ] Update scan roots to remove `Components.Hybrid`.
- [ ] Include `Components.WasmHost/System/StorefrontHybridRuntimeProbe.razor`.
- [ ] Include `Components.WasmHost/Content/StorefrontContactFormApp.razor`.
- [ ] Confirm reusable components use host-supplied class slots only.
- [ ] Confirm V2/V2.WASM own final class strings.
- [ ] Confirm reusable packages do not own theme CSS.
- [ ] Confirm V2-specific labels/copy remain in V2/V2.WASM wrappers.
- [ ] Confirm shared contracts expose label fields, not final storefront copy as business truth.

Exit criteria:

- [ ] Visual neutrality tests pass.
- [ ] `Components.WasmHost` remains reusable and browser-safe.
- [ ] No V2 visual class string appears in base shared/reusable component implementation except tests or V2 wrappers.

## Phase H3.10 - QA Route Policy Closure

Goal: make `/__qa/component-mvp` policy explicit before merge.

Decision:

- Keep `/__qa/component-mvp` as a hidden/noindex architecture QA route.
- Do not add it to navigation, sitemap, content catalog, or page template catalog.
- Keep middleware skip only for deterministic architecture QA paths required by tests.

Tasks:

- [ ] Confirm `ComponentMvpRoutePage.razor` sets robots noindex/nofollow metadata.
- [ ] Confirm `StorefrontCurrentStoreMiddleware` skips `/__qa/component-mvp` or `/__qa/*` intentionally.
- [ ] Confirm `StorefrontPublicRedirectMiddleware` skips `/__qa/component-mvp` or `/__qa/*` intentionally.
- [ ] Confirm sitemap does not include `/__qa/component-mvp`.
- [ ] Confirm navigation/menu does not include `/__qa/component-mvp`.
- [ ] Confirm robots policy remains documented.
- [ ] Decide whether docs use broad `/__qa/*` namespace or narrow `/__qa/component-mvp`.
- [ ] If broad namespace remains, document it as internal architecture QA namespace and require noindex/not in sitemap.
- [ ] If narrow exception is chosen, update middleware/tests to match exact path.

Exit criteria:

- [ ] QA route policy is explicit.
- [ ] Browser proof route stays deterministic.
- [ ] Production-facing navigation/SEO surfaces do not expose the route.

## Phase H3.11 - Documentation Closure

Goal: update active docs so agents and developers see the final H3 architecture, not the transitional H1/H2 model.

Files to update:

- [ ] `AGENTS.md`
- [ ] `BlazorShop.PresentationV2/COMPONENT-MODES.md`
- [ ] `docs/architecture/03-runtime-boundaries.md`
- [ ] `docs/architecture/05-project-and-folder-guide.md`
- [ ] `docs/architecture/08-agent-decision-rules.md`
- [ ] `docs/architecture/10-v2-contract-ownership.md`
- [ ] `docs/refactor-control-Commerce-storefront/QA-StorefrontV2.todo.md`
- [ ] This H3 plan file with execution evidence.

Required documentation changes:

- [ ] Remove active-current wording that lists `Components.Hybrid` as a live reusable project if retired.
- [ ] Preserve historical references only in historical plan files.
- [ ] State that `Hybrid = prerender/server-produced HTML + InteractiveWebAssembly browser interactivity`.
- [ ] State that semantic component mode is independent from physical project/package.
- [ ] State that reusable components do not own `@rendermode`.
- [ ] State that V2/host composition owns `@rendermode InteractiveWebAssembly`.
- [ ] State that public Storefront must not use `InteractiveServer` or `InteractiveAuto` without a new architecture decision.
- [ ] State that public Storefront component interaction must not depend on SignalR/Blazor Server UI circuit.
- [ ] State that browser components use Browser controllers and same-origin BFF, not direct Commerce Node APIs.
- [ ] Document the retired `Contracts.System` namespace if renamed.

Exit criteria:

- [ ] Active source-of-truth docs match code after H3.
- [ ] No active doc tells an agent to create new components in `Components.Hybrid`.
- [ ] No active doc describes nested Hybrid shell as canonical.

## Phase H3.12 - Focused Build Gate

Goal: prove the affected Storefront projects compile after project removal and namespace/descriptor changes.

Run after implementation:

```powershell
dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.Components/BlazorShop.Storefront.Components.csproj --no-restore
dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.Components.Ssr/BlazorShop.Storefront.Components.Ssr.csproj --no-restore
dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.Components.WasmHost/BlazorShop.Storefront.Components.WasmHost.csproj --no-restore
dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.Browser/BlazorShop.Storefront.Browser.csproj --no-restore
dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/BlazorShop.Storefront.Presentation.csproj --no-restore
dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM/BlazorShop.Storefront.V2.WASM.csproj --no-restore
dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.V2/BlazorShop.Storefront.V2.csproj --no-restore
```

Do not run a `Components.Hybrid` build if the project is removed.

Exit criteria:

- [ ] All focused builds pass with 0 new errors.
- [ ] Any existing unrelated warnings are recorded, not hidden.

## Phase H3.13 - Focused Test Gate

Goal: prove source guardrails and component tests reflect the final H3 model.

Run focused tests:

```powershell
dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~StorefrontComponentDescriptorTests|FullyQualifiedName~StorefrontComponentModeDependencyTests|FullyQualifiedName~StorefrontComponentModeBoundaryValidatorTests|FullyQualifiedName~StorefrontComponentVisualNeutralityTests|FullyQualifiedName~StorefrontVisualOnlyBoundaryTests|FullyQualifiedName~StorefrontIndependenceBoundaryTests|FullyQualifiedName~StorefrontContactFormComponentTests|FullyQualifiedName~StorefrontBrowserContactControllerTests|FullyQualifiedName~StorefrontHybridRuntimeProbeComponentTests|FullyQualifiedName~StorefrontComponentMvpArchitectureTests|FullyQualifiedName~StorefrontComponentMvpLabTests|FullyQualifiedName~StorefrontPageCompositionGuardrailTests|FullyQualifiedName~StorefrontCurrentStoreMiddlewareTests|FullyQualifiedName~StorefrontPublicRedirectMiddlewareTests"
```

Required test coverage:

- [ ] Descriptor inventory has no `Components.Hybrid` source path.
- [ ] Contact descriptor remains semantic `Hybrid`.
- [ ] Contact descriptor target is in `Components.WasmHost`.
- [ ] `StorefrontContactFormSection` remains V2.WASM host wrapper.
- [ ] Reusable components do not own `@rendermode`.
- [ ] V2 composition owns approved `InteractiveWebAssembly` placements.
- [ ] `InteractiveServer` is forbidden in public Storefront UI source.
- [ ] `InteractiveAuto` is forbidden in public Storefront UI source.
- [ ] `Components.Hybrid` project is absent from active project references.
- [ ] `Contracts.System` namespace is absent if renamed.
- [ ] Browser controller path remains same-origin.
- [ ] `/__qa/component-mvp` route ownership remains Presentation-only.

Exit criteria:

- [ ] Focused tests pass.
- [ ] Tests fail clearly if a future agent reintroduces `Components.Hybrid` as active architecture.

## Phase H3.14 - Mandatory Browser Regression Gate

Goal: prove runtime behavior still works in a real browser after cleanup.

Component MVP proof:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/qa/run-storefront-component-mvp-proof.ps1 -Phase RawHtml -RuntimeTimeoutSeconds 90 -NoBuild
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/qa/run-storefront-component-mvp-proof.ps1 -Phase Hybrid -RuntimeTimeoutSeconds 90 -NoBuild
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/qa/run-storefront-component-mvp-proof.ps1 -Phase Rail -RuntimeTimeoutSeconds 90 -NoBuild
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/qa/run-storefront-component-mvp-proof.ps1 -Phase Network -RuntimeTimeoutSeconds 90 -NoBuild
```

Required Component MVP assertions:

- [ ] Raw HTML route returns HTTP 200.
- [ ] Raw HTML contains SSR brand logo proof.
- [ ] Raw HTML contains Hybrid prerender state.
- [ ] Browser hydration changes runtime state to `interactive`.
- [ ] C# click changes value `0 -> 1 -> 2`.
- [ ] Rail loading state appears.
- [ ] Rail success state appears.
- [ ] Rail empty state appears.
- [ ] Rail error state appears.
- [ ] Rail retry state works.
- [ ] Network proof records no public `/_blazor` UI circuit.
- [ ] Network proof records no direct Commerce browser requests.
- [ ] Network proof records no credential leaks.
- [ ] Network proof records no console errors.
- [ ] Network proof records no page errors.

Contact browser regression:

- [ ] Start local V2 runtime with existing local script or current QA runner.
- [ ] Navigate to a page that renders the contact component.
- [ ] Submit empty form and verify validation.
- [ ] Submit valid form against configured local/test store route.
- [ ] Verify success state.
- [ ] Simulate or route failure if current QA utilities support it.
- [ ] Verify retry or recoverable error state.
- [ ] Verify no direct Commerce Node browser request.
- [ ] Verify no unexpected console/page errors.

If a dedicated contact Playwright wrapper already exists, use it instead of writing an ad hoc script. If none exists, add a focused wrapper under `scripts/qa` and record evidence under `output/playwright`.

Exit criteria:

- [ ] Component MVP browser proof passes after H3.
- [ ] Contact visible browser flow passes after H3.
- [ ] Evidence files are recorded in `docs/refactor-control-Commerce-storefront/QA-StorefrontV2.todo.md`.

## Phase H3.15 - Full Solution Gate

Goal: detect broader compile/test regressions after the focused gates pass.

Run:

```powershell
dotnet build BlazorShop.sln --no-restore
dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-restore
```

Rules:

- [ ] Do not hide unrelated known warnings.
- [ ] If unrelated tests fail, record exact failing tests and prove H3 focused gates independently pass.
- [ ] If any H3-related test fails, fix before closure.
- [ ] If solution references fail because `Components.Hybrid` was removed, update solution/test/project references rather than restoring the old project.

Exit criteria:

- [ ] Solution build has 0 errors.
- [ ] Relevant full tests pass, or unrelated failures are explicitly documented with H3 focused evidence.

## Phase H3.16 - Scope Drift Audit

Goal: ensure H3 remains an architecture closure phase and does not accidentally become a feature rewrite.

Expected touched areas:

- `BlazorShop.PresentationV2/BlazorShop.Storefront.Components`
- `BlazorShop.PresentationV2/BlazorShop.Storefront.Components.Ssr`
- `BlazorShop.PresentationV2/BlazorShop.Storefront.Components.WasmHost`
- `BlazorShop.PresentationV2/BlazorShop.Storefront.Components.Hybrid` only for removal
- `BlazorShop.PresentationV2/BlazorShop.Storefront.V2`
- `BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM`
- `BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation` only for QA route/middleware docs/tests if needed
- `BlazorShop.Tests.V2/PresentationV2/Storefront`
- `scripts/qa` only for contact/component proof if needed
- `docs/architecture`
- `docs/refactor-control-Commerce-storefront`
- `BlazorShop.sln`
- affected csproj files

Unexpected unless separately justified:

- Commerce Node business services
- Control Plane services
- checkout/order/payment/cart domain logic
- Storefront Runtime transport
- Storefront Client generated code
- StorefrontBuilder
- Starter/generated storefront projects
- database migrations

Tasks:

- [ ] Run `git diff --stat`.
- [ ] Run `git diff --name-only`.
- [ ] Classify each changed file as expected or justified.
- [ ] Confirm no unrelated feature work entered the phase.
- [ ] Confirm no user changes were reverted.

Exit criteria:

- [ ] H3 diff is limited to closure scope.

## Phase H3.17 - Final Closure Report

Goal: leave a durable execution record for merge readiness.

Update this file with:

- [ ] Final commit SHA or working branch state.
- [ ] Whether `Components.Hybrid` was removed or kept with blocker.
- [ ] Final contact descriptor owner.
- [ ] Final contact visible browser path.
- [ ] Final Hybrid semantic definition.
- [ ] Final render-mode owner.
- [ ] Final browser data path.
- [ ] Component MVP proof results.
- [ ] Contact browser regression result.
- [ ] Focused build result.
- [ ] Focused test result.
- [ ] Full solution result.
- [ ] Remaining technical debt.
- [ ] Merge readiness statement.

Expected final statement:

```text
BlazorShop Storefront Hybrid architecture is closed.
Hybrid means server-produced or prerendered HTML followed by InteractiveWebAssembly browser interactivity.
Reusable components do not own render modes.
Public Storefront UI does not use InteractiveServer, InteractiveAuto, SignalR/Blazor Server UI circuit, or WebSocket-based UI state.
Protected browser interactions use Browser controllers and same-origin BFF routes.
Semantic component mode is independent from physical package ownership.
Components.Hybrid has been retired from active architecture.
```

Exit criteria:

- [ ] Closure report is written.
- [ ] `Hybrid-Architecture` branch is ready for review/merge.

## Required Final Checks

Before marking H3 complete:

- [ ] `rg -n "BlazorShop.Storefront.Components.Hybrid" BlazorShop.PresentationV2 BlazorShop.Tests.V2 BlazorShop.sln -g "!*bin*" -g "!*obj*"` has no active source/test/project matches.
- [ ] `rg -n "Contracts.System|namespace BlazorShop.Storefront.Components.Contracts.System" BlazorShop.PresentationV2 BlazorShop.Tests.V2 -g "!*bin*" -g "!*obj*"` has no active source/test matches.
- [ ] `rg -n "InteractiveServer|InteractiveAuto" BlazorShop.PresentationV2/BlazorShop.Storefront* BlazorShop.Tests.V2 -g "!*bin*" -g "!*obj*"` shows only documentation/test-negative-fixture references, not active public Storefront implementation.
- [ ] `rg -n "@rendermode" BlazorShop.PresentationV2/BlazorShop.Storefront.Components* -g "!*bin*" -g "!*obj*"` shows no reusable component render-mode ownership.
- [ ] `rg -n "api/storefront/stores|CommerceNode|ControlPlane" BlazorShop.PresentationV2/BlazorShop.Storefront.Components.WasmHost BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM -g "!*bin*" -g "!*obj*"` shows no direct backend transport leak, excluding docs/tests if applicable.
- [ ] `dotnet build` focused gate passes.
- [ ] `dotnet test` focused gate passes.
- [ ] Playwright Component MVP proof passes all phases.
- [ ] Contact browser regression passes.
- [ ] `QA-StorefrontV2.todo.md` records final evidence.

## Failure Modes Registry

| Failure mode | Likelihood | Impact | Mitigation |
| --- | --- | --- | --- |
| Delete `Components.Hybrid` before moving descriptor/tests | Medium | Build/test failure | H3.1 moves descriptor and updates tests before H3.3 removal. |
| Public `contact-form` descriptor disappears | Medium | Future component inventory loses contact capability | Keep descriptor key `contact-form`, semantic `Hybrid`, target WasmHost app. |
| Descriptor target points to V2.WASM wrapper | Medium | Shared descriptor becomes V2-specific | Descriptor target must stay in reusable WasmHost. |
| Reusable component owns `@rendermode` again | Medium | Host ownership boundary regresses | H3.6 scanner rejects reusable render modes. |
| Tests still derive mode from project path | High | Future capability packages blocked | H3.4 decouples descriptor discovery from physical mode projects. |
| WebSocket audit fails unrelated dev tooling | Medium | False negative QA | H3.7 fails only Storefront UI circuit/direct Commerce/credential leaks. |
| `Contracts.System` continues shadowing `System.*` | Medium | Fragile code/usings | H3.8 renames to `Contracts.Diagnostics`. |
| Docs keep saying `Components.Hybrid` is active | High | Future agent reintroduces old model | H3.11 updates active source-of-truth docs. |
| Historical docs lose context | Low | Harder archaeology | Do not rewrite old completed plans except clear superseded notes if needed. |
| Contact browser flow regresses | Medium | User-facing content form broken | H3.14 mandatory visible contact Playwright regression. |

## Implementation Task Summary

- [ ] H3.0 baseline and evidence lock.
- [ ] H3.1 move `contact-form` descriptor to WasmHost.
- [ ] H3.2 remove historical contact shell.
- [ ] H3.3 remove `Components.Hybrid` project and references.
- [ ] H3.4 decouple descriptor discovery from project topology.
- [ ] H3.5 harden component dependency matrix.
- [ ] H3.6 harden render mode ownership.
- [ ] H3.7 harden server-interactive/browser transport guardrails.
- [ ] H3.8 rename `Contracts.System` namespace.
- [ ] H3.9 recheck visual neutrality and copy ownership.
- [ ] H3.10 close `/__qa` route policy.
- [ ] H3.11 update active docs and QA checklist.
- [ ] H3.12 run focused build gate.
- [ ] H3.13 run focused test gate.
- [ ] H3.14 run mandatory Playwright browser regression.
- [ ] H3.15 run full solution gate.
- [ ] H3.16 audit scope drift.
- [ ] H3.17 write final closure report.

## Decision Audit Trail

| # | Decision | Classification | Principle | Rationale | Rejected |
| --- | --- | --- | --- | --- | --- |
| 1 | Retire `Components.Hybrid` after descriptor/contact migration, not before. | Auto-decided | Preserve behavior before cleanup | Current tests/source still reference the project; deleting first would create avoidable breakage. | Immediate project deletion as first step. |
| 2 | Move `contact-form` descriptor to WasmHost and keep semantic `Hybrid`. | Auto-decided | Semantic mode is independent from physical package | H2 already proved Hybrid can live in downloadable WasmHost graph; V2.WASM is host-specific and should not own reusable descriptors. | Keep descriptor in Hybrid; move descriptor to V2.WASM. |
| 3 | Rename `Contracts.System` to `Contracts.Diagnostics`. | Auto-decided | Remove known fragility | Current namespace can shadow `System.*` and has already required workaround. | Keep namespace and rely on `global::System` everywhere. |
| 4 | Guard against `/_blazor` and direct Commerce, not all WebSockets. | Auto-decided | Test what matters | Dev tooling may use sockets; production boundary risk is Storefront UI circuit/direct backend transport. | Require WebSocket count always equals zero. |
| 5 | Keep `/__qa/component-mvp` as hidden/noindex architecture QA route. | Auto-decided | Preserve reproducible evidence | H2 proof depends on a deterministic internal route and QA checklist already records it. | Remove QA route during H3. |

## Autoplan Review Report

CEO review:

- Scope is correctly narrow: close a transitional architecture ambiguity before Phase 3 extraction.
- Business value is maintenance clarity, not new feature surface.
- The plan avoids overbuilding by not adding registry/runtime abstractions.
- Main risk is deleting compatibility code before tests and descriptor ownership are moved.
- Decision: approve with the ordered migration gate.

Design review:

- No visual redesign scope.
- V2 remains visual/copy/class owner.
- Reusable components stay visually neutral.
- Contact visible flow must be browser-regressed because it is user-facing.
- Decision: skip design expansion, keep visual ownership tests.

Engineering review:

- The proposal matches current codebase facts.
- The largest real cleanup is test topology, not component code.
- The final architecture should have only `Components.Ssr` and `Components.WasmHost` as reusable mode projects for now.
- `Components.Hybrid` removal is low-risk only after descriptor/test updates.
- Decision: phase order is mandatory.

DX review:

- Agent-facing docs must be updated in the same phase, otherwise future agents will recreate `Components.Hybrid`.
- Test failure messages should name the correct destination: V2 host composition for render mode, WasmHost for browser-executed reusable components, Browser controller for same-origin actions.
- The QA route should remain easy to run through the existing PowerShell wrapper.
- Decision: include docs, source scanners, and commands in the plan.

Cross-phase themes:

- Preserve H2 evidence while removing transitional ambiguity.
- Do not conflate semantic `Hybrid` with a physical project.
- Prefer source/test guardrails over convention-only docs.
- Browser proof is required because previous contact bridge rendered but did not hydrate correctly in visible V2 flow.

Final recommendation:

- Implement H3 before Phase 3 V2 Component Extraction.
- Treat `Components.Hybrid` removal as preferred, but only after descriptor and test consumers are migrated.
- Do not reopen render mode architecture unless browser evidence invalidates H2.

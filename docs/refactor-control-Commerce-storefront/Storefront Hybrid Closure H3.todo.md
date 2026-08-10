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

- [x] Create or move `StorefrontContactFormDescriptor` into `BlazorShop.Storefront.Components.WasmHost/Content`.
- [x] Set descriptor key to `contact-form`.
- [x] Set descriptor mode to `StorefrontComponentMode.Hybrid`.
- [x] Set descriptor category to `StorefrontComponentCategory.Content`.
- [x] Set descriptor component type to `typeof(StorefrontContactFormApp)`.
- [x] Remove `using BlazorShop.Storefront.Components.Hybrid.Content` from descriptor tests.
- [x] Update descriptor inventory expected path from `Components.Hybrid/Content/StorefrontContactFormDescriptor.cs` to `Components.WasmHost/Content/StorefrontContactFormDescriptor.cs`.
- [x] Update descriptor tests that currently expect `typeof(StorefrontContactForm)` to expect `typeof(StorefrontContactFormApp)`.
- [x] Re-evaluate and rename `StorefrontContactFormAppDoesNotPublishPublicDescriptor`; after this phase, the app is the public descriptor target, so the old assertion is obsolete.
- [x] Add a new assertion that no descriptor points at V2.WASM `StorefrontContactFormSection`.
- [x] Add a new assertion that no descriptor points at deleted/compatibility `Components.Hybrid` types.
- [x] Keep existing Browser controller and same-origin `/api/contact` behavior unchanged.

Files expected to change:

- `BlazorShop.PresentationV2/BlazorShop.Storefront.Components.WasmHost/Content/StorefrontContactFormDescriptor.cs`
- `BlazorShop.PresentationV2/BlazorShop.Storefront.Components.Hybrid/Content/StorefrontContactFormDescriptor.cs`
- `BlazorShop.Tests.V2/PresentationV2/Storefront/StorefrontComponentDescriptorTests.cs`
- Any source-reference tests that name the old descriptor path.

Exit criteria:

- [x] `rg -n "StorefrontContactFormDescriptor" BlazorShop.PresentationV2 BlazorShop.Tests.V2` shows the descriptor under WasmHost, not Hybrid.
- [x] Public descriptor inventory has no dependency on `Components.Hybrid`.
- [x] Contact descriptor still validates as semantic `Hybrid`.
- [x] Visible V2 contact route remains unchanged.

Implementation notes:

- 2026-08-10: `StorefrontContactFormDescriptor` moved to `BlazorShop.Storefront.Components.WasmHost/Content` and the old Hybrid descriptor source was deleted.
- 2026-08-10: descriptor remains key `contact-form`, semantic mode `Hybrid`, category `Content`, target `StorefrontContactFormApp`.
- 2026-08-10: descriptor tests no longer import `BlazorShop.Storefront.Components.Hybrid.Content`; the obsolete "app does not publish descriptor" assertion was replaced with positive WasmHost target coverage and negative V2.WASM wrapper/retired Hybrid type coverage.
- 2026-08-10: visible V2 contact route remains V2 `StorefrontPage.razor` -> V2.WASM `StorefrontContactFormSection` -> WasmHost `StorefrontContactFormApp`; Browser controller and `/api/contact` behavior were not changed.
- 2026-08-10: `dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~StorefrontComponentDescriptorTests"` passed 24/24. Existing warnings: MessagePack NU1902/NU1903 and Browserslist/caniuse-lite.
- 2026-08-10: `rg -n "StorefrontContactFormDescriptor" BlazorShop.PresentationV2 BlazorShop.Tests.V2 -g "!bin/**" -g "!obj/**"` shows active descriptor ownership under WasmHost; the only remaining Hybrid mention is historical README text scheduled for project retirement/docs cleanup.

## Phase H3.2 - Remove Historical Contact Shell

Goal: remove the unused historical shell that still owns `@rendermode` inside a reusable library.

Tasks:

- [x] Confirm no production source references `BlazorShop.Storefront.Components.Hybrid.Content.StorefrontContactForm`.
- [x] Delete `BlazorShop.Storefront.Components.Hybrid/Content/StorefrontContactForm.razor`.
- [x] Remove tests that inspect the deleted historical bridge.
- [x] Replace historical bridge tests with tests for the current visible V2.WASM wrapper path:
  - [x] `StorefrontContactFormSection` wraps `StorefrontContactFormApp`.
  - [x] `StorefrontContactFormSection` does not own `@rendermode`.
  - [x] V2 `StorefrontPage.razor` owns `@rendermode="InteractiveWebAssembly"` placement.
  - [x] V2.WASM wrapper supplies V2 labels/classes/action descriptor.
  - [x] Wrapper action stays same-origin `/api/contact`.
- [x] Keep `StorefrontContactFormApp` component tests for validation, submit request, success, failure, and Browser controller invocation.

Do not:

- [x] Do not move `StorefrontContactFormSection` into shared `Components`.
- [x] Do not put V2 labels/classes into shared package.
- [x] Do not introduce direct `HttpClient` into `StorefrontContactFormApp`.

Exit criteria:

- [x] No reusable component file contains `@rendermode` for the old contact bridge.
- [x] Visible contact composition remains V2 route -> V2.WASM wrapper -> WasmHost app.
- [x] Existing contact contracts remain browser-safe.

Implementation notes:

- 2026-08-10: deleted historical `BlazorShop.Storefront.Components.Hybrid/Content/StorefrontContactForm.razor`.
- 2026-08-10: `StorefrontContactFormComponentTests` now verifies the current visible path: V2 page owns `@rendermode="InteractiveWebAssembly"`, V2.WASM `StorefrontContactFormSection` wraps WasmHost `StorefrontContactFormApp`, and the wrapper keeps `/api/contact` as same-origin action.
- 2026-08-10: `StorefrontContactFormApp` remains Browser-controller-only: no `HttpClient`, no direct `/api/*`, no `api/storefront`, and no `@rendermode`.
- 2026-08-10: `rg -n "BlazorShop\.Storefront\.Components\.Hybrid\.Content\.StorefrontContactForm|<StorefrontContactForm(\s|>)" BlazorShop.PresentationV2 -g "!*bin*" -g "!*obj*"` returned no active production matches.
- 2026-08-10: `dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~StorefrontContactFormComponentTests|FullyQualifiedName~StorefrontComponentDescriptorTests"` passed 28/28. Existing warnings: MessagePack NU1902/NU1903 and Browserslist/caniuse-lite.

## Phase H3.3 - Remove Components.Hybrid Project

Goal: retire the physical transitional project after descriptor and shell consumers are gone.

Tasks:

- [x] Delete `BlazorShop.PresentationV2/BlazorShop.Storefront.Components.Hybrid/_Imports.razor`.
- [x] Delete `BlazorShop.PresentationV2/BlazorShop.Storefront.Components.Hybrid/README.md`.
- [x] Delete `BlazorShop.PresentationV2/BlazorShop.Storefront.Components.Hybrid/BlazorShop.Storefront.Components.Hybrid.csproj`.
- [x] Delete the now-empty `BlazorShop.Storefront.Components.Hybrid` folder.
- [x] Remove `BlazorShop.Storefront.Components.Hybrid` project entry from `BlazorShop.sln`.
- [x] Remove test project reference from `BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj`.
- [x] Remove all active build commands for `Components.Hybrid` from plan/docs/QA current gates.
- [x] Keep historical plan files as history, but update source-of-truth docs to say the project is retired.
- [x] Ensure no active project references `BlazorShop.Storefront.Components.Hybrid`.
- [x] Ensure no active test imports `BlazorShop.Storefront.Components.Hybrid`.

Fallback if removal is blocked:

- [x] Not applicable: no real consumer remains, so the project was not kept.
- [x] Not applicable: no blocker exists after descriptor/contact migration.
- [x] Not applicable: compatibility-file guard is unnecessary because the project was removed from tracked source.
- [x] Not applicable: descriptor guard is unnecessary because the project was removed from tracked source.

Preferred exit criteria:

- [x] `rg -n "BlazorShop.Storefront.Components.Hybrid" BlazorShop.PresentationV2 BlazorShop.Tests.V2 BlazorShop.sln` returns no active source/test/project references.
- [x] Historical docs may still mention the retired project only as historical context.
- [x] Active docs do not list `Components.Hybrid` as a current project.

Implementation notes:

- 2026-08-10: removed the retired Hybrid project from `BlazorShop.sln` with `dotnet sln BlazorShop.sln remove`.
- 2026-08-10: deleted tracked Hybrid project files: `_Imports.razor`, `README.md`, and `BlazorShop.Storefront.Components.Hybrid.csproj`; the tracked source tree no longer contains the project. Local ignored `bin`/`obj` artifacts are not part of git/build graph.
- 2026-08-10: removed the Hybrid project reference from `BlazorShop.Tests.V2.csproj`.
- 2026-08-10: updated active architecture tests and `COMPONENT-MODES.md` so reusable project roots are `Components.Ssr` and `Components.WasmHost`; Hybrid remains semantic mode only.
- 2026-08-10: `rg -n "BlazorShop.Storefront.Components.Hybrid" BlazorShop.PresentationV2 BlazorShop.Tests.V2 BlazorShop.sln -g "!*bin*" -g "!*obj*"` returned no matches.
- 2026-08-10: focused architecture/contact gate passed 106/106 with `dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~StorefrontComponentModeFoundationTests|FullyQualifiedName~StorefrontComponentModeDependencyTests|FullyQualifiedName~StorefrontComponentDescriptorTests|FullyQualifiedName~StorefrontComponentModeBoundaryValidatorTests|FullyQualifiedName~StorefrontComponentVisualNeutralityTests|FullyQualifiedName~StorefrontIndependenceBoundaryTests|FullyQualifiedName~StorefrontVisualOnlyBoundaryTests|FullyQualifiedName~StorefrontContactFormComponentTests"`. Existing warnings: MessagePack NU1902/NU1903 and Browserslist/caniuse-lite.

## Phase H3.4 - Descriptor Discovery Decoupling

Goal: make descriptor tests reflect semantic mode rather than mode-project topology.

Current problem:

- `StorefrontComponentDescriptorTests` scans fixed mode project directories.
- It resolves assembly names from path prefixes.
- It includes `Components.Hybrid` as a physical mode project.
- This contradicts the H2 decision that semantic `Hybrid` may live in WasmHost/capability assemblies.

Tasks:

- [x] Replace `ModeProjectDirectories` with `ReusableDescriptorSourceDirectories` or equivalent.
- [x] Include active reusable component source directories explicitly:
  - [x] `BlazorShop.Storefront.Components.Ssr`
  - [x] `BlazorShop.Storefront.Components.WasmHost`
  - [x] Future capability directories only when added by a later phase.
- [x] Remove special path mapping for `Components.Hybrid`.
- [x] Keep deterministic source discovery.
- [x] Keep duplicate key validation.
- [x] Keep descriptor semantic validation.
- [x] Add a positive assertion that `StorefrontHybridRuntimeProbeDescriptor` is semantic `Hybrid` while physically in WasmHost.
- [x] Add a positive assertion that `StorefrontContactFormDescriptor` is semantic `Hybrid` while physically in WasmHost.
- [x] Add a negative fixture or source-level assertion that no test derives mode from project name.
- [x] Do not create runtime descriptor discovery.
- [x] Do not add DI registry for descriptors in H3.

Exit criteria:

- [x] Descriptor tests pass without `Components.Hybrid`.
- [x] Semantic mode and physical project are visibly decoupled.
- [x] Future capability packaging remains possible.

Implementation notes:

- 2026-08-10: `StorefrontComponentDescriptorTests` now scans `ReusableDescriptorSourceDirectories`: `Components.Ssr` and `Components.WasmHost`.
- 2026-08-10: removed the old physical Hybrid path-to-assembly mapping; descriptor assembly resolution remains deterministic for the active reusable descriptor source directories.
- 2026-08-10: contact and runtime-probe descriptor assertions both prove semantic `Hybrid` while the component type physically lives in `BlazorShop.Storefront.Components.WasmHost`.
- 2026-08-10: added a source-level guard that the descriptor test does not reintroduce `ModeProjectDirectories`.
- 2026-08-10: `rg -n "ModeProjectDirectories|BlazorShop.PresentationV2/BlazorShop.Storefront.Components.Hybrid|ResolveAssemblyNameFromPath" BlazorShop.Tests.V2/PresentationV2/Storefront/StorefrontComponentDescriptorTests.cs` returned no matches.
- 2026-08-10: `dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~StorefrontComponentDescriptorTests"` passed 25/25. Existing warnings: MessagePack NU1902/NU1903 and Browserslist/caniuse-lite.

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

- [x] Update `StorefrontComponentModeDependencyTests` to remove `Components.Hybrid`.
- [x] Add direct dependency tests for base `Components`.
- [x] Keep SSR exact references test.
- [x] Keep WasmHost exact references test.
- [x] Add test that WasmHost does not reference Presentation.
- [x] Add test that WasmHost does not reference Runtime/Client/backend/core/API projects.
- [x] Add test that base Components does not reference Browser.
- [x] Add test that V2.WASM does not reference Runtime/Client/backend/core/API projects.
- [x] Add test that V2 does not reference `Components.Hybrid`.
- [x] Update `StorefrontComponentModeBoundaryValidator` messages so they no longer mention "until H2".
- [x] Remove old remediation text that describes `Components.Hybrid` as current dependency graph.

Exit criteria:

- [x] Architecture dependency tests express current target, not historical transition.
- [x] Error messages explain problem, cause, and correct destination.

Implementation notes:

- 2026-08-10: `StorefrontComponentModeDependencyTests` now checks the active reusable mode project paths only: SSR and WasmHost.
- 2026-08-10: added direct tests that base `Components` has no project references and no Browser reference, WasmHost keeps exact Components/Browser references and excludes Presentation/Runtime/Client/backend/core/API projects, V2.WASM excludes Runtime/Client/backend/core/API projects, and V2 excludes the retired Hybrid project.
- 2026-08-10: `StorefrontComponentModeBoundaryValidator` repository profiles now include only SSR and WasmHost; transitional Hybrid repository remediation was removed.
- 2026-08-10: `rg -n "until H2|transitional|compatibility project|Components\\.Hybrid" BlazorShop.Tests.V2/PresentationV2/Storefront/StorefrontComponentModeBoundaryValidator.cs BlazorShop.Tests.V2/PresentationV2/Storefront/StorefrontComponentModeBoundaryValidatorTests.cs` returned no matches.
- 2026-08-10: `dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~StorefrontComponentModeDependencyTests|FullyQualifiedName~StorefrontComponentModeBoundaryValidatorTests|FullyQualifiedName~StorefrontIndependenceBoundaryTests|FullyQualifiedName~StorefrontVisualOnlyBoundaryTests"` passed 57/57. Existing warnings: MessagePack NU1902/NU1903 and Browserslist/caniuse-lite.

## Phase H3.6 - Render Mode Ownership Guardrails

Goal: enforce that reusable components do not self-own render modes and public Storefront cannot regress to server-interactive modes.

Rules:

- Reusable SSR components must not contain `@rendermode`, `InteractiveWebAssembly`, `InteractiveServer`, or `InteractiveAuto`.
- Reusable WasmHost components must not contain `@rendermode`.
- V2/V2.WASM host composition may use `@rendermode="InteractiveWebAssembly"` only at approved boundaries.
- Public Storefront code must not use `InteractiveServer` or `InteractiveAuto`.
- Deleted `Components.Hybrid` cannot remain as an exception.

Tasks:

- [x] Add or update source scanner tests for reusable packages.
- [x] Scan `BlazorShop.Storefront.Components`.
- [x] Scan `BlazorShop.Storefront.Components.Ssr`.
- [x] Scan `BlazorShop.Storefront.Components.WasmHost`.
- [x] Scan `BlazorShop.Storefront.V2`.
- [x] Scan `BlazorShop.Storefront.V2.WASM`.
- [x] Fail if any active source contains `InteractiveServer`.
- [x] Fail if any active source contains `InteractiveAuto`.
- [x] Fail if reusable component files contain `@rendermode`.
- [x] Allow `@rendermode="InteractiveWebAssembly"` only in approved V2 composition files.
- [x] Add negative fixtures in tests so the scanner proves violations are caught.
- [x] Exclude docs and historical plan files from production-source checks.

Approved current render-mode owners:

- `BlazorShop.Storefront.V2/Pages/Ssr/Content/StorefrontPage.razor`
- `BlazorShop.Storefront.V2/Pages/Hybrid/Catalog/Home.razor`
- `BlazorShop.Storefront.V2/Pages/Hybrid/Commerce/CartPage.razor`
- `BlazorShop.Storefront.V2/Pages/Hybrid/Commerce/CheckoutPage.razor`
- `BlazorShop.Storefront.V2/Pages/WasmHost/Account/AccountHostPage.razor`
- `BlazorShop.Storefront.V2/Components/System/StorefrontComponentMvpLab.razor`

Exit criteria:

- [x] Render mode ownership is mechanically enforced.
- [x] No reusable component owns `@rendermode`.
- [x] No public Storefront source uses `InteractiveServer` or `InteractiveAuto`.

Implementation notes:

- 2026-08-10: added `StorefrontRenderModeOwnershipTests` with source scanners for reusable roots (`Components`, `Components.Ssr`, `Components.WasmHost`) and public Storefront roots (`Components`, `Components.Ssr`, `Components.WasmHost`, `V2`, `V2.WASM`).
- 2026-08-10: reusable package scanner rejects `@rendermode`; public Storefront scanner rejects `InteractiveServer` and `InteractiveAuto`.
- 2026-08-10: `InteractiveWebAssembly` placement is allowed only in the approved V2 composition files listed above; V2.WASM wrappers do not own render-mode directives.
- 2026-08-10: negative fixtures prove the scanner catches reusable render-mode ownership, server interactivity, auto interactivity, and unapproved WASM placement.
- 2026-08-10: `rg -n "@rendermode" BlazorShop.PresentationV2/BlazorShop.Storefront.Components BlazorShop.PresentationV2/BlazorShop.Storefront.Components.Ssr BlazorShop.PresentationV2/BlazorShop.Storefront.Components.WasmHost -g "*.razor" -g "*.cs" -g "!bin/**" -g "!obj/**"` returned no matches.
- 2026-08-10: `rg -n "InteractiveServer|InteractiveAuto" BlazorShop.PresentationV2/BlazorShop.Storefront.Components BlazorShop.PresentationV2/BlazorShop.Storefront.Components.Ssr BlazorShop.PresentationV2/BlazorShop.Storefront.Components.WasmHost BlazorShop.PresentationV2/BlazorShop.Storefront.V2 BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM -g "*.razor" -g "*.cs" -g "!bin/**" -g "!obj/**"` returned no matches.
- 2026-08-10: `dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~StorefrontRenderModeOwnershipTests|FullyQualifiedName~StorefrontComponentModeBoundaryValidatorTests|FullyQualifiedName~StorefrontContactFormComponentTests|FullyQualifiedName~StorefrontComponentMvpArchitectureTests|FullyQualifiedName~StorefrontComponentMvpLabTests"` passed 36/36. Existing warnings: MessagePack NU1902/NU1903 and Browserslist/caniuse-lite.

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

- [x] Keep `scripts/qa/run-storefront-component-mvp-proof.ps1`.
- [x] Keep `scripts/qa/storefront-component-mvp-proof.js`.
- [x] Review the Network phase classification.
- [x] Ensure Network phase fails on `/_blazor`.
- [x] Ensure Network phase fails on direct Commerce host/path.
- [x] Ensure Network phase fails on credential leak.
- [x] Ensure Network phase records WebSocket/EventSource counts for evidence.
- [x] Do not require WebSocket count to be zero unless the recorded URL is Storefront UI/circuit related.
- [x] Add source-level guard for `HubConnection`, `AddSignalR`, `MapHub`, `ClientWebSocket`, and `WebSocket.CreateFromStream` in public Storefront UI source.
- [x] Scope this guard to Storefront UI packages, not the entire solution.

Exit criteria:

- [x] H2 Network proof remains reproducible.
- [x] Guardrail catches actual Storefront server-interactive drift.
- [x] Guardrail does not fail on unrelated dev-tooling sockets.

Implementation notes:

- 2026-08-10: reviewed `scripts/qa/storefront-component-mvp-proof.js`; Network phase classifies document/static/_framework/same-origin BFF/EventSource/WebSocket/server UI circuit/direct Commerce requests, fails on `/_blazor`, direct Commerce, credential leaks, console errors, and page errors.
- 2026-08-10: added `StorefrontServerInteractiveTransportGuardrailTests` scoped to Storefront UI packages (`Browser`, `Components`, `Components.Ssr`, `Components.WasmHost`, `Presentation`, `V2`, `V2.WASM`), not the whole solution.
- 2026-08-10: static guard rejects `HubConnection`, `AddSignalR`, `MapHub`, `ClientWebSocket`, and `WebSocket.CreateFromStream`; negative fixtures prove every token is caught.
- 2026-08-10: `rg -n "HubConnection|AddSignalR|MapHub|ClientWebSocket|WebSocket\\.CreateFromStream" BlazorShop.PresentationV2/BlazorShop.Storefront.Browser BlazorShop.PresentationV2/BlazorShop.Storefront.Components BlazorShop.PresentationV2/BlazorShop.Storefront.Components.Ssr BlazorShop.PresentationV2/BlazorShop.Storefront.Components.WasmHost BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation BlazorShop.PresentationV2/BlazorShop.Storefront.V2 BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM -g "*.cs" -g "*.razor" -g "*.js" -g "!bin/**" -g "!obj/**"` returned no matches.
- 2026-08-10: `dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~StorefrontServerInteractiveTransportGuardrailTests|FullyQualifiedName~StorefrontRenderModeOwnershipTests"` passed 6/6. Existing warnings: MessagePack NU1902/NU1903 and Browserslist/caniuse-lite.
- 2026-08-10: `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/qa/run-storefront-component-mvp-proof.ps1 -Phase Network -StorefrontBaseUrl http://127.0.0.1:18640 -RuntimeTimeoutSeconds 120` passed. Evidence summary: `serverUiCircuit=0`, `directCommerce=0`, `credentialLeaks=[]`, `consoleErrors=[]`, `pageErrors=[]`, `webSockets=0`, `sameOriginBff=3`.

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

- [x] Move `StorefrontHybridRuntimeProbeLabels.cs` from `Contracts/System` to `Contracts/Diagnostics`.
- [x] Move `StorefrontHybridRuntimeProbeClasses.cs` from `Contracts/System` to `Contracts/Diagnostics`.
- [x] Rename namespace to `BlazorShop.Storefront.Components.Contracts.Diagnostics`.
- [x] Update WasmHost `_Imports.razor`.
- [x] Update V2.WASM `_Imports.razor`.
- [x] Update `StorefrontHybridRuntimeProbeComponentTests`.
- [x] Update any source-reference tests that list `System/StorefrontHybridRuntimeProbe*.cs`.
- [x] Remove unnecessary `global::System` workaround only if it becomes unnecessary and tests prove no regression.
- [x] If keeping `global::System` improves clarity with no downside, document why it remains.

Exit criteria:

- [x] `rg -n "Contracts.System|namespace BlazorShop.Storefront.Components.Contracts.System" BlazorShop.PresentationV2 BlazorShop.Tests.V2` returns no active source/test matches.
- [x] H2 probe continues to build and render.

Implementation notes:

- 2026-08-10: moved probe labels/classes and README from `Contracts/System` to `Contracts/Diagnostics`; namespaces now use `BlazorShop.Storefront.Components.Contracts.Diagnostics`.
- 2026-08-10: updated WasmHost and V2.WASM imports plus `StorefrontHybridRuntimeProbeComponentTests`.
- 2026-08-10: updated contract inventory test from `System/StorefrontHybridRuntimeProbe*.cs` to `Diagnostics/StorefrontHybridRuntimeProbe*.cs`.
- 2026-08-10: removed `global::System.Text.RegularExpressions` from `StorefrontComponentDescriptorValidator`; focused tests/build proved no regression.
- 2026-08-10: `rg -n "Contracts\\.System|namespace BlazorShop\\.Storefront\\.Components\\.Contracts\\.System|Contracts/System|global::System.Text.RegularExpressions" BlazorShop.PresentationV2 BlazorShop.Tests.V2 -g "!*bin*" -g "!*obj*"` returned no matches.
- 2026-08-10: `dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.Components.WasmHost/BlazorShop.Storefront.Components.WasmHost.csproj --no-restore` passed with 0 warnings/errors.
- 2026-08-10: `dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~StorefrontHybridRuntimeProbeComponentTests|FullyQualifiedName~StorefrontComponentsHeadlessPresentationRefactorTests|FullyQualifiedName~StorefrontComponentDescriptorTests|FullyQualifiedName~StorefrontRenderModeOwnershipTests"` passed 57/57. Existing warnings: MessagePack NU1902/NU1903 and Browserslist/caniuse-lite.

## Phase H3.9 - Visual Neutrality And Copy Ownership Recheck

Goal: ensure H3 cleanup does not move visual ownership into reusable packages.

Tasks:

- [x] Run current visual neutrality tests.
- [x] Update scan roots to remove `Components.Hybrid`.
- [x] Include `Components.WasmHost/System/StorefrontHybridRuntimeProbe.razor`.
- [x] Include `Components.WasmHost/Content/StorefrontContactFormApp.razor`.
- [x] Confirm reusable components use host-supplied class slots only.
- [x] Confirm V2/V2.WASM own final class strings.
- [x] Confirm reusable packages do not own theme CSS.
- [x] Confirm V2-specific labels/copy remain in V2/V2.WASM wrappers.
- [x] Confirm shared contracts expose label fields, not final storefront copy as business truth.

Exit criteria:

- [x] Visual neutrality tests pass.
- [x] `Components.WasmHost` remains reusable and browser-safe.
- [x] No V2 visual class string appears in base shared/reusable component implementation except tests or V2 wrappers.

Implementation notes:

- 2026-08-10: renamed visual neutrality scan roots to `ReusableComponentDirectories` and kept active roots at `Components.Ssr` and `Components.WasmHost`.
- 2026-08-10: added explicit coverage that the scan includes `Components.WasmHost/System/StorefrontHybridRuntimeProbe.razor` and `Components.WasmHost/Content/StorefrontContactFormApp.razor`.
- 2026-08-10: reusable components continue to use dynamic class slots/contracts; V2/V2.WASM wrappers own final classes, labels, and action descriptors.
- 2026-08-10: `rg -n "ModeProjectDirectories|Components.Hybrid|class=\"(?!@)|Shop now|Add to cart|Checkout|Sale|Free shipping|bs-storefront-|storefront.css|css/site.css|css/wasm-site.css|wwwroot/|/_content/BlazorShop.Storefront.V2" BlazorShop.Tests.V2/PresentationV2/Storefront/StorefrontComponentVisualNeutralityTests.cs BlazorShop.PresentationV2/BlazorShop.Storefront.Components.Ssr BlazorShop.PresentationV2/BlazorShop.Storefront.Components.WasmHost -g "*.razor" -g "*.cs" -g "*.md" -g "!bin/**" -g "!obj/**"` returned no matches.
- 2026-08-10: `dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~StorefrontComponentVisualNeutralityTests|FullyQualifiedName~StorefrontContactFormComponentTests|FullyQualifiedName~StorefrontHybridRuntimeProbeComponentTests|FullyQualifiedName~StorefrontDiscountedProductRailComponentTests|FullyQualifiedName~StorefrontVisualOnlyBoundaryTests"` passed 43/43. Existing warnings: MessagePack NU1902/NU1903 and Browserslist/caniuse-lite.

## Phase H3.10 - QA Route Policy Closure

Goal: make `/__qa/component-mvp` policy explicit before merge.

Decision:

- Keep `/__qa/component-mvp` as a hidden/noindex architecture QA route.
- Do not add it to navigation, sitemap, content catalog, or page template catalog.
- Keep middleware skip only for deterministic architecture QA paths required by tests.

Tasks:

- [x] Confirm `ComponentMvpRoutePage.razor` sets robots noindex/nofollow metadata.
- [x] Confirm `StorefrontCurrentStoreMiddleware` skips `/__qa/component-mvp` or `/__qa/*` intentionally.
- [x] Confirm `StorefrontPublicRedirectMiddleware` skips `/__qa/component-mvp` or `/__qa/*` intentionally.
- [x] Confirm sitemap does not include `/__qa/component-mvp`.
- [x] Confirm navigation/menu does not include `/__qa/component-mvp`.
- [x] Confirm robots policy remains documented.
- [x] Decide whether docs use broad `/__qa/*` namespace or narrow `/__qa/component-mvp`.
- [x] If broad namespace remains, document it as internal architecture QA namespace and require noindex/not in sitemap.
- [x] If narrow exception is chosen, update middleware/tests to match exact path.

Exit criteria:

- [x] QA route policy is explicit.
- [x] Browser proof route stays deterministic.
- [x] Production-facing navigation/SEO surfaces do not expose the route.

Implementation notes:

- 2026-08-10: kept the broad internal `/__qa/*` architecture QA namespace and made the policy explicit through `IsArchitectureQaPath` in both `StorefrontCurrentStoreMiddleware` and `StorefrontPublicRedirectMiddleware`.
- 2026-08-10: `ComponentMvpRoutePage.razor` remains Presentation-owned and emits `RobotsIndex = false` and `RobotsFollow = false` for both ready and unavailable states.
- 2026-08-10: added architecture tests that keep `/__qa/component-mvp` out of public navigation, sitemap, route catalogs, and V2 public route ownership.
- 2026-08-10: focused H3.10 test gate passed 28/28 with `dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~StorefrontComponentMvpArchitectureTests|FullyQualifiedName~StorefrontCurrentStoreMiddlewareTests|FullyQualifiedName~StorefrontPublicRedirectMiddlewareTests"`. Existing warnings: MessagePack NU1902/NU1903 and Browserslist/caniuse-lite.

## Phase H3.11 - Documentation Closure

Goal: update active docs so agents and developers see the final H3 architecture, not the transitional H1/H2 model.

Files to update:

- [x] `AGENTS.md`
- [x] `BlazorShop.PresentationV2/COMPONENT-MODES.md`
- [x] `docs/architecture/03-runtime-boundaries.md`
- [x] `docs/architecture/05-project-and-folder-guide.md`
- [x] `docs/architecture/08-agent-decision-rules.md`
- [x] `docs/architecture/10-v2-contract-ownership.md`
- [x] `docs/refactor-control-Commerce-storefront/QA-StorefrontV2.todo.md`
- [x] This H3 plan file with execution evidence.

Required documentation changes:

- [x] Remove active-current wording that lists `Components.Hybrid` as a live reusable project if retired.
- [x] Preserve historical references only in historical plan files.
- [x] State that `Hybrid = prerender/server-produced HTML + InteractiveWebAssembly browser interactivity`.
- [x] State that semantic component mode is independent from physical project/package.
- [x] State that reusable components do not own `@rendermode`.
- [x] State that V2/host composition owns `@rendermode InteractiveWebAssembly`.
- [x] State that public Storefront must not use `InteractiveServer` or `InteractiveAuto` without a new architecture decision.
- [x] State that public Storefront component interaction must not depend on SignalR/Blazor Server UI circuit.
- [x] State that browser components use Browser controllers and same-origin BFF, not direct Commerce Node APIs.
- [x] Document the retired `Contracts.System` namespace if renamed.

Exit criteria:

- [x] Active source-of-truth docs match code after H3.
- [x] No active doc tells an agent to create new components in `Components.Hybrid`.
- [x] No active doc describes nested Hybrid shell as canonical.

Implementation notes:

- 2026-08-10: removed `BlazorShop.Storefront.Components.Hybrid` from the active project list in `AGENTS.md`.
- 2026-08-10: `COMPONENT-MODES.md` already reflected the H3 semantic mode model, retired Hybrid project, reusable render-mode ownership guardrail, Browser controller + same-origin BFF path, and `Contracts.Diagnostics` namespace after H3.3/H3.8.
- 2026-08-10: updated active architecture docs so reusable mode projects are `Components.Ssr` and `Components.WasmHost`; `Components.Hybrid` is documented only as retired history.
- 2026-08-10: updated `QA-StorefrontV2.todo.md` to supersede older foundation wording that described three live mode projects or descriptor-mode/project coupling.

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

- [x] All focused builds pass with 0 new errors.
- [x] Any existing unrelated warnings are recorded, not hidden.

Implementation notes:

- 2026-08-10: focused build gate passed for `Components`, `Components.Ssr`, `Components.WasmHost`, `Browser`, `Presentation`, `V2.WASM`, and `V2` with `--no-restore`.
- 2026-08-10: each focused project build reported `0 Warning(s)` and `0 Error(s)`.
- 2026-08-10: no `Components.Hybrid` build was run because the physical project has been retired from the active solution.

## Phase H3.13 - Focused Test Gate

Goal: prove source guardrails and component tests reflect the final H3 model.

Run focused tests:

```powershell
dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~StorefrontComponentDescriptorTests|FullyQualifiedName~StorefrontComponentModeDependencyTests|FullyQualifiedName~StorefrontComponentModeBoundaryValidatorTests|FullyQualifiedName~StorefrontComponentVisualNeutralityTests|FullyQualifiedName~StorefrontVisualOnlyBoundaryTests|FullyQualifiedName~StorefrontIndependenceBoundaryTests|FullyQualifiedName~StorefrontContactFormComponentTests|FullyQualifiedName~StorefrontBrowserContactControllerTests|FullyQualifiedName~StorefrontHybridRuntimeProbeComponentTests|FullyQualifiedName~StorefrontComponentMvpArchitectureTests|FullyQualifiedName~StorefrontComponentMvpLabTests|FullyQualifiedName~StorefrontPageCompositionGuardrailTests|FullyQualifiedName~StorefrontCurrentStoreMiddlewareTests|FullyQualifiedName~StorefrontPublicRedirectMiddlewareTests"
```

Required test coverage:

- [x] Descriptor inventory has no `Components.Hybrid` source path.
- [x] Contact descriptor remains semantic `Hybrid`.
- [x] Contact descriptor target is in `Components.WasmHost`.
- [x] `StorefrontContactFormSection` remains V2.WASM host wrapper.
- [x] Reusable components do not own `@rendermode`.
- [x] V2 composition owns approved `InteractiveWebAssembly` placements.
- [x] `InteractiveServer` is forbidden in public Storefront UI source.
- [x] `InteractiveAuto` is forbidden in public Storefront UI source.
- [x] `Components.Hybrid` project is absent from active project references.
- [x] `Contracts.System` namespace is absent if renamed.
- [x] Browser controller path remains same-origin.
- [x] `/__qa/component-mvp` route ownership remains Presentation-only.

Exit criteria:

- [x] Focused tests pass.
- [x] Tests fail clearly if a future agent reintroduces `Components.Hybrid` as active architecture.

Implementation notes:

- 2026-08-10: focused H3.13 test gate passed 186/186 with `dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~StorefrontComponentDescriptorTests|FullyQualifiedName~StorefrontComponentModeDependencyTests|FullyQualifiedName~StorefrontComponentModeBoundaryValidatorTests|FullyQualifiedName~StorefrontComponentVisualNeutralityTests|FullyQualifiedName~StorefrontVisualOnlyBoundaryTests|FullyQualifiedName~StorefrontIndependenceBoundaryTests|FullyQualifiedName~StorefrontContactFormComponentTests|FullyQualifiedName~StorefrontBrowserContactControllerTests|FullyQualifiedName~StorefrontHybridRuntimeProbeComponentTests|FullyQualifiedName~StorefrontComponentMvpArchitectureTests|FullyQualifiedName~StorefrontComponentMvpLabTests|FullyQualifiedName~StorefrontPageCompositionGuardrailTests|FullyQualifiedName~StorefrontCurrentStoreMiddlewareTests|FullyQualifiedName~StorefrontPublicRedirectMiddlewareTests"`.
- 2026-08-10: existing unrelated warnings remain MessagePack NU1902/NU1903 and Browserslist/caniuse-lite.

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

- [x] H3.0 baseline and evidence lock.
- [x] H3.1 move `contact-form` descriptor to WasmHost.
- [x] H3.2 remove historical contact shell.
- [x] H3.3 remove `Components.Hybrid` project and references.
- [x] H3.4 decouple descriptor discovery from project topology.
- [x] H3.5 harden component dependency matrix.
- [x] H3.6 harden render mode ownership.
- [x] H3.7 harden server-interactive/browser transport guardrails.
- [x] H3.8 rename `Contracts.System` namespace.
- [x] H3.9 recheck visual neutrality and copy ownership.
- [x] H3.10 close `/__qa` route policy.
- [x] H3.11 update active docs and QA checklist.
- [x] H3.12 run focused build gate.
- [x] H3.13 run focused test gate.
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

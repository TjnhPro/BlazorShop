# Storefront Component Mode Foundation v2

Status: planned
Owner: Storefront V2 architecture
Branch: `Hybrid-Architecture`
Predecessor: `Storefront Hybrid Architecture Clarification.todo.md`
Successor: H2 Reference Component MVP
Scope: component mode foundation, dependency graph, runtime ownership, architecture tests, documentation

## Goal

Re-align the Storefront component mode foundation after H0 clarified that `Ssr`, `Hybrid`, and `WasmHost` are BlazorShop architecture classifications, not direct `.NET` render-mode aliases and not physical project-name contracts.

H1 must make the foundation flexible enough for H2 to implement the next real reusable component MVP without inheriting the old assumption:

```text
Hybrid == Components.Hybrid assembly == server shell == nested WasmHost child
```

The target meaning is:

```text
Ssr
  = primary useful behavior can render from server-prepared state.

Hybrid
  = useful server-produced or prerendered HTML/page snapshot
    + client-side WebAssembly interactivity after hydration.

WasmHost
  = browser-side reusable interactive root included in the downloadable WASM graph.
```

## Planning Evidence From Investigation

The following facts were verified from the current codebase before writing this plan.

- Current branch is `Hybrid-Architecture`.
- Current working tree was clean at investigation time.
- `BlazorShop.PresentationV2/COMPONENT-MODES.md` already defines `Ssr`, `Hybrid`, and `WasmHost` as BlazorShop classifications.
- `StorefrontComponentMode` is already a minimal enum with `Ssr`, `Hybrid`, and `WasmHost`.
- `StorefrontComponentDescriptor` is already minimal: `Key`, `Mode`, `Category`, `ComponentType`.
- `StorefrontComponentDescriptorValidator` validates descriptor shape only and does not enforce physical assembly ownership.
- The stale coupling is primarily in architecture tests:
  - `StorefrontComponentDescriptorTests` maps known mode assemblies to mode values.
  - `StorefrontComponentDescriptorTests` requires descriptor mode to match owning assembly mode.
  - `StorefrontComponentModeDependencyTests` requires `Components.Hybrid` to reference exactly Components, Presentation, and WasmHost.
  - `StorefrontComponentModeBoundaryValidator` profiles still treat Hybrid as a project with a fixed historical graph.
- Current direct project graph:

```text
BlazorShop.Storefront.Components.Ssr
  -> BlazorShop.Storefront.Components
  -> BlazorShop.Storefront.Presentation

BlazorShop.Storefront.Components.Hybrid
  -> BlazorShop.Storefront.Components
  -> BlazorShop.Storefront.Presentation
  -> BlazorShop.Storefront.Components.WasmHost

BlazorShop.Storefront.Components.WasmHost
  -> BlazorShop.Storefront.Components
  -> BlazorShop.Storefront.Browser
```

- Current V2 visible interactive examples use V2/V2.WASM wrappers:
  - `StorefrontPage.razor` renders `StorefrontContactFormSection @rendermode="InteractiveWebAssembly"`.
  - `StorefrontContactFormSection.razor` wraps `StorefrontContactFormApp`.
  - `Home.razor` renders `StorefrontDiscountedProductRailSection @rendermode="InteractiveWebAssembly"`.
  - `StorefrontDiscountedProductRailSection.razor` wraps `StorefrontDiscountedProductRail`.
- `BlazorShop.Storefront.V2` references `Components`, `Components.Ssr`, `Presentation`, `Browser`, and `V2.WASM`; it does not directly reference `Components.Hybrid`.
- `BlazorShop.Storefront.V2.WASM` references `Browser`, `Components`, and `Components.WasmHost`; it does not reference `Components.Hybrid`.
- `Components.Hybrid` still contains the historical contact form shell and descriptor, and its csproj description still says "hybrid shell mode library".

## Core Decision

H1 should be implemented as:

```text
semantic decoupling
  +
guardrail realignment
  +
documentation cleanup
```

H1 should not become a broad production component extraction.

Preferred result at the end of H1:

```text
Components.Ssr
  -> server reusable rendering boundary

Components.WasmHost
  -> browser/WASM reusable interactive boundary

Components.Hybrid
  -> transitional compatibility project until H2 proves the permanent pattern

StorefrontComponentMode
  -> semantic runtime/usage metadata, not physical assembly ownership
```

## Hard Scope

Allowed production areas:

```text
BlazorShop.PresentationV2/BlazorShop.Storefront.Components/**
BlazorShop.PresentationV2/BlazorShop.Storefront.Components.Ssr/**
BlazorShop.PresentationV2/BlazorShop.Storefront.Components.Hybrid/**
BlazorShop.PresentationV2/BlazorShop.Storefront.Components.WasmHost/**
BlazorShop.PresentationV2/BlazorShop.Storefront.Browser/**
BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/**
BlazorShop.Tests.V2/**
BlazorShop.PresentationV2/COMPONENT-MODES.md
docs/architecture/**
docs/refactor-control-Commerce-storefront/**
```

V2/V2.WASM may be changed only if a compile/runtime foundation blocker requires a narrow compatibility adjustment. Any V2/V2.WASM change must be documented in this file with reason and evidence.

Forbidden H1 scope:

- Do not add Product/Catalog/Cart reusable component suites.
- Do not start Phase 3 component extraction.
- Do not modify Commerce Node business behavior.
- Do not add Storefront APIs.
- Do not change checkout/payment/cart/order truth.
- Do not modify Control Plane.
- Do not modify StorefrontBuilder generation logic.
- Do not migrate Starter to a new component model.
- Do not introduce capability module projects such as `Components.Product`.
- Do not create component registry, reflection scanner, plugin system, or theme system.
- Do not rewrite V2 visuals.
- Do not introduce `InteractiveServer`, `InteractiveAuto`, SignalR/circuit-based public Storefront interactivity, or persistent server-side component state for public storefront UI.
- Do not delete or rename `Components.Hybrid` until H2 proves the permanent replacement and all consumers/tests have been deliberately migrated.

## Required Reading

- [x] `AGENTS.md`
- [x] `docs/architecture/README.md`
- [x] `docs/architecture/03-runtime-boundaries.md`
- [x] `docs/architecture/05-project-and-folder-guide.md`
- [x] `docs/architecture/08-agent-decision-rules.md`
- [x] `docs/architecture/10-v2-contract-ownership.md`
- [x] `BlazorShop.PresentationV2/COMPONENT-MODES.md`
- [x] `docs/refactor-control-Commerce-storefront/Storefront Hybrid Architecture Clarification.todo.md`
- [x] `docs/refactor-control-Commerce-storefront/Storefront Component Mode Foundation.todo.md`
- [x] `docs/refactor-control-Commerce-storefront/Storefront Component Mode Foundation Closure Patch.todo.md`
- [x] `docs/refactor-control-Commerce-storefront/Storefront Reference Components.todo.md`

## Phase H1.0 - Baseline Inventory

### Goal

Record the current implementation before changing tests or project boundaries.

### Tasks

- [x] Confirm branch with `git branch --show-current`.
- [x] Record `git status --short`.
- [x] Record branch comparison against `master` if needed for implementation review.
- [x] Record current direct project references for:
  - [x] `BlazorShop.Storefront.Components`
  - [x] `BlazorShop.Storefront.Components.Ssr`
  - [x] `BlazorShop.Storefront.Components.Hybrid`
  - [x] `BlazorShop.Storefront.Components.WasmHost`
  - [x] `BlazorShop.Storefront.Browser`
  - [x] `BlazorShop.Storefront.Presentation`
  - [x] `BlazorShop.Storefront.V2`
  - [x] `BlazorShop.Storefront.V2.WASM`
- [x] Find all current descriptor definitions:
  - [x] `StorefrontComponentMode.Ssr`
  - [x] `StorefrontComponentMode.Hybrid`
  - [x] `StorefrontComponentMode.WasmHost`
- [x] Find all current direct usages of `BlazorShop.Storefront.Components.Hybrid`.
- [x] Find all current `@rendermode` placements in Storefront V2, V2.WASM, Ssr, Hybrid, and WasmHost projects.
- [x] Find all tests that infer mode from physical path, project, namespace, or assembly name.
- [x] Find all tests that require exact mode-project project references.
- [x] Find all tests that scan for forbidden render/runtime tokens.

### Commands

```powershell
git branch --show-current
git status --short
rg -n "StorefrontComponentMode\\.(Ssr|Hybrid|WasmHost)" BlazorShop.PresentationV2 BlazorShop.Tests.V2
rg -n "@rendermode|InteractiveWebAssembly|InteractiveServer|InteractiveAuto" BlazorShop.PresentationV2/BlazorShop.Storefront*
rg -n "OwnerMode|ResolveOwnerMode|HybridReferencesExactly|Components.Hybrid" BlazorShop.Tests.V2 BlazorShop.PresentationV2
```

### Output

Implementation notes:

```text
Branch:
Hybrid-Architecture

Git status:
M "docs/refactor-control-Commerce-storefront/Storefront Component Mode Foundation v2.todo.md"
The plan file was already modified when this H1 run started; H1.0 records that plan baseline rather than reverting it.

Branch comparison:
Not needed for H1.0 implementation review because the requested work is scoped to the current Hybrid-Architecture branch and the current branch/status/project graph were recorded before rule changes.

Current project graph:
BlazorShop.Storefront.Components -> no ProjectReference entries.
BlazorShop.Storefront.Components.Ssr -> Components, Presentation.
BlazorShop.Storefront.Components.Hybrid -> Components, Presentation, Components.WasmHost.
BlazorShop.Storefront.Components.WasmHost -> Components, Browser.
BlazorShop.Storefront.Browser -> Components.
BlazorShop.Storefront.Presentation -> Components (PrivateAssets=all), Runtime.
BlazorShop.Storefront.V2 -> ServiceDefaults, Browser, Components, Components.Ssr, Presentation, V2.WASM.
BlazorShop.Storefront.V2.WASM -> Browser, Components, Components.WasmHost.

Current descriptor assumptions:
StorefrontBrandLogoDescriptor declares brand-logo / Ssr / Brand / StorefrontBrandLogo.
StorefrontContactFormDescriptor declares contact-form / Hybrid / Content / StorefrontContactForm.
StorefrontDiscountedProductRailDescriptor declares discounted-product-rail / WasmHost / Catalog / StorefrontDiscountedProductRail.
StorefrontComponentDescriptorTests currently maps mode assemblies and descriptor file paths back to Ssr/Hybrid/WasmHost owner modes.

Current Hybrid consumers:
BlazorShop.Tests.V2 references Components.Hybrid for descriptor and component tests.
Components.Hybrid source files are _Imports.razor, Content/StorefrontContactForm.razor, Content/StorefrontContactFormDescriptor.cs, README.md, and the csproj.
V2 does not directly reference Components.Hybrid; visible contact uses V2.WASM wrapper StorefrontContactFormSection.

Current render-mode placements:
Components.Hybrid/Content/StorefrontContactForm.razor has InteractiveWebAssembly on StorefrontContactFormApp as a historical bridge.
V2/Pages/WasmHost/Account/AccountHostPage.razor has InteractiveWebAssembly.
V2/Pages/Hybrid/Catalog/Home.razor has InteractiveWebAssembly on StorefrontDiscountedProductRailSection.
V2/Pages/Hybrid/Commerce/CartPage.razor has InteractiveWebAssembly on the cart shell.
V2/Pages/Hybrid/Commerce/CheckoutPage.razor has InteractiveWebAssembly on checkout and confirmation shells.
V2/Pages/Ssr/Content/StorefrontPage.razor has InteractiveWebAssembly on StorefrontContactFormSection.
No InteractiveServer or InteractiveAuto placement was found in the checked Storefront projects.

Current stale architecture-test assumptions:
StorefrontComponentDescriptorTests has OwnerModeResolver* tests, DescriptorModeConsistency* tests, RepositoryModeProjectDescriptorsAreValidAndOwnedByTheirModeProjects, ResolveOwnerModeFromPath, and StorefrontComponentDescriptorModeOwnership.
StorefrontComponentModeDependencyTests has HybridReferencesExactlyComponentsPresentationAndWasmHost with permanent-sounding naming.
StorefrontComponentModeBoundaryValidator profiles keep the right transitional allowlist but Hybrid remediation still says "Hybrid components may reference only..." without H1/H2 transitional context.
StorefrontVisualOnlyBoundaryTests.F1_41_ReferenceComponentModeReferences_AreNarrowAndAdoptedOnlyByV2 already matches current V2/V2.WASM wrapper adoption and does not require Components.Hybrid as a V2 reference.
```

### Exit Criteria

- [x] No production or test rule is changed before current behavior is recorded.
- [x] All historical Hybrid coupling points are identified.
- [x] H1 remains foundation-only.

## Phase H1.1 - Lock Semantic Mode Contract

### Goal

Make explicit that mode metadata describes runtime/usage semantics, not project or assembly ownership.

### Tasks

- [x] Review `StorefrontComponentMode`.
- [x] Keep existing enum values unless real evidence requires more.
- [x] Do not add `HybridSsr`, `HybridWasm`, `HybridServer`, or `HybridClient`.
- [x] Review `StorefrontComponentDescriptor`.
- [x] Keep descriptor minimal: `Key`, `Mode`, `Category`, `ComponentType`.
- [x] Review `StorefrontComponentDescriptorValidator`.
- [x] Preserve validation of:
  - [x] non-empty lowercase kebab-case key;
  - [x] valid mode;
  - [x] valid category;
  - [x] component type implements `IComponent`.
- [x] Do not add registry/discovery/routing/theme responsibilities to descriptor validation.
- [x] Add or update comments/docs only if source currently implies physical assembly ownership.

### Files

- [x] `BlazorShop.PresentationV2/BlazorShop.Storefront.Components/Contracts/Components/StorefrontComponentMode.cs`
- [x] `BlazorShop.PresentationV2/BlazorShop.Storefront.Components/Contracts/Components/StorefrontComponentDescriptor.cs`
- [x] `BlazorShop.PresentationV2/BlazorShop.Storefront.Components/Contracts/Components/StorefrontComponentDescriptorValidator.cs`
- [x] `BlazorShop.PresentationV2/COMPONENT-MODES.md`

### Exit Criteria

- [x] `Mode` remains semantic metadata.
- [x] No production code path requires descriptor mode to match assembly/project name; stale architecture-test coupling is isolated for H1.2.
- [x] Descriptor remains small and not a registry framework.

Implementation notes:

- 2026-08-10: `StorefrontComponentMode` kept exactly `Ssr`, `Hybrid`, and `WasmHost`; added XML summaries that describe runtime semantics rather than physical package ownership.
- 2026-08-10: `StorefrontComponentDescriptor` stayed limited to `Key`, `Mode`, `Category`, and `ComponentType`; added a summary that the descriptor is not a registry entry and does not define the component's physical assembly owner.
- 2026-08-10: `StorefrontComponentDescriptorValidator` was reviewed and left unchanged; it still validates only key shape, enum values, and `IComponent`.

## Phase H1.2 - Refactor Descriptor Ownership Tests

### Goal

Remove stale architecture-test coupling where descriptor mode must match physical mode project.

### Required Changes

In `StorefrontComponentDescriptorTests`:

- [x] Remove or replace `OwnerModeResolverMapsKnownModeAssemblies`.
- [x] Remove or replace `OwnerModeResolverTreatsUnknownEmptyOrNullAssembliesAsNotApplicable`.
- [x] Remove or replace `OwnerModeResolverTreatsNonModeComponentAssembliesAsNotApplicable`.
- [x] Remove or replace `DescriptorModeConsistencyPassesWhenDescriptorModeMatchesOwnerMode`.
- [x] Remove or replace `DescriptorModeConsistencySkipsUnknownOwnerMode`.
- [x] Remove or replace `DescriptorModeConsistencyFailsWhenDescriptorModeDiffersFromOwnerMode`.
- [x] Replace `RepositoryModeProjectDescriptorsAreValidAndOwnedByTheirModeProjects` with a semantic descriptor validity test.
- [x] Keep `RepositoryModeProjectsExposeExpectedReferenceDescriptorsOnly` only if the intention is to freeze the current reference descriptor inventory. If kept, rename/comment it as current reference inventory, not mode ownership rule.
- [ ] Keep direct descriptor tests for existing reference components:
  - [x] `BrandLogoDescriptorIsValidAndMatchesSsrMode`
  - [x] `ContactFormDescriptorIsValidAndMatchesHybridMode`
  - [x] `DiscountedProductRailDescriptorIsValidAndMatchesWasmHostMode`
- [x] Update those direct tests so they assert descriptor semantics and component type, but do not require physical owner assembly equality.
- [x] Keep `ContactFormAppDoesNotPublishPublicDescriptor` if still needed to prevent duplicate public descriptor for the nested app implementation.

### New Test Direction

Descriptor tests should prove:

- [x] invalid keys fail;
- [x] invalid enum values fail;
- [x] null or non-component type fails;
- [x] current public descriptors are valid;
- [x] duplicate public descriptor keys are not present if duplicate-key guard already exists or is easy to keep;
- [x] descriptors do not own route, render mode, theme, or registry responsibilities.

Descriptor tests should not prove:

- [x] `Components.Ssr` assembly can only publish `Ssr` descriptors;
- [x] `Components.Hybrid` assembly can only publish `Hybrid` descriptors;
- [x] `Components.WasmHost` assembly can only publish `WasmHost` descriptors;
- [x] future capability package physical location determines component mode.

### Exit Criteria

- [x] A component in a future `Components.Product` package could legally declare `Ssr`, `Hybrid`, or `WasmHost` mode without changing descriptor architecture.
- [x] Existing reference descriptors still validate.
- [x] Tests no longer preserve the historical assembly-mode equation.

Implementation notes:

- 2026-08-10: removed `OwnerModeResolver*`, `DescriptorModeConsistency*`, `StorefrontComponentDescriptorModeOwnership`, and owner-mode path inference from `StorefrontComponentDescriptorTests`.
- 2026-08-10: replaced `RepositoryModeProjectDescriptorsAreValidAndOwnedByTheirModeProjects` with `RepositoryPublicDescriptorsAreSemanticallyValid`, which validates descriptor shape, enum values, `IComponent`, and current descriptor keys without asserting descriptor mode equals assembly/project owner.
- 2026-08-10: renamed the exact descriptor inventory test to `RepositoryReferenceDescriptorInventoryMatchesCurrentMvp` so it documents current MVP descriptor inventory, not an assembly-mode law.
- 2026-08-10: added `RepositoryPublicDescriptorKeysAreUnique` and `DescriptorContractDoesNotOwnRouteRenderModeThemeOrRegistryMetadata`.
- 2026-08-10: direct descriptor tests still assert `brand-logo`/`Ssr`, `contact-form`/`Hybrid`, and `discounted-product-rail`/`WasmHost` semantics and component type, but no longer resolve owner assembly mode.
- 2026-08-10: `rg -n "OwnerMode|ResolveOwnerMode|DescriptorModeConsistency|OwnedByTheirModeProjects|StorefrontComponentDescriptorModeOwnership" BlazorShop.Tests.V2/PresentationV2/Storefront/StorefrontComponentDescriptorTests.cs` returned no matches.
- 2026-08-10: `dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~StorefrontComponentDescriptorTests"` passed: 21 passed, 0 failed. Existing MessagePack NU1902/NU1903 and Browserslist warnings remain unrelated.

## Phase H1.3 - Reclassify Components.Hybrid As Transitional

### Goal

Make `Components.Hybrid` explicit as a transitional compatibility project, not the canonical physical home for all Hybrid behavior.

### Tasks

- [x] Inventory every source file under `BlazorShop.Storefront.Components.Hybrid`.
- [x] Classify each file as:
  - [x] historical contact shell;
  - [x] descriptor compatibility;
  - [x] render-mode bridge;
  - [x] genuinely reusable Hybrid behavior;
  - [x] obsolete or H2 candidate.
- [x] Keep the project compiling if current tests/code still depend on it.
- [x] Do not delete the project in H1.
- [x] Do not move the visible V2 contact route back to the old nested Hybrid shell.
- [x] Update `BlazorShop.Storefront.Components.Hybrid.csproj` description so it does not advertise the old shell model as final.
- [x] Update `BlazorShop.Storefront.Components.Hybrid/README.md` with:
  - [x] transitional status;
  - [x] current allowed references;
  - [x] no-new-component rule for H1;
  - [x] H2 decision requirement.
- [x] Document whether `StorefrontContactFormDescriptor` remains as historical compatibility only.

### Decision Rule

Default decision for H1:

```text
Keep Components.Hybrid temporarily.
Do not add new production components there.
Do not make it the semantic definition of Hybrid.
H2 must prove whether it stays, narrows, moves, or is retired.
```

### Exit Criteria

- [x] Agents can no longer infer that every Hybrid component must live in `Components.Hybrid`.
- [x] The current contact shell remains compile-compatible if still referenced by tests.
- [x] The visible V2.WASM wrapper path remains the preferred current runtime proof.

Implementation notes:

- 2026-08-10: source inventory for `Components.Hybrid` is `_Imports.razor`, `Content/StorefrontContactForm.razor`, `Content/StorefrontContactFormDescriptor.cs`, `README.md`, and `BlazorShop.Storefront.Components.Hybrid.csproj` when excluding `bin`/`obj`.
- 2026-08-10: `_Imports.razor` is compatibility imports; `StorefrontContactForm.razor` is the historical contact shell and render-mode bridge; `StorefrontContactFormDescriptor.cs` remains historical compatibility descriptor for `contact-form`.
- 2026-08-10: no obsolete file was deleted in H1 because current tests still cover the compatibility project and H2 owns the permanent runtime proof.
- 2026-08-10: `BlazorShop.Storefront.Components.Hybrid.csproj` description now says the project is transitional pending H2 runtime proof.
- 2026-08-10: `Components.Hybrid/README.md` records transitional status, current allowed references, no-new-component rule for H1, H2 decision requirement, and the fact that visible V2 contact stays on the V2.WASM wrapper path.
- 2026-08-10: `dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.Components.Hybrid/BlazorShop.Storefront.Components.Hybrid.csproj --no-restore` passed with 0 warnings and 0 errors.

## Phase H1.4 - Rework Mode Dependency Tests

### Goal

Keep SSR and WasmHost strict while changing Hybrid from a permanent exact graph rule into a transitional compatibility rule.

### Required Changes

In `StorefrontComponentModeDependencyTests`:

- [ ] Keep `SsrReferencesExactlyComponentsAndPresentation` unless H1 discovers a concrete blocker.
- [ ] Keep `WasmHostReferencesExactlyComponentsAndBrowser`.
- [ ] Keep `ModeProjectsDoNotReferenceRuntimeClientConsumersOrBackendProjects`.
- [ ] Keep `StorefrontPackagesHaveNoProjectReferenceCycles`.
- [ ] Replace or rename `HybridReferencesExactlyComponentsPresentationAndWasmHost`.

Recommended replacement:

```text
HybridProject_RemainsTransitionalCompatibilityGraphUntilH2
```

The replacement should:

- [ ] prove current `Components.Hybrid` references only its temporary allowlist;
- [ ] explain this graph is compatibility status, not semantic Hybrid law;
- [ ] keep forbidden dependencies blocked;
- [ ] avoid claiming all future Hybrid components require Presentation + WasmHost.

### Dependency Matrix

Base `Components`:

- Allowed:
  - [ ] framework abstractions needed for contracts/headless primitives.
- Forbidden:
  - [ ] Presentation
  - [ ] Browser
  - [ ] Runtime
  - [ ] Client
  - [ ] V2
  - [ ] V2.WASM
  - [ ] Starter
  - [ ] backend/core/API projects
  - [ ] `Web.SharedV2`

`Components.Ssr`:

- Current allowed:
  - [ ] `Components`
  - [ ] `Presentation`
- Forbidden:
  - [ ] Browser
  - [ ] Runtime
  - [ ] Client
  - [ ] V2/V2.WASM
  - [ ] Starter/Starter.WASM
  - [ ] backend/core/API projects
  - [ ] `Web.SharedV2`

`Components.WasmHost`:

- Current allowed:
  - [ ] `Components`
  - [ ] `Browser`
- Forbidden:
  - [ ] Presentation
  - [ ] Runtime
  - [ ] Client
  - [ ] V2/V2.WASM
  - [ ] Starter/Starter.WASM
  - [ ] backend/core/API projects
  - [ ] `Web.SharedV2`

`Components.Hybrid` during H1:

- Status:
  - [ ] transitional compatibility project.
- Current allowed while transitional:
  - [ ] `Components`
  - [ ] `Presentation`
  - [ ] `Components.WasmHost`
- Forbidden:
  - [ ] Browser direct reference
  - [ ] Runtime
  - [ ] Client
  - [ ] V2/V2.WASM
  - [ ] Starter/Starter.WASM
  - [ ] backend/core/API projects
  - [ ] `Web.SharedV2`

### Exit Criteria

- [ ] Tests protect real dependency safety.
- [ ] Tests no longer encode old Hybrid physical shell as semantic truth.
- [ ] Future capability packaging remains possible.

## Phase H1.5 - Rework Boundary Validator Profiles

### Goal

Align `StorefrontComponentModeBoundaryValidator` with H0/H1 semantics without over-hardening future H2 experimentation.

### Tasks

- [ ] Review `StorefrontComponentModeProfiles.Ssr`.
- [ ] Review `StorefrontComponentModeProfiles.Hybrid`.
- [ ] Review `StorefrontComponentModeProfiles.WasmHost`.
- [ ] Keep SSR source-token restrictions:
  - [ ] no `HttpClient`;
  - [ ] no `IHttpClientFactory`;
  - [ ] no `IJSRuntime`;
  - [ ] no `@rendermode`;
  - [ ] no `InteractiveWebAssembly`;
  - [ ] no direct `/api/*`;
  - [ ] no Commerce Node URL/base URL.
- [ ] Keep WasmHost restrictions:
  - [ ] no Presentation reference;
  - [ ] no Runtime/Client references;
  - [ ] no direct Commerce Node URLs;
  - [ ] no `HttpClient`;
  - [ ] no `HttpContext`/`IHttpContextAccessor`;
  - [ ] no `@rendermode`.
- [ ] For Hybrid, split rule wording into:
  - [ ] current transitional project profile;
  - [ ] semantic Hybrid lifecycle notes.
- [ ] Do not add a broad anti-SignalR/WebSocket repository scanner in H1.
- [ ] Do not block valid H2 progressive enhancement experiments unless they violate the current files being protected.

### Recommended Hybrid Profile Change

Keep current transitional direct references only if the project remains:

```text
Components
Presentation
Components.WasmHost
```

But update remediation text from:

```text
Hybrid components may reference only base Components, Presentation, and Components.WasmHost.
```

to:

```text
The transitional Components.Hybrid project may reference only base Components, Presentation, and Components.WasmHost until H2 decides its permanent role. This is not the semantic definition of Hybrid mode.
```

### Exit Criteria

- [ ] Validator profiles distinguish current physical project from semantic mode.
- [ ] SSR and WasmHost remain strict.
- [ ] Hybrid does not become a permanent nested-shell rule by accident.

## Phase H1.6 - Render-Mode Ownership Review

### Goal

Ensure reusable component libraries do not own host render-mode placement.

### Tasks

- [ ] Inventory all `@rendermode` directives in:
  - [ ] `BlazorShop.Storefront.V2`
  - [ ] `BlazorShop.Storefront.V2.WASM`
  - [ ] `BlazorShop.Storefront.Components.Ssr`
  - [ ] `BlazorShop.Storefront.Components.Hybrid`
  - [ ] `BlazorShop.Storefront.Components.WasmHost`
- [ ] Classify each directive as:
  - [ ] route/page host placement;
  - [ ] V2 composition wrapper placement;
  - [ ] reusable component self-placement;
  - [ ] historical bridge.
- [ ] Preserve host/composition-owned `InteractiveWebAssembly` placement in V2/V2.WASM where it is already proven.
- [ ] SSR reusable components must not contain `@rendermode`.
- [ ] WasmHost reusable components must not contain `@rendermode`.
- [ ] The current Hybrid contact shell may temporarily contain `@rendermode` only if documented as historical bridge compatibility.
- [ ] Do not introduce `InteractiveServer` or `InteractiveAuto`.

### Exit Criteria

- [ ] One render-mode ownership rule is documented.
- [ ] V2/V2.WASM wrappers remain free to own render-mode placement.
- [ ] Reusable components stay composition-neutral where they are intended to be reused.

## Phase H1.7 - Preserve Current Reference Components

### Goal

Ensure foundation changes do not regress existing reference component behavior.

### Reference Components

- [ ] `StorefrontBrandLogo`
- [ ] `StorefrontContactForm`
- [ ] `StorefrontContactFormApp`
- [ ] `StorefrontDiscountedProductRail`
- [ ] `StorefrontContactFormSection`
- [ ] `StorefrontDiscountedProductRailSection`

### Tasks

- [ ] Build current component projects after test/doc changes.
- [ ] Keep `StorefrontBrandLogo` SSR behavior unchanged.
- [ ] Keep visible V2 contact path unchanged unless a compile blocker requires adjustment.
- [ ] Keep `StorefrontContactFormApp` Browser/BFF contract unchanged.
- [ ] Keep visible discounted rail path unchanged.
- [ ] Keep no-direct-Commerce-Node-call rule intact.
- [ ] Keep no-V2-copy/layout ownership in reusable libraries.
- [ ] If any descriptor test changes affect these components, document the reason.

### Browser QA Rule

Run Playwright/browser QA only if H1 changes:

- visible composition;
- hydration behavior;
- render-mode directive placement;
- wrapper/component path used by V2;
- browser controller invocation path.

If H1 only changes docs/tests/project metadata, browser QA may be deferred to H2.

### Exit Criteria

- [ ] Existing reference component runtime behavior remains unchanged.
- [ ] No visible V2 route is moved back to the old nested Hybrid shell.
- [ ] Browser/BFF protected interaction path remains intact.

## Phase H1.8 - Visual Ownership And Source Boundary Guardrails

### Goal

Keep H1 aligned with the visual-host architecture already established for V2, Starter, and generated storefronts.

### Tasks

- [ ] Review `StorefrontComponentVisualNeutralityTests`.
- [ ] Review `StorefrontVisualOnlyBoundaryTests`.
- [ ] Review `StorefrontVisualConsumerBoundaryValidator`.
- [ ] Confirm reusable component mode libraries still do not own:
  - [ ] literal V2 class values;
  - [ ] theme CSS;
  - [ ] Tailwind config;
  - [ ] store-specific copy;
  - [ ] generated storefront output;
  - [ ] V2 route/page layout.
- [ ] Confirm semantic `data-storefront-*` hooks remain allowed.
- [ ] Confirm host-supplied class slots remain allowed.
- [ ] Re-evaluate `StorefrontVisualOnlyBoundaryTests.F1_41_ReferenceComponentModeReferences_AreNarrowAndAdoptedOnlyByV2` if it assumes the old physical mode graph.

### Exit Criteria

- [ ] H1 does not weaken V2 visual-only ownership.
- [ ] H1 does not move visual CSS/copy/layout into shared components.
- [ ] Any changed visual boundary test describes architecture intent, not historical implementation shape.

## Phase H1.9 - Documentation Synchronization

### Goal

Update source-of-truth documentation after real code/test decisions are made.

### Files

- [ ] `BlazorShop.PresentationV2/COMPONENT-MODES.md`
- [ ] `docs/architecture/03-runtime-boundaries.md`
- [ ] `docs/architecture/05-project-and-folder-guide.md`
- [ ] `docs/architecture/10-v2-contract-ownership.md`
- [ ] `BlazorShop.PresentationV2/BlazorShop.Storefront.Components.Hybrid/README.md`
- [ ] `docs/refactor-control-Commerce-storefront/QA-StorefrontV2.todo.md` if browser-visible behavior changes
- [ ] This H1 todo file

### Tasks

- [ ] Remove wording that presents nested `Components.Hybrid -> WasmHost` shell as canonical.
- [ ] Keep historical plans historically accurate, but add superseding notes where needed.
- [ ] Document `Components.Hybrid` transitional status.
- [ ] Document that descriptor mode is semantic and physical packaging may become capability-based later.
- [ ] Document that H2 owns the permanent Hybrid proof.
- [ ] Update QA checklist only if H1 changes visible behavior or browser flow.
- [ ] Do not update `AGENTS.md` unless H1 introduces a stable rule important enough for every future agent.

### Exit Criteria

- [ ] Docs match the actual tests and project graph after H1.
- [ ] Future agents can tell current state from historical plan evidence.
- [ ] H2 has clear guidance.

## Phase H1.10 - Focused Build Gates

### Goal

Prove component mode packages and V2 host still compile after H1 changes.

### Required Builds

Run the changed-project builds first:

```powershell
dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.Components/BlazorShop.Storefront.Components.csproj --no-restore
dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.Components.Ssr/BlazorShop.Storefront.Components.Ssr.csproj --no-restore
dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.Components.Hybrid/BlazorShop.Storefront.Components.Hybrid.csproj --no-restore
dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.Components.WasmHost/BlazorShop.Storefront.Components.WasmHost.csproj --no-restore
dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM/BlazorShop.Storefront.V2.WASM.csproj --no-restore
dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.V2/BlazorShop.Storefront.V2.csproj --no-restore
```

### Optional Broader Build

Run only if restore/environment is clean enough:

```powershell
dotnet build BlazorShop.sln --no-restore
```

If broader build fails due to unrelated restore/environment issues, record the failure and keep H1 focused.

### Exit Criteria

- [ ] Every changed component mode project builds.
- [ ] V2 and V2.WASM build if they are in the affected graph.
- [ ] Any skipped broad build is documented with reason.

## Phase H1.11 - Focused Test Gates

### Goal

Prove architecture tests now protect H0/H1 intent instead of stale implementation.

### Required Focused Test Filter

Start with:

```powershell
dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~StorefrontComponentModeFoundationTests|FullyQualifiedName~StorefrontComponentModeDependencyTests|FullyQualifiedName~StorefrontComponentModeBoundaryValidatorTests|FullyQualifiedName~StorefrontComponentDescriptorTests|FullyQualifiedName~StorefrontComponentVisualNeutralityTests|FullyQualifiedName~StorefrontVisualOnlyBoundaryTests"
```

Add focused reference tests if touched or affected:

```powershell
dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~StorefrontBrandLogoComponentTests|FullyQualifiedName~StorefrontContactFormComponentTests|FullyQualifiedName~StorefrontDiscountedProductRailComponentTests|FullyQualifiedName~StorefrontBrowserContactControllerTests|FullyQualifiedName~StorefrontBrowserProductRailControllerTests"
```

### Broader Test Gate

Run if local environment is healthy:

```powershell
dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-restore
```

### Exit Criteria

- [ ] Descriptor tests pass with semantic mode behavior.
- [ ] Dependency tests pass with strict SSR/WasmHost and transitional Hybrid.
- [ ] Boundary validator tests pass.
- [ ] Visual neutrality tests pass.
- [ ] Reference component tests pass if related code was touched.
- [ ] Exact command output/result count is recorded in implementation notes.

## Phase H1.12 - Browser QA Decision Gate

### Goal

Avoid unnecessary browser QA for docs/test-only work while making browser verification mandatory for visible behavior changes.

### Browser QA Required If

- [ ] V2 page composition changes.
- [ ] V2.WASM wrapper composition changes.
- [ ] `@rendermode` placement changes.
- [ ] Hydration path changes.
- [ ] `StorefrontContactFormApp` behavior changes.
- [ ] `StorefrontDiscountedProductRail` behavior changes.
- [ ] Browser controller path changes.
- [ ] Same-origin BFF action descriptor path changes.

### Browser QA May Be Skipped If

- [ ] Only tests were refactored.
- [ ] Only docs were updated.
- [ ] Only csproj package description or README wording changed.
- [ ] No visible markup, render-mode, Browser controller, BFF endpoint, or wrapper path changed.

### Required Evidence If Browser QA Runs

- [ ] Start local V2 runtime with the existing local runner or documented equivalent.
- [ ] Verify contact form visible route hydrates and can submit through same-origin `/api/contact`.
- [ ] Verify discounted rail renders and its browser request remains same-origin.
- [ ] Verify no direct Commerce Node browser call is made.
- [ ] Capture Playwright console/network failures.
- [ ] Record exact command and result in this file.

### Exit Criteria

- [ ] Browser QA decision is documented.
- [ ] If skipped, skip reason is explicit and tied to no visible behavior change.
- [ ] If run, failures are fixed or H1 is not closed.

## Phase H1.13 - H2 Handoff Contract

### Goal

Define exactly what H2 must prove with real code and runtime evidence.

### H2 SSR Proof

H2 must include at least one real SSR reusable component or route surface proving:

- [ ] server-rendered useful HTML;
- [ ] no WASM requirement for primary behavior;
- [ ] host visual ownership;
- [ ] no Browser/Runtime/Client/backend references.

### H2 Hybrid Proof

H2 must include at least one real Hybrid proof showing:

- [ ] useful prerendered HTML before WASM interaction;
- [ ] `InteractiveWebAssembly` startup/hydration;
- [ ] browser interaction after hydration;
- [ ] protected interaction through Browser controller and same-origin BFF when needed;
- [ ] no `InteractiveServer`/`InteractiveAuto`;
- [ ] no requirement that implementation lives in `Components.Hybrid`.

### H2 WasmHost Proof

H2 must include at least one real WasmHost proof showing:

- [ ] component is in the downloadable WASM graph;
- [ ] component consumes Browser controller;
- [ ] no Presentation/Runtime/Client/backend references;
- [ ] host owns render-mode placement.

### H2 Architecture Questions To Answer

- [ ] Should `Components.Hybrid` remain after H2?
- [ ] Should `Components.Hybrid` be narrowed to compatibility only?
- [ ] Should it be retired after consumers move to V2.WASM/capability assemblies?
- [ ] Should future reusable components be organized by capability package instead of mode package?
- [ ] Which serialization boundaries appear in real Hybrid code?
- [ ] Which progressive enhancement hooks are genuinely needed?
- [ ] Which H3 guardrails should become repository-wide after runtime proof?

### Exit Criteria

- [ ] H2 can start without re-litigating H1 language.
- [ ] H2 has concrete proof requirements.
- [ ] H3 hardening is deferred until after H2 evidence.

## Phase H1.14 - Closure And Commit Plan

### Goal

Close H1 in small reviewable commits and avoid mixing docs/tests/project metadata with unrelated behavior.

### Suggested Commit Breakdown

```text
docs(storefront): record component mode foundation v2 baseline
test(storefront): decouple component descriptor mode from project ownership
test(storefront): align component mode dependency guardrails
docs(storefront): clarify transitional hybrid component project
docs(storefront): close component mode foundation v2
```

If production code changes are required, separate them from test/doc-only commits:

```text
refactor(storefront): clarify hybrid component project metadata
```

### Closure Checklist

- [ ] H1 baseline notes are filled in.
- [ ] Descriptor mode is semantic.
- [ ] Descriptor tests no longer enforce physical assembly ownership.
- [ ] SSR dependency boundary remains explicit.
- [ ] WasmHost dependency boundary remains explicit.
- [ ] Hybrid project status is transitional or otherwise explicitly decided.
- [ ] Boundary validator wording matches the selected status.
- [ ] Existing reference components still compile.
- [ ] V2 visible contact and discounted rail paths are unchanged unless documented.
- [ ] Browser QA decision is documented.
- [ ] Focused build/test commands and results are recorded.
- [ ] Documentation is synchronized.
- [ ] H2 handoff is complete.
- [ ] No backend, Control Plane, StorefrontBuilder, Product/Catalog/Cart extraction, plugin/theme/registry, or visual rewrite work entered H1.

## Risks And Mitigations

| Risk | Why It Matters | Mitigation |
| --- | --- | --- |
| Tests keep encoding assembly-mode ownership | Future capability packages cannot contain mixed semantic modes | Replace owner-mode tests with semantic descriptor validity tests |
| H1 deletes `Components.Hybrid` too early | Existing historical descriptor/tests may break without runtime proof | Keep transitional project until H2 evidence |
| H1 blesses current `Components.Hybrid` graph forever | Old nested shell assumption survives under new wording | Rename tests/docs as transitional compatibility |
| H1 weakens browser/backend safety | WASM code could call Commerce Node directly | Preserve WasmHost/Browser/BFF guardrails |
| H1 over-hardens SignalR/WebSocket scans | H2 experimentation becomes blocked by speculative rules | Defer broad anti-drift scanner to H3 |
| H1 becomes component extraction | Scope balloons and destabilizes V2 | Keep Product/Catalog/Cart suites forbidden |
| Docs and tests disagree | Future agents follow stale plans | Sync docs only after real code/test changes |

## Definition Of Done

H1 is complete only when all items below are true.

### Semantic Architecture

- [ ] `Ssr`, `Hybrid`, and `WasmHost` have clear runtime meanings.
- [ ] `Hybrid` is not defined by `Components.Hybrid` physical location.
- [ ] `Hybrid` is not defined as mandatory nested server-shell to WasmHost composition.
- [ ] `StorefrontComponentMode` metadata is semantic.

### SSR

- [ ] SSR dependency graph is explicit.
- [ ] SSR has no browser runtime requirement.
- [ ] SSR tests protect server-safe ownership.

### WasmHost

- [ ] WasmHost is browser-safe.
- [ ] WasmHost uses Browser controllers for protected browser interactions.
- [ ] WasmHost does not reference Presentation, Runtime, Client, V2, Starter, backend/core, or `Web.SharedV2`.
- [ ] WasmHost does not self-own render mode.

### Hybrid

- [ ] Current `Components.Hybrid` project has explicit transitional/permanent status.
- [ ] H1 does not force a permanent physical model without H2 evidence.
- [ ] Hybrid prerender/WASM lifecycle is documented.
- [ ] Host/composition owns render-mode placement.
- [ ] Browser-executed dependencies remain browser-safe.

### Descriptor Architecture

- [ ] Descriptor/project-name coupling is removed or explicitly marked historical compatibility.
- [ ] Descriptors remain minimal.
- [ ] No component registry/scanner/plugin/theme framework is introduced.
- [ ] Future capability-based project organization remains possible.

### Tests

- [ ] Dependency architecture tests pass.
- [ ] Visual neutrality tests pass.
- [ ] Descriptor tests pass.
- [ ] Render-mode ownership tests pass.
- [ ] Existing reference component tests pass if touched.
- [ ] Full relevant V2 test gate passes or any skip is documented.

### Scope

- [ ] No Phase 3 component extraction occurred.
- [ ] No Product/Catalog capability module was introduced.
- [ ] No StorefrontBuilder migration occurred.
- [ ] No backend feature work occurred.
- [ ] No unnecessary runtime behavior changes occurred.

### H2 Readiness

- [ ] H2 has a clear SSR MVP requirement.
- [ ] H2 has a clear Hybrid runtime proof requirement.
- [ ] H2 has a clear WasmHost proof requirement.
- [ ] H2 owns final evidence for H3 guardrail hardening.

## Autoplan Decision Audit Trail

| # | Decision | Classification | Principle | Rationale | Rejected |
| --- | --- | --- | --- | --- | --- |
| 1 | Update the existing H1 todo instead of creating a duplicate file | Auto-decided | Single source of truth | A same-name H1 backlog already exists and is referenced by H0 | Create another competing H1 plan |
| 2 | Treat H1 as semantic decoupling and guardrail realignment | Auto-decided | Minimal blast radius | Production descriptor contracts are already minimal; stale coupling is mostly in tests/docs | Rewrite component model |
| 3 | Keep `Components.Hybrid` transitional during H1 | Auto-decided | Preserve working system | Current code/tests still contain historical contact shell; deletion needs H2 runtime evidence | Delete/rename project in H1 |
| 4 | Keep SSR and WasmHost strict, relax only stale Hybrid physical assumptions | Auto-decided | Protect real boundaries | SSR/WasmHost graphs match intended safety; Hybrid graph is historical | Relax all mode boundaries |
| 5 | Browser QA only if visible runtime behavior changes | Auto-decided | Evidence proportional to change | Test/doc/metadata changes do not exercise hydration; visible composition changes do | Always run browser QA for docs-only work |

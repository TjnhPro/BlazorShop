# Storefront Component Mode Foundation

Status: in-progress
Owner: Storefront V2 architecture
Scope: Phase 1 foundation only

## Goal

Create the architecture foundation for reusable Storefront component libraries with explicit render/runtime modes:

- `Ssr`
- `Hybrid`
- `WasmHost`

This phase must make the mode boundaries obvious in project structure, contracts, documentation, and architecture tests. It must not implement real reusable storefront feature components yet.

## Current Codebase Facts

- `BlazorShop.Storefront.Components` is currently a logic-only package using `Microsoft.NET.Sdk`.
- `BlazorShop.Storefront.Components` currently owns `Contracts`, `Headless`, and browser-safe primitive contracts only.
- `BlazorShop.Storefront.Components/Features` has been retired and must not return.
- `BlazorShop.Storefront.Browser` is a Razor class library that owns browser-side controllers and same-origin local API primitives.
- `BlazorShop.Storefront.Presentation` owns shared route composition, page contexts, BFF endpoints, SEO, media, and Runtime-backed adapters.
- `BlazorShop.Storefront.V2` and `BlazorShop.Storefront.Starter` own visual templates, assets, copy, and host view registrations.
- `BlazorShop.Storefront.V2.WASM` and `BlazorShop.Storefront.Starter.WASM` are bootable WASM client assemblies for interactive host-owned visual components.
- Existing tests already guard Components logic-only behavior, no `Features` folder, Presentation visual neutrality, visual consumer boundaries, and package role boundaries.

## Target Projects

Add these sibling projects under `BlazorShop.PresentationV2/`:

- `BlazorShop.Storefront.Components.Ssr`
- `BlazorShop.Storefront.Components.Hybrid`
- `BlazorShop.Storefront.Components.WasmHost`

Do not add:

- `BlazorShop.Storefront.Components.Common`
- `BlazorShop.Storefront.Features.Contracts`
- `BlazorShop.Storefront.ComponentRuntime`
- `BlazorShop.Storefront.ComponentRegistry`
- runtime plugin or discovery projects

## Correct Dependency Graph

Direct project references only:

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

Important:

- `BlazorShop.Storefront.Components` must not reference `Presentation`, `Browser`, `Runtime`, `Client`, V2, backend, or Control Plane projects.
- `BlazorShop.Storefront.Presentation` must not reference the new mode projects in Phase 1.
- `BlazorShop.Storefront.V2`, `Starter`, and generated storefronts must not reference the new mode projects until real components are implemented and adopted in a later phase.
- No circular references are allowed.

## Mode Semantics

### SSR

`Ssr` components render completely on the server and do not require browser runtime for their primary function.

Allowed:

- prepared Presentation contexts
- semantic render/input contracts
- normal Razor forms and links
- `RenderFragment`
- accessibility markup
- `data-storefront-*` semantic hooks
- class parameters or class descriptor parameters supplied by the host

Forbidden:

- `BlazorShop.Storefront.Browser`
- `BlazorShop.Storefront.Runtime`
- `BlazorShop.Storefront.Client`
- `BlazorShop.Storefront.V2`
- `BlazorShop.Storefront.V2.WASM`
- `BlazorShop.Storefront.Starter`
- `BlazorShop.Storefront.Starter.WASM`
- Commerce Node, Control Plane, Application, Domain, Infrastructure
- `HttpClient`
- `IJSRuntime`
- `@rendermode`
- `InteractiveWebAssembly`
- direct `/api/*` or Commerce Node URLs

### Hybrid

`Hybrid` components are server-owned shells that can prepare SSR structure, initial browser state, antiforgery/form contracts, and host a `WasmHost` child.

Allowed:

- `BlazorShop.Storefront.Components`
- `BlazorShop.Storefront.Presentation`
- `BlazorShop.Storefront.Components.WasmHost`
- server-side initial state
- semantic action descriptors
- form/action descriptors supplied by Presentation
- `@rendermode` bridge when hosting a WasmHost child

Forbidden:

- direct `BlazorShop.Storefront.Browser` reference
- `BlazorShop.Storefront.Runtime`
- `BlazorShop.Storefront.Client`
- Commerce Node, Control Plane, Application, Domain, Infrastructure
- direct `HttpClient`
- direct backend/API routes
- browser controller injection
- direct `IJSRuntime` behavior
- theme CSS, store-specific copy, or V2 layout ownership

### WasmHost

`WasmHost` components are browser-interactive feature roots that run in WASM and call Browser controllers. They do not self-own route or render-mode hosting.

Allowed:

- `BlazorShop.Storefront.Components`
- `BlazorShop.Storefront.Browser`
- browser controllers
- browser-safe state and action contracts
- `EventCallback`
- component lifecycle for interaction
- `IJSRuntime` only for real browser behavior

Forbidden:

- `BlazorShop.Storefront.Presentation`
- `BlazorShop.Storefront.Runtime`
- `BlazorShop.Storefront.Client`
- `BlazorShop.Storefront.V2`
- `BlazorShop.Storefront.V2.WASM`
- `BlazorShop.Storefront.Starter`
- `BlazorShop.Storefront.Starter.WASM`
- Commerce Node, Control Plane, Application, Domain, Infrastructure
- `HttpContext`
- `IHttpContextAccessor`
- `HttpClient`
- direct `/api/*`
- direct `api/storefront/*`
- localhost/backend URLs
- Presentation service injection

Required data path:

```text
WasmHost component
  -> Browser controller
  -> same-origin Presentation/BFF endpoint
  -> Runtime
  -> Commerce Node Storefront API
```

Forbidden data path:

```text
WasmHost component
  -> HttpClient
  -> Commerce Node Storefront API
```

## Out Of Scope

Do not implement these real components in this phase:

- `StorefrontBrandLogo`
- `StorefrontContactForm`
- `StorefrontContactFormApp`
- `StorefrontDiscountedProductRail`

Do not implement:

- new contact APIs
- discounted product APIs
- new Browser controllers for feature components
- V2 component migration
- Starter component migration
- StorefrontBuilder generator changes
- generated storefront adoption
- component registry
- assembly scanning
- reflection discovery
- JSON manifest engine
- drag/drop builder
- AI component catalog
- Playwright browser QA

## Phase 0 - Baseline And Scope Lock

- [x] Read `AGENTS.md`.
- [x] Read `docs/architecture/README.md`.
- [x] Read `docs/architecture/05-project-and-folder-guide.md`.
- [x] Read `docs/architecture/10-v2-contract-ownership.md`.
- [x] Confirm current project list under `BlazorShop.PresentationV2`.
- [x] Confirm `BlazorShop.Storefront.Components` still uses `Microsoft.NET.Sdk`.
- [x] Confirm `BlazorShop.Storefront.Components` still has no `.razor` files.
- [x] Confirm `BlazorShop.Storefront.Components/Features` does not exist.
- [x] Confirm `BlazorShop.Storefront.Browser` references only `Storefront.Components` plus required browser packages.
- [x] Confirm `BlazorShop.Storefront.Presentation` owns route contexts and BFF composition.
- [x] Confirm `V2` and `Starter` are visual hosts only.
- [x] Record the exact baseline in this todo under "Implementation Notes" before coding.

Exit criteria:

- [x] Scope is locked as foundation-only.
- [x] No real feature component is included in the implementation task list.
- [x] The dependency graph above is accepted as the implementation target.

## Phase 1 - Documentation Source Of Truth

- [x] Add `BlazorShop.PresentationV2/COMPONENT-MODES.md`.
- [x] Document `Ssr`, `Hybrid`, and `WasmHost` definitions.
- [x] Document the exact dependency graph.
- [x] Document the project-reference allowlist per mode.
- [x] Document the package-reference allowlist per mode.
- [x] Document render-mode ownership:
  - `Ssr` must not use `@rendermode`.
  - `Hybrid` may host a WasmHost child with `@rendermode`.
  - `WasmHost` must not self-own `@rendermode`.
- [x] Document data ownership:
  - Presentation prepares server context.
  - Browser owns same-origin browser controllers.
  - Runtime remains server/BFF only.
  - Component libraries do not call Commerce Node directly.
- [x] Document visual ownership:
  - reusable component libraries may expose semantic hooks and class slots.
  - reusable component libraries do not own theme CSS, V2 layout, store copy, or generated output.
- [x] Document naming conventions:
  - no mode prefix in component names.
  - namespaces grouped by mode and category.
- [x] Document first future reference components as next phase examples only.
- [x] Update `docs/architecture/05-project-and-folder-guide.md` with the new mode project ownership rules.
- [x] Update `docs/architecture/10-v2-contract-ownership.md` with the descriptor and mode boundary rule.
- [ ] Update `AGENTS.md` active V2 presentation/runtime list after projects are created.

Exit criteria:

- [x] Architecture docs explain where each mode belongs.
- [x] Docs explicitly state that `Components/Features` remains retired.
- [x] Docs explicitly state that Phase 1 does not add real components.

## Phase 2 - Descriptor Contracts In Base Components

- [ ] Add descriptor contracts under `BlazorShop.Storefront.Components/Contracts/Components/`.
- [ ] Add `StorefrontComponentMode`.
- [ ] Add `StorefrontComponentCategory`.
- [ ] Add `StorefrontComponentDescriptor`.
- [ ] Add a minimal `StorefrontComponentDescriptorValidator` or equivalent pure helper.
- [ ] Validate `Key` is required.
- [ ] Validate `Key` matches lowercase kebab-case:

```text
^[a-z0-9]+(?:-[a-z0-9]+)*$
```

- [ ] Validate `Mode` is a defined enum value.
- [ ] Validate `Category` is a defined enum value.
- [ ] Validate `ComponentType` is not null.
- [ ] Validate `ComponentType` implements `Microsoft.AspNetCore.Components.IComponent`.
- [ ] Verify base `Components` can reference the required Blazor component abstraction without switching to `Microsoft.NET.Sdk.Razor`.
- [ ] If a package/framework reference is required for `IComponent`, keep it minimal and document why.
- [ ] Do not add descriptor assembly scanning.
- [ ] Do not add DI registration.
- [ ] Do not add runtime component registry.
- [ ] Do not add JSON manifests.

Exit criteria:

- [ ] Base `Components` still builds.
- [ ] Base `Components` still uses `Microsoft.NET.Sdk`.
- [ ] Base `Components` still has no `.razor`, CSS, JS, theme assets, or `Features` folder.

## Phase 3 - Create Mode RCL Projects

- [ ] Create `BlazorShop.Storefront.Components.Ssr`.
- [ ] Create `BlazorShop.Storefront.Components.Hybrid`.
- [ ] Create `BlazorShop.Storefront.Components.WasmHost`.
- [ ] Use `Microsoft.NET.Sdk.Razor` for all three mode libraries.
- [ ] Target `net10.0`.
- [ ] Enable nullable reference types.
- [ ] Enable implicit usings.
- [ ] Set `PackageId`, `Version`, `Authors`, `Description`, and `RepositoryUrl` consistent with existing Storefront packages.
- [ ] Add ownership `README.md` to each mode project.
- [ ] Add `_Imports.razor` only if needed for future Razor components, and keep imports mode-safe.
- [ ] Do not create production dummy components.
- [ ] Do not create `SsrTestComponent`, `HybridTestComponent`, or `WasmHostTestComponent` in production projects.
- [ ] Use test fixtures for validator negative/positive examples instead of production dummy components.
- [ ] Add all three projects to `BlazorShop.sln`.
- [ ] Do not add the new projects to V2, Starter, generated storefronts, or Presentation references in this phase.

Project references:

- [ ] `Components.Ssr` references only:
  - `../BlazorShop.Storefront.Components/BlazorShop.Storefront.Components.csproj`
  - `../BlazorShop.Storefront.Presentation/BlazorShop.Storefront.Presentation.csproj`
- [ ] `Components.Hybrid` references only:
  - `../BlazorShop.Storefront.Components/BlazorShop.Storefront.Components.csproj`
  - `../BlazorShop.Storefront.Presentation/BlazorShop.Storefront.Presentation.csproj`
  - `../BlazorShop.Storefront.Components.WasmHost/BlazorShop.Storefront.Components.WasmHost.csproj`
- [ ] `Components.WasmHost` references only:
  - `../BlazorShop.Storefront.Components/BlazorShop.Storefront.Components.csproj`
  - `../BlazorShop.Storefront.Browser/BlazorShop.Storefront.Browser.csproj`

Exit criteria:

- [ ] All three projects build empty/minimal.
- [ ] No new project references point from base `Components`, `Presentation`, `Browser`, V2, Starter, or generated storefronts back to these mode projects.

## Phase 4 - Mode Boundary Validator

- [ ] Add `StorefrontComponentModeBoundaryValidator` under `BlazorShop.Tests.V2/PresentationV2/Storefront/`.
- [ ] Do not overload `StorefrontVisualConsumerBoundaryValidator`.
- [ ] Add `StorefrontComponentModeProfile`.
- [ ] Add `StorefrontComponentModeBoundaryViolation`.
- [ ] Validate `.csproj` project references through strict allowlists.
- [ ] Validate package references through strict allowlists.
- [ ] Validate source tokens by mode.
- [ ] Enumerate source extensions:
  - `.cs`
  - `.razor`
  - `.cshtml`
  - `.js`
  - `.mjs`
  - `.ts`
  - `.json`
  - `.yaml`
  - `.yml`
  - `.css`
  - `.scss`
  - `.sass`
  - `.less`
- [ ] Ignore `bin`, `obj`, `node_modules`, generated build output, and package output.
- [ ] Report violation path, forbidden token/reference, owner, and remediation message.
- [ ] Keep validator deterministic with sorted output.

SSR forbidden references:

- [ ] `BlazorShop.Storefront.Browser`
- [ ] `BlazorShop.Storefront.Runtime`
- [ ] `BlazorShop.Storefront.Client`
- [ ] `BlazorShop.Storefront.V2`
- [ ] `BlazorShop.Storefront.V2.WASM`
- [ ] `BlazorShop.Storefront.Starter`
- [ ] `BlazorShop.Storefront.Starter.WASM`
- [ ] `BlazorShop.CommerceNode.API`
- [ ] `BlazorShop.ControlPlane`
- [ ] `BlazorShop.Application`
- [ ] `BlazorShop.Domain`
- [ ] `BlazorShop.Infrastructure`
- [ ] `BlazorShop.Web.SharedV2`

Hybrid forbidden direct references:

- [ ] `BlazorShop.Storefront.Browser`
- [ ] `BlazorShop.Storefront.Runtime`
- [ ] `BlazorShop.Storefront.Client`
- [ ] `BlazorShop.Storefront.V2`
- [ ] `BlazorShop.Storefront.V2.WASM`
- [ ] `BlazorShop.Storefront.Starter`
- [ ] `BlazorShop.Storefront.Starter.WASM`
- [ ] backend/core/API projects
- [ ] `BlazorShop.Web.SharedV2`

WasmHost forbidden references:

- [ ] `BlazorShop.Storefront.Presentation`
- [ ] `BlazorShop.Storefront.Runtime`
- [ ] `BlazorShop.Storefront.Client`
- [ ] `BlazorShop.Storefront.V2`
- [ ] `BlazorShop.Storefront.V2.WASM`
- [ ] `BlazorShop.Storefront.Starter`
- [ ] `BlazorShop.Storefront.Starter.WASM`
- [ ] backend/core/API projects
- [ ] `BlazorShop.Web.SharedV2`

SSR forbidden source tokens:

- [ ] `HttpClient`
- [ ] `IHttpClientFactory`
- [ ] `IJSRuntime`
- [ ] `JSImport`
- [ ] `@rendermode`
- [ ] `InteractiveWebAssembly`
- [ ] `InteractiveServer`
- [ ] `"/api/`
- [ ] `'/api/`
- [ ] `api/storefront`
- [ ] `localhost:`
- [ ] `CommerceNodeBaseUrl`
- [ ] `StorefrontLocalApiClient`

Hybrid forbidden source tokens:

- [ ] `HttpClient`
- [ ] `IHttpClientFactory`
- [ ] `IJSRuntime`
- [ ] `JSImport`
- [ ] `"/api/`
- [ ] `'/api/`
- [ ] `api/storefront`
- [ ] `localhost:`
- [ ] `CommerceNodeBaseUrl`
- [ ] `StorefrontLocalApiClient`
- [ ] `IStorefrontBrowser`

Hybrid allowed source tokens:

- [ ] `@rendermode`
- [ ] `InteractiveWebAssembly`
- [ ] `BlazorShop.Storefront.Components.WasmHost`

WasmHost forbidden source tokens:

- [ ] `HttpClient`
- [ ] `IHttpClientFactory`
- [ ] `HttpContext`
- [ ] `IHttpContextAccessor`
- [ ] `"/api/`
- [ ] `'/api/`
- [ ] `api/storefront`
- [ ] `localhost:`
- [ ] `CommerceNodeBaseUrl`
- [ ] `BlazorShop.Storefront.Presentation`
- [ ] `IStorefrontRuntime`
- [ ] `IStorefrontCatalogClient`
- [ ] `IStorefrontCartClient`
- [ ] `IStorefrontCheckoutClient`
- [ ] `IStorefrontCustomerClient`

WasmHost allowed source tokens:

- [ ] `IJSRuntime`
- [ ] `EventCallback`
- [ ] browser controller interfaces from `BlazorShop.Storefront.Browser`

Exit criteria:

- [ ] Validator can pass clean source.
- [ ] Validator gives actionable violation messages.
- [ ] Validator has profile-specific allowlists, not a global forbidden-only scan.

## Phase 5 - Architecture Tests

Add focused tests under `BlazorShop.Tests.V2/PresentationV2/Storefront/`.

### StorefrontComponentModeFoundationTests

- [ ] Assert all three project directories exist.
- [ ] Assert all three `.csproj` files exist.
- [ ] Assert all three projects use `Microsoft.NET.Sdk.Razor`.
- [ ] Assert all three projects target `net10.0`.
- [ ] Assert package metadata exists and matches Storefront package conventions.
- [ ] Assert all three projects are included in `BlazorShop.sln`.
- [ ] Assert base `Components` still uses `Microsoft.NET.Sdk`.
- [ ] Assert base `Components` still has no `.razor` files.
- [ ] Assert base `Components/Features` still does not exist.
- [ ] Assert `Presentation`, V2, V2.WASM, Starter, and Starter.WASM do not reference the new mode projects in Phase 1.

### StorefrontComponentModeDependencyTests

- [ ] Assert `Components.Ssr` references exactly Components and Presentation.
- [ ] Assert `Components.Hybrid` references exactly Components, Presentation, and Components.WasmHost.
- [ ] Assert `Components.WasmHost` references exactly Components and Browser.
- [ ] Assert `Components.WasmHost` does not reference Presentation.
- [ ] Assert no mode project references Runtime or Client.
- [ ] Assert no mode project references V2 or Starter consumer projects.
- [ ] Assert no mode project references backend/core/API projects.
- [ ] Assert no project-reference cycle exists among Storefront packages.

### StorefrontComponentModeBoundaryValidatorTests

- [ ] Positive fixture: SSR profile with allowed refs/source passes.
- [ ] Positive fixture: Hybrid profile with allowed WasmHost reference and `@rendermode` bridge passes.
- [ ] Positive fixture: WasmHost profile with Browser controller dependency and `IJSRuntime` passes.
- [ ] Negative fixture: SSR with `IJSRuntime` fails.
- [ ] Negative fixture: SSR referencing Browser fails.
- [ ] Negative fixture: SSR using `@rendermode` fails.
- [ ] Negative fixture: Hybrid referencing Browser directly fails.
- [ ] Negative fixture: Hybrid injecting `HttpClient` fails.
- [ ] Negative fixture: Hybrid injecting browser controller directly fails.
- [ ] Negative fixture: WasmHost referencing Presentation fails.
- [ ] Negative fixture: WasmHost injecting `HttpClient` fails.
- [ ] Negative fixture: WasmHost calling `/api/storefront` fails.
- [ ] Negative fixture: any mode referencing V2 fails.
- [ ] Negative fixture: any mode referencing backend/core/API projects fails.

### StorefrontComponentDescriptorTests

- [ ] Valid descriptor passes.
- [ ] Empty key fails.
- [ ] Whitespace key fails.
- [ ] Uppercase key fails.
- [ ] Snake case key fails.
- [ ] Key with slash fails.
- [ ] Key with dot fails.
- [ ] Key with double dash fails.
- [ ] Invalid mode enum value fails.
- [ ] Invalid category enum value fails.
- [ ] Null component type fails.
- [ ] Type not implementing `IComponent` fails.
- [ ] Razor component fixture implementing `IComponent` passes.

### StorefrontComponentVisualNeutralityTests

- [ ] Assert no `.css`, `.scss`, `.sass`, `.less` files exist in the three mode projects.
- [ ] Assert no `tailwind.config.*` exists in the three mode projects.
- [ ] Assert no `postcss.config.*` exists in the three mode projects.
- [ ] Assert no `wwwroot/css` or theme asset folder exists in the three mode projects.
- [ ] Assert no literal `class="rounded`, `class="bg-`, `class="text-`, `class="shadow`, `class="grid`, `class="flex`, `class="px-`, `class="mx-`, or responsive Tailwind prefixes exist.
- [ ] Allow `class="@..."`
- [ ] Allow `data-storefront-*`
- [ ] Assert no final storefront copy strings are introduced.
- [ ] Assert no V2 CSS classes or V2 asset paths are referenced.

Exit criteria:

- [ ] New tests fail before implementation where appropriate.
- [ ] New tests pass after implementation.
- [ ] Existing Components headless tests still pass.

## Phase 6 - QA Checklist And Closure Gates

- [ ] Update `docs/refactor-control-Commerce-storefront/QA-StorefrontV2.todo.md` with a new section for Component Mode Foundation.
- [ ] Add checklist item for base Components role preserved.
- [ ] Add checklist item for three mode projects created.
- [ ] Add checklist item for exact dependency allowlists.
- [ ] Add checklist item for descriptor validation.
- [ ] Add checklist item for visual neutrality.
- [ ] Add checklist item for no real feature components in Phase 1.
- [ ] Add checklist item explaining why Playwright is not required for this phase.

Build gate:

```powershell
dotnet build BlazorShop.PresentationV2\BlazorShop.Storefront.Components\BlazorShop.Storefront.Components.csproj --no-restore
dotnet build BlazorShop.PresentationV2\BlazorShop.Storefront.Presentation\BlazorShop.Storefront.Presentation.csproj --no-restore
dotnet build BlazorShop.PresentationV2\BlazorShop.Storefront.Browser\BlazorShop.Storefront.Browser.csproj --no-restore
dotnet build BlazorShop.PresentationV2\BlazorShop.Storefront.Components.Ssr\BlazorShop.Storefront.Components.Ssr.csproj --no-restore
dotnet build BlazorShop.PresentationV2\BlazorShop.Storefront.Components.WasmHost\BlazorShop.Storefront.Components.WasmHost.csproj --no-restore
dotnet build BlazorShop.PresentationV2\BlazorShop.Storefront.Components.Hybrid\BlazorShop.Storefront.Components.Hybrid.csproj --no-restore
```

Focused test gate:

```powershell
dotnet test BlazorShop.Tests.V2\BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~StorefrontComponentModeFoundationTests|FullyQualifiedName~StorefrontComponentModeDependencyTests|FullyQualifiedName~StorefrontComponentModeBoundaryValidatorTests|FullyQualifiedName~StorefrontComponentDescriptorTests|FullyQualifiedName~StorefrontComponentVisualNeutralityTests|FullyQualifiedName~StorefrontComponentsHeadlessPresentationRefactorTests|FullyQualifiedName~StorefrontSharedPlatformPackageContractTests|FullyQualifiedName~StorefrontPageCompositionGuardrailTests"
```

No Playwright gate:

- [ ] Do not run Playwright for Phase 1 unless a real browser-visible component is accidentally added.
- [ ] If a browser-visible component is added, stop and split it into the next phase.

Exit criteria:

- [ ] All build gates pass.
- [ ] Focused architecture tests pass.
- [ ] QA checklist is updated with evidence.
- [ ] No browser QA is required because no real browser behavior changed.

## Phase 7 - Final Audit Before Commit

- [ ] Run `git status --short`.
- [ ] Verify no unrelated user changes were modified.
- [ ] Verify no production dummy components exist.
- [ ] Verify no `Components/Features` folder exists.
- [ ] Verify no V2/Starter/generated project references the new mode projects.
- [ ] Verify no Runtime/Client/backend reference exists in any mode project.
- [ ] Verify base Components still has no `.razor` files.
- [ ] Verify docs mention future components only as next phase.
- [ ] Verify this todo has implementation evidence filled in.
- [ ] Commit with a focused message after tests pass.

Suggested commit message:

```text
chore: add storefront component mode foundation
```

## Definition Of Done

- [ ] `BlazorShop.Storefront.Components` remains the lowest browser-safe contracts/headless layer.
- [ ] `BlazorShop.Storefront.Components` still has no Razor components.
- [ ] `BlazorShop.Storefront.Components/Features` remains absent.
- [ ] `StorefrontComponentMode` exists.
- [ ] `StorefrontComponentCategory` exists.
- [ ] `StorefrontComponentDescriptor` exists.
- [ ] Descriptor validation is tested.
- [ ] `Components.Ssr` exists and has strict dependency rules.
- [ ] `Components.Hybrid` exists and has strict dependency rules.
- [ ] `Components.WasmHost` exists and has strict dependency rules.
- [ ] SSR cannot use Browser, Runtime, Client, JS interop, or render mode.
- [ ] Hybrid can bridge to WasmHost but cannot own browser transport.
- [ ] WasmHost can use Browser controllers but cannot call backend APIs directly.
- [ ] No new mode project references V2, Starter, Commerce Node, Control Plane, Application, Domain, Infrastructure, Runtime, Client, or Web.SharedV2.
- [ ] No theme CSS/assets are introduced in reusable mode libraries.
- [ ] Literal visual classes are rejected in the new mode libraries.
- [ ] Documentation explains mode ownership and examples.
- [ ] QA checklist is updated.
- [ ] Focused build/test gates pass.
- [ ] No real feature implementation is included.

## Next Phase Preview

After this foundation is green, the next phase should implement exactly three reference components to prove the modes:

- `StorefrontBrandLogo` in `Components.Ssr`
- `StorefrontContactForm` in `Components.Hybrid`
- `StorefrontContactFormApp` in `Components.WasmHost`
- `StorefrontDiscountedProductRail` in `Components.WasmHost`

The next phase must include Playwright only after these components are rendered through V2 or another browser-visible host.

## Implementation Notes

- [x] Baseline evidence:
  - 2026-08-09: read `AGENTS.md`, `docs/architecture/README.md`, `docs/architecture/05-project-and-folder-guide.md`, and `docs/architecture/10-v2-contract-ownership.md`; ASP.NET/Blazor skill guidance read from `C:\Users\admin\.codex\skills\aspnet-core\SKILL.md` and `references\ui-blazor.md`.
  - 2026-08-09: current `BlazorShop.PresentationV2` projects are ControlPlane API/Web, CommerceNode API, Storefront Browser/Client/Components/Presentation/Runtime/Starter/Starter.WASM/V2/V2.WASM, and Web.SharedV2. No component mode projects existed before Phase 1.
  - 2026-08-09: `BlazorShop.Storefront.Components` uses `Microsoft.NET.Sdk`, targets `net10.0`, has nullable/implicit usings enabled, has no `.razor` files, and `BlazorShop.Storefront.Components/Features` does not exist.
  - 2026-08-09: `BlazorShop.Storefront.Browser` uses `Microsoft.NET.Sdk.Razor`, references only `BlazorShop.Storefront.Components` by project reference plus `Microsoft.AspNetCore.Components.WebAssembly`.
  - 2026-08-09: Presentation owns `Pages`, `Endpoints`, `Services`, `Views/Foundation`, and `MapStorefrontPresentation`; V2 and Starter register `StorefrontFoundationViewSet` through `V2FoundationViewRegistration` and `StarterFoundationViewRegistration` and remain visual hosts.
  - 2026-08-09: scope locked as foundation-only; no `StorefrontBrandLogo`, contact form, discounted rail, production dummy component, registry, scanner, Playwright, or generator adoption is in scope.
- [x] Docs updated:
  - 2026-08-09: added `BlazorShop.PresentationV2/COMPONENT-MODES.md` with SSR, Hybrid, and WasmHost definitions, dependency graph, project/package allowlists, render-mode/data/visual ownership, naming conventions, and future examples only.
  - 2026-08-09: updated `docs/architecture/05-project-and-folder-guide.md` with ownership rules and direct reference allowlists for `Components.Ssr`, `Components.Hybrid`, and `Components.WasmHost`.
  - 2026-08-09: updated `docs/architecture/10-v2-contract-ownership.md` with descriptor contract ownership and mode boundary rules.
  - 2026-08-09: deferred the `AGENTS.md` active project list update until Phase 3 because that checklist explicitly says to update it after projects are created.
- [ ] Projects added:
- [ ] Tests added:
- [ ] Build evidence:
- [ ] Focused test evidence:
- [ ] QA checklist evidence:
- [ ] Commit:

## Decision Audit Trail

| # | Decision | Classification | Rationale | Rejected |
|---|---|---|---|---|
| 1 | Create three explicit mode projects instead of reusing `Components/Features`. | Architecture | Existing codebase retired `Components/Features`; mode projects make SSR/Hybrid/WasmHost ownership visible without turning base Components into a visual library. | Reopen `Features` folder. |
| 2 | Keep base `Components` as `Microsoft.NET.Sdk` and place only descriptor contracts there. | Boundary | Current tests and docs define Components as browser-safe contracts/headless primitives, not Razor component ownership. | Convert base Components to Razor SDK. |
| 3 | Do not reference mode projects from Presentation, V2, Starter, or generated storefronts in Phase 1. | Risk control | Phase 1 is foundation-only; adoption belongs to real component implementation. | Add unused host references now. |
| 4 | Add a new mode boundary validator instead of overloading the visual consumer validator. | Test design | Existing visual consumer validator has host-specific assumptions; reusable mode libraries need different allowlists. | Extend `StorefrontVisualConsumerBoundaryValidator` directly. |
| 5 | No Playwright in Phase 1. | QA scope | No browser-visible behavior changes if no real components are implemented. | Run browser QA without a visible feature surface. |

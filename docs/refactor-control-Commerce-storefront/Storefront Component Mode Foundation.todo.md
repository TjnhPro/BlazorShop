# Storefront Component Mode Foundation

Status: complete
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
- [x] Update `AGENTS.md` active V2 presentation/runtime list after projects are created.

Exit criteria:

- [x] Architecture docs explain where each mode belongs.
- [x] Docs explicitly state that `Components/Features` remains retired.
- [x] Docs explicitly state that Phase 1 does not add real components.

## Phase 2 - Descriptor Contracts In Base Components

- [x] Add descriptor contracts under `BlazorShop.Storefront.Components/Contracts/Components/`.
- [x] Add `StorefrontComponentMode`.
- [x] Add `StorefrontComponentCategory`.
- [x] Add `StorefrontComponentDescriptor`.
- [x] Add a minimal `StorefrontComponentDescriptorValidator` or equivalent pure helper.
- [x] Validate `Key` is required.
- [x] Validate `Key` matches lowercase kebab-case:

```text
^[a-z0-9]+(?:-[a-z0-9]+)*$
```

- [x] Validate `Mode` is a defined enum value.
- [x] Validate `Category` is a defined enum value.
- [x] Validate `ComponentType` is not null.
- [x] Validate `ComponentType` implements `Microsoft.AspNetCore.Components.IComponent`.
- [x] Verify base `Components` can reference the required Blazor component abstraction without switching to `Microsoft.NET.Sdk.Razor`.
- [x] If a package/framework reference is required for `IComponent`, keep it minimal and document why.
- [x] Do not add descriptor assembly scanning.
- [x] Do not add DI registration.
- [x] Do not add runtime component registry.
- [x] Do not add JSON manifests.

Exit criteria:

- [x] Base `Components` still builds.
- [x] Base `Components` still uses `Microsoft.NET.Sdk`.
- [x] Base `Components` still has no `.razor`, CSS, JS, theme assets, or `Features` folder.

## Phase 3 - Create Mode RCL Projects

- [x] Create `BlazorShop.Storefront.Components.Ssr`.
- [x] Create `BlazorShop.Storefront.Components.Hybrid`.
- [x] Create `BlazorShop.Storefront.Components.WasmHost`.
- [x] Use `Microsoft.NET.Sdk.Razor` for all three mode libraries.
- [x] Target `net10.0`.
- [x] Enable nullable reference types.
- [x] Enable implicit usings.
- [x] Set `PackageId`, `Version`, `Authors`, `Description`, and `RepositoryUrl` consistent with existing Storefront packages.
- [x] Add ownership `README.md` to each mode project.
- [x] Add `_Imports.razor` only if needed for future Razor components, and keep imports mode-safe.
- [x] Do not create production dummy components.
- [x] Do not create `SsrTestComponent`, `HybridTestComponent`, or `WasmHostTestComponent` in production projects.
- [x] Use test fixtures for validator negative/positive examples instead of production dummy components.
- [x] Add all three projects to `BlazorShop.sln`.
- [x] Do not add the new projects to V2, Starter, generated storefronts, or Presentation references in this phase.

Project references:

- [x] `Components.Ssr` references only:
  - `../BlazorShop.Storefront.Components/BlazorShop.Storefront.Components.csproj`
  - `../BlazorShop.Storefront.Presentation/BlazorShop.Storefront.Presentation.csproj`
- [x] `Components.Hybrid` references only:
  - `../BlazorShop.Storefront.Components/BlazorShop.Storefront.Components.csproj`
  - `../BlazorShop.Storefront.Presentation/BlazorShop.Storefront.Presentation.csproj`
  - `../BlazorShop.Storefront.Components.WasmHost/BlazorShop.Storefront.Components.WasmHost.csproj`
- [x] `Components.WasmHost` references only:
  - `../BlazorShop.Storefront.Components/BlazorShop.Storefront.Components.csproj`
  - `../BlazorShop.Storefront.Browser/BlazorShop.Storefront.Browser.csproj`

Exit criteria:

- [x] All three projects build empty/minimal.
- [x] No new project references point from base `Components`, `Presentation`, `Browser`, V2, Starter, or generated storefronts back to these mode projects.

## Phase 4 - Mode Boundary Validator

- [x] Add `StorefrontComponentModeBoundaryValidator` under `BlazorShop.Tests.V2/PresentationV2/Storefront/`.
- [x] Do not overload `StorefrontVisualConsumerBoundaryValidator`.
- [x] Add `StorefrontComponentModeProfile`.
- [x] Add `StorefrontComponentModeBoundaryViolation`.
- [x] Validate `.csproj` project references through strict allowlists.
- [x] Validate package references through strict allowlists.
- [x] Validate source tokens by mode.
- [x] Enumerate source extensions:
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
- [x] Ignore `bin`, `obj`, `node_modules`, generated build output, and package output.
- [x] Report violation path, forbidden token/reference, owner, and remediation message.
- [x] Keep validator deterministic with sorted output.

SSR forbidden references:

- [x] `BlazorShop.Storefront.Browser`
- [x] `BlazorShop.Storefront.Runtime`
- [x] `BlazorShop.Storefront.Client`
- [x] `BlazorShop.Storefront.V2`
- [x] `BlazorShop.Storefront.V2.WASM`
- [x] `BlazorShop.Storefront.Starter`
- [x] `BlazorShop.Storefront.Starter.WASM`
- [x] `BlazorShop.CommerceNode.API`
- [x] `BlazorShop.ControlPlane`
- [x] `BlazorShop.Application`
- [x] `BlazorShop.Domain`
- [x] `BlazorShop.Infrastructure`
- [x] `BlazorShop.Web.SharedV2`

Hybrid forbidden direct references:

- [x] `BlazorShop.Storefront.Browser`
- [x] `BlazorShop.Storefront.Runtime`
- [x] `BlazorShop.Storefront.Client`
- [x] `BlazorShop.Storefront.V2`
- [x] `BlazorShop.Storefront.V2.WASM`
- [x] `BlazorShop.Storefront.Starter`
- [x] `BlazorShop.Storefront.Starter.WASM`
- [x] backend/core/API projects
- [x] `BlazorShop.Web.SharedV2`

WasmHost forbidden references:

- [x] `BlazorShop.Storefront.Presentation`
- [x] `BlazorShop.Storefront.Runtime`
- [x] `BlazorShop.Storefront.Client`
- [x] `BlazorShop.Storefront.V2`
- [x] `BlazorShop.Storefront.V2.WASM`
- [x] `BlazorShop.Storefront.Starter`
- [x] `BlazorShop.Storefront.Starter.WASM`
- [x] backend/core/API projects
- [x] `BlazorShop.Web.SharedV2`

SSR forbidden source tokens:

- [x] `HttpClient`
- [x] `IHttpClientFactory`
- [x] `IJSRuntime`
- [x] `JSImport`
- [x] `@rendermode`
- [x] `InteractiveWebAssembly`
- [x] `InteractiveServer`
- [x] `"/api/`
- [x] `'/api/`
- [x] `api/storefront`
- [x] `localhost:`
- [x] `CommerceNodeBaseUrl`
- [x] `StorefrontLocalApiClient`

Hybrid forbidden source tokens:

- [x] `HttpClient`
- [x] `IHttpClientFactory`
- [x] `IJSRuntime`
- [x] `JSImport`
- [x] `"/api/`
- [x] `'/api/`
- [x] `api/storefront`
- [x] `localhost:`
- [x] `CommerceNodeBaseUrl`
- [x] `StorefrontLocalApiClient`
- [x] `IStorefrontBrowser`

Hybrid allowed source tokens:

- [x] `@rendermode`
- [x] `InteractiveWebAssembly`
- [x] `BlazorShop.Storefront.Components.WasmHost`

WasmHost forbidden source tokens:

- [x] `HttpClient`
- [x] `IHttpClientFactory`
- [x] `HttpContext`
- [x] `IHttpContextAccessor`
- [x] `"/api/`
- [x] `'/api/`
- [x] `api/storefront`
- [x] `localhost:`
- [x] `CommerceNodeBaseUrl`
- [x] `BlazorShop.Storefront.Presentation`
- [x] `IStorefrontRuntime`
- [x] `IStorefrontCatalogClient`
- [x] `IStorefrontCartClient`
- [x] `IStorefrontCheckoutClient`
- [x] `IStorefrontCustomerClient`

WasmHost allowed source tokens:

- [x] `IJSRuntime`
- [x] `EventCallback`
- [x] browser controller interfaces from `BlazorShop.Storefront.Browser`

Exit criteria:

- [x] Validator can pass clean source.
- [x] Validator gives actionable violation messages.
- [x] Validator has profile-specific allowlists, not a global forbidden-only scan.

## Phase 5 - Architecture Tests

Add focused tests under `BlazorShop.Tests.V2/PresentationV2/Storefront/`.

### StorefrontComponentModeFoundationTests

- [x] Assert all three project directories exist.
- [x] Assert all three `.csproj` files exist.
- [x] Assert all three projects use `Microsoft.NET.Sdk.Razor`.
- [x] Assert all three projects target `net10.0`.
- [x] Assert package metadata exists and matches Storefront package conventions.
- [x] Assert all three projects are included in `BlazorShop.sln`.
- [x] Assert base `Components` still uses `Microsoft.NET.Sdk`.
- [x] Assert base `Components` still has no `.razor` files.
- [x] Assert base `Components/Features` still does not exist.
- [x] Assert `Presentation`, V2, V2.WASM, Starter, and Starter.WASM do not reference the new mode projects in Phase 1.

### StorefrontComponentModeDependencyTests

- [x] Assert `Components.Ssr` references exactly Components and Presentation.
- [x] Assert `Components.Hybrid` references exactly Components, Presentation, and Components.WasmHost.
- [x] Assert `Components.WasmHost` references exactly Components and Browser.
- [x] Assert `Components.WasmHost` does not reference Presentation.
- [x] Assert no mode project references Runtime or Client.
- [x] Assert no mode project references V2 or Starter consumer projects.
- [x] Assert no mode project references backend/core/API projects.
- [x] Assert no project-reference cycle exists among Storefront packages.

### StorefrontComponentModeBoundaryValidatorTests

- [x] Positive fixture: SSR profile with allowed refs/source passes.
- [x] Positive fixture: Hybrid profile with allowed WasmHost reference and `@rendermode` bridge passes.
- [x] Positive fixture: WasmHost profile with Browser controller dependency and `IJSRuntime` passes.
- [x] Negative fixture: SSR with `IJSRuntime` fails.
- [x] Negative fixture: SSR referencing Browser fails.
- [x] Negative fixture: SSR using `@rendermode` fails.
- [x] Negative fixture: Hybrid referencing Browser directly fails.
- [x] Negative fixture: Hybrid injecting `HttpClient` fails.
- [x] Negative fixture: Hybrid injecting browser controller directly fails.
- [x] Negative fixture: WasmHost referencing Presentation fails.
- [x] Negative fixture: WasmHost injecting `HttpClient` fails.
- [x] Negative fixture: WasmHost calling `/api/storefront` fails.
- [x] Negative fixture: any mode referencing V2 fails.
- [x] Negative fixture: any mode referencing backend/core/API projects fails.

### StorefrontComponentDescriptorTests

- [x] Valid descriptor passes.
- [x] Empty key fails.
- [x] Whitespace key fails.
- [x] Uppercase key fails.
- [x] Snake case key fails.
- [x] Key with slash fails.
- [x] Key with dot fails.
- [x] Key with double dash fails.
- [x] Invalid mode enum value fails.
- [x] Invalid category enum value fails.
- [x] Null component type fails.
- [x] Type not implementing `IComponent` fails.
- [x] Razor component fixture implementing `IComponent` passes.

### StorefrontComponentVisualNeutralityTests

- [x] Assert no `.css`, `.scss`, `.sass`, `.less` files exist in the three mode projects.
- [x] Assert no `tailwind.config.*` exists in the three mode projects.
- [x] Assert no `postcss.config.*` exists in the three mode projects.
- [x] Assert no `wwwroot/css` or theme asset folder exists in the three mode projects.
- [x] Assert no literal `class="rounded`, `class="bg-`, `class="text-`, `class="shadow`, `class="grid`, `class="flex`, `class="px-`, `class="mx-`, or responsive Tailwind prefixes exist.
- [x] Allow `class="@..."`
- [x] Allow `data-storefront-*`
- [x] Assert no final storefront copy strings are introduced.
- [x] Assert no V2 CSS classes or V2 asset paths are referenced.

Exit criteria:

- [x] New tests fail before implementation where appropriate.
- [x] New tests pass after implementation.
- [x] Existing Components headless tests still pass.

## Phase 6 - QA Checklist And Closure Gates

- [x] Update `docs/refactor-control-Commerce-storefront/QA-StorefrontV2.todo.md` with a new section for Component Mode Foundation.
- [x] Add checklist item for base Components role preserved.
- [x] Add checklist item for three mode projects created.
- [x] Add checklist item for exact dependency allowlists.
- [x] Add checklist item for descriptor validation.
- [x] Add checklist item for visual neutrality.
- [x] Add checklist item for no real feature components in Phase 1.
- [x] Add checklist item explaining why Playwright is not required for this phase.

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

- [x] Do not run Playwright for Phase 1 unless a real browser-visible component is accidentally added.
- [x] If a browser-visible component is added, stop and split it into the next phase.

Exit criteria:

- [x] All build gates pass.
- [x] Focused architecture tests pass.
- [x] QA checklist is updated with evidence.
- [x] No browser QA is required because no real browser behavior changed.

## Phase 7 - Final Audit Before Commit

- [x] Run `git status --short`.
- [x] Verify no unrelated user changes were modified.
- [x] Verify no production dummy components exist.
- [x] Verify no `Components/Features` folder exists.
- [x] Verify no V2/Starter/generated project references the new mode projects.
- [x] Verify no Runtime/Client/backend reference exists in any mode project.
- [x] Verify base Components still has no `.razor` files.
- [x] Verify docs mention future components only as next phase.
- [x] Verify this todo has implementation evidence filled in.
- [x] Commit with a focused message after tests pass.

Suggested commit message:

```text
chore: add storefront component mode foundation
```

## Definition Of Done

- [x] `BlazorShop.Storefront.Components` remains the lowest browser-safe contracts/headless layer.
- [x] `BlazorShop.Storefront.Components` still has no Razor components.
- [x] `BlazorShop.Storefront.Components/Features` remains absent.
- [x] `StorefrontComponentMode` exists.
- [x] `StorefrontComponentCategory` exists.
- [x] `StorefrontComponentDescriptor` exists.
- [x] Descriptor validation is tested.
- [x] `Components.Ssr` exists and has strict dependency rules.
- [x] `Components.Hybrid` exists and has strict dependency rules.
- [x] `Components.WasmHost` exists and has strict dependency rules.
- [x] SSR cannot use Browser, Runtime, Client, JS interop, or render mode.
- [x] Hybrid can bridge to WasmHost but cannot own browser transport.
- [x] WasmHost can use Browser controllers but cannot call backend APIs directly.
- [x] No new mode project references V2, Starter, Commerce Node, Control Plane, Application, Domain, Infrastructure, Runtime, Client, or Web.SharedV2.
- [x] No theme CSS/assets are introduced in reusable mode libraries.
- [x] Literal visual classes are rejected in the new mode libraries.
- [x] Documentation explains mode ownership and examples.
- [x] QA checklist is updated.
- [x] Focused build/test gates pass.
- [x] No real feature implementation is included.

## Closure Patch - 2026-08-09

- [x] Visual neutrality guard was strengthened from selected Tailwind/V2 prefix checks to a generic Razor `class` attribute scanner for reusable SSR, Hybrid, and WasmHost mode project markup.
- [x] Reusable mode projects may expose semantic `data-storefront-*` hooks and fully dynamic class slots such as `class="@CssClass"`, `class="@Classes.Container"`, `class="@GetCssClass()"`, and `class="@(BuildCssClass())"`.
- [x] Reusable mode projects must reject literal class ownership, including non-Tailwind names such as `storefront-logo`, and mixed dynamic/literal values such as `class="@CssClass selected"`.
- [x] Descriptor mode/project consistency is enforced by repository architecture tests through a test-side assembly resolver; `StorefrontComponentDescriptorValidator` remains generic and unchanged.
- [x] No production Storefront project behavior, DI, runtime code, or real component implementation was changed by the closure patch.

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
- [x] Descriptor contracts evidence:
  - 2026-08-09 Phase 2 added only base descriptor contract files under `BlazorShop.Storefront.Components/Contracts/Components/`; no mode projects yet.
  - 2026-08-09 added `StorefrontComponentMode`, `StorefrontComponentCategory`, `StorefrontComponentDescriptor`, `StorefrontComponentDescriptorValidationResult`, and `StorefrontComponentDescriptorValidator`.
  - 2026-08-09 `StorefrontComponentDescriptorValidator` validates required key, lowercase kebab-case key pattern, defined mode/category enum values, non-null component type, and `IComponent` implementation.
  - 2026-08-09 base Components remains `Microsoft.NET.Sdk`; added minimal `PackageReference Include="Microsoft.AspNetCore.Components" Version="10.0.9"` so the logic-only project can reference `Microsoft.AspNetCore.Components.IComponent` without becoming an RCL or adding an ASP.NET shared-framework reference to WASM consumers.
  - 2026-08-09 no descriptor assembly scanning, DI registration, runtime registry, or JSON manifest code was added.
  - 2026-08-09 `dotnet build BlazorShop.PresentationV2\BlazorShop.Storefront.Components\BlazorShop.Storefront.Components.csproj --no-restore` passed with 0 warnings/errors.
  - 2026-08-09 scan confirmed base Components still has no `.razor`, CSS, JS, theme assets, or `Features` folder.
- [x] Projects added:
  - 2026-08-09: created `BlazorShop.Storefront.Components.Ssr`, `BlazorShop.Storefront.Components.Hybrid`, and `BlazorShop.Storefront.Components.WasmHost` as empty/minimal `Microsoft.NET.Sdk.Razor` projects targeting `net10.0` with nullable and implicit usings enabled.
  - 2026-08-09: each mode project has Storefront package metadata (`PackageId`, `Version`, `Authors`, `Description`, `RepositoryUrl`) and an ownership `README.md`.
  - 2026-08-09: no `_Imports.razor` was added because there are no Razor components yet; no production dummy/test components or future feature components were added.
  - 2026-08-09: `dotnet sln BlazorShop.sln list` includes all three mode projects. Existing unrelated solution-folder change in `BlazorShop.sln` is preserved in the working tree but excluded from phase commits.
  - 2026-08-09: updated `AGENTS.md` active V2 presentation/runtime list with the three mode projects after they were created.
  - 2026-08-09: `Components.Ssr` references exactly base Components and Presentation; `Components.Hybrid` references exactly base Components, Presentation, and Components.WasmHost; `Components.WasmHost` references exactly base Components and Browser.
  - 2026-08-09: an initial parallel build hit shared `obj/bin` file locks; sequential builds for Ssr, WasmHost, and Hybrid then passed with 0 warnings/errors.
  - 2026-08-09: source scan confirmed no `.razor` files or production dummy/future feature component names exist in the mode projects.
- [x] Tests/validator evidence:
  - 2026-08-09: added `StorefrontComponentModeBoundaryValidator`, `StorefrontComponentModeProfiles`, `StorefrontComponentModeProfile`, and `StorefrontComponentModeBoundaryViolation` under `BlazorShop.Tests.V2/PresentationV2/Storefront/`.
  - 2026-08-09: validator parses `.csproj` project/package references with strict profile allowlists, scans mode source extensions including `.cs`, `.razor`, `.cshtml`, `.js`, `.mjs`, `.ts`, `.json`, `.yaml`, `.yml`, `.css`, `.scss`, `.sass`, and `.less`, ignores `bin`, `obj`, `node_modules`, generated/package output folders, and returns sorted violations with path, forbidden token/reference, owner, remediation, and kind.
  - 2026-08-09: profile factory defines separate SSR, Hybrid, and WasmHost required/allowed project refs, forbidden tokens, and mode-specific allowed source tokens. `StorefrontVisualConsumerBoundaryValidator` was not modified.
  - 2026-08-09: `dotnet build BlazorShop.Tests.V2\BlazorShop.Tests.V2.csproj --no-restore` initially exposed that base Components `FrameworkReference Include="Microsoft.AspNetCore.App"` breaks WASM consumers with `NETSDK1082`; changed it to `PackageReference Include="Microsoft.AspNetCore.Components" Version="10.0.9"` to keep `IComponent` validation browser-wasm compatible.
  - 2026-08-09: after the package reference fix, `dotnet restore BlazorShop.PresentationV2\BlazorShop.Storefront.Components\BlazorShop.Storefront.Components.csproj` reported up-to-date and `dotnet build BlazorShop.Tests.V2\BlazorShop.Tests.V2.csproj --no-restore` passed. Existing warnings: MessagePack NU1902/NU1903 and Browserslist.
- [x] Tests added:
  - 2026-08-09: added `StorefrontComponentModeFoundationTests`, `StorefrontComponentModeDependencyTests`, `StorefrontComponentModeBoundaryValidatorTests`, `StorefrontComponentDescriptorTests`, and `StorefrontComponentVisualNeutralityTests`.
  - 2026-08-09: updated `StorefrontComponentsHeadlessPresentationRefactorTests` contract inventory to include descriptor contracts and moved its neutrality documentation assertion to this active component-mode plan.
- [x] Build evidence:
  - 2026-08-09: sequential build gate passed for `BlazorShop.Storefront.Components`, `BlazorShop.Storefront.Presentation`, `BlazorShop.Storefront.Browser`, `BlazorShop.Storefront.Components.Ssr`, `BlazorShop.Storefront.Components.WasmHost`, and `BlazorShop.Storefront.Components.Hybrid`; each build reported 0 warnings and 0 errors.
- [x] Focused test evidence:
  - 2026-08-09: `dotnet test BlazorShop.Tests.V2\BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~StorefrontComponentModeFoundationTests|FullyQualifiedName~StorefrontComponentModeDependencyTests|FullyQualifiedName~StorefrontComponentModeBoundaryValidatorTests|FullyQualifiedName~StorefrontComponentDescriptorTests|FullyQualifiedName~StorefrontComponentVisualNeutralityTests"` passed: 48 passed, 0 failed.
  - 2026-08-09: `dotnet test BlazorShop.Tests.V2\BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~StorefrontComponentsHeadlessPresentationRefactorTests"` passed: 26 passed, 0 failed.
- [x] QA checklist evidence:
  - 2026-08-09: added `Storefront Component Mode Foundation` section to `docs/refactor-control-Commerce-storefront/QA-StorefrontV2.todo.md` covering base Components preservation, mode projects, dependency allowlists, descriptor validation, visual neutrality, no real feature components, and no Playwright requirement.
  - 2026-08-09: focused closure gate passed: `dotnet test BlazorShop.Tests.V2\BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~StorefrontComponentModeFoundationTests|FullyQualifiedName~StorefrontComponentModeDependencyTests|FullyQualifiedName~StorefrontComponentModeBoundaryValidatorTests|FullyQualifiedName~StorefrontComponentDescriptorTests|FullyQualifiedName~StorefrontComponentVisualNeutralityTests|FullyQualifiedName~StorefrontComponentsHeadlessPresentationRefactorTests|FullyQualifiedName~StorefrontSharedPlatformPackageContractTests|FullyQualifiedName~StorefrontPageCompositionGuardrailTests"` passed: 129 passed, 0 failed.
  - 2026-08-09: Playwright was intentionally not run because this foundation adds no browser-visible routes, component markup, CSS, JS behavior, or runtime rendering.
- [x] Commit:
  - 2026-08-09: phase commits created: `039ea147`, `f6dd5d73`, `9907ce4c`, `b55687c8`, `b72d0330`, `c53f3e07`, and `712cb878`; final audit closure is committed separately in Phase 7.

## Decision Audit Trail

| # | Decision | Classification | Rationale | Rejected |
|---|---|---|---|---|
| 1 | Create three explicit mode projects instead of reusing `Components/Features`. | Architecture | Existing codebase retired `Components/Features`; mode projects make SSR/Hybrid/WasmHost ownership visible without turning base Components into a visual library. | Reopen `Features` folder. |
| 2 | Keep base `Components` as `Microsoft.NET.Sdk` and place only descriptor contracts there. | Boundary | Current tests and docs define Components as browser-safe contracts/headless primitives, not Razor component ownership. | Convert base Components to Razor SDK. |
| 3 | Do not reference mode projects from Presentation, V2, Starter, or generated storefronts in Phase 1. | Risk control | Phase 1 is foundation-only; adoption belongs to real component implementation. | Add unused host references now. |
| 4 | Add a new mode boundary validator instead of overloading the visual consumer validator. | Test design | Existing visual consumer validator has host-specific assumptions; reusable mode libraries need different allowlists. | Extend `StorefrontVisualConsumerBoundaryValidator` directly. |
| 5 | No Playwright in Phase 1. | QA scope | No browser-visible behavior changes if no real components are implemented. | Run browser QA without a visible feature surface. |

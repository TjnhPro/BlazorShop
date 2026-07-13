# BlazorShop Storefront WASM Foundation Todo

## Goal

Add a minimal Blazor WebAssembly foundation to `BlazorShop.Storefront.V2` without changing Storefront pages, routes, cart behavior, catalog behavior, auth behavior, Commerce Node APIs, or deployment boundaries.

Success for this phase means:

- `BlazorShop.Storefront.V2` remains the only Storefront runtime/host.
- `BlazorShop.Storefront.WASM` exists as a browser-side bundle project, not as a separate app to run.
- `BlazorShop.Storefront.Components` exists as the shared Razor component library for future SSR/WASM component modules.
- One small development-only component renders through `InteractiveWebAssembly` and proves that browser-side .NET event handling works.
- Existing SSR pages continue to render normally.

## Locked Decisions

| Area | Decision |
|---|---|
| Storefront host | Keep `BlazorShop.Storefront.V2` as the only executable Storefront web app. |
| WASM project name | Use `BlazorShop.Storefront.WASM` so the project name is explicit and not confused with a separate client service. |
| Component project name | Use `BlazorShop.Storefront.Components` for shared SSR/WASM components. |
| Runtime model | Blazor Web App, SSR-first, with component-level `InteractiveWebAssembly` islands. |
| New pages | None in this phase. |
| Business feature migration | None in this phase. Do not move cart, product, search, auth, or checkout to WASM yet. |
| API changes | None. Storefront V2 still calls Commerce Node `api/internal/*` from server-side services. |
| JavaScript | Keep `wwwroot/js/storefrontCommerce.js` unchanged in this phase. |
| Secrets | No node credentials, store secrets, or privileged API behavior in WASM. |
| Probe visibility | Development-only diagnostic component in the existing layout, not a new public route. |

## Target Architecture

```text
Browser
  -> BlazorShop.Storefront.V2
      -> SSR renders Storefront route HTML
      -> serves _framework/blazor.web.js
      -> serves BlazorShop.Storefront.WASM static assets
      -> hydrates selected components only

BlazorShop.Storefront.V2
  -> BlazorShop.Storefront.Components
  -> BlazorShop.Storefront.WASM
  -> BlazorShop.Web.SharedV2

BlazorShop.Storefront.WASM
  -> BlazorShop.Storefront.Components
  -> BlazorShop.Web.SharedV2 only for browser-safe helpers when needed

BlazorShop.Storefront.Components
  -> no dependency on Storefront.V2
  -> no dependency on CommerceNode.API
  -> no dependency on ControlPlane
```

## Current Code Facts

- `BlazorShop.Storefront.V2` targets `net10.0` and uses `Microsoft.NET.Sdk.Web`.
- `Program.cs` currently calls `builder.Services.AddRazorComponents();` only.
- `Program.cs` currently maps `app.MapRazorComponents<App>();` only.
- `App.razor` does not load `_framework/blazor.web.js` yet.
- `Routes.razor` uses Storefront V2 assembly routes only.
- Storefront layout lives at `Components/Layout/MainLayout.razor`.
- Storefront V2 currently uses `storefrontCommerce.js` for cart and toast browser behavior.
- `BlazorShop.Web.SharedV2` already contains browser-safe helper classes and references Blazor WebAssembly packages.

## Autoplan Review Summary

### CEO Review

The valuable outcome is not "make Storefront a WASM app". The valuable outcome is a low-risk foundation that lets future modules opt into browser-side behavior when they need it.

Auto-decisions:

- Keep SSR as the default for SEO and first render.
- Do not add customer-facing pages in this phase.
- Prove the infrastructure with one tiny component instead of migrating a real feature immediately.

### Engineering Review

The safest architecture is a three-project split:

- `Storefront.V2` owns hosting, routes, SSR, SEO, cookies, server-side API calls, and deployment.
- `Storefront.Components` owns reusable Razor UI that can render under SSR or WASM.
- `Storefront.WASM` owns browser-side service registration and the WebAssembly entry point.

Main risk:

- Accidentally applying `InteractiveWebAssembly` too high in the component tree can change SSR behavior and increase bundle cost. Keep the first test as an isolated component in Development only.

### Design Review

No production UI design change is required. The probe should be visually small and development-only so it does not become storefront UX.

### DX Review

The developer path should stay simple:

1. Start Storefront V2 as usual.
2. Open an existing Storefront page.
3. Confirm the development-only probe hydrates.
4. Click the probe button and confirm the counter increments without a full page reload.

## Phase 1 - Define Project Boundaries

- [x] Add this plan file.
- [x] Confirm no code references to legacy Storefront are needed.
- [x] Document that `BlazorShop.Storefront.WASM` is not a separate deployed service.
- [x] Document that `BlazorShop.Storefront.Components` is the future home for shared module components.

Expected files:

- `docs/refactor-control-Commerce-storefront/BlazorShop.StorefrontWasmFoundation.todo.md`

Verification:

- [x] Plan reviewed and accepted before implementation.

## Phase 2 - Add Storefront Components RCL

- [ ] Create `BlazorShop.PresentationV2/BlazorShop.Storefront.Components`.
- [ ] Use a Razor Class Library style project targeting `net10.0`.
- [ ] Set namespace/root namespace to `BlazorShop.Storefront.Components`.
- [ ] Add `_Imports.razor` for component-level usings.
- [ ] Add a small `WasmDiagnostics/WasmProbe.razor` component.
- [ ] Keep the component free of business logic and API calls.

Probe behavior:

- Server prerender displays an SSR-safe initial state.
- Browser hydration updates state to show it is running in WebAssembly.
- A button increments a counter through Blazor event handling.
- The component should be easy to remove or keep as a dev diagnostic.

Expected files:

- `BlazorShop.PresentationV2/BlazorShop.Storefront.Components/BlazorShop.Storefront.Components.csproj`
- `BlazorShop.PresentationV2/BlazorShop.Storefront.Components/_Imports.razor`
- `BlazorShop.PresentationV2/BlazorShop.Storefront.Components/WasmDiagnostics/WasmProbe.razor`

Verification:

- [ ] `dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.Components/BlazorShop.Storefront.Components.csproj`

## Phase 3 - Add Storefront WASM Project

- [ ] Create `BlazorShop.PresentationV2/BlazorShop.Storefront.WASM`.
- [ ] Use a Blazor WebAssembly project shape targeting `net10.0`.
- [ ] Keep it as a bundle/entry-point project hosted by `Storefront.V2`.
- [ ] Reference `BlazorShop.Storefront.Components`.
- [ ] Reference `BlazorShop.Web.SharedV2` only if a browser-safe helper is needed.
- [ ] Do not reference `BlazorShop.Infrastructure`, `BlazorShop.CommerceNode.API`, `BlazorShop.ControlPlane.API`, or legacy Presentation projects.
- [ ] Keep `Program.cs` minimal.

Expected files:

- `BlazorShop.PresentationV2/BlazorShop.Storefront.WASM/BlazorShop.Storefront.WASM.csproj`
- `BlazorShop.PresentationV2/BlazorShop.Storefront.WASM/Program.cs`
- `BlazorShop.PresentationV2/BlazorShop.Storefront.WASM/_Imports.razor`

Verification:

- [ ] `dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.WASM/BlazorShop.Storefront.WASM.csproj`

## Phase 4 - Wire Storefront V2 Host

- [ ] Add project references from `BlazorShop.Storefront.V2` to:
  - `BlazorShop.Storefront.Components`
  - `BlazorShop.Storefront.WASM`
- [ ] Update `Program.cs` Razor component registration:

```csharp
builder.Services
    .AddRazorComponents()
    .AddInteractiveWebAssemblyComponents();
```

- [ ] Update component endpoint mapping:

```csharp
app.MapRazorComponents<App>()
    .AddInteractiveWebAssemblyRenderMode();
```

- [ ] Add additional assemblies only if routing/component discovery requires it after build validation.
- [ ] Add `_framework/blazor.web.js` to `App.razor`.
- [ ] Add the development-only `WasmProbe` to `Components/Layout/MainLayout.razor`.
- [ ] Keep every existing route and page unchanged.

Expected files:

- `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/BlazorShop.Storefront.V2.csproj`
- `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Program.cs`
- `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/App.razor`
- `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Components/Layout/MainLayout.razor`
- `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/_Imports.razor`

Verification:

- [ ] `dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.V2/BlazorShop.Storefront.V2.csproj`
- [ ] Existing Storefront pages still build without route changes.

## Phase 5 - QA Checklist Update

- [ ] Add a "WASM Foundation" section to `QA-StorefrontV2.todo.md`.
- [ ] Require visible browser QA when the user asks to observe.
- [ ] Verify SSR page HTML still loads.
- [ ] Verify `_framework/blazor.web.js` loads.
- [ ] Verify WASM boot resources load.
- [ ] Verify the probe counter increments without a full page reload.
- [ ] Verify `storefrontCommerce.js` cart/toast behavior still initializes.

Expected files:

- `docs/refactor-control-Commerce-storefront/QA-StorefrontV2.todo.md`

Verification:

- [ ] `dotnet build BlazorShop.sln`
- [ ] Playwright visible browser smoke on an existing route such as `/signin` or `/`.

## Phase 6 - Runtime Smoke

- [ ] Start Storefront V2 on its existing local port.
- [ ] Open an existing Storefront route.
- [ ] Confirm no new page is required to test WASM.
- [ ] Confirm the Development-only probe is visible only in Development.
- [ ] Click the probe button.
- [ ] Confirm the counter updates without navigation.
- [ ] Confirm browser console has no Blazor boot errors.

Optional network checks:

- [ ] `_framework/blazor.web.js` returns 200.
- [ ] `_framework/blazor.boot.json` returns 200.
- [ ] Storefront WASM assemblies load from the Storefront V2 host.

## Non-Goals

- [ ] Do not convert Storefront V2 to standalone WASM.
- [ ] Do not introduce a second Storefront process or port.
- [ ] Do not move cart behavior to WASM in this phase.
- [ ] Do not move product detail behavior to WASM in this phase.
- [ ] Do not move search behavior to WASM in this phase.
- [ ] Do not change Commerce Node APIs.
- [ ] Do not change Control Plane APIs or UI.
- [ ] Do not add runtime references to legacy Presentation projects.
- [ ] Do not put secrets or privileged node credentials in WASM.

## Risks And Mitigations

| Risk | Impact | Mitigation |
|---|---|---|
| WASM bundle becomes too large early | Slower public Storefront load | Keep only the probe and minimal services in WASM. Do not reference heavy server projects. |
| Render mode applied too broadly | SSR behavior and SEO can regress | Apply `InteractiveWebAssembly` only to the probe component in this phase. |
| Browser-side services accidentally depend on server-only APIs | Runtime failure after hydration | Keep `Storefront.WASM` dependencies browser-safe. |
| Existing JS and Blazor both bind to the same elements | Duplicate click behavior | Probe must use unique markup and avoid current cart/toast selectors. |
| New project naming confuses deployment | Operator may try to run WASM separately | Document `Storefront.WASM` as hosted static assets only. |

## Definition Of Done

- [ ] `BlazorShop.Storefront.Components` exists and builds.
- [ ] `BlazorShop.Storefront.WASM` exists and builds.
- [ ] `BlazorShop.Storefront.V2` hosts Interactive WebAssembly components.
- [ ] One development-only WASM probe hydrates and handles a click event.
- [ ] Existing Storefront V2 routes remain SSR-first.
- [ ] No Commerce Node or Control Plane API changes were required.
- [ ] `QA-StorefrontV2.todo.md` contains WASM Foundation QA checks.

## Decision Audit Trail

| # | Phase | Decision | Classification | Principle | Rationale | Rejected |
|---|---|---|---|---|---|---|
| 1 | Intake | Keep Storefront V2 as the only runtime host. | Mechanical | Explicit over clever | The user clarified WASM should be registered by Storefront V2, not run as a separate service. | Separate `Storefront.Client` runtime. |
| 2 | Architecture | Create `BlazorShop.Storefront.WASM` for browser entry point. | Mechanical | Naming consistency | The project name makes the browser-side role obvious and avoids the ambiguous `Client` name. | `BlazorShop.Storefront.Client`. |
| 3 | Architecture | Create `BlazorShop.Storefront.Components` for shared Razor components. | Mechanical | DRY | Future SSR/WASM modules need a neutral component home that does not depend on the host. | Put all future components directly in `Storefront.V2`. |
| 4 | Scope | Add only a development-only WASM probe in this phase. | Mechanical | Pragmatic | This proves the infrastructure without touching business behavior. | Migrate cart/product/search immediately. |
| 5 | QA | Add QA checks to `QA-StorefrontV2.todo.md`. | Mechanical | Completeness | A visible hydration proof is required before future feature migration. | Rely on build only. |

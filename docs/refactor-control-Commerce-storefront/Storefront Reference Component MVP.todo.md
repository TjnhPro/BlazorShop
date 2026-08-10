# Storefront Reference Component MVP

Status: planned
Owner: Storefront V2 architecture
Branch: `Hybrid-Architecture`
Predecessor: `Storefront Component Mode Foundation v2.todo.md`
Successor: H3 Component Architecture Hardening / Closure
Scope: H2 runtime proof for SSR, Hybrid, and WasmHost component modes

## Goal

Implement one deterministic runtime proof surface for the active Storefront architecture.

H2 must prove, in a running Storefront V2 application, that:

```text
SSR component
  renders useful server HTML without WASM.

Hybrid component
  renders useful prerendered HTML first,
  then becomes WebAssembly-interactive,
  then handles a C# browser-side event.

WasmHost component
  runs in the downloaded WASM graph,
  uses Browser controllers,
  calls same-origin BFF only,
  and does not call Commerce Node directly.
```

This phase is a runtime proof phase. It is not a broad component extraction phase.

## Current Codebase Facts

Verified before this plan:

- Branch is `Hybrid-Architecture`.
- H1 foundation checklist is implemented, but the H1 plan header still says `Status: planned`.
- `BlazorShop.PresentationV2/COMPONENT-MODES.md` is the current component mode source of truth.
- `StorefrontBrandLogo` exists in `BlazorShop.Storefront.Components.Ssr/Brand`.
- `StorefrontDiscountedProductRail` exists in `BlazorShop.Storefront.Components.WasmHost/Catalog`.
- `StorefrontDiscountedProductRailSection` exists in `BlazorShop.Storefront.V2.WASM/Components/Catalog` and supplies V2 labels, classes, templates, and the same-origin action `/api/catalog/discounted-products`.
- `StorefrontContactForm` still exists in `BlazorShop.Storefront.Components.Hybrid` as a transitional historical bridge.
- Visible V2 contact and discounted rail routes use V2.WASM wrappers with `@rendermode="InteractiveWebAssembly"`.
- `BlazorShop.Storefront.V2/Program.cs` maps Presentation and additional V2.WASM/WasmHost assemblies through `MapStorefrontApplication(...)`.
- Presentation owns route pages. V2 owns visual view registrations.
- `docs/refactor-control-Commerce-storefront/QA-StorefrontV2.todo.md` records the earlier route ownership lock: V2, Starter, and V2.WASM should not introduce visual `@page` directives.

## Corrected H2 Architecture Decision

The original H2 idea was right, but route ownership must match the current codebase.

Correct ownership:

```text
BlazorShop.Storefront.Presentation
  owns Component MVP route shell and page context.

BlazorShop.Storefront.V2
  owns visual lab markup, layout, copy, and classes.

BlazorShop.Storefront.V2.WASM
  owns browser-downloadable wrapper roots when a host wrapper is needed.

BlazorShop.Storefront.Components.Ssr
  owns SSR reusable component implementation.

BlazorShop.Storefront.Components.WasmHost
  owns browser-interactive reusable component implementation.

BlazorShop.Storefront.Components.Hybrid
  remains transitional until H2 evidence decides keep/narrow/retire.
```

Forbidden H2 route ownership:

```text
BlazorShop.Storefront.V2/*.razor
  must not add @page for /component-mvp or /__qa/component-mvp.
```

Recommended route:

```text
/__qa/component-mvp
```

This route is an architecture QA surface, not a public design-system page. It must not be added to menus or sitemap. It should be `noindex`, or be enabled only under Development/QA configuration if the implementation chooses that route.

## Selected MVP Proofs

| Mode | Proof | Why |
| --- | --- | --- |
| SSR | Existing `StorefrontBrandLogo` | Already clean: context + classes -> useful HTML, no Browser, no WASM, no render mode. |
| Hybrid | New `StorefrontHybridRuntimeProbe` | Small deterministic proof of prerender -> WASM hydration -> C# interaction without BFF/business complexity. |
| WasmHost | Existing `StorefrontDiscountedProductRail` | Already proves Browser controller -> same-origin BFF -> Runtime path and has loading/success/empty/error/retry states. |

Do not use ContactForm as the canonical H2 Hybrid proof. It remains useful historical evidence, but it combines form state, BFF, validation, submission, and business endpoint behavior. H2 needs a focused runtime proof first.

## Not In Scope

- No Product/Catalog/Cart/Checkout/Account component extraction.
- No Starter migration.
- No generated storefront migration.
- No StorefrontBuilder changes.
- No Commerce Node API or database changes.
- No Control Plane changes.
- No pricing, cart, checkout, payment, order, auth, or inventory behavior changes.
- No new component registry, plugin system, scanner, theme framework, or capability module rollout.
- No `InteractiveServer`, `InteractiveAuto`, public SignalR/circuit UI state, or server-side interactive Storefront UI.
- No direct Commerce Node browser calls.
- No `@page` route directives in V2 or V2.WASM visual projects.

## Architecture Diagram

```text
Browser request
  |
  v
Presentation route: /__qa/component-mvp
  |
  v
Presentation page shell/context
  |
  v
V2 registered visual lab view
  |------------------------------|
  |                              |
  v                              v
Components.Ssr                V2.WASM wrapper/root
StorefrontBrandLogo             |
                                | @rendermode InteractiveWebAssembly
                                v
                      Components.WasmHost
                      StorefrontHybridRuntimeProbe
                      StorefrontDiscountedProductRail
                                |
                                v
                         Browser controller
                                |
                                v
                     same-origin Presentation BFF
                                |
                                v
                         Runtime / Commerce Node
```

Hybrid runtime path:

```text
HTTP response HTML
  -> data-storefront-runtime-state="prerender"
  -> WASM boot/hydration
  -> data-storefront-runtime-state="interactive"
  -> C# event increments local state
```

## Failure Modes Registry

| Failure | Impact | Prevention in plan |
| --- | --- | --- |
| V2 gets a new `@page` directive | Breaks Presentation-owned route architecture | H2 route must live in Presentation and V2 registers only visual view/component. |
| Hybrid probe placed in `Components.Hybrid` | Recreates stale physical-mode coupling | Put browser-executed implementation in downloadable WASM graph, preferably `Components.WasmHost`. |
| Browser QA only waits for final page | Misses prerender failures | Add raw initial HTML test before browser JS executes. |
| Network audit rejects all WebSocket traffic | False failures from dev tooling/hot reload | Assert no public Blazor Server UI circuit, not zero websocket globally. |
| Rail success depends on current store data | Flaky tests | Use deterministic fixture data or Playwright route mocking for BFF states. |
| Shared component owns V2 copy/classes | Breaks reusable component boundary | Keep copy/classes in V2 or V2.WASM wrappers. |
| Reusable component self-owns render mode | Makes component hard to reuse across hosts | Host/composition owns `@rendermode`. |
| H2 deletes `Components.Hybrid` too early | Large cleanup risk inside proof phase | H2 records keep/narrow/retire decision; deletion is H3 unless isolated and safe. |

## Phase H2.0 - Baseline And H1 Cleanup

### Goal

Start from a clean, documented baseline and remove stale H1 wording that would confuse the H2 implementer.

### Tasks

- [x] Confirm branch:

```powershell
git branch --show-current
git status --short
```

- [x] Read:
  - [x] `AGENTS.md`
  - [x] `docs/architecture/README.md`
  - [x] `docs/architecture/03-runtime-boundaries.md`
  - [x] `docs/architecture/05-project-and-folder-guide.md`
  - [x] `docs/architecture/10-v2-contract-ownership.md`
  - [x] `BlazorShop.PresentationV2/COMPONENT-MODES.md`
  - [x] `docs/refactor-control-Commerce-storefront/Storefront Component Mode Foundation v2.todo.md`
  - [x] this file
- [x] Update stale H1 status wording if still present:
  - [x] `Storefront Component Mode Foundation v2.todo.md` header should not imply H1 is unimplemented if implementation notes show closure.
  - [x] `COMPONENT-MODES.md` should not end with "Until H1 is complete" wording after H1 is complete.
- [x] Record current direct references for:
  - [x] `BlazorShop.Storefront.Components`
  - [x] `BlazorShop.Storefront.Components.Ssr`
  - [x] `BlazorShop.Storefront.Components.Hybrid`
  - [x] `BlazorShop.Storefront.Components.WasmHost`
  - [x] `BlazorShop.Storefront.Browser`
  - [x] `BlazorShop.Storefront.Presentation`
  - [x] `BlazorShop.Storefront.V2`
  - [x] `BlazorShop.Storefront.V2.WASM`
- [x] Record all current render-mode placements:

```powershell
rg -n "@rendermode|InteractiveWebAssembly|InteractiveServer|InteractiveAuto" BlazorShop.PresentationV2/BlazorShop.Storefront*
```

- [x] Confirm no existing `/component-mvp`, `/__qa/component-mvp`, or `StorefrontHybridRuntimeProbe` implementation exists.

### Exit Criteria

- [x] H1 docs no longer contradict H1 implementation state.
- [x] H2 starts from a recorded clean baseline.
- [x] No production behavior changes have been made yet.

Implementation notes:

- 2026-08-10: branch is `Hybrid-Architecture`.
- 2026-08-10: initial status showed only this H2 plan file as untracked.
- 2026-08-10: predecessor H1 plan header was updated from `Status: planned` to `Status: complete`.
- 2026-08-10: `COMPONENT-MODES.md` no longer ends with stale "Until H1 is complete" wording; it now records H2 runtime proof as the next decision point.
- 2026-08-10: current project graph is unchanged: Components has no project references; Ssr references Components and Presentation; Hybrid references Components, Presentation, and WasmHost; WasmHost references Components and Browser; Browser references Components; Presentation references Components and Runtime; V2 references ServiceDefaults, Browser, Components, Components.Ssr, Presentation, and V2.WASM; V2.WASM references Browser, Components, and Components.WasmHost.
- 2026-08-10: current render-mode placements are all `InteractiveWebAssembly`: the historical Hybrid contact bridge, V2 account host, V2 catalog discounted rail, V2 content contact section, V2 cart shell, and V2 checkout shells. No `InteractiveServer` or `InteractiveAuto` placement was found in the checked Storefront projects.
- 2026-08-10: search found no existing `/component-mvp`, `/__qa/component-mvp`, or `StorefrontHybridRuntimeProbe` implementation outside this H2 plan file.
- 2026-08-10: no production behavior changed in H2.0.

## Phase H2.1 - Add Presentation-Owned Component MVP Route

### Goal

Provide a deterministic QA route without moving route ownership into V2.

### Tasks

- [x] Add a new Presentation route page under the appropriate Presentation page folder.
- [ ] Preferred route:

```razor
@page "/__qa/component-mvp"
```

- [x] The route page should use the existing Storefront page shell pattern.
- [x] The route page should render a new view slot or narrowly scoped outlet that V2 can supply.
- [x] If the route requires a context object, define it in Presentation, not V2.
- [x] The context should contain only browser-safe, deterministic proof data:
  - [x] brand/logo context or enough data to build it;
  - [x] no secrets;
  - [x] no customer identity;
  - [x] no cart token;
  - [x] no order/payment data.
- [x] Ensure the route is not included in:
  - [x] main navigation;
  - [x] footer navigation;
  - [x] sitemap;
  - [x] page template catalog;
  - [x] public content page catalog.
- [x] Add `noindex` metadata for this route, or guard it behind Development/QA configuration.
- [x] Do not add `@page` directives in V2 or V2.WASM.

### Candidate Files

- [x] `BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Pages/.../ComponentMvpRoutePage.razor`
- [x] `BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Services/.../StorefrontComponentMvpPageContext.cs`
- [x] `BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Views/Foundation/StorefrontFoundationViewSet.cs`
- [x] `BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Views/Foundation/StorefrontFoundationViewOptionsValidator.cs`

### Exit Criteria

- [x] Route is owned by Presentation.
- [x] V2 can provide the visual implementation through the existing foundation view model.
- [x] V2/V2.WASM still have no visual `@page` route directives.
- [x] Route is hidden from public navigation/discovery.

Implementation notes:

- 2026-08-10: added Presentation route `Pages/Ssr/System/ComponentMvpRoutePage.razor` with `@page "/__qa/component-mvp"`.
- 2026-08-10: added Presentation-owned `StorefrontComponentMvpPageContext` containing only `StorefrontBrandLogoContext`; no secrets, identity, cart token, order, payment, or API data is included.
- 2026-08-10: added optional Foundation view slot `ComponentMvpLab`; it validates the expected Presentation context when registered and does not force Starter migration.
- 2026-08-10: route uses `StorefrontPage` and `StorefrontPageDocument` with `RobotsIndex = false` and `RobotsFollow = false`.
- 2026-08-10: the route is not referenced by navigation, footer, sitemap, template catalog, or content catalog source.
- 2026-08-10: no `@page` directive was added to V2 or V2.WASM.
- 2026-08-10: `dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/BlazorShop.Storefront.Presentation.csproj --no-restore` passed with 0 warnings and 0 errors.

## Phase H2.2 - Add V2 Visual Lab View

### Goal

Let V2 compose and style the proof surface while Presentation keeps route ownership.

### Tasks

- [x] Add a V2 visual component for the lab view.
- [x] Register it in `V2FoundationViewRegistration`.
- [x] Include exactly three visible proof sections:
  - [x] SSR proof;
  - [x] Hybrid proof;
  - [x] WasmHost proof.
- [x] Add stable semantic QA hooks:

```html
data-storefront-component-mvp
data-storefront-component-mvp-section="ssr|hybrid|wasmhost"
```

- [x] Keep V2 labels, copy, and CSS classes in V2.
- [x] Do not introduce a general gallery/design-system framework.
- [x] Do not add route registration in reusable component libraries.

### Candidate Files

- [x] `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Components/System/StorefrontComponentMvpLab.razor`
- [x] `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/V2FoundationViewRegistration.cs`

### Exit Criteria

- [x] `/__qa/component-mvp` renders V2-owned visual lab markup.
- [x] All three proof areas are visible and have stable selectors.
- [x] No general visual framework is introduced.

Implementation notes:

- 2026-08-10: added `StorefrontComponentMvpLab` under V2 `Components/System` and registered it as the `ComponentMvpLab` foundation view slot.
- 2026-08-10: lab view renders exactly three sections with `data-storefront-component-mvp-section="ssr"`, `"hybrid"`, and `"wasmhost"`.
- 2026-08-10: labels, copy, and `.bs-storefront-component-mvp*` CSS classes live in V2; no reusable component library route or visual framework was added.
- 2026-08-10: `dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.V2/BlazorShop.Storefront.V2.csproj --no-restore` passed with 0 warnings and 0 errors.

## Phase H2.3 - SSR Proof With StorefrontBrandLogo

### Goal

Prove the existing SSR component is useful from raw server HTML.

### Tasks

- [x] Render `StorefrontBrandLogo` in the SSR section of the V2 lab view.
- [x] Supply `StorefrontBrandLogoContext` and `StorefrontBrandLogoClasses` from V2.
- [x] Confirm the component output includes:
  - [x] anchor root;
  - [x] `data-storefront-component="brand-logo"`;
  - [x] `data-storefront-brand`;
  - [x] logo image or brand-name fallback;
  - [x] accessible label.
- [x] Confirm `StorefrontBrandLogo` still has:
  - [x] no `@rendermode`;
  - [x] no Browser dependency;
  - [x] no Runtime/Client dependency;
  - [x] no direct API calls;
  - [x] no V2 literal theme classes inside the reusable component.

### Tests

- [x] Focused component test for context/classes/fallback.
- [x] Raw HTML Playwright/API assertion that SSR markup exists before browser JS executes.

### Exit Criteria

- [x] SSR proof appears in raw initial HTML.
- [x] SSR proof remains useful with JavaScript disabled or before WASM starts.

Implementation notes:

- 2026-08-10: `StorefrontComponentMvpLab` now renders `StorefrontBrandLogo` in the `ssr` section, using the Presentation-owned `Context.BrandLogo` and V2-owned `StorefrontBrandLogoClasses`.
- 2026-08-10: `StorefrontBrandLogo` source remains unchanged and still has no `@rendermode`, Browser dependency, Runtime/Client dependency, direct API call, or V2 literal theme classes.
- 2026-08-10: added `StorefrontComponentMvpLabTests.RendersBrandLogoInSsrSectionWithRawServerHtml`, which renders the lab with `HtmlRenderer` and asserts the SSR raw HTML includes anchor, component marker, brand marker, image URL, alt text, accessible label, and V2 class slots before any browser JS/hydration.
- 2026-08-10: `dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~StorefrontBrandLogoComponentTests|FullyQualifiedName~StorefrontComponentMvpLabTests"` passed: 3 passed, 0 failed. Existing MessagePack NU1902/NU1903 and Browserslist warnings remain unrelated.

## Phase H2.4 - Add Hybrid Runtime Probe Contract

### Goal

Create the smallest proof of Hybrid runtime state without business logic.

### Physical Placement Decision

Preferred placement:

```text
BlazorShop.Storefront.Components.WasmHost
```

Reason:

- The implementation must execute in the downloaded WASM graph.
- H1 decoupled semantic mode from physical project name.
- `Components.Hybrid` is transitional and must not receive new reusable components before H2 decides its permanent role.

Acceptable alternative:

```text
BlazorShop.Storefront.V2.WASM
```

Only use this if the proof is intentionally V2-only and no public reusable descriptor is needed.

Do not place new reusable probe implementation in `Components.Hybrid`.

### Component Requirements

- [x] Name: `StorefrontHybridRuntimeProbe`.
- [ ] Component root:

```html
<section
  data-storefront-component="hybrid-runtime-probe"
  data-storefront-hybrid-probe
  data-storefront-runtime-state="prerender|interactive">
```

- [x] Use `RendererInfo.IsInteractive` as the source of runtime state.
- [x] Initial counter value is `0`.
- [x] Prerender output shows:
  - [x] runtime state = `prerender`;
  - [x] value = `0`;
  - [x] useful static HTML.
- [x] Hydrated output shows:
  - [x] runtime state = `interactive`;
  - [x] value = `0`.
- [x] Button click increments value:
  - [x] `0 -> 1`;
  - [x] exactly once per click.
- [ ] Stable selectors:

```html
data-storefront-hybrid-value
data-storefront-hybrid-action
```

- [x] No API call.
- [x] No database.
- [x] No cart/checkout/auth/order dependency.
- [x] No `HttpClient`.
- [x] No server-only service injection.
- [x] No V2 theme classes.
- [x] No hardcoded final storefront copy unless supplied by V2/V2.WASM wrapper parameters.

### Descriptor

Add a descriptor only if it fits the current descriptor model.

Recommended descriptor if component lives in `Components.WasmHost`:

```text
Key: hybrid-runtime-probe
Mode: Hybrid
Category: System
ComponentType: StorefrontHybridRuntimeProbe
```

Do not add a new enum category just for the probe.

### Tests

- [x] Descriptor validates if descriptor is added.
- [x] Descriptor tests prove `Mode = Hybrid` can live outside `Components.Hybrid`.
- [x] Component unit test covers initial semantic markup where practical.
- [x] Runtime transition is browser-tested, not faked only in unit tests.

### Exit Criteria

- [x] Probe builds in the downloadable WASM graph.
- [x] Probe exposes actual runtime state.
- [x] Probe is small and has no business/BFF dependency.

Implementation notes:

- 2026-08-10: added browser-safe `StorefrontHybridRuntimeProbeLabels` and `StorefrontHybridRuntimeProbeClasses` contracts under `BlazorShop.Storefront.Components/Contracts/System`.
- 2026-08-10: added `StorefrontHybridRuntimeProbe` under `BlazorShop.Storefront.Components.WasmHost/System`; it uses `RendererInfo.IsInteractive`, renders `data-storefront-runtime-state="prerender"` before interactivity, starts at value `0`, and increments a local C# counter through `@onclick`.
- 2026-08-10: added `StorefrontHybridRuntimeProbeDescriptor` with key `hybrid-runtime-probe`, `Mode = Hybrid`, `Category = System`, and component type in the WasmHost assembly.
- 2026-08-10: fixed direct namespace shadowing caused by adding `BlazorShop.Storefront.Components.Contracts.System`; `StorefrontComponentDescriptorValidator` now imports `global::System.Text.RegularExpressions`.
- 2026-08-10: first H2.4 build attempt failed because `Contracts.System` shadowed `System.Text.RegularExpressions`; after the `global::System` fix, `dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.Components.WasmHost/BlazorShop.Storefront.Components.WasmHost.csproj --no-restore` passed with 0 warnings and 0 errors.
- 2026-08-10: `dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~StorefrontHybridRuntimeProbeComponentTests|FullyQualifiedName~StorefrontComponentDescriptorTests|FullyQualifiedName~ContractModelInventory_RecordsReusableProductAndCatalogContracts"` passed: 25 passed, 0 failed. Existing MessagePack NU1902/NU1903 and Browserslist warnings remain unrelated.
- 2026-08-10: unit tests cover prerender/static markup and descriptor semantics; real hydrated transition/click evidence remains mandatory and is scheduled for H2.9.

## Phase H2.5 - Integrate Hybrid Probe Through V2.WASM Composition

### Goal

Prove real V2 composition with host-owned render mode.

### Tasks

- [x] Add a V2.WASM wrapper/root if needed by existing composition pattern.
- [x] The wrapper may supply labels/classes/options.
- [x] The wrapper should not duplicate runtime state logic.
- [x] V2 lab view renders the wrapper/root with:

```razor
@rendermode="InteractiveWebAssembly"
```

- [x] Prerender remains enabled.
- [x] Parameters crossing static-to-interactive boundary are JSON serializable.
- [x] Do not pass `RenderFragment` across static-to-interactive boundary unless verified safe.
- [x] Do not inject Presentation/Runtime/Client into browser-executed component code.
- [x] Ensure the component assembly is included in the V2.WASM downloadable graph.
- [x] Ensure `MapStorefrontApplication(...)` additional assemblies remain sufficient.

### Exit Criteria

- [x] Initial HTTP response includes Hybrid probe HTML.
- [x] Browser later observes `interactive` marker.
- [x] Button click changes C# component state.
- [x] No server-interactive mechanism is introduced.

Implementation notes:

- 2026-08-10: added V2.WASM `StorefrontHybridRuntimeProbeSection`, which supplies V2-owned labels/classes and delegates runtime state/counter logic to `StorefrontHybridRuntimeProbe`.
- 2026-08-10: `StorefrontComponentMvpLab` renders `<StorefrontHybridRuntimeProbeSection @rendermode="InteractiveWebAssembly" />`; no `InteractiveServer`, `InteractiveAuto`, route directive, `RenderFragment`, Presentation/Runtime/Client injection, or server-interactive path was introduced.
- 2026-08-10: V2.WASM already references `BlazorShop.Storefront.Components.WasmHost`, so the probe is in the downloadable WASM graph. Existing `MapStorefrontApplication(...)` additional assemblies remain sufficient because it already maps the V2.WASM assembly and the WasmHost assembly.
- 2026-08-10: `dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM/BlazorShop.Storefront.V2.WASM.csproj --no-restore` passed with 0 warnings and 0 errors.
- 2026-08-10: `dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.V2/BlazorShop.Storefront.V2.csproj --no-restore` passed with 0 warnings and 0 errors.
- 2026-08-10: first direct HtmlRenderer test of the full V2 lab failed because `HtmlRenderer` does not support rendering a child with `InteractiveWebAssembly` render mode outside a Blazor Web App host. The test was corrected to source-guard the V2 placement and render the V2.WASM wrapper directly for prerender markup.
- 2026-08-10: `dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~StorefrontComponentMvpLabTests|FullyQualifiedName~StorefrontHybridRuntimeProbeComponentTests"` passed: 5 passed, 0 failed. Existing MessagePack NU1902/NU1903 and Browserslist warnings remain unrelated.
- 2026-08-10: browser-observed `interactive` marker and click state are implemented here and remain explicitly proven by the mandatory H2.9 Playwright gate.

## Phase H2.6 - WasmHost Proof With Discounted Product Rail

### Goal

Prove browser-interactive reusable component data flow through approved Browser/BFF architecture.

### Tasks

- [x] Render `StorefrontDiscountedProductRailSection` or an equivalent V2.WASM-owned wrapper in the WasmHost section.
- [x] Preserve action route as same-origin BFF, not Commerce Node:

```text
/api/catalog/discounted-products
```

- [x] Preserve data path:

```text
StorefrontDiscountedProductRail
  -> IStorefrontBrowserProductRailController
  -> StorefrontLocalApiClient
  -> same-origin Presentation BFF
  -> Runtime
  -> Commerce Node Storefront API
```

- [x] Confirm states are observable:
  - [x] loading;
  - [x] success;
  - [x] empty;
  - [x] error;
  - [x] retry.
- [x] Tests must not depend on uncontrolled store catalog data.
- [x] Use deterministic seeded fixture or Playwright network route mocking for BFF response states.
- [x] Do not add backend discount API behavior in H2.

### Exit Criteria

- [x] Rail works inside the MVP route.
- [x] Browser calls only same-origin local endpoint.
- [x] Error and retry behavior can be proven deterministically.

Implementation notes:

- 2026-08-10: `StorefrontComponentMvpLab` now renders the existing V2.WASM-owned `StorefrontDiscountedProductRailSection` in the `wasmhost` proof section with `@rendermode="InteractiveWebAssembly"`.
- 2026-08-10: wrapper source still supplies `StorefrontDiscountedProductRailActionDescriptor` with same-origin BFF route `/api/catalog/discounted-products`; no direct `api/storefront/stores`, `CommerceNode`, or `HttpClient` dependency was introduced in the wrapper.
- 2026-08-10: existing product rail/controller tests cover deterministic loading, success, empty, error, retry, and browser controller behavior without uncontrolled store catalog data. Runtime browser route mocking remains the mandatory H2.10 proof.
- 2026-08-10: no backend discount API behavior was added in H2.6.
- 2026-08-10: `dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.V2/BlazorShop.Storefront.V2.csproj --no-restore` passed with 0 warnings and 0 errors.
- 2026-08-10: first focused H2.6 test run failed because a source assertion expected a non-target-typed constructor string. The assertion was corrected to lock behavior instead of syntax.
- 2026-08-10: `dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~StorefrontComponentMvpLabTests|FullyQualifiedName~StorefrontDiscountedProductRailComponentTests|FullyQualifiedName~StorefrontBrowserProductRailControllerTests"` passed: 16 passed, 0 failed. Existing MessagePack NU1902/NU1903 and Browserslist warnings remain unrelated.

## Phase H2.7 - Focused Component And Architecture Tests

### Goal

Keep fast regression tests for contracts and boundary rules.

### Required Tests

- [x] `StorefrontBrandLogoComponentTests` update or add coverage if lab usage changes class/context assumptions.
- [x] New `StorefrontHybridRuntimeProbeComponentTests` for:
  - [x] root marker;
  - [x] value marker;
  - [x] action marker;
  - [x] label/classes parameters if present;
  - [x] descriptor validation if descriptor exists.
- [x] `StorefrontComponentDescriptorTests` update:
  - [x] expected descriptor inventory includes `hybrid-runtime-probe` if public descriptor is added;
  - [x] no owner assembly equals mode assumption is reintroduced.
- [x] `StorefrontComponentModeDependencyTests` update only if project references actually change.
- [x] Visual neutrality tests:
  - [x] reusable component has no literal V2 Tailwind classes;
  - [x] V2/V2.WASM own visual classes.
- [x] Route ownership test:
  - [x] no `@page` directive in V2/V2.WASM;
  - [x] `/__qa/component-mvp` route exists only in Presentation.
- [x] Bootstrap test:
  - [x] V2 maps required additional assemblies for V2.WASM/WasmHost.

### Exit Criteria

- [x] Fast tests catch descriptor, route, and visual boundary regressions.
- [x] Runtime lifecycle remains delegated to browser Playwright tests.

Implementation notes:

- 2026-08-10: added `StorefrontComponentMvpArchitectureTests` to lock Presentation-owned `/__qa/component-mvp`, noindex metadata, optional `ComponentMvpLab` view slot, V2 visual composition, and V2 bootstrap assembly mapping for V2.WASM plus WasmHost.
- 2026-08-10: existing `StorefrontHybridRuntimeProbeComponentTests` covers root/value/action markers, host-supplied labels/classes, no API/server injection/render mode, and descriptor validation remains in `StorefrontComponentDescriptorTests`.
- 2026-08-10: existing visual neutrality and dependency tests still cover reusable component projects; H2.7 did not change project references.
- 2026-08-10: `dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~StorefrontBrandLogoComponentTests|FullyQualifiedName~StorefrontDiscountedProductRailComponentTests|FullyQualifiedName~StorefrontComponentDescriptorTests|FullyQualifiedName~StorefrontComponentModeDependencyTests|FullyQualifiedName~StorefrontComponentVisualNeutralityTests|FullyQualifiedName~StorefrontVisualOnlyBoundaryTests|FullyQualifiedName~StorefrontHybridRuntimeProbeComponentTests|FullyQualifiedName~StorefrontComponentMvpArchitectureTests|FullyQualifiedName~StorefrontComponentMvpLabTests"` passed: 75 passed, 0 failed. Existing MessagePack NU1902/NU1903 and Browserslist warnings remain unrelated.

## Phase H2.8 - Playwright Raw HTML Proof

### Goal

Prove prerender state before browser JavaScript runs.

### Test Method

Use Playwright `request.get(...)`, an HTTP client, or browser route blocking for `_framework/*` to inspect the initial document response before WASM executes.

### Required Assertions

- [ ] `GET /__qa/component-mvp` returns HTTP 200.
- [ ] Response HTML contains:

```html
data-storefront-component-mvp
data-storefront-component="brand-logo"
data-storefront-component="hybrid-runtime-probe"
data-storefront-runtime-state="prerender"
data-storefront-hybrid-value
```

- [ ] Response HTML contains useful initial Hybrid content.
- [ ] Response HTML contains SSR proof markup.
- [ ] Response HTML does not require a completed WASM startup to satisfy State A.
- [ ] Route returns noindex metadata or is gated from production indexing.

### Exit Criteria

- [ ] State A is proven independently from hydration.
- [ ] Evidence block is added to this file or QA checklist.

## Phase H2.9 - Playwright Hydrated Hybrid Proof

### Goal

Prove the Hybrid component becomes WebAssembly-interactive and owns C# interaction in the browser.

### Test Flow

```text
navigate /__qa/component-mvp
wait for hybrid root
wait for data-storefront-runtime-state="interactive"
assert value = 0
click data-storefront-hybrid-action
assert value = 1
click again if desired
assert value = 2
```

### Required Assertions

- [ ] Browser eventually sees `data-storefront-runtime-state="interactive"`.
- [ ] The test waits on the component marker, not arbitrary fixed timeout.
- [ ] One click increments exactly once.
- [ ] No hydration console error.
- [ ] No page error.
- [ ] No direct Commerce Node request during probe-only interaction.
- [ ] No `InteractiveServer` or `InteractiveAuto` path is needed.

### Exit Criteria

- [ ] State B is proven.
- [ ] C# event handling after WASM hydration is proven.

## Phase H2.10 - Playwright WasmHost Rail Proof

### Goal

Prove rail behavior through Browser controller and same-origin BFF.

### Test Cases

- [ ] Loading state:
  - [ ] route fulfills or delays `/api/catalog/discounted-products`;
  - [ ] `data-storefront-product-rail-loading` appears.
- [ ] Success state:
  - [ ] BFF returns deterministic product list;
  - [ ] `data-storefront-product-rail-list` appears;
  - [ ] `data-storefront-product-rail-item` count matches fixture.
- [ ] Empty state:
  - [ ] BFF returns empty product list;
  - [ ] `data-storefront-product-rail-empty` appears;
  - [ ] stale products are not shown.
- [ ] Error state:
  - [ ] BFF returns retryable failure or transport error;
  - [ ] `data-storefront-product-rail-error` appears;
  - [ ] safe code/default message is exposed.
- [ ] Retry state:
  - [ ] first request fails;
  - [ ] retry button is clicked;
  - [ ] second request succeeds;
  - [ ] request count proves retry invoked Browser controller again.

### Network Assertions

- [ ] Browser calls same-origin `/api/catalog/discounted-products`.
- [ ] Browser does not call `api/storefront/stores/{storeKey}/*` directly.
- [ ] Browser does not call Commerce Node host/port directly.
- [ ] Node credentials never appear in request headers or browser storage.

### Exit Criteria

- [ ] WasmHost proof covers loading/success/empty/error/retry.
- [ ] Browser/BFF boundary is proven from network evidence.

## Phase H2.11 - Runtime Transport Audit

### Goal

Record real runtime transport behavior for future H3 guardrails.

### Tasks

- [ ] Capture network activity while loading `/__qa/component-mvp`.
- [ ] Classify requests:
  - [ ] document;
  - [ ] static assets;
  - [ ] `_framework` WASM assets;
  - [ ] same-origin BFF requests;
  - [ ] WebSocket connections;
  - [ ] EventSource connections.
- [ ] Assert no public Storefront Blazor Server UI circuit is required.
- [ ] Do not fail solely because dev tooling/hot reload opens a development websocket.
- [ ] Specifically inspect for app UI circuit endpoints such as `/_blazor`.
- [ ] Confirm Hybrid button interaction does not depend on a persistent server UI connection.

### Exit Criteria

- [ ] Transport evidence is recorded.
- [ ] H3 can harden only rules proven by runtime evidence.

## Phase H2.12 - QA Checklist Update

### Goal

Make H2 browser checks discoverable in production QA tracking.

### Tasks

- [ ] Update `docs/refactor-control-Commerce-storefront/QA-StorefrontV2.todo.md`.
- [ ] Add a section for Component MVP runtime proof.
- [ ] Include checkboxes for:
  - [ ] raw HTML SSR proof;
  - [ ] raw HTML Hybrid prerender proof;
  - [ ] hydrated Hybrid interactive proof;
  - [ ] Hybrid counter/action proof;
  - [ ] WasmHost loading/success/empty/error/retry proof;
  - [ ] no direct Commerce Node browser call;
  - [ ] no public Blazor Server UI circuit;
  - [ ] route not in menu/sitemap;
  - [ ] noindex or QA-only route behavior.
- [ ] Record exact Playwright command once implemented.
- [ ] Record evidence date and result counts at closure.

### Exit Criteria

- [ ] QA checklist is production-release usable.
- [ ] Browser tests are not described as smoke tests.

## Phase H2.13 - Documentation Update

### Goal

Align architecture docs with observed runtime behavior after implementation.

### Files

- [ ] `BlazorShop.PresentationV2/COMPONENT-MODES.md`
- [ ] `docs/architecture/03-runtime-boundaries.md`
- [ ] `docs/architecture/05-project-and-folder-guide.md`
- [ ] `docs/architecture/10-v2-contract-ownership.md`
- [ ] `BlazorShop.PresentationV2/BlazorShop.Storefront.Components.Hybrid/README.md`
- [ ] This H2 file

### Tasks

- [ ] Document actual physical location of `StorefrontHybridRuntimeProbe`.
- [ ] Document actual route ownership:

```text
Presentation route shell + V2 visual view + V2.WASM/WasmHost interactive roots
```

- [ ] Document actual two-state Hybrid evidence:
  - [ ] prerender;
  - [ ] interactive;
  - [ ] C# click state.
- [ ] Document actual network findings.
- [ ] Decide and record future of `Components.Hybrid`:
  - [ ] keep;
  - [ ] narrow;
  - [ ] retire in H3.
- [ ] Do not delete `Components.Hybrid` in H2 unless deletion is isolated, tests are updated, and V2 visible behavior is unaffected.

### Exit Criteria

- [ ] Docs match implemented behavior.
- [ ] H3 receives a concrete cleanup decision, not speculation.

## Phase H2.14 - Build Gates

### Required Focused Builds

Run in this order:

```powershell
dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.Components/BlazorShop.Storefront.Components.csproj --no-restore
dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.Components.Ssr/BlazorShop.Storefront.Components.Ssr.csproj --no-restore
dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.Components.WasmHost/BlazorShop.Storefront.Components.WasmHost.csproj --no-restore
dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.Components.Hybrid/BlazorShop.Storefront.Components.Hybrid.csproj --no-restore
dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.Browser/BlazorShop.Storefront.Browser.csproj --no-restore
dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/BlazorShop.Storefront.Presentation.csproj --no-restore
dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM/BlazorShop.Storefront.V2.WASM.csproj --no-restore
dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.V2/BlazorShop.Storefront.V2.csproj --no-restore
```

Attempt broader build only if environment is healthy:

```powershell
dotnet build BlazorShop.sln --no-restore
```

### Exit Criteria

- [ ] All affected focused projects build.
- [ ] Any skipped broader build has a concrete reason.

## Phase H2.15 - Test Gates

### Required Focused Tests

Start with component and architecture tests:

```powershell
dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~StorefrontBrandLogoComponentTests|FullyQualifiedName~StorefrontDiscountedProductRailComponentTests|FullyQualifiedName~StorefrontComponentDescriptorTests|FullyQualifiedName~StorefrontComponentModeDependencyTests|FullyQualifiedName~StorefrontComponentVisualNeutralityTests|FullyQualifiedName~StorefrontVisualOnlyBoundaryTests"
```

Add new focused tests for H2:

```powershell
dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~StorefrontHybridRuntimeProbe|FullyQualifiedName~ComponentMvp|FullyQualifiedName~StorefrontComponentMvp"
```

Run browser/controller tests if affected:

```powershell
dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~StorefrontBrowserProductRailControllerTests|FullyQualifiedName~StorefrontDiscountedProductRailPresentationTests"
```

### Required Playwright Tests

Add and run a real browser suite. Suggested logical names:

```text
ComponentMvp_RawHtml_ContainsSsrAndHybridPrerender
ComponentMvp_Hybrid_BecomesInteractive
ComponentMvp_Hybrid_ClickIncrementsCSharpState
ComponentMvp_WasmHost_LoadingState
ComponentMvp_WasmHost_SuccessState
ComponentMvp_WasmHost_EmptyState
ComponentMvp_WasmHost_ErrorAndRetry
ComponentMvp_Network_UsesSameOriginBffOnly
ComponentMvp_Network_DoesNotRequireServerUiCircuit
ComponentMvp_NoConsoleOrPageErrors
```

### Exit Criteria

- [ ] Focused unit/architecture tests pass.
- [ ] Browser Playwright tests pass.
- [ ] Exact commands and results are recorded in this file and QA checklist.

## Phase H2.16 - H2 Closure And H3 Handoff

### Closure Checklist

- [ ] Presentation owns `/__qa/component-mvp` route.
- [ ] V2 owns visual lab markup/classes/copy.
- [ ] V2/V2.WASM contain no new `@page` route directives.
- [ ] SSR proof is visible in raw initial HTML.
- [ ] Hybrid proof is visible in raw initial HTML with `prerender` marker.
- [ ] Hybrid proof hydrates to `interactive` marker.
- [ ] Hybrid C# click changes state exactly once per click.
- [ ] WasmHost rail loads through Browser controller and same-origin BFF.
- [ ] WasmHost rail loading/success/empty/error/retry are proven.
- [ ] Browser network does not call Commerce Node directly.
- [ ] Browser network does not require public Blazor Server UI circuit.
- [ ] Route is noindex or QA-only and not public-discovery linked.
- [ ] Documentation updated.
- [ ] QA checklist updated.
- [ ] Future of `Components.Hybrid` recorded for H3.
- [ ] No backend, Control Plane, Starter, StorefrontBuilder, or generated storefront changes entered H2.

### H3 Handoff Questions

Answer after H2 evidence:

- [ ] Should `Components.Hybrid` remain as a compatibility package?
- [ ] Should `Components.Hybrid` be retired after historical contact bridge migration?
- [ ] Should future reusable packages be capability-based instead of mode-based?
- [ ] Which render-mode placements should H3 guard with repository-wide scanners?
- [ ] Which network assertions are stable enough for H3 static/browser guardrails?
- [ ] Should `/__qa/component-mvp` remain as long-term QA evidence or be removed after H3?

### Suggested Commit Breakdown

```text
docs(storefront): plan reference component mvp
docs(storefront): close component mode foundation wording
feat(storefront): add presentation component mvp route
feat(storefront): add v2 component mvp lab view
feat(storefront): add hybrid runtime probe
test(storefront): cover component mvp architecture boundaries
test(storefront): prove hybrid prerender and wasm interaction
test(storefront): prove wasmhost component mvp browser flow
docs(storefront): record component mvp runtime evidence
docs(storefront): hand off component hardening decisions
```

## Definition Of Done

H2 is complete only when all are true:

- [ ] `StorefrontBrandLogo` proves SSR in raw initial HTML.
- [ ] `StorefrontHybridRuntimeProbe` proves prerender -> WASM interactive -> C# state change.
- [ ] `StorefrontDiscountedProductRail` proves WasmHost Browser/BFF dynamic flow.
- [ ] Route ownership remains in Presentation.
- [ ] Visual ownership remains in V2/V2.WASM.
- [ ] Reusable components do not own V2 classes/copy/routes/render mode.
- [ ] Browser/WASM code does not call Commerce Node directly.
- [ ] No `InteractiveServer` or `InteractiveAuto` path is introduced.
- [ ] Playwright covers real browser behavior, not only smoke checks.
- [ ] QA checklist and architecture docs are synchronized.
- [ ] H3 cleanup decisions are grounded in H2 evidence.

## Autoplan Review Report

### Plan Summary

H2 should proceed as a runtime proof phase with one SSR proof, one Hybrid proof, and one WasmHost proof. The important correction is that the MVP route must be Presentation-owned while V2 owns the visual lab implementation.

### CEO Review

Premises accepted:

- Proving the architecture with three real runtime examples is the right next step after H1 semantic foundation.
- A small Hybrid runtime probe is better than using ContactForm as the canonical proof because it isolates hydration and interaction from BFF/form complexity.
- Keeping H2 narrow avoids turning a proof phase into broad component extraction.

Risk avoided:

- If H2 skips browser/runtime evidence, H3 guardrails would be speculative and may block legitimate patterns or miss real hydration failures.

### Design Review

UI scope is limited to a QA lab, not a customer-facing design page. The lab still needs explicit states and stable selectors because Playwright must inspect behavior deterministically.

Design decisions:

- Keep layout utilitarian and compact.
- Show three proof sections clearly.
- Do not build a design-system showcase.
- Do not rely on screenshots as the source of truth for prerender state.

### Engineering Review

Architecture concern found and resolved:

- Original wording "V2-owned MVP route/page" conflicts with current Presentation route ownership. The plan now requires a Presentation route shell plus V2 visual view.

Primary engineering rule:

- Host/composition owns `@rendermode`; reusable component libraries own behavior/state contracts only.

### DX Review

The implementer needs a plan that prevents accidental boundary drift. This file includes exact forbidden paths, candidate files, commands, browser assertions, network assertions, and closure evidence format.

### Decision Audit Trail

| # | Phase | Decision | Classification | Principle | Rationale | Rejected |
| --- | --- | --- | --- | --- | --- | --- |
| 1 | Intake | Create a new H2 todo file instead of editing H1 | Mechanical | Explicit over clever | H1 is predecessor and already has closure notes; H2 needs an independent implementation checklist. | Append H2 into H1 file |
| 2 | Architecture | Use Presentation-owned route plus V2 visual lab | Mechanical | DRY / existing boundary | Current repo routes live in Presentation and V2 view registration supplies visuals. | Add `@page` route to V2 |
| 3 | Architecture | Place new Hybrid probe in downloadable WASM graph, preferably `Components.WasmHost` | Mechanical | Existing boundary | Browser-executed code must be in WASM graph; `Components.Hybrid` is transitional. | Add new reusable probe to `Components.Hybrid` |
| 4 | QA | Make Playwright mandatory for H2 | Mechanical | Completeness | H2 changes visible runtime/hydration behavior, so unit tests alone cannot prove the two-state requirement. | Treat H2 as smoke/unit-only |
| 5 | Scope | Defer deleting `Components.Hybrid` to H3 unless isolated and safe | Taste | Pragmatic | H2 should produce evidence and decision; deletion may broaden scope. | Delete `Components.Hybrid` during the proof by default |
| 6 | Route Exposure | Prefer `/__qa/component-mvp` hidden/noindex over public `/component-mvp` | Mechanical | Explicit over clever | It clearly communicates QA/runtime-proof intent and avoids public design-system interpretation. | Add public route without noindex/discovery guard |

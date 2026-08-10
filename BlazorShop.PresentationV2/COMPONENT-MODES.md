# Storefront Component Modes

This document is the current source of truth for BlazorShop Storefront component mode language. The original component mode foundation is complete and real reference components now exist. This document supersedes older plan wording that described mode projects as foundation-only or treated Hybrid as a mandatory nested shell pattern.

BlazorShop component modes are architecture classifications. They are not a one-to-one copy of ASP.NET Core render mode names, and they do not by themselves decide the final physical project graph.

## Projects

The current reusable component projects live beside the active Storefront packages under `BlazorShop.PresentationV2`:

- `BlazorShop.Storefront.Components.Ssr`
- `BlazorShop.Storefront.Components.Primitives`
- `BlazorShop.Storefront.Components.WasmHost`

Do not recreate `BlazorShop.Storefront.Components/Features`. The retired `Features` folder must remain absent unless a later architecture decision reopens it.

Do not add replacement shared-component projects without an approved follow-up phase:

- `BlazorShop.Storefront.Components.Common`
- `BlazorShop.Storefront.Features.Contracts`
- `BlazorShop.Storefront.ComponentRuntime`
- `BlazorShop.Storefront.ComponentRegistry`

H3 retired the old physical Hybrid compatibility project after moving its descriptor and deleting the historical contact bridge. Hybrid remains a semantic mode; it is not a required project name.

## Current Project Graph

The current direct reusable component project references are guardrails from the completed foundation/reference work:

```text
BlazorShop.Storefront.Components.Ssr
  -> BlazorShop.Storefront.Components
  -> BlazorShop.Storefront.Presentation

BlazorShop.Storefront.Components.Primitives
  -> BlazorShop.Storefront.Components

BlazorShop.Storefront.Components.WasmHost
  -> BlazorShop.Storefront.Components
  -> BlazorShop.Storefront.Browser
```

The graph above describes the current repository state. H2 proved the browser-visible V2.WASM wrapper pattern for interactive roots and placed the canonical Hybrid runtime probe in the downloadable WasmHost graph.

The base `BlazorShop.Storefront.Components` project remains the lowest browser-safe contracts and headless layer. It must not reference `Presentation`, `Browser`, `Runtime`, `Client`, V2 hosts, Starter hosts, backend/core/API projects, Control Plane projects, or `Web.SharedV2`.

`BlazorShop.Storefront.Components.Primitives` is a browser-safe render-only Razor package for small reusable semantic primitives such as Product Summary cards and Product Detail gallery rendering. It is not a component mode and must not declare descriptors. It references only `BlazorShop.Storefront.Components`, consumes contracts/class slots/labels supplied by hosts, and must not own final CSS classes, store-specific copy, static assets, `@rendermode`, JS interop, HTTP/API calls, Browser controllers, Presentation services, Runtime, Client, backend/core/API projects, V2 hosts, Starter hosts, generated storefront projects, Control Plane projects, or `Web.SharedV2`.

Descriptor mode is semantic architecture metadata. Repository architecture tests validate descriptor shape, current public descriptor inventory, duplicate keys, and small contract surface, but they must not require descriptor mode to match a physical assembly or project name.

## ASP.NET Render Mode Facts

ASP.NET Core render modes are framework runtime choices:

- `Static`: static server-side rendering with no interactivity.
- `InteractiveWebAssembly`: interactive client-side Blazor WebAssembly rendering.
- `InteractiveServer`: interactive server rendering over a server circuit/real-time connection.
- `InteractiveAuto`: starts with server interactivity and later uses WebAssembly on subsequent visits after the app bundle is available.

BlazorShop public Storefront interactive behavior targets `InteractiveWebAssembly` with prerendering where needed. It does not target `InteractiveServer`, SignalR/circuit-based public storefront interactivity, or `InteractiveAuto`.

Important framework constraints for Storefront design:

- Prerendering is enabled by default for interactive components.
- `InteractiveWebAssembly` components must be built from a separate client-side project so they are included in the downloaded app bundle.
- Parameters crossing from a static parent to an interactive child must be JSON serializable.
- `RenderFragment` and child content cannot be freely passed across a static-to-interactive render-mode boundary.
- Components should avoid hard-coupling implementation assumptions to a specific render mode and should degrade gracefully where possible.
- `RendererInfo.IsInteractive` and `AssignedRenderMode` exist for future phases that need explicit runtime awareness.
- JavaScript initializers such as `beforeWebAssemblyStart` and `afterWebAssemblyStarted` are WASM startup hooks, not a reason to introduce server interactivity.

## SSR Mode

`Ssr` means a component or route surface can render its primary function from server-prepared state without requiring browser runtime.

Allowed:

- prepared Presentation contexts;
- semantic render/input contracts;
- normal Razor forms and links;
- `RenderFragment` when it stays inside SSR ownership;
- accessibility markup;
- `data-storefront-*` semantic hooks;
- class parameters or class descriptor parameters supplied by the host.

Forbidden:

- `BlazorShop.Storefront.Browser`;
- `BlazorShop.Storefront.Runtime`;
- `BlazorShop.Storefront.Client`;
- V2, V2.WASM, Starter, Starter.WASM, generated storefront projects;
- Commerce Node, Control Plane, Application, Domain, Infrastructure, or `Web.SharedV2`;
- `HttpClient`;
- `IJSRuntime`;
- `@rendermode`;
- `InteractiveWebAssembly`;
- direct `/api/*`, Commerce Node URLs, or localhost backend URLs.

SSR components must not use `@rendermode`.

## Hybrid Mode

`Hybrid` is a BlazorShop architectural classification for:

```text
server-produced/prerendered HTML or page snapshot
  + client-side WebAssembly interactivity after hydration
  + optional progressive enhancement
```

Hybrid does not mean:

- `.NET InteractiveAuto`;
- `InteractiveServer`;
- SignalR/circuit-based public storefront interactivity;
- mandatory retired-Hybrid-project to WasmHost child nesting;
- mandatory server shell to nested interactive child implementation.

Hybrid route or component surfaces may use server-prepared state, semantic action descriptors, same-origin BFF routes, and client-side WASM interactivity. The browser-visible V2 contact and discounted rail proofs currently use V2.WASM wrapper components rendered with `@rendermode InteractiveWebAssembly`, then delegate behavior to reusable WasmHost components.

`@rendermode InteractiveWebAssembly` placement is host/composition ownership. It is not a guarantee that a reusable component library owns render-mode directives, and it is not proof that a physical Hybrid project must be the composition layer.

The old physical Hybrid compatibility project is retired. Do not recreate it for new reusable components without a new architecture decision.

## WasmHost Mode

`WasmHost` means browser-side WebAssembly interactive roots that must be included in a downloadable WASM app graph. WasmHost components use Browser controllers and browser-safe contracts. They do not call backend APIs directly and do not self-own routes.

Allowed:

- `BlazorShop.Storefront.Components`;
- `BlazorShop.Storefront.Browser`;
- browser controllers;
- browser-safe state and action contracts;
- `EventCallback`;
- component lifecycle for interaction;
- `IJSRuntime` only for real browser behavior.

Forbidden:

- `BlazorShop.Storefront.Presentation`;
- `BlazorShop.Storefront.Runtime`;
- `BlazorShop.Storefront.Client`;
- V2, V2.WASM, Starter, Starter.WASM, generated storefront projects;
- Commerce Node, Control Plane, Application, Domain, Infrastructure, or `Web.SharedV2`;
- `HttpContext`;
- `IHttpContextAccessor`;
- `HttpClient`;
- direct `/api/*`;
- direct `api/storefront/*`;
- localhost/backend URLs;
- Presentation service injection.

WasmHost components must not use `@rendermode`. A host or composition root owns render-mode placement.

Required browser data path:

```text
WASM/browser component
  -> Browser controller
  -> same-origin Presentation/BFF endpoint
  -> Runtime
  -> Commerce Node Storefront API
```

Forbidden data path:

```text
WASM/browser component
  -> HttpClient
  -> Commerce Node Storefront API
```

## Visual Ownership

Reusable component libraries may expose:

- semantic hooks such as `data-storefront-*`;
- class slots supplied by host visual templates;
- descriptor values used by host code or tests.

Reusable component libraries must not own:

- theme CSS;
- Tailwind config;
- literal `class` attribute values in Razor markup;
- mixed literal/dynamic class values such as `class="@CssClass selected"`;
- V2 layout classes;
- store-specific copy;
- generated storefront output;
- final visual assets.

Allowed class slots must be fully dynamic, for example `class="@CssClass"`, `class="@Classes.Container"`, `class="@GetCssClass()"`, or `class="@(BuildCssClass())"`. Semantic `data-storefront-*` attributes are allowed because they are stable behavior hooks rather than visual ownership.

## Naming

Component names should not repeat the mode name. Use capability names that remain stable if the render strategy changes later.

Namespaces are grouped by current reusable component project and category:

```text
BlazorShop.Storefront.Components.Ssr.{Category}
BlazorShop.Storefront.Components.Primitives.{Category}
BlazorShop.Storefront.Components.WasmHost.{Category}
```

This namespace convention is current repository structure, not a claim that semantic Hybrid implementations require a dedicated physical project. `Components.Primitives` namespaces identify render-only primitives, not a new semantic mode.

## Current Reference Examples

Implemented reference examples:

- `StorefrontBrandLogo` in `Components.Ssr`;
- `StorefrontContactFormDescriptor` and `StorefrontContactFormApp` in `Components.WasmHost`, with descriptor mode `Hybrid`;
- `StorefrontHybridRuntimeProbe` in `Components.WasmHost` with semantic descriptor mode `Hybrid`;
- `StorefrontDiscountedProductRail` in `Components.WasmHost`;
- `StorefrontProductSummaryCard`, `StorefrontProductSummaryImage`, `StorefrontProductSummaryPurchaseActions`, and render-only `StorefrontProductGallery` in `Components.Primitives`;
- `StorefrontProductPricing`, `StorefrontProductAvailability`, and informational `StorefrontProductVariantList` in `Components.Ssr`;
- V2.WASM wrapper components for browser-visible contact and discounted rail adoption.

The contact reference proves Browser/BFF/WASM behavior through V2 page composition, V2.WASM wrapper ownership, and a reusable WasmHost app.

## H2 Runtime Proof Evidence

H2 completed browser-visible runtime evidence for the hidden/noindex Presentation route `/__qa/component-mvp`:

- raw server HTML contains SSR `StorefrontBrandLogo`;
- raw server HTML contains Hybrid prerender state for `StorefrontHybridRuntimeProbe`;
- browser hydration changes the Hybrid probe to `data-storefront-runtime-state="interactive"`;
- C# browser-side clicks change the probe value `0 -> 1 -> 2`;
- `StorefrontDiscountedProductRail` proves WasmHost loading, success, empty, error, and retry states through Browser controllers and same-origin BFF;
- network evidence records no direct Commerce browser calls, no `/_blazor` public server UI circuit, and no credential leaks.

H2 route ownership is:

```text
BlazorShop.Storefront.Presentation
  owns /__qa/component-mvp route shell and page context

BlazorShop.Storefront.V2
  owns visual lab markup, classes, labels, and copy

BlazorShop.Storefront.V2.WASM
  owns InteractiveWebAssembly wrapper/root placement

BlazorShop.Storefront.Components.WasmHost
  owns browser-executed reusable probe and rail implementations
```

H2 did not change Starter or generated storefronts. H3 keeps future reusable packages capability-based, guards render-mode placement repository-wide, and retires the historical compatibility project after the contact bridge migration.

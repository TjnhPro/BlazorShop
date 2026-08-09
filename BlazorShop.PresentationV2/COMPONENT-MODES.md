# Storefront Component Modes

This document defines the reusable Storefront component library modes. The mode projects are foundation-only in this phase. They make boundaries explicit, but they do not contain real storefront feature components yet.

## Projects

The mode projects live beside the active Storefront packages under `BlazorShop.PresentationV2`:

- `BlazorShop.Storefront.Components.Ssr`
- `BlazorShop.Storefront.Components.Hybrid`
- `BlazorShop.Storefront.Components.WasmHost`

Do not recreate `BlazorShop.Storefront.Components/Features`. The retired `Features` folder must remain absent unless a later architecture decision reopens it.

Do not add these projects for this foundation:

- `BlazorShop.Storefront.Components.Common`
- `BlazorShop.Storefront.Features.Contracts`
- `BlazorShop.Storefront.ComponentRuntime`
- `BlazorShop.Storefront.ComponentRegistry`

## Dependency Graph

Direct project references are fixed by mode:

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

The base `BlazorShop.Storefront.Components` project remains the lowest browser-safe contracts and headless layer. It must not reference `Presentation`, `Browser`, `Runtime`, `Client`, V2 hosts, Starter hosts, backend/core/API projects, Control Plane projects, or `Web.SharedV2`.

`BlazorShop.Storefront.Presentation`, `BlazorShop.Storefront.V2`, `BlazorShop.Storefront.Starter`, and generated storefronts must not reference the mode projects until a later phase implements and adopts real components.

## Package References

The base `BlazorShop.Storefront.Components` project may use minimal framework abstractions needed for component descriptors while staying on `Microsoft.NET.Sdk`.

Mode project package references are intentionally narrow:

- `Components.Ssr`: no Storefront transport, browser, runtime, client, V2, Starter, backend, or Control Plane packages.
- `Components.Hybrid`: no direct Browser package dependency beyond its project reference to `Components.WasmHost`; no Runtime, Client, V2, Starter, backend, or Control Plane packages.
- `Components.WasmHost`: browser-safe dependencies only; Browser controller access comes through `BlazorShop.Storefront.Browser`.

If a future component requires a new package, the owning phase must update this document and the mode boundary tests with a specific reason.

## SSR Mode

SSR components render completely on the server and do not require browser runtime for their primary function.

Allowed:

- prepared Presentation contexts;
- semantic render/input contracts;
- normal Razor forms and links;
- `RenderFragment`;
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

Hybrid components are server-owned shells that can prepare SSR structure, initial browser state, antiforgery/form contracts, and host a `WasmHost` child.

Allowed:

- `BlazorShop.Storefront.Components`;
- `BlazorShop.Storefront.Presentation`;
- `BlazorShop.Storefront.Components.WasmHost`;
- server-side initial state;
- semantic action descriptors;
- form/action descriptors supplied by Presentation;
- `@rendermode` only as a bridge when hosting a WasmHost child.

Forbidden:

- direct `BlazorShop.Storefront.Browser` reference;
- `BlazorShop.Storefront.Runtime`;
- `BlazorShop.Storefront.Client`;
- V2, V2.WASM, Starter, Starter.WASM, generated storefront projects;
- Commerce Node, Control Plane, Application, Domain, Infrastructure, or `Web.SharedV2`;
- direct `HttpClient`;
- direct backend/API routes;
- browser controller injection;
- direct `IJSRuntime` behavior;
- theme CSS, store-specific copy, or V2 layout ownership.

## WasmHost Mode

WasmHost components are browser-interactive feature roots that run in WASM and call Browser controllers. They do not own routes or self-host render modes.

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

WasmHost components must not use `@rendermode`. The host or Hybrid shell owns render-mode placement.

Required browser data path:

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

## Visual Ownership

Reusable component libraries may expose:

- semantic hooks such as `data-storefront-*`;
- class slots supplied by host visual templates;
- descriptor values used by host code or tests.

Reusable component libraries must not own:

- theme CSS;
- Tailwind config;
- V2 layout classes;
- store-specific copy;
- generated storefront output;
- final visual assets.

## Naming

Component names should not repeat the mode name. Use capability names that remain stable if the render strategy changes later.

Namespaces are grouped by mode and category:

```text
BlazorShop.Storefront.Components.Ssr.{Category}
BlazorShop.Storefront.Components.Hybrid.{Category}
BlazorShop.Storefront.Components.WasmHost.{Category}
```

## Future Examples

The first real component phase may use these examples to prove the modes:

- `StorefrontBrandLogo` in `Components.Ssr`;
- `StorefrontContactForm` in `Components.Hybrid`;
- `StorefrontContactFormApp` in `Components.WasmHost`;
- `StorefrontDiscountedProductRail` in `Components.WasmHost`.

Those components are not part of this foundation phase. Browser QA starts only when a later phase renders real browser-visible component behavior through a host.

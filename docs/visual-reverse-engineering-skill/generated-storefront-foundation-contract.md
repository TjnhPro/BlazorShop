# Generated Storefront Foundation Contract

This contract freezes the StorefrontBuilder foundation shape for Starter and generated storefronts. It records the V2 runtime markers that Starter must match without copying V2 visual implementation.

## Evidence Command

```powershell
rg -n "AddStorefrontBrowserControllers|AddStorefrontBrowserRuntime|MapStorefrontApplication|InteractiveWebAssembly" BlazorShop.PresentationV2
```

## Server Host Shape

The canonical generated server host is based on `BlazorShop.PresentationV2/BlazorShop.Storefront.Starter`, with V2 used only as a runtime parity reference.

Required server markers:

- `AddStorefrontApplication(builder.Configuration)` registers the Presentation-owned Storefront application surface.
- `AddStorefrontBrowserControllers()` registers browser-side same-origin controller endpoints.
- `AddStarterFoundationViews()` or the generated equivalent registers the project-local visual view set.
- `UseStorefrontApplication()` enables the Presentation-owned middleware and static application behavior.
- `MapStorefrontApplication(typeof(<FoundationViewRegistration>), typeof(<GeneratedWasmMarker>).Assembly)` maps Presentation routes plus the generated WASM assembly.

`BlazorShop.ServiceDefaults` remains V2-host specific for this phase. Generated storefronts do not require Aspire/default endpoint wiring.

## WASM Host Shape

The canonical generated WASM host is based on a neutral `BlazorShop.Storefront.Starter.WASM` template, not on `BlazorShop.Storefront.V2.WASM` visuals.

Required WASM markers:

- SDK `Microsoft.NET.Sdk.BlazorWebAssembly`.
- Target framework `net10.0`.
- `WebAssemblyHostBuilder.CreateDefault(args)`.
- `builder.Services.AddStorefrontBrowserRuntime(builder.HostEnvironment)`.
- Browser-safe Components contracts/headless behavior only.
- No `@page` route declarations in generated visual files.
- No direct Commerce Node, Control Plane, Runtime, or Client usage.

The stable generated assembly marker is the account host component:

```text
BlazorShop.Storefront.{Name}.WASM.Components.Account.StorefrontAccountApp
```

## Package Ownership

Generated server projects consume shared foundation through packages:

- `BlazorShop.Storefront.Presentation`
- `BlazorShop.Storefront.Components`
- `BlazorShop.Storefront.Browser`

Generated WASM projects consume:

- `BlazorShop.Storefront.Components`
- `BlazorShop.Storefront.Browser`

`BlazorShop.Storefront.Runtime` and `BlazorShop.Storefront.Client` are not direct generated source dependencies. Runtime is composed by Presentation, and Client is a Runtime transport dependency. Their versions remain metadata/provenance proof inputs.

All Storefront package versions for a proof run must share one immutable build identity derived from the source `HEAD`, unless explicitly overridden for emergency manual proof. Restore must use a local generated package feed and `--no-cache --force-evaluate`.

## Generated Visual Ownership

Generated server visual ownership:

- `Components/Layout/**`
- `Components/Catalog/**`
- server visual wrappers under `Components/Commerce/**`
- `Components/States/**`
- `Pages/Ssr/**`
- visual wrappers under `Pages/Hybrid/**`
- `wwwroot/css/**`
- `wwwroot/assets/generated/**`

Generated WASM visual ownership:

- `BlazorShop.Storefront.{Name}.WASM/Components/Account/**`
- `BlazorShop.Storefront.{Name}.WASM/Components/Cart/**`
- `BlazorShop.Storefront.{Name}.WASM/Components/Checkout/**`
- visual-only assets under `BlazorShop.Storefront.{Name}.WASM/wwwroot/**`

Generated visual code may change markup, CSS, layout, copy, assets, and project-local visual components. It must preserve Presentation-owned descriptors, same-origin browser actions, SEO route behavior, auth behavior, cart/checkout semantics, and BFF/API transport.

## Forbidden References

Generated output must not reference:

- `BlazorShop.Storefront.V2`
- `BlazorShop.Storefront.V2.WASM`
- `BlazorShop.Storefront.Starter`
- `BlazorShop.Storefront.Starter.WASM`
- `BlazorShop.Storefront.Runtime` as a direct package or project reference
- `BlazorShop.Storefront.Client` as a direct package or project reference
- `BlazorShop.Application`
- `BlazorShop.Domain`
- `BlazorShop.Infrastructure`
- `BlazorShop.CommerceNode.API`
- `BlazorShop.ControlPlane.API`
- `BlazorShop.ControlPlane.Web`
- `BlazorShop.Web.SharedV2` or `Web.SharedV2`

The generated server may have exactly one `ProjectReference`: its generated sibling WASM project under the same generated root. Generated WASM projects must not have `ProjectReference` entries.

## V2-Specific Decisions

V2 visual files are not copied into Starter or generated projects. V2 is a parity reference for runtime wiring only:

- Browser controller registration.
- WASM assembly mapping.
- Browser runtime registration.
- Interactive WebAssembly render mode integration owned by Presentation.

Generated storefronts differ from V2 by markup, CSS, layout, assets, view registrations, and store configuration, not by browser runtime foundation.

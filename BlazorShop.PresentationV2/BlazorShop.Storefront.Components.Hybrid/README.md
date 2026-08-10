# BlazorShop.Storefront.Components.Hybrid

Transitional Storefront Hybrid compatibility project from the earlier foundation/reference component work.

This project currently exists for the historical Hybrid contact shell and descriptor. It is not the canonical physical home for every future `StorefrontComponentMode.Hybrid` component. H2 runtime evidence narrowed this project to compatibility only; H3 should migrate or retire the historical contact bridge if that can be done without visible V2 behavior loss.

Allowed direct project references:

- `BlazorShop.Storefront.Components`
- `BlazorShop.Storefront.Presentation`
- `BlazorShop.Storefront.Components.WasmHost`

Current guardrails remain in force:

- no direct Browser reference;
- no Browser controller injection;
- no direct API calls;
- no theme CSS;
- no V2 layout or copy ownership;
- no production dummy components;
- no new reusable components during the H2/H3 transition without a new architecture decision.

Current source classification:

- `_Imports.razor`: compatibility imports for the historical contact shell.
- `Content/StorefrontContactForm.razor`: historical render-mode bridge that hosts the WasmHost contact app.
- `Content/StorefrontContactFormDescriptor.cs`: historical compatibility public descriptor for `contact-form`.

Do not treat nested WasmHost child bridging as the desired default for future Hybrid work. Hybrid means server-produced or prerendered HTML/page snapshots plus client-side WebAssembly interactivity after hydration. H2 proved the canonical runtime pattern through `StorefrontHybridRuntimeProbe` in `BlazorShop.Storefront.Components.WasmHost`, rendered by a V2.WASM wrapper on the Presentation-owned `/__qa/component-mvp` route.

The visible V2 contact route currently uses a V2.WASM wrapper path. Do not move it back to this historical bridge without a later phase that includes browser runtime evidence.

H2 evidence to preserve for H3:

- raw HTML contains Hybrid prerender state;
- browser hydration changes runtime state to interactive;
- C# click handling changes local state without a server UI circuit;
- network audit records no `/_blazor` public circuit and no direct Commerce Node browser call.

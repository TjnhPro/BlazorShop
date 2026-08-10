# BlazorShop.Storefront.Components.Hybrid

Transitional Storefront Hybrid compatibility project from the earlier foundation/reference component work.

This project currently exists for the historical Hybrid contact shell and descriptor. It is not the canonical physical home for every future `StorefrontComponentMode.Hybrid` component. H2 must prove the permanent pattern with runtime evidence before this project is expanded, narrowed, renamed, or retired.

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
- no new reusable components during H1.

Current source classification:

- `_Imports.razor`: compatibility imports for the historical contact shell.
- `Content/StorefrontContactForm.razor`: historical render-mode bridge that hosts the WasmHost contact app.
- `Content/StorefrontContactFormDescriptor.cs`: historical compatibility public descriptor for `contact-form`.

Do not treat nested WasmHost child bridging as the desired default for future Hybrid work. Hybrid means server-produced or prerendered HTML/page snapshots plus client-side WebAssembly interactivity after hydration; H2 must prove the permanent physical pattern.

The visible V2 contact route currently uses a V2.WASM wrapper path. Do not move it back to this historical bridge without a later phase that includes browser runtime evidence.

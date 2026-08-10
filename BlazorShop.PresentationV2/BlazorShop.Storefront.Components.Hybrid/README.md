# BlazorShop.Storefront.Components.Hybrid

Reusable Storefront Hybrid component mode library from the earlier foundation/reference component work.

This project currently exists for historical Hybrid shell components, including the reference `StorefrontContactForm` descriptor/component. Its future role is pending H1 re-evaluation after the Hybrid architecture clarification in `BlazorShop.PresentationV2/COMPONENT-MODES.md`.

Allowed direct project references:

- `BlazorShop.Storefront.Components`
- `BlazorShop.Storefront.Presentation`
- `BlazorShop.Storefront.Components.WasmHost`

Current guardrails remain in force until H1 changes tests/code:

- no direct Browser reference;
- no Browser controller injection;
- no direct API calls;
- no theme CSS;
- no V2 layout or copy ownership;
- no production dummy components.

Do not treat nested WasmHost child bridging as the desired default for future Hybrid work. Hybrid means server-produced or prerendered HTML/page snapshots plus client-side WebAssembly interactivity after hydration; the physical project pattern must be reviewed in H1.

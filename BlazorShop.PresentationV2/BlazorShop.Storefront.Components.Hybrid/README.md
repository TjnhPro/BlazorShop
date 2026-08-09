# BlazorShop.Storefront.Components.Hybrid

Reusable Storefront Hybrid component shell mode library.

Allowed direct project references:

- `BlazorShop.Storefront.Components`
- `BlazorShop.Storefront.Presentation`
- `BlazorShop.Storefront.Components.WasmHost`

This project is foundation-only until a later phase adds real shared components. It may bridge server-prepared state to a WasmHost child, but it must not reference Browser directly, inject browser controllers, call APIs directly, own theme CSS, own V2 layout, or add production dummy components.

# BlazorShop.Storefront.Components.WasmHost

Reusable Storefront browser-interactive WASM host component mode library.

Allowed direct project references:

- `BlazorShop.Storefront.Components`
- `BlazorShop.Storefront.Browser`

This project is foundation-only until a later phase adds real shared components. Browser data must go through Browser controllers and same-origin Presentation/BFF endpoints. Do not reference Presentation, Runtime, Client, V2, Starter, backend/core/API projects, use direct `HttpClient`, direct `/api/*`, render-mode ownership, theme CSS, V2 layout classes, store-specific copy, generated output, or production dummy components here.

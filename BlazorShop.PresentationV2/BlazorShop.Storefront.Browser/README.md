# Storefront Browser Runtime

`BlazorShop.Storefront.Browser` owns browser-safe runtime services for interactive storefront features that call same-origin Storefront Presentation BFF endpoints.

- Local API calls must use relative same-origin routes.
- Protected mutations read antiforgery metadata from the storefront host document.
- Runtime services may depend on `BlazorShop.Storefront.Components` contracts and browser/WASM framework packages only.

Do not add Storefront Presentation, Runtime, Client, V2, backend, Control Plane, Commerce Node, CSS, layout, or theme ownership here.

# Storefront Page Render Ownership

`Pages` is grouped by render ownership instead of ecommerce domain area.

- `Ssr` pages are usable from server-rendered HTML. They own data loading, HTTP status, crawler behavior, and SEO metadata for pages that should not require WASM.
- `Hybrid` pages own route parameters, SEO, status handling, and initial snapshots while composing interactive feature components for browser behavior.
- `WasmHost` pages own direct links, noindex metadata, auth redirects, antiforgery/bootstrap data, and hand off the feature UI to WebAssembly.

New pages should choose the folder by the render boundary they need. Keep route URLs stable when moving files between these folders.

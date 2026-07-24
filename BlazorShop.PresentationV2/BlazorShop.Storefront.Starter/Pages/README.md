# Storefront Starter Pages

Route ownership is visible from folders:

- `Ssr`: server-rendered pages such as home, content, auth, maintenance, and not-found states.
- `Hybrid`: SEO/snapshot pages that can host browser interactivity, such as product, category, search, cart, checkout, payment result, and deals.
- `WasmHost`: server-owned route/security boundaries for browser-owned account features.

Hydration modes:

- `InitialSnapshot`: SSR data is supplied and browser code must not duplicate the first fetch.
- `BrowserFetch`: the browser component owns the first data read after hydration.
- `RefreshAfterHydration`: SSR data can render first, then browser code may refresh after hydration.

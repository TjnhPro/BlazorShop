# BlazorShop Storefront Components Primitives

This project contains browser-safe render-only Razor primitives for storefront hosts.

Boundary:

- Depends only on `BlazorShop.Storefront.Components`.
- Contains semantic DOM hooks and accessibility markup.
- Does not reference Presentation, Browser, Runtime, Client, backend, core, API, or host projects.
- Does not own final CSS, final copy, brand styling, layout decisions, or host design tokens.
- Is not a storefront component mode and must not add a `StorefrontComponentMode` value.

Host projects provide final labels, CSS classes, placement, and runtime behavior.

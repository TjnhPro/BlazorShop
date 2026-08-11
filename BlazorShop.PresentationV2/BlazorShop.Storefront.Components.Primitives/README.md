# BlazorShop Storefront Components Primitives

This project contains browser-safe render-only Razor primitives for storefront hosts, including product purchase semantics and the storefront toast region/template.

Boundary:

- Depends only on `BlazorShop.Storefront.Components`.
- Contains semantic DOM hooks and accessibility markup, plus neutral class-slot and label contracts supplied by a host.
- Does not reference Presentation, Browser, Runtime, Client, backend, core, API, or host projects.
- Does not own final CSS, final copy, brand styling, layout decisions, or host design tokens.
- Is not a storefront component mode and must not add a `StorefrontComponentMode` value.

Host projects provide final labels, CSS classes, placement, and runtime behavior. Storefront V2 provides the final purchase/toast visual values and toast behavior; this project never embeds those values.

# BlazorShop.Storefront.Components

`BlazorShop.Storefront.Components` is the browser-safe shared package for Storefront presentation contracts, headless interaction state, and browser primitives.

## Ownership

- `Contracts/{Capability}` owns stable render/input models, labels, route descriptors, and other browser-safe contracts used by Storefront hosts.
- `Headless/{Capability}` owns reusable behavior state, action descriptors, selectors, and interaction helpers that do not know a host route or design system.
- `Browser` owns same-origin browser/BFF client primitives, antiforgery token reading, structured local API result/error handling, and browser interop support.

## Consumer Rules

- Storefront V2, Starter, and generated/custom storefronts may consume `Contracts`, `Headless`, and `Browser` when they need shared browser-safe behavior.
- Storefront V2, Starter, and generated/custom storefronts own their visual templates, markup, CSS, store-specific assets, pages, and analysis artifacts.
- Browser interop modules are hosted by the concrete storefront project that owns the interactive component using them.
- Browser code must use same-origin BFF endpoints for protected actions before Runtime or Commerce Node Storefront APIs.

## Do Not Add

- Storefront V2 route helpers, theme/layout implementations, store-specific CSS, or generated visual output.
- Razor components, shared visual wrappers, visual class bags, static web assets, or final storefront copy.
- Commerce Node, Control Plane, Application, Domain, Infrastructure, EF, API client, credential, or `Web.SharedV2` dependencies.
- Public HTTP API DTOs, admin request models, store ownership fields, credentials, or server-owned fields.

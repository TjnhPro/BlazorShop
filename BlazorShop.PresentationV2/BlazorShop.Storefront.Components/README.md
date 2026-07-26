# BlazorShop.Storefront.Components

`BlazorShop.Storefront.Components` is the browser-safe shared package for Storefront presentation contracts, headless interaction state, and temporary compatibility component primitives.

## Ownership

- `Contracts/{Capability}` owns stable render/input models, labels, route descriptors, and other browser-safe contracts used by Storefront hosts.
- `Headless/{Capability}` owns reusable behavior state, action descriptors, selectors, and interaction helpers that do not know a host route or design system.
- `Browser` owns same-origin browser/BFF client primitives, antiforgery token reading, structured local API result/error handling, and browser interop support.
- `Features/{Capability}` owns CSS-neutral compatibility Razor wrappers only while current Storefront V2/shared wrappers are being migrated.

## Consumer Rules

- Storefront V2, Starter, and generated/custom storefronts may consume `Contracts`, `Headless`, and `Browser` when they need shared browser-safe behavior.
- Starter and generated/custom storefronts own their visual templates, generated markup, generated CSS, store-specific assets, pages, and analysis artifacts.
- Generated storefronts should not copy `Features` wrappers as their visual baseline; those wrappers are compatibility primitives, not stable presentation contracts.
- Browser code must use same-origin BFF endpoints for protected actions before Runtime or Commerce Node Storefront APIs.

## Do Not Add

- Storefront V2 route helpers, theme/layout implementations, store-specific CSS, or generated visual output.
- Commerce Node, Control Plane, Application, Domain, Infrastructure, EF, API client, credential, or `Web.SharedV2` dependencies.
- Public HTTP API DTOs, admin request models, store ownership fields, credentials, or server-owned fields.

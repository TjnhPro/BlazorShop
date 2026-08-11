# BlazorShop.Storefront.Components.Ssr

Reusable Storefront SSR component mode library for server-rendered semantic components over prepared Presentation contexts.

Allowed direct project references:

- `BlazorShop.Storefront.Components`
- `BlazorShop.Storefront.Presentation`

`StorefrontConsentPanel` is the consent semantic renderer. It receives `StorefrontConsentContext` and host-supplied labels/classes; Storefront Presentation JavaScript owns current/save/revoke calls and native hidden-state behavior, while the V2 wrapper owns registration, final copy/classes, and placement.

Do not add browser controllers, direct API calls, render-mode ownership, theme CSS, V2 layout classes, store-specific copy, generated output, or production dummy components here.

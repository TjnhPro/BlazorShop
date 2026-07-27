# ADR: Storefront Presentation Foundation

Date: 2026-07-27
Status: Accepted

## Context

Storefront V2 had become the canonical storefront and gradually accumulated App/Routes, SSR route shells, local browser/BFF endpoints, SEO/discovery, media endpoint composition, and page services. `BlazorShop.Storefront.Starter` and generated StorefrontBuilder projects needed the same application behavior without copying Storefront V2 source or generating route/BFF/SEO logic from scratch.

## Decision

- Introduce `BlazorShop.Storefront.Presentation` as the shared storefront application engine.
- Presentation owns App/Routes, route shells, page services, same-origin BFF/local endpoints, SEO/discovery, media composition, and view-slot contracts.
- Storefront V2, Starter, and generated storefronts consume Presentation and provide host configuration, view registrations, visual templates, assets, copy, and store-specific output.
- Hosts call `UseStorefrontPresentation()` and `MapStorefrontPresentation()` instead of mapping individual route/BFF/SEO endpoint groups.
- Generated StorefrontBuilder projects consume `BlazorShop.Storefront.Presentation`, `BlazorShop.Storefront.Runtime`, and `BlazorShop.Storefront.Components` through package boundaries when they need the full storefront application surface. Client remains Runtime's generated transport dependency and is pinned in generated package metadata.

## Boundaries

Presentation must not reference Storefront V2, Starter, generated storefronts, Control Plane, Commerce Node API, Application, Domain, Infrastructure, or `Web.SharedV2`.

Presentation must not own ecommerce truth. Pricing, sellability, inventory, cart validity, checkout rules, order creation, and payment provider behavior remain Commerce Node Storefront API responsibilities.

Generated storefronts must not recreate Presentation-owned App/Routes/page services/BFF/SEO/media logic. They should replace visual templates, CSS, assets, copy, and view registrations while reusing the shared application engine.

## Consequences

- Fixes to shared route, BFF, SEO, media, or page-service behavior benefit Storefront V2, Starter, and generated storefronts.
- Storefront V2 becomes the active production host/visual implementation, not the owner of shared application composition.
- Starter becomes the neutral second consumer rather than a parallel route/BFF/SEO implementation.
- StorefrontBuilder isolation gates must pack Client, Runtime, Presentation, and Components; generated projects directly reference Runtime, Presentation, and Components while Client is verified as Runtime's generated transport dependency.

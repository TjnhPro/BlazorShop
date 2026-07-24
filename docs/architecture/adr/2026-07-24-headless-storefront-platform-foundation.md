# ADR: Headless Storefront Platform Foundation

Date: 2026-07-24
Status: Accepted

## Context

BlazorShop V2 already has a scoped Storefront API under `api/storefront/stores/{storeKey}/*` and a real Storefront V2 implementation that consumes that API. The next platform step is to make Commerce Node the framework-neutral Storefront API platform while keeping Storefront V2 a product storefront, not a starter template.

## Decision

- `BlazorShop.CommerceNode.API` is the headless ecommerce backend and Storefront API platform. It owns public Storefront HTTP contracts, ecommerce business truth, store-scoped route resolution, checkout/order/payment rules, and provider callback/webhook routes.
- `BlazorShop.Storefront.V2` is the first real storefront consumer. It owns its design, page composition, SSR/BFF behavior, SEO composition, and same-origin browser endpoints.
- `BlazorShop.Storefront.Client` is the future generated Storefront API client package. It will contain generated transport/contracts from the Commerce Node Storefront OpenAPI document and must not reference backend/core/API projects.
- `BlazorShop.Storefront.Runtime` is optional and may be introduced only after decoupling proves shared runtime primitives are needed. It must stay neutral and backend-independent.
- `BlazorShop.Storefront.Starter` is a future neutral skeleton and is not part of this foundation.
- `BlazorShop.Storefront.{Name}` represents future independent generated storefronts. They must consume the Storefront API/client boundary instead of copying Storefront V2 internals.

## Dependency Rules

Final forbidden dependencies:

- `Storefront.V2` must not reference `Domain`, `Application`, `Infrastructure`, `CommerceNode.API`, or `ControlPlane.API`.
- `Storefront.Client` must not reference `Domain`, `Application`, `Infrastructure`, `CommerceNode.API`, `ControlPlane.API`, or `Storefront.V2`.
- `Storefront.Runtime`, if present, must not reference `Domain`, `Application`, `Infrastructure`, `CommerceNode.API`, `ControlPlane.API`, or `Storefront.V2`.
- `Storefront.Components` and `Storefront.WASM` must remain backend-independent.

`Storefront.V2` may keep transitional `Application` and `Web.SharedV2` references only until the capability-by-capability decoupling phase removes backend-owned DTO usage.

## Storefront V2 Is Not Starter Source

Storefront V2 must not be copied into `Storefront.Starter`. Starter work, when approved later, must start from the Storefront OpenAPI/client/runtime boundary and use Storefront V2 only as behavior reference.

## Consequences

- Commerce Node Storefront OpenAPI is the canonical machine-readable frontend contract.
- Generated clients are frontend-readable contracts.
- Storefront V2 can keep presentation-specific view models, but must not add handwritten duplicate API DTO clones where OpenAPI-generated types should be used.
- Browser/WASM flows continue to call same-origin `/api/*` BFF endpoints and never hold Commerce Node URLs, node credentials, or access tokens in browser storage.

# ADR: Storefront Starter Foundation

Date: 2026-07-24
Status: Accepted; implemented for Starter/Sample and extended by StorefrontBuilder

## Context

The Headless Storefront Platform Foundation completed the Commerce Node Storefront API, generated Storefront client package, and Storefront V2 decoupling needed to start a separate Starter. The next step is a neutral Blazor storefront skeleton that proves new storefronts can consume package-based Storefront contracts without copying Storefront V2 internals.

## Decision

- `BlazorShop.Storefront.Starter` is the neutral skeleton source for deterministic generated storefronts.
- `BlazorShop.Storefront.Sample` is the first deterministic generated project used to prove the Starter.
- `BlazorShop.Storefront.V2` remains the real storefront implementation and behavior reference. It must not be copied into Starter or generated storefront output.
- `BlazorShop.Storefront.Client` remains the generated OpenAPI transport and contract package.
- `BlazorShop.Storefront.Runtime` may be introduced only for neutral duplicated runtime primitives needed by both Storefront V2 and Starter.
- `BlazorShop.Storefront.Components/Features/*` remains presentation-only reusable Blazor component code and is not a backend contract or business-rule package.

## Ownership

| Owner | Owns | Does not own |
| --- | --- | --- |
| Backend | pricing, sellability, cart validation, checkout, orders, authorization, provider rules, public config/capability projection | frontend layout/composition |
| Storefront.Client | generated contracts and HTTP transport | UI state, BFF logic, business rules |
| Storefront.Runtime | neutral store/client/error/auth/BFF primitives when proven | V2 design, CSS, route composition, backend rules |
| Storefront.Starter | neutral skeleton, examples, conventions, generation manifest | V2-specific design/source, backend source |
| Storefront.Sample | generated project proof | platform contract ownership |
| Storefront.V2 | real storefront design, BFF, deployment, behavior reference | Starter source template |

## Protected Areas For Future Generation

Future deterministic scaffolding and AI-assisted storefront generation must treat these areas as protected unless a human explicitly reopens the contract:

- generated client source under `BlazorShop.Storefront.Client/Generated`;
- runtime security primitives, including session, return URL, antiforgery, and BFF error mapping code;
- BFF transport/security code that owns tokens, cart cookies, same-origin command routes, and Commerce Node calls;
- package/version manifests such as NuGet package pins and compatibility tables;
- generated storefront manifests that record feature placement, store key, package versions, and generation inputs.

## Dependency Rules

Starter and generated storefront projects must not reference `BlazorShop.Storefront.V2`, `BlazorShop.Domain`, `BlazorShop.Application`, `BlazorShop.Infrastructure`, `BlazorShop.CommerceNode.API`, `BlazorShop.ControlPlane.API`, or `BlazorShop.ControlPlane.Web`.

Starter must consume `BlazorShop.Storefront.Client` through `PackageReference` in the independent proof. A monorepo development project may exist, but the release gate must prove that an external project restores and builds from local/private packages.

Starter source must not import `BlazorShop.Web.SharedV2.Models` business contracts and must not copy the manual `StorefrontApiClient` transport from Storefront V2. Generated client contracts are the default contract source. Manual HTTP exceptions require an exception registry entry with reason, owner, test, and revisit trigger.

## Consequences

- Storefront V2 remains stable and can be used as behavior reference only.
- Starter teaches SSR, BFF, browser, capability, and feature-placement conventions without owning ecommerce business truth.
- Sample generation can later prove the Starter deterministically before any AI generator work begins.

## Amendment

Later phases on 2026-07-24 introduced StorefrontBuilder tooling and the committed `BlazorShop.Storefront.BuilderDemo` proof. `BlazorShop.Storefront.Runtime`, `BlazorShop.Storefront.Starter`, and `BlazorShop.Storefront.Sample` are now active. The current source of truth for generated storefront preparation is [StorefrontBuilder Architecture](../11-storefront-builder.md).

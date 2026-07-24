# StorefrontBuilder Architecture Note

Date: 2026-07-24
Status: Accepted for MVP planning

## Role

StorefrontBuilder is development-time tooling for visual reverse engineering and generated-storefront preparation. It is not a production ASP.NET service, not a Commerce Node extension, and not a runtime module system.

The tool analyzes reference ecommerce sites, records reviewable evidence, plans generation, and writes generated storefront projects only under the approved generated target pattern:

```text
BlazorShop.PresentationV2/BlazorShop.Storefront.{Name}
```

`{Name}` must be normalized before use as a folder, project name, namespace segment, and file prefix. Unsafe names are rejected before files are created.

## Source Of Truth Order

StorefrontBuilder decisions follow this order:

1. Storefront OpenAPI and `BlazorShop.Storefront.Client` generated contracts.
2. Starter generation/runtime contract.
3. Backend capability state.
4. Starter feature manifest.
5. Visual evidence from the target site.
6. AI inference, recorded explicitly when evidence is incomplete.

## Starter Boundary

`BlazorShop.Storefront.Starter` is a read-only template input for StorefrontBuilder. The generated `BlazorShop.Storefront.{Name}` project is the editable and tunable store project.

StorefrontBuilder must not write store-specific presentation, assets, CSS, generated analysis artifacts, or AI-tuned components back into Starter. Starter remains neutral and reusable.

## Out Of Scope

The MVP does not change API contracts, Runtime security primitives, BFF security behavior, cart/checkout/order/payment/pricing/sellability business logic, optional module installation, marketplace UI, or production deployment behavior.

## Consequence

Generated storefronts consume `BlazorShop.Storefront.Client` and `BlazorShop.Storefront.Runtime` through package boundaries, keep browser commands behind same-origin BFF endpoints, and treat Commerce Node as the owner of ecommerce business truth.

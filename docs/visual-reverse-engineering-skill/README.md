# Visual Reverse Engineering Skill Docs

This folder documents the StorefrontBuilder workflow for turning reference ecommerce storefront evidence into reviewable, generated Blazor storefront projects.

## Read First

1. [StorefrontBuilder Architecture](../architecture/11-storefront-builder.md) - ownership, boundaries, artifact rules, and validation gates.
2. [Reference](reference.md) - commands, modes, generated artifacts, and gate expectations.
3. [How To Generate And Validate](how-to-generate-and-validate.md) - operator workflow for an existing or new generated storefront.
4. [Tutorial: Generated Proof](tutorial-generated-proof.md) - concrete walkthrough using the on-demand generated proof artifact.
5. [Explanation: Boundaries And Regeneration](explanation-boundaries-and-regeneration.md) - why generated storefronts stay isolated from Storefront V2 and backend projects.

## Historical Plans

The phase plans are retained as implementation history and checklist evidence:

- [01-StorefrontBuilder-Foundation.todo.md](01-StorefrontBuilder-Foundation.todo.md)
- [02-StorefrontBuilder-Visual-Generation.todo.md](02-StorefrontBuilder-Visual-Generation.todo.md)
- [03-StorefrontBuilder-QA-Regeneration.todo.md](03-StorefrontBuilder-QA-Regeneration.todo.md)
- [StorefrontBuilder Architecture Note](StorefrontBuilder-architecture-note.md)

The architecture docs are the current source of truth when a historical plan conflicts with current code.

## Runtime Boundary Reminder

Generated storefront server/BFF projects consume `BlazorShop.Storefront.Presentation` for shared App/Routes/page services/BFF/SEO/media composition, then register generated visual components as Presentation view slots and register `BlazorShop.Storefront.Runtime` through `AddStorefrontPlatformRuntime` or explicit `AddStorefront{Capability}Runtime` methods. Runtime owns the direct generated-client transport dependency, while generated projects reference Runtime/Presentation/Components packages and keep Client version metadata for compatibility. Generated visual files must not declare `@page` or add route assemblies. Retired compatibility aliases are not part of the current Runtime API surface. Browser and WASM code must use same-origin generated endpoints and browser-safe `BlazorShop.Storefront.Components` contracts/headless behavior and Browser local API primitives, not Runtime or guessed Storefront API envelopes.

`BlazorShop.Storefront.Components.Features` is retired. StorefrontBuilder output should generate project-local visual templates from evidence while consuming shared `Contracts`, `Headless`, and `Browser` primitives.

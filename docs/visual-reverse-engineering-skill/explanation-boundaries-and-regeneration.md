# Explanation: Boundaries And Regeneration

StorefrontBuilder exists to create store-specific storefronts without turning the active Storefront V2 runtime into a template or leaking backend contracts into generated UI projects.

## Why Starter Is Neutral

`BlazorShop.Storefront.Starter` is the clean template input. It demonstrates the shape of a generated storefront, package consumption, same-origin BFF boundaries, and expected loading/error/empty states.

It must stay neutral because every generated storefront needs a stable base. If store-specific CSS, assets, AI-tuned sections, or reference-site artifacts are written back to Starter, the next storefront inherits accidental design and behavior from the previous one.

## Why Generated Storefronts Use Packages

Generated projects consume `BlazorShop.Storefront.Client` and `BlazorShop.Storefront.Runtime` as packages so they can prove independence from the monorepo implementation details.

This keeps generated storefronts from depending on:

- Storefront V2 source layout.
- Domain/Application/Infrastructure internals.
- Commerce Node API implementation classes.
- Control Plane runtime behavior.

The isolation gate enforces this by packing Client/Runtime and scanning the generated storefront for forbidden project references and backend/core/API names.

## Why Browser Commands Stay Same-Origin

Protected browser and WASM flows must call same-origin generated storefront endpoints first. The server-side storefront then uses the generated Storefront client to call Commerce Node Storefront APIs with the correct store key.

That preserves the V2 rule that public storefront browser code does not hold node credentials and does not call Commerce Admin or Control Plane routes.

## Why Artifacts Are Committed

Generated storefronts keep review artifacts under `docs/storefront-analysis/` because visual reverse engineering is evidence-driven. The artifacts let reviewers see:

- What source metadata was used.
- Which assets were selected or generated.
- Which files are generated-owned.
- Which QA checks ran.
- Where inference was used because evidence was incomplete.

Without these files, regeneration becomes hard to review and manual changes are harder to distinguish from generated output.

## How Regeneration Should Be Scoped

Use the smallest regeneration scope that matches the change:

- Use `css` for token/foundation style changes.
- Use `page` for one page-level composition.
- Use `component` for one reusable generated component.
- Use `all` when the visual system, composition, or manifest state changed broadly.
- Use `validate` or `conflicts` when checking state without applying output.

Manual edits to generated files should either be reflected in generation inputs or documented in generated-file ownership metadata so later regeneration does not silently erase intentional work.

# Explanation: Boundaries And Regeneration

StorefrontBuilder exists to create store-specific storefronts without turning the active Storefront V2 runtime into a template or leaking backend contracts into generated UI projects.

## Why Starter Is Neutral

`BlazorShop.Storefront.Starter` is the clean template input. It demonstrates the shape of a generated storefront, package consumption, Presentation-owned same-origin BFF boundaries, and expected loading/error/empty states.

It must stay neutral because every generated storefront needs a stable base. If store-specific CSS, assets, AI-tuned sections, or reference-site artifacts are written back to Starter, the next storefront inherits accidental design and behavior from the previous one.

## Why Generated Storefronts Use Packages

Generated projects consume `BlazorShop.Storefront.Presentation` and `BlazorShop.Storefront.Components` as direct packages so they can prove independence from the monorepo implementation details while reusing the shared storefront application engine. Presentation composes Runtime internally, and Runtime owns direct `BlazorShop.Storefront.Client` transport usage. Generated projects keep Runtime/Client package metadata current for package proof compatibility, but generated visual source does not compile directly against Runtime or Client types.

This keeps generated storefronts from depending on:

- Storefront V2 source layout.
- Domain/Application/Infrastructure internals.
- Commerce Node API implementation classes.
- Control Plane runtime behavior.

The isolation gate enforces this by packing Client/Runtime/Presentation/Components, requiring direct package references to Presentation/Components, keeping Client/Runtime package metadata current, and scanning the generated storefront for forbidden project references and backend/core/API names. Generated storefronts do not recreate shared App/Routes/page services/BFF/SEO/media logic from scratch.

## Why Browser Commands Stay Same-Origin

Protected browser and WASM flows must call same-origin generated storefront endpoints first. The server-side storefront then uses the generated Storefront client to call Commerce Node Storefront APIs with the correct store key.

That preserves the V2 rule that public storefront browser code does not hold node credentials and does not call Commerce Admin or Control Plane routes.

## Why Artifacts Exist

Generated storefronts keep review artifacts under `docs/storefront-analysis/` because visual reverse engineering is evidence-driven. The artifacts let reviewers see:

- What source metadata was used.
- Which assets were selected or generated.
- Which files are generated-owned.
- Which QA checks ran.
- Where inference was used because evidence was incomplete.

Without these files, regeneration becomes hard to review and manual changes are harder to distinguish from generated output. Canonical generated proof output under `artifacts/` and `obj/` is ignored and disposable; commit generated artifacts only when a phase explicitly promotes a specific generated storefront or report into tracked evidence.

## Why Phase 3C Handoff Is Separate

`BlazorShop.AI.StorefrontReverseEngineering` writes Phase 3C artifacts under `analysis/agent-handoff/` so future generation can start from reviewed, constrained evidence instead of rereading raw screenshots or DOM snapshots. The handoff package names allowed files, protected files, page compositions, Storefront pattern contracts, unresolved regions, and final readiness.

Phase 3D hardens that handoff with typed reviewed artifacts, exact page slot validation, packaged screenshots/crops, schema validation, positive and negative fixtures, and a no-skip clean-head closure gate. That handoff is still evidence, not active generation input. StorefrontBuilder does not consume `analysis/agent-handoff/*` until a separate approved Phase 4 plan changes the generation boundary. Phase 4 may read only `analysis/agent-handoff/*` and schemas after closure passes, must fail unless final handoff readiness passed, must not reinterpret raw evidence unless it runs a new ReverseEngineering pass, must not write into Starter, and must not modify StorefrontBuilder generation without that approved plan.

## How Regeneration Should Be Scoped

Use the smallest regeneration scope that matches the change:

- Use `css` for token/foundation style changes.
- Use `page` for one page-level composition.
- Use `component` for one reusable generated component.
- Use `all` when the visual system, composition, or manifest state changed broadly.
- Use `validate` or `conflicts` when checking state without applying output.

Manual edits to generated files should either be reflected in generation inputs or documented in generated-file ownership metadata so later regeneration does not silently erase intentional work.

Regeneration uses a fresh candidate generated from current Starter/template inputs. `-WhatIf` runs that same candidate generation and planning pipeline, writes a stable report outside the generated target, prints summary/action output to the console, and exits before target writes. Apply mode then copies only planned safe generated/managed changes into the target, preserves user-owned/protected/manual-edited files, reports obsolete candidates, and rolls back if requested validation/build fails.

Use `-Scope foundation` for explicit platform metadata updates such as package compatibility metadata and the copied Starter contract. Normal visual scopes do not silently update protected foundation files.

StorefrontBuilder generator version is a tooling provenance value, not a package version. Both PowerShell generation metadata and Node generated-file manifests read it from `tools/BlazorShop.AI.StorefrontBuilder/version.json`.

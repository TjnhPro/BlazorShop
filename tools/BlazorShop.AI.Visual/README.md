# BlazorShop AI Visual

`BlazorShop.AI.Visual` is a development-time visual skill, reference, schema, and report workspace for StorefrontBuilder Phase 4 MVP closure.

It does not generate storefront projects directly. StorefrontBuilder remains the only owner of project generation, regeneration, generated-file manifests, visual write recording, visual QA scripts, and repair helpers.

The visual workspace consumes generated StorefrontBuilder artifacts such as `docs/storefront-analysis/generation-plan.json`, `generation-plan.yaml`, `agent-task-package/`, generated metadata, visual write recorder output, and browser QA reports.

It must not call commerce node, control plane, storefront runtime, or storefront v2 services. It must not add production runtime references, API transport code, route declarations, BFF behavior, SEO behavior, auth/session behavior, cart/checkout/account behavior, or database behavior.

This folder is intentionally file-based for the MVP:

- canonical skill instructions under `skills/`
- shared workflow references under `references/`
- JSON schemas under `schemas/`
- local helper scripts under `scripts/`
- host adapter notes under `adapters/`
- schema examples under `examples/`

Do not add a `.csproj` to this workspace unless a later architecture decision explicitly promotes it to executable tooling.

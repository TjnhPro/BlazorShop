---
name: storefront-visual-plan
description: Plan generated storefront visual work from StorefrontBuilder Phase 4 handoff artifacts without editing files.
---

# Storefront Visual Plan

Canonical source path: `tools/BlazorShop.AI.Visual/skills/storefront-visual-plan/SKILL.md`.

Use this skill when a generated storefront already has StorefrontBuilder Phase 4 artifacts and an agent needs a reviewable visual implementation plan before edits.

Read first:

1. `tools/BlazorShop.AI.Visual/references/architecture-boundary.md`
2. `tools/BlazorShop.AI.Visual/references/handoff-input-contract.md`
3. `tools/BlazorShop.AI.Visual/references/visual-ownership.md`
4. generated project `docs/storefront-analysis/generation-plan.json`
5. generated project `docs/storefront-analysis/agent-task-package/manifest.json`

Output must be generated-project-local under `docs/storefront-analysis/` and schema-backed by `tools/BlazorShop.AI.Visual/schemas/visual-plan.schema.json`.

Do not edit generated visual files in this skill. Missing inputs, unsupported behavior, protected file requests, and raw/source fallback needs become blockers.

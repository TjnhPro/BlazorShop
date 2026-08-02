---
name: storefront-visual-plan
description: Plan generated storefront visual work from StorefrontBuilder Phase 4 handoff artifacts without editing files.
---

# Storefront Visual Plan

Canonical source path: `tools/BlazorShop.AI.Visual/skills/storefront-visual-plan/SKILL.md`.

Use this skill when a generated storefront already has StorefrontBuilder Phase 4 artifacts and an agent needs a reviewable visual implementation plan before edits.

## Read Order

1. `tools/BlazorShop.AI.Visual/references/architecture-boundary.md`
2. `tools/BlazorShop.AI.Visual/references/handoff-input-contract.md`
3. `tools/BlazorShop.AI.Visual/references/visual-ownership.md`
4. generated project `docs/storefront-analysis/generation-plan.json`
5. generated project `docs/storefront-analysis/generation-plan.yaml`
6. generated project `docs/storefront-analysis/agent-task-package/manifest.json`
7. generated project `docs/storefront-analysis/agent-task-package/*`
8. generated project `docs/storefront-analysis/handoff-generation-summary.md`
9. generated project `docs/storefront-analysis/generated-files.yaml`

## Required Input Checks

Before planning, verify these paths exist:

- `docs/storefront-analysis/generation-plan.json`
- `docs/storefront-analysis/generation-plan.yaml`
- `docs/storefront-analysis/agent-task-package/manifest.json`
- every task package input file referenced by the manifest

Hash `generation-plan.json` and `agent-task-package/manifest.json` with SHA-256. Record those hashes in `visual-plan.json`; if the manifest exposes a source handoff hash, record it as `handoffHash`. Do not infer a handoff hash from raw evidence.

List every allowed output file from the task package. Normalize paths to forward slashes, reject traversal, and sort by normalized relative path.

## Planning Rules

Map every page and visual slot from `generation-plan.json` to exactly one implementation task or one blocked reason. Unsupported behavior must be blocked instead of implemented through transport, route, auth, SEO, BFF, cart, checkout, account, payment, order, or runtime changes.

Stable output ordering is required:

- pages stay in generation plan order, using route priority when present
- files sort by normalized relative path
- tasks group by page, then slot, then capability
- blockers and risks sort by stable ID

Do not edit generated visual files in this skill.

## Outputs

Write these generated-project-local artifacts:

- `docs/storefront-analysis/visual-plan.json`
- `docs/storefront-analysis/visual-implementation-checklist.todo.md`
- `docs/storefront-analysis/visual-plan-summary.md`

`visual-plan.json` must validate against `tools/BlazorShop.AI.Visual/schemas/visual-plan.schema.json`. Use `node tools/BlazorShop.AI.Visual/scripts/validate-visual-examples.mjs` to prove schema/example integrity before relying on the contract.

The checklist must include each allowed file, its planned tasks, required screenshots, acceptance checks, and explicit blockers. Missing inputs, unsupported behavior, protected file requests, and raw/source fallback needs become blockers.

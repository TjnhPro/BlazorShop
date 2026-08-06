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

Hash `generation-plan.json` and `agent-task-package/manifest.json` with SHA-256. Record those hashes in `visual-plan.json`. Record `handoffHash` from `agent-task-package/manifest.json` `handoffHash` or `sourceHandoffPackageHash`; if older packages do not expose it, use `generation-plan.json` `sourceHandoffPackageHash`. Do not infer a handoff hash from raw evidence.

List every allowed output file and every protected file from the task package. Read protected files from `agent-task-package/manifest.json` `protectedFiles`, or from `agent-task-package/inputs/file-boundary-manifest.json` `protectedFiles` for older packages. Normalize paths to forward slashes, reject traversal, and sort by normalized relative path.

Read `projects`, `serverProjectRoot`, `wasmProjectRoot`, `allowedOutputFilesByProject`, and `protectedFilesByProject` from `agent-task-package/manifest.json` when present. Every planned file must record `targetProject` (`server` or `wasm`) and `projectRelativePath`. If an older package lacks grouped fields, derive `targetProject` from `targetPath`: paths starting with `<ProjectName>.WASM/` are `wasm`; all other generated-project-relative paths are `server`.

## Planning Rules

Map every page and visual slot from `generation-plan.json` to exactly one implementation task or one blocked reason. Unsupported behavior must be blocked instead of implemented through transport, route, auth, SEO, BFF, cart, checkout, account, payment, order, or runtime changes.

Plan server and WASM work separately:

- SSR layout, catalog, content, state pages, generated CSS, and server visual wrappers target `server`.
- Account, cart, checkout, auth, and other hydrated browser-facing visual shells target `wasm` by default.
- Product, cart, and checkout descriptors remain Presentation-owned contracts; plan only visual wrapper changes that preserve descriptors.
- If a requested change would require moving a slot across `server`/`wasm` or editing protected runtime files, block it.

Stable output ordering is required:

- pages stay in generation plan order, using route priority when present
- files sort by normalized relative path
- tasks group by page, then slot, then capability
- blockers and risks sort by stable ID

Do not edit generated visual files in this skill.

## Outputs

Write these generated-project-local artifacts:

- `docs/storefront-analysis/visual-plan.json`
- `docs/storefront-analysis/visual-implementation-checklist.json`
- `docs/storefront-analysis/visual-implementation-checklist.todo.md`
- `docs/storefront-analysis/visual-plan-summary.md`

`visual-plan.json` must validate against `tools/BlazorShop.AI.Visual/schemas/visual-plan.schema.json` and include `projects`, `allowedFileTargets`, `protectedFileTargets`, and `targetProject` for every visual slot. Use `node tools/BlazorShop.AI.Visual/scripts/validate-visual-examples.mjs` to prove schema/example integrity before relying on the contract.

`visual-implementation-checklist.json` is the closure contract artifact and must validate against `tools/BlazorShop.AI.Visual/schemas/visual-implementation-checklist.schema.json`. The `.todo.md` checklist may mirror the JSON for human review, but closure gates read the JSON artifact.

The checklist must include each allowed file, its planned tasks, required screenshots, acceptance checks, and explicit blockers. Missing inputs, unsupported behavior, protected file requests, and raw/source fallback needs become blockers. Use only `completed`, `blocked`, or `not-applicable` as closure status values.

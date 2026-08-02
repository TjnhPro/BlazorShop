---
name: storefront-visual-implement
description: Implement visual-only generated storefront changes from an approved visual plan and checklist.
---

# Storefront Visual Implement

Canonical source path: `tools/BlazorShop.AI.Visual/skills/storefront-visual-implement/SKILL.md`.

Use this skill only after `storefront-visual-plan` has produced a visual plan and implementation checklist with no blocking item for the requested scope.

Read first:

1. `tools/BlazorShop.AI.Visual/references/architecture-boundary.md`
2. `tools/BlazorShop.AI.Visual/references/visual-ownership.md`
3. `tools/BlazorShop.AI.Visual/references/razor-visual-rules.md`
4. `tools/BlazorShop.AI.Visual/references/css-visual-rules.md`
5. generated project `docs/storefront-analysis/visual-plan.json`
6. generated project `docs/storefront-analysis/visual-implementation-checklist.todo.md`
7. generated project `docs/storefront-analysis/agent-task-package/manifest.json`

Edits must stay inside generated-owned visual files allowed by StorefrontBuilder's task package. After edits, run StorefrontBuilder's visual write recorder and a generated project build or focused compile check.

Output must include `docs/storefront-analysis/visual-implementation-report.json` and `docs/storefront-analysis/visual-implementation-report.md`.

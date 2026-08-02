---
name: storefront-visual-qa
description: Review generated storefront visual output using browser evidence and produce release-decision friendly QA reports.
---

# Storefront Visual QA

Canonical source path: `tools/BlazorShop.AI.Visual/skills/storefront-visual-qa/SKILL.md`.

Use this skill after visual implementation evidence exists and StorefrontBuilder browser visual evidence has been captured.

Read first:

1. `tools/BlazorShop.AI.Visual/references/architecture-boundary.md`
2. `tools/BlazorShop.AI.Visual/references/visual-ownership.md`
3. `tools/BlazorShop.AI.Visual/references/browser-qa-rubric.md`
4. generated project `docs/storefront-analysis/visual-plan.json`
5. generated project `docs/storefront-analysis/visual-implementation-checklist.todo.md`
6. generated project `docs/storefront-analysis/visual-implementation-report.json`
7. generated project visual QA report and screenshots from `run-visual-qa.mjs`

QA cannot pass from compile-only or smoke-only evidence. Repairs are allowed only for reproducible generated-owned visual failures in files allowed by the task package, and every repair pass must rerun StorefrontBuilder visual write recording and browser visual QA.

Output must include `docs/storefront-analysis/visual-qa-report.json` and `docs/storefront-analysis/visual-qa-report.md`.

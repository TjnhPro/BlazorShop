---
name: storefront-visual-qa
description: Review generated storefront visual output using browser evidence and produce release-decision friendly QA reports.
---

# Storefront Visual QA

Canonical source path: `tools/BlazorShop.AI.Visual/skills/storefront-visual-qa/SKILL.md`.

Use this skill after visual implementation evidence exists and StorefrontBuilder browser visual evidence has been captured.

## Required Read Order

1. `tools/BlazorShop.AI.Visual/references/architecture-boundary.md`
2. `tools/BlazorShop.AI.Visual/references/visual-ownership.md`
3. `tools/BlazorShop.AI.Visual/references/browser-qa-rubric.md`
4. `tools/BlazorShop.AI.Visual/references/visual-checkpoint-contract.md`
5. generated project `docs/storefront-analysis/agent-task-package/manifest.json`
6. generated project `docs/storefront-analysis/visual-plan.json`
7. generated project `docs/storefront-analysis/visual-implementation-checklist.json`
8. generated project `docs/storefront-analysis/visual-implementation-report.json`
9. generated project latest `docs/storefront-analysis/visual-checkpoints/{operationId}/visual-checkpoint.json`
10. reference evidence paths listed by the visual plan or task package manifest
11. generated project runtime visual QA report and screenshots from `run-visual-qa.mjs`

Stop if browser evidence is missing, stale, or only compile/smoke evidence exists.

## Browser Evidence Review

Inspect every captured desktop, tablet, and mobile screenshot required by the visual plan. Compare each runtime screenshot against the approved reference evidence for the same page and viewport. Review the markdown output and machine-readable summary from `run-visual-qa.mjs` and record:

- server SSR route status for planned `server` pages
- WASM bootstrap asset loading for planned `wasm` components/routes
- interactive component hydration/startup on account, cart, and checkout visual shells
- direct refresh results for `/account`, `/cart`, and `/checkout` when those pages are in scope
- same-origin network behavior and absence of direct Commerce Node, Control Plane, Commerce Admin, or legacy API calls
- generated CSS link/load status and whether the page applies generated typography/styles
- horizontal overflow across desktop, tablet, and mobile
- blank page findings
- overlapping text
- cropped controls
- mobile navigation availability
- visible cart, account, and checkout entry points when the visual plan or storefront route requires them
- product gallery 1:1 presentation
- product price and action readability
- broken image placeholders
- visual hierarchy and ecommerce scanability
- missing required visual slots from the plan
- console warnings/errors, page errors, failed requests, CSS status, overflow findings, and placeholder findings

Record `referenceEvidenceReviewed: true` only after the reference evidence paths were opened and compared against runtime evidence. QA must fail if reference evidence is missing, stale, or not comparable to the required page/viewport matrix.

QA cannot pass from compile-only, restore-only, or smoke-only evidence.

When `visual-plan.json` or the task package includes `targetProject`, record whether each issue belongs to `server` or `wasm`. Use `server` for SSR layout/catalog/content/CSS rendering issues and `wasm` for account/cart/checkout hydration, browser bootstrap, or interactive visual shell issues. Do not recommend protected server/WASM runtime edits as fixes for visual-only QA findings.

## Closure Severity

Use this severity vocabulary in `visual-qa-report.json`:

- `Critical`: blank route, broken core layout, missing checkout/cart/account entry, blocked main flow, fatal runtime browser error.
- `Major`: visible mismatch against reference that harms ecommerce use, important responsive break, missing visual slot, broken gallery or product action area.
- `Minor`: polish difference that does not block release.

Closure requires `unacceptedCriticalCount: 0` and `unacceptedMajorCount: 0`. Minor issues may remain only when they are recorded with a follow-up and are not a release blocker.

Accepted differences must be explicit in `acceptedDifferences` with page, viewport, severity, reviewer, and reason. Do not hide accepted differences inside prose-only notes.

## Repair Policy

Repair is optional and bounded. Default `maxRepairAttempts` is `2`; use `3` only when the gate explicitly configures it.

`tools/BlazorShop.AI.StorefrontBuilder/scripts/qa/repair-visual-generation.mjs` is a bounded mechanical helper, not the canonical visual QA skill. Use it only after this QA skill has identified a reproducible browser-evidence failure that matches the helper's supported generated visual repair patterns.

Allow a repair attempt only when all conditions are true:

- the issue is reproducible from browser evidence
- the failing file is generated-owned
- the failing file is listed in the allowed task package
- the fix is visual-only
- the fix does not add routes, direct API transport, business logic, auth/session behavior, SEO behavior, descriptor edits, or protected file edits

Each repair attempt must identify its source as one of:

- `manual-agent-edit`
- `mechanical-repair-helper`
- `no-repair-attempted`

After every repair pass, update the checkpoint pre/post snapshot and run StorefrontBuilder visual write recording with `record-agent-visual-writes.mjs --from-checkpoint <checkpoint> --closure-mode`, then rerun browser evidence capture with `run-visual-qa.mjs`. If either step fails, stop repair and keep the issue unresolved in the report.

## Outputs

Write both generated-project-local outputs:

- `docs/storefront-analysis/visual-qa-report.json`
- `docs/storefront-analysis/visual-qa-report.md`

The JSON output must follow `tools/BlazorShop.AI.Visual/schemas/visual-qa-report.schema.json`. The markdown output must be release-decision friendly and include:

- evidence paths reviewed
- runtime evidence paths
- reference evidence paths
- `referenceEvidenceReviewed`
- page and viewport coverage
- independent reviewer identity
- comparison dimensions
- accepted differences with reason
- unaccepted critical and major issue counters
- pass/fail decision
- issue severity
- target file hints
- repair attempt count and source
- unresolved issues with next action

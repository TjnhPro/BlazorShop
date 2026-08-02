---
name: storefront-visual-implement
description: Implement visual-only generated storefront changes from an approved visual plan and checklist.
---

# Storefront Visual Implement

Canonical source path: `tools/BlazorShop.AI.Visual/skills/storefront-visual-implement/SKILL.md`.

Use this skill only after `storefront-visual-plan` has produced a visual plan and implementation checklist with no blocking item for the requested scope.

## Read Order

1. `tools/BlazorShop.AI.Visual/references/architecture-boundary.md`
2. `tools/BlazorShop.AI.Visual/references/visual-ownership.md`
3. `tools/BlazorShop.AI.Visual/references/razor-visual-rules.md`
4. `tools/BlazorShop.AI.Visual/references/css-visual-rules.md`
5. `tools/BlazorShop.AI.Visual/references/visual-checkpoint-contract.md`
6. generated project metadata under `docs/storefront-analysis/metadata.yaml`
7. generated project `docs/storefront-analysis/visual-plan.json`
8. generated project `docs/storefront-analysis/visual-implementation-checklist.json`
9. generated project `docs/storefront-analysis/agent-task-package/manifest.json`

## Stop Conditions

Stop before edits when the visual plan contains blockers for the requested scope, when the checklist hash does not match the approved plan, or when a requested file is not allowed by the task package.

## Edit Rules

Edits must stay inside generated-owned visual files allowed by StorefrontBuilder's task package.

Preserve:

- no @page generated visual files
- product purchase descriptors
- same-origin browser action descriptors
- route ownership
- SEO ownership
- account, cart, checkout, auth, payment, and order contract behavior
- component parameters and semantic descriptors required by Presentation and Browser contracts

Use existing generated project patterns before introducing local abstractions. Keep visual code focused on markup, CSS, layout, responsive structure, and scanable ecommerce presentation.

When product gallery files are touched, preserve or create 1:1 image frames. When header, main navigation, product detail, listing, cart, account, or checkout visual shells are in scope, include responsive desktop, tablet, and mobile states.

Do not add transport, business logic, auth/session logic, SEO logic, route declarations, direct API calls, or runtime package references.

## Required Commands

After edits, run StorefrontBuilder's visual write recorder:

```powershell
node tools\BlazorShop.AI.StorefrontBuilder\scripts\generate\record-agent-visual-writes.mjs --project-root <generated-project-root> --from-checkpoint docs/storefront-analysis/visual-checkpoints/{operationId}/visual-checkpoint.json --closure-mode
```

`--written-files` may be passed as a hint, but closure truth comes from the checkpoint pre/post snapshot.

Then run a generated project build or focused compile check:

```powershell
dotnet build <generated-project-csproj> --no-restore
```

Also scan the generated project for forbidden visual drift:

```powershell
rg -n "@page|HttpClient|fetch\(|/api/storefront/stores|CommerceNodeBaseUrl" <generated-project-root>
```

## Outputs

Emit:

- `docs/storefront-analysis/visual-implementation-report.json`
- `docs/storefront-analysis/visual-implementation-report.md`
- checkpoint artifacts under `docs/storefront-analysis/visual-checkpoints/{operationId}/`

`visual-implementation-report.json` must include before/after snapshot hashes, changed file list, visual write recorder result path, build result, boundary result, and unresolved items.
For closure, it must also include the visual plan `operationId` and the checkpoint path under `docs/storefront-analysis/visual-checkpoints/{operationId}/visual-checkpoint.json`.
The recorder output must have `detectionMode: checkpoint-auto-detect`; hint-only write records are not valid closure evidence.

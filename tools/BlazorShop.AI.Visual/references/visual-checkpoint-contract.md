# Visual Checkpoint Contract

Version: `0.1.0`
Provenance: StorefrontBuilder Phase 4.10 MVP visual skill workspace.

## Purpose

A visual checkpoint records exactly what changed during an agent visual implementation or repair pass. It is generated-project-local and disposable with the generated project.

Checkpoint artifacts live under:

```text
docs/storefront-analysis/visual-checkpoints/{operationId}/
```

## Required Fields

Each checkpoint must record:

- generated project root
- operation ID
- source visual plan hash
- source checklist hash
- pre-edit hashes for every allowed file in scope
- post-edit hashes for every changed file
- changed file detection result
- diff summary
- visual write recorder result path

The JSON artifact must conform to `tools/BlazorShop.AI.Visual/schemas/visual-checkpoint.schema.json`.

Closure checkpoints must include `operationId`, `visualPlanHash`, `checklistHash`, `preEditSnapshotHash`, `postEditSnapshotHash`, `changedFiles`, `unexpectedFiles`, and `sourceTreeSnapshotScope`. `unexpectedFiles` must be an empty array for closure pass.

## Detection Rules

Changed files must be detected from filesystem content hashes and path comparison, not trusted only from an agent-supplied list.

Before edits, hash every task-package allowed file in scope. After edits, re-hash those files and scan the generated project for changed files relative to the pre-edit snapshot. A file reported by the agent but unchanged stays in the report as `unchanged`. A changed file not reported by the agent is still included.

Closure write recording must use:

```powershell
node tools\BlazorShop.AI.StorefrontBuilder\scripts\generate\record-agent-visual-writes.mjs --project-root <generated-project-root> --from-checkpoint docs/storefront-analysis/visual-checkpoints/{operationId}/visual-checkpoint.json --closure-mode
```

`--written-files` is only a hint. In closure mode, omitted changed files, unchanged hint files, unexpected files, deleted generated visual files, and implementation-report/checkpoint mismatches fail before browser QA.

## Failure Rules

The checkpoint fails when:

- a changed file is outside the task package allowed generated visual files
- a changed file is protected by StorefrontBuilder
- the source visual plan hash does not match the plan used by the implementation skill
- the source checklist hash does not match the checklist used by the implementation skill
- the visual write recorder result is missing after implementation or repair

A stale checklist cannot silently drive implementation. Hash drift must stop the implementation and force a new planning pass or an explicit checklist refresh.

# Handoff Input Contract

Version: `0.1.0`
Provenance: StorefrontBuilder Phase 4.10 MVP visual skill workspace.

## Allowed Inputs

Visual skills may consume these generated-project-local StorefrontBuilder artifacts:

- `docs/storefront-analysis/generation-plan.json`
- `docs/storefront-analysis/generation-plan.yaml`
- `docs/storefront-analysis/handoff-generation-summary.md`
- `docs/storefront-analysis/handoff-placeholders.json`
- `docs/storefront-analysis/metadata.yaml`
- `docs/storefront-analysis/generated-files.yaml`
- `docs/storefront-analysis/agent-task-package/manifest.json`
- `docs/storefront-analysis/agent-task-package/*`
- `docs/storefront-analysis/agent-written-files.json`
- StorefrontBuilder visual QA and repair reports under `docs/storefront-analysis/`

## Required Task Package Inputs

The `agent-task-package` must identify allowed visual targets, protected targets, planned slots, route and descriptor constraints, source handoff hashes, generation plan hash, and generator version provenance.

The `generation-plan` files must identify planned files, pages, slots, write modes, ownership, protected files, warnings, and blockers before any implementation skill edits files.

## Forbidden Fallbacks

Visual skills must not read these as fallback inputs:

- raw captures
- source project folders
- source analysis folders outside the portable handoff package
- unresolved review folders
- report folders from ReverseEngineering
- Storefront v2 source
- backend source
- Starter source

Unsupported or missing inputs must become blockers in `visual-plan.json` or the implementation checklist.

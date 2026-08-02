# StorefrontVisualSkills Phase 4.10 MVP End-To-End Closure.todo

Status: Planned
Owner: Storefront Platform
Created: 2026-08-02
Target folder: `docs/visual-reverse-engineering-skill`
Depends on:

- `18-StorefrontBuilder-Phase4-Agent-Assisted-Visual-Generation.todo.md`.
- StorefrontBuilder Phase 4 handoff preflight, generation plan, Starter-based project generation, visual write recorder, visual QA, bounded repair, and handoff-aware regeneration.
- `docs/architecture/11-storefront-builder.md`.
- `docs/agents/storefront-builder.md`.
- `docs/visual-reverse-engineering-skill/README.md`.

Primary goal: add a small, deterministic visual-skill workspace around the existing StorefrontBuilder Phase 4 pipeline so an agent can plan, implement, QA, and close one generated storefront visual pass from the approved handoff package without guessing backend contracts, rewriting StorefrontBuilder, or mutating protected projects.

## Why This File Exists

Phase 4 already proves the core generator path:

```text
portable handoff package
  -> StorefrontBuilder preflight
  -> deterministic generation plan
  -> Starter-based generated project
  -> constrained visual write recording
  -> visual QA
  -> bounded repair
  -> regeneration safety
```

The remaining MVP problem is operational. Agents still need a clear visual workflow that says:

```text
plan what visual files should change
  -> implement only approved generated-owned visual files
  -> record writes through StorefrontBuilder
  -> run independent browser QA
  -> repair only visual regressions
  -> produce release-ready evidence
```

This phase must not introduce a second generator. It creates skill instructions, report contracts, evidence conventions, and target-specific gates around the current StorefrontBuilder implementation.

## Codebase Baseline Findings

- `tools/BlazorShop.AI.StorefrontBuilder` already owns the active Phase 4 generation and regeneration engine.
- `tools/BlazorShop.AI.StorefrontReverseEngineering` already owns source capture, analysis, portable handoff packaging, readiness, and schema-backed handoff validation.
- `tools/BlazorShop.AI.Visual` does not currently exist.
- Canonical visual skills such as `storefront-visual-plan`, `storefront-visual-implement`, and `storefront-visual-qa` do not currently exist in the repository.
- `tools/BlazorShop.AI.StorefrontBuilder/scripts/generate/write-agent-task-package.mjs` already emits `docs/storefront-analysis/agent-task-package/`.
- `tools/BlazorShop.AI.StorefrontBuilder/scripts/generate/record-agent-visual-writes.mjs` already enforces allowed generated visual writes and forbids route, API transport, business, auth, SEO, and protected descriptor drift.
- `tools/BlazorShop.AI.StorefrontBuilder/scripts/qa/run-visual-qa.mjs` already captures multi-viewport evidence and checks generated visual structure.
- `tools/BlazorShop.AI.StorefrontBuilder/scripts/qa/repair-visual-generation.mjs` already provides a bounded mechanical repair loop.
- Target-specific Phase 4 MVP and final closure wrapper scripts are not currently present.
- Visual implementation/report/checklist schemas for the proposed skills are not currently present.
- Codex and Claude will not automatically discover repo-local skill files unless there is an adapter, setup path, or explicit invocation path.

## Locked Decisions

- `tools/BlazorShop.AI.Visual` is a development-time skill, reference, schema, and report workspace only.
- Do not create a `.csproj` or runtime executable for `tools/BlazorShop.AI.Visual` in this MVP phase.
- StorefrontBuilder remains the only generator/regeneration owner.
- ReverseEngineering remains the only source evidence and handoff package owner.
- Visual skills consume `generation-plan.json`, `generation-plan.yaml`, and `agent-task-package/*`; they must not reinterpret raw captures, source project folders, V2 source, backend source, or Starter source as visual instructions.
- Visual implementation may edit only generated-owned files allowed by StorefrontBuilder's task package and visual write recorder.
- Visual implementation must not edit StorefrontBuilder, ReverseEngineering, Starter, Presentation, Runtime, Client, Browser, Commerce Node, Control Plane, database, OpenAPI, BFF, auth, SEO, cart, checkout, account, payment, or order logic.
- Browser interactions stay same-origin and contract-driven. No generated visual skill may add direct Commerce Node, Commerce Admin, Control Plane, or legacy API calls.
- Visual QA must use browser evidence, not smoke-only checks.
- GitHub Actions evidence is out of scope while Actions are disabled during development. Local deterministic gates are authoritative for this phase.

## Out Of Scope

- AgentRunner or automatic model orchestration.
- Direct OpenAI, Claude, Gemini, or other model API integration.
- New production services.
- New Commerce Node APIs.
- New Storefront API contracts.
- Pixel-perfect visual diff as a hard MVP gate.
- Automatic promotion of generated projects into `BlazorShop.sln`.
- Editing or regenerating Phase 3 handoff evidence.
- Functional commerce QA replacement. Existing StorefrontBuilder and ecommerce/browser QA gates remain the source of truth for functional behavior.

## Autoplan Review Decisions

| Decision | Classification | Chosen Direction | Rationale |
| --- | --- | --- | --- |
| Workspace type | Auto-decided | Add `tools/BlazorShop.AI.Visual` as a file-based skill/report workspace. | The codebase already has StorefrontBuilder and ReverseEngineering executables; a third runtime tool would duplicate ownership. |
| Skill shape | Auto-decided | Create three canonical skills: plan, implement, QA. | These match the real agent workflow and keep responsibilities reviewable. |
| Skill discovery | Auto-decided | Add thin host adapters and installation docs instead of assuming repo-local discovery. | Codex/Claude do not automatically load arbitrary repo-local skill folders. |
| Report contracts | Auto-decided | Add schemas before relying on skill outputs. | Machine-readable evidence prevents vague "agent said it passed" closure. |
| Implementation boundary | Auto-decided | Require StorefrontBuilder visual write recorder after every implementation or repair pass. | This reuses the existing trusted boundary gate instead of creating a weaker duplicate. |
| QA strategy | Auto-decided | Run browser-based visual QA plus StorefrontBuilder boundary/regeneration gates. | The user explicitly needs real Playwright-style evidence, not smoke-only checks. |
| Final closure | Auto-decided | Add local wrapper gates, but do not require GitHub Actions while disabled. | The repo is in development mode and Actions are intentionally disabled. |
| Visual fidelity | Taste decision | Keep pixel-perfect scoring deferred; capture evidence and structural visual checks are MVP. | Current codebase does not have a stable visual diff baseline, and strict pixel diff would create noisy failures before the workflow is mature. |

## Phase Order

1. Phase 4.10.0 - Baseline And Scope Lock
2. Phase 4.10.1 - Visual Workspace Foundation
3. Phase 4.10.2 - Shared Visual References
4. Phase 4.10.3 - Schemas And Report Contracts
5. Phase 4.10.4 - Skill Discovery And Host Adapters
6. Phase 4.10.5 - `storefront-visual-plan`
7. Phase 4.10.6 - Implementation Checkpoint Foundation
8. Phase 4.10.7 - `storefront-visual-implement`
9. Phase 4.10.8 - Visual Capture Evidence
10. Phase 4.10.9 - `storefront-visual-qa`
11. Phase 4.10.10 - Repair Policy Alignment
12. Phase 4.10.11 - Target-Specific Phase 4 MVP Gate
13. Phase 4.10.12 - End-To-End Pilot
14. Phase 4.10.13 - Final Closure Gate
15. Phase 4.10.14 - Documentation And Agent Guide Updates

Implement in this order. Do not implement the visual QA skill before the visual plan and implementation report contracts exist.

## Phase 4.10.0 - Baseline And Scope Lock

Goal: prove this phase starts from the current StorefrontBuilder architecture and does not reopen completed generator work.

Tasks:

- [x] Confirm `tools/BlazorShop.AI.Visual` still does not exist before implementation starts.
- [x] Confirm current StorefrontBuilder Phase 4 scripts exist:
  - [x] `tools/BlazorShop.AI.StorefrontBuilder/build-storefront.ps1`.
  - [x] `tools/BlazorShop.AI.StorefrontBuilder/scripts/generate/write-agent-task-package.mjs`.
  - [x] `tools/BlazorShop.AI.StorefrontBuilder/scripts/generate/record-agent-visual-writes.mjs`.
  - [x] `tools/BlazorShop.AI.StorefrontBuilder/scripts/qa/run-visual-qa.mjs`.
  - [x] `tools/BlazorShop.AI.StorefrontBuilder/scripts/qa/repair-visual-generation.mjs`.
  - [x] `tools/BlazorShop.AI.StorefrontBuilder/scripts/validate/Test-StorefrontBuilderHandoffBoundary.mjs`.
- [x] Confirm there is no existing `scripts/qa/run-storefront-phase4-mvp-gate.ps1`.
- [x] Confirm there is no existing `scripts/qa/run-storefront-phase4-final-closure-gate.ps1`.
- [x] Confirm there are no existing visual skill schemas with the target names.
- [x] Capture the current StorefrontBuilder generated artifact paths used by Phase 4.
- [x] Capture the current generated project ownership rules from `docs/architecture/11-storefront-builder.md`.
- [x] Confirm GitHub Actions are intentionally out of scope for this phase.

Checks:

```powershell
Test-Path tools\BlazorShop.AI.Visual
Test-Path scripts\qa\run-storefront-phase4-mvp-gate.ps1
Test-Path scripts\qa\run-storefront-phase4-final-closure-gate.ps1
rg -n "record-agent-visual-writes|run-visual-qa|repair-visual-generation|write-agent-task-package" tools\BlazorShop.AI.StorefrontBuilder
```

DoD:

- [x] Baseline is written into the implementation commit notes.
- [x] No code changes are made before this scope lock is complete.

Phase 4.10.0 evidence:

- Baseline checks ran before edits in this plan file.
- `Test-Path tools\BlazorShop.AI.Visual` returned `False`.
- `Test-Path scripts\qa\run-storefront-phase4-mvp-gate.ps1` returned `False`.
- `Test-Path scripts\qa\run-storefront-phase4-final-closure-gate.ps1` returned `False`.
- Required StorefrontBuilder scripts all exist: `build-storefront.ps1`, `write-agent-task-package.mjs`, `record-agent-visual-writes.mjs`, `run-visual-qa.mjs`, `repair-visual-generation.mjs`, and `Test-StorefrontBuilderHandoffBoundary.mjs`.
- Target visual skill/schema names were found only in this Phase 4.10 plan before workspace creation.
- Current generated artifact roots remain `artifacts/storefront-builder/generated/{ProjectName}` and `obj/storefront-builder/generated/{ProjectName}`.
- Current ownership remains: StorefrontBuilder owns generation/regeneration; ReverseEngineering owns evidence/handoff; generated storefronts own only generated markup, CSS, store-specific assets, pages, visual analysis artifacts, and AI-tuned components inside the generated/custom project.
- GitHub Actions evidence remains intentionally out of scope while Actions are disabled; local deterministic gates are authoritative.

## Phase 4.10.1 - Visual Workspace Foundation

Goal: create the visual skill workspace without adding production runtime coupling.

Tasks:

- [x] Create `tools/BlazorShop.AI.Visual/`.
- [x] Add `tools/BlazorShop.AI.Visual/README.md` explaining:
  - [x] this is a development-time skill workspace.
  - [x] it does not generate storefront projects directly.
  - [x] it consumes StorefrontBuilder artifacts.
  - [x] it never calls Commerce Node, Control Plane, Storefront Runtime, or Storefront V2.
- [x] Add `tools/BlazorShop.AI.Visual/version.json` for visual skill/report contract versioning.
- [x] Add folders:
  - [x] `skills/`.
  - [x] `skills/storefront-visual-plan/`.
  - [x] `skills/storefront-visual-implement/`.
  - [x] `skills/storefront-visual-qa/`.
  - [x] `references/`.
  - [x] `schemas/`.
  - [x] `scripts/`.
  - [x] `adapters/`.
  - [x] `examples/`.
- [x] Do not add a `.csproj`.
- [x] Do not add references from existing production projects into this folder.
- [x] Add `.gitkeep` files only where empty folders are needed.

Checks:

```powershell
Get-ChildItem tools\BlazorShop.AI.Visual -Recurse
rg -n "<Project Sdk=|ProjectReference|PackageReference|CommerceNode|ControlPlane|Storefront.V2" tools\BlazorShop.AI.Visual -S
```

DoD:

- [x] Workspace exists.
- [x] Workspace is documentation/script/schema only.
- [x] Static search proves no runtime dependency has been introduced.

Phase 4.10.1 evidence:

- Added inert workspace root `tools/BlazorShop.AI.Visual/`.
- Added `README.md`, `version.json`, and required folders with `.gitkeep` files for empty folders.
- Did not add a `.csproj`.
- `Get-ChildItem tools\BlazorShop.AI.Visual -Recurse` confirmed the expected folder/file layout.
- `Get-ChildItem tools\BlazorShop.AI.Visual -Recurse -Filter *.csproj` returned no files.
- `rg -n "<Project Sdk=|ProjectReference|PackageReference|CommerceNode|ControlPlane|Storefront\.V2" tools\BlazorShop.AI.Visual -S` returned no matches.

## Phase 4.10.2 - Shared Visual References

Goal: create one shared reference base consumed by all three skills.

Tasks:

- [x] Add `references/architecture-boundary.md`:
  - [x] StorefrontBuilder owns generation/regeneration.
  - [x] ReverseEngineering owns evidence/handoff.
  - [x] visual skills own planning, visual edits, and visual QA reports only.
- [x] Add `references/handoff-input-contract.md`:
  - [x] allowed input files.
  - [x] required `agent-task-package` files.
  - [x] required `generation-plan` files.
  - [x] forbidden fallback folders.
- [x] Add `references/visual-ownership.md`:
  - [x] generated-owned Razor files.
  - [x] generated-owned CSS files.
  - [x] generated-owned static assets.
  - [x] protected files and protected descriptors.
- [x] Add `references/razor-visual-rules.md`:
  - [x] no `@page`.
  - [x] no API transport.
  - [x] no business logic.
  - [x] no direct auth/session/customer/order assumptions.
  - [x] preserve descriptors and component contracts.
- [x] Add `references/css-visual-rules.md`:
  - [x] responsive behavior.
  - [x] no hidden overflow masking as a general fix.
  - [x] no blocking overlay.
  - [x] avoid one-note palettes.
  - [x] preserve ecommerce scanability.
- [x] Add `references/browser-qa-rubric.md`:
  - [x] desktop, tablet, mobile evidence.
  - [x] product gallery 1:1 checks.
  - [x] header/navigation/cart/action visibility.
  - [x] overflow, blank, broken image, and placeholder checks.
  - [x] functional flows remain covered by existing StorefrontBuilder/browser gates.
- [x] Add a reference version/provenance section to each file.

Checks:

```powershell
rg -n "StorefrontBuilder|ReverseEngineering|agent-task-package|generation-plan|no @page|same-origin" tools\BlazorShop.AI.Visual\references
```

DoD:

- [x] Every visual skill can link to the shared references instead of duplicating boundary rules.
- [x] References explicitly reject raw capture/source fallback.

Phase 4.10.2 evidence:

- Added shared reference docs under `tools/BlazorShop.AI.Visual/references/`.
- `architecture-boundary.md` records StorefrontBuilder, ReverseEngineering, and Visual skill ownership.
- `handoff-input-contract.md` lists allowed generated-project-local inputs and forbidden raw/source fallback folders.
- `visual-ownership.md` defines generated-owned Razor, CSS, static assets, protected files, and protected descriptors.
- `razor-visual-rules.md`, `css-visual-rules.md`, and `browser-qa-rubric.md` define visual edit and browser QA boundaries.
- `rg -n "StorefrontBuilder|ReverseEngineering|agent-task-package|generation-plan|no @page|same-origin" tools\BlazorShop.AI.Visual\references` returned matches across the reference set.

## Phase 4.10.3 - Schemas And Report Contracts

Goal: define machine-readable artifacts before agents produce them.

Tasks:

- [x] Add `schemas/visual-plan.schema.json` for `storefront-visual-plan` output:
  - [x] project name.
  - [x] store key.
  - [x] handoff hash.
  - [x] generation plan hash.
  - [x] task package hash.
  - [x] pages.
  - [x] visual slots.
  - [x] allowed files.
  - [x] protected files.
  - [x] implementation order.
  - [x] risks.
  - [x] blockers.
- [x] Add `schemas/visual-implementation-checklist.schema.json`:
  - [x] checklist ID.
  - [x] source visual plan hash.
  - [x] file tasks.
  - [x] acceptance checks.
  - [x] required screenshots.
  - [x] forbidden edits.
- [x] Add `schemas/visual-implementation-report.schema.json`:
  - [x] before snapshot hash.
  - [x] after snapshot hash.
  - [x] changed file list.
  - [x] recorder result path.
  - [x] build result.
  - [x] boundary result.
  - [x] unresolved items.
- [x] Add `schemas/visual-checkpoint.schema.json`:
  - [x] checkpoint ID.
  - [x] operation ID.
  - [x] plan hash.
  - [x] checklist hash.
  - [x] pre-edit file hashes.
  - [x] post-edit file hashes.
  - [x] diff summary.
- [x] Add `schemas/visual-qa-report.schema.json`:
  - [x] viewport captures.
  - [x] evidence paths.
  - [x] issue list.
  - [x] severity.
  - [x] target file hints.
  - [x] repair attempts.
  - [x] pass/fail.
- [x] Add `schemas/phase4-mvp-gate-report.schema.json`:
  - [x] command metadata.
  - [x] generated project root.
  - [x] input handoff metadata.
  - [x] gate steps.
  - [x] artifact paths.
  - [x] final decision.
- [x] Add a lightweight schema validation script only if existing repo test helpers cannot validate these JSON files cleanly.
- [x] Add example valid JSON artifacts under `examples/`.

Checks:

```powershell
rg -n "\"\\$schema\"|visual-plan|visual-implementation|visual-qa|phase4-mvp" tools\BlazorShop.AI.Visual\schemas tools\BlazorShop.AI.Visual\examples
```

DoD:

- [x] Skill outputs are schema-backed.
- [x] Example artifacts validate.
- [x] Schema names do not conflict with StorefrontBuilder or ReverseEngineering schemas.

Phase 4.10.3 evidence:

- Added six visual workspace schemas under `tools/BlazorShop.AI.Visual/schemas/`.
- Added six matching valid examples under `tools/BlazorShop.AI.Visual/examples/`.
- Added dependency-free validator `tools/BlazorShop.AI.Visual/scripts/validate-visual-examples.mjs` because existing schema helpers target StorefrontBuilder/ReverseEngineering schemas rather than this workspace.
- `node --check tools\BlazorShop.AI.Visual\scripts\validate-visual-examples.mjs` passed.
- `node tools\BlazorShop.AI.Visual\scripts\validate-visual-examples.mjs` passed with `Visual schema examples validated: 6.`
- `rg -n '"\$schema"|visual-plan|visual-implementation|visual-qa|phase4-mvp' tools\BlazorShop.AI.Visual\schemas tools\BlazorShop.AI.Visual\examples` returned visual schema/example matches.

## Phase 4.10.4 - Skill Discovery And Host Adapters

Goal: make the skills usable by agents without assuming repo-local automatic discovery.

Tasks:

- [x] Add canonical `SKILL.md` files under:
  - [x] `tools/BlazorShop.AI.Visual/skills/storefront-visual-plan/SKILL.md`.
  - [x] `tools/BlazorShop.AI.Visual/skills/storefront-visual-implement/SKILL.md`.
  - [x] `tools/BlazorShop.AI.Visual/skills/storefront-visual-qa/SKILL.md`.
- [x] Add `adapters/codex/README.md` with the exact recommended invocation pattern.
- [x] Add `adapters/claude/README.md` with the exact recommended invocation pattern.
- [x] Add optional copy/install script only if it copies from canonical skill files and does not create divergent skill bodies.
- [x] Add a rule that adapters are thin pointers; canonical instructions stay in `tools/BlazorShop.AI.Visual/skills/*/SKILL.md`.
- [x] Add a static check that adapter files mention the canonical source path.
- [x] Document how to invoke the skills explicitly by path when they are not installed into the user's skill root.

Checks:

```powershell
rg -n "canonical|tools/BlazorShop.AI.Visual/skills|storefront-visual-plan|storefront-visual-implement|storefront-visual-qa" tools\BlazorShop.AI.Visual\adapters tools\BlazorShop.AI.Visual\skills
```

DoD:

- [x] Agents have a clear path to use the skills.
- [x] No duplicated skill body becomes a second source of truth.

Phase 4.10.4 evidence:

- Added canonical `SKILL.md` files under `tools/BlazorShop.AI.Visual/skills/storefront-visual-plan/`, `storefront-visual-implement/`, and `storefront-visual-qa/`.
- Added thin pointer adapter docs under `tools/BlazorShop.AI.Visual/adapters/codex/` and `tools/BlazorShop.AI.Visual/adapters/claude/`.
- Did not add a copy/install script because it would add no value before install automation is requested; adapters explicitly point to canonical files.
- `rg -n "canonical|tools/BlazorShop.AI.Visual/skills|storefront-visual-plan|storefront-visual-implement|storefront-visual-qa" tools\BlazorShop.AI.Visual\adapters tools\BlazorShop.AI.Visual\skills` returned matches for canonical paths and all three skill names.

## Phase 4.10.5 - `storefront-visual-plan`

Goal: produce a deterministic visual implementation checklist from StorefrontBuilder artifacts.

Inputs:

- `docs/storefront-analysis/generation-plan.json`.
- `docs/storefront-analysis/generation-plan.yaml`.
- `docs/storefront-analysis/agent-task-package/manifest.json`.
- `docs/storefront-analysis/agent-task-package/*`.
- StorefrontBuilder metadata and handoff summary under `docs/storefront-analysis/`.

Tasks:

- [x] Define required read order in `storefront-visual-plan/SKILL.md`.
- [x] Require the skill to verify all expected inputs exist before planning.
- [x] Require the skill to hash the generation plan and task package manifest.
- [x] Require the skill to list every allowed output file from the task package.
- [x] Require the skill to map every visual slot/page from the generation plan to an implementation task or a blocked reason.
- [x] Require the skill to mark unsupported behavior as blocked instead of inventing transport or business logic.
- [x] Require the skill to emit:
  - [x] `docs/storefront-analysis/visual-plan.json`.
  - [x] `docs/storefront-analysis/visual-implementation-checklist.todo.md`.
  - [x] `docs/storefront-analysis/visual-plan-summary.md`.
- [x] Require schema validation of `visual-plan.json`.
- [x] Require stable output ordering:
  - [x] pages sorted by route priority or generation plan order.
  - [x] files sorted by normalized relative path.
  - [x] tasks grouped by page/slot/capability.
- [x] Add a negative example showing a blocked item when the handoff asks for unsupported behavior.

Checks:

```powershell
node tools\BlazorShop.AI.StorefrontBuilder\scripts\generate\write-agent-task-package.mjs --help
rg -n "visual-plan.json|visual-implementation-checklist.todo.md|blocked|generation-plan" tools\BlazorShop.AI.Visual\skills\storefront-visual-plan
```

DoD:

- [x] Visual planning can be reviewed before any visual edits happen.
- [x] Missing/unsupported requirements become explicit checklist blockers.

Phase 4.10.5 evidence:

- Expanded `tools/BlazorShop.AI.Visual/skills/storefront-visual-plan/SKILL.md` with read order, required input checks, hashing, allowed file listing, slot-to-task/blocker mapping, stable ordering, generated-project-local outputs, and schema validation requirements.
- Added negative example `tools/BlazorShop.AI.Visual/examples/visual-plan.blocked-unsupported-behavior.json`.
- Added `--help` support to `tools/BlazorShop.AI.StorefrontBuilder/scripts/generate/write-agent-task-package.mjs` so the documented check is artifact-independent.
- `node --check tools\BlazorShop.AI.StorefrontBuilder\scripts\generate\write-agent-task-package.mjs` passed.
- `node tools\BlazorShop.AI.StorefrontBuilder\scripts\generate\write-agent-task-package.mjs --help` printed usage and exited successfully.
- `node tools\BlazorShop.AI.Visual\scripts\validate-visual-examples.mjs` passed.
- `rg -n "visual-plan.json|visual-implementation-checklist.todo.md|blocked|generation-plan" tools\BlazorShop.AI.Visual\skills\storefront-visual-plan tools\BlazorShop.AI.Visual\examples\visual-plan.blocked-unsupported-behavior.json` returned matches.

## Phase 4.10.6 - Implementation Checkpoint Foundation

Goal: capture before/after evidence around agent edits so QA can prove exactly what changed.

Tasks:

- [x] Add a checkpoint contract that records:
  - [x] generated project root.
  - [x] operation ID.
  - [x] source visual plan hash.
  - [x] source checklist hash.
  - [x] pre-edit hashes for all allowed files.
  - [x] post-edit hashes for all changed files.
  - [x] changed file detection result.
  - [x] visual write recorder result path.
- [x] Add guidance that changed files must be detected from filesystem diff, not trusted only from an agent-supplied list.
- [x] Require the checkpoint to fail if changed files include paths outside the allowed task package.
- [x] Require the checkpoint to fail if plan/checklist hashes do not match the files used by the implementation skill.
- [x] Require checkpoint artifacts under `docs/storefront-analysis/visual-checkpoints/{operationId}/`.
- [x] Ensure checkpoint artifacts are generated-project-local and disposable.

Checks:

```powershell
rg -n "visual-checkpoints|before|after|hash|allowed files|changed files" tools\BlazorShop.AI.Visual
```

DoD:

- [x] QA can inspect exact visual changes without reading conversation history.
- [x] A stale checklist cannot silently drive implementation.

Phase 4.10.6 evidence:

- Added `tools/BlazorShop.AI.Visual/references/visual-checkpoint-contract.md`.
- The contract records generated project root, operation ID, source plan/checklist hashes, pre/post file hashes, changed file detection, diff summary, and StorefrontBuilder recorder path.
- The contract requires filesystem hash/path detection rather than trusting agent-supplied changed file lists.
- The contract fails on out-of-package changes, protected changes, plan/checklist hash drift, and missing recorder result.
- `rg -n "visual-checkpoints|before|after|hash|allowed files|changed files" tools\BlazorShop.AI.Visual` returned checkpoint contract and schema/example matches.

## Phase 4.10.7 - `storefront-visual-implement`

Goal: implement visual-only changes in generated-owned files and record them through StorefrontBuilder.

Tasks:

- [x] Define required read order in `storefront-visual-implement/SKILL.md`:
  - [x] shared references.
  - [x] generated project metadata.
  - [x] visual plan.
  - [x] visual checklist.
  - [x] task package manifest.
- [x] Require the implementation skill to stop on blockers from the visual plan.
- [x] Require edits to stay inside allowed generated visual files.
- [x] Require edits to preserve:
  - [x] `@page` absence in generated visual components.
  - [x] product purchase descriptors.
  - [x] same-origin browser action descriptors.
  - [x] route ownership.
  - [x] SEO ownership.
  - [x] account/cart/checkout contract behavior.
- [x] Require visual code to use existing generated project patterns before introducing new local abstractions.
- [x] Require 1:1 product gallery image frames where product gallery is touched.
- [x] Require responsive states for header, main nav, product detail, listing, cart/account/checkout visual shells when those files are in scope.
- [x] Require running `record-agent-visual-writes.mjs` after edits.
- [x] Require a build or focused generated project compile check after edits.
- [x] Emit `docs/storefront-analysis/visual-implementation-report.json`.
- [x] Emit `docs/storefront-analysis/visual-implementation-report.md`.

Checks:

```powershell
node tools\BlazorShop.AI.StorefrontBuilder\scripts\generate\record-agent-visual-writes.mjs --help
rg -n "@page|HttpClient|fetch\\(|/api/storefront/stores|CommerceNodeBaseUrl" <generated-project-root>
dotnet build <generated-project-csproj> --no-restore
```

DoD:

- [x] Implementation creates no transport, business, auth, SEO, or runtime drift.
- [x] StorefrontBuilder recorder accepts the changed files.
- [x] Generated project still builds.

Phase 4.10.7 evidence:

- Expanded `tools/BlazorShop.AI.Visual/skills/storefront-visual-implement/SKILL.md` with read order, blocker stop conditions, visual-only edit rules, descriptor preservation, product gallery/responsive requirements, recorder/build commands, forbidden drift scan, checkpoint output, and implementation report output.
- Added `--help` support to `tools/BlazorShop.AI.StorefrontBuilder/scripts/generate/record-agent-visual-writes.mjs` so the documented check is artifact-independent.
- `node --check tools\BlazorShop.AI.StorefrontBuilder\scripts\generate\record-agent-visual-writes.mjs` passed.
- `node tools\BlazorShop.AI.StorefrontBuilder\scripts\generate\record-agent-visual-writes.mjs --help` printed usage and exited successfully.
- `rg -n "visual-implementation-report|record-agent-visual-writes|dotnet build|@page|same-origin|product gallery|responsive|checkpoint" tools\BlazorShop.AI.Visual\skills\storefront-visual-implement` returned matches.

## Phase 4.10.8 - Visual Capture Evidence

Goal: make browser evidence available to the QA skill in a stable location.

Tasks:

- [x] Reuse `run-visual-qa.mjs` for browser captures instead of creating a second capture tool.
- [x] Ensure the visual QA output includes or can link to:
  - [x] desktop screenshots.
  - [x] tablet screenshots.
  - [x] mobile screenshots.
  - [x] per-page status.
  - [x] console/network failure summary.
  - [x] CSS asset status.
  - [x] broken image summary.
  - [x] overflow/blank/placeholder findings.
- [x] Add an optional `--evidence-root` or documented screenshot root convention if current output paths are not stable enough.
- [x] Keep capture evidence read-only for the QA skill.
- [x] Do not let capture automatically repair visual files.

Checks:

```powershell
node tools\BlazorShop.AI.StorefrontBuilder\scripts\qa\run-visual-qa.mjs --help
```

DoD:

- [x] QA can review real browser artifacts, not only compile output.
- [x] Capture and repair remain separate steps.

Phase 4.10.8 evidence:

- Reused `tools/BlazorShop.AI.StorefrontBuilder/scripts/qa/run-visual-qa.mjs`; no second capture tool was added.
- Added artifact-independent `--help` support to `run-visual-qa.mjs`.
- Added browser event capture for console warnings/errors, page errors, and failed requests, reported under `Browser Event Summary`.
- Confirmed existing visual QA captures desktop, tablet, and mobile screenshots, per-page route captures, CSS responses, broken images, overflow, blank body, required slot visibility, product gallery shape, and placeholder findings.
- Documented `--screenshot-root <path>` as the stable evidence root convention in `tools/BlazorShop.AI.Visual/references/browser-qa-rubric.md`.
- `node --check tools\BlazorShop.AI.StorefrontBuilder\scripts\qa\run-visual-qa.mjs` passed.
- `node tools\BlazorShop.AI.StorefrontBuilder\scripts\qa\run-visual-qa.mjs --help` printed usage and exited successfully.
- `rg -n "screenshot-root|Browser Event Summary|console|network|desktop|tablet|mobile|broken image|overflow|placeholder|repair" tools\BlazorShop.AI.StorefrontBuilder\scripts\qa\run-visual-qa.mjs tools\BlazorShop.AI.Visual\references\browser-qa-rubric.md` returned matches.

## Phase 4.10.9 - `storefront-visual-qa`

Goal: independently review generated visual output with browser evidence and optionally produce bounded visual repairs.

Tasks:

- [x] Define required read order in `storefront-visual-qa/SKILL.md`:
  - [x] shared references.
  - [x] visual plan.
  - [x] implementation checklist.
  - [x] implementation report.
  - [x] checkpoint.
  - [x] browser evidence/report from `run-visual-qa.mjs`.
- [x] Require QA to inspect rendered screenshots for:
  - [x] blank page.
  - [x] overlapping text.
  - [x] cropped controls.
  - [x] mobile navigation availability.
  - [x] visible cart/account/checkout entry points where applicable.
  - [x] product gallery 1:1 presentation.
  - [x] product price/action readability.
  - [x] broken image placeholders.
  - [x] visual hierarchy and ecommerce scanability.
  - [x] missing required visual slots from the plan.
- [x] Require QA findings to be written to `docs/storefront-analysis/visual-qa-report.json`.
- [x] Require QA findings to be written to `docs/storefront-analysis/visual-qa-report.md`.
- [x] Allow repair only when:
  - [x] the failing file is generated-owned.
  - [x] the file is in the allowed task package.
  - [x] the fix is visual-only.
  - [x] the issue is reproducible from browser evidence.
- [x] Cap repair attempts at a configured small number, default `2` or `3`.
- [x] Require `record-agent-visual-writes.mjs` after every repair pass.
- [x] Require rerunning `run-visual-qa.mjs` after repair.
- [x] Require unresolved issues to remain in the report with severity and next action.

Checks:

```powershell
rg -n "visual-qa-report|repair attempt|browser evidence|record-agent-visual-writes" tools\BlazorShop.AI.Visual\skills\storefront-visual-qa
```

DoD:

- [x] QA cannot pass without browser evidence.
- [x] QA cannot repair outside generated visual ownership.
- [x] Remaining issues are explicit and release-decision friendly.

Phase 4.10.9 evidence:

- Expanded `tools/BlazorShop.AI.Visual/skills/storefront-visual-qa/SKILL.md` with required read order across shared references, task package, plan, checklist, implementation report, checkpoint, and browser evidence.
- Required screenshot inspection for blank pages, overlapping text, cropped controls, mobile navigation, ecommerce entry points, product gallery shape, price/action readability, broken images, hierarchy, scanability, and missing visual slots.
- Required generated-project-local `visual-qa-report.json` and `visual-qa-report.md` outputs.
- Restricted repair to reproducible browser-evidence failures in generated-owned task-package files, with visual-only scope and a default cap of `2` attempts.
- Required `record-agent-visual-writes.mjs` and `run-visual-qa.mjs` after every repair pass.
- `rg -n "visual-qa-report|repair attempt|browser evidence|record-agent-visual-writes" tools\BlazorShop.AI.Visual\skills\storefront-visual-qa` returned matches.

## Phase 4.10.10 - Repair Policy Alignment

Goal: keep the existing mechanical repair helper useful without letting it become an uncontrolled visual generator.

Tasks:

- [ ] Document that `repair-visual-generation.mjs` is a bounded helper, not the canonical visual QA skill.
- [ ] Confirm it still rejects:
  - [ ] route additions.
  - [ ] direct API transport.
  - [ ] business logic changes.
  - [ ] auth/session changes.
  - [ ] SEO changes.
  - [ ] protected descriptor edits.
- [ ] Align visual QA skill wording with the existing helper's actual capabilities.
- [ ] If needed, add report fields showing whether a fix came from:
  - [ ] manual agent edit.
  - [ ] mechanical repair helper.
  - [ ] no repair attempted.
- [ ] Do not expand repair into business or platform behavior.

Checks:

```powershell
node tools\BlazorShop.AI.StorefrontBuilder\scripts\qa\repair-visual-generation.mjs --help
rg -n "HttpClient|/api/storefront/stores|@page|auth|seo|descriptor" tools\BlazorShop.AI.StorefrontBuilder\scripts\qa\repair-visual-generation.mjs
```

DoD:

- [ ] Repair ownership is explicit.
- [ ] Repair cannot be mistaken for final visual QA.

## Phase 4.10.11 - Target-Specific Phase 4 MVP Gate

Goal: add one local command that proves a generated storefront has enough evidence for MVP visual closure.

Tasks:

- [ ] Add `scripts/qa/run-storefront-phase4-mvp-gate.ps1`.
- [ ] Inputs:
  - [ ] generated project root.
  - [ ] optional fixture root.
  - [ ] optional handoff root.
  - [ ] optional screenshot/evidence root.
  - [ ] optional max repair attempts.
  - [ ] switch to skip repair but not skip QA.
- [ ] Gate steps:
  - [ ] validate generated project metadata.
  - [ ] validate generation plan presence.
  - [ ] validate agent task package presence.
  - [ ] validate visual plan/checklist/report schemas when present.
  - [ ] run StorefrontBuilder handoff boundary validation.
  - [ ] run generated project restore/build.
  - [ ] run visual write ownership validation.
  - [ ] run `run-visual-qa.mjs`.
  - [ ] run bounded repair only when configured.
  - [ ] rerun visual QA after repair.
  - [ ] run regeneration `-WhatIf` or no-op ownership check where supported.
  - [ ] write `phase4-mvp-gate-report.json`.
  - [ ] write `phase4-mvp-gate-report.md`.
- [ ] Failure output must include:
  - [ ] problem.
  - [ ] likely cause.
  - [ ] exact command to rerun.
  - [ ] report path.
  - [ ] evidence path.
- [ ] Do not invoke GitHub Actions.

Checks:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\qa\run-storefront-phase4-mvp-gate.ps1 -Help
```

DoD:

- [ ] One local command can prove the visual MVP workflow for a generated storefront.
- [ ] The command fails with actionable evidence, not only a shell exit code.

## Phase 4.10.12 - End-To-End Pilot

Goal: run the workflow on one generated storefront and record closure evidence.

Tasks:

- [ ] Choose one portable handoff fixture or real reviewed handoff package.
- [ ] Run StorefrontBuilder:
  - [ ] `preflight-only`.
  - [ ] `plan-only`.
  - [ ] `generate` or `full`.
- [ ] Run `storefront-visual-plan`.
- [ ] Review generated checklist for completeness.
- [ ] Run `storefront-visual-implement` on a small but real visual scope.
- [ ] Record visual writes.
- [ ] Build generated project.
- [ ] Run visual capture/QA.
- [ ] Run bounded repair if QA finds generated-owned visual defects.
- [ ] Rerun QA.
- [ ] Run the target MVP gate.
- [ ] Store evidence under generated project `docs/storefront-analysis/` and ignored `obj/storefront-builder/reports/`.
- [ ] Record the exact commands and result in a tracked pilot summary only if it does not include machine-specific absolute paths or transient screenshots.

Checks:

```powershell
.\tools\BlazorShop.AI.StorefrontBuilder\build-storefront.ps1 -Mode preflight-only -HandoffRoot <portable-handoff-root> -HandoffSchemaRoot tools\BlazorShop.AI.StorefrontReverseEngineering\Schemas
.\tools\BlazorShop.AI.StorefrontBuilder\build-storefront.ps1 -Mode plan-only -Name Demo -StoreKey sample -HandoffRoot <portable-handoff-root> -HandoffSchemaRoot tools\BlazorShop.AI.StorefrontReverseEngineering\Schemas
.\tools\BlazorShop.AI.StorefrontBuilder\build-storefront.ps1 -Mode generate -Name Demo -StoreKey sample -OutputRoot obj/storefront-builder/generated -HandoffRoot <portable-handoff-root> -HandoffSchemaRoot tools\BlazorShop.AI.StorefrontReverseEngineering\Schemas -Force
.\scripts\qa\run-storefront-phase4-mvp-gate.ps1 -GeneratedProjectRoot <generated-project-root>
```

DoD:

- [ ] One complete plan -> implement -> QA -> repair -> gate loop has passed locally.
- [ ] Any failures are captured as follow-up TODOs instead of silently ignored.

## Phase 4.10.13 - Final Closure Gate

Goal: create the final local closure command for Phase 4.10 after the pilot is stable.

Tasks:

- [ ] Add `scripts/qa/run-storefront-phase4-final-closure-gate.ps1`.
- [ ] Require a clean working tree at start.
- [ ] Require tested `HEAD` equality at end.
- [ ] Run visual workspace static checks:
  - [ ] no `.csproj` under `tools/BlazorShop.AI.Visual`.
  - [ ] no runtime project references.
  - [ ] canonical skill paths exist.
  - [ ] adapters point to canonical skill paths.
  - [ ] schemas exist.
  - [ ] examples validate.
- [ ] Run relevant StorefrontBuilder static checks:
  - [ ] generated write recorder available.
  - [ ] visual QA script available.
  - [ ] repair helper available.
  - [ ] handoff boundary validator available.
- [ ] Run StorefrontBuilder generated proof at the minimum level needed for the phase.
- [ ] Run StorefrontBuilder regeneration/no-op ownership gate.
- [ ] Run the Phase 4 MVP gate against the chosen pilot generated storefront or fixture.
- [ ] Write final closure report under `obj/storefront-builder/reports/`.
- [ ] Track only human-readable closure summary if needed; keep transient screenshots/reports ignored.
- [ ] Do not require GitHub Actions.

Checks:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\qa\run-storefront-phase4-final-closure-gate.ps1
```

DoD:

- [ ] Final gate passes locally on clean `HEAD`.
- [ ] Closure evidence does not require Actions.
- [ ] Failure output is actionable.

## Phase 4.10.14 - Documentation And Agent Guide Updates

Goal: make the new workflow visible without making historical plans the source of truth.

Tasks:

- [ ] Update `docs/visual-reverse-engineering-skill/README.md`:
  - [ ] add this plan to historical plans.
  - [ ] document visual skill workflow.
  - [ ] link target MVP/final gates after they exist.
- [ ] Update `docs/visual-reverse-engineering-skill/reference.md`:
  - [ ] add skill paths.
  - [ ] add report artifacts.
  - [ ] add gate commands.
- [ ] Update `docs/visual-reverse-engineering-skill/how-to-generate-and-validate.md`:
  - [ ] add plan -> implement -> QA sequence.
  - [ ] keep StorefrontBuilder generation commands as the source of project creation.
- [ ] Update `docs/architecture/11-storefront-builder.md`:
  - [ ] describe `tools/BlazorShop.AI.Visual` as a dev-time skill/report workspace.
  - [ ] state it is not a generator/runtime package.
  - [ ] preserve StorefrontBuilder and ReverseEngineering ownership.
- [ ] Update `docs/agents/storefront-builder.md`:
  - [ ] when to use visual skills.
  - [ ] what files they may read.
  - [ ] what files they may edit.
  - [ ] which gates must pass before closure.
- [ ] Update `AGENTS.md` only if the workflow becomes part of standard agent required reading.

Checks:

```powershell
rg -n "BlazorShop.AI.Visual|storefront-visual-plan|storefront-visual-implement|storefront-visual-qa|run-storefront-phase4-mvp-gate" docs\visual-reverse-engineering-skill docs\architecture docs\agents AGENTS.md
```

DoD:

- [ ] Agent workflow can be followed from docs without conversation context.
- [ ] Architecture docs remain the source of truth over historical plans.

## Required QA Gates For Implementation Commits

Run focused checks per phase instead of waiting until the final gate:

```powershell
rg -n "<Project Sdk=|ProjectReference|PackageReference|CommerceNode|ControlPlane|Storefront.V2" tools\BlazorShop.AI.Visual -S
rg -n "tools/BlazorShop.AI.Visual/skills|storefront-visual-plan|storefront-visual-implement|storefront-visual-qa" tools\BlazorShop.AI.Visual docs\visual-reverse-engineering-skill
node tools\BlazorShop.AI.StorefrontBuilder\scripts\generate\record-agent-visual-writes.mjs --help
node tools\BlazorShop.AI.StorefrontBuilder\scripts\qa\run-visual-qa.mjs --help
node tools\BlazorShop.AI.StorefrontBuilder\scripts\qa\repair-visual-generation.mjs --help
```

After wrapper scripts exist:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\qa\run-storefront-phase4-mvp-gate.ps1 -Help
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\qa\run-storefront-phase4-final-closure-gate.ps1
```

When a generated project is available:

```powershell
.\scripts\qa\run-storefront-builder-generated-proof.ps1 -ProofLevel Structure
.\scripts\qa\run-storefront-builder-regeneration-gate.ps1
.\scripts\qa\run-storefront-builder-isolation-gate.ps1
.\scripts\qa\run-storefront-phase4-mvp-gate.ps1 -GeneratedProjectRoot <generated-project-root>
```

Use `FoundationFunctionalFast` or the full fixture proof when the implementation touches generated browser actions, same-origin BFF behavior, or commerce-regression evidence.

## Release Definition Of Done

- [ ] `tools/BlazorShop.AI.Visual` exists and has no `.csproj`.
- [ ] `tools/BlazorShop.AI.Visual` has no production runtime references.
- [ ] Three canonical skills exist:
  - [ ] `storefront-visual-plan`.
  - [ ] `storefront-visual-implement`.
  - [ ] `storefront-visual-qa`.
- [ ] Host adapters point to canonical skill files instead of duplicating them.
- [ ] Shared references define boundaries, inputs, ownership, Razor rules, CSS rules, and browser QA rubric.
- [ ] Visual output schemas exist and validate example artifacts.
- [ ] Visual plan output is deterministic and reviewable before edits.
- [ ] Visual implementation reports exact changed files and StorefrontBuilder recorder result.
- [ ] Visual QA uses browser evidence and cannot pass from compile-only smoke output.
- [ ] Repair is bounded and generated-visual-only.
- [ ] Target-specific MVP gate exists and runs locally.
- [ ] Final closure gate exists and does not require GitHub Actions.
- [ ] Documentation explains how StorefrontBuilder, ReverseEngineering, and Visual skills relate.
- [ ] A pilot generated storefront proves the end-to-end flow.

## Deferred Scope

- AgentRunner orchestration.
- Multi-agent scheduler.
- Model provider integration.
- Visual diff scoring as a hard gate.
- Automatic asset replacement or image generation.
- Functional commerce test replacement.
- Production deployment.
- Promotion workflow into a long-lived generated storefront repository.
- Public plugin packaging for the visual skills.

## Risk Register

| Risk | Impact | Mitigation |
| --- | --- | --- |
| Visual workspace becomes a second generator | Conflicting ownership with StorefrontBuilder | Keep `AI.Visual` script/schema/skill-only and require StorefrontBuilder artifacts as input. |
| Agents edit protected behavior | Checkout/account/cart/payment regressions | Require task package allowed files, checkpoint diff, and `record-agent-visual-writes.mjs`. |
| QA becomes smoke-only | Real browser layout defects slip through | Require `run-visual-qa.mjs` evidence and screenshots in the QA report. |
| Skill adapters drift from canonical instructions | Agents follow stale rules | Make adapters thin pointers and add static checks. |
| Reports are prose-only | Closure cannot be automated | Add JSON schemas for plan, implementation, QA, and gate reports. |
| Final closure depends on disabled CI | Phase cannot close during development | Use local gates as authoritative; document GitHub Actions as out of scope. |
| Pixel diff is introduced too early | Noisy failures block useful progress | Defer strict visual fidelity scoring until baseline management exists. |

## Handoff To Implementation

Start with Phase 4.10.0 and Phase 4.10.1 only. Do not build all skills in one commit. The first implementation commit should prove the workspace is inert and has no production coupling. Subsequent commits should add schemas, skill instructions, then wrapper gates in that order.

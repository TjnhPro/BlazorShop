# Visual Reverse Engineering Closure Integrity.todo

Status: In Progress
Owner: Storefront Platform
Created: 2026-08-02
Target folder: `docs/visual-reverse-engineering-skill`
Depends on: current StorefrontBuilder and ReverseEngineering closure work already merged
Primary goal: close the remaining developer-facing and portability gaps without reopening the architecture, without depending on GitHub Actions, and without broadening scope beyond the current closure blockers.

## Why This File Exists

The current codebase is close to final closure, but a few small proof gaps still matter:

- `-WhatIf` computes a real plan but the report is only easy to recover through hidden candidate artifacts.
- `generatorVersion` is split across more than one source.
- Portable handoff validation still needs a cleaner canonical contract for copied packages and readiness alignment.
- Slot validation is already source-aware, but the remaining proof needs tighter regression coverage and clearer terminology.

GitHub Actions evidence is intentionally out of scope for this cleanup round because Actions are disabled during active development.

## Current Codebase Findings

- `tools/BlazorShop.AI.StorefrontBuilder/regenerate-storefront.ps1` writes a useful plan, but the report is not surfaced in a stable developer-visible location by default.
- The current `-WhatIf` path still depends on temporary candidate artifacts unless debug retention is enabled.
- StorefrontBuilder version provenance is split between PowerShell and Node-facing generation code.
- `tools/BlazorShop.AI.StorefrontReverseEngineering/Analysis/Handoff/AgentHandoffAssembler.cs` still treats `analysis/agent-handoff/manifest.json` and `analysis/agent-handoff/handoff-readiness.json` as special bootstrap artifacts.
- `tools/BlazorShop.AI.StorefrontReverseEngineering/Analysis/Handoff/PortableHandoffValidator.cs` already validates portable copied packages, but the canonical membership and readiness alignment rules can be made clearer.
- `tools/BlazorShop.AI.StorefrontReverseEngineering/Analysis/Blueprint/PageCompositionSlotValidator.cs` is already source-aware; the remaining risk is not the old `Dictionary<string,int>` model, but whether the current authoritative-slot proof is covered by focused tests and docs.
- `tools/BlazorShop.AI.StorefrontReverseEngineering/tests/BlazorShop.AI.StorefrontReverseEngineering.Tests/Phase3EFinalClosureGateTests.cs` still encodes the final gate through script assertions and needs to keep proving the same end state after any plan/report changes.

## Locked Decisions

- Do not reopen the core StorefrontBuilder or ReverseEngineering architecture.
- Do not require GitHub Actions to close this phase.
- Keep candidate cleanup enabled by default.
- Keep the slot validator source-aware; do not regress to heuristic-only slot inference.
- Keep this work local and deterministic.
- Prefer one canonical version source for `generatorVersion`.

## Out Of Scope

- New StorefrontBuilder generation features.
- Storefront V2 runtime behavior changes.
- Commerce Node or Control Plane feature work.
- GitHub Actions recovery or CI proof.
- Rewriting the slot validator model from scratch.

## Phase Order

1. Phase 3E.0 - Baseline And Cleanup Scope Lock
2. Phase 3E.1 - Stable `-WhatIf` Output And Report Location
3. Phase 3E.2 - Unified Generator Version Source
4. Phase 3E.3 - Portable Handoff Canonical Integrity
5. Phase 3E.4 - Slot Proof Regression Coverage
6. Phase 3E.5 - Documentation And Local Closure Evidence

Implement in this order. Do not skip directly to docs or tests before the report/version contracts are fixed.

## Phase 3E.0 - Baseline And Cleanup Scope Lock

Goal: confirm the exact closure blockers and lock the work to local deterministic proof only.

Tasks:

- [x] Record the current blocker list in this plan file before any code changes.
- [x] Confirm the hidden candidate-retention env var remains debug-only.
- [x] Confirm GitHub Actions are intentionally excluded from this closure round.
- [x] Confirm the current slot validator is already source-aware and should not be rewritten to the old counter model.
- [x] Identify the exact scripts and validators touched by this cleanup:
  - [x] `tools/BlazorShop.AI.StorefrontBuilder/regenerate-storefront.ps1`
  - [x] `tools/BlazorShop.AI.StorefrontBuilder/scripts/generate/StorefrontBuilderProjectSafety.ps1`
  - [x] `tools/BlazorShop.AI.StorefrontBuilder/scripts/generate/generated-file-manifest.mjs`
  - [x] `tools/BlazorShop.AI.StorefrontReverseEngineering/Analysis/Handoff/AgentHandoffAssembler.cs`
  - [x] `tools/BlazorShop.AI.StorefrontReverseEngineering/Analysis/Handoff/PortableHandoffValidator.cs`
  - [x] `tools/BlazorShop.AI.StorefrontReverseEngineering/Analysis/Handoff/AgentHandoffReadinessValidator.cs`
  - [x] `tools/BlazorShop.AI.StorefrontReverseEngineering/Analysis/Blueprint/PageCompositionSlotValidator.cs`

Checks:

- [x] `rg -n "WhatIfReportPath|WhatIf completed|generatorVersion|handoff-readiness|duplicate-non-repeatable-slot|unapproved-extra-section" tools scripts docs`
- [x] `git status --short`

Done when:

- [x] The plan is aligned to the current codebase instead of the older review assumptions.

Phase 3E.0 evidence:

- Current blocker list is closure-integrity only: stable `-WhatIf` developer output, single generator version provenance, portable handoff canonical integrity/readiness alignment, and focused source-aware slot regression proof.
- `SFB_KEEP_REGENERATION_CANDIDATE_ARTIFACTS=1` is still present only as a debug/preserved-candidate path in `regenerate-storefront.ps1` and regeneration safety tests; normal `-WhatIf` output uses a stable report path.
- GitHub Actions remains out of scope while disabled; local deterministic commands are the closure evidence.
- `PageCompositionSlotValidator` already uses source-aware `SlotObservationSource` records and suggestion-only observations, so this plan must strengthen tests/docs rather than rewrite the validator to a counter-only model.
- Scope correction: StorefrontBuilder generation helpers live under `tools/BlazorShop.AI.StorefrontBuilder/scripts/generate/`, not repo-root `scripts/generate/`.
- Baseline status before this phase commit: only this new plan file was untracked.

## Phase 3E.1 - Stable `-WhatIf` Output And Report Location

Goal: make `-WhatIf` useful without hidden artifacts.

Tasks:

- [ ] Define one stable report path outside the temporary candidate tree.
- [ ] Prefer a path under the builder output root, not inside the generated project.
- [ ] Keep candidate cleanup on by default.
- [ ] Copy or write the computed plan report to the stable location before cleanup runs.
- [ ] Print the stable report path to console.
- [ ] Print a short summary of the plan:
  - [ ] create count
  - [ ] update count
  - [ ] metadata update count
  - [ ] conflict count
  - [ ] obsolete count
  - [ ] protected/user-owned skip count
- [ ] Print meaningful per-file plan lines only, not noisy no-op spam.
- [ ] Add `-WhatIfReportPath` support if the current default report location still needs an override.
- [ ] Validate `-WhatIfReportPath`:
  - [ ] reject paths under the generated project;
  - [ ] reject traversal;
  - [ ] reject unsafe roots;
  - [ ] create parent folders safely.
- [ ] Keep `SFB_KEEP_REGENERATION_CANDIDATE_ARTIFACTS=1` as debug-only, not as the normal way to read the report.

Implementation notes:

- Reuse the existing report writer rather than inventing a second report format.
- If a conflict exists, the console should tell the developer what to do next instead of only saying the plan finished.
- The normal developer path must not depend on a hidden candidate folder or a special env var.

QA:

```powershell
.\tools\BlazorShop.AI.StorefrontBuilder\regenerate-storefront.ps1 `
  -ProjectRoot artifacts/storefront-builder/generated/BlazorShop.Storefront.GeneratedProof `
  -Scope all `
  -WhatIf
```

Expected verification:

- [ ] Console prints the report path.
- [ ] Console prints a readable action summary.
- [ ] Stable report file exists after the command exits.
- [ ] Generated target tree is unchanged.
- [ ] Candidate cleanup still runs by default.

Done when:

- [ ] A developer can review the plan without knowing internal cleanup switches.

## Phase 3E.2 - Unified Generator Version Source

Goal: make `generatorVersion` come from one place only.

Tasks:

- [ ] Add one shared version source file for StorefrontBuilder generation.
- [ ] Read that version from both PowerShell and Node-side generation paths.
- [ ] Normalize the naming so both outputs use the same `generatorVersion` semantics.
- [ ] Update any tests or fixtures that still hard-code stale generator version values.
- [ ] Add a guard test that fails if the version source drifts between script families.
- [ ] Update docs to point at the same source of truth.

Preferred shape:

```json
{
  "generatorVersion": "2.5.0"
}
```

Implementation notes:

- Keep the reader dependency-light.
- Do not infer the version from package versioning.
- Do not keep two different `generatorVersion` values for the same generated artifact family.

QA:

```powershell
.\scripts\qa\run-storefront-builder-regeneration-gate.ps1
dotnet test BlazorShop.Tests.V2\BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~StorefrontBuilder"
```

Done when:

- [ ] Generated metadata and generated-file manifests use one clear provenance source.

## Phase 3E.3 - Portable Handoff Canonical Integrity

Goal: keep copied handoff packages valid without the source project, while avoiding false coupling.

Tasks:

- [ ] Keep the portable package contract explicit about required artifacts and schema kinds.
- [ ] Make canonical membership checks part of `PortableHandoffValidator`, not just source-side assembly.
- [ ] Keep the portable validator focused on the copied package root.
- [ ] Decide and enforce one readiness invariant:
  - [ ] either include readiness in the integrity chain directly,
  - [ ] or assert that manifest readiness and handoff readiness agree exactly.
- [ ] Preserve deterministic package hash behavior across copied package locations.
- [ ] Keep diagnostic-only provenance out of the consumer contract.
- [ ] Keep generated-target-path and external-url references in their own reference category.
- [ ] Make the validator failure messages tell the developer what to copy or repair.

Implementation notes:

- Do not let copied-package validation fall back to the original project root.
- Do not blur consumer dependency, diagnostic provenance, and generated target paths into one string scan.
- Keep `AgentHandoffAssembler` and `PortableHandoffValidator` aligned on the same canonical contract set.

QA:

```powershell
dotnet test tools\BlazorShop.AI.StorefrontReverseEngineering\tests\BlazorShop.AI.StorefrontReverseEngineering.Tests\BlazorShop.AI.StorefrontReverseEngineering.Tests.csproj --no-restore --filter "FullyQualifiedName~PortableHandoff"
```

Expected verification:

- [ ] Copied package validates independently.
- [ ] Mutating readiness fails clearly.
- [ ] Missing canonical artifact or schema fails clearly.
- [ ] Package hash remains stable across copy.

Done when:

- [ ] Portable closure no longer depends on hidden source-project state.

## Phase 3E.4 - Slot Proof Regression Coverage

Goal: keep the current authoritative slot model honest with focused regressions.

Tasks:

- [ ] Preserve the current source-aware slot observation model.
- [ ] Update any stale comments or docs that still describe the old counter-only model.
- [ ] Add or refresh regression tests for:
  - [ ] `duplicate-non-repeatable-slot`
  - [ ] `unapproved-extra-section`
  - [ ] reviewed mapping missing from slot proof
  - [ ] orphan reviewed mapping if the validator needs that explicit blocker
- [ ] Keep role/text inference as suggestion-only, not authoritative.
- [ ] Make sure the tests prove the actual validator behavior, not just fixture string matching.

Implementation notes:

- The current validator already uses source-aware observations; this phase should strengthen proof, not redesign it.
- If a new blocker code is added, document the exact reason and keep it narrowly scoped.

QA:

```powershell
dotnet test tools\BlazorShop.AI.StorefrontReverseEngineering\tests\BlazorShop.AI.StorefrontReverseEngineering.Tests\BlazorShop.AI.StorefrontReverseEngineering.Tests.csproj --no-restore --filter "FullyQualifiedName~PageCompositionSlotValidator|FullyQualifiedName~Phase3EFinalClosureGateTests"
```

Done when:

- [ ] Slot proof still blocks the wrong graph and still accepts the reviewed graph.

## Phase 3E.5 - Documentation And Local Closure Evidence

Goal: align docs with the final local behavior and close the phase without CI dependence.

Tasks:

- [ ] Update the visual reverse engineering docs to describe the stable `-WhatIf` report path.
- [ ] Update docs to explain the unified generator version source.
- [ ] Update any closure docs that still tell people to rely on hidden candidate artifacts for normal usage.
- [ ] Record that GitHub Actions is intentionally out of scope while disabled during development.
- [ ] Capture local command summaries for the final closure note.
- [ ] Confirm the final plan no longer mentions a temporary-only workaround as the normal path.

Local closure evidence:

```powershell
.\scripts\qa\run-storefront-client-regeneration-gate.ps1
.\scripts\qa\run-storefront-builder-regeneration-gate.ps1
.\scripts\qa\run-storefront-builder-generated-proof.ps1 -ProofLevel Structure
.\scripts\qa\run-storefront-builder-generated-proof.ps1 -ProofLevel FoundationFunctionalFast
dotnet test BlazorShop.Tests.V2\BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~StorefrontBuilder"
```

Optional:

```powershell
.\scripts\qa\run-storefront-builder-full-proof-with-fixture.ps1
```

Done when:

- [ ] The docs, the plan output, and the local proof all describe the same closure behavior.

## Final Closure Gate

The phase is complete only when all of the following are true:

- [ ] Normal `-WhatIf` output is readable without debug env vars.
- [ ] The stable report location is documented.
- [ ] One generator version source is used everywhere.
- [ ] Portable handoff validation works on a copied package.
- [ ] Slot proof still blocks incorrect mappings and extra sections.
- [ ] Docs match the runtime behavior.
- [ ] GitHub Actions remains intentionally excluded while disabled.

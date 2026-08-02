# Visual Reverse Engineering Closure Integrity.todo

Status: Complete
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

## Baseline Codebase Findings Before Phase 3E.0

- At baseline, `tools/BlazorShop.AI.StorefrontBuilder/regenerate-storefront.ps1` wrote a useful plan, but the report was not surfaced in a stable developer-visible location by default.
- At baseline, the normal `-WhatIf` review path still depended on temporary candidate artifacts unless debug retention was enabled.
- At baseline, StorefrontBuilder version provenance was split between PowerShell and Node-facing generation code.
- At baseline, `tools/BlazorShop.AI.StorefrontReverseEngineering/Analysis/Handoff/AgentHandoffAssembler.cs` still treated `analysis/agent-handoff/manifest.json` and `analysis/agent-handoff/handoff-readiness.json` as special bootstrap artifacts.
- At baseline, `tools/BlazorShop.AI.StorefrontReverseEngineering/Analysis/Handoff/PortableHandoffValidator.cs` already validated portable copied packages, but the canonical membership and readiness alignment rules needed clearer enforcement.
- At baseline, `tools/BlazorShop.AI.StorefrontReverseEngineering/Analysis/Blueprint/PageCompositionSlotValidator.cs` was already source-aware; the remaining risk was not the old `Dictionary<string,int>` model, but whether the current authoritative-slot proof was covered by focused tests and docs.
- At baseline, `tools/BlazorShop.AI.StorefrontReverseEngineering/tests/BlazorShop.AI.StorefrontReverseEngineering.Tests/Phase3EFinalClosureGateTests.cs` encoded the final gate through script assertions and needed to keep proving the same end state after any plan/report changes.

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

- [x] Define one stable report path outside the temporary candidate tree.
- [x] Prefer a path under the builder output root, not inside the generated project.
- [x] Keep candidate cleanup on by default.
- [x] Copy or write the computed plan report to the stable location before cleanup runs.
- [x] Print the stable report path to console.
- [x] Print a short summary of the plan:
  - [x] create count
  - [x] update count
  - [x] metadata update count
  - [x] conflict count
  - [x] obsolete count
  - [x] protected/user-owned skip count
- [x] Print meaningful per-file plan lines only, not noisy no-op spam.
- [x] Add `-WhatIfReportPath` support if the current default report location still needs an override.
- [x] Validate `-WhatIfReportPath`:
  - [x] reject paths under the generated project;
  - [x] reject traversal;
  - [x] reject unsafe roots;
  - [x] create parent folders safely.
- [x] Keep `SFB_KEEP_REGENERATION_CANDIDATE_ARTIFACTS=1` as debug-only, not as the normal way to read the report.

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

- [x] Console prints the report path.
- [x] Console prints a readable action summary.
- [x] Stable report file exists after the command exits.
- [x] Generated target tree is unchanged.
- [x] Candidate cleanup still runs by default.

Done when:

- [x] A developer can review the plan without knowing internal cleanup switches.

Phase 3E.1 evidence:

- Existing implementation in `regenerate-storefront.ps1` resolves default `-WhatIf` reports to `{OutputRoot}/.regeneration-reports/{ProjectName}-{operationId}.md`, copies the candidate `docs/storefront-analysis/regeneration-report.md` there before cleanup, and prints `WhatIf report: <path>`.
- Console summary includes `create`, `update`, `platformMetadataUpdate`, `conflict`, `obsolete`, and `protectedOrUserOwnedSkip`; per-file console output is filtered by `Test-MeaningfulWhatIfAction`.
- `Resolve-WhatIfReportPath` rejects paths under the generated target, rejects paths outside approved roots, rejects missing parent/file names, and creates approved parent directories safely.
- QA command: `.\tools\BlazorShop.AI.StorefrontBuilder\regenerate-storefront.ps1 -ProjectRoot artifacts/storefront-builder/generated/BlazorShop.Storefront.GeneratedProof -Scope all -WhatIf`.
- Result: passed; report path printed as `artifacts/storefront-builder/generated/.regeneration-reports/BlazorShop.Storefront.GeneratedProof-c20dacd02c2442309997747f9aacfe55.md`, summary printed `create=0; update=0; platformMetadataUpdate=0; conflict=0; obsolete=0; protectedOrUserOwnedSkip=16`, stable report existed after exit, and `.regeneration-candidate/c20dacd02c2442309997747f9aacfe55` was removed.
- `git status --short` was clean after the QA command; generated proof output remains ignored.

## Phase 3E.2 - Unified Generator Version Source

Goal: make `generatorVersion` come from one place only.

Tasks:

- [x] Add one shared version source file for StorefrontBuilder generation.
- [x] Read that version from both PowerShell and Node-side generation paths.
- [x] Normalize the naming so both outputs use the same `generatorVersion` semantics.
- [x] Update any tests or fixtures that still hard-code stale generator version values.
- [x] Add a guard test that fails if the version source drifts between script families.
- [x] Update docs to point at the same source of truth.

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

- [x] Generated metadata and generated-file manifests use one clear provenance source.

Phase 3E.2 evidence:

- Shared source: `tools/BlazorShop.AI.StorefrontBuilder/version.json` contains `generatorVersion`.
- PowerShell generation reads the shared source through `Read-StorefrontBuilderGeneratorVersion` in `tools/BlazorShop.AI.StorefrontBuilder/scripts/generate/StorefrontBuilderProjectSafety.ps1`; Node manifest generation reads the same file through `storefront-builder-version.mjs`.
- `generated-file-manifest.mjs` imports `generatorVersion` from the Node reader, while `new-storefront-project.ps1` writes metadata from `$script:StorefrontBuilderGeneratorVersion`.
- Guard coverage exists in `StorefrontBuilderQaRegenerationTests.StorefrontBuilderGeneratorVersion_UsesSingleSource` and generation safety tests assert metadata/manifest version agreement.
- Docs already point to `tools/BlazorShop.AI.StorefrontBuilder/version.json` in `docs/architecture/11-storefront-builder.md`.
- QA passed: `.\scripts\qa\run-storefront-builder-regeneration-gate.ps1`; `dotnet test BlazorShop.Tests.V2\BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~StorefrontBuilder" --blame-hang --blame-hang-timeout 5m` passed `39/39`.
- Note: the V2 test run emitted existing `MessagePack` vulnerability warnings; no phase source changes were required.

## Phase 3E.3 - Portable Handoff Canonical Integrity

Goal: keep copied handoff packages valid without the source project, while avoiding false coupling.

Tasks:

- [x] Keep the portable package contract explicit about required artifacts and schema kinds.
- [x] Make canonical membership checks part of `PortableHandoffValidator`, not just source-side assembly.
- [x] Keep the portable validator focused on the copied package root.
- [x] Decide and enforce one readiness invariant:
  - [x] keep readiness outside the package hash because it is a bootstrap artifact written after manifest assembly,
  - [x] assert that manifest readiness and handoff readiness agree exactly.
- [x] Preserve deterministic package hash behavior across copied package locations.
- [x] Keep diagnostic-only provenance out of the consumer contract.
- [x] Keep generated-target-path and external-url references in their own reference category.
- [x] Make the validator failure messages tell the developer what to copy or repair.

Implementation notes:

- Do not let copied-package validation fall back to the original project root.
- Do not blur consumer dependency, diagnostic provenance, and generated target paths into one string scan.
- Keep `AgentHandoffAssembler` and `PortableHandoffValidator` aligned on the same canonical contract set.

QA:

```powershell
dotnet test tools\BlazorShop.AI.StorefrontReverseEngineering\tests\BlazorShop.AI.StorefrontReverseEngineering.Tests\BlazorShop.AI.StorefrontReverseEngineering.Tests.csproj --no-restore --filter "FullyQualifiedName~PortableHandoff"
```

Expected verification:

- [x] Copied package validates independently.
- [x] Mutating readiness fails clearly.
- [x] Missing canonical artifact or schema fails clearly.
- [x] Package hash remains stable across copy.

Done when:

- [x] Portable closure no longer depends on hidden source-project state.

Phase 3E.3 evidence:

- `PortableHandoffValidator` now checks manifest membership against `AgentHandoffContract.RequiredArtifacts`, allows only canonical required artifacts plus packaged evidence files, and returns explicit blocker codes for missing/extra/duplicate/mismatched artifact entries.
- `PortableHandoffValidator` now checks required schema kind membership against `AgentHandoffContract.RequiredSchemaKinds` and returns explicit blocker codes for missing/extra/duplicate/mismatched schema requirements.
- Chosen readiness invariant: `handoff-readiness.json` stays out of the package hash because it is a bootstrap artifact written after manifest assembly, and the portable validator enforces `manifest.readinessPassed == handoff-readiness.passed`.
- Reference policy validation keeps consumer dependency, diagnostic provenance, generated target path, and external informational URL categories distinct.
- Added focused tests for manifest/readiness mismatch, missing canonical artifact entry, and missing canonical schema requirement.
- QA passed: `dotnet test tools\BlazorShop.AI.StorefrontReverseEngineering\tests\BlazorShop.AI.StorefrontReverseEngineering.Tests\BlazorShop.AI.StorefrontReverseEngineering.Tests.csproj --no-restore --filter "FullyQualifiedName~PortableHandoff" --blame-hang --blame-hang-timeout 5m` passed `18/18`.

## Phase 3E.4 - Slot Proof Regression Coverage

Goal: keep the current authoritative slot model honest with focused regressions.

Tasks:

- [x] Preserve the current source-aware slot observation model.
- [x] Update any stale comments or docs that still describe the old counter-only model.
- [x] Add or refresh regression tests for:
  - [x] `duplicate-non-repeatable-slot`
  - [x] `unapproved-extra-section`
  - [x] reviewed mapping missing from slot proof
  - [x] orphan reviewed mapping if the validator needs that explicit blocker
- [x] Keep role/text inference as suggestion-only, not authoritative.
- [x] Make sure the tests prove the actual validator behavior, not just fixture string matching.

Implementation notes:

- The current validator already uses source-aware observations; this phase should strengthen proof, not redesign it.
- If a new blocker code is added, document the exact reason and keep it narrowly scoped.

QA:

```powershell
dotnet test tools\BlazorShop.AI.StorefrontReverseEngineering\tests\BlazorShop.AI.StorefrontReverseEngineering.Tests\BlazorShop.AI.StorefrontReverseEngineering.Tests.csproj --no-restore --filter "FullyQualifiedName~PageCompositionSlotValidator|FullyQualifiedName~Phase3EFinalClosureGateTests"
```

Done when:

- [x] Slot proof still blocks the wrong graph and still accepts the reviewed graph.

Phase 3E.4 evidence:

- `PageCompositionSlotValidator` still uses `SlotObservationSource` and role suggestions remain warning-only through `section-slot-suggestion-unreviewed`.
- Reviewed mappings now only contribute authoritative slot observations when their source page and section exist in the reviewed page composition tree.
- Added blocker `reviewed-slot-mapping-orphan` for reviewed mappings that point at a missing section or when a composition node references a reviewed mapping for another page/section.
- Existing tests already cover `duplicate-non-repeatable-slot`, `unapproved-extra-section`, role suggestion warnings, repeatable product cards, and accepted reviewed mappings.
- Added focused behavioral tests proving orphan reviewed mappings do not satisfy required slots and node-to-mapping section mismatch is blocked.
- QA passed: `dotnet test tools\BlazorShop.AI.StorefrontReverseEngineering\tests\BlazorShop.AI.StorefrontReverseEngineering.Tests\BlazorShop.AI.StorefrontReverseEngineering.Tests.csproj --no-restore --filter "FullyQualifiedName~PageCompositionSlotValidator|FullyQualifiedName~Phase3EFinalClosureGateTests" --blame-hang --blame-hang-timeout 5m` passed `9/9`.
- Additional fixture regression passed: `dotnet test tools\BlazorShop.AI.StorefrontReverseEngineering\tests\BlazorShop.AI.StorefrontReverseEngineering.Tests\BlazorShop.AI.StorefrontReverseEngineering.Tests.csproj --no-restore --filter "FullyQualifiedName~BlueprintV1ReadinessTests" --blame-hang --blame-hang-timeout 5m` passed `34/34`.

## Phase 3E.5 - Documentation And Local Closure Evidence

Goal: align docs with the final local behavior and close the phase without CI dependence.

Tasks:

- [x] Update the visual reverse engineering docs to describe the stable `-WhatIf` report path.
- [x] Update docs to explain the unified generator version source.
- [x] Update any closure docs that still tell people to rely on hidden candidate artifacts for normal usage.
- [x] Record that GitHub Actions is intentionally out of scope while disabled during development.
- [x] Capture local command summaries for the final closure note.
- [x] Confirm the final plan no longer mentions a temporary-only workaround as the normal path.

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

- [x] The docs, the plan output, and the local proof all describe the same closure behavior.

Phase 3E.5 evidence:

- Updated active docs to describe the stable `-WhatIf` report path and approved `-WhatIfReportPath` roots in `docs/architecture/11-storefront-builder.md`, `docs/visual-reverse-engineering-skill/reference.md`, and `docs/visual-reverse-engineering-skill/how-to-generate-and-validate.md`.
- Updated active docs to identify `tools/BlazorShop.AI.StorefrontBuilder/version.json` as the unified `generatorVersion` source for generated metadata and generated-file manifests.
- Updated ReverseEngineering docs and `docs/qa/phase3e-final-closure.md` to record portable copied-package canonical artifact/schema checks, manifest/readiness agreement, typed reference categories, and source-aware slot provenance with `reviewed-slot-mapping-orphan`.
- GitHub Actions remains intentionally out of scope while disabled during development; local deterministic gates are the closure evidence.
- Confirmed the final plan treats hidden candidate retention only as baseline/debug context, not the normal `-WhatIf` review path.
- QA passed: `.\scripts\qa\run-storefront-client-regeneration-gate.ps1` completed without drift.
- QA passed: `.\scripts\qa\run-storefront-builder-regeneration-gate.ps1` completed without live Commerce Node data; its intentional rollback case printed a build failure and the gate still ended in `PASS`.
- QA passed: `.\scripts\qa\run-storefront-builder-generated-proof.ps1 -ProofLevel Structure` completed generated package/build/static validation/isolation/regeneration proof.
- QA passed: `.\scripts\qa\run-storefront-builder-generated-proof.ps1 -ProofLevel FoundationFunctionalFast` completed generated fast browser proof and wrote `fast-foundation-functional-report.md` under the ignored generated artifact.
- QA passed: `dotnet test BlazorShop.Tests.V2\BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~StorefrontBuilder" --blame-hang --blame-hang-timeout 5m` passed `39/39`.
- Known warnings observed during package/test proof: existing `MessagePack` NuGet vulnerability warnings, existing Presentation package `NU5100` content placement warnings, and Browserslist `caniuse-lite` freshness warning.

## Final Closure Gate

The phase is complete only when all of the following are true:

- [x] Normal `-WhatIf` output is readable without debug env vars.
- [x] The stable report location is documented.
- [x] One generator version source is used everywhere.
- [x] Portable handoff validation works on a copied package.
- [x] Slot proof still blocks incorrect mappings and extra sections.
- [x] Docs match the runtime behavior.
- [x] GitHub Actions remains intentionally excluded while disabled.

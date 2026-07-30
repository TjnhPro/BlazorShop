# StorefrontBuilder Phase 2.9 WhatIf Output And Version Polish.todo

Status: Proposed
Owner: Storefront Platform
Created: 2026-07-29
Scope: StorefrontBuilder final DX and release-evidence polish before AI Visual Generator work

## Purpose

Close the remaining non-architecture issues found after Phase 2.8. Phase 2 has already reached the core architecture target, but two developer-facing gaps still need a small focused cleanup:

- `regenerate-storefront.ps1 -WhatIf` computes a real candidate plan but does not leave a usable report after candidate cleanup.
- StorefrontBuilder exposes multiple `generatorVersion` values from different source files.

GitHub Actions evidence is intentionally not required in this phase because Actions are disabled during active development to avoid continuous runner cost/noise. Local deterministic gates remain required.

## Current Codebase Findings

- `regenerate-storefront.ps1` writes `regeneration-report.md` into the temporary candidate project.
- The candidate project is removed in `finally` unless `SFB_KEEP_REGENERATION_CANDIDATE_ARTIFACTS=1`.
- Normal `-WhatIf` output only says `WhatIf completed without writing generated project files.`
- Current docs tell users to review `docs/storefront-analysis/regeneration-report.md` after `-WhatIf`, but apply mode is the path that writes target report reliably.
- Existing `WhatIf` tests preserve the candidate via `SFB_KEEP_REGENERATION_CANDIDATE_ARTIFACTS=1`, which proves planner correctness but not normal developer usability.
- PowerShell project generation uses `$script:StorefrontBuilderGeneratorVersion = "2.4.0"`.
- Node generated-file manifest uses `generatorVersion = "2.5.0"`.
- Both appear as `generatorVersion`, so generated artifacts do not have one obvious provenance source.
- `.github/workflows/storefront-builder.yml` has a self-contained full fixture proof job, but GitHub Actions are disabled by project choice during development.

## Phase Order

1. Phase 2.9A - WhatIf Output Contract
2. Phase 2.9B - WhatIf Regression Tests
3. Phase 2.9C - Unified Generator Version Source
4. Phase 2.9D - Documentation And Local Closure Evidence

Implement in this order. Do not change generator architecture or reopen Phase 2.8 fresh-candidate work unless a direct blocker is found while implementing these tasks.

## Phase 2.9A - WhatIf Output Contract

Goal: make `-WhatIf` useful to normal developers and AI agents without relying on hidden environment variables.

Tasks:

- [x] Define the user-facing `-WhatIf` output contract.
- [x] Keep `-WhatIf` as a no-write operation for the generated target project.
- [x] Keep candidate cleanup enabled by default.
- [x] Add stable report output outside the generated target and outside the temporary candidate.
- [x] Preferred default report root:
  - [x] `{OutputRoot}/.regeneration-reports/`.
- [x] Preferred default report name:
  - [x] `{ProjectName}-{operationId}.md`.
- [x] Add optional explicit report path:
  - [x] `-WhatIfReportPath <path>`.
- [x] Validate `-WhatIfReportPath`:
  - [x] reject paths under the generated target project;
  - [x] reject paths outside approved generated output/report roots unless explicitly rooted under `obj` or `artifacts/storefront-builder`;
  - [x] reject path traversal;
  - [x] create parent directory safely.
- [x] Copy or write the computed report to the stable report path before exiting `-WhatIf`.
- [x] Print the stable report path to console.
- [x] Print a concise action plan summary to console:
  - [x] count of create actions;
  - [x] count of update actions;
  - [x] count of platform metadata updates;
  - [x] count of manual-edit conflicts;
  - [x] count of obsolete candidates;
  - [x] count of protected/user-owned skips.
- [x] Print per-action lines for meaningful entries:
  - [x] `filePath: action - reason`.
- [x] Keep skip-unchanged entries out of default console noise unless a verbose switch already exists or is added.
- [x] If every entry is skip/no-op, print a clear no-op line.
- [x] If conflicts exist, print the next action:
  - [x] resolve conflicts manually;
  - [x] rerun `-Scope conflicts`;
  - [x] rerun desired update scope.
- [x] Ensure `-WhatIf` exits zero when it successfully computes a plan, even when conflicts are present.
- [x] Ensure planning failures use existing `SFB-REGEN-*` style errors with problem, cause, and fix.
- [x] Keep `SFB_KEEP_REGENERATION_CANDIDATE_ARTIFACTS=1` as an internal/debug escape hatch only.

Implementation notes:

- Reuse `Write-RegenerationReport` instead of generating a second report format.
- Add a small `Write-RegenerationPlanSummary` helper for console output.
- Use existing path-safety helpers from `StorefrontBuilderProjectSafety.ps1`.
- Do not write `docs/storefront-analysis/regeneration-report.md` in the target project during `-WhatIf`.
- Do not disable candidate cleanup just to preserve the report.

QA:

```powershell
.\tools\BlazorShop.AI.StorefrontBuilder\regenerate-storefront.ps1 `
  -ProjectRoot artifacts/storefront-builder/generated/BlazorShop.Storefront.GeneratedProof `
  -Scope all `
  -WhatIf
```

Expected manual verification:

- [x] Console prints summary counts.
- [x] Console prints meaningful create/update/conflict/obsolete lines when present.
- [x] Console prints a stable report path.
- [x] Stable report file exists after command exits.
- [x] Generated target project tree is unchanged.
- [x] `.regeneration-candidate` is cleaned unless debug env is set.

Exit gate:

- [x] A normal `-WhatIf` run leaves a readable report.
- [x] A normal `-WhatIf` run does not require `SFB_KEEP_REGENERATION_CANDIDATE_ARTIFACTS`.
- [x] Docs can point to a stable report location without lying.

## Phase 2.9B - WhatIf Regression Tests

Goal: prove `-WhatIf` developer behavior, not only internal planner correctness.

Tasks:

- [x] Add a test that runs `-WhatIf` without `SFB_KEEP_REGENERATION_CANDIDATE_ARTIFACTS`.
- [x] Assert target tree hash is unchanged after `-WhatIf`.
- [x] Assert temporary candidate directory is cleaned after `-WhatIf`.
- [x] Assert stable report exists after `-WhatIf`.
- [x] Assert stable report contains:
  - [x] create;
  - [x] update;
  - [x] conflict manual edit;
  - [x] obsolete candidate;
  - [x] skip user-owned or skip protected.
- [x] Assert console output contains:
  - [x] stable report path;
  - [x] summary counts;
  - [x] at least one meaningful action line;
  - [x] conflict next-action guidance when conflicts exist.
- [x] Add a custom `-WhatIfReportPath` test.
- [x] Assert custom report path is respected.
- [x] Assert unsafe `-WhatIfReportPath` fails before writing.
- [x] Assert report path under target project is rejected.
- [x] Keep existing internal candidate-preservation tests if they still help inspect candidate internals.
- [x] Rename existing tests or comments so it is clear which tests cover internal planner state vs normal CLI behavior.
- [x] Add or update architecture tests that prevent docs from saying target `docs/storefront-analysis/regeneration-report.md` is the `-WhatIf` report location.

Implementation notes:

- Use disposable projects under `obj/storefront-builder/generated`.
- Avoid mutating tracked Starter files unless the test already backs up/restores them safely.
- Prefer one focused PowerShell script test for CLI behavior and one C# architecture test for docs wording.

QA:

```powershell
.\scripts\qa\run-storefront-builder-regeneration-gate.ps1
dotnet test BlazorShop.Tests.V2\BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~StorefrontBuilder"
```

Exit gate:

- [x] Regression tests fail against the old candidate-only report behavior.
- [x] Regression tests pass only when normal `-WhatIf` output/report is usable.
- [x] No test depends on hidden env vars to prove user-facing `-WhatIf` report access.

## Phase 2.9C - Unified Generator Version Source

Goal: make StorefrontBuilder artifact provenance unambiguous before AI artifacts are introduced.

Tasks:

- [x] Add a single version source file.
- [x] Preferred file:
  - [x] `tools/BlazorShop.AI.StorefrontBuilder/version.json`.
- [ ] Suggested shape:

```json
{
  "generatorVersion": "2.5.0"
}
```

- [x] Update PowerShell scripts to read the shared version source:
  - [x] `scripts/generate/StorefrontBuilderProjectSafety.ps1`;
  - [x] `scripts/generate/new-storefront-project.ps1` if direct reading is clearer;
  - [x] `regenerate-storefront.ps1` if it emits generator version directly.
- [x] Update Node scripts to read the same version source:
  - [x] `scripts/generate/generated-file-manifest.mjs`;
  - [x] any script writing metadata/composition artifacts with `generatorVersion`.
- [x] Decide the current version value.
- [x] Preferred current value:
  - [x] `2.5.0`, because manifest and Phase 2.8 regeneration behavior already uses it.
- [x] Update schema fixtures to match the chosen version if needed.
- [x] Add a test that scans StorefrontBuilder scripts and fixtures for hard-coded stale generator versions.
- [x] Add a test that generated `metadata.yaml` and `generated-files.yaml` use the same `generatorVersion`.
- [x] Add a validation rule that metadata and generated-file manifest versions must match.
- [x] Keep version naming singular: `generatorVersion`.
- [x] Do not add separate `manifestGeneratorVersion` unless there is a real product reason.

Implementation notes:

- Keep the reader simple and dependency-free for both PowerShell and Node.
- If JSON parsing in PowerShell is used, fail with an actionable message when the version file is missing or malformed.
- Do not infer version from package versions; package versions and generator version are different provenance dimensions.

QA:

```powershell
.\tools\BlazorShop.AI.StorefrontBuilder\build-storefront.ps1 -Url https://example.test -Name VersionProbe -StoreKey sample -OutputRoot obj/storefront-builder/generated/version-probe -Mode generate -Force
.\tools\BlazorShop.AI.StorefrontBuilder\validate-storefront.ps1 -ProjectRoot obj/storefront-builder/generated/version-probe/BlazorShop.Storefront.VersionProbe -Name BlazorShop.Storefront.VersionProbe -StoreKey sample
.\scripts\qa\run-storefront-builder-regeneration-gate.ps1
dotnet test BlazorShop.Tests.V2\BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~StorefrontBuilder"
```

Exit gate:

- [x] No active StorefrontBuilder script contains a conflicting hard-coded generator version.
- [x] Generated metadata and manifest agree on `generatorVersion`.
- [x] Missing/malformed `version.json` fails clearly.

## Phase 2.9D - Documentation And Local Closure Evidence

Goal: update docs to match the final DX behavior and close Phase 2 locally without requiring GitHub Actions.

Tasks:

- [ ] Update `docs/architecture/11-storefront-builder.md`:
  - [ ] document stable `-WhatIf` report location;
  - [ ] document console summary behavior;
  - [ ] document `-WhatIfReportPath` if added;
  - [ ] document unified generator version source.
- [ ] Update `docs/agents/storefront-builder.md`:
  - [ ] instruct agents to read the stable `-WhatIf` report path from console;
  - [ ] mention hidden candidate-preservation env only as debug-only;
  - [ ] keep Actions evidence optional while workflows are disabled during dev.
- [ ] Update `docs/visual-reverse-engineering-skill/reference.md`.
- [ ] Update `docs/visual-reverse-engineering-skill/how-to-generate-and-validate.md`.
- [ ] Update `docs/visual-reverse-engineering-skill/explanation-boundaries-and-regeneration.md`.
- [ ] Update `05-StorefrontBuilder-Phase2-Closure.todo.md` if it claims `-WhatIf` report is in target `docs/storefront-analysis`.
- [ ] Update `06-StorefrontBuilder-Phase2-8-Closure-Fix.todo.md` with a short note that Phase 2.9 polished `-WhatIf` output and version provenance.
- [ ] Do not require a GitHub Actions green run for Phase 2.9 while Actions are disabled.
- [ ] Add a deferred note:
  - [ ] when Actions are re-enabled, run StorefrontBuilder workflow manually with `run_browser_gates=true`;
  - [ ] record `fast-checks`, `full-fixture-proof`, and artifact upload evidence.
- [ ] Record local closure evidence with exact command summaries.

Required local release gate:

```powershell
.\scripts\qa\run-storefront-client-regeneration-gate.ps1
.\scripts\qa\run-storefront-builder-regeneration-gate.ps1
.\scripts\qa\run-storefront-builder-generated-proof.ps1 -ProofLevel Structure
.\scripts\qa\run-storefront-builder-generated-proof.ps1 -ProofLevel FoundationFunctionalFast
dotnet test BlazorShop.Tests.V2\BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~StorefrontBuilder"
```

Optional local full fixture proof:

```powershell
.\scripts\qa\run-storefront-builder-full-proof-with-fixture.ps1
```

Exit gate:

- [ ] Docs no longer tell users to look for a `-WhatIf` report that is deleted.
- [ ] Local release gate passes.
- [ ] GitHub Actions evidence is explicitly deferred because workflows are disabled by project decision.
- [ ] Phase 2.9 checklist records final command evidence before completion.

## Final Definition Of Done

Phase 2.9 is complete when:

- [ ] normal `-WhatIf` prints actionable action plan output;
- [ ] normal `-WhatIf` leaves a stable report outside the generated target and outside the deleted candidate;
- [ ] normal `-WhatIf` does not mutate the generated target;
- [ ] unsafe report paths are rejected;
- [ ] docs point to the correct report behavior;
- [ ] StorefrontBuilder generator version has one source of truth;
- [ ] generated metadata and generated-file manifest versions match;
- [ ] regression tests cover the normal user behavior without hidden env vars;
- [ ] local release gates pass;
- [ ] GitHub Actions evidence is marked deferred while Actions are disabled.

## Not In Scope

- [ ] Reopening Phase 2.8 architecture.
- [ ] Changing fresh-candidate regeneration semantics beyond report output.
- [ ] Requiring GitHub Actions to run while disabled during dev.
- [ ] AI visual generator implementation.
- [ ] Semantic Razor/CSS merge.
- [ ] React/Next/Vue starter work.
- [ ] Production deployment.

## Autoplan Review Report

CEO review:

- This is a small closure polish, not a new architecture phase.
- The highest-value fix is making `-WhatIf` trustworthy for a normal developer or AI agent.
- Skipping GitHub Actions evidence is acceptable during dev because the user explicitly disabled Actions; local deterministic gates must remain strict.

Engineering review:

- The root cause is not planner correctness; it is report lifetime and visibility.
- The fix should preserve candidate cleanup and write a stable external report.
- Version provenance should be unified now before AI-generated artifacts multiply metadata sources.

DX review:

- `-WhatIf` should answer: what will change, why, where is the full report, and what do I do next.
- Hidden env vars should not be required for ordinary user workflows.
- Error messages for report path validation must include problem, cause, and fix.

Design review:

- No UI/design work is involved.
- No visual generated output should change except incidental regenerated proof artifacts, which remain disposable.

Decision audit:

| # | Decision | Rationale | Rejected |
| --- | --- | --- | --- |
| 1 | Keep GitHub Actions evidence deferred | User confirmed Actions are disabled during dev. | Forcing CI closure now. |
| 2 | Preserve candidate cleanup | Candidate artifacts are temporary implementation detail. | Keeping candidates by default. |
| 3 | Add stable external WhatIf report | Users need a report after command exit without target mutation. | Writing report only inside deleted candidate. |
| 4 | Use one generator version source | AI artifact provenance should not have ambiguous version values. | Keeping PowerShell and Node hard-coded versions. |

## Suggested Commit Slices

1. `WhatIf` stable report path and console summary.
2. `WhatIf` user-facing regression tests.
3. Shared StorefrontBuilder version source.
4. Documentation and local closure evidence.

Each commit should update this file from `[ ]` to `[x]` only after its focused QA passes.

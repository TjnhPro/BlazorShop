# StorefrontBuilder Generated Store Cleanup.todo

Date: 2026-07-24

Status: Planned

Skill used: autoplan

## Goal

Remove the committed generated storefront projects from the active codebase and make the documentation unambiguous that generated storefronts are reproducible artifacts, not active platform projects.

The main outcome is documentation and project-shape clarity. Physical removal is only one part of the work.

## Decision

`BlazorShop.Storefront.Sample` and `BlazorShop.Storefront.BuilderDemo` should not remain committed as active projects.

`BlazorShop.Storefront.Client`, `BlazorShop.Storefront.Runtime`, and `BlazorShop.Storefront.Starter` remain source-owned platform projects. Generated stores move to an on-demand proof flow under ignored output, produced by StorefrontBuilder from the current code.

## Non-Goals

- [ ] Do not fix true visual reverse-engineering quality in this cleanup.
- [ ] Do not claim BuilderDemo visual fidelity is production-ready.
- [ ] Do not change Commerce Node API, Control Plane API, or Storefront V2 runtime behavior.
- [ ] Do not remove `BlazorShop.Storefront.Client`, `BlazorShop.Storefront.Runtime`, or `BlazorShop.Storefront.Starter`.
- [ ] Do not make `run-v2-local.ps1` depend on committed generated storefront projects.

## Current Problems To Resolve

- [ ] `BlazorShop.Storefront.Sample` and `BlazorShop.Storefront.BuilderDemo` are listed as active projects in docs.
- [ ] The solution contains solution folders and project entries for both generated stores.
- [ ] Multiple docs describe `Sample` and `BuilderDemo` as committed proof projects.
- [ ] Several scripts default to `BlazorShop.PresentationV2/BlazorShop.Storefront.BuilderDemo`.
- [ ] Some tests read committed generated files instead of proving generation from current StorefrontBuilder code.
- [ ] Visual QA reports currently behave like smoke checks and do not prove visual fidelity against the input URL.
- [ ] Functional commerce QA currently needs clearer pass criteria for actual command behavior.

## Phase 0 - Recon And Baseline

Goal: build a complete removal map before editing source.

- [x] Confirm working tree state and identify pre-existing unrelated changes.
- [x] Capture all references to `BuilderDemo`, `Storefront.Sample`, `BlazorShop.Storefront.Sample`, committed proof wording, generated proof wording, and QA command defaults.
- [x] Classify each reference as docs, architecture, agents, script, test, solution, project source, or historical plan.
- [x] Record which historical plan files should remain historical and which active docs must change.
- [x] Confirm current active StorefrontBuilder source projects:
  - [x] `BlazorShop.Storefront.Client`
  - [x] `BlazorShop.Storefront.Runtime`
  - [x] `BlazorShop.Storefront.Starter`
  - [x] `tools/BlazorShop.AI.StorefrontBuilder`
- [x] Confirm generated project directories to remove later:
  - [x] `BlazorShop.PresentationV2/BlazorShop.Storefront.Sample`
  - [x] `BlazorShop.PresentationV2/BlazorShop.Storefront.BuilderDemo`
- [x] Confirm solution entries for generated projects and their solution folders.
- [x] Confirm tests that directly read committed generated output.

Acceptance:

- [x] A reference inventory exists in the implementation notes or commit message for later verification.
- [x] No generated project is removed before scripts/tests/docs have a replacement path.

Reference inventory:

- Active root docs: `README.md`, `AGENTS.md`.
- Active architecture docs: `docs/architecture/README.md`, `01-system-map.md`, `05-project-and-folder-guide.md`, `06-feature-map.md`, `07-deployment-and-local-run.md`, `08-agent-decision-rules.md`, `10-v2-contract-ownership.md`, `11-storefront-builder.md`, and ADR `2026-07-24-storefront-starter-foundation.md`.
- StorefrontBuilder docs: `docs/visual-reverse-engineering-skill/README.md`, `reference.md`, `how-to-generate-and-validate.md`, `tutorial-builder-demo.md`, `StorefrontBuilder-architecture-note.md`, and `docs/agents/storefront-builder.md`.
- Historical docs: `docs/refactor-control-Commerce-storefront/Storefront Starter Foundation.todo.md`, `Storefront Starter Foundation.sample-qa.md`, and `docs/storefront-platform/storefront-ai-generator-plan.md`.
- Scripts: `validate-storefront.ps1`, `regenerate-storefront.ps1`, generation scripts under `tools/BlazorShop.AI.StorefrontBuilder/scripts/generate/`, browser QA scripts under `tools/BlazorShop.AI.StorefrontBuilder/scripts/qa/`, `scripts/qa/run-storefront-builder-isolation-gate.ps1`, `scripts/generate-storefront-sample.ps1`, and `scripts/qa/run-storefront-sample-release-gate.ps1`.
- Tests: `StorefrontStarterFoundationBoundaryTests.cs`, `StorefrontBuilderVisualGenerationTests.cs`, and `StorefrontBuilderQaRegenerationTests.cs`.
- Solution/source: `BlazorShop.sln`, `BlazorShop.PresentationV2/BlazorShop.Storefront.Sample`, and `BlazorShop.PresentationV2/BlazorShop.Storefront.BuilderDemo`.

Commit:

- [x] Optional documentation-only planning commit if this file is committed before implementation.

## Phase 1 - Define Generated Store Output Policy

Goal: make generated storefront location explicit before removing committed projects.

- [x] Define generated storefronts as ignored artifacts, not source-owned projects.
- [x] Select the manual/local generated output root:
  - [x] Preferred: `artifacts/storefront-builder/generated/{ProjectName}`
- [x] Select the automated test output root:
  - [x] Preferred: temporary test directory or `obj/storefront-builder/generated/{ProjectName}`
- [x] Update `.gitignore` to exclude generated StorefrontBuilder output roots if not already excluded.
- [x] Decide the canonical generated proof name for examples:
  - [x] Preferred: `BlazorShop.Storefront.GeneratedProof`
- [x] Document that generated output may be deleted at any time and regenerated from StorefrontBuilder.
- [x] Document that generated output must not be added to `BlazorShop.sln` by default.
- [x] Document that generated output must not become an architecture owner or API contract owner.

Generated output policy:

- Manual/local proof output root is `artifacts/storefront-builder/generated/{ProjectName}`.
- Automated proof output root is `obj/storefront-builder/generated/{ProjectName}` or a test-owned temporary directory.
- The canonical proof project name is `BlazorShop.Storefront.GeneratedProof`.
- Generated output is disposable, ignored by git, and recreated from `BlazorShop.Storefront.Starter` plus `tools/BlazorShop.AI.StorefrontBuilder`.
- Generated output must not be added to `BlazorShop.sln` by default and must not become an architecture or API contract owner.

Acceptance:

- [x] There is one documented output policy for generated stores.
- [x] Future generated stores cannot be mistaken for active source projects.
- [x] The output path is safe to clean and is ignored by git.

Commit:

- [x] Commit message: `docs: define generated storefront output policy`

## Phase 2 - De-Hardcode StorefrontBuilder Scripts

Goal: remove source-path defaults that point tools at committed `BuilderDemo` or `Sample`.

- [x] Update `tools/BlazorShop.AI.StorefrontBuilder/validate-storefront.ps1`.
- [x] Update `tools/BlazorShop.AI.StorefrontBuilder/regenerate-storefront.ps1`.
- [x] Update `tools/BlazorShop.AI.StorefrontBuilder/build-storefront.ps1`.
- [x] Update generation scripts under `tools/BlazorShop.AI.StorefrontBuilder/scripts/generate/`.
- [x] Update QA scripts under `tools/BlazorShop.AI.StorefrontBuilder/scripts/qa/`.
- [x] Update `scripts/qa/run-storefront-builder-isolation-gate.ps1`.
- [x] Update `scripts/generate-storefront-sample.ps1` or retire it if the Builder flow supersedes it.
- [x] Update `scripts/qa/run-storefront-sample-release-gate.ps1` so it no longer requires a committed sample project.
- [x] Replace hardcoded defaults with explicit parameters or generated-artifact defaults.
- [x] Ensure validation commands fail fast with actionable messages when a project path is missing.
- [x] Ensure any cleanup command verifies the resolved target stays inside the generated output root.

Acceptance:

- [x] No StorefrontBuilder script defaults to `BlazorShop.PresentationV2/BlazorShop.Storefront.BuilderDemo`.
- [x] No starter/sample QA script requires committed `BlazorShop.Storefront.Sample`.
- [x] Existing Starter/Runtime/Client build behavior remains unchanged.

Commit:

- [x] Commit message: `chore: decouple storefront builder scripts from committed demos`

## Phase 3 - Add On-Demand Generated Proof Flow

Goal: replace committed proof projects with a reproducible generation command.

- [x] Add or update a single proof command, for example:
  - [x] `scripts/qa/run-storefront-builder-generated-proof.ps1`
- [x] The proof command must:
  - [x] Resolve repo root.
  - [x] Clean only the approved generated output path.
  - [x] Pack or restore local StorefrontBuilder dependencies as needed.
  - [x] Generate a storefront project from `BlazorShop.Storefront.Starter`.
  - [x] Use a deterministic proof project name.
  - [x] Restore the generated project.
  - [x] Build the generated project.
  - [x] Run static StorefrontBuilder validation.
  - [x] Run package/reference isolation checks.
  - [x] Optionally start the generated project for browser QA when requested.
- [x] Ensure the proof command produces review artifacts under the generated output directory.
- [x] Ensure generated proof artifacts are not committed.

Acceptance:

- [x] A clean checkout can generate a proof storefront without `Sample` or `BuilderDemo` in source.
- [x] The generated proof command is the single documented replacement for committed generated stores.
- [x] The generated proof can be deleted and recreated without manual cleanup.

Commit:

- [x] Commit message: `test: add generated storefront proof workflow`

## Phase 4 - Move Tests From Committed Projects To Generated Artifacts

Goal: make tests prove current StorefrontBuilder behavior instead of reading stale committed output.

- [x] Update `BlazorShop.Tests.V2/Architecture/StorefrontStarterFoundationBoundaryTests.cs`.
- [x] Update `BlazorShop.Tests.V2/Architecture/StorefrontBuilderVisualGenerationTests.cs`.
- [x] Update `BlazorShop.Tests.V2/Architecture/StorefrontBuilderQaRegenerationTests.cs`.
- [x] Replace direct reads of `BlazorShop.Storefront.Sample` with Starter/Runtime/Client contract checks or generated temp output checks.
- [x] Replace direct reads of `BlazorShop.Storefront.BuilderDemo` with generated temp output checks.
- [x] Remove assertions that docs must call `Storefront.Sample` an active deterministic project.
- [x] Add assertions that active docs describe generated stores as artifacts.
- [x] Add assertions that `BuilderDemo` and `Sample` are absent from active solution entries after removal.
- [x] Keep historical plan assertions scoped to historical docs only, if still needed.
- [x] Ensure tests do not depend on uncommitted generated artifacts.

Acceptance:

- [x] Architecture tests pass without committed `Sample` or `BuilderDemo` directories.
- [x] Tests fail if docs reintroduce those projects as active source.
- [x] Tests still protect Starter/Runtime/Client boundaries.

Commit:

- [x] Commit message: `test: validate generated storefronts as artifacts`

## Phase 5 - Clean Active Documentation And Agent Guidance

Goal: remove the misleading mental model from docs so future agents do not assume `Sample` or `BuilderDemo` still exist.

- [x] Update root `README.md`.
- [x] Update root `AGENTS.md`.
- [x] Update `docs/architecture/README.md`.
- [x] Update `docs/architecture/01-system-map.md`.
- [x] Update `docs/architecture/05-project-and-folder-guide.md`.
- [x] Update `docs/architecture/06-feature-map.md`.
- [x] Update `docs/architecture/07-deployment-and-local-run.md`.
- [x] Update `docs/architecture/08-agent-decision-rules.md`.
- [x] Update `docs/architecture/10-v2-contract-ownership.md`.
- [x] Update `docs/architecture/11-storefront-builder.md`.
- [x] Update `docs/architecture/adr/2026-07-24-storefront-starter-foundation.md` with a superseding note.
- [x] Update `docs/agents/README.md` if it references generated proof projects.
- [x] Update `docs/agents/storefront-builder.md`.
- [x] Update `docs/visual-reverse-engineering-skill/README.md`.
- [x] Update `docs/visual-reverse-engineering-skill/reference.md`.
- [x] Update `docs/visual-reverse-engineering-skill/how-to-generate-and-validate.md`.
- [x] Replace or retire `docs/visual-reverse-engineering-skill/tutorial-builder-demo.md`.
- [x] Update `docs/visual-reverse-engineering-skill/StorefrontBuilder-architecture-note.md`.
- [x] Update `docs/storefront-platform/storefront-ai-generator-plan.md`.
- [x] Mark historical phase plans under `docs/refactor-control-Commerce-storefront/` as historical where they mention `Storefront.Sample`.
- [x] Search active docs for these phrases and remove or qualify them:
  - [x] `BuilderDemo`
  - [x] `Storefront.Sample`
  - [x] `committed proof`
  - [x] `active generated proof`
  - [x] `Validate the committed generated proof`
- [x] Keep only clearly historical mentions or current removal-plan mentions.

Acceptance:

- [x] Active docs say generated storefronts are on-demand artifacts.
- [x] Active docs list only Client, Runtime, Starter, and StorefrontBuilder tooling as the StorefrontBuilder platform source surface.
- [x] No active doc tells agents to run `BuilderDemo` as the canonical proof.
- [x] No active doc tells agents `Storefront.Sample` is an active project.

Commit:

- [x] Commit message: `docs: remove committed generated storefront guidance`

## Phase 6 - Remove Generated Projects From Source And Solution

Goal: physically remove stale generated projects after replacement flow and docs are ready.

- [x] Remove `BlazorShop.Storefront.Sample` from `BlazorShop.sln`.
- [x] Remove `BlazorShop.Storefront.BuilderDemo` from `BlazorShop.sln`.
- [x] Remove empty generated-store solution folders if present.
- [x] Delete `BlazorShop.PresentationV2/BlazorShop.Storefront.Sample`.
- [x] Delete `BlazorShop.PresentationV2/BlazorShop.Storefront.BuilderDemo`.
- [x] Verify `dotnet sln BlazorShop.sln list` does not include either project.
- [x] Verify `BlazorShop.Storefront.Client`, `BlazorShop.Storefront.Runtime`, and `BlazorShop.Storefront.Starter` remain in the solution.
- [x] Verify no project references point to removed project paths.
- [x] Verify generated artifacts are absent from tracked source.

Note: tracked source for both generated projects was removed. A leftover empty ignored `BuilderDemo/docs/storefront-analysis/capture` directory may remain locally while a GUI process holds a handle, but it is not tracked and no active source/project files remain.

Acceptance:

- [x] The committed source tree no longer contains generated storefront projects.
- [x] The solution no longer contains generated storefront project entries.
- [x] The repo still builds with active platform projects.

Commit:

- [x] Commit message: `chore: remove committed generated storefront projects`

## Phase 7 - Make QA Labels Honest

Goal: prevent future false confidence from smoke tests being described as full visual or commerce QA.

- [ ] Rename or qualify visual QA report wording if it only checks rendering smoke.
- [ ] Add CSS-applied checks to browser QA:
  - [ ] Stylesheet requests return non-empty CSS.
  - [ ] Browser `document.styleSheets` exposes loaded rules or a known computed style changes from browser defaults.
  - [ ] Body font is not default `Times New Roman` unless explicitly intended.
- [ ] Add artifact integrity checks:
  - [ ] `generated-files.yaml` must not list missing files as generated proof.
  - [ ] `capture-manifest.json` URL must be connected to generation inputs or reported as unused.
  - [ ] Missing `design-tokens.yaml`, `ui-patterns.yaml`, or `composition-manifest.yaml` must be reported clearly if the flow claims visual generation.
- [ ] Rename functional commerce checks if they only validate selectors.
- [ ] Add real interaction assertions where feasible:
  - [ ] Add-to-cart click produces a success state, cart count change, or expected disabled/offline explanation.
  - [ ] Product route has expected data-backed content.
  - [ ] Checkout/cart links resolve to expected pages or explicit placeholders.
- [ ] Clearly separate:
  - [ ] Scaffold smoke pass.
  - [ ] Runtime CSS/application pass.
  - [ ] Commerce interaction pass.
  - [ ] Visual fidelity pass.
- [ ] Mark visual fidelity against the input URL as not implemented until the generator actually uses captured layout/theme artifacts.

Acceptance:

- [ ] QA reports cannot imply visual fidelity when only smoke checks ran.
- [ ] A CSS-serving failure produces a failed QA result.
- [ ] Missing visual generation artifacts produce actionable failures or explicit "not implemented" status.

Commit:

- [ ] Commit message: `test: clarify storefront builder qa gates`

## Phase 8 - Future Optional Local Runner Integration

Goal: keep `run-v2-local.ps1` separate from this cleanup, while documenting the future path.

- [ ] Do not block removal on `run-v2-local.ps1` support for generated stores.
- [ ] Document current state: `run-v2-local.ps1` is for active Storefront V2 runtime unless explicitly extended.
- [ ] Future extension may add a storefront selection mode:
  - [ ] `-StorefrontProject BlazorShop.Storefront.V2`
  - [ ] `-StorefrontProjectPath artifacts/storefront-builder/generated/...`
  - [ ] `-StorefrontEnvPrefix` or explicit env mapping
- [ ] Future extension must not assume `BuilderDemo` exists in source.

Acceptance:

- [ ] Local runner docs no longer mention committed `BuilderDemo` as a built-in target.
- [ ] Generated storefront local run remains possible through explicit `dotnet run --project <generated path>`.

Commit:

- [ ] Fold into docs/script commit if no code changes are needed.

## Phase 9 - Final Verification

Goal: prove the cleanup is complete and does not leave stale active references.

- [ ] Run `git status --short` and confirm only intended files changed.
- [ ] Run `rg -n "BuilderDemo|Storefront\\.Sample|BlazorShop\\.Storefront\\.Sample|committed proof|active generated proof" README.md AGENTS.md docs scripts tools BlazorShop.Tests.V2 BlazorShop.sln`.
- [ ] Review every remaining match and confirm it is historical, test-fixture-only, or this cleanup plan.
- [ ] Run `dotnet restore BlazorShop.sln`.
- [ ] Run `dotnet build BlazorShop.sln --no-restore`.
- [ ] Run focused StorefrontBuilder tests.
- [ ] Run the generated proof command from Phase 3.
- [ ] Run StorefrontBuilder validation against the generated artifact.
- [ ] Run StorefrontBuilder isolation gate against the generated artifact.
- [ ] If browser QA changed, run the browser smoke checks against the generated artifact.
- [ ] Confirm no generated artifact files are tracked.

Acceptance:

- [ ] Build passes without committed generated stores.
- [ ] Focused tests pass without committed generated stores.
- [ ] Generated proof can be recreated from source.
- [ ] Active docs no longer describe `Sample` or `BuilderDemo` as existing active projects.

Commit:

- [ ] Commit message: `chore: verify generated storefront cleanup`

## Commit Order

- [ ] Phase 0/1 planning and policy.
- [ ] Phase 2 script de-hardcoding.
- [ ] Phase 3 generated proof workflow.
- [ ] Phase 4 tests.
- [ ] Phase 5 docs cleanup.
- [ ] Phase 6 project removal.
- [ ] Phase 7 QA label and gate cleanup.
- [ ] Phase 9 final verification if it produces tracked changes.

Each phase should be committed separately. If a phase has no tracked changes, record its verification in the next commit message or final implementation notes.

## Risk Register

- [ ] Removing generated projects before replacing the proof path can break existing tests and docs.
- [ ] Leaving docs stale is worse than leaving stale source because future agents will recreate the wrong architecture.
- [ ] Generated-on-demand tests may be slower; keep them focused and separate from cheap architecture tests.
- [ ] Output-root cleanup is destructive; require path resolution and generated-root containment checks.
- [ ] Historical docs may still mention `Storefront.Sample`; those mentions must be explicitly marked historical or left only in archived plans.
- [ ] Visual QA may still be weak after cleanup; label it honestly and fail on CSS/runtime defects.

## Architecture Shape After Completion

```text
Source-owned StorefrontBuilder platform:

BlazorShop.Storefront.Client
BlazorShop.Storefront.Runtime
BlazorShop.Storefront.Starter
tools/BlazorShop.AI.StorefrontBuilder

Generated artifacts:

artifacts/storefront-builder/generated/{ProjectName}
obj/storefront-builder/generated/{ProjectName}

Not source-owned:

BlazorShop.Storefront.Sample
BlazorShop.Storefront.BuilderDemo
```

## GSTACK REVIEW REPORT

Autoplan synthesis:

- CEO review: commit to one model, generated stores are disposable proof artifacts, not active product surface.
- Engineering review: replace source-owned demos with a generated-on-demand proof before deleting existing projects.
- Developer experience review: remove misleading defaults and make commands explicit so future agents do not target stale projects.
- Design review: no user-facing redesign belongs in this cleanup; QA wording must not imply visual fidelity that is not proven.

Final recommendation:

- [ ] Proceed with removal only after Phase 2, Phase 3, and Phase 5 have clear replacements for scripts, proof flow, and docs.
- [ ] Treat Phase 5 documentation cleanup as the central deliverable.
- [ ] Keep true visual generation improvements for the later StorefrontBuilder correction phases.

NO UNRESOLVED DECISIONS

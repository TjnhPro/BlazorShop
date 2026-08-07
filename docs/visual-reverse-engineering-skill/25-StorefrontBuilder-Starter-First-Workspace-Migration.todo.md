# StorefrontBuilder Starter-First Workspace Migration

Status: Proposed
Owner: StorefrontBuilder / Storefront Platform
Created: 2026-08-07
Supersedes: `24-StorefrontBuilder-Starter-Wasm-Parity-Generation-Hardening.todo.md` where that file assumes a nested generated WASM project or marks server/WASM sibling output as complete while code still emits nested output.

## Objective

Move StorefrontBuilder generation, validation, regeneration, and QA proof from the current nested generated project shape to a Starter-first workspace shape that mirrors the actual Starter source architecture:

```text
artifacts/storefront-builder/generated/BlazorShop.Storefront.{Name}/
|-- BlazorShop.Storefront.{Name}.sln
|-- StorefrontPackageVersions.props
|-- nuget.config
|-- docs/
|   `-- storefront-analysis/
|-- BlazorShop.Storefront.{Name}/
|   |-- BlazorShop.Storefront.{Name}.csproj
|   `-- ...
`-- BlazorShop.Storefront.{Name}.WASM/
    |-- BlazorShop.Storefront.{Name}.WASM.csproj
    `-- ...
```

The generated workspace must be disposable, isolated from `BlazorShop.sln`, built by packages where required, and must not use nested-WASM exclusion hacks.

## Current Codebase Findings

These findings were verified from the current repo and must drive implementation:

- `BlazorShop.PresentationV2/BlazorShop.Storefront.Starter` exists as the server/BFF Starter.
- `BlazorShop.PresentationV2/BlazorShop.Storefront.Starter.WASM` exists as the browser/WASM Starter.
- Starter server currently references Starter.WASM as a sibling project and maps the WASM assembly through `MapStorefrontApplication(...)`.
- Starter server registers `AddStorefrontBrowserControllers()`.
- Starter.WASM uses `AddStorefrontBrowserRuntime(...)`.
- `StorefrontPackageVersions.props` already contains Storefront package version properties for Client, Runtime, Presentation, Components, and Browser.
- `starter-generation.contract.yaml` still describes older single-project output in several places and its build commands restore/build a generated `.csproj`, not a workspace `.sln`.
- `scripts/generate-storefront-sample.ps1` still copies Starter.WASM into the generated server root as a nested folder.
- `scripts/generate-storefront-sample.ps1` still rewrites the generated server reference to `BlazorShop.Storefront.{Name}.WASM/...` and adds `Compile/Content/EmbeddedResource/None Remove` exclusions for that nested folder.
- `tools/BlazorShop.AI.StorefrontBuilder/scripts/validate/Test-StorefrontBuilderGeneratedProject.ps1` still validates the nested shape and requires exclusion markers.
- `scripts/qa/run-storefront-builder-isolation-gate.ps1` still assumes generated project root equals server project root and that WASM lives under it.
- `tools/BlazorShop.AI.StorefrontBuilder/build-storefront.ps1` still treats `OutputRoot/ProjectName` as the generated project root without a separate workspace/server/WASM path model.
- `plan-generation-files.mjs` still models server root as `"."` and WASM root as `{name}.WASM`.
- `generated-file-manifest.mjs` infers WASM ownership from path substring matching instead of explicit project metadata.
- `regenerate-storefront.ps1` still treats `ProjectRoot` as the root that contains `{Name}.csproj` and `docs/storefront-analysis`.
- Architecture docs already point toward Starter + Starter.WASM and generated sibling WASM output, so this phase is a code and docs alignment phase, not a new architecture invention.

## Chosen Direction

Use a workspace-first model:

```text
WorkspaceRoot = OutputRoot / ProjectName
ServerProjectRoot = WorkspaceRoot / ProjectName
WasmProjectRoot = WorkspaceRoot / (ProjectName + ".WASM")
SolutionPath = WorkspaceRoot / (ProjectName + ".sln")
AnalysisRoot = WorkspaceRoot / "docs/storefront-analysis"
```

Keep current monorepo Starter source as sibling projects:

```text
BlazorShop.PresentationV2/
|-- BlazorShop.Storefront.Starter/
`-- BlazorShop.Storefront.Starter.WASM/
```

Builder must copy, rename, and rewrite from these sources. It must not move WASM into the server root, must not add generated exclusion ItemGroups to hide nested WASM files, and must not infer Storefront app structure from folder accidents.

## Non-Goals

- Do not rewrite Storefront Presentation, Runtime, Browser, or Components.
- Do not change Commerce Node APIs or Storefront API contracts.
- Do not add generated projects to `BlazorShop.sln`.
- Do not migrate historical generated artifacts in place unless a separate operator command is explicitly approved.
- Do not reintroduce V2 visual source as a generation source.
- Do not turn StorefrontBuilder into a production service.
- Do not require GitHub Actions evidence in this phase. Local gates are the closure evidence while Actions are disabled during development.

## Autoplan Review Summary

### CEO Review

Decision: keep the change narrow but complete.

Rationale:

- The current nested-WASM generated shape creates false complexity and fragile exclusions.
- A generated storefront is a disposable product workspace, not one server project with hidden nested source.
- The user goal is maintainability and agent correctness, so the generated output must be obvious from the filesystem layout.
- The correct business outcome is fewer special cases before visual AI generation, not more compatibility layers.

Rejected:

- Patch the nested shape with more exclusions.
- Keep old generation shape until later AI visual phases.
- Add an auto-migration system for old generated artifacts before fresh generation is correct.

### Engineering Review

Decision: introduce an explicit workspace path model and propagate it through generator, validator, regeneration, manifests, QA gates, and docs in one coordinated phase set.

Rationale:

- The same incorrect assumption appears across PowerShell, Node manifest code, validator code, and docs.
- Fixing only `generate-storefront-sample.ps1` would leave validation and regeneration incorrectly judging the new output.
- Explicit path variables reduce risk compared with deriving behavior from string matching.

Required guardrails:

- Generated workspace contains exactly one server project and one sibling WASM project.
- Server project references sibling WASM via `..\{Name}.WASM\{Name}.WASM.csproj`.
- Server project does not contain `Compile/Content/EmbeddedResource/None Remove` exclusions for the WASM subtree.
- WASM project never references Runtime, Client, Commerce Node, Control Plane, V2, backend, or core projects.
- Generated host uses Presentation and Browser through approved package/reference boundaries only.

### DX Review

Decision: CLI should prefer `-WorkspaceRoot`, keep `-ProjectRoot` as a compatibility alias for one cycle, and every error must say problem, cause, and fix.

Rationale:

- Operators and AI agents need commands that match the actual filesystem shape.
- `ProjectRoot` is ambiguous once there is a workspace containing two projects.
- Abruptly breaking existing proof commands is unnecessary; an alias with deprecation guidance is lower risk.

### Design Review

Decision: no UI design changes in this phase.

Rationale:

- This phase changes generated project structure and tooling behavior.
- Browser proof remains required because generated page behavior can break when the server/WASM relationship changes.

## Decision Audit Trail

| # | Phase | Decision | Classification | Principle | Rationale | Rejected |
|---|-------|----------|----------------|-----------|-----------|----------|
| 1 | Architecture | Use workspace-first generated output | Auto-decided | Maintainability | Mirrors Starter source and makes server/WASM boundaries visible | Nested WASM with exclusions |
| 2 | Compatibility | Keep `-ProjectRoot` as alias temporarily | Auto-decided | DX stability | Existing scripts/docs/tests can migrate without immediate hard break | Immediate removal |
| 3 | Validation | Remove required nested exclusion checks and replace with negative checks | Auto-decided | Correctness | Exclusions hide a wrong folder layout instead of proving isolation | More exclusion markers |
| 4 | Regeneration | Compare workspace-relative files with explicit project ownership | Auto-decided | Safety | Prevents string-substring project detection bugs | `.WASM/` substring inference |
| 5 | Legacy generated artifacts | Do not auto-migrate old generated artifacts | Auto-decided | Scope control | Generated artifacts are disposable and fresh output is the target | In-place migration engine |
| 6 | CI evidence | Local closure gates only | User constraint | Practicality | GitHub Actions are disabled during development | Mandatory Actions proof |

## Phase 0 - Baseline And Drift Lock

Goal: prove the current drift before editing so implementation does not guess.

Tasks:

- [x] Record current git status and note unrelated existing modifications, especially `BlazorShop.sln`, without reverting them.
- [x] Confirm Starter source layout:
  - [x] `BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/BlazorShop.Storefront.Starter.csproj`
  - [x] `BlazorShop.PresentationV2/BlazorShop.Storefront.Starter.WASM/BlazorShop.Storefront.Starter.WASM.csproj`
- [x] Confirm Starter server maps Starter.WASM assembly and registers Browser controllers.
- [x] Confirm Starter.WASM references Browser and Components but not Runtime, Client, Commerce Node, Control Plane, V2, backend, or core projects.
- [x] Confirm current generator still emits nested WASM output and exclusion ItemGroups.
- [x] Confirm current validator and isolation gate still assume nested output.
- [x] Confirm current docs already describe Starter + Starter.WASM as the desired canonical template pair.
- [x] Add a short implementation note to the working issue/commit message that this phase is "docs-aligned code migration", not new architecture.

Exit criteria:

- [x] A developer can point to the exact files that still encode the old nested shape.
- [x] No code has been changed yet.

Phase 0 evidence:

- `git status --short` before implementation showed unrelated `BlazorShop.sln` modification plus this new todo file; `BlazorShop.sln` was not edited or reverted.
- Starter server and Starter.WASM source projects both exist. Starter server registers `AddStorefrontBrowserControllers()` and maps the Starter.WASM assembly through `MapStorefrontApplication(...)`.
- Starter.WASM references only `BlazorShop.Storefront.Browser` and `BlazorShop.Storefront.Components` project references, with no Runtime, Client, Commerce Node, Control Plane, V2, backend, or core project reference.
- Old nested shape is still encoded in `scripts/generate-storefront-sample.ps1`, `tools/BlazorShop.AI.StorefrontBuilder/scripts/validate/Test-StorefrontBuilderGeneratedProject.ps1`, `scripts/qa/run-storefront-builder-isolation-gate.ps1`, `tools/BlazorShop.AI.StorefrontBuilder/regenerate-storefront.ps1`, and manifest/planning helpers that classify `.WASM/` paths relative to the current project root.
- Architecture and agent docs already name Starter plus Starter.WASM as the canonical template pair, so this migration is a docs-aligned code migration, not a new architecture.

## Phase 1 - Canonical Contract Update

Goal: make `starter-generation.contract.yaml` describe the workspace output and remove ambiguity before script changes.

Tasks:

- [x] Update `starter-generation.contract.yaml` to declare a `workspace` model:
  - [x] `workspaceRootPattern: "{OutputRoot}/{ProjectName}"`
  - [x] `solutionFile: "{ProjectName}.sln"`
  - [x] `analysisRoot: "docs/storefront-analysis"`
  - [x] `sharedFiles: ["StorefrontPackageVersions.props", "nuget.config"]`
- [x] Add explicit project entries:
  - [x] `server.namePattern: "{ProjectName}"`
  - [x] `server.rootPath: "{ProjectName}"`
  - [x] `server.projectPath: "{ProjectName}/{ProjectName}.csproj"`
  - [x] `wasm.namePattern: "{ProjectName}.WASM"`
  - [x] `wasm.rootPath: "{ProjectName}.WASM"`
  - [x] `wasm.projectPath: "{ProjectName}.WASM/{ProjectName}.WASM.csproj"`
- [x] Declare source template entries:
  - [x] `server.sourceProject: "BlazorShop.PresentationV2/BlazorShop.Storefront.Starter"`
  - [x] `wasm.sourceProject: "BlazorShop.PresentationV2/BlazorShop.Storefront.Starter.WASM"`
- [x] Declare generated package/reference rules:
  - [x] Server may reference sibling generated WASM project.
  - [x] Server consumes Presentation, Components, and Browser through package boundaries in independent generated proof.
  - [x] Runtime and Client remain package metadata/provenance unless explicitly needed through Presentation.
  - [x] WASM consumes Browser and Components only.
- [x] Declare forbidden output:
  - [x] No nested `{ProjectName}.WASM` folder under server project root.
  - [x] No generated exclusion ItemGroups for nested WASM.
  - [x] No direct V2/backend/core/API project references.
  - [x] No generated `@page` directives in visual slot files.
- [x] Update contract build commands:
  - [x] `dotnet restore {WorkspaceRoot}/{ProjectName}.sln --no-cache --force-evaluate`
  - [x] `dotnet build {WorkspaceRoot}/{ProjectName}.sln --no-restore`
  - [x] `dotnet run --project {WorkspaceRoot}/{ProjectName}/{ProjectName}.csproj`
- [x] Keep route metadata aligned with Presentation route truth.
- [x] If contract schema validation exists, update schema and tests in the same phase.

Exit criteria:

- [x] Contract describes the target workspace without referring to nested output.
- [x] Contract can be used by scripts and validators without path inference.

Phase 1 evidence:

- `starter-generation.contract.yaml` now declares workspace root, solution, analysis root, shared files, explicit server/WASM sibling project paths, source template projects, generated reference rules, and forbidden output.
- Existing contract preflight passed with `Test-StorefrontBuilderPreflight.ps1 -ReferenceUrls https://www.kindredcoast.com/ -Name Kindredcoast -StoreKey kindredcoast -OutputRoot BlazorShop.PresentationV2 -Mode validate-only`.
- `Test-StorefrontBuilderCapabilities.ps1` and `Test-StorefrontBuilderTopology.ps1` require generated artifact path parameters and were not applicable to this contract-only phase.

## Phase 2 - Starter Source Hardening

Goal: make Starter and Starter.WASM a clean template source pair before copying them.

Tasks:

- [x] Review Starter server `Program.cs` for current app composition:
  - [x] `AddStorefrontApplication(...)`
  - [x] `AddStorefrontBrowserControllers()`
  - [x] `UseStorefrontApplication()`
  - [x] `MapStorefrontApplication(...)`
- [x] Replace direct WASM component type usage with a stable marker if needed:
  - [x] Add `StarterWasmAssemblyMarker` under Starter.WASM.
  - [x] Map `typeof(StarterWasmAssemblyMarker).Assembly`.
  - [x] Ensure generator can rename this marker namespace safely.
- [x] Ensure Starter server project contains no nested-WASM exclusion ItemGroups.
- [x] Ensure Starter server ProjectReferences remain monorepo-only:
  - [x] Presentation
  - [x] Components
  - [x] Browser
  - [x] Starter.WASM
- [x] Ensure Starter.WASM ProjectReferences remain browser-safe:
  - [x] Browser
  - [x] Components
- [x] Ensure Starter.WASM has no Runtime or Client dependency.
- [x] Decide generated props import rewrite:
  - [x] Source Starter may keep its monorepo-local import.
  - [x] Generated server project imports `..\StorefrontPackageVersions.props`.
  - [x] Generated WASM project imports `..\StorefrontPackageVersions.props`.
- [x] Add or update Starter-specific tests if current test coverage is missing:
  - [x] Starter server builds in monorepo.
  - [x] Starter.WASM builds in monorepo.
  - [x] Starter.WASM does not reference Runtime or Client.

Exit criteria:

- [x] Starter source is valid independently from generated output rules.
- [x] The generated rewrite target is deterministic.

Phase 2 evidence:

- Added `StarterWasmAssemblyMarker` in Starter.WASM and mapped `MapStorefrontApplication(...)` to the marker assembly instead of a specific account component.
- `dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/BlazorShop.Storefront.Starter.csproj --no-restore` passed.
- `dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.Starter.WASM/BlazorShop.Storefront.Starter.WASM.csproj --no-restore` passed after rerunning sequentially; the first parallel run hit a compiler file lock on shared Components output.
- `rg` scan over Starter/Starter.WASM `.csproj` and `.cs` files found no Runtime, Client, V2, Commerce Node, Control Plane, backend/core references, or nested-WASM exclusion ItemGroups.

## Phase 3 - Workspace Path Model In StorefrontBuilder

Goal: replace ambiguous "project root" assumptions with explicit workspace/server/WASM paths.

Tasks:

- [x] Introduce a shared path model in PowerShell helper code or a small script module:
  - [x] `ProjectName`
  - [x] `OutputRoot`
  - [x] `WorkspaceRoot`
  - [x] `ServerProjectRoot`
  - [x] `WasmProjectRoot`
  - [x] `SolutionPath`
  - [x] `AnalysisRoot`
  - [x] `MetadataPath`
  - [x] `ContractPath`
- [x] Normalize and validate `ProjectName` before any path is created.
- [x] Verify resolved paths stay under approved output roots.
- [x] Add `-WorkspaceRoot` to relevant scripts:
  - [x] `validate-storefront.ps1`
  - [x] `regenerate-storefront.ps1`
  - [x] isolation gate
  - [x] visual QA wrappers if they parse project structure
  - [x] phase4 MVP gate if it reads generated analysis/source
- [x] Keep `-ProjectRoot` as a compatibility alias for `-WorkspaceRoot` for this migration:
  - [x] If both are supplied and differ, fail with problem/cause/fix.
  - [x] If only `-ProjectRoot` is supplied, print deprecation guidance.
  - [x] Do not interpret `-ProjectRoot` as server project root.
- [x] Update log output to print all derived paths at high-signal entrypoints.
- [x] Add tests for path derivation:
  - [x] Normal project name.
  - [x] Unsafe project name rejected before writes.
  - [x] Workspace under `artifacts/storefront-builder/generated`.
  - [x] Workspace under `obj/storefront-builder/generated`.
  - [x] Conflict between `-ProjectRoot` and `-WorkspaceRoot`.

Exit criteria:

- [x] Scripts can identify workspace, server, WASM, solution, and analysis roots without guessing.
- [x] Existing operators get a clear migration message instead of a silent behavior change.

Phase 3 evidence:

- Added `Resolve-StorefrontBuilderWorkspacePaths` and `Write-StorefrontBuilderWorkspacePaths` to `StorefrontBuilderProjectSafety.ps1`.
- Added `-WorkspaceRoot` to `validate-storefront.ps1`, `regenerate-storefront.ps1`, and `run-storefront-builder-isolation-gate.ps1`; `-ProjectRoot` now warns as a temporary alias and conflicts with `-WorkspaceRoot` fail with `SFB-PROJECT-014`.
- Added `--workspace-root` to `run-visual-qa.mjs`, `repair-visual-generation.mjs`, `record-agent-visual-writes.mjs`, and `write-agent-task-package.mjs`; `--project-root` remains an alias.
- Added `-WorkspaceRoot` to `run-storefront-phase4-mvp-gate.ps1` and updated internal calls to workspace arguments.
- `Test-StorefrontBuilderWorkspacePaths.ps1` passed.
- `run-storefront-builder-isolation-gate.ps1 -WorkspaceRoot ... -Describe`, Phase 4 MVP `-Help`, Node helper `--help`, and `build-storefront.ps1 -Mode plan-only` passed.

## Phase 4 - Generator Rewrite

Goal: generate a sibling server/WASM workspace and solution from Starter sources.

Tasks:

- [ ] Update `scripts/generate-storefront-sample.ps1` to create:
  - [ ] `WorkspaceRoot`
  - [ ] `WorkspaceRoot/{ProjectName}`
  - [ ] `WorkspaceRoot/{ProjectName}.WASM`
  - [ ] `WorkspaceRoot/docs/storefront-analysis`
- [ ] Copy Starter server into `ServerProjectRoot`.
- [ ] Copy Starter.WASM into `WasmProjectRoot`.
- [ ] Copy or create workspace-level shared files:
  - [ ] `StorefrontPackageVersions.props`
  - [ ] `nuget.config`
  - [ ] `docs/storefront-analysis/metadata.yaml`
  - [ ] `docs/storefront-analysis/generated-files.yaml`
  - [ ] `docs/storefront-analysis/asset-manifest.yaml`
  - [ ] `docs/storefront-analysis/starter-generation.contract.yaml`
- [ ] Rename server project:
  - [ ] File name to `{ProjectName}.csproj`.
  - [ ] Assembly/root namespace to `{ProjectName}`.
  - [ ] Razor/component namespaces.
  - [ ] Static asset base path if present.
- [ ] Rename WASM project:
  - [ ] File name to `{ProjectName}.WASM.csproj`.
  - [ ] Assembly/root namespace to `{ProjectName}.WASM`.
  - [ ] Razor/component namespaces.
  - [ ] Browser runtime namespace imports.
- [ ] Rewrite generated server references:
  - [ ] Presentation, Components, Browser to PackageReferences when independent proof mode requires package consumption.
  - [ ] Sibling WASM as `<ProjectReference Include="..\{ProjectName}.WASM\{ProjectName}.WASM.csproj" />`.
  - [ ] No Runtime/Client direct source references unless explicitly approved in contract.
- [ ] Rewrite generated WASM references:
  - [ ] Browser and Components to package references.
  - [ ] No Runtime/Client references.
  - [ ] No server project reference.
- [ ] Remove the old nested-WASM exclusion ItemGroup generation completely.
- [ ] Add a generated `.sln` at workspace root:
  - [ ] Contains server project.
  - [ ] Contains WASM project.
  - [ ] Does not include V2, Starter, backend, Control Plane, Commerce Node, or generated proof outputs from other stores.
  - [ ] Uses deterministic project order.
  - [ ] Uses standard `.sln` output rather than `.slnx` unless repo intentionally moves to `.slnx`.
- [ ] Update generated README or operator notes:
  - [ ] Restore/build solution from workspace root.
  - [ ] Run server project from sibling folder.
  - [ ] Explain analysis artifacts under workspace `docs/storefront-analysis`.
- [ ] Update `build-storefront.ps1`:
  - [ ] `OutputRoot/ProjectName` means workspace root.
  - [ ] `-Mode generate` creates workspace.
  - [ ] `-Mode full` validates workspace after generation.
  - [ ] `-Mode validate-only` accepts workspace root.
- [ ] Ensure `-Force` deletes/replaces only the target workspace root after path safety checks.
- [ ] Ensure generated output is not added to `BlazorShop.sln`.

Exit criteria:

- [ ] Fresh generation produces the canonical workspace tree.
- [ ] Fresh generation has no nested `{ProjectName}.WASM` folder under server root.
- [ ] Fresh generation has no generated WASM exclusion ItemGroups.
- [ ] `dotnet restore {WorkspaceRoot}/{ProjectName}.sln` succeeds.
- [ ] `dotnet build {WorkspaceRoot}/{ProjectName}.sln --no-restore` succeeds.

## Phase 5 - Package Version And Provenance Alignment

Goal: keep generated proof package consumption deterministic across both sibling projects.

Tasks:

- [ ] Ensure the generated workspace imports one shared `StorefrontPackageVersions.props`.
- [ ] Ensure both generated server and generated WASM import the workspace props via a correct relative path.
- [ ] Keep one generator version source:
  - [ ] `tools/BlazorShop.AI.StorefrontBuilder/version.json`.
  - [ ] `metadata.yaml` reads the same version.
  - [ ] generated-file manifest reads the same version.
  - [ ] reports read the same version.
- [ ] Pack local packages for proof:
  - [ ] `BlazorShop.Storefront.Client`
  - [ ] `BlazorShop.Storefront.Runtime`
  - [ ] `BlazorShop.Storefront.Presentation`
  - [ ] `BlazorShop.Storefront.Components`
  - [ ] `BlazorShop.Storefront.Browser`
- [ ] Verify generated server package closure:
  - [ ] Presentation package present.
  - [ ] Components package present if server visual templates compile against shared contracts.
  - [ ] Browser package present only if server-side controller registration needs compile-time Browser extension types.
  - [ ] Runtime and Client present transitively or as metadata according to current contract.
- [ ] Verify generated WASM package closure:
  - [ ] Browser package present.
  - [ ] Components package present.
  - [ ] Runtime package absent from direct references.
  - [ ] Client package absent from direct references.
- [ ] Record package version and hash evidence in generated metadata.
- [ ] Verify package version evidence is workspace-wide, not duplicated differently in server and WASM metadata.

Exit criteria:

- [ ] Server and WASM restore from the same package version source.
- [ ] No generator version drift remains.
- [ ] Package proof reflects the actual workspace shape.

## Phase 6 - Static Validator And Isolation Gate Rewrite

Goal: make validation prove the new shape and fail the old nested shape.

Tasks:

- [ ] Update `validate-storefront.ps1` to call validation with workspace semantics.
- [ ] Update `Test-StorefrontBuilderGeneratedProject.ps1` path assumptions:
  - [ ] Project root parameter means workspace root.
  - [ ] Server project path is `{WorkspaceRoot}/{Name}/{Name}.csproj`.
  - [ ] WASM project path is `{WorkspaceRoot}/{Name}.WASM/{Name}.WASM.csproj`.
  - [ ] Analysis root is `{WorkspaceRoot}/docs/storefront-analysis`.
- [ ] Replace required exclusion-marker checks:
  - [ ] Remove checks that require `Compile Remove="{Name}.WASM\**"`.
  - [ ] Remove checks that require `Content Remove="{Name}.WASM\**"`.
  - [ ] Remove checks that require `EmbeddedResource Remove="{Name}.WASM\**"`.
  - [ ] Remove checks that require `None Remove="{Name}.WASM\**"`.
  - [ ] Add negative checks that fail if those exclusions exist.
- [ ] Add structure checks:
  - [ ] Workspace root exists.
  - [ ] Solution file exists.
  - [ ] Server project exists in sibling server folder.
  - [ ] WASM project exists in sibling WASM folder.
  - [ ] No nested WASM folder exists under server project root.
  - [ ] `docs/storefront-analysis` exists at workspace root.
- [ ] Add solution checks:
  - [ ] Solution contains server project.
  - [ ] Solution contains WASM project.
  - [ ] Solution does not contain forbidden monorepo projects.
  - [ ] Solution does not contain generated projects from another output root.
- [ ] Add reference checks:
  - [ ] Server ProjectReference to sibling WASM uses `..\{Name}.WASM\{Name}.WASM.csproj`.
  - [ ] Server does not reference Starter.WASM.
  - [ ] Server does not reference Storefront.V2.
  - [ ] Server does not reference backend/core/API projects.
  - [ ] WASM does not reference server project.
  - [ ] WASM does not reference Runtime or Client.
- [ ] Update isolation gate:
  - [ ] Restore/build solution instead of individual nested project first.
  - [ ] Scan both server and WASM project trees.
  - [ ] Scan workspace shared files.
  - [ ] Preserve package boundary proof.
  - [ ] Fail with problem/cause/fix messages.
- [ ] Update architecture tests or add focused tests for:
  - [ ] Old nested shape fails.
  - [ ] New workspace shape passes.
  - [ ] Old exclusion ItemGroups fail.
  - [ ] Forbidden reference in server fails.
  - [ ] Forbidden reference in WASM fails.

Exit criteria:

- [ ] Static validation passes on fresh workspace output.
- [ ] Static validation fails on old nested output with actionable guidance.
- [ ] Isolation gate passes on fresh workspace output.

## Phase 7 - Manifest, Generation Plan, And Ownership Rewrite

Goal: make all file ownership and visual task artifacts workspace-aware.

Tasks:

- [ ] Update `plan-generation-files.mjs`:
  - [ ] Model workspace root explicitly.
  - [ ] Model server project explicitly.
  - [ ] Model WASM project explicitly.
  - [ ] Emit workspace-relative paths.
  - [ ] Emit project ownership metadata, not substring-derived ownership.
- [ ] Update `generated-file-manifest.mjs`:
  - [ ] Stop using `.WASM/` substring detection as the source of truth.
  - [ ] Add fields such as `workspaceRelativePath`, `projectKind`, `projectName`, and `projectRelativePath`.
  - [ ] Preserve generated-owned, user-owned, protected, and artifact-only categories.
  - [ ] Treat workspace shared files as workspace-owned, not server-owned.
- [ ] Update handoff generation plan compiler:
  - [ ] Allowed visual files may target server project or WASM project explicitly.
  - [ ] Protected files may target workspace, server, or WASM explicitly.
  - [ ] Agent task package records workspace-relative target paths.
- [ ] Update `write-agent-task-package` logic:
  - [ ] Include both project roots.
  - [ ] Include allowed target project kind.
  - [ ] Reject ambiguous target paths.
- [ ] Update `record-agent-visual-writes.mjs`:
  - [ ] Accept `--workspace-root`.
  - [ ] Keep `--project-root` alias with deprecation guidance.
  - [ ] Validate writes against workspace-relative manifest paths.
  - [ ] Reject writes into workspace shared files unless explicitly allowed by a foundation task.
  - [ ] Reject writes into generated server app/route/BFF protected files.
  - [ ] Reject writes into generated WASM transport/bootstrap protected files.
- [ ] Update visual repair scripts:
  - [ ] Read project roots from manifest/metadata.
  - [ ] Preserve workspace-relative path reporting.
  - [ ] Avoid applying repair patches to the wrong sibling project.
- [ ] Update visual QA scripts:
  - [ ] Resolve generated CSS/asset paths against server project root.
  - [ ] Resolve analysis artifacts against workspace root.
  - [ ] Resolve browser/WASM artifact expectations against WASM project root where needed.
- [ ] Update metadata schema:
  - [ ] Store `workspaceLayoutVersion`.
  - [ ] Store `serverProjectRoot`.
  - [ ] Store `wasmProjectRoot`.
  - [ ] Store `solutionPath`.
  - [ ] Store `analysisRoot`.

Exit criteria:

- [ ] Every generated file can be traced to workspace, server, or WASM ownership.
- [ ] Visual agent tasks cannot write to the wrong sibling project.
- [ ] Manifest and generation plan can validate without path substring guesses.

## Phase 8 - Regeneration Workspace Migration

Goal: make regeneration create, compare, report, and apply changes against the workspace shape safely.

Tasks:

- [ ] Update `regenerate-storefront.ps1` parameters:
  - [ ] Add `-WorkspaceRoot`.
  - [ ] Keep `-ProjectRoot` alias for one cycle.
  - [ ] Validate old nested shape and fail with "regenerate fresh" guidance unless an explicit upgrade path is approved.
- [ ] Update candidate generation:
  - [ ] Candidate root is a generated workspace.
  - [ ] Candidate contains solution, server, WASM, shared props, nuget config, and analysis docs.
  - [ ] Candidate cleanup does not delete stable WhatIf reports.
- [ ] Update comparison logic:
  - [ ] Compare workspace-relative paths.
  - [ ] Compare server files under `{Name}/`.
  - [ ] Compare WASM files under `{Name}.WASM/`.
  - [ ] Compare shared metadata under workspace root.
  - [ ] Preserve user-owned files in either sibling project.
  - [ ] Report obsolete files in either sibling project.
- [ ] Update protected-file logic:
  - [ ] Workspace shared files protected unless `-Scope foundation`.
  - [ ] Server bootstrapping files protected unless approved.
  - [ ] WASM bootstrapping files protected unless approved.
  - [ ] Visual files may be updated only when generated-owned.
- [ ] Update conflict logic:
  - [ ] Manual edit conflicts include workspace-relative path.
  - [ ] Conflict report includes project kind.
  - [ ] Conflict guidance says whether to keep user edit, rerun scoped generation, or run foundation upgrade.
- [ ] Preserve WhatIf behavior from prior closure:
  - [ ] Print plan lines to console.
  - [ ] Write stable report outside target workspace.
  - [ ] Do not require `SFB_KEEP_REGENERATION_CANDIDATE_ARTIFACTS` for normal report access.
- [ ] Update `-ValidateAfterApply` to run static validation against workspace root.
- [ ] Update `-BuildAfterApply` to build solution.
- [ ] Update regeneration ownership tests:
  - [ ] No-op regeneration stays deterministic.
  - [ ] Server visual scoped update works.
  - [ ] WASM visual scoped update works if such files are generated-owned.
  - [ ] Foundation scoped update can update shared props/contract metadata.
  - [ ] Manual edit conflict works in server project.
  - [ ] Manual edit conflict works in WASM project.
  - [ ] Obsolete file reported in server project.
  - [ ] Obsolete file reported in WASM project.
  - [ ] Old nested artifact fails with clear guidance.

Exit criteria:

- [ ] Regeneration can update a fresh workspace output.
- [ ] Regeneration does not apply files outside the target workspace.
- [ ] WhatIf remains useful after candidate cleanup.

## Phase 9 - QA Runner And Browser Proof Update

Goal: make all StorefrontBuilder proof runners use the workspace shape and still test real browser behavior.

Tasks:

- [ ] Update `scripts/qa/run-storefront-builder-generated-proof.ps1`:
  - [ ] Generate workspace output.
  - [ ] Restore solution.
  - [ ] Build solution.
  - [ ] Run static validation with workspace root.
  - [ ] Run isolation gate with workspace root.
  - [ ] Run regeneration proof with workspace root.
  - [ ] Start generated server from server project path.
- [ ] Update `FoundationFunctionalFast` flow:
  - [ ] Mock same-origin BFF routes still work.
  - [ ] Product descriptors still render.
  - [ ] Product selection preview still works.
  - [ ] Add-to-cart still works.
  - [ ] Cart badge still works.
  - [ ] Cart page still works.
  - [ ] Checkout route still works.
  - [ ] Account route still works if covered by current browser package.
  - [ ] Consent save/revoke still works.
  - [ ] Direct Commerce Node browser calls remain rejected.
- [ ] Update `FoundationFunctionalFull` flow:
  - [ ] Generated host starts from server project under workspace.
  - [ ] Fixture-backed store/category/product/page/payment data still load.
  - [ ] COD checkout proof remains place-order capable when fixture runtime is available.
  - [ ] SEO, sitemap, robots, missing slug, and consent flows still run.
- [ ] Update `scripts/qa/run-storefront-builder-isolation-gate.ps1`:
  - [ ] Restore/build solution.
  - [ ] Pack required Storefront packages once.
  - [ ] Validate both generated projects consume packages correctly.
  - [ ] Scan both sibling project trees for forbidden references.
- [ ] Update `scripts/qa/run-storefront-builder-regeneration-gate.ps1`.
- [ ] Update `scripts/qa/run-storefront-phase4-mvp-gate.ps1`.
- [ ] Update `scripts/qa/run-storefront-phase4-final-closure-gate.ps1`.
- [ ] Update `scripts/qa/run-storefront-builder-full-proof-with-fixture.ps1`.
- [ ] Update browser QA commands in docs to use workspace root.
- [ ] If any script still needs a server project path, name the parameter `-ServerProjectRoot`, not `-ProjectRoot`.

Exit criteria:

- [ ] Structure proof passes locally.
- [ ] FoundationFunctionalFast passes locally.
- [ ] Full fixture proof command has correct paths and `-Describe` works even when fixture runtime is not started.
- [ ] No proof runner assumes nested WASM output.

## Phase 10 - Documentation And Operator Workflow

Goal: make docs, examples, and agent guidance match the new behavior.

Tasks:

- [ ] Update `docs/architecture/11-storefront-builder.md`:
  - [ ] Generated artifact tree shows workspace with solution, sibling server, sibling WASM, and workspace docs.
  - [ ] Entrypoints use `-WorkspaceRoot` where applicable.
  - [ ] `-ProjectRoot` is documented only as temporary alias.
  - [ ] Regeneration examples use workspace root.
  - [ ] Validation examples use workspace root.
  - [ ] Isolation examples use workspace root.
- [ ] Update `docs/agents/storefront-builder.md`:
  - [ ] Generated storefront requirements mention workspace shape.
  - [ ] Validation commands use workspace root.
  - [ ] Browser QA commands use workspace root.
  - [ ] Old nested-WASM wording removed.
- [ ] Update `docs/visual-reverse-engineering-skill/README.md`.
- [ ] Update `docs/visual-reverse-engineering-skill/reference.md`.
- [ ] Update `docs/visual-reverse-engineering-skill/how-to-generate-and-validate.md`.
- [ ] Update `docs/visual-reverse-engineering-skill/tutorial-generated-proof.md`.
- [ ] Update `docs/visual-reverse-engineering-skill/explanation-boundaries-and-regeneration.md`.
- [ ] Add a factual note to plan 24 or related historical summary:
  - [ ] Plan 24 was superseded by workspace migration.
  - [ ] Historical checked items must not be used as current closure evidence.
- [ ] Update any README snippets emitted into generated output.
- [ ] Ensure all examples use:
  - [ ] `dotnet restore <workspace>/<name>.sln`
  - [ ] `dotnet build <workspace>/<name>.sln --no-restore`
  - [ ] `dotnet run --project <workspace>/<name>/<name>.csproj`

Exit criteria:

- [ ] A developer following docs does not create or validate the old nested shape.
- [ ] An agent can identify workspace, server, WASM, and analysis paths from docs alone.

## Phase 11 - Compatibility Cleanup And Guardrails

Goal: remove old assumptions and prevent regression.

Tasks:

- [ ] Search and eliminate active old-shape assumptions:
  - [ ] `Compile Remove=.*WASM`
  - [ ] `Content Remove=.*WASM`
  - [ ] `EmbeddedResource Remove=.*WASM`
  - [ ] `None Remove=.*WASM`
  - [ ] `{Name}.WASM/{Name}.WASM.csproj` as nested path
  - [ ] `{Name}.WASM\\{Name}.WASM.csproj` as nested path
  - [ ] `Join-Path $projectRoot "$Name.WASM"`
  - [ ] `rootPath: "."` for generated server project ownership
  - [ ] `.WASM/` substring as project ownership truth
- [ ] Keep historical docs only if they are clearly marked as historical and not current instructions.
- [ ] Add guardrail tests:
  - [ ] Validator fails if nested WASM folder exists under server root.
  - [ ] Validator fails if exclusion ItemGroups are present.
  - [ ] Validator fails if solution is missing.
  - [ ] Validator fails if solution contains unexpected projects.
  - [ ] Regeneration fails old nested workspace with clear guidance.
- [ ] Add static source scan in QA:
  - [ ] StorefrontBuilder scripts do not create nested WASM output.
  - [ ] StorefrontBuilder scripts do not generate WASM exclusion ItemGroups.
  - [ ] Manifest code does not infer ownership only from `.WASM/`.
- [ ] Update failure messages:
  - [ ] Problem: what failed.
  - [ ] Cause: old nested shape or missing workspace file.
  - [ ] Fix: regenerate fresh or pass workspace root.

Exit criteria:

- [ ] Old nested shape cannot silently pass validation.
- [ ] Old nested generation code is removed from active scripts.
- [ ] Guardrails explain exactly how to fix failed output.

## Phase 12 - Fresh GeneratedProof And Kindredcoast Proof

Goal: prove the new workspace output with both canonical proof and a real named generated storefront scenario.

Tasks:

- [ ] Recreate canonical generated proof under ignored output:
  - [ ] Delete only the exact proof workspace after path safety validation.
  - [ ] Generate fresh `BlazorShop.Storefront.GeneratedProof`.
  - [ ] Verify solution file exists.
  - [ ] Verify sibling server and WASM projects exist.
  - [ ] Verify no nested WASM under server root.
- [ ] Run structure proof:
  - [ ] `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/qa/run-storefront-builder-generated-proof.ps1 -ProofLevel Structure`
- [ ] Run fast browser proof:
  - [ ] `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/qa/run-storefront-builder-generated-proof.ps1 -ProofLevel FoundationFunctionalFast`
- [ ] Run regeneration proof:
  - [ ] No-op regeneration.
  - [ ] Manual edit conflict.
  - [ ] Foundation scope.
  - [ ] WhatIf report outside target.
- [ ] Run isolation gate:
  - [ ] `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/qa/run-storefront-builder-isolation-gate.ps1 -WorkspaceRoot artifacts/storefront-builder/generated/BlazorShop.Storefront.GeneratedProof -Name BlazorShop.Storefront.GeneratedProof`
- [ ] Run `-Describe` on full fixture proof:
  - [ ] Verify paths show workspace shape.
  - [ ] Verify generated host port and project path are correct.
- [ ] If fixture runtime is available, run full fixture proof.
- [ ] Generate a real named storefront proof such as `BlazorShop.Storefront.Kindredcoast`:
  - [ ] Fresh generation.
  - [ ] Solution restore/build.
  - [ ] Static validation.
  - [ ] Visual QA if handoff/fixture package is available.
  - [ ] Confirm not added to `BlazorShop.sln`.
- [ ] Record local evidence in generated reports only, unless a tracked closure summary is explicitly required.

Exit criteria:

- [ ] Canonical generated proof passes.
- [ ] Real named generated output proves the path is not hardcoded to `GeneratedProof`.
- [ ] Browser behavior remains tested with Playwright, not only smoke tests.

## Phase 13 - Final Closure Review

Goal: close this migration only when code, docs, tests, and generated artifacts agree.

Tasks:

- [ ] Run focused unit/integration tests:
  - [ ] `dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~StorefrontBuilder"`
  - [ ] StorefrontReverseEngineering StorefrontBuilder handoff tests if touched.
- [ ] Run build checks:
  - [ ] Starter server build.
  - [ ] Starter.WASM build.
  - [ ] Generated proof solution build.
- [ ] Run static gates:
  - [ ] validate-storefront.
  - [ ] isolation gate.
  - [ ] regeneration gate.
- [ ] Run browser gates:
  - [ ] FoundationFunctionalFast.
  - [ ] Runtime visual QA when visual-generation paths are touched.
  - [ ] Full fixture proof when fixture runtime is available.
- [ ] Run final source scans:
  - [ ] No active nested-WASM generation.
  - [ ] No active required exclusion markers.
  - [ ] No active docs instruct nested validation.
  - [ ] No generated output added to `BlazorShop.sln`.
- [ ] Review diff manually:
  - [ ] No unrelated user changes reverted.
  - [ ] No generated artifacts committed unless intentionally tracked.
  - [ ] No package lock churn outside expected proof metadata.
- [ ] Update this todo file with completed checkboxes during implementation.
- [ ] Commit with a message that names the migration, for example:
  - [ ] `refactor(storefront-builder): generate starter workspaces with sibling wasm`

Exit criteria:

- [ ] All applicable local gates pass.
- [ ] Docs and scripts use the same workspace vocabulary.
- [ ] A fresh generated storefront can be restored, built, validated, regenerated, and browser-tested.
- [ ] Old nested-WASM output is rejected, not supported silently.

## File-Level Implementation Checklist

Expected files to inspect and likely update:

- [ ] `starter-generation.contract.yaml`
- [ ] `scripts/generate-storefront-sample.ps1`
- [ ] `tools/BlazorShop.AI.StorefrontBuilder/build-storefront.ps1`
- [ ] `tools/BlazorShop.AI.StorefrontBuilder/validate-storefront.ps1`
- [ ] `tools/BlazorShop.AI.StorefrontBuilder/regenerate-storefront.ps1`
- [ ] `tools/BlazorShop.AI.StorefrontBuilder/scripts/validate/Test-StorefrontBuilderGeneratedProject.ps1`
- [ ] `tools/BlazorShop.AI.StorefrontBuilder/scripts/generate/plan-generation-files.mjs`
- [ ] `tools/BlazorShop.AI.StorefrontBuilder/scripts/generate/generated-file-manifest.mjs`
- [ ] `tools/BlazorShop.AI.StorefrontBuilder/scripts/generate/write-agent-task-package.mjs`
- [ ] `tools/BlazorShop.AI.StorefrontBuilder/scripts/generate/record-agent-visual-writes.mjs`
- [ ] `tools/BlazorShop.AI.StorefrontBuilder/scripts/qa/run-visual-qa.mjs`
- [ ] `tools/BlazorShop.AI.StorefrontBuilder/scripts/qa/repair-visual-generation.mjs`
- [ ] `scripts/qa/run-storefront-builder-generated-proof.ps1`
- [ ] `scripts/qa/run-storefront-builder-isolation-gate.ps1`
- [ ] `scripts/qa/run-storefront-builder-regeneration-gate.ps1`
- [ ] `scripts/qa/run-storefront-builder-full-proof-with-fixture.ps1`
- [ ] `scripts/qa/run-storefront-phase4-mvp-gate.ps1`
- [ ] `scripts/qa/run-storefront-phase4-final-closure-gate.ps1`
- [ ] `docs/architecture/11-storefront-builder.md`
- [ ] `docs/agents/storefront-builder.md`
- [ ] `docs/visual-reverse-engineering-skill/README.md`
- [ ] `docs/visual-reverse-engineering-skill/reference.md`
- [ ] `docs/visual-reverse-engineering-skill/how-to-generate-and-validate.md`
- [ ] `docs/visual-reverse-engineering-skill/tutorial-generated-proof.md`
- [ ] `docs/visual-reverse-engineering-skill/explanation-boundaries-and-regeneration.md`

Optional only if Phase 2 chooses marker class cleanup:

- [ ] `BlazorShop.PresentationV2/BlazorShop.Storefront.Starter.WASM/.../StarterWasmAssemblyMarker.cs`
- [ ] `BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/Program.cs`

## Negative Checks

The phase is not complete if any active source path still does one of these:

- [ ] Generates `{WorkspaceRoot}/{ProjectName}/{ProjectName}.WASM`.
- [ ] Requires server project exclusion markers for nested WASM.
- [ ] Builds only `{WorkspaceRoot}/{ProjectName}.csproj` while ignoring sibling WASM.
- [ ] Treats `docs/storefront-analysis` as server-project-local.
- [ ] Infers file ownership solely from `.WASM/` substring matching.
- [ ] Uses `ProjectRoot` to mean both workspace root and server project root in the same script.
- [ ] Allows generated WASM to reference Runtime or Client.
- [ ] Allows generated output to reference Storefront V2.
- [ ] Allows generated output to reference backend/core/API projects.
- [ ] Writes generated visual changes into Starter.

## QA Matrix

| Area | Required proof | Command or check |
| --- | --- | --- |
| Starter source | Server and WASM build | `dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/BlazorShop.Storefront.Starter.csproj`; `dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.Starter.WASM/BlazorShop.Storefront.Starter.WASM.csproj` |
| Generation shape | Fresh workspace output | `scripts/generate-storefront-sample.ps1` through the normal builder entrypoint |
| Solution build | Restore/build generated solution | `dotnet restore <workspace>/<name>.sln --no-cache --force-evaluate`; `dotnet build <workspace>/<name>.sln --no-restore` |
| Static validation | New shape passes, old shape fails | `tools/BlazorShop.AI.StorefrontBuilder/validate-storefront.ps1 -WorkspaceRoot <workspace> -Name <name> -StoreKey <store>` |
| Isolation | Package and forbidden reference proof | `scripts/qa/run-storefront-builder-isolation-gate.ps1 -WorkspaceRoot <workspace> -Name <name>` |
| Regeneration | No-op, conflict, foundation, WhatIf | `tools/BlazorShop.AI.StorefrontBuilder/regenerate-storefront.ps1 -WorkspaceRoot <workspace> -Scope all -WhatIf`; apply variants with `-ValidateAfterApply -BuildAfterApply` |
| Fast browser behavior | Playwright functional proof | `scripts/qa/run-storefront-builder-generated-proof.ps1 -ProofLevel FoundationFunctionalFast` |
| Full commerce behavior | Fixture-backed proof when available | `scripts/qa/run-storefront-builder-full-proof-with-fixture.ps1` |
| Visual handoff behavior | Runtime visual QA if visual paths touched | `scripts/qa/run-storefront-phase4-mvp-gate.ps1` and `scripts/qa/run-storefront-phase4-final-closure-gate.ps1` as applicable |
| Docs | Commands match workspace shape | Manual docs scan plus `rg` source scan |

## Release Definition Of Done

- [ ] Generated output uses workspace root with `.sln`, shared props, `nuget.config`, workspace `docs/storefront-analysis`, sibling server project, and sibling WASM project.
- [ ] Generated server references generated sibling WASM with `..\{Name}.WASM\{Name}.WASM.csproj`.
- [ ] Generated server does not contain nested-WASM exclusion ItemGroups.
- [ ] Generated WASM has no Runtime or Client reference.
- [ ] Generated workspace does not reference Storefront V2, backend/core/API projects, Control Plane Web, or `Web.SharedV2`.
- [ ] Static validator rejects old nested output.
- [ ] Isolation gate validates both sibling projects.
- [ ] Regeneration compares and applies workspace-relative paths.
- [ ] WhatIf report remains stable after candidate cleanup.
- [ ] Browser proof uses Playwright and validates real interactions, not smoke-only rendering.
- [ ] Docs and agent guide use `WorkspaceRoot` vocabulary.
- [ ] Historical plan 24 no longer acts as current closure evidence.
- [ ] GitHub Actions evidence is not required while Actions are intentionally disabled.

## Notes For Implementing Agents

- Start by changing path contracts and tests before moving generation output.
- Keep all path handling explicit; avoid deriving project kind from folder name substrings.
- Use `rg` before deleting old shape assumptions because they appear in PowerShell, Node, docs, tests, and validation scripts.
- Preserve unrelated worktree changes. Do not revert user modifications such as existing `BlazorShop.sln` changes.
- Use `apply_patch` for manual edits and use native PowerShell path APIs for filesystem operations.
- Generated artifacts under `artifacts/` or `obj/` are disposable proof output; do not commit them unless a phase explicitly promotes evidence.
- Prefer local gate evidence over GitHub Actions in this phase because Actions are disabled during development.

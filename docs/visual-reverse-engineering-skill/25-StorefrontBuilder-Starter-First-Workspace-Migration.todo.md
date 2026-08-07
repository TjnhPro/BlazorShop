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

- [x] Update `scripts/generate-storefront-sample.ps1` to create:
  - [x] `WorkspaceRoot`
  - [x] `WorkspaceRoot/{ProjectName}`
  - [x] `WorkspaceRoot/{ProjectName}.WASM`
  - [x] `WorkspaceRoot/docs/storefront-analysis`
- [x] Copy Starter server into `ServerProjectRoot`.
- [x] Copy Starter.WASM into `WasmProjectRoot`.
- [x] Copy or create workspace-level shared files:
  - [x] `StorefrontPackageVersions.props`
  - [x] `nuget.config`
  - [x] `docs/storefront-analysis/metadata.yaml`
  - [x] `docs/storefront-analysis/generated-files.yaml`
  - [x] `docs/storefront-analysis/asset-manifest.yaml`
  - [x] `docs/storefront-analysis/starter-generation.contract.yaml`
- [x] Rename server project:
  - [x] File name to `{ProjectName}.csproj`.
  - [x] Assembly/root namespace to `{ProjectName}`.
  - [x] Razor/component namespaces.
  - [x] Static asset base path if present.
- [x] Rename WASM project:
  - [x] File name to `{ProjectName}.WASM.csproj`.
  - [x] Assembly/root namespace to `{ProjectName}.WASM`.
  - [x] Razor/component namespaces.
  - [x] Browser runtime namespace imports.
- [x] Rewrite generated server references:
  - [x] Presentation, Components, Browser to PackageReferences when independent proof mode requires package consumption.
  - [x] Sibling WASM as `<ProjectReference Include="..\{ProjectName}.WASM\{ProjectName}.WASM.csproj" />`.
  - [x] No Runtime/Client direct source references unless explicitly approved in contract.
- [x] Rewrite generated WASM references:
  - [x] Browser and Components to package references.
  - [x] No Runtime/Client references.
  - [x] No server project reference.
- [x] Remove the old nested-WASM exclusion ItemGroup generation completely.
- [x] Add a generated `.sln` at workspace root:
  - [x] Contains server project.
  - [x] Contains WASM project.
  - [x] Does not include V2, Starter, backend, Control Plane, Commerce Node, or generated proof outputs from other stores.
  - [x] Uses deterministic project order.
  - [x] Uses standard `.sln` output rather than `.slnx` unless repo intentionally moves to `.slnx`.
- [x] Update generated README or operator notes:
  - [x] Restore/build solution from workspace root.
  - [x] Run server project from sibling folder.
  - [x] Explain analysis artifacts under workspace `docs/storefront-analysis`.
- [x] Update `build-storefront.ps1`:
  - [x] `OutputRoot/ProjectName` means workspace root.
  - [x] `-Mode generate` creates workspace.
  - [x] `-Mode full` validates workspace after generation.
  - [x] `-Mode validate-only` accepts workspace root.
- [x] Ensure `-Force` deletes/replaces only the target workspace root after path safety checks.
- [x] Ensure generated output is not added to `BlazorShop.sln`.

Exit criteria:

- [x] Fresh generation produces the canonical workspace tree.
- [x] Fresh generation has no nested `{ProjectName}.WASM` folder under server root.
- [x] Fresh generation has no generated WASM exclusion ItemGroups.
- [x] `dotnet restore {WorkspaceRoot}/{ProjectName}.sln` succeeds.
- [x] `dotnet build {WorkspaceRoot}/{ProjectName}.sln --no-restore` succeeds.

Phase 4 evidence:

- `scripts/generate-storefront-sample.ps1` now writes the canonical workspace with sibling server and WASM projects, workspace-level shared files, workspace-level analysis artifacts, and a standard `.sln`.
- `build-storefront.ps1 -Mode generate -Name Phase4WrapperProof -StoreKey sample -OutputRoot obj/storefront-builder/generated -Force` passed and wrote visual CSS/assets under the generated server project while keeping analysis under workspace `docs/storefront-analysis`.
- `build-storefront.ps1 -Mode validate-only -Name Phase4WrapperProof -StoreKey sample -OutputRoot obj/storefront-builder/generated` passed schema, generated project, asset, CSS, composition, guard, idempotency, and static validation.
- `build-storefront.ps1 -Mode full -Name Phase4FullProof -StoreKey sample -OutputRoot obj/storefront-builder/generated -Force -SkipVisualQa -SkipCommerceRegression` passed generation and validation in one command.
- `dotnet restore obj/storefront-builder/generated/BlazorShop.Storefront.Phase4WrapperProof/BlazorShop.Storefront.Phase4WrapperProof.sln --no-cache --force-evaluate` passed.
- `dotnet build obj/storefront-builder/generated/BlazorShop.Storefront.Phase4WrapperProof/BlazorShop.Storefront.Phase4WrapperProof.sln --no-restore` passed with 0 warnings and 0 errors.
- Scan of `Phase4FullProof` found no nested server `{ProjectName}.WASM` folder, no generated WASM exclusion ItemGroups, and no forbidden V2, Starter, backend, Control Plane, or Commerce Node references.

## Phase 5 - Package Version And Provenance Alignment

Goal: keep generated proof package consumption deterministic across both sibling projects.

Tasks:

- [x] Ensure the generated workspace imports one shared `StorefrontPackageVersions.props`.
- [x] Ensure both generated server and generated WASM import the workspace props via a correct relative path.
- [x] Keep one generator version source:
  - [x] `tools/BlazorShop.AI.StorefrontBuilder/version.json`.
  - [x] `metadata.yaml` reads the same version.
  - [x] generated-file manifest reads the same version.
  - [x] reports read the same version.
- [x] Pack local packages for proof:
  - [x] `BlazorShop.Storefront.Client`
  - [x] `BlazorShop.Storefront.Runtime`
  - [x] `BlazorShop.Storefront.Presentation`
  - [x] `BlazorShop.Storefront.Components`
  - [x] `BlazorShop.Storefront.Browser`
- [x] Verify generated server package closure:
  - [x] Presentation package present.
  - [x] Components package present if server visual templates compile against shared contracts.
  - [x] Browser package present only if server-side controller registration needs compile-time Browser extension types.
  - [x] Runtime and Client present transitively or as metadata according to current contract.
- [x] Verify generated WASM package closure:
  - [x] Browser package present.
  - [x] Components package present.
  - [x] Runtime package absent from direct references.
  - [x] Client package absent from direct references.
- [x] Record package version and hash evidence in generated metadata.
- [x] Verify package version evidence is workspace-wide, not duplicated differently in server and WASM metadata.

Exit criteria:

- [x] Server and WASM restore from the same package version source.
- [x] No generator version drift remains.
- [x] Package proof reflects the actual workspace shape.

Phase 5 evidence:

- Generated server and WASM projects both import `..\StorefrontPackageVersions.props`; no project-local duplicate props file is used.
- `write-review-artifacts.mjs`, `metadata.yaml`, and `generated-files.yaml` all read/report generator version `2.5.0` from `tools/BlazorShop.AI.StorefrontBuilder/version.json`; regeneration safety test now reads the expected version from that file instead of hardcoding it.
- `run-storefront-builder-isolation-gate.ps1 -WorkspaceRoot obj/storefront-builder/generated/BlazorShop.Storefront.Phase5PackageProof -Name Phase5PackageProof` passed after packing Client, Runtime, Presentation, Components, and Browser as `1.0.0-local.0c9a8e1ec071`.
- Generated metadata records package versions and SHA-256 hashes for all five Storefront packages, with feed path `../../../../artifacts/storefront-packages`.
- Isolation gate restores/builds the generated solution from the workspace, verifies server package closure for Presentation/Components/Browser, verifies WASM closure for Components/Browser, and rejects direct Runtime/Client package references.
- `build-storefront.ps1 -Mode validate-only -Name Phase5PackageProof -StoreKey sample -OutputRoot obj/storefront-builder/generated` passed after the gate refreshed `generated-files.yaml`.

## Phase 6 - Static Validator And Isolation Gate Rewrite

Goal: make validation prove the new shape and fail the old nested shape.

Tasks:

- [x] Update `validate-storefront.ps1` to call validation with workspace semantics.
- [x] Update `Test-StorefrontBuilderGeneratedProject.ps1` path assumptions:
  - [x] Project root parameter means workspace root.
  - [x] Server project path is `{WorkspaceRoot}/{Name}/{Name}.csproj`.
  - [x] WASM project path is `{WorkspaceRoot}/{Name}.WASM/{Name}.WASM.csproj`.
  - [x] Analysis root is `{WorkspaceRoot}/docs/storefront-analysis`.
- [x] Replace required exclusion-marker checks:
  - [x] Remove checks that require `Compile Remove="{Name}.WASM\**"`.
  - [x] Remove checks that require `Content Remove="{Name}.WASM\**"`.
  - [x] Remove checks that require `EmbeddedResource Remove="{Name}.WASM\**"`.
  - [x] Remove checks that require `None Remove="{Name}.WASM\**"`.
  - [x] Add negative checks that fail if those exclusions exist.
- [x] Add structure checks:
  - [x] Workspace root exists.
  - [x] Solution file exists.
  - [x] Server project exists in sibling server folder.
  - [x] WASM project exists in sibling WASM folder.
  - [x] No nested WASM folder exists under server project root.
  - [x] `docs/storefront-analysis` exists at workspace root.
- [x] Add solution checks:
  - [x] Solution contains server project.
  - [x] Solution contains WASM project.
  - [x] Solution does not contain forbidden monorepo projects.
  - [x] Solution does not contain generated projects from another output root.
- [x] Add reference checks:
  - [x] Server ProjectReference to sibling WASM uses `..\{Name}.WASM\{Name}.WASM.csproj`.
  - [x] Server does not reference Starter.WASM.
  - [x] Server does not reference Storefront.V2.
  - [x] Server does not reference backend/core/API projects.
  - [x] WASM does not reference server project.
  - [x] WASM does not reference Runtime or Client.
- [x] Update isolation gate:
  - [x] Restore/build solution instead of individual nested project first.
  - [x] Scan both server and WASM project trees.
  - [x] Scan workspace shared files.
  - [x] Preserve package boundary proof.
  - [x] Fail with problem/cause/fix messages.
- [x] Update architecture tests or add focused tests for:
  - [x] Old nested shape fails.
  - [x] New workspace shape passes.
  - [x] Old exclusion ItemGroups fail.
  - [x] Forbidden reference in server fails.
  - [x] Forbidden reference in WASM fails.

Exit criteria:

- [x] Static validation passes on fresh workspace output.
- [x] Static validation fails on old nested output with actionable guidance.
- [x] Isolation gate passes on fresh workspace output.

Phase 6 evidence:

- `validate-storefront.ps1 -WorkspaceRoot obj/storefront-builder/generated/BlazorShop.Storefront.Phase5PackageProof -SkipIdempotency` passed while deriving project name and store key from the workspace/server sibling shape.
- `Test-StorefrontBuilderGeneratedProject.ps1` now validates server/WASM sibling paths, workspace analysis artifacts, solution membership, no nested WASM folder, no exclusion ItemGroups, and forbidden references.
- `run-storefront-builder-isolation-gate.ps1` now restores/builds the generated solution, scans workspace/source boundaries, preserves package proof, refreshes generated-files after package provenance updates, and emits Problem/Cause/Fix errors.
- `Test-StorefrontBuilderMultiProjectValidation.ps1` passed and covers old nested shape failure, missing/unexpected solution entries, new workspace pass, old exclusion/faulty project-reference failures, direct Runtime/Client rejection, server forbidden namespace rejection, and WASM ProjectReference rejection.
- `run-storefront-builder-isolation-gate.ps1 -WorkspaceRoot obj/storefront-builder/generated/BlazorShop.Storefront.Phase5PackageProof -Name Phase5PackageProof` passed after the Phase 6 changes.
- `build-storefront.ps1 -Mode validate-only -Name Phase5PackageProof -StoreKey sample -OutputRoot obj/storefront-builder/generated` passed with idempotency after the isolation gate refreshed metadata and manifest.

## Phase 7 - Manifest, Generation Plan, And Ownership Rewrite

Goal: make all file ownership and visual task artifacts workspace-aware.

Tasks:

- [x] Update `plan-generation-files.mjs`:
  - [x] Model workspace root explicitly.
  - [x] Model server project explicitly.
  - [x] Model WASM project explicitly.
  - [x] Emit workspace-relative paths.
  - [x] Emit project ownership metadata, not substring-derived ownership.
- [x] Update `generated-file-manifest.mjs`:
  - [x] Stop using `.WASM/` substring detection as the source of truth.
  - [x] Add fields such as `workspaceRelativePath`, `projectKind`, `projectName`, and `projectRelativePath`.
  - [x] Preserve generated-owned, user-owned, protected, and artifact-only categories.
  - [x] Treat workspace shared files as workspace-owned, not server-owned.
- [x] Update handoff generation plan compiler:
  - [x] Allowed visual files may target server project or WASM project explicitly.
  - [x] Protected files may target workspace, server, or WASM explicitly.
  - [x] Agent task package records workspace-relative target paths.
- [x] Update `write-agent-task-package` logic:
  - [x] Include both project roots.
  - [x] Include allowed target project kind.
  - [x] Reject ambiguous target paths.
- [x] Update `record-agent-visual-writes.mjs`:
  - [x] Accept `--workspace-root`.
  - [x] Keep `--project-root` alias with deprecation guidance.
  - [x] Validate writes against workspace-relative manifest paths.
  - [x] Reject writes into workspace shared files unless explicitly allowed by a foundation task.
  - [x] Reject writes into generated server app/route/BFF protected files.
  - [x] Reject writes into generated WASM transport/bootstrap protected files.
- [x] Update visual repair scripts:
  - [x] Read project roots from manifest/metadata.
  - [x] Preserve workspace-relative path reporting.
  - [x] Avoid applying repair patches to the wrong sibling project.
- [x] Update visual QA scripts:
  - [x] Resolve generated CSS/asset paths against server project root.
  - [x] Resolve analysis artifacts against workspace root.
  - [x] Resolve browser/WASM artifact expectations against WASM project root where needed.
- [x] Update metadata schema:
  - [x] Store `workspaceLayoutVersion`.
  - [x] Store `serverProjectRoot`.
  - [x] Store `wasmProjectRoot`.
  - [x] Store `solutionPath`.
  - [x] Store `analysisRoot`.

Exit criteria:

- [x] Every generated file can be traced to workspace, server, or WASM ownership.
- [x] Visual agent tasks cannot write to the wrong sibling project.
- [x] Manifest and generation plan can validate without path substring guesses.

Evidence:

- `build-storefront.ps1 -Mode generate -Name Phase7ManifestProof -StoreKey sample -OutputRoot obj/storefront-builder/generated -Force` passed with workspace/server/WASM manifest fields.
- `build-storefront.ps1 -Mode plan-only -Name Phase7ManifestProof -StoreKey sample -OutputRoot obj/storefront-builder/generated` emitted workspace-relative default plan paths.
- `validate-storefront.ps1 -WorkspaceRoot obj/storefront-builder/generated/BlazorShop.Storefront.Phase7ManifestProof -SkipIdempotency` passed.
- `Test-StorefrontBuilderMultiProjectValidation.ps1` passed.
- `build-storefront.ps1 -Mode generate -Name Phase7HandoffProof -StoreKey sample -OutputRoot obj/storefront-builder/generated -HandoffRoot tools/BlazorShop.AI.StorefrontBuilder/tests/generation/fixtures/phase4-11-closure/portable-handoff -Force` passed.
- `Test-StorefrontBuilderHandoffBoundary.mjs --project-root obj/storefront-builder/generated/BlazorShop.Storefront.Phase7HandoffProof` passed after `record-agent-visual-writes.mjs --workspace-root ... --written-files BlazorShop.Storefront.Phase7HandoffProof/wwwroot/css/storefront-builder.generated.css`.

## Phase 8 - Regeneration Workspace Migration

Goal: make regeneration create, compare, report, and apply changes against the workspace shape safely.

Tasks:

- [x] Update `regenerate-storefront.ps1` parameters:
  - [x] Add `-WorkspaceRoot`.
  - [x] Keep `-ProjectRoot` alias for one cycle.
  - [x] Validate old nested shape and fail with "regenerate fresh" guidance unless an explicit upgrade path is approved.
- [x] Update candidate generation:
  - [x] Candidate root is a generated workspace.
  - [x] Candidate contains solution, server, WASM, shared props, nuget config, and analysis docs.
  - [x] Candidate cleanup does not delete stable WhatIf reports.
- [x] Update comparison logic:
  - [x] Compare workspace-relative paths.
  - [x] Compare server files under `{Name}/`.
  - [x] Compare WASM files under `{Name}.WASM/`.
  - [x] Compare shared metadata under workspace root.
  - [x] Preserve user-owned files in either sibling project.
  - [x] Report obsolete files in either sibling project.
- [x] Update protected-file logic:
  - [x] Workspace shared files protected unless `-Scope foundation`.
  - [x] Server bootstrapping files protected unless approved.
  - [x] WASM bootstrapping files protected unless approved.
  - [x] Visual files may be updated only when generated-owned.
- [x] Update conflict logic:
  - [x] Manual edit conflicts include workspace-relative path.
  - [x] Conflict report includes project kind.
  - [x] Conflict guidance says whether to keep user edit, rerun scoped generation, or run foundation upgrade.
- [x] Preserve WhatIf behavior from prior closure:
  - [x] Print plan lines to console.
  - [x] Write stable report outside target workspace.
  - [x] Do not require `SFB_KEEP_REGENERATION_CANDIDATE_ARTIFACTS` for normal report access.
- [x] Update `-ValidateAfterApply` to run static validation against workspace root.
- [x] Update `-BuildAfterApply` to build solution.
- [x] Update regeneration ownership tests:
  - [x] No-op regeneration stays deterministic.
  - [x] Server visual scoped update works.
  - [x] WASM visual scoped update works if such files are generated-owned.
  - [x] Foundation scoped update can update shared props/contract metadata.
  - [x] Manual edit conflict works in server project.
  - [x] Manual edit conflict works in WASM project.
  - [x] Obsolete file reported in server project.
  - [x] Obsolete file reported in WASM project.
  - [x] Old nested artifact fails with clear guidance.

Exit criteria:

- [x] Regeneration can update a fresh workspace output.
- [x] Regeneration does not apply files outside the target workspace.
- [x] WhatIf remains useful after candidate cleanup.

Phase 8 evidence:

- `regenerate-storefront.ps1` now accepts `-WorkspaceRoot`, keeps `-ProjectRoot` as a warning alias, rejects old/incomplete workspace shapes with `SFB-REGEN-033`, validates via workspace root, and builds the generated solution for `-BuildAfterApply`.
- Regeneration planning now compares workspace-relative manifest paths, uses explicit `projectKind`/project metadata instead of `.WASM/` substring inference, reports workspace/server/WASM summaries, and path-checks copy source/target roots before applying changed files.
- Handoff/regeneration guards now treat workspace shared files and server/WASM bootstrap files as protected unless a reviewed foundation path is used.
- Validation passed:
  - `build-storefront.ps1 -Url https://example.test -Name Phase8RegenProof -StoreKey sample -OutputRoot obj/storefront-builder/generated -Mode generate -Force`
  - `validate-storefront.ps1 -WorkspaceRoot obj/storefront-builder/generated/BlazorShop.Storefront.Phase8RegenProof -SkipIdempotency`
  - `regenerate-storefront.ps1 -WorkspaceRoot obj/storefront-builder/generated/BlazorShop.Storefront.Phase8RegenProof -Scope css -WhatIf`
  - `Test-StorefrontBuilderRegenerationSafety.ps1`

## Phase 9 - QA Runner And Browser Proof Update

Goal: make all StorefrontBuilder proof runners use the workspace shape and still test real browser behavior.

Tasks:

- [x] Update `scripts/qa/run-storefront-builder-generated-proof.ps1`:
  - [x] Generate workspace output.
  - [x] Restore solution.
  - [x] Build solution.
  - [x] Run static validation with workspace root.
  - [x] Run isolation gate with workspace root.
  - [x] Run regeneration proof with workspace root.
  - [x] Start generated server from server project path.
- [x] Update `FoundationFunctionalFast` flow:
  - [x] Mock same-origin BFF routes still work.
  - [x] Product descriptors still render.
  - [x] Product selection preview still works.
  - [x] Add-to-cart still works.
  - [x] Cart badge still works.
  - [x] Cart page still works.
  - [x] Checkout route still works.
  - [x] Account route still works if covered by current browser package.
  - [x] Consent save/revoke still works.
  - [x] Direct Commerce Node browser calls remain rejected.
- [x] Update `FoundationFunctionalFull` flow:
  - [x] Generated host starts from server project under workspace.
  - [x] Fixture-backed store/category/product/page/payment data still load.
  - [x] COD checkout proof remains place-order capable when fixture runtime is available.
  - [x] SEO, sitemap, robots, missing slug, and consent flows still run.
- [x] Update `scripts/qa/run-storefront-builder-isolation-gate.ps1`:
  - [x] Restore/build solution.
  - [x] Pack required Storefront packages once.
  - [x] Validate both generated projects consume packages correctly.
  - [x] Scan both sibling project trees for forbidden references.
- [x] Update `scripts/qa/run-storefront-builder-regeneration-gate.ps1`.
- [x] Update `scripts/qa/run-storefront-phase4-mvp-gate.ps1`.
- [x] Update `scripts/qa/run-storefront-phase4-final-closure-gate.ps1`.
- [x] Update `scripts/qa/run-storefront-builder-full-proof-with-fixture.ps1`.
- [x] Update browser QA commands in docs to use workspace root.
- [x] If any script still needs a server project path, name the parameter `-ServerProjectRoot`, not `-ProjectRoot`.

Exit criteria:

- [x] Structure proof passes locally.
- [x] FoundationFunctionalFast passes locally.
- [x] Full fixture proof command has correct paths and `-Describe` works even when fixture runtime is not started.
- [x] No proof runner assumes nested WASM output.

Validation passed:

- `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/qa/run-storefront-builder-generated-proof.ps1 -Name BlazorShop.Storefront.Phase9Proof -StoreKey sample -OutputRoot obj/storefront-builder/generated/p9 -ProofLevel Structure -RuntimeTimeoutSeconds 45`
- `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/qa/run-storefront-builder-generated-proof.ps1 -Name BlazorShop.Storefront.Phase9FastProof -StoreKey sample -OutputRoot obj/storefront-builder/generated/p9fast -ProofLevel FoundationFunctionalFast -RuntimeTimeoutSeconds 45`
- `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/qa/run-storefront-builder-full-proof-with-fixture.ps1 -Describe`
- PowerShell parser syntax checks for updated QA runners.
- `node --check` for `run-fast-foundation-functional.mjs` and `run-commerce-regression.mjs`.

## Phase 10 - Documentation And Operator Workflow

Goal: make docs, examples, and agent guidance match the new behavior.

Tasks:

- [x] Update `docs/architecture/11-storefront-builder.md`:
  - [x] Generated artifact tree shows workspace with solution, sibling server, sibling WASM, and workspace docs.
  - [x] Entrypoints use `-WorkspaceRoot` where applicable.
  - [x] `-ProjectRoot` is documented only as temporary alias.
  - [x] Regeneration examples use workspace root.
  - [x] Validation examples use workspace root.
  - [x] Isolation examples use workspace root.
- [x] Update `docs/agents/storefront-builder.md`:
  - [x] Generated storefront requirements mention workspace shape.
  - [x] Validation commands use workspace root.
  - [x] Browser QA commands use workspace root.
  - [x] Old nested-WASM wording removed.
- [x] Update `docs/visual-reverse-engineering-skill/README.md`.
- [x] Update `docs/visual-reverse-engineering-skill/reference.md`.
- [x] Update `docs/visual-reverse-engineering-skill/how-to-generate-and-validate.md`.
- [x] Update `docs/visual-reverse-engineering-skill/tutorial-generated-proof.md`.
- [x] Update `docs/visual-reverse-engineering-skill/explanation-boundaries-and-regeneration.md`.
- [x] Add a factual note to plan 24 or related historical summary:
  - [x] Plan 24 was superseded by workspace migration.
  - [x] Historical checked items must not be used as current closure evidence.
- [x] Update any README snippets emitted into generated output.
- [x] Ensure all examples use:
  - [x] `dotnet restore <workspace>/<name>.sln`
  - [x] `dotnet build <workspace>/<name>.sln --no-restore`
  - [x] `dotnet run --project <workspace>/<name>/<name>.csproj`

Exit criteria:

- [x] A developer following docs does not create or validate the old nested shape.
- [x] An agent can identify workspace, server, WASM, and analysis paths from docs alone.

Validation passed:

- `powershell -NoProfile -Command "$tokens=$null;$errors=$null;[System.Management.Automation.Language.Parser]::ParseFile('scripts/generate-storefront-sample.ps1',[ref]$tokens,[ref]$errors) | Out-Null; if ($errors.Count) { $errors | Format-List; exit 1 }; 'scripts\generate-storefront-sample.ps1 syntax ok'"`
- Current-doc scan for `ProjectRoot`, `GeneratedProjectRoot`, `generated-project-root`, and `--project-root` found only the documented temporary `-ProjectRoot` alias note.
- Current-doc scan confirms examples use workspace roots, solution restore/build, and server-project `dotnet run` paths.

## Phase 11 - Compatibility Cleanup And Guardrails

Goal: remove old assumptions and prevent regression.

Tasks:

- [x] Search and eliminate active old-shape assumptions:
  - [x] `Compile Remove=.*WASM`
  - [x] `Content Remove=.*WASM`
  - [x] `EmbeddedResource Remove=.*WASM`
  - [x] `None Remove=.*WASM`
  - [x] `{Name}.WASM/{Name}.WASM.csproj` as nested path
  - [x] `{Name}.WASM\\{Name}.WASM.csproj` as nested path
  - [x] `Join-Path $projectRoot "$Name.WASM"`
  - [x] `rootPath: "."` for generated server project ownership
  - [x] `.WASM/` substring as project ownership truth
- [x] Keep historical docs only if they are clearly marked as historical and not current instructions.
- [x] Add guardrail tests:
  - [x] Validator fails if nested WASM folder exists under server root.
  - [x] Validator fails if exclusion ItemGroups are present.
  - [x] Validator fails if solution is missing.
  - [x] Validator fails if solution contains unexpected projects.
  - [x] Regeneration fails old nested workspace with clear guidance.
- [x] Add static source scan in QA:
  - [x] StorefrontBuilder scripts do not create nested WASM output.
  - [x] StorefrontBuilder scripts do not generate WASM exclusion ItemGroups.
  - [x] Manifest code does not infer ownership only from `.WASM/`.
- [x] Update failure messages:
  - [x] Problem: what failed.
  - [x] Cause: old nested shape or missing workspace file.
  - [x] Fix: regenerate fresh or pass workspace root.

Exit criteria:

- [x] Old nested shape cannot silently pass validation.
- [x] Old nested generation code is removed from active scripts.
- [x] Guardrails explain exactly how to fix failed output.

Validation passed:

- `powershell -NoProfile -ExecutionPolicy Bypass -File tools/BlazorShop.AI.StorefrontBuilder/tests/generation/Test-StorefrontBuilderWorkspaceGuardrails.ps1`
- `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/qa/run-storefront-builder-regeneration-gate.ps1 -Describe`
- `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/qa/run-storefront-builder-regeneration-gate.ps1`
- PowerShell parser syntax checks for `run-storefront-builder-regeneration-gate.ps1`, `Test-StorefrontBuilderGeneratedProject.ps1`, and `Test-StorefrontBuilderWorkspaceGuardrails.ps1`.

## Phase 12 - Fresh GeneratedProof And Kindredcoast Proof

Goal: prove the new workspace output with both canonical proof and a real named generated storefront scenario.

Tasks:

- [x] Recreate canonical generated proof under ignored output:
  - [x] Delete only the exact proof workspace after path safety validation.
  - [x] Generate fresh `BlazorShop.Storefront.GeneratedProof`.
  - [x] Verify solution file exists.
  - [x] Verify sibling server and WASM projects exist.
  - [x] Verify no nested WASM under server root.
- [x] Run structure proof:
  - [x] `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/qa/run-storefront-builder-generated-proof.ps1 -ProofLevel Structure`
- [x] Run fast browser proof:
  - [x] `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/qa/run-storefront-builder-generated-proof.ps1 -ProofLevel FoundationFunctionalFast`
- [x] Run regeneration proof:
  - [x] No-op regeneration.
  - [x] Manual edit conflict.
  - [x] Foundation scope.
  - [x] WhatIf report outside target.
- [x] Run isolation gate:
  - [x] `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/qa/run-storefront-builder-isolation-gate.ps1 -WorkspaceRoot artifacts/storefront-builder/generated/BlazorShop.Storefront.GeneratedProof -Name BlazorShop.Storefront.GeneratedProof`
- [x] Run `-Describe` on full fixture proof:
  - [x] Verify paths show workspace shape.
  - [x] Verify generated host port and project path are correct.
- [x] If fixture runtime is available, run full fixture proof. Not run because `http://localhost:5180/health` refused the connection, so the fixture runtime was not available.
- [x] Generate a real named storefront proof such as `BlazorShop.Storefront.Kindredcoast`:
  - [x] Fresh generation.
  - [x] Solution restore/build.
  - [x] Static validation.
  - [x] Visual QA if handoff/fixture package is available. No Kindredcoast fixture/handoff package was available; canonical browser fast proof covered runtime browser behavior.
  - [x] Confirm not added to `BlazorShop.sln`.
- [x] Record local evidence in generated reports only, unless a tracked closure summary is explicitly required.

Exit criteria:

- [x] Canonical generated proof passes.
- [x] Real named generated output proves the path is not hardcoded to `GeneratedProof`.
- [x] Browser behavior remains tested with Playwright, not only smoke tests.

Validation passed:

- `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/qa/run-storefront-builder-generated-proof.ps1 -Name BlazorShop.Storefront.GeneratedProof -StoreKey sample -OutputRoot artifacts/storefront-builder/generated -ProofLevel Structure -RuntimeTimeoutSeconds 45`
- `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/qa/run-storefront-builder-generated-proof.ps1 -Name BlazorShop.Storefront.GeneratedProof -StoreKey sample -OutputRoot artifacts/storefront-builder/generated -ProofLevel FoundationFunctionalFast -RuntimeTimeoutSeconds 45`
- `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/qa/run-storefront-builder-isolation-gate.ps1 -WorkspaceRoot artifacts/storefront-builder/generated/BlazorShop.Storefront.GeneratedProof -Name BlazorShop.Storefront.GeneratedProof`
- `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/qa/run-storefront-builder-full-proof-with-fixture.ps1 -Describe`
- `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/qa/run-storefront-builder-generated-proof.ps1 -Name BlazorShop.Storefront.Kindredcoast -StoreKey kindredcoast -Url https://www.kindredcoast.com/ -OutputRoot artifacts/storefront-builder/generated -ProofLevel Structure -RuntimeTimeoutSeconds 45`
- Workspace checks found solutions and sibling server/WASM projects for `GeneratedProof` and `Kindredcoast`, and no nested WASM folder under either server project.
- `Select-String` found no `GeneratedProof`, `Kindredcoast`, or generated artifact path entries in `BlazorShop.sln`.

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

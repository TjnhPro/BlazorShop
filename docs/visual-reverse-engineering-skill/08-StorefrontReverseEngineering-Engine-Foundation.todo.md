# Storefront Reverse Engineering Engine Foundation.todo

Scope: Phase 3A - `BlazorShop.AI.StorefrontReverseEngineering` executable foundation.

Status: Proposed

Owner area: development-time tooling under `tools/`, StorefrontBuilder documentation, local fixture tests.

## Goal

Create an executable reverse-engineering foundation that can take a reference storefront URL, create a visual project, run deterministic browser capture, persist versioned evidence artifacts, validate the artifacts, and produce a neutral Visual Blueprint draft.

This phase does not clone a site, generate Razor/CSS, change Storefront V2 behavior, or change Commerce Node/Control Plane APIs. It creates the stable evidence and workflow layer that later visual analysis and generated storefront phases can consume.

## Current Codebase Facts

- `tools/BlazorShop.AI.StorefrontBuilder` already owns development-time capture, analysis, generation, regeneration, validation, and browser QA scripts.
- `docs/architecture/11-storefront-builder.md` defines StorefrontBuilder as development-time tooling only, not a production service, Commerce Node extension, or runtime plugin system.
- Existing generated storefront artifacts live under `artifacts/storefront-builder/generated/{ProjectName}` or `obj/storefront-builder/generated/{ProjectName}`.
- Existing capture script `tools/BlazorShop.AI.StorefrontBuilder/scripts/capture/capture-storefront.mjs` already captures desktop/tablet/mobile screenshots, DOM, computed-style samples, bounding boxes, asset lists, and `capture-manifest.json`.
- Existing schemas cover StorefrontBuilder output artifacts such as metadata, asset manifest, page topology, responsive model, UI patterns, capability decisions, behaviors, design tokens, generated files, generation plan, and AI inference log.
- Existing evidence validator checks required evidence metadata and referenced files, but it validates generated storefront analysis folders, not a standalone reverse-engineering project lifecycle.
- Existing tooling is script-first. It does not yet have typed runtime contracts, workflow state, retry/resume step persistence, artifact store abstraction, schema registry, `IReferenceBrowser`, `IVisualArtifactStore`, `IWorkflowStep`, or `IVisualAnalysisProvider`.

## Architecture Decisions

| Decision | Choice | Rationale |
| --- | --- | --- |
| Tool location | `tools/BlazorShop.AI.StorefrontReverseEngineering` | Repo already uses `tools/BlazorShop.AI.StorefrontBuilder`; root `src/` is not the active convention. |
| Runtime type | Development-time executable/tooling | Must not become Commerce Node, Control Plane, or production ASP.NET runtime. |
| Artifact root | `artifacts/storefront-reverse-engineering/projects/{ProjectId}` and `obj/storefront-reverse-engineering/projects/{ProjectId}` | Keeps generated/research artifacts disposable and separate from generated storefront source. |
| StorefrontBuilder relationship | ReverseEngineering creates evidence/blueprint; StorefrontBuilder consumes it in later phases | Avoids rewriting or duplicating current generation/regeneration engine in Phase 3A. |
| Browser runtime | Playwright behind `IReferenceBrowser` | Existing builder uses Playwright; abstraction keeps implementation replaceable. |
| AI use | Optional provider behind `IVisualAnalysisProvider`; rule-based fallback required | Phase 3A must work without paid/provider-specific AI. |
| Skill catalog | Manifest/documentation, not execution runtime | Skills guide agents; workflow services execute deterministic steps. |
| JSON artifacts | Source of truth | Markdown reports are human-readable outputs only. |
| External references | Learn workflow/capture ideas only | Do not copy Next.js/React/Tailwind scaffolds, external codegen, source content, logo, or brand assets. |

## Non-Goals

- Do not generate Blazor Razor components.
- Do not generate CSS/theme output.
- Do not create `BlazorShop.Storefront.{Name}` projects.
- Do not modify `BlazorShop.Storefront.Starter`.
- Do not modify `BlazorShop.Storefront.V2`.
- Do not modify Storefront Client, Runtime, Presentation, Browser, Components, Commerce Node, Control Plane, Domain, Application, or Infrastructure.
- Do not add backend APIs.
- Do not implement ecommerce business logic.
- Do not crawl an entire website automatically.
- Do not automate authenticated checkout/account flows against reference sites.
- Do not copy protected source content, brand assets, logos, or full asset mirrors.
- Do not make pixel-perfect cloning a success criterion.
- Do not require internet access for automated tests.
- Do not depend on MCP browser tools as the engine runtime.

## Proposed Source Layout

```text
tools/
└── BlazorShop.AI.StorefrontReverseEngineering/
    ├── BlazorShop.AI.StorefrontReverseEngineering.csproj
    ├── README.md
    ├── Domain/
    ├── Application/
    ├── Contracts/
    ├── Browser/
    ├── Storage/
    ├── Workflows/
    ├── Analysis/
    ├── Skills/
    ├── Schemas/
    ├── Validation/
    ├── Cli/
    └── tests/
        └── BlazorShop.AI.StorefrontReverseEngineering.Tests/
```

Notes:

- Keep one production tool assembly first. Split assemblies only after boundaries prove stable.
- Keep tests under the tool folder unless the repo later decides to promote them into `BlazorShop.Tests.V2`.
- No production project may reference this tool.
- The tool may reuse ideas and validation conventions from `tools/BlazorShop.AI.StorefrontBuilder`, but Phase 3A should not move or break existing StorefrontBuilder commands.

## Proposed Artifact Layout

Manual artifacts:

```text
artifacts/storefront-reverse-engineering/projects/{project-id}/
```

Automated/test artifacts:

```text
obj/storefront-reverse-engineering/projects/{project-id}/
```

Project layout:

```text
{project-id}/
├── project.json
├── configuration.json
├── discovery/
│   ├── site-profile.json
│   ├── reconnaissance.json
│   └── capture-plan.json
├── captures/
│   └── {page-id}/
│       ├── desktop-1440/
│       │   ├── full-page.png
│       │   ├── viewport-segments/
│       │   ├── dom.html
│       │   ├── styles.json
│       │   ├── boxes.json
│       │   ├── assets.json
│       │   └── manifest.json
│       ├── tablet-768/
│       └── mobile-390/
├── interactions/
│   └── {page-id}/
│       └── {state-name}/
├── analysis/
│   ├── page-topology.draft.json
│   ├── page-specifications/
│   ├── component-specifications/
│   └── visual-blueprint.draft.json
├── reports/
│   ├── evidence-validation.md
│   ├── originality-audit.md
│   └── readiness-report.md
└── runs/
    └── {run-id}.json
```

## Artifact Contract Rules

- Every JSON artifact must include `schemaVersion`, `artifactKind`, `artifactId`, `createdUtc` or equivalent provenance metadata when applicable.
- Evidence artifacts must link back to source URL, normalized page ID, viewport ID, browser engine, capture method, timestamp, and related file paths.
- AI-assisted outputs must reference evidence IDs and inference IDs.
- Markdown reports must not be the only copy of a machine-readable decision.
- Logs must not include passwords, API keys, authorization headers, sensitive cookies, or session tokens.
- Artifact paths must be normalized and blocked from escaping approved artifact roots.

## Phase 0 - Architecture Lock And Inventory

Goal: prepare the phase without disrupting current StorefrontBuilder.

Tasks:

- [x] Confirm `tools/BlazorShop.AI.StorefrontBuilder` remains active and unchanged as the current generation/regeneration tool.
- [x] Confirm `BlazorShop.AI.StorefrontReverseEngineering` is development-time only and not a production runtime project.
- [x] Confirm no production project will reference the new tool.
- [x] Confirm output roots:
  - [x] `artifacts/storefront-reverse-engineering/projects`
  - [x] `obj/storefront-reverse-engineering/projects`
- [x] Decide whether the tool `.csproj` is added to `BlazorShop.sln` or validated by direct project commands only.
- [x] Inventory reusable StorefrontBuilder scripts and schemas:
  - [x] `scripts/capture/capture-storefront.mjs`
  - [x] `scripts/capture/discover-pages.mjs`
  - [x] `scripts/validate/Test-StorefrontBuilderEvidence.ps1`
  - [x] `schemas/*.schema.json`
  - [x] `tests/playwright/capture-fixture.spec.mjs`
- [x] Write a short architecture note that ReverseEngineering produces evidence/blueprint artifacts and StorefrontBuilder consumes them later.

Guardrails:

- [x] No generated storefront project is added to `BlazorShop.sln`.
- [x] No changes to Commerce Node, Control Plane, Storefront V2, Starter, Presentation, Runtime, Client, Browser, Components.
- [x] No generated artifacts are committed.

Verification:

```powershell
rg -n "BlazorShop.AI.StorefrontReverseEngineering" BlazorShop.PresentationV2 BlazorShop.Domain BlazorShop.Application BlazorShop.Infrastructure
rg -n "storefront-reverse-engineering" docs/architecture docs/visual-reverse-engineering-skill tools
```

Exit criteria:

- [x] Ownership and output roots are documented.
- [x] Current StorefrontBuilder behavior is not moved or replaced.
- [x] Phase 3A boundaries are unambiguous before code starts.

## Phase 1 - Project Skeleton And Core Contracts

Goal: create the executable/tooling skeleton and typed domain contracts.

Files/directories:

- `tools/BlazorShop.AI.StorefrontReverseEngineering/BlazorShop.AI.StorefrontReverseEngineering.csproj`
- `tools/BlazorShop.AI.StorefrontReverseEngineering/Domain/`
- `tools/BlazorShop.AI.StorefrontReverseEngineering/Contracts/`
- `tools/BlazorShop.AI.StorefrontReverseEngineering/Cli/`
- `tools/BlazorShop.AI.StorefrontReverseEngineering/tests/BlazorShop.AI.StorefrontReverseEngineering.Tests/`

Tasks:

- [x] Create `VisualProjectId` value object.
- [x] Create `VisualProject`.
- [x] Create `VisualProjectStatus`:
  - [x] `Created`
  - [x] `Discovering`
  - [x] `Discovered`
  - [x] `Capturing`
  - [x] `Captured`
  - [x] `Analyzing`
  - [x] `DraftReady`
  - [x] `ValidationFailed`
  - [x] `Failed`
- [x] Create `ViewportDefinition`.
- [x] Create `ReferenceUrl`.
- [x] Create base `VisualArtifactMetadata`.
- [x] Create `VisualProjectConfiguration`.
- [x] Create `CapturePolicy`.
- [x] Create `ReferenceSiteProfile`.
- [x] Create `CapturePlan`.
- [x] Create CLI host entrypoint with help output.
- [x] Add command parser for placeholder commands:
  - [x] `init`
  - [x] `discover`
  - [x] `capture`
  - [x] `inspect`
  - [x] `validate`
- [x] Add serialization tests for core contracts.

Guardrails:

- [x] Project does not reference Storefront V2.
- [x] Project does not reference Commerce Node, Control Plane, Domain, Application, Infrastructure.
- [x] DTO/contracts do not depend on generated storefront models.

Verification:

```powershell
dotnet build tools\BlazorShop.AI.StorefrontReverseEngineering\BlazorShop.AI.StorefrontReverseEngineering.csproj
dotnet test tools\BlazorShop.AI.StorefrontReverseEngineering\tests\BlazorShop.AI.StorefrontReverseEngineering.Tests\BlazorShop.AI.StorefrontReverseEngineering.Tests.csproj
rg -n "ProjectReference" tools/BlazorShop.AI.StorefrontReverseEngineering
```

Exit criteria:

- [x] Tool builds.
- [x] Core contracts round-trip JSON.
- [x] CLI help can be executed.
- [x] No forbidden production references exist.

## Phase 2 - Artifact Store, Path Safety, And Schema Registry

Goal: make artifacts durable, versioned, validated, and path-safe.

Files/directories:

- `Storage/`
- `Schemas/`
- `Validation/`
- `tests/.../Schema/`

Tasks:

- [x] Create `ArtifactPath`.
- [x] Create approved artifact root resolver.
- [x] Create path traversal guard.
- [x] Create `IVisualArtifactStore`.
- [x] Implement `FileSystemVisualArtifactStore`.
- [x] Create `IVisualSchemaRegistry`.
- [x] Add schema registration for:
  - [x] `visual-project`
  - [x] `configuration`
  - [x] `reference-site-profile`
  - [x] `reconnaissance`
  - [x] `capture-plan`
  - [x] `capture-manifest`
  - [x] `screenshot-evidence`
  - [x] `dom-evidence`
  - [x] `computed-style-evidence`
  - [x] `asset-inventory`
  - [x] `interaction-evidence`
  - [x] `page-topology-draft`
  - [x] `page-specification-draft`
  - [x] `component-specification-draft`
  - [x] `visual-blueprint-draft`
  - [x] `originality-audit`
  - [x] `readiness-report`
  - [x] `workflow-run`
- [x] Add JSON schema validation service.
- [x] Add artifact write validation.
- [x] Add artifact read deserialization validation.
- [x] Add provenance fields to all first-class artifacts.
- [x] Add tests for invalid schema.
- [x] Add tests for path traversal rejection.
- [x] Add tests for approved manual and automation roots.

Guardrails:

- [x] No artifact writes outside `artifacts/storefront-reverse-engineering` or `obj/storefront-reverse-engineering`.
- [x] No generated storefront artifact roots are reused for reverse-engineering project state.
- [x] Existing StorefrontBuilder schemas remain compatible.

Verification:

```powershell
dotnet test tools\BlazorShop.AI.StorefrontReverseEngineering\tests\BlazorShop.AI.StorefrontReverseEngineering.Tests\BlazorShop.AI.StorefrontReverseEngineering.Tests.csproj --filter "FullyQualifiedName~Schema|FullyQualifiedName~Artifact"
rg -n "VisualProjects/" tools docs
```

Exit criteria:

- [x] Artifact store can write/read typed JSON.
- [x] Invalid schemas fail clearly.
- [x] Unsafe paths fail before write.
- [x] Artifact schema versioning is explicit.

## Phase 3 - Visual Project Lifecycle Commands

Goal: make `init`, `inspect`, and lifecycle status transitions executable.

Tasks:

- [x] Implement `init --url --name --output-root`.
- [x] Normalize project ID/name separately from generated storefront project names.
- [x] Validate `http`, `https`, and local fixture URL policy.
- [x] Write `project.json`.
- [x] Write `configuration.json`.
- [x] Set initial status `Created`.
- [x] Implement `inspect --project`.
- [x] Print project status, source URL, artifact root, latest run, and validation summary.
- [x] Add status transition helper.
- [x] Reject invalid transitions unless command uses an explicit recovery mode.
- [x] Add test for duplicate project without `--force`.
- [x] Add test for invalid URL.
- [x] Add test for inspect on missing project.

Guardrails:

- [x] Do not accept arbitrary output paths outside approved roots.
- [x] Do not infer generated storefront `StoreKey`; reverse-engineering project identity is independent.

Verification:

```powershell
dotnet run --project tools\BlazorShop.AI.StorefrontReverseEngineering\BlazorShop.AI.StorefrontReverseEngineering.csproj -- init --url https://example.test --name Demo --output-root obj/storefront-reverse-engineering/projects
dotnet run --project tools\BlazorShop.AI.StorefrontReverseEngineering\BlazorShop.AI.StorefrontReverseEngineering.csproj -- inspect --project obj/storefront-reverse-engineering/projects/demo
```

Exit criteria:

- [x] A project can be initialized repeatably.
- [x] Project state can be inspected by humans and agents.
- [x] Invalid URL/output-root errors include problem, cause, and fix.

## Phase 4 - Workflow Runner And Run State

Goal: add deterministic sequential workflows with retry, skip, resume, and step state.

Files/directories:

- `Workflows/`
- `Application/`

Tasks:

- [x] Create `IWorkflowStep<TContext>`.
- [x] Create `WorkflowStepResult`.
- [x] Create `WorkflowRun`.
- [x] Create `WorkflowRunStatus`.
- [x] Create `WorkflowStepStatus`.
- [x] Create sequential workflow runner.
- [x] Persist run records under `runs/{run-id}.json`.
- [x] Add retry count to step state.
- [x] Add cancellation handling.
- [x] Add `--force-step` or equivalent re-run control.
- [x] Add skip completed behavior.
- [x] Add structured warning/error model.
- [x] Add tests for:
  - [x] successful sequential run
  - [x] failed step stops downstream steps
  - [x] retryable failure
  - [x] resume after partial success
  - [x] cancellation is not logged as timeout

Guardrails:

- [x] No distributed workflow engine.
- [x] No background queue.
- [x] No hidden global mutable state.

Verification:

```powershell
dotnet test tools\BlazorShop.AI.StorefrontReverseEngineering\tests\BlazorShop.AI.StorefrontReverseEngineering.Tests\BlazorShop.AI.StorefrontReverseEngineering.Tests.csproj --filter "FullyQualifiedName~Workflow"
```

Exit criteria:

- [x] Workflow run artifacts explain what ran, what was skipped, and why failures happened.
- [x] A failed run can be resumed without deleting the project.

## Phase 5 - Browser Abstraction And Basic Capture

Goal: implement the first browser adapter and capture basic evidence from a local fixture.

Files/directories:

- `Browser/`
- `tests/.../Fixtures/`

Tasks:

- [x] Create `IReferenceBrowser`.
- [x] Create `BrowserPageSession`.
- [x] Create `BrowserCaptureResult`.
- [x] Choose implementation path:
  - [x] Preferred: .NET `Microsoft.Playwright` behind `PlaywrightReferenceBrowser`.
  - [x] Acceptable bridge: wrap existing Node Playwright script behind the same interface for initial parity.
- [x] Add browser install/setup documentation.
- [x] Implement viewport setup.
- [x] Implement page open with timeout.
- [x] Implement DOM snapshot capture.
- [x] Implement basic screenshot capture.
- [x] Implement computed-style sample extraction.
- [x] Implement bounding box extraction.
- [x] Implement asset inventory extraction.
- [x] Add local fixture storefront page:
  - [x] header
  - [x] hero
  - [x] product grid
  - [x] product card
  - [x] product detail section
  - [x] footer
  - [x] sticky header
  - [x] lazy-loaded section
  - [x] mobile menu
  - [x] hover state
  - [x] accordion
  - [x] cookie banner fixture
  - [x] fake brand asset for originality tests
- [x] Add integration test for desktop capture.
- [x] Add integration test for mobile capture.

Guardrails:

- [x] Automated tests use local fixture only.
- [x] Browser logs must not write sensitive cookies or headers.
- [x] Capture policy must enforce max height and timeout.

Verification:

```powershell
dotnet test tools\BlazorShop.AI.StorefrontReverseEngineering\tests\BlazorShop.AI.StorefrontReverseEngineering.Tests\BlazorShop.AI.StorefrontReverseEngineering.Tests.csproj --filter "FullyQualifiedName~Browser|FullyQualifiedName~Capture"
```

Exit criteria:

- [x] Fixture can be captured in desktop and mobile.
- [x] Screenshot, DOM, styles, boxes, assets, and manifest files exist.
- [x] Browser failures produce actionable error messages.

## Phase 6 - Reconnaissance And Capture Plan

Goal: make `discover` produce a project-specific capture plan before capture runs.

Tasks:

- [x] Implement `discover --project`.
- [x] Open the reference URL through `IReferenceBrowser`.
- [x] Capture title, canonical URL, language, meta viewport, document dimensions.
- [x] Detect obvious blockers:
  - [x] navigation failure
  - [x] non-HTML response
  - [x] robots/crawler warning marker if available
  - [x] cookie banner or modal overlay
  - [x] authentication wall
  - [x] excessive page height
  - [x] unsupported protocol
- [x] Create default viewport plan:
  - [x] desktop `1440x1000`
  - [x] tablet `768x1000`
  - [x] mobile `390x900`
- [x] Allow viewport override in configuration.
- [x] Write `discovery/site-profile.json`.
- [x] Write `discovery/reconnaissance.json`.
- [x] Write `discovery/capture-plan.json`.
- [x] Update project status to `Discovered`.
- [x] Add tests for fixture reconnaissance.
- [x] Add tests for blocker detection.

Guardrails:

- [x] Do not crawl beyond configured `maximumPages`.
- [x] Do not use discovered links for full-site capture in Phase 3A.
- [x] Do not bypass authentication, bot protection, or paywalls.

Verification:

```powershell
dotnet run --project tools\BlazorShop.AI.StorefrontReverseEngineering\BlazorShop.AI.StorefrontReverseEngineering.csproj -- discover --project obj/storefront-reverse-engineering/projects/demo
```

Exit criteria:

- [x] Capture plan is deterministic.
- [x] Blockers are recorded instead of silently ignored.
- [x] Discover can be re-run safely.

## Phase 7 - Stable Full-Page Capture And Quality Report

Goal: make full-page capture robust enough for lazy/reveal-heavy ecommerce pages.

Tasks:

- [x] Implement page stabilization service:
  - [x] wait for DOM ready
  - [x] wait for network idle with fallback
  - [x] wait for fonts when available
  - [x] warm scroll down/up
  - [x] optionally hide configured noise selectors
  - [x] record removed/hidden noise elements
- [x] Implement native full-page screenshot path.
- [x] Implement stitched capture fallback:
  - [x] segment viewport captures
  - [x] overlap handling
  - [x] stitch metadata
  - [x] final image dimension validation
- [x] Implement capture quality checks:
  - [x] blank image detection
  - [x] suspicious white/empty regions
  - [x] incomplete height detection
  - [x] missing screenshot file
  - [x] inconsistent manifest dimensions
- [x] Write `CaptureQualityReport`.
- [x] Record capture method: `native-full-page`, `stitched`, or `failed`.
- [x] Add lazy-load fixture test.
- [x] Add forced fallback test.
- [x] Add quality failure test.

Guardrails:

- [x] Do not rely only on native Playwright full-page screenshot.
- [x] Do not mutate the live page beyond configured noise handling and capture stabilization.
- [x] Do not keep unlimited screenshot segments after successful stitch unless configured.

Verification:

```powershell
dotnet test tools\BlazorShop.AI.StorefrontReverseEngineering\tests\BlazorShop.AI.StorefrontReverseEngineering.Tests\BlazorShop.AI.StorefrontReverseEngineering.Tests.csproj --filter "FullyQualifiedName~StableCapture|FullyQualifiedName~Quality"
```

Exit criteria:

- [x] Lazy-loaded fixture sections appear in captured output.
- [x] Capture manifest records whether native or stitched capture was used.
- [x] Quality report blocks obviously broken evidence.

## Phase 8 - Evidence Extraction And Manifest Normalization

Goal: normalize visual evidence into bounded, linked, schema-valid artifacts.

Tasks:

- [x] Create `ElementEvidenceIndex`.
- [x] Create computed-style property allowlist.
- [x] Add element selection policy:
  - [x] semantic landmarks
  - [x] headings
  - [x] links
  - [x] buttons
  - [x] inputs/selects
  - [x] product-card-like candidates
  - [x] images/assets
  - [x] sections/articles
- [x] Add element count limit.
- [x] Add DOM depth limit.
- [x] Extract typography evidence.
- [x] Extract color/background/border/shadow evidence.
- [x] Extract layout/display/grid/flex evidence.
- [x] Extract positioning/sticky/fixed evidence.
- [x] Extract transition/transform evidence.
- [x] Extract asset metadata:
  - [x] URL
  - [x] media type if available
  - [x] dimensions if image is loaded
  - [x] source element
  - [x] reference-only flag placeholder
- [x] Write viewport-level `manifest.json`.
- [x] Write page-level `capture-manifest.json`.
- [x] Link all viewport evidence to project/page/run IDs.
- [x] Add tests for bounded output size.
- [x] Add tests for evidence links.

Guardrails:

- [x] Do not store computed styles for every DOM node by default.
- [x] Do not download full external asset mirrors.
- [x] Do not treat source assets as reusable generated assets.

Verification:

```powershell
dotnet test tools\BlazorShop.AI.StorefrontReverseEngineering\tests\BlazorShop.AI.StorefrontReverseEngineering.Tests\BlazorShop.AI.StorefrontReverseEngineering.Tests.csproj --filter "FullyQualifiedName~Evidence"
```

Exit criteria:

- [x] Evidence output is bounded.
- [x] Every derived artifact can trace to evidence IDs.
- [x] Missing referenced files fail validation.

## Phase 9 - Interaction State Capture

Goal: support configured interaction evidence without trying to auto-discover every UI behavior.

Tasks:

- [x] Create `InteractionCapturePlan`.
- [x] Create `InteractionActionDefinition`.
- [x] Support configured actions:
  - [x] click selector
  - [x] hover selector
  - [x] focus selector
  - [x] scroll to selector
  - [x] wait
- [x] Capture before state.
- [x] Execute action.
- [x] Capture after state.
- [x] Extract before/after screenshot and DOM/style diff metadata.
- [x] Classify interaction model:
  - [x] `Static`
  - [x] `ClickDriven`
  - [x] `HoverDriven`
  - [x] `ScrollDriven`
  - [x] `TimeDriven`
  - [x] `Mixed`
  - [x] `Unknown`
- [x] Record failed selector as warning or blocking error based on policy.
- [x] Add tests for mobile menu fixture.
- [x] Add tests for accordion fixture.
- [x] Add tests for hover fixture.

Guardrails:

- [x] Phase 3A does not need automatic exhaustive interaction discovery.
- [x] Do not execute destructive actions, forms, checkout, login, payment, or account mutations on reference sites.
- [x] Only run configured safe selectors.

Verification:

```powershell
dotnet test tools\BlazorShop.AI.StorefrontReverseEngineering\tests\BlazorShop.AI.StorefrontReverseEngineering.Tests\BlazorShop.AI.StorefrontReverseEngineering.Tests.csproj --filter "FullyQualifiedName~Interaction"
```

Exit criteria:

- [x] Configured interactions produce before/after evidence.
- [x] Unsafe or unsupported actions are refused.
- [x] Interaction evidence validates against schema.

## Phase 10 - Skill Catalog Manifest

Goal: capture the selected reverse-engineering skills as a domain-specific manifest.

Files/directories:

- `Skills/`
- `Schemas/skill-definition.schema.json`
- `docs/visual-reverse-engineering-skill/reference.md`

Tasks:

- [x] Create `SkillDefinition`.
- [x] Create `SkillExecutionType`:
  - [x] `Deterministic`
  - [x] `AI-assisted`
  - [x] `Hybrid`
  - [x] `DocumentationOnly`
- [x] Create skill manifest schema.
- [x] Define selected skills:
  - [x] `storefront-reference-reconnaissance`
  - [x] `stabilize-reference-page`
  - [x] `capture-stable-full-page`
  - [x] `capture-responsive-evidence`
  - [x] `discover-visual-interactions`
  - [x] `extract-visual-evidence`
  - [x] `analyze-page-topology`
  - [x] `create-visual-specification-draft`
  - [x] `audit-reference-originality`
  - [x] `validate-visual-evidence`
- [x] For each skill, define:
  - [x] name
  - [x] version
  - [x] category
  - [x] purpose
  - [x] inputs
  - [x] outputs
  - [x] dependencies
  - [x] execution type
  - [x] human review requirement
  - [x] completion criteria
  - [x] forbidden actions
- [x] Add validation test for catalog completeness.
- [x] Add test that skills are documentation/manifest, not runtime magic.

Guardrails:

- [x] Do not copy external `SKILL.md` files into the runtime as executable behavior.
- [x] Do not make skills a substitute for typed workflow services.
- [x] Do not introduce React/Next/Tailwind output concepts into BlazorShop contracts.

Verification:

```powershell
dotnet test tools\BlazorShop.AI.StorefrontReverseEngineering\tests\BlazorShop.AI.StorefrontReverseEngineering.Tests\BlazorShop.AI.StorefrontReverseEngineering.Tests.csproj --filter "FullyQualifiedName~Skill"
```

Exit criteria:

- [x] Selected skill catalog is explicit and validated.
- [x] Each skill has input/output/completion criteria.
- [x] Forbidden actions are documented and machine-readable.

## Phase 11 - Rule-Based Draft Analysis And AI Provider Abstraction

Goal: produce a neutral Visual Blueprint draft without requiring an AI provider.

Files/directories:

- `Analysis/`

Tasks:

- [x] Create `IVisualAnalysisProvider`.
- [x] Create `VisualAnalysisResult`.
- [x] Create `AnalysisContext`.
- [x] Implement `RuleBasedVisualAnalysisProvider`.
- [x] Create `PageTopologyDraft`.
- [x] Create `SectionCandidate`.
- [x] Create `GlobalShellCandidate`.
- [x] Create `PageSpecificationDraft`.
- [x] Create `ComponentSpecificationDraft`.
- [x] Create `VisualBlueprintDraft`.
- [x] Include evidence references in every draft output.
- [x] Add confidence fields.
- [x] Add unsupported pattern warnings.
- [x] Add optional AI inference log contract.
- [x] Add tests proving rule-based analysis works without AI secrets.
- [x] Add tests that AI provider settings are optional.

Guardrails:

- [x] Do not call external AI in automated tests.
- [x] Do not embed provider-specific DTOs in core contracts.
- [x] Do not create Blazor component names as required output yet; use neutral component candidates.

Verification:

```powershell
dotnet test tools\BlazorShop.AI.StorefrontReverseEngineering\tests\BlazorShop.AI.StorefrontReverseEngineering.Tests\BlazorShop.AI.StorefrontReverseEngineering.Tests.csproj --filter "FullyQualifiedName~Analysis|FullyQualifiedName~Blueprint"
```

Exit criteria:

- [x] Blueprint draft can be produced from fixture evidence.
- [x] Blueprint references evidence IDs.
- [x] AI provider abstraction exists but no provider is required.

## Phase 12 - Originality And Provenance Foundation

Goal: prevent reference evidence from being treated as reusable source material by default.

Tasks:

- [x] Create `OriginalityAuditReport`.
- [x] Create `ReferenceOnlyAsset`.
- [x] Create `ProvenanceWarning`.
- [x] Create `GenerationRestriction`.
- [x] Flag likely brand/logo assets.
- [x] Flag source copy/text blocks for review.
- [x] Flag external images/videos as reference-only.
- [x] Distinguish common visual grammar from distinctive source-specific elements.
- [x] Record originality policy in `configuration.json`.
- [x] Add markdown report writer.
- [x] Add tests for fake brand asset fixture.
- [x] Add tests that asset inventory defaults to reference-only unless explicitly allowed.

Guardrails:

- [x] Do not make legal claims.
- [x] Do not declare assets safe to reuse by default.
- [x] Do not download and copy source brand assets into generated storefront output.

Verification:

```powershell
dotnet test tools\BlazorShop.AI.StorefrontReverseEngineering\tests\BlazorShop.AI.StorefrontReverseEngineering.Tests\BlazorShop.AI.StorefrontReverseEngineering.Tests.csproj --filter "FullyQualifiedName~Originality|FullyQualifiedName~Provenance"
```

Exit criteria:

- [x] Originality audit exists and is linked to evidence.
- [x] Reference-only assets are machine-readable.
- [x] Generated phases have clear restrictions to consume later.

## Phase 13 - End-To-End Vertical Slice CLI

Goal: make one local command path prove the whole foundation.

Required vertical slice:

```text
URL
-> create visual project
-> reconnaissance
-> stabilize
-> capture desktop/tablet/mobile
-> capture DOM/styles/boxes/assets
-> write evidence manifest
-> run validation
-> create page topology draft
-> create visual blueprint draft
-> run originality audit
-> generate readiness report
```

Tasks:

- [x] Implement `capture --project`.
- [x] Implement `validate --project`.
- [x] Implement `analyze --project`.
- [x] Implement `run --url --name --output-root` as convenience command.
- [x] Add `--force`, `--resume`, and `--no-ai` options where appropriate.
- [x] Write run summary to console.
- [x] Write `reports/readiness-report.md`.
- [x] Return non-zero exit code for blocking validation failures.
- [x] Keep warnings non-blocking unless configured as strict.
- [x] Add E2E local fixture test.
- [x] Add timeout failure test.
- [x] Add resume test after injected failure.

Guardrails:

- [x] `run` must not generate storefront source.
- [x] `run` must not write to `artifacts/storefront-builder/generated`.
- [x] `run` must not mutate Starter or generated storefronts.

Verification:

```powershell
dotnet run --project tools\BlazorShop.AI.StorefrontReverseEngineering\BlazorShop.AI.StorefrontReverseEngineering.csproj -- run --url <fixture-url> --name FixtureDemo --output-root obj/storefront-reverse-engineering/projects --no-ai
dotnet test tools\BlazorShop.AI.StorefrontReverseEngineering\tests\BlazorShop.AI.StorefrontReverseEngineering.Tests\BlazorShop.AI.StorefrontReverseEngineering.Tests.csproj
```

Exit criteria:

- [x] Local fixture full workflow passes.
- [x] Output contains schema-valid artifacts.
- [x] Readiness report is readable and points to artifact paths.
- [x] Command can be repeated safely.

## Phase 14 - StorefrontBuilder Integration Contract Prep

Goal: prepare the handoff from ReverseEngineering evidence to StorefrontBuilder without forcing generation changes in Phase 3A.

Tasks:

- [x] Define `visual-blueprint.draft.json` fields StorefrontBuilder may consume later.
- [x] Add a compatibility note mapping current StorefrontBuilder artifacts to ReverseEngineering artifacts:
  - [x] current `capture-manifest.json`
  - [x] current `asset-manifest.yaml`
  - [x] current `page-topology.yaml`
  - [x] current `design-tokens.yaml`
  - [x] current `ai-inference-log.json`
- [x] Add explicit non-consumption warning: StorefrontBuilder generation does not yet use new ReverseEngineering artifacts until Phase 3B/3C.
- [x] Add migration path for current Node capture script:
  - [x] keep script as fallback/reference
  - [x] optionally wrap through browser adapter
  - [x] later retire only after parity tests
- [x] Add docs for how generated storefront phases will consume blueprint later.

Guardrails:

- [x] Do not break existing `build-storefront.ps1`.
- [x] Do not break existing `regenerate-storefront.ps1`.
- [x] Do not change generated storefront static validation gate unless needed for documentation only.

Verification:

```powershell
.\tools\BlazorShop.AI.StorefrontBuilder\build-storefront.ps1 -Url https://example.test -Name Demo -StoreKey sample -Mode plan-only
.\tools\BlazorShop.AI.StorefrontBuilder\tests\generation\Test-StorefrontBuilderCreateHardening.ps1
```

Exit criteria:

- [x] Existing StorefrontBuilder command behavior remains intact.
- [x] Future blueprint consumption is documented but not activated.
- [x] No generation behavior changes are hidden inside Phase 3A.

## Phase 15 - Documentation And QA Closure

Goal: make Phase 3A maintainable for agents and humans.

Docs to update:

- [x] `docs/architecture/11-storefront-builder.md`
- [x] `docs/visual-reverse-engineering-skill/README.md`
- [x] `docs/visual-reverse-engineering-skill/reference.md`
- [x] `docs/visual-reverse-engineering-skill/how-to-generate-and-validate.md` if commands or boundaries affect operator flow
- [x] `docs/agents/storefront-builder.md`
- [x] `tools/BlazorShop.AI.StorefrontReverseEngineering/README.md`

QA tasks:

- [x] Add focused architecture/source scan:
  - [x] no production project references ReverseEngineering
  - [x] no ReverseEngineering output writes into generated storefront roots
  - [x] no Storefront V2 reference
  - [x] no backend/core/API reference
- [x] Add local fixture E2E capture test.
- [x] Add schema validation test suite.
- [x] Add workflow retry/resume tests.
- [x] Add secret/cookie log redaction test.
- [x] Add evidence provenance tests.
- [x] Add artifact path safety tests.
- [x] Add StorefrontBuilder compatibility smoke to prove existing builder still works.

Suggested release gate:

```powershell
dotnet build tools\BlazorShop.AI.StorefrontReverseEngineering\BlazorShop.AI.StorefrontReverseEngineering.csproj
dotnet test tools\BlazorShop.AI.StorefrontReverseEngineering\tests\BlazorShop.AI.StorefrontReverseEngineering.Tests\BlazorShop.AI.StorefrontReverseEngineering.Tests.csproj
dotnet test BlazorShop.Tests.V2\BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~StorefrontBuilder"
.\tools\BlazorShop.AI.StorefrontBuilder\tests\generation\Test-StorefrontBuilderCreateHardening.ps1
.\tools\BlazorShop.AI.StorefrontBuilder\build-storefront.ps1 -Url https://example.test -Name Demo -StoreKey sample -Mode plan-only
```

Optional release gate when browser dependencies are installed:

```powershell
Push-Location tools\BlazorShop.AI.StorefrontBuilder
npm install
npx playwright install chromium
npm run capture:fixture
Pop-Location
```

Exit criteria:

- [x] Phase 3A command documentation is copy-paste usable.
- [x] Architecture docs explain the difference between StorefrontBuilder and StorefrontReverseEngineering.
- [x] Local release gate passes.
- [x] Known limitations are documented.
- [x] Phase 3B handoff is explicit.

## Phase 3A Definition Of Done

Architecture:

- [x] `BlazorShop.AI.StorefrontReverseEngineering` exists as independent development-time tooling.
- [x] No production runtime project references it.
- [x] No Commerce Node, Control Plane, Storefront V2, Starter, Runtime, Presentation, Client, Browser, Components, Domain, Application, or Infrastructure dependency is introduced.
- [x] Artifact roots stay under approved `artifacts/storefront-reverse-engineering` and `obj/storefront-reverse-engineering` folders.

Runtime:

- [x] CLI can initialize, inspect, discover, capture, analyze, validate, and run a local fixture workflow.
- [x] Workflow runner supports retry, resume, skip completed steps, and step state persistence.
- [x] Browser capture uses `IReferenceBrowser`.
- [x] Artifact writes use `IVisualArtifactStore`.
- [x] Schema validation uses `IVisualSchemaRegistry`.
- [x] AI provider access is behind `IVisualAnalysisProvider`.
- [x] Rule-based fallback works without AI secrets.

Evidence:

- [x] Desktop, tablet, and mobile evidence exists.
- [x] Screenshot, DOM, styles, boxes, assets, manifests, run logs, topology draft, blueprint draft, originality audit, and readiness report are written.
- [x] Evidence artifacts are bounded and schema-valid.
- [x] Evidence and analysis artifacts include provenance.
- [x] Sensitive cookies, tokens, and credentials are not logged.

StorefrontBuilder compatibility:

- [x] Existing StorefrontBuilder commands still run.
- [x] Existing generated storefront roots and validation gates remain unchanged.
- [x] New ReverseEngineering artifacts are not silently consumed by generation until a later approved phase.

Testing:

- [x] Unit tests pass.
- [x] Schema tests pass.
- [x] Browser fixture tests pass.
- [x] Workflow retry/resume tests pass.
- [x] Artifact path safety tests pass.
- [x] Secret redaction tests pass.
- [x] StorefrontBuilder compatibility smoke passes.

## Deferred To Phase 3B

- Full page discovery/crawling.
- Design token extraction beyond bounded first-pass evidence.
- Automatic section segmentation with high confidence.
- Ecommerce region mapping to Storefront Presentation slots.
- Component candidate scoring.
- Human review UI.
- External AI provider implementations.
- Visual diff between reference and generated storefront.
- StorefrontBuilder consumption of `visual-blueprint.draft.json`.
- Generated Razor/CSS output.

## Autoplan Review Notes

CEO lens:

- [x] Keep Phase 3A narrow: executable evidence foundation, not full visual generation.
- [x] Avoid creating a second generator competing with StorefrontBuilder.
- [x] Make artifacts inspectable so AI agents can work from evidence rather than guessing.

Engineering lens:

- [x] Prefer typed contracts and schema validation over procedural scripts as hidden source of truth.
- [x] Reuse current Playwright capture behavior as baseline evidence, but put it behind replaceable interfaces.
- [x] Keep the new tool independent of production V2 runtime projects.

DX lens:

- [x] CLI commands must be predictable and copy-paste runnable.
- [x] Errors must include problem, cause, and fix.
- [x] Local fixture workflow must be the hello-world path.
- [x] Internet URL capture must be manual/optional, not CI-required.

Risk register:

| Risk | Impact | Mitigation |
| --- | --- | --- |
| Tool becomes a second StorefrontBuilder | Duplicated generation logic | Keep Phase 3A output to evidence/blueprint only. |
| Artifact paths drift from repo policy | Unsafe writes or confusing cleanup | Use approved `artifacts/storefront-reverse-engineering` and `obj/storefront-reverse-engineering` roots. |
| Browser capture is flaky | Unreliable evidence | Use fixture tests, stabilization, stitched fallback, quality report. |
| AI provider becomes required | Tests become fragile and costly | Rule-based fallback and provider abstraction. |
| Source assets are copied accidentally | Legal/product risk | Originality/provenance audit and reference-only asset default. |
| Existing StorefrontBuilder breaks | Regression in generated storefront flow | Compatibility smoke and no generation changes in Phase 3A. |

## Implementation Order Summary

1. Phase 0: architecture lock and inventory.
2. Phase 1: project skeleton and contracts.
3. Phase 2: artifact store and schemas.
4. Phase 3: visual project lifecycle commands.
5. Phase 4: workflow runner and run state.
6. Phase 5: browser abstraction and basic capture.
7. Phase 6: reconnaissance and capture plan.
8. Phase 7: stable full-page capture and quality report.
9. Phase 8: evidence extraction and manifest normalization.
10. Phase 9: interaction state capture.
11. Phase 10: skill catalog manifest.
12. Phase 11: rule-based draft analysis and AI provider abstraction.
13. Phase 12: originality and provenance foundation.
14. Phase 13: end-to-end vertical slice CLI.
15. Phase 14: StorefrontBuilder integration contract prep.
16. Phase 15: documentation and QA closure.

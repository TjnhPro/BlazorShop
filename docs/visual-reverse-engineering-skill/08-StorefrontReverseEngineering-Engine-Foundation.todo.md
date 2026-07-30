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

- [ ] Implement `init --url --name --output-root`.
- [ ] Normalize project ID/name separately from generated storefront project names.
- [ ] Validate `http`, `https`, and local fixture URL policy.
- [ ] Write `project.json`.
- [ ] Write `configuration.json`.
- [ ] Set initial status `Created`.
- [ ] Implement `inspect --project`.
- [ ] Print project status, source URL, artifact root, latest run, and validation summary.
- [ ] Add status transition helper.
- [ ] Reject invalid transitions unless command uses an explicit recovery mode.
- [ ] Add test for duplicate project without `--force`.
- [ ] Add test for invalid URL.
- [ ] Add test for inspect on missing project.

Guardrails:

- [ ] Do not accept arbitrary output paths outside approved roots.
- [ ] Do not infer generated storefront `StoreKey`; reverse-engineering project identity is independent.

Verification:

```powershell
dotnet run --project tools\BlazorShop.AI.StorefrontReverseEngineering\BlazorShop.AI.StorefrontReverseEngineering.csproj -- init --url https://example.test --name Demo --output-root obj/storefront-reverse-engineering/projects
dotnet run --project tools\BlazorShop.AI.StorefrontReverseEngineering\BlazorShop.AI.StorefrontReverseEngineering.csproj -- inspect --project obj/storefront-reverse-engineering/projects/demo
```

Exit criteria:

- [ ] A project can be initialized repeatably.
- [ ] Project state can be inspected by humans and agents.
- [ ] Invalid URL/output-root errors include problem, cause, and fix.

## Phase 4 - Workflow Runner And Run State

Goal: add deterministic sequential workflows with retry, skip, resume, and step state.

Files/directories:

- `Workflows/`
- `Application/`

Tasks:

- [ ] Create `IWorkflowStep<TContext>`.
- [ ] Create `WorkflowStepResult`.
- [ ] Create `WorkflowRun`.
- [ ] Create `WorkflowRunStatus`.
- [ ] Create `WorkflowStepStatus`.
- [ ] Create sequential workflow runner.
- [ ] Persist run records under `runs/{run-id}.json`.
- [ ] Add retry count to step state.
- [ ] Add cancellation handling.
- [ ] Add `--force-step` or equivalent re-run control.
- [ ] Add skip completed behavior.
- [ ] Add structured warning/error model.
- [ ] Add tests for:
  - [ ] successful sequential run
  - [ ] failed step stops downstream steps
  - [ ] retryable failure
  - [ ] resume after partial success
  - [ ] cancellation is not logged as timeout

Guardrails:

- [ ] No distributed workflow engine.
- [ ] No background queue.
- [ ] No hidden global mutable state.

Verification:

```powershell
dotnet test tools\BlazorShop.AI.StorefrontReverseEngineering\tests\BlazorShop.AI.StorefrontReverseEngineering.Tests\BlazorShop.AI.StorefrontReverseEngineering.Tests.csproj --filter "FullyQualifiedName~Workflow"
```

Exit criteria:

- [ ] Workflow run artifacts explain what ran, what was skipped, and why failures happened.
- [ ] A failed run can be resumed without deleting the project.

## Phase 5 - Browser Abstraction And Basic Capture

Goal: implement the first browser adapter and capture basic evidence from a local fixture.

Files/directories:

- `Browser/`
- `tests/.../Fixtures/`

Tasks:

- [ ] Create `IReferenceBrowser`.
- [ ] Create `BrowserPageSession`.
- [ ] Create `BrowserCaptureResult`.
- [ ] Choose implementation path:
  - [ ] Preferred: .NET `Microsoft.Playwright` behind `PlaywrightReferenceBrowser`.
  - [ ] Acceptable bridge: wrap existing Node Playwright script behind the same interface for initial parity.
- [ ] Add browser install/setup documentation.
- [ ] Implement viewport setup.
- [ ] Implement page open with timeout.
- [ ] Implement DOM snapshot capture.
- [ ] Implement basic screenshot capture.
- [ ] Implement computed-style sample extraction.
- [ ] Implement bounding box extraction.
- [ ] Implement asset inventory extraction.
- [ ] Add local fixture storefront page:
  - [ ] header
  - [ ] hero
  - [ ] product grid
  - [ ] product card
  - [ ] product detail section
  - [ ] footer
  - [ ] sticky header
  - [ ] lazy-loaded section
  - [ ] mobile menu
  - [ ] hover state
  - [ ] accordion
  - [ ] cookie banner fixture
  - [ ] fake brand asset for originality tests
- [ ] Add integration test for desktop capture.
- [ ] Add integration test for mobile capture.

Guardrails:

- [ ] Automated tests use local fixture only.
- [ ] Browser logs must not write sensitive cookies or headers.
- [ ] Capture policy must enforce max height and timeout.

Verification:

```powershell
dotnet test tools\BlazorShop.AI.StorefrontReverseEngineering\tests\BlazorShop.AI.StorefrontReverseEngineering.Tests\BlazorShop.AI.StorefrontReverseEngineering.Tests.csproj --filter "FullyQualifiedName~Browser|FullyQualifiedName~Capture"
```

Exit criteria:

- [ ] Fixture can be captured in desktop and mobile.
- [ ] Screenshot, DOM, styles, boxes, assets, and manifest files exist.
- [ ] Browser failures produce actionable error messages.

## Phase 6 - Reconnaissance And Capture Plan

Goal: make `discover` produce a project-specific capture plan before capture runs.

Tasks:

- [ ] Implement `discover --project`.
- [ ] Open the reference URL through `IReferenceBrowser`.
- [ ] Capture title, canonical URL, language, meta viewport, document dimensions.
- [ ] Detect obvious blockers:
  - [ ] navigation failure
  - [ ] non-HTML response
  - [ ] robots/crawler warning marker if available
  - [ ] cookie banner or modal overlay
  - [ ] authentication wall
  - [ ] excessive page height
  - [ ] unsupported protocol
- [ ] Create default viewport plan:
  - [ ] desktop `1440x1000`
  - [ ] tablet `768x1000`
  - [ ] mobile `390x900`
- [ ] Allow viewport override in configuration.
- [ ] Write `discovery/site-profile.json`.
- [ ] Write `discovery/reconnaissance.json`.
- [ ] Write `discovery/capture-plan.json`.
- [ ] Update project status to `Discovered`.
- [ ] Add tests for fixture reconnaissance.
- [ ] Add tests for blocker detection.

Guardrails:

- [ ] Do not crawl beyond configured `maximumPages`.
- [ ] Do not use discovered links for full-site capture in Phase 3A.
- [ ] Do not bypass authentication, bot protection, or paywalls.

Verification:

```powershell
dotnet run --project tools\BlazorShop.AI.StorefrontReverseEngineering\BlazorShop.AI.StorefrontReverseEngineering.csproj -- discover --project obj/storefront-reverse-engineering/projects/demo
```

Exit criteria:

- [ ] Capture plan is deterministic.
- [ ] Blockers are recorded instead of silently ignored.
- [ ] Discover can be re-run safely.

## Phase 7 - Stable Full-Page Capture And Quality Report

Goal: make full-page capture robust enough for lazy/reveal-heavy ecommerce pages.

Tasks:

- [ ] Implement page stabilization service:
  - [ ] wait for DOM ready
  - [ ] wait for network idle with fallback
  - [ ] wait for fonts when available
  - [ ] warm scroll down/up
  - [ ] optionally hide configured noise selectors
  - [ ] record removed/hidden noise elements
- [ ] Implement native full-page screenshot path.
- [ ] Implement stitched capture fallback:
  - [ ] segment viewport captures
  - [ ] overlap handling
  - [ ] stitch metadata
  - [ ] final image dimension validation
- [ ] Implement capture quality checks:
  - [ ] blank image detection
  - [ ] suspicious white/empty regions
  - [ ] incomplete height detection
  - [ ] missing screenshot file
  - [ ] inconsistent manifest dimensions
- [ ] Write `CaptureQualityReport`.
- [ ] Record capture method: `native-full-page`, `stitched`, or `failed`.
- [ ] Add lazy-load fixture test.
- [ ] Add forced fallback test.
- [ ] Add quality failure test.

Guardrails:

- [ ] Do not rely only on native Playwright full-page screenshot.
- [ ] Do not mutate the live page beyond configured noise handling and capture stabilization.
- [ ] Do not keep unlimited screenshot segments after successful stitch unless configured.

Verification:

```powershell
dotnet test tools\BlazorShop.AI.StorefrontReverseEngineering\tests\BlazorShop.AI.StorefrontReverseEngineering.Tests\BlazorShop.AI.StorefrontReverseEngineering.Tests.csproj --filter "FullyQualifiedName~StableCapture|FullyQualifiedName~Quality"
```

Exit criteria:

- [ ] Lazy-loaded fixture sections appear in captured output.
- [ ] Capture manifest records whether native or stitched capture was used.
- [ ] Quality report blocks obviously broken evidence.

## Phase 8 - Evidence Extraction And Manifest Normalization

Goal: normalize visual evidence into bounded, linked, schema-valid artifacts.

Tasks:

- [ ] Create `ElementEvidenceIndex`.
- [ ] Create computed-style property allowlist.
- [ ] Add element selection policy:
  - [ ] semantic landmarks
  - [ ] headings
  - [ ] links
  - [ ] buttons
  - [ ] inputs/selects
  - [ ] product-card-like candidates
  - [ ] images/assets
  - [ ] sections/articles
- [ ] Add element count limit.
- [ ] Add DOM depth limit.
- [ ] Extract typography evidence.
- [ ] Extract color/background/border/shadow evidence.
- [ ] Extract layout/display/grid/flex evidence.
- [ ] Extract positioning/sticky/fixed evidence.
- [ ] Extract transition/transform evidence.
- [ ] Extract asset metadata:
  - [ ] URL
  - [ ] media type if available
  - [ ] dimensions if image is loaded
  - [ ] source element
  - [ ] reference-only flag placeholder
- [ ] Write viewport-level `manifest.json`.
- [ ] Write page-level `capture-manifest.json`.
- [ ] Link all viewport evidence to project/page/run IDs.
- [ ] Add tests for bounded output size.
- [ ] Add tests for evidence links.

Guardrails:

- [ ] Do not store computed styles for every DOM node by default.
- [ ] Do not download full external asset mirrors.
- [ ] Do not treat source assets as reusable generated assets.

Verification:

```powershell
dotnet test tools\BlazorShop.AI.StorefrontReverseEngineering\tests\BlazorShop.AI.StorefrontReverseEngineering.Tests\BlazorShop.AI.StorefrontReverseEngineering.Tests.csproj --filter "FullyQualifiedName~Evidence"
```

Exit criteria:

- [ ] Evidence output is bounded.
- [ ] Every derived artifact can trace to evidence IDs.
- [ ] Missing referenced files fail validation.

## Phase 9 - Interaction State Capture

Goal: support configured interaction evidence without trying to auto-discover every UI behavior.

Tasks:

- [ ] Create `InteractionCapturePlan`.
- [ ] Create `InteractionActionDefinition`.
- [ ] Support configured actions:
  - [ ] click selector
  - [ ] hover selector
  - [ ] focus selector
  - [ ] scroll to selector
  - [ ] wait
- [ ] Capture before state.
- [ ] Execute action.
- [ ] Capture after state.
- [ ] Extract before/after screenshot and DOM/style diff metadata.
- [ ] Classify interaction model:
  - [ ] `Static`
  - [ ] `ClickDriven`
  - [ ] `HoverDriven`
  - [ ] `ScrollDriven`
  - [ ] `TimeDriven`
  - [ ] `Mixed`
  - [ ] `Unknown`
- [ ] Record failed selector as warning or blocking error based on policy.
- [ ] Add tests for mobile menu fixture.
- [ ] Add tests for accordion fixture.
- [ ] Add tests for hover fixture.

Guardrails:

- [ ] Phase 3A does not need automatic exhaustive interaction discovery.
- [ ] Do not execute destructive actions, forms, checkout, login, payment, or account mutations on reference sites.
- [ ] Only run configured safe selectors.

Verification:

```powershell
dotnet test tools\BlazorShop.AI.StorefrontReverseEngineering\tests\BlazorShop.AI.StorefrontReverseEngineering.Tests\BlazorShop.AI.StorefrontReverseEngineering.Tests.csproj --filter "FullyQualifiedName~Interaction"
```

Exit criteria:

- [ ] Configured interactions produce before/after evidence.
- [ ] Unsafe or unsupported actions are refused.
- [ ] Interaction evidence validates against schema.

## Phase 10 - Skill Catalog Manifest

Goal: capture the selected reverse-engineering skills as a domain-specific manifest.

Files/directories:

- `Skills/`
- `Schemas/skill-definition.schema.json`
- `docs/visual-reverse-engineering-skill/reference.md`

Tasks:

- [ ] Create `SkillDefinition`.
- [ ] Create `SkillExecutionType`:
  - [ ] `Deterministic`
  - [ ] `AI-assisted`
  - [ ] `Hybrid`
  - [ ] `DocumentationOnly`
- [ ] Create skill manifest schema.
- [ ] Define selected skills:
  - [ ] `storefront-reference-reconnaissance`
  - [ ] `stabilize-reference-page`
  - [ ] `capture-stable-full-page`
  - [ ] `capture-responsive-evidence`
  - [ ] `discover-visual-interactions`
  - [ ] `extract-visual-evidence`
  - [ ] `analyze-page-topology`
  - [ ] `create-visual-specification-draft`
  - [ ] `audit-reference-originality`
  - [ ] `validate-visual-evidence`
- [ ] For each skill, define:
  - [ ] name
  - [ ] version
  - [ ] category
  - [ ] purpose
  - [ ] inputs
  - [ ] outputs
  - [ ] dependencies
  - [ ] execution type
  - [ ] human review requirement
  - [ ] completion criteria
  - [ ] forbidden actions
- [ ] Add validation test for catalog completeness.
- [ ] Add test that skills are documentation/manifest, not runtime magic.

Guardrails:

- [ ] Do not copy external `SKILL.md` files into the runtime as executable behavior.
- [ ] Do not make skills a substitute for typed workflow services.
- [ ] Do not introduce React/Next/Tailwind output concepts into BlazorShop contracts.

Verification:

```powershell
dotnet test tools\BlazorShop.AI.StorefrontReverseEngineering\tests\BlazorShop.AI.StorefrontReverseEngineering.Tests\BlazorShop.AI.StorefrontReverseEngineering.Tests.csproj --filter "FullyQualifiedName~Skill"
```

Exit criteria:

- [ ] Selected skill catalog is explicit and validated.
- [ ] Each skill has input/output/completion criteria.
- [ ] Forbidden actions are documented and machine-readable.

## Phase 11 - Rule-Based Draft Analysis And AI Provider Abstraction

Goal: produce a neutral Visual Blueprint draft without requiring an AI provider.

Files/directories:

- `Analysis/`

Tasks:

- [ ] Create `IVisualAnalysisProvider`.
- [ ] Create `VisualAnalysisResult`.
- [ ] Create `AnalysisContext`.
- [ ] Implement `RuleBasedVisualAnalysisProvider`.
- [ ] Create `PageTopologyDraft`.
- [ ] Create `SectionCandidate`.
- [ ] Create `GlobalShellCandidate`.
- [ ] Create `PageSpecificationDraft`.
- [ ] Create `ComponentSpecificationDraft`.
- [ ] Create `VisualBlueprintDraft`.
- [ ] Include evidence references in every draft output.
- [ ] Add confidence fields.
- [ ] Add unsupported pattern warnings.
- [ ] Add optional AI inference log contract.
- [ ] Add tests proving rule-based analysis works without AI secrets.
- [ ] Add tests that AI provider settings are optional.

Guardrails:

- [ ] Do not call external AI in automated tests.
- [ ] Do not embed provider-specific DTOs in core contracts.
- [ ] Do not create Blazor component names as required output yet; use neutral component candidates.

Verification:

```powershell
dotnet test tools\BlazorShop.AI.StorefrontReverseEngineering\tests\BlazorShop.AI.StorefrontReverseEngineering.Tests\BlazorShop.AI.StorefrontReverseEngineering.Tests.csproj --filter "FullyQualifiedName~Analysis|FullyQualifiedName~Blueprint"
```

Exit criteria:

- [ ] Blueprint draft can be produced from fixture evidence.
- [ ] Blueprint references evidence IDs.
- [ ] AI provider abstraction exists but no provider is required.

## Phase 12 - Originality And Provenance Foundation

Goal: prevent reference evidence from being treated as reusable source material by default.

Tasks:

- [ ] Create `OriginalityAuditReport`.
- [ ] Create `ReferenceOnlyAsset`.
- [ ] Create `ProvenanceWarning`.
- [ ] Create `GenerationRestriction`.
- [ ] Flag likely brand/logo assets.
- [ ] Flag source copy/text blocks for review.
- [ ] Flag external images/videos as reference-only.
- [ ] Distinguish common visual grammar from distinctive source-specific elements.
- [ ] Record originality policy in `configuration.json`.
- [ ] Add markdown report writer.
- [ ] Add tests for fake brand asset fixture.
- [ ] Add tests that asset inventory defaults to reference-only unless explicitly allowed.

Guardrails:

- [ ] Do not make legal claims.
- [ ] Do not declare assets safe to reuse by default.
- [ ] Do not download and copy source brand assets into generated storefront output.

Verification:

```powershell
dotnet test tools\BlazorShop.AI.StorefrontReverseEngineering\tests\BlazorShop.AI.StorefrontReverseEngineering.Tests\BlazorShop.AI.StorefrontReverseEngineering.Tests.csproj --filter "FullyQualifiedName~Originality|FullyQualifiedName~Provenance"
```

Exit criteria:

- [ ] Originality audit exists and is linked to evidence.
- [ ] Reference-only assets are machine-readable.
- [ ] Generated phases have clear restrictions to consume later.

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

- [ ] Implement `capture --project`.
- [ ] Implement `validate --project`.
- [ ] Implement `analyze --project`.
- [ ] Implement `run --url --name --output-root` as convenience command.
- [ ] Add `--force`, `--resume`, and `--no-ai` options where appropriate.
- [ ] Write run summary to console.
- [ ] Write `reports/readiness-report.md`.
- [ ] Return non-zero exit code for blocking validation failures.
- [ ] Keep warnings non-blocking unless configured as strict.
- [ ] Add E2E local fixture test.
- [ ] Add timeout failure test.
- [ ] Add resume test after injected failure.

Guardrails:

- [ ] `run` must not generate storefront source.
- [ ] `run` must not write to `artifacts/storefront-builder/generated`.
- [ ] `run` must not mutate Starter or generated storefronts.

Verification:

```powershell
dotnet run --project tools\BlazorShop.AI.StorefrontReverseEngineering\BlazorShop.AI.StorefrontReverseEngineering.csproj -- run --url <fixture-url> --name FixtureDemo --output-root obj/storefront-reverse-engineering/projects --no-ai
dotnet test tools\BlazorShop.AI.StorefrontReverseEngineering\tests\BlazorShop.AI.StorefrontReverseEngineering.Tests\BlazorShop.AI.StorefrontReverseEngineering.Tests.csproj
```

Exit criteria:

- [ ] Local fixture full workflow passes.
- [ ] Output contains schema-valid artifacts.
- [ ] Readiness report is readable and points to artifact paths.
- [ ] Command can be repeated safely.

## Phase 14 - StorefrontBuilder Integration Contract Prep

Goal: prepare the handoff from ReverseEngineering evidence to StorefrontBuilder without forcing generation changes in Phase 3A.

Tasks:

- [ ] Define `visual-blueprint.draft.json` fields StorefrontBuilder may consume later.
- [ ] Add a compatibility note mapping current StorefrontBuilder artifacts to ReverseEngineering artifacts:
  - [ ] current `capture-manifest.json`
  - [ ] current `asset-manifest.yaml`
  - [ ] current `page-topology.yaml`
  - [ ] current `design-tokens.yaml`
  - [ ] current `ai-inference-log.json`
- [ ] Add explicit non-consumption warning: StorefrontBuilder generation does not yet use new ReverseEngineering artifacts until Phase 3B/3C.
- [ ] Add migration path for current Node capture script:
  - [ ] keep script as fallback/reference
  - [ ] optionally wrap through browser adapter
  - [ ] later retire only after parity tests
- [ ] Add docs for how generated storefront phases will consume blueprint later.

Guardrails:

- [ ] Do not break existing `build-storefront.ps1`.
- [ ] Do not break existing `regenerate-storefront.ps1`.
- [ ] Do not change generated storefront static validation gate unless needed for documentation only.

Verification:

```powershell
.\tools\BlazorShop.AI.StorefrontBuilder\build-storefront.ps1 -Url https://example.test -Name Demo -StoreKey sample -Mode plan-only
.\tools\BlazorShop.AI.StorefrontBuilder\tests\generation\Test-StorefrontBuilderCreateHardening.ps1
```

Exit criteria:

- [ ] Existing StorefrontBuilder command behavior remains intact.
- [ ] Future blueprint consumption is documented but not activated.
- [ ] No generation behavior changes are hidden inside Phase 3A.

## Phase 15 - Documentation And QA Closure

Goal: make Phase 3A maintainable for agents and humans.

Docs to update:

- [ ] `docs/architecture/11-storefront-builder.md`
- [ ] `docs/visual-reverse-engineering-skill/README.md`
- [ ] `docs/visual-reverse-engineering-skill/reference.md`
- [ ] `docs/visual-reverse-engineering-skill/how-to-generate-and-validate.md` if commands or boundaries affect operator flow
- [ ] `docs/agents/storefront-builder.md`
- [ ] `tools/BlazorShop.AI.StorefrontReverseEngineering/README.md`

QA tasks:

- [ ] Add focused architecture/source scan:
  - [ ] no production project references ReverseEngineering
  - [ ] no ReverseEngineering output writes into generated storefront roots
  - [ ] no Storefront V2 reference
  - [ ] no backend/core/API reference
- [ ] Add local fixture E2E capture test.
- [ ] Add schema validation test suite.
- [ ] Add workflow retry/resume tests.
- [ ] Add secret/cookie log redaction test.
- [ ] Add evidence provenance tests.
- [ ] Add artifact path safety tests.
- [ ] Add StorefrontBuilder compatibility smoke to prove existing builder still works.

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

- [ ] Phase 3A command documentation is copy-paste usable.
- [ ] Architecture docs explain the difference between StorefrontBuilder and StorefrontReverseEngineering.
- [ ] Local release gate passes.
- [ ] Known limitations are documented.
- [ ] Phase 3B handoff is explicit.

## Phase 3A Definition Of Done

Architecture:

- [ ] `BlazorShop.AI.StorefrontReverseEngineering` exists as independent development-time tooling.
- [ ] No production runtime project references it.
- [ ] No Commerce Node, Control Plane, Storefront V2, Starter, Runtime, Presentation, Client, Browser, Components, Domain, Application, or Infrastructure dependency is introduced.
- [ ] Artifact roots stay under approved `artifacts/storefront-reverse-engineering` and `obj/storefront-reverse-engineering` folders.

Runtime:

- [ ] CLI can initialize, inspect, discover, capture, analyze, validate, and run a local fixture workflow.
- [ ] Workflow runner supports retry, resume, skip completed steps, and step state persistence.
- [ ] Browser capture uses `IReferenceBrowser`.
- [ ] Artifact writes use `IVisualArtifactStore`.
- [ ] Schema validation uses `IVisualSchemaRegistry`.
- [ ] AI provider access is behind `IVisualAnalysisProvider`.
- [ ] Rule-based fallback works without AI secrets.

Evidence:

- [ ] Desktop, tablet, and mobile evidence exists.
- [ ] Screenshot, DOM, styles, boxes, assets, manifests, run logs, topology draft, blueprint draft, originality audit, and readiness report are written.
- [ ] Evidence artifacts are bounded and schema-valid.
- [ ] Evidence and analysis artifacts include provenance.
- [ ] Sensitive cookies, tokens, and credentials are not logged.

StorefrontBuilder compatibility:

- [ ] Existing StorefrontBuilder commands still run.
- [ ] Existing generated storefront roots and validation gates remain unchanged.
- [ ] New ReverseEngineering artifacts are not silently consumed by generation until a later approved phase.

Testing:

- [ ] Unit tests pass.
- [ ] Schema tests pass.
- [ ] Browser fixture tests pass.
- [ ] Workflow retry/resume tests pass.
- [ ] Artifact path safety tests pass.
- [ ] Secret redaction tests pass.
- [ ] StorefrontBuilder compatibility smoke passes.

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

- [ ] Keep Phase 3A narrow: executable evidence foundation, not full visual generation.
- [ ] Avoid creating a second generator competing with StorefrontBuilder.
- [ ] Make artifacts inspectable so AI agents can work from evidence rather than guessing.

Engineering lens:

- [ ] Prefer typed contracts and schema validation over procedural scripts as hidden source of truth.
- [ ] Reuse current Playwright capture behavior as baseline evidence, but put it behind replaceable interfaces.
- [ ] Keep the new tool independent of production V2 runtime projects.

DX lens:

- [ ] CLI commands must be predictable and copy-paste runnable.
- [ ] Errors must include problem, cause, and fix.
- [ ] Local fixture workflow must be the hello-world path.
- [ ] Internet URL capture must be manual/optional, not CI-required.

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

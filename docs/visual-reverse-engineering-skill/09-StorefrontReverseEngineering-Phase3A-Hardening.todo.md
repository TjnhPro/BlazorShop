# Storefront Reverse Engineering Phase 3A Hardening.todo

Scope: final hardening for `tools/BlazorShop.AI.StorefrontReverseEngineering` before Phase 3A closure.

Status: In progress

Target area:

```text
tools/BlazorShop.AI.StorefrontReverseEngineering
tools/BlazorShop.AI.StorefrontReverseEngineering/tests
docs/visual-reverse-engineering-skill
docs/architecture/11-storefront-builder.md
docs/agents/storefront-builder.md
scripts/qa
```

## Goal

Turn the current Phase 3A foundation from a scaffold/prototype-capable tool into an executable, deterministic, inspectable, and testable reverse-engineering runtime foundation.

The hardening target is not Visual Analysis & Ecommerce Mapping. It is the runtime layer below that work:

```text
Reference URL
-> real Chromium render
-> stable browser state
-> real screenshot, DOM, style, box, asset evidence
-> real quality gate and stitched fallback
-> one consistent capture snapshot per viewport
-> workflow runner run-state
-> per-artifact schema validation
-> quality-aware readiness
-> real safe interaction evidence
-> local browser fixture proof
```

## Current Verified Gaps

These gaps were verified from current source scans and focused test output.

Evidence capture:

- `PlaywrightReferenceBrowser` launches Chromium and captures screenshot/DOM, but computed styles are still produced by `BuildStyleSamples()`.
- `PlaywrightReferenceBrowser` bounding boxes are still produced by `BuildBoxes(...)`, not by `getBoundingClientRect()`.
- `PlaywrightReferenceBrowser` asset inventory still uses regex over HTML, not rendered-page asset metadata.
- `FixtureReferenceBrowser` uses `OnePixelPng` plus hard-coded style and box samples, so current tests do not prove real browser rendering.
- `NodePlaywrightReferenceBrowser` still throws `NotSupportedException` after script execution, so it should not be considered an active recommended adapter.

Stable capture:

- `StableFullPageCaptureService.Stabilize(...)` records stabilization step names, but does not execute DOM/font/image waits, noise hiding, or warm scroll in a browser session.
- Forced stitched fallback changes `CaptureMethod` to `stitched` and builds segment metadata, but does not create real segment screenshots or a stitched image.
- Capture quality checks are shallow and do not validate real image dimensions, blank regions, stitched output, or lower-page content.

Evidence consistency:

- `VisualProjectWorkflowService.CaptureAsync(...)` calls capture once to write raw artifacts and then calls browser capture again to normalize evidence.
- That can produce inconsistent raw and normalized evidence for dynamic pages.
- `VisualEvidenceExtractor.WriteViewportEvidenceAsync(...)` writes page-level `capture-manifest.json` per viewport, which can overwrite previous viewport entries.

Workflow:

- `SequentialWorkflowRunner<TContext>` exists and has tests, but CLI `run` currently orchestrates with project-status `if` branches instead of actual workflow steps.
- Normal runs do not create a real `runs/{runId}.json` that represents the CLI vertical slice.
- `inspect` can show latest run, but current CLI `run` does not consistently create one.

Schemas and readiness:

- `VisualSchemaRegistry` currently registers artifact kinds but enforces shared metadata only.
- There are no per-artifact domain schemas for nested fields, enum values, required evidence arrays, or manifest references.
- `ValidateAsync(...)` uses a hard-coded list with `captures/home/desktop-1440`, `tablet-768`, and `mobile-390` instead of reading the capture plan.
- Readiness checks file existence, but not quality reports, schema depth, evidence references, workflow run state, or originality restrictions.

Interactions:

- `InteractionCaptureService` does not execute browser actions.
- It writes identical before/after screenshots.
- It creates after DOM by appending a comment.
- It sets `DomChanged: true` without a real diff.

Boundary:

- Source scan did not find production projects referencing `BlazorShop.AI.StorefrontReverseEngineering`, so project isolation is currently acceptable.
- This hardening must preserve that boundary.

## Non-Goals

- Do not implement full website crawler.
- Do not implement design-token extraction completeness.
- Do not implement automatic semantic ecommerce mapping.
- Do not implement Presentation component catalog mapping.
- Do not implement human review UI.
- Do not make StorefrontBuilder consume `visual-blueprint.draft.json`.
- Do not generate Razor.
- Do not generate CSS/theme output.
- Do not copy source brand assets.
- Do not run authenticated checkout/account/payment automation.
- Do not introduce distributed queue or cloud browser farm.
- Do not change Commerce Node, Control Plane, Storefront V2, Starter, Client, Runtime, Presentation, Browser, Components, Domain, Application, or Infrastructure.

## Target Architecture

```text
CLI
  -> VisualProjectWorkflowContext
  -> SequentialWorkflowRunner
      -> InitializeProjectStep
      -> DiscoverReferenceStep
      -> CaptureViewportStep(desktop)
      -> CaptureViewportStep(tablet)
      -> CaptureViewportStep(mobile)
      -> AnalyzeDraftStep
      -> OriginalityAuditStep
      -> ValidateReadinessStep
  -> runs/{runId}.json

CaptureViewportStep
  -> IReferenceBrowser.OpenSessionAsync(...)
  -> IReferenceBrowserSession.NavigateAsync(...)
  -> IReferenceBrowserSession.StabilizeAsync(...)
  -> IReferenceBrowserSession.CaptureCurrentStateAsync(...)
  -> CapturedViewportResult
      -> raw artifacts
      -> normalized evidence
      -> quality report
      -> manifest references
```

## Phase H0 - Hardening Preparation And Guardrails

Goal: lock hardening scope and prevent accidental Phase 3B work.

Tasks:

- [x] Add this hardening plan to the Phase 3A documentation index.
- [x] Update `08-StorefrontReverseEngineering-Engine-Foundation.todo.md` with a note that Phase 3A has a hardening follow-up before closure.
- [x] Confirm all hardening work stays under:
  - [x] `tools/BlazorShop.AI.StorefrontReverseEngineering`
  - [x] `scripts/qa` for the hardening gate
  - [x] StorefrontBuilder/reverse-engineering docs
- [x] Add a source scan checklist that fails if production projects reference `BlazorShop.AI.StorefrontReverseEngineering`.
- [x] Add a source scan checklist that identifies prototype-only markers before closure:
  - [x] `OnePixelPng`
  - [x] `BuildStyleSamples`
  - [x] `BuildBoxes`
  - [x] `afterDom = before.DomHtml`
  - [x] `DomChanged: true`
  - [x] `CaptureMethod = "stitched"` without real stitch output
  - [x] `NotSupportedException` in recommended browser adapter path
- [x] Decide final test categories:
  - [x] unit
  - [x] schema
  - [x] synthetic fixture
  - [x] real Playwright local integration
  - [x] full hardening gate

Guardrails:

- [x] No StorefrontBuilder generation behavior changes in this hardening.
- [x] No generated storefront root writes.
- [x] No internet dependency in automated tests.
- [x] No production runtime references to reverse-engineering tooling.

Verification:

```powershell
rg -n "BlazorShop.AI.StorefrontReverseEngineering|StorefrontReverseEngineering" BlazorShop.PresentationV2 BlazorShop.Domain BlazorShop.Application BlazorShop.Infrastructure BlazorShop.ServiceDefaults BlazorShop.Tests.V2 BlazorShop.sln --glob "!bin/**" --glob "!obj/**"
rg -n "OnePixelPng|BuildStyleSamples|BuildBoxes|afterDom = before.DomHtml|DomChanged: true|NotSupportedException" tools/BlazorShop.AI.StorefrontReverseEngineering --glob "!bin/**" --glob "!obj/**"
```

Exit criteria:

- [x] Hardening scope is documented.
- [x] Prototype markers are tracked as intentional closure blockers.
- [x] Boundary scan remains clean.

Implementation evidence:

- Boundary scan was run against active production projects and `BlazorShop.sln`; `rg` returned no matches.
- Prototype-marker scan found the expected hardening blockers in the reverse-engineering tool only: `OnePixelPng`, `BuildStyleSamples`, `BuildBoxes`, `afterDom = before.DomHtml`, `DomChanged: true`, `CaptureMethod = "stitched"` metadata, and the deferred Node adapter `NotSupportedException`.
- Final test categories for this hardening are unit, schema, synthetic fixture, real Playwright local integration, and full hardening gate.

## Phase H1 - Real Playwright Browser Session And Evidence Extraction

Goal: collect screenshot, DOM, computed styles, bounding boxes, and asset metadata from the same rendered Chromium page state.

Current files:

- `Browser/ReferenceBrowserContracts.cs`
- `Browser/PlaywrightReferenceBrowser.cs`
- `Browser/FixtureReferenceBrowser.cs`
- `Browser/ReferenceBrowserFactory.cs`
- `Evidence/VisualEvidenceExtractor.cs`
- `tests/.../BrowserCaptureTests.cs`
- `tests/.../EvidenceExtractionTests.cs`

Tasks:

- [x] Introduce `IReferenceBrowserSession : IAsyncDisposable`.
- [x] Add browser lifecycle methods:
  - [x] `NavigateAsync`
  - [x] `StabilizeAsync`
  - [x] `CaptureCurrentStateAsync`
  - [x] `ExecuteAsync`
  - [x] `DisposeAsync`
- [x] Keep one `IPlaywright`, `IBrowser`, `IBrowserContext`, and `IPage` alive for the viewport session.
- [x] Keep `IReferenceBrowser` only as compatibility/facade if useful.
- [x] Replace `BuildStyleSamples()` in the Playwright path with `page.EvaluateAsync` using `getComputedStyle()`.
- [x] Replace `BuildBoxes(...)` in the Playwright path with `getBoundingClientRect()`.
- [x] Extract element evidence candidates from the live DOM:
  - [x] tag name
  - [x] stable selector
  - [x] generated evidence selector
  - [x] semantic role
  - [x] bounded text snippet
  - [x] visibility
  - [x] display
  - [x] position
  - [x] z-index
  - [x] typography
  - [x] color/background
  - [x] border/radius/shadow
  - [x] grid/flex
  - [x] overflow
  - [x] transform/transition
- [x] Add stable evidence identity:
  - [x] prefer `id`
  - [x] then stable `data-*`
  - [x] then semantic tag + class
  - [x] then generated DOM path
  - [x] reject dynamic class hash as the only identity when possible
- [x] Extract rendered asset metadata from the live page:
  - [x] `img.src`
  - [x] `img.currentSrc`
  - [x] `srcset`
  - [x] `picture/source`
  - [x] CSS `background-image`
  - [x] inline SVG metadata
  - [x] video source/poster
  - [x] used font families
  - [x] natural width/height
  - [x] rendered width/height
  - [x] source element evidence ID
  - [x] `ReferenceOnly = true` by default
- [x] Enforce capture limits from `CapturePolicy`:
  - [x] max elements
  - [x] max depth
  - [x] max text length
  - [x] max properties
  - [x] max assets
  - [x] max page height
- [x] Ignore hidden/script/style/template/noise nodes unless policy includes them.
- [x] Update contracts so style, box, asset, and normalized evidence share `EvidenceId`.
- [x] Add tests that assert Playwright path no longer returns only `body/img/viewport/document` placeholders.
- [x] Keep synthetic fixture tests only for fast contract tests; do not let them be release evidence for browser rendering.

Guardrails:

- [x] Do not start a new Chromium process for each sub-step in one viewport.
- [x] Do not download external asset mirrors.
- [x] Do not log cookies, tokens, or headers.

Verification:

```powershell
dotnet test tools\BlazorShop.AI.StorefrontReverseEngineering\tests\BlazorShop.AI.StorefrontReverseEngineering.Tests\BlazorShop.AI.StorefrontReverseEngineering.Tests.csproj --filter "Browser|Evidence"
```

Exit criteria:

- [x] Sticky header fixture returns real `position: sticky`.
- [x] Product card fixture returns non-null real bounding box.
- [x] Hero fixture returns real typography and grid/flex evidence.
- [x] Asset inventory includes natural/rendered dimensions where available.
- [x] Production Playwright path has no hard-coded style/box placeholders.
- [x] Evidence extraction is bounded by policy.

Implementation evidence:

- Added stateful `IReferenceBrowserSession` lifecycle while preserving `IReferenceBrowser.CaptureAsync` as compatibility facade.
- Replaced Playwright placeholder style/box/asset extraction with a rendered-page `page.EvaluateAsync` evidence collector using `getComputedStyle()`, `getBoundingClientRect()`, current image sources, CSS backgrounds, inline SVG, video/poster metadata, and font-family evidence.
- Added shared `EvidenceId` across style, box, asset, and normalized element evidence.
- Verification: `dotnet test tools\BlazorShop.AI.StorefrontReverseEngineering\tests\BlazorShop.AI.StorefrontReverseEngineering.Tests\BlazorShop.AI.StorefrontReverseEngineering.Tests.csproj --filter "Browser|Evidence"` passed: 10 tests.

## Phase H2 - Real Page Stabilization And Stitched Full-Page Fallback

Goal: make `StableFullPageCaptureService` execute stabilization and stitched capture, not just record metadata.

Current files:

- `Browser/StableFullPageCaptureService.cs`
- `Browser/StableCaptureContracts.cs`
- `Browser/PlaywrightReferenceBrowser.cs`
- `tests/.../StableCaptureQualityTests.cs`

Tasks:

- [x] Move stabilization onto `IReferenceBrowserSession`.
- [x] Implement real stabilization steps:
  - [x] wait `DOMContentLoaded`
  - [x] wait network idle with fallback
  - [x] wait `document.fonts.ready` when available
  - [x] wait important images where `complete && naturalWidth > 0`
  - [x] inject reduced-motion capture style when policy allows
  - [x] pause or neutralize known carousel/time-driven noise when configured
  - [x] hide configured noise selectors
  - [x] warm scroll from top to bottom
  - [x] settled delay at each scroll step
  - [x] return to top
  - [x] write actual hidden noise selectors and warnings
- [x] Add native full-page quality evaluator:
  - [x] PNG decodes
  - [x] expected width/height
  - [x] not empty
  - [x] not near single-color blank
  - [x] document height matches manifest
  - [x] lower-page content is present when expected
- [x] Add real viewport segment capture:
  - [x] calculate scroll positions
  - [x] overlap segments
  - [x] scroll and settle per segment
  - [x] capture viewport screenshot per segment
  - [x] write `viewport-segments/*.png`
  - [x] write segment metadata: id, y, crop, effective height
  - [x] enforce max segment count
- [x] Add cross-platform image stitching:
  - [x] choose approved .NET image package
  - [x] crop overlap
  - [x] compose final `full-page.png`
  - [x] write `stitch-manifest.json`
  - [x] mark `CaptureMethod = stitched` only when stitched output exists
- [x] Preserve or cleanup segments based on policy.
- [x] Update `CaptureQualityReport`:
  - [x] native attempt result
  - [x] fallback reason
  - [x] segment count
  - [x] final dimensions
  - [x] final method
  - [x] blocking findings
  - [x] warnings
- [x] Add tests for:
  - [x] warm scroll reveals lazy content
  - [x] noise banner hidden by policy
  - [x] forced stitched fallback creates segment files
  - [x] stitched image dimensions reflect segment composition
  - [x] failed native capture triggers fallback
  - [x] final failed capture blocks readiness

Guardrails:

- [x] Do not rely solely on native Playwright full-page screenshot.
- [x] Do not use OS-specific tools such as `sips`.
- [x] Do not require FFmpeg.
- [x] Do not mark metadata as stitched without real image output.

Verification:

```powershell
dotnet test tools\BlazorShop.AI.StorefrontReverseEngineering\tests\BlazorShop.AI.StorefrontReverseEngineering.Tests\BlazorShop.AI.StorefrontReverseEngineering.Tests.csproj --filter "StableCapture|Stitch|Quality"
```

Exit criteria:

- [x] Lazy-loaded fixture appears after real warm scroll.
- [x] Noise selectors are actually hidden when policy allows.
- [x] Forced fallback creates real segment images.
- [x] Stitched output is a real composed image.
- [x] Native quality failure triggers fallback.
- [x] Readiness fails if both native and stitched capture fail.

Implementation evidence:

- Stabilization now runs through `IReferenceBrowserSession` and the Playwright adapter performs DOM/network/font/image waits, reduced-motion style injection, noise hiding, warm scroll, settle delays, and return-to-top.
- Forced fallback now scrolls the same browser session, captures viewport PNG segments, writes `viewport-segments/*.png`, composes a real stitched PNG, and writes `stitch-manifest.json`.
- Magick.NET-Q8-AnyCPU was selected for cross-platform PNG load/crop/composite/write after ImageSharp 4.0 failed local build without a Six Labors license key.
- Verification: `dotnet test tools\BlazorShop.AI.StorefrontReverseEngineering\tests\BlazorShop.AI.StorefrontReverseEngineering.Tests\BlazorShop.AI.StorefrontReverseEngineering.Tests.csproj --filter "StableCapture|Stitch|Quality"` passed: 3 tests.

## Phase H3 - Single-Snapshot Capture Consistency

Goal: ensure raw artifacts and normalized evidence come from one capture result per viewport.

Current files:

- `Application/VisualCaptureService.cs`
- `Application/VisualProjectWorkflowService.cs`
- `Evidence/VisualEvidenceExtractor.cs`
- `Contracts/CoreContracts.cs`
- `tests/.../BrowserCaptureTests.cs`
- `tests/.../EvidenceExtractionTests.cs`

Tasks:

- [x] Introduce `CapturedViewportResult`.
- [x] Include:
  - [x] `CaptureViewportManifest`
  - [x] `BrowserCaptureResult`
  - [x] `CaptureQualityReport`
  - [x] `PageStabilizationReport`
  - [x] capture correlation ID
  - [x] run ID
  - [x] browser session ID
- [x] Change `VisualCaptureService.CaptureViewportAsync(...)` to return `CapturedViewportResult`.
- [x] Make `VisualEvidenceExtractor` consume `CapturedViewportResult`.
- [x] Remove the second browser capture call from `VisualProjectWorkflowService.CaptureAsync(...)`.
- [x] Add capture correlation ID to:
  - [x] viewport manifest
  - [x] styles evidence
  - [x] boxes evidence
  - [x] asset inventory
  - [x] element evidence index
  - [x] quality report
- [x] Create aggregated page capture manifest:
  - [x] contains every configured viewport
  - [x] contains manifest paths
  - [x] contains quality paths
  - [x] contains normalized evidence paths
  - [x] records complete/partial/failed state
- [x] Stop overwriting `captures/{pageId}/capture-manifest.json` per viewport.
- [x] Validate raw and normalized evidence share the same capture correlation ID.
- [x] Add a test that fails if capture is called twice for one viewport.
- [x] Add a test that fails when manifest references mismatch correlation IDs.

Guardrails:

- [x] Do not normalize from a fresh browser state.
- [x] Do not analyze a viewport before normalized evidence and raw manifest agree.

Verification:

```powershell
dotnet test tools\BlazorShop.AI.StorefrontReverseEngineering\tests\BlazorShop.AI.StorefrontReverseEngineering.Tests\BlazorShop.AI.StorefrontReverseEngineering.Tests.csproj --filter "Consistency|Manifest|Evidence"
```

Exit criteria:

- [x] One browser capture call per viewport step.
- [x] Raw and normalized artifacts share one correlation ID.
- [x] Page manifest contains desktop, tablet, and mobile.
- [x] No page manifest overwrite issue remains.
- [x] Mismatched evidence cannot pass readiness.

Implementation evidence:

- Added `CapturedViewportResult` and changed capture service to return the raw capture, viewport manifest, stabilization report, quality report, correlation ID, run ID, and browser session ID together.
- `VisualProjectWorkflowService.CaptureAsync(...)` now passes the same captured result into `VisualEvidenceExtractor`; the second browser capture call was removed.
- Page capture manifest now merges viewport manifest paths, quality paths, normalized evidence paths, and correlation IDs instead of overwriting per viewport.
- Verification: `dotnet test tools\BlazorShop.AI.StorefrontReverseEngineering\tests\BlazorShop.AI.StorefrontReverseEngineering.Tests\BlazorShop.AI.StorefrontReverseEngineering.Tests.csproj --filter "Consistency|Manifest|Evidence"` passed: 14 tests.

## Phase H4 - CLI Workflow Runner Integration

Goal: make `run`, `resume`, `force-step`, and `inspect` use persisted workflow state.

Current files:

- `Cli/CliHost.cs`
- `Application/VisualProjectWorkflowService.cs`
- `Workflows/SequentialWorkflowRunner.cs`
- `Workflows/WorkflowContracts.cs`
- `Application/VisualProjectService.cs`
- `tests/.../WorkflowRunnerTests.cs`
- `tests/.../EndToEndCliTests.cs`

Tasks:

- [x] Create `VisualProjectWorkflowContext`.
- [x] Context includes:
  - [x] project
  - [x] configuration
  - [x] artifact root
  - [x] capture plan
  - [x] artifact store
  - [x] browser session factory
  - [x] analysis provider
  - [x] run ID
  - [x] current command options
- [x] Implement actual workflow steps:
  - [x] `InitializeProjectStep`
  - [x] `DiscoverReferenceStep`
  - [x] `CaptureViewportStep`
  - [x] `AnalyzeDraftStep`
  - [x] `OriginalityAuditStep`
  - [x] `ValidateReadinessStep`
- [x] Give every step:
  - [x] stable name
  - [x] input artifact list
  - [x] output artifact list
  - [x] completion check
  - [x] retryable error mapping
  - [x] non-retryable error mapping
  - [x] warning collection
  - [x] status transition
- [x] Change CLI `run` to create a run ID and invoke `SequentialWorkflowRunner`.
- [x] Persist real `runs/{runId}.json`.
- [x] Add `--resume --run-id <id>`.
- [x] Resume should:
  - [x] load existing run
  - [x] skip succeeded/skipped steps
  - [x] retry failed/canceled/pending steps
  - [x] keep project status consistent
- [x] Add `--force-step <step-name>`.
- [x] Force-step should:
  - [x] rerun selected step
  - [x] invalidate dependent downstream steps or rerun them according to policy
  - [x] avoid unrelated artifact overwrite
- [x] Update `inspect` to show:
  - [x] latest run ID
  - [x] run status
  - [x] step status
  - [x] retry count
  - [x] latest failure
  - [x] readiness summary
  - [x] blueprint path
- [x] Add cancellation tests for actual CLI workflow.

Guardrails:

- [x] Do not keep project-status-only orchestration as the canonical `run` path.
- [x] Do not hide failed step details behind only a final exit code.

Verification:

```powershell
dotnet test tools\BlazorShop.AI.StorefrontReverseEngineering\tests\BlazorShop.AI.StorefrontReverseEngineering.Tests\BlazorShop.AI.StorefrontReverseEngineering.Tests.csproj --filter "Workflow|Cli|Resume"
```

Exit criteria:

- [x] Normal CLI `run` creates `runs/{runId}.json`.
- [x] Run log includes duration, status, retry count, warnings, and errors.
- [x] Resume skips successful steps.
- [x] Retry works on actual browser/capture workflow step.
- [x] `--force-step` works and has tests.
- [x] `inspect` reflects real latest run state.

Implementation evidence:

- Added `VisualProjectWorkflowContext` and typed workflow steps for initialize, discovery, per-viewport capture, analysis, originality audit check, and readiness validation.
- CLI `run`/`resume` now creates or resumes a `runs/{runId}.json` through `SequentialWorkflowRunner`; `--force-step` reruns the selected step and downstream steps.
- `inspect` now prints latest run status, step statuses, retry counts, and latest failure details from persisted workflow state.
- Verification: `dotnet test tools\BlazorShop.AI.StorefrontReverseEngineering\tests\BlazorShop.AI.StorefrontReverseEngineering.Tests\BlazorShop.AI.StorefrontReverseEngineering.Tests.csproj --filter "Workflow|Cli|Resume"` passed: 16 tests.

## Phase H5 - Per-Artifact Schemas And Readiness Gate

Goal: replace metadata-only validation with artifact-kind-specific schema and quality-aware readiness.

Current files:

- `Validation/VisualSchemaRegistry.cs`
- `Validation/VisualSchemaValidator.cs`
- `Validation/ReadinessReport.cs`
- `Schemas/`
- `Application/VisualProjectWorkflowService.cs`
- `tests/.../SchemaArtifactTests.cs`

Tasks:

- [x] Create machine-readable schema files for first-class artifacts:
  - [x] `visual-project`
  - [x] `configuration`
  - [x] `reference-site-profile`
  - [x] `reconnaissance`
  - [x] `capture-plan`
  - [x] `capture-viewport-manifest`
  - [x] `page-capture-manifest`
  - [x] `capture-quality-report`
  - [x] `page-stabilization-report`
  - [x] `computed-style-evidence`
  - [x] `element-box-evidence`
  - [x] `element-evidence-index`
  - [x] `asset-inventory`
  - [x] `interaction-evidence`
  - [x] `page-topology-draft`
  - [x] `page-specification-draft`
  - [x] `component-specification-draft`
  - [x] `visual-blueprint-draft`
  - [x] `ai-inference-log`
  - [x] `originality-audit`
  - [x] `workflow-run`
  - [x] `readiness-report`
  - [x] `skill-definition`
- [x] Schema must validate:
  - [x] required fields
  - [x] nested objects
  - [x] enum values
  - [x] arrays and array item shape
  - [x] URI/path strings where appropriate
  - [x] numeric minimum/maximum
  - [x] schema version
  - [x] artifact kind
- [x] Change `VisualSchemaRegistry` to load schema files instead of metadata-only definitions.
- [x] Add schema fixture tests:
  - [x] valid fixture for each artifact kind
  - [x] missing required domain field
  - [x] invalid nested shape
  - [x] invalid enum
  - [x] stale schema version
- [x] Make readiness derive requirements from `capture-plan.json`.
- [x] For every configured page/viewport, readiness validates:
  - [x] viewport manifest exists
  - [x] screenshot exists
  - [x] DOM exists
  - [x] styles exist
  - [x] boxes exist
  - [x] assets exist
  - [x] normalized evidence exists
  - [x] quality report exists and passed
  - [x] capture correlation IDs match
- [x] Validate page-level manifest references.
- [x] Validate latest workflow run state.
- [x] Validate visual blueprint evidence references.
- [x] Validate originality audit restrictions.
- [x] Validate sensitive data redaction where possible.
- [x] Normalize severity:
  - [x] `blocking`
  - [x] `warning`
  - [x] `info`
- [x] Blocking findings include:
  - [x] capture failed
  - [x] quality failed
  - [x] missing screenshot
  - [x] missing DOM
  - [x] empty computed-style evidence
  - [x] no useful bounding boxes
  - [x] missing manifest reference
  - [x] invalid schema
  - [x] missing evidence reference from blueprint
  - [x] failed latest run
  - [x] missing provenance
- [x] Re-validation pass after recovery moves project back to `DraftReady`.

Guardrails:

- [x] Do not treat Markdown reports as source of truth.
- [x] Do not hard-code `home/desktop-1440` as readiness input.
- [x] Do not pass readiness on file existence alone.

Verification:

```powershell
dotnet test tools\BlazorShop.AI.StorefrontReverseEngineering\tests\BlazorShop.AI.StorefrontReverseEngineering.Tests\BlazorShop.AI.StorefrontReverseEngineering.Tests.csproj --filter "Schema|Readiness|Validation"
```

Exit criteria:

- [x] Each first-class JSON artifact validates against a real schema file.
- [x] Artifact missing a domain field is rejected.
- [x] Capture plan drives readiness requirements.
- [x] Quality failure blocks readiness.
- [x] Missing evidence reference blocks readiness.
- [x] Recovery validation can move `ValidationFailed` back to `DraftReady`.

Implementation evidence:

- Added machine-readable schema descriptor files under `tools/BlazorShop.AI.StorefrontReverseEngineering/Schemas` and changed `VisualSchemaRegistry` to load them.
- `VisualSchemaValidator` now checks required domain paths, array shape, enum values, numeric bounds, schema version, and artifact kind.
- Readiness now derives capture requirements from `capture-plan.json`, checks quality reports, validates page-manifest references/correlation, checks latest workflow run, checks blueprint evidence IDs, and supports recovery from `ValidationFailed` to `DraftReady`.
- Verification: `dotnet test tools\BlazorShop.AI.StorefrontReverseEngineering\tests\BlazorShop.AI.StorefrontReverseEngineering.Tests\BlazorShop.AI.StorefrontReverseEngineering.Tests.csproj --filter "Schema|Readiness|Validation"` passed: 10 tests.

## Phase H6 - Real Interaction Capture And Safe Action Guard

Goal: capture before/after interaction evidence by executing safe browser actions in the same rendered page session.

Current files:

- `Interactions/InteractionCaptureService.cs`
- `Interactions/InteractionContracts.cs`
- `Browser/ReferenceBrowserContracts.cs`
- `tests/.../InteractionCaptureTests.cs`

Tasks:

- [x] Add interaction methods to `IReferenceBrowserSession`.
- [x] Support safe actions:
  - [x] click selector
  - [x] hover selector
  - [x] focus selector
  - [x] scroll to selector
  - [x] wait
  - [x] optional safe key press
- [x] Capture before state:
  - [x] screenshot
  - [x] DOM
  - [x] computed styles
  - [x] boxes
  - [x] assets if relevant
- [x] Execute action.
- [x] Wait settled according to policy.
- [x] Capture after state.
- [x] Compute actual differences:
  - [x] screenshot hash difference
  - [x] DOM diff summary
  - [x] style diff summary
  - [x] changed element evidence IDs
- [x] Set `DomChanged` only when DOM really changed.
- [x] Set `StyleChanged` only when style evidence really changed.
- [x] Write:
  - [x] `before.png`
  - [x] `after.png`
  - [x] `before.dom.html`
  - [x] `after.dom.html`
  - [x] `before.styles.json`
  - [x] `after.styles.json`
  - [x] `interaction-evidence.json`
- [x] Enforce safe-action guard:
  - [x] reject form submit
  - [x] reject checkout
  - [x] reject payment
  - [x] reject login
  - [x] reject account mutation
  - [x] reject delete/purchase actions
  - [x] reject navigation outside allowed domain during interaction
  - [x] redact action logs
- [x] Add tests:
  - [x] mobile menu click changes DOM/style/screenshot
  - [x] accordion click changes DOM/style/screenshot
  - [x] product card hover changes style/screenshot
  - [x] missing selector warning/blocking behavior
  - [x] unsafe selector refusal
  - [x] external navigation refusal

Guardrails:

- [x] Do not fake after state by appending comments.
- [x] Do not reuse before screenshot as after screenshot.
- [x] Do not execute forms, checkout, login, account, purchase, or payment actions.

Verification:

```powershell
dotnet test tools\BlazorShop.AI.StorefrontReverseEngineering\tests\BlazorShop.AI.StorefrontReverseEngineering.Tests\BlazorShop.AI.StorefrontReverseEngineering.Tests.csproj --filter "Interaction"
```

Exit criteria:

- [x] Before/after screenshots are distinct for fixture hover/click actions.
- [x] DOM/style diff is computed from real browser state.
- [x] Unsafe action is refused before execution.
- [x] Interaction evidence includes provenance and validates schema.

Implementation evidence:

- `InteractionCaptureService` now opens one browser session, captures before state, executes configured safe actions, captures after state, writes before/after screenshots, DOM, styles, and computes screenshot/DOM/style diffs.
- Playwright session supports click, hover, focus, scroll-to-selector, wait, safe key press, scroll-to-y, and refuses cross-origin navigation.
- Unsafe selectors for forms, checkout, payment, login, account, delete, and purchase are rejected before execution.
- Verification: `dotnet test tools\BlazorShop.AI.StorefrontReverseEngineering\tests\BlazorShop.AI.StorefrontReverseEngineering.Tests\BlazorShop.AI.StorefrontReverseEngineering.Tests.csproj --filter "Interaction"` passed: 5 tests.

## Phase H7 - Local HTTP Fixture And Real Playwright E2E

Goal: prove the hardened runtime with Chromium against a deterministic local HTTP site.

Current files:

- `tests/.../Fixtures/static-storefront.html`
- `tests/.../*.cs`
- potential new script `scripts/qa/run-storefront-reverse-engineering-phase3a-gate.ps1`

Tasks:

- [x] Add local HTTP fixture runner instead of relying on `file://` for browser integration.
- [x] Fixture must be deterministic and offline.
- [x] Fixture should include:
  - [x] sticky header
  - [x] hero
  - [x] product grid
  - [x] product card hover state
  - [x] mobile menu
  - [x] accordion
  - [x] lazy-loaded section
  - [x] cookie banner/noise selector
  - [x] responsive column changes
  - [x] CSS background image
  - [x] inline SVG
  - [x] fake video/poster metadata
  - [x] fake brand asset for originality audit
- [x] Add real Playwright local integration tests:
  - [x] Chromium launches
  - [x] desktop screenshot has real dimensions
  - [x] mobile screenshot has real dimensions
  - [x] sticky header style is captured
  - [x] product card bounding box exists
  - [x] mobile grid differs from desktop grid
  - [x] lazy section appears after stabilization
  - [x] cookie banner is hidden when policy allows
  - [x] hover before/after differs
  - [x] accordion click before/after differs
  - [x] stitched fallback creates real image
  - [x] full workflow produces readiness pass
- [x] Keep fast unit tests separate from browser integration tests.
- [x] Add category/filter names:
  - [x] `Unit`
  - [x] `Schema`
  - [x] `Playwright`
  - [x] `EndToEnd`
- [x] Add browser installation docs for .NET Playwright.

Guardrails:

- [x] Browser E2E must not depend on internet.
- [x] Browser E2E must stop local fixture server in `finally`.
- [x] Do not use source/reference external sites in CI.

Verification:

```powershell
dotnet test tools\BlazorShop.AI.StorefrontReverseEngineering\tests\BlazorShop.AI.StorefrontReverseEngineering.Tests\BlazorShop.AI.StorefrontReverseEngineering.Tests.csproj --filter "Playwright|EndToEnd"
```

Exit criteria:

- [x] Browser integration tests use real Chromium.
- [x] Test fixture proves responsive, lazy, noise, hover, click, asset, and stitched behavior.
- [x] Full vertical slice works without internet.

Implementation evidence:

- Added `TestHttpFixtureServer` for deterministic `127.0.0.1` HTTP fixture serving HTML and fake local assets.
- Expanded `static-storefront.html` with responsive layout, JS-driven mobile menu/accordion/lazy section, cookie noise selector, CSS background, inline SVG, video/poster, and fake brand assets.
- Added real Playwright integration tests for rendered evidence, responsive viewport differences, stitched fallback, interaction diffs, and full workflow readiness.
- Verification: `dotnet test tools\BlazorShop.AI.StorefrontReverseEngineering\tests\BlazorShop.AI.StorefrontReverseEngineering.Tests\BlazorShop.AI.StorefrontReverseEngineering.Tests.csproj --filter "Playwright|EndToEnd"` passed: 12 tests.

## Phase H8 - CLI Semantics, Force Behavior, And Inspect Reports

Goal: make CLI behavior predictable for developers and AI agents.

Current files:

- `Cli/CliHost.cs`
- `Cli/CommandOptions.cs`
- `Application/VisualProjectService.cs`
- `Application/VisualProjectWorkflowService.cs`
- `README.md`

Tasks:

- [ ] Define final command surface:
  - [ ] `init`
  - [ ] `discover`
  - [ ] `capture`
  - [ ] `analyze`
  - [ ] `validate`
  - [ ] `inspect`
  - [ ] `run`
  - [ ] `resume` or `run --resume`
- [ ] Add `--run-id`.
- [ ] Add `--force-step`.
- [ ] Clarify `--force`:
  - [ ] deletes old project root safely before init
  - [ ] only under approved roots
  - [ ] removes old capture/analysis/run artifacts
  - [ ] does not touch generated storefront roots
- [ ] Add tests for safe force cleanup.
- [ ] Add tests for unsafe force root rejection.
- [ ] Align report names:
  - [ ] `readiness-report.json`
  - [ ] `readiness-report.md`
  - [ ] `originality-audit.json`
  - [ ] `originality-audit.md`
- [ ] Update inspect output:
  - [ ] project status
  - [ ] latest run ID
  - [ ] run status
  - [ ] step table
  - [ ] retry count
  - [ ] latest failure
  - [ ] readiness result
  - [ ] blueprint path
  - [ ] artifact root
- [ ] Ensure error messages follow problem/cause/fix style.

Guardrails:

- [ ] Do not make `--force` ambiguous.
- [ ] Do not inspect non-existent report filenames.
- [ ] Do not require reading temporary artifacts to understand run status.

Verification:

```powershell
dotnet run --project tools\BlazorShop.AI.StorefrontReverseEngineering\BlazorShop.AI.StorefrontReverseEngineering.csproj -- --help
dotnet test tools\BlazorShop.AI.StorefrontReverseEngineering\tests\BlazorShop.AI.StorefrontReverseEngineering.Tests\BlazorShop.AI.StorefrontReverseEngineering.Tests.csproj --filter "Cli|Lifecycle|Security"
```

Exit criteria:

- [ ] CLI help is copy-paste usable.
- [ ] `inspect` shows real run/readiness state.
- [ ] `--force`, `--resume`, and `--force-step` have clear tested semantics.

## Phase H9 - Full Phase 3A Hardening Gate

Goal: provide one local release gate proving Phase 3A hardening without GitHub Actions or internet.

New file:

```text
scripts/qa/run-storefront-reverse-engineering-phase3a-gate.ps1
```

Script tasks:

- [ ] Set `$ErrorActionPreference = "Stop"`.
- [ ] Build tool project.
- [ ] Run fast tests.
- [ ] Ensure Playwright browser installed or print actionable setup message.
- [ ] Start local HTTP fixture server.
- [ ] Run full reverse-engineering workflow with `--no-ai`.
- [ ] Force stitched fallback proof.
- [ ] Run interaction proof.
- [ ] Validate artifacts.
- [ ] Validate run log.
- [ ] Validate readiness pass.
- [ ] Run boundary scan.
- [ ] Run prototype-marker scan.
- [ ] Run StorefrontBuilder compatibility smoke:
  - [ ] `build-storefront.ps1 -Mode plan-only`
  - [ ] existing StorefrontBuilder create hardening test
- [ ] Stop fixture server in `finally`.
- [ ] Write a short gate report under `obj/storefront-reverse-engineering/reports`.

Suggested commands inside gate:

```powershell
dotnet build tools\BlazorShop.AI.StorefrontReverseEngineering\BlazorShop.AI.StorefrontReverseEngineering.csproj
dotnet test tools\BlazorShop.AI.StorefrontReverseEngineering\tests\BlazorShop.AI.StorefrontReverseEngineering.Tests\BlazorShop.AI.StorefrontReverseEngineering.Tests.csproj
dotnet run --project tools\BlazorShop.AI.StorefrontReverseEngineering\BlazorShop.AI.StorefrontReverseEngineering.csproj -- --help
.\tools\BlazorShop.AI.StorefrontBuilder\build-storefront.ps1 -Url https://example.test -Name Demo -StoreKey sample -Mode plan-only
```

Guardrails:

- [ ] Gate must not require external website.
- [ ] Gate must not leave long-running fixture server processes.
- [ ] Gate must not mutate generated storefronts.
- [ ] Gate must not depend on GitHub Actions.

Exit criteria:

- [ ] One script proves build, tests, browser E2E, artifacts, readiness, boundary, and compatibility.
- [ ] Failure output points to report path and exact failing step.

## Phase H10 - Documentation And Phase 3B Handoff

Goal: document the hardened runtime honestly and prevent Phase 3B from patching Phase 3A runtime gaps.

Docs to update:

- [ ] `tools/BlazorShop.AI.StorefrontReverseEngineering/README.md`
- [ ] `docs/architecture/11-storefront-builder.md`
- [ ] `docs/visual-reverse-engineering-skill/README.md`
- [ ] `docs/visual-reverse-engineering-skill/reference.md`
- [ ] `docs/agents/storefront-builder.md`
- [ ] `08-StorefrontReverseEngineering-Engine-Foundation.todo.md`
- [ ] this hardening plan with implementation evidence

Documentation content:

- [ ] Explain difference between StorefrontBuilder and StorefrontReverseEngineering.
- [ ] Explain artifact roots.
- [ ] Explain CLI commands and examples.
- [ ] Explain browser installation.
- [ ] Explain fixture-based testing.
- [ ] Explain workflow runs and resume.
- [ ] Explain readiness report meaning.
- [ ] Explain originality/provenance limitations.
- [ ] Explain that Phase 3A does not perform:
  - [ ] full design token extraction
  - [ ] ecommerce mapping
  - [ ] component generation
  - [ ] StorefrontBuilder blueprint consumption
- [ ] Add Phase 3B handoff:
  - [ ] design-token extraction
  - [ ] semantic token normalization
  - [ ] section segmentation
  - [ ] responsive comparison
  - [ ] component detection
  - [ ] ecommerce region mapping
  - [ ] confidence scoring
  - [ ] human review
  - [ ] StorefrontBuilder consumption of blueprint

Guardrails:

- [ ] Do not claim Phase 3A is a visual generator.
- [ ] Do not claim AI analysis is complete.
- [ ] Do not claim assets are reusable by default.

Exit criteria:

- [ ] Human and AI agent can run the hardening gate from docs.
- [ ] Known limitations are explicit.
- [ ] Phase 3B starts from stable runtime evidence, not from patched prototype behavior.

## Required Test Matrix

| Area | Required proof |
| --- | --- |
| Boundary | No production references to ReverseEngineering tooling. |
| Browser | Real Chromium launch, navigation, viewport, screenshot dimensions. |
| Stabilization | DOM/font/image waits, warm scroll, noise hide. |
| Capture | Desktop/tablet/mobile raw artifacts from same session state. |
| Stitch | Segment screenshots and real stitched image output. |
| Evidence | Real `getComputedStyle`, real `getBoundingClientRect`, rendered asset metadata. |
| Consistency | Capture correlation ID links raw and normalized artifacts. |
| Workflow | Actual CLI run uses `SequentialWorkflowRunner`, retry, resume, force-step. |
| Schema | Per-artifact schemas reject invalid nested fields. |
| Readiness | Quality-aware, capture-plan-driven readiness blocks bad evidence. |
| Interaction | Real before/after browser actions and diffs. |
| Security | Secret/cookie/header redaction and unsafe action refusal. |
| Compatibility | Existing StorefrontBuilder plan-only/create-hardening smoke. |
| DX | Help, inspect, reports, and failure messages are actionable. |

## Full Definition Of Done

Architecture:

- [ ] ReverseEngineering remains development-time tooling only.
- [ ] No production runtime or backend project references it.
- [ ] Artifact writes stay under approved reverse-engineering roots.
- [ ] StorefrontBuilder generation behavior is unchanged.
- [ ] StorefrontBuilder does not consume blueprint until a later approved phase.

Browser runtime:

- [ ] Real Chromium is used in integration tests.
- [ ] Browser session is stateful for a viewport workflow.
- [ ] Stabilization runs against the actual page.
- [ ] Styles, boxes, and assets come from the rendered page.
- [ ] Evidence collection is bounded by policy.

Capture:

- [ ] Native screenshot has a quality gate.
- [ ] Failed native capture triggers real stitched fallback.
- [ ] Stitched fallback creates segment images and final stitched image.
- [ ] Capture method metadata reflects actual output.

Evidence:

- [ ] One capture snapshot per viewport.
- [ ] Raw and normalized artifacts share correlation metadata.
- [ ] Page capture manifest aggregates all configured viewports.
- [ ] Manifest references are validated.

Workflow:

- [ ] CLI `run` uses `SequentialWorkflowRunner`.
- [ ] Each run writes `runs/{runId}.json`.
- [ ] Retry, resume, cancellation, and force-step are tested in actual workflow.
- [ ] Inspect shows latest run and step statuses.

Validation:

- [ ] Every first-class artifact has a schema file.
- [ ] Schema validation checks domain fields, not metadata only.
- [ ] Readiness is generated from capture plan.
- [ ] Readiness validates quality, references, workflow run, blueprint, and originality.
- [ ] Blocking findings return non-zero exit code.

Interaction:

- [ ] Safe browser actions execute for real.
- [ ] Unsafe actions are refused.
- [ ] Before/after evidence is real.
- [ ] DOM/style/screenshot differences are computed, not faked.

Testing:

- [ ] Unit tests pass.
- [ ] Schema tests pass.
- [ ] Real Playwright fixture tests pass.
- [ ] Stable capture and stitch tests pass.
- [ ] Interaction tests pass.
- [ ] Readiness tests pass.
- [ ] Boundary tests pass.
- [ ] StorefrontBuilder compatibility smoke passes.
- [ ] Full hardening gate passes locally without internet.

## Implementation Order

1. H0 - hardening preparation and guardrails.
2. H1 - real Playwright browser session and evidence extraction.
3. H2 - real stabilization and stitched fallback.
4. H3 - single-snapshot consistency.
5. H4 - CLI workflow runner integration.
6. H5 - per-artifact schemas and readiness gate.
7. H6 - real interaction capture and safe action guard.
8. H7 - local HTTP fixture and real Playwright E2E.
9. H8 - CLI semantics, force behavior, and inspect reports.
10. H9 - full Phase 3A hardening gate.
11. H10 - documentation and Phase 3B handoff.

## Autoplan Decision Notes

CEO lens:

- The hardening is required before claiming Phase 3A is complete because current tests pass on prototype behavior.
- Do not broaden into Phase 3B. The goal is trustworthy evidence, not smarter interpretation.
- Preserve current StorefrontBuilder generation workflow until blueprint consumption is explicitly planned.

Engineering lens:

- The first architectural correction is stateful browser session ownership. It unlocks real stabilization, real interactions, and single-snapshot consistency.
- The second correction is schema/readiness depth. Metadata-only validation can hide broken artifacts.
- The third correction is actual workflow runner usage in CLI. A runner tested in isolation is not release evidence.

DX lens:

- The hardening gate must be one command and must not require internet.
- Errors must include problem, cause, and fix.
- `inspect` must be useful enough for a developer or AI agent to resume work without opening every artifact by hand.

Risk register:

| Risk | Impact | Mitigation |
| --- | --- | --- |
| Playwright hardening becomes flaky | Blocks closure | Use deterministic local HTTP fixture and split fast/browser tests. |
| Stitching adds OS-specific dependency | Fails on CI/dev machines | Use cross-platform .NET image package only. |
| Workflow rewrite breaks CLI | Tool becomes unusable | Introduce workflow steps behind existing command names with tests. |
| Schema work becomes too broad | Delays Phase 3A | Limit to first-class artifacts already produced by Phase 3A. |
| Readiness overclaims quality | Bad evidence reaches Phase 3B | Blocking findings for failed quality, missing evidence, and invalid references. |
| Interaction accidentally mutates reference site | Security/product risk | Safe-action allow/deny guard and local fixture-only automation tests. |

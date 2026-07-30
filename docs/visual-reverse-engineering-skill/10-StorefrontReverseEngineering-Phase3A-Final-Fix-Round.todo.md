# Storefront Reverse Engineering Phase 3A Final Fix Round.todo

Status: In progress

Scope: final closure fixes for `tools/BlazorShop.AI.StorefrontReverseEngineering` after Phase 3A Hardening.

This file is intentionally separate from `09-StorefrontReverseEngineering-Phase3A-Hardening.todo.md`. Phase 09 records the implemented hardening history. This plan covers the remaining closure blockers found after reviewing the current codebase.

## Goal

Close Phase 3A with evidence that the reverse-engineering runtime can safely hand off to Phase 3B without Phase 3B having to repair capture, readiness, inspect, or gate foundation.

Target final runtime behavior:

```text
Reference URL
-> one browser session per viewport
-> navigate and stabilize page
-> extract rendered evidence
-> attempt native full-page screenshot
-> automatically fallback to stitched screenshot when native output is unusable
-> persist one correlated viewport capture snapshot
-> validate schema and evidence quality
-> inspect latest run and readiness state from machine-readable reports
-> produce a local closure report tied to the current commit
```

## Current Codebase Evidence

Observed from current source and local tests:

- `StableFullPageCaptureService` already creates real stitched output through viewport segment screenshots and ImageMagick composition.
- `StableFullPageCaptureService` already falls back when native quality has blocking findings after `CaptureCurrentStateAsync` returns.
- `VisualProjectWorkflowService.RunAsync(...)` already uses `SequentialWorkflowRunner`.
- `ReadinessReport` and `reports/readiness-report.json` already exist.
- Readiness already checks `capture-quality-report.json` and blocks when `quality.Passed == false`.
- Schema descriptors already support required properties, required paths, array paths, enum rules, and numeric rules.
- Current test suite passes: `71/71`.

Remaining gaps:

- `CaptureCurrentStateAsync` still extracts rendered evidence and captures the native full-page screenshot in one operation. If the native screenshot throws, rendered evidence can be lost.
- Native screenshot exception only falls back if a partial `nativeCapture` already exists.
- `CapturePolicy` does not own important runtime limits such as evidence counts, segment limits, settle timings, fallback toggle, blank threshold, or noise selectors.
- Playwright evidence limits and stitch limits are still hard-coded inside implementation files.
- `inspect` still derives `ValidationSummary` from `reports/evidence-validation.md` instead of `reports/readiness-report.json`.
- Readiness does not deeply validate empty or low-value evidence when the required files exist.
- Readiness does not fully validate correlation, originality restriction depth, or latest-run step ownership.
- Node bridge messaging is confusing: the factory does not select `NodePlaywrightReferenceBrowser`, but docs still describe it as a manual capture path.
- Gate report is short and not commit-linked enough for final closure evidence.

## Non-Goals

- Do not implement design-token extraction.
- Do not implement semantic token normalization.
- Do not implement section segmentation beyond evidence quality checks.
- Do not implement ecommerce component mapping.
- Do not make StorefrontBuilder consume `visual-blueprint.draft.json`.
- Do not generate Razor, CSS, or storefront projects from reverse-engineering output.
- Do not change Commerce Node, Control Plane, Storefront V2, Starter, Presentation, Runtime, Client, Browser, Components, Domain, Application, or Infrastructure behavior.
- Do not require GitHub Actions for closure while Actions are disabled during development.
- Do not delete the existing StorefrontBuilder Node Playwright script. It remains a separate StorefrontBuilder baseline until a later parity phase.

## Target Architecture

```text
VisualCaptureService
  -> StableFullPageCaptureService
      -> IReferenceBrowser.OpenSessionAsync
      -> IReferenceBrowserSession.NavigateAsync
      -> IReferenceBrowserSession.StabilizeAsync
      -> IReferenceBrowserSession.ExtractRenderedEvidenceAsync
      -> IReferenceBrowserSession.CaptureNativeFullPageScreenshotAsync
          -> CaptureFallbackDecision
              -> native accepted
              -> or CaptureStitchedAsync on the same session
      -> CapturedViewportResult
          -> BrowserCaptureResult
          -> CaptureQualityReport
          -> PageStabilizationReport
          -> correlation ID

VisualEvidenceExtractor
  -> normalized evidence from CapturedViewportResult
  -> element evidence index
  -> asset inventory
  -> page capture manifest

ValidateReadinessStep
  -> schema validation
  -> capture quality validation
  -> evidence depth validation
  -> correlation validation
  -> originality validation
  -> workflow latest-run validation

inspect
  -> project.json
  -> runs/{latestRunId}.json
  -> reports/readiness-report.json
```

## Autoplan Review Summary

Mode: HOLD SCOPE.

CEO lens:

- This is the right problem to solve before Phase 3B. Phase 3B depends on trustworthy evidence, so capture/readiness/inspect gaps must not be deferred into analysis/generation.
- Do not expand into visual intelligence or ecommerce mapping. The user goal is closure of foundation quality, not bigger generator scope.
- Local evidence is acceptable because GitHub Actions are disabled during development.

Engineering lens:

- The most important correction is splitting rendered evidence extraction from native screenshot capture. That keeps fallback possible when native full-page screenshot fails.
- The second correction is readiness depth. File existence plus basic schema is not enough for a visual evidence engine.
- The third correction is DX and gate evidence. `inspect` and the closure report must tell a developer what happened without opening every artifact manually.

DX lens:

- One command must prove closure locally.
- `inspect` must read machine-readable readiness, not legacy Markdown.
- Error messages and gate output should include exact paths and fix commands.

Design lens:

- Skipped. This plan has no product UI or visual design surface. It is CLI/tooling hardening.

## Phase F0 - Scope Lock And Baseline Verification

Goal: prove the starting point and prevent repeating already completed Phase 09 work.

Files to inspect:

- `docs/visual-reverse-engineering-skill/09-StorefrontReverseEngineering-Phase3A-Hardening.todo.md`
- `docs/architecture/11-storefront-builder.md`
- `docs/agents/storefront-builder.md`
- `tools/BlazorShop.AI.StorefrontReverseEngineering/README.md`
- `scripts/qa/run-storefront-reverse-engineering-phase3a-gate.ps1`
- `tools/BlazorShop.AI.StorefrontReverseEngineering/**/*.cs`
- `tools/BlazorShop.AI.StorefrontReverseEngineering/tests/**/*.cs`

Tasks:

- [x] Confirm Phase 09 remains historical implementation evidence and is not rewritten as the new plan.
- [x] Confirm this final round stays under:
  - [x] `tools/BlazorShop.AI.StorefrontReverseEngineering`
  - [x] `tools/BlazorShop.AI.StorefrontReverseEngineering/tests`
  - [x] `scripts/qa/run-storefront-reverse-engineering-phase3a-gate.ps1`
  - [x] `docs/visual-reverse-engineering-skill`
  - [x] `docs/architecture/11-storefront-builder.md`
  - [x] `docs/agents/storefront-builder.md`
- [x] Run the current focused tests before edits.
- [x] Run the current production boundary scan before edits.
- [x] Record known current gaps in implementation evidence after F0 is complete.
- [x] Confirm GitHub Actions closure evidence is explicitly out of scope while Actions are disabled.

Verification:

```powershell
dotnet test tools\BlazorShop.AI.StorefrontReverseEngineering\tests\BlazorShop.AI.StorefrontReverseEngineering.Tests\BlazorShop.AI.StorefrontReverseEngineering.Tests.csproj --no-restore

rg -n "BlazorShop.AI.StorefrontReverseEngineering|StorefrontReverseEngineering" BlazorShop.PresentationV2 BlazorShop.Domain BlazorShop.Application BlazorShop.Infrastructure BlazorShop.ServiceDefaults BlazorShop.Tests.V2 BlazorShop.sln --glob "!bin/**" --glob "!obj/**"
```

Exit criteria:

- [x] Current test count/result is recorded.
- [x] Boundary scan remains clean.
- [x] No Phase 3B or production runtime scope is added.

Implementation evidence:

- Phase 09 remains complete historical evidence in `09-StorefrontReverseEngineering-Phase3A-Hardening.todo.md`; this file is the active final fix round.
- Baseline verification before F1 edits: `dotnet test tools\BlazorShop.AI.StorefrontReverseEngineering\tests\BlazorShop.AI.StorefrontReverseEngineering.Tests\BlazorShop.AI.StorefrontReverseEngineering.Tests.csproj --no-restore` passed `71/71` in about 16 seconds.
- Production boundary scan before F1 edits: `rg -n "BlazorShop.AI.StorefrontReverseEngineering|StorefrontReverseEngineering" BlazorShop.PresentationV2 BlazorShop.Domain BlazorShop.Application BlazorShop.Infrastructure BlazorShop.ServiceDefaults BlazorShop.Tests.V2 BlazorShop.sln --glob "!bin/**" --glob "!obj/**"` returned no matches.
- Current gap scan confirmed the remaining blockers named in this plan: native full-page screenshot is still coupled to `CaptureCurrentStateAsync`, fallback relies on partial native capture/forced fallback paths, evidence and stitch limits are hard-coded, `VisualProjectService.InspectAsync` still looks at `reports/evidence-validation.md`, and docs/messages still mention `NodePlaywrightReferenceBrowser`.
- GitHub Actions closure remains out of scope for this round because local gate evidence is the approved development-phase closure proof.

## Phase F1 - Split Rendered Evidence From Native Full-Page Screenshot

Goal: native screenshot failure must not destroy rendered evidence needed for stitched fallback.

Current files:

- `Browser/ReferenceBrowserContracts.cs`
- `Browser/PlaywrightReferenceBrowser.cs`
- `Browser/FixtureReferenceBrowser.cs`
- `Browser/SyntheticReferenceBrowser.cs`
- `Browser/StableFullPageCaptureService.cs`
- `Browser/StableCaptureContracts.cs`
- `Application/VisualCaptureService.cs`
- `tests/.../StableCaptureQualityTests.cs`
- `tests/.../PlaywrightIntegrationTests.cs`

Tasks:

- [x] Introduce a rendered evidence model that can carry:
  - [x] document width
  - [x] document height
  - [x] DOM HTML
  - [x] computed styles
  - [x] bounding boxes
  - [x] rendered asset inventory
  - [x] warnings
- [x] Split `IReferenceBrowserSession.CaptureCurrentStateAsync(...)` into narrower operations:
  - [x] `ExtractRenderedEvidenceAsync(...)`
  - [x] `CaptureNativeFullPageScreenshotAsync(...)`
  - [x] existing `CaptureViewportScreenshotAsync(...)`
- [x] Keep compatibility method behavior only where useful, but ensure the stable capture path uses the split operations.
- [x] Update `PlaywrightReferenceBrowserSession` so evidence extraction runs before native full-page screenshot.
- [x] Update fixture/synthetic browser sessions to implement the split operations deterministically for tests.
- [x] Update `StableFullPageCaptureService` flow:
  - [x] navigate
  - [x] stabilize
  - [x] extract evidence
  - [x] try native full-page screenshot
  - [x] evaluate native output
  - [x] fallback to stitched capture on failure/low quality
  - [x] construct final `BrowserCaptureResult` from the already extracted evidence
- [x] Ensure browser session stays open after native screenshot failure so stitched capture can reuse it.
- [x] Ensure session is opened once per viewport capture.
- [x] Ensure cancellation still propagates and does not become a synthetic failed capture.

Guardrails:

- [x] Do not create a second Chromium page for fallback.
- [x] Do not re-extract evidence after fallback unless a test proves it is necessary.
- [x] Do not swallow caller cancellation.
- [x] Do not change StorefrontBuilder generation behavior.

Tests:

- [x] Native full-page screenshot exception triggers stitched fallback.
- [x] Rendered evidence from before the exception is present in the final capture.
- [x] Browser session open count is exactly one for native plus fallback.
- [x] Native success path still returns `native-full-page`.
- [x] Cancellation token cancellation is thrown, not converted to quality failure.

Verification:

```powershell
dotnet test tools\BlazorShop.AI.StorefrontReverseEngineering\tests\BlazorShop.AI.StorefrontReverseEngineering.Tests\BlazorShop.AI.StorefrontReverseEngineering.Tests.csproj --filter "StableCapture|Playwright|Browser"
```

Exit criteria:

- [x] Screenshot exception no longer loses DOM/style/box/asset evidence.
- [x] Stitched fallback can run from the same session after native failure.
- [x] Existing Playwright full workflow still passes.

Implementation evidence:

- Added `RenderedPageEvidence` and split browser session operations into `ExtractRenderedEvidenceAsync`, `CaptureNativeFullPageScreenshotAsync`, and `CaptureViewportScreenshotAsync` while retaining `CaptureCurrentStateAsync` as compatibility facade.
- Updated Playwright and fixture sessions so evidence extraction is independent from native full-page screenshot bytes.
- Updated `StableFullPageCaptureService` to navigate, stabilize, extract evidence, then attempt native screenshot; native screenshot exceptions now fall back to stitched capture using the same browser session and already extracted evidence.
- Added regression tests proving native screenshot exception fallback preserves rendered evidence, opens exactly one session, native success does not fallback, and cancellation is propagated.
- Verification: `dotnet test tools\BlazorShop.AI.StorefrontReverseEngineering\tests\BlazorShop.AI.StorefrontReverseEngineering.Tests\BlazorShop.AI.StorefrontReverseEngineering.Tests.csproj --filter "StableCapture|Playwright|Browser"` passed `15/15`.

## Phase F2 - Capture Fallback Decision And Screenshot Quality Depth

Goal: fallback must be explicit, automatic, testable, and persisted with reason codes.

Current files:

- `Browser/StableFullPageCaptureService.cs`
- `Browser/StableCaptureContracts.cs`
- `Contracts/CoreContracts.cs`
- `tests/.../StableCaptureQualityTests.cs`
- `tests/.../PlaywrightIntegrationTests.cs`

Tasks:

- [x] Add `CaptureFallbackDecision`.
- [x] Make fallback decision derive from `CaptureQualityFinding` codes and policy.
- [x] Persist fallback decision data in `CaptureQualityReport`:
  - [x] `NativeAttemptPassed`
  - [x] `FallbackReason`
  - [x] triggering finding codes
  - [x] final method
  - [x] final dimensions
  - [x] segment count
- [x] Treat these findings as automatic fallback triggers:
  - [x] `missing-screenshot-file`
  - [x] `png-decode-failed`
  - [x] `unexpected-image-width`
  - [x] `unexpected-image-height`
  - [x] `native-capture-exception`
  - [x] `blank-image`
  - [x] `document-height-mismatch`
  - [x] `missing-lower-page-content`
- [x] Convert `suspicious-single-color-image` from warning to fallback trigger when policy threshold is exceeded.
- [x] Improve blank and low-content detection:
  - [x] decode PNG through ImageMagick
  - [x] compute dominant color ratio
  - [x] compute simple histogram or entropy score
  - [x] detect large blank bands
  - [x] compare native image dimensions to expected document dimensions
  - [x] check lower-page evidence anchors when document height exceeds viewport height
- [x] Add `EnableAutomaticStitchedFallback` policy switch.
- [x] If automatic fallback is disabled, native blocking findings must make final quality fail instead of silently stitching.
- [x] Remove reliance on `forceStitchedFallback` for production path behavior.
- [x] Keep `forceStitchedFallback` only if tests still need an explicit manual proof hook; otherwise replace tests with automatic scenarios.

Guardrails:

- [x] Do not introduce heavy computer vision.
- [x] Do not attempt pixel-perfect visual validation.
- [x] Do not mark a result `stitched` unless stitched PNG and segment manifest exist.
- [x] Do not pass readiness when native and stitched both fail.

Tests:

- [x] Empty screenshot bytes trigger fallback if possible.
- [x] Undecodable PNG triggers fallback if possible.
- [x] Valid blank PNG triggers fallback.
- [x] Unexpected native image width triggers fallback.
- [x] Unexpected native image height triggers fallback.
- [x] Native screenshot exception triggers fallback.
- [x] Native valid full-page image does not fallback.
- [x] Stitched fallback failure results in `capture-failed` quality.
- [x] Fallback disabled returns native quality failure.
- [x] Fallback reason and triggering finding codes are persisted.

Verification:

```powershell
dotnet test tools\BlazorShop.AI.StorefrontReverseEngineering\tests\BlazorShop.AI.StorefrontReverseEngineering.Tests\BlazorShop.AI.StorefrontReverseEngineering.Tests.csproj --filter "StableCapture|Stitch|Quality"
```

Exit criteria:

- [x] Fallback decision is visible and deterministic.
- [x] Native screenshot failure modes are covered by tests.
- [x] Final quality report reflects the actual final screenshot artifact.

Implementation evidence:

- Added `CaptureFallbackDecision` and persisted it on `CaptureQualityReport`; reports retain native attempt status, fallback reason, triggering finding codes, final method, final dimensions, and segment count.
- Added automatic fallback decision logic keyed by `CaptureQualityFinding` codes plus `CapturePolicy.EnableAutomaticStitchedFallback`.
- Replaced byte-diversity blank detection with ImageMagick PNG decode, dimensions, dominant-color ratio, lower-band ratio, document-height mismatch, and lower-page content checks.
- Retained `forceStitchedFallback` only as an explicit proof hook; production quality failures now use automatic fallback when policy allows.
- Added tests for blank PNG, undecodable PNG, unexpected width, native screenshot exception, valid native no-fallback, stitch failure, fallback disabled, and persisted reason/codes.
- Verification: `dotnet test tools\BlazorShop.AI.StorefrontReverseEngineering\tests\BlazorShop.AI.StorefrontReverseEngineering.Tests\BlazorShop.AI.StorefrontReverseEngineering.Tests.csproj --filter "StableCapture|Stitch|Quality"` passed `13/13`.

## Phase F3 - Move Capture And Evidence Limits Into CapturePolicy

Goal: runtime limits must be configurable through `configuration.json`, not hidden inside implementation constants.

Current files:

- `Contracts/CoreContracts.cs`
- `Schemas/configuration.schema.json`
- `Browser/PlaywrightReferenceBrowser.cs`
- `Browser/StableFullPageCaptureService.cs`
- `Evidence/VisualEvidenceExtractor.cs`
- `Application/VisualProjectService.cs`
- `tests/.../SchemaArtifactTests.cs`
- `tests/.../BrowserCaptureTests.cs`
- `tests/.../EvidenceExtractionTests.cs`

Tasks:

- [x] Extend `CapturePolicy` with:
  - [x] `MaximumEvidenceElements`
  - [x] `MaximumEvidenceAssets`
  - [x] `MaximumTextLength`
  - [x] `MaximumSegmentCount`
  - [x] `SegmentOverlapPixels`
  - [x] `ScrollSettleMilliseconds`
  - [x] `FinalSettleMilliseconds`
  - [x] `EnableAutomaticStitchedFallback`
  - [x] `MaximumSingleColorRatio`
  - [x] `NoiseSelectors`
- [x] Keep default values compatible with current behavior:
  - [x] evidence elements default `80`
  - [x] evidence assets default `80`
  - [x] text length default `160`
  - [x] segment count default `50`
  - [x] overlap default `80`
  - [x] scroll settle default `100`
  - [x] final settle default `150`
  - [x] automatic fallback default `true`
  - [x] single-color ratio default around `0.98`
- [x] Replace Playwright hard-coded evidence limits with policy values.
- [x] Replace stitch hard-coded segment count and overlap with policy values.
- [x] Replace stabilization hard-coded settle delays with policy values.
- [x] Replace hard-coded noise selector list with policy defaults.
- [x] Decide where default noise selectors live:
  - [x] recommended: helper on `CapturePolicy` or internal default resolver
  - [x] avoid duplicating defaults in Playwright and tests
- [x] Update configuration schema numeric validation:
  - [x] positive timeout
  - [x] positive maximum page height
  - [x] positive evidence limits
  - [x] positive segment count
  - [x] overlap less than viewport height at runtime
  - [x] single-color ratio between `0` and `1`
- [x] Add validation error messages with problem/cause/fix style.

Guardrails:

- [x] Do not make existing generated `configuration.json` invalid unless a migration/default path exists.
- [x] Do not require users to set every option manually.
- [x] Do not expose policy options unrelated to Phase 3A.

Tests:

- [x] Custom `MaximumEvidenceElements` limits rendered evidence.
- [x] Custom `MaximumEvidenceAssets` limits assets.
- [x] Custom `MaximumTextLength` truncates text snippets.
- [x] Custom noise selector is hidden during stabilization.
- [x] Custom settle timings are honored in deterministic fake/session tests.
- [x] Custom `MaximumSegmentCount` blocks overly tall stitching.
- [x] Invalid negative or zero limits fail schema/domain validation.
- [x] Invalid single-color ratio fails validation.
- [x] Automatic fallback can be disabled by policy.

Verification:

```powershell
dotnet test tools\BlazorShop.AI.StorefrontReverseEngineering\tests\BlazorShop.AI.StorefrontReverseEngineering.Tests\BlazorShop.AI.StorefrontReverseEngineering.Tests.csproj --filter "Schema|Policy|Browser|Evidence|StableCapture"
```

Exit criteria:

- [x] Important runtime limits are not hard-coded in browser/stitch implementation.
- [x] Defaults preserve current behavior.
- [x] Invalid policy is caught before confusing runtime failures.

Implementation evidence:

- Extended `CapturePolicy` with evidence limits, stitch limits, settle timings, automatic fallback, blank threshold, and optional noise selectors.
- Added `CapturePolicyDefaults` for default noise selector resolution and problem/cause/fix validation.
- Replaced Playwright evidence/image/noise/settle hard-codes and stitch segment/overlap hard-codes with policy values.
- Updated `configuration.schema.json` numeric rules for the policy surface, including single-color ratio `0..1`; overlap is checked at runtime against viewport height.
- Added tests for custom evidence/assets/text limits, custom noise selectors, custom settle timing, custom segment limits, invalid schema policy values, and fallback-disable behavior.
- Verification: `dotnet test tools\BlazorShop.AI.StorefrontReverseEngineering\tests\BlazorShop.AI.StorefrontReverseEngineering.Tests\BlazorShop.AI.StorefrontReverseEngineering.Tests.csproj --filter "Schema|Policy|Browser|Evidence|StableCapture"` passed `39/39`.

## Phase F4 - Harden Readiness By Evidence Quality

Goal: readiness must fail when evidence files exist but do not contain usable evidence.

Current files:

- `Application/VisualProjectWorkflowService.cs`
- `Validation/ReadinessReport.cs`
- `Validation/VisualSchemaDefinition.cs`
- `Validation/VisualSchemaValidator.cs`
- `Validation/VisualSchemaRegistry.cs`
- `Schemas/*.schema.json`
- `Evidence/EvidenceContracts.cs`
- `Provenance/OriginalityContracts.cs`
- `tests/.../EndToEndCliTests.cs`
- `tests/.../SchemaArtifactTests.cs`

Tasks:

- [x] Add evidence readiness validator functions or a focused internal service. Keep it internal unless multiple consumers appear.
- [x] Validate computed-style evidence for every page/viewport in the capture plan:
  - [x] `elements` has minimum length
  - [x] at least one semantic landmark or section candidate
  - [x] at least one element has non-empty style groups
  - [x] typography evidence exists
  - [x] layout evidence exists
  - [x] style values are not all blank
- [x] Validate bounding boxes:
  - [x] at least one useful box
  - [x] width and height greater than zero
  - [x] box fits reasonable document bounds
  - [x] at least one major element has a useful box
- [x] Validate capture correlation:
  - [x] viewport manifest has `captureCorrelationId`
  - [x] element evidence has same correlation ID
  - [x] asset inventory has same correlation ID
  - [x] page manifest records the same viewport/correlation pair
  - [x] run ID is present for workflow-generated captures
- [x] Validate screenshot quality artifacts:
  - [x] `quality.Passed == true`
  - [x] final image dimensions are present
  - [x] screenshot file decodes
  - [x] method and artifact shape match
  - [x] stitched method requires segment count greater than zero
  - [x] stitched method requires `stitch-manifest.json`
  - [x] fallback reason is present when fallback was used
- [x] Validate originality/provenance:
  - [x] `generationRestrictions` minimum length is greater than zero
  - [x] external/source assets remain reference-only by default
  - [x] likely brand assets produce review warning when policy is enabled
  - [x] audit has project and page provenance
- [x] Validate workflow:
  - [x] latest run exists
  - [x] latest run status is `Succeeded`
  - [x] required steps are `Succeeded` or valid `Skipped`
  - [x] no required step is `Pending`, `Running`, `Failed`, or `Canceled`
  - [x] readiness report belongs to latest run or records enough context to prove it was generated after latest run
- [x] Add schema descriptor support:
  - [x] minimum array length rules
  - [x] required non-empty string paths
  - [x] nested array item required fields for key artifacts
  - [x] correlation field requirements
  - [x] `generationRestrictions` minimum length
  - [x] `elements` minimum length
- [x] Keep schema validation deterministic and local. Do not switch to a full JSON Schema implementation unless the custom descriptor becomes harder to maintain than using a library.

Suggested blocking finding codes:

```text
empty-computed-style-evidence
empty-style-groups
missing-layout-evidence
missing-typography-evidence
missing-useful-bounding-box
invalid-element-box
missing-capture-correlation
capture-correlation-mismatch
empty-generation-restrictions
missing-reference-only-policy
missing-originality-provenance
invalid-stitch-artifact
missing-stitch-manifest
failed-latest-run
partial-latest-run
stale-readiness-run
```

Guardrails:

- [x] Do not require full design-token extraction to pass readiness.
- [x] Do not require specific ecommerce semantics to pass readiness.
- [x] Do not require external website access.
- [x] Do not make fixture-only evidence rules that fail realistic storefronts without cause.

Tests:

- [x] Empty element evidence array fails readiness.
- [x] Elements with empty style groups fail readiness.
- [x] Missing typography evidence fails readiness.
- [x] Missing layout evidence fails readiness.
- [x] Missing useful boxes fail readiness.
- [x] Invalid box dimensions fail readiness.
- [x] Missing correlation ID fails readiness.
- [x] Correlation mismatch fails readiness.
- [x] Empty generation restrictions fail readiness.
- [x] Stitched method without stitch manifest fails readiness.
- [x] Latest run failed fails readiness.
- [x] Latest run partial/pending fails readiness.
- [x] Fixing artifacts and re-running validate returns project to `DraftReady`.
- [x] Local HTTP fixture full workflow still passes readiness.

Verification:

```powershell
dotnet test tools\BlazorShop.AI.StorefrontReverseEngineering\tests\BlazorShop.AI.StorefrontReverseEngineering.Tests\BlazorShop.AI.StorefrontReverseEngineering.Tests.csproj --filter "Readiness|Validation|Schema|Workflow|EndToEnd"
```

Exit criteria:

- [x] Bad or empty evidence cannot pass readiness.
- [x] Missing correlation cannot pass readiness.
- [x] Empty originality restrictions cannot pass readiness.
- [x] Failed or partial workflow cannot pass readiness.
- [x] Full fixture workflow still passes.

Implementation evidence:

- Added internal readiness validation for viewport evidence depth, style-group usefulness, typography/layout presence, useful/valid boxes, correlation, screenshot quality shape, stitch manifest requirements, originality restrictions/provenance, and latest workflow status/steps.
- Added schema descriptor support for minimum array length and non-empty string paths, then applied it to computed-style evidence, capture manifests, asset inventory, and originality audit.
- Allowed the active `validate-readiness` step to validate the currently running workflow without self-blocking, while still blocking failed/partial runs outside that current-step state.
- Added mutation tests for empty elements, empty style groups, missing typography/layout, invalid boxes, missing/mismatched correlation, empty originality restrictions, stitched-without-manifest, failed run, partial run, recovery to `DraftReady`, and full local fixture readiness.
- Verification: `dotnet test tools\BlazorShop.AI.StorefrontReverseEngineering\tests\BlazorShop.AI.StorefrontReverseEngineering.Tests\BlazorShop.AI.StorefrontReverseEngineering.Tests.csproj --filter "Readiness|Validation|Schema|Workflow|EndToEnd"` passed `32/32`.

## Phase F5 - Fix Inspect And CLI Developer Feedback

Goal: `inspect` must show the actual health of the project from machine-readable artifacts.

Current files:

- `Application/VisualProjectService.cs`
- `Cli/CliHost.cs`
- `Validation/ReadinessReport.cs`
- `Workflows/WorkflowContracts.cs`
- `tests/.../VisualProjectLifecycleTests.cs`
- `tests/.../EndToEndCliTests.cs`
- `tools/BlazorShop.AI.StorefrontReverseEngineering/README.md`
- `docs/visual-reverse-engineering-skill/reference.md`

Tasks:

- [ ] Replace `reports/evidence-validation.md` inspection logic with `reports/readiness-report.json`.
- [ ] Expand `VisualProjectInspection` to include:
  - [ ] latest run ID
  - [ ] latest run status
  - [ ] readiness passed
  - [ ] blocking finding count
  - [ ] warning count
  - [ ] latest blocking finding
  - [ ] blueprint path
  - [ ] readiness report path
  - [ ] artifact root
- [ ] Make `inspect` handle each state clearly:
  - [ ] initialized but never run
  - [ ] run exists but readiness missing
  - [ ] readiness passed
  - [ ] readiness failed
  - [ ] latest run file missing
  - [ ] latest run file invalid
- [ ] CLI output should include:
  - [ ] project
  - [ ] name
  - [ ] status
  - [ ] source URL
  - [ ] artifact root
  - [ ] latest run
  - [ ] latest run status
  - [ ] readiness passed
  - [ ] blocking findings
  - [ ] warnings
  - [ ] latest blocking finding
  - [ ] blueprint path
  - [ ] readiness report path
  - [ ] step table when run exists
- [ ] Keep output script-friendly enough for gate assertions.
- [ ] Update README and reference docs to tell developers to use `readiness-report.json` as source of truth.

Guardrails:

- [ ] Do not rely on Markdown reports as source of truth.
- [ ] Do not hide failed readiness behind generic validation text.
- [ ] Do not make `inspect` require Playwright or browser dependencies.

Tests:

- [ ] Inspect before any run shows no latest run and no readiness.
- [ ] Inspect after successful run shows readiness passed and zero blocking findings.
- [ ] Inspect after failed readiness shows readiness failed and blocking count.
- [ ] Inspect shows latest blocking finding.
- [ ] Inspect handles missing run file.
- [ ] Inspect handles invalid readiness JSON with clear error or degraded summary.
- [ ] Existing lifecycle inspect tests are updated away from `evidence-validation.md`.

Verification:

```powershell
dotnet test tools\BlazorShop.AI.StorefrontReverseEngineering\tests\BlazorShop.AI.StorefrontReverseEngineering.Tests\BlazorShop.AI.StorefrontReverseEngineering.Tests.csproj --filter "Cli|Lifecycle|Inspect|Readiness"

dotnet run --project tools\BlazorShop.AI.StorefrontReverseEngineering\BlazorShop.AI.StorefrontReverseEngineering.csproj -- inspect --project obj/storefront-reverse-engineering/projects/<project-id>
```

Exit criteria:

- [ ] `inspect` reflects the same readiness state as `reports/readiness-report.json`.
- [ ] Developers can identify the next action without manually opening artifact folders.
- [ ] No code path references `reports/evidence-validation.md` as active inspection source.

## Phase F6 - Node Bridge Cleanup Without Breaking StorefrontBuilder Baseline

Goal: remove confusion around `NodePlaywrightReferenceBrowser` while preserving the separate StorefrontBuilder Node capture baseline.

Current files:

- `Browser/NodePlaywrightReferenceBrowser.cs`
- `Browser/ReferenceBrowserFactory.cs`
- `Browser/FixtureReferenceBrowser.cs`
- `tools/BlazorShop.AI.StorefrontReverseEngineering/README.md`
- `docs/architecture/11-storefront-builder.md`
- `docs/agents/storefront-builder.md`
- `docs/visual-reverse-engineering-skill/reference.md`
- `scripts/qa/run-storefront-reverse-engineering-phase3a-gate.ps1`

Decision:

- Do not delete or retire the existing StorefrontBuilder Node Playwright script in this phase.
- Treat `NodePlaywrightReferenceBrowser` wrapper as deferred/legacy unless it has active consumers that require it.
- The active ReverseEngineering runtime path remains `.NET PlaywrightReferenceBrowser` plus local fixtures.

Tasks:

- [ ] Find all consumers of `NodePlaywrightReferenceBrowser`.
- [ ] If there are no active consumers:
  - [ ] either remove `NodePlaywrightReferenceBrowser.cs`
  - [ ] or keep it internal with an explicit deferred marker and tests proving it is not factory-selected
- [ ] If there are active consumers:
  - [ ] migrate them to `PlaywrightReferenceBrowser` or a clear interface
  - [ ] remove `NotSupportedException` from any recommended runtime path
- [ ] Ensure `ReferenceBrowserFactory` never returns the Node bridge for active capture.
- [ ] Update `FixtureReferenceBrowser` error message so it does not suggest Node bridge as the fix for non-fixture URLs.
- [ ] Update docs:
  - [ ] StorefrontBuilder Node script remains baseline for StorefrontBuilder only
  - [ ] ReverseEngineering runtime uses .NET Playwright path
  - [ ] Node bridge wrapper is deferred or removed
  - [ ] README no longer recommends manual Node bridge runs for Phase 3A
- [ ] Update prototype marker scan so it does not falsely fail on documented historical plan text, but still fails active source blockers.

Guardrails:

- [ ] Do not delete StorefrontBuilder Node capture scripts.
- [ ] Do not change StorefrontBuilder generation or QA behavior.
- [ ] Do not leave docs claiming the unsupported wrapper is an active supported path.

Tests:

- [ ] Factory does not select Node bridge for HTTP URLs.
- [ ] Factory does not select Node bridge for `.test` URLs.
- [ ] Factory does not select Node bridge for `file://` URLs.
- [ ] Active source scan does not find `NotSupportedException` in a recommended browser adapter path.
- [ ] README/reference docs no longer recommend `NodePlaywrightReferenceBrowser` for normal use.

Verification:

```powershell
rg -n "NodePlaywrightReferenceBrowser|NotSupportedException|Manual capture can also wrap" tools/BlazorShop.AI.StorefrontReverseEngineering docs/architecture/11-storefront-builder.md docs/agents/storefront-builder.md docs/visual-reverse-engineering-skill --glob "!bin/**" --glob "!obj/**"

dotnet test tools\BlazorShop.AI.StorefrontReverseEngineering\tests\BlazorShop.AI.StorefrontReverseEngineering.Tests\BlazorShop.AI.StorefrontReverseEngineering.Tests.csproj --filter "Browser|Boundary|Cli"
```

Exit criteria:

- [ ] Active runtime cannot accidentally select the Node bridge.
- [ ] Docs distinguish StorefrontBuilder Node baseline from ReverseEngineering runtime.
- [ ] No misleading unsupported browser path remains.

## Phase F7 - Closure Gate And Commit-Linked Local Evidence

Goal: one local command must prove Phase 3A Final Fix Round closure without GitHub Actions.

Current files:

- `scripts/qa/run-storefront-reverse-engineering-phase3a-gate.ps1`
- `docs/qa` or `docs/visual-reverse-engineering-skill`
- `tools/BlazorShop.AI.StorefrontReverseEngineering/README.md`
- `docs/visual-reverse-engineering-skill/reference.md`
- `docs/architecture/11-storefront-builder.md`
- `docs/agents/storefront-builder.md`

Tasks:

- [ ] Update Phase 3A gate to include final fix tests:
  - [ ] automatic fallback from native screenshot exception
  - [ ] automatic fallback from blank valid PNG
  - [ ] no fallback for valid native full-page image
  - [ ] stitched fallback failure blocks readiness
  - [ ] readiness rejects empty evidence
  - [ ] readiness rejects empty style groups
  - [ ] readiness rejects missing useful boxes
  - [ ] readiness rejects missing/mismatched correlation
  - [ ] readiness rejects empty originality restrictions
  - [ ] inspect reports passed readiness
  - [ ] inspect reports failed readiness
  - [ ] custom capture policy is honored
  - [ ] Node bridge is not selected by factory
- [ ] Update gate report metadata:
  - [ ] status
  - [ ] commit SHA
  - [ ] branch
  - [ ] UTC timestamp
  - [ ] .NET version
  - [ ] Playwright/Chromium installed state
  - [ ] OS
  - [ ] executed commands
  - [ ] passed steps
  - [ ] failed step, if any
  - [ ] artifact project root
  - [ ] workflow run ID
  - [ ] readiness report path
  - [ ] test counts when available
- [ ] Keep full report under `obj/storefront-reverse-engineering/reports`.
- [ ] Add committed closure summary when the gate passes:
  - [ ] recommended file: `docs/qa/phase3a-final-fix-closure.md`
  - [ ] include commit SHA
  - [ ] include gate command
  - [ ] include gate result
  - [ ] include test count
  - [ ] include known limitations
  - [ ] include closure decision
- [ ] Ensure closure summary does not commit screenshots, generated artifacts, or large `obj` output.
- [ ] Update docs to make local gate the active closure proof while GitHub Actions are disabled.

Full closure command:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\qa\run-storefront-reverse-engineering-phase3a-gate.ps1
```

Gate must prove:

```text
Build
-> unit/schema tests
-> automatic fallback tests
-> real Playwright tests
-> full workflow
-> readiness validation
-> inspect validation
-> boundary scan
-> active-source prototype marker scan
-> StorefrontBuilder compatibility smoke
-> commit-linked local report
```

Guardrails:

- [ ] Do not depend on external websites.
- [ ] Do not require GitHub Actions.
- [ ] Do not leave fixture servers running.
- [ ] Do not mutate generated storefront source.
- [ ] Do not commit `obj` artifacts.

Exit criteria:

- [ ] Gate passes locally.
- [ ] Gate report includes commit SHA and run ID.
- [ ] Closure summary is committed as documentation evidence.
- [ ] Phase 3A can be marked complete without requiring Phase 3B fixes.

## Phase F8 - Documentation Reconciliation And Phase 3B Handoff

Goal: docs must describe the actual final state and stop future agents from reopening solved Phase 3A foundation work.

Docs to update:

- `tools/BlazorShop.AI.StorefrontReverseEngineering/README.md`
- `docs/visual-reverse-engineering-skill/README.md`
- `docs/visual-reverse-engineering-skill/reference.md`
- `docs/architecture/11-storefront-builder.md`
- `docs/agents/storefront-builder.md`
- `docs/visual-reverse-engineering-skill/09-StorefrontReverseEngineering-Phase3A-Hardening.todo.md`
- this file

Tasks:

- [ ] Document final capture flow:
  - [ ] evidence extraction before native screenshot
  - [ ] automatic fallback decision
  - [ ] stitched fallback requirements
  - [ ] quality report meanings
- [ ] Document final readiness semantics:
  - [ ] file existence
  - [ ] schema
  - [ ] image quality
  - [ ] evidence depth
  - [ ] correlation
  - [ ] originality
  - [ ] latest workflow run
- [ ] Document `inspect` output and how developers should use it.
- [ ] Document configurable `CapturePolicy` fields and defaults.
- [ ] Document Node bridge status accurately.
- [ ] Document local closure gate and closure summary.
- [ ] Add a short Phase 3B handoff note:
  - [ ] Phase 3B may consume trustworthy evidence
  - [ ] Phase 3B must not repair capture fallback
  - [ ] Phase 3B must not repair readiness depth
  - [ ] Phase 3B must not repair inspect state
  - [ ] Phase 3B can focus on design-token extraction, semantic normalization, section segmentation, component detection, ecommerce mapping, confidence scoring, and human review

Guardrails:

- [ ] Do not claim Phase 3A generates storefronts.
- [ ] Do not claim AI visual analysis is complete.
- [ ] Do not claim source assets are reusable by default.
- [ ] Do not rewrite historical implementation evidence except to add a factual closure note.

Exit criteria:

- [ ] Human and AI agents can understand the final Phase 3A boundary from docs.
- [ ] Future work can start Phase 3B without guessing what Phase 3A already guarantees.

## Required Test Matrix

| Area | Required proof |
| --- | --- |
| Boundary | Production projects do not reference ReverseEngineering tooling. |
| Browser session | One browser session can extract evidence, fail native screenshot, and still stitch. |
| Native success | Valid native full-page screenshot does not fallback. |
| Native exception | Native screenshot exception automatically stitches from same session. |
| Blank image | Technically valid blank PNG triggers fallback or quality failure. |
| Stitch failure | Failed stitch produces blocking final quality. |
| Policy | Evidence, stitch, settle, noise, fallback, and blank thresholds are configurable. |
| Schema | Invalid limits, empty arrays, empty strings, and missing nested fields are rejected. |
| Readiness evidence | Empty elements/style groups/boxes cannot pass. |
| Readiness correlation | Missing or mismatched correlation cannot pass. |
| Readiness originality | Empty generation restrictions cannot pass. |
| Readiness workflow | Latest failed/partial run cannot pass. |
| Inspect | Passed and failed readiness states are shown from JSON source of truth. |
| Node bridge | Active factory path cannot select unsupported Node wrapper. |
| Playwright | Local HTTP fixture full workflow still passes. |
| Gate | Local gate writes commit-linked report. |

## Full Definition Of Done

Capture:

- [ ] Rendered evidence extraction is separated from native full-page screenshot.
- [ ] Native screenshot exception can still fallback using same session.
- [ ] Fallback decision is explicit and persisted.
- [ ] Blank/low-content PNG is detected beyond byte diversity.
- [ ] Native valid output does not fallback.
- [ ] Stitched output is real and required when method is `stitched`.

Evidence:

- [ ] Raw and normalized evidence remain correlated.
- [ ] Evidence contains useful styles, boxes, and assets.
- [ ] Empty evidence cannot pass readiness.

Policy:

- [ ] Capture/evidence/stitch limits are configurable.
- [ ] Defaults preserve existing behavior.
- [ ] Invalid policy values fail clearly.

Readiness:

- [ ] Readiness validates schema, quality, evidence depth, correlation, originality, and latest workflow run.
- [ ] Readiness report is machine-readable source of truth.
- [ ] Project can recover from `ValidationFailed` to `DraftReady` after fixing artifacts.

CLI/DX:

- [ ] `inspect` reads `readiness-report.json`.
- [ ] `inspect` shows run status, readiness status, counts, and latest blocker.
- [ ] Docs and errors point to exact files and next commands.

Node bridge:

- [ ] ReverseEngineering runtime does not recommend or select the unsupported Node wrapper.
- [ ] StorefrontBuilder Node baseline remains preserved for its own later parity work.

Proof:

- [ ] `dotnet test` passes.
- [ ] Phase 3A gate passes locally.
- [ ] Gate report includes commit SHA, branch, timestamp, runtime versions, artifact root, run ID, readiness path, and steps.
- [ ] Closure summary is tracked outside `obj`.
- [ ] GitHub Actions are not required for this dev-phase closure.

## Implementation Order

1. F0 - Scope lock and baseline verification.
2. F1 - Split rendered evidence from native full-page screenshot.
3. F2 - Capture fallback decision and screenshot quality depth.
4. F3 - Move capture and evidence limits into `CapturePolicy`.
5. F4 - Harden readiness by evidence quality.
6. F5 - Fix inspect and CLI developer feedback.
7. F6 - Node bridge cleanup without breaking StorefrontBuilder baseline.
8. F7 - Closure gate and commit-linked local evidence.
9. F8 - Documentation reconciliation and Phase 3B handoff.

## Risk Register

| Risk | Impact | Mitigation |
| --- | --- | --- |
| Splitting browser session operations creates broad interface churn | Tests and adapters break | Update `IReferenceBrowserSession` first, then adapters, then stable capture service. Keep old facade only if useful. |
| Fallback logic becomes too clever | False fallback hides native issues | Persist triggering finding codes and native attempt status. Add native-success no-fallback tests. |
| Blank detection is flaky | Valid minimalist pages may fallback | Use conservative thresholds and policy override. Treat severe cases as blocking only when threshold is crossed. |
| Readiness becomes Phase 3B analysis | Scope creep | Only validate evidence usefulness, not design tokens or ecommerce semantics. |
| Schema descriptor becomes too complex | Harder maintenance than JSON Schema | Add only minimum array/non-empty/nested required support now. Re-evaluate before adding more. |
| Node bridge cleanup breaks StorefrontBuilder docs | Confusing boundary | Preserve StorefrontBuilder Node baseline; only demote/remove unsupported ReverseEngineering wrapper. |
| Local gate becomes slow | Developers skip it | Keep focused tests plus one real fixture workflow; make full gate explicit closure command. |
| Closure summary drifts from latest commit | False release evidence | Gate writes commit SHA, and closure summary must be refreshed in same commit after final pass. |

## Decision Audit Trail

| # | Decision | Classification | Principle | Rationale | Rejected |
| --- | --- | --- | --- | --- | --- |
| 1 | Create a new Phase 10 final-fix todo instead of rewriting Phase 09 | Auto-decided | Preserve history | Phase 09 is marked complete and contains implementation evidence. New blockers need a new closure plan. | Mutating completed plan history. |
| 2 | Use local closure evidence instead of GitHub Actions | User-approved | Fit environment | User stated Actions are disabled during development. Local gate plus tracked summary is the right closure proof now. | Requiring CI green run. |
| 3 | Keep scope to Phase 3A foundation | Auto-decided | Hold scope | Review explicitly excludes design-token extraction, ecommerce mapping, and generation. | Expanding into Phase 3B. |
| 4 | Do not delete StorefrontBuilder Node baseline in this phase | Auto-decided | Preserve boundary | Architecture docs still treat the Node script as StorefrontBuilder baseline. Only unsupported ReverseEngineering wrapper should be demoted/removed. | Deleting unrelated StorefrontBuilder script. |
| 5 | Prefer custom schema descriptor extension over full JSON Schema engine for this round | Auto-decided | Smallest complete change | Current registry is descriptor-based. Minimum array/non-empty/nested required rules close the gap without changing the validator model. | Introducing full JSON Schema library now. |

## GSTACK REVIEW REPORT

| Review | Status | Findings | Decision |
| --- | --- | --- | --- |
| CEO | Passed with hold-scope constraint | Phase 3A must close evidence foundation before Phase 3B. No scope expansion recommended. | Keep this as final closure round. |
| Design | Skipped | No UI/product design surface in this plan. | No design review needed. |
| Engineering | Passed with required fixes | Main risks are browser operation split, fallback determinism, readiness depth, and policy migration. | Implement phases F1-F7 in order. |
| DX | Passed with required fixes | `inspect`, docs, and closure report must be developer-readable and machine-checkable. | Fix inspect and local gate evidence before closure. |

VERDICT: Approved plan for implementation as a final Phase 3A closure round.

NO UNRESOLVED DECISIONS

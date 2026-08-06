# StorefrontReverseEngineering Offscreen Evidence Readiness Fix.todo

Status: In progress
Owner: Storefront Platform
Created: 2026-08-06
Target folder: `docs/visual-reverse-engineering-skill`

Depends on:

- `AGENTS.md`
- `docs/architecture/11-storefront-builder.md`
- `docs/visual-reverse-engineering-skill/README.md`
- `docs/agents/storefront-builder.md`
- `scripts/reverse-engineering/run-storefront-reverse-engineering-production.ps1`
- `tools/BlazorShop.AI.StorefrontReverseEngineering/Browser/PlaywrightReferenceBrowser.cs`
- `tools/BlazorShop.AI.StorefrontReverseEngineering/Evidence/VisualEvidenceExtractor.cs`
- `tools/BlazorShop.AI.StorefrontReverseEngineering/Application/VisualProjectWorkflowService.cs`
- `tools/BlazorShop.AI.StorefrontReverseEngineering/tests/BlazorShop.AI.StorefrontReverseEngineering.Tests`

Primary goal: fix the false positive Phase 3A readiness blocker where a valid accessibility/offscreen helper link is captured as visual evidence, receives `x = -99999`, and causes `invalid-element-box`, leaving `Readiness passed: False` for the production Kindred Coast run.

Production verification URL:

```text
https://www.kindredcoast.com/
```

This is a tooling correctness patch, not a Storefront runtime, Commerce Node, Control Plane, cart, checkout, or generated storefront feature.

## Problem Statement

The current production run for Kindred Coast captures three viewports successfully and writes all required Phase 3A artifacts. Build, Playwright, URL probing, screenshots, capture manifests, asset inventory, and capture quality reports all complete. Readiness still fails because the evidence index includes an accessibility skip link:

```text
selector: a.skip-to-content-link.button-secondary
box.x: -99999
box.y: 0
width: 152.13
height: 51.59
```

`VisualProjectWorkflowService` treats any element box with `x < -10` as `invalid-element-box`. The failure is valid according to the current validator, but the element itself is not meaningful visual evidence. It is an accessibility helper intentionally positioned offscreen until focused.

Observed readiness findings:

- `invalid-element-box` for `ev-004` in `home/desktop-1440`.
- `invalid-element-box` for `ev-005` in `home/tablet-768`.
- `invalid-element-box` for `ev-005` in `home/mobile-390`.
- `failed-latest-run` and `partial-latest-run` findings caused by the failed `validate-readiness` step.

## Autoplan Review Decisions

| # | Phase | Decision | Classification | Principle | Rationale | Rejected |
|---|-------|----------|----------------|-----------|-----------|----------|
| 1 | Scope | Fix capture/evidence selection first, not production runner output. | Auto-decided | Explicit over clever | The runner is already using the correct repo root and reports the real failure. The false positive starts when evidence is selected. | Hiding the failure in the runner. |
| 2 | Scope | Keep validator strict for real visual evidence. | Auto-decided | Choose completeness | Real invalid boxes should still block readiness; the fix must avoid admitting bad visual data. | Removing or broadly relaxing `x < -10` validation. |
| 3 | Test | Add regression coverage before behavior change. | Auto-decided | Bias toward action | This bug is small but workflow-blocking; a focused failing test keeps the fix narrow and reviewable. | Manual artifact edits only. |
| 4 | Production QA | Re-run Kindred Coast after the fix, not only fixture tests. | Auto-decided | Choose completeness | The original failure happened on a real Shopify production site with real theme behavior. | Declaring done from synthetic tests only. |
| 5 | DX | Improve diagnostic output only if it remains narrowly scoped. | Taste | Explicit over clever | Better problem/cause/fix text helps future runs, but the core bug can be fixed without expanding CLI behavior. | Large CLI/report redesign. |

## Locked Scope

- Exclude offscreen accessibility/noise helpers from visual evidence before readiness validation.
- Preserve strict readiness validation for real visual elements.
- Add regression tests for offscreen skip links and for real invalid boxes still blocking.
- Re-run the production Kindred Coast workflow through the script in `scripts/reverse-engineering`.
- Record evidence paths and final readiness outcome.

## Explicitly Out Of Scope

- Changing Storefront V2, Presentation, Runtime, Commerce Node, Control Plane, cart, checkout, account, payment, or order behavior.
- Adding new production services, queues, databases, or browser runners.
- Pixel-perfect comparison or visual scoring.
- Making `invalid-element-box` a warning globally.
- Editing generated production artifacts by hand to force readiness pass.
- Treating all hidden/offscreen elements as safe without a narrow accessibility/noise rule.
- Adding generated `artifacts` or `obj` output to Git.

## What Already Exists

| Area | Existing behavior |
| --- | --- |
| Production runner | `scripts/reverse-engineering/run-storefront-reverse-engineering-production.ps1` builds the tool, probes URL, checks Playwright, runs/resumes workflow, inspects, validates, and writes a report. |
| Capture engine | `PlaywrightReferenceBrowser` selects interesting DOM elements and records styles, boxes, and assets from browser state. |
| Evidence extractor | `VisualEvidenceExtractor` joins styles and boxes into `element-evidence-index.json`. |
| Readiness validator | `VisualProjectWorkflowService` blocks invalid boxes, missing evidence, failed latest run, and incomplete required Phase 3A steps. |
| Existing production evidence | `artifacts/storefront-reverse-engineering/projects/kindredcoast` contains the failed Kindred Coast run and screenshots for desktop, tablet, and mobile. |

## Phase Order

1. Phase 3A.1 - Baseline Reproduction And Evidence Lock
2. Phase 3A.2 - Regression Tests For Offscreen Helpers
3. Phase 3A.3 - Evidence Selection Fix
4. Phase 3A.4 - Readiness Validation Safety Net
5. Phase 3A.5 - Kindred Coast Production Re-run
6. Phase 3A.6 - Docs And Runner Guidance
7. Phase 3A.7 - Final Closure Evidence

Do not implement Phase 3A.3 before Phase 3A.2. The first code change must be protected by a failing test that describes the offscreen accessibility helper case.

## Phase 3A.1 - Baseline Reproduction And Evidence Lock

Goal: preserve the current failure as a concrete baseline before changing behavior.

Tasks:

- [x] Confirm current Git status and do not disturb unrelated user files.
- [x] Capture current production runner command and latest report path:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\reverse-engineering\run-storefront-reverse-engineering-production.ps1 -Url "https://www.kindredcoast.com/" -Name "KindredCoast" -Resume
```

- [x] Read `reports/readiness-report.json` from the Kindred Coast artifact root.
- [x] Record all blocking findings and verify the first three are `invalid-element-box`.
- [x] Read each affected `element-evidence-index.json` and `boxes.json`.
- [x] Prove the failing evidence selector and coordinates:
  - [x] `home/desktop-1440`: `ev-004`, selector `a.skip-to-content-link.button-secondary`, `x = -99999`;
  - [x] `home/tablet-768`: `ev-005`, selector `a.skip-to-content-link.button-secondary`, `x = -99999`;
  - [x] `home/mobile-390`: `ev-005`, selector `a.skip-to-content-link.button-secondary`, `x = -99999`.
- [x] Confirm capture quality reports still pass for all three viewports.
- [x] Confirm screenshots are nonblank and visually inspect at least desktop and mobile captures.
- [x] Identify the exact code path:
  - [x] element selection in `PlaywrightReferenceBrowser`;
  - [x] evidence index construction in `VisualEvidenceExtractor`;
  - [x] invalid-box readiness rule in `VisualProjectWorkflowService`.
- [x] Record baseline evidence in this plan under the Evidence section before moving to Phase 3A.2.

Checks:

```powershell
Get-Content artifacts\storefront-reverse-engineering\projects\kindredcoast\reports\readiness-report.json -Raw
Get-Content artifacts\storefront-reverse-engineering\projects\kindredcoast\captures\home\desktop-1440\element-evidence-index.json -Raw
Get-Content artifacts\storefront-reverse-engineering\projects\kindredcoast\captures\home\tablet-768\element-evidence-index.json -Raw
Get-Content artifacts\storefront-reverse-engineering\projects\kindredcoast\captures\home\mobile-390\element-evidence-index.json -Raw
```

DoD:

- [x] Root cause evidence is written down with file paths and element IDs.
- [x] The failure is confirmed as a false positive on accessibility/offscreen helper evidence, not missing capture output.
- [x] No production artifact is manually edited.

## Phase 3A.2 - Regression Tests For Offscreen Helpers

Goal: add tests that fail before the fix and distinguish accessibility/noise helpers from real bad visual evidence.

Tasks:

- [x] Add a focused unit or integration test around evidence selection/readiness for a page containing:
  - [x] visible header/main/section/product-like visual evidence;
  - [x] a skip link positioned with `left: -99999px`;
  - [x] a visually hidden accessibility node;
  - [x] a real visual element with an impossible invalid box for negative coverage.
- [x] Prove offscreen skip links do not appear in `element-evidence-index.json`, or appear as ignored evidence that readiness does not evaluate as visual evidence.
- [x] Prove a real visual element with invalid coordinates still produces `invalid-element-box`.
- [x] Include selectors/classes commonly used by Shopify themes:
  - [x] `.skip-to-content-link`;
  - [x] `.visually-hidden`;
  - [x] `[aria-live]` helper node if existing code paths support it.
- [x] Keep tests under the existing StorefrontReverseEngineering test project.
- [x] Use test names that state the problem/cause/fix, for example:
  - [x] `Playwright_HttpFixture_ExcludesOffscreenAccessibilityHelpersFromRenderedEvidence`;
  - [x] `Readiness_StillBlocksInvalidVisibleElementBox`.
- [x] Avoid live network dependencies in these tests.

Suggested commands:

```powershell
dotnet test tools\BlazorShop.AI.StorefrontReverseEngineering\tests\BlazorShop.AI.StorefrontReverseEngineering.Tests\BlazorShop.AI.StorefrontReverseEngineering.Tests.csproj --filter "FullyQualifiedName~Readiness|FullyQualifiedName~Evidence" --blame-hang --blame-hang-timeout 5m
```

DoD:

- [x] At least one regression test fails before implementation because the skip link is treated as invalid visual evidence.
- [x] At least one safety-net test proves real invalid visible boxes remain blocking.
- [x] Tests do not depend on `https://www.kindredcoast.com/`.

## Phase 3A.3 - Evidence Selection Fix

Goal: remove non-visual offscreen helper elements from visual evidence at the source while preserving strict validation for visible visual elements.

Preferred implementation direction:

- Apply the primary filter in `PlaywrightReferenceBrowser` or the narrowest shared evidence selection path.
- Treat offscreen accessibility helpers as non-visual evidence when they meet clear criteria.
- Keep `VisualProjectWorkflowService` strict for the remaining visual evidence.

Tasks:

- [x] Inspect existing `CapturePolicyDefaults.ResolveNoiseSelectors(policy)` behavior.
- [x] Decide whether the fix belongs in:
  - [x] browser `interesting` predicate;
  - [x] default capture noise selectors;
  - [x] evidence extractor filtering;
  - [x] readiness validator exception path.
- [x] Prefer the least surprising rule:
  - [x] exclude elements whose bounding rect is fully offscreen left or top and whose selector/class/attributes identify accessibility helpers;
  - [x] exclude common helper selectors such as `.skip-to-content-link`, `.visually-hidden`, `[aria-live]` status nodes only when they are not visible visual UI;
  - [x] do not exclude visible focused skip links if a future capture intentionally focuses them.
- [x] Ensure the rule does not remove meaningful sticky banners, announcement bars, nav links, product cards, buttons, images, hero sections, or footer content.
- [x] Ensure evidence IDs remain stable enough for downstream blueprint references after filtering.
- [x] If filtering changes evidence ID ordering, verify blueprint evidence references still match available evidence IDs.
- [x] Keep changes narrowly scoped to StorefrontReverseEngineering tooling.
- [x] Do not add site-specific Kindred Coast selectors unless no generic safe rule is possible.

Checks:

```powershell
dotnet build tools\BlazorShop.AI.StorefrontReverseEngineering\BlazorShop.AI.StorefrontReverseEngineering.csproj
dotnet test tools\BlazorShop.AI.StorefrontReverseEngineering\tests\BlazorShop.AI.StorefrontReverseEngineering.Tests\BlazorShop.AI.StorefrontReverseEngineering.Tests.csproj --filter "FullyQualifiedName~Readiness|FullyQualifiedName~Evidence" --blame-hang --blame-hang-timeout 5m
```

DoD:

- [x] Offscreen accessibility helper evidence no longer triggers `invalid-element-box`.
- [x] Real invalid visible boxes still trigger `invalid-element-box`.
- [x] Capture quality artifacts remain unchanged in purpose and schema.
- [x] No StorefrontBuilder, Storefront V2, Runtime, API, or Commerce behavior changes are required.

## Phase 3A.4 - Readiness Validation Safety Net

Goal: harden the validator/test coverage so this fix does not become a broad bypass.

Tasks:

- [x] Review `VisualProjectWorkflowService.ValidateViewportArtifactsAsync`.
- [x] Keep these readiness blockers intact:
  - [x] width <= 0;
  - [x] height <= 0;
  - [x] unexpected `x < -10` on visible visual evidence;
  - [x] unexpected `y < -10` on visible visual evidence;
  - [x] coordinates beyond `FinalWidth * 2`;
  - [x] coordinates beyond `FinalHeight * 2`.
- [x] Add focused test coverage if Phase 3A.2 did not already prove all relevant rules.
- [x] Confirm `missing-useful-bounding-box` still fails when no useful visual boxes exist.
- [x] Confirm major visible evidence categories remain present:
  - [x] `semantic-landmark`;
  - [x] `section`;
  - [x] `heading`;
  - [x] `product-card-candidate`.
- [x] If the fix adds ignored evidence diagnostics, ensure diagnostics are informational and do not pollute `readiness-report.json` with false blockers.

Checks:

```powershell
dotnet test tools\BlazorShop.AI.StorefrontReverseEngineering\tests\BlazorShop.AI.StorefrontReverseEngineering.Tests\BlazorShop.AI.StorefrontReverseEngineering.Tests.csproj --filter "FullyQualifiedName~EndToEndCliTests|FullyQualifiedName~Readiness|FullyQualifiedName~Evidence" --blame-hang --blame-hang-timeout 5m
```

DoD:

- [x] Validator remains strict for real visual evidence.
- [x] False positive accessibility/noise helpers are handled before or during validation in a documented, test-proven way.
- [x] No broad `invalid-element-box` downgrade is introduced.

## Phase 3A.5 - Kindred Coast Production Re-run

Goal: prove the fix on the original production site and update the existing `kindredcoast` artifact without manual artifact edits.

Tasks:

- [ ] Start from a known Git state and record `git status --porcelain=v1`.
- [ ] Decide whether to use `-Force` or targeted resume:
  - [ ] use `-Force` if old artifact contents retain stale evidence IDs from the previous failed capture;
  - [ ] use `-Resume` only if the workflow can force the relevant capture/readiness steps cleanly.
- [ ] Run production capture against Kindred Coast:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\reverse-engineering\run-storefront-reverse-engineering-production.ps1 -Url "https://www.kindredcoast.com/" -Name "KindredCoast" -Force -CommandTimeoutSeconds 900
```

- [ ] If Playwright Chromium is missing, rerun once with `-InstallPlaywright`.
- [ ] Confirm the runner writes report under:

```text
artifacts/storefront-reverse-engineering/reports/
```

- [ ] Confirm the project root remains:

```text
artifacts/storefront-reverse-engineering/projects/kindredcoast
```

- [ ] Inspect `readiness-report.json`.
- [ ] Confirm no `invalid-element-box` finding exists for skip links or visually hidden helpers.
- [ ] Confirm all three viewport capture quality reports pass.
- [ ] Inspect screenshots for desktop and mobile:
  - [ ] desktop screenshot nonblank and includes Kindred Coast header/hero;
  - [ ] mobile screenshot nonblank and includes header/hero/product/customer sections.
- [ ] If readiness still fails, classify remaining findings:
  - [ ] true visual evidence issue;
  - [ ] unrelated production-site dynamic issue;
  - [ ] new tooling false positive.
- [ ] Do not mark Phase 3A.5 complete while `invalid-element-box` still points to accessibility/offscreen helpers.

Checks:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\reverse-engineering\run-storefront-reverse-engineering-production.ps1 -Url "https://www.kindredcoast.com/" -Name "KindredCoast" -Force -CommandTimeoutSeconds 900
dotnet run --project tools\BlazorShop.AI.StorefrontReverseEngineering\BlazorShop.AI.StorefrontReverseEngineering.csproj -- inspect --project artifacts\storefront-reverse-engineering\projects\kindredcoast
dotnet run --project tools\BlazorShop.AI.StorefrontReverseEngineering\BlazorShop.AI.StorefrontReverseEngineering.csproj -- validate --project artifacts\storefront-reverse-engineering\projects\kindredcoast
```

DoD:

- [ ] Production runner completes without script/path errors.
- [ ] Kindred Coast capture produces all required viewport artifacts.
- [ ] Readiness no longer fails because of offscreen skip-link evidence.
- [ ] Any remaining `Readiness passed: False` is documented with a new root cause and is not this bug.

## Phase 3A.6 - Docs And Runner Guidance

Goal: make the fix understandable for future agents and users running production reverse engineering.

Tasks:

- [ ] Update `docs/visual-reverse-engineering-skill/README.md` if the production runner or evidence policy becomes part of the documented workflow.
- [ ] Update `docs/visual-reverse-engineering-skill/how-to-generate-and-validate.md` with the Kindred Coast production command if it is now the canonical example.
- [ ] Update `docs/agents/storefront-builder.md` only if the agent workflow needs a new rule about offscreen accessibility/noise evidence.
- [ ] Update `tools/BlazorShop.AI.StorefrontReverseEngineering/README.md` if capture policy/default noise selector behavior changes.
- [ ] If the runner gets new options, ensure `-Help` output explains problem/cause/fix clearly.
- [ ] Do not document `artifacts` output as committed source.

Checks:

```powershell
rg -n "offscreen|skip-to-content|skip link|noiseSelectors|KindredCoast|run-storefront-reverse-engineering-production" docs\visual-reverse-engineering-skill docs\agents tools\BlazorShop.AI.StorefrontReverseEngineering\README.md scripts\reverse-engineering
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\reverse-engineering\run-storefront-reverse-engineering-production.ps1 -Help
```

DoD:

- [ ] Docs explain why offscreen accessibility helpers are not visual evidence.
- [ ] Production runner usage remains copy-paste runnable.
- [ ] Future agents know not to solve this by weakening all invalid-box validation.

## Phase 3A.7 - Final Closure Evidence

Goal: finish with a clean, reviewable proof set and one commit per implemented phase.

Required final commands:

```powershell
git status --porcelain=v1
dotnet build tools\BlazorShop.AI.StorefrontReverseEngineering\BlazorShop.AI.StorefrontReverseEngineering.csproj
dotnet test tools\BlazorShop.AI.StorefrontReverseEngineering\tests\BlazorShop.AI.StorefrontReverseEngineering.Tests\BlazorShop.AI.StorefrontReverseEngineering.Tests.csproj --filter "FullyQualifiedName~Readiness|FullyQualifiedName~Evidence|FullyQualifiedName~EndToEndCliTests" --blame-hang --blame-hang-timeout 5m
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\reverse-engineering\run-storefront-reverse-engineering-production.ps1 -Url "https://www.kindredcoast.com/" -Name "KindredCoast" -Force -CommandTimeoutSeconds 900
git status --porcelain=v1
```

Evidence to record:

- [ ] Commit hash for each implementation phase.
- [ ] Test command output summary.
- [ ] Production runner report path.
- [ ] Kindred Coast project root.
- [ ] Readiness report path.
- [ ] Final readiness result.
- [ ] Any remaining non-blocking warnings.
- [ ] Screenshot paths inspected.
- [ ] Final `git status --porcelain=v1`.

DoD:

- [ ] All planned tests pass.
- [ ] Production Kindred Coast run proves the original false positive is gone.
- [ ] If `Readiness passed` is still `False`, the remaining blocker is not `invalid-element-box` on offscreen accessibility helper evidence and is documented as a separate follow-up.
- [ ] No generated production artifacts are committed.
- [ ] Only intentional source/docs/test files are committed.

## Release Definition Of Done

- [ ] Baseline root cause is recorded from actual Kindred Coast artifacts.
- [ ] Regression test covers `a.skip-to-content-link.button-secondary` with `x = -99999`.
- [ ] Regression test proves real invalid visible boxes still block readiness.
- [ ] Evidence selection no longer includes offscreen accessibility helpers as visual evidence.
- [ ] Validator still blocks invalid visible visual evidence.
- [ ] Kindred Coast production run completes through the runner in `scripts/reverse-engineering`.
- [ ] No `invalid-element-box` finding remains for skip links, visually hidden accessibility helpers, or offscreen ARIA helper nodes.
- [ ] Capture quality remains passed for desktop, tablet, and mobile.
- [ ] Desktop and mobile screenshots are nonblank.
- [ ] Docs or README mention the capture/evidence policy if behavior changes.
- [ ] Final workspace contains no accidental generated artifact changes.

## Risk Register

## Evidence

### Phase 3A.1 Baseline - 2026-08-06

- Git status before implementation: untracked plan file plus unrelated untracked `scripts/reverse-engineering/readme.md`; unrelated file was not edited.
- Baseline command:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\reverse-engineering\run-storefront-reverse-engineering-production.ps1 -Url "https://www.kindredcoast.com/" -Name "KindredCoast" -Resume -CommandTimeoutSeconds 900
```

- Runner report: `artifacts/storefront-reverse-engineering/reports/storefront-reverse-engineering-production-kindredcoast-20260806110448.md`.
- Project root: `artifacts/storefront-reverse-engineering/projects/kindredcoast`.
- Readiness report: `artifacts/storefront-reverse-engineering/projects/kindredcoast/reports/readiness-report.json`.
- Readiness result: `passed=false`, `blocking=6`, `warnings=0`.
- Root blockers:
  - `home/desktop-1440`: `ev-004`, selector `a.skip-to-content-link.button-secondary`, box `x=-99999`, `y=0`, `width=152.13`, `height=51.59`.
  - `home/tablet-768`: `ev-005`, selector `a.skip-to-content-link.button-secondary`, box `x=-99999`, `y=0`, `width=152.13`, `height=51.59`.
  - `home/mobile-390`: `ev-005`, selector `a.skip-to-content-link.button-secondary`, box `x=-99999`, `y=0`, `width=152.13`, `height=51.59`.
- Derived blockers: `failed-latest-run` and two `partial-latest-run` findings from failed `validate-readiness`.
- Capture quality reports passed for `desktop-1440`, `tablet-768`, and `mobile-390`.
- Visually inspected screenshots:
  - `captures/home/desktop-1440/full-page.png` is nonblank and shows Kindred Coast header/hero.
  - `captures/home/mobile-390/full-page.png` is nonblank and shows header/hero/product/customer/footer sections.
- Code path:
  - `Browser/PlaywrightReferenceBrowser.cs`: browser-side `interesting` predicate includes visible-size anchors.
  - `Evidence/VisualEvidenceExtractor.cs`: joins captured style and box evidence into `element-evidence-index.json`.
  - `Application/VisualProjectWorkflowService.cs`: readiness blocks boxes with `width <= 0`, `height <= 0`, `x < -10`, `y < -10`, or far out-of-range coordinates.

The baseline confirms this is a false positive on a valid offscreen accessibility helper. It is not missing output, blank screenshots, failed Playwright setup, or a production runner path error.

### Phase 3A.2 Regression Tests - 2026-08-06

- Added `Playwright_HttpFixture_ExcludesOffscreenAccessibilityHelpersFromRenderedEvidence` in `PlaywrightIntegrationTests.cs`.
- Added `Readiness_StillBlocksInvalidVisibleElementBox` in `EndToEndCliTests.cs`.
- Failing pre-fix command:

```powershell
dotnet test tools\BlazorShop.AI.StorefrontReverseEngineering\tests\BlazorShop.AI.StorefrontReverseEngineering.Tests\BlazorShop.AI.StorefrontReverseEngineering.Tests.csproj --filter "FullyQualifiedName~Playwright_HttpFixture_ExcludesOffscreenAccessibilityHelpersFromRenderedEvidence|FullyQualifiedName~Readiness_StillBlocksInvalidVisibleElementBox" --blame-hang --blame-hang-timeout 5m
```

- Result before implementation: `Failed: 1, Passed: 1, Total: 2`.
- Expected failing assertion: captured boxes still include `a.skip-to-content-link.button-secondary` with `x=-99999` and `div.visually-hidden` with `x=-99999`.
- Safety-net result: `Readiness_StillBlocksInvalidVisibleElementBox` passed, proving a real visual element moved to `x=-99999` remains an `invalid-element-box` blocker.

### Phase 3A.3 Evidence Selection Fix - 2026-08-06

- Implemented `isNonVisualAccessibilityHelper` inside `Browser/PlaywrightReferenceBrowser.cs`.
- Kept `CapturePolicyDefaults.ResolveNoiseSelectors(policy)` unchanged: default noise selectors remain `.cookie-banner` and `[data-capture-noise]`.
- Decision: filter in the browser `interesting` predicate, before styles/boxes/assets become rendered evidence. This keeps readiness strict and avoids site-specific Kindred Coast rules.
- Excluded only helper-like elements when they are fully offscreen or clipped to assistive-only dimensions:
  - helper selectors/classes such as `skip-to-content`, `skip-link`, `visually-hidden`, `sr-only`, `screen-reader`;
  - `aria-live` helper nodes with status-like roles;
  - offscreen/clipped layout state.
- Build command passed:

```powershell
dotnet build tools\BlazorShop.AI.StorefrontReverseEngineering\tests\BlazorShop.AI.StorefrontReverseEngineering.Tests\BlazorShop.AI.StorefrontReverseEngineering.Tests.csproj
```

- Focused regression command passed: `Failed: 0, Passed: 2, Total: 2`.
- Wider phase command passed: `Failed: 0, Passed: 143, Total: 143`, duration `1 m 55 s`.
- One attempted parallel build/test produced compiler file lock `CS2012` from simultaneous writes to the same project output; reran sequentially and passed.

### Phase 3A.4 Readiness Validation Safety Net - 2026-08-06

- Added `Readiness_BlocksInvalidVisibleElementBoxBoundaryRules` to cover width/height zero, negative x/y, and coordinates beyond the quality report final dimensions.
- Added `Readiness_MissingUsefulBoundingBoxesStillFails` to prove all-zero boxes still trigger `missing-useful-bounding-box` and `invalid-element-box`.
- Added `Evidence_ClassifiesMajorVisibleEvidenceCategories` to lock evidence categories for `semantic-landmark`, `section`, `heading`, and `product-card-candidate`.
- No readiness validator downgrade or warning conversion was introduced.
- Validation command passed:

```powershell
dotnet test tools\BlazorShop.AI.StorefrontReverseEngineering\tests\BlazorShop.AI.StorefrontReverseEngineering.Tests\BlazorShop.AI.StorefrontReverseEngineering.Tests.csproj --no-build --filter "FullyQualifiedName~EndToEndCliTests|FullyQualifiedName~Readiness|FullyQualifiedName~Evidence" --blame-hang --blame-hang-timeout 5m
```

- Result: `Failed: 0, Passed: 154, Total: 154`, duration `2 m`.

| Risk | Impact | Mitigation |
| --- | --- | --- |
| Filtering is too broad and removes useful visual evidence | Blueprint quality drops or misses real page structure | Filter only clear accessibility/noise helpers and keep tests for major evidence categories. |
| Validator is weakened too much | Real capture bugs pass readiness | Keep `invalid-element-box` blocking for visible visual evidence and add negative tests. |
| Evidence IDs shift after filtering | Blueprint references can point to missing evidence | Validate blueprint evidence references and rerun end-to-end CLI/readiness tests. |
| Kindred Coast dynamic widgets still create other blockers | Production run remains `False` | Classify any new blocker separately; do not conflate it with skip-link fix. |
| `-Resume` reuses stale failed artifacts | Fix appears ineffective | Use `-Force` for production proof unless a targeted force-step flow is proven. |
| Site changes during capture | Flaky production proof | Rely on artifact diagnostics and screenshots; keep deterministic fixture tests as the authoritative regression guard. |

## Implementation Commit Plan

Commit one phase at a time:

1. `Phase 3A.1: record offscreen evidence readiness baseline`
2. `Phase 3A.2: add offscreen evidence readiness regression tests`
3. `Phase 3A.3: filter nonvisual offscreen helper evidence`
4. `Phase 3A.4: harden readiness invalid-box safety tests`
5. `Phase 3A.5: prove Kindred Coast readiness no longer fails on skip link`
6. `Phase 3A.6: document offscreen evidence capture policy`
7. `Phase 3A.7: record final closure evidence`

Do not combine test, implementation, production proof, and docs into one commit. The false positive is small, but its workflow impact is large enough that each evidence layer should stay reviewable.

## Agent Implementation Handoff

When implementing this plan:

- Start with the failing regression test.
- Do not edit Kindred Coast artifacts by hand.
- Prefer fixing capture/evidence selection over weakening readiness validation.
- Keep the production runner command exactly copy-paste runnable.
- Use `-Force` for the final production proof unless the artifact store supports a clean targeted re-capture.
- Treat `Readiness passed: False` after the fix as acceptable only if the remaining findings are different and documented with a new root cause.
- Commit after each phase and update this checklist before moving to the next phase.

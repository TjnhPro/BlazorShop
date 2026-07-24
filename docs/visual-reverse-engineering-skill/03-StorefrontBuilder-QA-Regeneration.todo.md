# StorefrontBuilder QA Regeneration.todo

Status: planning
Scope: BlazorShop.AI.StorefrontBuilder Visual Reverse Engineering Skill MVP
Phase group: S20-S29

## Purpose

Prove the generated `BlazorShop.Storefront.{Name}` is buildable, visually useful, boundary-safe, idempotent, and does not break ecommerce flows supplied by Starter, Runtime, BFF, and Commerce Node Storefront APIs.

This phase group depends on:

- `01-StorefrontBuilder-Foundation.todo.md`
- `02-StorefrontBuilder-Visual-Generation.todo.md`

## Current Codebase Anchors

- Existing package gate: `scripts/qa/run-storefront-starter-isolation-gate.ps1`
- Existing sample release gate: `scripts/qa/run-storefront-sample-release-gate.ps1`
- Existing AI guardrails: `docs/storefront-platform/storefront-ai-generator-plan.md`
- Existing Starter route model: `Pages/Ssr`, `Pages/Hybrid`, `Pages/WasmHost`
- Existing same-origin BFF pattern: `Endpoints/StarterBffEndpoints.cs`
- Existing public API contract standard: `docs/architecture/09-api-contract-standards.md`
- Existing contract ownership standard: `docs/architecture/10-v2-contract-ownership.md`

## S20 - Asset Pipeline And Provenance

Goal: handle assets safely while recognizing generated stores will be manually tuned before production use.

Tasks:

- [x] Discover target image/font/icon assets.
- [x] Record source URL.
- [x] Record checksum.
- [x] Record content type.
- [x] Record detected usage.
- [x] Normalize filenames.
- [x] Deduplicate assets.
- [x] Copy only allowed assets.
- [x] Generate placeholders where asset rights are unclear.
- [x] Mark replacement-needed assets clearly.
- [x] Support later manual replacement without breaking generated manifests.

Output:

- [x] `asset-manifest.yaml`

Rules:

- [x] No production claim that reference-site assets are licensed.
- [x] Missing asset must not break build.
- [x] Manual asset replacement must not require code changes.

Done when:

- [x] Generated storefront can render with placeholders and a clear asset replacement list.

Verification:

- [x] Broken asset URL check.
- [x] Manifest validation.
- [x] Browser check confirms no broken visible critical images.

## S21 - Static Validation Gate

Goal: catch structural problems before running browser QA.

Checks:

- [x] All artifact schemas pass.
- [x] Starter generation contract passes.
- [x] Composition manifest passes.
- [x] Generation plan passes.
- [x] Generated file manifest passes.
- [x] Asset manifest passes.
- [x] No protected file violation.
- [x] No forbidden dependency/import.
- [x] No direct Commerce Node browser call.
- [x] No DTO clone.
- [x] No missing slot.
- [x] No missing asset.
- [x] No duplicate route.
- [x] No package version mismatch.

Done when:

- [x] `validate-storefront` can fail fast with actionable messages.

Verification:

- [x] Unit tests for each validation category.
- [x] Bad fixture project fails validation with exact rule IDs.

## S22 - Build And Isolation Gate

Goal: prove generated storefront builds like an independent consumer.

Tasks:

- [x] Restore packages from configured feed.
- [x] Build generated `BlazorShop.Storefront.{Name}`.
- [x] Pack `BlazorShop.Storefront.Client`.
- [x] Pack `BlazorShop.Storefront.Runtime`.
- [x] Build generated storefront from packages, not project references.
- [x] Confirm no dependency on `Storefront.V2`.
- [x] Confirm no dependency on backend/core/API projects.
- [x] Confirm package compatibility metadata is present.

Recommended command shape:

```powershell
.\scripts\qa\run-storefront-builder-isolation-gate.ps1 -Name BlazorShop.Storefront.{Name}
```

Done when:

- [x] Generated storefront can be restored and built from packages in isolation.

Verification:

- [x] CI describe mode lists the isolation gate.
- [x] Local gate passes for a fixture generated storefront.

## S23 - Visual QA Gate

Goal: compare target evidence and generated storefront without requiring impossible pixel-perfect matching.

Pages:

- [x] Header/footer shell.
- [x] Home.
- [x] Catalog/category.
- [x] Product detail.
- [x] Cart fallback.
- [x] Checkout fallback.
- [x] Account fallback.

Viewports:

- [x] Desktop 1440px.
- [x] Tablet 768px.
- [x] Mobile 390px.

Severity:

- [x] Critical: wrong section order, broken layout, hidden primary content, unusable mobile, missing major component.
- [x] Major: strong typography/spacing/color mismatch, incorrect product card layout, weak responsive behavior.
- [x] Minor: decorative mismatch, small animation/shadow/icon difference.

Done criteria:

- [x] Zero Critical discrepancies.
- [x] Major discrepancies are under agreed threshold.
- [x] Minor discrepancies are recorded but do not block MVP.

Output:

- [x] `visual-qa-report.md`

Verification:

- [x] Playwright screenshots saved for target and generated storefront.
- [x] Report links each discrepancy to screenshot/evidence.

## S24 - Basic Commerce Regression Gate

Goal: prove visual generation did not break Starter ecommerce behavior.

Browser tests:

- [x] Home renders.
- [x] Catalog renders.
- [x] Product renders.
- [x] Product link navigation works.
- [x] Product image/gallery region renders.
- [x] Quantity control can change.
- [x] Add-to-cart command works through same-origin BFF.
- [x] Cart badge updates.
- [x] Cart page renders.
- [x] Checkout route renders.
- [x] Account route renders.
- [x] Login/register shell renders according to store policy.
- [x] Product SEO initial HTML exists.
- [x] Browser does not call Commerce Node protected APIs directly.

Notes:

- [x] COD test order placement can be included only when test store/env is configured.
- [x] PayPal/Stripe production providers are not required for this MVP; sandbox can be separate later.

Output:

- [x] `functional-commerce-report.md`

Done when:

- [x] Generated storefront passes real browser commerce regression for Starter-supported paths.

Verification:

- [x] Playwright tests fail on direct Commerce Node browser calls.
- [x] Playwright tests fail when add-to-cart bypasses BFF.

## S25 - Idempotent Regeneration

Goal: allow repeated generation and later manual tuning without unexpected diffs.

Generated file manifest:

- [x] File path.
- [x] Ownership.
- [x] Generator version.
- [x] Source artifact IDs.
- [x] Source/spec hash.
- [x] Generated hash.
- [x] Last generated timestamp.
- [x] Manual edit detected flag.
- [x] Conflict status.

Commands:

- [x] Regenerate all generated files.
- [x] Regenerate one page.
- [x] Regenerate one component.
- [x] Regenerate only CSS tokens.
- [x] Validate without writing.
- [x] Show conflict report.

Rules:

- [x] No-op run must create no diff.
- [x] Manual changes in generated files must be detected.
- [x] Manual changes in managed files must require explicit patch plan.
- [x] Protected files must never be modified.

Output:

- [x] `generated-files.yaml`
- [x] `regeneration-report.md`

Done when:

- [x] Running the same input twice produces no unexpected file changes.

Verification:

- [x] Idempotency test on fixture project.
- [x] Manual edit conflict fixture test.

## S26 - Human Review And Tuning Workflow

Goal: support the expected reality that generated stores are tuned before use.

Review artifacts:

- [x] Visual decision summary.
- [x] Unsupported feature list.
- [x] Hidden target feature list.
- [x] Starter fallback list.
- [x] Asset replacement list.
- [x] AI inference review list.
- [x] Manual tuning checklist.

Workflow:

- [x] `analyze-only` creates artifacts only.
- [x] `plan-only` creates generation plan only.
- [x] `generate` writes generated visual output.
- [x] `update` safely refreshes an existing generated store.
- [x] `validate-only` runs guards and gates without generation.
- [x] `full` runs capture, plan, generate, build, visual QA, and commerce regression.

Done when:

- [x] Human can inspect decisions before trusting generated output.

Verification:

- [x] `plan-only` creates no source diffs outside analysis artifacts.
- [x] `validate-only` creates no source diffs.

## S27 - Skill Commands And Packaging

Goal: expose a predictable development-time command surface.

Commands:

- [x] `/analyze-storefront <url>`
- [x] `/map-storefront`
- [x] `/generate-storefront`
- [x] `/validate-storefront`
- [x] `/build-storefront <url>`

Required options:

- [x] `--name`
- [x] `--store-key`
- [x] `--starter`
- [x] `--output-root`
- [x] `--mode`
- [x] `--force`
- [x] `--skip-visual-qa`
- [x] `--skip-commerce-regression`

Docs:

- [x] README with quick start.
- [x] Examples for one reference URL.
- [x] Examples for multiple URLs.
- [x] Failure troubleshooting.
- [x] Protected file rule explanation.

Done when:

- [x] A developer can run a full POC from docs without guessing required inputs.

Verification:

- [x] Command help snapshot test.
- [x] README command examples are tested or smoke-checked.

## S28 - CI And Release Gate Integration

Goal: add enough automated checks to keep StorefrontBuilder from regressing.

CI checks:

- [x] Schema tests.
- [x] Preflight tests.
- [x] Protected file guard tests.
- [x] Generation fixture tests.
- [x] Idempotency tests.
- [x] Isolation gate describe mode.
- [x] Visual QA fixture smoke.
- [x] Commerce regression fixture smoke.

Do not run by default on every normal backend PR unless cost is acceptable:

- [x] Full external reference-site capture.
- [x] Full visual diff against live target.
- [x] Full payment/order browser regression.

Done when:

- [x] CI protects contracts and generation safety without making every backend PR slow.

Verification:

- [x] Workflow includes fast checks.
- [x] Expensive browser checks can be run manually or nightly.

## S29 - MVP POC Gate

Goal: finish the Visual Reverse Engineering Skill MVP with one generated storefront proof.

POC command:

```powershell
.\tools\BlazorShop.AI.StorefrontBuilder\build-storefront.ps1 `
  -Url https://reference.example `
  -Name Demo `
  -StoreKey sample `
  -Mode full
```

Expected output:

```text
BlazorShop.PresentationV2/BlazorShop.Storefront.Demo
```

MVP must prove:

- [x] Capture desktop/tablet/mobile.
- [x] Page inventory exists.
- [x] Design tokens exist.
- [x] UI patterns exist.
- [x] Behavior/responsive model exists.
- [x] Page topology exists.
- [x] Capability decisions exist.
- [x] Composition manifest exists.
- [x] Generation plan exists.
- [x] Generated project exists.
- [x] Generated CSS/components/pages exist.
- [x] Asset manifest exists.
- [x] Generated file manifest exists.
- [x] Build passes.
- [x] Dependency guard passes.
- [x] Visual QA has zero Critical findings.
- [x] Basic commerce regression passes.
- [x] No direct browser Commerce Node calls.
- [x] No protected file changes.
- [x] Re-run is idempotent.

Done when:

- [x] StorefrontBuilder can generate one usable `Storefront.{Name}` project from a reference site and prove it did not break ecommerce/security contracts.

## Deferred Beyond MVP

- [x] Full AI Storefront Generator orchestration.
- [x] Control Plane marketplace UI.
- [x] Automatic module install.
- [x] Backend plugin generation.
- [x] Full cart/checkout/payment/order reconstruction.
- [x] Multi-store upgrade orchestration.
- [x] Multi-target visual blending.
- [x] Advanced visual diff engine.
- [x] Full legal asset management.
- [x] React/Next skeleton generation.

## Autoplan Decision Audit Trail

| # | Phase | Decision | Classification | Principle | Rationale | Rejected |
|---|-------|----------|----------------|-----------|-----------|----------|
| 1 | S20 | Allow placeholder/manual replacement asset workflow | User direction preserved | Practical generated-store workflow | Generated store is not production-ready immediately and will be tuned. | Blocking generation on asset licensing completeness |
| 2 | S23 | Use severity-based visual QA, not pixel-perfect requirement | Auto-decided | Useful MVP gate | Pixel-perfect every detail is too expensive and not needed for first generated storefront. | Pixel-perfect blocking gate |
| 3 | S24 | Browser commerce regression must test real Starter behavior | User direction preserved | Browser tests find real faults | Smoke-only checks do not catch broken BFF/cart/checkout wiring. | Static smoke only |
| 4 | S25 | Idempotency is MVP, not later polish | Auto-decided | Safe regeneration | Generated stores will be tuned and regenerated; no-op stability is required. | One-shot generator |
| 5 | S28 | Keep expensive live visual capture out of every normal backend PR | Auto-decided | CI must stay usable | Fast safety checks should run often; heavy browser runs can be manual/nightly. | Full live visual QA on every PR |

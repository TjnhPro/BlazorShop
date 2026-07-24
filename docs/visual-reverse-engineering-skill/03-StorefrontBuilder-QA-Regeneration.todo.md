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

- [ ] Discover target image/font/icon assets.
- [ ] Record source URL.
- [ ] Record checksum.
- [ ] Record content type.
- [ ] Record detected usage.
- [ ] Normalize filenames.
- [ ] Deduplicate assets.
- [ ] Copy only allowed assets.
- [ ] Generate placeholders where asset rights are unclear.
- [ ] Mark replacement-needed assets clearly.
- [ ] Support later manual replacement without breaking generated manifests.

Output:

- [ ] `asset-manifest.yaml`

Rules:

- [ ] No production claim that reference-site assets are licensed.
- [ ] Missing asset must not break build.
- [ ] Manual asset replacement must not require code changes.

Done when:

- [ ] Generated storefront can render with placeholders and a clear asset replacement list.

Verification:

- [ ] Broken asset URL check.
- [ ] Manifest validation.
- [ ] Browser check confirms no broken visible critical images.

## S21 - Static Validation Gate

Goal: catch structural problems before running browser QA.

Checks:

- [ ] All artifact schemas pass.
- [ ] Starter generation contract passes.
- [ ] Composition manifest passes.
- [ ] Generation plan passes.
- [ ] Generated file manifest passes.
- [ ] Asset manifest passes.
- [ ] No protected file violation.
- [ ] No forbidden dependency/import.
- [ ] No direct Commerce Node browser call.
- [ ] No DTO clone.
- [ ] No missing slot.
- [ ] No missing asset.
- [ ] No duplicate route.
- [ ] No package version mismatch.

Done when:

- [ ] `validate-storefront` can fail fast with actionable messages.

Verification:

- [ ] Unit tests for each validation category.
- [ ] Bad fixture project fails validation with exact rule IDs.

## S22 - Build And Isolation Gate

Goal: prove generated storefront builds like an independent consumer.

Tasks:

- [ ] Restore packages from configured feed.
- [ ] Build generated `BlazorShop.Storefront.{Name}`.
- [ ] Pack `BlazorShop.Storefront.Client`.
- [ ] Pack `BlazorShop.Storefront.Runtime`.
- [ ] Build generated storefront from packages, not project references.
- [ ] Confirm no dependency on `Storefront.V2`.
- [ ] Confirm no dependency on backend/core/API projects.
- [ ] Confirm package compatibility metadata is present.

Recommended command shape:

```powershell
.\scripts\qa\run-storefront-builder-isolation-gate.ps1 -Name BlazorShop.Storefront.{Name}
```

Done when:

- [ ] Generated storefront can be restored and built from packages in isolation.

Verification:

- [ ] CI describe mode lists the isolation gate.
- [ ] Local gate passes for a fixture generated storefront.

## S23 - Visual QA Gate

Goal: compare target evidence and generated storefront without requiring impossible pixel-perfect matching.

Pages:

- [ ] Header/footer shell.
- [ ] Home.
- [ ] Catalog/category.
- [ ] Product detail.
- [ ] Cart fallback.
- [ ] Checkout fallback.
- [ ] Account fallback.

Viewports:

- [ ] Desktop 1440px.
- [ ] Tablet 768px.
- [ ] Mobile 390px.

Severity:

- [ ] Critical: wrong section order, broken layout, hidden primary content, unusable mobile, missing major component.
- [ ] Major: strong typography/spacing/color mismatch, incorrect product card layout, weak responsive behavior.
- [ ] Minor: decorative mismatch, small animation/shadow/icon difference.

Done criteria:

- [ ] Zero Critical discrepancies.
- [ ] Major discrepancies are under agreed threshold.
- [ ] Minor discrepancies are recorded but do not block MVP.

Output:

- [ ] `visual-qa-report.md`

Verification:

- [ ] Playwright screenshots saved for target and generated storefront.
- [ ] Report links each discrepancy to screenshot/evidence.

## S24 - Basic Commerce Regression Gate

Goal: prove visual generation did not break Starter ecommerce behavior.

Browser tests:

- [ ] Home renders.
- [ ] Catalog renders.
- [ ] Product renders.
- [ ] Product link navigation works.
- [ ] Product image/gallery region renders.
- [ ] Quantity control can change.
- [ ] Add-to-cart command works through same-origin BFF.
- [ ] Cart badge updates.
- [ ] Cart page renders.
- [ ] Checkout route renders.
- [ ] Account route renders.
- [ ] Login/register shell renders according to store policy.
- [ ] Product SEO initial HTML exists.
- [ ] Browser does not call Commerce Node protected APIs directly.

Notes:

- [ ] COD test order placement can be included only when test store/env is configured.
- [ ] PayPal/Stripe production providers are not required for this MVP; sandbox can be separate later.

Output:

- [ ] `functional-commerce-report.md`

Done when:

- [ ] Generated storefront passes real browser commerce regression for Starter-supported paths.

Verification:

- [ ] Playwright tests fail on direct Commerce Node browser calls.
- [ ] Playwright tests fail when add-to-cart bypasses BFF.

## S25 - Idempotent Regeneration

Goal: allow repeated generation and later manual tuning without unexpected diffs.

Generated file manifest:

- [ ] File path.
- [ ] Ownership.
- [ ] Generator version.
- [ ] Source artifact IDs.
- [ ] Source/spec hash.
- [ ] Generated hash.
- [ ] Last generated timestamp.
- [ ] Manual edit detected flag.
- [ ] Conflict status.

Commands:

- [ ] Regenerate all generated files.
- [ ] Regenerate one page.
- [ ] Regenerate one component.
- [ ] Regenerate only CSS tokens.
- [ ] Validate without writing.
- [ ] Show conflict report.

Rules:

- [ ] No-op run must create no diff.
- [ ] Manual changes in generated files must be detected.
- [ ] Manual changes in managed files must require explicit patch plan.
- [ ] Protected files must never be modified.

Output:

- [ ] `generated-files.yaml`
- [ ] `regeneration-report.md`

Done when:

- [ ] Running the same input twice produces no unexpected file changes.

Verification:

- [ ] Idempotency test on fixture project.
- [ ] Manual edit conflict fixture test.

## S26 - Human Review And Tuning Workflow

Goal: support the expected reality that generated stores are tuned before use.

Review artifacts:

- [ ] Visual decision summary.
- [ ] Unsupported feature list.
- [ ] Hidden target feature list.
- [ ] Starter fallback list.
- [ ] Asset replacement list.
- [ ] AI inference review list.
- [ ] Manual tuning checklist.

Workflow:

- [ ] `analyze-only` creates artifacts only.
- [ ] `plan-only` creates generation plan only.
- [ ] `generate` writes generated visual output.
- [ ] `update` safely refreshes an existing generated store.
- [ ] `validate-only` runs guards and gates without generation.
- [ ] `full` runs capture, plan, generate, build, visual QA, and commerce regression.

Done when:

- [ ] Human can inspect decisions before trusting generated output.

Verification:

- [ ] `plan-only` creates no source diffs outside analysis artifacts.
- [ ] `validate-only` creates no source diffs.

## S27 - Skill Commands And Packaging

Goal: expose a predictable development-time command surface.

Commands:

- [ ] `/analyze-storefront <url>`
- [ ] `/map-storefront`
- [ ] `/generate-storefront`
- [ ] `/validate-storefront`
- [ ] `/build-storefront <url>`

Required options:

- [ ] `--name`
- [ ] `--store-key`
- [ ] `--starter`
- [ ] `--output-root`
- [ ] `--mode`
- [ ] `--force`
- [ ] `--skip-visual-qa`
- [ ] `--skip-commerce-regression`

Docs:

- [ ] README with quick start.
- [ ] Examples for one reference URL.
- [ ] Examples for multiple URLs.
- [ ] Failure troubleshooting.
- [ ] Protected file rule explanation.

Done when:

- [ ] A developer can run a full POC from docs without guessing required inputs.

Verification:

- [ ] Command help snapshot test.
- [ ] README command examples are tested or smoke-checked.

## S28 - CI And Release Gate Integration

Goal: add enough automated checks to keep StorefrontBuilder from regressing.

CI checks:

- [ ] Schema tests.
- [ ] Preflight tests.
- [ ] Protected file guard tests.
- [ ] Generation fixture tests.
- [ ] Idempotency tests.
- [ ] Isolation gate describe mode.
- [ ] Visual QA fixture smoke.
- [ ] Commerce regression fixture smoke.

Do not run by default on every normal backend PR unless cost is acceptable:

- [ ] Full external reference-site capture.
- [ ] Full visual diff against live target.
- [ ] Full payment/order browser regression.

Done when:

- [ ] CI protects contracts and generation safety without making every backend PR slow.

Verification:

- [ ] Workflow includes fast checks.
- [ ] Expensive browser checks can be run manually or nightly.

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

- [ ] Capture desktop/tablet/mobile.
- [ ] Page inventory exists.
- [ ] Design tokens exist.
- [ ] UI patterns exist.
- [ ] Behavior/responsive model exists.
- [ ] Page topology exists.
- [ ] Capability decisions exist.
- [ ] Composition manifest exists.
- [ ] Generation plan exists.
- [ ] Generated project exists.
- [ ] Generated CSS/components/pages exist.
- [ ] Asset manifest exists.
- [ ] Generated file manifest exists.
- [ ] Build passes.
- [ ] Dependency guard passes.
- [ ] Visual QA has zero Critical findings.
- [ ] Basic commerce regression passes.
- [ ] No direct browser Commerce Node calls.
- [ ] No protected file changes.
- [ ] Re-run is idempotent.

Done when:

- [ ] StorefrontBuilder can generate one usable `Storefront.{Name}` project from a reference site and prove it did not break ecommerce/security contracts.

## Deferred Beyond MVP

- [ ] Full AI Storefront Generator orchestration.
- [ ] Control Plane marketplace UI.
- [ ] Automatic module install.
- [ ] Backend plugin generation.
- [ ] Full cart/checkout/payment/order reconstruction.
- [ ] Multi-store upgrade orchestration.
- [ ] Multi-target visual blending.
- [ ] Advanced visual diff engine.
- [ ] Full legal asset management.
- [ ] React/Next skeleton generation.

## Autoplan Decision Audit Trail

| # | Phase | Decision | Classification | Principle | Rationale | Rejected |
|---|-------|----------|----------------|-----------|-----------|----------|
| 1 | S20 | Allow placeholder/manual replacement asset workflow | User direction preserved | Practical generated-store workflow | Generated store is not production-ready immediately and will be tuned. | Blocking generation on asset licensing completeness |
| 2 | S23 | Use severity-based visual QA, not pixel-perfect requirement | Auto-decided | Useful MVP gate | Pixel-perfect every detail is too expensive and not needed for first generated storefront. | Pixel-perfect blocking gate |
| 3 | S24 | Browser commerce regression must test real Starter behavior | User direction preserved | Browser tests find real faults | Smoke-only checks do not catch broken BFF/cart/checkout wiring. | Static smoke only |
| 4 | S25 | Idempotency is MVP, not later polish | Auto-decided | Safe regeneration | Generated stores will be tuned and regenerated; no-op stability is required. | One-shot generator |
| 5 | S28 | Keep expensive live visual capture out of every normal backend PR | Auto-decided | CI must stay usable | Fast safety checks should run often; heavy browser runs can be manual/nightly. | Full live visual QA on every PR |

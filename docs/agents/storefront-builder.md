# StorefrontBuilder Agent Guide

Use this guide when changing StorefrontBuilder tooling, Starter, generated storefronts, or visual reverse engineering docs.

## Required Reading

1. `AGENTS.md`
2. `docs/architecture/README.md`
3. `docs/architecture/11-storefront-builder.md`
4. `docs/visual-reverse-engineering-skill/README.md`
5. The relevant phase plan under `docs/visual-reverse-engineering-skill/`
6. Existing scripts/tests found with `rg StorefrontBuilder`

## Boundaries

StorefrontBuilder is development-time tooling. Do not add it as a production ASP.NET service, Commerce Node module, or Control Plane feature unless a new architecture decision explicitly changes that.

`BlazorShop.AI.StorefrontReverseEngineering` is a separate Phase 3A development-time executable under `tools/`. It creates reference evidence, workflow state, neutral visual-blueprint drafts, originality audit, and readiness reports under `artifacts/storefront-reverse-engineering/projects/{ProjectId}` or `obj/storefront-reverse-engineering/projects/{ProjectId}`. It must not reference production runtime projects, generated storefront roots, or Storefront V2. StorefrontBuilder generation does not consume its artifacts until a later approved phase.

Phase 3A ReverseEngineering work is evidence hardening only. Do not present it as a visual generator, do not claim AI analysis is complete, and do not treat captured assets, logos, copy, or brand-specific visuals as reusable by default. Phase 3B is where design-token extraction, semantic token normalization, section segmentation, responsive comparison, component detection, ecommerce region mapping, confidence scoring, human review, and approved StorefrontBuilder blueprint consumption may be planned.

Generated storefronts must:

- Live as disposable artifacts under `artifacts/storefront-builder/generated/{ProjectName}` for manual proof runs or `obj/storefront-builder/generated/{ProjectName}` for automated proof runs.
- Consume `BlazorShop.Storefront.Presentation` and `BlazorShop.Storefront.Components` through package boundaries when they need the full storefront application surface. Presentation composes Runtime internally, Runtime owns direct `BlazorShop.Storefront.Client` transport usage, and generated projects keep Client/Runtime package metadata for compatibility proof only.
- Use Storefront Presentation for shared App/Routes/page services/BFF/SEO/media composition. Generated projects provide views, assets, copy, feature manifests, host configuration, and Starter-derived semantic descriptors instead of recreating application logic.
- Register generated visual components as Presentation view slots; generated files must not declare `@page` routes or add route assemblies.
- Register Storefront Presentation in the generated server/BFF host with `AddStorefrontApplication()`, `UseStorefrontApplication()`, and `MapStorefrontApplication()`. Do not register Runtime directly in generated visual hosts unless a documented low-level extension explicitly reopens that boundary.
- Use `BlazorShop.Storefront.Components` only through a package boundary when reusable browser-safe contracts/headless behavior or Browser local API primitives are needed.
- Keep protected browser actions behind same-origin BFF endpoints.
- Keep review artifacts under `docs/storefront-analysis/`.
- Stay out of `BlazorShop.sln` by default.
- Keep presentation-specific CSS, assets, generated pages, visual analysis artifacts, and AI-tuned components inside the generated/custom project.
- Use generated package contracts instead of guessing Storefront API response shapes.
- Keep browser and WASM code on same-origin generated endpoints and browser-safe Components contracts/headless behavior; do not reference Runtime from browser code.
- Render product-selection semantics from Presentation events, such as price, stock, image, SKU, and GTIN labels. Do not read raw preview fields or rebuild add-to-cart/product-selection payloads in generated visual JavaScript.

Generated storefronts must not:

- Reference `BlazorShop.Storefront.V2`.
- Reference backend/core/API projects.
- Reference `BlazorShop.Web.SharedV2` or `Web.SharedV2`.
- Call `api/commerce/*`, `api/control-plane/*`, or legacy `api/internal/*` from browser code.
- Copy Storefront V2 transport internals.
- Copy or import retired `BlazorShop.Storefront.Components.Features` wrappers. Generated storefronts must use shared `Contracts`, `Headless`, and `Browser` primitives and render project-local DOM/CSS.
- Use Components `Features` wrappers as the generated/custom storefront presentation source.
- Generate route/BFF/SEO/media application logic from scratch when Storefront Presentation already owns the shared surface.
- Declare `@page` in generated visual files.
- Write store-specific output into `BlazorShop.Storefront.Starter`.

## Protected Areas

Treat these as contract surfaces:

- `BlazorShop.Storefront.Client` generated transport and DTOs.
- `contracts/storefront/storefront.openapi.json` canonical Storefront OpenAPI contract and `scripts/qa/run-storefront-client-regeneration-gate.ps1` package release gate.
- `BlazorShop.Storefront.Runtime` security, error, capability, and client-registration primitives.
- `BlazorShop.Storefront.Presentation` App/Routes/page services/BFF/SEO/media composition and view-slot contracts.
- `BlazorShop.Storefront.Components` browser-safe contracts/headless/browser primitives package; retired `Features` wrappers must not be reintroduced without a new architecture decision.
- Generated storefront `StorefrontPackageVersions.props`.
- Generated storefront `starter-generation.contract.yaml`.
- Generated file manifests under `docs/storefront-analysis/`.
- Same-origin BFF endpoints and token/session handling.

Change protected areas only when the phase explicitly requires it and tests/gates are updated in the same commit.

## Validation

Use focused validation for StorefrontBuilder changes:

```powershell
dotnet test BlazorShop.Tests.V2\BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~StorefrontBuilder"
.\scripts\qa\run-storefront-builder-generated-proof.ps1 -ProofLevel Structure
.\scripts\qa\run-storefront-builder-generated-proof.ps1 -ProofLevel FoundationFunctionalFast
.\scripts\qa\run-storefront-builder-full-proof-with-fixture.ps1 -Describe
.\scripts\qa\run-storefront-builder-full-proof-with-fixture.ps1
.\scripts\qa\run-storefront-builder-regeneration-gate.ps1
.\scripts\qa\run-storefront-client-regeneration-gate.ps1
.\tools\BlazorShop.AI.StorefrontBuilder\validate-storefront.ps1 -ProjectRoot artifacts/storefront-builder/generated/BlazorShop.Storefront.GeneratedProof -Name BlazorShop.Storefront.GeneratedProof -StoreKey sample
.\scripts\qa\run-storefront-builder-isolation-gate.ps1 -ProjectRoot artifacts/storefront-builder/generated/BlazorShop.Storefront.GeneratedProof -Name BlazorShop.Storefront.GeneratedProof
```

Use focused validation for StorefrontReverseEngineering changes:

```powershell
dotnet build tools\BlazorShop.AI.StorefrontReverseEngineering\BlazorShop.AI.StorefrontReverseEngineering.csproj
dotnet test tools\BlazorShop.AI.StorefrontReverseEngineering\tests\BlazorShop.AI.StorefrontReverseEngineering.Tests\BlazorShop.AI.StorefrontReverseEngineering.Tests.csproj
dotnet run --project tools\BlazorShop.AI.StorefrontReverseEngineering\BlazorShop.AI.StorefrontReverseEngineering.csproj -- --help
powershell -ExecutionPolicy Bypass -File scripts\qa\run-storefront-reverse-engineering-phase3a-gate.ps1
```

Install .NET Playwright Chromium once before browser tests or the hardening gate:

```powershell
dotnet build tools\BlazorShop.AI.StorefrontReverseEngineering\BlazorShop.AI.StorefrontReverseEngineering.csproj
.\tools\BlazorShop.AI.StorefrontReverseEngineering\bin\Debug\net10.0\playwright.ps1 install chromium
```

The hardening gate uses a local HTTP fixture, validates readiness output, scans production boundaries, scans for prototype fallback markers, and runs StorefrontBuilder compatibility smoke. It should be the closure proof when ReverseEngineering runtime evidence, workflow state, schemas, interactions, or handoff docs change.

ReverseEngineering browser capture uses .NET Playwright for non-fixture HTTP pages, `FixtureReferenceBrowser` for local `file://` fixtures, and `SyntheticReferenceBrowser` for `.test` hosts. The StorefrontBuilder Node Playwright scripts remain StorefrontBuilder capture/QA baselines only and are not a supported ReverseEngineering runtime adapter.

Phase 3A final closure guarantees evidence extraction before native screenshot capture, explicit screenshot quality/fallback decisions, stitched capture only with segment artifacts, readiness validation over schema/quality/evidence depth/correlation/originality/latest-run state, and `inspect` output sourced from `reports/readiness-report.json`. Phase 3B agents should consume this foundation and focus on design-token extraction, semantic normalization, section segmentation, component detection, ecommerce mapping, confidence scoring, human review, and approved blueprint consumption.

Use `Structure` proof for package/boundary checks plus generated lifecycle proof: post-regeneration build, deterministic no-op regeneration, and manual-edit conflict reporting. Use `run-storefront-builder-regeneration-gate.ps1` for CI-friendly ownership/regeneration checks that do not require live Commerce Node data. Use `FoundationFunctionalFast` for PR-safe generated browser behavior checks. Use `run-storefront-builder-full-proof-with-fixture.ps1` before release closure or when fixture-backed live generated behavior changes; it starts and tears down the V2 fixture runtime itself. Call `run-storefront-builder-generated-proof.ps1 -ProofLevel FoundationFunctionalFull` directly only when Commerce Node fixture data is already running and verified. When GitHub Actions are disabled during development, local gate output is the closure evidence; run the StorefrontBuilder workflow manually with `run_browser_gates=true` after Actions are re-enabled.

Regeneration must come from a fresh candidate generated from current Starter/template inputs. Do not assume update mode copies the target project and patches it. `-WhatIf` runs the same candidate/planning pipeline as apply mode and exits before target writes; read the `WhatIf report:` console path for the stable report under `.regeneration-reports/` or the explicit `-WhatIfReportPath`. `SFB_KEEP_REGENERATION_CANDIDATE_ARTIFACTS=1` is debug-only for inspecting temporary planner state, not normal report access. Use `-Scope foundation` only for explicit platform metadata/package/starter contract updates.

When generated page behavior changes, run browser QA against the generated storefront:

```powershell
node tools\BlazorShop.AI.StorefrontBuilder\scripts\qa\run-visual-qa.mjs --base-url http://127.0.0.1:18991 --project-root artifacts/storefront-builder/generated/BlazorShop.Storefront.GeneratedProof
node tools\BlazorShop.AI.StorefrontBuilder\scripts\qa\run-commerce-regression.mjs --base-url http://127.0.0.1:18991 --project-root artifacts/storefront-builder/generated/BlazorShop.Storefront.GeneratedProof
```

Browser QA reports are written under the generated artifact. Do not commit the generated artifact unless a phase explicitly asks for tracked evidence.

## Documentation

When StorefrontBuilder behavior changes, update:

- `docs/architecture/11-storefront-builder.md`
- `docs/visual-reverse-engineering-skill/reference.md`
- The relevant how-to/tutorial/explanation page.
- The relevant phase checklist or QA artifact.

Keep historical todo files as implementation evidence; do not rewrite completed phase history unless correcting a factual error.

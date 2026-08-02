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

`BlazorShop.AI.StorefrontReverseEngineering` is a separate development-time executable under `tools/`. It creates reference evidence, workflow state, neutral visual-blueprint drafts, reviewed visual/ecommerce mappings, Phase 3C final handoff packages, Phase 3E portable handoff validation/dry-run evidence, originality audit, and readiness reports under `artifacts/storefront-reverse-engineering/projects/{ProjectId}` or `obj/storefront-reverse-engineering/projects/{ProjectId}`. It must not reference production runtime projects, generated storefront roots, or Storefront V2. StorefrontBuilder may consume portable `analysis/agent-handoff/*` packages only through the approved Phase 4 preflight, generation-plan compiler, Starter-based handoff project generation, constrained visual write recorder, visual QA, repair, and regeneration path.

Phase 3A ReverseEngineering work is evidence hardening only. Phase 3B adds visual analysis, ecommerce mapping, confidence review, and Visual Blueprint v1. Phase 3C adds the strict `analysis/agent-handoff/*` package and final handoff readiness. Phase 3D hardens final closure proof for that package. Phase 3E proves the package is portable through handoff-local artifacts, schema/hash validation, typed reference containment, reviewed slot provenance, copied-package validation, dry-run loading, negative portability mutations, and a no-skip final gate. Do not present any of those phases as visual generation, do not claim captured assets, logos, copy, or brand-specific visuals are reusable by default, and do not bypass the Phase 4 preflight/generation-plan path when wiring StorefrontBuilder consumption.

Phase 4 may read only `analysis/agent-handoff/*` and schemas as input after the Phase 3E final runtime gate passes on a clean unchanged `HEAD`. It must fail unless `analysis/agent-handoff/handoff-readiness.json` passed, must not reinterpret raw reference evidence unless explicitly running a new ReverseEngineering pass, must not write into Starter, and must not change protected Storefront runtime behavior. Use `build-storefront.ps1 -Mode preflight-only|plan-only|generate|full -HandoffRoot <path>`, `validate-handoff`, `inspect-handoff`, or the read-only dry-run loader as portable handoff surfaces; do not read source project folders, raw captures, `analysis/pages/*`, `analysis/resolved/*`, `presentation-catalog/*`, `review/*`, or `reports/*` as fallback inputs.

Use Phase 4 visual skills only after StorefrontBuilder has produced a handoff-generated project and `docs/storefront-analysis/agent-task-package/manifest.json` exists. The canonical skill instructions live at `tools/BlazorShop.AI.Visual/skills/storefront-visual-plan/SKILL.md`, `tools/BlazorShop.AI.Visual/skills/storefront-visual-implement/SKILL.md`, and `tools/BlazorShop.AI.Visual/skills/storefront-visual-qa/SKILL.md`.

Visual skills may read `tools/BlazorShop.AI.Visual/references/*`, `tools/BlazorShop.AI.Visual/schemas/*`, `tools/BlazorShop.AI.Visual/examples/*`, the generated project's `docs/storefront-analysis/generation-plan.json`, `agent-task-package/*`, generated file manifests, task-package-listed visual source files, and browser evidence from `run-visual-qa.mjs`. They must not read raw ReverseEngineering captures, draft visual-blueprint artifacts, Storefront V2 source, backend/API/core source, or Starter as a fallback implementation source.

Visual skills may edit only allowed generated visual files in the generated project and may write generated-project-local reports such as `visual-plan.json`, `visual-implementation-checklist.todo.md`, `visual-checkpoints/*`, `visual-implementation-report.json`, and `visual-qa-report.json`. Preserve Presentation descriptors and do not add routes, transport, auth, SEO, business behavior, or protected-file changes. After visual edits, run `record-agent-visual-writes.mjs` before browser QA or closure gates.

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
.\scripts\qa\run-storefront-phase4-mvp-gate.ps1 -GeneratedProjectRoot <generated-project-root> -FixtureRoot <fixture-root> -HandoffRoot <portable-handoff-root> -CommandTimeoutSeconds 600
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\qa\run-storefront-phase4-final-closure-gate.ps1 -CommandTimeoutSeconds 900
dotnet test tools\BlazorShop.AI.StorefrontReverseEngineering\tests\BlazorShop.AI.StorefrontReverseEngineering.Tests\BlazorShop.AI.StorefrontReverseEngineering.Tests.csproj --no-restore --filter "FullyQualifiedName~StorefrontBuilderHandoffPreflightTests|FullyQualifiedName~StorefrontBuilderHandoffGenerationPlanTests|FullyQualifiedName~StorefrontBuilderHandoffProjectGenerationTests|FullyQualifiedName~StorefrontBuilderAgentTaskPackageTests|FullyQualifiedName~StorefrontBuilderHandoffBoundaryValidationTests|FullyQualifiedName~StorefrontBuilderHandoffVisualQaTests|FullyQualifiedName~StorefrontBuilderHandoffRepairLoopTests|FullyQualifiedName~StorefrontBuilderHandoffRegenerationSafetyTests" --blame-hang --blame-hang-timeout 5m
```

Use focused validation for StorefrontReverseEngineering changes:

```powershell
dotnet build tools\BlazorShop.AI.StorefrontReverseEngineering\BlazorShop.AI.StorefrontReverseEngineering.csproj
dotnet test tools\BlazorShop.AI.StorefrontReverseEngineering\tests\BlazorShop.AI.StorefrontReverseEngineering.Tests\BlazorShop.AI.StorefrontReverseEngineering.Tests.csproj
dotnet run --project tools\BlazorShop.AI.StorefrontReverseEngineering\BlazorShop.AI.StorefrontReverseEngineering.csproj -- --help
powershell -ExecutionPolicy Bypass -File scripts\qa\run-storefront-reverse-engineering-phase3a-gate.ps1
powershell -ExecutionPolicy Bypass -File scripts\qa\run-storefront-reverse-engineering-phase3b-gate.ps1
powershell -ExecutionPolicy Bypass -File scripts\qa\run-storefront-reverse-engineering-phase3c-final-handoff-gate.ps1
powershell -ExecutionPolicy Bypass -File scripts\qa\run-storefront-reverse-engineering-phase3d-final-closure-gate.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\qa\run-storefront-reverse-engineering-phase3e-final-closure-gate.ps1
dotnet run --project tools\BlazorShop.AI.StorefrontReverseEngineering\BlazorShop.AI.StorefrontReverseEngineering.csproj -- validate-handoff --handoff-root <path> --schema-root tools/BlazorShop.AI.StorefrontReverseEngineering/Schemas
dotnet run --project tools\BlazorShop.AI.StorefrontReverseEngineering\BlazorShop.AI.StorefrontReverseEngineering.csproj -- inspect-handoff --handoff-root <path> --schema-root tools/BlazorShop.AI.StorefrontReverseEngineering/Schemas
```

Install .NET Playwright Chromium once before browser tests or the hardening gate:

```powershell
dotnet build tools\BlazorShop.AI.StorefrontReverseEngineering\BlazorShop.AI.StorefrontReverseEngineering.csproj
.\tools\BlazorShop.AI.StorefrontReverseEngineering\bin\Debug\net10.0\playwright.ps1 install chromium
```

The hardening gate uses a local HTTP fixture, validates readiness output, scans production boundaries, scans for prototype fallback markers, and runs StorefrontBuilder compatibility smoke. It should be the closure proof when ReverseEngineering runtime evidence, workflow state, schemas, interactions, or handoff docs change.

ReverseEngineering browser capture uses .NET Playwright for non-fixture HTTP pages, `FixtureReferenceBrowser` for local `file://` fixtures, and `SyntheticReferenceBrowser` for `.test` hosts. The StorefrontBuilder Node Playwright scripts remain StorefrontBuilder capture/QA baselines only and are not a supported ReverseEngineering runtime adapter.

Phase 3A final closure guarantees evidence extraction before native screenshot capture, explicit screenshot quality/fallback decisions, stitched capture only with segment artifacts, readiness validation over schema/quality/evidence depth/correlation/originality/latest-run state, and `inspect` output sourced from `reports/readiness-report.json`. Phase 3B agents should consume this foundation and focus on design-token extraction, semantic normalization, section segmentation, component detection, ecommerce mapping, confidence scoring, human review, and reviewed blueprint assembly. Phase 3D keeps StorefrontBuilder consumption disabled while proving that the future Phase 4 input is only the reviewed `analysis/agent-handoff/*` package. Phase 3E keeps generation consumption disabled while proving that package is self-contained, portable, schema-backed, hash-backed, and loadable without the original project root; Phase 4 consumes that package only through the controlled StorefrontBuilder path.

Use `Structure` proof for package/boundary checks plus generated lifecycle proof: post-regeneration build, deterministic no-op regeneration, and manual-edit conflict reporting. Use `run-storefront-builder-regeneration-gate.ps1` for CI-friendly ownership/regeneration checks that do not require live Commerce Node data. Use `FoundationFunctionalFast` for PR-safe generated browser behavior checks. Use `run-storefront-builder-full-proof-with-fixture.ps1` before release closure or when fixture-backed live generated behavior changes; it starts and tears down the V2 fixture runtime itself. Call `run-storefront-builder-generated-proof.ps1 -ProofLevel FoundationFunctionalFull` directly only when Commerce Node fixture data is already running and verified. When GitHub Actions are disabled during development, local gate output is the closure evidence; run the StorefrontBuilder workflow manually with `run_browser_gates=true` after Actions are re-enabled.

Non-handoff regeneration must come from a fresh candidate generated from current Starter/template inputs. Handoff-generated regeneration preserves stored metadata, copies the target into a temporary candidate, reapplies stored `docs/storefront-analysis/generation-plan.json`, and rejects handoff package/readiness or Starter contract drift before planning. `-WhatIf` runs the same candidate/planning pipeline as apply mode and exits before target writes; read the `WhatIf report:` console path for the stable report under `.regeneration-reports/` or the explicit `-WhatIfReportPath`. `SFB_KEEP_REGENERATION_CANDIDATE_ARTIFACTS=1` is debug-only for inspecting temporary planner state, not normal report access. Use `-Scope foundation` only for explicit platform metadata/package/starter contract updates.

When generated page behavior changes, run browser QA against the generated storefront:

```powershell
node tools\BlazorShop.AI.StorefrontBuilder\scripts\qa\run-visual-qa.mjs --base-url http://127.0.0.1:18991 --project-root artifacts/storefront-builder/generated/BlazorShop.Storefront.GeneratedProof
node tools\BlazorShop.AI.StorefrontBuilder\scripts\qa\run-commerce-regression.mjs --base-url http://127.0.0.1:18991 --project-root artifacts/storefront-builder/generated/BlazorShop.Storefront.GeneratedProof
```

For handoff skeleton or agent visual proof with seeded/mock fixture pages:

```powershell
node tools\BlazorShop.AI.StorefrontBuilder\scripts\qa\run-visual-qa.mjs --project-root <generated-project-root> --fixture-root <fixture-root> --screenshot-root obj/storefront-builder/visual-qa-screens --allow-planned-placeholders
node tools\BlazorShop.AI.StorefrontBuilder\scripts\generate\record-agent-visual-writes.mjs --project-root <generated-project-root> --written-files <comma-separated-generated-visual-paths>
node tools\BlazorShop.AI.StorefrontBuilder\scripts\qa\repair-visual-generation.mjs --project-root <generated-project-root> --failure-report <report.md> --max-attempts 2
```

Browser QA reports are written under the generated artifact. Do not commit the generated artifact unless a phase explicitly asks for tracked evidence. Before closing Phase 4 visual work, the MVP gate must pass on a handoff-generated project and the final closure gate must pass from a clean unchanged `HEAD`; GitHub Actions are not required for that local closure.

## Documentation

When StorefrontBuilder behavior changes, update:

- `docs/architecture/11-storefront-builder.md`
- `docs/visual-reverse-engineering-skill/reference.md`
- The relevant how-to/tutorial/explanation page.
- The relevant phase checklist or QA artifact.

Keep historical todo files as implementation evidence; do not rewrite completed phase history unless correcting a factual error.

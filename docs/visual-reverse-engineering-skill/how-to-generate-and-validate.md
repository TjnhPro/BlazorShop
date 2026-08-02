# How To Generate And Validate A Storefront

Use this workflow when creating or updating a generated storefront from the Storefront Starter.

## Prerequisites

- .NET SDK from `global.json`.
- PowerShell.
- Node dependencies installed in `tools/BlazorShop.AI.StorefrontBuilder` when browser QA is required.
- Current Storefront API client/runtime/presentation/components packages build successfully.

Install Node dependencies:

```powershell
Push-Location tools\BlazorShop.AI.StorefrontBuilder
npm ci
Pop-Location
```

## Generate

Create a generated storefront:

```powershell
.\tools\BlazorShop.AI.StorefrontBuilder\build-storefront.ps1 `
  -Url https://reference.example `
  -Name Demo `
  -StoreKey sample `
  -OutputRoot artifacts/storefront-builder/generated `
  -Mode generate
```

Use a full project name only when the folder must already include the `BlazorShop.Storefront.` prefix:

```powershell
.\tools\BlazorShop.AI.StorefrontBuilder\build-storefront.ps1 `
  -Name BlazorShop.Storefront.Demo `
  -StoreKey sample `
  -OutputRoot artifacts/storefront-builder/generated `
  -Mode generate
```

## Update

Regenerate all generated visual/composition output:

```powershell
.\tools\BlazorShop.AI.StorefrontBuilder\regenerate-storefront.ps1 `
  -ProjectRoot artifacts/storefront-builder/generated/BlazorShop.Storefront.Demo `
  -Scope all
```

Regenerate a narrower target:

```powershell
.\tools\BlazorShop.AI.StorefrontBuilder\regenerate-storefront.ps1 `
  -ProjectRoot artifacts/storefront-builder/generated/BlazorShop.Storefront.Demo `
  -Scope page `
  -Target Home
```

Use `-Scope conflicts` before manual edits to generated files when you need to confirm idempotency state.

Preview before applying:

```powershell
.\tools\BlazorShop.AI.StorefrontBuilder\regenerate-storefront.ps1 `
  -ProjectRoot artifacts/storefront-builder/generated/BlazorShop.Storefront.Demo `
  -Scope all `
  -WhatIf
```

`-WhatIf` runs the same fresh-candidate planning pipeline as apply mode and exits before copying target changes. Read the `WhatIf report:` path printed by the command; by default it is written under the output root `.regeneration-reports/` folder and contains create/update/conflict/obsolete/platform metadata actions. Use `-WhatIfReportPath <path>` only when you need a custom approved report path under the output report folder, repo `obj`, or `artifacts/storefront-builder`.

Require validation and build after applying:

```powershell
.\tools\BlazorShop.AI.StorefrontBuilder\regenerate-storefront.ps1 `
  -ProjectRoot artifacts/storefront-builder/generated/BlazorShop.Storefront.Demo `
  -Scope all `
  -ValidateAfterApply `
  -BuildAfterApply
```

Refresh platform metadata, package compatibility versions, and the copied Starter contract only with the explicit foundation scope:

```powershell
.\tools\BlazorShop.AI.StorefrontBuilder\regenerate-storefront.ps1 `
  -ProjectRoot artifacts/storefront-builder/generated/BlazorShop.Storefront.Demo `
  -Scope foundation `
  -ValidateAfterApply `
  -BuildAfterApply
```

Manual edits to generated/managed files are not overwritten automatically. They are recorded in `docs/storefront-analysis/generated-files.yaml` and summarized by `docs/storefront-analysis/regeneration-report.md`; resolve the file intentionally, then rerun `-Scope conflicts`.

Generated `metadata.yaml` and `generated-files.yaml` share the StorefrontBuilder `generatorVersion` from `tools/BlazorShop.AI.StorefrontBuilder/version.json`. Validation fails if those artifact versions drift.

ReverseEngineering Phase 3A can create reference evidence and `analysis/visual-blueprint.draft.json`. Phase 3B can add reviewed visual analysis and Visual Blueprint v1. Phase 3C can assemble a strict final handoff package under `analysis/agent-handoff/`. Phase 3D is the final correctness and no-skip closure proof for that package. Phase 3E makes the package portable with handoff-local artifacts, schema requirements, hashes, reference containment, portable validation commands, dry-run loading, isolated copy proof, and a final clean-HEAD gate. Generated storefront commands do not consume those artifacts yet. Treat Phase 3C/3D/3E output as future handoff evidence until a later StorefrontBuilder phase explicitly enables consumption.

For future Phase 4 planning, the approved input root is only `analysis/agent-handoff/*` plus the registered schemas. A Phase 4 consumer must not read draft artifacts such as `analysis/pages/*`, raw `captures/*`, `analysis/visual-blueprint.draft.json`, `analysis/visual-blueprint.v1.draft.json`, or any unresolved reviewed-source file as generation input.

To run the Phase 3A fixture evidence workflow:

```powershell
$fixture = (Resolve-Path 'tools\BlazorShop.AI.StorefrontReverseEngineering\tests\BlazorShop.AI.StorefrontReverseEngineering.Tests\Fixtures\static-storefront.html').Path
$fixtureUrl = [Uri]::new($fixture).AbsoluteUri
dotnet run --project tools\BlazorShop.AI.StorefrontReverseEngineering\BlazorShop.AI.StorefrontReverseEngineering.csproj -- run --url $fixtureUrl --name FixtureDemo --output-root obj/storefront-reverse-engineering/projects --no-ai --force
```

Inspect final handoff readiness:

```powershell
dotnet run --project tools\BlazorShop.AI.StorefrontReverseEngineering\BlazorShop.AI.StorefrontReverseEngineering.csproj -- inspect --project obj/storefront-reverse-engineering/projects/fixturedemo
```

Validate or inspect a copied portable handoff package without the original project root:

```powershell
dotnet run --project tools\BlazorShop.AI.StorefrontReverseEngineering\BlazorShop.AI.StorefrontReverseEngineering.csproj -- validate-handoff --handoff-root obj/storefront-reverse-engineering/projects/fixturedemo --schema-root tools/BlazorShop.AI.StorefrontReverseEngineering/Schemas
dotnet run --project tools\BlazorShop.AI.StorefrontReverseEngineering\BlazorShop.AI.StorefrontReverseEngineering.csproj -- inspect-handoff --handoff-root obj/storefront-reverse-engineering/projects/fixturedemo --schema-root tools/BlazorShop.AI.StorefrontReverseEngineering/Schemas
```

Apply review decisions and rerun final handoff validation:

```powershell
dotnet run --project tools\BlazorShop.AI.StorefrontReverseEngineering\BlazorShop.AI.StorefrontReverseEngineering.csproj -- resume --project obj/storefront-reverse-engineering/projects/fixturedemo --force-step apply-review-decisions
dotnet run --project tools\BlazorShop.AI.StorefrontReverseEngineering\BlazorShop.AI.StorefrontReverseEngineering.csproj -- resume --project obj/storefront-reverse-engineering/projects/fixturedemo --force-step validate-agent-handoff-readiness
```

Run the Phase 3C local gate:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\qa\run-storefront-reverse-engineering-phase3c-final-handoff-gate.ps1
```

Run the Phase 3D final closure gate only from a clean working tree:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\qa\run-storefront-reverse-engineering-phase3d-final-closure-gate.ps1
```

The Phase 3D gate has no skip flags. It records clean-tree proof, tested SHA, final `HEAD`, focused fixture results, boundary scans, and the final handoff readiness path `analysis/agent-handoff/handoff-readiness.json`.

Run the Phase 3E final closure gate only after the final candidate commit and a clean working tree:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/qa/run-storefront-reverse-engineering-phase3e-final-closure-gate.ps1
```

Phase 3E remains in progress until the final Phase 3E runtime gate passes on this same clean HEAD. The ignored gate report is authoritative final proof; tracked docs must not require a post-gate source commit.

The Phase 3E gate is non-recursive: it does not call the Phase 3D gate as a child process. It restores/builds once, runs later tests with `--no-build --no-restore`, relies on the grouped closure proof bucket for CLI/browser/portable coverage, uses shared positive and portable baselines, runs one StorefrontBuilder plan-only smoke, runs one canonical boundary scan, records timeout/process/slow-step telemetry, and cleans transient success artifacts. GitHub Actions evidence remains intentionally outside this development closure path while Actions are disabled.

Phase 4 may read only `analysis/agent-handoff/*` and schemas as future input. It must fail unless `analysis/agent-handoff/handoff-readiness.json` passed, must not reinterpret raw captures unless running a new ReverseEngineering pass, must not write into Starter, and must not change StorefrontBuilder generation without its own approved plan.

## Validate

Run the static gate:

```powershell
.\tools\BlazorShop.AI.StorefrontBuilder\validate-storefront.ps1 `
  -ProjectRoot artifacts/storefront-builder/generated/BlazorShop.Storefront.Demo `
  -Name BlazorShop.Storefront.Demo `
  -StoreKey sample
```

Run tests:

```powershell
dotnet test BlazorShop.Tests.V2\BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~StorefrontBuilder"
```

Run isolation:

```powershell
.\scripts\qa\run-storefront-builder-isolation-gate.ps1 -ProjectRoot artifacts/storefront-builder/generated/BlazorShop.Storefront.Demo -Name BlazorShop.Storefront.Demo
```

Run the CI-friendly regeneration ownership gate:

```powershell
.\scripts\qa\run-storefront-builder-regeneration-gate.ps1
```

Run the canonical generated proof:

```powershell
.\scripts\qa\run-storefront-builder-generated-proof.ps1 -ProofLevel Structure
```

Run the PR-safe browser proof:

```powershell
.\scripts\qa\run-storefront-builder-generated-proof.ps1 -ProofLevel FoundationFunctionalFast
```

Run the self-contained full fixture proof before release closure:

```powershell
.\scripts\qa\run-storefront-builder-full-proof-with-fixture.ps1 -Describe
.\scripts\qa\run-storefront-builder-full-proof-with-fixture.ps1
```

## Compatibility Rules

- Generated storefronts use Runtime-backed Presentation contexts and BFF contracts instead of direct generated-client references in visual source.
- Generated storefronts use `BlazorShop.Storefront.Presentation` for shared App/Routes/page services/BFF/SEO/media composition and provide project-local views/assets/copy.
- Generated storefronts register project-local views as Presentation view slots; generated files must not declare `@page` routes or add route assemblies.
- Generated storefronts use Storefront Presentation for server-side storefront application registration. Presentation composes Runtime internally for generated-client registration, store context, capability/error primitives, and BFF integration primitives.
- Generated storefronts may use `BlazorShop.Storefront.Components` contracts/headless behavior and Browser local API primitives for reusable browser-safe UI components; generated project-local components are allowed for store-specific presentation.
- `BlazorShop.Storefront.Components.Features` is retired; generated storefronts should consume `Contracts`, `Headless`, and `Browser` primitives and own their visual templates locally.
- `BlazorShop.Storefront.{Name}` owns generated markup, generated CSS, store-specific assets, generated pages, and analysis artifacts.
- StorefrontBuilder may replace product card/grid/gallery/purchase/cart/checkout/account visual templates without changing shared behavior contracts.
- Protected browser actions go through same-origin BFF endpoints.
- Do not generate route/BFF/SEO/media application logic from scratch when Presentation already owns it.
- Do not reference `BlazorShop.Storefront.V2`, backend/API/core projects, Control Plane Web, or `BlazorShop.Web.SharedV2`/`Web.SharedV2`.
- Do not use Storefront V2 visual markup as the generated/custom storefront presentation source.
- Do not copy Components `Features` wrappers as generated visual templates or stable presentation contracts.
- Do not guess API response shapes; use generated package contracts through Runtime, Presentation BFF contracts, or explicitly documented host-local extensions.

## Browser QA

Start the generated storefront:

```powershell
dotnet run --no-build --project artifacts/storefront-builder/generated/BlazorShop.Storefront.Demo/BlazorShop.Storefront.Demo.csproj --urls http://127.0.0.1:18991
```

Run visual and commerce checks from another PowerShell session:

```powershell
node tools\BlazorShop.AI.StorefrontBuilder\scripts\qa\run-visual-qa.mjs --base-url http://127.0.0.1:18991 --project-root artifacts/storefront-builder/generated/BlazorShop.Storefront.Demo
node tools\BlazorShop.AI.StorefrontBuilder\scripts\qa\run-commerce-regression.mjs --base-url http://127.0.0.1:18991 --project-root artifacts/storefront-builder/generated/BlazorShop.Storefront.Demo
```

Review the resulting reports under the generated artifact's `docs/storefront-analysis/`. Do not commit the generated artifact by default.

## Before Commit

Check these points before promoting generated storefront output or committing tooling changes:

- `BlazorShop.Storefront.Starter` has no store-specific visual output.
- `BlazorShop.Storefront.Starter` owns neutral visual templates and does not copy Storefront V2 visual components.
- The generated project references `BlazorShop.Storefront.Presentation` and `BlazorShop.Storefront.Components` as direct packages, and keeps `BlazorShop.Storefront.Runtime` plus `BlazorShop.Storefront.Client` version metadata because Presentation/Runtime own the application and generated transport dependencies.
- Browser code uses same-origin BFF routes for protected actions.
- Generated visual files contain no `@page` route directives.
- Required analysis artifacts exist.
- Static gate, focused tests, and isolation gate pass.
- Regeneration ownership gate passes when generated ownership, manifest, or regeneration behavior changed.
- Generated proof `Structure` passes before release closure because it recreates the proof, builds it, validates package/reference boundaries, proves safe regeneration, proves no-op determinism, and proves manual-edit conflict reporting.
- Generated proof `FoundationFunctionalFast` passes for PR-safe browser action behavior.
- Self-contained full fixture proof passes before release closure; it starts V2 fixture runtime, verifies store/category/product/page/payment data, runs `FoundationFunctionalFull`, collects reports, and tears down.
- Storefront client regeneration gate passes before package proof if the canonical Storefront contract or generated client changed.
- Browser QA reports are current when page behavior changed.
- Generated storefront artifacts remain out of `BlazorShop.sln` unless a separate architecture decision promotes them.

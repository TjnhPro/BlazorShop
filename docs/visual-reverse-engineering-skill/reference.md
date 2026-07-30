# StorefrontBuilder Reference

## Tool Layout

| Path | Purpose |
| --- | --- |
| `tools/BlazorShop.AI.StorefrontBuilder/build-storefront.ps1` | Main orchestration command. |
| `tools/BlazorShop.AI.StorefrontBuilder/validate-storefront.ps1` | Static validation entrypoint for generated storefronts. |
| `tools/BlazorShop.AI.StorefrontBuilder/regenerate-storefront.ps1` | Regenerates generated CSS, pages, components, manifests, or conflict checks. |
| `tools/BlazorShop.AI.StorefrontBuilder/scripts/capture/` | Playwright capture and page discovery helpers. |
| `tools/BlazorShop.AI.StorefrontBuilder/scripts/generate/` | Generation, planning, token extraction, topology, capability, and manifest scripts. |
| `tools/BlazorShop.AI.StorefrontBuilder/scripts/validate/` | Static validation scripts and guardrails. |
| `tools/BlazorShop.AI.StorefrontBuilder/scripts/qa/` | Browser visual QA and commerce regression runners. |
| `tools/BlazorShop.AI.StorefrontReverseEngineering/Skills/reverse-engineering-skills.json` | Phase 3A reverse-engineering skill catalog manifest. It documents deterministic, hybrid, and review-required steps; it is not an executable skill runtime. |
| `scripts/qa/run-storefront-builder-generated-proof.ps1` | Canonical generated proof workflow. |
| `scripts/qa/run-storefront-builder-full-proof-with-fixture.ps1` | Self-contained CI/manual/release wrapper for full fixture proof. |
| `scripts/qa/run-storefront-builder-regeneration-gate.ps1` | CI-friendly regeneration ownership gate. |
| `scripts/qa/run-storefront-builder-isolation-gate.ps1` | Generated storefront build/package/reference isolation gate. |

## Generated Project Shape

Generated storefront projects use this naming pattern:

```text
artifacts/storefront-builder/generated/BlazorShop.Storefront.{Name}
obj/storefront-builder/generated/BlazorShop.Storefront.{Name}
```

Required generated project files include:

- `{ProjectName}.csproj`
- `StorefrontPackageVersions.props`
- `starter-generation.contract.yaml`
- `docs/storefront-analysis/metadata.yaml`
- `docs/storefront-analysis/asset-manifest.yaml`
- `docs/storefront-analysis/generated-files.yaml`

Generated proof projects are ignored artifacts, not committed source projects. The canonical proof name for local validation is `BlazorShop.Storefront.GeneratedProof`.

Generated/custom storefront compatibility rules:

- Use Runtime-backed Presentation contexts and BFF contracts instead of direct generated-client references in generated visual source.
- Treat `contracts/storefront/storefront.openapi.json` as the canonical Storefront API contract behind the Runtime-owned `BlazorShop.Storefront.Client` package; run `scripts/qa/run-storefront-client-regeneration-gate.ps1` before package proof when the contract or generated client changes.
- Use `BlazorShop.Storefront.Presentation` package contracts for shared App/Routes/page services/BFF/SEO/media composition.
- Register generated visual components as Presentation view slots; generated source must not declare `@page` routes or add route assemblies.
- Use Storefront Presentation for server-side storefront application registration. Presentation composes Runtime internally for generated-client registration, store context, capability/error primitives, and BFF integration primitives.
- Use `BlazorShop.Storefront.Components` contracts/headless behavior and Browser local API primitives only when reusable browser-safe UI components are needed; local presentation components can stay inside the generated storefront.
- Treat `BlazorShop.Storefront.Components.Features` as retired. Normal generation consumes `Contracts`, `Headless`, and `Browser` primitives and emits project-local visual templates.
- `BlazorShop.Storefront.{Name}` owns generated markup, generated CSS, store-specific assets, pages, and analysis artifacts.
- StorefrontBuilder may replace product card/grid/gallery/purchase/cart/checkout/account visual templates without changing shared behavior contracts.
- Route protected browser actions through same-origin BFF endpoints.
- Do not generate route/BFF/SEO/media application logic from scratch when Presentation already owns it.
- Never reference `BlazorShop.Storefront.V2`, backend/API/core projects, Control Plane Web, or `BlazorShop.Web.SharedV2`/`Web.SharedV2`.
- Do not use Storefront V2 visual markup as the generated/custom storefront presentation source.
- Do not copy Components `Features` wrappers as generated visual templates or stable presentation contracts.
- Do not guess API response shapes; use generated package contracts through Runtime, Presentation BFF contracts, or explicitly documented host-local extensions.

## Main Command

```powershell
.\tools\BlazorShop.AI.StorefrontBuilder\build-storefront.ps1 `
  -Url https://reference.example `
  -Name Demo `
  -StoreKey sample `
  -Mode validate-only
```

Parameters:

| Parameter | Default | Notes |
| --- | --- | --- |
| `Url` | `https://reference.example` | Reference storefront URL used for analysis artifacts. |
| `Name` | `Demo` | Normalized to `BlazorShop.Storefront.{Name}` unless the full project name is already supplied. |
| `StoreKey` | `sample` | Storefront API route scope for generated configuration. |
| `OutputRoot` | `artifacts/storefront-builder/generated` | Generated artifact root. |
| `Mode` | `validate-only` | One of `analyze-only`, `plan-only`, `generate`, `update`, `validate-only`, `full`. |
| `Force` | off | Allows project generation to overwrite an existing generated target when the generation script permits it. |
| `SkipVisualQa` | off | Suppresses visual QA runner reporting in `full` mode. |
| `SkipCommerceRegression` | off | Suppresses commerce regression runner reporting in `full` mode. |

Modes:

| Mode | Result |
| --- | --- |
| `analyze-only` | Runs `write-review-artifacts.mjs`. |
| `plan-only` | Runs `plan-generation-files.mjs --dry-run`. |
| `generate` | Creates a new storefront project and writes review artifacts. |
| `update` | Runs regeneration with `Scope all`. |
| `validate-only` | Runs `validate-storefront.ps1`. |
| `full` | Generates, writes artifacts, validates, and prints browser QA runner names. |

## Regeneration Command

```powershell
.\tools\BlazorShop.AI.StorefrontBuilder\regenerate-storefront.ps1 `
  -ProjectRoot artifacts/storefront-builder/generated/BlazorShop.Storefront.GeneratedProof `
  -Scope all
```

Scopes:

| Scope | Behavior |
| --- | --- |
| `all` | Generates a fresh candidate from current Starter/template inputs, plans all generated/managed visual file actions, applies safe changes, updates generated manifest, and checks idempotency. |
| `page` | Plans and applies page/composition output for the optional `Target`. |
| `component` | Plans and applies component/composition output for the optional `Target`. |
| `css` | Plans and applies generated visual foundation CSS. |
| `foundation` | Explicitly refreshes generated platform metadata, package compatibility metadata, and the copied Starter contract. |
| `validate` | Runs the static storefront validation gate. |
| `conflicts` | Runs idempotency/conflict validation. |

Use `-WhatIf` to run the same fresh-candidate planning pipeline as apply mode without copying changed files into the generated target. The console prints a stable `WhatIf report:` path, summary counts, meaningful `filePath: action - reason` lines, and conflict next-action guidance when needed. By default the report is written outside the target under `{OutputRoot}/.regeneration-reports/{ProjectName}-{operationId}.md`; `-WhatIfReportPath <path>` can redirect it to an approved report path under the output report folder, repo `obj`, or `artifacts/storefront-builder`. The report records create, update, skip unchanged, skip user-owned, skip protected, manual-edit conflict, platform metadata update, and obsolete candidate actions.

StorefrontBuilder generator provenance comes from `tools/BlazorShop.AI.StorefrontBuilder/version.json`. Generated `metadata.yaml` and `generated-files.yaml` entries must agree on the same `generatorVersion`.

Use `-ValidateAfterApply` and `-BuildAfterApply` when a regeneration must prove the generated project still validates and builds before the change is accepted.

Refresh platform metadata intentionally:

```powershell
.\tools\BlazorShop.AI.StorefrontBuilder\regenerate-storefront.ps1 `
  -ProjectRoot artifacts/storefront-builder/generated/BlazorShop.Storefront.GeneratedProof `
  -Scope foundation `
  -ValidateAfterApply `
  -BuildAfterApply
```

## Validation Commands

Static gate:

```powershell
.\tools\BlazorShop.AI.StorefrontBuilder\validate-storefront.ps1 `
  -ProjectRoot artifacts/storefront-builder/generated/BlazorShop.Storefront.GeneratedProof `
  -Name BlazorShop.Storefront.GeneratedProof `
  -StoreKey sample
```

Isolation gate:

```powershell
.\scripts\qa\run-storefront-builder-isolation-gate.ps1 -ProjectRoot artifacts/storefront-builder/generated/BlazorShop.Storefront.GeneratedProof -Name BlazorShop.Storefront.GeneratedProof
```

Canonical structure proof:

```powershell
.\scripts\qa\run-storefront-builder-generated-proof.ps1 -ProofLevel Structure
```

Canonical fast foundation functional proof:

```powershell
.\scripts\qa\run-storefront-builder-generated-proof.ps1 -ProofLevel FoundationFunctionalFast
```

Canonical full foundation functional proof:

```powershell
.\scripts\qa\run-storefront-builder-generated-proof.ps1 -ProofLevel FoundationFunctionalFull
```

Self-contained full fixture proof:

```powershell
.\scripts\qa\run-storefront-builder-full-proof-with-fixture.ps1 -Describe
.\scripts\qa\run-storefront-builder-full-proof-with-fixture.ps1
```

CI-friendly regeneration ownership gate:

```powershell
.\scripts\qa\run-storefront-builder-regeneration-gate.ps1
```

`Structure` generates/restores/builds the proof project, runs static validation, runs isolation, runs the shared visual consumer boundary validator, proves post-regeneration build, proves deterministic no-op regeneration, and proves manual-edit conflict reporting. `run-storefront-builder-regeneration-gate.ps1` separately proves no-op determinism, scoped CSS/page/component updates, real `-WhatIf` planning, platform metadata update, manual generated-file conflicts, user-owned preservation, protected-file rejection, obsolete-file reporting, and rollback without live Commerce Node data. `FoundationFunctionalFast` uses mocked same-origin Presentation BFF routes in Playwright and writes `fast-foundation-functional-report.md` under the generated artifact. `FoundationFunctionalFull` verifies fixture data, starts the generated storefront in Development, runs visual smoke QA and commerce-regression network checks, and writes `visual-qa-report.md` plus `functional-commerce-report.md` under the generated artifact. Use `run-storefront-builder-full-proof-with-fixture.ps1` for scheduled/manual/release validation because it starts Docker dependencies and the local V2 fixture runtime, checks health and fixture endpoints, runs the full proof, writes `full-proof-with-fixture-report.md`, and tears down services. `FoundationFunctional` and `-RunBrowserQa` remain compatibility aliases for the full proof.

Generated storefront validation must fail when generated source declares `@page`, imports `BlazorShop.Storefront.Components.Features`, or recreates protected Presentation-owned application logic; normal generation consumes Presentation plus `Contracts`, `Headless`, and `Browser` primitives and renders project-local DOM.

Focused test filter:

```powershell
dotnet test BlazorShop.Tests.V2\BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~StorefrontBuilder"
```

## Browser QA

Install Node dependencies once:

```powershell
Push-Location tools\BlazorShop.AI.StorefrontBuilder
npm ci
Pop-Location
```

Run the generated storefront before browser QA:

```powershell
dotnet run --no-build --project artifacts/storefront-builder/generated/BlazorShop.Storefront.GeneratedProof/BlazorShop.Storefront.GeneratedProof.csproj --urls http://127.0.0.1:18991
```

Then run:

```powershell
node tools\BlazorShop.AI.StorefrontBuilder\scripts\qa\run-visual-qa.mjs --base-url http://127.0.0.1:18991 --project-root artifacts/storefront-builder/generated/BlazorShop.Storefront.GeneratedProof --category-slug apparel --product-slug qa-simple-product-100
node tools\BlazorShop.AI.StorefrontBuilder\scripts\qa\run-commerce-regression.mjs --base-url http://127.0.0.1:18991 --project-root artifacts/storefront-builder/generated/BlazorShop.Storefront.GeneratedProof --category-slug apparel --product-slug qa-simple-product-100 --page-slug customer-service
```

Browser QA writes `visual-qa-report.md` and `functional-commerce-report.md` under the generated artifact. Do not commit generated proof output by default.

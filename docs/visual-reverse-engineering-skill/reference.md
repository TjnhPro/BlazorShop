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
| `scripts/qa/run-storefront-builder-isolation-gate.ps1` | Generated storefront build/package/reference isolation gate. |

## Generated Project Shape

Generated storefront projects use this naming pattern:

```text
BlazorShop.PresentationV2/BlazorShop.Storefront.{Name}
```

Required generated project files include:

- `{ProjectName}.csproj`
- `StorefrontPackageVersions.props`
- `starter-generation.contract.yaml`
- `docs/storefront-analysis/metadata.yaml`
- `docs/storefront-analysis/asset-manifest.yaml`
- `docs/storefront-analysis/generated-files.yaml`

Current generated proof projects:

- `BlazorShop.PresentationV2/BlazorShop.Storefront.Sample`
- `BlazorShop.PresentationV2/BlazorShop.Storefront.BuilderDemo`

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
  -ProjectRoot BlazorShop.PresentationV2/BlazorShop.Storefront.BuilderDemo `
  -Scope all
```

Scopes:

| Scope | Behavior |
| --- | --- |
| `all` | Applies visual foundation, applies composition, updates generated manifest, and checks idempotency. |
| `page` | Applies page/composition output for the optional `Target`. |
| `component` | Applies component/composition output for the optional `Target`. |
| `css` | Applies generated visual foundation CSS. |
| `validate` | Runs the static storefront validation gate. |
| `conflicts` | Runs idempotency/conflict validation. |

Use `-WhatIf` to print the intended scope and target without writing.

## Validation Commands

Static gate:

```powershell
.\tools\BlazorShop.AI.StorefrontBuilder\validate-storefront.ps1 `
  -ProjectRoot BlazorShop.PresentationV2/BlazorShop.Storefront.BuilderDemo `
  -Name BlazorShop.Storefront.BuilderDemo `
  -StoreKey builder-demo
```

Isolation gate:

```powershell
.\scripts\qa\run-storefront-builder-isolation-gate.ps1 -Name BlazorShop.Storefront.BuilderDemo
```

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
dotnet run --no-build --project BlazorShop.PresentationV2/BlazorShop.Storefront.BuilderDemo/BlazorShop.Storefront.BuilderDemo.csproj --urls http://127.0.0.1:18991
```

Then run:

```powershell
node tools\BlazorShop.AI.StorefrontBuilder\scripts\qa\run-visual-qa.mjs --base-url http://127.0.0.1:18991 --project-root BlazorShop.PresentationV2/BlazorShop.Storefront.BuilderDemo
node tools\BlazorShop.AI.StorefrontBuilder\scripts\qa\run-commerce-regression.mjs --base-url http://127.0.0.1:18991 --project-root BlazorShop.PresentationV2/BlazorShop.Storefront.BuilderDemo
```

Commit updated `visual-qa-report.md` and `functional-commerce-report.md` when behavior changes.

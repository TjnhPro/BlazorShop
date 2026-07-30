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

`-WhatIf` runs the same fresh-candidate planning pipeline as apply mode and exits before copying target changes. Read the `WhatIf report:` path printed by the command; by default it is written under the output root `.regeneration-reports/` folder and contains create/update/conflict/obsolete/platform metadata actions.

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

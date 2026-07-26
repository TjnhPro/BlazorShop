# How To Generate And Validate A Storefront

Use this workflow when creating or updating a generated storefront from the Storefront Starter.

## Prerequisites

- .NET SDK from `global.json`.
- PowerShell.
- Node dependencies installed in `tools/BlazorShop.AI.StorefrontBuilder` when browser QA is required.
- Current Storefront API client/runtime packages build successfully.

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

## Compatibility Rules

- Generated storefronts use `BlazorShop.Storefront.Client` package contracts for Storefront API transport and DTOs.
- Generated storefronts use `BlazorShop.Storefront.Runtime` for server-side generated-client registration, store context, capability/error primitives, and BFF integration primitives.
- Generated storefronts may use `BlazorShop.Storefront.Components` contracts/headless behavior and Browser local API primitives for reusable browser-safe UI components; generated project-local components are allowed for store-specific presentation.
- `BlazorShop.Storefront.Components/Features` contains CSS-neutral compatibility wrappers only; do not copy them as generated visual templates or treat them as stable presentation contracts.
- `BlazorShop.Storefront.{Name}` owns generated markup, generated CSS, store-specific assets, generated pages, and analysis artifacts.
- StorefrontBuilder may replace product card/grid/gallery/purchase/cart/checkout/account visual templates without changing shared behavior contracts.
- Protected browser actions go through same-origin BFF endpoints.
- Do not reference `BlazorShop.Storefront.V2`, backend/API/core projects, Control Plane Web, or `BlazorShop.Web.SharedV2`/`Web.SharedV2`.
- Do not use Storefront V2 visual markup as the generated/custom storefront presentation source.
- Do not copy Components `Features` wrappers as generated visual templates or stable presentation contracts.
- Do not guess API response shapes; use generated package contracts.

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
- The generated project references `BlazorShop.Storefront.Client` and `BlazorShop.Storefront.Runtime` as packages, and uses `BlazorShop.Storefront.Components` only as a package when shared browser-safe UI components are needed.
- Browser code uses same-origin BFF routes for protected actions.
- Required analysis artifacts exist.
- Static gate, focused tests, and isolation gate pass.
- Storefront client regeneration gate passes before package proof if the canonical Storefront contract or generated client changed.
- Browser QA reports are current when page behavior changed.
- Generated storefront artifacts remain out of `BlazorShop.sln` unless a separate architecture decision promotes them.

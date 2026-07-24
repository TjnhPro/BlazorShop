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
- The generated project references `BlazorShop.Storefront.Client` and `BlazorShop.Storefront.Runtime` as packages.
- Browser code uses same-origin BFF routes for protected actions.
- Required analysis artifacts exist.
- Static gate, focused tests, and isolation gate pass.
- Browser QA reports are current when page behavior changed.
- Generated storefront artifacts remain out of `BlazorShop.sln` unless a separate architecture decision promotes them.

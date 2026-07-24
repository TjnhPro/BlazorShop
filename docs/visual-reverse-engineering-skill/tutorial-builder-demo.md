# Tutorial: Validate BuilderDemo

This tutorial validates the committed StorefrontBuilder proof project.

## 1. Restore Tool Dependencies

```powershell
Push-Location tools\BlazorShop.AI.StorefrontBuilder
npm ci
Pop-Location
```

## 2. Run Static Validation

```powershell
.\tools\BlazorShop.AI.StorefrontBuilder\validate-storefront.ps1 `
  -ProjectRoot BlazorShop.PresentationV2/BlazorShop.Storefront.BuilderDemo `
  -Name BlazorShop.Storefront.BuilderDemo `
  -StoreKey builder-demo
```

Expected result:

```text
StorefrontBuilder static validation gate passed for BlazorShop.PresentationV2/BlazorShop.Storefront.BuilderDemo.
```

## 3. Run Focused Tests

```powershell
dotnet test BlazorShop.Tests.V2\BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~StorefrontBuilder"
```

Expected result: all StorefrontBuilder tests pass.

## 4. Run Isolation Gate

```powershell
.\scripts\qa\run-storefront-builder-isolation-gate.ps1 -Name BlazorShop.Storefront.BuilderDemo
```

Expected result:

```text
StorefrontBuilder isolation gate passed for BlazorShop.Storefront.BuilderDemo.
```

## 5. Run Browser QA

Start BuilderDemo:

```powershell
dotnet run --no-build --project BlazorShop.PresentationV2/BlazorShop.Storefront.BuilderDemo/BlazorShop.Storefront.BuilderDemo.csproj --urls http://127.0.0.1:18991
```

Run visual QA:

```powershell
node tools\BlazorShop.AI.StorefrontBuilder\scripts\qa\run-visual-qa.mjs --base-url http://127.0.0.1:18991 --project-root BlazorShop.PresentationV2/BlazorShop.Storefront.BuilderDemo
```

Run commerce regression:

```powershell
node tools\BlazorShop.AI.StorefrontBuilder\scripts\qa\run-commerce-regression.mjs --base-url http://127.0.0.1:18991 --project-root BlazorShop.PresentationV2/BlazorShop.Storefront.BuilderDemo
```

Expected reports:

- `BlazorShop.PresentationV2/BlazorShop.Storefront.BuilderDemo/docs/storefront-analysis/visual-qa-report.md`
- `BlazorShop.PresentationV2/BlazorShop.Storefront.BuilderDemo/docs/storefront-analysis/functional-commerce-report.md`

## 6. Inspect Evidence

Review:

- `metadata.yaml` for source URL and project metadata.
- `asset-manifest.yaml` for generated asset evidence.
- `generated-files.yaml` for generated-file ownership.
- `mvp-poc-report.md` for the current POC summary.

These artifacts explain what was generated and what was validated.

# BlazorShop.AI.StorefrontReverseEngineering

Development-time executable for Phase 3A visual evidence capture and neutral blueprint drafting.

This tool is independent from StorefrontBuilder generation. It writes reverse-engineering project state under `artifacts/storefront-reverse-engineering/projects/{ProjectId}` for manual work or `obj/storefront-reverse-engineering/projects/{ProjectId}` for automated tests.

```powershell
dotnet run --project tools\BlazorShop.AI.StorefrontReverseEngineering\BlazorShop.AI.StorefrontReverseEngineering.csproj -- --help
```

## Local Fixture Workflow

```powershell
$fixture = (Resolve-Path 'tools\BlazorShop.AI.StorefrontReverseEngineering\tests\BlazorShop.AI.StorefrontReverseEngineering.Tests\Fixtures\static-storefront.html').Path
$fixtureUrl = [Uri]::new($fixture).AbsoluteUri
dotnet run --project tools\BlazorShop.AI.StorefrontReverseEngineering\BlazorShop.AI.StorefrontReverseEngineering.csproj -- run --url $fixtureUrl --name FixtureDemo --output-root obj/storefront-reverse-engineering/projects --no-ai --force
```

Step-by-step:

```powershell
dotnet run --project tools\BlazorShop.AI.StorefrontReverseEngineering\BlazorShop.AI.StorefrontReverseEngineering.csproj -- init --url $fixtureUrl --name FixtureDemo --output-root obj/storefront-reverse-engineering/projects --force
dotnet run --project tools\BlazorShop.AI.StorefrontReverseEngineering\BlazorShop.AI.StorefrontReverseEngineering.csproj -- discover --project obj/storefront-reverse-engineering/projects/fixturedemo
dotnet run --project tools\BlazorShop.AI.StorefrontReverseEngineering\BlazorShop.AI.StorefrontReverseEngineering.csproj -- capture --project obj/storefront-reverse-engineering/projects/fixturedemo
dotnet run --project tools\BlazorShop.AI.StorefrontReverseEngineering\BlazorShop.AI.StorefrontReverseEngineering.csproj -- analyze --project obj/storefront-reverse-engineering/projects/fixturedemo --no-ai
dotnet run --project tools\BlazorShop.AI.StorefrontReverseEngineering\BlazorShop.AI.StorefrontReverseEngineering.csproj -- validate --project obj/storefront-reverse-engineering/projects/fixturedemo
```

## Outputs

- `project.json` and `configuration.json`
- `discovery/site-profile.json`, `discovery/reconnaissance.json`, and `discovery/capture-plan.json`
- `captures/{pageId}/{viewportId}` screenshot, DOM, styles, boxes, assets, manifest, quality report, and normalized evidence
- `analysis/page-topology.draft.json`, page/component specifications, `visual-blueprint.draft.json`, and `originality-audit.json`
- `reports/originality-audit.md` and `reports/readiness-report.md`

## Limitations

Phase 3A does not generate Razor, CSS, or StorefrontBuilder output. It does not crawl a full site, bypass authentication, execute checkout/account/payment flows, or declare reference assets safe to reuse. External AI providers are optional and no provider is required for the rule-based blueprint draft.

## Browser Setup

Automated tests use the deterministic local fixture browser and do not require internet access. Manual capture can wrap the existing StorefrontBuilder Node Playwright capture script through `NodePlaywrightReferenceBrowser`; install its dependencies before manual browser runs:

```powershell
Push-Location tools\BlazorShop.AI.StorefrontBuilder
npm install
npx playwright install chromium
Pop-Location
```

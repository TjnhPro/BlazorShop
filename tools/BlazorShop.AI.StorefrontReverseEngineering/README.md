# BlazorShop.AI.StorefrontReverseEngineering

Development-time executable for Phase 3A visual evidence capture and neutral blueprint drafting.

This tool is independent from StorefrontBuilder generation. StorefrontReverseEngineering records rendered reference evidence, workflow state, validation reports, originality notes, and neutral `visual-blueprint.draft.json` files. StorefrontBuilder remains the generator/regenerator for Blazor storefront projects and does not consume ReverseEngineering artifacts in Phase 3A.

Reverse-engineering project state is written under `artifacts/storefront-reverse-engineering/projects/{ProjectId}` for manual work or `obj/storefront-reverse-engineering/projects/{ProjectId}` for automated tests. Generated storefronts continue to live under `artifacts/storefront-builder/generated/{ProjectName}` or `obj/storefront-builder/generated/{ProjectName}`.

```powershell
dotnet run --project tools\BlazorShop.AI.StorefrontReverseEngineering\BlazorShop.AI.StorefrontReverseEngineering.csproj -- --help
```

## Commands

Primary commands:

- `init --url <url> --name <name> [--output-root <path>] [--force]` creates a project.
- `discover --project <path>` writes site profile, reconnaissance, and capture plan artifacts.
- `capture --project <path>` captures configured page and viewport evidence.
- `analyze --project <path> [--no-ai]` writes rule-based draft topology, specifications, blueprint, and originality artifacts.
- `validate --project <path>` validates schemas, capture quality, references, workflow state, blueprint links, and originality restrictions.
- `inspect --project <path>` prints current project status, latest run, blueprint path, and readiness report path.
- `run --url <url> --name <name> [--output-root <path>] [--no-ai] [--force] [--run-id <id>]` executes the full sequential workflow.
- `resume --project <path> [--run-id <id>] [--force-step <step>]` resumes or reruns a workflow step plus downstream steps.

`--force` on `init` or `run` deletes only the resolved single ReverseEngineering project root under the approved output root. It refuses generated storefront roots.

## Local Fixture Workflow

```powershell
$fixture = (Resolve-Path 'tools\BlazorShop.AI.StorefrontReverseEngineering\tests\BlazorShop.AI.StorefrontReverseEngineering.Tests\Fixtures\static-storefront.html').Path
$fixtureUrl = [Uri]::new($fixture).AbsoluteUri
dotnet run --project tools\BlazorShop.AI.StorefrontReverseEngineering\BlazorShop.AI.StorefrontReverseEngineering.csproj -- run --url $fixtureUrl --name FixtureDemo --output-root obj/storefront-reverse-engineering/projects --no-ai --force
dotnet run --project tools\BlazorShop.AI.StorefrontReverseEngineering\BlazorShop.AI.StorefrontReverseEngineering.csproj -- inspect --project obj/storefront-reverse-engineering/projects/fixturedemo
```

Step-by-step:

```powershell
dotnet run --project tools\BlazorShop.AI.StorefrontReverseEngineering\BlazorShop.AI.StorefrontReverseEngineering.csproj -- init --url $fixtureUrl --name FixtureDemo --output-root obj/storefront-reverse-engineering/projects --force
dotnet run --project tools\BlazorShop.AI.StorefrontReverseEngineering\BlazorShop.AI.StorefrontReverseEngineering.csproj -- discover --project obj/storefront-reverse-engineering/projects/fixturedemo
dotnet run --project tools\BlazorShop.AI.StorefrontReverseEngineering\BlazorShop.AI.StorefrontReverseEngineering.csproj -- capture --project obj/storefront-reverse-engineering/projects/fixturedemo
dotnet run --project tools\BlazorShop.AI.StorefrontReverseEngineering\BlazorShop.AI.StorefrontReverseEngineering.csproj -- analyze --project obj/storefront-reverse-engineering/projects/fixturedemo --no-ai
dotnet run --project tools\BlazorShop.AI.StorefrontReverseEngineering\BlazorShop.AI.StorefrontReverseEngineering.csproj -- validate --project obj/storefront-reverse-engineering/projects/fixturedemo
dotnet run --project tools\BlazorShop.AI.StorefrontReverseEngineering\BlazorShop.AI.StorefrontReverseEngineering.csproj -- resume --project obj/storefront-reverse-engineering/projects/fixturedemo --force-step capture
```

## Outputs

- `project.json` and `configuration.json`
- `discovery/site-profile.json`, `discovery/reconnaissance.json`, and `discovery/capture-plan.json`
- `captures/{pageId}/{viewportId}` screenshot, DOM, styles, boxes, assets, manifest, quality report, and normalized evidence
- `analysis/page-topology.draft.json`, page/component specifications, `visual-blueprint.draft.json`, and `originality-audit.json`
- `runs/{runId}.json`
- `reports/originality-audit.md` and `reports/readiness-report.md`

The readiness report is the release handoff check. A ready report means produced artifacts are present, schema-valid, linked by capture correlation IDs, quality-checked, tied to the latest workflow run, and constrained by originality/provenance findings. It does not mean the reference design has been fully interpreted or that assets can be reused.

## Hardening Gate

Run the Phase 3A hardening gate after changing the tool, schemas, workflow, browser runtime, interaction capture, or StorefrontBuilder handoff docs:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\qa\run-storefront-reverse-engineering-phase3a-gate.ps1
```

The gate builds the tool, checks local Playwright Chromium installation, runs fast tests, runs real Playwright fixture tests, executes a no-AI CLI workflow against the local HTML fixture, validates readiness/inspect output, scans production boundaries, scans for prototype fallback markers, and runs StorefrontBuilder compatibility smoke. It uses local fixtures and does not require an external website.

## Limitations

Phase 3A does not generate Razor, CSS, or StorefrontBuilder output. It does not perform full design token extraction, ecommerce mapping, component generation, or StorefrontBuilder blueprint consumption. It does not crawl a full site, bypass authentication, execute checkout/account/payment flows, or declare reference assets safe to reuse. External AI providers are optional and no provider is required for the rule-based blueprint draft.

Originality and provenance checks are conservative. Captured media, logos, copy, and brand-specific visual material are reference-only by default until a human or later approved workflow clears reuse.

## Phase 3B Handoff

Phase 3B should start from stable Phase 3A runtime evidence, not from patched prototype behavior. Deferred work includes design-token extraction, semantic token normalization, section segmentation, responsive comparison, component detection, ecommerce region mapping, confidence scoring, human review workflow, and approved StorefrontBuilder consumption of `analysis/visual-blueprint.draft.json`.

## Browser Setup

Fast unit/schema tests use deterministic fixture capture and do not require internet access. Real browser integration tests use .NET Playwright against a local HTTP fixture and require Chromium to be installed once on the machine:

```powershell
dotnet build tools\BlazorShop.AI.StorefrontReverseEngineering\BlazorShop.AI.StorefrontReverseEngineering.csproj
.\tools\BlazorShop.AI.StorefrontReverseEngineering\bin\Debug\net10.0\playwright.ps1 install chromium
dotnet test tools\BlazorShop.AI.StorefrontReverseEngineering\tests\BlazorShop.AI.StorefrontReverseEngineering.Tests\BlazorShop.AI.StorefrontReverseEngineering.Tests.csproj --filter "Playwright|EndToEnd"
```

Manual capture can also wrap the existing StorefrontBuilder Node Playwright capture script through `NodePlaywrightReferenceBrowser`; install its dependencies before manual Node bridge runs:

```powershell
Push-Location tools\BlazorShop.AI.StorefrontBuilder
npm install
npx playwright install chromium
Pop-Location
```

# BlazorShop.AI.StorefrontBuilder

Development-time visual reverse-engineering tooling for generated BlazorShop storefronts.

This folder is intentionally outside production runtime projects. It does not contain a production `.csproj`, does not get referenced by V2 runtime projects, and is used only by developers or agents running StorefrontBuilder workflows.

Target generated projects use:

```text
BlazorShop.PresentationV2/BlazorShop.Storefront.{Name}
```

## Quick Start

```powershell
.\tools\BlazorShop.AI.StorefrontBuilder\build-storefront.ps1 `
  -Url https://reference.example `
  -Name Demo `
  -StoreKey sample `
  -Mode full `
  -SkipVisualQa `
  -SkipCommerceRegression
```

## Commands

- `/analyze-storefront <url>`: capture and analyze reference evidence without writing generated source.
- `/map-storefront`: map target visuals to Starter slots and backend-supported capabilities.
- `/generate-storefront`: create or update `BlazorShop.Storefront.{Name}` from Starter plus generated presentation.
- `/validate-storefront`: run static validation, dependency guards, asset checks, and generation safety checks.
- `/build-storefront <url>`: run the full POC workflow through generation, validation, build, and optional browser QA.

## Required Options

- `--name`: safe generated storefront suffix or full `BlazorShop.Storefront.{Name}`.
- `--store-key`: route-scoped store key used by the generated storefront.
- `--starter`: source Starter project path.
- `--output-root`: generated project root, normally `BlazorShop.PresentationV2`.
- `--mode`: `analyze-only`, `plan-only`, `generate`, `update`, `validate-only`, or `full`.
- `--force`: replace deterministic generated output when explicitly requested.
- `--skip-visual-qa`: skip live Playwright visual QA.
- `--skip-commerce-regression`: skip live Playwright commerce regression.

## Examples

Single reference URL:

```powershell
.\tools\BlazorShop.AI.StorefrontBuilder\build-storefront.ps1 -Url https://reference.example -Name Demo -StoreKey sample -Mode full
```

Multiple reference URLs are analyzed as evidence inputs before mapping:

```powershell
.\tools\BlazorShop.AI.StorefrontBuilder\build-storefront.ps1 -Url "https://reference.example,https://reference.example/product/sku" -Name Demo -StoreKey sample -Mode analyze-only
```

## Troubleshooting

- If preflight fails, verify `-Name`, `-StoreKey`, Starter contract, package versions, and output root.
- If validation fails with protected-file errors, inspect `generation-plan.yaml` before writing files.
- If visual QA cannot run, start the generated storefront locally and rerun the Playwright scripts under `scripts/qa`.
- If commerce regression fails on checkout/payment, confirm the test store and payment sandbox are configured; production PayPal/Stripe are outside the MVP gate.

## Protected Files

StorefrontBuilder may generate presentation surfaces, CSS, analysis artifacts, and explicitly managed fallback styling. It must not modify Starter BFF transport, return URL security, Storefront.Client generated code, Storefront.Runtime, or package version contracts.

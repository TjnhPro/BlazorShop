# StorefrontBuilder Agent Guide

Use this guide when changing StorefrontBuilder tooling, Starter, generated storefronts, or visual reverse engineering docs.

## Required Reading

1. `AGENTS.md`
2. `docs/architecture/README.md`
3. `docs/architecture/11-storefront-builder.md`
4. `docs/visual-reverse-engineering-skill/README.md`
5. The relevant phase plan under `docs/visual-reverse-engineering-skill/`
6. Existing scripts/tests found with `rg StorefrontBuilder`

## Boundaries

StorefrontBuilder is development-time tooling. Do not add it as a production ASP.NET service, Commerce Node module, or Control Plane feature unless a new architecture decision explicitly changes that.

Generated storefronts must:

- Live as disposable artifacts under `artifacts/storefront-builder/generated/{ProjectName}` for manual proof runs or `obj/storefront-builder/generated/{ProjectName}` for automated proof runs.
- Consume `BlazorShop.Storefront.Client` and `BlazorShop.Storefront.Runtime` through package boundaries.
- Keep protected browser actions behind same-origin BFF endpoints.
- Keep review artifacts under `docs/storefront-analysis/`.
- Stay out of `BlazorShop.sln` by default.

Generated storefronts must not:

- Reference `BlazorShop.Storefront.V2`.
- Reference backend/core/API projects.
- Call `api/commerce/*`, `api/control-plane/*`, or legacy `api/internal/*` from browser code.
- Copy Storefront V2 transport internals.
- Write store-specific output into `BlazorShop.Storefront.Starter`.

## Protected Areas

Treat these as contract surfaces:

- `BlazorShop.Storefront.Client` generated transport and DTOs.
- `BlazorShop.Storefront.Runtime` security, error, capability, and client-registration primitives.
- Generated storefront `StorefrontPackageVersions.props`.
- Generated storefront `starter-generation.contract.yaml`.
- Generated file manifests under `docs/storefront-analysis/`.
- Same-origin BFF endpoints and token/session handling.

Change protected areas only when the phase explicitly requires it and tests/gates are updated in the same commit.

## Validation

Use focused validation for StorefrontBuilder changes:

```powershell
dotnet test BlazorShop.Tests.V2\BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~StorefrontBuilder"
.\scripts\qa\run-storefront-builder-generated-proof.ps1
.\tools\BlazorShop.AI.StorefrontBuilder\validate-storefront.ps1 -ProjectRoot artifacts/storefront-builder/generated/BlazorShop.Storefront.GeneratedProof -Name BlazorShop.Storefront.GeneratedProof -StoreKey sample
.\scripts\qa\run-storefront-builder-isolation-gate.ps1 -ProjectRoot artifacts/storefront-builder/generated/BlazorShop.Storefront.GeneratedProof -Name BlazorShop.Storefront.GeneratedProof
```

When generated page behavior changes, run browser QA against the generated storefront:

```powershell
node tools\BlazorShop.AI.StorefrontBuilder\scripts\qa\run-visual-qa.mjs --base-url http://127.0.0.1:18991 --project-root artifacts/storefront-builder/generated/BlazorShop.Storefront.GeneratedProof
node tools\BlazorShop.AI.StorefrontBuilder\scripts\qa\run-commerce-regression.mjs --base-url http://127.0.0.1:18991 --project-root artifacts/storefront-builder/generated/BlazorShop.Storefront.GeneratedProof
```

Browser QA reports are written under the generated artifact. Do not commit the generated artifact unless a phase explicitly asks for tracked evidence.

## Documentation

When StorefrontBuilder behavior changes, update:

- `docs/architecture/11-storefront-builder.md`
- `docs/visual-reverse-engineering-skill/reference.md`
- The relevant how-to/tutorial/explanation page.
- The relevant phase checklist or QA artifact.

Keep historical todo files as implementation evidence; do not rewrite completed phase history unless correcting a factual error.

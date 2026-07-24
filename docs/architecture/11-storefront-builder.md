# StorefrontBuilder Architecture

StorefrontBuilder is development-time tooling for visual reverse engineering and generated storefront preparation. It is not a production service, not a Commerce Node extension, and not a runtime plugin system.

## Ownership

| Area | Owner | Responsibility |
| --- | --- | --- |
| Storefront API contracts | `BlazorShop.PresentationV2/BlazorShop.Storefront.Client` | Generated Storefront API transport and DTOs from Commerce Node Storefront OpenAPI. |
| Neutral runtime package | `BlazorShop.PresentationV2/BlazorShop.Storefront.Runtime` | Store context, generated-client registration, capability reading, normalized errors, and BFF-safe result primitives. |
| Neutral skeleton | `BlazorShop.PresentationV2/BlazorShop.Storefront.Starter` | Template source for generated storefronts. It stays reusable and store-neutral. |
| Deterministic proof | `BlazorShop.PresentationV2/BlazorShop.Storefront.Sample` | Generated sample storefront proving Starter can build and run from package-style boundaries. |
| Committed POC | `BlazorShop.PresentationV2/BlazorShop.Storefront.BuilderDemo` | Current generated StorefrontBuilder proof with analysis artifacts and QA reports. |
| Builder tooling | `tools/BlazorShop.AI.StorefrontBuilder` | Capture, analysis, generation, regeneration, validation, and browser QA scripts. |
| Isolation gate | `scripts/qa/run-storefront-builder-isolation-gate.ps1` | Verifies generated storefronts consume Client/Runtime as packages and avoid forbidden project references. |

Generated storefronts live only under:

```text
BlazorShop.PresentationV2/BlazorShop.Storefront.{Name}
```

The storefront name must be normalized before it is used as a folder, project name, namespace segment, or file prefix. Unsafe names must fail before files are created.

## Boundary Model

StorefrontBuilder follows the existing Storefront API and BFF model:

```text
Generated storefront SSR
  -> BlazorShop.Storefront.Client
      -> CommerceNode.API api/storefront/stores/{storeKey}/*

Generated browser or WASM features
  -> same-origin generated storefront /api/*
      -> BlazorShop.Storefront.Client
          -> CommerceNode.API api/storefront/stores/{storeKey}/*
```

Generated storefronts must not:

- Reference `BlazorShop.Storefront.V2`.
- Reference `BlazorShop.Application`, `BlazorShop.Domain`, `BlazorShop.Infrastructure`, `BlazorShop.CommerceNode.API`, or `BlazorShop.ControlPlane.API`.
- Call Commerce Node, Commerce Admin, Control Plane, or legacy `api/internal/*` routes directly from browser code.
- Copy Storefront V2 transport internals, backend DTOs, credentials, or business rules.
- Mutate `BlazorShop.Storefront.Starter` with store-specific CSS, assets, pages, analysis artifacts, or AI-tuned components.

## Source Order

When visual evidence and backend capability do not agree, decisions follow this order:

1. Commerce Node Storefront OpenAPI and `BlazorShop.Storefront.Client` contracts.
2. Starter generation/runtime contract.
3. Backend capability state.
4. Starter feature manifest and protected-file rules.
5. Visual evidence captured from the reference storefront.
6. AI inference recorded explicitly when evidence is incomplete.

## Generated Artifacts

Each generated storefront keeps reviewable artifacts under:

```text
docs/storefront-analysis/
```

Required artifacts:

- `metadata.yaml`
- `asset-manifest.yaml`
- `generated-files.yaml`

Current review and QA artifacts:

- `review-summary.md`
- `regeneration-report.md`
- `visual-qa-report.md`
- `functional-commerce-report.md`
- `mvp-poc-report.md`

These files are source evidence for review. Do not treat them as disposable build output.

## Entrypoints

Primary builder command:

```powershell
.\tools\BlazorShop.AI.StorefrontBuilder\build-storefront.ps1 -Url https://reference.example -Name Demo -StoreKey sample -Mode validate-only
```

Supported modes:

| Mode | Behavior |
| --- | --- |
| `analyze-only` | Writes review artifacts from the reference URL for the target generated project. |
| `plan-only` | Produces a dry-run generation plan. |
| `generate` | Creates a generated storefront project from Starter and writes analysis artifacts. |
| `update` | Runs regeneration for the generated storefront. |
| `validate-only` | Runs the static validation gate for the generated storefront. |
| `full` | Generates, writes artifacts, validates, and reports visual/commerce QA script entrypoints. |

Regeneration command:

```powershell
.\tools\BlazorShop.AI.StorefrontBuilder\regenerate-storefront.ps1 -ProjectRoot BlazorShop.PresentationV2/BlazorShop.Storefront.BuilderDemo -Scope all
```

Supported scopes are `all`, `page`, `component`, `css`, `validate`, and `conflicts`.

Static validation command:

```powershell
.\tools\BlazorShop.AI.StorefrontBuilder\validate-storefront.ps1 -ProjectRoot BlazorShop.PresentationV2/BlazorShop.Storefront.BuilderDemo -Name BlazorShop.Storefront.BuilderDemo -StoreKey builder-demo
```

Isolation gate:

```powershell
.\scripts\qa\run-storefront-builder-isolation-gate.ps1 -Name BlazorShop.Storefront.BuilderDemo
```

## Validation Gates

The static gate checks:

- StorefrontBuilder JSON/YAML schemas.
- Generated project name, folder, store key, package metadata, and route uniqueness.
- Required analysis artifacts.
- Asset manifest shape and referenced files.
- CSS token and generated style rules.
- Composition files.
- Protected-file guardrails.
- Generated Client/Runtime package references.

The isolation gate additionally restores and builds the generated storefront, packs `BlazorShop.Storefront.Client` and `BlazorShop.Storefront.Runtime`, and scans the generated project for forbidden references.

Browser QA is owned by the Node/Playwright scripts in `tools/BlazorShop.AI.StorefrontBuilder/scripts/qa/`. Run it against an already running generated storefront and commit the resulting QA report when behavior changes.

## Deferred Scope

StorefrontBuilder MVP does not change:

- Commerce Node API contracts.
- Runtime security primitives.
- Cart, checkout, payment, pricing, sellability, or authorization business rules.
- Production deployment topology.
- Marketplace installation UX.
- Optional module packaging.

Those areas remain product/runtime work and must follow the normal V2 architecture docs before implementation.

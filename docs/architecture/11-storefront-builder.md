# StorefrontBuilder Architecture

StorefrontBuilder is development-time tooling for visual reverse engineering and generated storefront preparation. It is not a production service, not a Commerce Node extension, and not a runtime plugin system.

## Ownership

| Area | Owner | Responsibility |
| --- | --- | --- |
| Storefront API contracts | `BlazorShop.PresentationV2/BlazorShop.Storefront.Client` | Generated Storefront API transport and DTOs from the canonical Storefront OpenAPI contract at `contracts/storefront/storefront.openapi.json`. |
| Neutral runtime package | `BlazorShop.PresentationV2/BlazorShop.Storefront.Runtime` | Store context, server-side generated-client registration, capability reading, normalized errors, and BFF-safe result primitives. |
| Portable component package | `BlazorShop.PresentationV2/BlazorShop.Storefront.Components` | Browser-safe Storefront contracts, headless interaction state, Browser local API primitives, and temporary compatibility component primitives that stay independent of Storefront V2 host, backend projects, and server-only APIs. |
| Neutral skeleton | `BlazorShop.PresentationV2/BlazorShop.Storefront.Starter` | Template source for generated storefronts. It stays reusable and store-neutral. |
| Generated proof artifacts | `artifacts/storefront-builder/generated/{ProjectName}` or `obj/storefront-builder/generated/{ProjectName}` | Disposable generated storefront proofs created on demand from Starter and StorefrontBuilder. |
| Builder tooling | `tools/BlazorShop.AI.StorefrontBuilder` | Capture, analysis, generation, regeneration, validation, and browser QA scripts. |
| Generated proof workflow | `scripts/qa/run-storefront-builder-generated-proof.ps1` | Recreates, restores, builds, validates, and isolation-checks the canonical generated proof artifact. |
| Isolation gate | `scripts/qa/run-storefront-builder-isolation-gate.ps1` | Verifies generated storefronts consume Client/Runtime as packages and avoid forbidden project references. |

Generated storefront artifacts live under ignored output roots:

```text
artifacts/storefront-builder/generated/{ProjectName}
obj/storefront-builder/generated/{ProjectName}
```

The storefront name must be normalized before it is used as a folder, project name, namespace segment, or file prefix. Unsafe names must fail before files are created. Generated proof output must not be added to `BlazorShop.sln` by default.

## Boundary Model

StorefrontBuilder follows the existing Storefront API and BFF model:

```text
Generated storefront SSR
  -> BlazorShop.Storefront.Runtime
      -> BlazorShop.Storefront.Client
          -> CommerceNode.API api/storefront/stores/{storeKey}/*

Generated browser or WASM features
  -> same-origin generated storefront /api/*
      -> BlazorShop.Storefront.Runtime
          -> BlazorShop.Storefront.Client
              -> CommerceNode.API api/storefront/stores/{storeKey}/*
```

Generated storefronts must not:

- Reference `BlazorShop.Storefront.V2`.
- Reference `BlazorShop.Application`, `BlazorShop.Domain`, `BlazorShop.Infrastructure`, `BlazorShop.CommerceNode.API`, or `BlazorShop.ControlPlane.API`.
- Call Commerce Node, Commerce Admin, Control Plane, or legacy `api/internal/*` routes directly from browser code.
- Do not: copy Storefront V2 transport internals, backend DTOs, credentials, or business rules.
- Mutate `BlazorShop.Storefront.Starter` with store-specific CSS, assets, pages, analysis artifacts, or AI-tuned components.

## Starter And Generated Storefront Compatibility

`BlazorShop.Storefront.Starter` is the neutral skeleton source, not the production storefront and not a copy target for `BlazorShop.Storefront.V2`.

Starter consumer rules:

- Use the `BlazorShop.Storefront.Client` package for Storefront API transport and generated DTO contracts. The package is generated from `contracts/storefront/storefront.openapi.json`.
- Use the `BlazorShop.Storefront.Runtime` package for server-side generated-client registration, store context, capability/error primitives, and BFF integration primitives.
- Register Runtime only in the generated server/BFF host. Use `AddStorefrontPlatformRuntime` for the full starter surface, or explicit `AddStorefront{Capability}Runtime` methods when a generated host intentionally supports a smaller surface.
- Use `BlazorShop.Storefront.Components` only for reusable browser-safe contracts/headless behavior and Browser local API primitives when a starter or generated storefront needs that shared component package.
- Do not use `BlazorShop.Storefront.Components/Features` wrappers as generated visual templates; they are CSS-neutral compatibility wrappers, not stable presentation contracts.
- Starter owns its neutral visual templates; Starter-local neutral visual components may remain local and must not copy Storefront V2 visual components.
- Do not reference `BlazorShop.Storefront.V2`.
- Do not reference backend/API/core projects, Control Plane Web, or `BlazorShop.Web.SharedV2`/`Web.SharedV2`.

Generated/custom storefront consumer rules:

- Use project names in the `BlazorShop.Storefront.{Name}` pattern after safe normalization.
- `BlazorShop.Storefront.{Name}` owns generated markup, generated CSS, store-specific assets, pages, visual analysis artifacts, and AI-tuned components inside the generated/custom storefront project.
- Generated/custom storefronts may use `BlazorShop.Storefront.Components` contracts/headless behavior and Browser primitives, but they must not use Storefront V2 visual markup as their presentation source.
- Generated/custom storefronts must not copy Components `Features` wrappers as their presentation source; those wrappers are CSS-neutral compatibility wrappers only.
- StorefrontBuilder may replace product card, grid, gallery, purchase, cart, checkout, and account visual templates in the generated/custom project without changing shared behavior contracts.
- Route protected browser actions through same-origin BFF endpoints before Storefront Runtime or Commerce Node Storefront APIs.
- Browser and WASM code must not reference `BlazorShop.Storefront.Runtime`; it consumes same-origin generated endpoints and browser-safe `BlazorShop.Storefront.Components` contracts/headless behavior.
- Use generated package contracts instead of guessing API response shapes.
- Do not reference `BlazorShop.Storefront.V2`, backend/API/core projects, Control Plane Web, `BlazorShop.Web.SharedV2`/`Web.SharedV2`, or generated proof output from another store.

## Source Order

When visual evidence and backend capability do not agree, decisions follow this order:

1. Canonical Storefront OpenAPI (`contracts/storefront/storefront.openapi.json`) and `BlazorShop.Storefront.Client` contracts.
2. `BlazorShop.Storefront.Runtime` server-side registration and facade contract.
3. `BlazorShop.Storefront.Components` presentation contract.
4. Starter generation/runtime contract.
5. Backend capability state.
6. Starter feature manifest and protected-file rules.
7. Visual evidence captured from the reference storefront.
8. AI inference recorded explicitly when evidence is incomplete.

## Generated Artifacts

Each generated storefront artifact keeps reviewable artifacts under:

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

These files are source evidence for reviewing that generated artifact. They are disposable with the artifact unless a phase explicitly promotes a specific artifact into tracked evidence.

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
.\tools\BlazorShop.AI.StorefrontBuilder\regenerate-storefront.ps1 -ProjectRoot artifacts/storefront-builder/generated/BlazorShop.Storefront.GeneratedProof -Scope all
```

Supported scopes are `all`, `page`, `component`, `css`, `validate`, and `conflicts`.

Static validation command:

```powershell
.\tools\BlazorShop.AI.StorefrontBuilder\validate-storefront.ps1 -ProjectRoot artifacts/storefront-builder/generated/BlazorShop.Storefront.GeneratedProof -Name BlazorShop.Storefront.GeneratedProof -StoreKey sample
```

Isolation gate:

```powershell
.\scripts\qa\run-storefront-builder-isolation-gate.ps1 -ProjectRoot artifacts/storefront-builder/generated/BlazorShop.Storefront.GeneratedProof -Name BlazorShop.Storefront.GeneratedProof
```

Canonical proof workflow:

```powershell
.\scripts\qa\run-storefront-builder-generated-proof.ps1
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

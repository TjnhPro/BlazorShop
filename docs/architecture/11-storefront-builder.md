# StorefrontBuilder Architecture

StorefrontBuilder is development-time tooling for visual reverse engineering and generated storefront preparation. It is not a production service, not a Commerce Node extension, and not a runtime plugin system.

## Ownership

| Area | Owner | Responsibility |
| --- | --- | --- |
| Storefront API contracts | `BlazorShop.PresentationV2/BlazorShop.Storefront.Client` | Generated Storefront API transport and DTOs from the canonical Storefront OpenAPI contract at `contracts/storefront/storefront.openapi.json`. |
| Neutral runtime package | `BlazorShop.PresentationV2/BlazorShop.Storefront.Runtime` | Store context, server-side generated-client registration, capability reading, normalized errors, and BFF-safe result primitives. |
| Storefront application package | `BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation` | Shared App/Routes/page services/BFF/SEO/media composition and view-slot contracts consumed by V2, Starter, and generated storefronts. |
| Browser runtime package | `BlazorShop.PresentationV2/BlazorShop.Storefront.Browser` | Same-origin local API client primitives and browser-side cart, checkout, and account controllers for interactive WASM flows. |
| Portable component package | `BlazorShop.PresentationV2/BlazorShop.Storefront.Components` | Browser-safe Storefront contracts, headless interaction state, and temporary compatibility component primitives that stay independent of Storefront V2 host, backend projects, and server-only APIs. |
| Neutral skeleton | `BlazorShop.PresentationV2/BlazorShop.Storefront.Starter` | Template source for generated storefronts. It stays reusable and store-neutral. |
| Generated proof artifacts | `artifacts/storefront-builder/generated/{ProjectName}` or `obj/storefront-builder/generated/{ProjectName}` | Disposable generated storefront proofs created on demand from Starter and StorefrontBuilder. |
| Builder tooling | `tools/BlazorShop.AI.StorefrontBuilder` | Capture, analysis, generation, regeneration, validation, and browser QA scripts. |
| Generated proof workflow | `scripts/qa/run-storefront-builder-generated-proof.ps1` | Recreates, restores, builds, validates, isolation-checks, and runs structure, fast functional, or full fixture-backed browser proof for the canonical generated proof artifact. |
| Regeneration ownership gate | `scripts/qa/run-storefront-builder-regeneration-gate.ps1` | CI-friendly generated update proof for no-op determinism, scoped updates, manual-edit conflicts, user-owned preservation, protected-file rejection, and obsolete-file reporting without live Commerce Node data. |
| Isolation gate | `scripts/qa/run-storefront-builder-isolation-gate.ps1` | Verifies generated storefronts consume Presentation/Components as direct packages, keep Client/Runtime package metadata for transitive package proof, and avoid forbidden Runtime/Client, project, V2, Web.SharedV2, backend, core, or API references. |

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
  -> BlazorShop.Storefront.Presentation
  -> BlazorShop.Storefront.Runtime
      -> BlazorShop.Storefront.Client
          -> CommerceNode.API api/storefront/stores/{storeKey}/*

Generated browser or WASM features
  -> same-origin generated storefront /api/*
      -> BlazorShop.Storefront.Presentation
      -> BlazorShop.Storefront.Runtime
          -> BlazorShop.Storefront.Client
              -> CommerceNode.API api/storefront/stores/{storeKey}/*
```

Generated storefronts must not:

- Reference `BlazorShop.Storefront.V2`.
- Reference `BlazorShop.Application`, `BlazorShop.Domain`, `BlazorShop.Infrastructure`, `BlazorShop.CommerceNode.API`, or `BlazorShop.ControlPlane.API`.
- Recreate Storefront App/Routes/page services/BFF/SEO/media logic that belongs to `BlazorShop.Storefront.Presentation`.
- Call Commerce Node, Commerce Admin, Control Plane, or legacy `api/internal/*` routes directly from browser code.
- Do not: copy Storefront V2 transport internals, backend DTOs, credentials, or business rules.
- Mutate `BlazorShop.Storefront.Starter` with store-specific CSS, assets, pages, analysis artifacts, or AI-tuned components.

## Starter And Generated Storefront Compatibility

`BlazorShop.Storefront.Starter` is the neutral skeleton source, not the production storefront and not a copy target for `BlazorShop.Storefront.V2`.

Starter consumer rules:

- In monorepo development, consume `BlazorShop.Storefront.Presentation` through a `ProjectReference`; in independent proof and generated projects, rewrite it to a `PackageReference`.
- Use `BlazorShop.Storefront.Presentation` for server-side storefront application registration. Presentation composes `BlazorShop.Storefront.Runtime` internally for generated-client registration, store context, capability/error primitives, and BFF integration primitives.
- Use the `BlazorShop.Storefront.Presentation` package for shared App/Routes/page services/BFF/SEO/media composition. Starter/generated projects provide views, assets, copy, feature manifests, and host configuration.
- Let Runtime own direct `BlazorShop.Storefront.Client` transport and generated DTO package usage. Presentation exposes the Runtime dependency to visual hosts. Starter/generator metadata still pins Client/Runtime package versions for package proof compatibility, but Starter/generated source must not directly compile against Runtime or Client types unless a documented low-level transport extension explicitly requires it.
- Register view slots through `StorefrontFoundationViewSet`; generated visual files must not declare `@page` or register route assemblies.
- Register Presentation only in the generated server/BFF host. Presentation composes Runtime internally through `AddStorefrontApplication`; generated hosts register only their visual foundation view set.
- Use `BlazorShop.Storefront.Components` only for reusable browser-safe contracts/headless behavior when a starter or generated storefront needs that shared component package. Use `BlazorShop.Storefront.Browser` only for browser-side same-origin controller/runtime behavior, not for visual templates.
- Do not import or recreate retired `BlazorShop.Storefront.Components.Features` wrappers. Generated storefronts consume shared `Contracts`, `Headless`, and `Browser` primitives, then own their project-local markup, CSS, layout, assets, and copy.
- Starter owns its neutral visual templates; Starter-local neutral visual components may remain local and must not copy Storefront V2 visual components.
- Do not reference `BlazorShop.Storefront.V2`.
- Do not reference backend/API/core projects, Control Plane Web, or `BlazorShop.Web.SharedV2`/`Web.SharedV2`.

Generated/custom storefront consumer rules:

- Use project names in the `BlazorShop.Storefront.{Name}` pattern after safe normalization.
- Consume `BlazorShop.Storefront.Presentation` through a package when full storefront routes/BFF/SEO/media behavior is needed; do not generate route/BFF/SEO application logic from scratch.
- `BlazorShop.Storefront.{Name}` owns generated markup, generated CSS, store-specific assets, pages, visual analysis artifacts, and AI-tuned components inside the generated/custom storefront project.
- Generated/custom storefronts may use `BlazorShop.Storefront.Components` contracts/headless behavior and `BlazorShop.Storefront.Browser` controllers where interactive WASM behavior is explicitly needed, but they must not use Storefront V2 visual markup as their presentation source.
- Generated/custom storefronts must not copy retired Components `Features` wrappers as their presentation source; no active shared wrapper source exists in `BlazorShop.Storefront.Components`.
- StorefrontBuilder may replace product card, grid, gallery, purchase, cart, checkout, and account visual templates in the generated/custom project without changing shared behavior contracts.
- Treat `BlazorShop.Storefront.Presentation` `@page` directives and generator-relevant `StorefrontRoutes` constants as the route truth. `starter-generation.contract.yaml` must list matching route metadata before generated storefronts consume a new page route. Account child routes such as `/account/profile`, `/account/addresses`, `/account/orders`, and `/account/change-password` are child metadata for the same account WASM host route, not separate generated route declarations. Current payment routes are `/payment-success`, `/payment-cancel`, and `/payment/result`.
- Route protected browser actions through same-origin BFF endpoints before Storefront Runtime or Commerce Node Storefront APIs.
- Product purchase, product-selection preview, add-to-cart, cart badge, and consent browser action binders are owned by `BlazorShop.Storefront.Presentation`. Generated markup inherits functional descriptors from Starter templates and declares semantic descriptors such as `data-storefront-product-purchase`, `data-selection-preview-route`, `data-storefront-purchase-quantity`, `data-storefront-command="cart.add-line"`, `data-storefront-product-purchase-submit`, and `data-storefront-cart-badge`.
- Generated visual templates may render Presentation semantic product-selection event values such as price, stock, image, SKU, and GTIN labels. They must not read raw preview fields, build product-selection/add-to-cart payloads, or invoke browser application commands directly.
- Generated storefronts must not emit copied browser application controller JavaScript. `wwwroot/js/storefront-builder.functional.js` is forbidden. If a later phase needs generated visual JavaScript, it must live only under `wwwroot/js/visual`, register through the visual script slot, and listen to Presentation semantic events without invoking application commands or constructing command payloads.
- Browser and WASM code must not reference `BlazorShop.Storefront.Runtime`; it consumes same-origin generated endpoints through `BlazorShop.Storefront.Browser` and browser-safe `BlazorShop.Storefront.Components` contracts/headless behavior.
- Use generated package contracts instead of guessing API response shapes.
- Do not reference `BlazorShop.Storefront.V2`, backend/API/core projects, Control Plane Web, `BlazorShop.Web.SharedV2`/`Web.SharedV2`, or generated proof output from another store.

## Source Order

When visual evidence and backend capability do not agree, decisions follow this order:

1. Canonical Storefront OpenAPI (`contracts/storefront/storefront.openapi.json`) and `BlazorShop.Storefront.Client` contracts.
2. `BlazorShop.Storefront.Runtime` server-side registration and facade contract.
3. `BlazorShop.Storefront.Components` presentation contract.
4. Storefront Presentation route/page/BFF/SEO/view-slot contract.
5. Starter generation/runtime contract.
6. Backend capability state.
7. Starter feature manifest and protected-file rules.
8. Visual evidence captured from the reference storefront.
9. AI inference recorded explicitly when evidence is incomplete.

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
- `fast-foundation-functional-report.md`
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

CI-friendly regeneration ownership gate:

```powershell
.\scripts\qa\run-storefront-builder-regeneration-gate.ps1
```

Static validation command:

```powershell
.\tools\BlazorShop.AI.StorefrontBuilder\validate-storefront.ps1 -ProjectRoot artifacts/storefront-builder/generated/BlazorShop.Storefront.GeneratedProof -Name BlazorShop.Storefront.GeneratedProof -StoreKey sample
```

Isolation gate:

```powershell
.\scripts\qa\run-storefront-builder-isolation-gate.ps1 -ProjectRoot artifacts/storefront-builder/generated/BlazorShop.Storefront.GeneratedProof -Name BlazorShop.Storefront.GeneratedProof
```

Canonical structure proof workflow:

```powershell
.\scripts\qa\run-storefront-builder-generated-proof.ps1 -ProofLevel Structure
```

Canonical fast foundation functional proof workflow:

```powershell
.\scripts\qa\run-storefront-builder-generated-proof.ps1 -ProofLevel FoundationFunctionalFast
```

Canonical full foundation functional proof workflow:

```powershell
.\scripts\qa\run-storefront-builder-generated-proof.ps1 -ProofLevel FoundationFunctionalFull
```

## Validation Gates

The static gate checks:

- StorefrontBuilder JSON/YAML schemas.
- Generated project name, folder, store key, package metadata, and no generated `@page` route directives.
- Required analysis artifacts.
- Asset manifest shape and referenced files.
- CSS token and generated style rules.
- Composition files.
- Browser action descriptor markup and absence of generated functional JavaScript outside an explicitly allowed visual-only script zone.
- Protected-file guardrails.
- Generated Runtime/Presentation/Components package references plus Client compatibility metadata.

The isolation gate additionally restores and builds the generated storefront, packs `BlazorShop.Storefront.Client`, `BlazorShop.Storefront.Runtime`, `BlazorShop.Storefront.Presentation`, and `BlazorShop.Storefront.Components`, and scans the generated project for forbidden references. Generated projects reference Presentation and Components directly; Runtime is consumed through Presentation, and Client is packed/pinned as Runtime's generated transport dependency.

`Structure` proof generates/restores/builds the proof project, runs the static StorefrontBuilder gate, runs the isolation gate, runs the shared visual consumer boundary validator, proves post-regeneration build, proves deterministic no-op regeneration, and proves manual-edit conflict reporting. `FoundationFunctionalFast` is the PR gate: it starts from deterministic generated proof markup, uses mocked same-origin Presentation BFF routes in Playwright, and proves product descriptors, selection preview, add-to-cart, cart badge, cart page, checkout route, consent save/revoke, and no direct Commerce Node browser calls. `FoundationFunctionalFull` is the manual/scheduled/release gate: it requires fixture-backed store/category/product/page/payment data, starts the generated host, runs visual smoke QA, and runs commerce regression checks for same-origin add-to-cart, cart badge, cart, checkout entry, account route, SEO, consent, missing slug, and direct Commerce Node browser-call rejection. `FoundationFunctional` remains a compatibility alias for the full gate.

Browser QA is owned by the Node/Playwright scripts in `tools/BlazorShop.AI.StorefrontBuilder/scripts/qa/`. Run the fast proof on PR and closure guardrail changes; run the regeneration ownership gate whenever generated ownership, manifests, or regeneration behavior changes; run the full proof for manual, scheduled, and release validation. Commit the resulting QA report only when a phase explicitly asks for tracked evidence.

## Deferred Scope

StorefrontBuilder MVP does not change:

- Commerce Node API contracts.
- Runtime security primitives.
- Cart, checkout, payment, pricing, sellability, or authorization business rules.
- Production deployment topology.
- Marketplace installation UX.
- Optional module packaging.

Those areas remain product/runtime work and must follow the normal V2 architecture docs before implementation.

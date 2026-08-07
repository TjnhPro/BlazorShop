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
| Neutral browser skeleton | `BlazorShop.PresentationV2/BlazorShop.Storefront.Starter.WASM` | Template source for generated interactive account, cart, and checkout WASM roots. It uses Browser runtime/controllers and browser-safe Components contracts without Runtime/Client direct references. |
| Generated proof artifacts | `artifacts/storefront-builder/{ProjectName}`, `artifacts/storefront-builder/generated/{ProjectName}`, or `obj/storefront-builder/generated/{ProjectName}` | Disposable generated storefront proofs created on demand from Starter and StorefrontBuilder. |
| Builder tooling | `tools/BlazorShop.AI.StorefrontBuilder` | Capture, analysis, generation, regeneration, validation, and browser QA scripts. |
| Reverse-engineering evidence tooling | `tools/BlazorShop.AI.StorefrontReverseEngineering` | Development-time executable that creates reference-site evidence, workflow state, validation reports, originality notes, visual-blueprint drafts, reviewed mappings, and Phase 3C/3D/3E-hardened portable `analysis/agent-handoff/*` packages for Phase 4 StorefrontBuilder consumption. |
| Visual skill/report workspace | `tools/BlazorShop.AI.Visual` | Development-time agent instruction, schema, reference, adapter, and example workspace for Phase 4 visual plan/implementation/QA reports. It is not a generator, runtime package, production service, or project reference target. |
| Generated proof workflow | `scripts/qa/run-storefront-builder-generated-proof.ps1` | Recreates, restores, builds, validates, isolation-checks, and runs structure, fast functional, or full fixture-backed browser proof for the canonical generated proof artifact. |
| Full fixture proof wrapper | `scripts/qa/run-storefront-builder-full-proof-with-fixture.ps1` | CI-safe manual/scheduled/release wrapper that stops any existing V2 runtime, starts Docker dependencies plus the local Control Plane/Commerce Node/Storefront fixture runtime, verifies fixture data, runs `FoundationFunctionalFull`, collects reports, and tears down in `finally`. |
| Regeneration ownership gate | `scripts/qa/run-storefront-builder-regeneration-gate.ps1` | CI-friendly generated update proof for no-op determinism, scoped updates, manual-edit conflicts, user-owned preservation, protected-file rejection, and obsolete-file reporting without live Commerce Node data. |
| Isolation gate | `scripts/qa/run-storefront-builder-isolation-gate.ps1` | Verifies generated storefronts consume Presentation/Components as direct packages, keep Client/Runtime package metadata for transitive package proof, and avoid forbidden Runtime/Client, project, V2, Web.SharedV2, backend, core, or API references. |
| Phase 4 visual gates | `scripts/qa/run-storefront-phase4-mvp-gate.ps1`, `scripts/qa/run-storefront-phase4-final-closure-gate.ps1` | Local closure gates for the visual skill MVP flow and clean-HEAD Phase 4.12 final closure. They prove runtime visual QA, generated functional behavior, tracked fixture reproducibility, and do not require GitHub Actions. |

Generated storefront artifacts live under ignored output roots:

```text
artifacts/storefront-builder/{ProjectName}
artifacts/storefront-builder/generated/{ProjectName}
obj/storefront-builder/generated/{ProjectName}
```

Each `{ProjectName}` path is the generated workspace root:

```text
{WorkspaceRoot}/
  {ProjectName}.sln
  StorefrontPackageVersions.props
  nuget.config
  docs/storefront-analysis/
  {ProjectName}/
    {ProjectName}.csproj
  {ProjectName}.WASM/
    {ProjectName}.WASM.csproj
```

`artifacts/storefront-builder/generated` remains the default proof output. `artifacts/storefront-builder` is also an approved manual output root when an operator wants the generated project directly under the StorefrontBuilder artifact folder.

The storefront name must be normalized before it is used as a folder, project name, namespace segment, or file prefix. Unsafe names must fail before files are created. Generated proof output must not be added to `BlazorShop.sln` by default. Scripts use `-WorkspaceRoot` for generated workspace input; `-ProjectRoot` is only a temporary compatibility alias and must not mean the server project folder.

Reverse-engineering project artifacts are separate from generated storefront source and live under:

```text
artifacts/storefront-reverse-engineering/projects/{ProjectId}
obj/storefront-reverse-engineering/projects/{ProjectId}
```

`BlazorShop.AI.StorefrontReverseEngineering` is validated by direct `dotnet build`, `dotnet test`, `dotnet run --project`, `validate-handoff`, `inspect-handoff`, and Phase 3 gate commands. It is not added to `BlazorShop.sln` by default, no production runtime project may reference it, and StorefrontBuilder may consume only portable `analysis/agent-handoff/*` packages through the approved Phase 4 preflight, generation-plan compiler, and Starter-based generated project path. StorefrontBuilder must not consume `visual-blueprint.draft.json`, Visual Blueprint v1, raw source analysis, or evidence/report folders as fallback generation input. The ReverseEngineering tool produces evidence, reviewed analysis, and constrained handoff artifacts only; StorefrontBuilder owns Razor/CSS/generated project output.

Phase 3A hardening makes the ReverseEngineering runtime evidence layer deterministic enough for later analysis work: real Chromium fixture tests, stateful per-viewport browser sessions, stabilized full-page capture, native quality gates with stitched fallback, single capture correlation IDs, schema-backed artifacts, workflow run/resume state, safe interaction diffs, and readiness reports. The release gate is:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\qa\run-storefront-reverse-engineering-phase3a-gate.ps1
```

The gate uses local fixtures and StorefrontBuilder compatibility smoke; it must not require an external website or mutate generated storefront source.

ReverseEngineering handoff artifacts are inactive in Phase 3A, Phase 3B, Phase 3C, Phase 3D, and Phase 3E. Phase 4 enables StorefrontBuilder to preflight portable Phase 3E packages under `analysis/agent-handoff/`, compile a deterministic handoff generation plan, create a Starter-based handoff project skeleton, package constrained agent visual tasks, validate visual-only boundaries, run browser visual proof, run bounded repair, and regenerate safely through stored handoff metadata. Phase 3E portability proof requires canonical artifact/schema membership, manifest/readiness agreement, deterministic copied-package validation, and source-aware reviewed slot provenance. Existing non-handoff StorefrontBuilder generation still uses its original StorefrontBuilder artifacts and scripts.

Phase 4 may read only `analysis/agent-handoff/*` and schemas as input after the Phase 3E final runtime gate passes on a clean unchanged `HEAD`. It must fail when `analysis/agent-handoff/handoff-readiness.json` is missing, not passed, or disagrees with `manifest.json` readiness. It must not reinterpret raw reference evidence unless it explicitly runs a new ReverseEngineering pass, must not write into `BlazorShop.Storefront.Starter`, and must not change protected Storefront runtime behavior. Portable preflight uses `validate-handoff`, `inspect-handoff`, and the read-only dry-run loader through `build-storefront.ps1 -Mode preflight-only -HandoffRoot <path>`. Handoff generation uses `build-storefront.ps1 -Mode plan-only|generate|full -HandoffRoot <path>` and writes only generated project artifacts. Source project folders, raw captures, `analysis/pages/*`, `analysis/resolved/*`, `presentation-catalog/*`, `review/*`, and `reports/*` are not fallback inputs.

`tools/BlazorShop.AI.Visual` sits beside StorefrontBuilder and ReverseEngineering as a docs/schema/skill-only workspace. StorefrontBuilder remains the only owner of generated project creation, regeneration, write recording, boundary validation, browser QA execution, and repair wrappers. ReverseEngineering remains the only owner of reference evidence and portable handoff packages. Visual skills consume the generated project's `generation-plan.json`, `agent-task-package/*`, manifests, allowed visual source files, and StorefrontBuilder browser evidence; they must not add `.csproj` files, runtime references, generators, routes, transports, protected browser actions, SEO behavior, auth behavior, cart/checkout semantics, or backend/API calls.

Phase 4.12 final closure is stricter than the early skeleton proof and closes the seeded-evidence gap from Phase 4.11. Skeleton/static fixture proof may validate a handoff project shell with seeded files and planned placeholders, but it is not release closure. Release closure must start from a clean unchanged `HEAD`, validate the tracked portable handoff fixture at `tools/BlazorShop.AI.StorefrontBuilder/tests/generation/fixtures/phase4-11-closure/portable-handoff`, run StorefrontBuilder generation with `-HandoffRoot` and `-HandoffSchemaRoot`, generate fresh disposable pilot output under `obj/storefront-builder/generated/`, and use the StorefrontBuilder-generated `generation-plan.json` and `agent-task-package/manifest.json`. The gate must not manually write a generation plan, manually write a task package, or accept marker-only handoff input as closure evidence. It then requires the complete visual artifact chain, runs automatic changed-file detection, runs runtime visual QA against a generated host, materializes `visual-qa-report.json` from the current `visual-qa-runtime-summary.json`, runs the MVP gate, runs `FoundationFunctionalFast` at minimum, runs regeneration/no-op ownership proof, and finishes with the same clean `HEAD`. GitHub Actions are not required while disabled during local development. Full fixture commerce proof through `run-storefront-builder-full-proof-with-fixture.ps1` is the release-level commerce proof when the fixture runtime is available.

Phase 3A does not perform full design-token extraction, semantic token normalization, ecommerce region mapping, component detection, component generation, or visual generation. Captured reference assets, logos, copy, and brand-specific visual material are reference-only by default until later human review and an approved workflow clear reuse. Phase 3B starts from the stable runtime evidence and adds visual interpretation: design-token extraction, semantic token normalization, section segmentation, responsive comparison, component detection, ecommerce region mapping, confidence scoring, human review, and reviewed blueprint assembly for later handoff planning. Phase 3D proves resolved reviewed inputs, exact slot enforcement, per-viewport evidence packaging, and positive/negative closure behavior. Phase 3E proves the handoff can be copied, validated, and dry-run loaded without its source project, and blocks orphan reviewed slot mappings that do not belong to the active page composition. Phase 4 exposes that portable validation/dry-run path through StorefrontBuilder and adds controlled visual generation/QA/regeneration on top of it.

Compatibility map for future handoff:

| Current StorefrontBuilder artifact | ReverseEngineering artifact prepared in Phase 3A |
| --- | --- |
| `capture-manifest.json` | `captures/{pageId}/capture-manifest.json` and viewport manifests. |
| `asset-manifest.yaml` | `asset-inventory.normalized.json` plus `analysis/originality-audit.json`. |
| `page-topology.yaml` | `analysis/page-topology.draft.json`. |
| `design-tokens.yaml` | Bounded computed-style evidence; token extraction remains deferred. |
| `ai-inference-log.json` | Optional `analysis/ai-inference-log.json`; rule-based fallback requires none. |

The existing Node Playwright capture and QA scripts remain the StorefrontBuilder baseline only. ReverseEngineering Phase 3A uses the .NET `PlaywrightReferenceBrowser` runtime path for non-fixture capture plus fixture/synthetic adapters for tests; it no longer wraps the StorefrontBuilder Node capture script as a supported runtime adapter. Do not retire or replace the StorefrontBuilder Node scripts until a later StorefrontBuilder parity phase approves that change.

Final Phase 3A closure means the runtime extracts rendered evidence before native screenshot capture, records automatic fallback decisions, requires real stitch artifacts for stitched captures, validates readiness across file existence/schema/image quality/evidence depth/correlation/originality/latest-run state, and exposes readiness through `inspect` from `reports/readiness-report.json`. Phase 3B may consume this evidence foundation; it must not reopen capture fallback, readiness depth, inspect state, or ReverseEngineering Node bridge cleanup as prerequisite repair work.

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
- In monorepo development, Starter server references `BlazorShop.Storefront.Starter.WASM` as its additional interactive assembly, registers `AddStorefrontBrowserControllers()`, and maps the WASM assembly through `MapStorefrontApplication`.
- In monorepo development, Starter server may use ProjectReferences to Presentation, Components, Browser, and Starter.WASM. Independent proof and generated projects rewrite Presentation, Components, and Browser to PackageReferences and keep only the generated server to generated sibling WASM ProjectReference.
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
- Generated server hosts must map their generated sibling WASM assembly and must not map Storefront V2, Starter.WASM, or any external monorepo source assembly.
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

Handoff-generated projects additionally keep:

- `generation-plan.json` and `generation-plan.yaml`
- `handoff-generation-summary.md`
- `handoff-placeholders.json`
- `agent-task-package/`
- `agent-written-files.json` after constrained agent visual writes
- `repair-history.md` after bounded visual repair attempts
- `visual-plan.json` when `storefront-visual-plan` is used
- `visual-implementation-checklist.todo.md` and `visual-checkpoints/{operationId}/visual-checkpoint.json` when `storefront-visual-implement` is used
- `visual-implementation-report.json` and `visual-qa-report.json` when visual implementation or QA evidence is produced
- Reference visual evidence and accepted-difference records when final visual QA compares against a reference contract
- `phase4-mvp-gate-report.json` and `phase4-mvp-gate-report.md` after the Phase 4 MVP gate

Current review and QA artifacts:

- `review-summary.md`
- `regeneration-report.md`
- `fast-foundation-functional-report.md`
- `visual-qa-report.md`
- `functional-commerce-report.md`
- `full-proof-with-fixture-report.md`
- `mvp-poc-report.md`

These files are source evidence for reviewing that generated artifact. They are disposable with the artifact unless a phase explicitly promotes a specific artifact into tracked evidence.

StorefrontBuilder tool provenance uses one generator version source: `tools/BlazorShop.AI.StorefrontBuilder/version.json`. Generated `metadata.yaml` and generated-file manifest entries must use that same `generatorVersion`; validation fails when they drift.

## Entrypoints

Primary builder command:

```powershell
.\tools\BlazorShop.AI.StorefrontBuilder\build-storefront.ps1 -Url https://reference.example -Name Demo -StoreKey sample -Mode validate-only
```

Supported modes:

| Mode | Behavior |
| --- | --- |
| `analyze-only` | Writes review artifacts from the reference URL for the target generated project. |
| `preflight-only` | Validates a portable `analysis/agent-handoff/*` package and writes a preflight report without generating a storefront project. |
| `plan-only` | Produces a dry-run generation plan. |
| `generate` | Creates a generated storefront project from Starter and writes analysis artifacts. |
| `update` | Runs regeneration for the generated storefront. |
| `validate-only` | Runs the static validation gate for the generated storefront. |
| `full` | Generates, writes artifacts, validates, and reports visual/commerce QA script entrypoints. |

Handoff preflight:

```powershell
.\tools\BlazorShop.AI.StorefrontBuilder\build-storefront.ps1 -Mode preflight-only -HandoffRoot <portable-handoff-root> -HandoffSchemaRoot tools\BlazorShop.AI.StorefrontReverseEngineering\Schemas
```

Handoff plan review:

```powershell
.\tools\BlazorShop.AI.StorefrontBuilder\build-storefront.ps1 -Mode plan-only -Name Demo -StoreKey sample -HandoffRoot <portable-handoff-root> -HandoffSchemaRoot tools\BlazorShop.AI.StorefrontReverseEngineering\Schemas
```

Handoff project generation:

```powershell
.\tools\BlazorShop.AI.StorefrontBuilder\build-storefront.ps1 -Mode generate -Name Demo -StoreKey sample -OutputRoot obj/storefront-builder/generated -HandoffRoot <portable-handoff-root> -HandoffSchemaRoot tools\BlazorShop.AI.StorefrontReverseEngineering\Schemas -Force
```

Handoff generation writes a Starter-based `BlazorShop.Storefront.{Name}` project, records `generationMode: handoff-project-skeleton` in `metadata.yaml`, stores the compiled generation plan under `docs/storefront-analysis/`, writes an agent task package containing only handoff-local inputs and allowed visual target boundaries, and does not mutate Starter or add the generated project to `BlazorShop.sln`.

Regeneration command:

```powershell
.\tools\BlazorShop.AI.StorefrontBuilder\regenerate-storefront.ps1 -WorkspaceRoot artifacts/storefront-builder/generated/BlazorShop.Storefront.GeneratedProof -Scope all
```

Supported scopes are `all`, `page`, `component`, `css`, `foundation`, `validate`, and `conflicts`. Non-handoff regeneration creates a fresh candidate from the current Starter/template source, writes a shared action plan, and applies only safe generated/managed changes from that candidate into the target. Handoff regeneration preserves stored handoff metadata, copies the target into a candidate, reapplies the stored `generation-plan.json`, rejects package/readiness/Starter contract drift, and applies only safe generated/managed visual changes. Manual edits to generated/managed files are reported as conflicts, user-owned/artifact-only files are preserved, protected files are skipped unless an explicit reviewed foundation path is used, and obsolete candidates are reported instead of deleted.

Use `-WhatIf` with any update scope to run the same candidate generation and planning pipeline without copying changed files into the generated target:

```powershell
.\tools\BlazorShop.AI.StorefrontBuilder\regenerate-storefront.ps1 -WorkspaceRoot artifacts/storefront-builder/generated/BlazorShop.Storefront.GeneratedProof -Scope all -WhatIf
```

`-WhatIf` keeps candidate cleanup enabled and writes a stable report outside the generated target. By default the report is `{OutputRoot}/.regeneration-reports/{ProjectName}-{operationId}.md`. The console prints `WhatIf report: <path>`, summary counts for create/update/platform metadata/conflict/obsolete/protected-or-user-owned skips, meaningful `filePath: action - reason` lines, and conflict next-action guidance when conflicts exist. Use `-WhatIfReportPath <path>` only for approved report locations under the output report folder, repo `obj`, or `artifacts/storefront-builder`; target-project paths are rejected.

Use `-Scope foundation` only for explicit platform metadata updates such as `StorefrontPackageVersions.props`, `starter-generation.contract.yaml`, and generated metadata/package contract fields:

```powershell
.\tools\BlazorShop.AI.StorefrontBuilder\regenerate-storefront.ps1 -WorkspaceRoot artifacts/storefront-builder/generated/BlazorShop.Storefront.GeneratedProof -Scope foundation -ValidateAfterApply -BuildAfterApply
```

CI-friendly regeneration ownership gate:

```powershell
.\scripts\qa\run-storefront-builder-regeneration-gate.ps1
```

Constrained agent writes are recorded after an agent updates generated visual files:

```powershell
node tools\BlazorShop.AI.StorefrontBuilder\scripts\generate\record-agent-visual-writes.mjs --workspace-root <generated-workspace-root> --written-files <comma-separated-generated-visual-paths>
```

The recorder validates that writes are planned generated-owned visual outputs, reject route declarations, direct Commerce Node/Admin/Control Plane calls, protected package paths, business/auth/SEO ownership leaks, and unplanned JavaScript. Bounded visual repair uses only failure output, the generation plan, and the generated agent task package:

```powershell
node tools\BlazorShop.AI.StorefrontBuilder\scripts\qa\repair-visual-generation.mjs --workspace-root <generated-workspace-root> --failure-report <report.md> --max-attempts 2
```

Phase 4 visual skills run only after StorefrontBuilder creates a handoff-generated project and its `agent-task-package/manifest.json` exists. Use the skills in this order: `storefront-visual-plan`, `storefront-visual-implement`, then `storefront-visual-qa`. StorefrontBuilder remains the only project generator and recorder:

```powershell
node tools\BlazorShop.AI.StorefrontBuilder\scripts\generate\record-agent-visual-writes.mjs --workspace-root <generated-workspace-root> --written-files <comma-separated-generated-visual-paths>
node tools\BlazorShop.AI.StorefrontBuilder\scripts\qa\run-visual-qa.mjs --workspace-root <generated-workspace-root> --fixture-root <fixture-root> --screenshot-root obj/storefront-builder/visual-qa-screens
.\scripts\qa\run-storefront-phase4-mvp-gate.ps1 -WorkspaceRoot <generated-workspace-root> -FixtureRoot <fixture-root> -HandoffRoot <portable-handoff-root> -CommandTimeoutSeconds 600
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\qa\run-storefront-phase4-final-closure-gate.ps1 -CommandTimeoutSeconds 900
```

Static validation command:

```powershell
dotnet restore artifacts/storefront-builder/generated/BlazorShop.Storefront.GeneratedProof/BlazorShop.Storefront.GeneratedProof.sln --no-cache --force-evaluate
dotnet build artifacts/storefront-builder/generated/BlazorShop.Storefront.GeneratedProof/BlazorShop.Storefront.GeneratedProof.sln --no-restore
dotnet run --project artifacts/storefront-builder/generated/BlazorShop.Storefront.GeneratedProof/BlazorShop.Storefront.GeneratedProof/BlazorShop.Storefront.GeneratedProof.csproj
```

```powershell
.\tools\BlazorShop.AI.StorefrontBuilder\validate-storefront.ps1 -WorkspaceRoot artifacts/storefront-builder/generated/BlazorShop.Storefront.GeneratedProof -Name BlazorShop.Storefront.GeneratedProof -StoreKey sample
```

Isolation gate:

```powershell
.\scripts\qa\run-storefront-builder-isolation-gate.ps1 -WorkspaceRoot artifacts/storefront-builder/generated/BlazorShop.Storefront.GeneratedProof -Name BlazorShop.Storefront.GeneratedProof
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

Self-contained full fixture proof for scheduled/manual/release validation:

```powershell
.\scripts\qa\run-storefront-builder-full-proof-with-fixture.ps1
```

Use `.\scripts\qa\run-storefront-builder-full-proof-with-fixture.ps1 -Describe` to inspect the CI runtime bootstrap without starting services. The wrapper uses the local V2 fixture ports from `scripts/env/v2-local.env` (`5280`, `5281`, `5180`, `18598`) and starts the generated proof host on `18620`, so the generated host does not conflict with Storefront V2.

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
- Generated Runtime/Presentation/Components/Browser package references plus Client compatibility metadata and package provenance.

The isolation gate additionally restores and builds the generated storefront, packs `BlazorShop.Storefront.Client`, `BlazorShop.Storefront.Runtime`, `BlazorShop.Storefront.Presentation`, `BlazorShop.Storefront.Components`, and `BlazorShop.Storefront.Browser`, and scans the generated project for forbidden references. Generated projects reference Presentation, Components, and Browser directly where needed; Runtime is consumed through Presentation, and Client is packed/pinned as Runtime's generated transport dependency. Local proof runners derive immutable `1.0.0-local.{shortSha}` package versions from the current source `HEAD`, clear only exact Storefront package ID/version cache folders, restore with `--no-cache --force-evaluate`, and record package hashes in generated metadata.

`Structure` proof generates/restores/builds the proof project, runs the static StorefrontBuilder gate, runs the isolation gate, runs the shared visual consumer boundary validator, proves post-regeneration build, proves deterministic no-op regeneration, and proves manual-edit conflict reporting. `FoundationFunctionalFast` is the PR gate: it starts from deterministic generated proof markup, uses mocked same-origin Presentation BFF routes in Playwright, and proves product descriptors, selection preview, add-to-cart, cart badge, cart page, checkout route, consent save/revoke, and no direct Commerce Node browser calls. `FoundationFunctionalFull` requires fixture-backed store/category/product/page/payment data, starts the generated host, runs visual smoke QA, and runs commerce regression checks for same-origin add-to-cart, cart badge, cart, checkout entry, account route, SEO, consent, missing slug, and direct Commerce Node browser-call rejection. Run the self-contained wrapper for scheduled/manual/release validation because it owns fixture runtime bootstrap, endpoint checks, report collection, and teardown. `FoundationFunctional` remains a compatibility alias for the full gate.

Browser QA is owned by the Node/Playwright scripts in `tools/BlazorShop.AI.StorefrontBuilder/scripts/qa/`. Run the fast proof on PR and closure guardrail changes; run the regeneration ownership gate whenever generated ownership, manifests, or regeneration behavior changes; run the full proof for manual, scheduled, and release validation. Commit the resulting QA report only when a phase explicitly asks for tracked evidence.

For handoff-generated projects, `run-visual-qa.mjs` auto-detects `docs/storefront-analysis/generation-plan.json`, derives required route/slot checks from planned slots, verifies generated CSS linkage, seeded/mock data visibility, body nonblank state, required slot visibility, browser-action descriptors, generated asset resolution, product gallery shape, and horizontal overflow. Use `--fixture-root <folder>` for file-based seeded/mock proof and `--allow-planned-placeholders` only while proving the generated skeleton before agent visual replacement. Runtime visual proof uses `--base-url` with a running generated host and must not be mixed with `--fixture-root`. The report records screenshots, route/viewport/selector discrepancies, reference evidence reviewed, severity counters, and accepted differences.

Before closing Phase 4 visual-skill work, the MVP gate must prove the generated visual plan/implementation/QA path against a handoff project, and the final closure gate must pass from a clean unchanged `HEAD`. The final closure gate regenerates its pilot from tracked portable handoff fixture input and ignored fresh output, so it must not rely on a pre-existing `obj` project, marker-only handoff folder, seeded task package, seeded generation plan, or copied `visual-qa-report.json`. Runtime closure evidence is bound by operation ID, base URL, screenshot paths, and timestamps from the current-run `visual-qa-runtime-summary.json`. GitHub Actions are not required for this local closure.

```powershell
node tools\BlazorShop.AI.StorefrontBuilder\scripts\qa\run-visual-qa.mjs --workspace-root <generated-workspace-root> --fixture-root <fixture-root> --screenshot-root obj/storefront-builder/visual-qa-screens --allow-planned-placeholders
```

## Deferred Scope

StorefrontBuilder MVP does not change:

- Commerce Node API contracts.
- Runtime security primitives.
- Cart, checkout, payment, pricing, sellability, or authorization business rules.
- Production deployment topology.
- Marketplace installation UX.
- Optional module packaging.

Those areas remain product/runtime work and must follow the normal V2 architecture docs before implementation.
